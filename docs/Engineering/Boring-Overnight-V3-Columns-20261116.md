# Boring Overnight V3 — Thinkorswim columns (MCC-V3-BORING-0.1)

**Created:** 2026-11-16. **Status:** source written and unit-tested in C#. **Not yet compiled in Thinkorswim**, and parity with the C# reference is not yet evidenced (operator steps below).
**Files:** [`docs/Column Formulas/V3/`](../Column%20Formulas/V3/): `V3 Edge.txt`, `V3 Boringness.txt`, `V3 Status.txt`.
**C# reference (authoritative):** `BoringOvernightV3Calculator` (`src/MomentumCommandCenterV2.API/BoringOvernight/`), 54 unit tests (synthetic fixtures).
**MCC parity endpoint:** `GET /api/boring-overnight/v3-column?symbol=RTO`.
**Spec sources:**
- [V3 Decision Column TODO](../project-control/Trading_System/MomentumCommandCenterV3_Boring_Overnight_Decision_Column_TODO.md)
- [Column Data Contract v1](../project-control/Boring_Overnight_Column_Data_Contract_v1.md) (§3 gates, §10A point maps)
- [READY semantics](Boring-Overnight-READY-Semantics-20261114.md)

## What the three columns show

| Column | Sortable value | Text | Purpose |
|---|---|---|---|
| **V3 Edge** | Overnight Edge Score: your installed **Overnight Score** formula, unchanged | `EDGE OK`, or the first failing edge gate: `WIN<60`, `AVG<0.10`, `5D<=20D` | Historical ability to produce positive overnight returns |
| **V3 Boringness** | Boringness Score 0–100 | Score plus band: `PERFECT` (90+), `VERY GOOD` (80+), `POSSIBLE` (70+), `AGGRESSIVE` (60+), `REJECT` | How controlled and repeatable the overnight profile is. Kept separate from Edge, as the spec requires |
| **V3 Status** | 3 CANDIDATE, 2 WATCH, 1 BLOCKED, 0 DATA? | Status, primary reason, `+n` for further reasons | Triage before MCC's READY check |
| **V3 Rank** (added 2026-11-16) | Status × 100 + Boringness | none (plain number) | **Sort key** for the scan's "Sorted by" list, which greys out the labelled columns. Descending = CANDIDATEs first by Boringness |

- **V3 Status never shows READY.** `CANDIDATE` means every gate Thinkorswim can compute has passed.
- The symbol still needs MCC's **READY check** (BORING OVERNIGHT workspace, step 4). That check covers catalyst sign-off, the exclusion list, data freshness, 5M permission and the buy window.
- Nothing here is an order instruction.

### Definitions (identical in ThinkScript and C#)

- **Bars:** daily, regular session, split-adjusted. The latest 21 bars are used (20 overnight returns); fewer gives `DATA?`.
- **Overnight return:** `r = open / close[1] − 1` (the bar's open vs the previous close), as in your installed columns.
- **Edge metrics:**
  - win rate = share of the last 20 `r` > 0;
  - mean (20 and 5) and median (20) of `r`.
- **Edge gates:** win rate ≥ 60%, 20D mean ≥ +0.10%, and 5D mean strictly above the 20D mean.
- **Other hard gates:**
  - close at least $5.00 (exactly $5.00 passes; operator decision 2026-11-16, same in MCC's evaluator);
  - average volume of the **prior** 20 sessions ≥ 500,000;
  - close above the 20-session SMA.
- **Boringness points** (`BORING-0.1-PROVISIONAL`, contract §10A), interpolated linearly between anchors:

| Component | Max | Anchors |
|---|---:|---|
| Win rate | 25 | 60% = 0 · 70% = 10 · 80% = 18 · 90%+ = 25 |
| Median ON | 20 | ≤ 0% = 0 · 0.10% = 8 · 0.25% = 14 · 0.50%+ = 20 |
| 20D mean ON | 15 | < 0.10% = 0 · 0.10% = 5 · 0.25% = 10 · 0.50% = 15 · 1.00% = 10 · 1.50% = 5 · 2.00%+ = 0 |
| 5D − 20D (pp) | 10 | ≤ 0 = 0 · just above 0 = 5 · 0.10 = 7 · 0.50 = 10 · 1.00 = 7 · 3.00 = 3 · 5.00+ = 0 |
| Consistency (share of 20 returns in [0%, +2%]) | 10 | 60% = 0 · 70% = 5 · 80% = 8 · 90%+ = 10 |
| Volatility (population std dev of 20 returns) | 10 | ≤ 0.25% = 10 · 0.50% = 8 · 1.00% = 5 · 1.50% = 2 · 2.00%+ = 0 |
| Average volume (prior 20) | 5 | < 500k = 0 · 500k = 1 · 750k = 2 · 1M = 3 · 2M+ = 5 |
| Price | 5 | $5 = 0 · $10 = 1 · $25 = 3 · $50+ = 5 |

- **WATCH cautions:**
  - today's overnight return ≥ 3%;
  - 5D mean ≥ 5%;
  - 5D − 20D ≥ 3 pp;
  - today's range / prior close > 1.5 × ATR% (ATR = simple mean of the prior 14 true ranges / prior close);
  - today's volume ≥ 3 × the prior 20-session average;
  - Boringness < 60.
- **Status order:** DATA? → BLOCKED (any hard gate) → WATCH (any caution) → CANDIDATE. MCC lists all reasons; the column shows the first plus a count.

## Install in Thinkorswim (operator)

1. In a watchlist or the scan-results header, right-click a column header → **Customize…**.
2. Pick an unused **Custom** column and click its script icon.
3. Name it **V3 Edge** and paste the whole of `docs/Column Formulas/V3/V3 Edge.txt`.
4. Set **Aggregation: D**. Leave **Include extended hours trading session** unticked.
5. Click **OK** and confirm there's no compile error. If there is one, copy the exact message for Claude.
6. Repeat for **V3 Boringness** and **V3 Status**, then add the three columns to the view.
7. Record for each column: compiled yes/no and the Thinkorswim version. This is the **TOS compile evidence** item in the To-Do.

## Parity check with MCC (operator)

**When:** after the regular close and MCC's 16-minute SIP delay, i.e. after **14:16 AZ in standard time** (13:16 AZ in daylight time). By then both sides use the same completed day. During the session Thinkorswim includes today's forming bar and MCC doesn't, so values differ by design.

**Steps:**
1. Start MCC.
2. For each of five symbols from your Overnight list, open `http://localhost:53360/api/boring-overnight/v3-column?symbol=SYMBOL`.
3. Compare with the three Thinkorswim columns:
   - `calculation.overnightEdgeScore` ↔ V3 Edge;
   - `calculation.boringnessScore` and `boringnessBand` ↔ V3 Boringness;
   - `calculation.status` and `primaryReason` ↔ V3 Status.
4. Report symbol, time, the three Thinkorswim values and the MCC values.

**Read the result:**
- **Match:** equal at 2 decimals, and the same status and primary reason.
- **Small differences** usually come from the source, not the formulas:
  - Thinkorswim adjusting for dividends (MCC uses split-only);
  - a different daily open than SIP;
  - values sitting exactly on a gate threshold, where floating-point rounding can flip the result.
- `parityComparable: false` means MCC returned DATA? (a missing session or a non-SIP feed); don't compare that symbol.

## Known limits and open decisions

- **Provisional numbers.** The point maps and bands come from the 2026-10-23 contract and aren't calibrated. Freeze them only after labelled historical cases (contract §12.2).
- **My interpretation, to be confirmed by you:** the contract's "≥ +3.00 pp = 0–3 points" for the 5D component is implemented as 3 at +3.00 pp, falling to 0 at +5.00 pp.
- **Price gate (decided 2026-11-16):** exactly $5.00 passes, only prices below $5 block. Applied to the V3 columns, the C# reference, MCC's READY evaluator and the Dashboard Daily gate.
- **Watchlist sorting with labels:** whether Thinkorswim sorts by the plot value or the label text is Data not found in sources. Check by sorting the column.
- **Thinkorswim functions used:** `Median`, `TrueRange`, `Max`/`Min`, `Sqrt`, `AsText` with `NumberFormat.TWO_DECIMAL_PLACES` and `AssignBackgroundColor`.
- **Compile history (operator, 2026-11-16):**
  - The first V3 Boringness and V3 Status failed with `TooComplexException`, because about 30 calls to a `Lerp` helper script were each inlined.
  - Both were rewritten with clamped segment sums (no helper, no if-chains). The values are identical: checked against the anchor maps on a 20,001-point grid per component, maximum difference 1.8e-15.
  - V3 Edge was not reported as failing.
  - Rule added: `docs/rules/thinkscript.md` §8.
- **Not in Thinkorswim:**
  - catalyst/news, the exclusion list, data freshness, 5M permission and the buy window (MCC READY check);
  - the GTC_EXT sell-limit history (MCC's sell-limit gate).
- **Optional companion chart study** (contract §10): not built. The columns plus MCC's READY panel cover the explanation. Build it only if you find you need it.
