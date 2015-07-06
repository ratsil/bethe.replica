/********************************** media."tStorageTypes" *********************************/
CREATE OR REPLACE FUNCTION media."fStorageTypeGet"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tStorageTypes';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fStorageTypeAdd"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tStorageTypes';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** media."tStorages" *************************************/
CREATE OR REPLACE FUNCTION media."fStorageGet"(idStorageTypes integer, sName character varying, sPath text, bEnabled boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tStorages';
	aColumns := '{{idStorageTypes,'||COALESCE(idStorageTypes::text,'NULL')||'},{sName,0},{sPath,0},{bEnabled,'||CASE WHEN bEnabled IS NULL THEN 'NULL' WHEN bEnabled THEN 'true' ELSE 'false' END||'}}';
	aColumns[2][2] := COALESCE(sName,'NULL'); --чтобы не морочиться с экранированием
	aColumns[3][2] := COALESCE(sPath,'NULL');
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fStorageGet"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tStorages';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := COALESCE(sName,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fStorageAdd"(idStorageTypes integer, sName character varying, sPath text, bEnabled boolean) RETURNS  int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tStorages';
	aColumns := '{{idStorageTypes,'||COALESCE(idStorageTypes::text,'NULL')||'},{sName,0},{sPath,0},{bEnabled,'||CASE WHEN bEnabled IS NULL THEN 'NULL' WHEN bEnabled THEN 'true' ELSE 'false' END||'}}';
	aColumns[2][2] := COALESCE(sName,'NULL'); --чтобы не морочиться с экранированием
	aColumns[3][2] := COALESCE(sPath,'NULL');
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** media."tFiles" ****************************************/
CREATE OR REPLACE FUNCTION media."fFileGet"(idStorages integer, sFilename text) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFiles';
	aColumns := '{{idStorages,'||idStorages||'},{sFilename,0}}';
	aColumns[2][2] := sFilename; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAdd"(idStorages integer, sFilename text) RETURNS  int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFiles';
	aColumns := '{{idStorages,'||idStorages||'},{sFilename,0}}';
	aColumns[2][2] := sFilename; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** media."tFileAttributes" *******************************/
CREATE OR REPLACE FUNCTION media."fFileAttributeGet"(idFiles bigint, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFileAttributes';
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeGet"(idFiles bigint, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fFileAttributeGet"(idFiles, idRegisteredTables, NULL, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION media."fFileAttributeGet"(idFiles bigint, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFileAttributes';
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeGet"(idFiles bigint, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fFileAttributeGet"(idFiles, NULL, sKey);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeValueGet"(idFiles bigint, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFileAttributes';
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGetInt"(stTable, aColumns, 'nValue');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeAdd"(idFiles bigint, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tFileAttributes';
	aColumns := '{{idFiles,'||COALESCE(idFiles::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeAdd"(idFiles bigint, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fFileAttributeAdd"(idFiles, idRegisteredTables, NULL, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeAdd"(idFiles bigint, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fFileAttributeAdd"(idFiles, NULL, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeSet"(idFiles bigint, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fFileAttributeGet"(idFiles, idRegisteredTables, sKey);
	IF NOT stRetVal."bValue" THEN
		stRetVal := media."fFileAttributeAdd"(idFiles, idRegisteredTables, sKey, nValue);
	ELSE
		UPDATE media."tFileAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
		stRetVal."bValue" := true;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION media."fFileAttributeSet"(idFiles bigint, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := media."fFileAttributeGet"(idFiles, sKey);
	IF NOT stRetVal."bValue" THEN
		stRetVal := media."fFileAttributeAdd"(idFiles, NULL, sKey, nValue);
	ELSE
		UPDATE media."tFileAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
		stRetVal."bValue":=true;
	END IF;
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
	stRetVal := hk."fRegisteredTableGet"('media', 'tDictionary');
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT GET A REGISTERED TABLE ENTRY FOR media."tDictionary"';
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
		UPDATE media."tDictionary" SET "sValue"=sValue WHERE id=stRetVal."nValue";
		stRetVal."bValue":=true;
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

CREATE OR REPLACE FUNCTION media."fFileErrorSet"(idFiles bigint, eError error) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idErrorScopes integer;
BEGIN
	IF idFiles IS NULL THEN
		RAISE EXCEPTION 'FILE ID CAN''T BE NULL';
	END IF;
	IF eError IS NULL THEN
		RAISE EXCEPTION 'ERROR CAN''T BE NULL';
	END IF;
	stRetVal := hk."fRegisteredTableGet"('media','tFiles');
	IF stRetVal."bValue" THEN
		stRetVal := hk."fErrorScopeGet"(stRetVal."nValue", eError);
		IF stRetVal."bValue" THEN
			idErrorScopes := stRetVal."nValue"; 
			stRetVal := hk."fRegisteredTableGet"('hk','tErrorScopes');
			IF stRetVal."bValue" THEN
				stRetVal := media."fFileAttributeSet"(idFiles, stRetVal."nValue", 'error', idErrorScopes);
			END IF;
		END IF;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

/********************************** media."tDictionary" ***********************************/
CREATE OR REPLACE FUNCTION media."fDictionary"(eTarget target, sValue text) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='media';
	stTable."sName":='tDictionary';
	aColumns := '{{sValue,0}}';
	aColumns[1][2] := COALESCE(sValue::text,'NULL'); --чтобы не морочиться с экранированием
	IF 'add' = eTarget THEN 
		stRetVal := "fTableAdd"(stTable, aColumns,true);
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
	stRetVal := hk."fRegisteredTableGet"('media','tDictionary');
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT GET A REGISTERED TABLE ENTRY FOR media."tDictionary"';
	END IF;
	idRegisteredTables := stRetVal."nValue";
	DELETE FROM media."tDictionary" WHERE id NOT IN (SELECT d.id FROM media."tFileAttributes" ta, media."tDictionary" d WHERE idRegisteredTables = ta."idRegisteredTables" AND d.id = ta."nValue");
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

/********************************** OTHER *************************************************/

CREATE OR REPLACE FUNCTION media."fFileRemove"(idFiles bigint) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	IF idFiles IS NULL THEN
		RAISE EXCEPTION 'FILE ID CAN''T BE NULL';
	END IF;
	DELETE FROM media."tFileAttributes" WHERE "idFiles"=idFiles;
	DELETE FROM media."tFiles" WHERE id=idFiles;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idFiles;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

