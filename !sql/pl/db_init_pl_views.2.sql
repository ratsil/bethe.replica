CREATE OR REPLACE VIEW pl."vPlayListClips" AS 
	SELECT DISTINCT plr.id
		FROM pl."vPlayListResolved" plr, mam."tVideos" v, mam."tVideoTypes" vt
		WHERE plr."idAssets" = v."idAssets" AND v."idVideoTypes" = vt.id AND vt."sName"::text = 'clip'::text;
