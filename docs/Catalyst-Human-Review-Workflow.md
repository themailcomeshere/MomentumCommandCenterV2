# Human Catalyst Review Workflow

## Purpose

Collect auditable human labels for ticker-linked news articles so the catalyst triage rules can be evaluated against a labelled set. This is a data-collection workflow only. It does not train a classifier, modify `AlpacaCatalystAssessmentClient`, or emit `CLEAR` / `CLEAR_WITH_SOURCE`.

## Labels

- `MATERIAL_RISK`: article identifies a material adverse, legal, regulatory, dilution, solvency, listing, product or other downside risk.
- `MATERIAL_EVENT`: relevant material event (earnings, guidance, M&A, major contract, leadership, corporate action) without asserting that it is adverse.
- `NON_MATERIAL`: ticker-linked but not material to the overnight decision.
- `IRRELEVANT`: not actually relevant to the symbol/company.
- `DUPLICATE`: duplicate or substantially repeated coverage of another article.
- `UNCERTAIN`: insufficient information or ambiguous relevance.

Do not use a label of CLEAR. No human review label automatically clears a candidate.

## API

### Append a review

`POST /api/market-data/catalyst-reviews`

Example body:

```json
{
  "symbol": "EVER",
  "articleId": "provider-article-id",
  "headline": "Company reports quarterly results",
  "url": "https://example.com/news/article",
  "publishedAtUtc": "2026-11-02T20:05:00Z",
  "source": "Example News",
  "label": "MATERIAL_EVENT",
  "ruleSetVersion": "MCC-CATALYST-TRIAGE-1.0",
  "notes": "Q3 earnings released after the close."
}
```

The response includes a unique review ID and review timestamp. Records append to `%LOCALAPPDATA%/MomentumCommandCenterV2/catalyst-human-reviews.jsonl`, or the path specified by `MCC_CATALYST_REVIEW_PATH`.

### Read reviews

`GET /api/market-data/catalyst-reviews?symbol=EVER&limit=100`

Omit symbol to read the latest records across symbols. Limit is 1–500.

## Data quality and governance

- Submit one review per article/symbol pair; preserve the source article ID, headline, URL, source name and publication time.
- Review the actual article/filing, not only the headline. Notes must explain why the label was chosen; a source, ruleset version or rationale consisting of placeholder text is invalid.
- Use `UNCERTAIN` when the article cannot be verified. Do not infer a missing article or an empty feed as a clearance.
- Keep the automated assessment and human label as separate fields. The store is append-only; corrections should be submitted as a new review with an explanation, not by overwriting the old record.
- Labels are not yet adjudicated, de-duplicated, or used to compute precision/recall. A representative labelled set, double-review sampling, agreement measurement, and time-separated holdout validation are still required before classifier promotion.
- This API is local development functionality and currently has no multi-user authentication or reviewer identity attribution. Do not expose it publicly.

## [Uncertainties]

The existing Alpaca news endpoint returns ticker-linked articles, but article-level source completeness and historical lookback coverage have not been independently validated. Human reviewers must verify the original article and its timestamp.


## Dashboard workflow

1. Import the scanner CSV and select either MCC Top 4 or My Top 4 in the Overnight qualification panel.
2. Click Fetch Daily + catalyst for the active four.
3. For each returned article, open the source link and review the original article/filing.
4. Select one of the six allowed labels, add concise rationale in Notes, and click Save human review.
5. MCC appends the record to its local review store. The UI confirms the review ID/status; it does not change the automated catalyst assessment or candidate decision.

The production and prototype dashboard both expose this queue. Articles only appear after a successful catalyst fetch; no-news remains UNKNOWN and is not a labelable article.


## Resolving conflicting reviews (append-only correction)

The validation endpoint now returns a `conflictingArticles` array. Each entry includes the symbol/article identity, conflicting labels, and the review IDs, saved predictions, timestamps, headlines and URLs for the conflicting records.

After reviewing the original article/filing, submit one new adjudication. Preserve the triage prediction that was saved for the reviewed article, provide a rationale in `notes`, and supersede both conflicting review IDs:

```json
{
  "symbol": "SYMBOL_FROM_CONFLICT_DETAILS",
  "articleId": "ARTICLE_ID_FROM_CONFLICT_DETAILS",
  "headline": "Original article headline",
  "url": "https://publisher.example/original-article",
  "publishedAtUtc": "2026-11-06T20:00:00Z",
  "source": "Original publisher",
  "label": "MATERIAL_RISK",
  "ruleSetVersion": "MCC-CATALYST-TRIAGE-1.0",
  "notes": "Source-backed adjudication. Explain why this label supersedes the two prior reviews.",
  "predictedSeverity": "UNCLASSIFIED",
  "predictedCategories": [],
  "supersedesReviewIds": [
    "FIRST_CONFLICTING_REVIEW_ID",
    "SECOND_CONFLICTING_REVIEW_ID"
  ]
}
```

The values above are a schema example only; do not copy the placeholder values or choose a label/prediction without inspecting the actual conflict and original source. The API checks that each superseded ID exists and belongs to the same symbol/article. Supersession rationale must be meaningful and at least 12 characters. Literal placeholder values such as `string`, `test`, `todo`, `placeholder`, `n/a` and `na` are rejected in source, ruleset version, notes and prediction categories. Older records containing placeholder/missing evidence are excluded from validation and counted in `exclusionBreakdown.inadequateReviewEvidence`. Original records are retained in the append-only audit log; superseded records no longer participate in validation. Query `GET /api/market-data/catalyst-reviews/validation` again afterward.

The two non-evaluable reviews in the operator's latest response remain excluded unless their source material supports an independent new adjudication. Do not turn UNCERTAIN into a positive or negative label to increase sample size.
