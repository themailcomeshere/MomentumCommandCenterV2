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

ThinkScript string values must remain in the string expression passed to `AddOrder`; they cannot be assigned to a numeric `def`. This behavior and the dynamic-name expression are still hypotheses until verified in Thinkorswim. Specifically, verify all of the following:

1. `GetTime()[1]` is accepted and represents the intended source-bar time.
2. Dynamic `AddOrder` names are accepted and are not truncated or rewritten.
3. Decimal values and volume survive report serialization without loss that affects validation.
4. Each report row corresponds to exactly one source bar, with no first-bar, last-bar, or one-bar shift.

If exact timestamps cannot be embedded reliably, do not guess. Use a verified reconstruction from report timestamp plus aggregation/session metadata, or use native chart-data export instead. Rows without a verified timestamp must remain unknown and must not enter replay.

## Session, aggregation, and timezone policy

Run separate exports for `1m` and `5m`; do not mix them in one export job. Set the MCC strategy's `startTime` and `endTime` inputs explicitly for every run, and make the chart's extended-hours setting match the run. The default validation matrix is:

- RTH-only: 9:30 AM–4:00 PM New York time;
- extended-hours: the user's explicitly selected Thinkorswim extended session, recorded in the manifest;
- no fixed Arizona offset: convert using a named IANA timezone such as `America/Phoenix` for Arizona and `America/New_York` for exchange time, allowing New York DST changes;
- preserve the original offset in normalized timestamps and record the conversion convention;
- do not fabricate bars across overnight, weekend, holiday, or session gaps. A missing field is invalid; an absent expected bar is a validation finding, not a zero-volume bar.

The export strategy's session predicate is only a draft guard. The actual chart session settings and symbol's exchange calendar must be recorded and checked against consecutive chart bars.

The strategy defaults are `startDate = 19700101`, `endDate = 20991231`, `startTime = 0930`, and `endTime = 1600`. Date inputs use Thinkorswim's `YYYYMMDD` exchange-session date. `SecondsFromTime` and `SecondsTillTime` use Eastern Time, matching the original `kg_EveryTickSOHLCP` comments; they are not Arizona wall-clock inputs. For example, during Eastern daylight time, 5:00 AM Arizona is approximately 8:00 AM Eastern. For an extended-hours run, change the Eastern-time inputs deliberately (for example, `0800` and `1100` for approximately 5:00–8:00 AM Arizona during DST) and enable extended-hours data on the chart. The date inputs filter calendar dates; the time inputs filter time of day. These inputs are not the same as the report's `Work Time` range: `Work Time` describes the first and last emitted report events, while the strategy inputs control which completed source bars emit MCC rows. The filter is applied to the same `[1]` source bar whose OHLCV values are encoded.

`aggregationLabel` does not change the chart aggregation. MCC now compares it with `GetAggregationPeriod()`, emits no rows on a mismatch, and derives the payload timeframe from the actual chart aggregation. Selecting `FiveMinute` requires changing the chart itself to 5 minutes; otherwise the label is red and no export rows should be produced.

The strategy displays an `MCC_OHLCV_V1 loaded` label when installed and cyan point markers for bars passing the export filter. If the label is absent, the edited strategy is not the active chart strategy. If the label is present but markers are absent, the date/time filter or chart-loaded range excludes the source bars.

Changing the inputs does not move the report's `Work Time` text or the chart's loaded data range. It should change the number and timestamps of `MCC_OHLCV_V1` rows. If the rows remain identical, the installed strategy is not the saved `MCC_OHLCV_V1` source, the inputs were changed on another strategy, or Thinkorswim has not recalculated the chart; remove/re-add the strategy, save its settings, and confirm the strategy name before exporting.

## Transport comparison

**MCC strategy report:** available through `AddOrder`, can carry an owned machine-readable payload, and is compatible with the parser added here. It has higher effort and risk because AddOrder timing, dynamic names, serialization length, and strategy-report semantics require TOS verification. It must not be treated as a broker workflow.

**Native Thinkorswim chart-data export:** preferred if the user's installed TOS UI can export chart bars with timestamp, symbol, OHLC, and actual volume directly. It avoids AddOrder timing and order-name truncation, but this repository has no evidence that the user's UI exposes a stable native export for both aggregations/session modes. Confirm the menu, fields, timestamp convention, and repeatability in the user's installation before choosing it.

Decision: implement and test the strategy-report parser as a controlled fallback while investigating native export first. Native export becomes the preferred transport only after its produced file satisfies the same schema and parity checks.

## Normalization and validation

`ThinkorswimOhlcvStrategyReportReader` skips report headers, summary rows, and the column header. It accepts the observed Thinkorswim wrapper `MCC_OHLCV_V1(MCC_OHLCV|v1|...)`, removes thousands separators from numeric fields, and ignores unrelated transport or legacy strategy rows in a mixed report. A report with no complete MCC payloads is rejected; therefore both the original XRPN report and `StrategyReports_XRPN_101926_2127.csv` are negative fixtures. The parser also rejects malformed payloads, missing or non-positive timestamps, invalid OHLC relationships, negative volume, duplicate symbol/timeframe/timestamp keys, and unexpected symbol/timeframe values. It does not repair or fill missing bars. The existing `CsvMarketBarReader` remains the reader for already-normalized canonical CSV.

The current XRPN report is a required negative fixture: its six encoded records contain no identifiable volume and must be rejected as incomplete OHLCV.

Observed export evidence: `StrategyReports_XRPN_101926_2147.csv` contains one MCC-only 1m row with OHLC `15.43/15.43/15.43/15.43` and volume `1`. Its embedded epoch timestamp renders as `2026-10-19T00:53:00-07:00`, while the report row is dated `1:19 AM`; this 26-minute discrepancy means timestamp/bar alignment remains unverified. One row is also insufficient for consecutive-bar or gap validation.

Observed successful multi-bar export: `StrategyReports_XRPN_102026_933.csv` contains 355 MCC payload rows and 355 transport rows from 6:32 AM through 1:02 PM report time. Embedded payload timestamps run from 6:30 AM through 1:00 PM Arizona time, producing a stable two-minute report-to-payload offset in this capture. Payload timestamps are predominantly one minute apart, but 31 gaps are longer than one minute, including 2- and 3-minute gaps. OHLC invariants pass and volumes are nonnegative (`100` through `62,346` in the captured rows). This is a successful parser/export smoke test, not yet chart parity or proof that missing intervals should be filled.

The follow-up reports `StrategyReports_XRPN_102026_950.csv` and `StrategyReports_XRPN_102026_951.csv` remain 1m exports: both payloads are labeled `1m` and advance by 60 seconds. The first report begins with an embedded 6:30 AM bar reported at 6:32 AM; the second begins with an embedded 6:28 AM bar reported at 6:30 AM. This is a consistent two-minute transport/report difference, not evidence that changing `aggregationLabel` changed aggregation. The chart aggregation must be changed separately.

The verified 5m capture `StrategyReports_XRPN_102026_5m_offset_1027.csv` contains 81 payload rows labeled `5m`, with embedded timestamps from 6:20 AM through 1:00 PM Arizona time and report times from 6:30 AM through 1:10 PM. The ten-minute difference is consistent with the report transport timing for this aggregation. The `startTime` must describe the desired embedded source-bar window; do not move it earlier merely to make the report's `Date/Time` appear aligned.

## Current action checklist

1. Keep `startTime` and `endTime` set to the desired **source-bar** window in Eastern time; do not compensate for the report timestamp. The report `Date/Time` is transport metadata, not the normalized bar timestamp.
2. For 1m validation, set the chart to 1 minute and `aggregationLabel = OneMinute`; preserve the raw report and compare at least 20 payload rows to the chart.
3. For 5m validation, set the chart to 5 minutes and `aggregationLabel = FiveMinute`; preserve a separate raw report and compare at least 20 payload rows to the chart.
4. Record whether each embedded timestamp is a bar-open or completed-bar availability timestamp. Do not change the parser or add a fixed 1m/5m offset before that comparison.
5. Compare 5m OHLCV with the corresponding 1m bars and classify every observed gap.
6. Store the raw reports, normalized copies, comparison notes, session settings, date/time inputs, chart aggregation, timezone/DST context, and validation status as provenance.

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

The imported legacy reports show alternating close/open rows at each emitted timestamp. MCC uses the same simulated close/reopen transport pattern because repeated `BUY_TO_OPEN` rows are not emitted reliably while a strategy position remains open. The payload `BUY_TO_OPEN` declaration must precede the transport `SELL_TO_CLOSE` declaration: the observed `StrategyReports_XRPN_102026_901.csv` showed that declaring the close first on an empty position produced `Sell to Open` and suppressed the payload row. The close row is transport-only; the buy row carries the payload. This behavior is an engineering hypothesis until verified in Thinkorswim. Specifically, verify all of the following:

1. The first emitted row's timestamp matches the expected source-bar timestamp or availability time.
2. Subsequent rows alternate close/open as seen in the legacy reports.
3. Declaring `BUY_TO_OPEN` before `SELL_TO_CLOSE` rows is necessary and effective.
