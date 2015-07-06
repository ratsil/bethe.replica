var _ui_dvProgress = null;
var _ui_dvAuthorization = null;
var _ui_dvNavigation = null;
var _ui_dvSocial = null;
var _ui_dvPreferences = null;

function init() {
    social.init();
    _ui_dvProgress = document.getElementById('_ui_dvProgress');
    _ui_dvAuthorization = document.getElementById('_ui_dvAuthorization');
    _ui_dvNavigation = document.getElementById('_ui_dvNavigation');
    _ui_dvSocial = document.getElementById('_ui_dvSocial');
    _ui_dvPreferences = document.getElementById('_ui_dvPreferences');

    hide_all();
    navigation_show();
    _ui_dvNavigation.style.display = 'block';
    _ui_dvSocial.style.display = 'block';
}
function hide_all() {
    _ui_dvProgress.style.display = 'none';
    _ui_dvAuthorization.style.display = 'none';
    _ui_dvNavigation.style.display = 'none';
    _ui_dvSocial.style.display = 'none';
    _ui_dvPreferences.style.display = 'none';
}

function navigation_show() {
    _ui_dvNavigation.style.display = 'none';
    var aNavigationTargets = _ui_dvNavigation.getElementsByTagName('span');
    if (!aNavigationTargets)
        return;
    for (var nIndx = 0; aNavigationTargets.length > nIndx; nIndx++) {
        if(aNavigationTargets[nIndx].getAttribute('target')) {
            aNavigationTargets[nIndx].onclick = function() { navigate(document.getElementById(this.getAttribute('target'))); }
            aNavigationTargets[nIndx].innerHTML = aNavigationTargets[nIndx].getAttribute('caption');
            aNavigationTargets[nIndx].className = 'navigation';
            aNavigationTargets[nIndx].style.display = 'inline';
        }
        else
            aNavigationTargets[nIndx].style.display = 'none';
    }
}
function navigate(ui_target) {
    if(!ui_target)
        return;
    ui_target.style.display = 'block';
}
function File(oFile)
{
    if(!oFile || !oFile.name || !oFile.type || !oFile.aBytes)
        return null;
    this.sName = oFile.name;
    this.sType = oFile.type;
    this.aBytes = oFile.aBytes;
}
function publish() {
    if (!_ui_dvSocial.uiFiles) {
        _ui_dvSocial.uiMessage = document.getElementById('_ui_taSocialMessage');
        _ui_dvSocial.uiFiles = document.getElementById('_ui_flSocialFiles');
    }
    var aFiles = null;
    if (_ui_dvSocial.uiFiles && _ui_dvSocial.uiFiles.files && _ui_dvSocial.uiFiles.files[0]) {
        if (!_ui_dvSocial.uiFiles.files[0].aBytes) {
            var cReader = new FileReader();
            cReader.onloadend = function (evt) {
                _ui_dvSocial.uiFiles.files[0].aBytes = evt.target.result;
                publish();
            }
            cReader.onerror = function (evt) {
                error(evt);
            }
            _ui_dvSocial.uiFiles.files[0].aBytes = null;
            cReader.readAsBinaryString(_ui_dvSocial.uiFiles.files[0]);
            return;
        }
        aFiles = [new File(_ui_dvSocial.uiFiles.files[0])];
    }
    request('request.aspx?social&scope=vk&type=publish&sMessage=' + _ui_dvSocial.uiMessage.value, aFiles);
}

