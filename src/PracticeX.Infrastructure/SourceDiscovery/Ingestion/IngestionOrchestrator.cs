using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Classification;
using PracticeX.Application.SourceDiscovery.Connectors;
using PracticeX.Application.SourceDiscovery.Ingestion;
using PracticeX.Application.SourceDiscovery.Storage;
using PracticeX.Domain.Audit;
using PracticeX.Domain.Documents;
using PracticeX.Domain.Sources;
using PracticeX.Domain.Workflow;
using PracticeX.Infrastructure.Persistence;

namespace PracticeX.Infrastructure.SourceDiscovery.Ingestion;

/// <summary>
/// Persists DiscoveryResult to the canonical pipeline tables and emits audit events.
/// Connectors only describe what was discovered; this class is the only place that
/// touches contract-adjacent storage tables (source_objects, document_assets, etc).
///
/// Important: this never writes to contract.contracts. Approved candidates go to
/// the review queue and only become canonical contracts after explicit reviewer
/// decision.
/// </summary>
public sealed class IngestionOrchestrator(
    PracticeXDbContext dbContext,
    IDocumentStorage storage,
    IDocumentClassifier classifier,
    IClock clock,
    ILogger<IngestionOrchestrator> logger) : IIngestionOrchestrator
{
    public async Task<Result<IngestionBatchSummary>> IngestAsync(
        IngestionRequest request,
        DiscoveryResult discovery,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var batch = new IngestionBatch
        {
            TenantId = request.TenantId,
            SourceType = request.SourceType,
            SourceConnectionId = request.ConnectionId,
            CreatedByUserId = request.InitiatedByUserId,
            Status = IngestionBatchStatus.Running,
            FileCount = discovery.Items.Count,
            StartedAt = now,
            Notes = request.Notes,
            CreatedAt = now
        };
        dbContext.IngestionBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        var summaries = new List<IngestionItemSummary>(discovery.Items.Count);
        var candidateCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        foreach (var item in discovery.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await IngestItemAsync(request, batch.Id, item, cancellationToken);
                summaries.Add(result);

                switch (result.Status)
                {
                    case DocumentCandidateStatus.Skipped:
                        skippedCount++;
                        break;
                    case "error":
                        errorCount++;
                        break;
                    default:
                        candidateCount++;
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ingest item {External} for batch {Batch}", item.ExternalId, batch.Id);
                errorCount++;
            }
        }

        batch.CandidateCount = candidateCount;
        batch.SkippedCount = skippedCount;
        batch.ErrorCount = errorCount;
        batch.CompletedAt = clock.UtcNow;
        batch.Status = errorCount > 0
            ? (candidateCount > 0 ? IngestionBatchStatus.PartialSuccess : IngestionBatchStatus.Failed)
            : IngestionBatchStatus.Completed;
        batch.UpdatedAt = clock.UtcNow;

        var batchAudit = new AuditEvent
        {
            TenantId = request.TenantId,
            ActorType = "user",
            ActorId = request.InitiatedByUserId,
            EventType = "ingestion.batch.completed",
            ResourceType = "ingestion_batch",
            ResourceId = batch.Id,
            MetadataJson = JsonSerializer.Serialize(new
            {
                fileCount = batch.FileCount,
                candidateCount,
                skippedCount,
                errorCount,
                sourceType = request.SourceType
            }),
            CreatedAt = clock.UtcNow
        };
        dbContext.AuditEvents.Add(batchAudit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestionBatchSummary>.Ok(new IngestionBatchSummary
        {
            BatchId = batch.Id,
            FileCount = batch.FileCount,
            CandidateCount = candidateCount,
            SkippedCount = skippedCount,
            ErrorCount = errorCount,
            Status = batch.Status,
            Items = summaries
        });
    }

    private async Task<IngestionItemSummary> IngestItemAsync(
        IngestionRequest request,
        Guid batchId,
        DiscoveredItem item,
        CancellationToken cancellationToken)
    {
        var sourceObject = await dbContext.SourceObjects
            .FirstOrDefaultAsync(
                x => x.ConnectionId == request.ConnectionId && x.ExternalId == item.ExternalId,
                cancellationToken);

        if (sourceObject is null)
        {
            sourceObject = new SourceObject
            {
                TenantId = request.TenantId,
                ConnectionId = request.ConnectionId,
                ExternalId = item.ExternalId,
                Uri = item.Uri ?? item.ExternalId,
                Name = item.Name,
                MimeType = item.MimeType,
                Sha256 = item.Sha256,
                ObjectKind = item.ObjectKind,
                RelativePath = item.RelativePath,
                ParentExternalId = item.ParentExternalId,
                SizeBytes = item.SizeBytes,
                MetadataJson = item.MetadataJson,
                SourceCreatedAt = item.SourceCreatedAt,
                SourceModifiedAt = item.SourceModifiedAt,
                CreatedAt = clock.UtcNow
            };
            dbContext.SourceObjects.Add(sourceObject);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Mail message containers carry no bytes; record but skip asset creation.
        if (item.ObjectKind is SourceObjectKinds.Folder or SourceObjectKinds.MailMessage || item.InlineContent is null || item.InlineContent.Length == 0)
        {
            return new IngestionItemSummary
            {
                SourceObjectId = sourceObject.Id,
                Name = item.Name,
                CandidateType = DocumentCandidateTypes.Other,
                Confidence = 0,
                ReasonCodes = item.ObjectKind == SourceObjectKinds.MailMessage
                    ? [IngestionReasonCodes.OutlookSubjectKeywords]
                    : [],
                Status = item.InlineContent is null && item.ObjectKind != SourceObjectKinds.MailMessage
                    ? DocumentCandidateStatus.Skipped
                    : DocumentCandidateStatus.Candidate,
                RelativePath = item.RelativePath
            };
        }

        // Persist preserved original.
        StoredDocument stored;
        using (var contentStream = new MemoryStream(item.InlineContent))
        {
            stored = await storage.StoreAsync(request.TenantId, item.Name, contentStream, item.MimeType, cancellationToken);
        }

        var existingAsset = await dbContext.DocumentAssets
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.Sha256 == stored.Sha256, cancellationToken);

        DocumentAsset asset;
        bool isDuplicate;
        if (existingAsset is not null)
        {
            asset = existingAsset;
            isDuplicate = true;
        }
        else
        {
            asset = new DocumentAsset
            {
                TenantId = request.TenantId,
                SourceObjectId = sourceObject.Id,
                StorageUri = stored.StorageUri,
                Sha256 = stored.Sha256,
                MimeType = item.MimeType,
                SizeBytes = stored.SizeBytes,
                TextStatus = "pending",
                OcrStatus = "pending",
                CreatedAt = clock.UtcNow
            };
            dbContext.DocumentAssets.Add(asset);
            await dbContext.SaveChangesAsync(cancellationToken);
            isDuplicate = false;
        }

        var classification = classifier.Classify(new ClassificationInput
        {
            FileName = item.Name,
            RelativePath = item.RelativePath,
            MimeType = item.MimeType,
            SizeBytes = stored.SizeBytes,
            FolderHint = item.ParentExternalId,
            Hints = item.Hints
        });

        var reasonCodes = isDuplicate
            ? classification.ReasonCodes.Append(IngestionReasonCodes.DuplicateContent).ToList()
            : classification.ReasonCodes.ToList();

        var candidateStatus = isDuplicate
            ? DocumentCandidateStatus.Skipped
            : classification.Status;

        var candidate = new DocumentCandidate
        {
            TenantId = request.TenantId,
            DocumentAssetId = asset.Id,
            CandidateType = classification.CandidateType,
            Confidence = classification.Confidence,
            Status = candidateStatus,
            ReasonCodesJson = JsonSerializer.Serialize(reasonCodes),
            ClassifierVersion = classifier.Version,
            OriginFilename = item.Name,
            RelativePath = item.RelativePath,
            SourceObjectId = sourceObject.Id,
            CounterpartyHint = classification.CounterpartyHint,
            CreatedAt = clock.UtcNow
        };
        dbContext.DocumentCandidates.Add(candidate);

        var job = new IngestionJob
        {
            TenantId = request.TenantId,
            BatchId = batchId,
            SourceObjectId = sourceObject.Id,
            DocumentAssetId = asset.Id,
            Status = candidateStatus == DocumentCandidateStatus.Skipped
                ? IngestionJobStatus.Skipped
                : IngestionJobStatus.Succeeded,
            Stage = IngestionStage.Classified,
            AttemptCount = 1,
            CreatedAt = clock.UtcNow
        };
        dbContext.IngestionJobs.Add(job);

        if (candidateStatus == DocumentCandidateStatus.PendingReview)
        {
            dbContext.ReviewTasks.Add(new ReviewTask
            {
                TenantId = request.TenantId,
                ResourceType = "document_candidate",
                ResourceId = candidate.Id,
                Reason = $"classifier:{classifier.Version}|type:{classification.CandidateType}",
                Priority = classification.Confidence < 0.7m ? 1 : 2,
                Decision = "pending",
                CreatedAt = clock.UtcNow
            });
        }

        dbContext.AuditEvents.Add(new AuditEvent
        {
            TenantId = request.TenantId,
            ActorType = "user",
            ActorId = request.InitiatedByUserId,
            EventType = isDuplicate ? "ingestion.candidate.duplicate" : "ingestion.candidate.created",
            ResourceType = "document_candidate",
            ResourceId = candidate.Id,
            MetadataJson = JsonSerializer.Serialize(new
            {
                candidateType = classification.CandidateType,
                confidence = classification.Confidence,
                reasonCodes,
                duplicate = isDuplicate
            }),
            CreatedAt = clock.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new IngestionItemSummary
        {
            SourceObjectId = sourceObject.Id,
            DocumentAssetId = asset.Id,
            DocumentCandidateId = candidate.Id,
            Name = item.Name,
            CandidateType = classification.CandidateType,
            Confidence = classification.Confidence,
            ReasonCodes = reasonCodes,
            Status = candidateStatus,
            RelativePath = item.RelativePath
        };
    }
}
