using PracticeX.Application.Common;
using PracticeX.Application.SourceDiscovery.Connectors;

namespace PracticeX.Application.SourceDiscovery.Ingestion;

/// <summary>
/// Owns the canonical ingestion pipeline:
///   discovered_items -> source_objects -> ingestion_batch + jobs
///   -> document_assets -> document_candidates -> review_tasks -> audit
///
/// Connectors only produce DiscoveredItems. They never write canonical contract
/// records. Approval into the contract repository happens in the review queue.
/// </summary>
public interface IIngestionOrchestrator
{
    Task<Result<IngestionBatchSummary>> IngestAsync(
        IngestionRequest request,
        DiscoveryResult discovery,
        CancellationToken cancellationToken);
}

public sealed class IngestionRequest
{
    public required Guid TenantId { get; init; }
    public required Guid InitiatedByUserId { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string SourceType { get; init; }
    public string? Notes { get; init; }
}

public sealed class IngestionBatchSummary
{
    public required Guid BatchId { get; init; }
    public required int FileCount { get; init; }
    public required int CandidateCount { get; init; }
    public required int SkippedCount { get; init; }
    public required int ErrorCount { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<IngestionItemSummary> Items { get; init; }
}

public sealed class IngestionItemSummary
{
    public required Guid SourceObjectId { get; init; }
    public Guid? DocumentAssetId { get; init; }
    public Guid? DocumentCandidateId { get; init; }
    public required string Name { get; init; }
    public required string CandidateType { get; init; }
    public decimal Confidence { get; init; }
    public required IReadOnlyList<string> ReasonCodes { get; init; }
    public required string Status { get; init; }
    public string? RelativePath { get; init; }
}
