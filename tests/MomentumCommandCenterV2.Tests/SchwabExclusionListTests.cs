using MomentumCommandCenterV2.Infrastructure.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

public sealed class SchwabExclusionListTests
{
    private static readonly string[] ExpectedSymbols =
    {
        "AMHpG", "ANL", "ATHpA", "AXIAY", "BACpQ", "BHRpB", "BRNX", "BUSEP",
        "BZUN", "CAPMF", "DAO", "DBGI", "DKI", "FITBpI", "FMX", "FNMAS",
        "FOCL", "FSUGY", "GGAL", "HELP", "HENKY", "KCDMY", "KEYpI", "KOF",
        "KREFpA", "METCZ", "MFApC", "MTBpJ", "MTBpK", "NAMI", "NEEpT",
        "NEEpW", "OAKpA", "PCLA", "PPERY", "PPRUY", "PSApN", "PSNYW", "RFpF",
        "SHTDY", "SMMYY", "SPCB", "SSM", "STFS", "STTpG", "SWVL", "TJGC",
        "TRTXpC", "TRYIY", "TYOYY", "VCRE", "WBKCY", "WYHG", "YDES", "YJ"
    };

    [Fact]
    public void Constructor_LoadsCompleteListInSourceOrderAndPreservesSpelling()
    {
        var exclusions = new SchwabExclusionList();

        Assert.Equal(ExpectedSymbols.Length, exclusions.Symbols.Count);
        Assert.Equal(ExpectedSymbols, exclusions.Symbols);
        Assert.All(ExpectedSymbols, symbol => Assert.True(exclusions.IsExcluded(symbol), symbol));
    }

    [Theory]
    [InlineData("AMHpG")]
    [InlineData("amhpg")]
    [InlineData(" AMHpG ")]
    [InlineData("KEYpI")]
    [InlineData("TRTXpC")]
    public void IsExcluded_MatchesTrimmedSymbolsWithoutChangingSourceSpelling(string symbol)
    {
        var exclusions = new SchwabExclusionList();

        Assert.True(exclusions.IsExcluded(symbol));
    }

    [Theory]
    [InlineData("AAPL")]
    [InlineData("AN")]
    [InlineData("SHTD")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsExcluded_DoesNotExcludeNearMatchesOrBlankSymbols(string symbol)
    {
        var exclusions = new SchwabExclusionList();

        Assert.False(exclusions.IsExcluded(symbol));
    }

    [Fact]
    public void IsExcluded_ReturnsFalseForNull()
    {
        var exclusions = new SchwabExclusionList();

        Assert.False(exclusions.IsExcluded(null));
    }
}
