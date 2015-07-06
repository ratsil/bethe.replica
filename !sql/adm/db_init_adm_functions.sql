/********************************** adm."tCommands" ***************************************/
CREATE OR REPLACE FUNCTION adm."fCommandGet"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tCommands';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fCommandAdd"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tCommands';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** adm."tCommandStatuses" ********************************/
CREATE OR REPLACE FUNCTION adm."fCommandStatusGet"(sName character varying) RETURNS integer AS
$$
DECLARE
	nRetVal integer;
BEGIN
	SELECT id INTO nRetVal FROM adm."tCommandStatuses" WHERE "sName"=sName;
	RETURN nRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** adm."tCommandsQueue" **********************************/
CREATE OR REPLACE FUNCTION adm."fCommandsQueueAdd"(idCommands integer, idUsers integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	INSERT INTO adm."tCommandsQueue" ("idCommands","idUsers") VALUES (idCommands, idUsers);
	stRetVal."nValue" := lastval();
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION adm."fCommandsQueueAdd"(sCommand character varying, sUser character varying) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idUsers integer;
BEGIN
	stRetVal := hk."fUserGet"(sUser);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idUsers := stRetVal."nValue";
	stRetVal := adm."fCommandGet"(sCommand);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	stRetVal := adm."fCommandsQueueAdd"(stRetVal."nValue", idUsers);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION adm."fCommandsQueueAdd"(sCommand character varying) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"(sCommand, session_user::text);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fCommandsQueueStatusGet"(nQueueID integer) RETURNS SETOF adm."tCommandStatuses" AS
$$
DECLARE
	stRetVal adm."tCommandStatuses"%ROWTYPE;
BEGIN
	SELECT * INTO stRetVal FROM adm."tCommandStatuses" WHERE id IN (SELECT "idCommandStatuses" FROM adm."tCommandsQueue" WHERE id=nQueueID);
	RETURN NEXT stRetVal;
	RETURN;
END;
$$
LANGUAGE plpgsql VOLATILE;

/********************************** adm."tCommandParameters" ******************************/
CREATE OR REPLACE FUNCTION adm."fCommandParameterAdd"(idCommandsQueue integer, sKey text, sValue text) RETURNS int_bool AS
$$
DECLARE
	idUsers integer;
	stRetVal int_bool;
BEGIN
	INSERT INTO adm."tCommandParameters" ("idCommandsQueue", "sKey", "sValue") VALUES (idCommandsQueue, sKey, sValue);
	stRetVal."nValue" := lastval();
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fCommandParametersGet"(idCommandsQueue integer, sKey text) RETURNS text_bool AS
$$
DECLARE
	stRetVal text_bool;
BEGIN
	SELECT "sValue" INTO stRetVal."sValue" FROM adm."tCommandParameters" WHERE "idCommandsQueue" = idCommandsQueue AND "sKey" = sKey;
	IF stRetVal."sValue" IS NULL THEN
		stRetVal."bValue" := false;
	ELSE
		stRetVal."bValue" := true;
	END IF;
	RETURN stRetVal;
END
$$
LANGUAGE plpgsql VOLATILE;
/******************************************************************************************/
CREATE OR REPLACE FUNCTION adm."fPlayerSkip"(idItems integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('player_skip');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'idItems', idItems::text);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPlaylistItemDurationRefresh"(idItems integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('playlist_item_duration_update');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'idItems', idItems::text);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fAssetDurationRefresh"(idAssets integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('asset_duration_update');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'idAssets', idAssets::text);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fFileUpload"(sFile text, bForce bool) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('file_upload');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'sFile', sFile);
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'sFile', bForce::text);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fCuesTemplateProcess"(sFile text, bShow bool) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
	sCommand text;
BEGIN
	IF bShow THEN 
		sCommand := 'cues_template_show';
	ELSE
		sCommand := 'cues_template_hide';
	END IF;
	stRetVal := adm."fCommandsQueueAdd"(sCommand);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'path', sFile);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fFileDurationGet"(idFiles integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('file_duration_get');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'idFiles', idFiles::text);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fServiceStart"(sServiceName text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('service_start');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'sServiceName', sServiceName);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION adm."fServiceStop"(sServiceName text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('service_stop');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'sServiceName', sServiceName);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION adm."fServiceRestart"(sServiceName text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := adm."fServiceStop"(sServiceName);
	stRetVal := adm."fServiceStart"(sServiceName);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION adm."fFailoverSync"() RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('failover_sync');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION adm."fFailoverSkip"(idItems integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idCommandsQueue integer;
BEGIN
	stRetVal := adm."fCommandsQueueAdd"('failover_skip');
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idCommandsQueue := stRetVal."nValue";
	stRetVal := adm."fCommandParameterAdd"(idCommandsQueue, 'idItems', idItems::text);
	stRetVal."nValue" := idCommandsQueue;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** adm."tPreferenceClasses" ******************************/
CREATE OR REPLACE FUNCTION adm."fPreferenceClassGet"(sName character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tPreferenceClasses';
	aColumns := '{{sName,0},{bActive,0}}';
	aColumns[1][2] := COALESCE(sName::text,'NULL'); --чтобы не морочиться с экранированием
	aColumns[2][2] := COALESCE(bActive::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceClassAdd"(sName character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tPreferenceClasses';
	aColumns := '{{sName,0},{bActive,0}}';
	aColumns[1][2] := COALESCE(sName::text,'NULL'); --чтобы не морочиться с экранированием
	aColumns[2][2] := COALESCE(bActive::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** adm."tPreferences" ************************************/
CREATE OR REPLACE FUNCTION adm."fPreferenceGet"(idPreferenceClasses integer, sName character varying, sValue character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tPreferences';
	aColumns := '{{idPreferenceClasses,'||COALESCE(idPreferenceClasses::text,'NULL')||'},{sName,0},{sValue,0},{bActive,0}}';
	aColumns[2][2] := COALESCE(sName::text,'NULL'); --чтобы не морочиться с экранированием
	aColumns[3][2] := COALESCE(sValue::text,'NULL'); --чтобы не морочиться с экранированием
	aColumns[4][2] := COALESCE(bActive::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceAdd"(idPreferenceClasses integer, sName character varying, sValue character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tPreferences';
	aColumns := '{{idPreferenceClasses,'||COALESCE(idPreferenceClasses::text,'NULL')||'},{sName,0},{sValue,0},{bActive,0}}';
	aColumns[2][2] := COALESCE(sName::text,'NULL'); --чтобы не морочиться с экранированием
	aColumns[3][2] := COALESCE(sValue::text,'NULL'); --чтобы не морочиться с экранированием
	aColumns[4][2] := COALESCE(bActive::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceSet"(idPreferenceClasses integer, sName character varying, sValue character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='adm';
	stTable."sName":='tPreferences';
	aColumns := '{{idPreferenceClasses,'||COALESCE(idPreferenceClasses::text,'NULL')||'},{sName,0}}';
	aColumns[2][2] := COALESCE(sName::text,'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	IF stRetVal."bValue" THEN
		UPDATE adm."tPreferences" SET "sValue"=sValue WHERE id=stRetVal."nValue";
		stRetVal."bValue" := true;
	ELSE
		stRetVal := adm."fPreferenceAdd"(idPreferenceClasses, sName, sValue, true);
	END IF;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceGet"(sPreferenceClassName character varying, bPreferenceClassActive boolean, sName character varying, sValue character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idPreferenceClasses integer;
BEGIN
	stRetVal := adm."fPreferenceClassGet"(sPreferenceClassName, bPreferenceClassActive);
	idPreferenceClasses := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := adm."fPreferenceGet"(idPreferenceClasses, sName, sValue, bActive);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceGet"(sPreferenceClassName character varying, sName character varying, sValue character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := adm."fPreferenceGet"(sPreferenceClassName, true, sName, sValue, bActive);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceAdd"(sPreferenceClassName character varying, bPreferenceClassActive boolean, sName character varying, sValue character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idPreferenceClasses integer;
BEGIN
	stRetVal := adm."fPreferenceClassGet"(sPreferenceClassName, bPreferenceClassActive);
	idPreferenceClasses := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := adm."fPreferenceAdd"(idPreferenceClasses, sName, sValue, bActive);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceAdd"(sPreferenceClassName character varying, sName character varying, sValue character varying, bActive boolean) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := adm."fPreferenceAdd"(sPreferenceClassName, true, sName, sValue, bActive);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceSet"(sPreferenceClassName character varying, bPreferenceClassActive boolean, sName character varying, sValue character varying) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
	idPreferenceClasses integer;
BEGIN
	stRetVal := adm."fPreferenceClassGet"(sPreferenceClassName, bPreferenceClassActive);
	idPreferenceClasses := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := adm."fPreferenceSet"(idPreferenceClasses, sName, sValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION adm."fPreferenceSet"(sPreferenceClassName character varying, sName character varying, sValue character varying) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := adm."fPreferenceSet"(sPreferenceClassName, true, sName, sValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;