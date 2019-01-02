----------------------------------- mam."tAssets"
----------------------------------- mam."tAssetAttributes"
----------------------------------- mam."tAssetTypes"
	INSERT INTO mam."tAssetTypes" ("sName") VALUES ('series');
	INSERT INTO mam."tAssetTypes" ("sName") VALUES ('episode');
	INSERT INTO mam."tAssetTypes" ("sName") VALUES ('part');
----------------------------------- mam."tPersonTypes"
	SELECT * FROM mam."fPersonTypeAdd"('artist');
----------------------------------- mam."tVideoTypes"
	SELECT * FROM mam."fVideoTypeAdd"('clip');
	SELECT * FROM mam."fVideoTypeAdd"('advertisement');
	SELECT * FROM mam."fVideoTypeAdd"('program');
	SELECT * FROM mam."fVideoTypeAdd"('design');
----------------------------------- mam."tVideos"
----------------------------------- mam."tAlbums"
----------------------------------- mam."tCues"
----------------------------------- mam."tCategories"
	SELECT * FROM mam."fCategoryAdd"('style');
	SELECT * FROM mam."fCategoryAdd"('rotation');
	SELECT * FROM mam."fCategoryAdd"('sound_level');
	SELECT * FROM mam."fCategoryAdd"('palette');
	SELECT * FROM mam."fCategoryAdd"('sex');
----------------------------------- mam."tCategoryValues"
	SELECT * FROM mam."fCategoryValueAdd"(1, 'поп');
	SELECT * FROM mam."fCategoryValueAdd"(1, 'рок');
	SELECT * FROM mam."fCategoryValueAdd"(1, 'r&b');

	SELECT * FROM mam."fCategoryValueAdd"(2, 'эксклюзив');
	SELECT * FROM mam."fCategoryValueAdd"(2, 'новинки');
	SELECT * FROM mam."fCategoryValueAdd"(2, 'стоп');
	SELECT * FROM mam."fCategoryValueAdd"(2, 'первая');
	SELECT * FROM mam."fCategoryValueAdd"(2, 'вторая');
	SELECT * FROM mam."fCategoryValueAdd"(2, 'третья');

	SELECT * FROM mam."fCategoryValueAdd"(3, 'низкий');
	SELECT * FROM mam."fCategoryValueAdd"(3, 'средний');
	SELECT * FROM mam."fCategoryValueAdd"(3, 'высокий');

	SELECT * FROM mam."fCategoryValueAdd"(4, 'цветная');
	SELECT * FROM mam."fCategoryValueAdd"(4, 'черно-белая');
	SELECT * FROM mam."fCategoryValueAdd"(4, 'мультипликация');
	SELECT * FROM mam."fCategoryValueAdd"(4, 'сепия');

	SELECT * FROM mam."fCategoryValueAdd"(5, 'жен.');
	SELECT * FROM mam."fCategoryValueAdd"(5, 'муж.');
----------------------------------- mam."tTimeMapTypes"
	SELECT * FROM mam."fTimeMapTypeAdd"('все дни');
	SELECT * FROM mam."fTimeMapTypeAdd"('выходные');
	SELECT * FROM mam."fTimeMapTypeAdd"('праздники');
	SELECT * FROM mam."fTimeMapTypeAdd"('четные числа');
	SELECT * FROM mam."fTimeMapTypeAdd"('нечетные числа');
	SELECT * FROM mam."fTimeMapTypeAdd"('понедельник');
	SELECT * FROM mam."fTimeMapTypeAdd"('вторник');
	SELECT * FROM mam."fTimeMapTypeAdd"('среда');
	SELECT * FROM mam."fTimeMapTypeAdd"('четверг');
	SELECT * FROM mam."fTimeMapTypeAdd"('пятница');
	SELECT * FROM mam."fTimeMapTypeAdd"('суббота');
	SELECT * FROM mam."fTimeMapTypeAdd"('воскресенье');
----------------------------------- mam."tTimeMaps"
----------------------------------- mam."tTimeMapBinds"
----------------------------------- mam."tCustomValues"
----------------------------------- mam."tExternalProviders"
----------------------------------- mam."tMacroTypes"
	SELECT mam."fMacroTypeAdd"('sql');
	SELECT mam."fMacroTypeAdd"('link');
	SELECT mam."fMacroTypeAdd"('value');
----------------------------------- mam."tMacros"
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(0)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 0)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(1)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 1)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(2)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 2)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(3)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 3)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(4)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 4)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(5)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 5)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::RINGTONE::LINE(6)%}', 'SELECT * FROM cues."fPLIRingtone"({%RUNTIME::PLI::ID%}, 6)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::ARTIST%}', 'SELECT * FROM cues."fPLIArtist"({%RUNTIME::PLI::ID%})');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::CUES::SONG%}', 'SELECT * FROM cues."fPLISong"({%RUNTIME::PLI::ID%})');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(0)::CUES::ARTIST%}', 'SELECT * FROM cues."fCUArtist"({%RUNTIME::PLI::ID%}, 0)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(0)::CUES::SONG%}', 'SELECT * FROM cues."fCUSong"({%RUNTIME::PLI::ID%}, 0)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+1)::CUES::ARTIST%}', 'SELECT * FROM cues."fCUArtist"({%RUNTIME::PLI::ID%}, 1)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+1)::CUES::SONG%}', 'SELECT * FROM cues."fCUSong"({%RUNTIME::PLI::ID%}, 1)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+2)::CUES::ARTIST%}', 'SELECT * FROM cues."fCUArtist"({%RUNTIME::PLI::ID%}, 2)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+2)::CUES::SONG%}', 'SELECT * FROM cues."fCUSong"({%RUNTIME::PLI::ID%}, 2)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+3)::CUES::ARTIST%}', 'SELECT * FROM cues."fCUArtist"({%RUNTIME::PLI::ID%}, 3)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+3)::CUES::SONG%}', 'SELECT * FROM cues."fCUSong"({%RUNTIME::PLI::ID%}, 3)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+4)::CUES::ARTIST%}', 'SELECT * FROM cues."fCUArtist"({%RUNTIME::PLI::ID%}, 4)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+4)::CUES::SONG%}', 'SELECT * FROM cues."fCUSong"({%RUNTIME::PLI::ID%}, 4)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+5)::CUES::ARTIST%}', 'SELECT * FROM cues."fCUArtist"({%RUNTIME::PLI::ID%}, 5)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+5)::CUES::SONG%}', 'SELECT * FROM cues."fCUSong"({%RUNTIME::PLI::ID%}, 5)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(0)::MEDIA::FILE%}', 'SELECT * FROM cues."fCUFile"({%RUNTIME::PLI::ID%}, 0)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+1)::MEDIA::FILE%}', 'SELECT * FROM cues."fCUFile"({%RUNTIME::PLI::ID%}, 1)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+2)::MEDIA::FILE%}', 'SELECT * FROM cues."fCUFile"({%RUNTIME::PLI::ID%}, 2)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+3)::MEDIA::FILE%}', 'SELECT * FROM cues."fCUFile"({%RUNTIME::PLI::ID%}, 3)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+4)::MEDIA::FILE%}', 'SELECT * FROM cues."fCUFile"({%RUNTIME::PLI::ID%}, 4)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CU(+5)::MEDIA::FILE%}', 'SELECT * FROM cues."fCUFile"({%RUNTIME::PLI::ID%}, 5)');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::ASSET::SMOKING::SHOW%}', 'SELECT * FROM cues."fPLIAssetSmokingShow"({%RUNTIME::PLI::ID%})');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CRAWL(0)%}', '');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CRAWL(1)%}', 'СМОТРИТЕ СЕГОДНЯ В НОВОСТЯХ - ВСЁ САМОЕ ИНТЕРЕСНОЕ!');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CRAWL(2)%}', 'СМОТРИТЕ ЗАВТРА В ПРОГРАММЕ ХИП-ХОП ЧАРТ ВСЁ САМОЕ ЛУЧШЕЕ!!!');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::TEMPLATE::LINE(0)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplates''), {%RUNTIME::TCB::TEMPLATE::ID%}, ''line#0'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::TEMPLATE::LINE(1)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplates''), {%RUNTIME::TCB::TEMPLATE::ID%}, ''line#1'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::PATH%}', 'SELECT * FROM cues."fDictionaryValueGet"   ((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''path'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::LINE(0)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''line#0'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::LINE(1)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''line#1'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::LINE(2)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''line#2'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::LINE(3)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''line#3'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::LINE(4)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''line#4'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::CUES::TCB::SCHEDULE::LINE(5)%}', 'SELECT * FROM cues."fDictionaryValueGet"((''cues'', ''tTemplatesSchedule''), {%RUNTIME::TCB::SCHEDULE::ID%}, ''line#5'')');
	SELECT mam."fMacroAdd"('sql', '{%MACRO::REPLICA::PLI::MEDIA::FILE%}', 'SELECT "sPath"||"sFilename" FROM pl."vPlayListResolved" WHERE id={%RUNTIME::PLI::ID%}');

