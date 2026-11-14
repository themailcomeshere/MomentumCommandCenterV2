# Catalyst human-review sample plan

**Created:** 2026-11-14. Labels and rules: [Catalyst-Human-Review-Workflow.md](../Catalyst-Human-Review-Workflow.md).

## Where the sample stands

Review store on the operator machine, 2026-11-14: **7 records** for ARDT, ENOV and EWTX (MATERIAL_RISK 4, UNCERTAIN 2, MATERIAL_EVENT 1; 3 of them supersede earlier reviews). The EWTX conflict correction is still open (Master To-Do §7). That is far below a usable sample, and it has no negative labels (NON_MATERIAL / IRRELEVANT). Without negatives, precision and recall cannot be computed.

## Targets (proposed; change them if you want a different size)

| Target | Value | Why |
|---|---|---|
| Evaluable labels | ≥ 120 | Labels counted by the validation endpoint: MATERIAL_RISK, MATERIAL_EVENT, NON_MATERIAL, IRRELEVANT. |
| Human-positive (MATERIAL_RISK + MATERIAL_EVENT) | ≥ 40 | Enough holdout positives for a recall figure. |
| Human-negative (NON_MATERIAL + IRRELEVANT) | ≥ 40 | Enough holdout negatives for a false-positive figure. |
| Batches | ≥ 3, on different weeks | The validation holdout is the latest 20% **by review time**, so later batches form the holdout. |
| Max per symbol per batch | 3 | Keeps one news-heavy symbol from dominating. |

UNCERTAIN and DUPLICATE are allowed and honest. They are excluded from metrics, so they do not count toward the targets.

## How to do a batch

1. Start MCC: `.\scripts\MCC.ps1 start`.
2. Build the sheet:
   ```powershell
   .\scripts\New-CatalystReviewQueue.ps1 -SymbolFile <your symbols.txt>
   ```
   Defaults: 14-day lookback, a 60-row stratified sample (every RISK-predicted article, then MATERIAL and UNCLASSIFIED alternated, round-robin across symbols), 3 per symbol, and already-reviewed articles skipped. The sheet is written to `Documents\MCC-Reviews\`.
3. Open the CSV in Excel. For each row, open the **Url**, read the article (not just the headline), then fill **Label** and **Notes**. Notes need at least 40 characters of facts from the article and why they support the label. Leave Label blank to skip a row.
4. Dry run, which checks labels and note length and sends nothing:
   ```powershell
   .\scripts\Submit-CatalystReviews.ps1 -Path <sheet>
   ```
5. Save. This step is append-only: a saved review can only be superseded, never edited.
   ```powershell
   .\scripts\Submit-CatalystReviews.ps1 -Path <sheet> -Submit
   ```
6. Check progress with `GET /api/market-data/catalyst-reviews/validation`, which reports counts, holdout and confusion metrics.

**First batch is ready:** `Documents\MCC-Reviews\catalyst-queue-20261114-200253.csv`. It has 60 articles from 51 symbols: the 2026-11-13 ranking pool plus EWTX, ENOV, ARDT and IBN, over 14 days. Predicted: RISK 11, MATERIAL 25, UNCLASSIFIED 24. The predictions are the fixed triage rules, not truth; your label is the truth.

## Limits (state them in any result)

- One reviewer: agreement between two people cannot be measured. A blind re-review of 20% by the same person (intra-rater) needs a re-review mode that keeps the second label separate. Today it would register as a conflict. That mode is not built yet (To-Do §7).
- Alpaca News coverage and lookback completeness are not independently validated.
- The metrics describe the fixed MCC-CATALYST-TRIAGE-1.0 rules. They do not clear any candidate, and zero news stays UNKNOWN.
