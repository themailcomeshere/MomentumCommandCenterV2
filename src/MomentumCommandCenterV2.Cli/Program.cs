using MomentumCommandCenterV2.Core;
using MomentumCommandCenterV2.Core.Models.Enums;
using MomentumCommandCenterV2.Infrastructure.MarketData;

/// <summary>
/// CLI ENTRY POINT
/// PURPOSE: Lists existing signal states or replays a local OHLCV CSV for inspection.
/// WHEN CALLED: Invoked by dotnet run; use replay followed by a CSV path to print point-in-time frame availability.
/// </summary>
if (args.Length >= 2 && string.Equals(args[0], "replay", StringComparison.OrdinalIgnoreCase))
{
    var path = Path.GetFullPath(args[1]);
    using var reader = File.OpenText(path);
    var bars = new CsvMarketBarReader().Read(reader);
    var frames = new MarketReplayEngine().Replay(bars);

    Console.WriteLine($"Loaded {bars.Count} bars from {path}");
    Console.WriteLine($"Replay frames: {frames.Count}");
    foreach (var frame in frames)
    {
        var fiveMinuteClose = frame.FiveMinuteBar is null ? "DATA?" : frame.FiveMinuteBar.Close.ToString();
        var fiveMinuteAge = frame.FiveMinuteAge?.ToString() ?? "DATA?";
        Console.WriteLine($"{frame.Timestamp:O} {frame.Symbol} 1M close={frame.OneMinuteBar.Close} 5M close={fiveMinuteClose} 5M age={fiveMinuteAge}");
    }
    return;
}

Console.WriteLine("Momentum Command Center V2 Prototype");
Console.WriteLine("5M permission -> 1M execution -> position lifecycle -> exit protection");
Console.WriteLine("Usage: dotnet run --project src/MomentumCommandCenterV2.Cli -- replay <path-to-csv>");
foreach (var state in Enum.GetValues<SignalState>())
{
    Console.WriteLine(state);
}
