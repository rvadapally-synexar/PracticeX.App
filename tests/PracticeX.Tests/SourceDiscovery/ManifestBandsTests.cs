using PracticeX.Application.SourceDiscovery.Ingestion;

namespace PracticeX.Tests.SourceDiscovery;

public class ManifestBandsTests
{
    [Theory]
    [InlineData(0.95, ManifestBands.Strong, ManifestRecommendedActions.Select)]
    [InlineData(0.80, ManifestBands.Strong, ManifestRecommendedActions.Select)]
    [InlineData(0.79, ManifestBands.Likely, ManifestRecommendedActions.Select)]
    [InlineData(0.60, ManifestBands.Likely, ManifestRecommendedActions.Select)]
    [InlineData(0.59, ManifestBands.Possible, ManifestRecommendedActions.Optional)]
    [InlineData(0.35, ManifestBands.Possible, ManifestRecommendedActions.Optional)]
    [InlineData(0.34, ManifestBands.Skipped, ManifestRecommendedActions.Skip)]
    [InlineData(0.00, ManifestBands.Skipped, ManifestRecommendedActions.Skip)]
    public void BandsAndActions_RespectThresholds(double confidence, string expectedBand, string expectedAction)
    {
        var c = (decimal)confidence;
        Assert.Equal(expectedBand, ManifestBands.From(c));
        Assert.Equal(expectedAction, ManifestBands.RecommendedAction(c));
    }
}
