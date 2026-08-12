mergeInto(LibraryManager.library, {
  LearnHearthstoneDownloadPng: function (data, length, fileNamePointer) {
    var fileName = UTF8ToString(fileNamePointer);
    var bytes = new Uint8Array(length);
    var index;
    for (index = 0; index < length; index += 1) {
      bytes[index] = HEAPU8[data + index];
    }

    var url = URL.createObjectURL(new Blob([bytes], { type: "image/png" }));
    var link = document.createElement("a");
    link.style.display = "none";
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.setTimeout(function () {
      URL.revokeObjectURL(url);
    }, 0);
  },
});
