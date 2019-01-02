using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

using System.Threading;
using System.Collections;
using System.IO;
using System.Xml;
using helpers;
using helpers.replica.pl;
using helpers.replica.cues;
using RC = replica.cues;
using helpers.extensions;

namespace replica
{
	public class Cues
	{
		public interface IInteract
		{
			IInteract Init();

			PlaylistItem PlaylistItemOnAirGet();
			Queue<PlaylistItem> PlaylistItemsPreparedGet();
			TemplateBind[] TemplateBindsGet(PlaylistItem cPLI);
		}
        /*
		public class Preferences : helpers.Preferences
		{
			static protected Preferences _cInstance = new Preferences();

			static public string sCacheFolder
			{
				get
				{
					return _cInstance._sCacheFolder;
				}
			}

			private string _sCacheFolder;

			public Preferences()
				: base("//replica/player")
			{
			}
			override protected void LoadXML(XmlNode cXmlNode)
			{
				if (null == cXmlNode)
					return;
				_sCacheFolder = cXmlNode.AttributeValueGet("cache");
				if (!System.IO.Directory.Exists(_sCacheFolder))
					throw new Exception("указанная папка кэша плеера не существует [cache:" + _sCacheFolder + "][" + cXmlNode.Name + "]"); //TODO LANG
			}
		}
        */
		public class Logger : helpers.Logger
		{
			static public string sFile = null;

			public Logger()
				: base("cues", sFile)
			{ }
		}

		private IInteract _iInteract;
		public WaitCallback WatcherCommands;
		private bool _bRunning;
		private bool _bStopped;
		protected static PlaylistItem _cCurrentPLI = null;

        public void Start(IInteract iInteract)
        {
			try
			{
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
				RC.Template.ProccesingStart();
				Dictionary<string, byte> ahFails = new Dictionary<string, byte>();
				List<long> aPassed = new List<long>();
				ahFails.Add("pli:onair", 0);
				ahFails.Add("pli:onair:start", 0);
				ahFails.Add("pli:prepared", 0);
				Queue<PlaylistItem> aPLIs = null;
				PlaylistItem cPlaylistItem = null;
				_cCurrentPLI = null;
				TemplateBind[] aTemplateBinds = null;
				DateTime dtCurrentStop;
				Dictionary<long, TemplateBind[]> ahPLIBinds = new Dictionary<long, TemplateBind[]>();
                string sLogClasses;


				if (null != WatcherCommands)
					ThreadPool.QueueUserWorkItem(WatcherCommands);

                _iInteract = _iInteract.Init();
                (new Logger()).WriteNotice("waiting for onair PLI playing now (not in the past)");
                while (true)
                {
                    PlaylistItem cPLI = _iInteract.PlaylistItemOnAirGet();
                    if (cPLI != null && cPLI.dtStopPlanned.AddSeconds(5) > DateTime.Now)
                    {
                        (new Logger()).WriteNotice("correct onair PLI found [id=" + cPLI.nID + "][pli.stop = " + cPLI.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                        break;
                    }
                }
                (new Logger()).WriteNotice("модуль титрования запущен");//TODO LANG

                while (_bRunning)
				{
					Thread.Sleep(500);
//					_iInteract = _iInteract.Init();
					try
					{
						try
						{
							#region get current pli
							if (null == (cPlaylistItem = _iInteract.PlaylistItemOnAirGet()) || DateTime.MaxValue == cPlaylistItem.dtStartReal)
							{
								if (2 == ahFails["pli:onair"])
									(new Logger()).WriteWarning("не найден текущий элемент плейлиста");
								if (3 > ahFails["pli:onair"])
									ahFails["pli:onair"]++;
								continue;
							}
							if (2 < ahFails["pli:onair"])
								(new Logger()).WriteWarning("найден текущий элемент плейлиста");
							ahFails["pli:onair"] = 0;

							if (null != _cCurrentPLI && _cCurrentPLI.nID == cPlaylistItem.nID)
							{
								if (DateTime.Now.AddSeconds(3) > _cCurrentPLI.dtStartReal.AddMilliseconds(_cCurrentPLI.nFramesQty * 40))
								{
									if (10 == ahFails["pli:onair:start"])
										(new Logger()).WriteWarning("невозможно определить начало следующего элемента плейлиста");
									if (11 > ahFails["pli:onair:start"])
										ahFails["pli:onair:start"]++;
								}
								continue;
							}
                            if (cPlaylistItem.aClasses == null)
                            {
                                (new Logger()).WriteError("no classes for pli [fn:" + cPlaylistItem.cFile.sFile + "]");
                                cPlaylistItem.aClasses = new Class[0];
                            }
                            sLogClasses = cPlaylistItem.aClasses.ToStr();
                            ahFails["pli:onair:start"] = 0;
                            (new Logger()).WriteNotice("::::::::::::::::::::найден новый текущий элемент плейлиста [fn:" + cPlaylistItem.cFile.sFile + "][sr:" + cPlaylistItem.dtStartReal + "][sp:" + cPlaylistItem.dtStopPlanned + "][fs:" + cPlaylistItem.nFrameStop + "][cla:" + cPlaylistItem.aClasses.ToStr() + "][id:" + cPlaylistItem.nID + "][cur_id:" + (null == _cCurrentPLI ? "null" : _cCurrentPLI.nID.ToString()) + "]"); //TODO LANG

                            if (!ahFails.ContainsKey("pli:class:" + sLogClasses))
								ahFails["pli:class:" + sLogClasses] = 0;

							if (!ahPLIBinds.ContainsKey(cPlaylistItem.nID))      // кладём сюда бинды ещё при обнаружении препаредов для экономии времени на текущем элементе, а то 10 секунд может занимать TemplateBindsGet!
							{
								(new Logger()).WriteNotice("для нового элемента нет заранее подготовленного массива с биндами - будем брать");
								if (null != (aTemplateBinds = _iInteract.TemplateBindsGet(cPlaylistItem)) && 0 < aTemplateBinds.Length)
									ahPLIBinds.Add(cPlaylistItem.nID, aTemplateBinds);   
							}
							if (ahPLIBinds.ContainsKey(cPlaylistItem.nID))
							{
								aTemplateBinds = ahPLIBinds[cPlaylistItem.nID];
								if (0 < ahFails["pli:class:" + sLogClasses])
								{
									(new Logger()).WriteWarning("найдены шаблоны графического оформления для класса " + sLogClasses);
									ahFails["pli:class:" + sLogClasses] = 0;
								}
								foreach (TemplateBind cTB in aTemplateBinds.ToArray())
								{
									if (!ahFails.ContainsKey("pli:template:" + cTB.cTemplate.nID))
										ahFails["pli:template:" + cTB.cTemplate.nID] = 0;
									if (!System.IO.File.Exists(cTB.cTemplate.sFile))
									{
										if (1 > ahFails["pli:template:" + cTB.cTemplate.nID])
											(new Logger()).WriteWarning("не найден файл шаблона:" + cTB.cTemplate.sFile);
										if (2 > ahFails["pli:template:" + cTB.cTemplate.nID])
											ahFails["pli:template:" + cTB.cTemplate.nID]++;
										aTemplateBinds = aTemplateBinds.Where(row => row != cTB).ToArray();
									}
									else if (ahFails.ContainsKey("pli:template:" + cTB.cTemplate.nID))
									{
										if (0 < ahFails["pli:template:" + cTB.cTemplate.nID])
											(new Logger()).WriteWarning("найден файл шаблона:" + cTB.cTemplate.sFile);
										ahFails["pli:template:" + cTB.cTemplate.nID] = 0;
									}
								}
								cPlaylistItem.dtStopReal = cPlaylistItem.dtStartReal.AddMilliseconds(cPlaylistItem.nDuration * 40);
								if (null != _cCurrentPLI)
									RC.Template.PlaylistItemStopped(_cCurrentPLI);
								RC.Template.PlaylistItemStarted(cPlaylistItem, aTemplateBinds);
								ahPLIBinds.Remove(cPlaylistItem.nID);
							}
							else
							{
								if (1 > ahFails["pli:class:" + sLogClasses])
									(new Logger()).WriteWarning("не найдено шаблонов графического оформления для класса " + sLogClasses);
								if (2 > ahFails["pli:class:" + sLogClasses])
									ahFails["pli:class:" + sLogClasses]++;
							}
							_cCurrentPLI = cPlaylistItem;
							#endregion
							#region get prepared plis
							if (null != (aPLIs = _iInteract.PlaylistItemsPreparedGet()) && 0 < aPLIs.Count)
							{
								if (2 == ahFails["pli:prepared"])
								{
									(new Logger()).WriteWarning("найдены подготовленные элементы плейлиста");
									if (3 > ahFails["pli:prepared"])
										ahFails["pli:prepared"]++;
								}

								dtCurrentStop = cPlaylistItem.dtStopReal;
								int nDelta;
								bool bBreaked = false;
								if (aPassed.Contains(cPlaylistItem.nID))
									aPassed.Remove(cPlaylistItem.nID);

								(new Logger()).WriteDebug2("before_while");
								while (0 < aPLIs.Count)    // ВНИМАНИЕ!  один цикл может достигать 1 секунды!!  А элементов может быть десятки! ... даже  10 секунд, т.к. много бывает навешано на иной класс!!     вродь фиксанул с помощью ahPLIBinds
								{  // причем много времени идёт на TemplateBindsGet
                                    if (!_bRunning) break;
									if ((nDelta = (int)dtCurrentStop.Subtract(DateTime.Now).TotalSeconds) < 2)
									{
										(new Logger()).WriteDebug2("срочно закончили обработку элементов плейлиста [id:" + cPlaylistItem.nID + "][plis_count:" + aPLIs.Count + "][delta:" + nDelta + "sec]");
										bBreaked = true;
										break;
									}
									cPlaylistItem = aPLIs.Dequeue();
                                    if ((aPLIs.Count + 1) * 1 > nDelta && aPassed.Contains(cPlaylistItem.nID))   // т.к. длительность обработки этого участка примерно 3 сек
									{
										(new Logger()).WriteDebug2("элемент плейлиста сразу пропущен, как уже обработанный ранее [id:" + cPlaylistItem.nID + "][plis_count:" + aPLIs.Count + "][delta:" + nDelta + "sec]");
										bBreaked = true;
										continue;
									}
                                    if (cPlaylistItem.aClasses == null)
                                    {
                                        (new Logger()).WriteWarning("no classes for pli [fn:" + cPlaylistItem.cFile.sFile + "]");
                                        aPassed.Add(cPlaylistItem.nID);
                                        continue;
                                    }
                                    sLogClasses = cPlaylistItem.aClasses.ToStr();

                                    (new Logger()).WriteDebug2("найден подготовленный элемент плейлиста [" + cPlaylistItem.nID + "]");
									if (!ahFails.ContainsKey("pli:class:" + sLogClasses))
										ahFails["pli:class:" + sLogClasses] = 0;
									if (null != (aTemplateBinds = _iInteract.TemplateBindsGet(cPlaylistItem)) && 0 < aTemplateBinds.Length)
									{
										if (!ahPLIBinds.ContainsKey(cPlaylistItem.nID))
											ahPLIBinds.Add(cPlaylistItem.nID, aTemplateBinds);
										else
											ahPLIBinds[cPlaylistItem.nID] = aTemplateBinds;

										if (0 < ahFails["pli:class:" + sLogClasses])
										{
											(new Logger()).WriteWarning("найдены шаблоны графического оформления для класса " + sLogClasses);
											ahFails["pli:class:" + sLogClasses] = 0;
										}
										foreach (TemplateBind cTB in aTemplateBinds.ToArray())
										{
											if (!ahFails.ContainsKey("pli:template:" + cTB.cTemplate.nID))
												ahFails["pli:template:" + cTB.cTemplate.nID] = 0;
											if (!System.IO.File.Exists(cTB.cTemplate.sFile))
											{
												if (1 > ahFails["pli:template:" + cTB.cTemplate.nID])
													(new Logger()).WriteWarning("не найден файл шаблона2:" + cTB.cTemplate.sFile);
												if (2 > ahFails["pli:template:" + cTB.cTemplate.nID])
													ahFails["pli:template:" + cTB.cTemplate.nID]++;
												aTemplateBinds = aTemplateBinds.Where(row => row != cTB).ToArray();
											}
											else if (ahFails.ContainsKey("pli:template:" + cTB.cTemplate.nID))
											{
												if (0 < ahFails["pli:template:" + cTB.cTemplate.nID])
													(new Logger()).WriteWarning("найден файл шаблона2:" + cTB.cTemplate.sFile);
												ahFails["pli:template:" + cTB.cTemplate.nID] = 0;
											}
										}
										RC.Template.PlaylistItemPrepared(cPlaylistItem, aTemplateBinds);
										aPassed.Add(cPlaylistItem.nID);
									}
									else
									{
										if (1 > ahFails["pli:class:" + sLogClasses])
											(new Logger()).WriteWarning("не найдено шаблонов графического оформления для класса " + sLogClasses);
										if (2 > ahFails["pli:class:" + sLogClasses])
											ahFails["pli:class:" + sLogClasses]++;
									}
								}
								(new Logger()).WriteDebug2("after_while");
								if (!bBreaked)
								{
									(new Logger()).WriteDebug2("все подготовленные элементы плейлиста обработаны без пропусков");
								}
							}
							else
							{
								if (2 == ahFails["pli:prepared"])
									(new Logger()).WriteWarning("невозможно найти подготовленные элементы плейлиста");
								if (3 > ahFails["pli:prepared"])
									ahFails["pli:prepared"]++;
							}
							#endregion
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
						}
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("модуль титрования остановлен"); //TODO LANG
		}
	}
}
