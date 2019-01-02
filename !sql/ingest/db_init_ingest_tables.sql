	CREATE SCHEMA ingest;
----------------------------------- ingest."tItems"
	CREATE TABLE ingest."tItems"
		(
			id serial PRIMARY KEY, 
			dt timestamp with time zone NOT NULL DEFAULT now(),
			"sName" text NOT NULL UNIQUE
		) 
		WITHOUT OIDS;

----------------------------------- ingest."tItemAttributes"
	CREATE TABLE ingest."tItemAttributes"
		(
			id serial PRIMARY KEY,
			"idItems" integer,
			"idRegisteredTables" integer, -- ссылка на таблицу значений
			"sKey" character varying(128), 
			"nValue" integer NOT NULL, -- может быть значением, как таковым (если idRegisteredTables=NULL) или может быть ссылкой на запись в таблице значений (если idRegisteredTables!=NULL)
			FOREIGN KEY ("idItems") REFERENCES ingest."tItems" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
			FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
		) 
		WITHOUT OIDS;

----------------------------------- ingest."tSongs"
	CREATE TABLE ingest."tSongs"
		(
			id serial PRIMARY KEY,
			"sName" text
		) 
		WITHOUT OIDS;