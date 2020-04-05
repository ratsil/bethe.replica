using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using helpers;
using helpers.replica.media;
using helpers.replica.pl;
using helpers.replica.mam;
using helpers.extensions;
using System.Net;
using System.Collections;
using SIO=System.IO;
using System.Linq;

namespace replica.sync
{
    public partial class Service : ServiceBase
    {
		private ManualResetEvent _mreWatcherCacheStopping;
		private ManualResetEvent _mreWatcherCacheStopped;
		private ManualResetEvent _mreWatcherCommandStopping;
		private ManualResetEvent _mreWatcherCommandStopped;
		private ManualResetEvent _mreWatcherPreviewStopping;
		private ManualResetEvent _mreWatcherPreviewStopped;
		private ManualResetEvent _mreWatcherStorageStopping;
		private ManualResetEvent _mreWatcherStorageStopped;
        private ManualResetEvent _mreWatcherClearingStorageStopping;
        private ManualResetEvent _mreWatcherClearingStorageStopped;

        private bool _bAbortWatchers;
        private string _sFilePauseCopying;
        private CopyFileExtended _cCurrentCopying;
        private object _oLockCopying;
        private CopyFileExtended _cCurrentMoving;
        private object _oLockMoving;

        public Service()
        {
            InitializeComponent();
            _oLockCopying = new object();
            _oLockMoving = new object();
            _ahPLDurations = new Dictionary<long, long>();
        }

		protected override void OnStart(string[] args)
		{
			base.OnStart(args);
			try
			{
				(new Logger("sync")).WriteWarning("получен сигнал на запуск");//TODO LANG
                _bAbortWatchers = false;
#if !DEBUG
                if (Preferences.nAffinity != IntPtr.Zero)
                    System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity = Preferences.nAffinity;
                if (false) // команд нет пока
                {
                    _mreWatcherCommandStopping = new ManualResetEvent(false);
                    _mreWatcherCommandStopped = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem((object o) => { WatcherCommand(); });
                }
#endif
                if (null != Preferences.cCache)
                {
                    _sFilePauseCopying = SIO.Path.Combine(Preferences.cCache.sFolder, SyncConstants.sFilePauseCopying);
                    if (SIO.File.Exists(_sFilePauseCopying))
                        SIO.File.Move(_sFilePauseCopying, _sFilePauseCopying + "!");
                    if (!SIO.File.Exists(_sFilePauseCopying + "!"))
                    {
                        string sText = "# This file 'PAUSE_COPYING' flags the sync service to pause copying (sync checks every 3 seconds)\n";
                        sText += "# After sync service restarting this file renames to 'PAUSE_COPYING!'\n";
                        SIO.File.WriteAllText(_sFilePauseCopying + "!", sText);
                    }
                    _mreWatcherCacheStopping = new ManualResetEvent(false);
                    _mreWatcherCacheStopped = new ManualResetEvent(false);
					ThreadPool.QueueUserWorkItem((object o) => { WatcherCache(); });
				}
				if (null != Preferences.cStorage)
				{
					_mreWatcherStorageStopping = new ManualResetEvent(false);
					_mreWatcherStorageStopped = new ManualResetEvent(false);
					ThreadPool.QueueUserWorkItem((object o) => { WatcherStorage(); });

                    _mreWatcherClearingStorageStopping = new ManualResetEvent(false);
                    _mreWatcherClearingStorageStopped = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem((object o) => { WatcherClearingStorage(); });
                }
                if (null != Preferences.cPreview)
				{
					ffmpeg.net.Logger.eLevel = Logger.Level.notice;
					_mreWatcherPreviewStopping = new ManualResetEvent(false);
					_mreWatcherPreviewStopped = new ManualResetEvent(false);
					ThreadPool.QueueUserWorkItem((object o) => { WatcherPreview(); });
				}
     //           _mreWatcherCommandStopped.Reset();
     //           if (null != _mreWatcherCacheStopped)
					//_mreWatcherCacheStopped.Reset();
     //           if (null != _mreWatcherStorageStopped)
     //               _mreWatcherStorageStopped.Reset();
     //           if (null != _mreWatcherPreviewStopped)
					//_mreWatcherPreviewStopped.Reset();

				_aFilesNotFromIngest = new LinkedList<string>();
			}
			catch (Exception ex)
			{
				(new Logger("sync")).WriteError(ex);
			}
		}

		public void TestStart()
		{
			OnStart(null);
		}




		private LinkedList<string> _aFilesNotFromIngest;
        private Dictionary<string, DateTime> _ahFilesChanged;
        private Dictionary<long, long> _ahPLDurations;
        protected override void OnStop()
		{
			try
            {
                (new Logger("sync")).WriteWarning("получен сигнал на остановку");//TODO LANG
                _bAbortWatchers = true;
                if (null != _mreWatcherCommandStopping)
                    _mreWatcherCommandStopping.Set();
                if (null != _mreWatcherCacheStopping)
                {
                    _mreWatcherCacheStopping.Set();
                    lock (_oLockCopying)
                        if (_cCurrentCopying != null)
                            _cCurrentCopying.Cancel();
                }
                if (null != _mreWatcherStorageStopping)
                {
                    _mreWatcherStorageStopping.Set();
                }
                if (null != _mreWatcherClearingStorageStopping)
                {
                    _mreWatcherClearingStorageStopping.Set();
                    lock (_oLockMoving)
                        if (_cCurrentMoving != null)
                            _cCurrentMoving.Cancel();
                }
                if (null != _mreWatcherPreviewStopping)
					_mreWatcherPreviewStopping.Set();

                if (null != _mreWatcherCommandStopped)
                    _mreWatcherCommandStopped.WaitOne(15000, false);
                if (null != _mreWatcherPreviewStopped)
                    _mreWatcherPreviewStopped.WaitOne(15000, false);
				if (null != _mreWatcherStorageStopped)
					_mreWatcherStorageStopped.WaitOne(15000, false);
                if (null != _mreWatcherClearingStorageStopped)
                    _mreWatcherClearingStorageStopped.WaitOne(15000, false);
                if (null != _mreWatcherCacheStopped)
                    _mreWatcherCacheStopped.WaitOne(15000, false);
            }
            catch (Exception ex)
			{
				(new Logger("sync")).WriteError(ex);
            }
            finally
            {
                (new Logger("sync")).WriteNotice("сервис остановлен");//TODO LANG
                while (Logger.nQueueLength > 0)
                    Thread.Sleep(1);
            }
        }

        private void WatcherCommand()  //модуль управления командами
        {
            try
            {
                (new Logger("WatcherCommand")).WriteNotice("модуль управления командами запущено");//TODO LANG
                return; // команды пустые пока !!!!



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
                        (new Logger("WatcherCommand")).WriteError(ex); //UNDONE
                    }
                } while (!_mreWatcherCommandStopping.WaitOne(Preferences.cCommands.tsCommandsSleepDuration, false));
            }
            catch (Exception ex)
            {
                (new Logger("WatcherCommand")).WriteError(ex); //UNDONE
            }
            finally
            {
                (new Logger("WatcherCommand")).WriteNotice("модуль управления командами остановлено");//TODO LANG
                if (null != _mreWatcherCommandStopped)
                    _mreWatcherCommandStopped.Set();
            }
		}
		private void WatcherCache()  //модуль кэширования
        {
            try
            {
                (new Logger("WatcherCache")).WriteNotice("модуль кэширования запущен");//TODO LANG
                DBInteract cDBI = new DBInteract();
                _ahFilesChanged = new Dictionary<string, DateTime>();
                DateTime dtNextDelete = DateTime.Now;
                do
                {
                    try
                    {
                        if (DateTime.Now >= dtNextDelete)
                        {
                            CacheStorageDeleteOldFiles();
                            dtNextDelete = DateTime.Now.AddMinutes(10);
                        }
                        CacheStorageFill();
                    }
                    catch (Exception ex)
                    {
                        (new Logger("WatcherCache")).WriteError(ex); //UNDONE
                    }
                    System.Threading.Thread.Sleep(1);
                } while (!_mreWatcherCacheStopping.WaitOne(Preferences.cCache.tsSleepDuration, false));
            }
            catch (Exception ex)
            {
                (new Logger("WatcherCache")).WriteError(ex); //UNDONE
            }
            finally
            {
                (new Logger("WatcherCache")).WriteNotice("модуль кэширования остановлен");//TODO LANG
                if (null != _mreWatcherCacheStopped)
                    _mreWatcherCacheStopped.Set();
            }
		}
		private void WatcherStorage()  //модуль синхронизации
        {
            try
            {
                (new Logger("WatcherStorage")).WriteNotice("модуль синхронизации запущен, ищем БД...");//TODO LANG
                DBInteract cDBI;
                do
                    try
                    {
                        cDBI = new DBInteract();
                    }
                    catch (Exception ex)
                    {
                        (new Logger()).WriteError(ex);
                        cDBI = null;
                        Thread.Sleep(100);
                    }
                while (null == cDBI);
                (new Logger("WatcherStorage")).WriteNotice("connected to DB");

                do
                {
                    try
                    {
                        StoragesSync();
                    }
                    catch (Exception ex)
                    {
                        (new Logger()).WriteError(ex); //UNDONE
                    }
                    System.Threading.Thread.Sleep(1);
                } while (!_mreWatcherStorageStopping.WaitOne(new TimeSpan(0, 0, 10), false));
            }
            catch (Exception ex)
            {
                (new Logger("WatcherStorage")).WriteError(ex); //UNDONE
            }
            finally
            {
                (new Logger("WatcherStorage")).WriteNotice("модуль синхронизации остановлен");//TODO LANG
                if (null != _mreWatcherStorageStopped)
                    _mreWatcherStorageStopped.Set();
            }
		}
        private void WatcherClearingStorage()  //модуль удаления/переноса лишних файлов
        {
            try
            {
                (new Logger("WatcherClearingStorage")).WriteNotice("модуль синхронизации запущен, ищем БД...");//TODO LANG
                DBInteract cDBI;
                do
                    try
                    {
                        cDBI = new DBInteract();
                    }
                    catch (Exception ex)
                    {
                        (new Logger()).WriteError(ex);
                        cDBI = null;
                        Thread.Sleep(1000);
                    }
                while (null == cDBI);

                (new Logger("WatcherClearingStorage")).WriteNotice("connected to DB");
                DateTime dtNow = DateTime.Now;
                DateTime dtMoveStorage = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 30, 0);
                if (dtNow > dtMoveStorage)
                    dtMoveStorage = dtMoveStorage.AddDays(1);
                do
                {
                    try
                    {
                        if (DateTime.Now > dtMoveStorage)
                        {
                            dtMoveStorage = dtMoveStorage.AddDays(1);
                            StoragesMoveDelete();
                        }
                    }
                    catch (Exception ex)
                    {
                        (new Logger()).WriteError(ex); //UNDONE
                    }
                    System.Threading.Thread.Sleep(1);
                } while (!_mreWatcherClearingStorageStopping.WaitOne(new TimeSpan(0, 1, 0), false));
            }
            catch (Exception ex)
            {
                (new Logger("WatcherClearingStorage")).WriteError(ex); //UNDONE
            }
            finally
            {
                (new Logger("WatcherClearingStorage")).WriteNotice("модуль синхронизации остановлен");//TODO LANG
                if (null != _mreWatcherClearingStorageStopped)
                    _mreWatcherClearingStorageStopped.Set();
            }
        }
        private void WatcherPreview()  //модуль обеспечения предпросмотра
        {
            try
            {
                (new Logger("WatcherPreview")).WriteNotice("модуль обеспечения предпросмотра запущен");//TODO LANG
                DBInteract cDBI;
                string sFilePreview;
                DateTime dtModified;
                do
                {
                    try
                    {
                        cDBI = new DBInteract();

                        Dictionary<long, File> ahFiles = cDBI.FilesGet();

                        if (null != ahFiles)
                        {
                            foreach (File cFile in ahFiles.Values)
                            {
                                if (!SIO.File.Exists(cFile.sFile))
                                    continue;
                                sFilePreview = cFile.cStorage.sPath + "/" + Preferences.cPreview.sFolder;
                                if (!SIO.File.Exists(sFilePreview))
                                    SIO.Directory.CreateDirectory(sFilePreview);
                                sFilePreview += "/" + cFile.sFilename;
                                dtModified = SIO.File.GetLastWriteTimeUtc(cFile.sFile);
                                if (!SIO.File.Exists(sFilePreview) || (SIO.File.GetLastWriteTimeUtc(sFilePreview) != dtModified) || 1 > (new SIO.FileInfo(sFilePreview)).Length)
                                {
                                    try
                                    {
                                        VideoConvert(cFile.sFile, sFilePreview);
                                        SIO.File.SetLastWriteTime(sFilePreview, dtModified);
                                        (new Logger("WatcherPreview")).WriteNotice("создан файл предварительного просмотра [" + sFilePreview + "]");//TODO LANG
                                    }
                                    catch (Exception ex)
                                    {
                                        (new Logger("WatcherPreview")).WriteError(new Exception("ошибка создания файла предварительного просмотра [" + sFilePreview + "]", ex));
                                        try
                                        {
                                            if (SIO.File.Exists(sFilePreview))
                                                SIO.File.Delete(sFilePreview);
                                        }
                                        catch (Exception exx)
                                        {
                                            (new Logger("WatcherPreview")).WriteWarning("ошибка удаления файла [" + sFilePreview + "]", exx);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        (new Logger("WatcherPreview")).WriteError(ex); //UNDONE
                    }
                } while (!_mreWatcherPreviewStopping.WaitOne(Preferences.cPreview.tsSleepDuration, false));
            }
            catch (Exception ex)
            {
                (new Logger("WatcherPreview")).WriteError(ex); //UNDONE
            }
            finally
            {
                (new Logger("WatcherPreview")).WriteNotice("модуль обеспечения предпросмотра остановлен");//TODO LANG
                if (null != _mreWatcherPreviewStopped)
                    _mreWatcherPreviewStopped.Set();
            }
		}
		//-----
		public void StoragesMoveDelete()
        {
            DBInteract cDBI = new DBInteract();
            Dictionary<long, File> ahFiles = cDBI.FilesGet();
            Queue<PlaylistItem> ahPLIs = cDBI.PlaylistItemsPlanGet(DateTime.Now, DateTime.Now.AddYears(1));
            (new Logger("StoragesMoveDelete")).WriteDebug("got files and PL [files=" + (ahFiles == null ? "null" : "" + ahFiles.Count) + "][PLIs=" + (ahPLIs == null ? "null" : "" + ahPLIs.Count) + "]");
            DateTime dtFileCreation;
            DateTime dtNow = DateTime.Now;
            DateTime dtMoveStorageStop = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 12, 30, 0);
            DateTime dtLastUsge;
            bool bDelete;
            int nAge;			   // >0 will move file;    <0 will delete file;     ==0 do nothing;
            int nCount = ahFiles.Count;
            int nDaysTimeout;
            PlaylistItem cPLI;
            string sTrashFolder;
            string sTrashFilename;
            string sMoveFilename;
            string sMailLogDeleted = "";
            string sMailLogMoved = "";

            ClearAllTrashFolders(cDBI.StoragesGet().ToArray());

            foreach (File cF in ahFiles.Values)
            {
                if (_bAbortWatchers) return;
                try
                {
#if !DEBUG
                    if (DateTime.Now > dtMoveStorageStop)
                    {
                        (new Logger("StoragesMoveDelete")).WriteDebug("it's time to stop: [" + dtMoveStorageStop.ToString("yyyy-MM-dd HH:mm:ss") + "][files_left=" + nCount + "][files_total=" + ahFiles.Count + "]");
                        break;
                    }
#endif
                    nCount--;

                    if (cF.eStatus == File.Status.InStock && cF.nAge != 0 && cF.nAge > int.MinValue)  //  MinValue  -  это ролики, пришедшие в обход КПП  
					{
						if (cF.nAge < 0)
						{
							bDelete = true;
							nAge = -1 * cF.nAge;
						}
						else
						{
							bDelete = false;
							nAge = cF.nAge;
						}
						dtFileCreation = SIO.File.GetCreationTime(cF.sFile);
						if (dtFileCreation.AddMonths(nAge) < DateTime.Now)
						{
							if (!SIO.File.Exists(cF.sFile))
							{
								(new Logger("StoragesMoveDelete")).WriteWarning("Cant DELETE or MOVE file. No File! [file=" + cF.sFile + "][delete=" + bDelete + "]");
								cF.StatusSet(File.Status.Deleted);
								cDBI.FileErrorSet(cF);
							}
							else if (null != (cPLI = ahPLIs.FirstOrDefault(o => o.cFile.sFile == cF.sFile)))  // o.cAsset.cFile.sFile
							{
								(new Logger("StoragesMoveDelete")).WriteNotice("Cant DELETE or MOVE file: It's in current Playlist! [pli=" + cPLI.nID + "][start=" + cPLI.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][file=" + cF.sFile + "]");
                            }
                            else if ((dtLastUsge = FileNotTimeOutLastUsageGet(cDBI, cF, out nDaysTimeout)) > DateTime.MinValue)
                            {
                                (new Logger("StoragesMoveDelete")).WriteNotice("Cant DELETE or MOVE file. It's LastEvent date is not timed out! [last_pl_usage=" + dtLastUsge.ToString("yyyy-MM-dd HH:mm:ss") + "][timeout=" + nDaysTimeout + " days][timeout_left=" + (nDaysTimeout - (int)DateTime.Now.Subtract(dtLastUsge).TotalDays) + "][last_event=" + cF.dtLastEvent.ToString("yyyy-MM-dd HH:mm:ss") + "][file=" + cF.sFile + "]");
                            }
							else
                            {
                                if (bDelete)
                                {
                                    sTrashFolder = SIO.Path.Combine(SIO.Path.GetDirectoryName(cF.sFile), Preferences.cStorage.sTrashFolder);
                                    if (!SIO.Directory.Exists(sTrashFolder))
                                        SIO.Directory.CreateDirectory(sTrashFolder);
                                    sTrashFilename = SIO.Path.Combine(sTrashFolder, SIO.Path.GetFileName(cF.sFile));
                                    (new Logger("StoragesMoveDelete")).WriteNotice("Deleting file to trash [" + sTrashFolder + "] (out of date [" + cF.nAge + " months]) [file=" + cF.sFile + "][creation=" + dtFileCreation.ToString("yyyy-MM-dd HH:mm:ss") + "][timeout=" + nDaysTimeout + " days][last_event=" + cF.dtLastEvent.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                    SIO.FileInfo cFI = new SIO.FileInfo(cF.sFile);
                                    cFI.LastWriteTime = DateTime.Now;
                                    SIO.File.Move(cF.sFile, sTrashFilename);
                                    cF.StatusSet(File.Status.Deleted);
                                    if (cF.eError != helpers.replica.Error.no)
                                        cDBI.FileErrorRemove(cF);
                                    sMailLogDeleted += "was deleted: [file=" + cF.sFilename + "]\n";
                                }
                                else if (null != Preferences.cStorage.sMoveToFolder)
                                {
                                    (new Logger("StoragesMoveDelete")).WriteNotice("Moving file (out of date [" + cF.nAge + " months]) [file=" + cF.sFile + "][creation=" + dtFileCreation.ToString("yyyy-MM-dd HH:mm:ss") + "][to=" + Preferences.cStorage.sMoveToFolder + "][timeout=" + nDaysTimeout + " days][last_event=" + cF.dtLastEvent.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                    if (SIO.Directory.Exists(Preferences.cStorage.sMoveToFolder))
                                    {
                                        sMoveFilename = SIO.Path.Combine(Preferences.cStorage.sMoveToFolder, cF.sFilename);

                                        lock (_oLockMoving)
                                            _cCurrentMoving = new CopyFileExtended(cF.sFile, sMoveFilename + "!", 0, 0, 0);  // медленное копирование 
                                        if (_bAbortWatchers) return;
                                        _cCurrentMoving.DoCopy2(); // из-за отработки onStop
                                        if (_bAbortWatchers && !_cCurrentMoving.bCompleted)
                                            return;

                                        if (SIO.File.Exists(sMoveFilename))
                                        {
                                            SIO.File.Delete(sMoveFilename);
                                            (new Logger("StoragesMoveDelete")).WriteWarning("File was in target folder - was deleted [" + sMoveFilename + "]");
                                        }
                                        SIO.File.Move(sMoveFilename + "!", sMoveFilename);
                                        (new Logger("StoragesMoveDelete")).WriteNotice("File moved [" + sMoveFilename + "]");
                                        SIO.File.Delete(cF.sFile);
                                        cF.StatusSet(File.Status.MovedToTape);
                                        if (cF.eError != helpers.replica.Error.no)
                                            cDBI.FileErrorRemove(cF);
                                        sMailLogMoved += "was moved: [file=" + cF.sFilename + "]\n";
                                    }
                                    else
                                        (new Logger("StoragesMoveDelete")).WriteError("Cant MOVE file. DESTINATION WAS NOT FOUND! [file=" + cF.sFile + "][dest=" + Preferences.cStorage.sMoveToFolder + "]");
                                }
                                else
                                    (new Logger("StoragesMoveDelete")).WriteError("Cant MOVE file. NO DESTINATION! [file=" + cF.sFile + "][dest=" + Preferences.cStorage.sMoveToFolder + "]");
                            }
                        }
                    }
				}
				catch (Exception ex)
				{
					(new Logger("StoragesMoveDelete")).WriteError("file=" + (cF == null ? "null" : cF.sFilename)+"[lastevent="+ cF.dtLastEvent + "]", ex); //UNDONE
                }
            }
            if (sMailLogDeleted.Length > 0 || sMailLogMoved.Length > 0)
                Logger.Email(Preferences.cStorage.sOnFileReplaceMailRecipients, "Файлы были автоматически удалены или перенесены", sMailLogDeleted + "\n" + sMailLogMoved);
            (new Logger("StoragesMoveDelete")).WriteNotice("Stop moving/deleting");
        }
        private DateTime FileNotTimeOutLastUsageGet(DBInteract cDBI, File cF, out int nTimeoutCurrent)
        {
            nTimeoutCurrent = Preferences.cStorage.nDeleteMoveTimeoutDefault;
            if (!Preferences.cStorage.ahDeleteMoveTimeoutsByStorages.IsNullOrEmpty() && Preferences.cStorage.ahDeleteMoveTimeoutsByStorages.ContainsKey(cF.cStorage.sName))
                nTimeoutCurrent = Preferences.cStorage.ahDeleteMoveTimeoutsByStorages[cF.cStorage.sName];

            DateTime dtRetVal = cDBI.FileLastUsageInPlaylistGet(cF, DateTime.Now.AddDays(-nTimeoutCurrent), DateTime.Now.AddHours(1));
            if (dtRetVal == DateTime.MaxValue)
                dtRetVal = DateTime.MinValue;

            return dtRetVal;
        }
        private void ClearAllTrashFolders(Storage[] aStorages)
        {
            string sTrashPath = null;
            DateTime dtMaximum = DateTime.Now.AddDays(-10);
            foreach (Storage cS in aStorages)
            {
                try
                {
                    sTrashPath = SIO.Path.Combine(cS.sPath, Preferences.cStorage.sTrashFolder);
                    if (!SIO.Directory.Exists(sTrashPath))
                        continue;
                    foreach (SIO.FileSystemInfo cFSInf in (new SIO.DirectoryInfo(sTrashPath)).GetFileSystemInfos())
                    {
                        if ((cFSInf.Attributes & SIO.FileAttributes.Directory) > 0)
                            continue;
                        if (dtMaximum < cFSInf.LastWriteTime)
                            continue;
                        cFSInf.Delete();
                        (new Logger("ClearAllTrashFolders")).WriteNotice("Файл удалён из корзины: " + cFSInf.FullName + " [last_write=" + cFSInf.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + "]");//TODO LANG
                    }
                }
                catch (Exception ex)
                {
                    (new Logger("ClearAllTrashFolders")).WriteError("clearing trash [sTrashPath=" + sTrashPath + "]", ex); //UNDONE
                }
            }
        }
        public bool StoragesSync()
		{
			if (null== _aFilesNotFromIngest)
				_aFilesNotFromIngest = new LinkedList<string>();
			(new Logger("StoragesSync")).WriteDebug("storage sync in");
			Storage cStorage;
			string sName;
			try
			{
				//Thread.Sleep(10000);
				DBInteract cDBI;
				try
				{
					cDBI = new DBInteract();
				}
				catch (Exception ex)
				{
					(new Logger("StoragesSync")).WriteError(ex);
					cDBI = null;
				}

				Queue<Storage> aqStorages = null;

				if (null != (aqStorages = cDBI.StoragesGet()))
				{
					(new Logger("StoragesSync")).WriteDebug("папки получены = " + aqStorages.Count);
					File[] aFilesUnused = cDBI.FilesUnusedGet();
					(new Logger("StoragesSync")).WriteDebug("неиспользованные файлы получены = " + (null == aFilesUnused ? "null" : "" + aFilesUnused.Length));
                    if (_bAbortWatchers) return false;
                    Dictionary<long, File> ahFiles = cDBI.FilesGet();
					(new Logger("StoragesSync")).WriteDebug("файлы получены = " + (null == ahFiles ? "null" : "" + ahFiles.Count));
                    if (_bAbortWatchers) return false;
                    Dictionary<long, List<Asset>> ahFileIds_Assets = cDBI.AssetsFastGet();
					(new Logger("StoragesSync")).WriteDebug("ассеты получены = " + (null == ahFileIds_Assets ? "null" : "" + ahFileIds_Assets.Count));
                    if (_bAbortWatchers) return false;
                    if (ahFileIds_Assets.IsNullOrEmpty())
						(new Logger("StoragesSync")).WriteWarning("ассеты не получены! = " + (null == ahFileIds_Assets ? "null" : "" + ahFileIds_Assets.Count));
					Dictionary<long, List<Asset>> ahFileIds_AssetsWaiting = new Dictionary<long, List<Asset>>();    //aAssetsWaiting.ToDictionary(o => o.cFile.nID, o => o);
					foreach(long nFID in ahFileIds_Assets.Keys)
					{
						foreach(Asset cA in ahFileIds_Assets[nFID])
						{
							if (cA.cFile != null && (cA.nFrameIn == long.MaxValue || cA.nFrameOut == long.MaxValue || cA.nFramesQty == long.MaxValue || cA.nFrameIn < 0 || cA.nFrameOut < 0 || cA.nFramesQty < 0))
							{
								if (!ahFileIds_AssetsWaiting.ContainsKey(nFID))
									ahFileIds_AssetsWaiting.Add(nFID, new List<Asset>() { cA });
								else
									ahFileIds_AssetsWaiting[nFID].Add(cA);
							}
						}
					}
					(new Logger("StoragesSync")).WriteDebug("только что добавленные ассеты получены = " + (null == ahFileIds_AssetsWaiting ? "null" : "" + ahFileIds_AssetsWaiting.Count));
                    if (_bAbortWatchers) return false;

                    Dictionary<long, Dictionary<string, File>> ahDBFiles = aqStorages.ToDictionary(o => o.nID, o => new Dictionary<string, File>());
					foreach(long nID in ahFiles.Keys)
						ahDBFiles[ahFiles[nID].cStorage.nID].Add(ahFiles[nID].sFilename.ToLower(), ahFiles[nID]);
					File cFile;
					byte nFPS;
					long nQty;
                    File.Status eStatusPrevious;
                    string sMailInfo;
					while (0 < aqStorages.Count)
					{
						cStorage = aqStorages.Dequeue();
						(new Logger("StoragesSync")).WriteDebug("обрабатываем папку = " + cStorage.sName + "[path = " + cStorage.sPath + "]" + " [count_bd = " + ahDBFiles[cStorage.nID].Count + "]");
						if (SIO.Directory.Exists(cStorage.sPath))
						{
							foreach (SIO.FileInfo cFI in (new SIO.DirectoryInfo(cStorage.sPath)).GetFiles())
							{
                                if (_bAbortWatchers) return false;
                                if (cFI.Name.StartsWith(".") || cFI.Name.EndsWith("!"))
									continue;
								sName = cFI.Name.ToLower();
                                if (ahDBFiles[cStorage.nID].ContainsKey(sName))
								{
									cFile = ahDBFiles[cStorage.nID][sName];

									if (_aFilesNotFromIngest.Contains(sName))
										_aFilesNotFromIngest.Remove(sName);

                                    sMailInfo = null;
                                    eStatusPrevious = cFile.eStatus;
                                    if (cFile.eError == helpers.replica.Error.unknown)
                                    {
                                        (new Logger("StoragesSync")).WriteWarning("файл имеет пометку 'unknown error' попробуем его опять проверить! [name=" + sName + "][error=" + cFile.eError + "][modification=" + cFI.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + "][creation=" + cFI.CreationTime.ToString("yyyy-MM-dd HH:mm:ss") + "][last_event=" + cFile.dtLastEvent.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                        cFile.eStatus = File.Status.Waiting;
                                        if (null != ahFileIds_AssetsWaiting && ahFileIds_Assets.ContainsKey(cFile.nID))
                                        {
                                            if (!ahFileIds_AssetsWaiting.ContainsKey(cFile.nID))
                                                ahFileIds_AssetsWaiting.Add(cFile.nID, ahFileIds_Assets[cFile.nID]);
                                            else
                                                ahFileIds_AssetsWaiting[cFile.nID] = ahFileIds_Assets[cFile.nID];
                                        }
                                    }
                                    if (cFI.ModificationCreationDateLast() > cFile.dtLastEvent && ahFileIds_Assets.ContainsKey(cFile.nID))
                                    {
                                        (new Logger("StoragesSync")).WriteNotice("файл изменился! [name=" + sName + "][error=" + cFile.eError + "][modification=" + cFI.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + "][creation=" + cFI.CreationTime.ToString("yyyy-MM-dd HH:mm:ss") + "][last_event=" + cFile.dtLastEvent.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                        cFile.eStatus = File.Status.Waiting;
                                        if (null != ahFileIds_AssetsWaiting)
                                        {
                                            if (!ahFileIds_AssetsWaiting.ContainsKey(cFile.nID))
                                                ahFileIds_AssetsWaiting.Add(cFile.nID, ahFileIds_Assets[cFile.nID]);
                                            else
                                                ahFileIds_AssetsWaiting[cFile.nID] = ahFileIds_Assets[cFile.nID];
										}
									}
									if (cFile.eStatus != File.Status.InStock)
									{
										try
										{
											if (cFile.eStatus == File.Status.Waiting && Preferences.cStorage.bUseFFMPEG) // т.е. прямо из КПП идёт и настройка разрешает
											{
                                                ffmpeg.net.File.Input cF = new ffmpeg.net.File.Input(cFile.sFile);
												nFPS = (byte)cF.nFramesPerSecond;
												nQty = (long)cF.nFramesQty;
												if (ahFileIds_AssetsWaiting.Keys.Contains(cFile.nID))
												{
                                                    if (null != sMailInfo)
                                                        sMailInfo += "Ассеты, соответствующие этому фалу:\n";
                                                    foreach (Asset cA in ahFileIds_AssetsWaiting[cFile.nID])
                                                    {
                                                        if (eStatusPrevious != File.Status.Waiting)
                                                            sMailInfo += "[is_in_playlist or in_first_100min_fragment = " + cDBI.FileCheckIsInPlaylist(cFile.nID, 100).ToString() + "]\n[asset_name = " + cA.sName + "]\n\t\t[dur_old = " + cA.nFramesQty.ToFramesString(true, false, false, false, false, false) + "]\n\t\t[dur_new = " + nQty.ToFramesString(true, false, false, false, false, false) + "]\n";
                                                        else
                                                            sMailInfo += "[asset_name = " + cA.sName + "][dur_new = " + nQty.ToFramesString(true, false, false, false, false, false) + "]\n";
                                                        cA.nFrameOut = cA.nFramesQty = nQty;
                                                        cA.nFrameIn = 1;
                                                        cA.FileSet();
														(new Logger("StoragesSync")).WriteNotice("к ассету добавили хронометраж [name=" + cA.sName + "][fqty=" + cA.nFramesQty + "]");
													}
												}
												FileChangeFormat(cFile, nFPS, cF.cFormatVideo.nWidth, cF.cFormatVideo.nHeight, cF.cFormatVideo.nAspectRatio_dividend, cF.cFormatVideo.nAspectRatio_divider, nQty);
												cF.Dispose();
                                            }
                                            if (!Preferences.cStorage.sOnFileReplaceMailRecipients.IsNullOrEmpty())
                                            {
                                                (new Logger("StoragesSync")).WriteDebug("рассылка писем по поводу изменения хронометража файла [name=" + sName + "]");
                                                if (eStatusPrevious != File.Status.Waiting)
                                                {
                                                    if (cFile.eError == helpers.replica.Error.unknown)
                                                        Logger.Email(Preferences.cStorage.sOnFileReplaceMailRecipients, "с файла была убрана пометка 'unknown error'", "с Файла [name=" + sName + "][status=" + eStatusPrevious.ToString() + "] \nбыла убрана пометка 'unknown error'\n\n" + sMailInfo);
                                                    else
                                                        Logger.Email(Preferences.cStorage.sOnFileReplaceMailRecipients, "Файл был изменен!", "Внимание! \nФайл [name=" + sName + "][status=" + eStatusPrevious.ToString() + "] \nбыл заменён другим файлом!\n\n" + sMailInfo);
                                                }
                                                else
                                                    Logger.Email(Preferences.cStorage.sOnFileReplaceMailRecipients, "Файл был добавлен", "Файл [name=" + sName + "][status=" + eStatusPrevious.ToString() + "] \nбыл добавлен\n\n" + sMailInfo);
                                            }
                                            cFile.StatusSet(File.Status.InStock);
                                            cDBI.FileErrorRemove(cFile);
                                            cFile.LastFileEventUpdate();
                                        }
										catch (Exception ex)
										{
											(new Logger("StoragesSync")).WriteError("unknown error found: [file=" + cFile.sFile + "]    ", ex);
                                            if (!Preferences.cStorage.sOnFileReplaceMailRecipients.IsNullOrEmpty())
                                                Logger.Email(Preferences.cStorage.sOnFileReplaceMailRecipients, "Файл был помечен 'unknown error'!", "Внимание! \nФайл [name=" + sName + "][status=" + eStatusPrevious.ToString() + "][error=" + cFile.eError + "] \nбыл помечен 'unknown error'!\n\n" + sMailInfo);
                                            cFile.eError = helpers.replica.Error.unknown;
											cDBI.FileErrorSet(cFile); //UNDONE
										}
									}
									if (helpers.replica.Error.missed == cFile.eError)
									{
                                        //cFile.FileModificationUpdate(cFI.ModificationCreationDateLast()); // заполнить в первый раз и удалить эту строку ))...  бессмысленно, т.к. мд заменяется при копировании файла через интерфейс...
                                        cDBI.FileErrorRemove(cFile);
										(new Logger("StoragesSync")).WriteNotice("файл появился на диске [id:" + cFile.nID + "][file:" + cFile.sFile + "]"); //TODO LANG
									}
									ahFiles.Remove(cFile.nID);
								}
								else
								{
									if (Preferences.cStorage.bAddFreeFiles)
										(new Logger("StoragesSync")).WriteNotice("добавлен файл, НЕ прошедший ЧЕРЕЗ КПП ДОБАВЛЯЕМ В БД [" + cDBI.FileAdd(cStorage.nID, sName).sFile + "]"); //TODO LANG
									else
									{
										if (!_aFilesNotFromIngest.Contains(sName))
										{
											_aFilesNotFromIngest.AddLast(sName);
											(new Logger("StoragesSync")).WriteError("обнаружен файл, НЕ прошедший ЧЕРЕЗ КПП!!!! Он НЕ будет добавлен в БД [" + sName + "]"); //TODO LANG
										}
									}
								}
							}
							(new Logger("StoragesSync")).WriteDebug3("обработали папку = " + cStorage.sName + " [count_files = " + ahFiles.Count + "]");
						}//UNDONE mark storage as failed
						else
							(new Logger("StoragesSync")).WriteDebug("папка НЕ НАЙДЕНА!!! = " + cStorage.sName + "[path = "+ cStorage.sPath + "]");
					}
					(new Logger("StoragesSync")).WriteDebug("обрабатываем отсутствующие файлы [count_missed_files = " + ahFiles.Count + "]");
					foreach (long nID in ahFiles.Keys)
                    {
                        if (_bAbortWatchers) return false;
                        if (SIO.Directory.Exists(ahFiles[nID].cStorage.sPath))
                        {
                            if (null == aFilesUnused || 1 > aFilesUnused.Count(row => row.nID == nID))
							{
								if (helpers.replica.Error.missed != ahFiles[nID].eError)
								{
									cFile = ahFiles[nID];
									cFile.eError = helpers.replica.Error.missed;
									cDBI.FileErrorSet(cFile); //UNDONE
									(new Logger("StoragesSync")).WriteNotice("файл отсутствует на диске [id:" + nID + "][file:" + ahFiles[nID].sFile + "]"); //TODO LANG
								}
							}
							else if (ahFiles[nID].eStatus == File.Status.InStock && ahFiles[nID].eError == helpers.replica.Error.no) // если файл не использован в ассетах и нет на диске И он "в строю" формально
							{
								ahFiles[nID].eError = helpers.replica.Error.missed;
								cDBI.FileErrorSet(ahFiles[nID]);
								(new Logger("StoragesSync")).WriteWarning("не найден файл ни надиске, ни в ассетах, хотя он in_stock! [id:" + nID + "][file:" + ahFiles[nID].sFile + "]"); //TODO LANG
							}
							else   // пока не удаляем ни удалённые ни ждущие тем более!  вдруг надо разбираться начать. по удаленным надо понимать когда они были удалены и т.п.
							{
								//cDBI.FileRemove(ahFiles[nID]);
								//(new Logger("StoragesSync")).WriteNotice("удалена запись о файле [id:" + nID + "][file:" + ahFiles[nID].sFile + "]"); //TODO LANG
							}
						}
						else
							(new Logger("StoragesSync")).WriteNotice("Не выставлены errors и не удалены файлы из БД, т.к. недоступен storage!!! [path:" + ahFiles[nID].cStorage.sPath + "]"); //TODO LANG
					}
                    (new Logger("StoragesSync")).WriteDebug("out");
                }
			}
			catch (Exception ex)
			{
				(new Logger("StoragesSync")).WriteError(ex);
			}
			return true;
		}
		private void FileChangeFormat(File cFile, byte nFPS, ushort nWidth, ushort nHeight, int nAspect_dividend, int nAspect_divider, long nFramesQty)
		{
			string sLog = "[name=" + cFile.sFilename + "][w=" + nWidth + "][h=" + nHeight + "][fps=" + nFPS + "][aspect=" + nAspect_dividend + "/" + nAspect_divider + "][fqty=" + nFramesQty + "]";
			cFile.FormatSet(nFPS, nWidth, nHeight, nAspect_dividend, nAspect_divider, nFramesQty);
			(new Logger("StoragesSync")).WriteDebug("проверка формата файла прошла " + sLog);
		}
        private long ClearPlDurations()
        {
            Dictionary<long, long> ahDurs = new Dictionary<long, long>();
            long nPLIID, nRetVal = 0;
            string sFilenameClear;
            foreach (string sFile in SIO.Directory.GetFiles(Preferences.cCache.sFolder))
            {
                sFilenameClear = SIO.Path.GetFileNameWithoutExtension(sFile);
                sFilenameClear = sFilenameClear.StartsWith("_") ? sFilenameClear.Substring(1) : sFilenameClear;
                if (long.TryParse(sFilenameClear, out nPLIID))
                {
                    if (_ahPLDurations.ContainsKey(nPLIID) && !ahDurs.ContainsKey(nPLIID))
                    {
                        ahDurs.Add(nPLIID, _ahPLDurations[nPLIID]);
                        nRetVal += _ahPLDurations[nPLIID];
                    }
                }
            }
            _ahPLDurations = ahDurs;
            return nRetVal;
        }

        /// <summary>
        ///   Deletes files in cache older than Preferences.cCache.tsAgeMaximum
        ///   Deletes user files (for ex. asset_3434) in cache older than tsAgeMaxForUserItems
        /// </summary>
        public void CacheStorageDeleteOldFiles()
        {
            (new Logger("CacheStorageDeleteOldFiles")).WriteDebug("чистка in");

            string sLogInfo;
            bool bDoNotDelete = FailoverConstants.IsFilesDoNotRemoveMode(Preferences.cCache.sFolder, out sLogInfo);
            if (bDoNotDelete)
            {
                (new Logger("CacheStorageDeleteOldFiles")).WriteWarning("обнаружен флаг, запрещающий удалять файлы из кэша " + sLogInfo);
                return;
            }

            SIO.DirectoryInfo cStorageContent = null;
            SIO.FileSystemInfo[] acFileSystemInfos = null;
            cStorageContent = new SIO.DirectoryInfo(Preferences.cCache.sFolder);

            if (!cStorageContent.Exists)
            {
                (new Logger("CacheStorageDeleteOldFiles")).WriteError(new Exception("не найден путь кэш-хранилища при попытке очистить!! без очистки папка будет переполнена!!! [" + Preferences.cCache.sFolder + "]"));//TODO LANG
                return;
            }
            //System.Collections.Generic.Dictionary<long, string> aPLFilesPlayed = (new DBInteract()).ComingUpFilesGetInMinutes(1, Preferences.cCache.nAnalysisDepth);
            acFileSystemInfos = cStorageContent.GetFileSystemInfos();
            string sFilesDeleted = "";
            DateTime dtNow = DateTime.Now;
            TimeSpan tsAgeMaxForUserItems = TimeSpan.FromDays(2);
            string sFilenameClear;
            DBInteract cDBI = new DBInteract();
            (new Logger("CacheStorageDeleteOldFiles")).WriteNotice("files total found [" + acFileSystemInfos.Length + "]");
            foreach (SIO.FileSystemInfo cFSInf in acFileSystemInfos)
            {
                if ((cFSInf.Attributes & SIO.FileAttributes.Directory) > 0)
                    continue;
                if (Preferences.cCache.tsAgeMaximum > dtNow.Subtract(cFSInf.CreationTime))
                    continue;
                sFilenameClear = cFSInf.Name.StartsWith("_") ? cFSInf.Name.Substring(1) : cFSInf.Name;
                if (!char.IsDigit(sFilenameClear, 0) && tsAgeMaxForUserItems > dtNow.Subtract(cFSInf.CreationTime))
                    continue;
                try
                {
                    cFSInf.Delete();
                    sFilesDeleted += cFSInf.Name + "(Ok";
                    long nPLIID;
                    if (!Preferences.cCache.bDBReadOnly && long.TryParse(SIO.Path.GetFileNameWithoutExtension(sFilenameClear), out nPLIID))
                    {
                        cDBI.RemoveItemFromCache(nPLIID);
                        sFilesDeleted += " and removed from db";
                    }
                    sFilesDeleted += "),";
                }
                catch
                {
                    sFilesDeleted += cFSInf.Name + "(Error),";
                }
            }
            if (0 < sFilesDeleted.Length)
                (new Logger("CacheStorageDeleteOldFiles")).WriteNotice("Файлы удалены из кэша: " + sFilesDeleted.TrimEnd(','));//TODO LANG
            (new Logger("CacheStorageDeleteOldFiles")).WriteDebug("чистка out");
        }
        public void CacheStorageFill()
        {
            (new Logger("CacheStorageFill")).WriteDebug("CacheStorageFill in");
            if (SIO.File.Exists(_sFilePauseCopying))
            {
                (new Logger("CacheStorageFill")).WriteDebug("CacheStorageFill. обнаружен флаг поставить копирование на паузу [file = " + _sFilePauseCopying + "]");
                return;
            }

            string sLogInfo;
            bool bDoNotDelete = FailoverConstants.IsFilesDoNotRemoveMode(Preferences.cCache.sFolder, out sLogInfo);
            long nCurrentCacheDur = ClearPlDurations(); //_ahPLDurations
            int nMaxMinutesToCache = int.MaxValue;
            long nMaxFramesToCache = long.MaxValue;
            bool bCopyTookTooLong = false;
            bool bCopyTookTooLongFor10Min = false;
            DateTime dtDiffSumStoreTill = DateTime.MaxValue;
            double nDiffSum = 0;
            double nDiffSec;
            if (bDoNotDelete)
            {
                nMaxMinutesToCache = (int)(Preferences.cCache.nAnalysisDepth * 1.5);
                nMaxFramesToCache = nMaxMinutesToCache * 60 * 25;
                (new Logger("CacheStorageFill")).WriteWarning("обнаружен флаг, запрещающий удалять файлы из кэша! Мы сможем залить только 1.5 выделенного размера хранилища [nMaxMinutesToCache = " + nMaxMinutesToCache + "][current=" + nCurrentCacheDur / 60 / 25 + "] " + sLogInfo);
                //return;
            }
            
            Dictionary<long, DateTime> ahPLDates;
            Dictionary<long, long> ahPLDurs;
            Dictionary<long, string> ahPLFiles;
            List<long> aPLIDsInOrder = (new DBInteract()).ComingUpFilesGet(1, Preferences.cCache.nAnalysisDepth, out ahPLFiles, out ahPLDates, out ahPLDurs);
            foreach (long nID in ahPLDurs.Keys)
            {
                if (!_ahPLDurations.ContainsKey(nID))
                    _ahPLDurations.Add(nID, ahPLDurs[nID]);
            }
            (new Logger("CacheStorageFill")).WriteDebug("[current_cache_dur=" + nCurrentCacheDur / 60 / 25 + "][ближайшие файлы = " + (ahPLFiles == null ? "NULL" : "" + ahPLFiles.Count) + "]");

            string sCacheFile;
			string sFilesCached = "";
			string sInternalPlayerCacheFile;
			string sFileCheck;
			string sExtension = "";
			string sDirWithoutSeparates;
			bool bFirstTime = true;
			bool bIgnored;
			DBInteract cDBI = new DBInteract();
			DateTime dtStart, dtStopTryingMove, dtStopPause;
            int nIndxTrying;
            string sParamCode;
            double nFreeSpace;
            foreach (SIO.DriveInfo cDI in SIO.DriveInfo.GetDrives())
            {
                if (cDI.Name.ToLower() == SIO.Path.GetPathRoot(Preferences.cCache.sFolder).ToLower() && cDI.IsReady)
                    if ((nFreeSpace = (double)cDI.AvailableFreeSpace / 1024 / 1024 / 1024) < Preferences.cCache.nAlertFreeSpace)
                        (new Logger("CacheStorageFill")).WriteError("FREE SPACE ALERT ON DISK FOR CACHE!!! [name=" + cDI.Name + "][free_space=" + nFreeSpace.ToString("0.000") + " GB]");
                    else
                        (new Logger("CacheStorageFill")).WriteDebug("FREE SPACE: [name=" + cDI.Name + "][free_space=" + nFreeSpace.ToString("0.000") + " GB]");
            }
            foreach (long nID in aPLIDsInOrder)
			{
#if DEBUG
                if (bFirstTime)
					bFirstTime = false;
				else
                    break;
#endif
                if (_bAbortWatchers)
                    return;

                if (nCurrentCacheDur > nMaxFramesToCache)
                {
                    (new Logger("CacheStorageFill")).WriteWarning("останавливаем залив файлов - превышен лимит [current_cache_dur=" + nCurrentCacheDur / 60 / 25 + "][nMaxMinutesToCache = " + nMaxMinutesToCache + "]");
                    return;
                }

				try
				{
                    dtStopPause = DateTime.Now.AddMinutes(30);
                    while (SIO.File.Exists(_sFilePauseCopying))
                    {
                        Thread.Sleep(800);
                        if (DateTime.Now > dtStopPause)
                        {
                            (new Logger("CacheStorageFill")).WriteError("CacheStorageFill. флаг поставить копирование на паузу висит уже более 30 минут! Снимаемся! [file = " + _sFilePauseCopying + "]");
                            SIO.File.Move(_sFilePauseCopying, _sFilePauseCopying + "!");
                            break;
                        }
                    }

                    if (Preferences.cCache.aIgnoreFiles.Contains(SIO.Path.GetFileName(ahPLFiles[nID]).ToLower()))
					{
						(new Logger("CacheStorageFill")).WriteDebug("файл найден в игнорлисте файлов (см. preferences.xml) и не будет закеширован [file=" + ahPLFiles[nID] + "]");
                        continue;
                    }
                    bIgnored = false;
                    foreach (string sFilter in Preferences.cCache.aIgnoreStorages)
                    {
                        sDirWithoutSeparates = SIO.Path.GetDirectoryName(ahPLFiles[nID]).ToLower().Replace("\\", "").Replace("/", "");
                        if (sDirWithoutSeparates == sFilter)
                        {
                            if (ahPLDurs[nID] < Preferences.cCache.aIgnoreStoragesTreshold[sFilter].TotalSeconds * 25)
                            {
                                (new Logger("CacheStorageFill")).WriteDebug("путь файла найден в игнорлисте стораджей (см. preferences.xml), но он короче минимума и будет закеширован  [filter=" + sFilter + "][file=" + ahPLFiles[nID] + "][dur=" + ahPLDurs[nID] + "]");
                                break;
                            }
                            (new Logger("CacheStorageFill")).WriteDebug("путь файла найден в игнорлисте стораджей (см. preferences.xml) и файл не будет закеширован  [filter=" + sFilter + "][file=" + ahPLFiles[nID] + "]");
                            bIgnored = true;
                            break;
                        }
                    }
                    if (bIgnored)
                        continue;

                    sExtension = SIO.Path.GetExtension(ahPLFiles[nID]);
					sCacheFile = SIO.Path.Combine(Preferences.cCache.sFolder, nID + sExtension);
					sInternalPlayerCacheFile = SIO.Path.Combine(Preferences.cCache.sFolder, "_" + nID + sExtension);
					if (!SIO.File.Exists(ahPLFiles[nID]))
                    {
                        (new Logger("CacheStorageFill")).WriteError("исходный файл для плейлиста не найден!!! [name=" + ahPLFiles[nID] + "]");
                        continue;
					}
					sFileCheck = null;
                    if (SIO.File.Exists(sInternalPlayerCacheFile))
                        sFileCheck = sInternalPlayerCacheFile;
                    else if (SIO.File.Exists(sCacheFile))
                    {
                        if (ahPLDates[nID].Subtract(DateTime.Now) < Preferences.cCache.tsCacheRewriteMinimum)
                            sFileCheck = sCacheFile;
                        else
                        {
                            DateTime dtFCache = SIO.File.GetLastWriteTime(sCacheFile);
                            DateTime dtFOrig = SIO.File.GetLastWriteTime(ahPLFiles[nID]);
                            if (dtFCache < dtFOrig)
                            {
                                (new Logger("CacheStorageFill")).WriteNotice("обнаружен более старый закешированный файл, но еще есть время его заменить. будем качать... <br>\t\tисходный файл: [dtwrite=" + SIO.File.GetLastWriteTime(ahPLFiles[nID]) + "][file=" + ahPLFiles[nID] + "]<br>\t\tверсия в кэше: [dtwrite=" + SIO.File.GetLastWriteTime(sCacheFile) + "][file=" + sCacheFile + "]");
                                //SIO.File.Delete(sCacheFile);
                            }
                            else
                                sFileCheck = sCacheFile;
                        }
                    }
                    if (null != sFileCheck)
                    {
                        DateTime dtFCache = SIO.File.GetLastWriteTime(sFileCheck);
                        DateTime dtFOrig = SIO.File.GetLastWriteTime(ahPLFiles[nID]);
                        if (dtFCache < dtFOrig)
                        {
                            if (!_ahFilesChanged.ContainsKey(ahPLFiles[nID]))
                                _ahFilesChanged.Add(ahPLFiles[nID], DateTime.MinValue);
                            if (_ahFilesChanged[ahPLFiles[nID]] != dtFOrig)
                            {
                                _ahFilesChanged[ahPLFiles[nID]] = dtFOrig;
                                (new Logger("CacheStorageFill")).WriteNotice("обнаружено, что исходная версия файла обновилась. Изменения будут проигнорированы. <br>\t\tисходный файл: [dtwrite=" + SIO.File.GetLastWriteTime(ahPLFiles[nID]) + "][file=" + ahPLFiles[nID] + "]<br>\t\tверсия в кэше: [dtwrite=" + SIO.File.GetLastWriteTime(sFileCheck) + "][file=" + sFileCheck + "]");
                            }
                        }
                        continue;
                    }
                    if (SIO.File.Exists("_" + sCacheFile) || SIO.File.Exists("_" + sCacheFile + "!"))
                    {
                        (new Logger("CacheStorageFill")).WriteError("не успели начать копировать - плеер уже копирует сам или взял предыдущую версию файла... [df=" + sCacheFile + "]");
                        continue;
                    }

                    (new Logger("CacheStorageFill")).WriteDebug("файл копируется: [dur=" + _ahPLDurations[nID].ToFramesString(false, false, true, true, true, false) + "][planned=" + ahPLDates[nID].ToString("yyyy-MM-dd HH:mm:ss") + "][src=" + ahPLFiles[nID] + "][dest=" + sCacheFile + "]");

                    if (SIO.File.Exists(sCacheFile + "!"))
                        SIO.File.Delete(sCacheFile + "!");

					dtStart = DateTime.Now;

                    if (Preferences.cCache.bSlowCopy && !bCopyTookTooLongFor10Min)
                    {
                        CopyFileExtended.sFilePauseCopying = _sFilePauseCopying;
                        lock (_oLockCopying)
                            _cCurrentCopying = new CopyFileExtended(ahPLFiles[nID], sCacheFile + "!", Preferences.cCache.nSlowCopyDelay, Preferences.cCache.nSlowCopyPeriod, _ahPLDurations[nID]);  // медленное копирование 
                        if (_bAbortWatchers)
                            return;
                        _cCurrentCopying.DoCopy2(); // из-за отработки onStop
                        if (_bAbortWatchers && !_cCurrentCopying.bCompleted)
                            return;
                        nDiffSec = _cCurrentCopying.tsDiff.TotalSeconds;
                    }
                    else
                    {
                        SIO.File.Copy(ahPLFiles[nID], sCacheFile + "!");
                        TimeSpan tsCopy = DateTime.Now.Subtract(dtStart);
                        nDiffSec = (tsCopy.TotalMilliseconds - _ahPLDurations[nID] * 40) / 1000;
                        (new Logger("CacheStorageFill")).WriteDebug("fast copy ended [diff = " + nDiffSec.ToString("0.0") + " sec][dur=" + _ahPLDurations[nID].ToFramesString(false, false, true, true, true, false) + "]");
                        if (bCopyTookTooLongFor10Min && Preferences.cCache.bSlowCopy)
                            (new Logger("CacheStorageFill")).WriteError("Copy Took Too Long over than 10 minutes! We will copy files the fastest way until restart. Check raid or net and restart sync to return slow copy [diff_10_min = " + nDiffSum.ToString("0.0") + " sec]");
                        System.Threading.Thread.Sleep(50);
                    }

                    if (!bCopyTookTooLong && nDiffSec > 0)
                    {
                        bCopyTookTooLong = true;
                        dtDiffSumStoreTill = DateTime.Now.AddMinutes(10);
                        (new Logger("CacheStorageFill")).WriteDebug("Copy Took Too Long. Current Diff = " + nDiffSec + " sec");
                    }
                    if (bCopyTookTooLong)
                    {
                        nDiffSum += nDiffSec;
                        if (nDiffSum >= 0)
                        {
                            if (DateTime.Now > dtDiffSumStoreTill)
                            {
                                (new Logger("CacheStorageFill")).WriteError("Copy Took Too Long last 10 minutes [sum_diff = " + nDiffSum.ToString("0.0") + " sec]");
                                bCopyTookTooLongFor10Min = true;
                            }
                        }
                        else
                        {
                            nDiffSum = 0;
                            bCopyTookTooLong = false;
                            bCopyTookTooLongFor10Min = false;
                            dtDiffSumStoreTill = DateTime.MinValue;
                            (new Logger("CacheStorageFill")).WriteDebug("Copy Took Too Long, but normalized during last 10 minutes [diff = " + nDiffSum.ToString("0.0") + " sec]");
                        }
                    }

                    if (SIO.File.Exists(sCacheFile + "!"))
                    {
                        if (SIO.File.Exists(sCacheFile))
                        {
                            long nD = 0;
                            SIO.File.Delete(sCacheFile);
                            while (SIO.File.Exists(sCacheFile))
                            {
                                nD++;
                                Thread.Sleep(1);
                            }
                            (new Logger("CacheStorageFill")).WriteDebug("удалили предыдущую версию файла [df=" + sCacheFile + "][checks_count=" + nD + "]");
                        }

                        if (SIO.File.Exists("_" + sCacheFile))
                        {
                            SIO.File.Delete(sCacheFile + "!");
                            (new Logger("CacheStorageFill")).WriteError("не успели покопировать - плеер уже взял предыдущую версию файла к себе. удалили то, что качали... [df=" + sCacheFile + "]");
                        }
                        else
                        {
                            dtStopTryingMove = DateTime.Now.AddSeconds(30);
                            nIndxTrying = 0;
                            sParamCode = "SetAttributes_normal";
                            while (true)
                            {
                                try
                                {
                                    System.IO.File.SetAttributes(sCacheFile + "!", SIO.FileAttributes.Normal); sParamCode = "SetCreationTime"; // if not we cant change file time
                                    System.IO.File.SetCreationTime(sCacheFile + "!", DateTime.Now); sParamCode = "SetLastWriteTime";
                                    System.IO.File.SetLastWriteTime(sCacheFile + "!", DateTime.Now); sParamCode = "SetLastAccessTime";
                                    System.IO.File.SetLastAccessTime(sCacheFile + "!", DateTime.Now); sParamCode = "Move";
                                    SIO.File.Move(sCacheFile + "!", sCacheFile);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    (new Logger("CacheStorageFill")).WriteDebug($"не удалось переименовать файл (или изменить его параметры) [param = {sParamCode}]");
                                    if (DateTime.Now > dtStopTryingMove)
                                        throw new Exception($"пытались 30 секунд переименовать файл (или изменить его параметры) - никак [param = {sParamCode}]", ex);
                                    Thread.Sleep(50);
                                    nIndxTrying += 50;
                                }
                            }
                            nCurrentCacheDur += _ahPLDurations[nID];
                            TimeSpan tsCopy = DateTime.Now.Subtract(dtStart);
                            (new Logger("CacheStorageFill")).WriteDebug($"файл покопирован: ({tsCopy.ToString("mm\\:ss")})[df=" + sCacheFile + "][trying_ms=" + nIndxTrying + "]");
                        }
                        
                    }
                    else
                        (new Logger("CacheStorageFill")).WriteError(new Exception("Не удалось загрузить файл в кэш! Возможно диск переполнен!"));
					//SIO.File.Copy(aPLFiles[nID], sCacheFile);
					
					if (!Preferences.cCache.bDBReadOnly)
					{
						try
						{
							cDBI.PlaylistItemCached(nID);
						}
						catch (Exception ex)
						{
							(new Logger("CacheStorageFill")).WriteError("не удалось занести информацию о кэшировании элемента ", ex);
						}
					}

					sFilesCached += nID + sExtension + "(" + DateTime.Now.Subtract(dtStart).TotalSeconds.ToString("0.0") + " s), ";
				}
				catch (Exception ex)
                {
                    (new Logger("CacheStorageFill")).WriteError("[id=" + nID + "][file=" + ahPLFiles[nID] + "]", ex);
                    sFilesCached += nID + sExtension + "(E),";
				}
			}
			if (0 < sFilesCached.Length)
			{
				(new Logger("CacheStorageFill")).WriteNotice("файлы добавлены в кэш:" + sFilesCached.TrimEnd(','));//TODO LANG

				if (!Preferences.cCache.bDBReadOnly)
				{
					try
					{
						cDBI.CacheClear();
					}
					catch (Exception ex)
					{
						(new Logger("CacheStorageFill: clearing cache fails")).WriteError(ex);
					}
				}
			}
            (new Logger("CacheStorageFill")).WriteDebug("done");
        }
		public void VideoConvert(string sFileSource, string sFileTarget)
        {
            (new Logger("VideoConvert")).WriteDebug("start Video Convertion");
            ffmpeg.net.File.Input cFileSource = null;
            ffmpeg.net.File.Output cFileTarget = null;
            try
            {
                ffmpeg.net.Format.Video cFormatVideoInputTarget = new ffmpeg.net.Format.Video(Preferences.cPreview.nVideoWidth, Preferences.cPreview.nVideoHeight, ffmpeg.net.PixelFormat.AV_PIX_FMT_BGR24, 4, ffmpeg.net.AVFieldOrder.AV_FIELD_UNKNOWN);
                ffmpeg.net.Format.Audio cFormatAudioInputTarget = new ffmpeg.net.Format.Audio(48000, 2, ffmpeg.net.AVSampleFormat.AV_SAMPLE_FMT_S16, 4);
                cFileSource = new ffmpeg.net.File.Input(sFileSource);
                cFileSource.Prepare(cFormatVideoInputTarget, cFormatAudioInputTarget, ffmpeg.net.File.Input.PlaybackMode.GivesFrameOnDemand);
                if (!cFileSource.bCached)
                    throw new Exception("file was not prepared!");

                cFileTarget = new ffmpeg.net.File.Output(
                    sFileTarget,
                    new ffmpeg.net.Format.Video(Preferences.cPreview.nVideoWidth, Preferences.cPreview.nVideoHeight, Preferences.cPreview.eVideoCodecID, Preferences.cPreview.eVideoPixelFormat, 4, ffmpeg.net.AVFieldOrder.AV_FIELD_UNKNOWN),
                    new ffmpeg.net.Format.Audio(Preferences.cPreview.nAudioSamplesRate, Preferences.cPreview.nAudioChannelsQty, Preferences.cPreview.eAudioCodecID, Preferences.cPreview.eAudioSampleFormat, 4)
                    );

                bool bEndVideo = false, bEndAudio = false;
                ffmpeg.net.Frame cFrame = null;

                while (true)
                {
                    if (!bEndVideo && (null != (cFrame = cFileSource.FrameNextVideoGet())))
                    {
                        if (0 < cFrame.nLength)
                            cFileTarget.FrameNextVideo(cFormatVideoInputTarget, cFrame);
                        cFrame.Dispose();
                    }
                    else
                        bEndVideo = true;

                    if (!bEndAudio && null != (cFrame = cFileSource.FrameNextAudioGet()))
                    {
                        if (0 < cFrame.nLength)
                            cFileTarget.FrameNextAudio(cFormatAudioInputTarget, cFrame);
                        cFrame.Dispose();
                    }
                    else
                        bEndAudio = true;
                    if (bEndVideo && bEndAudio)
                        break;
                    //Thread.Sleep(30);
                }
            }
            catch (Exception ex)
            {
                (new Logger("VideoConvert: ")).WriteError(ex);
            }
            finally
            {
                if (cFileSource != null)
                    cFileSource.Close();
                if (cFileTarget != null)
                    cFileTarget.Close();
			}
		}
	}
}
