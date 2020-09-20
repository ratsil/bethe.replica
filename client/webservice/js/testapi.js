if (!API.Tests) {
	API.Tests = {};
	API.Tests.Console = function (s, a, o, b) {
		s = '--- API.' + s + 'fCallback) ---';

		if ((!b && null == o) || (null != o && 'System.NotImplementedException' == o.ClassName)) {
			console.log('Failed!' + s + ':');
			if (null != a) {
				a = ['P:'].concat(a);
			} else
				a = ['P:-']
			a.push('R:', o);
			console.log(a);
		} else {
			console.log('OK! ' + s);
			//console.log(o);
		}
	}


	API.Tests.Invasive = function () {

		a = [{}]
		API.MAM.Assets.Save(a[0], o => {
			API.Tests.Console('MAM.Assets.Save(a, ', a, o);
		});
		a = [{}]
		API.MAM.Assets.Remove(a, o => {
			API.Tests.Console('MAM.Assets.Remove(a, ', a, o);
		});
		a = [0]
		API.MAM.Assets.ParametersToPlaylistSave(a[0], o => {
			API.Tests.Console('MAM.Assets.ParametersToPlaylistSave(id, ', a, o);
		});
		a = [{}];
		API.MAM.Assets.ParentAssign(a[0], o => {
			API.Tests.Console('MAM.Assets.ParentAssign(a, ', a, o);
		});
		a = [1, 1];
		API.MAM.Assets.AssetVideoTypeChange(a[0], a[1], o => {
			API.Tests.Console('MAM.Assets.AssetVideoTypeChange(id, idVideoTypes, ', a, o);
		});

		a = [{}];
		API.MAM.Programs.Save(a[0], o => {
			API.Tests.Console('MAM.Programs.Save(o, ', a, o);
		});

		a = [{}];
		API.MAM.Clips.Save(a[0], o => {
			API.Tests.Console('MAM.Clips.Save(o, ', a, o);
		});

		a = [{}];
		API.MAM.Advertisements.Save(a[0], o => {
			API.Tests.Console('MAM.Advertisements.Save(o, ', a, o);
		});

		a = [{}];
		API.MAM.Designs.Save(a[0], o => {
			API.Tests.Console('MAM.Designs.Save(o, ', a, o);
		});

		a = [[{}]];
		API.MAM.Classes.Set(a[0], o => {
			API.Tests.Console('MAM.Classes.Set(aAssets, ', a, o);
		});

		a = [[{}]];
		API.MAM.Rotations.Set(a[0], o => {
			API.Tests.Console('MAM.Rotations.Set(aClips, ', a, o);
		});

		API.MAM.Statuses.ClearGet(o => {
			API.Tests.Console('MAM.Statuses.ClearGet(', null, o);
		});

		a = [{}, [{}]];
		API.MAM.ChatInOuts.Save(a[0], a[1], o => {
			API.Tests.Console('MAM.ChatInOuts.Save(oAsset, a, ', a, o);
		});

		a = [{}, 0];
		API.MAM.Ringtones.Add(a[0], a[1], o => {
			API.Tests.Console('MAM.Ringtones.Add(oClip, nRTCode, ', a, o);
		});

		a = [{}];
		API.MAM.Persons.Save(a[0], o => {
			API.Tests.Console('MAM.Persons.Save(o, ', a, o);
		});
		a = [{}];
		API.MAM.Persons.Remove(a, o => {
			API.Tests.Console('MAM.Persons.Remove(o, ', a, o);
		})


		a = [[{}]];
		API.MAM.Customs.Set(a[0], o => {
			API.Tests.Console('MAM.Customs.Set(aAssets, ', a, o);
		});

		a = [{}, 1];
		API.MAM.FilesAge.Set(a[0], a[1], o => {
			API.Tests.Console('MAM.FilesAge.Set(oAsset, nAge, ', a, o);
		});

		a = [''];
		API.MAM.CuesTemplate.Show(a[0], o => {
			API.Tests.Console('MAM.CuesTemplate.Show(sTemplateFile, ', a, o);
		});
		a = [''];
		API.MAM.CuesTemplate.Hide(a[0], o => {
			API.Tests.Console('MAM.CuesTemplate.Hide(sTemplateFile, ', a, o);
		});

		a = [[{}]];
		API.Media.Files.Remove(a[0], o => {
			API.Tests.Console('Media.Files.Remove(a, ', a, o);
		});

		a = [{}];
		API.Ingest.IngestForReplacedFile(a[0], o => {
			API.Tests.Console('Ingest.IngestForReplacedFile(oFile, ', a, o);
		});
		a = [{}];
		API.Ingest.Ingest(a[0], o => {
			API.Tests.Console('Ingest.Ingest(oInfo, ', a, o);
		});

		a = [1, new Date(), new Date()];
		API.PL.Items.StartsSet(a[0], a[1], a[2], o => {
			API.Tests.Console('PL.Items.StartsSet(id, dtStartPlanned, dtOld, ', a, o);
		});
		a = [[{}]];//PlaylistItem[]
		API.PL.Items.WorkerAdd(a[0], o => {
			API.Tests.Console('PL.Items.WorkerAdd(a, ', a, o);
		});
		a = [{}];//PlaylistItem
		API.PL.Items.PropertiesSet(a[0], o => {
			API.Tests.Console('PL.Items.PropertiesSet(o, ', a, o);
		});
		a = [[{}]];//PlaylistItem[]
		API.PL.Items.TimingsSet(a[0], o => {
			API.Tests.Console('PL.Items.TimingsSet(a, ', a, o);
		});
		a = [1, [{}]];
		API.PL.Items.ClassChange(a[0], a[1], o => {
			API.Tests.Console('PL.Items.ClassChange(id, aClasses, ', a, o);
		});
		a = [new Date()];
		API.PL.Items.DeleteSince(a[0], o => {
			API.Tests.Console('PL.Items.DeleteSince(dtBegin, ', a, o);
		});
		a = [[{}]];//IdNamePair[]
		API.PL.Items.Delete(a[0], o => {
			API.Tests.Console('PL.Items.Delete(a, ', a, o);
		});


		a = [{}];//cues.plugins.PlaylistItem
		API.PL.Advance.Items.Save(a[0], o => {
			API.Tests.Console('PL.Advance.Items.Save(o, ', a, o);
		});

		a = [{}];//cues.plugins.Playlist
		API.PL.Advance.AddReplace(a[0], o => {
			API.Tests.Console('PL.Advance.AddReplace(o, ', a, o);
		});
		a = [{}];//cues.plugins.Playlist
		API.PL.Advance.Start(a[0], o => {
			API.Tests.Console('PL.Advance.Start(o, ', a, o);
		});
		a = [{}];//cues.plugins.Playlist
		API.PL.Advance.Rename(a[0], o => {
			API.Tests.Console('PL.Advance.Rename(o, ', a, o);
		});
		a = [{}];//cues.plugins.Playlist
		API.PL.Advance.Delete(a[0], o => {
			API.Tests.Console('PL.Advance.Delete(o, ', a, o);
		});

		a = [''];
		API.PL.Import.PowerGoldFileParse(a[0], o => {
			API.Tests.Console('PL.Import.PowerGoldFileParse(sFile, ', a, o);
		});
		a = [''];
		API.PL.Import.VideoInternationalFileParse(a[0], o => {
			API.Tests.Console('PL.Import.VideoInternationalFileParse(sFile, ', a, o);
		});
		a = [''];
		API.PL.Import.DesignFileParse(a[0], o => {
			API.Tests.Console('PL.Import.DesignFileParse(sFile, ', a, o);
		});
		a = [1, 1, new Date(), 1];
		API.PL.Import.PlaylistsMerge(a[0], a[1], a[2], a[3], o => {
			API.Tests.Console('PL.Import.PlaylistsMerge(nPGAssetsHandle, nVIAssetsHandle, dtAdvertisementBind, nDesignAssetsHandle, ', a, o);
		});

		a = [[{}], {}];
		API.PL.Insert(a[0], a[1], o => {
			API.Tests.Console('PL.Insert(aAssets, oPLIPreceding, ', a, o);
		});
		a = [[{}], {}, 1];
		API.PL.InsertCopies(a[0], a[1], a[2], o => {
			API.Tests.Console('PL.InsertCopies(aAssets, oPLIPreceding, nCopiesQty, ', a, o);
		});
		a = [[{}], [{}]];
		API.PL.InsertInBlock(a[0], a[1], o => {
			API.Tests.Console('PL.InsertInBlock(aPLIsToAdd, aPLIsToMove, ', a, o);
		});
		a = [[{}]];
		API.PL.GroupMoving(a[0], o => {
			API.Tests.Console('PL.GroupMoving(aPLIs, ', a, o);
		});
		a = [1];
		API.PL.PLICurrentSkip(a[0], o => {
			API.Tests.Console('PL.PLICurrentSkip(id, ', a, o);
		});
		a = [1, 1];
		API.PL.RecalculateQuery(a[0], a[1], o => {
			API.Tests.Console('PL.RecalculateQuery(idPlaylistItems, nHoursQty, ', a, o);
		});


		a = [[{}]];
		API.Templates.Messages.Texts.Save(a[0], o => {
			API.Tests.Console('Templates.Messages.Texts.Save(aDict, ', a, o);
		});

		a = [[{}]];
		API.Templates.Schedule.Add(a[0], o => {
			API.Tests.Console('Templates.Schedule.Add(aTemplatesSchedule, ', a, o);
		});
		a = [[{}]];
		API.Templates.Schedule.Delete(a[0], o => {
			API.Tests.Console('Templates.Schedule.Delete(aTemplatesSchedule, ', a, o);
		});

		a = [[{}]];
		API.Templates.MacrosValuesSet(a[0], o => {
			API.Tests.Console('Templates.MacrosValuesSet(a, ', a, o);
		});

		a = [{}, ''];
		API.SCR.Shifts.Add(a[0], a[1], o => {
			API.Tests.Console('SCR.Shifts.Add(oPreset, sSubject, ', a, o);
		});
		a = [{}];
		API.SCR.Shifts.Start(a[0], o => {
			API.Tests.Console('SCR.Shifts.Start(o, ', a, o);
		});
		a = [{}];
		API.SCR.Shifts.Stop(a[0], o => {
			API.Tests.Console('SCR.Shifts.Stop(o, ', a, o);
		});

		a = [1];
		API.SCR.Messages.Mark(a[0], o => {
			API.Tests.Console('SCR.Messages.Mark(id, ', a, o);
		});
		a = [1];
		API.SCR.Messages.UnMark(a[0], o => {
			API.Tests.Console('SCR.Messages.UnMark(id, ', a, o);
		});

		a = [{}];
		API.SCR.Plaques.Add(a[0], o => {
			API.Tests.Console('SCR.Plaques.Add(oPlaque, ', a, o);
		});
		a = [{}];
		API.SCR.Plaques.Change(a[0], o => {
			API.Tests.Console('SCR.Plaques.Change(oPlaque, ', a, o);
		});
		a = [{}];
		API.SCR.Plaques.Delete(a[0], o => {
			API.Tests.Console('SCR.Plaques.Delete(oPlaque, ', a, o);
		});

		a = [1, [{}]];
		API.SCR.ClipsBDLog(a[0], a[1], o => {
			API.Tests.Console('SCR.ClipsBDLog(idShifts, aPLIs, ', a, o);
		});

		a = ['', {}];
		API.Stat.Export.Do(a[0], a[1], o => {
			API.Tests.Console('Stat.Export.Do(sTemplate, oFilters, ', a, o);
		});

		a = [''];
		API.Grid.Save(a[0], o => {
			API.Tests.Console('Grid.Save(sXML, ', a, o);
		});

		a = [''];
		API.Errors.Log(a[0], o => {
			API.Tests.Console('Errors.Log(sError, ', a, o);
		});
		API.Errors.Clear(o => {
			API.Tests.Console('Errors.Clear(', null, o);
		});


		a = ['', ''];
		API.Logger.Notice(a[0], a[1], o => {
			API.Tests.Console('Logger.Notice(sFrom, sText, ', a, o);
		});
		a = ['', ''];
		API.Logger.Error(a[0], a[1], o => {
			API.Tests.Console('Logger.Error(sFrom, sText, ', a, o);
		});
		
		a = [0];
		API.Upload.Begin(a[0], o => {
			API.Tests.Console('Upload.Begin(aBytes, ', a, o);
		});
		a = [1, [0]];
		API.Upload.Continue(a[0], a[1], o => {
			API.Tests.Console('Upload.Continue(nFileIndx, aBytes, ', a, o);
		});
		a = [1];
		API.Upload.End(a[0], o => {
			API.Tests.Console('Upload.End(nFileIndx, ', a, o);
		});
	}

	API.Tests.ReadOnly = function () {
		API.Authorize(o => {
			API.Tests.Console('Authorize(', null, true);
		});
		API.Ping(o => {
			API.Tests.Console('Ping(', null, o);
		})

		a = ['usertest', 'testtest'];
		API.Users.SignIn(a[0], a[1], o => {
			API.Tests.Console('Users.SignIn(sUser, sPassword, ', a, o);
		});
		API.Users.ProfileGet(o => {
			API.Tests.Console('Users.ProfileGet(', null, o);
		});
		API.Users.AccessScopesGet(o => {
			API.Tests.Console('Users.AccessScopesGet(', null, o);
		});
		console.log('Not Tested: API.Users.SignOut');

		a = [''];
		API.MAM.Assets.List(a[0], o => {
			API.Tests.Console('MAM.Assets.List(sVideoTypeFilter, ', a, o);
		});
		a = [[1]];
		API.MAM.Assets.VideoTypeGet(a[0], o => {
			API.Tests.Console('MAM.Assets.VideoTypeGet(a, ', a, o);
		});

		API.MAM.Programs.List(o => {
			API.Tests.Console('MAM.Programs.List(', null, o);
		});
		a = [1];
		API.MAM.Programs.Get(a[0], o => {
			API.Tests.Console('MAM.Programs.Get(id, ', a, o, true);
		});

		API.MAM.Clips.List(o => {
			API.Tests.Console('MAM.Clips.List(', null, o);
		});
		a = [1];
		API.MAM.Clips.Get(a[0], o => {
			API.Tests.Console('MAM.Clips.Get(id, ', a, o, true);
		});

		API.MAM.Advertisements.List(o => {
			API.Tests.Console('MAM.Advertisements.List(', null, o);
		});
		a = [1];
		API.MAM.Advertisements.Get(a[0], o => {
			API.Tests.Console('MAM.Advertisements.Get(id, ', a, o, true);
		});

		API.MAM.Designs.List(o => {
			API.Tests.Console('MAM.Designs.List(', null, o);
		});
		a = [1];
		API.MAM.Designs.Get(a[0], o => {
			API.Tests.Console('MAM.Designs.Get(id, ', a, o, true);
		});

		API.MAM.Classes.List(o => {
			API.Tests.Console('MAM.Classes.List(', null, o);
		});

		API.MAM.Rotations.List(o => {
			API.Tests.Console('MAM.Rotations.List(', null, o);
		});

		API.MAM.Statuses.List(o => {
			API.Tests.Console('MAM.Statuses.List(', null, o);
		});

		a = [{}];
		API.MAM.ChatInOuts.List(a[0], o => {
			API.Tests.Console('MAM.ChatInOuts.List(oAsset, ', a, o);
		});

		API.MAM.VideoTypes.List(o => {
			API.Tests.Console('MAM.VideoTypes.List(', null, o);
		});
		a = [''];
		API.MAM.VideoTypes.Get(a[0], o => {
			API.Tests.Console('MAM.VideoTypes.Get(sType, ', a, o, true);
		});

		API.MAM.Persons.Artists.List(o => {
			API.Tests.Console('MAM.Persons.Artists.List(', null, o);
		});
		a = [1];
		API.MAM.Persons.Artists.Load(a[0], o => {
			API.Tests.Console('MAM.Persons.Artists.Load(idAssets, ', a, o);
		});
		a = [[1]];
		API.MAM.Persons.Artists.CueNameGet(a[0], o => {
			API.Tests.Console('MAM.Persons.Artists.CueNameGet(aPersonIDs, ', a, o);
		});

		a = [''];
		API.MAM.Persons.List(a[0], o => {
			API.Tests.Console('MAM.Persons.List(sPersonTypeFilter, ', a, o);
		});
		a = [''];
		API.MAM.Persons.TypeGet(a[0], o => {
			API.Tests.Console('MAM.Persons.TypeGet(sPersonTypeFilter, ', a, o, true);
		});

		API.MAM.Styles.List(o => {
			API.Tests.Console('MAM.Styles.List(', null, o);
		});
		a = [1];
		API.MAM.Styles.Load(a[0], o => {
			API.Tests.Console('MAM.Styles.Load(idAssets, ', a, o);
		});

		API.MAM.Palettes.List(o => {
			API.Tests.Console('MAM.Palettes.List(', null, o);
		});

		API.MAM.Sexes.List(o => {
			API.Tests.Console('MAM.Sexes.List(', null, o);
		});

		API.MAM.Sounds.List(o => {
			API.Tests.Console('MAM.Sounds.List(', null, o);
		});

		a = [1];
		API.MAM.Customs.Load(a[0], o => {
			API.Tests.Console('MAM.Customs.Load(idAssets, ', a, o);
		});

		a = [{}];
		API.MAM.FilesAge.Get(a[0], o => {
			API.Tests.Console('MAM.FilesAge.Get(oAsset, ', a, o);
		});

		API.Media.Storages.List(o => {
			API.Tests.Console('Media.Storages.List(', null, o);
		});

		a = [1];
		API.Media.Files.List(a[0], o => {
			API.Tests.Console('Media.Files.List(idStorages, ', a, o);
		});
		a = [1];
		API.Media.Files.WithSourcesGet(a[0], o => {
			API.Tests.Console('Media.Files.WithSourcesGet(idStorages, ', a, o);
		});
		a = [{}, {}, {}, {}];
		API.Media.Files.AdditionalInfoGet(a[0], a[1], a[2], a[3], o => {
			API.Tests.Console('Media.Files.AdditionalInfoGet(oFile, oRTStrings, oRTAssets, oRTDates, ', a, o);
		});
		a = [1, 1];
		API.Media.Files.IsInPlaylist(a[0], a[1], o => {
			API.Tests.Console('Media.Files.IsInPlaylist(id, nMinutes, ', a, o);
		});
		a = [1];
		API.Media.Files.DurationQuery(a[0], o => {
			API.Tests.Console('Media.Files.DurationQuery(id, ', a, o);
		});
		a = [1];
		API.Media.Files.CommandStatusGet(a[0], o => {
			API.Tests.Console('Media.Files.CommandStatusGet(idCommandsQueue, ', a, o);
		});
		a = [1];
		API.Media.Files.FramesQtyGet(a[0], o => {
			API.Tests.Console('Media.Files.FramesQtyGet(idCommandsQueue, ', a, o);
		});
		a = [[1]];
		API.Media.Files.IDsInStockGet(a[0], o => {
			API.Tests.Console('Media.Files.IDsInStockGet(a, ', a, o);
		});

		a = [['']];
		API.Ingest.TSRItemsGet(a[0], o => {
			API.Tests.Console('Ingest.TSRItemsGet(aFilenames, ', a, o);
		});
		a = [''];
		API.Ingest.IsThereSameFile(a[0], o => {
			API.Tests.Console('Ingest.IsThereSameFile(sFilename, ', a, o);
		});
		a = [['']];
		API.Ingest.AreThereSameFiles(a[0], o => {
			API.Tests.Console('Ingest.AreThereSameFiles(aFilenames, ', a, o);
		});
		a = ['', ''];
		API.Ingest.IsThereSameCustomValue(a[0], a[1], o => {
			API.Tests.Console('Ingest.IsThereSameCustomValue(sName, sValue, ', a, o, true);
		});
		a = ['', ['']];
		API.Ingest.AreThereSameCustomValues(a[0], a[1], o => {
			API.Tests.Console('Ingest.AreThereSameCustomValues(sName, aValues, ', a, o);
		});

		API.HK.RegisteredTablesList(o => {
			API.Tests.Console('HK.RegisteredTablesList(', null, o);
		});

		a = [[{}]];
		API.PL.Items.List(a[0], o => {
			API.Tests.Console('PL.Items.List(aStatuses, ', a, o);
		});
		a = [new Date(), new Date()];
		API.PL.Items.ArchiveList(a[0], a[1], o => {
			API.Tests.Console('PL.Items.ArchiveList(dtBegin, dtEnd, ', a, o);
		});
		a = [new Date(), new Date()];
		API.PL.Items.PlannedList(a[0], a[1], o => {
			API.Tests.Console('PL.Items.PlannedList(dtBegin, dtEnd, ', a, o);
		});
		a = [new Date(), new Date()];
		API.PL.Items.AdvertisementsList(a[0], a[1], o => {
			API.Tests.Console('PL.Items.AdvertisementsList(dtBegin, dtEnd, ', a, o);
		})
		API.PL.Items.ComingUpGet(o => {
			API.Tests.Console('PL.Items.ComingUpGet(', null, o);
		});
		API.PL.Items.MinimumForImmediatePLGet(o => {
			API.Tests.Console('PL.Items.MinimumForImmediatePLGet(', null, o, true);
		});
		API.PL.Items.AddResultGet(o => {
			API.Tests.Console('PL.Items.AddResultGet(', null, o);
		});

		a = [new Date(), new Date()];
		API.PL.Advance.List(a[0], a[1], o => {
			API.Tests.Console('PL.Advance.List(dtBegin, dtEnd, ', a, o);
		});
		a = [{}];//cues.plugins.Playlist
		API.PL.Advance.Get(a[0], o => {
			API.Tests.Console('PL.Advance.Get(o, ', a, o);
		});

		API.PL.Import.ImportLogGet(o => {
			API.Tests.Console('PL.Import.ImportLogGet(', null, o, true);
		});

		API.PL.LastElementGet(o => {
			API.Tests.Console('PL.LastElementGet(', null, o);
		});
		API.PL.IsUpdated(o => {
			API.Tests.Console('PL.IsUpdated(', null, o);
		});
		API.PL.NearestAdvertsBlock(o => {
			API.Tests.Console('PL.NearestAdvertsBlock(', null, o);
		});
		a = [new Date(), new Date()];
		API.PL.BeforeAddCheckRange(a[0], a[1], o => {
			API.Tests.Console('PL.BeforeAddCheckRange(dtBegin, dtEnd, ', a, o);
		});



		a = [[{}]];
		API.Templates.Messages.Texts.List(a[0], o => {
			API.Tests.Console('Templates.Messages.Texts.List(aMessages, ', a, o);
		});

		API.Templates.Messages.List(o => {
			API.Tests.Console('Templates.Messages.List(', null, o);
		});

		a = [[{}], new Date()];
		API.Templates.Schedule.List(a[0], a[1], o => {
			API.Tests.Console('Templates.Schedule.List(aTemplateBinds, dtBegin, ', a, o);
		});

		API.Templates.RegisteredTableGet(o => {
			API.Tests.Console('Templates.RegisteredTableGet(', null, o);
		});
		API.Templates.MacrosCrawlsGet(o => {
			API.Tests.Console('Templates.MacrosCrawlsGet(', null, o);
		});
		API.Templates.BindsTrailsGet(o => {
			API.Tests.Console('Templates.BindsTrailsGet(', null, o);
		});
		a = [''];
		API.Templates.DirectoriesTrailsGet(a[0], o => {
			API.Tests.Console('Templates.DirectoriesTrailsGet(sPath, ', a, o);
		});

		API.SCR.Shifts.CurrentGet(o => {
			API.Tests.Console('SCR.Shifts.CurrentGet(', null, o, true);
		});

		API.SCR.Messages.QueueGet(o => {
			API.Tests.Console('SCR.Messages.QueueGet(', null, o);
		});

		a = [{}];
		API.SCR.Plaques.List(a[0], o => {
			API.Tests.Console('SCR.Plaques.List(oPreset, ', a, o);
		});

		API.SCR.AnnouncementsActualGet(o => {
			API.Tests.Console('SCR.AnnouncementsActualGet(', null, o);
		});
		a = [new Date(), true];
		API.SCR.TimeBlockGet(a[0], a[1], o => {
			API.Tests.Console('SCR.TimeBlockGet(dt, bForward, ', a, o);
		});
		API.SCR.StorageSCRGet(o => {
			API.Tests.Console('SCR.StorageSCRGet(', null, o);
		});
		a = [1];
		API.SCR.CuesGet(a[0], o => {
			API.Tests.Console('SCR.CuesGet(idAssets, ', a, o);
		});
		a = [[{}]];
		API.SCR.LogoBindingGet(a[0], o => {
			API.Tests.Console('SCR.LogoBindingGet(aPLIs, ', a, o);
		});
		a = [new Date(), new Date()];
		API.SCR.PLFragmentGet(a[0], a[1], o => {
			API.Tests.Console('SCR.PLFragmentGet(dtBegin, dtEnd, ', a, o);
		});

		a = [1];
		API.Stat.Export.WorkerProgressGet(a[0], o => {
			API.Tests.Console('Stat.Export.WorkerProgressGet(nWorkerInfoID, ', a, o);
		});
		a = [1];
		API.Stat.Export.ResultGet(a[0], o => {
			API.Tests.Console('Stat.Export.ResultGet(nWorkerInfoID, ', a, o);
		});

		a = [{}];
		API.Stat.Get(a[0], o => {
			API.Tests.Console('Stat.Get(oFilters, ', a, o);
		});
		a = [{}];
		API.Stat.MessagesGet(a[0], o => {
			API.Tests.Console('Stat.MessagesGet(oFilters, ', a, o);
		});

		a = [1];
		API.UI.FrequencyOfOccurrence(a[0], o => {
			API.Tests.Console('UI.FrequencyOfOccurrence(idVideoTypes, ', a, o);
		});

		API.Grid.Get(o => {
			API.Tests.Console('Grid.Get(', null, o);
		});
		API.Errors.IsThereAny(o => {
			API.Tests.Console('Errors.IsThereAny(', null, o);
		});
		API.Errors.List(o => {
			API.Tests.Console('Errors.List(', null, o);
		});
		API.Errors.ListAll(o => {
			API.Tests.Console('Errors.ListAll(', null, o);
		});
		API.Errors.LastGet(o => {
			API.Tests.Console('Errors.LastGet(', null, o, true);
		});


		API.TransliterationGet(o => {
			API.Tests.Console('TransliterationGet(', null, o);
		});
		API.DateTimeNowGet(o => {
			API.Tests.Console('DateTimeNowGet(', null, o);
		});
	}

}


$(document).ready(function () {

	API.Tests.ReadOnly();

	return;

	aStatuses = [{ nID: 1, sName: "Запланировано" },
	{ nID: 2, sName: "Поставлено в очередь" },
	{ nID: 3, sName: "Подготовлено" },
	{ nID: 4, sName: "В эфире" },
	{ nID: 5, sName: "Проиграно" },
	{ nID: 6, sName: "Пропущено" },
	{ nID: 7, sName: "С ошибкой" }];

	dtBegin = new Date('01.01.2020');
	dtEnd = new Date();

	API.MAM.Statuses.List(aStat => {
		console.log('---------------------------------------------------------------------')
		if (Array.isArray(aStat)) {
			console.log('API.MAM.Statuses.List - работает! Возвращает:');
			console.log(aStat);
		}
		else {
			console.log('API.MAM.Statuses.List - не работает! :(');
		}
	})

	API.PL.Items.List(aStatuses, o => {
		console.log('---------------------------------------------------------------------')
		if (o.Data != null) {
			console.log('API.PL.Items.List(aStatuses, fCallback) - работает! Возвращает:');
			console.log('Аргумент:')
			console.log(aStatuses);
			console.log(o.Data);
			console.log(o.Message);
		}
		else {
			console.log('API.PL.Items.List(aStatuses, fCallback) - НЕ работает! Возвращает:');
			console.log('Аргумент:')
			console.log(aStatuses);
			console.log(o.Data);
			console.log(o.Message);
		}
	})

	// ArchiveList: function(dtBegin, dtEnd, fCallback)
	API.PL.Items.ArchiveList(dtBegin, dtEnd, o => {
		console.log('---------------------------------------------------------------------')
		if (Array.isArray(o)) {
			console.log('API.PL.Items.ArchiveList(dtBegin, dtEnd, fCallback) - работает! Возвращает:');
			console.log('Аргументы: dtBegin: ' + dtBegin + ' dtEnd: ' + dtEnd)
			console.log(o);
		}
		else {
			console.log('API.PL.Items.ArchiveList(dtBegin, dtEnd, fCallback) - НЕ работает! Возвращает:');
			console.log('Аргументы: dtBegin: ' + dtBegin + ' dtEnd: ' + dtEnd)
			console.log(o.Data);
			console.log(o.Message);
		}
	})

	// PlannedList: function(dtBegin, dtEnd, fCallback) {
	API.PL.Items.PlannedList(dtBegin, dtEnd, o => {
		console.log('---------------------------------------------------------------------')
		if (o.Data != null) {
			console.log('API.PL.Items.PlannedList(dtBegin, dtEnd, fCallback) - работает! Возвращает:');
			console.log('Аргументы: dtBegin: ' + dtBegin + ' dtEnd: ' + dtEnd)
			console.log(o.Data);
			console.log(o.Message);
		}
		else {
			console.log('API.PL.Items.PlannedList(dtBegin, dtEnd, fCallback) - НЕ работает! Возвращает:');
			console.log('Аргументы: dtBegin: ' + dtBegin + ' dtEnd: ' + dtEnd)
			console.log(o.Data);
			console.log(o.Message);
		}
	})

	// AdvertisementsList: function(dtBegin, dtEnd, fCallback)
	API.PL.Items.AdvertisementsList(dtBegin, dtEnd, o => {
		console.log('---------------------------------------------------------------------')
		if (o.Data != null) {
			console.log('API.PL.Items.AdvertisementsList(dtBegin, dtEnd, fCallback) - работает! Возвращает:');
			console.log('Аргументы: dtBegin: ' + dtBegin + ' dtEnd: ' + dtEnd)
			console.log(o.Data);
			console.log(o.Message);
		}
		else {
			console.log('API.PL.Items.AdvertisementsList(dtBegin, dtEnd, fCallback) - НЕ работает! Возвращает:');
			console.log('Аргументы: dtBegin: ' + dtBegin + ' dtEnd: ' + dtEnd)
			console.log(o.Data);
			console.log(o.Message);
		}
	})

	// ComingUpGet: function(fCallback)
	API.PL.Items.ComingUpGet(o => {
		console.log('---------------------------------------------------------------------')
		if (o != null) {
			console.log('API.PL.Items.ComingUpGet(fCallback) - работает! Возвращает:');
			// console.log(o.Data);
			// console.log(o.Message);
		}
		else {
			console.log('API.PL.Items.ComingUpGet(fCallback) - НЕ работает! Возвращает:');
			// console.log(o.Data);
			// console.log(o.Message);
		}
	})

	// MinimumForImmediatePLGet: function(fCallback)
	API.PL.Items.MinimumForImmediatePLGet(o => {
		console.log('---------------------------------------------------------------------')
		if (o != null && o.sName != null) {
			console.log('API.PL.Items.MinimumForImmediatePLGet(fCallback) - работает! Возвращает:');
			console.log(o.sName);
			console.log(o.nFramesQty);
		}
		else if (null != o) {
			console.log('API.PL.Items.MinimumForImmediatePLGet(fCallback) - НЕ работает! Возвращает:');
			console.log(o.sName);
			console.log(o.nFramesQty);
		}
		else
			console.log('API.PL.Items.MinimumForImmediatePLGet(fCallback) - НЕ работает! Возвращает null');
	})

	//  AddResultGet: function(fCallback)
	API.PL.Items.AddResultGet(o => {
		console.log('---------------------------------------------------------------------')
		if (o.Data != null) {
			console.log('API.PL.Items.AddResultGet(fCallback) - работает! Возвращает:');
			console.log(o.Data);
			console.log(o.Message);
		}
		else {
			console.log('API.PL.Items.AddResultGet(fCallback) - НЕ работает! Возвращает:');
			console.log(o.Data);
			console.log(o.Message);
		}
	})

})