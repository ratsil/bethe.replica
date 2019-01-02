CREATE OR REPLACE VIEW cues."vClassAndTemplateBinds" AS
	WITH RECURSIVE classes(id, "sName", "idParent", "idBinds") AS 
	(
		SELECT id, "sName", "idParent", id FROM pl."tClasses" 
		UNION ALL
		SELECT classes.id,classes."sName", cls."idParent", cls.id FROM pl."tClasses" cls, classes WHERE cls.id=classes."idParent"
	)
	SELECT bnd.id, classes.id as "idClasses", classes."sName" AS "sClassName", bnd."idTemplates", tpl."sName" AS "sTemplateName", tpl."sFile" AS "sTemplateFile", bnd."idRegisteredTables", rt."sSchema" as "sRegisteredTableSchema", rt."sName" as "sRegisteredTableName", rt."dtUpdated" as "dtRegisteredTableUpdated", bnd."sKey", bnd."nValue"
		FROM cues."tClassAndTemplateBinds" bnd LEFT JOIN hk."tRegisteredTables" rt ON bnd."idRegisteredTables" = rt.id, cues."tTemplates" tpl, classes
		WHERE bnd."idTemplates" = tpl.id AND bnd."idClasses" = classes."idBinds";
