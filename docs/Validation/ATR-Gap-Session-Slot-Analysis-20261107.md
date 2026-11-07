# ATR gap session-slot analysis and qualification follow-up — 2026-11-07

**Branch:** `feature/opportunity-quality-classification`  
**Inputs:** operator-supplied parity JSON from the local API, using the existing persisted 327 EVR / 338 CBOE 5m observations. No CSV was re-imported.

## 1. Exact observed discontinuities

All timestamps below are UTC from the parity API. The Eastern conversions use EST (UTC−05:00) on the supplied November 4–5, 2026 dates.

| Symbol | Previous observed bar | Next observed bar | Eastern interval | Wall-clock gap | Raw 5m slots | Expected slots inside 04:00–20:00 ET |
|---|---|---|---|---:|---:|---:|
| EVR | 2026-11-05 05:10Z | 2026-11-05 14:05Z | 00:10 → 09:05 ET | 8h55m | 106 | 61 |
| EVR | 2026-11-05 14:05Z | 2026-11-05 14:30Z | 09:05 → 09:30 ET | 25m | 4 | 4 |
| CBOE | 2026-11-04 21:00Z | 2026-11-04 23:40Z | 16:00 → 18:40 ET | 2h40m | 31 | 31 |

The earlier `missingIntervalCount=106` for EVR was a wall-clock arithmetic count. It incorrectly treated scheduled 20:00–04:00 ET closure slots as if they were expected chart timestamps. The first EVR gap crosses both the overnight closure and premarket. Within the declared 04:00–20:00 ET extended-hours chart session, it contains 61 expected 5-minute timestamp slots (04:00 through 09:00 ET). The second EVR gap contains four expected slots (09:10, 09:15, 09:20, 09:25 ET). The CBOE after-hours gap contains 31 expected slots (16:05 through 18:35 ET).

**Important:** “expected slot” means the timestamp falls inside the declared chart-session window. It does not prove a trade occurred or that Thinkorswim should have rendered a candle at that time. No OHLCV row is synthesized.

## 2. Code correction

Updated `AtrTimestampGapDetector` to walk the expected source timestamps and count only slots inside 04:00–20:00 America/New_York. Scheduled 20:00–04:00 closure slots are excluded even when a same-date gap crosses from after midnight into premarket.

The parity response now reports:
- `missingIntervalCount`: expected timestamp slots inside the declared chart session, not wall-clock slots.
- `firstExpectedMissingBarAtUtc`
- `lastExpectedMissingBarAtUtc`
- Existing previous/current timestamps and elapsed gap duration.

Regression cases assert:
- EVR 00:10→09:05 ET: 61 expected session slots, first 04:00 ET, last 09:00 ET.
- EVR 09:05→09:30 ET: 4 expected session slots, first 09:10 ET, last 09:25 ET.
- CBOE 16:00→18:40 ET: 31 expected session slots, first 16:05 ET, last 18:35 ET.
- A synthetic 1-minute gap still reports its exact expected missing timestamp.

GitHub Actions passed on code/test commit `028796bb4915ab8b118f7f5550eb8acd425aa250`: Release build, 0 warnings/errors, 239/239 tests. The subsequent catalyst, Overnight provenance, and Opportunity Quality pool changes are tracked by their own workflow runs; check exact latest branch-head CI before pulling.

## 3. Existing exports inspected

The committed EVR and CBOE Strategy Report exports were inspected directly. They contain the same source discontinuities represented by the operator's parity response. They do not contain evidence that a trade occurred or a bar should exist inside the missing intervals. No repeat export or import is justified from this evidence alone.

The revised V2.1 ThinkScript source already removes the prior 07:00–19:59 ET export filter and exports every available host-chart bar in the selected date range. It remains uncompiled/unverified in Thinkorswim.

## 4. Required chart-side decision

For each interval, inspect the actual TOS chart with extended hours ON and the same symbol, aggregation, date, adjustment and timezone. Record whether the chart displays any 5-minute candles at the expected times and, if so, their exact timestamps/OHLCV.

- EVR 04:00–09:00 ET: determine whether the chart has premarket candles or genuinely no chart bars before 09:05.
- EVR 09:10–09:25 ET: determine whether those four premarket candles exist.
- CBOE 16:05–18:35 ET: determine whether after-hours candles exist between the two observed rows.

If candles exist but are absent from the Strategy Report, the export is incomplete and must be corrected. If the chart itself has no candles, document the TOS no-trade/no-bar semantics and decide whether the parity contract should distinguish “no source candle” from “omitted export row.” Do not impute zero-volume bars. Do not relax ATR tolerance.

## 5. Other work implemented in the same continuation

### Overnight boundary provenance

The close-to-next-open history result now explicitly identifies the next-open source as `ONE_MINUTE_BAR_OPEN_PROXY` and `OfficialOpeningAuctionVerified=false`. It documents that the previous RTH close to next RTH 09:30 bar-open interval spans Day 1 after-hours and Day 2 premarket in its net endpoint return, but does not reconstruct the path or eligible-window extrema.

This does **not** establish that the 09:30 one-minute bar Open equals the official opening-auction print or the first eligible observed trade. Current IEX coverage is diagnostic only; SIP remains parked under the known HTTP 403 blocker.

### Catalyst human-label validation infrastructure

Human-review records now preserve the triage prediction severity/categories that existed when the article was reviewed. Added `GET /api/market-data/catalyst-reviews/validation`, which deduplicates by symbol + article identity, excludes `DUPLICATE`, `UNCERTAIN`, legacy reviews without saved predictions, and conflicting labels, then reports a chronological later-20% holdout confusion matrix with precision/recall/specificity/accuracy.

Human-positive labels: `MATERIAL_RISK`, `MATERIAL_EVENT`. Human-negative labels: `NON_MATERIAL`, `IRRELEVANT`. Predictions: `RISK` or `MATERIAL` are positive; `UNCLASSIFIED` is negative. This evaluates the fixed triage rules; it does not train the model and cannot clear a catalyst. **No human-label sample or measured precision/recall is established by this code change.**

### Opportunity Quality pool ordering

Added `GET /api/market-data/opportunity-quality/rank?symbols=...` for 1–50 unique user-supplied symbols. It assesses every supplied symbol with concurrency capped at four, returns all candidates including DATA? / BLOCKED, and applies deterministic diagnostic ordering by existing classification then raw 5M RVOL, 30-bar 1M range/ATR and prior-high distance. It deliberately creates no weighted score or probability claim.

This is an API capability for an explicitly bounded pool, not market-wide discovery and not yet wired to a Dashboard control. The existing assessor thresholds remain provisional. Full Dashboard integration and historical calibration remain open.

## 6. [Uncertainties]

- TOS chart candle presence at the 61 EVR premarket slots, four later EVR slots and 31 CBOE after-hours slots is not established by the CSVs or parity JSON. **Data not found in sources.**
- Revised V2.1 ThinkScript compilation and fresh export remain unverified.
- Official opening-auction print / first eligible observed trade parity is not available from the current evidence. **Data not found in sources.**
- Catalyst validation metrics require a sufficient, human-adjudicated article sample. **Data not found in sources.**
- Opportunity Quality full-pool ranking remains diagnostic, bounded to the explicit symbols supplied, and not calibrated.
