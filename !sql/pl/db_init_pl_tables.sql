CREATE SCHEMA pl;
		COMMENT ON SCHEMA pl IS 'Playlist management';
----------------------------------- pl."tStatuses"
	CREATE TABLE pl."tStatuses"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256) UNIQUE,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tStatuses"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tClasses"
	CREATE TABLE pl."tClasses"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idParent" integer,
			"sName" character varying(256) UNIQUE,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idParent") REFERENCES pl."tClasses" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

	CREATE INDEX "indx_tClasses_1"
			ON pl."tClasses"
			USING btree
			("sName");
			
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tClasses"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tItems"
	CREATE TABLE pl."tItems"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idStatuses" integer NOT NULL DEFAULT 0,
			"idClasses" integer NOT NULL DEFAULT 0,
			"idFiles" integer NOT NULL DEFAULT 0,
			"sName" character varying(255) NOT NULL,
			"sNote" text,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idStatuses") REFERENCES pl."tStatuses" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idClasses") REFERENCES pl."tClasses" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idFiles") REFERENCES "media"."tFiles" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tItems"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tItemAttributes"
	CREATE TABLE pl."tItemAttributes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idItems" integer,
			"idRegisteredTables" integer, -- ссылка на таблицу значений
			"sKey" character varying(128), 
			"nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idItems") REFERENCES pl."tItems" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
			FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
		COMMENT ON COLUMN pl."tItemAttributes"."idRegisteredTables" IS 'ссылка на таблицу значений';
		COMMENT ON COLUMN pl."tItemAttributes"."nValue" IS 'может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)';

	CREATE INDEX "indx_tItemAttributes_1"
			ON pl."tItemAttributes"
			USING btree
			("idItems", "sKey");

	CREATE INDEX "indx_tItemAttributes_2"
			ON pl."tItemAttributes"
			USING btree
			("idItems", "idRegisteredTables");

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tItemAttributes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tItemDTEventTypes"
	CREATE TABLE pl."tItemDTEventTypes"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" varchar(255),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;

	CREATE INDEX "indx_tItemDTEventTypes_1"
			ON pl."tItemDTEventTypes" (id, "sName");

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tItemDTEventTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tItemDTEvents"
	CREATE TABLE pl."tItemDTEvents"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idItemDTEventTypes" integer NOT NULL,
			"idItems" integer NOT NULL,
			dt timestamp with time zone NOT NULL DEFAULT now(),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idItemDTEventTypes") REFERENCES pl."tItemDTEventTypes" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE RESTRICT,
			FOREIGN KEY ("idItems") REFERENCES pl."tItems" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE
		)
		WITHOUT OIDS;

	CREATE INDEX "indx_tItemDTEvents_idItems_idItemDTEventTypes"
			ON pl."tItemDTEvents" ("idItems", "idItemDTEventTypes");
	CREATE INDEX "indx_tItemDTEvents_idItemDTEventTypes_dt"
			ON pl."tItemDTEvents"
			USING btree
			("idItemDTEventTypes", dt);

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tItemDTEvents"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tPlugs"
	CREATE TABLE pl."tPlugs"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idFiles" integer NOT NULL DEFAULT 0,
			"nFramesQty" integer NOT NULL,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idFiles") REFERENCES media."tFiles" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tPlugs"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tPlugOffsets"
	CREATE TABLE pl."tPlugOffsets"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idPlugs" integer NOT NULL DEFAULT 0,
			"nFrameOffset" integer NOT NULL,
			"dtLastUsed" timestamp with time zone NOT NULL DEFAULT now(),
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idPlugs") REFERENCES pl."tPlugs" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR DELETE
			ON pl."tPlugOffsets"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- pl."tItemsCached"
	CREATE TABLE pl."tItemsCached"
		(
			id serial PRIMARY KEY,
			"idItems" integer NOT NULL,
			"dt" timestamp with time zone NOT NULL DEFAULT now(),
			FOREIGN KEY ("idItems") REFERENCES pl."tItems" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;
----------------------------------- pl."tProxies"
	CREATE TABLE pl."tProxies"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idClasses" integer ,
			"sName" character varying(256) NOT NULL UNIQUE,
			"sFile" text,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idClasses") REFERENCES pl."tClasses" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON pl."tProxies"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();