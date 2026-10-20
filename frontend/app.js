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

function renderScanner(){
 const rows=$("scannerRows"),group=$("scannerGroup").value;
 rows.replaceChildren(...(scannerData[group]||[]).map(candidate=>{
  const tr=document.createElement("tr");
  const statusClass=candidate.status==="BLOCKED"?"status-blocked":candidate.status==="WATCH"?"status-watch":"status-ready";
  tr.innerHTML=`<td><strong>${candidate.symbol}</strong></td><td class="score">${candidate.score}</td><td class="${statusClass}">${candidate.status}</td><td>${candidate.reason}</td><td></td>`;
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
  const imported=lines.map(line=>{const values=parseCsvLine(line);return {symbol:(values[symbolIndex]||"").trim().toUpperCase(),score:values[headers.indexOf("score")]||"—",status:values[headers.indexOf("status")]||"IMPORTED",reason:values[headers.indexOf("reason")]||"Imported local evidence."};}).filter(item=>item.symbol);
  scannerData.imported=imported;$("scannerGroup").value="imported";const option=document.querySelector('#scannerGroup option[value="imported"]')||document.createElement("option");option.value="imported";option.textContent="Imported local CSV";if(!option.parentElement)$("scannerGroup").append(option);$("scannerSource").textContent=`Imported ${imported.length} local rows`;renderScanner();
 };reader.readAsText(file);
});
renderScanner();renderWatchlist();
