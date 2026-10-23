# UPC 2026-10-22/23 — 5M-to-1M Extension-Danger Evidence Review

**Status:** Initial screenshot/fact review only. Installed-study parity is not certified.
**Symbol/date as reported:** UPC, 2026-10-22
**Reported event:** 09:25 chart time; chart timezone not recorded.
**Installed study names reported:** `MCC_V3_2_5M_V4_1_7`, `MCC_V3_2_5M_AuthorityDiagnosticV2`, `MCC_1M_14_4_10_AggressiveAuditOnly_2`.

## Evidence supplied

- Screenshot `Screenshot 2026-10-23 112544.png`: overview of UPC 5M and 1M charts, with extension-danger and in-trend-candidate examples visible on the 1M chart.
- Screenshot `Screenshot 2026-10-23 112726.png`: 1M Aggressive diagnostics plus 5M context.
- Screenshot `Screenshot 2026-10-23 113641.png`: dense 1M normal diagnostics plus 5M context.
- User-reported facts:
  - 5M Permission before/after: `Perm=1 → Perm=1`.
  - 5M diagnostic blocker: `PBLOCK=0 → PBLOCK=0`.
  - 1M reconstructed 5M values: `P=1 / BLK=6` (exact meaning of `BLK=6` is not yet confirmed from installed source).
  - 1M state: `5M RUNNER WANING; CANDIDATE BLOCKED: EXTENSION DANGER; IN-TREND CANDIDATE`.
  - `EXT=2.35`.
  - Session reported as regular.
  - User reports absent 5M candles at 09:20 and 09:30, and absent 1M candles at 09:16–09:24 and 09:26–09:36.

## Initial interpretation

1. The reported states are consistent with a **valid bullish 5M permission alongside a blocked 1M entry due to extension danger**. That is not, by itself, a contradiction or a reason to weaken the 5M gate. The in-trend-candidate label must not be treated as EXECUTION READY.
2. `EXT=2.35` is above the 2.00 ATR danger threshold documented in the repository's 1M study reference, but the exact installed source for `MCC_1M_14_4_10_AggressiveAuditOnly_2` has not been supplied in this evidence bundle. Therefore this is a repository-reference comparison, not certified installed-source parity.
3. The screenshots show a diagnostic-rich example but do not establish why the user reports absent intervals. A missing candle/bar does **not** prove there was no volume or no price movement. It could reflect a no-eligible-trade interval, chart/session/data settings, hidden/compressed display, a data-loading issue, or a mismatch between the stated event date and screenshot date. Do not synthesize bars or silently fill gaps.
4. The screenshots' top headers appear to show a date/time of **10/23/26 around 11:26 AM**, while the event facts specify **10/22/26**. The visible axis includes the 09:25 area, but the date mismatch must be reconciled before exact bar alignment.
5. The stated 5M values remain `Perm=1` and `PBLOCK=0`; thus the provided facts do not show a 5M permission revocation at 09:25. The test case instead concerns 1M extension blocking while 5M permission stays on.

## Do not infer

- Do not infer absent bars mean zero volume, zero movement, or a market halt.
- Do not infer `BLK=6` semantics without the installed diagnostic code mapping.
- Do not infer the installed studies exactly match repository source because their names were reported.
- Do not classify the in-trend candidate as safe to buy or READY.
- Do not alter permission, extension thresholds, or execution gates from this screenshot alone.

## Next evidence needed (minimum)

1. Confirm whether these screenshots show **2026-10-22 or 2026-10-23** and record the chart timezone (Arizona/MST or Eastern/ET).
2. In TOS, use the data/candle cursor or crosshair to identify the exact timestamps of the bars immediately before and after 09:25 on each aggregation. Confirm whether bars are genuinely absent or merely unlabeled/visually compressed.
3. If bars are genuinely absent, export or inspect the corresponding 1M/5M OHLCV for UPC and the selected session. Preserve source timestamps and do not fill gaps.
4. Provide the complete installed 1M study source (or confirm the exact uploaded source file is byte-for-byte the installed study) and the installed diagnostic code mapping for `BLK=6`.
5. If possible, capture a tight 5M/1M view spanning 09:10–09:40 with the date, chart timezone, aggregation, and crosshair time visible. The existing screenshots are enough to start the conceptual review, but not to certify exact bar-by-bar parity.

## Result so far

**No logic change justified.** Keep 5M authority separate from 1M execution eligibility. Treat this as a negative test case: extension danger should block execution while the higher-timeframe permission may remain valid. Continue only after timestamp/bar presence and installed source semantics are resolved.

## [Uncertainties]

- Chart timezone is not stated.
- Screenshot date appears to conflict with the reported event date.
- Whether the specified bars are truly absent or just unlabeled/visually compressed is not established.
- Installed 1M source and the `BLK=6` mapping are not verified in the supplied files.
