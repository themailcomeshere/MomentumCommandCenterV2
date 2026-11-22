# New chat handoff: 2026-11-22

Branch `feature/opportunity-quality-classification`. Background: [NewChatHandoff-20261121.md](NewChatHandoff-20261121.md). Checklist: [Master-To-Do.md](Master-To-Do.md). Daily routine: [MCC-Operator-Guide.md](MCC-Operator-Guide.md). Decision record: [Chart-Study-MCC-Parity-20261122.md](Engineering/Chart-Study-MCC-Parity-20261122.md).

Monday 2026-11-23 is US standard time: regular session 07:30–14:00 AZ (09:30–16:00 ET). EOD GET OUT = 13:55 AZ (15:55 ET).

## Operator question (Sunday 07:11 AZ)

"Based on the latest build, workflow, and goals, do the 5m/1m Chart Study formulas need to be updated?"

### Answer given

Yes. The study files were last changed 2026-11-09 (`c4c09f0`). A source comparison against head `0d7f9a1` found four gaps:

1. **Opening entry hold** (`b05bb98`, `0a2160f`): MCC withholds EXECUTION READY 09:30–09:45 ET; the 1M study did not.
2. **Overnight window:** repo 1M V14.4.11 decided at 12:30–12:55 AZ. In standard time that is 14:30–14:55 ET, before the final-hour buy window (15:00–16:00 ET, decided 2026-11-14).
3. **Overnight gates:** V14.4.11 used 5M permission/structure and other momentum gates as hard blocks, against READY decision 10 (`d2eebe4`) and the CLAUDE.md guardrail.
4. **EOD:** three different times (MCC engine 15:55 ET; latch and installed 5M 18:55/19:55 ET; repo copies 18:55/17:55 ET).

Unaffected: the 1M coverage gate (IEX-only), the 5M latch rules (already a port of the study), and the Red Zone colours.

## Operator decision (09:18 AZ)

**Option A: EOD = 15:55 ET in both seasons**, consistent with MCC. "Document this."

This reverses the earlier 3:55 PM Arizona target (Nov 2–6 audits). Supersession notes were added to `Thinkorswim-MCC-Extension-Parity-20261102.md` and `Full-Master-To-Do-Audit-20261103.md`.

## Built (`0c96252`, CI `45043823774` SUCCESS, 558/558)

- **Core:** `FiveMinutePermissionLatch` EOD defaults 1555/1555, `RulesVersion` = `TOS-5M-PERMISSION-LATCH-2 (5M study V4.1.8, EOD 15:55 ET)`. Two tests replace one (both seasons; equality with `V2Config` EOD; old inputs still configurable).
- **5M Main Study V4.1.8** and **5M Authority Diagnostic**: from the installed V4.1.7 sources; EOD inputs and the Arizona time diagnostic only.
- **1M Main Study V14.4.12**: from installed V14.4.10 (not V14.4.11):
  - EOD 1555/1555;
  - opening hold, `openingEntryHoldMinutes = 15` (READY → **PREPARE - OPENING HOLD**, AGG. BUY withheld, exits never delayed; READY block code 11, AGG block code 20);
  - Boring Overnight final-hour label and **OVERNIGHT WINDOW OPEN** bubble (only a confirmed 1M SELL reads NO HOLD; otherwise DATA? - CHECK MCC READY; 5M permission and EOD are notes; never green).
- **Archived:** repo 1M V14.4.11 and repo 5M V4.1.7 (1755) under `docs/archive/Charts/`.
- **Rules:** `docs/rules/thinkscript.md` §9: study EOD and opening-hold defaults must equal MCC's; engine rule changes need a matching study update or a §5 parity item; studies never emit an overnight HOLD.
- **Docs:** Master To-Do §5 updated in place; Operator Guide (opening hold step 5 and an End-of-day paragraph); decision record with install and Monday validation steps.

## Not verified

- Thinkorswim compilation of the three studies: Data not found in sources.
- Local Release tests: no .NET SDK in Claude's session. CI is the only build/test evidence.
- Whether TOS `RegularTradingEnd` honours early closes (2026-11-27): Data not found in sources.
- 1M execution-window ageing during the hold versus MCC's re-evaluation at release: no matched-bar evidence.

## Next operator steps

1. Before 07:30 AZ Monday, from `D:\source\repos\MomentumCommandCenterV2`: `MCC.ps1 stop`, `.\scripts\Get-Latest-And-Test.ps1` (expect SHA `0c96252` or later, 558+ tests), `MCC.ps1 start`, Ctrl+F5. This is an API change (latch EOD), so the restart is required.
2. Install the studies one at a time: [Chart-Study-MCC-Parity-20261122.md](Engineering/Chart-Study-MCC-Parity-20261122.md) → **Operator install steps** 1–7. Send any compile error with its line number.
3. During Monday's session, run **Operator validation** steps 1–4 in the same document (07:30, 07:45, 13:00 and 13:55 AZ) with WORKSPACE = **INTRADAY MOMENTUM**, **4 Red Zone** open. Step 5 (time diagnostics) is optional.
4. Report any mismatch with symbol, AZ bar time, the TOS value and the MCC value.

## Uncertainties

- The Thinkorswim click path in the install steps (**Studies → Edit studies… → scroll icon**) is written from general Thinkorswim knowledge, not checked against the operator's screen.
- The operator's saved study inputs may override the new defaults; the install steps say to check them.
