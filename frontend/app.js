/// MODULE: Live Decision Workbench
/// PURPOSE: Renders the focused symbol from the live assessment API; no synthetic snapshots are used.
const $=id=>document.getElementById(id);
const feedbackKey="mcc.dashboardFeedback.v1";
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
 const params=new URLSearchParams({symbol,providerName:"Alpaca"});
 const response=await fetch("/api/market-data/live-assessment?"+params.toString(),{cache:"no-store",headers:{Accept:"application/json"}});
 const body=await response.json().catch(()=>({}));
 if(!response.ok)throw new Error(body.detail||body.error||("HTTP "+response.status));
 return body;
}
function sourceFreshness(label,ageSeconds){
 if(!label)return "UNKNOWN";
 return label+(ageSeconds!==null&&ageSeconds!==undefined&&Number.isFinite(Number(ageSeconds))?" · "+Math.max(0,Number(ageSeconds)).toFixed(0)+"s":"");
}
function humanizeSignalState(value){return String(value||"UNKNOWN").replace(/([a-z])([A-Z])/g,"$1 $2").replaceAll("_"," ").toUpperCase();}
function formatMetric(value,digits=2,suffix=""){return value===null||value===undefined||!Number.isFinite(Number(value))?"UNKNOWN":Number(value).toFixed(digits)+suffix;}
function setV2ScoreDisplay(node,decision,evaluationStatus){
 if(!node)return;
 if(!decision||!Number.isFinite(Number(decision.score))){node.className="redzone-score score-unknown";node.textContent="V2 5M score: UNKNOWN · "+(evaluationStatus||"NOT EVALUATED");return;}
 const score=Number(decision.score),entryReady=decision.entryAllowed===true;
 const band=score<=2?"score-low":score<=4?"score-mid":"score-high";
 node.className="redzone-score "+band;
 const marker=score<=2?"✕ ":score>=5?"✓ ":"";
 const suffix=entryReady?" · EXECUTION READY":score>=5?" · 5M PERMISSION; WAIT FOR 1M":score<=2?" · LOW ALIGNMENT":" · PARTIAL ALIGNMENT";
 node.textContent=marker+"V2 5M score: "+score+"/6"+suffix;
}
async function refreshDecisionExplanation(){
 const symbol=($("symbol")?.value||"").trim().toUpperCase();
 const classification=$("classification"),headline=$("headline"),price=$("price"),rvol=$("rvol"),extension=$("extension"),eligibility=$("eligibility"),freshness=$("freshness"),reasons=$("reasons"),fiveState=$("fiveState"),oneState=$("oneState"),scoreNode=$("decisionScore"),sourceNode=$("decisionSource");
 if(!symbol||!classification)return;
 classification.textContent="LOADING LIVE ASSESSMENT";classification.className="classification watch";
 if(headline)headline.textContent="Requesting current completed 1M and 5M bars…";
 try{
  const data=await fetchLiveAssessment(symbol),one=data.oneMinute||{},five=data.fiveMinute||{},decision=data.decision||null;
  const transportLabel=$("marketTransportLabel");
  if(transportLabel)transportLabel.textContent=one.sourceTransport==="ALPACA_WEBSOCKET_LATEST_BAR"?"ALPACA 1M WS / 5M REST · 15s UI REFRESH":"ALPACA REST · 15s UI REFRESH";
  const permission=decision?.fiveMinutePermission===true?"YES":decision?.fiveMinutePermission===false?"NO":"UNKNOWN",execution=decision?.oneMinuteExecution||"UNKNOWN",ready=decision?.entryAllowed===true;
  classification.textContent=ready?"EXECUTION READY":decision?humanizeSignalState(decision.state):humanizeSignalState(data.evaluationStatus||"DATA UNAVAILABLE");
  classification.className="classification "+(ready?"high":decision?.fiveMinutePermission?"boring":"watch");
  if(headline)headline.textContent=decision?.reason||data.evaluationReason||"No decision reason returned.";
  if(price)price.textContent=one.close==null?"DATA?":"$"+Number(one.close).toFixed(2);
  if(rvol)rvol.textContent=five.relativeVolume==null?"UNKNOWN":Number(five.relativeVolume).toFixed(2)+"×";
  if(extension)extension.textContent=one.extensionAtr==null?"DATA?":Number(one.extensionAtr).toFixed(2)+" ATR";
  if(eligibility)eligibility.textContent="5M permission: "+permission;
  if(freshness)freshness.textContent="1M "+sourceFreshness(one.freshness,one.ageSeconds)+" · 5M "+sourceFreshness(five.freshness,five.ageSeconds);
  if(fiveState)fiveState.textContent="5M permission: "+permission+(decision?.fiveMinuteStructureBull===true?" · bullish structure":decision?.fiveMinuteStructureBull===false?" · structure not bullish":"");
  if(oneState)oneState.textContent="1M execution: "+execution+(ready?" · entry gate passed":"");
  setV2ScoreDisplay(scoreNode,decision,data.evaluationStatus);
  const lines=[decision?.reason?"Decision: "+decision.reason:"Decision unavailable: "+(data.evaluationReason||data.evaluationStatus||"No evaluation details returned.")];
  for(const criterion of (five.scoreBreakdown?.criteria||[])){const mark=criterion.passed===true?"✓":criterion.passed===false?"✕":"?";lines.push(mark+" "+criterion.label+(criterion.passed===null?" — unavailable":""));}
  if(five.relativeVolumeBaselineSamples!==undefined)lines.push("5M same-time RVOL baseline: "+five.relativeVolumeBaselineSamples+" prior-session samples; minimum 5.");
  if(one.sourceTimestampUtc)lines.push("1M source bar: "+new Date(one.sourceTimestampUtc).toLocaleString());
  if(five.sourceTimestampUtc)lines.push("5M source bar: "+new Date(five.sourceTimestampUtc).toLocaleString());
  if(one.ema9!=null&&one.ema20!=null)lines.push("1M EMA9 "+formatMetric(one.ema9)+" · EMA20 "+formatMetric(one.ema20)+" · RSI "+formatMetric(one.rsi,1));
  if(reasons)reasons.replaceChildren(...lines.map(line=>{const li=document.createElement("li");li.textContent=line;return li;}));
  if(sourceNode)sourceNode.textContent="Source "+(data.provider||"UNKNOWN")+"/"+(data.feed||"feed UNKNOWN")+" · 1M "+(one.sourceTransport||"UNKNOWN")+" · 5M REST · "+(data.evaluationStatus||"UNKNOWN")+" · evaluated "+(data.evaluatedAtUtc?new Date(data.evaluatedAtUtc).toLocaleTimeString():"UNKNOWN");
 }catch(error){
  classification.textContent="DATA UNAVAILABLE";classification.className="classification watch";
  if(headline)headline.textContent="Live assessment request failed: "+error.message;
  if(price)price.textContent="DATA?";if(rvol)rvol.textContent="UNKNOWN";if(extension)extension.textContent="DATA?";
  if(eligibility)eligibility.textContent="5M permission: UNKNOWN";if(freshness)freshness.textContent="Freshness: UNKNOWN";
  if(fiveState)fiveState.textContent="5M permission: UNKNOWN";if(oneState)oneState.textContent="1M execution: UNKNOWN";
  setV2ScoreDisplay(scoreNode,null,"REQUEST FAILED");
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

function parseCsvLine(line){
 const fields=[],buffer=[];let quoted=false;
 for(let i=0;i<line.length;i++){const char=line[i];if(char==='"'){if(quoted&&line[i+1]==='"'){buffer.push('"');i++;}else quoted=!quoted;}else if(char===','&&!quoted){fields.push(buffer.join(""));buffer.length=0;}else buffer.push(char);}
 fields.push(buffer.join(""));return fields;
}

$("scannerGroup").addEventListener("change",renderScanner);
$("watchlistForm").addEventListener("submit",event=>{event.preventDefault();addWatch($("watchSymbol").value);$("watchSymbol").value="";});
$("clearWatchlist").addEventListener("click",()=>writeWatchlist([]));
$("scannerFile").addEventListener("change",event=>{
 const file=event.target.files[0];if(!file)return;
 const reader=new FileReader();reader.onload=()=>{
  const lines=String(reader.result).split(/\r?\n/).filter(Boolean),headers=lines.shift().split(",").map(value=>value.trim().toLowerCase());
  const symbolIndex=headers.findIndex(value=>["symbol","ticker"].includes(value));
  if(symbolIndex<0){$("scannerSource").textContent="Import rejected: Symbol/Ticker column required";return;}
  const imported=lines.map(line=>{const values=parseCsvLine(line),field=name=>{const i=headers.indexOf(name);return i<0?"":(values[i]||"").trim();};return {symbol:(values[symbolIndex]||"").trim().toUpperCase(),score:field("score")||"—",status:field("status")||"IMPORTED",extension:field("extension")||field("ext")||"DATA?",freshness:field("freshness")||field("data_as_of")||"UNKNOWN",reason:field("reason")||"Imported local evidence."};}).filter(item=>item.symbol);
  scannerData.imported=imported;$("scannerGroup").value="imported";const option=document.querySelector('#scannerGroup option[value="imported"]')||document.createElement("option");option.value="imported";option.textContent="Imported local CSV";if(!option.parentElement)$("scannerGroup").append(option);$("scannerSource").textContent=`Imported ${imported.length} local rows`;renderScanner();
 };reader.readAsText(file);
});
renderScanner();renderWatchlist();


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
  return {pinned:Array.isArray(value.pinned)?value.pinned.filter(x=>typeof x==="string"):[],dismissed:Array.isArray(value.dismissed)?value.dismissed.filter(x=>typeof x==="string"):[]};
 }catch{return {pinned:[],dismissed:[]};}
}
function writeRedZoneState(state){localStorage.setItem("mcc.redZoneState.v1",JSON.stringify(state));renderMorningReport();}
let redZoneAssessmentRefreshers=[];
let redZoneSoundEnabled=false,redZoneAudioContext=null,previousRedZoneSymbols=null,suppressNextRedZoneAlert=false;
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
async function updateRedZoneCardAssessment(symbol,statusNode,scoreNode,extensionNode,freshnessNode,reasonNode,gateNode,chartNode){
 void ensureMinuteBarSubscription(symbol);
 try{
  const data=await fetchLiveAssessment(symbol),one=data.oneMinute||{},five=data.fiveMinute||{},decision=data.decision||null;
  statusNode.textContent=decision?humanizeSignalState(decision.state):humanizeSignalState(data.evaluationStatus||data.status||"DATA?");
  setV2ScoreDisplay(scoreNode,decision,data.evaluationStatus);
  extensionNode.textContent="Extension: "+(one.extensionAtr!==null&&one.extensionAtr!==undefined&&Number.isFinite(Number(one.extensionAtr))?Number(one.extensionAtr).toFixed(2)+" ATR":"DATA?")+" (1M close vs EMA9)";
  freshnessNode.textContent="Freshness: 1M "+sourceFreshness(one.freshness,one.ageSeconds)+" · 5M "+sourceFreshness(five.freshness,five.ageSeconds);
  reasonNode.textContent=decision?.reason||data.evaluationReason||"Decision not evaluated.";
  gateNode.textContent="5M Permission: "+(decision?.fiveMinutePermission===true?"YES":decision?.fiveMinutePermission===false?"NO":"UNKNOWN")+
   " · 1M execution: "+(decision?.oneMinuteExecution||"UNKNOWN");
  drawPriceLine(chartNode,one.chartBars||[],symbol);
 }catch(error){
  statusNode.textContent="DATA UNAVAILABLE";
  setV2ScoreDisplay(scoreNode,null,"REQUEST FAILED");
  extensionNode.textContent="Extension: DATA? · live request failed";
  freshnessNode.textContent="Freshness: UNKNOWN · "+error.message;
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
 const sortedRows=sourceRows.slice().sort((a,b)=>{
  const sa=Number(a.score),sb=Number(b.score);
  if(Number.isFinite(sa)!==Number.isFinite(sb))return Number.isFinite(sb)-Number.isFinite(sa);
  const av=Number.isFinite(sa)?(group==="live"?Math.abs(sa):sa):-Infinity;
  const bv=Number.isFinite(sb)?(group==="live"?Math.abs(sb):sb):-Infinity;
  return bv-av||String(a.symbol).localeCompare(String(b.symbol));
 });
 const dismissed=new Set(state.dismissed);
 const pinnedSet=new Set(state.pinned);
 const ranked=[...pinnedRows,...sortedRows.filter(item=>!dismissed.has(String(item.symbol||"").toUpperCase())&&!pinnedSet.has(String(item.symbol||"").toUpperCase()))]
  .filter((item,index,all)=>all.findIndex(x=>String(x.symbol||"").toUpperCase()===String(item.symbol||"").toUpperCase())===index)
  .slice(0,4);
 const currentSymbols=new Set(ranked.map(item=>String(item.symbol||"").toUpperCase()).filter(Boolean));
 if(previousRedZoneSymbols&&!suppressNextRedZoneAlert&&redZoneSoundEnabled){
  const added=[...currentSymbols].filter(symbol=>!previousRedZoneSymbols.has(symbol));
  if(added.length){playRedZoneAlert();const soundButton=$("toggleRedZoneSound");if(soundButton)soundButton.title="New Red Zone priority stock: "+added.join(", ");}
 }
 previousRedZoneSymbols=currentSymbols;suppressNextRedZoneAlert=false;
 $("morningTopCount").textContent=String(ranked.length);
 $("reportSubtitle").textContent="Preview generated "+new Date().toLocaleString()+" · local browser clock · not a market-data timestamp";
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
  const reason=document.createElement("p");reason.className="redzone-reason";reason.textContent=item.reason||"No reason supplied.";
  const gate=document.createElement("div");gate.className="gate-warning";gate.textContent="5M Permission: loading · 1M execution: loading";
  const chart=document.createElement("div");chart.className="redzone-sparkline";chart.textContent="Loading 1M price…";
  const actions=document.createElement("div");actions.className="redzone-actions";
  const focusButton=document.createElement("button");focusButton.type="button";focusButton.className="quiet-button";focusButton.textContent="Open in Main Chart";
  const manageButton=document.createElement("button");manageButton.type="button";manageButton.className="quiet-button";manageButton.textContent=pinnedSet.has(String(item.symbol||"").toUpperCase())?"Remove pin":"Dismiss";
  manageButton.addEventListener("click",event=>{
   event.stopPropagation();
   const current=readRedZoneState(),ticker=String(item.symbol||"").toUpperCase();
   if(current.pinned.includes(ticker))current.pinned=current.pinned.filter(x=>x!==ticker);
   else current.dismissed=[...new Set([...current.dismissed,ticker])];
   writeRedZoneState(current);
  });
  actions.append(focusButton,manageButton);
  card.append(top,symbol,score,ext,fresh,reason,chart,gate,actions);
  redZoneAssessmentRefreshers.push(()=>updateRedZoneCardAssessment(item.symbol,status,score,ext,fresh,reason,gate,chart));
  void updateRedZoneCardAssessment(item.symbol,status,score,ext,fresh,reason,gate,chart);
  card.tabIndex=0;card.setAttribute("role","button");card.setAttribute("aria-label","Focus "+(item.symbol||"candidate")+" price chart");
  const focus=()=>{const selector=$("symbol");if(selector&&item.symbol){let option=[...selector.options].find(o=>o.value===item.symbol);if(!option){option=document.createElement("option");option.value=item.symbol;option.textContent=item.symbol;selector.append(option);}selector.value=item.symbol;render();refreshFocusedPriceChart();}};
  focusButton.addEventListener("click",event=>{event.stopPropagation();focus();});
  card.addEventListener("click",focus);card.addEventListener("keydown",event=>{if(event.key==="Enter"||event.key===" "){event.preventDefault();focus();}});
  return card;
 }):[Object.assign(document.createElement("article"),{className:"redzone-empty",textContent:"No candidate rows available. Import a CSV or select a demo scanner group."})]));
}
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
 const state=readRedZoneState();state.dismissed=[];writeRedZoneState(state);
 $("redZoneManageStatus").textContent="Dismissed scanner candidates restored.";
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
   if (prefix === "alpaca" && feedMatch) {
    const feedNode = document.querySelector("#alpacaTitle")?.closest(".provider-card")?.querySelector(".provider-card-heading .chip");
    if (feedNode) feedNode.textContent = "Configured feed: " + feedMatch[1];
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

const requestedMinuteBarSymbols=new Set();
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
  node.textContent=!state.enabled?"WS minute bars: DISABLED":state.connected
   ?"WS minute bars: CONNECTED · "+(state.feed||"feed")+" · "+subscribed.length+" subscribed · "+(state.persistedBars??0)+" stored"
   :"WS minute bars: "+(state.state||"UNKNOWN")+(state.lastError?" · "+state.lastError:"");
  const perSymbol=Object.entries(barStats).map(([ticker,info])=>ticker+" "+info.barCount+" bars · "+(info.lastBarUtc||"no bar")).join("; ");
  node.title="MCC dynamically subscribes the selected symbol and Red Zone candidates. Configured symbols are only the initial seed. Acknowledged: "+(subscribed.join(", ")||"none")+" · Per-symbol bars: "+(perSymbol||"none yet")+" · Last stream bar: "+(state.lastBarUtc||"none");
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
loadMorningBrief();

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
 const blob = new Blob(["\\uFEFF", csv], {type:"text/csv;charset=utf-8"});
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
 const status = $("overnightStatus"), rows = $("overnightSnapshotRows");
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
   const gateInputs = [
    "dataFresh="+(request.dataFresh ?? request.DataFresh ?? "UNKNOWN"),
    "catalyst="+(request.catalystStatus || request.CatalystStatus || "UNKNOWN"),
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
    gateInputs
   ];
   for (const value of values) { const td=document.createElement("td"); td.textContent=String(value); tr.append(td); }
   return tr;
  }) : [Object.assign(document.createElement("tr"),{innerHTML:"<td colspan='6'>No saved evaluations found for this query.</td>"})]));
  status.textContent = "Loaded "+snapshots.length+" saved input snapshot(s); GET returns inputs, not the evaluator decision. "+(data.note||"");
 } catch(error) {
  status.textContent = "Could not load saved evaluations: "+error.message;
 }
}
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
  status.textContent="Bar request complete · "+symbol+" · "+$("overnightTimeframe").value+" · Arizona "+$("overnightFrom").value+" → "+$("overnightTo").value+" · UTC "+fromUtc+" → "+toUtc+".";
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
