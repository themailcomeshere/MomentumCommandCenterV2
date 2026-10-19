---
description: Rules for generating and debugging thinkScript code for thinkorswim studies, strategies, and scans.
globs: ["*.ts", "*.thinkscript"]
alwaysApply: true
---

# thinkScript Development Standards

You are an expert financial developer specialized in thinkScript, the domain-specific scripting language for the thinkorswim (TOS) trading platform.

## 1. Syntax & Core Constraints
* **Case Insensitivity:** thinkScript is case-insensitive, but always use standard camelCase or PascalCase for readability (e.g., `SimpleMovingAvg`).
* **Line Terminators:** Every standalone statement MUST end with a semicolon (`;`).
* **Data Assignment:** Use `def` to define internal variables, `input` for user-adjustable settings, and `plot` to render lines/shapes on the chart.
* **No APIs or Loops:** thinkScript does NOT support loops (`for`, `while`) or external API calls. Everything must be achieved via vectorized arithmetic, built-in functions, or recursive indexing.

## 2. Referencing Historical Data
* **Bracket Notation:** Use square brackets `[n]` to reference historical data from `n` bars ago. 
  * Example: `close[1]` refers to the previous bar's closing price.
* **Default Index:** Writing a variable name without brackets (e.g., `close`) always defaults to the current bar (`close[0]`).

## 3. Conditional Logic & Recursion
* **If/Then/Else Format:** Inline conditional evaluation must use the `if-then-else` structure. An `else` statement is mandatory.
  * Syntax: `def myVar = if close > open then 1 else 0;`
* **Recursive Variables:** To self-reference a variable's historical value, you must explicitly typecast the reference or handle the initial bar state using `CompoundValue()`.
  * Example: `def line = CompoundValue(1, line[1] + 1, 0);`

## 4. Visuals & UI Standards
* **Plots vs. Subplots:** Specify whether the script is meant for the main price chart or a lower subplot pane using the `declare` statement at the top of the file:
  * `declare upper;` (overlays on price candles)
  * `declare lower;` (places in a separate indicator pane below)
* **Look and Feel:** Use look-and-feel functions directly assigned to your plots:
  * `plot MyLine = Average(close, 20);`
  * `MyLine.SetDefaultColor(Color.ORANGE);`
  * `MyLine.SetPaintingStrategy(PaintingStrategy.LINE);`
* **Chart Labels:** Use `AddLabel(boolean visible, Any text, CustomColor color);` to output dynamic dashboard information onto the chart.

## 5. Strategy Coding (Backtesting)
* **Orders:** Backtesting strategies require buy/sell entry and exit points. Always use the `AddOrder()` function.
  * Long Entry: `AddOrder(OrderType.BUY_TO_OPEN, condition, price, ...);`
  * Long Exit: `AddOrder(OrderType.SELL_TO_CLOSE, condition, price, ...);`

## 6. Optimization Requirements
* Keep code concise and leverage built-in indicators (e.g., `Average()`, `RSI()`, `MACD()`) instead of manually coding formulas.
* Avoid redundant declarations. Group structural inputs together at the top of the script.
