CREATE OR REPLACE VIEW hk."vRegisteredTables" AS
	SELECT *, "sSchema"||'.'||"sName" as "sNameFull"
		FROM hk."tRegisteredTables";

CREATE OR REPLACE VIEW hk."vAccessScopesNamesQualified" AS
	SELECT id, array_to_string(array_agg("sName"), '.') as "sNameQualified" FROM (
		WITH RECURSIVE scopes(id, "idParent", "sName") AS 
		(
			SELECT id, "idParent", "sName" FROM hk."tAccessScopes"
			UNION ALL
			SELECT scopes.id, acs."idParent", acs."sName" FROM hk."tAccessScopes" acs, scopes WHERE acs.id=scopes."idParent"
		) 
		SELECT id, "idParent", "sName" FROM scopes ORDER BY id, "idParent" NULLS FIRST
	) sub GROUP BY id;

CREATE OR REPLACE VIEW hk."vAccessRoleScopes" AS
	SELECT ars.id, ars."sName" as "sAccessRoleName",
			acs.id as "idAccessScopes", acs."idParent" as "idAccessScopeParent", acs."sName" as "sAccessScopeName", asnq."sNameQualified" as "sAccessScopeNameQualified",
			acp.id as "idAccessPermissions", acp."bCreate", acp."bUpdate", acp."bDelete"
		FROM hk."tAccessRoles" ars, hk."tAccessScopes" acs, hk."tAccessPermissions" acp, hk."vAccessScopesNamesQualified" asnq
		WHERE acp."idAccessRoles" = ars.id AND acp."idAccessScopes" = acs.id AND asnq.id = acs.id;

CREATE OR REPLACE VIEW hk."vUserAccessScopes" AS
	SELECT ua."idUsers" as id, 
			arss.id as "idAccessRoles", arss."sAccessRoleName",
			arss."idAccessScopes", arss."idAccessScopeParent", arss."sAccessScopeName", arss."sAccessScopeNameQualified",
			arss."idAccessPermissions", arss."bCreate", arss."bUpdate", arss."bDelete"
		FROM hk."tUserAttributes" ua, hk."tRegisteredTables" rt, hk."vAccessRoleScopes" arss
		WHERE ua."sKey"='access_role' AND 'hk' = rt."sSchema" AND 'tAccessRoles' = rt."sName" AND rt.id = ua."idRegisteredTables" AND ua."nValue"=arss.id;