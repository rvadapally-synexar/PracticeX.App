namespace PracticeX.Discovery.Contracts;

public sealed record IngestionItemDto(
    Guid SourceObjectId,
    Guid? DocumentAssetId,
    Guid? DocumentCandidateId,
    string Name,
    string CandidateType,
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    string Status,
    string? RelativePath
);

public sealed record IngestionBatchSummaryDto(
    Guid BatchId,
    int FileCount,
    int CandidateCount,
    int SkippedCount,
    int ErrorCount,
    string Status,
    IReadOnlyList<IngestionItemDto> Items
);

public sealed record IngestionBatchDto(
    Guid Id,
    string SourceType,
    Guid? SourceConnectionId,
    string Status,
    int FileCount,
    int CandidateCount,
    int SkippedCount,
    int ErrorCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Notes
);

public sealed record DocumentCandidateDto(
    Guid Id,
    Guid? SourceObjectId,
    Guid DocumentAssetId,
    string CandidateType,
    decimal Confidence,
    string Status,
    IReadOnlyList<string> ReasonCodes,
    string ClassifierVersion,
    string? OriginFilename,
    string? RelativePath,
    string? CounterpartyHint,
    DateTimeOffset CreatedAt
);

public sealed record DeleteAllBatchesResult(int DeletedCount);
