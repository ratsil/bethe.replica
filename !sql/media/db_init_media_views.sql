CREATE OR REPLACE VIEW media."vStorages" as
	SELECT s.id, t.id as "idStorageTypes", t."sName" as "sTypeName", s."sPath", s."sName" as "sName", s."bEnabled"
		FROM media."tStorages" s, media."tStorageTypes" t
		WHERE s."idStorageTypes"=t.id;

CREATE OR REPLACE VIEW media."vFileErrors" as
	SELECT fa."idFiles" as id, es."eError"
		FROM media."tFileAttributes" fa, hk."tRegisteredTables" rt_errors, hk."tErrorScopes" es, hk."tRegisteredTables" rt_files
		WHERE 'error'="sKey" AND rt_errors.id = fa."idRegisteredTables"
			AND rt_errors."sSchema"='hk' AND rt_errors."sName"='tErrorScopes' AND fa."nValue" = es.id
			AND rt_files."sSchema"='media' AND rt_files."sName"='tFiles' AND es."idRegisteredTables"=rt_files.id;

CREATE OR REPLACE VIEW media."vFiles" as
	SELECT f.id, s.id as "idStorages", s."idStorageTypes", s."sTypeName" as "sStorageTypeName", 
			s."sPath", s."sName" as "sStorageName", s."bEnabled" as "bStorageEnabled", 
			f."sFilename", f."dtLastFileEvent", fe."eError"
		FROM media."vStorages" s, media."tFiles" f
			LEFT JOIN media."vFileErrors" fe ON f.id = fe.id
		WHERE f."idStorages"=s.id;
-- media."vFilesUnused" declaration is in db_init_mam_views.sql because if references to pl and mam schemas
-- CREATE OR REPLACE VIEW media."vFilesUnused"