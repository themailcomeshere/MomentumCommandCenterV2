using MomentumCommandCenterV2.Core.Models.Enums;

Console.WriteLine("Momentum Command Center V2 Prototype");
Console.WriteLine("5M permission -> 1M execution -> position lifecycle -> exit protection");

foreach (var state in Enum.GetValues<SignalState>())
    Console.WriteLine($"{state}"
);
