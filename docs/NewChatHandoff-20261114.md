# New Chat Handoff — 2026-11-14

**Branch:** `feature/opportunity-quality-classification` (never `master`)
**Checklist:** [Master-To-Do.md](Master-To-Do.md), the single authoritative list
**Rules:** [CLAUDE.md](../CLAUDE.md). The SIP rule was amended 2026-11-14: delayed historical SIP for completed Daily bars and for the GTC_EXT sell-window 1m history.
**Previous handoff:** [NewChatHandoff-20261113.md](NewChatHandoff-20261113.md) (launcher, warm-up, Dashboard checks 4a–4e in full detail)

## State at hand-off

| Item | Value |
|---|---|
| Latest code head | `02e88a3` (READY panel), CI run `43644529920` SUCCESS; local Release 392/392 (Claude). Previous: `b3ca453` Daily tolerance, CI `43642242199`; `0b4fbc1` NO_TRADE, CI `43636822164` |
| Previous heads | `0596df6` catalyst queue, CI `43635497154`; `5f1d5d1` delayed-SIP Daily, CI `43634828257`; `6a547c3` DATA? reasons, CI `43630951781` |
| Last operator-verified head | `4c743a1`, 302/302 |
| Dashboard cache keys | `app.js?v=20261114-ready-signal-v1`, `styles.css?v=20261114-ready-signal-v1` |
| Session times (standard time, Nov–Mar) | Regular 07:30–14:00 Arizona; buy window 13:00–14:00 AZ; GTC_EXT sell window 05:00–18:00 AZ |

## What changed on 2026-11-14

| Commit | Change |
|---|---|
| `6a547c3` | DATA? reasons: `DataStale` / `OutsideRegularSession` instead of always `DataMissing`. `MCC.ps1 warmup`. |
| `5f1d5d1` | `DailyFeed=sip_delayed`: SIP Daily bars with end ≥ 16 min old. A refusal falls back to IEX, labelled, never usable. Massive Daily client, `DailySourceCrossCheck`, `GET /api/market-data/daily/cross-check`. The Daily audit shows the SIP status. |
| `e48b4d2` | Delayed historical SIP 1m bars for the GTC_EXT sell-window history (operator approved). RTO: 15,201 SIP bars, 0/40 complete windows at first, because no-trade minutes have no bar. |
| `0b4fbc1` | NO_TRADE minutes (operator approved). On SIP, a no-bar minute is counted, never priced, when the reference close and regular-session bars are present. RTO is 40/40 complete. Sell limit still BLOCKED: TOS ATR gates and walk-forward adverse excursion. |
| `b3ca453` | Daily parity tolerance ±$0.01 / ±0.5% (operator approved); a fresh reference download confirms 18/18 sessions. | `catalyst-assessment?lookbackHours=`; `New-CatalystReviewQueue.ps1` (stratified sheet); `Submit-CatalystReviews.ps1` (dry run, `-Submit`). |
| docs | [READY semantics](Engineering/Boring-Overnight-READY-Semantics-20261114.md), [Catalyst sample plan](Validation/Catalyst-Sample-Plan-20261114.md), SIP evidence `artifacts/daily-data-qualification/20261114T195621Z-sip-delayed/`. |

### Operator decisions recorded

- Buy: any time in the final regular hour. Exit: GTC_EXT limit sell, Day 2 07:00–20:00 ET. Official opening auction: not required.
- Daily source: delayed historical Alpaca SIP, plus Massive free as the cross-check. Daily tolerance: ±$0.01 / ±0.5%.

### Key findings

- **Rank button not visible:** it only exists in **INTRADAY MOMENTUM** (`styles.css:1728` hides it in Boring Overnight). The 2026-11-13 handoff sent the operator to Boring Overnight first.
- **Delayed SIP Daily works on the free account.** RTO, EVR and CRBG: 20/20 `CONSOLIDATED_SIP`, `qualificationUsable=true`. Volume is within 0.11% of the independent reference; IEX was a fraction of it.
- **IEX close/open history gap blocks READY:** RTO 14/20 and CRBG 13/20 pairs. Qualification stays DATA? until that history uses SIP, which needs operator approval. earlier messages said "06:30–13:00 AZ". That is the daylight-time mapping. In November it is 07:30–14:00 AZ.

## Operator sequence

**Weekend**
1. Decide whether the 20-pair close/open history may use delayed SIP (the "Open question" at the end of the [READY semantics](Engineering/Boring-Overnight-READY-Semantics-20261114.md) record).
2. Update and start:
   ```powershell
   .\scripts\Get-Latest-And-Test.ps1
   .\scripts\MCC.ps1 start
   .\scripts\MCC.ps1 warmup      # about 15 s after start
   ```
   Report the SHA, the test totals and the warmup output.
3. **Ranking and exclusion:** open `http://localhost:53360/`, press Ctrl+F5, and set the left WORKSPACE switch to **INTRADAY MOMENTUM**.
   - In the Red Zone header, click **Rank quality pool · top 50**. After hours every card is DATA? with `DataStale, OutsideRegularSession`.
   - Exclusion test: in **EXCLUSIONS**, add one ranked symbol with a reason of at least a few words. Rank again: the card must read `QUALITY — EXCLUDED`. Remove the symbol.
4. **Daily audit:** switch to BORING OVERNIGHT and run the Daily data audit for `RTO,EVR,CRBG`. Each row must show `Alpaca / sip · delayed historical SIP`, 20/20, FRESH and `QUALIFICATION USABLE`. Take a screenshot.
5. **Catalyst batch 1:** open `Documents\MCC-Reviews\catalyst-queue-20261114-200253.csv` in Excel. For each row you want to label, open the Url and read the article, then fill Label and Notes (≥ 40 characters of article facts). Save as CSV, then:
   ```powershell
   .\scripts\Submit-CatalystReviews.ps1 -Path "$env:USERPROFILE\OneDrive\Documentos\MCC-Reviews\catalyst-queue-20261114-200253.csv"
   .\scripts\Submit-CatalystReviews.ps1 -Path "<same path>" -Submit
   ```
6. **Optional Massive cross-check:** create a free account at massive.com (you, not Claude). Then:
   ```powershell
   dotnet user-secrets set "MarketData:Massive:ApiKey" "<your key>" --project src\MomentumCommandCenterV2.API
   ```
   Restart MCC and open `http://localhost:53360/api/market-data/daily/cross-check?symbol=RTO`. Report `verdict` and `statusCounts`. Massive publishes a session only after midnight ET, so the latest session may show MISSING_REFERENCE.
7. **Stop, backup and sign-out:**
   ```powershell
   .\scripts\MCC.ps1 stop
   .\scripts\Backup-MccData.ps1
   .\scripts\Restore-MccData.ps1 -BackupPath "<folder printed>"
   ```
   Then sign out of Windows and back in, run `MCC.ps1 status` (expect NOT RUNNING), then `start`.
8. **Release evidence:** run `.\scripts\New-ReleaseEvidence.ps1 -BackupManifest "<backup>\manifest.json"`, then commit and push the two files it writes.

**Monday, regular session (07:30–14:00 AZ)**
9. Ranking in INTRADAY MOMENTUM: cards should classify instead of DATA?.
10. **READY check (13:00–14:00 AZ):** BORING OVERNIGHT, **READY check** panel near the top. Enter your candidates, click **Check READY now**, review each card, tick the sources you checked and click **Sign off: nothing material**, then tick **Re-check every 2 min**. Expect `READY (SHADOW — not yet validated)` only if every gate passes. Until the close/open history uses SIP, qualification stays DATA?. and **Fetch / retry 20-session close/open history**. Report the fields listed in the [2026-11-13 handoff](NewChatHandoff-20261113.md) step 5, plus the capture-history POST in Network.

## Open blockers

- READY is built (shadow mode). It cannot fire until the 20-pair close/open history is complete; that needs the SIP decision above.
- Sell limit: Thinkorswim ATR is no longer required. RTO and CRBG fail the walk-forward gates.
- TOS compile and parity not evidenced.
- Catalyst sample too small (7 records, no negatives).
- Schwab and broker reconciliation parked.

## Uncertainties

- Delayed SIP Daily was verified on three symbols on a weekend. Weekday behaviour shortly after the close (the 16-minute cutoff) is covered by tests, not yet observed live.
- The 2026-11-06 close differences against the reference are explained only by capture timing, which is unconfirmed.
- The Massive client is tested with fixtures only; no key is configured.
- Alpaca's terms for storing and displaying SIP Daily data on the free plan were not reviewed (Data not found in sources).
