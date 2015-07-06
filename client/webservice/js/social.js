(function (social, undefined) {
    var _ui_wAuthorizaton = null;
    var _tAuthorizaton = null;
    var _cAuthorizatonUrlPrevious = null;
    var _nAuthorizatonTimeout = 0;
    var _bBack = false;

    social.scope = {
        fb: "fb",
        tt: "tt",
        vk: "vk"
    },

    social.init = function () {
        VK.init({
            apiId: 3930961
        });
        request('request.aspx?social&scope=vk&type=status');
        return;
        request('request.aspx?social&scope=tt&type=status');
        window.fbAsyncInit = function () {
            FB.init({
                appId: '666660383352186', // App ID
                status: true, // check login status
                cookie: true, // enable cookies to allow the server to access the session
                xfbml: true  // parse XFBML
            });

            FB.Event.subscribe('auth.authResponseChange', function (response) {
                if (response.status === 'connected') {
                    request('request.aspx?social&scope=fb&type=at&value=' + response.authResponse.accessToken);
                    social.status(social.scope.fb, true);
                } else {
                    social.status(social.scope.fb, false);
                    if (response.status === 'not_authorized') {
                        alert('the user is logged in to Facebook, but has not authenticated pubme');
                    } else {
                        alert('the user isn`t logged in to Facebook.');
                    }
                }
            });
        };

        (function (d) {
            var js, id = 'facebook-jssdk', ref = d.getElementsByTagName('script')[0];
            if (d.getElementById(id)) { return; }
            js = d.createElement('script'); js.id = id; js.async = true;
            js.src = "//connect.facebook.net/en_US/all.js";
            ref.parentNode.insertBefore(js, ref);
        } (document));
    };
    social.log = function (msg) {
        setTimeout(function () {
            throw new Error(msg);
        }, 0);
    }
    social.auth = function (eScope) {
        window.showModalDialog('request.aspx?social&scope=' + eScope + '&type=login', window, 'center:on,dialogwidth:620,dialogheight:580,resizable:yes,scrollbars:yes');
        request('request.aspx?social&scope=' + eScope + '&type=status');
    }
    social.status = function (eScope, bAuthorized) {
        document.getElementById('_ui_b' + eScope.toUpperCase() + 'Login').getElementsByTagName('hr')[0].className = bAuthorized ? 'green' : 'red';
    }
} (window.social = window.social || {}));