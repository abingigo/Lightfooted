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
  LF_AnalyticsAttach: function(receiverPtr, endpointPtr, keyPtr) {
    var receiver = UTF8ToString(receiverPtr), endpoint = UTF8ToString(endpointPtr), key = UTF8ToString(keyPtr);
    if (window.lightfootedAnalyticsDetach) window.lightfootedAnalyticsDetach();
    var visibility = function() {
      SendMessage(receiver, 'BrowserVisibilityChanged', document.hidden ? 'hidden' : 'visible');
    };
    var leaving = function(event) {
      try { SendMessage(receiver, 'BrowserPageHide', event.persisted ? 'suspend' : 'quit'); } catch (error) { }
      try {
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
