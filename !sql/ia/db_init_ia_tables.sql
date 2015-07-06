CREATE SCHEMA ia;
/********************************** ia."tMessages" ****************************************/
CREATE TABLE ia."tMessages"
(
  id serial PRIMARY KEY
) 
WITHOUT OIDS;
/********************************** ia."tMessageAttributes" *******************************/
CREATE TABLE ia."tMessageAttributes"
(
  id serial PRIMARY KEY,
  "idMessages" integer NOT NULL,
  "idRegisteredTables" integer, -- если idRegisteredTables IS NULL, nValue является значением, как таковым. Если idRegisteredTables IS NOT NULL, nValue является FK на соответствующую таблицу
  "sKey" character varying(128), 
  "nValue" integer NOT NULL, -- если idRegisteredTables IS NULL, nValue является значением, как таковым. Если idRegisteredTables IS NOT NULL, nValue является FK на соответствующую таблицу
  FOREIGN KEY ("idMessages") REFERENCES ia."tMessages" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
  FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;
COMMENT ON COLUMN ia."tMessageAttributes"."idRegisteredTables" IS 'если idRegisteredTables IS NULL, nValue является значением, как таковым. Если idRegisteredTables IS NOT NULL, nValue является FK на соответствующую таблицу';
COMMENT ON COLUMN ia."tMessageAttributes"."nValue" IS 'если idRegisteredTables IS NULL, nValue является значением, как таковым. Если idRegisteredTables IS NOT NULL, nValue является FK на соответствующую таблицу';

CREATE INDEX "indx_ia_tMessageAttributes_1"
  ON ia."tMessageAttributes"
  USING btree
  ("idMessages" DESC NULLS LAST);

CREATE INDEX "indx_ia_tMessageAttributes_2"
  ON ia."tMessageAttributes"
  USING btree
  ("idRegisteredTables" ASC NULLS FIRST);

CREATE INDEX "indx_ia_tMessageAttributes_3"
  ON ia."tMessageAttributes"
  USING btree
  ("sKey");
/********************************** ia."tDTEventTypes" ************************************/
CREATE TABLE ia."tDTEventTypes"
(
  id serial PRIMARY KEY,
  "sName" varchar(255) NOT NULL UNIQUE
)
WITHOUT OIDS;
/********************************** ia."tDTEvents" ****************************************/
CREATE TABLE ia."tDTEvents"
(
  id serial PRIMARY KEY,
  "idDTEventTypes" integer NOT NULL,
  dt timestamp with time zone NOT NULL DEFAULT now(),
  FOREIGN KEY ("idDTEventTypes") REFERENCES ia."tDTEventTypes" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE RESTRICT
)
WITHOUT OIDS;
ALTER TABLE ia."tDTEvents" OWNER TO replica_management; --для возможности делать analyze перед запросами
CREATE INDEX "indx_ia_tDTEvents_1"
  ON ia."tDTEvents"
  USING btree
  (dt DESC NULLS FIRST);
/********************************** ia."tGateways" ****************************************/
CREATE TABLE ia."tGateways"
(
  id serial PRIMARY KEY,
  "sName" varchar(255) NOT NULL UNIQUE
)
WITHOUT OIDS;
/********************************** ia."tGatewayIPs" **************************************/
CREATE TABLE ia."tGatewayIPs"
(
  id serial PRIMARY KEY,
  "idGateways" integer,
  "cIP" inet NOT NULL UNIQUE,
  FOREIGN KEY ("idGateways") REFERENCES ia."tGateways" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE
) 
WITHOUT OIDS;
/********************************** ia."tNumbers" *****************************************/
CREATE TABLE ia."tNumbers"
(
  id serial PRIMARY KEY,
  "nNumber" bigint NOT NULL UNIQUE
) 
WITHOUT OIDS;

CREATE INDEX "indx_ia_tNumbers_1"
   ON ia."tNumbers" ("nNumber");
/********************************** ia."tTexts" *******************************************/
CREATE TABLE ia."tTexts"
(
  id serial PRIMARY KEY,
  "sText" text NOT NULL
) 
WITHOUT OIDS;
/********************************** ia."tImages" ******************************************/
CREATE TABLE ia."tImages"
(
  id serial PRIMARY KEY,
  "cImage" bytea NOT NULL
) 
WITHOUT OIDS;
/********************************** ia."tBinds" *******************************************/
CREATE TABLE ia."tBinds"
(
  id serial PRIMARY KEY,
  "sID" text UNIQUE
) 
WITHOUT OIDS;
/********************************** ia."tVJMessages" **************************************/
CREATE TABLE ia."tVJMessages"
(
  id serial PRIMARY KEY,
  "dtStart" timestamp with time zone NOT NULL DEFAULT now(),
  "dtStop" timestamp with time zone,
  "sText" text
) 
WITHOUT OIDS;
