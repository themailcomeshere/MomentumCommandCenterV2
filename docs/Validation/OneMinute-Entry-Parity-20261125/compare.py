"""Join MCC replay rows (asOf = bar start + 1 min) with study-port rows and classify each disagreement."""
import json, sys, datetime as dt
out = sys.argv[1]
AZ = dt.timezone(dt.timedelta(hours=-7))
for sym in sys.argv[2:]:
    mcc = {r["asOfUtc"]: r for r in json.load(open(f"{out}/mcc_{sym}.json"))}
    st = json.load(open(f"{out}/study_{sym}.json"))
    both = onlyM = onlyS = 0; diffs = []; extErr = []
    for s in st:
        t = dt.datetime.strptime(s["barUtc"], "%Y-%m-%dT%H:%M:%SZ")
        m = mcc.get((t + dt.timedelta(minutes=1)).strftime("%Y-%m-%dT%H:%M:%SZ"))
        if not m or m["status"] != "REPLAYED": continue
        if m["atrExtension"] is not None: extErr.append(abs(m["atrExtension"] - s["ext"]))
        mr, sr = bool(m["entryAllowed"]), s["ready"]
        if mr and sr: both += 1
        elif mr or sr:
            if mr: onlyM += 1
            else: onlyS += 1
            why = []
            if mr:
                if s["readyBeforeHold"] is False and s["age"] >= s.get("age", 0) and not s["trigger"] and s["age"] < 0: why.append("study: no trigger event / window expired")
                if not s["fiveExecPerm"]: why.append("study 5M perm (1M reconstruction) NO" if not s["fivePerm"] else "study 5M exec perm NO")
                if s["extDanger"]: why.append(f"ext {s['ext']:.2f} >= 2.00")
                if s["invalid"] and s["fiveExecPerm"] and not s["extDanger"]: why.append("study invalidation (EMA20/VWAP/RSI<45/MACD)")
                if not s["cand"] and s["fiveExecPerm"]: why.append("study candidate env NO (EMA20 not rising / below EMA20 / MACD)")
            else:
                if not m["fivePermission"]: why.append("MCC 5M latch NO")
                if m["fivePermission"] and not m["fiveStructureBull"]: why.append("MCC 5M structure not bull")
                if not m["oneStructureBull"]: why.append("MCC 1M structure (close>VWAP,>EMA9,EMA9>EMA20) NO")
                if not m["oneMomentumPositive"]: why.append("MCC 1M momentum (MACD>=signal, RSI>=50) NO")
                if s["rsi"] >= 78: why.append(f"RSI {s['rsi']:.1f} >= 78")
                cov = m.get("coverage") or {}
                if cov.get("possible") and cov["present"] / cov["possible"] < 0.8: why.append(f"1M coverage {cov['present']}/{cov['possible']}")
            diffs.append((t.replace(tzinfo=dt.timezone.utc).astimezone(AZ).strftime("%m-%d %H:%M AZ"),
                          "MCC only" if mr else "study only", s["close"], round(s["ext"], 2), round(s["rsi"], 1),
                          round(s["rvol"], 2), s["trigger"] or "-", s["age"], "; ".join(why) or "?", m["state"]))
    extErr.sort()
    print(f"\n=== {sym}: both READY {both}, MCC only {onlyM}, study only {onlyS}; "
          f"ext |MCC-port| median {extErr[len(extErr)//2]:.4f} max {extErr[-1]:.4f} (n={len(extErr)})")
    for d in diffs: print("  ", d)
