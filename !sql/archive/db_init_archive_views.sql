CREATE OR REPLACE VIEW archive."vHouseKeepingDeleted" AS
	SELECT "idHouseKeeping" as id FROM hk."tDTEvents" WHERE "idDTEventTypes"=3;

CREATE OR REPLACE VIEW archive."vPlayListResolvedFull" AS
	SELECT
		id,NULL as "idHK","idStatuses","idClasses","idFiles","sName","sNote","sStatusName",true as "bLocked","sClassName",
		"idStorages","idStorageTypes","sStorageTypeName","sPath","sStorageName","bStorageEnabled","sFilename","dtLastFileEvent", "eFileError",
		"nFrameStart","nFrameStop","nFramesQty","dtStartReal","dtStopReal",
		"dtStartPlanned",("dtStartPlanned" + ((("nFrameStop" - "nFrameStart" + 1) * 40)::text || ' milliseconds')::interval) as "dtStopPlanned",
		"dtStartQueued","dtStartHard","dtStartSoft","dtTimingsUpdate","dtStartReal" as "dtStart","dtStopReal" as "dtStop",
		"bPlug","idAssets","nBeepAdvBlockID","nBeepClipBlockID"	
		FROM archive."pl.tItems" 

	UNION

	SELECT
		id,"idHK","idStatuses","idClasses","idFiles","sName","sNote","sStatusName","bLocked","sClassName",
		"idStorages","idStorageTypes","sStorageTypeName","sPath","sStorageName","bStorageEnabled","sFilename","dtLastFileEvent", "eFileError",
		"nFrameStart","nFrameStop","nFramesQty","dtStartReal","dtStopReal",
		"dtStartPlanned","dtStopPlanned",
		"dtStartQueued","dtStartHard","dtStartSoft","dtTimingsUpdate","dtStart","dtStop",
		"bPlug","idAssets","nBeepAdvBlockID","nBeepClipBlockID"
		FROM pl."vPlayListResolved";

CREATE OR REPLACE VIEW archive."vPlayListWithAssetsResolvedFull" AS 
	SELECT p.id, p."idHK", p."idStatuses", p."idClasses", p."idFiles", p."idStorages", p."idStorageTypes", p."sStorageTypeName", p."sPath", p."sStorageName", p."bStorageEnabled", p."sFilename", p."dtLastFileEvent", p."eFileError", p."sName", p."sNote", p."sStatusName", p."bLocked", p."sClassName", p."nFrameStart", p."nFrameStop", p."nFramesQty", p."dtStartPlanned", p."dtStopPlanned", p."dtStartHard", p."dtStartSoft", p."dtStartReal", p."dtStopReal", p."dtStart", p."dtStop", p."bPlug", p."nBeepAdvBlockID", p."nBeepClipBlockID", p."idAssets", a."sName" AS "sAssetName", a."idVideos", a."sVideoName", a."idVideoTypes", a."sVideoTypeName", a."nPersonsQty", a."nAlbumsQty", a."nStylesQty", a."idRotations", a."sRotationName", a."idPalettes", a."sPaletteName", a."idSoundLevelsForStart", a."sSoundLevelNameForStart", a."idSoundLevelsForStop", a."sSoundLevelNameForStop", a."idCues", a."sCueSong", a."sCueArtist", a."sCueAlbum", a."nCueYear", a."sCuePossessor", a."nFramesQty" AS "nAssetFramesQty", a."nFrameIn", a."nFrameOut", a."dtLastPlayed", a."bPLEnabled", a."nTestatorClassID"
		FROM archive."vPlayListResolvedFull" p
		LEFT JOIN mam."vAssetsResolved" a ON p."idAssets" = a.id;
