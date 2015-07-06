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
					sRetVal += "[class:" + cPlaylistItem.cClass.nID + ":" + cPlaylistItem.cClass.sName + "]";
				}
				else
					sRetVal += "NULL]";
				sRetVal += "[bind:" + (null == cTemplateBind ? "NULL" : cTemplateBind.nID.ToString() + ":" + cTemplateBind.sKey) + "]";
				sRetVal += "[start:" + dtStart.ToStr() + "]";
				sRetVal += "[stop:" + dtStop.ToStr() + "]";
				return sRetVal;
			}
		}


		static public IInteract iInteract;
		static private bool _bProcessing;
        static private List<TemplatesSchedule> _aTemplatesSchedule;
        static private List<Template> _aTemplates;
		static private object _cSyncRoot = new object();

		private List<Range> _aRanges;
		private Range _cRangeStarted;
		private Range _cRangePreparing;

		public int nPlaylistItemID { get; set; }
		private bool _bActual;
		private Status? _eStatusTarget;

		public Template(string sFile)
			: base(sFile, COMMAND.unknown)
		{
			_eStatusTarget = null;
			if (null == _aTemplates)
				ProccesingStart();
			MacroExecute = OnMacroExecute;
			RuntimeGet = OnRuntimeGet;
			_aRanges = new List<Range>();
		}

		public void RangeAdd(Range cRange)
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
					//cRangeExisted = _aRanges.FirstOrDefault(row => row.cPlaylistItem.nID == cRange.cPlaylistItem.nID);
					//if (null != cRangeExisted)
					//{
					//    if (cRangeExisted.dtStart == cRange.dtStart && cRangeExisted.dtStop == cRange.dtStop)
					//        return;
					//    bool bReturn = false;
					//    if (DateTime.MaxValue == cRangeExisted.dtStart && DateTime.MaxValue > cRange.dtStart)
					//    {
					//        cRangeExisted.dtStart = cRange.dtStart;
					//        bReturn = true;
					//    }
					//    if (DateTime.MaxValue == cRangeExisted.dtStop && DateTime.MaxValue > cRange.dtStop)
					//    {
					//        cRangeExisted.dtStop = cRange.dtStop;
					//        bReturn = true;
					//    }
					//    if (bReturn)
					//        return;
					//}
				}
				_aRanges.Add(cRange);
				(new Logger()).WriteNotice("добавлен диапазон [" + sFile + "]" + cRange.ToString());
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
                    cTpl._cRangeStarted = null;
                    (new Logger()).WriteNotice("шаблон графического оформления остановлен:" + cTpl.sFile);
                }
                else if (_bProcessing)
                    (new Logger()).WriteWarning("impossible state: отсутствует текущий диапазон - возможно елемент не смог быть подготовлен:" + cTpl.sFile);
                cTpl.Dispose();
                _aAtoms = new List<ingenie.userspace.Atom>();
				_eStatusTarget = null;
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
                        if (null != _cRangeStarted.cTemplatesSchedule)
							sRetVal = _cRangeStarted.cTemplatesSchedule.nID.ToString();
                    }
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
				(new Logger()).WriteNotice("OnRuntimeGet: return = NULL!!!");
			return sRetVal;
		}
		public void InclusionAction(Inclusion cInclusion)
        {
            try
            {
				Func<Template> dTemplateFind = () =>
					{
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
		static public void ProccesingStop()
		{
			lock (_cSyncRoot)
			{
				if (null != _aTemplates)
				{
					_bProcessing = false;
					foreach (Template cTemplate in _aTemplates.ToArray())
					{
						try
						{
							cTemplate.Stop();
							cTemplate._aRanges.Clear();
							cTemplate._cRangeStarted = null;
						}
						catch (Exception ex)
						{
							(new Logger()).WriteWarning(ex);
						}
					}
					_aTemplates.Clear();
					//GC.Collect();
				}
			}
		}
		static private bool IsScheduleOnTime(TemplatesSchedule cTS, PlaylistItem cPLI)
		{
			if (1 > cTS.tsInterval.TotalMilliseconds)
				return false;
			DateTime dtNearest = cTS.dtStart;
			if (cTS.dtStart < cPLI.dtStart)
				while (dtNearest < cPLI.dtStart)
					dtNearest = dtNearest.Add(cTS.tsInterval);

			if (cPLI.dtStop > dtNearest && (cTS.dtLast.IsNullOrEmpty() || dtNearest.Subtract(cTS.dtLast).TotalMilliseconds > cTS.tsInterval.TotalMilliseconds / 2))
			{
				cTS.dtLast = dtNearest;
				return true;
			}
			else if (cPLI.dtStop > dtNearest)
				(new Logger()).WriteDebug2("IsScheduleOnTime_false: PLI=" + cPLI.sName + "Nearest:" + dtNearest + " Last:" + cTS.dtLast);
			return false;
		}
		static public void PlaylistItemStarted(PlaylistItem cPlaylistItem, TemplateBind[] aTemplateBinds)
		{
			(new Logger()).WriteNotice("pli:started:in:" + cPlaylistItem.nID + ":" + DateTime.Now.ToString("HH:mm:ss.ffff"));
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
				(new Logger()).WriteNotice("pli:started:out:" + cPlaylistItem.nID + ":" + DateTime.Now.ToString("HH:mm:ss.ffff"));
			}
		}
		static public void PlaylistItemPrepared(PlaylistItem cPlaylistItem, TemplateBind[] aTemplateBinds)
		{
			(new Logger()).WriteNotice("pli:prepared:in:" + cPlaylistItem.nID + ":" + DateTime.Now.ToString("HH:mm:ss.ffff"));
			if (null == _aTemplates)
				ProccesingStart();
			lock (_cSyncRoot)
			{
				Template cTemplate = null;
                TemplatesSchedule[] aTemplatesSchedule;
                TemplatesSchedule cTemplatesSchedule;
                Range cRange = null;
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
					if (0 < (aTemplatesSchedule = _aTemplatesSchedule.Where(o => o.cTemplateBind.nID == cTemplateBind.nID).OrderByDescending(o => o.nID).ToArray()).Length)
                    {
						if (null == (cTemplatesSchedule = aTemplatesSchedule.FirstOrDefault(o => IsScheduleOnTime(o, cPlaylistItem))))
						{
							if (null == cTemplate || null == (cRange = cTemplate._aRanges.FirstOrDefault(o => cPlaylistItem.nID == o.cPlaylistItem.nID && cTemplateBind.nID == o.cTemplateBind.nID)))
								(new Logger()).WriteNotice("у шаблона есть расписание, но время не подошло или прошло:" + cTemplateBind.cTemplate.sFile);
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
									else if ((0 > cTemplateBind.nValue && cRange.cPlaylistItem.dtStop != dtTarget) || (-1 < cTemplateBind.nValue && cRange.cPlaylistItem.dtStart != dtTarget))
										(new Logger()).WriteNotice("обновлен диапазон по предварительному стопу: [" + cTemplate.sFile + "]" + cRange.ToString());
									else
										continue;
									cRange.cPlaylistItem = cPLIPrevious;

									cRange.dtStop = dtTarget.AddSeconds(cTemplateBind.nValue);
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
					cTemplate.RangeAdd(cRange);
				}
				(new Logger()).WriteNotice("pli:prepared:out:" + cPlaylistItem.nID + ":" + DateTime.Now.ToString("HH:mm:ss.ffff"));
			}
		}
		static public void PlaylistItemStopped(PlaylistItem cPlaylistItem)
		{
			lock (_cSyncRoot)
			{
				(new Logger()).WriteDebug2("pli:stop:in:" + DateTime.Now.ToString("HH:mm:ss.ffff"));
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
					while (Status.Started == cTemplateRunning.eStatus) //UNDONE есть вероятность deadlock'а
						Thread.Sleep(100);
				}
				(new Logger()).WriteDebug2("pli:stop:out:" + DateTime.Now.ToString("HH:mm:ss.ffff"));
			}
		}
		static public void TEST_STOP_FORCED()
		{
			List<Template> aTemplatesStopping = new List<Template>();
			foreach (Template cTemplate in _aTemplates.ToArray())
			{
				if (LIFETIME.PLI == cTemplate.eLifeTime && Status.Started == cTemplate.eStatus)
				{
					(new Logger()).WriteNotice("принудительно останавливаем запущенный шаблон:" + cTemplate.sFile);
					cTemplate.StopAsync();
					aTemplatesStopping.Add(cTemplate);
				}
			}
			foreach (Template cTemplateRunning in aTemplatesStopping)
			{
				while (Status.Started == cTemplateRunning.eStatus) //UNDONE есть вероятность deadlock'а
					Thread.Sleep(100);
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
				List<Range> aRangesForRemove = new List<Range>();
				ushort nWait;
				Status eStatus = Status.Created;
				while (true)
				{
					foreach (Template cTemplate in aTemplates)
					{
						nWait = 0;
						while (null != cTemplate._eStatusTarget && cTemplate._eStatusTarget > (eStatus = cTemplate.eStatus)) //UNDONE возможный deadlock
						{
							Thread.Sleep(5);
							nWait += 2;
							if (2000 < nWait)
							{
								nWait = 1;
								(new Logger()).WriteDebug3("still waiting for " + cTemplate._eStatusTarget + " [f:" + cTemplate.sFile + "][s:" + eStatus + "]");
							}
						}
						if (1 == (nWait & 1))
							(new Logger()).WriteDebug3("end waiting for " + cTemplate._eStatusTarget + " [f:" + cTemplate.sFile + "][s:" + eStatus + "]");
						cTemplate._eStatusTarget = null;
					}
					if (!_bProcessing)
						break;
					aTemplates.Clear();

					lock (_cSyncRoot)
					{
						if (DateTime.Now > dt)
						{
							foreach (Template cTemplate in _aTemplates)
								(new Logger()).WriteNotice(cTemplate.sFile + ":" + cTemplate.eStatus.ToString() + ":" + cTemplate._aRanges.Count);
							dt = DateTime.Now.AddMinutes(5);
						}
						foreach (Template cTemplate in _aTemplates)
						{
							bBreak = false;
							aRangesForRemove.Clear();
							foreach (Range cRange in cTemplate._aRanges)
							{
								switch (cTemplate.eStatus)
								{
									case Status.Created:
										if (DateTime.MaxValue > cRange.dtStart && DateTime.MinValue < cRange.dtStart && !cRange.bAnyTime && DateTime.Now.Subtract(TimeSpan.FromSeconds(5)) > cRange.dtStart)
										{
											(new Logger()).WriteError(new Exception("пропущен диапазон на подготовке [" + cTemplate.sFile + "]" + cRange.ToString()));
											aRangesForRemove.Add(cRange);
										}
										else if (DateTime.MaxValue > cRange.dtStart)
										{
											(new Logger()).WriteNotice("подготовка шаблона графического оформления [" + cTemplate.sFile + "]" + cRange.ToString());
											if (cTemplate._cRangePreparing != cRange)
											{
												cTemplate._cRangePreparing = cRange;
												cTemplate._eStatusTarget = Status.Prepared;
												aTemplates.Add(cTemplate);
												bBreak = true;
											}
											else
												(new Logger()).WriteWarning("данный диапазон уже подготавливается [" + cTemplate.sFile + "]" + cRange.ToString());
										}
										break;
									case Status.Prepared:
										if (DateTime.Now >= cRange.dtStart && DateTime.MinValue < cRange.dtStart)
										{
											(new Logger()).WriteNotice("запуск шаблона графического оформления [" + cTemplate.sFile + "]" + cRange.ToString());
											if (cRange.bAnyTime || 10 > Math.Abs(DateTime.Now.Subtract(cRange.dtStart).TotalSeconds))
											{
                                                if (cTemplate._cRangePreparing == cRange || cTemplate.bMispreparedShow)
                                                {
                                                    if (cTemplate._cRangePreparing != cRange) 
                                                        (new Logger()).WriteWarning("стартуемый диапазон не зарегистрирован, как подготовленный [" + cTemplate.sFile + "] стартуемый диапазон:" + cRange.ToString() + " подготовленный диапазон:" + (null == cTemplate._cRangePreparing ? " NULL" : cTemplate._cRangePreparing.ToString()));
                                                    cTemplate._cRangePreparing = null;
                                                    if (cTemplate._cRangeStarted != cRange)
                                                    {
                                                        cTemplate._cRangeStarted = cRange;
														cTemplate._eStatusTarget = Status.Started;
														aTemplates.Add(cTemplate);
														bBreak = true;
                                                    }
                                                    else
                                                        (new Logger()).WriteWarning("данный диапазон уже является текущим [" + cTemplate.sFile + "]" + cRange.ToString());
                                                }
                                                else
                                                {
                                                    (new Logger()).WriteWarning("пропущен диапазон: стартуемый диапазон не зарегистрирован, как подготовленный [" + cTemplate.sFile + "] стартуемый диапазон:" + cRange.ToString() + " подготовленный диапазон:" + (null == cTemplate._cRangePreparing ? " NULL" : cTemplate._cRangePreparing.ToString()));
                                                    cTemplate.StopAsync();
                                                }
											}
											else
											{
												(new Logger()).WriteError(new Exception("пропущен диапазон на старте [" + cTemplate.sFile + "]" + cRange.ToString() ));
												aRangesForRemove.Add(cRange);
												cTemplate._eStatusTarget = Status.Stopped;
												aTemplates.Add(cTemplate);
											}
											aRangesForRemove.AddRange(cTemplate._aRanges.Where(row => (DateTime.MinValue < row.dtStart && row.dtStart < cRange.dtStart) || (DateTime.MinValue < row.dtStop && row.dtStop < cRange.dtStart)).ToArray());
										}
										break;
									case Status.Started:
										if (DateTime.MaxValue > cRange.dtStop && DateTime.MinValue < cRange.dtStop)
										{
											if (DateTime.Now >= cRange.dtStop)
											{
												(new Logger()).WriteNotice("остановка шаблона графического оформления [" + cTemplate.sFile + "]" + cRange.ToString());
												cTemplate._cRangeStarted = cRange;
												cTemplate._eStatusTarget = Status.Stopped;
												aTemplates.Add(cTemplate);
												bBreak = true;
												aRangesForRemove.AddRange(cTemplate._aRanges.Where(row => (DateTime.MinValue < row.dtStart && row.dtStart < cRange.dtStop) || (DateTime.MinValue < row.dtStop && row.dtStop < cRange.dtStop)).ToArray());
											}
										}
										break;
									case Status.Failed:
										if (null != cTemplate._cRangePreparing)
										{
											aRangesForRemove.Add(cTemplate._cRangePreparing);
											cTemplate._cRangePreparing = null;
										}
										else if (null != cTemplate._cRangeStarted)
										{
											aRangesForRemove.Add(cTemplate._cRangeStarted);
											cTemplate._cRangeStarted = null;
										}
										else
											(new Logger()).WriteWarning("невозможно определить проблемный диапазон [" + cTemplate.sFile + "]" + cRange.ToString());
										cTemplate.Dispose();
										cTemplate._aAtoms = new List<ingenie.userspace.Atom>();
										cTemplate._eStatusTarget = null;
										cTemplate.eStatus = Status.Created;
										break;
								}
								if (bBreak)
									break;
							}
							foreach (Range cRange in aRangesForRemove)
							{
                                (new Logger()).WriteNotice("удаление диапазона [" + cTemplate.sFile + "]" + cRange.ToString());
								cTemplate._aRanges.Remove(cRange);
							}
						}
					}
					foreach (Template cTemplate in aTemplates.Where(o => Status.Stopped == o._eStatusTarget))
						cTemplate.StopAsync();
					foreach (Template cTemplate in aTemplates.Where(o => Status.Started == o._eStatusTarget))
						cTemplate.StartAsync();
					foreach (Template cTemplate in aTemplates.Where(o => Status.Prepared == o._eStatusTarget))
						cTemplate.PrepareAsync();
					GC.Collect();
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
