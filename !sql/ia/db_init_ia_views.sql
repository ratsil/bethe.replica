CREATE OR REPLACE VIEW ia."vMessageAttributes" AS 
	SELECT *
		FROM ia."tMessageAttributes";

CREATE OR REPLACE VIEW ia."vMessageAttributesResolved" AS 
	SELECT "idMessages",
			max(CASE WHEN 'bind' = "sKey" THEN "nValue" END) AS "idBinds" , 
			max(CASE WHEN 'parts' = "sKey" THEN "nValue" END) as "nCount",  
			max(CASE WHEN 'register' = "sKey" THEN "nValue" END) as "idDTEventsRegister",  
			max(CASE WHEN 'display' = "sKey" THEN "nValue" END) as "idDTEventsDisplay",  
			max(CASE WHEN 'gateway_ip' = "sKey" THEN "nValue" END) AS "idGatewayIPs",
			max(CASE WHEN 'source' = "sKey" THEN "nValue" END) AS "idNumbersSource",
			max(CASE WHEN 'target' = "sKey" THEN "nValue" END) AS "idNumbersTarget",
			max(CASE WHEN 'text' = "sKey" THEN "nValue" END) AS "idTexts",
			max(CASE WHEN 'image' = "sKey" THEN "nValue" END) AS "idImages"
		FROM ia."tMessageAttributes"
		GROUP BY "idMessages";

CREATE OR REPLACE VIEW ia."vGateways" AS 
	SELECT g.id, gi.id AS "idGatewayIPs", gi."cIP", g."sName"
		FROM ia."tGateways" g, ia."tGatewayIPs" gi
		WHERE g.id = gi."idGateways";

CREATE OR REPLACE VIEW ia."vDTEvents" AS 
	SELECT dts.id, dts_tp.id AS "idDTEventTypes", dts_tp."sName" AS "sTypeName", dts.dt
		FROM ia."tDTEventTypes" dts_tp, ia."tDTEvents" dts
		WHERE dts_tp.id = dts."idDTEventTypes";

CREATE OR REPLACE VIEW ia."vMessageBinds" AS 
	SELECT mas."idMessages" AS id, b.id AS "idBinds", b."sID" AS "sBindID"
		FROM ia."vMessageAttributes" mas, ia."tBinds" b
		WHERE b.id = mas."nValue" AND 'bind' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageGateways" AS 
	SELECT mas."idMessages" AS id, g.id AS "idGateways", g."sName" AS "sGatewayName", g."idGatewayIPs", g."cIP" AS "cGatewayIP"
		FROM ia."vMessageAttributes" mas, ia."vGateways" g
		WHERE g."idGatewayIPs" = mas."nValue" AND 'gateway_ip' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageDTEventsDisplay" AS 
	SELECT mas."idMessages" AS id, dts.id as "idDTEvents", dts.dt
		FROM ia."vMessageAttributes" mas, ia."tDTEvents" dts
		WHERE dts.id = mas."nValue" AND 'display' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageDTEventsRegister" AS 
	SELECT mas."idMessages" AS id, dts.id as "idDTEvents", dts.dt
		FROM ia."vMessageAttributes" mas, ia."tDTEvents" dts
		WHERE dts.id = mas."nValue" AND 'register' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageNumbersSource" AS 
	SELECT mas."idMessages" AS id, ns.id as "idNumbers", ns."nNumber"
		FROM ia."vMessageAttributes" mas, ia."tNumbers" ns
		WHERE ns.id = mas."nValue" AND 'source' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageNumbersTarget" AS 
	SELECT mas."idMessages" AS id, ns.id as "idNumbers", ns."nNumber"
		FROM ia."vMessageAttributes" mas, ia."tNumbers" ns
		WHERE ns.id = mas."nValue" AND 'target' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageTexts" AS 
	SELECT mas."idMessages" AS id, ts.id as "idTexts", ts."sText"
		FROM ia."vMessageAttributes" mas, ia."tTexts" ts
		WHERE ts.id = mas."nValue" AND 'text' = mas."sKey";

CREATE OR REPLACE VIEW ia."vMessageImages" AS 
	SELECT mas."idMessages" AS id, i.id as "idImages", i."cImage"
		FROM ia."vMessageAttributes" mas, ia."tImages" i
		WHERE i.id = mas."nValue" AND 'image' = mas."sKey";
/*
CREATE OR REPLACE VIEW ia."vMessagesResolved" AS 
	SELECT msg.id, b."idBinds", b."sBindID", p."nValue" AS "nCount", dtr."idDTEvents" AS "idDTEventsRegister", dtr.dt AS "dtRegister", dtd."idDTEvents" AS "idDTEventsDisplay", dtd.dt AS "dtDisplay", 
			g."idGateways", g."sGatewayName", g."idGatewayIPs", g."cGatewayIP", ns."idNumbers" as "idNumbersSource", ns."nNumber" AS "nSource", nt."idNumbers" as "idNumbersTarget", nt."nNumber" AS "nTarget", t."idTexts", t."sText", i."idImages", i."cImage"
		FROM
			ia."tMessages" msg
			LEFT JOIN ia."vMessageBinds" b ON b.id = msg.id
			LEFT JOIN ia."vMessageDTEventsRegister" dtr ON dtr.id = msg.id
			LEFT JOIN ia."vMessageDTEventsDisplay" dtd ON dtd.id = msg.id
			LEFT JOIN ia."tMessageAttributes" p ON p."idMessages" = msg.id AND 'parts' = p."sKey"
			LEFT JOIN ia."vMessageGateways" g ON g.id = msg.id
			LEFT JOIN ia."vMessageNumbersSource" ns ON ns.id = msg.id
			LEFT JOIN ia."vMessageNumbersTarget" nt ON nt.id = msg.id
			LEFT JOIN ia."vMessageTexts" t ON t.id = msg.id
			LEFT JOIN ia."vMessageImages" i ON i.id = msg.id;
*/
CREATE OR REPLACE VIEW ia."vMessagesResolved" AS 
	SELECT mar."idMessages" as id, mar."idBinds", b."sID" as "sBindID", mar."nCount", mar."idDTEventsRegister", dtr.dt AS "dtRegister", mar."idDTEventsDisplay", dtd.dt AS "dtDisplay", gi."idGateways", g."sName" as "sGatewayName", mar."idGatewayIPs", gi."cIP" as "cGatewayIP", mar."idNumbersSource", ns."nNumber" AS "nSource", mar."idNumbersTarget", nt."nNumber" AS "nTarget", mar."idTexts", t."sText", mar."idImages", i."cImage"
		FROM ia."tDTEvents" dtr, ia."tNumbers" nt, ia."tGatewayIPs" gi, ia."tGateways" g, ia."tTexts" t, ia."vMessageAttributesResolved" mar
			LEFT JOIN ia."tDTEvents" dtd ON dtd.id = mar."idDTEventsDisplay"
			LEFT JOIN ia."tNumbers" ns ON ns.id = mar."idNumbersSource"
			LEFT JOIN ia."tBinds" b ON b.id = mar."idBinds"
			LEFT JOIN ia."tImages" i ON i.id = mar."idImages"
		WHERE dtr.id = mar."idDTEventsRegister" AND gi.id = mar."idGatewayIPs" AND g.id = gi."idGateways" AND nt.id = mar."idNumbersTarget" AND t.id = mar."idTexts";
