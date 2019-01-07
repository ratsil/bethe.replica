var API = (function () {
	if (arguments.callee._singletonInstance)
		return arguments.callee._singletonInstance;
	arguments.callee._singletonInstance = this;

	var _ResponseCheck = function (o, mCallBacks) {
		if (mCallBacks && mCallBacks[o.sStatus]) {
			mCallBacks[o.sStatus]();
			return
		}
		switch (o.sStatus) {
			case 'error':
				if (o.oValue && (0 < o.oValue.indexOf('The system cannot find the file specified') || 0 < o.oValue.indexOf('no such file or directory')))
					return error();
				error({ sMessage: o.oValue });
				return false;
			case 'forbidden':
				error('Eventually your session has expired. You will be redirected to the front page.');
				API.Users.SignOut();
				return false;
			case 'success':
				return true;
		}
		return false; //UNSURE
	};

	return {
		oUser: null,

		Authorize: function (fCallback) {
			Request.Send('authorize', function (oResponse) {
				if (!_ResponseCheck(oResponse))
					return;
				if (!(API.oUser = oResponse.oValue)) {
					window.location.go('/');
					return;
				}
			});
		},
		Users: {
			SignIn: function (sUser, sPassword, fCallback) {
				Request.Send('signin', { user: sUser, password: sPassword }, function (oResponse) {
					if ('forbidden' == oResponse.sStatus)
						oResponse = { sStatus: 'error', oValue: 'wrong credentials' };
					if (!_ResponseCheck(oResponse)) {
						console.log(JSON.stringify(oResponse));
						return;
					}
					if (!oResponse.oValue)
						throw 'wrong response format';
					if (fCallback)
						fCallback();
				});
			},
			SignOut: function () {
				API.oUser = null;
				Request.Send('signout', null, false, function () {
					window.location.go('/');
				});
				return false;
			}
		},
		MAM: {
		    Assets: {
		        List: function (sVideoTypeFilter, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('AssetsGet', [sVideoTypeFilter], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Save: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback?fCallback():null;
		            Request.Send('AssetsSave', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Remove: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback?fCallback():null;
		            Request.Send('AssetsRemove', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        },
		        ParametersToPlaylistSave: function (id, fCallback) {
		            Request.Send('AssetParametersToPlaylistSave', [id], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        ParentAssign: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback?fCallback():null;
		            Request.Send('AssetsParentAssign', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        VideoTypeGet: function (id, fCallback) {
		            Request.Send('AssetVideoTypeGet', [id], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        AssetVideoTypeChange: function (id, idVideoTypes, fCallback) {
		            Request.Send('AssetVideoTypeChange', [id, idVideoTypes], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    },
		    Programs: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ProgramsGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Get: function (id, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ProgramGet', [id], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Save: function (o, fCallback) {
		            Request.Send('ProgramSave', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Clips: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ClipsGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Get: function (id, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ClipGet', [id], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Save: function (o, fCallback) {
		            Request.Send('ClipSave', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Advertisements: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('AdvertisementsGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Get: function (id, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('AdvertisementGet', [id], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Save: function (o, fCallback) {
		            Request.Send('AdvertisementSave', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Designs: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('DesignsGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Get: function (id, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('DesignGet', [id], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Save: function (o, fCallback) {
		            Request.Send('DesignSave', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Classes: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ClassesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Set: function (aAssets, fCallback) {
		            if (!aAssets || !aAssets.length)
		                return fCallback?fCallback():null;
		            Request.Send('ClassesSet', [aAssets], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Rotations: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('RotationsGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Set: function (aClips, fCallback) {
		            if (!aClips || !aClips.length)
		                return fCallback?fCallback():null;
		            Request.Send('RotationsSet', [aClips], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Statuses: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('StatusesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        ClearGet: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('StatusesClearGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    ChatInOuts: {
		        List: function (oAsset, fCallback) {
		            if (!oAsset)
		                return fCallback?fCallback():null;
		            Request.Send('ChatInOutsGet', [oAsset], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Save: function (oAsset, a, fCallback) {
		            if (!oAsset)
		                return fCallback?fCallback():null;
		            Request.Send('ChatInOutsSave', [oAsset, a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Ringtones: {
		        Add: function (oClip, nRTCode, fCallback) {
		            if (!oClip)
		                return fCallback?fCallback():null;
		            Request.Send('RingtoneAdd', [oClip, nRTCode], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    },
		    VideoTypes: {
		        List: function (fCallback) {
		            Request.Send('VideoTypesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Get: function (sType, a, fCallback) {
		            Request.Send('VideoTypeGet', [sType], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Persons: {
		        Artists: {
		            List: function (fCallback) {
		                if (!fCallback)
		                    return;
		                Request.Send('ArtistsGet', function (oResponse) {
		                    if (!_ResponseCheck(oResponse))
		                        return console.log(JSON.stringify(oResponse));
		                    fCallback(oResponse.oValue || []);
		                });
		            },
		            Load: function (idAssets, fCallback) {
		                if (!fCallback)
		                    return;
		                Request.Send('ArtistsLoad', [idAssets], function (oResponse) {
		                    if (!_ResponseCheck(oResponse))
		                        return console.log(JSON.stringify(oResponse));
		                    fCallback(oResponse.oValue || []);
		                });
		            },
		            CueNameGet: function (aPersonIDs, fCallback) {
		                if (!aPersonIDs)
		                    return fCallback?fCallback():null;
		                Request.Send('ArtistsCueNameGet', [aPersonIDs], false, function (oResponse) {
		                    if (!_ResponseCheck(oResponse))
		                        return console.log(JSON.stringify(oResponse));
		                    if (fCallback)
		                        fCallback(oResponse.oValue || []);
		                });
		            }
		        },
		        List: function (sPersonTypeFilter, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PersonsGet', [sPersonTypeFilter], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        TypeGet: function (sPersonTypeFilter, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PersonTypeGet', [sPersonTypeFilter], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Save: function (o, fCallback) {
		            if (!o)
		                return fCallback?fCallback():null;
		            Request.Send('PersonSave', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Remove: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback?fCallback():null;
		            Request.Send('PersonsRemove', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Styles: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('StylesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Load: function (idAssets, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('StylesLoad', [idAssets], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Palettes: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PalettesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Sexes: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('SexGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Sounds: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('SoundsGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Customs: {
		        Load: function (idAssets, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('CustomsLoad', [idAssets], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Set: function (aAssets, fCallback) {
		            if (!aAssets || !aAssets.length)
		                return fCallback?fCallback():null;
		            Request.Send('CustomsLoad', [aAssets], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    },
		    FilesAge: {
		        Get: function (oAsset, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FilesAgeGet', [oAsset], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Set: function (oAsset, nAge, fCallback) {
		            if (!oAsset)
		                return fCallback?fCallback():null;
		            Request.Send('FilesAgeSet', [oAsset, nAge], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    },
		    CuesTemplate: {
		        Show: function (sTemplateFile, fCallback) {
		            if (!sTemplateFile)
		                return fCallback?fCallback():null;
		            Request.Send('CuesTemplateShow', [sTemplateFile], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        },
		        Hide: function (sTemplateFile, fCallback) {
		            if (!sTemplateFile)
		                return fCallback?fCallback():null;
		            Request.Send('CuesTemplateHide', [sTemplateFile], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    }
		}
	}
})();
