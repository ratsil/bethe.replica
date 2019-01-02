@echo off

set pghost=localhost

set pgdb=replica

set PATH=%PATH%;D:\Program Files\PostgreSQL\9.6\bin\;
rem chcp 65001
set PGCLIENTENCODING=utf8
echo START
echo @@START > logs\all.log

set IDB_TGT=drop
echo  	%IDB_TGT%
echo @@@@%IDB_TGT% >> logs\all.log
StreepBOM.exe common/db_init_%IDB_TGT%.sql | psql -h %pghost% -U pgsql %pgdb% 1> NUL 2> logs\%IDB_TGT%.log
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
StreepBOM.exe common/db_init_%IDB_TGT%.sql | psql -h %pghost% -U pgsql %pgdb% 1> NUL 2> logs\%IDB_TGT%.log
type logs\%IDB_TGT%.log >> logs\all.log
find /C "ERROR:  " logs\%IDB_TGT%.log > NUL
if not errorlevel 1 goto ERROR
echo 	OK
echo @@@@OK >> logs\all.log
echo 	___________________


set IDB_TGT=hk
echo  	hk
echo @@@@hk >  logs\hk.log
FOR %%F IN (hk/db_init_hk.sql hk/db_init_hk_tables.sql hk/db_init_hk_functions.sql hk/db_init_hk_views.sql hk/db_init_hk_types.sql hk/db_fill_hk.sql) DO (
	IF EXIST %%F (
		echo  	  	[%%F] >>  logs\hk.log
		StreepBOM %%F | psql -h %pghost% -U replica_init %pgdb% 1> NUL 2>> logs\hk.log
	)
)
type logs\hk.log >> logs\all.log
find /C "ERROR:  " logs\hk.log > NUL
if not errorlevel 1 goto ERROR
echo 	OK
echo @@@@OK >> logs\all.log
echo 	___________________

FOR %%T IN (media pl ia mam adm archive cues scr grid ingest logs) DO (
	set IDB_TGT=%%T
	echo  	%%T : init+tables+functions
	echo @@@@%%T >  logs\%%T.log
	FOR %%F IN (%%T/db_init_%%T.sql %%T/db_init_%%T_tables.sql %%T/db_init_%%T_functions.sql) DO (
		IF EXIST %%F (
			echo  	  	[%%F] >>  logs\%%T.log
			StreepBOM %%F | psql -h %pghost% -U replica_init %pgdb% 1> NUL 2>> logs\%%T.log
		)
	)
	type logs\%%T.log >> logs\all.log
	find /C "ERROR:  " logs\%%T.log > NUL
	if not errorlevel 1 goto ERROR
	echo 	OK
	echo @@@@OK >> logs\all.log
	echo 	___________________
)

FOR %%T IN (media pl ia mam adm archive cues scr grid ingest logs) DO (
	set IDB_TGT=%%T
	echo  	%%T : views+types+fill
	echo @@@@%%T >>  logs\%%T.log
	FOR %%F IN (%%T/db_init_%%T_views.sql %%T/db_init_%%T_types.sql %%T/db_fill_%%T.sql) DO (
		IF EXIST %%F (
			echo  	  	[%%F] >>  logs\%%T.log
			StreepBOM %%F | psql -h %pghost% -U replica_init %pgdb% 1> NUL 2>> logs\%%T.log
		)
	)
	type logs\%%T.log >> logs\all.log
	find /C "ERROR:  " logs\%%T.log > NUL
	if not errorlevel 1 goto ERROR
	echo 	OK
	echo @@@@OK >> logs\all.log
	echo 	___________________
)


FOR %%T IN (hk media pl ia mam adm archive cues scr grid ingest logs) DO (
	set IDB_TGT=%%T
	echo  	%%T : second pass
	echo @@@@%%T >>  logs\%%T.log
	FOR %%F IN (%%T/db_init_%%T.2.sql %%T/db_init_%%T_tables.2.sql %%T/db_init_%%T_functions.2.sql %%T/db_init_%%T_views.2.sql %%T/db_init_%%T_types.2.sql %%T/db_fill_%%T.2.sql) DO (
		IF EXIST %%F (
			echo  	  	[%%F] >>  logs\%%T.log
			StreepBOM %%F | psql -h %pghost% -U replica_init %pgdb% 1> NUL 2>> logs\%%T.log
		)
	)
	type logs\%%T.log >> logs\all.log
	find /C "ERROR:  " logs\%%T.log > NUL
	if not errorlevel 1 goto ERROR
	echo 	OK
	echo @@@@OK >> logs\all.log
	echo 	___________________
)

set IDB_TGT=grant
echo  	%IDB_TGT%
echo @@@@%IDB_TGT% >> logs\all.log
StreepBOM.exe common/db_init_%IDB_TGT%.sql | psql -h %pghost% -U replica_init %pgdb% 1> NUL 2> logs\%IDB_TGT%.log
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

