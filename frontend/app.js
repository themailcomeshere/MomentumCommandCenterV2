/*
/// MODULE: Demo Workbench State
/// PURPOSE: Drives illustrative front-end states for layout testing only.
/// WHEN CALLED: Runs in the browser when index.html loads or controls change.
/// IMPORTANT: No market-data, broker, or order APIs are called.
*/
const snapshots=[
{label:"Snapshot 1",price:"$11.21",rvol:"0.8×",extension:"0.4 ATR",classification:"WATCH — DEVELOPING",kind:"watch",headline:"The setup is developing, but the execution event is not active yet.",eligibility:"Eligibility: PASS (demo)",five:"Permission: YES (demo)",one:"Execution event: INACTIVE (demo)",reasons:["Execution event is inactive; do not treat this as READY.","Relative volume is below the illustrative participation band.","Wait for an actual 1M confirmation event."]},
{label:"Snapshot 2",price:"$11.25",rvol:"1.1×",extension:"0.5 ATR",classification:"READY — BORING",kind:"boring",headline:"Technically eligible, but participation and movement capacity look modest in this illustration.",eligibility:"Eligibility: PASS (demo)",five:"Permission: YES (demo)",one:"Execution event: ACTIVE (demo)",reasons:["Low relative volume — participation is modest.","Recent movement capacity is limited in this illustration.","READY describes eligibility/timing; it does not rank opportunity quality."]},
{label:"Snapshot 3",price:"$11.29",rvol:"1.2×",extension:"0.7 ATR",classification:"READY — BORING",kind:"boring",headline:"Technically eligible does not necessarily mean a high-opportunity trade.",eligibility:"Eligibility: PASS (demo)",five:"Permission: YES (demo)",one:"Execution event: ACTIVE (demo)",reasons:["Low relative volume — participation is modest.","Recent movement capacity is limited.","Confirm the actual chart, spread, resistance, and event context before acting."]},
{label:"Snapshot 4",price:"$11.36",rvol:"2.7×",extension:"0.8 ATR",classification:"READY — HIGH",kind:"high",headline:"Stronger participation is shown in this demo; the label is not a forecast or a probability.",eligibility:"Eligibility: PASS (demo)",five:"Permission: YES (demo)",one:"Execution event: ACTIVE (demo)",reasons:["Strong relative volume in this illustrative snapshot.","Momentum and recent movement capacity are assumed for this mockup.","This label is provisional until source-data parity and historical validation are complete."]},
{label:"Snapshot 5",price:"$11.44",rvol:"3.1×",extension:"1.8 ATR",classification:"READY — EXTENDED",kind:"extended",headline:"Strong movement can still mean poor entry timing. Avoid equating momentum with permission to chase.",eligibility:"Eligibility: PASS (demo)",five:"Permission: YES (demo)",one:"Execution event: ACTIVE (demo)",reasons:["ATR extension is above the illustrative warning threshold.","An extended entry can be downgraded even when momentum is strong.","Reassess timing and risk; this is not an order instruction."]}
];
const barHeights=[48,56,53,68,76],$=id=>document.getElementById(id);
const feedbackKey="mcc.dashboardFeedback.v1";
function render(){
 const index=Number($("snapshot").value),item=snapshots[index],overnight=$("mode").value==="overnight";
 $("classification").textContent=overnight?"RESEARCH ONLY":item.classification;
 $("classification").className="classification "+(overnight?"watch":item.kind);
 $("headline").textContent=overnight?"The overnight scoring pipeline is not wired to live or historical inputs yet.":item.headline;
 $("price").textContent=item.price;$("rvol").textContent=item.rvol;$("extension").textContent=item.extension;
 $("eligibility").textContent=item.eligibility;$("fiveState").textContent=item.five;$("oneState").textContent=item.one;
 $("reasons").replaceChildren(...item.reasons.map(reason=>{const li=document.createElement("li");li.textContent=reason;return li;}));
 $("replayLabel").textContent=item.label+" of 5";
 $("bars").replaceChildren(...barHeights.map((height,i)=>{const el=document.createElement("div");el.className="bar"+(i===index?" active":"");el.style.height=height+"%";const label=document.createElement("span");label.textContent=i+1;el.append(label);return el;}));
 $("overnightPanel").classList.toggle("hidden",!overnight);
 document.querySelector(".hero-card .eyebrow").textContent=overnight?"OVERNIGHT WORKFLOW · MOCKUP":"OPPORTUNITY CLASSIFICATION";
}
$("snapshot").addEventListener("input",render);$("mode").addEventListener("change",render);
$("symbol").addEventListener("change",()=>{const chosen=$("symbol").value;if(chosen==="MNSB"){render();$("freshness").textContent="Data: SIMULATED";}else{$("headline").textContent=chosen+" is a placeholder selection; no symbol data is loaded.";$("freshness").textContent="Data: NOT LOADED";}});
$("reset").addEventListener("click",()=>{$("mode").value="momentum";$("symbol").value="MNSB";$("snapshot").value="2";$("freshness").textContent="Data: SIMULATED";render();});
 render();

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
 const symbols=$("apiScanSymbols").value.trim().split(/[\\s,;]+/).filter(Boolean);
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
  renderScanner();
 }catch(error){source.textContent="API scan failed: "+error.message+" · confirm this dashboard is served by MCC API.";}
 finally{button.disabled=false;}
});

function renderScanner(){
 const rows=$("scannerRows"),group=$("scannerGroup").value;
 rows.replaceChildren(...(scannerData[group]||[]).map(candidate=>{
  const tr=document.createElement("tr");
  const statusClass=candidate.status==="BLOCKED"?"status-blocked":candidate.status==="WATCH"?"status-watch":"status-ready";
  const cell=value=>{const td=document.createElement("td");td.textContent=value??"DATA?";return td;};
  tr.append(cell(candidate.symbol),cell(candidate.score),cell(candidate.status),cell(candidate.extension||"DATA?"),cell(candidate.freshness||"UNKNOWN"),cell(candidate.reason),document.createElement("td"));
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
function renderMorningReport(){
 const grid=$("redZoneGrid");if(!grid)return;
 const group=$("scannerGroup")?.value||"momentum";
 const sourceRows=Array.isArray(scannerData[group])?scannerData[group]:[];
 const ranked=sourceRows.slice().sort((a,b)=>{
  const sa=Number(a.score),sb=Number(b.score);
  if(Number.isFinite(sa)!==Number.isFinite(sb))return Number.isFinite(sb)-Number.isFinite(sa);
  return (Number.isFinite(sb)?sb:-Infinity)-(Number.isFinite(sa)?sa:-Infinity)||String(a.symbol).localeCompare(String(b.symbol));
 }).slice(0,4);
 $("morningTopCount").textContent=String(ranked.length);
 $("reportSubtitle").textContent="Preview generated "+new Date().toLocaleString()+" · local browser clock · not a market-data timestamp";
 grid.replaceChildren(...(ranked.length?ranked.map((item,index)=>{
  const card=document.createElement("article");card.className="redzone-card";
  const top=document.createElement("div");top.className="redzone-card-top";
  const rank=document.createElement("span");rank.className="rank-pill";rank.textContent="#"+(index+1);
  const status=document.createElement("span");status.className="chip";status.textContent=item.status||"DATA?";
  top.append(rank,status);
  const symbol=document.createElement("h3");symbol.textContent=item.symbol||"UNKNOWN";
  const score=document.createElement("div");score.className="redzone-score";score.textContent="Supplied score: "+(item.score??"DATA?");
  const ext=document.createElement("p");ext.textContent="Extension: "+(item.extension||"DATA?");
  const fresh=document.createElement("p");fresh.textContent="Freshness: "+(item.freshness||"UNKNOWN");
  const reason=document.createElement("p");reason.className="redzone-reason";reason.textContent=item.reason||"No reason supplied.";
  const gate=document.createElement("div");gate.className="gate-warning";gate.textContent="5M Permission: UNKNOWN · 1M execution: UNKNOWN";
  card.append(top,symbol,score,ext,fresh,reason,gate);return card;
 }):[Object.assign(document.createElement("article"),{className:"redzone-empty",textContent:"No candidate rows available. Import a CSV or select a demo scanner group."})]));
}
$("scannerGroup")?.addEventListener("change",renderMorningReport);
$("exportMorningReport")?.addEventListener("click",()=>{
 const group=$("scannerGroup")?.value||"momentum";
 const rows=Array.isArray(scannerData[group])?scannerData[group]:[];
 const report={reportGeneratedAt:new Date().toISOString(),clockSource:"browser-local-clock; not market-data timestamp",mode:"PREVIEW_ONLY",liveScannerConnected:false,overnightCollectionRunning:false,provider:"NOT_SELECTED",scannerGroup:group,watchlist:readWatchlist(),topCandidates:rows.slice().sort((a,b)=>(Number(b.score)||-Infinity)-(Number(a.score)||-Infinity)).slice(0,4),boringOvernight:{status:"NOT_CALCULATED"},disclaimer:"Local demo/import data only. No live market data, scanner, 5M permission, 1M execution, or validated ranking is connected."};
 const blob=new Blob([JSON.stringify(report,null,2)],{type:"application/json"}),url=URL.createObjectURL(blob),a=document.createElement("a");
 a.href=url;a.download="mcc-morning-report-preview.json";a.click();URL.revokeObjectURL(url);
});
renderMorningReport();


// Provider comparison now calls the same-origin API. Keys never enter browser code.
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
 const from = new Date(date + "T00:00:00-07:00"), to = new Date(date + "T23:59:59-07:00");
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

async function loadMorningBrief() {
 try {
  const response = await fetch("/api/morning-brief", {headers:{Accept:"application/json"}});
  if (!response.ok) throw new Error("HTTP " + response.status);
  const report = await response.json();
  $("overnightServiceState").textContent = "Morning Brief API: " + report.status;
  $("overnightServiceDetail").textContent = "Generated " + new Date(report.generatedAtUtc).toLocaleString() + ". Observed market bars only; scanner, catalyst, sector, Red Zone and overnight scoring remain explicitly unavailable until their sources are connected.";
  $("reportSubtitle").textContent = "API report generated " + new Date(report.generatedAtUtc).toLocaleString() + " · source timestamps retained in response";
  const market = report.marketLandscape || [];
  const lines = market.map(item => {
   const obs = (item.observations || []).map(o => o.provider + ": " + (o.latest ? formatBar(o.latest) : (o.status || "DATA?"))).join(" | ");
   const moves = (item.observations || []).map(o => o.changeFromFirstPercent == null ? "" : o.provider + " window change " + o.changeFromFirstPercent + "%").filter(Boolean).join(" | ");
   return item.symbol + " — " + (obs || "DATA? No configured market-data source.") + (moves ? " · " + moves : "");
  });
  $("morningApiSnapshot")?.replaceChildren(...lines.map(line => { const p=document.createElement("p"); p.textContent=line; return p; }));
 } catch {
  $("overnightServiceState").textContent = "Morning Brief API: NOT CONNECTED";
  $("overnightServiceDetail").textContent = "Serve this dashboard through MomentumCommandCenterV2.API to load provider status and the partial Morning Brief. Direct file preview remains demo-only.";
 }
}
loadMorningBrief();


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
  if ((parsed.parityRows || []).length > 0) parityDownload?.classList.remove("hidden");\n  status.textContent = "Parse complete. " + (parsed.parityRows || []).length + " MCC OHLCV row(s) converted to the Chart Parity Template.";
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
