using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.media;
using helpers.replica.pl;

using g = globalization;

namespace webservice.services
{
	public class PlaylistImport
	{
		public class VIPlaylist
		{
			private class Block
			{
				public class Type
				{
					public string sName;
					public string sCover;
					static public bool operator ==(Type cLeft, Type cRight)
					{
						bool bRetVal = false;
						if (null == (object)cLeft) //привидение к object нужно, чтобы не было рекурсии
						{
							if (null == (object)cRight)
								return true;
							else
								return false;
						}
						else if (null == (object)cRight)
							return false;
						if (cLeft.sName == cRight.sName && cLeft.sCover == cRight.sCover)
							bRetVal = true;
						return bRetVal;
					}
					static public bool operator !=(Type cLeft, Type cRight)
					{
						return !(cLeft == cRight);
					}

					public Type(string sType, string sCover)
					{
						sName = sType.Trim(' ', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0').ToLower();
						this.sCover = sCover.ToLower();
					}
				}

				public TimeSpan tsStart;
				public Type cType;
				public Queue<Asset> aqAssets;

				public Block(TimeSpan tsStart, Type cType)
				{
					this.tsStart = tsStart;
					this.cType = cType;
					aqAssets = new Queue<Asset>();
				}
			}

			private List<Block> _aBlocks;
			private Block _cBlockLast;

			public int nBlocksQty
			{
				get
				{
					return _aBlocks.Count;
				}
			}

			public VIPlaylist()
			{
				_aBlocks = new List<Block>();
			}

			private bool DoesBlockExist(TimeSpan tsStart, Block.Type cType)
			{
				return (0 < _aBlocks.Count(o => o.tsStart == tsStart && (null == cType || o.cType == cType)));
			}
			public bool DoesBlockExist(TimeSpan tsStart, string sType, string sCover)
			{
				return DoesBlockExist(tsStart, new Block.Type(sType, sCover));
			}
			public bool DoesBlockExist(TimeSpan tsStart)
			{
				return DoesBlockExist(tsStart, null);
			}
			public void BlockAdd(TimeSpan tsStart, string sType, string sCover)
			{
				Block.Type cType = new Block.Type(sType, sCover);
				if (DoesBlockExist(tsStart, cType))
					throw new Exception("specified block already exists [" + tsStart.ToShort() + ":" + sType + ":" + sCover + "]");
				_cBlockLast = new Block(tsStart, cType);
				_aBlocks.Add(_cBlockLast);
			}
			private Block BlockGet(TimeSpan tsStart, string sType, string sCover)
			{
				return _aBlocks.FirstOrDefault(o => o.tsStart == tsStart && o.cType == new Block.Type(sType, sCover));
			}
			public int BlockAssetsQtyGet(TimeSpan tsStart, string sType, string sCover)
			{
				Block cBlock = BlockGet(tsStart, sType, sCover);
				if (null == cBlock)
					throw new Exception("specified block doesn't exist [" + tsStart.ToShort() + ":" + sType + ":" + sCover + "]");
				return cBlock.aqAssets.Count;
			}
			public void BlockLastAssetAdd(Asset cAsset)
			{
				if (null == _cBlockLast)
					throw new Exception("can't find last block");
				_cBlockLast.aqAssets.Enqueue(cAsset);
			}
			public Asset BlockAssetDequeue(TimeSpan tsStart, string sType, string sCover)
			{
				Block cBlock = BlockGet(tsStart, sType, sCover);
				if (null == cBlock)
					throw new Exception("specified block doesn't exist [" + tsStart.ToShort() + ":" + sType + ":" + sCover + "]");
				if (0 < cBlock.aqAssets.Count)
					return cBlock.aqAssets.Dequeue();
				return null;
			}
			public ILookup<TimeSpan, Asset[]> AssetsUnusedGet()
			{
				return _aBlocks.Where(o => o.aqAssets.Count > 0).ToLookup(k => k.tsStart, v => v.aqAssets.ToArray());
			}
		}
		private class RotateInfo
		{
			private LinkedList<Asset> _aAssets;
			private LinkedListNode<Asset> _cLLN;

			public RotateInfo(IEnumerable<Asset> iCollection)
			{
				_aAssets = new LinkedList<Asset>(iCollection);
			}
			public Asset Next()
			{
				return (_cLLN = (null == _cLLN || null == _cLLN.Next ? _aAssets.First : _cLLN.Next)).Value;
			}
		}

		public string[] aMessages
		{
			get
			{
				string sDelimeter = Environment.NewLine + " ";
				return _ahMessages.Select(o => Environment.NewLine + o.Key + (1 > o.Value.Count ? "" : sDelimeter + string.Join(sDelimeter, o.Value.Distinct()) + Environment.NewLine)).ToArray();
			}
		}

		private Dictionary<string, List<string>> _ahMessages;
		private Dictionary<string, RotateInfo> _ahBumpers;
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

		private Queue<List<string>> GetValuesFromExcel(string sFile)
		{
			Queue<List<string>> aqRetVal = null;

			java.io.InputStream inp = new java.io.FileInputStream(sFile);

			org.apache.poi.ss.usermodel.Workbook wb = (org.apache.poi.ss.usermodel.Workbook)org.apache.poi.ss.usermodel.WorkbookFactory.create(inp);
			org.apache.poi.ss.usermodel.Sheet sheet = wb.getSheetAt(0);
			int nRowsQty = sheet.getLastRowNum() + 1;
			int nCellsQty = 0;
			org.apache.poi.ss.usermodel.Row row = null;
			org.apache.poi.ss.usermodel.Cell cell = null;
			aqRetVal = new Queue<List<string>>();
			List<string> aRow = null;
			for (int nRowIndx = sheet.getFirstRowNum(); nRowsQty > nRowIndx; nRowIndx++)
			{
				if (null != (row = sheet.getRow(nRowIndx)))
				{
					nCellsQty = row.getLastCellNum() + 1;
					aRow = new List<string>();
					for (int nCellIndx = row.getFirstCellNum(); nCellsQty > nCellIndx; nCellIndx++)
					{
						if (null != (cell = row.getCell(nCellIndx)))
						{
							object cValue = null;
							switch (cell.getCellType())
							{
								case org.apache.poi.ss.usermodel.Cell.__Fields.CELL_TYPE_STRING:
									cValue = cell.getRichStringCellValue().getString();
									break;
								case org.apache.poi.ss.usermodel.Cell.__Fields.CELL_TYPE_NUMERIC:
									cValue = cell.getNumericCellValue();
									if (org.apache.poi.ss.usermodel.DateUtil.isCellDateFormatted(cell))
										cValue = DateTime.FromOADate((double)cValue).ToString("yyyy-MM-dd HH:mm:ss");
									break;
								case org.apache.poi.ss.usermodel.Cell.__Fields.CELL_TYPE_BOOLEAN:
									cValue = cell.getBooleanCellValue();
									break;
								case org.apache.poi.ss.usermodel.Cell.__Fields.CELL_TYPE_FORMULA:
									cValue = cell.getCellFormula();
									break;
								default:
									cValue = "";
									break;
							}
							aRow.Add(cValue.ToString());
						}
					}
					aqRetVal.Enqueue(aRow);
				}
			}
			inp.close();
			return aqRetVal;
		}
		private Asset RotateBumper(String sMask)
		{
			if (null == _ahBumpers)
				_ahBumpers = new Dictionary<string, RotateInfo>();
			if (!_ahBumpers.ContainsKey(sMask))
			{
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
				Dictionary<long, Asset> ahBinds = new Dictionary<long, Asset>();
				foreach (Asset cAsset in _cDBI.AssetsGet()) //new CustomValue("beep_id", null)
				{
					try
					{
						//nID = cAsset.aCustomValues[0].sValue.ToID();
						ahBinds.Add(cAsset.nID, cAsset);
					}
					catch { }
				}

				aqRetVal = new Queue<Asset>();
				System.IO.StreamReader cSR = new System.IO.StreamReader(sFile, System.Text.Encoding.GetEncoding(1251));
				int nLine = 0;
				while (null != (sFileLine = cSR.ReadLine()))
				{
					nLine++;
					if (1 > sFileLine.Length)
						continue;
					nStart = sFileLine.IndexOf(';') + 1;
					if (2 > nStart)
					{
						MessageAdd(g.Webservice.sErrorPLImport2, nLine + ": " + sFileLine);
						continue;
					}
					nStop = sFileLine.IndexOf(';', nStart);
					if (3 > nStop)
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
					if (ahBinds.ContainsKey(nID) && null != ahBinds[nID])
						aqRetVal.Enqueue(ahBinds[nID]);
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
		public VIPlaylist VideoInternationalFileParse(string sFile)
		{
			VIPlaylist cRetVal = null;
			if (null != sFile && System.IO.File.Exists(sFile))
			{
				Queue<List<string>> aqExcelValues = GetValuesFromExcel(sFile);

				Dictionary<string, Asset> ahVIBinds = new Dictionary<string, Asset>();
				foreach (Asset cAsset in _cDBI.AssetsGet(new CustomValue("vi_id", null)))
				{
					try
					{
						ahVIBinds.Add(cAsset.aCustomValues[0].sValue, cAsset);
					}
					catch { }
				}

				cRetVal = new VIPlaylist();
				List<string> aRow = null;
				TimeSpan tsBlockTime = TimeSpan.MinValue;
				TimeSpan tsPrevBlockTime = TimeSpan.MinValue;
				int nBlockExcelStart = 0;
				int nBlockAdvertisementStart = 0;
				string sTime, sText, sID, sType, sCover;
				TimeSpan ts;

				List<string> aMissedIDs = new List<string>();
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

					sTime = aRow[0].Trim();
					sText = aRow[1].Trim();
					sType = aRow[4].Trim().ToLower();
					sCover = aRow[12].Trim().ToLower();

					if (0 < sTime.Length)
					{
						try
						{
							ts = DateTime.Parse(sTime).TimeOfDay;
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
							tsBlockTime = new TimeSpan(ts.Hours, nBlockAdvertisementStart, 0);
							if (tsPrevBlockTime != tsBlockTime)
							{
								while (tsBlockTime < tsPrevBlockTime)   // || cRetVal.DoesBlockExist(tsBlockTime)
									tsBlockTime = tsBlockTime.Add(new TimeSpan(0, 1, 0, 0));
							}
							if (!cRetVal.DoesBlockExist(tsBlockTime, sType, sCover))
								cRetVal.BlockAdd(tsBlockTime, sType, sCover);
						}
						catch
						{
							throw new Exception(g.Webservice.sErrorPLImport7 + sTime);
						};
					}

					if (1 > sID.Length)
						throw new Exception(g.Webservice.sErrorPLImport8.Fmt((0 < tsBlockTime.Days ? tsBlockTime.Days + g.Helper.sDays : ""), tsBlockTime.Hours, tsBlockTime.Minutes));
					if (ahVIBinds.ContainsKey(sID))
						cRetVal.BlockLastAssetAdd(ahVIBinds[sID]);
					else
						aMissedIDs.Add(sID);
				}
				if (0 < aMissedIDs.Count)
					MessageAdd(g.Webservice.sErrorPLImport9, aMissedIDs);
				if (null == cRetVal || 1 > cRetVal.nBlocksQty)
					throw new Exception(g.Webservice.sErrorPLImport10);
			}
			else
				throw new Exception(g.Common.sErrorFileNotFound.ToLower() + ":" + sFile);
			return cRetVal;
		}
		public Dictionary<TimeSpan, Queue<Asset>> DesignFileParse(string sFile)
		{
			Dictionary<TimeSpan, Queue<Asset>> ahRetVal = null;
			if (null != sFile && System.IO.File.Exists(sFile))
			{
				Dictionary<string, Asset> ahAssetsNamesBinds = new Dictionary<string, Asset>();
				foreach (Asset cA in _cDBI.AssetsGet())
				{
					try
					{
						ahAssetsNamesBinds.Add(cA.sName, cA);
					}
					catch { }
				}

				ahRetVal = new Dictionary<TimeSpan, Queue<Asset>>();
				Queue<List<string>> aqExcelValues = GetValuesFromExcel(sFile);
				List<string> aRow = null;

				Queue<Asset> aqDesignAssets = null;
				TimeSpan tsBlockTime = TimeSpan.MinValue, ts;
				TimeSpan tsPrevBlockTime = TimeSpan.MinValue;
				string sTime, sText, sNote;
				int nBlockExcelStart, nBlockAdvertisementStart;
				Asset cAsset = null;

				Asset cVIMark = VIMarkGet();
				Asset cIgnoredMark = IgnoredHardStartMarkGet();
				Asset cBlockEdgeMark = BlockEdgeMarkGet();

				while (0 < aqExcelValues.Count)
				{
					aRow = aqExcelValues.Dequeue();
					if (6 > aRow.Count)
						continue;
					sTime = aRow[0].ToString().Trim();
					sText = aRow[1].ToString().Trim();
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
							tsBlockTime = new TimeSpan(ts.Hours, nBlockAdvertisementStart, 0);


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

		public List<PlaylistItem> PlaylistsMerge(Queue<Asset> aqPGPL, VIPlaylist cVIPL, DateTime dtAdvertisementBind, Dictionary<TimeSpan, Queue<Asset>> ahDsgnPL)
		{
			List<PlaylistItem> aRetVal = null;
			if (null == aqPGPL || null == cVIPL || null == ahDsgnPL)
				throw new Exception("can't find one of the intermediate PLs");
			aRetVal = new List<PlaylistItem>();
			PlaylistItem cPLI = new PlaylistItem();

			//dtAdvertisementBind = dtAdvertisementBind.Add(TimeSpan.FromMinutes(172));
			DateTime dtPLIStart = dtAdvertisementBind;

			#region adv
			Asset cVIMark = VIMarkGet();
			Asset cIgnoredMark = IgnoredHardStartMarkGet();
			Asset cBlockEdgeMark = BlockEdgeMarkGet();

			TimeSpan tsBlockDuration = TimeSpan.Zero;
			PlaylistItem cBlockStartPLI = null;
			Dictionary<PlaylistItem, TimeSpan> ahBlockDurations = new Dictionary<PlaylistItem, TimeSpan>();
			bool bAdvertisementBlockStopped = false;
			string sVIBlockType, sVIBlockCover;
			TimeSpan tsClipDurationMinimumForCut = _cDBI.AdmClipDurationMinimumForCutGet();
			TimeSpan tsPLIDurationMinimum = _cDBI.AdmPLIDurationMinimumGet();
			File[] aPlugs = _cDBI.PlaylistPlugsGet();

			foreach (TimeSpan tsDesignBlockStart in ahDsgnPL.Keys)
			{
				if (cIgnoredMark.nID != ahDsgnPL[tsDesignBlockStart].Peek().nID)
				{
					dtPLIStart = dtAdvertisementBind.Add(tsDesignBlockStart);
					if (0 < tsDesignBlockStart.Minutes)
						cPLI.dtStartSoft = dtPLIStart;
					else
						cPLI.dtStartHard = dtPLIStart;
					cPLI.dtStartPlanned = dtPLIStart;
					if (null != cBlockStartPLI)
					{
						ahBlockDurations.Add(cBlockStartPLI, tsBlockDuration);
						tsBlockDuration = TimeSpan.Zero;
					}
					cBlockStartPLI = cPLI;
					bAdvertisementBlockStopped = false;
				}
				else
					ahDsgnPL[tsDesignBlockStart].Dequeue();
				while (0 < ahDsgnPL[tsDesignBlockStart].Count)
				{
					cPLI.cAsset = ahDsgnPL[tsDesignBlockStart].Dequeue();
					cPLI.sName = cPLI.cAsset.sName;
					cPLI.nFramesQty = cPLI.cAsset.nFramesQty;
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
									cPLI.cAsset = cVIPL.BlockAssetDequeue(tsDesignBlockStart, sVIBlockType, sVIBlockCover);
									if (null == cPLI.cAsset)
										continue;
									cPLI.sName = cPLI.cAsset.sName;
									cPLI.nFramesQty = cPLI.cAsset.nFramesQty;
									dtPLIStart = cPLI.dtStartPlanned;
									if (0 < cPLI.nFramesQty)
									{
										if (5250 < cPLI.nFramesQty)
										{
											MessageAdd(g.Webservice.sErrorPLImport15, cPLI.sName);
											return new List<PlaylistItem>();
										}
										if (!bAdvertisementBlockStopped)
											tsBlockDuration = tsBlockDuration.Add(TimeSpan.FromMilliseconds(cPLI.nFramesQty * 40)); //TODO FPS
										aRetVal.Add(cPLI);
									}
									else
										MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
									cPLI = new PlaylistItem();
									cPLI.dtStartPlanned = cPLI.dtStartSoft = dtPLIStart.AddSeconds(1);
								}
							}
							catch
							{
								throw new Exception(g.Webservice.sErrorPLImport17.Fmt((0 < tsDesignBlockStart.Days ? tsDesignBlockStart.Days + g.Helper.sDays : ""), tsDesignBlockStart.Hours, tsDesignBlockStart.Minutes));
							}
						}
						else
							throw new Exception(g.Webservice.sErrorPLImport18.Fmt((0 < tsDesignBlockStart.Days ? tsDesignBlockStart.Days + g.Helper.sDays : ""), tsDesignBlockStart.Hours, tsDesignBlockStart.Minutes));
					}
					else if (cBlockEdgeMark.nID == cPLI.cAsset.nID)
					{
						bAdvertisementBlockStopped = true;
					}
					else
					{
						if (0 < cPLI.nFramesQty)
						{
							if (!bAdvertisementBlockStopped)
								tsBlockDuration = tsBlockDuration.Add(TimeSpan.FromMilliseconds(cPLI.nFramesQty * 40)); //TODO FPS
							aRetVal.Add(cPLI);
						}
						else
							MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
						dtPLIStart = cPLI.dtStartPlanned;
						cPLI = new PlaylistItem();
						cPLI.dtStartSoft = dtPLIStart.AddSeconds(1);
						cPLI.dtStartPlanned = cPLI.dtStartSoft;
					}
				}
			}
			if (null != cBlockStartPLI)
			{
				ahBlockDurations.Add(cBlockStartPLI, tsBlockDuration);
			}

			foreach (IGrouping<TimeSpan, Asset[]> cKVP in cVIPL.AssetsUnusedGet())
				MessageAdd(g.Webservice.sNoticePLImport1.Fmt(cKVP.Key), cKVP.SelectMany(o => o).Select(o => o.nID + ":" + o.sName));


			aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));
			tsBlockDuration = TimeSpan.Zero;
			for (int nIndx = 0; aRetVal.Count > nIndx; nIndx++)
			{
				if (ahBlockDurations.ContainsKey(aRetVal[nIndx]))
					tsBlockDuration = ahBlockDurations[aRetVal[nIndx]];
				if (DateTime.MaxValue > aRetVal[nIndx].dtStartHard)
					aRetVal[nIndx].dtStartHard = aRetVal[nIndx].dtStartHard.Subtract(tsBlockDuration);
				else
					aRetVal[nIndx].dtStartSoft = aRetVal[nIndx].dtStartSoft.Subtract(tsBlockDuration);
				aRetVal[nIndx].dtStartPlanned = aRetVal[nIndx].dtStartPlanned.Subtract(tsBlockDuration);
			}
			#endregion
			#region clips

			aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));

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
								aRetVal.Add(cPLI);
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
					aRetVal.Add(cPLI);
				else
					MessageAdd(g.Webservice.sErrorPLImport16, cPLI.sName);
			}
			aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));
			#endregion

			return aRetVal;
		}
	}
}
