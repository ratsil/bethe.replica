----------------------------------- hk."tAccessRoles"
	SELECT hk."fAccessRoleAdd"('replica_access');
	SELECT hk."fAccessRoleAdd"('replica_ingest');
	SELECT hk."fAccessRoleAdd"('replica_ingest_full');
	SELECT hk."fAccessRoleAdd"('replica_scr');
	SELECT hk."fAccessRoleAdd"('replica_scr_full');
	SELECT hk."fAccessRoleAdd"('replica_adm');
	SELECT hk."fAccessRoleAdd"('replica_adm_full');
	SELECT hk."fAccessRoleAdd"('replica_playlist');
	SELECT hk."fAccessRoleAdd"('replica_playlist_full');
	SELECT hk."fAccessRoleAdd"('replica_assets');
	SELECT hk."fAccessRoleAdd"('replica_assets_full');
	SELECT hk."fAccessRoleAdd"('replica_programs');
	SELECT hk."fAccessRoleAdd"('replica_programs_full');
	SELECT hk."fAccessRoleAdd"('replica_stat');
	SELECT hk."fAccessRoleAdd"('replica_rt');
	SELECT hk."fAccessRoleAdd"('replica_rt_full');
	SELECT hk."fAccessRoleAdd"('replica_ia_full');
	SELECT hk."fAccessRoleAdd"('replica_grid');
	SELECT hk."fAccessRoleAdd"('replica_grid_full');
	SELECT hk."fAccessRoleAdd"('replica_templates');
	SELECT hk."fAccessRoleAdd"('replica_templates_full');

	SELECT hk."fUserAccessRoleAdd"('replica_ia', 'replica_scr');
	SELECT hk."fUserAccessRoleAdd"('replica_ia', 'replica_ia_full');
----------------------------------- hk."tRegisteredTables"
	SELECT hk."fRegisteredTableAdd"('hk', 'tRegisteredTables');
	SELECT hk."fRegisteredTableAdd"('hk', 'tErrorScopes');
	SELECT hk."fRegisteredTableAdd"('hk', 'tHouseKeeping');
	SELECT hk."fRegisteredTableAdd"('hk', 'tErrors');
	SELECT hk."fRegisteredTableAdd"('hk', 'tDTEventTypes');
	SELECT hk."fRegisteredTableAdd"('hk', 'tDTEvents');
	SELECT hk."fRegisteredTableAdd"('hk', 'tUsers');
	SELECT hk."fRegisteredTableAdd"('hk', 'tUserAttributes');
	SELECT hk."fRegisteredTableAdd"('hk', 'tWebPages');
	SELECT hk."fRegisteredTableAdd"('hk', 'tAccessRoles');
	SELECT hk."fRegisteredTableAdd"('hk', 'tAccessPermissions');

	SELECT hk."fRegisteredTableAdd"('media', 'tStrings');
	SELECT hk."fRegisteredTableAdd"('media', 'tStorageTypes');
	SELECT hk."fRegisteredTableAdd"('media', 'tStorages');
	SELECT hk."fRegisteredTableAdd"('media', 'tFiles');

	SELECT hk."fRegisteredTableAdd"('pl', 'tClasses');
	SELECT hk."fRegisteredTableAdd"('pl', 'tItems');
	SELECT hk."fRegisteredTableAdd"('pl', 'tItemAttributes');
	SELECT hk."fRegisteredTableAdd"('pl', 'tItemDTEvents');

	SELECT hk."fRegisteredTableAdd"('logs', 'tPlaylistLog');
	SELECT hk."fRegisteredTableAdd"('logs', 'tPlaylistLogAttributes');

	SELECT hk."fRegisteredTableAdd"('mam', 'tAssets');
	SELECT hk."fRegisteredTableAdd"('mam', 'tAssetTypes');
	SELECT hk."fRegisteredTableAdd"('mam', 'tAssetAttributes');
	SELECT hk."fRegisteredTableAdd"('mam', 'tVideoTypes');
	SELECT hk."fRegisteredTableAdd"('mam', 'tVideos');
	SELECT hk."fRegisteredTableAdd"('mam', 'tPersonTypes');
	SELECT hk."fRegisteredTableAdd"('mam', 'tPersons');
	SELECT hk."fRegisteredTableAdd"('mam', 'tAlbums');
	SELECT hk."fRegisteredTableAdd"('mam', 'tCues');
	SELECT hk."fRegisteredTableAdd"('mam', 'tCategories');
	SELECT hk."fRegisteredTableAdd"('mam', 'tCategoryValues');
	SELECT hk."fRegisteredTableAdd"('mam', 'tRules');
	SELECT hk."fRegisteredTableAdd"('mam', 'tRuleBinds');
	SELECT hk."fRegisteredTableAdd"('mam', 'tTimeMapTypes');
	SELECT hk."fRegisteredTableAdd"('mam', 'tTimeMaps');
	SELECT hk."fRegisteredTableAdd"('mam', 'tTimeMapBinds');
	SELECT hk."fRegisteredTableAdd"('mam', 'tCustomValues');

	SELECT hk."fRegisteredTableAdd"('grid', 'tNumericValues');
	SELECT hk."fRegisteredTableAdd"('grid', 'tStringValues');

	SELECT hk."fRegisteredTableAdd"('cues', 'tTemplates');
	SELECT hk."fRegisteredTableAdd"('cues', 'tClassAndTemplateBinds');
	SELECT hk."fRegisteredTableAdd"('cues', 'tTemplatesSchedule');
	SELECT hk."fRegisteredTableAdd"('cues', 'tDictionary');
	SELECT hk."fRegisteredTableAdd"('cues', 'tBindTypes');
	SELECT hk."fRegisteredTableAdd"('cues', 'tBinds');
	SELECT hk."fRegisteredTableAdd"('cues', 'tStrings');
	SELECT hk."fRegisteredTableAdd"('cues', 'tTimestamps');
	SELECT hk."fRegisteredTableAdd"('cues', 'tPlugins');

	SELECT hk."fRegisteredTableAdd"('ia', 'tMessages');
	SELECT hk."fRegisteredTableAdd"('ia', 'tBinds');
	SELECT hk."fRegisteredTableAdd"('ia', 'tGateways');
	SELECT hk."fRegisteredTableAdd"('ia', 'tGatewayIPs');
	SELECT hk."fRegisteredTableAdd"('ia', 'tDTEvents');
	SELECT hk."fRegisteredTableAdd"('ia', 'tTexts');
	SELECT hk."fRegisteredTableAdd"('ia', 'tNumbers');
	SELECT hk."fRegisteredTableAdd"('ia', 'tImages');
----------------------------------- hk."tAccessScopes"
	SELECT hk."fAccessScopeAdd"('ingest', true);
	SELECT hk."fAccessScopeAdd"('playlist', true);
	SELECT hk."fAccessScopeAdd"('assets', true);
	SELECT hk."fAccessScopeAdd"('stat', true);
	SELECT hk."fAccessScopeAdd"('rt', true);
	SELECT hk."fAccessScopeAdd"('programs', true);
	SELECT hk."fAccessScopeAdd"('templates', true);
	SELECT hk."fAccessScopeAdd"('grid', true);

	SELECT hk."fAccessScopeAdd"('assets.name', true);
	SELECT hk."fAccessScopeAdd"('assets.classes', true);
	SELECT hk."fAccessScopeAdd"('assets.file', true);
	SELECT hk."fAccessScopeAdd"('assets.custom_values', true);

	SELECT hk."fAccessScopeAdd"('clips.name', true);
	SELECT hk."fAccessScopeAdd"('clips.classes', true);
	SELECT hk."fAccessScopeAdd"('clips.file', true);
	SELECT hk."fAccessScopeAdd"('clips.custom_values', true);
	SELECT hk."fAccessScopeAdd"('clips.artists', true);
	SELECT hk."fAccessScopeAdd"('clips.rotations', true);
	SELECT hk."fAccessScopeAdd"('clips.sound', true);
	SELECT hk."fAccessScopeAdd"('clips.palette', true);

	SELECT hk."fAccessScopeAdd"('programs.name', true);
	SELECT hk."fAccessScopeAdd"('programs.classes', true);
	SELECT hk."fAccessScopeAdd"('programs.file', true);
	SELECT hk."fAccessScopeAdd"('programs.custom_values', true);
	SELECT hk."fAccessScopeAdd"('programs.clips', true);
	SELECT hk."fAccessScopeAdd"('programs.chatinouts', true);

	SELECT hk."fAccessScopeAdd"('advertisements.name', true);
	SELECT hk."fAccessScopeAdd"('advertisements.classes', true);
	SELECT hk."fAccessScopeAdd"('advertisements.file', true);
	SELECT hk."fAccessScopeAdd"('advertisements.custom_values', true);

	SELECT hk."fAccessScopeAdd"('designs.name', true);
	SELECT hk."fAccessScopeAdd"('designs.classes', true);
	SELECT hk."fAccessScopeAdd"('designs.file', true);
	SELECT hk."fAccessScopeAdd"('designs.custom_values', true);

	SELECT hk."fAccessScopeAdd"('templates.schedule', true);

	--список нужно сильно дополнять. необходимо помнить, что запрещено все, что не разрешено

----------------------------------- hk."tUsers"
	-- see in db_init_hk_tables.sql
	SELECT hk."fUserAdd"('replica_client','');
	SELECT hk."fUserAdd"('replica_player','');
	SELECT hk."fUserAdd"('replica_management','');
	SELECT hk."fUserAdd"('replica_cues','');
	SELECT hk."fUserAdd"('replica_sync','');
	SELECT hk."fUserAdd"('replica_ia','');
	SELECT hk."fUserAdd"('replica_failover','');

----------------------------------- hk."tAccessPermissions"
	SELECT hk."fAccessPermissionAdd"('ingest', 'replica_ingest_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('playlist', 'replica_playlist_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('rt', 'replica_rt_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('templates', 'replica_templates_full', true, true, true);

	SELECT hk."fAccessPermissionAdd"('ingest', 'replica_ingest', false, false, false);
	SELECT hk."fAccessPermissionAdd"('playlist', 'replica_playlist', false, false, false);
	SELECT hk."fAccessPermissionAdd"('rt', 'replica_rt', false, false, false);
	SELECT hk."fAccessPermissionAdd"('stat', 'replica_stat', false, false, false);
	SELECT hk."fAccessPermissionAdd"('templates', 'replica_templates', false, false, false);

	SELECT hk."fAccessPermissionAdd"('assets', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('assets.name', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('assets.classes', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('assets.file', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('assets.custom_values', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.name', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.classes', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.file', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.custom_values', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.artists', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.rotations', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.sound', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('clips.palette', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.name', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.classes', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.file', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.custom_values', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.clips', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.chatinouts', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('advertisements', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('advertisements.name', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('advertisements.classes', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('advertisements.file', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('advertisements.custom_values', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('designs', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('designs.name', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('designs.classes', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('designs.file', 'replica_assets', false, false, false);
	SELECT hk."fAccessPermissionAdd"('designs.custom_values', 'replica_assets', false, false, false);

	SELECT hk."fAccessPermissionAdd"('assets', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('assets.name', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('assets.classes', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('assets.file', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('assets.custom_values', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.name', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.classes', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.file', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.custom_values', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.artists', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.rotations', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.sound', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('clips.palette', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.name', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.classes', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.file', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.custom_values', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.clips', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.chatinouts', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('advertisements', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('advertisements.name', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('advertisements.classes', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('advertisements.file', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('advertisements.custom_values', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('designs', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('designs.name', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('designs.classes', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('designs.file', 'replica_assets_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('designs.custom_values', 'replica_assets_full', true, true, true);

	SELECT hk."fAccessPermissionAdd"('programs', 'replica_programs', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.name', 'replica_programs', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.classes', 'replica_programs', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.file', 'replica_programs', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.custom_values', 'replica_programs', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.clips', 'replica_programs', false, false, false);
	SELECT hk."fAccessPermissionAdd"('programs.chatinouts', 'replica_programs', false, false, false);

	SELECT hk."fAccessPermissionAdd"('programs', 'replica_programs_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.name', 'replica_programs_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.classes', 'replica_programs_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.file', 'replica_programs_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.custom_values', 'replica_programs_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.clips', 'replica_programs_full', true, true, true);
	SELECT hk."fAccessPermissionAdd"('programs.chatinouts', 'replica_programs_full', true, true, true);

	SELECT hk."fAccessPermissionAdd"('templates.schedule', 'replica_templates', true, true, true);
	SELECT hk."fAccessPermissionAdd"('templates.schedule', 'replica_templates_full', true, true, true);

	SELECT hk."fAccessPermissionAdd"('grid', 'replica_grid', false, false, false);
	SELECT hk."fAccessPermissionAdd"('grid', 'replica_grid_full', true, true, true);
----------------------------------- hk."tWebPages"
	SELECT hk."fWebPageAdd"(NULL, 'ingest');
	SELECT hk."fWebPageAdd"(NULL, 'playlist');
	SELECT hk."fWebPageAdd"(NULL, 'assets');
	SELECT hk."fWebPageAdd"(NULL, 'stat');
	SELECT hk."fWebPageAdd"(NULL, 'program');
----------------------------------- hk."tErrorScopes"
	SELECT hk."fErrorScopeAdd"('media', 'tFiles', 'unknown');
	SELECT hk."fErrorScopeAdd"('media', 'tFiles', 'missed');
	SELECT hk."fErrorScopeAdd"('media', 'tStorages', 'unknown');
	SELECT hk."fErrorScopeAdd"('media', 'tStorages', 'missed');
----------------------------------- hk."tHouseKeeping"
	INSERT INTO hk."tHouseKeeping" VALUES (0); --системное событие не нуждающееся в логгирование на уровне БД
----------------------------------- hk."tDTEventTypes"
	SELECT hk."fDTEventTypeAdd"('create','INSERT');
	SELECT hk."fDTEventTypeAdd"('modify','UPDATE');
	SELECT hk."fDTEventTypeAdd"('delete','DELETE');
----------------------------------- hk."tDTEvents"
