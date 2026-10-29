# Boring Overnight Hold Decision Bubble

**Status:** Decision contract and server-side decision helper committed; Thinkorswim bubble implementation and runtime validation remain open.

## Decision

The red EOD GET OUT bubble and green HOLD OVERNIGHT bubble must not exist as unconditional, independent signals. They are mutually exclusive outcomes of the same Boring Overnight qualification decision, evaluated only inside the configured local-time window.

Default window: **12:30 through 12:55 local chart/trading time, inclusive**. The timezone must be explicitly configured; never assume the host computer timezone or silently mix Arizona and Eastern time.

| Condition inside the window | Bubble text | Color | Meaning |
|---|---|---|---|
| All required gates pass and evaluation is READY | HOLD OVERNIGHT | Green | The supplied snapshot qualifies under the configured boring-first rules |
| A known hard gate fails or a caution prevents qualification | EOD GET OUT | Red | Do not hold overnight under this decision; if a position is held, apply the user's position-management plan |
| Required evidence is missing, stale, or invalid | DATA? — NO HOLD | Yellow | Do not claim the candidate is safe or qualified; missing evidence is not a pass |
| Outside the configured time window | No hold/get-out bubble; neutral diagnostic optional | Neutral | This decision has not been evaluated in its authorized time window |

The EOD GET OUT wording is a **decision result**, not a statement about the clock. HOLD OVERNIGHT and EOD GET OUT must derive from the same timestamped input snapshot and versioned rule set.

## Qualification gates

Use the existing Boring Overnight hard gates as a baseline: price above $5; 20-day average volume at least 500,000; overnight win rate at least 60%; 20-day average overnight return at least +0.10%; 5-day average overnight return strictly greater than the 20-day average; price above 20-day SMA; not excluded; catalyst clearance; fresh data; 5M permission and bullish structure; 1M execution state not SELL/GET OUT/BLOCK; and no unresolved high-extension / abnormal-volatility warning. A score must never override a hard gate.

A READY result is possible only when all required inputs are present, fresh, and verified. A missing catalyst provider, missing historical window, stale feed, unverified exclusion lookup, or missing TOS parity must not silently pass. The current ThinkScript source cannot access these external inputs and therefore intentionally returns DATA? — NO HOLD when chart-only technical gates pass. It must not infer READY from technical indicators alone. If the policy treats a warning as non-qualifying, it must map to EOD GET OUT; otherwise explicitly document why that warning is allowed.

## Signal and position semantics

- The existing EOD study signal must not be interpreted as a time-of-day state.
- The proposed bubble is a **position decision** made during the window, not an independent EOD condition.
- HOLD OVERNIGHT means the candidate meets the configured rules for holding overnight; it is not an order or guarantee.
- EOD GET OUT means the candidate fails the configured overnight hold decision. It is not by itself an automated order.
- DATA? — NO HOLD prevents missing data from being mislabeled as a confirmed negative.
- Keep higher-timeframe 5M authority, 1M execution, candidate quality, and position-management output distinct internally even if the chart renders one final bubble.

## Implementation already committed

- BoringOvernightHoldDecision converts an evaluator result into a time-gated decision, using an explicit TimeZoneInfo.
- Green HOLD OVERNIGHT only for READY inside the window.
- Red EOD GET OUT for known non-READY outcomes inside the window.
- Yellow DATA? — NO HOLD for missing/invalid evidence inside the window.
- Outside the window it emits NO DECISION, not either of the two trading bubbles.
- Unit tests cover both window boundaries, outside-window behavior, READY, blocked, and DATA? outcomes.

## Still required before production

1. Wire the decision helper to the real persisted, source-backed candidate snapshot.
2. Verify local-time convention with the user and Thinkorswim chart settings, including DST and the Arizona/Eastern distinction.
3. [IMPLEMENTED IN REPOSITORY SOURCE] Updated docs/Charts/1M/MainStudy.txt and docs/Charts/5M/MainStudy.txt. The 1M study evaluates at the 12:55 Arizona bar; known chart technical failures are red EOD GET OUT, and passing chart checks with missing external qualification evidence are yellow DATA? — NO HOLD. The 5M label now reports ENTRY CUTOFF rather than a clock-based EOD GET OUT.
4. Validate the ThinkScript compile and same-bar parity in Thinkorswim, including 1M/5M timestamp alignment.
5. Complete overnight return history, GTC_EXT coverage, catalyst/exclusion evidence, and freshness gating.
6. Run build/tests and record verified results; current CI status must not be assumed.

## User instructions

1. The timezone is confirmed as **Arizona time (America/Phoenix)**.
2. Install the updated 1M source from docs/Charts/1M/MainStudy.txt in Thinkorswim and confirm it compiles. Install docs/Charts/5M/MainStudy.txt to update the 5M cutoff label.
3. Validate the 12:55 Arizona bar. Expected current behavior is red EOD GET OUT for a known technical failure, otherwise yellow DATA? — NO HOLD. Green HOLD OVERNIGHT is intentionally unavailable in ThinkScript until complete overnight history, catalyst, exclusion and freshness evidence is connected to the authoritative decision path.

No broker credentials or order permissions are needed; this feature must remain a decision aid, not automatic order submission.


## 2026-10-28 runtime validation

User reports successful installation/compilation of both updated studies and confirms the intended 12:55 PM Arizona outcomes:

- **SXC:** red `EOD GET OUT` when a defined chart-technical blocker is present. [Screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212549.png)
- **RTO:** yellow `DATA? — NO HOLD` when chart checks pass but required external qualification evidence is unavailable. [Screenshot](https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/src/MomentumCommandCenterV2.Application/Images/Screenshots/10282026/Screenshot%202026-10-28%20212531.png)
- Green `HOLD OVERNIGHT` remains intentionally unavailable until the full qualification inputs are wired into the authoritative server-side decision path.

This is user-reported runtime validation of the chart behavior; it is not proof of server/TOS parity, complete external data coverage, or CI status. The next implementation task is to feed the decision helper from persisted timestamped candidate snapshots and validated overnight/catalyst/exclusion/freshness inputs, preserving fail-closed DATA? behavior.
