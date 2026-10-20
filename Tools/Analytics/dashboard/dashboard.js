"use strict";
const $ = id => document.getElementById(id);
const shortName = value => value.split('/').pop().replace(/\.unity$/, '').replace(/([a-z])([A-Z])/g, '$1 $2');
const ns = 'http://www.w3.org/2000/svg';
function svgNode(tag, attrs, text) {
  const node = document.createElementNS(ns, tag);
  Object.entries(attrs || {}).forEach(([key,value]) => node.setAttribute(key,value));
  if (text !== undefined) node.textContent = text;
  return node;
}
function bars(id, rows, unit, maximum, color='#a8ead0') {
  const parent = $(id); parent.replaceChildren();
  if (!rows.length) { const p=document.createElement('p');p.className='none';p.textContent='No observations for this metric yet.';parent.append(p);return; }
  const width=510, left=190, right=65, height=rows.length*55+30;
  const max=maximum || Math.max(1,...rows.map(row=>row.value));
  const svg=svgNode('svg',{viewBox:`0 0 ${width} ${height}`,role:'img','aria-label':rows.map(row=>`${row.label}: ${row.value.toFixed(1)}${unit}`).join('; ')});
  rows.forEach((row,i)=>{
    const y=i*55+12;
    const label=svgNode('text',{x:0,y:y+17,fill:'#c5d3db','font-size':13},row.label.length>26?row.label.slice(0,25)+'…':row.label);
    label.append(svgNode('title',{},row.label));svg.append(label);
    svg.append(svgNode('rect',{x:left,y,width:width-left-right,height:25,rx:3,fill:'#273b48'}));
    svg.append(svgNode('rect',{x:left,y,width:Math.max(0,(width-left-right)*row.value/max),height:25,rx:3,fill:row.color||color}));
    svg.append(svgNode('text',{x:width-right+9,y:y+17,fill:'#e3edf1','font-size':13},row.value.toFixed(1)+unit));
    svg.append(svgNode('text',{x:left,y:y+42,fill:'#afbec7','font-size':11},row.note||''));
  });parent.append(svg);
}
function options(id, values, all) {
  const select=$(id), previous=select.value;
  select.replaceChildren(new Option(all,''),...values.map(value=>new Option(id==='level'?shortName(value):value,value)));
  if (values.includes(previous)) select.value=previous;
}
let busy=false;
async function refresh() {
  if(busy)return;busy=true;
  const query=new URLSearchParams(); if($('build').value)query.set('build',$('build').value);if($('level').value)query.set('level',$('level').value);
  try {
    const response=await fetch('/api/metrics?'+query,{cache:'no-store'});if(!response.ok)throw new Error('Collector returned '+response.status);
    const report=await response.json();options('build',report.builds,'All builds');options('level',report.levels,'All levels');
    $('empty').hidden=report.attempt_count>0;
    $('starts').textContent=report.attempt_count;
    $('completions').textContent=report.level_metrics.reduce((sum,row)=>sum+row.completions,0);
    $('unfinished').textContent=report.level_metrics.reduce((sum,row)=>sum+row.unfinished,0);
    $('events').textContent=report.event_count;
    bars('completion',report.level_metrics.map(row=>({label:shortName(row.level_id),value:row.completion_rate,note:`${row.completions} of ${row.starts} attempts completed`})), '%',100);
    bars('duration',report.level_metrics.filter(row=>row.median_active_completion_seconds!==null).map(row=>({label:shortName(row.level_id),value:row.median_active_completion_seconds,note:`${row.completions} completed attempts`})), 's',null,'#80bde9');
    bars('checkpoints',report.checkpoint_metrics.map(row=>({label:`${shortName(row.level_id)} / ${row.checkpoint_id}`,value:row.reach_rate,note:`${row.reaches} arrivals · median split ${row.median_split_seconds.toFixed(1)}s`})), '%',100,'#80bde9');
    bars('failures',report.level_metrics.flatMap(row=>[{label:shortName(row.level_id)+' / death',value:row.death_rate,note:`${row.deaths} of ${row.starts} attempts`,color:'#f29593'},{label:shortName(row.level_id)+' / restart',value:row.restart_rate,note:`${row.restarts} of ${row.starts} attempts`,color:'#edca87'}]), '%',100);
    $('table').replaceChildren(...report.level_metrics.map(row=>{const tr=document.createElement('tr');[shortName(row.level_id),row.starts,row.completions,row.deaths,row.restarts,row.unfinished].forEach(value=>{const td=document.createElement('td');td.textContent=value;tr.append(td)});return tr;}));
    $('export').href='/api/metrics.csv?'+query;
    $('status').className='';$('status').textContent='Database checked '+new Date().toLocaleTimeString();
  }catch(error){$('status').className='error';$('status').textContent='Could not refresh. Previous results may be stale. '+error.message;}
  finally{busy=false;}
}
$('refresh').addEventListener('click',refresh);$('build').addEventListener('change',refresh);$('level').addEventListener('change',refresh);
refresh();setInterval(refresh,5000);
