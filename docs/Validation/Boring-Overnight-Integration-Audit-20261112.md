# Boring Overnight integration audit — 2026-11-12

Branch: feature/opportunity-quality-classification

## Operator response reviewed

Source: data/ChatResponse.md (blob f3d935a92fcc35882777eab67178f65b3767c9d3).

### Selected-window bar retrieval (CRBG, Alpaca/IEX)

| Interval | Returned bars | Gap count | Newly persisted |
|---|---:|---:|---:|
| 1 minute | 339 | 42 | 0 |
| 5 minutes | 78 | 0 reported | 0 |

The 1-minute request covered 2026-11-11 14:30Z through 2026-11-12 03:00Z; returned data ended 2026-11-11 20:59Z. The 5-minute response ended 20:55Z. Both were IEX. Zero newly persisted means these observations were already present according to the endpoint's idempotent append result; it does not mean the provider returned no bars.

### Twenty-session capture

- 6,676 1-minute bars fetched; 4,225 newly persisted.
- 20 requested pairs; 13 valid; 7 missing; allPairsComplete=false; enoughHistory=false.
- Missing exact next-session 09:30 ET bars: 2026-10-16, 2026-10-22, 2026-10-27, 2026-11-05, 2026-11-09, 2026-11-10, 2026-11-12.
- All valid opening references are 09:30 ET one-minute bar Open proxies; official opening auction is not verified.
- Daily: 20/20 expected sessions, fresh, IEX_PARTIAL, qualificationUsable=false. Observed close/SMA/volume are diagnostics; qualification fields are null.
- Catalyst: one MATERIAL_EVENT_REVIEW article; coverageComplete=true. Human review is required.
- The saved snapshot had every evaluator input null, including DataFresh, despite a fresh Daily response. It also mapped MATERIAL_EVENT_REVIEW to UNKNOWN, which understated a review-required article.

### Top 4 Daily/catalyst evidence in operator response

| Symbol | Daily | Catalyst | Articles |
|---|---|---|---:|
| CRBG | 20/20 fresh, IEX_PARTIAL | MATERIAL_EVENT_REVIEW | 1 |
| FG | 20/20 fresh, IEX_PARTIAL | UNKNOWN | 0 |
| CSQR | 20/20 fresh, IEX_PARTIAL | UNKNOWN | 0 |
| INGM | 20/20 fresh, IEX_PARTIAL | UNCLASSIFIED_NEWS_REVIEW | 2 |

Zero articles remain UNKNOWN, not CLEAR. Unclassified articles require review, not clearance.

## Code changes made

1. Fixed the capture snapshot's DataFresh mapping. Previously, a fresh Daily response left DataFresh null due to a faulty conditional expression. It now records the Daily client's explicit freshness value; a missing Daily response remains null.
2. Added a fail-closed catalyst status mapper:
   - RISK_PRESENT, MATERIAL_EVENT_REVIEW and UNCLASSIFIED_NEWS_REVIEW map to RISK.
   - CLEAR_WITH_SOURCE maps to CLEAR only with at least one article and complete coverage.
   - Empty news, unknown status, incomplete coverage or absent assessment remain UNKNOWN.
   - Current Alpaca catalyst client does not emit CLEAR_WITH_SOURCE, so automated capture cannot silently clear news.
3. Added theory coverage for risk, material review, unclassified review, zero-news unknown, incomplete coverage and explicit source-backed clearance.

Files:
- src/MomentumCommandCenterV2.API/BoringOvernight/BoringOvernightHistoryEvaluationService.cs
- src/MomentumCommandCenterV2.API/Program.cs
- tests/MomentumCommandCenterV2.Tests/BoringOvernightHistoryEvaluationServiceTests.cs

Commits:
- f182d7495c1522a2278ea5ec56f1f45496816f25 — status mapper
- b0510f8927a440016d53175bbe64183e61af6f11 — capture wiring
- f854adec0ec24b2fdd714dccdffb3af5a0ca97a5 — regression tests

## Remaining blockers — not code-solvable from supplied evidence

- CRBG still has 7 exact 09:30 bars missing. Do not substitute nearest bars: current history contract intentionally requires exact timestamps.
- IEX_PARTIAL cannot qualify Daily inputs. Obtain a supported SIP or independent consolidated feed and verify adjustment/session parity.
- Catalyst articles require human review and authoritative-source review. No automated CLEAR status is currently produced.
- The 5M permission, 5M structure, 1M execution state, extension ATR, current volume-vs-average, daily range and ATR inputs are not supplied by the capture endpoint. They must be wired from the live technical assessment with source timestamps and freshness checks before qualification can be complete.
- Current Overnight return must be sourced from the current completed close-to-open interval; do not reuse the latest historical pair as a current observation.
- The capture's 09:30 one-minute bar Open remains a proxy, not verified official auction price.

## Required verification

1. Pull the latest feature branch and run Release build/tests.
2. Re-run CRBG capture after provider coverage is available; expect all 20 exact pairs before historical metrics populate.
3. Verify saved snapshot now has DataFresh=true when Daily says fresh, while IEX Daily qualification fields remain null.
4. Verify MATERIAL_EVENT_REVIEW and UNCLASSIFIED_NEWS_REVIEW persist as RISK; zero articles persist UNKNOWN.
5. Confirm evaluator remains DATA? / BLOCKED until all independent live technical, Daily, history and catalyst gates are satisfied.
6. Do not interpret READY as an order or submit orders; MCC remains decision support only.

## [Uncertainties]

- GitHub connector writes do not run the local .NET test suite. The new tests and code require CI/local execution before claiming build success.
- Whether an independent consolidated feed is configured is not established by these responses.
- Official auction print availability is not established; only 09:30 one-minute bar Open proxy is present.
