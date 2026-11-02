# Live Extension Inputs and Feed Diagnostics — 2026-11-02

## Purpose

Record the extension metric contract, make every input visible in the dashboard, and distinguish the requested Alpaca feed from the feed actually returned by each timeframe.

## 1. Extension formula

The installed Thinkorswim 1M study defines the extension as:

`AbsValue(close - dailyVWAP) / Average(TrueRange(high, close, low), 14)`

MCC now centralizes that formula in `V2SignalEngine.CalculateAtrExtension` and uses it in the live technical calculator and the engine's fallback path.

- **1M Close**: close of the latest completed 1-minute bar.
- **Daily VWAP**: current regular-session VWAP calculated from the 1-minute OHLCV observations available to MCC.
- **ATR14**: simple 14-bar average of true range, in price units. This is not the extension metric.
- **ATR Extension**: absolute close-to-Daily-VWAP distance divided by ATR14, expressed in ATR units.
- **5M RVOL**: independent same-time-of-day relative volume. It is not VWAP, ATR14, or ATR extension.

The Main Chart prototype now displays all five diagnostic values: 1M Close, Daily VWAP, ATR14, 5M Relative Volume, and ATR Extension. Red Zone's extension line also prints the close, VWAP, ATR14, and normalized extension inputs. The API returns `oneMinute.close`, `oneMinute.vwap`, `oneMinute.atr`, and `oneMinute.extensionAtr`.

## 2. Interpreting the user's reported values

The reported MCC value **11.20 ATR** is the normalized ATR Extension displayed by the ATR EXTENSION card. It is not the raw ATR14 price value.

The Thinkorswim value **EXT: 24.5 ATR** and MCC's **11.20 ATR** are not yet a validated mismatch on a matched observation. Compare the same symbol and completed 1-minute bar timestamp, then compare all four inputs: close, Daily VWAP, ATR14, and the calculated ratio. Do not adjust warning/hard-exit thresholds to force a match.

The prior live UI rounded price inputs too aggressively for a low-priced symbol. The prototype now displays close, VWAP, and ATR14 to five decimal places. A follow-up source audit found the timestamp display had shown the bar-start time under a label describing the completed close. The UI now displays `completedAtUtc` as the close time and separately displays the bar's start-to-close interval. See [Live 1-Minute Bar Timestamp and Latency](Live-Bar-Timestamp-Latency-20261102.md).

## 3. Alpaca feed selection and observation provenance

- The adapter reads `MarketData:Alpaca:Feed`, sends that exact value in the REST `feed=` query, and tags each returned bar with that feed.
- The stream worker and live assessment use the same effective configuration key.
- The committed API `appsettings.json` sets `sip`. The REST adapter, WebSocket worker, and live-assessment feed-match fallback now also default to SIP when the key is absent; missing configuration no longer silently selects IEX. An explicit higher-precedence IEX override can still select IEX.
- `GET /api/providers/status` returns the adapter instance's effective `configuredFeed`, `configuredFeedSource` (the highest-precedence configuration provider), and `feedConfigurationSources` (all providers contributing that key and value). This is specifically to diagnose a runtime IEX result when the tracked JSON says SIP.
- `GET /api/market-data/live-assessment` now returns `configuredFeed`, top-level observed `feed`, and separate `oneMinute.feed` / `fiveMinute.feed` fields.

The user's latest status response identified the override precisely: `appsettings.json=sip`, local `secrets.json=iex`, with `secrets.json` as `configuredFeedSource`. The user changed `secrets.json` to `sip`; status then reported configured SIP and both sources set to SIP. The live assessment failed with Alpaca HTTP 403: `subscription does not permit querying recent SIP data`. Restoring `secrets.json=iex` returned `VALIDATED_DATA_RETURNED` and 62 bars. Therefore configuration precedence is resolved; SIP entitlement is the blocker. Keep IEX selected for usable data until the account has recent SIP access. Do not claim SIP validation or merge feeds.

## 4. Regression fixes included

- The extension regression test now evaluates with `hasPosition: true`, because hard-exit flags are part of open-position management, not fresh-entry evaluation.
- TradeAnalyzer's shared strong-snapshot test fixture now places VWAP 0.25 price units below close instead of 10 units below close. With ATR=1, the old fixture accidentally represented a 10-ATR extension under the newly aligned formula and correctly blocked entry.
- Added formula tests for positive and negative close-to-VWAP distances and zero ATR.
- Added an Alpaca adapter test that verifies a configured SIP request is sent and returned bars are tagged SIP, with no IEX fallback.

## 5. Verification status

The November 2 earlier source head passed 178/178 in run [41491616284](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41491616284). The later feed-provenance, Main Chart layout, timestamp display, and SIP-default changes passed the Release build and **179/179 tests** in Actions run [41504217777](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504217777) on source commit `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae`. The docs-updated head `4443506be982d0ff6726992c7b2294d97871bad2` also passed Release build and 179/179 tests in run [41504521572](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504521572).

## 6. Updated acceptance

- [x] Configuration-precedence root cause identified: local `secrets.json` overrode tracked `appsettings.json`.
- [x] SIP configuration selected successfully from both JSON providers.
- [x] Captured explicit recent-SIP entitlement denial (HTTP 403).
- [x] Restored IEX and confirmed successful validation/data return (62 bars in the supplied provider-status result).
- [x] User confirmed prototype Main Chart fits without horizontal scrolling.
- [x] Corrected Main Chart timestamp to show completedAtUtc rather than the bar-start timestamp.
- [ ] Obtain recent SIP entitlement; then confirm live 1M/5M and historical responses carry `feed=sip`.
- [ ] Re-run RTO exact 40-boundary audit only after a successful SIP request. Prior evidence is IEX: 38/40 exact, missing Oct 14 and Oct 21 next-session opens.
- [ ] Capture exact-date/time matched TOS/MCC close, VWAP, ATR14 and historical ExtensionATR to five decimals.
- [ ] Measure actual completed-bar age from `evaluatedAtUtc - oneMinute.completedAtUtc`; if >=60 seconds for a liquid RTH symbol, investigate provider/source delay.
- [ ] Complete remaining Red Zone ranking and Boring Overnight validation items in the Master To-Do.
- [ ] Create Schwab app, confirm callback and Active/Ready status, then implement read-only OAuth/position sync; see `docs/Thinkorswim-Schwab-Integration-Plan.md`.

## [Uncertainties]

- Numerical parity with Thinkorswim is not established until the same symbol and timestamp are compared using matching feeds and session boundaries.
- SIP entitlement is explicitly denied for recent SIP requests in the user's current Alpaca subscription; this may change if the account plan or entitlement changes.
- The user has not yet supplied an API payload with actual `evaluatedAtUtc`, `completedAtUtc`, and `ageSeconds` from the apparent one-minute delay interval.
