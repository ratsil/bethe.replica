
CREATE OR REPLACE VIEW pl."vPlayListResolved" AS 
	SELECT itm.id,
			itm."idHK",
			itm."idStatuses",
			itm."idClasses",
			itm."idFiles",
			itm."sName",
			itm."sNote",
			sts."sName" AS "sStatusName",
			CASE
				WHEN 'planned'::text = sts."sName"::text THEN false
				ELSE true
			END AS "bLocked",
			c."sName" AS "sClassName",
			f."idStorages",
			f."idStorageTypes",
			f."sStorageTypeName",
			f."sPath",
			f."sStorageName",
			f."bStorageEnabled",
			f."sFilename",
			f."dtLastFileEvent",
			f."eError" AS "eFileError",
			t."nFrameStart",
			t."nFrameStop",
			t."nFramesQty",
			t."dtStartReal",
			t."dtStopReal",
			t."dtStartPlanned",
			t."dtStart" + ((t."nLength"::text || ' millisecond'::text)::interval) AS "dtStopPlanned",
			t."dtStartQueued",
			t."dtStartHard",
			t."dtStartSoft",
			t."dtTimingsUpdate",
			t."dtStart",
			COALESCE(t."dtStop", t."dtStart" + ((t."nLength"::text || ' millisecond'::text)::interval)) AS "dtStop",
			CASE
				WHEN plgs.id IS NULL THEN false
				ELSE true
			END AS "bPlug",
			ia_asset."nValue" AS "idAssets",
			ia_beepadv."nValue" AS "nBeepAdvBlockID",
			ia_beeppl."nValue" AS "nBeepClipBlockID",
			vv_asset."idVideoTypes",
			vv_asset."sVideoTypeName"
		FROM pl."tItems" itm
			LEFT JOIN pl."tClasses" c ON c.id = itm."idClasses"
			LEFT JOIN pl."tStatuses" sts ON sts.id = itm."idStatuses"
			LEFT JOIN media."vFiles" f ON f.id = itm."idFiles"
			LEFT JOIN pl."vItemTimings" t ON itm.id = t.id
			LEFT JOIN pl."tItemAttributes" ia_asset ON ia_asset."idItems" = itm.id AND 'asset'::text = ia_asset."sKey"::text
			LEFT JOIN mam."vAssetsVideoTypes" vv_asset ON vv_asset.id = ia_asset."nValue"
			LEFT JOIN pl."tItemAttributes" ia_beepadv ON ia_beepadv."idItems" = itm.id AND 'nBeepAdvBlockID'::text = ia_beepadv."sKey"::text
			LEFT JOIN pl."tItemAttributes" ia_beeppl ON ia_beeppl."idItems" = itm.id AND 'nBeepClipBlockID'::text = ia_beeppl."sKey"::text
			LEFT JOIN pl."tPlugs" plgs ON f.id = plgs."idFiles";

CREATE OR REPLACE VIEW pl."vPlayListResolvedOrdered" AS 
	SELECT id,
			"idHK",
			"idStatuses",
			"idClasses",
			"idFiles",
			"sName",
			"sNote",
			"sStatusName",
			"bLocked",
			"sClassName",
			"idStorages",
			"idStorageTypes",
			"sStorageTypeName",
			"sPath",
			"sStorageName",
			"bStorageEnabled",
			"sFilename",
			"dtLastFileEvent",
			"eFileError",
			"nFrameStart",
			"nFrameStop",
			"nFramesQty",
			"dtStartReal",
			"dtStopReal",
			"dtStartPlanned",
			"dtStopPlanned",
			"dtStartQueued",
			"dtStartHard",
			"dtStartSoft",
			"dtTimingsUpdate",
			"dtStart",
			"dtStop",
			"bPlug",
			"idAssets",
			"nBeepAdvBlockID",
			"nBeepClipBlockID",
			"idVideoTypes",
			"sVideoTypeName"
		FROM pl."vPlayListResolved"
		ORDER BY "dtStart";
  
CREATE OR REPLACE VIEW pl."vComingUp" AS 
	SELECT plr.*, CASE WHEN plr.id=tic.id THEN tic."nFrameCurrent" ELSE NULL END AS "nFrameCurrent"
		FROM pl."vPlayListResolved" plr, pl."vTimedItemCurrent" tic
		WHERE plr."sStatusName"<>'played' AND (plr.id = tic.id OR plr."dtStartPlanned" >= tic."dtStart");

CREATE OR REPLACE VIEW pl."vPlaylistFramesQty" AS 
	SELECT CASE WHEN sum("nFramesQty") IS NULL THEN 0 ELSE sum("nFramesQty") END AS "nDuration"
		FROM pl."vComingUp";

CREATE OR REPLACE VIEW pl."vPlayListClips" AS 
	SELECT DISTINCT plr.id
		FROM pl."vPlayListResolved" plr, mam."tVideos" v, mam."tVideoTypes" vt
		WHERE plr."idAssets" = v."idAssets" AND v."idVideoTypes" = vt.id AND vt."sName"::text = 'clip'::text;
