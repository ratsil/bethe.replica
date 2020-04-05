----------------------------------- cues."tTemplates"
	CREATE OR REPLACE FUNCTION cues."fTemplateGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tTemplates';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fTemplateAdd"(sName character varying, sFile text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tTemplates';
			aColumns := '{{sName,0},{sFile,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			aColumns[2][2] := sFile; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fTemplateStarted"(idTemplates integer, idPlaylistItems integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('cues', 'tTemplates');
			IF stRetVal."bValue" THEN
				stRetVal := pl."fItemAttributeAdd"(idPlaylistItems, stRetVal."nValue", 'template', idTemplates);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- cues."tClassAndTemplateBinds"
	CREATE OR REPLACE FUNCTION cues."fClassAndTemplateBindsGet"(idClasses integer, idTemplates integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tClassAndTemplateBinds';
			aColumns := '{{idClasses,'||COALESCE(idClasses::text,'NULL')||'},{idTemplates,'||COALESCE(idTemplates::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[4][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fClassAndTemplateBindsAdd"(idClasses integer, idTemplates integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tClassAndTemplateBinds';
			aColumns := '{{idClasses,'||COALESCE(idClasses::text,'NULL')||'},{idTemplates,'||COALESCE(idTemplates::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[4][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fClassAndTemplateBindsAdd"(sClassName character varying, sTemplateName character varying, sRegisteredTableSchema character varying, sRegisteredTableName character varying, sKey character varying, nTargetRecodID integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			idClasses integer;
			idTemplates integer;
			idRegisteredTables integer;
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fClassGet"(sClassName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idClasses := stRetVal."nValue";
			
			stRetVal := cues."fTemplateGet"(sTemplateName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idTemplates := stRetVal."nValue";
			
			stRetVal := hk."fRegisteredTableGet"(sRegisteredTableSchema, sRegisteredTableName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idRegisteredTables := stRetVal."nValue";
			
			stRetVal := cues."fClassAndTemplateBindsAdd"(idClasses, idTemplates, idRegisteredTables, sKey, nTargetRecodID);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fClassAndTemplateBindsAdd"(sClassName character varying, sTemplateName character varying, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			idClasses integer;
			idTemplates integer;
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fClassGet"(sClassName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idClasses := stRetVal."nValue";
			
			stRetVal := cues."fTemplateGet"(sTemplateName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idTemplates := stRetVal."nValue";
			
			stRetVal := cues."fClassAndTemplateBindsAdd"(idClasses, idTemplates, null, sKey, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fClassAndTemplateBindsRemove"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tClassAndTemplateBinds';
			stRetVal := "fTableRemove"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fPLIRingtone"(idItems integer, nLine integer) RETURNS text AS
		$$
		DECLARE
			stRecord record;
			aLines text[2][6];
		BEGIN
			aLines := '{{0,0,0,0,0,0},{0,0,0,0,0,0}}';
			aLines[1][1] := 'ГОЛОСУЙ ЗА КЛИПЫ';
			aLines[1][2] := 'ЗВОНИ';
			aLines[1][3] := '0971';
			aLines[1][4] := '';
			aLines[1][5] := '';
			aLines[1][6] := '';

			aLines[2][1] := 'ГОЛОСУЙ ЗА ЭТОТ ХИТ';
			aLines[2][2] := 'ОТПРАВЬ';
			aLines[2][3] := '';
			aLines[2][4] := 'НА';
			aLines[2][5] := '5555';
			aLines[2][6] := '';

			SELECT p.id, "nReplaceCode" as "nRTInfon", ac."sAlbum"::int as "nRTTemafon", p."sClassName" INTO stRecord FROM pl."vPlayListResolved" p, mam."vAssetsCues" ac LEFT JOIN cues."tRingtones" rt ON ac."sAlbum"::int=rt."nBindCode" WHERE p."idAssets" = ac.id AND idItems = p.id;
			IF FOUND THEN
				IF 'clip_with_fmn' = stRecord."sClassName" THEN
					RETURN '';
				ELSIF 'clip_with_novelty' = stRecord."sClassName" THEN
					RETURN '';
				ELSIF stRecord."nRTInfon" IS NULL THEN
					RETURN '';  /*  aLines[1][nLine + 1]   попросили отрубить вообще пока голосуй за клипы*/
				ELSIF 2 = nLine THEN
					RETURN to_char(stRecord."nRTInfon", '0000');
				ELSE
					RETURN aLines[2][nLine + 1];
				END IF;
			END IF;
			RETURN NULL;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fPLIArtist"(idItems integer) RETURNS text AS
		$$
		DECLARE
			sArtistName text;
		BEGIN
			SELECT ac."sArtist" INTO sArtistName FROM pl."vPlayListResolved" p, mam."vAssetsCues" ac WHERE p."idAssets" = ac.id AND idItems = p.id;
			RETURN sArtistName;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fCUArtist"(idItems integer, nOffset integer) RETURNS text AS
		$$
		DECLARE
			nTargetItemID integer;
			dtStart timestamp with time zone;
			nIndx integer;
		BEGIN
			nIndx := 1;
			SELECT "dtStart" INTO dtStart FROM pl."vPlayListResolved" WHERE idItems = id;
			FOR nTargetItemID IN SELECT cu.id FROM pl."vComingUp" cu, mam."vAssetsCues" ac WHERE cu."idAssets" = ac.id AND dtStart <= "dtStart" ORDER BY cu."dtStartReal", cu."dtStartQueued", cu."dtStartPlanned" LOOP
				IF nOffset < nIndx THEN
					RETURN cues."fPLIArtist"(nTargetItemID);
				END IF;
				nIndx := nIndx + 1;
			END LOOP;
			RETURN NULL;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fPLISong"(idItems integer) RETURNS text AS
		$$
		DECLARE
			sSongName text;
		BEGIN
			SELECT ac."sSong" INTO sSongName FROM pl."vPlayListResolved" p, mam."vAssetsCues" ac WHERE p."idAssets" = ac.id AND idItems = p.id;
			RETURN sSongName;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fCUSong"(idItems integer, nOffset integer) RETURNS text AS
		$$
		DECLARE
			nTargetItemID integer;
			dtStart timestamp with time zone;
			nIndx integer;
		BEGIN
			nIndx := 1;
			SELECT "dtStart" INTO dtStart FROM pl."vPlayListResolved" WHERE idItems = id;
			FOR nTargetItemID IN SELECT cu.id FROM pl."vComingUp" cu, mam."vAssetsCues" ac WHERE cu."idAssets" = ac.id AND dtStart <= "dtStart" ORDER BY cu."dtStartReal", cu."dtStartQueued", cu."dtStartPlanned" LOOP
				IF nOffset < nIndx THEN
					RETURN cues."fPLISong"(nTargetItemID);
				END IF;
				nIndx := nIndx + 1;
			END LOOP;
			RETURN NULL;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fCUFile"(idItems integer, nOffset integer) RETURNS text AS
		$$
		DECLARE
			sFile text;
			dtStart timestamp with time zone;
			nIndx integer;
		BEGIN
			nIndx := 1;
			SELECT "dtStart" INTO dtStart FROM pl."vPlayListResolved" WHERE idItems = id;
			FOR sFile IN SELECT "sPath"||"sFilename" FROM pl."vComingUp" cu, mam."vAssetsCues" ac WHERE cu."idAssets" = ac.id AND dtStart <= "dtStart" ORDER BY cu."dtStartReal", cu."dtStartQueued", cu."dtStartPlanned" LOOP
				IF nOffset < nIndx THEN
					RETURN sFile;
				END IF;
				nIndx := nIndx + 1;
			END LOOP;
			RETURN NULL;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fPLIAssetSmokingShow"(idItems integer) RETURNS boolean AS
		$$
		DECLARE
			idAssets integer;
		BEGIN
			IF EXISTS (SELECT 1 FROM pl."vPlayListResolved" itms, mam."vAssetsCustomValues" acv WHERE itms."idAssets" = acv.id AND 'smoking' = acv."sCustomValueName") THEN
				RETURN true;
			END IF;
			RETURN false;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fIsTemplateActual"(iditems integer, idclassandtemplatebinds integer)
		RETURNS boolean AS
		$BODY$
		DECLARE
			stCaTBinds RECORD;
			stItem RECORD;
			idAssets integer;
			sValue text;
			nValue integer;
			dtValue timestamp with time zone;
		BEGIN
			--RAISE NOTICE 'idItems:% :: idClassAndTemplateBinds:%', idItems, idClassAndTemplateBinds;
			SELECT array[bnds.*] as binds, array[itms.*] as items INTO stItem FROM cues."vClassAndTemplateBinds" bnds, pl."vPlayListResolved" itms WHERE bnds."idClasses"=itms."idClasses" AND idItems=itms.id AND idClassAndTemplateBinds=bnds.id;
			--RAISE NOTICE '%', stItem.binds;
			IF stItem.binds IS NULL THEN
				RETURN false;
			END IF;
			stCaTBinds := stItem.binds[1];
			stItem := stItem.items[1];
			--RAISE NOTICE '%', stCaTBinds."idTemplates";
			--SELECT * INTO stCaTBinds FROM cues."vClassAndTemplateBinds" bnds WHERE id=idClassAndTemplateBinds;
			--SELECT * INTO stItem FROM pl."vItemTimings" WHERE idItems=id;
			IF 'start_after' = stCaTBinds."sKey" THEN
				IF 'mam' = stCaTBinds."sRegisteredTableSchema" AND 'tVideoTypes' = stCaTBinds."sRegisteredTableName" THEN
					SELECT "idAssets" INTO idAssets FROM pl."vPlayListResolved" WHERE stItem."dtStart" > "dtStart" ORDER BY "dtStart" DESC LIMIT 1;
					IF idAssets IS NULL THEN
						RETURN false;
					END IF;
					SELECT id INTO idAssets FROM mam."vAssetsVideoTypes" WHERE idAssets=id AND stCaTBinds."nValue"="idVideoTypes";
					--RAISE NOTICE '   idAssets:%', idAssets;
					IF idAssets IS NULL THEN
						RETURN false;
					END IF;
				END IF;
			ELSIF 'start_before' = stCaTBinds."sKey" THEN
			END IF;
			--RAISE NOTICE '% :: %', stCaTBinds, stItem;
			RETURN true;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;



----------------------------------- cues."tTemplatesSchedule"
	CREATE OR REPLACE FUNCTION cues."fTemplatesScheduleGet"(idTemplates integer, dtStart timestamp with time zone, tsInterval interval, dtStop timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tTemplatesSchedule';
			aColumns := '{{idTemplates,'||idTemplates::text||'},{dtStart,0},{tsInterval,0},{dtStop,0}}';
			aColumns[2][2] := COALESCE(quote_literal(dtStart),'NULL');
			aColumns[3][2] := COALESCE(quote_literal(tsInterval),'NULL');
			aColumns[4][2] := COALESCE(quote_literal(dtStop),'NULL');
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fTemplatesScheduleAdd"(idClassAndTemplateBinds integer, dtStart timestamp with time zone, tsInterval interval, dtStop timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tTemplatesSchedule';
			aColumns := '{{idClassAndTemplateBinds,'||idClassAndTemplateBinds::text||'},{dtStart,0},{tsInterval,0},{dtStop,0}}';
			aColumns[2][2] := COALESCE(quote_literal(dtStart),'NULL');
			aColumns[3][2] := COALESCE(quote_literal(tsInterval),'NULL');
			aColumns[4][2] := COALESCE(quote_literal(dtStop),'NULL');
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fTemplatesScheduleSet"(id integer, idClassAndTemplateBinds integer, dtStart timestamp with time zone, tsInterval interval, dtStop timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tTemplatesSchedule';
			aColumns := '{{idClassAndTemplateBinds,'||idClassAndTemplateBinds::text||'},{dtStart,0},{tsInterval,0},{dtStop,0}}';
			aColumns[2][2] := COALESCE(quote_literal(dtStart),'NULL');
			aColumns[3][2] := COALESCE(quote_literal(tsInterval),'NULL');
			aColumns[4][2] := COALESCE(quote_literal(dtStop),'NULL');
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fTemplatesScheduleRemove"(idTemplatesSchedule integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
			idClassAndTemplateBinds integer;
		BEGIN
			SELECT "idClassAndTemplateBinds" INTO idClassAndTemplateBinds FROM cues."tTemplatesSchedule" WHERE idTemplatesSchedule=id;
			stTable."sSchema":='cues';
			stTable."sName":='tTemplatesSchedule';
			stRetVal := hk."fRegisteredTableGet"(stTable);
			IF stRetVal."bValue" THEN
				stRetVal := cues."fDictionaryValueRemove"(stRetVal."nValue", idTemplatesSchedule);
				IF stRetVal."bValue" THEN
					stRetVal := "fTableRemove"(stTable, idTemplatesSchedule);
					--IF stRetVal."bValue" THEN
					--	SELECT count(*) INTO stRetVal."nValue" FROM cues."tTemplatesSchedule" WHERE idClassAndTemplateBinds="idClassAndTemplateBinds";
					--	IF 1 > stRetVal."nValue" THEN
					--		stRetVal := cues."fClassAndTemplateBindsRemove"(idClassAndTemplateBinds);
					--	END IF;
					--END IF;
				END IF;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- cues."tDictionary"
	CREATE OR REPLACE FUNCTION cues."fDictionaryValueAdd"(idRegisteredTables integer, idTarget integer, sKey text, sValue text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tDictionary';
			aColumns := '{{"idRegisteredTables",'||idRegisteredTables::text||'},{"idTarget",'||idTarget::text||'},{sKey,0},{sValue,0}}';
			aColumns[3][2] := COALESCE(sKey,'NULL');
			aColumns[4][2] := COALESCE(sValue,'NULL');
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fDictionaryValueGet"(stTable table_name, idTarget integer, sKey text) RETURNS text AS
		$$
		DECLARE
			stRT int_bool;
		BEGIN
			stRT := hk."fRegisteredTableGet"(stTable);
			IF NOT stRT."bValue" THEN
				RAISE EXCEPTION 'Wrong table name';		
			END IF;
			RETURN cues."fDictionaryValueGet"(stRT."nValue", idTarget, sKey);
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fDictionaryValueGet"(idRegisteredTables integer, idTarget integer, sKey text) RETURNS text AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tDictionary';
			aColumns := '{{"idRegisteredTables",'||idRegisteredTables::text||'},{"idTarget",'||idTarget::text||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey,'NULL');
			RETURN "fTableGetText"(stTable, aColumns, 'sValue');
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fDictionaryValueRemove"(idRegisteredTables integer, idTarget integer, sKey text) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			DELETE FROM cues."tDictionary" WHERE idRegisteredTables="idRegisteredTables" AND idTarget="idTarget" AND sKey="sKey";
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fDictionaryValueRemove"(idRegisteredTables integer, idTarget integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			DELETE FROM cues."tDictionary" WHERE idRegisteredTables="idRegisteredTables" AND idTarget="idTarget";
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- cues."tChatInOuts"
	CREATE OR REPLACE FUNCTION cues."fChatInOutAdd"(
			idassets integer,
			nframein integer,
			nframeout integer)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			IF idAssets IS NOT NULL AND nFrameIn IS NOT NULL AND nFrameOut IS NOT NULL AND nFrameOut > nFrameIn THEN
				stTable."sSchema":='cues';
				stTable."sName":='tChatInOuts';
				aColumns := '{{idAssets,'||idAssets::text||'},{nFrameIn,'||nFrameIn::text||'},{nFrameOut,'||nFrameOut::text||'}}';
				stRetVal := "fTableAdd"(stTable, aColumns);
			ELSE
				stRetVal."bValue" := false;
			END IF; 
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;


	CREATE OR REPLACE FUNCTION cues."fChatInOutGet"(
			idassets integer,
			nframein integer,
			nframeout integer)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			IF idAssets IS NOT NULL AND nFrameIn IS NOT NULL AND nFrameOut IS NOT NULL AND nFrameOut > nFrameIn THEN
				stTable."sSchema":='cues';
				stTable."sName":='tChatInOuts';
				aColumns := '{{idAssets,'||idAssets::text||'},{nFrameIn,'||nFrameIn::text||'},{nFrameOut,'||nFrameOut::text||'}}';
				stRetVal := "fTableGet"(stTable, aColumns);
			ELSE
				stRetVal."bValue" := false;
			END IF; 
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fChatInOutsClear"(idassets integer)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stRetVal int_bool;
		BEGIN
			DELETE FROM cues."tChatInOuts" WHERE "idAssets"=idAssets;
			stRetVal."nValue" := idAssets;
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- cues."tMacro"
	CREATE OR REPLACE FUNCTION cues."fMacroAdd"(
			idmacrotypes integer,
			sname character varying,
			svalue text)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tMacros';
			aColumns := '{{idMacroTypes,'||idMacroTypes||'},{sName,0},{sValue,0}}';
			aColumns[2][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			aColumns[3][2] := sValue; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fMacroAdd"(
			stypename character varying,
			sname character varying,
			svalue text)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			idMacroTypes integer;
			stRetVal int_bool;
		BEGIN
			stRetVal := cues."fMacroTypeGet"(sTypeName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idMacroTypes := stRetVal."nValue";
			stRetVal := cues."fMacroAdd"(idMacroTypes, sName, sValue);
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fMacroGet"(
			idmacrotypes integer,
			sname character varying)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tMacros';
			aColumns := '{{idMacroTypes,'||idMacroTypes||'},{sName,0}}';
			aColumns[2][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fMacroTypeAdd"(sname character varying)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tMacroTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fMacroTypeGet"(sname character varying)
		RETURNS int_bool AS
		$BODY$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='cues';
			stTable."sName":='tMacroTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- cues."tPlugins"
	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistItemSave"(oPLI cues.tPluginPlaylistItem, bException bool) RETURNS cues.tPluginPlaylistItem AS
		$BODY$
		DECLARE
			nValue bigint;
			nID bigint;
			oPLISaved record;
			oRetVal cues.tPluginPlaylistItem;
		BEGIN
			SELECT * INTO oPLISaved FROM cues."vPluginPlaylistItems" WHERE oPLI.id=("oItem").id;
			oPLISaved := oPLISaved."oItem";
			IF oPLISaved IS NULL THEN
				IF bException THEN
					RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST ITEM [%]', oPLI.id;
				END IF;
				RETURN NULL;
			END IF;
			IF oPLI."oStatus" <> oPLISaved."oStatus" OR ( oPLI."oStatus").id <> (oPLISaved."oStatus").id THEN
				SELECT b.id INTO nID FROM cues."vBinds" b WHERE oPLI.id = b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'pl' = (i."oTableTarget")."sSchema" AND 'tStatuses' = (i."oTableTarget")."sName" AND 'status'=b."sName";
				IF FOUND THEN
					UPDATE cues."tBinds" SET "nValue"=(oPLI."oStatus").id WHERE nID=id;
				ELSIF oPLI."oStatus" IS NOT NULL THEN
					PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('pl','tStatuses'), 'status', (oPLI."oStatus").id, true);
				END IF;
			END IF;
			IF oPLI."oAsset" <> oPLISaved."oAsset" OR ( oPLI."oAsset").id <> (oPLISaved."oAsset").id THEN
				SELECT b.id INTO nID FROM cues."vBinds" b WHERE oPLI.id=b.id;
				IF FOUND THEN
					UPDATE cues."tBinds" SET "nValue"=(oPLI."oAsset").id WHERE nID=id;
				ELSIF oPLI."oStatus" IS NOT NULL THEN
					PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
				END IF;
			END IF;
			IF oPLI."dtStarted" <> oPLISaved."dtStarted" THEN
				IF EXISTS(SELECT b.id INTO nID FROM cues."vBindTimestamps" b WHERE oPLI.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'started'=b."sName") THEN
					nValue := NULL;
					IF oPLI."dtStarted" IS NOT NULL THEN
						nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPLI."dtStarted") || '}}')::text[][], true, false);
					END IF;
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE nID=id;
				ELSE
					IF oPLI."dtStarted" IS NOT NULL THEN
						nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPLI."dtStarted") || '}}')::text[][], true, false);
						PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('cues','tTimestamps'), 'started', nValue, true);
					END IF;
				END IF;
			END IF;
			SELECT * INTO oPLISaved FROM cues."vPluginPlaylistItems" WHERE oPLI.id=("oItem").id;
			RETURN oPLISaved."oItem";
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistItemSave"(oXML xml) RETURNS cues.tPluginPlaylist AS
		$BODY$
			SELECT cues."fPluginPlaylistItemSave"(oXML::cues.tPluginPlaylistItem, true);
		$BODY$
		LANGUAGE sql VOLATILE;


	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistSave"(oPlaylist cues.tPluginPlaylist, bException bool) RETURNS cues.tPluginPlaylist AS
		$BODY$
		DECLARE
			oTable table_name;
			nValue bigint;
			oPlaylistSaved record;
			oPLI cues.tPluginPlaylistItem;
		BEGIN
			
			oTable := ROW('cues','tBinds');
			IF oPlaylist.id IS NOT NULL THEN
				SELECT * INTO oPlaylistSaved FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
				oPlaylistSaved := oPlaylistSaved."oPlaylist";
				IF oPlaylistSaved IS NULL THEN
					IF bException THEN
						RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST [%]', ROW(oPlaylist.id, oPlaylist."sName");
					END IF;
					RETURN NULL;
				END IF;
				IF oPlaylist."sName" <> oPlaylistSaved."sName" THEN
					nValue := "fTableAdd"(ROW('cues','tStrings'), ('{{oValue, ' || oPlaylist."sName" || '}}')::text[][], true, false);
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindStrings" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'name'=b."sName");
				END IF;
				IF oPlaylist."dtStart" <> oPlaylistSaved."dtStart" THEN
					nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStart") || '}}')::text[][], true, false);
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindTimestamps" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'start'=b."sName");
				END IF;
				IF oPlaylist."dtStop" <> oPlaylistSaved."dtStop" THEN
					nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStop") || '}}')::text[][], true, false);
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindTimestamps" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'stop'=b."sName");
				END IF;
				FOR nIndx IN array_lower(oPlaylist."aItems", 1) .. array_upper(oPlaylist."aItems", 1) LOOP
					oPLI := oPlaylist."aItems"[nIndx];
					IF oPLI.id IS NULL THEN
						oPLI.id := "fBindAdd"(oTable, oPlaylist.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
						oPlaylist."aItems"[nIndx] := oPLI;
					END IF;
				END LOOP;
				FOR oPLI IN SELECT * FROM unnest(oPlaylistSaved."aItems") ac WHERE ac.id NOT IN (SELECT an.id FROM unnest(oPlaylist."aItems") an) LOOP
					DELETE FROM cues."tBinds" WHERE oPLI.id=id OR id IN(SELECT b.id FROM cues."vBinds" b WHERE oPLI.id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName");
				END LOOP;
			ELSIF EXISTS(SELECT "oPlaylist" FROM cues."vPluginPlaylists" WHERE oPlaylist."sName"=("oPlaylist")."sName") THEN
				IF bException THEN
					RAISE EXCEPTION 'PLUGIN PLAYLIST WITH SPECIFIED NAME ALREADY EXISTS [%]', oPlaylist."sName";
				END IF;
				RETURN NULL;
			ELSE
				nValue := "fTableGet"(ROW('cues','tPlugins'), '{{sName, "playlist"}}'::text[][], true);
				oPlaylist.id := "fBindAdd"(ROW('cues','tPlugins'), nValue, NULL, 'instance', NULL, true);
				nValue := "fTableAdd"(ROW('cues','tStrings'), ('{{oValue, ' || oPlaylist."sName" || '}}')::text[][], true, false);
				PERFORM "fBindAdd"(oTable, oPlaylist.id, ROW('cues','tStrings'), 'name', nValue, true);
				nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStart") || '}}')::text[][], true, false);
				PERFORM "fBindAdd"(oTable, oPlaylist.id, ROW('cues','tTimestamps'), 'start', nValue, true);
				nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, "' || quote_literal(oPlaylist."dtStop") || '"}}')::text[][], true, false);
				PERFORM "fBindAdd"(oTable, oPlaylist.id, ROW('cues','tTimestamps'), 'stop', nValue, true);
				FOR nIndx IN array_lower(oPlaylist."aItems", 1) .. array_upper(oPlaylist."aItems", 1) LOOP
					oPLI := oPlaylist."aItems"[nIndx];
					oPLI.id := "fBindAdd"(oTable, oPlaylist.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
					oPlaylist."aItems"[nIndx] := oPLI;
				END LOOP;
			END IF;
			FOR nIndx IN array_lower(oPlaylist."aItems", 1) .. array_upper(oPlaylist."aItems", 1) LOOP
				PERFORM cues."fPluginPlaylistItemSave"(oPlaylist."aItems"[nIndx], true);
			END LOOP;

			SELECT * INTO oPlaylistSaved FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
			RETURN oPlaylistSaved."oPlaylist";
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistSave"(oXML xml) RETURNS cues.tPluginPlaylist AS
		$BODY$
			SELECT cues."fPluginPlaylistSave"(oXML::cues.tPluginPlaylist, true);
		$BODY$
		LANGUAGE sql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistDelete"(oPlaylist cues.tPluginPlaylist, bException bool) RETURNS VOID AS
		$BODY$
		DECLARE
			oValue record;
		BEGIN
			SELECT * INTO oValue FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
			IF oValue IS NULL THEN
				IF bException THEN
					RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST [id:%]',oPlaylist.id;
				END IF;
				RETURN;
			END IF;
			oPlaylist := oValue."oPlaylist";
			DELETE FROM cues."tBinds" WHERE id IN(
					SELECT b.id FROM unnest(oPlaylist."aItems") as i, cues."vBinds" b WHERE (i).id=b.id OR ((i).id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName")
					UNION
					SELECT b.id FROM cues."vBinds" b WHERE oPlaylist.id=b.id OR (oPlaylist.id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName")
				);
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fIsGameActual"(stitem pl."vPlayListResolved") RETURNS boolean AS
		$BODY$
		DECLARE
			idAssets integer;
			dtPLIStart timestamp with time zone;
			dtMondayStart timestamp with time zone;
			dtDayStart timestamp with time zone;
			nHStart integer;
			nMaxPerDay integer;
			nHEndDay integer;
			nD integer;
			nH integer;
			bRetVal boolean;
			nVal integer;
			nRemainsH double precision;
			P integer;
		BEGIN
			IF stItem IS NULL THEN
				RETURN false;
			END IF;

			dtPLIStart = stItem."dtStartPlanned";
			IF dtPLIStart < '2018-11-22 13:00:00+03' OR dtPLIStart > '2019-01-01 13:00:00+03' THEN
				RETURN false; -- ========= СТАРТ И СТОП ВСЕЙ ИГРЫ =========
			END IF;
			
			SELECT "bIsActual" INTO bRetVal FROM logs."tGame" WHERE "idPlaylistItems" = stItem.id;
			IF NOT bRetVal IS NULL THEN
				RETURN bRetVal; -- уже ранее на этом PLI что-то решали
			END IF;
			IF dtPLIStart > now() + interval '1 hour' THEN
				RETURN false; -- нас дёрнули слишком заранее - наверно бекап или вручную
			END IF;
			idAssets = stItem."idAssets";
		RAISE NOTICE 'idA=%, dtS=%', idAssets, dtPLIStart;
			nD = EXTRACT(DOW FROM (dtPLIStart)); -- текущий день недели у PLI
			nH = EXTRACT(HOUR FROM (dtPLIStart)); -- текущий час у PLI
			
			-- ========= CONSTANTs =========
			nHStart = 18; -- ежесуточный старт начала учёта игры (сколько плашек выйдет именно от этого часа)
			nMaxPerDay = 12; -- целевой максимум выходов за сутки
			nHEndDay = 13;
			IF nD in (6, 0) THEN
				nHEndDay = 18; -- ежесуточный конец дня (не включительно)
			END IF;	

			IF nH >= nHEndDay AND nH < nHStart THEN
				RETURN false; -- необслуживаемый диапазон
			END IF;

			dtMondayStart = date_trunc('week', dtPLIStart) + nHStart * interval '1 hour'; -- 18 часов понедельника
			SELECT id INTO nVal FROM logs."tGame" WHERE "idAssets" = idAssets AND "bIsActual" = true AND "dtStartPlanned" >= dtMondayStart LIMIT 1;
		RAISE NOTICE 'dtM=%, nVal=%', dtMondayStart, nVal;
			IF NOT nVal IS NULL THEN
				RETURN false; -- этот ассет уже был в игре с понедельника
			END IF;

			dtDayStart = date_trunc('day', dtPLIStart) + nHStart * interval '1 hour';
			IF nH < nHStart THEN
				dtDayStart = dtDayStart - interval '1 day';
			END IF;
		RAISE NOTICE 'dtDStart=%, nH=%, nD=%', dtDayStart, nH, nD;

			SELECT COUNT("bIsActual") INTO nVal FROM logs."tGame" WHERE "bIsActual" = true AND "dtStartPlanned" >= dtDayStart GROUP BY "bIsActual";
			IF nVal IS NULL THEN
				nVal = 0;
			ELSIF nVal >= nMaxPerDay THEN
				RETURN false; -- на сегодня превышено количество
			END IF;
			nVal = nMaxPerDay - nVal;
			nRemainsH = EXTRACT(epoch FROM dtDayStart + interval '1 day' - dtPLIStart - (nHStart - nHEndDay) * interval '1 hour')/3600;
		RAISE NOTICE 'nRemainQty=%, nRemHours=%', nVal, nRemainsH;

			P = floor((60 * nRemainsH / nVal) / 3.5); -- вероятность - это 1/P; 3.5 - минут в среднем клип
		RAISE NOTICE 'P=%', P;
			IF nH in (0, 1, 2, 3, 4, 5, 6) THEN -- ========= НОЧНЫЕ ЧАСЫ + ВЕРОЯТНОСТЬ =========
				P = P * 3; -- понижаем вероятность втрое ночью
			END IF;
			IF P <= 1  OR  2 = floor(random() * P  + 1) THEN -- розыгрыш успешный - даём плашку. floor(random()* (high-low + 1) + low);
				INSERT INTO logs."tGame" ("idPlaylistItems", "idAssets", "dtStartPlanned", "bIsActual") VALUES (stItem.id, idAssets, dtPLIStart, true);
				RETURN true;
			ELSE
				INSERT INTO logs."tGame" ("idPlaylistItems", "idAssets", "dtStartPlanned", "bIsActual") VALUES (stItem.id, idAssets, dtPLIStart, false);
			END IF;

			RETURN false;
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE
		COST 100;
		ALTER FUNCTION cues."fIsGameActual"(pl."vPlayListResolved")
		OWNER TO pgsql;
