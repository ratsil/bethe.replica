using System;
using System.ServiceProcess;
using System.Threading;

using helpers.replica.pl;

namespace replica.player
{
    public partial class Service : ServiceBase
    {
		private Player _cPlayer;
		private Playlist _cPlaylist;
		private bool _bRunning;
		private int _nThreadsFinished;

		public Service()
        {
            InitializeComponent();
		}


//
		//public void Start()
		//{
		//    OnStart(null);
		//}
//



        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

			try
			{
				(new Logger("service")).WriteNotice("������� ������ �� ������");//TODO LANG
				_bRunning = true;
				_nThreadsFinished = 0;
				_cPlayer = new Player();
				replica.playlist.Calculator.Logger.sFile = "calculator";
				_cPlaylist = new Playlist();
				_cPlaylist.WatcherCommands = WatcherCommands;
				_cPlaylist.Start(new DBInteract(), _cPlayer);
				_cPlayer.Start(new DBInteract());
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
				(new Logger("service")).WriteNotice("������� ������ �� ���������");//TODO LANG
				_bRunning = false;
				DateTime dt = DateTime.Now;
				_cPlaylist.Stop();
				_cPlayer.Stop();
				while (1 > _nThreadsFinished && DateTime.Now.Subtract(dt).TotalSeconds < 2)
					Thread.Sleep(300);
				if (1 > _nThreadsFinished)
					(new Logger("service")).WriteNotice("��������� �������� ���������� �������");
			}
			catch (Exception ex)
			{
				(new Logger("service")).WriteError(ex);
			}
		}

		private void WatcherCommands(object cStateInfo)
		{
			(new Logger("commands")).WriteNotice("������ ���������� ��������� �������");//TODO LANG
			while (_bRunning)
			{
				try
				{
					(new DBInteract()).ProcessCommands(new Delegate[] { (DBInteract.PlayerSkipDelegate)_cPlayer.Skip });
					Thread.Sleep(300);
				}
				catch (Exception ex)
				{
					(new Logger("commands")).WriteError(ex);
					Thread.Sleep(5000);
				}
			}
			(new Logger("commands")).WriteNotice("������ ���������� ��������� ����������");//TODO LANG
			_nThreadsFinished++;
		}
		//private void WatcherPlaylist(object cStateInfo)
		//{
		//    try
		//    {
		//        (new Logger("playlist")).WriteNotice("������ ��������� �������");//TODO LANG
		//        PlaylistItem cPLIPrevious = null;
		//        DBInteract cDBI = null;
		//        int nEmptySlots = 0;
		//        ulong nFramesNeeded = 0;
		//        bool bEnqueueNeed = true, bFirstRun = true, bOnAirGetFailed = false;
		//        short nItemsLeft = -1;

		//        (new DBInteract()).PlaylistReset();

		//        ThreadPool.QueueUserWorkItem(new WaitCallback(WatcherCommands));

		//        while (_bRunning)
		//        {
		//            try
		//            {
		//                cDBI = new DBInteract();
		//                if (null == cPLIPrevious)
		//                {
		//                    try
		//                    {
		//                        cPLIPrevious = cDBI.PlaylistItemCurrentGet();
		//                        if (null == cPLIPrevious)
		//                            throw new Exception("�� ������� ����� ��������������� ��������������� �����������");
		//                        _cPlayer.QueueAdd(cPLIPrevious);
		//                    }
		//                    catch (System.IO.FileNotFoundException e)
		//                    {
		//                        cPLIPrevious = null;
		//                        (new Logger("playlist")).WriteError(new Exception("����������� ��������� ���� ["+e.Message+"]"));
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
		//                                if (Preferences.nEnqueueSafeDuration < cDBI.PlaylistItemOnAirFramesLeftGet())
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
		//                if(bEnqueueNeed)
		//                {
		//                    (new Logger("playlist")).WriteNotice("==========================================================================");
		//                    try
		//                    {
		//                        if(!bFirstRun)
		//                            cDBI.PlaylistItemsTimingsUpdate(TimeSpan.FromHours(3));
		//                        else
		//                            bFirstRun = false;
		//                    }
		//                    catch (Exception ex)
		//                    {
		//                        (new Logger("playlist")).WriteError(ex);
		//                    }
		//                    try
		//                    {
		//                        cPLIPrevious = cDBI.PlaylistItemLockedPreviousGet();
		//                    }
		//                    catch (Exception ex)
		//                    {
		//                        (new Logger("playlist")).WriteError(ex);
		//                    }
		//                    //PlaylistItem[] aPLIs = cDBI.PlaylistItemsPlannedGet(null == cPLIPrevious ? DateTime.Now : cPLIPrevious.dtStartPlanned);
		//                    PlaylistItem[] aPLIs = cDBI.PlaylistItemsPlannedGet();
		//                    nItemsLeft = (short)nEmptySlots;
		//                    for(int nIndx = 0; aPLIs.Length > nIndx; nIndx++)
		//                    {
		//                        if (0 == nItemsLeft)
		//                            break;
		//                        try
		//                        {
		//                            if(!System.IO.File.Exists(aPLIs[nIndx].cFile.sFile))
		//                            {
		//                                cDBI.PlaylistItemStop(aPLIs[nIndx]); //TODO: ��������� ���� ������ �� ����
		//                                cPLIPrevious = aPLIs[nIndx];
		//                                (new Logger("playlist")).WriteNotice("�� ������ ����:" + aPLIs[nIndx].cFile.sFile);
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
		//                                        (new Logger("playlist")).WriteNotice("���������� ���������� �������� ����� [" + cPLIPrevious.cFile.sFile + "] � [" + aPLIs[nIndx].cFile.sFile + "]");
		//                                        (new Logger("playlist")).WriteNotice("***************************************************************************");
		//                                        cPLIPrevious = null;
		//                                        break;
		//                                    }
		//                                }
		//                                //nFramesNeeded = (ulong)(aPLIs[nIndx].dtStartPlanned.Subtract(cPLIPrevious.dtStartPlanned).TotalSeconds * Preferences.nFPS);
		//                                //if (cPLIPrevious.nDuration - nFramesNeeded >= Preferences.nFPS && Preferences.nMinimumFrames <= nFramesNeeded)
		//                                //{
		//                                //    cPLIPrevious.nFrameStop = (int)((((ulong)cPLIPrevious.nFrameStart + nFramesNeeded) / Preferences.nFPS) * Preferences.nFPS);
		//                                //    (new Logger("playlist")).WriteNotice("������� [" + cPLIPrevious.nDuration + "][" + nFramesNeeded + "][" + cPLIPrevious.nFrameStop + "][" + cPLIPrevious.nFrameStart + "]");
		//                                //    cPLIPrevious.nFrameStop = (int)(((ulong)cPLIPrevious.nFrameStart + nFramesNeeded) / Preferences.nFPS * Preferences.nFPS);
		//                                //    QueueUpdate(cPLIPrevious);
		//                                //}
		//                            }
		//                            _cPlayer.QueueAdd(aPLIs[nIndx]);
		//                            cPLIPrevious = aPLIs[nIndx];
		//                            if(0 < nItemsLeft)
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

		//    (new Logger("playlist")).WriteNotice("������ ��������� ����������");//TODO LANG
		//    _nThreadsFinished++;
		//}
	}
}
