# Runner Management and Exit-State Validation — 2026-10-29

**Status:** Screenshot review completed; matched-bar replay validation remains open.  
**Sources already in GitHub:** `docs/Validation/Installed-5M-Study.txt`, `docs/Validation/Installed-1M-Study.txt`, `docs/Validation/Installed-5M-Authority-Diagnostic.txt`, `docs/Charts/5M/MainStudy.txt`, `docs/Charts/1M/MainStudy.txt`.  
**Existing chart review:** `docs/Validation/RTO-AMT-Chart-Review-20261028.md`.

## What was already done on October 28

The RTO and AMT screenshots were reviewed and their state semantics recorded. This work should not be repeated as a generic screenshot request.

### RTO — screenshot 2026-10-28 160419

- 5M pane displays `RUNNER RISING`, `5M PERMISSION: YES`, `5M STRUCTURE: RUNNER`, `BULLISH REGIME: ESTABLISHED`, `RUNNER MOMENTUM: RISING`, and `EXTENSION: 0.49 ATR`.
- 1M pane displays `1M STATE: GET OUT` and `EOD: GET OUT`; its status strip also shows `5M PERMISSION: NO` and `5M STRUCTURE: TRANSITIONING`.
- This is evidence that runner/exit states were reviewed. The two panes' selected bars are not proven to be the same timestamp, so it is not evidence of a same-bar contradiction.

### AMT — screenshot 2026-10-28 152731

- 5M pane displays `RUNNER RISING`, `5M PERMISSION: YES`, `5M STRUCTURE: BULLISH`, `BULLISH REGIME: ESTABLISHED`, and `EXTENSION: 2.03 ATR`.
- 1M pane displays `1M STATE: GET OUT` and `EOD: GET OUT`; its strip shows `5M PERMISSION: NO`.
- The chart review correctly treated the high extension and GET OUT as reasons not to open a new Boring Overnight position. It did not claim the 5M and 1M labels were aligned to the same bar.

### RTO — screenshot 2026-10-27 152146

- 5M pane shows `PREPARE TO BUY`, `5M PERMISSION: YES`, and bullish structure.
- 1M pane shows `GET OUT` / EOD exit context and a short-lived `EXECUTION EXPIRED` event near the right edge.
- This is a useful candidate/expiry/exit example, but the screenshot alone does not provide a complete timestamped state series.

## What exists in code versus what does not

- `MarketReplayEngine` already creates one frame per 1M bar and attaches only the latest 5M bar whose timestamp is not later than that 1M frame. Existing tests cover no-look-ahead, deterministic ordering, duplicate keys, and invalid OHLC.
- It does **not** calculate ThinkScript Permission, RUNNER, RUNNER WANING, SWING, GET OUT, continuation, or READY states.
- The screenshot review established state semantics and examples; it did not produce a machine-readable, per-bar state tape. Therefore no truthful replay test for the ThinkScript state transitions can be generated from those screenshots alone.

## Source version comparison that must be resolved

- The saved installed 5M source in `docs/Validation/Installed-5M-Study.txt` is V4.1.7. The current repository source in `docs/Charts/5M/MainStudy.txt` is also V4.1.7, but the displayed end-of-session label differs: installed copy says `EOD: GET OUT / ACTIVE`; repository copy says `ENTRY CUTOFF: ACTIVE / INACTIVE`. The 5M authority/runner calculations are otherwise the same in the compared source text; preserve the label distinction when reading captures.
- The saved installed 1M source in `docs/Validation/Installed-1M-Study.txt` is V14.4.10. The current repository 1M source in `docs/Charts/1M/MainStudy.txt` is V14.4.11. V14.4.11 adds the Arizona 12:30–12:55 time-gated Boring Overnight decision event and explicit `DATA? — NO HOLD` / `EOD GET OUT` outputs when external qualification data is unavailable. Do not assume V14.4.11 is installed based only on the repository file.
- The October 28 screenshot title text appears to identify a later 1M study label than the saved V14.4.10 copy. The exact full installed source corresponding to that screenshot is not established by the screenshot. Data not found in sources for an exact byte-for-byte installed V14.5 source file.

## Exact procedure to close matched-bar replay

This is the remaining validation, not a request to repeat the October 28 visual review.

1. In Thinkorswim, open RTO (first example) on a 5-minute chart and a 1-minute chart for **2026-10-28**. Keep both charts on the same symbol, date, session setting, and Arizona timezone.
2. On the 5-minute chart, keep the normal 5M Command Center active and add the 5M Authority Diagnostic V2 companion. Enable its per-bar historical/debug bubbles. Do not replace the normal study with the diagnostic.
3. On the 1-minute chart, use the exact installed 1M study and enable its historical/debug event bubbles. Do not alter trading thresholds or input overrides during this capture.
4. Zoom to the RTO period where the 5M pane showed RUNNER RISING / Permission YES while the 1M pane showed GET OUT / Permission NO. Capture the 5M and 1M charts with the same Arizona time axis visible, including at least 3 bars before and 3 bars after the transition.
5. Record the timestamp of the **5M candle close** and the 1M event timestamp. Record 5M Permission, blocker code, runner state, waning state, swing state, 1M reconstructed 5M Permission/blocker, candidate state, READY/expired state, GET OUT/EOD, and extension danger. Use UNKNOWN for anything not visible.
6. Repeat for AMT around its extension-danger / GET OUT example, then one RTO example around PREPARE TO BUY → EXECUTION EXPIRED.
7. Export the underlying 1M and 5M OHLCV for those exact windows from the same provider/feed/session settings. Keep original StrategyReport exports and chart screenshots unchanged. Include the timezone and whether the provider timestamp represents bar start or close.
8. Add the resulting per-bar state tape as a versioned CSV fixture under `data/validation/runner-exit/`. Only after that tape exists should we write a deterministic replay test asserting the observed transitions. The fixture must not infer unobserved intermediate states from candle colors or line slopes.

## Acceptance criteria

- [ ] Exact installed 5M and 1M sources/versions used for the October 28 captures are recorded.
- [ ] 5M and 1M bars are aligned by timestamp and 5M candle boundaries.
- [ ] The state tape includes RUNNER, RUNNER WANING, SWING, failure/GET OUT, candidate, READY, expiry, and EOD events where observed.
- [ ] No-look-ahead replay attaches only a 5M bar available at each 1M decision timestamp.
- [ ] Tests assert only transitions directly supported by the state tape.
- [ ] Any intentional difference between 5M authority and 1M reconstructed permission is documented; no gate is weakened without evidence.

## [Uncertainties]

- The screenshot panes show different selected bars/right-edge states; exact same-bar alignment is not established.
- The installed 1M source file currently saved in GitHub is V14.4.10, while the repository source is V14.4.11. The exact V14.5 source suggested by the screenshot label is not present in the reviewed files.
- The screenshot evidence is valid for visual state review but insufficient to reconstruct every intermediate state transition or a complete machine-readable replay tape.


## 2026-10-29 — Additional RTO capture: distinct 5M/1M status

The user's latest screenshot is separate evidence from the previously reviewed Oct 28 RTO screenshot.

- 5M status strip: `5M STATE: GET OUT`; structure shown as bullish; permission shown NO at the selected cursor state. The diagnostic bubbles show several historical permission/blocker transitions, later green permission/runner events, and a later 5M GET OUT state.
- 1M status strip: `1M STATE: EXECUTION EXPIRED`; it does not display `1M STATE: GET OUT` or `EOD: GET OUT` in this capture.
- Therefore, the latest evidence corrects the earlier generalized assumption that the 1M pane had displayed GET OUT/EOD GET OUT for this RTO sequence. The current capture instead documents an expired 1M execution event alongside a 5M GET OUT status.
- The selected bar timestamps in the panes are not demonstrated to be identical by the screenshot. This is not yet a parity failure or a code defect.
- User says 1M and 5M OHLCV exports have been committed under `data/validation/RTO`. Exact filenames and file rows were not returned by the attempted path reads: Data not found in sources. Do not guess filenames or assert the exports were analyzed.

### Required replay procedure
1. Establish exact committed filenames and inspect both files.
2. Validate columns, symbol, timezone, candle timestamp convention, first/last timestamp, row count, duplicate timestamps, OHLC validity, and missing intervals separately for 1M and 5M.
3. Convert to a common Arizona-local timeline only after determining whether each timestamp marks candle start or close.
4. Match each 1M bar to the latest completed 5M bar, never a future/incomplete 5M candle.
5. Build a row-per-minute state tape with the 5M permission/structure/blocker and 1M execution candidate/expiry/GET OUT/EOD fields sourced from actual chart diagnostics.
6. Add replay assertions only for transitions present in the matched evidence. Keep 5M authority, 1M entry expiry, and 1M exit state distinct.
