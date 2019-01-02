CREATE SCHEMA media;
----------------------------------- media."tStorageTypes"
	CREATE TABLE media."tStorageTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256) NOT NULL UNIQUE,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON media."tStorageTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- media."tStorages"
	CREATE TABLE media."tStorages"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idStorageTypes" integer NOT NULL,
			"sName" character varying(256) UNIQUE,
			"sPath" text NOT NULL UNIQUE,
			"bEnabled" boolean NOT NULL DEFAULT false,
			"idVideoTypes" integer,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idStorageTypes") REFERENCES media."tStorageTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON media."tStorages"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- media."tFiles"
	CREATE TABLE media."tFiles"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"dtLastFileEvent" timestamp with time zone NOT NULL DEFAULT now(),
			"idStorages" integer NOT NULL,
			"sFilename" text NOT NULL,
			UNIQUE("idStorages", "sFilename"),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idStorages") REFERENCES media."tStorages" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON media."tFiles"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- media."tFileAttributes"
	CREATE TABLE media."tFileAttributes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idFiles" integer NOT NULL,
			"idRegisteredTables" integer, -- ссылка на таблицу значений
			"sKey" character varying(128), 
			"nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idFiles") REFERENCES media."tFiles" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
		COMMENT ON COLUMN media."tFileAttributes"."idRegisteredTables" IS 'ссылка на таблицу значений';
		COMMENT ON COLUMN media."tFileAttributes"."nValue" IS 'может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)';

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON media."tFileAttributes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
		----------------------------------- media."tStrings"
	CREATE TABLE media."tStrings"
		(
			id bigserial PRIMARY KEY,
			"nQty" bigint NOT NULL DEFAULT 0,
			"oValue" text UNIQUE
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "media_tStrings_fDictionary"
			BEFORE INSERT OR UPDATE OR DELETE
			ON media."tStrings"
			FOR EACH ROW
			EXECUTE PROCEDURE "fDictionary"();
		----------------------------------- media."tDates"
	CREATE TABLE media."tDates"
		(
			id serial NOT NULL,
			dt timestamp with time zone,
			CONSTRAINT "tDates_pkey" PRIMARY KEY (id)
		);
