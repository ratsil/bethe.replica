--CREATE DATABASE "replica" WITH OWNER = user ENCODING = 'UTF8';
/*CREATE OR REPLACE FUNCTION plpgsql_call_handler()
  RETURNS language_handler AS
'$libdir/plpgsql', 'plpgsql_call_handler'
  LANGUAGE 'c' VOLATILE;
ALTER FUNCTION plpgsql_call_handler() OWNER TO user;

CREATE OR REPLACE FUNCTION plpgsql_validator(oid)
  RETURNS void AS
'$libdir/plpgsql', 'plpgsql_validator'
  LANGUAGE 'c' VOLATILE;
ALTER FUNCTION plpgsql_validator(oid) OWNER TO user;

CREATE TRUSTED PROCEDURAL LANGUAGE plpgsql
    HANDLER plpgsql_call_handler
    VALIDATOR plpgsql_validator;*/

CREATE USER replica_init LOGIN ENCRYPTED PASSWORD  '' INHERIT VALID UNTIL 'infinity';
GRANT ALL ON DATABASE "replica" TO replica_init;
SET SESSION AUTHORIZATION 'replica_init';

CREATE TYPE int_bool AS ("nValue" integer, "bValue" boolean);
CREATE TYPE text_bool AS ("sValue" text, "bValue" boolean);
CREATE TYPE table_name AS ("sSchema" text, "sName" text);
CREATE TYPE error AS ENUM ('unknown', 'missed');
CREATE TYPE target AS ENUM ('add', 'get');
CREATE TYPE tINP AS ( --IdNamePair
	id integer,
	"sName" name
);

CREATE OR REPLACE FUNCTION "typeof"(stTable table_name, sColumnName text) RETURNS text AS
$$
DECLARE
	sRetVal text;
BEGIN
	SELECT tp.typname INTO sRetVal FROM pg_namespace s, pg_class t, pg_attribute c, pg_type tp WHERE s.oid=t.relnamespace AND t.oid=c.attrelid AND c.atttypid=tp.oid AND s.nspname=stTable."sSchema" AND t.relname=stTable."sName" AND c.attname=sColumnName;
	RETURN sRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fMicrosecondsElapsed"(dt timestamp)
  RETURNS int AS
$$
DECLARE
	nRetVal integer;
BEGIN
	nRetVal := date_part('epoch', clock_timestamp() - dt) * 1000000;
	RETURN nRetVal;
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

CREATE OR REPLACE FUNCTION "fTableGetInt"(stTable table_name, aColumnValues anyarray, sTargetColumnName text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	stTB text_bool;
BEGIN
	stTB := "fTableGetText"(stTable, aColumnValues, sTargetColumnName);
	IF stTB."sValue" IS NULL THEN
		stRetVal."bValue" := false;
		stRetVal."nValue" := NULL;
	ELSE
		stRetVal."bValue" := stTB."bValue";
		stRetVal."nValue" := stTB."sValue"::int;
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGetID"(stTable table_name, aColumnValues anyarray) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fTableGetInt"(stTable, aColumnValues, 'id');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGetID"(stTable table_name, sName text) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGetID"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGetName"(stTable table_name, id integer) RETURNS text_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal text_bool;
BEGIN
	aColumns := '{{id,'||COALESCE(id::text,'NULL')||'}}';
	stRetVal := "fTableGetText"(stTable, aColumns, 'sName');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGet"(stTable table_name, nID integer) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	aColumns := '{{id,'||nID||'}}'; 
	stRetVal := "fTableGetID"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGet"(stTable table_name, aColumnValues anyarray) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fTableGetID"(stTable, aColumnValues);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fTableGet"(oTable table_name, aColumnValues anyarray, bException boolean) RETURNS bigint AS
$$
DECLARE
	oRetVal int_bool;
BEGIN
	oRetVal := "fTableGetID"(oTable, aColumnValues);
	IF bException AND NOT oRetVal."bValue" THEN
		RAISE EXCEPTION 'CANNOT FIND SPECIFIED RECORD [%][%]', oTable, aColumnValues;
	END IF;
	RETURN oRetVal."nValue";
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

CREATE OR REPLACE FUNCTION "fTableAdd"(stTable table_name, aColumnValues anyarray) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fTableAdd"(stTable, aColumnValues, false);
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
CREATE OR REPLACE FUNCTION "fTableRemove"(stTable table_name, id integer) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	aColumns := '{{id,'||id||'}}';
	stRetVal := "fTableRemove"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION "fTableRemove"(sTableSchema text, sTableName text, id integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":=sTableSchema;
	stTable."sName":=sTableName;
	stRetVal := "fTableRemove"(stTable, id);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** "AttributesTable" **************************************/
CREATE OR REPLACE FUNCTION "fAttributesTableGet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sValuesTableColumn IS NULL OR nValuesTableID IS NULL THEN
		RAISE EXCEPTION 'sValuesTableColumn AND nValuesTableID cannot be NULL';
	END IF;
	aColumns := '{{'||sValuesTableColumn||','||nValuesTableID::text||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableGet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sValuesTableColumn IS NULL OR nValuesTableID IS NULL THEN
		RAISE EXCEPTION 'sValuesTableColumn AND nValuesTableID cannot be NULL';
	END IF;
	aColumns := '{{'||sValuesTableColumn||','||nValuesTableID::text||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableGet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer,  sKey character varying) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sValuesTableColumn IS NULL OR nValuesTableID IS NULL THEN
		RAISE EXCEPTION 'sValuesTableColumn AND nValuesTableID cannot be NULL';
	END IF;
	aColumns := '{{'||sValuesTableColumn||','||nValuesTableID::text||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableGet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer,  sKey character varying) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sValuesTableColumn IS NULL OR nValuesTableID IS NULL THEN
		RAISE EXCEPTION 'sValuesTableColumn AND nValuesTableID cannot be NULL';
	END IF;
	aColumns := '{{'||sValuesTableColumn||','||nValuesTableID::text||'},{sKey,0}}';
	aColumns[2][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableAdd"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sValuesTableColumn IS NULL OR nValuesTableID IS NULL THEN
		RAISE EXCEPTION 'sValuesTableColumn AND nValuesTableID cannot be NULL';
	END IF;
	aColumns := '{{'||sValuesTableColumn||','||nValuesTableID::text||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableAdd"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fAttributesTableAdd"(stTable, sValuesTableColumn, nValuesTableID, idRegisteredTables, NULL, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableAdd"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fAttributesTableAdd"(stTable, sValuesTableColumn, nValuesTableID, NULL, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableSet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fAttributesTableGet"(stTable, sValuesTableColumn, nValuesTableID, sKey);
	IF stRetVal."bValue" THEN
		EXECUTE 'UPDATE '||quote_ident(stTable."sSchema")||'.'||quote_ident(stTable."sName")||' SET "nValue"='||COALESCE(nValue::text,'NULL')||' WHERE id='||stRetVal."nValue"::text;
	ELSE
		stRetVal := "fAttributesTableAdd"(stTable, sValuesTableColumn, nValuesTableID, sKey, nValue);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableSet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := "fAttributesTableGet"(stTable, sValuesTableColumn, nValuesTableID, idRegisteredTables, sKey);
	IF stRetVal."bValue" THEN
		EXECUTE 'UPDATE '||quote_ident(stTable."sSchema")||'.'||quote_ident(stTable."sName")||' SET "nValue"='||COALESCE(nValue::text,'NULL')||' WHERE id='||stRetVal."nValue"::text;
	ELSE
		stRetVal := "fAttributesTableAdd"(stTable, sValuesTableColumn, nValuesTableID, idRegisteredTables, sKey, nValue);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fAttributesTableValueGet"(stTable table_name, sValuesTableColumn character varying, nValuesTableID integer, idRegisteredTables integer,  sKey character varying) RETURNS int_bool AS
$$
DECLARE
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sValuesTableColumn IS NULL OR nValuesTableID IS NULL THEN
		RAISE EXCEPTION 'sValuesTableColumn AND nValuesTableID cannot be NULL';
	END IF;
	aColumns := '{{'||sValuesTableColumn||','||nValuesTableID::text||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);

	EXECUTE 'SELECT "nValue" FROM '||quote_ident(stTable."sSchema")||'.'||quote_ident(stTable."sName")||' WHERE id='||stRetVal."nValue"::text INTO stRetVal."nValue";
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION "fFrames"(dtInterval interval) RETURNS bigint AS
$$
BEGIN
	RETURN floor((((date_part('epoch', dtInterval) - date_part('second', dtInterval)) * 1000) + date_part('milliseconds', dtInterval)) / 40);
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
	nRetVal := "fTableAdd"(oTable, aColumns, true, bException);
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