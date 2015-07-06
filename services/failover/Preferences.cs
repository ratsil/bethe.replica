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

		static public byte nSyncTries
		{
			get
			{
				return _cInstance._nSyncTries;
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

		private string _sMainServerAddress;
		private string _sStorageClips;
		private TimeSpan _tsAdjustment;
		private TimeSpan _tsSyncMargin;
		private byte _nSyncTries;
		private helpers.replica.pl.Class _cDefaultClass;
		private helpers.replica.cues.TemplateBind[] _aDefaultTemplateBinds;
		private helpers.replica.media.File _cDefaultPlug;
		private string _sDefaultPlugClassName;
		private IdNamePair[] _aStatuses;
		private PersistentStatus[] _aStatusesStaled;
		private PersistentStatus[] _aStatusesLocked;

		public Preferences()
			: base("//replica/failover")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode || _bInitialized)
				return;
			_sMainServerAddress = cXmlNode.AttributeValueGet("main");
			_sStorageClips = cXmlNode.AttributeValueGet("clips");
			if (!System.IO.Directory.Exists(_sStorageClips))
				throw new Exception("указанный путь к медиа файлам не существует [clips:" + _sStorageClips + "]"); //TODO LANG
			_tsAdjustment = cXmlNode.AttributeGet<TimeSpan>("adjustment");

			XmlNode cNodeChild = cXmlNode.NodeGet("sync");
            _tsSyncMargin = cNodeChild.AttributeGet<TimeSpan>("margin");
            _nSyncTries = cNodeChild.AttributeGet<byte>("tries");

			XmlNode cXmlNodeDefaults = cXmlNode.NodeGet("defaults");

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
			_cDefaultPlug = new helpers.replica.media.File(nID, System.IO.Path.GetFileName(sValue), new helpers.replica.media.Storage(nID, "default", System.IO.Path.GetDirectoryName(sValue), true, nID, "default"), DateTime.MaxValue, helpers.replica.Error.no);
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
	}
}
