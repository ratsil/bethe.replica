CREATE OR REPLACE VIEW pl."vPlayList" AS
	SELECT itm.*, tbl."sName" as "sTableName", atr."sKey", atr."nValue"
		FROM 
			pl."tItems" itm
			LEFT JOIN pl."tItemAttributes" atr ON itm."id"=atr."idItems"
			LEFT JOIN "hk"."tRegisteredTables" tbl ON atr."idRegisteredTables"=tbl."id";

CREATE OR REPLACE VIEW pl."vItemAttributes" AS
	SELECT ia.*, t."sName" as "sTableName"
		FROM 
			pl."tItemAttributes" ia
			LEFT JOIN "hk"."tRegisteredTables" t ON ia."idRegisteredTables"=t."id";

CREATE OR REPLACE VIEW pl."vClasses" AS
	SELECT cls.*, prnt."sName" as "sParentName"
		FROM 
			pl."tClasses" cls
			LEFT JOIN pl."tClasses" prnt ON cls."idParent"=prnt.id;

CREATE OR REPLACE VIEW pl."vItemDTEvents" AS
	SELECT dts.id, dts_tp.id as "idItemsDTEventTypes", dts_tp."sName" as "sTypeName", "idItems", dt
		FROM pl."tItemDTEventTypes" dts_tp, pl."tItemDTEvents" dts
		WHERE dts_tp.id=dts."idItemDTEventTypes";

CREATE OR REPLACE VIEW pl."vItemFrames" AS
	SELECT itm.id, 
			COALESCE(ia_fstart."nValue", 1) as "nFrameStart", 
			COALESCE(ia_fstop."nValue", ia_fq."nValue") as "nFrameStop", 
			ia_fq."nValue" as "nFramesQty"
		FROM
			pl."tItems" itm 
			LEFT JOIN pl."tItemAttributes" ia_fstart ON ia_fstart."idItems"=itm.id AND 'nFrameStart'=ia_fstart."sKey"
			LEFT JOIN pl."tItemAttributes" ia_fstop ON ia_fstop."idItems"=itm.id AND 'nFrameStop'=ia_fstop."sKey"
			LEFT JOIN pl."tItemAttributes" ia_fq ON ia_fq."idItems"=itm.id AND 'nFramesQty'=ia_fq."sKey";

CREATE OR REPLACE VIEW pl."vItemTimings" AS
	SELECT itm.id, 
			"nFrameStart", 
			"nFrameStop", 
			"nFramesQty",
			dts_pstart.dt as "dtStartPlanned",
			dts_hstart.dt as "dtStartHard", dts_sstart.dt as "dtStartSoft", dts_tupdate.dt AS "dtTimingsUpdate",
			dts_qstart.dt AS "dtStartQueued", dts_rstart.dt as "dtStartReal", dts_rstop.dt as "dtStopReal",
			COALESCE(dts_rstart.dt, dts_qstart.dt, dts_pstart.dt) as "dtStart", 
			dts_rstop.dt as "dtStop",
			(("nFrameStop" - "nFrameStart" + 1) * 40) as "nLength"
		FROM
			pl."tItems" itm 
			LEFT JOIN pl."vItemFrames" f ON f.id=itm.id
			LEFT JOIN pl."vItemDTEvents" dts_pstart ON itm.id=dts_pstart."idItems" AND 'start_planned'=dts_pstart."sTypeName"
			LEFT JOIN pl."vItemDTEvents" dts_hstart ON itm.id=dts_hstart."idItems" AND 'start_hard'=dts_hstart."sTypeName"
			LEFT JOIN pl."vItemDTEvents" dts_sstart ON itm.id=dts_sstart."idItems" AND 'start_soft'=dts_sstart."sTypeName"
			LEFT JOIN pl."vItemDTEvents" dts_rstart ON itm.id=dts_rstart."idItems" AND 'start_real'=dts_rstart."sTypeName"
			LEFT JOIN pl."vItemDTEvents" dts_rstop ON itm.id=dts_rstop."idItems" AND 'stop_real'=dts_rstop."sTypeName"
			LEFT JOIN pl."vItemDTEvents" dts_tupdate ON itm.id=dts_tupdate."idItems" AND 'timings_update' = dts_tupdate."sTypeName"
			LEFT JOIN pl."vItemDTEvents" dts_qstart ON itm.id=dts_qstart."idItems" AND 'start_queued' = dts_qstart."sTypeName";

CREATE OR REPLACE VIEW pl."vTimedItemCurrent" AS 
 	SELECT id, "dtStart", ("dtStart" + ("nLength" || ' milliseconds')::interval) as "dtStop", "fFrames"(now() - "dtStart") + "nFrameStart" AS "nFrameCurrent"
		FROM pl."vItemTimings"
	WHERE now() >= "dtStart" AND now() < ("dtStart" + (("nLength" || ' milliseconds')::interval)) AND id NOT IN (SELECT id FROM pl."vItemTimings" WHERE "dtStopReal" IS NOT NULL)
	ORDER BY "dtStartReal" DESC NULLS LAST, "dtStartQueued" DESC NULLS LAST, "dtStartPlanned"
	LIMIT 1;

CREATE OR REPLACE VIEW pl."vTimedItemLast" AS
	SELECT *
		FROM pl."vItemTimings"
		ORDER BY "dtStop" DESC NULLS LAST, "dtStart" DESC NULLS LAST, id
		LIMIT 1;

CREATE OR REPLACE VIEW pl."vItemsLeft" AS 
	SELECT count(*) AS "nQty"
		FROM pl."vItemTimings"
		WHERE now() < "dtStart" AND id NOT IN (SELECT id FROM pl."vItemTimings" WHERE "dtStopReal" IS NOT NULL);

CREATE OR REPLACE VIEW pl."vItemsInserted" AS 
	SELECT pi."idItems" as id, pi."nValue" as "nPrecedingID", po."nValue" as "nPrecedingOffset"
		FROM pl."tItemAttributes" pi, pl."tItemAttributes" po
		WHERE pi."idItems"=po."idItems" AND 'preceding_item'=pi."sKey" AND 'preceding_offset'=po."sKey";
