/*
CREATE OR REPLACE VIEW grid."vGrids" AS
	SELECT ua."idUsers" as id, 
			arss.id as "idAccessRoles", arss."sAccessRoleName",
			arss."idAccessScopes", arss."idAccessScopeParent", arss."sAccessScopeName", arss."sAccessScopeNameQualified",
			arss."idAccessPermissions", arss."bCreate", arss."bUpdate", arss."bDelete"
		FROM grid."tGrids" gs, grid."tGridDays" ds, grid."tClocks" cs, grid."tClockBinds" cbs
		WHERE gs.id=cds."idGrids" AND cs.id=cds."idClocks" AND ds.id=cds."idGridDays";
*/