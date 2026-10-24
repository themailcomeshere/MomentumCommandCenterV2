# MCC V3 — XRPN 5M Authority and 1M Handoff Audit
**As of:** 2026-10-23 (Arizona time)  
**Status:** Screenshot evidence reviewed; source-level comparison underway. No study logic changed.

## 1. Evidence already provided
- User supplied `Screenshot 2026-10-23 154303.png`; chart dates are set to 10/02/2026 and the top-left chart date is not authoritative.
- The 5M time axis shows the 12:45 region. The diagnostic bubbles around the transition show `Perm=0`, `PRAW=0`, `PBLOCK=2`, `State=0` on red bars.
- In V4.1.7 Authority Diagnostic, `PBLOCK=2` means persistent major failure. The user also manually entered the bubble values. Do not request another screenshot or repeat data entry.

## 2. 5M permission logic observed in source
Normal `MCC_V3_2_5M_V4_1_7.txt` defines raw permission as:
- price above Daily VWAP;
- price above EMA9;
- EMA9 greater than or equal to EMA20;
- RSI at or above the configured minimum.

Permission latch clears on EOD, persistent major failure, or persistent structural damage; otherwise raw bullish permission sets the latch to 1, and absent a reset it retains its prior value. Major failure is defined as price below Daily VWAP, price below EMA9, EMA9 below EMA20, and EMA20 below SMA50 for the configured persistence count. The diagnostic block-code priority reports persistent major failure as `PBLOCK=2`.

## 3. V14.5 1M source-level divergence found
Library file: `MomentumCommandCenterV3_2_1M_V14_5.txt`. The screenshot's 1M study label is consistent with V14.5. V14.5 reconstructs 5M state locally from secondary 5M data rather than directly consuming the normal study's hidden permission plot.

Differences requiring same-bar validation:
1. Normal 5M raw permission explicitly requires 5M price above Daily VWAP; V14.5's `fiveBullishPermissionRaw` does not directly include a 5M Daily VWAP test. The 1M layer separately gates execution with current 1M price above Daily VWAP. Those are not equivalent conditions.
2. Normal 5M permission allows EMA9 >= EMA20; V14.5's `fiveBullishCore` requires EMA9 > EMA20, making the reconstructed permission stricter at equality.
3. Normal 5M structural damage is `price < Daily VWAP AND EMA9 < EMA20` for the persistence count. V14.5 uses different conditions involving close below EMA9/EMA20 and EMA9 slope.
4. Normal 5M major failure includes `price < Daily VWAP`; V14.5's reconstructed `fiveMajorFailureRaw` instead checks close < EMA9, EMA9 < EMA20, and EMA20 < SMA50 without the explicit Daily VWAP term.

These are verified source differences, not yet a confirmed production defect. The architecture may intentionally reconstruct a proxy because thinkScript has secondary-aggregation limitations, but that intention must be made explicit and the same bars compared before changes.

## 4. Exact next steps (no new user input currently needed)
1. Align 10/02 12:40, 12:45, and 12:50 5M bars between normal 5M V4.1.7 and Authority Diagnostic V2; record per-bar `Permission`, `PermissionRaw`, `PermissionBlock`, `StructuralDamage`, and `MajorFailure`.
2. On the corresponding V14.5 1M bars, record reconstructed `fivePermission`, `fiveExecutionPermission`, `fiveStructuralDamagePersistent`, `fiveMajorFailurePersistent`, and `fiveState`.
3. Identify the first bar where the normal 5M latch and V14.5 reconstructed permission differ. Explain which exact condition causes the divergence.
4. Decide whether 1M should use a faithfully mirrored 5M authority or a documented independent proxy. Do not modify code until this decision follows from bar-by-bar evidence.
5. Then validate candidate/confirmation/event-armed READY/expiry/extension block/re-entry and runner/swing transitions, keeping `NO NEW SIGNAL`, `HOLD`, and `EXIT` distinct.

## 5. Status / user asks
- Screenshot/bubble timestamp evidence: **received and reviewed**. No re-upload needed.
- Exact study identity: V14.5 source is present in Library and consistent with the screenshot label; no request to identify it again.
- Remaining user-owned input: none for the first source comparison. Only ask later if a required historical fixture or complete Schwab exclusion list cannot be found in existing project sources.
- No code changes or profitability claims are authorized by this audit alone.