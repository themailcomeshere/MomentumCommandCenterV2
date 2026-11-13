# Boring Overnight Workspace — Control Contract (source-traced)

**Traced:** 2026-11-13 from `frontend/app.js`, `frontend/index.html` and `src/MomentumCommandCenterV2.API/Program.cs` on `feature/opportunity-quality-classification`.
Closes the To-Do item "trace and document Fetch & analyze bars side effects". Browser Network confirmation is still an operator gate.

## Controls

| Control | Frontend handler | HTTP request(s), in order | Server side effects | Persists? |
|---|---|---|---|---|
| **Fetch & analyze bars** (`#overnightFetch`, form `#overnightBarsForm`) | `overnightBarsForm` submit listener | 1. `GET /api/market-data/bars?symbol&timeframe=1m\|5m&from&to&providerName=Alpaca` 2. then **automatically** `GET /api/boring-overnight/snapshots?limit=50&symbol=<symbol>` | Fetches provider bars for the window, **appends them to `market-bars.jsonl`** (idempotent; `newlyPersistedObservationCount` reports new rows), computes gap count. Does **not** create a snapshot or evaluation. | Yes — raw bars only |
| **Refresh saved evaluations** (`#overnightRefreshSnapshots`) | `refreshOvernightSnapshots()` | `GET /api/boring-overnight/snapshots?limit=50[&symbol=<symbol>]` | Reads up to 50 append-only snapshots and **recalculates** each evaluation under the current rules. No provider call, no write. | No |
| **Fetch / retry 20-session close/open history** (`#overnightCapture20`) | capture handler | `POST /api/boring-overnight/capture-history` `{symbol, providerName, requiredWindow:20}` | Fetches 1m bars in 30-day chunks up to *now* (never future), appends to `market-bars.jsonl`, calculates 20 historical pairs + separate current interval, fetches Daily + catalyst + live 1m/5m assessment, **appends one snapshot** to `boring-overnight-snapshots.jsonl`, returns the evaluation and a 42-row boundary audit. | Yes — bars + snapshot |
| Switching mode to **Boring Overnight** | `mode` change listener | `GET /api/boring-overnight/snapshots…` (via `refreshOvernightSnapshots`) | Read-only, same as Refresh. | No |

## Inputs and defaults

- **Bar interval:** `1m` (default) or `5m`.
- **From / To:** Arizona-local `datetime-local` inputs, converted with `phoenixLocalToUtc`. Client rejects From ≥ To and windows over 31 days; the API enforces the same.
- **Default window** (`initializeOvernightWindow`, applied only when both fields are empty): From = **13:00 Arizona** today (or yesterday if it is currently ≤ 13:00 AZ); To = now.
  - 13:00 AZ is 16:00 ET only during US **daylight** time. During **standard** time (e.g. 2026-11-13) 13:00 AZ is **15:00 ET**, so the default window starts one hour before the regular close. This is a descriptive default, not a calculation boundary; adjust From if you want the window to start at the close.
- **Symbol:** 1–8 of `A-Z 0-9 . -`; validated in the browser and on the server.
- **Provider:** hard-coded `Alpaca` in all three requests; feed is the configured feed (`iex`).

## What each result means

- The bars result is descriptive OHLCV evidence: provider/feed, count, first/latest UTC timestamps, age, gap count, and how many bars are at or after 13:00 AZ. Zero bars is "not proof of zero trading".
- "Refresh saved evaluations" can change a row's status without any new data. The rules are re-applied to the stored inputs; the stored inputs never change.
- Only the 20-session capture produces evaluator inputs. Fetch & analyze bars never changes a snapshot, so a refresh after Fetch & analyze shows the same snapshots (possibly with more history available to the *next* capture).

## Operator acceptance still required

1. In DevTools → Network, click **Fetch & analyze bars** and confirm exactly `GET /api/market-data/bars` followed by `GET /api/boring-overnight/snapshots`.
2. Click **Refresh saved evaluations** and confirm a single `GET /api/boring-overnight/snapshots`, with no `bars` or `capture-history` request.
3. Click the 20-session capture and confirm a single `POST /api/boring-overnight/capture-history`; the snapshot table should gain one row after the automatic refresh.
