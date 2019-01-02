using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Collections.Specialized;

using helpers;
using helpers.extensions;
using System.Xml;

using g = globalization;

namespace webservice
{
	public class DBKeysMap
	{
		public enum MapClass
		{
			pli_status,
			asset_videotype,
			person_type
		}
		private Dictionary<MapClass, StringDictionary> _aBinds;
		private Dictionary<MapClass, StringDictionary> _aTitles;
		public DBKeysMap()
		{
			_aBinds = new Dictionary<MapClass, StringDictionary>();
			_aTitles = new Dictionary<MapClass, StringDictionary>();

			StringDictionary ahStrings = new StringDictionary();
			ahStrings["planned"] = "planned";
			ahStrings["queued"] = "queued";
			ahStrings["prepared"] = "prepared";
			ahStrings["onair"] = "onair";
			ahStrings["played"] = "played";
			ahStrings["skipped"] = "skipped";
			ahStrings["failed"] = "failed";
			_aBinds.Add(MapClass.pli_status, ahStrings);
			ahStrings = new StringDictionary();
            ahStrings["planned"] = g.Webservice.sNoticePreferences1;
			ahStrings["queued"] = g.Webservice.sNoticePreferences2;
			ahStrings["prepared"] = g.Webservice.sNoticePreferences3;
			ahStrings["onair"] = g.Webservice.sNoticePreferences4;
			ahStrings["played"] = g.Webservice.sNoticePreferences5;
			ahStrings["skipped"] = g.Webservice.sNoticePreferences6;
			ahStrings["failed"] = g.Webservice.sNoticePreferences8;
			_aTitles.Add(MapClass.pli_status, ahStrings);

			ahStrings = new StringDictionary();
			ahStrings["clip"] = "clip";
			ahStrings["advertisement"] = "advertisement";
			ahStrings["program"] = "program";
			ahStrings["design"] = "design";
			_aBinds.Add(MapClass.asset_videotype, ahStrings);
			ahStrings = new StringDictionary();
            ahStrings["clip"] = g.Helper.sClip;
			ahStrings["advertisement"] = g.Helper.sAdvertisement;
			ahStrings["program"] = g.Helper.sProgram;
            ahStrings["design"] = g.Helper.sDesign;
			_aTitles.Add(MapClass.asset_videotype, ahStrings);

			ahStrings = new StringDictionary();
			ahStrings["artist"] = "artist";
			ahStrings["other"] = "other";
			_aBinds.Add(MapClass.person_type, ahStrings);
			ahStrings = new StringDictionary();
            ahStrings["artist"] = g.Helper.sArtist;
			ahStrings["other"] = g.Webservice.sNoticePreferences7;
			_aTitles.Add(MapClass.person_type, ahStrings);
		}
		public string GetKeyByValue(MapClass enMC, string sValue)
		{
			foreach (string sKey in _aBinds[enMC].Keys)
				if (sValue == _aBinds[enMC][sKey])
					return sKey;
			return null;
		}
		public string GetValueByKey(MapClass enMC, string sKey)
		{
			return _aBinds[enMC][sKey];
		}
		public string GetTitle(MapClass enMC, string sKey)
		{
			if (_aTitles[enMC].ContainsKey(sKey))
				return _aTitles[enMC][sKey];
			return sKey;
		}
	}
	public class Preferences : helpers.Preferences
	{
		static private Preferences _cInstance = new Preferences();

		public class Clients
		{
			[System.Xml.Serialization.XmlType(TypeName = "Preferences")]
			public class Replica : Clients
			{
				public long nFramesMinimum;
				public long nFramesBase;
				public int nFrequencyOfOccurrenceMax;
				public string sPreviewsPath;
				public string sTrailersPath;
				public bool bIsPgIdNeeded;
                public string sFilesDialogFilter;
				public string sLocale;
                public bool bContextMenuDeleteSince;
				public int nPLRecalculateTimeout;
				public int nPLImportTimeout;
				public bool bStatisticsRAOVisible;
                public bool bStatisticsMessagesVisible;
				public string sChannelName;

				public string sDefautClassClip;
				public string sDefautClassProgram;
				public string sDefautClassDesign;
				public string sDefautClassAdvertisement;
				public string sDefautClassUnknown;

				public Replica()
				{
                    bContextMenuDeleteSince = bStatisticsRAOVisible = bStatisticsMessagesVisible = true;
					nPLRecalculateTimeout = 180;
					nPLImportTimeout = 3600;
				}
			}
		}
		public enum ImportTargets
		{
			novalue,
			mam,
			playlist
		};
		static public DBKeysMap cDBKeysMap = new DBKeysMap();

		static public Clients.Replica cClientReplica
		{
			get
			{
				return _cInstance._cClientReplica;
			}
		}
		static public DB.Credentials DBCredentialsGet(string sUsername, string sPassword)
		{
			DB.Credentials cRetVal = new DB.Credentials()
			{
				sServer = _cInstance._cDBCredentials.sServer,
				nPort = _cInstance._cDBCredentials.nPort,
				sDatabase = _cInstance._cDBCredentials.sDatabase,
				sUser = sUsername,
				sPassword = sPassword,
				nTimeout = _cInstance._cDBCredentials.nTimeout
            };
			return cRetVal;
		}

        static public string sOAuthDomain
        {
            get
            {
                return _cInstance._sOAuthDomain;
            }
        }
        static public string sFacebookAppID
        {
            get
            {
                return _cInstance._sFacebookAppID;
            }
        }
        static public string sFacebookSecret
        {
            get
            {
                return _cInstance._sFacebookSecret;
            }
        }
        static public string sTwitterKey
        {
            get
            {
                return _cInstance._sTwitterKey;
            }
        }
        static public string sTwitterSecret
        {
            get
            {
                return _cInstance._sTwitterSecret;
            }
        }
        static public string sVKontakteAppID
        {
            get
            {
                return _cInstance._sVKontakteAppID;
            }
        }
        static public string sVKontakteSecret
        {
            get
            {
                return _cInstance._sVKontakteSecret;
            }
        }
		static public bool bPowerGoldIDsAreAssetIDs
		{
			get
			{
				return _cInstance._bPowerGoldIDsAreAssetIDs;
			}
		}
        static public int nColumnWithPGIds
        {
            get
            {
                return _cInstance._nColumnWithPGIds;
            }
        }
        static public bool bMakeAdvertAsset
        {
            get
            {
                return _cInstance._bMakeAdvertAsset;
            }
        }
        static public TimeSpan tsSafeRange
        {
            get
            {
                return _cInstance._tsSafeRange;
            }
        }


        static public string sClipStorageName;
		static public string sTSRConnection;

		private Clients.Replica _cClientReplica;
        private string _sOAuthDomain;
        private string _sFacebookAppID;
        private string _sFacebookSecret;
        private string _sTwitterKey;
        private string _sTwitterSecret;
        private string _sVKontakteAppID;
        private string _sVKontakteSecret;
		private bool _bPowerGoldIDsAreAssetIDs;
        private bool _bMakeAdvertAsset;
        private int _nColumnWithPGIds;
        private TimeSpan _tsSafeRange;
        private DB.Credentials _cDBCredentials;

        public Preferences()
			: base("//webservice")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			XmlNode cXmlNodeChild;
			XmlNode cXmlNodeClient;

			string sChName = cXmlNode.AttributeOrDefaultGet<string>("name", "channel");

            _cDBCredentials = new DB.Credentials(cXmlNode.NodeGet("database"));

            if (null != (cXmlNodeClient = cXmlNode.NodeGet("import", false)))
			{
				sTSRConnection = cXmlNodeClient.AttributeValueGet("tsr_connection");
				_bPowerGoldIDsAreAssetIDs = cXmlNodeClient.AttributeOrDefaultGet<bool>("pgid_assetid", true);
                _bMakeAdvertAsset = cXmlNodeClient.AttributeOrDefaultGet<bool>("make_advert_asset", false);
                _nColumnWithPGIds = cXmlNodeClient.AttributeOrDefaultGet<int>("column_pgid", 0);
            }
            if (null != (cXmlNodeClient = cXmlNode.NodeGet("playlist", false)))
            {
                _tsSafeRange = cXmlNodeClient.AttributeOrDefaultGet<TimeSpan>("safe_range", new TimeSpan(0, 15, 0));
            }
            if (null != (cXmlNodeClient = cXmlNode.NodeGet("storages", false)))
			{
                cXmlNodeChild = cXmlNodeClient.NodeGet("clips");
				_cClientReplica = new Clients.Replica();
                sClipStorageName = cXmlNodeChild.AttributeValueGet("name");
			}
			if (null != (cXmlNodeClient = cXmlNode.NodeGet("clients/replica", false)))
			{
                cXmlNodeChild = cXmlNodeClient.NodeGet("frames");
                _cClientReplica = new Clients.Replica() { sLocale = sLocale };
				_cClientReplica.sChannelName = sChName;
                _cClientReplica.nFramesMinimum = cXmlNodeChild.AttributeGet<int>("minimum");
				_cClientReplica.nFramesBase = cXmlNodeChild.AttributeGet<int>("base");
				cXmlNodeChild = cXmlNodeClient.NodeGet("customs");
                _cClientReplica.nFrequencyOfOccurrenceMax = cXmlNodeChild.AttributeGet<int>("occurrence");
                cXmlNodeChild = cXmlNodeClient.NodeGet("previews");
                _cClientReplica.sPreviewsPath = cXmlNodeChild.AttributeValueGet("path");
                cXmlNodeChild = cXmlNodeClient.NodeGet("trailers");
				_cClientReplica.sTrailersPath = cXmlNodeChild.AttributeValueGet("path");
				cXmlNodeChild = cXmlNodeClient.NodeGet("ingest");
				_cClientReplica.bIsPgIdNeeded = cXmlNodeChild.AttributeOrDefaultGet<bool>("pg_id_needed", true);
                _cClientReplica.sFilesDialogFilter = cXmlNodeChild.AttributeOrDefaultGet<string>("files_filter", "(*.*)|*.*|H264 files (*.mp4)|*.mp4|QuickTime Movies (*.mov)|*.mov|MPEG Files (*.mpg)|*.mpg|Material eXchange Format (*.mxf)|*.mxf|Audio Video Interleaved (*.avi)|*.avi|Windows Media Video (*.wmv)|*.wmv");
                cXmlNodeChild = cXmlNodeClient.NodeGet("pages/playlist/menu", false);
                if (null == cXmlNodeChild)
                {
                    cXmlNodeChild = cXmlNodeClient.NodeGet("context_menu_playlist", false);
					if (null != cXmlNodeChild)
					{
						_cClientReplica.bContextMenuDeleteSince = cXmlNodeChild.AttributeGet<bool>("delete_all");
						_cClientReplica.nPLRecalculateTimeout = cXmlNodeChild.AttributeGet<int>("recalc_timeout");
						_cClientReplica.nPLImportTimeout = cXmlNodeChild.AttributeGet<int>("import_timeout");
					}
                }
                else
                    _cClientReplica.bContextMenuDeleteSince = cXmlNodeChild.AttributeGet<bool>("deleteSince");
                cXmlNodeChild = cXmlNodeClient.NodeGet("pages/stat/rao", false);
                if (null != cXmlNodeChild)
                    _cClientReplica.bStatisticsRAOVisible = cXmlNodeChild.AttributeGet<bool>("visible");
                cXmlNodeChild = cXmlNodeClient.NodeGet("pages/stat/messages", false);
                if (null != cXmlNodeChild)
                    _cClientReplica.bStatisticsMessagesVisible = cXmlNodeChild.AttributeGet<bool>("visible");

				cXmlNodeChild = cXmlNodeClient.NodeGet("pages/assets/default_class", false);
				if (null != cXmlNodeChild)
				{
					_cClientReplica.sDefautClassClip = cXmlNodeChild.AttributeValueGet("clip", false);
					if (_cClientReplica.sDefautClassClip == null)
						_cClientReplica.sDefautClassClip = "unknown";
					_cClientReplica.sDefautClassProgram = cXmlNodeChild.AttributeValueGet("program", false);
					if (_cClientReplica.sDefautClassProgram == null)
						_cClientReplica.sDefautClassProgram = "unknown";
					_cClientReplica.sDefautClassDesign = cXmlNodeChild.AttributeValueGet("design", false);
					if (_cClientReplica.sDefautClassDesign == null)
						_cClientReplica.sDefautClassDesign = "unknown";
					_cClientReplica.sDefautClassAdvertisement = cXmlNodeChild.AttributeValueGet("advertisement", false);
					if (_cClientReplica.sDefautClassAdvertisement == null)
						_cClientReplica.sDefautClassAdvertisement = "unknown";
					_cClientReplica.sDefautClassUnknown = cXmlNodeChild.AttributeValueGet("unknown", false);
					if (_cClientReplica.sDefautClassUnknown == null)
						_cClientReplica.sDefautClassUnknown = "unknown";
				}
			}
			if (null != (cXmlNodeClient = cXmlNode.NodeGet("social", false)))
			{
                _sOAuthDomain = cXmlNodeClient.AttributeValueGet("domain");
                if (null != (cXmlNodeChild = cXmlNodeClient.NodeGet("facebook", false)))
                {
                    _sFacebookAppID = cXmlNodeChild.AttributeValueGet("app");
                    _sFacebookSecret = cXmlNodeChild.AttributeValueGet("secret");
                }
                if (null != (cXmlNodeChild = cXmlNodeClient.NodeGet("twitter", false)))
                {
                    _sTwitterKey = cXmlNodeChild.AttributeValueGet("key");
                    _sTwitterSecret = cXmlNodeChild.AttributeValueGet("secret");
                }
                _sVKontakteAppID = "";
            }
		}
	}
}
