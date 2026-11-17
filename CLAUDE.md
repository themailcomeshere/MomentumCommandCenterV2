# Momentum Command Center V2 — Claude rules

Decision-support trading dashboard: ASP.NET Core API (`src/`), vanilla JS Dashboard (`frontend/index.html`), xUnit tests (`tests/`), Thinkorswim studies. Order submission is NOT IMPLEMENTED and must stay that way.

## Primary rules (always apply)

@docs/rules/primaryRules.md

## Task-specific rules — read before working in that area

- C# / API / Application / Infrastructure: `docs/rules/aspCoreBestPracticesAndCodingStandards.md`
- Dashboard HTML / CSS / JavaScript: `docs/rules/htmlTailwindCssVanillaJavascriptCodingRules.md` (and `docs/rules/jqueryBestPractices.md` only where jQuery is already used)
- ThinkScript studies: `docs/rules/thinkscript.md`

## Git

- ALWAYS work on `feature/opportunity-quality-classification`. NEVER commit to or modify `master`.
- Commit and Push every time.
- NEVER reset, discard or overwrite the user's local changes.
- When an operator commit undoes something the operator asked for (for example a reverted file from a stale editor buffer), do not quietly keep it or quietly re-apply it. Name the commit and what it undid at the top of the reply, and ask with a yes/no button whether to re-apply. Added 2026-11-16 after `528aa1d` reverted the Exclusions dock button and the operator did not know.

## Build and test

- Operator verification: `.\scripts\Get-Latest-And-Test.ps1` from the repository root (Release build + full tests; reports the exact source SHA).
- A code-bearing head is verified only when exact-head CI and local Release tests pass. Browser, runtime, Thinkorswim compilation and data-source evidence are separate gates.

## Markdown rules

- `docs/Master-To-Do.md` is the single authoritative checklist. Update existing items in place; NEVER append dated duplicate sections. Move superseded history to `docs/archive/`.
- Tag every To-Do item **[Repo]** or **[Operator]**. NEVER mark operator-side, Thinkorswim, browser or provider-evidence items complete from code or CI alone.
- Use `- [ ]` / `- [x]` checklists. Dates as `YYYY-MM-DD`. Cite exact SHAs and CI run IDs as evidence.
- Missing data is written as "Data not found in sources." — never estimated.
- Operator click-path instructions MUST name the Dashboard WORKSPACE mode (INTRADAY MOMENTUM / BORING OVERNIGHT) for every step, and every named control MUST be checked as visible in that mode (some are hidden by `body.boring-overnight-mode` CSS). Added 2026-11-14 after the rank button was unreachable from the steps given.
- Arizona times MUST be converted from America/New_York for the date in question (Arizona has no DST: regular session is 06:30–13:00 AZ in daylight time, 07:30–14:00 AZ in standard time). State which applies. Added 2026-11-14.
- Operator steps MUST name the exact field each value goes into and what fills it ("the **Symbols** box in 4 READY check; MCC fills it from Boring Top 4"), never "enter your picks". Before handing over a workflow, run it end to end in the browser pane with the operator's real file and confirm each named control does what the step says. Added 2026-11-17 after an empty READY box silently checked SPY.
- <!-- Add your additional Markdown rules here. -->

## Trading-data guardrails

- Keep Alpaca on IEX for intraday, live and stream data; IEX_PARTIAL is diagnostic only, never consolidated coverage. No recent-SIP requests unless the operator reports an entitlement change.
- Exception (operator decisions 2026-11-14): completed **Daily** bars (`MarketData:Alpaca:DailyFeed=sip_delayed`) and completed historical **1m bars for the GTC_EXT sell-window history and the 20-pair close/open history** (`MarketData:Alpaca:AllowDelayedHistoricalSipMinutes=true`) may be requested on SIP as delayed historical queries (request end ≥ 16 minutes old). Live, stream and recent requests stay on IEX. A Daily SIP refusal falls back to IEX and is labelled; it never becomes qualification-usable.
- NEVER impute missing bars, sessions, prices, timestamps, ATR observations, reference closes or catalyst facts; no nearest-bar substitution.
- Zero news is UNKNOWN, not CLEAR. Research results (Boring Winner PASS, research quantiles) are never READY, a quote, or a trade instruction.
- Schwab integration and broker reconciliation are parked; do not start them.
- NEVER reuse an intraday momentum gate (5M permission/structure, 1M execution) as a hard gate for a Boring Overnight decision without an explicit operator decision. Before calling a multi-gate signal "built", replay it on at least one real candidate snapshot and show which gates would pass or fail. Added 2026-11-16 after READY blocked every boring candidate on "5M permission NO".
