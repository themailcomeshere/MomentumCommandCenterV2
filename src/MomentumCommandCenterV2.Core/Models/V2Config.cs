namespace MomentumCommandCenterV2.Core.Models
{
    public sealed record V2Config(decimal MinRvol = 1.5m, decimal MinRsi = 50m, decimal MaxRsiForFreshEntry = 78m, decimal AtrExtensionWarning = 1.50m, decimal AtrExtensionHardExit = 2.25m, int EndOfDayExitHourEt = 15, int EndOfDayExitMinuteEt = 55);
}
