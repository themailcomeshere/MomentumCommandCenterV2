# 2026-11-06 — latest operator parity results and consolidated task status

**Operator confirmed:** API restarted; Release build/tests passed at local source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`. API remains healthy in decision-support-only mode; order submission is not implemented. Alpaca effective feed is IEX, validated; Massive is not configured.

## Latest parity responses

| Series | Status | Source observations | Compared bars | Gaps in 119-bar window | Mean absolute error | Maximum absolute error |
|---|---|---:|---:|---:|---:|---:|
| EVR 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.00002603400795 | 0.00004668593564 |
| EVR 5m | `NOT_MATCH` | 163 | 20 | 1 | 0.00104671656564 | 0.00193573596950 |
| CBOE 5m | `NOT_MATCH` | 165 | 20 | 3 | 0.00052746764259 | 0.00096506405381 |

All three responses report parity version `MCC-WILDER-ATR-PARITY-1.1`, ATR(14) Wilder, 99 warm-up bars, 20 comparison bars, and fixed tolerance 0.00005. EVR 1m numeric errors are within tolerance, but the 13 gaps block MATCH. EVR 5m and CBOE 5m fail numeric tolerance; CBOE also has three gaps in the comparison window.

These calculations compare the server Wilder ATR to TOS-exported ATR on the imported TOS OHLCV. They do not diagnose provider-feed differences. The 5m mismatch root cause is not established. Do not relax tolerance, impute gaps or enable numeric sell-limit output.

## Reconciled active work

The former 310 unchecked historical boxes are now consolidated into one active checklist and one detailed backlog. The old checklist is preserved at `docs/archive/Master-To-Do-History-20261106.md`. The active list is `docs/Master-To-Do.md`; detailed acceptance criteria are in `docs/Active-Engineering-Backlog-20261106.md`.

The walk-forward validator is implemented; only runtime qualification against 40 complete eligible sessions remains. Daily bars, catalyst keyword triage, Opportunity Quality diagnostic assessment, collector/stream scripts and the sell-limit evidence orchestrator have existing code, but their remaining source-validation, calibration or runtime acceptance is explicitly tracked in the active backlog.

## Next operator action

Run `.scripts\validation\Export-AtrParityEvidence.ps1` from the repository root. Provide the generated output folder (or ZIP). It captures raw persisted TOS observation rows and aggregate parity for EVR/CBOE 1m/5m, health/provider status, local Git SHA where available, manifest and hashes. Use these row-level values to diagnose the numeric mismatch. No CSV re-import is needed.

Then compile/export corrected continuous V2.1 5m TOS history with at least 119 completed bars and resolve the unexplained gaps. SIP qualification remains paused until actual entitlement changes.

See `docs/Master-To-Do.md`, `docs/Active-Engineering-Backlog-20261106.md` and `docs/Runtime-Acceptance-Runbook.md`.

[Uncertainties]
- Numeric mismatch root causes and gap causes remain unknown.
- SIP entitlement, 40/40 history, walk-forward runtime pass, browser acceptance, provider/TOS equality and broker execution reconciliation remain unverified.

---

# 2026-11-06 — operator validation and re-import evidence

- [x] Operator reports Release build and tests PASS at source SHA `cd6e93f1c91a8bb69953bd12e5e9cee30e2fe839`. GitHub branch tree HEAD independently resolves to the same SHA.
- [x] API health reports `OK`, `decision-support-only`, and `orderSubmission=NOT IMPLEMENTED`.
- [x] Provider status confirms Alpaca `iex` configured and validated, with one successful validation bar. Massive is not configured. This is connectivity evidence only; it is not proof of SIP entitlement or full extended-hours coverage.
- [x] EVR 1m import: 346 observations, 0 appended, 346 identical skipped, 0 corrections; 48 gaps across the full export.
- [x] EVR 5m import: 82 observations, 0 appended, 82 identical skipped, 0 corrections; one 10-minute gap.
- [x] CBOE 5m single-day import: 85 observations, 0 appended, 85 identical skipped, 0 corrections; five gaps.
- [x] CBOE 5m two-day import: 165 observations, 0 appended, 165 identical skipped, 0 corrections; six gaps, including a 3h20m discontinuity.
- [ ] Re-query persisted parity for EVR/1m, EVR/5m and CBOE/5m after restarting the API from this Release build. Imports were idempotent; do not upload the CSVs again.
- [ ] Diagnose numeric mismatches using exact timestamp-matched source OHLCV/ATR rows and the full-history Wilder seed, true range, prior close, recurrence, ordering and session boundaries.
- [ ] Establish whether each gap represents a genuine no-trade interval or a missing chart/export observation. Do not infer; do not impute.

The latest import responses do not themselves contain fresh parity results. Previous parity results remain diagnostic context only until the endpoint is queried again. See the current handoff `docs/NewChatHandoff-20261106.md`.

## Exact parity recheck

```powershell
$base = "http://localhost:53360"
$parity = @(
    "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=1m",
    "$base/api/strategy-report/atr-parity?symbol=EVR&timeframe=5m",
    "$base/api/strategy-report/atr-parity?symbol=CBOE&timeframe=5m"
)
foreach ($url in $parity) {
    "`n===== $url ====="
    Invoke-RestMethod $url | ConvertTo-Json -Depth 20
}
```

[Uncertainties]
- Fresh parity output is not present in the latest operator evidence.
- Numeric mismatch root causes and the meaning of missing timestamps remain undetermined.
- SIP entitlement, 20/20 session qualification, runtime walk-forward pass, browser acceptance and broker execution reconciliation remain unverified.

---
## 2026-11-05 Arizona — Wilder ATR parity seed correction

- [x] Investigated the imported EVR 1m API result (`NOT_MATCH`, mean 0.000029, max 0.000061). The endpoint was trimming the source to the latest 119 bars before calculating Wilder ATR. That re-seeded the recursive ATR at the start of the 119-bar window rather than carrying the full chart-history seed forward.
- [x] Reproduced the exact API values from the committed 346-row EVR 1m V2.1 export: full-history calculation compared on the latest 20 gives mean absolute error 0.000026034 / max 0.000046686; a fresh seed from only the latest 119 gives 0.000028985 / 0.000060665, matching the operator's reported 0.000029 / 0.000061 after rounding.
- [x] Updated `ThinkorswimAtrParityService` to calculate Wilder ATR from all imported observations while applying the requested 119-bar (99 warm-up + 20 comparison) window only to the comparison slice and timestamp-gap qualification.
- [x] Updated `/api/strategy-report/atr-parity` to pass full history to the service. Timestamp gaps are counted only inside the latest requested comparison window; missing bars remain un-imputed.
- [x] Bumped parity implementation version to `MCC-WILDER-ATR-PARITY-1.1` and added a regression test proving the full-history seed is used while only the latest 20 bars are compared.
- [x] Local API parity re-test completed 2026-11-06. Latest results: EVR 1m `GAPS_PRESENT` (13 gaps in the 119-bar window); EVR 5m `NOT_MATCH` (163 observations, one window gap); CBOE 5m `NOT_MATCH` (165 observations, three window gaps). Browser acceptance is tracked separately in `docs/Active-Engineering-Backlog-20261106.md`.

[Historical uncertainty note as of 2026-11-05]
At the time this section was written, CI and local API verification had not yet been supplied. Both have since been reported/presented; see the 2026-11-06 section at the top. The 13 gaps in the EVR 1m comparison window still prevent a parity MATCH.

---

# ATR V2 Parity Investigation — 2026-11-05

## Data inspected

- `data/StrategyReports_EVR_11032026_FULL_1M.csv`: 692 numbered Strategy Report order rows, 346 ATR V2 payload observations, 48 missing one-minute intervals, 0 duplicate payload timestamps.
- `data/StrategyReports_EVR_11032026_FULL_5M.csv`: 162 numbered order rows, 81 ATR V2 payload observations, 1 missing five-minute interval, 0 duplicate payload timestamps.

Each source bar emits a transport-close order and an ATR payload order. Count payload observations, not total Strategy Report order rows, when measuring chart-bar coverage.

## Root cause

The previous study source used:

```thinkscript
def trueRange = TrueRange(high, close[1], low);
```

The official Thinkorswim function signature is `TrueRange(high, close, low)`; the function itself uses the previous close. The prior source therefore supplied a shifted close series and shifted the previous-close reference back an additional bar.

Reference: https://toslc.thinkorswim.com/center/reference/thinkScript/Functions/Tech-Analysis/TrueRange

Corrected source now uses:

```thinkscript
def trueRange = TrueRange(high, close, low);
```

The corrected source change is in commit `efecef0c26a9af064e5cba68d522fa9d7137ca51`.

## Numerical reproduction

On the 1m export, after 99 warm-up observations and comparing 247 timestamp-aligned rows:

| Calculation | Mean absolute error | Maximum absolute error |
|---|---:|---:|
| Standard previous-close Wilder ATR | ~0.063697 | ~0.147885 |
| Reproduced extra previous-close shift | ~0.000025445 | ~0.000049738 |

The V2 payload exports ATR to four decimal places. The extra-shift calculation is within the 0.00005 rounding bound; the standard calculation is not. This is strong evidence that the November 3 export was generated by the old shifted formula. It is not a reason to preserve that formula.

The server parity service uses standard Wilder true range (current high/low plus the immediately preceding bar close), with a 99-observation warm-up, at least 20 comparison bars, and 0.00005 maximum absolute error tolerance. The legacy 1m export should report `NOT_MATCH`; the 5m export should report `INSUFFICIENT_WARMUP` because it has only 81 observations.

## Gap policy

A numeric ATR comparison is not a `MATCH` if the latest comparison window contains missing source timestamps, even when the numeric ATR values happen to be within tolerance. The parity service returns `GAPS_PRESENT` and never imputes a missing bar. Sell-limit orchestration evaluates the latest 119 rows (99 warm-up + 20 comparison) and requires exact provider/TOS OHLCV equality across all 119 timestamps.

## Required re-validation

The corrected ThinkScript has to be compiled and re-exported in the user's Thinkorswim instance. A successful .NET build cannot verify ThinkScript semantics or installed chart settings.

For the 5m report, export at least 119 consecutive completed observations: 99 warm-up observations plus 20 comparison observations, with no timestamp gaps in the window. Keep the chart on the selected aggregation and confirm ATR(14), Wilder, Phoenix timezone, extended-hours ON and corporate-action adjustment ON.

Do not mark ATR parity complete or calculate a numeric sell limit until the corrected export returns `MATCH` and the separate SIP/history/reference-close gates pass.
