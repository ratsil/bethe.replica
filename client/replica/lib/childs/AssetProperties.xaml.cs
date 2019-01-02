using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Controls;
using swc = System.Windows.Controls;

using controls.childs.sl;
using controls.extensions.sl;
using controls.sl;
using helpers.extensions;
using replica.sl;
using replica.sl.ListProviders;
using helpers.replica.services.dbinteract;
using dbi = global::helpers.replica.services.dbinteract;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class AssetProperties : ChildWindow
	{
		private bool _bCanClose;

		private Progress _dlgProgress;
        private MsgBox _cMsgBox;
		private DBInteract _cDBI;
		private Asset _cAsset;
		public AssetSL cAsset
		{
			get
			{
				return AssetSL.GetAssetSL(_cAsset);
			}
		}
		public long nThisAssetID;
        private bool _bReadOnly;
        public bool bReadOnly    //TODO  сейчас пока просто не вносятся изменения, а по-хорошему надо бы блокировать возможность чо-то делать
        {
            get
			{
				return _bReadOnly;
			}
            set
            {
                _bReadOnly = value;
                if (_bReadOnly)
                    _ui_btnSave.IsEnabled = false;
            }
        }
        public string sDefaultFileStorageName;
		public AssetSL[] _aAssets = null;
		private AssetSL _cParent; // если добавление ассета происходит из-под родителя
		public AssetSL cParent
		{
			get
			{
				return _cParent;
			}
			set
			{
				_cParent = value;
				if (null == _cParent)
					_ui_tbParent.Text = "no parent";
				else
					_ui_tbParent.Text = _cParent.sName;
			}
		}
		private dbi.Type _cAssetType;
		public dbi.Type cAssetType
		{
			get
			{
				return _cAssetType;
			}
			set
			{
				_cAssetType = value;
				if (null == _cAssetType)
                    _ui_tbProgramType.Text = g.Helper.sPart.ToUpper();
				else
					switch (_cAssetType.eType)
					{
						case AssetType.part:
                            _ui_tbProgramType.Text = g.Helper.sPart.ToUpper();
							break;
						case AssetType.episode:
                            _ui_tbProgramType.Text = g.Helper.sEpisode.ToUpper();
							break;
						case AssetType.series:
                            _ui_tbProgramType.Text = g.Helper.sSeries.ToUpper();
							break;
					}
				if (null != _cAssetType && _cAssetType.eType != AssetType.part)
				{
					_ui_chkbxFile.IsChecked = false;
					_ui_tcFile.IsEnabled = false;
					_ui_chkbxFile.IsEnabled = false;
					_ui_chkbxAssetToPL.IsEnabled = false;
					_ui_ctrClasses.IsEnabled = false;
					_ui_tcClips.IsEnabled = false;
					_ui_tcChatInOuts.IsEnabled = false;
					_ui_tcCustom.IsEnabled = false;
				}
				if (null != _cAssetType && _cAssetType.eType == AssetType.series)
                    _ui_ctrClasses.IsEnabled = true;
			}
		}
		private AssetType _eAssetType
		{
			get
			{
				if (null != _cAssetType)
					return _cAssetType.eType;
				else
					return AssetType.part;
			}
		}
        private Clip _cClip;
		private Program _cProgram;
		private Advertisement _cAdvertisement;
        private Design _cDesign;
        private CustomValue _cCustomValueCurrent;
        //private System.Windows.Threading.DispatcherTimer _cTimerForCommandResult;
        private bool _bIs_ui_tbNameEditedByUser = false;
        private bool _bIs_ui_tbCuesArtistEditedByUser = false;
        private bool _bNameEditedByUser_Busy = false;
        private bool _bArtistEditedByUser_Busy = false;
        private List<IdNamePair> _aWordsFor_ui_dgCustomValues;
		private HaulierDialog ui_dlgArtists;
		private DateTime _dtNextMouseClickForDoubleClick;
		private ClipsFragment _cRAOIForDoubleClick;


		public AssetProperties()
		{
			InitializeComponent();
			_bCanClose = true;  
			_dlgProgress = new Progress();
            _cMsgBox = new MsgBox();
			_cDBI = new DBInteract();
			this.Loaded += new RoutedEventHandler(AssetProperties_Loaded);
			//role:replica_assets
            _cDBI.VideoTypeGetCompleted += new EventHandler<VideoTypeGetCompletedEventArgs>(_cDBI_VideoTypeGetCompleted);
			_cDBI.ClipGetCompleted += new EventHandler<ClipGetCompletedEventArgs>(_cDBI_ClipGetCompleted);
			_cDBI.AdvertisementGetCompleted += new EventHandler<AdvertisementGetCompletedEventArgs>(_cDBI_AdvertisementGetCompleted);
			_cDBI.ArtistsGetCompleted += new EventHandler<ArtistsGetCompletedEventArgs>(_cDBI_ArtistsGetCompleted);
			_cDBI.ArtistsLoadCompleted += new EventHandler<ArtistsLoadCompletedEventArgs>(_cDBI_ArtistsLoadCompleted);
			_cDBI.StylesGetCompleted += new EventHandler<StylesGetCompletedEventArgs>(_cDBI_StylesGetCompleted);
			_cDBI.StylesLoadCompleted += new EventHandler<StylesLoadCompletedEventArgs>(_cDBI_StylesLoadCompleted);
			_cDBI.RotationsGetCompleted += new EventHandler<RotationsGetCompletedEventArgs>(_cDBI_RotationsGetCompleted);
			_cDBI.PalettesGetCompleted += new EventHandler<PalettesGetCompletedEventArgs>(_cDBI_PalettesGetCompleted);
			_cDBI.SexGetCompleted += _cDBI_SexGetCompleted;
			_cDBI.SoundsGetCompleted += new EventHandler<SoundsGetCompletedEventArgs>(_cDBI_SoundsGetCompleted);
            _cDBI.DesignGetCompleted += new EventHandler<DesignGetCompletedEventArgs>(_cDBI_DesignGetCompleted);
			_cDBI.ArtistsCueNameGetCompleted += new EventHandler<ArtistsCueNameGetCompletedEventArgs>(_cDBI_ArtistsCueNameGetCompleted);
			_cDBI.AssetParametersToPlaylistSaveCompleted += new EventHandler<AssetParametersToPlaylistSaveCompletedEventArgs>(_cDBI_AssetParametersToPlaylistSaveCompleted);
			//role:replica_programs
			_cDBI.FrequencyOfOccurrenceCompleted += new EventHandler<FrequencyOfOccurrenceCompletedEventArgs>(_cDBI_FrequencyOfOccurrenceCompleted);
			_cDBI.ClassesGetCompleted += new EventHandler<ClassesGetCompletedEventArgs>(_cDBI_ClassesGetCompleted);
			_cDBI.AssetVideoTypeGetCompleted += new EventHandler<AssetVideoTypeGetCompletedEventArgs>(_cDBI_AssetVideoTypeGetCompleted);
			_cDBI.ProgramGetCompleted += new EventHandler<ProgramGetCompletedEventArgs>(_cDBI_ProgramGetCompleted);
			_cDBI.ChatInOutsGetCompleted += new EventHandler<ChatInOutsGetCompletedEventArgs>(_cDBI_ChatInOutsGetCompleted);
			_cDBI.CustomsLoadCompleted += new EventHandler<CustomsLoadCompletedEventArgs>(_cDBI_CustomsLoadCompleted);
			//role:replica_programs_full
			_cDBI.ProgramSaveCompleted += new EventHandler<ProgramSaveCompletedEventArgs>(_cDBI_ProgramSaveCompleted);
			_cDBI.ChatInOutsSaveCompleted += new EventHandler<ChatInOutsSaveCompletedEventArgs>(_cDBI_ChatInOutsSaveCompleted);
			//role:replica_assets_full
			_cDBI.ClipSaveCompleted += new EventHandler<ClipSaveCompletedEventArgs>(_cDBI_ClipSaveCompleted);
            _cDBI.PersonSaveCompleted += new EventHandler<PersonSaveCompletedEventArgs>(_cDBI_PersonSaveCompleted);
            _cDBI.AdvertisementSaveCompleted += new EventHandler<AdvertisementSaveCompletedEventArgs>(_cDBI_AdvertisementSaveCompleted);
            _cDBI.DesignSaveCompleted += new EventHandler<DesignSaveCompletedEventArgs>(_cDBI_DesignSaveCompleted);
			

			_ui_chkbxFile.Checked+=new RoutedEventHandler(_ui_chkbxFile_CheckedChanged);
			_ui_chkbxFile.Unchecked+=new RoutedEventHandler(_ui_chkbxFile_CheckedChanged);
			_ui_ctrRecalcFileDur.FileChanged += new controls.replica.sl.RecalculateFileDuration.OnFileChanged(_ui_ctrRecalcFileDur_FileChanged);
            this.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_DateTime_KeyDown), true);
            _ui_grdProgram.Visibility = System.Windows.Visibility.Collapsed;
            _ui_spClip.Visibility = System.Windows.Visibility.Collapsed;
            _ui_spAdvertisement.Visibility = System.Windows.Visibility.Collapsed;
            _ui_spDesign.Visibility = System.Windows.Visibility.Collapsed;
			_ui_dgClips.CellEditEnded += new EventHandler<DataGridCellEditEndedEventArgs>(_ui_dgClips_CellEditEnded);
			_ui_dgClips.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgClips_MouseLeftButtonDown), true);

			cAssetType = new dbi.Type() { nID = -1, eType = AssetType.part };
			cParent = null;
			_cAsset = null;
			_cClip = null;
			_cProgram = null;
			_cAdvertisement = null;
            _cDesign = null;
            //_cTimerForCommandResult = null;
            _aWordsFor_ui_dgCustomValues = new List<IdNamePair>();
            _ui_tiFile.Header = "    " + (string)_ui_tiFile.Header;
        }


		void _ui_dgClips_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
		{
			
		}
		void _ui_dgClips_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				FrameworkElement cFEClick = (FrameworkElement)((RoutedEventArgs)(e)).OriginalSource;
				if (cFEClick.Parent is DataGridCell)
				{
					if (_dtNextMouseClickForDoubleClick < DateTime.Now)
					{
						_dtNextMouseClickForDoubleClick = DateTime.Now.AddMilliseconds(400);
						_cRAOIForDoubleClick = (ClipsFragment)cFEClick.DataContext;
					}
					else
					{
						FrameworkElement cFEcolumn2 = _ui_dgClips.Columns[2].GetCellContent(_cRAOIForDoubleClick);
						_dtNextMouseClickForDoubleClick = DateTime.MinValue;
						if (_cRAOIForDoubleClick == (ClipsFragment)cFEClick.DataContext && cFEcolumn2 == cFEClick)   // значит был даблклик на этом же объекте
						{
							TimeCodeEnter dlgCodeEnter = new TimeCodeEnter();
							dlgCodeEnter.nInitialCode = _cRAOIForDoubleClick.nFramesQty;
							dlgCodeEnter.Closed += new EventHandler(dlgCodeEnter_Closed);
							dlgCodeEnter.Show();
						}
					}
				}
			}
			catch { };
		}
		void dlgCodeEnter_Closed(object sender, EventArgs e)
		{
			TimeCodeEnter dlgCodeEnter = (TimeCodeEnter)sender;
			dlgCodeEnter.Closed -= dlgCodeEnter_Closed;
			if (true==dlgCodeEnter.DialogResult)
			{
				long nTimeCode = dlgCodeEnter.nResultCode;
				_cRAOIForDoubleClick = new ClipsFragment() { cClip = _cRAOIForDoubleClick.cClip, nFramesQty = nTimeCode };
				List<ClipsFragment> aClipsFragment = (List<ClipsFragment>)_ui_dgClips.ItemsSource;
				ClipsFragment cRAOI = aClipsFragment.FirstOrDefault(o => o.cClip.nID == _cRAOIForDoubleClick.cClip.nID);
				if (null != cRAOI)
					cRAOI.nFramesQty = nTimeCode;
			}
		}

		void AssetProperties_Loaded(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();  //выключатель тут: _cDBI_CustomsLoadCompleted
			_ui_tbName.Focus();
		}
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			_dlgProgress.Close();
			_cMsgBox.Close();
			base.OnClosing(e);
		}
		//protected override void OnClosed(EventArgs e)
		//{
		//    _dlgProgress.Show();
		//    _cDBI.DBCredentialsUnsetAsync();
		//}
		//void _cDBI_DBCredentialsUnsetCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		//{
		//    try
		//    {
		//        base.OnClosed(e);
		//        _dlgProgress.Close();
		//    }
		//    catch { }
		//}
		
#region event handlers
    #region UI 
        // Временно из-за кредентиалсов ебучих
        private void _ui_btnSave_Click(object sender, RoutedEventArgs e)
        {
			Save_Click();
        }
        private void Save_Click()
		{
			try
			{
				try
				{
					if (-1 == _cAsset.nID || _cAsset.sName != _ui_tbName.Text)
						if (null != _aAssets.FirstOrDefault(o => o.sName == _ui_tbName.Text))
                            throw new Exception(g.Common.sErrorItemNameExists + "...");
					if (_ui_tcFile.IsEnabled)
						_ui_ctrRecalcFileDur.IsInputCorrect();
				}
				catch (Exception ex)
				{
					_cMsgBox.ShowError(ex.Message);
					return;
				}
				_bCanClose = IsInputCorrect();
				if (!_bCanClose)
				{
                    _cMsgBox.ShowError(g.Common.sErrorWrongFields + "!");   //ch=child 
					return;
				}
				switch (_cAsset.stVideo.cType.sName)
				{
					case "clip":
						ClipApply();
                        _cDBI.ClipSaveAsync(_cClip);
						break;
					case "design":
                        DesignApply();
                        _cDBI.DesignSaveAsync(_cDesign);
                        break;
                    case "advertisement":
                        AdvertisementApply();
                        _cDBI.AdvertisementSaveAsync(_cAdvertisement);
						break;
					case "program":
						ProgramApply();
						if (access.scopes.programs.bCanUpdate)
							_cDBI.ProgramSaveAsync(_cProgram);
						break;
					default:
						throw new ArgumentException(g.Replica.sErrorAssetProperties1 + ":" + _cAsset.stVideo.cType.sName);
				}
				//this.DialogResult = true;
			}
			catch (Exception ex)
			{
				_cMsgBox.ShowError(ex);
				_bCanClose = false;
                //this.DialogResult = false;
			}
		}
		private void _ui_btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
		private void _ui_cbCuesArtist_CheckedChanged(object sender, RoutedEventArgs e)
		{
			_ui_tbCuesArtist.IsEnabled = (bool)_ui_cbCuesArtist.IsChecked;
            if (_ui_tbCuesArtist.IsEnabled)
                _ui_tbCuesArtist.Mark();
            else
				_ui_tbCuesArtist.Background = Coloring.Notifications.cTextBoxInactive;
            _ui_cbCuesArtist.Mark(_ui_tbCuesArtist.Tag);
		}
		private void _ui_cbCuesSong_CheckedChanged(object sender, RoutedEventArgs e)
		{
			_ui_tbCuesSong.IsEnabled = (bool)_ui_cbCuesSong.IsChecked;
            if (_ui_tbCuesSong.IsEnabled)
                _ui_tbCuesSong.Mark();
            else
				_ui_tbCuesSong.Background = Coloring.Notifications.cTextBoxInactive;
            _ui_cbCuesSong.Mark(_ui_tbCuesSong.Tag);
		}
		private void _ui_cbCuesAlbum_CheckedChanged(object sender, RoutedEventArgs e)
		{
			_ui_tbCuesAlbum.IsEnabled = (bool)_ui_cbCuesAlbum.IsChecked;
            if (_ui_tbCuesAlbum.IsEnabled)
                _ui_tbCuesAlbum.Mark();
            else
				_ui_tbCuesAlbum.Background = Coloring.Notifications.cTextBoxInactive;
			_ui_cbCuesAlbum.Mark(_ui_tbCuesAlbum.Tag);
		}
		private void _ui_cbCuesYear_CheckedChanged(object sender, RoutedEventArgs e)
		{
			_ui_nudCuesYear.IsEnabled = (bool)_ui_cbCuesYear.IsChecked;
            //MarkNUDYear(_ui_nudCuesYear);
            if (_ui_nudCuesYear.IsEnabled)
				_ui_nudCuesYear.Mark();
            else
				_ui_spCuesYear.Background = Coloring.Notifications.cTextBoxInactive;
			_ui_cbCuesYear.Mark(null == _ui_nudCuesYear.Tag ? 0 : (int)_ui_nudCuesYear.Tag);
		}
		private void _ui_cbCuesPossessor_CheckedChanged(object sender, RoutedEventArgs e)
		{
			_ui_tbCuesPossessor.IsEnabled = (bool)_ui_cbCuesPossessor.IsChecked;
            if (_ui_tbCuesPossessor.IsEnabled)
				_ui_tbCuesPossessor.Mark();
            else
				_ui_tbCuesPossessor.Background = Coloring.Notifications.cTextBoxInactive;
			_ui_cbCuesPossessor.Mark(_ui_tbCuesPossessor.Tag);
		}
        private void ui_dlgArtists_Closed(object sender, EventArgs e)
		{
			//HaulierDialog ui_dlgArtists = (HaulierDialog)sender;
			ui_dlgArtists.Closed -= new EventHandler(ui_dlgArtists_Closed);
            if (null != ui_dlgArtists.DialogResult && (bool)ui_dlgArtists.DialogResult)
            {
                //_ui_lbArtists.DisplayMemberPath = ui_dlgArtists.HaulierControl.RightList.DisplayMemberPath;
                List<Person> aPers = new List<Person>();
                foreach (object c in ui_dlgArtists.HaulierControl.aItemsSelected)
                {
                    aPers.Add((Person)c);
                }
                //Person[] aPers = (Person[])(ui_dlgArtists.HaulierControl.RightList.ItemsSource);
                IdNamePair[] aListTMP = ConvertPersonsToPairs(aPers.ToArray());
                _ui_lbArtists.ItemsSource = aListTMP;    //.Cast<IdNamePair>().ToArray<IdNamePair>();
            }
            _ui_lbArtists.Mark(true);

            #region добавление артистов в поле _ui_tbCuesArtist
            if (!_bIs_ui_tbNameEditedByUser || !_bIs_ui_tbCuesArtistEditedByUser)
            {
				List<long> aNameIDs = new List<long>();
				List<string> aNames = new List<string>();
				foreach (IdNamePair cP in _ui_lbArtists.ItemsSource)
				{
					aNameIDs.Add(cP.nID);
					aNames.Add(cP.sName);
				}
				if (0 < aNameIDs.Count)
				{
					_cDBI.ArtistsCueNameGetAsync(MyIntSL.MyIntArrayGet(aNameIDs), aNames);
					_ui_tbCuesArtist.IsEnabled = false;
				}
			}
            #endregion
        }
		void _cDBI_ArtistsCueNameGetCompleted(object sender, ArtistsCueNameGetCompletedEventArgs e)
		{
			string sS = "", sC = "";
			int ni = 0;
            string sCueName;
            foreach (string sName in e.Result)
			{
				sCueName = sName == "" ? ((List<string>)e.UserState)[ni] : sName;

				if (sName == "")
                    sCueName = sCueName.ToUpperFirstLetterEveryWord(false);

				sS += sC + sCueName;
				sC = ", ";
				ni++;
			}
			if (!_bIs_ui_tbCuesArtistEditedByUser)
			{
				_bArtistEditedByUser_Busy = true;
				_ui_tbCuesArtist.Text = sS;
			}
			_ui_tbCuesArtist.IsEnabled = true;
		}
		private void ui_dlgStyles_Closed(object sender, EventArgs e)
		{
			HaulierDialog ui_dlgStyles = (HaulierDialog)sender;
			ui_dlgStyles.Closed -= ui_dlgStyles_Closed;
			if (null != ui_dlgStyles.DialogResult && (bool)ui_dlgStyles.DialogResult)
				_ui_lbStyles.ItemsSource = ui_dlgStyles.HaulierControl.aItemsSource.Cast<IdNamePair>().ToArray<IdNamePair>();
            _ui_lbStyles.Mark(false);
		}
        private void _ui_tbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_bNameEditedByUser_Busy)
                _bIs_ui_tbNameEditedByUser = true;
            else
                _bNameEditedByUser_Busy = false;
            _ui_tbName.Mark();
        }
        private void _ui_ddlSoundStart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			_ui_ddlSoundStart.Mark(false);
        }
        private void _ui_ddlSoundStop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			_ui_ddlSoundStop.Mark(false);
        }
        private void _ui_ddlRotation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			_ui_ddlRotation.Mark(false);
        }
        private void _ui_ddlPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			_ui_ddlPalette.Mark(false);
        }
		private void _ui_ddlSex_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_ui_ddlSex.Mark(false);
		}
		private void _ui_tbCuesArtist_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_bArtistEditedByUser_Busy)
                _bIs_ui_tbCuesArtistEditedByUser = true;
            else
                _bArtistEditedByUser_Busy = false;
            if (true == _ui_cbCuesArtist.IsChecked)
            {
				_ui_tbCuesArtist.Mark();
            }
            #region добавление артиста в поле _ui_tbName.Text
            if (!_bIs_ui_tbNameEditedByUser)
            {
                _bNameEditedByUser_Busy = true;
                _ui_tbName.Text = _ui_tbCuesArtist.Text + " : " + _ui_tbCuesSong.Text;
            }
            #endregion
        }
        private void _ui_tbCuesSong_TextChanged(object sender, TextChangedEventArgs e)
        {
			_ui_tbCuesSong.Mark();
            #region добавление названия песни в поле _ui_tbName.Text
            if (!_bIs_ui_tbNameEditedByUser)
            {
                _bNameEditedByUser_Busy = true;
                _ui_tbName.Text = _ui_tbCuesArtist.Text + " : " + _ui_tbCuesSong.Text;
            }
            #endregion
        }
        private void _ui_tbCuesAlbum_TextChanged(object sender, TextChangedEventArgs e)
        {
			_ui_tbCuesAlbum.Mark();
        }
        private void _ui_tbCuesPossessor_TextChanged(object sender, TextChangedEventArgs e)
        {
			_ui_tbCuesPossessor.Mark();
        }
        private void _ui_nudCuesYear_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
			_ui_nudCuesYear.Mark();
        }
        private void _ui_dgCustomValues_LoadingRow(object sender, DataGridRowEventArgs e)
        {
			_ui_dgCustomValues.Mark(e.Row, false);
        }
        private void _ui_dgCustomValues_RowEditEnded(object sender, DataGridRowEditEndedEventArgs e)
        {
			_ui_dgCustomValues.Mark(e.Row, true);
		}
        private void _ui_dgClips_LoadingRow(object sender, DataGridRowEventArgs e)
        {
			List<ClipsFragment> aClips = (List<ClipsFragment>)_ui_dgClips.Tag;
            if (null != e.Row)
				if (null != aClips.FirstOrDefault(o => o.cClip.nID == ((ClipsFragment)e.Row.DataContext).cClip.nID))
					e.Row.Background = Coloring.Notifications.cTextBoxActive;
                else
					e.Row.Background = Coloring.Notifications.cTextBoxChanged;
        }
        private void _ui_ctrRecalcFileDur_FileChanged(File cFile)
        {
			if (null != cFile)
				_ui_txtLastFileID.Text = cFile.nID.ToString();
			else
				_ui_txtLastFileID.Text = "-1";
        }
		private void _ui_dgChatInOuts_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			List<ChatInOut> aChatInOuts = (List<ChatInOut>)_ui_dgChatInOuts.Tag;
			if (null != e.Row)
			{
				if (null != aChatInOuts && null != aChatInOuts.FirstOrDefault(row => row.nID == ((ChatInOut)e.Row.DataContext).nID))
					e.Row.Background = Coloring.Notifications.cTextBoxActive;
				else
					e.Row.Background = Coloring.Notifications.cTextBoxChanged;
			}
		}
		private void _ui_btnPersonAdd_Click(string sText)
		{
			_dlgProgress.Show();
			Person cPers = new Person() { sName = sText, cType = new IdNamePair() { nID = 1, sName = "artist" }, nID = -1 };
			_cDBI.PersonSaveAsync(cPers, cPers);
		}
		private void ChatInChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
		{
			if (null == _ui_dgChatInOuts.SelectedItem || null == e.NewValue || !e.NewValue.HasValue)
				return;
			((ChatInOut)_ui_dgChatInOuts.SelectedItem).cTimeRange.nFrameIn = (long)(e.NewValue.Value.TimeOfDay.TotalMilliseconds / 40);
		}
		private void ChatOutChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
		{
			if (null == _ui_dgChatInOuts.SelectedItem || null == e.NewValue || !e.NewValue.HasValue)
				return;
			((ChatInOut)_ui_dgChatInOuts.SelectedItem).cTimeRange.nFrameOut = (long)(e.NewValue.Value.TimeOfDay.TotalMilliseconds / 40);
		}
		private void _ui_chkbxFile_CheckedChanged(object sender, RoutedEventArgs e)
		{
			if (true == _ui_chkbxFile.IsChecked)
			{
				_ui_tcFile.IsEnabled = true;
				if (_ui_ctrRecalcFileDur._cFile == null || _ui_ctrRecalcFileDur._cFile.nID < 1)
					_ui_ctrRecalcFileDur.bMarkRed = true;
			}
			else
			{
				_ui_tcFile.IsEnabled = false;
				_ui_ctrRecalcFileDur.bMarkRed = false;
			}
		}


		#region context menu
        private void _ui_dgCustomValues_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _cCustomValueCurrent = (CustomValue)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
            }
            catch
            {
                _cCustomValueCurrent = null;
            }
        }
		private void _ui_cmCustomValues_Opened(object sender, RoutedEventArgs e)
        {
            List<CustomValue> aCVs = ((IEnumerable<CustomValue>)_ui_dgCustomValues.ItemsSource).ToList();

			if (_ui_tcCustom.Background != Coloring.Notifications.cTextBoxError)     //if (!(0 < aCVs.Count && (null == aCVs[0].sName || 1 > aCVs[0].sName.Length)))
                _ui_cmCustomValuesAdd.IsEnabled = true;
            _ui_cmCustomValuesDelete.IsEnabled = false;
            if (null != _cCustomValueCurrent && 0 < aCVs.Count)
            {
                _ui_cmCustomValuesDelete.IsEnabled = true;
                _ui_cmCustomValuesDelete.Header = g.Common.sDelete.ToLower() + ": " + (1 <  _ui_dgCustomValues.SelectedItems.Count ? _ui_dgCustomValues.SelectedItems.Count.ToStr() : _cCustomValueCurrent.sName + " - " + _cCustomValueCurrent.sValue);
            }

            //MenuItem miTEST = new MenuItem() { Header = "TEST" };
            //Separator ui_cmSeparator = new Separator();
            ////MenuItem.SetContextMenu(ui_cmSeparator, miTEST);
            //_ui_cmCustomValues.Items.Add(ui_cmSeparator);
            foreach (IdNamePair cWord in _aWordsFor_ui_dgCustomValues)
            {
                MenuItem ui_MenuItem = new MenuItem() { Header = cWord.sName, Tag = cWord };
                ui_MenuItem.Click += new RoutedEventHandler(ui_MenuItem_Click);
                _ui_cmCustomValues.Items.Add(ui_MenuItem);
            }
            //TreeView ui_tvTEST = new TreeView();
            //TreeViewItem ui_tviTEST = new TreeViewItem() { Header="TEST TREE" };
            //ui_tvTEST.Items.Add(ui_tviTEST);
            ////ui_tviTEST.ItemsSource = _aWordsFor_ui_dgCustomValues;
            //ui_tvTEST.IsEnabled = true;
            
            //ContextMenuService.SetContextMenu(miTEST, _ui_cmCustomValues);
            
            
            _ui_cmCustomValuesDelete.Refresh();
			_ui_cmCustomValuesAdd.Refresh();
        }
        private void ui_MenuItem_Click(object sender, RoutedEventArgs e)
        {

            IdNamePair cSelectedWord = (IdNamePair)((MenuItem)sender).Tag;
            List<CustomValue> aCVs = ((IEnumerable<CustomValue>)_ui_dgCustomValues.ItemsSource).ToList();
            string sValue = "";
            if (100 > Clipboard.GetText().Length)
                sValue = Clipboard.GetText().Trim('\t', '\r', '\n', ' ');
            aCVs.Insert(0, new CustomValue());
            aCVs[0].nID = -1; aCVs[0].sName = cSelectedWord.sName; aCVs[0].sValue = sValue;
            _ui_dgCustomValues.ItemsSource = aCVs;
        }
        private void _ui_cmCustomValuesAdd_Clicked(object sender, RoutedEventArgs e)
        {
            List<CustomValue> aCVs = ((IEnumerable<CustomValue>)_ui_dgCustomValues.ItemsSource).ToList();
            aCVs.Insert(0, new CustomValue());
            aCVs[0].nID = -1; aCVs[0].sName = ""; aCVs[0].sValue = "";
            _ui_dgCustomValues.ItemsSource = aCVs;
        }
        private void _ui_cmCustomValues_Closed(object sender, RoutedEventArgs e)
        {
            List<MenuItem> aToDelete = new List<MenuItem>();
            foreach (object cMI in _ui_cmCustomValues.Items)
                if (cMI is MenuItem && ((MenuItem)cMI).Name != "_ui_cmCustomValuesAdd" && ((MenuItem)cMI).Name != "_ui_cmCustomValuesDelete")
                    aToDelete.Add(((MenuItem)cMI));
            foreach (MenuItem cMI in aToDelete)
                _ui_cmCustomValues.Items.Remove(cMI);
            _ui_cmCustomValuesAdd.IsEnabled = false;
            _ui_cmCustomValuesDelete.IsEnabled = false;
            _ui_cmCustomValuesDelete.Refresh();
            _ui_cmCustomValuesAdd.Refresh();
        }
        private void _ui_cmCustomValuesDelete_Clicked(object sender, RoutedEventArgs e)
        {
            List<CustomValue> aCVs = ((IEnumerable<CustomValue>)_ui_dgCustomValues.ItemsSource).ToList();
            List<CustomValue> aCVSelected=new List<CustomValue>();
            int nSelectedQty = _ui_dgCustomValues.SelectedItems.Count;
            if (null == aCVs) return;
            if (2 > nSelectedQty)
                aCVs.Remove(_cCustomValueCurrent);
            else
                for (int i = 0; i < nSelectedQty; i++)
                {
                    aCVs.Remove((CustomValue)_ui_dgCustomValues.SelectedItems[i]);
                }
            _ui_tcCustom.Tag = true;
            _ui_dgCustomValues.ItemsSource = aCVs;
            _ui_dgCustomValues.Mark(null, true);
        }
        private void _ui_cmStyles_Opened(object sender, RoutedEventArgs e)
        {
            _ui_cmStylesChange.IsEnabled = true;
            _ui_cmStylesChange.Focus();
            _ui_cmStyles.Focus();
        }
        private void _ui_cmStyles_Closed(object sender, RoutedEventArgs e)
        {
            _ui_cmStylesChange.IsEnabled = false;
        }
        private void _ui_cmStylesChange_Clicked(object sender, RoutedEventArgs e)
        {
            _cDBI.StylesGetCompleted += new EventHandler<StylesGetCompletedEventArgs>(_dlgProgress.AsyncRequestCompleted);
            _dlgProgress.Show();
            _cDBI.StylesGetAsync();
        }
        private void _ui_cmArtists_Opened(object sender, RoutedEventArgs e)
        {
            _ui_cmArtistsChange.IsEnabled = true;
            _ui_cmArtistsChange.Focus();
            _ui_cmArtists.Focus();
        }
        private void _ui_cmArtists_Closed(object sender, RoutedEventArgs e)
        {
            _ui_cmArtistsChange.IsEnabled = false;
        }
        private void _ui_cmArtistsChange_Clicked(object sender, RoutedEventArgs e)
        {
            _cDBI.ArtistsGetCompleted += new EventHandler<ArtistsGetCompletedEventArgs>(_dlgProgress.AsyncRequestCompleted);
            _dlgProgress.Show();
            _cDBI.ArtistsGetAsync();
        }

        private void _ui_dgClips_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
				_ui_tcClips.Tag = (ClipsFragment)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
            }
            catch
            {
				_ui_tcClips.Tag = null;
            }
        }
        private void _ui_cmClips_Opened(object sender, RoutedEventArgs e)
        {
            _ui_cmClipsDelete.Header = g.Common.sDelete.ToLower() + ": ";
            _ui_cmClipsAdd.IsEnabled = true;
			if (null != _ui_dgClips.SelectedItems && 0 < _ui_dgClips.SelectedItems.Count && null != _ui_tcClips.Tag)
            {
                _ui_cmClipsDelete.IsEnabled = true;
                if (1 < _ui_dgClips.SelectedItems.Count)
                    _ui_cmClipsDelete.Header += _ui_dgClips.SelectedItems.Count.ToString();
                else
					_ui_cmClipsDelete.Header += "'" + ((ClipsFragment)_ui_tcClips.Tag).cClip.sName + "'";
            }
            _ui_cmClipsAdd.Focus();
            _ui_cmClipsDelete.Focus();
            _ui_cmClips.Focus();
        }
        private void _ui_cmClipsAdd_Click(object sender, RoutedEventArgs e)
        {
            ClipsChooser dlgClipChooser = new ClipsChooser(_cDBI);
            dlgClipChooser.Closed += new EventHandler(dlgClipChooser_Closed);
            dlgClipChooser.Show();
        }
        private void dlgClipChooser_Closed(object sender, EventArgs e)
        {
			ClipsChooser dlgClipChooser = (ClipsChooser)sender;
			dlgClipChooser.Closed -= dlgClipChooser_Closed;
			if ((bool)(dlgClipChooser).DialogResult)
            {
				List<ClipsFragment> aClipsFragment = (List<ClipsFragment>)_ui_dgClips.ItemsSource;
                List<Clip> aSC = ((ClipsChooser)sender).SelectedClips;
                foreach (Clip cC in aSC)
                {
                    if (null == aClipsFragment.FirstOrDefault(s => s.cClip.nID == cC.nID))
						aClipsFragment.Add(new ClipsFragment() { cClip = cC, nFramesQty = cC.nFramesQty });
                }
                _ui_dgClips.ItemsSource = null;
                _ui_dgClips.ItemsSource = aClipsFragment;
				_ui_dgClips.Mark();
            }
        }
        private void _ui_cmClipsDelete_Click(object sender, RoutedEventArgs e)
        {
			List<ClipsFragment> aClipsFragment = (List<ClipsFragment>)_ui_dgClips.ItemsSource;
			List<ClipsFragment> aClipsSelected = new List<ClipsFragment>();
            for (int ni = 0; _ui_dgClips.SelectedItems.Count > ni; ni++)
				aClipsSelected.Add((ClipsFragment)_ui_dgClips.SelectedItems[ni]);
			foreach (ClipsFragment cC in aClipsSelected)
            {
                aClipsFragment.Remove(cC);
            }
            _ui_dgClips.ItemsSource = aClipsSelected;
            _ui_dgClips.ItemsSource = aClipsFragment;
			_ui_dgClips.Mark();
		}
        private void _ui_cmClips_Closed(object sender, RoutedEventArgs e)
        {
            _ui_cmClipsAdd.IsEnabled = false;
            _ui_cmClipsDelete.IsEnabled = false;
        }

        private void _ui_cmNameMake_Click(object sender, RoutedEventArgs e)
        {
            if (_ui_tbCuesArtist.Text != "" || _ui_tbCuesSong.Text != "")
            {
                _bNameEditedByUser_Busy = true;
                _ui_tbName.Text = _ui_tbCuesArtist.Text + " : " + _ui_tbCuesSong.Text;
            }
        }

		private void _ui_dgChatInOuts_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_ui_tcChatInOuts.Tag = (ChatInOut)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
			}
			catch
			{
				_ui_tcChatInOuts.Tag = null;
			}
		}
		private void _ui_cmChatInOuts_Opened(object sender, RoutedEventArgs e)
		{
			_ui_cmChatInOutDelete.IsEnabled = false;
			_ui_cmChatInOutDeleteAll.IsEnabled = false;
			_ui_cmChatInOutAdd.IsEnabled = false;
			if (access.scopes.programs.chatinouts.bCanDelete)
			{
				_ui_cmChatInOutDelete.Header = g.Common.sDelete.ToLower() + ": ";
				if (null != _ui_dgChatInOuts.SelectedItems && 0 < _ui_dgChatInOuts.SelectedItems.Count && null != _ui_tcChatInOuts.Tag)
				{
					ChatInOut cChatInOut = (ChatInOut)_ui_tcChatInOuts.Tag;
					_ui_cmChatInOutDelete.IsEnabled = true;
					_ui_cmChatInOutDeleteAll.IsEnabled = true;
					if (1 < _ui_dgChatInOuts.SelectedItems.Count)
						_ui_cmChatInOutDelete.Header += _ui_dgChatInOuts.SelectedItems.Count.ToString();
					else
						_ui_cmChatInOutDelete.Header += "'" + cChatInOut.cTimeRange.nFrameIn.ToFramesString(false) + "::" + cChatInOut.cTimeRange.nFrameIn.ToFramesString(false) + "'";
				}
				_ui_cmChatInOutDelete.Focus();
				_ui_cmChatInOutDeleteAll.Focus();
			}

			if (access.scopes.programs.chatinouts.bCanCreate)
			{
				_ui_cmChatInOutAdd.IsEnabled = true;
				_ui_cmChatInOutAdd.Focus();
			}

			_ui_cmChatInOuts.Focus();
		}
		private void _ui_cmChatInOuts_Closed(object sender, RoutedEventArgs e)
		{
			_ui_cmChatInOutAdd.IsEnabled = false;
			_ui_cmChatInOutDelete.IsEnabled = false;
		}
		private void _ui_cmChatInOutAdd_Click(object sender, RoutedEventArgs e)
		{
			List<ChatInOut> aChatInOuts = (List<ChatInOut>)_ui_dgChatInOuts.ItemsSource;
			ChatInOut cChatInOut = new ChatInOut();
			cChatInOut.cTimeRange = new TimeRange();
			cChatInOut.cTimeRange.nFrameIn = 0;
			cChatInOut.cTimeRange.nFrameOut = 0;
			aChatInOuts.Add(cChatInOut);
			_ui_dgChatInOuts.ItemsSource = aChatInOuts;
			_ui_dgChatInOuts.SelectedItem = cChatInOut;
			_ui_dgChatInOuts.BeginEdit();
		}
		private void _ui_cmChatInOutDelete_Click(object sender, RoutedEventArgs e)
		{
			List<ChatInOut> aChatInOuts = (List<ChatInOut>)_ui_dgChatInOuts.ItemsSource;
			List<ChatInOut> aChatInOutsSelected = new List<ChatInOut>();
			for (int ni = 0; _ui_dgChatInOuts.SelectedItems.Count > ni; ni++)
				aChatInOutsSelected.Add((ChatInOut)_ui_dgChatInOuts.SelectedItems[ni]);
			foreach (ChatInOut cC in aChatInOutsSelected)
			{
				aChatInOuts.Remove(cC);
			}
			_ui_dgChatInOuts.ItemsSource = aChatInOutsSelected;
			_ui_dgChatInOuts.ItemsSource = aChatInOuts;
			_ui_dgChatInOuts.Mark();
		}
		private void _ui_cmChatInOutDeleteAll_Click(object sender, RoutedEventArgs e)
		{
			_ui_dgChatInOuts.ItemsSource = new List<ChatInOut>();
		}
		#endregion
		private void _ui_DateTime_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.M)
                    _ui_cmNameMake_Click(null, null);
            }
            if (e.Key == Key.Enter)
                _ui_btnSave_Click(null, null);
        }
	#endregion
	#region DBI
    //--------------DBI--------------
        void _cDBI_ClipSaveCompleted(object sender, ClipSaveCompletedEventArgs e)
		{
			try
			{
				SaveAssetCompleted(e.Result);
			}
			catch(Exception ex)
			{
                _cMsgBox.ShowError(ex);
                //_cMsgBox.Closed += _cMsgBoxError_Closed;
                SaveAssetCompleted(-1);
			}
		}
        void _cDBI_AdvertisementSaveCompleted(object sender, AdvertisementSaveCompletedEventArgs e)
        {
			try
			{
				SaveAssetCompleted(e.Result);
			}
			catch(Exception ex)
			{
                _cMsgBox.ShowError(ex);
                //_cMsgBox.Closed += _cMsgBoxError_Closed;
                SaveAssetCompleted(-1);
			}
		}
        void _cDBI_ProgramSaveCompleted(object sender, ProgramSaveCompletedEventArgs e)
        {
			try
			{
				if (null == e.Error && 0 < e.Result)
				{
					List<ChatInOut> aChat = null;
					if (null != _ui_dgChatInOuts.ItemsSource)
						aChat = (List<ChatInOut>)_ui_dgChatInOuts.ItemsSource;
					if (access.scopes.programs.chatinouts.bCanUpdate && null != aChat && aChat.Count > 0)
						_cDBI.ChatInOutsSaveAsync(_cProgram, ((List<ChatInOut>)_ui_dgChatInOuts.ItemsSource).ToArray(), e.Result);
					else
						SaveAssetCompleted(e.Result);
				}
				else
					_cMsgBox.ShowError(g.Replica.sErrorAssetProperties2 + "!!");
			}
			catch (Exception ex)
			{
				_dlgProgress.Close();
                _cMsgBox.ShowError(ex);
            }
        }
		void _cDBI_DesignSaveCompleted(object sender, DesignSaveCompletedEventArgs e)
		{
			try
			{
				SaveAssetCompleted(e.Result);
			}
			catch { }
		}
		void _cDBI_PersonSaveCompleted(object sender, PersonSaveCompletedEventArgs e)
        {
			if (1 > e.Result)
				_cMsgBox.Show();
			else
			{
				List<Person> aPers = new List<Person>();
				aPers.Add((Person)e.UserState);
				aPers[0].nID = e.Result;
                foreach ( object cObj in ui_dlgArtists.HaulierControl.aItemsSource)
					aPers.Add((Person)cObj);
				ui_dlgArtists.HaulierControl.aItemsSource = new Person[0];
				ui_dlgArtists.HaulierControl.aItemsSource = aPers.OrderBy(o => o.sName).ToArray();
			}
			_dlgProgress.Close();
        }
        void _cDBI_FrequencyOfOccurrenceCompleted(object sender, FrequencyOfOccurrenceCompletedEventArgs e)
        {
			try
			{
				if (null == e.Result)
					throw new Exception(g.Replica.sErrorAssetProperties3);
				if (0 < e.Result.Length)
				{
					int nI = 0; // total не нужен
					_aWordsFor_ui_dgCustomValues.Clear();
					while (e.Result.Length > nI && Preferences.cServer.nFrequencyOfOccurrenceMax + 1 > nI)
						_aWordsFor_ui_dgCustomValues.Add(e.Result[nI++]);
				}
			}
			catch (Exception ex)
			{
				_cMsgBox.ShowError(ex);
                _cMsgBox.Closed += _cMsgBoxError_Closed;
            }
        }
		void _cDBI_ClassesGetCompleted(object sender, ClassesGetCompletedEventArgs e)
        {
			if (null != e.Result)
			{
				Classes.Set(e.Result);
                _ui_ctrClasses.Show(Classes.Array);
			}
			else
				_cMsgBox.ShowError(g.Replica.sErrorAssetProperties4 + "...");
            if (null != e.UserState && e.UserState is long)
            {
                long nID = e.UserState.ToID();
                _cDBI.AssetVideoTypeGetAsync(nID, nID);
            }
            else
                ControlsLoad();
		}
		void _cDBI_AssetVideoTypeGetCompleted(object sender, AssetVideoTypeGetCompletedEventArgs e)
		{
			long nAssetID = e.UserState.ToID();
			_cDBI.FrequencyOfOccurrenceAsync(e.Result.nID);
			switch (e.Result.sName)
			{
				case "clip":
                    Title = g.Helper.sClip.ToLower();
					_cDBI.ClipGetAsync(nAssetID);
					break;
				case "design":
                    Title = g.Helper.sDesign.ToLower();
                    _cDBI.DesignGetAsync(nAssetID);
                    break;
				case "advertisement":
                    Title = g.Helper.sAdvertisement.ToLower();
					_cDBI.AdvertisementGetAsync(nAssetID);
					break;
				case "program":
                    Title = g.Helper.sProgram.ToLower();
					_cDBI.ProgramGetAsync(nAssetID);
					break;
				default:
					throw new ArgumentException(g.Replica.sErrorAssetProperties1 + ":" + e.Result.sName);
			}
		}
        void _cDBI_VideoTypeGetCompleted(object sender, VideoTypeGetCompletedEventArgs e)
        {
            _cAsset.stVideo.cType.nID = e.Result.nID;
            _cDBI.FrequencyOfOccurrenceAsync(e.Result.nID, e.UserState);
			_cDBI.ClassesGetAsync(e.UserState);
        }
		void _cDBI_ClipGetCompleted(object sender, ClipGetCompletedEventArgs e)
		{
			try
			{
				_cClip = e.Result;
				_cAsset = _cClip;
				ControlsLoad();
			}
			catch(Exception ex)
			{
                MessageBox.Show(g.Common.sErrorDataReceive.ToLower());
                _cMsgBox.ShowError(ex);
                _cMsgBox.Closed += _cMsgBoxError_Closed;
            }
		}
		void _cDBI_AdvertisementGetCompleted(object sender, AdvertisementGetCompletedEventArgs e)
		{
			_cAdvertisement = e.Result;
			_cAsset = _cAdvertisement;
			cAssetType = _cAsset.cType;
			ControlsLoad();
		}
		void _cDBI_ProgramGetCompleted(object sender, ProgramGetCompletedEventArgs e)
		{
			_cProgram = e.Result;
			_cAsset = _cProgram;
			cAssetType = _cAsset.cType;
			_cDBI.ChatInOutsGetAsync(_cProgram);
		}
        void _cDBI_DesignGetCompleted(object sender, DesignGetCompletedEventArgs e)
        {
            _cDesign = e.Result;
            _cAsset = _cDesign;
            ControlsLoad();
        }
		void _cDBI_ArtistsGetCompleted(object sender, ArtistsGetCompletedEventArgs e)
		{
			if (null == e.Result)
			{
                MessageBox.Show(g.Helper.sErrorReceiveArtistsList + "!");
				return;
			}
			if (1 > e.Result.Length)
			{
                MessageBox.Show(g.Helper.sArtistsMissing);
				return;
			}
			ui_dlgArtists = new HaulierDialog();
            ui_dlgArtists.Title = g.Helper.sArtists.ToLower(); //"исполнители"
            ui_dlgArtists.HaulierControl.oLeftCaption = g.Common.sAvailiable; //"доступные";
			ui_dlgArtists.HaulierControl.sDisplayMemberPath = "sName";
            //ui_dlgArtists.HaulierControl.cTypeOfElement = typeof(Person);
			ui_dlgArtists.HaulierControl.bSearch = true;
			ui_dlgArtists.HaulierControl.bSearchButtonAdd = true;
            ui_dlgArtists.HaulierControl.oRightCaption = g.Common.sSelected; // "выбранные"; 
			//ui_dlgArtists.HaulierControl.RightList.DisplayMemberPath = "sName";
			ui_dlgArtists.Closed += new EventHandler(ui_dlgArtists_Closed);

            // TODO словарь убрали, т.к. поиск работает только с массивами пока. но словарь быстрее ищет. надо думать.
            //IdNamePair[] aListTMP = ConvertPersonsToPairs(e.Result);
            //Dictionary<int, IdNamePair> aList = aListTMP.ToDictionary(row => row.nID);
            //IdNamePair[] aSelected = ((IEnumerable<IdNamePair>)_ui_lbArtists.ItemsSource).ToArray();
            //foreach (IdNamePair cPerson in aSelected)
            //    if (aList.ContainsKey(cPerson.nID))
            //        aList.Remove(cPerson.nID);
            ui_dlgArtists.HaulierControl.aItemsSource = e.Result;
			long[] aSelectedIDs = ((IEnumerable<IdNamePair>)_ui_lbArtists.ItemsSource).Select(o => o.nID).ToArray();
			ui_dlgArtists.HaulierControl.aItemsSelected = e.Result.Where(o => aSelectedIDs.Contains(o.nID));
			//ui_dlgArtists.HaulierControl.RightList.Tag = ConvertPairsToPersons(((IEnumerable<IdNamePair>)_ui_lbArtists.Tag).ToArray()); //EMERGENCY:l зачем нужна эта строчка? ну т.е. понимаю глобально... но почему начальный список берем не из ItemsSource, а из Tag? на тот случай, если несколько раз открывали?
			//ui_dlgArtists.HaulierControl.RightList.ItemsSource = aSelected;

			ui_dlgArtists.PersonAdd = _ui_btnPersonAdd_Click;

			ui_dlgArtists.Show();
		}
		void _cDBI_StylesGetCompleted(object sender, StylesGetCompletedEventArgs e)
		{
			if (null == e.Result)
			{
                MessageBox.Show(g.Helper.sErrorReceiveStylesList);
				return;
			}
			if (1 > e.Result.Length)
			{
                MessageBox.Show(g.Helper.sStylesMissing + "!");
				return;
			}
			HaulierDialog ui_dlgStyles = new HaulierDialog();
            ui_dlgStyles.Title = g.Helper.sStyles.ToLower();
            //ui_dlgStyles.HaulierControl.ButtonL2RCaption = " ->> ";
            //ui_dlgStyles.HaulierControl.ButtonR2LCaption = " <<- ";
            ui_dlgStyles.HaulierControl.oLeftCaption = g.Common.sAvailiable; //"доступные";
			ui_dlgStyles.HaulierControl.sDisplayMemberPath = "sName";
            ui_dlgStyles.HaulierControl.oRightCaption = g.Common.sSelected; //"доступные";
			//ui_dlgStyles.HaulierControl.RightList.DisplayMemberPath = "sName";
            //ui_dlgStyles.HaulierControl.cTypeOfElement = typeof(IdNamePair);
            //ui_dlgStyles.HaulierControl.RightList.MaxHeight = ui_dlgStyles.ActualHeight;
            //ui_dlgStyles.HaulierControl.ItemAdd = StyleAdd;
			ui_dlgStyles.Closed += ui_dlgStyles_Closed;

			Dictionary<long, IdNamePair> aList = e.Result.ToDictionary(row => row.nID);
			long[] aSelectedIDs = ((IEnumerable<IdNamePair>)_ui_lbStyles.Tag).Select(o => o.nID).ToArray();
			IdNamePair[] aSelected = aList.Values.Where(o=>aSelectedIDs.Contains(o.nID)).ToArray();
            //foreach (IdNamePair cINP in aSelected)
            //    if (aList.ContainsKey(cINP.nID))
            //        aList.Remove(cINP.nID);
			ui_dlgStyles.HaulierControl.aItemsSource = aList.Values.ToArray();
			ui_dlgStyles.HaulierControl.aItemsSelected = aSelected;

			ui_dlgStyles.Show(); 
		}
		void _cDBI_RotationsGetCompleted(object sender, RotationsGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				Rotations.Set(e.Result);
				_ui_ddlRotation.ItemsSource = Rotations.Array;
			}
			_cDBI.PalettesGetAsync(e.UserState);
		}
		void _cDBI_PalettesGetCompleted(object sender, PalettesGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				Palettes.Set(e.Result);
				_ui_ddlPalette.ItemsSource = Palettes.Array;
			}
			_cDBI.SexGetAsync(e.UserState);
		}
		private void _cDBI_SexGetCompleted(object sender, SexGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				Sex.Set(e.Result);
				_ui_ddlSex.ItemsSource = Sex.Array;
			}
			_cDBI.SoundsGetAsync(e.UserState);
		}
		void _cDBI_SoundsGetCompleted(object sender, SoundsGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				Sounds.Set(e.Result);
				_ui_ddlSoundStart.ItemsSource = Sounds.Array;
				_ui_ddlSoundStop.ItemsSource = Sounds.Array;
			}
			_cDBI.CustomsLoadAsync(_cClip.nID, e.UserState);
		}
		void _cDBI_ArtistsLoadCompleted(object sender, ArtistsLoadCompletedEventArgs e)
		{
            IdNamePair[] aSourcePersons = new IdNamePair[0];
            if (null != e.Result)
                _ui_lbArtists.ItemsSource = aSourcePersons = ConvertPersonsToPairs(e.Result);
            else
                _ui_lbArtists.ItemsSource = aSourcePersons;

            IdNamePair[] sBkpPersons = new IdNamePair[aSourcePersons.Length];
            for (int i = 0; aSourcePersons.Length > i; i++)
            {
                sBkpPersons[i] = new IdNamePair();
                sBkpPersons[i].sName = aSourcePersons[i].sName;
            }
            _ui_lbArtists.Tag = sBkpPersons;

            if (0 == aSourcePersons.Length)
            {
				_ui_lbArtists.Background = Coloring.Notifications.cTextBoxInactive;
				_ui_tcArtists.Background = Coloring.Notifications.cTextBoxError;
            }
            else
				_ui_lbArtists.Background = Coloring.Notifications.cTextBoxActive;

            _cDBI.StylesLoadAsync(_cClip.nID, e.UserState);
		}
		void _cDBI_StylesLoadCompleted(object sender, StylesLoadCompletedEventArgs e)
		{
			if (null != e.Result)
				_ui_lbStyles.ItemsSource = e.Result;
			else
				_ui_lbStyles.ItemsSource = new IdNamePair[0];

            IdNamePair[] aSourceStyles = (IdNamePair[])_ui_lbStyles.ItemsSource;
            IdNamePair[] sBkpStyls = new IdNamePair[aSourceStyles.Length];
            for (int i = 0; aSourceStyles.Length > i; i++)
            {
                sBkpStyls[i] = new IdNamePair();
                sBkpStyls[i].sName = aSourceStyles[i].sName;
            }
            _ui_lbStyles.Tag = sBkpStyls;

            if (0 == aSourceStyles.Length)
				_ui_lbStyles.Background = Coloring.Notifications.cTextBoxInactive;
            else
				_ui_lbStyles.Background = Coloring.Notifications.cTextBoxActive;

			_cDBI.RotationsGetAsync(e.UserState);
		}
		void _cDBI_CustomsLoadCompleted(object sender, CustomsLoadCompletedEventArgs e)
		{
            List<CustomValue> aCVs = (null == e.Result ? new List<CustomValue>() : e.Result.ToList());
            List<CustomValue> aCVForTag = new List<CustomValue>();

            for (int i = 0; i < aCVs.Count; i++)
            {
                aCVForTag.Add(new CustomValue());
                aCVForTag[i].sName = aCVs[i].sName;
                aCVForTag[i].sValue = aCVs[i].sValue;
                aCVs[i].nID = i;                            //помечаем исходные строки индексом. А у добавляемых будет -1.
                aCVForTag[i].nID = i;
                //if (_aWordsFor_ui_dgCustomValues.Contains(aCVs[i].sName))
                //    _aWordsFor_ui_dgCustomValues.Remove(aCVs[i].sName);
            }
            //foreach (string sWord in _aWordsFor_ui_dgCustomValues)
            //{
            //    aCVs.Add(new CustomValue() { nID = -1, sName = sWord, sValue = "" });
            //    aCVForTag.Add(new CustomValue() { nID = -1, sName = sWord, sValue = "" });
            //}
            _ui_dgCustomValues.Tag = aCVForTag;
            _ui_dgCustomValues.ItemsSource = aCVs;

            if (null != e.UserState && e.UserState is TargetControlsLoad)
                ((TargetControlsLoad)e.UserState)();

			_dlgProgress.Close();
			//System.Windows.Browser.HtmlPage.Plugin.Focus();  // фокус и перемещение курсора в конец ТБ
			_ui_tbName.Focus(); // фокус и перемещение курсора в конец ТБ
			_ui_tbName.SelectionStart = _ui_tbName.Text.Length; // фокус и перемещение курсора в конец ТБ
		}
		void _cDBI_ChatInOutsGetCompleted(object sender, ChatInOutsGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				_ui_dgChatInOuts.ItemsSource = e.Result.ToList();
				_ui_dgChatInOuts.Tag = _ui_dgChatInOuts.ItemsSource;
			}
			ControlsLoad();
		}
		void _cDBI_ChatInOutsSaveCompleted(object sender, ChatInOutsSaveCompletedEventArgs e)
		{
			if (null != e && e.Result)
				SaveAssetCompleted(e.UserState.ToID());
			else
				_cMsgBox.ShowError(g.Replica.sErrorAssetProperties5 + "!!");
		}
		//void dlg_VI_Closed(object sender, EventArgs e)
		//{
		//    SaveAssetCompleted((int)((MsgBox)sender).Tag);
		//}
		void _cDBI_AssetParametersToPlaylistSaveCompleted(object sender, AssetParametersToPlaylistSaveCompletedEventArgs e)
		{
			if (!e.Result)
				_cMsgBox.ShowError(g.Replica.sErrorAssetProperties6 + "!!");
			else
				this.DialogResult = true;
		}
	#endregion
#endregion

#region Обработка Ассета (загрузка/сохранение)
		private void AssetCreate(string sVideoType)
		{
			switch (sVideoType)
			{
				case "clip":
					_cClip = new Clip();
					_cAsset = _cClip;
					break;
				case "design":
                    _cDesign = new Design();
                    _cAsset = _cDesign;
                    break;
                case "advertisement":
					_cAdvertisement = new Advertisement();
					_cAsset = _cAdvertisement;
					break;
				case "program":
					_cProgram = new Program();
					_cProgram.aClipsFragments = new ClipsFragment[0];
					_cAsset = _cProgram;
					_ui_tbName.Text = "";
					break;
				default:
					throw new ArgumentException(g.Replica.sErrorAssetProperties1 + ":" + sVideoType);
			}
			_cAsset.nID = -1;
            _cAsset.stVideo = new Video();
            _cAsset.stVideo.cType = new IdNamePair();
            _cAsset.stVideo.cType.sName = sVideoType;
			if (null != _cParent)
			{
				_ui_tbName.Text = _cParent.sName + " : ";
				_ui_tbName.Tag = _cParent.sName + " : ";
			}

			_cDBI.VideoTypeGetAsync(sVideoType, sVideoType);
			//_cAsset.stVideo.stType = cVideoType;
		}
		private void AssetLoad(long nID)
		{
			_cDBI.ClassesGetAsync(nID);
		}
        private void AssetApply()
		{
			_cAsset.sName = _ui_tbName.Text;
			_cAsset.stVideo.sName = _ui_tbName.Text;
			_cAsset.cFile = (File)_ui_ctrRecalcFileDur._cFile;
			_cAsset.nFrameIn = _ui_ctrRecalcFileDur._nIn;
			_cAsset.nFrameOut = _ui_ctrRecalcFileDur._nOut;
			_cAsset.nFramesQty = _ui_ctrRecalcFileDur._nTotal;
			_cAsset.aCustomValues = ((IEnumerable<CustomValue>)_ui_dgCustomValues.ItemsSource).ToArray();
            _cAsset.aClasses = _ui_ctrClasses.aSelectedItems;
            _cAsset.cType = cAssetType;
			if (null != _cParent)
				_cAsset.nIDParent = _cParent.nID;
			else
				_cAsset.nIDParent = -1;

			if (null == _cAsset.cFile || false == _ui_chkbxFile.IsChecked)
				_cAsset.cFile = new File() { nID = -1, cStorage = new Storage() };

			if (_cAsset.cType != null && _cAsset.cType.eType != AssetType.part)
				_cAsset.cFile = null;
		}
		private void ClipApply()
		{
			AssetApply();

			_cClip.aPersons = ConvertPairsToPersons(((IEnumerable<IdNamePair>)_ui_lbArtists.ItemsSource).ToArray());
			_cClip.aStyles = ((IEnumerable<IdNamePair>)_ui_lbStyles.ItemsSource).ToArray();

            _cClip.cRotation = (IdNamePair)_ui_ddlRotation.SelectedItem;
            _cClip.cPalette = (IdNamePair)_ui_ddlPalette.SelectedItem;
			_cClip.cSex = (IdNamePair)_ui_ddlSex.SelectedItem;
			if (null == _cClip.stSoundLevels)
                _cClip.stSoundLevels = new SoundLevels();
            _cClip.stSoundLevels.cStart = (IdNamePair)_ui_ddlSoundStart.SelectedItem;
            _cClip.stSoundLevels.cStop = (IdNamePair)_ui_ddlSoundStop.SelectedItem;

            if (null == _cClip.stCues)
                _cClip.stCues=new Cues();
            _cClip.stCues.sSong = ((bool)_ui_cbCuesSong.IsChecked) ? _ui_tbCuesSong.Text : null;
			_cClip.stCues.sArtist = ((bool)_ui_cbCuesArtist.IsChecked) ? _ui_tbCuesArtist.Text : null;
			_cClip.stCues.sAlbum = ((bool)_ui_cbCuesAlbum.IsChecked) ? _ui_tbCuesAlbum.Text : null;
			_cClip.stCues.sYear = ((bool)_ui_cbCuesYear.IsChecked) ? ((ushort)_ui_nudCuesYear.Value).ToString() : null;
			_cClip.stCues.sPossessor = ((bool)_ui_cbCuesPossessor.IsChecked) ? _ui_tbCuesPossessor.Text : null;
		}
		private void AdvertisementApply()
		{
			AssetApply();
		}
        private void DesignApply()
        {
            AssetApply();
        }
		private void ProgramApply()
		{
			AssetApply();
			if (null != _ui_dgClips.ItemsSource)
				_cProgram.aClipsFragments = ((List<ClipsFragment>)_ui_dgClips.ItemsSource).ToArray();
		}
		private void SaveAssetCompleted(long nID)
		{
			nThisAssetID = _cAsset.nID = nID;
			SaveCompleted(nID);
		}
		private void SaveCompleted(long nID)
		{
			if (0 < nID)
			{
				if (true == _ui_chkbxAssetToPL.IsChecked && access.scopes.playlist.bCanUpdate)
				{
					if (0 < nThisAssetID)
						_cDBI.AssetParametersToPlaylistSaveAsync(nThisAssetID);
					else
                        _cMsgBox.ShowError(g.Replica.sErrorAssetProperties6 + "!!");
				}
				else
					this.DialogResult = true;
			}
			else
				_cMsgBox.ShowError();
		}
#endregion

#region Функции инициализации элементов интерфейса
		delegate void TargetControlsLoad();
		private void ControlsLoad()
		{
			try
			{
				if (access.scopes.playlist.bCanUpdate && 0 < _cAsset.nID)
					_ui_chkbxAssetToPL.IsChecked = true;
				else
					_ui_chkbxAssetToPL.IsChecked = false;
				string sDefaultCuesClass = null;
				bool bSetDefaultCuesClass = false;
				_ui_ctrRecalcFileDur.ResetToDefault(AssetSL.GetAssetSL(_cAsset));
				if (-1 < _cAsset.nID)
				{
					_ui_tbName.Tag = _cAsset.sName;
					_ui_tbName.Text = _cAsset.sName;
					_bIs_ui_tbNameEditedByUser = true;

					_ui_txtLastAssetID.Text = _cAsset.nID.ToString();
					if (_cAsset.cFile != null)
						_ui_txtLastFileID.Text = _cAsset.cFile.nID.ToString();
					else
						_ui_txtLastFileID.Text = "-1";
					if (DateTime.MaxValue > _cAsset.dtLastPlayed)
						_ui_txtLastPlayed.Text = _cAsset.dtLastPlayed.ToString("yyyy-MM-dd HH:mm:ss");
				}
				else
				{
					_ui_tbName.Background = Coloring.Notifications.cTextBoxError;
				}

                if (null != _cAsset.aClasses)
                {
                    _ui_ctrClasses.aSelectedItems = _cAsset.aClasses;
                }
                else if (!_ui_ctrClasses.aClassesSrc.IsNullOrEmpty() && _ui_ctrClasses.aSelectedItems.IsNullOrEmpty())
                    bSetDefaultCuesClass = true;

                CustomTabControlRelease();
				if (_cAsset is Clip)
				{
					_ui_spClipRow2.Children.Add(_ui_tcCustom);
					_ui_spClip.Visibility = System.Windows.Visibility.Visible;
					_cDBI.ArtistsLoadAsync(_cAsset.nID, (TargetControlsLoad)ClipControlsLoad);
                    sDefaultCuesClass = Preferences.cServer.sDefautClassClip;
				}
				else if (_cAsset is Advertisement)
				{
					_ui_spAdvertisement.Visibility = System.Windows.Visibility.Visible;
					_ui_spAdvertisementRow1.Children.Add(_ui_tcCustom);
					_ui_tcCustom.Margin = new Thickness(0, 0, 0, 0);
					_ui_tcCustom.Width = LayoutRoot.ActualWidth;
					_cDBI.CustomsLoadAsync(_cAsset.nID, (TargetControlsLoad)AdvertisementControlsLoad);
					sDefaultCuesClass = Preferences.cServer.sDefautClassAdvertisement;
				}
				else if (_cAsset is Design)
				{
					_ui_spDesign.Visibility = System.Windows.Visibility.Visible;
					_ui_spDesignRow1.Children.Add(_ui_tcCustom);
					_ui_tcCustom.Margin = new Thickness(0, 0, 0, 0);
					_ui_tcCustom.Width = LayoutRoot.ActualWidth;
					_cDBI.CustomsLoadAsync(_cAsset.nID, (TargetControlsLoad)DesignControlsLoad);
					sDefaultCuesClass = Preferences.cServer.sDefautClassDesign;
				}
				else if (_cAsset is Program)
				{
					_ui_grdProgram.Children.Add(_ui_tcCustom);
					swc.Grid.SetRow(_ui_tcCustom, 0);
					swc.Grid.SetColumn(_ui_tcCustom, 1);
					_ui_grdProgram.Visibility = System.Windows.Visibility.Visible;
					_cDBI.CustomsLoadAsync(_cAsset.nID, (TargetControlsLoad)ProgramControlsLoad);
					if (_ui_ctrClasses.IsEnabled)
						sDefaultCuesClass = Preferences.cServer.sDefautClassProgram;
					else
						sDefaultCuesClass = "";
				}
				else
					throw new ArgumentException(g.Replica.sErrorAssetProperties1 + ":" + _cAsset.stVideo.cType.sName);
				if (bSetDefaultCuesClass)
				{
					if (sDefaultCuesClass == null)
                        sDefaultCuesClass = Preferences.cServer.sDefautClassUnknown;
                    Class cClass;
                    if (null != (cClass = _ui_ctrClasses.aClassesSrc.FirstOrDefault(o => o.sName.Equals(sDefaultCuesClass))))
                        _ui_ctrClasses.aSelectedItems = new Class[1] { cClass };
				}
				_ui_chkbxFile_CheckedChanged(null, null);
			}
			catch (Exception ex)
			{
                _cMsgBox.Closed += _cMsgBoxError_Closed;
				_cMsgBox.ShowError(ex);
            }
		}

        private void _cMsgBoxError_Closed(object sender, EventArgs e)
        {
            _cMsgBox.Closed -= _cMsgBoxError_Closed;
            _ui_btnCancel_Click(null, null);
        }

        private void CustomTabControlRelease()
        {
            _ui_tcCustom.Margin = new Thickness(6, 0, 0, 0);
            _ui_tcCustom.Width = 365;
            if (_ui_spClipRow2.Children.Contains(_ui_tcCustom))
                _ui_spClipRow2.Children.Remove(_ui_tcCustom);
            if (_ui_grdProgram.Children.Contains(_ui_tcCustom))
                _ui_grdProgram.Children.Remove(_ui_tcCustom);
            if (_ui_spAdvertisementRow1.Children.Contains(_ui_tcCustom))
                _ui_spAdvertisementRow1.Children.Remove(_ui_tcCustom);
            if (_ui_spDesignRow1.Children.Contains(_ui_tcCustom))
                _ui_spDesignRow1.Children.Remove(_ui_tcCustom);
        }
		//private bool MarkLB(ListBox _ui_lb, TabControl _ui_tc, bool OnEmptyReturnError)
		//{
		//    if (null == _ui_lb.Tag)
		//    {
		//        return false;
		//    }
            
		//    IdNamePair[] aSourcePersons = (IdNamePair[])_ui_lb.Tag;
		//    IdNamePair[] aPersons = (IdNamePair[])_ui_lb.ItemsSource;

		//    _ui_lb.Background = Coloring.Notifications.cTextBoxActive;
		//    _ui_tc.Background = Coloring.Notifications.cTextBoxInactive;
		//    if (0 == aPersons.Length)
		//    {
		//        if (OnEmptyReturnError)
		//        {
		//            _ui_tc.Background = Coloring.Notifications.cTextBoxError;
		//            return false;
		//        }
		//        _ui_lb.Background = Coloring.Notifications.cTextBoxInactive;
		//        if (0 < aSourcePersons.Length)
		//            _ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
		//        return true;
		//    }
		//    if (aPersons.Length != aSourcePersons.Length)
		//    {
		//        _ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
		//        return true;
		//    }
		//    for (int i = 0; aPersons.Length > i; i++)
		//    {
		//        if (aPersons[i].sName != aSourcePersons[i].sName)
		//        {
		//            _ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
		//            return true;
		//        }
		//    }
		//    return true;
		//}
		//private bool DataGridCustomValuesMark(DataGridRow dgRow, bool bParent) //parent == false если его цвет менять не нужно
		//{
		//    bool bRetVal = true;
		//    List<CustomValue> aCVsSource = (List<CustomValue>)_ui_dgCustomValues.Tag;
		//    int nCount = ((List<CustomValue>)_ui_dgCustomValues.ItemsSource).Count;

		//    if (1 > nCount)
		//    {
		//        _ui_dgCustomValues.Background = Coloring.Notifications.cTextBoxInactive;
		//        _ui_tcCustom.Background = Coloring.Notifications.cTextBoxInactive;
		//        if (0 < aCVsSource.Count)
		//            _ui_tcCustom.Background = Coloring.Notifications.cTextBoxChanged;
		//        return true;
		//    }
		//    _ui_dgCustomValues.Background = Coloring.Notifications.cTextBoxActive;
		//    if (null != dgRow)
		//    {
		//        StackPanel sp = null;
		//        CustomValue cCVrow = null;
		//        sp = ((StackPanel)_ui_dgCustomValues.Columns[0].GetCellContent(dgRow));
		//        cCVrow = (CustomValue)dgRow.DataContext;

		//        sp.Background = Coloring.Notifications.cTextBoxActive;
		//        int nSourceID = cCVrow.nID;
		//        if ("" == cCVrow.sName)
		//        {
		//            sp.Background = Coloring.Notifications.cTextBoxError;
		//            _ui_tcCustom.Background = Coloring.Notifications.cTextBoxError;
		//            return false;
		//        }
		//        else if (0 > nSourceID || aCVsSource[nSourceID].sName != cCVrow.sName || aCVsSource[nSourceID].sValue != cCVrow.sValue)
		//        {
		//            sp.Background = Coloring.Notifications.cTextBoxChanged;
		//        }
		//    }

		//    if (_ui_tcCustom.Background != Coloring.Notifications.cTextBoxError && nCount != aCVsSource.Count)
		//    {
		//        _ui_tcCustom.Background = Coloring.Notifications.cTextBoxChanged;
		//        return true;
		//    }

		//    if (!bParent)
		//    {
		//        if (_ui_tcCustom.Background == Coloring.Notifications.cTextBoxError)
		//            return false;
		//        else
		//            return true;
		//    }

		//    _ui_tcCustom.Background = Coloring.Notifications.cTextBoxInactive;
		//    foreach (CustomValue cCVData in _ui_dgCustomValues.ItemsSource)  //делаем только для выяснения цвета окантовки
		//    {
		//        //if (_aWordsFor_ui_dgCustomValues.Contains(cCVData.sName) && "" == cCVData.sValue)  // т.е. не рассматриваем неизмененные строки из _aWordsFor_ui_dgCustomValues
		//        //    continue;
		//        if (0 == cCVData.sName.Length)
		//        {
		//            _ui_tcCustom.Background = Coloring.Notifications.cTextBoxError;
		//            bRetVal = false;
		//        }
		//        else if (0 > cCVData.nID || aCVsSource[cCVData.nID].sName != cCVData.sName || aCVsSource[cCVData.nID].sValue != cCVData.sValue)
		//        {
		//            if (_ui_tcCustom.Background != Coloring.Notifications.cTextBoxError)
		//                _ui_tcCustom.Background = Coloring.Notifications.cTextBoxChanged;
		//        }
		//    }
		//    return bRetVal;
		//}
		private bool IsInputCorrect()
        {
            bool bRetVal = true;
            if (Coloring.Notifications.cTextBoxError == _ui_tbName.Background) bRetVal = false;
            if (_ui_ctrClasses.bMarkedRed) bRetVal = false;
            switch (_cAsset.stVideo.cType.sName)
            {
                case "clip":
                    if (Coloring.Notifications.cTextBoxError == _ui_tcArtists.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_tbCuesArtist.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_tbCuesSong.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_tbCuesAlbum.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_spCuesYear.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_tbCuesPossessor.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_ddlSoundStart.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_ddlSoundStop.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_ddlRotation.Background) bRetVal = false;
                    if (Coloring.Notifications.cTextBoxError == _ui_ddlPalette.Background) bRetVal = false;
					if (Coloring.Notifications.cTextBoxError == _ui_ddlSex.Background) bRetVal = false;
					if (Coloring.Notifications.cTextBoxError == _ui_tcStyles.Background) bRetVal = false;
                    break;
                case "design":
                case "advertisement":
                    break;
                case "program":
                    if (Coloring.Notifications.cTextBoxError == _ui_tcClips.Background) bRetVal = false;
                    break;
            }
            if (Coloring.Notifications.cTextBoxError == _ui_tcCustom.Background) bRetVal = false;
			if (_ui_ctrClasses.aSelectedItems.IsNullOrEmpty() && (null==_cAssetType || _cAssetType.eType== AssetType.part)) bRetVal = false;
            return bRetVal;
        }

        private void ComboBoxSet(ComboBox ui, string sElem)
        { 
            foreach(IdNamePair cElem in ui.Items)
            {
                if (sElem == cElem.sName)
                    ui.SelectedItem = cElem;
            }
        }

		private void ClipControlsLoad()
		{
			if (null == _ui_ddlRotation.SelectedItem && 0 < _ui_ddlRotation.Items.Count)
                ComboBoxSet(_ui_ddlRotation, "Третья"); //EMERGENCY:l надо как-то поменять этот механизм. нужно обсуждать //TODO LANG
			if (null == _ui_ddlPalette.SelectedItem && 0 < _ui_ddlPalette.Items.Count)
                ComboBoxSet(_ui_ddlPalette, g.Common.sUnknown);
			if (null == _ui_ddlSex.SelectedItem && 0 < _ui_ddlSex.Items.Count)
				ComboBoxSet(_ui_ddlSex, g.Common.sUnknown);
			if (null == _ui_ddlSoundStart.SelectedItem && 0 < _ui_ddlSoundStart.Items.Count)
                ComboBoxSet(_ui_ddlSoundStart, g.Common.sUnknown);
			if (null == _ui_ddlSoundStop.SelectedItem && 0 < _ui_ddlSoundStop.Items.Count)
                ComboBoxSet(_ui_ddlSoundStop, g.Common.sUnknown);

            _ui_nudCuesYear.Value = ("2000").ToInt32();

            if (null != _cClip.cRotation)
				foreach (IdNamePair cEnum in _ui_ddlRotation.Items)

            if (_cClip.cRotation.nID == cEnum.nID)
            {
                _ui_ddlRotation.Tag = cEnum;
                _ui_ddlRotation.SelectedItem = cEnum;
                _ui_ddlRotation.Background = Coloring.Notifications.cButtonNormal;
            }
			if (null != _cClip.cPalette)
				foreach (IdNamePair cEnum in _ui_ddlPalette.Items)
                    if (_cClip.cPalette.nID == cEnum.nID)
                    {
                        _ui_ddlPalette.Tag = cEnum;
                        _ui_ddlPalette.SelectedItem = cEnum;
                        _ui_ddlPalette.Background = Coloring.Notifications.cButtonNormal;
                    }
			if (null != _cClip.cSex)
				foreach (IdNamePair cEnum in _ui_ddlSex.Items)
					if (_cClip.cSex.nID == cEnum.nID)
					{
						_ui_ddlSex.Tag = cEnum;
						_ui_ddlSex.SelectedItem = cEnum;
						_ui_ddlSex.Background = Coloring.Notifications.cButtonNormal;
					}
			if (null != _cClip.stSoundLevels)
			{
				if (null != _cClip.stSoundLevels.cStart)
					foreach (IdNamePair cEnum in _ui_ddlSoundStart.Items)
                        if (_cClip.stSoundLevels.cStart.nID == cEnum.nID)
                        {
                            _ui_ddlSoundStart.Tag = cEnum;
                            _ui_ddlSoundStart.SelectedItem = cEnum;
                            _ui_ddlSoundStart.Background = Coloring.Notifications.cButtonNormal;
                        }
				if (null != _cClip.stSoundLevels.cStart)
					foreach (IdNamePair cEnum in _ui_ddlSoundStop.Items)
                        if (_cClip.stSoundLevels.cStop.nID == cEnum.nID)
                        {
                            _ui_ddlSoundStop.Tag = cEnum;
                            _ui_ddlSoundStop.SelectedItem = cEnum;
                            _ui_ddlSoundStop.Background = Coloring.Notifications.cButtonNormal;
                        }
			}

            if (null != _cClip.stCues)
            {
                _ui_tbCuesSong.Tag = _ui_tbCuesSong.Text = null == _cClip.stCues.sSong ? "" : _cClip.stCues.sSong;
                _ui_cbCuesSong.IsChecked = !(0 < _ui_tbCuesSong.Text.Length);  //это для 100% вызова функции ...isChackedChanged
                _ui_cbCuesSong.IsChecked = 0 < _ui_tbCuesSong.Text.Length;
                _ui_tbCuesArtist.Tag = _ui_tbCuesArtist.Text = null == _cClip.stCues.sArtist ? "" : _cClip.stCues.sArtist;
                if ("" != _ui_tbCuesArtist.Text)
                {
                    _bIs_ui_tbCuesArtistEditedByUser = true;
                }
                _ui_cbCuesArtist.IsChecked = !(0 < _ui_tbCuesArtist.Text.Length);
                _ui_cbCuesArtist.IsChecked = 0 < _ui_tbCuesArtist.Text.Length;
                _ui_tbCuesAlbum.Tag = _ui_tbCuesAlbum.Text = null == _cClip.stCues.sAlbum ? "" : _cClip.stCues.sAlbum;
                _ui_cbCuesAlbum.IsChecked = !(0 < _ui_tbCuesAlbum.Text.Length);
                _ui_cbCuesAlbum.IsChecked = 0 < _ui_tbCuesAlbum.Text.Length;
                _ui_cbCuesArtist.IsChecked = 0 < _ui_tbCuesArtist.Text.Length;

                if (null == _cClip.stCues.sYear)
                {
                    _ui_nudCuesYear.Value = 2000;
                }
                else
                {
                    _ui_nudCuesYear.IsEditable = true;
                    _ui_nudCuesYear.Tag = _cClip.stCues.sYear.ToInt32();
                    _ui_cbCuesYear.IsChecked = true;
					_ui_nudCuesYear.Value = _cClip.stCues.sYear.ToInt32();
                }
                _ui_tbCuesPossessor.Tag = _ui_tbCuesPossessor.Text = null == _cClip.stCues.sPossessor ? "" : _cClip.stCues.sPossessor;
                _ui_cbCuesPossessor.IsChecked = !(0 < _ui_tbCuesPossessor.Text.Length);
                _ui_cbCuesPossessor.IsChecked = 0 < _ui_tbCuesPossessor.Text.Length;

                if (_ui_tbCuesArtist.Text + " : " + _ui_tbCuesSong.Text == _ui_tbName.Text)
                    _bIs_ui_tbNameEditedByUser = false;
            }
            else
            {
                //============ по умолчанию вкл:
                _bIs_ui_tbNameEditedByUser = true;
                _ui_cbCuesSong.IsChecked = true;
                _ui_tbCuesSong.Tag = _ui_tbCuesSong.Text = "";
                _ui_tbCuesSong_TextChanged(null, null);
                _bArtistEditedByUser_Busy = true;
                _ui_cbCuesArtist.IsChecked = true;
                _ui_tbCuesArtist.Tag = _ui_tbCuesArtist.Text = "";
                _ui_tbCuesArtist_TextChanged(null, null);
                _bIs_ui_tbNameEditedByUser = false;
            }
		}
		private void AdvertisementControlsLoad()
		{
		}
        private void DesignControlsLoad()
        {
        }
		private void ProgramControlsLoad()
		{
			List<ClipsFragment> aRAOI = _cProgram.aClipsFragments.ToList(), aCTag = new List<ClipsFragment>();
            foreach (ClipsFragment cRAOI in aRAOI)
            {
				aCTag.Add(new ClipsFragment() { cClip = new Clip { sName = cRAOI.cClip.sName, nID = cRAOI.cClip.nID, nFramesQty = cRAOI.cClip.nFramesQty }, nFramesQty = cRAOI.nFramesQty });
            }
			if (_eAssetType == AssetType.part)
			{
				_ui_dgClips.Tag = aCTag;
				_ui_dgClips.ItemsSource = aRAOI;
				_ui_dgClips.Mark();
				_ui_dgClips.IsEnabled = access.scopes.programs.clips.bCanUpdate;
				_ui_dgChatInOuts.IsEnabled = access.scopes.programs.chatinouts.bCanUpdate;
			}
		}
#endregion

#region Переопределение функций Show...
		public void Show(string sVideoType)
		{
			base.Show();
            _ui_ctrRecalcFileDur.sDefaultFileStorageName = sDefaultFileStorageName;
			AssetCreate(sVideoType);
		}
		public void Show(long nAssetID)
		{
			base.Show();
			_ui_ctrRecalcFileDur.sDefaultFileStorageName = sDefaultFileStorageName;
			AssetLoad(nAssetID);
		}
		new public void Show()
		{
			throw new NotImplementedException();
		}
#endregion

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            object i = e.GetType();
        }
        Person[] ConvertPairsToPersons(IdNamePair[] aSourcePairs)
        {
            Person[] RetVal = null;
            if (null == aSourcePairs)
                return RetVal;
            RetVal = new Person[aSourcePairs.Length];
            for (int i = 0; aSourcePairs.Length > i; i++) 
            {
                RetVal[i]=new Person();
                RetVal[i].nID = aSourcePairs[i].nID;
                RetVal[i].sName = aSourcePairs[i].sName;
            }
            return RetVal;
        }
        IdNamePair[] ConvertPersonsToPairs(Person[] aSourcePairs)
        {
            IdNamePair[] RetVal = null;
            if (null == aSourcePairs)
                return RetVal;
            RetVal = new IdNamePair[aSourcePairs.Length];
            for (int i = 0; aSourcePairs.Length > i; i++)
            {
                RetVal[i] = new IdNamePair();
                RetVal[i].nID = aSourcePairs[i].nID;
                RetVal[i].sName = aSourcePairs[i].sName;
            }
            return RetVal;
        }

        private void _ui_chkbxFile_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_ui_chkbxAssetToPL.IsChecked != null && _ui_chkbxAssetToPL.IsChecked.Value)
                _ui_chkbxAssetToPL.IsChecked = false;
        }
    }
    static public class x
	{
		static public bool Mark(this ComboBox ui, bool bCanBeNull)
		{
			bool bRetVal = true;
			if (null == ui.SelectedItem && !bCanBeNull)
			{
				ui.Background = Coloring.Notifications.cButtonError;
				bRetVal = false;
			}
			else if (ui.SelectedItem == ui.Tag)
			{
				ui.Background = Coloring.Notifications.cButtonNormal;
				if (null == ui.SelectedItem)
					ui.Background = Coloring.Notifications.cButtonInactive;
			}
			else //if (ui.SelectedItem is Class)
				ui.Background = Coloring.Notifications.cButtonChanged;
			return bRetVal;
		}
		static public bool Mark(this CheckBox ui, object oValue)
		{
			StackPanel ui_spParent = (StackPanel)ui.Parent;
			if (null == ui_spParent)
				return false;
			bool bEmpty = false;
			if ((oValue is string && 1 > ((string)oValue).Length) || (oValue is int && 0 == (int)oValue))
				bEmpty = true;

			ui.Background = Coloring.Notifications.cButtonInactive;
			ui_spParent.Background = Coloring.Notifications.cTextBoxInactive;
			if (true == ui.IsChecked)
			{
				ui.Background = Coloring.Notifications.cButtonNormal;
				if (bEmpty)
				{
					ui.Background = Coloring.Notifications.cButtonChanged;
					ui_spParent.Background = Coloring.Notifications.cTextBoxChanged;
				}
			}
			else if (!bEmpty)
			{
				ui.Background = Coloring.Notifications.cButtonChanged;
				ui_spParent.Background = Coloring.Notifications.cTextBoxChanged;
			}
			return true;
		}
		static public bool Mark(this TextBox ui)
		{
			bool bRetVal = true;
			ui.Background = Coloring.Notifications.cTextBoxActive;
			if (1 > ui.Text.Length)
			{
				ui.Background = Coloring.Notifications.cTextBoxError;
				bRetVal = false;
			}
			else if ((null == ui.Tag && "" != ui.Text) || (string)ui.Tag != ui.Text)
			{
				ui.Background = Coloring.Notifications.cTextBoxChanged;
			}
			return bRetVal;
		}
		static public bool Mark(this NumericUpDown ui)
		{
			bool bRetVal = true;
			if (null == ui)    //это случаи только на этапе загрузки формы 
				return true;
			StackPanel ui_spParent = (StackPanel)ui.Parent;
			if (false == ui.IsEnabled && null == ui.Tag)
			{
				ui_spParent.Background = Coloring.Notifications.cTextBoxInactive;
				return true;
			}
			ui_spParent.Background = Coloring.Notifications.cTextBoxActive;
			if (1900 > ui.Value || DateTime.Now.Year.ToInt32() < ui.Value)
			{
				ui_spParent.Background = Coloring.Notifications.cTextBoxError;
				bRetVal = false;
			}
			else if (null == ui.Tag || 0 == ui.Tag.ToInt32() || ui.Tag.ToInt32() != ui.Value)
			{
				ui_spParent.Background = Coloring.Notifications.cTextBoxChanged;
			}
			return bRetVal;
		}
		static public bool Mark(this ListBox ui_lb, bool OnEmptyReturnError)
        {
			TabControl ui_tc = (TabControl)ui_lb.GetVisualAncestors().Where(r => r.GetType() == typeof(TabControl)).FirstOrDefault();
			if (null == ui_tc)
				return false;
            
            IdNamePair[] aSourceValues = (IdNamePair[])ui_lb.Tag;
            IdNamePair[] aValues = (IdNamePair[])ui_lb.ItemsSource;

			ui_lb.Background = Coloring.Notifications.cTextBoxActive;
			ui_tc.Background = Coloring.Notifications.cTextBoxInactive;
            if (0 == aValues.Length)
            {
                if (OnEmptyReturnError)
                {
					ui_tc.Background = Coloring.Notifications.cTextBoxError;
                    return false;
                }
				ui_lb.Background = Coloring.Notifications.cTextBoxInactive;
                if (0 < aSourceValues.Length)
					ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
                return true;
            }
            if (aValues.Length != aSourceValues.Length)
            {
				ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
                return true;
            }
            for (int i = 0; aValues.Length > i; i++)
            {
                if (aValues[i].sName != aSourceValues[i].sName)
                {
					ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
                    return true;
                }
            }
            return true;
		}
		static public bool Mark(this DataGrid ui_dg) //EMERGENCY желательно из этого метода и следующего сделать один, универсальный, если это возможно 
		{
			if ("_ui_dgClips" == ui_dg.Name || "_ui_dgChatInOuts" == ui_dg.Name)
			{
				TabControl ui_tc = (TabControl)ui_dg.GetVisualAncestors().Where(r => r.GetType() == typeof(TabControl)).FirstOrDefault();
				if (null == ui_tc)
					return false;
				object[] aValuesOriginal;
				object[] aValues = ui_dg.ItemsSource.Cast<object>().ToArray();
				if (null != ui_dg.Tag)
					aValuesOriginal = ((System.Collections.IEnumerable)ui_dg.Tag).Cast<object>().ToArray();
				else
					aValuesOriginal = new object[0];
				if (aValuesOriginal.Length != aValues.Length)
				{
					ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
				}
				else if (0 < aValuesOriginal.Length)
				{
					ui_tc.Background = Coloring.Notifications.cTextBoxActive;
					FieldInfo cFieldInfo = aValuesOriginal[0].GetType().GetField("nID");
					if (null != cFieldInfo)
					{
						long nID = 0;
						foreach (object cValue in aValues)
						{
							nID = (long)cFieldInfo.GetValue(cValue);
							if (null == aValuesOriginal.FirstOrDefault(o => (long)cFieldInfo.GetValue(o) == nID))
							{
								ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
								break;
							}
						}
					}
				}
				else
					ui_tc.Background = Coloring.Notifications.cTextBoxInactive;
				return true;
			}
			return ui_dg.Mark(null, false);
		}
		static public bool Mark(this DataGrid ui_dg, DataGridRow ui_dgr, bool bParent) //parent == false если его цвет менять не нужно
		{
			if ("_ui_dgCustomValues" != ui_dg.Name)
				throw new NotImplementedException();
			bool bRetVal = true;
			TabControl ui_tc = (TabControl)ui_dg.GetVisualAncestors().Where(r => r.GetType() == typeof(TabControl)).FirstOrDefault();
			if (null == ui_tc)
				return false;
			List<CustomValue> aCVsSource = (List<CustomValue>)ui_dg.Tag;  //UNDONE нужно заменить тип aCVsSource и т.п. на dictionary<long, CustomValue> и убрать все приведения long к int'у
			int nCount = ((List<CustomValue>)ui_dg.ItemsSource).Count;

			if (1 > nCount)
			{
				ui_dg.Background = Coloring.Notifications.cTextBoxInactive;
				ui_tc.Background = (0 < aCVsSource.Count ? Coloring.Notifications.cTextBoxChanged : Coloring.Notifications.cTextBoxInactive);
				return true;
			}
			ui_dg.Background = Coloring.Notifications.cTextBoxActive;
			if (null != ui_dgr)
			{
				StackPanel sp = null;
				CustomValue cCVrow = null;
				sp = ((StackPanel)ui_dg.Columns[0].GetCellContent(ui_dgr));
				cCVrow = (CustomValue)ui_dgr.DataContext;

				sp.Background = Coloring.Notifications.cTextBoxActive;
				int nSourceID = (int)cCVrow.nID;
				if ("" == cCVrow.sName)
				{
					sp.Background = Coloring.Notifications.cTextBoxError;
					ui_tc.Background = Coloring.Notifications.cTextBoxError;
					return false;
				}
				else if (0 > nSourceID || aCVsSource[nSourceID].sName != cCVrow.sName || aCVsSource[nSourceID].sValue != cCVrow.sValue)
					sp.Background = Coloring.Notifications.cTextBoxChanged;
			}

			if (ui_tc.Background != Coloring.Notifications.cTextBoxError && nCount != aCVsSource.Count)
			{
				ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
				return true;
			}

			if (!bParent)
			{
				if (ui_tc.Background == Coloring.Notifications.cTextBoxError)
					return false;
				return true;
			}

			ui_tc.Background = Coloring.Notifications.cTextBoxInactive;
			foreach (CustomValue cCVData in ui_dg.ItemsSource)  //делаем только для выяснения цвета окантовки
			{
				//if (_aWordsFor_ui_dgCustomValues.Contains(cCVData.sName) && "" == cCVData.sValue)  // т.е. не рассматриваем неизмененные строки из _aWordsFor_ui_dgCustomValues
				//    continue;
				if (0 == cCVData.sName.Length)
				{
					ui_tc.Background = Coloring.Notifications.cTextBoxError;
					bRetVal = false;
				}
				else if (0 > cCVData.nID || aCVsSource[(int)cCVData.nID].sName != cCVData.sName || aCVsSource[(int)cCVData.nID].sValue != cCVData.sValue)
				{
					if (ui_tc.Background != Coloring.Notifications.cTextBoxError)
						ui_tc.Background = Coloring.Notifications.cTextBoxChanged;
				}
			}
			return bRetVal;
		}
	}
}

