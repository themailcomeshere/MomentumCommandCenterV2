# Full Master To-Do Audit — 2026-11-03

Branch: `feature/opportunity-quality-classification`  
Audit head: `89b9fe73408cee06f0ddfedc1486b7038d335a2a`

This audit reconciles all 138 unchecked Master-To-Do entries by workstream. Repeated/historical entries are not independently re-run or blindly marked complete. A source implementation, unit test, runtime test, browser acceptance, and Thinkorswim acceptance are separate evidence levels.

## Completed in this pass
- [x] User ran `Get-Latest-And-Test.ps1` at `0917a90...`: Release build succeeded, 179/179 tests passed, 0 failed, 0 skipped.
- [x] User confirmed the committed 50-row Overnight Scanner CSV imported and Red Zone refreshed; MHK, OSW, EVR and EHC appeared.
- [x] User accepted prior prototype title/clock/transport-label layout.
- [x] Fixed prototype horizontal overflow at desktop (104px dock) and mobile (78px dock), commit `3aac28cf638228fd0901251e59969d08af248e17`.
- [x] Corrected standard-time EOD constant to 17:55 Eastern in repository 1M and 5M chart-study copies; daylight remains 18:55 Eastern.
- [x] Exact-head Actions run [41575143773](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41575143773) passed restore/build/test on `89b9fe73408cee06f0ddfedc1486b7038d335a2a`.

## 1. EOD parity — important source identity correction
Both repository study copies use the same US DST calendar rule and now map Arizona 3:55 PM to 18:55 EDT / 17:55 EST. However, `docs/Charts/1M/MainStudy.txt` identifies itself as V14.4.11, not the full V14.5.1 Library study previously identified as the normal execution baseline. The full `Momentum_Command_Center_V3_2_1M_V14_5_1.ts` was not found in the repo tree or Project file search. Do not install V14.4.11 as a replacement.

- [x] Repository 5M V4.1.7 source constant corrected.
- [x] Repository 1M V14.4.11 source copy corrected (not the current full V14.5.1 baseline).
- [ ] Export/upload the full V14.5.1 source; patch its standard-time constant to 1755 while retaining 1855 daylight and the independent 1M VWAP execution gate.
- [ ] Compile full V14.5.1 and V4.1.7 in Thinkorswim.
- [ ] Matched-bar diagnostics in both regimes: Arizona 15:54 / 15:55 / 15:56; Eastern 17:54 / 17:55 / 17:56 EST and 18:54 / 18:55 / 18:56 EDT. Record bar intervals, DST flag, selected cutoff and EOD state on both studies.
- [ ] Isolate screenshot bubbles by toggling one study at a time. A screenshot bubble alone does not identify its generating study.

## 2. Overnight shortlist — source-backed review
The committed CSV has no current price or 20D volume columns. External historical/statistics pages provide a cross-check only; no fresh Daily/5M/1M charts were supplied for November 3.

| Symbol | Price evidence | 20D average volume | Gate | Supplied Nov 2 5M state | Disposition |
|---|---:|---:|---|---|---|
| OSW | $30.22 close Nov 2 | 806,703 | price/volume pass | WAIT, Permission NO, BEARISH; close 30.02, VWAP 30.37, RSI 29.5, RVOL 16.52, extension 3.35 ATR | WATCH/REJECT: Oct 28 earnings/guidance catalyst; ~31.85% 20-session gain, RSI 84.71 |
| EVR | $303.26 close Nov 2 | 641,313 | price/volume pass | WAIT, Permission NO, BEARISH; close 303.26, VWAP 308.69, RSI 32.8, RVOL 21.95, extension 2.98 ATR | WATCH/REJECT: Oct 28 earnings catalyst and sharp post-results move |
| MHK | $150.01 reported Nov 3 | 1,336,567 | price/volume pass | WAIT, Permission NO, NEUTRAL; close 148.60, VWAP 148.59, RSI 51, extension 0.25 ATR | WATCH only: Oct 29 earnings/weak demand outlook plus buyback; current ON 3.13%, 5D ON mean 2.49% vs 20D 0.31% |

Sources: OSW https://stockanalysis.com/stocks/osw/history/ and https://stockanalysis.com/stocks/osw/statistics/; EVR https://ca.finance.yahoo.com/quote/EVR/history/ and https://stockanalysis.com/stocks/evr/statistics/; MHK https://www.marketscreener.com/quote/stock/MOHAWK-INDUSTRIES-INC-13521/quotes/ and https://stockanalysis.com/stocks/mhk/statistics/.

- [x] Price >$5 cross-check passes for all three.
- [x] 20D average volume >=500K cross-check passes for all three.
- [x] No candidate has 5M Permission YES in supplied screenshots.
- [x] Recent earnings catalysts identified for all three.
- [ ] Exclusion status: Data not found in sources. Local `App_Data/exclusions-watchlist.json` is runtime-only and complete Schwab list is not reconciled.
- [ ] Obtain current Daily/5M/1M charts for a current-session re-review.
- [ ] Obtain/import the full 248-row CSV; the committed file contains only 50 rows.

**Disposition: zero qualified BUY/HOLD candidates.** OSW/EVR have bearish 5M structure and Permission NO. MHK has neutral structure, Permission NO and an overly hot overnight move.

## 3. Thinkorswim scan ingestion — impediment
The public Schwab Trader API is a separate developer product describing market data, account information, orders and transactions; it does not document retrieval of custom saved Thinkorswim scan results or custom scanner columns. No public supported TOS scanner API/webhook was found in official developer materials: https://developer.schwab.com/products/trader-api--individual and https://developer.schwab.com/user-guides. Do not scrape the desktop UI.

Feasible automation bridge: a local watched-folder CSV adapter in MCC (configurable drop folder, atomic file handoff, hash/idempotency, archive/reject folders, parse diagnostics, source timestamp, audit record, status endpoint); keep manual upload fallback. It is not implemented yet. Alpaca movers are not a substitute for the custom Overnight scan.

## 4. Alpaca collector / SIP
- [x] Source calls `v1beta1/screener/stocks/movers?top=50`; this is bounded top gainers/losers, not a full-market scan.
- [x] Poll interval is clamped to 30–3600 seconds; events are persisted through provider-neutral ingestion.
- [x] User identified `secrets.json` as the feed override; SIP request returned HTTP 403 entitlement denial; IEX restored and returned validated data (62 bars).
- [ ] During regular market hours, capture `/api/scanner/discoveries` twice 5–10 minutes apart and verify last success/source timestamp, status, failures, symbol timestamps and persistence.
- [ ] SIP acceptance remains blocked until Alpaca grants recent SIP entitlement; do not label SIP healthy from configuration alone.
- [ ] Validate complete-market coverage, terms, rate limits and backfill before production scanner sign-off.

## 5. Boring Overnight / Red Zone truth
Already implemented: fail-closed hard-gate evaluator; explicit 20-session close/open history calculator; history service that overwrites caller-entered historical values or nulls them; candidate snapshots; local exclusion CRUD; quantile sell-limit calculator that requires 20 complete windows and ATR parity MATCH; Alpaca news client that returns unclassified headlines.

Still missing: daily history provider for price/SMA20/20D volume; complete reconciled exclusions; validated catalyst classification/coverage; matched current 5M/1M source evidence; immutable full-universe snapshots; GTC_EXT 07:00–20:00 ET outcome coverage; walk-forward target quantile; integration to Red Zone.

The existing `OpportunityQualityAssessor` is a diagnostic classifier, not a versioned automatic top-four ranker. Required remaining ranking work: version/config, 5M same-slot RVOL, daily liquidity, 1M execution state, momentum/extension/freshness, explicit reasons, Manual/Automatic mode, rank hysteresis and alert deduplication. Keep this rank separate from Boring Overnight qualification and 5M Permission.

- [ ] Catalyst remains UNKNOWN until a validated source is connected; caller assertion must not set CLEAR.
- [ ] Known exclusion subset in docs: SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE; not verified exhaustive and not currently seeded into runtime store.
- [ ] Boring Overnight remains WATCH/manual research, not READY.
- [ ] Calculated Limit Sell remains DATA?/not calculated. +0.5% is a heuristic, not a target.

## 6. Other Master-To-Do workstreams (138 unchecked entries reconciled)

| Workstream | Current source truth | Remaining acceptance |
|---|---|---|
| WebSocket | Subscription/worker implementation exists; after-hours SUBSCRIBED is not delivery proof | Regular-session per-symbol counts/timestamps for SPY/RKLX/ASTX/XRPN; verify eligible 1M WS selection and 5M REST authority |
| Collector recovery | Durable observations, watermarks, overlap polling, retries/checkpoints exist | Controlled outage/restart/checkpoint reload/sleep-wake on target host |
| Main Chart/feed parity | Feed-source diagnostics exist; SIP entitlement denied | Current feed status, bar age, exact TOS/MCC matched Close/VWAP/ATR14/extension |
| Morning Brief | Immutable version + SHA-256 snapshot store and market-bar watermarks exist | News/discovery watermarks, raw response references/retention, dependency manifest, outcome linkage; browser modal/export tests |
| PMI/provider comparison | Sparse-data protections implemented in code | Re-run one-bar insufficient-data case, liquid control, provider/feed/gap metadata, date persistence and July/non-Arizona timezone test |
| Schwab positions | Individual API approval reported | User app creation, Accounts & Trading Production, exact callback, Active/Ready status, server-side OAuth and read-only positions; no order writing |
| Packaging/retention | Browser-hosted API is current | Decide desktop/local service, installer/startup, data directory, retention, backup/restore |
| Production scanner/provider | Alpaca movers bounded to top 50; SIP denied | Licensing, entitlement, full-market coverage, backfill, rate limits and representative-symbol validation |

## Second pass — newly unblocked vs still blocked
- [x] Local Windows get-latest/build/test closed by user evidence (179/179).
- [x] 50-row CSV import and Red Zone refresh closed by user evidence.
- [x] Prototype overflow code fix committed; browser confirmation pending.
- [x] EOD standard-time source constant corrected in repository study copies; CI passed.
- [ ] Full V14.5.1 1M source, TOS compilation and matched-bar outputs still require user-supplied study/TOS evidence.
- [ ] Full 248-row export still requires user export/commit.
- [ ] TOS scan API remains unsupported/documented; watched-folder CSV bridge is future implementation.
- [ ] SIP entitlement requires Alpaca account change.
- [ ] Catalyst/exclusion/daily history/GTC_EXT and target validation require source data.
- [ ] Regular-session runtime/browser/recovery acceptance requires user's running Windows environment.
- [ ] Schwab OAuth requires app/callback setup and secure user-side credentials.

## Next operator actions
1. Pull latest branch and hard-refresh prototype; confirm scrollbar gone.
2. Export/upload the full V14.5.1 1M ThinkScript source. Do not send credentials.
3. Export/commit the complete 248-row TOS CSV with the original metadata preamble intact.
4. In TOS, compile V4.1.7 and full V14.5.1; run the before/at/after DST matrix above and provide the diagnostic output.
5. During regular hours, capture collector status/discoveries before and after 5–10 minutes.
6. Upload current Daily, 5M and 1M charts for OSW/EVR/MHK if they remain on the shortlist.
7. Continue only with a source-backed exclusion list and validated catalyst source; otherwise all three remain non-qualified.

## [Uncertainties]
- Full V14.5.1 source was not retrievable from repository or Project file search; V14.4.11 is not a substitute.
- Public market statistics are secondary-source cross-checks, not MCC authoritative feed data.
- Current exclusion status for OSW/EVR/MHK is unknown.
- No supported public TOS saved-scan API/webhook was found; a private Schwab arrangement could change this.
