# Market-Data and Historical-Replay Contract

**Status:** First engineering slice implemented; provider integration and indicator parity remain pending.

## Goals

- Make historical analysis repeatable from source-controlled data.
- Prevent look-ahead: a replay frame may only see bars timestamped at or before that frame.
- Keep source timestamps, timeframe, symbol, and raw OHLCV explicit.
- Separate raw market data, indicator calculations, signal evaluation, opportunity ranking, and UI presentation.
- Treat unavailable or stale inputs as unknown rather than synthesizing them.

## Current implementation

- `MarketBar`: provider-neutral raw OHLCV record with symbol, timeframe, and timestamp.
- `CsvMarketBarReader`: parses local CSV using invariant numeric formats and timezone-aware timestamps.
- `MarketReplayEngine`: emits a frame for each 1-minute bar and attaches the most recent 5-minute bar with timestamp <= the frame timestamp.
- `MarketReplayFrame.FiveMinuteAge`: exposes the age of 5-minute context so consumers can enforce freshness.
- Unit tests cover chronological replay, no future 5-minute data, duplicate bars, and invalid OHLC.
- CLI replay command and a synthetic sample CSV support local smoke testing.

## CSV format

Required columns:

```csv
Symbol,Timeframe,Timestamp,Open,High,Low,Close,Volume
MNSB,1m,2026-10-16T16:01:00Z,10.00,10.15,9.95,10.10,12500
MNSB,5m,2026-10-16T16:00:00Z,9.80,10.20,9.75,10.10,56000
```

- Timeframe accepts `1m` / `5m` (also `1` / `5`).
- Timestamp must have an explicit timezone offset. Preserve the provider timestamp; never silently assume local machine time.
- In this replay contract, `Timestamp` means the time the completed bar becomes available to the decision process (normally its close/availability time), not the bar-open time. If a provider labels bars by open time, its adapter must normalize to availability time before replay. Never pass open-time timestamps directly into replay, because a 5M bar could otherwise appear available before it has completed.
- Numeric values use invariant culture.
- Duplicate `symbol + timeframe + timestamp` keys and invalid OHLCV rows are rejected.
- The adapter currently accepts completed OHLCV bars only. Partial/in-progress bars need a separate explicit contract.

## Explicit non-goals / limitations

The reader does **not** compute VWAP, EMA, SMA, RSI, MACD, ATR, RVOL, scanner events, relative-volume-by-time-of-day, resistance distance, news/catalyst status, or fills. These require separate definitions and validation against the source ThinkOrSwim studies/provider data. Never use one bar to invent a recent-range baseline or a catalyst-clear flag.

The replay engine does not simulate orders, fills, spread, slippage, positions, exits, or P&L. It is a time-aligned data foundation, not a backtest result.

## Next steps

1. Add representative, sanitized 1M/5M CSV fixtures and provenance metadata.
2. Define session calendar, extended-hours inclusion, regular-session VWAP reset, and daylight-saving handling.
3. Port indicator formulas only after extracting exact definitions from versioned ThinkScript studies; add golden-file parity tests for matching symbol/time/bar.
4. Add explicit indicator snapshots with source timestamp and readiness/freshness metadata.
5. Connect replay frames to the existing V2 signal engine without changing its decision rules.
6. Persist signal transitions and opportunity assessments during replay.
7. Add position lifecycle and fill simulation only after data/indicator parity is demonstrated.
8. Add a real provider adapter only after selecting the source and agreeing its timestamp/session semantics.

## Safety boundary

This application is a research/decision-support prototype, not an auto-trader. No automated order placement or Schwab order submission is introduced by this milestone.
