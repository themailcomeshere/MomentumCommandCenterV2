# Installed Thinkorswim 5M → 1M Validation — User Steps

**Purpose:** Validate whether the installed 1M study's reconstructed 5M Permission agrees with the actual installed 5M authority study, and determine whether the source differences matter on real bars. This is a targeted study-parity exercise, not a request to repeat the already completed OHLCV-export/DST validation.

## What I have already done

Repository source comparison is documented in docs/Validation/5M-to-1M-Handoff-Audit-20261022.md. It found concrete differences in raw Permission, structural damage, and major-failure predicates. That comparison does not establish which code is installed in Thinkorswim or how those predicates behave on your chart. I cannot access your Thinkorswim installation or chart state remotely.

## What I need from you

Please provide the following evidence for one or two examples where the 5M and 1M outputs can be compared at the same symbol/time.

### Step 1 — Identify the exact installed study versions

For the installed 5M authority study:
1. Open a 5-minute chart for the symbol used in your example.
2. Open **Studies → Edit Studies**.
3. Select the exact 5M study instance, open its source/edit view, and copy the complete study source into a local text file. Do not edit the source.
4. Save as `Installed-5M-Study.txt`.
5. Note the study title shown in the editor and whether the Authority Diagnostic V2 is added separately.

For the installed 1M execution study:
1. Open a 1-minute chart for the same symbol.
2. Repeat the process for the exact installed 1M study.
3. Save the complete source as `Installed-1M-Study.txt`.
4. Note any study inputs changed from their defaults. If easier, screenshots of the input panels are acceptable, but the source text is strongly preferred.

Do not include account identifiers or other private information. The study source itself is what is needed.

### Step 2 — Pick a matched chart example

Choose one historical interval that shows an important state transition. Best first example:
- a time where the 5M Permission changed from YES to NO or NO to YES; or
- a 1M READY / candidate bubble appeared while 5M Permission or a blocker was visible; or
- an extension-danger block occurred while the 5M bullish thesis remained intact.

Use the same symbol and date on both charts. Set chart aggregation to 5m for the authority chart and 1m for the execution chart. Ensure both charts show the same time zone/session configuration and that the relevant candles are visible.

### Step 3 — Capture the 5M authority evidence

On the 5M chart:
1. Add the normal 5M study and, if available, Authority Diagnostic V2.
2. Turn on only the diagnostics needed to show the relevant historical bars. Avoid cluttering the whole chart.
3. Include the candles before, at, and after the state transition.
4. Take a screenshot that shows the symbol, aggregation, date/time axis, Permission/bubble, and diagnostic blocker code/text.
5. If the chart's right-edge labels show only the latest state, use per-bar bubbles or inspect the historical bar; do not treat the current right-edge label as a historical value.

Save as `5M-example-1.png`.

### Step 4 — Capture the 1M execution evidence

On the 1M chart for the same symbol and interval:
1. Add the exact installed 1M study.
2. Show the reconstructed 5M Permission / reconstructed blocker diagnostic if the study exposes them.
3. Show 1M candidate and READY diagnostics, including extension warning/danger if relevant.
4. Capture enough bars to identify the corresponding 5M candle boundaries and the 1M event time.
5. Save as `1M-example-1.png`.

If the diagnostic labels are not visible in the installed study, do not modify the production study just to create them. Send the existing source and screenshots first; I can then prepare a diagnostic-only patch for your review.

### Step 5 — Send the event facts

Alongside the images, send this small record (copy and fill what you know):

| Field | Value |
|---|---|
| Symbol | |
| Date | |
| 5M chart time shown at transition | |
| 1M chart time shown at event | |
| 5M Permission before → after | |
| 5M diagnostic blocker before → after | |
| 1M reconstructed 5M Permission / blocker | |
| 1M state (candidate / READY / blocked) | |
| Extension state, if relevant | |
| Regular or extended-hours session | |
| Installed 5M study title/version | |
| Installed 1M study title/version | |

Use chart-displayed times and identify the chart time zone. Arizona time is preferred for the report; Eastern time is useful for comparing provider/session data. Do not infer missing values.

## What I will do with the evidence

1. Compare installed source with repository source and identify exact version drift.
2. Align the 5M and 1M bars by timestamp and 5M candle boundaries.
3. Compare the actual 5M Permission/blocker with the 1M reconstruction on the same interval.
4. Determine whether the mismatch is expected from different predicates, an aggregation/initialization issue, or a genuine unintended gate interaction.
5. Recommend a minimal diagnostic or code change only when evidence supports it. I will not alter trading thresholds or weaken the 5M authority gate based on screenshots alone.

## Minimum acceptable first submission

If copying the study source is cumbersome, send the two chart screenshots for one specific transition plus the study titles and input settings. That is enough to start a targeted comparison, but not enough to certify source parity.

## [Uncertainties]

- Thinkorswim can display different labels depending on aggregation, session settings, chart timezone, and study inputs. These must be recorded before comparing timestamps.
- A screenshot can establish displayed states but cannot prove the complete installed source matches the repository. Full source text is the stronger evidence.
