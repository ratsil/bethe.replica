if (!window.location.go) {
	window.location.go = function (sURL) {
		if (sURL)
			window.location.assign(sURL);
		else
			window.location.reload();
	};
}
if (!Date.prototype.toUnix) {
	Date.prototype.toUnix = function () {
		return this / 1000 | 0;
	}
}
if (!Date.toUnix) {
	Date.toUnix = function (dt) {
		if ('string' == typeof dt)
			dt = (new Date(dt));
		return dt.toUnix();
	}
}

var Cookies = (function () {
	if (arguments.callee._singletonInstance)
		return arguments.callee._singletonInstance;
	arguments.callee._singletonInstance = this;

	return {
		Set: function (name, value, days, domain) {
			var expires;

			if (days) {
				var date = new Date();
				date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
				expires = "; expires=" + date.toGMTString();
			} else {
				expires = "";
			}
			if (domain)
				domain = ';domain=' + domain;
			else
				domain = '';
			document.cookie = encodeURIComponent(name) + "=" + encodeURIComponent(value) + expires + "; path=/" + domain;
		},
		Get: function (name) {
			var nameEQ = encodeURIComponent(name) + "=";
			var ca = document.cookie.split(';');
			for (var i = 0; i < ca.length; i++) {
				var c = ca[i];
				while (c.charAt(0) === ' ') c = c.substring(1, c.length);
				if (c.indexOf(nameEQ) === 0) return decodeURIComponent(c.substring(nameEQ.length, c.length));
			}
			return null;
		},
		Delete: function (name) {
			Cookies.Set(name, "", -1);
		}
	};
})();

var Loader = (function () {
	if (arguments.callee._singletonInstance)
		return arguments.callee._singletonInstance;
	arguments.callee._singletonInstance = this;
	var _ui = null, _n=0;

	return {
		Show: function () {
			_n++;
			if (_ui)
				return;
			Templates.Attach('spinner', function (ui) {
				if(1 > _n)
					return ui.remove();
				(_ui = ui).modal({
					backdrop: 'static',
					keyboard: false
				});
			});
		},
		Hide: function (bForce) {
			if (!bForce && 1 < _n)
				return _n--;
			_ui.next('.modal-backdrop').remove();
			_ui.remove();
			$('body').removeClass('modal-open');
			_n = 0;
			_ui = null;
		}
	};
})();

var Request = (function () {
	if (arguments.callee._singletonInstance)
		return arguments.callee._singletonInstance;
	arguments.callee._singletonInstance = this;

	var _sURL = '/api.aspx?';
	var _bLoader = false;

	return {
		Loader: function (b) {
			_bLoader = b;
		},
		Send: function (sRequest, oValues, bIdempotent, fCallback) {
			if ('function' == typeof bIdempotent) {
				fCallback = bIdempotent;
				bIdempotent = true;
			} else if ('function' == typeof oValues) {
				fCallback = oValues;
				bIdempotent = true;
				oValues = null;
			}
			if (_bLoader)
				Loader.Show();
			var fSuccess = function (o) {
				if (_bLoader)
					Loader.Hide();
				if (fCallback) {
					var oResult = { sStatus: 'success' };
					if (o)
						oResult.oValue = o;
					fCallback(oResult);
				}
			};
			var fError = function (jqXHR, textStatus, errorThrown) {
				if (_bLoader)
					Loader.Hide();
				if (fCallback) {
					var oResult = {};
					switch (jqXHR.status) {
						case 200:
							if (jqXHR.responseText.length) {
								jqXHR.status = 500;
								fError(jqXHR, textStatus, errorThrown);
							} else
								fSuccess();
							break;
						case 401:
							oResult.sStatus = 'forbidden';
							break;
						default:
							oResult.sStatus = 'error';
							if (jqXHR.responseText)
								oResult.oValue = jqXHR.responseText;
							break;
					}
					fCallback(oResult);
				}
			};
			$.ajax({
				xhrFields: { withCredentials: true },
				type: bIdempotent ? 'GET' : 'POST',
				url: _sURL + sRequest,
				data: oValues,
				dataType: "json",
				success: fSuccess,
				error: fError
			});
		}
	};
})();

var Templates = (function () {
	if (arguments.callee._singletonInstance)
		return arguments.callee._singletonInstance;
	arguments.callee._singletonInstance = this;
	var _mTemplates = {};
	return {
		Load: function (oPath, fCallback) {
			if (oPath.forEach) {
				if (0 < oPath.length) {
					Templates.Load(oPath.shift(), function () {
						Templates.Load(oPath, fCallback);
					});
					return;
				}
				if (fCallback)
					fCallback();
				return;
			}
			var sPath = ~oPath.indexOf('.html') ? oPath : oPath + '/content.html';
			if (!_mTemplates[sPath]) {
				$.get('/templates/' + sPath, function (sUI) {
					_mTemplates[sPath] = sUI;
					if (fCallback)
						fCallback(sUI);
				});
				return;
			}
			if (fCallback)
				fCallback(_mTemplates[sPath]);
			return _mTemplates[sPath];
		},
		Attach: function (sPath, aMeta, ui, fCallback) {
			if ('function' == typeof aMeta) {
				fCallback = aMeta;
				aMeta = null;
			} else if ('function' == typeof ui) {
				fCallback = ui;
				if (aMeta && aMeta instanceof jQuery) {
					ui = aMeta;
					aMeta = null;
				} else {
					ui = null;
				}
			} else if (aMeta && (aMeta instanceof jQuery || (!ui && !fCallback))) {
				ui = aMeta;
				aMeta = null;
			}
			if (!ui)
				ui = $('body');
			this.Load(sPath, function (sTemplate) {
				if (aMeta) {
					aMeta.forEach(function (o) {
						sTemplate = sTemplate.replace(new RegExp('{{' + o.sKey + '}}', 'g'), o.sValue);
					});
				}
				ui = $(sTemplate).appendTo(ui);
				if (fCallback)
					fCallback(ui);
			});
			return ui;
		}
	};
})();

function message(o, fCallBack) {
	if (!$('body').is(':visible')) {
		console.log(o);
		return;
	}
	Templates.Attach('message.html', function (ui) {
		var oDefault = { sTitle: 'Error', sMessage: 'Sorry, something went wrong...' };
		if ('string' === typeof o)
			o = { sMessage: o };
		else if ('object' !== typeof o)
			o = {};

		o = $.extend({}, oDefault, o);
		ui.find('.modal-title').html(o.sTitle);
		ui.find('.modal-message').html(o.sMessage);
		if (o.bYes || o.bNo) {
			var uiFooter = ui.find('.modal-footer').removeClass('d-none');
			if (o.bYes)
				uiFooter.find('[s=yes]').click(function () { fCallBack && fCallBack(true) }).removeClass('d-none');
			if (o.bNo)
				uiFooter.find('[s=no]').click(function () { fCallBack && fCallBack(false) }).removeClass('d-none');
		}
		ui.modal();
});
}
function error(o) {
	message(o);
}
function question(o, fCallBack) {
	if ('string' === typeof o)
		o = { sMessage: o };
	else if ('object' !== typeof o)
		o = {};
	message($.extend({}, { sTitle: 'Question', bYes: true, bNo: true }, o), fCallBack);
}
function getUrlParameter(sParam) {
	var sPageURL = decodeURIComponent(window.location.search.substring(1)),
		sURLVariables = sPageURL.split('&'),
		sParameterName,
		i;

	for (i = 0; i < sURLVariables.length; i++) {
		sParameterName = sURLVariables[i].split('=');

		if (sParameterName[0] === sParam) {
			return sParameterName[1] === undefined ? true : sParameterName[1];
		}
	}
};