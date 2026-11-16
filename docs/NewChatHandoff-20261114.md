# New Chat Handoff — 2026-11-14

**Branch:** `feature/opportunity-quality-classification` (never `master`)
**Location (moved 2026-11-16):** repository `D:\source\repos\MomentumCommandCenterV2`; backups `D:\source\repos\MCC-Backups`; catalyst review sheets `D:\source\repos\MCC-Reviews`. MCC runtime data stays in `%LOCALAPPDATA%\MomentumCommandCenterV2`.
**Checklist:** [Master-To-Do.md](Master-To-Do.md), the single authoritative list
**Rules:** [CLAUDE.md](../CLAUDE.md). The SIP rule was amended 2026-11-14: delayed historical SIP for completed Daily bars and for the GTC_EXT sell-window and 20-pair close/open 1m history. Live, stream and recent data stay on IEX.
**Decision record:** [Boring-Overnight-READY-Semantics-20261114.md](Engineering/Boring-Overnight-READY-Semantics-20261114.md)
**Previous handoff:** [NewChatHandoff-20261113.md](NewChatHandoff-20261113.md) (launcher, warm-up, Dashboard checks 4a–4e in full detail)

## State at hand-off

| Item | Value |
|---|---|
| Latest code head | `6fe6446` (close/open history on SIP), CI run `43646185327` SUCCESS; local Release 398/398 (Claude) |
| Previous heads (CI) | `02e88a3` READY panel `43644529920` · `b3ca453` Daily tolerance `43642242199` · `0b4fbc1` NO_TRADE `43636822164` · `e48b4d2` SIP 1m `43636134528` · `0596df6` catalyst queue `43635497154` · `5f1d5d1` SIP Daily `43634828257` · `6a547c3` DATA? reasons `43630951781` |
| Last operator-verified head | `4c743a1`, 302/302 |
| Dashboard cache keys | `app.js?v=20261114-ready-signal-v1`, `styles.css?v=20261114-ready-signal-v1` |
| Session times (standard time, Nov–Mar) | Regular 07:30–14:00 Arizona; buy window 13:00–14:00 AZ; GTC_EXT sell window 05:00–18:00 AZ |
| READY mode | Shadow (`BoringOvernight:ReadySignalMode=shadow`); 0/10 shadow sessions logged |

## What changed on 2026-11-14

| Commit | Change |
|---|---|
| `6a547c3` | DATA? reasons: `DataStale` / `OutsideRegularSession` instead of always `DataMissing`. `MCC.ps1 warmup`. |
| `5f1d5d1` | `DailyFeed=sip_delayed`: SIP Daily bars with end ≥ 16 min old. A refusal falls back to IEX, labelled, never usable. Massive Daily client and `GET /api/market-data/daily/cross-check`. |
| `0596df6` | `catalyst-assessment?lookbackHours=`; `New-CatalystReviewQueue.ps1` (stratified sheet); `Submit-CatalystReviews.ps1` (dry run, `-Submit`). |
| `e48b4d2` | Delayed historical SIP 1m bars for the GTC_EXT sell-window history. |
| `0b4fbc1` | NO_TRADE minutes: on SIP, a no-bar minute is counted, never priced, when the reference close and regular-session bars are present. RTO sell history is 40/40. |
| `b3ca453` | Daily parity tolerance ±$0.01 / ±0.5%; a fresh reference download confirms 18/18 sessions. |
| `4eafc9b` | Sell limit: Thinkorswim ATR gates are informational (`Required=false`); the limit never depended on ATR. |
| `8365f7e` | READY backend: evaluator, catalyst gate, sign-off store, shadow log with Day 2 outcomes; endpoints `ready/evaluate`, `ready/shadow-log`, `catalyst-attestations`. |
| `02e88a3` | Dashboard **READY check** panel (Overnight workspace): capture, evaluate, gates, sign-off form, auto re-check, sound in live mode only, shadow log table. |
| `6fe6446` | 20-pair close/open history on delayed SIP; live 1M/5M stays on IEX. RTO and CRBG are 20/20 (they were 14/20 and 13/20 on IEX). Fix: `currentOvernightPct` is now filled from the latest completed pair. |
| docs | Decision record, [Catalyst sample plan](Validation/Catalyst-Sample-Plan-20261114.md), evidence in `artifacts/daily-data-qualification/20261114T195621Z-sip-delayed/` and `artifacts/gtc-ext-sell-window/20261114T201053Z-sip-delayed/`. |

### Operator decisions (all recorded in the decision record)

- Buy any time in the final regular hour. Exit: GTC_EXT limit sell measured on Day 2, 07:00–20:00 ET; the order carries over if unfilled. Opening auction print not required.
- Data: delayed historical SIP for Daily, sell-window history and close/open history; Massive free as an optional cross-check. Daily tolerance ±$0.01 / ±0.5%. NO_TRADE minutes accepted with safeguards.
- Catalyst:
  - Earnings/guidance, FDA and unknown-type events block.
  - M&A, contract and analyst events pass with sign-off and a warning.
  - Risk articles block.
  - Sign-off is by ticking the sources checked. It shows as OPERATOR-ATTESTED, never CLEAR.
- READY is a clear go signal. It runs in shadow mode until the operator switches it to live, with a minimum of 10 logged sessions. The limit is the latest trade (at most 2 min old) × the tested target %. Position size is the operator's decision.

### Key findings

- **Rank button:** it exists only in **INTRADAY MOMENTUM** (`styles.css:1728` hides it in Boring Overnight).
- **Delayed SIP works on the free account** for Daily and for 1m history. SIP Daily volume is within 0.021% of the independent reference.
- **Live result, 2026-11-14 (Saturday).** Both RTO and CRBG have complete SIP history, but both fail real rules:
  - **RTO:** 5D +0.12% is not above 20D +0.44% (win rate 60%).
  - **CRBG:** 5D +0.68% is not above 20D +0.72% (win rate 80%).
  - Both also fail the walk-forward test.
  - The live 1M/5M inputs are empty until the session opens.
- **Session-time correction:** in November the regular session is 07:30–14:00 AZ, not 06:30–13:00.

## Update 2026-11-16 — operator walkthrough

- The operator's **daily core is INTRADAY MOMENTUM from 07:30 AZ**: TOS scanners 1, 2A, 2B, Momentum Pivot and D → Watchlist → charts → act on states in Schwab. The goal is for MCC to scan automatically, keep a 1–4 symbol Red Zone, and show colour-coded states. Boring Overnight is the secondary, nightly workflow.
- Added: step-number badges in both modes, colour-coded ranking reasons, and [MCC-Workflow-Steps.md](Engineering/MCC-Workflow-Steps.md), a numbered map that lists the layout problems and gaps with a proposed order of work.
- Key gaps: no TOS CSV import in Intraday mode; layout order runs backwards; states are not coloured like the TOS studies; scanners not implemented; live data is IEX only.
- Later on 2026-11-16: shared TOS import bar and reordered layouts (`037a9ce`); Red Zone state colours, HOLDING toggle and transition sounds (`8a27531`). The live engine had always run with `hasPosition=false`, so sell states never showed; `heldPositionDecision` fixes that for cards marked HOLDING. Automatic scanner on free data (`a942074`, Intraday step 2.a; TOS import renumbered 2.b): [MCC-Auto-Scanner-20261116.md](Engineering/MCC-Auto-Scanner-20261116.md). Cache keys `?v=20261116-auto-scan-v1`.
- V3 Boring Overnight Thinkorswim columns (`bb8db35`, CI `43933040879`): V3 Edge, V3 Boringness, V3 Status, C# reference and `GET /api/boring-overnight/v3-column`. Not compiled in Thinkorswim yet: [Boring-Overnight-V3-Columns-20261116.md](Engineering/Boring-Overnight-V3-Columns-20261116.md).
- READY decisions 10–12 (`d2eebe4`, CI `43962877084`): 5M momentum informational, LIMIT UNTESTED +0.5% heuristic, late buy window to 15:00 AZ (standard time) with the after-hours ≤ regular-close guard. Restart MCC to use them. Cache keys `?v=20261116-ready-late-v1`.
- Operator Step 3 result: the exclusion test passed (the `EXCLUDED_WATCHLIST` reason appeared, and disappeared after removal).

## Operator sequence

**Weekend**
1. Update and start:
   ```powershell
   .\scripts\Get-Latest-And-Test.ps1
   .\scripts\MCC.ps1 start
   .\scripts\MCC.ps1 warmup      # about 15 s after start
   ```
   Report the SHA, the test totals and the warmup output.
2. **Ranking and exclusion:** open `http://localhost:53360/`, press Ctrl+F5, and set the left WORKSPACE switch to **INTRADAY MOMENTUM**.
   - In the Red Zone header, click **Rank quality pool · top 50**. After hours every card is DATA? with `DataStale, OutsideRegularSession`.
   - Exclusion test: in **EXCLUSIONS**, add one ranked symbol with a reason. Rank again: the card must read `QUALITY — EXCLUDED`. Remove the symbol.
3. **Daily audit:** switch to BORING OVERNIGHT and run the Daily data audit for `RTO,EVR,CRBG`. Each row must show `Alpaca / sip · delayed historical SIP`, 20/20, FRESH and `QUALIFICATION USABLE`. Take a screenshot.
4. **Catalyst batch 1:** open `D:\source\repos\MCC-Reviews\catalyst-queue-20261114-200253.csv` in Excel. Label the rows you review (Label + Notes of at least 40 characters of article facts), save as CSV, then:
   ```powershell
   .\scripts\Submit-CatalystReviews.ps1 -Path "D:\source\repos\MCC-Reviews\catalyst-queue-20261114-200253.csv"
   .\scripts\Submit-CatalystReviews.ps1 -Path "<same path>" -Submit
   ```
5. **Optional Massive cross-check:** create a free account at massive.com yourself, then:
   ```powershell
   dotnet user-secrets set "MarketData:Massive:ApiKey" "<your key>" --project src\MomentumCommandCenterV2.API
   ```
   Restart MCC and open `http://localhost:53360/api/market-data/daily/cross-check?symbol=RTO`.
6. **Stop, backup, sign-out:**
   ```powershell
   .\scripts\MCC.ps1 stop
   .\scripts\Backup-MccData.ps1
   .\scripts\Restore-MccData.ps1 -BackupPath "<folder printed>"
   ```
   Then sign out of Windows and back in, run `MCC.ps1 status` (expect NOT RUNNING), then `start`.
7. **Release evidence:** run `.\scripts\New-ReleaseEvidence.ps1 -BackupManifest "<backup>\manifest.json"`, then commit and push the two files it writes.

**Monday, regular session (07:30–14:00 AZ)**
8. Ranking in INTRADAY MOMENTUM: cards should classify instead of DATA?.
9. **READY check (13:00–14:00 AZ).** Use the BORING OVERNIGHT workspace; the **READY check** panel is near the top.
   - Enter your candidates (comma-separated) and click **Check READY now**.
   - Read each card. For a symbol you want, do your news check, tick the sources, and click **Sign off: nothing material**.
   - Tick **Re-check every 2 min**.
   - `READY (SHADOW — not yet validated)` appears only when every gate passes. Shadow sessions start counting that day.
10. CRBG live assessment and **Fetch / retry 20-session close/open history**: report the fields from the [2026-11-13 handoff](NewChatHandoff-20261113.md) step 5, plus the capture-history POST in Network.

## Open blockers

- READY works end to end. Candidates must still pass every rule; RTO and CRBG currently fail 5D > 20D and the walk-forward test.
- TOS compile and parity are not evidenced (no longer needed for the limit).
- Catalyst sample too small (7 records, no negatives).
- Schwab and broker reconciliation parked.

## Uncertainties

- Delayed SIP behaviour shortly after the close (the 16-minute cutoff) is covered by tests, not yet observed live on a weekday.
- The live trade price used for the limit comes from IEX and may differ slightly from the consolidated price you trade at.
- The Massive client is tested with fixtures only; no key is configured.
- Alpaca's terms for storing and displaying SIP data on the free plan were not reviewed (Data not found in sources).
