CREATE SCHEMA archive;
/********************************** archive."tHouseKeeping" *******************************/
CREATE TABLE archive."tHouseKeeping"
(
	id serial PRIMARY KEY,
	dt timestamp with time zone NOT NULL DEFAULT now(),
	"idHouseKeeping" integer NOT NULL,
	"idUsers" integer NOT NULL, 
	"sUsername" varchar(256) NOT NULL,
	"idDTEventTypes" integer NOT NULL,
	"sDTEventTypeName" character varying(64) NOT NULL,
	"sTG_OP" character(6),
	"dtEvent" timestamp with time zone NOT NULL,
	"sNote" text
) 
WITHOUT OIDS;
ALTER TABLE archive."tHouseKeeping" OWNER TO replica_management;

/********************************** archive."pl.tItems.HouseKeepings" *********************/
CREATE TABLE archive."pl.tItems.HouseKeepings"
(
	id serial PRIMARY KEY,
	dt timestamp with time zone NOT NULL DEFAULT now(),
	"idItems" integer NOT NULL,
	"sName" character varying(255) NOT NULL,
	"idHouseKeepings" integer NOT NULL
) 
WITHOUT OIDS;
/********************************** archive."pl.tItems" ***********************************/
CREATE TABLE archive."pl.tItems"
(
	dt timestamp with time zone NOT NULL DEFAULT now(),

	id integer NOT NULL UNIQUE PRIMARY KEY,
	"sName" character varying(255) NOT NULL,
	"sNote" text,

	"idStatuses" integer NOT NULL,
	"sStatusName" character varying(255) NOT NULL,

	"idClasses" integer NOT NULL,
	"sClassName" character varying(255) NOT NULL,

	"idStorageTypes" integer NOT NULL,
	"sStorageTypeName" character varying(255) NOT NULL,

	"idStorages" integer NOT NULL,
	"bStorageEnabled" boolean,
	"sStorageName" character varying(255) NOT NULL,
	"sPath" character varying(255) NOT NULL,

	"idFiles" integer NOT NULL,
	"sFilename" character varying(255) NOT NULL,
	"dtLastFileEvent" timestamp with time zone,
	"eFileError" error,

	"nFrameStart" integer, 
	"nFrameStop" integer, 
	"nFramesQty" integer,

	"dtStartHard" timestamp with time zone,
	"dtStartSoft" timestamp with time zone,
	"dtStartPlanned" timestamp with time zone,
	"dtStartQueued" timestamp with time zone,
	"dtStartReal" timestamp with time zone,
	"dtStopReal" timestamp with time zone,

	"dtTimingsUpdate" timestamp with time zone,

	"bPlug" boolean,

	"idAssets" integer,
	"nBeepAdvBlockID" integer,
	"nBeepClipBlockID" integer
) 
WITHOUT OIDS;
CREATE INDEX "indx_archive_pl.tItems_1"
  ON archive."pl.tItems"
  USING btree
  ("sName");
CREATE INDEX "indx_archive_pl.tItems_2"
  ON archive."pl.tItems"
  USING btree
  ("idStatuses");
CREATE INDEX "indx_archive_pl.tItems_3"
  ON archive."pl.tItems"
  USING btree
  ("dtStartPlanned", "dtStartQueued");
CREATE INDEX "indx_archive_pl.tItems_4"
  ON archive."pl.tItems"
  USING btree
  ("dtStartReal", "dtStopReal");
CREATE INDEX "indx_archive_pl.tItems_5"
  ON archive."pl.tItems"
  USING btree
  ("idAssets");
CREATE INDEX "indx_archive_pl.tItems_6"
  ON archive."pl.tItems"
  USING btree
  ("eFileError");
/********************************** archive."ia.tMessages" **************************************/
CREATE TABLE archive."ia.tMessages"
(
	dt timestamp with time zone NOT NULL DEFAULT now(),

	id integer NOT NULL UNIQUE PRIMARY KEY,

	"idBinds" integer,
	"sBindID" text,

	"nCount" integer,

	"idDTEventsRegister" integer,
	"dtRegister" timestamp with time zone,
	
	"idDTEventsDisplay" integer,
	"dtDisplay" timestamp with time zone,
	
	"idGateways" integer,
	"sGatewayName" text,
	
	"idGatewayIPs" integer,
	"cGatewayIP" inet,
	
	"idNumbersSource" integer,
	"nSource" bigint,
	
	"idNumbersTarget" integer,
	"nTarget" bigint,
	
	"idTexts" integer,
	"sText" text,
	
	"idImages" integer,
	"cImage" bytea
) 
WITHOUT OIDS;
