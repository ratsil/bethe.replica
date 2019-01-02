CREATE SCHEMA mam;
----------------------------------- mam."tAssets"
	CREATE TABLE mam."tAssets"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256) NOT NULL UNIQUE,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tAssets"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tAssetAttributes"
	CREATE TABLE mam."tAssetAttributes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idAssets" integer NOT NULL,
			"idRegisteredTables" integer, -- ссылка на таблицу значений
			"sKey" character varying(128), 
			"nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idAssets") REFERENCES mam."tAssets" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
		COMMENT ON COLUMN mam."tAssetAttributes"."idRegisteredTables" IS 'ссылка на таблицу значений';
		COMMENT ON COLUMN mam."tAssetAttributes"."nValue" IS 'может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)';

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tAssetAttributes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tAssetTypes"
	CREATE TABLE mam."tAssetTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256) NOT NULL UNIQUE,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tAssetTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tPersonTypes"
	CREATE TABLE mam."tPersonTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256) NOT NULL UNIQUE,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tPersonTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tPersons"
	CREATE TABLE mam."tPersons"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idPersonTypes" integer NOT NULL DEFAULT 0,
			"sName" character varying(256),
			UNIQUE ("idPersonTypes", "sName"),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idPersonTypes") REFERENCES mam."tPersonTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tPersons"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tVideoTypes"
	CREATE TABLE mam."tVideoTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tVideoTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tVideos"
	CREATE TABLE mam."tVideos"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idAssets" integer NOT NULL DEFAULT 0,
			"idVideoTypes" integer NOT NULL DEFAULT 0,
			"sName" character varying(256),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idAssets") REFERENCES mam."tAssets" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idVideoTypes") REFERENCES mam."tVideoTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tVideos"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tAlbums"
	CREATE TABLE mam."tAlbums"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tAlbums"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tCues"
	CREATE TABLE mam."tCues"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sSong" text,
			"sArtist" text,
			"sAlbum" text,
			"nYear" integer,
			"sPossessor" text,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tCues"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tCategories"
	CREATE TABLE mam."tCategories"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tCategories"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tCategoryValues"
	CREATE TABLE mam."tCategoryValues"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idCategories" integer NOT NULL,
			"sValue" character varying(255),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idCategories") REFERENCES mam."tCategories" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tCategoryValues"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tTimeMapTypes"
	CREATE TABLE mam."tTimeMapTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tTimeMapTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tTimeMaps"
	CREATE TABLE mam."tTimeMaps"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idTimeMapTypes" integer NOT NULL,
			"bsMap" bit varying(1440), --кол-во минут в дне
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idTimeMapTypes") REFERENCES mam."tTimeMapTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tTimeMaps"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tTimeMapBinds"
	CREATE TABLE mam."tTimeMapBinds"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idTimeMaps" integer NOT NULL,
			"nOrder" integer NOT NULL,
			UNIQUE("idTimeMaps","nOrder"),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idTimeMaps") REFERENCES mam."tTimeMaps" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tTimeMapBinds"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tCustomValues"
	CREATE TABLE mam."tCustomValues"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sValue" character varying(256),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tCustomValues"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tMacroTypes"
	CREATE TABLE mam."tMacroTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(128) NOT NULL UNIQUE, 
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tMacroTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- mam."tMacros"
	CREATE TABLE mam."tMacros"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idMacroTypes" integer,
			"sName" character varying(256) NOT NULL UNIQUE, 
			"sValue" text,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idMacroTypes") REFERENCES mam."tMacroTypes" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON mam."tMacros"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

