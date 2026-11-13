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

## Build and test

- Operator verification: `.\scripts\Get-Latest-And-Test.ps1` from the repository root (Release build + full tests; reports the exact source SHA).
- A code-bearing head is verified only when exact-head CI and local Release tests pass. Browser, runtime, Thinkorswim compilation and data-source evidence are separate gates.

## Markdown rules

- `docs/Master-To-Do.md` is the single authoritative checklist. Update existing items in place; NEVER append dated duplicate sections. Move superseded history to `docs/archive/`.
- Tag every To-Do item **[Repo]** or **[Operator]**. NEVER mark operator-side, Thinkorswim, browser or provider-evidence items complete from code or CI alone.
- Use `- [ ]` / `- [x]` checklists. Dates as `YYYY-MM-DD`. Cite exact SHAs and CI run IDs as evidence.
- Missing data is written as "Data not found in sources." — never estimated.
- <!-- Add your additional Markdown rules here. -->

## Trading-data guardrails

- Keep Alpaca on IEX; IEX_PARTIAL is diagnostic only, never consolidated coverage. No SIP requests unless the operator reports an entitlement change.
- NEVER impute missing bars, sessions, prices, timestamps, ATR observations, reference closes or catalyst facts; no nearest-bar substitution.
- Zero news is UNKNOWN, not CLEAR. Research results (Boring Winner PASS, research quantiles) are never READY, a quote, or a trade instruction.
- Schwab integration and broker reconciliation are parked; do not start them.
