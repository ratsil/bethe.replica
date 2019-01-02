﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Data;

using controls.childs.sl;
using helpers.replica.services.dbinteract;
using controls.sl;
using controls.extensions.sl;
using helpers.replica.sl;
using helpers.extensions;
using controls.childs.replica.sl;
using g = globalization;

namespace replica.sl
{
    public partial class playlist : Page
    {
        string sPROBA_ERR = ""; //-------PROBA //EMERGENCY:l не пора ли убрать или, если нужно, поставить на нормальные рельсы?
        private Progress _dlgProgress = new Progress();
        private DBInteract _cDBI;
        private List<PlaylistItemSL> _aPlayListItemsPlanned;
        private int _nPlannedRowHeight = 21;
        private int _nArchievedRowHeight = 21;
        private int _nOnAirRowHeight = 21;
		private int _nOverlapMinutes = 60;
        private System.Windows.Threading.DispatcherTimer _cTimerForCommandResult = null;
		private System.Windows.Threading.DispatcherTimer _cTimerForPLAddResult = null;
		//private int nTimeoutForAddResult;
        private DateTime _dtCommandTimeout;
		private DateTime _dtPLImportTimeout;
        private PlaylistItemSL _cPlayListItemCurrent;
        private string _sPlannedDelete_Header;
        private MsgBox _dlgMsg = new MsgBox();
		private DateTime _dtActivePlannedDate;
		private bool _bInitIsOver;
        private PlaylistTimer _cPLTimer;
		private List<PlaylistItemSL> _aPLIsJustInserted;
		private int _nAttemptsCount;

		public playlist()
        {
            InitializeComponent();
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.Name);
            Title = g.Helper.sPlaylist;

            _cDBI = new DBInteract();
            _cDBI.PlaylistItemsGetCompleted += new EventHandler<PlaylistItemsGetCompletedEventArgs>(_cDBI_PlaylistItemsGetCompleted);
            _cDBI.PlaylistItemsArchGetCompleted += new EventHandler<PlaylistItemsArchGetCompletedEventArgs>(_cDBI_PlaylistItemsArchGetCompleted);
            _cDBI.PlaylistItemsPlanGetCompleted += new EventHandler<PlaylistItemsPlanGetCompletedEventArgs>(_cDBI_PlaylistItemsPlanGetCompleted);
            _cDBI.PlaylistItemsDeleteCompleted += new EventHandler<PlaylistItemsDeleteCompletedEventArgs>(_cDBI_PlaylistItemsDeleteCompleted);
            _cDBI.PlaylistItemStartsSetCompleted += new EventHandler<PlaylistItemStartsSetCompletedEventArgs>(_cDBI_PlaylistItemStartsSetCompleted);
			_cDBI.PlaylistItemsAddWorkerCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cDBI_PlaylistItemsAddCompleted);

			_cDBI.PlaylistRecalculateQueryCompleted += new EventHandler<PlaylistRecalculateQueryCompletedEventArgs>(_cDBI_PlaylistRecalculateQueryCompleted);
            _cDBI.CommandStatusGetCompleted += new EventHandler<CommandStatusGetCompletedEventArgs>(_cDBI_CommandStatusGetCompleted);
            _cDBI.PlaylistItemsDeleteSinceCompleted += new EventHandler<PlaylistItemsDeleteSinceCompletedEventArgs>(_cDBI_PlaylistItemsDeleteSinceCompleted);
            _cDBI.PlaylistInsertCompleted += new EventHandler<PlaylistInsertCompletedEventArgs>(_cDBI_PlaylistInsertCompleted);
			_cDBI.PlaylistItemAdd_ResultGetCompleted += new EventHandler<PlaylistItemAdd_ResultGetCompletedEventArgs>(_cDBI_PlaylistItemAdd_ResultGetCompleted);
			_cDBI.GroupMovingCompleted += new EventHandler<GroupMovingCompletedEventArgs>(_cDBI_GroupMovingCompleted);
			_cDBI.PlaylistItemsTimingsSetCompleted += _cDBI_PlaylistItemsTimingsSetCompleted;
			_cDBI.InsertInBlockCompleted += _cDBI_InsertInBlockCompleted;
            _ui_dgOnAir.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgOnAir_LoadingRow);
            _ui_dgPlanned.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgPlanned_LoadingRow);
            _ui_dgArchived.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgArchived_LoadingRow);
            _ui_rpAir.IsOpenChanged += _ui_rpAir_IsOpenChanged;
            _ui_rpPlanned.IsOpenChanged += _ui_rpPlanned_IsOpenChanged;
            _ui_rpArchived.IsOpenChanged += _ui_rpArchived_IsOpenChanged;
			_cDBI.PlaylistInsertCopiesCompleted += new EventHandler<PlaylistInsertCopiesCompletedEventArgs>(_cDBI_PlaylistInsertCopiesCompleted);
			_cDBI.PlaylistLastElementGetCompleted += new EventHandler<PlaylistLastElementGetCompletedEventArgs>(_cDBI_PlaylistLastElementGetCompleted);
			_cDBI.BeforeAddCheckRangeCompleted += new EventHandler<BeforeAddCheckRangeCompletedEventArgs>(_cDBI_BeforeAddCheckRangeCompleted);

			_dlgMsg.Closed += _dlgMsg_Closed_negative;
			_ui_rpPlanned.IsOpen = true;
            _ui_rpAir.IsOpen = false;
            _ui_rpArchived.IsOpen = false;
            _dlgProgress.Show();

            _ui_lblCurrentItem.Content = null;
            _ui_lblCurrentItemsdtStart.Content = null;
            _ui_lblCurrentItemsType.Content = null;
            _ui_lblCurrentItemLeft.Content = null;
            _ui_lblNextItem.Content = null;
            _ui_lblNextItemsdtStert.Content = null;
            _ui_lblNextItemsType.Content = null;

			_dtActivePlannedDate = DateTime.MinValue;
			_ui_dtpArchiveDate.DisplayDateStart = DateTime.Now.AddDays(-50);
			_ui_dtpArchiveDate.SelectedDate = DateTime.Now.AddDays(-2);
			_ui_dtpArchiveDate.DisplayDateEnd = DateTime.Now;
			_ui_dtpPlannedDate.DisplayDateStart = DateTime.Now;
			_ui_dtpPlannedDate.SelectedDate = DateTime.Now.AddDays(3);
			_ui_dtpPlannedDate.DisplayDateEnd = DateTime.Now.AddDays(50);

			this.Loaded += Playlist_Loaded;
			App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
            _ui_svMainViewer.MaxHeight = UI_Sizes.GetPossibleHeightOfPlaylistScrollViewer();
			_ui_dgPlanned.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Single();
			_ui_dgOnAir.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Single();
			_ui_dgArchived.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Single();
            Init();

            if (!access.scopes.playlist.bCanCreate)
                _ui_lblHours.Visibility = _ui_nudHoursQty.Visibility = _ui_btnRecalculatePart.Visibility = _ui_btnAdd.Visibility = _ui_btnImport.Visibility = Visibility.Collapsed;

            if (!access.scopes.playlist.bCanUpdate)
                _ui_nudHoursQty.Visibility = _ui_btnRecalculatePart.Visibility = Visibility.Collapsed;

            if (!access.scopes.playlist.bCanDelete)
            {
                _ui_btnDelete.Visibility = Visibility.Collapsed;
                if (!access.scopes.playlist.bCanUpdate)
                    _ui_spPlannedQty.Visibility = Visibility.Collapsed;
            }
			_bInitIsOver = true;
        }


		private void _cDBI_PlaylistItemsTimingsSetCompleted(object sender, PlaylistItemsTimingsSetCompletedEventArgs e)
		{
			if (!e.Result)
				_dlgMsg.ShowError(g.Replica.sErrorPlaylist1);
		}

		void BrowserWindow_Resized(object sender, EventArgs e)
        {
            _ui_svMainViewer.MaxHeight = UI_Sizes.GetPossibleHeightOfPlaylistScrollViewer();
			if (!(_ui_rpPlanned.IsOpen && _ui_rpAir.IsOpen) && !(_ui_rpPlanned.IsOpen && _ui_rpArchived.IsOpen) && !(_ui_rpArchived.IsOpen && _ui_rpAir.IsOpen))
			{
				_ui_dgPlanned.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Single();
				_ui_dgOnAir.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Single();
				_ui_dgArchived.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Single();
			}
			else
			{
				_ui_dgPlanned.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Default();
				_ui_dgOnAir.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Default();
				_ui_dgArchived.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInPlaylistView_Default();
			}
        }
		private class TimedCopyInfo
		{
			public DateTime dtStart;
			public List<PlaylistItemSL> aPLIsSelected;
		}
        private class PlaylistTimer
        {
            public enum Status
            {
                stopped,
                started,
                error
            }
            private DBInteract _cDBI;
            System.Windows.Threading.DispatcherTimer _cTimerForComingUp;
            System.Windows.Threading.DispatcherTimer _cTimerForLablesRefresh;
            Label _lblCurrentName, _lblCurrentStart, _lblCurrentFileStorageName, _lblCurrentLeft, _lblNextName, _lblNextStart, _lblNextFileStorageName;
            Status _eStatus;
            public Status eStatus
            { get { return _eStatus; } }
            DateTime _dtCurrentStop;
            public PlaylistTimer(DBInteract cDBI)
            {
                _cDBI = cDBI;
                _eStatus = Status.stopped;
                _cDBI.ComingUpGetCompleted += new EventHandler<ComingUpGetCompletedEventArgs>(_cDBI_ComingUpGetCompleted);
                _cTimerForComingUp = new System.Windows.Threading.DispatcherTimer();
                _cTimerForComingUp.Tick += new EventHandler(ComingUpGet);
				_cTimerForComingUp.Interval = new System.TimeSpan(0, 0, 20);
                _cTimerForLablesRefresh = new System.Windows.Threading.DispatcherTimer();
                _cTimerForLablesRefresh.Tick += new EventHandler(LablesRefresh);
				_cTimerForLablesRefresh.Interval = new System.TimeSpan(0, 0, 1);
            }
            public void Start(Label lblCurrentName, Label lblCurrentStart, Label lblCurrentType, Label lblCurrentLeft, Label lblNextName, Label lblNextStart, Label lblNextType)
            {
                _lblCurrentName = lblCurrentName;
                _lblCurrentStart = lblCurrentStart;
                _lblCurrentFileStorageName = lblCurrentType;
                _lblCurrentLeft = lblCurrentLeft;
                _lblNextName = lblNextName;
                _lblNextStart = lblNextStart;
                _lblNextFileStorageName = lblNextType;
                _cTimerForLablesRefresh.Start();
                _eStatus = Status.started;
            }
            void _cDBI_ComingUpGetCompleted(object sender, ComingUpGetCompletedEventArgs e)
            {
				try
				{
					if (null != e.Result && 2 == e.Result.Length)
					{
						PlaylistItemSL[] aPLIs = PlaylistItemSL.GetArrayOfPlaylistItemSLs(e.Result);
						_lblCurrentName.Content = aPLIs[0].sName;
						_lblCurrentStart.Content = aPLIs[0].sdtStart;
						_lblCurrentFileStorageName.Content = aPLIs[0].cFile.cStorage.sName;
						_lblNextName.Content = aPLIs[1].sName;
						_lblNextStart.Content = aPLIs[1].sdtStart;
						_lblNextFileStorageName.Content = aPLIs[1].cFile.cStorage.sName;
						if (DateTime.MaxValue == aPLIs[0].dtStartReal)
							_lblCurrentLeft.Content = "error";
						else
						{
							long nPast = (long)DateTime.Now.Subtract(aPLIs[0].dtStartReal).TotalMilliseconds / 40;
							_dtCurrentStop = aPLIs[0].dtStartReal.AddMilliseconds(aPLIs[0].nDuration * 40);
							_lblNextStart.Content = (string)_lblNextStart.Content + "_dtCurrentStop";
							_lblCurrentLeft.Content = (aPLIs[0].nDuration - nPast).ToFramesString(false, false, true, true);
						}
						if (!_cTimerForLablesRefresh.IsEnabled)
							_cTimerForLablesRefresh.Start();
					}
				}
				catch { }
            }
            private void ComingUpGet(object s, EventArgs args)
            {
                _cDBI.ComingUpGetAsync();
            }
            private void LablesRefresh(object s, EventArgs args)
            {
                long nLeft = DateTime.MinValue == _dtCurrentStop ? 0 : (long)_dtCurrentStop.Subtract(DateTime.Now).TotalMilliseconds / 40;
                if (250 > nLeft && _cTimerForComingUp.IsEnabled)
                    _cTimerForComingUp.Stop();
                if (250 < nLeft && !_cTimerForComingUp.IsEnabled)
                    _cTimerForComingUp.Start();
                if (1 > nLeft)
                {
                    _cTimerForLablesRefresh.Stop();
                    ComingUpGet(null, null);
                }
                else
                    _lblCurrentLeft.Content = nLeft.ToFramesString(false, false, true, true);
            }
        }
		#region event handlers
		#region UI
		private void Playlist_Loaded(object sender, RoutedEventArgs e)
		{
			_ui_dtpArchiveDate.SelectedDateChanged += _ui_dtpArchiveDate_SelectedDateChanged;
			_ui_dtpPlannedDate.SelectedDateChanged += _ui_dtpPlannedDate_SelectedDateChanged;
		}
		protected override void OnNavigatedTo(NavigationEventArgs e)
        {
		}
		private void Archived_Click(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			DateTime dtStart = DateTime.MinValue;
			switch (((Button)sender).Name)
			{
				case "_ui_btnArchivedToday":
					dtStart = DateTime.Today;
					break;
				case "_ui_btnArchivedYesterday":
					dtStart = DateTime.Today.AddDays(-1);
					break;
				case "_ui_btnArchivedDaysBefore":
					dtStart = DateTime.Today.AddDays(-_ui_nudDaysBeforeQty.Value);
					break;
			}
			ArchiveShow(dtStart);
        }
		private void _ui_dtpArchiveDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			_dlgProgress.Show();
			ArchiveShow(_ui_dtpArchiveDate.SelectedDate.Value);
		}
		private void ArchiveShow(DateTime dtStart)
		{
			DateTime dtEnd = dtStart.AddDays(1).AddMinutes(_nOverlapMinutes);
			if (dtEnd > DateTime.Now)
				dtEnd = DateTime.Now;
			_cDBI.PlaylistItemsArchGetAsync(dtStart.AddMinutes(-_nOverlapMinutes), dtEnd);
		}
		private void _ui_btnOnAirRefresh_Click(object sender, RoutedEventArgs e)
        {
            _dlgProgress.Show();
            IdNamePair[] aNPs = new IdNamePair[3];
            aNPs[0] = new IdNamePair(); aNPs[0].nID = 2; aNPs[0].sName = "queued";  // DB request will on sStatusName field only! (not on idStatuses)
            aNPs[1] = new IdNamePair(); aNPs[1].nID = 3; aNPs[1].sName = "prepared";
            aNPs[2] = new IdNamePair(); aNPs[2].nID = 4; aNPs[2].sName = "onair";
            _cDBI.PlaylistItemsGetAsync(aNPs);
        }
		private void _ui_nudDaysBeforeQty_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (null == _ui_nudDaysBeforeQty)
				return;
			_ui_lblDaysBefore.Content = GetCorrectWordForm(g.Helper.sDays, _ui_nudDaysBeforeQty.Value.ToInt32());
		}
		private void Planned_Click(object sender, RoutedEventArgs e)
        {
            _dlgProgress.Show();
            DateTime dtDay = DateTime.MinValue;
            switch (((Button)sender).Name)
            {
                case "_ui_btnToday":
                    dtDay = DateTime.Today;
                    break;
                case "_ui_btnTomorrow":
                    dtDay = DateTime.Today.AddDays(1);
                    break;
                case "_ui_btnTheDayAfterTomorrow":
                    dtDay = DateTime.Today.AddDays(2);
                    break;
            }
			PlannedShow(dtDay);
        }
		private void _ui_dtpPlannedDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			_dlgProgress.Show();
			PlannedShow(_ui_dtpPlannedDate.SelectedDate.Value);
		}
		private void PlannedShow(DateTime dtStart)
		{
			_dtActivePlannedDate = dtStart;
			DateTime dtNextDay = dtStart.AddDays(1);
			_cDBI.PlaylistItemsPlanGetAsync(dtStart.AddMinutes(-_nOverlapMinutes), dtNextDay.AddMinutes(_nOverlapMinutes));
		}
		private bool IsThisDateInRange(DateTime dtCheckingDate)
		{
			DateTime dtNextDay = _dtActivePlannedDate.AddDays(1).AddMinutes(_nOverlapMinutes); // для захлестика....
			if (dtCheckingDate > _dtActivePlannedDate.AddMinutes(-_nOverlapMinutes) && dtCheckingDate < dtNextDay)
				return true;
			else
				return false;
		}
        private void _ui_btnImport_Click(object sender, RoutedEventArgs e)
        {
			if (_ui_lblPLImportText.Visibility == Visibility.Collapsed)
			{
				_dlgMsg.ShowError(g.Replica.sErrorPlaylist1);
				_dlgProgress.Close();
				return;
			}
			PlaylistImport dlgPLImport = new PlaylistImport();
			dlgPLImport.Closed += new EventHandler(dlgPLImport_Closed);
            dlgPLImport.Show();
        }
		void dlgPLImport_Closed(object sender, EventArgs e)
		{
            PlaylistImport dlgPLImport = (PlaylistImport)sender;
			dlgPLImport.Closed -= dlgPLImport_Closed;
			if (dlgPLImport.DialogResult.HasValue && dlgPLImport.DialogResult.Value)
			{
				_dlgProgress.Show();
				PlaylistItem[] aPLI = dlgPLImport.aPlaylistItems.Where(row => !row.bPlug).ToArray();  //.OrderBy(o => o.dtStartPlanned)
				_cDBI.BeforeAddCheckRangeAsync(aPLI[0].dtStartPlanned, aPLI[aPLI.Length - 1].dtStartPlanned, aPLI);
			}
			else
			{
				_dlgProgress.Show();
				_dlgProgress.Close();
			}
		}
        private void _ui_dgPlanned_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = _ui_dgPlanned.SelectedItems.Count;
            if (0 == i)
                _ui_lblPlannedSelected.Content = " ";
            else
                _ui_lblPlannedSelected.Content = i.ToString();

			if (1 == i)
				_cPlayListItemCurrent = (PlaylistItemSL)_ui_dgPlanned.SelectedItem;
			else
				_cPlayListItemCurrent = null;

			if (0 < _ui_nudHoursQty.Value && 1 == i && DateTime.MaxValue > _cPlayListItemCurrent.dtTimingsUpdate || 0 == _ui_nudHoursQty.Value)
				_ui_btnRecalculatePart.IsEnabled = true;
			else
				_ui_btnRecalculatePart.IsEnabled = false;
        }
		private void _ui_nudHoursQty_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (null == _ui_dgPlanned)
				return;
			int i = _ui_dgPlanned.SelectedItems.Count;
			if (0 < _ui_nudHoursQty.Value && 1 == i || 0 == _ui_nudHoursQty.Value)
				_ui_btnRecalculatePart.IsEnabled = true;
			else
				_ui_btnRecalculatePart.IsEnabled = false;
		}
        private void _ui_rpAir_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpAir.IsOpen)
            {
                if (null == _ui_dgOnAir.ItemsSource || 0 == ((PlaylistItemSL[])_ui_dgOnAir.ItemsSource).Count())
                    _ui_btnOnAirRefresh_Click(null, null);
            }
        }
        private void _ui_rpPlanned_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpPlanned.IsOpen)
            {
                if (null == _ui_dgPlanned.ItemsSource || 0 == ((List<PlaylistItemSL>)_ui_dgPlanned.ItemsSource).Count())
                    Planned_Click(_ui_btnToday, null);
            }
        }
        private void _ui_rpArchived_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpArchived.IsOpen)
            {
                if (null == _ui_dgArchived.ItemsSource || 0 == ((PlaylistItemSL[])_ui_dgArchived.ItemsSource).Count())
					Archived_Click(_ui_btnArchivedToday, null);
            }
        }
        private void _ui_btnRecalculate_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button ui_btnRecalc = ((Button)sender);
				if (_ui_lblPLRecalcPartText.Visibility == Visibility.Visible)
				{
					_ui_lblPLRecalcPartText.Visibility = Visibility.Collapsed;
					_ui_pbPLRecalcPartProgress.Visibility = Visibility.Visible;
					int nHrsQty = _ui_nudHoursQty.Value.ToInt32();
					long nID = null == _cPlayListItemCurrent ? -1 : _cPlayListItemCurrent.nID;
					MsgBox dlgRecalculate;
					if (0 == nHrsQty)
						dlgRecalculate = new MsgBox(g.Replica.sNoticePlaylist1 + "?", g.Common.sAttention + "!", MsgBox.MsgBoxButton.OKCancel);
					else if (0 < nHrsQty && 0 < nID)
                        dlgRecalculate = new MsgBox(g.Replica.sNoticePlaylist2.Fmt(nHrsQty.ToStr(), GetCorrectWordForm(g.Helper.sHours, _ui_nudHoursQty.Value.ToInt32()), _cPlayListItemCurrent.sName), g.Common.sAttention + "!", MsgBox.MsgBoxButton.OKCancel);
					else
					{
						_dlgMsg.ShowError(g.Replica.sErrorPlaylist2);
						return;
					}
					dlgRecalculate.Closed += new EventHandler(dlgRecalculate_Closed);
					dlgRecalculate.Tag = new long[2] { nID, nHrsQty }; //EMERGENCY:l а зачем ты вот это присвоение делаешь? ты же его нигде не используешь)   ))) использую - см "dlg.Tag" 
					dlgRecalculate.Show();
				}
			}
			catch { }
        }
		void dlgRecalculate_Closed(object sender, EventArgs e)
		{
			//_dlgProgress.Show();
			//_dlgProgress.Close();
			MsgBox dlg = (MsgBox)sender;
			if (dlg.DialogResult == true && dlg.enMsgResult == MsgBox.MsgBoxButton.OK)
			{
				long[] aParams = (long[])dlg.Tag;
				_cDBI.PlaylistRecalculateQueryAsync(aParams[0], (ushort)aParams[1]);
			}
			else
				ResetButtonPLRecalc();
		}

        void MarkTypeCell(DataGridRow row, DataGrid dg, PlaylistItemSL cPLI)
        {
            if (null != row && null != dg && null != cPLI)
            {
                TextBox tb = ((TextBox)dg.Columns[8].GetCellContent(row));
                if (!cPLI.bIsInserted)
                {
					if (cPLI.cAsset == null)  // plug
						tb.Background = Coloring.Playlist.cTypeColumn_PlugBackgr;
					else if (cPLI.cAsset.stVideo.cType == null)
						tb.Background = Coloring.Playlist.cTypeColumn_ErrorBackgr;
					else if (cPLI.cAsset.stVideo.cType.sName == "clip")
                        tb.Background = Coloring.Playlist.cTypeColumn_ClipsBackgr;
                    else if (cPLI.cAsset.stVideo.cType.sName == "advertisement")
                        tb.Background = Coloring.Playlist.cTypeColumn_AdvertsBackgr;
                    else if (cPLI.cAsset.stVideo.cType.sName == "design")
                        tb.Background = Coloring.Playlist.cTypeColumn_DesignBackgr;
                    else if (cPLI.cAsset.stVideo.cType.sName == "program")
                        tb.Background = Coloring.Playlist.cTypeColumn_ProgramsBackgr;
                    else
                        tb.Background = Coloring.Playlist.cTypeColumn_ErrorBackgr;
				}
                else
                    tb.Background = Coloring.Playlist.cTypeColumn_InsertedBackgr;
            }
		}
        void _ui_dgOnAir_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var item = (PlaylistItemSL)e.Row.DataContext;
            if ("queued" == item.cStatus.sName)
                e.Row.Background = Coloring.Playlist.cRow_OnAirQueuedBackgr;
            else if ("prepared" == item.cStatus.sName)
                e.Row.Background = Coloring.Playlist.cRow_OnAirPreparedBackgr;
            else if ("onair" == item.cStatus.sName)
				e.Row.Background = Coloring.Playlist.cRow_OnAirOnAirBackgr;
            else
				e.Row.Background = Coloring.Playlist.cRow_CachedBackgr;  // вообще не должно быть

			TextBox tb = ((TextBox)_ui_dgOnAir.Columns[0].GetCellContent(e.Row));

			if (item.bCached)
				tb.Background = Coloring.Playlist.cRow_CachedBackgr;
			else
				tb.Background = Coloring.Playlist.cRow_UnCachedBackgr;

			MarkTypeCell(e.Row, _ui_dgOnAir, item);
        }
        private void _ui_dgPlanned_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var item = (PlaylistItemSL)e.Row.DataContext;
			if (item.bIsInserted)
				e.Row.Foreground = Coloring.Playlist.cRow_PlannedInsertedForegr;
			else
				e.Row.Foreground = Coloring.Playlist.cRow_PlannedNormalForegr;

			if (item.cFile.cStorage.sPath.Contains("clip"))
				e.Row.Background = Coloring.Playlist.cRow_PlannedClipBackgr;
			else if (item.cFile.cStorage.sPath.Contains("advertisement"))
				e.Row.Background = Coloring.Playlist.cRow_PlannedAdvBackgr;
			else if (item.cFile.cStorage.sPath.Contains("design"))
				e.Row.Background = Coloring.Playlist.cRow_PlannedDesignBackgr;
			else if (item.cFile.cStorage.sPath.Contains("program"))
				e.Row.Background = Coloring.Playlist.cRow_PlannedProgramBackgr;
			else if (item.cFile.cStorage.sPath.Contains("trailer"))
				e.Row.Background = Coloring.Playlist.cRow_PlannedTrailersBackgr;
			else if (item.cFile.cStorage.sPath.Contains("plug"))
				e.Row.Background = Coloring.Playlist.cRow_PlannedPlugsBackgr;
			else
				e.Row.Background = Coloring.Playlist.cRow_PlannedOtherBackgr;

			TextBox tb = ((TextBox)_ui_dgPlanned.Columns[0].GetCellContent(e.Row));
			if (item.bCached)
				tb.Background = Coloring.Playlist.cRow_CachedBackgr;
			else
				tb.Background = Coloring.Playlist.cRow_UnCachedBackgr;

			MarkTypeCell(e.Row, _ui_dgPlanned, item);
        }
        void _ui_dgArchived_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var item = (PlaylistItemSL)e.Row.DataContext;
            MarkTypeCell(e.Row, _ui_dgArchived, item);
        }
        private void _ui_btnAdd_Click(object sender, RoutedEventArgs e)
        {
			PlaylistItemSL cLast;
			List<PlaylistItemSL> aPL = null;
			if (1 < _ui_dgPlanned.SelectedItems.Count)
			{
				_dlgMsg.ShowError(g.Replica.sNoticePlaylist3);
			}
			else
			{
				_cPlayListItemCurrent = (PlaylistItemSL)_ui_dgPlanned.SelectedItem;
				AssetsChooser dlgAssetsChooser = new AssetsChooser();
				dlgAssetsChooser.Closed += new EventHandler(dlgAssetsChooser_Closed);
				dlgAssetsChooser.dtStart = DateTime.Now;

				if (null == _cPlayListItemCurrent) // ничего не выделено или, возможно, вообще ничего нет ещё
				{
					if (_ui_dgPlanned.ItemsSource != null)
						aPL = (List<PlaylistItemSL>)_ui_dgPlanned.ItemsSource;

					if (aPL == null || aPL.Count == 0)   // вообще ничего нет
						dlgAssetsChooser.dtSelected = DateTime.Now.AddHours(1);
					else  // что-то есть - берем последний, раз не сказано какой
					{
						_cPlayListItemCurrent = cLast = aPL[aPL.Count - 1];
						dlgAssetsChooser.dtSelected = cLast.dtStartPlanned.AddMilliseconds(40 * (cLast.nFrameStop - cLast.nFrameStart + 1));
					}
				}
				else
					dlgAssetsChooser.dtSelected = _cPlayListItemCurrent.dtStartPlanned.AddMilliseconds(40 * (_cPlayListItemCurrent.nFrameStop - _cPlayListItemCurrent.nFrameStart + 1));


				dlgAssetsChooser.dtEnd = DateTime.Now.AddMonths(3);
				dlgAssetsChooser.Show();
			}
        }
        void dlgAssetsChooser_Closed(object sender, EventArgs e)
		{
			AssetsChooser dlgAssetsChooser = (AssetsChooser)sender;
			dlgAssetsChooser.Closed -= dlgAssetsChooser_Closed;
            List<AssetSL> aAssets = dlgAssetsChooser.aSelectedAssets;
			if (dlgAssetsChooser.DialogResult == null || dlgAssetsChooser.DialogResult == false || aAssets.Count < 1)
				return;

			if (dlgAssetsChooser.dtSelected < DateTime.Now)
				return;

			_dlgProgress.Show();
			
			if (dlgAssetsChooser.bIsBlock) // если вставляем на время
			{
				long nTotalDur = 0;

				if (dlgAssetsChooser.dtPlanned < DateTime.MaxValue)  // вставляем просто пленнед
				{ 
					//_dlgProgress.Close();
					DoAddingAssetsAsBlock(dlgAssetsChooser.dtHard, dlgAssetsChooser.dtSoft, dlgAssetsChooser.dtPlanned, dlgAssetsChooser.aSelectedAssets);  
				}
				else  //  вставляем timed
				{
					foreach (AssetSL cAss in aAssets)
						nTotalDur += cAss.nFrameOut - cAss.nFrameIn + 1;
					_cDBI.BeforeAddCheckRangeAsync(dlgAssetsChooser.dtSelected, dlgAssetsChooser.dtSelected.AddMilliseconds(40 * nTotalDur), dlgAssetsChooser);
				}
			}
			else if (null == _cPlayListItemCurrent) // если пустой пл еще (первый раз)
			{
				long nTotalDur = 0;
				foreach (AssetSL cAss in aAssets)
					nTotalDur += cAss.nFrameOut - cAss.nFrameIn + 1;
				_cDBI.BeforeAddCheckRangeAsync(dlgAssetsChooser.dtSelected, dlgAssetsChooser.dtSelected.AddMilliseconds(40 * nTotalDur), dlgAssetsChooser);
			}
			else
			{
				if (!_cPlayListItemCurrent.bIsInserted)   // !_cPlayListItemCurrent.bCached &&
				{
					List<PlaylistItemSL> aPL;
					aPL = (List<PlaylistItemSL>)_ui_dgPlanned.ItemsSource;
					int nPrevIndx = aPL.IndexOf((PlaylistItemSL)helper.FindPrevItem(aPL, typeof(PlaylistItemSL), "nID", _cPlayListItemCurrent.nID));
					if (aPL.Count > nPrevIndx + 2 && aPL[nPrevIndx + 2].dtStartHardSoft.Subtract(_cPlayListItemCurrent.dtStartHardSoft).TotalSeconds == 1)
					{ // вставляем внутрь блока
						List<PlaylistItemSL> aTailOfBlock = new List<PlaylistItemSL>();
						int nI = nPrevIndx + 2;
						aTailOfBlock.Add(aPL[nI]);
						while (aPL.Count > nI + 1 && aPL[nI + 1].dtStartHardSoft.Subtract(aPL[nI].dtStartHardSoft).TotalSeconds == 1)
						{
							aTailOfBlock.Add(aPL[nI + 1]);
							nI++;
						}
						foreach (PlaylistItemSL cPLI in aTailOfBlock)
							cPLI.dtStartHardSoft = cPLI.dtStartHardSoft.AddSeconds(aAssets.Count);

						PlaylistItem[] aInsertedPLIs = PreparePLIsFromAssets(DateTime.MaxValue, _cPlayListItemCurrent.dtStartHardSoft.AddSeconds(1), _cPlayListItemCurrent.dtStopPlanned, aAssets);
						int nHours = (int)aTailOfBlock[aTailOfBlock.Count - 1].dtStopPlanned.Subtract(aTailOfBlock[0].dtStartPlanned).TotalHours + 1;
						_cDBI.InsertInBlockAsync(aInsertedPLIs, PlaylistItemSL.GetArrayOfBases(aTailOfBlock.ToArray()), new long[2] { _cPlayListItemCurrent.nID, 5 });
					}
					else // вставляем после блока или вообще вне блока
						_cDBI.PlaylistInsertAsync(AssetSL.GetArrayOfBases(aAssets.ToArray()), PlaylistItemSL.GetBase(_cPlayListItemCurrent), aAssets);
				}
				else
				{
					_dlgMsg.ShowError(g.Replica.sNoticePlaylist36);
					_dlgProgress.Close();
				}
			}
		}
		#region контекстное меню
        private void _ui_dgPlanned_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _cPlayListItemCurrent = (PlaylistItemSL)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
				if (null != _ui_dgPlanned.SelectedItems && 1 >= _ui_dgPlanned.SelectedItems.Count)
					_ui_dgPlanned.SelectedItem = _cPlayListItemCurrent;
            }
            catch
            {
                _cPlayListItemCurrent = null;
            }
        }
        private void _ui_cmPlanned_Opened(object sender, RoutedEventArgs e)
        {
            if (null != _cPlayListItemCurrent)
            {
                int nSelectedItems = _ui_dgPlanned.SelectedItems.Count == 0 ? 1 : _ui_dgPlanned.SelectedItems.Count;
                string sS = "";
                if (1 == nSelectedItems)
                {
                    sS = "  {" + _cPlayListItemCurrent.sName +"}";
                    _ui_cmPlannedMove.IsEnabled = true;
					_ui_cmPlannedProperties.IsEnabled = true;
                }
                else if (nSelectedItems < 10) sS = nSelectedItems.ToString() + " items!";
                else if (nSelectedItems < 20) sS = nSelectedItems.ToString() + " Items!!!!";
                else if (_aPlayListItemsPlanned.Count == nSelectedItems) sS = "ALL " + nSelectedItems.ToString() + " ITEMS !!!!!!!!";
                else sS = nSelectedItems.ToString() + " ITEMS !!!!!!!!";
                _sPlannedDelete_Header = _ui_cmPlannedDelete.Header.ToString();
				_ui_cmPlannedDelete.Header = _sPlannedDelete_Header + " " + sS;
                _ui_cmPlannedDelete.IsEnabled = true;
                _ui_cmPlannedDeleteSince.IsEnabled = true;

				//if (!_cPlayListItemCurrent.bCached && !_cPlayListItemCurrent.bIsInserted)
				_ui_cmPlannedAdd.IsEnabled = true;

				if (0 < _ui_nudHoursQty.Value && 1 == nSelectedItems && DateTime.MaxValue > _cPlayListItemCurrent.dtTimingsUpdate || 0 == _ui_nudHoursQty.Value)
					_ui_cmPlannedRecalculatePart.IsEnabled = true;
				if (0 < nSelectedItems)
				{
					_ui_cmPlannedGroupMoving.IsEnabled = true;
					_ui_cmPlannedGroupMoving.Header = g.Replica.sNoticePlaylist5;
					foreach (object oPLI in _ui_dgPlanned.SelectedItems)
						if (((PlaylistItemSL)oPLI).dtTimingsUpdate == DateTime.MaxValue)
							_ui_cmPlannedGroupMoving.IsEnabled = false;

					_ui_cmPlannedCopySelected.IsEnabled = true; // надо подумать... штука опасная и ненужная вроде
					_ui_cmPlannedTimedCopySelected.IsEnabled = true;
					_ui_cmPlannedAdvancedPL.IsEnabled = true;
                }
            }
            if (DateTime.MinValue < _dtActivePlannedDate)
            {
                _ui_cmPlannedRefresh.IsEnabled = true;
            }
            if (!access.scopes.playlist.bCanCreate)
				_ui_cmPlannedAdd.IsEnabled = _ui_cmPlannedCopySelected.IsEnabled = _ui_cmPlannedTimedCopySelected.IsEnabled = _ui_cmPlannedAdvancedPL.IsEnabled = false;
            if (!access.scopes.playlist.bCanUpdate)
				_ui_cmPlannedProperties.IsEnabled = _ui_cmPlannedMove.IsEnabled = _ui_cmPlannedRecalculatePart.IsEnabled = _ui_cmPlannedGroupMoving.IsEnabled = false;
            if (!access.scopes.playlist.bCanDelete)
				_ui_cmPlannedDelete.IsEnabled = _ui_cmPlannedDeleteSince.IsEnabled = false;
            if (!access.scopes.assets.bCanRead)
				_ui_cmPlannedProperties.IsEnabled = false;

            _ui_cmPlannedDeleteSince.IsEnabled = Preferences.cServer.bContextMenuDeleteSince;

			_ui_cmPlannedDelete.Refresh();
			_ui_cmPlannedDeleteSince.Refresh();
			_ui_cmPlannedAdd.Refresh();
			_ui_cmPlannedRecalculatePart.Refresh();
			_ui_cmPlannedGroupMoving.Refresh();
			_ui_cmPlannedProperties.Refresh();
			_ui_cmPlannedCopySelected.Refresh();
			_ui_cmPlannedAdvancedPL.Refresh();
            _ui_cmPlannedTimedCopySelected.Refresh();
			_ui_cmPlannedRefresh.Refresh();
        }
        private void _ui_cmPlanned_Closed(object sender, RoutedEventArgs e)
        {
            _ui_cmPlannedDelete.Header = _sPlannedDelete_Header;
            _ui_cmPlannedDelete.IsEnabled = false;
			_ui_cmPlannedDelete.Refresh();
            _ui_cmPlannedDeleteSince.IsEnabled = false;
            _ui_cmPlannedRefresh.IsEnabled = false;
			_ui_cmPlannedRefresh.Refresh();
            _ui_cmPlannedAdd.IsEnabled = false;
			_ui_cmPlannedRecalculatePart.IsEnabled = false;
			_ui_cmPlannedGroupMoving.IsEnabled = false;
			_ui_cmPlannedGroupMoving.Header = g.Replica.sNoticePlaylist5;
			_ui_cmPlannedGroupMoving.Refresh();
			_ui_cmPlannedCopySelected.IsEnabled = false;
			_ui_cmPlannedCopySelected.Refresh();
			_ui_cmPlannedTimedCopySelected.IsEnabled = false;
			_ui_cmPlannedTimedCopySelected.Refresh();
			_ui_cmPlannedAdvancedPL.IsEnabled = false;
			_ui_cmPlannedAdvancedPL.Refresh();


			_ui_cmPlannedDeleteSince.Refresh();
            _ui_cmPlannedAdd.Refresh();
			_ui_cmPlannedRecalculatePart.Refresh();
        }
        private void _ui_cmPlanned_Delete(object sender, RoutedEventArgs e)
        {
            if (1 > _ui_dgPlanned.SelectedItems.Count && null == _cPlayListItemCurrent)
                return;

			if (10 < _ui_dgPlanned.SelectedItems.Count)
			{
				MsgBox cMsgDel = new MsgBox(g.Replica.sNoticePlaylist6, g.Common.sAttention.ToUpper() + "!!", MsgBox.MsgBoxButton.OKCancel);
				cMsgDel.Closed += new EventHandler(cMsgDel_Closed);
				cMsgDel.Show();
			}
			else
				PlannedDelete();
        }

		void cMsgDel_Closed(object sender, EventArgs e)
		{
			//_dlgProgress.Show();
			//_dlgProgress.Close();
			MsgBox cMsg = (MsgBox)sender;
			cMsg.Closed -= cMsgDelSince_Closed;
			if (MsgBox.MsgBoxButton.OK == cMsg.enMsgResult)
			{
				PlannedDelete();
			}
		}
		List<PlaylistItemSL> _aPLIsToDelete;
		void PlannedDelete()
		{
			_aPLIsToDelete = new List<PlaylistItemSL>();
			MsgBox cMsgDelNewPLIs;
 			bool bIfThereAreNewPLIs=false;
			if (0 < _ui_dgPlanned.SelectedItems.Count)
			{
				foreach (PlaylistItemSL cPLI in _ui_dgPlanned.SelectedItems)
				{
					if (cPLI.nID > 0)
						_aPLIsToDelete.Add(cPLI);
					else
						bIfThereAreNewPLIs = true;
				}
			}
			else
			{
				if (_cPlayListItemCurrent.nID > 0)
					_aPLIsToDelete.Add(_cPlayListItemCurrent);
				else
					bIfThereAreNewPLIs = true;
			}

			if (bIfThereAreNewPLIs)
			{
				cMsgDelNewPLIs = new MsgBox("Некоторые элементы не могут быть удалены без обновления плей-листа, т.к. были только что добавлены. Вы хотите продолжить удаление всё-равно??", "ВНИМАНИЕ!!", MsgBox.MsgBoxButton.OKCancel);
				cMsgDelNewPLIs.Closed += new EventHandler(cMsgDelNewPLIs_Closed);
				cMsgDelNewPLIs.Show();
			}
			else
			{
				_dlgProgress.Show();
				DeletSelectedItems(_aPLIsToDelete);
            }
		}
		void DeletSelectedItems(List<PlaylistItemSL> aPLIs)
		{
			List<IdNamePair> aSelectedToDel = new List<IdNamePair>();
			foreach (PlaylistItemSL cPLI in aPLIs)
			{
				aSelectedToDel.Add(new IdNamePair() { nID = cPLI.nID, sName = cPLI.sName });
			}
			_cDBI.PlaylistItemsDeleteAsync(aSelectedToDel.ToArray(), aPLIs);
		}
		void cMsgDelNewPLIs_Closed(object sender, EventArgs e)
		{
			MsgBox cMsg = (MsgBox)sender;
			cMsg.Closed -= cMsgDelSince_Closed;
			_dlgProgress.Show();
			if (MsgBox.MsgBoxButton.OK == cMsg.enMsgResult && _aPLIsToDelete.Count > 0)
			{
				DeletSelectedItems(_aPLIsToDelete);
            }
			else
				_dlgProgress.Close(); // против глюка с белесым экраном
		}


        private void _ui_cmPlannedDeleteSince_Click(object sender, RoutedEventArgs e)
        {
            if (1 > _ui_dgPlanned.SelectedItems.Count && null == _cPlayListItemCurrent)
                return;

			MsgBox cMsgDelSince = new MsgBox(g.Replica.sNoticePlaylist7 + Environment.NewLine + "'" + _cPlayListItemCurrent.sName + "' ???", g.Common.sAttention + "!!", MsgBox.MsgBoxButton.OKCancel);
			cMsgDelSince.Closed += new EventHandler(cMsgDelSince_Closed);
			cMsgDelSince.Show();
        }
		void cMsgDelSince_Closed(object sender, EventArgs e)
		{
			_dlgProgress.Show();
			MsgBox cMsg = (MsgBox)sender;
			cMsg.Closed -= cMsgDelSince_Closed;
			if (MsgBox.MsgBoxButton.OK == cMsg.enMsgResult)
				_cDBI.PlaylistItemsDeleteSinceAsync(_cPlayListItemCurrent.dtStartPlanned.Subtract(new System.TimeSpan(0, 0, 2))); // -2секунды - а то текущий элемент не удаляется....
			else
				_dlgProgress.Close(); // против глюка с белесым экраном
		}
        private void _ui_cmPlanned_Move(object sender, RoutedEventArgs e)
        {
			sPROBA_ERR += g.Replica.sNoticePlaylist8 + Environment.NewLine;
            PlaylistItemSL cFirst = ((List<PlaylistItemSL>)_ui_dgPlanned.ItemsSource)[0];
            DateTime dtFirst = cFirst.dtStartPlanned.AddMinutes(5);
			MsgBox cMsgDate = new MsgBox(g.Replica.sNoticePlaylist9, g.Replica.sNoticePlaylist10, MsgBox.MsgBoxButton.OKCancel, dtFirst, dtFirst.AddDays(30), _cPlayListItemCurrent.dtStartPlanned);
            cMsgDate.Closed += new EventHandler(cMsgDate_Closed);
            cMsgDate.Show();
        }
        void cMsgDate_Closed(object sender, EventArgs e)
        {
			_dlgProgress.Show();
			sPROBA_ERR += g.Replica.sNoticePlaylist11 + ((MsgBox)sender).enMsgResult.ToString() + Environment.NewLine;
            if (MsgBox.MsgBoxButton.OK == ((MsgBox)sender).enMsgResult)
                _cDBI.PlaylistItemStartsSetAsync(_cPlayListItemCurrent.nID, ((MsgBox)sender).dtSelectedDateTime, _cPlayListItemCurrent.dtStartPlanned);
			else
				_dlgProgress.Close();
        }
        private void _ui_cmPlanned_Add(object sender, RoutedEventArgs e)
        {
            _ui_btnAdd_Click(null, null);
        }
        private void _ui_cmPlanned_Properties(object sender, RoutedEventArgs e)
        {
			PLIProreties ui_childPLIP;
			if (1 == _ui_dgPlanned.SelectedItems.Count)
			{
				ui_childPLIP = new PLIProreties(_cPlayListItemCurrent);
				ui_childPLIP.Closed += new EventHandler(ui_childPLIP_Closed);
				ui_childPLIP.Show();
			}
        }
		void ui_childPLIP_Closed(object sender, EventArgs e)
		{
			_dlgProgress.Show();
			PLIProreties dlgPLIP = (PLIProreties)sender;
			dlgPLIP.Closed -= ui_childPLIP_Closed;
			_dlgProgress.Close();
		}
        private void _ui_cmPlanned_Refresh(object sender, RoutedEventArgs e)
        {
			if (_dtActivePlannedDate > DateTime.MinValue)
				PlannedShow(_dtActivePlannedDate);
        }
        private void _ui_cmOnAir_Opened(object sender, RoutedEventArgs e)
        {

        }
        private void _ui_cmOnAir_Refresh(object sender, RoutedEventArgs e)
        {

        }
        private void _ui_cmArch_Opened(object sender, RoutedEventArgs e)
        {
			_ui_cmArchivedTimedCopySelected.IsEnabled = true;
			_ui_cmArchivedTimedCopySelected.Refresh();
			
			if (!access.scopes.playlist.bCanCreate)
				_ui_cmArchivedTimedCopySelected.Visibility = Visibility.Collapsed;
        }
		private void _ui_cmArch_Closed(object sender, RoutedEventArgs e)
		{
			_ui_cmArchivedTimedCopySelected.IsEnabled = false;
			_ui_cmArchivedTimedCopySelected.Refresh();
		}
		private void _ui_cmPlannedRecalculatePart_Click(object sender, RoutedEventArgs e)
		{
			_ui_btnRecalculate_Click(null, null);
		}
		private void _ui_cmPlanned_GroupMoving(object sender, RoutedEventArgs e)
		{
			if (0 < _ui_dgPlanned.SelectedItems.Count)
			{
				List<PlaylistItemSL> aPLIs = new List<PlaylistItemSL>();
				foreach (object oPLI in _ui_dgPlanned.SelectedItems)
					aPLIs.Add((PlaylistItemSL)oPLI);
				aPLIs = aPLIs.OrderBy(o => o.dtStartHardSoftPlanned).ToList();

				GroupMovingPLIs dlgGroupMoving;
				dlgGroupMoving = new GroupMovingPLIs(PlaylistItemSL.GetArrayOfBases(aPLIs.ToArray()).ToList());
				dlgGroupMoving.Closed += new EventHandler(dlgGroupMoving_Closed);
				dlgGroupMoving.Show();
			}
		}
		void dlgGroupMoving_Closed(object sender, EventArgs e)
		{
			_dlgProgress.Show();
			GroupMovingPLIs dlgGM = (GroupMovingPLIs)sender;
			if (dlgGM.DialogResult ?? false)
			{
				_cDBI.GroupMovingAsync(((GroupMovingPLIs)sender).aPLIs.ToArray());
			}
			else
				_dlgProgress.Close();
			dlgGM.Closed -= dlgGroupMoving_Closed;
		}
		private void _ui_cmPlanned_CopySelected(object sender, RoutedEventArgs e)
		{
			_dlgMsg.Closed += new EventHandler(_dlgMsg_Closed);
			_dlgMsg.ShowQuestion(g.Replica.sNoticePlaylist12.Fmt(Environment.NewLine), "1");
		}
		private void _ui_cmPlanned_AdvancedPL(object sender, RoutedEventArgs e)
		{
			controls.childs.replica.sl.AdvancedPlaylist dlgAdvancedPL = new controls.childs.replica.sl.AdvancedPlaylist();
			dlgAdvancedPL.Closed += DlgAdvancedPL_Closed;
            dlgAdvancedPL.dtSelectedInPL = _cPlayListItemCurrent.dtStartPlanned;
            dlgAdvancedPL.Show();
		}
		
		private void DlgAdvancedPL_Closed(object sender, EventArgs e)
		{
			controls.childs.replica.sl.AdvancedPlaylist dlgAdvancedPL = (controls.childs.replica.sl.AdvancedPlaylist)sender;
			dlgAdvancedPL.Closed -= DlgAdvancedPL_Closed;
			_dlgProgress.Show();
			_dlgProgress.Close();
		}

		private void _dlgMsg_Closed_negative(object sender, EventArgs e)
		{
			if (true)    //_dlgMsg.DialogResult == false
			{
				//_dlgProgress.Show();
				//_dlgProgress.Close();
			}
		}
		void _dlgMsg_Closed(object sender, EventArgs e)
		{
			_dlgMsg.Closed -= _dlgMsg_Closed;
			int nCopiesQty;
			if (_dlgMsg.DialogResult == true && int.TryParse(_dlgMsg.sText, out nCopiesQty) && 0 < _ui_dgPlanned.SelectedItems.Count)
			{
				_dlgProgress.Show();
				_cDBI.PlaylistLastElementGetAsync(nCopiesQty);
				//_cPlayListItemCurrent = _aPlayListItemsPlanned[_aPlayListItemsPlanned.Count - 1];
				//_cDBI.PlaylistInsertCopiesAsync(aAssets.ToArray(), PlaylistItemSL.GetBase(_cPlayListItemCurrent), nCopiesQty, aAllAssets);
			}
		}
		private void _ui_cmPlannedTimedCopySelected_Click(object sender, RoutedEventArgs e)
		{
			_ui_cmTimedCopySelected_Click(_ui_dgPlanned);
		}
		private void _ui_cmArchivedTimedCopySelected_Click(object sender, RoutedEventArgs e)
		{
			_ui_cmTimedCopySelected_Click(_ui_dgArchived);
		}
		private void _ui_cmTimedCopySelected_Click(DataGrid dgCurrent)
		{
			if (_ui_lblPLImportText.Visibility == Visibility.Collapsed)
			{
				_dlgMsg.ShowError(g.Replica.sErrorPlaylist3);
				return;
			}
			PlaylistItemSL cLastPLI = null;
			PlaylistItemSL cPLI = null;
			List<PlaylistItemSL> aPLIs = new List<PlaylistItemSL>();
			foreach (object oPLI in dgCurrent.SelectedItems)
			{
				cPLI = (PlaylistItemSL)oPLI;
				aPLIs.Add(cPLI);
				if (null == cLastPLI || cLastPLI.dtStartPlanned < cPLI.dtStartPlanned)
					cLastPLI = cPLI;
			}
			DateTime dtEnd = cLastPLI.dtStartPlanned.AddMilliseconds(cLastPLI.nFramesQty * 40);
			DateTime dtTrunc = dtEnd.AddMinutes(-dtEnd.Minute).AddSeconds(-dtEnd.Second).AddMilliseconds(-dtEnd.Millisecond);
			DateTime dtNearestRound = dtEnd.Minute < 30 ? dtTrunc : dtTrunc.AddHours(1);
			MsgBox dlgMsg_TimedCopy = new MsgBox(g.Replica.sNoticePlaylist13.Fmt(Environment.NewLine), g.Common.sWarning, MsgBox.MsgBoxButton.OKCancel, DateTime.Now, DateTime.Now.AddMonths(3), dtNearestRound);
			dlgMsg_TimedCopy.Closed += new EventHandler(_dlgMsg_TimedCopy_Closed);
			dlgMsg_TimedCopy.Show();
			dlgMsg_TimedCopy.Tag = aPLIs;
		}
		void _dlgMsg_TimedCopy_Closed(object sender, EventArgs e)
		{
			MsgBox dlgMsg_TimedCopy = (MsgBox)sender;
			List<PlaylistItemSL> aPLIs = (List<PlaylistItemSL>)dlgMsg_TimedCopy.Tag;
			if (dlgMsg_TimedCopy.DialogResult == true && 0 < aPLIs.Count)
			{
				_dlgProgress.Show();
				DateTime dtMaxStartPlanned = DateTime.MinValue;
				long nDuration = 0;
				foreach (PlaylistItemSL cPLISL in aPLIs)
				{
					nDuration += cPLISL.nDuration;
					if (dtMaxStartPlanned < cPLISL.dtStartPlanned)
						dtMaxStartPlanned = cPLISL.dtStartPlanned;
				}
				_cDBI.BeforeAddCheckRangeAsync(dlgMsg_TimedCopy.dtSelectedDateTime, dlgMsg_TimedCopy.dtSelectedDateTime.AddSeconds(nDuration / 25), new TimedCopyInfo() { dtStart = dlgMsg_TimedCopy.dtSelectedDateTime, aPLIsSelected = aPLIs });
			}
			else
			{
				_dlgProgress.Show();
				_dlgProgress.Close();
			}
		}
		void DoTimedCopy(TimedCopyInfo cCopyInfo)
		{
			List<PlaylistItem> aPLIs = new List<PlaylistItem>();
			PlaylistItem cPLI_pre = null, cPLI;
			bool bIsBlock = false;
			TimeSpan tsDelta = TimeSpan.MinValue;
			PlaylistItemSL cPLISL_pre = null;

			foreach (PlaylistItemSL cPLISL in cCopyInfo.aPLIsSelected)
			{
				if (null != cPLISL_pre)
					if (DateTime.MaxValue > PLIHardSoft(cPLISL_pre) && DateTime.MaxValue > PLIHardSoft(cPLISL) && 1 != PLIHardSoft(cPLISL).Subtract(PLIHardSoft(cPLISL_pre)).TotalSeconds)
						bIsBlock = false;
				cPLISL_pre = cPLISL;

				cPLI = PlaylistItemSL.GetBase(cPLISL);
				cPLI.nID = -1;
				cPLI.dtTimingsUpdate = DateTime.MaxValue;
                if (null != cPLI.cAsset)
                    cPLI.cAsset.aClasses = cPLI.aClasses;

                aPLIs.Add(cPLI);

				if (cPLI_pre == null)
				{
					tsDelta = cCopyInfo.dtStart.Subtract(PLIHardSoftPlanned(cPLI));
					cPLI.dtStartPlanned = cCopyInfo.dtStart;
				}
				else
					cPLI.dtStartPlanned = cPLI_pre.dtStartPlanned.AddMilliseconds((cPLI_pre.nFrameStop - cPLI_pre.nFrameStart + 1) * 40);

				if (DateTime.MaxValue > PLIHardSoft(cPLI))
				{
					if (!bIsBlock)
					{
						bIsBlock = true;
						PLIHardSoftSet(cPLI, PLIHardSoft(cPLI).Add(tsDelta));   // иначе будет постепенная эрозия софт-стартов
					}
					else
						PLIHardSoftSet(cPLI, PLIHardSoft(cPLI_pre).AddSeconds(1));
				}
				else
					bIsBlock = false;

				cPLI_pre = cPLI;
			}
			_dlgProgress.Close();
			_ui_lblPLImportText.Visibility = Visibility.Collapsed;
			_ui_pbPLImportProgress.Visibility = Visibility.Visible;

			aPLIs = aPLIs.Where(o => !o.bPlug).ToList();
			_aPLIsJustInserted = PlaylistItemSL.GetArrayOfPlaylistItemSLs(aPLIs.ToArray()).ToList();
			_cDBI.PlaylistItemsAddWorkerAsync(aPLIs.ToArray(), aPLIs.ToArray());
		}
		PlaylistItem[] PreparePLIsFromAssets(DateTime dtStartHard, DateTime dtStartSoft, DateTime dtPlanned, List<AssetSL> aAssetsSL)
		{
			PlaylistItem[] aPLIs = new PlaylistItem[aAssetsSL.Count];
			PlaylistItem cPLI;
			int nI = 0;
			bool bAsPlanned = false;
			if (dtStartHard == DateTime.MaxValue && dtStartSoft == DateTime.MaxValue)
				bAsPlanned = true;
			foreach (AssetSL cAss in aAssetsSL)
			{
				cPLI = new PlaylistItem()
				{
					cAsset = AssetSL.GetAsset(cAss),
                    aClasses = cAss.aClasses,
                    dtStartHard = DateTime.MaxValue,
					dtStartSoft = DateTime.MaxValue,
					dtStartPlanned = DateTime.MaxValue,
					dtStartQueued = DateTime.MaxValue,
					dtStartReal = DateTime.MaxValue,
					dtStopReal = DateTime.MaxValue,
					dtTimingsUpdate = DateTime.MaxValue,
					cStatus = _cPlayListItemCurrent == null ? null : _cPlayListItemCurrent.cStatus,
					nFrameCurrent = -1,
					nFramesQty = cAss.nFramesQty,
					nFrameStart = cAss.nFrameIn,
					nFrameStop = cAss.nFrameOut,
					nID = -1,
					sName = cAss.sName,
					sNote = "",
					cFile = cAss.cFile
				};
				if (nI == 0)
				{
					cPLI.dtStartHard = dtStartHard;
					cPLI.dtStartSoft = dtStartSoft;
					//cPLI.dtStartPlanned = dtPlanned;
					if (bAsPlanned)
						cPLI.dtStartPlanned = PLIHardSoftPlanned(cPLI);
					else
						cPLI.dtStartPlanned = dtPlanned == DateTime.MaxValue ? PLIHardSoft(cPLI) : dtPlanned;
				}
				else
				{
					if (!bAsPlanned)
						cPLI.dtStartSoft = PLIHardSoft(aPLIs[nI - 1]).AddSeconds(1);

					cPLI.dtStartPlanned = aPLIs[nI - 1].dtStartPlanned.AddMilliseconds((aPLIs[nI - 1].nFrameStop - aPLIs[nI - 1].nFrameStart + 1) * 40);
				}
				aPLIs[nI++] = cPLI;
			}
			return aPLIs;
		}
		void DoAddingAssetsAsBlock(DateTime dtStartHard, DateTime dtStartSoft, DateTime dtPlanned, List<AssetSL> aAssetsSL)
		{
			PlaylistItem[] aPLIs = PreparePLIsFromAssets(dtStartHard, dtStartSoft, dtPlanned, aAssetsSL);
			_dlgProgress.Close();
			_aPLIsJustInserted = PlaylistItemSL.GetArrayOfPlaylistItemSLs(aPLIs).ToList();
			AddSequencedToPL(aPLIs);
		}
		#endregion
		DateTime PLIHardSoft(PlaylistItem cPLI)
		{
			return DateTime.MaxValue > cPLI.dtStartHard ? cPLI.dtStartHard : cPLI.dtStartSoft;
		}
		DateTime PLIHardSoftPlanned(PlaylistItem cPLI)
		{
			DateTime dtPLIHardSoft = PLIHardSoft(cPLI);
			return DateTime.MaxValue > dtPLIHardSoft ? dtPLIHardSoft : cPLI.dtStartPlanned;
		}
		void PLIHardSoftSet(PlaylistItem cPLI, DateTime dtValue)
		{
			if (cPLI.dtStartHard < DateTime.MaxValue)
				cPLI.dtStartHard = dtValue;
			if (cPLI.dtStartSoft < DateTime.MaxValue)
				cPLI.dtStartSoft = dtValue;
		}
        #endregion
        #region DBI
        void Init()
        {
            if (_ui_rpPlanned.IsOpen)
                Planned_Click(_ui_btnToday, null);
            if (_ui_rpAir.IsOpen)
                _ui_btnOnAirRefresh_Click(null, null);
            if (_ui_rpArchived.IsOpen)
				Archived_Click(_ui_btnArchivedToday, null);
            if (!_ui_rpPlanned.IsOpen && !_ui_rpAir.IsOpen && !_ui_rpArchived.IsOpen)
                _dlgProgress.Close();
            _cPLTimer = new PlaylistTimer(_cDBI);
            _cPLTimer.Start(_ui_lblCurrentItem, _ui_lblCurrentItemsdtStart, _ui_lblCurrentItemsType, _ui_lblCurrentItemLeft, _ui_lblNextItem, _ui_lblNextItemsdtStert, _ui_lblNextItemsType);  
        }
        void ShowPlanned()
        {
            _ui_dgPlanned.ItemsSource = null;
            _ui_dgPlanned.ItemsSource = _aPlayListItemsPlanned;
            _ui_lblPlannedQty.Content = _aPlayListItemsPlanned.Count.ToString();
            _ui_dgPlanned.RowHeight = _nPlannedRowHeight;
            _ui_dgPlanned.UpdateLayout();
        }
        void _cDBI_PlaylistItemsPlanGetCompleted(object sender, PlaylistItemsPlanGetCompletedEventArgs e)
        {
			long nStart = DateTime.Now.Ticks;
			try
			{
				_ui_lblPlannedQty.Content = "0";
				if (null != e.Result)
				{
					PlaylistItemSL[] aPLI = PlaylistItemSL.GetArrayOfPlaylistItemSLs(e.Result);
					_aPlayListItemsPlanned = new List<PlaylistItemSL>();
					_aPlayListItemsPlanned.AddRange(aPLI);
					PlaylistItemSL cCurrentPLI = null;
					if (null != _cPlayListItemCurrent)
						cCurrentPLI = aPLI.FirstOrDefault(pli => pli.nID == _cPlayListItemCurrent.nID);
					ShowPlanned();
					if (null != cCurrentPLI)
					{
						ScrollTo(cCurrentPLI);
						_ui_dgPlanned.SelectedItem = cCurrentPLI;
					}
				}
			}
			catch { }
            _dlgProgress.Close();
			double nDelta5 = (new System.TimeSpan(DateTime.Now.Ticks - nStart)).TotalSeconds;
        }
        void _cDBI_PlaylistItemsArchGetCompleted(object sender, PlaylistItemsArchGetCompletedEventArgs e)
        {
			try
			{
				_ui_dgArchived.ItemsSource = null;
				_ui_lblArchivedQty.Content = "0";
				if (null != e.Result)
				{
					PlaylistItemSL[] aPLI = PlaylistItemSL.GetArrayOfPlaylistItemSLs(e.Result);
					_ui_dgArchived.ItemsSource = aPLI;
					_ui_lblArchivedQty.Content = aPLI.Length.ToString();
					_ui_dgArchived.RowHeight = _nArchievedRowHeight;
				}
			}
			catch { }
            _dlgProgress.Close();
        }
        void _cDBI_PlaylistItemsGetCompleted(object sender, PlaylistItemsGetCompletedEventArgs e)
        {
			try
			{
				_ui_dgOnAir.ItemsSource = null;
				_ui_lblOnAirQty.Content = "0";
				if (null != e.Result)
				{
					PlaylistItemSL[] aPLI = PlaylistItemSL.GetArrayOfPlaylistItemSLs(e.Result);
					_ui_dgOnAir.ItemsSource = aPLI;
					_ui_lblOnAirQty.Content = aPLI.Length.ToString();
					_ui_dgOnAir.RowHeight = _nOnAirRowHeight;
				}
			}
			catch { }
            _dlgProgress.Close();
        }
        void _cDBI_PlaylistRecalculateQueryCompleted(object sender, PlaylistRecalculateQueryCompletedEventArgs e)
        {
			try
			{
				if (0 <= e.Result)
					_cDBI.CommandStatusGetAsync(e.Result, e.Result);
				else
				{
					_dlgMsg.ShowError(g.Common.sErrorDataSave);
					ResetButtonPLRecalc();
				}
			}
			catch { }
        }
        void _cDBI_CommandStatusGetCompleted(object sender, CommandStatusGetCompletedEventArgs e)
        {
			try
			{
				string sRez;
				if (null == e.Result)
					sRez = "failed";
				else
					sRez = e.Result.sName;
				switch (sRez)
				{
					case "waiting":
					case "proccessing":
						if (null == _cTimerForCommandResult)
						{
							_dtCommandTimeout = DateTime.Now.AddSeconds(Preferences.cServer.nPLRecalculateTimeout);
							_cTimerForCommandResult = new System.Windows.Threading.DispatcherTimer();
							_cTimerForCommandResult.Tick +=
									delegate(object s, EventArgs args)
									{
										_cTimerForCommandResult.Stop();
										_cDBI.CommandStatusGetAsync((long)e.UserState, e.UserState);
									};
							_cTimerForCommandResult.Interval = new System.TimeSpan(0, 0, 0, 0, 1000);
						}
						if (DateTime.Now < _dtCommandTimeout)
						{
							_cTimerForCommandResult.Start();
							return;
						}
						else
						{
							_dlgMsg.ShowError(g.Replica.sErrorPlaylist4 + "! " + g.Common.sErrorTimeout.Fmt(Preferences.cServer.nPLRecalculateTimeout));//ch=child 
							break;
						}
					case "failed":                     // месага об ошибке + вернуть старый файл
						_dlgMsg.ShowError(g.Replica.sErrorPlaylist4 + "!");   //ch=child 
						break;
					case "succeed":
						//_ui_btnOnAirRefresh_Click(null, null);
						if (_ui_rpPlanned.IsOpen)
							_ui_cmPlanned_Refresh(null, null);
						break;
				}
				ResetButtonPLRecalc();
				_cTimerForCommandResult = null;
			}
			catch { }
        }
        void ResetButtonPLRecalc()
        {
            _ui_lblPLRecalcPartText.Visibility = Visibility.Visible;
            _ui_pbPLRecalcPartProgress.Visibility = Visibility.Collapsed;
        }

		void ResetButtonPLImport()
		{
			_ui_lblPLImportText.Visibility = Visibility.Visible;
			_ui_pbPLImportProgress.Visibility = Visibility.Collapsed;
		}
		void _cDBI_PlaylistItemAdd_ResultGetCompleted(object sender, PlaylistItemAdd_ResultGetCompletedEventArgs e)
		{
			string sError="";
			try
			{
				string sRez;
				if (null == e.Result)
					sRez = "failed";
				else
					sRez = e.Result;
				if (sRez.StartsWith("failed") && sRez.Length > 6)
				{
					sError = sRez.Substring(6);
					sRez = "failed";
				}
				switch (sRez)
				{
					case "waiting":
					case "proccessing":
						if (null == _cTimerForPLAddResult)
						{
							_dtPLImportTimeout = DateTime.Now.AddSeconds(Preferences.cServer.nPLImportTimeout);
							_cTimerForPLAddResult = new System.Windows.Threading.DispatcherTimer();
							_cTimerForPLAddResult.Tick +=
									delegate(object s, EventArgs args)
									{
										_cTimerForPLAddResult.Stop();
										_cDBI.PlaylistItemAdd_ResultGetAsync(e.UserState);
									};
							_cTimerForPLAddResult.Interval = new System.TimeSpan(0, 0, 0, 0, 500);
						}
						if (DateTime.Now < _dtPLImportTimeout)
						{
							_cTimerForPLAddResult.Start();
							return;
						}
						else
						{
                            _dlgMsg.Show(g.Common.sErrorAdd + "! " + g.Common.sErrorTimeout.Fmt(Preferences.cServer.nPLImportTimeout));   //ch=child 
							break;
						}
					case "failed":                     // месага об ошибке + вернуть старый файл
						if (sError.Length > 0)
                            _dlgMsg.ShowError(g.Common.sErrorAdd + "!" + Environment.NewLine + sError);
						else
							_dlgMsg.ShowError(g.Common.sErrorAdd + "! " + g.Common.sSeeTheLog);   //ch=child 
						break;
					case "succeed":
						PlaylistShowInserted(_aPLIsJustInserted, null);
						break;
				}
				ResetButtonPLImport();
				_cTimerForPLAddResult = null;
				_aPLIsJustInserted = null;
			}
			catch { }
		}
		void _cDBI_PlaylistLastElementGetCompleted(object sender, PlaylistLastElementGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				List<Asset> aAssets = new List<Asset>();
				List<AssetSL> aAllAssets = new List<AssetSL>(), aAssetSLs;
				Asset cAsset;
				PlaylistItem cPLI;
				int nCopiesQty = (int)e.UserState;
				foreach (object oPLI in _ui_dgPlanned.SelectedItems)
				{
					cPLI = (PlaylistItemSL)oPLI;
					cAsset = (cPLI).cAsset;
					if (null != cAsset)
						aAssets.Add(cAsset);
				}
				aAssetSLs = AssetSL.GetArrayOfAssetSLs(aAssets.ToArray()).ToList();
				for (int ni = 0; ni < nCopiesQty; ni++)
					aAllAssets.AddRange(aAssetSLs);
				_cPlayListItemCurrent = _aPlayListItemsPlanned[_aPlayListItemsPlanned.Count - 1];
				if (_cPlayListItemCurrent.nID != e.Result.nID)
					_cPlayListItemCurrent = null;
				_cDBI.PlaylistInsertCopiesAsync(aAssets.ToArray(), e.Result, nCopiesQty, aAllAssets);
			}
			else
				_dlgProgress.Close();
		}
		void _cDBI_PlaylistInsertCopiesCompleted(object sender, PlaylistInsertCopiesCompletedEventArgs e)
		{
			PlaylistInsertCompleted(e.Result, e.UserState);
		}
        void _cDBI_PlaylistInsertCompleted(object sender, PlaylistInsertCompletedEventArgs e)
        {
			PlaylistInsertCompleted(e.Result, e.UserState);
        }
		void PlaylistInsertCompleted(PlaylistItem[] aPLIs, object oUserState)
		{
			try
			{
				if (null == aPLIs || null == oUserState)
				{
					_dlgProgress.Close();
					_dlgMsg.ShowError(g.Common.sErrorItemsAdd + "!" + Environment.NewLine + g.Replica.sNoticePlaylist14);
					return;
				}
				List<AssetSL> aAssets = (List<AssetSL>)oUserState;
				List<PlaylistItemSL> aItemsInserted = new List<PlaylistItemSL>();
				List<string> aNotInserted = new List<string>();
				for (int ni = 0; aAssets.Count > ni; ni++)
				{
                    if (-1 < aPLIs[ni].nID)
                        aItemsInserted.Add(new PlaylistItemSL() { dtTimingsUpdate = DateTime.MaxValue, cAsset = AssetSL.GetAsset(aAssets[ni]), aClasses = aPLIs[ni].aClasses, nFrameStop = aPLIs[ni].nFrameStop, nFrameStart = aPLIs[ni].nFrameStart, sName = aPLIs[ni].sName, nID = aPLIs[ni].nID, cFile = aPLIs[ni].cFile, cStatus = aPLIs[ni].cStatus });
                    else
                        aNotInserted.Add(aAssets[ni].sName);
                }
				if (0 < aNotInserted.Count)
				{
					ListBox lb = new ListBox();
					lb.ItemsSource = aNotInserted;
					_dlgMsg.ShowError(g.Common.sErrorItemsAdd + "!" + Environment.NewLine + g.Helper.sNextItemsWereNotAdded, lb);
				}
				PlaylistShowInserted(aItemsInserted, _cPlayListItemCurrent);
			}
			catch { }
			_dlgProgress.Close();
		}
		void PlaylistShowInserted(List<PlaylistItemSL> aItemsInserted, PlaylistItemSL cPLIPrevious)
		{
			if (null == aItemsInserted || 1 > aItemsInserted.Count)
				return;

			if (null == cPLIPrevious)  // если мы добавляли х.з. куда
			{
				foreach (PlaylistItemSL cPLI in _aPlayListItemsPlanned)
					if (cPLI.dtStartPlanned > aItemsInserted[0].dtStartPlanned)
						break;
					else
						cPLIPrevious = cPLI;
				//if (!IsThisDateInRange(aItemsInserted[aItemsInserted.Count - 1].dtStartPlanned) && !IsThisDateInRange(aItemsInserted[0].dtStartPlanned))
				//    return;
				List<PlaylistItemSL> aPLIsIns = aItemsInserted.Where(o => IsThisDateInRange(o.dtStartPlanned)).ToList();
				if (aPLIsIns == null || aPLIsIns.Count < 1)
					return;
				else
					aItemsInserted = aPLIsIns;
			}
			if (_aPlayListItemsPlanned.Count == 0)  // если мы инсертим в пустой день
			{
				_aPlayListItemsPlanned.AddRange(aItemsInserted);
				ShowPlanned();
			}

			if (null != cPLIPrevious) // если мы инсертили после него
			{
				int nInsIndex = _aPlayListItemsPlanned.IndexOf(cPLIPrevious);
				DateTime dtHardSoft = PLIHardSoft(_aPlayListItemsPlanned[nInsIndex]);
				if (DateTime.MaxValue > dtHardSoft)
					nInsIndex = LastItemInBlock(_aPlayListItemsPlanned, nInsIndex);

				_aPlayListItemsPlanned.InsertRange(nInsIndex + 1, (IEnumerable<PlaylistItemSL>)aItemsInserted);
				PlaylistItemSL cPLI = aItemsInserted[0];
				ShowPlanned();
				if (null != (_ui_dgPlanned.SelectedItem = cPLI))
					ScrollTo(cPLI);
			}
		}
		private int LastItemInBlock(List<PlaylistItemSL> aPLIs, int nBlockPLIIndex)
		{
			int nPrevInd = nBlockPLIIndex;
			int nNextInd = nPrevInd;
			DateTime dtPrevHardSoft = PLIHardSoft(aPLIs[nPrevInd]);
			DateTime dtNextHardSoft = dtPrevHardSoft;

			while ((nNextInd == nPrevInd || 1 == dtNextHardSoft.Subtract(dtPrevHardSoft).TotalSeconds) && aPLIs.Count - 1 > nNextInd)
			{
				dtPrevHardSoft = dtNextHardSoft;
				nPrevInd = nNextInd;
				while (aPLIs.Count > nNextInd)
				{
					nNextInd++;
					dtNextHardSoft = PLIHardSoft(aPLIs[nNextInd]);
					if (DateTime.MaxValue > dtNextHardSoft)
						break;
				}
			}
			return nPrevInd;
		}
		void _cDBI_BeforeAddCheckRangeCompleted(object sender, BeforeAddCheckRangeCompletedEventArgs e)
		{
			if (e.Result)
			{
				if (null != e.UserState && e.UserState is AssetsChooser)
				{
					AssetsChooser dlgAC = (AssetsChooser)e.UserState;
					DoAddingAssetsAsBlock(dlgAC.dtHard, dlgAC.dtSoft, dlgAC.dtPlanned, dlgAC.aSelectedAssets);  
					return;
				}
				if (null != e.UserState && e.UserState is TimedCopyInfo)
				{
					DoTimedCopy((TimedCopyInfo)e.UserState);
					return;
				}
				if (null != e.UserState && e.UserState is PlaylistItem[])
				{
					_dlgProgress.Close();
					AddSequencedToPL((PlaylistItem[])e.UserState);
					return;
				}
			}
			else
				_dlgMsg.ShowError(g.Replica.sErrorPlaylist6.Fmt(Environment.NewLine));
			_dlgProgress.Close();
		}
		void AddSequencedToPL(PlaylistItem[] aPLIs)
		{
			_ui_lblPLImportText.Visibility = Visibility.Collapsed;
			_ui_pbPLImportProgress.Visibility = Visibility.Visible;
			//_dlgMsg.Show("total plis = " + aPLIs.Length);
			//_cDBI.PlaylistItemsAddTestAsync(_aPLIs);
			_nAttemptsCount = 0;
			_cDBI.PlaylistItemsAddWorkerAsync(aPLIs, aPLIs);
		}
		void _cDBI_PlaylistItemsAddCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null && _nAttemptsCount <= 10)
			{
				_nAttemptsCount++;
				_cDBI.PlaylistItemsAddWorkerAsync((PlaylistItem[])e.UserState, (PlaylistItem[])e.UserState);        // борьба с багом, когда иногда приходиться по несколько раз обращаться к серваку, иначе не видим его. Почему-то только при этом вызове - "PlaylistItemsAddWorkerAsync"!
			}
			else if (e.Error == null)
				_cDBI.PlaylistItemAdd_ResultGetAsync();
			else
				_cDBI.PlaylistItemsAddWorkerAsync(null);   // прекращение дальнейших попыток
		}

		#region контекстное меню
		void _cDBI_PlaylistItemStartsSetCompleted(object sender, PlaylistItemStartsSetCompletedEventArgs e)
        {
			sPROBA_ERR += g.Replica.sNoticePlaylist15 + e.Result + Environment.NewLine;
            if (DateTime.MaxValue == e.Result)
				_dlgMsg.Show(sPROBA_ERR + g.Replica.sErrorPlaylist7);
            else
            {
                _cPlayListItemCurrent.dtStartPlanned = e.Result;
                _aPlayListItemsPlanned.Remove(_cPlayListItemCurrent);
                PlaylistItemSL cLast = _aPlayListItemsPlanned.LastOrDefault(o => o.dtStartPlanned < _cPlayListItemCurrent.dtStartPlanned);
				if (null != cLast)
					_aPlayListItemsPlanned.Insert(_aPlayListItemsPlanned.IndexOf(cLast) + 1, _cPlayListItemCurrent);
                ShowPlanned();
            }
            _dlgProgress.Close();
			sPROBA_ERR = "";
        }
        void _cDBI_PlaylistItemsDeleteCompleted(object sender, PlaylistItemsDeleteCompletedEventArgs e)
        {
            _ui_lblPlannedQty.Content = "0";
            if (null == e.Result || null == e.UserState)
            {
                _dlgMsg.ShowError(g.Common.sErrorDelete);
                _dlgProgress.Close();
                return;
            }
			long nScrollID;
			long nDuration = 0;
            List<PlaylistItemSL> aDeletedPLIs = (List<PlaylistItemSL>)e.UserState;
            if (0 < e.Result.Length)  // не все удалились
            {
                MsgBox dlgRes = new MsgBox();
                ListBox ui_lbErr = new ListBox();
                //ui_lbErr.SetBinding(ListBox.ItemsSourceProperty, new Binding("sName"));
                ui_lbErr.ItemsSource = e.Result.Select(o => o.sName).ToList();
                dlgRes.ShowError(g.Common.sErrorDelete, ui_lbErr);
                nScrollID = e.Result[0].nID; // первый неудаленный
            }
            else
            {
				nScrollID = helper.FindPrevItemID(_ui_dgPlanned.ItemsSource, typeof(PlaylistItemSL), "nID", ((List<PlaylistItemSL>)e.UserState)[0].nID);
			}
			PlaylistItemSL cPLIFirst = _aPlayListItemsPlanned.FirstOrDefault(o => o.nID == aDeletedPLIs[0].nID);
			List<PlaylistItemSL> aPLIsInBlock = new List<PlaylistItemSL>();
            int nBeforeFirst = -1;
			bool bDeletingFromBlock = false;
			bool bDeletingAllFromMiddleOfOneBlock = false;
			if (null != cPLIFirst)   // если все удалились и были из одного блока, то поправим тайминги (в более сложных случаях пусть пересчитывают и обновляют ПЛ и руками слепляют блоки)
			{						//TODO  также сделать вставку!
				nBeforeFirst = _aPlayListItemsPlanned.IndexOf(cPLIFirst) - 1;
				if (0 <= nBeforeFirst && aDeletedPLIs[0].dtStartSoft.Subtract(PLIHardSoft(_aPlayListItemsPlanned[nBeforeFirst])).TotalSeconds == 1)
				{
					bDeletingFromBlock = true;
					bDeletingAllFromMiddleOfOneBlock = true;
					if (nBeforeFirst + aDeletedPLIs.Count <= _aPlayListItemsPlanned.Count)
						for (int nI = nBeforeFirst+1; nI <= nBeforeFirst + aDeletedPLIs.Count; nI++)
						{
							if (_aPlayListItemsPlanned[nI + 1].dtStartSoft.Subtract(_aPlayListItemsPlanned[nI].dtStartSoft).TotalSeconds != 1)
							{
								bDeletingAllFromMiddleOfOneBlock = false;
								break;
							}
						}
					if (bDeletingAllFromMiddleOfOneBlock)
					{
						aPLIsInBlock.Add(_aPlayListItemsPlanned[nBeforeFirst + aDeletedPLIs.Count + 1]);
                        for (int nI = nBeforeFirst + aDeletedPLIs.Count + 2; nI < _aPlayListItemsPlanned.Count; nI++)
						{
							if (_aPlayListItemsPlanned[nI].dtStartSoft.Subtract(_aPlayListItemsPlanned[nI-1].dtStartSoft).TotalSeconds != 1)
							{
								break;
							}
							aPLIsInBlock.Add(_aPlayListItemsPlanned[nI]);
						}
						foreach (PlaylistItemSL cPLI in aDeletedPLIs)
								nDuration += cPLI.nDuration;
						foreach (PlaylistItemSL cPLI in aPLIsInBlock)
						{
							cPLI.dtStartSoft = cPLI.dtStartSoft.AddSeconds(-1 * aDeletedPLIs.Count);
							cPLI.dtStartPlanned = cPLI.dtStartPlanned.AddMilliseconds(-40 * nDuration);
						}
						_cDBI.PlaylistItemsTimingsSetAsync(PlaylistItemSL.GetArrayOfBases(aPLIsInBlock.ToArray()));
					}
				}
			}
			foreach (PlaylistItemSL cPLI in aDeletedPLIs)  // удаляем кроме неудаленных
			{
				if (null != e.Result.FirstOrDefault(o => o.nID == cPLI.nID))
					continue;
				try
				{
					_aPlayListItemsPlanned.Remove(_aPlayListItemsPlanned.FirstOrDefault(o => o.nID == cPLI.nID));
				}
				catch { }
			}

			if (0 > nScrollID && 0 < _aPlayListItemsPlanned.Count)
				nScrollID = _aPlayListItemsPlanned[0].nID;
            ShowPlanned();
            ScrollTo(nScrollID);
            _dlgProgress.Close();
        }
        void _cDBI_PlaylistItemsDeleteSinceCompleted(object sender, PlaylistItemsDeleteSinceCompletedEventArgs e)
        {
            if (e.Result == -1)
            {
				_dlgMsg.ShowError(g.Replica.sErrorPlaylist8);
                _dlgProgress.Close();
            }
            else
            {
                if (e.Result > 0)
                    _dlgMsg.ShowError(g.Replica.sErrorPlaylist9.Fmt(e.Result));
				PlannedShow(_dtActivePlannedDate);
            }
        }
		void _cDBI_GroupMovingCompleted(object sender, GroupMovingCompletedEventArgs e)
		{
			if (null != e.Result)
				_dlgMsg.ShowError(e.Result);
			else
				_dlgMsg.Show(g.Replica.sNoticePlaylist16.Fmt(Environment.NewLine), g.Common.sInformation, MsgBox.MsgBoxButton.OK);
			_dlgProgress.Close();
		}
		private void _cDBI_InsertInBlockCompleted(object sender, InsertInBlockCompletedEventArgs e)
		{
			if (null != e.Result)
				_dlgMsg.ShowError(e.Result);
			long[] aParams = (long[])e.UserState;
			_cDBI.PlaylistRecalculateQueryAsync(aParams[0], (ushort)aParams[1]);
			_dlgProgress.Close();
		}

		void ScrollTo(long nID)
		{
			PlaylistItemSL cPLI = _aPlayListItemsPlanned.FirstOrDefault(ass => ass.nID == nID);
			if (null != cPLI)
				ScrollTo(cPLI);
		}
		void ScrollTo(PlaylistItemSL cPLI)
        {
            try
            {
                List<PlaylistItemSL> aAss = (List<PlaylistItemSL>)_ui_dgPlanned.ItemsSource;
                int ni = ((List<PlaylistItemSL>)_ui_dgPlanned.ItemsSource).ToList().IndexOf(cPLI);
                int nj = ni + 5 > aAss.Count - 1 ? aAss.Count - 1 : ni + 5;
				_ui_dgPlanned.SelectedItem = cPLI;
				Dispatcher.BeginInvoke(() => _ui_dgPlanned.ScrollIntoView(aAss[nj], null));
            }
            catch { }
        }
        #endregion
        #endregion
        #endregion
        public string GetCorrectWordForm(string sWord, int nQty)
        {
            switch(sWord)
            {
                case "часов":
                case "дней":
                    nQty = Math.Abs(nQty);
                    string sS = nQty.ToString();
                    if (99 < nQty)
                        sS = sS.Substring(sS.Count() - 2, 2);
                    int n1 = sS.Length > 1 ? int.Parse(sS.Substring(sS.Length - 2, 1)) : 0;
                    int n2 = int.Parse(sS.Substring(sS.Length - 1, 1));
                    if (n1 != 1){
                        if (1 < n2 && 5 > n2)
                            sWord = "часов" == sWord ? "часа" : "дня";
                        else if (1 == n2)
                            sWord = "часов" == sWord ? "час" : "день";
                    }
                    break;
                case "hours":
                case "days":
                    if (1 == Math.Abs(nQty))
                        sWord = "day";
                    break;
            }
            return sWord;
        }
    }
}
