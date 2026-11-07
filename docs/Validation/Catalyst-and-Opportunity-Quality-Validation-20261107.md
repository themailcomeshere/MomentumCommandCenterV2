# Catalyst and Opportunity Quality validation implementation — 2026-11-07

**Branch:** `feature/opportunity-quality-classification`  
**Dashboard:** `frontend/index.html`

## 1. Catalyst human-label validation

### What changed

- `CatalystHumanReviewStore` now stores the triage prediction severity and categories with each human review. This preserves what the rules predicted at the time of adjudication.
- The main Dashboard review submission sends those prediction fields.
- New API endpoint: `GET /api/market-data/catalyst-reviews/validation`.
- The endpoint deduplicates by symbol + article identity, excludes duplicate/uncertain human labels, legacy records without a saved prediction, and conflicting human labels for the same symbol/article.
- It splits the usable unique records chronologically; the later 20% is the holdout.
- It reports TP/FP/TN/FN, precision, recall, specificity, accuracy, support counts, holdout date range and exclusions.

### Label semantics

| Type | Positive | Negative | Excluded |
|---|---|---|---|
| Human truth | MATERIAL_RISK, MATERIAL_EVENT | NON_MATERIAL, IRRELEVANT | DUPLICATE, UNCERTAIN |
| Saved rule prediction | RISK, MATERIAL | UNCLASSIFIED | Missing/legacy prediction |

This measures the fixed phrase-triage rules against human adjudication. It does not train or tune a classifier. It does not convert UNKNOWN to CLEAR and is not a standalone production approval.

### How to inspect

From PowerShell after pulling/building/restarting the API:

```powershell
$base = "http://localhost:53360"
Invoke-RestMethod "$base/api/market-data/catalyst-reviews/validation" |
    ConvertTo-Json -Depth 12
```

A result of `INSUFFICIENT_LABELS` or `INSUFFICIENT_HOLDOUT_CLASS_COVERAGE` means the sample is not yet adequate for holdout precision/recall. Continue human review on both material and non-material articles. Do not manufacture labels to fill a class.

**Current validation result:** no runtime human-label sample has been inspected in this work session. Data not found in sources.

## 2. Opportunity Quality pool ranking

### What changed

- Added pure deterministic `OpportunityQualityPoolRanker`.
- Added `GET /api/market-data/opportunity-quality/rank?symbols=...`.
- Accepts 1–50 unique symbols and assesses every symbol with API concurrency capped at four.
- Returns all supplied candidates, including DATA? and BLOCKED.
- Orders by existing classification, then raw 5M RVOL, 30-bar 1M range/ATR and prior-high distance.
- Does not invent a weighted score or probability.

### Dashboard behavior

The main Red Zone heading now has **Rank quality pool · top 50**. It:
1. Uses the imported Thinkorswim scanner CSV rows.
2. Selects up to 50 symbols, ordered by the imported Overnight Score for this bounded prefilter.
3. Calls the API pool-ranking endpoint.
4. Displays rank, symbol, existing quality classification, RVOL, recent range/ATR, prior-high distance, 1M entry state and reasons.
5. Discloses how many imported symbols were included and that the ordering is diagnostic.

The Red Zone's four cards remain a separate monitor. The pool rank does not change the Red Zone's four-card selection, V2 permission, 1M execution, or order behavior.

### How to use the API directly

```powershell
$base = "http://localhost:53360"
Invoke-RestMethod "$base/api/market-data/opportunity-quality/rank?symbols=EVR,CBOE,RTO" |
    ConvertTo-Json -Depth 20
```

### Known limits

- The Dashboard ranks the top 50 imported scanner-score candidates, not every row if the imported CSV contains more than 50 symbols. This is an explicit bounded prefilter.
- The API is not market-wide discovery.
- Existing Opportunity Quality bands remain provisional.
- The raw metrics include research proxies (30 completed 1M bar range and prior-high distance).
- The endpoint is a ranking aid only; it cannot override hard eligibility or create an execution event.

**Still required:** historical time-separated candidate snapshots/outcomes, calibration, rank-stability analysis, and a decision on whether to page/expand beyond 50 candidates.

## 3. Runtime acceptance

Code-level tests cover the rank ordering and catalyst confusion-matrix calculation. GitHub Actions passed on code commit `5372a00cc02dbbb9ab9006d1d6abb17ad00a005e`: Release build, 0 warnings/errors, 243/243 tests. Dashboard syntax was included in the workflow. Later documentation/UI changes require checking the exact latest branch-head workflow.

Local API runtime, browser rendering, provider request behavior and human-label metrics must still be observed on the operator's machine.

## 4. [Uncertainties]

- Human review sample count and measured catalyst holdout precision/recall: Data not found in sources.
- Opportunity Quality ranking quality/profitability calibration: Data not found in sources.
- Actual imported scanner pool size at the time of runtime use: not captured in this work session.
- Dashboard click-through/runtime behavior: not yet operator-verified.
