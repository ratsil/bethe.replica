using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using helpers.extensions;
using helpers.replica.services.dbinteract;
using g = globalization;
using controls.childs.sl;

namespace replica.sl
{
	public class UI_Sizes
	{
		static public double GetPossibleHeightOfElementInAssetView()
		{
			double nH = App.Current.RootVisual.RenderSize.Height - 185;
			nH = nH > 150 ? nH : 150;
			return nH;
		}
		static public double GetPossibleHeightOfElementInPlaylistView_Single()
		{
			double nH = App.Current.RootVisual.RenderSize.Height - 245;
			nH = nH > 150 ? nH : 150;
			return nH;
		}
		static public double GetPossibleHeightOfElementInPlaylistView_Default()
		{
			return 400;
		}
		static public double GetPossibleHeightOfPlaylistScrollViewer()
		{
			double nH = App.Current.RootVisual.RenderSize.Height - 80;
			nH = nH > 150 ? nH : 150;
			return nH;
		}
		static public double GetPossibleHeightOfElementInSMSView_Single()
		{
			double nH = App.Current.RootVisual.RenderSize.Height - 130;
			nH = nH > 150 ? nH : 150;
			return nH;
		}
		static public double GetPossibleHeightOfElementInArtistSearchView()
		{
			double nH = App.Current.RootVisual.RenderSize.Height - 110;
			nH = nH > 150 ? nH : 150;
			return nH;
		}
	}
	public class AssetSL : Asset
	{
		public string sDuration
		{
			get
			{
				if (0 == nFramesQty || 1 == nDuration)
					return "";
				else
					return nDuration.ToFramesString(true);
			}
			set { }
		}
		public long nDuration
		{
			get { return nFrameOut - nFrameIn + 1; }
			set { }
		}
		public helpers.replica.services.dbinteract.Cues cClipsCues
		{
			get;
			set;
		}
		public IdNamePair cRotation
		{
			get;
			set;
		}
		public string sRotationName
		{
			get
			{
				if (null != cRotation)
					return cRotation.sName;
				else
					return "";
			}
		}
		public string sCuesName
		{
			get
			{
				if (null != cClipsCues)
					return cClipsCues.sArtist + " : " + cClipsCues.sSong;
				else
					return "NULL";
			}
		}
		public string sFilename
		{
			get
			{
				if (null != cFile)
					return cFile.sFilename;
				else
				{
					string sFile = "no file";
					if (null != cType)
					{
						if (cType.eType == AssetType.series)
							sFile = g.Helper.sSeries;
						if (cType.eType == AssetType.episode)
							sFile = g.Helper.sEpisode;
						sFile += " (" + nChildsQty + ")";
					}
					return sFile;
				}
			}
		}
		public int nChildsQty
		{
			get; set;
		}
        public bool bChildError
        {
            get; set;
        }
        public string sVideoTypeName
		{
			get
			{
				if (0 <= stVideo.nID && null != stVideo.cType)
					return stVideo.cType.sName;
				else
					return "";
			}
		}
        public string sClassName
        {
            get
            {
                if (!aClasses.IsNullOrEmpty())
                {
                    if (aClasses.Length == 1)
                        return aClasses[0].sName;
                    else
                        return "(" + aClasses.Length + ") " + aClasses[0].sName;
                }
                else
                    return "";
            }
        }
        public static AssetSL GetAssetSL(Asset cAss)
		{
			AssetSL cRes = new AssetSL()
			{
				aCustomValues = cAss.aCustomValues,
				bEnabled = cAss.bEnabled,
                aClasses = cAss.aClasses,
                dtLastPlayed = cAss.dtLastPlayed,
				nFrameIn = cAss.nFrameIn,
				nFrameOut = cAss.nFrameOut,
				nFramesQty = cAss.nFramesQty,
				nID = cAss.nID,
				sName = cAss.sName,
				cFile = cAss.cFile,
				stVideo = cAss.stVideo,
				nIDParent = cAss.nIDParent,
				cType = cAss.cType
			};
			if (cAss is helpers.replica.services.dbinteract.Clip)
			{
				helpers.replica.services.dbinteract.Clip cClip;
				cClip = (helpers.replica.services.dbinteract.Clip)cAss;
				cRes.cClipsCues = cClip.stCues;
				cRes.cRotation = cClip.cRotation;
			}
			return cRes;
		}
		static public Asset GetAsset(AssetSL cAss)
		{
			return GetAsset(cAss, false);
		}
		static public Asset GetAsset(AssetSL cAss, bool bClipsAsBase)
		{
			Asset cRes = new Asset();
			if (!bClipsAsBase && (cAss.cRotation != null || cAss.cClipsCues != null))
			{
				cRes = new Clip();
				((Clip)cRes).stCues = cAss.cClipsCues;
				((Clip)cRes).cRotation = cAss.cRotation;
				((Clip)cRes).stSoundLevels = new SoundLevels();
			}
			cRes.aCustomValues = cAss.aCustomValues;
			cRes.bEnabled = cAss.bEnabled;
            cRes.aClasses = cAss.aClasses;
            cRes.dtLastPlayed = cAss.dtLastPlayed;
			cRes.nFrameIn = cAss.nFrameIn;
			cRes.nFrameOut = cAss.nFrameOut;
			cRes.nFramesQty = cAss.nFramesQty;
			cRes.nID = cAss.nID;
			cRes.sName = cAss.sName;
			cRes.cFile = cAss.cFile;
			cRes.stVideo = cAss.stVideo;
			cRes.nIDParent = cAss.nIDParent;
			cRes.cType = cAss.cType;

			return cRes;
		}
		static public Asset[] GetArrayOfBases(AssetSL[] aAss)
		{
			Asset[] aRes = new Asset[aAss.Length];
			for (int ni = 0; aAss.Length > ni; ni++)
			{
				aRes[ni] = GetAsset(aAss[ni]);
			}
			return aRes;
		}
		static public AssetSL[] GetArrayOfAssetSLs(Asset[] aAss)
		{
			AssetSL[] aRes = new AssetSL[aAss.Length];
			for (int ni = 0; aAss.Length > ni; ni++)
			{
				aRes[ni] = GetAssetSL(aAss[ni]);
			}
			return aRes;
		}
	}
	public class PlaylistItemSL : PlaylistItem
	{
		public string sDuration
		{
			get
			{
				string sDelta = "";
				if (1 < nFrameStart || nFramesQty > nFrameStop)
					sDelta = " (" + (nFrameStart - 1 + nFramesQty - nFrameStop).ToFramesString(true) + ")";
				return nDuration.ToFramesString(true) + sDelta;
			}
			set { }
		}
		public long nDuration
		{
			get { return nFrameStop - nFrameStart + 1; }
			set { }
		}
		public DateTime dtStartHardSoftPlanned
		{
			get
			{
				return (DateTime.MaxValue > dtStartHard || DateTime.MaxValue > dtStartSoft ? DateTime.MaxValue > dtStartHard ? dtStartHard : dtStartSoft : dtStartPlanned);
			}
		}
		public DateTime dtStartHardSoft
		{
			get
			{
				return (DateTime.MaxValue > dtStartHard ? dtStartHard: dtStartSoft);
			}
			set
			{
				if (DateTime.MaxValue > dtStartHard)
					dtStartHard = value;
				else
					dtStartSoft = value;
			}
		}
		public DateTime dtStopPlanned
		{
			get
			{
				return dtStartPlanned.AddMilliseconds(nDuration * 40);
			}
		}
		public DateTime dtStart
		{
			get
			{
				return (DateTime.MaxValue == dtStartReal ? dtStartPlanned : dtStartReal);
			}
		}
		public string sdtStart
		{
			get { return dtStart.Date.ToString("yyyy-MM-dd") + "     " + dtStart.ToString("HH:mm:ss"); }
			set { }
		}
		public string sRotationName
		{
			get
			{
				if (cAsset is helpers.replica.services.dbinteract.Clip && null != ((helpers.replica.services.dbinteract.Clip)cAsset).cRotation)
					return ((helpers.replica.services.dbinteract.Clip)cAsset).cRotation.sName;
				else
					return "";
			}
			set { }
		}
        public string sClassName
        {
            get
            {
                if (!aClasses.IsNullOrEmpty())
                {
                    if (aClasses.Length == 1)
                        return aClasses[0].sName;
                    else
                        return "(" + aClasses.Length + ") " + aClasses[0].sName;
                }
                else
                    return "";
            }
        }
        public bool bIsInserted
		{
			get
			{
				if (DateTime.MinValue == dtTimingsUpdate || DateTime.MaxValue == dtTimingsUpdate)
					return true;
				else
					return false;
			}
		}
		static public PlaylistItemSL GetPlaylistItemSL(PlaylistItem cPLI)
		{
			PlaylistItemSL cRetVal = new PlaylistItemSL()
			{
				bCached = cPLI.bCached,
				bPlug = cPLI.bPlug,
				cAsset = cPLI.cAsset,
                aClasses = cPLI.aClasses,
                cStatus = cPLI.cStatus,
				dtStartHard = cPLI.dtStartHard,
				dtStartPlanned = cPLI.dtStartPlanned,
				dtStartReal = cPLI.dtStartReal,
				dtStartSoft = cPLI.dtStartSoft,
				dtStopReal = cPLI.dtStopReal,
				dtTimingsUpdate = cPLI.dtTimingsUpdate,
				nFrameCurrent = cPLI.nFrameCurrent,
				nFramesQty = cPLI.nFramesQty,
				nFrameStart = cPLI.nFrameStart,
				nFrameStop = cPLI.nFrameStop,
				nID = cPLI.nID,
				sName = cPLI.sName,
				sNote = cPLI.sNote,
				cFile = cPLI.cFile
			};
			return cRetVal;
		}
		static public PlaylistItem GetBase(PlaylistItemSL cPLI)
		{
			PlaylistItem cRetVal = new PlaylistItem()
			{
				bCached = cPLI.bCached,
				bPlug = cPLI.bPlug,
				cAsset = cPLI.cAsset,
                aClasses = cPLI.aClasses,
                cStatus = cPLI.cStatus,
				dtStartHard = cPLI.dtStartHard,
				dtStartPlanned = cPLI.dtStartPlanned,
				dtStartReal = cPLI.dtStartReal,
				dtStartSoft = cPLI.dtStartSoft,
				dtStopReal = cPLI.dtStopReal,
				dtTimingsUpdate = cPLI.dtTimingsUpdate,
				nFrameCurrent = cPLI.nFrameCurrent,
				nFramesQty = cPLI.nFramesQty,
				nFrameStart = cPLI.nFrameStart,
				nFrameStop = cPLI.nFrameStop,
				nID = cPLI.nID,
				sName = cPLI.sName,
				sNote = cPLI.sNote,
				cFile = cPLI.cFile
			};
			return cRetVal;
		}
		static public PlaylistItemSL[] GetArrayOfPlaylistItemSLs(PlaylistItem[] aPLI)
		{
			PlaylistItemSL[] aRes = new PlaylistItemSL[aPLI.Length];
			for (int ni = 0; aPLI.Length > ni; ni++)
			{
				aRes[ni] = GetPlaylistItemSL(aPLI[ni]);
			}
			return aRes;
		}
		static public PlaylistItem[] GetArrayOfBases(PlaylistItemSL[] aPLI)
		{
			PlaylistItem[] aRes = new PlaylistItem[aPLI.Length];
			for (int ni = 0; aPLI.Length > ni; ni++)
			{
				aRes[ni] = GetBase(aPLI[ni]);
			}
			return aRes;
		}
		static public DateTime HardSoftGet(PlaylistItem cPLI)
		{
			return DateTime.MaxValue > cPLI.dtStartHard ? cPLI.dtStartHard : cPLI.dtStartSoft;
		}
		static public DateTime HardSoftPlannedGet(PlaylistItem cPLI)
		{
			DateTime dtPLIHardSoft = HardSoftGet(cPLI);
			return DateTime.MaxValue > dtPLIHardSoft ? dtPLIHardSoft : cPLI.dtStartPlanned;
		}

	}
	public class AdvancedPlaylistSL : Playlist
	{
		public static int dComparison(AdvancedPlaylistSL APL1, AdvancedPlaylistSL APL2)
		{
			int nRetVal;
			nRetVal = APL1.dtStart.CompareTo(APL2.dtStart);
			if (nRetVal == 0)
				nRetVal = APL1.sName.CompareTo(APL2.sName);
			return nRetVal;
		}
		public string sStart
		{
			get
			{
				if (dtStart == DateTime.MaxValue)
					return "";
				else
					return dtStart.Date.ToString("yyyy-MM-dd") + "  " + dtStart.ToString("HH:mm:ss");
			}
		}
		public string sStop
		{
			get
			{
				if (dtStop == DateTime.MaxValue)
					return "";
				else
					return dtStop.Date.ToString("yyyy-MM-dd") + "  " + dtStop.ToString("HH:mm:ss");
			}
		}
		public string sTiming
		{
			get
			{
				if (dtStart == DateTime.MaxValue)
				{
					if (null == aItems)
						return "";
					else
					{
						long nAllFrames = 0;
						foreach (PluginPlaylistItem cPLI in aItems)
							nAllFrames += cPLI.oAsset.nFramesQty;
						TimeSpan tsRetVal = new TimeSpan(0, 0, 0, 0, (int)(40 * nAllFrames));
						return tsRetVal.ToString("hh\\:mm\\:ss");
					}
				}
				else
					return dtStop.Subtract(dtStart).ToString("hh\\:mm\\:ss");
			}
		}
		private string _sStatus;
		public string sStatus
		{
			get
			{
				if (null == _sStatus)
				{
					if (null == aItems || 0 >= aItems.Length)
						_sStatus = "unknown";
					else
					{
						PluginPlaylistItem cFirst = aItems[0];
						PluginPlaylistItem cLast = aItems[aItems.Length - 1];
						if (null == cFirst.oStatus || null == cLast.oStatus)
						{
							_sStatus = "error";
							return _sStatus;
						}
						if ((cFirst.oStatus.sName == "played" || cFirst.oStatus.sName == "onair"))
						{
							dtStart = cFirst.dtStarted;
							dtStop = this.CheckPlannedStartsAndGetEndOfPL(aItems);
						}
						if (null != aItems.FirstOrDefault(o => o.oStatus == null || o.oStatus.sName == "failed"))
						{
							_sStatus = "failed";
							return _sStatus;
						}
						if (cLast.oStatus.sName == "played")
						{
							_sStatus = "played";
						}
						else if ((cFirst.oStatus.sName == "played" || cFirst.oStatus.sName == "onair") && (cLast.oStatus.sName == "planned" || cLast.oStatus.sName == "onair") && DateTime.Now.AddMinutes(-1) < dtStop)
							_sStatus = "onair";
						else if (cFirst.oStatus.sName == "planned" && DateTime.Now.AddMinutes(-1) < dtStart)
							_sStatus = "planned";
						else
							_sStatus = "error";
					}
				}
				return _sStatus;
			}
		}
		static public AdvancedPlaylistSL GetSL(Playlist cAPL)
		{
			DateTime dtStop = cAPL.CheckPlannedStartsAndGetEndOfPL(cAPL.aItems);
			AdvancedPlaylistSL cRetVal = new AdvancedPlaylistSL()
			{
				dtStart = cAPL.dtStart,
				dtStop = dtStop,
				nID = cAPL.nID,
				sName = cAPL.sName,
				aItems = cAPL.aItems,
			};
			string sTMP_just_to_call_status = cRetVal.sStatus;
			return cRetVal;
		}
		static public Playlist GetBase(AdvancedPlaylistSL cAPL)
		{
			Playlist cRetVal = new Playlist()
			{
				dtStart = cAPL.dtStart,
				dtStop = cAPL.dtStop,
				nID = cAPL.nID,
				sName = cAPL.sName,
				aItems = cAPL.aItems,
			};
			return cRetVal;
		}
		static public AdvancedPlaylistSL[] GetArrayOfSLs(Playlist[] aAPLs)
		{
			AdvancedPlaylistSL[] aRetVal = new AdvancedPlaylistSL[aAPLs.Length];
			for (int ni = 0; aAPLs.Length > ni; ni++)
			{
				aRetVal[ni] = GetSL(aAPLs[ni]);
			}
			return aRetVal;
		}
		static public Playlist[] GetArrayOfBases(AdvancedPlaylistSL[] aAPLs)
		{
			Playlist[] aRetVal = new Playlist[aAPLs.Length];
			for (int ni = 0; aAPLs.Length > ni; ni++)
			{
				aRetVal[ni] = GetBase(aAPLs[ni]);
			}
			return aRetVal;
		}
		static public PluginPlaylistItem[] GetArrayOfAPLIs(AssetSL[] aAss, DateTime dtStart)
		{
			PluginPlaylistItem[] aRetVal = new PluginPlaylistItem[aAss.Length];
			int nI = 0;
			foreach (AssetSL cA in aAss)
			{
				aRetVal[nI] = new PluginPlaylistItem()
				{
					nID = -1,
					oAsset = AssetSL.GetAsset(cA, true),
					oStatus = new IdNamePair() { nID = 1, sName = "planned" },
					dtStarted = (nI == 0 ? dtStart : DateTime.MaxValue),
					nFramesQty = cA.nFramesQty,
				};
				nI++;
			}
			if (dtStart < DateTime.MaxValue)
				x.CheckPlannedStartsAndGetEndOfPL(null, aRetVal);
			return aRetVal;
		}
		public AdvancedPlaylistItemSL[] GetAPLIs()
		{
			AdvancedPlaylistItemSL[] aRetVal = new AdvancedPlaylistItemSL[aItems.Length];
			for (int nI = 0; nI < aItems.Length; nI++)
			{
				aRetVal[nI] = new AdvancedPlaylistItemSL(aItems[nI]);
			}
			return aRetVal;
		}
	}
	public class AdvancedPlaylistItemSL : AdvancedPlaylistSL  //для отображения в одной и той же таблице с APL
	{
		public new string sTiming
		{
			get
			{
				if (nFramesQty == long.MaxValue)
					return "";
				TimeSpan tsRetVal = new TimeSpan(0, 0, 0, 0, (int)(40 * nFramesQty));
				return tsRetVal.ToString("hh\\:mm\\:ss");
			}
		}

		public long nFramesQty
		{
			get
			{
				return (aItems[0].nFramesQty < long.MaxValue ? aItems[0].nFramesQty : aItems[0].oAsset.nFramesQty);
			}
			set
			{
				aItems[0].nFramesQty = value;
			}
		}
		public new string sStatus
		{
			get
			{
				return aItems[0].oStatus.sName;
			}
		}
		public AdvancedPlaylistItemSL(PluginPlaylistItem cPPLI)
		{
			aItems = new PluginPlaylistItem[1] { cPPLI };
			dtStart = cPPLI.dtStarted;
			dtStop = dtStart < DateTime.MaxValue ? dtStart.AddMilliseconds(40 * (cPPLI.nFramesQty < long.MaxValue ? cPPLI.nFramesQty : cPPLI.oAsset.nFramesQty)) : DateTime.MaxValue;
			sName = cPPLI.oAsset.sName;
			nID = cPPLI.nID;
		}
	}
	public class TemplatesScheduleSL
	{
		public string sMon { get; set; }
		public string sTue { get; set; }
		public string sWed { get; set; }
		public string sThu { get; set; }
		public string sFri { get; set; }
		public string sSat { get; set; }
		public string sSun { get; set; }
		public string sName { get; set; }
		public string sPath { get; set; }
		public string sFoldername { get; set; }
		public TemplateBind cTemplatesBind { get; set; }
		public string sLine01 { get; set; }
		public string sLine02 { get; set; }
		public DateTime dtStart { get; set; }
		public DateTime dtStop { get; set; }
		public bool IsChanged;
		public List<TemplatesSchedule> aTSI_Source
		{
			get
			{
				return _aTSI_Source;
			}
		}
		private List<TemplatesSchedule> _aTSI_Source;
		public string sdtStart
		{
			get { return dtStart.Date.ToString("yyyy-MM-dd") + "  " + dtStart.ToString("HH:mm:ss"); }
			set { }
		}
		public string sdtStop
		{
			get { return DateTime.MaxValue == dtStop ? "" : dtStop.Date.ToString("yyyy-MM-dd") + "  " + dtStart.ToString("HH:mm:ss"); }
			set { }
		}
		public TemplatesScheduleSL()
		{
			_aTSI_Source = new List<TemplatesSchedule>();
		}
		static private bool DictionariesAreEqual(DictionaryElement[] d1, DictionaryElement[] d2)
		{
			bool bRetVal = true;
			DictionaryElement cDE1, cDE2;
			if (null == (cDE1 = d1.FirstOrDefault(o => o.sKey == "path")) || null == (cDE2 = d2.FirstOrDefault(o => o.sKey == "path")) || cDE1.sKey != cDE2.sKey)
				bRetVal = false;
			cDE1 = d1.FirstOrDefault(o => o.sKey == "line#0");
			cDE2 = d2.FirstOrDefault(o => o.sKey == "line#0");
			if ((null == cDE1 || null == cDE2 || cDE1.sValue != cDE2.sValue) && !(null == cDE1 && null == cDE2))
				bRetVal = false;
			cDE1 = d1.FirstOrDefault(o => o.sKey == "line#1");
			cDE2 = d2.FirstOrDefault(o => o.sKey == "line#1");
			if ((null == cDE1 || null == cDE2 || cDE1.sValue != cDE2.sValue) && !(null == cDE1 && null == cDE2))
				bRetVal = false;
			return bRetVal;
		}
		static private string DictionaryValueGet(string sKey, DictionaryElement[] aD)
		{
			DictionaryElement cDE;
			if (null == (cDE = aD.FirstOrDefault(o => o.sKey == sKey)))
				return null;
			else
				return cDE.sValue;
		}
		static public TemplatesScheduleSL[] GetTemplatesScheduleSLs(TemplatesSchedule[] aTemplatesSchedule)
		{
			List<TemplatesScheduleSL> aRetVal = new List<TemplatesScheduleSL>();
			List<TemplatesSchedule> aSource = aTemplatesSchedule.ToList();
			TemplatesSchedule cTSI_prev = null;
			TemplatesScheduleSL cTSISL_current = null;
			string sDir;
			aSource.Sort(TSI_Compare);
			foreach (TemplatesSchedule cTSI in aSource)
			{
				if (null == cTSI_prev || cTSI_prev.cTemplateBind.nID != cTSI.cTemplateBind.nID || cTSI_prev.dtStop != cTSI.dtStop
					|| cTSI_prev.dtStart.TimeOfDay != cTSI.dtStart.TimeOfDay || cTSI_prev.dtStart.GetMonday() != cTSI.dtStart.GetMonday()
					|| !DictionariesAreEqual(cTSI_prev.aDictionary, cTSI.aDictionary))
				{
					sDir = DictionaryValueGet("path", cTSI.aDictionary);
					cTSISL_current = new TemplatesScheduleSL()
					{
						dtStart = cTSI.dtStart.GetMonday(),
						dtStop = cTSI.dtStop,
						cTemplatesBind = cTSI.cTemplateBind,
						sPath = sDir,
						sFoldername = System.IO.Path.GetFileName(sDir),
						sLine01 = DictionaryValueGet("line#0", cTSI.aDictionary),
						sLine02 = DictionaryValueGet("line#1", cTSI.aDictionary),
						IsChanged = false
					};
					aRetVal.Add(cTSISL_current);
					cTSI_prev = cTSI;
				}
				cTSISL_current._aTSI_Source.Add(cTSI);
				DateTime dtStartThisWeek = cTSI.dtStart;
				while (dtStartThisWeek < DateTime.Today.GetMonday())
					dtStartThisWeek = dtStartThisWeek.AddDays(7);

				if (dtStartThisWeek < cTSI.dtStop)
					switch (dtStartThisWeek.DayOfWeek)
					{
						case DayOfWeek.Monday:
							cTSISL_current.sMon = g.Common.sMon;
							break;
						case DayOfWeek.Tuesday:
							cTSISL_current.sTue = g.Common.sTue;
							break;
						case DayOfWeek.Wednesday:
							cTSISL_current.sWed = g.Common.sWed;
							break;
						case DayOfWeek.Thursday:
							cTSISL_current.sThu = g.Common.sThu;
							break;
						case DayOfWeek.Friday:
							cTSISL_current.sFri = g.Common.sFri;
							break;
						case DayOfWeek.Saturday:
							cTSISL_current.sSat = g.Common.sSat;
							break;
						case DayOfWeek.Sunday:
							cTSISL_current.sSun = g.Common.sSun;
							break;
					}
			}
			return aRetVal.ToArray();
		}
		static private int TSI_Compare(TemplatesSchedule a, TemplatesSchedule b)
		{
			int nRetVal = a.cTemplateBind.nID.CompareTo(b.cTemplateBind.nID);
			if (0 == nRetVal)
				nRetVal = a.dtStart.TimeOfDay.CompareTo(b.dtStart.TimeOfDay);
			return nRetVal;
		}
		public bool IsThisDayChacked(DayOfWeek enDayOfWeek)
		{
			switch (enDayOfWeek)
			{
				case DayOfWeek.Monday:
					if (null != sMon)
						return true;
					break;
				case DayOfWeek.Tuesday:
					if (null != sTue)
						return true;
					break;
				case DayOfWeek.Wednesday:
					if (null != sWed)
						return true;
					break;
				case DayOfWeek.Thursday:
					if (null != sThu)
						return true;
					break;
				case DayOfWeek.Friday:
					if (null != sFri)
						return true;
					break;
				case DayOfWeek.Saturday:
					if (null != sSat)
						return true;
					break;
				case DayOfWeek.Sunday:
					if (null != sSun)
						return true;
					break;
			}
			return false;
		}
		public TemplatesSchedule[] GetTemplatesSchedule()
		{
			List<TemplatesSchedule> aRetVal = new List<TemplatesSchedule>();
			TemplatesSchedule cTS;
			DateTime dtMonday = dtStart.GetMonday().Date;
			long nStartInSeconds = dtStart.Hour * 3600 + dtStart.Minute * 60 + dtStart.Second;

			for (int ni = 0; 7 > ni; ni++)
			{
				if (IsThisDayChacked((DayOfWeek)(ni == 6 ? 0 : ni + 1)))
				{
					cTS = new TemplatesSchedule();
					cTS.dtStart = dtMonday.AddDays(ni).AddSeconds(nStartInSeconds);
					cTS.dtStop = dtStop;
					cTS.nID = -1;
					cTS.nIntervalInMilliseconds = (int)(new System.TimeSpan(7, 0, 0, 0)).TotalMilliseconds;
					cTS.cTemplateBind = cTemplatesBind;
					List<DictionaryElement> aTMP = new List<DictionaryElement>();
					if (!sPath.IsNullOrEmpty()) aTMP.Add(new DictionaryElement() { nID = -1, nTargetID = -1, sKey = "path", sValue = sPath });
					if (!sLine01.IsNullOrEmpty()) aTMP.Add(new DictionaryElement() { nID = -1, nTargetID = -1, sKey = "line#0", sValue = sLine01 });
					if (!sLine02.IsNullOrEmpty()) aTMP.Add(new DictionaryElement() { nID = -1, nTargetID = -1, sKey = "line#1", sValue = sLine02 });
					cTS.aDictionary = aTMP.ToArray();
					//cTS.tsInterval=new System
					aRetVal.Add(cTS);
				}
			}
			return aRetVal.ToArray();
		}
		static public TemplatesSchedule[] GetTemplatesSchedule(TemplatesScheduleSL[] aTSISL)
		{
			List<TemplatesSchedule> aRetVal = new List<TemplatesSchedule>();

			foreach (TemplatesScheduleSL cTSISL in aTSISL)
				aRetVal.AddRange(cTSISL.GetTemplatesSchedule());

			return aRetVal.ToArray();
		}
		static public TemplatesSchedule[] GetTemplatesScheduleSource(TemplatesScheduleSL[] aTSISL)
		{
			List<TemplatesSchedule> aRetVal = new List<TemplatesSchedule>();

			foreach (TemplatesScheduleSL cTSISL in aTSISL)
				aRetVal.AddRange(cTSISL.aTSI_Source);

			return aRetVal.ToArray();
		}
	}
	public class MyIntSL : MyInt
	{
		static public MyInt[] MyIntArrayGet(List<long> aArray)
		{
			MyInt[] aRetVal = new MyInt[aArray.Count];
			for (int nI = 0; aArray.Count > nI; nI++)
			{
				aRetVal[nI] = new MyInt() { nID = aArray[nI] };
			}
			return aRetVal;
		}
	}
	public class DataGridsSortState
	{
		public string sHeaderName = null;
		public bool bBackward = false;
		public string sBindingPath = null;
	}
	public partial class MainPage : UserControl
	{
		private class DispatcherTimer : System.Windows.Threading.DispatcherTimer
		{
			public object oTag { get; set; }
			public void Start(object oTag)
			{
				this.oTag = oTag;
				base.Start();
			}
		}

		Uri _cUriAuth;
		Uri _cUriProfile;

		public MainPage()
		{
			InitializeComponent();
			App.Current.CheckAndDownloadUpdateCompleted += CheckAndDownloadUpdateCompleted;
			App.Current.CheckAndDownloadUpdateAsync();
			_cUriAuth = new Uri("/auth", UriKind.Relative);
			_cUriProfile = new Uri("/profile", UriKind.Relative);
			Preferences.ServerLoad();
			if (Application.Current.IsRunningOutOfBrowser)
				Application.Current.MainWindow.WindowState = WindowState.Normal;

			
		}
		private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			try
			{
				if (null == Preferences.cServer)
				{
					e.Cancel = true;
					return;
				}
				ApplicationNameTextBlock.Text = Preferences.cServer.sChannelName;
				if (_cUriAuth != e.Uri)
				{
					if (_cUriProfile == e.Uri || access.scopes.IsWebPageVisible(e.Uri.ToString().Remove(0, 1)))    //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  || true
					{
						_ui_spLinks.Visibility = System.Windows.Visibility.Visible;
						MenuUIReforming(e.Uri);
					}
					else
					{
						e.Cancel = true;
						ContentFrame.Navigate(_cUriAuth);
					}
				}
				else
					_ui_spLinks.Visibility = System.Windows.Visibility.Collapsed;
			}
			catch (Exception ex)
            {
                (new MsgBox()).ShowError(ex);
            }
		}

		private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			e.Handled = true;
			ChildWindow errorWin = new ErrorWindow(e.Uri);
			errorWin.Show();
		}
		void MenuUIReforming(Uri cNewURI)
		{
			string sURI;
			foreach (UIElement ui_sp in _ui_spLinks.Children)
			{
				if (ui_sp is StackPanel)
				{
					foreach (UIElement ui_hb in ((StackPanel)ui_sp).Children)
					{
						if (ui_hb != null && ui_hb is HyperlinkButton && ((HyperlinkButton)ui_hb).NavigateUri != null)
						{
							sURI = ((HyperlinkButton)ui_hb).NavigateUri.ToString();
							if (sURI.Equals(cNewURI.ToString(), StringComparison.CurrentCultureIgnoreCase))
							{
								if (!VisualStateManager.GoToState((HyperlinkButton)ui_hb, "ActiveLink", true))
								{
									DispatcherTimer cTimer = new DispatcherTimer();
									cTimer.Tick += (object sender, EventArgs e) => { if (VisualStateManager.GoToState((HyperlinkButton)cTimer.oTag, "ActiveLink", true)) cTimer.Stop(); };
									cTimer.Interval = new System.TimeSpan(0, 0, 0, 0, 10);
									cTimer.Start(ui_hb);
								}
							}
							else
								VisualStateManager.GoToState(((HyperlinkButton)ui_hb), "InactiveLink", true);
							if (access.scopes.IsWebPageVisible(sURI.Remove(0, 1)) && (!sURI.Equals("/ingest", StringComparison.CurrentCultureIgnoreCase) || Application.Current.IsRunningOutOfBrowser))   //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  || true
								ui_sp.Visibility = Visibility.Visible;
							else
								ui_sp.Visibility = Visibility.Collapsed;
						}
					}
				}
			}
		}

		public void Auth()
		{
			ContentFrame.Navigate(_cUriAuth);
		}
		private void CheckAndDownloadUpdateCompleted(object sender, CheckAndDownloadUpdateCompletedEventArgs e)
		{
			if (e.UpdateAvailable)
			{
				MessageBox.Show("An update has been downloaded. " +
					"Restart the application to run the new version.");
			}
			else if (e.Error != null &&
				e.Error is PlatformNotSupportedException)
			{
				MessageBox.Show("An application update is available, " +
					"but it requires a new version of Silverlight. " +
					"Visit the application home page to upgrade.");
			}
		}
	}
	static public class x
	{
		static public ingest.Tab TabGet(this TabControl ui)
		{
			if (null == ui.SelectedItem)
				return null;
			return ui.SelectedItem.ToTab();
		}
		static public ingest.Tab ToTab(this object o)
		{
			return (ingest.Tab)(((TabItem)o).Tag ?? null);
		}
		static public AssetSL[] AddOrReplaceElement(this AssetSL[] aAss, AssetSL cAss)
		{
			List<AssetSL> aRes = aAss.ToList();
			AssetSL cAssetSL;
			if (null != (cAssetSL = aRes.FirstOrDefault(o => o.nID == cAss.nID)))
			{
				aRes.Remove(cAssetSL);
			}
			aRes.Add(cAss);
			return aRes.ToArray();
		}
		static public AssetSL[] RemoveElements(this AssetSL[] aAss, Asset[] aAssetsToRemove)
		{
			AssetSL cAssToRemove;
			List<AssetSL> aAssests = aAss.ToList();
			foreach (Asset cAss in aAssetsToRemove)
			{
				if (null != (cAssToRemove = aAssests.FirstOrDefault(o => o.nID == cAss.nID)))
					aAssests.Remove(cAssToRemove);
			}
			return aAssests.ToArray();
		}
		static public DateTime CheckPlannedStartsAndGetEndOfPL(this Playlist cPL, PluginPlaylistItem[] aPLIs)
		{
			if (null == aPLIs)
				return DateTime.MaxValue;
			DateTime dtStart = aPLIs[0].dtStarted;
			if (null != cPL)
				dtStart = cPL.dtStart;

			DateTime dtEndLast = DateTime.MaxValue;

			foreach (PluginPlaylistItem cPPLI in aPLIs)
			{
				if (cPPLI.oStatus.sName != "planned" || dtEndLast == DateTime.MaxValue)
				{
					if (cPPLI.oStatus.sName == "planned" && dtEndLast == DateTime.MaxValue)
						cPPLI.dtStarted = dtStart;
					if (cPPLI.oStatus.sName == "planned" || cPPLI == aPLIs[aPLIs.Length - 1])
						dtEndLast = cPPLI.dtStarted < DateTime.MaxValue ? cPPLI.dtStarted.AddMilliseconds((cPPLI.nFramesQty < 1 ? cPPLI.oAsset.nFramesQty : cPPLI.nFramesQty) * 40) : DateTime.MaxValue;
					continue;
				}
				cPPLI.dtStarted = dtEndLast;
				dtEndLast = cPPLI.dtStarted < DateTime.MaxValue ? cPPLI.dtStarted.AddMilliseconds((cPPLI.nFramesQty < 1 ? cPPLI.oAsset.nFramesQty : cPPLI.nFramesQty) * 40) : DateTime.MaxValue;
			}
			return dtEndLast;
		}
	}
}