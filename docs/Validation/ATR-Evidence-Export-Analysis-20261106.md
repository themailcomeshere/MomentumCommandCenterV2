# ATR Evidence Export Analysis — 2026-11-06

> **Superseded for current 5m parity status:** the expanded EVR/CBOE exports imported on 2026-11-06 are analyzed in [ATR-Export-Coverage-Fix-20261106.md](ATR-Export-Coverage-Fix-20261106.md). This earlier document remains the analysis of the first evidence ZIP only.

**Evidence archive:** operator upload `20261106T092720.zip`  
**Captured local time:** 2026-11-06 09:27:21 America/Phoenix  
**Captured UTC:** 2026-11-06 16:27:21Z  
**Local Git SHA recorded by exporter:** `9dd2c224d8f1373c7935eb3f94ed5fddea837e5f`  
**Branch:** `feature/opportunity-quality-classification`  
**API health:** `OK`; mode `decision-support-only`; order submission `NOT IMPLEMENTED`.

## 1. Exporter execution — PASS

The uploaded ZIP contains all 12 expected files:

- health and provider status;
- EVR/CBOE parity JSON for 1m and 5m;
- EVR/CBOE persisted TOS observation JSON for 1m and 5m;
- manifest and SHA-256 list.

The exporter completed against `http://localhost:53360`. The manifest records the exact local source SHA above. This validates the exporter path/preflight/output-folder fixes for this run. It does not independently verify a Release build or test run for SHA `9dd2c224...`; the archive contains no build/test transcript.

## 2. Provider state

| Provider | Effective state | Evidence |
|---|---|---|
| Alpaca | configured, validated, IEX | `VALIDATED_DATA_RETURNED`; last validation returned 8 bars |
| Massive | not configured | `NOT_CONFIGURED` |

This is IEX connectivity evidence only. It does not establish SIP entitlement, consolidated-market completeness, extended-hours completeness, or sell-window qualification. Do not retry SIP until entitlement/access has changed and a small recent SIP request succeeds.

## 3. Persisted parity results

| Symbol | Timeframe | Status | TOS observations | Compared bars | Gaps in 119-bar window | Mean absolute error | Maximum absolute error |
|---|---:|---|---:|---:|---:|---:|---:|
| EVR | 1m | `GAPS_PRESENT` | 346 | 20 | 13 | 0.0000260340 | 0.0000466859 |
| EVR | 5m | `NOT_MATCH` | 163 | 20 | 1 | 0.0010467166 | 0.0019357360 |
| CBOE | 1m | `NO_DATA` | 0 | 0 | 0 | — | — |
| CBOE | 5m | `NOT_MATCH` | 165 | 20 | 3 | 0.0005274676 | 0.0009650641 |

Common settings on reports with data: parity `MCC-WILDER-ATR-PARITY-1.1`, ATR(14), Wilder, 99 warm-up bars, 20 comparison bars, tolerance 0.00005. Imported study metadata says `MCC_OHLCV_ATR_V2.1`, Phoenix timezone, extended hours ON and corporate-action adjustment ON.

The CBOE 1m result is not a failure of the ATR formula; no persisted CBOE 1m TOS observations exist in this capture.

## 4. Row-level analysis performed on the ZIP

The persisted observation arrays were sorted by embedded `sourceTimestampUtc`. Independent calculations were performed from each observation's OHLCV and exported ATR.

### Wilder recurrence check

For each pair of consecutive source timestamps exactly one timeframe apart, true range was recomputed from current high/low and the immediately preceding observed close. The exported ATR recurrence was checked using:

`ATR[t] = (13 * ATR[t-1] + TR[t]) / 14`

| Series | Consecutive intervals checked | Non-contiguous intervals excluded | Mean absolute recurrence residual | Maximum absolute residual |
|---|---:|---:|---:|---:|
| EVR 1m | 297 | 48 | 0.00003264 | 0.00009286 |
| EVR 5m | 159 | 3 | 0.00002934 | 0.00009286 |
| CBOE 5m | 157 | 7 | 0.00003112 | 0.00009286 |

The residuals are broadly consistent with a Wilder recurrence when ATR is exported to four decimal places; the largest residual is below 0.0001. This supports that the exported series behaves like a Wilder recurrence across consecutive exported observations. It does **not** establish the initial seed state, treatment of missing intervals, or equality to the server's initial state.

### Independent full-export recomputation

A separate standard Wilder calculation seeded from the first 14 exported observations was run over each sorted export. Its last-20 error against exported TOS ATR was:

| Series | Last-20 mean absolute error | Last-20 maximum absolute error |
|---|---:|---:|
| EVR 1m | 0.0000260340 | 0.0000466859 |
| EVR 5m | 0.0010494685 | 0.0019408227 |
| CBOE 5m | 0.0005327817 | 0.0009748868 |

These values closely reproduce the API parity magnitudes. EVR 1m is within numeric tolerance but remains blocked by 13 timestamp gaps. The 5m errors remain materially above tolerance.

### Interpretation — hypothesis, not proven root cause

The exported ATR appears to follow Wilder recurrence across consecutive bars, while the server's comparison differs in the latest 5m bars. A plausible remaining explanation is a different initial Wilder seed/prefetch context before the first exported observation, potentially compounded by discontinuous source history or how gaps/session boundaries are handled. The current export is not long enough to prove or eliminate this.

Do not declare this root cause proven. Do not relax tolerance, fill gaps, or change the production ATR algorithm based only on this evidence.

## 5. Timestamp continuity findings

The complete exported observation sets contain non-contiguous source timestamps:

- EVR 1m: 48 discontinuities across the full 346-row set; 13 are inside the parity qualification window.
- EVR 5m: 3 discontinuities across the 163-row set; 1 is inside the parity window.
- CBOE 5m: 7 discontinuities across the 165-row set; 3 are inside the parity window.
- CBOE 1m: no observations.

A discontinuity alone does not tell us whether the interval had no eligible trade or whether Thinkorswim/export transport omitted a bar. The evidence archive cannot establish that distinction. No gaps were imputed.

## 6. Important boundary of this evidence

The exporter explicitly captures TOS-imported OHLCV/ATR and parity responses separately from provider bars. It does not include provider OHLCV. Therefore this ZIP cannot prove exact provider/TOS equality, provider feed coverage, the exact provider prior regular-session close, or broker fills.

## 7. Next actions — ordered

1. **Obtain substantially longer 5m TOS history** for EVR and CBOE from the exact V2.1 study, same settings, with a long prefetch/seed runway before the latest 119-bar qualification window. Preserve embedded payload epochs and full provenance.
2. **Resolve every gap** by inspecting the chart/source around each expected timestamp. Classify only with evidence as legitimate no-trade/absent interval or export/study omission. Never synthesize bars.
3. **Import only genuinely new/corrected rows**; record parsed/appended/identical/correction counts. Re-run parity after import without changing tolerance.
4. **Diagnose the seed convention** by comparing per-bar server ATR and TOS ATR across the longer history, especially whether error decays as earlier history is added. Verify first valid TR, seed window, recurrence, bar ordering, session boundaries and behavior across gaps.
5. **Capture provider OHLCV separately** for the exact same 119 timestamps once an entitled feed is available. Compare all OHLCV fields and verify the exact prior RTH close from that same provider/feed.
6. Keep sell-limit output blocked until ATR parity is `MATCH`, the 119-bar provider/TOS equality and reference-close gates pass, SIP entitlement and 20/20 GTC_EXT coverage pass, and the implemented 20-train/20-unseen-test walk-forward validator returns a fully documented PASS.

## [Uncertainties]

- The initial Wilder seed/prefetch mismatch is a hypothesis, not a proven root cause.
- Missing timestamps cannot be classified as no-trade intervals versus source/export omissions from this ZIP alone.
- CBOE 1m has no persisted observations.
- Provider OHLCV, SIP entitlement, GTC_EXT completeness, runtime walk-forward PASS, browser acceptance and broker execution records are absent from this archive.
- The archive does not contain a build/test transcript for its recorded source SHA.
