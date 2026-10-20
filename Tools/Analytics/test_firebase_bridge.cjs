'use strict';
const {test}=require('node:test'),assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
const source=fs.readFileSync(path.join(__dirname,'../../Assets/Plugins/WebGL/WebAnalytics.jslib'),'utf8');
function setup(){
  const calls=[],scripts=[],library={},window={};
  const context={window,document:{head:{appendChild:script=>scripts.push(script)},createElement:()=>({})},
    UTF8ToString:value=>value,LibraryManager:{library},mergeInto:(target,values)=>Object.assign(target,values),Date,JSON,encodeURIComponent};
  vm.runInNewContext(source,context);
  return {calls,scripts,library,window,attach(){window.gtag=(...args)=>calls.push(args);}};
}
test('tag initializes once and uses the team measurement ID',()=>{const s=setup();s.library.AnalyticsStart('G-VGZFRN1EQG');s.library.AnalyticsStart('G-VGZFRN1EQG');assert.equal(s.scripts.length,1);assert.equal(s.scripts[0].src,'https://www.googletagmanager.com/gtag/js?id=G-VGZFRN1EQG');assert.equal(s.window.dataLayer[1][0],'config');assert.equal(s.window.dataLayer[1][1],'G-VGZFRN1EQG');});
test('events before initialization do not throw or send data',()=>{const s=setup();s.library.AnalyticsTrackEvent('level_started','{}');assert.equal(s.calls.length,0);assert.equal(s.scripts.length,0);});
test('completion time stays numeric through the actual WebGL bridge',()=>{const s=setup();s.attach();s.library.AnalyticsTrackEvent('level_completed',JSON.stringify({level:'Level 1',build_version:'qa',active_seconds:15.092}));assert.equal(s.calls[0][0],'event');assert.equal(s.calls[0][1],'level_completed');assert.equal(s.calls[0][2].active_seconds,15.092);assert.equal(typeof s.calls[0][2].active_seconds,'number');});
test('start and manual restart keep matching level/build parameters',()=>{const s=setup();s.attach();for(const name of ['level_started','level_restarted'])s.library.AnalyticsTrackEvent(name,'{"level":"TutorialLevel","build_version":"qa"}');assert.deepEqual(s.calls.map(c=>c[1]),['level_started','level_restarted']);for(const call of s.calls){assert.equal(call[2].level,'TutorialLevel');assert.equal(call[2].build_version,'qa');}});
test('existing teammate event names pass through unchanged',()=>{const s=setup();s.attach();for(const name of ['level_failed','coin_collected','death_by_falling'])s.library.AnalyticsTrackEvent(name,'{"level":"Level 2"}');assert.deepEqual(s.calls.map(c=>c[1]),['level_failed','coin_collected','death_by_falling']);});
test('events can queue while the Google script is still loading',()=>{const s=setup();s.library.AnalyticsStart('G-VGZFRN1EQG');s.library.AnalyticsTrackEvent('level_started','{"level":"Level 3"}');assert.equal(s.window.dataLayer.length,3);assert.equal(s.window.dataLayer[2][1],'level_started');});
