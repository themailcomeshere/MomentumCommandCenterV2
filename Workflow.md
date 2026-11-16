## Step 3
***8*** I want reasons (which include the EXCLUDED WATCHLIST) color-coded. 
***10*** Removing it and Ranking again removed its EXCLUDED_WATCHLIST reason


## Rubber Ducky Time
Things may have gotten off-track over the course of this solution and I want to walk you through what I want in order to clarify some things.
Currently, I swing/momentum trade. I enter my Buy/Sell orders on Schwab.com or the mobile app. I view Thinkorswim desktop app as my dashboard. Through the work in this solution, I use 5-minute and 1-minute charts with their own Studies and States to provide insights.
https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Validation/Installed-1M-Study.txt
https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Validation/Installed-5M-Authority-Diagnostic.txt
https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Validation/Installed-5M-Study.txt

***DAYTIME*** Up until Daylight Saving Time kicked in, I was only able to sit down and begin trading at 8:30 AM Arizona time. This meant I was missing out on 2 hours of trading time *at best* and this meant I was almost always missing the best trading window of the day (see "Workflow"). However, I now sit down and start at 7:30 AM Arizone time. I am able to watch my screen (currently Thinkorswim) actively and swing/momentum trade.

***OVERNIGHT*** I want to make consistent gains each night - small, incremental gains add up over time. To be consistent, it will almost certainly be less than 1% - hence the "boring" classification. I 

***DATA GATHERING*** https://github.com/themailcomeshere/MomentumCommandCenterV2/blob/feature/opportunity-quality-classification/docs/Charts/Flexible%20Grid/OHLCV/MCC_OHLCV_ATR_V2.txt
These are the Scanners I use: https://github.com/themailcomeshere/MomentumCommandCenterV2/tree/feature/opportunity-quality-classification/docs/Scanners

***CURRENT WORKFLOW*** I clear my Watchlist each morning. I scan 1 - MOMENTUM HUNTER. I click & drag the candidates to the Watchlist. Then I change to 2A - A+ MOMENTUM BULLISH and click & drag to Watchlist. Then I change to 2B - BREAKOUT TRIGGER and click & drag to Watchlist. Then I change to MOMENTUM PIVOT V2 and click & drag to Watchlist. Then I change to D - MARKET MOVERS / DISCOVERY and click & drag to Watchlist. Then I'll switch to "Charts" and use my keyboard Up/Down arrows or click with my mouse to scroll through all of the stocks I have added to my Watchlist. I observer the Charts' signals/bubbles/states to see if any are EXECUTION READY. If they are, I open it up in Schwab and place a Limit Buy order for a price I feel will capture it. I then watch my Chart for any SELL or RUNNER WANING signals, at which point I will set a Limit Sell order for a price I feel will capture it because I want to get out.


***DESIRED WORKFLOW*** I want to use engineering intelligence to automatically scan & create/update my Watchlist. I want that intelligence to create a "Red Zone" (I use that term because the NFL Red Zone's: when a team is in the Red Zone [and within scoring opportunity], the stream changes to that. And up to 4 games get that splitscreen so you can see 4 games at the same time, all exciting because they are within scoring opportunities. So my Red Zone are the best 4 stocks/tickers at the moment, automatically refreshing themselves on the screen/dashboard [so I don't have to manually do it and miss out on one that I either just scanned and missed or haven't gotten to yet]. I want clear, color-coded signals to inform me when a stock is "heating up" and is about to become execution ready [and I should prepare to buy], when a stock is execution ready [and I should BUY], when it is winding down [and I should prepare to sell], when a stock breaks down and I should Sell immediately, and more. You can see that the current system has these (I think we have 6 total states?).



# Numerically Labeleling MCC
I want you to numerically label each step in both the INTRADAY MOMENTUM and BORING OVERNIGHT processes. It can be a whole number for a section and then subsections (e.g. 1.1). If there are multiple options (If this, check this; else, check that), an example label would be 1.a.1 vs 1.b.2.
This will help me understand the flow and could point out errors/inefficiencies in the layout.