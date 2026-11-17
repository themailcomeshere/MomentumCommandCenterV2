# New Chat Handoff — 2026-11-16

**Branch:** `feature/opportunity-quality-classification` (never `master`)
**Location:** repository `D:\source\repos\MomentumCommandCenterV2`.
- Backups: `D:\source\repos\MCC-Backups`.
- Catalyst sheets: `D:\source\repos\MCC-Reviews`.
- Old TOS exports and notes: `D:\source\repos\MCC-Documents`.
- Research data: `D:\source\repos\MCC-Data` (outside the repo).

**Checklist:** [Master-To-Do.md](Master-To-Do.md). **Daily use:** [MCC-Operator-Guide.md](MCC-Operator-Guide.md). **Rules:** [CLAUDE.md](../CLAUDE.md) (a momentum-gate guardrail was added today).
**Previous handoff:** [NewChatHandoff-20261114.md](NewChatHandoff-20261114.md) (READY build, SIP decisions, older operator steps).

## State at hand-off

| Item | Value |
|---|---|
| Latest code head | `703bf03`, CI `43977782652` SUCCESS; Claude local 501/501 (Debug; the Release output was locked by the running MCC) |
| Operator's running MCC | `dd80170` (started 14:47 AZ); needs a restart to get `83949cf`, `f693f4e`, `0962645` |
| Dashboard cache keys | `?v=20261116-history-button-v1` |
| READY mode | Shadow; sessions logged from 2026-11-16 |
| Session times (standard time) | Regular 07:30–14:00 AZ; buy 13:00–14:00 AZ; late window 14:00–15:00 AZ; GTC_EXT sell next day 05:00–18:00 AZ |

## What changed on 2026-11-16

| Commit | Change |
|---|---|
| `6b74cdf`, `037a9ce` | Step numbers in both workspaces; shared TOS import; layouts reordered; Intraday Red Zone no longer filtered by the Overnight rules |
| `8a27531` | Red Zone state colours like the TOS labels, transition sounds, **Mark holding** (heldPositionDecision) |
| `a942074` | Automatic scanner on free data (Intraday 2.a), scans 1/2A/2B/PIV/D as written, TOS side-by-side log |
| `f9c8546` | Move to D:, script defaults next to the repo |
| `bb8db35`, `56867ed` | V3 Thinkorswim columns (Edge, Boringness, Status), C# reference, `/api/boring-overnight/v3-column`; helper script removed after TooComplexException |
| `3e6d986` | Price gate: exactly $5.00 passes |
| `d2eebe4` | READY decisions 10–12: 5M momentum informational; LIMIT UNTESTED +0.5% heuristic; late window to close + 60 min with the after-hours ≤ close guard |
| `83949cf` | Late window: operator-typed after-hours price (IEX has no after-hours trades) |
| `f693f4e` | Research: `/api/research/overnight-history` + `scripts/Export-OvernightHistory.ps1` (weekday statistics) |
| `0962645` | READY card: label catalyst articles inline |
| docs | [Operator guide](MCC-Operator-Guide.md), [layout and live-readiness](Engineering/MCC-Layout-and-Live-Readiness-20261116.md), [V3 columns](Engineering/Boring-Overnight-V3-Columns-20261116.md), [auto-scanner](Engineering/MCC-Auto-Scanner-20261116.md), [READY decisions 10–12](Engineering/Boring-Overnight-READY-Semantics-20261114.md) |

## Evidence gathered today

- **Shadow log, 13:18–13:49 AZ** (PRU, AFL, VOYA, MET, 72 checks): QUALIFICATION was blocked only by 5M momentum; every overnight gate passed. This led to decision 10.
- **IEX after-hours:** zero 16:00–17:00 ET 1m bars for PRU, AFL, VOYA, MET and SPY (checked through the live MCC). This led to the typed price in `83949cf`.
- **V3 Edge parity:** MCC's SIP-based Edge score equals the TOS Overnight Score export exactly for PRU 61.23, AFL 60.07, VOYA 56.86, MET 55.97, HQY 56.45.
- **MCC V3 Boringness:** PRU 79.30, AFL 78.35, MET 74.86, HQY 72.79, VOYA 71.33.
- **Late window, 14:51 AZ:** PRU passed QUALIFICATION. VOYA was BLOCKED on 5D not above 20D, a real rule failure.

## Evening update (2026-11-16)

- **Fixes (`1ca8a9d`):**
  - Export script crash (PowerShell 5.1 manifest bug).
  - 2025-01-09 closure added to the calendar (V3).
  - Pre-listing sessions reported separately from missing ones.
  - The Top 4 evidence table shows "none" instead of UNKNOWN for no error.
  - Intraday watchlist columns added (`docs/Column Formulas/Intraday/`).
- **V3:** compiled in Thinkorswim (operator screenshot); parity with MCC on 5 symbols (Edge identical, Boringness within 0.01).
- **History findings:** [Overnight-History-Findings-20261116.md](Research/Overnight-History-Findings-20261116.md).
  - The scanner's edge gates gave a next-night win of 53.4% vs a 49.7% base rate over 2 years.
  - No per-stock weekday effect.
- **Answered:** sort the TOS Overnight scan by V3 Boringness and take V3 Status CANDIDATE rows; the TOS Overnight scanner is still needed; intraday watchlist columns recommended (Operator Guide §C).
- **Open:** Scanner 1's aggregation (the operator runs it manually; read the `D`/`5m` button on each Study row).

## Night update (2026-11-16, `703bf03`, CI `43977782652`)

- **History export button** in the Dashboard header: amber = not run today, green = done today. Server-side export; the script still works and counts toward the status.
- **V3 Rank** plain-number sort column, because TOS greys out labelled columns in "Sorted by".
- **Scanner 1 is 5m** (operator); the **intraday watchlist columns compiled** (operator).
- **Operator plan:** intraday checks tomorrow morning.

## Late-night update (2026-11-16)

- **Market chip** in the header: `GET /api/market/regime`, `MarketRegime.cs`, `App_Data/macro-events.json` (official FOMC/BLS dates), MCC-REGIME-0.1. Awareness only.
- **Operator commit `528aa1d`** restored the Morning Brief page panel and the Exclusions page panel (no dock button). The Morning Brief modal copies the panel again and works with either markup.
- **Resolved:** header title back to 15 px (operator decision); header 51 px with the chip.

## Operator answers recorded today

- Price gate: $5.00 passes.
- READY: all three changes approved ("Yes, all three").
- Weekday history: wanted. Built as research, not a signal.
- The operator plans to edit `index.html` personally; the layout advice is in the assessment doc.

## Next steps

See the To-Do **Next operator actions** (items 1–8) and the [Operator Guide](MCC-Operator-Guide.md).

**Repo work waiting on operator evidence:**
- scanner comparison (5 sessions);
- V3 compile and parity;
- 1M/5M parity exports;
- weekday analysis after the first `Export-OvernightHistory.ps1` runs.

## Uncertainties

- The auto-scanner, the late window with a typed price, and inline labelling have not been exercised by the operator on live data.
- Alpaca's free-plan inclusion of the most-actives screener is unconfirmed.
- Alpaca's terms for storing SIP data are not reviewed, so research data stays outside the repo.
- Weekday effects per stock are expected to be weak. The capture exists to test that, not to assume it.
