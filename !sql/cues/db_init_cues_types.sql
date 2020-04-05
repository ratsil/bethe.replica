------------------------------------ cues.tPluginPlaylistItem
	CREATE TYPE cues.tPluginPlaylistItem AS (
		id bigint,
		"oStatus" tINP,
		"oAsset" mam."vAssetsResolved",
		"dtStarted" timestamp with time zone
	);

	CREATE OR REPLACE FUNCTION cast_to_xml(oPLI cues.tPluginPlaylistItem) RETURNS xml AS
	$$
		SELECT xmlforest(oPLI.id AS "nID", oPLI."oStatus"::xml as "oStatus", oPLI."oAsset"::xml as "oAsset", oPLI."dtStarted")
	$$
	LANGUAGE sql VOLATILE;
	CREATE CAST (cues.tPluginPlaylistItem AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylistItem);

	CREATE OR REPLACE FUNCTION cast_from_xml_cues_tPluginPlaylistItem(oXML xml) RETURNS cues.tPluginPlaylistItem AS
	$$
		SELECT CASE WHEN oXML IS NULL THEN NULL ELSE ROW(
			(xpath('/*/nID/text()', oXML))[1]::text::bigint,
			(xpath('/*/oStatus', oXML))[1]::tINP,
			(xpath('/*/oAsset', oXML))[1]::mam."vAssetsResolved",
			(xpath('/*/dtStarted/text()', oXML))[1]::text::timestamptz
		)::cues.tPluginPlaylistItem END
	$$
	LANGUAGE sql VOLATILE;
	CREATE CAST (xml as cues.tPluginPlaylistItem) WITH FUNCTION cast_from_xml_cues_tPluginPlaylistItem(xml);

	CREATE OR REPLACE FUNCTION cast_to_xml(aPLIs cues.tPluginPlaylistItem[]) RETURNS xml AS
	$$
	DECLARE
		oPLI cues.tPluginPlaylistItem;
		oRetVal xml;
	BEGIN
		FOR oPLI IN SELECT * FROM unnest(aPLIs) LOOP
			oRetVal := xmlconcat(oRetVal, xmlelement(name "oItem", oPLI::xml));
		END LOOP;
		RETURN oRetVal;
	END;	
	$$
	LANGUAGE plpgsql VOLATILE;
	CREATE CAST (cues.tPluginPlaylistItem[] AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylistItem[]);

------------------------------------ cues.tPluginPlaylist
	CREATE TYPE cues.tPluginPlaylist AS (
		id bigint,
		"sName" name,
		"dtStart" timestamp with time zone,
		"dtStop" timestamp with time zone,
		"aItems" cues.tPluginPlaylistItem[]
	);

	CREATE OR REPLACE FUNCTION cast_to_xml(oPlaylist cues.tPluginPlaylist) RETURNS xml AS
	$$
		SELECT xmlforest(oPlaylist.id AS "nID", oPlaylist."sName", oPlaylist."dtStart", oPlaylist."dtStop", (oPlaylist."aItems"::cues.tPluginPlaylistItem[])::xml as "aItems")
	$$
	LANGUAGE sql VOLATILE;
	CREATE CAST (cues.tPluginPlaylist AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylist);

	CREATE OR REPLACE FUNCTION cast_to_xml(oPlaylist cues.tPluginPlaylist) RETURNS xml AS
	$$
		SELECT xmlforest(oPlaylist.id AS "nID", oPlaylist."sName", oPlaylist."dtStart", oPlaylist."dtStop", (oPlaylist."aItems"::cues.tPluginPlaylistItem[])::xml as "aItems")
	$$
	LANGUAGE sql VOLATILE;
	CREATE CAST (cues.tPluginPlaylist AS xml) WITH FUNCTION cast_to_xml(cues.tPluginPlaylist);

	CREATE OR REPLACE FUNCTION cast_from_xml_cues_tPluginPlaylist(oXML xml) RETURNS cues.tPluginPlaylist AS
	$$
		SELECT CASE WHEN oXML IS NULL THEN NULL ELSE ROW(
			(xpath('/*/nID/text()', oXML))[1]::text::bigint,
			(xpath('/*/sName/text()', oXML))[1]::text,
			(xpath('/*/dtStart/text()', oXML))[1]::text::timestamptz,
			(xpath('/*/dtStop/text()', oXML))[1]::text::timestamptz,
			xpath('/*/aItems/oItem', oXML)::cues.tPluginPlaylistItem[]
		)::cues.tPluginPlaylist END
	$$
	LANGUAGE sql VOLATILE;
	CREATE CAST (xml as cues.tPluginPlaylist) WITH FUNCTION cast_from_xml_cues_tPluginPlaylist(xml);
