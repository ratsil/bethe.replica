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
		Ping: function (fCallback) {
		    Request.Send('Ping', function (oResponse) {
		        if (!_ResponseCheck(oResponse))
		            return console.log(JSON.stringify(oResponse));
		        if (fCallback)
		            fCallback(oResponse.oValue);
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
			ProfileGet: function (fCallback) {
			    if (!fCallback)
			        return;
			    Request.Send('ProfileGet', function (oResponse) {
			        if (!_ResponseCheck(oResponse))
			            return console.log(JSON.stringify(oResponse));
			        fCallback(oResponse.oValue);
			    });
			},
			WebPagesAccessGet: function (fCallback) {
			    if (!fCallback)
			        return;
			    Request.Send('WebPagesAccessGet', function (oResponse) {
			        if (!_ResponseCheck(oResponse))
			            return console.log(JSON.stringify(oResponse));
			        fCallback(oResponse.oValue || []);
			    });
			},
			AccessScopesGet: function (fCallback) {
			    if (!fCallback)
			        return;
			    Request.Send('AccessScopesGet', function (oResponse) {
			        if (!_ResponseCheck(oResponse))
			            return console.log(JSON.stringify(oResponse));
			        fCallback(oResponse.oValue || []);
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
		Upload: {
		    Begin: function (aBytes, fCallback) {
		        if (!fCallback)
		            return;
		        if (!aBytes || !aBytes.length)
		            return fCallback();
		        Request.Send('UploadFileBegin', [aBytes], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    },
		    Continue: function (nFileIndx, aBytes, fCallback) {
		        if (!fCallback)
		            return;
		        if (null == nFileIndx || !aBytes || !aBytes.length)
		            return fCallback();
		        Request.Send('UploadFileContinue', [nFileIndx, aBytes], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    },
		    End: function (nFileIndx, fCallback) {
		        if (!fCallback)
		            return;
		        if (null == nFileIndx)
		            return fCallback();
		        Request.Send('UploadFileEnd', [nFileIndx], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
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
		},
		Media: {
		    Storages: {
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('StoragesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Files: {
		        List: function (idStorages, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FilesGet', [idStorages], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        WithSourcesGet: function (idStorages, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FilesWithSourcesGet', [idStorages], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        AdditionalInfoGet: function (oFile, oRTStrings, oRTAssets, oRTDates, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FileAdditionalInfoGet', [oFile, oRTStrings, oRTAssets, oRTDates], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        IsInPlaylist: function (id, nMinutes, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FileCheckIsInPlaylist', [id, nMinutes], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        DurationQuery: function (id, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FileDurationQuery', [id], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        CommandStatusGet: function (idCommandsQueue, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('CommandStatusGet', [idCommandsQueue], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        FramesQtyGet: function (idCommandsQueue, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('FramesQtyGet', [idCommandsQueue], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        IDsInStockGet: function (a, fCallback) {
		            if (!fCallback)
		                return;
		            if (!a || !a.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('FileIDsInStockGet', [a], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Remove: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('FilesRemove', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    }
		},
		Ingest: {
		    TSRItemsGet: function (aFilenames, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('TSRItemsGet', [aFilenames], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    IsThereSameFile: function (sFilename, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('IsThereSameFile', [sFilename], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    AreThereSameFiles: function (aFilenames, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('AreThereSameFiles', [aFilenames], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    IsThereSameCustomValue: function (sName, sValue, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('IsThereSameCustomValue', [sName, sValue], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    AreThereSameCustomValues: function (sName, aValues, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('IsThereSameCustomValues', [sName, aValues], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    IngestForReplacedFile: function (oFile, fCallback) {
		        Request.Send('IngestForReplacedFile', [oFile], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    },
		    Ingest: function (oInfo, fCallback) {
		        Request.Send('Ingest', [oInfo], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    }
		},
		HK: {
		    RegisteredTablesList: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('RegisteredTablesGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    }
		},
		PL: {
		    Items: {
		        List: function (aStatuses, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaylistItemsGet', [aStatuses], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        ArchiveList: function (dtBegin, dtEnd, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaylistItemsArchGet', [dtBegin, dtEnd], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        PlannedList: function (dtBegin, dtEnd, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaylistItemsPlanGet', [dtBegin, dtEnd], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        AdvertisementsList: function (dtBegin, dtEnd, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaylistItemsAdvertsGet', [dtBegin, dtEnd], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        ComingUpGet: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ComingUpGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        MinimumForImmediatePLGet: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaylistItemMinimumForImmediatePLGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        AddResultGet: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaylistItemAdd_ResultGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        StartsSet: function (id, dtStartPlanned, dtOld, fCallback) {
		            Request.Send('PlaylistItemStartsSet', [id, dtStartPlanned, dtOld], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        WorkerAdd: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('PlaylistItemsAddWorker', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        },
		        PropertiesSet: function (o, fCallback) {
		            Request.Send('PLIPropertiesSet', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        TimingsSet: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('PlaylistItemsTimingsSet', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        ClassChange: function (id, aClasses, fCallback) {
		            if (!id)
		                return fCallback ? fCallback() : null;
		            Request.Send('PLIClassChange', [id, aClasses], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        DeleteSince: function (dtBegin, fCallback) {
		            Request.Send('PlaylistItemsDeleteSince', [dtBegin], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Delete: function (a, fCallback) {
		            if (!a || !a.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('PlaylistItemsDelete', [a], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Advance: {
		        Items: {
		            Save: function (o, fCallback) {
		                Request.Send('AdvancedPlaylistItemSave', [o], false, function (oResponse) {
		                    if (!_ResponseCheck(oResponse))
		                        return console.log(JSON.stringify(oResponse));
		                    if (fCallback)
		                        fCallback(oResponse.oValue);
		                });
		            }
		        },
		        List: function (dtBegin, dtEnd, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('AdvancedPlaylistsGet', [dtBegin, dtEnd], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Get: function (o, fCallback) {
		            if (!fCallback)
		                return;
		            if (!o)
		                return fCallback();
		            Request.Send('AdvancedPlaylistGet', [o], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        AddReplace: function (o, fCallback) {
		            Request.Send('AdvancedPlaylistAddReplace', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Start: function (o, fCallback) {
		            if (!o)
		                return fCallback?fCallback():null;
		            Request.Send('AdvancedPlaylistStart', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Rename: function (o, fCallback) {
		            if (!o)
		                return fCallback ? fCallback() : null;
		            Request.Send('AdvancedPlaylistRename', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Delete: function (o, fCallback) {
		            if (!o)
		                return fCallback ? fCallback() : null;
		            Request.Send('AdvancedPlaylistDelete', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Import: {
		        ImportLogGet: function (id, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ImportLogGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        PowerGoldFileParse: function (sFile, fCallback) {
		            if (!sFile)
		                return fCallback ? fCallback() : null;
		            Request.Send('PowerGoldFileParse', [sFile], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        VideoInternationalFileParse: function (sFile, fCallback) {
		            if (!sFile)
		                return fCallback ? fCallback() : null;
		            Request.Send('VideoInternationalFileParse', [sFile], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        DesignFileParse: function (sFile, fCallback) {
		            if (!sFile)
		                return fCallback ? fCallback() : null;
		            Request.Send('DesignFileParse', [sFile], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        PlaylistsMerge: function (nPGAssetsHandle, nVIAssetsHandle, dtAdvertisementBind, nDesignAssetsHandle, fCallback) {
		            Request.Send('PlaylistsMerge', [nPGAssetsHandle, nVIAssetsHandle, dtAdvertisementBind, nDesignAssetsHandle], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    LastElementGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('PlaylistLastElementGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    IsUpdated: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('IsPlaylistUpdated', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    NearestAdvertsBlock: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('NearestAdvertsBlock', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    BeforeAddCheckRange: function (dtBegin, dtEnd, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('BeforeAddCheckRange', [dtBegin, dtEnd], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    Insert: function (aAssets, oPLIPreceding, fCallback) {
		        if (!aAssets || !aAssets.length)
		            return fCallback ? fCallback() : null;
		        Request.Send('PlaylistInsert', [aAssets, oPLIPreceding], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue || []);
		        });
		    },
		    InsertCopies: function (aAssets, oPLIPreceding, nCopiesQty, fCallback) {
		        if (!aAssets || !aAssets.length)
		            return fCallback ? fCallback() : null;
		        Request.Send('PlaylistInsertCopies', [aAssets, oPLIPreceding, nCopiesQty], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue || []);
		        });
		    },
		    InsertInBlock: function (aPLIsToAdd, aPLIsToMove, fCallback) {
		        Request.Send('InsertInBlock', [aPLIsToAdd, aPLIsToMove], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    },
		    GroupMoving: function (aPLIs, fCallback) {
		        if (!aPLIs || !aPLIs.length)
		            return fCallback ? fCallback() : null;
		        Request.Send('GroupMoving', [aPLIs], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    },
		    PLICurrentSkip: function (id, fCallback) {
		        if (!id)
		            return fCallback ? fCallback() : null;
		        Request.Send('PLICurrentSkip', [id], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    },
		    RecalculateQuery: function (idPlaylistItems, nHoursQty, fCallback) {
		        Request.Send('PlaylistRecalculateQuery', [idPlaylistItems, nHoursQty], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    }
		},
		Templates: {
		    Messages: {
		        Texts: {
		            List: function (aMessages, fCallback) {
		                if (!fCallback)
		                    return;
		                if (!aMessages || !aMessages.length)
		                    return fCallback();
		                Request.Send('TemplateMessagesTextGet', [aMessages], function (oResponse) {
		                    if (!_ResponseCheck(oResponse))
		                        return console.log(JSON.stringify(oResponse));
		                    fCallback(oResponse.oValue || []);
		                });
		            },
		            Save: function (aDict, fCallback) {
		                if (!aDict || !aDict.length)
		                    return fCallback ? fCallback() : null;
		                Request.Send('TemplateMessagesTextSave', [aDict], false, function (oResponse) {
		                    if (!_ResponseCheck(oResponse))
		                        return console.log(JSON.stringify(oResponse));
		                    if (fCallback)
		                        fCallback(oResponse.oValue);
		                });
		            }
		        },
		        List: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('TempateMessagesGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        }
		    },
		    Schedule: {
		        List: function (aTemplateBinds, dtBegin, fCallback) {
		            if (!fCallback)
		                return;
		            if (!aTemplateBinds || !aTemplateBinds.length)
		                return fCallback();
		            Request.Send('TemplatesScheduleGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Add: function (aTemplatesSchedule, fCallback) {
		            if (!aTemplatesSchedule || !aTemplatesSchedule.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('TemplatesScheduleAdd', [aTemplatesSchedule], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Delete: function (aTemplatesSchedule, fCallback) {
		            if (!aTemplatesSchedule || !aTemplatesSchedule.length)
		                return fCallback ? fCallback() : null;
		            Request.Send('TemplatesScheduleDelete', [aTemplatesSchedule], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    RegisteredTableGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('TemplateRegisteredTableGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    MacrosCrawlsGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('MacrosCrawlsGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    BindsTrailsGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('TemplateBindsTrailsGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    DirectoriesTrailsGet: function (sPath, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('DirectoriesTrailsGet', [sPath], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    MacrosValuesSet: function (a, fCallback) {
		        if (!a || !a.length)
		            return fCallback ? fCallback() : null;
		        Request.Send('MacrosValuesSet', [a], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback(oResponse.oValue);
		        });
		    }
		},
		SCR: {
		    Shifts: {
		        CurrentGet: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('ShiftCurrentGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Add: function (oPreset, sSubject, fCallback) {
		            Request.Send('ShiftAdd', [oPreset, sSubject], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Start: function (o, fCallback) {
		            Request.Send('ShiftStart', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Stop: function (o, fCallback) {
		            Request.Send('ShiftStop', [o], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Messages: {
		        QueueGet: function (fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('MessagesQueueGet', function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Mark: function (id, fCallback) {
		            if (null == id)
		                return fCallback ? fCallback() : null;
		            Request.Send('MessageMark', [id], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        },
		        UnMark: function (id, fCallback) {
		            if (null == id)
		                return fCallback ? fCallback() : null;
		            Request.Send('MessageUnMark', [id], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback();
		            });
		        }
		    },
		    Plaques: {
		        List: function (oPreset, fCallback) {
		            if (!fCallback)
		                return;
		            Request.Send('PlaquesGet', [oPreset], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue || []);
		            });
		        },
		        Add: function (oPlaque, fCallback) {
		            if (!oPlaque)
		                return fCallback ? fCallback() : null;
		            Request.Send('PlaqueAdd', [oPlaque], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Change: function (oPlaque, fCallback) {
		            if (!oPlaque)
		                return fCallback ? fCallback() : null;
		            Request.Send('PlaqueChange', [oPlaque], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        },
		        Delete: function (oPlaque, fCallback) {
		            if (!oPlaque)
		                return fCallback ? fCallback() : null;
		            Request.Send('PlaqueDelete', [oPlaque], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    AnnouncementsActualGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('AnnouncementsActualGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    TimeBlockGet: function (dt, bForward, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('TimeBlockGet', [dt, bForward], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    StorageSCRGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('StorageSCRGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    CuesGet: function (idAssets, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('CuesGet', [idAssets], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    LogoBindingGet: function (aPLIs, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('LogoBindingGet', [aPLIs], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    ClipsBDLog: function (idShifts, aPLIs, fCallback) {
		        Request.Send('ClipsBDLog', [idShifts, aPLIs], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    },
		    PLFragmentGet: function (dtBegin, dtEnd, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('PLFragmentGet', [dtBegin, dtEnd], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    }
		},
		Stat: {
		    Export: {
		        ResultGet: function (nWorkerInfoID, fCallback) {
		            if (!fCallback)
		                return;
		            if (null == nWorkerInfoID)
		                return fCallback();
		            Request.Send('ExportResultGet', [nWorkerInfoID], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        WorkerProgressGet: function (nWorkerInfoID, fCallback) {
		            if (!fCallback)
		                return;
		            if (null == nWorkerInfoID)
		                return fCallback();
		            Request.Send('WorkerProgressGet', [nWorkerInfoID], function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                fCallback(oResponse.oValue);
		            });
		        },
		        Do: function (sTemplate, oFilters, fCallback) {
		            Request.Send('Export', [sTemplate, oFilters], false, function (oResponse) {
		                if (!_ResponseCheck(oResponse))
		                    return console.log(JSON.stringify(oResponse));
		                if (fCallback)
		                    fCallback(oResponse.oValue);
		            });
		        }
		    },
		    Get: function (oFilters, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('StatGet', [oFilters], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    MessagesGet: function (oFilters, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('MessagesGet', [oFilters], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    }
		},
		RT: {
		    BindsGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('RingtonesBindsGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    }
		},
		UI: {
		    FrequencyOfOccurrence: function (idVideoTypes, fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('FrequencyOfOccurrence', [idVideoTypes], function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    }
		},
		Grid: {
		    Get: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('GridGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    Save: function (sXML, fCallback) {
		        if (!sXML)
		            return fCallback ? fCallback() : null;
		        Request.Send('GridSave', [sXML], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    }
		},
		Errors: {
		    Log: function (sError, fCallback) {
		        Request.Send('ErrorLogging', [sError], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    },
		    IsThereAny: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('IsThereAnyErrors', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    },
		    Clear: function (fCallback) {
		        Request.Send('ErrorsClear', false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    },
		    List: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('ErrorsGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    ListAll: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('ErrorsAllGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue || []);
		        });
		    },
		    LastGet: function (fCallback) {
		        if (!fCallback)
		            return;
		        Request.Send('ErrorLastGet', function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            fCallback(oResponse.oValue);
		        });
		    }
		},
		Logger: {
		    Notice: function (sFrom, sText, fCallback) {
		        Request.Send('Logger_Notice', [sFrom, sText], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    },
		    Error: function (sFrom, sText, fCallback) {
		        Request.Send('Logger_Error', [sFrom, sText], false, function (oResponse) {
		            if (!_ResponseCheck(oResponse))
		                return console.log(JSON.stringify(oResponse));
		            if (fCallback)
		                fCallback();
		        });
		    }
		},
		TransliterationGet: function (fCallback) {
		    if (!fCallback)
		        return;
		    Request.Send('TransliterationGet', function (oResponse) {
		        if (!_ResponseCheck(oResponse))
		            return console.log(JSON.stringify(oResponse));
		        fCallback(oResponse.oValue || []);
		    });
		},
		DateTimeNowGet: function (fCallback) {
		    if (!fCallback)
		        return;
		    Request.Send('DateTimeNowGet', function (oResponse) {
		        if (!_ResponseCheck(oResponse))
		            return console.log(JSON.stringify(oResponse));
		        fCallback(oResponse.oValue);
		    });
		}
	}
})();
