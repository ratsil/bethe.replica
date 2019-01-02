using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.IO;
using System.Threading;
using helpers;
using helpers.extensions;
using helpers.replica.pl;
using helpers.replica.mam;
using g = globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Management;

namespace replica.management
{
	public partial class Service : ServiceBase
	{
		private ManualResetEvent _mrePlaylistWatcherStopping;
		private ManualResetEvent _mrePlaylistWatcherStopped;
		private ManualResetEvent _mreArchiveWatcherStopping;
		private ManualResetEvent _mreArchiveWatcherStopped;
		private ManualResetEvent _mreCommandWatcherStopping;
		private ManualResetEvent _mreCommandWatcherStopped;
		private ManualResetEvent _mreDBBackupWatcherStopping;
		private ManualResetEvent _mreDBBackupWatcherStopped;

		Dictionary<DateTime, VIPlaylist> _ahVIFilesChecked;
		Dictionary<DateTime, DateTime> _ahVIFilesModified;

		public Service()
		{
			InitializeComponent();
		}
		protected override void OnStart(string[] args)
		{
			base.OnStart(args);
			try
			{
				(new Logger("management")).WriteWarning("получен сигнал на запуск");//TODO LANG
				_mreArchiveWatcherStopping = new ManualResetEvent(false);
				_mreArchiveWatcherStopped = new ManualResetEvent(true);
				ThreadPool.QueueUserWorkItem(ArchiveWatcher, this);
				Thread.Sleep(300);
				if (Preferences.bPlaylistGenerating)
				{
					_mrePlaylistWatcherStopping = new ManualResetEvent(false);
					_mrePlaylistWatcherStopped = new ManualResetEvent(true);
					ThreadPool.QueueUserWorkItem(PlaylistWatcher, this);
					Thread.Sleep(300);
					_mrePlaylistWatcherStopped.Reset();
				}
				if (Preferences.sPgDumpBinPath!=null)
				{
					_mreDBBackupWatcherStopping = new ManualResetEvent(false);
					_mreDBBackupWatcherStopped = new ManualResetEvent(true);
					ThreadPool.QueueUserWorkItem(DBBackupWatcher, this);
					Thread.Sleep(300);
					_mreDBBackupWatcherStopped.Reset();
				}
				_mreCommandWatcherStopping = new ManualResetEvent(false);
				_mreCommandWatcherStopped = new ManualResetEvent(true);
				ThreadPool.QueueUserWorkItem(CommandWatcher, this);
				_mreCommandWatcherStopped.Reset();
				(new Logger("management")).WriteNotice("модуль управления запущен");//TODO LANG

			}
			catch (Exception ex)
			{
				(new Logger("management")).WriteError(ex);
			}
		}
		protected override void OnStop()
		{
			try
			{
				(new Logger("management")).WriteWarning("получен сигнал на остановку");//TODO LANG
				_mreArchiveWatcherStopping.Set();
				_mreCommandWatcherStopping.Set();
				if(null != _mrePlaylistWatcherStopping)
				{
					_mrePlaylistWatcherStopping.Set();
					_mrePlaylistWatcherStopped.WaitOne(15000, false);
				}

				if (null != _mreDBBackupWatcherStopping)
				{
					_mreDBBackupWatcherStopping.Set();
					_mreDBBackupWatcherStopped.WaitOne(15000, false);
				}

				_mreArchiveWatcherStopped.WaitOne(15000, false);
				_mreCommandWatcherStopped.WaitOne(15000, false);
				(new Logger("management")).WriteNotice("модуль управления остановлен");//TODO LANG
			}
			catch (Exception ex)
			{
				(new Logger("management")).WriteError(ex);
			}
		}

		public void PlaylistWatcher(object cStateInfo)
		{
			try
			{
				(new Logger("playlist")).WriteNotice("управление плейлистом запущено");//TODO LANG

				_ahVIFilesChecked = new Dictionary<DateTime, VIPlaylist>();
				_ahVIFilesModified = new Dictionary<DateTime, DateTime>();

				do
				{
					try
					{
						int nFramesLeft = (new DBInteract()).PlaylistFramesQtyGet();

						if (!Preferences.sAdvertsPath.IsNullOrEmpty())
							CheckAndClearVIFolder(Preferences.sAdvertsPath);

						if (-1 < nFramesLeft && Preferences.tsPlaylistMinimumLength.TotalMilliseconds > (nFramesLeft * 40))
                        {
                            DateTime dtStart = DateTime.Now;
							(new Logger("playlist")).WriteNotice("начало генерации плейлиста...    [текущий плейлист = " + nFramesLeft + " кадров] [генерируемый = " + Preferences.tsPlaylistGenerationLength.TotalMilliseconds / 40 + "]"); //TODO LANG
							PlaylistGenerate(Preferences.tsPlaylistGenerationLength);
                            TimeSpan dtDelta = DateTime.Now.Subtract(dtStart);
                            string sS = "";
                            if (dtDelta.TotalMinutes > 4)
                            {
                                sS = (DateTime.Today + dtDelta).ToString("HH:mm:ss");
                                sS = ", но за время превышающее 4 минуты!! [t=" + sS + "]";
                            }
							(new Logger("playlist")).WriteNotice("плейлист успешно сгенерирован" + sS); //TODO LANG
                        }
                    }
                    catch (Exception ex)
                    {
						(new Logger("playlist")).WriteError(ex); //UNDONE
                    }
#if DEBUG
					Thread.Sleep(1000);
				}
				while (true);
#else
				}
				while (!_mrePlaylistWatcherStopping.WaitOne(Preferences.tsSleepDuration, false));
#endif
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex); //UNDONE
			}
			(new Logger("playlist")).WriteNotice("управление плейлистом остановлено");//TODO LANG
			if (null != _mrePlaylistWatcherStopped)
				_mrePlaylistWatcherStopped.Set();
		}

		private void ArchiveWatcher(object cStateInfo)
		{
			try
			{
				(new Logger("archive")).WriteNotice("архивирование плейлиста запущено");//TODO LANG
				DateTime dtMessagesArchive = DateTime.MinValue;
				do
				{
					try
					{
						MoveItemsToArchive();
					}
					catch (Exception ex)
					{
						(new Logger("archive")).WriteError(ex); //UNDONE
					}
					try
					{
						if (DateTime.Now > dtMessagesArchive)
						{
							dtMessagesArchive = DateTime.Now.Add(TimeSpan.FromMinutes(10));
							(new DBInteract()).MessagesArchive();
						}
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex); //UNDONE
					}
				} while (!_mreArchiveWatcherStopping.WaitOne(Preferences.tsSleepDuration, false));
			}
			catch (Exception ex)
			{
				(new Logger("archive")).WriteError(ex); //UNDONE
			}
			(new Logger("archive")).WriteNotice("архивирование плейлиста остановлено");//TODO LANG
			if (null != _mreArchiveWatcherStopped)
				_mreArchiveWatcherStopped.Set();
		}

		public void DBBackupWatcher(object cStateInfo)
		{
			try
			{
				(new Logger("archive")).WriteNotice("слежение за бекапированием БД запущено");//TODO LANG
				DateTime dtLastDump = DateTime.MinValue;
                DateTime dtNow = DateTime.Now;

                string sNameBeginning = Preferences.sPgDumpName + "__" + dtNow.ToString("yyyy_MM_dd");
				string sFName;
				foreach (string sF in Directory.GetFiles(Preferences.sPgDumpPath))
				{
					sFName = Path.GetFileName(sF);
					if (sFName.StartsWith(sNameBeginning) && sFName.EndsWith(".zip"))
						dtLastDump = dtNow;
				}

				do
				{
					try
					{
                        dtNow = DateTime.Now;
                        if (dtLastDump.Date < dtNow.Date)
                        {
                            if (MakeDBBackup(Preferences.sPgDumpPath, Preferences.sPgDumpName, Preferences.sPgDumpBinPath, Preferences.sPgDBHostName, Preferences.sPgDBPort, 
                                             Preferences.sPgDBName, "user", Preferences.sPgDumpCopyToPath, Preferences.sPgDumpCopyToLogin, Preferences.sPgDumpCopyToPass))
                                dtLastDump = DateTime.Now;
                        }
					}
					catch (Exception ex)
					{
						(new Logger("archive")).WriteError("DB Dump", ex); //UNDONE
					}
#if DEBUG
					Thread.Sleep(1000);
				}
				while (true);  
#else
				}
				while (!_mreDBBackupWatcherStopping.WaitOne(Preferences.tsPgDumpSleepDuration, false));
#endif
			}
			catch (Exception ex)
			{
				(new Logger("archive")).WriteError("слежение за бекапированием БД", ex); //UNDONE
			}
			(new Logger("archive")).WriteNotice("слежение за бекапированием БД остановлено");//TODO LANG
			if (null != _mreDBBackupWatcherStopped)
				_mreDBBackupWatcherStopped.Set();
		}
        public bool MakeDBBackup(string sPgDumpPath, string sPgDumpName, string sPgDumpBinPath, string sPgDBHostName, string sPgDBPort, string sPgDBName, string sPgDBUser, 
                                 string sPgDumpCopyToPath, string sPgDumpCopyToLogin, string sPgDumpCopyToPass)
        {
            string sFileOutGlobals, sFileOut, sFileLog, sFileZip, sLogger = "", sCopyName;
            string sFName, sFileOptimized, sFileWithoutArchive, sFileArchive;
            DateTime dtNow = DateTime.Now;
            bool bRetVal = false;

            sFileOutGlobals = System.IO.Path.Combine(sPgDumpPath, sPgDumpName + "__" + dtNow.ToString("yyyy_MM_dd_HHmm") + "_backup_globals.sql");
            sFileOut = System.IO.Path.Combine(sPgDumpPath, sPgDumpName + "__" + dtNow.ToString("yyyy_MM_dd_HHmm") + "_backup.sql");
            sFileLog = System.IO.Path.Combine(sPgDumpPath, sPgDumpName + "__" + dtNow.ToString("yyyy_MM_dd_HHmm") + "_backup.log");
            sFileZip = System.IO.Path.Combine(sPgDumpPath, sPgDumpName + "__" + dtNow.ToString("yyyy_MM_dd_HHmm") + "_backup.zip");
            if (DB.BackUp.PgDump(sPgDumpBinPath, sPgDBHostName, sPgDBPort, sPgDBName, sPgDBUser, sFileOutGlobals, true, sLogger, out sLogger)
                && DB.BackUp.PgDump(sPgDumpBinPath, sPgDBHostName, sPgDBPort, sPgDBName, sPgDBUser, sFileOut, false, sLogger, out sLogger))
            {
                System.IO.File.WriteAllText(sFileLog, sLogger);
                System.Threading.Thread.Sleep(1000);
                if (!File.Exists(sFileOut))
                    throw new Exception("no dumped file here [f=" + sFileOut + "]");
                if (!File.Exists(sFileOutGlobals))
                    throw new Exception("no dumped file here [f=" + sFileOutGlobals + "]");

                PgDumpFileOptimizeAndSplit(sFileOut, out sFileOptimized, out sFileWithoutArchive, out sFileArchive);

                if (!File.Exists(sFileLog))
                    (new Logger("archive")).WriteWarning("no log file here [f=" + sFileLog + "]");//TODO LANG
                if (!File.Exists(sFileOptimized))
                    (new Logger("archive")).WriteWarning("no Optimized file here [f=" + sFileOptimized + "]");//TODO LANG
                if (!File.Exists(sFileWithoutArchive))
                    (new Logger("archive")).WriteWarning("no Without_Archive file here [f=" + sFileWithoutArchive + "]");//TODO LANG
                if (!File.Exists(sFileArchive))
                    (new Logger("archive")).WriteWarning("no archive_only file here [f=" + sFileArchive + "]");//TODO LANG

                if (File.Exists(sFileZip))
                    File.Delete(sFileZip);

                List<string> aFiles = MakeRestoringScripts(sPgDumpPath, sFileOutGlobals, sFileOptimized, sFileWithoutArchive, sFileArchive, sPgDumpBinPath);
                aFiles.AddRange(new string[5] { sFileOptimized, sFileOutGlobals, sFileLog, sFileWithoutArchive, sFileArchive });

                Zip.ZipFiles(aFiles.ToArray(), sFileZip, true, true, System.IO.Compression.CompressionLevel.Optimal);
                Thread.Sleep(200);
                File.Delete(sFileOut);
                bRetVal = true;

                foreach (string sF in Directory.GetFiles(sPgDumpPath))
                {
                    sFName = Path.GetFileName(sF);
                    if (sF != sFileZip && sFName.StartsWith(sPgDumpName) && sFName.EndsWith(".zip"))
                        File.Delete(sF);
                }

                if (null != sPgDumpCopyToPath)
                {
                    if (null != sPgDumpCopyToLogin && null != sPgDumpCopyToPass)
                    {
                        string sErrors = helpers.PinvokeWindowsNetworking.connectToRemote(sPgDumpCopyToPath, sPgDumpCopyToLogin, sPgDumpCopyToPass);
                        if (sErrors != null)
                            throw new Exception("can't copy to share - no share found. error is <br>[" + sErrors + "]");
                    }
                    sCopyName = Path.Combine(sPgDumpCopyToPath, Path.GetFileName(sFileZip));
                    if (File.Exists(sCopyName))
                    {
                        File.Delete(sCopyName);
                        Thread.Sleep(500);
                    }
                    File.Copy(sFileZip, sCopyName);  // иногда даёт разные варианты "System.IO.__Error.WinIOError", если херь с сетевым путём. Например 'превышен таймаут семафора'.
                }
            }
            return bRetVal;
        }
        public void PgDumpFileOptimize(string sFile, out string sFileOptimized)
		{
			StreamReader cRF = new StreamReader(sFile);
			string sName = Path.GetFileNameWithoutExtension(sFile);
			string sExtension = Path.GetExtension(sFile);
			string sPath = Path.GetDirectoryName(sFile);
			sFileOptimized = Path.Combine(sPath, sName + "_Optimized" + sExtension);
			System.IO.StreamWriter cWFOptim = new System.IO.StreamWriter(sFileOptimized);
			try
			{
				string sL;
				while ((sL = cRF.ReadLine()) != null)
				{
					cWFOptim.WriteLine(sL);
					if (TableData(sL))
					{
						(new Logger("archive")).WriteNotice("PgDumpFileSplit: table data found [" + sL + "]");
						ReadWriteOptimizeThisTable(false, cRF, cWFOptim, null, null, null);
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger("archive")).WriteError("Can't make without_archive version", ex); //UNDONE
			}
			finally
			{
				cWFOptim.Close();
				cRF.Close();
			}
		}
		public void PgDumpFileOptimizeAndSplit(string sFile, out string sFileOptimized, out string sFileWithoutArchive, out string sFileArchive)
		{
			StreamReader cRF = new StreamReader(sFile);
			string sName = Path.GetFileNameWithoutExtension(sFile);
			string sExtension = Path.GetExtension(sFile);
			string sPath = Path.GetDirectoryName(sFile);
			sFileWithoutArchive = Path.Combine(sPath, sName + "_WithoutArchiveTables" + sExtension);
			sFileArchive = Path.Combine(sPath, sName + "_ArchiveTables" + sExtension);
			sFileOptimized= Path.Combine(sPath, sName + "_Optimized" + sExtension);
			System.IO.StreamWriter cWFSmall = new System.IO.StreamWriter(sFileWithoutArchive);
			System.IO.StreamWriter cWFArch = new System.IO.StreamWriter(sFileArchive);
			System.IO.StreamWriter cWFOptim = new System.IO.StreamWriter(sFileOptimized);
			try
			{
				cWFSmall.WriteLine("-- This file is version of dump without archive tables (and 1 ia. and 1 hk. table) adding for faster DB restoring");
				cWFSmall.WriteLine("-- PLEASE TURN OFF the Management SERVICE BEFORE PROCEED!!!!!"); // иначе могут быть удалены те HK, от которых нет пока dtEvents, т.е. всё будет стёрто в hk."tHouseKeeping"
				cWFArch.WriteLine("-- This file is for adding rows to archive tables only (and 1 ia. and 1 hk. table)");
				cWFArch.WriteLine("-- PLEASE TURN OFF trigger FUNCTION archive.\"fCollector\" BEFORE PROCEED!!!!!  (insert only)");   // иначе не добавиться ничего
				//cWFArch.WriteLine("-- PLEASE TURN OFF trigger FUNCTION hk.\"fManagement\" BEFORE PROCEED!!!!!  (insert only)");  пока не касается таблиц, которые добавляются через этот файл
				cWFArch.WriteLine("SET statement_timeout = 0;");
				cWFArch.WriteLine("SET lock_timeout = 0;");
				cWFArch.WriteLine("SET client_encoding = 'UTF8';");
				cWFArch.WriteLine("SET standard_conforming_strings = on;");
				cWFArch.WriteLine("SET check_function_bodies = false;");
				cWFArch.WriteLine("SET client_min_messages = warning;");
				cWFArch.WriteLine("SET row_security = off;");

				string sL;
				while ((sL = cRF.ReadLine()) != null)
				{
					cWFSmall.WriteLine(sL);
					cWFOptim.WriteLine(sL);
					if (ArchTables(sL))
					{
						(new Logger("archive")).WriteNotice("PgDumpFileSplit: archive table data found [" + sL + "]");
						ReadWriteOptimizeThisTable(true, cRF, cWFOptim, cWFSmall, cWFArch, "SET search_path = public, archive, pg_catalog;");
					}
					else if (TableData(sL))
					{
						(new Logger("archive")).WriteNotice("PgDumpFileSplit: table data found [" + sL + "]");
						ReadWriteOptimizeThisTable(false, cRF, cWFOptim, cWFSmall, cWFArch, null);
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger("archive")).WriteError("Can't make without_archive version", ex); //UNDONE
			}
			finally
			{
				cWFOptim.Close();
				cWFSmall.Close();
				cWFArch.Close();
				cRF.Close();
			}
		}
		private int[] SplitInsertCommand(string sIns)
		{
			int[] aRetVal = new int[2];
			aRetVal[0] = sIns.IndexOf("VALUES", 12);
			aRetVal[1] = sIns.IndexOf("(", aRetVal[0]);
			string ss = sIns.Substring(0, aRetVal[0] + 7);
			ss = sIns.Substring(aRetVal[1], sIns.Length - 1 - aRetVal[1]);
			return aRetVal;
		}
		private void ReadWriteOptimizeThisTable(bool bSplit, StreamReader cRF, StreamWriter cFull, StreamWriter cSmall, StreamWriter cArch, string sSetSchema)
		{
			int nI = 0, nI2 = 0, nBlock = 0, nMaxElementsInBlock = 300;
			bool bInsertBegins = false;
			string sL, sL2;
			long nCountTotal = 0;
			bool bFirst = true;
			int[] aA = null;
			StreamWriter cArchBKP = cArch;
			if (!bSplit)
				cArch = cSmall;

			while ((sL = cRF.ReadLine()) != null)
			{
				nCountTotal++;
				if (!bInsertBegins && !sL.StartsWith("INSERT INTO") && nI >= 3)
				{
					(new Logger("archive")).WriteNotice("PgDumpFileSplit: can't find 'INSERT INTO'");  
					return;
				}

				if (!bInsertBegins && sL.StartsWith("INSERT INTO"))
				{
					bInsertBegins = true;
					if (!sSetSchema.IsNullOrEmpty() && bSplit)
						cArch.WriteLine(sSetSchema);
					aA = SplitInsertCommand(sL);
					nBlock = nMaxElementsInBlock + 1;
				}

				if (!bInsertBegins)
				{
					nI++;
					if (cSmall != null)
						cSmall.WriteLine(sL);
					cFull.WriteLine(sL);
					continue;
				}

				if (bInsertBegins && sL.StartsWith("INSERT INTO") && !sL.EndsWith(");"))
				{
					while ((sL2 = cRF.ReadLine()) != null)
					{
						nI2++;
						sL = sL + "\n" + sL2;
						if (sL2.EndsWith(");"))
							break;
						if (nI2 > 100)
							(new Logger("archive")).WriteError("PgDumpFileSplit: can't find ');' in the line " + nCountTotal + " and "+ nI2 + " next lines!");
					}
					nI2 = 0;
				}

				if (bInsertBegins)
				{
					if (sL.StartsWith("INSERT INTO"))
					{
						if (nBlock > nMaxElementsInBlock)
						{
							nBlock = 0;
							if (!bFirst)
							{
								if (cArch != null)
									cArch.Write(";\n");
								cFull.Write(";\n");
							}
							else
								bFirst = false;
							if (cArch != null)
								cArch.Write(sL.Substring(0, aA[0] + 7) + "\n");
							cFull.Write(sL.Substring(0, aA[0] + 7) + "\n");
						}
						else
						{
							if (cArch != null)
								cArch.Write(",\n");
							cFull.Write(",\n");
						}
						if (cArch != null)
							cArch.Write("\t" + sL.Substring(aA[1], sL.Length - 1 - aA[1]));
						cFull.Write("\t" + sL.Substring(aA[1], sL.Length - 1 - aA[1]));
						nBlock++;
					}
					else
					{
						if (cArch != null)
							cArch.Write(";\n");
						cFull.Write(";\n");
						if (cSmall != null)
							cSmall.WriteLine(sL);
						cFull.WriteLine(sL);
						break;
					}
				}
			}
			cArch = cArchBKP;
		}
		private bool TableData(string sS)
		{
			if (
					sS.StartsWith("-- Data for Name:") &&
					sS.Contains("Type: TABLE DATA")
				)
				return true;
			return false;
		}
		private bool ArchTables(string sS)
		{
			if (
					TableData(sS) &&
					sS.Contains("Schema: archive")
				)
				return true;
			return false;
		}
		private bool HkTables(string sS)
		{
			if (sS.StartsWith("-- Data for Name: tDTEvents; Type: TABLE DATA; Schema: hk"))
				return true;
			return false;
		}
		private bool IaTables(string sS)
		{
			if (sS.StartsWith("-- Data for Name: tNumbers; Type: TABLE DATA; Schema: ia"))
				return true;
			return false;
		}
		public List<string> MakeRestoringScripts(string sPath, string sFileGlobals, string sFileOptimized, string sFileWithoutArchive, string sFileArchive, string sPgDumpBinPath)
		{
			List<string> aRetVal = new List<string>();
			aRetVal.Add(Path.Combine(sPath, "01_restore_globals.cmd"));
			StreamWriter cSW = new StreamWriter(aRetVal[aRetVal.Count - 1]);
			string sPsqlExe = Path.Combine(sPgDumpBinPath, "psql.exe");
			cSW.WriteLine("REM This script is for restoring users and roles ONLY in DB cluster. Run if you create new cluster or have wrong users and roles");
			cSW.WriteLine("REM ---- instructions ----");
			cSW.WriteLine("REM COMPLETE these steps BEFORE RUN !!!");
			cSW.WriteLine("REM 1) MAKE DB cluster (version = postgresql-9.5.2-1 'PostgreSQL 9.5.2, build 1800, 64-bit') port 5432, symbols = C. (Distrib = /path/postgresql-9.5.2-1-windows-x64.exe)");
			cSW.WriteLine("REM 2) EDIT FILE - add password for user to /path/pgpass.conf (or delete '--no-password' from parameters)");
			cSW.WriteLine("REM                        localhost:5432:*:user:your_password");
			cSW.WriteLine("REM 3) Turn off initiator, sync and management services (all services)");
			cSW.WriteLine("REM 4) EDIT command below and try to run LOCALLY on NEW DB-SERVER");
			cSW.WriteLine("prompt $T$G");
			cSW.WriteLine("pause DID YOU READ COMMENTS????????????");
			cSW.WriteLine("\"" + sPsqlExe + "\" --file \"" + sFileGlobals + "\" --host localhost --port 5432 --username user --no-password");
			cSW.Close();
			aRetVal.Add(Path.Combine(sPath, "02_restore_without_archive.cmd"));
			cSW = new StreamWriter(aRetVal[aRetVal.Count - 1]);
			cSW.WriteLine("REM This script is for the partly restoring replica db without all archive tables. Run if you have no time for complete restore");
			cSW.WriteLine("REM ---- instructions ----");
			cSW.WriteLine("REM COMPLETE these steps BEFORE RUN !!!");
			cSW.WriteLine("REM 1) RUN globals restore script (if needed)");
			cSW.WriteLine("REM 2) (DELETE old AND) MAKE DB replica: owner user, kodirovka = UTF-8, tabli prostranstvo = pg_default, sopostavlenie = C, tip simvola = C");
			cSW.WriteLine("REM 3) EDIT FILE - add password for user to /path/pgpass.conf (or delete '--no-password' from parameters)");
			cSW.WriteLine("REM                        localhost:5432:*:user:your_password");
			cSW.WriteLine("REM 4) Turn off initiator, sync and management services (all services)");
			cSW.WriteLine("REM 5) EDIT command below (PATHs) and try to run LOCALLY on NEW DB-SERVER");
			cSW.WriteLine("REM");
			cSW.WriteLine("REM proces will take about 20 seconds on local machine.");
			cSW.WriteLine("prompt $T$G");
			cSW.WriteLine("pause DID YOU READ COMMENTS????????????");
			cSW.WriteLine("\"" + sPsqlExe + "\" --file \"" + sFileWithoutArchive + "\" --host localhost --port 5432 --no-password replica user");
			cSW.WriteLine("REM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!! AND Turn on initiator, player, sync and cues services   NOT MANAGEMENT !!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			cSW.Close();
			aRetVal.Add(Path.Combine(sPath, "03_restore_archive.cmd"));
			cSW = new StreamWriter(aRetVal[aRetVal.Count - 1]);
			cSW.WriteLine("REM This script is for the restoring archive tables in replica db. Run 'without_archive' version before");
			cSW.WriteLine("REM ---- instructions ----");
			cSW.WriteLine("REM COMPLETE these steps BEFORE RUN !!!");
			cSW.WriteLine("REM 1) RUN without_archive restore script and complete it's steps");
			cSW.WriteLine("REM 2) Turn off replica.management service (preferably)");
			cSW.WriteLine("REM 3) TURN OFF FUNCTION archive.\"fCollector\" BY UNCOMMENTING STRINGS (OR ADDING) like this: ");
			cSW.WriteLine("REM           IF TG_OP = 'UPDATE' OR  TG_OP = 'INSERT'   THEN");
			cSW.WriteLine("REM          		RETURN NEW;");
			cSW.WriteLine("REM          	END IF;");
			cSW.WriteLine("REM 4) EDIT command below and try to run LOCALLY on NEW DB-SERVER");
			cSW.WriteLine("REM");
			cSW.WriteLine("REM proces will take about 7 minutes on local machine (on 1.7 GBt file)");
			cSW.WriteLine("prompt $T$G");
			cSW.WriteLine("pause DID YOU READ COMMENTS????????????");
			cSW.WriteLine("\"" + sPsqlExe + "\" --file \"" + sFileArchive + "\" --host localhost --port 5432 --no-password replica user");
			cSW.WriteLine("REM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!! AND NOW RESTORE FUNCTION archive.\"fCollector\" !!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!! AND Turn on MANAGEMENT !!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			cSW.Close();
			aRetVal.Add(Path.Combine(sPath, "02_03_restore_full_version.cmd"));
			cSW = new StreamWriter(aRetVal[aRetVal.Count - 1]);
			cSW.WriteLine("REM This script is for the complete restoring replica db. If you have no time - run 'without_archive' version instead");
			cSW.WriteLine("REM ---- instructions ----");
			cSW.WriteLine("REM COMPLETE these steps BEFORE RUN !!!");
			cSW.WriteLine("REM 1) RUN globals restore script (if needed)");
			cSW.WriteLine("REM 2) (DELETE old AND) MAKE DB replica: owner user, kodirovka = UTF-8, tabli prostranstvo = pg_default, sopostavlenie = C, tip simvola = C");
			cSW.WriteLine("REM 3) EDIT FILE - add password for user to /path/pgpass.conf (or delete '--no-password' from parameters)");
			cSW.WriteLine("REM                        localhost:5432:*:user:your_password");
			cSW.WriteLine("REM 4) Turn off initiator, sync and management services (all services)");
			cSW.WriteLine("REM");
			cSW.WriteLine("REM proces will take about 7 minutes on local machine (on 1.7 GBt file)");
			cSW.WriteLine("prompt $T$G");
			cSW.WriteLine("pause DID YOU READ COMMENTS????????????");
			cSW.WriteLine("\"" + sPsqlExe + "\" --file \"" + sFileOptimized + "\" --host localhost --port 5432 --no-password replica user");
			cSW.WriteLine("REM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!! AND Turn on initiator, player, sync, management and cues services !!!!!!!!!!!!!");
			cSW.WriteLine("REM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			cSW.Close();
			return aRetVal;
		}

		public void CommandWatcher(object cStateInfo)
		{
			try
			{
				(new Logger("commands")).WriteNotice("управление командами запущено");//TODO LANG
				DBInteract cDBI = new DBInteract();
				do
				{
					try
					{
						cDBI.ProcessCommands();
					}
					catch (Exception ex)
					{
						cDBI = new DBInteract();
						(new Logger("commands")).WriteError(ex); //UNDONE
					}
				} while (!_mreCommandWatcherStopping.WaitOne(Preferences.tsCommandsSleepDuration, false));
			}
			catch (Exception ex)
			{
				(new Logger("commands")).WriteError(ex); //UNDONE
			}
			(new Logger("commands")).WriteNotice("управление командами остановлено");//TODO LANG
			if (null != _mreCommandWatcherStopped)
				_mreCommandWatcherStopped.Set();
		}
		public bool MoveItemsToArchive()
		{
			//(new Logger()).WriteNotice("Начало архивации tItems"); //UNDONE
			DBInteract cDBI = new DBInteract();
			int nQty = cDBI.ItemsMoveToArchive();
			if (0 > nQty)
				(new Logger("archive")).WriteNotice("некритическая ошибка архивации таблиц"); //UNDONE
			else if (0 < nQty)
				(new Logger("archive")).WriteNotice("произведена архивации таблиц. Кол-во заархивированных записей: " + nQty); //UNDONE
			return true;
		}
		#region generate
		#region generate.check_vi_file
		protected virtual bool IsFileLocked(string sFile)
		{
			FileInfo cFI = new FileInfo(sFile);
			FileStream stream = null;

			try
			{
				stream = cFI.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}

			//file is not locked
			return false;
		}
		private void CheckAndClearVIFolder(string sFolder)
		{
			string sFName = "";
			string sFNameExt = "";
			DateTime dtFilenameDate;
			VIPlaylist cVIPL;
			List<string> aFiles;
			foreach (string sFile in Directory.GetFiles(sFolder))
			{
				try
				{
					sFName = Path.GetFileNameWithoutExtension(sFile);
					sFNameExt = Path.GetFileName(sFile);
					if (DateTime.TryParseExact(sFName, "dd'.'MM'.'yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dtFilenameDate))
					{
						dtFilenameDate = dtFilenameDate.Date;
						if (DateTime.Now.Subtract(dtFilenameDate).TotalDays > 7)
						{
							File.Delete(sFile);
							(new Logger("VI")).WriteNotice("чистка папки с VI. удалён старый файл: [" + sFile + "]");
							continue;
						}
					}
					else
					{
						if (!Directory.Exists(Path.Combine(Preferences.sAdvertsPath, "trashed_files")))
							Directory.CreateDirectory(Path.Combine(Preferences.sAdvertsPath, "trashed_files"));
						File.Move(sFile, Path.Combine(Preferences.sAdvertsPath, "trashed_files", sFNameExt));
						if (!Preferences.sVIMailTargets.IsNullOrEmpty())
							(new Logger("VI")).Email(Preferences.sVIMailTargets, "Ошибка с файлом VI", "Во время чистки папки с VI найден файл со странным именем: \n[" + sFNameExt + "]");
						(new Logger("VI")).WriteNotice("чистка папки с VI. найден файл со странным именем: [" + sFile + "]");
						continue;
					}

					if ((!_ahVIFilesChecked.ContainsKey(dtFilenameDate) || _ahVIFilesModified[dtFilenameDate] != File.GetLastWriteTime(sFile)) && !IsFileLocked(sFile))
					{
						_ahVIFilesChecked.Add(dtFilenameDate, null);
						_ahVIFilesModified.Add(dtFilenameDate, File.GetLastWriteTime(sFile));

						cVIPL = VideoInternationalFileParse(sFile);

						if (cVIPL.sChannel.ToLower() != Preferences.sChannel.ToLower())
							LogAndSend("Ошибка с файлом VI. Не совпадает указанное название канала ["+ cVIPL.sChannel + "] с ожидаемым [" + Preferences.sChannel + "]\n[" + sFile + "]", "Ошибка при проверке файла VI: " + sFNameExt, null, new Exception("Ошибка при проверке файла VI"));

						if (cVIPL.dtDate.Date != dtFilenameDate.Date)
							LogAndSend("Ошибка с файлом VI. Не совпадает указанная дата [" + cVIPL.dtDate.ToString("dd.MM.yyyy") + "] с датой в имени файла [" + dtFilenameDate.ToString("dd.MM.yyyy") + "]\n[" + sFile + "]", "Ошибка при проверке файла VI: " + sFNameExt, null, new Exception("Ошибка при проверке файла VI"));

						if (null == cVIPL || cVIPL.nBlocksQty <= 0)
						{
							LogAndSend("Ошибка с файлом VI. Во время анализа файла VI не найдено ни одного блока: \n[" + sFile + "]", "Ошибка при проверке файла VI: " + sFNameExt, null, new Exception("Ошибка при проверке файла VI"));
							continue;
						}
						aFiles = cVIPL.CheckFiles();
						if (!aFiles.IsNullOrEmpty())
						{
							LogAndSend("Ошибка с рекламными видео файлами: их нет на эфирном сервере", "Ошибка с файлом VI", aFiles, new Exception("Ошибка с файлом VI"));
							continue;
						}
						_ahVIFilesChecked[dtFilenameDate] = cVIPL;
						LogAndSend("Успешно проверен файл: [" + sFNameExt + "]\nНайдено рекламных блоков: [" + cVIPL.nBlocksQty + "]", "Успешно проверен файл VI: " + sFNameExt);
					}
				}
				catch (Exception ex)
				{
					LogAndSend("Ошибка при проверке файла VI: [file=" + sFNameExt + "]", "Ошибка при проверке файла VI: " + sFNameExt, null, ex);
				}
			}
		}
		#endregion
		#region generate.adverts_get
		Dictionary<string, Asset> _ahVIBinds;
		private VIPlaylist VideoInternationalFileParse(string sFile)
		{
			DBInteract cDBI = new DBInteract();
			new Logger("playlist").WriteNotice("VI LOG");
			Dictionary<string, Class> ahClasses = cDBI.ClassesGet().ToDictionary(o => o.sName, o => o);
			VIPlaylist cRetVal = null;
			if (null != sFile && System.IO.File.Exists(sFile))
			{
				Queue<List<string>> aqExcelValues = Excel.GetValuesFromExcel(sFile);

				_ahVIBinds = new Dictionary<string, Asset>();
				foreach (Asset cAsset in cDBI.AssetsGet(new CustomValue("vi_id", null)))
				{
					try
					{
						_ahVIBinds.Add(cAsset.aCustomValues[0].sValue, cAsset);
					}
					catch { }
				}

				cRetVal = new VIPlaylist();
				List<string> aRow = null;
				TimeSpan tsBlockTime = TimeSpan.MinValue;
				TimeSpan tsPrevBlockTime = TimeSpan.MinValue;
				int nBlockExcelStart = 0;
				int nBlockAdvertisementStart = 0, nIndx = 0;
				string sTime, sText, sID, sType, sCover;
				TimeSpan ts;


				List<string> aMissedIDs = new List<string>();
				while (0 < aqExcelValues.Count)
				{
					nIndx++;
					aRow = aqExcelValues.Dequeue();
					if (nIndx == 2)
						cRetVal.sChannel = aRow[0];
					if (nIndx == 3)
					{
						if (!DateTime.TryParseExact(aRow[0], "dd'.'MM'.'yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out cRetVal.dtDate))
						{
							cRetVal.dtDate = DateTime.MinValue;
							LogAndSend("Не удаётся прочитать дату из файла [file=" + sFile + "][date=" + aRow[0] + "]", "Ошибка при проверке файла VI" + Path.GetFileName(sFile), null, new Exception("Ошибка при проверке файла VI"));
						}
					}
					if (3 > aRow.Count)
						continue;
					sID = aRow[2].Trim();
					if (1 > sID.Length)
						continue;
					if ("Номер ролика" == sID)
						continue;

					sTime = aRow[0].Trim();
					sText = aRow[1].Trim();
					sType = aRow[4].Trim().ToLower();
					sCover = aRow[12].Trim().ToLower();

					if (0 < sTime.Length)
					{
						try
						{
							ts = DateTime.Parse(sTime).TimeOfDay;
						}
						catch
						{
							continue;
						}

						if (1 > sText.Length)
							throw new Exception("отсутствует название рекламного блока на: " + sTime);
						try
						{
							nBlockExcelStart = ts.Minutes;
							nBlockAdvertisementStart = 0;
							for (int nMinute = 10; 60 > nMinute; nMinute += 20)
							{
								if (Math.Abs(nMinute - nBlockExcelStart) < 10 && nBlockAdvertisementStart < nMinute)
									nBlockAdvertisementStart = nMinute;
							}
							tsPrevBlockTime = tsBlockTime;
							tsBlockTime = new TimeSpan(ts.Hours, nBlockAdvertisementStart, 0);
							if (tsPrevBlockTime != tsBlockTime)
							{
								while (tsBlockTime < tsPrevBlockTime)   // || cRetVal.DoesBlockExist(tsBlockTime)
									tsBlockTime = tsBlockTime.Add(new TimeSpan(0, 1, 0, 0));
							}
							if (!cRetVal.DoesBlockExist(tsBlockTime, sType, sCover))
								cRetVal.BlockAdd(tsBlockTime, sType, sCover);
						}
						catch
						{
							throw new Exception("неправильный формат времени старта блока: " + sTime);
						}
					}

					if (1 > sID.Length)
						throw new Exception("отсутствует VI ID рекламного ролика на {0}{1} часов {2} минут".Fmt((0 < tsBlockTime.Days ? tsBlockTime.Days + g.Helper.sDays : ""), tsBlockTime.Hours, tsBlockTime.Minutes));

					if (_ahVIBinds.ContainsKey(sID))
						cRetVal.BlockLastAssetAdd(_ahVIBinds[sID], ahClasses);
					else
						aMissedIDs.Add(sID);
				}
				if (0 < aMissedIDs.Count)
					LogAndSend("в файле [" + Path.GetFileName(sFile) + "] отсутствуют привязки индентификаторов VI к ассетам: \n", "Ошибка с файлом VI: " + Path.GetFileName(sFile), aMissedIDs, new Exception("Ошибка с файлом VI"));
				if (null == cRetVal || 1 > cRetVal.nBlocksQty)
					throw new Exception("не удалось найти подходящие ассеты в процессе анализа файла Video International");
			}
			else
				throw new Exception(g.Common.sErrorFileNotFound.ToLower() + ":" + sFile);
			return cRetVal;
		}
		private void LogAndSend(string sMessage, string sSubj)
		{
			LogAndSend(sMessage, sSubj, null);
		}
		private void LogAndSend(string sMessage, string sSubj, List<string> aList)
		{
			LogAndSend(sMessage, sSubj, aList, null);
		}
		private void LogAndSend(string sMessage, string sSubj, List<string> aList, Exception ex) 
		{
			if (null == aList)
				aList = new List<string>();

			foreach (string sStr in aList)  //aFiles = aFiles.Select(o => "\n" + o).ToList();
				sMessage += "\n" + sStr;

			if (null != ex)
			{
				new Logger("playlist").WriteNotice(sMessage + ex.Message);
				sMessage += "\n" + ex.Message;
			}
			else
				new Logger("playlist").WriteNotice(sMessage);

			if (!Preferences.sVIMailTargets.IsNullOrEmpty())
				new Logger("playlist").Email(Preferences.sVIMailTargets, sSubj, sMessage);
		}


		#endregion
		#region generate.common
		public void PlaylistGenerate(TimeSpan tsInterval)
		{
			DBInteract cDBI = new DBInteract();
#if !DEBUG
			cDBI.CommandQueueAdd("playlist_recalculate");
#endif
			List<Asset> aAss = PlayListGet(tsInterval);
			if (null == aAss || 0 >= aAss.Count)
				throw new Exception("not enough acceptable assets"); //TODO LANG

			Queue aqAssetsIDs = new Queue();
			foreach (Asset cAss in aAss)
				aqAssetsIDs.Enqueue(cAss.nID);
			cDBI.PlaylistItemsAdd(aqAssetsIDs);
#if !DEBUG
			cDBI.CommandQueueAdd("playlist_recalculate");
#endif
		}
		public List<Asset> PlayListGet(TimeSpan tsInterval)
		{
			switch (Preferences.sChannel)
			{
				case "autochannel":
					return PlayListForAUTOCHANNELGet(tsInterval);
				case "channel2":
					return PlayListForChannel2Get(tsInterval);
				default:
					(new Logger("playlist")).WriteError("нет такого канала! Укажите в настройках параметр 'channel' правильно");
					return PlayListForAUTOCHANNELGet(tsInterval);
			}
		}
		private DateTime StartTimeGet()
		{
			// на будущее, если надо будет узнавать, на какой ротации закончилось планирование...
			// SELECT "dtStopPlanned","idAssets" FROM pl."vPlayListResolved" WHERE "dtStopPlanned"=(SELECT max("dtStopPlanned") FROM pl."vPlayListResolved")
			// SELECT "nValue" FROM mam."tAssetAttributes" WHERE "idAssets"=640 AND "sKey"='rotation'
			DBInteract cDBI = new DBInteract();
			DateTime dtRetVal;
			DateTime dtNow = DateTime.Now;
			try
			{
				dtRetVal = cDBI.PlaylistItemsLastUsageGet();
				if (dtRetVal < dtNow || DateTime.MaxValue == dtRetVal)
					dtRetVal = dtNow;
			}
			catch
			{
				dtRetVal = dtNow;
			}
			return dtRetVal;
		}
		private Dictionary<string, List<Asset>> _ahBumpers;
		private Dictionary<string, int> _ahBumpersIndex;
		private Asset NextBumperGet(string sBumpersNameBeginning)
		{
			DBInteract cDBI = new DBInteract();
			if (null == _ahBumpers)
			{
				_ahBumpers = new Dictionary<string, List<Asset>>();
				_ahBumpersIndex = new Dictionary<string, int>();
			}
			if (!_ahBumpers.ContainsKey(sBumpersNameBeginning))
			{
				List<Asset> aBumpers = new List<Asset>();
				try
				{
					aBumpers.AddRange(cDBI.AssetsByNameBeginningGet(sBumpersNameBeginning));
					//Отбивка рекламная вход 1   //Отбивка рекламная выход 1
					if (aBumpers.Count > 0)
					{
						_ahBumpers.Add(sBumpersNameBeginning, aBumpers);
						_ahBumpersIndex.Add(sBumpersNameBeginning, -1);
					}
					else
						throw new Exception("нет таких отбивок, начинающихся с [" + sBumpersNameBeginning + "]");

					string sLastBumperName = cDBI.LastPlannedAssetNameGet(sBumpersNameBeginning);
					Asset cLastAsset = aBumpers.FirstOrDefault(o => o.sName == sLastBumperName);
					_ahBumpersIndex[sBumpersNameBeginning] = aBumpers.IndexOf(cLastAsset);
				}
				catch (Exception ex)
				{
					(new Logger("playlist")).WriteError("ошибка получения ассетов отбивок!\n", ex);
					return null;
				}
			}
			_ahBumpersIndex[sBumpersNameBeginning]++;
			return _ahBumpers[sBumpersNameBeginning][_ahBumpersIndex[sBumpersNameBeginning] % _ahBumpers[sBumpersNameBeginning].Count];
		}
#endregion
		#region generate.channel2
		public List<Asset> PlayListForChannel2Get(TimeSpan tsInterval)
		{    // заканчиваем всегда на отбивке
			DBInteract cDBI = new DBInteract();
			List<Asset> aRetVal = new List<Asset>();
			DateTime dtStart = StartTimeGet();
			DateTime dtStop = dtStart.Add(tsInterval);
			Queue<Clip> aqClips = cDBI.ClipsForPlaylistGet();
			if (aqClips == null || aqClips.Count < 1)
				return new List<Asset>();

			//VIPlaylist cVIPL = VideoInternationalFileParse(System.IO.Path.Combine(Preferences.sAdvertsPath, dtStart.ToString("dd.MM.yyyy") + ".xls"));

			Asset cASS;
			cASS = NextBumperGet("Отбивка рекламная вход");
			cASS = NextBumperGet("Отбивка рекламная выход");
			cASS = NextBumperGet("Отбивка рекламная вход");
			cASS = NextBumperGet("Отбивка рекламная вход");

			Rotations cRotations = new Rotations(aqClips);

			DateTime dtCurrent = dtStart;
            Asset cCurrentAsset = null;
			DateTime dtEndOfBlock;
			try
			{
				(new Logger("playlist")).WriteNotice("генерим с [" + dtStart.ToString("yyyy-MM-dd HH:mm:ss") + "] по [" + dtStop.ToString("yyyy-MM-dd HH:mm:ss") + "]");
				while (dtCurrent < dtStop)
				{
					(new Logger("playlist")).WriteDebug("dtcurrent [" + dtCurrent.ToString("yyyy-MM-dd HH:mm:ss") + "]");
					switch (WhatBlockStarts(dtCurrent, out dtEndOfBlock))
					{
						case 1:
							cCurrentAsset = cRotations.GetNextClip(Rotations.Type.first, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset);
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.third, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.second, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.fourth, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.third, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							while (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.fourth, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); }
							break;
						case 2:
							cCurrentAsset = cRotations.GetNextClip(Rotations.Type.first, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset);
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.second, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.third, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.second, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							while (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.fourth, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); }
							break;
						case 3:
							cCurrentAsset = cRotations.GetNextClip(Rotations.Type.first, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset);
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.third, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.second, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.fourth, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.third, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							if (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.second, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); } else break;
							while (!BlockIsFull(dtEndOfBlock, dtCurrent)) { cCurrentAsset = cRotations.GetNextClip(Rotations.Type.fourth, dtCurrent); dtCurrent = dtCurrent.AddMilliseconds(40 * (cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1)); aRetVal.Add(cCurrentAsset); }
							break;
					}
					aRetVal.AddRange(AdvertsBlockGet(dtCurrent, out dtCurrent));
				}
			}
			catch (Exception ex)
			{
				(new Logger("management")).WriteError("cannot add other assets.", ex);
			}
			return aRetVal;
		}
		private List<Asset> AdvertsBlockGet(DateTime dtCurrent, out DateTime dtEnd)
		{
			dtEnd = dtCurrent;
			List<Asset> aRetVal = new List<Asset>();
			List<Asset> aAdverts = new List<Asset>();
			Asset cBumper;
			long nFramesTotal = 0;
			if (_ahVIFilesChecked.ContainsKey(dtCurrent.Date))
				try
				{
					List<VIPlaylist.Block> aBlocks = _ahVIFilesChecked[dtCurrent.Date].ClosestBlocksGet(dtCurrent, 10);
					// здесь можно будет учитывать тип блока, если надо будет
					foreach (VIPlaylist.Block cB in aBlocks)
					{
						foreach (Asset cA in cB.aqAssets)
						{
							aAdverts.Add(cA);
							nFramesTotal += cA.nFrameOut - cA.nFrameIn + 1;
						}
					}
				}
				catch (Exception ex)
				{
					(new Logger("management")).WriteError("this block was not found or wrong! Will be partly added [count=" + aAdverts.Count + "]", ex);
				}

			if (0 < aAdverts.Count)
			{
				cBumper = NextBumperGet("Отбивка рекламная вход"); nFramesTotal += cBumper.nFrameOut - cBumper.nFrameIn + 1; aRetVal.Add(cBumper);
				aRetVal.AddRange(aAdverts);
				cBumper = NextBumperGet("Отбивка рекламная выход"); nFramesTotal += cBumper.nFrameOut - cBumper.nFrameIn + 1; aRetVal.Add(cBumper);
			}
			else
			{
				cBumper = NextBumperGet("Бампер");
				nFramesTotal += cBumper.nFrameOut - cBumper.nFrameIn + 1;
				aRetVal.Add(cBumper);
			}
			dtEnd = dtCurrent.AddMilliseconds(40 * nFramesTotal);
			return aRetVal;
		}
		private int WhatBlockStarts(DateTime dtCurrent, out DateTime dtEndOfBlock)
		{
			int nMinute = dtCurrent.Minute;
			if (nMinute >= 0 && nMinute < 20)
			{
				dtEndOfBlock = new DateTime(dtCurrent.Year, dtCurrent.Month, dtCurrent.Day, dtCurrent.Hour, 30, 0);
				return 1;
			}
			if (nMinute >= 20 && nMinute < 40)
			{
				dtEndOfBlock = new DateTime(dtCurrent.Year, dtCurrent.Month, dtCurrent.Day, dtCurrent.Hour, 50, 0);
				return 2;
			}
			dtEndOfBlock = (new DateTime(dtCurrent.Year, dtCurrent.Month, dtCurrent.Day, dtCurrent.Hour, 10, 0)).AddHours(1);
			return 3;
		}
		private bool BlockIsFull(DateTime dtEndOfBlock, DateTime dtCurrent)
		{
			if (dtCurrent.AddMinutes(1) >= dtEndOfBlock)
				return true;
			else
				return false;
		}
#endregion
		#region generate.autochannel
		public List<Asset> PlayListForAUTOCHANNELGet(TimeSpan tsInterval)
		{
			DBInteract cDBI = new DBInteract();
			List<Asset> aRetVal = new List<Asset>();
			DateTime dtStart = StartTimeGet();
			DateTime dtStop = dtStart.Add(tsInterval);
            Queue<Clip> aqClips = cDBI.ClipsForPlaylistGet();
            if (aqClips == null || aqClips.Count < 1)
                return new List<Asset>();
            Rotations cRotations = new Rotations(aqClips);

            DateTime dtCurrent = dtStart;
            int nHour = 0;  // здесь час НЕ календарный, а от начала (dtStart)
			long nDelta;
            int nTotalIndex = 0;
            Asset cCurrentAsset = null;
            while (dtCurrent < dtStop)
            {
                nTotalIndex++;
                if (0 == nTotalIndex % 4)
                    cCurrentAsset = NextBumperGet("Отбивка");
                else
                    cCurrentAsset = cRotations.GetNextClip(cRotations.CurrentRotationNumberGet(nTotalIndex), dtCurrent);   // (nIndexInHour++))
                if (null == cCurrentAsset)
                    continue;
                nDelta = cCurrentAsset.nFrameOut - cCurrentAsset.nFrameIn + 1;
                dtCurrent = dtCurrent.AddMilliseconds(nDelta * 40);
                if (nHour < (int)dtCurrent.Subtract(dtStart).TotalHours)
                    nHour++;
                aRetVal.Add(cCurrentAsset);
            }
            return aRetVal;
        }
#endregion
		#endregion
	}
}
