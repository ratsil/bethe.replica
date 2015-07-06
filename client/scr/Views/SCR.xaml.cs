//#define LOCAL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;

using System.Threading;
using helpers.replica.services.dbinteract;
using scr.services.ingenie.cues;
using IC = scr.services.ingenie.cues;
using scr.services.ingenie.player;
using IP = scr.services.ingenie.player;
using scr.childs;
using controls.replica.sl;
using controls.sl;
using controls.childs.sl;
using controls.extensions.sl;
using helpers.sl;
using helpers.extensions;
using scr.services.preferences;
using scr.Views;
using DBI = helpers.replica.services.dbinteract;

using g = globalization;

namespace scr
{
	public partial class SCR : Page
	{
		public class Plaque
		{
			public int nGroupNumber { get; set; }
			public long nID { get; set; }
			public DBI.IdNamePair cPreset { get; set; }
			public string sName { get; set; }
			public string sFirstLine { get; set; }
			public string sSecondLine { get; set; }
			public static implicit operator Plaque(DBI.Plaque cPlaque)
			{
				Plaque cRetVal=new Plaque();
				cRetVal.nID = cPlaque.nID;
				cRetVal.cPreset = cPlaque.cPreset;
				cRetVal.sFirstLine = cPlaque.sFirstLine;
				cRetVal.sSecondLine = cPlaque.sSecondLine;
				cRetVal.sName = cPlaque.sName;
				return cRetVal;
			}
			public static implicit operator DBI.Plaque(Plaque cItem)
			{
				return new DBI.Plaque() { nID = cItem.nID, cPreset = cItem.cPreset, sFirstLine = cItem.sFirstLine, sSecondLine = cItem.sSecondLine, sName = cItem.sName };
			}
		}
		private Progress _dlgProgress;
		private DBInteract _cDBI;
		private PlayerSoapClient _cPlayer;
		private CuesSoapClient _cCues;
		private TemplateButton[] _aTemplateButtons;

		internal LPLWatcher _cLPLWatcher;
		private MsgBox _dlgMsgBox;

		private System.Windows.Threading.DispatcherTimer _cTimerForStatusGet;
		private System.Windows.Threading.DispatcherTimer _cTimerForPingDBI;
		private System.Windows.Threading.DispatcherTimer _cTimerForPingPlayer;
		private System.Windows.Threading.DispatcherTimer _cTimerForPingCues;
		private System.Windows.Threading.DispatcherTimer _cTimerForbtnStartAirError;
		private System.Windows.Threading.DispatcherTimer _cTimerForNearestAdvBlock;
		private System.Windows.Threading.DispatcherTimer _cTimerForRefreshAfterSkip;
		private DateTime dtNearestAdvBlock;
		private DateTime dtNextRefresh;  // чтобы клиент не терял сервис, будем взмолаживать......
		private DateTime dtNextMouseClickForDouble;
		private string sSequenceDirectory;
		private string sTrailDirectory;
		private List<IP.IdNamePair> _aStorages;
		internal List<LivePLItem> _aLivePLTotal;
		internal List<LivePLItem> _aLivePLSingle;
		internal List<LivePLItem> _aLivePLWithBlocks;
		AdvertsBlockChooser _dlgAdvChooser;
		FilesChooser _dlgFilesChooser;
		internal Shift _cShiftCurrent;
//		List<LivePLItem> _aSelectedClips;
		Plaque _cSelectedPlaque;
		LivePLItem _cSelectedLPLI;
		List<ulong> _aDTMFedItemsIDs;
		bool _bClipsGot, _bStatusGotForFirstTime;
		ulong _nWorkstationID;
		string _sLogTB;

		Item _cPlaylist
		{
			get
			{
				lock (_aTemplateButtons)
					return (Item)_ui_ctrTB_PlayList.cItem;
			}
		}
		Item _cPlaylistPrevious
		{
			get
			{
				lock (_aTemplateButtons)
					return (Item)_ui_ctrTB_PlayList.cItemPrevious;
			}
		}
		private Preset[] _aPresets
		{
			get
			{
				return _ui_tcAirTemplates.Items.Select(o => (Preset)((TabItem)o).Tag).ToArray();
			}
		}
		public Preset _cPresetSelected
		{
			get
			{
				return ((Preset)((TabItem)_ui_tcAirTemplates.SelectedItem).Tag);
			}
		}
		private Item[] _aCuesItems
		{
			get
			{
				lock (_aTemplateButtons)
					return _aTemplateButtons.Where(o => o != _ui_ctrTB_PlayList).Select(o => (Item)o.cItem).Where(o => null != o).ToArray();
			}
		}
		private Item[] _aItems
		{
			get
			{
				lock (_aTemplateButtons)
					return _aTemplateButtons.Select(o => (Item)o.cItem).Where(o => null != o).ToArray();
			}
		}
		public bool IsAirGoingNow
		{
			get
			{
				return (g.SCR.sNotice4.ToUpper() == _ui_btnStartAirText1.Text);
			}
		}

		public SCR()
		{
			try
			{
				InitializeComponent();

				//foreach (TabItem ui in _ui_tcAirTemplates.Items)
				//    if (null != ui.Tag && ui.Tag is string)
				//        ui.Tag = App.cPreferences.aPresets.FirstOrDefault(o => (string)ui.Tag == o.sName);




				//todo
				
				_ui_tiOrderDesk.Visibility = System.Windows.Visibility.Collapsed;

				TabItem ui_ti;
				foreach (Preset cPreset in App.cPreferences.aPresets)
				{
					if (cPreset.nID > 1)
					{
						ui_ti = new TabItem() { FontSize = 12 };
						_ui_tcAirTemplates.Items.Add(ui_ti);
					}
					else
						ui_ti = (TabItem)_ui_tcAirTemplates.Items[(int)cPreset.nID];

					ui_ti.Visibility = System.Windows.Visibility.Visible;
					ui_ti.Tag = cPreset;
					ui_ti.Header = cPreset.sCaption;
					ui_ti.Name = cPreset.sName;
				}



				_aTemplateButtons = new TemplateButton[] {
					_ui_ctrTB_Credits,
					_ui_ctrTB_Logo,
					_ui_ctrTB_PlayList,
					_ui_ctrTB_Template1Logo,
					_ui_ctrTB_Template1Chat,
					_ui_ctrTB_Template1Credits,
					_ui_ctrTB_Template1Bumper,
					_ui_ctrTB_TemplateUser1,
					_ui_ctrTB_TemplateUser2,
					_ui_ctrTB_TemplateUser3,
					_ui_ctrTB_TemplateUser4,
					_ui_ctrTB_TemplateSequence,
					_ui_ctrTB_Template1Plaques,
					_ui_ctrTB_Channel1,
					_ui_ctrTB_Channel2,
					_ui_ctrTB_Channel3,
					_ui_ctrTB_Channel4
				};
				_aLivePLTotal = new List<LivePLItem>();
				_aLivePLSingle = new List<LivePLItem>();
				_aLivePLWithBlocks = new List<LivePLItem>();
				_dlgProgress = new Progress();
				_dlgMsgBox = new MsgBox();
				_ui_tiOrderDesk.Content = null;
				_bStatusGotForFirstTime = true;
				_aDTMFedItemsIDs = new List<ulong>();
				//_bIsPLDeletingEnable = true;

				_dlgProgress.Show();
				_cLPLWatcher = null;
				TimersOff();

				_cShiftCurrent = null;


				#region dbi
				_cDBI = new DBInteract();
				_cDBI.InitCompleted += new EventHandler<helpers.replica.services.dbinteract.InitCompletedEventArgs>(_cDBI_InitCompleted);
				_cDBI.PlaquesGetCompleted += new EventHandler<PlaquesGetCompletedEventArgs>(_cDBI_PlaquesGetCompleted);
				_cDBI.PlaqueAddCompleted += new EventHandler<PlaqueAddCompletedEventArgs>(_cDBI_PlaqueAddCompleted);
				_cDBI.PlaqueDeleteCompleted += new EventHandler<PlaqueDeleteCompletedEventArgs>(_cDBI_PlaqueDeleteCompleted);
				_cDBI.PlaqueChangeCompleted += new EventHandler<PlaqueChangeCompletedEventArgs>(_cDBI_PlaqueChangeCompleted);
				_cDBI.ShiftAddCompleted += new EventHandler<ShiftAddCompletedEventArgs>(_cDBI_ShiftAddCompleted);
				_cDBI.ShiftStartCompleted += new EventHandler<ShiftStartCompletedEventArgs>(_cDBI_ShiftStartCompleted);
				_cDBI.ShiftStopCompleted += new EventHandler<ShiftStopCompletedEventArgs>(_cDBI_ShiftStopCompleted);
				_cDBI.ShiftCurrentGetCompleted += new EventHandler<ShiftCurrentGetCompletedEventArgs>(_cDBI_ShiftCurrentGetCompleted);
				_cDBI.NearestAdvertsBlockCompleted += new EventHandler<NearestAdvertsBlockCompletedEventArgs>(_cDBI_NearestAdvertsBlockCompleted);
				_cDBI.PingCompleted += new EventHandler<PingCompletedEventArgs>(_cDBI_PingCompleted);
				_cDBI.CuesGetCompleted += new EventHandler<CuesGetCompletedEventArgs>(_cDBI_CuesGetCompleted);
				_cDBI.StorageSCRGetCompleted += new EventHandler<StorageSCRGetCompletedEventArgs>(_cDBI_StorageSCRGetCompleted);
				_cDBI.ClipsBDLogCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cDBI_ClipsBDLogCompleted);
				#endregion

				#region player
				_cPlayer = new PlayerSoapClient();
				_cPlayer.InitCompleted += new EventHandler<services.ingenie.player.InitCompletedEventArgs>(_cPlayer_InitCompleted);
				_cPlayer.PingCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cPlayer_PingCompleted);
	
				_cPlayer.ItemCreateCompleted += new EventHandler<services.ingenie.player.ItemCreateCompletedEventArgs>(_cPlayer_ItemCreateCompleted);
				_cPlayer.ItemPrepareCompleted += new EventHandler<services.ingenie.player.ItemPrepareCompletedEventArgs>(_cPlayer_ItemPrepareCompleted);
				//_cPlayer.ItemStartCompleted += new EventHandler<services.ingenie.player.ItemStartCompletedEventArgs>(_cPlayer_ItemStartCompleted);
				//_cPlayer.ItemStopCompleted += new EventHandler<services.ingenie.player.ItemStopCompletedEventArgs>(_cPlayer_ItemStopCompleted);
				_cPlayer.ItemDeleteCompleted += new EventHandler<services.ingenie.player.ItemDeleteCompletedEventArgs>(_cPlayer_ItemDeleteCompleted);

				_cPlayer.ItemsUpdateCompleted += new EventHandler<services.ingenie.player.ItemsUpdateCompletedEventArgs>(_cPlayer_ItemsUpdateCompleted);
				_cPlayer.ItemsRunningGetCompleted += new EventHandler<services.ingenie.player.ItemsRunningGetCompletedEventArgs>(_cPlayer_ItemsRunningGetCompleted);

				_cPlayer.AddVideoCompleted += new EventHandler<AddVideoCompletedEventArgs>(_cPlayer_AddVideoCompleted);
				_cPlayer.PlaylistItemsGetCompleted += new EventHandler<services.ingenie.player.PlaylistItemsGetCompletedEventArgs>(_cPlayer_PlaylistItemsGetCompleted);
				_cPlayer.SkipCompleted += new EventHandler<SkipCompletedEventArgs>(_cPlayer_SkipCompleted);
				_cPlayer.PlaylistItemDeleteCompleted += new EventHandler<PlaylistItemDeleteCompletedEventArgs>(_cPlayer_PlaylistItemDeleteCompleted);

				_cPlayer.VideoFramesQtyGetCompleted += new EventHandler<VideoFramesQtyGetCompletedEventArgs>(_cPlayer_VideoFramesQtyGetCompleted);
				_cPlayer.FilesSCRGetCompleted += new EventHandler<FilesSCRGetCompletedEventArgs>(_cPlayer_FilesSCRGetCompleted);
				_cPlayer.AdvertsSCRGetCompleted += new EventHandler<AdvertsSCRGetCompletedEventArgs>(_cPlayer_AdvertsSCRGetCompleted);
				_cPlayer.StoragesSetCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cPlayer_StoragesSetCompleted);
				#endregion

				#region cues
				_cCues = new CuesSoapClient();
				_cCues.InitCompleted += new EventHandler<services.ingenie.cues.InitCompletedEventArgs>(_cCues_InitCompleted);
				_cCues.PingCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cCues_PingCompleted);
	
				_cCues.ItemCreateCompleted += new EventHandler<services.ingenie.cues.ItemCreateCompletedEventArgs>(_cCues_ItemCreateCompleted);
				_cCues.ItemPrepareCompleted += new EventHandler<services.ingenie.cues.ItemPrepareCompletedEventArgs>(_cCues_ItemPrepareCompleted);
				//_cCues.ItemStartCompleted += new EventHandler<services.ingenie.cues.ItemStartCompletedEventArgs>(_cCues_ItemStartCompleted);
				//_cCues.ItemStopCompleted += new EventHandler<services.ingenie.cues.ItemStopCompletedEventArgs>(_cCues_ItemStopCompleted);
				_cCues.ItemDeleteCompleted += new EventHandler<services.ingenie.cues.ItemDeleteCompletedEventArgs>(_cCues_ItemDeleteCompleted);

				_cCues.ItemsUpdateCompleted += new EventHandler<services.ingenie.cues.ItemsUpdateCompletedEventArgs>(_cCues_ItemsUpdateCompleted);
				_cCues.ItemsRunningGetCompleted += new EventHandler<services.ingenie.cues.ItemsRunningGetCompletedEventArgs>(_cCues_ItemsRunningGetCompleted);
				#endregion

				#region other
				_ui_dgLivePL.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgLivePL_LoadingRow);
				_ui_dgClipsSCR.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgClipsSCR_MouseLeftButtonDown), true);

				_dlgAdvChooser = new AdvertsBlockChooser(_cDBI, _cPlayer);
				_dlgAdvChooser.Closed += new EventHandler(dlgAdvChooser_Closed);

				_dlgFilesChooser = new FilesChooser(_cPlayer, _cCues);
				_dlgFilesChooser.Closed += new EventHandler(dlgFilesChooser_Closed);

				_cTimerForStatusGet = new System.Windows.Threading.DispatcherTimer();
				_cTimerForStatusGet.Tick += new EventHandler(ItemsUpdate);
				_cTimerForStatusGet.Interval = new System.TimeSpan(0, 0, 0, 0, 100);  // период проверки статуса темплейта
				_cTimerForPingDBI = new System.Windows.Threading.DispatcherTimer();
				_cTimerForPingDBI.Tick += new EventHandler(PingDBI);
				_cTimerForPingDBI.Interval = new System.TimeSpan(0, 0, 30);  // чтобы быть на связи с DBI
				_cTimerForPingPlayer = new System.Windows.Threading.DispatcherTimer();
				_cTimerForPingPlayer.Tick += new EventHandler(PingPlayer);
				_cTimerForPingPlayer.Interval = new System.TimeSpan(0, 0, 5);  // чтобы быть на связи с плеером
				_cTimerForPingCues = new System.Windows.Threading.DispatcherTimer();
				_cTimerForPingCues.Tick += new EventHandler(PingCues);
				_cTimerForPingCues.Interval = _cTimerForPingPlayer.Interval;  // чтобы быть на связи с cues
				_cTimerForRefreshAfterSkip = new System.Windows.Threading.DispatcherTimer();
				_cTimerForRefreshAfterSkip.Tick += new EventHandler(PlaylistItemsGetAfterDelay);
				_cTimerForRefreshAfterSkip.Interval = new System.TimeSpan(0, 0, 3);  // чтобы быть на связи с IG

				services.preferences.Template cTemplate = App.cPreferences.aTemplates.Get(Bind.channel_credits);
				_ui_ctrTB_Credits.sFile = cTemplate.sFile;
				_ui_ctrTB_Credits.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Credits.TemplateStart += ItemStart;
				_ui_ctrTB_Credits.TemplateStop += ItemStop;
				_ui_ctrTB_Credits.TemplateDrop += ItemDelete;

				cTemplate = App.cPreferences.aTemplates.Get(Bind.channel_logo);
				_ui_ctrTB_Logo.sFile = cTemplate.sFile;
				_ui_ctrTB_Logo.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Logo.TemplateStop += ItemStop;
				_ui_ctrTB_Logo.TemplateStart += ItemStart;
				_ui_ctrTB_Logo.TemplateDrop += ItemDelete;

				cTemplate = App.cPreferences.aTemplates.Get(Bind.channel_user1);
				_ui_ctrTB_Channel1.sFile = cTemplate.sFile;
				_ui_ctrTB_Channel1.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Channel1.TemplateStart += ItemStart;
				_ui_ctrTB_Channel1.TemplateStop += ItemStop;
				_ui_ctrTB_Channel1.TemplateDrop += ItemDelete;

				cTemplate = App.cPreferences.aTemplates.Get(Bind.channel_user2);
				_ui_ctrTB_Channel2.sFile = cTemplate.sFile;
				_ui_ctrTB_Channel2.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Channel2.TemplateStart += ItemStart;
				_ui_ctrTB_Channel2.TemplateStop += ItemStop;
				_ui_ctrTB_Channel2.TemplateDrop += ItemDelete;

				cTemplate = App.cPreferences.aTemplates.Get(Bind.channel_user3);
				_ui_ctrTB_Channel3.sFile = cTemplate.sFile;
				_ui_ctrTB_Channel3.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Channel3.TemplateStart += ItemStart;
				_ui_ctrTB_Channel3.TemplateStop += ItemStop;
				_ui_ctrTB_Channel3.TemplateDrop += ItemDelete;

				cTemplate = App.cPreferences.aTemplates.Get(Bind.channel_user4);
				_ui_ctrTB_Channel4.sFile = cTemplate.sFile;
				_ui_ctrTB_Channel4.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Channel4.TemplateStart += ItemStart;
				_ui_ctrTB_Channel4.TemplateStop += ItemStop;
				_ui_ctrTB_Channel4.TemplateDrop += ItemDelete;

				_ui_ctrTB_PlayList.sFile = App.cPreferences.aTemplates.Get(Bind.playlist).sFile;
				_ui_ctrTB_PlayList.TemplatePrepare += PlayListPrepare;
				_ui_ctrTB_PlayList.TemplateSkip += PlayListSkip;
				_ui_ctrTB_PlayList.TemplateStart += ItemStart;
				_ui_ctrTB_PlayList.TemplateStop += ItemStop;
				_ui_ctrTB_PlayList.TemplateStarted += _ui_ctrTB_PlayList_TemplateStarted;
				_ui_ctrTB_PlayList.TemplateStopped += _ui_ctrTB_PlayList_TemplateStopped;
				_ui_ctrTB_PlayList.TemplateDrop += ItemDelete;
				_ui_ctrTB_Template1Logo.sFile = App.cPreferences.aTemplates.Get(Bind.preset_logo).sFile;
				_ui_ctrTB_Template1Logo.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Template1Logo.TemplateStart += ItemStart;
				_ui_ctrTB_Template1Logo.TemplateStop += ItemStop;
				_ui_ctrTB_Template1Logo.TemplateDrop += ItemDelete;
				_ui_ctrTB_Template1Logo.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_Template1Chat.sFile = App.cPreferences.aTemplates.Get(Bind.channel_chat).sFile;
				_ui_ctrTB_Template1Chat.TemplatePrepare += ItemPrepare;   // ChatPrepare
				_ui_ctrTB_Template1Chat.TemplateStart += ItemStart;     // EffectStart
				_ui_ctrTB_Template1Chat.TemplateStop += ItemStop;   // EffectStop
				_ui_ctrTB_Template1Chat.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_Template1Chat.TemplateDrop += ItemDelete;
				_ui_ctrTB_Template1Credits.sFile = App.cPreferences.aTemplates.Get(Bind.preset_credits).sFile;
				_ui_ctrTB_Template1Credits.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Template1Credits.TemplateStart += ItemStart;
				_ui_ctrTB_Template1Credits.TemplateStop += ItemStop;
				_ui_ctrTB_Template1Credits.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_Template1Credits.TemplateDrop += ItemDelete;
				_ui_ctrTB_Template1Bumper.sFile = App.cPreferences.aTemplates.Get(Bind.preset_bumper).sFile;
				_ui_ctrTB_Template1Bumper.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Template1Bumper.TemplateStart += ItemStart;
				_ui_ctrTB_Template1Bumper.TemplateStop += ItemStop;
				_ui_ctrTB_Template1Bumper.TemplateDrop += ItemDelete;
				_ui_ctrTB_Template1Bumper.eFirstAction = TemplateButton.FirstAction.Start;

				_ui_ctrTB_TemplateUser1.sFile = App.cPreferences.aTemplates.Get(Bind.preset_user1).sFile;
				_ui_ctrTB_TemplateUser1.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_TemplateUser1.TemplateStart += ItemStart;
				_ui_ctrTB_TemplateUser1.TemplateStop += ItemStop;
				_ui_ctrTB_TemplateUser1.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_TemplateUser1.TemplateDrop += ItemDelete;

				_ui_ctrTB_TemplateUser2.sFile = App.cPreferences.aTemplates.Get(Bind.preset_user2).sFile;
				_ui_ctrTB_TemplateUser2.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_TemplateUser2.TemplateStart += ItemStart;
				_ui_ctrTB_TemplateUser2.TemplateStop += ItemStop;
				_ui_ctrTB_TemplateUser2.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_TemplateUser2.TemplateDrop += ItemDelete;

				_ui_ctrTB_TemplateUser3.sFile = App.cPreferences.aTemplates.Get(Bind.preset_user3).sFile;
				_ui_ctrTB_TemplateUser3.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_TemplateUser3.TemplateStart += ItemStart;
				_ui_ctrTB_TemplateUser3.TemplateStop += ItemStop;
				_ui_ctrTB_TemplateUser3.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_TemplateUser3.TemplateDrop += ItemDelete;

				_ui_ctrTB_TemplateUser4.sFile = App.cPreferences.aTemplates.Get(Bind.preset_user4).sFile;
				_ui_ctrTB_TemplateUser4.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_TemplateUser4.TemplateStart += ItemStart;
				_ui_ctrTB_TemplateUser4.TemplateStop += ItemStop;
				_ui_ctrTB_TemplateUser4.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_TemplateUser4.TemplateDrop += ItemDelete;

				_ui_ctrTB_Template1Plaques.sFile = App.cPreferences.aTemplates.Get(Bind.preset_credits).sFile;
				_ui_ctrTB_Template1Plaques.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_Template1Plaques.TemplateStart += ItemStart;
				_ui_ctrTB_Template1Plaques.TemplateStop += ItemStop;
				_ui_ctrTB_Template1Plaques.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_Template1Plaques.TemplateDrop += ItemDelete;

				_ui_ctrTB_TemplateSequence.sFile = App.cPreferences.aTemplates.Get(Bind.preset_sequence).sFile;
				_ui_ctrTB_TemplateSequence.TemplatePrepare += ItemPrepare;
				_ui_ctrTB_TemplateSequence.TemplateStart += ItemStart;
				_ui_ctrTB_TemplateSequence.TemplateStop += ItemStop;
				_ui_ctrTB_TemplateSequence.eFirstAction = TemplateButton.FirstAction.Start;
				_ui_ctrTB_TemplateSequence.TemplateDrop += ItemDelete;
				_ui_ctrTB_TemplateSequence.IsEnabled = false;
				_ui_lbSequenceDirectory.Content = g.SCR.sNoSequence.ToLower();

				_ui_tbChoosedString1.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_tbChoozed_KeyDown), true);
				_ui_tbChoosedString2.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_tbChoozed_KeyDown), true);
				_ui_dgPlaques.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_tbChoozed_KeyDown), true);

                _ui_rpClips.IsOpenChanged += _ui_rpClips_IsOpenChanged;
				_ui_rpClips.IsEnabled = false;
				_bClipsGot = false;
				_ui_rpAir.IsOpenChanged += _ui_rpAir_IsOpenChanged;
				_ui_rpTimer.IsOpenChanged += _ui_rpTimer_IsOpenChanged;
				_ui_rpCommon.IsOpen = true;
				_ui_rpAir.IsOpen = true;
				_ui_rpTimer.IsOpen = true;
				_ui_rpPlaylist.IsOpen = true;

				_ui_hlbtnAddClip.IsEnabled = false;
				_ui_ctrTB_Credits.IsEnabled = false;   
				_ui_ctrTB_Template1Credits.IsEnabled = false;   // нечего на нее жать... 

				_ui_Search.sCaption = g.SCR.sNotice1;
				_ui_Search.ItemAdd = null;
				_ui_Search.nGap2nd = 4;
				_ui_Search.sDisplayMemberPath = "sName";
				//_ui_Search.cElementType = typeof(scr.services.ingenie.player.Clip);
				#endregion


				ParametersSet(_ui_ctrTB_Credits, Bind.channel_credits);
				ParametersSet(_ui_ctrTB_Logo, Bind.channel_logo);
				ParametersSet(_ui_ctrTB_Channel1, Bind.channel_user1);
				ParametersSet(_ui_ctrTB_Channel2, Bind.channel_user2);
				ParametersSet(_ui_ctrTB_Channel3, Bind.channel_user3);
				ParametersSet(_ui_ctrTB_Channel4, Bind.channel_user4);

				_cDBI.InitAsync("user", "");
				_dlgProgress.sInfo = g.SCR.sNotice2;

				_sLogTB = helpers.sl.common.CookieGet("logTB");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

        void _ui_rpTimer_IsOpenChanged(object sender, EventArgs e)
		{
            _ui_rpAir.IsOpen = _ui_rpTimer.IsOpen;
		}
        void _ui_rpAir_IsOpenChanged(object sender, EventArgs e)
		{
			_ui_rpTimer.IsOpen = _ui_rpAir.IsOpen;
		}
		protected override void OnNavigatedTo(NavigationEventArgs e)    // Executes when the user navigates to this page.
		{
			//_ui_ctrTB_PlayList.btnPlay.IsEnabled = false;
		}

		#region ui
		#endregion

		#region services
		#region dbi
		void _cDBI_InitCompleted(object sender, helpers.replica.services.dbinteract.InitCompletedEventArgs e)
		{
			try
			{
				if (e.Result)
				{
					string sWorkstationID = common.CookieGet("this_comp_id");
					if (null == sWorkstationID || "" == sWorkstationID)
					{
						_nWorkstationID = DateTime.Now.Subtract(new DateTime(2011, 1, 1)).TotalMilliseconds.ToULong();
						common.CookieSet("this_comp_id", _nWorkstationID.ToString());
					}
					else
						_nWorkstationID = ulong.Parse(sWorkstationID);
					_cPlayer.InitAsync(_nWorkstationID);
					_dlgProgress.sInfo = g.SCR.sNotice3;
					_cTimerForPingDBI.Start();
				}
				else
					_dlgMsgBox.ShowError(g.SCR.sError1);
			}
			catch (Exception ex)
			{
				_dlgMsgBox.ShowError(g.SCR.sError1 + "\n\n", ex);
			}
		}
		void _cDBI_ShiftCurrentGetCompleted(object sender, ShiftCurrentGetCompletedEventArgs e)
		{
			_cShiftCurrent = null;

			Shift cShift = e.Result;

#if DEBUG
			cShift = null;
#endif

			if (null != cShift)
			{
				_cShiftCurrent = cShift;
				MsgBox dlgIsItYouShift = new MsgBox();
				dlgIsItYouShift.Closed += new EventHandler(dlgIsItYouShift_Closed);
				dlgIsItYouShift.Show(g.SCR.sNotice41.Fmt(Environment.NewLine), g.Common.sAttention.ToUpper() + "!", MsgBox.MsgBoxButton.OKCancel, cShift.cPreset.sName + " " + "(id = " + cShift.cPreset.nID + ")");
				return;
			}
			else if (_aItems.Length > 0)
			{
				Preset cPreset;
				foreach (TabItem ui in _ui_tcAirTemplates.Items.Select(o => (TabItem)o).Where(o => o.Name != "default"))
				{
					if (null != ui.Tag && ui.Tag is Preset)
					{
						cPreset = (Preset)ui.Tag;
						foreach (Item cItem in _aItems)
						{
							if (cPreset.sName == cItem.sPreset)
							{
								_ui_tcAirTemplates.SelectedItem = ui;
								cPreset = null;
								break;
							}
						}
						if (null == cPreset)
							break;
					}
				}
			}
			dlgIsItYouShift_Closed(null, null);
		}
		void dlgIsItYouShift_Closed(object sender, EventArgs e)
		{
			if (null != sender)
			{
				((MsgBox)sender).Closed -= dlgIsItYouShift_Closed;
				if (((MsgBox)sender).enMsgResult == MsgBox.MsgBoxButton.OK)
				{
					_ui_tcAirTemplates.SelectedItem = _ui_tcAirTemplates.Items.FirstOrDefault(o => null != o && ((Preset)((TabItem)o).Tag).sName == _cShiftCurrent.cPreset.sName);
					_ui_btnStartAirText1.Text = g.SCR.sNotice4.ToUpper();
					_ui_btnStartAirText1.Foreground = Coloring.SCR.Timer.cStopBtnTextForegr;
					_ui_btnStartAirText2.Foreground = Coloring.SCR.Timer.cStopBtnTextForegr;
				}
				else
					_cShiftCurrent = null;
			}
			if (null != _cPlaylist)
			{
				//lock(_aTemplateButtons)
				//    _ui_ctrTB_PlayList.cItem = null;
				_cPlayer.PlaylistItemsGetAsync(_cPlaylist);
			}
			else if (1 > _aItems.Length)
				_dlgProgress.Close();
			_cTimerForStatusGet.Start();
		}
		void _cDBI_PingCompleted(object sender, PingCompletedEventArgs e)
		{
			try
			{
                if (null != e.Error)
                    throw e.Error;
                if (!e.Result)  //TODO переделать на таймеры (если ответа нет за опред. время и т.п.)
					_dlgMsgBox.ShowError(g.SCR.sError2.Fmt(Environment.NewLine));
			}
			catch (Exception ex)
			{
				_dlgMsgBox.ShowError(g.SCR.sError3.Fmt(Environment.NewLine), ex);
			}
			_cTimerForPingDBI.Start();
		}
		void _cDBI_CuesGetCompleted(object sender, CuesGetCompletedEventArgs e)
		{
			try
			{
				if (null != e.Result && 0 < e.Result.nID)
				{
					UserReplacement[] ahUserStrings = new UserReplacement[2];
					ahUserStrings[0] = new UserReplacement() { sKey = "ARTIST", sValue = e.Result.sArtist }; //PREFERENCES
					ahUserStrings[1] = new UserReplacement() { sKey = "SONG", sValue = e.Result.sSong }; //PREFERENCES
					_cCues.ItemCreateAsync(_cPresetSelected.sName, PathApprove(((TemplateButton)e.UserState).sFile, _cPresetSelected), ahUserStrings, e.UserState);
				}
				else
					_dlgMsgBox.ShowError(g.SCR.sError4);
			}
			catch (Exception ex)
			{
                _dlgMsgBox.ShowError(g.SCR.sError5, ex);
			}
		}
		void _cDBI_StorageSCRGetCompleted(object sender, StorageSCRGetCompletedEventArgs e)
		{
			try
			{
				List<IP.IdNamePair> aRetVal = new List<IP.IdNamePair>();
				if (null != e.Result)
					foreach (StoragesMappings cSM in e.Result)
						aRetVal.Add(new IP.IdNamePair() { nID = cSM.nID, sName = cSM.sLocalPath });
				_aStorages = aRetVal;
				_cPlayer.StoragesSetAsync(aRetVal.ToArray());
			}
			catch (Exception ex)
			{
				_dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:StorageSCRGet)", ex);
			}
		}
		void _cDBI_PlaqueAddCompleted(object sender, PlaqueAddCompletedEventArgs e)
		{
			if (1 > e.Result)
			{
                _dlgMsgBox.ShowError(g.SCR.sError6);
			}
			else
			{
				List<Plaque> aPlaques = (List<Plaque>)_ui_dgPlaques.Tag;
				((Plaque)e.UserState).nID = e.Result;
				aPlaques.Add((Plaque)e.UserState);
				aPlaques.Sort(new PlaquesCompare());
				SetGroupsNumbers(aPlaques);
				PlaqueGridRefresh(aPlaques);
			}
			PlaquesBlock(false);
		}
		void _cDBI_PlaqueDeleteCompleted(object sender, PlaqueDeleteCompletedEventArgs e)
		{
			if (!e.Result)
			{
				_dlgMsgBox.ShowError(g.SCR.sError7);
			}
			else
			{
				List<Plaque> aPlaques = (List<Plaque>)_ui_dgPlaques.Tag;
				aPlaques.Remove(aPlaques.FirstOrDefault(o => o.nID == (long)e.UserState));
				SetGroupsNumbers(aPlaques);
				PlaqueGridRefresh(aPlaques);
			}
			PlaquesBlock(false);
		}
		void _cDBI_PlaquesGetCompleted(object sender, PlaquesGetCompletedEventArgs e)
		{
			List<Plaque> aPlaques = e.Result.Select(o => (Plaque)o).ToList();
			aPlaques.Sort(new PlaquesCompare());
			SetGroupsNumbers(aPlaques);
			PlaqueGridRefresh(aPlaques);
			PlaquesBlock(false);
		}
		void _cDBI_PlaqueChangeCompleted(object sender, PlaqueChangeCompletedEventArgs e)
		{
			if (e.Result)
			{
				List<Plaque> aPlaques = (List<Plaque>)_ui_dgPlaques.ItemsSource;
				Plaque cPlaque = aPlaques.FirstOrDefault(o => o.nID == (long)e.UserState);
				Plaque cPlaqueTaged = ((List<Plaque>)_ui_dgPlaques.Tag).FirstOrDefault(o => o.nID == (long)e.UserState);
				if (null != cPlaqueTaged && null != cPlaque)
				{
					cPlaqueTaged.sFirstLine = cPlaque.sFirstLine;
					cPlaqueTaged.sSecondLine = cPlaque.sSecondLine;
					if (cPlaqueTaged.sName != cPlaque.sName)
					{
						cPlaqueTaged.sName = cPlaque.sName;
						aPlaques.Sort(new PlaquesCompare());
						SetGroupsNumbers(aPlaques);
						PlaqueGridRefresh(aPlaques);
					}
				}
			}
			else
				_dlgMsgBox.ShowError(g.SCR.sError8.Fmt(Environment.NewLine));
			PlaquesBlock(false);
		}
		void _cDBI_ShiftAddCompleted(object sender, ShiftAddCompletedEventArgs e)
		{
			if (null != e.Result)
				_cDBI.ShiftStartAsync(e.Result, e.Result);
			else
				_ui_btnStartAirShowError();
		}
		void _cDBI_ShiftStartCompleted(object sender, ShiftStartCompletedEventArgs e)
		{
//sh
			if (_ui_cbFalseStart.IsChecked.Value)
			{
				Shift cSh = new Shift() { cPreset = new DBI.IdNamePair() { nID = 1, sName = "orderdesk" }, dt = DateTime.Now, dtStart = DateTime.Now, dtStop = DateTime.MaxValue, nID = int.MaxValue, sSubject = "" };
				StartAir(cSh);
				_cPlayer.ShiftStartAsync(_cShiftCurrent.nID, _cShiftCurrent.sSubject, _cPresetSelected.nID, _cPresetSelected.sName);
				_ui_btnStartAirEnabling();
				return;
			}
//sh


			if (null != e.UserState)   // т.е. получили новый shiftID 
			{
				StartAir((Shift)e.UserState);
				_cPlayer.ShiftStartAsync(_cShiftCurrent.nID, _cShiftCurrent.sSubject, _cPresetSelected.nID, _cPresetSelected.sName);
			}
			else
				_ui_btnStartAirShowError();
			_ui_btnStartAirEnabling();
		}
		void _cDBI_ShiftStopCompleted(object sender, ShiftStopCompletedEventArgs e)
		{
			_ui_btnStartAirEnabling();
			if (null != e && !e.Result)
				_ui_btnStartAirShowError();
			_cPlayer.ShiftStopAsync();
			if (true == _ui_cbFalseStart.IsChecked)
				_ui_cbFalseStart.IsChecked = false;
		}
		void _cDBI_NearestAdvertsBlockCompleted(object sender, NearestAdvertsBlockCompletedEventArgs e)
		{
			int nMinutes;
			try
			{
				if (null != _cTimerForNearestAdvBlock)
					_cTimerForNearestAdvBlock.Stop();
				_cTimerForNearestAdvBlock = new System.Windows.Threading.DispatcherTimer();
				_cTimerForNearestAdvBlock.Tick += new EventHandler(NearestAdvBlockCountdown);
				_cTimerForNearestAdvBlock.Interval = new System.TimeSpan(0, 0, 0, 1);
				_cTimerForNearestAdvBlock.Start();
				dtNearestAdvBlock = e.Result;             // = e.Result;   = DateTime.Now.AddSeconds(10); 
				if (150 <= (nMinutes = (int)dtNearestAdvBlock.Subtract(DateTime.Now).TotalMinutes))
					dtNextRefresh = DateTime.Now.AddMinutes(1);
				else if (12 <= nMinutes)
					dtNextRefresh = DateTime.Now.AddMinutes(10);
				else if (2 < nMinutes)
					dtNextRefresh = DateTime.Now.AddMinutes(nMinutes - 1);
				else
					dtNextRefresh = DateTime.Now.AddMinutes(5);
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
				//_dlgMsgBox.ShowError(g.SCR.sError9);
			}
		}
		void _cDBI_ClipsBDLogCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
            if (null != e.Error)
                _cCues.WriteErrorAsync(g.SCR.sError10 + ": " + e.Error.Message + "<br>" + e.Error.StackTrace + "<br>" + e.Error.InnerException);
		}
		#endregion

		#region player
		public void WritePlayerError(Exception ex)
		{
			_cPlayer.WriteErrorAsync("player: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.InnerException);
		}
		void _cPlayer_InitCompleted(object sender, services.ingenie.player.InitCompletedEventArgs e)
		{
			try
			{
				if ("" == e.Result)
				{
					_dlgProgress.sInfo = g.SCR.sNotice5;
					_cDBI.StorageSCRGetAsync();
				}
				else
					_dlgMsgBox.ShowError(e.Result); // e.Result
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
				_dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:init)", ex);
			}
		}
		void _cPlayer_StoragesSetCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				_ui_rpClips_IsOpenChanged(null, null);  // теперь в плеере прописаны стораджи и мы можем грузить клипы

				_dlgProgress.sInfo = g.SCR.sNotice6;
				_cCues.InitAsync(_nWorkstationID);
				_cTimerForPingDBI.Start();
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:storages:set)", ex);
			}
		}
		void _cPlayer_PingCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			_cTimerForPingPlayer.Start();
		}

		void _cPlayer_ItemCreateCompleted(object sender, services.ingenie.player.ItemCreateCompletedEventArgs e)
		{
			try
			{
				if (null != e.UserState && (e.UserState is TemplateButton))
					_cCues.WriteNoticeAsync("_cPlayer_ItemCreateCompleted: begin: [file = " + ((TemplateButton)e.UserState).sFile + "]");

				if (null == e.Result)
					throw new Exception(g.SCR.sError11);
				if (null == e.UserState || !(e.UserState is TemplateButton))
					throw new Exception(g.SCR.sError12);
				((TemplateButton)e.UserState).cItem = (Item)e.Result;
				_cPlayer.ItemPrepareAsync(e.Result, e.UserState);
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
				_dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:create)", ex);
			}
		}
		void _cPlayer_ItemPrepareCompleted(object sender, services.ingenie.player.ItemPrepareCompletedEventArgs e)
		{
			try
			{
				if (null != e.UserState && (e.UserState is TemplateButton))
					_cCues.WriteNoticeAsync("_cPlayer_ItemPrepareCompleted: begin: [file = " + ((TemplateButton)e.UserState).sFile + "]");

				if (!e.Result)
					throw new Exception(g.SCR.sError13);
				if (null == e.UserState || !(e.UserState is TemplateButton))
					throw new Exception(g.SCR.sError12);
				if ((TemplateButton)e.UserState == _ui_ctrTB_PlayList)
					_cPlayer.PlaylistItemsGetAsync((Item)_ui_ctrTB_PlayList.cItem, "PL_prepared");
				else
					ItemPrepareCompleted(e.UserState, null);
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:create)", ex);
			}
		}
		void _cPlayer_ItemDeleteCompleted(object sender, services.ingenie.player.ItemDeleteCompletedEventArgs e)
		{
			try
			{
				((TemplateButton)e.UserState).eStatus = (e.Result ? TemplateButton.Status.Idle : TemplateButton.Status.Error);
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:item:delete)", ex);
			}
		}

		void _cPlayer_ItemsUpdateCompleted(object sender, services.ingenie.player.ItemsUpdateCompletedEventArgs e)
		{
			try
			{
				if (null == e.Result)
					throw new Exception(g.SCR.sError14);
				else if (1 < e.Result.Length)
					throw new Exception(g.SCR.sError15);
				else if (1 > e.Result.Length)
					throw new Exception(g.SCR.sError16);

				if ("design_with_dtmf_in" == e.Result[0].sCurrentClass && !_aDTMFedItemsIDs.Contains(e.Result[0].nID))
				{
					_cCues.StartDTMFAsync(PathApprove(App.cPreferences.aTemplates.Get(Bind.dtmf_in).sFile, _cPresetSelected));
					_aDTMFedItemsIDs.Add(e.Result[0].nID);
				}
				else if ("design_with_dtmf_out" == e.Result[0].sCurrentClass && !_aDTMFedItemsIDs.Contains(e.Result[0].nID))
				{
					_cCues.StartDTMFAsync(PathApprove(App.cPreferences.aTemplates.Get(Bind.dtmf_out).sFile, _cPresetSelected));
					_aDTMFedItemsIDs.Add(e.Result[0].nID);
				}

				ItemsUpdate(e.Result.Translate());

				IC.Item[] aItems = _aCuesItems.Select(o => (IC.Item)o).ToArray();    //.Where(o => null != _cPlaylist && _cPlaylist.nID != o.nID || null == _cPlaylist).Select(o => (IC.Item)o).ToArray()
				if(0 < aItems.Length)
					_cCues.ItemsUpdateAsync(aItems);
				else
					_cTimerForStatusGet.Start();
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
				_dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:items:update)", ex);
			}
		}
		void _cPlayer_ItemsRunningGetCompleted(object sender, services.ingenie.player.ItemsRunningGetCompletedEventArgs e)
		{
			try
			{
				if (null != e.Result)
				{
					lock (_aTemplateButtons)
					{
						foreach (Item cItem in e.Result)
						{
							if (GetTemplateButton(cItem.sInfo) == _ui_ctrTB_PlayList)
							{
								_ui_ctrTB_PlayList.cItem = (Item)e.Result[0];
								break;
							}
						}
					}
				}
				_dlgProgress.sInfo = g.SCR.sNotice7;
				_cCues.ItemsRunningGetAsync();
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:items:running)", ex);
			}
		}

		void _cPlayer_AddVideoCompleted(object sender, AddVideoCompletedEventArgs e)
		{
			try
			{
				List<LivePLItem> aUserState = (List<LivePLItem>)e.UserState;
				if (null == e.Result)
				{
					_cPlayer.PlaylistItemsGetAsync((Item)_ui_ctrTB_PlayList.cItem);
					_dlgMsgBox.ShowError(g.Common.sErrorAdd);
				}
				else
				{
					for (int ni = 0; ni < e.Result.Length; ni++)
					{
						aUserState[ni].nAtomHashCode = e.Result[ni].nAtomHashCode;
						aUserState[ni].nEffectID = e.Result[ni].nEffectID;
					}
						_aLivePLTotal.AddRange(aUserState);
				}
				ShowPL();
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:add:video)", ex);
			}
		}
		void _cPlayer_PlaylistItemsGetCompleted(object sender, services.ingenie.player.PlaylistItemsGetCompletedEventArgs e)
		{
			_cCues.WriteNoticeAsync("_cPlayer_PlaylistItemsGetCompleted: begin");
			try
			{
				//if (null==_cLPLWatcher)
				//	_dlgProgress.Close();
				_ui_ctrTB_PlayList.IsEnabled = true;
				if (null == e.Result || 0 == e.Result.Length)
					return;
				if (e.UserState == "PL_stopped")
				{
					if (IsAirGoingNow && !_ui_cbFalseStart.IsChecked.Value && null != e.Result)
						ClipsLog(e.Result.ToList());
					return;
				}
				lock (_aLivePLTotal)
				{
					_aLivePLTotal = new List<LivePLItem>();
					LivePLItem cTMP;  //, cCurrent = null;
					foreach (IP.PlaylistItem cPLI in e.Result)
					{
						cTMP = LivePLItem.LivePLItemGet(cPLI);
						if (null != _cLPLWatcher && null != _cLPLWatcher.cCurrentLPLItem && _cLPLWatcher.cCurrentLPLItem.nID == cTMP.nID)
							_cLPLWatcher.cCurrentLPLItem = cTMP;
						if (null != cTMP)
							_aLivePLTotal.Add(cTMP);
					}
					ShowPL();
				}
				if (e.UserState == "PL_prepared")
					ItemPrepareCompleted(_ui_ctrTB_PlayList, null);
				//if (null == _ui_ctrTB_PlayList.cItem)
				//{
				//    lock (_aTemplateButtons)
				//    {
				//        _ui_ctrTB_PlayList.cItem = 
				//        _aTemplateButtons.Add(_ui_ctrTB_PlayList.cItem, _ui_ctrTB_PlayList);  // это вызовет старт этого темплейта, когда статус будет получен из БД... //EMERGENCY:l что значит этот твой комментарий? в БД нет статусов Item'ов, как и самих Item'ов
				//        _cPlaylist.eStatusPrevious = TemplateButton.Status.Idle;
				//    }
				//}
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:plis:get)", ex);
			}
		}
		void _cPlayer_SkipCompleted(object sender, SkipCompletedEventArgs e)
		{
			try
			{
				((TemplateButton)e.UserState).bSkipResult = e.Result;
				if (e.Result)
					_cTimerForRefreshAfterSkip.Start();
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:skip)", ex);
			}
		}
		void _cPlayer_PlaylistItemDeleteCompleted(object sender, PlaylistItemDeleteCompletedEventArgs e)
		{
			try
			{
				if (!e.Result)
				{
					_dlgMsgBox.ShowError(g.SCR.sError17);
					_ui_ctrTB_PlayList.IsEnabled = true;
				}
				else
					PlaylistItemsGetAfterDelay(null, null);
			}
			catch (Exception ex)
			{
				WritePlayerError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (player:pli:delete)", ex);
			}
		}

		void _cPlayer_VideoFramesQtyGetCompleted(object sender, VideoFramesQtyGetCompletedEventArgs e)
		{
		}
		void _cPlayer_FilesSCRGetCompleted(object sender, FilesSCRGetCompletedEventArgs e)
		{
		}
		void _cPlayer_AdvertsSCRGetCompleted(object sender, AdvertsSCRGetCompletedEventArgs e)
		{
		}
		#endregion

		#region cues
		void WriteCuesError(Exception ex)
		{
			_cCues.WriteErrorAsync("cues: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.InnerException);
		}
		void _cCues_InitCompleted(object sender, services.ingenie.cues.InitCompletedEventArgs e)
		{
			try
			{
				if ("" == e.Result)   // == ""
				{
					_cTimerForPingPlayer.Start();
					_cTimerForPingCues.Start();
					_cPlayer.ItemsRunningGetAsync();
					_dlgProgress.sInfo = g.SCR.sNotice8;
				}
				else
					_dlgMsgBox.ShowError(e.Result); // e.Result
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
				_dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (cues:init)", ex);
			}
		}
		void _cCues_PingCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			//TODO переделать на таймеры (если ответа нет за опред. время и т.п.)
			_cTimerForPingCues.Start();
		}

		void _cCues_ItemCreateCompleted(object sender, services.ingenie.cues.ItemCreateCompletedEventArgs e)
		{
			try
			{
				object oUserState = e.UserState;
				if (null != oUserState)
				{
					if (e.UserState is TemplateButton)
					{
						lock(_aTemplateButtons)
							((TemplateButton)oUserState).cItem = (Item)e.Result;
					}
					else
						throw new Exception("if(null != e.UserState && !(e.UserState is TemplateButton))"); //UNDONE
				}
				else
				{// взмолаживание чата, иначе тормоза при первом запуске.
					oUserState = (Item)e.Result;
					_dlgProgress.sInfo = g.SCR.sNotice9;
				}
				_cCues.ItemPrepareAsync(e.Result, oUserState);
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (cues:item:create)", ex);
			}
		}
		void _cCues_ItemPrepareCompleted(object sender, services.ingenie.cues.ItemPrepareCompletedEventArgs e)
		{
			try
			{
				if (!e.Result)
					throw new Exception(g.SCR.sError18);
				if (null != e.UserState) //взмолаживание чата... иначе тут должен быть экземпляр TemplateButton
				{
					if (e.UserState is Item) // взмолаживание чата, иначе тормоза при первом запуске.
						_cCues.ItemDeleteAsync((Item)e.UserState);
					else if (e.UserState is TemplateButton)
						ItemPrepareCompleted(e.UserState, null);
					else
						throw new Exception("!(e.UserState is TemplateButton)");
				}
				else
					throw new Exception("null == e.UserState");
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (cues:item:prepare)", ex);
			}
		}
		void _cCues_ItemDeleteCompleted(object sender, services.ingenie.cues.ItemDeleteCompletedEventArgs e)
		{
			try
			{
				if(null != e.UserState && e.UserState is TemplateButton)
					((TemplateButton)e.UserState).eStatus = (e.Result ? TemplateButton.Status.Idle : TemplateButton.Status.Error);
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (cues:item:delete)", ex);
			}
		}

		void _cCues_ItemsUpdateCompleted(object sender, services.ingenie.cues.ItemsUpdateCompletedEventArgs e)
		{
			try
			{
				ItemsUpdate(e.Result.Translate());
				_cTimerForStatusGet.Start();
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (cues:items:update)", ex);
			}
		}
		void _cCues_ItemsRunningGetCompleted(object sender, services.ingenie.cues.ItemsRunningGetCompletedEventArgs e)
		{
			try
			{
				if (null != e.Result)
				{
					lock (_aTemplateButtons)
					{
						foreach (Item cItem in e.Result)
						{
							TemplateButton cTB = GetTemplateButton(cItem.sInfo.Substring(10));
							if (null != cTB)
							{
								lock (_aTemplateButtons)
									cTB.cItem = cItem;
								if (cTB != _ui_ctrTB_PlayList)
								{
									cItem.eStatusPrevious = TemplateButton.Status.Idle;
									lock (_aTemplateButtons)
										cTB.cItem = cItem; //EMERGENCY:l ты уверен, что это нужно? у тебя повторное присвоение... пятью строчками выше было такое же)
								}
							}
						}
					}
				}
				if (1 > _aItems.Length)
				{
					// взмолаживание чата, иначе тормоза при первом запуске.
					if(0 < App.cPreferences.aTemplates.Get(Bind.channel_chat).aParameters.Count(o => o.bIsVisible))
						_cCues.ItemCreateAsync(_cPresetSelected.sName, PathApprove(_ui_ctrTB_Template1Chat.sFile, _cPresetSelected), null);
				}
				_cDBI.ShiftCurrentGetAsync();
				_dlgProgress.sInfo = g.SCR.sNotice10;
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
                _dlgMsgBox.ShowError(g.SCR.sErrorWebservice + " (cues:items:running)", ex);
			}
		}
		#endregion
		#endregion

		#region pings&updates
		private void PingDBI(object s, EventArgs args)
		{
			_cTimerForPingDBI.Stop();
			_cDBI.PingAsync();
		}
		private void PingPlayer(object sender, EventArgs e)
		{
			if (null == _cPlaylist)
			{
				_cTimerForPingPlayer.Stop();
				_cPlayer.PingAsync();
			}
		}
		private void PingCues(object sender, EventArgs e)
		{
			if (1 > _aCuesItems.Length)   //(o => null != _cPlaylist && _cPlaylist.nID != o.nID || null == _cPlaylist))
			{
				_cTimerForPingCues.Stop();
				_cCues.PingAsync();
			}
		}
		private void ItemsUpdate(object s, EventArgs args)
		{
			if (1 > _aItems.Length)
				return;
			_cTimerForStatusGet.Stop();
			if (null == _cPlaylist)
				_cCues.ItemsUpdateAsync(_aCuesItems.Select(o => (IC.Item)o).ToArray());
			else
				_cPlayer.ItemsUpdateAsync(new IP.Item[] { _cPlaylist });
		}
		#endregion
		#region . common .
		private void ItemPrepare(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			_cCues.WriteNoticeAsync("ItemPrepare: begin: [file = " + ui_tplb.sFile + "][pressed_by_user = " + ui_tplb.bPressedByUser + " ]");
			List<UserReplacement> ahUserStrings = new List<UserReplacement>();
			long nAssetsID = 0;
			string sFile = ui_tplb.sFile;

			#region - sequence
			if (_ui_ctrTB_TemplateSequence == ui_tplb)
			{
				ahUserStrings.Add(new UserReplacement() { sKey = "FOLDER", sValue = sSequenceDirectory });
				ahUserStrings.Add(new UserReplacement() { sKey = "LOOPS", sValue = _ui_nudLoopsQty.Value.ToString() });
			}
			#endregion
			#region - user plashka
			try
			{
				if (_ui_ctrTB_Template1Plaques == ui_tplb)   // если готовиться титр программы
				{
					if (_ui_ctrTB_Template1Credits.bTemplateButtonIsBusy) // и уже идет титр клипа
					{
						ItemPrepareCompleted(ui_tplb, null);
						return;
					}

					if ("" == _ui_tbChoosedString1.Text.Trim() && "" == _ui_tbChoosedString2.Text.Trim())  // а писать в титре нечего
					{
						if (TemplateButton.Status.Preparing == _ui_ctrTB_Template1Plaques.eStatus)
							_ui_ctrTB_Template1Plaques.eStatus = TemplateButton.Status.Idle;
						return;
					}
					string sOneString = _ui_tbChoosedString1.Text.Trim();

					if (sOneString.IsNullOrEmpty() || _ui_tbChoosedString2.Text.Trim().IsNullOrEmpty())
					{
						sOneString = sOneString.IsNullOrEmpty() ? _ui_tbChoosedString2.Text : _ui_tbChoosedString1.Text;
						ahUserStrings.Add(new UserReplacement() { sKey = "ARTIST", sValue = sOneString });
						if (true == _ui_cbAdvert.IsChecked)
							sFile = App.cPreferences.aTemplates.Get(Bind.preset_notice_advert).sFile;
						else if (true == _ui_cbTrail.IsChecked)
						{
							sFile = App.cPreferences.aTemplates.Get(Bind.preset_notice_trail).sFile;
							ahUserStrings.Add(new UserReplacement() { sKey = "FOLDER", sValue = sTrailDirectory });
						}
						else
							sFile = App.cPreferences.aTemplates.Get(Bind.preset_notice).sFile;
					}
					else
					{
						ahUserStrings.Add(new UserReplacement() { sKey = "ARTIST", sValue = _ui_tbChoosedString1.Text });
						ahUserStrings.Add(new UserReplacement() { sKey = "SONG", sValue = _ui_tbChoosedString2.Text });
						if (true == _ui_cbAdvert.IsChecked)
							sFile = App.cPreferences.aTemplates.Get(Bind.preset_credits_advert).sFile;
						else if (true == _ui_cbTrail.IsChecked)
						{
							sFile = App.cPreferences.aTemplates.Get(Bind.preset_credits_trail).sFile;
							ahUserStrings.Add(new UserReplacement() { sKey = "FOLDER", sValue = sTrailDirectory });
						}
						else
							sFile = App.cPreferences.aTemplates.Get(Bind.preset_credits).sFile;
					}
					_ui_ctrTB_Template1Plaques.sFile = sFile;
				}
			}
			catch (Exception ex)
			{
				_cCues.WriteErrorAsync("SLException: ItemStart: " + ex.Message + "<br>" + ex.StackTrace + "<br>" + ex.InnerException);
			}
			#endregion
			#region - credits
			try
			{
				if (_ui_ctrTB_Template1Credits == ui_tplb) // если готовится титр клипа    добавить огр на запуск слишком в конце клипов 
				{
					if (null == _cLPLWatcher || 1 > (nAssetsID = _cLPLWatcher.GetCurrentClipAssetID) || _ui_ctrTB_Template1Plaques.bTemplateButtonIsBusy)
					{
						ItemPrepareCompleted(ui_tplb, null);
						return;
					}// значит плейлист стартанул, идет что-то и есть ID этого чего-то
				}
			}
			catch (Exception ex)
			{
				_cCues.WriteErrorAsync("SLException: ItemStart: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.InnerException);
			}
		 
			#endregion

			#region - conflicts
			// здесь sFile уже актуален для любых кнопок


			services.preferences.Template cT = App.cPreferences.aTemplates.FirstOrDefault(o => o.sFile == sFile);
			services.preferences.Template cTStarted;
			if (cT != null)
			{
				TemplateButton[] aStartedBtns = _aTemplateButtons.Where(o => o.eStatus == TemplateButton.Status.Started).ToArray();
				bool bFirst = true;
				foreach (TemplateButton cTB in aStartedBtns)
				{
					cTStarted = App.cPreferences.aTemplates.FirstOrDefault(o => o.sFile == cTB.sFile); //TODO добавить знание о реальном файле, который в эфире (актуально только для плашек)

					if (null != cTStarted && App.cPreferences.aTemplates.Get(cT.eBind).IsItConflicts(_cPresetSelected.nID, cTStarted.eBind)) 
					{
						if (bFirst)
						{
							bFirst = !bFirst;
							if (TemplateButton.Status.Preparing == ui_tplb.eStatus)
								ui_tplb.eStatus = TemplateButton.Status.Idle;
							cTB.TemplateStopped += TemplateStoppedForAnotherTemplateStart;
							cTB.cButtonToActivateOnEvents = ui_tplb;   //TODO  можно еще препарить этот айтем, а при стопе сразу стартонуть...
						}
						cTB.Click();    
						
					}
					if (!bFirst)
						return;
				}
			}
			#endregion

			if (_ui_ctrTB_Template1Plaques == ui_tplb) // возвращаем на место - обязательно после конфликтов
			{
				_ui_cbAdvert.IsChecked = false;
				_ui_cbTrail.IsChecked = false;
			}

			// на этом пли уже не автоматизировать
			if (null != _cLPLWatcher && ui_tplb.bPressedByUser && IsAirGoingNow)  //если плейлист идёт, нажал пользователь и эфир включен
				_cLPLWatcher.TurnOffTemplateOnCurrentPLI(ui_tplb); 

			sFile = PathApprove(sFile, _cPresetSelected);
			if (1 > nAssetsID)
				_cCues.ItemCreateAsync(_cPresetSelected.sName, sFile, ahUserStrings.ToArray(), ui_tplb);
			else
				_cDBI.CuesGetAsync(nAssetsID, ui_tplb);
			_cCues.WriteNoticeAsync("ItemPrepare: end: [file = " + ui_tplb.sFile + "]");
		}
		private void ItemPrepareCompleted(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			if (null == ui_tplb)   // если вызывали не с кнопки.
				return;
			lock (_aTemplateButtons)
			{
				if (null == ui_tplb.cItem)
					ui_tplb.eStatus = TemplateButton.Status.Error;
				//else
				//	ui_tplb.cItem.eStatusPrevious = TemplateButton.Status.Unknown;
			}
		}
		private void ItemStart(object sender, EventArgs e)
		{
			try
			{
				TemplateButton ui_tplb = (TemplateButton)sender;
				_cCues.WriteNoticeAsync("ItemStart: begin: [file = " + ui_tplb.sFile + "][pressed_by_user = " + ui_tplb.bPressedByUser + " ]"); 
				if (_ui_ctrTB_PlayList == ui_tplb)
					_cPlayer.ItemStartAsync(_cPlaylist);
				else
					_cCues.ItemStartAsync((Item)ui_tplb.cItem);
			}
			catch (Exception ex)
			{
				_cCues.WriteErrorAsync("SLException: ItemStart: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.InnerException);
			}
		}
		private void ItemStop(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			_cCues.WriteNoticeAsync("ItemStop: begin: [file = " + ui_tplb.sFile + "][pressed_by_user = " + ui_tplb.bPressedByUser + " ]"); 
			if (_ui_ctrTB_PlayList == ui_tplb)
				_cPlayer.ItemStopAsync(_cPlaylist);
			else
				_cCues.ItemStopAsync((Item)ui_tplb.cItem);
		}
		private void ItemDelete(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			_cCues.WriteNoticeAsync("ItemDelete: begin: [file = " + ui_tplb.sFile + "][pressed_by_user = " + ui_tplb.bPressedByUser + " ]"); 
			if (_ui_ctrTB_PlayList == ui_tplb)
				_cPlayer.ItemDeleteAsync(_cPlaylist);
			else
				_cCues.ItemDeleteAsync((Item)ui_tplb.cItem);
		}
		void TemplateStoppedForAnotherTemplateStart(object sender, EventArgs e)
		{
			TemplateButton cTB = (TemplateButton)sender;
			cTB.TemplateStopped -= TemplateStoppedForAnotherTemplateStart;
			if (null != cTB.cButtonToActivateOnEvents)
			{
				cTB.cButtonToActivateOnEvents.Click();
				cTB.cButtonToActivateOnEvents = null;
			}
		}
		#endregion
		#region . air .
		#region .. ui .
		List<Plaque> _ui_dgPlaques_ItemsSource_BKP;
		private void _ui_dgPlaques_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Plaque cTit = (Plaque)_ui_dgPlaques.SelectedItem;
			if (null != cTit)
			{
				if (null == cTit.sFirstLine)
					cTit.sFirstLine = "";
				_ui_tbChoosedString1.Text = cTit.sFirstLine;

				if (null == cTit.sSecondLine)
					cTit.sSecondLine = "";
				_ui_tbChoosedString2.Text = cTit.sSecondLine;
			}
		}
		private void _ui_btnStartAir_Click(object sender, RoutedEventArgs e)
		{
			if (!IsAirGoingNow)
            {
				if (_ui_ctrTB_PlayList.bTemplateButtonIsBusy)
				{
					_dlgMsgBox.ShowAttention(g.SCR.sNotice12);
					return;
				}

				if (_ui_cbFalseStart.IsChecked.Value)
					_cDBI_ShiftStartCompleted(null, null);   //sh
				else
					_cDBI.ShiftAddAsync(new DBI.IdNamePair() { nID = _cPresetSelected.nID, sName = _cPresetSelected.sName }, ""); //EMERGENCY:l нужно добавить всё же возможность вводить тему эфира    //sh
			}
            else
            {
				MsgBox msgStopTemplate = new MsgBox();
				msgStopTemplate.Closed += new EventHandler(msgStopTemplate_Closed);
				msgStopTemplate.ShowQuestion(g.SCR.sNotice13.Fmt(Environment.NewLine));
			}
//sh
			if (!_ui_cbFalseStart.IsChecked.Value)
				_ui_btnStartAirWaiting();  //sh
		}
		void msgStopTemplate_Closed(object sender, EventArgs e) // останавливаем эфир
		{
			try
			{
				((MsgBox)sender).Closed -= msgStopTemplate_Closed;
				if (MsgBox.MsgBoxButton.OK == ((MsgBox)sender).enMsgResult)
				{
					_ui_btnStartAirText1.Text = g.SCR.sNotice11.ToUpper();    // JustSelectTheTab() на это смотрит....
					_ui_btnStartAirText1.Foreground = Coloring.SCR.Timer.cStartBtnTextForegr;
					_ui_btnStartAirText2.Foreground = Coloring.SCR.Timer.cStartBtnTextForegr;
					DoAutomationOnStudioEnd();
					JustSelectTheTab();
					if (true == _ui_cbFalseStart.IsChecked)
						_cDBI_ShiftStopCompleted(null, null);
					else
						_cDBI.ShiftStopAsync(_cShiftCurrent);
				}
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
			}
		}
		private void _ui_btnStartAirWaiting()
		{
			_ui_btnStartAir.IsEnabled = false;
			_ui_btnStartAirText.Visibility = Visibility.Collapsed;
			_ui_btnStartAirProgress.Visibility = Visibility.Visible;
		}
		private void _ui_btnStartAirEnabling()
		{
			_ui_btnStartAir.IsEnabled = true;
			_ui_btnStartAirText.Visibility = Visibility.Visible;
			_ui_btnStartAirProgress.Visibility = Visibility.Collapsed;
		}
		private void _ui_btnAddPlaque_Click(object sender, RoutedEventArgs e)
		{
			PlaquesBlock(true);
			Plaque cSelected = (Plaque)_ui_dgPlaques.SelectedItem;
			string sNewName = _ui_tbChoosedString1.Text;
			if (null != cSelected)
			{
				string sRes = GetNewNameInGroup(cSelected.nGroupNumber);
				if ("" != sRes)
					sNewName = sRes;
			}
			Plaque cPlaque = new Plaque()
			{
				sName = sNewName.Trim(),
				sFirstLine = _ui_tbChoosedString1.Text,
				sSecondLine = _ui_tbChoosedString2.Text,
				cPreset = new DBI.IdNamePair() { nID = _cPresetSelected.nID, sName = _cPresetSelected.sName }   //  { nID = _cShiftCurrent.cPreset.nID, sName = _cShiftCurrent.cPreset.sName } 
			};
			if (IsPlaqueCorrect(cPlaque))
				_cDBI.PlaqueAddAsync(cPlaque, cPlaque);
			else
				PlaquesBlock(false);
		}
		private string GetNewNameInGroup(int nGroupNumber)
		{
			Plaque cLastInGroup = ((List<Plaque>)_ui_dgPlaques.ItemsSource).LastOrDefault(o => o.nGroupNumber == nGroupNumber);
			string sRetVal = "";
			if (null != cLastInGroup)
			{
				string[] aSplited = cLastInGroup.sName.Split('-');
				if (null != aSplited && 1 < aSplited.Length)
				{
					int nLast;
					if (int.TryParse(aSplited[aSplited.Length - 1], out nLast))
					{
						for (int ni = 0; aSplited.Length - 1 > ni; ni++)
							sRetVal += aSplited[ni] + "-";
						return sRetVal + " " + ++nLast;
					}
				}
			}
			return sRetVal;
		}
		void _ui_btnStartAirShowError()
		{
			if (null != _cTimerForbtnStartAirError)
				return;
			_cTimerForbtnStartAirError = new System.Windows.Threading.DispatcherTimer();
			_cTimerForbtnStartAirError.Tick += new EventHandler(_ui_btnStartAirCloseError);
			_cTimerForbtnStartAirError.Interval = new System.TimeSpan(0, 0, 0, 7);
			_cTimerForbtnStartAirError.Start();
			_ui_btnStartAirTextError.Visibility = Visibility.Visible;
			_ui_btnStartAirText.Visibility = Visibility.Collapsed;
			_ui_btnStartAir.IsEnabled = false;
		}
		void _ui_btnStartAirCloseError(object s, EventArgs args)
		{
			_ui_btnStartAirTextError.Visibility = Visibility.Collapsed;
			_ui_btnStartAirText.Visibility = Visibility.Visible;
			_cTimerForbtnStartAirError.Stop();
			_cTimerForbtnStartAirError = null;
			_ui_btnStartAir.IsEnabled = true;
		}
		void StartAir(Shift cShift)
		{
			_ui_btnStartAirText1.Text = g.SCR.sNotice4.ToUpper();
			_ui_btnStartAirText1.Foreground = Coloring.SCR.Timer.cStopBtnTextForegr;
			_ui_btnStartAirText2.Foreground = Coloring.SCR.Timer.cStopBtnTextForegr;
			_cShiftCurrent = cShift;


			DoAutomationOnStudioBegin();
		}
		private bool IsAnyBtnTemplateBusy()
		{
			foreach (TemplateButton cTB in _aTemplateButtons)
				if (cTB.bTemplateButtonIsBusy)
					return true;
			return false;
		}
		private void _ui_tcAirTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null == _ui_tcAirTemplates)
				return;
			try
			{
				if (null != e && 0 < e.RemovedItems.Count)
				{
					TabItem uiTabItem = (TabItem)e.RemovedItems[0];
					if (!e.RemovedItems.Contains(_ui_tiChoose))
					{
						MsgBox msgChangeTemplate = new MsgBox();
						msgChangeTemplate.Closed += new EventHandler(msgChangeTemplate_Closed);

						if (IsAnyBtnTemplateBusy())
						{
							msgChangeTemplate.Tag = uiTabItem;
							msgChangeTemplate.ShowQuestion(g.SCR.sNotice13.Fmt(Environment.NewLine));
						}
						else
						{
							msgChangeTemplate.enMsgResult = MsgBox.MsgBoxButton.OK;
							msgChangeTemplate_Closed(msgChangeTemplate, null);
						}
						return;
					}
					else
						uiTabItem.Content = null;
				}
				JustSelectTheTab();
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
			}
		}
		void msgChangeTemplate_Closed(object sender, EventArgs e)
		{
			try
			{
				MsgBox cMsgBox = (MsgBox)sender;
				cMsgBox.Closed -= msgChangeTemplate_Closed;
				if (MsgBox.MsgBoxButton.OK == cMsgBox.enMsgResult)
				{
					if (IsAirGoingNow && _ui_tiChoose != _ui_tcAirTemplates.SelectedItem)
						DoAutomationOnStudioBegin();
					JustSelectTheTab();
				}
				else
					_ui_tcAirTemplates.SelectedItem = cMsgBox.Tag;
			}
			catch (Exception ex)
			{
				WriteCuesError(ex);
			}
		}
		void EmergencyStopTemplateButton(TemplateButton tbtnToStop)
		{
			if (TemplateButton.Status.Started == tbtnToStop.eStatus)
				tbtnToStop.Click();
			else if (TemplateButton.Status.Prepared == tbtnToStop.eStatus)
				tbtnToStop.eStatus = TemplateButton.Status.Error;
		}
		void ParametersSet(TemplateButton cBtn, Bind eBind)
		{
			cBtn.Visibility = System.Windows.Visibility.Visible;
			cBtn.IsEnabled = false;
			Parameters[] aParam = App.cPreferences.aTemplates.Get(eBind).aParameters;
			if (null != aParam)
			{
				Parameters cParam = aParam.FirstOrDefault(o => o.nPresetID == _cPresetSelected.nID);
				if (null != cParam)
				{
					cBtn.sText = cParam.sText + ": ";
					cBtn.IsEnabled = cParam.bIsEnabled;
					if (!cParam.bIsVisible)
						cBtn.Visibility = System.Windows.Visibility.Collapsed;

					if (cParam.eFirstAction == FirstAction.prepare)
						cBtn.eFirstAction = TemplateButton.FirstAction.Prepare;
					else
						cBtn.eFirstAction = TemplateButton.FirstAction.Start;
				}
			}
			//cBtn.UpdateLayout();
			cBtn.Refresh();
		}
		void ParametersSet(Control cControl, Bind eBind)
		{
			cControl.IsEnabled = false;
			cControl.Visibility = System.Windows.Visibility.Visible;
			Parameters[] aParam = App.cPreferences.aTemplates.Get(eBind).aParameters;
			if (null != aParam)
			{
				Parameters cParam = aParam.FirstOrDefault(o => o.nPresetID == _cPresetSelected.nID);
				if (null != cParam)
				{
					if (cControl is CheckBox)
						((CheckBox)cControl).Content = cParam.sText;
					cControl.IsEnabled = cParam.bIsEnabled;
					if (!cParam.bIsVisible)
						cControl.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}
		void ParametersSet(StackPanel cSP, Bind eBind)
		{
			foreach (object cChild in cSP.Children)
				if (cChild is Control)
					ParametersSet((Control)cChild, eBind);
		}
		void ParametersSet(ReducePanel cRP, Bind eBind)
		{
			PlayerParameters cPlayerPrefs;
			if (eBind == Bind.playlist)
			{
				cPlayerPrefs = (PlayerParameters)App.cPreferences.aTemplates.Get(Bind.playlist).aParameters.Get(_cPresetSelected.nID);
				cRP.IsOpen = cPlayerPrefs.bOpened;
				if (cPlayerPrefs.bIsVisible)
					cRP.Visibility = System.Windows.Visibility.Visible;
				else
					cRP.Visibility = System.Windows.Visibility.Collapsed;

				_ui_rpClips.IsOpen = cPlayerPrefs.bClipChooserOpened;
				if (cPlayerPrefs.bClipChooserVisible)
					_ui_rpClips.Visibility = System.Windows.Visibility.Visible;
				else
					_ui_rpClips.Visibility = System.Windows.Visibility.Collapsed;
			}
		}

		void JustSelectTheTab()
		{
			TabItem cTab = (TabItem)_ui_tcAirTemplates.SelectedItem;
//			if (null != _cShiftCurrent)
			{
				foreach (TemplateButton cTB in _aTemplateButtons)
					EmergencyStopTemplateButton(cTB);

				if (IsAirGoingNow)
					_ui_btnStartAir_Click(null, null);
				ClearLPL();
				((TabItem)_ui_tcAirTemplates.SelectedItem).Content = null;
			}

			Preset cPreset = (Preset)cTab.Tag;


			ParametersSet(_ui_ctrTB_Template1Chat, Bind.channel_chat);
			ParametersSet(_ui_ctrTB_Template1Bumper, Bind.preset_bumper);
			ParametersSet(_ui_ctrTB_Template1Credits, Bind.preset_credits);
			ParametersSet(_ui_ctrTB_Template1Logo, Bind.preset_logo);
			//ParametersSet(_ui_ctrTB_Template1Plaques, Bind.preset_credits);  //todo разнести плашки и титры
			ParametersSet(_ui_ctrTB_TemplateUser1, Bind.preset_user1);
			ParametersSet(_ui_ctrTB_TemplateUser2, Bind.preset_user2);
			ParametersSet(_ui_ctrTB_TemplateUser3, Bind.preset_user3);
			ParametersSet(_ui_ctrTB_TemplateUser4, Bind.preset_user4);

			ParametersSet(_ui_ctrTB_Credits, Bind.channel_credits);
			ParametersSet(_ui_ctrTB_Logo, Bind.channel_logo);
			ParametersSet(_ui_ctrTB_Channel1, Bind.channel_user1);
			ParametersSet(_ui_ctrTB_Channel2, Bind.channel_user2);
			ParametersSet(_ui_ctrTB_Channel3, Bind.channel_user3);
			ParametersSet(_ui_ctrTB_Channel4, Bind.channel_user4);

			ParametersSet(_ui_ctrTB_TemplateSequence, Bind.preset_sequence);
			ParametersSet(_ui_hlbtnChoosePngSeq, Bind.preset_sequence);
			ParametersSet(_ui_cbAdvert, Bind.preset_credits_advert);
			ParametersSet(_ui_cbTrail, Bind.preset_credits_trail);
			ParametersSet(_ui_spTrail, Bind.preset_credits_trail);
			ParametersSet(_ui_rpPlaylist, Bind.playlist);



			if (cPreset.nID == 0)
				_cShiftCurrent = null;
			else
			{
				_ui_lbSequenceDirectory.Content = _ui_lbTrailDirectory.Content = g.SCR.sNoSequence;

	
				
				_ui_cbTrail.IsEnabled = false;
				_ui_ctrTB_TemplateSequence.IsEnabled = false;

				PlaquesBlock(true);
				if (null != _ui_gTemplate.Parent)
					((TabItem)_ui_gTemplate.Parent).Content = null;
				cTab.Content = _ui_gTemplate;
				_cDBI.NearestAdvertsBlockAsync();
				PlaquesRefresh();
			}
		}
		private void _ui_dgPlaques_RowEditEnded(object sender, DataGridRowEditEndedEventArgs e)
		{
			Plaque cPlaque;
			if (null != e.Row)
			{
				cPlaque = (Plaque)e.Row.DataContext;
				Plaque cPlaqueTaged = ((List<Plaque>)_ui_dgPlaques.Tag).FirstOrDefault(o => o.nID == cPlaque.nID);
				if (cPlaque.sName != cPlaqueTaged.sName || cPlaque.sFirstLine != cPlaqueTaged.sFirstLine || cPlaque.sSecondLine != cPlaqueTaged.sSecondLine)
				{
					PlaquesBlock(true);
					if ((cPlaque.sName != cPlaqueTaged.sName || cPlaque.sFirstLine == "") && !IsPlaqueCorrect(cPlaque))
						PlaquesRefresh();
					else
						_cDBI.PlaqueChangeAsync(cPlaque, cPlaque.nID);
				}
			}
		}
		private void _ui_hlbtnRefresh_Click(object sender, RoutedEventArgs e)
		{
			_ui_lblNearestAdvBlockRemain.Focus();
			dtNearestAdvBlock = DateTime.Now;
			Brush cTMP = _ui_hlbtnRefresh.Foreground;
			_ui_hlbtnRefresh.Foreground = Coloring.SCR.cRefreshBtnPressed;
			System.Windows.Threading.DispatcherTimer _cTimerFor_ui_hlbtnRefresh = new System.Windows.Threading.DispatcherTimer();
			_cTimerFor_ui_hlbtnRefresh.Interval = new System.TimeSpan(0, 0, 1);
			_cTimerFor_ui_hlbtnRefresh.Tick +=
				delegate(object s, EventArgs args)
				{
					_cTimerFor_ui_hlbtnRefresh.Stop();
					_ui_hlbtnRefresh.Foreground = cTMP;
					_ui_lblNearestAdvBlockRemain.Foreground = Coloring.SCR.cRefreshBtnNormal;
				};
			_cTimerFor_ui_hlbtnRefresh.Start();
		}
		private void _ui_dgPlaques_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			e.Row.Background = GetColorForPlaques(((Plaque)e.Row.DataContext).nGroupNumber);
		}
		private Brush GetColorForPlaques(int ni)
		{
			ni = ni % 2;
			switch (ni)
			{
				case 0:
					return Coloring.SCR.cUserPlaques_FirstColorBackgr;
				case 1:
					return Coloring.SCR.cUserPlaques_SecondColorBackgr;
			}
			return Coloring.SCR.cUserPlaques_FirstColorBackgr;
		}
		private void _ui_tbChoozed_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && sender is TextBox)
			{
				if (((TextBox)sender).Name == "_ui_tbChoosedString1")
					_ui_tbChoosedString2.Focus();
				else if (((TextBox)sender).Name == "_ui_tbChoosedString2")
					_ui_btnAddPlaque.Focus();
			}
			else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Delete && sender is DataGrid)
			{
				if (((DataGrid)sender).Name == "_ui_dgPlaques" && null != (_cSelectedPlaque = (Plaque)_ui_dgPlaques.SelectedItem))
				{
					MsgBox dlgDel = new MsgBox();
					dlgDel.Closed += new EventHandler(dlgDel_Closed);
					dlgDel.ShowQuestion(g.SCR.sNotice14 + ": \n" + _cSelectedPlaque.sName + "', '" + _cSelectedPlaque.sFirstLine + "', '" + _cSelectedPlaque.sSecondLine + "'");
				}
			}
		}
		void dlgDel_Closed(object sender, EventArgs e)
		{
			((MsgBox)sender).Closed -= dlgDel_Closed;
			if (((MsgBox)sender).enMsgResult == MsgBox.MsgBoxButton.OK)
			{
				_ui_cmPlaques_Delete(null, null);
			}
			else
				_ui_dgPlaques.Focus();
		}
		private void PlaqueGridHide()
		{
			_ui_dgPlaques_ItemsSource_BKP = (List<Plaque>)_ui_dgPlaques.ItemsSource;
			_ui_dgPlaques.ItemsSource = new List<Plaque>();
			_ui_hlbtnHidePlaques.Content = g.SCR.sNotice16;
			_ui_btnAddPlaque.IsEnabled = false;
		}
		private void PlaqueGridShow()
		{
			_ui_dgPlaques.ItemsSource = new List<Plaque>();
			_ui_dgPlaques.ItemsSource = _ui_dgPlaques_ItemsSource_BKP;
			_ui_hlbtnHidePlaques.Content = g.SCR.sNotice15;
			_ui_btnAddPlaque.IsEnabled = true;

		}
		private void _ui_hlbtnHidePlaques_Click(object sender, RoutedEventArgs e)
		{
			if(g.SCR.sNotice15 == (string)_ui_hlbtnHidePlaques.Content)
			{
				PlaqueGridHide();
            }
            else if(g.SCR.sNotice16 == (string)_ui_hlbtnHidePlaques.Content)
            {
				PlaqueGridShow();
			}
		}
		private void _ui_hlbtnChoosePngSeq_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sFolder = ((PlayerParameters)App.cPreferences.aTemplates.Get(Bind.playlist).aParameters.Get(_cPresetSelected.nID)).sFolder;
				_dlgFilesChooser.Show(sFolder + @"Sequences\", new string[0], FilesChooser.Type.Sequence);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);//EMERGENCY:l и куда это пишется? просто чтобы во время дебага было видно чтоль?)
			}
		}
		private void _ui_hlbtnChooseTrail_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sFolder = ((PlayerParameters)App.cPreferences.aTemplates.Get(Bind.playlist).aParameters.Get(_cPresetSelected.nID)).sFolder;
				_dlgFilesChooser.Show(sFolder + @"Trail\", new string[0], FilesChooser.Type.Trail);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}
		private void _ui_cbTrail_Checked(object sender, RoutedEventArgs e)
		{
			if (null != _ui_cbAdvert.IsChecked && _ui_cbAdvert.IsChecked.Value)
				_ui_cbAdvert.IsChecked = false;
		}
		private void _ui_cbAdvert_Checked(object sender, RoutedEventArgs e)
		{
			if (null != _ui_cbTrail.IsChecked && _ui_cbTrail.IsChecked.Value)
				_ui_cbTrail.IsChecked = false;
		}
		#endregion
		#region .. ui air cm .
		private void _ui_dgPlaques_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_cSelectedPlaque = (Plaque)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
			}
			catch
			{
				_cSelectedPlaque = null;
			}
		}
		private void _ui_cmPlaques_Opened(object sender, RoutedEventArgs e)
		{
			if (g.SCR.sNotice16 == (string)_ui_hlbtnHidePlaques.Content)
				return;
			if (null != _cSelectedPlaque)
			{
				_ui_cmPlaquesDelete.Header = g.SCR.sNotice17.ToLower() + ": '" + _cSelectedPlaque.sName + "', '" + _cSelectedPlaque.sFirstLine + "', '" + _cSelectedPlaque.sSecondLine + "'     (CTRL + Delete)";
				_ui_cmPlaquesDelete.IsEnabled = true;
			}
			_ui_cmPlaquesRefresh.IsEnabled = true;
			_ui_cmPlaquesRefresh.Refresh();
			_ui_cmPlaquesDelete.Refresh();
		}
		private void _ui_cmPlaques_Closed(object sender, RoutedEventArgs e)
		{
			_ui_cmPlaquesRefresh.IsEnabled = false;
			_ui_cmPlaquesDelete.IsEnabled = false;
			_ui_cmPlaquesRefresh.Refresh();
			_ui_cmPlaquesDelete.Refresh();
		}
		private void _ui_cmPlaques_Refresh(object sender, RoutedEventArgs e)
		{
			PlaquesBlock(true);
			PlaquesRefresh();
		}
		private void _ui_cmPlaques_Delete(object sender, RoutedEventArgs e)
		{
			if (null != _cSelectedPlaque)
			{
				PlaquesBlock(true);
				_cDBI.PlaqueDeleteAsync(_cSelectedPlaque, _cSelectedPlaque.nID);
			}
		}
		#endregion
		#region ..
		private class PlaquesCompare : IComparer<Plaque>
		{
			int IComparer<Plaque>.Compare(Plaque x, Plaque y)
			{
				return string.Compare(x.sName, y.sName);
			}
		}
		private void PlaqueGridRefresh(List<Plaque> aPlaques)
		{
			_ui_dgPlaques.ItemsSource = new List<Plaque>();
			_ui_dgPlaques.ItemsSource = aPlaques;
			_ui_dgPlaques_ItemsSource_BKP = aPlaques;
			List<Plaque> aPlaquesTaged = new List<Plaque>();
			foreach (Plaque cPlaque in aPlaques)
				aPlaquesTaged.Add(new Plaque() { nGroupNumber = cPlaque.nGroupNumber, nID = cPlaque.nID, sFirstLine = cPlaque.sFirstLine, sName = cPlaque.sName, sSecondLine = cPlaque.sSecondLine, cPreset = cPlaque.cPreset });
			_ui_dgPlaques.Tag = aPlaquesTaged;

			services.preferences.Plaque cPrefPlaque = App.cPreferences.aPlaques.FirstOrDefault(o => o.nPresetID == _cPresetSelected.nID);
			bool bOpened = null == cPrefPlaque ? true : cPrefPlaque.bOpened;
			_ui_dgPlaques.MaxHeight = null == cPrefPlaque ? 300 : cPrefPlaque.nHeight;

			if (bOpened)
			{
				_ui_hlbtnHidePlaques.Content = g.SCR.sNotice15;
				PlaqueGridShow();
			}
			else
			{
				_ui_hlbtnHidePlaques.Content = g.SCR.sNotice16;
				PlaqueGridHide();
			}
		}
		private void SetGroupsNumbers(List<Plaque> aPlaques)
		{
			int nPrevColor = -1;
			string sPrevName = "";
			string sSubName;
			foreach (Plaque cPlaque in aPlaques)
			{
				sSubName = cPlaque.sName.Length > 3 ? cPlaque.sName.Substring(0, 4) : cPlaque.sName;
				if (sPrevName != sSubName)
				{
					nPrevColor++;
					sPrevName = sSubName;
				}
				cPlaque.nGroupNumber = nPrevColor;
			}
		}
		#endregion
		void NearestAdvBlockCountdown(object s, EventArgs args)
		{
			System.TimeSpan dtNBlo = dtNearestAdvBlock.Subtract(DateTime.Now);
			System.TimeSpan dtNRef = dtNextRefresh.Subtract(DateTime.Now);
			bool bRenew = false;
			try
			{
				if (System.TimeSpan.Zero > dtNBlo || System.TimeSpan.Zero > dtNRef)
					bRenew = true;
				else
				{
					_ui_lblNearestAdvBlockRemain.Content = (new DateTime(1, 1, 1, dtNBlo.Hours, dtNBlo.Minutes, dtNBlo.Seconds)).ToString("H:mm:ss");
					if (0 == dtNBlo.Hours && 5 > dtNBlo.Minutes)
						_ui_lblNearestAdvBlockRemain.Foreground = Coloring.SCR.Timer.cAdvTimerWarningForegr;
					else
						_ui_lblNearestAdvBlockRemain.Foreground = Coloring.SCR.Timer.cAdvTimerNormalForegr;
				}
			}
			catch
			{
				bRenew = true;
			}
			if (bRenew)
			{
				_cTimerForNearestAdvBlock.Stop();
				_cTimerForNearestAdvBlock = null;
				_cDBI.NearestAdvertsBlockAsync();
			}
		}
		bool IsNameCorrect(string sName)
		{
			if ("" == sName)
			{
				_dlgMsgBox.ShowError(g.SCR.sError19);
				return false;
			}
			List<Plaque> aPlaques = (List<Plaque>)_ui_dgPlaques.Tag;
			if (0 < aPlaques.Count(o => o.sName == sName))
			{
				_dlgMsgBox.ShowError(g.SCR.sError20);
				return false;
			}
			return true;
		}
		bool IsPlaqueCorrect(Plaque cPlaque)
		{
			if ("" == cPlaque.sFirstLine)
			{
				_dlgMsgBox.ShowError(g.SCR.sError21);
				return false;
			}
			if (!IsNameCorrect(cPlaque.sName))
				return false;
			return true;
		}
		void PlaquesRefresh()
		{
			if (_ui_tiChoose != _ui_tcAirTemplates.SelectedItem)
				_cDBI.PlaquesGetAsync(new DBI.IdNamePair() { nID = _cPresetSelected.nID, sName = _cPresetSelected.sName });
		}
		void PlaquesBlock(bool bBlock)
		{
			_ui_dgPlaques.IsEnabled = !bBlock;
			_ui_btnAddPlaque.IsEnabled = !bBlock;
			if (!bBlock)
				_ui_dgPlaques.Focus();
		}
		public void DoAutomationOnStudioBegin()
		{
			string sStarted = "", sStopped = "";
			scr.services.preferences.Template[] aTemplatesIn = App.cPreferences.aTemplates.Where(o => null != o.aOffsets && null != o.aOffsets.FirstOrDefault(oo => oo.sType == PLIType.Studio.ToString().ToLower() && int.MaxValue > oo.nOffsetIn && 0 <= oo.nOffsetIn)).ToArray();
			scr.services.preferences.Template[] aTemplatesOut = App.cPreferences.aTemplates.Where(o => null != o.aOffsets && null != o.aOffsets.FirstOrDefault(oo => oo.sType == PLIType.Studio.ToString().ToLower() && int.MaxValue > oo.nOffsetOut && 0 <= oo.nOffsetOut)).ToArray();

			foreach (scr.services.preferences.Template cT in aTemplatesIn)
			{
				sStarted += cT.eBind.ToString() + ", ";
				StartTemplate(cT);
			}
			foreach (scr.services.preferences.Template cT in aTemplatesOut)
			{
				sStopped += cT.eBind.ToString() + ", ";
				StopTemplate(cT);
			}
			_cPlayer.WriteNoticeAsync("DoAutomationOnStudioBegin: [started:" + sStarted + "][stopped:" + sStopped + "]");
		}
		public void DoAutomationOnStudioEnd()
		{
			string sStarted = "", sStopped = "";
			scr.services.preferences.Template[] aTemplatesIn = App.cPreferences.aTemplates.Where(o => null != o.aOffsets && null != o.aOffsets.FirstOrDefault(oo => oo.sType == PLIType.Studio.ToString().ToLower() && 0 > oo.nOffsetIn)).ToArray();
			scr.services.preferences.Template[] aTemplatesOut = App.cPreferences.aTemplates.Where(o => null != o.aOffsets && null != o.aOffsets.FirstOrDefault(oo => oo.sType == PLIType.Studio.ToString().ToLower() && 0 > oo.nOffsetOut)).ToArray();
			foreach (scr.services.preferences.Template cT in aTemplatesIn)
			{
				sStarted += cT.eBind.ToString() + ", ";
				StartTemplate(cT);
			}
			foreach (scr.services.preferences.Template cT in aTemplatesOut)
			{
				sStopped += cT.eBind.ToString() + ", ";
				StopTemplate(cT);
			}
			_cPlayer.WriteNoticeAsync("DoAutomationOnStudioEnd: [started:" + sStarted + "][stopped:" + sStopped + "]");
		}
		public bool StartTemplate(string sTextOnButton)
		{
			bool bRetVal = true;
			TemplateButton cTB = _aTemplateButtons.FirstOrDefault(o => o.sText.StartsWith(sTextOnButton));
			if (null != cTB && !cTB.bTemplateButtonIsBusy)
			{
				_cPlayer.WriteNoticeAsync("StartTemplate: TRUE: [template:" + sTextOnButton + "][status: " + cTB.eStatus + "][file=" + cTB.sFile + "]");
				cTB.Click();
			}
			else
			{
				_cPlayer.WriteNoticeAsync("StartTemplate: FALSE: [template:" + sTextOnButton + "][status: " + cTB == null ? "NULL" : cTB.eStatus + "]");
				bRetVal = false;
			}
			return bRetVal;
		}
		public bool StartTemplate(scr.services.preferences.Template cTemplate)
		{
			bool bRetVal = true;
			TemplateButton cTB = _aTemplateButtons.FirstOrDefault(o => o.sFile == cTemplate.sFile);
			if (null != cTB && !cTB.bTemplateButtonIsBusy)
			{
				_cPlayer.WriteNoticeAsync("StartTemplate: TRUE: [template:" + cTemplate.eBind.ToString() + "][status: " + cTB.eStatus + "][file=" + cTB.sFile + "]");
				cTB.Click();
			}
			else
			{
				_cPlayer.WriteNoticeAsync("StartTemplate: FALSE: [template:" + cTemplate.eBind.ToString() + "][status: " + cTB == null ? "NULL" : cTB.eStatus + "]");
				bRetVal = false;
			}
			return bRetVal;
		}
		public bool StopTemplate(scr.services.preferences.Template cTemplate)
		{
			bool bRetVal = true;
			TemplateButton cTB = _aTemplateButtons.FirstOrDefault(o => o.sFile == cTemplate.sFile);
			if (null != cTB && cTB.eStatus == TemplateButton.Status.Started)
			{
				_cPlayer.WriteNoticeAsync("StopTemplate: TRUE: [template:" + cTemplate.eBind.ToString() + "][status: " + cTB.eStatus + "][file=" + cTB.sFile + "]");
				cTB.Click();
			}
			else
			{
				_cPlayer.WriteNoticeAsync("StopTemplate: FALSE: [template:" + cTemplate.eBind.ToString() + "][status: " + cTB == null ? "NULL" : cTB.eStatus + "]");
				bRetVal = false;
			}
			return bRetVal;
		}
		#endregion
		#region . clips .
		private void _ui_dgClipsSCR_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			IP.Clip cT = (IP.Clip)e.Row.DataContext;
			if (cT.bLocked)
				e.Row.Foreground = Coloring.SCR.cClipsRow_BlockedForegr;
			else
				e.Row.Foreground = Coloring.SCR.cClipsRow_NormalForegr;
		}
        void _ui_rpClips_IsOpenChanged(object sender, EventArgs e)
		{
            if (_ui_rpClips.IsOpen && !_bClipsGot)
			{
				_bClipsGot = true;
				_ui_rpClips.IsEnabled = false;
				_cDBI.ClipsGetCompleted += new EventHandler<ClipsGetCompletedEventArgs>(_cDBI_ClipsGetCompleted);
				_cDBI.ClipsGetAsync();
			}
		}

		void _cDBI_ClipsGetCompleted(object sender, ClipsGetCompletedEventArgs e)
		{
			_cDBI.ClipsGetCompleted -= _cDBI_ClipsGetCompleted;
			_cPlayer.ClipsSCRGetCompleted += new EventHandler<ClipsSCRGetCompletedEventArgs>(_cPlayer_ClipsSCRGetCompleted);
			List<IP.Clip> aSCRClips = new List<IP.Clip>();
			if (null != e.Result)
			{
				_cCues.WriteNoticeAsync("_cDBI_ClipsGetCompleted: [count=" + e.Result.Length + "]");
				IP.Clip cIPClip;
				string sMissed = g.Common.sMissing.ToLower();
				foreach (DBI.Clip cDBIClip in e.Result)
				{
					if (null == cDBIClip)
						continue;
					cIPClip = new IP.Clip();
					cIPClip.nFramesQty = cDBIClip.nFramesQty;
					cIPClip.nID = cDBIClip.nID;
					cIPClip.sName = cDBIClip.sName;
					cIPClip.bSmoking = cDBIClip.bSmoking;
					cIPClip.sClassName = cDBIClip.cClass.sName;
					cIPClip.sFilename = cIPClip.sStorageName = cIPClip.sArtist = cIPClip.sSong = cIPClip.sRotation = sMissed;
					if (null != cDBIClip.cFile)
					{
						cIPClip.sFilename = cDBIClip.cFile.sFilename;
						if (null != cDBIClip.cFile.cStorage)
							cIPClip.sStorageName = cDBIClip.cFile.cStorage.sName;
					}
					if (null != cDBIClip.stCues)
					{
						cIPClip.sArtist = cDBIClip.stCues.sArtist;
						cIPClip.sSong = cDBIClip.stCues.sSong;
					}
					if (null != cDBIClip.cRotation)
						cIPClip.sRotation = cDBIClip.cRotation.sName;
					aSCRClips.Add(cIPClip);
				}
				_cPlayer.ClipsSCRGetAsync(aSCRClips.ToArray());
			}
			else
				_cCues.WriteNoticeAsync("_cDBI_ClipsGetCompleted: [count=NULL]");
		}
		void _cPlayer_ClipsSCRGetCompleted(object sender, ClipsSCRGetCompletedEventArgs e)
		{
			string sLog = "";
			_cPlayer.ClipsSCRGetCompleted -= _cPlayer_ClipsSCRGetCompleted;
			try
			{
				if (null != e && null != e.Result)
				{
					sLog = e.Result.Length.ToString();
					IP.Clip[] aClips = (IP.Clip[])e.Result;
					_ui_dgClipsSCR.Tag = aClips.ToList();
					_ui_dgClipsSCR.ItemsSource = new IP.Clip[0];
					_ui_dgClipsSCR.ItemsSource = aClips;  // _ui_Search.aItemsSource = _ui_Search.aItemsSourceInitial
					_ui_Search.DataContext = _ui_dgClipsSCR;

					_ui_rpClips.IsEnabled = true;
				}
				else
				{
					sLog = "NULL";
					_ui_dgClipsSCR.Tag = _ui_dgClipsSCR.ItemsSource = null;
					_ui_rpClips.IsEnabled = true;
					_dlgMsgBox.ShowError(g.SCR.sError22);
				}
				_cCues.WriteNoticeAsync("_cPlayer_ClipsSCRGetCompleted: [count=" + sLog + "]");
			}
			catch (Exception ex)
			{
				_cCues.WriteErrorAsync(ex.Message + "\n" + ex.StackTrace + "\n" + ex.InnerException.Message);
			}
		}
		void ClearSelected()
		{
			_ui_lblNameOfSelected.Content = g.Common.sNoSelection.ToUpper();
			_ui_lblClipChoosed.Content = "(" + g.Common.sNoSelection.ToLower() + ")";
			_ui_hlbtnAddClip.IsEnabled = false;
//			_aSelectedClips = new List<LivePLItem>();
		}
		private void _ui_dgClipsSCR_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ClearSelected();
			if (null == _ui_dgClipsSCR.SelectedItem)
				return;
			string sText;
			int nC = _ui_dgClipsSCR.SelectedItems.Count;
			if (1 == nC)
				sText = ((IP.Clip)_ui_dgClipsSCR.SelectedItem).sName;
			else
				sText = nC + " items";
			_ui_lblNameOfSelected.Content = sText;
			_ui_lblClipChoosed.Content = "(" + sText + ")";
//			//foreach (object objClip in _ui_dgClipsSCR.SelectedItems)
//			//    _aSelectedClips.Add(new LivePLItem((IP.Clip)objClip));
			_ui_hlbtnAddClip.IsEnabled = true;
		}
		private void _ui_hlbtnRefreshClips_Click(object sender, RoutedEventArgs e)
		{
			_bClipsGot = false;
			_ui_rpClips_IsOpenChanged(null, null);
		}
		void _ui_dgClipsSCR_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (dtNextMouseClickForDouble < DateTime.Now)
				dtNextMouseClickForDouble = DateTime.Now.AddMilliseconds(400);
			else
			{
				_ui_dgClipsSCR.SelectedItem = (IP.Clip)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
				dtNextMouseClickForDouble = DateTime.MinValue;
				_ui_hlbtnAddClip_Click(null, null);
			}
		}
		#region . clips cm .
		private void _ui_dgClipsSCR_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (1 >= _ui_dgClipsSCR.SelectedItems.Count)
				try
				{
					IP.Clip cClip = (IP.Clip)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
					_ui_dgClipsSCR.SelectedItem = cClip;
				}
				catch
				{
				}
		}
		private void _ui_cmClipsSCR_Opened(object sender, RoutedEventArgs e)
		{
			if (0 < _ui_dgClipsSCR.SelectedItems.Count)
			{
				if (1 == _ui_dgClipsSCR.SelectedItems.Count)
					_ui_cmClipsSCRAdd.Header = g.SCR.sNotice18 + ": \"" + ((IP.Clip)_ui_dgClipsSCR.SelectedItem).sName + "\"";
				else
					_ui_cmClipsSCRAdd.Header = g.SCR.sNotice19 + ": " + _ui_dgClipsSCR.SelectedItems.Count + " items";
				_ui_cmClipsSCRAdd.IsEnabled = _ui_hlbtnAddClip.IsEnabled;
			}
			_ui_cmClipsSCRAdd.Refresh();
		}
		private void _ui_cmClipsSCRAdd_Click(object sender, RoutedEventArgs e)
		{
			_ui_hlbtnAddClip_Click(null, null);
		}
		#endregion
		#endregion
		#region . playlist .
		void PlaylistItemsGetAfterDelay(object sender, EventArgs e)
		{
			_cPlayer.PlaylistItemsGetAsync((Item)_ui_ctrTB_PlayList.cItem);
		}
		public void _cPlayer_PlaylistItemsGet()
		{
			_cPlayer.PlaylistItemsGetAsync((Item)_ui_ctrTB_PlayList.cItem);
		}
		#region .. playlist ui .
		private void _ui_hlbtnAddAdverts_Click(object sender, RoutedEventArgs e)
		{
			_dlgAdvChooser.enType = AdvertsBlockChooser.BlockType.Adverts;
			_dlgAdvChooser.aStorages = _aStorages;
			_dlgAdvChooser.Show();
		}
		private void _ui_hlbtnAddClip_Click(object sender, RoutedEventArgs e)
		{
			List<LivePLItem> aSelectedClips = new List<LivePLItem>();
			if (null != _ui_dgClipsSCR.SelectedItems && 0 < _ui_dgClipsSCR.SelectedItems.Count)
			{
				foreach (object objClip in _ui_dgClipsSCR.SelectedItems)
					aSelectedClips.Add(new LivePLItem((IP.Clip)objClip));
				if (TemplateButton.Status.Started == _ui_ctrTB_PlayList.eStatus)
					PlayListVideoAdd(LivePLItem.SetIDs(aSelectedClips));
				else
				{
					_aLivePLTotal.AddRange(LivePLItem.SetIDs(aSelectedClips));
					ShowPL();
				}
			}
		}
		void dlgAdvChooser_Closed(object sender, EventArgs e)
		{
			_cCues.WriteNoticeAsync(_dlgAdvChooser.sLog);
			if (true == _dlgAdvChooser.DialogResult)
			{
				if (TemplateButton.Status.Started == _ui_ctrTB_PlayList.eStatus)
					PlayListVideoAdd(LivePLItem.SetIDs(LivePLItem.RemoveJustStrings(_dlgAdvChooser._aAdvSelectedSingle)));  // ((AdvertsBlockChooser)sender)._aAdvSelectedSingle
				else
				{
					_aLivePLTotal.AddRange(LivePLItem.SetIDs(LivePLItem.RemoveJustStrings(_dlgAdvChooser._aAdvSelectedSingle)));
					ShowPL();
				}
			}
		}
		private void _ui_hlbtnAddFile_Click(object sender, RoutedEventArgs e)
		{
			try 
			{
				string sFolder = ((PlayerParameters)App.cPreferences.aTemplates.Get(Bind.playlist).aParameters.Get(_cPresetSelected.nID)).sFolder;
				_dlgFilesChooser.Show(sFolder, new string[4] { "mov", "mp4", "mpg", "mxf" }, FilesChooser.Type.Videos);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}
		private void _ui_hlbtnAddImage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sFolder = ((PlayerParameters)App.cPreferences.aTemplates.Get(Bind.playlist).aParameters.Get(_cPresetSelected.nID)).sFolder;
				_dlgFilesChooser.Show(sFolder + @"Images\", new string[4] { "png", "jpg", "jpeg", "bmp" }, FilesChooser.Type.Images);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

		void dlgFilesChooser_Closed(object sender, EventArgs e)
		{
			List<LivePLItem> aItems = new List<LivePLItem>();
			if (null != sender)
			{
				if (true == _dlgFilesChooser.DialogResult)
				{
					switch (_dlgFilesChooser.eType)
					{
						case FilesChooser.Type.Sequence:
							_ui_nudLoopsQty.Value = 1;
							_ui_ctrTB_TemplateSequence.IsEnabled = true;
							sSequenceDirectory = _dlgFilesChooser.cResult.sFilenameFull;
							_ui_lbSequenceDirectory.Content = _dlgFilesChooser.cResult.sFilenameFull;
							break;
						case FilesChooser.Type.Trail:
							_ui_cbTrail.IsEnabled = true;
							sTrailDirectory = _dlgFilesChooser.cResult.sFilenameFull;
							_ui_lbTrailDirectory.Content = _dlgFilesChooser.cResult.sFilenameFull;
							break;
						case FilesChooser.Type.Images:
						case FilesChooser.Type.Videos:
							aItems.Add(_dlgFilesChooser.cResult);
							if (TemplateButton.Status.Started == _ui_ctrTB_PlayList.eStatus)
								PlayListVideoAdd(LivePLItem.SetIDs(aItems));
							else
							{
								_aLivePLTotal.AddRange(LivePLItem.SetIDs(aItems));
								ShowPL();
							}
							break;
					}
				}
				else if ("" != _dlgFilesChooser.sErr)
                    _dlgMsgBox.ShowError(_dlgFilesChooser.sErr);
			}
		}
		private void _ui_hlbtnDetales_Click(object sender, RoutedEventArgs e)
		{
			if (g.Common.sShowDetails.ToLower() == _ui_hlbtnDetales.Content.ToString())
				_ui_hlbtnDetales.Content = g.Common.sHideDetails.ToLower();
			else
				_ui_hlbtnDetales.Content = g.Common.sShowDetails.ToLower();
			ShowPL();
		}
		void _ui_dgLivePL_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			LivePLItem cLPLI = (LivePLItem)e.Row.DataContext;
			if (!cLPLI.bFileExist)
				e.Row.Background = Coloring.Notifications.cTextBoxError;
			// решил цветом не выделять текущий, а удалять из пл старые - т.е. текущий всегда первый.....
			//else if (null != _cLPLWatcher && (null != _cLPLWatcher.cCurrentLPLItem && (item == _cLPLWatcher.cCurrentLPLItem || item == _cLPLWatcher.cCurrentLPLItem.cBlock)))
			//    e.Row.Background = UI_Colors.cPlayListColors.stRowOnair;
			else if (PLIType.AdvBlockItem == cLPLI.eType)
				e.Row.Background = Coloring.SCR.cPLRow_AdvBlockItemBackgr;
			else if (PLIType.AdvBlock == cLPLI.eType || PLIType.JustString == cLPLI.eType)
				e.Row.Background = Coloring.SCR.cPLRow_AdvBlockBackgr;
			else if (PLIType.Clip == cLPLI.eType)
				e.Row.Background = Coloring.SCR.cPLRow_ClipBackgr;
			else
				e.Row.Background = Coloring.SCR.cPLRow_OthersBackgr;
			if (DateTime.MinValue < cLPLI.dtStopReal || DateTime.MinValue < cLPLI.dtStart && DateTime.Now > cLPLI.dtStart.AddMilliseconds(cLPLI._nFramesQty * 40))
				e.Row.Foreground = Coloring.SCR.cPLRow_StoppedForegr;
			else
				e.Row.Foreground = Coloring.SCR.cPLRow_NormalForegr;
		}
		void PlayListSkip(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			_cCues.WriteNoticeAsync("PlayListSkip: begin: [file = " + ui_tplb.sFile + "][pressed_by_user = " + ui_tplb.bPressedByUser + " ]"); 
			if (_cLPLWatcher.cCurrentLPLItem.eType == PLIType.AdvBlockItem || _cLPLWatcher.cCurrentLPLItem.eType == PLIType.AdvBlock)
			{
				_dlgMsgBox.ShowAttention(g.SCR.sNotice20);
				ui_tplb.SkippingCancel();
			}
			else
			//{
				//Sender.Tag2 = _cLPLWatcher.cCurrentLPLItem; //EMERGENCY:l закомментил, потому что нигде не используется
				_cPlayer.SkipAsync((Item)ui_tplb.cItem, true, 10, ui_tplb);
			//}
		}
		void PlayListPrepare(object sender, EventArgs e)
		{
			TemplateButton ui_tplb = (TemplateButton)sender;
			helpers.sl.common.CookieSet("logTB", ui_tplb.sLog += "PlayListPrepare: begin |");
			_cCues.WriteNoticeAsync("PlayListPrepare: begin: [file = " + ui_tplb.sFile + "][pressed_by_user = " + ui_tplb.bPressedByUser + " ]"); 
			try
			{
				if (0 == _aLivePLTotal.Count)
				{
					_cCues.WriteNoticeAsync("PlayListPrepare: end2: [file = " + ui_tplb.sFile + "]");
					return;
				}
				//_bIsPLDeletingEnable = false;
				PLButtonsEnable(false);
				List<IP.PlaylistItem> aItems = PLItemsPrepareForPL(_aLivePLTotal, null);
				if (0 < aItems.Count)
				{
					if ((aItems[0]._eType == PLIType.AdvBlockItem || aItems[0]._eType == PLIType.JustString) && _ui_ctrTB_Template1Chat.eStatus == TemplateButton.Status.Started)
						_ui_ctrTB_Template1Chat.Click();
					_cPlayer.ItemCreateAsync(_cPresetSelected.sName, aItems.ToArray(), 1, true, ui_tplb);
				}
			}
			catch (Exception ex)
			{
				_cCues.WriteErrorAsync("SLException: PlayListPrepare: " + ex.Message + "<br>" + ex.StackTrace + "<br>" + ex.InnerException);
			}
			_cCues.WriteNoticeAsync("PlayListPrepare: end: [file = " + ui_tplb.sFile + "]");
		}
		void PlayListVideoAdd(List<LivePLItem> aLPL)
		{
			if (null == aLPL)
			{
				_cCues.WriteErrorAsync("PlayListVideoAdd: begin: null == aLPL: return"); 
				return;
			}
			_cCues.WriteNoticeAsync("PlayListVideoAdd: begin: [file(s) = " + aLPL.Select(o => o.sFilename).ToString() + "]"); 
			List<IP.PlaylistItem> aItems = PLItemsPrepareForPL(aLPL, true);
			if (0 < aItems.Count)
				_cPlayer.AddVideoAsync((Item)_ui_ctrTB_PlayList.cItem, aItems.ToArray(), aLPL);
				//_cPlayer.AddVideoAsync(GetButtonItem(_ui_ctrTB_PlayList), aItems.ToArray(), aLPL);
		}
		List<IP.PlaylistItem> PLItemsPrepareForPL(List<LivePLItem> aLPL, bool? bPrevousItemIsAdv)
		{
			List<IP.PlaylistItem> aItems = new List<IP.PlaylistItem>();
			bool? bPrevIsAdv = bPrevousItemIsAdv;
			ushort nDur, nEndDur;
			foreach (LivePLItem cPLItem in aLPL)
			{
				nDur = 0;
				nEndDur = 0;
				if (null != cPLItem.sFilename && "" != cPLItem.sFilename)
				{
					if (g.Helper.sClips == cPLItem.sStorageName || cPLItem.bFileIsImage)
					{
						if (null == bPrevIsAdv)
							nDur = 25;
						else if (!(bool)bPrevIsAdv)
							nDur = 10;
						nEndDur = 25;
					}
					cPLItem.nTransDuration = nDur;
					cPLItem.nEndTransDuration = nEndDur;
					if (null == cPLItem.sFilenameFull || "" == cPLItem.sFilenameFull)
						cPLItem.sFilenameFull = cPLItem.sFilename;    // в дальнейшем будет ошибка, записанная в лог...
					aItems.Add(LivePLItem.PLItemGet(cPLItem));  // _ahStorages[cPLItem.sStorageName]
					//aItems.Add(new PLItem() { sName = _ahStorages[cPLItem.sStorageName] + cPLItem.sFilename, nTransDuration = nDur, nEndTransDuration = nEndDur });  // _ahStorages[cPLItem.sStorageName]
				}
			}
			return aItems;
		}
		void _ui_ctrTB_PlayList_TemplateStarted(object sender, EventArgs e)
		{
			if (IsAirGoingNow)
				DoAutomationOnStudioEnd();
			if (null != _cLPLWatcher)
			{
				_cLPLWatcher.TimerStart();
				ShowPL(false);
			}
			PLButtonsEnable(true);
		}
		void _ui_ctrTB_PlayList_TemplateStopped(object sender, EventArgs e)
		{
			_cPlayer.PlaylistItemsGetAsync(_cPlaylistPrevious, "PL_stopped");

			if (IsAirGoingNow)
				DoAutomationOnStudioBegin();

			if (null != _cLPLWatcher)
			{
				ClearLPL();
			}
			PLButtonsEnable(true);
		}
		void ClipsLog(List<IP.PlaylistItem> aLPL)
		{
			List<DBI.PlaylistItem> aItems = new List<DBI.PlaylistItem>();
			foreach (IP.PlaylistItem cLPL in aLPL)
			{
				if (cLPL._eType == PLIType.Clip)
					aItems.Add(new DBI.PlaylistItem { cAsset = new Asset { nID = cLPL._cClipSCR.nID, cFile = new File { cStorage = new Storage { } }, stVideo = new Video { } }, dtStartReal = cLPL.dtStartReal, dtStopReal = cLPL.dtStopReal, cFile = new File { cStorage = new Storage { } } });
				else if (cLPL._eType == PLIType.AdvBlockItem)
					aItems.Add(new DBI.PlaylistItem { cAsset = new Asset { nID = cLPL._cAdvertSCR.nAssetID, cFile = new File { cStorage = new Storage { } }, stVideo = new Video { } }, dtStartReal = cLPL.dtStartReal, dtStopReal = cLPL.dtStopReal, cFile = new File { cStorage = new Storage { } } });
			}
			if (0 < aItems.Count)
				_cDBI.ClipsBDLogAsync(_cPresetSelected.nID, aItems.ToArray());
		}
		void PLButtonsEnable(bool bIsEnable)
		{
			_ui_hlbtnAddAdverts.IsEnabled = bIsEnable;
			_ui_hlbtnAddClip.IsEnabled = bIsEnable;
			_ui_hlbtnAddFile.IsEnabled = bIsEnable;
		}
		#region .. playlist cm .
		private void _ui_dgPL_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_cSelectedLPLI = (LivePLItem)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
			}
			catch
			{
				_cSelectedLPLI = null;
			}
		}
		private void _ui_cmPL_Opened(object sender, RoutedEventArgs e)
		{
			_ui_cmPLDelete.Header = g.Common.sDelete + ": ";
			if (null == _cSelectedLPLI)
				_ui_cmPLDelete.IsEnabled = false;
			else
			{
				if (_ui_ctrTB_PlayList.eStatus == TemplateButton.Status.Preparing || (_ui_ctrTB_PlayList.eStatus == TemplateButton.Status.Started &&
					(_cLPLWatcher.cCurrentLPLItem.nID == _cSelectedLPLI.nID || _cLPLWatcher.cCurrentLPLItem.nID != _cSelectedLPLI.nID && DateTime.Now.AddSeconds(15) > _cSelectedLPLI.dtStart)))
				{
					_ui_cmPLDelete.IsEnabled = false;
				}
				else
				{
					if (PLIType.AdvBlockItem == _cSelectedLPLI.eType)
					{
						int nind = _aLivePLSingle.IndexOf(_cSelectedLPLI);
						for (int ni = nind; 0 <= ni; ni--)
							if (PLIType.JustString == (_cSelectedLPLI = _aLivePLSingle[ni]).eType)
								break;
					}
					if (PLIType.JustString == _cSelectedLPLI.eType)
						_cSelectedLPLI = _cSelectedLPLI.cBlock;

					_ui_cmPLDelete.IsEnabled = true;
					_ui_cmPLDelete.Header = g.Common.sDelete + ": \"" + _cSelectedLPLI.sName + "\"";
				}
			}
			_ui_cmPLDelete.Refresh();
		}
		private void _ui_cmPL_Delete(object sender, RoutedEventArgs e)
		{
			if (null != _cSelectedLPLI && _ui_ctrTB_PlayList.eStatus != TemplateButton.Status.Preparing)
			{
				if (null == _cLPLWatcher || (!_cLPLWatcher._bTimerStarted && _ui_ctrTB_PlayList.eStatus != TemplateButton.Status.Prepared))
				{
					DeleteSelectedFromPL();
				}
				else if (DateTime.Now.AddSeconds(15) < _cSelectedLPLI.dtStart || _ui_ctrTB_PlayList.eStatus == TemplateButton.Status.Prepared)
				{
					if (PLIType.AdvBlock == _cSelectedLPLI.eType)
					{
						List<IP.PlaylistItem> aItems = new List<IP.PlaylistItem>();
						foreach (LivePLItem cIt in _cSelectedLPLI.aItemsInThisBlock)
							if (cIt.eType != PLIType.JustString)
								aItems.Add(cIt);
						_cPlayer.PlaylistItemDeleteAsync((Item)_ui_ctrTB_PlayList.cItem, aItems.ToArray());
						_ui_ctrTB_PlayList.IsEnabled = false;
					}
					else if (PLIType.Clip == _cSelectedLPLI.eType || PLIType.File == _cSelectedLPLI.eType)
					{
						_cPlayer.PlaylistItemDeleteAsync((Item)_ui_ctrTB_PlayList.cItem, new IP.PlaylistItem[] { LivePLItem.PLItemGet(_cSelectedLPLI) });
						_ui_ctrTB_PlayList.IsEnabled = false;
					}
					DeleteSelectedFromPL();
				}
			}
		}
		private void DeleteSelectedFromPL()
		{
			if (null != _cSelectedLPLI)
			{
				if (PLIType.AdvBlock == _cSelectedLPLI.eType)
				{
					foreach (LivePLItem cIt in _cSelectedLPLI.aItemsInThisBlock)
					{
						cIt.dtStart = cIt.dtStartReal = cIt.dtStopReal = DateTime.MinValue;
						_aLivePLTotal.Remove(cIt);
					}
				}
				else if (PLIType.Clip == _cSelectedLPLI.eType || PLIType.File == _cSelectedLPLI.eType)
				{
					_aLivePLTotal.Remove(_cSelectedLPLI);
				}
				ShowPL();
			}
		}
		#endregion
		#endregion
		public void ShowPL()
		{
			ShowPL(true);
		}
		void ShowPL(bool bNeedRecalculate)
		{
			if (0 == _aLivePLTotal.Count && null != _cLPLWatcher)
			{
				_cLPLWatcher.Dispose();
			}
			if (0 < _aLivePLTotal.Count)
			{
				if (null == _cLPLWatcher)
					_cLPLWatcher = new LPLWatcher(this);
				if (bNeedRecalculate)
					_cLPLWatcher.PlaylistRecalculate();
				MakeSingleAndBlockedArrays();
			}
			else
			{
				_aLivePLSingle.Clear();
				_aLivePLWithBlocks.Clear();
			}
			if (g.Common.sShowDetails.ToLower() == _ui_hlbtnDetales.Content.ToString())
			{
				_ui_dgLivePL.ItemsSource = _aLivePLSingle;
				_ui_dgLivePL.ItemsSource = _aLivePLWithBlocks;
			}
			else
			{
				_ui_dgLivePL.ItemsSource = _aLivePLWithBlocks;
				_ui_dgLivePL.ItemsSource = _aLivePLSingle;
			}

			if (TemplateButton.Status.Started == _ui_ctrTB_PlayList.eStatus)
			{
				_ui_ctrTB_PlayList.btnPlay.IsEnabled = false;
				_ui_ctrTB_Template1Credits.IsEnabled = true;
				//_ui_ctrTB_Template1Plaques.IsEnabled = false;
			}
			else
			{
				_ui_ctrTB_Template1Credits.IsEnabled = false;
				_ui_ctrTB_Template1Plaques.IsEnabled = true;
				if (0 < _aLivePLTotal.Count && ((TextBlock)(_ui_ctrTB_PlayList.btnPlay.Content)).Text != "PREPARING")
					_ui_ctrTB_PlayList.btnPlay.IsEnabled = true;
				else if (TemplateButton.Status.Error != _ui_ctrTB_PlayList.eStatus)
				{
					_ui_ctrTB_PlayList.BreakStopedTimer();
					_ui_ctrTB_PlayList.btnPlay.IsEnabled = false;
				}
				//_ui_ctrTB_Credits.IsEnabled = false;
			}
		}
		void ClearLPL()
		{
			foreach (LivePLItem cLPLI in _aLivePLTotal)
				cLPLI.dtStart = cLPLI.dtStartReal = cLPLI.dtStopReal = DateTime.MinValue;
			_aLivePLTotal.Clear();
			ShowPL();
		}
		public void MakeSingleAndBlockedArrays()
		{
			if (0 == _aLivePLTotal.Count)
			{
				_aLivePLWithBlocks.Clear();
				_aLivePLSingle.Clear();
				return;
			}
			lock (_aLivePLTotal)
			{
				_aLivePLWithBlocks = LivePLItem.GetBlocksFromSingle(_aLivePLTotal);
				LivePLItem cCurrent = _cLPLWatcher == null || _cLPLWatcher.cCurrentLPLItem == null ? _aLivePLTotal[0] : _cLPLWatcher.cCurrentLPLItem;
				List<LivePLItem> aToRemove = new List<LivePLItem>();
				foreach (LivePLItem cLPLI in _aLivePLWithBlocks)
				{
					if (cLPLI._eType == PLIType.AdvBlock)
					{
						if (cLPLI.aItemsInThisBlock.Contains(cCurrent))
							break;
					}
					else
					{
						if (cLPLI == cCurrent)
							break;
					}
					aToRemove.Add(cLPLI);
				}
				foreach (LivePLItem cLPLI in aToRemove)
					_aLivePLWithBlocks.Remove(cLPLI);
				_aLivePLSingle = LivePLItem.GetSingleFromBlocks(_aLivePLWithBlocks);
			}
		}
		#endregion
		#region . timer .
		internal void RenewTimers()
		{
			_ui_lblCurrentDuration.Content = _cLPLWatcher.sCurrentDuration;
			_ui_lblCurrentName.Content = _cLPLWatcher.cCurrentLPLItem.sName;
			_ui_lblCurrentPast.Content = _cLPLWatcher.sCurrentPast;
			_ui_lblCurrentRemain.Content = _cLPLWatcher.sCurrentRemain;
			//_ui_lblTotalDuration.Content = _cLPLWatcher.sTotalDuration;
			_ui_lblTotalPast.Content = _cLPLWatcher.sTotalPast;
			_ui_lblTotalRemain.Content = _cLPLWatcher.sTotalRemain;
			if (1500 < _cLPLWatcher.nTotalRemain && Coloring.SCR.Timer.cTotalRemainNormalForegr != _ui_lblTotalRemain.Foreground)
				_ui_lblTotalRemain.Foreground = Coloring.SCR.Timer.cTotalRemainNormalForegr;
			else if (1500 >= _cLPLWatcher.nTotalRemain && 250 < _cLPLWatcher.nTotalRemain && Coloring.SCR.Timer.cTotalRemainPreWarningForegr != _ui_lblTotalRemain.Foreground)
				_ui_lblTotalRemain.Foreground = Coloring.SCR.Timer.cTotalRemainPreWarningForegr;
			else if (250 >= _cLPLWatcher.nTotalRemain && Coloring.SCR.Timer.cTotalRemainWarningForegr != _ui_lblTotalRemain.Foreground)
				_ui_lblTotalRemain.Foreground = Coloring.SCR.Timer.cTotalRemainWarningForegr;
		}
		internal void TimersOff()
		{
			_ui_lblCurrentDuration.Content = "";
			_ui_lblCurrentName.Content = "";
			_ui_lblCurrentPast.Content = "";
			_ui_lblCurrentRemain.Content = "";
			_ui_lblTotalDuration.Content = "";
			_ui_lblTotalPast.Content = "";
			_ui_lblTotalRemain.Content = "";
		}
		#endregion
		#region . Blocking .
		private void BlockSCR()
		{

		}
		#endregion

		public TemplateButton GetTemplateButton(string sFile)
		{
			foreach (Preset cPreset in _aPresets)
			{
				if (sFile == PathApprove(App.cPreferences.aTemplates.Get(Bind.preset_notice).sFile, cPreset))
					return _ui_ctrTB_Template1Plaques;
				foreach (TemplateButton cTB in _aTemplateButtons)
					if (sFile == PathApprove(cTB.sFile, cPreset))
						return cTB;
			}
			return null;
		}
		private void ItemsUpdate(Item[] aItems)
		{
			TemplateButton.Status eStatus;
			lock (_aTemplateButtons)
			{
				foreach (Item cItem in aItems)
				{
					eStatus = cItem.eStatus;  //.To<TemplateButton.Status>()
					if (cItem.eStatusPrevious != eStatus && TemplateButton.Status.Error != cItem.eStatusPrevious)
					{
						cItem.eStatusPrevious = eStatus;

						switch (eStatus)
						{
							case TemplateButton.Status.Started:
							case TemplateButton.Status.Prepared:
								_aTemplateButtons.First(o => null != o.cItem && o.cItem == cItem).eStatus = eStatus;
								break;
							case TemplateButton.Status.Stopped:
							case TemplateButton.Status.Error:
								TemplateButton cTemplateButton = _aTemplateButtons.First(o => null != o.cItem && o.cItem == cItem);
								cTemplateButton.eStatus = eStatus;
								((Item)cTemplateButton.cItem).Kill();
								cTemplateButton.cItemPrevious = cTemplateButton.cItem;
								cTemplateButton.cItem = null;
								if (_ui_ctrTB_Template1Plaques == cTemplateButton)
								{
									_ui_ctrTB_Template1Plaques.sFile = App.cPreferences.aTemplates.Get(Bind.preset_credits).sFile;
								}

								break;
							case TemplateButton.Status.Idle:
							default:
								break;
						}
					}
				}
			}
			if (_bStatusGotForFirstTime)
			{
				if (null != _sLogTB && "" != _sLogTB)
				{
					_cCues.WriteNoticeAsync("logTB: " + _sLogTB);
					helpers.sl.common.CookieSet("logTB", "");
				}
				//if (null == _ui_ctrTB_PlayList.cItem)
				_dlgProgress.Close();
				//else
				{
					//_cIG.PLItemsGetAsync(_nPLAliveID);
					//_nPLAliveID = 0;
				}
				_bStatusGotForFirstTime = false;
			}
		}//
		private string PathApprove(string sPath, Preset cPreset)
		{
			return sPath.Replace(App.cPreferences.sTemplatePresetMask, cPreset.sFolder).Replace(App.cPreferences.sTemplateChannelMask, cPreset.sChannel);
		}


	}
	#region . classes .
	public class LPLWatcher
	{
		static public SCR _cPage;
		public DateTime dtPLStarted;
		public bool _bTimerStarted = false;
		public long nCurrentRemain;
		public long nTotalRemain;
		public long nCurrentPast;
		public long nTotalPast;
		public long nTotalDuration;
		public string sCurrentRemain;
		public string sTotalRemain;
		public string sCurrentPast;
		public string sTotalPast;
		public string sTotalDuration
		{
			set
			{
				_cPage._ui_lblTotalDuration.Content = value;
			}
		}
		public string sCurrentDuration;
		private List<long> _aInTitled, _aOutTitled, _aRefreshed, _aSmokingTitred, _aAgeTitred;
		private List<string> _aWasAutomated;
		private List<PLITemplatePair> _aDontAutomate;
		private LivePLItem _cCurrentLPLItem = null;
		public LivePLItem cCurrentLPLItem
		{
			get
			{ return _cCurrentLPLItem; }
			set
			{
				if (value != _cCurrentLPLItem)
				{
					_cPreviousLPLItem = _cCurrentLPLItem;
					_cCurrentLPLItem = value;
				}
			}
		}
		private LivePLItem _cNextLPLItem;
		private LivePLItem _cPreviousLPLItem = null;
		public LivePLItem cNextLPLItem
		{
			get
			{ return _cNextLPLItem; }
		}
		private System.Windows.Threading.DispatcherTimer _cTimerForWatcher;
		private class PLITemplatePair
		{
			public LivePLItem cPLI;
			public scr.services.preferences.Template cTemplate;
			public PLITemplatePair()
			{ }
		}
		public LPLWatcher(SCR cPage)
		{
			_cTimerForWatcher = new System.Windows.Threading.DispatcherTimer();
			_cTimerForWatcher.Tick += new EventHandler(_cTimerForWatcher_Tick);
			_cTimerForWatcher.Interval = new System.TimeSpan(0, 0, 0, 0, 1000);
			_cPage = cPage;
			dtPLStarted = DateTime.MinValue;
			_aWasAutomated = new List<string>();
			_aDontAutomate = new List<PLITemplatePair>();
			_aInTitled = new List<long>();
			_aOutTitled = new List<long>();
			_aRefreshed = new List<long>();
			_aSmokingTitred = new List<long>();
			_aAgeTitred = new List<long>();
		}
		public void TimerStart()
		{
			try
			{
				if (!_bTimerStarted && 0 < _cPage._aLivePLTotal.Count)
				{
					_bTimerStarted = true;
					if (DateTime.MinValue == _cPage._aLivePLTotal[0].dtStartReal)
					{
						dtPLStarted = DateTime.Now;
					}
					else
					{
						dtPLStarted = _cPage._aLivePLTotal[0].dtStartReal;
						CurrentParamsSet(false);
					}
					PlaylistRecalculate();
					_cTimerForWatcher.Start();
				}
			}
			catch (Exception ex)
			{
				_cPage.WritePlayerError(ex);
			}
		}
		public long GetCurrentClipAssetID
		{
			get
			{
				long nRetVal = 0;
				if (null != cCurrentLPLItem && null != cCurrentLPLItem.cClipSCR)
					nRetVal = cCurrentLPLItem.cClipSCR.nID;
				return nRetVal;
			}
		}
		public void PlaylistRecalculate()
		{
			if (null == _cPage._aLivePLTotal || 0 == _cPage._aLivePLTotal.Count)
			{
				nTotalDuration = 0;
				sTotalDuration = "";
			}
			lock (_cPage._aLivePLTotal)
			{
				long nRetVal = 0;
				if (DateTime.MinValue != _cPage._aLivePLTotal[0].dtStartReal)
					dtPLStarted = _cPage._aLivePLTotal[0].dtStartReal;
				DateTime dtLastStopReal = DateTime.MinValue;
				DateTime dtPrevousStop = dtPLStarted;
				foreach (LivePLItem cLPLI in _cPage._aLivePLTotal)
				{
					if (DateTime.MinValue < dtPLStarted)
						cLPLI.dtStart = dtPrevousStop;
					dtPrevousStop = cLPLI.dtStopReal == DateTime.MinValue ? dtPrevousStop.AddMilliseconds(cLPLI._nFramesQty * 40) : cLPLI.dtStopReal; //FPS
					if (0 == nRetVal && DateTime.MinValue != cLPLI.dtStopReal)
					{
						dtLastStopReal = cLPLI.dtStopReal;
						continue;
					}
					if (0 == nRetVal && DateTime.MinValue != dtLastStopReal)
						nRetVal = (long)(dtLastStopReal.Subtract(dtPLStarted).TotalMilliseconds) / 40; //FPS
					nRetVal += cLPLI._nFramesQty;
				}
				nTotalDuration = nRetVal;
				sTotalDuration = nRetVal.ToFramesString(false);
			}
		}
		private void CurrentParamsSet(bool bRefreshOnCurrentChanged)
		{
			if (DateTime.MaxValue == dtPLStarted)
				return;
			long nTotalDelta = (long)(DateTime.Now.Subtract(dtPLStarted).TotalMilliseconds) / 40; //FPS
			long nDelta = 0;
			nTotalPast = nTotalDelta;
			nTotalRemain = nTotalDuration - nTotalPast;
			lock (_cPage._aLivePLTotal)
				foreach (LivePLItem cLPLI in _cPage._aLivePLTotal)
				{
					if (DateTime.MinValue != cLPLI.dtStopReal)
					{
						continue;
					}
					if (0 == nDelta)
						nDelta = (long)(DateTime.Now.Subtract(cLPLI.dtStart).TotalMilliseconds) / 40; //FPS
					nDelta -= cLPLI._nFramesQty;
					if (0 > nDelta)
					{
						nCurrentRemain = -nDelta;
						nCurrentPast = cLPLI._nFramesQty - nCurrentRemain;
						if (cCurrentLPLItem != cLPLI)
						{
							cCurrentLPLItem = cLPLI;
							if (bRefreshOnCurrentChanged)
								_cPage.ShowPL();
							int nCurIndx = _cPage._aLivePLTotal.IndexOf(cLPLI);
							if (_cPage._aLivePLTotal.Count > nCurIndx + 1)
								_cNextLPLItem = _cPage._aLivePLTotal[nCurIndx + 1];
							else
								_cNextLPLItem = null;
						}
						return;
					}
				}
		}
		void ConvertToString()
		{
			sCurrentRemain = nCurrentRemain.ToFramesString(false);
			sTotalRemain = nTotalRemain.ToFramesString(false);
			sCurrentPast = nCurrentPast.ToFramesString(false);
			sTotalPast = nTotalPast.ToFramesString(false);
			sCurrentDuration = cCurrentLPLItem._nFramesQty.ToFramesString(false);
		}
		void _cTimerForWatcher_Tick(object sender, EventArgs e)
		{
			try
			{
				scr.services.preferences.Template[] aTemplatesToAutomate = null;
				if (TemplateButton.Status.Started == _cPage._ui_ctrTB_PlayList.eStatus)
				{
					CurrentParamsSet(true);
					if (PLIType.Clip == cCurrentLPLItem.eType && 1500 < nCurrentPast && !_aRefreshed.Contains(cCurrentLPLItem.nID))   // 1500
					{                                                   // загрузка плейлиста из IG на клипах
						_cPage._cPlayer_PlaylistItemsGet();
						_aRefreshed.Add(cCurrentLPLItem.nID);
					}
					if ((PLIType.File == cCurrentLPLItem.eType || PLIType.AdvBlockItem == cCurrentLPLItem.eType) && 220 < cCurrentLPLItem._nFramesQty && 100 < nCurrentPast && !_aRefreshed.Contains(cCurrentLPLItem.nID))
					{													// загрузка плейлиста из IG на рекламе
						_cPage._cPlayer_PlaylistItemsGet();
						_aRefreshed.Add(cCurrentLPLItem.nID);
					}
					if (PLIType.AdvBlockItem == cCurrentLPLItem.eType)   // блокировка скипа
						_cPage._ui_ctrTB_PlayList.bSkipBtnIsEnabled = false;
					else
						_cPage._ui_ctrTB_PlayList.bSkipBtnIsEnabled = true;
					ConvertToString();
					_cPage.RenewTimers();


					if (null != _cPage._cShiftCurrent && _cPage.IsAirGoingNow)
					{ // если идет прямой эфир
						if (PLIType.AdvBlockItem == cCurrentLPLItem.eType)
						{ // идет реклама
							// надо полноценно заменять ручное управление на авто через префы. Чат и титры тоже фигарить как-то так.
							// меняем. пока оставил разделение на ифы.

						}

						if (PLIType.File == cCurrentLPLItem.eType)
						{// идет пользовательский файл какой-то

						}

						if (PLIType.Clip == cCurrentLPLItem.eType && TemplateButton.Status.Started == _cPage._ui_ctrTB_PlayList.eStatus)
						{ // идет клип в плейлисте


						}
						AutomateTemplates();   //TODO потом снабдить обработкой конфликтов
					}
				}
			}
			catch (Exception ex)
			{
				_cPage.WritePlayerError(ex);
			}
		}
		private void AutomateTemplates()
		{
			long nPresetID = _cPage._cPresetSelected.nID;
			int nDurSafe;
			string sHash;
			scr.services.preferences.Parameters cParam;
			foreach (scr.services.preferences.Template cT in App.cPreferences.aTemplates.Where(o => o.aOffsets != null))
			{
				if (null != _aDontAutomate.FirstOrDefault(o => o.cPLI.nID == cCurrentLPLItem.nID && o.cTemplate.eBind.ToString() == cT.eBind.ToString()))
					continue;

				cParam = cT.aParameters.Get(nPresetID);

				if (null == cParam)
					continue;

				foreach (Offset cOffset in cT.aOffsets)
				{
					if (cOffset.nPresetID != nPresetID || !cParam.bIsEnabled || !cOffset.IsOffsetFeats(_cPage._cPresetSelected.nID, cCurrentLPLItem, cNextLPLItem, _cPreviousLPLItem))
						continue;

					nDurSafe = int.MaxValue == cOffset.nDurationSafe ? 0 : cOffset.nDurationSafe;
					if (cCurrentLPLItem._nFramesQty <= nDurSafe)
						continue;

					if (cOffset.bDoOnlyIfLast && cOffset.IsOffsetFeats(_cPage._cPresetSelected.nID, cNextLPLItem, null, cCurrentLPLItem))
						continue;

					if (int.MaxValue > cOffset.nOffsetIn && (cOffset.nOffsetIn >= 0 && cOffset.nOffsetIn < nCurrentPast || cOffset.nOffsetIn < 0 && -cOffset.nOffsetIn > nCurrentRemain))
					{
						sHash = cCurrentLPLItem.nID + "_" + cT.eBind.ToString() + "_" + "in=" + cOffset.nOffsetIn.ToString();
						if (!_aWasAutomated.Contains(sHash) && cParam.bAutostart)
						{
							_aWasAutomated.Add(sHash);
							_cPage.StartTemplate(cT);
						}
					}
					if (int.MaxValue > cOffset.nOffsetOut && (cOffset.nOffsetOut >= 0 && cOffset.nOffsetOut < nCurrentPast || cOffset.nOffsetOut < 0 && -cOffset.nOffsetOut > nCurrentRemain))
					{
						sHash = cCurrentLPLItem.nID + "_" + cT.eBind.ToString() + "_" + "out=" + cOffset.nOffsetOut.ToString();
						if (!_aWasAutomated.Contains(sHash))
						{
							_aWasAutomated.Add(sHash);
							_cPage.StopTemplate(cT);
						}
					}
				}
			}
		}
		public void TurnOffTemplateOnCurrentPLI(TemplateButton cTB)
		{
			services.preferences.Template cT = App.cPreferences.aTemplates.FirstOrDefault(o => o.sFile == cTB.sFile);
			if (null != cT && null != cCurrentLPLItem)
				_aDontAutomate.Add(new PLITemplatePair() { cPLI = cCurrentLPLItem, cTemplate = cT });
		}
		internal void Dispose()
		{
			//if (TemplateButton.Status.Started == _cPage._ui_ctrTB_Template1Chat.eStatus)
			//    _cPage._ui_ctrTB_Template1Chat.Click();
			_cPage.TimersOff();
			_cTimerForWatcher.Stop();
			_cPage._cLPLWatcher = null;
		}
	}
	//может хранить как одиночный item так и целый блок как item - для облегчения переключения между сокращенным и подробным режимами
	public partial class LivePLItem : IP.PlaylistItem
	{
		public string sDuration { get; set; }
		public string sStart { get; set; }
		public PLIType eType
		{ get { return _eType; } }
		public string sClassName
		{
			get
			{
				switch (eType)
				{
					case PLIType.AdvBlock:
						return "";
						break;
					case PLIType.AdvBlockItem:
						return _cAdvertSCR.sClassName;
						break;
					case PLIType.Clip:
						return _cClipSCR.sClassName;
						break;
					case PLIType.File:
						return "";
						break;
					case PLIType.JustString:
						return "";
						break;
					default:
						return null;
						break;
				}
			}
		}
		public LivePLItem cBlock;   // для отсылки на блок
		private List<LivePLItem> _aItemsInThisBlock = null;
		public List<LivePLItem> aItemsInThisBlock
		{
			set
			{
				value[0].bIsFirstItemInBlock = true;
				_aItemsInThisBlock = value;
				sName = g.SCR.sNotice21;
				sFilename = "";
				if (DateTime.MinValue < value[1].dtStart)
					dtStart = value[1].dtStart;
				if (null != value[0]._cAdvertSCR)
					sName += value[0]._cAdvertSCR.sStartPlanned;
				foreach (LivePLItem cLPLI in _aItemsInThisBlock)
				{
					if (PLIType.JustString == cLPLI._eType)
						continue;
					cLPLI.cBlock = this;
					nFramesQty += cLPLI.nFramesQty;
					if (!cLPLI.bFileExist)
						bFileExist = false;
				}
			}
			get { return _aItemsInThisBlock; }
		}
		public void RemoveBlockItem(LivePLItem cBlockItem)
		{
			if (null != _aItemsInThisBlock)
			{
				_aItemsInThisBlock.Remove(cBlockItem);
				nFramesQty = 0;
				LivePLItem tmp = _aItemsInThisBlock[0];
				foreach (LivePLItem cLPLI in _aItemsInThisBlock)
					if (PLIType.JustString != cLPLI.eType)
						nFramesQty += cLPLI.nFramesQty;
				tmp.nFramesQty = nFramesQty;
			}
		}
		public IP.Advertisement cAdvertSCR
		{
			set
			{
				_cAdvertSCR = value;
				string sSpace = _eType == PLIType.AdvBlockItem ? "        " : "";
				sName = sSpace + value.sName;
				sFilename = value.sFilename;
				sFilenameFull = value.sStoragePath + value.sFilename;
				nFramesQty = value.nFramesQty;
				sStorageName = value.sStorageName;
				bFileExist = value.bFileExist;
			}
			get
			{
				return _cAdvertSCR;
			}
		}
		public IP.Clip cClipSCR
		{
			set
			{
				_cClipSCR = value;
				sName = value.sName;
				sFilename = value.sFilename;
				sFilenameFull = value.sStoragePath + value.sFilename;
				sStorageName = value.sStorageName;
				nFramesQty = value.nFramesQty;
			}
			get
			{
				return _cClipSCR;
			}
		}
		public long nFramesQty
		{
			get
			{
				return _nFramesQty;
			}
			set
			{
				_nFramesQty = value;
				sDuration = "   " + value.ToFramesString();
			}
		}
		private DateTime _dtStart;
		public DateTime dtStart
		{
			set
			{
				if (PLIType.JustString != _eType)
					sStart = value.ToString("HH:mm:ss");     // yyyy.MM.dd hh:mm:ss
				_dtStart = value;
			}
			get
			{
				if (DateTime.MinValue == dtStartReal)
					return _dtStart;
				else
					return dtStartReal;
			}
		}
		private static int _nMaxID = 1;

		public LivePLItem()
		{
			bFileExist = true;
			dtStartReal = DateTime.MinValue;
			dtStopReal = DateTime.MinValue;
		}
		public LivePLItem(IP.Advertisement cPLI)
			: this()
		{
			_eType = PLIType.AdvBlockItem;
			cAdvertSCR = cPLI;
		}
		public LivePLItem(string sName, string sFilename, bool bIsImage)
			: this()
		{
			_eType = PLIType.File;
			bFileIsImage = bIsImage;
			this.sName = sName;
			this.sFilename = sFilename;
		}
		public LivePLItem(IP.Clip cClip)
			: this()
		{
			cClipSCR = cClip;
			_eType = PLIType.Clip;
		}
		public LivePLItem(List<LivePLItem> aBlock)
			: this()
		{
			aItemsInThisBlock = aBlock;
			List<LivePLItem> tmp = new List<LivePLItem>();
			tmp.Add(new LivePLItem(sName, _nFramesQty, this));
			tmp.AddRange(_aItemsInThisBlock);
			_aItemsInThisBlock = tmp;
			_eType = PLIType.AdvBlock;
		}
		public LivePLItem(string sName, long nDuration, LivePLItem cLPLI)
			: this()
		{
			this.sName = sName;
			cBlock = cLPLI;
			nFramesQty = nDuration;
			_eType = PLIType.JustString;
		}

		public void SetNewId()
		{
			if (0 == nID)
				nID = _nMaxID++;
		}
		static public List<LivePLItem> SetIDs(List<LivePLItem> aLPLIs)
		{
			foreach (LivePLItem cLPLI in aLPLIs)
				cLPLI.SetNewId();
			return aLPLIs;
		}
		static public LivePLItem SetID(LivePLItem cLPLI)
		{
			cLPLI.SetNewId();
			return cLPLI;
		}
		static public LivePLItem LivePLItemGet(IP.PlaylistItem cPLI)
		{
			LivePLItem cLPLI;
			switch (cPLI._eType)
			{
				case PLIType.AdvBlockItem:
					cLPLI = new LivePLItem(cPLI._cAdvertSCR);
					cLPLI._cClipSCR = cPLI._cClipSCR;
					break;
				case PLIType.Clip:
					cLPLI = new LivePLItem(cPLI._cClipSCR);
					cLPLI._cAdvertSCR = cPLI._cAdvertSCR;
					break;
				case PLIType.File:
					cLPLI = new LivePLItem(cPLI.sName, cPLI.sFilename, cPLI.bFileIsImage);
					break;
				case PLIType.AdvBlock:
				case PLIType.JustString:
				default:
					return null;
			}
			cLPLI.bIsFirstItemInBlock = cPLI.bIsFirstItemInBlock;
			cLPLI.bFileExist = cPLI.bFileExist;
			cLPLI.nFramesQty = cPLI._nFramesQty;
			cLPLI.sStorageName = cPLI.sStorageName;
			cLPLI.nTransDuration = cPLI.nTransDuration;
			cLPLI.nEndTransDuration = cPLI.nEndTransDuration;
			cLPLI.dtStartReal = cPLI.dtStartReal;
			cLPLI.dtStopReal = cPLI.dtStopReal;
			cLPLI.nAtomHashCode = cPLI.nAtomHashCode;
			cLPLI.nEffectID = cPLI.nEffectID;
			cLPLI.sFilenameFull = cPLI.sFilenameFull;
			cLPLI.nID = cPLI.nID;
			cLPLI.bFileIsImage = cPLI.bFileIsImage;
			return cLPLI;
		}
		static public IP.PlaylistItem PLItemGet(LivePLItem cLPLI)
		{
			IP.PlaylistItem cRetVal = new IP.PlaylistItem()
			{
				_cAdvertSCR = cLPLI._cAdvertSCR,
				_cClipSCR = cLPLI._cClipSCR,
				_eType = cLPLI._eType,
				_nFramesQty = cLPLI._nFramesQty,
				bFileExist = cLPLI.bFileExist,
				bIsFirstItemInBlock = cLPLI.bIsFirstItemInBlock,
				dtStartReal = cLPLI.dtStartReal,
				dtStopReal = cLPLI.dtStopReal,
				nAtomHashCode = cLPLI.nAtomHashCode,
				nEffectID = cLPLI.nEffectID,
				nEndTransDuration = cLPLI.nEndTransDuration,
				nTransDuration = cLPLI.nTransDuration,
				sFilename = cLPLI.sFilename,
				sFilenameFull = cLPLI.sFilenameFull,
				sName = cLPLI.sName,
				sStorageName = cLPLI.sStorageName,
				nID = cLPLI.nID,
				bFileIsImage=cLPLI.bFileIsImage
			};
			return cRetVal;
		}
		static public List<LivePLItem> GetBlocksFromSingle(List<LivePLItem> aSingle)
		{
			List<LivePLItem> aRetVal = new List<LivePLItem>();
			List<LivePLItem> aBlock = null;
			int nIndex = 0;
			while (nIndex < aSingle.Count)
			{
				if (aSingle[nIndex].bIsFirstItemInBlock)
				{
					aBlock = new List<LivePLItem>();
					aBlock.Add(aSingle[nIndex++]);
					while (nIndex < aSingle.Count && !aSingle[nIndex].bIsFirstItemInBlock && aSingle[nIndex]._eType == PLIType.AdvBlockItem)
					{
						aBlock.Add(aSingle[nIndex++]);
					}
					aRetVal.Add(new LivePLItem(aBlock));
				}
				else
				{
					aRetVal.Add(aSingle[nIndex++]);
				}
			}

			return aRetVal;
		}
		static public List<LivePLItem> GetSingleFromBlocks(List<LivePLItem> aBlocks)
		{
			List<LivePLItem> aRetVal = new List<LivePLItem>();
			foreach (LivePLItem cLPLI in aBlocks)
			{
				if (cLPLI._eType == PLIType.AdvBlock)
					aRetVal.AddRange(cLPLI._aItemsInThisBlock);
				else if (cLPLI._eType == PLIType.JustString)
					continue;
				else
					aRetVal.Add(cLPLI);
			}
			return aRetVal;
		}
		internal static List<LivePLItem> RemoveJustStrings(List<LivePLItem> _aSelectedClips)
		{
			List<LivePLItem> aRetVal = new List<LivePLItem>();
			foreach (LivePLItem cLPLI in _aSelectedClips)
			{
				if (cLPLI._eType != PLIType.JustString)
					aRetVal.Add(cLPLI);
			}
			return aRetVal;
		}
	}
	#endregion
}
