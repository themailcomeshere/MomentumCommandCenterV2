# Overnight chart re-review — 2026-10-27

**Scope:** Six GitHub screenshots in `src/MomentumCommandCenterV2.Application/Images/Screenshots/10272026` plus the newer user-supplied RTO screenshot (file timestamp 15:21:46). This is chart-state triage only; it does not establish overnight historical consistency, catalyst clearance, liquidity, or profitability.

## Key correction: distinguish the 5M permission from 1M execution state

The RTO screenshots do not show one unified state. In both RTO captures, the upper 5M strip shows **PREPARE TO BUY / 5M PERMISSION YES / bullish structure / bullish regime established / 1M execution environment YES / EOD ACTIVE**. The lower 1M strip in the newer capture explicitly shows **1M STATE GET OUT / 5M STATE GET OUT / 5M PERMISSION NO / extension 5.7 ATR / EOD GET OUT**. The newest right-edge 1M candle is an unusually large green volume/price bar, and the latest labels include candidate-blocked / execution-expired. So the upper timeframe is constructive, but the 1M panel does not confirm an actionable entry at the captured right edge. This is a timeframe disagreement, not a reason to ignore the 5M panel or to infer the 1M panel is ready.

The older RTO capture also shows a 5M extension around 0.43 ATR and the lower 1M extension around 5.7 ATR. That difference matters: 5M extension is not excessive in this snapshot, while the 1M move is extremely extended. ATR extension is a risk/entry-quality input, not an automatic rejection solely because the strategy prefers boring trades. Here it is one of several reasons to avoid chasing the last 1M impulse. Reassess after a fresh, stable pullback/reclaim or consolidation; do not weaken the original 1M gate.

## Per-symbol chart assessment

| Symbol | Visible chart evidence | Overnight disposition |
|---|---|---|
| **RTO** | 5M permission YES, bullish structure/regime, 5M extension ~0.43 ATR, RVOL 5.16, RSI ~65.4. But newer lower 1M strip is GET OUT, 1M extension ~5.7 ATR, a very large last green bar, and labels show blocked/expired execution. | **Watch only; no chase entry.** Best 5M structure in this set, but no valid 1M confirmation at the newest right edge. Needs fresh post-impulse setup and independent Boring Overnight/catalyst gates. |
| **MFM** | 5M runner rising, permission YES, bullish regime, but RVOL only ~0.13 in the top strip. Lower 1M shows permission NO, transition, extension ~2.67 ATR and EOD GET OUT. | **Watch / not ready.** Conflicting volume evidence and lower-timeframe rejection. |
| **BNC** | 5M permission YES and bullish regime, but 5M extension ~2.68 ATR, RVOL ~0.23, neutral runner momentum. Lower 1M shows permission NO, transition, extension ~10.24 ATR and EOD GET OUT. | **Reject for boring overnight at this snapshot.** Strong extension and no lower-timeframe confirmation. |
| **SXC** | 5M permission NO, neutral structure, bullish regime not established, despite RVOL ~14.58 and extension ~0.78 ATR. Price spikes and then fades; lower 1M trends down and ends GET OUT. | **Reject / catalyst-spike risk.** High RVOL alone does not create eligibility. |
| **SAP** | 5M permission NO, bearish structure, bullish regime not established, RVOL ~0.03, extension ~3.87 ATR. Lower 1M remains bearish/GET OUT. | **Reject.** |
| **PANW** | 5M permission YES, transitioning structure, bullish regime established, but extension ~3.35 ATR and very low displayed RVOL (~0.03). Lower 1M permission NO, extension ~5.37 ATR and EOD GET OUT. | **Watch at most; not ready.** Extended move and lower timeframe does not confirm entry. |

## Candidate conclusion

- **No screenshot is sufficient to certify a Boring Overnight BUY.**
- **RTO is the one to keep on a conditional technical watchlist**, not because it is cleared, but because its 5M structure/permission is the strongest of these charts. The latest 1M state and extreme 1M extension prevent calling it entry-ready.
- Do not use the EOD label in isolation: evaluate timeframe, timestamp, current bar, extension, permission and execution state together. Conversely, do not ignore an explicit current 1M GET OUT just because 5M is positive.
- “Boring” refers primarily to consistency of the overnight return distribution and low event/overnight risk, not merely low ATR. High intraday extension is a separate chase/gap-risk penalty; it is not a substitute for the historical ON statistics.

## Evidence still missing

No current source in this screenshot set proves the full Boring Overnight hard gates for RTO or any other symbol: exact unrounded 20D/5D overnight statistics, current overnight return, daily average volume, current quote/spread, authoritative catalyst/news clearance, exclusions, or fresh next-session bars. **Data not found in sources.** These screenshots are dated 2026-10-27 and must not be treated as live market state after their capture time.
