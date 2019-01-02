﻿----------------------------------- grid."tGridDays"
	SELECT * FROM grid."fGridDayAdd"('monday');
	SELECT * FROM grid."fGridDayAdd"('tuesday');
	SELECT * FROM grid."fGridDayAdd"('wednesday');
	SELECT * FROM grid."fGridDayAdd"('thursday');
	SELECT * FROM grid."fGridDayAdd"('friday');
	SELECT * FROM grid."fGridDayAdd"('saturday');
	SELECT * FROM grid."fGridDayAdd"('sunday');
----------------------------------- grid."tPropertyTypes"
	SELECT * FROM grid."fPropertyTypeAdd"('select', 'grid', 'tNumericValues');
	SELECT * FROM grid."fPropertyTypeAdd"('string', 'grid', 'tStringValues');
	SELECT * FROM grid."fPropertyTypeAdd"('numeric', 'grid', 'tNumericValues');
	SELECT * FROM grid."fPropertyTypeAdd"('bool');
----------------------------------- grid."tProperties"
	SELECT * FROM grid."fPropertyAdd"('id', 'select', 'fIDsINPs');
	SELECT * FROM grid."fPropertyAdd"('rotation', 'select', 'fRotationsINPs');
	SELECT * FROM grid."fPropertyAdd"('sex', 'select', 'fSexesINPs');
	SELECT * FROM grid."fPropertyAdd"('week_day', 'select', 'fWeekDaysINPs');
	SELECT * FROM grid."fPropertyAdd"('artist', 'select', 'fArtistsINPs');
	SELECT * FROM grid."fPropertyAdd"('frames_qty', 'numeric');
	SELECT * FROM grid."fPropertyAdd"('video_type', 'select', 'fVideoTypesINPs');
	SELECT * FROM grid."fPropertyAdd"('now', 'bool');
	SELECT * FROM grid."fPropertyAdd"('generation', 'bool');
	SELECT * FROM grid."fPropertyAdd"('position', 'numeric');
	SELECT * FROM grid."fPropertyAdd"('day', 'numeric');
	SELECT * FROM grid."fPropertyAdd"('hour', 'numeric');
	SELECT * FROM grid."fPropertyAdd"('minute', 'numeric');
----------------------------------- grid."tFunctionKnotTypes"
	SELECT * FROM grid."fFunctionKnotTypeAdd"('range');
	SELECT * FROM grid."fFunctionKnotTypeAdd"('string');
	SELECT * FROM grid."fFunctionKnotTypeAdd"('numeric');
	SELECT * FROM grid."fFunctionKnotTypeAdd"('bool');
	SELECT * FROM grid."fFunctionKnotTypeAdd"('decision');
	SELECT * FROM grid."fFunctionKnotTypeAdd"('property');
----------------------------------- grid."tFunctions"
	SELECT * FROM grid."fFunctionAdd"('dMakeDecision','decision');
	SELECT * FROM grid."fFunctionAdd"('bFind','bool');
	SELECT * FROM grid."fFunctionAdd"('bEqual','bool');
	SELECT * FROM grid."fFunctionAdd"('bNotEqual','bool');
	SELECT * FROM grid."fFunctionAdd"('bEqualOrLess','bool');
	SELECT * FROM grid."fFunctionAdd"('bEqualOrMore','bool');
	SELECT * FROM grid."fFunctionAdd"('bLess','bool');
	SELECT * FROM grid."fFunctionAdd"('bMore','bool');
	SELECT * FROM grid."fFunctionAdd"('bContains','bool');
	SELECT * FROM grid."fFunctionAdd"('bWithin','bool');
	SELECT * FROM grid."fFunctionAdd"('bLike','bool');
	SELECT * FROM grid."fFunctionAdd"('sGet','string');
	SELECT * FROM grid."fFunctionAdd"('nGet','numeric');
	SELECT * FROM grid."fFunctionAdd"('nCount','numeric');
	SELECT * FROM grid."fFunctionAdd"('nSum','numeric');
	SELECT * FROM grid."fFunctionAdd"('dBlock','decision');
	SELECT * FROM grid."fFunctionAdd"('dAllow','decision');
	SELECT * FROM grid."fFunctionAdd"('dForce','decision');
	SELECT * FROM grid."fFunctionAdd"('dNext','decision');
	SELECT * FROM grid."fFunctionAdd"('dSkeep','decision');
----------------------------------- grid."tFunctionKnotTypeBinds"
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('dMakeDecision','bool',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bFind','property',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bFind','property',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqual','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqual','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bNotEqual','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bNotEqual','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrLess','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrLess','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrMore','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrMore','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bLess','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bLess','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bMore','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bMore','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bContains','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bWithin','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bLike','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('sGet','property',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('sGet','property',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nGet','property',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nGet','property',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nCount','property',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nSum','property',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('dNext','numeric',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('dSkeep','range',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('dMakeDecision','decision',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bFind','range',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bFind','range',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqual','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqual','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bNotEqual','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bNotEqual','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrLess','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrLess','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrMore','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bEqualOrMore','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bLess','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bLess','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bMore','numeric',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bMore','string',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bContains','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bWithin','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bLike','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('sGet','range',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nGet','range',2);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nCount','range',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('nSum','range',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('dMakeDecision','decision',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bFind','string',1);
	SELECT * FROM grid."fFunctionKnotTypeBindAdd"('bFind','numeric',2);

