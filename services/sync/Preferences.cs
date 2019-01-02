using System;
using System.Collections.Generic;
using System.Text;
using helpers;
using helpers.extensions;
using System.Xml;
using System.Linq;

namespace replica.sync
{
	public class Preferences : helpers.Preferences
	{
		static private Preferences _cInstance = new Preferences();

		public class Cache
		{
			public TimeSpan tsSleepDuration;
			public string sFolder;
			public int nAnalysisDepth;
            public int nAlertFreeSpace;
            public bool bSlowCopy;   // for hd playback from remote raid
            public int nSlowCopyPeriod;
            public int nSlowCopyDelay;
            public bool bDBReadOnly;
			public TimeSpan tsAgeMaximum;
            public TimeSpan tsCacheRewriteMinimum;
            public string[] aIgnoreFiles;
			public string[] aIgnoreStorages;
		}
		public class Preview
		{
			public TimeSpan tsSleepDuration;
			public string sFolder;

			public ushort nVideoWidth;
			public ushort nVideoHeight;
			public ffmpeg.net.PixelFormat eVideoPixelFormat;
			public ffmpeg.net.AVCodecID eVideoCodecID;

			public int nAudioSamplesRate;
			public int nAudioChannelsQty;
			public ffmpeg.net.AVSampleFormat eAudioSampleFormat;
			public ffmpeg.net.AVCodecID eAudioCodecID;
		}
		public class Storage
		{
			public string sMoveToFolder;
            public string sTrashFolder;
            public bool bAddFreeFiles;
			public bool bUseFFMPEG;
			public int nDeleteMoveTimeoutDefault;
            public Dictionary<string, int> ahDeleteMoveTimeoutsByStorages;
            public string sOnFileReplaceMailRecipients;

        }
		public class Commands
		{
			public TimeSpan tsCommandsSleepDuration;
		}

		static public IntPtr nAffinity
		{
			get
			{
				return _cInstance._nAffinity;
			}
		}
		static public Cache cCache
		{
			get
			{
				return _cInstance._cCache;
			}
		}
		static public Preview cPreview
		{
			get
			{
				return _cInstance._cPreview;
			}
		}
		static public Storage cStorage
		{
			get
			{
				return _cInstance._cStorage;
			}
		}
		static public Commands cCommands
		{
			get
			{
				return _cInstance._cCommands;
			}
		}

		private IntPtr _nAffinity;

		private Cache _cCache;
		private Preview _cPreview;
		private Storage _cStorage;
		private Commands _cCommands;

		public Preferences()
			: base("//replica/sync")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode || _bInitialized)
				return;
            string sAffinity = cXmlNode.AttributeValueGet("affinity", false);
			if (sAffinity.IsNullOrEmpty())
				_nAffinity = IntPtr.Zero;
			else
				_nAffinity = (IntPtr)uint.Parse(sAffinity, System.Globalization.NumberStyles.HexNumber);
            XmlNode cXmlNodeChild = cXmlNode.NodeGet("commands", false);
			if (null != cXmlNodeChild)
			{
				_cCommands = new Commands();
				_cCommands.tsCommandsSleepDuration = cXmlNodeChild.AttributeGet<TimeSpan>("sleep");
			}

            if (null != (cXmlNodeChild = cXmlNode.NodeGet("cache", false)))
			{
				_cCache = new Cache();
				_cCache.bDBReadOnly = cXmlNodeChild.AttributeOrDefaultGet<bool>("db_readonly", false);
				_cCache.tsSleepDuration = cXmlNodeChild.AttributeGet<TimeSpan>("sleep");
                _cCache.tsCacheRewriteMinimum = cXmlNodeChild.AttributeOrDefaultGet<TimeSpan>("rewrite_minimum", TimeSpan.FromMinutes(40)); // для бекапа и т.п., где кешируется намного больше, чем на мейне и потому надо обновлять кеш. Новости, например, и т.д.
                _cCache.sFolder = cXmlNodeChild.AttributeValueGet("folder");
                _cCache.nAlertFreeSpace= cXmlNodeChild.AttributeOrDefaultGet<int>("alert_space", 100); // 100 гигабайт
                if (!System.IO.Directory.Exists(_cCache.sFolder))
					throw new Exception("указанная папка кэша не существует [folder:" + _cCache.sFolder + "][" + cXmlNodeChild.Name + "]"); //TODO LANG
                _cCache.nAnalysisDepth = cXmlNodeChild.AttributeGet<int>("depth");  // in minutes
				_cCache.bSlowCopy = cXmlNodeChild.AttributeOrDefaultGet<bool>("slow_copy", false);
                _cCache.nSlowCopyPeriod = (int)cXmlNodeChild.AttributeOrDefaultGet<TimeSpan>("slow_copy_period", TimeSpan.FromSeconds(0)).TotalMilliseconds; // 0 == no delay
                _cCache.nSlowCopyDelay = (int)cXmlNodeChild.AttributeOrDefaultGet<TimeSpan>("slow_copy_delay", TimeSpan.FromSeconds(0)).TotalMilliseconds;
                _cCache.tsAgeMaximum = cXmlNodeChild.AttributeGet<TimeSpan>("age");  // must be greater than nAnalysisDepth
				if (_cCache.tsAgeMaximum.TotalMinutes < 2 * _cCache.nAnalysisDepth)
				{
					TimeSpan tsOld = _cCache.tsAgeMaximum;
					_cCache.tsAgeMaximum = TimeSpan.FromMinutes(2 * _cCache.nAnalysisDepth);
					(new Logger("sync_prefs")).WriteNotice("Storing age must be greater than double analysis depth - changed. [old=" + tsOld.TotalMinutes + " min][new=" + _cCache.tsAgeMaximum.TotalMinutes + " min]");
				}

				string sIgnore = cXmlNodeChild.AttributeValueGet("ignor_files", false);
                if (null != sIgnore)
                {
                    _cCache.aIgnoreFiles = sIgnore.Split(new char[] { ',', ';' }).Select(o => o.Trim()).ToArray();
                    sIgnore = "";
                    foreach (string sStr in _cCache.aIgnoreFiles)
                        sIgnore += "[" + sStr + "]\t";
                    (new Logger("sync_prefs")).WriteNotice("ignor_files:" + sIgnore);
                }
                else
                    _cCache.aIgnoreFiles = new string[0];

                sIgnore = cXmlNodeChild.AttributeValueGet("ignor_storages", false);
                if (null != sIgnore)
                {
                    _cCache.aIgnoreStorages = sIgnore.Split(new char[] { ',', ';' }).Select(o => o.Trim().Replace("\\", "").Replace("/", "")).ToArray();
                    sIgnore = "";
                    foreach (string sStr in _cCache.aIgnoreStorages)
                        sIgnore += "[" + sStr + "]\t";
                    (new Logger("sync_prefs")).WriteNotice("ignor_storages:" + sIgnore);
                }
                else
                    _cCache.aIgnoreStorages = new string[0];
            }
			if (null != (cXmlNodeChild = cXmlNode.NodeGet("storage", false)))
			{
				_cStorage = new Storage();
				_cStorage.sMoveToFolder = cXmlNodeChild.AttributeValueGet("move_folder", false);
                _cStorage.sTrashFolder = cXmlNodeChild.AttributeValueGet("trash_folder", false);

                if (null != _cStorage.sMoveToFolder && !System.IO.Directory.Exists(_cStorage.sMoveToFolder))
                {
                    (new Logger("sync_prefs")).WriteError("move folder not found: [dir=" + _cStorage.sMoveToFolder + "]");
                    _cStorage.sMoveToFolder = null;
                }
                else
                    (new Logger("sync_prefs")).WriteNotice("move folder was found: [dir=" + _cStorage.sMoveToFolder + "]");

                _cStorage.bAddFreeFiles = cXmlNodeChild.AttributeOrDefaultGet<bool>("add_free_files", true); // add (or ignore) files not through ingest system
				_cStorage.bUseFFMPEG = cXmlNodeChild.AttributeOrDefaultGet<bool>("use_ffmpeg", false);  // determine and add to DB file attributes using ffmpeg
                _cStorage.sOnFileReplaceMailRecipients = cXmlNodeChild.AttributeOrDefaultGet<string>("file_replace_mail", "");
                (new Logger("sync_prefs")).WriteNotice("file_replace_mail: [" + _cStorage.sOnFileReplaceMailRecipients + "]");

                _cStorage.nDeleteMoveTimeoutDefault = 10;
                XmlNode cXmlNodeTimeouts = cXmlNodeChild.NodeGet("del_move_timeouts", false);
                if (null != cXmlNodeTimeouts)
                {
                    _cStorage.nDeleteMoveTimeoutDefault = cXmlNodeTimeouts.AttributeOrDefaultGet<int>("default", 10);
                    XmlNode[] aNodes = cXmlNodeTimeouts.NodesGet("timeout", false);
                    if (null != aNodes)
                    {
                        _cStorage.ahDeleteMoveTimeoutsByStorages = new Dictionary<string, int>();
                        foreach (XmlNode cNode in aNodes)
                        {
                            _cStorage.ahDeleteMoveTimeoutsByStorages.Add(cNode.AttributeGet<string>("storage"), cNode.AttributeGet<int>("days"));
                        }
                    }
                }
            }
            XmlNode cXmlNodePreview = cXmlNode.NodeGet("preview", false);
			if(null != cXmlNodePreview)
			{
				_cPreview = new Preview();
                _cPreview.tsSleepDuration = cXmlNodePreview.AttributeGet<TimeSpan>("sleep");
                _cPreview.sFolder = cXmlNodePreview.AttributeValueGet("folder");
                cXmlNodeChild = cXmlNodePreview.NodeGet("video");
                _cPreview.nVideoWidth = cXmlNodeChild.AttributeGet<ushort>("width");
                _cPreview.nVideoHeight = cXmlNodeChild.AttributeGet<ushort>("height");
                _cPreview.eVideoPixelFormat = ("av_pix_fmt_" + cXmlNodeChild.AttributeValueGet("pixels")).To<ffmpeg.net.PixelFormat>();
				_cPreview.eVideoCodecID = ("av_codec_id_" + cXmlNodeChild.AttributeValueGet("codec")).To<ffmpeg.net.AVCodecID>();

                cXmlNodeChild = cXmlNodePreview.NodeGet("audio");
                _cPreview.nAudioSamplesRate = cXmlNodeChild.AttributeGet<int>("rate");
                _cPreview.nAudioChannelsQty = cXmlNodeChild.AttributeGet<int>("channels");
                _cPreview.eAudioSampleFormat = ("av_sample_fmt_" + cXmlNodeChild.AttributeValueGet("bits")).To<ffmpeg.net.AVSampleFormat>();
                _cPreview.eAudioCodecID = ("av_codec_id_" + cXmlNodeChild.AttributeValueGet("codec")).To<ffmpeg.net.AVCodecID>();
			}
		}
	}
}
