----------------------------------- scr."tTemplates"
	CREATE OR REPLACE FUNCTION scr."fTemplateGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tTemplates';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fTemplateAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tTemplates';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fTemplateNameGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tTemplates';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- scr."tShifts"
	CREATE OR REPLACE FUNCTION scr."fShiftAdd"(idTemplates integer, sSubject text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tShifts';
			aColumns := '{{idTemplates,'||COALESCE(idTemplates::text,'NULL')||'},{sSubject,0}}';
			aColumns[2][2] := sSubject; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fShiftStart"(idShifts integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			nShiftID integer;
		BEGIN
			SELECT id INTO nShiftID FROM scr."tShifts" WHERE "dtStart" is NULL AND id=idShifts;
			stRetVal."nValue" := idShifts;
			stRetVal."bValue" := false;
			IF idShifts = nShiftID THEN
				UPDATE scr."tShifts" SET "dtStart" = now() WHERE id = idShifts;
				stRetVal."bValue" := true;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql;

	CREATE OR REPLACE FUNCTION scr."fShiftStop"(idShifts integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			nShiftID integer;
		BEGIN
			SELECT id INTO nShiftID FROM scr."vShiftCurrent";
			stRetVal."nValue" := idShifts;
			stRetVal."bValue" := false;
			IF idShifts = nShiftID THEN
				UPDATE scr."tShifts" SET "dtStop" = now() WHERE id = idShifts;
				stRetVal."bValue" := true;
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql;
----------------------------------- scr."tPlaques"
	CREATE OR REPLACE FUNCTION scr."fPlaqueAdd"(idPresets integer, sName text, sFirst text, sSecond text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tPlaques';
			aColumns := '{{idTemplates,'||COALESCE(idPresets::text,'NULL')||'},{sName,0},{sFirstLine,0},{sSecondLine,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			aColumns[3][2] := sFirst; --чтобы не морочиться с экранированием
			aColumns[4][2] := sSecond; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- scr."tMessagesMarks"
	CREATE OR REPLACE FUNCTION scr."fMessageMarkGet"(idMessages integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tMessagesMarks';
			aColumns := '{{idMessages,'||idMessages::text||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fMessageMarkAdd"(idMessages integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tMessagesMarks';
			aColumns := '{{idMessages,'||idMessages::text||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fMessageMarkRemove"(idMessages integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tMessagesMarks';
			aColumns := '{{idMessages,'||idMessages::text||'}}';
			stRetVal := "fTableRemove"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- scr."tAnnouncements"
	CREATE OR REPLACE FUNCTION scr."fAnnouncementAdd"(idShifts integer, sText text) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tAnnouncements';
			aColumns := '{{idShifts,'||COALESCE(idShifts::text,'NULL')||'},{sText,0}}';
			aColumns[2][2] := sText; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fAnnouncementUpdate"(idAnnouncements integer, sText text) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			UPDATE scr."tAnnouncements" SET "sText"=sText WHERE id = idAnnouncements;
			stRetVal."nValue" := idAnnouncements;
			stRetVal."bValue" := true;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fAnnouncementRemove"(idAnnouncements integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tAnnouncements';
			aColumns := '{{id,'||idAnnouncements::text||'}}';
			stRetVal := "fTableRemove"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- scr."tCueTypes"
	CREATE OR REPLACE FUNCTION scr."fCueTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tCueTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fCueTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tCueTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION scr."fCueTypeNameGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='scr';
			stTable."sName":='tCueTypes';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- scr."tCues"
