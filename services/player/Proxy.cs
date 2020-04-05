using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pl=helpers.replica.pl;
using helpers.replica.pl;
using iu = ingenie.userspace;
using ingenie.userspace;
using System.Threading;
using helpers.extensions;
using mam = helpers.replica.mam;
using helpers;
using System.Xml;
using System.IO;

namespace replica
{
    class Proxy
    {
        class Part : Video
        {
            public class Plugin : iu.Plugin
            {
                public string sOutput;
                public long nFrameStart;
                public long nDuration;
                public bool bOpacity;
				public ushort nTransition = 0;
            }

            private bool _bRendered = false;
            public long nFrameStart;
            public long nDuration;
			public ushort nTransition = 0;
			public bool bRendered
			{
				get
				{
					return _bRendered;
				}
			}
			public bool bStarted
            {
                get
                {
                    lock (_cSyncRoot)
                        return _bStarted;
                }
            }
            public Plugin cPlugin
            {
                get
                {
                    return _cPlugin;
                }
                set
                {
                    if (null == _cPlugin && null != value)
                        value.Stopped += Plugin_Stopped;
                    _cPlugin = value;
                }
            }
            public bool bDepended = false;
            private Plugin _cPlugin;
            private object _cSyncRoot = new object();
            private bool _bStarted = false;

            private void Plugin_Stopped(Atom cSender)
			{
				lock (_cSyncRoot)
				{
					try
					{
						string sPluginFile = null;
						if (Directory.Exists(cPlugin.sOutput))
							sPluginFile = (new DirectoryInfo(cPlugin.sOutput)).GetFileSystemInfos().Where(o => !o.Attributes.HasFlag(FileAttributes.Directory) && !o.Attributes.HasFlag(FileAttributes.Hidden)).Select(o => o.FullName).FirstOrDefault();
						else
							throw new Exception("folder made by plugin wasn't found: ["+cPlugin.sOutput + "]");
						if (sPluginFile != null)
						{
							if (bStarted)  // папку-то он уже наплодил, видимо, и файл, которые некому удалять...
							{
								Proxy.AddFileToListToDelete(sPluginFile);
								throw new Exception("plugin render lated");
							}
							else
							{
								sFile = sPluginFile;
								nFrameStart = cPlugin.nFrameStart;
								nDuration = cPlugin.nDuration;
								bOpacity = cPlugin.bOpacity;
								nTransition = cPlugin.nTransition;
								_bRendered = true;
								(new Logger()).WriteDebug("plugin stopped: [file=" + sFile + "][start=" + nFrameStart + "][dur=" + nDuration + "][trans=" + nTransition + "]");
							}
						}
						else
						{
							if (!Directory.EnumerateFileSystemEntries(cPlugin.sOutput).Any())
							{
								Directory.Delete(cPlugin.sOutput);
								(new Player.Logger()).WriteNotice("пустая директория удалена после ошибки плагина:" + cPlugin.sOutput);
							}
							throw new Exception("file made by plugin wasn't found");
						}
					}
					catch (Exception ex)
					{
						cPlugin = null;
						(new Player.Logger()).WriteError(ex);
					}
				}
				cSender.Dispose();
			}
			public void TimingsUpdate(PlaylistItem cPLI)
			{
				lock (_cSyncRoot)
				{
					if (bStarted)
						throw new Exception("part already started");
					try
					{
						if (!_bRendered)
						{
							long nPLIFrameStart = (long)(cPLI.nFrameCurrent > cPLI.nFrameStart ? cPLI.nFrameCurrent : cPLI.nFrameStart);
							long nFrameStartTarget = (0 < nPLIFrameStart && long.MaxValue > nPLIFrameStart ? nPLIFrameStart : 1);
							long nFrameStopTarget = (0 < cPLI.nFrameStop && long.MaxValue > cPLI.nFrameStop ? cPLI.nFrameStop : cPLI.nFramesQty);
							long nDurationTarget = nFrameStopTarget - nFrameStartTarget + 1;

							if (0 > nFrameStart)
								nFrameStart = nFrameStopTarget + nFrameStart;
							else
								nFrameStart = nFrameStartTarget + (long.MaxValue > nFrameStart ? nFrameStart - 1 : 0);
							if (0 > nDuration)
								nDuration = Math.Abs(nDurationTarget + nDuration);
							else
								nDuration = (long.MaxValue > nDuration ? nDuration : nDurationTarget - nFrameStart + 1);
						}
					}
					catch (Exception ex)
					{
						(new Player.Logger()).WriteError(ex);
					}
				}
			}
			public void Start(PlaylistItem cPLI)
            {
                lock (_cSyncRoot)
                {
                    if (_bStarted)
                        throw new Exception("part already started");
                    _bStarted = true;
                }
                try
                {
					if (0 < nFrameStart)
						nFrameStart--;

					base.nFrameStart = (ulong)nFrameStart;
					base.nDuration = (ulong)nDuration;
				}
                catch (Exception ex)
                {
                    (new Player.Logger()).WriteError(ex);
                }
            }
        }

        #region statics
        static private List<string> _aFilesForDelete;
        static private LinkedList<Proxy> _aStore;
        static private object _oAdjustmentSync;
        static private long _nAdjustment;

        static public Player.IInteract iInteract;
        static public Proxy cCurrent;
        static public ManualResetEvent mreNextPlaylistItemPrepare;

        static Proxy()
        {
            _aFilesForDelete = new List<string>();
            _aStore = new LinkedList<Proxy>();
            _oAdjustmentSync = new object();
            _nAdjustment = 0;
            mreNextPlaylistItemPrepare = new ManualResetEvent(true);
            ThreadPool.QueueUserWorkItem(Watcher);
        }
        static public void AdjustmentFramesAdd(uint nAdjustment)
        {
            if (0 < nAdjustment)
            {
                long nAdjustmentTotal;
                lock (_oAdjustmentSync)
                {
                    _nAdjustment += nAdjustment;
                    nAdjustmentTotal = _nAdjustment;
                }
                (new Player.Logger()).WriteNotice("adjustment: +" + nAdjustment + "; total:" + nAdjustmentTotal);
            }
        }
        static public void AdjustmentFramesRemove(uint nAdjustment)
        {
            if (0 < nAdjustment)
            {
                long nAdjustmentTotal;
                lock (_oAdjustmentSync)
                {
                    _nAdjustment -= nAdjustment;
                    nAdjustmentTotal = _nAdjustment;
                }
                (new Player.Logger()).WriteNotice("adjustment: -" + nAdjustment + "; total:" + nAdjustmentTotal);
            }
        }
		static public void AddFileToListToDelete(string sFileToDelete)
		{
			lock (_aFilesForDelete)
			{
				(new Player.Logger()).WriteDebug("файл добавлен на удаление-2: [file_to_del =" + sFileToDelete + "]"); //TODO LANG
				_aFilesForDelete.Add(sFileToDelete);
			}
		}
        static private void Watcher(object oState)
        {
			Proxy cProxy;
            while (true)
            {
                try
                {
					cProxy = Proxy.cCurrent;
                    if (null != cProxy && DateTime.MinValue < cProxy._dtPlannedStop && 5 < DateTime.Now.Subtract(cProxy._dtPlannedStop).TotalSeconds)
                    {
                        (new Player.Logger()).WriteNotice("::");
                        string sAddLog = "";
                        if (!cProxy._aParts.IsNullOrEmpty())
                        {
                            sAddLog += "[dtPlannedStop = " + cProxy._dtPlannedStop + "]";
                            sAddLog += "[eff.nDuration = " + cProxy._aParts[cProxy._nPartCurrent].nDuration + "][eff.eStatus = " + cProxy._aParts[cProxy._nPartCurrent].eStatus + "]";
                            if (null != cProxy._aParts[cProxy._nPartCurrent].cTemplate)
                                sAddLog += "[templ.sFile = " + cProxy._aParts[cProxy._nPartCurrent].cTemplate.sFile + "]";
                            else
                                sAddLog += "[template==null]";
                        }
                        else
                            sAddLog += "[effect==null]";
                        (new Player.Logger()).WriteError(new Exception("while (!_mreNextPlaylistItemPrepare.WaitOne(500)): unplanned effect overtime - more than 5 sec" + sAddLog));
                        //cProxy._cPlaylist.Skip(cProxy.aVideos[cProxy._nPartCurrent]);
                    }
                }
                catch (ThreadAbortException)
                { break; }
                catch (Exception ex)
                {
                    (new Player.Logger()).WriteError(ex);
                }
				Thread.Sleep(1000);
			}
        }
        #endregion

        public bool bStarted
        {
            get
            {
                return _aParts[0].bStarted;
            }
        }
        private DateTime _dtPlannedStop;
        public static iu.Playlist cPlaylist;

        private object _cSyncRoot;
        private PlaylistItem _cPLI;
		public ulong nDuration
		{
			get
			{
				return _cPLI == null ? 0 : _cPLI.nDuration;
			}
		}
        private Part[] _aParts;
        private byte _nPartCurrent;
        private string _sFile;
        private bool _bCached;
		private string _sCachePath;
		
		static private Queue<Part> _aqParts = new Queue<Part>();

        public Proxy(PlaylistItem cPLI)
        {
            _cSyncRoot = new object();
            _bCached = false;
			_sCachePath = (new System.IO.DirectoryInfo(Player.Preferences.sCacheFolder)).FullName.ToLower();
            _cPLI = cPLI;
            _dtPlannedStop = DateTime.MinValue;
            _nPartCurrent = 0;
            FileCache();
            try
            {
                if (System.IO.File.Exists(_sFile))
                {
                    ffmpeg.net.File.Input cFile = new ffmpeg.net.File.Input(_sFile);
					cPLI.nFramesQty = (int)cFile.nFramesQty;
					if (cPLI.nFrameStop > cPLI.nFramesQty)
						cPLI.nFrameStop = cPLI.nFramesQty;
					cFile.Dispose();
                }
                else
                    throw new Exception("файл не найден [" + _sFile + "]");
            }
            catch (Exception ex)
            {
                (new Player.Logger()).WriteError(ex);
            }
            lock (_aStore)
                _aStore.AddLast(this);
            iInteract.PlaylistItemQueue(cPLI);
            _aParts = new Part[] { new Part()
                    {
                        bOpacity = Player.Preferences.bOpacity,
                        sFile = _sFile,
                        nFrameStart = long.MaxValue,
                        nDuration = long.MaxValue
                    }
                };
			_aParts[0].TimingsUpdate(_cPLI);
			try
            {
                pl.Proxy cDBProxy = null;
                if (!_cPLI.aClasses.IsNullOrEmpty())
                    for (int nI = 0; nI < _cPLI.aClasses.Length; nI++)
                        if (null != cPLI.aClasses && null != (cDBProxy = iInteract.ProxyGet(cPLI.aClasses[nI])))
                            break;

                if (null != cDBProxy)
                {
					XmlDocument cXmlDocument = new XmlDocument();
					cXmlDocument.Load(cDBProxy.sFile);
					XmlNode cXmlNode = cXmlDocument.NodeGet("proxy");
					string sTarget = cXmlNode.AttributeValueGet("target", false);
					Proxy cTarget = (Proxy)RuntimeParse(sTarget);
					if (null == cTarget)
					{
						(new Logger()).WriteWarning("can't find specified proxy [" + sTarget + "]");  // не ошибка, т.к. может (и скорее всего) уже нашли нужный клип раньше
						return;
					}
					else
						(new Logger()).WriteDebug("previous proxy was found: " + cTarget._sFile);
					lock (cTarget._cSyncRoot)
					{
						if (0 > cTarget._cPLI.nFrameCurrent && !cTarget.bStarted && 2 > cTarget._aParts.Length)
							cTarget.Apply(cXmlNode);
						else if (2 > cTarget._aParts.Length)
							throw new Exception("template lated - proxy already started");
					}
                }
			}
			catch (Exception ex)
			{
				(new Player.Logger()).WriteError(ex);
			}
        }
        private void Apply(XmlNode cXmlNode)
        {
            List<Part> aParts = new List<Part>();
			foreach (XmlNode cXNPart in cXmlNode.NodesGet())
			{
				aParts.Add(PartCreate(cXNPart));
				_aParts = aParts.ToArray();
				(new Logger()).WriteDebug3("parts created: " + _aParts.Count());
			}
			(new Logger()).WriteDebug3("apply: end");
        }
        private Part PartCreate(XmlNode cXmlNode)
        {
            Part cRetVal = new Part();
            if("plugin" == cXmlNode.Name)
            {
                XmlNode cXNData = cXmlNode.NodeGet("data");
                cRetVal.cPlugin = new Part.Plugin()
                {
                    sFile = cXmlNode.AttributeValueGet("file"),
                    sClass = cXmlNode.AttributeValueGet("class"),
                    sData = ProcessMacros(cXNData.OuterXml),
                    sOutput = ProcessRuntimes(cXNData.AttributeValueGet("output")),
                    nFrameStart = cXmlNode.AttributeOrDefaultGet<long>("start", 0),
                    nDuration = cXmlNode.AttributeOrDefaultGet<long>("duration", long.MaxValue),
					nTransition = cXmlNode.AttributeOrDefaultGet<ushort>("transition", 0)
                };
                if (0 > cRetVal.cPlugin.nFrameStart || 1 > cRetVal.cPlugin.nDuration)
                    throw new Exception("wrong plugin framing");//TODO LANG


				ThreadPool.QueueUserWorkItem((object o) =>
				{
					try
					{
						cRetVal.cPlugin.Start();
					}
					catch (Exception ex)
					{
						(new Player.Logger()).WriteError(ex);
					}
				});
				//try
				//{
				//    cRetVal.cPlugin.Start();
				//}
				//catch (Exception ex)
				//{
				//    (new Player.Logger()).WriteError(ex);
				//}
                cRetVal.bDepended = (null == (cXmlNode = cXmlNode.NodeGet("fallback/video", false)));
            }
            if (!cRetVal.bDepended)
            {
                cRetVal.sFile = ProcessRuntimes(cXmlNode.AttributeValueGet("file"));
                cRetVal.bOpacity = cXmlNode.AttributeOrDefaultGet<bool>("opacity", false);

                cRetVal.nDuration = cXmlNode.AttributeOrDefaultGet<long>("duration", long.MaxValue);
				cRetVal.nTransition = cXmlNode.AttributeOrDefaultGet<ushort>("transition", 0);
                cRetVal.nFrameStart = cXmlNode.AttributeOrDefaultGet<long>("start", 0);
                long nFrameStop = cXmlNode.AttributeOrDefaultGet<long>("stop", long.MaxValue);
                if ((long.MaxValue > cRetVal.nFrameStart && long.MaxValue > nFrameStop && long.MaxValue > cRetVal.nDuration) || 1 > cRetVal.nDuration)
                    throw new Exception("wrong video framing");//TODO LANG
                if (long.MaxValue > nFrameStop)
                {
					if (long.MaxValue > cRetVal.nFrameStart)
					{
						cRetVal.nDuration = nFrameStop - cRetVal.nFrameStart + 1;
						if (0 > cRetVal.nFrameStart && 0 <= nFrameStop)
							cRetVal.nDuration *= -1;
					}
					else
						cRetVal.nFrameStart = nFrameStop - cRetVal.nDuration + 1;
                }
				cRetVal.TimingsUpdate(_cPLI);
            }
			(new Logger()).WriteDebug("[is_video=" + !cRetVal.bDepended + "][file=" + cRetVal.sFile + "][start=" + cRetVal.nFrameStart + "][dur=" + cRetVal.nDuration + "]");
            return cRetVal;
        }
		internal protected string ProcessMacros(string sText)
		{
			string sRetVal = ProcessRuntimes(sText);
#if DEBUG
			return sRetVal; //DNF
#endif
			System.Text.RegularExpressions.MatchCollection cMatches;
			string sValue = null;
			cMatches = System.Text.RegularExpressions.Regex.Matches(sRetVal, @"\{\%MACRO\:\:.*?%}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			if (0 < cMatches.Count)
			{
				foreach (System.Text.RegularExpressions.Match cMatch in cMatches)
				{
					try
					{
						sValue = null;
						sValue = MacroExecute(cMatch.Value);
						sValue = ProcessRuntimes(sValue);
						sRetVal = sRetVal.Replace(cMatch.Value, sValue.ForXML());
					}
					catch
					{
						(new Logger()).WriteNotice("proxy got error while processing macro [macro=" + cMatch.Value + "] [value=" + sValue + "]");
						throw;
					}
				}
			}
			return sRetVal;
		}
		private string MacroExecute(string sText)
		{
			mam.Macro.Flags eFlags = mam.Macro.ParseFlags(ref sText);
			mam.Macro cMacro = null;
			string sRetVal = "";
			if (null != iInteract)
			{
				lock (iInteract)
					iInteract = iInteract.Init();
				cMacro = iInteract.MacroGet(sText);
				switch (cMacro.cType.sName)
				{
					case "sql":
						cMacro.sValue = ProcessRuntimes(cMacro.sValue);
						sRetVal = iInteract.MacroExecute(cMacro);
						int nIndx = 0;
						while (null == sRetVal)  // просто эксперимент, т.к. дикие случаи, когда просто не может такого быть ))),  например SELECT * FROM cues."fCUSong"(73176, 1)  выполняется отл, а тут была NULL
						{
							if (nIndx++ > 20)
								break;
							(new Logger()).WriteNotice("MacroExecute: return = NULL!!! (" + nIndx + " times!) [text=" + sText + "][macro=" + (null == cMacro ? "NULL" : cMacro.sValue) + "]");
							Thread.Sleep(1);
							sRetVal = iInteract.MacroExecute(cMacro);
						}
						break;
					default:
						throw new Exception("обнаружен неизвестный тип макро-строки [" + cMacro.cType.sName + "]"); //TODO LANG
				}
			}
			if (null != sRetVal)
			{
				if (eFlags.HasFlag(mam.Macro.Flags.Escaped))
					sRetVal = sRetVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
				if (eFlags.HasFlag(mam.Macro.Flags.Caps))
					sRetVal = sRetVal.ToUpper();
			}
			return sRetVal;
		}
		private string ProcessRuntimes(string sText)
        {
            string sRetVal = sText;
            System.Text.RegularExpressions.MatchCollection cMatches;
            cMatches = System.Text.RegularExpressions.Regex.Matches(sRetVal, @"\{\%RUNTIME\:\:.*?%}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (0 < cMatches.Count)
            {
                string sValue;
                foreach (System.Text.RegularExpressions.Match cMatch in cMatches)
                {
                    sValue = null;
                    sValue = RuntimeParse(cMatch.Value).ToString();
                    sRetVal = sRetVal.Replace(cMatch.Value, sValue.ForXML());
                }
            }
            return sRetVal;
        }
        private object RuntimeParse(string sText)
        {
			mam.Macro.Flags eFlags = mam.Macro.ParseFlags(ref sText);
			string sRetVal = null;
			switch (sText)
            {
                case null:
                case "{%RUNTIME::PROXY::(0)%}":
                    return this;
                case "{%RUNTIME::PROXY::(-1)%}":
                    return _aStore.Find(this).Previous.Value;
                case "{%RUNTIME::PROXY::(-1)::TYPE::CLIP%}":
					PlaylistItem[] aPLIs = iInteract.PlaylistClipsGet();
					if (0 < aPLIs.Length)
					{
						LinkedListNode<Proxy> cLLN = _aStore.Last;
						while (null != (cLLN = cLLN.Previous))
							if (0 < aPLIs.Count(o => o.nID == cLLN.Value._cPLI.nID))
								return cLLN.Value;
					}
					return null;
				case "{%RUNTIME::PLI::ID%}":
					sRetVal = _cPLI.nID.ToString();
					break;
				case "{%RUNTIME::PLAYER::PROXY::FILE%}":
					sRetVal = _cPLI.cFile.sFile;   // _sFile; 
					break;
				case "{%RUNTIME::PLAYER::PROXY::FILE_CACHED%}":
					sRetVal = _sFile;
					break;
				case "{%RUNTIME::PLAYER::PROXY::FILE::FOLDERED%}":
					sRetVal = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_sFile), System.IO.Path.GetFileNameWithoutExtension(_sFile));
					break;
				case "{%RUNTIME::PLAYER::PROXY::VIDEO(0)::STOP%}":
					sRetVal = (_aParts[0].nFrameStart + _aParts[0].nDuration).ToString();
					break;
				default:
					throw new Exception("обнаружен запрос неизвестного runtime-свойства [" + sText + "]"); //TODO LANG
			}
			if (null != sRetVal)
			{
				if (eFlags.HasFlag(mam.Macro.Flags.Escaped))
					sRetVal = sRetVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
				if (eFlags.HasFlag(mam.Macro.Flags.Caps))
					sRetVal = sRetVal.ToUpper();
			}
			else
				(new Logger()).WriteNotice("OnRuntimeGet: return = NULL!!!");
			return sRetVal;
		}
        public void Start()
        {
			string sParts="proxy_start:";
			foreach (Part cP in _aParts)
				sParts += "\t[" + cP.sFile + "]";
			(new Player.Logger()).WriteDebug(sParts);

            Player.Logger cLogger = new Player.Logger();
            lock (_cSyncRoot)
            {
				if (2 > _aParts.Length)
				{
					long nFrameStart = (long)(_cPLI.nFrameCurrent > _cPLI.nFrameStart ? _cPLI.nFrameCurrent : _cPLI.nFrameStart);
					long nDuration = (0 < _cPLI.nFrameStop ? _cPLI.nFrameStop - nFrameStart : _cPLI.nFramesQty - nFrameStart) + 1;
					lock (_oAdjustmentSync)
					{
						if (0 > _nAdjustment)
						{
							if ((long)Playlist.Preferences.nDurationMinimumFr < (nDuration + _nAdjustment))
							{
								nDuration += _nAdjustment;
								_nAdjustment = 0;
							}
						}
						else if (0 < _nAdjustment)
						{
							_aParts[0].cShow.nDelay = (ulong)_nAdjustment;
							_nAdjustment = 0;
						}
					}
					_aParts[0].nDuration = nDuration;
				}
				
				foreach(Part cPart in _aParts)
					_aqParts.Enqueue(cPart);

				if (3 > cPlaylist.nEffectsQty)
					PartStart();
            }
        }
        static private void PartStart()
        {
			lock (_aStore)
			{
				Part cPart = _aqParts.Dequeue();
				(new Player.Logger()).WriteDebug("\t\tpart start [" + cPart.sFile + "][framestart=" + cPart.nFrameStart + "][dur=" + cPart.nDuration + "]");
				Proxy cProxy;

				if (null == (cProxy = _aStore.FirstOrDefault(o => null != o._aParts && o._aParts.Contains(cPart))))
					throw new Exception("указан незарегистрированный элемент плейлиста ingenie:" + cPart.sFile); //TODO LANG

				cPart.Start(cProxy._cPLI);
				if (!System.IO.File.Exists(cPart.sFile))
				{
					if (cProxy._sFile == cPart.sFile && cProxy._bCached)
						(new Player.Logger()).WriteError(new System.IO.FileNotFoundException("зарегистрированный файл в кэше не найден [" + cProxy._sFile + "]. будет использован исходный файл [" + (cPart.sFile = cProxy._sFile = cProxy._cPLI.cFile.sFile) + "]")); //TODO LANG
				}
				if (!System.IO.File.Exists(cPart.sFile))
				{
					iInteract.PlaylistItemFail(cProxy._cPLI);
					throw new System.IO.FileNotFoundException("файл не найден", cPart.sFile); //TODO LANG
				}
#if DEBUG
				//cPart.stArea = new Area(0, -180, 1920, 1440);  //DNF
#endif
				cPlaylist.EffectAdd(cPart, cPart.nTransition);    //  попробовать тут
			}
        }
        private void FileCache()
        {
            Player.Logger cLogger = new Player.Logger();
            _bCached = true;
            string sExtension = System.IO.Path.GetExtension(_cPLI.cFile.sFilename);
            string sFileCached = System.IO.Path.Combine(Player.Preferences.sCacheFolder, "_" + _cPLI.nID + sExtension);
            if (!System.IO.File.Exists(sFileCached))
            {
                _sFile = System.IO.Path.Combine(Player.Preferences.sCacheFolder, _cPLI.nID + sExtension);
                if (System.IO.File.Exists(_sFile))
                {
                    try
                    {
						cLogger.WriteDebug("попытка переименовать файл в кэше: [file = " + _sFile + "][cashed = " + sFileCached + "]" + sFileCached);
                        System.IO.File.Move(_sFile, sFileCached);
                        cLogger.WriteNotice("файл переименован в кэше:" + sFileCached);
                        _sFile = sFileCached;
                    }
                    catch (Exception ex)
                    {
                        cLogger.WriteError(ex);
                    }
                }
                else
				{
					if (Player.Preferences.aIgnoreFiles.Contains(System.IO.Path.GetFileName(_cPLI.cFile.sFile).ToLower()))
						cLogger.WriteNotice("файл не найден в кэше, т.к. он в игнор-листе - даём оригинал [original:" + (_sFile = _cPLI.cFile.sFile) + "]");//TODO LANG
					else
						cLogger.WriteError("файл не найден в кэше-2. Даём оригинал! [cached:" + _sFile + "][original:" + (_sFile = _cPLI.cFile.sFile) + "]");//TODO LANG
					_bCached = false;
                }
            }
            else
                cLogger.WriteWarning("переименованный файл в кэше уже существует:" + (_sFile = sFileCached));   // после перезапуска, например
        }
		static public void CheckCacheAndCopy(PlaylistItem cPLI)  // в FileCache нельзя - он под локом
		{
#if DEBUG
            return; //DNF
#endif
            if (Player.Preferences.aIgnoreFiles.Contains(System.IO.Path.GetFileName(cPLI.cFile.sFile).ToLower()))
			{
				(new Logger("CheckCacheAndCopy")).WriteDebug("файл найден в игнорлисте (см. preferences.xml) и не будет закеширован = " + cPLI.cFile.sFile);
				return;
			}
			Player.Logger cLogger = new Player.Logger();
			string sExtension = System.IO.Path.GetExtension(cPLI.cFile.sFilename);
			string sFileCached = System.IO.Path.Combine(Player.Preferences.sCacheFolder, "_" + cPLI.nID + sExtension);
			try
			{
				if (!System.IO.File.Exists(sFileCached))
				{
					string sFileInCache = System.IO.Path.Combine(Player.Preferences.sCacheFolder, cPLI.nID + sExtension);
					if (!System.IO.File.Exists(sFileInCache))
					{
						cLogger.WriteError("файл не найден в кэше - будем экстренно копировать! [cached:" + sFileInCache + "][original:" + cPLI.cFile.sFile + "]");//TODO LANG
						System.IO.File.Copy(cPLI.cFile.sFile, sFileCached);
					}
				}
			}
			catch (Exception ex)
			{
				cLogger.WriteError("CheckCacheAndCopy [" + cPLI.nID + "][" + cPLI.cFile.sFile + "]", ex);
			}
		}
		static public void Dispose()
		{
			FilesDelete();
            _aFilesForDelete = null;
        }
        static public void FilesDelete()
        {
            string sDir, sName, sFileNew, sLogInfo;
            bool bCached;
            bool bDoNotDelete = FailoverConstants.IsFilesDoNotRemoveMode(Player.Preferences.sCacheFolder, out sLogInfo);
            lock (_aFilesForDelete)
            {
                foreach (string sFile in _aFilesForDelete.ToArray())
                {
                    try
                    {
                        bCached = false;
                        sDir = Path.GetDirectoryName(sFile);
                        sName = Path.GetFileName(sFile);
                        if (PathsAreEqual(sDir, Player.Preferences.sCacheFolder) && sName.StartsWith("_"))
                            bCached = true;

                        if (bDoNotDelete && bCached)
                        {
                            (new Player.Logger()).WriteError("обнаружен флаг: 'не удалять файлы из кэша' " + sLogInfo);
                            //sFileNew = Path.Combine(Player.Preferences.sCacheFolder, sName.Substring(1));
                            //File.Move(sFile, sFileNew);
                            File.SetCreationTime(sFile, DateTime.Now);
                            File.SetLastWriteTime(sFile, DateTime.Now);
                            (new Player.Logger()).WriteWarning("файл оставлен в кэше: [cached=" + sFile + "]"); //[new=" + sFileNew + "]
                        }
                        else
                        {
                            System.IO.File.Delete(sFile);
                            (new Player.Logger()).WriteNotice("файл удален из кэша:" + sFile);
                        }

                        _aFilesForDelete.Remove(sFile);
						if (!PathsAreEqual(sDir, Player.Preferences.sCacheFolder) && !Directory.EnumerateFileSystemEntries(sDir).Any())  // удаляем временные папки всякие, кроме папки кэша, даже если она пустая
						{
							Directory.Delete(sDir);
							(new Player.Logger()).WriteNotice("пустая директория удалена:" + sDir);
						}
                    }
                    catch
                    {
                    }
                }
            }
        }
		static public bool PathsAreEqual(string sPath1, string sPath2)
		{
			if (0 == String.Compare(System.IO.Path.GetFullPath(sPath1).TrimEnd('\\'), System.IO.Path.GetFullPath(sPath2).TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase))
				return true;
			return false;
		}
        static public void Playlist_EffectAdded(ingenie.userspace.Container cSender, Effect cEffect)
        {
            try
            {
                Proxy cProxy = null;
                lock (_aStore)
					if (null == (cProxy = _aStore.FirstOrDefault(o => null != o._aParts && o._aParts.Contains(cEffect))))
                        throw new Exception("указан незарегистрированный элемент плейлиста ingenie:" + cEffect); //TODO LANG
                if (cEffect != cProxy._aParts[0])
                {
                    (new Player.Logger()).WriteNotice("\t\tподготовлена очередная часть proxy [" + ((Video)cEffect).sFile + "]"); //TODO LANG
                    return;
                }
                iInteract.PlaylistItemPrepare(cProxy._cPLI);
                (new Player.Logger()).WriteNotice("подготовлено к воспроизведению:" + cProxy._cPLI.ToString()); //TODO LANG
            }
            catch (Exception ex)
            {
                (new Player.Logger()).WriteError(ex);
            }
        }
        static public void Playlist_EffectStarted(ingenie.userspace.Container cSender, Effect cEffect)
        {
            try
            {
                Proxy cProxy = null;
                lock (_aStore)
					if (null == (cProxy = _aStore.FirstOrDefault(o => null != o._aParts && o._aParts.Contains(cEffect))))
                        throw new Exception("указан незарегистрированный элемент плейлиста ingenie:" + cEffect); //TODO LANG
				cProxy._dtPlannedStop = (cEffect.nDuration == ulong.MaxValue ? DateTime.MaxValue : DateTime.Now.AddMilliseconds(cEffect.nDuration * (ulong)Player.Preferences.nFrameMs));
                if (cEffect != cProxy._aParts[0])
                {
                    cProxy._nPartCurrent++;
					(new Player.Logger()).WriteNotice("\t\tзапущена очередная часть proxy [" + ((Video)cEffect).sFile + "] [effect_in: " + (cEffect is EffectVideo ? ((EffectVideo)cEffect).nFrameStart : ushort.MaxValue) + "] [effect_dur: " + (cEffect is EffectVideo ? ((EffectVideo)cEffect).nDuration : ushort.MaxValue) + "]"); //TODO LANG
                    return;
                }
                iInteract.PlaylistItemStart(cProxy._cPLI, cEffect.cShow.nDelay);
				string sText;
				if (cProxy._aParts.Count() > 1)
					sText = "в эфире первая часть proxy";
				else
					sText = "в эфире";
				(new Player.Logger()).WriteNotice(sText + (0 < cEffect.cShow.nDelay ? " [delay: " + cEffect.cShow.nDelay + "]" : "") + ": " + cProxy._cPLI.ToString() + "[effect_in: " + (cEffect is EffectVideo ? ((EffectVideo)cEffect).nFrameStart : ushort.MaxValue) + "] [effect_dur: " + (cEffect is EffectVideo ? ((EffectVideo)cEffect).nDuration : ushort.MaxValue) + "]"); //TODO LANG
				cCurrent = cProxy;
			}
			catch (Exception ex)
            {
                (new Player.Logger()).WriteError(ex);
            }
        }
        static public void Playlist_EffectStopped(ingenie.userspace.Container cSender, Effect cEffect)
        {
			(new Player.Logger()).WriteDebug("\t\teffect stopped proxy [" + ((Video)cEffect).sFile + "] [parts_qty=" + _aqParts.Count + "] [next_part=" + (1 > _aqParts.Count ? "NONE" : _aqParts.Peek().sFile) + "]"); 
            Player.Logger cLogger = new Player.Logger();
			bool bStartError = false;
            try
            {
				if (1 > _aqParts.Count)
					mreNextPlaylistItemPrepare.Set();
				else
					try
					{
						PartStart();
					}
					catch (Exception ex)
					{
						cLogger.WriteError(new Exception("возможно, proxy завершен раньше, чем должен был!", ex)); //TODO LANG
						bStartError = true;
					}

                Proxy cProxy = null;
                lock (_aStore)
                {
					if (null == (cProxy = _aStore.FirstOrDefault(o => null != o._aParts && o._aParts.Contains(cEffect))))
                        throw new Exception("указан незарегистрированный элемент плейлиста ingenie:" + cEffect); //TODO LANG
                }
				if (!bStartError && cEffect != cProxy._aParts.Last())
				{
					(new Player.Logger()).WriteNotice("\t\tостановлена очередная часть proxy [" + ((Video)cEffect).sFile + "]"); //TODO LANG
					return;
				}
				lock (_aStore)
					_aStore.Remove(cProxy);
                cProxy._dtPlannedStop = DateTime.MinValue;

				iInteract.PlaylistItemStop(cProxy._cPLI);
                cLogger.WriteNotice("остановлено: " + cProxy._cPLI.ToString()); //TODO LANG
                cProxy.FilesRelease();
            }
            catch (Exception ex)
            {
                cLogger.WriteError(new Exception("возможна потеря функционала воспроизведения", ex)); //TODO LANG
            }
        }
        static public void Playlist_EffectFailed(ingenie.userspace.Container cSender, Effect cEffect)
        {
            Player.Logger cLogger = new Player.Logger();
            try
            {
                Proxy cProxy = null;
                lock (_aStore)
					if (null == (cProxy = _aStore.FirstOrDefault(o => null != o._aParts && o._aParts.Contains(cEffect))))
                        throw new Exception("указан незарегистрированный элемент плейлиста ingenie:" + cEffect); //TODO LANG
                if (cEffect != cProxy._aParts.Last())
                {
                    (new Player.Logger()).WriteNotice("\t\tпроизошел сбой на очередной части proxy [" + ((Video)cEffect).sFile + "]"); //TODO LANG
                    return;
                }
				lock (_aStore)
					_aStore.Remove(cProxy);
				mreNextPlaylistItemPrepare.Set();
                iInteract.PlaylistItemFail(cProxy._cPLI);
                cLogger.WriteError("произошел сбой: " + cProxy._cPLI.ToString()); //TODO LANG
                cProxy.FilesRelease();
            }
            catch (Exception ex)
            {
                cLogger.WriteError(new Exception("возможна потеря функционала воспроизведения", ex)); //TODO LANG
            }
        }
        private void FilesRelease()
        {
            FilesDelete();
			string sFilePath;
            foreach (Video cVideo in _aParts)
            {
				sFilePath = (new System.IO.FileInfo(cVideo.sFile)).FullName.ToLower();

				(new Player.Logger()).WriteDebug("file. before release: [file =" + cVideo.sFile + "][_file = " + _sFile + "][cached = " + _bCached + "][rendered = " + ((Part)cVideo).bRendered + "][cache = " + _sCachePath + "]"); // может rendered поможет правильно удалять - помог
				if (  ((Part)cVideo).bRendered || (cVideo.sFile != _sFile || _bCached) && sFilePath.StartsWith(_sCachePath)  )
				{
					lock (_aFilesForDelete)
						if (!_aFilesForDelete.Contains(cVideo.sFile))
						{
							_aFilesForDelete.Add(cVideo.sFile);
							(new Player.Logger()).WriteDebug("файл добавлен на удаление: [file =" + cVideo.sFile + "][_file = " + _sFile + "][cached = " + _bCached + "]"); //TODO LANG
						}
				}
            }
        }
    }
}
