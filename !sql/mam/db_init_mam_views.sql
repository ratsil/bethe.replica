CREATE OR REPLACE VIEW mam."vAssets" AS
	SELECT ast.id, ast."sName", tbl."sName" as "sTableName", attr."sKey", attr."nValue"
		FROM mam."tAssets" ast 
			LEFT JOIN mam."tAssetAttributes" attr ON ast."id"=attr."idAssets"
			LEFT JOIN hk."tRegisteredTables" tbl ON attr."idRegisteredTables"=tbl."id";

CREATE OR REPLACE VIEW mam."vVideos" AS
	SELECT v.id, v."sName", v."idVideoTypes", vt."sName" as "sVideoTypeName"
		FROM mam."tVideos" v, mam."tVideoTypes" vt
		WHERE v."idVideoTypes" = vt.id;

CREATE OR REPLACE VIEW mam."vPersons" AS
	SELECT p.id, p."sName", pt.id as "idPersonTypes", pt."sName" as "sPersonTypeName"
		FROM mam."tPersons" p, mam."tPersonTypes" pt
		WHERE p."idPersonTypes"=pt.id;

CREATE OR REPLACE VIEW mam."vStyles" AS
	SELECT cv.id, cv."sValue" as "sName"
		FROM mam."tCategoryValues" cv, mam."tCategories" c
		WHERE c."sName"='style' AND cv."idCategories"=c.id;

CREATE OR REPLACE VIEW mam."vRotations" AS
	SELECT cv.id, cv."sValue" as "sName"
		FROM mam."tCategoryValues" cv, mam."tCategories" c
		WHERE c."sName"='rotation' AND cv."idCategories"=c.id;

CREATE OR REPLACE VIEW mam."vPalettes" AS
	SELECT cv.id, cv."sValue" as "sName"
		FROM mam."tCategoryValues" cv, mam."tCategories" c
		WHERE c."sName"='palette' AND cv."idCategories"=c.id;

CREATE OR REPLACE VIEW mam."vSoundLevels" AS
	SELECT cv.id, cv."sValue" as "sName"
		FROM mam."tCategoryValues" cv, mam."tCategories" c
		WHERE c."sName"='sound_level' AND cv."idCategories"=c.id;

CREATE OR REPLACE VIEW mam."vAssetTypes" AS
	SELECT ast.id, a.id AS "idType", a."sName" AS "sType"
		FROM mam."vAssets" ast, mam."tAssetTypes" a
		WHERE ast."sKey" = 'asset_type' AND a.id = ast."nValue";

CREATE OR REPLACE VIEW mam."vParents" AS
	SELECT a.id, aa."nValue" as "idParent"
		FROM mam."tAssets" a
		JOIN mam."tAssetAttributes" aa ON a.id = aa."idAssets" AND aa."sKey"::text = 'parent'::text;

CREATE OR REPLACE VIEW mam."vAssetTimings" AS
	SELECT a.id, max(lp."dtStart") AS "dtLastPlayed"
		FROM mam."tAssets" a
			LEFT JOIN pl."tItemAttributes" ia ON a.id = ia."nValue" AND ia."sKey" = 'asset'
			LEFT JOIN pl."vItemTimings" lp ON lp.id = ia."idItems"
		GROUP BY a.id;

CREATE OR REPLACE VIEW mam."vAssetsEnabled" AS
	SELECT id
		FROM mam."vAssets"
		WHERE 'bPLEnabled'="sKey" AND 1="nValue";

CREATE OR REPLACE VIEW mam."vAssetsVideoTypes" AS
	SELECT ast.id, vtp.id as "idVideoTypes", vtp."sName" as "sVideoTypeName"
		FROM mam."vAssets" ast, mam."tVideos" vid, mam."tVideoTypes" vtp
		WHERE ast.id=vid."idAssets" AND vid."idVideoTypes"=vtp.id;

CREATE OR REPLACE VIEW mam."vAssetsPersons" AS
	SELECT ast.id, p.id as "idPersons", p."sName" as "sPersonName"
		FROM mam."vAssets" ast, mam."tPersons" p 
		WHERE 'tPersons'=ast."sTableName" AND p.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsAlbums" AS
	SELECT ast.id, a.id as "idAlbums", a."sName" as "sAlbumName"
		FROM mam."vAssets" ast, mam."tAlbums" a
		WHERE 'tAlbums'=ast."sTableName" AND a.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsCues" AS
	SELECT ast.id, c.id as "idCues", c."sSong", c."sArtist", c."sAlbum", c."nYear", c."sPossessor"
		FROM mam."vAssets" ast, mam."tCues" c 
		WHERE 'tCues'=ast."sTableName" AND c.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsStyles" AS
	SELECT ast.id, s.id as "idStyles", s."sName" as "sStyleName"
		FROM mam."vAssets" ast, mam."vStyles" s
		WHERE 'tCategoryValues'=ast."sTableName" AND s.id=ast."nValue";
		
CREATE OR REPLACE VIEW mam."vAssetsRotations" AS
	SELECT ast.id, r.id as "idRotations", r."sName" as "sRotationName"
		FROM mam."vAssets" ast, mam."vRotations" r
		WHERE 'tCategoryValues'=ast."sTableName" AND r.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsPalettes" AS
	SELECT ast.id, p.id as "idPalettes", p."sName" as "sPaletteName"
		FROM mam."vAssets" ast, mam."vPalettes" p
		WHERE 'tCategoryValues'=ast."sTableName" AND p.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsSoundLevels" AS
	SELECT ast.id, sl.id as "idSoundLevels", sl."sName" as "sSoundLevelName", ast."sKey" as "sSoundLevelType"
		FROM mam."vAssets" ast, mam."vSoundLevels" sl
		WHERE 'tCategoryValues'=ast."sTableName" AND sl.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsCustomValues" AS
	SELECT ast.id, ast."sKey" as "sCustomValueName", cv."sValue" as "sCustomValue"
		FROM mam."vAssets" ast, mam."tCustomValues" cv
		WHERE 'tCustomValues'=ast."sTableName" AND cv.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsFiles" AS
	SELECT ast.id, f.id as "idFiles", f."idStorages", f."idStorageTypes", f."sStorageTypeName", f."bStorageEnabled", 
			f."sPath", f."sStorageName", f."sFilename", f."dtLastFileEvent", f."sPath"||f."sFilename" as "sFile",
			f."eError"
		FROM mam."vAssets" ast, media."vFiles" f
		WHERE 'tFiles'=ast."sTableName" AND f.id=ast."nValue";

CREATE OR REPLACE VIEW mam."vAssetsTimeCodes" AS
	SELECT ast.id, ast_in."nValue" as "nFrameIn", ast_out."nValue" as "nFrameOut", ast."nValue" as "nFramesQty"
		FROM 
			mam."vAssets" ast
			LEFT JOIN mam."vAssets" ast_in ON ast.id=ast_in.id AND 'nFrameIn'=ast_in."sKey"
			LEFT JOIN mam."vAssets" ast_out ON ast.id=ast_out.id AND 'nFrameOut'=ast_out."sKey"
		WHERE 'nFramesQty'=ast."sKey";

CREATE OR REPLACE VIEW mam."vAssetsClasses" AS
	SELECT ast.id, c.id as "idClasses", c."sName" as "sClassName", c."idParent" as "nTestatorClassID"
		FROM mam."vAssets" ast, pl."tClasses" c
		WHERE 'tClasses'=ast."sTableName" AND c.id=ast."nValue";
		
CREATE OR REPLACE VIEW mam."vAssetsResolved" AS
		 SELECT DISTINCT ON (ast.id) ast.id, ast."sName", vid.id AS "idVideos", vid."sName" AS "sVideoName", vtp.id AS "idVideoTypes", vtp."sName" AS "sVideoTypeName", ast_p."nPersonsQty", ast_a."nAlbumsQty", ast_s."nStylesQty", ast_r."idRotations", ast_r."sRotationName", ast_pl."idPalettes", ast_pl."sPaletteName", ast_sli."idSoundLevels" AS "idSoundLevelsForStart", ast_sli."sSoundLevelName" AS "sSoundLevelNameForStart", ast_slo."idSoundLevels" AS "idSoundLevelsForStop", ast_slo."sSoundLevelName" AS "sSoundLevelNameForStop", ast_c."idCues", ast_c."sSong" AS "sCueSong", ast_c."sArtist" AS "sCueArtist", ast_c."sAlbum" AS "sCueAlbum", ast_c."nYear" AS "nCueYear", ast_c."sPossessor" AS "sCuePossessor", ast_f."idFiles", ast_f."idStorages", ast_f."idStorageTypes", ast_f."sStorageTypeName", ast_f."bStorageEnabled", ast_f."sPath", ast_f."sStorageName", ast_f."sFilename", ast_f."dtLastFileEvent", ast_f."sFile", ast_f."eError" AS "eFileError", ast_tc."nFrameIn", ast_tc."nFrameOut", ast_tc."nFramesQty", ast_lp."dtLastPlayed", 
				CASE
					WHEN ast_e.id IS NULL THEN 0
					ELSE 1
				END AS "bPLEnabled", ast_cl."idClasses", ast_cl."sClassName", ast_cl."nTestatorClassID", ast_tp."sType", ast_pr."idParent", ast_tp."idType"
		   FROM mam."vAssetTimings" ast_lp, mam."vAssets" ast
		   LEFT JOIN mam."vAssetsEnabled" ast_e ON ast.id = ast_e.id
		   LEFT JOIN ( SELECT "vAssetsPersons".id, count(*) AS "nPersonsQty"
			  FROM mam."vAssetsPersons"
			 GROUP BY "vAssetsPersons".id) ast_p ON ast.id = ast_p.id
		   LEFT JOIN ( SELECT "vAssetsAlbums".id, count(*) AS "nAlbumsQty"
		   FROM mam."vAssetsAlbums"
		  GROUP BY "vAssetsAlbums".id) ast_a ON ast.id = ast_a.id
		   LEFT JOIN ( SELECT "vAssetsStyles".id, count(*) AS "nStylesQty"
		   FROM mam."vAssetsStyles"
		  GROUP BY "vAssetsStyles".id) ast_s ON ast.id = ast_s.id
		   LEFT JOIN mam."vAssetsCues" ast_c ON ast.id = ast_c.id
		   LEFT JOIN mam."vAssetsRotations" ast_r ON ast.id = ast_r.id
		   LEFT JOIN mam."vAssetsPalettes" ast_pl ON ast.id = ast_pl.id
		   LEFT JOIN mam."vAssetsSoundLevels" ast_sli ON ast.id = ast_sli.id AND 'sound_start'::text = ast_sli."sSoundLevelType"::text
		   LEFT JOIN mam."vAssetsSoundLevels" ast_slo ON ast.id = ast_slo.id AND 'sound_end'::text = ast_slo."sSoundLevelType"::text
		   LEFT JOIN mam."vAssetsFiles" ast_f ON ast.id = ast_f.id
		   LEFT JOIN mam."vAssetsTimeCodes" ast_tc ON ast.id = ast_tc.id
		   LEFT JOIN mam."tVideos" vid ON ast.id = vid."idAssets"
		   LEFT JOIN mam."tVideoTypes" vtp ON vid."idVideoTypes" = vtp.id
		   LEFT JOIN mam."vAssetsClasses" ast_cl ON ast.id = ast_cl.id
		   LEFT JOIN mam."vAssetTypes" ast_tp ON ast.id = ast_tp.id
		   LEFT JOIN mam."vParents" ast_pr ON ast.id = ast_pr.id
		  WHERE ast.id = ast_lp.id AND ('nFramesQty'::text = ast."sKey"::text OR 'program'::text = vtp."sName"::text);

CREATE OR REPLACE VIEW mam."vPersonsCueLast" AS
	SELECT "idPersons" as id, "sArtist" as "sCue" 
		FROM mam."vAssetsCues" ac, (SELECT id, max("idPersons") as "idPersons", count(*) as "nQty" FROM mam."vAssetsPersons" GROUP BY id) ap 
		WHERE ac.id=ap.id AND 1 = ap."nQty";

-- media view declaration. here because of pl and mam references
CREATE OR REPLACE VIEW media."vFilesUnused" as
	SELECT * 
		FROM media."vFiles" 
		WHERE id NOT IN 
			(
				SELECT "idFiles" FROM pl."tItems" GROUP BY "idFiles" 
				UNION 
				SELECT "idFiles" FROM mam."vAssetsFiles" GROUP BY "idFiles" 
				UNION 
				SELECT "idFiles" FROM pl."tPlugs" GROUP BY "idFiles"
			);
CREATE OR REPLACE VIEW mam."vMacros" AS
	SELECT m.id, m."idMacroTypes", mt."sName" as "sMacroTypeName", m."sName", m."sValue"
		FROM mam."tMacroTypes" mt, mam."tMacros" m
		WHERE mt.id = m."idMacroTypes";