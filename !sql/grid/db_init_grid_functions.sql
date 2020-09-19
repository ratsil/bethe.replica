----------------------------------- grid."tStringValues"
	CREATE OR REPLACE FUNCTION grid."fStringValueGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tStringValues';
			aColumns := '{{id,'||COALESCE(id::text,'NULL')||'}}';
			stRetVal := "fTableGetText"(stTable, aColumns, 'sValue');
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fStringValueAdd"(sValue character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tStringValues';
			aColumns := '{{sValue,0}}';
			aColumns[1][2] := sValue; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tNumericValues"
	CREATE OR REPLACE FUNCTION grid."fNumericValueGet"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tNumericValues';
			aColumns := '{{id,'||COALESCE(id::text,'NULL')||'}}';
			stRetVal := "fTableGetInt"(stTable, aColumns, 'nValue');
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fNumericValueAdd"(nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tNumericValues';
			aColumns := '{{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tGrids"
	CREATE OR REPLACE FUNCTION grid."fGridGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tGrids';
			stRetVal := "fTableGetID"(stTable, sName);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fGridGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tGrids';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fGridAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tGrids';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tGridDays"
	CREATE OR REPLACE FUNCTION grid."fGridDayGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tGridDays';
			stRetVal := "fTableGetID"(stTable, sName);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fGridDayGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tGridDays';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fGridDayAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tGridDays';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tClocks"
	CREATE OR REPLACE FUNCTION grid."fClockGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tClocks';
			stRetVal := "fTableGetID"(stTable, sName);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fClockGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tClocks';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fClockAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tClocks';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tClockBinds"
	CREATE OR REPLACE FUNCTION grid."fClockBindGet"(idClocks integer, idGrids integer, idGridDays integer, nHour integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tClockBinds';
			aColumns := '{{idClocks,'||COALESCE(idClocks::text,'NULL')||'},{idGrids,'||COALESCE(idGrids::text,'NULL')||'},{idGridDays,'||COALESCE(idGridDays::text,'NULL')||'},{nHour,'||COALESCE(nHour::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fClockBindAdd"(idClocks integer, idGrids integer, idGridDays integer, nHour integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tClockBinds';
			aColumns := '{{idClocks,'||COALESCE(idClocks::text,'NULL')||'},{idGrids,'||COALESCE(idGrids::text,'NULL')||'},{idGridDays,'||COALESCE(idGridDays::text,'NULL')||'},{nHour,'||COALESCE(nHour::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tPropertyTypes"
	CREATE OR REPLACE FUNCTION grid."fPropertyTypeGet"(sName character varying, idRegisteredTables integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tPropertyTypes';
			aColumns := '{{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyTypeGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tPropertyTypes';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tPropertyTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyTypeAdd"(sName character varying, idRegisteredTables integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tPropertyTypes';
			aColumns := '{{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyTypeAdd"(sName character varying, sSchema character varying, sTable character varying) RETURNS int_bool AS
		$$
		DECLARE
			idRegisteredTables integer;
			stRetVal int_bool;
		BEGIN
			stRetVal := hk."fRegisteredTableGet"(sSchema, sTable);
			IF NOT stRetVal."bValue" OR stRetVal."nValue" IS NULL THEN
				RAISE EXCEPTION 'WRONG SCHEMA OR TABLE SPECIFIED! SCHEMA:[%] TABLE:[%] (PROPERTY TYPE NAME:%)', sSchema, sTable, sName;
			END IF;
			idRegisteredTables := stRetVal."nValue";
			stRetVal := grid."fPropertyTypeAdd"(sName, idRegisteredTables);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := grid."fPropertyTypeAdd"(sName, NULL);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tProperties"
	CREATE OR REPLACE FUNCTION grid."fPropertyGet"(sName character varying, idPropertyTypes integer, sINPsFunction character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tProperties';
			aColumns := '{{idPropertyTypes,'||COALESCE(idPropertyTypes::text,'NULL')||'},{sName,0},{sINPsFunction,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			aColumns[3][2] := sINPsFunction; --чтобы не морочиться с экранированием
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tProperties';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyAdd"(sName character varying, idPropertyTypes integer, sINPsFunction character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tProperties';
			aColumns := '{{idPropertyTypes,'||COALESCE(idPropertyTypes::text,'NULL')||'},{sName,0},{sINPsFunction,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			aColumns[3][2] := sINPsFunction; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyAdd"(sName character varying, sPropertyTypeName character varying, sINPsFunction character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idPropertyTypes integer;
		BEGIN
			stRetVal := grid."fPropertyTypeGet"(sPropertyTypeName);
			IF NOT stRetVal."bValue" OR stRetVal."nValue" IS NULL THEN
				RAISE EXCEPTION 'WRONG PROPERTY TYPE SPECIFIED [%] (PROPERTY NAME:%)', sPropertyTypeName, sName;
			END IF;
			idPropertyTypes := stRetVal."nValue";
			stRetVal := grid."fPropertyAdd"(sName, idPropertyTypes, sINPsFunction);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fPropertyAdd"(sName character varying, sPropertyTypeName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := grid."fPropertyAdd"(sName, sPropertyTypeName, NULL);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tRangeMarks"
	CREATE OR REPLACE FUNCTION grid."fRangeMarkGet"(idProperties integer, nValuesTableID integer, nCount integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRangeMarks';
			aColumns := '{{idProperties,'||COALESCE(idProperties::text,'NULL')||'},{nValuesTableID,'||COALESCE(nValuesTableID::text,'NULL')||'},{nCount,'||COALESCE(nCount::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRangeMarkAdd"(idProperties integer, nValuesTableID integer, nCount integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRangeMarks';
			aColumns := '{{idProperties,'||COALESCE(idProperties::text,'NULL')||'},{nValuesTableID,'||COALESCE(nValuesTableID::text,'NULL')||'},{nCount,'||COALESCE(nCount::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRangeMarkAdd"(idProperties integer, nCount integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := grid."fRangeMarkAdd"(idProperties, NULL, nCount);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tRanges"
	CREATE OR REPLACE FUNCTION grid."fRangeGet"(idRangeMarksForReferencePoint integer, idRangeMarksForDuration integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRanges';
			aColumns := '{{idRangeMarksForReferencePoint,'||COALESCE(idRangeMarksForReferencePoint::text,'NULL')||'},{idRangeMarksForDuration,'||COALESCE(idRangeMarksForDuration::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRangeAdd"(idRangeMarksForReferencePoint integer, idRangeMarksForDuration integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRanges';
			aColumns := '{{idRangeMarksForReferencePoint,'||COALESCE(idRangeMarksForReferencePoint::text,'NULL')||'},{idRangeMarksForDuration,'||COALESCE(idRangeMarksForDuration::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tFunctionKnotTypes"
	CREATE OR REPLACE FUNCTION grid."fFunctionKnotTypeGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnotTypes';
			stRetVal := "fTableGetID"(stTable, sName);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionKnotTypeGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnotTypes';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionKnotTypeAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnotTypes';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tFunctions"
	CREATE OR REPLACE FUNCTION grid."fFunctionGet"(sName character varying, idFunctionKnotTypes integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctions';
			aColumns := '{{idFunctionKnotTypes,'||COALESCE(idFunctionKnotTypes::text,'NULL')||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctions';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctions';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionAdd"(sName character varying, idFunctionKnotTypes integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctions';
			aColumns := '{{idFunctionKnotTypes,'||COALESCE(idFunctionKnotTypes::text,'NULL')||'},{sName,0}}';
			aColumns[2][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionAdd"(sName character varying, sFunctionKnotTypeName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
			idFunctionKnotTypes integer;
		BEGIN
			stRetVal := grid."fFunctionKnotTypeGet"(sFunctionKnotTypeName);
			IF NOT stRetVal."bValue" OR stRetVal."nValue" IS NULL THEN
				RAISE EXCEPTION 'WRONG FUNCTIONKNOT TYPE SPECIFIED [%] (FUNCTION NAME:%)', sFunctionKnotTypeName, sName;
			END IF;
			idFunctionKnotTypes := stRetVal."nValue";
			stRetVal := grid."fFunctionAdd"(sName, idFunctionKnotTypes);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tFunctionKnotTypeBinds"
	CREATE OR REPLACE FUNCTION grid."fFunctionKnotTypeBindGet"(idFunctions integer, idFunctionKnotTypes integer, nOverloadsNumber integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnotTypeBinds';
			aColumns := '{{idFunctions,'||COALESCE(idFunctions::text,'NULL')||'},{idFunctionKnotTypes,'||COALESCE(idFunctionKnotTypes::text,'NULL')||'},{nOverloadsNumber,'||COALESCE(nOverloadsNumber::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionKnotTypeBindAdd"(idFunctions integer, idFunctionKnotTypes integer, nOverloadsNumber integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnotTypeBinds';
			aColumns := '{{idFunctions,'||COALESCE(idFunctions::text,'NULL')||'},{idFunctionKnotTypes,'||COALESCE(idFunctionKnotTypes::text,'NULL')||'},{nOverloadsNumber,'||COALESCE(nOverloadsNumber::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionKnotTypeBindAdd"(sFunction character varying, sFunctionKnotTypeName character varying, nOverloadsNumber integer) RETURNS int_bool AS
		$$
		DECLARE
			idFunctions integer;
			idFunctionKnotTypes integer;
			stRetVal int_bool;
		BEGIN
			stRetVal := grid."fFunctionGet"(sFunction);
			IF NOT stRetVal."bValue" OR stRetVal."nValue" IS NULL THEN
				RAISE EXCEPTION 'WRONG FUNCTION NAME SPECIFIED [%]', sFunction;
			END IF;
			idFunctions := stRetVal."nValue";
			
			stRetVal := grid."fFunctionKnotTypeGet"(sFunctionKnotTypeName);
			IF NOT stRetVal."bValue" OR stRetVal."nValue" IS NULL THEN
				RAISE EXCEPTION 'WRONG FUNCTIONKNOT TYPE SPECIFIED [%] (FUNCTION NAME:%)', sFunctionKnotTypeName, sFunction;
			END IF;
			idFunctionKnotTypes := stRetVal."nValue";
			
			stRetVal := grid."fFunctionKnotTypeBindAdd"(idFunctions, idFunctionKnotTypes, nOverloadsNumber);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tFunctionKnots"
	CREATE OR REPLACE FUNCTION grid."fFunctionKnotGet"(idParent integer, idFunctionKnotTypeBinds integer, idRegisteredTables integer, nValuesTableID integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnots';
			aColumns := '{{idParent,'||COALESCE(idParent::text,'NULL')||'},{idFunctionKnotTypeBinds,'||COALESCE(idFunctionKnotTypeBinds::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nValuesTableID,'||COALESCE(nValuesTableID::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fFunctionKnotAdd"(idParent integer, idFunctionKnotTypeBinds integer, idRegisteredTables integer, nValuesTableID integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tFunctionKnots';
			aColumns := '{{idParent,'||COALESCE(idParent::text,'NULL')||'},{idFunctionKnotTypeBinds,'||COALESCE(idFunctionKnotTypeBinds::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nValuesTableID,'||COALESCE(nValuesTableID::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tRulesSets"
	CREATE OR REPLACE FUNCTION grid."fRulesSetGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRulesSets';
			stRetVal := "fTableGetID"(stTable, sName);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRulesSetGet"(id integer) RETURNS text_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal text_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRulesSets';
			stRetVal := "fTableGetName"(stTable, id);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRulesSetAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRulesSets';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --чтобы не морочиться с экранированием
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tRules"
	CREATE OR REPLACE FUNCTION grid."fRuleGet"(idRulesSets integer, nRulesSetPosition integer, idFunctionKnots integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRules';
			aColumns := '{{idRulesSets,'||COALESCE(idRulesSets::text,'NULL')||'},{nRulesSetPosition,'||COALESCE(nRulesSetPosition::text,'NULL')||'},{idFunctionKnots,'||COALESCE(idFunctionKnots::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRuleAdd"(idRulesSets integer, nRulesSetPosition integer, idFunctionKnots integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRules';
			aColumns := '{{idRulesSets,'||COALESCE(idRulesSets::text,'NULL')||'},{nRulesSetPosition,'||COALESCE(nRulesSetPosition::text,'NULL')||'},{idFunctionKnots,'||COALESCE(idFunctionKnots::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- grid."tRulesSetBinds"
	CREATE OR REPLACE FUNCTION grid."fRulesSetBindGet"(idRulesSets integer, idRegisteredTables integer, nTargetTableID integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRulesSetBinds';
			aColumns := '{{idRulesSets,'||COALESCE(idRulesSets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nTargetTableID,'||COALESCE(nTargetTableID::text,'NULL')||'}}';
			stRetVal := "fTableGetID"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;

	CREATE OR REPLACE FUNCTION grid."fRulesSetBindAdd"(idRulesSets integer, idRegisteredTables integer, nTargetTableID integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='grid';
			stTable."sName":='tRulesSetBinds';
			aColumns := '{{idRulesSets,'||COALESCE(idRulesSets::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nTargetTableID,'||COALESCE(nTargetTableID::text,'NULL')||'}}';
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;