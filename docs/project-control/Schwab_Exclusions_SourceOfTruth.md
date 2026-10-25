# Schwab Exclusions — Current User-Provided Source of Truth

**Last supplied by user:** 2026-10-25  
**Purpose:** Maintain the growing set of symbols excluded from MCC candidate/watchlist/scanner qualification because they are Schwab-excluded/unavailable according to the user's workflow.

## Current list

```text
AMHpG
ANL
ATHpA
AXIAY
BACpQ
BHRpB
BRNX
BUSEP
BZUN
CAPMF
DAO
DBGI
DKI
FITBpI
FMX
FNMAS
FOCL
FSUGY
GGAL
HELP
HENKY
KCDMY
KEYpI
KOF
KREFpA
METCZ
MFApC
MTBpJ
MTBpK
NAMI
NEEpT
NEEpW
OAKpA
PCLA
PPERY
PPRUY
PSApN
PSNYW
RFpF
SHTDY
SMMYY
SPCB
SSM
STFS
STTpG
SWVL
TJGC
TRTXpC
TRYIY
TYOYY
VCRE
WBKCY
WYHG
YDES
YJ
```

- Count: 55 symbols (preserve exact casing and punctuation, including lowercase `p` in symbols such as `AMHpG` and `KEYpI`).
- Treat this as the current list supplied by the user, not proof of the live tradability status of each security.
- The list is append-oriented: add newly identified exclusions; do not remove existing entries without explicit review/authorization.
- Normalize symbols only for comparison where a separate canonicalization rule is defined. Never silently rewrite the displayed/source symbol.
- Exclusion checks must happen before a candidate can become `READY`; an excluded symbol must be `BLOCKED` with reason `GATE_SCHWAB_EXCLUSION`.
- Record list version/date with every decision snapshot so historical outcomes can be reproduced.
- When adding a symbol, retain the date/source/reason. If a symbol is explicitly removed later, retain the change history rather than deleting the prior record.

## Implementation requirements

1. Store the list in one canonical file/configuration source; do not maintain divergent hard-coded copies in scanner, Boring Overnight, MarketWatch, and Morning Report logic.
2. Load it into C# through a testable exclusion provider/service.
3. Add tests for every listed symbol, a near-match that must not be excluded, case/punctuation preservation, duplicate entries, and list-version changes.
4. Apply the same canonical list to scanner and watchlist qualification; display exclusion reason to the user.
5. Keep provider/tradability checks distinct from this manually maintained exclusion list. A provider saying a symbol is unavailable should be logged as a separate observation and must not silently mutate this source of truth.
