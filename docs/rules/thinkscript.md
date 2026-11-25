---
description: Project-specific standards for authoring, auditing, and debugging Momentum Command Center ThinkScript studies and scans in thinkorswim.
globs: ["*.ts", "*.thinkscript", "*.txt"]
alwaysApply: true
---

# Momentum Command Center — thinkScript Development Standards

These rules apply to MCC 1M/5M chart studies and any related scans. Treat source files in `docs/Charts/1M/` and `docs/Charts/5M/` as version-controlled references, not proof that the same version is installed in Thinkorswim.

## 1. MCC architecture and safety boundaries

- **5M is authoritative** for market structure, permission, runner state, and swing context. **1M consumes 5M context** for execution, confirmation, and timing. Never make 5M depend on 1M.
- Keep eligibility/permission, opportunity quality, entry timing, position management, and data confidence distinct. Do not collapse them into one score or label.
- This repository is a research and decision-support prototype, **not an auto-trader**. Do not add automatic order placement, broker submission, or claims of live execution as part of a study change.
- Preserve existing signal semantics, thresholds, state transitions, and the 5M-to-1M authority boundary unless a reproducible defect is demonstrated and the change is explicitly scoped.
- Do not infer that a chart's installed study matches a repository file from the filename alone. Confirm version/header and compare relevant source when validating a screenshot or behavior.

## 2. Syntax and language constraints

- thinkScript is case-insensitive; use readable camelCase/PascalCase consistently.
- Every standalone statement must end with a semicolon.
- Use `input` for user-configurable settings and `def` for calculated values. Use `plot` for plotted series and shapes.
- thinkScript has no general-purpose loops or external API calls. Use supported vectorized functions, built-ins, and recursive indexing.
- For conditional expressions, use the complete `if ... then ... else ...` form.
- For recursive variables, handle initialization explicitly; use `CompoundValue()` where needed and validate behavior at the first available bars.

## 3. Historical-bar semantics and state

- `close` (or another series without an index) means the current bar; `close[1]` means the previous bar.
- Event bubbles are evaluated on the bar where their event condition is true. A current/right-edge `AddLabel()` is not evidence of what the state was on an earlier bar.
- When investigating historical bubbles, inspect per-bar diagnostics and the exact event/gate definitions. Do not explain a historical event using only current labels.
- Explicitly inspect finite-window age, event re-arming, persistence counters, invalidation, and session boundaries when those concepts participate in the event.
- Keep diagnostics observational: diagnostic labels/bubbles must not silently change execution gates or state transitions.

## 4. Study types and chart presentation

- Declare chart placement explicitly where applicable: `declare upper;` for price-chart overlays and `declare lower;` for lower studies.
- Use `AddLabel()` for current/right-edge summary state and `AddChartBubble()` for bar-specific events or diagnostics. Label text must make that distinction clear.
- Keep historical debug bubbles optional and warn that per-bar bubbles can clutter a chart. Do not create duplicate bubbles where a single composite bubble is the established design.
- Keep diagnostic codes documented at their definitions and ensure the displayed code corresponds to the actual ordered conditions.
- A chart study and a backtesting strategy are different artifacts. **Do not add `AddOrder()` to a study merely because a generic strategy template mentions it.** Use orders only in a separately scoped strategy/backtest artifact with explicit fill assumptions and tests.

## 5. Formula fidelity and change control

- Do not replace a manually specified MCC formula with a built-in indicator simply to shorten the code. First establish whether the formula, smoothing, aggregation, price input, session behavior, and warm-up semantics are equivalent.
- Prefer built-in indicators when they are proven equivalent to the intended source behavior; document any intentional divergence.
- Avoid redundant calculations, but do not refactor signal logic during a diagnostic-only change.
- Preserve existing input defaults and user-facing label/bubble meanings unless the task explicitly changes them.
- Make the smallest change that resolves the demonstrated issue. Do not tune thresholds or optimize performance against a single screenshot.

## 6. Validation required before calling a change successful

1. Identify the exact source file/version and the specific behavior being changed.
2. Trace the full dependency chain for the event or state; check both the 1M logic and relevant 5M inputs.
3. Confirm ThinkScript syntax/compile behavior in Thinkorswim. A repository edit alone is not compile validation.
4. Compare relevant historical bars, including preceding bars needed by `[1]`, recursion, persistence, and finite windows.
5. Check regular/extended-hours behavior and Arizona/Eastern/DST conversions wherever time logic is involved.
6. Verify diagnostics match the same-bar conditions and do not affect trading-state logic.
7. Record the observed result, test conditions, unresolved unknowns, and whether the installed study was confirmed to match the source.

Do not claim a fix, runtime success, or signal improvement without the corresponding evidence. When data or chart evidence is missing, state: **“Data not found in sources.”**

## 7. Dashboard labels and scans

- Use clear, stable names for inputs, calculations, events, and state codes.
- A scan/watchlist column must not be described as equivalent to a chart study unless its aggregation and required secondary data have been verified.
- Keep current state, historical event, entry eligibility, opportunity quality, and position-management state visibly distinct.
- Keep diagnostic text concise enough to read on the chart, and document abbreviations/codes in the source or companion documentation.

## 8. Watchlist column complexity limit (added 2026-11-16)

Thinkorswim rejects custom watchlist columns whose expression graph is too large with `TooComplexException: The complexity of the expression suggests that it may not be reliable with real-time data.` This happened to the first V3 Boringness and V3 Status columns, which called a `script` helper about 30 times with arguments derived from 20-bar averages.

- NEVER call a user `script` repeatedly in a watchlist column. Thinkorswim inlines the script and its argument expressions at every call, so the cost multiplies.
- ALWAYS write piecewise-linear maps as clamped segment sums: `slope * (Max(lo, Min(hi, x)) - lo)` per segment. Do not use long `if … else if` chains.
- ALWAYS compute each rolling statistic once in a `def` and reuse the `def`.
- ALWAYS hand the operator one column at a time when complexity is uncertain, and state that a repository file is not proof of compilation.

## 9. Study ↔ MCC engine parity (added 2026-11-22)

The 5M and 1M studies previously had three different EOD times (installed 18:55/19:55 ET, repository 18:55/17:55 ET, MCC engine 15:55 ET), and the 1M had no opening hold after MCC added one.

- ALWAYS keep the study EOD inputs (`eodTimeEasternDaylight`, `eodTimeEasternStandard`) equal to `V2Config.EndOfDayExitHourEt/MinuteEt` and `PermissionLatchConfig` EOD defaults. Operator decision 2026-11-22: **15:55 ET in both seasons** (12:55 AZ daylight, 13:55 AZ standard).
- ALWAYS keep the 1M `openingEntryHoldMinutes` default equal to `MarketData:Intraday:OpeningEntryHoldMinutes` in `appsettings.json` (15 as of 2026-11-21).
- When an MCC engine commit changes an entry, exit, EOD or session-window rule that a study also implements, ALWAYS update the matching study in the same task, or add an **[Operator]**/**[Repo]** parity item to `docs/Master-To-Do.md` §5 naming both sides.
- ALWAYS gate the 1M study's `fivePermission` with the 5M Main Study permission latch (advanced once per completed 5M bar, reset at each new date), never with a separate 1M-side 5M reconstruction. MCC's `FiveMinutePermissionLatch` is the C# port of the same latch; a rule change goes into all three. Operator decision 2026-11-25 (V14.4.13); evidence: `docs/Validation/OneMinute-Entry-Parity-Replay-20261125.md`.
- NEVER give a study a Boring Overnight HOLD/READY signal. The study cannot see the overnight statistics, catalyst sign-off, exclusions or limit; it may show DATA? or a 1M SELL block only.
- ALWAYS build a new study version from the source the operator has installed (`docs/Validation/Installed-*.txt`) unless the operator confirms a different file is installed, and archive superseded repository copies under `docs/archive/Charts/`.
