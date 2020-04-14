CREATE OR REPLACE VIEW media."vStorages" as
	SELECT s.id,
			t.id AS "idStorageTypes",
			t."sName" AS "sTypeName",
			s."sPath",
			s."sName",
			s."bEnabled",
			s."idVideoTypes",
			v."sName" AS "sVideoTypesName"
		FROM media."tStorages" s
			LEFT JOIN mam."tVideoTypes" v ON s."idVideoTypes" = v.id,
			media."tStorageTypes" t
		WHERE s."idStorageTypes" = t.id;

CREATE OR REPLACE VIEW media."vFileErrors" as
	SELECT fa."idFiles" as id, es."eError"
		FROM media."tFileAttributes" fa, hk."tRegisteredTables" rt_errors, hk."tErrorScopes" es, hk."tRegisteredTables" rt_files
		WHERE 'error'="sKey" AND rt_errors.id = fa."idRegisteredTables"
			AND rt_errors."sSchema"='hk' AND rt_errors."sName"='tErrorScopes' AND fa."nValue" = es.id
			AND rt_files."sSchema"='media' AND rt_files."sName"='tFiles' AND es."idRegisteredTables"=rt_files.id;

CREATE OR REPLACE VIEW media."vFiles" as
	SELECT f.id,
			s.id AS "idStorages",
			s."idStorageTypes",
			s."sTypeName" AS "sStorageTypeName",
			s."sPath",
			s."sName" AS "sStorageName",
			s."bEnabled" AS "bStorageEnabled",
			f."sFilename",
			f."dtLastFileEvent",
			fe."eError",
			fa."nValue" AS "nStatus",
			fa2."nValue" AS "nAge"
		FROM media."vStorages" s,
			media."tFiles" f
			LEFT JOIN media."vFileErrors" fe ON f.id = fe.id
			LEFT JOIN media."tFileAttributes" fa ON f.id = fa."idFiles" AND fa."sKey"::text = 'status'::text
			LEFT JOIN media."tFileAttributes" fa2 ON f.id = fa2."idFiles" AND fa2."sKey"::text = 'age'::text
		WHERE f."idStorages" = s.id;
