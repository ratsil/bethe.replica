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
			_aBinds.Add(MapClass.pli_status, ahStrings);
			ahStrings = new StringDictionary();
            ahStrings["planned"] = g.Webservice.sNoticePreferences1;
			ahStrings["queued"] = g.Webservice.sNoticePreferences2;
			ahStrings["prepared"] = g.Webservice.sNoticePreferences3;
			ahStrings["onair"] = g.Webservice.sNoticePreferences4;
			ahStrings["played"] = g.Webservice.sNoticePreferences5;
			ahStrings["skipped"] = g.Webservice.sNoticePreferences6;
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
				public int nFrequencyOfOccurrenceMax;
				public string sPreviewsPath;
				public string sTrailersPath;
                public string sLocale;
                public bool bContextMenuDeleteSince;
                public bool bStatisticsRAOVisible;
                public bool bStatisticsMessagesVisible;

				public Replica()
				{
                    bContextMenuDeleteSince = bStatisticsRAOVisible = bStatisticsMessagesVisible = true;
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
				sServer = "db.channel.replica",
				nPort = 5432,
				sDatabase = "replica",
				sUser = sUsername,
				sPassword = sPassword,
				nTimeout = 240
			};
			if (null == sPassword)
			{
				switch (sUsername)
				{
					case "user":
						cRetVal.sPassword = "";
						break;
					case "replica_client":
						cRetVal.sPassword = "";
						break;
					case "replica_management":
						cRetVal.sPassword = "";
						break;
				}
			}
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

        static public string sClipStorageName;

		private Clients.Replica _cClientReplica;
        private string _sOAuthDomain;
        private string _sFacebookAppID;
        private string _sFacebookSecret;
        private string _sTwitterKey;
        private string _sTwitterSecret;
        private string _sVKontakteAppID;
        private string _sVKontakteSecret;

		public Preferences()
			: base("//webservice")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			XmlNode cXmlNodeChild;
			XmlNode cXmlNodeClient = cXmlNode.NodeGet("storages", false);
			if (null != cXmlNodeClient)
			{
                cXmlNodeChild = cXmlNodeClient.NodeGet("clips");
				_cClientReplica = new Clients.Replica();
                sClipStorageName = cXmlNodeChild.AttributeValueGet("name");
			}
			cXmlNodeClient = cXmlNode.NodeGet("clients/replica", false);
			if (null != cXmlNodeClient)
			{
                cXmlNodeChild = cXmlNodeClient.NodeGet("frames");
                _cClientReplica = new Clients.Replica() { sLocale = sLocale };
                _cClientReplica.nFramesMinimum = cXmlNodeChild.AttributeGet<int>("minimum");
                cXmlNodeChild = cXmlNodeClient.NodeGet("customs");
                _cClientReplica.nFrequencyOfOccurrenceMax = cXmlNodeChild.AttributeGet<int>("occurrence");
                cXmlNodeChild = cXmlNodeClient.NodeGet("previews");
                _cClientReplica.sPreviewsPath = cXmlNodeChild.AttributeValueGet("path");
                cXmlNodeChild = cXmlNodeClient.NodeGet("trailers");
                _cClientReplica.sTrailersPath = cXmlNodeChild.AttributeValueGet("path");
                cXmlNodeChild = cXmlNodeClient.NodeGet("pages/playlist/menu", false);
                if (null == cXmlNodeChild)
                {
                    cXmlNodeChild = cXmlNodeClient.NodeGet("context_menu_playlist", false);
                    if (null != cXmlNodeChild)
                        _cClientReplica.bContextMenuDeleteSince = cXmlNodeChild.AttributeGet<bool>("delete_all");
                }
                else
                    _cClientReplica.bContextMenuDeleteSince = cXmlNodeChild.AttributeGet<bool>("deleteSince");
                cXmlNodeChild = cXmlNodeClient.NodeGet("pages/stat/rao", false);
                if (null != cXmlNodeChild)
                    _cClientReplica.bStatisticsRAOVisible = cXmlNodeChild.AttributeGet<bool>("visible");
                cXmlNodeChild = cXmlNodeClient.NodeGet("pages/stat/messages", false);
                if (null != cXmlNodeChild)
                    _cClientReplica.bStatisticsMessagesVisible = cXmlNodeChild.AttributeGet<bool>("visible");
            }
			cXmlNodeClient = cXmlNode.NodeGet("social", false);
			if (null != cXmlNodeClient)
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
                _sVKontakteAppID = "3930961";
            }
		}
	}
}
