# Boring Overnight Trade-Readiness Runbook (Provisional)

Status: decision-support specification; not a validated strategy or automated trading permission. Version BORING-OVERNIGHT-READINESS-0.1. Updated 2026-10-25.

## 1. Objective and user-confirmed routine

The target is a nominal +0.5% sell price, Monday–Thursday entries, no planned Friday entries. Research candidates 1:00–3:00 PM Arizona time. Intended limit-buy window is 3:00–5:00 PM Arizona time. After a confirmed fill, user currently sets a limit sell at regular-session end-of-day close +0.5%, described as GTC + Extended. Broker for execution is Schwab; Alpaca is currently a market-data/API test source, not the execution broker for this routine.

A +0.5% target four nights a week is a performance objective to test, not a near-guaranteed outcome. Orders can fail to fill, gap against the position, or incur spread/slippage. MCC will not submit orders.

## 2. Critical Schwab order-mode distinction

Schwab docs distinguish GTC + Extended / thinkorswim EXT 13h (7:00 AM–8:00 PM Eastern, spanning premarket, regular and after-hours) from thinkorswim EXTO 24h (overnight execution, eligible securities only). A GTC + Extended label alone must not be treated as proof that the order can execute during 8:00 PM–4:00 AM Eastern. Overnight trading is available through thinkorswim for selected securities. Confirm the actual platform, order qualifier, and symbol eligibility in the order ticket.

Official sources:
- https://www.schwab.com/stocks/extended-hours-trading
- https://international.schwab.com/investment-products/extended-hours-trading
- https://www.schwab.com/learn/story/how-does-extended-hours-trading-work

## 3. Arizona/Eastern time conversion

Arizona does not observe daylight saving time. During Eastern Daylight Time, 3:00–5:00 PM Arizona = 6:00–8:00 PM Eastern. During Eastern Standard Time, it = 5:00–7:00 PM Eastern. Both fall within Eastern after-hours, but order mode, venue, symbol eligibility, and liquidity determine actual execution. Use named time zones and date-specific conversion; do not hardcode offsets.

## 4. ADR fee clarification

ADR pass-through fees are depositary/custody fees for certain American Depositary Receipts, not fees imposed simply because a trade happens overnight. Amount and timing vary by ADR. The user can keep excluding ADRs as a strategy policy, but MCC should label the reason as ADR fee/security-structure policy. Schwab reference: https://www.schwab.com/legal/schwab-pricing-guide-for-individual-investors.

## 5. Cost assumptions

The calculator's estimated-cost percentage is a user-supplied estimate for round-trip execution economics at the broker used for the trade (Schwab in this workflow). It is not Alpaca API access cost, Alpaca data subscription cost, or a fee automatically retrieved from a broker.

Track separately: explicit transaction/commission/regulatory charges, spread paid on entry/exit, slippage relative to decision price, and any security-specific fees. Do not double-count spread/slippage if realized fill data already includes it. Verify assumptions against Schwab trade confirmations and current pricing. Until measured, label the cost estimate unverified.

## 6. Historical data requirements

### Confirm the session first
1. Confirm platform and exact order qualifier used for the buy.
2. Confirm platform and exact order qualifier used for the sell.
3. Confirm whether sell execution during overnight 8 PM–4 AM Eastern is required.
4. Verify current symbol eligibility in Schwab's order ticket / thinkorswim Public → 24 Hour Trading watchlist.
5. Define what happens to unfilled positions by Friday.

### Source coverage
Alpaca documents IEX as a single-exchange feed, SIP as consolidated US exchange coverage subject to plan access, and BOATS as an overnight ATS feed. BOATS alone does not cover the full after-hours + overnight + premarket + regular-session lifecycle. Alpaca highs do not prove a Schwab limit order could have filled at that price.

References:
- https://docs.alpaca.markets/us/reference/stockbars
- https://docs.alpaca.markets/us/docs/245-trading-for-trading-api
- https://docs.alpaca.markets/us/v1.1/docs/historical-stock-data-1

### Per-observation data
Preserve symbol/date, reference entry price and its source (actual fill if available), window start/end and named time zone, provider/feed/bar/quote/trade type, timestamps and entitlement/delay status, maximum favorable and adverse excursion, missing interval flags, spread/quote availability, target-touch time, whether an executable quote or only a bar high crossed the target, actual fill evidence if available, corporate-action treatment, and calculation version. Numeric-only CSV is not enough for production provenance.

### Collection procedure
1. Fetch historical bars and where available quotes/trades for the actual window.
2. Store raw timestamps in UTC while preserving exchange-local timestamps and named zone.
3. Apply exchange calendar for holidays, early closes, DST, and overnight date attribution.
4. Detect gaps, stale feeds, duplicates, symbol changes and corporate actions; fail closed when coverage is inadequate.
5. Compute max favorable and adverse excursions across the documented eligible window.
6. Generate the calculator CSV only from verified completed observations with source provenance.
7. Twenty observations are a basic input minimum, not evidence of strategy reliability. Use multi-month, broad-universe out-of-sample validation.

## 7. Interpret the reported AAPL output carefully

The reported run used 20 samples and returned a $106.70 reference from a $100 example entry, 70% historical high-touch rate, and 6.7% configured-cost-adjusted return. This demonstrates the calculator produced a result; it does not establish that the input CSV matched Schwab's eligible window, that a live order would fill, or that the strategy is profitable. The actual CSV and provenance have not been reviewed. Data not found in sources for a verified Schwab-window-matched sample.

## 8. Hard pre-trade checklist

MCC must not return READY unless: symbol passes the user's security-type policy and Schwab exclusions; actual order ticket confirms order/session eligibility; price, volume, spread and data freshness pass; historical observations are valid and session-matched; catalyst sources are fresh or status is DATA?/MANUAL REVIEW; no incompatible earnings/news/corporate-action risk is known; actual average entry fill is known before post-fill sell calculation; target, tick size, cost, spread and slippage are visible; and an exit deadline/weekend policy exists. User manually reviews and submits every order.

## 9. Weekend and unfilled-position rule

No Friday entries does not prevent a Thursday position from remaining open over the weekend. Decide latest entry time, whether Thursday entries are allowed, exit deadline, sell review/cancel/reprice rules, and whether any position may be carried over a weekend. Do not cancel/reprice automatically without authorization. Until the policy is set, any open position or unfilled exit remains a visible alert, not a completed cycle.

## 10. Validation matrix

Test multiple months and market regimes: target touch with actual fill, bar-high-only touch, no touch, gap down/adverse excursion, earnings/news spike, wide spread/low volume, excluded/ADR-policy symbols, holidays/DST/early closes/gaps/stale data, corporate actions, partial/multiple fills, Thursday fill unresolved Friday, and 5M authority/1M execution transitions. Report target-touch rate, executable quote touch, actual fill rate where available, no-fill rate, net return after costs, adverse excursion, spread/slippage, weekend carry, false READY rate, and results by market regime. Separate development and out-of-sample periods.

## 11. Release gate

First usable release is read-only, manually reviewed decision support with explicit order-eligibility, catalyst, data freshness, liquidity, cost, and weekend-risk gates. Do not describe +0.5% four nights a week as nearly guaranteed. Do not automate order submission.

## [Uncertainties]
- Exact Schwab platform and order qualifier for each leg not yet confirmed.
- The CSV/provenance behind the 20-sample AAPL output has not been reviewed.
- Alpaca feed entitlements and historical coverage for the needed symbols/windows have not been verified.
- Weekend/unfilled-position policy remains undecided.
