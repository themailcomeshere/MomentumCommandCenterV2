# Boring Overnight Main Chart — browser acceptance evidence

**Date:** 2026-11-10  
**Branch:** `feature/opportunity-quality-classification`  
**Feature:** Boring Overnight Main Chart's five shared diagnostic metric cards  
**Evidence source:** Operator-reported browser output after confirming the frontend cache key `20261110-overnight-main-chart-metrics-v1`.

## Operator acceptance result

| Main Chart field | Operator-observed value | Displayed provenance / interpretation | Result |
|---|---:|---|---|
| 1M CLOSE | $35.8900 | Latest returned 1M bar close; historical research only; 2026-11-10 1:59 PM Arizona; Alpaca/IEX; not a live quote | PASS |
| DAILY VWAP | $35.9837 | RTH 1M typical-price VWAP proxy; session 2026-11-10; 285 volume bars | PASS |
| ATR14 | $0.0314 | Simple average true range from 1M bars; 1,021 ranges available; 14-bar value | PASS |
| RELATIVE VOLUME | NOT USED | Intraday same-time 5M RVOL is intentionally not evaluated in Boring Overnight | PASS |
| ATR EXTENSION | 2.98 ATR | Historical absolute 1M close-to-RTH-VWAP distance divided by 1M ATR14; diagnostic only | PASS |

Additional operator confirmations:

- [x] The loaded `app.js` request contained cache key `20261110-overnight-main-chart-metrics-v1`.
- [x] Intraday Scanner Workbench is hidden in Boring Overnight mode.
- [x] Ctrl+F finds the exact heading **Intraday Scanner Workbench** in Intraday mode.

**Acceptance:** The Boring Overnight five-metric browser rendering and scanner-visibility checks are accepted.

## What this evidence does not establish

- The 1M close is a historical bar close, not a live quote, NBBO, or executable price.
- The displayed DAILY VWAP is a 1M typical-price proxy for the named RTH session, not an independent provider VWAP.
- The displayed ATR14 is calculated from 1-minute bars. It is not Daily ATR14 or proof of Thinkorswim ATR V2 parity.
- RELATIVE VOLUME is intentionally not used in Boring Overnight; no same-time baseline is claimed.
- ATR EXTENSION is descriptive research only. It does not establish candidate qualification, permission, readiness, entry/exit advice, or fill probability.
- Alpaca/IEX remains partial-exchange coverage. These cards do not upgrade any candidate's `DATA? · IEX_PARTIAL` state.

## Remaining Boring Overnight workflow acceptance

The five metric cards are no longer a blocker. The broader workflow still needs separate operator evidence for:

1. Import the current Overnight scanner CSV and verify all source rows import and display.
2. Verify Boring Overnight shortlist ordering and top-20 selection against the imported rows.
3. Run Daily audit and Fetch Daily + catalyst for Boring Top 4; verify four distinct symbols and each symbol's evidence row.
4. Review price/volume, exclusions, Daily completeness, catalyst evidence, and Main Chart context for each candidate.
5. Confirm zero returned articles remain UNKNOWN, not CLEAR, and IEX_PARTIAL remains non-qualifying for consolidated coverage.
6. Verify the Calculated Limit Sell disclosure renders every API-returned gate. The current API result is BLOCKED; no target should be promoted.
7. Verify the evaluation countdown against the actual exchange close, including DST and early-close handling.

## Related evidence boundaries

- Daily audit rows reported for CRBG, FG and IBN: 20/20 expected sessions, FRESH, no missing sessions, Alpaca/IEX, split/dividend adjusted; qualification remains `DATA? · IEX_PARTIAL`.
- LYSDY and BNPQY remain 0/20 on the configured IEX source. No independently validated replacement Daily source is evidenced.
- No order submission is implemented. MCC remains decision-support-only.
