CREATE OR REPLACE VIEW logs."vScrPlayListWithAssetsResolved" AS 
 SELECT scr.id,
    ts.id AS "idStatuses",
    ast_cl."idClasses",
    vaf."idFiles",
    vaf."idStorages",
    vaf."idStorageTypes",
    vaf."sStorageTypeName",
    vaf."sPath",
    vaf."sStorageName",
    vaf."bStorageEnabled",
    vaf."sFilename",
    vaf."dtLastFileEvent",
    vaf."eError" AS "eFileError",
    ta."sName",
    NULL::text AS "sNote",
    ts."sName" AS "sStatusName",
    false AS "bLocked",
    ast_cl."sClassName",
    ast_tc."nFrameIn" AS "nFrameStart",
    ast_tc."nFrameOut" AS "nFrameStop",
    ast_tc."nFramesQty",
    scr."dtStart" AS "dtStartPlanned",
    scr."dtStop" AS "dtStopPlanned",
    NULL::timestamp with time zone AS "dtStartHard",
    NULL::timestamp with time zone AS "dtStartSoft",
    scr."dtStart" AS "dtStartReal",
    scr."dtStop" AS "dtStopReal",
    scr."dtStart",
    scr."dtStop",
    false AS "bPlug",
    ta.id AS "idAssets",
    ta."sName" AS "sAssetName",
    vid.id AS "idVideos",
    vid."sName" AS "sVideoName",
    vtp.id AS "idVideoTypes",
    vtp."sName" AS "sVideoTypeName",
    NULL::bigint AS "nPersonsQty",
    NULL::bigint AS "nAlbumsQty",
    NULL::bigint AS "nStylesQty",
    ast_r."idRotations",
    ast_r."sRotationName",
    NULL::integer AS "idPalettes",
    NULL::character varying(255) AS "sPaletteName",
    NULL::integer AS "idSoundLevelsForStart",
    NULL::character varying(255) AS "sSoundLevelNameForStart",
    NULL::integer AS "idSoundLevelsForStop",
    NULL::character varying(255) AS "sSoundLevelNameForStop",
    ast_c."idCues",
    ast_c."sSong" AS "sCueSong",
    ast_c."sArtist" AS "sCueArtist",
    ast_c."sAlbum" AS "sCueAlbum",
    ast_c."nYear" AS "nCueYear",
    ast_c."sPossessor" AS "sCuePossessor",
    ast_tc."nFramesQty" AS "nAssetFramesQty",
    ast_tc."nFrameIn",
    ast_tc."nFrameOut",
    ast_lp."dtLastPlayed",
    0 AS "bPLEnabled",
    ast_cl."nTestatorClassID",
    ast_clar."aClasses"
   FROM logs."tSCR" scr
     LEFT JOIN mam."vAssetTimings" ast_lp ON scr."idAssets" = ast_lp.id
     LEFT JOIN mam."tAssets" ta ON scr."idAssets" = ta.id
     LEFT JOIN mam."vAssetsFiles" vaf ON scr."idAssets" = vaf.id
     LEFT JOIN mam."vAssetsCues" ast_c ON scr."idAssets" = ast_c.id
     LEFT JOIN mam."vAssetsTimeCodes" ast_tc ON scr."idAssets" = ast_tc.id
     LEFT JOIN mam."vAssetsClasses" ast_cl ON scr."idAssets" = ast_cl.id
     LEFT JOIN mam."vAssetsClassesArrays" ast_clar ON scr."idAssets" = ast_clar.id
     LEFT JOIN mam."tVideos" vid ON scr."idAssets" = vid."idAssets"
     LEFT JOIN mam."tVideoTypes" vtp ON vid."idVideoTypes" = vtp.id
     LEFT JOIN mam."vAssetsRotations" ast_r ON scr."idAssets" = ast_r.id
     LEFT JOIN pl."tStatuses" ts ON 'played'::text = ts."sName"::text;

CREATE OR REPLACE VIEW logs."vGameAssets" AS 
 SELECT tg.id,
    tg."dtStartPlanned",
    ta."sName" AS "sAssetsName"
   FROM logs."tGame" tg
     LEFT JOIN mam."tAssets" ta ON tg."idAssets" = ta.id
  WHERE tg."bIsActual" = true
  ORDER BY tg."dtStartPlanned" DESC;
