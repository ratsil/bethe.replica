using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Data.SqlClient;
using System.Web;
using helpers;
using helpers.extensions;
using System.Linq;
using g = globalization;
using sio = System.IO;
using System.IO.Compression;


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
            if (object.ReferenceEquals(null, cRegisteredTable1) && object.ReferenceEquals(null, cRegisteredTable2))
                return true;
            if (object.ReferenceEquals(null, cRegisteredTable1) || object.ReferenceEquals(null, cRegisteredTable2))
                return false;
            if (cRegisteredTable1.nID > 0 || cRegisteredTable2.nID > 0)
                return (cRegisteredTable1.nID == cRegisteredTable2.nID);
            if (cRegisteredTable1.GetHashCode() == cRegisteredTable2.GetHashCode())
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
			: this(oID.ToID(), oSchema.ToString(), oName.ToString(), oUpdated.ToDT(), oNote.ToStr())
		{ }
		public RegisteredTable(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sSchema"], ahDBRow["sName"], ahDBRow["dtUpdated"], ahDBRow["sNote"])
		{ }
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
			sHash += (null == sName ? "null" : sName) + "::";
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
            if (cStorage1.nID > 0 || cStorage2.nID > 0)
                return (cStorage1.nID == cStorage2.nID);
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
		public IdNamePair cVideoType;

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
		public Storage(long nID, string sName, string sPath, bool bEnabled, IdNamePair cType, IdNamePair cVideoType)
		{
			this.nID = nID;
			this.sName = sName;
			this.sPath = sPath;
			this.bEnabled = bEnabled;
			this.cType = cType;
			this.cVideoType = cVideoType;
		}
		public Storage(object cID, object cName, object cPath, object cEnabled, object cTypeID, object cTypeName, object cVideoTypeID, object cVideoTypesName)
			: this(cID.ToID(), cName.ToString(), cPath.ToString(), cEnabled.ToBool(), new IdNamePair(cTypeID.ToID(), cTypeName.ToString()), null == cVideoTypeID ? null : new IdNamePair(cVideoTypeID.ToID(), cVideoTypesName.ToString()))
		{ }
		public Storage(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sName"], ahDBRow["sPath"], ahDBRow["bEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sTypeName"], ahDBRow["idVideoTypes"], ahDBRow["sVideoTypesName"])
		{ }
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
				public int nPG_ID;
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
				public tsr.TSRItem cTSR;

				public Advertisement()
				{ }
			}
			[Serializable]
			[XmlType("IngestProgram")]
			public class Program : Ingest
			{
				public mam.Asset cSeries;
				public mam.Asset cEpisode;
				public string sPart;

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
			public bool bCreateAsset;
            public DateTime dtSourceModification;
            public helpers.replica.pl.Class[] aClasses;

			public Ingest()
			{ }
		}
		public enum Status
		{
			Waiting = 0,
			InStock = 1,
			MovedToTape = 2,
			Deleted = 3
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
            if (cFile1.nID > 0 || cFile2.nID > 0)
                return (cFile1.nID == cFile2.nID);
            if (cFile1.GetHashCode() == cFile2.GetHashCode())
				return true;
			return false;
		}
		static public bool operator !=(File cFile1, File cFile2)
		{
			return !(cFile1 == cFile2);
		}
		#endregion

		static private Dictionary<long, File> _ahLoadCache;
		static private List<long> _aCreatedAndCached;
		static public File Load(Storage cStorage, string sFilename)
		{
			File cRetVal;
			if (null == (cRetVal = _ahLoadCache.Values.FirstOrDefault(o => cStorage == o.cStorage && sFilename == o.sFilename)))
			{
				if (null != (cRetVal = DBInteract.cCache.FileGet(cStorage, sFilename)))
					_ahLoadCache.Add(cRetVal.nID, cRetVal);
			}
			return cRetVal;
		}
		static public File Load(long nID)
		{
			if (!_ahLoadCache.ContainsKey(nID))
			{
				File cFile = DBInteract.cCache.FileGet(nID);
				if (null != cFile)
					_ahLoadCache.Add(cFile.nID, cFile);
			}
			return _ahLoadCache[nID];
		}
		static public File Create(Storage cStorage, string sFilename)
		{
			if (null != cStorage)
			{
				File cFile = DBInteract.cCache.FileAdd(cStorage.nID, sFilename);
				_ahLoadCache.Add(cFile.nID, cFile);
				_aCreatedAndCached.Add(cFile.nID);
				return cFile;
			}
			return null;
		}
		static public void CreateRollBack(long nID)
		{
			if (_aCreatedAndCached.Contains(nID))
			{
				_ahLoadCache.Remove(nID);
				_aCreatedAndCached.Remove(nID);
			}
		}
		public void StatusSet(Status eStatus)
		{
			this.eStatus = eStatus;
			DBInteract.cCache.FileStatusSet(this);
		}
		public void LastFileEventUpdate()
		{
			this.dtLastEvent = DateTime.Now;
			DBInteract.cCache.LastFileEventUpdate(this);
		}
        public void FileModificationUpdate(DateTime dtModification)
        {
            this.dtModification = dtModification;
            DBInteract.cCache.FileModificationUpdate(this);
        }

        public void FormatSet(byte nFPS, ushort nWidth, ushort nHeight, int nAspect_dividend, int nAspect_divider, long nFramesQty)
		{
			DBInteract.cCache.FileFormatSet(this, nFPS, nWidth, nHeight, nAspect_dividend, nAspect_divider, nFramesQty);
		}
		public void Rename(string sNewName)
		{
			DBInteract.cCache.FileRename(this, sNewName);
			sFilename = sNewName;
        }

		public long nID;
		public string sFilename;
		public Storage cStorage;
		public DateTime dtLastEvent;
        public DateTime dtModification;
        public Error eError;
		public Status eStatus;
		public int nAge;

		// additional info
		public int? nFPS;
		public string sSourceFile;
		public string sSong;
		public string sSeries;
		public string sEpisode;
		public string sCustomValue;
		public int? nAspectRatioDivd;
		public int? nAspectRatioDivr;
		public long? nPGID;
		public int? nWidth;
		public int? nHeight;
		public int? nFramesQTY;
		public bool? bToDelete;

		public string sFile
		{
			get
			{
				return cStorage.sPath + sFilename;
			}
		}
		static File()
		{
			_ahLoadCache = new Dictionary<long, File>();
			_aCreatedAndCached = new List<long>();
		}
		public File()
		{
			nID = extensions.x.ToID(null);
			sFilename = null;
			cStorage = null;
			dtLastEvent = x.ToDT(null);
			eError = Error.no;
		}
		public File(long nID, string sFilename, Storage cStorage, DateTime dtLastEvent, Error eError, Status eStatus, int nAge)
		{
			this.nID = nID;
			this.sFilename = sFilename;
			this.cStorage = cStorage;
			this.dtLastEvent = dtLastEvent;
			this.eError = eError;
			this.eStatus = eStatus;
			this.nAge = nAge;
        }
		public File(object oID, object oFilename, Storage cStorage, object oLastEvent, object oError, object oStatus, object oAge)
			: this(oID.ToID(), oFilename.ToString(), cStorage, oLastEvent.ToDT(), (null == oError ? Error.no : (Error)Enum.Parse(typeof(Error), oError.ToString())), (null == oStatus ? Status.Waiting : (Status)Enum.Parse(typeof(Status), oStatus.ToString())), (null == oAge ? int.MinValue : oAge.ToInt()))
		{ }
		public File(object oFileID, object oFilename, object oStorageID, object oStorageName, object oStoragePath, object oStorageEnabled, object oStorageTypeID, object oStorageTypeName, object oLastEvent, object oError, object oStatus, object oAge)
			: this(oFileID.ToID(), oFilename.ToString(), new Storage(oStorageID, oStorageName, oStoragePath, oStorageEnabled, oStorageTypeID, oStorageTypeName, null, null), oLastEvent, oError, oStatus, oAge)
		{ }
		public File(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["sFilename"], ahDBRow["idStorages"], ahDBRow["sStorageName"], ahDBRow["sPath"], ahDBRow["bStorageEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sStorageTypeName"], ahDBRow["dtLastFileEvent"], ahDBRow["eError"], ahDBRow["nStatus"], ahDBRow["nAge"])
		{ }
		public void FileAdditionalInfoSet(Queue<Hashtable> aRows)
		{
			Hashtable ahRow;
			while (aRows.Count > 0)
			{
				ahRow = aRows.Dequeue();
				if (ahRow["idFiles"].ToLong() != nID)
					continue;
				switch (ahRow["sKey"].ToString())
				{
					case "fps":
						nFPS = ahRow["nValue"] == null ? (int?)null : ahRow["nValue"].ToInt();
						break;
					case "source":
						sSourceFile = ahRow["oValue"] == null ? null : ahRow["oValue"].ToString();
						break;
					case "song":
						sSong = ahRow["oValue"] == null ? null : ahRow["oValue"].ToString();
						break;
					case "series":
						sSeries = ahRow["sName"] == null ? null : ahRow["sName"].ToString();
						break;
					case "episode":
						sEpisode = ahRow["sName"] == null ? null : ahRow["sName"].ToString();
						break;
					case "id":
						sCustomValue = ahRow["nValue"] == null ? null : ahRow["nValue"].ToString();
						sCustomValue = ahRow["oValue"]==null? sCustomValue : ahRow["oValue"].ToString();
						break;
					case "aspect_divd":
						nAspectRatioDivd = ahRow["nValue"] == null ? (int?)null : ahRow["nValue"].ToInt();
						break;
					case "aspect_divr":
						nAspectRatioDivr = ahRow["nValue"] == null ? (int?)null : ahRow["nValue"].ToInt();
						break;
					case "pg_id":
						nPGID = ahRow["nValue"] == null ? (long?)null : ahRow["nValue"].ToLong();
						break;
					case "w":
						nWidth = ahRow["nValue"] == null ? (int?)null : ahRow["nValue"].ToInt();
						break;
					case "h":
						nHeight = ahRow["nValue"] == null ? (int?)null : ahRow["nValue"].ToInt();
						break;
					case "fr_qty":
						nFramesQTY = ahRow["nValue"] == null ? (int?)null : ahRow["nValue"].ToInt();
						break;
					case "to_delete":
						bToDelete = ahRow["nValue"] == null ? (bool?)null : ahRow["nValue"].ToBool();
						break;
                    case "modification":
                        dtModification = ahRow["dt"] == null ? DateTime.MinValue : ahRow["dt"].ToDT();
                        break;
                    default:
						break;
				}
			}
		}
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
				(new Logger()).WriteNotice("error info: [cDBI " + (cDBI == null ? "is NULL" : "not NULL") + "][name=" + sName + "]");
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}
		static public string MacroExecute(this DBInteract cDBI, Macro cMacro)
		{
			if ("value" == cMacro.cType.sName) //EMERGENCY:l это зачем такое? мне что-то подсказывает, что подобное делается иначе и через рот... а не так...
				return cMacro.sValue;
			else
				return cDBI.ValueGet(cMacro.sValue).FromDB();
		}
		static public Person Load(this Person cObj, long nID)
		{
			return DBInteract.cCache.PersonGet(nID);
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
	[Serializable]
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
			: this(oID.ToID(), (null == oSong ? null : oSong.ToString().FromDB()), (null == oArtist ? null : oArtist.ToString().FromDB()), (null == oAlbum ? null : oAlbum.ToString().FromDB()), (null == oYear ? -1 : oYear.ToInt32()), (null == oPossessor ? null : oPossessor.ToString().FromDB()))
		{ }
	};
	[Serializable]
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
			: this(nID, sName, new IdNamePair(nTypeID, sTypeName))
		{ }
		public Video(object nID, object sName, object nTypeID, object sTypeName)
			: this(nID.ToID(), sName.ToString().FromDB(), nTypeID.ToID(), sTypeName.ToString())
		{ }
	};
	public class Person
	{
		public void Save()
		{
			DBInteract.cCache.PersonSave(this);
		}
		public long nID;
		public string sName;
		public IdNamePair cType;
		public Person()
			: this(-1, null)
		{ }
		public Person(long nID, string sName, IdNamePair cType)
		{
			this.nID = nID;
			this.sName = sName;
			this.cType = cType;
		}
		public Person(long nID, string sName, string sTypeName, long nTypeID)
			: this(nID, sName, new IdNamePair(nTypeID, sTypeName))
		{ }
		public Person(long nID, string sName)
			: this(nID, sName, null)
		{ }
		public Person(object nID, object sName)
			: this(nID.ToID(), sName.ToString(), null)
		{ }
		public Person(Hashtable ahDBRow)
			: this(ahDBRow["id"].ToID(), ahDBRow["sName"].ToString().FromDB(), ahDBRow["sPersonTypeName"].ToString(), ahDBRow["idPersonTypes"].ToID())
		{ }
		public override string ToString()
		{
			return sName;
		}
        public static bool ArraysHaveTheSameSetOfElements(Person[] aP1, Person[] aP2)
        {
            if (aP1.IsNullOrEmpty() && aP2.IsNullOrEmpty())
                return true;
            if (aP1.IsNullOrEmpty() || aP2.IsNullOrEmpty())
                return false;
            if (aP1.Length != aP2.Length)
                return false;

            long[] aIDs2 = aP2.Where(o => o != null).Select(o => o.nID).ToArray();
            foreach (Person cP in aP1)
            {
                if (cP == null)
                    continue;
                if (!aIDs2.Contains(cP.nID))
                    return false;
            }
            return true;
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
			: this(new IdNamePair(idStart, sStart), new IdNamePair(idStop, sStop))
		{ }
		public SoundLevels(object idStart, object sStart, object idStop, object sStop)
			: this(new IdNamePair(idStart, sStart), new IdNamePair(idStop, sStop))
		{ }
	};
	[Serializable]
	public struct CustomValue //TODO переделать в класс
	{
		public long nID;
		public string sName;
		public string sValue;
		public CustomValue(long nID, string sName, string sValue)
		{
			this.nID = nID;
			this.sName = null == sName ? null : sName.FromDB();
			this.sValue = null == sValue ? null : sValue.FromDB();
		}
		public CustomValue(string sName, string sValue)
			: this(-1, sName, sValue)
		{ }
		public CustomValue(Hashtable ahRow)
			: this(ahRow["sName"].ToString(), ahRow["sValue"].ToString())
		{
		}
	}
	[Serializable]
	public class Asset
	{
		[Serializable]
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
		public media.File cFile;
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
		//public pl.Class cClass;
        public pl.Class[] aClasses;
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
				sName = ahDBRow["sName"].ToString().FromDB();
				if (null != ahDBRow["idVideos"])
					stVideo = new Video(ahDBRow["idVideos"], (null == ahDBRow["sVideoName"] ? "" : ahDBRow["sVideoName"]), ahDBRow["idVideoTypes"], ahDBRow["sVideoTypeName"]);
				if (null != ahDBRow["idFiles"])
					cFile = new media.File(ahDBRow["idFiles"], ahDBRow["sFilename"], ahDBRow["idStorages"], ahDBRow["sStorageName"], ahDBRow["sPath"], ahDBRow["bStorageEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sStorageTypeName"], ahDBRow["dtLastFileEvent"], ahDBRow["eFileError"], ahDBRow["nStatus"], ahDBRow["nAge"]);
                //if (null != ahDBRow["idClasses"])
                //    cClass = new pl.Class(ahDBRow["idClasses"], ahDBRow["sClassName"]);
                if (null != ahDBRow["aClasses"])
                    aClasses = pl.Class.GetArray(ahDBRow["aClasses"]);
                else if (null != ahDBRow["idClasses"])
                    aClasses = new pl.Class[1] { new pl.Class(ahDBRow["idClasses"], ahDBRow["sClassName"]) };

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
		public void FileSet()
		{
			DBInteract.cCache.AssetFileSave(this);
        }
		public static Asset[] GetAssets(Queue<Hashtable> ahAssets)
		{
			if (ahAssets == null || ahAssets.Count <= 0)
				return new Asset[0];
			return ahAssets.Select(o => new Asset(o)).ToArray();  // бывает очень неудобно одновременно с запросом это делать... и читабельнее ))
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
					try
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
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
						return null;
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
		public IdNamePair cSex;
		public bool bSmoking;
		public SoundLevels stSoundLevels;
		public void PersonsLoad()
		{
			aPersons = DBInteract.cCache.ArtistsLoad(nID);
		}
		public void SexLoad()
		{
			cSex = DBInteract.cCache.SexGet(nID);
        }
		public Cues stCues;
		public Clip()
			: base()
		{
			base.stVideo.cType = cVideoType;
			bSmoking = false;
		}
		public Clip(long nID, IdNamePair cVideoType)
			: base()
		{
			base.nID = nID;
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
			if (null != ahDBRow["idSex"])
				cSex = new IdNamePair(ahDBRow["idSex"], ahDBRow["sSexName"]);
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
			: base(ahDBRow)
		{ }
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
			: base(ahDBRow)
		{ }
	}
	[Serializable]
	public class Program : Asset
	{
		public class ClipsFragment
		{
			public Clip cClip;
			public long nFramesQty;
			public ClipsFragment()
			{
				cClip = null;
				nFramesQty = -1;
			}
		} 
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

		public ClipsFragment[] aClipsFragments;
		public void ClipsFragmentsLoad()
		{
			aClipsFragments = DBInteract.cCache.ProgramRAOInfoGet(this).ToArray();
		}
		public Program()
			: base()
		{ }
		public Program(Hashtable ahDBRow)
			: base(ahDBRow)
		{
		}
	}
}
namespace helpers.replica.pl
{
	public static class x
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
        static public string ToIdsArrayForDB(this Class[] aClasses)
        {
            return "array[" + aClasses.Select(o => o.nID).ToEnumerationString(true) + "]::integer[]";
        }
        static public string ToNamesArrayForDB(this Class[] aClasses)
        {
            return "array[" + aClasses.Select(o => o.sName).ToEnumerationString("'", true) + "]::character varying[]";
        }
        static public string ToStr(this Class[] aClasses)
        {
            return "(" + aClasses.Select(o => o.ToString()).ToEnumerationString(true) + ")";
        }
        static public bool ContainsPartOfName(this Class[] aClasses, string sFragment)
        {
            foreach(Class cClass in aClasses)
            {
                if (cClass.sName.ToLower().Contains(sFragment.ToLower()))
                    return true;
            }
            return false;
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
		//public Class cClass;
        public Class[] aClasses;
        //public bool bIsAdv;  мутный атрибут какой-то и вроде не юзается нигде...
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
		public DateTime dtStartRealPlanned
		{
			get
			{
				return DateTime.MaxValue > dtStartReal ? dtStartReal : dtStartPlanned;
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
			sName = ahDBRow["sName"].ToString().FromDB();
			nFramesQty = ahDBRow["nFramesQty"].ToLong();
			nFrameStart = ahDBRow["nFrameStart"].ToLong();
			nFrameStop = ahDBRow["nFrameStop"].ToLong();
			if (null != ahDBRow["nFrameCurrent"])
                nFrameCurrent = ahDBRow["nFrameCurrent"].ToLong();
            //if (null != ahDBRow["idClasses"])
            //    cClass = new Class(ahDBRow["idClasses"].ToID(), ahDBRow["sClassName"].ToString());
            if (null != ahDBRow["aClasses"])
                aClasses = pl.Class.GetArray(ahDBRow["aClasses"]);

            if (aClasses.IsNullOrEmpty())
                aClasses = new Class[1] { new Class(ahDBRow["idClasses"].ToID(), ahDBRow["sClassName"].ToString()) };

            //bIsAdv = cClass.sName.Contains("advertisement");

            if (null == ahDBRow["idStorages"])
				cFile = new helpers.replica.media.File(ahDBRow["idFiles"], ahDBRow["sFilename"], null, ahDBRow["dtLastFileEvent"], ahDBRow["eFileError"], ahDBRow["nStatus"], ahDBRow["nAge"]);
			else
				cFile = new helpers.replica.media.File(ahDBRow["idFiles"], ahDBRow["sFilename"], ahDBRow["idStorages"], ahDBRow["sStorageName"], ahDBRow["sPath"], ahDBRow["bStorageEnabled"], ahDBRow["idStorageTypes"], ahDBRow["sStorageTypeName"], ahDBRow["dtLastFileEvent"], ahDBRow["eFileError"], ahDBRow["nStatus"], ahDBRow["nAge"]);

			cStatus = new IdNamePair(ahDBRow["idStatuses"], ahDBRow["sStatusName"]);

			dtStartPlanned = ahDBRow["dtStartPlanned"].ToDT();
			dtStartHard = ahDBRow["dtStartHard"].ToDT();
			dtStartSoft = ahDBRow["dtStartSoft"].ToDT();
			dtStartQueued = ahDBRow["dtStartQueued"].ToDT();
			dtStartReal = ahDBRow["dtStartReal"].ToDT();
			dtStopReal = ahDBRow["dtStopReal"].ToDT();
			dtTimingsUpdate = ahDBRow["dtTimingsUpdate"].ToDT();

			sNote = (null == ahDBRow["sNote"] ? "" : ahDBRow["sNote"].ToString().FromDB());
			bPlug = ahDBRow["bPlug"].ToBool();

			if (null != ahDBRow["idAssets"])
			{
				cAsset = new mam.Asset();
				cAsset.nID = ahDBRow["idAssets"].ToID();
				if (null!= ahDBRow["idVideoTypes"] && null != ahDBRow["sVideoTypeName"])
				{
					cAsset.stVideo.cType = new IdNamePair(ahDBRow["idVideoTypes"].ToLong(), ahDBRow["sVideoTypeName"].ToString());
				}
			}
		}
        public PlaylistItem(PlaylistItem cPLIOld, long nIDNew) // makes a copy
            : this()
        {
            nID = nIDNew;
            sName = cPLIOld.sName;
            nFramesQty = cPLIOld.nFramesQty;
            nFrameStart = cPLIOld.nFrameStart;
            nFrameStop = cPLIOld.nFrameStop;
            nFrameCurrent = cPLIOld.nFrameCurrent;

            aClasses = cPLIOld.aClasses == null ? null : cPLIOld.aClasses.ToArray();
            if (null != cPLIOld.cFile)
                if (null == cPLIOld.cFile.cStorage)
                    cFile = new helpers.replica.media.File(cPLIOld.cFile.nID, cPLIOld.cFile.sFilename, null, cPLIOld.cFile.dtLastEvent, cPLIOld.cFile.eError, cPLIOld.cFile.eStatus, cPLIOld.cFile.nAge);
                else
                    cFile = new helpers.replica.media.File(cPLIOld.cFile.nID, cPLIOld.cFile.sFilename, cPLIOld.cFile.cStorage.nID, cPLIOld.cFile.cStorage.sName, cPLIOld.cFile.cStorage.sPath, cPLIOld.cFile.cStorage.bEnabled, cPLIOld.cFile.cStorage.cType?.nID, cPLIOld.cFile.cStorage.cType?.sName, cPLIOld.cFile.dtLastEvent, cPLIOld.cFile.eError, cPLIOld.cFile.eStatus, cPLIOld.cFile.nAge);
            cStatus = cPLIOld.cStatus == null ? null : new IdNamePair(cPLIOld.cStatus.nID, cPLIOld.cStatus.sName);

            dtStartPlanned = cPLIOld.dtStartPlanned;
            dtStartHard = cPLIOld.dtStartHard;
            dtStartSoft = cPLIOld.dtStartSoft;
            dtStartQueued = cPLIOld.dtStartQueued;
            dtStartReal = cPLIOld.dtStartReal;
            dtStopReal = cPLIOld.dtStopReal;
            dtTimingsUpdate = cPLIOld.dtTimingsUpdate;

            sNote = cPLIOld.sNote;
            bPlug = cPLIOld.bPlug;

            if (null != cPLIOld.cAsset)
            {
                cAsset = new mam.Asset();
                cAsset.nID = cPLIOld.cAsset.nID;
                cAsset.sName = cPLIOld.cAsset.sName;
                cAsset.stVideo = cPLIOld.cAsset.stVideo;
                if (null != cPLIOld.cAsset.stVideo.cType)
                {
                    cAsset.stVideo.cType = new IdNamePair(cPLIOld.cAsset.stVideo.cType.nID, cPLIOld.cAsset.stVideo.cType.sName);
                }
            }
        }

        override public string ToString()
		{
			string sRetVal = "id:" + nID + ";";
            sRetVal += "status:" + (null == cStatus || null == cStatus.sName ? "NULL" : cStatus.sName) + ";";
            sRetVal += "file:" + (null == cFile ? "NULL" : null == cFile.sFile ? "NULL2" : cFile.sFile) + ";";
            sRetVal += "class:" + (null == aClasses ? "NULL" : "[" + aClasses.ToStr() + "]") + ";";

            if (DateTime.MaxValue > dtStartReal)
				sRetVal += "sr:" + dtStartReal.ToStr();
			if (DateTime.MaxValue > dtStopReal)
				sRetVal += ":" + dtStopReal.ToStr();
			sRetVal += ";";

			if (DateTime.MaxValue > dtStartQueued)
				sRetVal += "sque:" + dtStartQueued.ToStr() + ";";
			sRetVal += "plan:" + dtStartPlanned.ToStr() + " <-> " + dtStopPlanned.ToStr() + ";";
			if (DateTime.MaxValue > dtStartHard)
				sRetVal += "shar:" + dtStartHard.ToStr() + ";";
			else if (DateTime.MaxValue > dtStartSoft)
				sRetVal += "ssof:" + dtStartSoft.ToStr() + ";";
			sRetVal += "fqty:" + nFramesQty + ";";
			sRetVal += "in:" + nFrameStart + ";";
			sRetVal += "out:" + nFrameStop + ";";
			sRetVal += "dur:" + nDuration + ";";
			sRetVal += "frcur:" + nFrameCurrent + ";";
			return sRetVal;
		}
        public string ToStringShort()
        {
            string sRetVal = "[id=" + nID + "]";
            sRetVal += "[file=" + (null == cFile ? "NULL" : null == cFile.sFile ? "NULL2" : cFile.sFile) + "]";
            sRetVal += "[plan=" + dtStartPlanned.ToStr() + " <-> " + dtStopPlanned.ToStr() + "]";
            if (DateTime.MaxValue > dtStartHard)
                sRetVal += "[shar=" + dtStartHard.ToStr() + "]";
            else if (DateTime.MaxValue > dtStartSoft)
                sRetVal += "[ssof=" + dtStartSoft.ToStr() + "]";
            sRetVal += "[fqty=" + nFramesQty + "]";
            return sRetVal;
        }
    }
    [Serializable]
	public class Class: IdNamePair
    {

        //static private Dictionary<long, Class> _aLoadCache;
        //static public Class Load(string sName)
        //{
        //	if (null == _aLoadCache)
        //		_aLoadCache = new Dictionary<long, Class>();
        //	{
        //		foreach (Class cClass in _aLoadCache.Values)
        //			if (sName == cClass.sName)
        //				return cClass;
        //	}
        //	{
        //		Class cClass;
        //		cClass = DBInteract.cCache.ClassGet(sName);
        //		_aLoadCache.Add(cClass.nID, cClass);
        //		return cClass;
        //	}
        //}
        //static public Class Load(long nID)
        //{
        //	if (null == _aLoadCache)
        //		_aLoadCache = new Dictionary<long, Class>();
        //	if (!_aLoadCache.ContainsKey(nID))
        //	{
        //		Class cClass = DBInteract.cCache.ClassGet(nID);
        //		_aLoadCache.Add(cClass.nID, cClass);
        //	}
        //	return _aLoadCache[nID];
        //}

        //public bool bResolved;
        //[System.Xml.Serialization.XmlIgnore]
        //public Class cTestator;
        //[System.Xml.Serialization.XmlIgnore]
        //public Dictionary<long, Class> aHeritors;

        //      public Class()
        //          :base()
        //{
        //	//bResolved = false;
        //	//cTestator = null;
        //	//aHeritors = null;
        //}
        private Class()
            : base() { }
        public Class(long nID, string sName)
            : base(nID, sName) { }
        public Class(object oID, object oName)
            : base(oID, oName) { }
        public Class(Hashtable aValues)
            : base(aValues) { }
        new public static Class[] GetArray(object oArray)
        {
            IdNamePair[] aNPs = IdNamePair.GetArray(oArray);
            return (aNPs.IsNullOrEmpty() ? null : aNPs.Select(o => new pl.Class(o.nID, o.sName)).ToArray());
        }
        public static bool ArraysHaveTheSameSetOfElements(Class[] aP1, Class[] aP2)
        {
            if (aP1.IsNullOrEmpty() && aP2.IsNullOrEmpty())
                return true;
            if (aP1.IsNullOrEmpty() || aP2.IsNullOrEmpty())
                return false;
            if (aP1.Length != aP2.Length)
                return false;

            long[] aIDs2 = aP2.Where(o => o != null).Select(o => o.nID).ToArray();
            foreach (Class cP in aP1)
            {
                if (cP == null)
                    continue;
                if (!aIDs2.Contains(cP.nID))
                    return false;
            }
            return true;
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
	public class VIPlaylist
	{
		public class Block
		{
			public class Type
			{
				public string sName;
				public string sCover;
				public static Dictionary<string, string> ahCover_Class;
				static public bool operator ==(Type cLeft, Type cRight)
				{
                    if (object.ReferenceEquals(null, cLeft) && object.ReferenceEquals(null, cRight))    //привидение к object нужно, чтобы не было рекурсии
                        return true;
                    if (object.ReferenceEquals(null, cLeft) || object.ReferenceEquals(null, cRight))
                        return false;
                    if (cLeft.sName == cRight.sName && cLeft.sCover == cRight.sCover)
                        return true;
                    return false;
				}
				static public bool operator !=(Type cLeft, Type cRight)
				{
					return !(cLeft == cRight);
				}
				static Type()
				{
					ahCover_Class = new Dictionary<string, string>();
					ahCover_Class.Add("сеть", "advertisement_with_logo");
					ahCover_Class.Add("москва", "advertisement_without_logo");
					ahCover_Class.Add("спонсорство", "advertisement_with_logo");
				}
				public Type(string sType, string sCover)
				{
					this.sName = sType.Trim(' ', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0').ToLower();
					this.sCover = sCover.ToLower();
				}
			}

			public TimeSpan tsStart;
			public Type cType;
			public Queue<mam.Asset> aqAssets;

			public Block(TimeSpan tsStart, Type cType)
			{
				this.tsStart = tsStart;
				this.cType = cType;
				aqAssets = new Queue<mam.Asset>();
			}
		}

		private List<Block> _aBlocks;
		private Block _cBlockLast;
		public DateTime dtDate;
		public string sChannel;

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
		public void BlockLastAssetAdd(mam.Asset cAsset, Dictionary<string, Class> ahClasses)
		{
			if (null == _cBlockLast)
				throw new Exception("can't find last block");
			if (cAsset.aClasses.IsNullOrEmpty())
                cAsset.aClasses = new Class[1] { ahClasses[Block.Type.ahCover_Class[_cBlockLast.cType.sCover]] };
            _cBlockLast.aqAssets.Enqueue(cAsset);
		}
		public mam.Asset BlockAssetDequeue(TimeSpan tsStart, string sType, string sCover)
		{
			Block cBlock = BlockGet(tsStart, sType, sCover);
			if (null == cBlock)
				throw new Exception("specified block doesn't exist [" + tsStart.ToShort() + ":" + sType + ":" + sCover + "]");
			if (0 < cBlock.aqAssets.Count)
			{
				return cBlock.aqAssets.Dequeue();
			}
			return null;
		}
		public ILookup<TimeSpan, mam.Asset[]> AssetsUnusedGet()
		{
			return _aBlocks.Where(o => o.aqAssets.Count > 0).ToLookup(k => k.tsStart, v => v.aqAssets.ToArray());
		}
		public List<Block> ClosestBlocksGet(DateTime dtTarget, int nMinutesAverage)
		{
            int nDays = dtTarget.Subtract(dtDate).Days; // not tested yet  (case when day is from 6:00 to 6:00)
			TimeSpan tsTarget = new TimeSpan(nDays, dtTarget.Hour, dtTarget.Minute, dtTarget.Second);
			List<Block> aRetVal=new List<Block>();
			foreach (Block cB in _aBlocks)
			{
				if (Math.Abs(tsTarget.Subtract(cB.tsStart).TotalMinutes) < nMinutesAverage)
					aRetVal.Add(cB);
			}
			return aRetVal;
		}
		public List<string> CheckFiles()
		{
			List<string> aRetVal = new List<string>();
			foreach (Block cB in _aBlocks)
			{
				foreach (mam.Asset cAss in cB.aqAssets)
				{
					if (!System.IO.File.Exists(cAss.cFile.sFile))
						aRetVal.Add(cAss.cFile.sFile);
				}
			}
			return aRetVal;
		}
	}
	public class Excel
	{
		static public Queue<List<string>> GetValuesFromExcel(string sFile)
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
	}
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
	[Serializable]
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
	[Serializable]
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
		public TemplateBind(long nID, long nClassID, string sClassName, long nTemplateID, long nRegisteredTableID, string sKey, long nValue, string sTemplateName, string sTemplateFile)
		{
			this.nID = nID;
			this.cClass = new pl.Class(nClassID, sClassName);
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
		public TemplateBind(object nID, object nClassID, object sClassName, object nTemplateID, object nRegisteredTableID, object sKey, object nValue, object sTemplateName, object sFile)
			: this(nID.ToID(), nClassID.ToID(), (null == sClassName ? null : sClassName.ToString()), nTemplateID.ToID(), nRegisteredTableID.ToID(), (null == sKey ? null : sKey.ToString()), nValue.ToID(), (null == sTemplateName ? null : sTemplateName.ToString()), (null == sFile ? null : sFile.ToString()))
		{ }
		public TemplateBind(Hashtable ahDBRow)
			: this(ahDBRow["id"], ahDBRow["idClasses"], ahDBRow["sClassName"], ahDBRow["idTemplates"], ahDBRow["idRegisteredTables"], ahDBRow["sKey"], ahDBRow["nValue"], ahDBRow["sName"], ahDBRow["sFile"])
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
			{  //SELECT "oPlaylist"::xml as "oPlaylist" from cues."vPluginPlaylists" where ("oPlaylist").id = 152
				return cDBI.PlaylistsGet("(`oPlaylist`).id=" + nID).FirstOrDefault();
			}
			static public Playlist[] PlaylistsGet(this DBInteract cDBI)
			{
				return cDBI.PlaylistsGet("");
			}
			static public Playlist[] PlaylistsGet(this DBInteract cDBI, string sWhere)
			{
				return cDBI.RowsGet("SELECT `oPlaylist`::xml as `oPlaylist` from cues.`vPluginPlaylists` " + (sWhere.IsNullOrEmpty() ? "" : "WHERE " + sWhere)).Select(o => Playlist.Deserialize(o["oPlaylist"].ToStr()).OrderItemsByID()).ToArray();
			}
			static public Playlist PlaylistSave(this DBInteract cDBI, Playlist oPlaylist)
			{
                Queue<Hashtable> ahRow = cDBI.RowsGet("SELECT ROW(id, `sName`, `dtStart`, `dtStop`, `aItems`)::cues.tpluginplaylist::xml AS `oPlaylist` FROM cues.`fPluginPlaylistSave`(:oPlaylist::xml)", new Map() { { "oPlaylist", Playlist.Serialize(oPlaylist) } }); //`oPlaylist`::xml as `oPlaylist`
				return ahRow.Select(o => Playlist.Deserialize(o["oPlaylist"].ToStr())).ToArray()[0];
			}
			static public void PlaylistSaveOnly(this DBInteract cDBI, Playlist oPlaylist)
			{
				PlaylistItem[] aTMP = oPlaylist.aItems;
				oPlaylist.aItems = null;
				cDBI.Perform("SELECT * FROM cues.`fPluginPlaylistSave`(:oPlaylist::xml)", new Map() { { "oPlaylist", Playlist.Serialize(oPlaylist) } });
				//cDBI.Perform("SELECT * FROM cues.`fPluginPlaylistOnlySave`(:oPlaylist::xml)", new Map() { { "oPlaylist", Playlist.Serialize(oPlaylist) } });
				oPlaylist.aItems = aTMP;
			}
			static public void PlaylistItemSave(this DBInteract cDBI, PlaylistItem oPLI)
			{
				cDBI.Perform("SELECT * FROM cues.`fPluginPlaylistItemSave`(:oPLI::xml)", new Map() { { "oPLI", PlaylistItem.Serialize(oPLI) } });
			}
			static public void PlaylistDelete(this DBInteract cDBI, Playlist oPL)
			{
				cDBI.Perform("SELECT * FROM cues.`fPluginPlaylistDelete`(:oPL::xml)", new Map() { { "oPL", Playlist.Serialize(oPL) } });
			}
			static public void PlaylistStart(this DBInteract cDBI, Playlist oPL)
			{
				if (0 < adm.QueuedCommand.Load("`sCommandName` = 'cues_plugin_playlist_start' AND ('waiting'=`sCommandStatus` OR 'proccessing'=`sCommandStatus`)", null, null).Length)
					throw new Exception("One uncompleted command 'cues_plugin_playlist_start' is already there");
                adm.QueuedCommand cQC = new adm.QueuedCommand("cues_plugin_playlist_start");
				cQC.aParameters =new adm.QueuedCommand.Parameter[1] { new adm.QueuedCommand.Parameter("idPL", oPL.nID.ToString()) };
				cQC.Save();
			}
		}
		[Serializable]
		//[XmlRoot("PlaylistItem", Namespace = "helpers.replica.cues.plugins")]
		[XmlType("PluginPlaylistItem")]
		public class PlaylistItem
		{
			static internal PlaylistItem Deserialize(string sXML)
			{
				return (PlaylistItem)(new XmlSerializer(typeof(PlaylistItem), new XmlRootAttribute() { ElementName = "oItem", IsNullable = true })).Deserialize(new System.IO.StringReader("<oItem>" + sXML + "</oItem>"));
			}
			static internal string Serialize(PlaylistItem oPLI)
			{
				StringWriter oStringWriter = new StringWriter();
				(new XmlSerializer(typeof(PlaylistItem), new XmlRootAttribute() { ElementName = "oItem", IsNullable = true })).Serialize(oStringWriter, oPLI);
				return oStringWriter.ToString();
			}
			public long nID;
			public IdNamePair oStatus;
			public DateTime dtStarted;
			public mam.Asset oAsset;
			public long nFramesQty;

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

			public void Save(DBInteract cDBI)  // иногда не пашет выцепление dbi из threads
			{
				cDBI.PlaylistItemSave(this);
			}
            public void Save()
			{
				Save(DBInteract.cCache);
			}
		}
		[Serializable]
		//[XmlRoot("Playlist", Namespace = "helpers.replica.cues.plugins")]
		public class Playlist
		{
			static private Dictionary<long, Playlist> _aLoadCache;

			static public Playlist Load(DBInteract cDBI, long nID)  // cache почему-то из плагина не видит часто DBInteract !! 
			{
				if (null == _aLoadCache)
					_aLoadCache = new Dictionary<long, Playlist>();
				if (!_aLoadCache.ContainsKey(nID))
				{
					Playlist cPlaylist = cDBI.PlaylistGet(nID);
					_aLoadCache.Add(cPlaylist.nID, cPlaylist);
				}
				return _aLoadCache[nID];
			}
            static public Playlist Load(long nID)
			{
				return Load(DBInteract.cCache, nID);
			}
			static public Playlist[] Get()
			{
				return DBInteract.cCache.PlaylistsGet();
			}
			static internal Playlist Deserialize(string sXML)
			{
				return (Playlist)(new XmlSerializer(typeof(Playlist), new XmlRootAttribute() { ElementName = "oPlaylist", IsNullable = true })).Deserialize(new System.IO.StringReader("<oPlaylist>" + sXML + "</oPlaylist>"));
			}
			static internal string Serialize(Playlist oPlaylist)
			{
				StringWriter oStringWriter = new StringWriter();
				(new XmlSerializer(typeof(Playlist), new XmlRootAttribute() { ElementName = "oPlaylist", IsNullable = true })).Serialize(oStringWriter, oPlaylist);
				return oStringWriter.ToString();
			}
			public Playlist OrderItemsByID()   // сортировка элементов ПЛ в БД приводит к многократному увеличению времени запроса почему-то...  from 2sec to 15sec
			{
				aItems = aItems.OrderBy(o => o.nID).ToArray();
				return this;
			}


			public long nID;
			public string sName;
			public DateTime dtStart;
			public DateTime dtStop;
			[XmlArrayItem("oItem", typeof(PlaylistItem))]
			public PlaylistItem[] aItems;

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
				dtStart = ahDBRow["dtStart"].ToDT();
				dtStop = ahDBRow["dtStop"].ToDT();
			}
			public Playlist Save()
			{
				return DBInteract.cCache.PlaylistSave(this);
			}
			public void SaveOnly()
			{
				DBInteract.cCache.PlaylistSaveOnly(this);
			}
			public void Delete()
			{
				DBInteract.cCache.PlaylistDelete(this);
			}
			public void Start()
			{
				DBInteract.cCache.PlaylistStart(this);
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
				if (null == cIP)
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
			: this(nID.ToID(), sBindID.ToString(), (cGatewayIP is Gateway.IP ? (Gateway.IP)cGatewayIP : new Gateway.IP(cGatewayIP.ToString())), nCount.ToUInt16(), nSourceNumber.ToUInt64(), nTargetNumber.ToUInt64(), sText.ToString(), (null == aImageBytes ? null : (byte[])aImageBytes), dtRegister.ToDT(), dtDisplay.ToDT())
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
    public class XAP
    {
        static object _oLockVersion = new object();
        static public string GetVersionOfDll(string sXapFileRelative, string sDllFileRelative)
        {
            lock (_oLockVersion)
            {
                string sRetVal = "";
                ZipArchive cZip;
                string sParentDir = HttpContext.Current.Server.MapPath("~/");
                string sDllName = sio.Path.GetFileName(sDllFileRelative);
                sXapFileRelative = sio.Path.Combine(sParentDir, sXapFileRelative);
                sDllFileRelative = sio.Path.Combine(sParentDir, sDllFileRelative);
                if (!System.IO.File.Exists(sXapFileRelative))
                    return "";
                cZip = new ZipArchive(System.IO.File.OpenRead(sXapFileRelative), ZipArchiveMode.Read);
                foreach (ZipArchiveEntry cZAE in cZip.Entries)
                    if (cZAE.Name == sDllName)
                    {
                        cZAE.ExtractToFile(sDllFileRelative);
                        System.Diagnostics.FileVersionInfo myFileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(sDllFileRelative);
                        sRetVal = myFileVersionInfo.FileVersion;
                        (new Logger()).WriteNotice("replica.dll version detected [" + sRetVal + "]");
                        sio.File.Delete(sDllFileRelative);
                        break;
                    }
                cZip.Dispose();
                cZip = null;
                return sRetVal;
            }
        }
    }
}
namespace helpers.replica.adm
{
	public class QueuedCommand //UNDONE нужно прикрутить получение параметров
	{
		public class Parameter
		{
			public long nID;
			public string sKey;
			public string sValue;
			public Parameter()
			{
				nID = x.ToID(null);
				sKey = null;
				sValue = null;
			}
			public Parameter(string sKey, string sValue)
				: this(-1, sKey, sValue)
			{
			}
			public Parameter(long nID, string sKey, string sValue)
			{
				this.nID = nID;
				this.sKey = sKey;
				this.sValue = sValue;
			}
			public Parameter(object nID, object sKey, object sValue)
				: this(nID.ToID(), sKey.ToString(), sValue.ToString())
			{
			}
			public Parameter(Hashtable ahDBRow)
				: this(ahDBRow["id"], ahDBRow["sKey"], ahDBRow["sValue"])
			{
			}
		}
		public long nID;
		public DateTime dt;
		public IdNamePair cCommand;
		public IdNamePair cCommandStatus;
		public IdNamePair cUser;
		public Parameter[] aParameters;

		public QueuedCommand()
		{
			nID = x.ToID(null);
			dt = DateTime.MaxValue;
			cCommand = null;
			cCommandStatus = null;
			cUser = null;
		}
		public QueuedCommand(string sCommand)
			: this(new IdNamePair(sCommand))
		{
		}
        public QueuedCommand(IdNamePair cCommand)
			: this(-1, DateTime.MaxValue, cCommand, null, null)
		{
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

		public static QueuedCommand[] Load(string sWhere, string sOrderBy, string sCount)
		{
			DBInteract cDBI = DBInteract.cCache;
			(new Logger("QueuedCommand")).WriteDebug("StatusChange [dbi=" + (cDBI == null ? "null" : cDBI.sUserName) + "]");
			List<QueuedCommand> aRetVal = new List<QueuedCommand>();
			foreach (QueuedCommand cQC in cDBI.RowsGet("SELECT * FROM adm.`vCommandsQueue` " + (sWhere == null ? "" : "WHERE " + sWhere) + (sOrderBy == null ? "" : " ORDER BY " + sOrderBy) + (sCount == null ? "" : " LIMIT " + sCount)).Select(o => new QueuedCommand(o)))
			{
				List<Parameter> aQCPs = new List<Parameter>();
				foreach (Parameter cQCP in cDBI.RowsGet("SELECT * FROM adm.`tCommandParameters` WHERE `idCommandsQueue`=" + cQC.nID).Select(o => new Parameter(o)))
				{
					aQCPs.Add(cQCP);
				}
				cQC.aParameters = aQCPs.ToArray();
                aRetVal.Add(cQC);
            }
			return aRetVal.ToArray();
		}
		public void Save()
		{
			DBInteract cDBI = DBInteract.cCache;
			(new Logger("QueuedCommand")).WriteDebug("StatusChange [dbi=" + (cDBI == null ? "null" : cDBI.sUserName) + "]");
			cDBI.TransactionBegin();
			try
			{
				Hashtable ahRow = cDBI.RowGet("SELECT `bValue`, `nValue` FROM adm.`fCommandsQueueAdd`('" + cCommand.sName + "')");
				bool bCommandAdded = ahRow["bValue"].ToBool();
				nID = ahRow["nValue"].ToID();
				if (bCommandAdded)
				{
					if (null != aParameters)
						foreach (Parameter cQCP in aParameters)
							if (!cDBI.BoolGet("SELECT `bValue` FROM adm.`fCommandParameterAdd` (" + nID + ",'" + cQCP.sKey + "', '" + cQCP.sValue + "')"))
								throw new Exception("command parameter adding failed! [command=" + nID + "][key=" + cQCP.sKey + "][value=" + cQCP.sValue + "]");
					cDBI.TransactionCommit();
				}
				else
					throw new Exception("command adding failed! [command=" + nID + "][name=" + cCommand.sName + "]");
			}
			catch (Exception ex)
			{
				cDBI.TransactionRollBack();
				throw ex;
			}
		}
		public void StatusChange(string sStatus)
		{
			DBInteract cDBI = DBInteract.cCache;
			(new Logger("QueuedCommand")).WriteDebug("StatusChange [dbi=" + (cDBI == null ? "null" : cDBI.sUserName) + "]");
			long nStatusID = cDBI.LongGet("select id from adm.`tCommandStatuses` where `sName` = '" + sStatus + "'");
			IdNamePair cIDNP = new IdNamePair() { sName = sStatus, nID = nStatusID };
			cDBI.Perform("UPDATE adm.`tCommandsQueue` SET `idCommandStatuses`= " + nStatusID + " WHERE id=" + nID);
			cCommandStatus = cIDNP;
		}

	}
}
namespace helpers.replica.tsr
{
	[Serializable]
	public class TSRItem
	{
		[XmlType("TSRType")]
		public enum Type
		{
			МОСКВА,
			СЕТЬ,
			NULL
		}
		public enum Block
		{
			РЕКЛАМА,
			АНОНС,
			NULL
		}
		private static string sSelect = "SELECT DISTINCT [mdVersion],[mdShortName],[mdName],[atpName],SUBSTRING([prgName], 1, 3) AS [mdType],[mtName] FROM [POWERGOLD].[dbo].[USER] ";  // top 1
		public string sS_Code;
		public string sVI_Code;
		public string sName;
		public object oTag;
		public Type eType;
		public Block eBlock;
		public TSRItem()
		{
			eType = Type.NULL;
			eBlock = Block.NULL;
		}
		public TSRItem(object oS_Code, object oVI_Code, object oName, object oType)
			: this(oS_Code, oVI_Code, oName, oType, null)
		{
		}
		public TSRItem(object oS_Code, object oVI_Code, object oName, object oType, object oBlock)
			: this()
		{
			sS_Code = oS_Code.ToString();
			sVI_Code = oVI_Code.ToString();
			sName = oName.ToString();
			eType = oType == null || "" == oType.ToString() ? Type.NULL : (Type)Enum.Parse(typeof(Type), oType.ToString());
			if (oBlock == null || "" == oBlock.ToString())
				eBlock = Block.NULL;
			else if (oBlock.ToString().Substring(0, 1).ToUpper() == "А")
				eBlock = Block.АНОНС;
			else
				eBlock = Block.РЕКЛАМА;
		}
		new public string ToString()
		{
			return "TSRItem = [scode=" + this.sS_Code + "][vicode=" + this.sVI_Code + "][name=" + this.sName + "][type=" + this.eType.ToString() + "][block=" + this.eBlock.ToString() + "]";
		}
		private static TSRItem ItemGetBySCode(string sTSRConnection, string sSCode)
		{
			return ItemGetByWhere(sTSRConnection, "WHERE [mdVersion] in ('" + sSCode + "');");
		}
		private static TSRItem ItemGetByVICode(string sTSRConnection, string sVICode)
		{
			return ItemGetByWhere(sTSRConnection, "WHERE [mdShortName] in ('" + sVICode + "');");
		}
		private static TSRItem ItemGetByWhere(string sTSRConnection, string sWhere)
		{
			SqlConnection cTSR_DBI = new SqlConnection(sTSRConnection);//webservice.Preferences.sTSRConnection
			SqlCommand command = new SqlCommand(sSelect + sWhere, cTSR_DBI);
			command.CommandTimeout = 60;
			SqlDataReader reader;

			try
			{
				cTSR_DBI.Open();
				reader = command.ExecuteReader();
			}
			catch (Exception ex)
			{
				throw new Exception(g.Helper.sTSR1 + " [" + sWhere + "][" + ex.Message + "]");
			}
			try
			{
				if (reader.Read())
					return new TSRItem(reader["mdVersion"], reader["mdShortName"], reader["mdName"], reader["atpName"]);
				else
					throw new Exception(g.Helper.sTSR2 + " [" + sWhere + "]");
			}
			finally
			{
				reader.Close();
			}
		}
		public static List<TSRItem> ItemsGetBySCodes(string sTSRConnection, List<string> aSCodes)
		{
			if (aSCodes.IsNullOrEmpty())
				return new List<TSRItem>();

			string sWhere = aSCodes.ToEnumerationString("'", null, null, true);
			return ItemsGetByWhere(sTSRConnection, "WHERE [mdVersion] in (" + sWhere + ");");
		}
		public static List<TSRItem> ItemsGetByVICodes(string sTSRConnection, List<string> aVICodes)
		{
			if (aVICodes.IsNullOrEmpty())
				return new List<TSRItem>();

			string sWhere = aVICodes.ToEnumerationString("'", null, null, true);
			return ItemsGetByWhere(sTSRConnection, "WHERE [mdShortName] in (" + sWhere + ");");
		}
		public static List<TSRItem> ItemsGetByWhere(string sTSRConnection, string sWhere)
		{
			SqlConnection cTSR_DBI = new SqlConnection(sTSRConnection);
			SqlCommand command = new SqlCommand(sSelect + sWhere, cTSR_DBI);
			command.CommandTimeout = 60;
			SqlDataReader reader;
			List<TSRItem> aRetVal = new List<TSRItem>();
			List<string> aSCodes = new List<string>();
			TSRItem cTSRI;
			try
			{
				cTSR_DBI.Open();
				reader = command.ExecuteReader();
			}
			catch (Exception ex)
			{
				throw new Exception(g.Helper.sTSR1 + " [" + sWhere + "][" + ex.Message + "]");
			}
			try
			{
				while (reader.Read())
				{
					cTSRI = new TSRItem(reader["mdVersion"], reader["mdShortName"], reader["mdName"], reader["atpName"], reader["mdType"]);
					if (!aSCodes.Contains(cTSRI.sS_Code))
					{
						aSCodes.Add(cTSRI.sS_Code);
						aRetVal.Add(cTSRI);
					}
					else
						(new Logger()).WriteWarning("TSR ERROR: один S-CODE и МОСКВА и СЕТЬ! " + cTSRI.ToString());
				}
			}
			catch (Exception ex)
			{
				throw new Exception(g.Helper.sTSR2 + " [" + sWhere + "][" + ex.Message + "]");
			}
			finally
			{
				reader.Close();
			}
			//if (aRetVal.Count < 1)
			//	throw new Exception(g.Helper.sTSR2 + " [" + sWhere + "]");  // не ошибка
			return aRetVal;
		}
		static public string VICodeGet(string sFilename)
		{
			if (null == sFilename || sFilename.Length <= 0)
				return null;
			int nIndx;
			char[] aFN = sFilename.ToCharArray();
			for (nIndx = 0; nIndx < sFilename.Length; nIndx++)
			{
				if (!char.IsDigit(aFN[nIndx]))
					break;
			}
            if (nIndx > 0)
            {
                return "VI" + sFilename.Substring(0, nIndx);
            }
            else
                return sio.Path.GetFileNameWithoutExtension(sFilename);    //.Replace(sio.Path.GetExtension(sFilename), ".mov"); // само имя файла тогда является кодом vi
		}
	}
}
