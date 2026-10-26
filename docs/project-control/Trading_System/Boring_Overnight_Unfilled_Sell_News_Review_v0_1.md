# Boring Overnight — Unfilled-Sell News Review v0.1

Status: Design contract for manual decision support; not an order-management system.
Updated: 2026-10-26 (Arizona time)

## Purpose

When a Boring Overnight sell remains unfilled, MCC should gather current Market News and available price/liquidity context, then provide a source-grounded diagnosis so the operator can decide whether to keep the order working, consider a lower limit, prioritize an exit, or investigate further.

The +0.5% target is the operator's current heuristic, motivated by observed median ON % often around 0.56–1.00%. It is not a fixed rule. A lower target may be appropriate in some cases, including the smallest profitable exit, but profitability must be calculated from the actual entry fill/cost and any applicable execution costs. Do not assume the EOD-close reference price equals the actual buy fill.

## Current repository capability

Master contains:
- `AlpacaApiClient.GetLatestNewsAsync(symbol, limit)`, which requests news from the last 24 hours.
- CLI command `alpaca-news-check <SYMBOL>`.
- Tests for article metadata parsing, denied news access, and invalid limit handling.

This is a read-only news retrieval smoke test. It does not establish complete news coverage, classify a catalyst reliably, compare a story against the original thesis, or recommend an executable limit. A zero-article result is not proof that no news exists.

## Inputs required for a useful review

- Symbol and review timestamp.
- Position state: shares/quantity, actual average buy fill, fill timestamp, and known transaction costs.
- Current sell limit, order qualifier/time-in-force, order status, and how long it has remained unfilled.
- Latest price and timestamp, recent price/volume behavior, and spread/liquidity evidence where available.
- News articles with headline, summary, publication/created time, updated time, URL, associated symbols, retrieval time, and provider/feed identity.
- Any known halt/trading-pause status and its official reason/source, if available.
- Original reason for the trade and any known catalyst/thesis.

If position/order details or a data source are unavailable, label them unknown. Never infer an actual fill, current order status, spread, or halt reason.

## Review output contract

### 1. Evidence
For every article show:
- Headline and source URL.
- Published/created time and last-updated time when available.
- Retrieval/observation time.
- Symbols tagged by the provider.
- Provider summary, clearly labeled as provider-supplied.
- Whether the item is new, updated, potentially duplicated, or conflicting with another source.

Do not present an LLM paraphrase as an independent source. Link the underlying report. Prefer company filings/press releases, exchange notices, or named primary sources for material deal, regulatory, or trading-halt claims when available.

### 2. Diagnosis
Separate the following:
- **Verified facts:** what the retrieved source actually says.
- **Interpretation:** why it may matter to the position; identify uncertainty.
- **Thesis impact:** supports, weakens, contradicts, or does not yet establish a change to the original reason for holding.
- **Price/liquidity context:** current price and time, volume/volatility, spread if available, and any halt/pause status.
- **Coverage status:** `CURRENT`, `STALE`, `NEWS_UNKNOWN`, or `CONFLICTING`.

Never state “no news” unless the source's coverage limits are explicit. If a provider fails, returns no items, or coverage is not known to be complete, state that the check did not find a report in the queried source/window—not that no material news exists.

### 3. Manual options (not an order)
Possible outputs:
- `LEAVE_WORKING_FOR_REVIEW`: no verified thesis-changing evidence; price/liquidity do not independently indicate an urgent exit.
- `CONSIDER_LOWER_LIMIT`: target may be too ambitious relative to current conditions; show candidate prices only if supported by actual entry economics and current market data.
- `URGENT_MANUAL_REVIEW`: verified adverse, thesis-changing event (for example, a confirmed deal cancellation or material adverse filing).
- `HALT_OR_PAUSE_INVESTIGATION`: trading interruption detected or reported; seek official reason and monitor reopening conditions. A halt alone does not prove the thesis remains sound.
- `NEWS_UNKNOWN` / `CONFLICTING`: data are unavailable, stale, or inconsistent; do not issue a confident target suggestion.

Each option must state evidence, assumptions, and what could make the conclusion wrong. Do not automatically cancel, replace, sell, or modify an order.

## Profitability and target math

- Preserve the actual average entry fill and any verified transaction charges.
- Separate the close-based reference target from actual buy-fill economics.
- If evaluating a candidate sell price, show gross return from actual entry fill and known costs separately. Mark unknown costs as unknown rather than zero unless the operator has confirmed that assumption for the trade.
- Spread and slippage are execution risks even when commission is $0.
- A limit price touching the historical high is not proof that an order would fill.

## Safety / failure behavior

- No order placement, cancellation, replacement, or price modification.
- A provider error, stale result, missing symbol match, missing price, or conflicting report must be visible.
- Deduplicate by article ID/URL where possible, but retain conflicting reports as separate evidence.
- Do not turn rumor into fact. Do not state that an analyst target changed unless the retrieved source identifies the analyst/firm, target, publication time, and context.
- Never use keyword matching alone as proof that a deal was signed/cancelled, a trading halt is favorable, or an exit is required.
- No source-backed diagnosis available => say `DATA NOT FOUND IN SOURCES` for the missing evidence and leave the decision manual.

## Implementation sequence

1. Improve `alpaca-news-check` output to show article summary, updated time, URL, tagged symbols, retrieval time, and explicit 24-hour coverage limitation.
2. Add a read-only `open-position-review` report taking the symbol and optional actual entry fill/current sell-limit/order status. Fetch latest news and latest bar; do not invent missing position details.
3. Add tests for no articles, stale/updated/duplicate articles, provider failure, symbol mismatch, missing position inputs, and unknown/conflicting status.
4. Add manual review classifications only when each classification can be tied to retrieved evidence; retain uncertainty and competing sources.
5. Add quote/spread/liquidity and official halt-status providers only after access and coverage are verified.
6. Validate against known historical events, cancelled deals, false rumors, trading halts, ordinary no-news days, and unfilled sells before relying on the workflow.

## Launch boundary

The current news endpoint and CLI smoke test are useful components, not a complete catalyst diagnosis. Until the review report, provenance/freshness, and market-context checks are implemented and validated, use `alpaca-news-check` as a source-gathering aid only. All order decisions remain manual.
