using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

using helpers;
using helpers.extensions;
using System.Linq;

namespace helpers.replica
{
    public enum Error
	{
		no = 0,
		unknown = 1,
		missed = 2
	}
}
namespace helpers.replica.hk
{
	[Serializable]
	public class RegisteredTable
	{
		#region overrides
		override public int GetHashCode()
		{
			string sHash = "";
			sHash = nID + "::";
			sHash += (null == sSchema ? "null" : sSchema) + "::";
			sHash += (null == sName ? "null" : sName) + "::";
			sHash += dtUpdated.ToString("yyyy-MM-dd hh:mm:ss") + "::";
			sHash += (null == sNote ? "null" : sNote) + "::";
			return sHash.GetHashCode();
		}
		override public bool Equals(object cRegisteredTable)
		{
			try
			{
				if (null == cRegisteredTable || !(cRegisteredTable is RegisteredTable))
					return false;
				return this == (RegisteredTable)cRegisteredTable;
			}
			catch { }
			return false;
		}
		static public bool operator ==(RegisteredTable cRegisteredTable1, RegisteredTable cRegisteredTable2)
		{
			if (null == (object)cRegisteredTable1)
			{
				if (null == (object)cRegisteredTable2)
					return true;
				else
					return false;
			}
			else if (null == (object)cRegisteredTable2)
				return false;
			else if (cRegisteredTable1.GetHashCode() == cRegisteredTable2.GetHashCode())
				return true;
			return false;
		}
		static public bool operator !=(RegisteredTable cRegisteredTable1, RegisteredTable cRegisteredTable2)
		{
			return !(cRegisteredTable1 == cRegisteredTable2);
		}
		#endregion

		static private Dictionary<long, RegisteredTable> _aLoadCache;
		public RegisteredTable()
		{ }
		static public RegisteredTable Load(string sSchema, string sName)
		{
			if (null == _aLoadCache)
				_aLoadCache = new Dictionary<long, RegisteredTable>();
			{
				foreach (RegisteredTable cRegisteredTable in _aLoadCache.Values)
					if (sName == cRegisteredTable.sName)
						return cRegisteredTable;
			}
			{
				RegisteredTable cRegisteredTable;
				cRegisteredTable = DBInteract.cCache.RegisteredTableGet(sSchema, sName);
				_aLoadCache.Add(cRegisteredTable.nID, cRegisteredTable);
				return cRegisteredTable;
			}
		}
		static public RegisteredTable Load(long nID)
		{
			if (null == _aLoadCache)
				_aLoadCache = new Dictionary<long, RegisteredTable>();
			if (!_aLoadCache.ContainsKey(nID))
			{
				RegisteredTable cRegisteredTable = DBInteract.cCache.RegisteredTableGet(nID);
				_aLoadCache.Add(cRegisteredTable.nID, cRegisteredTable);
			}
			return _aLoadCache[nID];
		}

		public long nID;
		public string sSchema;
		public string sName;
		public DateTime dtUpdated;
		public string sNote;
		public string sFullQualifiedName
		{
			get
			{
				if (null == sName)
					return null;
				string sRetVal = sSchema;
				if (null != sSchema)
				{
					if (sSchema != sSchema.ToLower())
						sRetVal = "`" + sRetVal + "`";
					sRetVal += ".";
				}
				else
					sRetVal = "";
				if (sName != sName.ToLower())
					sRetVal += "`" + sName + "`";
				else
					sRetVal += sName;
				return sRetVal;
			}
		}

		public RegisteredTable(long nID, string sSchema, string sName, DateTime dtUpdated, string sNote)
		{
			this.nID = nID;
			this.sSchema = sSchema;
			this.sName = sName;
			this.dtUpdated = dtUpdated;
			this.sNote = sNote;
		}
		public RegisteredTable(object oID, object oSchema, object oName, object oUpdated, object oNote)
			: this(oID.ToID(), oSchema.ToString(), oName.ToString(), oUpdated.ToDT(), oNote.ToStr()) { }
		public RegisteredTable(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sSchema"], ahDBRow["sName"], ahDBRow["dtUpdated"], ahDBRow["sNote"]) { }
	};

}
namespace helpers.replica.media
{
	[Serializable]
	public class Storage
	{
		#region overrides
		override public int GetHashCode()
		{
			string sHash = "";
			sHash = nID + "::";
			sHash += (null == sName? "null" : sName) + "::";
			sHash += (null == sPath ? "null" : sPath) + "::";
			sHash += bEnabled + "::";
			sHash += (null == cType ? "null" : cType.GetHashCode().ToString()) + "::";
			return sHash.GetHashCode();
		}
		override public bool Equals(object cStorage)
		{
			try
			{
				return this == (Storage)cStorage;
			}
			catch { }
			return false;
		}
		static public bool operator ==(Storage cStorage1, Storage cStorage2)
		{
			if (object.ReferenceEquals(null, cStorage1) && object.ReferenceEquals(null, cStorage2))
                return true;
			if (object.ReferenceEquals(null, cStorage1) || object.ReferenceEquals(null, cStorage2)) 
                return false;

			if (cStorage1.GetHashCode() == cStorage2.GetHashCode())
				return true;
			return false;
		}
		static public bool operator !=(Storage cStorage1, Storage cStorage2)
		{
			return !(cStorage1 == cStorage2);
		}
		#endregion

		static private Dictionary<long, Storage> _aLoadCache;
        static public Storage Load(string sName)
        {
            Storage cRetVal;
            if (null == _aLoadCache)
                _aLoadCache = new Dictionary<long, Storage>();
            if (null == (cRetVal = _aLoadCache.Values.FirstOrDefault(o => sName == o.sName)))
            {
                cRetVal = DBInteract.cCache.StorageGet(sName);
                _aLoadCache.Add(cRetVal.nID, cRetVal);
            }
            return cRetVal;
        }
        static public Storage Load(System.IO.DirectoryInfo cDirectoryInfo)
        {
            Storage cRetVal = null;
            if (cDirectoryInfo.Exists)
            {
                if (null == _aLoadCache)
                    _aLoadCache = new Dictionary<long, Storage>();
                string sPath = cDirectoryInfo.FullName.ToPath().ToLower();
                if (null == (cRetVal = _aLoadCache.Values.FirstOrDefault(o => sPath == o.sPath.ToPath().ToLower())))
                {
                    cRetVal = DBInteract.cCache.StorageGetByPath(sPath);
                    _aLoadCache.Add(cRetVal.nID, cRetVal);
                }
            }
            else
                throw new System.IO.DirectoryNotFoundException(cDirectoryInfo.FullName);
            return cRetVal;
        }
        static public Storage Load(long nID)
        {
            if (null == _aLoadCache)
                _aLoadCache = new Dictionary<long, Storage>();
            if (!_aLoadCache.ContainsKey(nID))
            {
                Storage cStorage = DBInteract.cCache.StorageGet(nID);
                _aLoadCache.Add(cStorage.nID, cStorage);
            }
            return _aLoadCache[nID];
        }

		public long nID;
		public string sName;
		public string sPath;
		public bool bEnabled;
		public IdNamePair cType;

        public Storage()
		{
            nID = extensions.x.ToID(null);
            sName = sPath = null;
            bEnabled = false;
            cType = null;
        }
		public Storage(long nID, string sName, string sPath, bool bEnabled, IdNamePair cType)
		{
			this.nID = nID;
			this.sName = sName;
			this.sPath = sPath;
			this.bEnabled = bEnabled;
			this.cType = cType;
		}
		public Storage(object cID, object cName, object cPath, object cEnabled, object cTypeID, object cTypeName)
			: this(cID.ToID(), cName.ToString(), cPath.ToString(), cEnabled.ToBool(), new IdNamePair(cTypeID.ToID(), cTypeName.ToString())) { }
		public Storage(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sName"], ahDBRow["sPath"], ahDBRow["bEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sTypeName"]) { }
	};
	[Serializable]
	public class File
	{
        [Serializable]
        public class Ingest
        {
            [Serializable]
            [XmlType("IngestClip")]
            public class Clip : Ingest
            {
                public mam.Person[] aArtists;
                public string sSongName;
                public byte nQuality;
                public bool bLocation;
                public bool bRemix;
                public bool bPromo;
                public bool bCutted;
                public bool bForeign;

                public Clip()
                { }
            }
            [Serializable]
            [XmlType("IngestAdvertisement")]
            public class Advertisement : Ingest
            {
                public string sID;
				public string sCompany;
				public string sCampaign;

                public Advertisement()
                { }
            }
            [Serializable]
            [XmlType("IngestProgram")]
            public class Program : Ingest
            {
                public mam.Asset cSeries;
                public mam.Asset cEpisode;
                public ulong nPart;

                public Program()
                { }
            }
            [Serializable]
            [XmlType("IngestDesign")]
            public class Design : Ingest
            {
				public string sSeason;
				public string sType;
				public bool bDTMF;
                public string sName;

                public Design()
                { }
            }

            public Storage cStorage;
            public string sFilename;
            public string sOriginalFile;
            public sbyte nAge;
            public bool bBroadcast;
            public byte? nVersion;
            public int nFormat;
            public byte nFPS;

            public Ingest()
            { }
        }

		#region overrides
		override public int GetHashCode()
		{
			string sHash = "";
			sHash = nID + "::";
			sHash += (null == sFilename ? "null" : sFilename) + "::";
			sHash += dtLastEvent + "::";
			sHash += cStorage.GetHashCode() + "::";
			return sHash.GetHashCode();
		}
		override public bool Equals(object cFile)
		{
			try
			{
				return this == (File)cFile;
			}
			catch { }
			return false;
		}
		static public bool operator ==(File cFile1, File cFile2)
		{
			if (object.ReferenceEquals(null, cFile1) && object.ReferenceEquals(null, cFile2))
                return true;
			if (object.ReferenceEquals(null, cFile1) || object.ReferenceEquals(null, cFile2)) 
                return false;
			if (cFile1.GetHashCode() == cFile2.GetHashCode())
				return true;
			return false;
		}
		static public bool operator !=(File cFile1, File cFile2)
		{
			return !(cFile1 == cFile2);
		}
		#endregion

		static private Dictionary<long, File> _aLoadCache;
        static public File Load(Storage cStorage, string sFilename)
		{
            File cRetVal;
			if (null == _aLoadCache)
                _aLoadCache = new Dictionary<long, File>();
            if (null == (cRetVal = _aLoadCache.Values.FirstOrDefault(o => cStorage == o.cStorage && sFilename == o.sFilename)))
            {
                if(null != (cRetVal = DBInteract.cCache.FileGet(cStorage, sFilename)))
					_aLoadCache.Add(cRetVal.nID, cRetVal);
			}
            return cRetVal;
        }
        static public File Load(long nID)
		{
			if (null == _aLoadCache)
                _aLoadCache = new Dictionary<long, File>();
			if (!_aLoadCache.ContainsKey(nID))
			{
                File cFile = DBInteract.cCache.FileGet(nID);
				if (null != cFile)
					_aLoadCache.Add(cFile.nID, cFile);
			}
			return _aLoadCache[nID];
		}
        static public File Create(Storage cStorage, string sFilename)
		{
			if (null == _aLoadCache)
                _aLoadCache = new Dictionary<long, File>();
			if (null != cStorage)
			{
				File cFile = DBInteract.cCache.FileAdd(cStorage.nID, sFilename);
				_aLoadCache.Add(cFile.nID, cFile);
				return cFile;
			}
			return null;
		}

		public long nID;
		public string sFilename;
		public Storage cStorage;
		public DateTime dtLastEvent;
		public Error eError;

		public string sFile
		{
			get
			{
				return cStorage.sPath + sFilename;
			}
		}

        public File()
		{
            nID = extensions.x.ToID(null);
            sFilename = null;
            cStorage = null;
            dtLastEvent = x.ToDT(null);
            eError = Error.no;
        }
		public File(long nID, string sFilename, Storage cStorage, DateTime dtLastEvent, Error eError)
		{
			this.nID = nID;
			this.sFilename = sFilename;
            this.cStorage = cStorage;
			this.dtLastEvent = dtLastEvent;
			this.eError = eError;
		}
        public File(object oID, object oFilename, Storage cStorage, object oLastEvent, object oError)
            : this(oID.ToID(), oFilename.ToString(), cStorage, oLastEvent.ToDT(), (null == oError ? Error.no : (Error)Enum.Parse(typeof(Error), oError.ToString()))) { }
		public File(object oFileID, object oFilename, object oStorageID, object oStorageName, object oStoragePath, object oStorageEnabled, object oStorageTypeID, object oStorageTypeName, object oLastEvent, object oError)
			: this(oFileID.ToID(), oFilename.ToString(), new Storage(oStorageID, oStorageName, oStoragePath, oStorageEnabled, oStorageTypeID, oStorageTypeName), oLastEvent, oError)
		{ }
		public File(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sFilename"], ahDBRow["idStorages"], ahDBRow["sStorageName"], ahDBRow["sPath"], ahDBRow["bStorageEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sStorageTypeName"], ahDBRow["dtLastFileEvent"], ahDBRow["eError"])
		{ }
	};
}
namespace helpers.replica.mam
{
    static internal class x
    {
        static public Macro[] MacrosGet(this DBInteract cDBI)
        {
            return cDBI.RowsGet("SELECT * FROM mam.`vMacros`").Select(o => new Macro(o)).ToArray();
        }
        static public Macro MacroGet(this DBInteract cDBI, string sName)
        {
            Macro cRetVal = null;
            try
            {
                cRetVal = new Macro(cDBI.RowGet("SELECT * FROM mam.`vMacros` WHERE `sName`='" + sName + "'"));
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return cRetVal;
        }
        static public string MacroExecute(this DBInteract cDBI, Macro cMacro)
        {
            if ("value" == cMacro.cType.sName) //EMERGENCY:l это зачем такое? мне что-то подсказывает, что подобное делается иначе и через рот... а не так...
                return cMacro.sValue;
            else
                return cDBI.ValueGet(cMacro.sValue);
        }
    }
    public class Macro
    {
		public enum Flags
		{
			None = 0,
			Caps = 1,
			Escaped = 2
		}
		static public Flags ParseFlags(ref string sText)
		{
			Flags eRetVal = Flags.None;
			int nPos = sText.LastIndexOf("::");
			string sFlags = sText.Substring(nPos + 2);
			string sFlag;
			foreach (Flags eFlag in Enum.GetValues(typeof(Flags)))
			{
				sFlag = eFlag.ToString().ToUpper();
				if (sFlags.Contains(sFlag))
					eRetVal |= eFlag;
			}
			if (Flags.None != eRetVal)
				sText = sText.Substring(0, nPos) + "%}";
			return eRetVal;
		}
		static public Macro Get(string sName)
        {
            return DBInteract.cCache.MacroGet(sName);
        }
        public long nID;
        public IdNamePair cType;
        public string sName;
        public string sValue;
        public Macro()
            : this(0, null, "", "")
        { }
        public Macro(long nID, IdNamePair cType, string sName, string sValue)
        {
            this.nID = nID;
            this.sName = sName;
            this.sValue = sValue;
            this.cType = cType;
        }
        public Macro(Hashtable ahDBRow)
            : this(ahDBRow["id"].ToID(), new IdNamePair(ahDBRow["idMacroTypes"], ahDBRow["sMacroTypeName"]), ahDBRow["sName"].ToString(), ahDBRow["sValue"].ToString())
        { }

        public string Execute()
        {
            return DBInteract.cCache.MacroExecute(this);
        }
    };
    public struct Cues //TODO переделать в класс
	{
		public long nID;
		public string sSong;
		public string sArtist;
		public string sAlbum;
		public string sYear;
		public string sPossessor;
		public Cues(long nID, string sSong, string sArtist, string sAlbum, int nYear, string sPossessor)
		{
			this.nID = nID;
			this.sSong = sSong;
			this.sArtist = sArtist;
			this.sAlbum = sAlbum;
			this.sYear = 0 > nYear ? null : nYear.ToString();
			this.sPossessor = sPossessor;
		}
		public Cues(object oID, object oSong, object oArtist, object oAlbum, object oYear, object oPossessor)
			: this(oID.ToID(), (null == oSong ? null : oSong.ToString()), (null == oArtist ? null : oArtist.ToString()), (null == oAlbum ? null : oAlbum.ToString()), (null == oYear ? -1 : oYear.ToInt32()), (null == oPossessor ? null : oPossessor.ToString())) { }
	};
	public struct Video //TODO переделать в класс
	{
		public long nID;
		public string sName;
		public IdNamePair cType;
		public Video(long nID, string sName, IdNamePair cType)
		{
			this.nID = nID;
			this.sName = sName;
			this.cType = cType;
		}
		public Video(long nID, string sName, long nTypeID, string sTypeName)
			: this(nID, sName,  new IdNamePair(nTypeID, sTypeName)){}
		public Video(object nID, object sName, object nTypeID, object sTypeName)
			: this(nID.ToID(), sName.ToString(), nTypeID.ToID(), sTypeName.ToString()){}
	};
	public class Person
	{
		static public Person Load(long nID)
		{
			return DBInteract.cCache.PersonGet(nID);
		}

		public long nID;
		public string sName;
		public IdNamePair cType;
        public Person()
            : this(-1, null) { }
		public Person(long nID, string sName, IdNamePair cType)
		{
			this.nID = nID;
			this.sName = sName;
			this.cType = cType;
		}
		public Person(long nID, string sName, string sTypeName, long nTypeID)
			: this(nID, sName, new IdNamePair(nTypeID, sTypeName)){}
		public Person(long nID, string sName)
			: this(nID, sName, null) { }
		public Person(object nID, object sName)
			: this(nID.ToID(), sName.ToString(), null) { }
		public Person(Hashtable ahDBRow)
			: this(ahDBRow["id"].ToID(), ahDBRow["sName"].ToString(), ahDBRow["sPersonTypeName"].ToString(), ahDBRow["idPersonTypes"].ToID()) { }
		public override string ToString()
		{
			return sName;
		}
	};
	public struct SoundLevels //TODO переделать в класс
	{
		public IdNamePair cStart;
		public IdNamePair cStop;
		public SoundLevels(IdNamePair cStart, IdNamePair cStop)
		{
			this.cStart = cStart;
			this.cStop = cStop;
		}
		public SoundLevels(int idStart, string sStart, int idStop, string sStop)
			: this(new IdNamePair(idStart, sStart), new IdNamePair(idStop, sStop)){}
		public SoundLevels(object idStart, object sStart, object idStop, object sStop)
			: this(new IdNamePair(idStart, sStart), new IdNamePair(idStop, sStop)){}
	};
	public struct CustomValue //TODO переделать в класс
	{
		public long nID;
		public string sName;
		public string sValue;
		public CustomValue(long nID, string sName, string sValue)
		{
			this.nID = nID;
			this.sName = sName;
			this.sValue = sValue;
		}
		public CustomValue(string sName, string sValue)
			: this(-1, sName, sValue){}
	}
    [Serializable]
	public class Asset
	{
		public class Type
		{
			public enum AssetType
			{
				part = 0,
				episode = 1,
				series = 2
			}
			public long nID;
			public AssetType eType;
			public Type()
			{
				nID = -1;
			}
			public Type(object oID, object oType)
			{
				nID = oID.ToID();
				if (null != oType)
					eType = (AssetType)Enum.Parse(typeof(AssetType), oType.ToString());
			}
		}

		static public Asset Load(long nID)
		{
			return DBInteract.cCache.AssetGet(nID);
		} //TODO убрать статик
		static public Asset Load(CustomValue stCV)
		{
			return DBInteract.cCache.AssetGet(stCV);
		}

		public long nID;
		public string sName;
		public Video stVideo;
		public CustomValue[] aCustomValues;
		public helpers.replica.media.File cFile;
		private long _nFrameIn;
		public long nFrameIn
		{
			get
			{
				return _nFrameIn;
			}
			set
			{
				_nFrameIn = value;
				if (0 > _nFrameIn)
					_nFrameIn = 1;
			}
		}
		public long nFrameOut;
		public long nFramesQty;
		public DateTime dtLastPlayed;
		public bool bEnabled;
		public helpers.replica.pl.Class cClass;
		public long nIDParent;  //EMERGENCY:l обычно у нас nParentID, хотя в твоем варианте есть иерархический смысл)
		public Type cType;

		public Asset()
		{
			nID = extensions.x.ToID(null);
			sName = "";
            cFile = null;
            nFramesQty = -1;
			nFrameIn = -1;
			nFrameOut = -1;
			dtLastPlayed = DateTime.MaxValue;
			cType = null;
		}
		public Asset(Hashtable ahDBRow)
			: this()
		{
			try
			{
				nID = ahDBRow["id"].ToID();
				sName = ahDBRow["sName"].ToString();
				if (null != ahDBRow["idVideos"])
					stVideo = new Video(ahDBRow["idVideos"], (null == ahDBRow["sVideoName"] ? "" : ahDBRow["sVideoName"]), ahDBRow["idVideoTypes"], ahDBRow["sVideoTypeName"]);
				if (null != ahDBRow["idFiles"])
					cFile = new helpers.replica.media.File(ahDBRow["idFiles"], ahDBRow["sFilename"], ahDBRow["idStorages"], ahDBRow["sStorageName"], ahDBRow["sPath"], ahDBRow["bStorageEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sStorageTypeName"], ahDBRow["dtLastFileEvent"], ahDBRow["eFileError"]);
				if (null != ahDBRow["idClasses"])
					cClass = new helpers.replica.pl.Class(ahDBRow["idClasses"],ahDBRow["sClassName"]);
				nFrameIn = ahDBRow["nFrameIn"].ToLong();
				if (0 == nFrameIn)
					nFrameIn = 1;
				nFrameOut = ahDBRow["nFrameOut"].ToLong();
				nFramesQty = ahDBRow["nFramesQty"].ToLong();
				if (1 > nFrameOut)
					nFrameOut = nFramesQty;
				dtLastPlayed = ahDBRow["dtLastPlayed"].ToDT();
				bEnabled = ahDBRow["bPLEnabled"].ToBool();
				if (null != ahDBRow["sType"])
					cType = new Type(ahDBRow["idType"], ahDBRow["sType"]);
				nIDParent = ahDBRow["idParent"].ToID();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
	}
    [Serializable]
	public class Clip : Asset
	{
		static private IdNamePair _cVideoType = null;
		static public IdNamePair cVideoType
		{
			get
			{
				if (null == _cVideoType)
				{
					if (null == DBInteract.cCache)
                        return null;
					foreach (IdNamePair cINP in DBInteract.cCache.AssetVideoTypesGet())
					{
						if ("clip" == cINP.sName)
						{
							_cVideoType = cINP;
							break;
						}
					}
				}
				return _cVideoType;
			}
		}
		new static public Clip Load(long nID)
		{
			return DBInteract.cCache.ClipGet(nID);
		}

		public Person[] aPersons;
		public IdNamePair[] aStyles;
		public IdNamePair[] aAlbums;
		public IdNamePair cRotation;
		public IdNamePair cPalette;
		public bool bSmoking;
		public SoundLevels stSoundLevels;
        public void PersonsLoad()
        {
			aPersons = DBInteract.cCache.ArtistsLoad(nID);
        }
		public Cues stCues;
		public Clip()
			: base()
		{
			base.stVideo.cType = cVideoType;
			bSmoking = false;
		}
		public Clip(Hashtable ahDBRow)
			: base(ahDBRow)
		{
			aPersons = new Person[ahDBRow["nPersonsQty"].ToCount()];
			aAlbums = new IdNamePair[ahDBRow["nAlbumsQty"].ToCount()];
			aStyles = new IdNamePair[ahDBRow["nStylesQty"].ToCount()];
			if (null != ahDBRow["idRotations"])
				cRotation = new IdNamePair(ahDBRow["idRotations"], ahDBRow["sRotationName"]);
			if (null != ahDBRow["idPalettes"])
				cPalette = new IdNamePair(ahDBRow["idPalettes"], ahDBRow["sPaletteName"]);
			if (null != ahDBRow["idSoundLevelsForStart"] && null != ahDBRow["idSoundLevelsForStop"])
				stSoundLevels = new SoundLevels(ahDBRow["idSoundLevelsForStart"], ahDBRow["sSoundLevelNameForStart"], ahDBRow["idSoundLevelsForStop"], ahDBRow["sSoundLevelNameForStop"]);
			if (null != ahDBRow["idCues"])
				stCues = new Cues(ahDBRow["idCues"], ahDBRow["sCueSong"], ahDBRow["sCueArtist"], ahDBRow["sCueAlbum"], ahDBRow["nCueYear"], ahDBRow["sCuePossessor"]);
		}
	}
	[Serializable]
	public class Advertisement : Asset
	{
		static private IdNamePair _cVideoType = null;
		static public IdNamePair cVideoType
		{
			get
			{
				if (null == _cVideoType)
				{
					if (null == DBInteract.cCache)
                        return null;
					foreach (IdNamePair cINP in DBInteract.cCache.AssetVideoTypesGet())
					{
						if ("advertisement" == cINP.sName)
						{
							_cVideoType = cINP;
							break;
						}
					}
				}
				return _cVideoType;
			}
		}

		public Advertisement()
			: base()
		{
			base.stVideo.cType = cVideoType;
		}
		public Advertisement(Hashtable ahDBRow)
			: base(ahDBRow) { }
	}
    [Serializable]
    public class Design : Asset
    {
        static private IdNamePair _cVideoType = null;
        static public IdNamePair cVideoType
        {
            get
            {
                if (null == _cVideoType)
                {
					if (null == DBInteract.cCache)
                        return null;
					foreach (IdNamePair cINP in DBInteract.cCache.AssetVideoTypesGet())
                    {
                        if ("design" == cINP.sName)
                        {
                            _cVideoType = cINP;
                            break;
                        }
                    }
                }
                return _cVideoType;
            }
        }

        public Design()
            : base()
        {
            base.stVideo.cType = cVideoType;
        }
        public Design(Hashtable ahDBRow)
            : base(ahDBRow) { }
    }
	[Serializable]
	public class Program : Asset
	{
		public class RAOInfo
		{
			public Clip cClip;
			public long nFramesQty;
			public RAOInfo()
			{
				cClip = null;
				nFramesQty = -1;
			}
		} //EMERGENCY:l Валяяяяя!!!! этого класса тут не должно быть!!!! РАО - это частный случай
		static private IdNamePair _cVideoType = null;
		static public IdNamePair cVideoType
		{
			get
			{
				if (null == _cVideoType)
				{
					if (null == DBInteract.cCache)
                        return null;
					foreach (IdNamePair cINP in DBInteract.cCache.AssetVideoTypesGet())
					{
						if ("program" == cINP.sName)
						{
							_cVideoType = cINP;
							break;
						}
					}
				}
				return _cVideoType;
			}
		}
		new static public Program Load(long nID)
		{
			return DBInteract.cCache.ProgramGet(nID);
		}

        public RAOInfo[] aRAOInfo;
        public void RAOInfoLoad()
        {
			aRAOInfo = DBInteract.cCache.ProgramRAOInfo_old_and_new_Get(this).ToArray();
        }
		public Program()
			: base() {}
		public Program(Hashtable ahDBRow)
			: base(ahDBRow) 
        {
        }
	}
}
namespace helpers.replica.pl
{
    static internal class x
    {
        static public Proxy ProxyGet(this DBInteract cDBI, long nID)
        {
            return cDBI.ProxiesGet(nID + "=id").FirstOrDefault();
        }
        static public Proxy ProxyGet(this DBInteract cDBI, Class cClass)
        {
            if (null == cClass)
                return null;
            return cDBI.ProxiesGet(cClass.nID + " = `idClasses`").FirstOrDefault();
        }
        static public Proxy[] ProxiesGet(this DBInteract cDBI)
        {
            return cDBI.ProxiesGet("");
        }
        static private Proxy[] ProxiesGet(this DBInteract cDBI, string sWhere)
        {
            return cDBI.RowsGet("SELECT * FROM pl.`tProxies` " + (sWhere.IsNullOrEmpty() ? "" : "WHERE " + sWhere)).Select(o => new Proxy(o)).ToArray();
        }
    }
    [Serializable]
	public class PlaylistItem
	{
        static public PlaylistItem Get(long nID)
		{
            return DBInteract.cCache.PlaylistItemGet(nID);
		}
		public long nID;
        public bool bCached;
        public string sName;
        public long nFramesQty;
		public long nFrameStart;
		public long nFrameStop;
		public long nFrameCurrent;
		public ulong nDuration
		{
			get
			{
                return (ulong)(nFrameStop - nFrameStart + 1);
			}
		}
        public IdNamePair cStatus;
		public Class cClass;
		public bool bIsAdv;
		public helpers.replica.media.File cFile;

		public DateTime dtStartPlanned { get; set; }
		public DateTime dtStopPlanned
		{
			get
			{
                return dtStart.AddMilliseconds(nDuration * 40); //TODO FPS
			}
		}
		public DateTime dtStartReal { get; set; }
		public DateTime dtStopReal { get; set; }
		public DateTime dtStartHard { get; set; }
		public DateTime dtStartSoft { get; set; }
		public DateTime dtTimingsUpdate { get; set; }
		public DateTime dtStartQueued { get; set; }
		public DateTime dtStart
		{
			get 
			{
				if (DateTime.MaxValue > dtStartReal)
					return dtStartReal;
				if (DateTime.MaxValue > dtStartQueued)
					return dtStartQueued;
				return dtStartPlanned;
			}
		}
		public DateTime dtStop
		{
			get
			{
				return (DateTime.MaxValue == dtStopReal ? dtStopPlanned : dtStopReal);
			}
		}
		public DateTime dtStartHardSoft
		{
			get
			{
				return DateTime.MaxValue > dtStartHard ? dtStartHard : dtStartSoft;
			}
		}
		public DateTime dtStartHardSoftPlanned
		{
			get
			{
				return DateTime.MaxValue > dtStartHardSoft ? dtStartHardSoft : dtStartPlanned;
			}
		}

		public bool bLocked
		{
			get
			{
				return ("planned" != cStatus.sName);
			}
		}

		public string sNote;
		public bool bPlug;

		//public BeepInfo cBeepInfo; //DEPRECATED

		public mam.Asset cAsset;

		public PlaylistItem()
		{
			nID = extensions.x.ToID(null);
			sName = "";
			nFramesQty = -1;
			nFrameCurrent = -1;
			cFile = null;
			cStatus = null;
			bIsAdv = false;
			cAsset = null;
			dtStartHard = DateTime.MaxValue;
			dtStartSoft = DateTime.MaxValue;
			dtStartPlanned = DateTime.MaxValue;
			dtStartQueued = DateTime.MaxValue;
			dtStartReal = DateTime.MaxValue;
			dtStopReal = DateTime.MaxValue;
            dtTimingsUpdate = DateTime.MaxValue;
		}
		public PlaylistItem(Hashtable ahDBRow)
			: this()
		{
			nID = ahDBRow["id"].ToID();
			sName = ahDBRow["sName"].ToString();
			nFramesQty = ahDBRow["nFramesQty"].ToLong();
			nFrameStart = ahDBRow["nFrameStart"].ToLong();
			nFrameStop = ahDBRow["nFrameStop"].ToLong();
			if (null != ahDBRow["nFrameCurrent"])
				nFrameCurrent = ahDBRow["nFrameCurrent"].ToLong();

			cClass = new Class(ahDBRow["idClasses"].ToID(), ahDBRow["sClassName"].ToString());
			bIsAdv = cClass.sName.Contains("advertisement");

			if (null == ahDBRow["idStorages"])
				cFile = new helpers.replica.media.File(ahDBRow["idFiles"], ahDBRow["sFilename"], null, ahDBRow["dtLastFileEvent"], ahDBRow["eFileError"]);
			else
				cFile = new helpers.replica.media.File(ahDBRow["idFiles"], ahDBRow["sFilename"], ahDBRow["idStorages"], ahDBRow["sStorageName"], ahDBRow["sPath"], ahDBRow["bStorageEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sStorageTypeName"], ahDBRow["dtLastFileEvent"], ahDBRow["eFileError"]);

			cStatus = new IdNamePair(ahDBRow["idStatuses"], ahDBRow["sStatusName"]);

			dtStartPlanned = ahDBRow["dtStartPlanned"].ToDT();
			dtStartHard = ahDBRow["dtStartHard"].ToDT();
			dtStartSoft = ahDBRow["dtStartSoft"].ToDT();
            dtStartQueued = ahDBRow["dtStartQueued"].ToDT();
			dtStartReal = ahDBRow["dtStartReal"].ToDT();
			dtStopReal = ahDBRow["dtStopReal"].ToDT();
            dtTimingsUpdate = ahDBRow["dtTimingsUpdate"].ToDT();

			sNote = (null == ahDBRow["sNote"] ? "" : ahDBRow["sNote"].ToString());
			bPlug = ahDBRow["bPlug"].ToBool();

			if (null != ahDBRow["idAssets"])
			{
				cAsset = new mam.Asset();
				cAsset.nID = ahDBRow["idAssets"].ToID();
			}
		}


		override public string ToString()
		{
			string sRetVal = "id:" + nID + ";";
			sRetVal += "status:" + (null == cStatus || null == cStatus.sName ? "NULL" : cStatus.sName) + ";";
			sRetVal += "file:" + (null == cFile.sFile ? "NULL" : cFile.sFile) + ";";
			
			if (DateTime.MaxValue > dtStartReal)
				sRetVal += "sr:" + dtStartReal.ToStr();
			if (DateTime.MaxValue > dtStopReal)
				sRetVal += ":" + dtStopReal.ToStr();
			sRetVal += ";";

			if (DateTime.MaxValue > dtStartQueued)
				sRetVal += "sq:" + dtStartQueued.ToStr() + ";";
			sRetVal += "sp:" + dtStartPlanned.ToStr() + ":" + dtStopPlanned.ToStr() + ";";
			if (DateTime.MaxValue > dtStartHard)
				sRetVal += "sh:" + dtStartHard.ToStr() + ";";
			else if (DateTime.MaxValue > dtStartSoft)
				sRetVal += "ss:" + dtStartSoft.ToStr() + ";";
			sRetVal += "fq:" + nFramesQty + ";";
			sRetVal += "in:" + nFrameStart + ";";
			sRetVal += "out:" + nFrameStop + ";";
			sRetVal += "dur:" + nDuration + ";";
			sRetVal += "fc:" + nFrameCurrent + ";";
			return sRetVal;
		}
	}

	public class Class
	{
		static private Dictionary<long, Class> _aLoadCache;
		static public Class Load(string sName)
		{
			if (null == _aLoadCache)
				_aLoadCache = new Dictionary<long, Class>();
			{
				foreach (Class cClass in _aLoadCache.Values)
					if (sName == cClass.sName)
						return cClass;
			}
			{
				Class cClass;
				cClass = DBInteract.cCache.ClassGet(sName);
				_aLoadCache.Add(cClass.nID, cClass);
				return cClass;
			}
		}
		static public Class Load(long nID)
		{
			if (null == _aLoadCache)
				_aLoadCache = new Dictionary<long, Class>();
			if (!_aLoadCache.ContainsKey(nID))
			{
				Class cClass = DBInteract.cCache.ClassGet(nID);
				_aLoadCache.Add(cClass.nID, cClass);
			}
			return _aLoadCache[nID];
		}

		public long nID;
		public string sName;
		public bool bResolved;
		[System.Xml.Serialization.XmlIgnore]
		public Class cTestator;
		[System.Xml.Serialization.XmlIgnore]
		public Dictionary<long, Class> aHeritors;
		public Class()
		{
            nID = extensions.x.ToID(null);
			sName = null;
			bResolved = false;
			cTestator = null;
			aHeritors = null;
		}
		public Class(long nID, string sName)
			: this()
		{
			this.nID = nID;
			this.sName = sName;
		}
		public Class(object nID, object sName)
			: this(nID.ToID(), sName.ToString()) { }

		public override int GetHashCode()
		{
			return nID.GetHashCode();
		}
	}
    public class Proxy
    {
        static public Proxy Get(long nID)
        {
            return DBInteract.cCache.ProxyGet(nID);
        }
        static public Proxy Get(Class cClass)
        {
            return DBInteract.cCache.ProxyGet(cClass);
        }
        static public Proxy[] Get()
        {
            return DBInteract.cCache.ProxiesGet();
        }

        public long nID { get; set; }
        public Class cClass { get; set; }
        public string sName { get; set; }
        public string sFile { get; set; }
        public Proxy()
        { }
        public Proxy(long nID, long nClassID, string sName, string sFile)
        {
            this.nID = nID;
            this.cClass = new Class(nClassID, null);
            this.sName = sName;
            this.sFile = sFile;
        }
        public Proxy(Hashtable ahDBRow)
            : this(ahDBRow["id"].ToID(), ahDBRow["idClasses"].ToID(), ahDBRow["sName"].ToString(), ahDBRow["sFile"].ToString())
        { }
    };
}
namespace helpers.replica.cues
{
    static internal class x
    {
        static public Template TemplateGet(this DBInteract cDBI, long nID)
        {
            return cDBI.TemplatesGet(nID + "=id").FirstOrDefault();
        }
        static public Template[] TemplatesGet(this DBInteract cDBI)
        {
            return cDBI.TemplatesGet("");
        }
        static public Template[] TemplatesGet(this DBInteract cDBI, string sWhere)
        {
            return cDBI.RowsGet("SELECT * FROM cues.`tTemplates` " + (sWhere.IsNullOrEmpty() ? "" : "WHERE " + sWhere)).Select(o => new Template(o)).ToArray();
        }
		static public void TemplateStarted(this DBInteract cDBI, Template cTemplate, pl.PlaylistItem cPLI)
		{
			cDBI.Perform("SELECT * FROM cues.`fTemplateStarted`(" + cTemplate.nID + ", " + cPLI.nID + ")");
		}
	}
    
    public class Template
	{
        static public Template Get(long nID)
        {
            return DBInteract.cCache.TemplateGet(nID);
        }
		static public Template[] Get()
		{
			return DBInteract.cCache.TemplatesGet();
		}

		public long nID { get; set; }
		public string sName { get; set; }
		public string sFile { get; set; }
		public Template()
		{ }
		public Template(long nID, string sName, string sFile)
		{
			this.nID = nID;
			this.sName = sName;
			this.sFile = sFile;
		}
		public Template(Hashtable ahDBRow)
			: this(ahDBRow["id"].ToID(), ahDBRow["sName"].ToString(), ahDBRow["sFile"].ToString())
		{ }

		public void Started(pl.PlaylistItem cPLI)
		{
			DBInteract.cCache.TemplateStarted(this, cPLI);
		}
	};
    public class TemplateBind
    {
        public long nID { get; set; }
        public pl.Class cClass { get; set; }
        public Template cTemplate { get; set; }
        public hk.RegisteredTable cRegisteredTable { get; set; }
        public string sKey { get; set; }
        public long nValue { get; set; }

		public TemplateBind()
		{ }
        public TemplateBind(long nID)
        {
            this.nID = nID;
        }
		public TemplateBind(long nID, long nClassID, long nTemplateID, long nRegisteredTableID, string sKey, long nValue, string sTemplateName, string sTemplateFile)
        {
            this.nID = nID;
            this.cClass = new pl.Class(nClassID, null);
			this.cTemplate = new Template(nTemplateID, sTemplateName, sTemplateFile);
            this.cRegisteredTable = new hk.RegisteredTable(nRegisteredTableID, null, null, DateTime.MaxValue, null);
            this.sKey = sKey;
            this.nValue = nValue;
        }
        public TemplateBind(long nID, pl.Class cClass, Template cTemplate, hk.RegisteredTable cRegisteredTable, string sKey, int nValue)
        {
            this.nID = nID;
            this.cClass = cClass;
            this.cTemplate = cTemplate;
            this.cRegisteredTable = cRegisteredTable;
            this.sKey = sKey;
            this.nValue = nValue;
        }
		public TemplateBind(object nID, object nClassID, object nTemplateID, object nRegisteredTableID, object sKey, object nValue, object sName, object sFile)
			: this(nID.ToID(), nClassID.ToID(), nTemplateID.ToID(), nRegisteredTableID.ToID(), (null == sKey ? null : sKey.ToString()), nValue.ToID(), (null == sName ? null : sName.ToString()), (null == sFile ? null : sFile.ToString()))
        { }
        public TemplateBind(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["idClasses"], ahDBRow["idTemplates"], ahDBRow["idRegisteredTables"], ahDBRow["sKey"], ahDBRow["nValue"], ahDBRow["sName"], ahDBRow["sFile"])
        { }
    };
	public class ChatInOut
	{
		public long nID;
		public TimeRange cTimeRange;
		public ChatInOut()
		{
			this.nID = -1;
			this.cTimeRange = null;
		}
		public ChatInOut(long nID, TimeRange cTimeRange)
		{
			this.nID = nID;
			this.cTimeRange = cTimeRange;
		}
		public ChatInOut(long nID, int nFrameIn, int nFrameOut)
			: this(nID, new TimeRange(nFrameIn, nFrameOut))
		{ }
		public ChatInOut(Hashtable ahDBRow)
			: this(ahDBRow["id"].ToID(), ahDBRow["nFrameIn"].ToInt32(), ahDBRow["nFrameOut"].ToInt32())
		{ }
	};
    public class TemplatesSchedule
    {
        public long nID { get; set; }
        public TemplateBind cTemplateBind { get; set; }
		public DateTime _dtLast;
		public DateTime dtLast 
		{ 
			get; 
			set; 
		}
		public DateTime dtStart { get; set; }
		[System.Xml.Serialization.XmlIgnore]
		public TimeSpan tsInterval { get; set; }
		public int nIntervalInMilliseconds { get; set; } //  TimeSpan чо-то не передаётся в sl
		public DateTime dtStop { get; set; }
		public DictionaryElement[] aDictionary { get; set; }
		public TemplatesSchedule()
		{ }
        public TemplatesSchedule(long nID, TemplateBind cTemplateBind, DateTime dtLast, DateTime dtStart, TimeSpan tsInterval, DateTime dtStop, Template cTemplate)
        {
            this.nID = nID;
            this.cTemplateBind = cTemplateBind;
            this.dtLast = dtLast;
            this.dtStart = dtStart;
            this.tsInterval = tsInterval;
			this.nIntervalInMilliseconds = (int)tsInterval.TotalMilliseconds;
            this.dtStop = dtStop;
			if (null != cTemplateBind)
				this.cTemplateBind.cTemplate = cTemplate;
        }
		public TemplatesSchedule(long nID, long nTemplateBind, DateTime dtLast, DateTime dtStart, TimeSpan tsInterval, DateTime dtStop, long nTemplateID)
			: this(nID, new TemplateBind(nTemplateBind), dtLast, dtStart, tsInterval, dtStop, -1 == nTemplateID ? null : new Template(nTemplateID, null, null))
		{ }
		public TemplatesSchedule(Hashtable ahDBRow)
			: this(ahDBRow["id"].ToID(), ahDBRow["idClassAndTemplateBinds"].ToID(), ahDBRow["dtLast"].ToDT(), ahDBRow["dtStart"].ToDT(), ahDBRow["tsInterval"].ToTS(), ahDBRow["dtStop"].ToDT(), ahDBRow["idTemplates"].ToID())
        { }
		public void DictionarySet(Queue<Hashtable> ahDBRow)
		{
			List<DictionaryElement> aDict = new List<DictionaryElement>();
			while (ahDBRow.Count > 0)
				aDict.Add(new DictionaryElement(ahDBRow.Dequeue()));
			aDictionary = aDict.ToArray();
		}
    }
	public class DictionaryElement
	{
		public long nID { get; set; }
		public long nTargetID { get; set; }
		public string sKey { get; set; }
		public string sValue { get; set; }
		public long nRegisteredTablesID { get; set; }
		public DictionaryElement()
		{ }
		public DictionaryElement(long nID, long nTargetID, string sKey, string sValue, long nRegisteredTablesID)
		{
			this.nID = nID;
			this.nTargetID = nTargetID;
			this.sKey = sKey;
			this.sValue = sValue;
			this.nRegisteredTablesID = nRegisteredTablesID;
		}
		public DictionaryElement(Hashtable ahDBRow)
			: this(ahDBRow["id"].ToID(), ahDBRow["idTarget"].ToID(), ahDBRow["sKey"].ToString(), ahDBRow["sValue"].ToString(), ahDBRow["idRegisteredTables"].ToID())
		{ }
	}
	namespace plugins
	{
		static internal class x
		{
			static public Playlist PlaylistGet(this DBInteract cDBI, long nID)
			{
				return cDBI.PlaylistsGet(nID + "=id").FirstOrDefault();
			}
			static public Playlist[] PlaylistsGet(this DBInteract cDBI)
			{
				return cDBI.PlaylistsGet("");
			}
			static public Playlist[] PlaylistsGet(this DBInteract cDBI, string sWhere)
			{
				return cDBI.RowsGet("SELECT * FROM cues.`vPluginPlaylists` " + (sWhere.IsNullOrEmpty() ? "" : "WHERE " + sWhere)).Select(o => new Playlist(o)).ToArray();
			}
			static public void PlaylistStarted(this DBInteract cDBI, Playlist cPlaylist, pl.PlaylistItem cPLI)
			{
				cDBI.Perform("SELECT * FROM cues.`fPluginPlaylistSave`(" + cPlaylist.nID + ", " + cPLI.nID + ")");
			}
		}
		[Serializable]
		public class PlaylistItem
		{
			public long nID;
			public IdNamePair oStatus;
			public DateTime dtStarted;
			public mam.Asset oAsset;

			public PlaylistItem()
			{
				nID = extensions.x.ToID(null);
				dtStarted = DateTime.MaxValue;
			}
			public PlaylistItem(Hashtable ahDBRow)
				: this()
			{
				nID = ahDBRow["id"].ToID();
				dtStarted = ahDBRow["dtStarted"].ToDT();
				oStatus = new IdNamePair(ahDBRow["idStatuses"], ahDBRow["sStatusName"]);
				oAsset = new mam.Asset() { };
			}
		}
		[Serializable]
		public class Playlist
		{
			static private Dictionary<long, Playlist> _aLoadCache;
			static public Playlist Load(long nID)
			{
				if (null == _aLoadCache)
					_aLoadCache = new Dictionary<long, Playlist>();
				if (!_aLoadCache.ContainsKey(nID))
				{
					Playlist cPlaylist = DBInteract.cCache.PlaylistGet(nID);
					_aLoadCache.Add(cPlaylist.nID, cPlaylist);
				}
				return _aLoadCache[nID];
			}
			static public Playlist[] Get()
			{
				return DBInteract.cCache.PlaylistsGet();
			}

			public long nID;
			public string sName;
			public DateTime dtStart;
			public DateTime dtStop;
			public IdNamePair oStatus;
			public helpers.replica.mam.Asset[] aAssets;

			public Playlist()
			{
				nID = extensions.x.ToID(null);
				sName = "";
				dtStart = DateTime.MaxValue;
				dtStop = DateTime.MaxValue;
			}
			public Playlist(Hashtable ahDBRow)
				: this()
			{
				nID = ahDBRow["id"].ToID();
				sName = ahDBRow["sName"].ToString();
				dtStart = ahDBRow["dtStartPlanned"].ToDT();
				dtStop = ahDBRow["dtStartReal"].ToDT();
				oStatus = new IdNamePair(ahDBRow["idStatuses"], ahDBRow["sStatusName"]);
			}
		}
	}
}
namespace helpers.replica.ia
{
	public class Gateway
	{
		public class IP
		{
			public long nID;
			public System.Net.IPAddress cIP;
			public IP(string sIP)
			{
				if ("::1" == sIP)
					sIP = "127.0.0.1";
				cIP = System.Net.IPAddress.Parse(sIP);
			}
			new public string ToString()
			{
				if(null == cIP)
					return "";
				return cIP.ToString();
			}
		}
		public long nID;
		public string sName;
		public IP[] aIPs;
	}
	public class Message
	{
		public long nID;
		public string sBindID;
		public Gateway.IP cGatewayIP;
		public ushort nCount;
		public ulong nSourceNumber;
		public ulong nTargetNumber;
		public string sText;
		public byte[] aImageBytes;
		public DateTime dtRegister;
		public DateTime dtDisplay;
		public Message(long nID, string sBindID, Gateway.IP cGatewayIP, ushort nCount, ulong nSourceNumber, ulong nTargetNumber, string sText, byte[] aImageBytes, DateTime dtRegister, DateTime dtDisplay)
		{
			this.nID = nID;
			this.sBindID = sBindID;
			this.cGatewayIP = cGatewayIP;
			this.nCount = nCount;
			this.nSourceNumber = nSourceNumber;
			this.nTargetNumber = nTargetNumber;
			this.sText = sText;
			this.aImageBytes = aImageBytes;
			this.dtRegister = dtRegister;
			this.dtDisplay = dtDisplay;
		}
		public Message(object nID, object sBindID, object cGatewayIP, object nCount, object nSourceNumber, object nTargetNumber, object sText, object aImageBytes, object dtRegister, object dtDisplay)
			: this(nID.ToID(), sBindID.ToString(), (cGatewayIP is Gateway.IP?(Gateway.IP)cGatewayIP:new Gateway.IP(cGatewayIP.ToString())), nCount.ToUInt16(), nSourceNumber.ToUInt64(), nTargetNumber.ToUInt64(), sText.ToString(), (null == aImageBytes?null:(byte[])aImageBytes), dtRegister.ToDT(), dtDisplay.ToDT())
		{
		}
		public Message(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sBindID"], ahDBRow["cGatewayIP"], ahDBRow["nCount"], ahDBRow["nSource"], ahDBRow["nTarget"], ahDBRow["sText"], ahDBRow["cImage"], ahDBRow["dtRegister"], ahDBRow["dtDisplay"])
		{
		}
	}
}
namespace helpers.replica.scr
{
	public class Shift
	{
		public long nID;
		public IdNamePair cPreset;
		public DateTime dt;
		public DateTime dtStart;
		public DateTime dtStop;
		public string sSubject;

		public Shift()
		{
			nID = x.ToID(null);
			cPreset = null;
			dt = DateTime.MaxValue;
			dtStart = DateTime.MaxValue;
			dtStop = DateTime.MaxValue;
			sSubject = null;
		}
		public Shift(long nID, IdNamePair cPreset, DateTime dt, DateTime dtStart, DateTime dtStop, string sSubject)
		{
			this.nID = nID;
			this.cPreset = cPreset;
			this.dt = dt;
			this.dtStart = dtStart;
			this.dtStop = dtStop;
			this.sSubject = sSubject;
		}
		public Shift(object nID, IdNamePair cPreset, object dt, object dtStart, object dtStop, object sSubject)
			: this(nID.ToID(), cPreset, dt.ToDT(), dtStart.ToDT(), dtStop.ToDT(), sSubject.ToString())
		{
		}
		public Shift(Hashtable ahDBRow)
			: this(ahDBRow["id"], new IdNamePair(ahDBRow["idPresets"], ahDBRow["sPresetName"]), ahDBRow["dt"], ahDBRow["dtStart"], ahDBRow["dtStop"], ahDBRow["sSubject"])
		{
		}
	}
	public class Announcement
	{
		public long nID { get; set; }
		public Shift cShift { get; set; }
		public string sText { get; set; }

		public Announcement()
		{
			nID = x.ToID(null);
			cShift = null;
			sText = null;
		}
		public Announcement(long nID, Shift cShift, string sText)
		{
			this.nID = nID;
			this.cShift = cShift;
			this.sText = sText;
		}
		public Announcement(object nID, Shift cShift, object sText)
			: this(nID.ToID(), cShift, sText.ToString())
		{
		}
		public Announcement(Hashtable ahDBRow)
			: this(ahDBRow["id"], null, ahDBRow["sText"])
		{
		}
	}
	public class Plaque
	{
		public long nID;
		public IdNamePair cPreset;
		public string sName;
		public string sFirstLine;
		public string sSecondLine;
		public Plaque()
		{
			nID = -1;
			cPreset = null;
			sName = null;
			sFirstLine = null;
			sSecondLine = null;
		}
		public Plaque(Hashtable ahRow)
			: this()
		{
			nID = ahRow["id"].ToID();
			cPreset = new IdNamePair(ahRow["idPresets"], ahRow["sPresetName"]);
			sName = (string)ahRow["sName"];
			sFirstLine = (string)ahRow["sFirstLine"];
			sSecondLine = (string)ahRow["sSecondLine"];
		}
	}
	public class StoragesMappings
	{
		public string sName;
		public long nID;
		public string sLocalPath;
		public StoragesMappings(Hashtable ahDBRow)
		{
			sName = (string)ahDBRow["sName"];
			nID = ahDBRow["id"].ToID();
			sLocalPath = (string)ahDBRow["sLocalPath"];
		}
		private StoragesMappings()
		{
			nID = -1;
			sName = "";
			sLocalPath = "";
		}
	}
}
namespace helpers.replica.adm
{
	public class QueuedCommand //UNDONE нужно прикрутить получение параметров
	{
		public long nID;
		public DateTime dt;
		public IdNamePair cCommand;
		public IdNamePair cCommandStatus;
		public IdNamePair cUser;

		public QueuedCommand()
		{
			nID = x.ToID(null);
			dt = DateTime.MaxValue;
			cCommand = null;
			cCommandStatus = null;
			cUser = null;
		}
		public QueuedCommand(long nID, DateTime dt, IdNamePair cCommand, IdNamePair cCommandStatus, IdNamePair cUser)
		{
			this.nID = nID;
			this.dt = dt;
			this.cCommand = cCommand;
			this.cCommandStatus = cCommandStatus;
			this.cUser = cUser;
		}
		public QueuedCommand(object nID, object dt, IdNamePair cCommand, IdNamePair cCommandStatus, IdNamePair cUser)
			: this(nID.ToID(), dt.ToDT(), cCommand, cCommandStatus, cUser)
		{
		}
		public QueuedCommand(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["dt"], new IdNamePair(ahDBRow["idCommands"], ahDBRow["sCommandName"]), new IdNamePair(ahDBRow["idCommandStatuses"], ahDBRow["sCommandStatus"]), new IdNamePair(ahDBRow["idUsers"], ahDBRow["sUsername"]))
		{
		}
	}
}
