# P2/P3 Engineering Execution Plan — 2026-11-10

**Target branch:** `feature/opportunity-quality-classification`  
**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Baseline supplied by operator:** local Release build + 267/267 tests passed at `8304f274724a2f1c0d2e9cb2eefbfc222e1ef2b8`.  
**Repository verification:** that SHA exists in GitHub and its commit message is “Reconcile README with November 10 runtime evidence”. The available combined-status query returned no status records. Therefore exact-head CI is **NOT VERIFIED by this check**; an empty status response is not a pass.

## Scope and safety

This is a decision-support application. Order submission remains NOT IMPLEMENTED and is out of scope. No implementation may create, replace, cancel, or submit broker orders. Actual broker execution facts must come from broker-supplied order/fill records. OHLCV bars, Strategy Report order-event rows, target touches, and calculated prices are not fills.

## Current evidence and architecture found

- The repository already contains a Schwab Order History CSV importer at `src/MomentumCommandCenterV2.Infrastructure/Schwab/SchwabTradeImporter.cs`, an `ITradeImporter` abstraction, broker-independent `TradeImportResult`/trade models, and `UnmatchedOrder`.
- The current importer filters filled trade orders, reads a single fill price and quantity per CSV row, sorts by timestamp, then matches buys and sells FIFO. It retains unmatched sell quantity and remaining buy lots as open positions.
- The importer currently collapses each input row to a single fill-price/quantity event. The inspected source does not establish a durable broker order identity, multiple execution IDs, per-execution fees, partial-fill aggregation, correction/cancel lineage, import-batch identity, or persisted idempotent reconciliation. The existing CSV import is a useful historical foundation, not proof that full broker reconciliation is complete.
- The README and current project docs state that Schwab API developer approval, app registration, callback and OAuth remain prerequisites. No order-writing API is in scope.
- The current Master To-Do already records the four requested P2/P3 themes but only at summary level. This plan defines the missing implementation boundaries and acceptance evidence.

## P2.1 — Broker order/fill reconciliation

**Status: NOT IMPLEMENTED / NOT VERIFIED.** Existing CSV importer is a partial historical importer; do not mark this project complete.

### Required canonical records

1. **BrokerOrder** — broker, account reference (non-secret/non-display-safe identifier), broker order ID, client order ID if supplied, symbol, side, order type, requested quantity, limit/stop price if present, submitted/updated timestamps, broker status, source payload reference and import batch.
2. **BrokerExecution** — broker execution/fill ID, broker order ID, symbol, side, execution quantity, execution price, execution timestamp, currency, fee amount/currency, liquidity/venue fields when supplied, source payload reference and import batch.
3. **OrderEvent / Correction lineage** — append-only event identity, event type (accepted, routed, partial fill, fill, cancel requested, canceled, rejected, corrected, busted), broker event/version identifiers, supersedes/reverses relationship, effective time, observed time, and raw-source reference.
4. **Reconciliation result** — matched order/execution quantities, remaining quantity, status conflicts, duplicate detection, correction chain, fee totals, and unresolved exceptions. Preserve unmatched executions/orders for review.

Do not invent identifiers absent from a broker export. A missing fee is UNKNOWN, not zero. A canceled order may still have partial executions. A correction/bust must reverse or supersede the prior execution through an explicit lineage record; never mutate away the original evidence.

### Processing rules

- Ingest immutable source files/API pages with SHA-256, source name, retrieval/import time, account scope, schema version and batch ID.
- Idempotency key must prefer broker execution ID; otherwise use a documented composite only when the broker supplies no stable ID, and flag the reduced confidence.
- Preserve raw rows/payloads and parser version. Re-importing the same batch must not duplicate orders, executions, fees or derived lots.
- Reconcile order requested quantity against sum of active executions; partial fills remain partial until broker status/quantities establish otherwise.
- Apply corrections and busts as append-only compensating events, then recompute the derived position/trade view deterministically.
- Allocate fees to executions first. Any trade-level allocation must be explicit and deterministic (quantity-proportional with residual rounding policy documented); retain original fee currency and amount.
- FIFO matching may remain a reporting projection, but it must be based on executions and retain links back to each execution. Multiple fills on one order remain distinct.
- Keep theoretical target-touch/backtest observations in a separate model and UI; never join them as broker fills.

### Tests and acceptance

- Multiple partial fills for one order; multiple orders for one symbol; partial sell against multiple buy lots; partial buy remaining open; over-sell/unmatched execution; duplicate batch; duplicate execution across batches; out-of-order events; canceled order with partial fill; correction, bust and re-fill; fees in multiple currencies; missing fee; daylight-saving timestamp boundary; malformed row; unsupported schema.
- Re-import produces zero new canonical events and identical derived totals.
- Reconciliation report exposes every mismatch and source row.
- Acceptance requires at least one real, redacted broker export containing partial fills, fees and a correction/cancel case if the broker supplies those fields. If the supplied export does not contain a case, mark that scenario NOT OBSERVED and retain a synthetic unit test only.
- No OHLCV-derived fill inference; no order submission.

### Immediate implementation gate

Before changing the parser/model, capture the exact current Schwab CSV headers and representative redacted rows, plus any available Activity/Transactions export that exposes execution IDs and fees. **Data not found in sources:** a representative raw broker export proving the exact fee, execution-ID and correction fields is not present in the inspected repository sources. Do not guess Schwab column names or semantics.

## P2.2 — Schwab read-only API integration

**Status: BLOCKED ON EXTERNAL APPROVAL / OPERATOR SETUP.** Do not implement token acquisition against invented app settings or callback details.

### Preconditions

- Schwab developer/API approval is active for the required Individual account use.
- App is registered and the required Accounts/Trading read scopes are explicitly granted.
- Production callback/redirect URI is registered and matches the application exactly.
- Client ID/secret and refresh token are stored only in an OS-protected secret store or server-side secret provider. Never commit them, expose them to the browser, write them to logs, or place them in ordinary appsettings.
- Confirm current Schwab API product documentation, OAuth flow, token lifetimes, refresh behavior, rate limits and read scopes before implementation.

### Read-only boundary

- Initial endpoints: account identity/metadata needed for mapping, positions, and orders/order history if the approved scopes support them.
- Do not request trading-write scopes. Do not create order, replace order, cancel order, or preview/submit order endpoints.
- Adapter must be read-only by construction: expose query methods only; no generic HTTP method exposed to the UI; allowlist GET routes; reject non-GET verbs.
- Persist minimal normalized snapshots and provenance, with account identifiers redacted in UI/logs. Include disconnect/revoke, token refresh failure and stale-data states.
- Reconcile API order/fill records into P2.1 canonical records; do not create a separate incompatible execution model.

### Acceptance

- OAuth authorization succeeds using the registered callback; state/nonce/PKCE protections follow current Schwab requirements.
- Access token never reaches frontend/browser storage or logs.
- Refresh, expiry, revoked consent, timeout, rate limit and provider outage are handled without converting stale data to current.
- Positions and orders are read-only and match the Schwab UI/export for a documented timestamp.
- Automated tests assert no non-GET broker request can be emitted by this integration.
- Explicit operator confirmation of read-only scopes and account mapping is required before live acceptance.

## P2.4 — Deployment, backup and recovery

**Status: DESIGN / IMPLEMENTATION NOT VERIFIED.** Current app runs locally through the .NET API and static Dashboard; production packaging and unattended lifecycle must not be assumed.

### Deployment decisions to freeze

- Target: Windows workstation first (consistent with the current Visual Studio/.NET 8 workflow); do not choose installer/service technology until the operator confirms whether MCC should run only while logged in or unattended.
- API lifecycle: one owner for start/stop/restart; single-instance guard; health/readiness endpoints; graceful shutdown; bounded startup recovery; explicit port conflict handling.
- Data root: separate code/config/secrets from durable data. Keep JSONL, imported references, reconciliation events and logs outside replaceable binaries. Document retention and disk limits.
- Backup: consistent snapshot of all durable stores plus manifests (UTC timestamp, app/source SHA, schema versions, file sizes and hashes). Exclude secrets by default; back them up only through a separately approved encrypted secret-store process.
- Restore: restore into a staging directory, validate hashes/schema/record counts, run integrity checks, then atomically promote; never overwrite the only good copy.
- Logging: structured rolling logs, bounded retention, size cap, UTC timestamps, correlation IDs; redact credentials, tokens, account IDs and sensitive request headers/bodies.
- Monitoring: health/readiness, last successful collector cycle, per-symbol watermarks, WebSocket status, provider status, storage write errors, disk space and process uptime. Alert on stale/failed data; do not imply health means data completeness.
- Recovery: restart after app crash, OS sleep/wake, network loss, Alpaca auth failure/rate limit, WebSocket reconnect, duplicate/replayed events, partial persistence and interrupted backfill. Watermarks and idempotency must prevent gaps/duplicates.

### Required test matrix

- Clean install/start; normal stop; forced process termination; port occupied; corrupt/missing config; missing secrets; disk nearly full; disk full/write failure; backup during active writes; restore to clean machine/profile; corrupted backup; schema migration; Windows sleep/wake; network loss/recovery; provider outage/rate limit; repeated collector restart; duplicate stream events.
- Verify a restored dataset has matching manifest hashes and counts, and collector resumes from persisted watermarks without duplicate observations.
- Measure start/stop/recovery duration and record exact machine/OS/build/source SHA.
- Acceptance requires an operator-run Windows recovery drill and a successful restore into a separate location. A backup file existing is not restore acceptance.

## P3 — Release governance

**Status: PARTIALLY DOCUMENTED; exact-head gate not yet evidenced for supplied SHA.**

Every candidate release must have a single immutable source SHA and a release evidence record. The release decision is the conjunction of independent gates, not a single green build.

### Required gates

1. **Branch/source gate:** branch is `feature/opportunity-quality-classification`; working tree clean; record exact commit SHA; no master changes.
2. **GitHub CI gate:** workflow is triggered for that exact SHA; Release build, tests, JavaScript syntax/lint checks and artifact generation pass. A missing status/workflow result is UNKNOWN, not PASS.
3. **Operator local gate:** fetch latest feature branch, verify SHA, Release build and complete test suite; record totals, duration, warnings/errors and machine/runtime.
4. **API/runtime gate:** start the built API, confirm process/source SHA, health/readiness, provider configuration provenance and key endpoint smoke checks. API health does not establish feed entitlement or market data completeness.
5. **Browser gate:** hard-refresh main `frontend/index.html`; capture console exceptions, failed required requests, key workflows, responsive layout, timing and screenshots. PrototypeIndex is not an acceptance target.
6. **Thinkorswim gate:** compile exact source in TOS and record study name/version, aggregation, ATR length/type, extended-hours, adjustment, timezone, compile outcome and matched-bar/state evidence. .NET CI cannot compile ThinkScript.
7. **Independent data gate:** record provider, feed, entitlement, symbol, timeframe, adjustment, session, UTC interval, retrieval time and independent reference. IEX_PARTIAL remains diagnostic; SIP-dependent gates remain parked after known 403 unless operator reports entitlement change.
8. **Release decision:** publish PASS/FAIL/UNKNOWN per gate, blockers, rollback point and artifact hashes. No gate may be silently waived.

### Evidence bundle

Store one dated release record with: source SHA; CI run URL/status; local build/test output; API process SHA/health; browser diagnostics/screenshots; TOS compile evidence; independent data/parity evidence; backup/restore status; known blockers; and release decision. Preserve raw evidence, not only a summary.

### Immediate exact-head check

The supplied SHA `8304f274724a2f1c0d2e9cb2eefbfc222e1ef2b8` exists in GitHub. The available combined commit-status query returned an empty status list. **Exact-head CI: UNKNOWN / NOT VERIFIED by this check.** Operator reports local Release build + 267/267 tests passed at this SHA; this is accepted as operator-reported local evidence, not independently executed here.

## Execution sequence and division of work

1. Operator supplies a redacted current Schwab Order History/Activity export (include headers and representative rows; preserve partial fills and fee/correction fields if present). Engineering then implements the actual schema without guessing.
2. Operator confirms developer approval, registered app, callback and read-only scopes. Until then Schwab OAuth remains blocked; proceed with the generic read-only adapter contract and tests only if it does not assume provider-specific endpoints.
3. Engineering audits current data-store and hosted-worker lifecycle before selecting backup/restore implementation; then adds manifest-based backup/restore and recovery tests.
4. Engineering adds an exact-SHA release evidence script/workflow and immutable release record; operator performs Windows/TOS/browser acceptance.
5. Only after each independent gate is evidenced can the corresponding project status move to COMPLETE.

## Status snapshot — 2026-11-10

| Workstream | Repository implementation | External/operator evidence | Overall |
|---|---|---|---|
| Broker reconciliation | Partial CSV importer; no verified full execution ledger/correction model | Actual fee/ID/correction sample absent | OPEN |
| Schwab read-only | Existing CSV importer only; OAuth API not verified | Developer approval/app/callback/scopes not evidenced in current sources | BLOCKED |
| Deployment/recovery | Current local .NET API/JSONL architecture; production backup/restore lifecycle not evidenced | Windows restore/recovery drill absent | OPEN |
| Release governance | Existing CI/build/test scripts and runbooks; no complete immutable release bundle | Exact-head CI for supplied SHA returned no status records; local build/test reported PASS | OPEN |

## [Uncertainties]

- Whether Schwab's current export includes stable execution IDs, fees, correction/bust events or order-event version IDs is not established by the inspected files.
- Exact Schwab API scopes, OAuth parameters, token lifecycle and endpoint availability must be confirmed against current approved developer documentation after approval.
- The target Windows deployment mode (interactive-only vs unattended) has not been frozen.
- The available GitHub status endpoint returned no status rows for the supplied SHA; no inference about whether a workflow is pending, absent or inaccessible is safe.
