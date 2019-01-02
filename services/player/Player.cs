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

using helpers.replica.pl;
using helpers.extensions;
using ingenie.userspace;
using IU = ingenie.userspace;
using System.Xml;

namespace replica
{
    public class Player
    {
        public interface IInteract
        {
            IInteract Init();

            void PlaylistItemQueue(PlaylistItem cPLI);
            void PlaylistItemPrepare(PlaylistItem cPLI);
            void PlaylistItemStart(PlaylistItem cPLI, ulong nDelay);
            void PlaylistItemStop(PlaylistItem cPLI);
            void PlaylistItemFail(PlaylistItem cPLI);
            void PlaylistItemSkip(PlaylistItem cPLI);

			//helpers.replica.pl.Proxy ProxyGet(string sMacroName);
			helpers.replica.mam.Macro MacroGet(string sMacroName);
			helpers.replica.pl.Proxy ProxyGet(Class cClass);
			string MacroExecute(helpers.replica.mam.Macro cMacro);
			PlaylistItem[] PlaylistClipsGet();
		}
        public class Preferences : helpers.Preferences
        {
            static protected Preferences _cInstance = new Preferences();

            static public byte nFPS
            {
                get
                {
                    return _cInstance._nFPS;
                }
            }
            static public int nFrameMs
            {
                get
                {
                    return _cInstance._nFrameDur;
                }
            }
            static public string sCacheFolder
            {
                get
                {
                    return _cInstance._sCacheFolder;
                }
            }
            static public bool bOpacity
            {
                get
                {
                    return _cInstance._bOpacity;
                }
            }
			static public string[] aIgnoreFiles
			{
				get
				{
					return _cInstance._aIgnoreFiles;
				}
			}

			private byte _nFPS;
            private int _nFrameDur;
            private string _sCacheFolder;
            private bool _bOpacity;
			private string[] _aIgnoreFiles;

			public Preferences()
                : base("//replica/player")
            {
            }
            override protected void LoadXML(XmlNode cXmlNode)
            {
                if (null == cXmlNode)
                    return;

                _nFPS = cXmlNode.AttributeGet<byte>("fps");  //TODO get it from ig_server
                _nFrameDur = 1000 / _nFPS;
                _sCacheFolder = cXmlNode.AttributeValueGet("cache");
                if (!System.IO.Directory.Exists(_sCacheFolder))
					throw new Exception("указанная папка кэша плеера не существует [cache:" + _sCacheFolder + "][" + cXmlNode.Name + "]"); //TODO LANG
				(new Logger()).WriteNotice("prefs got cache folder: " + _sCacheFolder);

				string sIgnore = cXmlNode.AttributeValueGet("ignor_files", false);
                if (null != sIgnore)
                {
                    _aIgnoreFiles = sIgnore.Split(new char[] { ',', ';' }).Select(o => o.Trim()).ToArray();
                    sIgnore = "";
                    foreach (string sStr in _aIgnoreFiles)
                        sIgnore += "[" + sStr + "]\t";
                    (new Logger()).WriteNotice("prefs got ignor_files:" + sIgnore);
                }
                else
                    _aIgnoreFiles = new string[0];

                _bOpacity = cXmlNode.AttributeOrDefaultGet<bool>("opacity", false);
				(new Logger()).WriteNotice("prefs got opacity: " + _bOpacity);
			}
		}
        public class Logger : helpers.Logger
        {
            static public string sFile = null;

            public Logger()
                : base("player", sFile)
            { }
        }
        private enum Command
        {
            skip
        }

        static public void AdjustmentFramesAdd(uint nAdjustment)
        {
            Proxy.AdjustmentFramesAdd(nAdjustment);
        }
        static public void AdjustmentFramesRemove(uint nAdjustment)
        {
            Proxy.AdjustmentFramesRemove(nAdjustment);
        }

        private bool _bRunning;
        private bool _bStopped;
        private Queue<Proxy> _aqPlaylist;
        private List<string> _aFilesToDelete;
        private Queue<Command> _aqCommands;
		public bool _bAdding;

        public ushort nQueueLength
        {
            get
            {
                lock (_aqPlaylist)
                    return (ushort)_aqPlaylist.Count;   // можно еще из proxy.cplaylist  count брать, но пока не надо вроде
            }
        }
		public ulong nPlaylistDurationTotal
		{
			get
			{
				lock(_aqPlaylist)
				{
                    if (Proxy.cPlaylist == null)
                        return 0;
					ulong nRetVal = Proxy.cPlaylist.nSumDuration;

                    if (_aqPlaylist.IsNullOrEmpty())
                        return nRetVal;
                    foreach (Proxy cPr in _aqPlaylist)
					{
						nRetVal += cPr.nDuration;
					}
					return nRetVal;
				}
			}
		}
        public Player()
        {
			_bAdding = false;
        }

        public void Start(IInteract iInteract)
        {
            try
            {
                Proxy.iInteract = iInteract;
                _bRunning = true;
                _bStopped = false;
                _aqPlaylist = new Queue<Proxy>();
                _aFilesToDelete = new List<string>();
                _aqCommands = new Queue<Command>();

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
                Skip();
                DateTime dt = DateTime.Now;
                while (!_bStopped && DateTime.Now.Subtract(dt).TotalSeconds < 2)
                    Thread.Sleep(300);
                if (!_bStopped)
                    (new Logger()).WriteNotice("превышено ожидание завершения потока");
                Proxy.Dispose();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }

        private void Worker(object cStateInfo)
        {
            (new Logger()).WriteNotice("модуль воспроизведения запущен");//TODO LANG
            Proxy cProxy = null;
            bool bStarted = false;
			int nIndxGCForced = 0;
            try
            {
                IU.Playlist cPlaylist = new IU.Playlist();
                cPlaylist.bStopOnEmpty = false;
                cPlaylist.bOpacity = Preferences.bOpacity;
                cPlaylist.aChannels = null;
                cPlaylist.Prepared += new EventDelegate(InGeniePlaylist_Prepared);
                cPlaylist.Started += new EventDelegate(InGeniePlaylist_Started);
                cPlaylist.Stopped += new EventDelegate(InGeniePlaylist_Stopped);
                cPlaylist.EffectAdded += Proxy.Playlist_EffectAdded;
                cPlaylist.EffectStarted += Proxy.Playlist_EffectStarted;
                cPlaylist.EffectStopped += Proxy.Playlist_EffectStopped;
                cPlaylist.EffectFailed += Proxy.Playlist_EffectFailed;
                cPlaylist.nLayer = 1;

                DateTime dt;
				Proxy.cPlaylist = cPlaylist;
				Logger.Timings cTimings = new helpers.Logger.Timings("player:Worker");
				while (_bRunning)
                {
                    try
                    {
                        cProxy = null;
                        dt = DateTime.Now;
						ulong nPLDuration = 0;
                        while (true)
                        {
                            if (DateTime.Now.Subtract(dt).TotalMinutes > 2)
                            {
                                (new Logger()).WriteNotice("до сих пор торчим в ожидании _aqPlaylist:" + cPlaylist.nEffectsQty);
                                dt = DateTime.Now;
                            }
                            lock (_aqPlaylist)
                            {
								if (0 < _aqPlaylist.Count)
									cProxy = _aqPlaylist.Dequeue();
                            }
							if (null != cProxy)
							{
								if (2 < cPlaylist.nEffectsQty || 0 < cPlaylist.nEffectsQty && 1500 < (nPLDuration = cPlaylist.nSumDuration))
									(new Logger()).WriteNotice("sleeping until small qty in queue:" + cPlaylist.nEffectsQty + "   dur: " + nPLDuration);
								while (2 < cPlaylist.nEffectsQty || 0 < cPlaylist.nEffectsQty && 1500 < (nPLDuration = cPlaylist.nSumDuration))   // копия нижнего условия
									Thread.Sleep(500);
								(new Logger()).WriteNotice("sleeping end: queue:" + cPlaylist.nEffectsQty + "   dur: " + nPLDuration);
								break;
							}
                            Thread.Sleep(500);
                        }


						if (0 < cPlaylist.nEffectsQty && 1500 < cPlaylist.nSumDuration) // т.е. 1 запасной точно есть (aPL count),  1500 = 60sec
						{
							(new Logger()).WriteDebug("sleep 4000 to wait adding starts");
							Thread.Sleep(4000);
						}


						if (_bAdding)  // на >3 минутном PLI идёт
							(new Logger()).WriteDebug("there is adding! will wait the end!");  // обычно adding 2-3 секунды
						while (_bAdding)
						{
							Thread.Sleep(500);
							if (0 == cPlaylist.nEffectsQty) // + 1 в эфире идёт
								break;
						}
						
						cProxy.Start(); // эта строка добавляет в Proxy._aqParts часть, 
						// которую тут же выкидывают из этого Proxy параллельно при Player.Add(), заменяя на 3 части
						// и потом при старте будет ошибка "часть не найдена в _aStore" и этот клип НЕ ВЫЙДЕТ в эфир!
						// проблема будет решена, если всегда сначала обслуживается добавление (оно только на >3 минутном PLI идёт)
						// а потом эта строчка Start()
						// экспериментально просто добавил задержку, т.к. по идее несколько секунд вообще погоды в эфире не должны сделать
						// и хорошо бы еще наладить блокировку, чтобы они не могли действовать одновременно (а они реально могут)
						// лок поставил, но боюсь! ))  - ну да с локом всё повисло ))) лок убрал, но sleep на 2000 впритык обошел add(), 
						// поставил 3000, но 3000 иногда не обходит add - поставил 5000... чот похер иногда все-равно совпадают. вроде реже.
						// надо все-таки заблочить как-то
						// лан, так как они если пересекаются, то стартуют одновременно, то попробую просто _bAdding
						
						// т.к. проблема осталась, если начался клип после рекламы, а их до следующей рекламы 3 штуки
						// то на первом же клипе стартует Player.Add() т.к. это первый > 3 минут
						// и тут же стартует cProxy.Start() т.к. клипов стало < 3 в очереди
						// для этого такой выход:
						// добавил параметр в контейнере nSumDuration - это полный хр-ж того что лежит в ПЛ, включая то что в эфире
						// если там лежит более чем на минуту и кол-во элементов более 1, то ИМХО можно не париться
						// пробую так сделать....
						// ...получилось норм вроде!
						// одно осталось тут - мы добавляем часто не когда надо, а когда заканчивается что-то не важно что и вообще не смотрим на очередь, 
						// а т.к. на каминапах конец наступает 3 раза, то и добавляем мы 3 раза иногда на один элемент и можем опережать время
						// что приводит к пропуску каминапа из-за неуспевания подготовить его, хотя время еще есть и полно.
						// попробую это тоже попроверять...
						// ...вроде получилось - теперь одна проблема. Уже другая - добавление no_smoke иногда (пока 1 раз за сутки после старта) происходит тогда, 
						// когда последний клип уже прошел или вот-вот пройдет.
						// нет, более частая проблема другая! раз 10 в сутки запрос вида "SELECT * FROM cues."fCUSong"(1568214, 1)" выдаёт NULL 
						// т.к. такого быть не может, то и не очень ясно что делать!!!!


                        if (!bStarted)
                        {
                            cPlaylist.Start();
                            bStarted = true;
                        }
						nPLDuration = 0;
						if (2 < cPlaylist.nEffectsQty || 0 < cPlaylist.nEffectsQty && 1500 < cPlaylist.nSumDuration)
                        {

							
							//_nIndxGCForced++;
							//cTimings.TotalRenew();
							if (System.Runtime.GCSettings.LatencyMode != System.Runtime.GCLatencyMode.Interactive)
								System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Interactive;
							//GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
							//cTimings.Stop("GC > 10", "GC-" + "Optimized" + " " + System.Runtime.GCSettings.LatencyMode + " [eff_qty=" + cPlaylist.nEffectsQty + "][sum_dur=" + cPlaylist.nSumDuration + "]", 10);



							Proxy.mreNextPlaylistItemPrepare.Reset();
                            while (!Proxy.mreNextPlaylistItemPrepare.WaitOne(500))
                            {
                                lock (_aqCommands)
                                {
                                    while (0 < _aqCommands.Count)
                                    {
                                        switch (_aqCommands.Dequeue())
                                        {
                                            case Command.skip:
                                                cPlaylist.Skip(); //UNDONE
                                                break;
                                        }
                                    }
                                }
								if (2 >= cPlaylist.nEffectsQty && (0 >= cPlaylist.nEffectsQty || 1500 >= cPlaylist.nSumDuration))
								{
									(new Logger()).WriteNotice("its time to add effects to playlist 2: [eq=" + cPlaylist.nEffectsQty + "][sd=" + cPlaylist.nSumDuration + "]");
									break;
								}
                            }
                        }
                        else
							(new Logger()).WriteNotice("its time to add effects to playlist 1: [eq=" + cPlaylist.nEffectsQty + "][sd=" + cPlaylist.nSumDuration + "]");
                    }
                    catch (Exception ex)
                    {
                        //UNDONE возможно получить "мертвые" PLI в _ahInGenieBinds
                        (new Logger()).WriteError(ex);//TODO LANG
                    }
                }
                cPlaylist.Stop();
                cPlaylist.Dispose();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            (new Logger()).WriteNotice("модуль воспроизведения остановлен"); //TODO LANG
            _bStopped = true;
        }

        void InGeniePlaylist_Prepared(Atom cAtom)
        {
            (new Logger()).WriteNotice("плейлист подготовлен к воспроизведению"); //TODO LANG
        }
        void InGeniePlaylist_Started(Atom cAtom)
        {
            (new Logger()).WriteNotice("плейлист запущен"); //TODO LANG
        }
        void InGeniePlaylist_Stopped(Atom cAtom)
        {
            (new Logger()).WriteNotice("плейлист остановлен"); //TODO LANG
        }

		string[] aLPIs = new string[] {
			"kuzmin_vladimir__ei_krasotka___q1_l0_a1_r0_p0_c0_v.mov",
			"DiskotekaAvariya_LikeMe_new.mov",
			"timofeevnikolai_snovymgodom_remix_9.mov",
			"in2nation_augustvosmogo.mov",
			"kuzmin_vladimir__ei_krasotka___q1_l0_a1_r0_p0_c0_v.mov",
  		};
		int nIDX=0;
		bool bB = true;
		public void Add(PlaylistItem cPLI)
		{

#if DEBUG2
			if (bB && nIDX == 4)
			{
				cPLI.cClass = new Class(16, "comingup");  //DNF
				bB = false;
			}
			cPLI.bIsAdv = false;
			cPLI.cFile.cStorage = new helpers.replica.media.Storage() { nID = 1, bEnabled = true, sName = "Клипы", sPath = @"d:\storages\clips\", cType = new helpers.IdNamePair(1, "Файловая система") };
			cPLI.nFramesQty = 7675;
			cPLI.nFrameStop = 600;
			cPLI.nFrameStart = 1;
			cPLI.sName = "Наргиз : Ты - моя нежность";
			cPLI.nID = 1780862;
			if (aLPIs.Length == nIDX)
				nIDX = 0;
			cPLI.cFile.sFilename = aLPIs[nIDX++];
#endif

            Logger cLogger = new Logger();
            try
            {
                if (null == cPLI)
                    return;

				Proxy.CheckCacheAndCopy(cPLI);  // экстренное кеширование вне лока
                lock (_aqPlaylist)
                    _aqPlaylist.Enqueue(new Proxy(cPLI));  // заносим в "queued"

                cLogger.WriteNotice("добавлено в очередь: " + cPLI.ToString()); //TODO LANG
            }
            catch (Exception ex)
            {
                cLogger.WriteError(new Exception("возможна потеря функционала воспроизведения", ex)); //TODO LANG
            }
        }

        internal void Skip()
        {
            lock (_aqCommands)
                _aqCommands.Enqueue(Command.skip);
            Proxy.mreNextPlaylistItemPrepare.Set();
        }
    }
}
