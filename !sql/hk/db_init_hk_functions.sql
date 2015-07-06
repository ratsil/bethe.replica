/********************************** hk."tUsers" *******************************************/
CREATE OR REPLACE FUNCTION hk."fUserGet"(sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUsers';
	aColumns := '{{sUsername,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fUserNameGet"(idUsers integer) RETURNS name AS
$$
DECLARE
	sRetVal name;
BEGIN
	SELECT "sUsername" INTO sRetVal FROM hk."tUsers" WHERE idUsers=id;
	RETURN sRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

-- CREATE OR REPLACE FUNCTION hk."fUserAdd" - see in db_init_hk_tables.sql
CREATE OR REPLACE FUNCTION hk."fUserAdd"(sName name, sPassword text) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	EXECUTE 'CREATE USER ' || quote_ident(sName) || ' LOGIN ENCRYPTED PASSWORD  ' || quote_literal(sPassword) || ' INHERIT VALID UNTIL ' || quote_literal('infinity');

	EXECUTE 'GRANT replica_access TO ' || quote_ident(sName);

	stTable."sSchema":='hk';
	stTable."sName":='tUsers';
	aColumns := '{{sUsername,0}}'; 
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tWebPages" ****************************************/
CREATE OR REPLACE FUNCTION hk."fWebPageGet"(idParent integer, sPage name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tWebPages';
	aColumns := '{{idParent,'||COALESCE(idParent::text,'NULL')||'},{sPage,0}}';
	aColumns[2][2] := sPage; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fWebPageAdd"(idParent integer, sPage name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tWebPages';
	aColumns := '{{idParent,'||COALESCE(idParent::text,'NULL')||'},{sPage,0}}';
	aColumns[2][2] := sPage; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tUserAttributes" **********************************/
CREATE OR REPLACE FUNCTION hk."fUserAttributeGet"(idUsers integer, idRegisteredTables integer, sKey name, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUserAttributes';
	stRetVal := "fAttributesTableGet"(stTable, 'idUsers', idUsers, idRegisteredTables, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeGet"(idUsers integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUserAttributes';
	aColumns := '{{idUsers,'||COALESCE(idUsers::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeGet"(idUsers integer, idRegisteredTables integer,  sKey name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUserAttributes';
	aColumns := '{{idUsers,'||COALESCE(idUsers::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeGet"(idUsers integer,  sKey name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUserAttributes';
	aColumns := '{{idUsers,'||COALESCE(idUsers::text,'NULL')||'},{sKey,0}}';
	aColumns[2][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeAdd"(idUsers integer, idRegisteredTables integer, sKey name, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUserAttributes';
	aColumns := '{{idUsers,'||COALESCE(idUsers::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeAdd"(idUsers integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserAttributeAdd"(idUsers, idRegisteredTables, NULL, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeAdd"(idUsers integer, sKey name, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserAttributeAdd"(idUsers, NULL, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeSet"(idUsers integer, sKey name, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserAttributeGet"(idUsers, sKey);
	IF stRetVal."bValue" THEN
		UPDATE hk."tUserAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
	ELSE
		stRetVal := hk."fUserAttributeAdd"(idUsers, sKey, nValue);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeSet"(idUsers integer, idRegisteredTables integer, sKey name, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserAttributeGet"(idUsers, idRegisteredTables, sKey);
	IF stRetVal."bValue" THEN
		UPDATE hk."tUserAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
	ELSE
		stRetVal := hk."fUserAttributeAdd"(idUsers, idRegisteredTables, sKey, nValue);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeRemove"(idUsers integer, idRegisteredTables integer, sKey name, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	DELETE FROM hk."tUserAttributes" WHERE idUsers="idUsers" AND idRegisteredTables="idRegisteredTables" AND sKey="sKey" AND nValue="nValue";
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fUserAttributeRemove"(idUsers integer, idRegisteredTables integer, sKey name) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	DELETE FROM hk."tUserAttributes" WHERE idUsers="idUsers" AND idRegisteredTables="idRegisteredTables" AND sKey="sKey";
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAttributeValueGet"(idUsers integer, idRegisteredTables integer,  sKey name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tUserAttributes';
	aColumns := '{{idUsers,'||COALESCE(idUsers::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
	aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	SELECT "nValue" INTO stRetVal."nValue" FROM hk."tUserAttributes" WHERE id=stRetVal."nValue";
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/******************************************************************************************/
CREATE OR REPLACE FUNCTION hk."fUserHomePageGet"(idUsers integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('hk', 'tWebPages');
	idRegisteredTables := stRetVal."nValue";
	stRetVal := hk."fUserAttributeValueGet"(idUsers, idRegisteredTables, 'homepage');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserHomePageValueGet"(idUsers integer) RETURNS name AS
$$
DECLARE
	stIB int_bool;
	idUsersPrecised integer;
	sRetVal name;
BEGIN
	IF idUsers IS NULL THEN
		SELECT id INTO idUsersPrecised FROM hk."tUsers" WHERE "sUsername" = session_user;
	ELSE
		idUsersPrecised := idUsers;
	END IF;
	stIB := hk."fUserHomePageGet"(idUsersPrecised);
	SELECT "sPage" INTO sRetVal FROM hk."tWebPages" WHERE id=stIB."nValue";
	RETURN sRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserHomePageValueGet"() RETURNS name AS
$$
DECLARE
	sRetVal name;
BEGIN
	sRetVal := hk."fUserHomePageValueGet"(NULL);
	RETURN sRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserHomePageSet"(idUsers integer, idWebPages integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('hk', 'tWebPages');
	idRegisteredTables := stRetVal."nValue";

	DELETE FROM hk."tUserAttributes" WHERE "idUsers"=idUsers AND "idRegisteredTables"=idRegisteredTables AND "sKey"='homepage';
	IF idWebPages IS NOT NULL THEN
		stRetVal := hk."fUserAttributeSet"(idUsers, idRegisteredTables, 'homepage', idWebPages);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fUserHomePageSet"(sUsername name, idWebPages integer) RETURNS int_bool AS
$$
DECLARE
	idUsers integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserGet"(sUsername);
	idUsers := stRetVal."nValue";
	stRetVal := hk."fUserHomePageSet"(idUsers, idWebPages);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fUserHomePageSet"(sUser name, sPage name) RETURNS int_bool AS
$$
DECLARE
	idWebPages integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fWebPageGet"(NULL, sPage);
	idWebPages := stRetVal."nValue";
	IF idWebPages IS NOT NULL THEN
		stRetVal := hk."fUserHomePageSet"(sUser, idWebPages);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAccessRoleAdd"(idUsers integer, idAccessRoles integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	sUser name;
	sAccessRole name;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('hk', 'tAccessRoles');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	sUser := hk."fUserNameGet"(idUsers);
	sAccessRole := hk."fAccessRoleNameGet"(idAccessRoles);
	IF sUser IS NULL OR sAccessRole IS NULL THEN
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	stRetVal := hk."fUserAttributeAdd"(idUsers, stRetVal."nValue", 'access_role', idAccessRoles);
	IF stRetVal."bValue" THEN
		EXECUTE 'GRANT ' || quote_ident(sAccessRole) || ' TO ' || quote_ident(sUser);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fUserAccessRoleAdd"(sUser name, sAccessRole name) RETURNS int_bool AS
$$
DECLARE
	idUsers integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserGet"(sUser);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idUsers := stRetVal."nValue";
	stRetVal := hk."fAccessRoleGet"(sAccessRole);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	stRetVal := hk."fUserAccessRoleAdd"(idUsers, stRetVal."nValue");
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fUserAccessRoleRemove"(idUsers integer, idAccessRoles integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('hk', 'tAccessRoles');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	stRetVal := hk."fUserAttributeRemove"(idUsers, stRetVal."nValue", 'access_role', idAccessRoles);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tHouseKeeping" ************************************/
/********************************** hk."tRegisteredTables" ********************************/
CREATE OR REPLACE FUNCTION hk."fRegisteredTableGet"(stTargetTable table_name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tRegisteredTables';
	aColumns := '{{sSchema,0},{sName,0}}';
	aColumns[1][2] := stTargetTable."sSchema";  --чтобы не морочиться с экранированием
	aColumns[2][2] := stTargetTable."sName";
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableGet"(sSchema name, sName name) RETURNS int_bool AS
$$
DECLARE
	stTargetTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTargetTable."sSchema":=sSchema;
	stTargetTable."sName":=sName;
	stRetVal := hk."fRegisteredTableGet"(stTargetTable);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableAdd"(sSchema name, sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tRegisteredTables';
	aColumns := '{{sSchema,0},{sName,0}}';
	aColumns[1][2] := sSchema;  --чтобы не морочиться с экранированием
	aColumns[2][2] := sName;
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableGetUpdated"(idRegisteredTables int) RETURNS timestamp with time zone AS
$$
DECLARE
	dtRetVal timestamp with time zone;
BEGIN
	SELECT "dtUpdated" INTO dtRetVal FROM hk."tRegisteredTables" WHERE idRegisteredTables=id;
	RETURN dtRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableGetUpdated"(sSchema name,sName name) RETURNS timestamp with time zone AS
$$
DECLARE
	dtRetVal timestamp with time zone;
	idRegisteredTables int;
BEGIN
	SELECT id INTO idRegisteredTables FROM hk."tRegisteredTables" WHERE sSchema="sSchema" AND sName="sName";
	dtRetVal := hk."fRegisteredTableGetUpdated"(idRegisteredTables);
	RETURN dtRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableUpdate"(idRegisteredTables int) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	UPDATE hk."tRegisteredTables" SET "dtUpdated"=now() WHERE idRegisteredTables=id;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idRegisteredTables;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fRegisteredTableUpdate"(sSchema name,sName name) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idRegisteredTables int;
BEGIN
	SELECT id INTO idRegisteredTables FROM hk."tRegisteredTables" WHERE sSchema="sSchema" AND sName="sName";
	stRetVal := hk."fRegisteredTableUpdate"(idRegisteredTables);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tErrorScopes" *************************************/
CREATE OR REPLACE FUNCTION hk."fErrorScopeGet"(idRegisteredTables integer, eError error) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tErrorScopes';
	aColumns := '{{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{eError,0}}';
	aColumns[2][2] := ''''||eError||'''';
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fErrorScopeAdd"(idRegisteredTables integer, eError error) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tErrorScopes';
	aColumns := '{{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{eError,0}}';
	aColumns[2][2] := ''''||eError||'''';
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fErrorScopeAdd"(sRegisteredTableSchema name, sRegisteredTableName name, eError error) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"(sRegisteredTableSchema, sRegisteredTableName);
	IF stRetVal."bValue" THEN
		stRetVal := hk."fErrorScopeAdd"(stRetVal."nValue", eError);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tDTEventTypes" ************************************/
CREATE OR REPLACE FUNCTION hk."fDTEventTypeGet"(sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tDTEventTypes';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fDTEventTypeGet"(sTG_OP character(6)) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tDTEventTypes';
	aColumns := '{{sTG_OP,0}}';
	aColumns[1][2] := sTG_OP; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fDTEventTypeAdd"(sName name, sTG_OP character(6)) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tDTEventTypes';
	aColumns := '{{sName,0},{sTG_OP,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	aColumns[2][2] := sTG_OP;
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tDTEvents" ****************************************/
CREATE OR REPLACE FUNCTION hk."fDTEventGet"(idDTEventTypes integer, idHouseKeeping integer, idUsers integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tDTEvents';
	aColumns := '{{idDTEventTypes,'||idDTEventTypes||'},{idHouseKeeping,'||idHouseKeeping||'},{idUsers,'||idUsers||'}}';
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fDTEventAdd"(idDTEventTypes integer, idHouseKeeping integer, idUsers integer, sNote text) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tDTEvents';
	aColumns := '{{idDTEventTypes,'||idDTEventTypes||'},{idHouseKeeping,'||idHouseKeeping||'},{idUsers,'||idUsers||'},{sNote,0}}';
	aColumns[4][2] := sNote; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** Global trigger functions ******************************/
CREATE OR REPLACE FUNCTION hk."fManagement"() RETURNS trigger AS
$Management$
DECLARE
	idHKType integer;
	idRegisteredTables integer;
	nHouseKeepingTableID integer;
	idUsers integer;
	stTable table_name;
	aColumns text[][];
BEGIN
	IF 'replica_init' = session_user THEN -- OR 'replica_management' = session_user OR 'replica_player' = session_user)
		IF (TG_OP = 'DELETE') THEN
			RETURN OLD;
		END IF;
		NEW."idHK":=0;
		RETURN NEW;
	END IF;
	SELECT id INTO idRegisteredTables FROM hk."tRegisteredTables" WHERE "sName"=TG_TABLE_NAME AND "sSchema"=TG_TABLE_SCHEMA;
	SELECT id INTO idHKType FROM hk."tDTEventTypes" WHERE "sTG_OP"=TG_OP;
	SELECT id INTO idUsers FROM hk."tUsers" WHERE "sUsername"=session_user;
	SELECT id INTO nHouseKeepingTableID FROM hk."tRegisteredTables" WHERE "sSchema"='hk' AND "sName"='tHouseKeeping';
	--RAISE NOTICE '[TG_TABLE_SCHEMA: %] [TG_TABLE_NAME: %] [TG_OP: %]', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP;
	--RAISE NOTICE '[idRegisteredTables: %] [idHKType: %] [idUsers: %] [nHouseKeepingTableID: %]', idRegisteredTables, idHKType, idUsers, nHouseKeepingTableID;

		IF (TG_OP = 'DELETE') THEN
			--RAISE NOTICE 'Call hk.Management for DELETE: [idRegisteredTables: %] [TG_OP: %] [idHKType: %]', idRegisteredTables, TG_OP, idHKType;
			PERFORM hk."fDTEventAdd"(idHKType, OLD."idHK", idUsers, '['||TG_OP||']['||TG_TABLE_SCHEMA||']['||TG_TABLE_NAME||']');
			--RAISE NOTICE '[% idHK]', NEW."idHK";
			RETURN OLD;
		ELSIF (TG_OP = 'UPDATE') THEN
			PERFORM hk."fDTEventAdd"(idHKType, NEW."idHK", idUsers, '['||TG_OP||']['||TG_TABLE_SCHEMA||']['||TG_TABLE_NAME||']');
			--RAISE NOTICE '[% idHK]', NEW."idHK";
			RETURN NEW;
		ELSIF (TG_OP = 'INSERT') THEN
			INSERT INTO hk."tHouseKeeping" DEFAULT VALUES;
			NEW."idHK":=lastval();
			PERFORM hk."fDTEventAdd"(idHKType, NEW."idHK", idUsers, '['||TG_OP||']['||TG_TABLE_SCHEMA||']['||TG_TABLE_NAME||']');
			--RAISE NOTICE '[% lastval] [% idHK]', lastval(), NEW."idHK";
			RETURN NEW;
		END IF;
		RETURN NULL;

    END;
$Management$ LANGUAGE 'plpgsql' VOLATILE;
/********************************** hk."tAccessRoles" **************************************/
CREATE OR REPLACE FUNCTION hk."fAccessRoleGet"(sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tAccessRoles';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName::text; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fAccessRoleNameGet"(idAccessRoles integer) RETURNS name AS
$$
DECLARE
	sRetVal name;
BEGIN
	SELECT "sName" INTO sRetVal FROM hk."tAccessRoles" WHERE idAccessRoles=id;
	RETURN sRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fAccessRoleAdd"(sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fUserGet"(sName);
	IF NOT stRetVal."bValue" THEN
		EXECUTE 'CREATE ROLE ' || quote_ident(sName) || ' INHERIT VALID UNTIL ' || quote_literal('infinity');
		IF 'replica_access' <> sName THEN
			EXECUTE 'GRANT replica_access TO ' || quote_ident(sName);
		END IF;
	END IF;
	stTable."sSchema":='hk';
	stTable."sName":='tAccessRoles';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName::text; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;



CREATE OR REPLACE FUNCTION hk."fAccessRoleScopeSet"(idAccessRoles integer, idAccessScopes integer, bCreate boolean, bUpdate boolean, bDelete boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idAccessPermissions integer;
	nCount integer;
BEGIN
	DELETE FROM hk."tAccessPermissions" WHERE idAccessRoles = "idAccessRoles" AND idAccessScopes = "idAccessScopes";
	stRetVal."bValue" := true;
	stRetVal."nValue" := NULL;
	IF bCreate IS NULL AND bUpdate IS NULL AND bDelete IS NULL THEN
		RETURN stRetVal;
	END IF;
	stRetVal := hk."fAccessPermissionAdd"(idAccessScopes, idAccessRoles, bCreate, bUpdate, bDelete);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fAccessRoleScopeSet"(sAccessRole name, sAccessScopeName name, bCreate boolean, bUpdate boolean, bDelete boolean) RETURNS int_bool AS
$$
DECLARE
	idUsers integer;
	idAccessRoles integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fAccessRoleGet"(sAccessRole);
	IF NOT stRetVal THEN
		RETURN stRetVal;
	END IF;
	idAccessRoles := stRetVal."nValue";
	stRetVal := hk."fAccessScopeGet"(sAccessScopeName);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	stRetVal := hk."fUserAccessScopeSet"(idAccessRoles, stRetVal."nValue", bCreate, bUpdate, bDelete);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tAccessScopes" *************************************/
CREATE OR REPLACE FUNCTION hk."fAccessScopeGet"(idParent integer, sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tAccessScopes';
	aColumns := '{{idParent,'||COALESCE(idParent::text,'NULL')||'},{sName,0}}';
	aColumns[2][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fAccessScopeAdd"(idParent integer, sName name) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tAccessScopes';
	aColumns := '{{idParent,'||COALESCE(idParent::text,'NULL')||'},{sName,0}}';
	aColumns[2][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fAccessScopeGet"(sNameQualified text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	sNameReminder text;
	sName name;
	nDotPosition integer;
	idParent integer;
BEGIN
	idParent := NULL;
	sNameReminder := sNameQualified;
	stRetVal."bValue" := true;
	stRetVal."nValue" := -1;
	WHILE true LOOP
		nDotPosition := position('.' in sNameReminder);
		EXIT WHEN nDotPosition IS NULL OR 1 > nDotPosition;
		stRetVal."bValue" := false;

		sName := substring(sNameReminder from 1 for nDotPosition - 1);
		EXIT WHEN sName IS NULL OR 1 > char_length(sName);
		sNameReminder := substring(sNameReminder from nDotPosition + 1);

		stRetVal := hk."fAccessScopeGet"(idParent, sName);
		EXIT WHEN NOT stRetVal."bValue";

		idParent := stRetVal."nValue";
	END LOOP;
	IF stRetVal."bValue" THEN
		stRetVal := hk."fAccessScopeGet"(idParent, sNameReminder);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fAccessScopeAdd"(sNameQualified text, bParentsCreate boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	sNameReminder text;
	sName name;
	nDotPosition integer;
	idParent integer;
BEGIN
	idParent := NULL;
	sNameReminder := sNameQualified;
	stRetVal."bValue" := true;
	stRetVal."nValue" := -1;
	WHILE true LOOP
		nDotPosition := position('.' in sNameReminder);
		EXIT WHEN nDotPosition IS NULL OR 1 > nDotPosition;
		stRetVal."bValue" := false;

		sName := substring(sNameReminder from 1 for nDotPosition - 1);
		EXIT WHEN sName IS NULL OR 1 > char_length(sName);
		sNameReminder := substring(sNameReminder from nDotPosition + 1);

		stRetVal := hk."fAccessScopeGet"(idParent, sName);
		IF NOT stRetVal."bValue" THEN
			EXIT WHEN NOT bParentsCreate;
			stRetVal := hk."fAccessScopeAdd"(idParent, sName);
			EXIT WHEN NOT stRetVal."bValue";
		END IF;

		idParent := stRetVal."nValue";
	END LOOP;
	IF stRetVal."bValue" THEN
		stRetVal := hk."fAccessScopeAdd"(idParent, sNameReminder);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** hk."tAccessPermissions" ********************************/
CREATE OR REPLACE FUNCTION hk."fAccessPermissionGet"(idAccessScopes integer, idAccessRoles integer, bCreate boolean, bUpdate boolean, bDelete boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tAccessPermissions';
	aColumns := '{{idAccessScopes,'||COALESCE(idAccessScopes::text,'NULL')||'},{idAccessRoles,'||COALESCE(idAccessRoles::text,'NULL')||'},{bCreate,0},{bUpdate,0},{bDelete,0}}';
	aColumns[3][2] := bCreate::text;
	aColumns[4][2] := bUpdate::text;
	aColumns[5][2] := bDelete::text;
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fAccessPermissionAdd"(idAccessScopes integer, idAccessRoles integer, bCreate boolean, bUpdate boolean, bDelete boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='hk';
	stTable."sName":='tAccessPermissions';
	aColumns := '{{idAccessScopes,'||COALESCE(idAccessScopes::text,'NULL')||'},{idAccessRoles,'||COALESCE(idAccessRoles::text,'NULL')||'},{bCreate,0},{bUpdate,0},{bDelete,0}}';
	aColumns[3][2] := bCreate::text;
	aColumns[4][2] := bUpdate::text;
	aColumns[5][2] := bDelete::text;
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION hk."fAccessPermissionAdd"(sAccessScopeNameQualified name, sAccessRole name, bCreate boolean, bUpdate boolean, bDelete boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idAccessScopes integer;
BEGIN
	stRetVal := hk."fAccessScopeGet"(sAccessScopeNameQualified);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idAccessScopes := stRetVal."nValue";
	stRetVal := hk."fAccessRoleGet"(sAccessRole);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	stRetVal := hk."fAccessPermissionAdd"(idAccessScopes, stRetVal."nValue", bCreate, bUpdate, bDelete);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;