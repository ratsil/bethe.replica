GRANT replica_ingest TO replica_ingest_full;
GRANT replica_scr TO replica_scr_full;
GRANT replica_adm TO replica_adm_full;
GRANT replica_playlist TO replica_playlist_full;
GRANT replica_programs TO replica_assets;
GRANT replica_programs TO replica_programs_full;
GRANT replica_assets TO replica_assets_full;
GRANT replica_programs_full TO replica_assets_full;
GRANT replica_templates TO replica_templates_full;

-----------------------------------

GRANT USAGE ON SCHEMA hk TO replica_access;
GRANT EXECUTE ON FUNCTION hk."fUserAccessScopeGet"() TO replica_access;
GRANT SELECT ON TABLE hk."tUsers" TO replica_access;
GRANT SELECT ON TABLE hk."vUserAccessScopes" TO replica_access;
GRANT SELECT ON TABLE hk."tUserAttributes" TO replica_access;
GRANT SELECT ON TABLE hk."tWebPages" TO replica_access;

GRANT USAGE ON SCHEMA hk TO replica_client;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_client;
GRANT SELECT ON TABLE hk."vUserAccessScopes" TO replica_client;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_client;
GRANT SELECT,INSERT ON TABLE hk."tDTEvents" TO replica_client;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_client;
GRANT SELECT,INSERT ON TABLE hk."tUserAttributes" TO replica_client;
GRANT USAGE ON SEQUENCE hk."tUserAttributes_id_seq" TO replica_client;
GRANT SELECT ON TABLE hk."tUsers" TO replica_client;
GRANT SELECT, INSERT ON TABLE hk."tHouseKeeping" TO replica_client;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_client;

GRANT USAGE ON SCHEMA media TO replica_client;
GRANT SELECT,UPDATE,INSERT ON TABLE media."tStorages" TO replica_client;
GRANT USAGE ON SEQUENCE media."tStorages_id_seq" TO replica_client;
GRANT SELECT ON TABLE media."vStorages" TO replica_client;
GRANT SELECT,UPDATE,INSERT ON TABLE media."tFiles" TO replica_client;
GRANT USAGE ON SEQUENCE media."tFiles_id_seq" TO replica_client;
GRANT SELECT ON TABLE media."vFiles" TO replica_client;

GRANT USAGE ON SCHEMA mam TO replica_client;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE mam."tAssets" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tAssets_id_seq" TO replica_client;
GRANT ALL ON TABLE mam."tAssetAttributes" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_client;
GRANT SELECT ON TABLE mam."tVideoTypes" TO replica_client;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE mam."tVideos" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tVideos_id_seq" TO replica_client;
GRANT SELECT ON TABLE mam."tPersonTypes" TO replica_client;
GRANT SELECT,INSERT ON TABLE mam."tAlbums" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tAlbums_id_seq" TO replica_client;
GRANT ALL ON TABLE mam."tCues" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tCues_id_seq" TO replica_client;
GRANT ALL ON TABLE mam."tPersons" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tPersons_id_seq" TO replica_client;

GRANT SELECT,INSERT ON TABLE mam."tCategories" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tCategories_id_seq" TO replica_client;
GRANT ALL ON TABLE mam."tCategoryValues" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tCategoryValues_id_seq" TO replica_client;
GRANT SELECT ON TABLE mam."tTimeMapTypes" TO replica_client;
GRANT SELECT,INSERT ON TABLE mam."tTimeMaps" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tTimeMaps_id_seq" TO replica_client;
GRANT SELECT,INSERT ON TABLE mam."tTimeMapBinds" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tTimeMapBinds_id_seq" TO replica_client;

GRANT ALL ON TABLE mam."tCustomValues" TO replica_client;
GRANT USAGE ON SEQUENCE mam."tCustomValues_id_seq" TO replica_client;
GRANT SELECT ON TABLE mam."vAssets" TO replica_client;
GRANT SELECT ON TABLE mam."vStyles" TO replica_client;
GRANT SELECT ON TABLE mam."vRotations" TO replica_client;
GRANT SELECT ON TABLE mam."vPalettes" TO replica_client;
GRANT SELECT ON TABLE mam."vSex" TO replica_client;
GRANT SELECT ON TABLE mam."vSoundLevels" TO replica_client;
GRANT SELECT ON TABLE mam."vPersons" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsPersons" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsCues" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsStyles" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsRotations" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsPalettes" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsSex" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsSoundLevels" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsCustomValues" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsFiles" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_client;
GRANT SELECT ON TABLE mam."vAssetsVideoTypes" TO replica_client;

GRANT USAGE ON SCHEMA pl TO replica_client;
GRANT SELECT ON TABLE pl."tClasses" TO replica_client;
GRANT SELECT ON TABLE pl."tStatuses" TO replica_client;
GRANT SELECT ON TABLE pl."vClasses" TO replica_client;
GRANT ALL ON TABLE pl."tItems" TO replica_client;
GRANT USAGE ON SEQUENCE pl."tItems_id_seq" TO replica_client;
GRANT ALL ON TABLE pl."tItemAttributes" TO replica_client;
GRANT USAGE ON SEQUENCE pl."tItemAttributes_id_seq" TO replica_client;
GRANT SELECT ON TABLE pl."tItemDTEventTypes" TO replica_client;
GRANT ALL ON TABLE pl."tItemDTEvents" TO replica_client;
GRANT USAGE ON SEQUENCE pl."tItemDTEvents_id_seq" TO replica_client;
GRANT ALL ON TABLE pl."tPlugs" TO replica_client;
GRANT USAGE ON SEQUENCE pl."tPlugs_id_seq" TO replica_client;
GRANT ALL ON TABLE pl."tPlugOffsets" TO replica_client;
GRANT USAGE ON SEQUENCE pl."tPlugOffsets_id_seq" TO replica_client;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_client;
GRANT SELECT ON TABLE pl."vItemTimings" TO replica_client;
GRANT SELECT ON TABLE pl."vTimedItemLast" TO replica_client;
GRANT SELECT ON TABLE pl."vTimedItemCurrent" TO replica_client;
GRANT SELECT ON TABLE pl."vItemsLeft" TO replica_client;
GRANT SELECT ON TABLE pl."vPlaylistFramesQty" TO replica_client;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_client;

--GRANT EXECUTE ON FUNCTION pl."fItemsTimingsUpdate"() TO replica_client;

GRANT USAGE ON SCHEMA adm TO replica_client;
GRANT SELECT ON TABLE adm."tCommands" TO replica_client;
GRANT SELECT,INSERT,UPDATE ON TABLE adm."tCommandsQueue" TO replica_client;
GRANT USAGE ON SEQUENCE adm."tCommandsQueue_id_seq" TO replica_client;
GRANT SELECT,INSERT ON TABLE adm."tCommandParameters" TO replica_client;
GRANT USAGE ON SEQUENCE adm."tCommandParameters_id_seq" TO replica_client;
GRANT SELECT ON TABLE adm."tPreferences" TO replica_client;
GRANT SELECT ON TABLE adm."tTransliteration" TO replica_client;
GRANT SELECT ON TABLE adm."vPreferences" TO replica_client;
GRANT SELECT ON TABLE adm."tCommandStatuses" TO replica_client;

GRANT USAGE ON SCHEMA archive TO replica_client;
GRANT SELECT ON TABLE archive."pl.tItems" TO replica_client;
GRANT SELECT ON TABLE archive."vPlayListResolvedFull" TO replica_client;
GRANT SELECT ON TABLE archive."vPlayListWithAssetsResolvedFull" TO replica_client;

GRANT SELECT ON TABLE pl."tItemsCached" TO replica_client;

-----------------------------------
GRANT SELECT,INSERT,DELETE ON TABLE scr."tPlaques" TO replica_client;

-----------------------------------

GRANT USAGE ON SCHEMA ia TO replica_scr;
GRANT SELECT ON TABLE ia."tMessages" TO replica_scr;

GRANT USAGE ON SCHEMA media TO replica_scr;
GRANT SELECT ON TABLE media."tStorages" TO replica_scr;
GRANT USAGE ON SEQUENCE media."tStorages_id_seq" TO replica_scr;

GRANT SELECT ON TABLE cues."vClassAndTemplateBinds" TO replica_scr;

GRANT USAGE ON SCHEMA logs TO replica_scr_full;
GRANT SELECT,INSERT,DELETE ON TABLE logs."tSCR" TO replica_scr_full;
GRANT USAGE ON SEQUENCE logs."tSCR_id_seq" TO replica_scr;

GRANT USAGE ON SCHEMA scr TO replica_scr;
GRANT SELECT,INSERT,DELETE ON TABLE scr."tMessagesMarks" TO replica_scr;
GRANT USAGE ON SEQUENCE scr."tMessagesMarks_id_seq" TO replica_scr;
GRANT SELECT ON TABLE scr."tAnnouncements" TO replica_scr;
GRANT SELECT ON TABLE scr."vShiftCurrent" TO replica_scr;
GRANT SELECT ON TABLE scr."vAnnouncementsActual" TO replica_scr;
GRANT SELECT ON TABLE scr."vMessagesQueue" TO replica_scr;
GRANT SELECT ON TABLE scr."vShifts" TO replica_scr;
GRANT SELECT ON TABLE scr."vPlaques" TO replica_scr;
GRANT SELECT ON TABLE scr."tStoragesMappings" TO replica_scr;
GRANT USAGE ON SEQUENCE scr."tStoragesMappings_id_seq" TO replica_scr;
GRANT SELECT ON TABLE scr."vShiftCurrent" TO replica_scr;

GRANT SELECT,INSERT,UPDATE ON TABLE scr."tShifts" TO replica_scr_full;
GRANT USAGE ON SEQUENCE scr."tShifts_id_seq" TO replica_scr_full;
GRANT ALL ON TABLE scr."tAnnouncements" TO replica_scr_full;
GRANT USAGE ON SEQUENCE scr."tAnnouncements_id_seq" TO replica_scr_full;
GRANT ALL ON TABLE scr."tPlaques" TO replica_scr_full;
GRANT USAGE ON SEQUENCE scr."tPlaques_id_seq" TO replica_scr_full;
GRANT SELECT ON TABLE scr."tTemplates" TO replica_scr_full;

-----------------------------------

GRANT USAGE ON SCHEMA hk TO replica_player;
GRANT SELECT,UPDATE ON TABLE hk."tRegisteredTables" TO replica_player;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_player;
GRANT SELECT,INSERT ON TABLE hk."tDTEvents" TO replica_player;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_player;
GRANT SELECT,INSERT ON TABLE hk."tUserAttributes" TO replica_player;
GRANT USAGE ON SEQUENCE hk."tUserAttributes_id_seq" TO replica_player;
GRANT SELECT ON TABLE hk."tUsers" TO replica_player;
GRANT SELECT, INSERT ON TABLE hk."tHouseKeeping" TO replica_player;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_player;
GRANT SELECT ON TABLE hk."vUserAccessScopes" TO replica_player;

GRANT USAGE ON SCHEMA pl TO replica_player;
GRANT SELECT ON TABLE pl."tClasses" TO replica_player;
GRANT SELECT ON TABLE pl."tStatuses" TO replica_player;
GRANT USAGE ON SEQUENCE pl."tItems_id_seq" TO replica_player;
GRANT ALL ON TABLE pl."tItems" TO replica_player;
GRANT ALL ON TABLE pl."tItemAttributes" TO replica_player;
GRANT USAGE ON SEQUENCE pl."tItemAttributes_id_seq" TO replica_player;
GRANT SELECT ON TABLE pl."tItemDTEventTypes" TO replica_player;
GRANT ALL ON TABLE pl."tItemDTEvents" TO replica_player;
GRANT USAGE ON SEQUENCE pl."tItemDTEvents_id_seq" TO replica_player;
GRANT SELECT ON TABLE pl."tPlugs" TO replica_player;
GRANT SELECT, UPDATE ON TABLE pl."tPlugOffsets" TO replica_player;
GRANT SELECT ON TABLE pl."tProxies" TO replica_player;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_player;
GRANT SELECT ON TABLE pl."vPlayListResolvedOrdered" TO replica_player;
GRANT SELECT ON TABLE pl."vTimedItemCurrent" TO replica_player;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_player;
GRANT SELECT ON TABLE pl."vClasses" TO replica_player;
GRANT SELECT ON TABLE pl."vPlayListClips" TO replica_player;

GRANT USAGE ON SCHEMA mam TO replica_player;
GRANT SELECT ON TABLE mam."vMacros" TO replica_player;

GRANT USAGE ON SCHEMA media TO replica_player;
GRANT SELECT,INSERT ON TABLE media."tFiles" TO replica_player;
GRANT SELECT ON TABLE media."vFiles" TO replica_player;

GRANT USAGE ON SCHEMA adm TO replica_player;
GRANT SELECT,UPDATE ON TABLE adm."tCommandsQueue" TO replica_player;
GRANT SELECT ON TABLE adm."vCommandsQueue" TO replica_player;

GRANT SELECT ON TABLE pl."vItemsInserted" TO replica_player;

GRANT USAGE ON SCHEMA cues TO replica_player;
GRANT SELECT ON TABLE mam."vAssetsCues" TO replica_player;

GRANT SELECT,UPDATE,DELETE ON TABLE pl."tItemsCached" TO replica_player;
GRANT SELECT, INSERT ON TABLE adm."tCommandParameters" TO replica_player;
GRANT USAGE ON SEQUENCE adm."tCommandParameters_id_seq" TO replica_player;

-----------------------------------

GRANT USAGE ON SCHEMA hk TO replica_cues;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_cues;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_cues;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_cues;
GRANT ALL ON TABLE hk."tDTEvents" TO replica_cues;
GRANT USAGE ON SEQUENCE hk."tUserAttributes_id_seq" TO replica_cues;
GRANT SELECT,INSERT ON TABLE hk."tUserAttributes" TO replica_cues;
GRANT SELECT ON TABLE hk."tUsers" TO replica_cues;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_cues;
GRANT ALL ON TABLE hk."tHouseKeeping" TO replica_cues;
GRANT SELECT ON TABLE hk."vUserAccessScopes" TO replica_cues;

GRANT USAGE ON SCHEMA pl TO replica_cues;
GRANT SELECT ON TABLE pl."vItemTimings" TO replica_cues;
GRANT SELECT ON TABLE pl."vTimedItemCurrent" TO replica_cues;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_cues;
GRANT SELECT ON TABLE pl."vPlayListResolvedOrdered" TO replica_cues;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_cues;
GRANT USAGE ON SEQUENCE pl."tItemAttributes_id_seq" TO replica_cues;
GRANT SELECT,INSERT ON TABLE pl."tItemAttributes" TO replica_cues;

GRANT USAGE ON SCHEMA cues TO replica_cues;
GRANT SELECT ON TABLE cues."vClassAndTemplateBinds" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tClassAndTemplateBinds_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tClassAndTemplateBinds" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tTemplatesSchedule_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tTemplatesSchedule" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tDictionary_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tDictionary" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tBindTypes_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tBindTypes" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tBinds_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tBinds" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tStrings_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tStrings" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tTimestamps_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tTimestamps" TO replica_cues;
GRANT USAGE ON SEQUENCE cues."tPlugins_id_seq" TO replica_cues;
GRANT ALL ON TABLE cues."tPlugins" TO replica_cues;

GRANT ALL ON TABLE cues."vBinds" TO replica_cues;
GRANT ALL ON TABLE cues."vBindStrings" TO replica_cues;
GRANT ALL ON TABLE cues."vBindTimestamps" TO replica_cues;
GRANT ALL ON TABLE cues."vPluginPlaylistItems" TO replica_cues;
GRANT ALL ON TABLE cues."vPluginPlaylists" TO replica_cues;

GRANT USAGE ON SCHEMA adm TO replica_cues;
GRANT SELECT,UPDATE ON TABLE adm."tCommandsQueue" TO replica_cues;
GRANT SELECT ON TABLE adm."tCommandParameters" TO replica_cues;
GRANT SELECT ON TABLE adm."vCommandsQueue" TO replica_cues;

GRANT USAGE ON SCHEMA mam TO replica_cues;
GRANT SELECT ON TABLE mam."vAssetsCues" TO replica_cues;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_cues;
GRANT SELECT ON TABLE mam."vAssetsVideoTypes" TO replica_cues;
GRANT SELECT ON TABLE mam."vAssetsCustomValues" TO replica_cues;
GRANT SELECT ON TABLE mam."vMacros" TO replica_cues;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_cues;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_cues;


GRANT USAGE ON SCHEMA ia TO replica_cues;
GRANT SELECT ON TABLE ia."vMessagesResolved" TO replica_cues;
GRANT ALL ON TABLE ia."tMessageAttributes" TO replica_cues;
GRANT USAGE ON SEQUENCE ia."tMessageAttributes_id_seq" TO replica_cues;
GRANT SELECT ON TABLE ia."tDTEventTypes" TO replica_cues;
GRANT ALL ON TABLE ia."tDTEvents" TO replica_cues;
GRANT USAGE ON SEQUENCE ia."tDTEvents_id_seq" TO replica_cues;

GRANT SELECT ON TABLE ia."tVJMessages" TO replica_cues;

GRANT USAGE ON SCHEMA archive TO replica_cues;
GRANT SELECT ON TABLE archive."vPlayListResolvedFull" TO replica_cues;
GRANT SELECT ON pl."vItemAttributes" to replica_cues;

GRANT USAGE ON SCHEMA logs TO replica_cues;
GRANT SELECT,INSERT,DELETE ON TABLE logs."tGame" TO replica_cues;
GRANT USAGE ON SEQUENCE logs."tGame_id_seq" TO replica_cues;



-----------------------------------

GRANT USAGE ON SCHEMA pl TO replica_failover;
GRANT SELECT ON TABLE pl."tStatuses" TO replica_failover;
GRANT SELECT ON TABLE pl."tItemAttributes" TO replica_failover;
GRANT SELECT ON TABLE pl."tPlugs" TO replica_failover;
GRANT SELECT ON TABLE pl."tProxies" TO replica_failover;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_failover;
GRANT SELECT ON TABLE pl."vPlayListResolvedOrdered" TO replica_failover;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_failover;
GRANT SELECT ON TABLE pl."vPlayListClips" TO replica_failover;

GRANT USAGE ON SCHEMA media TO replica_failover;
GRANT SELECT ON TABLE media."vFiles" TO replica_failover;

GRANT USAGE ON SCHEMA adm TO replica_failover;
GRANT SELECT,UPDATE ON TABLE adm."tCommandsQueue" TO replica_failover;
GRANT SELECT ON TABLE adm."vCommandsQueue" TO replica_failover;

GRANT USAGE ON SCHEMA cues TO replica_failover;
GRANT SELECT ON TABLE cues."vClassAndTemplateBinds" TO replica_failover;

GRANT USAGE ON SCHEMA mam TO replica_failover;
GRANT SELECT ON TABLE mam."vAssetsCues" TO replica_failover;
GRANT SELECT ON TABLE mam."vAssetsVideoTypes" TO replica_failover;

GRANT USAGE ON SCHEMA archive TO replica_failover;
GRANT SELECT ON TABLE archive."vPlayListResolvedFull" TO replica_failover;
GRANT SELECT ON pl."vItemAttributes" to replica_failover;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_failover;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_failover;

GRANT USAGE ON SCHEMA logs TO replica_failover;
GRANT SELECT,INSERT,DELETE ON TABLE logs."tGame" TO replica_failover;
GRANT USAGE ON SEQUENCE logs."tGame_id_seq" TO replica_failover;



-----------------------------------

GRANT USAGE ON SCHEMA hk TO replica_management;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_management;
GRANT SELECT ON TABLE hk."tErrorScopes" TO replica_management;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_management;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_management;
GRANT ALL ON TABLE hk."tDTEvents" TO replica_management;
GRANT USAGE ON SEQUENCE hk."tUserAttributes_id_seq" TO replica_management;
GRANT SELECT,INSERT ON TABLE hk."tUserAttributes" TO replica_management;
GRANT SELECT ON TABLE hk."tUsers" TO replica_management;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_management;
GRANT ALL ON TABLE hk."tHouseKeeping" TO replica_management;

GRANT USAGE ON SCHEMA mam TO replica_management;
GRANT SELECT ON TABLE mam."tAssets" TO replica_management;
GRANT ALL ON TABLE mam."tAssetAttributes" TO replica_management;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_management;
GRANT ALL ON TABLE mam."tCustomValues" TO replica_management;
GRANT USAGE ON SEQUENCE mam."tCustomValues_id_seq" TO replica_management;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_management;
GRANT SELECT ON TABLE mam."vAssetsFiles" TO replica_management;
GRANT SELECT ON TABLE mam."vAssetsPersons" TO replica_management;
GRANT SELECT ON TABLE mam."vAssetsVideoTypes" TO replica_management;
GRANT SELECT ON TABLE mam."vAssetsClasses" TO replica_management;
GRANT SELECT ON TABLE mam."vPersons" TO replica_management;
GRANT USAGE ON SEQUENCE mam."tCategoryValues_id_seq" TO replica_management;
GRANT SELECT ON TABLE mam."tCategoryValues" TO replica_management;


GRANT USAGE ON SCHEMA pl TO replica_management;
GRANT SELECT ON TABLE pl."tClasses" TO replica_management;
GRANT ALL ON TABLE pl."tItems" TO replica_management;
GRANT USAGE ON SEQUENCE pl."tItems_id_seq" TO replica_management;
GRANT ALL ON TABLE pl."tItemAttributes" TO replica_management;
GRANT USAGE ON SEQUENCE pl."tItemAttributes_id_seq" TO replica_management;
GRANT SELECT ON TABLE pl."tItemDTEventTypes" TO replica_management;
GRANT USAGE ON SEQUENCE pl."tItemDTEvents_id_seq" TO replica_management;
GRANT ALL ON TABLE pl."tItemDTEvents" TO replica_management;
GRANT SELECT ON TABLE pl."tPlugs" TO replica_management;
GRANT SELECT,DELETE ON TABLE pl."tItemsCached" TO replica_management;
GRANT SELECT, UPDATE ON TABLE pl."tPlugOffsets" TO replica_management;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_management;
GRANT SELECT ON TABLE pl."vPlayListResolvedOrdered" TO replica_management;
GRANT SELECT ON TABLE pl."vItemTimings" TO replica_management;
GRANT SELECT ON TABLE pl."vTimedItemLast" TO replica_management;
GRANT SELECT ON TABLE pl."vTimedItemCurrent" TO replica_management;
GRANT SELECT ON TABLE pl."vItemsLeft" TO replica_management;
GRANT SELECT ON TABLE pl."vPlaylistFramesQty" TO replica_management;

GRANT USAGE ON SCHEMA media TO replica_management;
GRANT SELECT,INSERT ON TABLE media."tFiles" TO replica_management;
GRANT USAGE ON SEQUENCE media."tFiles_id_seq" TO replica_management;
GRANT SELECT,INSERT,DELETE ON TABLE media."tFileAttributes" TO replica_management;
GRANT USAGE ON SEQUENCE media."tFileAttributes_id_seq" TO replica_management;
GRANT SELECT ON TABLE media."vFiles" TO replica_management;
GRANT SELECT ON TABLE media."vStorages" TO replica_management;

GRANT ALL ON SCHEMA archive TO replica_management;
GRANT USAGE ON SEQUENCE archive."tHouseKeeping_id_seq" TO replica_management;
GRANT ALL ON TABLE archive."tHouseKeeping" TO replica_management;
GRANT SELECT, INSERT ON TABLE archive."pl.tItems" TO replica_management;
GRANT USAGE ON SEQUENCE archive."pl.tItems.HouseKeepings_id_seq" TO replica_management;
GRANT SELECT, INSERT ON TABLE archive."pl.tItems.HouseKeepings" TO replica_management;
GRANT SELECT ON TABLE archive."vHouseKeepingDeleted" TO replica_management;
GRANT SELECT, INSERT ON TABLE archive."ia.tMessages" TO replica_management;
GRANT SELECT ON TABLE archive."vPlayListResolvedFull" TO replica_management;

GRANT USAGE ON SCHEMA adm TO replica_management;
GRANT SELECT ON TABLE adm."vCommandsQueue" TO replica_management;
GRANT UPDATE ON TABLE adm."tCommandsQueue" TO replica_management;
GRANT SELECT ON TABLE adm."tCommandParameters" TO replica_management;

GRANT SELECT ON TABLE adm."tCommands" TO replica_management;
GRANT SELECT,INSERT,UPDATE ON TABLE adm."tCommandsQueue" TO replica_management;
GRANT USAGE ON SEQUENCE adm."tCommandsQueue_id_seq" TO replica_management;
GRANT SELECT,INSERT ON TABLE adm."tCommandParameters" TO replica_management;
GRANT USAGE ON SEQUENCE adm."tCommandParameters_id_seq" TO replica_management;
GRANT SELECT,UPDATE ON TABLE adm."tPreferences" TO replica_management;

GRANT USAGE ON SCHEMA ia TO replica_management;
GRANT SELECT, INSERT ON TABLE archive."ia.tMessages" TO replica_management;
GRANT SELECT, DELETE ON TABLE ia."tMessages" TO replica_management;
GRANT SELECT, DELETE ON TABLE ia."tMessageAttributes" TO replica_management;
GRANT ALL ON TABLE ia."tDTEvents" TO replica_management; --здесь ALL только для того, чтобы делать ANALYZE в клиенте перед получением сообщений
GRANT SELECT, DELETE ON TABLE ia."tTexts" TO replica_management;
GRANT SELECT, DELETE ON TABLE ia."tImages" TO replica_management;
GRANT SELECT, DELETE ON TABLE ia."tBinds" TO replica_management;
GRANT SELECT ON TABLE ia."vMessagesResolved" TO replica_management;

GRANT SELECT ON TABLE pl."vClasses" TO replica_management;
GRANT SELECT ON TABLE mam."vAssetsCustomValues" TO replica_management;

-----------------------------------

GRANT USAGE ON SCHEMA hk TO replica_sync;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_sync;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_sync;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_sync;
GRANT ALL ON TABLE hk."tDTEvents" TO replica_sync;
GRANT USAGE ON SEQUENCE hk."tUserAttributes_id_seq" TO replica_sync;
GRANT SELECT,INSERT ON TABLE hk."tUserAttributes" TO replica_sync;
GRANT SELECT ON TABLE hk."tUsers" TO replica_sync;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_sync;
GRANT ALL ON TABLE hk."tHouseKeeping" TO replica_sync;
GRANT SELECT ON TABLE hk."tErrorScopes" TO replica_sync;
GRANT SELECT ON TABLE hk."vUserAccessScopes" TO replica_sync;

GRANT USAGE ON SCHEMA pl TO replica_sync;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_sync;
GRANT SELECT ON TABLE pl."vPlayListResolvedOrdered" TO replica_sync;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_sync;
GRANT USAGE ON SCHEMA mam TO replica_sync;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_sync;
GRANT SELECT ON TABLE mam."vAssetsCustomValues" TO replica_sync;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_sync;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_sync;
GRANT ALL ON TABLE mam."tAssetAttributes" TO replica_sync;

GRANT USAGE ON SCHEMA media TO replica_sync;
GRANT USAGE ON SEQUENCE media."tFiles_id_seq" TO replica_sync;
GRANT ALL ON TABLE media."tFiles" TO replica_sync;
GRANT ALL ON TABLE media."tFileAttributes" TO replica_sync;
GRANT SELECT ON TABLE media."vFiles" TO replica_sync;
GRANT SELECT ON TABLE media."vStorages" TO replica_sync;
GRANT SELECT ON TABLE media."vFilesUnused" TO replica_sync;

GRANT USAGE ON SCHEMA adm TO replica_sync;
GRANT SELECT ON TABLE adm."tCommands" TO replica_sync;
GRANT SELECT,INSERT,UPDATE ON TABLE adm."tCommandsQueue" TO replica_sync;
GRANT USAGE ON SEQUENCE adm."tCommandsQueue_id_seq" TO replica_sync;
GRANT SELECT,INSERT ON TABLE adm."tCommandParameters" TO replica_sync;
GRANT USAGE ON SEQUENCE adm."tCommandParameters_id_seq" TO replica_sync;
GRANT SELECT ON TABLE adm."vCommandsQueue" TO replica_sync;

GRANT ALL ON TABLE pl."tItemsCached" TO replica_sync;
GRANT USAGE ON SEQUENCE pl."tItemsCached_id_seq" TO replica_sync;
GRANT SELECT ON TABLE pl."tItems" TO replica_sync;
GRANT USAGE ON SEQUENCE media."tFileAttributes_id_seq" TO replica_sync;
GRANT SELECT,UPDATE,INSERT,DELETE ON TABLE media."tStrings" TO replica_sync;
GRANT USAGE ON SEQUENCE media."tStrings_id_seq" TO replica_sync;
GRANT USAGE ON SEQUENCE media."tDates_id_seq" TO replica_sync;
GRANT ALL ON TABLE media."tDates" TO replica_sync;

GRANT USAGE ON SCHEMA archive TO replica_sync;
GRANT SELECT ON TABLE archive."pl.tItems" TO replica_sync;

-----------------------------------
GRANT USAGE ON SCHEMA ingest TO replica_ingest;
GRANT SELECT,INSERT ON TABLE ingest."tItems" TO replica_ingest;
GRANT USAGE ON SEQUENCE ingest."tItems_id_seq" TO replica_ingest;
GRANT SELECT,INSERT ON TABLE ingest."tItemAttributes" TO replica_ingest;
GRANT USAGE ON SEQUENCE ingest."tItemAttributes_id_seq" TO replica_ingest;
GRANT SELECT,INSERT ON TABLE ingest."tSongs" TO replica_ingest;
GRANT USAGE ON SEQUENCE ingest."tSongs_id_seq" TO replica_ingest;

GRANT USAGE ON SCHEMA mam TO replica_ingest;
GRANT SELECT ON TABLE mam."vPersons" TO replica_ingest;
GRANT SELECT,INSERT ON TABLE mam."tPersons" TO replica_ingest;
GRANT USAGE ON SEQUENCE mam."tPersons_id_seq" TO replica_ingest;

GRANT USAGE ON SCHEMA hk TO replica_ingest;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_ingest;
GRANT SELECT ON TABLE hk."tUsers" TO replica_ingest;
GRANT SELECT ON TABLE hk."tWebPages" TO replica_ingest;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_ingest;
GRANT SELECT,INSERT ON TABLE hk."tDTEvents" TO replica_ingest;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_ingest;
GRANT SELECT,INSERT ON TABLE hk."tHouseKeeping" TO replica_ingest;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_ingest;

GRANT USAGE ON SCHEMA adm TO replica_ingest;
GRANT SELECT ON TABLE adm."tTransliteration" TO replica_ingest;

GRANT SELECT ON TABLE mam."vAssetsPersons" TO replica_ingest_full;
GRANT UPDATE, DELETE ON TABLE mam."tPersons" TO replica_ingest_full;

GRANT USAGE ON SCHEMA media TO replica_ingest;
GRANT SELECT ON TABLE media."vStorages" TO replica_ingest;
GRANT SELECT ON TABLE media."vFiles" TO replica_ingest;
GRANT SELECT ON TABLE media."tFileAttributes" TO replica_ingest;
GRANT SELECT,UPDATE,INSERT ON TABLE media."tFiles" TO replica_ingest_full;
GRANT USAGE ON SEQUENCE media."tFiles_id_seq" TO replica_ingest_full;
GRANT SELECT,UPDATE,INSERT,DELETE ON TABLE media."tFileAttributes" TO replica_ingest_full;
GRANT USAGE ON SEQUENCE media."tFileAttributes_id_seq" TO replica_ingest_full;
GRANT SELECT,UPDATE,INSERT,DELETE ON TABLE media."tStrings" TO replica_ingest_full;
GRANT USAGE ON SEQUENCE media."tStrings_id_seq" TO replica_ingest_full;
GRANT USAGE ON SEQUENCE media."tDates_id_seq" TO replica_ingest_full;
GRANT ALL ON TABLE media."tDates" TO replica_ingest_full;


-----------------------------------

GRANT USAGE ON SCHEMA adm TO replica_adm;
GRANT SELECT, INSERT ON TABLE adm."tCommandsQueue" TO replica_adm;
GRANT USAGE ON SEQUENCE adm."tCommandsQueue_id_seq" TO replica_adm;
GRANT SELECT ON TABLE adm."tCommands" TO replica_adm;
GRANT SELECT ON TABLE adm."tCommandStatuses" TO replica_adm;
GRANT SELECT, INSERT ON TABLE adm."tCommandParameters" TO replica_adm;
GRANT USAGE ON SEQUENCE adm."tCommandParameters_id_seq" TO replica_adm;
GRANT USAGE ON SCHEMA hk TO replica_adm;
GRANT SELECT ON TABLE hk."tUsers" TO replica_adm;
GRANT SELECT ON TABLE adm."vPreferences" TO replica_adm;

-----------------------------------
GRANT USAGE ON SCHEMA pl TO replica_playlist;
GRANT SELECT ON TABLE pl."tClasses" TO replica_playlist;
GRANT SELECT ON TABLE pl."tStatuses" TO replica_playlist;
GRANT SELECT ON TABLE pl."vClasses" TO replica_playlist;
GRANT DELETE, INSERT, UPDATE ON TABLE pl."tItems" TO replica_playlist_full;
GRANT SELECT ON TABLE pl."tItems" TO replica_playlist;
GRANT USAGE ON SEQUENCE pl."tItems_id_seq" TO replica_playlist;
GRANT DELETE, INSERT, UPDATE ON TABLE pl."tItemAttributes" TO replica_playlist_full;
GRANT SELECT ON TABLE pl."tItemAttributes" TO replica_playlist;
GRANT USAGE ON SEQUENCE pl."tItemAttributes_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE pl."tItemDTEventTypes" TO replica_playlist;
GRANT DELETE, INSERT, UPDATE ON TABLE pl."tItemDTEvents" TO replica_playlist_full;
GRANT SELECT ON TABLE pl."tItemDTEvents" TO replica_playlist;
GRANT USAGE ON SEQUENCE pl."tItemDTEvents_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE pl."vItemDTEvents" TO replica_playlist;
GRANT DELETE, INSERT, UPDATE ON TABLE pl."tPlugs" TO replica_playlist_full;
GRANT SELECT ON TABLE pl."tPlugs" TO replica_playlist;
GRANT USAGE ON SEQUENCE pl."tPlugs_id_seq" TO replica_playlist;
GRANT DELETE, INSERT, UPDATE ON TABLE pl."tPlugOffsets" TO replica_playlist_full;
GRANT SELECT ON TABLE pl."tPlugOffsets" TO replica_playlist;
GRANT USAGE ON SEQUENCE pl."tPlugOffsets_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE pl."vPlayListResolved" TO replica_playlist;
GRANT SELECT ON TABLE pl."vPlayListResolvedOrdered" TO replica_playlist;
GRANT SELECT ON TABLE pl."vItemTimings" TO replica_playlist;
GRANT SELECT ON TABLE pl."vTimedItemLast" TO replica_playlist;
GRANT SELECT ON TABLE pl."vTimedItemCurrent" TO replica_playlist;
GRANT SELECT ON TABLE pl."vItemsLeft" TO replica_playlist;
GRANT SELECT ON TABLE pl."vPlaylistFramesQty" TO replica_playlist;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_playlist;
GRANT USAGE ON SEQUENCE pl."tItemsCached_id_seq" TO replica_playlist;
GRANT DELETE, INSERT, UPDATE ON TABLE pl."tItemsCached" TO replica_playlist_full;
GRANT SELECT ON TABLE pl."tItemsCached" TO replica_playlist;
GRANT SELECT ON TABLE pl."vItemsInserted" TO replica_playlist;

GRANT USAGE ON SCHEMA mam TO replica_playlist;
GRANT USAGE ON SEQUENCE mam."tCategoryValues_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE mam."tCategoryValues" TO replica_playlist;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_playlist;
GRANT USAGE ON SEQUENCE mam."tVideoTypes_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE mam."tVideoTypes" TO replica_playlist;
GRANT SELECT ON TABLE mam."vAssetsClasses" TO replica_playlist;
GRANT SELECT ON TABLE mam."tAssets" TO replica_playlist;

GRANT USAGE ON SCHEMA archive TO replica_playlist;
GRANT SELECT ON TABLE archive."vPlayListResolvedFull" TO replica_playlist;

GRANT USAGE ON SCHEMA hk TO replica_playlist;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_playlist;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_playlist;
GRANT SELECT ON TABLE hk."tUsers" TO replica_playlist;
GRANT SELECT ON TABLE hk."tHouseKeeping" TO replica_playlist;
GRANT INSERT, DELETE, UPDATE  ON TABLE hk."tHouseKeeping" TO replica_playlist_full;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_playlist;
GRANT SELECT ON TABLE hk."tDTEvents" TO replica_playlist;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_playlist;
GRANT INSERT, DELETE, UPDATE ON TABLE hk."tDTEvents" TO replica_playlist_full;

GRANT USAGE ON SCHEMA adm TO replica_playlist_full;
GRANT SELECT ON TABLE adm."vPreferences" TO replica_playlist_full;

-----------------------------------
GRANT USAGE ON SCHEMA mam TO replica_assets;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE mam."tAssets" TO replica_assets;
GRANT USAGE ON SEQUENCE mam."tAssets_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_assets;
GRANT DELETE, INSERT, UPDATE ON TABLE mam."tAssetAttributes" TO replica_assets_full;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tVideoTypes" TO replica_assets;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE mam."tVideos" TO replica_assets;
GRANT USAGE ON SEQUENCE mam."tVideos_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tPersonTypes" TO replica_assets;
GRANT SELECT,INSERT ON TABLE mam."tAlbums" TO replica_assets;
GRANT USAGE ON SEQUENCE mam."tAlbums_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tCues" TO replica_assets;
GRANT DELETE, INSERT, UPDATE ON TABLE mam."tCues" TO replica_assets_full;
GRANT USAGE ON SEQUENCE mam."tCues_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tPersons" TO replica_assets;
GRANT DELETE, INSERT, UPDATE ON TABLE mam."tPersons" TO replica_assets_full;
GRANT USAGE ON SEQUENCE mam."tPersons_id_seq" TO replica_assets;

GRANT SELECT,INSERT ON TABLE mam."tCategories" TO replica_assets;
GRANT USAGE ON SEQUENCE mam."tCategories_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tCategoryValues" TO replica_assets;
GRANT DELETE, INSERT, UPDATE ON TABLE mam."tCategoryValues" TO replica_assets_full;
GRANT USAGE ON SEQUENCE mam."tCategoryValues_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tTimeMapTypes" TO replica_assets;
GRANT SELECT,INSERT ON TABLE mam."tTimeMaps" TO replica_assets;
GRANT USAGE ON SEQUENCE mam."tTimeMaps_id_seq" TO replica_assets;
GRANT SELECT,INSERT ON TABLE mam."tTimeMapBinds" TO replica_assets;
GRANT USAGE ON SEQUENCE mam."tTimeMapBinds_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."tAssetTypes" TO replica_assets;

GRANT SELECT ON TABLE mam."tCustomValues" TO replica_assets;
GRANT DELETE, INSERT, UPDATE ON TABLE mam."tCustomValues" TO replica_assets_full;
GRANT USAGE ON SEQUENCE mam."tCustomValues_id_seq" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssets" TO replica_assets;
GRANT SELECT ON TABLE mam."vStyles" TO replica_assets;
GRANT SELECT ON TABLE mam."vRotations" TO replica_assets;
GRANT SELECT ON TABLE mam."vPalettes" TO replica_assets;
GRANT SELECT ON TABLE mam."vSex" TO replica_assets;
GRANT SELECT ON TABLE mam."vSoundLevels" TO replica_assets;
GRANT SELECT ON TABLE mam."vPersons" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsPersons" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsCues" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsStyles" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsRotations" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsPalettes" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsSex" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsSoundLevels" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsCustomValues" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsFiles" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_assets;
GRANT SELECT ON TABLE mam."vAssetsVideoTypes" TO replica_assets;
GRANT SELECT ON TABLE mam."vPersonsCueLast" TO replica_assets;

GRANT USAGE ON SCHEMA media TO replica_assets;
GRANT SELECT ON TABLE media."tFiles" TO replica_assets;
GRANT USAGE ON SEQUENCE media."tFiles_id_seq" TO replica_assets;
GRANT SELECT ON TABLE media."tFileAttributes" TO replica_assets;
GRANT USAGE ON SEQUENCE media."tFileAttributes_id_seq" TO replica_assets;
GRANT SELECT ON TABLE media."vFiles" TO replica_assets;
GRANT SELECT ON TABLE media."vStorages" TO replica_assets;
GRANT SELECT ON TABLE media."vFilesUnused" TO replica_assets;

GRANT USAGE ON SCHEMA pl TO replica_assets;
GRANT SELECT ON TABLE pl."vClasses" TO replica_assets;

GRANT USAGE ON SCHEMA hk TO replica_assets;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_assets;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_assets;
GRANT SELECT ON TABLE hk."tUsers" TO replica_assets;
GRANT SELECT ON TABLE hk."tHouseKeeping" TO replica_assets;
GRANT INSERT, DELETE, UPDATE  ON TABLE hk."tHouseKeeping" TO replica_assets_full;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_assets;
GRANT SELECT ON TABLE hk."tDTEvents" TO replica_assets;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_assets;
GRANT INSERT, DELETE, UPDATE ON TABLE hk."tDTEvents" TO replica_assets_full;

-----------------------------------

GRANT USAGE ON SCHEMA mam TO replica_programs;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_programs;
GRANT SELECT ON TABLE mam."tVideos" TO replica_programs;
GRANT SELECT ON TABLE mam."vAssetsVideoTypes" TO replica_programs;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_programs;
GRANT SELECT ON TABLE mam."vAssetsCustomValues" TO replica_programs;

GRANT USAGE ON SCHEMA pl TO replica_programs;
GRANT SELECT ON TABLE pl."vClasses" TO replica_programs;

GRANT EXECUTE ON FUNCTION mam."fAssetAttributeAdd"(idAssets integer, idRegisteredTables integer, sKey character varying, nValue integer) TO replica_programs_full;
GRANT EXECUTE ON FUNCTION mam."fAssetAttributeSet"(idAssets integer, sKey character varying, nValue integer) TO replica_programs_full;
GRANT EXECUTE ON FUNCTION mam."fAssetFileSet"(idAssets integer, idFiles integer) TO replica_programs_full;

GRANT USAGE ON SCHEMA hk TO replica_programs;
GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_programs;
GRANT SELECT ON TABLE hk."tDTEventTypes" TO replica_programs;
GRANT SELECT ON TABLE hk."tUsers" TO replica_programs;

GRANT SELECT ON TABLE hk."tHouseKeeping" TO replica_programs;
GRANT INSERT, DELETE, UPDATE  ON TABLE hk."tHouseKeeping" TO replica_programs_full;
GRANT USAGE ON SEQUENCE hk."tHouseKeeping_id_seq" TO replica_programs;
GRANT SELECT ON TABLE hk."tDTEvents" TO replica_programs;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_programs;
GRANT INSERT, DELETE, UPDATE ON TABLE hk."tDTEvents" TO replica_programs_full;
GRANT USAGE ON SEQUENCE mam."tAssets_id_seq" TO replica_programs;
GRANT SELECT ON TABLE mam."tAssets" TO replica_programs;
GRANT INSERT, DELETE, UPDATE ON TABLE mam."tAssets" TO replica_programs_full;
GRANT USAGE ON SEQUENCE mam."tAssetAttributes_id_seq" TO replica_programs;
GRANT SELECT ON TABLE mam."tAssetAttributes" TO replica_programs;
GRANT INSERT, DELETE, UPDATE ON TABLE mam."tAssetAttributes" TO replica_programs_full;
GRANT USAGE ON SEQUENCE mam."tCustomValues_id_seq" TO replica_programs;
GRANT SELECT ON TABLE mam."tCustomValues" TO replica_programs;
GRANT INSERT, DELETE, UPDATE ON TABLE mam."tCustomValues" TO replica_programs_full;
GRANT SELECT ON TABLE mam."tCues" TO replica_programs;
GRANT DELETE, INSERT, UPDATE ON TABLE mam."tCues" TO replica_programs_full;
GRANT USAGE ON SEQUENCE mam."tCues_id_seq" TO replica_programs;

GRANT USAGE ON SCHEMA cues TO replica_programs;
GRANT USAGE ON SEQUENCE cues."tChatInOuts_id_seq" TO replica_programs;
GRANT SELECT ON TABLE cues."tChatInOuts" TO replica_programs;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE cues."tChatInOuts" TO replica_programs_full;


-----------------------------------

GRANT USAGE ON SCHEMA ia TO replica_ia;
GRANT SELECT ON TABLE ia."tMessages" TO replica_ia;
GRANT SELECT ON TABLE ia."tMessageAttributes" TO replica_ia;
GRANT SELECT ON TABLE ia."tDTEventTypes" TO replica_ia;
GRANT SELECT ON TABLE ia."tDTEvents" TO replica_ia;
GRANT SELECT ON TABLE ia."tGateways" TO replica_ia;
GRANT SELECT ON TABLE ia."tGatewayIPs" TO replica_ia;
GRANT SELECT ON TABLE ia."tNumbers" TO replica_ia;
GRANT SELECT ON TABLE ia."tTexts" TO replica_ia;
GRANT SELECT ON TABLE ia."tImages" TO replica_ia;
GRANT SELECT ON TABLE ia."tBinds" TO replica_ia;
GRANT SELECT ON TABLE ia."vMessagesResolved" TO replica_ia;
GRANT SELECT ON TABLE ia."vMessageGateways" TO replica_ia;
GRANT SELECT ON TABLE ia."tVJMessages" TO replica_ia;

GRANT USAGE ON SCHEMA cues TO replica_ia;
GRANT USAGE ON SEQUENCE cues."tChatInOuts_id_seq" TO replica_ia;
GRANT SELECT ON TABLE cues."tChatInOuts" TO replica_ia;

GRANT USAGE ON SCHEMA scr TO replica_ia;
GRANT SELECT ON TABLE scr."tShifts" TO replica_ia;
GRANT SELECT ON TABLE scr."vShiftCurrent" TO replica_ia;

GRANT SELECT ON TABLE hk."tRegisteredTables" TO replica_ia;

GRANT USAGE ON SCHEMA mam TO replica_ia;
GRANT SELECT ON TABLE mam."vAssetsResolved" TO replica_ia;
GRANT SELECT ON TABLE mam."vAssetsPersons" TO replica_ia;

GRANT USAGE ON SCHEMA pl TO replica_ia;
GRANT SELECT ON TABLE pl."vComingUp" TO replica_ia;
GRANT USAGE ON SCHEMA logs TO replica_ia;
GRANT SELECT ON TABLE logs."tSCR" TO replica_ia;

GRANT SELECT ON TABLE mam."vPersons" TO replica_ia;

GRANT ALL ON TABLE cues."tChatInOuts" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tMessages" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tMessages_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tMessageAttributes" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tMessageAttributes_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tDTEventTypes" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tDTEventTypes_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tDTEvents" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tDTEvents_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tGatewayIPs" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tGatewayIPs_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tNumbers" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tNumbers_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tTexts" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tTexts_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tImages" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tImages_id_seq" TO replica_ia_full;
GRANT INSERT ON TABLE ia."tBinds" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tBinds_id_seq" TO replica_ia_full;

GRANT INSERT,UPDATE ON TABLE ia."tVJMessages" TO replica_ia_full;
GRANT USAGE ON SEQUENCE ia."tVJMessages_id_seq" TO replica_ia_full;

-----------------------------------

GRANT USAGE ON SCHEMA archive TO replica_stat;
GRANT SELECT ON TABLE archive."pl.tItems" TO replica_stat;
GRANT SELECT ON TABLE archive."vPlayListResolvedFull" TO replica_stat;
GRANT SELECT ON TABLE archive."vPlayListWithAssetsResolvedFull" TO replica_stat;
GRANT USAGE ON SCHEMA logs TO replica_stat;
GRANT USAGE ON SCHEMA scr TO replica_stat;
GRANT SELECT ON TABLE logs."tSCR" TO replica_stat;
GRANT SELECT ON TABLE scr."vShifts" TO replica_stat;
GRANT USAGE ON SCHEMA ia TO replica_stat;
GRANT SELECT ON TABLE ia."vMessagesResolved" TO replica_stat;
GRANT SELECT ON TABLE archive."ia.tMessages" TO replica_stat;

-----------------------------------

GRANT USAGE ON SCHEMA mam TO replica_templates;
GRANT SELECT,UPDATE ON TABLE mam."tMacros" TO replica_templates_full;
GRANT SELECT ON TABLE mam."vMacros" TO replica_templates;

GRANT USAGE ON SCHEMA cues TO replica_templates;
GRANT SELECT ON TABLE cues."tTemplates" TO replica_templates;
GRANT SELECT ON TABLE hk."tHouseKeeping" TO replica_templates;
GRANT INSERT, DELETE, UPDATE  ON TABLE hk."tHouseKeeping" TO replica_templates_full;

GRANT SELECT ON TABLE cues."vClassAndTemplateBinds" TO replica_templates;
GRANT SELECT ON TABLE cues."tClassAndTemplateBinds" TO replica_templates;
GRANT USAGE ON SEQUENCE cues."tClassAndTemplateBinds_id_seq" TO replica_templates_full;
GRANT INSERT, DELETE, UPDATE  ON TABLE cues."tClassAndTemplateBinds" TO replica_templates_full;

GRANT SELECT ON TABLE cues."tTemplatesSchedule" TO replica_templates;
GRANT USAGE ON SEQUENCE cues."tTemplatesSchedule_id_seq" TO replica_templates_full;
GRANT INSERT, DELETE, UPDATE  ON TABLE cues."tTemplatesSchedule" TO replica_templates_full;

GRANT SELECT ON TABLE cues."tDictionary" TO replica_templates;
GRANT USAGE ON SEQUENCE cues."tDictionary_id_seq" TO replica_templates_full;
GRANT INSERT, DELETE, UPDATE  ON TABLE cues."tDictionary" TO replica_templates_full;
GRANT SELECT ON TABLE adm."vCommandsQueue" TO replica_templates;

GRANT USAGE ON SEQUENCE cues."tChatInOuts_id_seq" TO replica_templates;
GRANT SELECT ON TABLE cues."tChatInOuts" TO replica_templates;
GRANT SELECT, INSERT, DELETE ON TABLE cues."tChatInOuts" TO replica_templates_full;


GRANT SELECT,INSERT ON TABLE hk."tDTEvents" TO replica_templates;
GRANT USAGE ON SEQUENCE hk."tDTEvents_id_seq" TO replica_templates;




GRANT USAGE ON SEQUENCE cues."tDictionary_id_seq" TO replica_templates_full;
GRANT ALL ON TABLE cues."tDictionary" TO replica_templates_full;
GRANT USAGE ON SEQUENCE cues."tBindTypes_id_seq" TO replica_templates_full;
GRANT ALL ON TABLE cues."tBindTypes" TO replica_templates_full;
GRANT USAGE ON SEQUENCE cues."tBinds_id_seq" TO replica_templates_full;
GRANT ALL ON TABLE cues."tBinds" TO replica_templates_full;
GRANT USAGE ON SEQUENCE cues."tStrings_id_seq" TO replica_templates_full;
GRANT ALL ON TABLE cues."tStrings" TO replica_templates_full;
GRANT USAGE ON SEQUENCE cues."tTimestamps_id_seq" TO replica_templates_full;
GRANT ALL ON TABLE cues."tTimestamps" TO replica_templates_full;
GRANT USAGE ON SEQUENCE cues."tPlugins_id_seq" TO replica_templates_full;
GRANT ALL ON TABLE cues."tPlugins" TO replica_templates_full;


GRANT ALL ON TABLE cues."vBinds" TO replica_templates_full;
GRANT ALL ON TABLE cues."vBindStrings" TO replica_templates_full;
GRANT ALL ON TABLE cues."vBindTimestamps" TO replica_templates_full;
GRANT ALL ON TABLE cues."vPluginPlaylistItems" TO replica_templates_full;
GRANT ALL ON TABLE cues."vPluginPlaylists" TO replica_templates_full;

-------------------------------------immediate pl

GRANT USAGE ON SEQUENCE cues."tDictionary_id_seq" TO replica_playlist_full;
GRANT ALL ON TABLE cues."tDictionary" TO replica_playlist_full;
GRANT USAGE ON SEQUENCE cues."tBindTypes_id_seq" TO replica_playlist_full;
GRANT ALL ON TABLE cues."tBindTypes" TO replica_playlist_full;
GRANT USAGE ON SEQUENCE cues."tBinds_id_seq" TO replica_playlist_full;
GRANT ALL ON TABLE cues."tBinds" TO replica_playlist_full;
GRANT USAGE ON SEQUENCE cues."tStrings_id_seq" TO replica_playlist_full;
GRANT ALL ON TABLE cues."tStrings" TO replica_playlist_full;
GRANT USAGE ON SEQUENCE cues."tTimestamps_id_seq" TO replica_playlist_full;
GRANT ALL ON TABLE cues."tTimestamps" TO replica_playlist_full;
GRANT USAGE ON SEQUENCE cues."tPlugins_id_seq" TO replica_playlist_full;
GRANT ALL ON TABLE cues."tPlugins" TO replica_playlist_full;


GRANT ALL ON TABLE cues."vBinds" TO replica_playlist_full;
GRANT ALL ON TABLE cues."vBindStrings" TO replica_playlist_full;
GRANT ALL ON TABLE cues."vBindTimestamps" TO replica_playlist_full;
GRANT ALL ON TABLE cues."vPluginPlaylistItems" TO replica_playlist_full;
GRANT ALL ON TABLE cues."vPluginPlaylists" TO replica_playlist_full;

GRANT SELECT ON TABLE cues."tTemplates" TO replica_playlist_full;