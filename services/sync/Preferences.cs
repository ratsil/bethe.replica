using System;
using System.Collections.Generic;
using System.Text;
using helpers;
using helpers.extensions;
using System.Xml;

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
			public TimeSpan tsAgeMaximum;
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

		static public IntPtr nAffinity
		{
			get
			{
				return _cInstance._nAffinity;
			}
		}
		static public TimeSpan tsCommandsSleepDuration
		{
			get
			{
				return _cInstance._tsCommandsSleepDuration;
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

		private IntPtr _nAffinity;
		private TimeSpan _tsCommandsSleepDuration;

		private Cache _cCache;
		private Preview _cPreview;

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
            XmlNode cXmlNodeChild = cXmlNode.NodeGet("commands");
            _tsCommandsSleepDuration = cXmlNodeChild.AttributeGet<TimeSpan>("sleep");

            if (null != (cXmlNodeChild = cXmlNode.NodeGet("cache", false)))
			{
				_cCache = new Cache();
                _cCache.tsSleepDuration = cXmlNodeChild.AttributeGet<TimeSpan>("sleep");
                _cCache.sFolder = cXmlNodeChild.AttributeValueGet("folder");
				if (!System.IO.Directory.Exists(_cCache.sFolder))
					throw new Exception("указанная папка кэша не существует [folder:" + _cCache.sFolder + "][" + cXmlNodeChild.Name + "]"); //TODO LANG
                _cCache.nAnalysisDepth = cXmlNodeChild.AttributeGet<int>("depth");
                _cCache.tsAgeMaximum = cXmlNodeChild.AttributeGet<TimeSpan>("age");
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
