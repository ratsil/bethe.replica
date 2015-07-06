/********************************** scr."tTemplates" **************************************/
/********************************** scr."tShifts" *****************************************/
CREATE OR REPLACE VIEW scr."vShifts" as
	SELECT s.*, t."sName" as "sTemplateName"
		FROM scr."tShifts" s, scr."tTemplates" t
		WHERE s."idTemplates" = t.id;
CREATE OR REPLACE VIEW scr."vShiftCurrent" as
	SELECT *
		FROM scr."vShifts"
		WHERE "dtStart" IS NOT NULL AND "dtStop" IS NULL
		ORDER BY dt DESC LIMIT 1;
/********************************** scr."tPlaques" ****************************************/
CREATE OR REPLACE VIEW scr."vPlaques" as
	SELECT pq.*, pr.id AS "idPresets", pr."sName" AS "sPresetName"
		FROM scr."tPlaques" pq, scr."tTemplates" pr
		WHERE pq."idTemplates"=pr.id;
/********************************** scr."tMessagesMarks" **********************************/
CREATE OR REPLACE VIEW scr."vMessagesQueue" as
	SELECT mr.*, CASE WHEN mm."idMessages" IS NULL THEN false ELSE true END as "bMark"
		FROM ia."vMessagesResolved" mr
			LEFT JOIN scr."tMessagesMarks" mm ON mm."idMessages"=mr.id
		WHERE mm."idMessages" IS NOT NULL or now()-'4 hour'::interval < mr."dtRegister";
/********************************** scr."tAnnouncements" **********************************/
CREATE OR REPLACE VIEW scr."vAnnouncementsActual" as
	SELECT a.* 
		FROM scr."tAnnouncements" a, scr."vShiftCurrent" sc
		WHERE a."idShifts" IS NULL OR a."idShifts" = sc.id;

/********************************** scr."tCueTypes" ***************************************/
/********************************** scr."tCues" *******************************************/
