@echo off

set pghost=localhost

set pgdb=replica

rem set PATH=%PATH%;C:\Program Files\PostgreSQL\9.1\bin\;
rem chcp 65001
set PGCLIENTENCODING=utf8
echo START
echo @@START > logs\all.log

set IDB_TGT=drop
echo  	%IDB_TGT%
echo @@@@%IDB_TGT% >> logs\all.log
StreepBOM.exe !sql/common/db_init_%IDB_TGT%.sql | !bin\psql -h %pghost% -U user %pgdb% 1> NUL 2> logs\%IDB_TGT%.log
type logs\%IDB_TGT%.log >> logs\all.log
find /C "ERROR:  " logs\%IDB_TGT%.log > NUL
if not errorlevel 1 goto DROP_ERROR_START
echo 	OK
echo @@@@OK >> logs\all.log
goto DROP_ERROR_END
:DROP_ERROR_START
echo 	ERROR
echo @@@@ERROR >> logs\all.log
:DROP_ERROR_END
echo 	___________________

set IDB_TGT=global
echo  	%IDB_TGT%
echo @@@@%IDB_TGT% >> logs\all.log
StreepBOM.exe !sql/common/db_init_%IDB_TGT%.sql | !bin\psql -h %pghost% -U user %pgdb% 1> NUL 2> logs\%IDB_TGT%.log
type logs\%IDB_TGT%.log >> logs\all.log
find /C "ERROR:  " logs\%IDB_TGT%.log > NUL
if not errorlevel 1 goto ERROR
echo 	OK
echo @@@@OK >> logs\all.log
echo 	___________________

FOR %%G IN (sql 2.sql) DO (
	FOR %%T IN (hk media pl ia mam adm archive cues scr grid ingest logs) DO (
		set IDB_TGT=%%T
		echo  	%%T
		echo @@@@%%T >  logs\%%T.log
		FOR %%L IN (. 2.) DO (
			FOR %%F IN (!sql/%%T/db_init_%%T%%L%%G !sql/%%T/db_init_%%T_tables%%L%%G !sql/%%T/db_init_%%T_functions%%L%%G !sql/%%T/db_init_%%T_views%%L%%G !sql/%%T/db_init_%%T_types%%L%%G !sql/%%T/db_fill_%%T%%L%%G) DO (
				IF EXIST %%F (
					echo  	  	[%%F] >>  logs\%%T.log
					StreepBOM %%F | !bin\psql -h %pghost% -U user %pgdb% 1> NUL 2>> logs\%%T.log
				)
			)
		)
		type logs\%%T.log >> logs\all.log
		find /C "ERROR:  " logs\%%T.log > NUL
		if not errorlevel 1 goto ERROR
		echo 	OK
		echo @@@@OK >> logs\all.log
		echo 	___________________
	)
)
set IDB_TGT=grant
echo  	%IDB_TGT%
echo @@@@%IDB_TGT% >> logs\all.log
StreepBOM.exe !sql/common/db_init_%IDB_TGT%.sql | !bin\psql -h %pghost% -U user %pgdb% 1> NUL 2> logs\%IDB_TGT%.log
type logs\%IDB_TGT%.log >> logs\all.log
find /C "ERROR:  " logs\%IDB_TGT%.log > NUL
if not errorlevel 1 goto ERROR
echo 	OK
echo @@@@OK >> logs\all.log
echo 	___________________

echo ALL OK
echo @@ALL OK >> logs\all.log
pause
exit 0

:ERROR
echo FINISHED WITH ERROR IN %IDB_TGT%
echo @@FINISHED WITH ERROR IN %IDB_TGT% >> logs\all.log
pause
exit 1

