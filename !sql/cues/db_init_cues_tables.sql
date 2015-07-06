CREATE SCHEMA cues;
/********************************** cues."tTemplates" *************************************/
CREATE TABLE cues."tTemplates"
(
  id serial PRIMARY KEY,
  "idHK" integer NOT NULL,
  "sName" character varying(256) NOT NULL UNIQUE,
  "sFile" text,
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;
CREATE TRIGGER "HKManagement"
  BEFORE INSERT OR UPDATE OR DELETE
  ON cues."tTemplates"
  FOR EACH ROW
  EXECUTE PROCEDURE hk."fManagement"();
/********************************** cues."tClassAndTemplateBinds" **********************/
CREATE TABLE cues."tClassAndTemplateBinds"
(
  id serial PRIMARY KEY,
  "idHK" integer NOT NULL,
  "idClasses" integer,
  "idTemplates" integer,
  "idRegisteredTables" integer, -- ссылка на таблицу значений
  "sKey" character varying(128), 
  "nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
  FOREIGN KEY ("idClasses") REFERENCES pl."tClasses" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
  FOREIGN KEY ("idTemplates") REFERENCES cues."tTemplates" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
  FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
)
WITHOUT OIDS;
CREATE TRIGGER "HKManagement"
  BEFORE INSERT OR UPDATE OR DELETE
  ON cues."tClassAndTemplateBinds"
  FOR EACH ROW
  EXECUTE PROCEDURE hk."fManagement"();
/********************************** cues."tTemplatesSchedule" **************************/
CREATE TABLE cues."tTemplatesSchedule"
(
  id serial PRIMARY KEY,
  "idHK" integer NOT NULL,
  "idClassAndTemplateBinds" integer NOT NULL,
  "dtLast" timestamp with time zone DEFAULT NULL,
  "dtStart" timestamp with time zone NOT NULL,
  "tsInterval" interval DEFAULT NULL,
  "dtStop" timestamp with time zone DEFAULT NULL,
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
  FOREIGN KEY ("idClassAndTemplateBinds") REFERENCES cues."tClassAndTemplateBinds" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;
CREATE TRIGGER "HKManagement"
  BEFORE INSERT OR UPDATE OR DELETE
  ON cues."tTemplatesSchedule"
  FOR EACH ROW
  EXECUTE PROCEDURE hk."fManagement"();
/********************************** cues."tDictionary" *********************************/
CREATE TABLE cues."tDictionary"
(
  id serial PRIMARY KEY,
  "idRegisteredTables" integer NOT NULL, -- ссылка на таблицу значений
  "idTarget" integer NOT NULL,
  "sKey" character varying(128), 
  "sValue" text NOT NULL,
  FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
)
WITHOUT OIDS;


CREATE TABLE cues."tChatInOuts"
(
  id serial NOT NULL,
  "idHK" integer NOT NULL,
  "idAssets" integer,
  "nFrameIn" integer NOT NULL,
  "nFrameOut" integer NOT NULL,
  CONSTRAINT "tChatInOuts_pkey" PRIMARY KEY (id),
  CONSTRAINT "tChatInOuts_idAssets_fkey" FOREIGN KEY ("idAssets")
      REFERENCES mam."tAssets" (id) MATCH SIMPLE
      ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT "tChatInOuts_idHK_fkey" FOREIGN KEY ("idHK")
      REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE
      ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT "tChatInOuts_check" CHECK (("nFrameOut" - "nFrameIn") > (25 * 20))
)
WITH (
  OIDS=FALSE
);