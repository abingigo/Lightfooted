mergeInto(LibraryManager.library, {

  // Loads Google Analytics only when called (i.e. after the player consents).
  AnalyticsStart: function (idPtr) {
    var id = UTF8ToString(idPtr);
    if (window.__gaStarted) return;
    window.__gaStarted = true;

    window.dataLayer = window.dataLayer || [];
    window.gtag = function () { window.dataLayer.push(arguments); };

    var script = document.createElement('script');
    script.async = true;
    script.src = 'https://www.googletagmanager.com/gtag/js?id=' + encodeURIComponent(id);
    document.head.appendChild(script);

    window.gtag('js', new Date());
    window.gtag('config', id);
  },

  AnalyticsTrackEvent: function (namePtr, jsonPtr) {
    if (typeof window.gtag !== 'function') return;
    var name = UTF8ToString(namePtr);
    var params = {};
    try { params = JSON.parse(UTF8ToString(jsonPtr)); } catch (e) {}
    window.gtag('event', name, params);
  }

});
