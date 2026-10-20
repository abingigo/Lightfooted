mergeInto(LibraryManager.library, {
  LF_AnalyticsRead: function(keyPtr) {
    var text;
    try { text = window.localStorage.getItem(UTF8ToString(keyPtr)) || ''; }
    catch (error) { text = '{"storage_unavailable":true}'; }
    var size = lengthBytesUTF8(text) + 1;
    var buffer = _malloc(size);
    stringToUTF8(text, buffer, size);
    return buffer;
  },
  LF_AnalyticsWrite: function(keyPtr, valuePtr) {
    try { window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr)); return 1; }
    catch (error) { return 0; }
  },
  LF_AnalyticsDownload: function(jsonPtr, filenamePtr) {
    var blob = new Blob([UTF8ToString(jsonPtr)], {type: 'application/json'});
    var url = URL.createObjectURL(blob);
    var link = document.createElement('a');
    link.href = url; link.download = UTF8ToString(filenamePtr);
    document.body.appendChild(link); link.click(); link.remove();
    setTimeout(function() { URL.revokeObjectURL(url); }, 60000);
  },
  LF_AnalyticsStatus: function(textPtr, count) {
    var status = document.getElementById('lf-playtest-status');
    if (status) status.textContent = count + ' saved events. ' + UTF8ToString(textPtr);
    var download = document.getElementById('lf-playtest-download');
    if (download) download.disabled = count === 0;
  },
  LF_AnalyticsAttach: function(receiverPtr, endpointPtr, keyPtr, dashboardPtr) {
    var receiver = UTF8ToString(receiverPtr), endpoint = UTF8ToString(endpointPtr), key = UTF8ToString(keyPtr);
    var dashboard = UTF8ToString(dashboardPtr);
    if (window.lightfootedAnalyticsDetach) window.lightfootedAnalyticsDetach();
    var previous = document.getElementById('lf-playtest-panel');
    if (previous) previous.remove();
    var panel = document.createElement('section');
    panel.id = 'lf-playtest-panel'; panel.setAttribute('aria-label', 'Playtest data');
    panel.style.cssText = 'box-sizing:border-box;position:relative;z-index:2;max-width:960px;margin:16px auto;padding:16px 20px;background:#172532;color:#e3edf1;border:1px solid #415563;border-radius:8px;font:14px/1.5 system-ui,sans-serif;';
    var title = document.createElement('strong'); title.textContent = 'Playtest data';
    var help = document.createElement('p');
    help.textContent = 'Finish a level or return to level select, then download your data and import it into the graphs. Downloading during play pauses the game and includes the unfinished run.';
    help.style.margin = '6px 0 12px';
    var button = document.createElement('button');
    button.id = 'lf-playtest-download'; button.textContent = 'Download playtest data'; button.disabled = true;
    button.style.cssText = 'padding:10px 16px;margin-right:16px;background:#a8ead0;color:#0e1721;border:0;border-radius:4px;font:600 14px system-ui;cursor:pointer;';
    button.addEventListener('click', function() { SendMessage(receiver, 'BrowserExportRequested', ''); });
    var link = document.createElement('a'); link.textContent = 'Open playtest graphs';
    link.href = dashboard; link.target = '_blank'; link.rel = 'noopener'; link.style.color = '#a8ead0';
    var status = document.createElement('p'); status.id = 'lf-playtest-status'; status.setAttribute('role', 'status');
    status.textContent = 'Loading saved events…'; status.style.margin = '10px 0 0';
    panel.appendChild(title); panel.appendChild(help); panel.appendChild(button); panel.appendChild(link); panel.appendChild(status);
    document.body.appendChild(panel);
    // Put the panel after the standard Unity player without covering its canvas.
    var container = document.getElementById('unity-container');
    if (container && !container.classList.contains('unity-mobile')) {
      container.style.position = 'relative'; container.style.left = 'auto'; container.style.top = 'auto';
      container.style.transform = 'none'; container.style.margin = '24px auto 0';
      container.style.width = 'fit-content'; container.style.maxWidth = '100%';
      var footer = document.getElementById('unity-footer');
      if (footer) footer.style.display = 'flow-root';
    }
    var visibility = function() {
      SendMessage(receiver, 'BrowserVisibilityChanged', document.hidden ? 'hidden' : 'visible');
    };
    var leaving = function(event) {
      try { SendMessage(receiver, 'BrowserPageHide', event.persisted ? 'suspend' : 'quit'); } catch (error) { }
      if (endpoint) try {
        var saved = JSON.parse(window.localStorage.getItem(key) || '{"events":[]}');
        var events = saved.events.slice(0, 32);
        if (events.length && endpoint) {
          // A beacon has no receipt. Keep the queue; the next visit can safely retry these IDs.
          navigator.sendBeacon(endpoint, new Blob([JSON.stringify({events: events})], {type: 'text/plain;charset=UTF-8'}));
        }
      } catch (error) { }
    };
    document.addEventListener('visibilitychange', visibility);
    window.addEventListener('pagehide', leaving);
    window.lightfootedAnalyticsDetach = function() {
      document.removeEventListener('visibilitychange', visibility);
      window.removeEventListener('pagehide', leaving);
    };
  }
});
