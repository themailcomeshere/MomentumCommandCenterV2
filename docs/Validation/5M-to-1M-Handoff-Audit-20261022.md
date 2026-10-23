# 5M → 1M Handoff Source Audit

**Date:** 2026-10-22 (Arizona time)  
**Branch:** `feature/opportunity-quality-classification`  
**Scope:** Repository source comparison only. This is not proof that the installed Thinkorswim studies match these files.

## Findings

### Authority boundary

- The 5M study is the authority for Permission, bullish regime, runner, waning, swing, and failure states. It contains no 1M aggregation references in the reviewed source.
- The 1M study is a consumer. It reconstructs a 5M layer from `AggregationPeriod.FIVE_MIN` secondary OHLCV; it does not read the actual 5M study's internal Permission latch.
- Therefore the 1M study's reconstructed 5M state can differ from the actual 5M chart's displayed state. A reconstructed “5M BUY” bubble is a setup/state diagnostic, not proof of actual 5M Permission or executable entry.
- Canonical 1M READY is gated by `executionWindowActive`, `fiveExecutionPermission`, and `!bullishExtensionDanger`. `fiveExecutionPermission` additionally requires reconstructed 5M permission, 1M price above Daily VWAP, no persistent reconstructed major failure, and no reconstructed confirmed weakness.

### Concrete source differences to preserve and validate

| Area | 5M authority source | 1M reconstructed source | Implication |
|---|---|---|---|
| Raw Permission | `bullishPermissionRaw` requires price above Daily VWAP, price above EMA9, EMA9 >= EMA20, and RSI >= minimum. | `fiveBullishPermissionRaw` uses `fiveBullishCore`, RSI >= minimum, and `!fiveBearishStack`. | These predicates are not textually equivalent. Test same-symbol/same-bar examples before treating reconstructed Permission as exact parity. |
| Structural damage | `structuralDamage` is price below Daily VWAP and EMA9 below EMA20; persistence uses `permissionFailureBars`. | `fiveStructuralDamage` includes several EMA/price conditions and a `!fiveEMA9Rising` condition; persistence also uses `permissionFailureBars`. | The revocation path may differ between studies. Do not change either predicate without chart evidence and an agreed contract. |
| Major failure | Price below Daily VWAP, price below EMA9, EMA9 below EMA20, and EMA20 below SMA50; persistence uses `majorFailureBars`. | Reconstructed major failure uses 5M close below 5M EMA9, EMA9 below EMA20, and EMA20 below SMA50; persistence uses `majorFailureBars`. | The 1M reconstruction omits the explicit Daily VWAP condition present in 5M major failure. This is a concrete parity difference to validate. |
| Permission latch | 5M checks EOD, persistent major failure, persistent structural damage, then raw Permission; otherwise it retains prior state. | The reconstructed 5M latch follows the same broad ordering, but consumes its own predicates. | Similar latch ordering does not guarantee equivalent state. |
| Execution readiness | 5M does not define 1M READY. | READY is a short-lived 1M window gated by reconstructed 5M execution permission and extension danger. | Do not conflate a candidate, continuation watch, reconstructed 5M setup, and READY. |

### Other verified semantics

- 1M `inTrendExecutionCandidate = executionCandidate AND NOT freshTrigger`; it is a candidate classification, not READY by itself.
- 1M extension uses `AbsValue(price - dailyVWAP) / ATR(14)`; defaults are warning at 1.50 ATR and danger at 2.00 ATR. Bullish extension danger requires price above Daily VWAP and extension at/above the danger threshold.
- The 5M Permission latch revokes for EOD, persistent major failure, or persistent structural damage; recovery requires raw bullish Permission. The diagnostic code maps EOD to 1, persistent major failure to 2, and persistent structural damage to 3.
- The source files define Arizona 15:55 EOD using Eastern time inputs 18:55 during Eastern daylight time and 19:55 during Eastern standard time.

## Disposition

**Audit status: source comparison completed; behavioral parity remains unverified.** No ThinkScript logic was changed because the source differences require evidence from the installed studies and chart bars, not a speculative edit.

## Next validation

1. Compare installed 5M and 1M studies against the repository files and record exact version/source identity.
2. On identical symbol/date/time bars, record actual 5M Permission, 5M diagnostic blocker, reconstructed 5M Permission, reconstructed blocker, and 1M READY.
3. Prioritize cases where the raw Permission, structural damage, or major-failure predicates disagree.
4. Confirm whether the intended contract is exact predicate parity or intentionally conservative/different 1M gating before changing code.
5. Then test fresh candidate, in-trend candidate, continuation/re-entry, READY expiration, extension danger, runner/waning/swing interactions, and EOD transitions.

**No trading outcome or profitability claim is made.**
