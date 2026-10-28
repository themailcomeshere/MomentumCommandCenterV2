# Overnight Scanner Review — 2026-10-28

**Timezone:** Arizona.  
**Evidence:** Readable Overnight Scanner screenshot captured 15:24:43 Arizona and the MCC table supplied by the user. Individual symbol chart screenshots linked in the request could not be read through the current GitHub image retrieval route. This review does not make unsupported candle-pattern claims.

## Decision

**Preliminary Boring Overnight chart-review order: RTO, AMT, SXC. None is READY on these inputs alone.** This is a review shortlist, not a buy instruction. Catalyst clearance, 20-day average volume, price/SMA, exclusions and fresh chart state must be verified.

| Symbol | Scanner score | ON win % | Avg ON % | Median ON % | 5D ON % | Current ON % | ON momentum | MCC 5M change | MCC 1M change | MCC range 5M / 1M | Assessment |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| RTO | 63.98 | 70% | 0.69 | 0.30 | 1.60 | -0.17 | 0.91 | -0.26% | -0.22% | 1.29% / 1.29% | Best preliminary boring profile: 70% win rate, positive median and narrowest supplied intraday range among the leading candidates. Negative intraday move still needs chart/context review. |
| AMT | 60.03 | 60% | 0.96 | 0.22 | 1.79 | 0.61 | 0.82 | +0.83% | +0.49% | 4.37% / 4.36% | Better historical average, but only 60% win rate and materially wider intraday range. Secondary candidate. |
| SXC | 60.55 | 60% | 0.58 | 0.10 | 1.96 | 0.09 | 1.37 | +1.24% | +1.24% | 6.20% / 6.20% | Positive historical metrics and small current ON move, but 6.20% intraday range conflicts with a low-noise strategy. Not the first choice based on current evidence. |
| BNC | 77.63 | 60% | 1.39 | 1.15 | 2.62 | 1.14 | 1.23 | -1.57% | -1.91% | 4.88% / 4.86% | Highest scanner score among these, but current intraday weakness and wider range make it less boring. Identity needs confirmation: scanner description reads “BNB Standard Corp”. |
| RNG | 67.60 | 60% | 0.86 | 0.38 | 2.38 | 0.53 | 1.53 | -0.05% | +0.71% | 4.08% / 4.11% | Mixed intraday direction and wide range; watch pending chart/catalyst checks. |
| TRMD | 55.78 | 60% | 0.54 | 0.83 | 0.75 | 4.19 | 0.21 | -1.11% | -0.64% | 2.63% / 2.64% | Current ON move 4.19%, above preferred <3% range; WATCH/too-hot review. |
| EVR | 53.69 | 65% | 0.37 | 0.46 | 0.66 | 5.83 | 0.29 | +1.95% | +4.39% | 4.98% / 5.10% | Current ON move 5.83% and strong 1M move make it too hot for boring-first selection. |
| MANH | 54.78 | 60% | 0.71 | 0.27 | 1.21 | 7.06 | 0.50 | -3.99% | -2.86% | 5.43% / 5.49% | Current ON move 7.06% plus negative intraday movement; deprioritize. |
| OSW | 53.58 | 60% | 0.29 | 0.15 | 1.21 | 6.73 | 0.92 | +2.26% | +6.00% | 5.78% / 6.00% | Too hot with wide intraday range; not a boring-first candidate. |
| STNG | 54.20 | 60% | 0.56 | 0.22 | 1.20 | 6.53 | 0.64 | -2.62% | -2.73% | 5.47% / 5.47% | Current ON move 6.53% and negative intraday behavior; deprioritize. |
| MFM | 61.15 | 80% | 0.44 | 0.41 | 0.71 | 0.58 | 0.27 | -0.97% | -0.97% | 0.97% / 0.97% | Only 8 MCC bars and last observations around 12:25–12:28 Arizona, about three hours old at review time. DATA STALE/UNKNOWN, not actionable. |

## Operating interpretation

1. RTO is the first chart-review candidate, not a buy. Confirm whether the negative intraday move is ordinary noise or deterioration.
2. AMT is second; its 4.36–4.37% range is materially wider than RTO.
3. SXC is third; its 6.20% range is the main reason not to select it first despite the user's initial preference.
4. BNC has the highest scanner score among these names, but scanner rank is not the same as boringness or execution quality. Confirm ticker/instrument identity first.
5. TRMD, EVR, MANH, OSW and STNG are above the preferred below-3% current ON range; deprioritize for boring-first selection.
6. MFM fails freshness/completeness checks.
7. Every supplied MCC row is OBSERVED · NOT QUALIFIED. None is an MCC-qualified entry event.

The red EOD: GET OUT state at 12:55 Arizona is intentional for the user's live dashboard and must not be treated as an unexpected scanner anomaly.

Before an overnight READY decision, verify symbol identity, fresh data, price >= $5, 20-day average volume >= 500,000, ON win rate >= 60%, 20D average ON return >= +0.10%, 5D ON return > 20D ON return, price above 20D SMA, full exclusion-list status, catalyst status, and relevant 5M/1M states. Unknown evidence => WATCH or DATA?, never READY.

## [Uncertainties]

- Individual chart screenshots were not readable through the current GitHub image retrieval route; this assessment uses the readable scanner screenshot and user-provided MCC table only.
- Catalyst status, average daily volume, price vs. 20D SMA, spread, and complete exclusion-list status are not established by the supplied figures.
- BNC instrument identity remains uncertain.
