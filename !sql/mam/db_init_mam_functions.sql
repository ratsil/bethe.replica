----------------------------------- mam."tAssets"
	CREATE OR REPLACE FUNCTION mam."fAssetGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssets';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssets';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetNameSet"(idAssets integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			UPDATE mam."tAssets" SET "sName"=sName WHERE id=idAssets;
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetRemove"(nAssetsID integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetCustomValuesClear"(nAssetsID);
			IF NOT stRetVal."bValue" THEN RETURN stRetVal; END IF;
			stRetVal := mam."fAssetPersonsClear"(nAssetsID);
			IF NOT stRetVal."bValue" THEN RETURN stRetVal; END IF;
			stRetVal := mam."fAssetStylesClear"(nAssetsID);
			IF NOT stRetVal."bValue" THEN RETURN stRetVal; END IF;
			stRetVal := mam."fAssetCuesClear"(nAssetsID);
			IF NOT stRetVal."bValue" THEN RETURN stRetVal; END IF;
			DELETE FROM mam."tVideos" WHERE "idAssets" = nAssetsID;
			DELETE FROM mam."tAssetAttributes" WHERE "idAssets" = nAssetsID;
			DELETE FROM mam."tAssets" WHERE "id" = nAssetsID;
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tAssetAttributes"
	CREATE OR REPLACE FUNCTION mam."fAssetAttributeGet"(idAssets integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeGet"(idAssets integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetAttributeGet"(idAssets, idRegisteredTables, NULL, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION mam."fAssetAttributeGet"(idAssets integer, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeGet"(idAssets integer, sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetAttributeGet"(idAssets, NULL, sKey);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeValueGet"(idAssets integer, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGetInt"(stTable, aColumns, 'nValue');
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeAdd"(idAssets integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeAdd"(idAssets integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, NULL, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeAdd"(idAssets integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, sKey, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeSet"(idAssets integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetAttributeGet"(idAssets, idRegisteredTables, sKey);
			IF NOT stRetVal."bValue" THEN
				stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, sKey, nValue);
			ELSE
				UPDATE mam."tAssetAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
				stRetVal."bValue" := true;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeSet"(idAssets integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fAssetAttributeGet"(idAssets, sKey);
			IF NOT stRetVal."bValue" THEN
				stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, sKey, nValue);
			ELSE
				UPDATE mam."tAssetAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
				stRetVal."bValue" := true;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeRemove"(idAssets integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableRemove"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeRemove"(idAssets integer, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableRemove"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetAttributeRemove"(idAssets integer, sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAssetAttributes';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{sKey,0}}';
			aColumns[2][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableRemove"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetClassSet"(idAssets integer, idClasses integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('pl','tClasses');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fAssetAttributeGet"(idAssets, idRegisteredTables, 'class');
			IF NOT stRetVal."bValue" THEN
				stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'class', idClasses);
			ELSE
				UPDATE mam."tAssetAttributes" SET "nValue"=idClasses WHERE id=stRetVal."nValue";
				stRetVal."bValue":=true;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetFileSet"(idAssets integer, idFiles integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('media','tFiles');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fAssetAttributeGet"(idAssets, idRegisteredTables, 'file');
			IF NOT stRetVal."bValue" THEN
				stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'file', idFiles);
			ELSE
				UPDATE mam."tAssetAttributes" SET "nValue"=idFiles WHERE id=stRetVal."nValue";
				stRetVal."bValue" := true;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tPersonTypes"
	CREATE OR REPLACE FUNCTION mam."fPersonTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tPersonTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fPersonTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tPersonTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tPersons"
	CREATE OR REPLACE FUNCTION mam."fPersonGet"(idPersonTypes integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tPersons';
			aColumns := '{{idPersonTypes,'||idPersonTypes||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fPersonAdd"(idPersonTypes integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tPersons';
			aColumns := '{{idPersonTypes,'||idPersonTypes||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fArtistAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal:=mam."fPersonTypeGet"('artist');
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			stRetVal := mam."fPersonAdd"(stRetVal."nValue",sName);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fPersonRemove"(idd integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			nAssetsQty integer;
		BEGIN
			SELECT count(*) INTO nAssetsQty FROM mam."vAssetsPersons" ap WHERE ap."idPersons"=idd;
			stRetVal."nValue" := nAssetsQty;
			stRetVal."bValue" := false;
			IF 1 > nAssetsQty THEN
				stRetVal := "fTableRemove"('mam', 'tPersons', id);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tVideoTypes"
	CREATE OR REPLACE FUNCTION mam."fVideoTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tVideoTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fVideoTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tVideoTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tVideos"
	CREATE OR REPLACE FUNCTION mam."fVideoGet"(idAssets integer, idVideoTypes integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tVideos';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idVideoTypes,'||COALESCE(idVideoTypes::text,'NULL')||'},{sName,0}}';
			aColumns[3][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fVideoGet"(idAssets integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tVideos';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fVideoAdd"(idAssets integer, idVideoTypes integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tVideos';
			aColumns := '{{idAssets,'||COALESCE(idAssets::text,'NULL')||'},{idVideoTypes,'||COALESCE(idVideoTypes::text,'NULL')||'},{sName,0}}';
			aColumns[3][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fVideoSet"(idAssets integer, idVideoTypes integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fVideoGet"(idAssets);
			IF stRetVal."bValue" THEN
				UPDATE mam."tVideos" SET "idVideoTypes"=idVideoTypes, "sName"=sName WHERE idAssets="idAssets";
			ELSE
				stRetVal := mam."fVideoAdd"(idAssets, idVideoTypes, sName);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetVideoTypeSet"(idAssets integer, idVideoTypes integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			UPDATE mam."tVideos" SET "idVideoTypes"=idVideoTypes WHERE "idAssets"=idAssets;
			stRetVal."nValue" := idAssets;
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tAlbums"
	CREATE OR REPLACE FUNCTION mam."fAlbumGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAlbums';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAlbumAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tAlbums';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tCues"
	CREATE OR REPLACE FUNCTION mam."fCuesAdd"(sSong text, sArtist text, sAlbum text, nYear integer, sPossessor text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCues';
			aColumns := '{{sSong,0},{sArtist,0},{sAlbum,0},{nYear,'||COALESCE(nYear::text,'NULL')||'},{sPossessor,0}}';
			aColumns[1][2] := sSong; --чтобы не морочиться с экранированием
			aColumns[2][2] := sArtist;
			aColumns[3][2] := sAlbum;
			--aColumns[4][2] := nYear::text;
			aColumns[5][2] := sPossessor;
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tCategories"
	CREATE OR REPLACE FUNCTION mam."fCategoryGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCategories';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fCategoryAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCategories';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tCategoryValues"
	CREATE OR REPLACE FUNCTION mam."fCategoryValueGet"(idCategories integer, sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCategoryValues';
			aColumns := '{{idCategories,0},{sValue,0}}';
			aColumns[1][2] := idCategories;
			aColumns[2][2] := sValue; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fCategoryValueAdd"(idCategories integer, sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCategoryValues';
			aColumns := '{{idCategories,0},{sValue,0}}';
			aColumns[1][2] := idCategories;
			aColumns[2][2] := sValue; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tTimeMapTypes"
	CREATE OR REPLACE FUNCTION mam."fTimeMapTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tTimeMapTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fTimeMapTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tTimeMapTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- mam."tTimeMaps"
	CREATE OR REPLACE FUNCTION mam."fTimeMapAdd"(idTimeMapTypes integer, bsMap bit varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tTimeMaps';
			aColumns := '{{idTimeMapTypes,0},{bsMap,0}}';
			aColumns[1][2] := idTimeMapTypes;
			aColumns[2][2] := bsMap; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tTimeMapBinds"
	CREATE OR REPLACE FUNCTION mam."fTimeMapBindGet"(idTimeMaps integer, nOrder integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tTimeMapBinds';
			aColumns := '{{idTimeMaps,0},{nOrder,0}}';
			aColumns[1][2] := idTimeMaps; --чтобы не морочиться с экранированием
			aColumns[2][2] := nOrder;
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fTimeMapBindAdd"(idTimeMaps integer, nOrder integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tTimeMapBinds';
			aColumns := '{{idTimeMaps,0},{nOrder,0}}';
			aColumns[1][2] := idTimeMaps; --чтобы не морочиться с экранированием
			aColumns[2][2] := nOrder;
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tCustomValues"
	CREATE OR REPLACE FUNCTION mam."fCustomValueGet"(sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCustomValues';
			aColumns := '{{sValue,0}}';
			aColumns[1][2] := sValue; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fCustomValueAdd"(sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tCustomValues';
			aColumns := '{{sValue,0}}';
			aColumns[1][2] := sValue; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fCustomValueSet"(idCustomValues integer, sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			UPDATE mam."tCustomValues" SET "sValue"=sValue WHERE id=idCustomValues;
			stRetVal."bValue" := true;
			stRetVal."nValue" := idCustomValues;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- Common actions
	/*
	CREATE OR REPLACE FUNCTION mam."fVideoClipAdd"(sTitle text, sArtist text, sSong text, sAlbum text, nYear integer, sFilename text, nFramesQty integer, sExternalProvider text, sExternalProviderID text, bPLEnabled boolean) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			sValue text;
			nValue integer;
			idPersons integer;
			idAlbums integer;
			idAssets integer;
			idVideoTypes integer;
			idClasses integer;
			idVideos integer;
			idRegisteredTables integer;
			idExternalProviders integer;
			idCustomValues integer;
			idStorages integer;
			idFiles integer;
			idCues integer;
			aColumns text[];
		BEGIN
			stRetVal := mam."fAssetAdd"(sTitle);
			idAssets := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fVideoTypeGet"('clip');
			idVideoTypes := stRetVal."nValue";

			stRetVal := mam."fVideoAdd"(idAssets, idVideoTypes, sTitle);
			idVideos := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fClassGet"('clip_common');
			idClasses := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('pl','tClasses');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'class', idClasses);
			---------------------------------------------------------
			stRetVal := mam."fArtistAdd"(sArtist);
			idPersons := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('mam','tPersons');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'person', idPersons);
			---------------------------------------------------------
			sValue:=sAlbum;
			IF sAlbum IS NULL THEN 
				sValue:='Неизвестно';
			END IF;
			stRetVal := mam."fAlbumAdd"(sValue);
			idAlbums := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('mam','tAlbums');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'album', idAlbums);
			---------------------------------------------------------
			IF nYear IS NOT NULL THEN 
				stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, 'nYear',  nYear);
			END IF;
			---------------------------------------------------------
			stRetVal := media."fStorageAdd"(1, 'Клипы', 'j:/storages/clips/', true);
			idStorages := stRetVal."nValue";
			stRetVal := media."fFileAdd"(idStorages, sFilename);
			idFiles := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('media','tFiles');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'file',  idFiles);
			---------------------------------------------------------
			stRetVal := mam."fExternalProviderAdd"(sExternalProvider);
			idExternalProviders := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('mam','tExternalProviders');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'ExternalProvider',  idExternalProviders);
			
			stRetVal := mam."fCustomValueAdd"(sExternalProviderID);
			idCustomValues := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('mam','tCustomValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'ExternalProviderID',  idCustomValues);
			---------------------------------------------------------
			stRetVal := mam."fCuesAdd"(sSong, sArtist, null, null, null);
			idCues := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('mam','tCues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'cues',  idCues);
			---------------------------------------------------------
			stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, 'nFramesQty',  nFramesQty);
			---------------------------------------------------------
			nValue:=0;
			IF true=bPLEnabled THEN 
				nValue:=1;
			END IF;
			stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, 'bPLEnabled',  nValue);
			---------------------------------------------------------
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAdvertisementAdd"(sTitle text, sFilename text, nFramesQty integer, sExternalProvider text, sExternalProviderID text, bPLEnabled boolean) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			sValue text;
			nValue integer;
			idAssets integer;
			idVideoTypes integer;
			idClasses integer;
			idVideos integer;
			idRegisteredTables integer;
			idExternalProviders integer;
			idStorages integer;
			idFiles integer;
		BEGIN
			stRetVal := mam."fAssetAdd"(sTitle);
			idAssets := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fVideoTypeGet"('advertisement');
			idVideoTypes := stRetVal."nValue";

			stRetVal := mam."fVideoAdd"(idAssets, idVideoTypes, sTitle);
			idVideos := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fClassGet"('advertisement_common');
			idClasses := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('pl','tClasses');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'class', idClasses);
			---------------------------------------------------------
			stRetVal := media."fStorageAdd"(1, 'Реклама', 'j:/storages/advertisements/', true);
			idStorages := stRetVal."nValue";
			stRetVal := media."fFileAdd"(idStorages, sFilename);
			idFiles := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('media','tFiles');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'file',  idFiles);
			---------------------------------------------------------
			stRetVal := mam."fExternalProviderAdd"(sExternalProvider);
			idExternalProviders := stRetVal."nValue";

			stRetVal := hk."fRegisteredTableGet"('mam','tExternalProviders');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, sExternalProviderID,  idExternalProviders);
			---------------------------------------------------------
			stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, 'nFramesQty',  nFramesQty);
			---------------------------------------------------------
			nValue:=0;
			IF true=bPLEnabled THEN 
				nValue:=1;
			END IF;
			stRetVal := mam."fAssetAttributeAdd"(idAssets, NULL, 'bPLEnabled',  nValue);
			---------------------------------------------------------
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
		*/
	CREATE OR REPLACE FUNCTION mam."fAssetPersonAdd"(idAssets integer, idPersons integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tPersons');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'person', idPersons);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetPersonsClear"(idAssets integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('mam','tPersons');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			DELETE FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables AND "sKey"='person';
			---------------------------------------------------------
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetStyleAdd"(idAssets integer, idCategoryValues integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, 'style', idCategoryValues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetStylesClear"(idAssets integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			DELETE FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables AND "sKey"='style';
			---------------------------------------------------------
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetRotationSet"(idAssets integer, idCategoryValues integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeSet"(idAssets, idRegisteredTables, 'rotation', idCategoryValues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetRotationSet"(idAssets integer, sRotation text) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idCategories integer;
			idCategoryValues integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := mam."fCategoryGet"('rotation');
			idCategories := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fCategoryValueGet"(idCategories, sRotation);
			idCategoryValues := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fAssetRotationSet"(idAssets, idCategoryValues);
			---------------------------------------------------------

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetPaletteSet"(idAssets integer, idCategoryValues integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeSet"(idAssets, idRegisteredTables, 'palette', idCategoryValues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION mam."fAssetSexSet"(idAssets integer, idCategoryValues integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeSet"(idAssets, idRegisteredTables, 'sex', idCategoryValues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetSoundBeginSet"(idAssets integer, idCategoryValues integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeSet"(idAssets, idRegisteredTables, 'sound_start', idCategoryValues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetSoundEndSet"(idAssets integer, idCategoryValues integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCategoryValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeSet"(idAssets, idRegisteredTables, 'sound_end', idCategoryValues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetCustomValuesClear"(idAssets integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('mam','tCustomValues');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			DELETE FROM mam."tCustomValues" WHERE id IN (SELECT "nValue" FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables);
			DELETE FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables;
			---------------------------------------------------------
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetCuesClear"(idAssets integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('mam','tCues');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			DELETE FROM mam."tCues" WHERE id IN (SELECT "nValue" FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables);
			DELETE FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables;
			---------------------------------------------------------
			stRetVal."bValue" := true;
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetCuesSet"(idAssets integer, sSong character varying, sArtist character varying, sAlbum character varying, nYear integer, sPossessor character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idCues integer;
			idRegisteredTables integer;
		BEGIN
			stRetVal := mam."fAssetCuesClear"(idAssets);
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tCues');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fCuesAdd"(sSong, sArtist, sAlbum, nYear, sPossessor);
			idCues := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fAssetAttributeSet"(idAssets, idRegisteredTables, 'cues', idCues);
			---------------------------------------------------------
			stRetVal."nValue" := idAssets;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetCustomValueAdd"(idAssets integer, sName character varying, sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
			idCustomValues integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('mam','tCustomValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fCustomValueAdd"(sValue);
			idCustomValues := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := mam."fAssetAttributeAdd"(idAssets, idRegisteredTables, sName,  idCustomValues);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fAssetCustomValueSet"(idAssets integer, sName character varying, sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('mam','tCustomValues');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := mam."fAssetAttributeValueGet"(idAssets, idRegisteredTables, sName);
			IF stRetVal."bValue" THEN
				stRetVal := mam."fCustomValueSet"(stRetVal."nValue", sValue);
			ELSE
				stRetVal := mam."fAssetCustomValueAdd"(idAssets, sName, sValue);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tMacroTypes"
	CREATE OR REPLACE FUNCTION mam."fMacroTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tMacroTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fMacroTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tMacroTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- mam."tMacros"
	CREATE OR REPLACE FUNCTION mam."fMacroGet"(idMacroTypes integer, sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tMacros';
			aColumns := '{{idMacroTypes,'||idMacroTypes||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fMacroAdd"(idMacroTypes integer, sName character varying, sValue text) RETURNS  int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='mam';
			stTable."sName":='tMacros';
			aColumns := '{{idMacroTypes,'||idMacroTypes||'},{sName,0},{sValue,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			aColumns[3][2] := sValue; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION mam."fMacroAdd"(sTypeName character varying, sName character varying, sValue text) RETURNS  int_bool AS
		$$
		DECLARE
			idMacroTypes integer;
			stRetVal int_bool;
		BEGIN
			stRetVal := mam."fMacroTypeGet"(sTypeName);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			idMacroTypes := stRetVal."nValue";
			stRetVal := mam."fMacroAdd"(idMacroTypes, sName, sValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
