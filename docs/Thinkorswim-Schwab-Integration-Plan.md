# Thinkorswim / Schwab Integration Plan

**Status:** Design only. No Schwab credentials, account data, or positions are connected.

## Important distinction

Thinkorswim is Schwab's trading platform. MCC should not attempt to scrape the Thinkorswim desktop UI or pretend that the desktop application itself is an API. The supported integration path to investigate is Schwab's **Trader API — Individual**, a separate developer product for an individual's own self-directed Schwab brokerage account.

Official entry points:
- https://developer.schwab.com/products/trader-api--individual
- https://developer.schwab.com/user-guides/individual-developer/become-individual-developer
- https://developer.schwab.com/user-guides

Schwab describes the Individual Trader API as providing authentication, market data, account information, order/transaction information, order entry, and order preview. A Schwab brokerage account is required; developer registration and API product access are separate approval/setup steps.

## Recommended implementation sequence

### Phase 1 — Read-only account and position visibility

1. User creates/logs into a Schwab Developer Portal account and adds the Individual Developer role.
2. User requests access to the Trader API products needed for account/position and market-data reads.
3. User creates an app and supplies the OAuth client configuration through server-side secrets/configuration. Never put client secrets or access/refresh tokens in frontend JavaScript or commit them to Git.
4. Implement OAuth authorization-code flow, encrypted token persistence, expiry handling, refresh, and a clear reconnect-required status.
5. Implement read-only account and position endpoints. Retrieve the Schwab brokerage positions associated with the account; do not assume Thinkorswim's visible UI state is a separate source of truth.
6. Normalize positions into a versioned MCC contract: account identity (non-display internal identifier), symbol, long/short quantity, average price/cost basis when supplied, market value when supplied, source timestamp, and source status.
7. Reconcile position snapshots and detect missing, duplicate, stale, or changed positions. Never silently interpret a failed positions request as an empty account.
8. Pass an explicit position context into V2. The current boolean `hasPosition=false` is too coarse for robust management; evolve it to include signed quantity, average entry, and source timestamp. Keep long-only V2 rules explicit until short-position rules are separately designed and tested.

### Phase 2 — Position-aware V2 management

- Evaluate the current completed 1m/5m bars with the real position state.
- Enable existing-position states (runner, runner waning, prepare sell, confirmed weakness, breakdown, EOD exit) only when position data is fresh and reconciled.
- Distinguish "no position" from "position data unavailable". A failed account connection must not be treated as `hasPosition=false`.
- Display position source, account alias, quantity, average price, last update, and connection health in MCC.
- Keep all decisions advisory. No order placement in this phase.

### Phase 3 — Optional order preview / execution

Do not begin until read-only position synchronization and decision logic have passed independent tests. Order preview and order submission are separate permissions and workflows. Require explicit user authorization, risk limits, duplicate-order protection, audit records, and a kill switch. Do not infer permission to trade from permission to read positions.

## What this means for the positions currently visible in Thinkorswim

If those are positions held in the user's Schwab brokerage account, the Schwab Trader API is the appropriate route to retrieve the brokerage positions. MCC does not read them merely because Thinkorswim is open on the same computer. A separate authenticated Schwab API connection must be configured and approved.

Interim alternative: a user-supplied positions export/import or manually entered position list can support early testing, but must be timestamped and clearly marked as manual/stale-capable. It is not a live sync.

## Cost and access notes

- Schwab says thinkorswim platform access is available to Schwab clients at no platform charge; normal brokerage commissions/fees still apply where relevant.
- The official Trader API Individual product page describes eligibility and capabilities but does not publish a simple monthly subscription price. Do not promise that API access, data entitlements, or every API capability is free; verify current portal terms and Schwab approval before implementation.
- A Schwab brokerage account is required for the Individual Developer role.
- API access and Thinkorswim platform access are related to the same brokerage relationship but are not the same integration.

## MCC engineering requirements

- New provider abstraction: `ISchwabAccountProvider` and normalized position DTOs.
- OAuth callback hosted by MCC API; tokens encrypted at rest and never logged.
- Secret configuration only via environment variables/user secrets or a secure secret store.
- Read-only account and position endpoints first.
- Health endpoint reports configured/authenticated/last successful sync/position age/error state.
- Position snapshots are append-only/auditable; no missing-data-to-empty conversion.
- V2 receives a position context, not a hard-coded boolean.
- Tests for long, short, flat, partial fills, multiple accounts, stale auth, revoked access, API errors, and missing position payload.
- No order placement in Phase 1 or Phase 2.

## Open decisions

- Confirm the brokerage account type and whether the positions in Thinkorswim are held at Schwab.
- Confirm Schwab Developer Portal Individual Trader API approval and exact products available to the account.
- Confirm whether MCC needs only position state or also live account balances, open orders, fills, and executions.
- Decide whether the initial integration should be read-only Schwab API or a manually imported position snapshot while access is pending.


## Implementation status update — 2026-10-30

Read-only Schwab position synchronization is **not implemented yet**. The repository now has a historical replay toggle that can simulate the existing engine's boolean `hasPosition` input, but it does not retrieve a real Schwab account or position.

### User setup required before connector implementation
1. Register/log in to the Schwab Developer Portal.
2. Add the Individual Developer role to the portal profile (a Schwab brokerage account is required).
3. Request access to Trader API Individual and the Accounts & Trading production API product.
4. Create an approved app and register its callback URL. The callback must match the actual MCC API deployment URL; do not invent a localhost/production URL before the hosting choice is known.
5. Keep the app key/secret private and enter them into server-side secret storage. Do not paste credentials or OAuth tokens into chat, source control, frontend JavaScript, or screenshots.

The official product page does not publish a clear monthly API price. Confirm fees, entitlements, and approval with Schwab before relying on a cost assumption.

### Connector acceptance criteria
- OAuth authorization-code flow with state validation and secure token refresh/storage.
- Read-only account-number/hash and position retrieval; no order endpoints wired into MCC.
- Normalize symbol, signed quantity, long/short side, average entry price, market value, and source/update timestamp.
- Distinguish no position from failed/stale/unauthorized sync; unknown position state must fail closed.
- Reconcile repeated snapshots and record provenance; never silently treat a missing account response as flat.
- Position-aware engine input must expand beyond a boolean before production use; test quantity/side/entry-aware management explicitly.
- Order placement stays out of scope and disabled.
