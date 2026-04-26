using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Application.SourceDiscovery.Connectors;
using PracticeX.Application.SourceDiscovery.Ingestion;
using PracticeX.Application.SourceDiscovery.Storage;
using PracticeX.Discovery.Classification;
using PracticeX.Discovery.Contracts;
using PracticeX.Discovery.Validation;
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
    IDocumentValidityInspector validityInspector,
    IComplexityProfiler complexityProfiler,
    IPricingPolicy pricingPolicy,
    IClock clock,
    ILogger<IngestionOrchestrator> logger) : IIngestionOrchestrator
{
    public const string ManifestExternalIdPrefix = "manifest:";
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
            Phase = IngestionBatchPhase.Complete,
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
        ValidityReport? validity = null;
        if (existingAsset is not null)
        {
            asset = existingAsset;
            isDuplicate = true;
        }
        else
        {
            validity = validityInspector.Inspect(item.InlineContent, item.MimeType, item.Name);
            var complexity = complexityProfiler.Profile(item.InlineContent, item.MimeType, item.Name, validity);
            var estimatedHours = pricingPolicy.EstimateHours(complexity);

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
                ValidityStatus = validity.ValidityStatus,
                PageCount = validity.PageCount,
                HasTextLayer = validity.HasTextLayer,
                IsEncrypted = validity.IsEncrypted,
                ExtractionRoute = validity.ExtractionRoute,
                ComplexityTier = complexity.Tier.ToCode(),
                ComplexityFactorsJson = JsonSerializer.Serialize(complexity.Factors),
                ComplexityBlockersJson = JsonSerializer.Serialize(complexity.Blockers),
                MetadataJson = complexity.MetadataJson,
                EstimatedComplexityHours = estimatedHours,
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
            : (validity is null
                ? classification.ReasonCodes.ToList()
                : classification.ReasonCodes.Concat(validity.ReasonCodes).ToList());

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

    public async Task<Result<ManifestScanResult>> ScoreManifestAsync(
        IngestionRequest request,
        IReadOnlyList<ManifestItem> items,
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
            Phase = IngestionBatchPhase.Manifest,
            FileCount = items.Count,
            StartedAt = now,
            Notes = request.Notes,
            CreatedAt = now
        };
        dbContext.IngestionBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.AuditEvents.Add(new AuditEvent
        {
            TenantId = request.TenantId,
            ActorType = "user",
            ActorId = request.InitiatedByUserId,
            EventType = "ingestion.manifest.created",
            ResourceType = "ingestion_batch",
            ResourceId = batch.Id,
            MetadataJson = JsonSerializer.Serialize(new { itemCount = items.Count, sourceType = request.SourceType }),
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var scored = new List<ManifestScoredItem>(items.Count);
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scored.Add(await ScoreManifestItemAsync(request, batch.Id, item, cancellationToken));
        }

        // Manifest pre-scoring populates `notes` with a summary; CandidateCount /
        // SkippedCount stay at 0 because no document_candidates have been created
        // yet. Those counters increment only when bundle ingestion runs.
        batch.Notes = $"manifest:scored={scored.Count} eligible={scored.Count(s => s.RecommendedAction != ManifestRecommendedActions.Skip)} skipped={scored.Count(s => s.RecommendedAction == ManifestRecommendedActions.Skip)}";
        batch.UpdatedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ManifestScanResult>.Ok(new ManifestScanResult
        {
            BatchId = batch.Id,
            Phase = IngestionBatchPhase.Manifest,
            Items = scored
        });
    }

    public async Task<Result<IngestionBatchSummary>> IngestBundleAsync(
        IngestionRequest request,
        Guid manifestBatchId,
        DiscoveryResult discovery,
        CancellationToken cancellationToken)
    {
        var batch = await dbContext.IngestionBatches.FirstOrDefaultAsync(
            x => x.Id == manifestBatchId && x.TenantId == request.TenantId, cancellationToken);

        if (batch is null)
        {
            return Result<IngestionBatchSummary>.Fail("manifest_batch_not_found",
                $"No manifest batch {manifestBatchId} for this tenant.");
        }

        if (batch.Phase == IngestionBatchPhase.Complete)
        {
            return Result<IngestionBatchSummary>.Fail("manifest_already_complete",
                "Manifest batch is already complete; start a new scan.");
        }

        batch.Phase = IngestionBatchPhase.Bundle;
        batch.Status = IngestionBatchStatus.Running;
        batch.UpdatedAt = clock.UtcNow;

        dbContext.AuditEvents.Add(new AuditEvent
        {
            TenantId = request.TenantId,
            ActorType = "user",
            ActorId = request.InitiatedByUserId,
            EventType = "ingestion.bundle.received",
            ResourceType = "ingestion_batch",
            ResourceId = batch.Id,
            MetadataJson = JsonSerializer.Serialize(new { fileCount = discovery.Items.Count }),
            CreatedAt = clock.UtcNow
        });
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

                // Mark the source_object as uploaded so subsequent manifest scans
                // can show "already processed" state.
                var so = await dbContext.SourceObjects.FirstOrDefaultAsync(
                    x => x.Id == result.SourceObjectId, cancellationToken);
                if (so is not null)
                {
                    so.ProposedStatus = SourceObjectProposedStatuses.Uploaded;
                    so.UpdatedAt = clock.UtcNow;
                }

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
                logger.LogError(ex, "Failed to ingest bundle item {External} for batch {Batch}", item.ExternalId, batch.Id);
                errorCount++;
            }
        }

        // Counts are additive over the manifest scan because some manifest items
        // may not have been uploaded (user pruned them). FileCount stays at
        // manifest size; CandidateCount/SkippedCount accumulate.
        batch.CandidateCount += candidateCount;
        batch.SkippedCount += skippedCount;
        batch.ErrorCount += errorCount;
        batch.CompletedAt = clock.UtcNow;
        batch.Phase = IngestionBatchPhase.Complete;
        batch.Status = errorCount > 0
            ? (candidateCount > 0 ? IngestionBatchStatus.PartialSuccess : IngestionBatchStatus.Failed)
            : IngestionBatchStatus.Completed;
        batch.UpdatedAt = clock.UtcNow;

        dbContext.AuditEvents.Add(new AuditEvent
        {
            TenantId = request.TenantId,
            ActorType = "user",
            ActorId = request.InitiatedByUserId,
            EventType = "ingestion.bundle.completed",
            ResourceType = "ingestion_batch",
            ResourceId = batch.Id,
            MetadataJson = JsonSerializer.Serialize(new
            {
                uploadedCount = discovery.Items.Count,
                candidateCount,
                skippedCount,
                errorCount
            }),
            CreatedAt = clock.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestionBatchSummary>.Ok(new IngestionBatchSummary
        {
            BatchId = batch.Id,
            FileCount = batch.FileCount,
            CandidateCount = batch.CandidateCount,
            SkippedCount = batch.SkippedCount,
            ErrorCount = batch.ErrorCount,
            Status = batch.Status,
            Items = summaries
        });
    }

    private async Task<ManifestScoredItem> ScoreManifestItemAsync(
        IngestionRequest request,
        Guid batchId,
        ManifestItem item,
        CancellationToken cancellationToken)
    {
        var folderHint = ExtractFolderHint(item.RelativePath);
        var classification = classifier.Classify(new ClassificationInput
        {
            FileName = item.Name,
            RelativePath = item.RelativePath,
            MimeType = item.MimeType ?? "application/octet-stream",
            SizeBytes = item.SizeBytes,
            FolderHint = folderHint,
            Hints = []
        });

        var manifestItemId = BuildManifestExternalId(item);

        var sourceObject = await dbContext.SourceObjects.FirstOrDefaultAsync(
            x => x.ConnectionId == request.ConnectionId && x.ExternalId == manifestItemId,
            cancellationToken);

        var manifestMetadata = JsonSerializer.Serialize(new
        {
            manifest = new
            {
                score = new
                {
                    candidateType = classification.CandidateType,
                    confidence = classification.Confidence,
                    reasonCodes = classification.ReasonCodes,
                    counterpartyHint = classification.CounterpartyHint
                },
                lastModifiedUtc = item.LastModifiedUtc,
                browserMimeType = item.MimeType,
                folderHint
            }
        });

        if (sourceObject is null)
        {
            sourceObject = new SourceObject
            {
                TenantId = request.TenantId,
                ConnectionId = request.ConnectionId,
                ExternalId = manifestItemId,
                Uri = $"manifest:{item.RelativePath}",
                Name = item.Name,
                MimeType = item.MimeType ?? "application/octet-stream",
                ObjectKind = SourceObjectKinds.File,
                RelativePath = item.RelativePath,
                ParentExternalId = folderHint,
                SizeBytes = item.SizeBytes,
                ProposedStatus = SourceObjectProposedStatuses.Proposed,
                MetadataJson = manifestMetadata,
                SourceModifiedAt = item.LastModifiedUtc,
                CreatedAt = clock.UtcNow
            };
            dbContext.SourceObjects.Add(sourceObject);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            sourceObject.ProposedStatus = SourceObjectProposedStatuses.Proposed;
            sourceObject.SizeBytes = item.SizeBytes;
            sourceObject.SourceModifiedAt = item.LastModifiedUtc;
            sourceObject.MetadataJson = manifestMetadata;
            sourceObject.UpdatedAt = clock.UtcNow;
        }

        dbContext.IngestionJobs.Add(new IngestionJob
        {
            TenantId = request.TenantId,
            BatchId = batchId,
            SourceObjectId = sourceObject.Id,
            DocumentAssetId = null,
            Status = IngestionJobStatus.Queued,
            Stage = IngestionStage.Discovered,
            AttemptCount = 1,
            CreatedAt = clock.UtcNow
        });

        return new ManifestScoredItem
        {
            ManifestItemId = manifestItemId,
            RelativePath = item.RelativePath,
            Name = item.Name,
            SizeBytes = item.SizeBytes,
            CandidateType = classification.CandidateType,
            Confidence = classification.Confidence,
            ReasonCodes = classification.ReasonCodes,
            RecommendedAction = ManifestBands.RecommendedAction(classification.Confidence),
            Band = ManifestBands.From(classification.Confidence),
            CounterpartyHint = classification.CounterpartyHint
        };
    }

    public static string BuildManifestExternalId(ManifestItem item) =>
        ManifestExternalIdPrefix + item.RelativePath + "|" + item.SizeBytes + "|" + item.LastModifiedUtc.ToUnixTimeSeconds();

    private static string? ExtractFolderHint(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return null;
        }
        var normalized = relativePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash <= 0 ? null : normalized[..lastSlash];
    }
}
