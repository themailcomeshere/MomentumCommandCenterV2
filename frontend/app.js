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
