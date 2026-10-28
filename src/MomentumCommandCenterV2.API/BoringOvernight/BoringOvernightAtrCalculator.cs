using MomentumCommandCenterV2.Core.Models;

namespace MomentumCommandCenterV2.API.BoringOvernight;

/// <summary>
/// Calculates ATR from supplied OHLC bars. The period, timeframe and anchor are explicit;
/// this does not claim parity with a Thinkorswim study until compared against its output.
/// </summary>
public static class BoringOvernightAtrCalculator
{
    public static AtrResult Calculate(IReadOnlyList<MarketBar> sourceBars, int period)
    {
        ArgumentNullException.ThrowIfNull(sourceBars);
        if (period < 1) throw new ArgumentOutOfRangeException(nameof(period), "ATR period must be positive.");
        if (sourceBars.Count < period + 1)
            throw new ArgumentException($"At least {period + 1} bars are required to calculate {period}-period ATR.", nameof(sourceBars));

        var bars = sourceBars.OrderBy(x => x.Timestamp).ToArray();
        var symbol = bars[0].Symbol;
        var timeframe = bars[0].Timeframe;
        if (bars.Any(x => !string.Equals(x.Symbol, symbol, StringComparison.OrdinalIgnoreCase) || x.Timeframe != timeframe))
            throw new ArgumentException("All bars must use the same symbol and timeframe.", nameof(sourceBars));
        if (bars.GroupBy(x => x.Timestamp).Any(g => g.Count() > 1))
            throw new ArgumentException("Duplicate bar timestamps are not allowed.", nameof(sourceBars));
        if (bars.Any(x => x.High < x.Low || x.Open <= 0 || x.High <= 0 || x.Low <= 0 || x.Close <= 0 || x.Volume < 0))
            throw new ArgumentException("Bars contain invalid OHLCV values.", nameof(sourceBars));

        var trueRanges = new decimal[bars.Length - 1];
        for (var i = 1; i < bars.Length; i++)
        {
            var bar = bars[i];
            var previousClose = bars[i - 1].Close;
            trueRanges[i - 1] = Math.Max(bar.High - bar.Low,
                Math.Max(Math.Abs(bar.High - previousClose), Math.Abs(bar.Low - previousClose)));
        }

        // Wilder ATR: seed with the simple mean of the first period TR values,
        // then apply the recursive Wilder smoothing formula.
        var atr = trueRanges.Take(period).Average();
        for (var i = period; i < trueRanges.Length; i++)
            atr = ((atr * (period - 1)) + trueRanges[i]) / period;

        return new AtrResult(
            symbol.ToUpperInvariant(),
            timeframe,
            period,
            bars[^1].Timestamp,
            bars[^1].Close,
            trueRanges[^1],
            atr,
            atr / bars[^1].Close * 100m,
            "Wilder ATR; source OHLCV bars; compare against the exact Thinkorswim study before using as a qualification gate.");
    }

    /// <summary>Signed distance from an explicitly supplied reference price, in ATR units.</summary>
    public static decimal CalculateSignedExtensionAtr(decimal currentPrice, decimal referencePrice, decimal atr)
    {
        if (currentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(currentPrice));
        if (referencePrice <= 0) throw new ArgumentOutOfRangeException(nameof(referencePrice));
        if (atr <= 0) throw new ArgumentOutOfRangeException(nameof(atr));
        return (currentPrice - referencePrice) / atr;
    }
}

public sealed record AtrResult(
    string Symbol,
    BarTimeframe Timeframe,
    int Period,
    DateTimeOffset AsOf,
    decimal Close,
    decimal LatestTrueRange,
    decimal Atr,
    decimal AtrPercent,
    string Provenance);
