/// MODULE: Live Decision Workbench
/// PURPOSE: Renders the focused symbol from the live assessment API; no synthetic snapshots are used.
const $=id=>document.getElementById(id);
const feedbackKey="mcc.dashboardFeedback.v1";

/// MODULE: Exchange session countdown
/// PURPOSE: Render the API's calendar-backed next session transition in the header.
/// SAFETY: If the API/calendar is unavailable, show DATA? rather than a guessed countdown.
let marketSessionTargetUtc = null;
let overnightEvaluationTargetUtc = null;
let overnightBuyDeadlineUtc = null;
let overnightEvaluationOpen = false;
let marketSessionRefreshInFlight = false;
async function refreshMarketSessionClock(){
 if(marketSessionRefreshInFlight)return;
 marketSessionRefreshInFlight=true;
 try{
  const response=await fetch("/api/market-session/clock",{cache:"no-store",headers:{Accept:"application/json"}});
  const data=await response.json();
  if(!response.ok)throw new Error(data.detail||data.error||("HTTP "+response.status));
  const target=new Date(data.targetUtc);
  if(Number.isNaN(target.getTime()))throw new Error("Invalid market-session target timestamp.");
  marketSessionTargetUtc=target.getTime();
  const evaluationTarget=new Date(data.overnightEvaluationStartUtc);
  const buyDeadline=new Date(data.overnightBuyDeadlineUtc);
  if(Number.isNaN(evaluationTarget.getTime())||Number.isNaN(buyDeadline.getTime()))throw new Error("Invalid Overnight process schedule.");
  overnightEvaluationTargetUtc=evaluationTarget.getTime();
  overnightBuyDeadlineUtc=buyDeadline.getTime();
  overnightEvaluationOpen=data.overnightEvaluationOpen===true;
  const label=$("marketSessionLabel"),detail=$("marketSessionDetail");
  if(label)label.textContent=data.label||"Until Market opens";
  if(detail)detail.textContent=(data.exchangeTimeZone||"America/New_York")+" · "+(data.targetEvent||"NEXT SESSION TRANSITION")+" · "+(data.calendarVersion||"CALENDAR UNKNOWN");
  renderMarketSessionCountdown();
 }catch(error){
  marketSessionTargetUtc=null;
  if($("marketSessionLabel"))$("marketSessionLabel").textContent="Market session clock";
  if($("marketSessionCountdown"))$("marketSessionCountdown").textContent="DATA?";
  overnightEvaluationTargetUtc=null;overnightBuyDeadlineUtc=null;overnightEvaluationOpen=false;
  if($("overnightEvaluationCountdown"))$("overnightEvaluationCountdown").textContent="DATA?";
  if($("overnightBuyCountdown"))$("overnightBuyCountdown").textContent="DATA?";
  if($("marketSessionDetail"))$("marketSessionDetail").textContent="Unavailable · "+error.message;
 }finally{marketSessionRefreshInFlight=false;}
}
function renderMarketSessionCountdown(){
 const node=$("marketSessionCountdown");
 if(!node||marketSessionTargetUtc===null)return;
 const remaining=Math.max(0,Math.ceil((marketSessionTargetUtc-Date.now())/1000));
 const days=Math.floor(remaining/86400),hours=Math.floor((remaining%86400)/3600),minutes=Math.floor((remaining%3600)/60),seconds=remaining%60;
 const pad=value=>String(value).padStart(2,"0");
 node.textContent=(days?days+"d ":"")+pad(hours)+"h "+pad(minutes)+"m "+pad(seconds)+"s";
 if(remaining===0)void refreshMarketSessionClock();
 const formatCountdown=target=>{const seconds=Math.max(0,Math.ceil((target-Date.now())/1000));const d=Math.floor(seconds/86400),h=Math.floor((seconds%86400)/3600),m=Math.floor((seconds%3600)/60),s=seconds%60;return (d?d+"d ":"")+pad(h)+"h "+pad(m)+"m "+pad(s)+"s";};
 const evaluationLabel=$("overnightEvaluationLabel"),evaluationCountdown=$("overnightEvaluationCountdown"),buyCountdown=$("overnightBuyCountdown");
 const now=Date.now(),evaluationWindowOpen=overnightEvaluationTargetUtc!==null&&overnightBuyDeadlineUtc!==null&&now>=overnightEvaluationTargetUtc&&now<overnightBuyDeadlineUtc;
 if(evaluationLabel)evaluationLabel.textContent=evaluationWindowOpen?"Overnight candidate evaluation · OPEN":"Overnight candidate evaluation begins in";
 if(evaluationCountdown)evaluationCountdown.textContent=overnightEvaluationTargetUtc===null?"DATA?":(evaluationWindowOpen?"OPEN":formatCountdown(overnightEvaluationTargetUtc));
 if(buyCountdown)buyCountdown.textContent=overnightBuyDeadlineUtc===null?"DATA?":formatCountdown(overnightBuyDeadlineUtc);
}
void refreshMarketSessionClock();
window.setInterval(renderMarketSessionCountdown,1000);
window.setInterval(()=>{if(document.visibilityState==="visible")void refreshMarketSessionClock();},60000);


/// PURPOSE: Track dynamic WebSocket subscription requests before any startup refresh can run.
const requestedMinuteBarSymbols=new Set();
function render(){
 const overnight=$("mode")?.value==="overnight";
 $("overnightPanel")?.classList.toggle("hidden",!overnight);
 const eyebrow=document.querySelector(".hero-card .eyebrow");
 if(eyebrow)eyebrow.textContent=overnight?"BORING OVERNIGHT · RESEARCH":"LIVE OPPORTUNITY CLASSIFICATION";
 void refreshDecisionExplanation();
}
$("mode")?.addEventListener("change",render);
$("symbol")?.addEventListener("change",()=>{render();void refreshFocusedPriceChart();});
$("reset")?.addEventListener("click",()=>{if($("mode"))$("mode").value="momentum";render();void refreshFocusedPriceChart();});
render();
void refreshFocusedPriceChart();
window.setInterval(()=>{if(document.visibilityState==="visible"){void refreshRedZoneAssessments();void refreshFocusedPriceChart();void refreshDecisionExplanation();void loadMinuteBarStreamStatus();}},15000);


/// MODULE: Quick Start guide
/// PURPOSE: Opens the user-facing workflow guide without navigating away from the dashboard.
const quickStartDialog=$("quickStartDialog");
function openQuickStart(){if(quickStartDialog&&!quickStartDialog.open)quickStartDialog.showModal();}
$("openQuickStart")?.addEventListener("click",openQuickStart);
$("openQuickStartFooter")?.addEventListener("click",openQuickStart);
$("closeQuickStart")?.addEventListener("click",()=>quickStartDialog?.close());


const scannerData={
 momentum:[
  {symbol:"XRPN",score:86,status:"WATCH",reason:"Participation and setup require chart confirmation."},
  {symbol:"MNSB",score:78,status:"WATCH",reason:"Developing momentum; no active execution event in this demo."},
  {symbol:"DEMO",score:64,status:"BLOCKED",reason:"Illustrative candidate; required data is not loaded."}
 ],
 breakout:[
  {symbol:"MNSB",score:81,status:"WATCH",reason:"Breakout candidate; resistance and volume are unverified."},
  {symbol:"XRPN",score:73,status:"BLOCKED",reason:"Do not rank without a fresh chart snapshot."}
 ],
 reclaim:[
  {symbol:"XRPN",score:79,status:"WATCH",reason:"Reclaim candidate; 5M permission must be checked."},
  {symbol:"DEMO",score:58,status:"DATA?",reason:"Missing source data."}
 ],
 discovery:[
  {symbol:"MNSB",score:70,status:"WATCH",reason:"Discovery only; not an overnight qualification."},
  {symbol:"XRPN",score:68,status:"WATCH",reason:"Discovery only; catalyst and tradability are unverified."}
 ]
};

/// MODULE STATE: Declare Red Zone state before startup refreshes or restored imports can render it.
let redZoneAssessmentRefreshers=[];
let redZoneSoundEnabled=false,redZoneAudioContext=null,previousRedZoneSymbols=null,suppressNextRedZoneAlert=false;

let restoredScannerImport=null;
try{
 const saved=JSON.parse(localStorage.getItem("mcc.scannerImport.v1")||"null");
 if(saved&&Array.isArray(saved.rows)&&saved.rows.every(row=>row&&typeof row.symbol==="string"))restoredScannerImport=saved;
}catch{restoredScannerImport=null;}

async function refreshLiveMovers(selectLive=true,quiet=false){
 const button=$("refreshMarketMovers"),status=$("liveMoversStatus");
 if(button&&button.disabled&&!quiet)return;
 if(button&&!quiet)button.disabled=true;
 if(status&&!quiet)status.textContent="Loading persisted Alpaca discovery events…";
 try{
  const response=await fetch("/api/scanner/discoveries?limit=200",{cache:"no-store",headers:{Accept:"application/json"}});
  const data=await response.json();
  if(!response.ok)throw new Error(data.error||("HTTP "+response.status));
  const seen=new Set();
  scannerData.live=(data.discoveries||[]).map(envelope=>{
   const item=envelope.discovery||{},fields=item.rawFields||{},ticker=String(item.symbol||"").toUpperCase();
   if(!ticker||seen.has(ticker))return null;seen.add(ticker);
   const pct=fields.percent_change==null?null:Number(fields.percent_change);
   const move=pct===null||!Number.isFinite(pct)?"DATA?":(pct>=0?"+":"")+pct.toFixed(2)+"%";
   const groupCode=item.scannerGroup==="ALPACA_TOP_GAINERS"?"ATG¹":item.scannerGroup==="ALPACA_TOP_LOSERS"?"ATL¹":(item.scannerGroup||"DISCOVERY");
   const sourceDate=item.sourceTimestamp?new Date(item.sourceTimestamp):null;
   const sourceLocal=sourceDate&&!Number.isNaN(sourceDate.getTime())?sourceDate.toLocaleString("en-US",{timeZone:"America/Phoenix",hour12:true})+" Arizona":"UNKNOWN";
   const received=envelope.receivedAt?new Date(envelope.receivedAt):null;
   const age=received&&!Number.isNaN(received.getTime())?Math.max(0,Math.round((Date.now()-received.getTime())/60000))+" min since receipt":"UNKNOWN";
   return {symbol:ticker,score:pct,scoreLabel:move,status:"DISCOVERY — NOT QUALIFIED",extension:"NOT CALCULATED",freshness:age,
    reason:groupCode+" · change "+move+" · source "+sourceLocal};
  }).filter(Boolean);
  let option=document.querySelector('#scannerGroup option[value="live"]');
  if(!option){option=document.createElement("option");option.value="live";option.textContent="E · Alpaca live discovery";$("scannerGroup").append(option);}
  if(selectLive)$("scannerGroup").value="live";
  const health=data.health||{};
  if(status)status.textContent="Alpaca · "+(health.status||"UNKNOWN")+" · "+(data.count??0)+" persisted events · "+(data.configured?"credentials configured":"credentials missing")+" · source is SIP-based"+(quiet?" · auto-refresh 30s":"");
  if($("scannerGroup")?.value==="live"){renderScanner();renderMorningReport();}
 }catch(error){if(status)status.textContent="Market-mover retrieval failed: "+error.message;}
 finally{if(button&&!quiet)button.disabled=false;}
}
$("refreshMarketMovers")?.addEventListener("click",()=>void refreshLiveMovers(true,false));
window.setInterval(()=>{if(document.visibilityState==="visible"&&$("scannerGroup")?.value==="live")void refreshLiveMovers(false,true);},30000);




/// MODULE: API-backed focus charts
/// PURPOSE: Draw real close-price lines for the focused symbol and Red Zone candidates.
/// DATA RULE: An empty/failed request is displayed as unavailable, never as a flat/zero line.
const svgNs="http://www.w3.org/2000/svg";
async function fetchPriceSeries(symbol,minutes=180){
 const to=new Date(),from=new Date(to.getTime()-minutes*60000);
 const params=new URLSearchParams({symbol,timeframe:"1m",from:from.toISOString(),to:to.toISOString(),providerName:"Alpaca"});
 const response=await fetch("/api/market-data/bars?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
 const body=await response.json();if(!response.ok)throw new Error(body.detail||body.error||("HTTP "+response.status));
 return (Array.isArray(body.bars)?body.bars:[]).filter(b=>Number.isFinite(Number(b.close))&&Number.isFinite(new Date(b.timestamp).getTime())).sort((a,b)=>new Date(a.timestamp)-new Date(b.timestamp));
}
function drawPriceLine(container,bars,label){
 container.replaceChildren();
 if(!bars.length){const p=document.createElement("p");p.className="chart-unavailable";p.textContent=label+" · price history unavailable (no returned bars)";container.append(p);return;}
 const width=600,height=150,pad=12,values=bars.map(b=>Number(b.close)),min=Math.min(...values),max=Math.max(...values),span=max-min||Math.max(Math.abs(max)*0.001,0.01);
 const points=values.map((v,i)=>{const x=pad+i/(Math.max(1,values.length-1))*(width-2*pad),y=height-pad-(v-min)/span*(height-2*pad);return x.toFixed(1)+","+y.toFixed(1);}).join(" ");
 const svg=document.createElementNS(svgNs,"svg");svg.setAttribute("viewBox","0 0 "+width+" "+height);svg.setAttribute("role","img");svg.setAttribute("aria-label",label+" close price line");
 const line=document.createElementNS(svgNs,"polyline");line.setAttribute("points",points);line.setAttribute("fill","none");line.setAttribute("stroke","currentColor");line.setAttribute("stroke-width","2.5");line.setAttribute("stroke-linecap","round");line.setAttribute("stroke-linejoin","round");
 svg.append(line);
 const meta=document.createElement("div");meta.className="chart-meta";meta.textContent=bars.length+" × 1m bars · "+new Date(bars[0].timestamp).toLocaleTimeString()+"–"+new Date(bars[bars.length-1].timestamp).toLocaleTimeString()+" · latest $"+values[values.length-1].toFixed(2);
 container.append(svg,meta);
}
const liveAssessmentInFlight=new Map();
async function fetchLiveAssessment(symbol){
 const ticker=String(symbol||"").trim().toUpperCase();
 if(liveAssessmentInFlight.has(ticker))return liveAssessmentInFlight.get(ticker);
 const request=(async()=>{
  const params=new URLSearchParams({symbol:ticker,providerName:"Alpaca"});
  const response=await fetch("/api/market-data/live-assessment?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
  const body=await response.json().catch(()=>({}));
  if(!response.ok)throw new Error(body.detail||body.error||("HTTP "+response.status));
  return body;
 })();
 liveAssessmentInFlight.set(ticker,request);
 try{return await request;}finally{if(liveAssessmentInFlight.get(ticker)===request)liveAssessmentInFlight.delete(ticker);}
}
function sourceFreshness(label,ageSeconds){
 if(!label)return "UNKNOWN";
 return label+(ageSeconds!==null&&ageSeconds!==undefined&&Number.isFinite(Number(ageSeconds))?" · "+Math.max(0,Number(ageSeconds)).toFixed(0)+"s":"");
}
function humanizeSignalState(value){return String(value||"UNKNOWN").replace(/([a-z])([A-Z])/g,"$1 $2").replaceAll("_"," ").toUpperCase();}
function formatMetric(value,digits=2,suffix=""){return value===null||value===undefined||!Number.isFinite(Number(value))?"UNKNOWN":Number(value).toFixed(digits)+suffix;}
function setV2ScoreDisplay(node,scoreBreakdown,decision,evaluationStatus){
 if(!node)return;
 const rawScore=scoreBreakdown?.score;
 if(rawScore===null||rawScore===undefined||!Number.isFinite(Number(rawScore))||scoreBreakdown?.status!=="CURRENT"){
  node.className="redzone-score score-unknown";node.textContent="V2 5M score: UNKNOWN · "+(evaluationStatus||"NOT EVALUATED");return;
 }
 const score=Number(rawScore),entryReady=decision?.entryAllowed===true;
 const band=score<=2?"score-low":score<=4?"score-mid":"score-high";
 node.className="redzone-score "+band;
 const marker=score<=2?"✕ ":score>=5?"✓ ":"";
 const suffix=entryReady?" · EXECUTION READY":score>=5?(decision?" · 5M PERMISSION; CHECK 1M STATE":" · 5M PERMISSION; 1M NOT EVALUATED"):score<=2?" · LOW ALIGNMENT":" · PARTIAL ALIGNMENT";
 node.textContent=marker+"V2 5M score: "+score+"/6"+suffix;
}
let decisionRefreshSequence=0;
async function refreshDecisionExplanation(){
 const requestId=++decisionRefreshSequence;
 const symbol=($("symbol")?.value||"").trim().toUpperCase();
 const classification=$("classification"),headline=$("headline"),price=$("price"),dailyVwap=$("dailyVwap"),atr14=$("atr14"),rvol=$("rvol"),extension=$("extension"),eligibility=$("eligibility"),freshness=$("freshness"),reasons=$("reasons"),fiveState=$("fiveState"),oneState=$("oneState"),scoreNode=$("decisionScore"),sourceNode=$("decisionSource");
 if(!symbol||!classification)return;
 classification.textContent="UPDATING TO "+symbol;classification.className="classification watch";
 if(headline)headline.textContent="Updating to "+symbol+"… requesting current completed 1M and 5M bars.";
 try{
  const data=await fetchLiveAssessment(symbol);
  if(requestId!==decisionRefreshSequence)return;
  const one=data.oneMinute||{},five=data.fiveMinute||{},decision=data.decision||null;
  const transportLabel=$("marketTransportLabel");
  if(transportLabel)transportLabel.textContent=one.sourceTransport==="ALPACA_WEBSOCKET_LATEST_BAR"?"ALPACA 1M WS / 5M REST · 15s UI REFRESH":"ALPACA REST · 15s UI REFRESH";
  const fiveMinutePermission=five.scoreBreakdown?.permission??decision?.fiveMinutePermission,permission=fiveMinutePermission===true?"YES":fiveMinutePermission===false?"NO":"UNKNOWN",execution=decision?.oneMinuteExecution||"UNKNOWN",ready=decision?.entryAllowed===true;
  classification.textContent=ready?"EXECUTION READY":decision?humanizeSignalState(decision.state):humanizeSignalState(data.evaluationStatus||"DATA UNAVAILABLE");
  classification.className="classification "+(ready?"high":fiveMinutePermission?"boring":"watch");
  if(headline)headline.textContent=decision?.reason||data.evaluationReason||"No decision reason returned.";
  if(price)price.textContent=one.close==null?"DATA?":"$"+Number(one.close).toFixed(5);
  const oneMinuteTimestamp=$("oneMinuteTimestamp");
  if(oneMinuteTimestamp){
   const sourceTime=one.sourceTimestampUtc?new Date(one.sourceTimestampUtc):null;
   const completedTime=one.completedAtUtc?new Date(one.completedAtUtc):null;
   const validTime=value=>value&&!Number.isNaN(value.getTime());
   const azTime=value=>value.toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"2-digit",minute:"2-digit",second:"2-digit",hour12:true});
   oneMinuteTimestamp.textContent=validTime(sourceTime)&&validTime(completedTime)
    ?"Closed "+azTime(completedTime)+" AZ · bar "+azTime(sourceTime)+"–"+azTime(completedTime)+" AZ · "+completedTime.toISOString().slice(11,19)+"Z"
    :validTime(sourceTime)?"Bar start "+azTime(sourceTime)+" AZ · completion timestamp unavailable":"Source timestamp unavailable";
   oneMinuteTimestamp.title="The candle timestamp is its start time. MCC intentionally evaluates the last completed 1-minute bar; the current in-progress minute is excluded.";
  }
  if(dailyVwap)dailyVwap.textContent=one.vwap==null?"DATA?":"$"+Number(one.vwap).toFixed(5);
  if(atr14)atr14.textContent=one.atr==null?"DATA?":"$"+Number(one.atr).toFixed(5);
  if(rvol)rvol.textContent=five.relativeVolume==null?"UNKNOWN":Number(five.relativeVolume).toFixed(2)+"×";
  if(extension)extension.textContent=one.extensionAtr==null?"DATA?":Number(one.extensionAtr).toFixed(2)+" ATR";
  if(eligibility)eligibility.textContent="5M permission: "+permission;
  if(freshness)freshness.textContent="1M "+sourceFreshness(one.freshness,one.ageSeconds)+" · 5M "+sourceFreshness(five.freshness,five.ageSeconds);
  if(fiveState)fiveState.textContent="5M permission: "+permission+(decision?.fiveMinuteStructureBull===true?" · bullish structure":decision?.fiveMinuteStructureBull===false?" · structure not bullish":"");
  if(oneState)oneState.textContent="1M execution: "+execution+(ready?" · entry gate passed":"");
  setV2ScoreDisplay(scoreNode,five.scoreBreakdown,decision,data.evaluationStatus);
  const lines=[decision?.reason?"Decision: "+decision.reason:"Decision unavailable: "+(data.evaluationReason||data.evaluationStatus||"No evaluation details returned.")];
  for(const criterion of (five.scoreBreakdown?.criteria||[])){const mark=criterion.passed===true?"✓":criterion.passed===false?"✕":"?";lines.push(mark+" "+criterion.label+(criterion.passed===null?" — unavailable":""));}
  if(five.relativeVolumeBaselineSamples!==undefined)lines.push("5M same-time RVOL baseline: "+five.relativeVolumeBaselineSamples+" prior-session samples; minimum 5.");
  if(one.sourceTimestampUtc)lines.push("1M source bar: "+new Date(one.sourceTimestampUtc).toLocaleString());
  if(five.sourceTimestampUtc)lines.push("5M source bar: "+new Date(five.sourceTimestampUtc).toLocaleString());
  if(one.close!=null&&one.vwap!=null&&one.atr!=null)lines.push("1M extension inputs: close $"+Number(one.close).toFixed(5)+" · Daily VWAP $"+Number(one.vwap).toFixed(5)+" · ATR14 $"+Number(one.atr).toFixed(5)+" · extension "+formatMetric(one.extensionAtr,2)+" ATR.");
  if(one.ema9!=null&&one.ema20!=null)lines.push("1M EMA9 "+formatMetric(one.ema9)+" · EMA20 "+formatMetric(one.ema20)+" · RSI "+formatMetric(one.rsi,1));
  if(reasons)reasons.replaceChildren(...lines.map(line=>{const li=document.createElement("li");li.textContent=line;return li;}));
  if(sourceNode)sourceNode.textContent="Source "+(data.provider||"UNKNOWN")+" · configured "+(data.configuredFeed||"feed UNKNOWN")+" · observed 1M "+(one.feed||data.feed||"UNKNOWN")+"/5M "+(five.feed||data.feed||"UNKNOWN")+" · 1M "+(one.sourceTransport||"UNKNOWN")+" · 5M REST · "+(data.evaluationStatus||"UNKNOWN")+" · evaluated "+(data.evaluatedAtUtc?new Date(data.evaluatedAtUtc).toLocaleTimeString():"UNKNOWN");
 }catch(error){
  classification.textContent="DATA UNAVAILABLE";classification.className="classification watch";
  if(headline)headline.textContent="Live assessment request failed: "+error.message;
  if(price)price.textContent="DATA?";if($("oneMinuteTimestamp"))$("oneMinuteTimestamp").textContent="Source timestamp unavailable";if(dailyVwap)dailyVwap.textContent="DATA?";if(atr14)atr14.textContent="DATA?";if(rvol)rvol.textContent="UNKNOWN";if(extension)extension.textContent="DATA?";
  if(eligibility)eligibility.textContent="5M permission: UNKNOWN";if(freshness)freshness.textContent="Freshness: UNKNOWN";
  if(fiveState)fiveState.textContent="5M permission: UNKNOWN";if(oneState)oneState.textContent="1M execution: UNKNOWN";
  setV2ScoreDisplay(scoreNode,null,null,"REQUEST FAILED");
  if(reasons)reasons.replaceChildren(Object.assign(document.createElement("li"),{textContent:error.message}));
  if(sourceNode)sourceNode.textContent="No live assessment received.";
 }
}
async function refreshFocusedPriceChart(){
 const host=$("focusPriceChart");if(!host)return;
 const symbol=($("symbol")?.value||"").trim().toUpperCase();void ensureMinuteBarSubscription(symbol);host.replaceChildren();
 const symbolBadge=$("mainChartSymbol");if(symbolBadge)symbolBadge.textContent=symbol||"NO SYMBOL";
 const title=document.createElement("div");title.className="chart-title";title.textContent=symbol+" · 1-minute OHLCV close · auto-refresh 15s";host.append(title);
 try{
  const data=await fetchLiveAssessment(symbol);
  drawPriceLine(host,data.oneMinute?.chartBars||[],symbol);
  const decision=data.decision||{};
  const detail=document.createElement("p");detail.className="chart-meta";
  detail.textContent="Source "+(data.provider||"UNKNOWN")+"/"+(data.feed||"feed unknown")+
   " · 1M "+sourceFreshness(data.oneMinute?.freshness,data.oneMinute?.ageSeconds)+
   " · 5M "+sourceFreshness(data.fiveMinute?.freshness,data.fiveMinute?.ageSeconds)+
   " · 1M transport "+(data.oneMinute?.sourceTransport||"UNKNOWN")+" · 5M transport REST"+
   " · 5M permission "+(decision.fiveMinutePermission===true?"YES":decision.fiveMinutePermission===false?"NO":"UNKNOWN")+
   " · 1M execution "+(decision.oneMinuteExecution||data.evaluationStatus||"UNKNOWN")+
   " · evaluated "+(data.evaluatedAtUtc||"UNKNOWN");
  host.append(detail);
  if(data.evaluationReason&& !data.decision){const reason=document.createElement("p");reason.className="chart-meta";reason.textContent=data.evaluationReason;host.append(reason);}
 }catch(e){const p=document.createElement("p");p.className="chart-unavailable";p.textContent="Live assessment unavailable · "+e.message;host.append(p);}
}

function localDateTimeValue(date){
 const pad=value=>String(value).padStart(2,"0");
 return date.getFullYear()+"-"+pad(date.getMonth()+1)+"-"+pad(date.getDate())+"T"+pad(date.getHours())+":"+pad(date.getMinutes());
}
if($("apiScanFrom")&&$("apiScanTo")){
 const end=new Date(),start=new Date(end.getTime()-6*60*60*1000);
 $("apiScanFrom").value=localDateTimeValue(start);$("apiScanTo").value=localDateTimeValue(end);
}
$("apiScannerForm")?.addEventListener("submit",async event=>{
 event.preventDefault();
 const button=$("apiScanRun"),source=$("scannerSource");
 const symbols=$("apiScanSymbols").value.trim().split(/[\s,;]+/).filter(Boolean);
 if(!symbols.length||symbols.length>50){source.textContent="Enter between 1 and 50 symbols.";return;}
 const from=new Date($("apiScanFrom").value),to=new Date($("apiScanTo").value);
 if(!Number.isFinite(from.getTime())||!Number.isFinite(to.getTime())||from>=to){source.textContent="Invalid scan window; From must precede To.";return;}
 button.disabled=true;source.textContent="Requesting historical bars from MCC API…";
 try{
  const params=new URLSearchParams({symbols:symbols.join(","),timeframe:$("apiScanTimeframe").value,from:from.toISOString(),to:to.toISOString(),providerName:"Alpaca"});
  const response=await fetch("/api/scanner/universe?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
  const data=await response.json();
  if(!response.ok)throw new Error(data.error||("HTTP "+response.status));
  scannerData.api=(data.results||[]).map(item=>({
   symbol:item.symbol,
   score:item.changePercent==null?"—":(item.changePercent>=0?"+":"")+item.changePercent.toFixed(2)+"%",
   status:item.status,
   extension:item.observedRangePercent==null?"DATA?":item.observedRangePercent.toFixed(2)+"% range",
   freshness:item.latestBarAgeMinutes==null?"UNKNOWN":item.latestBarAgeMinutes+" min old",
   reason:item.barCount+" bars · volume "+(item.summedVolume??"DATA?")+" · "+item.interpretation
  }));
  let option=document.querySelector('#scannerGroup option[value="api"]');
  if(!option){option=document.createElement("option");option.value="api";option.textContent="API historical scan";$("scannerGroup").append(option);}
  $("scannerGroup").value="api";
  source.textContent="API scan · "+data.scannedSymbolCount+" supplied symbols · "+data.provider+" · "+data.timeframe+" · "+new Date(data.generatedAtUtc).toLocaleTimeString();
  renderScanner();renderMorningReport();
 }catch(error){source.textContent="API scan failed: "+error.message+" · confirm this dashboard is served by MCC API.";}
 finally{button.disabled=false;}
});

function renderScanner(){
 const rows=$("scannerRows"),group=$("scannerGroup").value;
 rows.replaceChildren(...(scannerData[group]||[]).map(candidate=>{
  const tr=document.createElement("tr");
  const statusClass=candidate.status==="BLOCKED"?"status-blocked":candidate.status==="WATCH"?"status-watch":"status-ready";
  const cell=value=>{const td=document.createElement("td");td.textContent=value??"DATA?";return td;};
  tr.append(cell(candidate.symbol),cell(candidate.scoreLabel??candidate.score),cell(candidate.status),cell(candidate.extension||"DATA?"),cell(candidate.freshness||"UNKNOWN"),cell(candidate.reason),document.createElement("td"));
  const button=document.createElement("button");button.className="row-action";button.textContent="Watch";button.type="button";button.addEventListener("click",()=>addWatch(candidate.symbol));tr.lastElementChild.append(button);return tr;
 }));
}

/// MODULE: Boring Overnight qualification
/// PURPOSE: Evaluate imported historical gates and display source-backed Daily/catalyst evidence for a bounded top-four review set.
/// IMPORTANT: Provisional catalyst triage never clears a candidate; no READY state is possible from this panel.
const overnightLiveEvidence = new Map();
const overnightShortlistStorageKey="mcc.overnightShortlists.v1";
let overnightShortlistMode="mcc",overnightManualSymbols=[];
try{const saved=JSON.parse(localStorage.getItem(overnightShortlistStorageKey)||"null");if(saved&&Array.isArray(saved.manualSymbols))overnightManualSymbols=[...new Set(saved.manualSymbols.map(x=>String(x).trim().toUpperCase()).filter(Boolean))].slice(0,4);if(saved?.mode==="manual")overnightShortlistMode="manual";}catch{}
function saveOvernightShortlists(){try{localStorage.setItem(overnightShortlistStorageKey,JSON.stringify({mode:overnightShortlistMode,manualSymbols:overnightManualSymbols}));}catch{}}
function renderOvernightShortlistPicker(rows){
 const picker=$("overnightManualTopFourPicker"),summary=$("overnightShortlistSummary");if(!picker)return;
 const symbols=[...new Set(rows.map(x=>String(x.symbol||"").trim().toUpperCase()).filter(Boolean))];overnightManualSymbols=overnightManualSymbols.filter(x=>symbols.includes(x)).slice(0,4);picker.replaceChildren();
 const ranked=rows.slice().sort((a,b)=>(Number(String(b.score??"").replace(/[^0-9.-]/g,""))||-Infinity)-(Number(String(a.score??"").replace(/[^0-9.-]/g,""))||-Infinity)||String(a.symbol).localeCompare(String(b.symbol))).slice(0,4).map(x=>String(x.symbol||"").toUpperCase());
 const paint=(id,items,empty)=>{const host=$(id);if(!host)return;host.replaceChildren();(items.length?items:[]).forEach((symbol,index)=>{const tag=document.createElement("span");tag.className="overnight-shortlist-symbol";tag.textContent=(index+1)+". "+symbol;host.append(tag);});if(!items.length){const note=document.createElement("span");note.className="muted small";note.textContent=empty;host.append(note);}};
 paint("overnightMccTopFourDisplay",ranked,"No imported rows");paint("overnightMyTopFourDisplay",overnightManualSymbols,"Select four symbols below");
 symbols.forEach(symbol=>{const label=document.createElement("label");label.className="overnight-shortlist-option";const input=document.createElement("input");input.type="checkbox";input.value=symbol;input.checked=overnightManualSymbols.includes(symbol);input.disabled=!input.checked&&overnightManualSymbols.length>=4;input.addEventListener("change",()=>{if(input.checked){if(overnightManualSymbols.length>=4){input.checked=false;return;}overnightManualSymbols.push(symbol);}else overnightManualSymbols=overnightManualSymbols.filter(x=>x!==symbol);saveOvernightShortlists();renderOvernightShortlistPicker(rows);updateOvernightShortlistMode();renderCatalystHumanReviewQueue();});const name=document.createElement("span");name.textContent=symbol;label.append(input,name);picker.append(label);});
 if(summary)summary.textContent=overnightManualSymbols.length+"/4 selected"+(overnightManualSymbols.length===4?"":" · select exactly four");
 const manual=$("overnightShortlistManual");if(manual)manual.disabled=overnightManualSymbols.length!==4;
}
function updateOvernightShortlistMode(){
 const mcc=$("overnightShortlistMcc"),manual=$("overnightShortlistManual"),button=$("overnightQualRefreshMarket"),active=$("overnightShortlistActive");
 if(mcc){mcc.setAttribute("aria-pressed",String(overnightShortlistMode==="mcc"));mcc.classList.toggle("active",overnightShortlistMode==="mcc");}
 if(manual){manual.setAttribute("aria-pressed",String(overnightShortlistMode==="manual"));manual.classList.toggle("active",overnightShortlistMode==="manual");manual.disabled=overnightManualSymbols.length!==4;}
 if(button)button.textContent=overnightShortlistMode==="mcc"?"Fetch Daily + catalyst for MCC Top 4":"Fetch Daily + catalyst for My Top 4";
 if(active)active.textContent=overnightShortlistMode==="mcc"?"Active group: MCC Top 4 · highest imported scanner scores":"Active group: My Top 4 · "+(overnightManualSymbols.length===4?overnightManualSymbols.join(", "):"select four symbols");
}
$("overnightShortlistMcc")?.addEventListener("click",()=>{overnightShortlistMode="mcc";saveOvernightShortlists();updateOvernightShortlistMode();});
$("overnightShortlistManual")?.addEventListener("click",()=>{if(overnightManualSymbols.length!==4)return;overnightShortlistMode="manual";saveOvernightShortlists();updateOvernightShortlistMode();});
async function fetchOvernightTopFourEvidence(){
 const rows=Array.isArray(scannerData.imported)?scannerData.imported:[];const button=$("overnightQualRefreshMarket"),status=$("overnightQualificationStatus");
 if(!rows.length){if(status)status.textContent="Import the Overnight Scanner CSV first.";return;}
 const ranked=rows.slice().sort((a,b)=>(Number(String(b.score??"").replace(/[^0-9.-]/g,""))||-Infinity)-(Number(String(a.score??"").replace(/[^0-9.-]/g,""))||-Infinity)||String(a.symbol).localeCompare(String(b.symbol)));
 const selected=overnightShortlistMode==="manual"?overnightManualSymbols.map(symbol=>rows.find(item=>String(item.symbol||"").trim().toUpperCase()===symbol)).filter(Boolean):ranked.slice(0,4);
 if(selected.length!==4){if(status)status.textContent=overnightShortlistMode==="manual"?"Select exactly four symbols in My Top 4 before fetching.":"MCC could not identify four distinct imported candidates.";return;}
 if(button)button.disabled=true;if(status)status.textContent="Fetching Daily and catalyst evidence for "+(overnightShortlistMode==="manual"?"My Top 4":"MCC Top 4")+": "+selected.map(x=>x.symbol).join(", ")+"…";
 const results=await Promise.all(selected.map(async item=>{
  const symbol=String(item.symbol||"").trim().toUpperCase();
  const get=async path=>{const response=await fetch(path,{cache:"no-store",headers:{Accept:"application/json"}});if(!response.ok){let detail="HTTP "+response.status;try{const body=await response.json();detail=body.detail||body.title||body.error||detail;}catch{}throw new Error(detail);}return response.json();};
  const [daily,catalyst]=await Promise.allSettled([
   get("/api/market-data/daily?symbol="+encodeURIComponent(symbol)),
   get("/api/market-data/catalyst-assessment?symbol="+encodeURIComponent(symbol))
  ]);
  const evidence={
   daily:daily.status==="fulfilled"?daily.value:null,
   dailyError:daily.status==="rejected"?daily.reason.message:null,
   catalyst:catalyst.status==="fulfilled"?catalyst.value:null,
   catalystError:catalyst.status==="rejected"?catalyst.reason.message:null,
   retrievedAtUtc:new Date().toISOString()
  };
  overnightLiveEvidence.set(symbol,evidence);
  return {symbol,evidence};
 }));
 if(button)button.disabled=false;
 renderOvernightQualification();
 const failed=results.filter(x=>x.evidence.dailyError||x.evidence.catalystError);
 renderCatalystHumanReviewQueue();
 if(status)status.textContent="Live evidence fetched for "+results.length+" candidates: "+(results.length-failed.length)+" complete, "+failed.length+" with provider/API errors. Only this selected group of four was queried; other imported rows remain DATA?. No candidate can become READY because catalyst clearance and 5M/1M evidence are not validated.";
}

function renderCatalystHumanReviewQueue(){
 const host=$("catalystReviewQueue"),count=$("catalystReviewQueueCount");if(!host)return;
 const rows=[];
 overnightLiveEvidence.forEach((evidence,symbol)=>{
  (Array.isArray(evidence.catalyst?.articles)?evidence.catalyst.articles:[]).forEach(item=>{const article=item.article||item;rows.push({symbol,item,article,evidence});});
 });
 if(count)count.textContent=rows.length+" articles";
 if(!rows.length){host.replaceChildren(Object.assign(document.createElement("p"),{className:"muted small",textContent:"No articles returned for the currently fetched candidate groups. No news is UNKNOWN, not CLEAR."}));return;}
 host.replaceChildren(...rows.map(({symbol,item,article,evidence})=>{
  const card=document.createElement("article");card.className="catalyst-review-card";
  const title=document.createElement("strong");title.textContent=symbol+" · "+(article.headline||"Untitled article");
  const meta=document.createElement("p");meta.className="muted small";meta.textContent=[item.severity||"UNCLASSIFIED",(article.publishedAtUtc||"Publication time unavailable"),(item.categories||[]).join(", ")].filter(Boolean).join(" · ");
  const summary=document.createElement("p");summary.className="small";summary.textContent=article.summary||item.reason||"No article summary supplied.";
  const link=document.createElement("a");link.href=article.url||"#";link.target="_blank";link.rel="noopener noreferrer";link.textContent="Open source article";
  const controls=document.createElement("div");controls.className="catalyst-review-controls";
  const label=document.createElement("select");label.setAttribute("aria-label","Human catalyst label for "+symbol);
  ["MATERIAL_RISK","MATERIAL_EVENT","NON_MATERIAL","IRRELEVANT","DUPLICATE","UNCERTAIN"].forEach(value=>{const option=document.createElement("option");option.value=value;option.textContent=value;label.append(option);});
  const notes=document.createElement("textarea");notes.maxLength=2000;notes.placeholder="Why does this label fit? Review the source article.";notes.setAttribute("aria-label","Review notes for "+symbol);
  const save=document.createElement("button");save.type="button";save.className="quiet-button";save.textContent="Save human review";
  const status=document.createElement("span");status.className="muted small";
  save.addEventListener("click",async()=>{
   if(!article.url||!/^https?:\/\//i.test(article.url)){status.textContent="Cannot save: source article URL is missing or invalid.";return;}
   save.disabled=true;status.textContent="Saving append-only review…";
   try{
    const response=await fetch("/api/market-data/catalyst-reviews",{method:"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},body:JSON.stringify({symbol,articleId:article.id||null,headline:article.headline||"Untitled article",url:article.url,publishedAtUtc:article.publishedAtUtc||null,source:"Alpaca News",label:label.value,ruleSetVersion:evidence.catalyst?.ruleSetVersion||"MCC-CATALYST-TRIAGE-1.0",notes:notes.value||null})});
    const payload=await response.json();if(!response.ok)throw new Error(payload.detail||payload.title||payload.error||("HTTP "+response.status));
    status.textContent="Saved "+label.value+" · "+(payload.reviewId||payload.review?.reviewId||"review recorded")+". This does not clear catalyst status.";
    const overall=$("catalystReviewStatus");if(overall)overall.textContent="Human review appended for "+symbol+". Automated triage remains unchanged.";
   }catch(error){status.textContent="Review save failed: "+error.message;save.disabled=false;}
  });
  controls.append(label,notes,save);card.append(title,meta,summary,link,controls,status);return card;
 }));
}

async function renderOvernightQualification(){
 const body=$("overnightQualificationRows");
 if(!body)return;
 const rows=Array.isArray(scannerData.imported)?scannerData.imported:[];
 renderOvernightShortlistPicker(rows);updateOvernightShortlistMode();
 const setText=(id,value)=>{const node=$(id);if(node)node.textContent=String(value);};
 setText("overnightQualImported",rows.length);
 if(!rows.length){
  body.replaceChildren(Object.assign(document.createElement("tr"),{innerHTML:"<td colspan=\"12\">Import the Overnight Scanner CSV from Scanner Workbench → Candidate discovery.</td>"}));
  setText("overnightQualificationStatus","No imported scanner rows. This panel does not run a new Thinkorswim scan.");
  setText("overnightQualPass","DATA?");setText("overnightQualFail","DATA?");setText("overnightQualUnknown","DATA?");setText("overnightQualExcluded","DATA?");setText("overnightQualReady","0");
  return;
 }
 let exclusionSet=null,exclusionCount="DATA?",exclusionError=null;
 try{
  const response=await fetch("/api/watchlists/exclusions",{cache:"no-store",headers:{Accept:"application/json"}});
  if(!response.ok)throw new Error("HTTP "+response.status);
  const payload=await response.json(),entries=Array.isArray(payload)?payload:(Array.isArray(payload.symbols)?payload.symbols:null);
  if(!entries)throw new Error("Unexpected exclusion response shape");
  const normalize=value=>String(value||"").toUpperCase().replace(/[^A-Z0-9]/g,"");
  exclusionSet=new Set(entries.map(x=>normalize(x.symbol??x.Symbol)).filter(Boolean));
  exclusionCount=entries.length;
 }catch(error){exclusionError=error.message;}
 const metric=value=>{const raw=String(value??"").trim();if(!raw||/^(loading|data\?|—|n\/a)$/i.test(raw))return null;const n=Number(raw.replace("%","").replace(/,/g,"").trim());return Number.isFinite(n)?n:null;};
 const fmt=value=>value===null||value===undefined?"DATA?":Number(value).toLocaleString(undefined,{maximumFractionDigits:3});
 let passed=0,failed=0,unknown=0,excluded=0;
 const evaluated=rows.map(item=>{
  const m=item.overnightMetrics||{};
  const onWin=metric(m.onWin),avgOn=metric(m.avgOn),fiveDay=metric(m.fiveDayOn),currentOn=metric(m.currentOn),above20=metric(m.above20Sma);
  const checks=[onWin>=60,avgOn>=0.10,fiveDay>avgOn,currentOn<3,above20===1];
  const requiredMetrics=[onWin,avgOn,fiveDay,currentOn,above20];
  // Missing custom-expression output is UNKNOWN, never a failed historical gate.
  const history=requiredMetrics.some(value=>value===null)?"DATA?":checks.every(Boolean)?"PASS":"FAIL";
  if(history==="PASS")passed++;else if(history==="FAIL")failed++;else unknown++;
  const normalized=String(item.symbol||"").toUpperCase().replace(/[^A-Z0-9]/g,"");
  const isExcluded=exclusionSet?exclusionSet.has(normalized):null;
  if(isExcluded)excluded++;
  const live=overnightLiveEvidence.get(String(item.symbol||"").toUpperCase())||{};
  const daily=live.daily||null,catalyst=live.catalyst||null;
  const dailyUsable=daily?.qualificationUsable===true;
  const price=dailyUsable&&daily.price!==null&&daily.price!==undefined&&Number.isFinite(Number(daily.price))?Number(daily.price):null;
  const averageVolume=dailyUsable&&daily.averageDailyVolume20!==null&&daily.averageDailyVolume20!==undefined&&Number.isFinite(Number(daily.averageDailyVolume20))?Number(daily.averageDailyVolume20):null;
  const dailyGate=price!==null&&averageVolume!==null?(price<=5||averageVolume<500000?"FAIL":"PASS"):"DATA?";
  const catalystStatus=catalyst?.status||"UNKNOWN";
  const catalystLabel=live.catalystError?"ERROR · "+live.catalystError:!catalyst?(live.dailyError||live.daily?"NOT LOADED":"NOT FETCHED"):catalystStatus+(catalyst.coverageComplete===false?" · COVERAGE INCOMPLETE":"");
  const catalystRisk=catalystStatus==="RISK_PRESENT";
  const decision=isExcluded===true?"BLOCKED · SCHWAB EXCLUSION":exclusionError?"DATA? · EXCLUSION LIST":history==="FAIL"?"BLOCKED · HISTORICAL GATE":dailyGate==="FAIL"?"BLOCKED · DAILY PRICE / LIQUIDITY":catalystRisk?"BLOCKED · CATALYST RISK":daily?.qualificationUsable!==true?"DATA? · DAILY SIP / 20 SESSIONS":dailyGate!=="PASS"?"DATA? · DAILY METRICS":!catalyst||catalyst.coverageComplete!==true||catalystStatus!=="CLEAR_WITH_SOURCE"?"MANUAL REVIEW · CATALYST": "MANUAL REVIEW · 5M / 1M";
  return {...item,onWin,avgOn,fiveDay,currentOn,above20,history,isExcluded,decision,daily,price,averageVolume,dailyGate,catalystLabel,live};
 }).sort((a,b)=>(metric(b.score)??-Infinity)-(metric(a.score)??-Infinity)||String(a.symbol).localeCompare(String(b.symbol)));
 const cell=value=>{const td=document.createElement("td");td.textContent=value===null||value===undefined?"DATA?":String(value);return td;};
 body.replaceChildren(...evaluated.map(item=>{
  const tr=document.createElement("tr");
  tr.append(cell(item.symbol),cell(item.onWin),cell(item.avgOn),cell(item.fiveDay),cell(item.currentOn),cell(item.above20===null?null:item.above20===1?"YES":"NO"),cell(item.history),cell(exclusionError?"DATA?":item.isExcluded?"EXCLUDED":"NO"),cell(item.price===null?(item.daily?.feedCoverage==="IEX_PARTIAL"?"DATA? · IEX PARTIAL":item.live.dailyError?"ERROR · "+item.live.dailyError:null):fmt(item.price)),cell(item.averageVolume===null?(item.live.dailyError?"ERROR · "+item.live.dailyError:null):fmt(item.averageVolume)),cell(item.catalystLabel),cell(item.decision));
  tr.className=item.decision.startsWith("BLOCKED")?"qualification-blocked":"qualification-pending";
  return tr;
 }));
 setText("overnightQualPass",passed);setText("overnightQualFail",failed);setText("overnightQualUnknown",unknown);setText("overnightQualExcluded",exclusionError?"DATA?":excluded);setText("overnightQualReady","0");
 const liveCount=overnightLiveEvidence.size;
 setText("overnightQualificationStatus",`Imported ${rows.length} rows · historical gates: ${passed} PASS / ${failed} FAIL / ${unknown} DATA? · exclusions: ${exclusionError?"DATA? ("+exclusionError+")":exclusionCount+" loaded, "+excluded+" matches"} · Daily/catalyst queried for ${liveCount} symbols only. SIP entitlement and catalyst clearance are not assumed; READY remains 0.`);
}
$("overnightQualRefreshMarket")?.addEventListener("click",()=>void fetchOvernightTopFourEvidence());

function readWatchlist(){
 try{return JSON.parse(localStorage.getItem("mcc.watchlist")||"[]").filter(item=>typeof item.symbol==="string");}
 catch{return []}
}

function writeWatchlist(items){localStorage.setItem("mcc.watchlist",JSON.stringify(items));renderWatchlist();}

function addWatch(symbol){
 const normalized=symbol.trim().toUpperCase();if(!/^[A-Z][A-Z0-9.-]{0,7}$/.test(normalized))return;
 const items=readWatchlist();if(!items.some(item=>item.symbol===normalized)){items.push({symbol:normalized,addedAt:new Date().toISOString()});writeWatchlist(items);}
}

function removeWatch(symbol){writeWatchlist(readWatchlist().filter(item=>item.symbol!==symbol));}

function renderWatchlist(){
 const list=$("watchlistRows"),items=readWatchlist();
 list.replaceChildren(...(items.length?items.map(item=>{
  const li=document.createElement("li"),label=document.createElement("span"),button=document.createElement("button");
  label.innerHTML=`<span class="watch-symbol">${item.symbol}</span><br><span class="watch-meta">Local watchlist</span>`;
  button.type="button";button.className="row-action";button.textContent="Remove";button.addEventListener("click",()=>removeWatch(item.symbol));li.append(label,button);return li;
 }):[Object.assign(document.createElement("li"),{textContent:"No candidates added yet."})]));
}

function detectCsvDelimiter(line){
 const candidates=[",",";","\t"],counts={",":0,";":0,"\t":0};let quoted=false;
 for(let i=0;i<line.length;i++){const char=line[i];if(char==='"'){if(quoted&&line[i+1]==='"'){i++;}else quoted=!quoted;}else if(!quoted&&Object.prototype.hasOwnProperty.call(counts,char))counts[char]++;}
 return candidates.reduce((best,item)=>counts[item]>counts[best]?item:best,",");
}
function parseCsvLine(line,delimiter=","){
 const fields=[],buffer=[];let quoted=false;
 for(let i=0;i<line.length;i++){const char=line[i];if(char==='"'){if(quoted&&line[i+1]==='"'){buffer.push('"');i++;}else quoted=!quoted;}else if(char===delimiter&&!quoted){fields.push(buffer.join(""));buffer.length=0;}else buffer.push(char);}
 fields.push(buffer.join(""));return fields;
}
function normalizeCsvHeader(value){
 return String(value??"").replace(/^\uFEFF/,"").trim().replace(/^"(.*)"$/,"$1").trim().toLowerCase().replace(/\s+/g,"").replace(/[^a-z0-9/]/g,"");
}

$("scannerGroup").addEventListener("change",renderScanner);
$("watchlistForm").addEventListener("submit",event=>{event.preventDefault();addWatch($("watchSymbol").value);$("watchSymbol").value="";});
$("clearWatchlist").addEventListener("click",()=>writeWatchlist([]));
$("scannerFile").addEventListener("change",event=>{
 const file=event.target.files[0];if(!file)return;
 const reader=new FileReader();reader.onload=()=>{
  const text=String(reader.result).replace(/^\uFEFF/,"");
  const lines=text.split(/\r?\n/).filter(line=>line.trim().length);
  if(lines[0]&&/^sep=./i.test(lines[0]))lines.shift();
  if(!lines.length){$("scannerSource").textContent="Import rejected: CSV is empty";return;}
  const symbolHeaders=["symbol","ticker","symbol/ticker","symbolticker","tickersymbol"];
  let headerIndex=-1,delimiter=",",headers=[],symbolIndex=-1;
  for(let i=0;i<Math.min(lines.length,25);i++){
   const candidateDelimiter=detectCsvDelimiter(lines[i]);
   const candidateHeaders=parseCsvLine(lines[i],candidateDelimiter).map(normalizeCsvHeader);
   const candidateSymbolIndex=candidateHeaders.findIndex(value=>symbolHeaders.includes(value));
   if(candidateSymbolIndex>=0){headerIndex=i;delimiter=candidateDelimiter;headers=candidateHeaders;symbolIndex=candidateSymbolIndex;break;}
  }
  if(headerIndex<0){const detected=lines.slice(0,10).map(line=>parseCsvLine(line,detectCsvDelimiter(line)).map(normalizeCsvHeader).join(", ")).filter(Boolean).join(" | ");$("scannerSource").textContent="Import rejected: Symbol/Ticker header row not found in first 25 lines. Detected rows: "+detected;return;}
  const imported=lines.slice(headerIndex+1).map(line=>{
   const values=parseCsvLine(line,delimiter),field=(...names)=>{const i=names.map(name=>headers.indexOf(name)).find(index=>index>=0);return i===undefined?"":(values[i]||"").trim();};
   const overnightMetrics={
    onWin:field("onwin")||"DATA?",
    avgOn:field("avgon")||"DATA?",
    medianOn:field("medianon")||"DATA?",
    fiveDayOn:field("5don")||"DATA?",
    onMomentum:field("onmomentum")||"DATA?",
    currentOn:field("currenton")||"DATA?",
    above20Sma:field("above20sma")||"DATA?"
   };
   const metricSummary="ON win "+overnightMetrics.onWin+" · 20D avg "+overnightMetrics.avgOn+" · median "+overnightMetrics.medianOn+" · 5D "+overnightMetrics.fiveDayOn+" · momentum "+overnightMetrics.onMomentum+" · current ON "+overnightMetrics.currentOn+" · above 20 SMA "+overnightMetrics.above20Sma;
   return {
    symbol:(values[symbolIndex]||"").trim().toUpperCase(),
    score:field("score","overnightscore")||"—",
    status:field("status")||"IMPORTED",
    extension:field("extension","ext")||"DATA?",
    freshness:field("freshness","data_as_of","dataasof")||"UNKNOWN",
    reason:field("reason")||metricSummary,
    overnightMetrics
   };
  }).filter(item=>item.symbol);
  if(!imported.length){$("scannerSource").textContent="Import rejected: header found, but no rows with a Symbol/Ticker value were present.";return;}
  scannerData.imported=imported;
  overnightLiveEvidence.clear();
  const importedSymbols=new Set(imported.map(item=>String(item.symbol||"").toUpperCase()).filter(Boolean));
  const redZoneState=readRedZoneState();
  const previouslyDismissed=redZoneState.dismissed.length;
  redZoneState.dismissed=redZoneState.dismissed.filter(symbol=>!importedSymbols.has(String(symbol).toUpperCase()));
  const dismissalResetCount=previouslyDismissed-redZoneState.dismissed.length;
  writeRedZoneState(redZoneState);
  let persistenceNote="";
  try{localStorage.setItem("mcc.scannerImport.v1",JSON.stringify({filename:file.name,importedAtUtc:new Date().toISOString(),rows:imported}));}
  catch(error){persistenceNote=" · browser storage unavailable: "+error.message;}
  const groupSelect=$("scannerGroup");const option=groupSelect.querySelector('option[value="imported"]')||document.createElement("option");option.value="imported";option.textContent="Imported local CSV";if(!option.parentElement)groupSelect.append(option);groupSelect.value="imported";renderScanner();renderMorningReport();
  const renderedRows=$("scannerRows")?.querySelectorAll("tr").length??0;
  const redZoneCards=$("redZoneGrid")?.querySelectorAll(".redzone-card").length??0;
  const redZoneSymbols=[...($("redZoneGrid")?.querySelectorAll(".redzone-card h3")??[])].map(node=>node.textContent).filter(Boolean);
  $("scannerSource").textContent=`Imported ${imported.length} local rows · scanner table rendered ${renderedRows} · Red Zone rendered ${redZoneCards}/4: ${redZoneSymbols.join(", ")||"none"} · reset ${dismissalResetCount} matching old dismissals${persistenceNote}`;
  void renderOvernightQualification();
 };reader.readAsText(file);
});
renderScanner();renderWatchlist();
if(restoredScannerImport?.rows?.length){
 scannerData.imported=restoredScannerImport.rows;
 const option=document.querySelector('#scannerGroup option[value="imported"]')||document.createElement("option");
 option.value="imported";option.textContent="Imported local CSV";if(!option.parentElement)$("scannerGroup").append(option);
 $("scannerGroup").value="imported";renderScanner();renderMorningReport();void renderOvernightQualification();
 const stamp=restoredScannerImport.importedAtUtc?new Date(restoredScannerImport.importedAtUtc).toLocaleString():"previous session";
 $("scannerSource").textContent=`Restored ${restoredScannerImport.rows.length} local rows from ${restoredScannerImport.filename||"CSV"} · imported ${stamp}`;
}


// Feedback notes stay in browser local storage; no network calls are made.
function readFeedback(){
 try { const value=JSON.parse(localStorage.getItem(feedbackKey)||"[]"); return Array.isArray(value)?value:[]; }
 catch { return []; }
}
function renderFeedback(){
 const list=$("feedbackRows"); if(!list)return;
 const items=readFeedback();
 list.replaceChildren(...(items.length?items.slice().reverse().map(item=>{
  const li=document.createElement("li"),body=document.createElement("div"),meta=document.createElement("div"),note=document.createElement("p"),remove=document.createElement("button");
  meta.className="feedback-meta";meta.textContent=item.area+" · "+item.priority+" · "+new Date(item.createdAt).toLocaleString();
  note.textContent=item.text;body.append(meta,note);remove.type="button";remove.className="row-action";remove.textContent="Delete";
  remove.addEventListener("click",()=>{localStorage.setItem(feedbackKey,JSON.stringify(readFeedback().filter(x=>x.id!==item.id)));renderFeedback();});
  li.append(body,remove);return li;
 }):[Object.assign(document.createElement("li"),{textContent:"No feedback notes saved yet."})]));
}
$("feedbackForm")?.addEventListener("submit",event=>{
 event.preventDefault();const note=$("feedbackText").value.trim();if(!note)return;
 const items=readFeedback();items.push({id:Date.now().toString(36),area:$("feedbackArea").value,priority:$("feedbackPriority").value,text:note,createdAt:new Date().toISOString(),symbol:$("symbol").value,snapshot:$("snapshot").value});
 localStorage.setItem(feedbackKey,JSON.stringify(items));$("feedbackText").value="";renderFeedback();
});
$("clearFeedback")?.addEventListener("click",()=>{localStorage.removeItem(feedbackKey);renderFeedback();});
$("exportFeedback")?.addEventListener("click",()=>{
 const blob=new Blob([JSON.stringify(readFeedback(),null,2)],{type:"application/json"}),url=URL.createObjectURL(blob),a=document.createElement("a");
 a.href=url;a.download="mcc-dashboard-feedback.json";a.click();URL.revokeObjectURL(url);
});
renderFeedback();


/// MODULE: Morning Report Preview
/// PURPOSE: Presents a transparent, locally derived report preview from demo/import rows.
/// IMPORTANT: This is NOT a market-data collector, live scanner, or validated opportunity ranker.
function readRedZoneState(){
 try{
  const value=JSON.parse(localStorage.getItem("mcc.redZoneState.v1")||"{}");
  return {
   pinned:Array.isArray(value.pinned)?value.pinned.filter(x=>typeof x==="string"):[],
   dismissed:Array.isArray(value.dismissed)?value.dismissed.filter(x=>typeof x==="string"):[],
   mode:value.mode==="manual"?"manual":"automatic"
  };
 }catch{return {pinned:[],dismissed:[],mode:"automatic"};}
}
function writeRedZoneState(state){localStorage.setItem("mcc.redZoneState.v1",JSON.stringify(state));renderMorningReport();}

function playRedZoneAlert(){
 if(!redZoneSoundEnabled)return;
 const AudioContextType=window.AudioContext||window.webkitAudioContext;
 if(!AudioContextType)return;
 try{
  redZoneAudioContext=redZoneAudioContext||new AudioContextType();
  if(redZoneAudioContext.state==="suspended")void redZoneAudioContext.resume();
  const now=redZoneAudioContext.currentTime;
  [880,1175].forEach((frequency,index)=>{
   const oscillator=redZoneAudioContext.createOscillator(),gain=redZoneAudioContext.createGain(),start=now+index*.18;
   oscillator.type="sine";oscillator.frequency.value=frequency;
   gain.gain.setValueAtTime(.0001,start);gain.gain.exponentialRampToValueAtTime(.16,start+.015);gain.gain.exponentialRampToValueAtTime(.0001,start+.13);
   oscillator.connect(gain);gain.connect(redZoneAudioContext.destination);oscillator.start(start);oscillator.stop(start+.14);
  });
 }catch(error){const button=$("toggleRedZoneSound");if(button)button.title="Sound unavailable: "+error.message;}
}
$("toggleRedZoneSound")?.addEventListener("click",async()=>{
 redZoneSoundEnabled=!redZoneSoundEnabled;
 const button=$("toggleRedZoneSound");
 if(button){button.textContent=redZoneSoundEnabled?"Sound alerts: ON":"Enable sound alerts";button.setAttribute("aria-pressed",String(redZoneSoundEnabled));}
 if(redZoneSoundEnabled){
  const AudioContextType=window.AudioContext||window.webkitAudioContext;
  if(AudioContextType){redZoneAudioContext=redZoneAudioContext||new AudioContextType();if(redZoneAudioContext.state==="suspended")await redZoneAudioContext.resume();playRedZoneAlert();}
  else{redZoneSoundEnabled=false;if(button){button.textContent="Sound unavailable";button.setAttribute("aria-pressed","false");}}
 }
});
async function updateRedZoneCardAssessment(symbol,statusNode,scoreNode,extensionNode,freshnessNode,participationNode,reasonNode,gateNode,chartNode){
 void ensureMinuteBarSubscription(symbol);
 try{
  const data=await fetchLiveAssessment(symbol),one=data.oneMinute||{},five=data.fiveMinute||{},decision=data.decision||null;
  statusNode.textContent=decision?humanizeSignalState(decision.state):humanizeSignalState(data.evaluationStatus||data.status||"DATA?");
  setV2ScoreDisplay(scoreNode,five.scoreBreakdown,decision,data.evaluationStatus);
  const money=value=>value!==null&&value!==undefined&&Number.isFinite(Number(value))?"$"+Number(value).toFixed(3):"DATA?";
  extensionNode.textContent="1M close "+money(one.close)+" · Daily VWAP "+money(one.vwap)+" · ATR14 "+money(one.atr)+" · Extension "+(one.extensionAtr!==null&&one.extensionAtr!==undefined&&Number.isFinite(Number(one.extensionAtr))?Number(one.extensionAtr).toFixed(2)+" ATR":"DATA?");
  freshnessNode.textContent="Freshness: 1M "+sourceFreshness(one.freshness,one.ageSeconds)+" · 5M "+sourceFreshness(five.freshness,five.ageSeconds);
  const relativeVolume=Number(five.relativeVolume);
  const rvolCriterion=five.scoreBreakdown?.criteria?.find(item=>item.key==="rvolSupportive");
  const participationKnown=Number.isFinite(relativeVolume)&&typeof rvolCriterion?.passed==="boolean";
  const lowParticipation=participationKnown&&rvolCriterion.passed===false;
  const baselineSamples=Number(five.relativeVolumeBaselineSamples);
  participationNode.textContent=participationKnown
   ? "5M participation: "+relativeVolume.toFixed(2)+"x same-slot RVOL · "+(lowParticipation?"BELOW V2 THRESHOLD":"V2 THRESHOLD MET")+" · "+(Number.isFinite(baselineSamples)?baselineSamples:"?")+" samples"
   : "5M participation: DATA? · same-slot RVOL baseline unavailable";
  participationNode.className="redzone-participation "+(participationKnown?(lowParticipation?"low":"ok"):"unknown");
  const decisionReason=decision?.reason||data.evaluationReason||"Decision not evaluated.";
  const quality=data.opportunityQuality||{};
  const qualityMetrics=quality.metrics||{};
  const qualitySummary=quality.status==="DATA?"
   ?"Opportunity quality: DATA? · required live inputs missing or stale."
   :"Opportunity quality (diagnostic): "+String(quality.displayLabel||quality.classification||"UNKNOWN")+
    " · RVOL "+formatMetric(qualityMetrics.relativeVolume,2,"x")+
    " · range "+formatMetric(qualityMetrics.recentRangeInAtr,2," ATR")+
    " · extension "+formatMetric(qualityMetrics.atrExtension,2," ATR")+
    " · "+(Array.isArray(quality.reasons)?quality.reasons.join(", "):"No reason codes");
  reasonNode.textContent=(lowParticipation?decisionReason+" Low participation: 5M same-time-of-day RVOL is below the existing V2 threshold.":decisionReason)+
   " "+qualitySummary+
   " Provisional diagnostic only; does not override V2 Permission/entry state.";
  gateNode.textContent="5M Permission: "+((five.scoreBreakdown?.permission??decision?.fiveMinutePermission)===true?"YES":(five.scoreBreakdown?.permission??decision?.fiveMinutePermission)===false?"NO":"UNKNOWN")+
   " · 1M execution: "+(decision?.oneMinuteExecution||"UNKNOWN");
  drawPriceLine(chartNode,one.chartBars||[],symbol);
 }catch(error){
  statusNode.textContent="DATA UNAVAILABLE";
  setV2ScoreDisplay(scoreNode,null,null,"REQUEST FAILED");
  extensionNode.textContent="Extension: DATA? · live request failed";
  freshnessNode.textContent="Freshness: UNKNOWN · "+error.message;
  participationNode.textContent="5M participation: DATA? · live assessment unavailable";
  participationNode.className="redzone-participation unknown";
  reasonNode.textContent="Live market assessment unavailable. No prior status is being represented as current.";
  gateNode.textContent="5M Permission: UNKNOWN · 1M execution: UNKNOWN";
  chartNode.textContent="Live price unavailable";
 }
}
async function refreshRedZoneAssessments(){
 await Promise.allSettled(redZoneAssessmentRefreshers.map(refresh=>refresh()));
}
function renderMorningReport(){
 const grid=$("redZoneGrid");if(!grid)return;
 redZoneAssessmentRefreshers=[];
 const group=$("scannerGroup")?.value||"momentum";
 const sourceRows=Array.isArray(scannerData[group])?scannerData[group]:[];
 const state=readRedZoneState();
 const bySymbol=new Map(sourceRows.filter(x=>x&&x.symbol).map(x=>[String(x.symbol).toUpperCase(),x]));
 const pinnedRows=state.pinned.map(symbol=>bySymbol.get(symbol)||({symbol,score:"DATA?",status:"MANUAL — REVIEW",extension:"DATA?",freshness:"UNKNOWN",reason:"Manually pinned candidate. Source data, 5M Permission, and 1M execution are not established."}));
 const importedMetric=value=>{const raw=String(value??"").trim();if(!raw||/^(loading|data\?|—|n\/a)$/i.test(raw))return null;const n=Number(raw.replace("%","").trim());return Number.isFinite(n)?n:null;};
 const rankedSourceRows=group==="imported"?sourceRows.filter(item=>{
  const m=item.overnightMetrics||{},onWin=importedMetric(m.onWin),avgOn=importedMetric(m.avgOn),fiveDay=importedMetric(m.fiveDayOn),currentOn=importedMetric(m.currentOn),above20=importedMetric(m.above20Sma);
  return onWin!==null&&onWin>=60&&avgOn!==null&&avgOn>=0.10&&fiveDay!==null&&fiveDay>avgOn&&currentOn!==null&&currentOn<3&&above20===1;
 }):sourceRows;
 const sortedRows=rankedSourceRows.slice().sort((a,b)=>{
  const sa=Number(a.score),sb=Number(b.score);
  if(Number.isFinite(sa)!==Number.isFinite(sb))return Number.isFinite(sb)-Number.isFinite(sa);
  const av=Number.isFinite(sa)?(group==="live"?Math.abs(sa):sa):-Infinity;
  const bv=Number.isFinite(sb)?(group==="live"?Math.abs(sb):sb):-Infinity;
  return bv-av||String(a.symbol).localeCompare(String(b.symbol));
 });
 const dismissed=new Set(state.dismissed);
 const pinnedSet=new Set(state.pinned);
 const automaticRows=sortedRows.filter(item=>!dismissed.has(String(item.symbol||"").toUpperCase()));
 const ranked=(state.mode==="manual"
  ? pinnedRows
  : automaticRows)
  .filter((item,index,all)=>all.findIndex(x=>String(x.symbol||"").toUpperCase()===String(item.symbol||"").toUpperCase())===index)
  .slice(0,4);
 const modeButton=$("toggleRedZoneMode");
 if(modeButton){
  modeButton.textContent=state.mode==="manual"?"Mode: Manual":"Mode: Automatic";
  modeButton.setAttribute("aria-pressed",String(state.mode==="manual"));
  modeButton.title=state.mode==="manual"?"Showing pinned symbols in saved order.":"Showing top scanner-score preview; this is not a calibrated Opportunity Quality ranking.";
 }
 const currentSymbols=new Set(ranked.map(item=>String(item.symbol||"").toUpperCase()).filter(Boolean));
 if(previousRedZoneSymbols&&!suppressNextRedZoneAlert&&redZoneSoundEnabled){
  const added=[...currentSymbols].filter(symbol=>!previousRedZoneSymbols.has(symbol));
  if(added.length){playRedZoneAlert();const soundButton=$("toggleRedZoneSound");if(soundButton)soundButton.title="New Red Zone priority stock: "+added.join(", ");}
 }
 previousRedZoneSymbols=currentSymbols;suppressNextRedZoneAlert=false;
 $("morningTopCount").textContent=String(ranked.length);
 $("reportSubtitle").textContent=state.mode==="manual"
  ?"Manual mode · pinned symbols in your saved order · pins and dismissals are preserved when switching modes."
  :(group==="imported"?"Automatic preview · top four by supplied Overnight Score among historical-gate passes · NOT a calibrated live Opportunity Quality rank · current price, 20D volume, catalyst and chart gates remain separate.":"Automatic preview · sorted by supplied scanner score · NOT a calibrated live Opportunity Quality rank.");
 grid.replaceChildren(...(ranked.length?ranked.map((item,index)=>{
  const card=document.createElement("article");card.className="redzone-card";
  const top=document.createElement("div");top.className="redzone-card-top";
  const rank=document.createElement("span");rank.className="rank-pill";rank.textContent="#"+(index+1);
  const status=document.createElement("span");status.className="chip";status.textContent=item.status||"DATA?";
  top.append(rank,status);
  const symbol=document.createElement("h3");symbol.textContent=item.symbol||"UNKNOWN";
  const score=document.createElement("div");score.className="redzone-score score-unknown";score.textContent="V2 5M score: LOADING live assessment…";
  const ext=document.createElement("p");ext.textContent="Extension: loading live 1M bars…";
  const fresh=document.createElement("p");fresh.textContent="Freshness: requesting 1M/5M source timestamps…";
  const participation=document.createElement("p");participation.className="redzone-participation unknown";participation.textContent="5M participation: loading same-slot RVOL…";
  const reason=document.createElement("p");reason.className="redzone-reason";reason.textContent=item.reason||"No reason supplied.";
  const gate=document.createElement("div");gate.className="gate-warning";gate.textContent="5M Permission: loading · 1M execution: loading";
  const chart=document.createElement("div");chart.className="redzone-sparkline";chart.textContent="Loading 1M price…";
  const actions=document.createElement("div");actions.className="redzone-actions";
  const focusButton=document.createElement("button");focusButton.type="button";focusButton.className="quiet-button";focusButton.textContent="Open in Main Chart";
  const manageButton=document.createElement("button");manageButton.type="button";manageButton.className="quiet-button";manageButton.textContent=state.mode==="manual"?"Remove pin":"Dismiss";
  manageButton.addEventListener("click",event=>{
   event.stopPropagation();
   const current=readRedZoneState(),ticker=String(item.symbol||"").toUpperCase();
   if(current.mode==="manual")current.pinned=current.pinned.filter(x=>x!==ticker);
   else current.dismissed=[...new Set([...current.dismissed,ticker])];
   writeRedZoneState(current);
  });
  actions.append(focusButton,manageButton);
  card.append(top,symbol,score,ext,fresh,participation,reason,chart,gate,actions);
  redZoneAssessmentRefreshers.push(()=>updateRedZoneCardAssessment(item.symbol,status,score,ext,fresh,participation,reason,gate,chart));
  void updateRedZoneCardAssessment(item.symbol,status,score,ext,fresh,participation,reason,gate,chart);
  card.tabIndex=0;card.setAttribute("role","button");card.setAttribute("aria-label","Focus "+(item.symbol||"candidate")+" price chart");
  const focus=()=>{const selector=$("symbol");if(selector&&item.symbol){if(selector.tagName==="SELECT"){let option=[...selector.options].find(o=>o.value===item.symbol);if(!option){option=document.createElement("option");option.value=item.symbol;option.textContent=item.symbol;selector.append(option);}}selector.value=item.symbol;render();refreshFocusedPriceChart();}};
  focusButton.addEventListener("click",event=>{event.stopPropagation();focus();});
  card.addEventListener("click",focus);card.addEventListener("keydown",event=>{if(event.key==="Enter"||event.key===" "){event.preventDefault();focus();}});
  return card;
 }):[Object.assign(document.createElement("article"),{className:"redzone-empty",textContent:"No candidate rows available. Import a CSV or select a demo scanner group."})]));
}
$("toggleRedZoneMode")?.addEventListener("click",()=>{
 const state=readRedZoneState();
 state.mode=state.mode==="manual"?"automatic":"manual";
 suppressNextRedZoneAlert=true;
 writeRedZoneState(state);
 const status=$("redZoneManageStatus");
 if(status)status.textContent=state.mode==="manual"
  ?"Manual mode enabled. Red Zone now shows only your pinned symbols in saved order. Add a ticker with Pin to Red Zone."
  :"Automatic mode enabled. Red Zone shows the top four supplied scanner-score candidates after historical gates; live Opportunity Quality is diagnostic and not yet the ranker.";
});
$("scannerGroup")?.addEventListener("change",()=>{suppressNextRedZoneAlert=true;renderMorningReport();});
$("redZoneAddForm")?.addEventListener("submit",event=>{
 event.preventDefault();
 const input=$("redZoneAddSymbol"),ticker=(input?.value||"").trim().toUpperCase();
 if(!/^[A-Z][A-Z0-9.-]{0,7}$/.test(ticker)){ $("redZoneManageStatus").textContent="Enter a valid ticker symbol."; return; }
 const state=readRedZoneState();
 state.pinned=[...new Set([...state.pinned,ticker])];
 state.dismissed=state.dismissed.filter(x=>x!==ticker);
 writeRedZoneState(state);
 input.value="";
 $("redZoneManageStatus").textContent=ticker+" pinned to Red Zone. Missing evidence remains DATA? until supplied.";
});
$("restoreRedZone")?.addEventListener("click",()=>{
 const state=readRedZoneState();const restored=state.dismissed.length;state.dismissed=[];writeRedZoneState(state);
 $("redZoneManageStatus").textContent="Restored "+restored+" dismissed symbols. Red Zone repopulated from the selected scanner group.";
});
$("clearRedZoneCandidates")?.addEventListener("click",()=>{
 const group=$("scannerGroup")?.value||"momentum";
 const rows=Array.isArray(scannerData[group])?scannerData[group]:[];
 const symbols=[...new Set(rows.map(item=>String(item?.symbol||"").trim().toUpperCase()).filter(Boolean))];
 const state=readRedZoneState();
 state.dismissed=[...new Set([...state.dismissed,...symbols])];
 state.pinned=state.pinned.filter(symbol=>!symbols.includes(String(symbol).toUpperCase()));
 writeRedZoneState(state);
 $("redZoneManageStatus").textContent="Cleared "+symbols.length+" symbols from this scanner group. Use Restore dismissed / repopulate to bring eligible candidates back.";
});
$("refreshRedZone")?.addEventListener("click",()=>{
 renderMorningReport();refreshFocusedPriceChart();
 $("redZoneManageStatus").textContent="Red Zone refreshed from the currently selected scanner group. Use Refresh Alpaca market movers to retrieve new provider discoveries.";
});
$("exportMorningReport")?.addEventListener("click",()=>{
 const group=$("scannerGroup")?.value||"momentum";
 const rows=Array.isArray(scannerData[group])?scannerData[group]:[];
 const report={reportGeneratedAt:new Date().toISOString(),clockSource:"browser-local-clock; not market-data timestamp",mode:"PREVIEW_ONLY",liveScannerConnected:false,overnightCollectionRunning:false,provider:"NOT_SELECTED",scannerGroup:group,watchlist:readWatchlist(),topCandidates:rows.slice().sort((a,b)=>(Number(b.score)||-Infinity)-(Number(a.score)||-Infinity)).slice(0,4),boringOvernight:{status:"NOT_CALCULATED"},disclaimer:"Local demo/import data only. No live market data, scanner, 5M permission, 1M execution, or validated ranking is connected."};
 const blob=new Blob([JSON.stringify(report,null,2)],{type:"application/json"}),url=URL.createObjectURL(blob),a=document.createElement("a");
 a.href=url;a.download="mcc-morning-report-preview.json";a.click();URL.revokeObjectURL(url);
});
renderMorningReport();


// Provider comparison now calls the same-origin API. Keys never enter browser code.
// Initialize the date before other dashboard rendering can fail. A date input accepts YYYY-MM-DD,
// not the datetime-local value returned by phoenixLocalInput().
const providerDateInput = $("providerDate");
if (providerDateInput && !providerDateInput.value) {
 const p = phoenixParts(new Date());
 providerDateInput.value = p.year + "-" + p.month + "-" + p.day;
}
const providerCompareButton = $("providerCompareRun");
const formatBar = bar => bar ? new Date(bar.timestamp).toLocaleString("en-US", {timeZone:"America/Phoenix", hour12:false}) + " Arizona · O " + bar.open + " H " + bar.high + " L " + bar.low + " C " + bar.close + " · V " + bar.volume : "DATA?";
function renderProviderResult(prefix, result) {
 const connection = $(prefix + "Connection"), latest = $(prefix + "Latest"), coverage = $(prefix + "Coverage"), recovery = $(prefix + "Recovery");
 if (!result) { connection.textContent = "NO RESPONSE"; latest.textContent = "DATA?"; coverage.textContent = "UNKNOWN"; recovery.textContent = "NOT TESTED"; return; }
 connection.textContent = result.configured ? (result.error ? "REQUEST FAILED" : "REQUEST COMPLETED") : "NOT CONFIGURED";
 latest.textContent = result.latestBar ? formatBar(result.latestBar) : (result.error || "DATA?");
 coverage.textContent = result.barCount + " bars · " + result.gapCount + " observed timestamp gaps · " + result.duplicateTimestampCount + " duplicate timestamps";
 recovery.textContent = result.error ? "NOT VERIFIED: " + result.error : "Historical REST request only; reconnect/backfill not tested";
}
providerCompareButton?.addEventListener("click", async () => {
 const symbol = ($("providerSymbol")?.value || "").trim().toUpperCase();
 const interval = $("providerInterval")?.value || "1m";
 const date = $("providerDate")?.value || "";
 if (!/^[A-Z][A-Z0-9.-]{0,7}$/.test(symbol) || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
  $("providerCompareNotice").textContent = "Invalid test inputs. Enter a valid ticker and date.";
  return;
 }
 const from = new Date(date + "T00:00:00-07:00"), to = new Date(date + "T23:59:59.999-07:00");
 const params = new URLSearchParams({symbol, timeframe: interval, from: from.toISOString(), to: to.toISOString()});
 $("providerCompareNotice").textContent = "Requesting the same UTC window from configured providers…";
 providerCompareButton.disabled = true;
 try {
  const response = await fetch("/api/market-data/compare?" + params.toString(), {headers: {Accept:"application/json"}});
  const data = await response.json();
  if (!response.ok) throw new Error(data.error || ("HTTP " + response.status));
  const alpaca = data.results.find(item => item.provider === "Alpaca");
  const massive = data.results.find(item => item.provider === "Massive");
  renderProviderResult("alpaca", alpaca); renderProviderResult("massive", massive);
  const rows = [
   ["Request", symbol + " · " + interval + " · " + date, symbol + " · " + interval + " · " + date],
   ["Connection / request", alpaca?.error || (alpaca?.configured ? "REQUEST COMPLETED" : "NOT CONFIGURED"), massive?.error || (massive?.configured ? "REQUEST COMPLETED" : "NOT CONFIGURED")],
   ["Bars received", String(alpaca?.barCount ?? "DATA?"), String(massive?.barCount ?? "DATA?")],
   ["New raw observations persisted", String(alpaca?.persistedObservationCount ?? "DATA?"), String(massive?.persistedObservationCount ?? "DATA?")],
   ["First observation", formatBar(alpaca?.firstBar), formatBar(massive?.firstBar)],
   ["Latest observation", formatBar(alpaca?.latestBar), formatBar(massive?.latestBar)],
   ["Observed timestamp gaps", String(alpaca?.gapCount ?? "DATA?"), String(massive?.gapCount ?? "DATA?")],
   ["Duplicate timestamps", String(alpaca?.duplicateTimestampCount ?? "DATA?"), String(massive?.duplicateTimestampCount ?? "DATA?")],
   ["Reconnect / backfill", "NOT TESTED", "NOT TESTED"],
   ["Source entitlement / license", "NOT VERIFIED", "NOT VERIFIED"]
  ];
  $("providerComparisonRows").replaceChildren(...rows.map(([field,a,b]) => {
   const tr = document.createElement("tr");
   for (const value of [field,a,b]) { const td = document.createElement("td"); td.textContent = value; tr.append(td); }
   return tr;
  }));
  $("providerCompareNotice").textContent = data.interpretation + " Window: " + data.from + " to " + data.to + ".";
 } catch (error) {
  $("providerCompareNotice").textContent = "API comparison failed: " + error.message + ". Check that the dashboard is served by MomentumCommandCenterV2.API and inspect /api/providers/status.";
 } finally { providerCompareButton.disabled = false; }
});

async function loadProviderStatus() {
 const setText = (id, value) => { const node = $(id); if (node) node.textContent = value; };
 try {
  const response = await fetch("/api/providers/status", {cache:"no-store",headers:{Accept:"application/json"}});
  if (!response.ok) throw new Error("HTTP " + response.status);
  const data = await response.json();
  const alpaca = data.find(item => item.provider === "Alpaca"), massive = data.find(item => item.provider === "Massive");
  for (const [prefix, provider] of [["alpaca", alpaca], ["massive", massive]]) {
   if (!provider) continue;
   setText(prefix + "Connection", provider.validated ? "CONNECTED · DATA RETURNED" : (provider.configured ? "CONFIGURED · NOT YET VALIDATED" : "NOT CONFIGURED"));
   const validation = provider.lastSuccessfulDataValidationUtc
    ? new Date(provider.lastSuccessfulDataValidationUtc).toLocaleString("en-US", {timeZone:"America/Phoenix"})
    : "No successful non-empty response in this API process";
   const bars = provider.lastSuccessfulDataValidationBarCount ?? 0;
   setText(prefix + "Latest", provider.validated
    ? bars + " bars returned · last validated " + validation
    : "No successful non-empty response · " + validation);
   setText(prefix + "Coverage", "Not measured by status endpoint; run a date-window comparison");
   setText(prefix + "Recovery", "Historical REST only · collector/restart state loading…");
   const caps = (provider.capabilities || []).join("; ");
   const feedMatch = caps.match(/Configured feed: ([^;]+)/i);
   const configuredFeed = provider.configuredFeed || feedMatch?.[1];
   if (prefix === "alpaca" && configuredFeed) {
    const feedNode = document.querySelector("#alpacaTitle")?.closest(".provider-card")?.querySelector(".provider-card-heading .chip");
    if (feedNode) feedNode.textContent = "Configured feed: " + configuredFeed.toUpperCase();
   }
  }
  try {
   const collectorResponse = await fetch("/api/market-data/collector/status", {cache:"no-store",headers:{Accept:"application/json"}});
   if (collectorResponse.ok) {
    const collector = await collectorResponse.json();
    const summary = collector.enabled
     ? "Collector cycles " + collector.cycles + " · new observations " + collector.newlyPersistedObservations + " · last-cycle errors " + collector.lastCycleErrors
     : "Collector disabled · configure MarketData:Collector:Symbols";
    setText("alpacaRecovery", summary + " · restart recovery still needs a controlled test");
   }
  } catch { setText("alpacaRecovery", "Collector status unavailable"); }
 } catch {
  setText("alpacaConnection", "API NOT CONNECTED (static preview or API unavailable)");
  setText("alpacaLatest", "DATA NOT LOADED");
  setText("alpacaCoverage", "Not measured");
  setText("alpacaRecovery", "Not run");
  setText("massiveConnection", "API NOT CONNECTED (static preview or API unavailable)");
  setText("massiveLatest", "DATA NOT LOADED");
  setText("massiveCoverage", "Not measured");
  setText("massiveRecovery", "Not run");
 }
}
loadProviderStatus();

async function ensureMinuteBarSubscription(symbol){
 const ticker=String(symbol||"").trim().toUpperCase();
 if(!/^[A-Z0-9.-]{1,8}$/.test(ticker)||requestedMinuteBarSymbols.has(ticker))return;
 requestedMinuteBarSymbols.add(ticker);
 try{
  const response=await fetch("/api/market-data/stream/subscribe?symbol="+encodeURIComponent(ticker),{method:"POST",cache:"no-store",headers:{Accept:"application/json"}});
  if(!response.ok)throw new Error("HTTP "+response.status);
  const result=await response.json();
  if(!result.accepted&&result.state!=="DISABLED"&&result.state!=="MISCONFIGURED")requestedMinuteBarSymbols.delete(ticker);
 }catch(error){/* REST assessment remains the fallback when streaming is unavailable. */}
}
async function loadMinuteBarStreamStatus(){
 const node=$("streamStatus");if(!node)return;
 try{
  const response=await fetch("/api/market-data/stream/status",{cache:"no-store",headers:{Accept:"application/json"}});
  if(!response.ok)throw new Error("HTTP "+response.status);
  const state=await response.json();
  const selected=$("symbol")?.value;
  if(state.enabled&&selected)void ensureMinuteBarSubscription(selected);
  const subscribed=state.subscribedSymbols||[],barStats=state.barsBySymbol||{};
  node.textContent=!state.enabled?"WS OFF":state.connected
   ?"WS OK · "+(state.feed||"feed")+" · "+subscribed.length+" subs · "+(state.persistedBars??0)+" bars"
   :"WS "+(state.state||"UNKNOWN");
  const perSymbol=Object.entries(barStats).map(([ticker,info])=>ticker+" "+info.barCount+" bars · "+(info.lastBarUtc||"no bar")).join("; ");
  node.title="MCC dynamically subscribes the selected symbol and Red Zone candidates. Configured symbols are only the initial seed. Acknowledged: "+(subscribed.join(", ")||"none")+" · Per-symbol bars: "+(perSymbol||"none yet")+" · Last stream bar: "+(state.lastBarUtc||"none")+(state.lastError?" · Last error: "+state.lastError:"");
 }catch(error){node.textContent="WS minute bars: STATUS UNAVAILABLE";node.title=error.message;}
}
void loadMinuteBarStreamStatus();

let morningBriefCache=null,morningBriefLoadedAt=0,morningBriefRequest=null;
async function loadMorningBrief(force=false) {
 if(!force&&morningBriefCache&&Date.now()-morningBriefLoadedAt<60000)return morningBriefCache;
 if(morningBriefRequest)return morningBriefRequest;
 morningBriefRequest=(async()=>{
  try {
   const response = await fetch("/api/morning-brief", {headers:{Accept:"application/json"},cache:"no-store"});
   if (!response.ok) throw new Error("HTTP " + response.status);
   const report = await response.json();
   morningBriefCache=report;morningBriefLoadedAt=Date.now();
   $("overnightServiceState").textContent = "Morning Brief API: " + report.status;
   $("overnightServiceDetail").textContent = "Generated " + new Date(report.generatedAtUtc).toLocaleString("en-US",{timeZone:"America/Phoenix"}) + " Arizona. Benchmark bars, Alpaca mover observations, sector ETF proxies, and unclassified news headlines are source data only; no trading decision is inferred.";
   $("reportSubtitle").textContent = "API report generated " + new Date(report.generatedAtUtc).toLocaleString("en-US",{timeZone:"America/Phoenix"}) + " Arizona · source timestamps retained in response";
   const market = report.marketLandscape || [];
   const lines = market.map(item => {
    const obs = (item.observations || []).map(o => o.provider + ": " + (o.latest ? formatBar(o.latest) : (o.status || "DATA?"))).join(" | ");
    const moves = (item.observations || []).map(o => o.changeFromFirstPercent == null ? "" : o.provider + " window change " + o.changeFromFirstPercent + "%").filter(Boolean).join(" | ");
    return item.symbol + " — " + (obs || "DATA? No configured market-data source.") + (moves ? " · " + moves : "");
   });
   const movers = report.movers || {};
   lines.push("Movers: " + (movers.status || "UNKNOWN") + " · " + (movers.recentDiscoveryCount ?? 0) + " recent persisted observations · " + (movers.source || "source unknown"));
   const sector = report.sectorFlow || {};
   lines.push("Sector proxies: " + (sector.status || "DATA?") + " · " + ((sector.proxySymbols || []).join(", ") || "DATA NOT FOUND"));
   const breadth = report.breadth || {};
   lines.push("Breadth: " + (breadth.status || "DATA NOT FOUND IN SOURCES") + " · " + (breadth.note || "Full-market advancing/declining breadth is not validated."));
   const headlines = report.newsHeadlines || {};
   lines.push("News: " + (headlines.status || "UNKNOWN") + " · source " + (headlines.source || "unknown"));
   const snapshot=$("morningApiSnapshot");
   if(snapshot){
    const nodes=lines.map(line=>{const p=document.createElement("p");p.textContent=line;return p;});
    (headlines.items||[]).slice(0,8).forEach(item=>{
     const p=document.createElement("p");
     p.append(document.createTextNode((item.headline||"Headline unavailable")+" · "+(item.symbols||[]).join(", ")+" · published "+(item.publishedAtUtc?new Date(item.publishedAtUtc).toLocaleString("en-US",{timeZone:"America/Phoenix"})+" Arizona":"UNKNOWN")+" · "));
     if(item.url){const a=document.createElement("a");a.href=item.url;a.target="_blank";a.rel="noopener noreferrer";a.textContent="News Link";a.title=item.url;p.append(a);}else{p.append(document.createTextNode("News Link unavailable"));}
     nodes.push(p);
    });
    snapshot.replaceChildren(...nodes);
   }
   return report;
  } catch(error) {
   $("overnightServiceState").textContent = "Morning Brief API: NOT CONNECTED";
   $("overnightServiceDetail").textContent = "Morning Brief request failed: "+error.message+". Direct file preview remains demo-only.";
   return null;
  }
 })();
 try{return await morningBriefRequest;}finally{morningBriefRequest=null;}
}
// Morning Brief loads on demand from its modal; it must not block dashboard startup.

/// MODULE: Morning Command Brief modal
/// PURPOSE: Reuse the same live API brief in the header modal and the collapsed full-width footer section.
/// SAFETY: The modal is a read-only snapshot; export delegates to the existing JSON export action.
const morningBriefDialog=$("morningBriefDialog");
function renderMorningBriefModal(){
 const source=$("morning-brief"),host=$("morningBriefModalContent");
 if(!source||!host)return;
 const clone=source.cloneNode(true);
 clone.removeAttribute("id");
 clone.querySelector(".section-heading")?.remove();
 clone.querySelectorAll("[id]").forEach(node=>node.removeAttribute("id"));
 clone.querySelectorAll("button").forEach(button=>button.remove());
 host.replaceChildren(clone);
}
$("openMorningBrief")?.addEventListener("click",()=>{
 if(!morningBriefDialog||morningBriefDialog.open)return;
 renderMorningBriefModal();
 morningBriefDialog.showModal();
 if(!morningBriefCache||Date.now()-morningBriefLoadedAt>=60000)
  void loadMorningBrief(true).then(renderMorningBriefModal);
});
$("closeMorningBrief")?.addEventListener("click",()=>morningBriefDialog?.close());
$("exportMorningReportModal")?.addEventListener("click",()=>$("exportMorningReport")?.click());

/* API-backed exclusion watchlist and StrategyReport upload. */
let latestStrategyReportJson = null;
async function refreshExclusions() {
 const status = $("exclusionStatus"), list = $("exclusionRows");
 if (!status || !list) return;
 status.textContent = "Loading exclusions from MCC API…";
 try {
  const response = await fetch("/api/watchlists/exclusions", {headers:{Accept:"application/json"}});
  if (!response.ok) throw new Error("HTTP " + response.status);
  const data = await response.json();
  list.replaceChildren(...(data.symbols || []).map(entry => {
   const li = document.createElement("li");
   const detail = document.createElement("span");
   detail.textContent = entry.symbol + " — " + entry.reason;
   const remove = document.createElement("button");
   remove.type = "button"; remove.className = "quiet-button"; remove.textContent = "Remove";
   remove.addEventListener("click", async () => {
    remove.disabled = true;
    try {
     const res = await fetch("/api/watchlists/exclusions/" + encodeURIComponent(entry.symbol), {method:"DELETE"});
     if (!res.ok) throw new Error("HTTP " + res.status);
     await refreshExclusions();
    } catch (e) { status.textContent = "Could not remove symbol: " + e.message; remove.disabled = false; }
   });
   li.append(detail, remove);
   return li;
  }));
  status.textContent = data.count + " excluded symbol(s) · API-backed MCC list";
 } catch (e) {
  status.textContent = "Exclusion API unavailable: " + e.message + ". Serve this page through the MCC API.";
 }
}
$("refreshExclusions")?.addEventListener("click", refreshExclusions);
$("exclusionForm")?.addEventListener("submit", async event => {
 event.preventDefault();
 const symbol = $("exclusionSymbol").value.trim().toUpperCase();
 const reason = $("exclusionReason").value.trim();
 const status = $("exclusionStatus");
 try {
  const response = await fetch("/api/watchlists/exclusions", {
   method:"POST", headers:{"Content-Type":"application/json","Accept":"application/json"},
   body:JSON.stringify({symbol,reason:reason || null})
  });
  const body = await response.json();
  if (!response.ok) throw new Error(body.error || ("HTTP " + response.status));
  $("exclusionSymbol").value = ""; $("exclusionReason").value = "";
  await refreshExclusions();
 } catch (e) { status.textContent = "Could not add exclusion: " + e.message; }
});
$("strategyReportForm")?.addEventListener("submit", async event => {
 event.preventDefault();
 const file = $("strategyReportFile").files?.[0];
 const status = $("strategyReportStatus"), summary = $("strategyReportSummary"), download = $("downloadStrategyReportJson"), parityDownload = $("downloadParityCsv");
 if (!file) { status.textContent = "Choose a CSV file first."; return; }
 status.textContent = "Uploading and parsing " + file.name + "…";
 summary.classList.add("hidden"); download.classList.add("hidden"); parityDownload?.classList.add("hidden"); latestStrategyReportJson = null;
 try {
  const form = new FormData(); form.append("file", file, file.name);
  const response = await fetch("/api/strategy-report/upload", {method:"POST",body:form,headers:{Accept:"application/json"}});
  const body = await response.json();
  if (!response.ok) throw new Error(body.error || ("HTTP " + response.status));
  latestStrategyReportJson = body;
  const parsed = body.parsed;
  summary.replaceChildren();
  const intro = document.createElement("p");
  intro.textContent = body.fileName + " · " + parsed.parsedRowCount + " parsed rows · " + parsed.headers.length + " columns";
  summary.append(intro);
  const headers = document.createElement("p");
  headers.textContent = "Columns: " + parsed.headers.join(", ");
  summary.append(headers);
  if (parsed.warnings?.length) {
   const warning = document.createElement("p"); warning.className = "upload-warning";
   warning.textContent = "Warnings: " + parsed.warnings.slice(0,8).join(" | ");
   summary.append(warning);
  }
  const preview = document.createElement("pre");
  preview.textContent = JSON.stringify(parsed.previewRows || [], null, 2);
  summary.append(preview);
  summary.classList.remove("hidden"); download.classList.remove("hidden");
  if ((parsed.parityRows || []).length > 0) parityDownload?.classList.remove("hidden");
  status.textContent = "Parse complete. " + (parsed.parityRows || []).length + " MCC OHLCV row(s) converted to the Chart Parity Template.";
 } catch (e) {
  status.textContent = "StrategyReport upload failed: " + e.message;
 }
});
$("downloadStrategyReportJson")?.addEventListener("click", () => {
 if (!latestStrategyReportJson) return;
 const blob = new Blob([JSON.stringify(latestStrategyReportJson, null, 2)], {type:"application/json"});
 const url = URL.createObjectURL(blob);
 const link = document.createElement("a"); link.href = url; link.download = "strategy-report-parsed.json";
 document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url);
});
refreshExclusions();

$("downloadParityCsv")?.addEventListener("click", () => {
 const rows = latestStrategyReportJson?.parsed?.parityRows || [];
 if (!rows.length) return;
 const headers = ["SourceRow","Symbol","Timeframe","PayloadTimestampUtc","PayloadOpen","PayloadHigh","PayloadLow","PayloadClose","PayloadVolume","ChartTimestamp","ChartOpen","ChartHigh","ChartLow","ChartClose","ChartVolume","ChartTimestampMeaning","Finding"];
 const quote = value => '"' + String(value ?? "").replace(/"/g, '""') + '"';
 const csv = [headers, ...rows.map(row => headers.map(header => row[header] ?? ""))]
   .map(row => row.map(quote).join(",")).join("\r\n");
 const blob = new Blob(["\uFEFF", csv], {type:"text/csv;charset=utf-8"});
 const url = URL.createObjectURL(blob);
 const link = document.createElement("a"); link.href = url; link.download = "mcc-chart-parity-template.csv";
 document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url);
});



/*
/// MODULE: Scan & Analyze (Arizona-local input, Alpaca 1M + 5M bars)
/// PURPOSE: Fetch both supported timeframes for each user-supplied symbol and summarize returned bars.
/// WHEN CALLED: Submits the Scan & Analyze form on the API-served dashboard.
/// SAFETY: Descriptive OHLCV only; never fabricates Thinkorswim states or a Boring Overnight qualification.
*/
function phoenixParts(date) {
 const parts = new Intl.DateTimeFormat("en-CA", {timeZone:"America/Phoenix",year:"numeric",month:"2-digit",day:"2-digit",hour:"2-digit",minute:"2-digit",hourCycle:"h23"}).formatToParts(date);
 return Object.fromEntries(parts.filter(p=>p.type!=="literal").map(p=>[p.type,p.value]));
}
function phoenixLocalInput(date) {
 const p=phoenixParts(date);
 return p.year+"-"+p.month+"-"+p.day+"T"+p.hour+":"+p.minute;
}
function phoenixLocalToUtc(value) {
 const match=/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
 if(!match) throw new Error("Enter a valid Arizona local date and time.");
 const target={year:+match[1],month:+match[2],day:+match[3],hour:+match[4],minute:+match[5]};
 const targetAsUtc=Date.UTC(target.year,target.month-1,target.day,target.hour,target.minute);
 let guess=targetAsUtc;
 for(let i=0;i<4;i++){
  const p=phoenixParts(new Date(guess));
  const represented=Date.UTC(+p.year,+p.month-1,+p.day,+p.hour,+p.minute);
  const delta=targetAsUtc-represented;
  if(delta===0) return new Date(guess).toISOString();
  guess+=delta;
 }
 const p=phoenixParts(new Date(guess));
 if(+p.year!==target.year||+p.month!==target.month||+p.day!==target.day||+p.hour!==target.hour||+p.minute!==target.minute)
  throw new Error("Could not resolve that Arizona local time.");
 return new Date(guess).toISOString();
}
function summarizeBars(payload) {
 const bars=(Array.isArray(payload.bars)?payload.bars:[]).filter(b=>b&&Number.isFinite(new Date(b.timestamp).getTime())).sort((a,b)=>new Date(a.timestamp)-new Date(b.timestamp));
 if(!bars.length) return {count:0,latestAge:null,change:null,range:null,volume:null,first:null,last:null,feed:payload.feed||"unknown"};
 const first=bars[0],last=bars[bars.length-1];
 const enoughBars=bars.length>=2;
 const highs=bars.filter(b=>b.high!==null&&b.high!==undefined).map(b=>Number(b.high)).filter(Number.isFinite),lows=bars.filter(b=>b.low!==null&&b.low!==undefined).map(b=>Number(b.low)).filter(Number.isFinite);
 const max=highs.length?Math.max(...highs):null,min=lows.length?Math.min(...lows):null;
 const volume=bars.reduce((sum,b)=>sum+(Number(b.volume)||0),0);
 return {
  count:bars.length,latestAge:Math.max(0,(Date.now()-new Date(last.timestamp).getTime())/60000),
  change:enoughBars&&Number.isFinite(Number(first.close))&&Number.isFinite(Number(last.close))&&Number(first.close)!==0?(Number(last.close)/Number(first.close)-1)*100:null,
  range:enoughBars&&min!==null&&max!==null&&Number(first.close)!==0?(max-min)/Number(first.close)*100:null,
  volume,first:first.timestamp,last:last.timestamp,feed:payload.feed||"unknown"
 };
}
async function getBarsForScan(symbol,timeframe,fromUtc,toUtc) {
 const params=new URLSearchParams({symbol,timeframe,from:fromUtc,to:toUtc,providerName:"Alpaca"});
 const response=await fetch("/api/market-data/bars?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
 const body=await response.json();
 if(!response.ok) throw new Error(body.detail||body.error||("HTTP "+response.status));
 return summarizeBars(body);
}
const scanAnalyzeForm=$("scanAnalyzeForm");
if(scanAnalyzeForm) {
 const runButton=$("apiScanRun");
 if(runButton) runButton.textContent="Scan & Analyze (1M + 5M)";
 const fromInput=$("apiScanFrom"),toInput=$("apiScanTo");
 if(fromInput&&toInput) {
  const now=new Date();
  fromInput.value=phoenixLocalInput(new Date(now.getTime()-6*60*60*1000));
  toInput.value=phoenixLocalInput(now);
  fromInput.setAttribute("aria-label","From time in Arizona local time");
  toInput.setAttribute("aria-label","To time in Arizona local time");
 }
 scanAnalyzeForm.addEventListener("submit",async event=>{
  event.preventDefault();
  const button=$("apiScanRun"),source=$("scannerSource"),rows=$("scannerRows");
  const symbols=[...new Set($("apiScanSymbols").value.trim().split(/[\s,;]+/).filter(Boolean).map(s=>s.toUpperCase()))];
  if(!symbols.length||symbols.length>25){source.textContent="Enter 1–25 unique symbols. Scan & Analyze retrieves two timeframes per symbol.";return;}
  if(symbols.some(s=>!/^[A-Z0-9.-]{1,8}$/.test(s))){source.textContent="One or more symbols contain invalid characters.";return;}
  let fromUtc,toUtc;
  try {fromUtc=phoenixLocalToUtc(fromInput.value);toUtc=phoenixLocalToUtc(toInput.value);}
  catch(error){source.textContent=error.message;return;}
  if(new Date(fromUtc)>=new Date(toUtc)||new Date(toUtc)-new Date(fromUtc)>31*86400000){source.textContent="Require From before To and a maximum 31-day window.";return;}
  button.disabled=true;rows.replaceChildren();source.textContent="Scanning "+symbols.length+" symbols on Alpaca 1M and 5M…";
  const results=[];
  try {
   for(let offset=0;offset<symbols.length;offset+=3) {
    const batch=symbols.slice(offset,offset+3);
    const batchResults=await Promise.all(batch.map(async symbol=>{
     const [one,five]=await Promise.all([
      getBarsForScan(symbol,"1m",fromUtc,toUtc).catch(error=>({error:error.message,count:0})),
      getBarsForScan(symbol,"5m",fromUtc,toUtc).catch(error=>({error:error.message,count:0}))
     ]);
     return {symbol,one,five};
    }));
    results.push(...batchResults);
    source.textContent="Scanned "+results.length+" / "+symbols.length+" symbols…";
   }
   rows.replaceChildren(...results.map(item=>{
    const tr=document.createElement("tr"),cell=value=>{const td=document.createElement("td");td.textContent=value??"DATA?";return td;};
    const bothPresent=item.one.count>=2&&item.five.count>=2;
     const errors=[item.one.error?"1M: "+item.one.error:"",item.five.error?"5M: "+item.five.error:""].filter(Boolean);
     const sparse=[item.one.count===1?"1M returned only 1 bar; change/range unavailable":item.one.count===0?"1M returned no bars":"",item.five.count===1?"5M returned only 1 bar; change/range unavailable":item.five.count===0?"5M returned no bars":""].filter(Boolean);
     const status=bothPresent?"OBSERVED · NOT QUALIFIED":"INSUFFICIENT DATA";
    const formatArizonaTimestamp=value=>{const date=new Date(value);return Number.isNaN(date.getTime())?"DATA?":new Intl.DateTimeFormat("en-US",{timeZone:"America/Phoenix",year:"numeric",month:"2-digit",day:"2-digit",hour:"2-digit",minute:"2-digit",second:"2-digit",hour12:false}).format(date)+" Arizona";};
     const summary=s=>s.count?(s.count+" bars; "+(s.change===null?"change unavailable (need ≥2 valid bars)":s.change.toFixed(2)+"% change")+"; "+(s.range===null?"range unavailable (need ≥2 valid bars)":s.range.toFixed(2)+"% range")+"; vol "+Math.round(s.volume)+"; latest "+formatArizonaTimestamp(s.last)):(s.error||"No bars returned");
     tr.append(cell(item.symbol),cell(item.five.change===null||item.five.change===undefined?"— (insufficient bars)":(item.five.change>=0?"+":"")+item.five.change.toFixed(2)+"% (5M)"),cell(status),cell("5M "+(item.five.range==null?"DATA? (need ≥2 bars)":item.five.range.toFixed(2)+"%")+" · 1M "+(item.one.range==null?"DATA? (need ≥2 bars)":item.one.range.toFixed(2)+"%")),cell("5M "+(item.five.latestAge==null?"UNKNOWN":item.five.latestAge.toFixed(1)+"m")+" · 1M "+(item.one.latestAge==null?"UNKNOWN":item.one.latestAge.toFixed(1)+"m")),cell("5M: "+summary(item.five)+" | 1M: "+summary(item.one)+(sparse.length?" | COVERAGE WARNING: "+sparse.join("; "):"")+(errors.length?" | Errors: "+errors.join("; "):"")),document.createElement("td"));
    const watch=document.createElement("button");watch.type="button";watch.className="row-action";watch.textContent="Watch";watch.addEventListener("click",()=>addWatch(item.symbol));tr.lastElementChild.append(watch);return tr;
   }));
   const errors=results.reduce((n,r)=>n+Number(Boolean(r.one.error))+Number(Boolean(r.five.error)),0);
   source.textContent="Scan complete · "+results.length+" symbols · 1M + 5M · Alpaca · "+errors+" timeframe request errors · Arizona "+fromInput.value+" → "+toInput.value+" ("+fromUtc+" → "+toUtc+" UTC). OHLCV summaries only; qualification gates are not connected.";
   let option=document.querySelector('#scannerGroup option[value="scanAnalyze"]');
   if(!option){option=document.createElement("option");option.value="scanAnalyze";option.textContent="Scan & Analyze results";$("scannerGroup").append(option);}
   $("scannerGroup").value="scanAnalyze";
  } catch(error) {source.textContent="Scan & Analyze failed: "+error.message;}
  finally {button.disabled=false;}
 });
}


/*
/// MODULE: Boring Overnight bar retrieval and audit view
/// PURPOSE: Fetch user-selected 1m/5m bars through the API and display persisted evaluation snapshots.
/// WHEN CALLED: User switches to Overnight mode and submits the Overnight form.
/// IMPORTANT: This does not synthesize session boundaries or qualify a candidate. Provider coverage is reported as returned.
*/
function overnightArizonaTimestamp(value) {
 const date = new Date(value);
 if (!Number.isFinite(date.getTime())) return "DATA?";
 return new Intl.DateTimeFormat("en-US", {
  timeZone:"America/Phoenix", year:"numeric", month:"2-digit", day:"2-digit",
  hour:"2-digit", minute:"2-digit", second:"2-digit", hour12:false
 }).format(date) + " Arizona";
}
function resetOvernightSellLimit(reason) {
 const price=$("overnightSellLimitPrice"), close=$("overnightSellLimitClose"), method=$("overnightSellLimitMethod"), validation=$("overnightSellLimitValidation"), note=$("overnightSellLimitReason");
 if(price)price.textContent="DATA? — NOT CALCULATED";
 if(close)close.textContent="DATA?";
 if(method)method.textContent="Historical eligible-window target distribution + ATR context";
 if(validation)validation.textContent="NOT VERIFIED";
 if(note)note.textContent=reason||"No numeric sell limit is issued until source-backed historical target-touch data, a fresh reference close, and ATR parity/context are available. A bar high touching a limit is not an actual fill.";
}
resetOvernightSellLimit();
function initializeOvernightWindow() {
 const from = $("overnightFrom"), to = $("overnightTo");
 if (!from || !to || (from.value && to.value)) return;
 const now = new Date(), p = phoenixParts(now);
 const hour = Number(p.hour), minute = Number(p.minute);
 const start = new Date(now.getTime());
 if (hour < 13 || (hour === 13 && minute === 0)) start.setDate(start.getDate() - 1);
 const startParts = phoenixParts(start);
 from.value = startParts.year+"-"+startParts.month+"-"+startParts.day+"T13:00";
 to.value = phoenixLocalInput(now);
}
async function overnightFetchJson(url, options) {
 const response = await fetch(url, {cache:"no-store", headers:{Accept:"application/json",...(options&&options.headers||{})}, ...(options||{})});
 const body = await response.json().catch(()=>({}));
 if (!response.ok) throw new Error(body.error || body.detail || ("HTTP "+response.status));
 return body;
}
async function refreshOvernightSnapshots() {
 const status = $("overnightSnapshotStatus") || $("overnightStatus"), rows = $("overnightSnapshotRows");
 if (!status || !rows) return;
 status.textContent = "Loading saved Overnight evaluations…";
 try {
  const symbol = $("overnightSymbol").value.trim().toUpperCase();
  const params = new URLSearchParams({limit:"50"});
  if (/^[A-Z0-9.-]{1,8}$/.test(symbol)) params.set("symbol",symbol);
  const data = await overnightFetchJson("/api/boring-overnight/snapshots?"+params.toString());
  const snapshots = Array.isArray(data.snapshots) ? data.snapshots : [];
  rows.replaceChildren(...(snapshots.length ? snapshots.map(snapshot => {
   const tr = document.createElement("tr");
   const request = snapshot.evaluationRequest || snapshot.EvaluationRequest || {};
   const history = snapshot.historicalCompleteness || snapshot.HistoricalCompleteness || {};
   const sourceTimestamps = snapshot.fieldSourceTimestampsUtc || snapshot.FieldSourceTimestampsUtc || {};
   const dailyEvidence = snapshot.dailyEvidence || snapshot.DailyEvidence || {};
   const catalystEvidence = snapshot.catalystEvidence || snapshot.CatalystEvidence || {};
   const validPairs = history.validPairCount ?? history.ValidPairCount;
   const requestedPairs = history.requestedPairCount ?? history.RequestedPairCount;
   const missingPairs = history.missingPairCount ?? history.MissingPairCount;
   const historyLabel = validPairs === undefined
    ? "UNKNOWN"
    : String(validPairs)+"/"+String(requestedPairs)+" valid · "+String(missingPairs)+" missing · "+(history.status || history.Status || "UNKNOWN");
   const fmt = value => value === null || value === undefined ? "UNKNOWN" :
    Number.isFinite(Number(value)) ? Number(value).toLocaleString(undefined,{maximumFractionDigits:4}) : String(value);
   const dailyLabel = [
    "price="+fmt(request.price ?? request.Price),
    "SMA20="+fmt(request.sma20 ?? request.Sma20),
    "avgVol20="+fmt(request.averageDailyVolume20 ?? request.AverageDailyVolume20),
    "feedCoverage="+(dailyEvidence.feedCoverage || dailyEvidence.FeedCoverage || "UNKNOWN"),
    "qualUsable="+(dailyEvidence.qualificationUsable ?? dailyEvidence.QualificationUsable ?? "UNKNOWN"),
    "adjustment="+(dailyEvidence.adjustment || dailyEvidence.Adjustment || "UNKNOWN"),
    "bar="+(sourceTimestamps.price || sourceTimestamps.Price || "UNKNOWN")
   ].join(" · ");
   const catalystArticles = catalystEvidence.articles || catalystEvidence.Articles || [];
   const catalystHeadlines = catalystArticles.slice(0,2).map(item => item.headline || item.Headline || "").filter(Boolean);
   const catalystLabel = [
    "catalyst="+(request.catalystStatus || request.CatalystStatus || "UNKNOWN"),
    "news="+(catalystEvidence.articleCount ?? catalystEvidence.ArticleCount ?? "UNKNOWN"),
    "coverage="+(catalystEvidence.coverageComplete ?? catalystEvidence.CoverageComplete ?? "UNKNOWN"),
    "rules="+(catalystEvidence.ruleSetVersion || catalystEvidence.RuleSetVersion || "UNKNOWN"),
    catalystHeadlines.length ? "headlines="+catalystHeadlines.join(" | ").slice(0,180) : null
   ].filter(Boolean).join(" · ");
   const gateInputs = [
    "dataFresh="+(request.dataFresh ?? request.DataFresh ?? "UNKNOWN"),
    catalystLabel,
    "5M permission="+(request.fiveMinutePermission ?? request.FiveMinutePermission ?? "UNKNOWN"),
    "5M structure="+(request.fiveMinuteStructure || request.FiveMinuteStructure || "UNKNOWN"),
    "1M state="+(request.oneMinuteState || request.OneMinuteState || "UNKNOWN")
   ].join(" · ");
   const values = [
    snapshot.symbol || snapshot.Symbol || "DATA?",
    snapshot.asOfUtc || snapshot.AsOfUtc || "DATA?",
    snapshot.recordedAtUtc || snapshot.RecordedAtUtc || "DATA?",
    (snapshot.provider || snapshot.Provider || "DATA?")+" / "+(snapshot.feed || snapshot.Feed || "DATA?"),
    "INPUT SNAPSHOT — NOT A DECISION",
    historyLabel,
    dailyLabel,
    gateInputs
   ];
   for (const value of values) { const td=document.createElement("td"); td.textContent=String(value); tr.append(td); }
   return tr;
  }) : [Object.assign(document.createElement("tr"),{innerHTML:"<td colspan='8'>No saved evaluations found for this query.</td>"})]));
  status.textContent = "Loaded "+snapshots.length+" saved input snapshot(s); GET returns inputs, not the evaluator decision. "+(data.note||"");
 } catch(error) {
  status.textContent = "Could not load saved evaluations: "+error.message;
 }
}
/// MODULE: One-click 20-session Overnight close/open capture
/// PURPOSE: Ask the API to retrieve and persist provider 1m bars, generate session boundaries, and calculate 20 close/open pairs.
$("overnightCapture20")?.addEventListener("click", async () => {
 const button=$("overnightCapture20"), status=$("overnightHistoryStatus");
 const symbol=$("overnightSymbol").value.trim().toUpperCase();
 if(!/^[A-Z0-9.-]{1,8}$/.test(symbol)){status.textContent="Enter a valid Overnight candidate symbol first.";return;}
 const fields=["overnightHistoryPairs","overnightHistoryWinRate","overnightHistoryMean20","overnightHistoryMedian20","overnightHistoryMean5","overnightHistoryMissing","overnightHistoryProvider","overnightHistoryBars","overnightHistoryCalendar","overnightHistoryAsOf"];
 for(const id of fields){const el=$(id);if(el)el.textContent="REQUEST IN PROGRESS";}
 const rows=$("overnightHistoryRows"); rows.replaceChildren();
 button.disabled=true; const captureStartedAt=Date.now(); status.textContent="Fetching Alpaca 1-minute bars and calculating the latest 20 regular-session close/open pairs for "+symbol+"…";
 const captureProgressTimer=setInterval(()=>{const elapsed=Math.floor((Date.now()-captureStartedAt)/1000),minutes=Math.floor(elapsed/60),seconds=elapsed%60;status.textContent="Still working · Alpaca 1-minute history capture for "+symbol+" · "+minutes+"m "+String(seconds).padStart(2,"0")+"s elapsed · waiting for provider fetch, persistence and exact-boundary calculation.";},10000);
 try{
  const data=await overnightFetchJson("/api/boring-overnight/capture-history",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({symbol,providerName:"Alpaca",requiredWindow:20})});
  const h=data.history||{};
  const pct=v=>v===null||v===undefined?"DATA?":Number(v).toFixed(3)+"%";
  $("overnightHistoryPairs").textContent=(h.validPairCount??0)+" / "+(h.requiredWindow??20);
  $("overnightHistoryWinRate").textContent=pct(h.winRatePct20D);
  $("overnightHistoryMean20").textContent=pct(h.averageReturnPct20D);
  $("overnightHistoryMedian20").textContent=pct(h.medianReturnPct);
  $("overnightHistoryMean5").textContent=pct(h.averageReturnPct5D);
  $("overnightHistoryMissing").textContent=String(h.missingPairCount??"DATA?");
  $("overnightHistoryProvider").textContent=(data.provider||"DATA?")+" / "+(data.feed||"FEED UNKNOWN");
  $("overnightHistoryBars").textContent=(data.fetchedBarCount??0)+" fetched / "+(data.newlyPersistedObservationCount??0)+" newly stored";
  $("overnightHistoryCalendar").textContent=data.calendarVersion||"NOT REPORTED";
  $("overnightHistoryAsOf").textContent=h.calculatedAtUtc||data.retrievedAtUtc||"DATA?";
  const returns=Array.isArray(h.returns)?h.returns:[];
  for(const item of returns){
   const tr=document.createElement("tr");
   const values=[item.nextSessionDate,item.previousClose,item.nextOpen,pct(item.returnPct),item.previousCloseBarTimestampUtc+" → "+item.nextOpenBarTimestampUtc];
   for(const value of values){const td=document.createElement("td");td.textContent=String(value??"DATA?");tr.append(td);}
   rows.append(tr);
  }
  const missing=Array.isArray(h.missing)?h.missing:[];
  for(const item of missing){
   const tr=document.createElement("tr");
   const values=[item.nextSessionDate,"MISSING",item.reasonCode,"DATA?","Expected "+item.missingTimestampUtc];
   for(const value of values){const td=document.createElement("td");td.textContent=String(value??"DATA?");tr.append(td);}
   rows.append(tr);
  }
  if(!rows.children.length){const tr=document.createElement("tr");const td=document.createElement("td");td.colSpan=5;td.textContent="No close/open pairs were returned.";tr.append(td);rows.append(tr);}
  const auditRows=$("overnightBoundaryCoverageRows");if(auditRows){
   auditRows.replaceChildren();
   const audit=Array.isArray(data.boundaryCoverageAudit)?data.boundaryCoverageAudit:[];
   for(const item of audit){
    const tr=document.createElement("tr");
    const values=[item.expectedTimestampUtc?.slice(0,10)||"DATA?",item.boundaryType||"UNKNOWN",item.expectedTimestampUtc||"DATA?",item.providerReturnedExactBar===true?"YES":item.providerReturnedExactBar===false?"NO":"DATA?",item.storeHasExactBar===true?"YES":item.storeHasExactBar===false?"NO":"DATA?",item.nearestProviderBarTimestampUtc?(item.nearestProviderBarTimestampUtc+" ("+(item.nearestProviderBarOffsetMinutes??"UNKNOWN")+" min)"):"NO NEARBY PROVIDER BAR",item.status||"UNKNOWN"];
    for(const value of values){const td=document.createElement("td");td.textContent=String(value);tr.append(td);}auditRows.append(tr);
   }
   if(!audit.length){const tr=document.createElement("tr"),td=document.createElement("td");td.colSpan=7;td.textContent="Data not found in sources: API response did not include boundaryCoverageAudit.";tr.append(td);auditRows.append(tr);}
  }
  const audit=Array.isArray(data.boundaryCoverageAudit)?data.boundaryCoverageAudit:[];
  const providerExact=audit.filter(x=>x.providerReturnedExactBar===true).length;
  const storeExact=audit.filter(x=>x.storeHasExactBar===true).length;
  status.textContent=(h.allPairsComplete&&h.enoughHistory?"20-PAIR CAPTURE COMPLETE. ":"HISTORY INCOMPLETE — rolling metrics withheld. ")+(h.provenanceNote||data.note||"")+" Exact boundary audit: provider "+providerExact+"/"+audit.length+", store "+storeExact+"/"+audit.length+". Calendar: "+(data.calendarVersion||"unknown")+". Input snapshot "+(data.snapshot?.snapshotId||"NOT RETURNED")+" saved; evaluator "+(data.evaluation?.status||"UNKNOWN")+". Current price, Daily liquidity, catalyst and chart gates remain UNKNOWN until supplied."; 
  await refreshOvernightSnapshots();
 }catch(error){
  for(const id of fields){const el=$(id);if(el)el.textContent="DATA?";}
  const tr=document.createElement("tr"),td=document.createElement("td");td.colSpan=5;td.textContent="Capture failed: "+error.message;tr.append(td);rows.append(tr);
  status.textContent="20-session capture failed. No calculated metrics are displayed. "+error.message;
 }finally{clearInterval(captureProgressTimer);button.disabled=false;}

});

/// MODULE: GTC_EXT sell-window history capture
/// PURPOSE: Fetch/persist exact extended-hours 1m observations only when the effective Alpaca feed is SIP.
/// SAFETY: IEX is diagnostic only; no target is calculated from incomplete or partial-exchange coverage.
$("overnightCaptureSellWindow")?.addEventListener("click", async () => {
 const button=$("overnightCaptureSellWindow"),status=$("overnightSellWindowStatus");
 const symbol=($("overnightSymbol")?.value||"").trim().toUpperCase();
 if(!/^[A-Z0-9.-]{1,8}$/.test(symbol)){status.textContent="Enter a valid Overnight candidate symbol first.";return;}
 button.disabled=true;
 status.textContent="Checking the effective Alpaca feed before requesting GTC_EXT history…";
 const started=Date.now();
 const timer=setInterval(()=>{const elapsed=Math.floor((Date.now()-started)/1000);status.textContent="GTC_EXT capture running for "+symbol+" · "+elapsed+"s elapsed · fetching bounded 7-day 1-minute history chunks and auditing exact timestamps.";},10000);
 try{
  const statusResponse=await fetch("/api/providers/status",{cache:"no-store",headers:{Accept:"application/json"}});
  const providers=await statusResponse.json();
  if(!statusResponse.ok)throw new Error(providers.detail||providers.title||("Provider status HTTP "+statusResponse.status));
  const alpaca=Array.isArray(providers)?providers.find(x=>String(x.provider||"").toLowerCase()==="alpaca"):null;
  if(!alpaca)throw new Error("Alpaca provider status was not returned; capture was not started.");
  const effectiveFeed=String(alpaca.configuredFeed||"").trim().toLowerCase();
  if(effectiveFeed!=="sip"){
   status.textContent="NOT RUN — effective Alpaca feed is "+(effectiveFeed.toUpperCase()||"UNKNOWN")+". GTC_EXT qualification research requires SIP. No provider history request was made and no bars were persisted. Resolve SIP entitlement/configuration, restart MCC, and verify /api/providers/status before retrying.";
   return;
  }
  status.textContent="SIP confirmed as the effective configured feed. Fetching 20 exact GTC_EXT sell windows for "+symbol+"…";
  const response=await fetch("/api/boring-overnight/capture-sell-window-history",{method:"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},body:JSON.stringify({symbol,providerName:"Alpaca",feed:"sip"})});
  const data=await response.json();
  if(!response.ok)throw new Error(data.detail||data.title||data.error||("HTTP "+response.status));
  const windows=Array.isArray(data.windows)?data.windows:[];
  const missing=windows.reduce((sum,item)=>sum+(Number(item.missingIntervalCount)||0)+(item.referenceClose===null?1:0),0);
  const complete=data.completeWindowCount??0,required=data.requiredWindow??20;
  const samples=windows.flatMap(item=>(item.missingIntervalSampleUtc||[]).map(ts=>item.sellSessionDate+": "+ts)).slice(0,5);
  const qualification=data.qualificationUsable===true&&data.allWindowsComplete===true;
  status.textContent="Capture finished for "+symbol+" · "+complete+"/"+required+" complete windows · "+missing+" missing minute/reference observations · "+(data.fetchedBarCount??0)+" fetched / "+(data.newlyPersistedBarCount??0)+" newly stored · feed coverage "+(data.feedCoverage||"UNKNOWN")+" · qualification "+(qualification?"USABLE":"NOT USABLE")+". "+(samples.length?"Missing examples: "+samples.join(" | "):"No missing-minute samples were reported.")+" "+(data.note||"");
  const reason=$("overnightSellLimitReason");
  if(reason)reason.textContent=qualification
   ?"GTC_EXT SIP coverage is complete. The numeric target remains NOT CALCULATED until ATR parity is MATCH and walk-forward target-touch/adverse-excursion validation is complete. Coverage is not proof of executable quotes or fills."
   :"Sell target remains DATA? — NOT CALCULATED. Complete 20/20 exact SIP windows and ATR parity are required; missing intervals are not imputed.";
 }catch(error){
  status.textContent="GTC_EXT capture failed or was blocked: "+error.message+" No sell target was calculated.";
 }finally{
  clearInterval(timer);
  button.disabled=false;
 }
});
 
$("overnightBarsForm")?.addEventListener("submit", async event => {
 event.preventDefault();
 const button = $("overnightFetch"), status = $("overnightStatus");
 const symbol = $("overnightSymbol").value.trim().toUpperCase();
 if (!/^[A-Z0-9.-]{1,8}$/.test(symbol)) { status.textContent="Enter a valid symbol (1–8 letters, digits, dot, or hyphen)."; return; }
 let fromUtc, toUtc;
 try { fromUtc=phoenixLocalToUtc($("overnightFrom").value); toUtc=phoenixLocalToUtc($("overnightTo").value); }
 catch(error) { status.textContent=error.message; return; }
 if (new Date(fromUtc)>=new Date(toUtc) || new Date(toUtc)-new Date(fromUtc)>31*86400000) {
  status.textContent="Require From before To and a maximum 31-day window."; return;
 }
 button.disabled=true;
 const barsRequestStartedAt=performance.now();
 resetOvernightSellLimit("Current request is descriptive OHLCV only. The API has not returned a validated target distribution or an approved sell-limit calculation.");
 // Clear the previous response before starting a new request so failed requests cannot leave stale bars visible.
 $("overnightProvider").textContent="REQUEST IN PROGRESS";
 $("overnightCount").textContent="—";
 $("overnightFirst").textContent="—";
 $("overnightLatest").textContent="—";
 $("overnightAge").textContent="—";
 $("overnightGaps").textContent="—";
 $("overnightInterpretation").textContent="Waiting for the API response. No prior response is being reused.";
 status.textContent="Requesting "+symbol+" "+$("overnightTimeframe").value+" bars from Alpaca · Arizona "+$("overnightFrom").value+" → "+$("overnightTo").value+" · UTC "+fromUtc+" → "+toUtc+"…";
 try {
  const params = new URLSearchParams({symbol,timeframe:$("overnightTimeframe").value,from:fromUtc,to:toUtc,providerName:"Alpaca"});
  const data = await overnightFetchJson("/api/market-data/bars?"+params.toString());
  $("overnightProvider").textContent=(data.provider||"DATA?")+" / "+(data.feed||"feed not reported");
  $("overnightCount").textContent=String(data.barCount??0);
  $("overnightFirst").textContent=data.firstSourceTimestampUtc?overnightArizonaTimestamp(data.firstSourceTimestampUtc)+" · "+data.firstSourceTimestampUtc:"NO BARS";
  $("overnightLatest").textContent=data.latestSourceTimestampUtc?overnightArizonaTimestamp(data.latestSourceTimestampUtc)+" · "+data.latestSourceTimestampUtc:"NO BARS";
  const latest = data.latestSourceTimestampUtc ? new Date(data.latestSourceTimestampUtc) : null;
  $("overnightAge").textContent=latest?Math.max(0,(Date.now()-latest.getTime())/60000).toFixed(1)+" minutes":"UNKNOWN";
  $("overnightGaps").textContent=String(data.gapCount??"UNKNOWN");
  const bars=Array.isArray(data.bars)?data.bars:[];
  const extended=bars.filter(bar=>{
   const parts=phoenixParts(new Date(bar.timestamp));
   const hour=Number(parts.hour), minute=Number(parts.minute);
   return hour>13 || (hour===13 && minute>=0);
  }).length;
  $("overnightInterpretation").textContent=bars.length
   ? "Retrieved "+bars.length+" "+$("overnightTimeframe").value+" bars; "+extended+" have Arizona timestamps at or after 1:00 PM. Newly persisted observations: "+(data.newlyPersistedObservationCount??"UNKNOWN")+". Returned bars are source evidence only; inspect session coverage and feed before evaluating."
   : "The API returned zero bars for this symbol and time window. This is not proof of zero trading; verify provider status, symbol entitlement, feed and requested timestamps.";
  status.textContent="Bar request complete · "+symbol+" · "+$("overnightTimeframe").value+" · "+(data.barCount??0)+" bars · "+((performance.now()-barsRequestStartedAt)/1000).toFixed(1)+"s · Arizona "+$("overnightFrom").value+" → "+$("overnightTo").value+" · UTC "+fromUtc+" → "+toUtc+".";
  await refreshOvernightSnapshots();
 } catch(error) {
  $("overnightProvider").textContent="REQUEST FAILED · NO NEW DATA";
  $("overnightCount").textContent="NOT AVAILABLE";
  $("overnightFirst").textContent="NOT AVAILABLE";
  $("overnightLatest").textContent="NOT AVAILABLE";
  $("overnightAge").textContent="NOT AVAILABLE";
  $("overnightGaps").textContent="NOT CALCULATED";
  status.textContent="Overnight bar request failed: "+error.message+" · Requested Arizona "+$("overnightFrom").value+" → "+$("overnightTo").value+" · UTC "+fromUtc+" → "+toUtc+". Check provider configuration and API logs.";
  $("overnightInterpretation").textContent="No successful response for this request. Previous results were cleared. Do not infer missing bars or Overnight qualification.";
 } finally { button.disabled=false; }
});
/*
/// MODULE: Overnight provider coverage control
/// PURPOSE: Compare selected symbol and SPY over the same explicit UTC window.
/// SAFETY: Descriptive coverage only; no inference of no-trade, entitlement, or qualification from empty/sparse responses.
*/
$("overnightCoverageForm")?.addEventListener("submit", async event => {
 event.preventDefault();
 const status=$("overnightCoverageStatus"), rows=$("overnightCoverageRows"), button=$("overnightCoverageRun");
 const symbol=$("overnightSymbol").value.trim().toUpperCase();
 const fromValue=$("overnightCoverageFrom").value, toValue=$("overnightCoverageTo").value;
 const fromUtc=new Date(fromValue+"Z"), toUtc=new Date(toValue+"Z");
 if(!/^[A-Z0-9.-]{1,8}$/.test(symbol)){status.textContent="Enter a valid Overnight symbol first.";return;}
 if(!Number.isFinite(fromUtc.getTime())||!Number.isFinite(toUtc.getTime())||fromUtc>=toUtc||toUtc-fromUtc>31*86400000){status.textContent="Enter a valid UTC window (From before To; maximum 31 days).";return;}
 const symbols=[...new Set([symbol,"SPY"])];
 button.disabled=true; rows.replaceChildren(); status.textContent="Requesting "+symbols.join(" and ")+" over the same UTC window…";
 try {
  const results=await Promise.all(symbols.map(async ticker=>{
   const params=new URLSearchParams({symbol:ticker,timeframe:$("overnightTimeframe").value,from:fromUtc.toISOString(),to:toUtc.toISOString(),providerName:"Alpaca"});
   try{return {symbol:ticker,data:await overnightFetchJson("/api/market-data/bars?"+params.toString())};}
   catch(error){return {symbol:ticker,error:error.message};}
  }));
  for(const result of results){
   const tr=document.createElement("tr");
   const data=result.data||{}, count=Number(data.barCount??(data.bars||[]).length)||0;
   const first=data.firstSourceTimestampUtc||"—", latest=data.latestSourceTimestampUtc||"—";
   const interpretation=result.error?"REQUEST FAILED":count===0?"EMPTY RESPONSE · CAUSE UNKNOWN":count<5?"SPARSE SOURCE COVERAGE · NOT COMPLETE SESSION COVERAGE":"BARS RETURNED · COVERAGE STILL REQUIRES REVIEW";
   const values=[result.symbol,result.error?"Alpaca · unavailable":(data.provider||"Alpaca")+" / "+(data.feed||"feed unknown"),result.error?"—":String(count),result.error?"—":first+" / "+latest,interpretation+(result.error?": "+result.error:"")];
   for(const value of values){const td=document.createElement("td");td.textContent=String(value);tr.append(td);} rows.append(tr);
  }
  const spy=results.find(x=>x.symbol==="SPY"), selected=results.find(x=>x.symbol===symbol);
  const spyCount=Number(spy?.data?.barCount??0), selectedCount=Number(selected?.data?.barCount??0);
  status.textContent="Control complete · "+(spyCount>0?"SPY returned "+spyCount+" post-market observation(s); IEX delivered some data in this window.":"SPY returned no bars; feed coverage remains unresolved.")+" "+(symbol==="SPY"?"Selected ticker is the control symbol.":symbol+" returned "+selectedCount+" bar(s). Sparse results are not proof of complete coverage.");
 } catch(error){status.textContent="Coverage comparison failed: "+error.message;}
 finally{button.disabled=false;}
});

$("overnightRefreshSnapshots")?.addEventListener("click",refreshOvernightSnapshots);
$("mode")?.addEventListener("change",()=>{ if($("mode").value==="overnight"){initializeOvernightWindow();refreshOvernightSnapshots();} });
initializeOvernightWindow();


/// MODULE: Historical V2 decision replay
/// PURPOSE: Requests deterministic point-in-time decisions from persisted bars using the same V2 engine.
/// SAFETY: Requires an explicit UTC as-of timestamp and feed; server excludes bars whose close occurs after that time.
const replayAsOfLocal=$("replayAsOfLocal");
if(replayAsOfLocal&&!replayAsOfLocal.value)replayAsOfLocal.value=phoenixLocalInput(new Date());
$("historicalReplayForm")?.addEventListener("submit",async event=>{
 event.preventDefault();
 const symbol=($("replaySymbol")?.value||"").trim().toUpperCase();
 const feed=$("replayFeed")?.value||"";
 const localAsOf=($("replayAsOfLocal")?.value||"").trim();
 const hasPosition=$("replayHasPosition")?.checked===true;
 const status=$("replayStatus"),host=$("replayResult"),button=$("replayRun");
 if(!/^[A-Z0-9.-]{1,8}$/.test(symbol)){status.textContent="Enter a valid ticker symbol.";return;}
 let asOfUtc;
 try{asOfUtc=phoenixLocalToUtc(localAsOf);}catch(error){status.textContent=error.message;return;}
 const params=new URLSearchParams({symbol,providerName:"Alpaca",feed,asOfUtc,hasPosition:String(hasPosition)});
 button.disabled=true;status.textContent="Replaying persisted bars through V2SignalEngine… Arizona "+localAsOf+" ("+asOfUtc+" UTC).";host.replaceChildren();
 try{
  const response=await fetch("/api/market-data/replay?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
  const data=await response.json();if(!response.ok)throw new Error(data.detail||data.error||("HTTP "+response.status));
  const decision=data.decision||{};
  const summary=document.createElement("p");
  summary.textContent=data.status==="REPLAYED"
   ? "REPLAYED · "+symbol+" · "+feed.toUpperCase()+" · Arizona "+localAsOf+" · "+(decision.state||"UNKNOWN")+" / "+(decision.action||"UNKNOWN")+" · score "+(decision.score??"DATA?")+" · hasPosition="+hasPosition+" · "+(decision.reason||"No reason returned.")
   : data.status+" · "+symbol+" · 1M bars "+(data.oneMinuteBars??"DATA?")+" · 5M bars "+(data.fiveMinuteBars??"DATA?")+" · "+(data.note||"No decision returned.");
  host.append(summary);
  const audit=document.createElement("pre");audit.className="replay-audit";audit.textContent=JSON.stringify(data,null,2);host.append(audit);
  status.textContent="Replay complete. Arizona local time was converted to UTC. No order was submitted. Historical position state is simulated only; broker positions are not connected.";
 }catch(error){status.textContent="Replay failed: "+error.message;host.textContent="No decision returned. Check that the API is current and stored-bar history exists.";}
 finally{button.disabled=false;}
});
