using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;

using helpers;
using helpers.extensions;
using helpers.replica.pl;

namespace replica.player
{
	 public class DBInteract : helpers.replica.DBInteract, Playlist.IInteract, Player.IInteract
	{
		public delegate void PlayerSkipDelegate();
		public DBInteract()
		{
			_cDB = new DB();
			_cDB.CredentialsLoad();
		}

		public void ProcessCommands(Delegate[] aDelegates)
		{
			try
			 {
				long nCommandQueueID = -1, nCommandStatusID = 3;
				string sCommandName = null;
                Queue<Hashtable> aqDBValues = _cDB.Select("SELECT id, `sCommandName` FROM adm.`vCommandsQueue` WHERE `sCommandName` IN ('playlist_recalculate','player_start','player_pause','player_stop','player_skip') AND 'waiting'=`sCommandStatus` ORDER BY dt"); //UNDONE сделать нормальную обработку символьных имен команд через Preferences
				if (null == aqDBValues)
					return;
				Hashtable ahRow = null;
				while (0 < aqDBValues.Count)
				{
					try
					{
						ahRow = aqDBValues.Dequeue();
						sCommandName = ahRow["sCommandName"].ToString();
						(new Logger("commands")).WriteNotice("Начало выполнения команды [" + sCommandName + "]");
						nCommandQueueID = ahRow["id"].ToID();
						_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=2 WHERE id=" + nCommandQueueID);
						switch (sCommandName)
						{
							case "player_start":
							case "player_pause":
							case "player_stop":
								nCommandStatusID = 3;
								break;
							case "player_skip":
								PlayerSkipDelegate PlayerSkip;
								if (!aDelegates.IsNullOrEmpty() && null != (PlayerSkip = (PlayerSkipDelegate)aDelegates.FirstOrDefault(o => o is PlayerSkipDelegate)))
								{
									PlayerSkip();
									nCommandStatusID = 4;
								}
								else
									(new Logger("commands")).WriteError("отсутствует необходимый метод [PlayerSkip]");
                                break;
                            case "playlist_recalculate":
								long nPLitemsID = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'nPLitemsID'=`sKey` AND `idCommandsQueue`=" + nCommandQueueID).ToID(); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
								int nHoursQty = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'nHoursQty'=`sKey` AND `idCommandsQueue`=" + nCommandQueueID).ToInt32(); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
								//EMERGENCY а потом руссинке понадобится плейлист считать на два с половиной часа и что ты будешь делать? хотя я знаю... ты добавишь еще один параметр, который будет указывать кол-во минут... =))) Валь, возьми себя в руки =))
								TimeSpan dtTS;
								if (0 == nHoursQty || int.MaxValue == nHoursQty)
								{
									dtTS = TimeSpan.MaxValue;
									nPLitemsID = int.MaxValue;
								}
								else
									dtTS = new TimeSpan(nHoursQty, 0, 0);
								if (PlaylistItemsTimingsUpdate(dtTS, nPLitemsID))
									nCommandStatusID = 4;
								else
									nCommandStatusID = 3;
								break;
							default:
								throw new Exception("Неизвестная команда");
						}
					}
					catch (Exception ex)
					{
						(new Logger("commands-2")).WriteError(ex);
					}
					try
					{
						if (0 < nCommandQueueID)
							_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=" + nCommandStatusID + " WHERE id=" + nCommandQueueID);
					}
					catch (Exception ex)
					{
						(new Logger("commands-3")).WriteError(ex);
					}
					if (null != sCommandName)
						(new Logger("commands")).WriteNotice("Завершение выполнения команды [" + sCommandName + "] [status=" + nCommandStatusID + "]");
				}
			}
			catch (Exception ex)
			{

				(new Logger("commands-1")).WriteError(ex);
			}
		}

		public PlaylistItem PlaylistItemCurrentGet()
		{
			PlaylistItem cRetVal = null;
			try
			{
				cRetVal = new PlaylistItem(_cDB.GetRow("SELECT DISTINCT * FROM pl.`vTimedItemCurrent` cti, pl.`vPlayListResolved` plr WHERE cti.id=plr.id"));
			}
			catch// (Exception ex)
			{
				//(new Logger("playlist")).WriteError(ex);
			}
			return cRetVal;
		}
		public PlaylistItem PlaylistItemLockedPreviousGet()
		{
			PlaylistItem cRetVal = null;
			try
			{
				cRetVal = new PlaylistItem(_cDB.GetRow("SELECT DISTINCT * FROM pl.`vPlayListResolved` WHERE `sStatusName` IN ('queued','prepared','onair') ORDER BY `dtStartReal` DESC,`dtStartQueued` DESC LIMIT 1"));
			}
			catch// (Exception ex)
			{
				//(new Logger("playlist")).WriteError(ex);
			}

			return cRetVal;
		}
		public PlaylistItem[] PlaylistItemsPlannedGet()
		{
			List<PlaylistItem> aRetVal = new List<PlaylistItem>();
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT * FROM pl.`vPlayListResolved` WHERE 'planned'=`sStatusName` ORDER BY `dtStartPlanned` LIMIT 100");
			if (null != aqDBValues)
			{
				while (0 < aqDBValues.Count)
					aRetVal.Add(new PlaylistItem(aqDBValues.Dequeue()));
			}
			return aRetVal.ToArray();
		}

		public ulong PlaylistItemOnAirFramesLeftGet(int nOneFrameInMs)
		{
			ulong nRetVal = 0;
            ulong nCurrentFramesPast;
            Hashtable ahRes;
			try
			{
                ahRes = _cDB.GetRow("SELECT `nFrameStop`-`nFrameStart` as `nFramesQty`, `dtStartReal` FROM pl.`vPlayListResolved` WHERE 'onair'=`sStatusName` ORDER BY `dtStartReal` DESC LIMIT 1");
                nRetVal = ahRes["nFramesQty"].ToULong();
                DateTime dtSR = ahRes["dtStartReal"].ToDT();
                if (DateTime.Now <= dtSR)
                    return 0;
                nCurrentFramesPast = (ulong)(DateTime.Now.Subtract(dtSR).TotalMilliseconds / nOneFrameInMs);
                nRetVal = nRetVal - nCurrentFramesPast;
            }
			catch// (Exception ex)
			{
				//(new Logger("playlist")).WriteError(ex);
			}
			return nRetVal;
		}

		public void PlaylistReset()
		{
			while (true)
			{
				try
				{
					_cDB.Perform("UPDATE pl.`tItems` SET `idStatuses`=1 WHERE `idStatuses` IN (2, 3, 4)");
					PlaylistItem cPLICurrent = PlaylistItemCurrentGet();
					double nDelta = 1000;
					if (null == cPLICurrent || 60 < (nDelta = DateTime.Now.Subtract(cPLICurrent.dtTimingsUpdate).TotalMinutes) || 0 > nDelta)
					{
						(new Logger("playlist")).WriteNotice("Перед запуском идёт полный пересчет плей-листа, т.к. предыдущий раз пересчет текущего элемента был слишком давно (более 60 минут назад)"); //TODO LANG
						PlaylistItemsTimingsUpdate();
					}
					else
						(new Logger("playlist")).WriteNotice("Перед запуском пересчет плей-листа не нужен, т.к. предыдущий раз пересчет текущего элемента был всего лишь " + Math.Round(nDelta, 2) + " минут назад"); //TODO LANG
					_cDB.Perform("UPDATE pl.`tItems` SET `idStatuses`=6 WHERE id IN (SELECT id FROM pl.`vPlayListResolved` WHERE `dtStartPlanned` < now() AND 'planned'=`sStatusName`)");
					break;
				}
				catch (Exception ex)
				{
					(new Logger("playlist")).WriteError(ex);
				}
			}
		}

        public bool PlaylistItemsTimingsUpdate()
        {
			lock (playlist.Calculator.cSyncRoot)
			{
				return PlaylistItemsTimingsUpdate(TimeSpan.MaxValue);
			}
        }
		public bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope)
		{
			return PlaylistItemsTimingsUpdate(tsUpdateScope, -1);
		}
		public bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, long nStartPlitemsID)
        {
			lock (playlist.Calculator.cSyncRoot)
            {
				if (0 > nStartPlitemsID && TimeSpan.MaxValue > tsUpdateScope && TimeSpan.FromSeconds(60) > DateTime.Now.Subtract(playlist.Calculator.dtLast))  // если пересчитывает робот, то не чаще раза в минуту
				{
					(new Logger("playlist")).WriteNotice("Recalculating was aborted because the last recalculating was less than 60 seconds ago. Info: [startPLI=" + nStartPlitemsID + "][scope=" + tsUpdateScope.TotalHours + " hours]");
					return true;
				}
				bool bRetVal = false;
                _cDB.TransactionBegin();
				try
                {
					playlist.Calculator cCalculator = new playlist.Calculator();

					cCalculator.ItemsInsertedFill = ItemsInsertedFill;
					cCalculator.ItemStartPlannedSet = ItemStartPlannedSet;
					cCalculator.ItemTimingsUpdateSet = ItemTimingsUpdateSet;
					cCalculator.ItemFrameStopChange = ItemFrameStopChange;
					cCalculator.ItemFrameStopRestore = ItemFrameStopRestore;
					cCalculator.ItemRemove = ItemRemove;
					cCalculator.ItemsRemoveFromCached = ItemsRemoveFromCached;
					cCalculator.PlugAdd = PlugAdd;
					cCalculator.PlugUpdate = PlugUpdate;
					cCalculator.ahFrameStopsInitials = new Dictionary<long, long>();

					Queue<Hashtable> aqDBValues = null;
					Hashtable ahRow = null;
					if(null != (aqDBValues = _cDB.Select("SELECT `idItems`, `nValue` as `nFrameStopInitial` FROM pl.`tItemAttributes` WHERE 'nFrameStopInitial'=`sKey`;")))
					{
						while (0 < aqDBValues.Count)
						{
							ahRow = aqDBValues.Dequeue();
							if (null != ahRow["idItems"] && null != ahRow["nFrameStopInitial"])
								cCalculator.ahFrameStopsInitials.Add(ahRow["idItems"].ToID(), ahRow["nFrameStopInitial"].ToLong());
						}
					}
					List<PlaylistItem> aPLIs = new List<PlaylistItem>();
					aqDBValues = _cDB.Select("SELECT DISTINCT * FROM pl.`vPlayListResolved` WHERE `sStatusName` NOT IN ('played', 'skipped', 'failed');");
					if (null == aqDBValues)
						throw new Exception("can't find any playlist items"); //TODO LANG
					while (0 < aqDBValues.Count)
						aPLIs.Add(new PlaylistItem(aqDBValues.Dequeue()));

					cCalculator.aPLIs = aPLIs;
					cCalculator.tsUpdateScope = tsUpdateScope;
					cCalculator.nStartPlitemsID = nStartPlitemsID;
					cCalculator.aCached = base.PlaylistItemIDsCachedGet();

					bRetVal = cCalculator.Calculate();
					_cDB.TransactionCommit();
					return bRetVal;
                }
                catch (Exception ex)
                {
                    _cDB.TransactionRollBack();
					playlist.Calculator.dtLast = DateTime.MinValue;
					(new playlist.Calculator.Logger()).WriteError(ex);
                }
				return false;
            }
        }

		#region calculator callbacks
		private void ItemsInsertedFill(Dictionary<long, List<long>> ahItemsInserted, Dictionary<long, PlaylistItem> ahItemsInsertedBinds)
		{
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT `nPrecedingID`, id FROM pl.`vItemsInserted` ORDER BY `nPrecedingID`, `nPrecedingOffset`;");
			Hashtable ahRow;
			if (null != aqDBValues)
			{
				long nPrecedingID, nID;
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					nPrecedingID = ahRow["nPrecedingID"].ToID();
					if (!ahItemsInserted.ContainsKey(nPrecedingID))
						ahItemsInserted.Add(nPrecedingID, new List<long>());
					nID = ahRow["id"].ToID();
					ahItemsInserted[nPrecedingID].Add(nID);
					ahItemsInsertedBinds.Add(nID, null);
				}
			}
		}
		private void ItemStartPlannedSet(long nID, DateTime dtStartPlanned)
		{
			_cDB.Cache("SELECT pl.`fItemStartPlannedSet`(" + nID + ", '" + dtStartPlanned.ToStr() + "');");
		}
		private void ItemTimingsUpdateSet(long nID, DateTime dtTimingsUpdate)
		{
			_cDB.Cache("SELECT pl.`fItemTimingsUpdateSet`(" + nID + ", '" + dtTimingsUpdate.ToStr() + "');");
		}
		private void ItemFrameStopChange(long nID, long nFrameStopInitial, long nFrameStopNew)
		{
			_cDB.Cache("SELECT pl.`fItemAttributeAdd`(" + nID + ", 'nFrameStopInitial', " + nFrameStopInitial + ");SELECT pl.`fItemAttributeSet`(" + nID + ", 'nFrameStop', " + nFrameStopNew + ");");
		}
		private void ItemFrameStopRestore(long nID)
		{
			_cDB.Cache("SELECT CASE WHEN `bValue` THEN pl.`fItemAttributeSet`(" + nID + ", 'nFrameStop', `nValue`) END FROM pl.`fItemAttributeValueGet`(" + nID + ", NULL, 'nFrameStopInitial');");
		}
		private void ItemRemove(long nID)
		{
			_cDB.Cache("DELETE FROM pl.`tItemsCached` WHERE `idItems` = " + nID + "");
			_cDB.Cache("SELECT pl.`fItemRemove`(" + nID + ");");
		}
		private void PlugAdd(DateTime dtStart, long nDuration, DateTime dtTimingsUpdate)
		{
			(new replica.playlist.Calculator.Logger()).WriteNotice("plug add: start=" + dtStart.ToStr() + ", duration=" + nDuration);
			_cDB.Cache("SELECT pl.`fItemTimingsUpdateSet`(`nValue`, '" + dtTimingsUpdate.ToStr() + "') FROM pl.`fPlaylistPlugAdd`('" + dtStart.ToStr() + "'," + nDuration + ");");
		}
		private void PlugUpdate(long nID, DateTime dtStart, long nDuration, DateTime dtTimingsUpdate)
		{
			(new replica.playlist.Calculator.Logger()).WriteNotice("plug update: nID=" + nID + ", start=" + dtStart.ToStr() + ", duration=" + nDuration);
			_cDB.Cache("SELECT pl.`fItemTimingsUpdateSet`(`nValue`, '" + dtTimingsUpdate.ToStr() + "') FROM pl.`fPlaylistPlugUpdate`(" + nID + ",'" + dtStart.ToStr() + "'," + nDuration + ");");
			//DELETE FROM pl."tItemDTEvents" WHERE id = (SELECT e.id FROM pl."tItemDTEvents" AS e join pl."tItemDTEventTypes" AS t ON e."idItemDTEventTypes"=t.id WHERE "idItems"=31711 AND "sName"='start_queued')
			// у локедов мы не апдейтим 
			_cDB.Cache("DELETE FROM pl.`tItemDTEvents` WHERE id = (SELECT e.id FROM pl.`tItemDTEvents` AS e join pl.`tItemDTEventTypes` AS t ON e.`idItemDTEventTypes`=t.id WHERE `idItems`=" + nID + " AND `sName`='start_queued')");
		}
		private void ItemsRemoveFromCached(List<long> aIDs)
		{
			string sIDs = "";
			foreach (long nID in aIDs)
			{
				if ("" == sIDs)
					sIDs += "" + nID;
				else
					sIDs += ", " + nID;
			}
			_cDB.Cache("DELETE FROM pl.`tItemsCached` WHERE `idItems` in (" + sIDs + ")");
			(new replica.playlist.Calculator.Logger()).WriteNotice("remove from cached: nIDs=[" + sIDs + "]");
		}
		#endregion calculator callbacks

		public helpers.replica.pl.Proxy ProxyGet(Class cClass)
		{
			try
			{
				return helpers.replica.pl.Proxy.Get(cClass);
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
			return null;
		}

		public void PlaylistItemQueue(PlaylistItem cPLI)
		{
			try
			{
				_cDB.PerformAsync("SELECT `bValue` FROM pl.`fItemQueued`(" + cPLI.nID + ", '" + cPLI.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss.ff") + "')");
				//TODO LOG ошибочный статус селекта
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		public void PlaylistItemPrepare(PlaylistItem cPLI)
		{
			try
			{
				_cDB.PerformAsync("SELECT `bValue` FROM pl.`fItemPrepared`(" + cPLI.nID + ")");
				//TODO LOG ошибочный статус селекта
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		public void PlaylistItemStart(PlaylistItem cPLI, ulong nDelay) //EMERGENCY нужно обрабатывать delay
		{
			try
			{
				_cDB.PerformAsync("SELECT `bValue` FROM pl.`fItemStarted`(" + cPLI.nID + ", " + cPLI.nFrameStart + ");");
				//TODO LOG ошибочный статус селекта
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		public void PlaylistItemStop(PlaylistItem cPLI)
		{
			try
			{
				_cDB.PerformAsync("SELECT `bValue` FROM pl.`fItemStopped`(" + cPLI.nID + ")");
				//TODO LOG ошибочный статус селекта
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		public void PlaylistItemFail(PlaylistItem cPLI)
		{
			try
			{
				_cDB.PerformAsync("SELECT `bValue` FROM pl.`fItemFailed`(" + cPLI.nID + ")");
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		public void PlaylistItemSkip(PlaylistItem cPLI)
		{
			try
			{
				_cDB.PerformAsync("SELECT `bValue` FROM pl.`fItemSkipped`(" + cPLI.nID + ")");
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}

		#region реализация Playlist.IInteract
		Playlist.IInteract Playlist.IInteract.Init()
		{
			return new DBInteract();
		}
		void Playlist.IInteract.PlaylistReset()
		{
			this.PlaylistReset();
		}

		PlaylistItem Playlist.IInteract.PlaylistItemCurrentGet() { return this.PlaylistItemCurrentGet(); }
		PlaylistItem Playlist.IInteract.PlaylistItemLockedPreviousGet() { return this.PlaylistItemLockedPreviousGet(); }
		PlaylistItem[] Playlist.IInteract.PlaylistItemsPlannedGet() { return this.PlaylistItemsPlannedGet(); }
		ulong Playlist.IInteract.PlaylistItemOnAirFramesLeftGet(int nOneFrameInMs) { return this.PlaylistItemOnAirFramesLeftGet(nOneFrameInMs); }

		bool Playlist.IInteract.PlaylistItemsTimingsUpdate() { return this.PlaylistItemsTimingsUpdate(); }
		bool Playlist.IInteract.PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope) { return this.PlaylistItemsTimingsUpdate(tsUpdateScope); }
		bool Playlist.IInteract.PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID) { return this.PlaylistItemsTimingsUpdate(tsUpdateScope, nStartPlitemsID); }

		void Playlist.IInteract.PlaylistItemFail(PlaylistItem cPLI) { this.PlaylistItemFail(cPLI); }
		#endregion реализация Playlist.IInteract
		#region реализация Player.IInteract
		Player.IInteract Player.IInteract.Init()
		{
			return new DBInteract();
		}
		helpers.replica.pl.Proxy Player.IInteract.ProxyGet(Class cClass) { return ProxyGet(cClass); }
		void Player.IInteract.PlaylistItemQueue(PlaylistItem cPLI) { this.PlaylistItemQueue(cPLI); }
		void Player.IInteract.PlaylistItemPrepare(PlaylistItem cPLI) { this.PlaylistItemPrepare(cPLI); }
		void Player.IInteract.PlaylistItemStart(PlaylistItem cPLI, ulong nDelay) { this.PlaylistItemStart(cPLI, nDelay); }
		void Player.IInteract.PlaylistItemStop(PlaylistItem cPLI) { this.PlaylistItemStop(cPLI); }
		void Player.IInteract.PlaylistItemFail(PlaylistItem cPLI) { this.PlaylistItemFail(cPLI); }
		void Player.IInteract.PlaylistItemSkip(PlaylistItem cPLI) { this.PlaylistItemSkip(cPLI); }
		helpers.replica.mam.Macro Player.IInteract.MacroGet(string sMacroName) { return helpers.replica.mam.Macro.Get(sMacroName); }
		string Player.IInteract.MacroExecute(helpers.replica.mam.Macro cMacro) { return cMacro.Execute(); }
		PlaylistItem[] Player.IInteract.PlaylistClipsGet() { return _cDB.Select("SELECT * FROM pl.`vPlayListClips` ORDER BY id").Select(o => new PlaylistItem() { nID = o["id"].ToID() }).ToArray(); }
		#endregion реализация Player.IInteract
	}
}