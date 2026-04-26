namespace PracticeX.Application.SourceDiscovery.Complexity;

/// <summary>
/// Converts complexity reports into hours / dollars. Default implementation
/// (PlaceholderPricingPolicy) ships with labelled-as-placeholder rates so the
/// UI shows real-looking numbers in demos. When sales delivers real numbers,
/// swap to ContractedRateCardPricingPolicy via DI.
///
/// Pricing is advisory — the UI surfaces it as estimate, not quote. Real
/// invoicing happens out-of-band per signed engagement.
/// </summary>
public interface IPricingPolicy
{
    decimal? EstimateHours(ComplexityReport report);
    PricingEstimate Estimate(BatchComplexityProfile profile, int totalEstimatedOcrPages);
}

/// <summary>
/// All-up estimate for a batch: human time + Azure Document Intelligence cost
/// + (eventually) LLM cost. UI shows the total + the disclaimer.
/// </summary>
public sealed record PricingEstimate
{
    public required decimal SetupHours { get; init; }
    public required int AzureDocIntelPagesEstimated { get; init; }
    public required decimal AzureDocIntelCostUsd { get; init; }
    public required decimal LlmTokensEstimated { get; init; }
    public required decimal LlmCostUsd { get; init; }
    public required decimal TotalEstimatedCostUsd { get; init; }
    public required string DisclaimerText { get; init; }
}
