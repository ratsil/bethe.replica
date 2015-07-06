function Area(nLeft, nTop, nWidth, nHeight) {
    this.nLeft = nLeft;
    this.nTop = nTop;
    this.nWidth = nWidth;
    this.nHeight = nHeight

    Area.prototype.toString = function () {
        return this.nLeft + "%0A" + this.nTop + "%0A" + this.nWidth + "%0A" + this.nHeight;
    }
}
function Color(nAlpha, nRed, nGreen, nBlue) {
    this.nAlpha = nAlpha;
    this.nRed = nRed;
    this.nGreen = nGreen;
    this.nBlue = nBlue;

    Color.prototype.toString = function () {
        return this.nAlpha + "%0A" + this.nRed + "%0A" + this.nGreen + "%0A" + this.nBlue;
    }
}
function Border(nWidth) {
    this.nWidth = nWidth;
    this.cColor = new Color(0, 0, 0, 0);

    Border.prototype.toString = function () {
        return this.nWidth + "%0A" + this.cColor.toString();
    }
}
function Font(sName, nSize) {
    this.sName = sName;
    this.nSize = nSize;
    this.cColor = new Color(0, 1, 1, 1);
    this.cBorder = new Border(1);

    Font.prototype.toString = function () {
        return this.sName + "%0A" + this.nSize + "%0A" + this.cColor.toString() + "%0A" + this.cBorder.toString();
    }
}
function Roll(sDirection) {
    this.sType = 'roll';
    this.oArea = new Area(0, 0, 300, 300);
    this.sDirection = sDirection;
    this.nSpeed = 25;

    this.toString = function () {
        return this.sType + '=' + this.oArea.toString() + '%0A' + this.sDirection + '%0A' + this.nSpeed;
    }
}

var _oLogo = new function () {
    this.sType = 'logo';
    this.oArea = new Area(10, 80, 119, 20);

    this.toString = function () {
        return this.sType + '=' + this.oArea.toString();
    }
}
var _oClock = new function () {
    this.sType = 'clock';
    this.oArea = new Area(10, 105, 166, 105);
    this.sFormat = "HH:MM:SS";
    this.sSuffix = "мск";
    this.cFont = new Font('Arial', 10);

    this.toString = function () {
        return this.sType + '=' + this.oArea.toString() + '%0A' + this.sFormat + '%0A' + this.sSuffix + '%0A' + this.cFont.toString();
    }
}
var _oVideo = new function () {
    this.sType = 'video';
    this.oArea = new Area(0, 0, 720, 576);

    this.toString = function () {
        return this.sType + '=' + this.oArea.toString();
    }
}
var _oPlaque = new function () {
    this.sType = 'plaque';
    this.oArea = new Area(0, 200, 720, 63);

    this.toString = function () {
        return this.sType + '=' + this.oArea.toString();
    }
}

var _oRoll = new Roll('up');
_oRoll.oArea.nLeft = 100;
_oRoll.oArea.nTop = 300;
_oRoll.oArea.nWidth = 520;
_oRoll.oArea.nHeight = 100;
var _oCrawl = new Roll('left');
_oCrawl.oArea.nLeft = 0;
_oCrawl.oArea.nTop = 0;
_oCrawl.oArea.nWidth = 720;
_oCrawl.oArea.nHeight = 576;

var _ui_scrGateway = null;

function init() {
    _oLogo.ui_btn = document.getElementById('_ui_btnLogo');
    _oClock.ui_btn = document.getElementById('_ui_btnClock');
    _oRoll.ui_btn = document.getElementById('_ui_btnRoll');
    _oCrawl.ui_btn = document.getElementById('_ui_btnCrawl');
    _oVideo.ui_btn = document.getElementById('_ui_btnVideo');
    _oPlaque.ui_btn = document.getElementById('_ui_btnPlaque');
}

function request(sRequest) {
    var sUrl = 'request.aspx?' + sRequest + '&cp=' + (new Date()).getTime();
    var ui_scr = document.createElement('script');
    ui_scr.src = sUrl;
    if (_ui_scrGateway)
        document.body.replaceChild(ui_scr, _ui_scrGateway);
    else
        document.body.appendChild(ui_scr);
    _ui_scrGateway = ui_scr;
}
function status_received() {
}

function roll_add(oEffect) {
    request(_oRoll.toString() + '&add=' + oEffect.sType +'&' + oEffect.toString());
}
function crawl_add(oEffect) {
    request(_oCrawl.toString() + '&add=' + oEffect.sType + '&' + oEffect.toString());
}

function effect_process(oEffect) {
    request(oEffect.toString());
    oEffect.ui_btn.disabled = true;
}
function effect_started(ui_btn) {
    ui_btn.innerHTML = 'ОСТАНОВИТЬ';
    ui_btn.disabled = false;
}
function effect_stopped() {
    ui_btn.innerHTML = 'ПОКАЗАТЬ';
    ui_btn.disabled = false;
}
function logo_started() {
    effect_started(_oLogo.ui_btn);
}
function logo_stopped() {
    effect_stopped(_oLogo.ui_btn);
}
function clock_started() {
    effect_started(_oClock.ui_btn);
}
function clock_stopped() {
    effect_stopped(_oClock.ui_btn);
}
function roll_started() {
    effect_started(_oRoll.ui_btn);
}
function roll_stopped() {
    effect_stopped(_oRoll.ui_btn);
}
function crawl_started() {
    effect_started(_oCrawl.ui_btn);
}
function crawl_stopped() {
    effect_stopped(_oCrawl.ui_btn);
}
function video_started() {
    effect_started(_oVideo.ui_btn);
}
function video_stopped() {
    effect_stopped(_oVideo.ui_btn);
}
function plaque_started() {
    effect_started(_oPlaque.ui_btn);
}
function plaque_stopped() {
    effect_stopped(_oPlaque.ui_btn);
}
