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
using System.Net;
using System.Collections;
using SIO=System.IO;
using System.Linq;

namespace replica.sync
{
    public partial class Service : ServiceBase
    {
		private ManualResetEvent _mreWatcherStorageStopping;
		private ManualResetEvent _mreWatcherStorageStopped;
		private ManualResetEvent _mreWatcherCommandStopping;
		private ManualResetEvent _mreWatcherCommandStopped;
		private ManualResetEvent _mreWatcherPreviewStopping;
		private ManualResetEvent _mreWatcherPreviewStopped;

		public Service()
        {
            InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			base.OnStart(args);
			try
			{
				(new Logger()).WriteNotice("получен сигнал на запуск");//TODO LANG
#if !DEBUG
				System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity = Preferences.nAffinity;
				_mreWatcherCommandStopping = new ManualResetEvent(false);
				_mreWatcherCommandStopped = new ManualResetEvent(true);
				ThreadPool.QueueUserWorkItem((object o) => { WatcherCommand(); });
				Thread.Sleep(300);
#endif
				if (null != Preferences.cCache)
				{
					_mreWatcherStorageStopping = new ManualResetEvent(false);
					_mreWatcherStorageStopped = new ManualResetEvent(true);
					ThreadPool.QueueUserWorkItem((object o) => { WatcherStorage(); });
					Thread.Sleep(300);
				}
				if (null != Preferences.cPreview)
				{
					ffmpeg.net.Logger.eLevel = Logger.Level.notice;
					_mreWatcherPreviewStopping = new ManualResetEvent(false);
					_mreWatcherPreviewStopped = new ManualResetEvent(true);
					ThreadPool.QueueUserWorkItem((object o) => { WatcherPreview(); });
				}
				_mreWatcherCommandStopped.Reset();
				if (null != _mreWatcherStorageStopped)
					_mreWatcherStorageStopped.Reset();
				if(null != _mreWatcherPreviewStopped)
					_mreWatcherPreviewStopped.Reset();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

		public void TestStart()
		{
			OnStart(null);
		}





		protected override void OnStop()
		{
			try
			{
				(new Logger()).WriteNotice("получен сигнал на остановку");//TODO LANG
				_mreWatcherCommandStopping.Set();
				if (null != _mreWatcherStorageStopping)
					_mreWatcherStorageStopping.Set();
				if (null != _mreWatcherPreviewStopping)
					_mreWatcherPreviewStopping.Set();

				_mreWatcherCommandStopped.WaitOne(15000, false);
				if (null != _mreWatcherStorageStopped)
					_mreWatcherStorageStopped.WaitOne(15000, false);
				if (null != _mreWatcherPreviewStopped)
					_mreWatcherPreviewStopped.WaitOne(15000, false);

			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

		private void WatcherCommand()
		{
			//Thread.Sleep(10000);
			try
			{
				(new Logger()).WriteNotice("управление командами запущено");//TODO LANG
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
						(new Logger()).WriteError(ex); //UNDONE
					}
				} while (!_mreWatcherCommandStopping.WaitOne(Preferences.tsCommandsSleepDuration, false));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex); //UNDONE
			}
			(new Logger()).WriteNotice("управление командами остановлено");//TODO LANG
			if (null != _mreWatcherCommandStopped)
				_mreWatcherCommandStopped.Set();
		}
		private void WatcherStorage()
		{
			//Thread.Sleep(10000);
			try
			{
				(new Logger()).WriteNotice("модуль синхронизации запущен");//TODO LANG
				DBInteract cDBI = new DBInteract();
				do
				{
					try
					{
						CacheStorageDeleteOldFiles();
						CacheStorageFill();
						StoragesSync();
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex); //UNDONE
					}
					System.Threading.Thread.Sleep(1);
				} while (!_mreWatcherStorageStopping.WaitOne(Preferences.cCache.tsSleepDuration, false));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex); //UNDONE
			}
			(new Logger()).WriteNotice("модуль синхронизации остановлен");//TODO LANG
			if (null != _mreWatcherStorageStopped)
				_mreWatcherStorageStopped.Set();
		}
		private void WatcherPreview()
		{
			try
			{
				(new Logger()).WriteNotice("модуль обеспечения предпросмотра запущен");//TODO LANG
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
								if(!SIO.File.Exists(cFile.sFile))
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
										(new Logger()).WriteNotice("создан файл предварительного просмотра [" + sFilePreview + "]");//TODO LANG
									}
									catch (Exception ex)
									{
										(new Logger()).WriteError(new Exception("ошибка создания файла предварительного просмотра [" + sFilePreview + "]", ex));
										try
										{
											if (SIO.File.Exists(sFilePreview))
												SIO.File.Delete(sFilePreview);
										}
										catch (Exception exx)
										{
											(new Logger()).WriteWarning("ошибка удаления файла [" + sFilePreview + "]", exx);
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex); //UNDONE
					}
				} while (!_mreWatcherPreviewStopping.WaitOne(Preferences.cPreview.tsSleepDuration, false));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex); //UNDONE
			}
			(new Logger()).WriteNotice("модуль обеспечения предпросмотра остановлен");//TODO LANG
			if (null != _mreWatcherPreviewStopped)
				_mreWatcherPreviewStopped.Set();
		}

		public bool StoragesSync()
		{
			try
			{
				//Thread.Sleep(10000);
				DBInteract cDBI = new DBInteract();

				Queue<Storage> aqStorages = null;

				if (null != (aqStorages = cDBI.StoragesGet()))
				{
					(new Logger()).WriteDebug("папки получены = " + aqStorages.Count);
					File[] aFilesUnused = cDBI.FilesUnusedGet();
					(new Logger()).WriteDebug("неиспользованные файлы получены = " + aFilesUnused.Length);
					Dictionary<long, File> ahFiles = cDBI.FilesGet();
					(new Logger()).WriteDebug("файлы получены = " + ahFiles.Count);
					Dictionary<long, Dictionary<string, File>> ahDBFiles = aqStorages.ToDictionary(o => o.nID, o => new Dictionary<string, File>());
					foreach(long nID in ahFiles.Keys)
						ahDBFiles[ahFiles[nID].cStorage.nID].Add(ahFiles[nID].sFilename.ToLower(), ahFiles[nID]);
                    Storage cStorage;
					string sName;
					while (0 < aqStorages.Count)
					{
						cStorage = aqStorages.Dequeue();
						(new Logger()).WriteDebug("обрабатываем папку = " + cStorage.sName + "[path = " + cStorage.sPath + "]" + " [count_bd = " + ahDBFiles[cStorage.nID].Count + "]");
						if (SIO.Directory.Exists(cStorage.sPath))
						{
							foreach (SIO.FileInfo cFI in (new SIO.DirectoryInfo(cStorage.sPath)).GetFiles())
							{
								if (cFI.Name.StartsWith(".") || cFI.Name.EndsWith("!"))
									continue;
								sName = cFI.Name.ToLower();
								if (ahDBFiles[cStorage.nID].ContainsKey(sName))
								{
									if (helpers.replica.Error.missed == ahDBFiles[cStorage.nID][sName].eError)
									{
										cDBI.FileErrorRemove(ahDBFiles[cStorage.nID][sName]);
										(new Logger()).WriteNotice("файл появился на диске [id:" + ahDBFiles[cStorage.nID][sName].nID + "][file:" + ahDBFiles[cStorage.nID][sName].sFile + "]"); //TODO LANG
									}
									ahFiles.Remove(ahDBFiles[cStorage.nID][sName].nID);
								}
								else
                                    (new Logger()).WriteNotice("добавлен файл [" + cDBI.FileAdd(cStorage.nID, cFI.Name).sFile + "]"); //TODO LANG
							}
							(new Logger()).WriteDebug("обработали папку = " + cStorage.sName + " [count_files = " + ahFiles.Count + "]");
						}//UNDONE mark storage as failed
						else
							(new Logger()).WriteDebug("папка НЕ НАЙДЕНА!!! = " + cStorage.sName + "[path = "+ cStorage.sPath + "]");
					}
					(new Logger()).WriteDebug("обработали все папки [count_files = " + ahFiles.Count + "]");
					File cFile;
					foreach (long nID in ahFiles.Keys)
					{
						if (1 > aFilesUnused.Count(row => row.nID == nID))
						{
							if (helpers.replica.Error.missed != ahFiles[nID].eError)
							{
								cFile = ahFiles[nID];
								cFile.eError = helpers.replica.Error.missed;
//								cDBI.FileErrorSet(cFile); //UNDONE
                                (new Logger()).WriteNotice("файл отсутствует на диске [id:" + nID + "][file:" + ahFiles[nID].sFile + "]"); //TODO LANG
							}
						}
						else
						{
//						    cDBI.FileRemove(ahFiles[nID]);
                            (new Logger()).WriteNotice("удалена запись о файле [id:" + nID + "][file:" + ahFiles[nID].sFile + "]"); //TODO LANG
						}
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return true;
		}
		private void CacheStorageDeleteOldFiles()
		{
			(new Logger()).WriteDebug("чистка in");
			SIO.DirectoryInfo cStorageContent = null;
			SIO.FileSystemInfo[] acFileSystemInfos = null;
			cStorageContent = new SIO.DirectoryInfo(Preferences.cCache.sFolder);

			if (!cStorageContent.Exists)
			{
				(new Logger()).WriteError(new Exception("не найден путь кэш-хранилища " + Preferences.cCache.sFolder));//TODO LANG
				return;
			}

			acFileSystemInfos = cStorageContent.GetFileSystemInfos();
			string sFilesDeleted = "";
			DateTime dtNow = DateTime.Now;
			foreach (SIO.FileSystemInfo cFSInf in acFileSystemInfos)
			{
				if ((cFSInf.Attributes & SIO.FileAttributes.Directory) > 0)
					continue;
				if (Preferences.cCache.tsAgeMaximum > dtNow.Subtract(cFSInf.CreationTime))
					continue;
				try
				{
					cFSInf.Delete();
					sFilesDeleted += cFSInf.Name + "(D),";
				}
				catch
				{
					sFilesDeleted += cFSInf.Name + "(E),";
				}
			}
			if(0 < sFilesDeleted.Length)
				(new Logger()).WriteNotice("Файлы удалены из кэша:" + sFilesDeleted.TrimEnd(','));//TODO LANG
			(new Logger()).WriteDebug("чистка out");
		}
		private void CacheStorageFill()
		{
			System.Collections.Generic.Dictionary<long, string> aPLFiles = (new DBInteract()).ComingUpFilesGet(3, Preferences.cCache.nAnalysisDepth - 3);
			(new Logger()).WriteDebug("ближайшие файлы = "+ aPLFiles.Count);
			string sCacheFile;
			string sFilesCached = "";
			string sInternalPlayerCacheFile;
			string sFileCheck;
			string sExtension = "";
			bool bFirstTime = true;
			DBInteract cDBI = new DBInteract();
			foreach (long nID in aPLFiles.Keys)
			{
#if DEBUG
				if (bFirstTime)
					bFirstTime = false;
				else
                    break;
#endif
				try
				{
					sExtension = SIO.Path.GetExtension(aPLFiles[nID]);
					sCacheFile = Preferences.cCache.sFolder + nID + sExtension;
					sInternalPlayerCacheFile = Preferences.cCache.sFolder + "_" + nID + sExtension;
					if (!SIO.File.Exists(aPLFiles[nID]))
					{
						(new Logger()).WriteDebug("файл не найден = " + aPLFiles[nID]);
						continue;
					}
					sFileCheck = null;
					if (SIO.File.Exists(sInternalPlayerCacheFile))
						sFileCheck = sInternalPlayerCacheFile;
					else if (SIO.File.Exists(sCacheFile))
						sFileCheck = sCacheFile;
					if(null != sFileCheck)
					{
						if (SIO.File.GetLastWriteTime(sFileCheck) < SIO.File.GetLastWriteTime(aPLFiles[nID]))
							(new Logger()).WriteError(new Exception("обнаружено расхождение между исходной версией файла и версией в кэше. изменения будут проигнорированы. исходный файл:" + aPLFiles[nID] + "версия в кэше:" + sFileCheck));
						continue;
					}
					new CopyFileExtended(aPLFiles[nID], sCacheFile + "!", 0, 1000);
					SIO.File.Move(sCacheFile + "!", sCacheFile);
					//SIO.File.Copy(aPLFiles[nID], sCacheFile);
					(new Logger()).WriteDebug("файл покопирован: [sf=" + aPLFiles[nID] + "][df=" + sCacheFile + "]");
					try
					{
						cDBI.PlaylistItemCached(nID);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
					}
					try
					{
						System.IO.File.SetCreationTime(sCacheFile, DateTime.Now);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteNotice("не удалось обновить время создания файла:" + ex.Message);
					}
					try
					{
						System.IO.File.SetLastWriteTime(sCacheFile, DateTime.Now);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteNotice("не удалось обновить время записи файла:" + ex.Message);
					}
					try
					{
						System.IO.File.SetLastAccessTime(sCacheFile, DateTime.Now);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteNotice("не удалось обновить время доступа к файлу:" + ex.Message);
					}
					sFilesCached += nID + sExtension+"(N),";
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
					sFilesCached += nID + sExtension+"(E),";
				}
			}
			if (0 < sFilesCached.Length)
			{
				(new Logger()).WriteNotice("файлы добавлены в кэш:" + sFilesCached.TrimEnd(','));//TODO LANG
				try
				{
					cDBI.CacheClear();
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
			}
		}
		public void VideoConvert(string sFileSource, string sFileTarget)
		{
			ffmpeg.net.Format.Video cFormatVideoInputTarget = new ffmpeg.net.Format.Video(Preferences.cPreview.nVideoWidth, Preferences.cPreview.nVideoHeight, ffmpeg.net.PixelFormat.AV_PIX_FMT_BGR24, 4);
            ffmpeg.net.Format.Audio cFormatAudioInputTarget = new ffmpeg.net.Format.Audio(48000, 2, ffmpeg.net.AVSampleFormat.AV_SAMPLE_FMT_S16, 4);
			ffmpeg.net.File.Input cFileSource = new ffmpeg.net.File.Input(sFileSource);
			cFileSource.tsTimeout = TimeSpan.FromMinutes(1);
			cFileSource.Prepare(cFormatVideoInputTarget, cFormatAudioInputTarget);

			ffmpeg.net.File.Output cFileTarget = new ffmpeg.net.File.Output(
				sFileTarget,
				new ffmpeg.net.Format.Video(Preferences.cPreview.nVideoWidth, Preferences.cPreview.nVideoHeight, Preferences.cPreview.eVideoCodecID, Preferences.cPreview.eVideoPixelFormat, 4),
				new ffmpeg.net.Format.Audio(Preferences.cPreview.nAudioSamplesRate, Preferences.cPreview.nAudioChannelsQty, Preferences.cPreview.eAudioCodecID, Preferences.cPreview.eAudioSampleFormat, 4)
			);

			bool bEndVideo = false, bEndAudio = false;
			ffmpeg.net.Frame cFrame = null;
			try
			{
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
			finally
			{
				cFileSource.Close();
				cFileTarget.Close();
			}
		}
	}
}
