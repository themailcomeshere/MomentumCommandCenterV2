# Overnight history — first findings (2026-11-16)

**Source:** the operator's run of `scripts/Export-OvernightHistory.ps1` on 2026-11-16 (MCC `64981f8`).
- Input: the 50 symbols of the 14:28 AZ Overnight scanner export.
- Data: about 2 years of completed Daily bars on delayed SIP, split-adjusted; 23,651 overnight pairs.
- Raw data: `D:\source\repos\MCC-Data\overnight-history\20261116-222904\` (outside the repo). Only aggregate statistics are recorded here.

**Definitions:**
- **Overnight return** = next regular-session open / buy-session close − 1, the same definition as your TOS columns.
- **Signal test:** on each past day the trailing 20 returns (ending with that day's opening gap) met the rule, and the outcome is that night's return.
- Only contiguous sessions are used. Analysis by Claude, Python, 2026-11-16.

**RESEARCH ONLY:** not READY, not a trade instruction.

## 1. Do the scanner rules predict the next night?

| Condition on the buy day | Nights | Next-night win % | Mean | Median | t |
|---|---:|---:|---:|---:|---:|
| Every night (base rate) | 21,731 | 49.7% | +0.026% | 0.000% | 2.61 |
| Edge gates: win ≥ 60%, 20D avg ≥ +0.10%, 5D > 20D | 1,899 | **53.4%** | **+0.107%** | +0.067% | 2.64 |
| Edge + median ≥ average | 910 | 52.3% | +0.078% | +0.054% | 1.33 |
| Edge + above SMA20 | 1,459 | 53.1% | +0.098% | +0.065% | 2.31 |
| MCC shortlist rules (edge + median ≥ avg + SMA20 + current gap < 3%) | 621 | 52.5% | +0.091% | +0.037% | 1.29 |

**Reading:**
- The rules add a **small** edge: about +3–4 points of win rate, and a mean about 4× the base rate.
- They do **not** deliver the 60–80% win rates the scanner shows. Those come from the last 20 nights, chosen because they looked good.
- Across the 50 symbols, the **median 2-year win rate is 49.6%**, while their last-20-night win rates were 60–80%.
- Positive 2-year mean overnight return: 38 of 50 symbols.

**Caveats:**
- **In-sample bias:** the 50 symbols were picked by today's scan, which likely inflates these results. A fair test needs daily scanner snapshots collected going forward (the export script now archives them).
- **No costs:** spreads and fees are not included.
- **Close-to-open only:** this measures the next open. Your exit is a GTC_EXT limit touched during Day 2 (07:00–20:00 ET), measured separately by MCC's sell-window history and walk-forward gates. A +0.5% touch is a different and generally easier target than a +0.5% open.
- **Dividends:** split-only adjustment counts ex-dividend gaps as overnight losses, as your TOS columns do.

## 2. Weekday effects ("on this weekday this stock usually rises")

| Buy weekday | Nights | Win % | Mean | t |
|---|---:|---:|---:|---:|
| Monday | 4,558 | 49.8% | +0.077% | 2.70 |
| Tuesday | 4,936 | 48.3% | +0.038% | 1.89 |
| Wednesday | 4,798 | 52.1% | +0.013% | 0.61 |
| Thursday | 4,659 | 49.7% | −0.001% | −0.05 |
| Friday (over the weekend) | 4,700 | 49.4% | +0.036% | 2.01 |

**Per stock:** only **10 of 250** symbol × weekday groups had |t| ≥ 2. About 12 would be expected by pure chance, so there's **no evidence of per-stock weekday effects**.

**Pooled:** Monday looks positive, but the 50 symbols share the same market nights (many are insurers), so the pooled t is overstated.

**Conclusion:** don't add a weekday rule. Re-test only if months of forward data show the same pattern.

## 3. What this means for the strategy

1. **Expectations:** a scanner pick's real next-night win rate is closer to 52–53% than to the 75–80% the TOS columns show. Size positions accordingly; the size decision is yours.
2. **Collecting forward data is the important part.** Run the export after each scan so we can measure the rules **out of sample** on the names the scanner actually picked that day (To-Do §2c).
3. **Next analysis worth doing:** whether the V3 Boringness score and the 5D-vs-20D momentum separate better nights from worse ones, measured on the GTC_EXT high-touch outcome rather than the open.
