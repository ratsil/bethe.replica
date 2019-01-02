CREATE SCHEMA adm;
----------------------------------- adm."tCommands"
	CREATE TABLE adm."tCommands"
		(
			id serial PRIMARY KEY,
			"sName" varchar(64) NOT NULL
		) 
		WITHOUT OIDS;
----------------------------------- adm."tCommandStatuses"
	CREATE TABLE adm."tCommandStatuses"
		(
			id serial PRIMARY KEY,
			"sName" varchar(64) NOT NULL
		) 
		WITHOUT OIDS;
----------------------------------- adm."tCommandsQueue"
	CREATE TABLE adm."tCommandsQueue"
		(
			id serial PRIMARY KEY,
			dt timestamp with time zone NOT NULL DEFAULT now(),
			"idCommands" integer NOT NULL,
			"idCommandStatuses" integer NOT NULL DEFAULT 1,
			"idUsers" integer NOT NULL,
			FOREIGN KEY ("idCommands") REFERENCES adm."tCommands" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idCommandStatuses") REFERENCES adm."tCommandStatuses" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idUsers") REFERENCES hk."tUsers" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
----------------------------------- adm."tCommandParameters"
	CREATE TABLE adm."tCommandParameters"
		(
			id serial PRIMARY KEY,
			"idCommandsQueue" integer NOT NULL,
			"sKey" varchar(128) NOT NULL,
			"sValue" text NOT NULL,
			FOREIGN KEY ("idCommandsQueue") REFERENCES adm."tCommandsQueue" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE ("idCommandsQueue", "sKey")
		) 
		WITHOUT OIDS;
----------------------------------- adm."tPreferenceClasses"
	CREATE TABLE adm."tPreferenceClasses"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"sName" character varying(256),
			"bActive" boolean DEFAULT false,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE("sName", "bActive")
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON adm."tPreferenceClasses"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- adm."tPreferences"
	CREATE TABLE adm."tPreferences"
		(
			id serial PRIMARY KEY,
			"idHK" integer NOT NULL,
			"idPreferenceClasses" integer NOT NULL,
			"sName" character varying(256),
			"sValue" text,
			"bActive" boolean DEFAULT false,
			"sNote" text,
			FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idPreferenceClasses") REFERENCES adm."tPreferenceClasses" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE("idPreferenceClasses", "sName", "bActive")
		) 
		WITHOUT OIDS;
	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON adm."tPreferences"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();
----------------------------------- adm."tTransliteration"
	CREATE TABLE adm."tTransliteration"
		(
			id serial PRIMARY KEY,
			"sSource" character(1) NOT NULL,
			"sTarget" character varying(2) NOT NULL,
			UNIQUE("sSource", "sTarget")
		) 
		WITHOUT OIDS;
