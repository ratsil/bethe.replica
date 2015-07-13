SELECT hk."fAccessScopeAdd"('assets.name', true);
SELECT hk."fAccessScopeAdd"('assets.classes', true);
SELECT hk."fAccessScopeAdd"('assets.file', true);
SELECT hk."fAccessScopeAdd"('assets.custom_values', true);

SELECT hk."fAccessScopeAdd"('advertisements.name', true);
SELECT hk."fAccessScopeAdd"('advertisements.classes', true);
SELECT hk."fAccessScopeAdd"('advertisements.file', true);
SELECT hk."fAccessScopeAdd"('advertisements.custom_values', true);

SELECT hk."fAccessScopeAdd"('designs.name', true);
SELECT hk."fAccessScopeAdd"('designs.classes', true);
SELECT hk."fAccessScopeAdd"('designs.file', true);
SELECT hk."fAccessScopeAdd"('designs.custom_values', true);


SELECT hk."fAccessPermissionAdd"('assets.name', 'replica_assets', false, false, false);
SELECT hk."fAccessPermissionAdd"('assets.classes', 'replica_assets', false, false, false);
SELECT hk."fAccessPermissionAdd"('assets.file', 'replica_assets', false, false, false);
SELECT hk."fAccessPermissionAdd"('assets.custom_values', 'replica_assets', false, false, false);
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

SELECT hk."fAccessPermissionAdd"('assets.name', 'replica_assets_full', true, true, true);
SELECT hk."fAccessPermissionAdd"('assets.classes', 'replica_assets_full', true, true, true);
SELECT hk."fAccessPermissionAdd"('assets.file', 'replica_assets_full', true, true, true);
SELECT hk."fAccessPermissionAdd"('assets.custom_values', 'replica_assets_full', true, true, true);
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



GRANT SELECT ON TABLE adm."vPreferences" TO replica_playlist_full;


CREATE OR REPLACE FUNCTION pl."fPlaylistPlugAdd"(dtStart timestamp with time zone, nFramesQty integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	stPlug RECORD;
	nFrameOffset integer;
	idItems integer;
BEGIN
	SELECT p."idFiles", po."idPlugs", po."nFrameOffset", p."nFramesQty" INTO stPlug FROM pl."tPlugOffsets" po, pl."tPlugs" p WHERE nFramesQty <= p."nFramesQty"-po."nFrameOffset"+1 ORDER BY po."dtLastUsed" LIMIT 1;
	stRetVal := pl."fItemAddAsFile"(stPlug."idFiles", stPlug."nFramesQty", 'design_common', dtStart);
	idItems := stRetVal."nValue";
	stRetVal := pl."fPlaylistPlugUpdate"(idItems, dtStart, nFramesQty);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;








CREATE OR REPLACE FUNCTION pl."fItemAddAsAsset"(idAssets integer, idClasses integer, dtStart timestamp with time zone) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idRegisteredTables integer;
	idFiles integer;
	sName varchar(255);
	idItems integer;
	nFramesQty integer;
	nValue integer;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('media','tFiles');
	idRegisteredTables := stRetVal."nValue";
	SELECT "nValue" INTO idFiles FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables;
	---------------------------------------------------------
	SELECT "sName" INTO sName FROM mam."tAssets" WHERE "id"=idAssets;
	---------------------------------------------------------
	SELECT "nValue" INTO nFramesQty FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFramesQty'="sKey";
	IF nFramesQty IS NULL THEN
		nFramesQty := 0;
	END IF;
	stRetVal := pl."fItemPlannedAdd"(sName, idClasses, idFiles, dtStart, nFramesQty);
	idItems := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('mam','tAssets');
	idRegisteredTables := stRetVal."nValue";
	stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'asset', idAssets);
	---------------------------------------------------------
	SELECT "nValue" INTO nValue FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFrameIn'="sKey";
	IF nValue IS NOT NULL AND 1 < nValue THEN
		stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'nFrameStart', nValue);
	END IF;
	SELECT "nValue" INTO nValue FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFrameOut'="sKey";
	IF nValue IS NOT NULL AND (1 > nFramesQty OR nFramesQty > nValue) THEN
		stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'nFrameStop', nValue);
	END IF;
	---------------------------------------------------------
	stRetVal."nValue":=idItems;
	stRetVal."bValue":=true;

	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fDictionary"() RETURNS trigger AS
$Dictionary$
DECLARE
	sTableName text;
	sValue text;
	sType text;
	oRecord RECORD;
BEGIN
	sTableName := quote_ident(TG_TABLE_SCHEMA)||'.'||quote_ident(TG_TABLE_NAME);
	IF (TG_OP = 'DELETE') THEN
		IF 'cues' = TG_TABLE_SCHEMA THEN
			IF NOT EXISTS(SELECT id FROM cues."vBinds" WHERE OLD.id="nValue" AND TG_TABLE_SCHEMA=("oTableTarget")."sSchema" AND TG_TABLE_NAME=("oTableTarget")."sName") THEN
				RETURN OLD;
			END IF;
		END IF;
		RETURN NULL;
	ELSIF (TG_OP = 'UPDATE') THEN
		IF OLD."oValue" = NEW."oValue" THEN
			RETURN NEW;
		END IF;
		sType := typeof(ROW(TG_TABLE_SCHEMA,TG_TABLE_NAME),'oValue');
		sValue := NEW."oValue";
		IF 'name'=sType OR 'varchar'=sType OR 'bpchar'=sType OR 'text'=sType THEN
			sValue := quote_literal(sValue);
		END IF;
		EXECUTE 'INSERT INTO '||sTableName||' ("oValue") VALUES ('||sValue||') RETURNING *' INTO oRecord;
		RETURN oRecord;
	ELSIF (TG_OP = 'INSERT') THEN
		sType := typeof(ROW(TG_TABLE_SCHEMA,TG_TABLE_NAME),'oValue');
		sValue := NEW."oValue";
		IF 'name'=sType OR 'varchar'=sType OR 'bpchar'=sType OR 'text'=sType OR 'timestamp'=left(sType, 9) THEN
			sValue := quote_literal(sValue);
		END IF;
		EXECUTE 'SELECT * FROM '||sTableName||' WHERE "oValue"='||sValue INTO oRecord;
		IF oRecord.id IS NULL THEN
			IF 'cues' = TG_TABLE_SCHEMA THEN
				EXECUTE 'DELETE FROM '||sTableName||' WHERE id NOT IN (SELECT DISTINCT "nValue" FROM cues."vBinds" WHERE '||quote_literal(TG_TABLE_SCHEMA)||'=("oTableTarget")."sSchema" AND '||quote_literal(TG_TABLE_NAME)||'=("oTableTarget")."sName" AND "nValue" IS NOT NULL)';
			END IF;
			RETURN NEW;
		END IF;
	END IF;
	RETURN NULL;
END;
$Dictionary$ LANGUAGE 'plpgsql' VOLATILE;

----------------------------------- media."tStrings"
CREATE TABLE media."tStrings"
(
  id bigserial PRIMARY KEY,
  "oValue" text UNIQUE
)
WITHOUT OIDS;

SELECT hk."fRegisteredTableAdd"('media', 'tStrings');
CREATE TYPE target AS ENUM ('add', 'get');
CREATE OR REPLACE FUNCTION media."fDictionary"(eTarget target, sValue text) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tStrings';
	aColumns := '{{oValue,0}}';
	aColumns[1][2] := COALESCE(sValue::text,'NULL'); --чтобы не морочиться с экранированием
	IF 'add' = eTarget THEN 
		stRetVal := "fTableAdd"(stTable, aColumns, true);
		IF NOT stRetVal."bValue" AND stRetVal."nValue" IS NULL THEN
			stRetVal := "fTableGet"(stTable, aColumns);
		END IF;
		--RAISE NOTICE '%,%', stRetVal."bValue", stRetVal."nValue";
	ELSIF 'get' = eTarget THEN
		stRetVal := "fTableGet"(stTable, aColumns);
	ELSE
		RAISE EXCEPTION 'NOT IMPLEMENTED [target:%]', eTarget;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fDictionaryGet"(sValue text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fDictionary"('get', sValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fDictionaryAdd"(sValue text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fDictionary"('add', sValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fDictionaryGarbageCollector"() RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRecord RECORD;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('media','tStrings');
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT GET A REGISTERED TABLE ENTRY FOR media."tStrings"';
	END IF;
	idRegisteredTables := stRetVal."nValue";
	DELETE FROM media."tStrings" WHERE id NOT IN (SELECT d.id FROM media."tFileAttributes" ta, media."tStrings" d WHERE idRegisteredTables = ta."idRegisteredTables" AND d.id = ta."nValue");
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeSet"(idFiles bigint, sKey character varying, sValue text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idRegisteredTables integer;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('media', 'tStrings');
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT GET A REGISTERED TABLE ENTRY FOR media."tStrings"';
	END IF;
	idRegisteredTables := stRetVal."nValue";
	stRetVal := media."fFileAttributeGet"(idFiles, idRegisteredTables, sKey);
	IF NOT stRetVal."bValue" THEN
		stRetVal := media."fDictionaryAdd"(sValue);
		IF NOT stRetVal."bValue" THEN
			RAISE EXCEPTION 'CANNOT ADD A DICTIONARY VALUE [%]', sValue;
		END IF;
		stRetVal := media."fFileAttributeAdd"(idFiles, idRegisteredTables, sKey, stRetVal."nValue");
	ELSE
		UPDATE media."tStrings" SET "oValue"=sValue WHERE id=stRetVal."nValue";
		stRetVal."bValue" := true;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeRemove"(aColumns text[][]) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFileAttributes';
	stRetVal := "fTableRemove"(stTable, aColumns);
	PERFORM media."fDictionaryGarbageCollector"();
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeRemove"(idFiles bigint, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := media."fFileAttributeRemove"(aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeRemove"(idFiles bigint, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := media."fFileAttributeRemove"(aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeRemove"(idFiles bigint, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{sKey,0}}';
	aColumns[2][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := media."fFileAttributeRemove"(aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;




CREATE OR REPLACE FUNCTION "fTableAdd"(stTable table_name, aColumnValues anyarray, bForceAdd boolean) RETURNS int_bool AS
$$
DECLARE
	nColumnValuesLength integer;
	idTable integer;
	stRetVal int_bool;
	sColumn text;
	sValue text;
	sType text;
	sSQL text;
	sSQLColumns text;
	sSQLValues text;
	nID integer;
BEGIN
--RAISE EXCEPTION 'FUNCTION "fTableAdd" [stTable: %.%] [nColumnValuesLength: %] [bForceAdd: %]', stTable."sSchema", stTable."sName", nColumnValuesLength, bForceAdd;
	IF NOT bForceAdd THEN
		SELECT "nValue" INTO idTable FROM "fTableGet"(stTable,aColumnValues);
		IF idTable IS NOT NULL THEN
			stRetVal."nValue" := idTable;
			stRetVal."bValue" := false;
			RETURN stRetVal;
		END IF;
	END IF;
	sSQL:='INSERT INTO '||quote_ident(stTable."sSchema")||'.'||quote_ident(stTable."sName")||'(id';
	sSQLColumns := '';
	sSQLValues := '';
	nID := -1;
	nColumnValuesLength := array_upper(aColumnValues, 1);
	FOR nValIndx IN 1..nColumnValuesLength LOOP
		sColumn := aColumnValues[nValIndx][1]::text;
		sValue := aColumnValues[nValIndx][2]::text;
		IF sColumn IS NOT NULL THEN
--RAISE NOTICE 'FUNCTION "fTableAdd" [sType: %]', sType;
			IF 'id' = sColumn THEN
				nID := sValue::integer;
			ELSE
				sType := typeof(stTable,sColumn);
				IF 'name'=sType OR 'varchar'=sType OR 'bpchar'=sType OR 'text'=sType THEN
					sValue := quote_literal(sValue);
				END IF;
			END IF;
			sSQLColumns:=sSQLColumns || ', ' || quote_ident(sColumn);
			sSQLValues:=sSQLValues || ', ' || COALESCE(sValue,'NULL');
		END IF;
	END LOOP;
	sSQL:=sSQL || sSQLColumns || ') VALUES(DEFAULT' || sSQLValues || ')';
	EXECUTE sSQL || ' RETURNING id' INTO nID;
	--RAISE NOTICE 'FUNCTION "fTableAdd" [nID:%] [sSQL: %] [sSQLColumns: %] [sSQLValues: %] [currval:%]', nID,  sSQL, sSQLColumns, sSQLValues, currval(stTable."sSchema"||'."'||stTable."sName"||'_id_seq"');
	IF nID IS NULL AND EXISTS (SELECT c.oid FROM pg_trigger t, pg_class c, pg_proc p, pg_namespace n WHERE t.tgrelid=c.oid AND t.tgfoid=p.oid AND p.proname='fDictionary' AND c.relnamespace = n.oid AND stTable."sSchema"=n.nspname AND stTable."sName"=c.relname) THEN
		SELECT "nValue" INTO nID FROM "fTableGet"(stTable,aColumnValues);
	END IF;
	IF nID IS NOT NULL THEN
		IF 0 > nID THEN
			nID = currval(stTable."sSchema"||'."'||stTable."sName"||'_id_seq"');
		END IF;
		stRetVal := "fTableGet"(stTable, nID);
	ELSE
		stRetVal."nValue" := NULL;
		stRetVal."bValue" := false;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION "fTableAdd"(oTable table_name, aColumnValues anyarray, bForceAdd boolean, bException boolean) RETURNS bigint AS
$$
DECLARE
	oRetVal int_bool;
BEGIN
	oRetVal := "fTableAdd"(oTable, aColumnValues, bForceAdd);
	IF bException AND NOT oRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT ADD RECORD TO TABLE [%][%]', oTable, aColumnValues;
	END IF;
	RETURN oRetVal."nValue";
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGetText"(stTable table_name, aColumnValues anyarray, sTargetColumnName text) RETURNS text_bool AS
$$
DECLARE
	nColumnValuesLength integer;
	sTargetValue text;
	stRetVal text_bool;
	sColumn text;
	sValue text;
	sType text;
	sSQL text;
BEGIN
--RAISE EXCEPTION 'FUNCTION "fTableGet" [stTable: %.%] [nColumnValuesLength: %]', stTable."sSchema", stTable."sName", nColumnValuesLength;
	sSQL:='SELECT '||quote_ident(sTargetColumnName)||'::text FROM '||quote_ident(stTable."sSchema")||'.'||quote_ident(stTable."sName")||' WHERE true';
	nColumnValuesLength := array_upper(aColumnValues, 1);
	FOR nValIndx IN 1..nColumnValuesLength LOOP
		sColumn := aColumnValues[nValIndx][1]::text;
		sValue := aColumnValues[nValIndx][2]::text;
		IF sColumn IS NOT NULL THEN
			sType := typeof(stTable,sColumn);
			IF 'name'=sType OR 'varchar'=sType OR 'bpchar'=sType OR 'text'=sType THEN
				sValue := quote_literal(sValue);
			END IF;
			sSQL:=sSQL || ' AND ' || quote_ident(sColumn) || COALESCE(' = '||sValue,' IS NULL');
		ELSEIF sValue IS NOT NULL THEN
			sSQL:=sSQL || ' AND ' || sValue;
		END IF;
	END LOOP;
	--RAISE EXCEPTION '[%]', sSQL;
	IF sSQL IS NULL THEN
		stRetVal."sValue" := null;
		stRetVal."bValue" := false;
		-- RAISE EXCEPTION 'aColumnValues:% nColumnValuesLength:%', aColumnValues, nColumnValuesLength;
		-- RAISE EXCEPTION 'FUNCTION "fTableGet" [sSQL: %] [sType: %] [sValue: %] [sColumn: %] [stTable: %.%] [nColumnValuesLength: %]', sSQL, sType, sValue, sColumn, stTable."sSchema", stTable."sName", nColumnValuesLength;
	ELSE
		EXECUTE sSQL INTO sTargetValue;
		stRetVal."sValue" := sTargetValue;
		stRetVal."bValue" := true;
	END IF;
	/*
--RAISE EXCEPTION 'FUNCTION "fTableGet" [sSQL: %] [sType: %] [sValue: %] [sColumn: %] [idTable:%] [stTable: %.%] [nColumnValuesLength: %]', sSQL, sType, sValue, sColumn, idTable, stTable."sSchema", stTable."sName", nColumnValuesLength;
	IF 'id'=sColumn AND idTable IS NULL THEN
		SELECT id INTO idTable FROM mam."tAssets";
		RAISE EXCEPTION 'FUNCTION "fTableGet" [sSQL: %] [sType: %] [sValue: %] [sColumn: %] [idTable:%] [stTable: %.%] [nColumnValuesLength: %]', sSQL, sType, sValue, sColumn, idTable, stTable."sSchema", stTable."sName", nColumnValuesLength;
	END IF;
	*/
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION "fTableRemove"(stTable table_name, aColumnValues anyarray) RETURNS int_bool AS
$$
DECLARE
	nColumnValuesLength integer;
	idTable integer;
	stRetVal int_bool;
	sColumn text;
	sValue text;
	sType text;
	sSQL text;
	nRowsQty integer;
BEGIN
--RAISE EXCEPTION 'FUNCTION "fTableAdd" [stTable: %.%] [nColumnValuesLength: %] [bForceAdd: %]', stTable."sSchema", stTable."sName", nColumnValuesLength, bForceAdd;
	SELECT "nValue" INTO idTable FROM "fTableGet"(stTable,aColumnValues) LIMIT 1;
	IF idTable IS NULL THEN
		stRetVal."nValue" := idTable;
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	sSQL:=' FROM '||quote_ident(stTable."sSchema")||'.'||quote_ident(stTable."sName")||' WHERE true';
	nColumnValuesLength := array_upper(aColumnValues, 1);
	FOR nValIndx IN 1..nColumnValuesLength LOOP
		sColumn := aColumnValues[nValIndx][1]::text;
		sValue := aColumnValues[nValIndx][2]::text;
		IF sColumn IS NOT NULL THEN
			sType := typeof(stTable,sColumn);
			IF 'name'=sType OR 'varchar'=sType OR 'bpchar'=sType OR 'text'=sType THEN
				sValue := quote_literal(sValue);
			END IF;
			sSQL := sSQL || ' AND ' || quote_ident(sColumn) || COALESCE(' = '||sValue,' IS NULL');
		END IF;
	END LOOP;
	IF sSQL IS NULL THEN
--		RAISE EXCEPTION 'aColumnValues:% nColumnValuesLength:%', aColumnValues, nColumnValuesLength;
		RAISE EXCEPTION 'FUNCTION "fTableRemove" [sSQL: %] [sType: %] [sValue: %] [sColumn: %] [idTable:%] [stTable: %.%] [nColumnValuesLength: %]', sSQL, sType, sValue, sColumn, idTable, stTable."sSchema", stTable."sName", nColumnValuesLength;
	END IF;
	EXECUTE 'SELECT count(*) as count'||sSQL INTO nRowsQty;
	stRetVal."nValue" := nRowsQty;
	stRetVal."bValue" := false;
	IF 0 < nRowsQty THEN
		EXECUTE 'DELETE'||sSQL;
		stRetVal."bValue" := true;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;




CREATE OR REPLACE FUNCTION hk."fRegisteredTableGet"(oTargetTable table_name, bException boolean) RETURNS integer AS
$$
DECLARE
	oRetVal int_bool;
BEGIN
	oRetVal := hk."fRegisteredTableGet"(oTargetTable);
	IF bException AND NOT oRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT FIND SPECIFIED TABLE [%]', oTargetTable;
	END IF;
	RETURN oRetVal."nValue";
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableGet"(sSchema name, sName name, bException boolean) RETURNS integer AS
$$
DECLARE
	nRetVal integer;
BEGIN
	nRetVal := hk."fRegisteredTableGet"(ROW(sSchema, sName), bException);
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

------------------------------------ Binds
CREATE OR REPLACE FUNCTION "fBindTypeGet"(oTableSource table_name, oTableTarget table_name, sName name, bException boolean) RETURNS bigint AS
$$
DECLARE
	oTable table_name;
	aColumns text[][];
	nRetVal bigint;
BEGIN
	oTable."sSchema":=oTableSource."sSchema";
	oTable."sName":='tBindTypes';
	aColumns := '{{idTableSource,'||hk."fRegisteredTableGet"(oTableSource, bException)::text||'},{idTableTarget,0},{sName,0}}';
	IF oTableTarget IS NULL THEN
		aColumns[2][2] := NULL;
	ELSE
		aColumns[2][2] := hk."fRegisteredTableGet"(oTableTarget, bException)::text;
	END IF;
	aColumns[3][2] := COALESCE(sName::text,'NULL');
	nRetVal := "fTableGet"(oTable, aColumns, bException);
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION "fBindAdd"(oTableSource table_name, idSource bigint, oTableTarget table_name, sName name, nValue bigint, bException boolean) RETURNS bigint AS
$$
DECLARE
	idBindTypes integer;
	oTable table_name;
	aColumns text[][];
	nRetVal bigint;
BEGIN
	idBindTypes := "fBindTypeGet"(oTableSource, oTableTarget, sName, bException);
	IF idBindTypes IS NULL THEN
		IF bException THEN
			RAISE EXCEPTION 'fBindAdd:Wrong bind type';
		END IF;
		RETURN -1;
	END IF;
	oTable."sSchema":=oTableSource."sSchema";
	oTable."sName":='tBinds';
	aColumns := '{{idBindTypes,'||idBindTypes::text||'},{idSource,'||idSource::text||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	nRetVal := "fTableAdd"(oTable, aColumns, false, bException);
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fBindGet"(oTableSource table_name, idSource bigint, oTableTarget table_name, sName name, nValue bigint, bException boolean) RETURNS bigint AS
$$
DECLARE
	idBindTypes integer;
	oTable table_name;
	aColumns text[][];
	nRetVal bigint;
BEGIN
	idBindTypes := "fBindTypeGet"(oTableSource, oTableTarget, sName, bException);
	oTable."sSchema":=oTableSource."sSchema";
	oTable."sName":='tBinds';
	aColumns := '{{idBindTypes,'||idBindTypes::text||'},{idSource,'||idSource::text||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	nRetVal := "fTableGet"(oTable, aColumns, bException);
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fBindGet"(oTableSource table_name, idSource bigint, sName name, bException boolean) RETURNS bigint AS
$$
DECLARE
	idBindTypes integer;
	oTable table_name;
	aColumns text[][];
	nRetVal bigint;
BEGIN
	idBindTypes := "fBindTypeGet"(oTableSource, null, sName, bException);
	oTable."sSchema":=oTableSource."sSchema";
	oTable."sName":='tBinds';
	aColumns := '{{idBindTypes,'||idBindTypes::text||'},{idSource,'||idSource::text||'}}';
	nRetVal := "fTableGet"(oTable, aColumns, bException);
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;


CREATE OR REPLACE FUNCTION "fBindSet"(oTableSource table_name, idSource bigint, oTableTarget table_name, sName name, nValue bigint, bException boolean) RETURNS bigint AS
$$
DECLARE
	nRetVal bigint;
BEGIN
	nRetVal := "fBindGet"(oTableSource, idSource, oTableTarget, sName, nValue, false);
	IF nRetVal IS NOT NULL THEN
		EXECUTE 'UPDATE "' || oTableSource."sSchema" || '"."tBinds" SET "nValue"='||COALESCE(nValue::text,'NULL')||' WHERE id='||nRetVal::text;
	ELSE
		nRetVal := "fBindAdd"(oTableSource, idSource, oTableTarget, sName, nValue, bException);
	END IF;
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fBindValueGet"(oTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idTables integer,  sKey character varying) RETURNS bigint AS
$$
DECLARE
	idBindTypes integer;
	oTable table_name;
	aColumns text[][];
	nRetVal bigint;
BEGIN
	idBindTypes := "fBindTypeGet"(oTableSource, oTableTarget, sName, bException);
	oTable."sSchema":=oTableSource."sSchema";
	oTable."sName":='tBinds';
	aColumns := '{{idBindTypes,'||idBindTypes::text||'},{idSource,'||idSource::text||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	nRetVal := "fTableGet"(oTable, aColumns, bException);

	EXECUTE 'SELECT "nValue" FROM "tTables" WHERE id='||nRetVal::text INTO nRetVal;
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

------------------------------------ cues."tBindTypes"
CREATE TABLE cues."tBindTypes"
(
  id serial PRIMARY KEY,
  "idTableSource" integer NOT NULL,  -- ссылка на таблицу привязки
  "idTableTarget" integer,  -- ссылка на таблицу значений
  "bUnique" boolean NOT NULL DEFAULT false,
  "sName" name,
  UNIQUE("idTableSource", "sName")
) 
WITHOUT OIDS;
------------------------------------ cues."tBinds"
CREATE TABLE cues."tBinds"
(
  id bigserial PRIMARY KEY,
  "idBindTypes" integer NOT NULL,
  "idSource" bigint NOT NULL,
  "nValue" bigint,
  FOREIGN KEY ("idBindTypes") REFERENCES "tBindTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;
------------------------------------ cues."tStrings"
CREATE TABLE cues."tStrings"
(
  id bigserial PRIMARY KEY,
  "oValue" text UNIQUE
) 
WITHOUT OIDS;
CREATE TRIGGER "cues_tStrings_fDictionary"
  BEFORE INSERT OR UPDATE OR DELETE
  ON cues."tStrings"
  FOR EACH ROW
  EXECUTE PROCEDURE "fDictionary"();

------------------------------------ cues."tTimestamps"
CREATE TABLE cues."tTimestamps"
(
  id bigserial PRIMARY KEY,
  "oValue" timestamp with time zone UNIQUE
) 
WITHOUT OIDS;
CREATE TRIGGER "cues_tTimestamps_fDictionary"
  BEFORE INSERT OR UPDATE OR DELETE
  ON cues."tTimestamps"
  FOR EACH ROW
  EXECUTE PROCEDURE "fDictionary"();
----------------------------------- cues."tPlugins"
CREATE TABLE cues."tPlugins"
(
	id serial PRIMARY KEY,
	"sName" name
) 
WITHOUT OIDS;
CREATE TYPE tINP AS ( --IdNamePair
	id integer,
	"sName" name
);
CREATE TYPE cues.tPluginPlaylistItem AS (
	id bigint,
	"oStatus" tINP,
	"oAsset" mam."vAssetsResolved",
	"dtStarted" timestamp with time zone
);
CREATE TYPE cues.tPluginPlaylist AS (
	id bigint,
	"sName" name,
	"dtStart" timestamp with time zone,
	"dtStop" timestamp with time zone,
	"aItems" cues.tPluginPlaylistItem[]
);

SELECT hk."fRegisteredTableAdd"('cues', 'tBindTypes');
SELECT hk."fRegisteredTableAdd"('cues', 'tBinds');
SELECT hk."fRegisteredTableAdd"('cues', 'tStrings');
SELECT hk."fRegisteredTableAdd"('cues', 'tTimestamps');
SELECT hk."fRegisteredTableAdd"('cues', 'tPlugins');

CREATE OR REPLACE VIEW cues."vPluginPlaylistItems" AS
	SELECT ROW(b.id, s."oStatus", ROW(a.*), st."oValue")::cues.tPluginPlaylistItem as "oItem"
		FROM hk."tRegisteredTables" t, mam."vAssetsResolved" a, cues."vBinds" b
			LEFT JOIN
			(
				SELECT i.id, ROW(s.id, s."sName")::tINP as "oStatus"
					FROM cues."tBinds" i, cues."vBinds" b, hk."tRegisteredTables" t, pl."tStatuses" s
					WHERE (b."oTableTarget").id = t.id AND 'pl' = t."sSchema" AND 'tStatuses' = t."sName" AND b."nValue" = s.id AND i.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'status'=b."sName"
			) s ON b.id=s.id		
			LEFT JOIN
			(
					SELECT i.id, t."oValue" 
						FROM cues."tBinds" i, cues."vBindTimestamps" t
						WHERE i.id=t."idSource" AND 'cues' = (t."oTableSource")."sSchema" AND 'tBinds' = (t."oTableSource")."sName" AND 'stop'=t."sName"
			) st ON b.id=st.id		
		WHERE (b."oTableTarget").id = t.id AND 'mam' = t."sSchema" AND 'tAssets' = t."sName" AND b."nValue" = a.id AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'item'=b."sName";
CREATE OR REPLACE VIEW cues."vPluginPlaylists" AS
	SELECT ROW(p.id, pn."oValue", pst."oValue", psp."oValue", pa."aItems")::cues.tPluginPlaylist as "oPlaylist"
		FROM 
			(
				SELECT p.id, s."oValue" 
					FROM cues."tBinds" p, cues."vBindStrings" s
					WHERE p.id=s."idSource" AND 'cues' = (s."oTableSource")."sSchema" AND 'tBinds' = (s."oTableSource")."sName" AND 'name'=s."sName"
			) pn,
			(
				SELECT p.id, t."oValue" 
					FROM cues."tBinds" p, cues."vBindTimestamps" t
					WHERE p.id=t."idSource" AND 'cues' = (t."oTableSource")."sSchema" AND 'tBinds' = (t."oTableSource")."sName" AND 'start'=t."sName"
			) pst,
			(
				SELECT p.id, t."oValue" 
					FROM cues."tBinds" p, cues."vBindTimestamps" t
					WHERE p.id=t."idSource" AND 'cues' = (t."oTableSource")."sSchema" AND 'tBinds' = (t."oTableSource")."sName" AND 'stop'=t."sName"
			) psp,
			(	
				SELECT b.id
					FROM cues."tPlugins" p, cues."vBinds" b, hk."tRegisteredTables" rt
					WHERE (b."oTableSource").id=rt.id AND 'cues'=rt."sSchema" AND 'tPlugins'=rt."sName" AND b."idSource" = p.id AND 'playlist'=p."sName" AND 'instance'=b."sName"
			) p
			LEFT JOIN
			(
				SELECT p.id, array_agg(i."oItem") as "aItems"
					FROM cues."tBinds" p, cues."vBinds" b, hk."tRegisteredTables" t, cues."vPluginPlaylistItems" i
					WHERE (b."oTableTarget").id = t.id AND b.id = (i."oItem").id AND p.id=b."idSource"
					GROUP BY p.id
			) pa ON p.id=pa.id
		WHERE p.id=pn.id AND p.id=pst.id AND p.id=psp.id;

INSERT INTO cues."tPlugins" ("sName") VALUES ('playlist');

INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tPlugins'))."nValue", NULL, false, 'instance');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('cues', 'tStrings'))."nValue", true, 'name');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('cues', 'tTimestamps'))."nValue", true, 'start');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('cues', 'tTimestamps'))."nValue", true, 'stop');
INSERT INTO cues."tBindTypes" VALUES (DEFAULT, (hk."fRegisteredTableGet"('cues', 'tBinds'))."nValue", (hk."fRegisteredTableGet"('mam', 'tAssets'))."nValue", false, 'item');

CREATE OR REPLACE FUNCTION cues."fPluginPlaylistItemSave"(oPLI cues.tPluginPlaylistItem, bException bool) RETURNS cues.tPluginPlaylistItem AS
$BODY$
DECLARE
	nValue bigint;
	nID bigint;
	oRetVal cues.tPluginPlaylistItem;
BEGIN
	SELECT "oItem" INTO oRetVal FROM cues."vPluginPlaylistItems" WHERE oPLI.id=("oItem").id;
	IF oRetVal IS NULL THEN
		IF bException THEN
			RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST ITEM [%]', oPLI.id;
		END IF;
		RETURN NULL;
	END IF;
	IF oRetVal."oStatus" <> oPLI."oStatus" OR (oRetVal."oStatus").id <> (oPLI."oStatus").id THEN
		IF EXISTS(SELECT b.id INTO nID FROM cues."vBinds" b WHERE oPLI.id = b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'pl' = (i."oTableTarget")."sSchema" AND 'tStatuses' = (i."oTableTarget")."sName" AND 'status'=b."sName") THEN
			UPDATE cues."tBinds" SET "nValue"=(oPLI."oStatus").id WHERE nID=id;
		ELSIF oPLI."oStatus" IS NOT NULL THEN
			PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('pl','tStatuses'), 'status', (oPLI."oStatus").id, true);
		END IF;
	END IF;
	IF oRetVal."oAsset" <> oPLI."oAsset" OR (oRetVal."oAsset").id <> (oPLI."oAsset").id THEN
		SELECT b.id INTO nID FROM cues."vBinds" b WHERE oPLI.id=b.id;
		IF FOUND THEN
			UPDATE cues."tBinds" SET "nValue"=(oPLI."oAsset").id WHERE nID=id;
		ELSIF oPLI."oStatus" IS NOT NULL THEN
			PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
		END IF;
	END IF;
	IF oRetVal."dtStarted" <> oPLI."dtStarted" THEN
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
	SELECT * INTO oRetVal FROM cues."vPluginPlaylistItems" WHERE oPLI.id=("oItem").id;
	RETURN oRetVal;
END;
$BODY$
  LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION cues."fPluginPlaylistSave"(oPlaylist cues.tPluginPlaylist, bException bool) RETURNS cues.tPluginPlaylist AS
$BODY$
DECLARE
	oTable table_name;
	nValue bigint;
	oValue record;
	oPLI cues.tPluginPlaylistItem;
BEGIN
	
	oTable := ROW('cues','tBinds');
	IF oPlaylist.id IS NOT NULL THEN
		SELECT * INTO oValue FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
		oValue := oValue."oPlaylist";
		IF oValue IS NULL THEN
			IF bException THEN
				RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST [%]', ROW(oPlaylist.id, oPlaylist."sName");
			END IF;
			RETURN NULL;
		END IF;
		IF oPlaylist."sName" <> oValue."sName" THEN
			nValue := "fTableAdd"(ROW('cues','tStrings'), ('{{oValue, ' || oPlaylist."sName" || '}}')::text[][], true, false);
			UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindStrings" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'name'=b."sName");
		END IF;
		IF oPlaylist."dtStart" <> oValue."dtStart" THEN
			nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStart") || '}}')::text[][], true, false);
			UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindTimestamps" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'start'=b."sName");
		END IF;
		IF oPlaylist."dtStop" <> oValue."dtStop" THEN
			nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStop") || '}}')::text[][], true, false);
			UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindTimestamps" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'stop'=b."sName");
		END IF;
		FOR oPLI IN SELECT * FROM unnest(oValue."aItems") ac WHERE ac.id NOT IN (SELECT an.id FROM unnest(oPlaylist."aItems") an) LOOP
			DELETE FROM cues."tBinds" WHERE oPLI.id=id OR id IN(SELECT b.id FROM cues."vBinds" b WHERE oPLI.id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName");
		END LOOP;
		FOR oPLI IN SELECT * FROM unnest(oPlaylist."aItems") ac WHERE ac.id NOT IN (SELECT an.id FROM unnest(oValue."aItems") an) LOOP
			oPLI.id := "fBindAdd"(oTable, oPlaylist.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
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
		FOR oPLI IN SELECT * FROM unnest(oPlaylist."aItems") a LOOP
			oPLI.id := "fBindAdd"(oTable, oPlaylist.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
		END LOOP;
	END IF;

	FOR oPLI IN SELECT * FROM unnest(oPlaylist."aItems") LOOP
		PERFORM cues."fPluginPlaylistItemSave"(oPLI, true);
	END LOOP;

	SELECT * INTO oValue FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
	RETURN oValue."oPlaylist";
END;
$BODY$
  LANGUAGE plpgsql VOLATILE;
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