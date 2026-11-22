# New chat handoff: 2026-11-21

Branch `feature/opportunity-quality-classification`. Background: [NewChatHandoff-20261117.md](NewChatHandoff-20261117.md). Checklist: [Master-To-Do.md](Master-To-Do.md). Daily routine: [MCC-Operator-Guide.md](MCC-Operator-Guide.md).

Monday 2026-11-23 is US standard time: regular session 07:30–14:00 AZ (09:30–16:00 ET).

## Operator question (Saturday 21:59 AZ)

"Should I wait about an hour after the open before making any INTRADAY trades, because the first hour is volatile and chaotic?"

### Answer given

Hold new entries during the opening window, but as an adjustable, logged hold, not a blanket "no trading" rule. Exits are never held.

- **For a hold:**
  - Published intraday studies find volatility, volume and bid-ask spreads highest near the open (the "U-shaped" pattern).
  - MCC-specific: IEX is a partial feed. The 1M coverage gate (`a31b8a5`) already withholds entries on thin prints, and the open is when IEX is thinnest relative to real volume.
  - The 5M latch resets each Eastern day, and session VWAP starts from the first bars. Early-session indicators therefore rest on very little data.
  - The SLS case (2026-11-17): buyers at the open were underwater for most of the first hour; the latch first granted at 08:40 AZ.
- **Against a full 60 minutes:**
  - Momentum strategies take much of their edge in the first half hour. In Gao, Han, Li & Zhou (JFE 2018), the first half-hour return predicts the last half-hour return.
  - Zarattini, Barbon & Aziz (2024) found a 5-minute opening-range breakout on "stocks in play" profitable net of costs.
  - Waiting 60 minutes gives some of that up, and later entries are more often ATR-extended.
- **Conclusion (first answer):** 60 minutes is a provisional operator choice; superseded at 22:26 AZ by a 15-minute default (below). MCC has no outcome data yet to say whether 0, 15, 30 or 60 minutes is best. The hold records what it withheld, so this can be measured.

## Built (`b05bb98`, CI `44988045554` SUCCESS, 556/556)

- **Core:**
  - `OpeningEntryHold`: window [09:30 ET, +N min), Eastern clock, DST-safe, with the release time in ET and AZ.
  - `V2Config.OpeningEntryHoldMinutes`: engine default 0, so behaviour is unchanged unless a caller passes it.
  - `SignalEvaluationContext.OpeningHoldWithheld`.
- **Engine:** a fresh Buy inside the hold becomes PREPARE TO BUY with the reason "OPENING HOLD: …". Exits and held-position states are unaffected.
- **API:**
  - `IntradaySettingsStore`, saved to `%LOCALAPPDATA%\MomentumCommandCenterV2\intraday-settings.json`; default `MarketData:Intraday:OpeningEntryHoldMinutes` = 60 in `b05bb98`; now 15 (below).
  - `GET/PUT /api/intraday/settings`. A PUT clears the live-assessment cache.
  - The live assessment returns `openingHold`.
  - The replay takes `openingHoldMinutes`.
- **Dashboard (INTRADAY MOMENTUM only):**
  - The **Opening hold** dropdown and status line sit in the 4 Red Zone header and are hidden in BORING OVERNIGHT.
  - Cards and the Main Chart read **PREPARE TO BUY · OPENING HOLD**.
  - `app.js` was parse-checked (`new Function` and `node --check`). The status logic was exercised in Node for each state.

## Not verified

- A browser-pane check of the running Dashboard: no MCC instance runs in Claude's session.
- Local Release tests: no .NET SDK in Claude's session. CI is the only build/test evidence.
- Whether the hold improves outcomes: Data not found in sources.

## Default changed to 15 minutes (22:26 AZ)

The operator asked for the number the peer-reviewed research supports. Claude recommended **15 minutes**; the full evidence is in [Opening-Hold-Evidence-20261121.md](Research/Opening-Hold-Evidence-20261121.md).

- Spreads and quote noise peak in the first minutes and fall quickly on NYSE (McInish & Wood 1992; Chung et al.).
- Pre-open news is largely priced within 5–15 minutes (cited in Busse & Green 2002).
- The first 30 minutes carry the momentum signal (Gao et al. 2018), so 30–60 minutes gives up the signal window.
- MCC mechanics: the 1M coverage window is 15 minutes, and by 09:45 ET three completed 5m bars exist.

`appsettings.json` = 15, and the dropdown labels it **15 min (default)**. New test `CommittedDefaultIsFifteenMinutes`. The "Next operator steps" below now use 15 minutes.

## Next operator steps

1. Before 07:30 AZ Monday, from `D:\source\repos\MomentumCommandCenterV2`: `MCC.ps1 stop`, `.\scripts\Get-Latest-And-Test.ps1` (expect SHA `b05bb98` or later, 556+ tests), `MCC.ps1 start`, Ctrl+F5.
2. WORKSPACE = **INTRADAY MOMENTUM** → **4 Red Zone**: the **Opening hold** bar reads "15 min · today 7:30 AM–7:45 AM AZ (9:30 AM–9:45 AM ET)" (standard time). If the dropdown shows another value, a saved setting overrides the default: pick **15 min (default)**.
3. From 07:30 AZ, same bar: "ACTIVE · new entries held until 7:45 AM AZ · …m …s left". Qualifying cards read **PREPARE TO BUY · OPENING HOLD** until 07:45 AZ.
4. Optional: change the dropdown (for example to **30 min**). The status line and cards update without a restart.
5. After 10+ sessions, ask Claude to compare the withheld entries (Signal log reason starting **OPENING HOLD**) with what happened after the release.

## Uncertainties

- The 15-minute default is a synthesis. No peer-reviewed study tests a wait time for a 5M/1M momentum system. Two of the supporting sources (Santosh; Zarattini et al.) are working papers.
- The cited studies cover the S&P 500 ETF and liquid "stocks in play". They may not transfer to the small caps that often appear in MCC's Red Zone.
