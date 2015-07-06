using System;
using System.ServiceProcess;
using System.Threading;

using helpers.replica.pl;

namespace replica.failover
{
	public partial class Service : ServiceBase
	{
		private Player _cPlayer;
		private Playlist _cPlaylist;
		private Cues _cCues;
		private bool _bRunning;
		private int _nThreadsFinished;

		public Service()
		{
			InitializeComponent();
		}
		public void Start()
		{
			OnStart(null);
		}
		protected override void OnStart(string[] args)
		{
			base.OnStart(args);

			try
			{
				(new Logger("service")).WriteNotice("получен сигнал на запуск");//TODO LANG
				_bRunning = true;
				_nThreadsFinished = 0;

				Player.Logger.sFile = "player";
				_cPlayer = new Player();
				replica.playlist.Calculator.Logger.sFile = "calculator";
				Playlist.Logger.sFile = "playlist";
				_cPlaylist = new Playlist();
				_cPlaylist.WatcherCommands = WatcherCommands;
				_cPlaylist.Start(new Failover(), _cPlayer);
				_cPlayer.Start(new Failover());

				Cues.Logger.sFile = replica.cues.Template.Logger.sFile = "cues";
				replica.cues.Template.iInteract = new Failover();
				_cCues = new Cues();
				_cCues.WatcherCommands = WatcherCommands;
				_cCues.Start(new Failover());
			}
			catch (Exception ex)
			{
				(new Logger("service")).WriteError(ex);
			}
		}
		protected override void OnStop()
		{
			try
			{
				(new Logger("service")).WriteNotice("получен сигнал на остановку");//TODO LANG
				_bRunning = false;
				DateTime dt = DateTime.Now;
				_cPlaylist.Stop();
				_cPlayer.Stop();
				while (1 > _nThreadsFinished && DateTime.Now.Subtract(dt).TotalSeconds < 2)
					Thread.Sleep(300);
				if (1 > _nThreadsFinished)
					(new Logger("service")).WriteNotice("превышено ожидание завершения потоков");
			}
			catch (Exception ex)
			{
				(new Logger("service")).WriteError(ex);
			}
		}

		private void WatcherCommands(object cStateInfo)
		{
			(new Logger("commands")).WriteNotice("модуль управления командами запущен");//TODO LANG
			while (_bRunning)
			{
				try
				{
					(new DBInteract()).ProcessCommands(new Delegate[] { (DBInteract.PlayerSkipDelegate)_cPlayer.Skip, (DBInteract.FailoverSynchronizeDelegate)Failover.Synchronize });
					Thread.Sleep(300);
				}
				catch (Exception ex)
				{
					(new Logger("commands")).WriteError(ex);
					Thread.Sleep(5000);
				}
			}
			(new Logger("commands")).WriteNotice("модуль управления командами остановлен");//TODO LANG
			_nThreadsFinished++;
		}
		//private void WatcherPlaylist(object cStateInfo)
		//{
		//    try
		//    {
		//        (new Logger("playlist")).WriteNotice("модуль плейлиста запущен");//TODO LANG
		//        PlaylistItem cPLIPrevious = null;
		//        int nEmptySlots = 0;
		//        bool bEnqueueNeed = true, bFirstRun = true, bOnAirGetFailed = false;
		//        short nItemsLeft = -1;

		//        Failover.Synchronize();

		//        ThreadPool.QueueUserWorkItem(new WaitCallback(WatcherCommands));

		//        while (_bRunning)
		//        {
		//            try
		//            {
		//                if (null == cPLIPrevious)
		//                {
		//                    try
		//                    {
		//                        cPLIPrevious = Failover.PlaylistItemCurrentGet();
		//                        if (null == cPLIPrevious)
		//                            throw new Exception("на текущее время запланированное воспроизведение отсутствует");
		//                        _cPlayer.QueueAdd(cPLIPrevious);
		//                    }
		//                    catch (System.IO.FileNotFoundException e)
		//                    {
		//                        cPLIPrevious = null;
		//                        (new Logger("playlist")).WriteError(new Exception("отсутствует указанный файл [" + e.Message + "]"));
		//                    }
		//                    catch (Exception ex)
		//                    {
		//                        cPLIPrevious = null;
		//                        (new Logger("playlist")).WriteError(ex);
		//                        Thread.Sleep(3000);
		//                    }
		//                    continue;
		//                }
		//                if (!bEnqueueNeed)
		//                {
		//                    if ((Preferences.nQueueLength - 2) > (nEmptySlots = Preferences.nQueueLength - _cPlayer.QueueLength()))
		//                    {
		//                        if (3 < nEmptySlots)
		//                        {
		//                            try
		//                            {
		//                                if (Preferences.nEnqueueSafeDuration < Failover.PlaylistItemOnAirFramesLeftGet())
		//                                    bEnqueueNeed = true;
		//                                bOnAirGetFailed = false;
		//                            }
		//                            catch (Exception ex)
		//                            {
		//                                if (bOnAirGetFailed)
		//                                    bEnqueueNeed = true;
		//                                bOnAirGetFailed = true;
		//                                (new Logger("playlist")).WriteError(ex);
		//                            }
		//                        }
		//                    }
		//                    else
		//                    {
		//                        (new Logger("playlist")).WriteNotice("::" + Preferences.nQueueLength + "::" + nEmptySlots + "::" + _cPlayer.QueueLength());
		//                        bEnqueueNeed = true;
		//                    }
		//                }
		//                if (bEnqueueNeed)
		//                {
		//                    (new Logger("playlist")).WriteNotice("==========================================================================");
		//                    try
		//                    {
		//                        if (bFirstRun)
		//                        {
		//                            bFirstRun = false;
		//                            ThreadPool.QueueUserWorkItem(new WaitCallback(WatcherCues));
		//                        }
		//                        else
		//                            Failover.PlaylistItemsTimingsUpdate(TimeSpan.FromHours(3));
		//                    }
		//                    catch (Exception ex)
		//                    {
		//                        (new Logger("playlist")).WriteError(ex);
		//                    }
		//                    try
		//                    {
		//                        cPLIPrevious = Failover.PlaylistItemLockedPreviousGet();
		//                    }
		//                    catch (Exception ex)
		//                    {
		//                        (new Logger("playlist")).WriteError(ex);
		//                    }
		//                    PlaylistItem[] aPLIs = Failover.PlaylistItemsPlannedGet();
		//                    nItemsLeft = (short)nEmptySlots;
		//                    for (int nIndx = 0; aPLIs.Length > nIndx; nIndx++)
		//                    {
		//                        if (0 == nItemsLeft)
		//                            break;
		//                        try
		//                        {
		//                            if (!System.IO.File.Exists(aPLIs[nIndx].cFile.sFile))
		//                            {
		//                                Failover.PlaylistItemStop(aPLIs[nIndx]); //TODO: выставить флаг ошибки на файл
		//                                cPLIPrevious = aPLIs[nIndx];
		//                                (new Logger("playlist")).WriteNotice("НЕ НАЙДЕН ФАЙЛ:" + aPLIs[nIndx].cFile.sFile);
		//                                (new Logger("playlist")).WriteNotice("***************************************************************************");
		//                                continue;
		//                            }

		//                            if (Preferences.nThresholdFrames > aPLIs[nIndx].nDuration)
		//                                aPLIs[nIndx].nFrameStop = 0;
		//                            if (null != cPLIPrevious)
		//                            {
		//                                if (aPLIs[nIndx].cClass.sName.ToString().ToLower().Contains("advertisement"))
		//                                {
		//                                    if (
		//                                        (DateTime.MaxValue > aPLIs[nIndx].dtStartHard
		//                                            && aPLIs[nIndx].dtStartHard > cPLIPrevious.dtStopPlanned.AddMilliseconds(Preferences.nThresholdMilliseconds)
		//                                        )
		//                                        || (
		//                                            DateTime.MaxValue > aPLIs[nIndx].dtStartSoft
		//                                            && aPLIs[nIndx].dtStartSoft > cPLIPrevious.dtStopPlanned.AddMilliseconds(Preferences.nThresholdMilliseconds)
		//                                        )
		//                                    )
		//                                    {
		//                                        (new Logger("playlist")).WriteNotice("ОБНАРУЖЕНО ОТСУТСТВИЕ КОНТЕНТА МЕЖДУ [" + cPLIPrevious.cFile.sFile + "] И [" + aPLIs[nIndx].cFile.sFile + "]");
		//                                        (new Logger("playlist")).WriteNotice("***************************************************************************");
		//                                        cPLIPrevious = null;
		//                                        break;
		//                                    }
		//                                }
		//                                //nFramesNeeded = (ulong)(aPLIs[nIndx].dtStartPlanned.Subtract(cPLIPrevious.dtStartPlanned).TotalSeconds * Preferences.nFPS);
		//                                //if (cPLIPrevious.nDuration - nFramesNeeded >= Preferences.nFPS && Preferences.nMinimumFrames <= nFramesNeeded)
		//                                //{
		//                                //    cPLIPrevious.nFrameStop = (int)((((ulong)cPLIPrevious.nFrameStart + nFramesNeeded) / Preferences.nFPS) * Preferences.nFPS);
		//                                //    (new Logger("playlist")).WriteNotice("допинфо [" + cPLIPrevious.nDuration + "][" + nFramesNeeded + "][" + cPLIPrevious.nFrameStop + "][" + cPLIPrevious.nFrameStart + "]");
		//                                //    cPLIPrevious.nFrameStop = (int)(((ulong)cPLIPrevious.nFrameStart + nFramesNeeded) / Preferences.nFPS * Preferences.nFPS);
		//                                //    QueueUpdate(cPLIPrevious);
		//                                //}
		//                            }
		//                            _cPlayer.QueueAdd(aPLIs[nIndx]);
		//                            cPLIPrevious = aPLIs[nIndx];
		//                            if (0 < nItemsLeft)
		//                                nItemsLeft--;
		//                            else if (Preferences.nEnqueueSafeDuration < aPLIs[nIndx].nDuration || aPLIs[nIndx].bPlug)
		//                                nItemsLeft = 2;
		//                        }
		//                        catch (Exception ex)
		//                        {
		//                            (new Logger("playlist")).WriteError(ex);
		//                        }
		//                    }
		//                }
		//            }
		//            catch (Exception ex)
		//            {
		//                (new Logger("playlist")).WriteError(ex);
		//            }
		//            bEnqueueNeed = false;
		//            Thread.Sleep(500);
		//        }
		//    }
		//    catch (Exception ex)
		//    {
		//        (new Logger("playlist")).WriteError(ex);
		//    }

		//    (new Logger("playlist")).WriteNotice("модуль плейлиста остановлен");//TODO LANG
		//    _nThreadsFinished++;
		//}
		//private void WatcherCues(object cStateInfo)
		//{
		//    try
		//    {
		//        (new Logger("cues")).WriteNotice("модуль титрования запущен");//TODO LANG
		//    }
		//    catch (Exception ex)
		//    {
		//        (new Logger("cues")).WriteError(ex);
		//    }

		//    (new Logger("cues")).WriteNotice("модуль титрования остановлен");//TODO LANG
		//    _nThreadsFinished++;
		//}
	}
}
