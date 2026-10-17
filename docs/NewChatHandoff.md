# Momentum Command Center — New Chat Handoff

**Updated:** 2026-10-16  
**Repository:** [MomentumCommandCenterV2](https://github.com/themailcomeshere/MomentumCommandCenterV2)  
**Working branch:** `feature/opportunity-quality-classification`

## Project intent and boundaries

This is a C#/.NET 8 research and decision-support prototype, not an auto-trader. Preserve the architecture: 5M is the authority/permission timeframe; 1M may consume 5M context for execution timing, but 5M must never depend on 1M. Entry eligibility, opportunity quality, entry timing, position management, and data confidence are separate dimensions. No automatic order placement or Schwab order submission.

## Current branch status

The branch contains the opportunity-quality diagnostic slice, separated models/enums, version-controlled 1M/5M ThinkScript references, development rules under `docs/rules/abpDevRules.md`, and the market-data/replay foundation:
- Raw `MarketBar` OHLCV contract with symbol, timeframe, and timestamp.
- CSV reader using invariant numeric parsing and timezone-aware timestamps.
- Deterministic replay frames: each 1M bar sees only a 5M bar whose timestamp is <= the 1M frame timestamp.
- Replay CLI command and synthetic sample CSV.
- Unit tests for no-look-ahead, duplicate bars, and invalid OHLC.
- Static front-end scaffold under `frontend/`, explicitly marked demo data.

The user reports that the previous branch build and unit tests passed. A new GitHub Actions workflow was added; the replay foundation's build and tests passed on commit `fdefa8fb85fee0c2e68f17e4848201894be5fc4b`. A second run covering the subsequent signal-engine documentation-only change is in progress as of this handoff. The user should still pull and build locally to verify the exact working environment.

## Canonical documents and source files

- `README.md`: project overview and top-level roadmap.
- `docs/Master-To-Do.md`: canonical master checklist. It preserves the prior 180-line To-Do and records which sections are done, deferred, or still unresolved.
- `docs/archive/README_ORIGINAL.md`: archived historical source after reconciliation against the current README and master checklist.
- `docs/BoringOvernight.md`: strategy and intended Design vs Use workflow; retain separate Overnight Edge and Boringness concepts.
- `docs/Opportunity-Quality-and-Ranking.md`: diagnostic classification specification; provisional bands are not calibrated trading rules.
- `docs/Market-Data-and-Replay-Contract.md`: raw data, CSV, replay guarantees, limitations, and next steps.
- `docs/Charts/1M/` and `docs/Charts/5M/`: version-controlled current MainStudy and DiagnosticStudy ThinkScript sources. Do not rename/replace a baseline without verifying the installed version and intended role.
- `docs/rules/abpDevRules.md`: development rules. Flowerbox every class and method with purpose and when called; keep layer dependencies clean.

## Data/replay contract

CSV header: `Symbol,Timeframe,Timestamp,Open,High,Low,Close,Volume`. Timeframe `1m` or `5m`. Timestamps must carry timezone offsets. The bar timestamp convention (open vs close) is not yet settled for an actual provider; document it before integration. The reader does not calculate VWAP, EMA/SMA, RSI, MACD, ATR, RVOL, scanner/catalyst events, or fills. Do not infer missing values. Golden parity tests against exact ThinkScript versions are required before signal integration.

## Current UI

Open `frontend/index.html` directly in a browser. It is static and requires no server/build. Values are illustrative UI fixtures, not live feed, not calculated by C#, and not trading advice. The next UI step should consume a documented JSON replay output after the data contract stabilizes.

## Next engineering sequence

1. Build and run all tests after the market-data/replay changes.
2. Reconcile every line of `README_ORIGINAL.md` and `To Do List.md` against current docs/code; migrate the master checklist into `docs/Master-To-Do.md` and remove root duplicates only after the comparison is complete.
3. Maintain the XML flowerboxing standard; a source audit added purpose/when-called comments to the classes and methods found in the current C# files. Recheck new code during review.
4. Add sanitized historical 1M/5M fixtures and provenance metadata.
5. Define regular/extended session rules, timestamp open/close semantics, and DST handling.
6. Port TOS indicators one at a time with golden parity tests.
7. Connect replay frames to V2 signal evaluation without changing current trading rules; then record decision transitions and opportunity assessments.
8. Keep Boring Overnight scoring separate from intraday momentum; treat catalyst status unknown unless a reliable event source exists.

## Important validation context

A prior low-momentum entry (MNSB) raised concern that `EXECUTION READY` does not communicate opportunity quality. The quality label is intended to distinguish `READY — HIGH`, `READY — MODERATE`, `READY — BORING`, and `READY — EXTENDED`, without changing the underlying eligibility gate. Two individual trades do not prove a code defect; validate against timestamped historical market snapshots.

## Development note

The current project is not an ABP Framework app. Apply compatible C# conventions, separation of concerns, testing, and flowerboxing from `abpDevRules.md`; do not add ABP modules, Entity Framework, authentication, or distributed caching without an actual requirement.
