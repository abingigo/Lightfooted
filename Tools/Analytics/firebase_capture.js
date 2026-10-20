// Development build only. Capture real game calls locally without sending QA data to Google.
(function () {
  if (new URLSearchParams(location.search).get('analytics-capture') !== '1') return;
  window.__gaStarted = true;
  var records = [];
  function render() {
    var panel = document.getElementById('firebase-capture');
    if (!panel) {
      panel = document.createElement('section'); panel.id = 'firebase-capture';
      panel.style.cssText = 'position:relative;max-width:960px;margin:18px auto;padding:18px;background:#172532;color:#e3edf1;font:14px/1.5 system-ui;';
      var title = document.createElement('h2'); title.textContent = 'LOCAL QA · events captured, not sent to Google';
      var pre = document.createElement('pre'); pre.id = 'firebase-capture-events';
      pre.style.cssText = 'white-space:pre-wrap;overflow-wrap:anywhere;';
      panel.appendChild(title); panel.appendChild(pre); document.body.appendChild(panel);
      var container = document.getElementById('unity-container');
      if (container) {
        container.style.cssText = 'position:relative;left:auto;top:auto;transform:none;width:fit-content;margin:20px auto;';
        var footer = document.getElementById('unity-footer'); if (footer) footer.style.display = 'flow-root';
      }
    }
    document.getElementById('firebase-capture-events').textContent = JSON.stringify(records, null, 2);
  }
  window.gtag = function (kind, name, parameters) {
    if (kind !== 'event') return;
    records.push({name:name, parameters:parameters, captured_ms:Math.round(performance.now())});
    if (document.body) render();
  };
  document.addEventListener('DOMContentLoaded', render);
})();
