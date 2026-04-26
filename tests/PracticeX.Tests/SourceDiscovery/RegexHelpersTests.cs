using PracticeX.Discovery.FieldExtraction.Helpers;

namespace PracticeX.Tests.SourceDiscovery;

public class RegexHelpersTests
{
    [Fact]
    public void ParseDate_LongForm()
    {
        var d = RegexHelpers.ParseDate("April 11, 2026");
        Assert.NotNull(d);
        Assert.Equal(2026, d!.Value.Year);
        Assert.Equal(4, d.Value.Month);
        Assert.Equal(11, d.Value.Day);
    }

    [Fact]
    public void ParseDate_Slash()
    {
        var d = RegexHelpers.ParseDate("4/11/2026");
        Assert.NotNull(d);
        Assert.Equal(4, d!.Value.Month);
        Assert.Equal(11, d.Value.Day);
    }

    [Fact]
    public void ParseDate_Iso()
    {
        var d = RegexHelpers.ParseDate("2026-04-11");
        Assert.NotNull(d);
        Assert.Equal(2026, d!.Value.Year);
        Assert.Equal(4, d.Value.Month);
        Assert.Equal(11, d.Value.Day);
    }

    [Fact]
    public void ParseDate_Bogus_ReturnsNull()
    {
        Assert.Null(RegexHelpers.ParseDate("not a date"));
        Assert.Null(RegexHelpers.ParseDate(""));
        Assert.Null(RegexHelpers.ParseDate(null));
    }

    [Fact]
    public void ParseMoney_FormattedDollars()
    {
        Assert.Equal(150000m, RegexHelpers.ParseMoney("$150,000"));
        Assert.Equal(150000.50m, RegexHelpers.ParseMoney("$150,000.50"));
    }

    [Fact]
    public void ParseMoney_BareNumberReturnsNull()
    {
        Assert.Null(RegexHelpers.ParseMoney("150000"));
        Assert.Null(RegexHelpers.ParseMoney(""));
    }

    [Fact]
    public void ParsePercent_Standard()
    {
        Assert.Equal(1.00m, RegexHelpers.ParsePercent("1.00%"));
        Assert.Equal(3m, RegexHelpers.ParsePercent("3%"));
    }

    [Fact]
    public void LooksLikePlaceholder_Cases()
    {
        Assert.True(RegexHelpers.LooksLikePlaceholder("______"));
        Assert.True(RegexHelpers.LooksLikePlaceholder("[Your State]"));
        Assert.True(RegexHelpers.LooksLikePlaceholder("[date]"));
        Assert.False(RegexHelpers.LooksLikePlaceholder("Delaware"));
        Assert.False(RegexHelpers.LooksLikePlaceholder(""));
    }
}
