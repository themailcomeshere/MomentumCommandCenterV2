# ATR and catalyst validation — operator evidence update — 2026-11-07

**Repository:** `themailcomeshere/MomentumCommandCenterV2`  
**Working branch:** `feature/opportunity-quality-classification`  
**Source head reviewed:** `b212caf0c2d1df0393fdcc05b5f872c0966aeb67`  
**Dashboard:** `frontend/index.html`  
**Safety:** decision-support-only; order submission is NOT IMPLEMENTED.

## 1. Latest local ATR parity recheck

The operator queried the local API with `warmupBars=99&windowBars=119` and supplied these results:

| Symbol | Timeframe | Status | Persisted observations | Compared bars | Timestamp gaps |
|---|---|---|---:|---:|---:|
| EVR | 5m | `GAPS_PRESENT` | 327 | 20 | 2 |
| CBOE | 5m | `GAPS_PRESENT` | 338 | 20 | 1 |

### EVR — November 5, 2026

| Previous observed UTC | Next observed UTC | ET interval | Missing 5m slots inside 04:00–20:00 ET |
|---|---|---|---:|
| 05:10Z | 14:05Z | 00:10 → 09:05 EST | 61 (04:00–09:00 ET) |
| 14:05Z | 14:30Z | 09:05 → 09:30 EST | 4 (09:10, 09:15, 09:20, 09:25 ET) |

The committed EVR Strategy Report export has its first November 5 payload at 14:05Z (07:05 Arizona / 09:05 ET). The export therefore contains no payload rows for the earlier expected slots in the first interval.

### CBOE — November 4, 2026

| Previous observed UTC | Next observed UTC | ET interval | Missing 5m slots inside 04:00–20:00 ET |
|---|---|---|---:|
| 21:00Z | 23:40Z | 16:00 → 18:40 EST | 31 (16:05–18:35 ET) |

The committed CBOE export contains the 21:00Z row followed by the 23:40Z row for this interval.

### Operator's chart observation — not yet time-aligned

The operator reports seeing chart bars beginning at **05:21 for EVR** and **06:15 for CBOE**. These observations are recorded, but the message does not establish AM/PM, chart date for each time, or whether the displayed axis is America/Phoenix or another timezone. Do not convert these times or claim they overlap a specific gap until those attributes are confirmed.

This is the key distinction:
- `GAPS_PRESENT` means the persisted Strategy Report observations have timestamp discontinuities in the declared chart-session window.
- The detector counts scheduled 5-minute timestamp slots; it cannot prove a trade occurred or a candle was rendered in Thinkorswim.
- A visible TOS candle inside a missing export interval would establish an export omission.
- No TOS candle in the interval may indicate a legitimate no-print/no-bar period; that must be recorded as source behavior, not filled with synthetic bars.

**No bar has been synthesized, no tolerance has been relaxed, and no unchanged export was re-imported.**

## 2. Required ATR resolution

1. Open the exact EVR 5m chart for **2026-11-05** and the CBOE 5m chart for **2026-11-04**. Confirm chart timezone is America/Phoenix, extended hours is ON, corporate-action adjustment is ON, aggregation is 5m, and the compiled study version is the corrected V2.1 payload study.
2. For EVR, inspect the 04:00–09:00 ET interval and the 09:10–09:25 ET interval. Record the exact first and last candle timestamps actually visible, plus whether the reported 05:21 time is AM/PM and the date.
3. For CBOE, inspect 16:05–18:35 ET on November 4. Record whether any 5m candles are actually visible and their exact first/last timestamps. Confirm whether 06:15 refers to that chart date/time or a different session.
4. Use the chart's actual timestamp axis/crosshair; do not infer candle timestamps from the left edge of a compressed chart.
5. If a candle is visible in TOS but absent from the committed export, compile the corrected study, create a genuinely new export for the affected symbol/date, inspect its embedded `MCC_OHLCV_ATR` epochs before import, and import only if it adds/corrects actual observations.
6. If TOS itself has no candle, retain the raw observation and document the no-bar behavior. Do not insert zero-volume or interpolated bars. Then decide whether the parity contract should distinguish an absent chart candle from an omitted export row before changing the detector's fail-closed policy.
7. Re-run the same persisted parity endpoint after any genuinely new import. Numeric parity and timestamp continuity remain separate acceptance conditions.

## 3. Latest Catalyst Human Review validation

The operator supplied the current endpoint response:

- `status=INSUFFICIENT_LABELS`
- `rawReviewCount=0`
- `includedUniqueReviewCount=0`
- `excludedReviewCount=0`
- `conflictingArticleCount=0`
- `trainingCount=0`
- `holdoutCount=0`
- Training and holdout confusion matrices are empty; precision, recall, specificity and accuracy are null.

**Interpretation:** the current local review store contains zero records. This is not a measured classifier failure and is not a validation pass. There are no human labels to compare with saved triage predictions. Metrics cannot be computed.

### What is already implemented

- Append-only human review storage.
- The Dashboard review submission captures the triage severity/categories that existed at review time.
- `GET /api/market-data/catalyst-reviews/validation` deduplicates by symbol/article identity, excludes uncertain/duplicate/legacy-predictionless/conflicting records, and creates a chronological later-20% holdout.
- Positive human truth: `MATERIAL_RISK`, `MATERIAL_EVENT`.
- Negative human truth: `NON_MATERIAL`, `IRRELEVANT`.
- Excluded labels: `DUPLICATE`, `UNCERTAIN`.
- Positive saved prediction: `RISK` or `MATERIAL`; `UNCLASSIFIED` is negative.
- The workflow does not train a classifier and never converts an unknown/no-news state to CLEAR.

### Required data collection

1. In the main Dashboard, import/select the current scanner list and open the Overnight Qualification review area.
2. Click **Fetch Daily + catalyst** for the active review group.
3. Open each returned source article/filing; review the source, not just the headline.
4. Save one human label per symbol/article pair with a short rationale. Use `UNCERTAIN` when the source is ambiguous or cannot be verified; do not force a positive/negative label.
5. Include both material and non-material cases. A queue consisting only of obvious catalyst hits is class-imbalanced and cannot establish specificity.
6. Re-query the validation endpoint after each review batch. Preserve the raw JSON.
7. Continue until the later chronological holdout contains at least one positive and one negative truth class. That is only the minimum structural gate; a useful evaluation needs a substantially larger, representative sample, difficult/ambiguous cases, and double-review agreement checks.
8. Do not relabel or fabricate old articles to populate the holdout. Corrections are new append-only review records with an explanation.

## 4. CI and source status

The exact reviewed feature-branch head `b212caf0c2d1df0393fdcc05b5f872c0966aeb67` has a successful GitHub Actions .NET build/test run:
- Run: [42340443270](https://github.com/themailcomeshere/MomentumCommandCenterV2/actions/runs/42340443270)
- Conclusion: success.
- This verifies repository build/tests only. It does not compile ThinkScript, prove TOS chart candle presence, run the operator's local API, or create human labels.

## 5. [Uncertainties]

- The chart observations “EVR 05:21” and “CBOE 06:15” are not fully anchored to AM/PM, exact chart date, or displayed timezone. Data not found in sources for those attributes.
- The source data cannot establish whether the missing slots contained actual TOS candles or no eligible prints.
- The current local catalyst-review store has zero rows; no article-level human adjudications were supplied.
- The operator's latest pasted ATR output did not include current MAE/max-error values, so this note does not restate numeric error metrics.


## Operator recheck — 2026-11-08 UTC (local evening Nov 7)

### Local runtime identity

- Operator independently verified local source SHA: `bb6a04aedc6f3cbfda1250a7cc9d8d676594419e`.
- API health returned `OK`, mode `decision-support-only`, `orderSubmission=NOT IMPLEMENTED`, timestamp `2026-11-08T03:53:22.1809216Z`.

### New inspect CSV imports — important coverage finding

Both files parsed as V2.1, ATR14 Wilder, America/Phoenix, extended hours ON and corporate-action adjustment ON. Both imports were idempotent (zero appended, all rows identical).

| File | Payload rows | First bar UTC / Eastern | Last bar UTC / Eastern | Interior gap count | Appended |
|---|---:|---|---|---:|---:|
| CBOE Nov 4 inspect | 82 | 06:10Z / Nov 4 01:10 ET | 20:55Z / Nov 4 15:55 ET | 2 | 0 |
| EVR Nov 5 inspect | 78 | 14:30Z / Nov 5 09:30 ET | 20:55Z / Nov 5 15:55 ET | 0 | 0 |

**These files did not cover the intervals the operator was asked to inspect:**
- EVR begins at 09:30 ET, after both requested windows (04:00–09:00 and 09:10–09:25 ET). Its `gapCount=0` only means there were no interior discontinuities among the 78 included rows; it says nothing about the omitted leading interval.
- CBOE ends at 15:55 ET, before the requested 16:05–18:35 ET interval. Its final requested window is absent from this export. Its 2 interior gaps are at 00:40 ET → 07:00 ET and 07:05 ET → 07:25 ET, not the requested after-hours window.
- Both reports start/end within a partial chart-session range. The API importer previously reported only gaps between rows inside the file and did not explicitly flag leading/trailing 04:00–20:00 ET coverage. This is a diagnostics defect, not evidence that the source chart had bars during every scheduled slot.

### Catalyst review count

The new validation response is:
- raw reviews 3
- included unique reviews 1
- excluded reviews 2
- conflicts 0
- training/holdout 0/0

The operator confirms clicking Save for each of three article reviews. Therefore, save success and evaluation eligibility are separate: two saved records were excluded. The prior validation payload does not expose why. The latest source change adds a mutually exclusive exclusion breakdown: missing saved prediction, non-evaluable label (DUPLICATE/UNCERTAIN or unsupported label), duplicate same-label article review, and conflicting-article records. The Dashboard will display those reasons after the new build is deployed. The exact reason counts for the operator's existing three records are **Data not found in sources** until the operator re-queries the endpoint with the new build.

## Engineering changes committed after the operator's local SHA

The following commits are on `feature/opportunity-quality-classification` and must be pulled before their diagnostics are available locally:

- `f646c915dc0707e8f9eaa96210cf1aa8f129ff1f` — catalyst validation now returns an exclusion-reason breakdown.
- `ba9df90f54d5d9bef8dec40edc59a86363ebc985` — ATR import parser now reports leading/trailing missing scheduled slots per observed Eastern session date, separately from interior gaps.
- `85bee1c45027d39ec65b43168c43dbe485f7fb16` and `cbd299750dba1f8130e4e4bfe2d862005abe8cee` — regression coverage for partial ATR export ranges and Eastern/UTC conversion.
- `72a8cafd04df931d72726c628f38faaed3f9fd77` — regression coverage for catalyst exclusion diagnostics.
- `65ba3b8e42704d722d29b10265b5f8d4606e9d55` — Dashboard displays exclusion reasons after validation refresh.

GitHub Actions for parser, coverage tests and catalyst validation tests have passed on commits through `72a8cafd`; the latest Dashboard commit workflow was still in progress when this note was prepared. Do not claim full latest-head CI success until it completes.

### Next operator evidence required

1. Pull the latest feature branch and build/test.
2. Re-import is NOT needed for the two inspect CSVs; they added no data.
3. Re-query the ATR import/parity endpoint. Inspect the new per-session coverage array. Expect EVR Nov 5 to report leading and trailing partial-session coverage; CBOE Nov 4 to report trailing coverage after 15:55 ET. These are scheduled-slot coverage warnings, not proof of actual candles.
4. Re-export from TOS with an explicit range that includes EVR 04:00–09:25 ET and CBOE 16:00–18:40 ET on the target dates. Check first/last payload timestamps before importing. The export must actually span the requested interval.
5. Re-query catalyst validation. Copy the new `exclusionBreakdown` object and the three saved-review records from `GET /api/market-data/catalyst-reviews?limit=20` (redact article URLs if needed, but retain symbol, articleId, label, predictedSeverity, reviewedAtUtc). This identifies why two reviews were excluded without guessing.
