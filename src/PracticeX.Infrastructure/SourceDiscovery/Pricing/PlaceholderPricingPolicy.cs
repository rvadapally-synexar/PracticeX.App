using PracticeX.Application.SourceDiscovery.Complexity;

namespace PracticeX.Infrastructure.SourceDiscovery.Pricing;

/// <summary>
/// Default pricing policy. Setup-hours rates are PLACEHOLDER (not from sales).
/// Azure Doc Intel rate is the real public rate ($1.50 per 1000 prebuilt-layout
/// pages). Disclaimer is attached to every estimate so UI surfaces are clearly
/// labelled "estimate, not quote".
///
/// Replace with ContractedRateCardPricingPolicy via DI when sales delivers
/// negotiated numbers.
/// </summary>
public sealed class PlaceholderPricingPolicy : IPricingPolicy
{
    // Setup hours per file by tier. Placeholder rates — not approved by sales.
    private const decimal HoursPerSimple   = 0.05m;
    private const decimal HoursPerModerate = 0.50m;
    private const decimal HoursPerLarge    = 2.00m;
    private const decimal HoursPerExtra    = 4.00m;

    // Real Azure Document Intelligence rate (prebuilt-layout, public pricing).
    // ~$1.50 per 1000 pages → $0.0015/page.
    private const decimal DocIntelDollarsPerPage = 0.0015m;

    private const decimal LlmDollarsPerToken = 0m; // Wire when LLM provider locks.

    private const string Disclaimer =
        "Estimate only. Placeholder rates for sizing — final pricing per signed engagement.";

    public decimal? EstimateHours(ComplexityReport report) => report.Tier switch
    {
        ComplexityTier.Simple => HoursPerSimple,
        ComplexityTier.Moderate => HoursPerModerate,
        ComplexityTier.Large => HoursPerLarge,
        ComplexityTier.Extra => HoursPerExtra,
        _ => null
    };

    public PricingEstimate Estimate(BatchComplexityProfile profile, int totalEstimatedOcrPages)
    {
        var setupHours =
            profile.SimpleCount   * HoursPerSimple +
            profile.ModerateCount * HoursPerModerate +
            profile.LargeCount    * HoursPerLarge +
            profile.ExtraCount    * HoursPerExtra;

        var docIntelCost = totalEstimatedOcrPages * DocIntelDollarsPerPage;
        var llmCost = 0m;
        var total = docIntelCost + llmCost; // setup hours billed separately, not folded into the per-batch dollar total

        return new PricingEstimate
        {
            SetupHours = setupHours,
            AzureDocIntelPagesEstimated = totalEstimatedOcrPages,
            AzureDocIntelCostUsd = docIntelCost,
            LlmTokensEstimated = 0m,
            LlmCostUsd = llmCost,
            TotalEstimatedCostUsd = total,
            DisclaimerText = Disclaimer
        };
    }
}
