using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.Threading;
using System.Linq;

using pl=helpers.replica.pl;
using helpers.replica.pl;
using ingenie.userspace;
using System.Xml;
using helpers.extensions;

namespace replica
{
	public class Playlist
	{
		public interface IInteract
		{
			IInteract Init();

			void PlaylistReset();

			PlaylistItem PlaylistItemCurrentGet();
			PlaylistItem PlaylistItemLockedPreviousGet();

			PlaylistItem[] PlaylistItemsPlannedGet();

			ulong PlaylistItemOnAirFramesLeftGet(int nOneFrameInMs);

			bool PlaylistItemsTimingsUpdate();
			bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope);
			bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID);
			void PlaylistItemFail(PlaylistItem cPLI);
		}
		public class Preferences : helpers.Preferences
		{
			static protected Preferences _cInstance = new Preferences();

			static public long nMinimumFramesToEnqueue
			{
				get
				{
					return (long)_cInstance._nMinimumFramesToEnqueue;
				}
			}
			static public ulong nMinimumFramesInQueue
			{
				get
				{
					return _cInstance._nMinimumFramesInQueue;
				}
			}
			static public ulong nEnqueueMinimalSafeDuration
			{
				get
				{
					return _cInstance._nEnqueueSafeDuration;
				}
			}
			static public ulong nDurationMinimum
			{
				get
				{
					return _cInstance._nDurationMinimum;
				}
			}
            static public ulong nDurationMinimumFr
            {
                get
                {
                    return _cInstance._nDurationMinimumFr;
                }
            }
            static public ulong nDurationClipMinimum
			{
				get
				{
					return _cInstance._nDurationClipMinimum;
				}
			}
            static public helpers.replica.media.File cDefaultPlug
            {
                get
                {
                    return _cInstance._cDefaultPlug;
                }
            }
            static public double nStartHardThresholdMs
            {
                get
                {
                    return _cInstance._nStartHardThresholdMs;
                }
            }
            private ulong _nMinimumFramesToEnqueue;
			private ulong _nMinimumFramesInQueue;
			private ulong _nEnqueueSafeDuration;
			private ulong _nDurationMinimum;
            private ulong _nDurationMinimumFr;
            private ulong _nDurationClipMinimum;
            private helpers.replica.media.File _cDefaultPlug;
            private ulong _nStartHardThresholdMs;

            public Preferences()
				: base("//replica/player/playlist")
            { }
            override protected void LoadXML(XmlNode cXmlNode)
			{
				if (null == cXmlNode)
					return;
                _nMinimumFramesInQueue = (ulong)(cXmlNode.AttributeGet<TimeSpan>("dur_queue_min").TotalSeconds * Player.Preferences.nFPS);
                _nMinimumFramesToEnqueue = (ulong)cXmlNode.AttributeGet<TimeSpan>("dur_enqueue").TotalSeconds * Player.Preferences.nFPS;
				_nEnqueueSafeDuration = (ulong)cXmlNode.AttributeGet<TimeSpan>("safe").TotalMilliseconds;
				_nDurationMinimum = (ulong)cXmlNode.AttributeGet<TimeSpan>("durmin").TotalMilliseconds;
                _nDurationMinimumFr = _nDurationMinimum / (ulong)Player.Preferences.nFrameMs;
                _nDurationClipMinimum = (ulong)cXmlNode.AttributeGet<TimeSpan>("durclipmin").TotalMilliseconds;
                _nStartHardThresholdMs = (ulong)cXmlNode.AttributeGet<TimeSpan>("start_hard_threshold").TotalMilliseconds;
                (new Logger()).WriteNotice("prefs got start_hard_threshold: " + _nStartHardThresholdMs + " ms");

                long nID = cXmlNode.AttributeIDGet("plug_id");
                string sValue = cXmlNode.AttributeValueGet("plug_file");
                if (!System.IO.File.Exists(sValue))
                    throw new Exception("указанный файл заглушки не существует [file:" + sValue + "][" + cXmlNode.Name + "]"); //TODO LANG
                _cDefaultPlug = new helpers.replica.media.File(nID, System.IO.Path.GetFileName(sValue), new helpers.replica.media.Storage(nID, "default", System.IO.Path.GetDirectoryName(sValue) + System.IO.Path.AltDirectorySeparatorChar, true, nID, "default", null, null), DateTime.MaxValue, helpers.replica.Error.no, helpers.replica.media.File.Status.InStock, 0);
            }
		}
		public class Logger : helpers.Logger
		{
			static public string sFile = null;

			public Logger()
				: base("playlist", sFile)
			{ }
		}

		public WaitCallback WatcherCommands;

		private bool _bRunning;
		private bool _bStopped;
		private Player _cPlayer;
		private IInteract _iInteract;
		private int nIndxGCForced = 0;

		public void Start(IInteract iInteract, Player cPlayer)
		{
			try
			{
				_cPlayer = cPlayer;
				_iInteract = iInteract;
				_bRunning = true;
				_bStopped = false;

				ThreadPool.QueueUserWorkItem(Worker);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		public void Stop()
		{
			try
			{
				_bRunning = false;
				DateTime dt = DateTime.Now;
				while (!_bStopped && DateTime.Now.Subtract(dt).TotalSeconds < 2)
					Thread.Sleep(300);
				if (!_bStopped)
					(new Logger()).WriteNotice("превышено ожидание завершения потока");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		private void Worker(object cStateInfo)
		{
			try
			{
				(new Logger()).WriteNotice("модуль плейлиста запущен");//TODO LANG
				PlaylistItem cPLIPrevious = null;
				PlaylistItem cVeryFirstItem = null;
				ulong nPLQueueDurFrames = 0;
				long nPLQueueDurLeft = 0;
                int nDFr = Player.Preferences.nFrameMs;
				DateTime dtNextDurGet = DateTime.MinValue;
				bool bEnqueueNeed = true, bFirstRun = true, bOnAirGetFailed = false, bEnqueueWas = false;
				Logger.Timings cTimings = new helpers.Logger.Timings("player:playlistWorker");
                long nDiff;
//
                _iInteract.PlaylistReset(); //TODO ВНИМАНИЕ - НАДО НЕ ЖДАТЬ ПЕРЕСЧЕТА ПЛ всего - это могут быть 10 минут и более!!!!
//
				if(null != WatcherCommands)
					ThreadPool.QueueUserWorkItem(WatcherCommands);

				while (_bRunning)
				{
					try
					{
						//if(!bFirstRun)
							_iInteract = _iInteract.Init();
						if (null == cPLIPrevious)
						{
							try
							{
								cPLIPrevious = _iInteract.PlaylistItemCurrentGet();
								if (null == cPLIPrevious)
									throw new Exception("на текущее время запланированное воспроизведение отсутствует");
								if (null != cVeryFirstItem && cVeryFirstItem.nID == cPLIPrevious.nID)
								{
									(new Logger()).WriteWarning("НЕ ДОБАВЛЯЕМ ПЕРВЫЙ ЭЛЕМЕНТ, КОТОРЫЙ УЖЕ БЫЛ ДОБАВЛЕН РАНЕЕ:\t[id: " + cPLIPrevious.nID + "]\t[" + cPLIPrevious.cFile.sFile + "]");
									cVeryFirstItem = null;
								}
								else
								{
									_cPlayer.Add(cPLIPrevious);
									(new Logger()).WriteNotice("добавлено в очередь на воспроизведение впервые " + cPLIPrevious.ToString()); //TODO LANG
								}
								if (bFirstRun)
									cVeryFirstItem = cPLIPrevious;
							}
							catch (System.IO.FileNotFoundException e)
							{
								cPLIPrevious = null;
								(new Logger()).WriteError(new Exception("отсутствует указанный файл [" + e.Message + "]"));
							}
							catch (Exception ex)
							{
								cPLIPrevious = null;
								(new Logger()).WriteError(ex);
								Thread.Sleep(3000);
							}
							continue;
						}

						if (!bEnqueueNeed)
						{
							if (2 >= _cPlayer.nQueueLength)
							{
								(new Logger()).WriteNotice("слишком мало клипов в очереди - берём::qlen=" + _cPlayer.nQueueLength);
								bEnqueueNeed = true;
								nPLQueueDurLeft = 0;
							}
							else if (dtNextDurGet < DateTime.Now)
							{
								dtNextDurGet = DateTime.Now.AddSeconds(10); // что б не частить
								nPLQueueDurFrames = _cPlayer.nPlaylistDurationTotal;
								nPLQueueDurLeft = (long)(Preferences.nMinimumFramesInQueue - nPLQueueDurFrames);
								if (nPLQueueDurFrames < Preferences.nMinimumFramesInQueue)
								{
									(new Logger()).WriteNotice("очередь стала слишком мала - берём [total_dur=" + FrToStrMin(nPLQueueDurFrames) + " min][qlen=" + _cPlayer.nQueueLength + " шт][qleft=" + FrToStrMin(nPLQueueDurLeft) + " min]");
									bEnqueueNeed = true;

									if (nPLQueueDurFrames > Preferences.nMinimumFramesInQueue / 2)  // можно подождать длинного PLI
										try
										{
											ulong nCurDur = _iInteract.PlaylistItemOnAirFramesLeftGet(nDFr);
											if (Preferences.nEnqueueMinimalSafeDuration < (ulong)nDFr * nCurDur)
												bEnqueueNeed = true;
                                            else
                                            {
                                                (new Logger()).WriteNotice("недостаточно длинный текущий элемент и можно ждать - ждём [safe_dur = " + (Preferences.nEnqueueMinimalSafeDuration / (ulong)nDFr) + "][cur_pli_dur = " + nCurDur + "]");
                                                bEnqueueNeed = false;
                                            }
                                            bOnAirGetFailed = false;
										}
										catch (Exception ex)
										{
											if (bOnAirGetFailed)
												bEnqueueNeed = true;
											bOnAirGetFailed = true;
											(new Logger()).WriteError(ex);
										}
								}
							}
						}
						if (bEnqueueNeed)
						{
                            //
                            //							bFirstRun = true;
                            bEnqueueWas = false;


                            try
							{
								if (!bFirstRun)
								{
									_cPlayer._bAdding = true;
									(new Logger()).WriteDebug("sleep 1000 before 3 hour recalculate");
									System.Threading.Thread.Sleep(1000);  // ждем, чтобы в БД успел внестись SatrtReal... 
									_iInteract.PlaylistItemsTimingsUpdate(TimeSpan.FromHours(3));
								}
								else
								{
									bFirstRun = false;
								}
							}
							catch (Exception ex)
							{
								(new Logger()).WriteError("PlaylistItemsTimingsUpdate", ex);
							}
							try
							{
								cPLIPrevious = _iInteract.PlaylistItemLockedPreviousGet();
								(new Logger()).WriteNotice("previous item:" + (null == cPLIPrevious?"NULL":cPLIPrevious.ToString()));
							}
							catch (Exception ex)
							{
								(new Logger()).WriteError("PlaylistItemLockedPreviousGet", ex);
							}
							//PlaylistItem[] aPLIs = cDBI.PlaylistItemsPlannedGet(null == cPLIPrevious ? DateTime.Now : cPLIPrevious.dtStartPlanned);
							PlaylistItem[] aPLIs = _iInteract.PlaylistItemsPlannedGet();   // get 100 plis





//
							//foreach (PlaylistItem cPLI in aPLIs)
							//{
							//    helpers.replica.media.File cFile = new helpers.replica.media.File(1111111, @"d:\storages\clips\IndigoChaika_TyZhdesh_DVD_9.mov", helpers.replica.media.Storage.Empty, new DateTime(2012, 4, 1), helpers.replica.Error.no);
							//    cPLI.cFile = cFile;
							//}
//





							if (1 > aPLIs.Length)
							{
								(new Logger()).WriteError("PlaylistItemsPlannedGet: got nothing in aPLIs!!!");
								Thread.Sleep(500);
								continue;
							}

							if (nPLQueueDurLeft < Preferences.nMinimumFramesToEnqueue)
								nPLQueueDurLeft = Preferences.nMinimumFramesToEnqueue;

							Logger cLogger = new Logger();
							cLogger.WriteNotice("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@<br>start processing items [qty_plis:" + aPLIs.Length + "][durleft:" + FrToStrMin(nPLQueueDurLeft) + " min][id[0]:" + aPLIs[0].nID + "]");
							for (int nIndx = 0; aPLIs.Length > nIndx; nIndx++)
							{
								if (0 >= nPLQueueDurLeft)
									break;
								if (null != cVeryFirstItem && 0 == nIndx && cVeryFirstItem.nID == aPLIs[nIndx].nID)
								{
									cLogger.WriteWarning("ПРОПУСКАЕМ ПЕРВЫЙ ЭЛЕМЕНТ, КОТОРЫЙ УЖЕ БЫЛ ДОБАВЛЕН РАНЕЕ:\t[id: " + aPLIs[nIndx].nID + "]\t[" + aPLIs[nIndx].cFile.sFile + "]");
									cVeryFirstItem = null;
									continue;
								}
								cLogger.WriteNotice("[durleft:" + FrToStrMin(nPLQueueDurLeft) + " min]***************************************************************************");
								try
								{
									cLogger.WriteDebug("trying to add: " + aPLIs[nIndx].ToString()); //TODO LANG
									if (!System.IO.File.Exists(aPLIs[nIndx].cFile.sFile))
									{
										_iInteract.PlaylistItemFail(aPLIs[nIndx]); //TODO: выставить флаг ошибки на файл   //  это делает синкер
										//cPLIPrevious = aPLIs[nIndx];  // не надо, т.к. не добавили его и скорее всего будет дырка, о которой надо знать, т.к. вставим plug ниже
                                        cLogger.WriteError("НЕ НАЙДЕН ФАЙЛ:" + aPLIs[nIndx].cFile.sFile);
										continue;
									}

                                    if (Playlist.Preferences.nDurationMinimumFr > aPLIs[nIndx].nDuration)
                                    {
                                        //aPLIs[nIndx].nFrameStop = 0;
                                        _iInteract.PlaylistItemFail(aPLIs[nIndx]);
                                        continue;
                                    }

                                    if (bEnqueueWas && nPLQueueDurLeft < (long)aPLIs[nIndx].nDuration && aPLIs.Length - 1 > nIndx && DateTime.MaxValue > aPLIs[nIndx + 1].dtStartHard)
                                    {
                                        cLogger.WriteNotice("Предотвращено добавление последнего элемента перед хардом:<br>" + aPLIs[nIndx].ToString() + "<br>" + aPLIs[nIndx + 1].ToString());
                                        break;
                                    }

                                    if (null != cPLIPrevious)
									{
                                        if (DateTime.MaxValue > aPLIs[nIndx].dtStartHard)   //aPLIs[nIndx].cClass.sName.ToString().ToLower().Contains("advertisement")
                                        {
                                            nDiff = (long)aPLIs[nIndx].dtStartHard.Subtract(cPLIPrevious.dtStopPlanned).TotalMilliseconds;
                                            if (
                                                (
                                                     nDiff > Playlist.Preferences.nStartHardThresholdMs
                                                //    aPLIs[nIndx].dtStartHard > cPLIPrevious.dtStopPlanned.AddMilliseconds(Playlist.Preferences.nStartHardThresholdMs)
                                                )
                                            //|| 
                                            //(DateTime.MaxValue > aPLIs[nIndx].dtStartSoft
                                            //	&& aPLIs[nIndx].dtStartSoft > cPLIPrevious.dtStopPlanned.AddMilliseconds(Playlist.Preferences.nStartHardThresholdMs)
                                            //)
                                            )
                                            {
                                                cLogger.WriteError("ОБНАРУЖЕНО ОТСУТСТВИЕ КОНТЕНТА [tresho=" + (int)(Playlist.Preferences.nStartHardThresholdMs / nDFr) + "][frleft=" + (int)(nDiff / nDFr) + "] МЕЖДУ:<br>\t[pre=" + cPLIPrevious.ToString() + "]<br>\t[cur=" + aPLIs[nIndx].ToString() + "]");
                                                //cPLIPrevious = null;

                                                //if (nPLQueueDurLeft < (long)Preferences.nMinimumFramesInQueue / 2)
                                                //    break;

                                                if (Playlist.Preferences.cDefaultPlug != null)
                                                {
                                                    ffmpeg.net.File.Input cFile = new ffmpeg.net.File.Input(Playlist.Preferences.cDefaultPlug.sFile, 0);  //d:\storages\clips\reflex_yaneborazbila_9.mov
                                                    Playlist.Preferences.cDefaultPlug.nFramesQTY = (int)cFile.nFramesQty;
                                                    cFile.Close();
                                                    int nFrQty;
                                                    PlaylistItem cPlug, cPre= cPLIPrevious;
                                                    while (nDiff > Playlist.Preferences.nStartHardThresholdMs)
                                                    {
                                                        if (nDiff < nDFr * (long)Playlist.Preferences.cDefaultPlug.nFramesQTY.Value)
                                                            nFrQty = (int)(nDiff / nDFr);
                                                        else
                                                            nFrQty = Playlist.Preferences.cDefaultPlug.nFramesQTY.Value;
                                                        cPlug = new PlaylistItem() { nID = -1, bPlug = true, nFrameStart = 1, nFrameStop = nFrQty, dtStartPlanned = cPre.dtStopPlanned, cFile = Playlist.Preferences.cDefaultPlug };
                                                        nDiff -= nDFr * (long)nFrQty;
                                                        _cPlayer.Add(cPlug);
                                                        cPre = cPlug;
                                                        cLogger.WriteNotice("PLUG добавлен в очередь на воспроизведение [frleft=" + (int)(nDiff / nDFr) + "][frqty=" + nFrQty + "][f=" + Playlist.Preferences.cDefaultPlug.sFile + "]"); //TODO LANG
                                                    }
                                                }
                                                else if (aPLIs[nIndx].dtStartHard > DateTime.Now.AddSeconds(5))
                                                    break;
                                            }
                                        }
                                        //nFramesNeeded = (ulong)(aPLIs[nIndx].dtStartPlanned.Subtract(cPLIPrevious.dtStartPlanned).TotalSeconds * Preferences.nFPS);
                                        //if (cPLIPrevious.nDuration - nFramesNeeded >= Preferences.nFPS && Preferences.nMinimumFrames <= nFramesNeeded)
                                        //{
                                        //    cPLIPrevious.nFrameStop = (int)((((ulong)cPLIPrevious.nFrameStart + nFramesNeeded) / Preferences.nFPS) * Preferences.nFPS);
                                        //    (new Logger()).WriteNotice("допинфо [" + cPLIPrevious.nDuration + "][" + nFramesNeeded + "][" + cPLIPrevious.nFrameStop + "][" + cPLIPrevious.nFrameStart + "]");
                                        //    cPLIPrevious.nFrameStop = (int)(((ulong)cPLIPrevious.nFrameStart + nFramesNeeded) / Preferences.nFPS * Preferences.nFPS);
                                        //    QueueUpdate(cPLIPrevious);
                                        //}
                                    }
									_cPlayer.Add(aPLIs[nIndx]);
                                    bEnqueueWas = true;
                                    cLogger.WriteNotice("добавлено в очередь на воспроизведение " + aPLIs[nIndx].ToString()); //TODO LANG
									cPLIPrevious = aPLIs[nIndx];

									nPLQueueDurLeft -= (long)aPLIs[nIndx].nDuration;
								}
								catch (Exception ex)
								{
									(new Logger()).WriteError(ex);
								}
							}
							cLogger.WriteNotice("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
							_cPlayer._bAdding = false;
						}
					}
					catch (Exception ex)
					{
						_cPlayer._bAdding = false;
						(new Logger()).WriteError(ex);
					}
					bEnqueueNeed = false;


					//_nIndxGCForced++;
					//cTimings.TotalRenew();
					if (System.Runtime.GCSettings.LatencyMode != System.Runtime.GCLatencyMode.Interactive)
						System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Interactive;
					//GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
					//cTimings.Stop("GC > 10", "GC-" + "Optimized" + " " + System.Runtime.GCSettings.LatencyMode + " queue:", 10);


					Thread.Sleep(500);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("модуль плейлиста остановлен");//TODO LANG
		}
		private string FrToStrMin(long nFramesQty)
		{
			return ((float)nFramesQty / 60 / Player.Preferences.nFPS).ToString("0.0");
		}
		private string FrToStrMin(ulong nFramesQty)
		{
			return ((float)nFramesQty / 60 / Player.Preferences.nFPS).ToString("0.0");
		}

	}
}
