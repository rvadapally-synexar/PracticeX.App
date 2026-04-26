namespace PracticeX.Application.SourceDiscovery.Complexity;

/// <summary>
/// Aggregate complexity across a batch (all files in a manifest scan or a
/// bundle upload). Drives the prepare-upload preview in the UI and the
/// pricing estimate.
/// </summary>
public sealed record BatchComplexityProfile
{
    public required int SimpleCount { get; init; }
    public required int ModerateCount { get; init; }
    public required int LargeCount { get; init; }
    public required int ExtraCount { get; init; }
    public required IReadOnlyList<BlockerSummary> Blockers { get; init; }
    public decimal? TotalEstimatedHours { get; init; }
    public int? EstimatedDocumentIntelligencePages { get; init; }
    public decimal? EstimatedDocumentIntelligenceCostUsd { get; init; }

    public int TotalCount => SimpleCount + ModerateCount + LargeCount + ExtraCount;

    public static BatchComplexityProfile FromReports(IEnumerable<ComplexityReport> reports)
    {
        var list = reports.ToList();
        var blockerCounts = list
            .SelectMany(r => r.Blockers)
            .GroupBy(b => b)
            .Select(g => new BlockerSummary(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ToList();

        return new BatchComplexityProfile
        {
            SimpleCount = list.Count(r => r.Tier == ComplexityTier.Simple),
            ModerateCount = list.Count(r => r.Tier == ComplexityTier.Moderate),
            LargeCount = list.Count(r => r.Tier == ComplexityTier.Large),
            ExtraCount = list.Count(r => r.Tier == ComplexityTier.Extra),
            Blockers = blockerCounts,
            TotalEstimatedHours = list.Sum(r => r.EstimatedHours ?? 0m) is var sum && sum > 0m ? sum : null
        };
    }
}

public sealed record BlockerSummary(string Code, int Count);
