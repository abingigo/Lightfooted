mergeInto(LibraryManager.library, {

  // Posts an already-encoded form body to a Google Form.
  // 'no-cors' means the request is sent but the reply can't be read, which is
  // fine here: we only need Google to receive it.
  FormSubmit: function (urlPtr, bodyPtr) {
    var url = UTF8ToString(urlPtr);
    var body = UTF8ToString(bodyPtr);
    try {
      fetch(url, {
        method: 'POST',
        mode: 'no-cors',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: body
      }).catch(function () {});
    } catch (e) {}
  }

});
