namespace MomentumCommandCenterV2.Application.Models;

public sealed record UnmatchedOrder(
    string Symbol,
    string Action,
    decimal FillPrice,
    DateTimeOffset Timestamp,
    int Quantity,
    int SourceRowNumber);