using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

using System.Linq;
using helpers;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.pl;
using helpers.replica.media;

using System.ServiceProcess;

namespace replica.management
{
	public class DBInteract : helpers.replica.DBInteract   //TODO убрать public (поставил для теста извне)
	{
		private struct COMMAND_FILE_UPDATE_INFO
		{
			public long nID;
			public bool bForce;
			public Dictionary<long, long> aAssetCommandBinds;
			public COMMAND_FILE_UPDATE_INFO(long nID, bool bForce)
			{
				this.nID = nID;
				this.bForce = bForce;
				aAssetCommandBinds = new Dictionary<long, long>();
			}
		}

		public DBInteract()
		{
			_cDB = new DB();
			_cDB.CredentialsLoad();
		}
		public void ProcessCommands()
		{
			try
			{
				long nCommandQueueID = -1, nCommandStatusID = 3;
				string sCommandName = null;
				try
				{
                    Queue<Hashtable> aqFileUpdateCommands = new Queue<Hashtable>();
					Queue<Hashtable> aqDBValues = _cDB.Select("SELECT id, `sCommandName` FROM adm.`vCommandsQueue` WHERE `sCommandName` IN ('asset_file_update','playlist_item_duration_update','asset_duration_update','file_duration_get', 'service_start', 'service_stop') AND 'waiting'=`sCommandStatus` ORDER BY dt"); //UNDONE сделать нормальную обработку символьных имен команд через Preferences
					if (null == aqDBValues)
						return;
					Hashtable ahRow = null;
					while (0 < aqDBValues.Count)
					{
						try
						{
							ahRow = aqDBValues.Dequeue();
							sCommandName = ahRow["sCommandName"].ToString();
							(new Logger()).WriteNotice("Начало выполнения команды [" + sCommandName + "]");
							nCommandQueueID = ahRow["id"].ToID();
							_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=2 WHERE id=" + nCommandQueueID);
							switch (sCommandName)
							{
								case "playlist_item_duration_update":
									long nPLIID = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'idItems'=`sKey` AND `idCommandsQueue`=" + nCommandQueueID).ToID(); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
									PlaylistItemDurationUpdate(nPLIID);
									nCommandStatusID = 4;
									break;
								case "asset_duration_update":
									long nAssetID = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'idAssets'=`sKey` AND `idCommandsQueue`=" + nCommandQueueID).ToID(); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
									AssetDurationUpdate(nAssetID);
									nCommandStatusID = 4;
									break;
                                case "file_duration_get":
                                    long nFileID = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE `sKey`='idFiles' AND `idCommandsQueue`=" + nCommandQueueID).ToID(); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
                                    File cFile = File.Load(nFileID);
                                    nCommandStatusID = 3;
                                    if (null != cFile)
                                    {
                                        ulong nFramesQty = FileDurationGet(cFile.sFile);
                                        if (0 < nFramesQty)
                                        {
                                            int nN = _cDB.Perform("SELECT adm.`fCommandParameterAdd` (" + nCommandQueueID + ", 'nFramesQty', '" + nFramesQty + "')");
                                            nCommandStatusID = 4;
                                            if (int.MinValue == nN)
                                            {
                                                nCommandStatusID = 3;
                                            }
                                        }
                                    }
									break;
								case "service_start":
									if (ServiceStart(_cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'sServiceName'=`sKey` AND `idCommandsQueue`=" + nCommandQueueID))) //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
										nCommandStatusID = 4;
									break;
								case "service_stop":
									if (ServiceStop(_cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'sServiceName'=`sKey` AND `idCommandsQueue`=" + nCommandQueueID))) //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
										nCommandStatusID = 4;
									break;
								case "asset_file_update":
                                    aqFileUpdateCommands.Enqueue(ahRow);
                                    nCommandQueueID = -1;
                                    sCommandName = null;
                                    break;
								default:
									throw new Exception("неизвестная команда [" + sCommandName + "]");
							}
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
						}
						try
						{
							if (0 < nCommandQueueID)
								_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=" + nCommandStatusID + " WHERE id=" + nCommandQueueID);
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
						}
						if (null != sCommandName)
							(new Logger()).WriteNotice("Завершение выполнения команды [" + sCommandName + "]");
					}
                    if(0 < aqFileUpdateCommands.Count)
                        FileUpdate(aqFileUpdateCommands);
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

        public void FileUpdate(Queue<Hashtable> aqFileUpdateCommands)
		{
			try
			{
				long nCommandQueueID = -1;
				string sCommandName = null;
				try
				{
					Hashtable ahRow = null, ahParam = null;
					Dictionary<string, COMMAND_FILE_UPDATE_INFO> aFileBinds = new Dictionary<string, COMMAND_FILE_UPDATE_INFO>();
                    Queue<Hashtable> aqParams = null;
					if (null == aqFileUpdateCommands)
						return;
					long nID = -1;
					string sFile = null;
					bool bForce = false;
					_cDB.TransactionBegin();
					while (0 < aqFileUpdateCommands.Count)
					{
						try
						{
							ahRow = aqFileUpdateCommands.Dequeue();
							sCommandName = ahRow["sCommandName"].ToString();
							nCommandQueueID = ahRow["id"].ToID();
							(new Logger()).WriteNotice("начало выполнения команды [" + sCommandName + "][" + nCommandQueueID + "]");
							_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=2 WHERE id=" + nCommandQueueID);
							nID = -1;
							sFile = null;
							bForce = false;
							aqParams = _cDB.Select("SELECT `sKey`,`sValue` FROM adm.`tCommandParameters` WHERE `idCommandsQueue`=" + nCommandQueueID); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
							while (0 < aqParams.Count)
							{
								ahParam = aqParams.Dequeue();
								switch (ahParam["sKey"].ToString())
								{
									case "sBeepFile":
										sFile = ahParam["sValue"].ToString();
										break;
									case "idAssets":
										nID = ahParam["sValue"].ToID();
										break;
									case "bForce":
										bForce = ahParam["sValue"].ToBool();
										break;
								}
							}
							if (0 > nID || null == sFile)
							{
								_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=3 WHERE id=" + nCommandQueueID);
								(new Logger()).WriteNotice("ошибочное (некорректные параметры) завершение выполнения команды [" + nCommandQueueID + "]");
								break;
							}
							if (!aFileBinds.ContainsKey(sFile))
							{
								long nCommandUploadID = _cDB.GetValue("SELECT `nValue` FROM adm.`fFileUpload`('" + sFile + "', " + bForce.ToString() + ")").ToID();
								aFileBinds.Add(sFile, new COMMAND_FILE_UPDATE_INFO(nCommandUploadID, bForce));
							}
							else if(bForce && !aFileBinds[sFile].bForce)
							{
								_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=1 WHERE id=" + nCommandQueueID);
								continue;
							}
							switch (sCommandName)
							{
								case "asset_file_update":
									if (!aFileBinds[sFile].aAssetCommandBinds.ContainsKey(nID))
										aFileBinds[sFile].aAssetCommandBinds.Add(nID, nCommandQueueID);
									break;
								default:
									throw new Exception("Неизвестная команда");
							}
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
						}
					}
					_cDB.TransactionCommit();
					string sStatus;
					DateTime dtNow = DateTime.Now;
					long nFileID;
					Dictionary<string, bool> aFileDone = new Dictionary<string, bool>();
					{
						foreach (string sFD in aFileBinds.Keys)
							aFileDone.Add(sFD, false);
					}
					while (true)
					{
						foreach (string sBeepFile in aFileBinds.Keys)
						{
							try
							{
								sStatus = _cDB.GetValue("SELECT `sCommandStatus` FROM adm.`vCommandsQueue` WHERE id=" + aFileBinds[sBeepFile].nID);
								if ("succeed" == sStatus)
								{
									nFileID = _cDB.GetValue("SELECT `sValue` FROM adm.`tCommandParameters` WHERE 'idFiles'=`sKey` AND `idCommandsQueue`=" + aFileBinds[sBeepFile].nID).ToID(); //UNDONE сделать нормальную обработку символьных названий параметров через Preferences
									foreach (long nAssetID in aFileBinds[sBeepFile].aAssetCommandBinds.Keys)
									{
										try
										{
											_cDB.Perform("SELECT mam.`fAssetFileSet`(" + nAssetID + ", " + nFileID + ")");
											AssetDurationUpdate(nAssetID);
											_cDB.Perform("SELECT mam.`fAssetCustomValueSet`(" + nAssetID + ", 'beep_file', '" + sBeepFile + "')");
											Queue<PlaylistItem> aqPLIs = PlaylistItemsGet(Asset.Load(nAssetID));
											if (null != aqPLIs)
											{
												PlaylistItem cPLI = null;
												while (0 < aqPLIs.Count)
												{
													cPLI = aqPLIs.Dequeue();
													_cDB.Perform("SELECT pl.`fItemFileSet`(" + cPLI.nID + ", " + nFileID + ")");
													PlaylistItemDurationUpdate(cPLI.nID);
												}
											}
											_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=4 WHERE id=" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID]);
											(new Logger()).WriteNotice("Завершение выполнения команды [" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID] + "]");
										}
										catch (Exception ex)
										{
											(new Logger()).WriteError(ex);
											_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=3 WHERE id=" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID]);
											(new Logger()).WriteNotice("Ошибочное завершение выполнения команды [" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID] + "]");
										}
									}
									aFileDone[sBeepFile] = true;
									
								}
								else if ("failed" == sStatus)
								{
									foreach (long nAssetID in aFileBinds[sBeepFile].aAssetCommandBinds.Keys)
									{
										_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=3 WHERE id=" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID]);
										(new Logger()).WriteNotice("Ошибочное (по загрузке файла) завершение выполнения команды [" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID] + "]");
									}
									aFileDone[sBeepFile] = true;
								}
								else if ("waiting" == sStatus && 60 < DateTime.Now.Subtract(dtNow).TotalMinutes)
								{
									foreach (long nAssetID in aFileBinds[sBeepFile].aAssetCommandBinds.Keys)
									{
										_cDB.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`=3 WHERE id=" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID]);
										(new Logger()).WriteNotice("Ошибочное (по таймауту загрузки) завершение выполнения команды [" + aFileBinds[sBeepFile].aAssetCommandBinds[nAssetID] + "]");
									}
									aFileDone[sBeepFile] = true;
								}
							}
							catch (Exception ex)
							{
								(new Logger()).WriteError(ex);
							}
						}
						foreach (string sFD in aFileDone.Keys)
							if (aFileDone[sFD] && aFileBinds.ContainsKey(sFD))
								aFileBinds.Remove(sFD);
						if (1 > aFileBinds.Count)
							break;

					}
				}
				catch (Exception ex)
				{
					_cDB.TransactionRollBack();
					(new Logger()).WriteError(ex);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		public bool ServiceStart(string sServiceName)
		{
			(new Logger()).WriteWarning("запуск службы [" + sServiceName + "]");//TODO LANG
			ServiceController cCues = new ServiceController(sServiceName);
			try
			{
				TimeSpan tsTimeout = TimeSpan.FromMilliseconds(10000);

				cCues.Start();
				cCues.WaitForStatus(ServiceControllerStatus.Running, tsTimeout);
				(new Logger()).WriteWarning("служба запущена [" + sServiceName + "]");//TODO LANG
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex); //UNDONE
				return false;
			}
			return true;
		}
		public bool ServiceStop(string sServiceName)
		{
			(new Logger()).WriteWarning("остановка службы [" + sServiceName + "]");//TODO LANG
			ServiceController cCues = new ServiceController(sServiceName);
			try
			{
				TimeSpan tsTimeout = TimeSpan.FromMilliseconds(10000);

				cCues.Stop();
				cCues.WaitForStatus(ServiceControllerStatus.Stopped, tsTimeout);
				(new Logger()).WriteWarning("служба остановлена [" + sServiceName + "]");//TODO LANG
			}
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex); //UNDONE
                return false;
            }
            return true;
        }

		public void PlaylistItemDurationUpdate(long nPLIID)
		{
			PlaylistItem cPLI = PlaylistItem.Get(nPLIID);
			ulong nFramesQty = FileDurationGet(cPLI.cFile.sFile);
			_cDB.Perform("SELECT pl.`fItemTimingsSet`(" + nPLIID + ", NULL, " + nFramesQty + ")");
		}
        public ulong FileDurationGet(string sFile)
        {
			ulong nRetVal = 0;
			if (System.IO.File.Exists(sFile))
			{
				ffmpeg.net.File.Input cFile = null;
				try
				{
					cFile = new ffmpeg.net.File.Input(sFile);
					nRetVal = cFile.nFramesQty;
				}
				catch (Exception ex)
				{
					throw new Exception("ffmpeg.net.File.Input(sFile): ошибка обработки файла: " + sFile, ex);
				}
				finally
				{
					try
					{
						if(null != cFile)
							cFile.Dispose();
					}
					catch (Exception ex)
					{
						throw new Exception("cFile.Dispose(): ошибка обработки файла: " + sFile, ex);
					}
				}
			}
			else
				throw new System.IO.FileNotFoundException("не найден файл: " + sFile); //TODO LANG
			return nRetVal;
        }
		public ulong AssetDurationUpdate(Asset cAsset)
		{
			ulong nRetVal = 0;
			Exception cException = null;
			try
			{
				nRetVal = FileDurationGet(cAsset.cFile.sFile);
				_cDB.Perform("SELECT mam.`fAssetAttributeSet`(" + cAsset.nID + ", NULL, 'nFramesQty', " + nRetVal + ")");
			}
			catch (Exception ex)
			{
				cException = ex;
			}
			finally
			{
				if (null != cException)
				{
					if (helpers.replica.Error.no == cAsset.cFile.eError)
					{
						if (cException is System.IO.FileNotFoundException)
							cAsset.cFile.eError = helpers.replica.Error.missed;
						else
							cAsset.cFile.eError = helpers.replica.Error.unknown;
						FileErrorSet(cAsset.cFile); //UNDONE
						(new Logger()).WriteError(cException); //UNDONE
					}
					throw cException;
				}
				else if (null == cException && helpers.replica.Error.no != cAsset.cFile.eError)
					FileErrorRemove(cAsset.cFile);
			}
			return nRetVal;
		}
		public ulong AssetDurationUpdate(long nAssetID)
		{
			return AssetDurationUpdate(Asset.Load(nAssetID));
		}
		public Dictionary<long, DateTime> ArchiveLastPlayedAssetsGet()
        {
			Dictionary<long, DateTime> ahLastPlayed = new Dictionary<long, DateTime>();
			long nID;
            try
            {
                Queue<Hashtable> aqDBValues = _cDB.Select("SELECT max(`dtStartReal`) as `dtLastPlayed`, `idAssets` FROM archive.`pl.tItems` group by `idAssets`");
				Hashtable ahRow;
                while (null != aqDBValues && 0 < aqDBValues.Count)
                {
                    ahRow = aqDBValues.Dequeue();
                    if (null != ahRow && null != ahRow["idAssets"] && null != ahRow["dtLastPlayed"])
						ahLastPlayed.Add(ahRow["idAssets"].ToID(), ahRow["dtLastPlayed"].ToDT());
                }
				aqDBValues = _cDB.Select("SELECT max(`dtStartPlanned`) as `dtLastPlayed`, `idAssets` from pl.`vPlayListResolved`  group by `idAssets`");
				while (null != aqDBValues && 0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					if (null != ahRow && null != ahRow["idAssets"] && null != ahRow["dtLastPlayed"])
					{
						nID = ahRow["idAssets"].ToID();
                        if (!ahLastPlayed.ContainsKey(nID))
							ahLastPlayed.Add(nID, ahRow["dtLastPlayed"].ToDT());
						else
							ahLastPlayed[nID] = ahRow["dtLastPlayed"].ToDT();
                    }
				}
			}
			catch (Exception ex)
            {
                ahLastPlayed = null;
                (new Logger()).WriteError(ex);
            }
            return ahLastPlayed;
        }
		public string LastPlannedAssetNameGet(string sNameBeginning)
		{
			Hashtable ahRow = _cDB.GetRow("select max(`dtStartPlanned`) as `dtLastPlanned`, `sName` from pl.`vPlayListResolved` where `sStorageName`='оформление' and `sName` like '"+ sNameBeginning + "%' group by `sName` order by `dtLastPlanned` desc limit 1");
			if (null != ahRow && ahRow.Count > 0)
				return ahRow["sName"].ToString();
			return null;
		}
		public Queue<Clip> ClipsForPlaylistGet()
        {
            Queue<Clip> aqRetVal = new Queue<Clip>();
            try
            {
                Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT * FROM mam.`vAssetsResolved` WHERE 'clip'=`sVideoTypeName` AND `sRotationName`<>'стоп' ORDER BY `dtLastPlayed` NULLS FIRST, `dtLastFileEvent`");
				while (null != aqDBValues && 0 < aqDBValues.Count)
                    aqRetVal.Enqueue(new Clip(aqDBValues.Dequeue()));
            }
            catch (Exception ex)
            {
                aqRetVal = null;
                (new Logger()).WriteError(ex);
            }
            return aqRetVal;
        }
        public void StorageFreeSpaceSet(ulong nFreeSpace)
		{
            _cDB.Perform("UPDATE adm.`tPreferences` SET `sValue`='" + nFreeSpace + "' WHERE id=2");
		}
		public int RotationClipsQtyGet(DateTime dtHour)
		{
			return _cDB.GetValueInt("SELECT count(*) FROM pl.`vPlayListResolved` plr, mam.`vAssetsResolved` ar WHERE plr.`idAssets`=ar.id AND plr.`dtStart` >= '" + dtHour.ToString("yyyy-MM-dd HH:00:00zzz") + "' AND plr.`dtStart` <= '" + dtHour.ToString("yyyy-MM-dd HH:59:59zzz") + "' AND `sRotationName`='Первая'");
		}
		public int ItemsMoveToArchive()
		{
			try
			{
				if (DayOfWeek.Tuesday == DateTime.Now.DayOfWeek)
				{
					string sDate = DateTime.Now.ToString("yyMMdd");
					string sTableName = _cDB.GetValue("SELECT table_name FROM information_schema.tables WHERE table_name = 'tHouseKeeping_" + sDate + "'");
					if (sTableName.IsNullOrEmpty())
					{
						_cDB.TransactionBegin();
						_cDB.Perform(
							"CREATE TABLE archive.`tHouseKeeping_" + sDate + "` " +
							"(" +
							  "id integer NOT NULL DEFAULT nextval('archive.`tHouseKeeping_id_seq`'::regclass) PRIMARY KEY," +
							  "dt timestamp with time zone NOT NULL DEFAULT now()," +
							  "`idHouseKeeping` integer NOT NULL," +
							  "`idUsers` integer NOT NULL," +
							  "`sUsername` character varying(256) NOT NULL," +
							  "`idDTEventTypes` integer NOT NULL," +
							  "`sDTEventTypeName` character varying(64) NOT NULL," +
							  "`sTG_OP` character(6)," +
							  "`dtEvent` timestamp with time zone NOT NULL," +
							  "`sNote` text" +
							")" +
							"WITH (OIDS=FALSE);"
						);
						_cDB.Perform("GRANT SELECT, INSERT ON TABLE archive.`tHouseKeeping_" + sDate + "` TO replica_management;");
						_cDB.Perform("ALTER TABLE archive.`tHouseKeeping_" + sDate + "` RENAME TO `tHouseKeeping_" + sDate + "_sync`;");
						_cDB.Perform("ALTER TABLE archive.`tHouseKeeping` RENAME TO `tHouseKeeping_" + sDate + "`;");
						_cDB.Perform("ALTER TABLE archive.`tHouseKeeping_" + sDate + "_sync` RENAME TO `tHouseKeeping`;");
						_cDB.TransactionCommit();
					}
				}
			}
			catch (Exception ex)
			{
				_cDB.TransactionRollBack();
                (new Logger()).WriteWarning("catch-1:  " + ex); 
            }

			int nRetVal = 0;
			int nCount = 0;
			int nTimeout = _cDB.TimeoutGet();
			try
			{
				_cDB.TimeoutSet(60 * 15); //15 минут
				while (true)
				{
					_cDB.TransactionBegin();
					nCount = _cDB.GetValueInt("SELECT `nValue` FROM archive.`fPlaylistArchive`(10)");
					_cDB.TransactionCommit();
					//(new Logger()).WriteNotice("fPlaylistArchive:" + nCount);
					if (1 > nCount)
						break;
					nRetVal += nCount;
				}
				while (true)
				{
					_cDB.TransactionBegin();
					nCount = _cDB.GetValueInt("SELECT count(*) as `nQty` FROM (SELECT archive.`fHouseKeepingArchive`(id) FROM archive.`vHouseKeepingDeleted` LIMIT 100) sq");
					_cDB.TransactionCommit();
					//(new Logger()).WriteNotice("fHouseKeepingArchive:" + nCount);
					if (1 > nCount)
						break;
					nRetVal += nCount;
				}
				_cDB.TransactionBegin();
				_cDB.Perform("DELETE FROM hk.`tHouseKeeping` WHERE id IN (SELECT h.id FROM hk.`tHouseKeeping` h LEFT JOIN hk.`tDTEvents` d ON h.id = d.`idHouseKeeping` WHERE d.id is null)");
				_cDB.TransactionCommit();
				//(new Logger()).WriteNotice("tHouseKeeping");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				_cDB.TransactionRollBack();
			}
			_cDB.TimeoutSet(nTimeout);
			return nRetVal;
		}
		public void MessagesArchive()
		{
			_cDB.Perform("SELECT * FROM archive.`fMessages`()");
		}
	}
}

