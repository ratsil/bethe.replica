using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using helpers;
using System.Xml;
using helpers.extensions;

namespace replica.failover
{
    public class Preferences : helpers.Preferences
    {
        static private Preferences _cInstance = new Preferences();

        public enum PersistentStatus
        {
            failed,
            played,
            skipped,
            onair,
            prepared,
            queued,
            planned
        }
        static public IdNamePair[] FakeStatusesGet()
        {
            IdNamePair[] aRetVal = new IdNamePair[Enum.GetValues(typeof(Preferences.PersistentStatus)).Length];
            int nI = 0;
            foreach (Preferences.PersistentStatus e in Enum.GetValues(typeof(Preferences.PersistentStatus)))
                aRetVal[nI++] = new IdNamePair(e.ToString());
            return aRetVal;
        }
        static public byte nSyncTries
        {
            get
            {
                return _cInstance._nSyncTries;
            }
        }
        static public string[] aReplacePath
        {
            get
            {
                return _cInstance._aPathReplace;
            }
        }
        static public string sStorageClips
        {
            get
            {
                return _cInstance._sStorageClips;
            }
        }
        static public TimeSpan tsAdjustment
        {
            get
            {
                return _cInstance._tsAdjustment;
            }
        }
        static public TimeSpan tsSyncMargin
        {
            get
            {
                return _cInstance._tsSyncMargin;
            }
        }
        static public TimeSpan tsSyncPeriodAfterError { get { return _cInstance._tsSyncPeriodAfterError; } }
        static public TimeSpan tsSyncPeriodShort { get { return _cInstance._tsSyncPeriodShort; } }
        static public TimeSpan tsSyncPeriodLong { get { return _cInstance._tsSyncPeriodLong; } }
        static public TimeSpan tsSyncDurMax { get { return _cInstance._tsSyncDurMax; } }
        static public ulong nPLDurSafe { get { return _cInstance._nPlDurSafe; } }
        static public TimeSpan tsPLDurMin { get { return _cInstance._tsPlDurMin; } }
        static public ulong nBlockElementDurMax { get { return _cInstance._nBlockElementDurMax; } }
        static public TimeSpan tsPLGetDurShort { get { return _cInstance._tsPlgetDurShort; } }
        static public TimeSpan tsPLGetDurLong { get { return _cInstance._tsPlgetDurLong; } }
        static public TimeSpan tsAnalysisDepthInSyncService { get { return _cInstance._tsAnalysisDepthInSyncService; } }
        static public TimeSpan tsMinCacheCover { get { return _cInstance._tsMinCacheCover; } }
        static public int nPLCountSafe { get { return _cInstance._nPlCountSafe; } }
        static public int nPLCountMin { get { return _cInstance._nPlCountMin; } }
        static public string sInfoPath { get { return _cInstance._sInfoPath; } }
        static public bool bDeepFileChecking { get { return _cInstance._bDeepFileChecking; } }



        static public helpers.replica.pl.Class cDefaultClass
        {
            get
            {
                return _cInstance._cDefaultClass;
            }
        }
        static public helpers.replica.cues.TemplateBind[] aDefaultTemplateBinds
        {
            get
            {
                return _cInstance._aDefaultTemplateBinds;
            }
        }
        static public helpers.replica.media.File cDefaultPlug
        {
            get
            {
                return _cInstance._cDefaultPlug;
            }
        }
        static public string sDefaultPlugClassName
        {
            get
            {
                return _cInstance._sDefaultPlugClassName;
            }
            set
            {
                _cInstance._sDefaultPlugClassName = value;
            }
        }
        static public IdNamePair[] aStatuses
        {
            get
            {
                return _cInstance._aStatuses;
            }
        }
        static public PersistentStatus[] aStatusesStaled
        {
            get
            {
                return _cInstance._aStatusesStaled;
            }
        }
        static public PersistentStatus[] aStatusesLocked
        {
            get
            {
                return _cInstance._aStatusesLocked;
            }
        }
        static public string sTemplatesDriveLetter
        {
            get
            {
                return _cInstance._sTemplatesDriveLetter;
            }
        }

        

        private string _sMainServerAddress;
        private string _sStorageClips;
        private string _sPathReplace;
        private string[] _aPathReplace;
        private TimeSpan _tsAdjustment;
        private TimeSpan _tsSyncMargin;
        private TimeSpan _tsSyncPeriodAfterError;
        private TimeSpan _tsSyncPeriodShort;
        private TimeSpan _tsSyncPeriodLong;
        private TimeSpan _tsSyncDurMax;
        private ulong _nPlDurSafe;
        private TimeSpan _tsPlDurMin;
        private ulong _nBlockElementDurMax;
        private TimeSpan _tsPlgetDurShort;
        private TimeSpan _tsPlgetDurLong;
        private TimeSpan _tsAnalysisDepthInSyncService;
        private TimeSpan _tsMinCacheCover;
        private int _nPlCountSafe;
        private int _nPlCountMin;
        private bool _bDeepFileChecking;
        private byte _nSyncTries;
        private string _sInfoPath;
        private helpers.replica.pl.Class _cDefaultClass;
        private helpers.replica.cues.TemplateBind[] _aDefaultTemplateBinds;
        private helpers.replica.media.File _cDefaultPlug;
        private string _sDefaultPlugClassName;
        private IdNamePair[] _aStatuses;
        private PersistentStatus[] _aStatusesStaled;
        private PersistentStatus[] _aStatusesLocked;
        private string _sTemplatesDriveLetter; // 'e:'

        public Preferences()
            : base("//replica/failover")
        {
        }
        override protected void LoadXML(XmlNode cXmlNode)
        {
            try
            {
                if (null == cXmlNode || _bInitialized)
                    return;
                _sMainServerAddress = cXmlNode.AttributeValueGet("main");
                _sStorageClips = cXmlNode.AttributeValueGet("clips");
                _sPathReplace = null;
                _sPathReplace = cXmlNode.AttributeValueGet("replace", false);
                (new Logger("pref")).WriteNotice("got _sPathReplace = " + _sPathReplace);
                if (_sPathReplace != null)
                {
                    _aPathReplace = _sPathReplace.Split('%');
                    (new Logger("pref")).WriteNotice("got _aPathReplace = " + _aPathReplace.Length);
                }
                if (!System.IO.Directory.Exists(_sStorageClips))
                    throw new Exception("указанный путь к медиа файлам не существует [clips:" + _sStorageClips + "]"); //TODO LANG
                _tsAdjustment = cXmlNode.AttributeGet<TimeSpan>("adjustment");

                XmlNode cNodeChild = cXmlNode.NodeGet("sync");
                _tsSyncMargin = cNodeChild.AttributeGet<TimeSpan>("margin");
                _tsSyncPeriodAfterError = cNodeChild.AttributeGet<TimeSpan>("sync_period_err");
                _tsSyncPeriodShort = cNodeChild.AttributeGet<TimeSpan>("sync_period_short");
                _tsSyncPeriodLong = cNodeChild.AttributeGet<TimeSpan>("sync_period_long");
                _tsSyncDurMax = cNodeChild.AttributeGet<TimeSpan>("sync_dur_max");
                _nPlDurSafe = (ulong)(cNodeChild.AttributeGet<TimeSpan>("pl_dur_safe").TotalSeconds * 25);
                _tsPlDurMin = cNodeChild.AttributeGet<TimeSpan>("pl_dur_min");
                _nBlockElementDurMax = (ulong)(cNodeChild.AttributeGet<TimeSpan>("block_elem_dur_max").TotalSeconds * 25);
                _tsPlgetDurShort = cNodeChild.AttributeGet<TimeSpan>("plget_dur_short");
                _tsPlgetDurLong = cNodeChild.AttributeGet<TimeSpan>("plget_dur_long");
                _nPlCountSafe = cNodeChild.AttributeGet<int>("pl_count_safe");
                _nPlCountMin = cNodeChild.AttributeGet<int>("pl_count_min");
                _bDeepFileChecking = cNodeChild.AttributeGet<bool>("deep_check");
                _nSyncTries = cNodeChild.AttributeGet<byte>("tries");
                _sInfoPath = cNodeChild.AttributeValueGet("info_path");

                string sSyncPrefsFile = System.IO.Path.Combine(cNodeChild.AttributeValueGet("sync_path"), "preferences.xml");
                (new Logger("pref")).WriteNotice("got sSyncPrefsFile = " + sSyncPrefsFile);
                XmlDocument cXMLDocument = new XmlDocument();
                cXMLDocument.LoadXml(System.IO.File.ReadAllText(sSyncPrefsFile));
                //(new Logger("pref")).WriteNotice("got sync cXMLDocument = " + cXMLDocument.OuterXml);
                XmlNode cRetVal = cXMLDocument.SelectNodes("/preferences/replica/sync/cache")[0];
                //(new Logger("pref")).WriteNotice("got sync xml = " + cRetVal.OuterXml);
                int nMinutes = cRetVal.AttributeGet<int>("depth");
                _tsAnalysisDepthInSyncService = TimeSpan.FromMinutes(nMinutes);
                (new Logger("pref")).WriteNotice("got sync depth nMinutes = " + nMinutes);
                _tsMinCacheCover = _tsAnalysisDepthInSyncService.Subtract(TimeSpan.FromHours(1));

                XmlNode cXmlNodeDefaults = cXmlNode.NodeGet("defaults");

                cNodeChild = cXmlNodeDefaults.NodeGet("cues");
                _sTemplatesDriveLetter = cNodeChild.AttributeValueGet("drive_letter", false);
                if (_sTemplatesDriveLetter != null && (_sTemplatesDriveLetter.Length != 2 || _sTemplatesDriveLetter.Substring(1, 1) != ":"))
                    throw new Exception("drive letter for cues must be like this: 'c:'");
                if (_sTemplatesDriveLetter != null)
                    _sTemplatesDriveLetter = _sTemplatesDriveLetter.ToLower();
                (new Logger("pref")).WriteNotice("got _sTemplatesDriveLetter = " + _sTemplatesDriveLetter);

                cNodeChild = cXmlNodeDefaults.NodeGet("class");
                long nID = cNodeChild.AttributeIDGet("id");
                string sValue = cNodeChild.AttributeValueGet("name");
                _cDefaultClass = new helpers.replica.pl.Class(nID, sValue);
                XmlNode[] aXmlNodesTemplates = cNodeChild.NodesGet("binds/template");
                List<helpers.replica.cues.TemplateBind> aTemplateBinds = new List<helpers.replica.cues.TemplateBind>();
                nID = 0;
                foreach (XmlNode cXmlNodeTemplate in aXmlNodesTemplates)
                {
                    sValue = cXmlNodeTemplate.AttributeValueGet("path");
                    if (!System.IO.File.Exists(sValue))
                        throw new Exception("указанный файл шаблона не существует [path:" + sValue + "][" + cXmlNodeTemplate.Name + "]][binds][" + cNodeChild.Name + "][" + cXmlNodeDefaults.Name + "]"); //TODO LANG
                    aTemplateBinds.Add(
                        new helpers.replica.cues.TemplateBind
                        (
                            nID++,
                            _cDefaultClass,
                            (
                                aTemplateBinds.Select(o => o.cTemplate).FirstOrDefault(o => sValue == o.sFile)
                                ?? new helpers.replica.cues.Template(nID, System.IO.Path.GetFileNameWithoutExtension(sValue), sValue)
                            ),
                            null,
                            cXmlNodeTemplate.AttributeValueGet("key"),
                            cXmlNodeTemplate.AttributeGet<int>("value")
                        )
                    );
                }
                _aDefaultTemplateBinds = aTemplateBinds.ToArray();

                cNodeChild = cXmlNodeDefaults.NodeGet("plug");
                nID = cNodeChild.AttributeIDGet("id");
                sValue = cNodeChild.AttributeValueGet("file");
                if (!System.IO.File.Exists(sValue))
                    throw new Exception("указанный файл заглушки не существует [file:" + sValue + "][" + cNodeChild.Name + "]"); //TODO LANG
                _cDefaultPlug = new helpers.replica.media.File(nID, System.IO.Path.GetFileName(sValue), new helpers.replica.media.Storage(nID, "default", System.IO.Path.GetDirectoryName(sValue) + System.IO.Path.AltDirectorySeparatorChar, true, nID, "default", null, null), DateTime.MaxValue, helpers.replica.Error.no, helpers.replica.media.File.Status.InStock, 0);
                _sDefaultPlugClassName = cNodeChild.AttributeValueGet("class");

                XmlNode cXmlNodeStatuses = cXmlNodeDefaults.NodeGet("statuses");
                IdNamePair cStatus;
                List<IdNamePair> aStatuses = new List<IdNamePair>();
                foreach (string sStatusName in Enum.GetNames(typeof(PersistentStatus)))
                {
                    cNodeChild = cXmlNodeStatuses.NodeGet(sStatusName);
                    nID = cNodeChild.AttributeIDGet("id");
                    sValue = cNodeChild.AttributeValueGet("name");
                    if (0 < aStatuses.Count(o => nID == o.nID || sValue == o.sName))
                        throw new Exception("указанный статус уже добавлен [id:" + nID + "][name:" + sValue + "][" + cNodeChild.Name + "]"); //TODO LANG
                    cStatus = new IdNamePair(nID, sValue);
                    aStatuses.Add(cStatus);
                }
                _aStatuses = aStatuses.ToArray();
                PersistentStatus eStatus;
                List<PersistentStatus> aGroupStatuses = new List<PersistentStatus>();
                foreach (XmlNode cXmlNodeGroup in cXmlNodeStatuses.NodesGet("group"))
                {
                    sValue = cXmlNodeGroup.AttributeValueGet("name");
                    aGroupStatuses.Clear();
                    foreach (XmlNode cXmlNodeStatus in cXmlNodeGroup.NodesGet())
                    {
                        try
                        {
                            eStatus = cXmlNodeStatus.Name.To<PersistentStatus>();
                            aGroupStatuses.Add(eStatus);
                        }
                        catch
                        {
                            throw new Exception("указанно некорректное имя статуса в группе [name:" + cXmlNodeStatus.Name + "][group:" + sValue + "][" + cXmlNodeGroup.Name + "][" + cXmlNodeStatuses.Name + "][" + cXmlNodeDefaults.Name + "]"); //TODO LANG
                        }
                    }
                    switch (sValue)
                    {
                        case "staled":
                            _aStatusesStaled = aGroupStatuses.ToArray();
                            break;
                        case "locked":
                            _aStatusesLocked = aGroupStatuses.ToArray();
                            break;
                        default:
                            throw new Exception("указана неизвестная группа статусов [name:" + sValue + "][" + cXmlNodeGroup.Name + "][" + cXmlNodeStatuses.Name + "][" + cXmlNodeDefaults.Name + "]"); //TODO LANG
                    }
                }
            }
            catch (Exception ex)
            {
                (new Logger("service")).WriteError(ex);
                throw;
            }
        }
    }
}
