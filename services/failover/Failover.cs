using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using helpers;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.pl;
using SIO = System.IO;

namespace replica.failover
{
	class Failover : Playlist.IInteract, Player.IInteract, Cues.IInteract, replica.cues.Template.IInteract
	{
		public enum ErrorTarget
		{
			dbi_plug,
			dbi_framesinitial,
			playlist,
			dbi_commands
		}
		static public Dictionary<ErrorTarget, DateTime> ahErrors;

		static private object _cSyncRoot;
		static private bool _bJoint;
		static private byte _nSyncTries;
		static private LinkedList<PlaylistItem> _aPlaylist
		{
			get
			{
				return (_bJoint ? _aPlaylistOnline : _aPlaylistOffline);
			}
		}
		static private LinkedList<PlaylistItem> _aPlaylistOffline;
		static private LinkedList<PlaylistItem> _aPlaylistOnline;
		static private Dictionary<long, long> _ahFrameStopsInitials;
		//static private Dictionary<int, int> _ahFrameStopsInitials;
		static private Dictionary<Class, helpers.replica.cues.TemplateBind[]> _ahClasses;
		static private helpers.replica.media.File _cPlug;
		static private string _sDefaultPlugClassName;
		#region statuses
		static private IdNamePair[] _aStatuses;
		static private IdNamePair[] _aStatusesLocked
		{
			get
			{
				return _aStatuses.Where(o => Preferences.aStatusesLocked.Contains(o.sName.To<Preferences.PersistentStatus>())).ToArray();
			}
		}
		static private IdNamePair[] _aStatusesStaled
		{
			get
			{
				return _aStatuses.Where(o => Preferences.aStatusesStaled.Contains(o.sName.To<Preferences.PersistentStatus>())).ToArray();
			}
		}
		static private IdNamePair _cStatusFailed
		{
			get
			{
				string sValue = Preferences.PersistentStatus.failed.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		static private IdNamePair _cStatusPlayed
		{
			get
			{
				string sValue = Preferences.PersistentStatus.played.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		static private IdNamePair _cStatusSkipped
		{
			get
			{
				string sValue = Preferences.PersistentStatus.skipped.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		static private IdNamePair _cStatusOnAir
		{
			get
			{
				string sValue = Preferences.PersistentStatus.onair.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		static private IdNamePair _cStatusPrepared
		{
			get
			{
				string sValue = Preferences.PersistentStatus.prepared.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		static private IdNamePair _cStatusQueued
		{
			get
			{
				string sValue = Preferences.PersistentStatus.queued.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		static private IdNamePair _cStatusPlanned
		{
			get
			{
				string sValue = Preferences.PersistentStatus.planned.ToString();
				return _aStatuses.First(o => sValue == o.sName);
			}
		}
		#endregion statuses
		static private Dictionary<long, helpers.replica.mam.Cues> _ahCues;
		static private sbyte _nAdjustmentDelay = 0;

		static Failover()
		{
			ahErrors = new Dictionary<ErrorTarget, DateTime>();
			foreach (ErrorTarget e in Enum.GetValues(typeof(ErrorTarget)))
				ahErrors.Add(e, DateTime.MinValue);
			_cSyncRoot = new object();
			_bJoint = true;
			_nSyncTries = Preferences.nSyncTries;
			_aPlaylistOffline = new LinkedList<PlaylistItem>();
			_aPlaylistOnline = new LinkedList<PlaylistItem>();
			_ahFrameStopsInitials = new Dictionary<long, long>();
			_ahClasses = null;
			_cPlug = Preferences.cDefaultPlug;
			_sDefaultPlugClassName = null;
			_aStatuses = null;
			_ahCues = new Dictionary<long, helpers.replica.mam.Cues>();
			Adjust(TimeSpan.Zero);
		}
		static void Adjust(TimeSpan ts)
		{
			if (0 < _nAdjustmentDelay--)
				return;
			double nAdjustment = ts.Add(Preferences.tsAdjustment).TotalSeconds * 25;
			_nAdjustmentDelay = 5;
			if(25 < nAdjustment)
				Player.AdjustmentFramesAdd((uint)nAdjustment);
			else if (-25 > nAdjustment)
				Player.AdjustmentFramesRemove((uint)(-1 * nAdjustment));
		}

		static public void Synchronize()
		{
			Logger.Sync cLogger = new Logger.Sync();
			cLogger.WriteNotice("start syncing...");
			DBInteract cDBI = new DBInteract();
			PlaylistItem cPLI;
			DateTime dtStart;
			lock(_cSyncRoot)
			{
				PlaylistClean();
				if (_bJoint)
				{
					try
					{
						try
						{
							_cPlug = cDBI.PlaylistPlugsGet().First();
							ahErrors[ErrorTarget.dbi_plug] = DateTime.MinValue;
						}
						catch (Exception ex)
						{
							if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_plug])
								cLogger.WriteError(ex);
							ahErrors[ErrorTarget.dbi_plug] = DateTime.Now;
						}
						if (_cPlug.sFile.IsNullOrEmpty() || !SIO.File.Exists(_cPlug.sFile))
							_cPlug = Preferences.cDefaultPlug;
						_sDefaultPlugClassName = Preferences.sDefaultPlugClassName;

						try
						{
							_ahFrameStopsInitials = cDBI.PlaylistItemsFramesStopInitialsGet();
							ahErrors[ErrorTarget.dbi_framesinitial] = DateTime.MinValue;
						}
						catch (Exception ex)
						{
							if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_framesinitial])
								cLogger.WriteError(ex);
							ahErrors[ErrorTarget.dbi_framesinitial] = DateTime.Now;
						}
					}
					catch (Exception ex)
					{
						cLogger.WriteError(new Exception("playlist synchronization failed", ex));
						return;
					}
					try
					{
						TransactionScope cTransaction = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromSeconds(200));
						Queue<PlaylistItem> aqPLIs = null;
						LinkedList<PlaylistItem> aPlaylistNew = new LinkedList<PlaylistItem>();
						Dictionary<Class, helpers.replica.cues.TemplateBind[]> ahClasses = new Dictionary<Class, helpers.replica.cues.TemplateBind[]>();
						Dictionary<long, Class> ahClassesBinds = new Dictionary<long, Class>();
						Dictionary<long, helpers.replica.mam.Cues> ahCues = new Dictionary<long, helpers.replica.mam.Cues>();
						List<IdNamePair> aStatuses = new List<IdNamePair>(cDBI.PlaylistItemsStatusesGet());
						IdNamePair cStatus;
						bool bUpdate = false;
						if(!_aStatuses.IsNullOrEmpty())
						{
							foreach (IdNamePair cINP in _aStatuses)
								if (null != (cStatus = aStatuses.FirstOrDefault(o => cINP.nID == o.nID)))
									aStatuses.Remove(cStatus);
							aStatuses.AddRange(_aStatuses);
						}
						_aStatuses = aStatuses.ToArray();
						ahCues = cDBI.ComingUpAssetsCuesGet();

						if (null == (aqPLIs = cDBI.ComingUpGet(TimeSpan.MaxValue)) || 1 > aqPLIs.Count)
							throw new Exception("receive empty playlist from DB");
						if (0 < _aPlaylistOnline.Count)
						{
							bUpdate = true;
							PlaylistItem cPLIExisted;
							while (true)
							{
								cPLI = aqPLIs.Peek();
								if (null == (cPLIExisted = _aPlaylistOnline.FirstOrDefault(o => cPLI.nID == o.nID)))
									break;
								cLogger.WriteNotice("skipping on existence:<br>[" + cPLI.ToString() + "]<br>[" + cPLIExisted.ToString() + "]");
								aqPLIs.Dequeue();
							}
							foreach (PlaylistItem cPLIQueued in _aPlaylistOnline)
							{
								if (!ahClassesBinds.ContainsKey(cPLIQueued.cClass.nID))
								{
									ahClassesBinds.Add(cPLIQueued.cClass.nID, cPLIQueued.cClass);
									ahClasses.Add(cPLIQueued.cClass, cDBI.TemplateBindsGet(cPLIQueued));
								}
								else
									cPLIQueued.cClass = ahClassesBinds[cPLIQueued.cClass.nID];
								if (null != cPLIQueued.cAsset && _ahCues.ContainsKey(cPLIQueued.cAsset.nID) && !ahCues.ContainsKey(cPLIQueued.cAsset.nID))
									ahCues.Add(cPLIQueued.cAsset.nID, _ahCues[cPLIQueued.cAsset.nID]);
								aPlaylistNew.AddLast(cPLIQueued);
								cLogger.WriteNotice("update offline playlist: " + cPLIQueued.ToString());
							}
						}
						string[] aStatusesLockedNames = _aStatusesLocked.Select(o => o.sName).ToArray();
						string sPLI;
						while (0 < aqPLIs.Count)
						{
							cPLI = aqPLIs.Dequeue();
							if (DateTime.MaxValue > cPLI.dtStopReal || 0 < aPlaylistNew.Count(o => cPLI.nID == o.nID))
							{
								cLogger.WriteNotice("something goes wrong [first]");
								continue;
							}

							sPLI = "[" + cPLI.ToString() + "]";
							if (DateTime.MaxValue > cPLI.dtStartReal)
							{
								cPLI.dtStartPlanned = cPLI.dtStartReal;
								cPLI.dtStartQueued = cPLI.dtStartReal;
							}
							else if (DateTime.MaxValue > cPLI.dtStartQueued)
								cPLI.dtStartPlanned = cPLI.dtStartQueued;

                            if (aStatusesLockedNames.Contains(cPLI.cStatus.sName))
							{
								if (bUpdate && 0 < _aPlaylistOnline.Count(o => cPLI.nID == o.nID))
									cLogger.WriteNotice("something goes wrong [second]");
								cLogger.WriteNotice("added to online playlist: " + sPLI);
								_aPlaylistOnline.AddLast(cPLI);
							}
							//else
								//cLogger.WriteNotice("added to offline playlist: " + sPLI);
							cPLI.cStatus = _cStatusPlanned;
							aPlaylistNew.AddLast(cPLI);
							if (!ahClassesBinds.ContainsKey(cPLI.cClass.nID))
							{
								ahClassesBinds.Add(cPLI.cClass.nID, cPLI.cClass);
								ahClasses.Add(cPLI.cClass, cDBI.TemplateBindsGet(cPLI));
							}
							cPLI.cClass = ahClassesBinds[cPLI.cClass.nID];
						}

						_ahClasses = ahClasses;
						_ahCues = ahCues;

						_aPlaylistOffline = aPlaylistNew;

						cTransaction.Complete();
						_bJoint = true;
						_nSyncTries = Preferences.nSyncTries;
						ahErrors[ErrorTarget.playlist] = DateTime.MinValue;
					}
					catch (Exception ex)
					{
						if (DateTime.MinValue == ahErrors[ErrorTarget.playlist])
							cLogger.WriteError(ex);
						ahErrors[ErrorTarget.playlist] = DateTime.Now;
						if (1 > _nSyncTries--)
						{
							cLogger.WriteWarning("going to divorce [st:" + _nSyncTries + "]");
							_bJoint = false;
						}
					}
				}
				ulong nFrames = 0;
				if (!_aPlaylistOffline.IsNullOrEmpty() && Preferences.tsSyncMargin <= TimeSpan.FromMilliseconds((nFrames = (ulong)_aPlaylistOffline.Where(o => _cStatusPlanned == o.cStatus).Sum(o => (decimal)o.nDuration)) * 40))
					return;
				cLogger.WriteError("received playlist is not long enough. collecting local media files [frames:" + nFrames + "][plis:" + _aPlaylistOffline.Count + "]");
				_bJoint = false;

				_ahClasses = new Dictionary<Class,helpers.replica.cues.TemplateBind[]>();
				_ahClasses.Add(Preferences.cDefaultClass, Preferences.aDefaultTemplateBinds);
				_sDefaultPlugClassName = Preferences.cDefaultClass.sName;
				_aStatuses = Preferences.aStatuses;

				SIO.DirectoryInfo cStorageContent = null;
				SIO.FileInfo[] aFileInfos = null;
				cStorageContent = new SIO.DirectoryInfo(Preferences.sStorageClips);

				if (!cStorageContent.Exists)
				{
					cLogger.WriteError("не найден указанный путь [" + Preferences.sStorageClips + "]");//TODO LANG
					return;
				}

				Random cRandom = new Random();
				aFileInfos = cStorageContent.GetFiles().OrderBy(o => cRandom.Next()).ToArray();
				cPLI = null;
				if(1 > _aPlaylistOffline.Count || DateTime.Now > (dtStart = _aPlaylistOffline.Max(o => o.dtStopPlanned)))
					dtStart = DateTime.Now;
				cLogger = new Logger.Sync();
				for (int nIndx = 0; aFileInfos.Length > nIndx; nIndx++)
				{
					if ((aFileInfos[nIndx].Attributes & SIO.FileAttributes.Directory) > 0)
						continue;
					try
					{
						cPLI = PlaylistItemCreate(aFileInfos[nIndx].FullName, dtStart);
						dtStart = dtStart.AddMilliseconds(cPLI.nDuration * 40);
						_aPlaylistOffline.AddLast(cPLI);
						cLogger.WriteNotice("added to playlist [" + aFileInfos[nIndx].FullName + "][" + cPLI.dtStartPlanned.ToStr() + "]");
					}
					catch (Exception ex)
					{
						cLogger.WriteError(new Exception("ошибка создания элемента плейлиста [" + aFileInfos[nIndx].FullName + "]", ex));
					}
				}
			}
		}

		static public PlaylistItem PlaylistItemCurrentGet()
		{
			PlaylistItem cRetVal = null;
			try
			{
				Synchronize();
				DateTime dtNow = DateTime.Now;
				lock(_cSyncRoot)
					cRetVal = _aPlaylist.FirstOrDefault(o => o.dtStart < dtNow && o.dtStop > dtNow);
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
			return cRetVal;
		}
		static public PlaylistItem PlaylistItemLockedPreviousGet()
		{
			PlaylistItem cRetVal = null;
			try
			{
				lock (_cSyncRoot)
					cRetVal = _aPlaylist.Where(o => _aStatusesLocked.Contains(o.cStatus)).OrderByDescending(o => o.dtStartReal).ThenByDescending(o => o.dtStartQueued).FirstOrDefault();
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}

			return cRetVal;
		}

		static private PlaylistItem[] PlaylistItemsPlannedGet()
		{
			PlaylistItem[] aRetVal = null;
			lock (_cSyncRoot)
				aRetVal = _aPlaylist.Where(o => _cStatusPlanned == o.cStatus).OrderBy(o => o.dtStartPlanned).ToArray();
			return aRetVal;
		}

		static public PlaylistItem PlaylistItemOnAirGet()
		{
			lock (_cSyncRoot)
				return _aPlaylist.Where(o => _cStatusOnAir == o.cStatus).OrderByDescending(o => o.dtStartReal).FirstOrDefault();
		}
		static public ulong PlaylistItemOnAirFramesLeftGet()
		{
			ulong nRetVal = 0;
			try
			{
				PlaylistItem cPLI = null;
				lock (_cSyncRoot)
					if (null != (cPLI = _aPlaylist.Where(o => _cStatusOnAir == o.cStatus).OrderByDescending(o => o.dtStartReal).FirstOrDefault()))
						nRetVal = (ulong)(cPLI.nFrameStop - cPLI.nFrameStart);
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
			return nRetVal;
		}

		static private PlaylistItem PlaylistItemCreate(string sFile, DateTime dtStart)
		{
			string sFolder = System.IO.Path.GetDirectoryName(sFile) + "/";
			string sDefault = "default";
			helpers.replica.media.File cFile = new helpers.replica.media.File(sFile.GetHashCode(), SIO.Path.GetFileName(sFile), new helpers.replica.media.Storage(sFolder.GetHashCode(), sDefault, sFolder, true, sDefault.GetHashCode(), sDefault), DateTime.MaxValue, helpers.replica.Error.no);

			ffmpeg.net.File.Input cFileInput = new ffmpeg.net.File.Input(sFile);
            long nDuration = (long)cFileInput.nFramesQty;
            cFileInput.Dispose();

			return PlaylistItemCreate(cFile, Preferences.cDefaultClass, dtStart, nDuration, DateTime.MinValue);
		}
		static private PlaylistItem PlaylistItemCreate(helpers.replica.media.File cFile, Class cClass, DateTime dtStart, long nDuration, DateTime dtTimingsUpdate)
		{
			PlaylistItem cRetVal = new PlaylistItem();
			cRetVal.dtTimingsUpdate = dtTimingsUpdate;
			cRetVal.dtStartPlanned = dtStart;
			cRetVal.nFrameStart = 1;
			cRetVal.nFrameStop = (int)(cRetVal.nFrameStart + nDuration);
			cRetVal.nFramesQty = (int)nDuration;

			lock (_cSyncRoot)
				if (1 > _aPlaylistOffline.Count || -2 < (cRetVal.nID = _aPlaylistOffline.Min(o => o.nID) - 1)) //стремная фигня конечно, но на ум ниче не идет более удобоваримое... т.к. логичный Max здесь не прокатит - если плейлист будет обновлен, а в локах окажется заглушка, мы рискуем поиметь два PLI с одинаковым ID
					cRetVal.nID = -2;
			cRetVal.cFile = cFile;
			cRetVal.cClass = cClass;
			cRetVal.cStatus = _cStatusPlanned;
			return cRetVal;
		}

		static public bool PlaylistItemsTimingsUpdate()
		{
			lock (playlist.Calculator.cSyncRoot)
			{
				if (PlaylistItemsTimingsUpdate(TimeSpan.MaxValue))
				{
					playlist.Calculator.dtLast = DateTime.Now;
					return true;
				}
				return false;
			}
		}
		static public bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope)
		{
			return PlaylistItemsTimingsUpdate(tsUpdateScope, -1);
		}
		static public bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID)
		{
			lock (playlist.Calculator.cSyncRoot)
			{
				if (-1 == nStartPlitemsID && TimeSpan.MaxValue > tsUpdateScope && TimeSpan.FromSeconds(60) > DateTime.Now.Subtract(playlist.Calculator.dtLast))
					return true;
				Synchronize();
				if (_bJoint)
				{
					playlist.Calculator.dtLast = DateTime.Now;
					return true;
				}

				bool bRetVal = false;
				TransactionScope cTransaction = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromSeconds(60));
				try
				{
					playlist.Calculator cCalculator = new playlist.Calculator();

					//cCalculator.ItemsInsertedFill = ItemsInsertedFill; //на фэйловере нам это не должно понадобиться, т.к. мы не редактируем его плейлист...
					cCalculator.ItemStartPlannedSet = ItemStartPlannedSet;
					cCalculator.ItemTimingsUpdateSet = ItemTimingsUpdateSet;
					cCalculator.ItemFrameStopChange = ItemFrameStopChange;
					cCalculator.ItemFrameStopRestore = ItemFrameStopRestore;
					cCalculator.ItemRemove = ItemRemove;
					cCalculator.PlugAdd = PlugAdd;
					cCalculator.PlugUpdate = PlugUpdate;
					
					cCalculator.ahFrameStopsInitials = _ahFrameStopsInitials;

					lock (_cSyncRoot)
						cCalculator.aPLIs = _aPlaylistOffline.Where(o => !_aStatusesStaled.Contains(o.cStatus)).ToList();
					cCalculator.tsUpdateScope = tsUpdateScope;
					cCalculator.nStartPlitemsID = nStartPlitemsID;
					bRetVal = cCalculator.Calculate();
					cTransaction.Complete();
					return bRetVal;
				}
				catch (Exception ex)
				{
					(new playlist.Calculator.Logger()).WriteError(ex);
				}
				return false;
			}
		}

		#region calculator callbacks
		static private void ItemStartPlannedSet(long nID, DateTime dtStartPlanned)
		{
			lock (_cSyncRoot)
				_aPlaylistOffline.FirstOrDefault(o => nID == o.nID).dtStartPlanned = dtStartPlanned;
		}
		static private void ItemTimingsUpdateSet(long nID, DateTime dtTimingsUpdate)
		{
			lock (_cSyncRoot)
				_aPlaylistOffline.FirstOrDefault(o => nID == o.nID).dtTimingsUpdate = dtTimingsUpdate;
		}
		static private void ItemFrameStopChange(long nID, long nFrameStopInitial, long nFrameStopNew)
		{
			lock (_cSyncRoot)
			{
				_aPlaylistOffline.FirstOrDefault(o => nID == o.nID).nFrameStop = (int)nFrameStopNew;
				if(!_ahFrameStopsInitials.ContainsKey(nID))
					_ahFrameStopsInitials.Add(nID, (int)nFrameStopInitial);
				else if(nFrameStopInitial != _ahFrameStopsInitials[nID])
					(new playlist.Calculator.Logger()).WriteWarning("новый исходный конечный кадр отличается от сохраненного ранее [id:" + nID + "][ifs.old:" + _ahFrameStopsInitials[nID] + "][ifs.new:" + nFrameStopInitial + "][fs:" + nFrameStopNew + "]");
			}
		}
		static private void ItemFrameStopRestore(long nID)
		{
			lock (_cSyncRoot)
			{
				if (_ahFrameStopsInitials.ContainsKey(nID))
				{
					_aPlaylistOffline.FirstOrDefault(o => nID == o.nID).nFrameStop = (int)_ahFrameStopsInitials[nID];
					_ahFrameStopsInitials.Remove(nID);
				}
			}
		}
		static private void ItemRemove(long nID)
		{
			lock (_cSyncRoot)
				_aPlaylistOffline.Remove(_aPlaylistOffline.FirstOrDefault(o => nID == o.nID));
		}
		static private void PlugAdd(DateTime dtStart, long nDuration, DateTime dtTimingsUpdate)
		{
			PlaylistItem cPLI = PlaylistItemCreate(_cPlug, _ahClasses.Keys.First(o => _sDefaultPlugClassName == o.sName), dtStart, nDuration, dtTimingsUpdate);
			cPLI.bPlug = true;
			lock (_cSyncRoot)
				_aPlaylistOffline.AddLast(cPLI);
		}
		static private void PlugUpdate(long nID, DateTime dtStart, long nDuration, DateTime dtTimingsUpdate)
		{
			lock (_cSyncRoot)
			{
				PlaylistItem cPLI = _aPlaylistOffline.First(o => nID == o.nID);
				cPLI.dtTimingsUpdate = dtTimingsUpdate;
				cPLI.dtStartPlanned = dtStart;
				cPLI.nFrameStop = (int)(cPLI.nFrameStart + nDuration);
				cPLI.nFramesQty = (int)nDuration;
			}
		}
		#endregion calculator callbacks

		static public void PlaylistItemFail(PlaylistItem cPLI)
		{
			try
			{
				lock (_cSyncRoot)
					cPLI.cStatus = _cStatusFailed;
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemStop(PlaylistItem cPLI)
		{
			try
			{
				DateTime dtNow = DateTime.Now;
				lock (_cSyncRoot)
				{
					if (_bJoint)
					{
						PlaylistItem cPLIMain = null;
						try
						{
							cPLIMain = (new DBInteract()).PlaylistItemGet(cPLI.nID);
						}
						catch (Exception ex)
						{
							(new Logger("adjustment")).WriteError(ex);
						}
						if (null != cPLIMain)
						{
							if (DateTime.MaxValue > cPLIMain.dtStopReal)
							{
								(new Logger("adjustment")).WriteNotice("before adjustment [main:" + cPLIMain.dtStopReal.ToString("HH:mm:ss.ffff") + "][local:" + dtNow.ToString("HH:mm:ss.ffff") + "]");
								Adjust(cPLIMain.dtStopReal.Subtract(dtNow));
							}
							else if (DateTime.MaxValue > cPLI.dtStartReal && DateTime.MaxValue > cPLIMain.dtStartReal)
							{
								(new Logger("adjustment")).WriteNotice("before adjustment [main:" + cPLIMain.dtStartReal.ToString("HH:mm:ss.ffff") + "][local:" + cPLI.dtStartReal.ToString("HH:mm:ss.ffff") + "]");
								Adjust(cPLIMain.dtStartReal.Subtract(cPLI.dtStartReal));
							}
							else
							{
								int nCount = 0;
								lock (_cSyncRoot)
									nCount = _aPlaylist.Count(o => !_aStatusesStaled.Contains(o.cStatus));
								if (3 > nCount)
								{
									(new Logger.Sync()).WriteWarning("going to divorce [st:" + _nSyncTries + "][ct:" + nCount + "]");
									_bJoint = false;
								}
							}
						}
					}
					cPLI.cStatus = _cStatusPlayed;
					cPLI.dtStopReal = dtNow;
				}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemSkip(PlaylistItem cPLI)
		{
			try
			{
				lock (_cSyncRoot)
				{
					cPLI.cStatus = _cStatusSkipped;
					cPLI.dtStopReal = DateTime.Now;
				}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemStart(PlaylistItem cPLI, ulong nDelay)
		{
			try
			{
				lock (_cSyncRoot)
				{
					cPLI.cStatus = _cStatusOnAir;
					cPLI.dtStartReal = DateTime.Now.AddMilliseconds(nDelay * 40);
				}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemPrepare(PlaylistItem cPLI)
		{
			try
			{
				lock (_cSyncRoot)
					cPLI.cStatus = _cStatusPrepared;
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemQueue(PlaylistItem cPLI)
		{
			try
			{
				lock (_cSyncRoot)
				{
					cPLI.cStatus = _cStatusQueued;
					cPLI.dtStartQueued = cPLI.dtStartPlanned;
				}
				//_aPlaylist.First(o => cPLI.nID == o.nID).dtStartQueued = cPLI.dtStartPlanned;
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistClean()
		{
			try
			{
				PlaylistItem[] aPLIs;
				DateTime dt = DateTime.Now.Subtract(TimeSpan.FromMinutes(20));
				lock (_cSyncRoot)
				{
					if (!(aPLIs = _aPlaylistOffline.Where(o => dt > o.dtStopPlanned).ToArray()).IsNullOrEmpty())
						foreach (PlaylistItem cPLI in aPLIs)
							_aPlaylistOffline.Remove(cPLI);
					if (!(aPLIs = _aPlaylistOnline.Where(o => dt > o.dtStopPlanned).ToArray()).IsNullOrEmpty())
						foreach (PlaylistItem cPLI in aPLIs)
							_aPlaylistOnline.Remove(cPLI);
				}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}

		static public helpers.replica.mam.Cues? ComingUpCuesGet(long nID, ushort nOffset)
		{
			helpers.replica.mam.Cues? cRetVal = null;
			lock (_cSyncRoot)
			{
				if (null != _ahCues)
				{
					PlaylistItem cPLI = _aPlaylistOffline.OrderBy(o => o.dtStartReal).ThenBy(o => o.dtStartQueued).ThenBy(o => o.dtStartPlanned).SkipWhile(o => nID != o.nID).Where(o => null != o.cAsset && _ahCues.ContainsKey(o.cAsset.nID) && !_aStatusesStaled.Contains(o.cStatus)).Skip(nOffset).FirstOrDefault();
					if (null != cPLI)
						cRetVal = _ahCues[cPLI.cAsset.nID];
				}
			}
			return cRetVal;
		}
		static public string ComingUpFileGet(long nID, ushort nOffset)
		{
			string sRetVal = null;
			lock (_cSyncRoot)
			{
				if (null != _ahCues)
				{
					PlaylistItem cPLI = _aPlaylistOffline.OrderBy(o => o.dtStartReal).ThenBy(o => o.dtStartQueued).ThenBy(o => o.dtStartPlanned).SkipWhile(o => nID != o.nID).Where(o => null != o.cAsset && _ahCues.ContainsKey(o.cAsset.nID) && !_aStatusesStaled.Contains(o.cStatus)).Skip(nOffset).FirstOrDefault();
					if (null != cPLI)
						sRetVal = cPLI.cFile.sFile;
				}
			}
			return sRetVal;
		}
		static public Queue<PlaylistItem> PlaylistItemsPreparedGet()
		{
			lock (_cSyncRoot)
				return new Queue<PlaylistItem>(_aPlaylistOffline.Where(o => _cStatusPrepared == o.cStatus).OrderBy(o => o.dtStart));
		}
		static public helpers.replica.cues.TemplateBind[] TemplateBindsGet(PlaylistItem cPLI)
		{
			lock (_cSyncRoot)
				return (_ahClasses.ContainsKey(cPLI.cClass) ? _ahClasses[cPLI.cClass] : null);
		}
		static public helpers.replica.mam.Macro MacroGet(string sMacroName)
		{
			return new helpers.replica.mam.Macro(sMacroName.GetHashCode(), new IdNamePair(0, "sql"), sMacroName, "{%RUNTIME::PLI::ID%}");
		}
		static public string MacroExecute(helpers.replica.mam.Macro cMacro)
		{
			string sRetVal ="";
			helpers.replica.mam.Cues? cCues = null;
			string sFile = null;
			switch (cMacro.sName)
			{
				case "{%MACRO::REPLICA::CU(0)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 0)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::PLI::CUES::ARTIST%}":
				case "{%MACRO::REPLICA::CU(0)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 0)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::PLI::CUES::SONG%}":
				case "{%MACRO::REPLICA::CU(0)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 0)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+1)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 1)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+1)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 1)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+1)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 1)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+2)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 2)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+2)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 2)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+2)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 2)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+3)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 3)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+3)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 3)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+3)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 3)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+4)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 4)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+4)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 4)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+4)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 4)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+5)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 5)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+5)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 5)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+5)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 5)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;
				default:
					(new Logger("template")).WriteNotice("указан неизвестный макрос [" + cMacro.sName + "]");
					break;
			}
			return sRetVal;
		}

		#region реализация Playlist.IInteract
		Playlist.IInteract Playlist.IInteract.Init()
		{
			return new Failover();
		}

		void Playlist.IInteract.PlaylistReset()
		{
			Synchronize();
		}

		PlaylistItem Playlist.IInteract.PlaylistItemCurrentGet() { return PlaylistItemCurrentGet(); }
		PlaylistItem Playlist.IInteract.PlaylistItemLockedPreviousGet() { return PlaylistItemLockedPreviousGet(); }
		PlaylistItem[] Playlist.IInteract.PlaylistItemsPlannedGet() { return PlaylistItemsPlannedGet(); }
		ulong Playlist.IInteract.PlaylistItemOnAirFramesLeftGet() { return PlaylistItemOnAirFramesLeftGet(); }

		bool Playlist.IInteract.PlaylistItemsTimingsUpdate() { return PlaylistItemsTimingsUpdate(); }
		bool Playlist.IInteract.PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope) { return PlaylistItemsTimingsUpdate(tsUpdateScope); }
		bool Playlist.IInteract.PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID) { return PlaylistItemsTimingsUpdate(tsUpdateScope, nStartPlitemsID); }

		void Playlist.IInteract.PlaylistItemFail(PlaylistItem cPLI) { PlaylistItemFail(cPLI); }
		#endregion реализация Playlist.IInteract
		#region реализация Player.IInteract
		Player.IInteract Player.IInteract.Init()
		{
			return new Failover();
		}
		
		void Player.IInteract.PlaylistItemQueue(PlaylistItem cPLI) { PlaylistItemQueue(cPLI); }
		void Player.IInteract.PlaylistItemPrepare(PlaylistItem cPLI) { PlaylistItemPrepare(cPLI); }
		void Player.IInteract.PlaylistItemStart(PlaylistItem cPLI, ulong nDelay) { PlaylistItemStart(cPLI, nDelay); }
		void Player.IInteract.PlaylistItemStop(PlaylistItem cPLI) { PlaylistItemStop(cPLI); }
		void Player.IInteract.PlaylistItemFail(PlaylistItem cPLI) { PlaylistItemFail(cPLI); }
		void Player.IInteract.PlaylistItemSkip(PlaylistItem cPLI) { PlaylistItemSkip(cPLI); }
		helpers.replica.mam.Macro Player.IInteract.MacroGet(string sMacroName) { return MacroGet(sMacroName); }
		string Player.IInteract.MacroExecute(helpers.replica.mam.Macro cMacro) { return MacroExecute(cMacro); }
		PlaylistItem[] replica.Player.IInteract.PlaylistClipsGet()
		{
			return new PlaylistItem[0];
		}
		#endregion реализация Player.IInteract
		#region реализация Cues.IInteract
		Cues.IInteract Cues.IInteract.Init()
		{
			return new Failover();
		}

		PlaylistItem Cues.IInteract.PlaylistItemOnAirGet() { return PlaylistItemOnAirGet(); }
		Queue<PlaylistItem> Cues.IInteract.PlaylistItemsPreparedGet() { return PlaylistItemsPreparedGet(); }
		helpers.replica.cues.TemplateBind[] Cues.IInteract.TemplateBindsGet(PlaylistItem cPLI) { return TemplateBindsGet(cPLI); }
		#endregion реализация Cues.IInteract
		#region реализация Template.IInteract
		replica.cues.Template.IInteract replica.cues.Template.IInteract.Init()
		{
			return new Failover();
		}
		helpers.replica.mam.Macro replica.cues.Template.IInteract.MacroGet(string sMacroName) { return MacroGet(sMacroName); }
		string replica.cues.Template.IInteract.MacroExecute(helpers.replica.mam.Macro cMacro) { return MacroExecute(cMacro); }
		helpers.replica.cues.TemplatesSchedule[] replica.cues.Template.IInteract.TemplatesScheduleGet() { return new helpers.replica.cues.TemplatesSchedule[0]; }
		void replica.cues.Template.IInteract.TemplatesScheduleSave(helpers.replica.cues.TemplatesSchedule cTemplatesSchedule) { }
		void replica.cues.Template.IInteract.TemplateStarted(replica.cues.Template.Range cRange) { }
		PlaylistItem replica.cues.Template.IInteract.PlaylistItemPreviousGet(PlaylistItem cPLI) { return new PlaylistItem(); }
		#endregion реализация Template.IInteract
	}
}
