CREATE OR REPLACE FUNCTION archive."fHouseKeepingArchive"(idHouseKeeping integer) RETURNS int_bool AS
	$$
	DECLARE
		stRetVal int_bool;
		stRecord record;
	BEGIN
		FOR stRecord IN SELECT e.id, e."idHouseKeeping", u.id AS "idUsers", u."sUsername", e."idDTEventTypes", et."sName" AS "sDTEventTypeName", et."sTG_OP", e.dt AS "dtEvent", e."sNote" FROM hk."tUsers" u, hk."tDTEventTypes" et, hk."tDTEvents" e WHERE e."idDTEventTypes" = et.id AND e."idUsers" = u.id AND idHouseKeeping = e."idHouseKeeping" LOOP
			INSERT INTO archive."tHouseKeeping" 
				("idHouseKeeping","idUsers","sUsername","idDTEventTypes","sDTEventTypeName","sTG_OP","dtEvent","sNote")
				VALUES 
				(stRecord."idHouseKeeping", stRecord."idUsers", stRecord."sUsername", stRecord."idDTEventTypes", stRecord."sDTEventTypeName", stRecord."sTG_OP", stRecord."dtEvent", stRecord."sNote");
		END LOOP;
		DELETE FROM hk."tDTEvents" WHERE idHouseKeeping = "idHouseKeeping";
		DELETE FROM hk."tHouseKeeping" WHERE idHouseKeeping = id;
		stRetVal."bValue" := true;
		stRetVal."nValue" := idHouseKeeping;
		RETURN stRetVal;
	END;
	$$
	LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION archive."fPlaylistArchive"(nItemsQty integer) RETURNS int_bool AS
	$$
	DECLARE
		stRetVal int_bool;
		dtStart timestamp with time zone;
		nID integer;
	BEGIN
		stRetVal."nValue" := 0;
		SELECT "dtStart" INTO dtStart FROM pl."vPlayListResolved" WHERE "sStatusName" IN ('played', 'failed', 'skipped') AND "dtStart" < now() - '3 hours'::interval ORDER BY "dtStart" DESC LIMIT 1;
		FOR nID IN INSERT INTO archive."pl.tItems" SELECT DISTINCT now() as dt, id, "sName", "sNote", "idStatuses", "sStatusName", "idClasses", "sClassName", 
			"idStorageTypes", "sStorageTypeName", "idStorages", "bStorageEnabled", "sStorageName", "sPath", "idFiles", "sFilename", "dtLastFileEvent", "eFileError",
			"nFrameStart", "nFrameStop", "nFramesQty", "dtStartHard", "dtStartSoft", "dtStartPlanned", "dtStartQueued", "dtStartReal", "dtStopReal", 
			"dtTimingsUpdate", "bPlug", "idAssets", "nBeepAdvBlockID", "nBeepClipBlockID" 
			FROM pl."vPlayListResolved"
			WHERE "dtStart" < dtStart
			ORDER BY "dtStartPlanned" LIMIT nItemsQty
			RETURNING id
		LOOP
			stRetVal."nValue" := stRetVal."nValue" + 1;
		END LOOP;
		stRetVal."bValue" := true;
		RETURN stRetVal;
	END;
	$$
	LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION archive."fCollector"()
	RETURNS trigger AS
	$BODY$
	DECLARE
		stRecord record;
		stResult int_bool;
	BEGIN
		--RAISE NOTICE '[TG_TABLE_SCHEMA: %] [TG_TABLE_NAME: %] [TG_OP: %]', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP;
		--IF TG_OP = 'UPDATE' OR  TG_OP = 'INSERT' THEN
		--	RETURN NEW;
		--END IF;
		--IF TG_OP = 'DELETE' THEN
		--	RETURN OLD;
		--END IF;
		IF TG_OP <> 'INSERT' THEN
			RETURN NULL;
		END IF;

		IF 'pl.tItems' = TG_TABLE_NAME THEN
			SELECT "idHK" INTO stRecord FROM pl."tItems" WHERE id = NEW.id;
			IF stRecord."idHK" IS NULL THEN
				RETURN NULL;
			END IF;
			INSERT INTO archive."pl.tItems.HouseKeepings" ("idItems", "sName", "idHouseKeepings") VALUES (NEW.id, 'item', stRecord."idHK");
			FOR stRecord IN SELECT t."sName", e."idHK" FROM pl."tItemDTEvents" e, pl."tItemDTEventTypes" t WHERE e."idItems" = NEW.id AND e."idItemDTEventTypes" = t.id LOOP
				INSERT INTO archive."pl.tItems.HouseKeepings" ("idItems", "sName", "idHouseKeepings") VALUES (NEW.id, stRecord."sName", stRecord."idHK");
			END LOOP;
			DELETE FROM pl."tItemDTEvents" WHERE NEW.id = "idItems";
			DELETE FROM pl."tItemsCached" WHERE NEW.id = "idItems";
			DELETE FROM pl."tItemAttributes" WHERE NEW.id = "idItems";
			DELETE FROM pl."tItems" WHERE NEW.id = id;
			FOR stRecord IN SELECT "idHouseKeepings" as id FROM archive."pl.tItems.HouseKeepings" WHERE NEW.id = "idItems" LOOP
				stResult := archive."fHouseKeepingArchive"(stRecord.id);
			END LOOP;
			RETURN NEW;
		ELSIF 'ia.tMessages' = TG_TABLE_NAME THEN
			DELETE FROM ia."tMessageAttributes" WHERE NEW.id = "idMessages";
			DELETE FROM ia."tMessages" WHERE NEW.id = id;
			DELETE FROM ia."tBinds" WHERE NEW."idBinds" = id;
			DELETE FROM ia."tTexts" WHERE NEW."idTexts" = id;
			DELETE FROM ia."tDTEvents" WHERE NEW."idDTEventsRegister" = id OR NEW."idDTEventsDisplay" = id;
			RETURN NEW;
		END IF;
		RETURN NULL;
	END;
	$BODY$
	LANGUAGE plpgsql VOLATILE;

CREATE TRIGGER "Collector"
	BEFORE INSERT OR UPDATE OR DELETE
	ON archive."pl.tItems"
	FOR EACH ROW
	EXECUTE PROCEDURE archive."fCollector"();

CREATE TRIGGER "Collector"
	BEFORE INSERT OR UPDATE OR DELETE
	ON archive."ia.tMessages"
	FOR EACH ROW
	EXECUTE PROCEDURE archive."fCollector"();

CREATE OR REPLACE FUNCTION archive."fMessages"() RETURNS int_bool AS
	$$
	DECLARE
		stRetVal int_bool;
		dtNow timestamp with time zone;
	BEGIN
		dtNow := now();
		---------------------------------------------------------
		INSERT INTO archive."ia.tMessages"
			SELECT
				dtNow, *
				FROM ia."vMessagesResolved" mr 
				WHERE "dtRegister" < CURRENT_DATE - '7 day'::interval OR "dtRegister" IS NULL 
				ORDER BY "dtRegister";
		---------------------------------------------------------
		SELECT count(*) INTO stRetVal."nValue" FROM archive."ia.tMessages" WHERE dt=dtNow;
		stRetVal."bValue" := true;
		RETURN stRetVal;
	END;
	$$
	LANGUAGE plpgsql VOLATILE;