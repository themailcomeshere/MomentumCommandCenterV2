# Market Data / Scanner Provider Comparison

**Reviewed:** 2026-10-23. Research only: no provider selected, account entitlement verified, or adapter implemented.

## MCC requirements
- Real-time US equities and 1-minute aggregates, including premarket and after-hours with explicit session semantics.
- Historical 1-minute bars for warm-up, recovery and replay; documented handling for no-trade minutes, corrections, halts, splits, and symbol changes.
- Scalable universe discovery/market movers/scanner support; market data alone is not a screener.
- Source timestamps, receive timestamps, pagination/backfill, rate limits, reconnect guidance, and entitlement errors.
- Clear rights for private personal dashboard use, local storage and historical retention. No redistribution assumed.
- Sufficient coverage for small-cap/low-float movers; single-venue data may diverge from market-wide behavior.

## Shortlist

| Provider | Public plan details | Fit | Main gaps |
|---|---|---|---|
| **Alpaca Market Data** | Basic: free, real-time IEX, 30 websocket symbols, history since 2016 but historical endpoints limited to latest 15 minutes. Algo Trader Plus: $99/month, all US stock exchanges/SIP, unlimited websocket symbols, history without that 15-minute restriction; docs list 10,000 historical calls/minute. Websocket includes minute bars and pre/post-market aggregates. | Strong developer-first candidate for streaming and historical recovery; clear IEX vs SIP distinction. | Must verify scanner/movers/universe coverage, small-cap and extended-hours completeness, exact current terms and local history storage rights. Basic/IEX must not be treated as consolidated tape. |
| **Massive (formerly Polygon)** | Stocks Basic free: 5 API calls/minute, 2 years aggregate history, EOD data/minute aggregates. Starter $29/month: 15-minute delayed, websocket, 5 years history. Developer $79/month: 15-minute delayed, 10 years history. Advanced $199/month: real-time, 20+ years history, websockets, aggregates, trades and quotes. Individual-use plans; business plans differ. | Strong history and explicit minute-aggregate websocket; useful replay/backfill option if Advanced fits. | Free/lower tiers are not real-time. Verify screener/movers endpoints, session coverage, entitlements and whether individual-use terms fit. |
| **Twelve Data** | Public individual pricing lists Basic free with limited credits/trial websocket; Grow listed at $79/month with real-time US stocks and internal display access; credit bundles/annual billing affect price. Websocket subscriptions consume per-symbol credits. Provider says default real-time US feed captures about 5% of total US trading volume; full consolidated coverage requires separately arranged access. | Convenient API/websocket; possible prototype option if coverage is acceptable. | Roughly 5% venue-volume coverage is a concern for low-float intraday scanner completeness. Verify historical intraday depth, pre/post-market, screener support, licensing/storage and credit weights. |
| **Schwab Developer API** | Relevant ecosystem, but this review did not establish a sufficiently specific current endpoint/entitlement matrix for streaming, historical bars, screener coverage, rate limits and data terms. | Worth further investigation because manual order entry uses Schwab, but account access does not prove every data entitlement. | Data not found in sources for a verified complete fit matrix. Do not build on assumptions. |

## Preliminary recommendation
**Evaluate Alpaca Algo Trader Plus ($99/month list price) and Massive Stocks Advanced ($199/month) head-to-head before choosing.** This is not a final selection or a purchase instruction.
Alpaca's documentation explicitly distinguishes single-venue IEX Basic from SIP coverage and documents minute bars and historical data. Massive documents minute aggregates over websocket and broad history. The decision depends on real low-float discovery coverage, premarket completeness, history/backfill, rights to store data, and scanner endpoints—not just price.
Do not use Massive Starter/Developer's 15-minute delayed feed for live 1M execution timing. Twelve Data is lower priority unless its venue coverage meets the target universe. A separate news/catalyst source may be needed.

## Proof-of-fit checklist
1. Verify actual account plan and entitlement; keep keys out of repository and browser.
2. Test 20–50 symbols including low-priced/small-cap names and SPY/QQQ/IWM, with premarket examples.
3. Compare 1-minute OHLCV, volume, timestamp and session coverage against Thinkorswim for the same symbol/date; record understood differences.
4. Test no-trade minutes, late/corrected bars, halts, splits, symbol changes and extended hours.
5. Measure discovery coverage against a manually curated known-movers list; verify top gainers/most active/custom scans and universe/latency.
6. Test disconnect, auth expiry, rate limit, restart, and historical reconciliation.
7. Verify personal/internal display, non-display use, local storage/retention, and redistribution terms.
8. Estimate subscribed symbols and requests/minute against plan limits.

## Official sources
- Alpaca plans: https://docs.alpaca.markets/us/docs/about-market-data-api
- Alpaca websocket bars: https://docs.alpaca.markets/us/docs/real-time-stock-pricing-data
- Alpaca IEX vs SIP: https://docs.alpaca.markets/us/docs/market-data-faq
- Massive stocks pricing: https://massive.com/pricing?product=stocks
- Massive stock websocket: https://massive.com/docs/websocket/stocks/overview
- Massive history windows: https://massive.com/knowledge-base/article/how-much-historical-stock-data-does-massive-have
- Twelve Data pricing: https://twelvedata.com/pricing
- Twelve Data US coverage/licensing: https://support.twelvedata.com/en/articles/9935903-us-equities-market-data
- Twelve Data credits: https://support.twelvedata.com/en/articles/5615854-credits

## [Uncertainties]
Prices/features change and must be verified before purchase. Provider-specific scanner coverage, API limits, history retention and contract permissions need direct confirmation. No account entitlement has been checked.
