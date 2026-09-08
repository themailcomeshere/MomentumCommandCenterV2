using MomentumCommandCenterV2.Core.Models.Enums;

namespace MomentumCommandCenterV2.Core.Models
{
    public sealed record CommandCenterSnapshot(DateTimeOffset Timestamp, string Symbol, BarSnapshot OneMinute, BarSnapshot FiveMinute, MarketPhase Phase);
}
