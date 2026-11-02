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

The prior live UI rounded price inputs too aggressively for a low-priced symbol. The prototype now displays close, VWAP, and ATR14 to five decimal places and shows the exact 1M bar timestamp in Arizona time and UTC.

## 3. Alpaca feed selection and observation provenance

- The adapter reads `MarketData:Alpaca:Feed`, sends that exact value in the REST `feed=` query, and tags each returned bar with that feed.
- The stream worker and live assessment use the same effective configuration key.
- The committed API `appsettings.json` sets `sip`. The REST adapter, WebSocket worker, and live-assessment feed-match fallback now also default to SIP when the key is absent; missing configuration no longer silently selects IEX. An explicit higher-precedence IEX override can still select IEX.
- `GET /api/providers/status` returns the adapter instance's effective `configuredFeed`, `configuredFeedSource` (the highest-precedence configuration provider), and `feedConfigurationSources` (all providers contributing that key and value). This is specifically to diagnose a runtime IEX result when the tracked JSON says SIP.
- `GET /api/market-data/live-assessment` now returns `configuredFeed`, top-level observed `feed`, and separate `oneMinute.feed` / `fiveMinute.feed` fields.

The user confirmed `src/MomentumCommandCenterV2.API/appsettings.json` contains `Feed: sip`, `appsettings.Development.json` does not exist, and `launchSettings.json` only sets `ASPNETCORE_ENVIRONMENT=Development`. That launch setting does not itself override the feed. ASP.NET Core Development configuration also loads User Secrets, and environment variables override JSON. Query `/api/providers/status` after rebuilding/restarting: inspect `configuredFeedSource` and every entry in `feedConfigurationSources`. If source is User Secrets, inspect `dotnet user-secrets list --project src/MomentumCommandCenterV2.API` and remove or change only `MarketData:Alpaca:Feed`. If source is EnvironmentVariables, inspect `MarketData__Alpaca__Feed` at process/user/machine scope. If source is `appsettings.json` but the value is IEX, verify the running API content root and output copy; the tracked file currently says SIP. The adapter now reports its own cached feed, so this diagnostic is not merely a reread of configuration. A configured SIP value with an observed IEX value is a provenance discrepancy that must be investigated; do not combine the feeds.

## 4. Regression fixes included

- The extension regression test now evaluates with `hasPosition: true`, because hard-exit flags are part of open-position management, not fresh-entry evaluation.
- TradeAnalyzer's shared strong-snapshot test fixture now places VWAP 0.25 price units below close instead of 10 units below close. With ATR=1, the old fixture accidentally represented a 10-ATR extension under the newly aligned formula and correctly blocked entry.
- Added formula tests for positive and negative close-to-VWAP distances and zero ATR.
- Added an Alpaca adapter test that verifies a configured SIP request is sent and returned bars are tagged SIP, with no IEX fallback.

## 5. Verification status

The November 2 earlier source head passed 178/178 in run [41491616284](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41491616284). The later feed-provenance, Main Chart layout, timestamp display, and SIP-default changes passed the Release build and **179/179 tests** in Actions run [41504217777](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504217777) on source commit `ee2e929e3bdf15f1457211534dfc9456dc8aa5ae`. The docs-updated head `4443506be982d0ff6726992c7b2294d97871bad2` also passed Release build and 179/179 tests in run [41504521572](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/41504521572).

## 6. Remaining acceptance

1. Verify the latest source head's Release build and tests; the previous 178/178 run predates these changes.
2. Verify `GET /api/providers/status` returns `configuredFeed: "sip"`.
3. Verify live assessment returns `configuredFeed: "sip"` and both observed timeframe feeds as `sip`; if not, capture the full sanitized JSON and provider error.
4. Use [Thinkorswim / MCC Extension Parity](Thinkorswim-MCC-Extension-Parity-20261102.md) to add ATR14/extension plots in TOS, then compare the exact same completed 1M timestamp and five-decimal inputs.
5. Browser-check the prototype dock labels, 3-column Main Chart header, chart-side 2x3 metrics, exact source timestamp, and no horizontal overflow at desktop/mobile widths.
6. Re-run the RTO boundary audit against SIP; the prior 38/40 result was IEX and is not an SIP comparison. The October 14 and October 21 open-boundary gaps remain unresolved.

## [Uncertainties]

- Numerical parity with Thinkorswim is not established until the same symbol and timestamp are compared using matching feeds and session boundaries.
- SIP entitlement and actual returned SIP bars are not established by changing configuration alone.
- The exact runtime configuration source that produced the user's IEX result is not known until the new `configuredFeed` diagnostic is read from the running API.
