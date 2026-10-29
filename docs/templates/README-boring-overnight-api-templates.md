# Boring Overnight API request templates

## Timezone rule for every Swagger endpoint

MCC API date/time inputs use **UTC ISO-8601 timestamps ending in `Z`**. Do not paste Arizona wall-clock time into a UTC field.

For October 26, 2026, Arizona is MST (UTC−07:00):
- `2026-10-26T13:30:00Z` = `2026-10-26 06:30:00` Arizona time.
- `2026-10-26T19:59:00Z` = `2026-10-26 12:59:00` Arizona time.
- `2026-10-26T20:00:00Z` = `2026-10-26 13:00:00` Arizona time.

Use the Swagger descriptions for each endpoint; they now show UTC query examples and Arizona equivalents where relevant.

## Template file

Open [boring-overnight-evaluate-with-history.template.json](boring-overnight-evaluate-with-history.template.json) and paste its JSON into Swagger's `POST /api/boring-overnight/evaluate-with-history` request body **only after replacing the marked placeholders and verifying the bar timestamps**.

The file has 20 boundary objects and all required JSON fields. Its date range and timestamps are **illustrative examples, not evidence that those bars exist in your local store**. The timestamps `19:59:00Z` and `13:30:00Z` assume one-minute bar-start timestamps for the last regular-session bar and first regular-session bar. Providers/feeds may differ. Exact matching is required by the API.

## Make the template usable with your actual stored data

1. Call `GET /api/market-data/stored-bars?symbol=SPY&fromUtc=2026-09-30T13:30:00Z&toUtc=2026-10-28T20:00:00Z&provider=Alpaca&limit=10000` in Swagger. These dates are example bounds; expand/reduce them to match data you actually have.
2. Inspect each returned observation's source timestamp, provider, feed, symbol, and OHLC. Confirm the feed is the same for all boundary bars.
3. For each session, select the exact regular-session final bar and the next session's first regular-session bar. Copy each bar's exact source timestamp into the corresponding boundary. Do not convert an Arizona timestamp and then guess its UTC equivalent; copy source timestamps from the response.
4. Confirm every `previousSessionDate` and `nextSessionDate` refers to an actual exchange session. The API does not currently generate or validate the authoritative exchange calendar.
5. Replace `asOfUtc` with the actual UTC time at which you are evaluating the snapshot (ending in `Z`). Replace `source`, `calculationVersion`, and `fieldSourceTimestampsUtc` with truthful provenance. Keep the timestamp dictionary empty only if you do not have field-level source timestamps to report; do not invent them.
6. Fill the non-history fields in `evaluationRequest` only from actual data. Nulls intentionally produce `DATA?`; do not use example guesses to make a candidate qualify. The integrated endpoint overrides caller-supplied historical metrics and forces `catalystStatus` to `UNKNOWN` until a validated catalyst source is connected.

## What to expect

- If all 20 pairs match and there are no missing bars, `history.allPairsComplete` and `history.enoughHistory` can be true.
- If a bar is missing, inspect `history.missing[].reasonCode`; the historical evaluator metrics are nulled rather than filled from the request.
- The endpoint appends a snapshot. Read it back using `GET /api/boring-overnight/snapshots?symbol=SPY&limit=10`.
- If `count: 0` is returned, no snapshots are present at the store path used by this API process. Check that the POST returned success, that you are querying the same running instance, and that `MCC_BORING_OVERNIGHT_SNAPSHOT_PATH` has not changed between runs.

## Negative test

Use a copied request and change one boundary timestamp to a time for which no bar exists in the selected provider/feed response. Keep the timestamp valid UTC and ensure it is within the query range. The response should report the missing pair; history-backed evaluator metrics should be null and the candidate should not become READY.

**Safety note:** even complete history does not mean production hold authorization. Session calendar validation, corporate actions, source coverage, catalyst/news, and chart-study inputs remain separate requirements.
