------------------------------------ cues.tPluginPlaylistItem
	CREATE TYPE cues.tPluginPlaylistItem AS (
			id bigint,
			"oStatus" tINP,
			"oAsset" mam."vAssetsResolved",
			"dtStarted" timestamp with time zone
		);

------------------------------------ cues.tPluginPlaylist
	CREATE TYPE cues.tPluginPlaylist AS (
			id bigint,
			"sName" name,
			"dtStart" timestamp with time zone,
			"dtStop" timestamp with time zone,
			"aItems" cues.tPluginPlaylistItem[]
		);