----------------------------------- ingest."tItems"
	CREATE OR REPLACE FUNCTION ingest."fItemGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItems';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemGet"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItems';
			aColumns := '{{id,0}}';
			aColumns[1][2] := id; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItems';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAdd"(sName character varying, aArtistsIDs integer[], sSong character varying, nQuality integer, bLocation boolean, bAir boolean, bRemix boolean, bPromo boolean, bCutted boolean, nVersion integer, nFormat integer, bForeign boolean) RETURNS int_bool AS
		$$
		DECLARE
			idItems integer;
			idSongs integer;
			nFlag integer;
			nPersonsQty integer;
			idRegisteredTables integer;
			stRetVal int_bool;
		BEGIN
			IF sName IS NULL OR aArtistsIDs IS NULL OR sSong IS NULL THEN
				RAISE EXCEPTION 'NAME, ARTISTS LIST AND SONG CAN NOT BE NULL';
			END IF;
			nPersonsQty := array_upper(aArtistsIDs, 1);
			IF 1 > nPersonsQty THEN
				RAISE EXCEPTION 'PERSONS ARRAY CAN NOT BE EMPTY';
			END IF;
			stRetVal := ingest."fItemAdd"(sName);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING NEW ITEM';
			END IF;
			idItems := stRetVal."nValue";
			stRetVal := hk."fRegisteredTableGet"('mam','tPersons');
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'THERE IS AN ERROR WHILE GETTING REGISTERED TABLE ID FOR mam."tPersons"';
			END IF;
			idRegisteredTables := stRetVal."nValue";
			FOR nPersonIndx IN 1..nPersonsQty LOOP
				stRetVal := ingest."fItemAttributeAdd"(idItems, idRegisteredTables, 'idPersons', aArtistsIDs[nPersonIndx]::integer);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING PERSON TO THE ITEM';
				END IF;
			END LOOP;
			--------------------------------------------------------------------
			stRetVal := ingest."fSongAdd"(sSong);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING SONG NAME';
			END IF;
			idSongs := stRetVal."nValue";
			stRetVal := ingest."fItemAttributeAdd"(idItems, 'idSongs', idSongs);
			IF NOT stRetVal."bValue" THEN
				RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING SONG ID TO THE ITEM';
			END IF;
			--------------------------------------------------------------------
			IF nQuality IS NOT NULL THEN
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'nQuality', nQuality);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF bLocation IS NOT NULL THEN
				IF bLocation THEN 
					nFlag := 1;
				ELSE
					nFlag := 0;
				END IF; 
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'bLocation', nFlag);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF bAir IS NOT NULL THEN
				IF bAir THEN 
					nFlag := 1;
				ELSE
					nFlag := 0;
				END IF; 
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'bAir', nFlag);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF bRemix IS NOT NULL THEN
				IF bRemix THEN 
					nFlag := 1;
				ELSE
					nFlag := 0;
				END IF; 
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'bRemix', nFlag);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF bPromo IS NOT NULL THEN
				IF bPromo THEN 
					nFlag := 1;
				ELSE
					nFlag := 0;
				END IF; 
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'bPromo', nFlag);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF bCutted IS NOT NULL THEN
				IF bCutted THEN 
					nFlag := 1;
				ELSE
					nFlag := 0;
				END IF; 
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'bCutted', nFlag);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF nVersion IS NOT NULL THEN
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'nVersion', nVersion);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF nFormat IS NOT NULL THEN
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'nFormat', nFormat);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			IF bForeign IS NOT NULL THEN
				IF bForeign THEN 
					nFlag := 1;
				ELSE
					nFlag := 0;
				END IF; 
				stRetVal := ingest."fItemAttributeAdd"(idItems, 'bForeign', nFlag);
				IF NOT stRetVal."bValue" THEN
					RAISE EXCEPTION 'THERE IS AN ERROR WHILE ADDING ONE OF THE ATTRIBUTES TO THE ITEM';
				END IF;
			END IF;
			--------------------------------------------------------------------
			stRetVal."nValue" := idItems;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE plpgsql VOLATILE;
----------------------------------- ingest."tItemAttributes"
	CREATE OR REPLACE FUNCTION ingest."fItemAttributeGet"(idItems integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItemAttributes';
			stRetVal := "fAttributesTableGet"(stTable, 'idItems', idItems, idRegisteredTables, sKey, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeGet"(idItems integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeGet"(idItems integer, idRegisteredTables integer,  sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeGet"(idItems integer,  sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{sKey,0}}';
			aColumns[2][2] := COALESCE(sKey::text,'NULL'); --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeAdd"(idItems integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0},{nValue,'||COALESCE(nValue::text,'NULL')||'}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableAdd"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeAdd"(idItems integer, idRegisteredTables integer, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := ingest."fItemAttributeAdd"(idItems, idRegisteredTables, NULL, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeAdd"(idItems integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := ingest."fItemAttributeAdd"(idItems, NULL, sKey, nValue);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeSet"(idItems integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := ingest."fItemAttributeGet"(idItems, sKey);
			IF stRetVal."bValue" THEN
				UPDATE ingest."tItemAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
			ELSE
				stRetVal := ingest."fItemAttributeAdd"(idItems, sKey, nValue);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeSet"(idItems integer, idRegisteredTables integer, sKey character varying, nValue integer) RETURNS int_bool AS
		$$
		DECLARE
			stRetVal int_bool;
		BEGIN
			stRetVal := ingest."fItemAttributeGet"(idItems, idRegisteredTables, sKey);
			IF stRetVal."bValue" THEN
				UPDATE ingest."tItemAttributes" SET "nValue"=nValue WHERE id=stRetVal."nValue";
			ELSE
				stRetVal := ingest."fItemAttributeAdd"(idItems, idRegisteredTables, sKey, nValue);
			END IF;
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fItemAttributeValueGet"(idItems integer, idRegisteredTables integer,  sKey character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tItemAttributes';
			aColumns := '{{idItems,'||COALESCE(idItems::text,'NULL')||'},{idRegisteredTables,'||COALESCE(idRegisteredTables::text,'NULL')||'},{sKey,0}}';
			aColumns[3][2] := COALESCE(sKey::text,'NULL'); --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			SELECT "nValue" INTO stRetVal."nValue" FROM ingest."tItemAttributes" WHERE id=stRetVal."nValue";
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';
----------------------------------- ingest."tSongs"
	CREATE OR REPLACE FUNCTION ingest."fSongGet"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tSongs';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fSongGet"(id integer) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tSongs';
			aColumns := '{{id,0}}';
			aColumns[1][2] := id; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableGet"(stTable, aColumns);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';

	CREATE OR REPLACE FUNCTION ingest."fSongAdd"(sName character varying) RETURNS int_bool AS
		$$
		DECLARE
			stTable table_name;
			aColumns text[][];
			stRetVal int_bool;
		BEGIN
			stTable."sSchema":='ingest';
			stTable."sName":='tSongs';
			aColumns := '{{sName,0}}';
			aColumns[1][2] := sName; --обходим недоработку switch'а на 386 строке файла arrayfuncs.c
			stRetVal := "fTableAdd"(stTable, aColumns, true);
			RETURN stRetVal;
		END;
		$$
		LANGUAGE 'plpgsql';
