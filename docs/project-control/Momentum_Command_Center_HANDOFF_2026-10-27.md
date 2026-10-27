# Momentum Command Center — New Chat Handoff

**Updated:** 2026-10-27 (Arizona time)  
**Repository:** https://github.com/themailcomeshere/MomentumCommandCenterV2  
**Canonical branch:** `master`  
**Active engineering PR:** https://github.com/themailcomeshere/MomentumCommandCenterV2/pull/7

## Current verified repository state

- Latest observed `master` commit: `b90fdb7e03e43e86c8c1b8429f4398c330a80230` (2026-10-26).
- The latest commit adds October data-readiness findings to the Boring Overnight launch checklist.
- The latest `master` commit's combined-status endpoint returned no status entries. Do not describe that response as green CI.
- PR #7 adds a read-only Alpaca historical-bars client with page-token traversal and bounded retry handling, plus unit tests. At handoff time, PR #7 is open and no workflow run was returned for its head commit. It is not verified green or merged.
- The user's prior report of 94/94 unit tests passed applies to the user's prior local state; it does not prove that the newly added PR #7 code compiles or passes tests.

## StrategyReport → parity output

### User requirement

When the user uploads a Thinkorswim StrategyReport export, extract its embedded `MCC_OHLCV` payload rows and convert them to the readable, usable parity-template format. Do not make the user manually copy individual rows.

### Implemented in PR #7

- `StrategyReportParityParser.Parse(reportText)` reads the semicolon-delimited StrategyReport table and extracts embedded payloads of the form `MCC_OHLCV|v1|SYMBOL|TIMEFRAME|EPOCH_MS|OPEN|HIGH|LOW|CLOSE|VOLUME`.
- Transport-close rows are ignored.
- Output uses embedded epoch timestamps, not the StrategyReport's transport `Date/Time` column.
- The parser validates payload version, timestamp, numeric fields, and OHLC bounds. It does not synthesize missing bars.
- `ToParityCsv()` emits a normalized semicolon-delimited interchange CSV with Symbol, Timeframe, TimestampUtc, Open, High, Low, Close, Volume.
- Unit tests cover sample parsing, transport-close filtering, stable CSV output, missing header, and invalid OHLC bounds.

### Still unresolved

- The exact canonical parity workbook/template was not found in the accessible repository sources during this turn. The normalized CSV is a useful machine-readable intermediate, but must not be described as a perfect match to an unavailable workbook schema.
- The current `master` tree does not expose the Swagger upload API/controller mentioned in the operator's workflow. The new parser is not yet wired into that API or an upload endpoint. Locate the actual API project/repository or merge its branch before claiming upload integration complete.
- Verify output against the user's actual parity template and a real export, including multi-day reports, quoted fields, empty payload rows, duplicate timestamps, malformed payloads, and 1M/5M alignment.

## Alpaca historical data

### Implemented in PR #7; awaiting CI

- Read-only historical stock-bar request for a symbol, timeframe, start/end and feed.
- Page-token traversal with repeated-token loop protection.
- Bounded retry attempts for HTTP 429/5xx and transport failures, respecting Retry-After when present.
- Chronological output, timestamp parsing, parameter validation, and tests for pagination/retry/non-transient HTTP errors.
- No order-placement API added.

### Not implemented / not verified

1. Durable persistence and idempotent upsert semantics for bars.
2. Collector checkpoint/cursor persistence and restart recovery after partial collection or process interruption.
3. Explicit gap detection, overlap re-fetch policy, duplicate-bar conflict policy, and correction handling.
4. Correct persistence-count semantics: distinguish requested, fetched, validated, inserted, updated, duplicate, rejected, and missing/gap observations. Counts must not label a fetched row as persisted unless the database confirms it.
5. Historical and extended-hours feed coverage validation for the operator's required session. Alpaca IEX coverage must not be represented as equivalent to Schwab GTC_EXT.
6. Provider-backed bars wired into scanner candidate generation and the Boring Overnight workflow with freshness, feed, timestamp, and data-gap state visible.
7. End-to-end tests proving provider failure, stale data, incomplete coverage, excluded symbols, and persistence failures cannot silently yield READY.
8. Real-provider verification with the user's configured credentials/feed after local Release build and tests.

No source-backed historical data for the full GTC_EXT window was found in the audited files. Do not use placeholder excursion values or infer fill probability from highs.

## TTE chart bubble stacks — user screenshot 2026-10-27

The screenshot shows repeated yellow `PREPARE TO SELL` bubbles and red `SELL` bubbles crowded around nearby bars, plus simultaneous 1M/5M status labels. Bubble content overlaps enough to obscure price action.

This is a chart usability defect that must be fixed in the actual installed study source, not by guessing from an older Library version.

### Required implementation

1. Obtain/identify the exact installed `MCC_1M_14_5_EOD_BubbleFix2` source and determine whether the repeated bubbles originate from multiple `AddChartBubble` calls in the 1M study, the 5M companion study, or both.
2. Keep the 5M authority bubbles and 1M execution bubbles semantically distinct; do not suppress one layer blindly.
3. On each study, emit at most one primary state/event bubble per bar. Merge simultaneous same-study events into a concise composite label with deterministic priority, rather than drawing multiple bubbles at the same x-coordinate.
4. Do not draw a new state bubble on every bar merely because the state persists. Prefer transition/event bubbles, with an input to show historical per-bar diagnostics only when explicitly enabled.
5. Use deterministic vertical lanes/offsets for the remaining 1M and 5M bubbles so the two studies do not occupy the same position. Use ThinkScript-supported offsets/conditions; do not invent unsupported drawing APIs.
6. Preserve EOD detection and all state semantics. Correctly block every comment using ThinkScript comment syntax and keep expressions readable without unnecessary multi-line stretching.
7. Compile in Thinkorswim, compare TTE and at least one contrasting symbol, and verify the number of bubbles per bar visually. Source inspection alone is not runtime verification.

No source edit was made to the installed ThinkScript study in this turn because the exact `MCC_1M_14_5_EOD_BubbleFix2` source was not available in the accessible Library results. The screenshot is sufficient to confirm the symptom, not to identify the precise duplicated call sites.

## Other known open work / blockers

- Finish the master To-Do reconciliation against the older master To-Do, Boring Overnight To-Do, and all recent project notes. Preserve already-completed items and mark evidence/branch status precisely.
- Finish 5M authority → 1M execution handoff validation with aligned same-symbol/timeframe exports and study diagnostics. OHLCV alone does not expose Permission, block reason, candidate/re-arm, READY, or expiration.
- October 2 XRPN remains blocked: the purported 1M file is actually HPK. Do not pair HPK with XRPN 5M.
- October 19 XRPN 1M/5M has partial aligned coverage; missing timestamps remain data gaps, not presumed no-trade. The chart-parity worksheet has only 20 manually entered rows per timeframe.
- Boring Overnight target-reach research remains blocked until source-backed close and full 7:00 AM–8:00 PM ET eligible-window bars are available for the symbol/date.
- News-assisted open-position review remains manual only. The current latest-news endpoint has a limited recent window and zero articles is not proof of no news.
- No automatic order placement, cancel, replace, or sell.

## Next steps — assistant

1. Verify PR #7 CI; fix compile/test failures before merge.
2. Add collector persistence/checkpoint/recovery and explicit count semantics with tests.
3. Wire historical provider output through the existing candidate/evaluator path only after inspecting the actual domain interfaces and adding stale/gap/exclusion tests.
4. Inspect all current To-Do sources and reconcile the master list; do not treat one recent document as the only source.
5. Locate the exact installed V14.5 ThinkScript source, then fix per-bar bubble duplication and produce a complete install-ready file.
6. Find the actual parity template and API upload project, then wire StrategyReport parsing into that upload flow and verify with a real export.
7. Update this handoff and the master To-Do with actual test/CI results and exact remaining blockers.

## Specific operator actions

1. **StrategyReport parity:** Upload the actual parity-template workbook/file (or give its exact repository path) if it is not already present in Project Sources. The workbook's exact column order and labels are not discoverable from the currently accessible source results.
2. **ThinkScript bubble fix:** Supply or identify the exact currently installed `MCC_1M_14_5_EOD_BubbleFix2` source file if it is not in the Project/Library sources. The screenshot confirms duplication but cannot establish which source calls create it.
3. **Alpaca:** After PR #7 is green and the persistence/collector work is ready, run the local Release build/tests and a credentialed historical request using the intended feed. Report account/feed entitlements and whether required extended-hours bars are available; never send credentials in chat or commit them.
4. **Data needed for validation:** Provide source-backed complete-session exports only when requested for a specific unresolved test. Do not re-upload the already identified mislabeled HPK file as XRPN.

## [Uncertainties]

- PR #7's CI result and compilation status are not yet known.
- The exact canonical parity template and Swagger upload API source are not present in the accessible master tree results.
- The exact installed V14.5 study source has not been identified, so the bubble fix is not yet safely patchable.
- Alpaca feed/session coverage and provider entitlement for the required GTC_EXT window remain unverified.
