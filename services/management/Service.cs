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
using helpers.replica.pl;
using helpers.replica.mam;
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

		public Service()
		{
			InitializeComponent();
		}
		protected override void OnStart(string[] args)
		{
			base.OnStart(args);
			try
			{
				if (System.IO.Directory.Exists(@"\\airfs\clips01"))
					(new Logger("management")).WriteNotice("папка \\\\airfs\\clips01 видна )");
				else
					(new Logger("management")).WriteNotice("папка \\\\airfs\\clips01 не видна!");  // просто тест

				(new Logger("management")).WriteNotice("получен сигнал на запуск");//TODO LANG
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
				(new Logger("management")).WriteNotice("получен сигнал на остановку");//TODO LANG
				_mreArchiveWatcherStopping.Set();
				_mreCommandWatcherStopping.Set();
				if(null != _mrePlaylistWatcherStopping)
				{
					_mrePlaylistWatcherStopping.Set();
					_mrePlaylistWatcherStopped.WaitOne(15000, false);
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

		private void PlaylistWatcher(object cStateInfo)
		{
			try
			{
				(new Logger("playlist")).WriteNotice("управление плейлистом запущено");//TODO LANG
				do
				{
					try
					{
						int nFramesLeft = (new DBInteract()).PlaylistFramesQtyGet();
                        if (-1 < nFramesLeft && Preferences.tsPlaylistGenerationLength.TotalMilliseconds > (nFramesLeft * 40))
                        {
                            DateTime dtStart = DateTime.Now;
							(new Logger("playlist")).WriteNotice("начало генерации плейлиста...    [текущий плейлист = " + nFramesLeft + "] [AutoPLLength = " + Preferences.tsPlaylistGenerationLength.TotalMilliseconds / 40 + "]"); //TODO LANG
							PlaylistGenerate(new TimeSpan(6, 0, 0));
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
					//try
					//{
					//    ManagementObject cDisk = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
					//    cDisk.Get();
					//    cDBI.StorageFreeSpaceSet(To.ULong(cDisk["FreeSpace"]));
					//}
					//catch (Exception ex)
					//{
					//    (new Logger()).WriteError(ex); //UNDONE
					//}
				} while (!_mrePlaylistWatcherStopping.WaitOne(Preferences.tsSleepDuration, false));
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
		public void CommandWatcher()  // для отладки в тесте //EMERGENCY не проще ли в тесте сразу вызывать replica.management.DBInteract.ProcessCommands() ??
		{
			DBInteract cDBI = new DBInteract();
			try
			{
				cDBI.ProcessCommands();
			}
			catch (Exception ex)
			{
				(new Logger("commands")).WriteError(ex); //UNDONE
			}
		}
		private void CommandWatcher(object cStateInfo)
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
        #region Генерация ПЛ
        private List<Asset> _aBumpers = null;
		private List<Asset> _aBumpersIDs = null;
        private int _nCurrentBumperIndex = -1;
		private int _nCurrentBumperIDsIndex = -1;
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
        private Asset NextBumperGet()
        {
            DBInteract cDBI = new DBInteract();
            if (null == _aBumpers)
            {
                _aBumpers = new List<Asset>();
				_aBumpersIDs = new List<Asset>();
                try
                {
                    _aBumpers.Add(cDBI.AssetGet("Отбивка №1"));
                    _aBumpers.Add(cDBI.AssetGet("Отбивка №2"));
                    _aBumpers.Add(cDBI.AssetGet("Отбивка №3"));
                    _nCurrentBumperIndex = -1;
					_aBumpersIDs.AddRange(cDBI.AssetsGet(new IdNamePair("design")).Where(o => o.sName.StartsWith("ArtistID")).OrderBy(o=>o.sName));
					string sLastArtistID = cDBI.LastPlannedArtistIDGet();
					sLastArtistID = sLastArtistID.Substring(10, 3);
					_nCurrentBumperIDsIndex = int.Parse(sLastArtistID) - 1;
                }
                catch (Exception ex)
                {
					(new Logger("playlist")).WriteError("ошибка получения ассетов отбивок ХХХ", ex);
					return null;
                }
            }
            _nCurrentBumperIndex++;
			if (0 >= _aBumpersIDs.Count)
				return _aBumpers[_nCurrentBumperIndex % _aBumpers.Count];
			else
			{
				int nI = _nCurrentBumperIndex % (_aBumpers.Count + 1);
				if (_aBumpers.Count == nI)
					return _aBumpersIDs[++_nCurrentBumperIDsIndex % _aBumpersIDs.Count];
				else
					return _aBumpers[nI];
			}
        }
        public List<Asset> PlayListGet(TimeSpan tsInterval)
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
                    cCurrentAsset = NextBumperGet();
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
            _aBumpers = null;
            _nCurrentBumperIndex = -1;
            return aRetVal;
        }
		public void PlaylistGenerate(TimeSpan tsInterval)
        {
            DBInteract cDBI = new DBInteract();
			List<Asset> aAss = PlayListGet(tsInterval);
            if (null == aAss || 0 >= aAss.Count)
                throw new Exception("not enough acceptable assets"); //TODO LANG

            Queue aqAssetsIDs = new Queue();
            foreach (Asset cAss in aAss)
                aqAssetsIDs.Enqueue(cAss.nID);
            cDBI.PlaylistItemAdd(aqAssetsIDs);
        }
        #endregion
		private bool MoveItemsToArchive()
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
	}
}
