using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.media;
using helpers.replica.pl;

using g = globalization;
using helpers.replica.tsr;

namespace webservice.services
{
	public class PlaylistImport
	{
		private class RotateInfo
		{
			private List<Asset> _aAssets;
			private int _cCurrentIndex;

            public RotateInfo(IEnumerable<Asset> iCollection)
            {
                _aAssets = new List<Asset>(iCollection);
                _cCurrentIndex = 0;
            }
            public Asset Next()
            {
                lock (_aAssets)
                    return _aAssets[(_cCurrentIndex = (_cCurrentIndex >= _aAssets.Count - 1 ? 0 : _cCurrentIndex + 1))];
            }
            public void Add(Asset cAsset)
            {
                lock (_aAssets)
                    _aAssets.Add(cAsset);
            }
            public void Randomize()
            {
                lock (_aAssets)
                {
                    if (_aAssets.Count < 2)
                        return;
                    Random cR = new Random();
                    List<Asset> aRandomAssets = new List<Asset>();
                    Asset cAsset;
                    int nRandomIndex;
                    while (_aAssets.Count > 0)
                    {
                        nRandomIndex = cR.Next(0, _aAssets.Count - 1);
                        cAsset = _aAssets[nRandomIndex];
                        _aAssets.RemoveAt(nRandomIndex);
                        aRandomAssets.Add(cAsset);
                    }
                    _aAssets = aRandomAssets;
                }
            }
		}
		public class XLSRow
		{
			public string sID;
			public TimeSpan tsTime;
            public long nFramesQty;
			public string sText;
			public string sType;
			public string sCover;
		}

		public string[] aMessages
		{
			get
			{
				string sDelimeter = Environment.NewLine + " ";
				return _ahMessages.Select(o => Environment.NewLine + o.Key + (1 > o.Value.Count ? "" : sDelimeter + string.Join(sDelimeter, o.Value.Distinct()) + Environment.NewLine)).ToArray();
			}
		}
		public string sLog;

		private Dictionary<string, List<string>> _ahMessages;
		private Dictionary<string, RotateInfo> _ahBumpers;
		Dictionary<string, Asset> _ahVIBinds;
        private Queue<Asset> _ahAllAssets;
		private webservice.DBInteract _cDBI;

		public PlaylistImport(webservice.DBInteract cDBI)
		{
			_ahMessages = new Dictionary<string, List<string>>();
			_cDBI = cDBI;
		}

		private void MessageAdd(string sKey)
		{
			MessageAdd(sKey, (string)null);
		}
		private void MessageAdd(string sKey, IEnumerable<string> aValues)
		{
			if (!_ahMessages.ContainsKey(sKey))
				_ahMessages.Add(sKey, new List<string>(aValues));
			else
				_ahMessages[sKey].AddRange(aValues);
		}
		private void MessageAdd(string sKey, string sValue)
		{
			if (!_ahMessages.ContainsKey(sKey))
				_ahMessages.Add(sKey, new List<string>());
			if (null != sValue)
				_ahMessages[sKey].Add(sValue);
		}
		private Asset VIMarkGet()
		{
			Asset cRetVal = new Asset();
			cRetVal.nID = -2;
			cRetVal.sName = "vi:";
			return cRetVal;
		}
		private Asset IgnoredHardStartMarkGet()
		{
			Asset cRetVal = new Asset();
			cRetVal.nID = -3;
			cRetVal.sName = "!";
			return cRetVal;
		}
		private Asset BlockEdgeMarkGet()
		{
			Asset cRetVal = new Asset();
			cRetVal.nID = -4;
			cRetVal.sName = "#";
			return cRetVal;
		}

		private Asset RotateBumper(String sMask)
		{
			if (null == _ahBumpers)
				_ahBumpers = new Dictionary<string, RotateInfo>();
			if (!_ahBumpers.ContainsKey(sMask))
			{
				return null; // to speed up parsing this part was moved to the beginning of parse


				Queue<Asset> aqAssets = null;
				if (null != (aqAssets = _cDBI.AssetsGet(sMask, DBInteract.SearchMask.starts)) && 0 < aqAssets.Count)
				{
					RotateInfo cRotateInfo = new RotateInfo(aqAssets);
					_ahBumpers.Add(sMask, cRotateInfo);
				}
				else
				{
					MessageAdd(g.Webservice.sErrorPLImport1, sMask);
					return null;
				}
			}
			return _ahBumpers[sMask].Next();
		}

		public Queue<Asset> PowerGoldFileParse(string sFile)
		{
			Queue<Asset> aqRetVal = null;
			if (null != sFile && System.IO.File.Exists(sFile))
			{
				string sFileLine = "";
				long nID = -1;
				int nStart, nStop;
				Dictionary<long, Asset> ahBinds;

				if (webservice.Preferences.bPowerGoldIDsAreAssetIDs)   // это использовалось когда в ПГ-файле стояли айди ассетов (старый эфир), а не кастом вэльюз, как сейчас (hd эфир) (из-за старой базы - PG уже привязан к ней)
                {
					ahBinds = new Dictionary<long, Asset>();
					if (null == _ahAllAssets)
						_ahAllAssets = _cDBI.AssetsGet();
					foreach (Asset cAsset in _ahAllAssets) //new CustomValue("beep_id", null)
					{
						try
						{
							//nID = cAsset.aCustomValues[0].sValue.ToID();
							ahBinds.Add(cAsset.nID, cAsset);
						}
						catch(Exception ex)
                        {
                            (new Logger()).WriteError("foreach: ", ex);
                        }
					}
				}
				else
				{
					ahBinds = _cDBI.PGAssetsResolvedGet();
				}


				aqRetVal = new Queue<Asset>();
				System.IO.StreamReader cSR = new System.IO.StreamReader(sFile, System.Text.Encoding.GetEncoding(1251));
				int nLine = 0;
				while (null != (sFileLine = cSR.ReadLine()))
				{
					nLine++;
					if (1 > sFileLine.Length)
						continue;
                    nStart = 0;
                    for (int ni = 0; ni < webservice.Preferences.nColumnWithPGIds; ni++)
                    {
                        nStart = sFileLine.IndexOf(';') + 1;
                    }

                    if (0 > webservice.Preferences.nColumnWithPGIds && webservice.Preferences.nColumnWithPGIds >= nStart)
                    {
						MessageAdd(g.Webservice.sErrorPLImport2, nLine + ": " + sFileLine);
						continue;
					}
					nStop = sFileLine.IndexOf(';', nStart);
					if (3 > nStop || nStop - nStart < 1)
					{
						MessageAdd(g.Webservice.sErrorPLImport2, nLine + ": " + sFileLine);
						continue;
					}
					try
					{
						nID = sFileLine.Substring(nStart, nStop - nStart).ToID();
					}
					catch
					{
						nID = -1;
					}
					if (1 > nID)
					{
						MessageAdd(g.Webservice.sErrorPLImport2, nLine + ": " + sFileLine);
						continue;
					}
					if (ahBinds.ContainsKey(nID) && null != ahBinds[nID] && null != ahBinds[nID].cFile)
					{
						if (ahBinds[nID].cFile.eError != helpers.replica.Error.no)
						{
							MessageAdd(g.Webservice.sErrorPLImport21.Fmt(ahBinds[nID].cFile.eStatus.ToString(), ahBinds[nID].cFile.eError.ToString(), ahBinds[nID].cFile.sFile));
							continue;
                        }
						aqRetVal.Enqueue(ahBinds[nID]);
					}
					else
						MessageAdd(g.Webservice.sErrorPLImport3, nID.ToStr());
				}
				if (null == aqRetVal || 1 > aqRetVal.Count)
					throw new Exception(g.Webservice.sErrorPLImport4);
			}
			else
				throw new Exception(g.Webservice.sErrorPLImport5 + ": " + sFile);
			return aqRetVal;
		}
		public Queue<XLSRow> RowsGet(Queue<List<string>> aqExcelValues)
		{
			Queue<XLSRow> cRetVal = new Queue<XLSRow>();
			XLSRow cCurrent;
			List<string> aRow = null;
			List<string> aSIDs = new List<string>();
			string sTime, sText, sID, sType, sCover, sDur;
            int nDay = 0, nLastHour = 0;
			TimeSpan ts;
            long nDurSec;
			while (0 < aqExcelValues.Count)
			{
				aRow = aqExcelValues.Dequeue();
				if (3 > aRow.Count)
					continue;
				sID = aRow[2].Trim();
				if (1 > sID.Length)
					continue;
				if (g.Webservice.sNoticePLImport2 == sID)
					continue;

				ts = TimeSpan.MinValue;
				sTime = aRow[0].Trim();
				sText = aRow[1].Trim();
                sDur = aRow[3].Trim();
				sType = aRow[4].Trim().ToLower();
				sCover = aRow[12].Trim().ToLower();

				if (0 < sTime.Length)
				{
					try
					{
						ts = DateTime.Parse(sTime).TimeOfDay;
                        if (ts.Hours < nLastHour && nLastHour - ts.Hours > 10)
                        {
                            nDay++;
                        }
                        nLastHour = ts.Hours;
                        ts = ts.Add(new TimeSpan(nDay, 0, 0, 0));
                    }
					catch
					{
						continue;
					}

					if (1 > sText.Length)
						throw new Exception(g.Webservice.sErrorPLImport6 + sTime);
				}
                nDurSec = long.TryParse(sDur, out nDurSec) ? nDurSec : 0;
                cCurrent = new XLSRow() { sID = sID, tsTime = ts, sType = sType, sCover = sCover, sText = sText, nFramesQty = nDurSec * 25 };
				cRetVal.Enqueue(cCurrent);

				if (!_ahVIBinds.ContainsKey(sID))
					aSIDs.Add(sID);
			}
			AddAdvertsBySIDs(aSIDs);
			return cRetVal;
		}
		public VIPlaylist VideoInternationalFileParse(string sFile)
		{
			sLog = "VI LOG\n";
			Dictionary<string, Class> ahClasses = _cDBI.ClassesGet().ToDictionary(o => o.sName, o => o);
			VIPlaylist cRetVal = null;
			if (null != sFile && System.IO.File.Exists(sFile))
			{
				Queue<List<string>> aqExcelValues = Excel.GetValuesFromExcel(sFile);

				_ahVIBinds = new Dictionary<string, Asset>();
				foreach (Asset cAsset in _cDBI.AssetsGet(new CustomValue("vi_id", null)))
				{
					try
					{
						_ahVIBinds.Add(cAsset.aCustomValues[0].sValue, cAsset);
					}
					catch { }
				}

				cRetVal = new VIPlaylist();
				TimeSpan tsBlockTime = TimeSpan.MinValue;
				TimeSpan tsPrevBlockTime = TimeSpan.MinValue;
				int nBlockExcelStart = 0;
				int nBlockAdvertisementStart = 0;

				List<string> aMissedIDs = new List<string>();
                Dictionary<string, string> ahDifferentDurations = new Dictionary<string, string>();
                XLSRow cRow;
				// берем заранее все sIDs из TSR, иначе таймауты идут при множественных обращениях
				Queue<XLSRow> ahRows = RowsGet(aqExcelValues);


				while (0 < ahRows.Count)
				{
					cRow = ahRows.Dequeue();
					if (TimeSpan.MinValue < cRow.tsTime)
					{

						if (1 > cRow.sText.Length)
							throw new Exception(g.Webservice.sErrorPLImport6 + cRow.tsTime);
						try
						{
							nBlockExcelStart = cRow.tsTime.Minutes;
							nBlockAdvertisementStart = 0;
							for (int nMinute = 0; 60 > nMinute; nMinute += 20)
							{
								if (Math.Abs(nMinute - nBlockExcelStart) < 10 && nBlockAdvertisementStart < nMinute)
									nBlockAdvertisementStart = nMinute;
							}
							tsPrevBlockTime = tsBlockTime;
							tsBlockTime = new TimeSpan(cRow.tsTime.Days, cRow.tsTime.Hours, nBlockAdvertisementStart, 0);
							if (tsPrevBlockTime != tsBlockTime)
                            {
                                while (tsBlockTime < tsPrevBlockTime)   // || cRetVal.DoesBlockExist(tsBlockTime)
                                    tsBlockTime = tsBlockTime.Add(new TimeSpan(0, 1, 0, 0));
                            }
                            if (!cRetVal.DoesBlockExist(tsBlockTime, cRow.sType, cRow.sCover))
                                cRetVal.BlockAdd(tsBlockTime, cRow.sType, cRow.sCover);
                        }
                        catch
                        {
							throw new Exception(g.Webservice.sErrorPLImport7 + cRow.tsTime);
						};
					}

					if (1 > cRow.sID.Length)
						throw new Exception(g.Webservice.sErrorPLImport8.Fmt((0 < tsBlockTime.Days ? tsBlockTime.Days + g.Helper.sDays : ""), tsBlockTime.Hours, tsBlockTime.Minutes));
                    if (_ahVIBinds.ContainsKey(cRow.sID))
                    {
                        cRetVal.BlockLastAssetAdd(_ahVIBinds[cRow.sID], ahClasses);
                        if (_ahVIBinds[cRow.sID].nFramesQty != cRow.nFramesQty && !ahDifferentDurations.ContainsKey(cRow.sID))
                            ahDifferentDurations.Add(cRow.sID, "[xls=" + cRow.sText + "][time=" + cRow.tsTime.ToString("hh\\:mm\\:ss") + "][SCode=" + cRow.sID + "][dur_tsr=" + cRow.nFramesQty.ToFramesString(true, false, false, false, false, false) + "] -- [asset=" + _ahVIBinds[cRow.sID].sName + "][dur_asset=" + _ahVIBinds[cRow.sID].nFramesQty.ToFramesString(true, false, false, false, false, false) + "][file=" + _ahVIBinds[cRow.sID].cFile.sFilename + "]");
                    }
                    else
                    {
                        aMissedIDs.Add(cRow.sID);
                    }
				}
				if (0 < aMissedIDs.Count)
					MessageAdd(g.Webservice.sErrorPLImport9, aMissedIDs);
                if (0 < ahDifferentDurations.Count)
                    MessageAdd(g.Webservice.sErrorPLImport33, ahDifferentDurations.Values);
                if (null == cRetVal || 1 > cRetVal.nBlocksQty)
					throw new Exception(g.Webservice.sErrorPLImport10);
			}
			else
				throw new Exception(g.Common.sErrorFileNotFound.ToLower() + ":" + sFile);
			return cRetVal;
		}
		private void AddAdvertsBySIDs(List<string> aSIDs)
		{
			List<TSRItem> aTSRI;
			try
			{
				aTSRI = TSRItem.ItemsGetBySCodes(webservice.Preferences.sTSRConnection, aSIDs);
				foreach (TSRItem cI in aTSRI)
				{
					AddAdvertBySID(cI);
				}
			}
			catch (Exception ex)
			{
				MessageAdd(ex.Message);
				WebServiceError.Add(ex);
			}
		}
		private bool AddAdvertBySID(TSRItem cTSRI)
		{

			Advertisement cAsset;
			File cFile;
			Class[] aClasses = null;
			long nFrQty = 0;
			string sLogInfo = "";
			
			try
			{
				sLogInfo = cTSRI.sS_Code + "__" + cTSRI.sVI_Code + "__" + cTSRI.sName;
				if (cTSRI.sVI_Code == "")
				{
					MessageAdd(g.Webservice.sErrorPLImport25 + " [" + sLogInfo + "]");
					return false;
				}
				if (cTSRI.sVI_Code.Substring(0, 1) == "\\" || cTSRI.sVI_Code.Substring(0, 1) == "/")  // исправление чел фактора при копировании 
					cTSRI.sVI_Code = cTSRI.sVI_Code.Substring(1);
				if (cTSRI.sVI_Code.Substring(cTSRI.sVI_Code.Length - 1, 1) == "\"")   // исправление чел фактора при копировании 
					cTSRI.sVI_Code = cTSRI.sVI_Code.Substring(0, cTSRI.sVI_Code.Length - 1);
				if (cTSRI.eType == TSRItem.Type.NULL)
				{
					MessageAdd(g.Webservice.sErrorPLImport26 + " [" + sLogInfo + "]");
					return false;
				}

				if (cTSRI.eType == TSRItem.Type.СЕТЬ) //  "сеть" - с логотипом
					aClasses = _cDBI.ClassesGet("`sName` in ('advertisement_with_logo')").ToArray();
				else if (cTSRI.eType == TSRItem.Type.МОСКВА)
					aClasses = _cDBI.ClassesGet("`sName` in ('advertisement_without_logo')").ToArray();
				else
				{
					MessageAdd(g.Webservice.sErrorPLImport29 + " [" + sLogInfo + "]");
					return false;
				}

				if (!webservice.Preferences.bMakeAdvertAsset)   // новая схема, т.е. ассет рекламы есть и добавляем инфу в ассет. хр-ж уже есть
				{
					cAsset = _cDBI.AssetGetByVIID(cTSRI.sVI_Code);
					if (null == cAsset || cAsset.cFile.eError != helpers.replica.Error.no)
					{
						MessageAdd(g.Webservice.sErrorPLImport27 + " [" + sLogInfo + "]");
						return false;
					}
					sLogInfo += " [asset="+ cAsset.sName + "][fileid=" + cAsset.cFile.nID + "][filename=" + cAsset.cFile.sFilename + "]";
					cAsset.aClasses = aClasses;
					cAsset.aCustomValues = new CustomValue[] { new CustomValue("vi_id", cTSRI.sS_Code.ToUpper()) };
				}
				else // старая схема, т.е. ассета нет и ищем по файлу и добавляем ассет
				{
					cFile = _cDBI.FileGetByVIID(cTSRI.sVI_Code);
					if (null == cFile || cFile.eError != helpers.replica.Error.no)
					{
						MessageAdd(g.Webservice.sErrorPLImport27 + " [" + sLogInfo + "]");
						return false;
					}
					sLogInfo += " [fileid=" + cFile.nID + "][filename=" + cFile.sFilename + "]";
					long nQueryID;
					if (0 >= (nQueryID = _cDBI.FileDurationQuery(cFile.nID)))
					{
						MessageAdd(g.Webservice.sErrorPLImport30 + " [" + sLogInfo + "]");
						return false;
					}
					DateTime dtQuerryEnd = DateTime.Now.AddSeconds(30);
					helpers.IdNamePair cQuerryRes;
					while (dtQuerryEnd > DateTime.Now)
					{
						cQuerryRes = _cDBI.CommandStatusGet(nQueryID);
						if (null != cQuerryRes && cQuerryRes.sName == "succeed")
						{
							nFrQty = _cDBI.FileDurationResultGet(nQueryID);
							break;
						}
					}
					if (0 >= nFrQty)
					{
						MessageAdd(g.Webservice.sErrorPLImport31 + " [" + sLogInfo + "]");
						return false;
					}
					cAsset = new Advertisement()
					{
						aCustomValues = new CustomValue[] { new CustomValue("vi_id", cTSRI.sS_Code.ToUpper()) },
						cFile = cFile,
						nID = -1,
						sName = cTSRI.sName + "_" + cTSRI.sS_Code,
						stVideo = new Video(-1, cTSRI.sName + "_" + cTSRI.sS_Code, _cDBI.VideoTypeGet("advertisement")),
						aClasses = aClasses,
						nFrameIn = 1,
						nFrameOut = nFrQty,
						nFramesQty = nFrQty,
					};
				}
				_cDBI.AdvertisementSave(cAsset);
				sLog += sLogInfo + "\n";
				_ahVIBinds.Add(cTSRI.sS_Code, cAsset);
			}
			catch (Exception ex)
			{
				MessageAdd(g.Webservice.sErrorPLImport28 + " [" + sLogInfo + "]");
				WebServiceError.Add(ex);
				return false;
			}
			return true;
		}
		public Dictionary<TimeSpan, Queue<Asset>> DesignFileParse(string sFile)
		{
			Dictionary<TimeSpan, Queue<Asset>> ahRetVal = null;
			if (null != sFile && System.IO.File.Exists(sFile))
			{
				ahRetVal = new Dictionary<TimeSpan, Queue<Asset>>();
				Queue<List<string>> aqExcelValues =Excel.GetValuesFromExcel(sFile);
				List<string> aRow = null;
				Dictionary<string, Asset> ahAssetsNamesBinds = new Dictionary<string, Asset>();
				string[] aRotationsNames = aqExcelValues.Where(o => o[5].ToString().Trim() == "@").Select(o => o[1].ToString().Trim()).ToArray();
				string sRot;
				if (null == _ahBumpers)
					_ahBumpers = new Dictionary<string, RotateInfo>();
				if (null == _ahAllAssets)
					_ahAllAssets = _cDBI.AssetsGet();
				foreach (Asset cA in _ahAllAssets)
				{
					try
					{
						ahAssetsNamesBinds.Add(cA.sName, cA);

						if (null != (sRot = aRotationsNames.FirstOrDefault(o => cA.sName.StartsWith(o))))
						{
							if (!_ahBumpers.ContainsKey(sRot))
							{
								Queue<Asset> aqAssets = new Queue<Asset>();
								RotateInfo cRotateInfo = new RotateInfo(aqAssets);
								_ahBumpers.Add(sRot, cRotateInfo);
							}
							_ahBumpers[sRot].Add(cA);
						}
					}
					catch { }
				}
                foreach(string sBumper in _ahBumpers.Keys)
                {
                    _ahBumpers[sBumper].Randomize();
                }
                //if (null != aRotationsNames.Intersect(_ahBumpers.Keys))   //?
                //{
                //	foreach (string sErr in aRotationsNames.Intersect(_ahBumpers.Keys))
                //		MessageAdd(g.Webservice.sErrorPLImport1, sErr);
                //}


                Queue<Asset> aqDesignAssets = null;
				TimeSpan tsBlockTime = TimeSpan.MinValue, ts;
				TimeSpan tsPrevBlockTime = TimeSpan.MinValue;
				string sTime, sText, sNote, sText2;
				int nBlockExcelStart, nBlockAdvertisementStart;
				Asset cAsset = null;

				Asset cVIMark = VIMarkGet();
				Asset cIgnoredMark = IgnoredHardStartMarkGet();
				Asset cBlockEdgeMark = BlockEdgeMarkGet();
                int nLastHour = 0, nDay = 0;

                while (0 < aqExcelValues.Count)
				{
					aRow = aqExcelValues.Dequeue();
					if (6 > aRow.Count)
						continue;
					sTime = aRow[0].ToString().Trim();
					sText = aRow[1].ToString().Trim();
                    sText2 = aRow[3].ToString().Trim();
                    sNote = aRow[5].ToString().Trim();
					if (1 > sTime.Length && 1 > sText.Length && 1 > sNote.Length)
						continue;
					if ("Клипы" == sText)
						continue;
					if (sTime.ToLower().StartsWith(cVIMark.sName.ToLower()))
					{//поимели vi:[анонсы или рекламный блок]:[сеть или москва)
						if (TimeSpan.MinValue == tsBlockTime || null == aqDesignAssets)
							throw new Exception(g.Webservice.sErrorPLImport12);
						cVIMark.sName = sTime.ToLower().Substring(3);
						aqDesignAssets.Enqueue(cVIMark);
						cVIMark = VIMarkGet();
						continue;
					}
					else if (0 < sTime.Length)
					{
						try
						{
							ts = DateTime.Parse(sTime).TimeOfDay;
                            if (ts.Hours < nLastHour && nLastHour - ts.Hours > 10)
                            {
                                nDay++;
                            }
                            nLastHour = ts.Hours;
                            ts = ts.Add(new TimeSpan(nDay, 0, 0, 0));
                        }
                        catch
						{
							continue;
						}

						if (1 > sText.Length)
							throw new Exception(g.Webservice.sErrorPLImport6 + sTime);
						try
						{
							nBlockExcelStart = ts.Minutes;
							nBlockAdvertisementStart = 0;
							for (int nMinute = 0; 60 > nMinute; nMinute += 20)
							{
								if (Math.Abs(nMinute - nBlockExcelStart) < 10 && nBlockAdvertisementStart < nMinute)
									nBlockAdvertisementStart = nMinute;
							}
							tsPrevBlockTime = tsBlockTime;
							tsBlockTime = new TimeSpan(ts.Days, ts.Hours, nBlockAdvertisementStart, 0);


							if (tsPrevBlockTime != tsBlockTime)
							{
								while (tsBlockTime < tsPrevBlockTime || ahRetVal.ContainsKey(tsBlockTime))
									tsBlockTime = tsBlockTime.Add(new TimeSpan(1, 0, 0, 0));
							}
							if (tsPrevBlockTime == tsBlockTime)
								continue;
						}
						catch
						{
							throw new Exception(g.Webservice.sErrorPLImport7 + sTime);
						}

						if (null != aqDesignAssets && 0 < aqDesignAssets.Count)
						{
							ahRetVal[tsPrevBlockTime] = aqDesignAssets;
							aqDesignAssets = null;
						}
						aqDesignAssets = new Queue<Asset>();
						if (0 < sNote.Length && sNote.StartsWith(cIgnoredMark.sName))
							aqDesignAssets.Enqueue(cIgnoredMark);
						continue;
					}
					else if (null == aqDesignAssets)
						continue;

					if (1 > sText.Length)
						MessageAdd(g.Webservice.sErrorPLImport8.Fmt((0 < tsBlockTime.Days ? tsBlockTime.Days + g.Helper.sDays : ""), tsBlockTime.Hours, tsBlockTime.Minutes));
					cAsset = null;
					if (0 < sNote.Length && '@' == sNote[0])
					{
						cAsset = RotateBumper(sText);
						if (null == cAsset)
							MessageAdd(g.Webservice.sErrorPLImport1, sText);
					}
					else
					{
						try
						{
							if (0 < sNote.Length && cBlockEdgeMark.sName == sNote)
							{//получили границу блока "#"
								if (TimeSpan.MinValue == tsBlockTime || null == aqDesignAssets)
									throw new Exception(g.Webservice.sErrorPLImport12);
								aqDesignAssets.Enqueue(cBlockEdgeMark);
							}
							cAsset = ahAssetsNamesBinds[sText];
						}
						catch
						{
							MessageAdd(g.Webservice.sErrorPLImport14, sText);
						}
					}
					if (null == cAsset)
						continue;

                    if (!sText2.IsNullOrEmpty())
                        cAsset.oTag = ahAssetsNamesBinds.ContainsKey(sText2) ? ahAssetsNamesBinds[sText2] : null;
                    aqDesignAssets.Enqueue(cAsset);
				}
				if (null != aqDesignAssets)
					ahRetVal[tsBlockTime] = aqDesignAssets;

				if (null == ahRetVal || 1 > ahRetVal.Count)
					throw new Exception(g.Webservice.sErrorPLImport20);
			}
			else
				throw new Exception(g.Common.sErrorFileNotFound + ": " + sFile);
			return ahRetVal;
		}

		private List<PlaylistItem> MergedBlockGet(DateTime dtAdvertisementBind, TimeSpan tsDesignBlockStart, VIPlaylist cVIPL, Queue<Asset> ahDesignBlock, PlaylistItem cLastPLI, out PlaylistItem cNewLastPLI)
		{
			List<PlaylistItem> aRetVal = new List<PlaylistItem>();
			long nFramesQty = 0;
			PlaylistItem cPLI = new PlaylistItem();
            PlaylistItem cPLITmp;
            cNewLastPLI = null;
            bool bAdvertisementBlockStopped = false;
			string sVIBlockType, sVIBlockCover;
			Asset cVIMark = VIMarkGet();
			Asset cIgnoredMark = IgnoredHardStartMarkGet();
			Asset cBlockEdgeMark = BlockEdgeMarkGet();
			DateTime dtPLIStart, dtPLIStartPlanned;
			bool bIsTimeShiftIgnored = false;
			bool bBumperOutNeeded = false;
			string sBumperOutNameBeginning = "реклама выход";
			string sBumperInNameBeginning = "реклама вход";

			if (cIgnoredMark.nID != ahDesignBlock.Peek().nID)   // ! не стоит на начале блока - стартуем по дефалту
			{
				dtPLIStart = dtAdvertisementBind.Add(tsDesignBlockStart);
				if (0 < tsDesignBlockStart.Minutes)
					cPLI.dtStartSoft = dtPLIStart;
				else
					cPLI.dtStartHard = dtPLIStart;
				cPLI.dtStartPlanned = dtPLIStart;
				bAdvertisementBlockStopped = false;
			}
			else if (null != cLastPLI)
			{
				cPLI.dtStartSoft = cLastPLI.dtStartHardSoft.AddSeconds(1);
				cPLI.dtStartPlanned = cLastPLI.dtStartPlanned.AddMilliseconds(cLastPLI.nFramesQty * 40);
				ahDesignBlock.Dequeue();
				bIsTimeShiftIgnored = true;
			}
			else
				throw new Exception(g.Webservice.sErrorPLImport22);

			while (0 < ahDesignBlock.Count)
			{
				cPLI.cAsset = ahDesignBlock.Dequeue();
				cPLI.sName = cPLI.cAsset.sName;
				cPLI.nFramesQty = cPLI.cAsset.nFramesQty;
				if (cPLI.cAsset.nID > 0 && !bBumperOutNeeded && cPLI.sName.Replace("  ", " ").ToLower().StartsWith(sBumperOutNameBeginning))
					continue;
				if (cVIMark.nID == cPLI.cAsset.nID)
				{
					//анонсы:сеть
					sVIBlockType = cPLI.cAsset.sName.Substring(0, cPLI.cAsset.sName.IndexOf(':'));
					sVIBlockCover = cPLI.cAsset.sName.Substring(sVIBlockType.Length + 1);
					if (cVIPL.DoesBlockExist(tsDesignBlockStart, sVIBlockType, sVIBlockCover))
					{
						try
						{
							while (0 < cVIPL.BlockAssetsQtyGet(tsDesignBlockStart, sVIBlockType, sVIBlockCover))
							{
								bBumperOutNeeded = true;
								cPLI.cAsset = cVIPL.BlockAssetDequeue(tsDesignBlockStart, sVIBlockType, sVIBlockCover);
								if (null == cPLI.cAsset)
									continue;
                                if (sVIBlockType == "спонсор часа" && aRetVal.Count > 1 && (cPLITmp = aRetVal[aRetVal.Count - 1]).cAsset.oTag != null && cPLITmp.cAsset.oTag is Asset)
                                {
                                    nFramesQty -= cPLITmp.nFramesQty;
                                    cPLI.dtStartPlanned = cPLI.dtStartPlanned.AddMilliseconds(-cPLITmp.nFramesQty * 40);
                                    cPLITmp.cAsset = (Asset)cPLITmp.cAsset.oTag;
                                    cPLITmp.sName = cPLITmp.cAsset.sName;
                                    cPLITmp.nFramesQty = cPLITmp.cAsset.nFramesQty;
                                    cPLI.dtStartPlanned = cPLI.dtStartPlanned.AddMilliseconds(cPLITmp.nFramesQty * 40);
                                    CheckFileIsOk(cPLITmp.cAsset.cFile);
                                    nFramesQty += cPLITmp.nFramesQty;
                                }
								cPLI.sName = cPLI.cAsset.sName;
								cPLI.nFramesQty = cPLI.cAsset.nFramesQty;
								dtPLIStart = cPLI.dtStartHardSoft.AddSeconds(1);
								dtPLIStartPlanned = cPLI.dtStartPlanned.AddMilliseconds(cPLI.nFramesQty * 40);
								if (0 < cPLI.nFramesQty)
								{
									if (5250 < cPLI.nFramesQty)
									{
										MessageAdd(g.Webservice.sErrorPLImport15, cPLI.sName);
										return null;
									}
									CheckFileIsOk(cPLI.cAsset.cFile);
									if (!bAdvertisementBlockStopped)
										nFramesQty += cPLI.nFramesQty;
									aRetVal.Add(cPLI);
								}
								else
									MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
								cPLI = new PlaylistItem();
								cPLI.dtStartSoft = dtPLIStart;
								cPLI.dtStartPlanned = dtPLIStartPlanned;
							}
						}
						catch
						{
							throw new Exception(g.Webservice.sErrorPLImport17.Fmt((0 < tsDesignBlockStart.Days ? tsDesignBlockStart.Days + g.Helper.sDays : ""), tsDesignBlockStart.Hours, tsDesignBlockStart.Minutes));
						}
					}
					else
					{
						if (0 < aRetVal.Count && aRetVal[aRetVal.Count - 1].sName.Replace("  ", " ").ToLower().StartsWith(sBumperInNameBeginning))
						{
							cPLI.dtStartHard = aRetVal[aRetVal.Count - 1].dtStartHard;
							cPLI.dtStartSoft = aRetVal[aRetVal.Count - 1].dtStartSoft;
							cPLI.dtStartPlanned = aRetVal[aRetVal.Count - 1].dtStartPlanned;
							nFramesQty -= aRetVal[aRetVal.Count - 1].nFramesQty;
                            aRetVal.RemoveAt(aRetVal.Count - 1);
						}
						continue;
						//раньше давали ошибку, а теперь просто игнорим. Это "удаление кармана" в файле оформления
						//throw new Exception(g.Webservice.sErrorPLImport18.Fmt((0 < tsDesignBlockStart.Days ? tsDesignBlockStart.Days + g.Helper.sDays : ""), tsDesignBlockStart.Hours, tsDesignBlockStart.Minutes));
					}
				}
				else if (cBlockEdgeMark.nID == cPLI.cAsset.nID)
				{
					bAdvertisementBlockStopped = true;
				}
				else
				{
                    if (0 < aRetVal.Count 
                        && aRetVal[aRetVal.Count - 1].sName.Replace("  ", " ").ToLower().StartsWith(sBumperOutNameBeginning) 
                        && cPLI.sName.Replace("  ", " ").ToLower().StartsWith(sBumperOutNameBeginning))
                    {
                        continue;
                    }
					if (0 < cPLI.nFramesQty)
					{
						CheckFileIsOk(cPLI.cAsset.cFile);
						if (!bAdvertisementBlockStopped)
							nFramesQty += cPLI.nFramesQty;
						aRetVal.Add(cPLI);
					}
					else
						MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
					dtPLIStart = cPLI.dtStartHardSoft.AddSeconds(1);
					dtPLIStartPlanned = cPLI.dtStartPlanned.AddMilliseconds(cPLI.nFramesQty * 40);
					cPLI = new PlaylistItem();
					cPLI.dtStartSoft = dtPLIStart;
                    cPLI.dtStartPlanned = dtPLIStartPlanned;
				}
			}

			if (aRetVal.Count <= 0)
				return null;

			cNewLastPLI = aRetVal[aRetVal.Count - 1];

			if (bIsTimeShiftIgnored || 0 == nFramesQty)
				return aRetVal;

			TimeSpan tsBlockDuration = TimeSpan.FromMilliseconds(nFramesQty * 40);
			for (int nIndx = 0; aRetVal.Count > nIndx; nIndx++)
			{
				if (DateTime.MaxValue > aRetVal[nIndx].dtStartHard)
					aRetVal[nIndx].dtStartHard = aRetVal[nIndx].dtStartHard.Subtract(tsBlockDuration);
				else
					aRetVal[nIndx].dtStartSoft = aRetVal[nIndx].dtStartSoft.Subtract(tsBlockDuration);
				aRetVal[nIndx].dtStartPlanned = aRetVal[nIndx].dtStartPlanned.Subtract(tsBlockDuration);
			}
			return aRetVal;
		}
		private bool CheckFileIsOk(File cFile)
		{
			if (cFile.eError!= helpers.replica.Error.no) // cFile.eStatus!= File.Status.InStock ||    estatus не передаётся в ассет через vAssetsResolved
			{
				MessageAdd(g.Webservice.sErrorPLImport32, cFile.sFilename);
				return false;
			}
			return true;
		}
		public List<PlaylistItem> PlaylistsMerge(Queue<Asset> aqPGPL, VIPlaylist cVIPL, DateTime dtAdvertisementBind, Dictionary<TimeSpan, Queue<Asset>> ahDsgnPL)
		{
			_ahAllAssets = null;
			List<PlaylistItem> aRetVal = null;
			if (null == aqPGPL || null == cVIPL || null == ahDsgnPL)
				throw new Exception(" can't find one of the intermediate PLs 2");
			aRetVal = new List<PlaylistItem>();
			PlaylistItem cPLI = new PlaylistItem();

			//dtAdvertisementBind = dtAdvertisementBind.Add(TimeSpan.FromMinutes(172));
			DateTime dtPLIStart = dtAdvertisementBind;
			PlaylistItem cLastPLI = null;
			#region adv
			TimeSpan tsBlockDuration = TimeSpan.Zero;
			TimeSpan tsClipDurationMinimumForCut = _cDBI.AdmClipDurationMinimumForCutGet();
			TimeSpan tsPLIDurationMinimum = _cDBI.AdmPLIDurationMinimumGet();
			File[] aPlugs = _cDBI.PlaylistPlugsGet();
			List<PlaylistItem> aBlock;

			foreach (TimeSpan tsDesignBlockStart in ahDsgnPL.Keys)
			{
				if (null != (aBlock = MergedBlockGet(dtAdvertisementBind, tsDesignBlockStart, cVIPL, ahDsgnPL[tsDesignBlockStart], cLastPLI,  out cLastPLI)))
				{
					aRetVal.AddRange(aBlock);
				}
			}

			foreach (IGrouping<TimeSpan, Asset[]> cKVP in cVIPL.AssetsUnusedGet())
				MessageAdd(g.Webservice.sNoticePLImport1.Fmt(cKVP.Key), cKVP.SelectMany(o => o).Select(o => o.nID + ":" + o.sName));

			//aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));
			#endregion
			#region clips

			//aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));

			dtPLIStart = dtAdvertisementBind;

			cPLI = null;
			PlaylistItem cPlug = null;
			TimeSpan tsDiff;
			int nAdvQty = aRetVal.Count;
			for (int nIndx = 1; nAdvQty > nIndx; nIndx++)
			{
				dtPLIStart = aRetVal[nIndx - 1].dtStartPlanned.AddMilliseconds(aRetVal[nIndx - 1].nFramesQty * 40); //TODO FPS
				if (1 < aRetVal[nIndx].dtStartPlanned.Subtract(aRetVal[nIndx - 1].dtStartPlanned).TotalMinutes)
				{
					while (true)
					{
						if (1 > aqPGPL.Count || dtPLIStart >= aRetVal[nIndx].dtStartPlanned)
							break;
						if (null == cPLI)
						{
							cPLI = new PlaylistItem();
							cPLI.cAsset = aqPGPL.Dequeue();
						}
						if (null != cPLI.cAsset)
						{
							cPLI.sName = cPLI.cAsset.sName;
							cPLI.nFramesQty = cPLI.cAsset.nFramesQty;
							cPLI.dtStartPlanned = dtPLIStart;
							dtPLIStart = dtPLIStart.AddMilliseconds(cPLI.cAsset.nFramesQty * 40); //TODO FPS

							if (DateTime.MaxValue > aRetVal[nIndx].dtStartHard)
							{
								tsDiff = aRetVal[nIndx].dtStartHard.Subtract(cPLI.dtStartPlanned);
								if (aRetVal[nIndx].dtStartHard < dtPLIStart.Add(tsPLIDurationMinimum))
								{
									dtPLIStart = aRetVal[nIndx].dtStartHard;
									if (tsClipDurationMinimumForCut > tsDiff && null != aPlugs && 0 < aPlugs.Length)
									{
										cPlug = new PlaylistItem();
										cPlug.cFile = aPlugs[0];
										cPlug.bPlug = true;
										cPlug.sName = g.Helper.sPlug.ToLower();
										cPlug.nFramesQty = (int)tsDiff.TotalSeconds * 25; //TODO FPS
										cPlug.dtStartPlanned = cPLI.dtStartPlanned;
										aRetVal.Add(cPlug);
										continue;
									}
								}
							}
							if (0 < cPLI.nFramesQty)
							{
								CheckFileIsOk(cPLI.cAsset.cFile);
								aRetVal.Add(cPLI);
							}
							else
								MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
							cPLI = null;
						}
					}
					if (aRetVal[nIndx].dtStartPlanned > dtPLIStart)
					{
						MessageAdd(g.Webservice.sErrorPLImport19.Fmt(dtPLIStart.ToString("HH:mm:ss"), aRetVal[nIndx].dtStartPlanned.ToString("HH:mm:ss")));
						dtPLIStart = aRetVal[nIndx].dtStartPlanned;
					}
				}
				aRetVal[nIndx].dtStartPlanned = dtPLIStart;
			}
			if (null != cPLI)
			{
				List<Asset> aAss = aqPGPL.ToList();
				aAss.Insert(0, cPLI.cAsset);
				aqPGPL.Clear();
				foreach (Asset cAss in aAss)
					aqPGPL.Enqueue(cAss);
			}
			while (0 < aqPGPL.Count)
			{
				cPLI = new PlaylistItem();
				cPLI.cAsset = aqPGPL.Dequeue();
				cPLI.sName = cPLI.cAsset.sName;
				cPLI.nFramesQty = cPLI.cAsset.nFramesQty;
				cPLI.dtStartPlanned = dtPLIStart;
				dtPLIStart = dtPLIStart.AddMilliseconds(cPLI.cAsset.nFramesQty * 40); //TODO FPS
				if (0 < cPLI.nFramesQty)
				{
					CheckFileIsOk(cPLI.cAsset.cFile);
					aRetVal.Add(cPLI);
				}
				else
					MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
			}
			aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));
			#endregion

			return aRetVal;
		}
	}
}
