/********************************** ia."tMessages" ****************************************/
CREATE OR REPLACE FUNCTION ia."fMessageAdd"() RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	INSERT INTO ia."tMessages" (id) VALUES (DEFAULT);
	stRetVal."nValue" := currval('ia."tMessages_id_seq"');
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAdd"(sBindID character varying, ip inet, nCount integer, nSource bigint, nTarget bigint, sText text, cImage bytea) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	idMessages integer;
	idNumbers integer;
	idDTEventTypes integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageAdd"();
	idMessages := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageBindAdd"(idMessages, sBindID);
	---------------------------------------------------------
	stRetVal := ia."fMessageGatewayIPAdd"(idMessages, ip);
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'Unknown Gateway IP:%',ip;
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeAdd"(idMessages, 'parts', nCount);
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('ia','tNumbers');
	idRegisteredTables := stRetVal."nValue";
	
	stRetVal := ia."fNumberAdd"(nSource);
	idNumbers := stRetVal."nValue";
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, 'source', idNumbers);

	stRetVal := ia."fNumberAdd"(nTarget);
	idNumbers := stRetVal."nValue";
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, 'target', idNumbers);
	---------------------------------------------------------
	IF sText IS NOT NULL THEN
		IF 'ru ' = substr(sText, 1, 3) THEN
			stRetVal := ia."fMessageTextAdd"(idMessages, substr(sText, 4));
		ELSE
			stRetVal := ia."fMessageTextAdd"(idMessages, sText);
		END IF;
	END IF;
	---------------------------------------------------------
	IF cImage IS NOT NULL THEN
		stRetVal := ia."fMessageImageAdd"(idMessages, cImage);
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fDTEventTypeGet"('register');
	idDTEventTypes := stRetVal."nValue";
	stRetVal := ia."fMessageDTEventAdd"(idMessages, idDTEventTypes);
	---------------------------------------------------------
	stRetVal."nValue":=idMessages;
	stRetVal."bValue":=true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessagesCountGet"(ip inet) RETURNS int_bool AS
$$
DECLARE
	idGateways integer;
	nCount integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fGatewayGet"(ip);
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'Unknown Gateway IP:%',ip;
	END IF;
	idGateways := stRetVal."nValue";
	---------------------------------------------------------
	SELECT count(*) INTO nCount FROM ia."vMessageGateways" g WHERE idGateways = g."idGateways";
  	stRetVal."nValue":= nCount;
	stRetVal."bValue":=true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessagesDisplayedCountGet"(ip inet) RETURNS int_bool AS
$$
DECLARE
	idGateways integer;
	nCount integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fGatewayGet"(ip);
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'Unknown Gateway IP:%',ip;
	END IF;
	idGateways := stRetVal."nValue";
	---------------------------------------------------------
	SELECT count(*) INTO nCount FROM ia."vMessageGateways" g, ia."tMessageAttributes" ma  WHERE ma."idMessages" = g.id AND 'display' = ma."sKey" AND ma."nValue" IS NOT NULL AND idGateways = g."idGateways";
  	stRetVal."nValue":= nCount;
	stRetVal."bValue":=true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tMessageAttributes" *******************************/
CREATE OR REPLACE FUNCTION ia."fMessageAttributeGet"(idMessages integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableGet"(stTable, 'idMessages', idMessages, idRegisteredTables, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeGet"(idMessages integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableGet"(stTable, 'idMessages', idMessages, idRegisteredTables, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeGet"(idMessages integer, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableGet"(stTable, 'idMessages', idMessages, idRegisteredTables, sKey);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeGet"(idMessages integer, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableGet"(stTable, 'idMessages', idMessages, sKey);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeAdd"(idMessages integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableAdd"(stTable, 'idMessages', idMessages, idRegisteredTables, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeAdd"(idMessages integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, NULL, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeAdd"(idMessages integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageAttributeAdd"(idMessages, NULL, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeSet"(idMessages integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableSet"(stTable, 'idMessages', idMessages, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeSet"(idMessages integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableSet"(stTable, 'idMessages', idMessages, idRegisteredTables, sKey, nValue);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageAttributeValueGet"(idMessages integer, idRegisteredTables integer, sKey character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tMessageAttributes';
	stRetVal := "fAttributesTableValueGet"(stTable, 'idMessages', idMessages, idRegisteredTables, sKey);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tDTEventTypes" ************************************/
CREATE OR REPLACE FUNCTION ia."fDTEventTypeGet"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tDTEventTypes';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fDTEventTypeAdd"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tDTEventTypes';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fDTEventTypeNameGet"(id integer) RETURNS text_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal text_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tDTEventTypes';
	stRetVal := "fTableGetName"(stTable, id);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tDTEvents" ****************************************/
CREATE OR REPLACE FUNCTION ia."fMessageDTEventGet"(idMessages integer, idDTEventTypes integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stDTEventTypeName text_bool;
	sDTEventTypeName character varying;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('ia','tDTEvents');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stDTEventTypeName := ia."fDTEventTypeNameGet"(idDTEventTypes);
	sDTEventTypeName := stDTEventTypeName."sValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeGet"(idMessages, idRegisteredTables, sDTEventTypeName);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fDTEventAdd"(idDTEventTypes integer, dtEvent timestamp with time zone) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tDTEvents';
	aColumns := '{{idDTEventTypes,'||COALESCE(idDTEventTypes::text,'NULL')||'},{dt,0}}';
	aColumns[2][2] := COALESCE(quote_literal(dtEvent),'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageDTEventAdd"(idMessages integer, idDTEventTypes integer, dtEvent timestamp with time zone) RETURNS int_bool AS
$$
DECLARE
	idDTEvents integer;
	idRegisteredTables integer;
	stDTEventTypeName text_bool;
	sDTEventTypeName character varying;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageDTEventGet"(idMessages, idDTEventTypes);
	IF stRetVal."bValue" THEN
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fDTEventAdd"(idDTEventTypes, dtEvent);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idDTEvents := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('ia','tDTEvents');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stDTEventTypeName := ia."fDTEventTypeNameGet"(idDTEventTypes);
	sDTEventTypeName := stDTEventTypeName."sValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, sDTEventTypeName, idDTEvents);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fDTEventAdd"(idDTEventTypes integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fDTEventAdd"(idDTEventTypes, now());
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageDTEventAdd"(idMessages integer, idDTEventTypes integer) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageDTEventAdd"(idMessages, idDTEventTypes, now());
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageDTEventAdd"(idMessages integer, sDTEventType character varying) RETURNS int_bool AS
$$
DECLARE
	idDTEventTypes integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fDTEventTypeGet"(sDTEventType);
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'Unknown DTEvent Type';
	END IF;
	idDTEventTypes := stRetVal."nValue";
	stRetVal := ia."fMessageDTEventAdd"(idMessages, idDTEventTypes);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fDTEventSet"(idDTEvents integer, dtEvent timestamp with time zone) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	UPDATE ia."tDTEvents" SET dt=dtEvent WHERE id = idDTEvents;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idDTEvents;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageDTEventSet"(idMessages integer, idDTEventTypes integer, dtEvent timestamp with time zone) RETURNS int_bool AS
$$
DECLARE
	idDTEvents integer;
	idRegisteredTables integer;
	sDTEventTypeName character varying;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageDTEventGet"(idMessages, idDTEventTypes);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idDTEvents := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fDTEventSet"(idDTEvents, dtEvent);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tGateways" ****************************************/
CREATE OR REPLACE FUNCTION ia."fGatewayGet"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tGateways';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayGet"(ip inet) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tGatewayIPs';
	aColumns := '{{cIP,0}}';
	aColumns[1][2] := COALESCE(quote_literal(ip),'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGetInt"(stTable, aColumns, 'idGateways');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayAdd"(sName character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tGateways';
	aColumns := '{{sName,0}}';
	aColumns[1][2] := sName; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayNameGet"(id integer) RETURNS character varying AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	sRetVal character varying;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tGateways';
	sRetVal := "fTableGetName"(stTable, id)::character varying;
	RETURN sRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tGatewayIPs" **************************************/
CREATE OR REPLACE FUNCTION ia."fMessageGatewayIPGet"(idMessages integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('ia','tGatewayIPs');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeGet"(idMessages, idRegisteredTables, 'gateway_ip');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayIPGet"(ip inet) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tGatewayIPs';
	aColumns := '{{cIP,0}}';
	aColumns[1][2] := COALESCE(quote_literal(ip),'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableGet"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayIPAdd"(idGateways integer, ip inet) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tGatewayIPs';
	aColumns := '{{idGateways,'||COALESCE(idGateways::text,'NULL')||'},{cIP,0}}';
	aColumns[2][2] := COALESCE(quote_literal(ip),'NULL'); --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayIPAdd"(sGateway character varying, ip inet) RETURNS int_bool AS
$$
DECLARE
	idGateways integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fGatewayGet"(sGateway);
	IF NOT stRetVal."bValue" THEN
		RAISE EXCEPTION 'Unknown Gateway';
	END IF;
	idGateways := stRetVal."nValue";
	stRetVal := ia."fGatewayIPAdd"(idGateways, ip);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageGatewayIPAdd"(idMessages integer, ip inet) RETURNS int_bool AS
$$
DECLARE
	idGatewayIPs integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageGatewayIPGet"(idMessages);
	IF stRetVal."bValue" THEN
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fGatewayIPGet"(ip);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idGatewayIPs := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('ia','tGatewayIPs');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, 'gateway_ip', idGatewayIPs);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fGatewayIPSet"(idGatewayIPs integer, ip inet) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	UPDATE ia."tGatewayIPs" SET "cIP"=ip WHERE id = idGatewayIPs;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idGatewayIPs;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageGatewayIPSet"(idMessages integer, ip inet) RETURNS int_bool AS
$$
DECLARE
	idGatewayIPs integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fGatewayIPGet"(idMessages);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idGatewayIPs := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fGatewayIPSet"(idGatewayIPs, ip);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tNumbers" *****************************************/
CREATE OR REPLACE FUNCTION ia."fNumberAdd"(nValue bigint) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tNumbers';
	aColumns := '{{nNumber,'||nValue::text||'}}';
	stRetVal := "fTableAdd"(stTable, aColumns);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tTexts" *******************************************/
CREATE OR REPLACE FUNCTION ia."fMessageTextGet"(idMessages integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('ia','tTexts');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeGet"(idMessages, idRegisteredTables, 'text');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fTextAdd"(sText text) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tTexts';
	aColumns := '{{sText,0}}';
	aColumns[1][2] := sText; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageTextAdd"(idMessages integer, sText text) RETURNS int_bool AS
$$
DECLARE
	idTexts integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageTextGet"(idMessages);
	IF stRetVal."bValue" THEN
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fTextAdd"(sText);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idTexts := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('ia','tTexts');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, 'text', idTexts);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fTextSet"(idTexts integer, sText text) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	UPDATE ia."tTexts" SET "sText"=sText WHERE id = idTexts;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idTexts;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageTextSet"(idMessages integer, sText text) RETURNS int_bool AS
$$
DECLARE
	idTexts integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageTextGet"(idMessages);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idTexts := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fTextSet"(idTexts, sText);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tImages" ******************************************/
CREATE OR REPLACE FUNCTION ia."fMessageImageGet"(idMessages integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('ia','tImages');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeGet"(idMessages, idRegisteredTables, 'image');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fImageAdd"(cImage bytea) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tImages';
	aColumns := '{{cImage,0}}';
	aColumns[1][2] := cImage; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageImageAdd"(idMessages integer, cImage bytea) RETURNS int_bool AS
$$
DECLARE
	idImages integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageImageGet"(idMessages);
	IF stRetVal."bValue" THEN
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fImageAdd"(cImage);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idImages := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('ia','tImages');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, 'image', idImages);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fImageSet"(idImages integer, cImage bytea) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	UPDATE ia."tImages" SET "cImage"=cImage WHERE id = idImages;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idImages;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageImageSet"(idMessages integer, cImage bytea) RETURNS int_bool AS
$$
DECLARE
	idImages integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageImageGet"(idMessages);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idImages := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fImageSet"(idImages, cImage);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tBinds" *******************************************/
CREATE OR REPLACE FUNCTION ia."fMessageBindGet"(idMessages integer) RETURNS int_bool AS
$$
DECLARE
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := hk."fRegisteredTableGet"('ia','tBinds');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeGet"(idMessages, idRegisteredTables, 'bind');
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fBindAdd"(sID character varying) RETURNS int_bool AS
$$
DECLARE
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	stTable."sSchema":='ia';
	stTable."sName":='tBinds';
	aColumns := '{{sID,0}}';
	aColumns[1][2] := sID; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageBindAdd"(idMessages integer, sID character varying) RETURNS int_bool AS
$$
DECLARE
	idBinds integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageBindGet"(idMessages);
	IF stRetVal."bValue" THEN
		stRetVal."bValue" := false;
		RETURN stRetVal;
	END IF;
	---------------------------------------------------------
	stRetVal := ia."fBindAdd"(sID);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idBinds := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := hk."fRegisteredTableGet"('ia','tBinds');
	idRegisteredTables := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fMessageAttributeAdd"(idMessages, idRegisteredTables, 'bind', idBinds);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fBindSet"(idBinds integer, sID character varying) RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	UPDATE ia."tBinds" SET "sID"=sID WHERE id = idBinds;
	stRetVal."bValue" := true;
	stRetVal."nValue" := idBinds;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION ia."fMessageBindSet"(idMessages integer, sID character varying) RETURNS int_bool AS
$$
DECLARE
	idBinds integer;
	idRegisteredTables integer;
	stRetVal int_bool;
BEGIN
	stRetVal := ia."fMessageBindGet"(idMessages);
	IF NOT stRetVal."bValue" THEN
		RETURN stRetVal;
	END IF;
	idBinds := stRetVal."nValue";
	---------------------------------------------------------
	stRetVal := ia."fBindSet"(idBinds, sID);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
/********************************** ia."tVJMessages" **************************************/
CREATE OR REPLACE FUNCTION ia."fVJMessageAdd"(sText character varying) RETURNS int_bool AS
$$
DECLARE
	idVJMessages integer;
	stTable table_name;
	aColumns text[][];
	stRetVal int_bool;
BEGIN
	IF sText IS NULL OR '' = sText THEN
		RAISE EXCEPTION 'sText can not be NULL nor empty';
	END IF;
	stTable."sSchema":='ia';
	stTable."sName":='tVJMessages';
	aColumns := '{{sText,0}}';
	aColumns[1][2] := sText; --чтобы не морочиться с экранированием
	stRetVal := "fTableAdd"(stTable, aColumns, true);
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION ia."fVJMessageCurrentGet"() RETURNS SETOF ia."tVJMessages" AS
$$
DECLARE
	stRetVal ia."tVJMessages"%ROWTYPE;
BEGIN
	SELECT * INTO stRetVal FROM ia."tVJMessages" WHERE "dtStart" IS NOT NULL AND "dtStop" IS NULL ORDER BY "dtStart" DESC LIMIT 1;
	RETURN NEXT stRetVal;
	RETURN;
END;
$$
LANGUAGE plpgsql VOLATILE;
CREATE OR REPLACE FUNCTION ia."fVJMessageStop"() RETURNS int_bool AS
$$
DECLARE
	stRetVal int_bool;
BEGIN
	SELECT id INTO stRetVal."nValue" FROM ia."fVJMessageCurrentGet"();
	IF stRetVal."nValue" IS NULL THEN
		RAISE EXCEPTION 'can not find any vj messages to stop';
	END IF;
	UPDATE ia."tVJMessages" SET "dtStop"=now() WHERE stRetVal."nValue" = id;
	stRetVal."bValue" := true;
	RETURN stRetVal;
END;
$$
LANGUAGE plpgsql VOLATILE;
