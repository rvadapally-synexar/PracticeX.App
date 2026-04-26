namespace PracticeX.Application.SourceDiscovery.Complexity;

/// <summary>
/// Per-file complexity assessment. Emitted by IComplexityProfiler after the
/// validity inspector and consumed by:
///   * IngestionOrchestrator — persisted on document_assets
///   * IPricingPolicy        — converted to hours / dollars
///   * ExtractionRouteDecider — informs Doc Intel routing
///   * UI                     — displayed as Tier badge + factors tooltip
/// </summary>
public sealed record ComplexityReport
{
    public required ComplexityTier Tier { get; init; }
    public required IReadOnlyList<string> Factors { get; init; }
    public required IReadOnlyList<string> Blockers { get; init; }
    public required string MetadataJson { get; init; }
    public decimal? EstimatedHours { get; init; }

    public static ComplexityReport Empty() => new()
    {
        Tier = ComplexityTier.Simple,
        Factors = Array.Empty<string>(),
        Blockers = Array.Empty<string>(),
        MetadataJson = "{}"
    };
}
