# MCC Next Steps and External Dependencies

**Updated:** 2026-10-23 (Arizona time)
**Canonical companion to:** `docs/Master-To-Do.md`
**Purpose:** Separate development work that can proceed locally from user decisions, account access, and validation that cannot be fabricated.

## Direct answer: do you need both provider accounts?

**No. Create neither paid subscription yet.** MCC can continue to be developed and tested without vendor credentials. Accounts are needed to verify real responses and entitlements, not to write most of the infrastructure.

The current code has historical REST adapters for Alpaca and Massive. It does not yet have a live streaming collector, scanner integration, broad movers, quote/spread feed, news/catalyst source, or production recovery/backfill pipeline.

Suggested sequence:
1. Create one free Alpaca account first. Use it for initial API plumbing and schema/provenance tests; never treat its free-tier results as production-grade market authority. Defer Massive until a second provider materially improves comparison.
2. Configure one provider at a time with server-side environment variables and test the adapter against real responses. Do not put keys in source control, browser JavaScript, screenshots, or chat.
3. Keep paid subscriptions on hold until we complete the documented proof-of-fit comparison and verify personal-use/display rights, historical retention rights, extended-hours behavior, rate limits, scanner/mover coverage, and exact plan entitlements.
4. If you prefer to minimize setup, start with one free provider. We can implement the provider-neutral contracts and test harness first, then add the second account when it materially improves the comparison.

The current research shortlist is Alpaca Algo Trader Plus and Massive Stocks Advanced, but neither is selected or verified. See `docs/Provider-Comparison.md`.

## User actions that genuinely require your input

### A. Provider access (only for live-data verification)
- [ ] Create a free Alpaca account and generate an API key/secret. No paid plan is needed for this first smoke test.
- [ ] Configure credentials locally using .NET User Secrets; exact commands are in `src/MomentumCommandCenterV2.API/README.md`. `appsettings.json` is for non-secret defaults only. Do not send secrets in chat or commit them.
- [ ] Confirm which feed the account actually allows. Keep `iex` unless the account explicitly entitles you to another feed; do not configure SIP unless entitled.
- [ ] Run the API locally, inspect `/api/providers/status`, then run a small `/api/market-data/compare` request. Report sanitized status/error messages and observed timestamps only.
- [ ] We will not call the provider "verified" until real authentication, feed entitlement, timestamps and coverage are checked. Defer Massive account setup until a second provider is needed.

### B. Thinkorswim parity evidence (required before trusting trading gates)
- [x] Received the XRPN 2026-10-02 screenshot with normal 5M study, Authority Diagnostic V2 and readable time axis covering the target window (user clarified the top-left chart date can be ignored). Do not request this screenshot again.
- [ ] For the UPC chart-gap question, either provide continuous 1M/5M OHLCV covering 2026-10-22 09:16–09:36 Arizona time, or inspect exact timestamps in TOS. This is only needed to settle that historical incident; it does not block adapter development.
- [ ] When asked for installed-study evidence, capture the study name/version and the relevant bars. A repository file does not prove that the same code is installed in TOS.

### C. UX feedback
- [ ] Review the hosted dashboard after running the API, not just the static HTML. Report any layout or wording issues. Browser rendering has not been independently automated/verified.
- [ ] Confirm whether the proposed left Watchlist/News rail is wanted only after seeing the current responsive layout; the spec intentionally leaves it undecided.
- [ ] Supply your actual trading window only when we implement time-window-specific ranking; do not block the data foundation on it.

## Work I can continue without your accounts or feedback

These tasks should not wait for provider credentials:
- [ ] Add provider pagination, bounded retries, timeouts, rate-limit/error normalization, and tests with sanitized fixture payloads.
- [ ] Add explicit provider-neutral stream/collector interfaces and a background hosted-service lifecycle with RUNNING / DEGRADED / STOPPED / UNKNOWN health. Keep real vendor stream bindings marked NOT CONNECTED until tested.
- [ ] Add collector health and durable ingestion checkpoints, process-restart behavior, duplicate/late/correction handling, and deterministic shutdown tests.
- [ ] Improve raw-bar storage validation, corrupt-log behavior, storage error health reporting, retention/rotation design, and store tests.
- [ ] Define market-session-aware gap classification without labeling an interval as a data defect until the calendar, eligible session and provider semantics are known.
- [ ] Build the Morning Brief pipeline incrementally: benchmark observations and freshness first; then broad market movers, sector/breadth data, and news/catalyst provider contracts. Missing inputs must stay DATA? / NOT CALCULATED.
- [ ] Build a source-neutral scanner-event contract, candidate provenance, deduplication, re-ranking and continuation-watch workflow. Do not let ranking bypass 5M Permission or 1M READY.
- [ ] Implement Boring Overnight calculation contracts and deterministic offline calculations against fixtures; keep catalyst status UNKNOWN without an event source.
- [ ] Continue 5M/1M indicator parity audits and tests from versioned source-controlled ThinkScript. Do not change live trading gates without evidence from the installed studies.
- [ ] Update the canonical To-Do and handoff whenever implementation or verification status changes.

## Decisions that should not be guessed

- [ ] Final paid provider / plan and license rights.
- [ ] Whether the provider's session definitions and bar timestamp semantics match the exact Thinkorswim study behavior.
- [ ] Whether scanner endpoints cover the user's required small-cap and premarket universe.
- [ ] The exact production freshness limits, gap policy, and handling of halts / no-trade intervals.
- [ ] Whether the left Watchlist/News rail belongs in the final layout.
- [ ] Any strategy threshold changes or new continuation-entry rule.
- [ ] Whether MCC has achieved enough measured parity and operational recovery to replace TOS for decision support.

## Proposed milestone order

1. **Offline reliability:** pagination, retries/timeouts, fixture tests, store hardening, health lifecycle.
2. **One-provider smoke test:** configure one free account; verify auth, response schema, source timestamps and historical window. Add the second provider for direct comparison.
3. **Live observation collector:** stream + REST reconciliation, reconnect/backfill, raw data persistence, health/freshness.
4. **Market Brief:** benchmark health, broad discovery/movers, sectors/breadth and sourced catalyst/news data.
5. **Signal parity:** replay same symbol/date against source-controlled and installed Thinkorswim studies; preserve 5M authority / 1M execution separation.
6. **Decision layers:** Red Zone and Boring Overnight from verified inputs, reason codes and DATA? states.
7. **Shadow validation:** run MCC alongside TOS across representative sessions; log differences and missed/false candidates before relying on it.

**Release gate:** MCC is not a TOS replacement for live decisions until data entitlement, live freshness, recovery/backfill, timestamp/session parity, study parity, and failure-state behavior have been tested. No automated order placement is in scope.

## [Uncertainties]

- Provider plan prices and entitlements can change and have not been confirmed in the user's accounts.
- No account credentials, license terms, or live API response payloads were supplied; live compatibility is unverified.
- The user's final dashboard layout preference remains open. The Boring Overnight order window has been user-defined as 07:00–20:00 Eastern for GTC_EXT; Arizona entry timing is separately documented. Do not treat layout preference as a blocker for backend work.

## 2026-10-26 export audit reconciliation

The files already committed to `feature/opportunity-quality-classification` have been reviewed. Do not ask the user to re-upload the same files.

- The October 19 XRPN 1M/5M pair is available and useful for OHLCV coverage checks. Its 1M export has 30 absent minute timestamps from 09:00–12:59 Arizona; 27 complete post-09:00 5M windows exactly match aggregated 1M OHLCV.
- The October 22 1M report filename has been corrected to `data/StrategyReports_XRPN_10222026_1M_EntireDay.csv`; it is not the October 2 1M pair. The October 2 5M file remains unmatched by a verified October 2 1M full-day export.
- These reports contain raw OHLCV payloads, not internal ThinkScript READY/Permission diagnostics. To diagnose why READY is rare, the missing evidence is the gate state on the same bars (or an equivalent diagnostic export), not another screenshot of the already supplied October 2 chart.
- The current files do not cover the full GTC_EXT-eligible window for Boring Overnight target research. The prior placeholder rows in `eligible-window-high-excursions.csv` have been removed; it is now header-only and must not be used as market evidence.

See `docs/Validation/OHLCV-Opportunity-Readiness-Audit-20261026.md` for row counts, date/range checks, complete-window comparison, limitations, and next steps.

## October 2 upload reviewed — 2026-10-26

The new file was found on the feature branch and inspected. Despite the old filename containing XRPN, its report header and all embedded bars identify **HPK 1M**, dated October 2. It has 214 bars from 06:30–12:59 Arizona and duplicates the corresponding rows in the existing HPK multi-day export. I renamed it to `data/StrategyReports_HPK_10022026_1M_EntireDay.csv`. It does not complete the XRPN 1M/5M pair; the XRPN 5M October 2 file still lacks a verified same-symbol 1M partner.

The existing October 19 chart-parity worksheets contain chart entries for 20 bars per timeframe (1M: 10 MATCH / 10 ROUNDING ONLY; 5M: 9 MATCH / 11 ROUNDING ONLY). The remaining worksheet rows are unfilled. These results support sampled OHLCV transport mapping but do not resolve all timestamp semantics, missing-bar classification, or READY-state behavior.

Do not re-upload the HPK file. The remaining evidence needed is specifically:
1. For READY root cause: same-symbol/date XRPN 1M OHLCV plus matching 1M/5M ThinkScript gate diagnostics, if October 2 is the target event.
2. For Boring Overnight: full source-backed regular close and bars over the complete next GTC_EXT-eligible window, with gaps explicit; actual fills are a separate dataset.

## Release-readiness summary

See [Release-Readiness-2026-10-26.md](Release-Readiness-2026-10-26.md). Current posture is local prototype / diagnostic only, not approved for reliance on live decisions. The most useful next work is automated report-provenance validation, importer regression coverage, and completing offline replay/UI integration without weakening ThinkScript eligibility gates. User/Thinkorswim evidence is still required for XRPN October 2 READY diagnosis and full GTC_EXT-window overnight research.


## 2026-10-26 follow-up — XRPN pair and Alpaca smoke test

### XRPN October 2 evidence status

- [DONE — OHLCV PAIR] data/StrategyReports_XRPN_100226_1M_EntireDay_New.csv is the intended XRPN 1M export. It contains 391 consecutive minute timestamps from 06:30–13:00 Arizona. Paired with the October 2 XRPN 5M export, 78 complete 5M windows aggregate from 1M and match OHLCV.
- [OPEN — STUDY STATE] The files contain OHLCV transport payloads only. Per-bar Permission/block code/candidate/extension-danger/READY/re-arm/expiration diagnostic fields: Data not found in sources. If uploaded separately, provide the exact path; otherwise export diagnostic output for the same bars/timeframes.

### Alpaca smoke test — exact user steps

The saved Alpaca key/secret are the intended credentials. Do not paste them into chat. From the repository root:

1. Start the API: dotnet run --project src/MomentumCommandCenterV2.API.
2. Open /api/health and confirm the API responds.
3. Open /api/providers/status; confirm Alpaca is configured. This does not prove live authentication.
4. Call /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z using the host/port printed by ASP.NET Core. Inspect Alpaca status/error, bar count, first/latest source timestamps, feed, duplicates and gaps. Massive may report not configured; record separately.
5. Run dotnet test MomentumCommandCenterV2.sln --configuration Release independently. User-reported 87/87 tests passed is fixture/offline test evidence, not proof of real API connectivity.
6. Report only sanitized response fields and errors; remove credentials, authorization headers and tokens.

If Alpaca shows unconfigured, reset only the local User Secrets values using the commands in src/MomentumCommandCenterV2.API/README.md. Keep the iex feed unless the account explicitly entitles another feed. No paid plan or Massive account is needed for this first smoke test.

### Boring Overnight screenshot next step

Initial chart-review candidates from the 2026-10-26 Overnight Scanner screenshot: TTE, RTO, NNN, OKE, AHR, FMS; CCI is a secondary comparison. This is a research shortlist, not a qualification decision. Capture each on a 5-minute chart with extended hours enabled, same visible window: 2026-10-26 regular session through 2026-10-27 20:00 Eastern, ensuring the next-day 07:00–20:00 Eastern window and prior regular close are visible. For quantitative reach/touch research, follow with source-backed OHLCV export and explicit DATA_GAP intervals.


## Correction and local action — 2026-10-26

### Boring Overnight chart window correction

The prior request to chart 2026-10-26 through 2026-10-28 was incorrect. Do not use that future-facing window. User is capturing AHR, FMS, and NNN for 2026-10-21 through 2026-10-23; accept those screenshots for initial visual review. They do not by themselves establish full GTC_EXT target-touch research. Any next request must specify completed historical sessions and include the previous regular-session close plus the entire subsequent 07:00–20:00 Eastern window. Preserve missing bars as DATA_GAP.

### Alpaca smoke-test result and fix

The user supplied: /api/health OK; decision-support-only; order submission not implemented. /api/providers/status says Alpaca and Massive are NOT_CONFIGURED. /api/market-data/compare returned zero bars and zero persisted observations. Alpaca explicitly reports missing MarketData:Alpaca:ApiKey and ApiSecret or environment variables. Release tests: build succeeded; 87 passed, 0 failed, 0 skipped.

Tests are green, but live market-data verification failed at configuration. API project UserSecretsId is MomentumCommandCenterV2-API-Local. Existing secrets may have been saved for another project ID or Windows user. From the repository root, set the existing credentials locally (replace placeholders only in local terminal):

1. dotnet user-secrets set "MarketData:Alpaca:ApiKey" "<YOUR_EXISTING_ALPACA_KEY>" --project src/MomentumCommandCenterV2.API
2. dotnet user-secrets set "MarketData:Alpaca:ApiSecret" "<YOUR_EXISTING_ALPACA_SECRET>" --project src/MomentumCommandCenterV2.API

Do not paste actual secrets into chat or commit them. Restart API, verify /api/providers/status says Alpaca configured, then repeat compare endpoint. If still unconfigured, confirm same machine/user and exact project path. Massive can remain unconfigured.


## 2026-10-26 — Final Overnight review: exact remaining actions

### What the assistant did
- Inspected all three available chart images identified as AHR, FMS, and NNN.
- The visible chart headers show 2026-10-19 through 2026-10-21, not the intended 2026-10-21 through 2026-10-23. The images are retained as provisional Oct 19–21 visual evidence only; no date substitution or inferred data.
- Wrote the visual-only observations and explicit evidence gaps to docs/NewChatHandoff.md.

### Your exact next actions

1. In Visual Studio, stop the running API and start it again after saving User Secrets. Use the API project as the startup project / confirm the launch profile runs MomentumCommandCenterV2.API.
2. Open the same localhost origin/port used by that Visual Studio process: /api/providers/status and /api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z.
3. Send back sanitized results only: provider configured/status/error, returned bar count, first/latest source timestamps, feed name, and any persistence count. Never send the key, secret, authorization header, or token.
4. If provider remains unconfigured, check the API project's UserSecretsId is MomentumCommandCenterV2-API-Local and the Visual Studio launch environment is Development. Confirm the secrets were saved under the same Windows user account running Visual Studio.
5. For the overnight screenshots, first check whether the correct AHR/FMS/NNN files for Oct 21–23 are already present in Project Sources. If so, identify those exact files; if not, provide them when convenient. Do not repeat the invalid Oct 26–28 capture window.

### What remains blocked
- Date-correct visual conclusion for Oct 21–23.
- Full GTC_EXT target-touch study: source-backed regular close + complete subsequent 07:00–20:00 Eastern bars, missing intervals marked DATA_GAP.
- Any claim that +0.5% is optimal, or any live Overnight READY status, until full-window outcomes, catalyst review and required data gates are validated.
- Provider smoke test until actual Alpaca bars are returned by the Visual Studio-hosted API.


## 2026-10-26 — Updated exact actions: TTE bubbles and Alpaca validation

### TTE 1M bubbles — please do this first

1. On the TTE chart, use the lower 1M pane's Studies/Edit Studies control. Select the **1M Command Center study** (do not edit the upper 5M study).
2. Open its settings/gear and verify the input `showStateBubbles` is `yes`. Apply the setting.
3. Compare the exact 1M study name/version and inputs against the FMS lower 1M pane, which displays bubbles. Do not assume the two charts use the same installed script simply because their labels look similar.
4. If `showStateBubbles` was already yes and the script/version/inputs match FMS, report that result before changing code. The Library V14.5 source plots bubbles only on specific events/transitions; missing bubbles alone do not imply HOLD or SELL.

### Alpaca provider status

The user's SPY comparison already proved the Visual Studio-hosted API can retrieve and persist 5 historical 5M IEX bars for the requested 2026-10-23 UTC window. The remaining issue was the status endpoint: source code hardcoded `CONFIGURED_NOT_YET_VALIDATED` even after a successful fetch. I updated the feature branch to track successful non-empty responses and report `VALIDATED_DATA_RETURNED` plus the last successful validation timestamp/bar count. A new empty-response unit test was added; CI/local tests for these code changes are not yet confirmed.

After the updated branch is built/run:

1. Stop and restart the API in Visual Studio.
2. Repeat `/api/market-data/compare?symbol=SPY&timeframe=5m&from=2026-10-23T13:30:00Z&to=2026-10-23T13:50:00Z`.
3. Reopen `/api/providers/status`. Alpaca should show `validated: true`, `status: VALIDATED_DATA_RETURNED`, a UTC validation timestamp, and the last successful non-empty bar count.
4. If it still says `CONFIGURED_NOT_YET_VALIDATED`, the running app is likely not using the updated code or it was restarted after the compare (the validation state is process-local). Confirm the startup project/branch and request order.
5. Do not configure Massive for this test; it is not needed to validate Alpaca's IEX historical path.

### Historical Overnight chart set

User confirms the existing AHR/FMS/NNN screenshots are for 2026-10-21–2026-10-23. Do not request re-upload. Finish the date-correct visual analysis from those sources; target-touch rates/fills still require full-window source OHLCV.


## 2026-10-26 — concrete user actions and current findings

### Tonight's Overnight decision
1. Treat CCI and NICE as WATCH, not READY, based on the supplied scanner columns and charts. Do not force a trade because today's shortlist is thin.
2. Before reconsidering either, verify the exact unrounded AVG ON and 5D ON values; the current hard gate is 5D > 20D average. Verify 20-day average daily share volume >=500K, price >=$5, above 20 SMA, Schwab exclusions, fresh data and a source-backed earnings/news/corporate-action check.
3. Recheck current 5M permission/structure and 1M context. CCI currently shows 5M Permission NO / neutral structure. NICE shows Permission YES but neutral structure, 9.48 RVOL, execution environment NO and price below VWAP/EMAs/SMA50. Neither is ready from current evidence.
4. If catalyst state, volume, or required source freshness cannot be verified, record WATCH / DATA? and use zero position. Do not interpret scanner score as a trade authorization.

### Install the TTE EOD bubble display fix
1. Open the repository source docs/Charts/1M/MainStudy.txt at branch feature/opportunity-quality-classification.
2. In Thinkorswim, edit the lower 1M Command Center study (not the upper 5M study), replace its source with the updated 1M source, save/apply, and verify the study compiles.
3. Check TTE around the EOD window. The updated source should emit one red “EOD GET OUT” bubble when the 1M state first enters EOD_GET_OUT. This is a display-only event; entry/permission logic is unchanged.
4. If no bubble appears, capture the exact compiler message or confirm the installed study name/version and whether the chart has a real EOD state transition. Do not send account credentials or unrelated personal data.
5. Do not leave showHistoricalDebugBubbles enabled during ordinary use; it draws a diagnostic bubble on every bar and is intended only for tightly zoomed debugging.

### Provider validation
- The SPY historical smoke test has succeeded: 5 IEX 5M bars returned and 5 observations persisted for 2026-10-23 13:30Z–13:50Z; no duplicates/gaps/errors. The status endpoint reports VALIDATED_DATA_RETURNED after a non-empty response in the current process.
- Massive can stay unconfigured. The successful request does not prove stream availability, SIP entitlement, all-symbol coverage or full overnight history.

### Evidence needed later for quantitative target research
- For each sample: source-backed regular-session close and all eligible GTC_EXT bars for the following 07:00–20:00 Eastern window, with timestamps/source and any gaps preserved as DATA_GAP.
- Keep historical target touches distinct from actual fills. Do not claim the +0.5% heuristic is optimal without out-of-sample evaluation.


### Candidate-specific catalyst note
- CCI reported Q3 results and updated outlook after close on Oct 21, 2026. Treat as recent post-earnings context and inspect price/volume stability before considering a boring overnight hold: https://crowncastle.gcs-web.com/news-releases/news-release-details/crown-castle-reports-third-quarter-2026-results-and-updates.
- NICE's official IR events page currently lists no upcoming events, but that is not a full catalyst clearance: https://www.nice.com/investors.
- These checks do not change the result: CCI and NICE remain WATCH / NOT READY until all hard gates and current chart conditions pass.


## New actions after 2026-10-27 API / Boring Overnight work

### User actions
1. Pull the latest `feature/opportunity-quality-classification` branch only after reviewing its changes; do not merge the entire branch into master blindly.
2. Run `dotnet run --project src/MomentumCommandCenterV2.API --configuration Release` and open `/swagger` on the exact origin printed by ASP.NET Core.
3. In Swagger, run `GET /api/health`, then `GET /api/providers/status`, then the historical `GET /api/market-data/compare` request. Keep the response sanitized. A successful historical request does not validate websocket/streaming or all extended-hours windows.
4. Optionally exercise `POST /api/boring-overnight/evaluate` with known, timestamped candidate inputs. Treat DATA? as incomplete evidence and BLOCKED/WATCH as non-eligible; this endpoint is not yet wired to scanner or catalyst providers.
5. Install `/mnt/data/MomentumCommandCenterV3_2_1M_V14_5_EOD_Bubble_Fix.txt` in the lower 1M study. Inspect a chart that includes the bar immediately before the EOD transition. Send the resulting screenshot only if no EOD bubble appears; no compile error alone does not prove runtime behavior.

### Engineering actions still open
- Confirm CI for exact latest branch head and correct any failures.
- Wire Boring Overnight evaluator inputs to provider-backed/persisted data, and connect catalyst review; do not let missing inputs default to READY.
- Implement complete GTC_EXT collector and walk-forward target-touch research before choosing a numeric target formula.
- Resolve ThinkScript version/source parity; repository `docs/Charts/1M/MainStudy.txt` is V14.4.10 and is not the uploaded V14.5 source.
- Continue Alpaca proof-of-fit: pagination/retries, persistence semantics, extended-hours coverage, websocket/collector recovery and TOS parity. Massive remains unconfigured/deferred.


## New Chat Handoff — 2026-10-28 (Overnight Scanner + V3 implementation)

### Today's scanner review
- Review artifact: docs/Validation/Overnight-Scanner-Review-20261028.md.
- Preliminary Boring Overnight chart-review order: RTO, AMT, SXC. None is READY.
- SXC is not automatically best: MCC reports a 6.20% intraday range. RTO's reported range is 1.29%, with 70% ON win rate, +0.30% median ON return and +1.60% 5D ON return.
- TRMD (4.19% current ON), EVR (5.83%), MANH (7.06%), OSW (6.73%), and STNG (6.53%) are above the preferred <3% current ON range.
- MFM is stale/incomplete (8 bars; last around 12:25–12:28 Arizona). Treat as DATA? / not actionable.
- Confirm BNC identity; screenshot description says “BNB Standard Corp”.
- All supplied MCC rows were OBSERVED · NOT QUALIFIED. Scanner ranking does not override qualification.
- Red EOD: GET OUT at 12:55 Arizona is expected by the user's live-dashboard design.

### Implementation state and immediate order
1. **CI/build truth:** inspect GitHub Actions for the latest branch head after commit 65c83f7918200e413a5b20fb7942392357c7d193. A prior run failed with CS0136 and a fix was committed; current-head green status is not established by this review.
2. **Boring Overnight evaluator:** invalid-input hardening is committed, but daily/overnight metric computation, GTC_EXT session collector, catalyst provider and scanner discovery are not fully integrated.
3. **Scanner/Morning Report:** provider-neutral contract exists; no verified live market-wide scanner feed, background overnight collector, catalyst source, or fully automatic Morning Report is established. A browser tab is not overnight collection.
4. **Provider proof:** continue free Alpaca proof-of-fit only when the user runs local credentials/smoke test. Never request or store secrets in chat. Massive and paid plans remain deferred.
5. **TOS parity:** repository chart source does not prove installed-study parity. Preserve 5M authority / 1M execution hierarchy and exact ATR-extension gate; do not weaken gates to increase READY frequency.
6. **Boring Overnight hard gates:** current ON <3% is a preferred range/penalty, not a substitute for hard gates. Required evidence includes price, 20D volume, 20D/5D ON return, win rate, price vs. 20D SMA, exclusions, catalyst status, freshness and relevant 5M/1M states.
7. **No trade recommendation from today's table:** RTO, AMT and SXC are chart-review candidates only. Missing catalyst/volume/price-SMA/exclusion evidence means WATCH or DATA?, never READY.

### Known blockers / user actions
- No immediate credential or preference decision is needed to continue code work. For live-data smoke testing, configure free Alpaca locally using API README instructions and share sanitized responses only.
- Individual screenshots 1–12 were not readable through the current GitHub image route. The preliminary ranking is based on the readable scanner screenshot and supplied MCC table.
- Engineering: verify current-head CI; complete Boring Overnight data calculations and catalyst inputs; implement persistent background collection/recovery; integrate snapshot/reason provenance; validate before calling anything live/actionable.

## [Uncertainties]
- Current-head CI result was not retrieved in this review.
- Individual chart images were not visually inspected.
- Candidate catalyst status, daily volume, price vs. 20D SMA, full exclusion-list status and BNC identity remain unverified.


## Unified candidate review scaffold — 2026-10-28

- Contract: docs/Architecture/Unified-Candidate-Review-Contract.md. It specifies the candidate snapshot/assessment boundary, fail-closed freshness and catalyst handling, separate execution vs. quality state, provenance, reason codes, API/UI seams and acceptance criteria.
- This is a design scaffold only. Application types, deterministic evaluator, persisted observations, provider wiring, API endpoint and dashboard display remain open. Start with immutable input/output types and unit tests; do not invent freshness thresholds before provider/session contracts are agreed.


## RTO / AMT direct Project Source chart review — 2026-10-28

### What I reviewed
- Direct Project Source screenshots: RTO `Screenshot 2026-10-28 160419.png`; AMT `Screenshot 2026-10-28 152731.png`.
- Full visual notes: `docs/Validation/RTO-AMT-Chart-Review-20261028.md`.

### Findings
- **RTO:** 5M pane displayed Runner Rising, Permission YES, RSI 61.1, extension 0.49 ATR and RVOL 4.32. Lower 1M pane displayed GET OUT / EOD GET OUT after intraday weakness and a late rebound. Best historical profile of this shortlist remains RTO, but these charts do not establish an eligible new late-day overnight entry.
- **AMT:** 5M pane displayed extension 2.03 ATR and a large selloff/recovery (marked low about $175.78; right-edge price about $184.02). Lower 1M pane displayed GET OUT / EOD GET OUT. Deprioritize for this session's boring-first selection.
- **Order:** RTO further review; AMT defer; SXC remains third/caution based on previously supplied 6.20% range. None is READY.
- The upper and lower panes show different Permission labels. Do not infer a defect until the exact timestamps and study inputs are aligned.

### Next engineering work — no new user action required now
1. Add/test source-backed True Range, ATR and ATR-normalized extension calculations with explicit timeframe/lookback and provenance.
2. Compare MCC ATR/extension values against the exact selected TOS study/timeframe before using them as a gate.
3. Preserve separate historical Boringness, 5M authority, 1M execution/EOD, RVOL, catalyst, and freshness fields in the unified review.
4. Continue implementing the end-to-end data path: completed close/open overnight returns, full eligible GTC_EXT window, catalyst/exclusion evidence, persisted candidate snapshots and shared evaluator integration.
5. Run build/tests and exact-head CI after source changes. Do not mark the work complete based on documentation or screenshot analysis alone.

### User action
- None for this review. For later live-data validation, run local checks and share sanitized results only; never send credentials/secrets.

## [Uncertainties]
- The screenshots are visual evidence for the selected chart bars, not live quotes.
- Catalyst clearance, current source freshness, complete historical ON samples, daily average volume, price/20D-SMA and spread are not proven by these images.


## EOD signal semantics and evaluator correction — 2026-10-28

### Source changes made on the requested branch
- Updated `src/MomentumCommandCenterV2.API/BoringOvernight/BoringOvernightEvaluator.cs` to accept optional `EodSignal` separately from the 1M execution state. `GET OUT` / `SELL` is explicitly a signal, not an end-of-day clock/session state; it blocks a fresh overnight entry and indicates position management for an existing holding.
- Added tests in `tests/MomentumCommandCenterV2.Tests/BoringOvernightEvaluatorTests.cs` for GET OUT/SELL semantics.
- Source commit: `89ad3ad03cf80552a973419f0665d54245f753a7`; test commit: `fa03c32bbd5692e55fed5225b7fd87106e9dbef0`. Current-head CI/build status has not yet been verified.

### Re-evaluated chart shortlist
- **RTO:** top of the three for further review. 5M Runner Rising / Permission YES / bullish regime / 0.49 ATR extension are constructive. However, 1M/EOD GET OUT is an active exit signal, and RVOL 4.32 needs investigation. Not READY for a new overnight entry.
- **AMT:** defer for boring-first entry: ~2.03 ATR extension, large intraday selloff/recovery, and active 1M/EOD GET OUT exit signal. If held, treat the signal as position management.
- **SXC:** no individual SXC chart in the reviewed source set; scanner range 6.20% is too high for the low-noise preference. WATCH/DEFER pending chart and hard-gate verification.
- The differing 5M/1M Permission labels still require bar-time alignment; do not infer a code defect from a screenshot alone.

### Remaining P0 work
1. Implement/test source-backed True Range, ATR and normalized extension from OHLCV.
2. Connect the shared evaluator to source-backed historical overnight metrics, current data freshness, catalyst status, exclusion evidence, and persisted decision snapshots.
3. Complete market-calendar-aware close/open return pairing and full eligible GTC_EXT-window coverage.
4. Integrate candidate qualification into scanner, Morning Report, and MarketWatch without letting ranking/score override hard gates.
5. Run build/tests and verify CI on the exact branch head. Do not claim CI passed until evidence is available.


## ATR implementation — 2026-10-28

- Added `src/MomentumCommandCenterV2.API/BoringOvernight/BoringOvernightAtrCalculator.cs` and tests. It computes Wilder ATR/True Range from source bars with explicit timeframe and period, reports ATR%, and computes signed extension only against an explicit reference price.
- This is implemented but not yet connected to the Boring Overnight evaluator. First compare its outputs against the selected Thinkorswim study using matching symbol, timeframe, bar window and period. Do not infer parity from similar names.
- Latest branch head at this update: `6f000252ba9724dda40687748a6905b17072ae0a`. GitHub CI/status lookup returned no records, and a local build was unavailable because this execution environment could not resolve github.com. Tests are added but not verified as passing.
- The largest remaining P0 engineering gap is still the end-to-end pipeline: historical overnight close/open pairing, full eligible GTC_EXT data, catalyst/exclusion evidence, persistent candidate decisions, and scanner/Morning Report/MarketWatch integration.


## Boring Overnight bubble redesign — implementation update 2026-10-28

### Implemented
- User confirmed Arizona time (America/Phoenix).
- Updated the canonical 1M ThinkScript source to evaluate at the 12:55 Arizona bar inside a 12:30–12:55 window. Known chart-technical failure => red EOD GET OUT; chart checks otherwise pass but external evidence is unavailable => yellow DATA? — NO HOLD. The study does not show green HOLD OVERNIGHT without complete qualification evidence.
- Removed the unconditional EOD clock override from 1M final state. Kept the 3:55 PM Arizona cutoff as a separate intraday-entry control.
- Changed the 5M label to ENTRY CUTOFF ACTIVE/INACTIVE.
- Removed EodSignal from the evaluator inputs to prevent circular logic; actual 1M SELL/GET OUT/BLOCK remains a hard gate.
- Added Arizona timezone convenience entry point and test to the server-side decision helper.
- Design contract: docs/Architecture/Boring-Overnight-Hold-Decision-Bubble.md.

### Instructions for the user
1. Open the 1M study in Thinkorswim, replace its source with the contents of [docs/Charts/1M/MainStudy.txt](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Charts/1M/MainStudy.txt), save, and confirm Thinkorswim compiles it.
2. Review the 12:55 PM Arizona bar. If the chart technical gates fail, the decision bubble should be red EOD GET OUT; if those chart checks pass, it should be yellow DATA? — NO HOLD until the missing external overnight qualification inputs are connected.
3. Replace the 5M study with [docs/Charts/5M/MainStudy.txt](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Charts/5M/MainStudy.txt) to replace the clock-based EOD label with ENTRY CUTOFF. Existing late-session entry blocking remains.

### Still open
- The API decision helper is not connected to persisted candidate snapshots or surfaced to Thinkorswim.
- Historical overnight metrics, complete GTC_EXT coverage, catalyst/exclusion/freshness evidence and scanner/report integration remain incomplete.
- Build/test and CI for the exact latest branch head remain unverified.


## 2026-10-28 — Boring Overnight bubble validation

### Confirmed user actions
- [x] Updated 1M study installed successfully in Thinkorswim.
- [x] Updated 5M study installed successfully in Thinkorswim.
- [x] User reports the 12:55 PM Arizona bar behaves as designed: SXC shows red `EOD GET OUT` with a chart-technical blocker; RTO shows yellow `DATA? — NO HOLD` when chart checks pass but external qualification evidence is missing.
- [x] Green `HOLD OVERNIGHT` remains intentionally unavailable pending full qualification inputs.
- Evidence: [RTO screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212531.png), [SXC screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212549.png).

### Engineering work can continue without more screenshots
- [ ] Connect `BoringOvernightHoldDecision` to persisted, timestamped candidate snapshots rather than manually supplied/unpersisted inputs.
- [ ] Derive and persist overnight win rate, mean/median returns, 5D-vs-20D comparison, volume/price/SMA gates and source/freshness metadata from validated bars with the exact eligible session boundary.
- [ ] Add source-backed exclusion lookup and a catalyst provider contract; until connected and current, event/exclusion status must remain UNKNOWN and prevent READY.
- [ ] Integrate the final decision and reason codes into scanner / Morning Brief / report surfaces without allowing ranking scores to bypass 5M permission or 1M execution gates.
- [ ] Add integration tests for READY / BLOCKED / DATA? and stale/missing evidence, then verify CI at the exact branch head.

### User input currently needed
- None for the next offline implementation steps. Later live provider smoke tests still require the user to configure credentials locally; never request secrets in chat.


## Handoff refresh — CI recovery and next implementation slice (2026-10-28)

### Verified
- CI red X was one stale test assertion, not a production ATR calculation defect. The explicit-bar test documents a 2-period Wilder seed of 2.5 followed by smoothing with TR=2, yielding 2.25. Updated expected ATR to 2.25; no calculator logic changed.
- Commit: `96283fb20c054aee326205ff6d19c438f3033862`. GitHub Actions run `40818276331` completed successfully. This is a green result for that commit, not a guarantee that future commits remain green.
- User-confirmed chart behavior remains: SXC red `EOD GET OUT`; RTO yellow `DATA? — NO HOLD`. Missing qualification evidence must continue to fail closed.

### Implementation priority — do not stop at scaffolding
1. Add append-only Boring Overnight candidate snapshots with source/provider/feed, as-of timestamps, field-level provenance, evaluator/calculation version, and missing/stale evidence.
2. Add query/read access to persisted OHLCV observations, retaining correction lineage. Do not compute history from the write-only store or silently use missing bars as zero activity.
3. Calculate overnight edge/boringness only from correctly paired, session-valid overnight observations; reject incomplete history and disclose the window/count.
4. Connect persisted evidence to the evaluator and expose why a candidate is DATA?, WATCH, BLOCKED, or READY. Keep catalyst UNKNOWN and unverified TOS states from passing READY.
5. Add tests for the new path and confirm the current branch's CI is green after source changes.
6. Integrate only after validated snapshots exist; clearly distinguish user-supplied-universe scanning from market-wide discovery.

### User actions (only where truly blocked)
- **Provider credentials:** If using Alpaca, configure credentials locally following `src/MomentumCommandCenterV2.API/README.md`; never send secrets in chat. Then run a small live smoke test and share sanitized status/error and timestamp.
- **Thinkorswim parity:** When requested, provide exact chart/CSV evidence for the specific symbols/timeframes/intervals under review. Existing XRPN evidence is already recorded; do not ask for it again. Repository source cannot prove which study is installed in the live TOS workspace.
- **Catalysts:** No connected, validated news/event feed is established in source. Until one is selected and connected, catalyst status stays UNKNOWN and output stays DATA? / manual review.
- **Outcome research:** GTC_EXT fills, target-touch and adverse-excursion outcomes need timestamped exports. No target probability or profitability estimate is supported without these data.

### Current boundary
No automatic orders. No assumed Thinkorswim API. No green overnight hold based on caller-supplied fields alone. Exclusion store is MCC-local and not proof of synchronization with Schwab/TOS lists.


### Latest code slice (2026-10-28)
- Added append-only JSONL candidate snapshot persistence and API routes:
  - `POST /api/boring-overnight/snapshots`: records a timestamped snapshot with source/provider/feed, calculation version, field-level source timestamps, and evaluator inputs; returns the evaluated result.
  - `GET /api/boring-overnight/snapshots?symbol=RTO&limit=50`: retrieves newest persisted records (limit 1–500).
- Storage path defaults to the local application-data directory under `MomentumCommandCenterV2/boring-overnight-snapshots.jsonl`; override with `MCC_BORING_OVERNIGHT_SNAPSHOT_PATH`.
- Exclusion is resolved against MCC's local exclusion store at submission time. Source provenance is recorded but not independently verified. This is not yet an OHLCV-derived metric pipeline.
- CI run `40818694414` for commit `5de6613ea951d4d38ba160c1108a22ed4ae64870` was queued at the time of the documentation update. Confirm its final conclusion before using the new endpoints.

### Instructions for the user
1. After CI is green, pull branch `feature/opportunity-quality-classification` and start the API using the repository's normal local run instructions.
2. Open Swagger at the API's configured Swagger URL and expand **Boring Overnight**. First test `GET /api/boring-overnight/snapshots` (empty history is expected initially).
3. To record a test snapshot, use `POST /api/boring-overnight/snapshots` with a real symbol, UTC `asOfUtc` (ISO-8601 ending in `Z`), a truthful `source`, a `calculationVersion`, `fieldSourceTimestampsUtc`, and the required `evaluationRequest` fields. Do not label manual or stale values provider-verified. Use test data only until feed parity and event sources are validated.
4. Read it back with `GET /api/boring-overnight/snapshots?symbol=SYMBOL&limit=10`. Confirm the stored as-of/source timestamps and the DATA?/BLOCKED/WATCH/READY result match the submitted evidence.
5. Do not use READY as a trade authorization: snapshot provenance is caller-supplied; metrics are not yet computed from validated overnight bar pairs, catalyst feed is not connected, and TOS study parity is not established by this endpoint.

- Added `BoringOvernightCandidateSnapshotStoreTests` for round-trip provenance, symbol filtering, non-UTC rejection, and mismatched-symbol rejection. Source/API CI runs `40818681818` and `40818694414` passed; run for test commit `8139ef7eb6934fad423c41a29a28cfac4e4bc23b` was queued when this handoff note was written. Confirm latest run before calling HEAD green.


### New implementation while user runs local snapshot steps — raw market-bar query
- Extended the market-bar store with `GetLatestAsync(symbol, provider, fromUtc, toUtc, limit)`. It reads JSONL observations and returns the latest correction for each provider/symbol/source timestamp while preserving prior correction records in storage.
- Added `GET /api/market-data/stored-bars` with required `symbol`, `fromUtc`, `toUtc` (UTC ISO-8601); optional `provider`, `limit` (1–10000). Example: `/api/market-data/stored-bars?symbol=RTO&fromUtc=2026-10-27T00:00:00Z&toUtc=2026-10-29T00:00:00Z&limit=1000`.
- Added tests for latest-correction selection, provider/time-window filtering, and invalid range/limit. Workflow for the interface-only intermediate commit failed as expected because implementation had not yet been committed; implementation and tests are now committed in later revisions. Check the newest workflow before declaring current HEAD green.
- This endpoint returns raw OHLCV observations, not overnight return metrics. It does not pair regular-session close with next-session open or fill gaps. That service is the next implementation task.

### Next development slice
1. Confirm current CI is green.
2. Implement a pure, timezone-explicit overnight pairing/calculation service over queried bars. Require explicit session dates and close/open source bars; surface incomplete sessions and duplicates as data-quality failures, not zeros.
3. Unit-test overnight return convention, session boundaries, missing/duplicate bars, corrected bars, and lookback window/count.
4. Only then map calculated, provenance-carrying metrics into the evaluator request. Do not synthesize catalyst clearance or TOS technical values.


### Overnight history calculator implementation
- Added `BoringOvernightHistoryCalculator`, which calculates close-to-next-open return percentages from persisted `MarketBarObservation` rows and explicit `OvernightSessionBoundary` records. The caller must provide exact UTC bar timestamps and session dates; this deliberately avoids guessed holidays or session boundaries.
- Added `POST /api/boring-overnight/history`. Body fields: `symbol`, `provider`, optional `feed`, `requiredWindow` (minimum 5), and `boundaries` with `previousSessionDate`, `nextSessionDate`, `previousCloseBarTimestampUtc`, `nextOpenBarTimestampUtc`. Response includes each return, source observation IDs, missing boundary reasons, and whether all pairs/history are complete. Rolling metrics remain null unless all supplied pairs exist and required history is met.
- Tests cover 20 pairs, missing boundary fail-closed behavior, and duplicate next-session dates.
- CI exposed a test fixture expectation error in the market-bar query test: the corrected fixture kept Open at 10.0 while changing High/Close. The assertion is corrected to 10.0; new CI is pending.

### Suggested API sequence after CI passes
1. Import or collect raw OHLCV into the persisted store using existing provider/CSV workflows.
2. Query `GET /api/market-data/stored-bars` to inspect actual stored source timestamps and confirm provider/feed coverage.
3. Build explicit session boundary entries using validated regular-session close/open bar timestamps for each trading date. Do not assume calendar continuity across weekends/holidays.
4. Call `POST /api/boring-overnight/history` with those boundaries; inspect `missing`, `allPairsComplete`, `enoughHistory`, and the source observation IDs.
5. Do not map these metrics to a production READY decision until session-boundary generation, provider coverage, corporate-action treatment, and calculation parity are independently validated.

- Latest CI follow-up: fixed the two failing test expectations (corrected raw bar open remains 10.0; missing second pair is missing its previous-close timestamp). Commit `edef25ded7db56af767a71d8be037e32ef566f56`, workflow `40820294911` was in progress when this note was prepared. Check latest workflow before assuming all tests pass.


## 2026-10-29 — History-backed Overnight evaluation endpoint

### What changed
- Added `BoringOvernightHistoryEvaluationService` and tests. Historical evaluator fields (win rate, 20-session mean, 5-session mean, 20-session median) are now overwritten from the calculated persisted-bar history. If the history is incomplete or does not meet the requested window, these fields become null rather than falling back to manually supplied values.
- Added `POST /api/boring-overnight/evaluate-with-history`. It calculates history from the stored market-bar observations using explicit UTC boundaries, resolves the local MCC exclusion list, evaluates the shared hard gates, and appends an auditable snapshot.
- Source/test commits: `65603d909dfdac07a27df8eccecdaff7f0f1f46f`, `76d90c2abf96c05a9e59a918a72b71b07dc62f02`, and `cc1d852c0ac316c0efd3637c9d2d6611f6f3ca8f`. CI for these new commits is pending; do not treat this as verified until the latest workflow is green.

### Instructions for you — after the latest branch CI is green
1. Pull branch `feature/opportunity-quality-classification`.
2. Start the API using `src/MomentumCommandCenterV2.API/README.md` and open Swagger at the API's configured `/swagger` URL.
3. Confirm stored-bar data exists for your test symbol/provider using `GET /api/market-data/stored-bars?symbol=SYMBOL&fromUtc=...Z&toUtc=...Z&provider=Alpaca&limit=10000`. Use actual source timestamps from the response; do not fabricate bars or timestamps.
4. Prepare at least `requiredWindow` explicit boundaries (default recommendation: 20). Each entry must pair the previous eligible regular-session close bar timestamp with the next eligible session's open bar timestamp. The date labels must match the actual trading sessions; do not include weekends/market holidays as sessions. The API currently does not generate or validate the official exchange calendar.
5. In Swagger, call `POST /api/boring-overnight/evaluate-with-history` with:
   - `symbol`, `provider`, optional `feed`
   - `requiredWindow` and `boundaries[]`, each containing `previousSessionDate`, `nextSessionDate`, `previousCloseBarTimestampUtc`, `nextOpenBarTimestampUtc`
   - UTC `asOfUtc`, truthful `source`, `calculationVersion`, and `fieldSourceTimestampsUtc`
   - `evaluationRequest` containing the non-history fields (price, SMA20, average daily volume, current overnight move, exclusion placeholder, catalyst status, 5M permission/structure, 1M state, extension, RVOL/volume ratio, daily range and ATR%). Set unknown/unverified values to null or UNKNOWN; do not imply they were validated.
   - The endpoint resolves `excluded` from MCC's local exclusion watchlist. Do not rely on the caller's excluded value.
   - The endpoint forcibly overrides `catalystStatus` to `UNKNOWN`: no validated catalyst provider is connected. Even if a test payload says `CLEAR`, this workflow must not return READY until that source integration exists.
6. Inspect the response's `history`: `allPairsComplete`, `enoughHistory`, `validPairCount`, `missing`, each return's source observation IDs, and the calculated metrics. Inspect `evaluation` and the persisted `snapshot`.
7. Negative test: remove one boundary timestamp from the stored data (or use a boundary that genuinely has no stored bar) and repeat. The historical evaluator fields must be null and the result must not be READY.
8. Read the audit record back with `GET /api/boring-overnight/snapshots?symbol=SYMBOL&limit=10`.

### Current limitations — still not a production hold authorization
- Explicit boundaries remain caller-provided; official exchange holidays, half-days, DST/session handling, and bar timestamp conventions are not automatically validated.
- Source/provider labels and non-history fields are still caller-supplied. Catalyst/news evidence is not connected; UNKNOWN must remain DATA?/manual review.
- Corporate actions, full eligible GTC_EXT coverage, feed entitlements, spread/liquidity and exact Thinkorswim parity remain unvalidated.
- No orders are submitted. Do not interpret READY from caller-supplied non-history inputs as an instruction to trade.

### Latest verification
- Prior verified baseline: CI run `40820328662` passed for commit `ff1e00450904fe0939cbf62c5bfabbefced0a2ea`.
- CI passed for the history service commit `65603d909dfdac07a27df8eccecdaff7f0f1f46f`, tests `76d90c2abf96c05a9e59a918a72b71b07dc62f02`, and endpoint `cc1d852c0ac316c0efd3637c9d2d6611f6f3ca8f`. The subsequent fail-closed catalyst override `9ae6b79f43c07293b545b6447fdc82104b7f2790` and latest documentation commits were still queued/in progress at last check; verify current-head CI before local testing.

[Uncertainties]
- Data not found in sources for an authoritative exchange-calendar implementation already integrated into MCC, a connected catalyst provider, or verified source timestamps for the user's local market-data store.
