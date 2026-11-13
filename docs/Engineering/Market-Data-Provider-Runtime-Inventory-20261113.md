# Market-Data Provider Runtime Inventory

**Created:** 2026-11-13. Source-verified against `src/MomentumCommandCenterV2.API/MarketData/*.cs`.
Plan prices, plan limits and licensing research live in [Provider-Comparison.md](../Provider-Comparison.md) (reviewed 2026-10-30). This page records what MCC **actually calls today** and what has been **evidenced at runtime**. It is the prerequisite for any production-source selection; it does not select a source.

## Wired endpoints

| Client (source file) | Host | Path | Used for | Feed / params | Retry |
|---|---|---|---|---|---|
| `AlpacaMarketDataAdapter` | `data.alpaca.markets` | `v2/stocks/{sym}/bars?timeframe={1\|5}Min` | 1m/5m history, capture-history, live assessment REST fallback | `feed` from `MarketData:Alpaca:Feed` (`iex`) | up to 3 attempts on transport error, timeout, 408, 429, 5xx; honors `Retry-After` capped at 5 s |
| `AlpacaMarketDataAdapter` | `data.alpaca.markets` | `v2/stocks/{sym}/trades/latest` | latest-trade reference (not NBBO) | configured feed | same |
| `AlpacaMarketDataAdapter` | `api.alpaca.markets` / `paper-api.alpaca.markets` | `v2/assets/{sym}` | instrument display name | — | same |
| `AlpacaDailyMarketDataClient` | `data.alpaca.markets` | `v2/stocks/{sym}/bars?timeframe=1Day` | Daily close / SMA20 / ADV20 audit | configured feed; split/dividend adjusted | 3 attempts; 408/429/5xx; 422→422, 429→503, 5xx→502 at the API |
| `AlpacaMinuteBarStreamWorker` | `stream.data.alpaca.markets` | `wss …/v2/{feed}` | live 1m bars (`StreamSymbols` + dynamic subscriptions) | `iex` | reconnect loop; stale error cleared after a successful auth |
| `AlpacaNewsClient` | `data.alpaca.markets` | `v1beta1/news?limit&sort=desc` | Morning Brief headlines | — | none |
| `AlpacaCatalystAssessmentClient` | `data.alpaca.markets` | `v1beta1/news?symbols=…` | per-symbol catalyst triage | — | none |
| `AlpacaScannerDiscoverySource` | `data.alpaca.markets` | `v1beta1/screener/stocks/movers?top=50` | scanner discovery polling | — | none |
| `MassiveMarketDataAdapter` | `api.massive.com` | `v2/aggs/ticker/{sym}/range/{n}/minute/…?adjusted=false` | alternate 1m/5m history | — | — |
| `YahooFinanceQuoteClient` | `query1.finance.yahoo.com` | `v8/finance/chart/{sym}?range=1d&interval=1m&includePrePost=true` | indicative extended-hours reference only | — | — |

Credentials: `MarketData:Alpaca:ApiKey/ApiSecret` (user secrets) or `ALPACA_API_KEY/ALPACA_API_SECRET`; `MarketData:Massive:ApiKey` or `MASSIVE_API_KEY`. Keys never reach the browser.

## Runtime evidence to date

| Topic | Status | Evidence |
|---|---|---|
| Alpaca feed | **IEX, validated** | provider status `VALIDATED_DATA_RETURNED` (2026-11-10); SIP requests return HTTP 403 (repeated operator evidence) |
| Consolidated coverage | **Not available** | IEX is partial-exchange; Daily returns `feedCoverage=IEX_PARTIAL`, `qualificationUsable=false` |
| Symbol coverage gaps | **Observed** | BAYRY, LYSDY, BNPQY 0/20 Daily sessions; 20 scanner symbols returned zero 1m/5m bars in one window (2026-11-10) |
| Stream delivery | **One interval only** | 10-minute regular-session sample: SPY/RKLX/ASTX/XRPN delivered; no coverage or recovery certification |
| Massive | **NOT_CONFIGURED** | no key; entitlement, coverage and delay are unknown |
| Yahoo | **Indicative only** | not NBBO, not executable, no licensing review for production use |
| Opening auction | **Not available** | no source in MCC certifies the official opening print; the 09:30 1m Open is a proxy |

## Gaps that block production-source selection

- **Licensing / redistribution terms:** Data not found in sources for the operator's actual Alpaca account agreement. Provider-Comparison lists public plan pages only.
- **Rate limits at runtime:** MCC has no client-side rate limiter; it relies on retry-on-429. Public plan limits are in Provider-Comparison and have not been measured.
- **Retention:** MCC keeps raw bars indefinitely in append-only JSONL (`market-bars.jsonl`, 138.6 MB on 2026-11-13). No provider retention clause has been reviewed.
- **Scanner universe:** `screener/stocks/movers?top=50` is a top-50 movers list on the configured feed, not a full-market universe. Its coverage relative to the TOS scanner has not been compared.
- **Session coverage:** no measured per-session (pre/regular/post) completeness by symbol.
- **Corporate actions:** Daily uses adjusted bars; intraday bars and the TOS exports have not been checked for adjustment equivalence.

## Decision rule (unchanged)

Keep Alpaca on IEX. Do not request SIP again unless the operator reports an entitlement change. A new source qualifies only with documented entitlement, matched session/adjustment semantics and an independent OHLCV parity run.
