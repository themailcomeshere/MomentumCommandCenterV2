# Market Data / Scanner Provider Comparison

**Reviewed:** 2026-10-23. Research only: no provider selected, account entitlement verified, or adapter implemented. Prices below are current public individual-plan listings checked during this review; verify again before purchase.

## What MCC ultimately needs

MCC is not just a chart display. The intended system needs:
- Real-time US equities data for 1M/5M decision support, including premarket and after-hours with explicit session handling.
- Historical intraday bars for warm-up, restart recovery, reconciliation and replay.
- Broad discovery of small-cap/low-float movers, not merely a watchlist price feed.
- Quotes/spread context for tradability and risk; OHLCV alone is not enough.
- Timestamps, source provenance, late/corrected events, rate limits, reconnect/backfill, corporate actions, and clear licensing/retention terms.
- A separate catalyst/news source may still be needed for Morning Report and Boring Overnight event risk.
- No automated order submission; user prefers manual Schwab order entry.

## Free vs paid — decision summary

| Provider/plan | Current public price | What it can do for MCC | Main blocker |
|---|---:|---|---|
| **Alpaca Basic** | Free | Real-time IEX feed, 30 websocket symbols, history dating to 2016 but historical queries limited to latest 15 minutes, 200 historical API calls/minute. Useful for learning the API and a narrow proof of concept. | IEX-only is not consolidated US market coverage; weak basis for broad small-cap mover discovery or reliable cross-venue volume. |
| **Alpaca Algo Trader Plus** | $99/month | SIP/all-US-exchange stock coverage, unlimited websocket symbols, historical data without the Basic latest-15-minute restriction, 10,000 historical API calls/minute. Strong real-time + recovery candidate. | Must validate scanner/movers endpoints, plan terms, exact pre/post-market behavior and licensing. |
| **Massive Stocks Basic** | Free | All US ticker reference data, EOD, minute aggregates, corporate actions, 5 API calls/minute and 2 years of history. Good for API/provenance prototyping and historical schema exploration. | Not real-time streaming; 5 calls/minute makes broad polling impractical. Not suitable for live morning ranking. |
| **Massive Stocks Starter** | $29/month | 15-minute delayed intraday, unlimited API calls, minute aggregates, websocket, snapshots, 5 years history. | Delay is unacceptable for live 1M entry timing and actionable Red Zone. Good only for offline/UI development. |
| **Massive Stocks Developer** | $79/month | 15-minute delayed data, unlimited calls, 10 years history, websocket, minute/second aggregates, trades. | Still delayed; does not satisfy live entry timing. Quotes are listed as not included. |
| **Massive Stocks Advanced** | $199/month | Real-time US market data, websocket + REST, minute/second aggregates, trades and quotes/NBBO, snapshots/full-market snapshot, 20+ years history, broad venue coverage. Best-looking single-vendor fit among these candidates for real-time + discovery + liquidity context. | Highest cost in this shortlist; verify individual-use eligibility, actual market-data licensing, endpoint entitlements, scanner coverage and small-cap/extended-hours behavior. |
| **Twelve Data Basic** | Free | Small API quota (8 credits/minute, advertised 800/day) and 8 trial WebSocket credits; useful for trying endpoint syntax and limited internal non-display use. | US equity pricing source represents approximately 5% of total US trading volume; insufficient as MCC's authoritative broad-market feed. |
| **Twelve Data Grow** | $79/month monthly list price | Real-time US stocks, 20+ markets, internal display access, 377 API credits/minute and 8 trial WS credits on the listed individual plan. Could help internal display/news-adjacent multi-market needs. | The stated US source coverage is still approximately 5% of US trading volume unless a different licensed feed is contracted. Verify WebSocket quota and data rights for intended use. |
| **Twelve Data Pro / Ultra** | $229 / $999 per month monthly list prices | Higher API/WS quotas, broader markets, and additional internal-data access depending on tier. | Higher spend does not itself resolve the US equity venue-coverage concern; confirm feed source/coverage before considering. |

Official sources:
- Alpaca plan matrix: https://docs.alpaca.markets/us/docs/about-market-data-api
- Alpaca feed/stream docs: https://docs.alpaca.markets/us/docs/real-time-stock-pricing-data
- Alpaca IEX vs SIP FAQ: https://docs.alpaca.markets/us/docs/market-data-faq
- Massive pricing: https://massive.com/pricing?product=stocks
- Massive REST plan/endpoint matrix: https://massive.com/docs/rest/stocks/overview
- Massive real-time minute aggregates: https://massive.com/docs/websocket/stocks/aggregates-per-minute
- Massive full-market snapshots/movers: https://massive.com/docs/rest/stocks/overview
- Twelve Data pricing: https://twelvedata.com/pricing
- Twelve Data US coverage/licensing: https://support.twelvedata.com/en/articles/9935903-us-equities-market-data
- Twelve Data credit model: https://support.twelvedata.com/en/articles/5615854-credits

## Recommendations by stage

### Stage 1 — Free evaluation (no provider selected)

1. Use Massive Basic to test reference-data, historical aggregate schema, timestamps, no-trade interval behavior and corporate-action handling. Its free tier is not a live feed.
2. Use Alpaca Basic to prototype WebSocket/API integration and compare IEX data against TOS. Label results **IEX-only**, and do not use it as the truth source for volume-sensitive qualification or broad-market scanning.
3. Do not select Twelve Data as the primary live US equities feed unless its source coverage changes or a separate consolidated feed is confirmed. The documented approximately 5% venue coverage is a material mismatch for MCC's broad mover-discovery goal.
4. Free tiers are appropriate for plumbing, contracts, UI and failure/recovery development—not for certifying live 1M/5M parity or live Red Zone ranking.

### Stage 2 — Paid proof of fit

**Shortlist Alpaca Algo Trader Plus ($99/month) and Massive Stocks Advanced ($199/month).**

- Prefer **Alpaca** if predictable developer integration, broad SIP market coverage and a lower monthly cost win, and if the required discovery/scanner workflow can be supplied or built.
- Prefer **Massive Advanced** if a single API vendor's real-time trades, quotes, minute bars, full-market snapshots and market-mover endpoint coverage are proven to meet the scanner + spread + recovery requirements.
- Neither is approved yet. Test 20–50 representative symbols (including low-priced/small-cap names, known movers, SPY/QQQ/IWM and premarket examples) against TOS at matched timestamps. Record price/volume/time differences and expected venue/session differences.

### Mandatory proof-of-fit tests before buying/production

1. Confirm personal-use eligibility, real-time entitlement, display/non-display rights, local retention rights, and any redistribution restrictions.
2. Verify actual discovery endpoints: top gainers/losers, most active, custom filters, all-ticker snapshots and premarket coverage. A market-data feed is not automatically a full screener.
3. Compare 1M OHLCV, volume, timestamps and session coverage with TOS for the same symbol/date; do not assume vendors use identical bar eligibility/aggregation.
4. Test no-eligible-trade minutes, late/corrected bars, halts, splits, symbol changes, RTH/ETH and DST transitions.
5. Test disconnect, auth expiry, rate limiting, process restart and historical reconciliation without synthesizing bars.
6. Verify whether news/catalyst data is included; if not, budget a separate source.
7. Keep API keys server-side/local backend only, never in frontend JavaScript or committed source.
8. Confirm the vendor's terms cover the intended personal/internal dashboard and any stored historical dataset.

## Final assessment

- **Best free starting points:** Massive Basic for historical/reference API experiments; Alpaca Basic for stream integration experiments. Neither is adequate as the final real-time authority.
- **Best paid finalists to test:** Alpaca Algo Trader Plus and Massive Stocks Advanced.
- **Current recommended action:** run a no-cost proof of concept first; do not subscribe until the discovery endpoints, market-data rights and TOS parity tests are demonstrated.
- **No provider selected. No credentials connected. No live adapter exists.**

## [Uncertainties]

Public prices and plan matrices can change. Endpoint-specific entitlement, exact history limits, small-cap and premarket coverage, retention/licensing, scanner behavior and vendor terms require confirmation for the user's account and intended use. Data not found in sources: a verified selected provider, actual account entitlement, or contractual approval.
