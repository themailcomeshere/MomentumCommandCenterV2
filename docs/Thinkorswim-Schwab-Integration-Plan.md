# Thinkorswim / Schwab Integration Plan

**Status:** Individual Trader API access approved by the user (2026-11-02). OAuth app registration, callback URL, app credentials, token exchange, and read-only account/position sync are not yet configured or implemented. No Schwab credentials, account data, or positions are connected.

## Schwab app creation form — recommendation (2026-11-02)

The user reports the Trader API — Individual request is approved and has opened the Create App form.

- **API Product:** Select **Accounts and Trading Production** for Phase 1 read-only account balances/positions. Do not select **Market Data Production** yet; MCC already has Alpaca for market data, and a second market-data source is not currently required for position synchronization.
- **App Name:** `Momentum Command Center` (the portal displays this name to the end user).
- **Description (optional, under 255 characters):** `Read-only account and position synchronization for Momentum Command Center. No order placement or trading actions are performed.`
- **Callback URL(s):** Do **not** submit a guessed URL yet. The MCC OAuth callback route has not been implemented, and the permanent HTTPS hosting origin has not been selected. Schwab's portal documentation says the callback is where the user returns after authorizing the app; some API product lines enforce HTTPS. Register the exact URL only after the route and host are established. The local development profile currently has `https://localhost:53359`, but that is not yet a verified Schwab callback or production URL.

The app can remain uncreated until the callback is ready. This is lower priority and does not block current MCC work. Never paste App Key, App Secret, authorization codes, or tokens into chat or Git.

Official portal guide: https://developer.schwab.com/user-guides/apis-and-apps/create-an-app

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


## Approval update — 2026-11-02

The user confirmed that **Schwab Trader API — Individual has been approved**. This clears the Individual Developer / product-access prerequisite in the earlier checklist. It does not, by itself, establish that an MCC app has been created, that the app is Active/Ready for Use, or that OAuth/account access has been tested.

Official Schwab guidance distinguishes API product access from app creation and app approval:
- Individual Trader API product: https://developer.schwab.com/products/trader-api--individual
- Create an app: https://developer.schwab.com/user-guides/apis-and-apps/create-an-app

### Remaining operator setup

1. In the Schwab Developer Portal, open **Dashboard → Apps → Create App**.
2. Select the approved **Accounts and Trading Production** product for account/position reads. Do not add order-entry functionality to MCC.
3. Register the exact callback URL to be used by the local MCC API. The URL sent in the OAuth authorization and token exchange must match the portal registration exactly, including scheme, host, port, path and trailing slash. Do not guess or use a callback that MCC does not yet host.
4. Complete any app-level approval and wait until the app is **Active / Ready for Use**. Schwab's guide describes app creation and app status separately from Individual Developer/API product access.
5. Store the App Key and App Secret only in local server-side secret storage. Do not send them in chat, screenshots, GitHub issues, commits, or frontend code. No token file should be committed.
6. Provide only non-secret setup metadata for implementation: app status, registered callback URL, and confirmation that Accounts and Trading Production is attached. Do not provide the App Key, App Secret, authorization code, access token, or refresh token in chat.

### MCC implementation gate

Once the callback URL and app status are known, implement the local authorization-code flow and read-only endpoints. Use server-side token handling, state/CSRF validation, protected token persistence, refresh/reconnect status, and read-only `GET` account/position calls. Never log token or account identifiers. Position retrieval must distinguish:
- authenticated account with zero positions;
- authenticated account with positions;
- missing/partial position payload;
- expired/revoked authorization;
- provider/network failure;
- stale last-successful snapshot.

The API should expose a sanitized connection/position-sync status, and normalized snapshots must be timestamped and auditable. V2 must receive a position context with quantity/side/entry/source time; a failed sync must never be treated as `hasPosition=false`.

**Hard boundary:** Phase 1 and Phase 2 are read-only. No order preview, create, replace, cancel, or submission endpoint is to be wired into MCC. The API product's broader capabilities do not grant MCC permission to act on them.

### Current disposition

- [x] User reports Individual Trader API approval.
- [ ] Create the MCC Schwab app and attach Accounts and Trading Production.
- [ ] Confirm app status is Active / Ready for Use.
- [ ] Register and confirm the exact local MCC callback URL.
- [ ] Configure App Key / Secret in local server-side secret storage.
- [ ] Implement and test OAuth, protected token storage/refresh, and read-only account/position retrieval.
- [ ] Implement position snapshot provenance and fail-closed position context.
- [ ] Keep all order-writing functionality out of scope.

## [Uncertainties]

- The exact Schwab Developer Portal app status is not yet known.
- It is not yet known whether the approved product access includes the required Accounts and Trading Production product on the app.
- The callback URL and local HTTPS hosting arrangement have not been selected.
- No Schwab credentials or account response have been supplied or tested.
