using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using helpers;
using helpers.extensions;
using helpers.replica;
using cues = helpers.replica.cues;
using helpers.replica.cues;
using helpers.replica.mam;
using helpers.replica.media;
using helpers.replica.pl;
using helpers.replica.hk;
using helpers.replica.scr;
using SIO = System.IO;

using g = globalization;

namespace webservice.services
{
	[WebService(Namespace = "http://replica/services/DBInteract.asmx")] //local
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	public class DBInteract : webservice.DBInteract
	{
		/*заглушка для опознавания завуалированных типов (ну чтобы не морочиться с serialization и остальной бодягой*/
		[WebMethod]
        public void KnownTypes(DBFilters.DBFilter t1, DBFilters.Operators t2, File.Ingest.Clip t3, File.Ingest.Advertisement t4, File.Ingest.Program t5, File.Ingest.Design t6)
		{
		}

		#region classes

		public class SessionInfo : services.SessionInfo
		{

			public DB.Credentials cDBCredentials
			{
				get
				{
					return (DB.Credentials)_cStoreAtom.ValueGet("cDBCredentials");
				}
				set
				{
					_cStoreAtom.ValueSet("cDBCredentials", value);
				}
			}
			public Profile cProfile
			{
				get
				{
					if (null == _cStoreAtom)
                        throw new Exception(g.Webservice.sErrorDBInteract1);
					return (Profile)_cStoreAtom.ValueGet("cProfile");
				}
				set
				{
					_cStoreAtom.ValueSet("cProfile", value);
				}
			}

			public SessionInfo()
				: base("DBInteract")
			{
			}
		}

		//public class TimeRange : System.ComponentModel.INotifyPropertyChanged
		//{
		//    helpers.TimeRange _cTR;
		//    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		//    public long nTicksIn
		//    {
		//        get
		//        {
		//            return _cTR.nTicksIn;
		//        }
		//        set
		//        {
		//            _cTR.nTicksIn = value;
		//            NotifyChange("nTicksIn");
		//        }
		//    }
		//    public long nTicksOut
		//    {
		//        get
		//        {
		//            return _cTR.nTicksOut;
		//        }
		//        set
		//        {
		//            _cTR.nTicksOut = value;
		//            NotifyChange("nTicksOut");
		//        }
		//    }
		//    public DateTime dtIn
		//    {
		//        get
		//        {
		//            return new DateTime(_cTR.nTicksIn);
		//        }
		//        set
		//        {
		//            _cTR.nTicksIn = value.TimeOfDay.Ticks;
		//            NotifyChange("dtIn");
		//        }
		//    }
		//    public DateTime dtOut
		//    {
		//        get
		//        {
		//            return new DateTime(_cTR.nTicksOut);
		//        }
		//        set
		//        {
		//            _cTR.nTicksOut = value.TimeOfDay.Ticks;
		//            NotifyChange("dtOut");
		//        }
		//    }

		//    public TimeRange()
		//    {
		//        _cTR = new helpers.TimeRange();
		//    }
		//    public TimeRange(helpers.TimeRange cTR)
		//    {
		//        _cTR = cTR;
		//    }
		//    private void NotifyChange(params object[] properties)
		//    {
		//        if (PropertyChanged != null)
		//        {
		//            foreach (string prop in properties)
		//            {
		//                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
		//            }
		//        }
		//    }
		//}

		public class TransliterationPair
		{
			public string sSource;
			public string sTarget;

			public TransliterationPair()
			{
			}
			public TransliterationPair(string sSource, string sTarget)
				: this()
			{
				this.sSource = sSource;
				this.sTarget = sTarget;
			}
			public TransliterationPair(Hashtable ahDBRow)
				: this(ahDBRow["sSource"].ToString(), ahDBRow["sTarget"].ToString())
			{
			}
		}
		public class DBFilters
		{
			[System.Xml.Serialization.XmlType("DBFiltersOperators")]
			public enum Operators
			{
				equal,
				notequal,
				contains,
				notcontains,
				more,
				less
			}
			public enum Binds
			{
				and,
				or
			}

			[System.Xml.Serialization.XmlInclude(typeof(DBFilter))]
			public class DBFiltersGroup
			{
				public Nullable<Binds> eBind;

				public object cValue;

				public DBFiltersGroup cNext;

				public string sSQL
				{
					get
					{
						string sRetVal = "";
						if (null != cValue)
						{
							if (null != eBind)
								sRetVal += " " + eBind.Value.ToString().ToUpper() + " ";
							sRetVal += "(" + ((DBFilter)cValue).sSQL + ")";
							if (null != cNext)
							{
								if (cNext is DBFilter)
									sRetVal += ((DBFilter)cNext).sSQL;
								else if (cNext is DBFiltersGroup)
									sRetVal += ((DBFiltersGroup)cNext).sSQL;
							}
						}
						return sRetVal;
					}
				}
				public string sSQL_dtOnly
				{
					get
					{
						string sRetVal = "";
						if (null != cValue)
						{
							if (null != eBind)
								sRetVal += " " + eBind.Value.ToString().ToUpper() + " ";
							sRetVal += "(" + ((DBFilter)cValue).sSQL + ")";
							if (null != cNext)
							{
								if (cNext is DBFilter)
									sRetVal += ((DBFilter)cNext).sSQL;
								else if (cNext is DBFiltersGroup)
									sRetVal += ((DBFiltersGroup)cNext).sSQL;
							}
						}
						return sRetVal;
					}
				}

				public DBFiltersGroup()
				{
				}
			}
			public class DBFilter : DBFiltersGroup
			{
				public string sName;
				public Operators eOP;

				new public string sSQL
				{
					get
					{
						string sRetVal = "";
						if (null != eBind)
							sRetVal += " " + eBind.Value.ToString().ToUpper() + " ";
						sRetVal += "`" + sName + "`";

						#region operator proccessing
						switch (eOP)
						{
							case Operators.equal:
								if (null != cValue)
									sRetVal += " = ";
								else
									sRetVal += " IS NULL";
								break;
							case Operators.notequal:
								if (null != cValue)
									sRetVal += " <> ";
								else
									sRetVal += " IS NOT NULL";
								break;
							case Operators.contains:
								if (null != cValue && cValue is string)
								{
									sRetVal += " ILIKE ";
									cValue = "%" + cValue.ToString() + "%";
								}
								else
									return "";
								break;
							case Operators.notcontains:
								if (null != cValue && cValue is string)
								{
									sRetVal += " NOT ILIKE ";
									cValue = "%" + cValue.ToString() + "%";
								}
								else
									return "";
								break;
							case Operators.more:
								if (null != cValue)
									sRetVal += " > ";
								else
									return "";
								break;
							case Operators.less:
								if (null != cValue)
									sRetVal += " < ";
								else
									return "";
								break;
						}
						#endregion

						if (null != cValue)
						{
							if (cValue is string)
								sRetVal += "'" + cValue.ToString().Replace("`", "'").Replace("'", "''") + "'";
							else if (cValue is DateTime)
								sRetVal += "'" + ((DateTime)cValue).ToString("yyyy-MM-dd HH:mm:ss") + "'";
							else
								sRetVal += cValue.ToString();
						}

						if (null != cNext)
						{
							if (cNext is DBFilter)
								sRetVal += ((DBFilter)cNext).sSQL;
							else if (cNext is DBFiltersGroup)
								sRetVal += ((DBFiltersGroup)cNext).sSQL;
						}
						return sRetVal;
					}
				}
				new public string sSQL_dtOnly
				{
					get
					{
						string sRetVal = "";
						if (null != cValue)
						{
							if (null != eBind)
								sRetVal += " " + eBind.Value.ToString().ToUpper() + " ";
							sRetVal += "(" + ((DBFilter)cValue).sSQL + ")";
							if (null != cNext)
							{
								if (cNext is DBFilter)
									sRetVal += ((DBFilter)cNext).sSQL;
								else if (cNext is DBFiltersGroup)
									sRetVal += ((DBFiltersGroup)cNext).sSQL;
							}
						}
						return sRetVal;
					}
				}

				public DBFilter()
				{
					eBind = null;
				}
			}

			public DBFiltersGroup cGroup;

			public string sSQL
			{
				get
				{
					return cGroup.sSQL;
				}
			}

			public DBFilters()
			{
				cGroup = new DBFiltersGroup();
			}
		}

		public class AlterString  // для передачи массивов строк
		{
			public string sValue;
			static public AlterString[] GetArray(List<string> aStr)
			{
				AlterString[] aRetVal = new AlterString[aStr.Count];
				int nInd = 0;
				foreach (string sStr in aStr)
					aRetVal[nInd++] = new AlterString() { sValue = sStr };
				return aRetVal;
			}
		}

		#endregion

		private DateTime _dtPLUpdated;
		private SessionInfo _cSI;
		static object cSyncRoot;
		static private List<object> _aIntermediateObjectsStorage;
		static public DateTime _dtNearestAdvertsBlock;
		static public DateTime dtNearestAdvertsBlock
		{
			get
			{
				return _dtNearestAdvertsBlock;
			}
			set
			{
				//(new Logger()).WriteNotice("_dtNearestAdvertsBlock = value [" + value.ToStr() + "]<br>" + string.Join("<br>", (new System.Diagnostics.StackTrace(false)).GetFrames().Select(o => o.GetMethod().Name).ToArray()));
				_dtNearestAdvertsBlock = value;
			}
		}

		static DBInteract()
		{
			(new Logger()).WriteDebug("constructor:in");
			cSyncRoot = new object();
			_aIntermediateObjectsStorage = new List<object>();
			dtNearestAdvertsBlock = DateTime.MinValue;
		}
		public DBInteract()
		{
			lock (cSyncRoot)
			{
				(new Logger()).WriteDebug3("in [hc:" + GetHashCode() + "]");
				try
				{
					try
					{
						_cSI = new SessionInfo();
					}
					catch { }
					(new Logger()).WriteDebug4("after si ctor [hc:" + GetHashCode() + "]");
					if (null != _cSI && null != _cSI.cProfile && null != _cSI.cDBCredentials)
					{
						(new Logger()).WriteDebug4("preferences [hc:" + GetHashCode() + "][_csi: server = " + _cSI.cDBCredentials.sServer + ", user = " + _cSI.cProfile.sUsername + ", role = " + _cSI.cDBCredentials.sRole + "]");
						_cDB.CredentialsSet(_cSI.cDBCredentials);
					}
					else
						(new Logger()).WriteDebug3("preferences is null [hc:" + GetHashCode() + "]");
				}
				catch (Exception ex)
				{
					WebServiceError.Add(_cSI, ex);
				}
			}
		}

		#region upload files
		[WebMethod(EnableSession = true)]
		public int UploadFileBegin(byte[] aBytes)
		{
			int nRetVal = -1;

			if (null != aBytes)
			{
				System.IO.FileInfo cFI = new System.IO.FileInfo(System.IO.Path.GetTempFileName());
				System.IO.FileStream cFS = cFI.OpenWrite();
				cFS.Write(aBytes, 0, aBytes.Length);
				cFS.Close();
				_aIntermediateObjectsStorage.Add(cFI);
				nRetVal = _aIntermediateObjectsStorage.IndexOf(cFI);
			}

			return nRetVal;
		}
		[WebMethod(EnableSession = true)]
		public void UploadFileContinue(int nFileIndx, byte[] aBytes)
		{
			if (null != _aIntermediateObjectsStorage && nFileIndx < _aIntermediateObjectsStorage.Count && null != aBytes)
			{
				System.IO.FileStream cFS = ((System.IO.FileInfo)_aIntermediateObjectsStorage[nFileIndx]).OpenWrite();
				cFS.Seek(0, System.IO.SeekOrigin.End);
				cFS.Write(aBytes, 0, aBytes.Length);
				cFS.Close();
			}
		}
		[WebMethod(EnableSession = true)]
		public string UploadFileEnd(int nFileIndx)
		{
			string sRetVal = null;
			if (null != _aIntermediateObjectsStorage && nFileIndx < _aIntermediateObjectsStorage.Count)
			{
				sRetVal = ((System.IO.FileInfo)_aIntermediateObjectsStorage[nFileIndx]).FullName;
				_aIntermediateObjectsStorage[nFileIndx] = null;
			}
			return sRetVal;
		}
		#endregion

		#region errors

		[WebMethod(EnableSession = true)]
		public bool IsThereAnyErrors()
		{
			return WebServiceError.IsThereAny(_cSI);
		}

		[WebMethod(EnableSession = true)]
		public void ErrorsClear()
		{
			WebServiceError.Clear(_cSI);
		}

		[WebMethod(EnableSession = true)]
		public WebServiceError[] ErrorsGet()
		{
			return WebServiceError.Get(_cSI);
		}

		[WebMethod(EnableSession = true)]
		public WebServiceError[] ErrorsAllGet()
		{
			return WebServiceError.Get();
		}

		[WebMethod(EnableSession = true)]
		public WebServiceError ErrorLastGet()
		{
			return WebServiceError.LastGet(_cSI);
		}

		#endregion

		#region authentication
		[WebMethod(EnableSession = true)]
		public void InitSession()
		{
			try
			{
				if (null == _cSI)
					_cSI = new SessionInfo();
			}
			catch { }
		}
		[WebMethod(EnableSession = true)]
		public bool Init(string sName, string sPassword)
		{
			if (null == sName || null == sPassword)
				return false;
			bool bRetVal = true;
			try
			{
				if (null == _cSI)
					_cSI = new SessionInfo();
				_cDB.CredentialsSet(_cSI.cDBCredentials = webservice.Preferences.DBCredentialsGet(sName, sPassword));
				Profile cProfile = new Profile(sName, sPassword);
				_cSI.cProfile = cProfile;
				access.scopes.init(AccessScopesGet());
				if ("user" == sName)
					DBCredentialsSet("user");
			}
			catch (Exception ex)
			{
				try
				{
					WebServiceError.Add(_cSI, ex);
				}
				catch { }
				bRetVal = false;
			}
			return bRetVal;
		}

		public string[] WebPagesAccessGet()
		{
			try
			{
				return _cDB.Select("SELECT * FROM hk.`fUserWebPageAccessGet`('" + _cSI.cDBCredentials.sUser + "')").Select(o => o["sPage"].ToStr()).ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}

		[WebMethod(EnableSession = true)]
		public access.types.AccessScope[] AccessScopesGet()
		{
			try
			{
                return base.AccessScopesGet();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}

		[WebMethod(EnableSession = true)]
		public void DBCredentialsSet(string sUsername)
		{
			_cSI.cDBCredentials = webservice.Preferences.DBCredentialsGet(sUsername, null);
			_cDB.CredentialsSet(_cSI.cDBCredentials.sUser, _cSI.cDBCredentials.sPassword);
		}

		[WebMethod(EnableSession = true)]
		public Profile ProfileGet()
		{
			try
			{
				if (null != _cSI && null != _cSI.cProfile)
					return _cSI.cProfile;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public bool Ping()
		{
			if (null == _cSI.cProfile)
				return false;
			else
				return true;
		}
		#endregion

		#region mam

		[WebMethod(EnableSession = true)]
		public AlterString[] AssetsRemove(Asset[] aAssets)
		{
			List<string> aRes = new List<string>();
			_cDB.TransactionBegin();
			foreach (Asset cAsset in aAssets)
			{
				try
				{
					base.AssetRemove(cAsset);
				}
				catch (Exception ex)
				{
                    WebServiceError.Add(_cSI, ex);
					aRes.Add("ID=" + cAsset.nID + " Name=" + cAsset.sName); // эти  строки не удалились
				}
			}
			_cDB.TransactionCommit();
			return AlterString.GetArray(aRes);
		}

		[WebMethod(EnableSession = true)]
		new public Asset[] AssetsGet(string sVideoTypeFilter)
		{
			try
			{
				IdNamePair cVideoTypeFilter = null;
				if (null != sVideoTypeFilter && 0 < sVideoTypeFilter.Length)
					cVideoTypeFilter = new IdNamePair(webservice.Preferences.cDBKeysMap.GetValueByKey(DBKeysMap.MapClass.asset_videotype, sVideoTypeFilter));
				List<Asset> aAssets = base.AssetsGet(cVideoTypeFilter).ToList();
				if (null == sVideoTypeFilter || "clip" == sVideoTypeFilter)
				{
					List<Clip> aClips = base.ClipsGet().ToList();
					Clip cClip;
					for (int nI = 0; aAssets.Count > nI; nI++)
					{
						if (null != (cClip = aClips.FirstOrDefault(o => o.nID == aAssets[nI].nID)))
						{
							aClips.Remove(cClip);
							aAssets[nI] = cClip;
						}
					}
				}
				return aAssets.ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}

		[WebMethod(EnableSession = true)]
		new public Program[] ProgramsGet()
		{
			try
			{
				return base.ProgramsGet().ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}

		[WebMethod(EnableSession = true)]
		new public Clip ClipGet(long nAssetID)
		{
			return base.ClipGet(nAssetID);
		}

		[WebMethod(EnableSession = true)]
		new public Clip[] ClipsGet()
		{
			Clip[] aRetVal=base.ClipsGet().ToArray();
			(new Logger()).WriteNotice("clips_get: " + aRetVal.Length.ToString());
			Queue<Hashtable> ahRows = _cDB.Select("select `idAssets` from mam.`tAssetAttributes` where `sKey`='smoking'");
			Clip cClip = null;
			long nID;
			if (null != ahRows)
				while (0 < ahRows.Count)
				{
					nID = ahRows.Dequeue()["idAssets"].ToID();
					if (null != (cClip = aRetVal.FirstOrDefault(o => o.nID == nID)))
						cClip.bSmoking = true;
				}
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		new public Advertisement AdvertisementGet(long nAssetID)
		{
			return base.AdvertisementGet(nAssetID);
		}

		[WebMethod(EnableSession = true)]
		public Asset[] AdvertisementsGet()
		{
			Asset[] aRetVal = null;
			Queue<helpers.replica.mam.Asset> aqAssets = base.AssetsGet(new IdNamePair(webservice.Preferences.cDBKeysMap.GetValueByKey(DBKeysMap.MapClass.asset_videotype, "advertisement")));
			if (null != aqAssets)
                aRetVal = aqAssets.ToArray();
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		new public Design DesignGet(long nAssetID)
		{
			return base.DesignGet(nAssetID);
		}
		[WebMethod(EnableSession = true)]
		public Asset[] DesignsGet()
		{
			Asset[] aRetVal = null;
			Queue<helpers.replica.mam.Asset> aqAssets = base.AssetsGet(new IdNamePair(webservice.Preferences.cDBKeysMap.GetValueByKey(DBKeysMap.MapClass.asset_videotype, "design")));
			if (null != aqAssets)
                aRetVal = aqAssets.ToArray();
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair AssetVideoTypeGet(long nAssetID)
		{
			return base.AssetVideoTypeGet(nAssetID);
		}

		[WebMethod(EnableSession = true)]
		new public Class[] ClassesGet()
		{
			Class[] aRetVal = null;
			try
			{
				Queue<helpers.replica.pl.Class> aqClasses = base.ClassesGet();
				if (null != aqClasses)
					aRetVal = aqClasses.ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		public IdNamePair[] StatusesGet()
		{
			IdNamePair[] aRetVal = null;
			Queue<Hashtable> aqStatuses = null;
			Hashtable ahRow = null;
			if (null != (aqStatuses = _cDB.Select("SELECT * FROM pl.`tStatuses` ORDER BY `sName`")))
			{
				aRetVal = new IdNamePair[aqStatuses.Count];
				int nIndx = 0;
				while (0 < aqStatuses.Count)
				{
					ahRow = aqStatuses.Dequeue();
					aRetVal[nIndx++] = new IdNamePair(ahRow["id"], webservice.Preferences.cDBKeysMap.GetTitle(DBKeysMap.MapClass.pli_status, ahRow["sName"].ToString()));
				}
			}
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		new public ChatInOut[] ChatInOutsGet(Asset cAsset)
		{
			try
			{
				return base.ChatInOutsGet(cAsset).ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}

		[WebMethod(EnableSession = true)]
		public Class ProgramWithChatClassGet() //EMERGENCY:l это очень плохая, никуда негодная функция!!! ее надо уничтожить.
		{
			helpers.replica.pl.Class cRetVal = null;
			try
			{
				cRetVal = base.ClassGet("program_with_chat");
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return cRetVal;
		}

		[WebMethod(EnableSession = true)]
		public bool ChatInOutsSave(Asset cAsset, ChatInOut[] aChatInOuts)
		{
			if (!access.scopes.programs.chatinouts.bCanUpdate)
				return false;
			try
			{
				base.ChatInOutsSave(cAsset, aChatInOuts);
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
				return false;
			}
			return true;
		}

		[WebMethod(EnableSession = true)]
		public void RingtoneAdd(Clip cClip, int nRTCode)
		{
			_cDB.TransactionBegin();
			try
			{
				if (null == cClip.cClass)
					throw new Exception(g.Webservice.sErrorDBInteract8);
				base.AssetClassSet(cClip);
				_cDB.Select("DELETE FROM cues.`tRingtones` WHERE `nBindCode` = " + cClip.stCues.sAlbum.ToInt32());
				if (-1 < nRTCode)
					_cDB.Select("INSERT INTO cues.`tRingtones` (`nBindCode`, `nReplaceCode`) VALUES (" + cClip.stCues.sAlbum.ToInt32() + ", " + nRTCode + ")");
				Queue<helpers.replica.pl.PlaylistItem> aqPLIs = null;
				if (null != (aqPLIs = PlaylistItemsGet(cClip)))
				{
					while (0 < aqPLIs.Count)
						PLIClassChange(aqPLIs.Dequeue().nID, cClip.cClass.nID);
				}
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
				_cDB.TransactionRollBack();
			}
		}

		[WebMethod(EnableSession = true)]
		new public long ClipSave(Clip cClip) //EMERGENCY:l убрать long, возвращать Clip
		{
			try
			{
				base.ClipSave(cClip);
				return cClip.nID;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return -1;
			}
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair VideoTypeGet(string sType)
		{
			return base.VideoTypeGet(sType);
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair[] VideoTypesGet()
		{
			return base.VideoTypesGet();
		}

		[WebMethod(EnableSession = true)]
		new public Program ProgramGet(long nAssetID)
		{
			try
			{
				Program cResult = base.ProgramGet(nAssetID);
				cResult.RAOInfoLoad();
				return cResult;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return null;
			}
		}
		[WebMethod(EnableSession = true)]
		public AlterString[] AssetsSave(Asset[] aAsset)
		{
			List<string> aRetVal = new List<string>();
			try
			{
				_cDB.TransactionBegin();
				foreach (Asset cAsset in aAsset)
				{
					try
					{
						base.AssetSave(cAsset);
					}
					catch
					{
						aRetVal.Add("ID=" + cAsset.nID + " Name=" + cAsset.sName); // эти  строки не внеслись
					}
				}
				_cDB.TransactionCommit();
				return AlterString.GetArray(aRetVal);  // .ToArray()
			}
			catch (Exception ex)
			{
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
				return null;
			}
		}

		[WebMethod(EnableSession = true)]
		public bool AssetParametersToPlaylistSave(long nAssID)  //EMERGENCY:l убрать long, принимать Asset (меж.прочим, nAssID - это ID жопы))  :==))))
		{
			(new Logger()).WriteNotice("we are changing playlist items with asset [id = " + nAssID + "][user = " + _cSI.cDBCredentials.sUser + "]");
			bool bRetVal = true;
			Queue<Hashtable> ahRow;
			try
			{

				//select id from pl."vPlayListResolvedOrdered" where "idAssets" = 1233
				ahRow = _cDB.Select("select id from pl.`vPlayListResolvedOrdered` where `idAssets` = " + nAssID);
				if (null == ahRow || 0 >= ahRow.Count)
				{
					(new Logger()).WriteNotice("there are no pl-items with this asset id: " + nAssID + ", so there is nothing to do.");
					return true;
				}

													//select pl."fItemClassSet"(plr.id, ar."idClasses"), pl."fItemFileSet"(plr.id, ar."idFiles"), pl."fItemTimingsSet"(plr.id, 1,			ar."nFramesQty"), pl."fItemNameSet"(plr.id, ar."sName") from pl."vPlayListResolved" plr, mam."vAssetsResolved" ar where plr."idAssets"=ar.id AND plr."sStatusName"='planned' AND ar.id=    1176
				ahRow = _cDB.Select("select pl.`fItemClassSet`(plr.id, ar.`idClasses`), pl.`fItemFileSet`(plr.id, ar.`idFiles`), pl.`fItemTimingsSet`(plr.id, ar.`nFrameIn`, ar.`nFrameOut`, ar.`nFramesQty`), pl.`fItemNameSet`(plr.id, ar.`sName`) from pl.`vPlayListResolved` plr, mam.`vAssetsResolved` ar where plr.`idAssets`=ar.id AND plr.`sStatusName`='planned' AND ar.id=" + nAssID);
				string sErr = "AssetParametersToPlaylistSave: ";
				if (null != ahRow && 0 < ahRow.Count)
				{
					Hashtable hTab = ahRow.Dequeue();
					foreach (string sStr in hTab.Keys)
					{
						sErr += "[" + sStr + " " + hTab[sStr].ToString() + "] ";
						if (hTab[sStr].ToString().Contains(",f)"))
							bRetVal = false;
					}
					if (!bRetVal)
						(new Logger()).WriteError(new Exception(sErr));
					(new Logger()).WriteNotice("___" + sErr);
				}
				else
				{
					bRetVal = true;
					(new Logger()).WriteNotice("SELECT returned null or 0 rows");
				}
			}
			catch (Exception ex)
			{
				bRetVal = false;
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}

		[WebMethod(EnableSession = true)]
		public long ProgramSave(Program cProgram) //EMERGENCY:l убрать long, возвращать Program
		{
			try
			{
				base.ProgramSave(cProgram);
				return cProgram.nID;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return -1;
			}
		}

		[WebMethod(EnableSession = true)]
		public bool AssetsParentAssign(Asset[] aAsset)
		{
			try
			{
				_cDB.TransactionBegin();
				foreach (Asset cAss in aAsset)
					base.AssetParentSave(cAss);
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
				return false;
			}
			return true;
		}

		[WebMethod(EnableSession = true)]
		new public long AdvertisementSave(Advertisement cAdvertisement) //EMERGENCY:l убрать long, возвращать Advertisement
		{
			try
			{
				base.AdvertisementSave(cAdvertisement);
				return cAdvertisement.nID;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return -1;
			}
		}

		[WebMethod(EnableSession = true)]
		new public long DesignSave(Design cDesign) //EMERGENCY:l убрать long, возвращать Design
		{
			try
			{
				base.DesignSave(cDesign);
				return cDesign.nID;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return -1;
			}
		}

		[WebMethod(EnableSession = true)]
		new public long PersonSave(Person cPerson) //EMERGENCY:l убрать long, возвращать Person
		{
			try
			{
				base.PersonSave(cPerson);
				return cPerson.nID;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return -1;
			}
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair PersonTypeGet(string sPersonTypeFilter)
		{
			return base.PersonTypeGet(webservice.Preferences.cDBKeysMap.GetValueByKey(DBKeysMap.MapClass.person_type, sPersonTypeFilter));
		}

		[WebMethod(EnableSession = true)]
		public Person[] PersonsRemove(Person[] aPersons)
		{                 // возвращает кого не удалила
			List<Person> aRetVal = new List<Person>();
			foreach (Person stPerson in aPersons)
			{
				try
				{
					base.PersonRemove(stPerson);
				}
				catch
				{
					aRetVal.Add(stPerson);
				}
			}
			return aRetVal.ToArray();
		}

		[WebMethod(EnableSession = true)]
		new public Person[] PersonsGet(string sPersonTypeFilter)
		{
			Person[] aRetVal = null;
			try
			{
				IdNamePair cPersonTypeFilter = null;
				if (null != sPersonTypeFilter && 0 < sPersonTypeFilter.Length)
					cPersonTypeFilter = new IdNamePair(webservice.Preferences.cDBKeysMap.GetValueByKey(DBKeysMap.MapClass.person_type, sPersonTypeFilter));
				aRetVal = base.PersonsGet(cPersonTypeFilter).ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		public Person[] ArtistsGet()
		{
			Person[] aRetVal = null;
			try
			{
				string sName = webservice.Preferences.cDBKeysMap.GetValueByKey(DBKeysMap.MapClass.person_type, "artist");
				Queue<Person> aqPersons = PersonsGet(new IdNamePair(sName));
				aRetVal = aqPersons.ToArray();
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		new public Person[] ArtistsLoad(long nAssetID)
		{
			return base.ArtistsLoad(nAssetID);
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair[] StylesGet()
		{
			return base.StylesGet();
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair[] StylesLoad(long nAssetID)
		{
			return base.StylesLoad(nAssetID);
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair[] RotationsGet()
		{
			return base.RotationsGet();
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair[] PalettesGet()
		{
			return base.PalettesGet();
		}

		[WebMethod(EnableSession = true)]
		new public IdNamePair[] SoundsGet()
		{
			return base.SoundsGet();
		}

		[WebMethod(EnableSession = true)]
		new public CustomValue[] CustomsLoad(long nAssetID)
		{
			try
			{
				return base.CustomsLoad(nAssetID);
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return null;
			}
		}

		public class MyInt
		{
			public long nID;
		}

		[WebMethod(EnableSession = true)]
		public string[] ArtistsCueNameGet(MyInt[] aPersonIDs)
		{
			List<string> aRetVal = new List<string>();
			string sName;
			//SELECT "sCue" FROM mam."vPersonsCueLast" WHERE    2222          =id
			foreach (MyInt nPersonID in aPersonIDs)
			{
				try
				{
					sName = _cDB.GetValue("SELECT `sCue` FROM mam.`vPersonsCueLast` WHERE " + nPersonID.nID + "=id");
				}
				catch (Exception ex)
				{
                    WebServiceError.Add(_cSI, ex);
					sName = "";
				}
				sName = null == sName || "" == sName ? "" : sName;
				aRetVal.Add(sName);
			}
			return aRetVal.ToArray();
		}

		//[WebMethod(EnableSession = true)]
		//public Clip[] ClipsInProgramGet(long nAssetID)
		//{ 
		//    return new Clip[4];
		//}
		//[WebMethod(EnableSession = true)]
		//public bool ClipsInProgramSet(Clip[] aClips)
		//{
		//    return false;
		//}

		public void AssetClassChange(int idAssets, int idClasses)
		{
			_cDB.Perform("SELECT mam.`fAssetClassSet`(" + idAssets + "," + idClasses + ")");
		}

		public void AssetVideoTypeChange(int idAssets, int idVideoTypes)
		{
			_cDB.Perform("SELECT mam.`fAssetVideoTypeSet`(" + idAssets + "," + idVideoTypes + ")");
		}

		public void CuesTemplateShow(string sTemplateFile)
		{
			_cDB.Perform("SELECT adm.`fCuesTemplateProcess`('" + sTemplateFile + "', true)");
		}

		public void CuesTemplateHide(string sTemplateFile)
		{
			_cDB.Perform("SELECT adm.`fCuesTemplateProcess`('" + sTemplateFile + "', false)");
		}

		#endregion

		#region media

		[WebMethod(EnableSession = true)]
		new public Storage[] StoragesGet()
		{
			Storage[] aRetVal = null;
			Queue<Storage> aqStorages = null;
			try
			{
				if (null != (aqStorages = base.StoragesGet()))
					aRetVal = aqStorages.ToArray();
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
		new public File[] FilesGet(long nStorageID)
		{
			File[] aRetVal = null;
			Dictionary<long, File> aFiles = null;
			try
			{
				if (null != (aFiles = base.FilesGet(nStorageID)))
					aRetVal = aFiles.Values.ToArray();
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
		public long FileDurationQuery(long nFileID)
		{
			try
			{
				//_cDB.RoleSet("replica_adm");
				Hashtable ahRow = _cDB.GetRow("SELECT `bValue`, `nValue` FROM adm.`fFileDurationGet`(" + nFileID + ")");
				if (ahRow["bValue"].ToBool())
				{
					return ahRow["nValue"].ToID();
				}
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return -1;
		}
		[WebMethod(EnableSession = true)]
		public IdNamePair CommandStatusGet(long nCommandsQueueID)
		{
			try
			{
				//_cDB.RoleSet("replica_adm");
				return new IdNamePair(_cDB.GetRow("SELECT * FROM adm.`fCommandsQueueStatusGet`(" + nCommandsQueueID + ")"));
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public string FramesQtyGet(long nCommandsQueueID)
		{
			try
			{
				//_cDB.RoleSet("replica_adm");
				return _cDB.GetValue("SELECT `sValue` FROM adm.`fCommandParametersGet`(" + nCommandsQueueID + ", 'nFramesQty')");
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public File[] StorageFilesUnusedGet(long nStorageID)
		{
			List<File> aRetVal = new List<File>();
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT * FROM media.`vFilesUnused` WHERE `idStorages`=" + nStorageID + " order by `sFilename`")))
				{
					while (0 < aqDBValues.Count)
						aRetVal.Add(new File(aqDBValues.Dequeue()));
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
				return null;
			}
			return aRetVal.ToArray();
		}
		#endregion

		#region ingest

		[WebMethod(EnableSession = true)]
        public File Ingest(File.Ingest cInfo)
		{
            File cRetVal = null;
            try
            {
				TransactionBegin();
                if (null != cInfo)
                {
                    List<string> aFieldsFailed = new List<string>();
                    //_cDB.RoleSet("replica_ingest");
					string sFilename = SIO.Path.GetFileName(cInfo.sFilename);
                    if (null == (cRetVal = File.Load(cInfo.cStorage, sFilename)))
                        cRetVal = File.Create(cInfo.cStorage, sFilename);

                    string sSQL = "SELECT `bValue` FROM media.`fFileAttributeSet`(:idFiles, ";
                    string sSQLForInt = sSQL + ":sKey, :nValue::int)";
                    string sSQLForString = sSQL + ":sKey, :sValue)";
                    string sSQLForBool = sSQL + ":sKey, 1)";
					string sSQLForRT = "SELECT `bValue` FROM media.`fFileAttributeSet`(:idFiles, :idRegisteredTables::int, :sKey, :nValue::int)";
                    Dictionary<string, object> ahParams = new Dictionary<string, object>();
                    ahParams.Add("idFiles", cRetVal.nID);
                    ahParams.Add("sKey", null);
                    ahParams.Add("nValue", null);
                    ahParams.Add("sValue", null);

                    ahParams["sKey"] = "source";
                    ahParams["sValue"] = cInfo.sOriginalFile;
                    if (!_cDB.GetValueBool(sSQLForString, ahParams))
                        aFieldsFailed.Add("sOriginalFile");

                    ahParams["sKey"] = "age";
                    ahParams["nValue"] = cInfo.nAge;
                    if (!_cDB.GetValueBool(sSQLForInt, ahParams))
                        aFieldsFailed.Add("nAge");

                    if (cInfo.bBroadcast)
                    {
                        ahParams["sKey"] = "broadcast";
                        if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                            aFieldsFailed.Add("bBroadcast");
                    }
                    if (null != cInfo.nVersion)
                    {
                        ahParams["sKey"] = "version";
                        ahParams["nValue"] = cInfo.nVersion;
                        if (!_cDB.GetValueBool(sSQLForInt, ahParams))
                            aFieldsFailed.Add("nVersion");
                    }

                    ahParams["sKey"] = "format";
                    ahParams["nValue"] = cInfo.nFormat;
                    if (!_cDB.GetValueBool(sSQLForInt, ahParams))
                        aFieldsFailed.Add("nFormat");

                    ahParams["sKey"] = "fps";
                    ahParams["nValue"] = cInfo.nFPS;
                    if (!_cDB.GetValueBool(sSQLForInt, ahParams))
                        aFieldsFailed.Add("nFPS");

                    if (cInfo is File.Ingest.Clip)
                    {
                        File.Ingest.Clip cInfoClip = (File.Ingest.Clip)cInfo;
                        ahParams["sKey"] = "person";
                        ahParams["idRegisteredTables"] = RegisteredTableGet("mam", "tPersons").nID;
                        _cDB.Perform("SELECT * FROM media.`fFileAttributeRemove`(:idFiles, :idRegisteredTables::int, :sKey)", ahParams);
                        foreach (Person cPerson in cInfoClip.aArtists)
                        {
                            ahParams["nValue"] = cPerson.nID;
                            if (!_cDB.GetValueBool("SELECT `bValue` FROM media.`fFileAttributeAdd`(:idFiles, :idRegisteredTables::int, :sKey, :nValue::int)", ahParams))
                                aFieldsFailed.Add("aArtists:" + cPerson.nID);
                        }

                        ahParams["sKey"] = "song";
                        ahParams["sValue"] = cInfoClip.sSongName;
                        if (!_cDB.GetValueBool(sSQLForString, ahParams))
                            aFieldsFailed.Add("sSongName");

                        ahParams["sKey"] = "quality";
                        ahParams["nValue"] = cInfoClip.nQuality;
                        if (!_cDB.GetValueBool(sSQLForInt, ahParams))
                            aFieldsFailed.Add("nQuality");

                        if (cInfoClip.bLocation)
                        {
                            ahParams["sKey"] = "location";
                            if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                                aFieldsFailed.Add("bLocation");
                        }

                        if (cInfoClip.bRemix)
                        {
                            ahParams["sKey"] = "remix";
                            if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                                aFieldsFailed.Add("bRemix");
                        }

                        if (cInfoClip.bPromo)
                        {
                            ahParams["sKey"] = "promo";
                            if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                                aFieldsFailed.Add("bPromo");
                        }

                        if (cInfoClip.bCutted)
                        {
                            ahParams["sKey"] = "cutted";
                            if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                                aFieldsFailed.Add("bCutted");
                        }

                        if (cInfoClip.bForeign)
                        {
                            ahParams["sKey"] = "foreign";
                            if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                                aFieldsFailed.Add("bForeign");
                        }

                    }
                    else if(cInfo is File.Ingest.Advertisement)
                    {
                        File.Ingest.Advertisement cInfoAdvertisement = (File.Ingest.Advertisement)cInfo;

						ahParams["sKey"] = "company";
						ahParams["sValue"] = cInfoAdvertisement.sCompany;
						if (!_cDB.GetValueBool(sSQLForString, ahParams))
							aFieldsFailed.Add("sCompany");

						ahParams["sKey"] = "campaign";
						ahParams["sValue"] = cInfoAdvertisement.sCampaign;
						if (!_cDB.GetValueBool(sSQLForString, ahParams))
							aFieldsFailed.Add("sCampaign");
						
						if (!cInfoAdvertisement.sID.IsNullOrEmpty())
                        {
                            ahParams["sKey"] = "id";
                            ahParams["sValue"] = cInfoAdvertisement.sID;
                            if (!_cDB.GetValueBool(sSQLForString, ahParams))
                                aFieldsFailed.Add("sID");
                        }
                    }
                    else if(cInfo is File.Ingest.Program)
                    {
                        File.Ingest.Program cInfoProgram = (File.Ingest.Program)cInfo;

                        ahParams["idRegisteredTables"] = RegisteredTableGet("mam", "tAssets").nID;
                        ahParams["sKey"] = "series";
                        ahParams["nValue"] = cInfoProgram.cSeries.nID;
                        if (!_cDB.GetValueBool(sSQLForRT, ahParams))
                            aFieldsFailed.Add("cSeries");

                        ahParams["sKey"] = "episode";
                        ahParams["nValue"] = cInfoProgram.cEpisode.nID;
                        if (!_cDB.GetValueBool(sSQLForRT, ahParams))
                            aFieldsFailed.Add("cEpisode");

                        if (0 < cInfoProgram.nPart)
                        {
                            ahParams["sKey"] = "id";
                            ahParams["nValue"] = cInfoProgram.nPart;
                            if (!_cDB.GetValueBool(sSQLForInt, ahParams))
                                aFieldsFailed.Add("nPart");
                        }
                    }
                    else if(cInfo is File.Ingest.Design)
                    {
                        File.Ingest.Design cInfoDesign = (File.Ingest.Design)cInfo;

						if (null != cInfoDesign.sSeason)
						{
							ahParams["sKey"] = "season";
							ahParams["sValue"] = cInfoDesign.sSeason;
							if (!_cDB.GetValueBool(sSQLForString, ahParams))
								aFieldsFailed.Add("sSeason");
						}
						if (null != cInfoDesign.sType)
						{
							ahParams["sKey"] = "type";
							ahParams["sValue"] = cInfoDesign.sType;
							if (!_cDB.GetValueBool(sSQLForString, ahParams))
								aFieldsFailed.Add("sType");
						}
						if (cInfoDesign.bDTMF)
                        {
                            ahParams["sKey"] = "dtmf";
                            if (!_cDB.GetValueBool(sSQLForBool, ahParams))
                                aFieldsFailed.Add("bDTMF");
                        }

                        ahParams["sKey"] = "name";
                        ahParams["sValue"] = cInfoDesign.sName;
                        if (!_cDB.GetValueBool(sSQLForString, ahParams))
                            aFieldsFailed.Add("sName");
                    }
					if (0 < aFieldsFailed.Count)
					{
						TransactionRollBack();
						WebServiceError.Add(_cSI, "errors occurred while processing next fields:" + string.Join(",", aFieldsFailed));
					}
					else
						TransactionCommit();
                }
            }
            catch (Exception ex)
            {
				TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
            }
            return cRetVal;
		}
		#endregion

		#region pl
		public bool IsPlaylistUpdated()
		{
			//_cDB.RoleSet("replica_playlist_full");
			DateTime dtPLUpdated = _cDB.GetValueRaw("SELECT * FROM hk.`fRegisteredTableGetUpdated`('pl','tItems')").ToDT();
			if (dtPLUpdated != _dtPLUpdated)
			{
				_dtPLUpdated = dtPLUpdated;
				return true;
			}
			return false;
		}

		public void PLICurrentSkip(long nID)
		{
			//_cDB.RoleSet("replica_playlist_full");
			_cDB.Perform("SELECT adm.`fPlayerSkip`(" + nID + ")");
		}

		public bool PLIClassChange(long nItemID, long nClassID)
		{
			try
			{
				Hashtable ahRes = _cDB.Select("SELECT pl.`fItemClassSet`(" + nItemID + "," + nClassID + ")").Dequeue();
				string sRes = (string)ahRes["fItemClassSet"];
				if ('t' == sRes[sRes.Length - 2])
					return true;
				else
					return false;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return false;
			}
		}
		private int SetCached(PlaylistItem[] aPLIs)
		{
			List<long> aCached = PlaylistItemIDsCachedGet();
			int nRetVal = 0;
			foreach (PlaylistItem cPL in aPLIs)
				if (aCached.Contains(cPL.nID))
				{
					cPL.bCached = true;
					nRetVal++;
				}
				else
					cPL.bCached = false;
			return nRetVal;
		}
		[WebMethod(EnableSession = true)]
		new public PlaylistItem[] PlaylistItemsGet(IdNamePair[] aStatuses)
		{
			List<PlaylistItem> aPLIs = new List<PlaylistItem>();
			aPLIs.AddRange(base.PlaylistItemsWithRotationsGet(aStatuses).ToArray());
	
			//DateTime dtBegin = DateTime.Now;
			//DateTime dtEnd = DateTime.Now.AddHours(3);
			//Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT *, ass.`nValue` AS `nRotation`, cat.`sValue` AS `sRotation` FROM (SELECT DISTINCT * FROM pl.`vPlayListResolved` WHERE `dtStartPlanned` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStartPlanned` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `sStatusName` = 'planned') pl LEFT JOIN (SELECT `idAssets`, `nValue` FROM mam.`tAssetAttributes` WHERE mam.`tAssetAttributes`.`sKey`='rotation') ass ON pl.`idAssets`= ass.`idAssets` LEFT JOIN (SELECT id AS `nCatValID`, `sValue` FROM mam.`tCategoryValues`) cat ON cat.`nCatValID`=ass.`nValue` ORDER BY `dtStartReal`,`dtStartQueued`,`dtStartPlanned`");
			//Hashtable ahRow;
			//PlaylistItem cPLI;
			//LinkedList<PlaylistItem> aPLIsPlan = new LinkedList<PlaylistItem>();
			//while (0 < aqDBValues.Count)
			//{
			//    ahRow = aqDBValues.Dequeue();
			//    cPLI = new PlaylistItem(ahRow);
			//    if (ahRow.ContainsKey("nRotation") && ahRow.ContainsKey("sRotation") && null != ahRow["nRotation"] && null != cPLI.cAsset)
			//            cPLI.cAsset = new Clip() { nID = cPLI.cAsset.nID, cRotation = new IdNamePair() { nID = ahRow["nRotation"].ToID(), sName = ahRow["sRotation"].ToString() } };
			//    aPLIsPlan.AddLast(cPLI);
			//}
			//SetCached(aPLIsPlan.ToArray());
			//List<PlaylistItem> aPlugsInTheEnd = new List<PlaylistItem>();
			//foreach (PlaylistItem cPlan in aPLIsPlan)
			//    if (cPlan.bCached)
			//    {
			//        aPLIs.Add(cPlan);
			//        if (cPlan.bPlug)
			//            aPlugsInTheEnd.Add(cPlan);
			//        else if (0 < aPlugsInTheEnd.Count())
			//            aPlugsInTheEnd.Clear();
			//    }
			//foreach (PlaylistItem cPlug in aPlugsInTheEnd)  // убираем плаги в конце - они пусть попадут в список пленнед...
			//    aPLIs.Remove(cPlug);

			return aPLIs.ToArray();
		}

		[WebMethod(EnableSession = true)]
		public PlaylistItem[] PlaylistItemsArchGet(DateTime dtBegin, DateTime dtEnd)
		{
			PlaylistItem[] aPLIs = base.PlaylistItemsWithRotationsArchGet(dtBegin, dtEnd).ToArray();
			//foreach (PlaylistItem cPlan in aPLIs)
			//    cPlan.RotationSet();
			return aPLIs;
		}

		[WebMethod(EnableSession = true)]
		new public PlaylistItem[] PlaylistItemsPlanGet(DateTime dtBegin, DateTime dtEnd)
		{
			long nStart = DateTime.Now.Ticks;
			List<PlaylistItem> aPLIs = new List<PlaylistItem>();
			PlaylistItem[] aPlan = base.PlaylistItemsWithRotationsPlanGet(dtBegin, dtEnd).ToArray();
			List<PlaylistItem> aPlugsInTheEndOfCached = new List<PlaylistItem>();
			double nDelta3 = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			int nCashed = SetCached(aPlan);
			foreach (PlaylistItem cPlan in aPlan)
			{
				if (cPlan.bCached)
				{
					if (cPlan.bPlug)
						aPlugsInTheEndOfCached.Add(cPlan);
					else if (0 < aPlugsInTheEndOfCached.Count())
						aPlugsInTheEndOfCached.Clear();
					nCashed--;
					if (0 == nCashed)
						break;
				}
			}
			double nDelta4 = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			aPLIs.AddRange(aPlan);
			Queue<Hashtable> aqDBValues = null;
			Dictionary<long, List<long>> ahItemsInserted = new Dictionary<long, List<long>>();
			Hashtable ahRow = null;
			List<PlaylistItem> aAllInserted = new List<PlaylistItem>();
			List<long> aAttachedInserted = new List<long>();
			List<PlaylistItem> aLostInserted = new List<PlaylistItem>();
			DateTime dtHardSoft;
			double nDelta5 = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			if (null != (aqDBValues = _cDB.Select("SELECT `nPrecedingID`, id FROM pl.`vItemsInserted` ORDER BY `nPrecedingID`, `nPrecedingOffset`;")))
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
					aAttachedInserted.Add(nID);
				}
			}
			double nDelta6 = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			foreach (PlaylistItem cPlan in aPlan)
			{
				//if (cPlan.bCached && !aPlugsInTheEndOfCached.Contains(cPlan))   // вносим обратно кэшеды в пленнед...
				//{
				//    aPLIs.Remove(cPlan);
				//    continue;
				//}
				if (DateTime.MaxValue == cPlan.dtTimingsUpdate)
					cPlan.dtTimingsUpdate = DateTime.MinValue;
				if (DateTime.MinValue == cPlan.dtTimingsUpdate && DateTime.MaxValue == cPlan.dtStartPlanned)
				{
					aPLIs.Remove(cPlan);
					aAllInserted.Add(cPlan);
					if (!aAttachedInserted.Contains(cPlan.nID))
						aLostInserted.Add(cPlan);
				}
			}
			double nDelta7 = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			aPLIs = aPLIs.Where(o => o.nID > 0).OrderBy(o => o.dtStartPlanned).ToList();   //.OrderByDescending(o => o.dtTimingsUpdate).ThenBy(o => o.dtStartPlanned).ToList();
			List<PlaylistItem> aRetVal = new List<PlaylistItem>();
			PlaylistItem cLastItemInBlock = null;
			List<long> aFolowedItemIDsForLastItemInBlock = new List<long>();

			double nDelta = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			if (0 == aAllInserted.Count)
				return aPLIs.ToArray();
			for (int nI = 0; aPLIs.Count > nI; nI++)    // есть inserted...
			{
				aRetVal.Add(aPLIs[nI]); // туда сложим все элементы aPLIs + все inserted...
				int nJ = 0;
				while (aLostInserted.Count > nJ)  // перебираем каждый раз потерянные inserted...
				{
					if (aPLIs.Count > nI + 1 && aPLIs[nI].dtStartPlanned < aLostInserted[nJ].dtStartPlanned && aPLIs[nI + 1].dtStartPlanned >= aLostInserted[nJ].dtStartPlanned)
					{
						if (!ahItemsInserted.ContainsKey(aPLIs[nI].nID))
							ahItemsInserted.Add(aPLIs[nI].nID, new List<long>());
						ahItemsInserted[aPLIs[nI].nID].Add(aLostInserted[nJ].nID);
					}
				}
				if (ahItemsInserted.ContainsKey(aPLIs[nI].nID)) //если он оказался хозяином очереди...
				{
					dtHardSoft = DateTime.MaxValue > aPLIs[nI].dtStartHard ? aPLIs[nI].dtStartHard : aPLIs[nI].dtStartSoft;
					if (DateTime.MaxValue == dtHardSoft) // не (рекламный) блок, короче...
						aRetVal.AddRange(GetItemsOnIDs(aAllInserted, ahItemsInserted[aPLIs[nI].nID]));
					else  // если мы внутри рек блока....
					{
						cLastItemInBlock = LastItemInBlock(aPLIs, nI);
						aFolowedItemIDsForLastItemInBlock.AddRange(ahItemsInserted[aPLIs[nI].nID]);  // что б не отображался внутри блока....
					}
				}
				if (null != cLastItemInBlock && cLastItemInBlock == aPLIs[nI])   // конец блока и пора вставить накопившееся внутри блока....
				{
					aRetVal.AddRange(GetItemsOnIDs(aAllInserted, aFolowedItemIDsForLastItemInBlock));
					cLastItemInBlock = null;
				}
			}
			double nDelta2 = (new TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
			return aRetVal.ToArray();
		}
		private List<PlaylistItem> GetItemsOnIDs(List<PlaylistItem> aPLIs, List<long> aIDs)
		{
			return aPLIs.Where(o => aIDs.Contains(o.nID)).ToList();
		}
		private PlaylistItem LastItemInBlock(List<PlaylistItem> aPLIs, int nBlockPLIIndex)
		{
			int nPrevInd = nBlockPLIIndex;
			int nNextInd = nPrevInd;
			DateTime dtPrevHardSoft = DateTime.MaxValue > aPLIs[nPrevInd].dtStartHard ? aPLIs[nPrevInd].dtStartHard : aPLIs[nPrevInd].dtStartSoft;
			DateTime dtNextHardSoft = dtPrevHardSoft;

			while ((nNextInd == nPrevInd || 1 == dtNextHardSoft.Subtract(dtPrevHardSoft).TotalSeconds) && aPLIs.Count > nNextInd)
			{
				dtPrevHardSoft = dtNextHardSoft;
				nPrevInd = nNextInd;
				while (aPLIs.Count > nNextInd)
				{
					nNextInd++;
					dtNextHardSoft = DateTime.MaxValue > aPLIs[nNextInd].dtStartHard ? aPLIs[nNextInd].dtStartHard : aPLIs[nNextInd].dtStartSoft;
					if (DateTime.MaxValue > dtNextHardSoft)
						break;
				}
			}
			return aPLIs[nPrevInd];
		}

		[WebMethod(EnableSession = true)]
		new public PlaylistItem[] PlaylistItemsAdvertsGet(DateTime dtBegin, DateTime dtEnd)
		{
			try
			{
				PlaylistItem[] aPLIs = base.PlaylistItemsAdvertsGet(dtBegin, dtEnd).ToArray();
				return aPLIs;
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		private DateTime HardSoftGet(PlaylistItem cPLI)
		{
			return DateTime.MaxValue > cPLI.dtStartHard ? cPLI.dtStartHard : cPLI.dtStartSoft;
		}
		[WebMethod(EnableSession = true)]
		public DateTime NearestAdvertsBlock()
		{
			//return DateTime.Now.AddSeconds(180);
			dtNearestAdvertsBlock = DateTime.MinValue;
			(new Logger()).WriteDebug2("in [user:si:" + _cSI.cProfile.sUsername + "][user:db:" + _cDB.sUserName);
			try
			{
				//PlaylistItem[] aPLI = base.PlaylistItemsAdvertsGet(DateTime.Now.AddMinutes(-30), DateTime.Now.AddHours(1)).ToArray(); //не актуальна пока нет пересчета.....
				PlaylistItem[] aPLI = base.PlaylistItemsFastGet(DateTime.Now.AddMinutes(-30), DateTime.Now.AddHours(1)).ToArray();
				List<PlaylistItem> aPLIAdvertsStarts = new List<PlaylistItem>();
				PlaylistItem cPLIOnAir = base.PlaylistItemOnAirGet();
				DateTime dtNextBlock;
				if (DateTime.MaxValue > HardSoftGet(cPLIOnAir))
					dtNearestAdvertsBlock = DateTime.MaxValue;
				for (int ni = 1; aPLI.Length > ni; ni++)
				{
					if (
							DateTime.MaxValue > HardSoftGet(aPLI[ni]) 
							&& DateTime.MaxValue == HardSoftGet(aPLI[ni - 1]) 
							&& DateTime.MaxValue == aPLI[ni].dtStartReal 
							&& (aPLI[ni].cStatus.sName != "skipped" && aPLI[ni].cStatus.sName != "failed" && aPLI[ni].cStatus.sName != "played")
						)
					{
						if (DateTime.MaxValue > aPLI[ni].dtStartHard)
							dtNextBlock = aPLI[ni].dtStartHard;
						else
							dtNextBlock = aPLI[ni - 1].dtStartPlanned.AddSeconds((aPLI[ni - 1].nFrameStop - aPLI[ni - 1].nFrameStart + 1) / 25);
						//if (DateTime.MaxValue > dtNearestAdvertsBlock)
						dtNearestAdvertsBlock = dtNextBlock;
						(new Logger()).WriteNotice("nearest_block: " + dtNearestAdvertsBlock);
						return dtNextBlock;
					}
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return DateTime.Now.AddHours(3); // на эфире сразу станет ясно, что произошла хуйня - ближайший блок не найден
		}
		[WebMethod(EnableSession = true)]
		public int PlaylistItemsDeleteSince(DateTime dtBegin)
		{
			PlaylistItem[] aPlan = base.PlaylistItemsPlanGet(dtBegin, DateTime.MaxValue).ToArray();
			if (0 < SetCached(aPlan))
				return -1;
			int nRetVal = 0;
			_cDB.TransactionBegin();
			foreach (PlaylistItem cPLI in aPlan)
			{
				try
				{
					if (!base.PlaylistItemDelete(cPLI.nID))
						nRetVal++;
				}
				catch
				{
					nRetVal++;
				}
			}
			_cDB.TransactionCommit();
			return nRetVal;
		}
		[WebMethod(EnableSession = true)]
		public IdNamePair[] PlaylistItemsDelete(IdNamePair[] aIDs)
		{
			if (null == aIDs || 1 > aIDs.Length)
				return null;
			List<IdNamePair> aRetVal = new List<IdNamePair>();
			_cDB.TransactionBegin();
			foreach (IdNamePair cID in aIDs)
			{
				try
				{
					if (!base.PlaylistItemDelete(cID.nID))
						aRetVal.Add(cID);
				}
				catch
				{
					aRetVal.Add(cID);
				}
			}
			_cDB.TransactionCommit();
			return aRetVal.ToArray();
		}
		[WebMethod(EnableSession = true)]
		public DateTime PlaylistItemStartsSet(long nItemID, DateTime dtStartPlanned, DateTime dtOld)
		{
			try
			{
				(new Logger()).WriteDebug3("in [sp:" + dtStartPlanned.ToStr() + "][old:" + dtOld.ToStr() + "]");
				Hashtable ahRow = _cDB.Select("SELECT * FROM pl.`tItems` LEFT JOIN pl.`tItemDTEvents` ON pl.`tItems`.id = pl.`tItemDTEvents`.`idItems` WHERE `idStatuses`=1 AND `idClasses` IN (2,5,6,7,8,9,12,13) AND dt>'" + DateTime.Now.ToStr() + "' ORDER BY dt LIMIT 1").Dequeue(); // первый пленнед клип
				//SELECT * FROM pl."tItems" LEFT JOIN pl."tItemDTEvents" ON pl."tItems".id = pl."tItemDTEvents"."idItems" WHERE "idStatuses"=1 AND "idClasses" IN (2,5,6,7,8,9,12,13) AND dt>'2011-10-26 17:42:37+04' ORDER BY dt LIMIT 1
				DateTime dtFirstPlanned = ahRow["dt"].ToDT();
				dtFirstPlanned = dtFirstPlanned.AddMinutes(1); //зазорчик
				(new Logger()).WriteDebug4("middle [sp:" + dtFirstPlanned.ToStr() + "]");
				if (dtFirstPlanned > dtOld)
					return DateTime.MaxValue;
				if (dtFirstPlanned > dtStartPlanned)
					dtStartPlanned = dtFirstPlanned;
				base.PlaylistItemStartsSet(nItemID, dtStartPlanned, DateTime.MaxValue, DateTime.MaxValue);
				(new Logger()).WriteDebug4("return [sp:" + dtStartPlanned.ToStr() + "]");
				return dtStartPlanned;
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return DateTime.MaxValue;
		}
		[WebMethod(EnableSession = true)]
		public long PlaylistRecalculateQuery(long nPLitemsID, ushort nHoursQty) //EMERGENCY:l убрать ID'шки, передавать и возвращать классы
		{
			_cDB.TransactionBegin();
			try
			{
				Hashtable ahRow = _cDB.GetRow("SELECT `bValue`, `nValue` FROM adm.`fCommandsQueueAdd`('playlist_recalculate')");
				nHoursQty = ushort.MaxValue == nHoursQty ? (ushort)0 : nHoursQty;
				long nCommandID = ahRow["nValue"].ToID();

				if (ahRow["bValue"].ToBool())
				{
					if (
						_cDB.GetValueBool("SELECT `bValue` FROM adm.`fCommandParameterAdd` (" + nCommandID + ", 'nPLitemsID', '" + nPLitemsID + "')")
						&& _cDB.GetValueBool("SELECT `bValue` FROM adm.`fCommandParameterAdd` (" + nCommandID + ", 'nHoursQty', '" + nHoursQty + "')")
					)
					{
						_cDB.TransactionCommit();
						return nCommandID;
					}
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			_cDB.TransactionRollBack();
			return -1;
		}
		private static string _sPlaylistItemAddResult;
		private static PlaylistItem[] _aPLIs = new PlaylistItem[0];
		private static DB.Credentials _cCredentialsForAdd;
		private static void PlaylistItemAddWorker(object cState)
		{
			try
			{
				webservice.DBInteract cDBI = new webservice.DBInteract(_cCredentialsForAdd.sUser, _cCredentialsForAdd.sPassword);
				cDBI.PlaylistItemsAdd(_aPLIs);
				_sPlaylistItemAddResult = "succeed";
				(new Logger()).WriteNotice("PlaylistItemsAdd:succeed");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				_sPlaylistItemAddResult = "failed" + ex.Message;
			}
		}
		[WebMethod(EnableSession = true)]
		public string PlaylistItemAdd_ResultGet()
		{
			string sRetVal = _sPlaylistItemAddResult;
			if ("succeed" == _sPlaylistItemAddResult || _sPlaylistItemAddResult.StartsWith("failed"))
			{
				_sPlaylistItemAddResult = null;
				_aPLIs = new PlaylistItem[0];
			}
			return sRetVal;
		}
		[WebMethod(EnableSession = true)]
		new public void PlaylistItemsAdd(PlaylistItem[] aPLIs)
		{
			lock (_aPLIs)
			{
				if (_sPlaylistItemAddResult == null)
				{
					(new Logger()).WriteNotice("PlaylistItemsAdd:proccessing");
					_sPlaylistItemAddResult = "proccessing";
					_aPLIs = aPLIs;
					_cCredentialsForAdd = _cSI.cDBCredentials;
					System.Threading.ThreadPool.QueueUserWorkItem(PlaylistItemAddWorker);
				}
			}
		}
		[WebMethod(EnableSession = true)]
		public PlaylistItem[] ComingUpGet()
		{
			PlaylistItem[] aRetVal = null;
			try
			{
				Queue<PlaylistItem> ahComingUp = base.ComingUpGet(0, 2); // есть еще метод ComingUpWithAssetsResolvedGet
				if (null != ahComingUp && 2 == ahComingUp.Count)
				{
					aRetVal = new PlaylistItem[2] { ahComingUp.Dequeue(), ahComingUp.Dequeue() };
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
        public PlaylistItem[] PlaylistInsert(Asset[] aAssets, PlaylistItem cPLIPreceding)
		{
            return aAssets.Select(o => base.PlaylistInsert(o, cPLIPreceding)).ToArray();
		}
		[WebMethod(EnableSession = true)]
		public PlaylistItem[] PlaylistInsertCopies(Asset[] aAssets, PlaylistItem cPLIPreceding, int nCopiesQty)
		{
			List<Asset> aAllAssets = new List<Asset>();
			for (int ni = 0; ni < nCopiesQty; ni++)
				aAllAssets.AddRange(aAssets);
			return PlaylistInsert(aAllAssets.ToArray(), cPLIPreceding);
		}
		[WebMethod(EnableSession = true)]
		public string GroupMoving(PlaylistItem[] aPLIs)
		{
			string sLog = "";
			bool bRes;
			PlaylistItem cPLICached;
			Queue<PlaylistItem> aqPLI;

            if (null == aPLIs || 1 > aPLIs.Length)
                return g.Webservice.sErrorDBInteract3;

			try
			{
				List<long> aCached = base.PlaylistItemIDsCachedGet();
				if (null != (cPLICached = aPLIs.FirstOrDefault(o => aCached.Contains(o.nID))))
                    return g.Webservice.sErrorDBInteract4 + " [id: " + cPLICached.nID + "] [name: " + cPLICached.sName + "]";

				string sBegin = aPLIs[0].dtStartHardSoft.ToString("yyyy-MM-dd HH:mm:ss");
				string sEnd = aPLIs[aPLIs.Length - 1].dtStartHardSoft.AddSeconds(5).ToString("yyyy-MM-dd HH:mm:ss");

                if (aPLIs[aPLIs.Length - 1].dtStartHardSoft < DateTime.Now)
                    return g.Webservice.sErrorDBInteract5 + " [begin: " + sBegin + "] [end: " + sEnd + "]";

				aqPLI = base.PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `dtStartPlanned`<'" + sEnd + "' AND `dtStopPlanned`>'" + sBegin + "'");
				if (0 < aqPLI.Count && null != aqPLI.FirstOrDefault(o => aCached.Contains(o.nID)))
                    return g.Webservice.sErrorDBInteract6 + " [begin: " + sBegin + "] [end: " + sEnd + "]";

				//DNF - разкомментить это всё!!
				//aqPLI = base.PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `dtStartHard`>'" + sBegin + "' AND `dtStartHard`<'" + sEnd + "' OR `dtStartSoft`>'" + sBegin + "' AND `dtStartSoft`<'" + sEnd + "'");
				//if (0 < aqPLI.Count && null != aqPLI.FirstOrDefault(o => null == aPLIs.FirstOrDefault(oo => oo.nID == o.nID))) // перемещать сами в себя можно, но не в других
                //    return "Целевой диапазон перемещения содержит другие блоки: [begin: " + sBegin + "] [end: " + sEnd + "]";  //TODO LANG
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return ex.Message + ex.InnerException == null ? "" : Environment.NewLine + ex.InnerException.Message;
			}

			_cDB.TransactionBegin();
			try
			{
				foreach (PlaylistItem cPLI in aPLIs)
				{
					sLog += cPLI.nID + " | " + cPLI.sName + " | " + cPLI.dtStartHard + " | " + cPLI.dtStartSoft + "<br>";
					_cDB.Perform("DELETE FROM pl.`tItemDTEvents` where `idItems` = " + cPLI.nID + " and `idItemDTEventTypes` in (2,3);");
					bRes = _cDB.GetValueBool("SELECT `bValue` FROM  pl.`fItemDTEventSet`(" + (DateTime.MaxValue > cPLI.dtStartHard ? 2 : 3) + ", " + cPLI.nID + ", '" + cPLI.dtStartHardSoft.ToString("yyyy-MM-dd HH:mm:ss") + "');");
					if (!bRes)
                        throw new Exception("GroupMoving: " + g.Common.sErrorDataSave + ": pl.`fItemDTEventSet`");
				}
				_cDB.TransactionCommit();
				(new Logger()).WriteNotice("GroupMoving: <br>ID | Name | Hard | Soft <br>" + sLog);
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				_cDB.TransactionRollBack();
				return ex.Message + ex.InnerException == null ? "" : Environment.NewLine + ex.InnerException.Message;
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public bool PLIPropertiesSet(PlaylistItem cPLI)
		{
			bool bRetVal = false;
			_cDB.TransactionBegin();
			try
			{
				if (PLIClassChange(cPLI.nID, cPLI.cClass.nID))
					bRetVal = true;

				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				bRetVal = false;
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public PlaylistItem PlaylistLastElementGet()
		{
			PlaylistItem cRetVal=null;
			Queue<PlaylistItem> ahLastPLI = base.PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` limit 1  offset (select count(id) from pl.`vPlayListResolvedOrdered` limit 1) - 1;");
			//                     SELECT * FROM pl."vPlayListResolvedOrdered" limit 1  offset (select count(id) from pl."vPlayListResolvedOrdered" limit 1) - 1;
			if (null != ahLastPLI && 0 < ahLastPLI.Count)
				cRetVal = ahLastPLI.Dequeue();
			return cRetVal;
		}
		[WebMethod(EnableSession = true)]
		public bool BeforeAddCheckRange(DateTime dtBegin, DateTime dtEnd)
		{
			bool bRetVal = false;
			Queue<Hashtable> aqDBValues = null;
			//select id from pl."vPlayListResolvedOrdered" where "dtStartPlanned">'"2014-07-25 01:07:29+04"' AND "dtStartPlanned"<'"2014-07-25 04:07:29+04"';
			//aqDBValues = _cDB.Select("select id from pl.`vPlayListResolvedOrdered` where `dtStartPlanned`>'" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStartPlanned`<'" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "';");
			//select id from pl."vPlayListResolvedOrdered" where "dtStartHard">'2014-09-12 23:07:29+04' AND "dtStartHard"<'2014-09-13 04:07:29+04' OR "dtStartSoft">'2014-09-12 23:07:29+04' AND "dtStartSoft"<'2014-09-13 04:07:29+04';

			(new Logger()).WriteNotice("BeforeAddCheckRange - [begin = " + dtBegin + "] [end = " + dtEnd + "]");

			//select "dtStopPlanned" from pl."vPlayListResolved" where "sStatusName"='onair' or "sStatusName"='prepared' or "sStatusName"='queued' or "sStatusName"='planned' and id in (select "idItems" from pl."tItemsCached") order by "dtStopPlanned" desc limit 1
			string sCachedEnd = _cDB.GetValue("select `dtStopPlanned` from pl.`vPlayListResolved` where `sStatusName`='onair' or `sStatusName`='prepared' or `sStatusName`='queued' or `sStatusName`='planned' and id in (select `idItems` from pl.`tItemsCached`) order by `dtStopPlanned` desc limit 1");
			DateTime dtCachedEnd = DateTime.Now;
			if (null != sCachedEnd)
				dtCachedEnd = sCachedEnd.ToDT();
			if (dtBegin < dtCachedEnd)
			{
				(new Logger()).WriteNotice("BeforeAddCheckRange - there are cached or queued items in range [cached_or_queued_end = " + dtCachedEnd + "]");
				return false;
			}

			aqDBValues = _cDB.Select("select id from pl.`vPlayListResolvedOrdered` where `dtStartHard`>'" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStartHard`<'" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' OR `dtStartSoft`>'" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStartSoft`<'" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "';");
			Hashtable hPLI;
			if (null == aqDBValues || 0 == aqDBValues.Count)
				bRetVal = true;
			else
			{
				hPLI = aqDBValues.Peek();
				(new Logger()).WriteNotice("BeforeAddCheckRange - there is timed item in range [id[0]=" + hPLI["id"] + "]");
			}
			return bRetVal;
		}

		#region import
		[WebMethod(EnableSession = true)]
		public int PowerGoldFileParse(string sFile)
		{
			try
			{
				PlaylistImport cPlaylistImport = new PlaylistImport(this);
				Queue<Asset> aqPGAssets = cPlaylistImport.PowerGoldFileParse(sFile);
				_aIntermediateObjectsStorage.Add(aqPGAssets);
				string sNewLine = "<br>" + Environment.NewLine;
				foreach(string sMessage in cPlaylistImport.aMessages)
					WebServiceError.Add(_cSI, sMessage.Replace(Environment.NewLine, sNewLine));
				return _aIntermediateObjectsStorage.IndexOf(aqPGAssets);
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return -1;
		}

		[WebMethod(EnableSession = true)]
		public int VideoInternationalFileParse(string sFile)
		{
			try
			{
				PlaylistImport cPlaylistImport = new PlaylistImport(this);
				PlaylistImport.VIPlaylist cVIPlaylist = cPlaylistImport.VideoInternationalFileParse(sFile);
				_aIntermediateObjectsStorage.Add(cVIPlaylist);
				string sNewLine = "<br>" + Environment.NewLine;
				foreach (string sMessage in cPlaylistImport.aMessages)
					WebServiceError.Add(_cSI, sMessage.Replace(Environment.NewLine, sNewLine));
				return _aIntermediateObjectsStorage.IndexOf(cVIPlaylist);
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return -1;
		}

		[WebMethod(EnableSession = true)]
		public int DesignFileParse(string sFile)
		{
			try
			{
				PlaylistImport cPlaylistImport = new PlaylistImport(this);
				Dictionary<TimeSpan, Queue<Asset>> ahDesignAssets = cPlaylistImport.DesignFileParse(sFile);
				_aIntermediateObjectsStorage.Add(ahDesignAssets);
				string sNewLine = "<br>" + Environment.NewLine;
				foreach (string sMessage in cPlaylistImport.aMessages)
					WebServiceError.Add(_cSI, sMessage.Replace(Environment.NewLine, sNewLine));
				return _aIntermediateObjectsStorage.IndexOf(ahDesignAssets);
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return -1;
		}

		[WebMethod(EnableSession = true)]
		public List<PlaylistItem> PlaylistsMerge(int nPGAssetsHandle, int nVIAssetsHandle, DateTime dtAdvertisementBind, int nDesignAssetsHandle)
		{
			List<PlaylistItem> aRetVal = null;
			try
			{
				Queue<Asset> aqPGPL = null;
				PlaylistImport.VIPlaylist cVIPL = null;
				Dictionary<TimeSpan, Queue<Asset>> ahDsgnPL = null;
				try
				{
					aqPGPL = (Queue<Asset>)_aIntermediateObjectsStorage[nPGAssetsHandle];
					_aIntermediateObjectsStorage[nPGAssetsHandle] = null;
					cVIPL = (PlaylistImport.VIPlaylist)_aIntermediateObjectsStorage[nVIAssetsHandle];
					_aIntermediateObjectsStorage[nVIAssetsHandle] = null;
					ahDsgnPL = (Dictionary<TimeSpan, Queue<Asset>>)_aIntermediateObjectsStorage[nDesignAssetsHandle];
					_aIntermediateObjectsStorage[nDesignAssetsHandle] = null;
				}
				catch
				{
					throw new Exception("can't find one of the intermediate PLs");
				}
				PlaylistImport cPlaylistImport = new PlaylistImport(this);
				aRetVal = cPlaylistImport.PlaylistsMerge(aqPGPL, cVIPL, dtAdvertisementBind, ahDsgnPL);
				string sNewLine = "<br>" + Environment.NewLine;
				foreach (string sMessage in cPlaylistImport.aMessages)
					WebServiceError.Add(_cSI, sMessage.Replace(Environment.NewLine, sNewLine));
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}
		#endregion
		#endregion

		#region templates
		[WebMethod(EnableSession = true)]
		public RegisteredTable TemplateRegisteredTableGet()
		{
			return base.RegisteredTableGet("cues", "tTemplates");
		}

		[WebMethod(EnableSession = true)]
		public Macro[] MacrosCrawlsGet()
		{
            return base.CrawlsMacrosGet();
		}
		[WebMethod(EnableSession = true)]
		public cues.Template[] TempateMessagesGet()
		{//						   SELECT * FROM cues."tTemplates" WHERE "sName" LIKE '%Message-%';
			return base.MessagesTemplatesGet();
		}
		[WebMethod(EnableSession = true)]
		public TemplateBind[] TemplateBindsTrailsGet()
		{
            try
			{
                return base.TemplateBindsGet("WHERE `sName` LIKE '%Анонс-%'"); //EMERGENCY:l и вот что мне с этим делать?! как мне перевести на английский?))
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return null;
			}
		}
		[WebMethod(EnableSession = true)]
		public bool MacrosValuesSet(Macro[] aMacros)
		{
			bool bRetVal = true;
			try
			{
				_cDB.TransactionBegin();
				foreach (Macro cMacro in aMacros)
                    //update mam."tMacros" set "sValue"='TEST'                  where "sName"='{%MACRO::REPLICA::CRAWL(2)%}';
					if (1 > _cDB.Perform("update mam.`tMacros` set `sValue`='" + cMacro.sValue + "' where `sName`='" + cMacro.sName + "';"))
						bRetVal = false;
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				bRetVal = false;
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public bool TemplateMessagesTextSave(DictionaryElement[] aDict)
		{
			bool bRetVal = true;
			try
			{
				_cDB.TransactionBegin();
				foreach (DictionaryElement cDE in aDict)
				{
					if (cDE.nTargetID < 1 || cDE.nRegisteredTablesID < 1)
					{
						bRetVal = false;
						continue;
					}
					if (false == base.DictionaryElementSave(cDE))
					{
						_cDB.TransactionRollBack();
						return false;
					}
				}
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				bRetVal = false;
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public DictionaryElement[] TemplateMessagesTextGet(cues.Template[] aMessages)
		{
			try
			{
				string sTargetIDs = "", sS = "";
                foreach (cues.Template cT in aMessages)
				{
					sTargetIDs += sS + cT.nID;
					sS = ",";
				}
				RegisteredTable nCuesTemplate = base.RegisteredTableGet("cues", "tTemplates");
				if (null == nCuesTemplate)
					throw new Exception("RegisteredTable is null: [cues.tTemplates]");
				return base.DictionaryGet("WHERE `idRegisteredTables` = " + nCuesTemplate.nID + " AND `idTarget` in (" + sTargetIDs + ")");
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}


		[WebMethod(EnableSession = true)]
		public bool TemplatesScheduleAdd(TemplatesSchedule[] aTemplatesSchedule)
		{
			bool bRetVal = true;
			try
			{
				_cDB.TransactionBegin();
                foreach (TemplatesSchedule cTemplatesSchedule in aTemplatesSchedule)
                    base.TemplatesScheduleSave(cTemplatesSchedule);
                    //                    SELECT cues."fTemplatesScheduleAdd"(23, '2013-12-12 18:00:00+04','7 days', NULL);
                    //if (!_cDB.GetValueBool("SELECT `bValue` FROM cues.`fTemplatesScheduleAdd`(" + cTemplatesSchedule.cTemplate.nID + ", '" + cTemplatesSchedule.dtStart.ToString("yyyy-MM-dd HH:mm:ss") + "','" + cTemplatesSchedule.nIntervalInSeconds + " seconds', " + (DateTime.MaxValue == cTemplatesSchedule.dtStop ? "NULL" : "'" + cTemplatesSchedule.dtStop.ToString("yyyy-MM-dd HH:mm:ss") + "'") + ");"))
                    //  bRetVal = false;
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				bRetVal = false;
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public TemplatesSchedule[] TemplatesScheduleGet(TemplateBind[] aTemplateBinds, DateTime dtBegin)
		{//										   SELECT * FROM cues."vTemplatesSchedule" WHERE "dtStart"<'2013-12-16 00:00:00+04' AND ("dtStop">'2013-12-09 00:00:00+04' OR "dtStop" IS NULL) ORDER BY "idTemplates", "dtStart";
			TemplatesSchedule[] aRetVal = base.TemplatesScheduleGet(aTemplateBinds).Where(o => dtBegin < o.dtStop || o.dtStop.IsNullOrEmpty()).OrderBy(o => o.cTemplateBind.nID).ThenBy(o => o.dtStart).ToArray();

			RegisteredTable nCuesTemplateSchedule = base.RegisteredTableGet("cues", "tTemplatesSchedule");
			if (null == nCuesTemplateSchedule)
				throw new Exception("RegisteredTable is null: [cues.tTemplatesSchedule]");
			
			foreach (TemplatesSchedule cTS in aRetVal)
				cTS.DictionarySet(_cDB.Select("select * from cues.`tDictionary` where `idRegisteredTables`=" + nCuesTemplateSchedule.nID + " and `idTarget`=" + cTS.nID + " order by `sKey`"));
			//								   select * from cues."tDictionary" where "idRegisteredTables"=84 and "idTarget"=1 order by "sKey"
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
		public bool TemplatesScheduleDelete(TemplatesSchedule[] aTemplatesSchedule)
		{
			bool bRetVal = true;
			try
			{
				_cDB.TransactionBegin();
                foreach (TemplatesSchedule cTemplatesSchedule in aTemplatesSchedule)
                    if (!base.TemplatesScheduleRemove(cTemplatesSchedule))
                        bRetVal = false;
					//                    SELECT cues."fTemplatesScheduleRemove"(4);
					//if (!_cDB.GetValueBool("SELECT `bValue` FROM cues.`fTemplatesScheduleRemove`(" + cTemplatesSchedule.nID + ");"))
					//	bRetVal = false;
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				bRetVal = false;
				_cDB.TransactionRollBack();
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public string[] DirectoriesTrailsGet(string sPath)
		{
			List<string> aResult = new List<string>();
			try
			{
				System.IO.DirectoryInfo[] aDirectories;
				System.IO.DirectoryInfo cDir = new System.IO.DirectoryInfo(sPath);
				aDirectories = cDir.GetDirectories();
				aResult.AddRange(from cFInfo in aDirectories select cFInfo.Name);
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return aResult.ToArray();
		}
		#endregion

		#region scr
		[WebMethod(EnableSession = true)]
		public Shift ShiftAdd(IdNamePair cPreset, string sSubject)
		{
			try
			{
				return base.ShiftAdd(cPreset, sSubject);
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		new public bool ShiftStart(Shift cShift)
		{
			bool bRetVal = false;
			try
			{
				base.ShiftStart(cShift);
				bRetVal = true;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		new public bool ShiftStop(Shift cShift)
		{
			bool bRetVal = false;
			try
			{
				base.ShiftStop(cShift);
				bRetVal = true;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public Shift ShiftCurrentGet()
		{
			try
			{
				return base.ShiftCurrentGet();

			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}

		[WebMethod(EnableSession = true)]
		new public Announcement[] AnnouncementsActualGet()
		{
			return AnnouncementsActualGet();
		}

		[WebMethod(EnableSession = true)]
		new public Message[] MessagesQueueGet()
		{
			try
			{
				return base.MessagesQueueGet();
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return new Message[0];
		}
		[WebMethod(EnableSession = true)]
		new public void MessageMark(long nID)
		{
			base.MessageMark(nID);
		}
		[WebMethod(EnableSession = true)]
		new public void MessageUnMark(long nID)
		{
			base.MessageUnMark(nID);
		}

		[WebMethod(EnableSession = true)]
		public PlaylistItem[] TimeBlockGet(DateTime dt, bool bForward)
		{
			return new PlaylistItem[0];
			/*
			_cDB.Perform("SELECT scr.`fMessageMarkRemove`(" + nID + ")");
SELECT * FROM archive."vPlayListResolvedFull" 
WHERE ("dtStartHard" IS NOT NULL OR "dtStartSoft" IS NOT NULL) AND "dtStartPlanned" <
	(
		SELECT "dtStartPlanned" FROM archive."vPlayListResolvedFull" 
		WHERE "dtStartHard" IS NULL AND "dtStartSoft" IS NULL AND "dtStartPlanned" > 
			(
				SELECT "dtStartPlanned" 
				FROM archive."vPlayListResolvedFull" 
				WHERE "dtStartHard" IS NOT NULL OR "dtStartSoft" IS NOT NULL 
				ORDER BY "dtStartPlanned" 
				LIMIT 1
			)
		ORDER BY "dtStartPlanned"
		LIMIT 1
	)
ORDER BY "dtStartPlanned";
	*/
		}
		[WebMethod(EnableSession = true)]
		public int PlaqueAdd(Plaque cPlaque)
		{
			try
			{
				return _cDB.GetValueInt("SELECT * FROM scr.`fPlaqueAdd`(" + cPlaque.cPreset.nID + ",'" + cPlaque.sName.ForDB() + "','" + cPlaque.sFirstLine.ForDB() + "','" + cPlaque.sSecondLine.ForDB() + "')");
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
				return -1;
			}
		}
		[WebMethod(EnableSession = true)]
		public Plaque[] PlaquesGet(IdNamePair cPreset)
		{
			List<Plaque> aRetVal = new List<Plaque>();
			Queue<Hashtable> aqDBValues = null;
			try
			{
				if (null == (aqDBValues = _cDB.Select("SELECT * FROM scr.`vPlaques` WHERE `idPresets`='" + cPreset.nID + "';")))
					return null;
				while (0 < aqDBValues.Count)
					aRetVal.Add(new Plaque(aqDBValues.Dequeue()));
				return aRetVal.ToArray();
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public bool PlaqueDelete(Plaque cPlaque)
		{
			try
			{
				if (0 < _cDB.Perform("DELETE FROM scr.`tPlaques` WHERE id=" + cPlaque.nID))
					return true;
			}
			catch { }
			return false;
		}
		[WebMethod(EnableSession = true)]
		public bool PlaqueChange(Plaque cPlaque)
		{
			try
			{
				if (0 < _cDB.Perform("UPDATE scr.`tPlaques` SET `sName`='" + cPlaque.sName.ForDB() + "',`sFirstLine`='" + cPlaque.sFirstLine.ForDB() + "',`sSecondLine`='" + cPlaque.sSecondLine.ForDB() + "'  WHERE id=" + cPlaque.nID))
					return true;
			}
			catch { }
			return false;
		}
		[WebMethod(EnableSession = true)]
		public Cues CuesGet(long nAssetID)
		{
			try
			{
				return base.CuesLoad(nAssetID);
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return new Cues();
			//Dictionary<string, string> ahRetVal = new Dictionary<string, string>();
			////SELECT "idAssets", "sSong", "sArtist" FROM mam."tAssetAttributes" LEFT JOIN mam."tCues" ON mam."tAssetAttributes"."nValue"=mam."tCues".id WHERE "sKey"='cues' AND "idAssets"=    482
			//Hashtable ahCue = _cDB.GetRow("SELECT `idAssets`, `sSong`, `sArtist` FROM mam.`tAssetAttributes` LEFT JOIN mam.`tCues` ON mam.`tAssetAttributes`.`nValue`=mam.`tCues`.id WHERE `sKey`='cues' AND `idAssets`=" + nAssetID.ToString());
			//ahRetVal.Add("ARTIST", ahCue["sArtist"].ToString());
			//ahRetVal.Add("SONG", ahCue["sSong"].ToString());
			//return ahRetVal;
		}
		[WebMethod(EnableSession = true)]
		public StoragesMappings[] StorageSCRGet()
		{
			List<StoragesMappings> aRetVal = new List<StoragesMappings>();
			try
			{
				//_cDB.RoleSet("replica_assets");
				Queue<Hashtable> aqDBValues = _cDB.Select("SELECT media.`tStorages`.id, `sName`, `sLocalPath` FROM media.`tStorages` LEFT JOIN scr.`tStoragesMappings` ON media.`tStorages`.id=scr.`tStoragesMappings`.`idStorages`  ORDER BY `sName`");
				aRetVal = new List<StoragesMappings>();        //SELECT media."tStorages".id, "sName", "sLocalPath" FROM media."tStorages" LEFT JOIN scr."tStoragesMappings" ON media."tStorages".id=scr."tStoragesMappings"."idStorages"  ORDER BY "sName"    
				while (null != aqDBValues && 0 < aqDBValues.Count)
					aRetVal.Add(new StoragesMappings(aqDBValues.Dequeue()));
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return aRetVal.ToArray();
		}
		[WebMethod(EnableSession = true)]
		public bool[] LogoBindingGet(PlaylistItem[] aPLIs)
		{
			LinkedList<bool> aRetVal = new LinkedList<bool>();
			try
			{
				Queue<Hashtable> aqDBValues = _cDB.Select("SELECT `sClassName` FROM cues.`vClassAndTemplateBinds` WHERE `sTemplateName` like '%Лого%' AND `sKey`='start_offset'");
				Hashtable ahRow;
				LinkedList<string> aLogoBinding = new LinkedList<string>();
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					aLogoBinding.AddLast(ahRow["sClassName"].ToString());
				}
				foreach (PlaylistItem cPLI in aPLIs)
				{
					if (aLogoBinding.Contains(cPLI.cClass.sName))
						aRetVal.AddLast(true);
					else
						aRetVal.AddLast(false);
				}
			}
			catch (Exception ex)
			{
				aRetVal.Clear();
                WebServiceError.Add(_cSI, ex);
			}
			return aRetVal.ToArray();
		}

		[WebMethod(EnableSession = true)]
		public void ClipsBDLog(long nShiftID, PlaylistItem[] aPLIs)
		{
			try
			{
				if (1 > aPLIs.Length)
					return;
				//INSERT INTO logs."tSCR" ("idShifts", "idAssets", "dtStart", "dtStop") VALUES (2,2000,'2004-05-07','2004-05-07'),(2,2000,'2004-05-07','2004-05-07');
				string sSQL = "INSERT INTO logs.`tSCR` (`idShifts`, `idAssets`, `dtStart`, `dtStop`) VALUES ";
				string sZ = "";
				foreach (PlaylistItem cPLI in aPLIs)
					if (null != cPLI.cAsset && DateTime.MinValue < cPLI.dtStartReal && DateTime.MinValue < cPLI.dtStopReal)
					{
						sSQL += sZ + "(" + nShiftID + "," + cPLI.cAsset.nID + ",'" + cPLI.dtStartReal.ToString("yyyy-MM-dd HH:mm:ss") + "','" + cPLI.dtStopReal.ToString("yyyy-MM-dd HH:mm:ss") + "')";
						if (sZ == "") sZ = ",";
					}
				if ("," == sZ)
					_cDB.Select(sSQL + ";");
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
		}

        #endregion

		#region stat
		private PlaylistItem[] PlaylistGet(DBFilters cFilters)
		{
			PlaylistItem[] aRetVal = null;
			string sQuery = "";
			if (null != cFilters)
				sQuery = " WHERE " + cFilters.sSQL;
			try
			{
				Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM archive.`vPlayListResolvedFull`" + sQuery);
				if (null != aqDBValues)
				{
                    aRetVal = aqDBValues.Select(o => new PlaylistItem(o)).ToArray();
					for(int nIndx = 0; aRetVal.Length > nIndx; nIndx++)
						aRetVal[nIndx].cStatus.sName = webservice.Preferences.cDBKeysMap.GetTitle(DBKeysMap.MapClass.pli_status, aRetVal[nIndx].cStatus.sName);
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}
		private PlaylistItem[] PlaylistWithAssetsGet(DBFilters cFilters)
		{
			PlaylistItem[] aRetVal = null;
			string sQuery = "";
			if (null != cFilters)
				sQuery = " WHERE " + cFilters.sSQL;
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT * FROM archive.`vPlayListWithAssetsResolvedFull`" + sQuery)))
				{
					aRetVal = new PlaylistItem[aqDBValues.Count];
					Dictionary<long, Asset> ahAssets = new Dictionary<long, Asset>();
					int nIndx = 0;
					Hashtable ahRow = null;
					while (0 < aqDBValues.Count)
					{
						ahRow = aqDBValues.Dequeue();
						aRetVal[nIndx] = new PlaylistItem(ahRow);
						if (null != aRetVal[nIndx].cAsset)
						{
							if (!ahAssets.ContainsKey(aRetVal[nIndx].cAsset.nID))
							{
								ahRow["id"] = ahRow["idAssets"];
								ahRow["nFramesQty"] = ahRow["nAssetFramesQty"];
								if ("clip" == ahRow["sVideoTypeName"].ToString()) //UNDONE
									ahAssets.Add(aRetVal[nIndx].cAsset.nID, new Clip(ahRow));
								else if ("program" == ahRow["sVideoTypeName"].ToString()) //UNDONE
								{
									Program cProg = new Program(ahRow);
									cProg.aRAOInfo = ProgramRAOInfo_old_and_new_Get(cProg).ToArray();
									ahAssets.Add(aRetVal[nIndx].cAsset.nID, cProg);
								}
								else
									ahAssets.Add(aRetVal[nIndx].cAsset.nID, new Asset(ahRow));
							}
							aRetVal[nIndx].cAsset = ahAssets[aRetVal[nIndx].cAsset.nID];
						}
						nIndx++;
					}
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}
		private PlaylistItem[] RemoveUnderSCR_PLIs(PlaylistItem[] aSourcePLIs)
		{
			if (null == aSourcePLIs || 1 > aSourcePLIs.Length)
				return aSourcePLIs;

			List<PlaylistItem> aRetVal = new List<PlaylistItem>();
			aSourcePLIs = aSourcePLIs.OrderBy(o => o.dtStartReal).ToArray();
			PlaylistItem[] aStartRealPLIs = aSourcePLIs.Where(o => o.dtStartReal > DateTime.MinValue && o.dtStartReal < DateTime.MaxValue).ToArray();
			Shift[] aShifts = base.ShiftsGet(aStartRealPLIs.Min(o => o.dtStartReal), aStartRealPLIs.Max(o => o.dtStartReal));
			int nCurShift = 0;
			if (0 == aShifts.Length)
				return aSourcePLIs;
			for (int ni = 0; ni < aSourcePLIs.Length; ni++)
			{
				if (aSourcePLIs[ni].dtStartReal < aShifts[nCurShift].dtStart)
					aRetVal.Add(aSourcePLIs[ni]);
				else if (aSourcePLIs[ni].dtStartReal < aShifts[nCurShift].dtStop)
					continue;
				else if (nCurShift + 1 < aShifts.Length)
				{
					nCurShift++;
					ni--;
					continue;
				}
				else
					aRetVal.Add(aSourcePLIs[ni]);
			}
			return aRetVal.ToArray();
		}
		private PlaylistItem[] AddSCR_PLIs(PlaylistItem[] aSourcePLIs, DBFilters cFilters)
		{
			List<PlaylistItem> aRetVal = new List<PlaylistItem>();
			PlaylistItem[] aSCR_PLIs = new PlaylistItem[0];
			string sQuery = "";
			int nI;
			if (null != cFilters)
				sQuery = " WHERE " + cFilters.sSQL.Replace("dtStartPlanned", "dtStartReal").Replace("dtStopPlanned", "dtStopReal").Replace("`sName` ILIKE", "mam.`vAssetsResolved`.`sName` ILIKE");
			//SELECT scr.*, pl."tStatuses"."sName" AS "sStatusName", mam."vAssetsResolved".*, 1 as "nFrameStart", "nFramesQty" as "nFrameStop" FROM 
			//(SELECT "dtStart" as "dtStartReal", "dtStop" as "dtStopReal", 5 as "idStatuses", "idAssets"  FROM logs."tSCR") scr 
			//LEFT JOIN pl."tStatuses" ON scr."idStatuses" = pl."tStatuses".id
			//LEFT JOIN mam."vAssetsResolved" ON scr."idAssets"=mam."vAssetsResolved".id 
			//WHERE "sVideoTypeName" = 'clip';
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT scr.*, pl.`tStatuses`.`sName` AS `sStatusName`, mam.`vAssetsResolved`.*, 1 as `nFrameStart`, `nFramesQty` as `nFrameStop` FROM " +
														"(SELECT `dtStart` as `dtStartReal`, `dtStop` as `dtStopReal`, 5 as `idStatuses`, `idAssets`  FROM logs.`tSCR`) scr " +
														"LEFT JOIN pl.`tStatuses` ON scr.`idStatuses` = pl.`tStatuses`.id " +
														"LEFT JOIN mam.`vAssetsResolved` ON scr.`idAssets`=mam.`vAssetsResolved`.id " +
														sQuery)))
				{
					aSCR_PLIs = new PlaylistItem[aqDBValues.Count];
					Dictionary<long, Asset> ahAssets = new Dictionary<long, Asset>();
					int nIndx = 0;
					Hashtable ahRow = null;
					while (0 < aqDBValues.Count)
					{
						ahRow = aqDBValues.Dequeue();
						ahRow["id"] = long.MaxValue;
						aSCR_PLIs[nIndx] = new PlaylistItem(ahRow);
						aSCR_PLIs[nIndx].dtStartPlanned = aSCR_PLIs[nIndx].dtStartReal;
						if (null != aSCR_PLIs[nIndx].cAsset)
						{
							if (!ahAssets.ContainsKey(aSCR_PLIs[nIndx].cAsset.nID))
							{
								ahRow["id"] = ahRow["idAssets"];
								ahRow["nFramesQty"] = ahRow["nAssetFramesQty"];
								if ("clip" == ahRow["sVideoTypeName"].ToString()) //UNDONE
									ahAssets.Add(aSCR_PLIs[nIndx].cAsset.nID, new Clip(ahRow));
								else if ("program" == ahRow["sVideoTypeName"].ToString()) //UNDONE
								{
									Program cProg = new Program(ahRow);
									cProg.aRAOInfo= ProgramRAOInfo_old_and_new_Get(cProg).ToArray();
									ahAssets.Add(aSCR_PLIs[nIndx].cAsset.nID, cProg);
								}
								else
									ahAssets.Add(aSCR_PLIs[nIndx].cAsset.nID, new Asset(ahRow));
							}
							aSCR_PLIs[nIndx].cAsset = ahAssets[aSCR_PLIs[nIndx].cAsset.nID];
						}
						nIndx++;
						nI = nIndx;
					}
				}

			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			aRetVal.AddRange(aSourcePLIs);
			aRetVal.AddRange(aSCR_PLIs);
			aRetVal.Sort((pli1, pli2) => pli1.dtStartPlanned.CompareTo(pli2.dtStartPlanned));
			return aRetVal.ToArray();
		}

		[WebMethod(EnableSession = true)]
		public PlaylistItem[] StatGet(DBFilters cFilters)
		{
			PlaylistItem[] aRetVal = null;
			try
			{
				aRetVal = PlaylistGet(cFilters);

				aRetVal = this.RemoveUnderSCR_PLIs(aRetVal);
				aRetVal = this.AddSCR_PLIs(aRetVal, cFilters);
				for (int nIndx = 0; aRetVal.Length > nIndx; nIndx++)
					aRetVal[nIndx].cStatus.sName = webservice.Preferences.cDBKeysMap.GetTitle(DBKeysMap.MapClass.pli_status, aRetVal[nIndx].cStatus.sName);
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}

		[WebMethod(EnableSession = true)]
		public Message[] MessagesGet(DBFilters cFilters)
		{
			Message[] aRetVal = null;
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null == (aqDBValues = _cDB.Select("SELECT DISTINCT * FROM ia.`vMessagesResolved` WHERE ", (null == cFilters ? null : cFilters.sSQL), "`dtRegister` DESC", 0, 0, null)))
					return null;
				aRetVal = new Message[aqDBValues.Count];
				int nIndx = 0;
				while (0 < aqDBValues.Count)
					aRetVal[nIndx++] = new Message(aqDBValues.Dequeue());
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}

		internal class WorkerInfo
		{
			static ulong nWorkerInfoID = 1;
			static private object _cSyncRoot = new object();
			static private Dictionary<ulong, WorkerInfo> _ahWorkerInfos = new Dictionary<ulong, WorkerInfo>();

			public ulong nID;
			public double nProgress;
			public object cResult;

			protected System.Threading.Thread cThread;
			protected System.Threading.Semaphore cSemaphore;

			public WorkerInfo()
			{
				lock (_cSyncRoot)
					nID = nWorkerInfoID++;
				_ahWorkerInfos.Add(nID, this);
				nProgress = 0;
				cSemaphore = new System.Threading.Semaphore(0, 3);
				cThread = new System.Threading.Thread(Worker);
				cThread.Start();
			}
			static public WorkerInfo Get(ulong nID)
			{
				if (_ahWorkerInfos.ContainsKey(nID))
					return _ahWorkerInfos[nID];
				return null;
			}
			static public void Dispose(ulong nID)
			{
				if (_ahWorkerInfos.ContainsKey(nID))
					_ahWorkerInfos.Remove(nID);
			}
			virtual public void Worker()
			{
			}
		}
		internal class ExportWorkerInfo : WorkerInfo
		{
			internal class ProgressAsync
			{
				WorkerInfo _cWorkerInfo;
				double _nProgressStep;
				double _nMaximum;
				bool _bRun;
				public ProgressAsync(WorkerInfo cWorkerInfo, double nProgressStep, double nMaximum)
				{
					_cWorkerInfo = cWorkerInfo;
					_nProgressStep = nProgressStep;
					_nMaximum = nMaximum;
					_bRun = true;
					System.Threading.ThreadPool.QueueUserWorkItem(Worker);
				}
				private void Worker(object cState)
				{
					while (_nMaximum > _cWorkerInfo.nProgress && _bRun)
					{
						_cWorkerInfo.nProgress += _nProgressStep;
						System.Threading.Thread.Sleep(1000);
					}
					_cWorkerInfo = null;
				}
				public void Stop()
				{
					_bRun = false;
					while (null != _cWorkerInfo)
						System.Threading.Thread.Sleep(50);
				}
			}
			private string _sTemplate;
			private DBFilters _cFilters;
			private DBInteract _cDBI;

			public ExportWorkerInfo(DBInteract cDBI, string sTemplate, DBFilters cFilters)
				:base()
			{
				_cDBI = cDBI;
				_sTemplate = sTemplate;
				_cFilters = cFilters;
				cSemaphore.Release();
			}

			override public void Worker()
			{
				(new Logger()).WriteDebug3("in [tpl:" + _sTemplate + " ]");
				cSemaphore.WaitOne();
				(new Logger()).WriteDebug4("after semaphor [tpl:" + _sTemplate + " ]");
				string sResult = null;
				string sErrors = "", sHeader = "", sErrRow="<Row> <Cell ss:MergeAcross='9' ss:StyleID='cell_left'> <Data ss:Type='String'>{%ERROR%}</Data> </Cell> </Row>";
				Clip cCurrentClip = null;
				try
				{
					switch (_sTemplate)
					{
						case "export.RAO":
							DBFilters.DBFilter cDBF = (DBFilters.DBFilter)_cFilters.cGroup.cValue;
							DBFilters.DBFiltersGroup cDBFG;
							DateTime dtStart = DateTime.MinValue, dtStop = DateTime.MaxValue;
							int niter = 0;
							while (true)
							{
								(new Logger()).WriteDebug4("while(true). [iteration = " + niter++ + " ][cDBF.sName = " + cDBF.sName + " ]");
								if ("dtStartReal" == cDBF.sName)
								{
									switch (cDBF.eOP)
									{
										case DBFilters.Operators.more:
											dtStart = DateTime.Parse(cDBF.cValue.ToString());
											break;
										case DBFilters.Operators.less:
											dtStop = DateTime.Parse(cDBF.cValue.ToString());
											break;
									}
								}
								cDBFG = cDBF;
								while (null != cDBFG && !(cDBFG.cNext is DBFilters.DBFilter))
									cDBFG = cDBF.cNext;

								if (null == cDBFG)
									break;
								cDBF = (DBFilters.DBFilter)cDBFG.cNext;
							}
							double nProgressStep = 90 / (dtStop.Subtract(dtStart).TotalHours / 2);
							if (1 < nProgressStep)
								nProgressStep = 1;
							ProgressAsync cProgressAsync = new ProgressAsync(this, nProgressStep, 90);
							PlaylistItem[] aPLIs = _cDBI.PlaylistWithAssetsGet(_cFilters);
							aPLIs = _cDBI.RemoveUnderSCR_PLIs(aPLIs);
							aPLIs = _cDBI.AddSCR_PLIs(aPLIs, _cFilters);
							(new Logger()).WriteDebug4("[aPLIs.count = " + aPLIs.Length + " ][nProgressStep = " + nProgressStep + " ]");
							cProgressAsync.Stop();
							nProgressStep = (95 - nProgress) / (double)aPLIs.Length;
							Dictionary<long, int> ahClipsCounts = new Dictionary<long, int>();
							Dictionary<long, Clip> ahClips = new Dictionary<long, Clip>();
							Dictionary<long, Program.RAOInfo>  ahRAOInfo;
							List<Clip> aClips;
							

							{
								Clip cClipToDict = null;
								long nFramesQty;
								foreach (PlaylistItem cPLI in aPLIs)
								{
									aClips = new List<Clip>();
									ahRAOInfo=new Dictionary<long,Program.RAOInfo>();
									if (null != cPLI.cAsset && cPLI.cAsset is Program && null != ((Program)cPLI.cAsset).aRAOInfo)
										foreach (Program.RAOInfo cRAOI in ((Program)cPLI.cAsset).aRAOInfo)
										{
											aClips.Add(cRAOI.cClip);
											ahRAOInfo.Add(cRAOI.cClip.nID, cRAOI);
										}
									if (null != cPLI.cAsset && cPLI.cAsset is Clip)
										aClips.Add((Clip)cPLI.cAsset);

									foreach (Clip cClip in aClips)
									{
										if (!ahClips.ContainsKey(cClip.nID))
										{
											cClipToDict = cClip;
											cClipToDict.nFramesQty = 0;
											cClipToDict.aCustomValues = _cDBI.CustomsLoad(cClipToDict.nID);
											ahClips.Add(cClipToDict.nID, cClipToDict);
											ahClipsCounts.Add(cClipToDict.nID, 1);
										}
										else
										{
											cClipToDict = ahClips[cClip.nID];
											ahClipsCounts[cClipToDict.nID]++;
										}
										if (cPLI.cAsset is Program)
										{
											nFramesQty = ahRAOInfo[cClipToDict.nID].nFramesQty;
											if (0 == nFramesQty && !sErrors.Contains("[id=" + cPLI.cAsset.nID + "][clip_name=" + cClip.sName + "][clip_id=" + cClip.nID + "]"))
                                                sErrors += Environment.NewLine + sErrRow.Replace("{%ERROR%}", "ХРОНОМЕТРАЖ ЧАСТИ КЛИПА = 0!! В программе [name=" + cPLI.cAsset.sName + "][id=" + cPLI.cAsset.nID + "][clip_name=" + cClip.sName + "][clip_id=" + cClip.nID + "]"); //не переводим, т.к. это РАО для России
										}
										else
											try
											{
												if (cPLI.dtStopReal < DateTime.MaxValue)
												{
													nFramesQty = (long)(((long)(cPLI.dtStopReal.Subtract(cPLI.dtStartReal).TotalSeconds + 0.5)) * 25);
													if (Math.Abs((long)cPLI.nDuration - nFramesQty) <= 50)
														nFramesQty = (long)cPLI.nDuration;  // т.е. если дали походу целиком, то убираем погрешности измерения...
												}
												else
													nFramesQty = cPLI.nFramesQty;  // случай сбоя в эфире, если стопа нет. за клип все-равно надо платить...
											}
											catch
											{
												if (cPLI.nFramesQty > cClipToDict.nFramesQty)
													nFramesQty = cClipToDict.nFramesQty;
												else
													nFramesQty = cPLI.nFramesQty;
											}
										cClipToDict.nFramesQty += nFramesQty;
									}
									nProgress += nProgressStep;
								}
							}
							nProgressStep = (100 - nProgress) / (double)ahClips.Count;
							sResult = sHeader = templates.export_rao_header.Replace("{%DATE_START%}", dtStart.ToString("dd-MM-yyyy")).Replace("{%DATE_STOP%}", dtStop.ToString("dd-MM-yyyy"));
							string sRow = templates.export_rao_row;
							IEnumerable<Clip> aClipsOrdered = ahClips.Values.OrderBy(row => row.stCues.sArtist).ThenBy(row => row.stCues.sSong);
							CustomValue cMusic, cLyrics;
							string sSeconds;
							foreach (Clip cClip in aClipsOrdered)
							{
								cCurrentClip = cClip;
								if (null != cClip.aCustomValues)
								{
									cMusic = cClip.aCustomValues.FirstOrDefault(o => o.sName == "author_of_music");
									cLyrics = cClip.aCustomValues.FirstOrDefault(o => o.sName == "author_of_lyrics");
								}
								else
								{
									cMusic = new CustomValue("", "");
									cLyrics = new CustomValue("", "");
								}

								if (1 > cClip.nFramesQty)
                                    sErrors += Environment.NewLine + sErrRow.Replace("{%ERROR%}", "ОБЩИЙ ХРОНОМЕТРАЖ КЛИПА НЕ МОЖЕТ БЫТЬ РАВЕН 00:00 !!!!\t[name=" + cClip.sName + "]\t[id=" + cClip.nID + "]"); //не переводим, т.к. это РАО для России

								if (null == cClip.cFile)
									sSeconds = "03:33";    // т.к. РАО не приемлет ничего кроме циферок, а 0 им не подходит, то договорились заменять на 3:33. Деньги все-равно идут с колонки SECONDS_TOTAL
								else
									sSeconds = (DateTime.MinValue.AddSeconds((cClip.nFrameOut - cClip.nFrameIn + 1) / 25)).ToString("mm:ss");

								sResult += sRow.Replace("{%ARTIST_NAME%}", cClip.stCues.sArtist)
												.Replace("{%SONG_NAME%}", cClip.stCues.sSong)
												.Replace("{%SECONDS_TOTAL%}", (DateTime.MinValue.AddSeconds(cClip.nFramesQty / 25)).ToString("H:mm:ss"))
												.Replace("{%AUTHOR_OF_MUSIC%}", cMusic.sValue)
												.Replace("{%AUTHOR_OF_LYRICS%}", cLyrics.sValue)
												.Replace("{%RELISES_QTY%}", ahClipsCounts[cClip.nID].ToString())
												.Replace("{%SECONDS%}", sSeconds)
                                                .Replace("{%GENRE%}", "песня"); //не переводим, т.к. это РАО для России
								nProgress += nProgressStep;
							}
							sResult += templates.export_rao_footer;
							break;
					}
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
					string sCurrent = "current clip: [" + (cCurrentClip == null ? "null" : "id=" + cCurrentClip.nID + " name=" + cCurrentClip.sName) + "]";
					(new Logger()).WriteError(sCurrent);
					cResult = ex.Message + "\n\n" + ex.Source + "\n\n" + ex.StackTrace + "\n\n" + sCurrent + "\n\n" + sErrors;
					nProgress = 100;
					return;
				}
				if (0 < sErrors.Length)
					cResult = sHeader + "\n\n" + sErrors + templates.export_rao_footer;
				else
					cResult = sResult;
				nProgress = 100;
				(new Logger()).WriteDebug4("return");
			}
		}
		[WebMethod(EnableSession = true)]
		public double WorkerProgressGet(ulong nWorkerInfoID)
		{
			try
			{
				return WorkerInfo.Get(nWorkerInfoID).nProgress;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return byte.MaxValue;
		}
		[WebMethod(EnableSession = true)]
		public ulong Export(string sTemplate, DBFilters cFilters)
		{
            (new Logger()).WriteDebug3("in");
            try
			{
				ExportWorkerInfo cEWI = new ExportWorkerInfo(this, sTemplate, cFilters);
				(new Logger()).WriteDebug("Export: " + sTemplate + ": [id = " + cEWI.nID + " ][sql = " + cFilters.cGroup.sSQL + "]");
				return cEWI.nID;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			(new Logger()).WriteDebug4("return 0");
			return 0;
		}
		[WebMethod(EnableSession = true)]
		public string ExportResultGet(ulong nWorkerInfoID)
		{
			string sRetVal = null;
			try
			{
				sRetVal = (string)WorkerInfo.Get(nWorkerInfoID).cResult;
				WorkerInfo.Dispose(nWorkerInfoID);
				return sRetVal;
			}
			catch (Exception ex)
			{
                WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		#endregion

		#region rt   
		// УСТАРЕЛО ?????
		[WebMethod(EnableSession = true)]
		public List<int[]> RingtonesBindsGet()
		{
			List<int[]> aRetVal = new List<int[]>();
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT * FROM cues.`tRingtones`")))
				{
					Hashtable ahRow = null;
					while (0 < aqDBValues.Count)
					{
						ahRow = aqDBValues.Dequeue();
						aRetVal.Add(new int[] { ahRow["nBindCode"].ToInt32(), ahRow["nReplaceCode"].ToInt32() });
					}
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal;
		}

		#endregion

		#region ui
		[WebMethod(EnableSession = true)]
		public IdNamePair[] FrequencyOfOccurrence(long nVideoTypeID)
		{
			try
			{
				return _cDB.Select("SELECT count(*) as id, acv.`sCustomValueName` as `sName` FROM mam.`tVideos` v, mam.`vAssetsCustomValues` acv WHERE v.`idVideoTypes`=" + nVideoTypeID + " AND v.`idAssets`=acv.id GROUP BY acv.`sCustomValueName` ORDER BY id DESC").Select(o => new IdNamePair(o)).ToArray();
				// SELECT count(*) as id, 'total' as "sName" FROM mam."tVideos" WHERE "idVideoTypes"=2             UNION SELECT acv."sCustomValueName", count(*) as "nQty" FROM mam."tVideos" v, mam."vAssetsCustomValues" acv WHERE v."idVideoTypes"=2 AND v."idAssets"=acv.id GROUP BY acv."sCustomValueName" ORDER BY "nQty" DESC
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		#endregion

		#region grid
		[WebMethod(EnableSession = true)]
		public string GridGet()
		{
			try
			{
				return SIO.File.ReadAllText((new WebService()).Server.MapPath("/") + "grids/grid.xml");
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public void GridSave(string sXML)
		{
			try
			{
				SIO.File.WriteAllText((new WebService()).Server.MapPath("/") + "grids/grid.xml", sXML);
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
		}
		#endregion

		[WebMethod(EnableSession = true)]
		public TransliterationPair[] TransliterationGet()
		{
			List<TransliterationPair> aRetVal = new List<TransliterationPair>();
			try
			{
				Queue<Hashtable> aqDBValues = null;
				//_cDB.RoleSet("replica_ingest");
				if (null != (aqDBValues = _cDB.Select("SELECT * FROM adm.`tTransliteration`")))
				{
					while (0 < aqDBValues.Count)
						aRetVal.Add(new TransliterationPair(aqDBValues.Dequeue()));
				}
			}
			catch (Exception ex)
			{
				WebServiceError.Add(_cSI, ex);
			}
			return aRetVal.ToArray();
		}

		[WebMethod(EnableSession = true)]
		public DateTime DateTimeNowGet()
		{
			return DateTime.Now;
		}

	}
}
