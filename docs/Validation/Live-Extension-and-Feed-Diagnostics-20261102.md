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

The prior live UI did not expose Daily VWAP or raw ATR14, so the comparison could not be diagnosed from the displayed values alone. Those fields are now exposed.

## 3. Alpaca feed selection and observation provenance

- The adapter reads `MarketData:Alpaca:Feed`, sends that exact value in the REST `feed=` query, and tags each returned bar with that feed.
- The stream worker and live assessment use the same effective configuration key.
- The committed API `appsettings.json` default is now `sip`. No IEX fallback is implemented when SIP is explicitly configured; an unavailable feed/entitlement should surface as a provider error.
- `GET /api/providers/status` now returns `configuredFeed` for Alpaca.
- `GET /api/market-data/live-assessment` now returns `configuredFeed`, top-level observed `feed`, and separate `oneMinute.feed` / `fiveMinute.feed` fields.

If the live result still reports IEX after pulling and rebuilding, inspect the returned **configuredFeed** first. A value of IEX means the running API process is receiving an override or loading a different configuration copy. Check `appsettings.Development.json`, environment variable `MarketData__Alpaca__Feed`, Visual Studio launch environment, and the running API process/output directory. Stop the old process fully, rebuild, restart, and re-query. A configured SIP value with an observed IEX value is a provenance discrepancy that must be investigated; do not combine the feeds.

## 4. Regression fixes included

- The extension regression test now evaluates with `hasPosition: true`, because hard-exit flags are part of open-position management, not fresh-entry evaluation.
- TradeAnalyzer's shared strong-snapshot test fixture now places VWAP 0.25 price units below close instead of 10 units below close. With ATR=1, the old fixture accidentally represented a 10-ATR extension under the newly aligned formula and correctly blocked entry.
- Added formula tests for positive and negative close-to-VWAP distances and zero ATR.
- Added an Alpaca adapter test that verifies a configured SIP request is sent and returned bars are tagged SIP, with no IEX fallback.

## 5. Verification status

The user's November 2 Release build succeeded for all five projects, but the accompanying test run reported 172 passed / 4 failed. The source fixes above address the four reported failure causes. **The build and test suite have not yet been rerun against these fixes.** Do not mark the branch green until the user or CI confirms the exact current head.

## 6. Remaining acceptance

1. Run Release build and tests on the latest branch head.
2. Verify `GET /api/providers/status` returns `configuredFeed: "sip"`.
3. Verify live assessment returns `configuredFeed: "sip"` and both observed timeframe feeds as `sip`; if not, capture the full sanitized JSON and provider error.
4. Capture the same completed 1M timestamp in MCC and Thinkorswim and compare close, Daily VWAP, ATR14, and extension.
5. Browser-check the prototype dock labels and the five Main Chart metrics at desktop and mobile widths.
6. Re-run the RTO boundary audit against SIP; the prior 38/40 result was IEX and is not an SIP comparison. The October 14 and October 21 open-boundary gaps remain unresolved.

## [Uncertainties]

- Numerical parity with Thinkorswim is not established until the same symbol and timestamp are compared using matching feeds and session boundaries.
- SIP entitlement and actual returned SIP bars are not established by changing configuration alone.
- The exact runtime configuration source that produced the user's IEX result is not known until the new `configuredFeed` diagnostic is read from the running API.
