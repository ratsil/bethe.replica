using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Collections;
using helpers;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.cues;
using helpers.replica.pl;

namespace replica.cues
{
	public class Template : ingenie.userspace.Template
	{
		public interface IInteract
		{
			IInteract Init();

			Macro MacroGet(string sMacroName);
			string MacroExecute(Macro cMacro);
            TemplatesSchedule[] TemplatesScheduleGet();
			void TemplatesScheduleSave(TemplatesSchedule cTemplatesSchedule);
			void TemplateStarted(Range cRange);
			PlaylistItem PlaylistItemPreviousGet(PlaylistItem cPlaylistItem);
		}
		new public class Logger : ingenie.userspace.Template.Logger
		{}
		public class Range
		{
			public PlaylistItem cPlaylistItem { get; set; }
			public TemplateBind cTemplateBind { get; set; }
            public TemplatesSchedule cTemplatesSchedule { get; set; }
            public bool bAnyTime { get; set; }
			public DateTime dtStart { get; set; }
			public DateTime dtStop { get; set; }

			public Range()
			{
				dtStart = DateTime.MaxValue;
				dtStop = DateTime.MaxValue;
			}

			public override string ToString()
			{
				string sRetVal = "[anytime:" + bAnyTime + "][pli:";
				if (null != cPlaylistItem)
				{
					sRetVal += cPlaylistItem.nID.ToString() + ":" + cPlaylistItem.sName + "]";
					sRetVal += "[class:" + (null == cPlaylistItem.aClasses ? "NULL" : "[" + cPlaylistItem.aClasses.ToStr() + "]") + "]";
				}
				else
					sRetVal += "NULL]";
				sRetVal += "[bind:" + (null == cTemplateBind ? "NULL" : cTemplateBind.nID.ToString() + ":" + cTemplateBind.sKey) + "]";
				sRetVal += "[start:" + dtStart.ToStr() + "]";
				sRetVal += "[stop:" + dtStop.ToStr() + "]";
				return sRetVal;
			}
			public string ToStringShort()
			{
				string sRetVal = "[pli:";
				if (null != cPlaylistItem)
				{
					sRetVal += cPlaylistItem.nID.ToString() + ":" + cPlaylistItem.sName + "]";
				}
				else
					sRetVal += "NULL]";
				return sRetVal;
			}
		}


		static public IInteract iInteract;
		static private bool _bProcessing;
        static private List<TemplatesSchedule> _aTemplatesSchedule;
        static private List<Template> _aTemplates;
		static private object _cSyncRoot = new object();
		static private int _nIndxGCForced;

		private List<Range> _aRanges;
		private Range _cRangeStarted;
		private Range _cRangePreparing;
		private Range _cRangeLast; // для предотвращения запуска одного и того же темплейта на одно и то же время
		private bool RangeLookesTheSameAsLast(Range cRangeToTest)
		{
			if (null != _cRangeLast)
			{
				if (_cRangeLast.cPlaylistItem.nID == cRangeToTest.cPlaylistItem.nID && Math.Abs(cRangeToTest.dtStart.Subtract(_cRangeLast.dtStart).TotalSeconds) < 2)   // the same  
					return true;
			}
			return false;
		}
		private bool RangeLated(Range cRangeToTest)
		{
			if (DateTime.Now > cRangeToTest.dtStart.AddSeconds(5))    // comes too late
				return true;
			return false;
		}

		public int nPlaylistItemID { get; set; }
		private bool _bActual;
		private Status? _eStatusTarget;
		private int _nTimesWaiting;

		public Template(string sFile)
			: base(sFile, COMMAND.unknown)
		{
			_eStatusTarget = null;
			_nTimesWaiting = 0;
            if (null == _aTemplates)
				ProccesingStart();
			MacroExecute = OnMacroExecute;
			RuntimeGet = OnRuntimeGet;
			_aRanges = new List<Range>();
		}

		public void RangeAdd(Range cRange)
		{
			RangeAdd(cRange, false);
		}
		public void RangeAdd(Range cRange, bool bRenewOnly)
		{
			lock (_cSyncRoot)
			{
				if (0 < _aRanges.Count && null != cRange.cPlaylistItem && null != cRange.cTemplateBind)
				{
					Range cRangeExisted = _aRanges.FirstOrDefault(row => null != row.cPlaylistItem && null != row.cTemplateBind && row.cPlaylistItem.nID == cRange.cPlaylistItem.nID && row.cTemplateBind.nID == cRange.cTemplateBind.nID);
					if (null != cRangeExisted)
					{
						cRangeExisted.dtStart = cRange.dtStart;
						cRangeExisted.dtStop = cRange.dtStop;
						return;
					}
				}
				if (bRenewOnly)
					(new Logger()).WriteNotice("диапазон не обновлён [" + sFile + "]" + cRange.ToString());
				else
				{
					_aRanges.Add(cRange);
					(new Logger()).WriteNotice("добавлен диапазон [" + sFile + "]" + cRange.ToString());
				}
			}
		}
		public void Enqueue()
		{
			lock (_cSyncRoot)
			{
				if (0 < _aTemplates.Count(row => row.sFile == sFile))
					return;
				Done += Template_Done;
				_aTemplates.Add(this);
			}
		}
		void Template_Done(ingenie.userspace.Template cTemplate)
		{
			Template cTpl = (Template)cTemplate;
            lock (_cSyncRoot)
            {
                if (null != cTpl._cRangeStarted)
                {
                    if (cTpl._aRanges.Contains(cTpl._cRangeStarted))
                        cTpl._aRanges.Remove(cTpl._cRangeStarted);
                    if (null != cTpl._cRangeStarted.cTemplatesSchedule)
                    {
                        cTpl._cRangeStarted.cTemplatesSchedule.dtLast = DateTime.Now;
                        iInteract.TemplatesScheduleSave(cTpl._cRangeStarted.cTemplatesSchedule);
					}
					(new Logger()).WriteNotice("шаблон графического оформления остановлен:" + cTpl.sFile + " " + cTpl._cRangeStarted.ToStringShort());
					cTpl._cRangeLast = cTpl._cRangeStarted;
					cTpl._cRangeStarted = null;
                }
                else if (_bProcessing)
                    (new Logger()).WriteWarning("impossible state: отсутствует текущий диапазон - возможно елемент не смог быть подготовлен:" + cTpl.sFile);
                cTpl.Dispose();
                _aAtoms = new List<ingenie.userspace.Atom>();
				_eStatusTarget = null;
				_nTimesWaiting = 0;
                cTpl.eStatus = Status.Created;
				if (null != cFollowingTemplate)
				{
					cTpl = (Template)cFollowingTemplate;
					(new Logger()).WriteNotice("start following template:" + cTpl.sFile);
					cFollowingTemplate.Start();
					cFollowingTemplate = null;
				}
            }
		}

		private string OnMacroExecute(string sText)
		{
			Macro.Flags eFlags = Macro.ParseFlags(ref sText);
			string sRetVal = "";
			if (null != iInteract)
			{
				lock (iInteract)
					iInteract = iInteract.Init();
				Macro cMacro = iInteract.MacroGet(sText);
				switch (cMacro.cType.sName)
				{
					case "sql":
						cMacro.sValue = ProcessRuntimes(cMacro.sValue);
						sRetVal = iInteract.MacroExecute(cMacro);
						break;
					default:
						throw new Exception("обнаружен неизвестный тип макро-строки [" + cMacro.cType.sName + "]"); //TODO LANG
				}
			}
			if (null != sRetVal)
			{
				if (eFlags.HasFlag(Macro.Flags.Escaped))
					sRetVal = sRetVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
				if (eFlags.HasFlag(Macro.Flags.Caps))
					sRetVal = sRetVal.ToUpper();
			}
			else
				(new Logger()).WriteNotice("OnMacroExecute: return = NULL!!!");
			return sRetVal;
		}
		private string OnRuntimeGet(string sText)
		{
			Macro.Flags eFlags = Macro.ParseFlags(ref sText);
			string sRetVal = null;
            string sLog = "";
            switch (sText)
			{
				case "{%RUNTIME::PLI::ID%}":
					if (null != _cRangePreparing && null != _cRangePreparing.cPlaylistItem)
						sRetVal = _cRangePreparing.cPlaylistItem.nID.ToString();
					else if (null != _cRangeStarted && null != _cRangeStarted.cPlaylistItem)
						sRetVal = _cRangeStarted.cPlaylistItem.nID.ToString();
					break;
				case "{%RUNTIME::TCB::SCHEDULE::ID%}":
                    if (null != _cRangePreparing)
                    {
                        if (null != _cRangePreparing.cTemplatesSchedule)
							sRetVal = _cRangePreparing.cTemplatesSchedule.nID.ToString();
                    }
                    else if (null != _cRangeStarted)
                    {
                        sLog = "[_cRangePreparing is NULL] ";
                        if (null != _cRangeStarted.cTemplatesSchedule)
							sRetVal = _cRangeStarted.cTemplatesSchedule.nID.ToString();
                    }
                    else
                        sLog += "[_cRangeStarted is NULL] ";
                    break;
				case "{%RUNTIME::TCB::TEMPLATE::ID%}":
					(new Logger()).WriteNotice("OnRuntimeGet: case {%RUNTIME::TCB::TEMPLATE::ID%}");
					if (null != _cRangePreparing)
					{
						if (null != _cRangePreparing.cTemplateBind && null != _cRangePreparing.cTemplateBind.cTemplate)
						{
							(new Logger()).WriteNotice("OnRuntimeGet: null != _cRangePreparing: return = " + _cRangePreparing.cTemplateBind.cTemplate.nID);
							sRetVal = _cRangePreparing.cTemplateBind.cTemplate.nID.ToString();
						}
					}
					else if (null != _cRangeStarted)
					{
						if (null != _cRangeStarted.cTemplateBind && null != _cRangeStarted.cTemplateBind.cTemplate)
						{
							(new Logger()).WriteNotice("OnRuntimeGet: null == _cRangePreparing: return = " + _cRangePreparing.cTemplateBind.cTemplate.nID);
							sRetVal = _cRangeStarted.cTemplateBind.cTemplate.nID.ToString();
						}
					}
					break;
				default:
					throw new Exception("обнаружен запрос неизвестного runtime-свойства [" + sText + "]"); //TODO LANG
			}

			if (null != sRetVal)
			{
				if (eFlags.HasFlag(Macro.Flags.Escaped))
					sRetVal = sRetVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
				if (eFlags.HasFlag(Macro.Flags.Caps))
					sRetVal = sRetVal.ToUpper();
			}
            else
                (new Logger()).WriteNotice("OnRuntimeGet: return = NULL!!!  [rt=" + sText + "] " + sLog);
            return sRetVal;
		}
		public void InclusionAction(Inclusion cInclusion)
        {
            try
            {
				Func<Template> dTemplateFind = () =>
                    {
                        lock (_cSyncRoot)
                            return _aTemplates.FirstOrDefault(o => System.IO.Path.GetFullPath(o.sFile) == System.IO.Path.GetFullPath(cInclusion.sFile));
                    };
                Action dAction = null;
				Template cTemplate;
				switch (cInclusion.eAction)
                {
					case Inclusion.ACTION.Stepback:
						if (null != (cTemplate = dTemplateFind()) && cTemplate.eStatus == Status.Started)
						{
							_bActual = false;
							_eStatusTarget = null;
							_nTimesWaiting = 0;
							Range[] aRangesToDelete = ((Template)cInclusion.cParent)._aRanges.Where(o => o.cPlaylistItem.nID == (((Template)cInclusion.cParent)._cRangeStarted).cPlaylistItem.nID).ToArray();
							foreach (Range cRange in aRangesToDelete)
								if (_aRanges.Contains(cRange))
									_aRanges.Remove(cRange);
							cInclusion.cParent.Stop();
							(new Logger()).WriteNotice(cInclusion.cParent.sFile + " stepped back before " + cTemplate.sFile);
						}
						break;
					case Inclusion.ACTION.Wait:
						if (null != (cTemplate = dTemplateFind()) && cTemplate.eStatus == Status.Started)
						{
							_bActual = false;
							_eStatusTarget = null;
							_nTimesWaiting = 0;
                            cTemplate.cFollowingTemplate = cInclusion.cParent;
							Range[] aRangesToDelete = ((Template)cInclusion.cParent)._aRanges.Where(o => o.cPlaylistItem.nID == (((Template)cInclusion.cParent)._cRangeStarted).cPlaylistItem.nID).ToArray();
							foreach (Range cRange in aRangesToDelete)
								if (_aRanges.Contains(cRange))
									_aRanges.Remove(cRange);
							(new Logger()).WriteNotice("current template:" + cTemplate.sFile + "   following template:" + cTemplate.cFollowingTemplate.sFile);
						}
						break;
					case Inclusion.ACTION.Start:
						dAction = () =>
                        {
                            if (null == (cTemplate = dTemplateFind()))
                                lock (_cSyncRoot)
                                    _aTemplates.Add(cTemplate = new Template(cInclusion.sFile));
                            Range cRange = new Range() { dtStart = DateTime.Now, dtStop = DateTime.MaxValue, bAnyTime = true };
							Range cParentRange = ((Template)cInclusion.cParent)._cRangeStarted;
							if (null != cParentRange)
							{
								cRange.cPlaylistItem = cParentRange.cPlaylistItem;
								cRange.cTemplateBind = cParentRange.cTemplateBind;
							}
							cTemplate.RangeAdd(cRange);
						};
						break;
					case Inclusion.ACTION.Stop:
						dAction = () =>
						{
							if (null != (cTemplate = dTemplateFind()) && cTemplate.eStatus == Status.Started)
							{
								if (null != cTemplate._cRangeStarted)
								{
									foreach (Range cRange in cTemplate._aRanges.Where(row => (DateTime.MinValue < row.dtStart && row.dtStart < cTemplate._cRangeStarted.dtStop) || (DateTime.MinValue < row.dtStop && row.dtStop < cTemplate._cRangeStarted.dtStop)).ToArray())
									{
										(new Logger()).WriteNotice("удаление диапазона [" + cTemplate.sFile + "]" + cRange.ToString());
										cTemplate._aRanges.Remove(cRange);
									}
								}
								else
									(new Logger()).WriteWarning("impossible state: отсутствует текущий диапазон:" + cTemplate.sFile);
								cTemplate.Stop();
							}
						};
                        break;
                }
				if (null != dAction)
				{
					if (0 < cInclusion.nDelay)
					{
						ThreadPool.QueueUserWorkItem((o) =>
						{
							Thread.Sleep(TimeSpan.FromMilliseconds(cInclusion.nDelay * 40));  //FPS
							dAction();
						});
					}
					else
						dAction();
				}
			}
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
		protected override bool IsActual()
		{
			if (_bActual = base.IsActual())
			{
				if (null != aInclusions)
				{
					lock (_cSyncRoot)
					{
						foreach (Inclusion cInclusion in aInclusions)
							InclusionAction(cInclusion);
					}
				}
			}
			return _bActual;
		}
		public override void Start()
		{
			base.Start();
			if (_bActual)
			{
				if (null != _cRangeStarted)
					iInteract.TemplateStarted(_cRangeStarted);
				else
					(new Logger()).WriteWarning("impossible state: отсутствует текущий диапазон:" + sFile);
			}
		}

		static public void ProccesingStart()
		{
			lock (_cSyncRoot)
			{
				if (null == _aTemplates)
				{
					_aTemplates = new List<Template>();
                    _aTemplatesSchedule = new List<TemplatesSchedule>();
					_bProcessing = true;
					ThreadPool.QueueUserWorkItem(Worker);
				}
			}
		}

        static private int _nStopCount;
		static public void ProccesingStop()
		{
            _bProcessing = false;
            lock (_cSyncRoot)
			{
				if (null != _aTemplates)
				{
                    _nStopCount = _aTemplates.Count;
                    foreach (Template cTemplate in _aTemplates.ToArray())
					{
						try
						{
							(new Logger()).WriteNotice("ProccesingStop: [name=" + cTemplate.sFile + "][status=" + cTemplate.eStatus + "]");
                            ThreadPool.QueueUserWorkItem((o) =>
                            {
                                cTemplate.Stop();
                                cTemplate._aRanges.Clear();
                                cTemplate._cRangeLast = null;
                                cTemplate._cRangeStarted = null;
                                System.Threading.Interlocked.Decrement(ref _nStopCount);
                            });
						}
						catch (Exception ex)
						{
							(new Logger()).WriteWarning(ex);
						}
					}
                    while (_nStopCount > 0)
                        Thread.Sleep(10);
					_aTemplates.Clear();
					(new Logger()).WriteNotice("ProccesingStop: the end");
				}
			}
		}
		static private bool IsScheduleOnTime(TemplatesSchedule cTS, PlaylistItem cPLI)
		{
			(new Logger()).WriteDebug("IsScheduleOnTime: <br>\t\t[TS: [interval=" + cTS.tsInterval.ToString(@"dd\.hh\:mm\:ss") + "][start="+ cTS.dtStart + "][last="+ cTS.dtLast.ToString("yyyy-MM-dd HH:mm:ss") + "] <br>\t\tPLI: [name=" + cPLI.sName + "][start=" + cPLI.dtStart.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + cPLI.dtStop.ToString("yyyy-MM-dd HH:mm:ss") + "]]");
			if (1 > cTS.tsInterval.TotalMilliseconds)
				return false;

			if (cTS.tsInterval == TimeSpan.MaxValue)
			{
				if (cTS.dtStart >= cPLI.dtStart && cTS.dtStart < cPLI.dtStop && cTS.dtLast == DateTime.MaxValue)  // т.к. он вообще только один раз может пройти     1 > Math.Abs(cTS.dtLast.Subtract(cPLI.dtStart).TotalHours)
				{
					cTS.dtLast = cTS.dtStart;
					(new Logger()).WriteDebug2("IsScheduleOnTime: YES2 PLI=" + cPLI.sName + " [Last:" + cTS.dtLast.ToString("yyyy-MM-dd HH:mm:ss") + "]");
					return true;
				}
				else
					return false;
			}

			DateTime dtNearest = cTS.dtStart;
			if (cTS.dtStart < cPLI.dtStart)
				while (dtNearest < cPLI.dtStart)
					dtNearest = dtNearest.Add(cTS.tsInterval);

			if (cPLI.dtStop > dtNearest && (cTS.dtLast.IsNullOrEmpty() || dtNearest.Subtract(cTS.dtLast).TotalMilliseconds > cTS.tsInterval.TotalMilliseconds / 2))
			{
				cTS.dtLast = dtNearest;
				(new Logger()).WriteDebug2("IsScheduleOnTime: YES2 PLI=" + cPLI.sName + " [Nearest:" + dtNearest.ToString("yyyy-MM-dd HH:mm:ss") + "]");
				return true;
			}
			else if (cPLI.dtStop > dtNearest)
				(new Logger()).WriteDebug2("IsScheduleOnTime: PLI=" + cPLI.sName + " [Nearest:" + dtNearest.ToString("yyyy-MM-dd HH:mm:ss") + "] [Last:" + cTS.dtLast.ToString("yyyy-MM-dd HH:mm:ss") + "]");
			return false;
		}
		static public void PlaylistItemStarted(PlaylistItem cPlaylistItem, TemplateBind[] aTemplateBinds)
		{
			(new Logger()).WriteNotice("pli:started:in__" + cPlaylistItem.nID + "__" + cPlaylistItem.dtStart.ToString("HH:mm:ss.fff"));
			PlaylistItemPrepared(cPlaylistItem, aTemplateBinds);
			lock (_cSyncRoot)
			{
				if (null == _aTemplates)
					return;
				Template cTemplate = null;
				TemplateBind cTemplateBind = null;
				foreach (Range cRange in _aTemplates.SelectMany(o => o._aRanges.Where(r => cPlaylistItem.nID == r.cPlaylistItem.nID)).Distinct().ToArray())
				{
					cRange.cPlaylistItem = cPlaylistItem;
					cTemplateBind = cRange.cTemplateBind;
					switch (cTemplateBind.sKey)
					{
						case "start_offset":
							cRange.dtStart = (0 > cTemplateBind.nValue ? cPlaylistItem.dtStop : cPlaylistItem.dtStart).AddSeconds(cTemplateBind.nValue);
							(new Logger()).WriteNotice("обновлен диапазон по старту: [" + cTemplateBind.cTemplate.sFile + "]" + cRange.ToString());
							break;
						case "stop_offset":
							cRange.dtStop = (0 > cTemplateBind.nValue ? cPlaylistItem.dtStop : cPlaylistItem.dtStart).AddSeconds(cTemplateBind.nValue);
							(new Logger()).WriteNotice("обновлен диапазон по стопу: [" + cTemplateBind.cTemplate.sFile + "]" + cRange.ToString());
							break;
						case "start_after":
							if (null != cTemplateBind.cRegisteredTable && null != cTemplateBind.cRegisteredTable.sFullQualifiedName)
							{
								cRange.dtStart = cPlaylistItem.dtStart;
								cRange.bAnyTime = true;
							}
							else
								cRange.dtStart = cPlaylistItem.dtStop.Add(TimeSpan.FromSeconds(cTemplateBind.nValue));
							(new Logger()).WriteNotice("обновлен диапазон по старту [start_after][" + cTemplateBind.cTemplate.sFile + "]" + cRange.ToString());
							break;
						case "stop_before":
							if (null != cTemplateBind.cRegisteredTable && null != cTemplateBind.cRegisteredTable.sFullQualifiedName)
							{
								if (null != (cTemplate = _aTemplates.FirstOrDefault(o => cTemplateBind.cTemplate.sFile == o.sFile)))
								{
									if (Status.Started == cTemplate.eStatus)
									{
										(new Logger()).WriteNotice("принудительно останавливаем запущенный шаблон [stop_before][" + cTemplate.sFile + "]");
										cTemplate.StopAsync();
									}
								}
								else
									(new Logger()).WriteWarning("IS:NFI");
							}
							break;
						default:
							(new Logger()).WriteWarning("неизвестный ключ привязки классов и шаблонов графического оформления [" + cTemplateBind.sKey + "]");
							break;
					}
				}
				(new Logger()).WriteNotice("pli:started:out_-" + cPlaylistItem.nID + "-_-" + cPlaylistItem.dtStart.ToString("HH:mm:ss.fff"));
			}
		}
		static public void PlaylistItemPrepared(PlaylistItem cPlaylistItem, TemplateBind[] aTemplateBinds)
		{
			(new Logger()).WriteNotice("pli:prepared:in_-" + cPlaylistItem.nID + "-_-" + DateTime.Now.ToString("HH:mm:ss.fff"));
			if (null == _aTemplates)
				ProccesingStart();
			lock (_cSyncRoot)
			{
				Template cTemplate = null;
                TemplatesSchedule[] aTemplatesSchedule;
                TemplatesSchedule cTemplatesSchedule;
                Range cRange = null;
				bool bRenewOnly;
				PlaylistItem cPLIPrevious;
				DateTime dtTarget = DateTime.MaxValue;
				(new Logger()).WriteNotice("::::::::::::::::::::получены привязки:");
				foreach (TemplateBind cTemplateBind in aTemplateBinds.ToArray())
					(new Logger()).WriteNotice("::::::::::" + cTemplateBind.nID + ":" + cTemplateBind.sKey + ":" + cTemplateBind.nValue);
                aTemplatesSchedule = _aTemplatesSchedule.ToArray();
                _aTemplatesSchedule.Clear();
                foreach (TemplatesSchedule cTS in iInteract.TemplatesScheduleGet())
                {
                    if (null != (cTemplatesSchedule = aTemplatesSchedule.FirstOrDefault(o => cTS.nID == o.nID)))
                    {
                        cTemplatesSchedule.cTemplateBind = cTS.cTemplateBind;
                        cTemplatesSchedule.dtStart = cTS.dtStart;
                        cTemplatesSchedule.dtStop = cTS.dtStop;
                        cTemplatesSchedule.tsInterval = cTS.tsInterval;
						if (!cTS.dtLast.IsNullOrEmpty() && cTS.dtLast > cTemplatesSchedule.dtLast)
						{
							cTemplatesSchedule.dtLast = cTS.dtLast;
							(new Logger()).WriteDebug2("pli:prepared:last_changed:" + cTS.dtLast);
						}
                    }
                    else
                        cTemplatesSchedule = cTS;
                    _aTemplatesSchedule.Add(cTemplatesSchedule);
                }
                foreach (TemplateBind cTemplateBind in aTemplateBinds.ToArray())
				{
					cTemplate = _aTemplates.FirstOrDefault(row => row.sFile == cTemplateBind.cTemplate.sFile);
                    cTemplatesSchedule = null;
					if (		0 < (aTemplatesSchedule = _aTemplatesSchedule.Where(o => o.cTemplateBind.nID == cTemplateBind.nID).OrderByDescending(o => o.nID).ToArray()).Length 
								|| (cTemplateBind.cRegisteredTable != null && cTemplateBind.cRegisteredTable.sName == "tTemplatesSchedule")			)
					{
						if (null == (cTemplatesSchedule = aTemplatesSchedule.FirstOrDefault(o => IsScheduleOnTime(o, cPlaylistItem))))
						{
							if (null == cTemplate || null == (cRange = cTemplate._aRanges.FirstOrDefault(o => cPlaylistItem.nID == o.cPlaylistItem.nID && cTemplateBind.nID == o.cTemplateBind.nID)))
							{
								string sLog = "TemplatesSchedule in foreach [plid=" + cPlaylistItem.nID + "][tbid=" + cTemplateBind.nID + "]<br>\t\t";
								if (null != cTemplate)
									foreach (Range cR in cTemplate._aRanges)
										sLog += cR.ToString() + "<br>\t\t";
								(new Logger()).WriteNotice("у шаблона есть расписание, но время не подошло или прошло:" + cTemplateBind.cTemplate.sFile + sLog);
							}
							else
								(new Logger()).WriteNotice("у шаблона есть расписание и он уже заверстан [" + cTemplateBind.cTemplate.sFile + "][" + cRange.ToString() + "]");
							continue;
						}
						(new Logger()).WriteNotice("у шаблона есть расписание и пора показывать:" + cTemplateBind.cTemplate.sFile);
                    }
                    if (null == cTemplate)
					{
						cTemplate = new Template(cTemplateBind.cTemplate.sFile);
						cTemplate.Enqueue();
						(new Logger()).WriteNotice("загружен шаблон графического оформления:" + cTemplateBind.cTemplate.sFile);
					}
					bRenewOnly = false;
					cRange = new Range();
					cRange.cPlaylistItem = cPlaylistItem;
					cRange.cTemplateBind = cTemplateBind;
                    cRange.cTemplatesSchedule = cTemplatesSchedule;
                    cRange.bAnyTime = false;
					switch (cTemplateBind.sKey)
					{
						case "start_offset":
							cRange.dtStart = DateTime.MinValue;
							cRange.dtStop = DateTime.MaxValue;
							cRange.bAnyTime = (0 == cTemplateBind.nValue);
							break;
						case "stop_offset":
							cRange.dtStart = DateTime.MaxValue;
							cRange.dtStop = DateTime.MinValue;
							cRange.bAnyTime = true; //UNDONE maybe =)
							break;
						case "start_after":
							cRange.dtStart = DateTime.MinValue;
							cRange.dtStop = DateTime.MaxValue;
							cRange.bAnyTime = true; //UNDONE maybe =)
							break;
						case "stop_before":
							if (null == cTemplateBind.cRegisteredTable || null == cTemplateBind.cRegisteredTable.sFullQualifiedName)
							{
								if (DateTime.MaxValue > cPlaylistItem.dtStartReal && Status.Started == cTemplate.eStatus)
								{
									(new Logger()).WriteNotice("принудительно останавливаем запущенный шаблон по причине stop_before:" + cTemplate.sFile);
									cTemplate.StopAsync();
									aTemplateBinds = aTemplateBinds.Where(row => row != cTemplateBind).ToArray();
									continue;
								}
								if (null != (cPLIPrevious = iInteract.PlaylistItemPreviousGet(cPlaylistItem)))
								{
									//bool bIgnore = (cPLIPrevious.dtStartQueued < cPlaylistItem.dtStartQueued);
									cRange = cTemplate._aRanges.FirstOrDefault(o => cPLIPrevious.nID == o.cPlaylistItem.nID) ?? cRange;
									dtTarget = (0 > cTemplateBind.nValue ? cPLIPrevious.dtStop : cPLIPrevious.dtStart);
									if (cRange.cPlaylistItem.nID != cPLIPrevious.nID)
										(new Logger()).WriteNotice("будет добавлен диапазон по предварительному стопу: [" + cTemplate.sFile + "]" + cRange.ToString());
									else if ((0 > cTemplateBind.nValue && cRange.cPlaylistItem.dtStop != dtTarget) || (0 <= cTemplateBind.nValue && cRange.cPlaylistItem.dtStart != dtTarget))
										(new Logger()).WriteNotice("будет обновлен диапазон по предварительному стопу: [" + cTemplate.sFile + "]" + cRange.ToString());
									else
										continue;
									cRange.cPlaylistItem = cPLIPrevious;
									cRange.dtStop = dtTarget.AddSeconds(cTemplateBind.nValue);
									if (cRange.dtStop < DateTime.Now)
									{
										bRenewOnly = true;
										(new Logger()).WriteNotice("обновим диапазон по предварительному стопу только если он есть, т.к. стоп уже в прошлом: [" + cTemplate.sFile + "]" + cRange.ToString());
										continue;
									}
								}
								cRange.dtStart = DateTime.MaxValue;
								cRange.bAnyTime = true; //UNDONE maybe =)
							}
							else
							{
								aTemplateBinds = aTemplateBinds.Where(row => row != cTemplateBind).ToArray();
								continue;
							}
							break;
						default:
							(new Logger()).WriteWarning("неизвестный ключ привязки классов и шаблонов графического оформления:" + cTemplateBind.sKey);
							break;
					}
					cTemplate.RangeAdd(cRange, bRenewOnly);
				}
				(new Logger()).WriteNotice("pli:prepared:out_-" + cPlaylistItem.nID + "-_-" + DateTime.Now.ToString("HH:mm:ss.fff"));
			}
		}
		static public void PlaylistItemStopped(PlaylistItem cPlaylistItem)
		{
			lock (_cSyncRoot)
			{
				(new Logger()).WriteDebug2("pli:stop:in_-" + cPlaylistItem.nID + "-_-" + DateTime.Now.ToString("HH:mm:ss.fff"));
				List<Template> aTemplatesStopping = new List<Template>();
				foreach (Template cTemplate in _aTemplates.ToArray())
				{
					if (LIFETIME.PLI == cTemplate.eLifeTime && Status.Started == cTemplate.eStatus)
					{
						(new Logger()).WriteNotice("принудительно останавливаем запущенный шаблон:" + cTemplate.sFile);
						cTemplate.StopAsync();
						aTemplatesStopping.Add(cTemplate);
					}
					foreach (Range cRange in cTemplate._aRanges.Where(row => null == row.cPlaylistItem || row.cPlaylistItem.nID == cPlaylistItem.nID).ToArray())
						cTemplate._aRanges.Remove(cRange);
				}
				foreach (Template cTemplateRunning in aTemplatesStopping)
				{
					DateTime dtNoLock = DateTime.Now.AddSeconds(20);
					while (Status.Started == cTemplateRunning.eStatus && dtNoLock >= DateTime.Now) 
						Thread.Sleep(100);

					if (dtNoLock < DateTime.Now)
						(new Logger()).WriteError("запущенный шаблон не смог остановиться за 20 секунд!!!:" + cTemplateRunning.sFile);  // последствия не изучены
				}
				(new Logger()).WriteDebug2("pli:stop:out_-" + cPlaylistItem.nID + "-_-" + DateTime.Now.ToString("HH:mm:ss.fff"));
			}
		}
		static public void TEST_STOP_FORCED()
		{
			List<Template> aTemplatesStopping = new List<Template>();
            lock (_cSyncRoot)
            {
                foreach (Template cTemplate in _aTemplates.ToArray())
                {
                    if (LIFETIME.PLI == cTemplate.eLifeTime && Status.Started == cTemplate.eStatus)
                    {
                        (new Logger()).WriteNotice("принудительно останавливаем запущенный шаблон:" + cTemplate.sFile);
                        cTemplate.StopAsync();
                        aTemplatesStopping.Add(cTemplate);
                    }
                }
            }
			foreach (Template cTemplateRunning in aTemplatesStopping)
			{
				DateTime dtNoLock = DateTime.Now.AddSeconds(20);
				while (Status.Started == cTemplateRunning.eStatus && dtNoLock >= DateTime.Now) 
					Thread.Sleep(100);

				if (dtNoLock < DateTime.Now)
					(new Logger()).WriteError("запущенный шаблон не смог остановиться за 20 секунд!!!:" + cTemplateRunning.sFile);  // последствия не изучены
			}

		}
		static private void Worker(object cState)
		{
			try
			{
				(new Logger()).WriteNotice("модуль управления шаблонами графического оформления запущен");
				DateTime dt = DateTime.Now.AddMinutes(5);
				bool bBreak;
				List<Template> aTemplates = new List<Template>();
				List<Range> aRangesToRemove = new List<Range>();
				Logger.Timings cTimings = new helpers.Logger.Timings("cues:Worker", helpers.Logger.Level.debug3);
				while (true)
                {
                    if (!_bProcessing) break;
                    aTemplates.Clear();
					cTimings.TotalRenew();
					cTimings.Restart("before sync");
					lock (_cSyncRoot)
					{
						if (DateTime.Now > dt)
						{
							foreach (Template cTemplate in _aTemplates)
								(new Logger()).WriteNotice("before foreach: " +cTemplate.sFile + ": " + cTemplate.eStatus.ToString() + ": ranges = " + cTemplate._aRanges.Count);
							dt = DateTime.Now.AddMinutes(5);
						}
						cTimings.Restart("S1");
						foreach (Template cTemplate in _aTemplates)
						{
                            if (!_bProcessing) break;
                            cTimings.Restart("\n\tFE1 ["+ System.IO.Path.GetFileName(cTemplate.sFile) + "]");
							bBreak = false;
							aRangesToRemove.Clear();
							foreach (Range cRange in cTemplate._aRanges)
							{
                                if (!_bProcessing) break;
                                cTimings.Restart("\n\t\tFE2 [status=" + cTemplate.eStatus + "]" + cRange.ToStringShort());
                                if (null!=cRange.cPlaylistItem && cRange.cPlaylistItem.dtStopPlanned.AddHours(1) < DateTime.Now)
                                {
                                    aRangesToRemove.Add(cRange);  // пофиксил. а раньше ranges раздувались до невообразимых масштабов и кьюз приходилось перезапускать раз в квартал! )))
                                    (new Logger()).WriteDebug("фикс - устаревший диапазон будет удалён [f=" + cTemplate.sFile + "][rang_count=" + cTemplate._aRanges.Count + "]   " + cRange.ToString());
                                    continue;
                                }
								switch (cTemplate.eStatus)
								{
									case Status.Created:
										if (null != cTemplate._eStatusTarget && cTemplate._eStatusTarget != Status.Created)
										{
											if ((cTemplate._nTimesWaiting++) % 1 == 0)  // %30 -  раз в 10 секунд
												(new Logger()).WriteDebug3("still waiting for [target=" + cTemplate._eStatusTarget + "][" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
											break;
										}
										cTemplate._eStatusTarget = null;
                                        if (DateTime.MaxValue > cRange.dtStart && DateTime.MinValue < cRange.dtStart && !cRange.bAnyTime && (cTemplate.RangeLated(cRange) || cTemplate.RangeLookesTheSameAsLast(cRange)))  // прапорщеский зазор
										{
											if (cTemplate.RangeLookesTheSameAsLast(cRange))
												(new Logger()).WriteWarning(new Exception("пропущен диапазон на подготовке, т.к. он совпал с предыдущим [" + cTemplate.sFile + "][prev_r=" + cTemplate._cRangeLast.ToString() + "][range=" + cRange.ToString() + "]"));
											else
												(new Logger()).WriteError(new Exception("пропущен диапазон на подготовке, т.к. он опоздал более 5 сек [" + cTemplate.sFile + "][range=" + cRange.ToString() + "]"));
											aRangesToRemove.Add(cRange);
										}
										else if (DateTime.MaxValue > cRange.dtStart)
										{
											(new Logger()).WriteNotice("подготовка шаблона графического оформления [" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
											if (cTemplate._cRangePreparing != cRange)
											{
												cTemplate._cRangePreparing = cRange;
												cTemplate._eStatusTarget = Status.Prepared;
												cTemplate._nTimesWaiting = 0;
												aTemplates.Add(cTemplate);
												bBreak = true;
											}
											else
												(new Logger()).WriteWarning("данный диапазон уже подготавливается [" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
										}
										break;
									case Status.Prepared:
										if (null != cTemplate._eStatusTarget && cTemplate._eStatusTarget != Status.Prepared)
										{
											if ((cTemplate._nTimesWaiting++) % 1 == 0)  // %30 -  раз в 10 секунд
												(new Logger()).WriteDebug3("still waiting for [target=" + cTemplate._eStatusTarget + "][" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
											break;
										}
										cTemplate._eStatusTarget = null;
										if (DateTime.Now >= cRange.dtStart && DateTime.MinValue < cRange.dtStart)
										{
											(new Logger()).WriteNotice("запуск шаблона графического оформления [" + cTemplate.sFile + "][delta_sec=" + Math.Abs(DateTime.Now.Subtract(cRange.dtStart).TotalSeconds) + "]" + cRange.ToString());
											if (cRange.bAnyTime || 10 > Math.Abs(DateTime.Now.Subtract(cRange.dtStart).TotalSeconds))
											{
                                                if (cTemplate._cRangePreparing == cRange || cTemplate.bMispreparedShow)
                                                {
													if (cTemplate._cRangePreparing != cRange)
													{
														(new Logger()).WriteWarning("стартуемый диапазон не зарегистрирован, как подготовленный [" + cTemplate.sFile + "] стартуемый диапазон:" + cRange.ToString() + " подготовленный диапазон:" + (null == cTemplate._cRangePreparing ? " NULL" : cTemplate._cRangePreparing.ToString()));
														if (cTemplate._cRangePreparing != null && cTemplate._cRangePreparing.cPlaylistItem != null && cTemplate._cRangePreparing.cPlaylistItem.dtStopPlanned.AddMinutes(5) < DateTime.Now && cTemplate._aRanges.Contains(cTemplate._cRangePreparing))
														{
															aRangesToRemove.Add(cTemplate._cRangePreparing);
															(new Logger()).WriteNotice("будем удалять зависший подготовленный: " + cRange.ToString());
														}
													}
                                                    cTemplate._cRangePreparing = null;
                                                    if (cTemplate._cRangeStarted != cRange)
                                                    {
                                                        cTemplate._cRangeStarted = cRange;
														cTemplate._eStatusTarget = Status.Started;
														cTemplate._nTimesWaiting = 0;
														aTemplates.Add(cTemplate);
														bBreak = true;
                                                    }
                                                    else
                                                        (new Logger()).WriteWarning("данный диапазон уже является текущим [" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
                                                }
                                                else
                                                {
                                                    (new Logger()).WriteWarning("пропущен диапазон: стартуемый диапазон не зарегистрирован, как подготовленный [" + cTemplate.sFile + "] стартуемый диапазон:" + cRange.ToString() + " подготовленный диапазон:" + (null == cTemplate._cRangePreparing ? " NULL" : cTemplate._cRangePreparing.ToString()));
                                                    cTemplate.StopAsync();
                                                }
											}
											else
											{
												(new Logger()).WriteError(new Exception("пропущен диапазон на старте [" + cTemplate.sFile + "][anytime="+ cRange.bAnyTime + "][delta_sec="+ Math.Abs(DateTime.Now.Subtract(cRange.dtStart).TotalSeconds) + "][range=" + cRange.ToString() + "]"));
												aRangesToRemove.Add(cRange);
												cTemplate._eStatusTarget = Status.Stopped;
												cTemplate._nTimesWaiting = 0;
												aTemplates.Add(cTemplate);
											}
											aRangesToRemove.AddRange(cTemplate._aRanges.Where(row => (DateTime.MinValue < row.dtStart && row.dtStart < cRange.dtStart) || (DateTime.MinValue < row.dtStop && row.dtStop < cRange.dtStart)).ToArray());
										}
										break;
									case Status.Started:
										if (null != cTemplate._eStatusTarget && cTemplate._eStatusTarget != Status.Started)
										{
											if ((cTemplate._nTimesWaiting++) % 1 == 0)  // %30 -  раз в 10 секунд
												(new Logger()).WriteDebug3("still waiting for [target=" + cTemplate._eStatusTarget + "][" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
											break;
										}
										cTemplate._eStatusTarget = null;
										if (DateTime.MaxValue > cRange.dtStop && DateTime.MinValue < cRange.dtStop)
										{
											if (DateTime.Now >= cRange.dtStop)
											{
												(new Logger()).WriteNotice("остановка шаблона графического оформления [" + cTemplate.sFile + "][range=" + cRange.ToString() + "]");
												cTemplate._cRangeStarted = cRange;
												cTemplate._eStatusTarget = Status.Stopped;
												cTemplate._nTimesWaiting = 0;
												aTemplates.Add(cTemplate);
												bBreak = true;
												aRangesToRemove.AddRange(cTemplate._aRanges.Where(row => (DateTime.MinValue < row.dtStart && row.dtStart < cRange.dtStop) || (DateTime.MinValue < row.dtStop && row.dtStop < cRange.dtStop)).ToArray());
											}
										}
										break;
									case Status.Failed:
										if (null != cTemplate._cRangePreparing)
										{
											aRangesToRemove.Add(cTemplate._cRangePreparing);
											cTemplate._cRangePreparing = null;
										}
										else if (null != cTemplate._cRangeStarted)
										{
											aRangesToRemove.Add(cTemplate._cRangeStarted);
											cTemplate._cRangeLast = cTemplate._cRangeStarted;
											cTemplate._cRangeStarted = null;
										}
										else
											(new Logger()).WriteWarning("невозможно определить проблемный диапазон [" + cTemplate.sFile + "]" + cRange.ToString());
										cTemplate.Dispose();
										cTemplate._aAtoms = new List<ingenie.userspace.Atom>();
										cTemplate._eStatusTarget = null;
										cTemplate._nTimesWaiting = 0;
										cTemplate.eStatus = Status.Created;
										break;
								}
								if (bBreak)
									break;
							}
							foreach (Range cRange in aRangesToRemove)
                            {
                                (new Logger()).WriteNotice("удаление диапазона [f=" + cTemplate.sFile + "][rang_count=" + cTemplate._aRanges.Count + "]   " + cRange.ToString());
                                if (cTemplate._aRanges.Contains(cRange))
									cTemplate._aRanges.Remove(cRange);
							}
						}
						cTimings.Restart("\n\tF1out");
					}
					cTimings.Restart("\n\tSync Out");
					cTimings.Restart("after sync");
					foreach (Template cTemplate in aTemplates.Where(o => Status.Stopped == o._eStatusTarget))
						cTemplate.StopAsync();
					foreach (Template cTemplate in aTemplates.Where(o => Status.Started == o._eStatusTarget))
						cTemplate.StartAsync();
					foreach (Template cTemplate in aTemplates.Where(o => Status.Prepared == o._eStatusTarget))
						cTemplate.PrepareAsync();

					//_nIndxGCForced++;
					//cTimings.TotalRenew();
					if (System.Runtime.GCSettings.LatencyMode != System.Runtime.GCLatencyMode.Interactive)
						System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Interactive;
					//GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
					//cTimings.Stop("GC > 10", "GC-" + "Optimized" + " " + System.Runtime.GCSettings.LatencyMode + " queue:", 10);
					cTimings.Stop("foreach takes too long", "before sleep", 700);   

					Thread.Sleep(300);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("модуль управления шаблонами графического оформления остановлен");
		}
	}
}
