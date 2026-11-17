# New chat handoff: 2026-11-17

Branch `feature/opportunity-quality-classification`. Background up to last night: [NewChatHandoff-20261116.md](NewChatHandoff-20261116.md). Checklist: [Master-To-Do.md](Master-To-Do.md). Daily routine: [MCC-Operator-Guide.md](MCC-Operator-Guide.md).

Today is US standard time: regular session 07:30–14:00 AZ, Overnight buy window 13:00–14:00 AZ, late window to 15:00 AZ.

## State at 09:10 AZ

- **Running MCC:** API SHA `91b9178`. The Dashboard files are served from the working tree (`frontend/`), so frontend commits apply on Ctrl+F5 without a restart. API changes still need `MCC.ps1 stop` → `Get-Latest-And-Test.ps1` → `MCC.ps1 start`.
- **Market chip:** **RISK-OFF** at 08:35 AZ (operator); the API agreed at 09:01 AZ.
- **2.a automatic scan:** `COMPLETED` (operator). The status word is now a colour chip, and **Sources, feeds, and limits** is bold (`510ac1f`).
- **Overnight commits since the last handoff:**
  - `971ece3` market chip;
  - `4b5dd6f` Exclusions dock button re-applied after operator commit `528aa1d`;
  - `a362146` compact qualification heading;
  - `007ea25` the update script stops early when MCC is running.
  - The operator set the header title to 35 px (`67e5bf0`).

## SLS parity finding (TOS 5M permission YES, MCC NO)

Details: [Validation/SLS-Parity-20261117.md](Validation/SLS-Parity-20261117.md).

1. **Permission:** TOS 5M permission is a latch; MCC is a stateless 5-of-6 score. A real definitional gap.
   - Fixed: the latch is ported (operator approved; see the next section).
2. **VWAP:** TOS reads 22.76, MCC 20.31. Likely TOS includes extended hours.
   - Operator to check the chart setting.
   - Then decide which session MCC's VWAP should cover.

## 5M permission latch ported (operator approval "Yes, port it")

- **What changed:** MCC's 5M Permission is now the installed TOS study's latch (`FiveMinutePermissionLatch`), used by the live assessment and the replay. The 6-of-6 score is a diagnostic only.
- **Replay on SLS:** YES (HELD) at 08:55 AZ, matching TOS. Details: [Validation/SLS-Parity-20261117.md](Validation/SLS-Parity-20261117.md).
- **Needs:** an MCC restart (API change), then the operator's side-by-side check of the gate line against TOS.
- **Still open:** the VWAP session window (pre-market) and the study's standard-time EOD input (1955 vs 1755).

## Later today

- **TOS extended hours:** ON. IEX pre-market volume is too thin to reproduce TOS's VWAP (SLS 20.47 with pre-market vs TOS 22.76). Operator decision pending: options (a), (b), (c) in the To-Do.
- **SLS spike:** a binary Phase 3 readout. Not predictable from price or volume; the window was public. [Research/SLS-Spike-20261117.md](Research/SLS-Spike-20261117.md). Proposals: a pending-catalyst list and a pre-market gap watch.

## Pending binary-catalyst list (built, operator approval)

- **Where:** a **Catalysts** dock button and modal, and `/api/watchlists/pending-catalysts`. The data file `App_Data/pending-catalysts.json` is git-ignored.
- **Effect:** BLOCKS Boring Overnight READY (`PENDING_BINARY_CATALYST`) and flags Red Zone cards with ⚑.
- **Needs:** an MCC restart.
- **Found on the way:** `exclusions-watchlist.json` is committed, so operator exclusions dirty the tree and block `Get-Latest-And-Test.ps1`. A fix is proposed, waiting on the operator's OK.

## Overnight import without ON columns (12:52 AZ)

- The 12:42 Overnight export had the intraday columns plus V3 Rank, so Boring Top 4 was empty and Fetch did nothing visible. MCC now warns at import and on Fetch; the Operator Guide lists the eight ON columns the import needs.
- A test import by Claude in INTRADAY MOMENTUM appended a fake TOS-vs-MCC comparison line (AFL, J, SB, UNFI); it was removed, and the operator's 10:00 MarketMovers comparison is today's latest again.
- The operator kept `exclusions-watchlist.json` tracked ("Not now"); exclusion edits still dirty the tree.

## READY box checked SPY (~14:05 AZ)

- An empty READY **Symbols** box fell back to the focused symbol (SPY). It now fills from the active Top 4 group, with a **Use Top 4** button. The 14:02 export had `loading` in V3 Rank for 7 rows (CSQR, GTY, CON, HUM, PGR, FOX, ELV): TOS exported before those values finished; the ON columns were complete.

## HIG DATA? and the Daily-open fallback (~14:50 AZ)

- HIG had 19 of 20 close/open pairs: no SIP 09:30 ET minute bar on 2026-10-21. Operator chose "Yes, use Daily open": capture-history now uses the unadjusted SIP Daily open for a missing 09:30 bar, with a 1% close-basis check. HIG could not reach READY before the 15:00 AZ late deadline.
- Sign-off button now reads **Sign off: nothing blocking**.

## CSQR after the deadline (15:01 AZ)

- Not a data gap: CSQR's history was complete. The check ran after the 15:00 AZ late deadline, so it was for 2026-11-18 and the sign-off was refused. The READY card now says so with a banner and hides the sign-off until the buy day.

## Opt-in Overnight panels

- The full qualification table, 2.4, 2.5 and 2.6 are now closed-by-default toggles. The necessary flow is: import (1) → check Boring chips (2.3) → 4 READY check.

## Operator answers recorded today

- **Scans:** no changes to the five intraday TOS scans during the 5-session side-by-side; changing them would void the comparison.
- **Intraday watchlist columns:** unchanged. Symbol, Last, % Change, Volume, VWAP Dist %, 5M Trend, 5M RVOL, 5M Trigger, Ext ATR (`docs/Column Formulas/Intraday/`). These were compiled on 2026-11-16.
- **Style:** Oxford comma in Dashboard text and docs (rule added to `docs/rules/htmlTailwindCssVanillaJavascriptCodingRules.md`).

## New rules this session

- CLAUDE.md: surface operator commits that undo requested work.
- Frontend rules: parse-check `frontend/app.js` after every edit; MCC serves it live.

## Uncertainties

- The VWAP cause is inferred from the study code and the chart shading. The TOS chart setting was not seen.
- The 1m study's 5M labels differ from the 5m study's at the same moment. Not investigated.
