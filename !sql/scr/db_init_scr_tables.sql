CREATE SCHEMA scr;
----------------------------------- scr."tStoragesMappings"
	CREATE TABLE scr."tStoragesMappings"
		(
			id serial PRIMARY KEY,
			"idStorages" integer NOT NULL,
			"sLocalPath" text NOT NULL,
			UNIQUE("idStorages", "sLocalPath"),
			FOREIGN KEY ("idStorages") REFERENCES media."tStorages" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
----------------------------------- scr."tTemplates"
	CREATE TABLE scr."tTemplates"
		(
			id serial PRIMARY KEY,
			"sName" varchar(255) NOT NULL UNIQUE
		) 
		WITHOUT OIDS;
----------------------------------- scr."tShifts"
	CREATE TABLE scr."tShifts"
		(
			id serial PRIMARY KEY,
			"idTemplates" integer NOT NULL DEFAULT 0,
			dt timestamp with time zone NOT NULL DEFAULT now(),
			"dtStart" timestamp with time zone DEFAULT NULL,
			"dtStop" timestamp with time zone DEFAULT NULL,
			"sSubject" text NOT NULL,
			FOREIGN KEY ("idTemplates") REFERENCES scr."tTemplates" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
----------------------------------- scr."tPlaques"
	CREATE TABLE scr."tPlaques"
		(
			id serial PRIMARY KEY,
			"idTemplates" integer NOT NULL,
			"sName" character varying(128), 
			"sFirstLine" text NOT NULL,
			"sSecondLine" text,
			UNIQUE("idTemplates", "sName"),
			FOREIGN KEY ("idTemplates") REFERENCES scr."tTemplates" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;
----------------------------------- scr."tMessagesMarks"
	CREATE TABLE scr."tMessagesMarks"
		(
			id serial PRIMARY KEY,
			"idMessages" integer UNIQUE,
			FOREIGN KEY ("idMessages") REFERENCES ia."tMessages" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;
----------------------------------- scr."tAnnouncements"
	CREATE TABLE scr."tAnnouncements"
		(
			id serial PRIMARY KEY,
			"idShifts" integer DEFAULT NULL,
			"sText" text NOT NULL,
			FOREIGN KEY ("idShifts") REFERENCES scr."tShifts" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		)
		WITHOUT OIDS;
----------------------------------- scr."tCueTypes"
	CREATE TABLE scr."tCueTypes"
		(
			id serial PRIMARY KEY,
			"sName" varchar(128) NOT NULL UNIQUE
		)
		WITHOUT OIDS;
----------------------------------- scr."tCues"
	CREATE TABLE scr."tCues"
		(
			id serial PRIMARY KEY,
			"idCueTypes" integer NOT NULL,
			"idTemplates" integer,
			"sTemplateFile" text NOT NULL,
			FOREIGN KEY ("idCueTypes") REFERENCES scr."tCueTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			FOREIGN KEY ("idTemplates") REFERENCES scr."tTemplates" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
			UNIQUE("idCueTypes","idTemplates"),
			UNIQUE("idTemplates","sTemplateFile")
		)
		WITHOUT OIDS;
