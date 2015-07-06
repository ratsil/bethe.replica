using System;
using System.Collections.Generic;
using System.Text;
using helpers;
using helpers.extensions;
using System.Xml;

namespace replica
{
	namespace management
	{
		public class Preferences : helpers.Preferences
		{
			static private Preferences _cInstance = new Preferences();

			static public bool bPlaylistGenerating
			{
				get
				{
					return _cInstance._bPlaylistGenerating;
				}
			}
			static public TimeSpan tsPlaylistGenerationLength
			{
				get
				{
					return _cInstance._tsPlaylistGenerationLength;
				}
			}
			static public TimeSpan tsBlockArtistDuration
			{
				get
				{
					return _cInstance._tsBlockArtistDuration;
				}
			}
            static public TimeSpan tsBlockForeignDuration
            {
                get
                {
					return _cInstance._tsBlockForeignDuration;
				}
            }
			static public TimeSpan tsBlockClipDuration
			{
				get
				{
					return _cInstance._tsBlockClipDuration;
				}
			}
			static public TimeSpan tsSleepDuration
			{
				get
				{
					return _cInstance._tsSleepDuration;
				}
			}
			static public TimeSpan tsCommandsSleepDuration
			{
				get
				{
					return _cInstance._tsCommandsSleepDuration;
				}
			}

			private bool _bPlaylistGenerating;
			private TimeSpan _tsPlaylistGenerationLength;
			private TimeSpan _tsBlockArtistDuration;
			private TimeSpan _tsBlockForeignDuration;
			private TimeSpan _tsBlockClipDuration;
			private TimeSpan _tsSleepDuration;
			private TimeSpan _tsCommandsSleepDuration;

			public Preferences()
				: base("//replica/management")
			{
			}
			override protected void LoadXML(XmlNode cXmlNode)
			{
                if (null == cXmlNode || _bInitialized)
					return;
				_tsSleepDuration = cXmlNode.AttributeGet<TimeSpan>("sleep");
				XmlNode cNodeChild;
				XmlNode cNodePlaylist = cXmlNode.NodeGet("playlist", false);
				if (null != cNodePlaylist)
				{
					_bPlaylistGenerating = true;
                    cNodeChild = cNodePlaylist.NodeGet("generation");
                    _tsPlaylistGenerationLength = cNodeChild.AttributeGet<TimeSpan>("length");
                    cNodeChild = cNodePlaylist.NodeGet("blocks");
                    _tsBlockArtistDuration = cNodeChild.AttributeGet<TimeSpan>("artist");
                    _tsBlockForeignDuration = cNodeChild.AttributeGet<TimeSpan>("foreign");
                    _tsBlockClipDuration = cNodeChild.AttributeGet<TimeSpan>("clip");
				}
				else
					_bPlaylistGenerating = false;
				cNodeChild = cXmlNode.NodeGet("commands");
				_tsCommandsSleepDuration = cNodeChild.AttributeGet<TimeSpan>("sleep");
			}
		}
	}
}
