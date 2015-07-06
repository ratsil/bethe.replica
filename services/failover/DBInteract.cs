using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using helpers;
using helpers.extensions;
using helpers.replica.pl;

namespace replica.failover
{
	class DBInteract : helpers.replica.DBInteract
	{
		public delegate void PlayerSkipDelegate();
		public delegate void FailoverSynchronizeDelegate();

		public DBInteract()
		{
			_cDB = new DB();
			_cDB.CredentialsLoad();
		}

		public void ProcessCommands(Delegate[] aDelegates)
		{
			try
			{
				long nCommandQueueID = -1, nCommandStatusID;
				string sCommandName = null;
				Queue<Hashtable> aqDBValues = _cDB.Select("SELECT id, `sCommandName` FROM adm.`vCommandsQueue` WHERE `sCommandName` IN ('failover_sync', 'failover_skip') AND 'waiting'=`sCommandStatus` ORDER BY dt"); //UNDONE сделать нормальную обработку символьных имен команд через Preferences
				if (null == aqDBValues)
					return;
				Hashtable ahRow = null;
				while (0 < aqDBValues.Count)
				{
					nCommandStatusID = 3;
					try
					{
						ahRow = aqDBValues.Dequeue();
						sCommandName = ahRow["sCommandName"].ToString();
						(new Logger("commands")).WriteNotice("Ќачало выполнени€ команды [" + sCommandName + "]");
						nCommandQueueID = ahRow["id"].ToID();
						_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=2 WHERE id=" + nCommandQueueID);
						switch (sCommandName)
						{
							case "failover_sync":
								FailoverSynchronizeDelegate FailoverSynchronize;
								if (!aDelegates.IsNullOrEmpty() && null != (FailoverSynchronize = (FailoverSynchronizeDelegate)aDelegates.FirstOrDefault(o => o is FailoverSynchronizeDelegate)))
								{
									FailoverSynchronize();
									nCommandStatusID = 4;
								}
								else
									(new Logger("commands")).WriteError("отсутствует необходимый метод [FailoverSynchronize]");
								break;
							case "failover_skip":
								PlayerSkipDelegate PlayerSkip;
								if (!aDelegates.IsNullOrEmpty() && null != (PlayerSkip = (PlayerSkipDelegate)aDelegates.FirstOrDefault(o => o is PlayerSkipDelegate)))
								{
									PlayerSkip();
									nCommandStatusID = 4;
								}
								else
									(new Logger("commands")).WriteError("отсутствует необходимый метод [PlayerSkip]");
                                break;
							default:
								throw new Exception("неизвестна€ команда");
						}
					}
					catch (Exception ex)
					{
						(new Logger("commands")).WriteError(ex);
					}
					try
					{
						if (0 < nCommandQueueID)
							_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=" + nCommandStatusID + " WHERE id=" + nCommandQueueID);
					}
					catch (Exception ex)
					{
						(new Logger("commands")).WriteError(ex);
					}
					if (null != sCommandName)
						(new Logger("commands")).WriteNotice("завершение выполнени€ команды [" + sCommandName + "] [status=" + nCommandStatusID + "]");
				}
				Failover.ahErrors[Failover.ErrorTarget.dbi_framesinitial] = DateTime.MinValue;
			}
			catch (Exception ex)
			{
				if (DateTime.MinValue == Failover.ahErrors[Failover.ErrorTarget.dbi_framesinitial])
					(new Logger("commands")).WriteError(ex);
				Failover.ahErrors[Failover.ErrorTarget.dbi_framesinitial] = DateTime.Now;
			}
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
		public Dictionary<long, long> PlaylistItemsFramesStopInitialsGet()
		{
			Dictionary<long, long> ahRetVal = new Dictionary<long, long>();
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT `idItems`, `nValue` as `nFrameStopInitial` FROM pl.`tItemAttributes` WHERE 'nFrameStopInitial'=`sKey`");
			if (null != aqDBValues)
			{
				Hashtable ahRow;
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					ahRetVal.Add(ahRow["idItems"].ToID(), ahRow["nFrameStopInitial"].ToInt());
				}
			}
			return ahRetVal;
		}
		public Dictionary<long, helpers.replica.mam.Cues> ComingUpAssetsCuesGet()
		{
			Dictionary<long, helpers.replica.mam.Cues> ahRetVal = new Dictionary<long, helpers.replica.mam.Cues>();
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT ac.* FROM pl.`vComingUp` cu, mam.`vAssetsCues` ac WHERE cu.`idAssets` = ac.id");
			Hashtable ahRow;
			if (null != aqDBValues)
			{
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					ahRetVal.Add(ahRow["id"].ToID(), new helpers.replica.mam.Cues(ahRow["idCues"], ahRow["sSong"], ahRow["sArtist"], ahRow["sAlbum"], ahRow["nYear"], ahRow["sPossesor"]));
				}
			}
			return ahRetVal;
		}
	}
}