"""Replay MCC (V2 engine via /api/market-data/replay) at every 1-minute close of the regular session.
Usage: python -I mcc_replay.py OUTDIR SYMBOL... ; writes OUTDIR/mcc_<SYMBOL>.json"""
import json, sys, urllib.request, datetime as dt
from concurrent.futures import ThreadPoolExecutor

BASE = "http://localhost:53360/api/market-data/replay"
DAYS = {"2026-11-24": "21:00", "2026-11-25": "17:40"}  # US standard time: 09:30 ET = 14:30 UTC

def one(sym, t):
    url = f"{BASE}?symbol={sym}&asOfUtc={t:%Y-%m-%dT%H:%M:%SZ}&providerName=Alpaca&feed=iex"
    with urllib.request.urlopen(url, timeout=60) as r:
        d = json.load(r)
    dec = d.get("decision") or {}
    ev = dec.get("evaluation") or {}
    return {
        "asOfUtc": t.strftime("%Y-%m-%dT%H:%M:%SZ"), "status": d.get("status"),
        "state": dec.get("state"), "action": dec.get("action"), "entryAllowed": dec.get("entryAllowed"),
        "reason": dec.get("reason"), "atrExtension": ev.get("atrExtension"), "hardExtended": ev.get("hardExtended"),
        "fivePermission": (d.get("fiveMinutePermissionLatch") or {}).get("permission"), "fiveStructureBull": ev.get("fiveMinuteStructureBull"),
        "oneStructureBull": ev.get("oneMinuteStructureBull"), "oneMomentumPositive": ev.get("oneMinuteMomentumPositive"),
        "openingHoldWithheld": ev.get("openingHoldWithheld"),
        "coverage": d.get("oneMinuteCoverageLast15"), "oneMinuteBarUtc": d.get("oneMinuteSourceTimestampUtc"),
        "fiveMinuteBarUtc": d.get("fiveMinuteSourceTimestampUtc"),
    }

def main():
    out, syms = sys.argv[1], sys.argv[2:]
    for sym in syms:
        times = []
        for day, end in DAYS.items():
            t = dt.datetime.fromisoformat(day + "T14:31:00")
            stop = dt.datetime.fromisoformat(day + "T" + end + ":00")
            while t <= stop:
                times.append(t); t += dt.timedelta(minutes=1)
        with ThreadPoolExecutor(6) as ex:
            rows = list(ex.map(lambda t: one(sym, t), times))
        with open(f"{out}/mcc_{sym}.json", "w") as f:
            json.dump(rows, f)
        ready = sum(1 for r in rows if r["entryAllowed"])
        print(sym, len(rows), "minutes; MCC entryAllowed:", ready, flush=True)

main()
