# EVR 5-minute StrategyReport V2.2 review — 2026-11-05

**Source:** `data/ScannerExports/StrategyReports_EVR_11042026_5M_ATR_v2_2.csv`  
**Symbol/timeframe/version:** EVR / 5m / MCC_OHLCV_ATR V2.1 payload  
**Result:** Extended-hours coverage remains unproven; insufficient history for parity.

## Parsed source observations

| Field | Result |
|---|---:|
| Embedded payload observations | 82 |
| First source epoch | 2026-11-04 14:05:00Z |
| First source time | 09:05 ET / 07:05 Arizona |
| Last source epoch | 2026-11-04 21:00:00Z |
| Last source time | 16:00 ET / 14:00 Arizona |
| Intraday timestamp gaps | 1 |
| Gap | 09:15–09:30 ET (15 minutes) |
| Source bars strictly later than 16:00 ET | 0 |
| Source bars at 16:00 ET | 1 |

The StrategyReport also contains order-event Work Time / Date-Time fields. Those are not used as source-bar timestamps for coverage. The embedded epoch in the V2.1 payload is the timestamp used for this review.

## Findings

1. The new file adds one source observation at 16:00 ET relative to the earlier 81-observation V2.1 report.
2. It does not contain source bars after 16:00 ET. It therefore does not demonstrate consistent after-hours OHLCV coverage through the configured 19:59 ET window.
3. The 09:15–09:30 ET gap remains. The CSV cannot establish whether this is a missing trade/bar, chart-data omission, or study/export omission.
4. 82 observations are below the 119-observation parity window: 99 consecutive warm-up bars plus 20 comparison bars. This file cannot certify the 5m Wilder ATR parity window or 99-bar continuous warm-up.
5. No gap filling or interpolation was performed.

## Dashboard import issue

The checked-in `frontend/index.html` contains the five expected ATR metadata controls. The operator's running page nevertheless reports all five absent while the current app.js import handler is active. The exact cause of the runtime page/asset mismatch has not been proven.

Defensive remediation committed:
- `frontend/app.js` now recreates any missing metadata controls before registering the import handler.
- Defaults use the operator's confirmed V2.1 settings: ATR 14, Wilder, America/Phoenix, extended hours ON, corporate-action adjustment ON.
- Existing controls are preserved.
- `frontend/index.html` uses a new app.js cache key.
- API static-file middleware sends no-store/no-cache headers for `index.html` and `app.js` to prevent stale HTML/JavaScript generation mixing.

This remediation has passed CI syntax/build/tests but has not yet been verified in the operator's running browser/API instance.

## Required next evidence

1. Pull the branch and run `.scripts\Get-Latest-And-Test.ps1`.
2. Restart the API; hard-refresh Dashboard; confirm all five ATR controls render.
3. Import the Nov 3 1m and 5m V2.1 reports and capture full server responses.
4. Generate a new EVR 5m export. Inspect embedded source epochs, not StrategyReport Work Time. Confirm multiple bars after 16:00 ET, ideally through 19:55 ET.
5. Investigate the 09:15–09:30 ET gap without synthesizing bars.
6. Obtain at least 119 consecutive 5m observations with no intraday gaps for ATR parity evaluation.

## [Uncertainties]

- Why the runtime page omitted controls despite their presence in checked-in HTML remains unknown; no-store headers and runtime injection are defensive fixes, not a confirmed root-cause explanation.
- Whether Thinkorswim had actual eligible trades/bars after 16:00 ET on November 4 is not established by this export.
- The 15-minute gap's cause cannot be inferred from the StrategyReport alone.
