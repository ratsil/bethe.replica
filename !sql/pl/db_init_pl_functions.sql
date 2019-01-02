----------------------------------- pl."tStatuses"
	CREATE OR REPLACE FUNCTION pl."fStatusGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tStatuses';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fStatusAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tStatuses';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- pl."tClasses"
	CREATE OR REPLACE FUNCTION pl."fClassGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tClasses';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fClassAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tClasses';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fClassAdd"(sName character varying, idParent integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tClasses';
			aColumns := '{{sName,0},{idParent,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			aColumns[2][2] := idParent;
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION pl."fClassAdd"(sName character varying, sClassNameForInherit character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fClassGet"(sClassNameForInherit);
			IF NOT stRetVal."bValue" THEN
				RETURN stRetVal;
			END IF;
			stRetVal := pl."fClassAdd"(sName, stRetVal."nValue");
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- pl."tItems"
	CREATE OR REPLACE FUNCTION pl."fItemGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItems';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemGet"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItems';
			aColumns := '{{id,0}}';
			aColumns[1][2] := id; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAdd"(sName character varying, idClasses integer, idFiles integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItems';
			aColumns := '{{sName,0},{idClasses,0},{idFiles,0},{idStatuses,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			aColumns[2][2] := idClasses; 
			aColumns[3][2] := idFiles; 
			aColumns[4][2] := 1; 
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemClassSet"(idItems integer, idClasses integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			UPDATE pl."tItems" SET "idClasses"=idClasses WHERE id=idItems;
			stRetVal."bValue" := true;
			stRetVal."nValue" := idItems;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fIsItemLocked"(id integer) RETURNS bool AS
		$$
		DECLARE
			bRetVal bool;
		BEGIN
			SELECT CASE WHEN 'planned'=s."sName" THEN false ELSE true END as "bLocked" INTO bRetVal FROM pl."tItems" i, pl."tStatuses" s WHERE i.id=$1 AND i."idStatuses"=s.id;
			---------------------------------------------------------
			RETURN bRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemRemove"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			IF pl."fIsItemLocked"(id) THEN
				stRetVal."bValue":=false;
				return stRetVal;
			END IF;

			stTable."sSchema":='pl';
			aColumns := '{{idItems,'||COALESCE(id::text,'NULL')||'}}';

			stTable."sName":='tItemDTEvents';
			stRetVal := "fTableRemove"(stTable, aColumns);

			stTable."sName":='tItemAttributes';
			stRetVal := "fTableRemove"(stTable, aColumns);

			aColumns := '{{id,'||COALESCE(id::text,'NULL')||'}}';
			stTable."sName":='tItems';
			stRetVal := "fTableRemove"(stTable, aColumns);
			---------------------------------------------------------
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemsRemove"(nStartID integer, nQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			nMaxID integer;
			nRowsQty integer;
			nRowsRemoved integer;
		BEGIN
			SELECT max(id) INTO nMaxID FROM pl."tItems";
			nRowsQty := nQty;
			IF nQty IS NULL THEN
				nRowsQty := nMaxID-nStartID;
			END IF;
			stRetVal."bValue" := false;
			nRowsRemoved := 0;
			FOR idItems IN nStartID..(nStartID+nRowsQty) LOOP
				stRetVal := pl."fItemRemove"(idItems);
				IF stRetVal."bValue" THEN
					nRowsRemoved := nRowsRemoved + 1;
				ELSEIF nMaxID > idItems THEN
					nRowsQty := nRowsQty + 1;
				END IF;
			END LOOP;
			---------------------------------------------------------
			stRetVal."nValue" := nRowsRemoved;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemsClear"(nStartID integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemsRemove"(nStartID, NULL);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemsClear"() RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			nStartID int;
		BEGIN
			SELECT id INTO nStartID FROM pl."tItems" WHERE NOT pl."fIsItemLocked"(id) ORDER BY id LIMIT 1;
			IF nStartID IS NULL THEN
				stRetVal."nValue" := -1;
				stRetVal."bValue" := true;
				RETURN stRetVal;
			END IF;
			stRetVal := pl."fItemsRemove"(nStartID, NULL);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemPlannedAdd"(sName character varying, idClasses integer, idFiles integer, dtStartPlanned timestamp with time zone, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemAdd"(sName, idClasses, idFiles);
			idItems := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventTypeAdd"('start_planned');
			idItemDTEventTypes := stRetVal."nValue";
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, date_trunc('second', dtStartPlanned));
			---------------------------------------------------------
			--stRetVal := pl."fItemDTEventTypeAdd"('stop_planned');
			--idItemDTEventTypes := stRetVal."nValue";
			--stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, date_trunc('second', dtStopPlanned));
			---------------------------------------------------------
			--nFramesQty := date_part('epoch',dtStopPlanned-dtStartPlanned)::int*25;
			stRetVal := pl."fItemAttributeSet"(idItems, 'nFramesQty', nFramesQty);
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			--RAISE NOTICE 'FUNCTION "fItemPlannedAdd" (sName:%, idClasses:%, idFiles:%, dtStartPlanned:%, dtStopPlanned:%, nFramesQty:%)', sName, idClasses, idFiles, dtStartPlanned, dtStopPlanned, nFramesQty;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartPlannedSet"(idItems integer, dtStartPlanned timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemDTEventTypeAdd"('start_planned');
			idItemDTEventTypes := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, dtStartPlanned);
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartHardSet"(idItems integer, dtStartHard timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemDTEventTypeAdd"('start_hard');
			idItemDTEventTypes := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, dtStartHard);
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartSoftSet"(idItems integer, dtStartSoft timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemDTEventTypeAdd"('start_soft');
			idItemDTEventTypes := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, dtStartSoft);
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartQueuedSet"(idItems integer, dtStartQueued timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemDTEventTypeAdd"('start_queued');
			idItemDTEventTypes := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, dtStartQueued);
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemTimingsUpdateSet"(idItems integer, dtTimingsUpdate timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemDTEventTypeAdd"('timings_update');
			idItemDTEventTypes := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, dtTimingsUpdate);
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartsSet"(idItems integer, dtPlanned timestamp with time zone, dtHard timestamp with time zone, dtSoft timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('pl', 'tItemDTEvents');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventTypeAdd"('start_hard');
			DELETE FROM pl."tItemDTEvents" WHERE "idItems" = idItems AND "idItemDTEventTypes"=stRetVal."nValue";
			stRetVal := pl."fItemDTEventTypeAdd"('start_soft');
			DELETE FROM pl."tItemDTEvents" WHERE "idItems" = idItems AND "idItemDTEventTypes"=stRetVal."nValue";
			---------------------------------------------------------
			IF dtPlanned IS NOT NULL THEN
				stRetVal := pl."fItemDTEventTypeAdd"('start_planned');
				stRetVal := pl."fItemDTEventSet"(stRetVal."nValue", idItems, dtPlanned);
			END IF;
			IF dtHard IS NOT NULL THEN
				stRetVal := pl."fItemStartHardSet"(idItems, dtHard);
			ELSIF dtSoft IS NOT NULL THEN
				stRetVal := pl."fItemStartSoftSet"(idItems, dtSoft);
			END IF;
			---------------------------------------------------------
			--stRetVal."nValue":=idItems;
			--stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStatusChanged"(id integer, sStatusName varchar) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
		BEGIN
			stRetVal := pl."fItemGet"(id);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'NO SUCH ID:%',id;
			END IF;
			idItems := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fStatusGet"(sStatusName);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'NO SUCH STATUS:%',sStatusName;
			END IF;
			UPDATE pl."tItems" i SET "idStatuses"=stRetVal."nValue" WHERE i.id=idItems;
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;


	CREATE OR REPLACE FUNCTION pl."fItemQueued"(id integer, dtStartQueued timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemStatusChanged"(id, 'queued');
			IF stRetVal."bValue" THEN
				stRetVal := pl."fItemStartQueuedSet"(id, dtStartQueued);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemPrepared"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
		BEGIN
			stRetVal := pl."fItemStatusChanged"(id, 'prepared');
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStarted"(id integer, nFrameStarted integer, dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemGet"(id);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'NO SUCH ID:%',id;
			END IF;
			idItems := stRetVal."nValue";
			stRetVal := pl."fItemStatusChanged"(id, 'onair');
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventTypeAdd"('start_real');
			idItemDTEventTypes := stRetVal."nValue";
			stRetVal := pl."fItemDTEventGet"(idItemDTEventTypes, idItems);
			IF stRetVal."bValue" THEN
				stRetVal."bValue" := false;
				stRetVal."nValue" := idItems;
				return stRetVal;
			END IF;
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, dt);
			---------------------------------------------------------
			stRetVal := pl."fItemAttributeSet"(id, 'nFrameStarted', nFrameStarted);
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableUpdate"('pl','tItems');
			stRetVal."nValue":=idItems;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStarted"(id integer, nFrameStarted integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemStarted"(id, nFrameStarted, now());
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStopped"(id integer, dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
			idItemDTEventTypes integer;
		BEGIN
			stRetVal := pl."fItemGet"(id);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'NO SUCH ID:%',id;
			END IF;
			idItems := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemDTEventTypeAdd"('stop_real');
			idItemDTEventTypes := stRetVal."nValue";
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, date_trunc('second', dt));
			stRetVal := pl."fItemStatusChanged"(id, 'played');
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStopped"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemStopped"(id, now());
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemSkipped"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
		BEGIN
			stRetVal := pl."fItemStatusChanged"(id, 'skipped');
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemFailed"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idItems integer;
		BEGIN
			stRetVal := pl."fItemStatusChanged"(id, 'failed');
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartPlannedGet"(idItems integer) RETURNS timestamp with time zone AS
		$$
		DECLARE
			dtRetVal timestamp with time zone;
		BEGIN
			SELECT dt INTO dtRetVal FROM pl."vItemDTEvents" WHERE "idItems"=idItems AND 'start_planned'="sTypeName";
			IF NOT FOUND THEN
				SELECT "dtStartPlanned" INTO dtRetVal FROM archive."vPlayListResolvedFull" WHERE id=idItems;
			END IF;
			RETURN dtRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemStartTimedGet"(idItems integer) RETURNS timestamp with time zone AS
		$$
		DECLARE
			dtRetVal timestamp with time zone;
		BEGIN
			SELECT dt INTO dtRetVal FROM pl."vItemDTEvents" WHERE "idItems"=idItems AND ('start_hard'="sTypeName" OR 'start_soft'="sTypeName");
			IF NOT FOUND THEN
				SELECT "dtStartPlanned" INTO dtRetVal FROM archive."vPlayListResolvedFull" WHERE id=idItems;
			END IF;
			RETURN dtRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemByDTGet"(dt timestamp with time zone, bSequential bool, bForward bool) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			stRecord RECORD;
		BEGIN
			IF bForward THEN
				IF bSequential IS NULL THEN
					SELECT DISTINCT id, "dtStart" INTO stRecord FROM archive."vPlayListResolvedFull" WHERE "dtStart" >= dt ORDER BY "dtStart", id LIMIT 1;
				ELSIF bSequential THEN
					SELECT DISTINCT id, "dtStart" INTO stRecord FROM archive."vPlayListResolvedFull" WHERE "dtStartSoft" IS NULL AND "dtStartHard" IS NULL AND "dtStart" >= dt ORDER BY "dtStart", id LIMIT 1;
				ELSE
					SELECT DISTINCT id, "dtStart" INTO stRecord FROM archive."vPlayListResolvedFull" WHERE ("dtStartSoft" IS NOT NULL OR "dtStartHard" IS NOT NULL) AND "dtStart" >= dt ORDER BY "dtStart", id LIMIT 1;
				END IF;
			ELSE
				IF bSequential IS NULL THEN
					SELECT DISTINCT id, "dtStart" INTO stRecord FROM archive."vPlayListResolvedFull" WHERE "dtStart" > dt - '1 day'::interval AND "dtStart" < dt ORDER BY "dtStart" DESC, id DESC LIMIT 1;
				ELSIF bSequential THEN
					SELECT DISTINCT id, "dtStart" INTO stRecord FROM archive."vPlayListResolvedFull" WHERE "dtStartSoft" IS NULL AND "dtStartHard" IS NULL AND "dtStart" > dt - '1 day'::interval AND "dtStart" < dt ORDER BY "dtStart" DESC, id DESC LIMIT 1;
				ELSE
					SELECT DISTINCT id, "dtStart" INTO stRecord FROM archive."vPlayListResolvedFull" WHERE ("dtStartSoft" IS NOT NULL OR "dtStartHard" IS NOT NULL) AND "dtStart" > dt - '1 day'::interval AND "dtStart" < dt ORDER BY "dtStart" DESC, id DESC LIMIT 1;
				END IF;
			END IF;
			stRetVal."nValue" := stRecord.id;
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemPreviousGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt, NULL, false);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemNextGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt + interval '1 millisecond', NULL, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemPreviousGet"(idItems bigint) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"((SELECT "dtStart" FROM pl."vItemTimings" WHERE idItems=id), NULL, false);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemNextGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt + interval '1 millisecond', NULL, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemSequentialPreviousGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt, true, false);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemSequentialNextGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt, true, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemTimedPreviousGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt, false, false);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemTimedNextGet"(dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemByDTGet"(dt, false, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemTimingsSet"(idItems integer, nFrameStart integer, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemTimingsSet"(idItems, nFrameStart, NULL, nFramesQty);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemTimingsSet"(idItems integer, nFrameStart integer, nFrameStop integer, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			_nFrameStart integer;
			_nFrameStop integer;
			_nFramesQty integer;
		BEGIN
			stRetVal := pl."fItemGet"(idItems);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'NO SUCH ID:%',id;
			END IF;
			_nFrameStart := nFrameStart;
			_nFrameStop := nFrameStop;
			_nFramesQty := nFramesQty;
			---------------------------------------------------------
			-- Проверка параметра nFramesQty
			---------------------------------------------------------
			IF _nFramesQty IS NULL THEN
				stRetVal := pl."fItemAttributeValueGet"(idItems, NULL,  'nFramesQty');
				IF stRetVal."bValue" THEN
					_nFramesQty := stRetVal."nValue";
				ELSE
					RAISE EXCEPTION 'UNKNOWN FRAMES QTY';
				END IF;
			ELSEIF 1 > _nFramesQty THEN
					RAISE EXCEPTION 'WRONG FRAMES QTY:%', _nFramesQty;
			END IF;
			---------------------------------------------------------
			-- Проверка параметра nFrameStart
			---------------------------------------------------------
			IF _nFrameStart IS NULL THEN
				stRetVal := pl."fItemAttributeValueGet"(idItems, NULL,  'nFrameStart');
				IF stRetVal."bValue" THEN
					_nFrameStart := stRetVal."nValue";
				ELSE
					_nFrameStart := 1;
				END IF;
			ELSEIF 0 > _nFrameStart THEN
				RAISE EXCEPTION 'WRONG START:%',_nFrameStart;
			ELSEIF _nFramesQty <= _nFrameStart THEN
				RAISE EXCEPTION 'START MUST BE LESS THEN FRAMES QTY. START:% FRAMES QTY:%',_nFrameStart,_nFramesQty;
			END IF;
			---------------------------------------------------------
			-- Проверка параметра nFrameStop
			---------------------------------------------------------
			IF _nFrameStop IS NULL THEN
				stRetVal := pl."fItemAttributeValueGet"(idItems, NULL,  'nFrameStop');
				IF stRetVal."bValue" THEN
					_nFrameStop := stRetVal."nValue";
				ELSE
					_nFrameStop := 0;
				END IF;
			ELSEIF 1 > _nFrameStop THEN
				RAISE EXCEPTION 'WRONG STOP:%',_nFrameStop;
			ELSEIF _nFrameStart >= _nFrameStop THEN
				RAISE EXCEPTION 'START MUST BE LESS THEN STOP. START:% STOP:%',_nFrameStart,_nFrameStop;
			ELSEIF _nFramesQty < _nFrameStop THEN
				RAISE EXCEPTION 'STOP MUST BE LESS OR EQUAL THEN FRAMES QTY. STOP:% FRAMES QTY:%',_nFrameStop,_nFramesQty;
			END IF;
			---------------------------------------------------------
			-- Обработка
			---------------------------------------------------------
			stRetVal := pl."fItemAttributeSet"(idItems, 'nFrameStart', _nFrameStart);
			---------------------------------------------------------
			IF 0 < _nFrameStop THEN
				stRetVal := pl."fItemAttributeSet"(idItems, 'nFrameStop', _nFrameStop);
			END IF;
			---------------------------------------------------------
			stRetVal := pl."fItemAttributeSet"(idItems, 'nFramesQty', _nFramesQty);
			---------------------------------------------------------
			stRetVal."nValue" := idItems;
			
			---------------------------------------------------------
			-- Удаление nFrameStopInitial, если есть (он образуется при пересчете ПЛ при подрезке клипа)
			---------------------------------------------------------
			DELETE FROM pl."tItemAttributes" WHERE "idItems" = iditems AND "sKey" = 'nFrameStopInitial';
			
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
		/*
	CREATE OR REPLACE FUNCTION pl."fItemsTimingsUpdate"() RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;

			cLockedItems refcursor;
			cSequentialItems refcursor;
			cTimedItems refcursor;
			
			stCurrentSequentialItem RECORD;
			stCurrentTimedItem RECORD;
			stPreviousItem RECORD;

			nDiff integer;
			nDuration integer;
			nCount integer;

			tsDeviation interval;

			bForceAdd boolean;
			bPrevious boolean;

			dtTimedPreviousStop timestamp with time zone;
			nPlannedStartTypeID integer;
			
		BEGIN
			stRetVal := pl."fItemDTEventTypeGet"('start_planned');
			nPlannedStartTypeID := stRetVal."nValue";
			bForceAdd := false;
			bPrevious := false;
			stRetVal."bValue":=false;
			stRetVal."nValue":=-1;
			tsDeviation := '2 seconds'::interval;
			---------------------------------------------------------
			--SELECT "dtStartPlanned" INTO dtTimedPreviousStop FROM pl."vPlayListResolved" WHERE "dtStopReal" IS NOT NULL ORDER BY "dtStopReal" DESC LIMIT 1;
			SELECT max("dtStopPlanned") INTO dtTimedPreviousStop FROM pl."vPlayListResolved" WHERE "dtStopReal" IS NOT NULL;
			-- есть надежда, что если dtTimedPreviousStop is null, тогда мы выдернем все
			OPEN cLockedItems SCROLL FOR SELECT DISTINCT id, "dtStartPlanned", "dtStopPlanned", "nFrameStop"-"nFrameStart"+1 as "nFramesQty", "dtStartReal", "dtStopReal", COALESCE("dtStartHard", "dtStartSoft") as dt, CASE WHEN "dtStartHard" IS NOT NULL THEN true ELSE false END as "bHard", "bPlug", "idStatuses",true as "bLocked", "dtStart" FROM pl."vPlayListResolved" WHERE "dtStop" >= dtTimedPreviousStop AND ('prepared' = "sStatusName" OR 'onair' = "sStatusName" OR 'queued' = "sStatusName") ORDER BY "dtStart", id;
			---------------------------------------------------------
			FETCH cLockedItems INTO stPreviousItem;
			IF FOUND THEN
				bPrevious := true;
		--RAISE NOTICE 'ФЕТЧ ЛОКЕД ИН ПРЕВИОС [c:-2][p:%]',stPreviousItem;
				IF stPreviousItem."dtStartReal" IS NOT NULL THEN
					stPreviousItem."dtStopPlanned" := stPreviousItem."dtStartReal" + ((stPreviousItem."nFramesQty" * 40)::text || ' milliseconds')::interval;
		----RAISE NOTICE 'ПРЕВИОС СТАРТ РИАЛ [c:-2][p:%]',stPreviousItem;
				END IF;
				WHILE true LOOP
					FETCH cLockedItems INTO stCurrentSequentialItem;
					EXIT WHEN NOT FOUND;
					
					IF stCurrentSequentialItem."dtStartReal" IS NOT NULL THEN
						stPreviousItem."dtStopPlanned" := stCurrentSequentialItem."dtStartReal";
		--RAISE NOTICE 'ЛОКЕД СТАРТ РИАЛ [c:-2][p:%][l:%]',stPreviousItem,stCurrentSequentialItem;
					END IF;

					IF stCurrentSequentialItem."bHard" THEN
						stCurrentSequentialItem."dtStartPlanned" := stCurrentSequentialItem.dt;
					ELSE
						stCurrentSequentialItem."dtStartPlanned" := stPreviousItem."dtStopPlanned";
					END IF;
					
					stCurrentSequentialItem."dtStopPlanned" := stCurrentSequentialItem."dtStartPlanned" + ((stCurrentSequentialItem."nFramesQty" * 40)::text || ' milliseconds')::interval;

		--RAISE NOTICE 'ОБНОВЛЯЕМ ЛОКЕД ТАЙМЫ [c:-2][p:%][s:%]',stPreviousItem,stCurrentSequentialItem;
		-- мы обновляем "dtStartPlanned" и "dtStopPlanned" у залоченных item'ов для того, чтобы плейлист не стал похож на зебру... иначе при корректировки только planned, запланированные могут сползти наверх и попасть между залочеными, а это некрасиво...
					UPDATE pl."tItemDTEvents" SET dt=stCurrentSequentialItem."dtStartPlanned" WHERE stCurrentSequentialItem.id="idItems" AND nPlannedStartTypeID="idItemDTEventTypes";
					--UPDATE pl."tItemDTEvents" SET dt=stCurrentSequentialItem."dtStopPlanned" WHERE stCurrentSequentialItem.id="idItems" AND nPlannedStopTypeID="idItemDTEventTypes";
					stPreviousItem := stCurrentSequentialItem;
				END LOOP;
			END IF;
			---------------------------------------------------------
			OPEN cSequentialItems SCROLL FOR SELECT DISTINCT id, "nFrameStop"-"nFrameStart"+1 as "nFramesQty", "dtStartPlanned", "dtStopPlanned", "bPlug", "dtStart" as dt, NULL as "bHard", "dtStartReal", "dtStopReal","bLocked", "dtStart" FROM pl."vPlayListResolved" WHERE ('planned' = "sStatusName") AND "dtStartHard" IS NULL AND "dtStartSoft" IS NULL ORDER BY dt, id;
			-- я убрал из условия AND "dtStopPlanned" > now() - надеюсь т.к. теперь мы юзаем статус skipped, нам эта часть условия без надобности
			--dtTimedPreviousStop := now();
			--IF stPreviousItem.dt IS NOT NULL THEN
			--	dtTimedPreviousStop := stPreviousItem.dt;
			--END IF;
			OPEN cTimedItems SCROLL FOR SELECT DISTINCT id, "nFrameStop"-"nFrameStart"+1 as "nFramesQty", "dtStartPlanned", "dtStopPlanned", "bPlug", COALESCE("dtStartReal", "dtStartHard", "dtStartSoft") as dt, CASE WHEN "dtStartSoft" IS NULL THEN true ELSE false END as "bHard", "dtStartHard", "dtStartSoft", "bLocked", "dtStartReal", "dtStopReal", "dtStart" FROM pl."vPlayListResolved" WHERE ('planned' = "sStatusName") ORDER BY dt, id;
			-- я убрал из условия AND ("dtStartHard" > dtTimedPreviousStop OR "dtStartSoft" > dtTimedPreviousStop) - надеюсь т.к. теперь мы юзаем статус skipped, нам эта часть условия без надобности
			---------------------------------------------------------
			FETCH cSequentialItems INTO stCurrentSequentialItem;
			IF NOT FOUND THEN
				RETURN stRetVal;
			END IF;
		--RAISE NOTICE 'ФЕТЧ СЕКВЕНТ [c:-1][s:%]',stCurrentSequentialItem;
			FETCH cTimedItems INTO stCurrentTimedItem;
		--RAISE NOTICE 'ФЕТЧ ТАЙМД [c:-1][t:%]',stCurrentTimedItem;

			IF NOT bPrevious THEN
				IF stCurrentSequentialItem."dtStartPlanned" > stCurrentTimedItem.dt THEN
					stPreviousItem := stCurrentTimedItem;
					FETCH cTimedItems INTO stCurrentTimedItem;
		--RAISE NOTICE 'ПРЕВИОС ТАЙМД [c:-1][p:%][t:%]',stPreviousItem,stCurrentTimedItem;
				ELSE
					stPreviousItem := stCurrentSequentialItem;
					FETCH cSequentialItems INTO stCurrentSequentialItem;
		--RAISE NOTICE 'ПРЕВИОС СЕКВЕНТ [c:-1][p:%][s:%]',stPreviousItem,stCurrentSequentialItem;
				END IF;
			END IF;
			nCount := 1;
			---------------------------------------------------------
			WHILE true LOOP
				IF stCurrentTimedItem.dt IS NOT NULL THEN
					nDiff := "fFrames"(stCurrentTimedItem.dt + tsDeviation - stPreviousItem."dtStopPlanned");
				ELSE
					nDiff := 1;
					bForceAdd := true;
				END IF;
				nDuration := stCurrentSequentialItem."nFramesQty";

				IF 0 >= nDiff THEN
					IF stCurrentTimedItem."bHard" THEN
						--ставим хард
						stCurrentTimedItem."dtStartPlanned" := stCurrentTimedItem.dt + tsDeviation;
		--				dtTimedPreviousStop := stCurrentTimedItem."dtStartPlanned" + (floor(stCurrentTimedItem."nFramesQty"/25)::text || ' seconds')::interval;
		--				stCurrentTimedItem."dtStopPlanned" := dtTimedPreviousStop;
						stCurrentTimedItem."dtStopPlanned" := stCurrentTimedItem."dtStartPlanned" + ((stCurrentTimedItem."nFramesQty" * 40)::text || ' milliseconds')::interval;
						stPreviousItem := stCurrentTimedItem;
						FETCH cTimedItems INTO stCurrentTimedItem;
		--RAISE NOTICE 'СТАВИМ ХАРД [c:%][p:%][s:%][t:%]',nCount,stPreviousItem,stCurrentSequentialItem,stCurrentTimedItem;		
					ELSE
						-- ставим софт
						stCurrentTimedItem."dtStartPlanned" := stPreviousItem."dtStopPlanned";
						stCurrentTimedItem."dtStopPlanned" := stCurrentTimedItem."dtStartPlanned" + ((stCurrentTimedItem."nFramesQty" * 40)::text || ' milliseconds')::interval;
		--				IF stCurrentTimedItem.dt > dtTimedPreviousStop THEN
		--					dtTimedPreviousStop := stCurrentTimedItem."dtStopPlanned";
		--				END IF;
						stPreviousItem := stCurrentTimedItem;
						FETCH cTimedItems INTO stCurrentTimedItem;
		--RAISE NOTICE 'СТАВИМ СОФТ [c:%][p:%][s:%][t:%]',nCount,stPreviousItem,stCurrentSequentialItem,stCurrentTimedItem;		
					END IF;
					nCount := nCount + 1;
				ELSIF bForceAdd OR NOT stCurrentTimedItem."bHard" OR ( 125 < nDiff - nDuration ) OR ( 3750 < nDiff AND nDuration > nDiff ) THEN
					-- ставим секвент
					stCurrentSequentialItem."dtStartPlanned" := stPreviousItem."dtStopPlanned";
					stCurrentSequentialItem."dtStopPlanned" := stCurrentSequentialItem."dtStartPlanned" + ((nDuration * 40)::text || ' milliseconds')::interval;
					stPreviousItem := stCurrentSequentialItem;
					WHILE true LOOP
						FETCH cSequentialItems INTO stCurrentSequentialItem;
						EXIT WHEN NOT FOUND OR NOT stCurrentSequentialItem."bPlug";
						stRetVal := pl."fItemRemove"(stCurrentSequentialItem.id);
		--RAISE NOTICE 'ПЛАГ РЕМУВ [c:%][s:%]',nCount,stCurrentSequentialItem;
					END LOOP;
		--RAISE NOTICE 'СТАВИМ СЕКВЕНТ [c:%][p:%][s:%][t:%]',nCount,stPreviousItem,stCurrentSequentialItem,stCurrentTimedItem;
					nCount := nCount + 1;
					EXIT WHEN NOT FOUND;
				ELSIF (stCurrentTimedItem."bHard") AND (3750 >= nDiff OR 125 >= (nDiff - nDuration)) AND NOT stPreviousItem."bPlug" THEN 
					-- ставим плаг
					IF NOT stPreviousItem."bLocked" AND NOT stCurrentSequentialItem."bPlug" THEN
						stRetVal := pl."fPlaylistPlugAdd"(stPreviousItem."dtStopPlanned", nDiff);
						stPreviousItem.id := stRetVal."nValue";
						stPreviousItem."dtStopPlanned" := stPreviousItem."dtStopPlanned" + ((nDiff * 40)::text || ' milliseconds')::interval;
		--RAISE NOTICE 'СТАВИМ ПЛАГ [c:%][p:%][s:%][t:%]',nCount,stPreviousItem,stCurrentSequentialItem,stCurrentTimedItem;
					ELSE
						bForceAdd := true;
		--RAISE NOTICE 'ФОРСИМ [c:%][p:%][s:%][t:%][d:%]',nCount,stPreviousItem,stCurrentSequentialItem,stCurrentTimedItem,nDiff;		
					END IF;
					CONTINUE;
				ELSE
		--RAISE NOTICE 'ВЫХОД [с:%][p:%][s:%][t:%]',nCount,stPreviousItem,stCurrentSequentialItem,stCurrentTimedItem;
					EXIT;
				END IF;

				bForceAdd := false;
				UPDATE pl."tItemDTEvents" SET dt=stPreviousItem."dtStartPlanned" WHERE stPreviousItem.id="idItems" AND nPlannedStartTypeID="idItemDTEventTypes";
				--UPDATE pl."tItemDTEvents" SET dt=stPreviousItem."dtStopPlanned" WHERE stPreviousItem.id="idItems" AND nPlannedStopTypeID="idItemDTEventTypes";
		--RAISE NOTICE 'ОБНОВЛЯЕМ ТАЙМЫ [c:%][p:%]',nCount,stPreviousItem;
			END LOOP;
			---------------------------------------------------------
			stRetVal."nValue" := nCount;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
		*/
	CREATE OR REPLACE FUNCTION pl."fItemAddAsAsset"(idAssets integer, idClasses integer, dtStart timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
			idFiles integer;
			sName varchar(255);
			idItems integer;
			nFramesQty integer;
			nValue integer;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"('media','tFiles');
			idRegisteredTables := stRetVal."nValue";
			SELECT "nValue" INTO idFiles FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables;
			---------------------------------------------------------
			SELECT "sName" INTO sName FROM mam."tAssets" WHERE "id"=idAssets;
			---------------------------------------------------------
			SELECT "nValue" INTO nFramesQty FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFramesQty'="sKey";
			IF nFramesQty IS NULL THEN
				nFramesQty := 0;
			END IF;
			stRetVal := pl."fItemPlannedAdd"(sName, idClasses, idFiles, dtStart, nFramesQty);
			idItems := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tAssets');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'asset', idAssets);
			---------------------------------------------------------
			SELECT "nValue" INTO nValue FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFrameIn'="sKey";
			IF nValue IS NOT NULL AND 1 < nValue THEN
				stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'nFrameStart', nValue);
			END IF;
			SELECT "nValue" INTO nValue FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFrameOut'="sKey";
			IF nValue IS NOT NULL AND (1 > nFramesQty OR nFramesQty > nValue) THEN
				stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'nFrameStop', nValue);
			END IF;
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;

			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAddAsAsset"(idAssets integer, sClassName varchar, dtStart timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idClasses integer;
		BEGIN
			stRetVal := pl."fClassGet"(sClassName);
			idClasses := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemAddAsAsset"(idAssets, idClasses, dtStart);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAddAsAsset"(idAssets integer, sClassName varchar) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idClasses integer;
		BEGIN
			stRetVal := pl."fClassGet"(sClassName);
			idClasses := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemAddAsAsset"(idAssets, idClasses);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAddAsAsset"(idAssets integer, idClasses integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			dtStart timestamp with time zone;
		BEGIN
			SELECT max("dtStop") INTO dtStart FROM pl."vPlayListResolved" WHERE now() <= "dtStop" AND "dtStartHard" IS NULL AND "dtStartSoft" IS NULL;
			IF dtStart IS NULL THEN
				dtStart := now();
			END IF;
			---------------------------------------------------------
			stRetVal := pl."fItemAddAsAsset"(idAssets, idClasses, dtStart);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAddAsAsset"(idAssets integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idClasses integer;
		BEGIN
			SELECT "idClasses" INTO idClasses FROM mam."vAssetsClasses" WHERE idAssets=id;
			---------------------------------------------------------
			stRetVal := pl."fItemAddAsAsset"(idAssets, idClasses);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAddAsFile"(idFiles integer, nFramesQty integer, sClassName varchar, dtStart timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idClasses integer;
			idRegisteredTables integer;
			sName varchar(255);
			idItems integer;
			nLength integer;
		BEGIN
			stRetVal := pl."fClassGet"(sClassName);
			idClasses := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('media','tFiles');
			idRegisteredTables := stRetVal."nValue";
			---------------------------------------------------------
			SELECT "sFilename" INTO sName FROM media."tFiles" WHERE id=idFiles;
			---------------------------------------------------------
			IF nFramesQty IS NULL THEN
				nLength := 0;
			ELSE
				nLength := nFramesQty;
			END IF;
			stRetVal := pl."fItemPlannedAdd"(sName, idClasses, idFiles, dtStart, nLength);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAddAsFile"(idFiles integer, nFramesQty integer, sClassName varchar) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			dtStart timestamp with time zone;
		BEGIN
			SELECT max("dtStop") INTO dtStart FROM pl."vItemTimings" WHERE now() <= "dtStop" AND "dtStartHard" IS NULL AND "dtStartSoft" IS NULL;
			IF dtStart IS NULL THEN
				dtStart := now();
			END IF;
			---------------------------------------------------------
			stRetVal := pl."fItemAddAsFile"(idFiles, nFramesQty, sClassName, dtStart);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemFileSet"(idItems integer, idFiles integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemGet"(idItems);
			IF stRetVal."bValue" THEN
				UPDATE pl."tItems" SET "idFiles"=idFiles WHERE id=idItems;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemNameSet"(idItems integer, sName varchar) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemGet"(idItems);
			IF stRetVal."bValue" THEN
				UPDATE pl."tItems" SET "sName"=sName WHERE id=idItems;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemInsert"(idAssets integer, nPrecedingItemID integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idRegisteredTables integer;
			idItems integer;
			idClasses integer;
			idFiles integer;
			sName text;
			nFramesQty integer;
			nPrecedingOffset integer;
		BEGIN
			SELECT "idClasses" INTO idClasses FROM mam."vAssetsClasses" WHERE idAssets=id;
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('media','tFiles');
			idRegisteredTables := stRetVal."nValue";
			SELECT "nValue" INTO idFiles FROM mam."tAssetAttributes" WHERE "idAssets"=idAssets AND "idRegisteredTables"=idRegisteredTables;
			---------------------------------------------------------
			SELECT "sName" INTO sName FROM mam."tAssets" WHERE "id"=idAssets;
			---------------------------------------------------------
			SELECT "nValue" INTO nFramesQty FROM mam."tAssetAttributes" WHERE idAssets="idAssets" AND 'nFramesQty'="sKey";
			IF nFramesQty IS NULL THEN
				nFramesQty := 0;
			END IF;
			---------------------------------------------------------
			stRetVal := pl."fItemAdd"(sName, idClasses, idFiles);
			idItems := stRetVal."nValue";
			---------------------------------------------------------
			stRetVal := pl."fItemAttributeSet"(idItems, 'nFramesQty', nFramesQty);
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('mam','tAssets');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'asset', idAssets);
			---------------------------------------------------------
			stRetVal := hk."fRegisteredTableGet"('pl','tItems');
			idRegisteredTables := stRetVal."nValue";
			stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, 'preceding_item', nPrecedingItemID);
			---------------------------------------------------------
			SELECT max("nPrecedingOffset") + 1 INTO nPrecedingOffset FROM pl."vItemsInserted" WHERE nPrecedingItemID = "nPrecedingID";
			IF nPrecedingOffset IS NULL THEN
				nPrecedingOffset := 0;
			END IF;
			stRetVal := pl."fItemStartPlannedSet"(idItems, pl."fItemStartPlannedGet"(nPrecedingItemID) + ((1 + nPrecedingOffset)||' second')::interval);
			stRetVal := pl."fItemAttributeAdd"(idItems, 'preceding_offset', nPrecedingOffset);
			---------------------------------------------------------
			stRetVal."nValue":=idItems;
			stRetVal."bValue":=true;
			---------------------------------------------------------
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- pl."tItemAttributes"
	CREATE OR REPLACE FUNCTION pl."fItemAttributeGet"(idItems integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemAttributes';
			stRetVal := "fAttributesTableGet"(stTable, 'idItems', idItems, idRegisteredTables, sKey, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeGet"(idItems integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeGet"(idItems integer, idRegisteredTables integer,  sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeGet"(idItems integer,  sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{sKey,0}}';
			aColumns[2][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeAdd"(idItems integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeAdd"(idItems integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, NULL, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeAdd"(idItems integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemAttributeAdd"(idItems, NULL, sKey, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeSet"(idItems integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemAttributeGet"(idItems, sKey);
			IF stRetVal."bValue" THEN
				UPDATE pl."tItemAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
			ELSE
				stRetVal := pl."fItemAttributeAdd"(idItems, sKey, nValue);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeSet"(idItems integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemAttributeGet"(idItems, idRegisteredTables, sKey);
			IF stRetVal."bValue" THEN
				UPDATE pl."tItemAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
			ELSE
				stRetVal := pl."fItemAttributeAdd"(idItems, idRegisteredTables, sKey, nValue);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemAttributeValueGet"(idItems integer, idRegisteredTables integer,  sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			SELECT "nValue" INTO stRetVal."nValue" FROM pl."tItemAttributes" WHERE id=stRetVal."nValue";
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- pl."tItemDTEventTypes"
	CREATE OR REPLACE FUNCTION pl."fItemDTEventTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemDTEventTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemDTEventTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemDTEventTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- pl."tItemDTEvents"
	CREATE OR REPLACE FUNCTION pl."fItemDTEventGet"(idItemDTEventTypes integer, idItems integer, dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemDTEvents';
			aColumns := '{{idItemDTEventTypes,'||COALESCE(idItemDTEventTypes::text,'NULL')||'},{idItems,'||COALESCE(idItems::text,'NULL')||'},{dt,0}}';
			aColumns[3][2] := quote_literal(dt); --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemDTEventGet"(idItemDTEventTypes integer, idItems integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemDTEvents';
			aColumns := '{{idItemDTEventTypes,'||COALESCE(idItemDTEventTypes::text,'NULL')||'},{idItems,'||COALESCE(idItems::text,'NULL')||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemDTEventAdd"(idItemDTEventTypes integer, idItems integer, dt timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tItemDTEvents';
			aColumns := '{{idItemDTEventTypes,'||COALESCE(idItemDTEventTypes::text,'NULL')||'},{idItems,'||COALESCE(idItems::text,'NULL')||'},{dt,0}}';
			aColumns[3][2] := quote_literal(dt); --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemDTEventAdd"(idItemDTEventTypes integer, idItems integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemDTEventAdd"(idItemDTEventTypes, idItems, now());
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemDTEventSet"(idItemDTEventTypes integer, idItems integer, dtEvent timestamp with time zone) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemDTEventGet"(idItemDTEventTypes, idItems);
			IF stRetVal."bValue" THEN
				UPDATE pl."tItemDTEvents" SET dt=dtEvent WHERE "idItems" = idItems AND "idItemDTEventTypes" = idItemDTEventTypes;
				stRetVal."bValue" := true;
				stRetVal."nValue" := idItems;
			ELSE
				stRetVal := pl."fItemDTEventAdd"(idItemDTEventTypes, idItems, dtEvent);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fItemDTEventSet"(idItemDTEventTypes integer, idItems integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := pl."fItemDTEventSet"(idItemDTEventTypes, idItems, now());
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- pl."tPlugs"
	CREATE OR REPLACE FUNCTION pl."fPlugGet"(idFiles integer, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tPlugs';
			aColumns := '{{idFiles,'||idFiles||'},{nFramesQty,'||nFramesQty||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fPlugAdd"(idFiles integer, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tPlugs';
			aColumns := '{{idFiles,'||idFiles||'},{nFramesQty,'||nFramesQty||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fPlugAdd"(idStorages integer, sFilename text, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := media."fFileAdd"(idStorages, sFilename);
			stRetVal := pl."fPlugAdd"(stRetVal."nValue", nFramesQty);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fPlaylistPlugAdd"(dtStart timestamp with time zone, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			stPlug RECORD;
			nFrameOffset integer;
			idItems integer;
		BEGIN
			SELECT p."idFiles", po."idPlugs", po."nFrameOffset", p."nFramesQty" INTO stPlug FROM pl."tPlugOffsets" po, pl."tPlugs" p WHERE nFramesQty <= p."nFramesQty"-po."nFrameOffset"+1 ORDER BY po."dtLastUsed" LIMIT 1;
			stRetVal := pl."fItemAddAsFile"(stPlug."idFiles", stPlug."nFramesQty", 'design_common', dtStart);
			idItems := stRetVal."nValue";
			stRetVal := pl."fPlaylistPlugUpdate"(idItems, dtStart, nFramesQty);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fPlaylistPlugUpdate"(idItems integer, dtStart timestamp with time zone, nFramesQty integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			stPlug RECORD;
			nFrameOffset integer;
		BEGIN
			SELECT p."idFiles", po."idPlugs", po."nFrameOffset", p."nFramesQty" INTO stPlug FROM pl."tPlugOffsets" po, pl."tPlugs" p, pl."vPlayListResolved" i  WHERE idItems = i.id AND p."idFiles" = i."idFiles" AND nFramesQty <= p."nFramesQty"-po."nFrameOffset"+1 ORDER BY po."dtLastUsed" LIMIT 1;
			stRetVal := pl."fItemTimingsSet"(idItems, stPlug."nFrameOffset", stPlug."nFrameOffset" + nFramesQty - 1, stPlug."nFramesQty");
			stRetVal := pl."fItemStartPlannedSet"(idItems, dtStart);
			SELECT "nFrameOffset" INTO nFrameOffset FROM pl."tPlugOffsets" WHERE stPlug."idPlugs"="idPlugs" AND stPlug."nFrameOffset"+nFramesQty-1 < "nFrameOffset" ORDER BY "nFrameOffset" LIMIT 1;
			UPDATE pl."tPlugOffsets" SET "dtLastUsed" = now() WHERE stPlug."idPlugs"="idPlugs" AND stPlug."nFrameOffset" <= "nFrameOffset" AND nFrameOffset > "nFrameOffset";
			UPDATE pl."tPlugOffsets" SET "dtLastUsed" = now() WHERE stPlug."idPlugs"="idPlugs" AND stPlug."nFrameOffset" <= "nFrameOffset" AND COALESCE(nFrameOffset, stPlug."nFramesQty") > "nFrameOffset";
			stRetVal."nValue" := idItems;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

----------------------------------- pl."tPlugOffsets"
	CREATE OR REPLACE FUNCTION pl."fPlugOffsetGet"(idPlugs integer, nFrameOffset integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tPlugOffsets';
			aColumns := '{{idPlugs,'||idPlugs||'},{nFrameOffset,'||nFrameOffset||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fPlugOffsetAdd"(idPlugs integer, nFrameOffset integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tPlugOffsets';
			aColumns := '{{idPlugs,'||idPlugs||'},{nFrameOffset,'||nFrameOffset||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- pl."tProxies"
	CREATE OR REPLACE FUNCTION pl."fProxyGet"(idClasses integer, sName character varying, sFile text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tProxies';
			aColumns := '{{idClasses,'||idClasses||'},{sName,0}{sFile,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			aColumns[3][2] := sFile; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION pl."fProxyAdd"(idClasses integer, sName character varying, sFile text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='pl';
			stTable."sName":='tProxies';
			aColumns := '{{idClasses,'||idClasses||'},{sName,0}{sFile,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			aColumns[3][2] := sFile; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;