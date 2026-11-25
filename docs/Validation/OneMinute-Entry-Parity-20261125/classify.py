"""Classify READY disagreements between MCC replay rows and study-port rows (asOf = bar start + 1 min)."""
import json, sys, datetime as dt, collections
out, tag = sys.argv[1], sys.argv[2]; AZ = dt.timezone(dt.timedelta(hours=-7)); tot = collections.Counter(); lines = []
for sym in ["NVDA", "TSLA", "PLTR"]:
    mcc = {r["asOfUtc"]: r for r in json.load(open(f"{out}/mcc_{sym}.json"))}
    for s in json.load(open(f"{out}/study_{sym}.json")):
        t = dt.datetime.strptime(s["barUtc"], "%Y-%m-%dT%H:%M:%SZ")
        m = mcc.get((t + dt.timedelta(minutes=1)).strftime("%Y-%m-%dT%H:%M:%SZ"))
        if not m or m["status"] != "REPLAYED": continue
        mr, sr = bool(m["entryAllowed"]), s["ready"]
        tot["MCC READY"] += mr; tot["study READY"] += sr
        if mr and sr: tot["both"] += 1; continue
        if not (mr or sr): continue
        mp, sp = bool(m["fivePermission"]), s["fivePerm"]
        c = []
        if mr:
            if s["hold"]: c.append("opening-hold bar boundary")
            if mp and not sp: c.append("5M permission: study NO, MCC latch YES")
            if s["extDanger"]: c.append("extension 2.00-2.25 ATR")
            if sp and not s["fiveExecPerm"]: c.append("study 5M exec extras NO")
            if not (0 <= s["age"] < 2): c.append("trigger window")
            if s["invalid"] and s["fiveExecPerm"] and not s["extDanger"]: c.append("study invalidation")
            side = "MCC only"
        else:
            if sp and not mp: c.append("5M permission: study YES, MCC latch NO")
            elif not m["fiveStructureBull"]: c.append("MCC current 5M structure")
            if not m["oneStructureBull"]: c.append("MCC 1M close > EMA9 / EMA9 > EMA20")
            if not m["oneMomentumPositive"]: c.append("MCC 1M momentum")
            if s["rsi"] >= 78: c.append("RSI >= 78")
            side = "study only"
        tot[(side, c[0] if c else "?")] += 1
        lines.append([sym, t.replace(tzinfo=dt.timezone.utc).astimezone(AZ).strftime("%m-%d %H:%M"), side, s["close"], round(s["ext"], 2), round(s["rsi"], 1), "; ".join(c)])
for k, v in sorted(tot.items(), key=lambda x: str(x[0])): print(v, k)
json.dump(lines, open(f"{out}/disagreements_{tag}.json", "w"))
