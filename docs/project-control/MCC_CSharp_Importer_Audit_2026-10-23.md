# MCC V3 — C# Repository and Importer Audit
**Date:** 2026-10-23  
**Branch reviewed:** `feature/alpaca-readonly-provider-smoke-test`  
**Scope:** Source tree and existing trade importer; no source code changed by this audit.

## Verified repository layout
- Solution: `MomentumCommandCenterV2.sln`
- `src/MomentumCommandCenterV2.Core`: core models, `TradeAnalyzer.cs`, `V2SignalEngine.cs`
- `src/MomentumCommandCenterV2.Application`: `ITradeImporter`, `TradeImportResult`, trade analysis service/interfaces/models
- `src/MomentumCommandCenterV2.Infrastructure/Schwab/SchwabTradeImporter.cs`
- `src/MomentumCommandCenterV2.Infrastructure/Alpaca`: read-only Alpaca adapter
- `src/MomentumCommandCenterV2.Cli`: CLI entry point and User Secrets configuration
- `tests/MomentumCommandCenterV2.Tests`: Schwab importer, trade analysis, analyzer, signal engine, and Alpaca tests
- A legacy root-level `MomentumCommandCenterV2.Infrastructure` directory also exists alongside `src/MomentumCommandCenterV2.Infrastructure`; inspect its purpose before deleting or merging it.

## Importer work already implemented
- `ITradeImporter.ImportFile(string filePath)`
- `TradeImportResult` with completed trades, open positions, unmatched orders, imported order count, and errors
- Schwab CSV header validation and simple quoted-field parsing
- Uses Fill Price rather than submitted limit Price
- Parses `Time and Date(ET)` and resolves Eastern timezone IDs for Windows versus Linux/macOS
- FIFO buy/sell matching; open lots become open positions; unmatched sells are preserved
- Quantity is currently assumed to be 1 because the current source export reportedly lacks a quantity column
- Tests already cover fill price, buy/sell matching, open positions, canceled/open rows, multiple trades/symbols, unmatched sells, ordering, missing headers, malformed price, quoting, and money separators

## Gaps to address (do not duplicate existing implementation)
1. **ImportFile tests:** tests mainly call `Import(csv)`; direct disk import path, missing file, empty path, and encoding behavior need explicit tests.
2. **Duplicate/idempotency policy:** importing the same export twice will reconstruct the same trades again if the caller persists both results. There is no import batch identity, stable order ID, or deduplication contract yet.
3. **Partial failures:** malformed filled rows currently throw and stop the whole import; `TradeImportResult.Errors` is always empty for successful results. Decide whether fail-fast is intentional or add row-level diagnostics with a safe partial-result contract.
4. **Timestamp edge cases:** add tests for DST boundaries and invalid/ambiguous local times. Verify actual Schwab export formats across sample files before changing parsing.
5. **CSV limitations:** the custom parser handles quotes/escaped quotes per line but does not support quoted fields spanning multiple physical lines. Confirm whether this occurs in actual exports before adding a dependency or replacing the parser.
6. **Persistence:** repository/persistence abstractions are not present in the directories inspected. Do not claim durable trade storage until a storage layer and restart tests exist.
7. **Tests:** the GitHub status and workflow lookup returned no checks for the User Secrets fix. The user has confirmed the app builds/runs through the real Alpaca read-only smoke test, but a full `dotnet test MomentumCommandCenterV2.sln` result remains unreported.

## Exact next steps
1. [BASELINE PASSED 2026-10-25] User ran `dotnet test MomentumCommandCenterV2.sln`: 53 passed, 0 failed, 0 skipped; build succeeded. Re-run after the newly added Schwab exclusion provider/tests.
2. Add focused ImportFile/DST/malformed-row tests and agree on fail-fast versus partial-import behavior.
3. Define an import batch identity and deduplication policy before persistence; do not invent broker order IDs absent from the export.
4. Implement persistence and restart/idempotency tests after the storage model is selected.
5. Keep all brokerage functionality read-only; no order-placement functionality is authorized.
