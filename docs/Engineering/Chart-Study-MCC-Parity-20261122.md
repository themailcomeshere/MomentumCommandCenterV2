# Chart study ↔ MCC parity: EOD 15:55 ET, opening hold, overnight status (2026-11-22)

Branch `feature/opportunity-quality-classification`. Code and study commit: `0c96252`. Checklist: [Master-To-Do.md](../Master-To-Do.md) §5.

## Why

On 2026-11-22 the operator asked whether the 5M/1M chart studies needed updating for the latest build. A source comparison against head `0d7f9a1` found four places where Thinkorswim and MCC could show different states for the same bar:

| # | Rule | MCC before this change | Studies before this change |
|---|---|---|---|
| 1 | Opening entry hold (`b05bb98`, `0a2160f`) | EXECUTION READY withheld 09:30–09:45 ET | No hold |
| 2 | Boring Overnight buy window (READY decisions 2026-11-14) | Final regular hour, 15:00–16:00 ET | Repo 1M V14.4.11 only: 12:30–12:55 AZ, which is 14:30–14:55 ET in standard time |
| 3 | Overnight technical gates (decision 10, `d2eebe4`; CLAUDE.md guardrail) | 5M permission/structure are notes; a real 1M SELL blocks | Repo 1M V14.4.11 blocked on 5M permission, VWAP, stack, runner waning, extension and PREPARE TO SELL |
| 4 | EOD | V2 engine EOD GET OUT 15:55 ET; 5M latch 18:55 / 19:55 ET | Installed 18:55 / 19:55 ET; repo copies 18:55 / 17:55 ET |

## Decision (operator, 2026-11-22)

**Option A: the EOD is 15:55 ET in daylight and standard time**, the same as the V2 engine (`V2Config.EndOfDayExitHourEt/MinuteEt`).

- In Arizona this is **12:55 PM in US daylight time** and **1:55 PM in US standard time**.
- This supersedes the earlier intent of a 3:55 PM Arizona cutoff (Thinkorswim-MCC-Extension-Parity-20261102, Full-Master-To-Do-Audit-20261103, NewChatHandoff-20261106). Those documents describe the old target. They are not deleted.
- Consequence: from 15:55 ET the 5M and 1M studies show **EOD: GET OUT**, 5M permission releases, and new entries are blocked (READY/candidate block code 1) for the rest of the Eastern day, including after-hours. Pre-market entries the next day are not affected. MCC already behaved this way.
- Items 1–3 follow existing operator decisions and needed no new decision.

## What changed

### MCC (`0c96252`)

- `FiveMinutePermissionLatch`: `PermissionLatchConfig` EOD defaults are `1555 / 1555` (were `1855 / 1955`). `RulesVersion` = `TOS-5M-PERMISSION-LATCH-2 (5M study V4.1.8, EOD 15:55 ET)`; the live assessment and replay report it.
- Tests: `EodWindowIsFifteenFiftyFiveEastern` (15:50 no, 15:55 yes in both seasons; 08:00 next day no; latch default equals the V2 engine EOD; `RELEASED_EOD` at 15:55) and `InstalledV417EodInputsStillConfigurable`. The old `EodWindowFollowsInstalledStudy` test was replaced.

### 5M Main Study V4.1.8 (`docs/Charts/5M/MainStudy.txt`)

Built from the installed V4.1.7 source (`docs/Validation/Installed-5M-Study.txt`), not from the repository V4.1.7 copy (archived as `docs/archive/Charts/5M/MainStudy_V4.1.7-repo-1755.txt`).

- `eodTimeEasternDaylight = 1555`, `eodTimeEasternStandard = 1555`.
- Time diagnostics show the Arizona HHMM (`eodArizonaHhmm`: 1255 daylight, 1355 standard). Display only.
- No permission, setup, runner, swing or sell rule changed.

### 5M Authority Diagnostic (`docs/Charts/5M/DiagnosticStudy.txt`)

Same EOD and time-diagnostic change, so it agrees with the main 5M study. Nothing else changed.

### 1M Main Study V14.4.12 (`docs/Charts/1M/MainStudy.txt`)

Built from the installed V14.4.10 source (`docs/Validation/Installed-1M-Study.txt`). Repository V14.4.11 was never confirmed installed and is archived as `docs/archive/Charts/1M/MainStudy_V14.4.11.txt`. Not carried forward from V14.4.11: the 12:30–12:55 AZ window, the momentum hard gates on the overnight call, the removal of the intraday EOD GET OUT state, and the debug-bubble line-break changes.

1. **EOD** `1555 / 1555`, Arizona HHMM in the time diagnostics.
2. **Opening entry hold**, input `openingEntryHoldMinutes = 15` (0 = off):
   - `openingEntryHold` = regular session and bar start < `RegularTradingStart` + N minutes. With 15, the 09:44 ET bar is held and the 09:45 ET bar is not, the same boundary as MCC's `CommittedDefaultIsFifteenMinutes` test.
   - When every READY condition is met inside the hold, `finalExecutionReady` is false and the 1M state reads **PREPARE - OPENING HOLD**. Bubble: **OPENING HOLD (READY WITHHELD)**. Label: **OPENING HOLD: 15 MIN - NO NEW ENTRY**.
   - The aggressive lane stays suppressed whenever the normal READY conditions are met. Inside the hold, AGG. BUY is withheld and shows as AGG. PREPARE TO BUY.
   - Diagnostic codes: READY block code **11** = opening hold; AGG block code **20** = AGG. BUY conditions met inside the hold.
   - Exits (PREPARE TO SELL, SELL, EOD GET OUT) are never delayed.
3. **Boring Overnight final-hour status**, input `showOvernightWindowStatus = yes`:
   - `overnightFinalHour` = regular session and bar start ≥ `RegularTradingEnd` − 60 minutes (15:00–16:00 ET on a normal day: 13:00–14:00 AZ in standard time, 12:00–13:00 AZ in daylight time).
   - Label **OVERNIGHT BUY WINDOW: DATA? - CHECK MCC READY** (yellow), or **1M SELL - NO HOLD** (red) when the 1M `sellConfirmed` condition is true. It also shows **5M PERM YES/NO (note)** and, from 15:55 ET, **EOD (note)**.
   - Bubble **OVERNIGHT WINDOW OPEN** on the first bar of the window.
   - It is never green. The study cannot see the 20-pair statistics, catalyst sign-off, exclusions, freshness or the sell limit, so the READY decision stays in MCC (BORING OVERNIGHT → 4 READY check).

## Operator install steps (Thinkorswim desktop)

The repository files are not proof of compilation. Install one study at a time.

1. On GitHub, open `docs/Charts/5M/MainStudy.txt` on branch `feature/opportunity-quality-classification`, click **Raw**, select all and copy.
2. In Thinkorswim, open the 5-minute chart. Click **Studies** → **Edit studies…**. In the right-hand **Added studies** list, find your 5M Command Center study. Click the scroll icon next to it to open its source.
3. Select all the existing source, paste the new source and click **OK**. If Thinkorswim shows a compile error, stop and send Claude the full error text and the line number.
4. Confirm the header reads **V4.1.8** and the inputs show `eodTimeEasternDaylight = 1555` and `eodTimeEasternStandard = 1555`. If the inputs panel still shows 1855 / 1955, the old saved input values override the defaults; type 1555 in both.
5. Repeat steps 1–4 for `docs/Charts/5M/DiagnosticStudy.txt` on the same 5-minute chart, if you use the 5M Authority Diagnostic.
6. Repeat steps 1–3 for `docs/Charts/1M/MainStudy.txt` on the 1-minute chart. Confirm the header reads **V14.4.12**, the inputs show `eodTimeEasternDaylight = 1555`, `eodTimeEasternStandard = 1555`, `openingEntryHoldMinutes = 15` and `showOvernightWindowStatus = yes`.
7. Click **OK** / **Apply** and save the chart style if you use one.

## Operator validation, Monday 2026-11-23 (US standard time: regular session 07:30–14:00 AZ, 09:30–16:00 ET)

Keep MCC running in WORKSPACE = **INTRADAY MOMENTUM**, **4 Red Zone**, with **Opening hold** = **15 min (default)**.

1. **07:30–07:44 AZ.** 1M chart: the label **OPENING HOLD: 15 MIN - NO NEW ENTRY** shows; no **EXECUTION READY** or **AGG. BUY** bubble appears on any bar starting before 07:45 AZ. MCC Red Zone: qualifying cards read **PREPARE TO BUY · OPENING HOLD**. Record any symbol where one side shows READY and the other doesn't, with the AZ bar time.
2. **07:45 AZ onward.** The hold label disappears. READY can appear on both sides.
3. **13:00 AZ.** 1M chart: **OVERNIGHT WINDOW OPEN** bubble and the yellow **OVERNIGHT BUY WINDOW: DATA? - CHECK MCC READY** label on any symbol open in the chart.
4. **13:55 AZ.** 5M and 1M charts: **EOD: GET OUT**; 5M PERMISSION reads NO. MCC Red Zone card gate line: **5M Permission: NO · EOD exit window**. Record any mismatch with symbol, AZ time and both values.
5. Optional: set `showDebugLabels = yes` and `showTimeDiagnostics = yes` on the 1M study. The label reads `TIME AZ EOD=1355 ET=1555 DST=no`.

## Not verified

- Thinkorswim compilation of any of the three files: Data not found in sources.
- Whether Thinkorswim's `RegularTradingEnd` reflects early-close days (for example the day after Thanksgiving, 2026-11-27): Data not found in sources. If it does not, the overnight label will open at 15:00 ET on an early-close day, after the 13:00 ET close. MCC's `MarketSessionClock` handles early closes separately. The study's EOD is fixed at 15:55 ET and does not move on an early close, matching the V2 engine.
- The 1M execution window keeps ageing during the hold. A trigger in the first minutes can expire before 09:45 ET, so the chart may show **EXECUTION EXPIRED** at release where MCC re-evaluates its own conditions. Matched-bar evidence: Data not found in sources.
- CI result for `0c96252`: recorded in the Master To-Do once the run finishes.
