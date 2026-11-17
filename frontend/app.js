/// MODULE: Live Decision Workbench
/// PURPOSE: Renders the focused symbol from the live assessment API; no synthetic snapshots are used.
const $=id=>document.getElementById(id);
// Sequence guard must exist before the initial render can invoke the async refresh.
let decisionRefreshSequence=0;
const LIVE_ASSESSMENT_TIMEOUT_MS=20000;
const OVERNIGHT_METRIC_TIMEOUT_MS=20000;
let overnightPriceSummarySequence=0;
let overnightPriceSummaryController=null;
// Restore the selected workspace before any startup chart or live-assessment request can fire.
try{const savedMode=localStorage.getItem("mcc.workspaceMode.v1");if(savedMode==="overnight"||savedMode==="momentum")$("mode").value=savedMode;}catch{}
$("mode")?.addEventListener("change",()=>{try{localStorage.setItem("mcc.workspaceMode.v1",$("mode").value);}catch{}});
const feedbackKey="mcc.dashboardFeedback.v1";
const instrumentMetadataCache=new Map();
function symbolDisplayName(symbol){const item=instrumentMetadataCache.get(symbol);return item?.name?symbol+" — "+item.name:symbol;}
async function refreshInstrumentTitle(symbol){
 const heading=$("mainChartTitle");if(!heading)return;const ticker=String(symbol||"").trim().toUpperCase();if(!ticker)return;
 const cached=instrumentMetadataCache.get(ticker);if(cached){heading.textContent=symbolDisplayName(ticker);return;}
 heading.textContent=ticker+" — loading instrument name…";
 try{const response=await fetch("/api/market-data/instrument?"+new URLSearchParams({symbol:ticker}),{cache:"no-store",headers:{Accept:"application/json"}});
 const data=await response.json().catch(()=>({}));if(!response.ok)throw Error(data.detail||data.error||("HTTP "+response.status));
 instrumentMetadataCache.set(ticker,data);if(($("symbol")?.value||"").trim().toUpperCase()===ticker)heading.textContent=symbolDisplayName(ticker);
 }catch{
  try{
   const response=await fetch("/api/market-data/yahoo-extended-hours?"+new URLSearchParams({symbol:ticker}),{cache:"no-store",headers:{Accept:"application/json"}});
   const quote=await response.json();if(!response.ok)throw Error("Yahoo metadata unavailable");
   instrumentMetadataCache.set(ticker,{symbol:ticker,name:quote.name||null});
  }catch{instrumentMetadataCache.set(ticker,{symbol:ticker,name:null});}
  if(($("symbol")?.value||"").trim().toUpperCase()===ticker)heading.textContent=instrumentMetadataCache.get(ticker).name?symbolDisplayName(ticker):ticker+" — instrument name unavailable";
 }
}
function setDockClassification(label,kind="unknown"){const node=$("prototype-symbol-classification");if(node){node.textContent=label;node.className="prototype-symbol-classification classification-"+kind;}}
async function refreshOvernightDockClassification(symbol){
 const ticker=String(symbol||"").trim().toUpperCase();if(!ticker)return;
 setDockClassification("BORING WINNER · CHECKING RESEARCH","unknown");
 try{
  const response=await fetch("/api/overnight-scanner/thinkorswim-daily-bars?"+new URLSearchParams({symbol:ticker,limit:"1"}),{cache:"no-store",headers:{Accept:"application/json"}});
  const rows=await response.json();if(!response.ok)throw Error("reference lookup failed");
  if(($("symbol")?.value||"").trim().toUpperCase()!==ticker||$("mode")?.value!=="overnight")return;
  const result=Array.isArray(rows)&&rows.length?rows[0].calculation?.boringWinnerPass:null;
  if(result===true)setDockClassification("BORING WINNER · PASS · RESEARCH ONLY","high");
  else if(result===false)setDockClassification("BORING WINNER · FAIL · RESEARCH ONLY","blocked");
  else setDockClassification("BORING WINNER · NOT CALCULATED","unknown");
 }catch{if(($("symbol")?.value||"").trim().toUpperCase()===ticker&&$("mode")?.value==="overnight")setDockClassification("BORING WINNER · DATA?","unknown");}
}
function updateDockClassificationFromAssessment(data){
 const classification=String(data?.opportunityQuality?.classification||"DataUnknown");
 const map={ReadyHighOpportunity:["QUALITY — HIGH","high"],ReadyModerate:["QUALITY — MODERATE","moderate"],ReadyBoring:["QUALITY — LOW","boring"],ReadyExtended:["TIMING — EXTENDED","extended"],WatchDeveloping:["WATCH — DEVELOPING","watch"],Blocked:["BLOCKED","blocked"],DataUnknown:["DATA?","unknown"]};
 const [label,kind]=map[classification]||["DATA?","unknown"];setDockClassification("OPPORTUNITY QUALITY · "+label,kind);
}


/// MODULE: Exchange session countdown
/// PURPOSE: Render the API's calendar-backed next session transition in the header.
/// SAFETY: If the API/calendar is unavailable, show DATA? rather than a guessed countdown.
let marketSessionTargetUtc = null;
let overnightEvaluationTargetUtc = null;
let overnightBuyDeadlineUtc = null;
let overnightBuyWindowStartUtc = null;
let overnightSellWindowStartUtc = null;
let overnightSellWindowEndUtc = null;
let overnightBuySessionDate = null;
let overnightSellSessionDate = null;
let overnightMarketPhase = "UNKNOWN";
let overnightEvaluationOpen = false;
let overnightLateBuyDeadlineUtc = null;
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
   const buyWindowStart=new Date(data.overnightBuyWindowStartUtc),sellWindowStart=new Date(data.overnightSellWindowStartUtc),sellWindowEnd=new Date(data.overnightSellWindowEndUtc);
  if(Number.isNaN(evaluationTarget.getTime())||Number.isNaN(buyDeadline.getTime())||Number.isNaN(buyWindowStart.getTime())||Number.isNaN(sellWindowStart.getTime())||Number.isNaN(sellWindowEnd.getTime()))throw new Error("Invalid Overnight process schedule.");
  overnightEvaluationTargetUtc=evaluationTarget.getTime();
  overnightBuyDeadlineUtc=buyDeadline.getTime();
  overnightBuyWindowStartUtc=buyWindowStart.getTime();
  overnightSellWindowStartUtc=sellWindowStart.getTime();
  overnightSellWindowEndUtc=sellWindowEnd.getTime();
  overnightBuySessionDate=data.overnightBuySessionDate||null;
  overnightSellSessionDate=data.overnightSellSessionDate||null;
  overnightMarketPhase=String(data.phase||"UNKNOWN").toUpperCase();
  overnightEvaluationOpen=data.overnightEvaluationOpen===true;
  const lateDeadline=new Date(data.overnightLateBuyDeadlineUtc);
  // Late buy window (close + 60 min, 2026-11-16); older APIs omit it, so it stays null.
  overnightLateBuyDeadlineUtc=Number.isNaN(lateDeadline.getTime())||lateDeadline.getTime()<=0?null:lateDeadline.getTime();
  const label=$("marketSessionLabel"),detail=$("marketSessionDetail");
  if(label)label.textContent=data.label||"Until Market opens";
  if(detail)detail.textContent=(data.exchangeTimeZone||"America/New_York")+" · "+(data.targetEvent||"NEXT SESSION TRANSITION")+" · "+(data.calendarVersion||"CALENDAR UNKNOWN");
  renderMarketSessionCountdown();
 }catch(error){
  marketSessionTargetUtc=null;
  if($("marketSessionLabel"))$("marketSessionLabel").textContent="Market session clock";
  if($("marketSessionCountdown"))$("marketSessionCountdown").textContent="DATA?";
  overnightEvaluationTargetUtc=null;overnightBuyDeadlineUtc=null;overnightBuyWindowStartUtc=null;overnightSellWindowStartUtc=null;overnightSellWindowEndUtc=null;overnightBuySessionDate=null;overnightSellSessionDate=null;overnightMarketPhase="UNKNOWN";overnightEvaluationOpen=false;
   if($("overnightBuyWindowSchedule"))$("overnightBuyWindowSchedule").textContent="Buy window · DATA?";if($("overnightSellWindowSchedule"))$("overnightSellWindowSchedule").textContent="GTC_EXT sell window · DATA?";
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
 const evaluationLabel=$("overnightEvaluationLabel"),evaluationCountdown=$("overnightEvaluationCountdown"),buyLabel=$("overnightBuyCountdownLabel"),buyCountdown=$("overnightBuyCountdown");
 const now=Date.now(),evaluationWindowOpen=overnightEvaluationTargetUtc!==null&&overnightBuyDeadlineUtc!==null&&now>=overnightEvaluationTargetUtc&&now<overnightBuyDeadlineUtc;
 if(evaluationLabel)evaluationLabel.textContent=evaluationWindowOpen?"Overnight candidate evaluation · OPEN":"Overnight candidate evaluation begins in";
 if(evaluationCountdown)evaluationCountdown.textContent=overnightEvaluationTargetUtc===null?"DATA?":(evaluationWindowOpen?"OPEN":formatCountdown(overnightEvaluationTargetUtc));
 // Compact header (2026-11-16): one countdown that follows the buy window: before it, during it (urgency colours),
 // the late after-hours window, then closed.
 if(buyCountdown){
  buyCountdown.classList.remove("countdown-normal","countdown-caution","countdown-urgent","countdown-critical","countdown-closed");
  const lateOpen=overnightBuyDeadlineUtc!==null&&overnightLateBuyDeadlineUtc!==null&&now>=overnightBuyDeadlineUtc&&now<overnightLateBuyDeadlineUtc;
  if(evaluationWindowOpen){
   const secondsLeft=Math.max(0,Math.ceil((overnightBuyDeadlineUtc-now)/1000));
   buyCountdown.textContent=formatCountdown(overnightBuyDeadlineUtc);
   buyCountdown.classList.add(secondsLeft<=600?"countdown-critical":secondsLeft<=1800?"countdown-urgent":"countdown-caution");
   if(buyLabel)buyLabel.textContent="Buy window · closes in";
  }else if(lateOpen){
   buyCountdown.textContent=formatCountdown(overnightLateBuyDeadlineUtc);
   buyCountdown.classList.add("countdown-urgent");
   if(buyLabel)buyLabel.textContent="Late window (LMT EXT) · ends in";
  }else if(overnightEvaluationTargetUtc!==null&&now<overnightEvaluationTargetUtc){
   buyCountdown.textContent=formatCountdown(overnightEvaluationTargetUtc);
   buyCountdown.classList.add("countdown-normal");
   if(buyLabel)buyLabel.textContent="Buy window in";
  }else{
   buyCountdown.textContent="CLOSED";
   buyCountdown.classList.add("countdown-closed");
   if(buyLabel)buyLabel.textContent="Buy window";
  }
 }
 const buySchedule=$("overnightBuyWindowSchedule"),sellSchedule=$("overnightSellWindowSchedule");
 const azTime=stamp=>new Date(stamp).toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit"});
 if(buySchedule&&overnightBuyWindowStartUtc!==null&&overnightBuyDeadlineUtc!==null)buySchedule.textContent="Buy window · "+(overnightBuySessionDate||"date unknown")+" · "+azTime(overnightBuyWindowStartUtc)+"–"+azTime(overnightBuyDeadlineUtc)+" Arizona";
 if(sellSchedule&&overnightSellWindowStartUtc!==null&&overnightSellWindowEndUtc!==null)sellSchedule.textContent="GTC_EXT sell · "+(overnightSellSessionDate||"date unknown")+" · "+azTime(overnightSellWindowStartUtc)+"–"+azTime(overnightSellWindowEndUtc)+" Arizona";
}
void refreshMarketSessionClock();
window.setInterval(renderMarketSessionCountdown,1000);
window.setInterval(()=>{if(document.visibilityState==="visible")void refreshMarketSessionClock();},60000);


/// PURPOSE: Track dynamic WebSocket subscription requests before any startup refresh can run.
const requestedMinuteBarSymbols=new Set();
/// PURPOSE: Per-symbol live-assessment de-duplication. Declared before the first render() so the
/// startup Intraday assessment does not hit a temporal-dead-zone ReferenceError.
const liveAssessmentInFlight=new Map();
function render(){
 const overnight=$("mode")?.value==="overnight";
 document.body.classList.toggle("boring-overnight-mode",overnight);
 $("overnightPanel")?.classList.toggle("hidden",!overnight);
 // The qualification section is a sibling of overnightPanel, so it needs its own mode gate.
 $("overnight-qualification")?.classList.toggle("hidden",!overnight);
 $("overnight-opportunity-status")?.classList.toggle("hidden",!overnight);
 if(overnight&&$("overnightSymbol")&&$("symbol")?.value)$("overnightSymbol").value=$("symbol").value.trim().toUpperCase();
 const eyebrow=document.querySelector(".hero-card .eyebrow");
 if(eyebrow)eyebrow.textContent=overnight?"BORING OVERNIGHT · RESEARCH":"LIVE OPPORTUNITY CLASSIFICATION";
 if(overnight){
  const classification=$("classification"),headline=$("headline"),eligibility=$("eligibility"),freshness=$("freshness");
  if(classification){classification.textContent="BORING OVERNIGHT · RESEARCH";classification.className="classification boring";}
  if(headline)headline.textContent="Overnight workspace uses historical Daily and extended-hours chart evidence. Intraday 5M permission and 1M execution are not evaluated in this mode.";
  if(eligibility)eligibility.textContent="Intraday permission: NOT USED";
  if(freshness)freshness.textContent="Overnight chart: latest available bars · not a live-permission freshness gate";
  const source=$("decisionSource");if(source)source.textContent="Overnight research mode · no 5M/1M permission request";
  void refreshFocusedPriceChart();
 }else{
  void refreshDecisionExplanation();
  void refreshFocusedPriceChart();
 }
}
$("mode")?.addEventListener("change",()=>{render();renderMorningReport();});
$("symbol")?.addEventListener("change",render);
$("reset")?.addEventListener("click",()=>{if($("mode"))$("mode").value="momentum";render();void refreshFocusedPriceChart();});
render();
window.setInterval(()=>{
 if(document.visibilityState!=="visible")return;
 if($("mode")?.value==="overnight"){void refreshFocusedPriceChart();return;}
 void refreshRedZoneAssessments();void refreshFocusedPriceChart();void refreshDecisionExplanation();void loadMinuteBarStreamStatus();
},15000);


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
/// RED ZONE STATE COLOURS (2026-11-16): last displayed tone per symbol, used to detect state transitions.
const previousRedZoneTone=new Map();
const redZoneStateLog=[];
const HELD_SYMBOLS_KEY="mcc.heldSymbols.v1";
/// <summary>Reads the operator-declared HOLDING symbols (browser only; MCC has no broker position data). Called by every state view.</summary>
function readHeldSymbols(){try{const value=JSON.parse(localStorage.getItem(HELD_SYMBOLS_KEY)||"[]");return new Set(Array.isArray(value)?value.map(x=>String(x).toUpperCase()):[]);}catch{return new Set();}}
/// <summary>Marks or clears HOLDING for one symbol and forgets its last tone so the switch itself does not sound an alert. Called by the card toggle.</summary>
function setHeldSymbol(symbol,held){const ticker=String(symbol||"").toUpperCase(),set=readHeldSymbols();if(held)set.add(ticker);else set.delete(ticker);try{localStorage.setItem(HELD_SYMBOLS_KEY,JSON.stringify([...set]));}catch{/* storage blocked: toggle lasts until reload */}previousRedZoneTone.delete(ticker);}
const RED_ZONE_TONE_HINTS={
 wait:"Wait · no setup yet.",
 prepare:"Heating up · 5M permission; waiting for 1M confirmation.",
 ready:"Step 6.a · 5M permission + 1M confirmation. Your call: limit buy in Schwab.",
 runner:"Holding · 5M trend intact.",
 waning:"Step 6.b · Winding down. Plan or place your limit sell.",
 sell:"Step 6.c · Sell state. Get out.",
 data:"No live state · data stale, missing, or outside the regular session (07:30–14:00 AZ in standard time)."
};
/// <summary>
/// Chooses the decision to display for a live assessment: the held-position decision when the operator marked the
/// symbol HOLDING, otherwise the no-position decision. Returns the TOS-style label, tone and hint. Called by the Red
/// Zone cards and the focused classification on every refresh.
/// </summary>
function liveStateView(symbol,data){
 const held=readHeldSymbols().has(String(symbol||"").toUpperCase());
 const decision=held&&data?.heldPositionDecision?data.heldPositionDecision:(data?.decision||null);
 if(!decision)return{label:humanizeSignalState(data?.evaluationStatus||data?.status||"DATA?"),tone:"data",held,decision:null,hint:RED_ZONE_TONE_HINTS.data};
 const tone=decision.stateTone||(decision.entryAllowed===true?"ready":"data");
 return{label:decision.stateLabel||humanizeSignalState(decision.state),tone,held,decision,hint:RED_ZONE_TONE_HINTS[tone]||""};
}
/// <summary>
/// Records a state transition and sounds the matching alert. No alert on the first observation or when leaving DATA?
/// (so page load and the session open stay quiet). Alerts: PREPARE TO BUY, EXECUTION READY, RUNNER WANING / PREPARE TO
/// SELL, SELL / GET OUT. Returns true when the tone changed. Called after every card refresh and for off-screen pins.
/// </summary>
function observeRedZoneState(symbol,view){
 const key=String(symbol||"").toUpperCase(),prior=previousRedZoneTone.get(key);
 previousRedZoneTone.set(key,view.tone);
 if(prior===undefined||prior===view.tone)return false;
 if(prior!=="data"&&["prepare","ready","waning","sell"].includes(view.tone)){
  playRedZoneAlert(view.tone);
  const time=new Date().toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"2-digit",minute:"2-digit",second:"2-digit",hour12:true});
  redZoneStateLog.unshift(time+" AZ · "+key+" → "+view.label+(view.held?" (HOLDING)":""));
  redZoneStateLog.length=Math.min(redZoneStateLog.length,6);
  const log=$("redZoneStateLog");if(log)log.textContent="State changes: "+redZoneStateLog.join(" · ");
 }
 return true;
}

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
async function fetchLiveAssessment(symbol){
 const ticker=String(symbol||"").trim().toUpperCase();
 if(liveAssessmentInFlight.has(ticker))return liveAssessmentInFlight.get(ticker);
 const request=(async()=>{
  const params=new URLSearchParams({symbol:ticker,providerName:"Alpaca"});
  const controller=new AbortController();
  let timeoutId=null;
  try{
   timeoutId=window.setTimeout(()=>controller.abort(),LIVE_ASSESSMENT_TIMEOUT_MS);
   const response=await fetch("/api/market-data/live-assessment?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"},signal:controller.signal});
   let body;
   try{body=await response.json();}catch(error){if(controller.signal.aborted)throw error;body={};}
   if(!response.ok)throw new Error(body.detail||body.error||("HTTP "+response.status));
   return body;
  }catch(error){
   if(controller.signal.aborted)throw new Error("Live assessment timed out after "+(LIVE_ASSESSMENT_TIMEOUT_MS/1000)+" seconds. The request was cancelled; check API responsiveness and server diagnostics.");
   throw error;
  }finally{if(timeoutId!==null)window.clearTimeout(timeoutId);}
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
/// <summary>"YES (TOS latch since 8:35 AM AZ)" / "NO · persistent structural damage …" / "UNKNOWN" for a live assessment's 5M permission. Called by the Red Zone card gate line.</summary>
function permissionLatchText(five,decision){
 const value=five?.scoreBreakdown?.permission??decision?.fiveMinutePermission,latch=five?.permissionLatch;
 if(value===true)return "YES"+(latch?.grantedSinceUtc?" (TOS latch since "+new Date(latch.grantedSinceUtc).toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit"})+" AZ)":" (TOS latch)");
 if(value===false)return "NO"+(latch?.blockLabel?" · "+latch.blockLabel:"");
 return "UNKNOWN";
}
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
 // 2026-11-17: 5M permission is the TOS latch (scoreBreakdown.permission), not score >= 5; the score is alignment only.
 const permitted=scoreBreakdown?.permission===true;
 const suffix=entryReady?" · EXECUTION READY":permitted?(decision?" · 5M PERMISSION; CHECK 1M STATE":" · 5M PERMISSION; 1M NOT EVALUATED"):score<=2?" · LOW ALIGNMENT":score>=5?" · ALIGNED; NO 5M PERMISSION YET":" · PARTIAL ALIGNMENT";
 node.textContent=marker+"V2 5M score: "+score+"/6"+suffix;
}
async function refreshDecisionExplanation(){
 const requestId=++decisionRefreshSequence;
 if($("mode")?.value==="overnight"){
  if($("classification")){$("classification").textContent="BORING OVERNIGHT · RESEARCH";$("classification").className="classification boring";}
  if($("headline"))$("headline").textContent="Intraday permission and execution freshness are not part of Overnight research mode.";
  if($("eligibility"))$("eligibility").textContent="Intraday permission: NOT USED";
  if($("freshness"))$("freshness").textContent="Overnight chart: latest available bars";
  if($("decisionSource"))$("decisionSource").textContent="Overnight research mode · intraday assessment bypassed";
  return;
 }
 const symbol=($("symbol")?.value||"").trim().toUpperCase();
 const classification=$("classification"),headline=$("headline"),price=$("price"),dailyVwap=$("dailyVwap"),atr14=$("atr14"),rvol=$("rvol"),extension=$("extension"),eligibility=$("eligibility"),freshness=$("freshness"),reasons=$("reasons"),fiveState=$("fiveState"),oneState=$("oneState"),scoreNode=$("decisionScore"),sourceNode=$("decisionSource");
 if(!symbol||!classification)return;
 classification.textContent="UPDATING TO "+symbol;classification.className="classification watch";
 if(headline)headline.textContent="Updating to "+symbol+"… requesting current completed 1M and 5M bars.";
 try{
  const data=await fetchLiveAssessment(symbol);
  if(requestId!==decisionRefreshSequence)return;
  const one=data.oneMinute||{},five=data.fiveMinute||{},decision=data.decision||null;
   updateDockClassificationFromAssessment(data);
  const transportLabel=$("marketTransportLabel");
  if(transportLabel)transportLabel.textContent=one.sourceTransport==="ALPACA_WEBSOCKET_LATEST_BAR"?"ALPACA 1M WS / 5M REST · 15s UI REFRESH":"ALPACA REST · 15s UI REFRESH";
  const fiveMinutePermission=five.scoreBreakdown?.permission??decision?.fiveMinutePermission,permission=fiveMinutePermission===true?"YES":fiveMinutePermission===false?"NO":"UNKNOWN",execution=decision?.oneMinuteExecution||"UNKNOWN",ready=decision?.entryAllowed===true;
  const focusedView=liveStateView(symbol,data);
  classification.textContent=focusedView.label+(focusedView.held?" · HOLDING":"");
  classification.className="classification "+(ready?"high":fiveMinutePermission?"boring":"watch")+" state-tone-"+focusedView.tone;
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
async function refreshOvernightPriceSummary(symbol){
 const requestId=++overnightPriceSummarySequence;
 overnightPriceSummaryController?.abort();
 const controller=new AbortController();overnightPriceSummaryController=controller;
 const c=$("overnightAtClosePrice"),ct=$("overnightAtCloseTimestamp"),p=$("overnightCurrentPrice"),pl=$("overnightCurrentPriceLabel"),pt=$("overnightCurrentPriceTimestamp");
 const price=$("price"),oneMinuteTimestamp=$("oneMinuteTimestamp"),dailyVwapNode=$("dailyVwap"),atr14Node=$("atr14"),rvolNode=$("rvol"),extensionNode=$("extension");
 const priceDetail=$("priceMetricDetail"),vwapDetail=$("dailyVwapDetail"),atrDetail=$("atr14Detail"),rvolDetail=$("rvolDetail"),extensionDetail=$("extensionDetail");
 const isCurrent=()=>requestId===overnightPriceSummarySequence&&$("mode")?.value==="overnight"&&($("symbol")?.value||"").trim().toUpperCase()===String(symbol||"").trim().toUpperCase();
 [c,p,price,dailyVwapNode,atr14Node,extensionNode].forEach(x=>{if(x)x.textContent="LOADING…";});
 if(rvolNode)rvolNode.textContent="NOT USED";
 if(oneMinuteTimestamp)oneMinuteTimestamp.textContent="Historical bar timestamp: loading…";
 if(priceDetail)priceDetail.textContent="Latest returned 1M bar close · research only";
 if(vwapDetail)vwapDetail.textContent="Last available regular-session VWAP · historical diagnostic";
 if(atrDetail)atrDetail.textContent="1M simple average True Range · last 14 returned ranges";
 if(rvolDetail)rvolDetail.textContent="NOT EVALUATED in Boring Overnight mode; intraday same-time RVOL is not used.";
 if(extensionDetail)extensionDetail.textContent="Historical |1M close − RTH VWAP| / 1M ATR14 · diagnostic only";
 const timeoutId=window.setTimeout(()=>controller.abort(),OVERNIGHT_METRIC_TIMEOUT_MS);
 try{
  const to=new Date(),from=new Date(to.getTime()-5*86400000),q=new URLSearchParams({symbol,timeframe:"1m",from:from.toISOString(),to:to.toISOString(),providerName:"Alpaca"});
  const r=await fetch("/api/market-data/bars?"+q,{cache:"no-store",headers:{Accept:"application/json"},signal:controller.signal});
  let d;try{d=await r.json();}catch(error){if(controller.signal.aborted)throw error;throw Error("Daily/1M historical bars returned an unreadable response.");}
  if(!r.ok)throw Error(d.detail||d.error||("HTTP "+r.status));
  const bars=(Array.isArray(d.bars)?d.bars:[]).filter(b=>Number.isFinite(Number(b.close))&&Number.isFinite(new Date(b.timestamp).getTime())).sort((x,y)=>new Date(x.timestamp)-new Date(y.timestamp));
  if(!isCurrent())return;
  const parts=ts=>Object.fromEntries(new Intl.DateTimeFormat("en-US",{timeZone:"America/New_York",weekday:"short",year:"numeric",month:"2-digit",day:"2-digit",hour:"2-digit",minute:"2-digit",hourCycle:"h23"}).formatToParts(new Date(ts)).map(x=>[x.type,x.value]));
  const local=ts=>new Date(ts).toLocaleString("en-US",{timeZone:"America/Phoenix",hour12:true});
  const last=bars.at(-1),asOfParts=parts(Date.now()),asOfMinute=Number(asOfParts.hour)*60+Number(asOfParts.minute),asOfDate=asOfParts.year+"-"+asOfParts.month+"-"+asOfParts.day;
  const rth=bars.filter(b=>{const z=parts(b.timestamp),m=Number(z.hour)*60+Number(z.minute),date=z.year+"-"+z.month+"-"+z.day;return !["Sat","Sun"].includes(z.weekday)&&m>=570&&m<960&&(date<asOfDate||(asOfMinute>=960&&date===asOfDate));}).at(-1);
  const rthDate=rth?(()=>{const z=parts(rth.timestamp);return z.year+"-"+z.month+"-"+z.day;})():null;
  const sessionBars=rthDate?bars.filter(b=>{const z=parts(b.timestamp),m=Number(z.hour)*60+Number(z.minute);return z.year+"-"+z.month+"-"+z.day===rthDate&&m>=570&&m<960;}):[];
  const volumeBars=sessionBars.filter(b=>Number.isFinite(Number(b.volume))&&Number(b.volume)>0&&Number.isFinite(Number(b.high))&&Number.isFinite(Number(b.low)));
  const volumeTotal=volumeBars.reduce((sum,b)=>sum+Number(b.volume),0);
  const dailyVwap=volumeTotal>0?volumeBars.reduce((sum,b)=>sum+((Number(b.high)+Number(b.low)+Number(b.close))/3)*Number(b.volume),0)/volumeTotal:null;
  const trueRanges=bars.slice(1).map((b,i)=>{const high=Number(b.high),low=Number(b.low),prior=Number(bars[i].close);return Number.isFinite(high)&&Number.isFinite(low)&&Number.isFinite(prior)?Math.max(high-low,Math.abs(high-prior),Math.abs(low-prior)):null;}).filter(Number.isFinite);
  const atr14=trueRanges.length>=14?trueRanges.slice(-14).reduce((sum,x)=>sum+x,0)/14:null;
  const extension=last&&dailyVwap!==null&&atr14!==null&&atr14>0?Math.abs(Number(last.close)-dailyVwap)/atr14:null;
  const fmt=x=>x===null||x===undefined||!Number.isFinite(Number(x))?"DATA?":Number(x).toFixed(4);
  if(price)price.textContent=last?"$"+fmt(last.close):"DATA? — NO 1M BAR";
  if(oneMinuteTimestamp)oneMinuteTimestamp.textContent=last?"Last returned 1M bar · "+local(last.timestamp)+" Arizona · "+(d.provider||"UNKNOWN")+"/"+(d.feed||"UNKNOWN")+" · not a live quote":"Data not found in sources: no 1M bars returned.";
  if(priceDetail)priceDetail.textContent="Latest returned 1M bar close · historical research only; not an executable quote";
  if(dailyVwapNode)dailyVwapNode.textContent=dailyVwap===null?"DATA?":"$"+fmt(dailyVwap);
  if(vwapDetail)vwapDetail.textContent=rthDate?"RTH 1M typical-price VWAP proxy · session "+rthDate+" · "+volumeBars.length+" volume bars":"DATA? · no qualifying RTH volume bars";
  if(atr14Node)atr14Node.textContent=atr14===null?"DATA?":"$"+fmt(atr14);
  if(atrDetail)atrDetail.textContent="1M simple average True Range · "+trueRanges.length+" ranges available · "+(trueRanges.length>=14?"14-bar value":"14 required");
  if(rvolNode)rvolNode.textContent="NOT USED";
  if(rvolDetail)rvolDetail.textContent="Intraday same-time 5M RVOL is intentionally not evaluated in Boring Overnight mode.";
  if(extensionNode)extensionNode.textContent=extension===null?"DATA?":extension.toFixed(2)+" ATR";
  if(extensionDetail)extensionDetail.textContent=extension===null?"DATA? · requires a 1M close, RTH VWAP and 14-bar ATR":"Historical |1M close − RTH VWAP| / 1M ATR14 · diagnostic only";
  if(c)c.textContent=rth?"$"+Number(rth.close).toFixed(4):"DATA? — NO RTH BAR";
  if(ct){const z=rth?parts(rth.timestamp):null;ct.textContent=rth?"Last RTH 1M bar close · "+local(rth.timestamp)+" Arizona"+(z.hour==="15"&&z.minute==="59"?" · final 1M bar before 16:00 ET; official auction print not verified":" · exact session-close bar not established"):"Data not found in sources: no RTH 1M bar returned.";}
  const reasonList=$("reasons"),source=$("decisionSource"),score=$("decisionScore"),notice=$("mcc-static-0086-div");
  if(reasonList){reasonList.replaceChildren(...[
   "Overnight historical 1M close "+(last?"$"+fmt(last.close):"DATA?"),
   "Last RTH VWAP (1M typical-price proxy) "+(dailyVwap===null?"DATA?":"$"+fmt(dailyVwap)),
   "1M ATR14 (simple average True Range) "+(atr14===null?"DATA?":"$"+fmt(atr14)),
   "ATR extension "+(extension===null?"DATA?":extension.toFixed(2)+" ATR"),
   "Intraday 5M relative volume NOT USED in Boring Overnight mode."
  ].map(text=>Object.assign(document.createElement("li"),{textContent:text})));}
  if(source)source.textContent="Overnight diagnostic calculations · "+(d.provider||"UNKNOWN")+"/"+(d.feed||"UNKNOWN")+" · RTH VWAP session "+(rthDate||"UNKNOWN")+" · latest 1M bar "+(last?local(last.timestamp)+" Arizona":"UNKNOWN")+" · no intraday permission or execution evaluation.";
  if(score){score.textContent="OVERNIGHT DIAGNOSTICS · NOT A SIGNAL";score.className="redzone-score score-unknown";}
  if(notice)notice.textContent="These 1M close / RTH VWAP / ATR14 / extension values are descriptive historical diagnostics only. Relative volume is not evaluated in Boring Overnight mode. None establishes Boring Overnight qualification, 5M permission, 1M execution readiness, consolidated coverage, or an executable quote.";
  const readReference=async url=>{
   const referenceController=new AbortController(),referenceTimeout=window.setTimeout(()=>referenceController.abort(),5000);
   try{const response=await fetch(url,{cache:"no-store",headers:{Accept:"application/json"},signal:referenceController.signal});const body=await response.json();if(!response.ok)throw Error(body.detail||body.error||("HTTP "+response.status));return body;}
   finally{window.clearTimeout(referenceTimeout);}
  };
  const [tradeResult,yahooResult]=await Promise.allSettled([
   readReference("/api/market-data/latest-trade?"+new URLSearchParams({symbol})),
   readReference("/api/market-data/yahoo-extended-hours?"+new URLSearchParams({symbol}))
  ]);
  if(!isCurrent())return;
  const latestTrade=tradeResult.status==="fulfilled"?tradeResult.value.trade||null:null;
  const latestTradeError=tradeResult.status==="rejected"?tradeResult.reason?.message||"latest trade unavailable":null;
  const yahooQuote=yahooResult.status==="fulfilled"?yahooResult.value:null;
  const yahooError=yahooResult.status==="rejected"?yahooResult.reason?.message||"Yahoo reference unavailable":null;
  const barTimestamp=last?new Date(last.timestamp).getTime():0,tradeTimestamp=latestTrade?new Date(latestTrade.timestampUtc).getTime():0,yahooTimestamp=yahooQuote?.postMarketTimeUtc?new Date(yahooQuote.postMarketTimeUtc).getTime():0;
  const useTrade=latestTrade&&Number.isFinite(tradeTimestamp)&&tradeTimestamp>=barTimestamp;
  const useYahoo=yahooQuote?.postMarketPrice!=null&&Number.isFinite(yahooTimestamp)&&yahooTimestamp>=Math.max(barTimestamp,useTrade?tradeTimestamp:0);
  const shownPrice=useYahoo?Number(yahooQuote.postMarketPrice):useTrade?Number(latestTrade.price):last?Number(last.close):null;
  const shownTimestamp=useYahoo?yahooQuote.postMarketTimeUtc:useTrade?latestTrade.timestampUtc:last?.timestamp;
  const shownAgeMinutes=shownTimestamp?Math.max(0,(Date.now()-new Date(shownTimestamp).getTime())/60000):null;
  const shownParts=shownTimestamp?parts(shownTimestamp):null,shownMinute=shownParts?Number(shownParts.hour)*60+Number(shownParts.minute):-1;
  const shownSession=shownMinute>=960&&shownMinute<1200?"AFTER-HOURS":shownMinute>=240&&shownMinute<570?"PREMARKET":shownMinute>=570&&shownMinute<960?"REGULAR SESSION":"EXTENDED SESSION";
  if(p)p.textContent=shownPrice!==null&&Number.isFinite(shownPrice)?"$"+shownPrice.toFixed(4):"DATA? — NO TRADE OR BAR";
  if(pl)pl.textContent=useYahoo?(shownAgeMinutes>5?"LAST AFTER-HOURS PRICE · STALE":"AFTER-HOURS PRICE"):useTrade?(shownAgeMinutes>5?"LAST IEX TRADE · STALE":shownSession==="AFTER-HOURS"?"AFTER-HOURS PRICE":shownSession==="PREMARKET"?"PREMARKET PRICE":"LATEST PROVIDER TRADE"):"LAST RETURNED 1M BAR";
  if(pt)pt.textContent=useYahoo?"Yahoo Finance after-hours reference · "+local(yahooQuote.postMarketTimeUtc)+" Arizona · "+(shownAgeMinutes<=5?"recent reference":"STALE "+shownAgeMinutes.toFixed(0)+" minutes")+" · not NBBO / not executable":useTrade?"Alpaca latest "+shownSession.toLowerCase()+" trade · "+local(latestTrade.timestampUtc)+" Arizona · "+String(latestTrade.feed||"UNKNOWN").toUpperCase()+" · "+(shownAgeMinutes<=5?"recent provider trade":"STALE "+shownAgeMinutes.toFixed(0)+" minutes")+" · not NBBO / not guaranteed executable":last?"Last returned 1M bar close · "+local(last.timestamp)+" Arizona · "+(d.provider||"UNKNOWN")+"/"+(d.feed||"UNKNOWN")+" · "+(latestTradeError?"latest trade unavailable: "+latestTradeError:"no newer trade on configured feed")+(yahooError?" · Yahoo reference unavailable: "+yahooError:"")+" · not a live quote":"Data not found in sources: no 1M bars or latest trade returned.";
 }catch(error){
  if(!isCurrent())return;
  const message=controller.signal.aborted?"Overnight historical metric request timed out after "+(OVERNIGHT_METRIC_TIMEOUT_MS/1000)+" seconds. Check the bars API and server diagnostics.":error.message;
  if(c)c.textContent="DATA?";if(p)p.textContent="DATA?";if(ct)ct.textContent="Historical price request failed · "+message;if(pt)pt.textContent="Historical price request failed · source timestamp unavailable";
  if(price)price.textContent="DATA?";if(oneMinuteTimestamp)oneMinuteTimestamp.textContent="Historical bar unavailable · "+message;
  if(dailyVwapNode)dailyVwapNode.textContent="DATA?";if(atr14Node)atr14Node.textContent="DATA?";if(rvolNode)rvolNode.textContent="NOT USED";if(extensionNode)extensionNode.textContent="DATA?";
  if(priceDetail)priceDetail.textContent=message;if(vwapDetail)vwapDetail.textContent=message;if(atrDetail)atrDetail.textContent=message;if(rvolDetail)rvolDetail.textContent="Intraday same-time 5M RVOL is intentionally not evaluated in Boring Overnight mode.";if(extensionDetail)extensionDetail.textContent=message;
  const source=$("decisionSource");if(source)source.textContent="Overnight historical metrics unavailable · "+message;
  const reasonList=$("reasons");if(reasonList)reasonList.replaceChildren(Object.assign(document.createElement("li"),{textContent:message}));
 }finally{
  window.clearTimeout(timeoutId);
  if(overnightPriceSummaryController===controller)overnightPriceSummaryController=null;
 }
}
async function refreshFocusedPriceChart(){
 const host=$("focusPriceChart");if(!host)return;
 const symbol=($("symbol")?.value||"").trim().toUpperCase();
 void refreshInstrumentTitle(symbol);
 if($("mode")?.value==="overnight"){
  void refreshOvernightPriceSummary(symbol);
  host.replaceChildren();
  const badge=$("mainChartSymbol");if(badge)badge.textContent=symbol||"NO SYMBOL";
  const title=document.createElement("div");title.className="chart-title";title.textContent=symbolDisplayName(symbol)+" · 5-minute extended-hours research chart";host.append(title);
  try{
   const to=new Date(),from=new Date(to.getTime()-5*86400000);
   const params=new URLSearchParams({symbol,timeframe:"5m",from:from.toISOString(),to:to.toISOString(),providerName:"Alpaca"});
   const response=await fetch("/api/market-data/bars?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
   const data=await response.json();if(!response.ok)throw new Error(data.detail||data.error||("HTTP "+response.status));
   const bars=(Array.isArray(data.bars)?data.bars:[]).filter(b=>Number.isFinite(Number(b.close))&&Number.isFinite(new Date(b.timestamp).getTime())).sort((a,b)=>new Date(a.timestamp)-new Date(b.timestamp));
   drawPriceLine(host,bars,symbol);
   const chartTitle=document.createElement("div");chartTitle.className="chart-title";chartTitle.textContent=symbolDisplayName(symbol)+" · 5-minute extended-hours research chart";host.prepend(chartTitle);
   const last=bars.length?bars[bars.length-1]:null;
   const detail=document.createElement("p");detail.className="chart-meta";
   detail.textContent="Source "+(data.provider||"UNKNOWN")+"/"+(data.feed||"UNKNOWN")+" · "+bars.length+" returned 5M bars · latest source bar "+(last?new Date(last.timestamp).toLocaleString("en-US",{timeZone:"America/Phoenix"})+" Arizona":"NOT AVAILABLE")+" · intraday permission NOT EVALUATED · no stale-permission status applies in Overnight mode.";
   host.append(detail);
  }catch(error){const p=document.createElement("p");p.className="chart-unavailable";p.textContent="Overnight historical chart unavailable · "+error.message;host.append(p);}
  return;
 }
 void ensureMinuteBarSubscription(symbol);host.replaceChildren();
 const symbolBadge=$("mainChartSymbol");if(symbolBadge)symbolBadge.textContent=symbol||"NO SYMBOL";
 const title=document.createElement("div");title.className="chart-title";title.textContent=symbolDisplayName(symbol)+" · 1-minute OHLCV close · auto-refresh 15s";host.append(title);
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
 const scoreOf=item=>{const raw=String(item.score??"").replace(/,/g,"").match(/-?\d+(?:\.\d+)?/);return raw?Number(raw[0]):-Infinity;};
 const sortedRows=rows.slice().sort((a,b)=>scoreOf(b)-scoreOf(a)||String(a.symbol).localeCompare(String(b.symbol)));
 const uniqueRows=[];const seenSymbols=new Set();
 for(const item of sortedRows){const symbol=String(item.symbol||"").trim().toUpperCase();if(!symbol||seenSymbols.has(symbol))continue;seenSymbols.add(symbol);uniqueRows.push(item);}
 const ranked=getBoringOvernightCandidateRows(4).map(x=>String(x.symbol||"").toUpperCase());
 const paint=(id,items,empty)=>{const host=$(id);if(!host)return;host.replaceChildren();(items.length?items:[]).forEach((symbol,index)=>{const tag=document.createElement("span");tag.className="overnight-shortlist-symbol";tag.textContent=(index+1)+". "+symbol;host.append(tag);});if(!items.length){const note=document.createElement("span");note.className="muted small";note.textContent=empty;host.append(note);}};
 paint("overnightMccTopFourDisplay",ranked,"No imported rows");paint("overnightMyTopFourDisplay",overnightManualSymbols,"none selected");
 symbols.forEach(symbol=>{const label=document.createElement("label");label.className="overnight-shortlist-option";const input=document.createElement("input");input.type="checkbox";input.value=symbol;input.checked=overnightManualSymbols.includes(symbol);input.disabled=!input.checked&&overnightManualSymbols.length>=4;input.addEventListener("change",()=>{if(input.checked){if(overnightManualSymbols.length>=4){input.checked=false;return;}overnightManualSymbols.push(symbol);}else overnightManualSymbols=overnightManualSymbols.filter(x=>x!==symbol);saveOvernightShortlists();renderOvernightShortlistPicker(rows);updateOvernightShortlistMode();renderCatalystHumanReviewQueue();});const name=document.createElement("span");name.textContent=symbol;label.append(input,name);picker.append(label);});
 if(summary)summary.textContent=overnightManualSymbols.length+"/4"+(overnightManualSymbols.length===4?" ✓":"");
 const manual=$("overnightShortlistManual");if(manual)manual.disabled=overnightManualSymbols.length!==4;
}
function updateOvernightShortlistMode(){
 const mcc=$("overnightShortlistMcc"),manual=$("overnightShortlistManual"),button=$("overnightQualRefreshMarket"),active=$("overnightShortlistActive");
 if(mcc){mcc.setAttribute("aria-pressed",String(overnightShortlistMode==="mcc"));mcc.classList.toggle("active",overnightShortlistMode==="mcc");}
 if(manual){manual.setAttribute("aria-pressed",String(overnightShortlistMode==="manual"));manual.classList.toggle("active",overnightShortlistMode==="manual");manual.disabled=overnightManualSymbols.length!==4;}
 if(button){button.textContent="Fetch Daily + catalyst";button.title=overnightShortlistMode==="mcc"?"Fetch Daily + catalyst for Boring Top 4":"Fetch Daily + catalyst for My Top 4";}
 if(active)active.textContent=overnightShortlistMode==="mcc"?"Active group: Boring Top 4 · median-first historical profile":"Active group: My Top 4 · "+(overnightManualSymbols.length===4?overnightManualSymbols.join(", "):"select four symbols");
}
$("overnightShortlistMcc")?.addEventListener("click",()=>{overnightShortlistMode="mcc";saveOvernightShortlists();updateOvernightShortlistMode();syncReadySymbolsFromTopFour();renderMorningReport();});
$("overnightShortlistManual")?.addEventListener("click",()=>{if(overnightManualSymbols.length!==4)return;overnightShortlistMode="manual";saveOvernightShortlists();updateOvernightShortlistMode();syncReadySymbolsFromTopFour();renderMorningReport();});
async function fetchOvernightTopFourEvidence(){
 const rows=Array.isArray(scannerData.imported)?scannerData.imported:[];const button=$("overnightQualRefreshMarket"),status=$("overnightQualificationStatus");
 if(!rows.length){if(status)status.textContent="Import the Overnight Scanner CSV first.";return;}
 const scoreOf=item=>{const raw=String(item.score??"").replace(/,/g,"").match(/-?\d+(?:\.\d+)?/);return raw?Number(raw[0]):-Infinity;};
 const sortedRows=rows.slice().sort((a,b)=>scoreOf(b)-scoreOf(a)||String(a.symbol).localeCompare(String(b.symbol)));
 const uniqueRows=[];const seenSymbols=new Set();
 for(const item of sortedRows){const symbol=String(item.symbol||"").trim().toUpperCase();if(!symbol||seenSymbols.has(symbol))continue;seenSymbols.add(symbol);uniqueRows.push(item);}
 const ranked=uniqueRows;
 const selected=overnightShortlistMode==="manual"?overnightManualSymbols.map(symbol=>uniqueRows.find(item=>String(item.symbol||"").trim().toUpperCase()===symbol)).filter(Boolean):getBoringOvernightCandidateRows(4);
 if(selected.length!==4){const warn=document.createElement("span");warn.className="import-warning";if(status)status.replaceChildren(warn);warn.textContent=overnightShortlistMode==="manual"?"Select exactly four symbols in My Top 4 before fetching.":(scannerData.importedHasOvernightColumns===false?"⚠ Nothing fetched: Boring Top 4 is empty because the imported CSV has no Overnight columns (ON Win %, AVG ON %, and the rest). Re-export with those columns, or open the picker, choose four symbols, click My Top 4, then Fetch again.":"⚠ Nothing fetched: MCC could not identify four distinct imported candidates. Use My Top 4 instead.");return;}
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
 renderOvernightTopFourEvidence(results);
 const failed=results.filter(x=>x.evidence.dailyError||x.evidence.catalystError);
 renderCatalystHumanReviewQueue();
 if(status)status.textContent="Live evidence fetched for "+results.length+" distinct candidates ("+results.map(x=>x.symbol).join(", ")+"): "+(results.length-failed.length)+" complete, "+failed.length+" with provider/API errors. The human-review queue lists only articles actually returned; a symbol with no returned articles remains UNKNOWN, not CLEAR. Other imported rows remain DATA?. No candidate can become READY because catalyst clearance and 5M/1M evidence are not validated.";
}


function getOvernightTopSymbols(limit=20){
 return getBoringOvernightCandidateRows(limit).map(item=>String(item.symbol||"").trim().toUpperCase()).filter(Boolean);
}
function populateDailyAuditFromOvernightTop20(){
 const scannerInput=$("apiScanSymbols"),topSymbols=getOvernightTopSymbols(20);
 if(scannerInput&&topSymbols.length)scannerInput.value=topSymbols.join(",");

 const input=$("dailyAuditSymbols"),status=$("dailyAuditStatus");
 const symbols=getOvernightTopSymbols(20);
 if(!input||!symbols.length)return;
 input.value=symbols.join(",");
 if(status)status.textContent="Populated with "+symbols.length+" unique Boring Overnight shortlist symbols, median-first. Click Validate Daily data to request their 20-session evidence; this step only fills the input.";
}

async function fetchDailyDataAudit(){
 const input=$("dailyAuditSymbols"),button=$("dailyAuditRefresh"),status=$("dailyAuditStatus"),body=$("dailyAuditRows");
 if(!input||!body)return;
 const symbols=[...new Set(input.value.split(",").map(x=>x.trim().toUpperCase()).filter(x=>/^[A-Z0-9.-]{1,8}$/.test(x)))];
 if(!symbols.length){if(status)status.textContent="Enter at least one valid ticker.";return;}
 if(symbols.length>20){if(status)status.textContent="Daily data audit accepts at most 20 symbols per run.";return;}
 if(button)button.disabled=true;
 if(status)status.textContent="Requesting Daily bars and recalculating 20-session metrics for "+symbols.join(", ")+"…";
 const results=new Array(symbols.length);let nextIndex=0;
 const worker=async()=>{while(true){const index=nextIndex++;if(index>=symbols.length)return;const symbol=symbols[index];
  try{
   const response=await fetch("/api/market-data/daily?symbol="+encodeURIComponent(symbol),{cache:"no-store",headers:{Accept:"application/json"}});
   const payload=await response.json();
   if(!response.ok)throw new Error(payload.detail||payload.title||payload.error||("HTTP "+response.status));
   results[index]={symbol,payload,error:null};
  }catch(error){results[index]={symbol,payload:null,error:error.message};}
 }};
 await Promise.all(Array.from({length:Math.min(4,symbols.length)},worker));
 if(button)button.disabled=false;
 const cell=value=>{const td=document.createElement("td");td.textContent=value===null||value===undefined||value===""?"DATA?":String(value);return td;};
 const fmt=(value,digits=4)=>value===null||value===undefined||!Number.isFinite(Number(value))?"DATA?":Number(value).toLocaleString(undefined,{maximumFractionDigits:digits});
 body.replaceChildren(...results.map(result=>{
  const tr=document.createElement("tr");
  if(result.error){
   tr.append(cell(result.symbol),cell("ERROR · "+result.error),cell(null),cell(null),cell("DATA? / 20"),cell(null),cell(null),cell(null),cell(null),cell("UNKNOWN"),cell("DATA?"),cell(null));
   tr.className="qualification-pending";return tr;
  }
  const d=result.payload||{},complete=d.validSessionCount===20&&Array.isArray(d.missingSessionDates)&&d.missingSessionDates.length===0;
  const sipStatus=typeof d.sipRequestStatus==="string"?d.sipRequestStatus:"";
  const feedLabel=(d.provider||"UNKNOWN")+" / "+(d.feed||"UNKNOWN")+(sipStatus==="SIP_DELAYED_HISTORICAL_OK"?" · delayed historical SIP":sipStatus?" · SIP refused, fell back":"");
  const latest=(d.latestExpectedSessionDate||"EXPECTED UNKNOWN")+" · actual "+(d.latestCompletedSessionTimestampUtc||"MISSING");
  const sessionLabel=(d.validSessionCount??0)+"/20";
  const missing=Array.isArray(d.missingSessionDates)&&d.missingSessionDates.length?d.missingSessionDates.join(", "):complete?"NONE":"UNKNOWN";
  const fresh=d.dataFresh===true?"FRESH":d.dataFresh===false?"STALE / LATEST MISSING":"UNKNOWN";
  const qualified=d.qualificationUsable===true?"QUALIFICATION USABLE":"DATA? · "+(d.feedCoverage||d.completeness||"INCOMPLETE");
  const missingCell=cell(missing);missingCell.className=missing==="NONE"?"audit-status-good":missing==="UNKNOWN"?"audit-status-unknown":"audit-status-warning";
  tr.append(cell(result.symbol),cell(feedLabel),cell(d.adjustment),cell(latest),cell(d.retrievedAtUtc),cell(sessionLabel),cell(fmt(d.observedClose)),cell(fmt(d.observedSma20)),cell(fmt(d.observedAverageDailyVolume20,0)),cell(fresh),cell(qualified),missingCell);
  tr.className=d.qualificationUsable===true?"qualification-pending":"qualification-blocked";
  return tr;
 }));
 const errors=results.filter(x=>x.error).length;
 const passes=results.filter(x=>x.payload?.qualificationUsable===true).length;
 if(status)status.textContent="Daily API checked "+results.length+" symbol(s): "+passes+" qualification-usable, "+(results.length-passes-errors)+" returned but DATA? / diagnostic-only, "+errors+" API errors. Values shown as observed source metrics; no independent-source parity is implied.";
}
$("dailyAuditRefresh")?.addEventListener("click",()=>void fetchDailyDataAudit());

$("tosDailyReferenceImport")?.addEventListener("click",async()=>{
 const status=$("tosDailyReferenceStatus"),file=$("tosDailyReferenceFile")?.files?.[0];
 const symbol=String($("tosDailyReferenceSymbol")?.value||"").trim().toUpperCase();
 const rowsHost=$("tosDailyReferenceRows"),button=$("tosDailyReferenceImport");
 if(!/^[A-Z0-9.-]{1,8}$/.test(symbol)){status.textContent="Enter one valid symbol.";return;}
 if(!file){status.textContent="Choose a Thinkorswim daily OHLC CSV first.";return;}
 button.disabled=true;status.textContent="Reading TOS daily OHLC reference…";
 try{
  const raw=String(await file.text()).replace(/^\uFEFF/,""),lines=raw.split(/\r?\n/).filter(x=>x.trim());
  if(lines[0]&&/^sep=./i.test(lines[0]))lines.shift();
  if(!lines.length)throw new Error("CSV is empty.");
  const delimiter=detectCsvDelimiter(lines[0]),headers=parseCsvLine(lines[0],delimiter).map(normalizeCsvHeader);
  const find=(...names)=>names.map(n=>headers.indexOf(n)).find(i=>i>=0);
  const ix={date:find("date","sessiondate","tradingdate"),open:find("open"),high:find("high"),low:find("low"),close:find("close")};
  if(Object.values(ix).some(v=>v===undefined))throw new Error("CSV requires Date, Open, High, Low, Close headers.");
  const bars=lines.slice(1).map((line,index)=>{
   const v=parseCsvLine(line,delimiter),dateText=String(v[ix.date]||"").trim();
   let dateMatch=dateText.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/);
   let sessionDate;
   if(dateMatch)sessionDate=dateMatch[1]+"-"+dateMatch[2].padStart(2,"0")+"-"+dateMatch[3].padStart(2,"0");
   else {const us=dateText.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);if(us)sessionDate=us[3]+"-"+us[1].padStart(2,"0")+"-"+us[2].padStart(2,"0");}
   const number=i=>Number(String(v[i]??"").replace(/[$,%\s,]/g,""));
   if(!sessionDate||[ix.open,ix.high,ix.low,ix.close].some(i=>!Number.isFinite(number(i))))return null;
   return {sessionDate,open:number(ix.open),high:number(ix.high),low:number(ix.low),close:number(ix.close)};
  }).filter(Boolean).sort((a,b)=>a.sessionDate.localeCompare(b.sessionDate));
  if(bars.length<21)throw new Error("Only "+bars.length+" valid daily bars parsed; 21 are required to calculate 20 daily gaps.");
  const request={symbol,source:"Thinkorswim daily OHLC CSV",aggregation:"DAY",extendedHours:$("tosDailyExtendedHours").checked,
   corporateActionAdjusted:$("tosDailyAdjusted").checked,chartTimeZone:$("tosDailyTimezone").value.trim(),bars};
  const response=await fetch("/api/overnight-scanner/thinkorswim-daily-bars",{method:"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},body:JSON.stringify(request)});
  const payload=await response.json();if(!response.ok)throw new Error(payload.error||payload.detail||("HTTP "+response.status));
  const calc=payload.calculation||{},sourceRow=(scannerData.imported||[]).find(x=>String(x.symbol).toUpperCase()===symbol),m=sourceRow?.overnightMetrics||{};
  const values=[
   ["ON WIN %",m.onWin,calc.onWinPct,2],["AVG ON %",m.avgOn,calc.avgOnPct,2],["Median ON %",m.medianOn,calc.medianOnPct,2],
   ["5D ON %",m.fiveDayOn,calc.fiveDayOnPct,2],["ON Momentum",m.onMomentum,calc.onMomentum,2],
   ["Current ON %",m.currentOn,calc.currentOnPct,2],["Above 20 SMA",m.above20Sma,calc.above20Sma,0],["Overnight Score",sourceRow?.score,calc.overnightScore,2]
  ];
  rowsHost.replaceChildren(...values.map(([label,tos,mcc,digits])=>{
   const tr=document.createElement("tr"),a=Number(String(tos??"").replace(/[% ,]/g,"")),b=Number(mcc);
   const valid=tos!==null&&tos!==undefined&&String(tos).trim()!==""&&Number.isFinite(a)&&Number.isFinite(b);
   const match=valid&&a.toFixed(digits)===b.toFixed(digits);
   for(const value of [label,valid?a.toFixed(digits):"DATA?",Number.isFinite(b)?b.toFixed(digits):"DATA?",valid?(match?"MATCH":"MISMATCH"):"NO SCANNER VALUE"]){const td=document.createElement("td");td.textContent=value;tr.append(td);}
   tr.className=valid?(match?"qualification-pending":"qualification-blocked"):"qualification-pending";return tr;
  }));
  const matches=values.filter(([_,tos,mcc,d])=>tos!==null&&tos!==undefined&&String(tos).trim()!==""&&Number.isFinite(Number(String(tos).replace(/[% ,]/g,"")))&&Number.isFinite(Number(mcc))&&Number(String(tos).replace(/[% ,]/g,"")).toFixed(d)===Number(mcc).toFixed(d)).length;
  status.textContent=payload.status+" · "+symbol+" · "+bars.length+" TOS daily bars · "+matches+"/8 displayed fields match to scanner display precision · "+calc.calculationVersion+" · import "+payload.importId+". This verifies arithmetic against the supplied TOS bars, not the aggregation's extended-hours semantics or independent source entitlement.";
 }catch(error){status.textContent="TOS reference import/parity failed: "+error.message;}
 finally{button.disabled=false;}
});

$("dailyAuditTop20")?.addEventListener("click",populateDailyAuditFromOvernightTop20);

function renderOvernightTopFourEvidence(results){
 const body=$("overnightTopFourEvidenceRows");if(!body)return;
 const cell=value=>{const td=document.createElement("td");td.textContent=value===null||value===undefined||value===""?"UNKNOWN":String(value);return td;};
 body.replaceChildren(...results.map(({symbol,evidence})=>{
  const d=evidence.daily,c=evidence.catalyst,articles=Array.isArray(c?.articles)?c.articles:[],tr=document.createElement("tr");
  const dailyResult=evidence.dailyError?"ERROR":d?.qualificationUsable===true?"QUALIFICATION USABLE":d?.completeness||"DATA?";
  const dailyCoverage=d?[d.feedCoverage||"UNKNOWN",(d.validSessionCount??0)+"/20 sessions",d.dataFresh===true?"FRESH":"NOT FRESH"].join(" · "):"UNKNOWN";
  const catalystResult=evidence.catalystError?"ERROR":c?.status||"UNKNOWN";
  const coverage=c?(c.coverageComplete===true?"COMPLETE":"INCOMPLETE / UNKNOWN"):"UNKNOWN";
  // An absent error is "none", not UNKNOWN (UNKNOWN stays for genuinely missing values). 2026-11-16.
  const errorCell=(error,succeeded)=>cell(error?error:succeeded?"none":null);
  tr.append(cell(symbol),cell(dailyResult),cell(dailyCoverage),errorCell(evidence.dailyError,!!d),cell(catalystResult),cell(articles.length),cell(coverage),errorCell(evidence.catalystError,!!c));
  tr.className=evidence.dailyError||evidence.catalystError?"qualification-blocked":"qualification-pending";return tr;
 }));
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
  const notes=document.createElement("textarea");notes.maxLength=2000;notes.placeholder="At least 40 characters: cite specific facts from the article and explain why they fit this label. Do not rely on a recommendation.";notes.setAttribute("aria-label","Review notes for "+symbol);
  const save=document.createElement("button");save.type="button";save.className="quiet-button";save.textContent="Save human review";
  const status=document.createElement("span");status.className="muted small";
  save.addEventListener("click",async()=>{
   if(!article.url||!/^https?:\/\//i.test(article.url)){status.textContent="Cannot save: source article URL is missing or invalid.";return;}
   if(notes.value.trim().length<40||/because you said|because chatgpt said|because the assistant said|you told me|your recommendation|assistant recommendation|after reviewing it|certainly fits|i can confirm this|definitely more than|this is more than|it certainly fits/i.test(notes.value)){status.textContent="Add at least 40 characters of article-specific facts and explain why they support the label. Generic confirmation and assistant recommendations are not evidence.";return;}
   save.disabled=true;status.textContent="Saving append-only review…";
   try{
    const response=await fetch("/api/market-data/catalyst-reviews",{method:"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},body:JSON.stringify({symbol,articleId:article.id||null,headline:article.headline||"Untitled article",url:article.url,publishedAtUtc:article.publishedAtUtc||null,source:"Alpaca News",label:label.value,ruleSetVersion:evidence.catalyst?.ruleSetVersion||"MCC-CATALYST-TRIAGE-1.0",notes:notes.value||null,predictedSeverity:item.severity||"UNCLASSIFIED",predictedCategories:item.categories||[]})});
    const payload=await response.json();if(!response.ok)throw new Error(payload.detail||payload.title||payload.error||("HTTP "+response.status));
    status.textContent="Saved "+label.value+" · "+(payload.reviewId||payload.review?.reviewId||"review recorded")+". This does not clear catalyst status.";
    const overall=$("catalystReviewStatus");
    if(overall){
     overall.textContent="Human review appended for "+symbol+". Refreshing chronological triage validation…";
     try{
      const validationResponse=await fetch("/api/market-data/catalyst-reviews/validation",{cache:"no-store",headers:{Accept:"application/json"}});
      const validation=await validationResponse.json();
      if(!validationResponse.ok)throw new Error(validation.detail||validation.title||"HTTP "+validationResponse.status);
      const holdout=validation.holdout||{};
      const pct=value=>value==null?"n/a":(value*100).toFixed(1)+"%";
      const excluded=validation.exclusionBreakdown||{};
      const exclusionText="Excluded: "+(excluded.missingSavedPrediction||0)+" missing saved prediction, "+(excluded.nonEvaluableLabel||0)+" non-evaluable label, "+(excluded.duplicateSameLabelReview||0)+" duplicate review, "+(excluded.conflictingArticleReview||0)+" conflicting-article review(s).";
      overall.textContent=validation.status==="TIME_ORDERED_HOLDOUT_METRICS_AVAILABLE"
       ?"Human labels: "+validation.includedUniqueReviewCount+" unique · chronological holdout N="+validation.holdoutCount+" · precision "+pct(holdout.precision)+" · recall "+pct(holdout.recall)+". "+exclusionText+" Descriptive only; not catalyst clearance."
       :"Human labels saved: "+validation.includedUniqueReviewCount+" usable unique review(s); holdout validation "+validation.status+". "+exclusionText+" Need both human-positive and human-negative labels in the later time slice. No catalyst clearance.";
     }catch(validationError){overall.textContent="Human review saved; validation metrics unavailable: "+validationError.message+". Automated triage remains unchanged.";}
    }
   }catch(error){status.textContent="Review save failed: "+error.message;save.disabled=false;}
  });
  controls.append(label,notes,save);card.append(title,meta,summary,link,controls,status);return card;
 }));
}

async function renderOvernightQualification(){
 const body=$("overnightQualificationRows");
 if(!body)return;
 const rows=Array.isArray(scannerData.imported)?scannerData.imported:[];
 renderOvernightShortlistPicker(rows);updateOvernightShortlistMode();if(typeof syncReadySymbolsFromTopFour==="function")syncReadySymbolsFromTopFour();
 const setText=(id,value)=>{const node=$(id);if(node)node.textContent=String(value);};
 setText("overnightQualImported",rows.length);
 if(!rows.length){
  body.replaceChildren(Object.assign(document.createElement("tr"),{innerHTML:"<td colspan=\"15\">Import the Overnight Scanner CSV from Scanner Workbench → Candidate discovery.</td>"}));
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
  const onWin=metric(m.onWin),avgOn=metric(m.avgOn),medianOn=metric(m.medianOn),fiveDay=metric(m.fiveDayOn),onMomentum=metric(m.onMomentum),currentOn=metric(m.currentOn),above20=metric(m.above20Sma),score=metric(item.score);
  const checks=[onWin>=60,avgOn>=0.10,fiveDay>avgOn,currentOn<3,above20===1];
  // All eight source columns are required for a complete imported scanner observation.
  // Do not call a row PASS when Median, Momentum, or Overnight Score is missing.
  const requiredMetrics=[onWin,avgOn,medianOn,fiveDay,onMomentum,currentOn,above20,score];
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
  const dailyGate=price!==null&&averageVolume!==null?(price<5||averageVolume<500000?"FAIL":"PASS"):"DATA?";
  const catalystStatus=catalyst?.status||"UNKNOWN";
  const catalystLabel=live.catalystError?"ERROR · "+live.catalystError:!catalyst?(live.dailyError||live.daily?"NOT LOADED":"NOT FETCHED"):catalystStatus+(catalyst.coverageComplete===false?" · COVERAGE INCOMPLETE":"");
  const catalystRisk=catalystStatus==="RISK_PRESENT";
  const decision=isExcluded===true?"BLOCKED · SCHWAB EXCLUSION":exclusionError?"DATA? · EXCLUSION LIST":history==="FAIL"?"BLOCKED · HISTORICAL GATE":dailyGate==="FAIL"?"BLOCKED · DAILY PRICE / LIQUIDITY":catalystRisk?"BLOCKED · CATALYST RISK":daily?.qualificationUsable!==true?"DATA? · DAILY SIP / 20 SESSIONS":dailyGate!=="PASS"?"DATA? · DAILY METRICS":!catalyst||catalyst.coverageComplete!==true||catalystStatus!=="CLEAR_WITH_SOURCE"?"MANUAL REVIEW · CATALYST": "MANUAL REVIEW · 5M / 1M";
  return {...item,onWin,avgOn,medianOn,fiveDay,onMomentum,currentOn,above20,score,history,isExcluded,decision,daily,price,averageVolume,dailyGate,catalystLabel,live};
 }).sort((a,b)=>(metric(b.score)??-Infinity)-(metric(a.score)??-Infinity)||String(a.symbol).localeCompare(String(b.symbol)));
 const cell=value=>{const td=document.createElement("td");td.textContent=value===null||value===undefined?"DATA?":String(value);return td;};
 body.replaceChildren(...evaluated.map(item=>{
  const tr=document.createElement("tr");
  tr.append(cell(item.symbol),cell(item.onWin),cell(item.avgOn),cell(item.medianOn),cell(item.fiveDay),cell(item.onMomentum),cell(item.currentOn),cell(item.above20===null?null:item.above20===1?"YES":"NO"),cell(item.score),cell(item.history),cell(exclusionError?"DATA?":item.isExcluded?"EXCLUDED":"NO"),cell(item.price===null?(item.daily?.feedCoverage==="IEX_PARTIAL"?"DATA? · IEX PARTIAL":item.live.dailyError?"ERROR · "+item.live.dailyError:null):fmt(item.price)),cell(item.averageVolume===null?(item.live.dailyError?"ERROR · "+item.live.dailyError:null):fmt(item.averageVolume)),cell(item.catalystLabel),cell(item.decision));
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
/// PURPOSE: Report scanner custom-field availability for an imported Watchlist Scanner CSV.
/// WHEN CALLED: After a scanner import or a restored import.
/// IMPORTANT: Missing/placeholder/non-numeric custom-expression output (e.g. a subscription-limit message)
/// counts as unavailable. Denominators are the rows in THIS import only; earlier counts are not reused.
const scannerAvailabilityFields=[["ON Win %","onWin"],["AVG ON %","avgOn"],["Median ON %","medianOn"],["5D ON %","fiveDayOn"],["ON Momentum","onMomentum"],["Current ON %","currentOn"],["Above 20 SMA","above20Sma"],["Overnight Score","score"]];
function scannerFieldValue(row,key){return key==="score"?row?.score:row?.overnightMetrics?.[key];}
function isScannerFieldAvailable(value){const raw=String(value??"").trim();if(!raw||/^(loading|data\?|—|n\/a)$/i.test(raw))return false;return Number.isFinite(Number(raw.replace("%","").replace(/,/g,"").trim()));}
function computeScannerFieldAvailability(rows,source){
 const list=Array.isArray(rows)?rows:[];
 const fields=scannerAvailabilityFields.map(([label,key])=>{const available=list.filter(row=>isScannerFieldAvailable(scannerFieldValue(row,key))).length;return {label,key,available,total:list.length,missing:list.length-available,pct:list.length?available/list.length*100:null};});
 const symbols=list.map(row=>({symbol:String(row?.symbol||"").toUpperCase(),missing:scannerAvailabilityFields.filter(([,key])=>!isScannerFieldAvailable(scannerFieldValue(row,key))).map(([label])=>label)}));
 const complete=symbols.filter(x=>!x.missing.length).length;
 return {sourceFile:source?.filename||"UNKNOWN",sourceFeed:"THINKORSWIM_WATCHLIST_SCANNER_CSV",importedAtUtc:source?.importedAtUtc||null,rowCount:list.length,completeRowCount:complete,incompleteRowCount:list.length-complete,fields,symbolsWithMissingFields:symbols.filter(x=>x.missing.length)};
}
function renderScannerFieldAvailability(rows,source){
 const host=$("scannerFieldAvailability");if(!host)return null;
 const report=computeScannerFieldAvailability(rows,source);
 const stamp=report.importedAtUtc?new Date(report.importedAtUtc).toLocaleString("en-US",{timeZone:"America/Phoenix"})+" AZ":"UNKNOWN";
 const summary=document.createElement("summary");summary.textContent="Field availability: "+report.completeRowCount+"/"+report.rowCount+" rows complete · "+report.sourceFile+" · imported "+stamp;
 const table=document.createElement("table");table.className="field-availability-table";
 const head=document.createElement("tr");for(const text of ["Field","Available","Missing","Denominator","%"]){const th=document.createElement("th");th.textContent=text;head.append(th);}
 table.append(head);
 for(const field of report.fields){const tr=document.createElement("tr");for(const text of [field.label,field.available,field.missing,field.total,field.pct==null?"DATA?":field.pct.toFixed(1)+"%"]){const td=document.createElement("td");td.textContent=String(text);tr.append(td);}table.append(tr);}
 const note=document.createElement("p");note.className="muted small";note.textContent="Source "+report.sourceFeed+". Denominator = rows in this import. Missing custom-expression output (blank, DATA?, n/a or non-numeric text such as a subscription-limit message) is unavailable, never a failed gate.";
 const missing=document.createElement("p");missing.className="small";missing.textContent=report.symbolsWithMissingFields.length?"Rows with missing fields: "+report.symbolsWithMissingFields.map(x=>x.symbol+" ("+x.missing.join(", ")+")").join(" · "):"No rows have missing fields.";
 const exportButton=document.createElement("button");exportButton.type="button";exportButton.className="quiet-button";exportButton.textContent="Export availability JSON";
 exportButton.addEventListener("click",()=>{const blob=new Blob([JSON.stringify({...report,generatedAtUtc:new Date().toISOString()},null,2)],{type:"application/json"}),url=URL.createObjectURL(blob),a=document.createElement("a");a.href=url;a.download="mcc-scanner-field-availability.json";a.click();URL.revokeObjectURL(url);});
 host.replaceChildren(summary,table,note,missing,exportButton);host.hidden=false;
 return report;
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
  if($("mode")?.value!=="overnight")void recordAutoScanComparison(imported.map(item=>item.symbol),file.name);
  const importStatus=$("overnightImportStatus");if(importStatus)importStatus.textContent="Imported "+imported.length+" symbols from "+file.name+" at "+new Date().toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit"})+" AZ ("+($("mode")?.value==="overnight"?"BORING OVERNIGHT":"INTRADAY MOMENTUM")+" workspace). Red Zone, quality-ranking pool, Boring shortlist and Daily audit symbols updated.";
  // 2026-11-17: an Overnight export without the ON columns imports, but every historical gate is DATA? and Boring Top 4 is empty. Say so at import.
  scannerData.importedHasOvernightColumns=headers.includes("onwin");
  if(importStatus){importStatus.classList.toggle("import-warning",!scannerData.importedHasOvernightColumns&&$("mode")?.value==="overnight");if(!scannerData.importedHasOvernightColumns&&$("mode")?.value==="overnight")importStatus.textContent+=" ⚠ This CSV has no Overnight columns (ON Win %, AVG ON %, Median ON %, 5D ON %, ON Momentum, Current ON %, Above 20 SMA, and Overnight Score), so Boring Top 4 stays empty. Re-export with those columns showing, or use My Top 4, or go straight to 4 READY check (it fetches its own 20-session Daily history).";}
  overnightLiveEvidence.clear();
  populateDailyAuditFromOvernightTop20();
  const importedSymbols=new Set(imported.map(item=>String(item.symbol||"").toUpperCase()).filter(Boolean));
  const redZoneState=readRedZoneState();
  const previouslyDismissed=redZoneState.dismissed.length;
  redZoneState.dismissed=redZoneState.dismissed.filter(symbol=>!importedSymbols.has(String(symbol).toUpperCase()));
  // A fresh scanner import should repopulate the automatic candidate view.
  // Preserve saved pins so the operator can still switch to Manual afterwards.
  redZoneState.mode="automatic";
  const dismissalResetCount=previouslyDismissed-redZoneState.dismissed.length;
  writeRedZoneState(redZoneState);
  let persistenceNote="";
  const importedAtUtc=new Date().toISOString();
  try{localStorage.setItem("mcc.scannerImport.v1",JSON.stringify({filename:file.name,importedAtUtc,rows:imported}));}
  catch(error){persistenceNote=" · browser storage unavailable: "+error.message;}
  renderScannerFieldAvailability(imported,{filename:file.name,importedAtUtc});
  const groupSelect=$("scannerGroup");const option=groupSelect.querySelector('option[value="imported"]')||document.createElement("option");option.value="imported";option.textContent="Imported local CSV";if(!option.parentElement)groupSelect.append(option);groupSelect.value="imported";renderScanner();renderMorningReport();
  // The latest import wins: the Red Zone stops reading the automatic scan until Feed Red Zone is turned on again.
  try{localStorage.setItem("mcc.autoScanFeedsRedZone.v1","false");}catch{/* preference lasts until reload */}
  if(typeof syncAutoScanFeedButton==="function")syncAutoScanFeedButton();
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
 populateDailyAuditFromOvernightTop20();
 const option=document.querySelector('#scannerGroup option[value="imported"]')||document.createElement("option");
 option.value="imported";option.textContent="Imported local CSV";if(!option.parentElement)$("scannerGroup").append(option);
 $("scannerGroup").value="imported";renderScanner();renderMorningReport();void renderOvernightQualification();
 const stamp=restoredScannerImport.importedAtUtc?new Date(restoredScannerImport.importedAtUtc).toLocaleString():"previous session";
 renderScannerFieldAvailability(restoredScannerImport.rows,restoredScannerImport);
 $("scannerSource").textContent=`Restored ${restoredScannerImport.rows.length} local rows from ${restoredScannerImport.filename||"CSV"} · imported ${stamp}`;
 const restoredStatus=$("overnightImportStatus");if(restoredStatus)restoredStatus.textContent=`Current import: ${restoredScannerImport.rows.length} symbols from ${restoredScannerImport.filename||"CSV"} (imported ${stamp}). Import a new file to replace it.`;
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

/// <summary>Plays a short tone pattern per state: prepare = one soft tone, ready = rising pair twice, waning = falling pair, sell = low alternating. Called by observeRedZoneState and the sound toggle.</summary>
function playRedZoneAlert(kind="ready"){
 if(!redZoneSoundEnabled)return;
 const patterns={prepare:{type:"sine",notes:[660]},ready:{type:"sine",notes:[880,1175,880,1175]},waning:{type:"triangle",notes:[784,523]},sell:{type:"square",notes:[440,330,440,330]}};
 const pattern=patterns[kind]||patterns.ready;
 const AudioContextType=window.AudioContext||window.webkitAudioContext;
 if(!AudioContextType)return;
 try{
  redZoneAudioContext=redZoneAudioContext||new AudioContextType();
  if(redZoneAudioContext.state==="suspended")void redZoneAudioContext.resume();
  const now=redZoneAudioContext.currentTime;
  pattern.notes.forEach((frequency,index)=>{
   const oscillator=redZoneAudioContext.createOscillator(),gain=redZoneAudioContext.createGain(),start=now+index*.18;
   oscillator.type=pattern.type;oscillator.frequency.value=frequency;
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
 if($("mode")?.value!=="overnight")void ensureMinuteBarSubscription(symbol);
 try{
  if($("mode")?.value==="overnight"){
   statusNode.textContent="BORING OVERNIGHT · RESEARCH";scoreNode.textContent="Intraday V2 permission: NOT USED";scoreNode.className="redzone-score score-unknown";
   extensionNode.hidden=true;freshnessNode.hidden=true;participationNode.hidden=true;
   reasonNode.textContent="Reviewed under Boring Overnight rules. Intraday EXECUTION READY is not evaluated in this mode.";
   gateNode.textContent="Intraday 5M Permission / 1M execution: NOT USED · Overnight qualification is separate.";
   const q=new URLSearchParams({symbol,timeframe:"5m",from:new Date(Date.now()-5*86400000).toISOString(),to:new Date().toISOString(),providerName:"Alpaca"}),r=await fetch("/api/market-data/bars?"+q,{cache:"no-store"}),d=await r.json();if(!r.ok)throw Error(d.detail||d.error||("HTTP "+r.status));
   drawPriceLine(chartNode,(d.bars||[]).filter(b=>Number.isFinite(Number(b.close))).sort((x,y)=>new Date(x.timestamp)-new Date(y.timestamp)),symbol);return;
  }
  const data=await fetchLiveAssessment(symbol),one=data.oneMinute||{},five=data.fiveMinute||{},decision=data.decision||null;
  extensionNode.hidden=false;freshnessNode.hidden=false;participationNode.hidden=false;
  applyRedZoneCardState(symbol,statusNode,liveStateView(symbol,data));
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
  gateNode.textContent="5M Permission: "+permissionLatchText(five,decision)+
   " · 1M execution: "+(decision?.oneMinuteExecution||"UNKNOWN");
  drawPriceLine(chartNode,one.chartBars||[],symbol);
 }catch(error){
  applyRedZoneCardState(symbol,statusNode,{label:"DATA UNAVAILABLE",tone:"data",held:readHeldSymbols().has(String(symbol||"").toUpperCase()),decision:null,hint:"Live request failed · "+error.message});
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
/// <summary>Colours one Red Zone card, its state banner and chip from a state view, and pulses the card on a transition. Called by updateRedZoneCardAssessment.</summary>
function applyRedZoneCardState(symbol,statusNode,view){
 const card=statusNode.closest(".redzone-card");
 statusNode.textContent=view.held?"HOLDING":"WATCHING";
 statusNode.className="chip state-chip "+(view.held?"state-tone-runner":"state-tone-wait");
 if(!card)return;
 for(const name of [...card.classList])if(name.startsWith("card-tone-"))card.classList.remove(name);
 card.classList.add("card-tone-"+view.tone);
 const banner=card.querySelector(".redzone-state-banner");
 if(banner){banner.className="redzone-state-banner state-tone-"+view.tone;banner.querySelector("strong").textContent=view.label;banner.querySelector("span").textContent=view.hint;}
 if(observeRedZoneState(symbol,view)){card.classList.remove("state-changed");void card.offsetWidth;card.classList.add("state-changed");}
}
async function refreshRedZoneAssessments(){
 await Promise.allSettled(redZoneAssessmentRefreshers.map(refresh=>refresh()));
 const state=readRedZoneState();
 if(state.mode==="automatic"&&$("mode")?.value!=="overnight"){
  const shown=new Set((scannerData[$("scannerGroup")?.value||"momentum"]||[]).slice(0,4).map(x=>String(x.symbol||"").toUpperCase()));
  await Promise.allSettled(state.pinned.filter(s=>!shown.has(String(s).toUpperCase())).map(async s=>{try{const d=await fetchLiveAssessment(s);observeRedZoneState(s,liveStateView(s,d));}catch{/* unknown data does not alert */}}));
 }
}
function boringMetric(value){const raw=String(value??"").trim();if(!raw||/^(loading|data\?|—|n\/a)$/i.test(raw))return null;const parsed=Number(raw.replace(/,/g,"").replace("%","").trim());return Number.isFinite(parsed)?parsed:null;}
function getBoringOvernightCandidateRows(limit=4){
 const rows=Array.isArray(scannerData.imported)?scannerData.imported:[];
 const eligible=rows.filter(item=>{const m=item.overnightMetrics||{},win=boringMetric(m.onWin),avg=boringMetric(m.avgOn),median=boringMetric(m.medianOn),five=boringMetric(m.fiveDayOn),momentum=boringMetric(m.onMomentum),current=boringMetric(m.currentOn),above=boringMetric(m.above20Sma);return [win,avg,median,five,momentum,current,above,boringMetric(item.score)].every(x=>x!==null)&&win>=60&&avg>=0.10&&median>=avg&&five>avg&&Math.abs(current)<3&&above===1;});
 return eligible.sort((a,b)=>{const x=a.overnightMetrics||{},y=b.overnightMetrics||{};const win=boringMetric(y.onWin)-boringMetric(x.onWin);if(win)return win;const median=boringMetric(y.medianOn)-boringMetric(x.medianOn);if(median)return median;const consistency=Math.abs(boringMetric(x.avgOn)-boringMetric(x.medianOn))-Math.abs(boringMetric(y.avgOn)-boringMetric(y.medianOn));if(consistency)return consistency;const momentum=Math.abs(boringMetric(x.onMomentum))-Math.abs(boringMetric(y.onMomentum));if(momentum)return momentum;return String(a.symbol).localeCompare(String(b.symbol));}).slice(0,limit);
}
function renderOvernightRedZone(){
 const grid=$("redZoneGrid");if(!grid)return;const state=readRedZoneState(),rows=Array.isArray(scannerData.imported)?scannerData.imported:[],bySymbol=new Map(rows.map(item=>[String(item.symbol||"").toUpperCase(),item])),dismissed=new Set(state.dismissed||[]);
 const automatic=getBoringOvernightCandidateRows(20).filter(item=>!dismissed.has(String(item.symbol||"").toUpperCase())).slice(0,4),selected=state.mode==="manual"?state.pinned.map(symbol=>bySymbol.get(symbol)||{symbol,score:"DATA?",overnightMetrics:{}}):automatic;
 const ranked=selected.filter((item,index,all)=>all.findIndex(x=>String(x.symbol||"").toUpperCase()===String(item.symbol||"").toUpperCase())===index).slice(0,4);
 const title=$("mcc-static-0072-h2");if(title)title.textContent="Boring Overnight Shortlist";const subtitle=$("reportSubtitle");if(subtitle)subtitle.textContent=state.mode==="manual"?"Manual shortlist · pinned symbols in saved order. Missing historical metrics remain DATA?.":"Automatic shortlist · existing historical scanner checks, ranked by median ON, then win rate and distribution consistency. Extreme current gaps are excluded from this display shortlist. Research only; not trade permission.";
 const eyebrow=document.querySelector("#red-zone .eyebrow");if(eyebrow)eyebrow.textContent="BORING OVERNIGHT · HISTORICAL SHORTLIST";const modeButton=$("toggleRedZoneMode");if(modeButton){modeButton.textContent=state.mode==="manual"?"Mode: Manual":"Mode: Automatic";modeButton.setAttribute("aria-pressed",String(state.mode==="manual"));modeButton.title="Automatic mode shows the boring-profile shortlist; manual mode shows pinned symbols.";}
 const topCount=$("morningTopCount");if(topCount)topCount.textContent=String(ranked.length);
 const cards=ranked.map((item,index)=>{const m=item.overnightMetrics||{},card=document.createElement("article");card.className="redzone-card overnight-redzone-card";const top=document.createElement("div");top.className="redzone-card-top";const rank=document.createElement("span");rank.className="rank-pill";rank.textContent="#"+(index+1);const status=document.createElement("span");status.className="chip";status.textContent=state.mode==="manual"?"MANUAL · REVIEW":"SHORTLISTED · REVIEW";top.append(rank,status);const symbol=document.createElement("h3");symbol.textContent=item.symbol;const score=document.createElement("div");score.className="redzone-score";score.textContent="Overnight Score: "+(item.score??"DATA?");const metrics=document.createElement("dl");metrics.className="overnight-redzone-metrics";for(const [label,value] of [["ON Win",m.onWin],["20D Avg ON",m.avgOn],["Median ON",m.medianOn],["5D ON",m.fiveDayOn],["ON Momentum",m.onMomentum],["Current ON",m.currentOn],["Above 20 SMA",m.above20Sma]]){const wrap=document.createElement("div"),dt=document.createElement("dt"),dd=document.createElement("dd");dt.textContent=label;dd.textContent=value??"DATA?";wrap.append(dt,dd);metrics.append(wrap);}const actions=document.createElement("div");actions.className="redzone-actions";const focus=document.createElement("button");focus.type="button";focus.className="quiet-button";focus.textContent="Open in Main Chart";focus.addEventListener("click",()=>{const input=$("symbol");if(input){input.value=item.symbol;input.dispatchEvent(new Event("change",{bubbles:true}));input.scrollIntoView({behavior:"smooth",block:"center"});}});const manage=document.createElement("button");manage.type="button";manage.className="quiet-button";manage.textContent=state.mode==="manual"?"Remove pin":"Dismiss";manage.addEventListener("click",()=>{const current=readRedZoneState(),ticker=String(item.symbol).toUpperCase();if(current.mode==="manual")current.pinned=current.pinned.filter(x=>x!==ticker);else current.dismissed=[...new Set([...current.dismissed,ticker])];writeRedZoneState(current);});actions.append(focus,manage);card.append(top,symbol,score,metrics,actions);return card;});
 grid.replaceChildren(...(cards.length?cards:[Object.assign(document.createElement("article"),{className:"redzone-empty",textContent:rows.length?"No candidates meet the boring-profile shortlist rules. Review imported metrics and qualification.":"Import an Overnight Scanner CSV above to populate this shortlist."})]));previousRedZoneSymbols=new Set(ranked.map(x=>String(x.symbol||"").toUpperCase()));const status=$("redZoneManageStatus");if(status)status.textContent=ranked.length?"Boring Overnight shortlist: "+ranked.map(x=>x.symbol).join(", ")+". Intraday V2 permission and 5M/1M execution are not used in this workspace.":"No boring-profile candidates currently meet the shortlist display rules.";
}
function renderMorningReport(){
 if($("mode")?.value==="overnight"){renderOvernightRedZone();return;}
 const grid=$("redZoneGrid");if(!grid)return;
 // Restore the intraday Red Zone heading; renderOvernightRedZone() rewrites it when the Overnight workspace is shown.
 const intradayEyebrow=document.querySelector("#red-zone .eyebrow");if(intradayEyebrow)intradayEyebrow.textContent="TOP CANDIDATES · MULTI-PANE MONITOR";
 const intradayTitle=$("mcc-static-0072-h2");if(intradayTitle)intradayTitle.textContent="Red Zone";
 const intradayStatus=$("redZoneManageStatus");if(intradayStatus&&/boring/i.test(intradayStatus.textContent))intradayStatus.textContent="Red Zone shows up to four symbols from your imported TOS watchlist or pins. Technical card assessments refresh every 15 seconds.";
 redZoneAssessmentRefreshers=[];
 const group=$("scannerGroup")?.value||"momentum";
 const sourceRows=Array.isArray(scannerData[group])?scannerData[group]:[];
 const state=readRedZoneState();
 const bySymbol=new Map(sourceRows.filter(x=>x&&x.symbol).map(x=>[String(x.symbol).toUpperCase(),x]));
 const pinnedRows=state.pinned.map(symbol=>bySymbol.get(symbol)||({symbol,score:"DATA?",status:"MANUAL — REVIEW",extension:"DATA?",freshness:"UNKNOWN",reason:"Manually pinned candidate. Source data, 5M Permission, and 1M execution are not established."}));
 // INTRADAY MOMENTUM (2026-11-16): imported rows are the operator's TOS watchlist. They are NOT filtered by the
 // Boring Overnight historical rules (that filter left the intraday Red Zone empty for a normal watchlist export).
 // Rows with a numeric Score column sort first by score; rows without one keep the TOS watchlist order.
 const originalIndex=new Map(sourceRows.map((row,index)=>[row,index]));
 const sortedRows=sourceRows.slice().sort((a,b)=>{
  const sa=Number(a.score),sb=Number(b.score);
  if(Number.isFinite(sa)!==Number.isFinite(sb))return Number.isFinite(sb)-Number.isFinite(sa);
  const av=Number.isFinite(sa)?(group==="live"?Math.abs(sa):sa):-Infinity;
  const bv=Number.isFinite(sb)?(group==="live"?Math.abs(sb):sb):-Infinity;
  return bv-av||(originalIndex.get(a)-originalIndex.get(b));
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
 const topCount=$("morningTopCount");if(topCount)topCount.textContent=String(ranked.length);
 $("reportSubtitle").textContent=state.mode==="manual"
  ?"Manual mode · pinned symbols in your saved order · pins and dismissals are preserved when switching modes."
  :(group==="imported"?"Automatic preview · first four of your imported TOS watchlist (by a Score column when the file has one, otherwise in watchlist order) · use 4.1 Rank quality pool for a live ranking · not trade permission.":"Automatic preview · sorted by supplied scanner score · NOT a calibrated live Opportunity Quality rank.");
 grid.replaceChildren(...(ranked.length?ranked.map((item,index)=>{
  const card=document.createElement("article");card.className="redzone-card";
  const top=document.createElement("div");top.className="redzone-card-top";
  const rank=document.createElement("span");rank.className="rank-pill";rank.textContent="#"+(index+1);
  const status=document.createElement("span");status.className="chip";status.textContent=item.status||"DATA?";
  top.append(rank,status);
  const symbol=document.createElement("h3");symbol.textContent=item.symbol||"UNKNOWN";
  const banner=document.createElement("div");banner.className="redzone-state-banner state-tone-data";banner.setAttribute("aria-live","polite");
  const bannerLabel=document.createElement("strong");bannerLabel.textContent="LOADING STATE…";const bannerHint=document.createElement("span");bannerHint.textContent="Requesting live 1M/5M assessment.";banner.append(bannerLabel,bannerHint);
  card.classList.add("card-tone-data");
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
  const heldButton=document.createElement("button");heldButton.type="button";heldButton.className="quiet-button held-toggle";
  const heldNow=readHeldSymbols().has(String(item.symbol||"").toUpperCase());
  heldButton.textContent=heldNow?"HOLDING · click to clear":"Mark holding";heldButton.setAttribute("aria-pressed",String(heldNow));
  heldButton.title="Tell MCC you hold this stock so the card shows sell-side states (RUNNER, RUNNER WANING, PREPARE TO SELL, SELL). Stored in this browser only; MCC has no broker position data.";
  heldButton.addEventListener("click",event=>{
   event.stopPropagation();
   const next=heldButton.getAttribute("aria-pressed")!=="true";
   setHeldSymbol(item.symbol,next);
   heldButton.textContent=next?"HOLDING · click to clear":"Mark holding";heldButton.setAttribute("aria-pressed",String(next));
   void updateRedZoneCardAssessment(item.symbol,status,score,ext,fresh,participation,reason,gate,chart);
  });
  actions.append(heldButton,focusButton,manageButton);
  card.append(top,symbol,banner,score,ext,fresh,participation,reason,chart,gate,actions);
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
$("populateScannerTop20")?.addEventListener("click",()=>{
 const symbols=getOvernightTopSymbols(20),input=$("apiScanSymbols");if(input&&symbols.length)input.value=symbols.join(",");
 const status=$("scannerSource");if(status)status.textContent=symbols.length?"Scanner Workbench populated with "+symbols.length+" unique symbols sorted by Overnight Score.":"Import an Overnight Scanner CSV first.";
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
$("rankOpportunityPool")?.addEventListener("click",async()=>{
 const status=$("opportunityPoolRankingStatus"),host=$("opportunityPoolRanking"),button=$("rankOpportunityPool");
 const imported=Array.isArray(scannerData.imported)?scannerData.imported:[];
 if(!imported.length){if(status)status.textContent="Import a Thinkorswim scanner CSV before ranking the explicit candidate pool.";return;}
 const ordered=imported.slice().sort((a,b)=>{
  const av=Number(a.score),bv=Number(b.score);
  return (Number.isFinite(bv)?bv:-Infinity)-(Number.isFinite(av)?av:-Infinity);
 });
 const symbols=[...new Set(ordered.map(row=>String(row.symbol||"").trim().toUpperCase()).filter(Boolean))].slice(0,50);
 if(!symbols.length){if(status)status.textContent="No valid symbols were found in the imported scanner pool.";return;}
 if(button)button.disabled=true;
 if(status)status.textContent="Assessing "+symbols.length+" candidate(s) with bounded API concurrency. This may take a while.";
 if(host)host.replaceChildren();
 try{
  const response=await fetch("/api/market-data/opportunity-quality/rank?symbols="+encodeURIComponent(symbols.join(",")),{cache:"no-store",headers:{Accept:"application/json"}});
  const payload=await response.json();
  if(!response.ok)throw new Error(payload.detail||payload.title||payload.error||("HTTP "+response.status));
  const ranked=Array.isArray(payload.ranked)?payload.ranked:[];
  const cards=ranked.map(item=>{
   const card=document.createElement("article");card.className="catalyst-review-card";
   const title=document.createElement("strong");title.textContent="#"+item.rank+" · "+item.symbol+" · "+(item.displayLabel||item.classification||"QUALITY — UNKNOWN");
   const metrics=document.createElement("p");metrics.className="muted small";
   const value=x=>x==null?"DATA?":Number(x).toFixed(2);
   metrics.textContent="RVOL "+value(item.relativeVolume)+" · 30×1M range/ATR "+value(item.recentRangeInAtr)+" · prior-high distance/ATR "+value(item.resistanceDistanceInAtr)+" · 1M entry "+(item.entryAllowed==null?"UNKNOWN":item.entryAllowed?"READY":"NOT READY");
   // Reason codes as colour-coded chips: red = blocks the candidate, grey = data missing/stale/after hours,
   // amber = caution (weakens the setup), green = supportive. Unknown codes stay neutral.
   const reasons=document.createElement("p");reasons.className="small reason-chips";
   const reasonList=item.reasons||[];
   if(!reasonList.length)reasons.textContent="No reason codes returned.";
   reasonList.forEach(code=>{const chip=document.createElement("span");chip.className="reason-chip reason-"+qualityReasonTone(code);chip.textContent=code;chip.title=qualityReasonTitle(code);reasons.append(chip);});
   title.classList.add("quality-title","quality-"+String(item.status||item.classification||"UNKNOWN").replace(/[^A-Za-z]/g,"").toUpperCase());
   const stamp=x=>x?new Date(x).toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit",second:"2-digit"})+" AZ":"UNKNOWN";
   const provenance=document.createElement("p");provenance.className="muted small";
   provenance.textContent="Source "+(item.provider||"UNKNOWN")+" / "+(item.feed||"UNKNOWN")+" · 1M bar "+stamp(item.oneMinuteSourceTimestampUtc)+" · 5M bar "+stamp(item.fiveMinuteSourceTimestampUtc)+" · evaluated "+stamp(item.evaluatedAtUtc)+(item.excluded?" · EXCLUSION WATCHLIST — BLOCKED":"");
   card.append(title,metrics,reasons,provenance);return card;
  });
  const legend=document.createElement("p");legend.className="small reason-chips reason-legend";
  [["blocked","Blocks"],["data","Data missing / stale / after hours"],["caution","Caution"],["good","Supportive"]].forEach(([tone,label])=>{const chip=document.createElement("span");chip.className="reason-chip reason-"+tone;chip.textContent=label;legend.append(chip);});
  if(host)host.replaceChildren(...(cards.length?[legend,...cards]:[Object.assign(document.createElement("p"),{className:"muted small",textContent:"No ranking results returned."})]));
  if(status)status.textContent="Ranked "+ranked.length+" of "+imported.length+" imported symbols ("+(payload.excludedCount??0)+" excluded, "+(payload.dataUnknownCount??0)+" DATA?"+(payload.exclusionListChecked===false?"; EXCLUSION LIST NOT CHECKED: "+(payload.exclusionError||"unknown error"):"")+"). Pool scope: top "+symbols.length+" by imported Overnight Score. Diagnostic ordering only; not calibrated, not market-wide, and never overrides 5M/1M eligibility. DATA? candidates remain visible.";
 }catch(error){if(status)status.textContent="Opportunity Quality ranking failed: "+error.message;}
 finally{if(button)button.disabled=false;}
});
/// PURPOSE: Colour tone for an Opportunity Quality reason code (blocked, data, caution, good, neutral).
/// WHEN CALLED: Rendering each ranking card's reason chips.
function qualityReasonTone(code){
 const c=String(code||"");
 if(c==="EXCLUDED_WATCHLIST"||c==="EntryNotEligible")return "blocked";
 if(c==="DataMissing"||c==="DataStale"||c==="OutsideRegularSession")return "data";
 if(["LowRelativeVolume","MomentumNotConfirmed","LimitedRecentRange","ResistanceTooClose","EntryExtended","ExecutionEventInactive"].includes(c))return "caution";
 if(["StrongParticipation","MomentumConfirmed","AdequateMovementCapacity"].includes(c))return "good";
 return "neutral";
}
/// PURPOSE: Plain-English tooltip for a reason code.
/// WHEN CALLED: Rendering each reason chip.
function qualityReasonTitle(code){
 return ({EXCLUDED_WATCHLIST:"On your exclusion watchlist — blocked",EntryNotEligible:"V2 entry not eligible",DataMissing:"Required bars or metrics missing",DataStale:"Latest bars are too old",OutsideRegularSession:"Outside the regular session",LowRelativeVolume:"Relative volume below the threshold",MomentumNotConfirmed:"1M momentum not confirmed",LimitedRecentRange:"Little recent range relative to ATR",ResistanceTooClose:"Prior high is close above",EntryExtended:"Price is extended from its base",ExecutionEventInactive:"No active 1M execution event",StrongParticipation:"Strong relative volume",MomentumConfirmed:"1M momentum confirmed",AdequateMovementCapacity:"Room to move relative to ATR"})[code]||"Reason code";
}
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
   // 2026-11-16: the brief now renders straight into its modal. It no longer writes #reportSubtitle, which was a
   // duplicate id and overwrote the Red Zone subtitle whenever the brief loaded.
   const serviceState=$("overnightServiceState"),serviceDetail=$("overnightServiceDetail");
   if(serviceState)serviceState.textContent = "Morning Brief API: " + report.status;
   if(serviceDetail)serviceDetail.textContent = "Generated " + new Date(report.generatedAtUtc).toLocaleString("en-US",{timeZone:"America/Phoenix"}) + " Arizona. Benchmark bars, Alpaca mover observations, sector ETF proxies, and unclassified news headlines are source data only; no trading decision is inferred.";
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
   const serviceState=$("overnightServiceState"),serviceDetail=$("overnightServiceDetail");
   if(serviceState)serviceState.textContent = "Morning Brief API: NOT CONNECTED";
   if(serviceDetail)serviceDetail.textContent = "Morning Brief request failed: "+error.message+".";
   return null;
  }
 })();
 try{return await morningBriefRequest;}finally{morningBriefRequest=null;}
}
// Morning Brief loads on demand from its modal; it must not block dashboard startup.

/// MODULE: Morning Command Brief modal
/// PURPOSE: Opens the modal and shows the live API brief. Works with either markup: when the page still has the
/// #morning-brief panel (operator commit 528aa1d restored it), the brief renders there and is copied into the modal
/// without ids; when the panel is absent, the brief renders straight into the modal. Export downloads the API brief as JSON.
/// SAFETY: Read-only market snapshot; no trading decision is inferred.
const morningBriefDialog=$("morningBriefDialog");
/// <summary>Copies the page panel's brief into the modal when the panel exists. Called on open and after each load.</summary>
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
 if(!morningBriefCache||Date.now()-morningBriefLoadedAt>=60000)void loadMorningBrief(true).then(renderMorningBriefModal);
});
$("closeMorningBrief")?.addEventListener("click",()=>morningBriefDialog?.close());
$("exportMorningReportModal")?.addEventListener("click",async()=>{
 const report=morningBriefCache||await loadMorningBrief(true);
 if(!report){const d=$("overnightServiceDetail");if(d)d.textContent="Nothing to export: the Morning Brief API did not return a report.";return;}
 const blob=new Blob([JSON.stringify(report,null,2)],{type:"application/json"}),url=URL.createObjectURL(blob),a=document.createElement("a");
 a.href=url;a.download="mcc-morning-brief-"+new Date().toISOString().slice(0,10)+".json";a.click();URL.revokeObjectURL(url);
});

/// MODULE: Exclusions modal (2026-11-16: moved from a page panel to the left dock)
/// PURPOSE: The dock button opens the API-backed exclusion list in a modal; its badge shows how many symbols are excluded.
const exclusionsDialog=$("exclusionsDialog");
$("openExclusions")?.addEventListener("click",()=>{if(!exclusionsDialog||exclusionsDialog.open)return;exclusionsDialog.showModal();void refreshExclusions();$("exclusionSymbol")?.focus();});
$("closeExclusions")?.addEventListener("click",()=>exclusionsDialog?.close());

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
  const badge=$("exclusionCountBadge");if(badge)badge.textContent=String(data.count);
 } catch (e) {
  status.textContent = "Exclusion API unavailable: " + e.message + ". Serve this page through the MCC API.";
  const badge=$("exclusionCountBadge");if(badge)badge.textContent="?";
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
// Restore the ATR provenance controls when an older/mismatched Dashboard HTML
// document is paired with this newer script. These defaults are the operator's
// confirmed V2.1 chart settings; controls remain editable when present.
(function ensureAtrImportControls() {
 const form = $("strategyReportForm");
 if (!form) return;
 const ids = ["atrImportPeriod", "atrImportAverage", "atrImportTimezone", "atrImportExtendedHours", "atrImportAdjustment"];
 if (ids.every(id => $(id))) return;
 let fieldset = form.querySelector("fieldset.atr-import-metadata");
 if (!fieldset) {
  fieldset = document.createElement("fieldset");
  fieldset.className = "atr-import-metadata";
  const legend = document.createElement("legend");
  legend.textContent = "ATR V2 chart provenance";
  fieldset.append(legend);
  const submit = form.querySelector('button[type="submit"]');
  form.insertBefore(fieldset, submit || null);
 }
 const addField = (id, labelText, type, value) => {
  if ($(id)) return;
  const label = document.createElement("label");
  label.htmlFor = id;
  if (type === "checkbox") {
   const input = document.createElement("input");
   input.id = id; input.type = type; input.checked = true;
   label.append(input, document.createTextNode(" " + labelText));
  } else if (type === "select") {
   const select = document.createElement("select"); select.id = id;
   const option = document.createElement("option");
   option.value = "Wilder"; option.textContent = "Wilder"; option.selected = true;
   select.append(option); label.append(labelText + " ", select);
  } else {
   const input = document.createElement("input");
   input.id = id; input.type = type; input.value = value; input.required = true;
   if (type === "number") { input.min = "1"; input.max = "200"; }
   label.append(labelText + " ", input);
  }
  fieldset.append(label);
 };
 addField("atrImportPeriod", "ATR length", "number", "14");
 addField("atrImportAverage", "Average type", "select", "Wilder");
 addField("atrImportTimezone", "Chart timezone", "text", "America/Phoenix");
 addField("atrImportExtendedHours", "Extended hours ON", "checkbox");
 addField("atrImportAdjustment", "Corporate-action adjustment ON", "checkbox");
})();

$("strategyReportForm")?.addEventListener("submit", async event => {
 event.preventDefault();
 const file = $("strategyReportFile").files?.[0];
 const status = $("strategyReportStatus"), summary = $("strategyReportSummary"), download = $("downloadStrategyReportJson"), parityDownload = $("downloadParityCsv");
 if (!file) { status.textContent = "Choose a CSV file first."; return; }
 status.textContent = "Reading and validating " + file.name + "…";
 summary.classList.add("hidden"); download.classList.add("hidden"); parityDownload?.classList.add("hidden"); latestStrategyReportJson = null;
 try {
  const sourceText = await file.text();
  const isAtrV2 = sourceText.includes("MCC_OHLCV_ATR|v2");
  const form = new FormData(); form.append("file", file, file.name);
  const endpoint = isAtrV2 ? "/api/strategy-report/atr-import" : "/api/strategy-report/upload";
  if (isAtrV2) {
   // HTML and JS can be served from different browser-cache generations. Validate
   // every required control before reading it so a stale/mismatched page cannot
   // throw "Cannot read properties of null (reading 'value')" before upload.
   const metadataIds = [
    "atrImportPeriod", "atrImportAverage", "atrImportTimezone",
    "atrImportExtendedHours", "atrImportAdjustment"
   ];
   const missingControls = metadataIds.filter(id => !$(id));
   if (missingControls.length) {
    throw new Error("ATR import controls are missing from this Dashboard page: " +
     missingControls.join(", ") + ". Reload the page without cache (Ctrl+F5) and try again. The CSV was not uploaded.");
   }
   const period = $("atrImportPeriod").value.trim();
   const average = $("atrImportAverage").value.trim();
   const timezone = $("atrImportTimezone").value.trim();
   if (!period || !average || !timezone) {
    throw new Error("ATR import metadata is incomplete. Supply ATR length, average type and chart timezone.");
   }
   form.append("atrPeriod", period);
   form.append("atrAverageType", average);
   form.append("chartTimezone", timezone);
   form.append("extendedHours", String($("atrImportExtendedHours").checked));
   form.append("corporateActionAdjustment", String($("atrImportAdjustment").checked));
  }
  status.textContent = (isAtrV2 ? "Importing and saving ATR V2 observations…" : "Uploading and parsing StrategyReport…");
  const response = await fetch(endpoint, {method:"POST",body:form,headers:{Accept:"application/json"}});
  const body = await response.json();
  if (!response.ok) throw new Error(body.error || ("HTTP " + response.status));
  latestStrategyReportJson = body;
  summary.replaceChildren();
  if (isAtrV2) {
   const intro = document.createElement("p");
   intro.textContent = body.symbol + " · " + body.timeframe + " · " + body.atrObservationCount + " ATR observations · " + body.gapCount + " timestamp gap(s)";
   summary.append(intro);
   const details = document.createElement("p");
   details.textContent = "Source window: " + body.firstTimestampUtc + " through " + body.lastTimestampUtc +
    " · appended " + body.persistence.appendedCount + " · identical skipped " + body.persistence.identicalSkippedCount +
    " · corrections " + body.persistence.correctionCount;
   summary.append(details);
   if (body.gapCount > 0) {
    const warning = document.createElement("p"); warning.className = "upload-warning";
    warning.textContent = "Gaps are retained as gaps; no bars were synthesized. Inspect the returned gap samples before using this data for continuous ATR calculations.";
    summary.append(warning);
   }
   let parity = null;
   try {
    const parityResponse = await fetch("/api/strategy-report/atr-parity?symbol=" +
     encodeURIComponent(body.symbol) + "&timeframe=" + encodeURIComponent(body.timeframe) +
     "&studyVersion=" + encodeURIComponent("MCC_OHLCV_ATR_V2.1") + "&windowBars=119", {headers:{Accept:"application/json"}});
    const parityBody = await parityResponse.json();
    if (parityResponse.ok) parity = parityBody;
    else parity = {status:"UNAVAILABLE",explanation:parityBody.error || ("HTTP " + parityResponse.status)};
   } catch (parityError) {
    parity = {status:"UNAVAILABLE",explanation:parityError.message};
   }
   const parityLine = document.createElement("p");
   parityLine.textContent = "Server Wilder ATR parity: " + parity.status +
    (parity.comparedBarCount != null ? " · " + parity.comparedBarCount + " matched timestamps · mean abs error " +
     (parity.meanAbsoluteError == null ? "DATA?" : Number(parity.meanAbsoluteError).toFixed(6)) +
     " · max abs error " + (parity.maximumAbsoluteError == null ? "DATA?" : Number(parity.maximumAbsoluteError).toFixed(6)) : "") +
    (parity.explanation ? " · " + parity.explanation : "");
   if (parity.status !== "MATCH") parityLine.className = "upload-warning";
   summary.append(parityLine);
   const provenance = document.createElement("pre");
   provenance.textContent = JSON.stringify(body.metadata, null, 2);
   summary.append(provenance);
   status.textContent = "ATR V2 import saved. " + body.atrObservationCount + " observations parsed; " +
    body.persistence.appendedCount + " new records, " + body.persistence.identicalSkippedCount + " unchanged, " +
    body.persistence.correctionCount + " corrections. ATR parity: " + parity.status +
    ". A numeric sell limit remains withheld unless all independent gates pass.";
  } else {
   const parsed = body.parsed;
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
   status.textContent = "Parse complete. " + (parsed.parityRows || []).length + " MCC OHLCV V1 row(s) converted to the Chart Parity Template.";
   if ((parsed.parityRows || []).length > 0) parityDownload?.classList.remove("hidden");
  }
  summary.classList.remove("hidden"); download.classList.remove("hidden");
 } catch (e) {
  status.textContent = "StrategyReport import failed: " + e.message;
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
 if(!bars.length) return {count:0,latestAge:null,change:null,range:null,volume:null,first:null,last:null,feed:payload.feed||"unknown",provider:payload.provider||"Alpaca"};
 const first=bars[0],last=bars[bars.length-1];
 const enoughBars=bars.length>=2;
 const highs=bars.filter(b=>b.high!==null&&b.high!==undefined).map(b=>Number(b.high)).filter(Number.isFinite),lows=bars.filter(b=>b.low!==null&&b.low!==undefined).map(b=>Number(b.low)).filter(Number.isFinite);
 const max=highs.length?Math.max(...highs):null,min=lows.length?Math.min(...lows):null;
 const volume=bars.reduce((sum,b)=>sum+(Number(b.volume)||0),0);
 return {
  count:bars.length,latestAge:Math.max(0,(Date.now()-new Date(last.timestamp).getTime())/60000),
  change:enoughBars&&Number.isFinite(Number(first.close))&&Number.isFinite(Number(last.close))&&Number(first.close)!==0?(Number(last.close)/Number(first.close)-1)*100:null,
  range:enoughBars&&min!==null&&max!==null&&Number(first.close)!==0?(max-min)/Number(first.close)*100:null,
  volume,first:first.timestamp,last:last.timestamp,feed:payload.feed||"unknown",provider:payload.provider||"Alpaca"
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
     const summary=s=>s.count?(s.count+" bars from "+(s.provider||"provider")+"/"+String(s.feed||"unknown").toUpperCase()+"; "+(s.change===null?"change unavailable (need ≥2 valid bars)":s.change.toFixed(2)+"% change")+"; "+(s.range===null?"range unavailable (need ≥2 valid bars)":s.range.toFixed(2)+"% range")+"; vol "+Math.round(s.volume)+"; latest "+formatArizonaTimestamp(s.last)):(s.error||("No bars returned from "+(s.provider||"provider")+"/"+String(s.feed||"unknown").toUpperCase()+" in requested window; this is not proof of no market activity"));
     tr.append(cell(item.symbol),cell(item.five.change===null||item.five.change===undefined?"— (insufficient bars)":(item.five.change>=0?"+":"")+item.five.change.toFixed(2)+"% (5M)"),cell(status),cell("5M "+(item.five.range==null?"DATA? (need ≥2 bars)":item.five.range.toFixed(2)+"%")+" · 1M "+(item.one.range==null?"DATA? (need ≥2 bars)":item.one.range.toFixed(2)+"%")),cell("5M "+(item.five.latestAge==null?"UNKNOWN":item.five.latestAge.toFixed(1)+"m")+" · 1M "+(item.one.latestAge==null?"UNKNOWN":item.one.latestAge.toFixed(1)+"m")),cell("5M: "+summary(item.five)+" | 1M: "+summary(item.one)+(sparse.length?" | COVERAGE WARNING: "+sparse.join("; "):"")+(errors.length?" | Errors: "+errors.join("; "):"")),document.createElement("td"));
    const watch=document.createElement("button");watch.type="button";watch.className="row-action";watch.textContent="Watch";watch.addEventListener("click",()=>addWatch(item.symbol));tr.lastElementChild.append(watch);return tr;
   }));
   const errors=results.reduce((n,r)=>n+Number(Boolean(r.one.error))+Number(Boolean(r.five.error)),0);
   const feeds=[...new Set(results.flatMap(x=>[x.one.feed,x.five.feed]).filter(Boolean))];
    source.textContent="Scan complete · "+results.length+" symbols · 1M + 5M · Alpaca/"+(feeds.join(",")||"feed unknown").toUpperCase()+" · "+errors+" timeframe request errors · Arizona "+fromInput.value+" → "+toInput.value+" ("+fromUtc+" → "+toUtc+" UTC). IEX is partial exchange coverage; zero bars means no observations were returned on that feed/window, not that the symbol had no trading. OHLCV summaries only; qualification gates are not connected.";
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
  const evaluations = Array.isArray(data.evaluations) ? data.evaluations : [];
  const evaluationBySnapshotId = new Map(evaluations.map(item => [item.snapshotId, item.evaluation]));
  rows.replaceChildren(...(snapshots.length ? snapshots.map(snapshot => {
   const evaluation = evaluationBySnapshotId.get(snapshot.snapshotId || snapshot.SnapshotId) || {};
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
    evaluation.status ? ("EVALUATION · "+evaluation.status+" · eligible="+String(evaluation.eligible===true)) : "EVALUATION NOT RETURNED",
    historyLabel,
    dailyLabel,
    gateInputs
   ];
   for (const value of values) { const td=document.createElement("td"); td.textContent=String(value); tr.append(td); }
   return tr;
  }) : [Object.assign(document.createElement("tr"),{innerHTML:"<td colspan='8'>No saved evaluations found for this query.</td>"})]));
  status.textContent = "Loaded "+snapshots.length+" saved input snapshot(s) and recalculated "+evaluations.length+" current-rule evaluation(s). These are eligibility assessments, not orders. "+(data.note||"");
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
 button.disabled=true; const captureStartedAt=Date.now(); status.textContent="Fetching Alpaca 1-minute bars and calculating 20 completed historical close/open pairs plus the separate current Overnight interval for "+symbol+"…";
 const captureProgressTimer=setInterval(()=>{const elapsed=Math.floor((Date.now()-captureStartedAt)/1000),minutes=Math.floor(elapsed/60),seconds=elapsed%60;status.textContent="Still working · Alpaca 1-minute history capture for "+symbol+" · "+minutes+"m "+String(seconds).padStart(2,"0")+"s elapsed · waiting for provider fetch, persistence and exact-boundary calculation.";},10000);
 try{
  const data=await overnightFetchJson("/api/boring-overnight/capture-history",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({symbol,providerName:"Alpaca",requiredWindow:20})});
  const h=data.history||{};
  const calc=data.calculation||h.calculation||{};
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
  const current=data.currentInterval||{};
  const currentState=current.state||"UNKNOWN";
  const currentPct=current.returnPct;
  const currentLabel=[
   currentState,
   (current.previousSessionDate||"DATA?")+" close → "+(current.nextSessionDate||"DATA?")+" open",
   "expected open "+(current.nextOpenBarTimestampUtc||"DATA?"),
   "previous close="+(current.previousClose??"DATA?"),
   "next open="+(current.nextOpen??"DATA?"),
   "return="+(currentPct==null?"DATA?":Number(currentPct).toFixed(3)+"%"),
   "reference="+(current.referenceKind||"UNKNOWN")+"; official auction verified="+String(current.officialOpeningAuctionVerified===true)
  ].join(" · ");
  const currentEl=$("overnightCurrentInterval");
  if(currentEl) currentEl.textContent=currentLabel;
  const returns=Array.isArray(h.returns)?h.returns:[];
  for(const item of returns){
   const tr=document.createElement("tr");
   const bw=$("tosDailyBoringWinnerStatus");
   if(bw){
    const state=calc.boringWinnerPass===true?"PASS":calc.boringWinnerPass===false?"FAIL":"DATA? — INSUFFICIENT HISTORY";
    const count=calc.boringWinnerInRangeCount==null?"DATA?":calc.boringWinnerInRangeCount+"/20 in +0.10%…+3.00%";
    const sd=calc.boringWinnerSampleStdDevPct==null?"DATA?":Number(calc.boringWinnerSampleStdDevPct).toFixed(3)+"% sample SD";
    bw.textContent="Boring Winner · RESEARCH ONLY: "+state+" · "+count+" · "+sd+" · requires win ≥60%, mean +0.10%…+3.00%, ≥10 in-range gaps, sample SD ≤3.00%, and close > SMA20. Not a READY gate.";
   }
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
  status.textContent=(h.allPairsComplete&&h.enoughHistory?"20-PAIR HISTORICAL CAPTURE COMPLETE. ":"HISTORICAL WINDOW INCOMPLETE — rolling metrics withheld. ")+(h.provenanceNote||data.note||"")+" Exact boundary audit: provider "+providerExact+"/"+audit.length+", store "+storeExact+"/"+audit.length+". Calendar: "+(data.calendarVersion||"unknown")+". Input snapshot "+(data.snapshot?.snapshotId||"NOT RETURNED")+" saved; evaluator "+(data.evaluation?.status||"UNKNOWN")+". Current price, Daily liquidity, catalyst and chart gates remain UNKNOWN until supplied."; 
  await refreshOvernightSnapshots();
 }catch(error){
  for(const id of fields){const el=$(id);if(el)el.textContent="DATA?";}
  const tr=document.createElement("tr"),td=document.createElement("td");td.colSpan=5;td.textContent="Capture failed: "+error.message;tr.append(td);rows.append(tr);
  status.textContent="20-session capture failed. No calculated metrics are displayed. "+error.message;
 }finally{clearInterval(captureProgressTimer);button.disabled=false;}

});

/// MODULE: 40-session GTC_EXT sell-window history capture
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
  const delayedSip=alpaca.delayedHistoricalSipMinutes===true;
  if(effectiveFeed!=="sip"&&!delayedSip){
   status.textContent="NOT RUN — effective Alpaca feed is "+(effectiveFeed.toUpperCase()||"UNKNOWN")+" and delayed historical SIP is disabled. GTC_EXT qualification research requires SIP. No provider history request was made and no bars were persisted.";
   return;
  }
  status.textContent=(effectiveFeed==="sip"?"SIP confirmed as the effective configured feed.":"Using delayed historical SIP (completed windows only, request end at least 16 minutes old).")+" Fetching 40 exact GTC_EXT sell windows for "+symbol+"…";
  const response=await fetch("/api/boring-overnight/capture-sell-window-history",{method:"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},body:JSON.stringify({symbol,providerName:"Alpaca",feed:"sip"})});
  const data=await response.json();
  if(!response.ok)throw new Error(data.detail||data.title||data.error||("HTTP "+response.status));
  const windows=Array.isArray(data.windows)?data.windows:[];
  const missing=windows.reduce((sum,item)=>sum+(Number(item.missingIntervalCount)||0)+(item.referenceClose===null?1:0),0);
  const noTrade=windows.reduce((sum,item)=>sum+(Number(item.noTradeIntervalCount)||0),0);
  const complete=data.completeWindowCount??0,required=data.requiredWindow??40;
  const samples=windows.flatMap(item=>(item.missingIntervalSampleUtc||[]).map(ts=>item.sellSessionDate+": "+ts)).slice(0,5);
  const qualification=data.qualificationUsable===true&&data.allWindowsComplete===true;
  status.textContent="Capture finished for "+symbol+" · "+complete+"/"+required+" complete windows · "+missing+" missing minute/reference observations · "+noTrade+" NO_TRADE minutes (no bar on SIP; counted, never priced) · "+(data.fetchedBarCount??0)+" fetched / "+(data.newlyPersistedBarCount??0)+" newly stored · feed coverage "+(data.feedCoverage||"UNKNOWN")+" · qualification "+(qualification?"USABLE":"NOT USABLE")+". "+(samples.length?"Missing examples: "+samples.join(" | "):"No missing-minute samples were reported.")+" "+(data.note||"");
  const reason=$("overnightSellLimitReason");
  if(reason)reason.textContent=qualification
   ?"GTC_EXT SIP coverage is complete. The numeric target remains NOT CALCULATED until ATR parity is MATCH and walk-forward target-touch/adverse-excursion validation is complete. Coverage is not proof of executable quotes or fills."
   :"Sell target remains DATA? — NOT CALCULATED. Complete 40/40 exact SIP windows (20 training + 20 later test) and ATR parity are required; missing intervals are not imputed.";
 }catch(error){
  status.textContent="GTC_EXT capture failed or was blocked: "+error.message+" No sell target was calculated.";
 }finally{
  clearInterval(timer);
  button.disabled=false;
 }
});

async function runCalculatedSellLimitAssessment() {
 const button=$("overnightCalculateSellLimit");
 const quickButton=$("overnightQuickCalculateSellLimit");
 const status=$("overnightSellLimitAssessmentStatus");
 const gateList=$("overnightSellLimitEvidenceGates");
 const quickGateSummary=$("overnightQuickSellLimitGateSummary");
 const symbol=($("overnightSymbol")?.value||"").trim().toUpperCase();
 const quantile=Number($("overnightSellLimitQuantile")?.value);
 const showValidationError=message=>{
  if(status)status.textContent=message;
  if(quickGateSummary)quickGateSummary.textContent=message;
  if($("overnightQuickSellLimitDetail"))$("overnightQuickSellLimitDetail").textContent=message;
 };
 if(!/^[A-Z0-9.-]{1,8}$/.test(symbol)){showValidationError("Enter a valid Overnight candidate symbol first. No request was sent.");return;}
 if(!Number.isInteger(quantile)||quantile<1||quantile>99){showValidationError("Choose a research target quantile from 1 to 99. No request was sent.");return;}
 if(button)button.disabled=true;
 if(quickButton)quickButton.disabled=true;
 if(gateList)gateList.replaceChildren();
 if(quickGateSummary)quickGateSummary.replaceChildren();
 if(status)status.textContent="Reading effective provider/feed and evaluating persisted sell-limit evidence for "+symbol+"…";
 if(quickGateSummary)quickGateSummary.textContent="Assessment running for "+symbol+" at the "+quantile+"th percentile…";
 if($("overnightQuickSellLimitDetail"))$("overnightQuickSellLimitDetail").textContent="Assessment running · "+quantile+"th percentile";
 try{
  const providerResponse=await fetch("/api/providers/status",{cache:"no-store",headers:{Accept:"application/json"}});
  const providers=await providerResponse.json();
  if(!providerResponse.ok)throw new Error(providers.detail||providers.title||("Provider status HTTP "+providerResponse.status));
  const alpaca=Array.isArray(providers)?providers.find(x=>String(x.provider||"").toLowerCase()==="alpaca"):null;
  if(!alpaca)throw new Error("Alpaca provider status was not returned; no sell-limit assessment was submitted.");
  const configured=String(alpaca.configuredFeed||"").trim().toLowerCase();
  // Historical GTC_EXT evidence is captured on SIP when delayed historical SIP is enabled; evaluate that evidence.
  const feed=alpaca.delayedHistoricalSipMinutes===true?"sip":configured;
  if(!feed)throw new Error("Alpaca effective feed is unknown; no sell-limit assessment was submitted.");
  const response=await fetch("/api/boring-overnight/sell-limit",{method:"POST",headers:{"Content-Type":"application/json","Accept":"application/json"},
   body:JSON.stringify({symbol,providerName:"Alpaca",feed,targetQuantilePct:quantile})});
  const data=await response.json();
  if(!response.ok)throw new Error(data.detail||data.title||data.error||("HTTP "+response.status));
  const calculation=data.calculation;
  if($("overnightSellLimitPrice"))$("overnightSellLimitPrice").textContent=calculation?.calculatedLimitSell!=null?Number(calculation.calculatedLimitSell).toFixed(2):"DATA? — NOT CALCULATED";
  if($("overnightSellLimitClose"))$("overnightSellLimitClose").textContent=data.referenceClose!=null?Number(data.referenceClose).toFixed(2):"DATA?";
  if($("overnightSellLimitMethod"))$("overnightSellLimitMethod").textContent="Historical "+quantile+"th percentile · "+(data.atrParity?.status||"ATR DATA?");
  if($("overnightSellLimitValidation"))$("overnightSellLimitValidation").textContent=data.status+" · "+(data.atrParity?.status||"ATR DATA?");
  if($("overnightSellLimitReason"))$("overnightSellLimitReason").textContent=data.explanation||"No sell-limit explanation was returned.";
  if($("overnightQuickSellLimit"))$("overnightQuickSellLimit").textContent=calculation?.calculatedLimitSell!=null?"$"+Number(calculation.calculatedLimitSell).toFixed(2):"DATA? — NOT CALCULATED";
  if($("overnightQuickSellLimitDetail"))$("overnightQuickSellLimitDetail").textContent=data.status+" · "+quantile+"th percentile · no order submitted";
  const gates=Array.isArray(data.gates)?data.gates:[];
  if(quickGateSummary)quickGateSummary.replaceChildren();
  for(const gate of gates){
   const row=document.createElement("p");
   row.className=gate.passed?"muted":"upload-warning";
   row.textContent=(gate.passed?"PASS":"BLOCKED")+" · "+gate.code+" — "+gate.detail;
   if(gateList)gateList.append(row);
   if(quickGateSummary){const quickRow=document.createElement("p");quickRow.className=row.className;quickRow.textContent=row.textContent;quickGateSummary.append(quickRow);}
  }
  if(quickGateSummary&&!gates.length)quickGateSummary.textContent="The API returned no individual gate records. Check the assessment status below.";
  const timing=data.performanceDiagnostics;
  const timingSummary=timing
   ?" · "+(timing.totalElapsedMs/1000).toFixed(1)+"s total (history "+(timing.historyReadElapsedMs/1000).toFixed(1)+"s; ATR store "+(timing.atrStoreReadElapsedMs/1000)+"s; reference "+(timing.referenceCloseReadElapsedMs/1000)+"s; provider parity "+(timing.providerParityBarsReadElapsedMs/1000)+"s)"
   :"";
  const resultStatus=data.status+" · effective Alpaca feed "+feed.toUpperCase()+" · "+gates.filter(x=>x.passed).length+"/"+gates.length+" evidence gates passed"+timingSummary+" · no order submitted.";
  if(status)status.textContent=resultStatus;
 }catch(error){
  const message="Sell-limit assessment failed: "+error.message+" No numeric limit was calculated.";
  showValidationError(message);
  if($("overnightSellLimitPrice"))$("overnightSellLimitPrice").textContent="DATA? — NOT CALCULATED";
  if($("overnightQuickSellLimit"))$("overnightQuickSellLimit").textContent="DATA? — NOT CALCULATED";
 }finally{
  if(button)button.disabled=false;
  if(quickButton)quickButton.disabled=false;
 }
}

$("overnightCalculateSellLimit")?.addEventListener("click",runCalculatedSellLimitAssessment);
$("overnightQuickCalculateSellLimit")?.addEventListener("click",()=>{
 const chartSymbol=String($("symbol")?.value||"").trim().toUpperCase();
 if(chartSymbol&&$("overnightSymbol"))$("overnightSymbol").value=chartSymbol;
 const disclosure=$("overnightQuickCalculateSellLimit")?.closest("article")?.querySelector(".quick-sell-evidence");
 if(disclosure)disclosure.open=true;
 runCalculatedSellLimitAssessment();
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


/// MODULE: Boring Overnight READY signal (decision record docs/Engineering/Boring-Overnight-READY-Semantics-20261114.md)
/// PURPOSE: For each symbol: run the 20-session capture (fresh qualification snapshot), then POST /ready/evaluate and
/// render the signal, the limit, every gate, warnings, required actions and the catalyst sign-off form.
/// SAFETY: Decision support only. Sound plays only in effective LIVE mode, only on a new READY, and only after the
/// operator enabled it with a click. No order is submitted.
const readyState={soundEnabled:false,audio:null,lastStatus:{},autoTimer:null,running:false,operatorPrices:{}};
function readyEl(tag,cls,text){const node=document.createElement(tag);if(cls)node.className=cls;if(text!==undefined&&text!==null)node.textContent=String(text);return node;}
function readyAz(iso){if(!iso)return "DATA?";try{return new Date(iso).toLocaleString("en-US",{timeZone:"America/Phoenix",month:"short",day:"numeric",hour:"2-digit",minute:"2-digit"})+" AZ";}catch{return String(iso);}}
function readySymbols(){
 // 2026-11-17: no fallback to the focused symbol (it is SPY by default, so an empty box checked SPY). Empty box = active Top 4 group.
 const box=$("overnightReadySymbols");if(box&&!box.value.trim())syncReadySymbolsFromTopFour(true);
 const raw=(box?.value||"").trim();
 return [...new Set(raw.split(/[\s,;]+/).map(x=>x.trim().toUpperCase()).filter(x=>/^[A-Z0-9.-]{1,8}$/.test(x)))].slice(0,8);
}
/// <summary>The active Top 4 group's symbols (Boring Top 4, or My Top 4 when selected and complete). Called by the READY box sync.</summary>
function activeTopFourSymbols(){
 if(overnightShortlistMode==="manual"&&overnightManualSymbols.length===4)return overnightManualSymbols.slice();
 return getBoringOvernightCandidateRows(4).map(x=>String(x.symbol||"").trim().toUpperCase()).filter(Boolean);
}
var readyAutoFilledValue="";
/// <summary>Fills the READY symbol box from the active Top 4 group when it is empty or still holds MCC's own last fill (never overwrites typed symbols unless force). Called after import, group switch, the Use Top 4 button, and an empty Check READY.</summary>
function syncReadySymbolsFromTopFour(force=false){
 const box=$("overnightReadySymbols");if(!box)return;
 const current=box.value.trim();if(!force&&current&&current!==readyAutoFilledValue)return;
 const symbols=activeTopFourSymbols();if(!symbols.length)return;
 box.value=readyAutoFilledValue=symbols.join(", ");
}
$("overnightReadyUseTopFour")?.addEventListener("click",()=>syncReadySymbolsFromTopFour(true));
function playReadyAlert(){
 if(!readyState.soundEnabled)return;
 try{
  const AudioContextType=window.AudioContext||window.webkitAudioContext;if(!AudioContextType)return;
  readyState.audio=readyState.audio||new AudioContextType();if(readyState.audio.state==="suspended")void readyState.audio.resume();
  const now=readyState.audio.currentTime;
  [660,880,1320].forEach((frequency,index)=>{const osc=readyState.audio.createOscillator(),gain=readyState.audio.createGain(),start=now+index*.2;osc.type="sine";osc.frequency.value=frequency;gain.gain.setValueAtTime(.0001,start);gain.gain.exponentialRampToValueAtTime(.18,start+.02);gain.gain.exponentialRampToValueAtTime(.0001,start+.16);osc.connect(gain);gain.connect(readyState.audio.destination);osc.start(start);osc.stop(start+.18);});
 }catch(error){const b=$("overnightReadySound");if(b)b.title="Sound unavailable: "+error.message;}
}
$("overnightReadySound")?.addEventListener("click",async()=>{
 readyState.soundEnabled=!readyState.soundEnabled;
 const b=$("overnightReadySound");
 if(b){b.textContent=readyState.soundEnabled?"READY sound: ON (live mode only)":"Enable READY sound";b.setAttribute("aria-pressed",String(readyState.soundEnabled));}
 if(readyState.soundEnabled){const A=window.AudioContext||window.webkitAudioContext;if(A){readyState.audio=readyState.audio||new A();if(readyState.audio.state==="suspended")await readyState.audio.resume();}}
});
async function readyPost(url,body){
 const response=await fetch(url,{method:"POST",headers:{"Content-Type":"application/json",Accept:"application/json"},body:JSON.stringify(body)});
 let data=null;try{data=await response.json();}catch{data=null;}
 if(!response.ok)throw new Error((data&&(data.error||data.detail||data.title))||("HTTP "+response.status));
 return data;
}
/// <summary>True when the result is for a buy session other than the check's own Eastern date (after the late deadline,
/// a weekend, or a holiday). Mirrors the server's sign-off rule. Called by renderReadyCard.</summary>
function readyIsNextSession(result){
 if(!result?.buySessionDate||!result?.evaluatedAtUtc)return false;
 try{const eastern=new Intl.DateTimeFormat("en-CA",{timeZone:"America/New_York",year:"numeric",month:"2-digit",day:"2-digit"}).format(new Date(result.evaluatedAtUtc));return eastern!==String(result.buySessionDate);}catch{return false;}
}
function renderReadyCard(result,captureNote){
 // 2026-11-17: a late-window READY is observe-only, so it must not look like a green buy signal.
 const lateObserve=result.status==="READY"&&result.entryWindow==="LATE";
 const card=readyEl("article","ready-card "+(lateObserve?"ready-late-observe":"ready-"+String(result.status||"DATA").replace(/[^A-Z_]/gi,"")));
 card.dataset.symbol=result.symbol;
 const head=readyEl("div","ready-head");
 head.append(readyEl("strong","ready-symbol",result.symbol),readyEl("span","ready-label",result.signalLabel||result.status));
 card.append(head);
 const untested=result.limitBasis==="UNTESTED_HEURISTIC";
 const limit=result.calculatedLimitSell!=null
  ?"Limit sell (GTC_EXT): $"+Number(result.calculatedLimitSell).toFixed(2)+(untested?" · LIMIT UNTESTED — your +"+Number(result.targetReturnPct).toFixed(2)+"% heuristic":" · tested target +"+Number(result.targetReturnPct).toFixed(2)+"%")+" on entry price $"+Number(result.referencePrice).toFixed(2)+" (see SELL_LIMIT for its source) · re-check against your fill"
  :"Limit sell: DATA? — not issued";
 card.append(readyEl("p","ready-limit"+(untested?" ready-limit-untested":""),limit));
 const late=result.entryWindow==="LATE";
 card.append(readyEl("p","muted small","Buy session "+result.buySessionDate+" · final hour "+readyAz(result.buyWindowStartUtc)+" – "+readyAz(result.buyDeadlineUtc)+" · late window (after-hours, LMT EXT) until "+readyAz(result.lateBuyDeadlineUtc)+(late?" · NOW IN LATE WINDOW":"")+" · sell session "+result.sellSessionDate+" · checked "+readyAz(result.evaluatedAtUtc)+(captureNote?" · "+captureNote:"")));
 // 2026-11-17: after the late deadline (or on a non-session day) MCC evaluates the NEXT buy session. The server only
 // accepts a sign-off on the buy-session day itself, so say so up front and hide the form instead of failing on click.
 const nextSession=readyIsNextSession(result);
 if(nextSession){
  const weekday=(()=>{try{return new Date(result.buySessionDate+"T12:00:00Z").toLocaleDateString("en-US",{weekday:"long",timeZone:"UTC"});}catch{return "";}})();
  card.append(readyEl("p","ready-next-session","⏭ Today's buy windows are closed. This check is for the next buy session, "+weekday+" "+result.buySessionDate+": final hour "+readyAz(result.buyWindowStartUtc)+" – "+readyAz(result.buyDeadlineUtc)+", late window until "+readyAz(result.lateBuyDeadlineUtc)+". Catalyst sign-off opens that day. Come back after that day's Overnight scan and click Check READY now again."));
 }
 const gates=readyEl("ul","ready-gates");
 (result.gates||[]).forEach(g=>{const li=readyEl("li","ready-gate gate-"+String(g.state).replace(/[^A-Z_]/gi,""));li.append(readyEl("span","gate-state",g.state),readyEl("span","gate-code",g.code),readyEl("span","gate-detail",g.detail));gates.append(li);});
 card.append(gates);
 if((result.warnings||[]).length){const w=readyEl("ul","ready-warnings");result.warnings.forEach(x=>w.append(readyEl("li",null,"⚠ "+x)));card.append(w);}
 if((result.qualificationNotes||[]).length){const n=readyEl("ul","ready-notes muted small");result.qualificationNotes.forEach(x=>n.append(readyEl("li",null,"ℹ "+x)));card.append(n);}
 // Late window: the free IEX feed has no after-hours trades, so the operator types the after-hours price from TOS/Schwab.
 if(result.entryWindow==="LATE"){
  const pf=readyEl("form","ready-operator-price");
  pf.append(readyEl("strong",null,"Your after-hours price (from TOS/Schwab): "));
  const input=document.createElement("input");input.type="number";input.step="0.01";input.min="0.01";input.placeholder="e.g. 122.85";input.value=readyState.operatorPrices[result.symbol]??"";input.className="ready-price";
  const go=readyEl("button",null,"Re-check with this price");go.type="submit";
  const pm=readyEl("span","muted small"," The free data feed has no after-hours trades. Re-checks reuse this price until you change it.");
  pf.append(input,go,pm);
  pf.addEventListener("submit",async event=>{
   event.preventDefault();
   const price=Number(input.value);
   if(!Number.isFinite(price)||price<=0){pm.textContent=" Enter a positive price.";return;}
   readyState.operatorPrices[result.symbol]=price;go.disabled=true;pm.textContent=" Re-checking…";
   try{const updated=await readyPost("/api/boring-overnight/ready/evaluate",{symbol:result.symbol,operatorPrice:price});handleReadyResult(updated,"operator price "+price.toFixed(2));}
   catch(error){pm.textContent=" Not checked: "+error.message;go.disabled=false;}
  });
  card.append(pf);
 }
 const actions=(result.requiredActions||[]).filter(x=>!(nextSession&&/^sign off/i.test(String(x))));
 if(actions.length){const a=readyEl("ul","ready-actions");actions.forEach(x=>a.append(readyEl("li",null,"→ "+x)));card.append(a);}
 // Inline catalyst review (2026-11-16): label each unreviewed article here instead of the 2.6 queue,
 // which only lists articles for the active top-four group. Labels are append-only research data.
 (result.articlesToReview||[]).forEach(article=>{
  const f=readyEl("form","ready-review");
  const head=readyEl("div","ready-review-head");
  const link=document.createElement("a");link.textContent=article.headline;link.target="_blank";link.rel="noopener noreferrer";if(article.url)link.href=article.url;
  head.append(readyEl("strong",null,"Label this article: "),link,readyEl("span","muted small"," · "+(article.source||"source unknown")+" · "+(article.publishedAtUtc?readyAz(article.publishedAtUtc):"time unknown")+" · triage "+(article.severity||"?")+(article.currentLabel?" · current label "+article.currentLabel:"")));
  const select=document.createElement("select");
  [["","Choose a label…"],["NON_MATERIAL","NON_MATERIAL — routine, unlikely to matter overnight"],["IRRELEVANT","IRRELEVANT — not about this company"],["DUPLICATE","DUPLICATE — repeat of a reviewed article"],["MATERIAL_EVENT","MATERIAL_EVENT — significant but not adverse"],["MATERIAL_RISK","MATERIAL_RISK — specifically adverse"],["UNCERTAIN","UNCERTAIN — cannot tell from the source"]].forEach(([v,t])=>{const o=document.createElement("option");o.value=v;o.textContent=t;select.append(o);});
  const notes=document.createElement("textarea");notes.rows=2;notes.maxLength=2000;notes.placeholder="What the article says and why that supports the label (at least 40 characters). Open and read the source first.";notes.className="ready-review-notes";
  const save=readyEl("button",null,"Save label");save.type="submit";
  const msg=readyEl("span","muted small","");
  f.append(head,select,notes,save,msg);
  f.addEventListener("submit",async event=>{
   event.preventDefault();
   if(!select.value){msg.textContent=" Choose a label.";return;}
   if(notes.value.trim().length<40){msg.textContent=" Notes need at least 40 characters of article facts.";return;}
   if(!article.url){msg.textContent=" This article has no URL, so it cannot be reviewed here.";return;}
   save.disabled=true;msg.textContent=" Saving…";
   try{
    await readyPost("/api/market-data/catalyst-reviews",{symbol:result.symbol,articleId:article.id,headline:article.headline,url:article.url,publishedAtUtc:article.publishedAtUtc,source:article.source,label:select.value,ruleSetVersion:article.ruleSetVersion,notes:notes.value.trim(),predictedSeverity:["RISK","MATERIAL","UNCLASSIFIED"].includes(String(article.severity||"").toUpperCase())?String(article.severity).toUpperCase():null,predictedCategories:article.categories&&article.categories.length?article.categories:null});
    msg.textContent=" Saved. Re-checking…";
    const updated=await readyPost("/api/boring-overnight/ready/evaluate",{symbol:result.symbol,operatorPrice:readyState.operatorPrices[result.symbol]??null});
    handleReadyResult(updated,"label saved");
   }catch(error){msg.textContent=" Not saved: "+error.message;save.disabled=false;}
  });
  card.append(f);
 });
 if(!nextSession&&result.catalystState!=="OPERATOR_ATTESTED"&&result.catalystState!=="BLOCKED"){
  const form=readyEl("form","ready-signoff");
  form.append(readyEl("strong",null,"Catalyst sign-off for "+result.buySessionDate+" — tick what you checked:"));
  [["SCHWAB_NEWS","Schwab news"],["SEC_EDGAR","SEC EDGAR filings"],["COMPANY_IR","Company investor relations"],["OTHER","Other (name it in the note)"]].forEach(([value,label])=>{
   const l=readyEl("label","inline-check");const box=document.createElement("input");box.type="checkbox";box.value=value;l.append(box,document.createTextNode(" "+label));form.append(l);});
  const note=document.createElement("input");note.placeholder="Note (optional; required for Other), e.g. good news you saw";note.maxLength=300;note.className="ready-note";
  const submit=readyEl("button",null,"Sign off: nothing blocking");submit.type="submit";
  const msg=readyEl("span","muted small","");
  form.append(note,submit,msg);
  form.addEventListener("submit",async event=>{
   event.preventDefault();
   const sources=[...form.querySelectorAll("input[type=checkbox]:checked")].map(x=>x.value);
   submit.disabled=true;msg.textContent="Saving sign-off…";
   try{
    await readyPost("/api/boring-overnight/catalyst-attestations",{symbol:result.symbol,sourcesChecked:sources,note:note.value});
    msg.textContent="Saved. Re-checking…";
    const updated=await readyPost("/api/boring-overnight/ready/evaluate",{symbol:result.symbol,operatorPrice:readyState.operatorPrices[result.symbol]??null});
    handleReadyResult(updated,"sign-off saved");
   }catch(error){msg.textContent="Not saved: "+error.message;submit.disabled=false;}
  });
  card.append(form);
 }
 card.append(readyEl("p","muted small",result.modeNote||""));
 return card;
}
function handleReadyResult(result,captureNote){
 const host=$("overnightReadyCards");if(!host)return;
 const card=renderReadyCard(result,captureNote);
 const existing=[...host.children].find(x=>x.dataset.symbol===result.symbol);
 if(existing)existing.replaceWith(card);else host.append(card);
 // 2026-11-17: anything typed into a card (label notes, sign-off note, after-hours price) marks it dirty, so the
 // automatic re-check skips that card instead of re-rendering it and wiping the text. A save re-renders it clean.
 card.addEventListener("input",()=>{card.dataset.dirty="1";});
 const mode=$("overnightReadyMode");
 if(mode)mode.textContent="Mode: "+String(result.effectiveMode||"shadow").toUpperCase()+(result.requestedMode==="live"&&result.effectiveMode!=="live"?" (live requested)":"")+" · "+result.loggedShadowSessions+"/10 shadow sessions logged";
 const previous=readyState.lastStatus[result.symbol];
 readyState.lastStatus[result.symbol]=result.status;
 if(result.status==="READY"&&previous!=="READY"&&result.effectiveMode==="live")playReadyAlert();
}
/// <summary>True when the operator is typing in this symbol's card (focus inside it, or unsaved text). Called by the automatic re-check.</summary>
function readyCardBusy(symbol){
 const card=[...($("overnightReadyCards")?.children||[])].find(x=>x.dataset.symbol===symbol);
 return !!card&&(card.dataset.dirty==="1"||card.contains(document.activeElement));
}
async function runReadyCheck(auto=false){
 if(readyState.running)return;
 const symbols=readySymbols();const status=$("overnightReadyStatus");
 if(!symbols.length){if(status)status.textContent="Enter at least one valid symbol.";return;}
 readyState.running=true;const button=$("overnightReadyEvaluate");if(button)button.disabled=true;
 let ready=0;
 try{
  const skipped=[];
  for(const symbol of symbols){
   if(auto&&readyCardBusy(symbol)){skipped.push(symbol);continue;}
   if(status)status.textContent="Capturing 20-session history and live inputs for "+symbol+"…";
   let captureNote="capture OK";
   try{await readyPost("/api/boring-overnight/capture-history",{symbol,providerName:"Alpaca",requiredWindow:20});}
   catch(error){captureNote="capture failed: "+error.message;}
   if(status)status.textContent="Checking READY for "+symbol+"…";
   try{const result=await readyPost("/api/boring-overnight/ready/evaluate",{symbol,operatorPrice:readyState.operatorPrices[symbol]??null});if(result.status==="READY")ready++;handleReadyResult(result,captureNote);}
   catch(error){const host=$("overnightReadyCards");if(host){const c=readyEl("article","ready-card ready-DATA");c.dataset.symbol=symbol;c.append(readyEl("strong",null,symbol+" — check failed: "+error.message));const old=[...host.children].find(x=>x.dataset.symbol===symbol);if(old)old.replaceWith(c);else host.append(c);}}
  }
  if(status)status.textContent=(auto?"Auto re-check: ":"")+"Checked "+(symbols.length-skipped.length)+" symbol(s) at "+readyAz(new Date().toISOString())+": "+ready+" READY. No order was sent."+(skipped.length?" Skipped while you type: "+skipped.join(", ")+" (save or click Check READY now).":"");
 }finally{readyState.running=false;if(button)button.disabled=false;}
}
$("overnightReadyEvaluate")?.addEventListener("click",()=>void runReadyCheck());
// 2026-11-17 (operator question): the automatic re-check is ON by default and the choice is remembered per browser.
// It runs only while a buy window is open and skips any card you are typing in.
const READY_AUTO_KEY="mcc.readyAutoRecheck.v1";
$("overnightReadyAuto")?.addEventListener("change",event=>{
 try{localStorage.setItem(READY_AUTO_KEY,String(event.target.checked));}catch{}
 if(readyState.autoTimer){clearInterval(readyState.autoTimer);readyState.autoTimer=null;}
 if(!event.target.checked)return;
 readyState.autoTimer=setInterval(async()=>{
  try{const r=await fetch("/api/market-session/clock",{cache:"no-store",headers:{Accept:"application/json"}});const clock=await r.json();if(clock&&(clock.overnightBuyWindowOpen===true||clock.overnightLateBuyWindowOpen===true))await runReadyCheck(true);}catch{}
 },120000);
});
{const box=$("overnightReadyAuto");if(box){let on=true;try{const v=localStorage.getItem(READY_AUTO_KEY);if(v!==null)on=v==="true";}catch{}box.checked=on;if(on)box.dispatchEvent(new Event("change"));}}
$("overnightReadyShadowRefresh")?.addEventListener("click",async()=>{
 const body=$("overnightReadyShadowBody");if(!body)return;
 body.textContent="Loading shadow log (may fetch Day 2 SIP history)…";
 try{
  const response=await fetch("/api/boring-overnight/ready/shadow-log",{cache:"no-store",headers:{Accept:"application/json"}});
  const data=await response.json();if(!response.ok)throw new Error(data.detail||data.title||("HTTP "+response.status));
  body.replaceChildren();
  body.append(readyEl("p",null,"Mode requested: "+data.requestedMode+" · logged sessions "+data.loggedSessions+"/"+data.minimumSessionsForLive+" · signalled "+data.signalledSessions+" · resolved "+data.resolvedSignals+" · limit touched on Day 2: "+data.touchedSignals+(data.resolvedSignals?" ("+Math.round(100*data.touchedSignals/data.resolvedSignals)+"%)":"")));
  const table=readyEl("table","ready-shadow-table");const headRow=readyEl("tr");
  ["Buy day","Symbol","Checks","Signalled","Limit","Last status","Day 2 outcome","Day 2 high"].forEach(h=>headRow.append(readyEl("th",null,h)));
  const thead=readyEl("thead");thead.append(headRow);table.append(thead);const tbody=readyEl("tbody");
  (data.rows||[]).forEach(r=>{const tr=readyEl("tr");[r.buySessionDate,r.symbol,r.evaluations,r.signalled?"READY "+readyAz(r.firstReadyAtUtc)+(r.windowAtFirstReady==="LATE"?" · LATE (observe only)":""):"no",r.limitAtFirstReady!=null?Number(r.limitAtFirstReady).toFixed(2):"—",r.lastStatus,r.outcome,r.day2High!=null?Number(r.day2High).toFixed(2):"—"].forEach(v=>tr.append(readyEl("td",null,v)));tbody.append(tr);});
  table.append(tbody);body.append(table);
  if((data.errors||[]).length)body.append(readyEl("p","notice","Errors: "+data.errors.join(" | ")));
  body.append(readyEl("p","muted small",data.note));
 }catch(error){body.textContent="Shadow log failed: "+error.message;}
});

/// MODULE: MCC automatic scan (2026-11-16, Intraday step 2.a)
/// PURPOSE: Shows the server-side automatic scan (operator TOS scans 1, 2A, 2B, Momentum Pivot, D on free Alpaca data),
/// optionally feeds the Red Zone, and records side-by-side comparisons with TOS watchlist imports. Discovery only.
const AUTO_SCAN_FEED_KEY="mcc.autoScanFeedsRedZone.v1";
let latestAutoScan=null;
/// <summary>Formats an instant as Arizona time. Called by the automatic-scan renderers.</summary>
function autoScanAz(value){const d=new Date(value);return Number.isNaN(d.getTime())?"time unknown":d.toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit",second:"2-digit"})+" AZ";}
/// <summary>True when the operator chose to feed the Red Zone from the automatic scan (browser preference). Called on load and after each scan.</summary>
function autoScanFeedsRedZone(){try{return localStorage.getItem(AUTO_SCAN_FEED_KEY)==="true";}catch{return false;}}
/// <summary>Converts flagged scan candidates into Red Zone rows (score = scans passed). Called after each scan when feeding is on.</summary>
function autoScanRows(run){
 return (run?.candidates||[]).filter(c=>c.flagged).map(c=>{
  const passed=(c.scanners||[]).filter(s=>s.result==="PASS").map(s=>s.code);
  return {symbol:c.universe.symbol,score:c.momentumPassCount+(passed.includes("D")?0.5:0),status:"MCC AUTO · "+(passed.join(" ")||"—"),
   extension:"DATA?",freshness:"5m bar "+(c.latestBarAgeSeconds==null?"age unknown":Math.round(c.latestBarAgeSeconds)+"s after close"),
   reason:"MCC automatic scan passed: "+(passed.join(", ")||"none")+". Tags: "+(c.tags||[]).join(", ")+". Discovery only; not entry permission."};
 });
}
/// <summary>Points the Red Zone at the automatic scan or back. Called by the Feed Red Zone button and after each scan.</summary>
function applyAutoScanFeed(){
 const button=$("autoScanFeedRedZone"),on=autoScanFeedsRedZone(),group=$("scannerGroup");
 scannerData.mccAuto=autoScanRows(latestAutoScan);
 if(group){
  let option=group.querySelector('option[value="mccAuto"]');
  if(!option){option=document.createElement("option");option.value="mccAuto";option.textContent="MCC automatic scan";group.append(option);}
  if(on&&group.value!=="mccAuto"){group.value="mccAuto";suppressNextRedZoneAlert=true;}
  if(group.value==="mccAuto"){renderScanner();renderMorningReport();}
 }
 syncAutoScanFeedButton();
}
/// <summary>Shows ON when the Red Zone currently reads the automatic scan. Called after any Red Zone source change, including a TOS import.</summary>
function syncAutoScanFeedButton(){
 const button=$("autoScanFeedRedZone"),active=$("scannerGroup")?.value==="mccAuto";
 if(button){button.textContent=active?"Feed Red Zone: ON":"Feed Red Zone: OFF";button.setAttribute("aria-pressed",String(active));button.title=active?"The Red Zone shows the top 4 flagged by the automatic scan (most scans passed first).":"The Red Zone uses your TOS import or another source.";}
}
/// <summary>Colour class for the automatic-scan status: ok (COMPLETED and under 5 minutes old in session), warn (source errors or stale), bad (failed / not configured / no universe). Called by renderAutoScan.</summary>
function autoScanStatusClass(status,ageMinutes,regularSession){
 if(status==="COMPLETED")return regularSession!==false&&ageMinutes>5?"warn":"ok";
 if(status==="COMPLETED_WITH_SOURCE_ERRORS")return "warn";
 return "bad";
}
/// <summary>Renders the scan status, the flagged table and the source list. Called after each load.</summary>
function renderAutoScan(run){
 const status=$("autoScanStatus"),table=$("autoScanTable"),sources=$("autoScanSourceList");
 if(!status||!table)return;
 if(!run){status.textContent="Automatic scan: no run yet. It starts during the regular session (07:30–14:00 AZ, standard time), or click Scan now.";table.replaceChildren();return;}
 const errors=(run.sources||[]).filter(s=>!s.ok);
 // 2026-11-17: the status word is a colour chip (green COMPLETED, amber with source errors or stale, red failed).
 const scanAgeMin=run.completedAtUtc?(Date.now()-new Date(run.completedAtUtc).getTime())/60000:Infinity;
 const statusChip=document.createElement("span");statusChip.className="scan-status-chip "+autoScanStatusClass(run.status,scanAgeMin,run.regularSession);
 statusChip.textContent=run.status+(run.regularSession!==false&&scanAgeMin>5&&run.status==="COMPLETED"?" · STALE "+Math.round(scanAgeMin)+" min":"");
 status.replaceChildren("Last scan "+autoScanAz(run.completedAtUtc)+" ",statusChip,(run.statusDetail?" ("+run.statusDetail+")":"")+" · universe "+run.universeCount+" · bars evaluated "+run.evaluatedCount+" · flagged "+run.flaggedCount+" · prior close: "+run.priorCloseSource+(errors.length?" · "+errors.length+" source error(s): see Sources":""));
 const flagged=(run.candidates||[]).filter(c=>c.flagged).slice(0,40);
 if(!flagged.length){table.replaceChildren(Object.assign(document.createElement("p"),{className:"muted small",textContent:"No symbol passes a scan right now (fresh bars, not excluded)."}));}
 else{
  const t=document.createElement("table");t.className="auto-scan-table";
  t.innerHTML="<thead><tr><th>#</th><th>Symbol</th><th>Last</th><th>% chg</th><th>Day vol</th><th>1</th><th>2A</th><th>2B</th><th>PIV</th><th>D</th><th>Tags</th></tr></thead>";
  const body=document.createElement("tbody");
  flagged.forEach((c,i)=>{
   const tr=document.createElement("tr"),u=c.universe;
   const cell=(text,cls)=>{const td=document.createElement("td");if(cls)td.className=cls;td.textContent=text;tr.append(td);return td;};
   cell(String(i+1));const sym=cell(u.symbol);sym.style.fontWeight="800";
   cell(u.lastPrice==null?"DATA?":"$"+Number(u.lastPrice).toFixed(2),"num");
   cell(u.percentChange==null?"DATA?":(Number(u.percentChange)>=0?"+":"")+Number(u.percentChange).toFixed(2)+"%","num");
   cell(u.consolidatedDayVolume==null?"DATA?":Number(u.consolidatedDayVolume).toLocaleString("en-US"),"num");
   for(const code of ["1","2A","2B","PIV","D"]){
    const s=(c.scanners||[]).find(x=>x.code===code),td=document.createElement("td"),chip=document.createElement("span");
    const r=s?.result||"UNKNOWN";chip.className="scan-chip "+r.toLowerCase();chip.textContent=r==="PASS"?"✓":r==="FAIL"?"·":"?";
    chip.title=(s?.name||code)+": "+r+"\n"+(s?.conditions||[]).map(x=>(x.passed===true?"✓ ":x.passed===false?"✗ ":"? ")+x.label+(x.usesIexVolume?" (IEX volume, approximate)":"")).join("\n");
    td.append(chip);tr.append(td);
   }
   const tags=document.createElement("td");
   for(const tag of (c.tags||[]).filter(x=>!["1","2A","2B","PIV","D"].includes(x))){const span=document.createElement("span");span.className="auto-scan-tag"+(/APPROX|GAPS/.test(tag)?" warn":"");span.textContent=tag;tags.append(span);}
   tr.append(tags);body.append(tr);
  });
  t.append(body);table.replaceChildren(t);
 }
 if(sources)sources.textContent=(run.sources||[]).map(s=>s.source+" ["+s.feed+"]: "+(s.ok?"OK, "+s.items+" items":"FAILED "+(s.httpStatus??"")+" "+(s.error||""))).join(" · ")+" — "+(run.notes||"");
}
/// <summary>Loads the latest automatic scan (refresh=true asks the server to run one if the last is over 60 s old). Called on load, by Scan now and every 60 s.</summary>
async function loadAutoScan(refresh=false){
 if($("mode")?.value==="overnight")return;
 const status=$("autoScanStatus");
 if(refresh&&status)status.textContent="Scanning… (screeners, IEX prices and 5m bars; this can take 10–30 seconds)";
 try{
  const response=await fetch("/api/scanner/auto-momentum"+(refresh?"?refresh=true":""),{cache:"no-store",headers:{Accept:"application/json"}});
  const body=await response.json().catch(()=>({}));
  if(!response.ok)throw new Error(body.detail||body.error||("HTTP "+response.status));
  latestAutoScan=body;renderAutoScan(body);applyAutoScanFeed();
 }catch(error){if(status)status.replaceChildren(Object.assign(document.createElement("span"),{className:"scan-status-chip bad",textContent:"UNAVAILABLE"})," "+error.message+". Is MCC running (MCC.ps1 status)?"+(latestAutoScan?.completedAtUtc?" The table below is from the last good scan at "+autoScanAz(latestAutoScan.completedAtUtc)+".":""));}
}
/// <summary>Shows the latest side-by-side result per session. Called on load and after a comparison is recorded.</summary>
async function loadAutoScanComparison(){
 const node=$("autoScanComparison");if(!node)return;
 try{
  const response=await fetch("/api/scanner/auto-momentum/tos-comparison",{cache:"no-store"});if(!response.ok)return;
  const body=await response.json(),sessions=body.sessions||[];
  if(!sessions.length)return;
  const c=sessions[0];
  node.textContent="Side-by-side with TOS: "+sessions.length+"/"+(body.targetSessions||5)+" sessions recorded. Latest ("+c.sessionDate+", "+autoScanAz(c.recordedAtUtc)+"): your TOS list "+c.tosSymbols.length+" · MCC also flagged "+c.flaggedByMcc.length+(c.flaggedByMcc.length?" ("+c.flaggedByMcc.join(", ")+")":"")+" · MCC saw but did not flag "+c.seenButNotFlagged.length+(c.seenButNotFlagged.length?" ("+c.seenButNotFlagged.join(", ")+")":"")+" · not in MCC's free universe "+c.notInMccUniverse.length+(c.notInMccUniverse.length?" ("+c.notInMccUniverse.join(", ")+")":"")+" · flagged only by MCC "+c.flaggedOnlyByMcc.length+". Based on "+c.scanRunsThatSession+" scan runs that session.";
 }catch{/* report unavailable: leave the instruction text */}
}
/// <summary>Records a TOS-vs-MCC comparison for an Intraday TOS import. Called by the CSV import handler.</summary>
async function recordAutoScanComparison(symbols,fileName){
 try{
  const response=await fetch("/api/scanner/auto-momentum/tos-comparison",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({symbols,fileName})});
  if(response.ok)await loadAutoScanComparison();
 }catch{/* comparison is optional; the import itself already succeeded */}
}
$("autoScanRefresh")?.addEventListener("click",()=>void loadAutoScan(true));
$("autoScanFeedRedZone")?.addEventListener("click",()=>{
 const next=$("scannerGroup")?.value!=="mccAuto";
 try{localStorage.setItem(AUTO_SCAN_FEED_KEY,String(next));}catch{/* preference lasts until reload */}
 if(!next&&$("scannerGroup")?.value==="mccAuto"){const group=$("scannerGroup");group.value=scannerData.imported?.length?"imported":"momentum";renderScanner();renderMorningReport();}
 applyAutoScanFeed();
});
$("scannerGroup")?.addEventListener("change",()=>applyAutoScanFeed());
void loadAutoScan(false);void loadAutoScanComparison();
window.setInterval(()=>{if(document.visibilityState==="visible")void loadAutoScan(false);},60000);

/// MODULE: Daily overnight-history export button (2026-11-16)
/// PURPOSE: One click runs the research export (same output as scripts/Export-OvernightHistory.ps1) for the symbols of the
/// current TOS scanner import. The header button is amber until an export (button or script) exists for today's Eastern
/// date, then green with the time; blue while running; red on failure. Research only; nothing is traded.
const historyState={running:false,status:null,error:null,needsImport:false};
/// <summary>Formats an instant as Arizona time. Called by the history button.</summary>
function historyAz(iso){try{return new Date(iso).toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit"})+" AZ";}catch{return "";}}
/// <summary>Sets the history button's colour, text and tooltip from the current state. Called after every status change.</summary>
function renderHistoryButton(){
 const b=$("historyExportButton");if(!b)return;
 b.classList.remove("history-done","history-pending","history-running","history-error");
 const rows=Array.isArray(scannerData.imported)?scannerData.imported.length:0;
 if(historyState.running){b.classList.add("history-running");b.textContent="History export · running…";b.title="Fetching 2 years of daily history for "+rows+" symbols (about a minute).";return;}
 if(historyState.needsImport){b.classList.add("history-pending");b.textContent="History export · import a scan first";b.title="Import a TOS scanner CSV first (Import TOS CSV bar), then click again.";return;}
 if(historyState.error){b.classList.add("history-error");b.textContent="History export · failed";b.title=historyState.error;return;}
 const s=historyState.status,latest=s?.latest;
 if(s?.ranToday&&latest){
  b.classList.add("history-done");b.textContent="History ✓ "+historyAz(latest.createdAtUtc);
  const all=(latest.pooled||[]).find(p=>p.buyWeekday==="All");
  b.title="Today's export is done ("+latest.source+"): "+latest.symbolsSucceeded+"/"+latest.symbolsRequested+" symbols, "+latest.pairRows+" overnight pairs"+(all?", all-nights win "+all.winRatePct+"%":"")+". Saved in "+latest.folder+". Click to run again.";
 }else{
  b.classList.add("history-pending");b.textContent="History export · not run today";
  b.title="Not run today yet. Best after 14:16 AZ (standard time) so today's session is included. Uses the imported TOS scanner CSV ("+rows+" symbols)."+(latest?" Last run: "+new Date(latest.createdAtUtc).toLocaleString("en-US",{timeZone:"America/Phoenix"})+" AZ.":"");
 }
}
/// <summary>Reads whether today's export has run. Called on load and every 60 seconds.</summary>
async function loadHistoryStatus(){
 try{
  const r=await fetch("/api/research/overnight-history/export-status",{cache:"no-store",headers:{Accept:"application/json"}});
  if(r.status===404){historyState.error="This MCC version has no history export. Stop MCC, run Get-Latest-And-Test.ps1, start MCC.";}
  else if(r.ok){historyState.status=await r.json();historyState.error=null;historyState.running=historyState.running||historyState.status.running===true;}
 }catch{/* MCC not reachable: keep the last state */}
 renderHistoryButton();
}
$("historyExportButton")?.addEventListener("click",async()=>{
 if(historyState.running)return;
 const imported=Array.isArray(scannerData.imported)?scannerData.imported:[];
 if(!imported.length){historyState.needsImport=true;renderHistoryButton();return;}
 historyState.needsImport=false;
 let fileName=null;try{fileName=JSON.parse(localStorage.getItem("mcc.scannerImport.v1")||"null")?.filename||null;}catch{fileName=null;}
 const rows=imported.map(row=>{const m=row.overnightMetrics||{};return {symbol:row.symbol,score:row.score==null?null:String(row.score),status:row.status??null,onWin:m.onWin??null,avgOn:m.avgOn??null,medianOn:m.medianOn??null,fiveDayOn:m.fiveDayOn??null,onMomentum:m.onMomentum??null,currentOn:m.currentOn??null,above20Sma:m.above20Sma??null};});
 historyState.running=true;historyState.error=null;renderHistoryButton();
 try{
  const r=await fetch("/api/research/overnight-history/export",{method:"POST",headers:{"Content-Type":"application/json",Accept:"application/json"},body:JSON.stringify({symbols:imported.map(x=>x.symbol),scannerFileName:fileName,scannerRows:rows,days:730})});
  const body=await r.json().catch(()=>({}));
  if(!r.ok)throw new Error(body.error||body.detail||("HTTP "+r.status));
  historyState.running=false;await loadHistoryStatus();
 }catch(error){historyState.running=false;historyState.error="Export failed: "+error.message;renderHistoryButton();}
});
void loadHistoryStatus();
window.setInterval(()=>{if(!historyState.running)void loadHistoryStatus();},60000);

/// MODULE: Header market chip (2026-11-16, MCC-REGIME-0.1)
/// PURPOSE: One glanceable colour for the market's mood (SPY and QQQ: 5m close vs VWAP and EMA9 vs EMA20) plus a badge for a
/// scheduled high-impact event today or before the next session (official FOMC/BLS dates). Awareness only: no MCC gate
/// reads it, and it is never a trade signal.
/// <summary>Formats a UTC instant as Arizona time. Called by the market chip.</summary>
function regimeAz(iso){try{return new Date(iso).toLocaleTimeString("en-US",{timeZone:"America/Phoenix",hour:"numeric",minute:"2-digit"})+" AZ";}catch{return "";}}
/// <summary>Short text for one event (label, day, Arizona time when the source gives one). Called by the market chip.</summary>
function regimeEventText(e,when){return e.label+(e.tentative?" (tentative)":"")+(when?" "+when:"")+(e.atUtc?" "+regimeAz(e.atUtc):"");}
/// <summary>Short badge text: kind (CPI / FOMC / Jobs report), day and Arizona time. Called by the market chip.</summary>
function regimeEventShort(e,when){return ({CPI:"CPI",FOMC:"FOMC",JOBS:"Jobs report"})[e.kind]||e.label;}
/// <summary>Loads /api/market/regime and paints the chip and the event badge. Called on load and every 60 seconds.</summary>
async function loadMarketRegime(){
 const chip=$("marketRegimeChip"),badge=$("marketEventBadge");if(!chip)return;
 const yn=v=>v===true?"yes":v===false?"no":"?";
 try{
  const r=await fetch("/api/market/regime",{cache:"no-store",headers:{Accept:"application/json"}});
  if(r.status===404){chip.textContent="Market · update MCC";chip.className="regime-chip regime-data";chip.title="This MCC version has no market chip. Stop MCC, run Get-Latest-And-Test.ps1, start MCC.";if(badge)badge.hidden=true;return;}
  if(!r.ok)throw new Error("HTTP "+r.status);
  const d=await r.json();
  const map={RISK_ON:["Market ▲ RISK-ON","regime-on"],MIXED:["Market ◆ MIXED","regime-mixed"],RISK_OFF:["Market ▼ RISK-OFF","regime-off"],CLOSED:["Market · closed","regime-data"],"DATA?":["Market · DATA?","regime-data"]};
  const [text,cls]=map[d.status]||["Market · "+d.status,"regime-data"];
  chip.textContent=text;chip.className="regime-chip "+cls;
  chip.title=(d.symbols||[]).map(s=>s.symbol+": "+s.state+" · above VWAP "+yn(s.closeAboveVwap)+" · EMA9>EMA20 "+yn(s.ema9AboveEma20)+(s.lastClose!=null?" · last "+Number(s.lastClose).toFixed(2):"")).join("\n")+
   "\nBoth bullish = RISK-ON, both bearish = RISK-OFF, otherwise MIXED ("+(d.rulesVersion||"rules")+"). Checked "+regimeAz(d.evaluatedAtUtc)+". Awareness only; not a trade signal."+
   ((d.upcomingEvents||[]).length?"\nNext 14 days: "+d.upcomingEvents.map(e=>regimeEventText(e,e.date)).join(" · "):"");
  if(badge){
   const flagged=d.eventsFlagged||[];
   if(d.eventRisk==="NONE"||!flagged.length){badge.hidden=true;}
   else{
    const when=d.eventRisk==="TODAY"?"today":"next session";
    badge.hidden=false;
    badge.textContent="⚑ "+regimeEventShort(flagged[0])+" "+when+(flagged[0].atUtc?" "+regimeAz(flagged[0].atUtc):"")+(flagged.length>1?" +"+(flagged.length-1):"");
    badge.className="event-badge "+(d.eventRisk==="TODAY"?"event-today":"event-next");
    badge.title=flagged.map(e=>regimeEventText(e,e.date)).join("\n")+(d.eventRisk==="NEXT_SESSION"?"\nAn overnight hold is exposed to this release.":"")+"\nSource: official FOMC/BLS schedules (App_Data/macro-events.json).";
   }
  }
 }catch(error){chip.textContent="Market · DATA?";chip.className="regime-chip regime-data";chip.title="Market chip unavailable: "+error.message;}
}
void loadMarketRegime();
window.setInterval(()=>{if(document.visibilityState==="visible")void loadMarketRegime();},60000);

/// MODULE: Qualification drop-downs (2026-11-16)
/// PURPOSE: The info and My Top 4 picker drop-downs in the compact qualification heading close on an outside click or Escape.
/// <summary>Closes every open .qual-pop drop-down except the one passed. Called on document clicks and Escape.</summary>
function closeQualificationPopovers(except){document.querySelectorAll("details.qual-pop[open]").forEach(d=>{if(d!==except)d.open=false;});}
document.addEventListener("click",event=>closeQualificationPopovers(event.target instanceof Element?event.target.closest("details.qual-pop"):null));
document.addEventListener("keydown",event=>{if(event.key==="Escape")closeQualificationPopovers(null);});

/// MODULE: Pending binary catalysts (2026-11-17, operator request after the SLS Phase 3 spike)
/// PURPOSE: Left-dock button + modal for the operator-maintained list of pending binary catalysts, and a ⚑ flag on any
/// Red Zone card whose symbol is listed. READY blocking happens server-side (gate PENDING_BINARY_CATALYST).
let pendingCatalysts=new Map();
const catalystsDialog=$("catalystsDialog");
/// <summary>Adds, updates, or removes the ⚑ line under the symbol on every Red Zone card. Called after each load and whenever the grid re-renders.</summary>
function applyPendingCatalystFlags(){
 document.querySelectorAll("#redZoneGrid .redzone-card").forEach(card=>{
  const heading=card.querySelector("h3");if(!heading)return;
  const entry=pendingCatalysts.get(heading.textContent.trim().toUpperCase());
  let flag=card.querySelector(".catalyst-flag");
  if(!entry){flag?.remove();return;}
  if(!flag){flag=document.createElement("div");flag.className="catalyst-flag";heading.after(flag);}
  const text="⚑ Binary catalyst pending: "+entry.event+" · expected "+entry.expectedWindow;
  if(flag.textContent!==text)flag.textContent=text;
  flag.title=(entry.notes?entry.notes+"\n":"")+(entry.sourceUrl?"Source: "+entry.sourceUrl+"\n":"")+"Boring Overnight READY is blocked for this symbol. Intraday: awareness only.";
 });
}
/// <summary>Loads the list, repaints the modal rows, the dock badge, and the card flags. Called on load, every 60 s, on open, and after edits.</summary>
async function refreshPendingCatalysts(){
 const status=$("catalystStatus"),list=$("catalystRows"),badge=$("catalystCountBadge");
 try{
  const response=await fetch("/api/watchlists/pending-catalysts",{cache:"no-store",headers:{Accept:"application/json"}});
  if(!response.ok)throw new Error("HTTP "+response.status);
  const data=await response.json(),entries=data.catalysts||[];
  pendingCatalysts=new Map(entries.map(e=>[String(e.symbol).toUpperCase(),e]));
  if(badge){badge.textContent=String(entries.length);badge.classList.toggle("has-items",entries.length>0);}
  if(list)list.replaceChildren(...entries.map(entry=>{
   const li=document.createElement("li"),detail=document.createElement("span");
   const strong=document.createElement("strong");strong.textContent=entry.symbol;
   detail.append(strong," — "+entry.event+" · expected "+entry.expectedWindow+(entry.notes?" · "+entry.notes:"")+" ");
   if(entry.sourceUrl&&/^https?:\/\//i.test(entry.sourceUrl)){const a=document.createElement("a");a.href=entry.sourceUrl;a.target="_blank";a.rel="noopener noreferrer";a.textContent="source";detail.append(a);}
   const remove=document.createElement("button");remove.type="button";remove.className="quiet-button";remove.textContent="Remove (readout public)";
   remove.addEventListener("click",async()=>{
    remove.disabled=true;
    try{const res=await fetch("/api/watchlists/pending-catalysts/"+encodeURIComponent(entry.symbol),{method:"DELETE"});if(!res.ok)throw new Error("HTTP "+res.status);await refreshPendingCatalysts();}
    catch(e){if(status)status.textContent="Could not remove "+entry.symbol+": "+e.message;remove.disabled=false;}
   });
   li.append(detail,remove);return li;
  }));
  if(status)status.textContent=entries.length+" pending catalyst(s) · API-backed MCC list";
 }catch(error){
  if(badge)badge.textContent="?";
  if(status)status.textContent="Pending-catalyst API unavailable: "+error.message+(error.message.includes("404")?". Restart MCC on the latest version.":".");
 }
 applyPendingCatalystFlags();
}
$("openCatalysts")?.addEventListener("click",()=>{if(!catalystsDialog||catalystsDialog.open)return;catalystsDialog.showModal();void refreshPendingCatalysts();$("catalystSymbol")?.focus();});
$("closeCatalysts")?.addEventListener("click",()=>catalystsDialog?.close());
$("refreshCatalysts")?.addEventListener("click",()=>void refreshPendingCatalysts());
$("catalystForm")?.addEventListener("submit",async event=>{
 event.preventDefault();
 const status=$("catalystStatus"),body={symbol:$("catalystSymbol").value.trim().toUpperCase(),event:$("catalystEvent").value.trim(),expectedWindow:$("catalystWindow").value.trim(),sourceUrl:$("catalystSource").value.trim()||null,notes:$("catalystNotes").value.trim()||null};
 try{
  const res=await fetch("/api/watchlists/pending-catalysts",{method:"POST",headers:{"Content-Type":"application/json",Accept:"application/json"},body:JSON.stringify(body)});
  const data=await res.json().catch(()=>({}));
  if(!res.ok)throw new Error(data.error||("HTTP "+res.status));
  $("catalystForm").reset();await refreshPendingCatalysts();
  if(status)status.textContent="Added "+data.added+". "+status.textContent;
 }catch(e){if(status)status.textContent="Could not add: "+e.message;}
});
{const grid=$("redZoneGrid");if(grid)new MutationObserver(()=>applyPendingCatalystFlags()).observe(grid,{childList:true,subtree:true});}
void refreshPendingCatalysts();
window.setInterval(()=>{if(document.visibilityState==="visible")void refreshPendingCatalysts();},60000);

/// MODULE: Opt-in Overnight panels (2026-11-17, operator request: streamlined flow, information on demand)
/// PURPOSE: Remembers which optional panels (all imported rows, 2.4, 2.5, 2.6) the operator opened, and keeps the
/// row count in the "All imported rows" summary current. Panels are closed by default; contents still update while closed.
(()=>{
 const key="mcc.optinPanels.v1";let saved={};try{saved=JSON.parse(localStorage.getItem(key)||"{}")||{};}catch{}
 document.querySelectorAll("details.optin-details[id]").forEach(d=>{
  if(saved[d.id]===true)d.open=true;
  d.addEventListener("toggle",()=>{saved[d.id]=d.open;try{localStorage.setItem(key,JSON.stringify(saved));}catch{}});
 });
 const body=$("overnightQualificationRows"),label=$("optinQualTableLabel");
 if(body&&label){const paint=()=>{const n=[...body.rows].filter(r=>!r.querySelector("td[colspan]")).length;label.textContent=n?"All imported rows ("+n+")":"All imported rows";};new MutationObserver(paint).observe(body,{childList:true});paint();}
})();
