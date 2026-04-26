namespace PracticeX.Discovery.Contracts;

public sealed record ManifestItemDto(
    string RelativePath,
    string Name,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc,
    string? MimeType
);

public sealed record ManifestScanRequest(
    IReadOnlyList<ManifestItemDto> Items,
    string? Notes
);

public sealed record ManifestScoredItemDto(
    string ManifestItemId,
    string RelativePath,
    string Name,
    long SizeBytes,
    string CandidateType,
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    string RecommendedAction,
    string Band,
    string? CounterpartyHint,
    bool HasSignature = false,
    int SignatureCount = 0,
    IReadOnlyList<string>? SignatureProviders = null
);

public sealed record ManifestScanResponse(
    Guid BatchId,
    string Phase,
    int TotalItems,
    int StrongCount,
    int LikelyCount,
    int PossibleCount,
    int SkippedCount,
    IReadOnlyList<ManifestScoredItemDto> Items
);
