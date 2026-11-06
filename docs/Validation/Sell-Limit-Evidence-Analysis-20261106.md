# Sell-Limit Evidence Analysis — 2026-11-06 10:53 Arizona

**Evidence archive:** operator upload `20261106T105332.zip`  
**API:** `http://localhost:53360`; health `OK`; decision-support-only; order submission `NOT IMPLEMENTED`  
**Effective Alpaca feed:** `iex`  
**Qualification feed required:** `sip`  
**Branch:** `feature/opportunity-quality-classification`

## Executive result

- EVR and CBOE ATR values in the latest 20-bar numeric comparison are both within the configured absolute tolerance of 0.00005.
- Both parity results remain `GAPS_PRESENT`: EVR has 3 gaps and CBOE has 1 gap in the 119-observation qualification window. Neither is a parity `MATCH`.
- The provider comparison was performed on IEX, not SIP. It is diagnostic only.
- Zero of 119 requested timestamps matched exact OHLCV for either symbol. IEX returned 104 EVR bars and 109 CBOE bars; 15 and 10 requested timestamps respectively had no provider bar.
- No SIP request, 40-session capture, or sell-limit calculation was performed. The runner correctly skipped those actions because the effective feed was IEX.

## 1. Import and persisted-history reconciliation

| Measure | EVR 5m | CBOE 5m |
|---|---:|---:|
| CSV report data rows | 654 | 676 |
| Explicit ATR observations parsed | 327 | 338 |
| Newly appended | 2 | 9 |
| Identical observations skipped | 325 | 329 |
| Corrections | 0 | 0 |
| Full-import timestamp gaps | 5 | 18 |
| First parsed timestamp | 2026-11-02 14:30 UTC | 2026-11-02 08:05 UTC |
| Last parsed timestamp | 2026-11-05 20:55 UTC | 2026-11-05 21:05 UTC |

The filenames specify October 19–November 5, but the actual parsed observations begin November 2. The filename date range is not evidence that October 19–November 1 observations were exported.

The append-only store contains mixed source-file provenance. The two newly appended EVR rows are 2026-11-05 05:00 and 05:10 UTC. The nine newly appended CBOE rows are 2026-11-02 08:05, 2026-11-03 03:50 and 08:00, and 2026-11-04 02:45, 02:55, 05:45, 06:05, 06:10 and 07:35 UTC. Existing rows retain their original import metadata; this is expected and must not be overwritten merely to make the newest import filename appear on every row.

## 2. ATR parity

| Measure | EVR | CBOE |
|---|---:|---:|
| Persisted V2.1 observations | 327 | 338 |
| Compared bars | 20 | 20 |
| Gaps in latest 119-bar window | 3 | 1 |
| Mean absolute error | 0.00002943302061 | 0.00002038659478 |
| Maximum absolute error | 0.00004953492476 | 0.00004260763075 |
| P90 absolute error | 0.00004599671585 | 0.00004058596858 |
| Tolerance | 0.00005 | 0.00005 |
| Status | `GAPS_PRESENT` | `GAPS_PRESENT` |

The numeric errors are inside tolerance, but the timestamp gaps prevent recurrence from being certified across the qualification window. Do not relax tolerance, impute bars, or mark either symbol MATCH.

The parity response's `metadata.sourceFileName` points to an earlier import file even though the persisted observation set includes rows from the latest import. This is not, by itself, proof of incorrect persistence: the observation export shows per-row metadata and mixed source files. For provenance audits, use the per-observation metadata, not the single metadata object on the parity response.

## 3. Provider/TOS comparison — IEX diagnostic only

The 119-row window for each symbol was compared against the provider response. Both provider responses and every returned provider bar identify feed `iex`. The runner's output field `qualificationFeed=sip` described the required qualification feed, not the feed actually requested/returned.

| Measure | EVR | CBOE |
|---|---:|---:|
| TOS timestamps requested | 119 | 119 |
| IEX provider bars returned | 104 | 109 |
| Missing provider bars | 15 | 10 |
| Exact OHLCV rows | 0 | 0 |
| Rows with at least one mismatch or missing bar | 119 | 119 |
| Open mismatches | 85 | 99 |
| High mismatches | 93 | 106 |
| Low mismatches | 79 | 101 |
| Close mismatches | 69 | 88 |
| Volume mismatches | 104 | 109 |
| Provider response feed | `iex` | `iex` |
| Qualification status | Diagnostic only | Diagnostic only |

Comparison windows:
- EVR: 2026-11-04 17:55 UTC through 2026-11-05 20:55 UTC.
- CBOE: 2026-11-04 17:55 UTC through 2026-11-05 21:05 UTC.

The field mismatch counts overlap: one timestamp can have several OHLCV fields that differ. Missing provider bars are counted separately and do not contribute to field-level comparisons.

### Interpretation boundary

This result establishes that the returned IEX bar series does not exactly equal the imported TOS series over the requested timestamps. It does **not** establish which source is wrong, that TOS is necessarily SIP, or that SIP would match. IEX has partial-exchange coverage; a missing IEX bar may reflect feed coverage, no eligible IEX trade, or provider behavior. The available archive cannot distinguish those explanations.

Do not use this IEX comparison as evidence that the SIP gate passed or failed. Once SIP entitlement is confirmed, repeat the comparison with effective SIP configuration and verify that both the provider response and every row explicitly report `sip`.

## 4. Runner hardening applied after this evidence capture

Updated `scripts/validation/Invoke-SellLimitEvidenceQualification.ps1` to make the feed contract explicit in future runs:

- Emits `comparisonMode` as qualification-candidate versus diagnostic-only.
- Emits `qualificationFeedRequired=sip`, `providerResponseFeed`, and `feedIdentityMatch` separately.
- Checks feed identity on each returned provider bar, not only the response envelope.
- Adds field-level mismatch counts for missing bars, feed, open, high, low, close and volume.
- Distinguishes non-SIP diagnostic mismatches from SIP qualification mismatches.
- Keeps the existing hard rule: no SIP smoke request, 40-session capture, or sell-limit call unless effective feed is SIP.
- Does not loosen exact OHLCV comparison or ATR parity tolerance.

This source update has not yet been executed in the user's PowerShell runtime. It must be tested on the next local run before its new output fields are treated as runtime-verified.

## 5. Qualification gate status

| Gate | Result |
|---|---|
| EVR 5m ATR parity MATCH | BLOCKED — 3 window gaps |
| CBOE 5m ATR parity MATCH | BLOCKED — 1 window gap |
| 119 continuous qualified TOS bars | BLOCKED — gaps remain |
| Exact provider/TOS OHLCV | NOT ESTABLISHED — IEX diagnostic has 0/119 exact rows |
| Exact prior RTH close from same qualified feed | NOT CAPTURED |
| SIP entitlement | NOT CONFIRMED; latest effective feed is IEX |
| 40/40 SIP GTC_EXT sessions | NOT CAPTURED |
| Runtime walk-forward PASS on real history | NOT PROVEN |
| Numeric sell-limit output | BLOCKED |

## 6. Next actions

1. Pull the feature branch and use the revised full-chart V2.1 ThinkScript source. Compile it in Thinkorswim; .NET CI does not compile ThinkScript.
2. Export a genuinely broad EVR/CBOE 5m history with all available chart bars, extended hours ON, correct Phoenix timezone and corporate-action adjustment. The actual timestamps must include at least 99 warm-up bars before the latest 119-bar qualification window; do not infer coverage from the filename.
3. Import each export once. Review appended/identical/correction counts and per-observation provenance.
4. Re-run parity. Investigate every gap against the chart and source. Keep 0.00005 tolerance and do not impute.
5. Once both symbols have a continuous, qualifying 119-bar window and parity MATCH, obtain SIP entitlement through the provider account. Do not retry SIP while the account still returns HTTP 403 or the effective feed is IEX.
6. Configure effective SIP locally, restart the API, verify `/api/providers/status`, then run one recent SPY 1m SIP smoke request.
7. Run the updated qualification script. Require response feed and every bar feed to equal SIP. Analyze all 119 rows and the exact previous RTH close.
8. Capture and independently audit 40 complete SIP GTC_EXT sessions (20 train followed by 20 unseen test), each with 780 exact one-minute bar starts and exact previous RTH close.
9. Run the sell-limit endpoint only after source-qualified history exists. Preserve all gate details and walk-forward metrics. No orders are submitted.

## [Uncertainties]

- The cause of each TOS timestamp gap is not established; some may be legitimate no-trade intervals, but the CSV alone cannot prove that.
- The IEX/TOS OHLCV differences do not identify which feed is authoritative or predict SIP results.
- The exact previous RTH reference close was not captured in this archive.
- The revised ThinkScript and the latest PowerShell runner edit have not been compiled/executed in Thinkorswim/PowerShell.
- SIP entitlement, 40-session completeness, runtime walk-forward PASS, browser acceptance, regular-session stream recovery and actual broker fills remain unverified.
