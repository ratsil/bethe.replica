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
			static public TimeSpan tsPlaylistMinimumLength
			{
				get
				{
					return _cInstance._tsPlaylistMinimumLength;
				}
			}
			static public TimeSpan tsBlockArtistDuration
			{
				get
				{
					return _cInstance._tsBlockArtistDuration;
				}
			}
            static public TimeSpan tsBlockForeignArtistDuration
            {
                get
                {
					return _cInstance._tsBlockForeignArtistDuration;
				}
            }
			static public TimeSpan tsBlockClipDuration
			{
				get
				{
					return _cInstance._tsBlockClipDuration;
				}
			}
			static public TimeSpan tsBlockClip_1_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_1_Duration;
				}
			}
			static public TimeSpan tsBlockClip_2_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_2_Duration;
				}
			}
			static public TimeSpan tsBlockClip_3_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_3_Duration;
				}
			}
			static public TimeSpan tsBlockClip_4_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_4_Duration;
				}
			}
			static public TimeSpan tsBlockClip_foreign_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_foreign_Duration;
				}
			}
			static public TimeSpan tsBlockClip_force_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_force_Duration;
				}
			}
			static public TimeSpan tsBlockClip_3minimum_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_3minimum_Duration;
				}
			}
			static public TimeSpan tsBlockClip_4minimum_Duration
			{
				get
				{
					return _cInstance._tsBlockClip_4minimum_Duration;
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
            static public string sBlockAdvTemplate
            {
                get
                {
                    return _cInstance._sBlockAdvTemplate;
                }
            }
            static public string sChannel
			{
				get
				{
					return _cInstance._sChannel;
				}
			}
			static public string sAdvertsPath
			{
				get
				{
					return _cInstance._sAdvertsPath;
				}
			}
			static public string sVIMailTargets
			{
				get
				{
					return _cInstance._sVIMailTargets;
				}
			}
			static public string sPgDumpBinPath
			{
				get
				{
					return _cInstance._sPgDumpBinPath;
				}
			}
			static public string sPgDumpPath
			{
				get
				{
					return _cInstance._sPgDumpPath;
				}
			}
			static public string sPgDumpName
			{
				get
				{
					return _cInstance._sPgDumpName;
				}
			}
			static public TimeSpan tsPgDumpSleepDuration
			{
				get
				{
					return _cInstance._tsPgDumpSleepDuration;
				}
			}
			static public string sPgDBName
			{
				get
				{
					return _cInstance._sPgDBName;
				}
			}
			static public string sPgDBHostName
			{
				get
				{
					return _cInstance._sPgDBHostName;
				}
			}
			static public string sPgDumpCopyToPath
			{
				get
				{
					return _cInstance._sPgDumpCopyToPath;
				}
			}
			static public string sPgDumpCopyToLogin
			{
				get
				{
					return _cInstance._sPgDumpCopyToLogin;
				}
			}
			static public string sPgDumpCopyToPass
			{
				get
				{
					return _cInstance._sPgDumpCopyToPass;
				}
			}
			static public string sPgDBPort
			{
				get
				{
					return _cInstance._sPgDBPort;
				}
			}


			private string _sChannel;
			private string _sAdvertsPath;
			private string _sVIMailTargets;
			private bool _bPlaylistGenerating;
			private TimeSpan _tsPlaylistGenerationLength;
			private TimeSpan _tsPlaylistMinimumLength;
			private TimeSpan _tsBlockArtistDuration;
			private TimeSpan _tsBlockForeignArtistDuration;
			private TimeSpan _tsBlockClipDuration;
			private TimeSpan _tsBlockClip_1_Duration;
			private TimeSpan _tsBlockClip_2_Duration;
			private TimeSpan _tsBlockClip_3_Duration;
			private TimeSpan _tsBlockClip_4_Duration;
			private TimeSpan _tsBlockClip_foreign_Duration;
			private TimeSpan _tsBlockClip_force_Duration;
			private TimeSpan _tsBlockClip_3minimum_Duration;
			private TimeSpan _tsBlockClip_4minimum_Duration;
			private TimeSpan _tsSleepDuration;
			private TimeSpan _tsCommandsSleepDuration;
            private string _sBlockAdvTemplate;
            private string _sPgDumpBinPath;
			private string _sPgDumpPath;
			private string _sPgDumpName;
			private string _sPgDBName;
			private string _sPgDBHostName;
			private string _sPgDBPort;
			private string _sPgDumpCopyToPath;
			private string _sPgDumpCopyToLogin;
			private string _sPgDumpCopyToPass;
			private TimeSpan _tsPgDumpSleepDuration;

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
					_sChannel = cNodePlaylist.AttributeValueGet("channel", true);
					_sAdvertsPath = cNodePlaylist.AttributeValueGet("adv_path", false);
					_sVIMailTargets = cNodePlaylist.AttributeValueGet("vi_mail_target", false);
					_bPlaylistGenerating = true;
					cNodeChild = cNodePlaylist.NodeGet("generation");
					_tsPlaylistGenerationLength = cNodeChild.AttributeGet<TimeSpan>("length");
					if (null != cNodeChild.AttributeValueGet("pl_min", false))
						_tsPlaylistMinimumLength = cNodeChild.AttributeGet<TimeSpan>("pl_min");
					else
						_tsPlaylistMinimumLength = _tsPlaylistGenerationLength;
                    cNodeChild = cNodePlaylist.NodeGet("blocks");
					_tsBlockArtistDuration = cNodeChild.AttributeGet<TimeSpan>("artist");
					_tsBlockForeignArtistDuration = cNodeChild.AttributeGet<TimeSpan>("foreign");
					_tsBlockClipDuration = cNodeChild.AttributeGet<TimeSpan>("clip");
					_tsBlockClip_1_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_1", TimeSpan.MinValue);
					_tsBlockClip_2_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_2", TimeSpan.MinValue);
					_tsBlockClip_3_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_3", TimeSpan.MinValue);
					_tsBlockClip_4_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_4", TimeSpan.MinValue);
                    _tsBlockClip_foreign_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_foreign", TimeSpan.MinValue);
					_tsBlockClip_3minimum_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_3min", TimeSpan.MinValue);
					_tsBlockClip_4minimum_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_4min", TimeSpan.MinValue);
					_tsBlockClip_force_Duration = cNodeChild.AttributeOrDefaultGet<TimeSpan>("clip_force", TimeSpan.FromHours(1));
                    _sBlockAdvTemplate = cNodeChild.AttributeOrDefaultGet<string>("adv_template", null);
                }
				else
					_bPlaylistGenerating = false;

				cNodePlaylist = cXmlNode.NodeGet("pg_dump", false);
				if (null != cNodePlaylist)
				{
					_sPgDumpBinPath = cNodePlaylist.AttributeOrDefaultGet<string>("path_bin", null);
					_sPgDumpPath = cNodePlaylist.AttributeValueGet("path_out", true);
					_sPgDumpName = cNodePlaylist.AttributeValueGet("name_out", true);
					_sPgDBName = cNodePlaylist.AttributeValueGet("db_name", true);
					_sPgDBHostName = cNodePlaylist.AttributeValueGet("db_host", true);
					_sPgDBPort = cNodePlaylist.AttributeOrDefaultGet<string>("port", "5432");
					_sPgDumpCopyToPath = cNodePlaylist.AttributeOrDefaultGet<string>("copy_to", null);
					_sPgDumpCopyToLogin = cNodePlaylist.AttributeOrDefaultGet<string>("login", null);
					_sPgDumpCopyToPass = cNodePlaylist.AttributeOrDefaultGet<string>("pass", null);
					_tsPgDumpSleepDuration = cNodePlaylist.AttributeGet<TimeSpan>("sleep");
				}
				else
					_sPgDumpBinPath = null;

				cNodeChild = cXmlNode.NodeGet("commands");
				_tsCommandsSleepDuration = cNodeChild.AttributeGet<TimeSpan>("sleep");
			}
		}
	}
}
