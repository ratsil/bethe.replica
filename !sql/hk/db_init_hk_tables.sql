CREATE SCHEMA "hk";
----------------------------------- hk."tUsers"
	CREATE TABLE hk."tUsers"
		(
			id serial PRIMARY KEY,
			"sUsername" varchar(256) NOT NULL UNIQUE
		) 
		WITHOUT OIDS;

		GRANT ALL ON SCHEMA hk TO replica_init;
		GRANT ALL ON TABLE hk."tUsers" TO replica_init;
		INSERT INTO hk."tUsers" VALUES (0, 'pgsql');
----------------------------------- hk."tRegisteredTables"
	CREATE TABLE hk."tRegisteredTables"
		(
			id serial PRIMARY KEY,
			"sSchema" character varying(128) NOT NULL,
			"sName" character varying(128) NOT NULL,
			"dtUpdated" timestamp with time zone DEFAULT now(),
			"sNote" text,
			UNIQUE ("sSchema", "sName")
		) 
		WITHOUT OIDS;
----------------------------------- hk."tErrorScopes"
	CREATE TABLE hk."tErrorScopes"
		(
			id serial PRIMARY KEY,
			"idRegisteredTables" integer NOT NULL,
			"eError"	error NOT NULL,
			FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
----------------------------------- hk."tUserAttributes"
	CREATE TABLE hk."tUserAttributes"
		(
			id serial PRIMARY KEY,
			"idUsers" integer NOT NULL, 
			"idRegisteredTables" integer, -- ссылка на таблицу значений
			"sKey" character varying(128), 
			"nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
			FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE("idUsers","idRegisteredTables","sKey","nValue")
		)
		WITHOUT OIDS;
		COMMENT ON COLUMN hk."tUserAttributes"."idRegisteredTables" IS 'ссылка на таблицу значений';
		COMMENT ON COLUMN hk."tUserAttributes"."nValue" IS 'может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)';
----------------------------------- hk."tHouseKeeping"
	CREATE TABLE hk."tHouseKeeping"
		(
			id serial PRIMARY KEY
		) 
		WITHOUT OIDS;
----------------------------------- hk."tDTEventTypes"
	CREATE TABLE hk."tDTEventTypes"
		(
			id serial PRIMARY KEY,
			"sName" character varying(64) NOT NULL UNIQUE,
			"sTG_OP" character(6) -- поле соответствия тригеррным операциям
		) 
		WITHOUT OIDS;
----------------------------------- hk."tDTEvents"
	CREATE TABLE hk."tDTEvents"
		(
			id serial PRIMARY KEY,
			"idDTEventTypes" integer NOT NULL,
			"idHouseKeeping" integer NOT NULL,
			"idUsers" integer NOT NULL,
			dt timestamp with time zone NOT NULL DEFAULT now(),
			"sNote" text,
			FOREIGN KEY ("idDTEventTypes") REFERENCES hk."tDTEventTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idHouseKeeping") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idUsers") REFERENCES hk."tUsers" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
	CREATE INDEX "indx_hk_tDTEvents_1"
			ON hk."tDTEvents"
			USING btree
			("idHouseKeeping");
----------------------------------- hk."tWebPages"
	CREATE TABLE hk."tWebPages"
		(
			id serial PRIMARY KEY,
			"idParent" integer DEFAULT NULL,
			"sPage" varchar(64),
			FOREIGN KEY ("idParent") REFERENCES hk."tWebPages" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE ("idParent", "sPage")
		) 
		WITHOUT OIDS;
----------------------------------- hk."tAccessRoles"
	CREATE TABLE hk."tAccessRoles"
		(
			id serial PRIMARY KEY,
			"sName" varchar(256) NOT NULL,
			UNIQUE ("sName")
		) 
		WITHOUT OIDS;
----------------------------------- hk."tAccessScopes"
	CREATE TABLE hk."tAccessScopes"
		(
			id serial PRIMARY KEY,
			"idParent" integer,
			"sName" varchar(256) NOT NULL,
			FOREIGN KEY ("idParent") REFERENCES hk."tAccessScopes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE ("idParent", "sName")
		) 
		WITHOUT OIDS;
----------------------------------- hk."tAccessPermissions"
	CREATE TABLE hk."tAccessPermissions"
		(
			id serial PRIMARY KEY,
			"idAccessScopes" integer NOT NULL,
			"idAccessRoles" integer NOT NULL,
			"bCreate" boolean NOT NULL DEFAULT false,
			"bUpdate" boolean NOT NULL DEFAULT false,
			"bDelete" boolean NOT NULL DEFAULT false,
			FOREIGN KEY ("idAccessScopes") REFERENCES hk."tAccessScopes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idAccessRoles") REFERENCES hk."tAccessRoles" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE ("idAccessScopes", "idAccessRoles")
		)
		WITHOUT OIDS;

