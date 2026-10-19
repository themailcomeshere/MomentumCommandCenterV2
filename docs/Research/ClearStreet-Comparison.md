# Clear Street — Product Comparison for Momentum Command Center V3

**Reviewed:** 2026-10-18  
**Source:** Clear Street public website and API documentation. This is a capability comparison based on public product descriptions, not a claim that MCC matches Clear Street's infrastructure.

## Finding

Clear Street overlaps with MCC at the broad workflow level—researching opportunities, screening markets, monitoring risk, and helping a trader move toward a trade—but it is not the same product or system. Clear Street is a brokerage/trading platform with execution, account/portfolio context, market access, and APIs. MCC is a research and decision-support prototype centered on a specific 5M-authority / 1M-execution process. The public site does not establish that Clear Street implements MCC's particular signal logic.

## Capabilities worth learning from

1. **Unified decision surface:** Bring candidate discovery, current setup, open-position status, risk/exposure, alerts, and decision rationale into one view. Keep eligibility, quality, timing, position management, and data confidence as separate fields.
2. **Multi-factor screener:** Treat discovery as a distinct upstream layer. Screen candidates and retain why each symbol was surfaced, when it was surfaced, and what data was available at that moment.
3. **Explainable AI/research assistant:** A natural-language interface could answer questions against MCC's own logged bars, signal transitions, rules, and positions. Answers must cite the underlying MCC data and distinguish facts from inference; no ungrounded trade claims.
4. **Event-driven alerts:** Alert on meaningful state transitions (5M permission changes, 1M candidate/READY events, runner waning, invalidation, EOD) rather than noisy every-tick updates. Preserve timestamps and the exact rule/reason that fired.
5. **Risk-aware position context:** Show position size, entry, unrealized/realized P&L when trustworthy data exists, exposure, stop/exit plan, runner/waning state, and data freshness together. Do not let this layer override 5M authority or hard risk gates.
6. **Developer-friendly contracts:** If/when a market-data provider is chosen, use explicit schemas, stable identifiers, pagination/error handling, timestamps/time zones, freshness metadata, and deterministic replay. Do not choose a broker or wire execution merely because Clear Street offers an API.

## What not to copy

- Do not copy marketing claims, latency numbers, or feature promises as MCC requirements.
- Do not adopt Clear Street's broker, clearing, routing, margin, shorting, or multi-asset infrastructure as part of MCC's scope by implication.
- Do not build live order routing, automated execution, or autonomous trading. MCC remains a research/decision-support prototype; automated order placement is out of scope.
- Do not assume Clear Street's Omni assistant or its access to proprietary account/market systems can be reproduced without equivalent data access, permissions, and audited integrations.
- Do not infer that its screening factors or AI recommendations are the same as MCC's strategy rules.

## Fit against current MCC status

| Capability | MCC status based on repository review | Action |
|---|---|---|
| 5M permission → 1M execution hierarchy | Defined in chart-study references and prototype documentation; Thinkorswim parity/runtime remains unverified | Preserve; test parity |
| Candidate discovery / multi-factor screening | Scanner discovery is future work; no scanner data source is connected | Specify scanner interface and event provenance |
| Unified dashboard | Static demo front end exists, not connected to market data | Design around real domain contracts after replay/data parity |
| Position/risk context | Position lifecycle concepts exist; live account/broker state is not connected | Define read-only position and risk view, mark missing data explicitly |
| Alerts on transitions | Signal/event logging is future work | Add timestamped transition events after source parity |
| AI research assistant | Not implemented | Defer until grounded event store, data lineage, and permissions exist |
| Market-data / trading APIs | Replay and CSV foundation exist; live provider not selected | Choose provider only after data contract and licensing review |
| Automated execution | Explicitly out of scope | Do not implement |

## Suggested order

1. Make historical bars, timestamps, indicator parity, and 5M/1M transition logs trustworthy.
2. Build scanner discovery as a separate layer feeding the existing 5M authority gate.
3. Add a unified, read-only decision dashboard using stable domain models.
4. Add transition alerts and position/risk context with explicit freshness and unknown-data handling.
5. Consider natural-language analysis only after it can answer from auditable MCC data.
6. Keep live broker connectivity and order placement out of scope unless separately authorized and specified.

## Public sources

- [Clear Street trading platform](https://www.clearstreet.com/) — screener, options, execution, Omni, risk, alerts, and API positioning.
- [Clear Street Trading API](https://www.clearstreet.com/api) — programmatic research/trading integration and stated API capabilities.
- [Clear Street API documentation](https://docs.clearstreet.com/) — API setup, account, order, error handling, and developer resources.

## Limitations

This review uses public product descriptions, not a hands-on trial, complete API implementation audit, or independently verified performance claims. Clear Street's actual feature availability may depend on account, permissions, and product access.
