if (!XMLHttpRequest.prototype.sendAsBinary) {
    XMLHttpRequest.prototype.sendAsBinary = function (sData) {
        var nBytes = sData.length, aData = new Uint8Array(nBytes);
        for (var nIndx = 0; nIndx < nBytes; nIndx++)
            aData[nIndx] = sData.charCodeAt(nIndx) & 0xff;
        this.send(aData);
    };
}
function response(oEvent) {
    eval(oEvent.target.responseText);
}
function request(sUrl, aFiles) {
    var cXMLHttpRequest = new XMLHttpRequest();

    cXMLHttpRequest.onload = response;
    cXMLHttpRequest.open('post', sUrl, true);
    var sBoundary = "---------------------------" + Date.now().toString(16);
    cXMLHttpRequest.setRequestHeader("Content-Type", "multipart\/form-data; boundary=" + sBoundary);
    var sPostBody = "--" + sBoundary + "\r\n";
    if (aFiles && 0 < aFiles.length) {
        for (nIndx = 0; aFiles.length > nIndx; nIndx++) {
            sPostBody += "Content-Disposition: form-data; name=\"" + nIndx + "\"; filename=\"" + aFiles[nIndx].sName + "\"; size=" + aFiles[nIndx].aBytes.length + "\r\nContent-Type: " + aFiles[nIndx].sType + "\"\r\nContent-Length: " + aFiles[nIndx].aBytes.length + "\r\n\r\n";
            sPostBody += aFiles[nIndx].aBytes;
            sPostBody += "\r\n--" + sBoundary + "\r\n";
        }
    }
    sPostBody += "--" + sBoundary + "--\r\n";
    cXMLHttpRequest.sendAsBinary(sPostBody);
}