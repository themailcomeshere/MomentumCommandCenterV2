# Daily Market-Data Qualification — RTO and EVR — 2026-11-06

**Branch:** `feature/opportunity-quality-classification`  
**API endpoint:** `GET /api/market-data/daily?symbol={RTO|EVR}`  
**Dashboard:** main `frontend/index.html`, section **Daily data audit**  
**Qualification:** NOT YET PASSED — local API has not been queried from this session and SIP remains unconfirmed.

## 1. Findings and implementation changes

The existing Daily client requests Alpaca `1Day` bars with `adjustment=all`, builds the exact latest 20 expected US-equity sessions from `UsEquitiesSessionCalendar`, excludes the current incomplete session until its scheduled close (including early closes), and suppresses qualification price/SMA20/average-volume fields unless feed is SIP.

The review identified a zero-bars failure path: freshness used `Max()` on the available session keys. An empty but otherwise valid provider response could therefore produce an API 500 rather than a DATA? response. The client now determines freshness from the required latest expected session itself and returns an incomplete response when no bars exist.

The Daily response now exposes:
- `latestExpectedSessionDate`
- `expectedSessionDates` (exact ordered 20-session window)
- `missingSessionDates`
- `sessions` (each session date, source timestamp, OHLC and volume)
- Existing retrieved timestamp, provider, feed, adjustment, freshness, completeness, observed metrics, qualification metrics and feed-coverage status.

This makes the API independently auditable: an operator can recalculate the 20-session close average and volume average from the actual returned rows instead of trusting aggregate fields.

The Dashboard now has a dedicated RTO/EVR Daily audit panel. It displays provider/feed, adjustment, latest expected session, API retrieval timestamp, observed close, observed SMA20, observed 20-session average volume, freshness, qualification status and missing session dates. IEX observations are visibly diagnostic; they do not become SIP qualification values.

Tests were extended to cover session-level payloads, exact expected/missing session dates, arithmetic, IEX qualification suppression, and a zero-bars response. These are synthetic unit tests, not live-provider acceptance.

## 2. Independent reference sample — 20 sessions through 2026-11-06

A separate historical reference sample was assembled from Stock Analysis historical pages. Those pages identify S&P Global Market Intelligence as the underlying historical data source and state that historical prices are adjusted for stock splits. The rows are stored in:
- `data/validation/daily-reference-20261106/RTO.csv`
- `data/validation/daily-reference-20261106/EVR.csv`

The reference CSV schema is:
`SessionDate,Open,High,Low,Close,Volume,Adjustment`

All 20 sessions are ordered oldest-to-newest from 2026-10-12 through 2026-11-06. The 20 rows are the same expected dates for both symbols.

| Symbol | 2026-11-06 close | Independent 20-session close SMA | Independent 20-session average volume |
|---|---:|---:|---:|
| RTO | 23.63 | 22.3130 | 1,616,207.4 |
| EVR | 284.47 | 272.2235 | 643,488.4 |

These SMA and average-volume figures were independently recalculated from the 20 CSV rows. They are reference values, not values returned by the local MCC API.

### Adjustment-basis qualification for this sample

The public historical pages state split-adjusted prices; the MCC API requests Alpaca `adjustment=all` (split and dividend adjusted). These labels are not generally interchangeable.

For this specific 2026-10-12 through 2026-11-06 window, the known dividend ex-dates are outside the window:
- EVR: latest completed ex-dividend date was 2026-08-28; the next declared dividend has a 2026-11-27 ex/record date, after this sample window.
- RTO: Rentokil's published interim-dividend ex-dividend date was 2026-08-13, before this sample window.

Therefore the split-only versus split-and-dividend basis is expected to be equivalent over these 20 selected sessions, subject to confirming the provider's corporate-action history and absence of an in-window split/action. The qualification runner requires an explicit `-ConfirmWindowAdjustmentEquivalence` switch before it will compare the split-adjusted reference CSV to the API's all-adjusted values. Do not reuse that switch for another date window without rechecking corporate actions.

## 3. Run the local API validation

After pulling this branch, rebuilding and restarting the API, run:

```powershell
.scripts\validation\Test-DailyMarketDataQualification.ps1 `
  -BaseUri "http://localhost:53360" `
  -Symbols RTO,EVR `
  -ReferenceDirectory ".\data\validation\daily-reference-20261106" `
  -ReferenceProvider "Stock Analysis / S&P Global Market Intelligence" `
  -ReferenceAdjustment "split-adjusted" `
  -ConfirmWindowAdjustmentEquivalence
```

The script:
1. Checks API health and records the effective Alpaca feed.
2. Calls the Daily endpoint for RTO and EVR.
3. Saves each complete JSON response, including the 20 underlying daily rows.
4. Checks exactly 20 distinct, ordered expected sessions and zero missing dates.
5. Recalculates latest close, SMA20 and average daily volume from those rows.
6. Checks that the API's observed metrics equal those independent recalculations.
7. Verifies non-SIP feeds cannot expose qualification fields or set `qualificationUsable=true`.
8. Compares every date and OHLC field against the independent reference within $0.0001 and volume exactly.
9. Saves a timestamped evidence directory under `artifacts/daily-data-qualification/`.

**Interpretation:**
- `PASS_INTERNAL_CONSISTENCY` means the API's aggregate fields reconcile with its own 20 returned rows. It is not independent source parity.
- `PASS_EXACT_DATES_OHLCV_WITHIN_PRICE_TOLERANCE` means the independent reference rows match all 20 API session dates and OHLCV values under the documented tolerance/basis.
- `qualificationUsable=true` is allowed only when the effective Daily feed is SIP, all 20 expected sessions are present, and the latest expected session is fresh.
- With IEX, the independent comparison can still diagnose data differences, but the result remains non-qualifying and the UI must show DATA?.

## 4. Dashboard acceptance

Open the main Dashboard at `http://localhost:53360/`, switch to the Boring Overnight workspace, and use the **Daily data audit** panel.

1. Leave the symbols as `RTO,EVR`.
2. Click **Validate Daily data**.
3. Confirm two rows appear and each reports Alpaca, the configured feed, `all (split/dividend adjusted)`, the latest expected session and retrieval timestamp.
4. Confirm `20/20`, `NONE` missing sessions, observed close, SMA20, average volume and `FRESH` only if the API returned all required evidence.
5. With IEX, confirm the panel says `DATA? · IEX_PARTIAL` / diagnostic-only, not qualification usable.
6. Confirm the existing Overnight Qualification decision remains DATA? and Final READY remains zero unless all other independent gates are satisfied. This Daily audit alone cannot make a candidate READY.

Save a screenshot and the script's complete evidence folder. The browser panel and script must show the same feed, adjustment, session count, values and retrieval timestamp.

## 5. Current status

- [x] Reviewed the Daily client, API route, existing unit tests, Overnight evaluator wiring and Dashboard rendering.
- [x] Fixed empty-history freshness handling.
- [x] Exposed individual session rows and expected/missing dates in the API response.
- [x] Added a dedicated Dashboard RTO/EVR Daily audit panel with source and freshness provenance.
- [x] Added tests for complete, incomplete, IEX and empty-bar behavior.
- [x] Added a PowerShell qualification/evidence runner with independent CSV comparison.
- [x] Added 20-session independent RTO and EVR reference CSVs and recalculated baseline metrics.
- [ ] Run the Release build/tests against this branch after the latest changes.
- [ ] Run the script against the local API and inspect actual RTO/EVR payloads.
- [ ] Confirm exact independent OHLCV parity for all 20 sessions.
- [ ] Confirm SIP entitlement/feed and qualification status.
- [ ] Verify the Dashboard screenshot against the saved API evidence.

## Sources

- Stock Analysis / S&P Global Market Intelligence — [EVR historical prices](https://stockanalysis.com/stocks/evr/history/)
- Stock Analysis / S&P Global Market Intelligence — [RTO historical prices](https://stockanalysis.com/stocks/rto/history/)
- Evercore dividend evidence — [Slickcharts EVR dividend history](https://www.slickcharts.com/symbol/EVR/dividend), and [Evercore Q3 2026 SEC filing](https://www.sec.gov/Archives/edgar/data/1360901/000162828026070100/evr-20260930.htm)
- Rentokil Initial official dividend page — [Dividend history and ex-dividend dates](https://www.rentokil-initial.com/investors/shareholder-centre/dividends.aspx)

## [Uncertainties]

- Local Alpaca Daily responses and effective runtime feed have not been retrieved in this session.
- Independent reference data is sourced from S&P Global Market Intelligence as presented by Stock Analysis; its disclosed historical adjustment basis is split-adjusted, not explicitly dividend-adjusted.
- Window adjustment equivalence relies on the published corporate-action dates and must be rechecked for a different 20-session window.
- Exact API/reference OHLCV parity, SIP qualification and browser acceptance remain unverified until the operator runs the script and Dashboard.


---

## Runtime evidence update — 2026-11-06 21:53 UTC

**Evidence directory:** `artifacts/daily-data-qualification/20261106T215302Z/`  
**Run status:** internal consistency PASS for both symbols; independent-source comparison FAIL for both.  
**API health:** OK; decision-support-only; order submission NOT IMPLEMENTED.  
**Configured and returned feed:** Alpaca IEX.  
**Adjustment:** `all (split/dividend adjusted)`.  
**Expected/returned sessions:** 20/20 for each; zero missing; latest expected date 2026-11-06; fresh at retrieval.

| Symbol | API close | API SMA20 | API ADV20 | Independent close | Independent SMA20 | Independent ADV20 | Result |
|---|---:|---:|---:|---:|---:|---:|---|
| RTO | 23.615 | 22.31175 | 134,700.5 | 23.63 | 22.3130 | 1,616,207.4 | FAIL |
| EVR | 284.600 | 272.18775 | 50,448.1 | 284.47 | 272.2235 | 643,488.4 | FAIL |

The script's row-level diagnostics report multiple OHLC mismatches and volume mismatches on all 20 sessions for both symbols. The API's observed metrics exactly recalculate from its own 20 session rows, so this is not an internal arithmetic defect. It is an external source-parity failure.

**Interpretation:** the observed volume scale is consistent with IEX partial-exchange coverage being compared with the independent source's consolidated volume. The price differences are also consistent with different trade coverage; this evidence does not identify which individual print is authoritative. Keep IEX Daily values diagnostic-only. Do not use the volume values for qualified liquidity, and do not relax tolerances or overwrite the independent CSVs to make the run pass.

The source adjustment labels differ (Alpaca `all`, reference `split-adjusted`). The corporate-action checks documented above place known ex-dividend dates outside this sample window, but this does not convert the IEX series into consolidated data or explain the large volume mismatch.

### Acceptance status

- [x] Daily qualification script executed and evidence committed.
- [x] Exact 20 expected dates, zero missing dates and freshness confirmed.
- [x] API observed close/SMA20/ADV20 internally reconciled to returned session rows.
- [x] Fail-closed behavior confirmed: qualification fields are null and status is DATA? / IEX_PARTIAL.
- [ ] Independent OHLCV parity — FAILED; requires a consolidated, entitlement-authorized source comparison.
- [ ] SIP entitlement/feed identity and consolidated Daily parity — not established.
- [ ] Main Dashboard screenshot/browser comparison — not supplied; Data not found in sources.

Raw evidence is authoritative for this run: `RTO-daily-api.json`, `EVR-daily-api.json`, both summary JSON files, and `daily-qualification-summary.json` in the evidence directory.

## [Uncertainties]

- IEX partial coverage is the likely explanation for the volume-scale difference, but the evidence does not establish exact venue-level composition for each session.
- OHLC discrepancies cannot be assigned to an incorrect source without an independently authorized consolidated reference.
- Split-only versus all-adjusted equivalence is sample-window-specific and must be rechecked for any later period.
- SIP access and Dashboard browser acceptance remain unverified.
