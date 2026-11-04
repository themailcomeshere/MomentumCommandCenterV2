# Overnight Scanner → Boring Overnight: Operator Workflow

**Purpose:** A repeatable, evidence-first procedure for reviewing a Thinkorswim Overnight Scanner export in MCC. This is decision support; MCC does not submit orders.

## 1. Before the evaluation window

1. Update the feature branch and run `.scripts\Get-Latest-And-Test.ps1`. Restart the API only after the old process has stopped.
2. Confirm `/api/health` is OK and inspect `/api/providers/status`. Record the effective provider/feed and configuration source. Never assume tracked `appsettings.json` wins over local User Secrets or environment variables.
3. Confirm the market-session clock and use the exchange calendar. Start review in the **final 60 minutes before the actual regular-session close**. The buy deadline is the actual close, including early-close days. Show operator times in America/Phoenix; do not hardcode a fixed Arizona clock time across Eastern DST changes.
4. Export the complete current Thinkorswim Overnight Scanner results as CSV. The CSV is the scanner-to-MCC handoff; automatic Thinkorswim scan ingestion is not implemented.

## 2. Import and inspect the CSV

1. In MCC, select the scanner CSV file in the Overnight Scanner import control.
2. Confirm the import feedback's imported row count equals the CSV data-row count, and inspect parse/duplicate diagnostics. The Red Zone should switch to Imported CSV.
3. Review the historical columns separately: Overnight win rate, mean/median return, 5-day vs 20-day behavior, current overnight move, trend/above-SMA state, sample count, and exclusion status.
4. Treat the historical gates as a shortlist, not a trade decision. The export does not prove current price, validated 20-session daily liquidity, catalyst clearance, current 5M authority, 1M execution, extended-hours order eligibility, or a broker fill.
5. Red Zone **Automatic** mode is currently a supplied-scanner-score preview among rows passing the imported historical gates. It is not yet the calibrated live Opportunity Quality ranker. **Manual** mode shows only manually pinned symbols in saved order. Switching modes preserves pins and dismissals.

## 3. Review the Red Zone tiles

For each of up to four tiles:

- Check the source score separately from MCC's 5M score/Permission.
- Check 5M same-time-of-day RVOL and its baseline sample count. Do not interpret it as 1M RVOL.
- Check 1M close, Daily VWAP, ATR14, extension, and both 1M/5M freshness.
- Read the current 5M Permission and 1M execution state. A READY event is finite; do not treat an old state as current.
- Read the Opportunity Quality diagnostic and reason codes. Current thresholds and range/resistance proxies are provisional. The diagnostic cannot override V2 eligibility, Permission, HOLD, EXIT, or order actions.
- A tile with missing/stale inputs is DATA?/UNKNOWN, not a pass. Low participation must be visible even when other technical conditions look attractive.

Use **Open in Main Chart** to inspect the symbol. Use **Dismiss** to remove a candidate from Automatic preview, **Pin to Red Zone** to preserve it for Manual mode, and **Restore dismissed / repopulate** to clear dismissals for the selected scanner group. The sound control is optional and should not be treated as a signal.

## 4. Run Boring Overnight history and evidence capture

For each candidate worth deeper review:

1. Select the symbol in the Boring Overnight qualification panel.
2. Run the 20-session history capture. Inspect valid/missing close-to-next-open pairs, feed, exact boundary audit, and source timestamps. All requested pairs must exist; do not use nearest-bar substitution. An incomplete window must remain DATA?.
3. The successful capture appends an immutable evaluator-input snapshot. Inspect the saved-snapshot table and verify that historical metrics are null if any required pair is missing, and that current price, Daily liquidity, catalyst, and chart inputs remain UNKNOWN unless independently supplied.
4. Use the manual **Fetch Daily + catalyst for top 4** action only as a bounded refresh of the four highest-scored imported candidates. Daily qualification fields require complete, fresh SIP data. IEX Daily observations are diagnostic only. Catalyst phrase triage is provisional: no articles is UNKNOWN, and only an explicit risk phrase blocks; other categories require manual review. This action does not create READY.
5. Compare Daily close/SMA20/20D average volume against an independent source using the same adjustment basis. Review returned catalyst headlines, publication timestamps and source URLs against primary filings/news.
6. Save each article's human assessment separately from the automated triage. The local API now accepts append-only labels:
   ```powershell
   $api = "http://localhost:53360"
   $review = @{
       symbol = "EVER"
       articleId = "<article id from catalyst response>"
       headline = "<exact source headline>"
       url = "<exact article URL>"
       publishedAtUtc = "<source publication timestamp>"
       source = "Alpaca News"
       label = "MATERIAL_EVENT" # MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL, IRRELEVANT, DUPLICATE, UNCERTAIN
       ruleSetVersion = "MCC-CATALYST-TRIAGE-1.0"
       notes = "Why this article has this label, based on the article/filing."
   } | ConvertTo-Json

   Invoke-RestMethod -Method Post -Uri "$api/api/market-data/catalyst-reviews" -ContentType "application/json" -Body $review
   Invoke-RestMethod "$api/api/market-data/catalyst-reviews?symbol=EVER&limit=100"
   ```
   These labels are an audit dataset only. They do not clear a catalyst or change the candidate's eligibility.

## 5. Extended-hours sell-window research

The sell-window audit requires every exact 1-minute bar start from 07:00 through 19:59 ET (780 intervals) plus the exact previous regular-session close, for each of 20 sell sessions.

- Call `POST /api/boring-overnight/capture-sell-window-history` only when the provider's **effective** feed matches the requested feed.
- SIP is required for consolidated, qualification-usable research. IEX is partial-exchange diagnostic only, even if a timestamp count appears complete.
- Confirm `completeWindowCount=20`, `allWindowsComplete=true`, and `qualificationUsable=true` before connecting observations to the sell-target calculator.
- Missing intervals are not imputed. Historical high touches are not proof of executable quotes or fills. Extended-hours order eligibility depends on the broker/order qualifier and must be verified separately.

## 6. Make and record the human decision

Before the Day 1 regular close, record the candidate, timestamp, source/feed, historical evidence, Daily/liquidity evidence, catalyst review, 5M state, 1M state, current ON move, exclusion check, and manual decision/reasons. Zero position is valid. MCC is not an order-entry system.

After close, preserve after-hours and next-day premarket as distinct sessions. Track actual broker order status, fills, quantities, fees and exits separately from bar-derived historical target touches.

## Current limitations (do not interpret as completed)

- Automatic Thinkorswim scan ingestion is not implemented; CSV export/import is the handoff.
- The Red Zone Automatic rank is still scanner-score preview, not the full versioned Opportunity Quality rank.
- The current 09:30 ET one-minute bar Open is a proxy, not independently verified official opening-auction print.
- Alpaca effective feed may be IEX due local configuration override. SIP configuration alone does not prove entitlement; the recent SIP request has previously returned 403.
- GTC_EXT 20-session SIP history, ATR parity, walk-forward target validation, and actual fills/exits/fees are not yet evidenced.
- No automatic order submission exists.

## [Uncertainties]

The exact CSV row count, symbols and score distribution vary by export. Always use the current file's data-row count and source timestamp; do not assume a historical example's count or top-four list applies.


## 2026-11-04 — button-driven history capture and recovery

- After importing the CSV and narrowing candidates, use **Fetch Daily + catalyst for top 4** once for a bounded refresh of the four highest-scored imported rows. It fetches adjusted Daily bars and 24-hour ticker news; it is not a live entry/exit trigger and cannot clear catalyst risk.
- For a candidate with missing close/open pairs, use **Fetch / retry 20-session close/open history**. MCC re-fetches exact 1-minute provider bars, persists only returned observations, and audits all 40 expected close/open timestamps. EVR's eight missing 09:30 ET opens can only be populated if the provider returns those exact timestamps. Nearest bars are not substituted.
- For GTC_EXT sell-window research, use **Fetch 20-session GTC_EXT sell-window history** in the Calculated Limit Sell panel only when the effective Alpaca feed is SIP. The button preflights `/api/providers/status`; with IEX it stops without making a provider request. Each session requires all 780 exact one-minute starts from 07:00–19:59 ET plus the exact previous regular-session close.
- A numeric Calculated Limit Sell remains withheld until 20/20 exact SIP windows are complete, ATR parity is MATCH, and walk-forward target-touch/adverse-excursion validation is complete. Coverage and historical bar highs do not prove executable quotes or fills.
- Missing custom-expression metrics are `DATA?`, not historical FAIL. Review their source fields separately; do not discard them as failed stocks.
