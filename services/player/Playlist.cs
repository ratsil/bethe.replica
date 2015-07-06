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

			ulong PlaylistItemOnAirFramesLeftGet();

			bool PlaylistItemsTimingsUpdate();
			bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope);
			bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID);
			void PlaylistItemFail(PlaylistItem cPLI);
		}
		public class Preferences : helpers.Preferences
		{
			static protected Preferences _cInstance = new Preferences();

			static public int nQueueLength
			{
				get
				{
					return _cInstance._nQueueLength;
				}
			}
			static public ulong nEnqueueSafeDuration
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
			static public ulong nDurationClipMinimum
			{
				get
				{
					return _cInstance._nDurationClipMinimum;
				}
			}

			private int _nQueueLength;
			private ulong _nEnqueueSafeDuration;
			private ulong _nDurationMinimum;
			private ulong _nDurationClipMinimum;

			public Preferences()
				: base("//replica/player/playlist")
            { }
            override protected void LoadXML(XmlNode cXmlNode)
			{
				if (null == cXmlNode)
					return;
				_nQueueLength = cXmlNode.AttributeGet<int>("length");
				_nEnqueueSafeDuration = (ulong)cXmlNode.AttributeGet<TimeSpan>("safe").TotalMilliseconds;
				_nDurationMinimum = (ulong)cXmlNode.AttributeGet<TimeSpan>("durmin").TotalMilliseconds;
				_nDurationClipMinimum = (ulong)cXmlNode.AttributeGet<TimeSpan>("durclipmin").TotalMilliseconds;
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
				int nEmptySlots = 0;
				bool bEnqueueNeed = true, bFirstRun = true, bOnAirGetFailed = false;
				short nItemsLeft = -1;
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
							if ((Preferences.nQueueLength - 2) > (nEmptySlots = Preferences.nQueueLength - _cPlayer.nQueueLength))
							{
								if (3 < nEmptySlots)
								{
									try
									{
										if (Preferences.nEnqueueSafeDuration < _iInteract.PlaylistItemOnAirFramesLeftGet())
											bEnqueueNeed = true;
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
							else
							{
								(new Logger()).WriteNotice("::" + Preferences.nQueueLength + "::" + nEmptySlots + "::" + _cPlayer.nQueueLength);
								bEnqueueNeed = true;
							}
						}
						if (bEnqueueNeed)
						{
//
//							bFirstRun = true;

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
									bFirstRun = false;
							}
							catch (Exception ex)
							{
								(new Logger()).WriteError(ex);
							}
							try
							{
								cPLIPrevious = _iInteract.PlaylistItemLockedPreviousGet();
								(new Logger()).WriteNotice("previous item:" + (null == cPLIPrevious?"NULL":cPLIPrevious.ToString()));
							}
							catch (Exception ex)
							{
								(new Logger()).WriteError(ex);
							}
							//PlaylistItem[] aPLIs = cDBI.PlaylistItemsPlannedGet(null == cPLIPrevious ? DateTime.Now : cPLIPrevious.dtStartPlanned);
							PlaylistItem[] aPLIs = _iInteract.PlaylistItemsPlannedGet();





//
							//foreach (PlaylistItem cPLI in aPLIs)
							//{
							//    helpers.replica.media.File cFile = new helpers.replica.media.File(1111111, @"d:\storages\clips\IndigoChaika_TyZhdesh_DVD_9.mov", helpers.replica.media.Storage.Empty, new DateTime(2012, 4, 1), helpers.replica.Error.no);
							//    cPLI.cFile = cFile;
							//}
//





							if (1 > aPLIs.Length)
							{
								Thread.Sleep(500);
								continue;
							}
							nItemsLeft = (short)nEmptySlots;
							Logger cLogger = new Logger();
							cLogger.WriteNotice("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@<br>start processing items [qty:" + aPLIs.Length + "][es:" + nEmptySlots + "][id[0]:" + aPLIs[0].nID + "]");
							for (int nIndx = 0; aPLIs.Length > nIndx; nIndx++)
							{
								if (0 == nItemsLeft)
									break;
								if (null != cVeryFirstItem && 0 == nIndx && cVeryFirstItem.nID == aPLIs[nIndx].nID)
								{
									cLogger.WriteWarning("ПРОПУСКАЕМ ПЕРВЫЙ ЭЛЕМЕНТ, КОТОРЫЙ УЖЕ БЫЛ ДОБАВЛЕН РАНЕЕ:\t[id: " + aPLIs[nIndx].nID + "]\t[" + aPLIs[nIndx].cFile.sFile + "]");
									cVeryFirstItem = null;
									continue;
								}
								cLogger.WriteNotice("[il:" + nItemsLeft + "]***************************************************************************");
								try
								{
									if (!System.IO.File.Exists(aPLIs[nIndx].cFile.sFile))
									{
										_iInteract.PlaylistItemFail(aPLIs[nIndx]); //TODO: выставить флаг ошибки на файл
										cPLIPrevious = aPLIs[nIndx];
										cLogger.WriteNotice("НЕ НАЙДЕН ФАЙЛ:" + aPLIs[nIndx].cFile.sFile);
										continue;
									}

									if (Player.Preferences.nThresholdFrames > aPLIs[nIndx].nDuration)
										aPLIs[nIndx].nFrameStop = 0;
									if (null != cPLIPrevious)
									{
										if (aPLIs[nIndx].cClass.sName.ToString().ToLower().Contains("advertisement"))
										{
											if (
												(DateTime.MaxValue > aPLIs[nIndx].dtStartHard
													&& aPLIs[nIndx].dtStartHard > cPLIPrevious.dtStopPlanned.AddMilliseconds(Player.Preferences.nThresholdMilliseconds)
												)
												|| 
												(DateTime.MaxValue > aPLIs[nIndx].dtStartSoft
													&& aPLIs[nIndx].dtStartSoft > cPLIPrevious.dtStopPlanned.AddMilliseconds(Player.Preferences.nThresholdMilliseconds)
												)
											)
											{
												cLogger.WriteWarning("ОБНАРУЖЕНО ОТСУТСТВИЕ КОНТЕНТА МЕЖДУ:<br>\t[" + cPLIPrevious.ToString() + "]<br>\t[" + aPLIs[nIndx].cFile.sFile + "]");
												//cPLIPrevious = null;
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
									if (2 > nItemsLeft && aPLIs.Length - 1 > nIndx && DateTime.MaxValue > aPLIs[nIndx + 1].dtStartHard)
									{
										cLogger.WriteNotice("Предотвращено добавление последнего элемента перед хардом:<br>" + aPLIs[nIndx].ToString() + "<br>" + aPLIs[nIndx + 1].ToString());
										break;
									}
									_cPlayer.Add(aPLIs[nIndx]);
									cLogger.WriteNotice("добавлено в очередь на воспроизведение " + aPLIs[nIndx].ToString()); //TODO LANG
									cPLIPrevious = aPLIs[nIndx];
									if (0 < nItemsLeft)
										nItemsLeft--;
									else if (Preferences.nEnqueueSafeDuration < aPLIs[nIndx].nDuration || aPLIs[nIndx].bPlug)
										nItemsLeft = 2;
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
					Thread.Sleep(500);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("модуль плейлиста остановлен");//TODO LANG
		}
	}
}
