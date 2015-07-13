CREATE OR REPLACE VIEW cues."vBinds" AS
	SELECT b.*, ROW(ts.*)::hk."tRegisteredTables" AS "oTableSource", CASE WHEN tt.id IS NULL THEN NULL ELSE ROW(tt.*)::hk."tRegisteredTables" END AS "oTableTarget", bt."bUnique", bt."sName"
		FROM cues."tBinds" b, hk."tRegisteredTables" ts, cues."tBindTypes" bt
			LEFT JOIN hk."tRegisteredTables" tt ON bt."idTableTarget" = tt.id
		WHERE b."idBindTypes"=bt.id AND bt."idTableSource" = ts.id;
CREATE OR REPLACE VIEW cues."vBindStrings" AS
	SELECT b.*, s."oValue"
		FROM cues."vBinds" b, hk."tRegisteredTables" t, cues."tStrings" s
		WHERE (b."oTableTarget").id=t.id AND 'cues'=t."sSchema" AND 'tStrings'=t."sName" AND b."nValue" = s.id;
CREATE OR REPLACE VIEW cues."vBindTimestamps" AS
	SELECT b.*, s."oValue"
		FROM cues."vBinds" b, hk."tRegisteredTables" t, cues."tTimestamps" s
		WHERE (b."oTableTarget").id=t.id AND 'cues'=t."sSchema" AND 'tTimestamps'=t."sName" AND b."nValue" = s.id;

CREATE OR REPLACE VIEW cues."vPluginPlaylistItems" AS
	SELECT ROW(b.id, s."oStatus", ROW(a.*), st."oValue")::cues.tPluginPlaylistItem as "oItem"
		FROM hk."tRegisteredTables" t, mam."vAssetsResolved" a, cues."vBinds" b
			LEFT JOIN
			(
				SELECT i.id, ROW(s.id, s."sName")::tINP as "oStatus"
					FROM cues."tBinds" i, cues."vBinds" b, hk."tRegisteredTables" t, pl."tStatuses" s
					WHERE (b."oTableTarget").id = t.id AND 'pl' = t."sSchema" AND 'tStatuses' = t."sName" AND b."nValue" = s.id AND i.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'status'=b."sName"
			) s ON b.id=s.id		
			LEFT JOIN
			(
					SELECT i.id, t."oValue" 
						FROM cues."tBinds" i, cues."vBindTimestamps" t
						WHERE i.id=t."idSource" AND 'cues' = (t."oTableSource")."sSchema" AND 'tBinds' = (t."oTableSource")."sName" AND 'stop'=t."sName"
			) st ON b.id=st.id		
		WHERE (b."oTableTarget").id = t.id AND 'mam' = t."sSchema" AND 'tAssets' = t."sName" AND b."nValue" = a.id AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'item'=b."sName";
CREATE OR REPLACE VIEW cues."vPluginPlaylists" AS
	SELECT ROW(p.id, pn."oValue", pst."oValue", psp."oValue", pa."aItems")::cues.tPluginPlaylist as "oPlaylist"
		FROM 
			(
				SELECT p.id, s."oValue" 
					FROM cues."tBinds" p, cues."vBindStrings" s
					WHERE p.id=s."idSource" AND 'cues' = (s."oTableSource")."sSchema" AND 'tBinds' = (s."oTableSource")."sName" AND 'name'=s."sName"
			) pn,
			(
				SELECT p.id, t."oValue" 
					FROM cues."tBinds" p, cues."vBindTimestamps" t
					WHERE p.id=t."idSource" AND 'cues' = (t."oTableSource")."sSchema" AND 'tBinds' = (t."oTableSource")."sName" AND 'start'=t."sName"
			) pst,
			(
				SELECT p.id, t."oValue" 
					FROM cues."tBinds" p, cues."vBindTimestamps" t
					WHERE p.id=t."idSource" AND 'cues' = (t."oTableSource")."sSchema" AND 'tBinds' = (t."oTableSource")."sName" AND 'stop'=t."sName"
			) psp,
			(	
				SELECT b.id
					FROM cues."tPlugins" p, cues."vBinds" b, hk."tRegisteredTables" rt
					WHERE (b."oTableSource").id=rt.id AND 'cues'=rt."sSchema" AND 'tPlugins'=rt."sName" AND b."idSource" = p.id AND 'playlist'=p."sName" AND 'instance'=b."sName"
			) p
			LEFT JOIN
			(
				SELECT p.id, array_agg(i."oItem") as "aItems"
					FROM cues."tBinds" p, cues."vBinds" b, hk."tRegisteredTables" t, cues."vPluginPlaylistItems" i
					WHERE (b."oTableTarget").id = t.id AND b.id = (i."oItem").id AND p.id=b."idSource"
					GROUP BY p.id
			) pa ON p.id=pa.id
		WHERE p.id=pn.id AND p.id=pst.id AND p.id=psp.id;