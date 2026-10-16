Component	Weight	What we want
ON Win %	25	Higher is better
Median ON %	20	Positive and reasonably consistent
20D Avg ON %	15	Positive, but don't reward extreme gaps
5D Avg ON %	10	Positive / improving
ON Consistency	10	Higher is better
ON Volatility	10	Lower is better
Average Volume	5	Higher liquidity
Price	5	Prefer established >$5 stocks

From <https://chatgpt.com/c/6a7f50f6-f3a0-83e8-9f99-2ced6d82ff49> 

REJECT if:
	• ON Win % < 60% 
	• 20D Avg ON < +0.10% 
	• 5D Avg ON ≤ 20D Avg ON 
	• Avg Volume < 500K 
	• Price < $5 
	• Below 20 SMA 
	• One of our Schwab-excluded securities





Boring Overnight — Cheat Sheet
Purpose
We are going for Boring Overnight candidates/positions, valuing consistency over huge gains.
	Give me the stock that makes a small amount of money repeatedly, not the stock capable of making the biggest overnight move.
Zero position is a valid outcome.
BORINGNESS FILTER / RANK
Component	Weight	What we want
ON Win %	25	Higher is better
Median ON %	20	Positive and reasonably consistent
20D Avg ON %	15	Positive, but don't reward extreme gaps
5D Avg ON %	10	Positive / improving
ON Consistency	10	Higher is better
ON Volatility	10	Lower is better
Average Volume	5	Higher liquidity
Price	5	Prefer established >$5 stocks
Total	100	
PEAK “BORINGNESS” CHEAT SHEET
These are preferred / peak-boringness ranges, not hard rejection rules. They are quick ranking aids after the hard gates pass.
Current ON %
	• Preferred boring zone: small positive to <3.0%
	• Lower/modest is generally better.
	• A current ON move at or above ~3% increasingly reduces Boringness because it may indicate a catalyst, abnormal volatility, or an already-extended move.
20D Avg ON %
	• Preferred boring zone: +0.10% to <2.0%
	• Positive is required, but we do not want to maximize the average.
	• Very large averages can be caused by a few outsized overnight gaps.
5D Avg ON %
	• Preferred boring zone: positive to <5.0%
	• Ideally positive and improving, but still controlled.
	• 5D Avg ON should normally be above 20D Avg ON.
	• A very large 5D average reduces Boringness because it can indicate a recent volatility/catalyst regime.
Median ON %
	• Positive and reasonably close to the average is preferred.
	• A large average with a tiny median is not truly boring; median helps expose outliers.
ON Win %
	• Hard floor: 60%
	• 70%+ is substantially more attractive.
	• 80%+ is excellent if the other metrics remain boring.
ON Consistency
	• Higher is better.
	• 70%+ preferred; 80%+ excellent.
ON Volatility
	• Lower is better.
	• Prefer volatility that is low relative to the return being earned.
Average Volume
	• Hard floor: 20D average volume >=500K shares/day.
	• Comfortably above 500K is preferable.
Price
	• Hard floor: $5.
	• Prefer established stocks above $5.
HARD REJECTION GATES
REJECT if any are true:
	• ON Win % <60%
	• 20D Avg ON < +0.10%
	• 5D Avg ON <= 20D Avg ON
	• Average Volume <500K
	• Price <$5
	• Below 20 SMA
	• One of our Schwab-excluded securities
	• Material Event/Catalyst Risk incompatible with a boring overnight hold
Schwab exclusions
SHTDY, FMX, WBKCY, METCZ, ANL, GGAL, KOF, VCRE
If a candidate appears to violate a hard gate, investigate the scanner/data rather than treating its appearance as permission to trade.
BORINGNESS RANKING
Score	Classification
90–100	PERFECT BORING
80–89	VERY GOOD
70–79	POSSIBLE
60–69	AGGRESSIVE
<60	REJECT / NOT BORING ENOUGH
These bands are ranking guidance and do not replace the hard rejection gates.
CURRENT-DAY “TOO HOT” PENALTY
A stock can be historically boring and become non-boring today.
Reduce or reject Boringness for:
	• unusually large intraday percentage move
	• current ON % >=3%, especially a much larger move
	• abnormal volume spike
	• fresh news-driven move
	• fresh earnings/catalyst reaction
	• M&A / regulatory / government decision risk
	• oil/geopolitical sensitivity causing abnormal sector volatility
	• sharp extension away from normal structure
	• a large gap that makes the historical average misleading
	• materially deteriorating 5M structure
Examples from testing: a stock such as ECVT or FPS can be technically excellent yet too hot for a Boring Overnight position; FRO/DHT can have strong momentum but excessive oil/tanker or catalyst sensitivity.
COMPLETE OVERNIGHT PIPELINE
	1. OVER­NIGHT EDGE — historical overnight behavior.
	2. BORINGNESS FILTER / RANK — prefer small, repeatable, controlled returns.
	3. EVENT / CATALYST RISK — reject or penalize incompatible catalysts.
	4. CURRENT-DAY BEHAVIOR — penalize abnormal extension/volatility.
	5. 5M CHART — confirm current structure and permission.
	6. 1M CHART — confirm timing/permission.
	7. OVERNIGHT DECISION — OVERNIGHT READY / WATCH / REJECT.
	8. ZERO POSITION — if nothing survives, hold nothing.
KEY DESIGN PRINCIPLE
Keep Overnight Edge Score and Boringness Score conceptually separate.
	• Overnight Edge Score: historical ability to produce positive overnight returns.
	• Boringness Score: how controlled, repeatable, liquid, and non-extreme that edge is.
Final qualification should be:
Overnight Edge + Boringness + Event/Catalyst Risk + Current-Day Behavior + 5M Structure + 1M Confirmation
Do not collapse these concepts into one number until the individual components have been tested and validated.

From <https://chatgpt.com/library> 

