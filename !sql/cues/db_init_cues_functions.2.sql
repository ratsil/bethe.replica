------------------------------------ cues.tPluginPlaylistItem
	CREATE OR REPLACE FUNCTION cast_to_xml(oPLI cues.tPluginPlaylistItem) RETURNS xml AS
		$BODY$
			SELECT xmlforest(oPLI.id AS "nID", oPLI."oStatus"::xml as "oStatus", oPLI."oAsset"::xml as "oAsset", oPLI."dtStarted")
		$BODY$
			LANGUAGE sql VOLATILE;
	CREATE CAST (cues.tPluginPlaylistItem AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylistItem);

	CREATE OR REPLACE FUNCTION cast_from_xml_cues_tPluginPlaylistItem(oXML xml) RETURNS cues.tPluginPlaylistItem AS
		$BODY$
			SELECT CASE WHEN oXML IS NULL THEN NULL ELSE ROW(
				(xpath('/*/nID/text()', oXML))[1]::text::bigint,
				(xpath('/*/oStatus', oXML))[1]::tINP,
				(xpath('/*/oAsset', oXML))[1]::mam."vAssetsResolved",
				(xpath('/*/dtStarted/text()', oXML))[1]::text::timestamptz
			)::cues.tPluginPlaylistItem END
		$BODY$
			LANGUAGE sql VOLATILE;
	CREATE CAST (xml as cues.tPluginPlaylistItem) WITH FUNCTION cast_from_xml_cues_tPluginPlaylistItem(xml);

	CREATE OR REPLACE FUNCTION cast_to_xml(aPLIs cues.tPluginPlaylistItem[]) RETURNS xml AS
		$BODY$
		DECLARE
			oPLI cues.tPluginPlaylistItem;
			oRetVal xml;
		BEGIN
			FOR oPLI IN SELECT * FROM unnest(aPLIs) LOOP
				oRetVal := xmlconcat(oRetVal, xmlelement(name "oItem", oPLI::xml));
			END LOOP;
			RETURN oRetVal;
		END;	
		$BODY$
			LANGUAGE plpgsql VOLATILE;
	CREATE CAST (cues.tPluginPlaylistItem[] AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylistItem[]);

------------------------------------ cues.tPluginPlaylist
	CREATE OR REPLACE FUNCTION cast_to_xml(oPlaylist cues.tPluginPlaylist) RETURNS xml AS
		$BODY$
			SELECT xmlforest(oPlaylist.id AS "nID", oPlaylist."sName", oPlaylist."dtStart", oPlaylist."dtStop", (oPlaylist."aItems"::cues.tPluginPlaylistItem[])::xml as "aItems")
		$BODY$
			LANGUAGE sql VOLATILE;
	CREATE CAST (cues.tPluginPlaylist AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylist);

	CREATE OR REPLACE FUNCTION cast_from_xml_cues_tPluginPlaylist(oXML xml) RETURNS cues.tPluginPlaylist AS
		$BODY$
			SELECT CASE WHEN oXML IS NULL THEN NULL ELSE ROW(
				(xpath('/*/nID/text()', oXML))[1]::text::bigint,
				(xpath('/*/sName/text()', oXML))[1]::text,
				(xpath('/*/dtStart/text()', oXML))[1]::text::timestamptz,
				(xpath('/*/dtStop/text()', oXML))[1]::text::timestamptz,
				xpath('/*/aItems/oItem', oXML)::cues.tPluginPlaylistItem[]
			)::cues.tPluginPlaylist END
		$BODY$
			LANGUAGE sql VOLATILE;
	CREATE CAST (xml as cues.tPluginPlaylist) WITH FUNCTION cast_from_xml_cues_tPluginPlaylist(xml);
----------------------------------- cues."tPlugins"
	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistItemSave"(oPLI cues.tPluginPlaylistItem, bException bool) RETURNS cues.tPluginPlaylistItem AS
		$BODY$
		DECLARE
			nValue bigint;
			nID bigint;
			oPLISaved record;
			oRetVal cues.tPluginPlaylistItem;
		BEGIN
			SELECT * INTO oPLISaved FROM cues."vPluginPlaylistItems" WHERE oPLI.id=("oItem").id;
			oPLISaved := oPLISaved."oItem";
			IF oPLISaved IS NULL THEN
				IF bException THEN
					RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST ITEM [%]', oPLI.id;
				END IF;
				RETURN NULL;
			END IF;
			IF oPLI."oStatus" <> oPLISaved."oStatus" OR ( oPLI."oStatus").id <> (oPLISaved."oStatus").id THEN
				SELECT b.id INTO nID FROM cues."vBinds" b WHERE oPLI.id = b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'pl' = (i."oTableTarget")."sSchema" AND 'tStatuses' = (i."oTableTarget")."sName" AND 'status'=b."sName";
				IF FOUND THEN
					UPDATE cues."tBinds" SET "nValue"=(oPLI."oStatus").id WHERE nID=id;
				ELSIF oPLI."oStatus" IS NOT NULL THEN
					PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('pl','tStatuses'), 'status', (oPLI."oStatus").id, true);
				END IF;
			END IF;
			IF oPLI."oAsset" <> oPLISaved."oAsset" OR ( oPLI."oAsset").id <> (oPLISaved."oAsset").id THEN
				SELECT b.id INTO nID FROM cues."vBinds" b WHERE oPLI.id=b.id;
				IF FOUND THEN
					UPDATE cues."tBinds" SET "nValue"=(oPLI."oAsset").id WHERE nID=id;
				ELSIF oPLI."oStatus" IS NOT NULL THEN
					PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
				END IF;
			END IF;
			IF oPLI."dtStarted" <> oPLISaved."dtStarted" THEN
				IF EXISTS(SELECT b.id INTO nID FROM cues."vBindTimestamps" b WHERE oPLI.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'started'=b."sName") THEN
					nValue := NULL;
					IF oPLI."dtStarted" IS NOT NULL THEN
						nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPLI."dtStarted") || '}}')::text[][], true, false);
					END IF;
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE nID=id;
				ELSE
					IF oPLI."dtStarted" IS NOT NULL THEN
						nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPLI."dtStarted") || '}}')::text[][], true, false);
						PERFORM "fBindAdd"(ROW('cues','tBinds'), oPLI.id, ROW('cues','tTimestamps'), 'started', nValue, true);
					END IF;
				END IF;
			END IF;
			SELECT * INTO oPLISaved FROM cues."vPluginPlaylistItems" WHERE oPLI.id=("oItem").id;
			RETURN oPLISaved."oItem";
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistItemSave"(oXML xml) RETURNS cues.tPluginPlaylistItem AS
		$BODY$
			SELECT cues."fPluginPlaylistItemSave"(oXML::cues.tPluginPlaylistItem, true);
		$BODY$
		LANGUAGE sql VOLATILE;


	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistSave"(oPlaylist cues.tPluginPlaylist, bException bool) RETURNS cues.tPluginPlaylist AS
		$BODY$
		DECLARE
			oTable table_name;
			nValue bigint;
			oPlaylistSaved record;
			oPLI cues.tPluginPlaylistItem;
		BEGIN
			
			oTable := ROW('cues','tBinds');
			IF oPlaylist.id IS NOT NULL THEN
				SELECT * INTO oPlaylistSaved FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
				oPlaylistSaved := oPlaylistSaved."oPlaylist";
				IF oPlaylistSaved IS NULL THEN
					IF bException THEN
						RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST [%]', ROW(oPlaylist.id, oPlaylist."sName");
					END IF;
					RETURN NULL;
				END IF;
				IF oPlaylist."sName" <> oPlaylistSaved."sName" THEN
					nValue := "fTableAdd"(ROW('cues','tStrings'), ('{{oValue, ' || oPlaylist."sName" || '}}')::text[][], true, false);
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindStrings" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'name'=b."sName");
				END IF;
				IF oPlaylist."dtStart" <> oPlaylistSaved."dtStart" THEN
					nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStart") || '}}')::text[][], true, false);
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindTimestamps" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'start'=b."sName");
				END IF;
				IF oPlaylist."dtStop" <> oPlaylistSaved."dtStop" THEN
					nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStop") || '}}')::text[][], true, false);
					UPDATE cues."tBinds" SET "nValue"=nValue WHERE id IN (SELECT b.id FROM cues."vBindTimestamps" b WHERE oPlaylist.id=b."idSource" AND 'cues' = (b."oTableSource")."sSchema" AND 'tBinds' = (b."oTableSource")."sName" AND 'stop'=b."sName");
				END IF;
				FOR nIndx IN array_lower(oPlaylist."aItems", 1) .. array_upper(oPlaylist."aItems", 1) LOOP
					oPLI := oPlaylist."aItems"[nIndx];
					IF oPLI.id IS NULL THEN
						oPLI.id := "fBindAdd"(oTable, oPlaylist.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
						oPlaylist."aItems"[nIndx] := oPLI;
					END IF;
				END LOOP;
				FOR oPLI IN SELECT * FROM unnest(oPlaylistSaved."aItems") ac WHERE ac.id NOT IN (SELECT an.id FROM unnest(oPlaylist."aItems") an) LOOP
					DELETE FROM cues."tBinds" WHERE oPLI.id=id OR id IN(SELECT b.id FROM cues."vBinds" b WHERE oPLI.id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName");
				END LOOP;
			ELSIF EXISTS(SELECT "oPlaylist" FROM cues."vPluginPlaylists" WHERE oPlaylist."sName"=("oPlaylist")."sName") THEN
				IF bException THEN
					RAISE EXCEPTION 'PLUGIN PLAYLIST WITH SPECIFIED NAME ALREADY EXISTS [%]', oPlaylist."sName";
				END IF;
				RETURN NULL;
			ELSE
				nValue := "fTableGet"(ROW('cues','tPlugins'), '{{sName, "playlist"}}'::text[][], true);
				oPlaylist.id := "fBindAdd"(ROW('cues','tPlugins'), nValue, NULL, 'instance', NULL, true);
				nValue := "fTableAdd"(ROW('cues','tStrings'), ('{{oValue, ' || oPlaylist."sName" || '}}')::text[][], true, false);
				PERFORM "fBindAdd"(oTable, oPlaylist.id, ROW('cues','tStrings'), 'name', nValue, true);
				nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, ' || quote_literal(oPlaylist."dtStart") || '}}')::text[][], true, false);
				PERFORM "fBindAdd"(oTable, oPlaylist.id, ROW('cues','tTimestamps'), 'start', nValue, true);
				nValue := "fTableAdd"(ROW('cues','tTimestamps'), ('{{oValue, "' || quote_literal(oPlaylist."dtStop") || '"}}')::text[][], true, false);
				PERFORM "fBindAdd"(oTable, oPlaylist.id, ROW('cues','tTimestamps'), 'stop', nValue, true);
				FOR nIndx IN array_lower(oPlaylist."aItems", 1) .. array_upper(oPlaylist."aItems", 1) LOOP
					oPLI := oPlaylist."aItems"[nIndx];
					oPLI.id := "fBindAdd"(oTable, oPlaylist.id, ROW('mam','tAssets'), 'item', (oPLI."oAsset").id, true);
					oPlaylist."aItems"[nIndx] := oPLI;
				END LOOP;
			END IF;
			FOR nIndx IN array_lower(oPlaylist."aItems", 1) .. array_upper(oPlaylist."aItems", 1) LOOP
				PERFORM cues."fPluginPlaylistItemSave"(oPlaylist."aItems"[nIndx], true);
			END LOOP;

			SELECT * INTO oPlaylistSaved FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
			RETURN oPlaylistSaved."oPlaylist";
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;
	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistSave"(oXML xml) RETURNS cues.tPluginPlaylist AS
		$BODY$
			SELECT cues."fPluginPlaylistSave"(oXML::cues.tPluginPlaylist, true);
		$BODY$
		LANGUAGE sql VOLATILE;

	CREATE OR REPLACE FUNCTION cues."fPluginPlaylistDelete"(oPlaylist cues.tPluginPlaylist, bException bool) RETURNS VOID AS
		$BODY$
		DECLARE
			oValue record;
		BEGIN
			SELECT * INTO oValue FROM cues."vPluginPlaylists" WHERE oPlaylist.id=("oPlaylist").id;
			IF oValue IS NULL THEN
				IF bException THEN
					RAISE EXCEPTION 'CANNOT FIND SPECIFIED PLUGIN PLAYLIST [id:%]',oPlaylist.id;
				END IF;
				RETURN;
			END IF;
			oPlaylist := oValue."oPlaylist";
			DELETE FROM cues."tBinds" WHERE id IN(
					SELECT b.id FROM unnest(oPlaylist."aItems") as i, cues."vBinds" b WHERE (i).id=b.id OR ((i).id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName")
					UNION
					SELECT b.id FROM cues."vBinds" b WHERE oPlaylist.id=b.id OR (oPlaylist.id=b."idSource" AND 'cues'=(b."oTableSource")."sSchema" AND 'tBinds'=(b."oTableSource")."sName")
				);
		END;
		$BODY$
		LANGUAGE plpgsql VOLATILE;
