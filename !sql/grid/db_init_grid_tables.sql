CREATE SCHEMA grid;

CREATE TABLE grid."tStringValues"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"sValue" text NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tStringValues"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tNumericValues"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"nValue" integer NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tNumericValues"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tGrids"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"sName" character varying(256) UNIQUE,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tGrids"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tGridDays"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"sName" character varying(32) UNIQUE,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tGridDays"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tClocks"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"sName" character varying(256) UNIQUE,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tClocks"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tClockBinds"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idClocks" integer NOT NULL,
		"idGrids" integer NOT NULL,
		"idGridDays" integer NOT NULL,
		"nHour" integer NOT NULL,
		CHECK(24 > "nHour" AND -1 < "nHour"),
		UNIQUE("idClocks","idGrids","idGridDays","nHour"),
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idClocks") REFERENCES grid."tClocks" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
		FOREIGN KEY ("idGrids") REFERENCES grid."tGrids" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
		FOREIGN KEY ("idGridDays") REFERENCES grid."tGridDays" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tClockBinds"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tPropertyTypes"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idRegisteredTables" integer,
		"sName" character varying(256) UNIQUE,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tPropertyTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tProperties"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idPropertyTypes" integer NOT NULL,
		"sName" character varying(256) UNIQUE,
		"sINPsFunction" character varying(256),
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idPropertyTypes") REFERENCES grid."tPropertyTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tProperties"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tRangeMarks"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idProperties" integer NOT NULL,
		"nValuesTableID" integer,
		"nCount" integer NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idProperties") REFERENCES grid."tProperties" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tRangeMarks"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tRanges"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idRangeMarksForReferencePoint" integer NOT NULL,
		"idRangeMarksForDuration" integer NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRangeMarksForReferencePoint") REFERENCES grid."tRangeMarks" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRangeMarksForDuration") REFERENCES grid."tRangeMarks" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tRanges"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tFunctionKnotTypes"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"sName" character varying(256),
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tFunctionKnotTypes"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tFunctions"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idFunctionKnotTypes" integer NOT NULL,
		"sName" character varying(256),
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idFunctionKnotTypes") REFERENCES grid."tFunctionKnotTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tFunctions"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tFunctionKnotTypeBinds"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idFunctions" integer DEFAULT NULL,
		"idFunctionKnotTypes" integer NOT NULL,
		"nOverloadsNumber" integer NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idFunctions") REFERENCES grid."tFunctions" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
		FOREIGN KEY ("idFunctionKnotTypes") REFERENCES grid."tFunctionKnotTypes" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tFunctionKnotTypeBinds"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tFunctionKnots"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idParent" integer DEFAULT NULL,
		"idFunctionKnotTypeBinds" integer NOT NULL,
		"idRegisteredTables" integer NOT NULL,
		"nValuesTableID" integer,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idParent") REFERENCES grid."tFunctionKnots" (id) MATCH SIMPLE ON UPDATE CASCADE ON DELETE CASCADE,
		FOREIGN KEY ("idFunctionKnotTypeBinds") REFERENCES grid."tFunctionKnotTypeBinds" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tFunctionKnots"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tRulesSets"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"sName" character varying(256),
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tRulesSets"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tRules"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idRulesSets" integer NOT NULL,
		"nRulesSetPosition" integer NOT NULL,
		"idFunctionKnots" integer NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRulesSets") REFERENCES grid."tRulesSets" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idFunctionKnots") REFERENCES grid."tFunctionKnots" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tRules"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

CREATE TABLE grid."tRulesSetBinds"
	(
		id serial PRIMARY KEY,
		"idHK" integer NOT NULL,
		"idRulesSets" integer NOT NULL,
		"idRegisteredTables" integer NOT NULL,
		"nTargetTableID" integer NOT NULL,
		FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRulesSets") REFERENCES grid."tRulesSets" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT,
		FOREIGN KEY ("idRegisteredTables") REFERENCES hk."tRegisteredTables" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
	) 
	WITHOUT OIDS;

	CREATE TRIGGER "HKManagement"
			BEFORE INSERT OR UPDATE OR DELETE
			ON grid."tRulesSetBinds"
			FOR EACH ROW
			EXECUTE PROCEDURE hk."fManagement"();

