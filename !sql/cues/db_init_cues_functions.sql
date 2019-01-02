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
