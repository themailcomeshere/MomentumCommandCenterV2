# MCC automatic momentum scanner (free data), 2026-11-16

**Code:** `a942074`, in `src/MomentumCommandCenterV2.API/MarketData/AutoScan/`. Dashboard: INTRADAY MOMENTUM step **2.a**, **Automatic momentum scan** panel.
**Status:** built and unit-tested (13 tests). **Not yet run against live Alpaca data**; the first live session is an operator step.
**Purpose:** run the operator's TOS intraday scans automatically. It is discovery only: never READY, a quote or an order instruction.

## What runs

- **Schedule:** every 120 s during the regular session (09:30–16:00 ET = 07:30–14:00 AZ in standard time).
  - Config: `MarketData:AutoScan:Enabled` (default true) and `MarketData:AutoScan:IntervalSeconds` (default 120, minimum 60).
  - **Scan now** runs one immediately, at most once per 60 s.
- **Rules:** the five scans from `docs/Scanners/`, thresholds exactly as written. They have not been tuned, so the side-by-side comparison measures data coverage, not rule changes.
- **Logic:** all conditions within a scan are ANDed. Every bar condition uses the latest **completed** 5m regular-session bar.

| Code | Scan | Conditions |
|---|---|---|
| 1 | Momentum Hunter | Last $3–500 · close > VWAP · close > EMA9 > EMA20 > SMA50 · volume > 1.5 × 20-bar avg |
| 2A | Momentum Bullish | Last $4–501 · day volume ≥ 500,000 · % change ≥ +2% · close > VWAP · vol/avg20 ≥ 2.0 · (vol/avg20 > 2 and vol > 1.5 × previous bar) · close > highest high of prior 20 bars |
| 2B | Breakout Trigger | Last $3–500 · close > VWAP · close > EMA9 > EMA20 · volume > 1.5 × avg20 · close > prior 20-bar high |
| PIV | Momentum Pivot | Last $3.50–500 · day volume ≥ 500,000 · (20-bar breakout or VWAP reclaim) · close > EMA9 > EMA20 > SMA50 · close > VWAP · RSI(14) > 55 · volume > 1.5 × avg20 |
| D | Market Movers Discovery | Last $3–500 · day volume ≥ 250,000 · % change ≥ +2.99% |

- **Formulas (Thinkorswim):**
  - ExpAverage seeded with the first value.
  - Average is a simple average and includes the current bar.
  - RSI is Wilder's.
  - VWAP() is the session VWAP, using Alpaca's per-bar `vw` (typical price only when `vw` is absent).
- **Outcome:** PASS when all conditions are true, FAIL when any is false, UNKNOWN otherwise. An unknown input is never a pass.
- **Flagged** means all of:
  - passes at least one scan;
  - the latest bar is from today and closed within the last 10 minutes;
  - the symbol is not on the exclusion watchlist.
- **Ranking:** scans 1/2A/2B/PIV passed, then D, then % change.

## Data sources (free plan, CLAUDE.md compliant)

| Input | Source | Note |
|---|---|---|
| Universe | Alpaca screener: most-actives by volume (top 100), most-actives by trades (top 100), top 50 gainers | Alpaca documents most-actives as "based on real time SIP data". These are lists, not bar requests |
| Day volume | Most-actives `volume` (consolidated) | Unknown (DATA?) for gainers that are not in most-actives |
| Last price | IEX latest trade, from multi-symbol snapshots | |
| Prior close / % change | Completed SIP Daily bars, request end ≥ 16 min old (`DailyFeed=sip_delayed`, operator exception 2026-11-14) | Without `sip_delayed`, the IEX previous daily bar is used and labelled as a diagnostic |
| 5m bars | IEX, completed regular-session bars, 6 calendar days of history | Volume conditions are labelled `IEX_VOLUME_APPROX`; missing slots are reported as `IEX_GAPS_n` and never filled |

**Call budget:** about 10–15 requests per run (screeners 3, snapshots 2–3, Daily 2–3, bars about 2–8 pages). The free limit is 200 per minute.

## Known differences from Thinkorswim

1. **Universe:** a stock that is in neither the top-100 most-active lists nor the top-50 gainers is never scanned. TOS scans the whole market.
2. **Volume-surge conditions use IEX volume**, a small share of total volume, so they can differ from TOS in either direction.
3. **Completed bars only.** TOS also evaluates the forming bar, so MCC can flag up to one bar (5 min) later. The first result of the day appears after about 07:35 AZ.
4. **Scanner 1's timeframe:** 5m, confirmed by the operator 2026-11-16 (all Study rows show 5m), so MCC matches it.
5. **Extended hours:** regular-session bars only. This assumes the TOS scans have extended hours off.

## Side-by-side with TOS (5-session evaluation)

- Each TOS CSV import in INTRADAY MOMENTUM posts the imported symbols to `POST /api/scanner/auto-momentum/tos-comparison`.
- MCC compares them with every symbol it flagged or evaluated during that Eastern session, and puts each TOS symbol in one of three groups:
  - **flagged by MCC**;
  - **seen but not flagged**, which points to rule or IEX volume differences;
  - **not in MCC's universe**, which points to the free-plan coverage limit.
- It also lists symbols flagged only by MCC.
- **Logs** in `%LOCALAPPDATA%\MomentumCommandCenterV2\`:
  - `auto-momentum-scans.jsonl`: one compact line per run;
  - `auto-momentum-tos-comparisons.jsonl`.
- **Decision rule after 5 sessions** (operator decides):
  - If "not in universe" dominates the misses, the free universe is the limit. The $99 Alpaca Algo Trader Plus plan would allow a wider live universe.
  - If "seen but not flagged" dominates, it is rules or IEX volume; review the proposed improvements below.

## Proposed rule improvements (NOT built; need operator sign-off on the numbers)

1. Merge the five scans into one universe filter plus condition tags and a score. Today 1, 2B and Pivot repeat the trend, VWAP and volume checks.
2. Replace "volume vs the previous 20 five-minute bars" with same-time-of-day RVOL (MCC already computes this for the engine). The 20-bar average is dominated by the prior afternoon at the open.
3. Replace the fixed day-volume minimum (500k) with volume so far vs normal volume by this time of day, so stocks can qualify in the first 30 minutes.
4. Unify thresholds:
   - % change: +2% (2A) vs +2.99% (D);
   - day volume: 250k (D) vs 500k (2A, Pivot);
   - minimum price: $3 (1, 2B, D) / $3.50 (Pivot) / $4 (2A).
5. Add guards: ATR extension from VWAP, leveraged/inverse ETFs, and the exclusion list (already applied).
