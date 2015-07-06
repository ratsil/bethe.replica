CREATE OR REPLACE VIEW adm."vCommandsQueue" AS
	SELECT cq.id, cq."idCommands", cq."idCommandStatuses", cq."idUsers", c."sName" as "sCommandName", cs."sName" as "sCommandStatus", u."sUsername" , dt
		FROM adm."tCommandsQueue" cq, adm."tCommands" c, adm."tCommandStatuses" cs, hk."tUsers" u  
		WHERE cq."idCommands"=c.id AND cq."idCommandStatuses"=cs.id AND cq."idUsers"=u.id;
		
CREATE OR REPLACE VIEW adm."vPreferences" AS
	SELECT p.id, p."idPreferenceClasses", pc."sName" as "sClassName", pc."bActive" as "bClassActive", p."sName", p."sValue", p."bActive"
		FROM adm."tPreferenceClasses" pc, adm."tPreferences" p
		WHERE p."idPreferenceClasses"=pc.id;
