# Delayed historical SIP Daily — first runtime evidence (2026-11-14)

**Run by:** Claude on the operator machine, Development secrets, MCC launcher, code = `6bf7ea5` + uncommitted delayed-SIP change (committed afterwards in the same pass).
**Config:** `MarketData:Alpaca:DailyFeed = sip_delayed` (request end = now − 16 min), `adjustment=all`.
**Operator decision:** 2026-11-14, delayed historical SIP allowed for completed Daily bars only.

## API results (raw JSON in this folder)

| Symbol | feed | feedCoverage | sipRequestStatus | sessions | fresh | qualificationUsable | latest session |
|---|---|---|---|---|---|---|---|
| RTO | sip | CONSOLIDATED_SIP | SIP_DELAYED_HISTORICAL_OK | 20/20 | true | true | 2026-11-13 |
| EVR | sip | CONSOLIDATED_SIP | SIP_DELAYED_HISTORICAL_OK | 20/20 | true | true | 2026-11-13 |
| CRBG | sip | CONSOLIDATED_SIP | SIP_DELAYED_HISTORICAL_OK | 20/20 | true | true | 2026-11-13 |

The free Alpaca account accepted the SIP Daily request. Earlier HTTP 403 results were on recent SIP data.

## Comparison with the independent reference (S&P Global via Stock Analysis)

Reference CSVs: `../20261106T215302Z/{RTO,EVR}-independent-reference.csv` (split-adjusted). The API values are all-adjusted. Per the 2026-11-06 qualification doc, no ex-dividend dates fall inside the window, so the two bases are treated as equivalent for these dates. Overlap: 15 sessions per symbol (2026-10-19 … 2026-11-06).

| Symbol | Overlap | Volume ratio API/reference | Max abs O/H/L diff | Close diffs |
|---|---|---|---|---|
| RTO | 15 | 1.0000 – 1.0004 | 0.005 | 0.00 on 14 sessions; −0.02 on 2026-11-06 |
| EVR | 15 | 1.0001 – 1.0011 | 0.005 | 0.00 on 14 sessions; +0.18 on 2026-11-06 |

Earlier, with IEX on the same window, volume was a fraction of the consolidated reference, and qualification failed.

## Interpretation

- Volume now agrees within 0.11%, and prices agree within half a cent, except one close each. The reference rows have 2 decimals, so the 0.005 differences are consistent with rounding.
- **2026-11-06 close differences:** the reference was captured at 2026-11-06 21:53 UTC, 53 minutes after that session closed. The final official close may not have been published yet. This explanation is unconfirmed: re-download the 2026-11-06 row from the reference and compare.
- Under the existing strict rule (price ±0.0001, volume exact), this is still **FAIL**. Whether a rounding tolerance (±0.005) and a volume tolerance (for example ±0.2%) are acceptable is an operator decision.
- This is source-parity evidence only. It does not make any candidate READY.
