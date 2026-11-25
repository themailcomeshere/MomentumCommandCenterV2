"""Port of the 1M Main Study V14.4.12 GREEN execution lane (docs/Charts/1M/MainStudy.txt), run on the same
stored IEX bars MCC replays. Research tool for the 2026-11-25 parity replay; not production code.

Modelling choices (stated in the evidence doc):
- 1M indicators over every stored IEX 1m bar (extended hours included, as the operator's TOS chart has ext hours ON).
- dailyVWAP = regular-session VWAP from 09:30 ET (the same as MCC), so the comparison isolates RULE differences.
  TOS with extended hours ON includes pre-market in VWAP; that data difference is reported separately.
- 5M secondary series = stored IEX 5m bars; at a 1m bar the current 5m bucket is the DEVELOPING bar built from the
  1m bars up to that minute (what TOS shows live). Offsets [1] on 5M series = previous 5m bar.
- ExpAverage seeded with the first value; RSI = Wilder; ATR = SMA(TrueRange,14); RVOL = vol / SMA(vol,50).
- AGG. BUY lane, labels and bubbles are not ported.
Usage: python -I study_port.py OUTDIR SYMBOL...  -> OUTDIR/study_<SYMBOL>.json"""
import json, sys, os, bisect, urllib.request, datetime as dt
LATCH = os.environ.get('LATCH') == '1'  # V14.4.13: 5M permission = the 5M study latch on completed 5m bars


ET = dt.timezone(dt.timedelta(hours=-5))  # all dates 2026-11-17..25 are US standard time
BASE = "http://localhost:53360/api/market-data/stored-bars"
P = dict(rsiLength=14, rsiBull=50, rsiStrong=55, rsiWeak=45, fast=9, slow=20, sma5=50, mf=12, ms=26, ml=9,
         rvolLength=50, minPermRSI=50, minSetupRVOL=1.00, permFailBars=2, majorFailBars=2, runnerWaningBars=2,
         breakoutLength=10, execWindowBars=2, candidateRearmBars=2, contLookback=4, contPauseReq=2,
         contMinRSI=50, contCooldown=3, minExecRSI=50, minExecRVOL=1.00, extDanger=2.00, eodHHMM=1555, holdMin=15)

def fetch(sym, tf, frm, to):
    url = f"{BASE}?symbol={sym}&fromUtc={frm}&toUtc={to}&provider=Alpaca&timeframe={tf}&limit=10000"
    with urllib.request.urlopen(url, timeout=120) as r:
        d = json.load(r)
    bars = {}
    for o in d["observations"]:
        b = o["bar"]
        if b["feed"].lower() != "iex": continue
        bars[b["timestamp"]] = b  # latest observation per timestamp (store returns latest)
    out = [dict(t=dt.datetime.fromisoformat(k), o=v["open"], h=v["high"], l=v["low"], c=v["close"], v=v["volume"])
           for k, v in bars.items()]
    return sorted(out, key=lambda x: x["t"])

# ---------- indicator helpers on python lists ----------
def ema(xs, n):
    a = 2 / (n + 1); out = []; e = None
    for x in xs:
        e = x if e is None else e + a * (x - e); out.append(e)
    return out

def sma(xs, n):
    out = []; s = 0.0
    for i, x in enumerate(xs):
        s += x
        if i >= n: s -= xs[i - n]
        out.append(s / min(i + 1, n))
    return out

def wilder_rsi(cl, n):
    out = [50.0]; ag = al = 0.0
    for i in range(1, len(cl)):
        ch = cl[i] - cl[i - 1]; g, l = max(ch, 0), max(-ch, 0)
        if i <= n:
            ag += g / n; al += l / n
        else:
            ag = (ag * (n - 1) + g) / n; al = (al * (n - 1) + l) / n
        out.append(50.0 if i < n else (100.0 if al == 0 else 100 - 100 / (1 + ag / al)))
    return out

def true_range(bars):
    out = []
    for i, b in enumerate(bars):
        pc = bars[i - 1]["c"] if i else b["c"]
        out.append(max(b["h"] - b["l"], abs(b["h"] - pc), abs(b["l"] - pc)))
    return out

def is_rth(t):
    e = t.astimezone(ET); m = e.hour * 60 + e.minute
    return e.weekday() < 5 and 570 <= m < 960

def five_series(closed5, dev):
    """Indicators for the 5m series whose last element is the developing bar `dev` (or None)."""
    bars = closed5 + ([dev] if dev else [])
    c = [b["c"] for b in bars]; h = [b["h"] for b in bars]; l = [b["l"] for b in bars]; v = [b["v"] for b in bars]
    e9, e20, s50 = ema(c, P["fast"]), ema(c, P["slow"]), sma(c, P["sma5"])
    rsi = wilder_rsi(c, P["rsiLength"])
    mv = [a - b for a, b in zip(ema(c, P["mf"]), ema(c, P["ms"]))]; ms = ema(mv, P["ml"]); mh = [a - b for a, b in zip(mv, ms)]
    av = sma(v, P["rvolLength"])
    return dict(c=c, h=h, l=l, o=[b["o"] for b in bars], e9=e9, e20=e20, s50=s50, rsi=rsi, mv=mv, ms=ms, mh=mh,
                rvol=[x / a if a > 0 else 0 for x, a in zip(v, av)])

def run(sym, out):
    one = fetch(sym, "1m", "2026-11-17T09:00:00Z", "2026-11-25T17:40:00Z")
    fiveStored = fetch(sym, "5m", "2026-10-20T09:00:00Z", "2026-11-25T17:40:00Z")
    c = [b["c"] for b in one]; hi = [b["h"] for b in one]; lo = [b["l"] for b in one]; op = [b["o"] for b in one]
    vol = [b["v"] for b in one]
    e9, e20 = ema(c, P["fast"]), ema(c, P["slow"]); rsi = wilder_rsi(c, P["rsiLength"])
    mv = [a - b for a, b in zip(ema(c, P["mf"]), ema(c, P["ms"]))]; ms = ema(mv, P["ml"]); mh = [a - b for a, b in zip(mv, ms)]
    atr = sma(true_range(one), 14); av = sma(vol, P["rvolLength"]); rvol = [x / a if a > 0 else 0 for x, a in zip(vol, av)]
    # regular-session VWAP (same as MCC)
    vw = []; day = None; pv = vv = 0.0
    for b in one:
        e = b["t"].astimezone(ET)
        if e.date() != day: day, pv, vv = e.date(), 0.0, 0.0
        if is_rth(b["t"]): pv += (b["h"] + b["l"] + b["c"]) / 3 * b["v"]; vv += b["v"]
        vw.append(pv / vv if vv > 0 else b["c"])

    # V14.4.13 latch inputs: completed stored 5m bars, Daily VWAP at each bar's last 1m bar
    f5c = [x["c"] for x in fiveStored]; L9, L20, L50 = ema(f5c, P["fast"]), ema(f5c, P["slow"]), sma(f5c, P["sma5"])
    LR = wilder_rsi(f5c, P["rsiLength"]); oneT = [x["t"] for x in one]
    def vwap_at_end(bt):
        j = bisect.bisect_left(oneT, bt + dt.timedelta(minutes=5)) - 1
        return vw[j] if j >= 0 and oneT[j] >= bt else None
    L = dict(j=0, perm=0, dmg=0, maj=0, day=None)
    # state carried bar to bar (thinkScript rec / [1] on 1M series)
    S = dict(fivePerm=0, prep=0, contArmed=0, contCool=P["contCooldown"], pauseCnt=0, everActive=0, execAge=-1)
    prev = {}
    rows = []
    for i, b in enumerate(one):
        t = b["t"]; e = t.astimezone(ET); hhmm = e.hour * 100 + e.minute
        # 5M: closed 5m bars strictly before the bucket, developing bucket from 1m bars up to this minute
        bucket = t - dt.timedelta(minutes=t.minute % 5, seconds=t.second)
        closed5 = [x for x in fiveStored if x["t"] < bucket][-400:]
        parts = [x for x in one[max(0, i - 4):i + 1] if x["t"] >= bucket]
        dev = dict(t=bucket, o=parts[0]["o"], h=max(p["h"] for p in parts), l=min(p["l"] for p in parts),
                   c=parts[-1]["c"], v=sum(p["v"] for p in parts))
        if len(closed5) < 60 or i < 60: continue
        F = five_series(closed5, dev); k = len(F["c"]) - 1
        def f(name, off=0): return F[name][k - off]
        price = c[i]
        eod = hhmm >= P["eodHHMM"]
        hold = is_rth(t) and (e.hour * 60 + e.minute) < 570 + P["holdMin"]
        aboveVWAP = price > vw[i]
        extATR = abs(price - vw[i]) / atr[i] if atr[i] > 0 else 0
        bullExtDanger = price > vw[i] and extATR >= P["extDanger"]
        # 5M reconstruction
        fBullCore = f("e9") > f("e20") and f("c") > f("e9")
        fBearStack = f("c") < f("e9") < f("e20") < f("s50")
        fBullStack = f("c") > f("e9") > f("e20") > f("s50")
        def dmg(o): return (F["c"][k-o] < F["e9"][k-o] and F["e9"][k-o] < F["e20"][k-o]) or \
                           (F["c"][k-o] < F["e20"][k-o] and F["e9"][k-o] < F["e20"][k-o] and not F["e9"][k-o] > F["e9"][k-o-1])
        def major(o): return F["c"][k-o] < F["e9"][k-o] < F["e20"][k-o] < F["s50"][k-o]
        dmgPersist = all(dmg(o) for o in range(P["permFailBars"]))
        majorPersist = all(major(o) for o in range(P["majorFailBars"]))
        bullPermRaw = fBullCore and f("rsi") >= P["minPermRSI"] and not fBearStack
        S["fivePerm"] = 0 if (eod or majorPersist or dmgPersist) else (1 if bullPermRaw else S["fivePerm"])
        fivePerm = S["fivePerm"] == 1
        reconPerm = fivePerm
        while L["j"] < len(fiveStored) and fiveStored[L["j"]]["t"] < bucket:
            j = L["j"]; L["j"] += 1; bt = fiveStored[j]["t"]; d5 = bt.astimezone(ET).date()
            if d5 != L["day"]: L.update(day=d5, perm=0, dmg=0, maj=0)
            v5 = vwap_at_end(bt)
            if v5 is None: continue  # no 1m bar inside this 5m bar: VWAP at its end unknown, bar skipped
            cl = f5c[j]
            L["dmg"] = L["dmg"] + 1 if (cl < v5 and L9[j] < L20[j]) else 0
            L["maj"] = L["maj"] + 1 if (cl < v5 and cl < L9[j] < L20[j] < L50[j]) else 0
            if L["maj"] >= P["majorFailBars"] or L["dmg"] >= P["permFailBars"]: L["perm"] = 0
            elif cl > v5 and cl > L9[j] and L9[j] >= L20[j] and LR[j] >= P["minPermRSI"]: L["perm"] = 1
        if e.date() != L["day"] or eod: L["perm"] = 0 if eod else L["perm"]
        latchPerm = L["perm"] == 1 and e.date() == L["day"]
        if LATCH: fivePerm = latchPerm
        rsiW = lambda o: F["rsi"][k-o] < F["rsi"][k-o-1] and F["rsi"][k-o-1] <= F["rsi"][k-o-2]
        macdW = lambda o: F["mh"][k-o] < F["mh"][k-o-1] and F["mh"][k-o-1] < F["mh"][k-o-2]
        def waningRaw(o):
            ctx = fivePerm and aboveVWAP and F["e9"][k-o] > F["e20"][k-o] and not majorPersist
            return ctx and (rsiW(o) or macdW(o) or
                            (F["c"][k-o] < F["e9"][k-o] and (F["rsi"][k-o] < P["rsiStrong"] or F["mh"][k-o] < F["mh"][k-o-1])) or
                            (F["c"][k-o] < F["c"][k-o-1] and not F["e9"][k-o] > F["e9"][k-o-1] and F["rsi"][k-o] < F["rsi"][k-o-1]))
        waningPersist = all(waningRaw(o) for o in range(P["runnerWaningBars"]))
        swingIntact = fivePerm and f("c") > f("e20") and f("e20") >= f("s50") and aboveVWAP
        swingMomW = (rsiW(0) and macdW(0)) or (f("rsi") < P["rsiStrong"] and f("mh") < f("mh", 1))
        swingZone = swingIntact and waningPersist and swingMomW and not majorPersist
        confWeak = f("c") < f("e9") < f("e20") and (f("rsi") < P["rsiWeak"] or f("mv") < f("ms"))
        fiveExecPerm = fivePerm and aboveVWAP and not majorPersist and not confWeak
        runnerStructure = fivePerm and fBullStack and aboveVWAP
        # 1M
        macdBull = mv[i] > ms[i]; macdBear = mv[i] < ms[i]
        rsiRising = rsi[i] > rsi[i-1]; macdRising = mh[i] > mh[i-1]; e20Rising = e20[i] > e20[i-1]; e9Rising = e9[i] > e9[i-1]
        rvolHealthy = rvol[i] >= 1.00
        priorHigh = max(hi[i-P["breakoutLength"]:i])
        fresh = (price > priorHigh or (price > vw[i] and c[i-1] <= vw[i-1]) or (price > e9[i] and c[i-1] <= e9[i-1]) or
                 (lo[i] <= e9[i] and price > e9[i] and price > op[i]) or (lo[i] <= vw[i] and price > vw[i] and price > op[i]))
        momAccel = (rsiRising and macdRising) or (rsiRising and rvolHealthy) or (macdRising and rvolHealthy)
        qualityCandle = price > op[i] and (hi[i] > lo[i] and (price - lo[i]) / (hi[i] - lo[i]) >= 0.60)
        execVolume = rvol[i] >= P["minExecRVOL"]
        candEnv = fiveExecPerm and not swingZone and aboveVWAP and price > e20[i] and e20Rising
        candMom = rsi[i] >= P["minExecRSI"] and macdBull and (price > e9[i] or price > c[i-1] or rsiRising or macdRising or rvolHealthy)
        cand = candEnv and candMom
        confRaw = cand and qualityCandle and (momAccel or price > hi[i-1] or rsi[i] >= P["rsiStrong"]) and not bullExtDanger
        pauseEvid = lambda j: c[j] <= c[j-1] or hi[j] <= hi[j-1] or not rsi[j] > rsi[j-1] or not mh[j] > mh[j-1]
        contPause = sum(1 for j in range(i - P["contLookback"] + 1, i + 1) if pauseEvid(j)) >= P["contPauseReq"]
        contResume = price > e9[i] and e9Rising and price > hi[i-1] and rsi[i] >= P["contMinRSI"] and macdBull and (rsiRising or macdRising or rvolHealthy)
        contBase = runnerStructure and fiveExecPerm and not swingZone
        armedPrev = S["contArmed"]
        S["contArmed"] = 0 if (eod or not contBase or contResume) else (1 if contPause else S["contArmed"])
        contRaw = contBase and armedPrev == 1 and contResume and execVolume and qualityCandle and not bullExtDanger
        coolPrev = S["contCool"]
        S["contCool"] = 0 if contRaw else (coolPrev + 1 if coolPrev >= 0 else P["contCooldown"])
        contQual = contRaw and coolPrev >= P["contCooldown"]
        pauseRaw = (not cand) and fiveExecPerm and aboveVWAP and price > e20[i]
        S["pauseCnt"] = S["pauseCnt"] + 1 if pauseRaw else 0
        everPrev = S["everActive"]; S["everActive"] = 1 if cand else S["everActive"]
        # candidateRearmReady[1] uses previous bar's value
        rearmNow = (not everPrev) or S["pauseCnt"] >= P["candidateRearmBars"]
        rearmPrev = prev.get("rearm", True)
        freshEvent = cand and fresh and fresh and not prev.get("fresh", False) and rearmPrev
        inTrendCand = cand and not fresh
        inTrendEvent = inTrendCand and not prev.get("inTrendCand", False) and rearmPrev
        contEvent = contQual and not prev.get("contQual", False)
        confEvent = confRaw and not prev.get("confRaw", False)
        opp = freshEvent or inTrendEvent or contEvent or confEvent
        newTrig = opp and not prev.get("opp", False)
        a = S["execAge"]
        S["execAge"] = 0 if newTrig else (a + 1 if 0 <= a < P["execWindowBars"] else -1)
        winActive = 0 <= S["execAge"] < P["execWindowBars"]
        execReady = winActive and fiveExecPerm and not bullExtDanger
        invalid = (not fiveExecPerm) or price < e20[i] or price < vw[i] or rsi[i] < P["rsiWeak"] or macdBear or bullExtDanger
        readyBeforeHold = execReady and not invalid
        final = readyBeforeHold and not hold
        prev = dict(fresh=fresh, inTrendCand=inTrendCand, contQual=contQual, confRaw=confRaw, opp=opp, rearm=rearmNow)
        if is_rth(t) and t >= dt.datetime(2026, 11, 24, 14, 30, tzinfo=dt.timezone.utc):
            rows.append(dict(latchPerm=latchPerm, reconPerm=reconPerm, barUtc=t.strftime("%Y-%m-%dT%H:%M:%SZ"), close=price, vwap=vw[i], ext=extATR, rsi=rsi[i], rvol=rvol[i],
                             fivePerm=fivePerm, fiveExecPerm=fiveExecPerm, cand=cand, trigger=("F" if freshEvent else "") +
                             ("T" if inTrendEvent else "") + ("C" if contEvent else "") + ("K" if confEvent else ""),
                             age=S["execAge"], invalid=invalid, hold=hold, ready=final, readyBeforeHold=readyBeforeHold,
                             extDanger=bullExtDanger, macdBull=macdBull))
    json.dump(rows, open(f"{out}/study{'_latch' if LATCH else ''}_{sym}.json", "w"))
    print(sym, len(rows), "RTH bars; study READY:", sum(r["ready"] for r in rows), flush=True)

for s in sys.argv[2:]:
    run(s, sys.argv[1])
