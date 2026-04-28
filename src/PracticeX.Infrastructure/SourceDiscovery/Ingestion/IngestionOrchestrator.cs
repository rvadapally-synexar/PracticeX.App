using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Complexity;
using PracticeX.Application.SourceDiscovery.Connectors;
using PracticeX.Application.SourceDiscovery.DocumentAi;
using PracticeX.Application.SourceDiscovery.Ingestion;
using PracticeX.Application.SourceDiscovery.Storage;
using PracticeX.Discovery.Classification;
using PracticeX.Discovery.Contracts;
using PracticeX.Discovery.DocumentAi;
using PracticeX.Discovery.Signatures;
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
    ISignatureDetector signatureDetector,
    IDocumentIntelligenceProvider documentIntelligence,
    IOptions<DocumentIntelligenceOptions> documentIntelligenceOptions,
    IClock clock,
    ILogger<IngestionOrchestrator> logger) : IIngestionOrchestrator
{
    public const string ManifestExternalIdPrefix = "manifest:";
    private const decimal SignatureConfidenceBoost = 0.30m;
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
            Items = summaries,
            Complexity = AggregateComplexity(summaries)
        });
    }

    /// <summary>
    /// Builds the per-batch complexity aggregate from the per-item summaries.
    /// Returns null if no item carries a tier (e.g. all duplicates / mail
    /// containers — nothing to aggregate over).
    /// </summary>
    private static BatchComplexityProfile? AggregateComplexity(IReadOnlyList<IngestionItemSummary> items)
    {
        var tiered = items.Where(i => !string.IsNullOrEmpty(i.ComplexityTier)).ToList();
        if (tiered.Count == 0) return null;

        var blockerCounts = tiered
            .SelectMany(i => i.ComplexityBlockers ?? Array.Empty<string>())
            .GroupBy(b => b)
            .Select(g => new BlockerSummary(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ToList();

        var totalHours = tiered.Sum(i => i.EstimatedComplexityHours ?? 0m);

        return new BatchComplexityProfile
        {
            SimpleCount   = tiered.Count(i => i.ComplexityTier == "S"),
            ModerateCount = tiered.Count(i => i.ComplexityTier == "M"),
            LargeCount    = tiered.Count(i => i.ComplexityTier == "L"),
            ExtraCount    = tiered.Count(i => i.ComplexityTier == "X"),
            Blockers = blockerCounts,
            TotalEstimatedHours = totalHours > 0m ? totalHours : null
        };
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
        SignatureReport? signature = null;
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

            // Detect signatures only on supported containers; skip otherwise to avoid wasted work.
            if (signatureDetector.CanInspect(item.MimeType, item.Name))
            {
                signature = signatureDetector.Inspect(item.InlineContent, item.MimeType, item.Name);
            }

            var assetMetadata = MergeAssetMetadata(complexity.MetadataJson, signature);

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
                MetadataJson = assetMetadata,
                EstimatedComplexityHours = estimatedHours,
                CreatedAt = clock.UtcNow
            };
            dbContext.DocumentAssets.Add(asset);
            await dbContext.SaveChangesAsync(cancellationToken);
            isDuplicate = false;

            // Route through Azure Document Intelligence when local extraction
            // can't surface the text (scanned PDFs / image-only). Gated by
            // global enable flag AND per-tenant allowlist — neither alone is
            // enough. A failure here is logged + audited but never aborts the
            // batch; the asset stays with OcrStatus='failed' for reprocessing.
            await TryExtractLayoutAsync(request, asset, item, cancellationToken);
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

        // Combine reason codes from classifier + validity + signature, plus dedupe marker.
        var reasonCodes = classification.ReasonCodes.ToList();
        if (validity is not null)
        {
            foreach (var rc in validity.ReasonCodes) reasonCodes.Add(rc);
        }
        if (signature is not null && signature.HasSignature)
        {
            foreach (var rc in signature.ReasonCodes)
            {
                if (!reasonCodes.Contains(rc)) reasonCodes.Add(rc);
            }
        }
        if (isDuplicate)
        {
            reasonCodes.Add(IngestionReasonCodes.DuplicateContent);
        }

        // Boost confidence when a signature is detected — a Docusigned PDF in a
        // generic folder still lands in the Strong band.
        var boostedConfidence = signature is not null && signature.HasSignature
            ? Math.Min(0.99m, classification.Confidence + SignatureConfidenceBoost)
            : classification.Confidence;
        boostedConfidence = decimal.Round(boostedConfidence, 4);

        var promotedStatus = signature is not null && signature.HasSignature
            && classification.Status == DocumentCandidateStatus.Candidate
            && boostedConfidence >= 0.55m
                ? DocumentCandidateStatus.PendingReview
                : classification.Status;

        var candidateStatus = isDuplicate
            ? DocumentCandidateStatus.Skipped
            : promotedStatus;

        var candidate = new DocumentCandidate
        {
            TenantId = request.TenantId,
            DocumentAssetId = asset.Id,
            CandidateType = classification.CandidateType,
            Confidence = boostedConfidence,
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
                Priority = boostedConfidence < 0.7m ? 1 : 2,
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
                confidence = boostedConfidence,
                reasonCodes,
                duplicate = isDuplicate,
                hasSignature = signature is not null && signature.HasSignature,
                signatureProviders = signature?.Providers ?? Array.Empty<string>()
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
            Confidence = boostedConfidence,
            ReasonCodes = reasonCodes,
            Status = candidateStatus,
            RelativePath = item.RelativePath,
            ComplexityTier = asset.ComplexityTier,
            ComplexityFactors = SafeDeserializeStringList(asset.ComplexityFactorsJson),
            ComplexityBlockers = SafeDeserializeStringList(asset.ComplexityBlockersJson),
            EstimatedComplexityHours = asset.EstimatedComplexityHours
        };
    }

    /// <summary>
    /// Merges complexity-profiler metadata with signature info into the
    /// document_assets.metadata_json column. Both sides are optional;
    /// produces a single JSON object with stable keys "complexity" and
    /// "signature" so future readers don't need to know the producers.
    /// </summary>
    private static string? MergeAssetMetadata(string? complexityJson, SignatureReport? signature)
    {
        var hasComplexity = !string.IsNullOrWhiteSpace(complexityJson);
        var hasSignature = signature is not null && signature.HasSignature;
        if (!hasComplexity && !hasSignature)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            if (hasComplexity)
            {
                writer.WritePropertyName("complexity");
                using var doc = JsonDocument.Parse(complexityJson!);
                doc.RootElement.WriteTo(writer);
            }

            if (hasSignature)
            {
                writer.WritePropertyName("signature");
                writer.WriteStartObject();
                writer.WriteBoolean("has_signature", true);
                writer.WriteNumber("signature_count", signature!.SignatureCount);
                writer.WritePropertyName("providers");
                writer.WriteStartArray();
                foreach (var p in signature.Providers) writer.WriteStringValue(p);
                writer.WriteEndArray();
                writer.WritePropertyName("details");
                writer.WriteStartArray();
                foreach (var d in signature.Details)
                {
                    writer.WriteStartObject();
                    writer.WriteString("provider", d.Provider);
                    if (!string.IsNullOrWhiteSpace(d.SignerName)) writer.WriteString("signer_name", d.SignerName);
                    if (d.PageNumber.HasValue) writer.WriteNumber("page_number", d.PageNumber.Value);
                    if (!string.IsNullOrWhiteSpace(d.EnvelopeId)) writer.WriteString("envelope_id", d.EnvelopeId);
                    if (d.SignedAtUtc.HasValue) writer.WriteString("signed_at_utc", d.SignedAtUtc.Value);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IReadOnlyList<string>? SafeDeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch { return null; }
    }

    /// <summary>
    /// Routes a freshly-stored asset through Azure Document Intelligence when
    /// the validity inspector has determined that local text extraction is
    /// insufficient (no text layer, scanned PDF). Two-gate: provider must
    /// report IsConfigured=true AND the request's tenant must be in the
    /// AllowedTenantIds allowlist. Failures are non-fatal — the asset stays
    /// with OcrStatus='failed' so a later reprocess can retry.
    /// </summary>
    private async Task TryExtractLayoutAsync(
        IngestionRequest request,
        DocumentAsset asset,
        DiscoveredItem item,
        CancellationToken cancellationToken)
    {
        if (!documentIntelligence.IsConfigured)
        {
            return;
        }

        if (asset.ExtractionRoute != ExtractionRoutes.OcrFirstPages &&
            asset.ExtractionRoute != ExtractionRoutes.FullOcr)
        {
            return;
        }

        var options = documentIntelligenceOptions.Value;
        if (options.AllowedTenantIds is null || !options.AllowedTenantIds.Contains(request.TenantId))
        {
            logger.LogInformation(
                "DocIntel skipped for asset {AssetId}: tenant {Tenant} not in allowlist",
                asset.Id, request.TenantId);
            return;
        }

        if (item.InlineContent is null || item.InlineContent.Length == 0)
        {
            return;
        }

        try
        {
            asset.OcrStatus = "running";
            await dbContext.SaveChangesAsync(cancellationToken);

            var extraction = await documentIntelligence.ExtractLayoutAsync(
                new DocumentExtractionRequest(
                    Content: item.InlineContent,
                    FileName: item.Name,
                    MimeType: item.MimeType,
                    MaxPages: options.MaxPagesPerDocument),
                cancellationToken);

            asset.LayoutJson = JsonSerializer.Serialize(new
            {
                provider = extraction.ProviderName,
                model = extraction.ProviderModel,
                fullText = extraction.FullText,
                pages = extraction.Pages,
                tables = extraction.Tables,
                keyValuePairs = extraction.KeyValuePairs,
                latencyMs = extraction.LatencyMs
            });
            asset.LayoutProvider = extraction.ProviderName;
            asset.LayoutModel = extraction.ProviderModel;
            asset.LayoutExtractedAt = clock.UtcNow;
            asset.LayoutPageCount = extraction.Pages.Count;
            asset.OcrStatus = "completed";
            asset.UpdatedAt = clock.UtcNow;

            dbContext.AuditEvents.Add(new AuditEvent
            {
                TenantId = request.TenantId,
                ActorType = "system",
                ActorId = request.InitiatedByUserId,
                EventType = "ingestion.layout.extracted",
                ResourceType = "document_asset",
                ResourceId = asset.Id,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    provider = extraction.ProviderName,
                    model = extraction.ProviderModel,
                    pageCount = extraction.Pages.Count,
                    tableCount = extraction.Tables.Count,
                    kvpCount = extraction.KeyValuePairs.Count,
                    latencyMs = extraction.LatencyMs
                }),
                CreatedAt = clock.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "DocIntel layout extracted for asset {AssetId}: {Pages} pages, {Tables} tables in {Latency}ms",
                asset.Id, extraction.Pages.Count, extraction.Tables.Count, extraction.LatencyMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "DocIntel extraction failed for asset {AssetId} (tenant {Tenant}); marking ocr_status=failed",
                asset.Id, request.TenantId);

            asset.OcrStatus = "failed";
            asset.UpdatedAt = clock.UtcNow;

            dbContext.AuditEvents.Add(new AuditEvent
            {
                TenantId = request.TenantId,
                ActorType = "system",
                ActorId = request.InitiatedByUserId,
                EventType = "ingestion.layout.failed",
                ResourceType = "document_asset",
                ResourceId = asset.Id,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    error = ex.GetType().Name,
                    message = ex.Message
                }),
                CreatedAt = clock.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
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
            Items = summaries,
            Complexity = AggregateComplexity(summaries)
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
