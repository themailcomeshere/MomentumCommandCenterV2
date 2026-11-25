# 1M entry parity replay: MCC V2 vs 1M Main Study V14.4.12 (2026-11-25)

Branch `feature/opportunity-quality-classification`. MCC running `1023ba5` (launcher, PID 2920). Checklist: [Master-To-Do.md](../Master-To-Do.md) §5. Operator choice 2026-11-25: "Replay first" before deciding which side is the reference.

## Question

On the same bars, where do MCC's **EXECUTION READY** and the 1M study's green **EXECUTION READY** disagree, and which rule causes each disagreement?

## Method

- **Symbols and dates:** NVDA, TSLA and PLTR on 2026-11-24 (full regular session) and 2026-11-25 (09:31–12:40 ET). US standard time: AZ = ET − 2 h. These three had the most MCC READY minutes among NVDA, TSLA, PLTR, AMD, SMCI, MARA and AVGO.
- **Bars:** Alpaca IEX 1m and 5m bars, 2026-11-17 to 2026-11-25, fetched through `GET /api/market-data/bars` into the operator's bar store. Both sides read the same stored bars.
- **MCC side:** `GET /api/market-data/replay?feed=iex` at every minute close (opening hold = the operator setting, 15 min). READY = `decision.entryAllowed`.
- **Study side:** a Python port of the V14.4.12 green lane ([study_port.py](OneMinute-Entry-Parity-20261125/study_port.py)): the 1M study's own 5M reconstruction, fresh / in-trend / continuation / confirmation trigger events, rearm, cooldown, the 2-bar execution window, invalidation, the 2.00 ATR extension block and the opening hold. READY = `finalExecutionReady`. The AGG. BUY lane is not ported.
- **Indicator check:** the port's 1M ATR extension equals MCC's on all 1,624 matched minutes where MCC reports it (MCC omits the evaluation block on READY minutes) (maximum difference 0.0000). VWAP, ATR and the close series are therefore identical on both sides, so the differences below come from rules, not data.
- Scripts and the raw disagreement list: [OneMinute-Entry-Parity-20261125/](OneMinute-Entry-Parity-20261125/).

## Result

| | Minutes |
|---|---|
| MCC READY | 35 |
| Study READY | 32 |
| Both READY | **14** |
| MCC only | 21 |
| Study only | 18 |

All disagreements are on 2026-11-24. Neither side gave READY on 2026-11-25 before 12:40 ET.

### Why MCC said READY and the study did not (21 minutes)

| Cause | Minutes | Rule |
|---|---|---|
| Trigger window | 14 | The study needs a new trigger event, and READY lasts at most 2 bars. MCC stays READY while its conditions hold. |
| Extension 2.00–2.25 ATR | 6 | The study blocks at 2.00 ATR above VWAP (`extensionDangerATR`); MCC blocks at 2.25 (`AtrExtensionHardExit`). |
| Opening-hold boundary | 1 | PLTR 07:44 AZ (09:44 ET bar). MCC judges the hold by evaluation time, so the 09:44 bar's close at 09:45 is released. The study judges it by bar start, so that bar is held. On the wall clock both release at 09:45 ET; MCC acts on the 09:44 bar, the study on the 09:45 bar. |

### Why the study said READY and MCC did not (18 minutes)

| Cause | Minutes | Rule |
|---|---|---|
| 5M permission source | 9 | The 1M study does **not** read the 5M study's permission latch. It rebuilds its own 5M permission (`fivePermissionState`, set on EMA9 > EMA20, close > EMA9, RSI ≥ 50). MCC uses the port of the **5M study's** latch (`FiveMinutePermissionLatch`), which read NO. On Thinkorswim this means the 1M chart can show READY while the 5M chart shows PERMISSION NO. |
| MCC 5M structure | 7 | MCC also requires the last completed 5m bar to close above VWAP and EMA9 with EMA9 > EMA20 (`BarSnapshot.BullStructure`). The study has no such current-bar 5M check once permission is held. |
| MCC 1M structure | 2 | MCC requires 1M close > EMA9 and EMA9 > EMA20 on every READY bar. The study's invalidation only checks EMA20, VWAP, RSI < 45, MACD and extension. |

Not observed in this sample: RSI ≥ 78 (MCC-only block; TSLA peaked at 76.2) and 1M RVOL < 1.00 (study; it only gates the continuation path).

### Every disagreement with V14.4.12 (AZ time, 2026-11-24)

| Symbol | AZ | Side | Close | Ext ATR | RSI | Cause(s) |
|---|---|---|---|---|---|---|
| NVDA | 10:32 | MCC only | 153.63 | 1.53 | 61.7 | Trigger window |
| NVDA | 10:33 | MCC only | 153.84 | 2.06 | 64.2 | Extension; trigger window |
| NVDA | 10:34 | MCC only | 153.70 | 1.73 | 61.4 | Trigger window |
| NVDA | 10:58 | study only | 153.69 | 1.87 | 61.4 | MCC 5M structure |
| NVDA | 11:00 | MCC only | 153.47 | 1.30 | 55.7 | Trigger window |
| NVDA | 11:01 | MCC only | 153.49 | 1.32 | 56.1 | Trigger window |
| NVDA | 11:16 | study only | 153.285 | 0.64 | 55.1 | MCC 1M structure |
| NVDA | 11:17 | study only | 153.16 | 0.19 | 52.1 | MCC 1M structure |
| NVDA | 11:22 | study only | 153.12 | 0.06 | 51.7 | MCC 5M structure; MCC 1M structure |
| NVDA | 11:23 | study only | 153.37 | 1.09 | 57.4 | MCC 5M structure |
| NVDA | 11:24 | MCC only | 153.34 | 1.01 | 56.5 | Trigger window |
| NVDA | 11:27 | MCC only | 153.405 | 1.33 | 55.8 | Trigger window |
| NVDA | 11:28 | MCC only | 153.335 | 1.01 | 53.9 | Trigger window |
| NVDA | 11:31 | MCC only | 153.635 | 2.20 | 58.3 | Extension; trigger window |
| NVDA | 12:15 | study only | 153.275 | 0.57 | 53.2 | MCC 5M structure; MCC 1M structure |
| NVDA | 12:16 | study only | 153.395 | 1.04 | 56.4 | MCC 5M structure; MCC 1M structure |
| NVDA | 12:18 | study only | 153.39 | 1.01 | 55.9 | MCC 5M structure |
| NVDA | 12:20 | MCC only | 153.555 | 1.85 | 60.4 | Trigger window |
| NVDA | 12:23 | MCC only | 153.30 | 0.76 | 51.4 | Trigger window |
| NVDA | 12:26 | MCC only | 153.35 | 1.01 | 52.6 | Trigger window |
| NVDA | 13:46 | study only | 153.435 | 1.63 | 64.2 | 5M permission source |
| TSLA | 07:50 | study only | 387.96 | 1.36 | 57.3 | 5M permission source |
| TSLA | 07:51 | study only | 387.91 | 1.47 | 57.1 | 5M permission source |
| TSLA | 07:52 | study only | 388.145 | 1.67 | 57.9 | 5M permission source |
| TSLA | 07:53 | study only | 387.57 | 1.23 | 55.1 | 5M permission source |
| TSLA | 07:57 | MCC only | 388.555 | 2.23 | 58.9 | Extension |
| TSLA | 07:58 | MCC only | 388.17 | 2.03 | 56.6 | Extension; trigger window |
| TSLA | 10:16 | study only | 381.81 | 1.68 | 75.3 | 5M permission source |
| TSLA | 10:17 | study only | 382.00 | 1.98 | 76.2 | 5M permission source |
| TSLA | 10:19 | MCC only | 381.88 | 1.92 | 73.3 | Trigger window |
| TSLA | 10:21 | MCC only | 381.985 | 2.02 | 72.9 | Extension; trigger window |
| TSLA | 10:22 | MCC only | 381.79 | 1.82 | 69.4 | Trigger window |
| PLTR | 07:44 | MCC only | 161.46 | 1.45 | 59.2 | Opening-hold boundary |
| PLTR | 07:51 | MCC only | 161.97 | 1.95 | 57.2 | Trigger window |
| PLTR | 08:04 | MCC only | 161.975 | 2.03 | 57.3 | Extension |
| PLTR | 09:25 | study only | 160.895 | 0.30 | 62.3 | 5M permission source |
| PLTR | 09:33 | study only | 160.835 | 0.15 | 59.9 | 5M permission source |
| PLTR | 09:43 | study only | 161.00 | 0.74 | 59.7 | MCC 5M structure |
| PLTR | 09:45 | MCC only | 161.23 | 1.52 | 63.0 | Trigger window |

## Decision 1 applied: 1M Main Study V14.4.13 (2026-11-25)

Operator decision 2026-11-25: **the 5M study latch gates the 1M READY.** V14.4.13 (`docs/Charts/1M/MainStudy.txt`) replaces the 1M study's own 5M permission with the 5M Main Study V4.1.8 latch, advanced once per completed 5M bar (sampled on the first 1M bar of the next 5M bucket) and reset to NO at each new date. Nothing else changed. V14.4.12 is archived as `docs/archive/Charts/1M/MainStudy_V14.4.12.txt`.

**Latch check:** the port of the V14.4.13 latch equals MCC's `fiveMinutePermissionLatch.permission` on 1,652 of 1,674 replayed minutes. All 22 differences are timing conventions, not rules:

- 14 on the last minute of a 5m bucket (:x4 / :x9): MCC evaluates at the bucket close and already has that 5m bar; the study gets it on the next 1M bar. Same wall-clock moment.
- 8 at 15:55–15:58 ET: the study releases at the 15:55 EOD; MCC's latch releases when the 15:55 5m bar completes (16:00). MCC's V2 EOD already blocks entries from 15:55, so READY is unaffected.

**READY replay with V14.4.13:**

| | V14.4.12 | V14.4.13 |
|---|---|---|
| MCC READY | 35 | 35 |
| Study READY | 32 | 20 |
| Both | 14 | 11 |
| Study only: 5M permission source | 9 | **0** |
| Study only: MCC current 5M structure | 7 | 7 |
| Study only: MCC 1M close > EMA9 | 2 | 2 |
| MCC only: trigger window | 14 | 14 |
| MCC only: extension 2.00–2.25 ATR | 6 | 6 |
| MCC only: 5m-close timing (:x4 / :x9) | 0 | 3 |
| MCC only: opening-hold bar boundary | 1 | 1 |

The 3 new MCC-only minutes (TSLA 07:54 and 10:19 AZ, PLTR 09:34 AZ) are the 5m-close timing above. The study can show READY one minute later if its trigger window is still open. The remaining disagreements are decisions 2–4. Per-minute list: [disagreements_v14413.json](OneMinute-Entry-Parity-20261125/disagreements_v14413.json); the V14.4.12 list is [disagreements_v14412.json](OneMinute-Entry-Parity-20261125/disagreements_v14412.json).

## Decision 2 applied: MCC READY is a 2-bar event (2026-11-25)

Operator decisions 2026-11-25: **READY is a 2-bar event**, and MCC uses **the study's own triggers** (operator choice "Port study triggers" over a simpler onset window).

**What changed in MCC:**
- `OneMinuteExecutionEvents` (Core) ports the V14.4.13 green lane: FRESH (10-bar breakout, VWAP/EMA9 reclaim, EMA9/VWAP retest), IN_TREND, CONTINUATION (pause, resume, cooldown) and CONFIRMATION triggers, candidate rearm after a 2-bar pause, the 2-bar window and the study's invalidation. `FiveMinuteExecutionContext` ports the 1M study's 5M inputs (latch permission, major failure, confirmed weakness, runner structure, swing zone) on completed 5m bars.
- `LiveSymbolTechnicalCalculator.ExecutionEventSeries` runs it over every 1m bar in the live assessment and the replay. `V2SignalEngine` gives EXECUTION READY only when MCC's own conditions pass **and** the latest step is READY. Otherwise it returns PREPARE TO BUY with `ExecutionWindowWithheld = true`, and the Dashboard shows **PREPARE TO BUY · WAIT FOR 1M TRIGGER**. Exits and held positions are unchanged.
- Extension: the window uses MCC's 2.25 ATR until decision 3.

**Port check:** a harness ([harness/Program.cs](OneMinute-Entry-Parity-20261125/harness/Program.cs)) ran the new `HistoricalDecisionReplayService` at every minute on the same stored bars, copied from the running MCC. The C# window step equals the Python port (MCC timing: completed 5m bars, 2.25 ATR) on **all 1,674 minutes**. New MCC READY is 19 minutes (old 35); it is never READY on a minute where old MCC was not.

**Against the TOS model** (Python port with Thinkorswim's developing 5m bar and 2.00 ATR):

| | Old MCC vs V14.4.12 | Old MCC vs V14.4.13 | **New MCC vs V14.4.13** |
|---|---|---|---|
| Both READY | 14 | 11 | **11** |
| MCC only: trigger window | 14 | 14 | **0** |
| MCC only: extension 2.00–2.25 ATR | 6 | 6 | 3 |
| MCC only: 5m timing (completed vs developing bar) | 0 | 3 | 4 |
| MCC only: opening-hold bar boundary | 1 | 1 | 1 |
| Study only: 5M permission source | 9 | 0 | 0 |
| Study only: MCC current 5M structure | 7 | 7 | 7 |
| Study only: MCC 1M close > EMA9 | 2 | 2 | 2 |

What is left is decision 3 (3 minutes), decision 4 (9 minutes) and timing conventions (5 minutes). Per-minute list: [disagreements_mcc_window.json](OneMinute-Entry-Parity-20261125/disagreements_mcc_window.json).

## Decision 3 applied: entry extension block 2.00 ATR (2026-11-25)

Operator decision 2026-11-25: **2.00 ATR (the study's `extensionDangerATR`)**. `V2Config.AtrExtensionEntryBlock = 2.00` blocks a fresh entry, and the execution window uses the same value. A stock that passes everything else reads PREPARE TO BUY with "Entry conditions met, but 1M is x.xx ATR from VWAP (entry block 2.00)" (`EntryExtended`). Held positions still SELL only at **2.25** (`AtrExtensionHardExit`), so exits did not change. 1 test.

Harness replay on the same bars: the C# window step still equals the Python port (MCC timing, 2.00) on all 1,674 minutes. MCC READY 15 minutes. Against the TOS model: both **11**, extension disagreements **3 → 0**. Left: 5m timing 3, opening-hold bar boundary 1, MCC current 5M structure 7, MCC 1M close > EMA9 2 (decision 4). List: [disagreements_mcc_ext200.json](OneMinute-Entry-Parity-20261125/disagreements_mcc_ext200.json).

## Decisions still needed (operator)

1. ~~Which 5M permission gates the 1M READY?~~ **Decided 2026-11-25: the 5M study latch** (V14.4.13 above).
2. ~~READY as an event or a state?~~ **Decided 2026-11-25: a 2-bar event with the study's triggers** (above).
3. ~~Extension block: 2.00 or 2.25 ATR?~~ **Decided 2026-11-25: 2.00 ATR for entries** (above); held exit stays 2.25.
4. **MCC-only checks** (current 5M structure, 1M close > EMA9, RSI 78 cap): keep in MCC, add to the study, or drop.

## Limits

- IEX bars, not the consolidated data Thinkorswim uses, so these are not the exact bars on the operator's chart. The comparison isolates rules, not data; TOS VWAP also includes pre-market (extended hours ON), which MCC and this port do not.
- The 1M study's 5M series is modelled as the developing 5m bar (built from the 1m bars so far) with offsets on completed 5m bars. How Thinkorswim evaluates `[1]` and `rec` across a secondary aggregation is assumed, not confirmed. On historical bars Thinkorswim may display the completed 5m bar instead, so the chart's past bubbles can differ from what it showed live.
- Port fidelity is checked only through the indicator match (extension) and by reading the source. It has not been checked against Thinkorswim bubbles. Spot-check suggestion: on the TOS 1M chart for 2026-11-24, look at NVDA 10:32–10:34 AZ, TSLA 07:50–07:53 AZ and TSLA 07:57 AZ.
- One full session and one morning, three liquid symbols, 39 disagreeing minutes. Thin small caps (most Red Zone names) were not tested; MCC's 1M coverage gate would add more MCC-side withholds there.
