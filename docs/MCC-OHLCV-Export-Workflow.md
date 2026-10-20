# MCC-owned OHLCV export workflow

**Status:** Implemented as a draft transport and parser; not production-ready. Thinkorswim compilation, report-name preservation, timestamp semantics, and chart parity remain open.

## Ownership and provenance

The project-owned export artifact is `docs/Exports/MCC_OHLCV_ExportStrategy.txt`, with the unique strategy name `MCC_OHLCV_V1`. The original `data/StrategyReports_XRPN_101926.csv` is retained unchanged as a legacy input fixture. A normalized output must be written separately (for example, `data/normalized/XRPN_1m_20261019.csv`) and must retain a manifest containing:

- source report path and SHA-256, symbol, aggregation, session setting, export date, and timezone convention;
- parser version and validation status;
- whether Thinkorswim compilation and chart parity were completed.

Never overwrite an original report and never label an unverified result production-ready.

## Export schema

The order-name payload has this exact shape. `timestamp` is the positive Unix epoch millisecond value returned by Thinkorswim `GetTime()[1]`; the normalizer converts it to an explicit-offset ISO-8601 timestamp. ISO-8601 timestamps with an explicit offset are also accepted for hand-authored fixtures, but are not emitted by the draft strategy.

```text
MCC_OHLCV|v1|symbol|timeframe|timestamp|open|high|low|close|volume
```

The normalized CSV schema is:

```csv
Symbol,Timeframe,Timestamp,Open,High,Low,Close,Volume
```

`Timestamp` is an ISO-8601 timestamp with an explicit offset and represents the completed source bar's availability timestamp. `Open`, `High`, `Low`, and `Close` are the source bar's OHLC values. `Volume` is `volume[1]`, the actual source bar volume; it is never previous close, inferred, or silently converted to zero. `Timeframe` is independently set to `1m` or `5m`. The parser rejects any other payload and rejects the legacy `kg_EveryTickSOHLCP` encoding because its volume field is not identifiable.

The current C# replay contract consumes completed-bar availability timestamps. If Thinkorswim's `GetTime()[1]` is confirmed to be a bar-open timestamp, the export adapter must convert it to the documented completed-bar timestamp using the verified aggregation interval and session rules before normalization. Do not apply that conversion until chart comparison proves the source convention.

## ThinkScript indexing and AddOrder timing

The strategy emits on the current bar while encoding `[1]` values. This is intentional: current-bar values may be incomplete, while `[1]` is the preceding completed bar and avoids look-ahead. The report's own `Date/Time` is the order/report timestamp and must not be assumed to be the source bar timestamp. The payload separately carries `GetTime()[1]` so the normalizer can use the source timestamp if Thinkorswim preserves it.

This behavior is an engineering hypothesis until verified in Thinkorswim. Specifically, verify all of the following:

1. `GetTime()[1]` is accepted and represents the intended source-bar time.
2. Dynamic `AddOrder` names are accepted and are not truncated or rewritten.
3. Decimal values and volume survive report serialization without loss that affects validation.
4. Each report row corresponds to exactly one source bar, with no first-bar, last-bar, or one-bar shift.

If exact timestamps cannot be embedded reliably, do not guess. Use a verified reconstruction from report timestamp plus aggregation/session metadata, or use native chart-data export instead. Rows without a verified timestamp must remain unknown and must not enter replay.

## Session, aggregation, and timezone policy

Run separate exports for `1m` and `5m`; do not mix them in one export job. Set `includeExtendedHours` explicitly for every run. The default validation matrix is:

- RTH-only: 9:30 AM–4:00 PM New York time;
- extended-hours: the user's explicitly selected Thinkorswim extended session, recorded in the manifest;
- no fixed Arizona offset: convert using a named IANA timezone such as `America/Phoenix` for Arizona and `America/New_York` for exchange time, allowing New York DST changes;
- preserve the original offset in normalized timestamps and record the conversion convention;
- do not fabricate bars across overnight, weekend, holiday, or session gaps. A missing field is invalid; an absent expected bar is a validation finding, not a zero-volume bar.

The export strategy's session predicate is only a draft guard. The actual chart session settings and symbol's exchange calendar must be recorded and checked against consecutive chart bars.

## Transport comparison

**MCC strategy report:** available through `AddOrder`, can carry an owned machine-readable payload, and is compatible with the parser added here. It has higher effort and risk because AddOrder timing, dynamic names, serialization length, and strategy-report semantics require TOS verification. It must not be treated as a broker workflow.

**Native Thinkorswim chart-data export:** preferred if the user's installed TOS UI can export chart bars with timestamp, symbol, OHLC, and actual volume directly. It avoids AddOrder timing and order-name truncation, but this repository has no evidence that the user's UI exposes a stable native export for both aggregations/session modes. Confirm the menu, fields, timestamp convention, and repeatability in the user's installation before choosing it.

Decision: implement and test the strategy-report parser as a controlled fallback while investigating native export first. Native export becomes the preferred transport only after its produced file satisfies the same schema and parity checks.

## Normalization and validation

`ThinkorswimOhlcvStrategyReportReader` skips report headers, summary rows, and the column header, then accepts only `MCC_OHLCV|v1|` payloads. It rejects mixed/legacy rows, malformed payloads, missing or non-positive timestamps, invalid OHLC relationships, negative volume, duplicate symbol/timeframe/timestamp keys, and unexpected symbol/timeframe values. It does not repair or fill missing bars. The existing `CsvMarketBarReader` remains the reader for already-normalized canonical CSV.

The current XRPN report is a required negative fixture: its six encoded records contain no identifiable volume and must be rejected as incomplete OHLCV.

## Required TOS chart-parity procedure

This procedure is external validation, not an automated xUnit test:

1. Create or open `MCC_OHLCV_V1` in Thinkorswim and confirm the source version/header.
2. Run separate 1m and 5m exports for the same symbol, date, session mode, and timezone context.
3. Confirm compilation and inspect the Strategy Report for intact `MCC_OHLCV|v1|` names.
4. Normalize without changing the original report.
5. Compare at least 20 consecutive bars for each aggregation against chart OHLCV, including the first/last bar of a session, a gap, and a DST-observing date. Record timestamp, O/H/L/C, volume, and any shift.
6. Compare 1m bars against the corresponding completed 5m bar boundaries; document whether the 5m timestamp is open or availability time.
7. Repeat with extended hours if that mode will be used.
8. Store the comparison results and manifest. Only then may the transport be considered validated; it is still not production-ready until operational controls are separately approved.

Automated parser tests prove only framing and validation behavior. They cannot prove Thinkorswim compilation, installed-study identity, chart parity, or live export semantics.
