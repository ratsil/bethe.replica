CREATE SCHEMA logs;
CREATE TABLE logs."tPlaylistLog"
(
  id serial PRIMARY KEY,
  "idHK" integer NOT NULL,
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;

CREATE TABLE logs."tPlaylistLogAttributes"
(
  id serial PRIMARY KEY,
  "idHK" integer NOT NULL,
  "idPlaylist" integer NOT NULL,
  "idRegisteredTables" integer, -- ссылка на таблицу значений
  "sKey" character varying(128), 
  "nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
  FOREIGN KEY ("idPlaylist") REFERENCES logs."tPlaylistLog" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
  FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;
COMMENT ON COLUMN logs."tPlaylistLogAttributes"."idRegisteredTables" IS 'ссылка на таблицу значений';
COMMENT ON COLUMN logs."tPlaylistLogAttributes"."nValue" IS 'может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)';

/********************************** logs."tSCR" *******************************************/
CREATE TABLE logs."tSCR"
(
  id serial PRIMARY KEY,
  "idShifts" integer NOT NULL,
  "idAssets" integer NOT NULL,
  "dtStart" timestamp with time zone NOT NULL DEFAULT now(),
  "dtStop" timestamp with time zone DEFAULT NULL,
  FOREIGN KEY ("idShifts") REFERENCES scr."tShifts" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
  FOREIGN KEY ("idAssets") REFERENCES mam."tAssets" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;
