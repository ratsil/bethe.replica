CREATE OR REPLACE FUNCTION hk."fUserAccessScopeGet"(idUsers integer, idAccessScopes integer) RETURNS SETOF hk."vUserAccessScopes" AS
	$$
	DECLARE
		idUsersPrecised integer;
	BEGIN
		IF idUsers IS NULL THEN
			SELECT id INTO idUsersPrecised FROM hk."tUsers" WHERE "sUsername" = session_user;
		ELSE
			idUsersPrecised := idUsers;
		END IF;

		IF idAccessScopes IS NULL THEN
			RETURN QUERY SELECT * FROM hk."vUserAccessScopes" WHERE idUsersPrecised = id;
		ELSE
			RETURN QUERY SELECT * FROM hk."vUserAccessScopes" WHERE idUsersPrecised = id AND idAccessScopes = "idAccessScopes";
		END IF;
		RETURN;
	END;
	$$
	LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAccessScopeGet"(sUser name) RETURNS SETOF hk."vUserAccessScopes" AS
	$$
	DECLARE
		idUsers integer;
	BEGIN
		SELECT id INTO idUsers FROM hk."tUsers" WHERE sUser="sUsername";
		RETURN QUERY SELECT * FROM hk."fUserAccessScopeGet"(idUsers, NULL);
		RETURN;
	END;
	$$
	LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fUserAccessScopeGet"() RETURNS SETOF hk."vUserAccessScopes" AS
	$$
	DECLARE
	BEGIN
		RETURN QUERY SELECT * FROM hk."fUserAccessScopeGet"(NULL, NULL);
		RETURN;
	END;
	$$
	LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fAccessRoleScopeGet"(idAccessRoles integer, idAccessScopes integer) RETURNS SETOF hk."vAccessRoleScopes" AS
	$$
	DECLARE
	BEGIN
		IF idAccessScopes IS NULL THEN
			RETURN QUERY SELECT * FROM hk."vAccessRoleScopes" WHERE idAccessRoles = id;
		ELSE
			RETURN QUERY SELECT * FROM hk."vAccessRoleScopes" WHERE idAccessRoles = id AND idAccessScopes = "idAccessScopes";
		END IF;
		RETURN;
	END;
	$$
	LANGUAGE plpgsql VOLATILE;

CREATE OR REPLACE FUNCTION hk."fAccessRoleScopeGet"(sAccessRoleName name) RETURNS SETOF hk."vAccessRoleScopes" AS
	$$
	DECLARE
		idAccessRoles integer;
	BEGIN
		SELECT id INTO idAccessRoles FROM hk."tAccessRoles" WHERE sAccessRoleName = "sName";
		RETURN QUERY SELECT * FROM hk."fUserAccessScopeGet"(idAccessRoles, NULL);
		RETURN;
	END;
	$$
	LANGUAGE plpgsql VOLATILE;