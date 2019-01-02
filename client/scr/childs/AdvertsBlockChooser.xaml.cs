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

using helpers.replica.services.dbinteract;
using DBI = helpers.replica.services.dbinteract;
using IP = scr.services.ingenie.player;
using scr.services.ingenie.player;
using controls.sl;
using controls.childs.sl;
using helpers.extensions;
using scr;

using g = globalization;

namespace scr.childs
{
	public partial class AdvertsBlockChooser : ChildWindow
	{
		public enum BlockType
		{
			Adverts,
			Clips,
            Cached,
			Unknown
		}
		private DBInteract _cDBI;
		private PlayerSoapClient _cPlayer;
		private List<LivePLItem> _aAdvPLSingle = new List<LivePLItem>();
		private List<LivePLItem> _aAdvPLWithBlocks = new List<LivePLItem>();
		public List<LivePLItem> _aAdvSelectedSingle = new List<LivePLItem>();
		public List<LivePLItem> _aAdvSelectedWithBlocks = new List<LivePLItem>();
		private List<LivePLItem> _aAdvPreviousBlocks = new List<LivePLItem>();
		private BlockType _enType = BlockType.Unknown;
		private List<long> _aAdvertsStoppedPLIsIDs;
		public List<IP.IdNamePair> aStorages { get; set; }
		private long[] _aFileIDsInStock;
		public string sLog;
        private Dictionary<long, IP.Advertisement> _ahCachedPLIID_Item = new Dictionary<long, IP.Advertisement>();
        private List<IP.Advertisement> _aCachedClipsOnly = new List<IP.Advertisement>();
        private LivePLItem _cPLISelected;
        public LivePLItem cPLISelected
        {
            get
            {
                return _cPLISelected;
            }
        }
        private int _nRowIndexSelected = -1;
        public BlockType enType
		{
			get { return _enType; }
			set
			{
				_enType = value;
                ProgressOff();
            }
		}
		public AdvertsBlockChooser()
		{
			_aAdvertsStoppedPLIsIDs = new List<long>();
			InitializeComponent();
            Title = g.SCR.sNoticeAdvertsBlockChooser0;

		}
		public AdvertsBlockChooser(DBInteract cDBI, PlayerSoapClient cIG)
			: this()
		{
			_cDBI = cDBI;
			_cPlayer = cIG;
		}
        public List<LivePLItem> aAdvPLWithBlocks
        {
            get
            {
                return _aAdvPLWithBlocks;
            }
        }

        protected override void OnOpened()
		{
			base.OnOpened();
            ProgressOn();
            sLog = "";
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser1.Fmt(Environment.NewLine);
			_cPlayer.ClipsSCRGetCompleted += new EventHandler<ClipsSCRGetCompletedEventArgs>(_cPlayer_ClipsSCRGetCompleted);
			_cPlayer.AdvertsSCRGetCompleted += new EventHandler<AdvertsSCRGetCompletedEventArgs>(_cPlayer_AdvertsSCRGetCompleted);
			_cPlayer.AdvertsStoppedGetCompleted += new EventHandler<AdvertsStoppedGetCompletedEventArgs>(_cPlayer_AdvertsStoppedGetCompleted);
			_ui_dgAdvPL.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgAdvPL_LoadingRow);
			_cDBI.ClipsGetCompleted += new EventHandler<ClipsGetCompletedEventArgs>(_cDBI_ClipsGetCompleted);
			_cDBI.PlaylistItemsAdvertsGetCompleted += new EventHandler<PlaylistItemsAdvertsGetCompletedEventArgs>(_cDBI_PlaylistItemsAdvertsGetCompleted);
			_cDBI.LogoBindingGetCompleted += new EventHandler<LogoBindingGetCompletedEventArgs>(_cDBI_LogoBindingGetCompleted);
            _cDBI.FileIDsInStockGetCompleted += _cDBI_FileIDsInStockGetCompleted;
            _cPlayer.ItemsCachedGetCompleted += _cPlayer_ItemsCachedGetCompleted;

            _ui_btnRefresh.Visibility = Visibility.Collapsed;

            if (BlockType.Adverts == enType)
            {
                _ui_lblSelected.Content = g.SCR.sNoticeAdvertsBlockChooser11;
                sLog += _ui_lblStatus.Content = "[cached items getting...]";
                _cPlayer.ItemsCachedGetAsync(true);
            }
            else if (BlockType.Clips == enType)
            {
                _ui_lblSelected.Content = g.SCR.sNoticeAdvertsBlockChooser12;
                sLog += _ui_lblStatus.Content = "[cached items getting...]";
                _cPlayer.ItemsCachedGetAsync(false);
            }
            else if (BlockType.Cached == enType)
            {
                _ui_btnRefresh.Visibility = Visibility.Visible;
                _ui_lblSelected.Content = g.SCR.sNoticeAdvertsBlockChooser12;
                _nRowIndexSelected = -1;
                _cPLISelected = null;
                _ui_dgCached.LoadingRow += _ui_dgCached_LoadingRow;
                _ui_dgCached.SelectionChanged += _ui_dgCached_SelectionChanged;
                sLog += _ui_lblStatus.Content = "[cached items getting...]";
                _cPlayer.ItemsCachedGetAsync(false);
            }
            else
                throw new Exception("Unknown type of AdvertsBlockChooser [" + enType + "]");
        } 
        protected override void OnClosed(EventArgs e)
        {
            _cDBI.ClipsGetCompleted -= _cDBI_ClipsGetCompleted;
            _cDBI.PlaylistItemsAdvertsGetCompleted -= _cDBI_PlaylistItemsAdvertsGetCompleted;
            _cPlayer.ClipsSCRGetCompleted -= _cPlayer_ClipsSCRGetCompleted;
            _cPlayer.AdvertsStoppedGetCompleted -= _cPlayer_AdvertsStoppedGetCompleted;
            _cPlayer.AdvertsSCRGetCompleted -= _cPlayer_AdvertsSCRGetCompleted;
            _ui_dgAdvPL.LoadingRow -= _ui_dgAdvPL_LoadingRow;
            _cDBI.LogoBindingGetCompleted -= _cDBI_LogoBindingGetCompleted;
            _cDBI.FileIDsInStockGetCompleted -= _cDBI_FileIDsInStockGetCompleted;
            _cPlayer.ItemsCachedGetCompleted -= _cPlayer_ItemsCachedGetCompleted;
            _ui_dgCached.LoadingRow -= _ui_dgCached_LoadingRow;
            _ui_dgCached.SelectionChanged -= _ui_dgCached_SelectionChanged;

            ProgressOn();
            base.OnClosed(e);
        }
        new public void Show()
		{
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            base.Show();
		}
		private void ProgressOn()
		{
			_ui_gAdverts.Visibility = Visibility.Collapsed;
			_ui_gClips.Visibility = Visibility.Collapsed;
            _ui_gCached.Visibility = Visibility.Collapsed;
        }
		private void ProgressOff()
		{
			switch (_enType)
			{
				case BlockType.Adverts:
					_ui_gAdverts.Visibility = Visibility.Visible;
					_ui_gClips.Visibility = Visibility.Collapsed;
                    _ui_gCached.Visibility = Visibility.Collapsed;
                    break;
				case BlockType.Clips:
					_ui_gClips.Visibility = Visibility.Visible;
					_ui_gAdverts.Visibility = Visibility.Collapsed;
                    _ui_gCached.Visibility = Visibility.Collapsed;
                    break;
                case BlockType.Cached:
                    _ui_gClips.Visibility = Visibility.Collapsed;
                    _ui_gAdverts.Visibility = Visibility.Collapsed;
                    _ui_gCached.Visibility = Visibility.Visible;
                    break;
                default:
					break;
			}
		}

		#region . UI .
		private void _ui_btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (null == _ui_lblNameOfSelected.Content || (string)_ui_lblNameOfSelected.Content == g.Common.sNoSelection.ToUpper())
                this.DialogResult = false;
			else
			{
				this.DialogResult = true;
			}
		}
		private void _ui_btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
        private void _ui_btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (BlockType.Cached == enType)
            {
                ProgressOn();
                _ui_lblSelected.Content = g.SCR.sNoticeAdvertsBlockChooser12;
                _nRowIndexSelected = -1;
                _cPLISelected = null;
                _ui_dgCached.ItemsSource = null;

                sLog += _ui_lblStatus.Content = "[cached items getting...]";
                _cPlayer.ItemsCachedGetAsync(false);
            }
        }
        private void _ui_hlbtnDetales_Click(object sender, RoutedEventArgs e)
		{
			if (g.Common.sShowDetails.ToLower() == _ui_hlbtnDetales.Content.ToString())
			{
				_ui_dgAdvPL.ItemsSource = _aAdvPLSingle;
                _ui_hlbtnDetales.Content = g.Common.sHideDetails.ToLower();
			}
			else
			{
				_ui_dgAdvPL.ItemsSource = _aAdvPLWithBlocks;
                _ui_hlbtnDetales.Content = g.Common.sShowDetails.ToLower();
			}
		}
		private void _ui_btnShowBlocks_Click(object sender, RoutedEventArgs e)
		{
			if (null != _ui_tpStartTime.Value)
			{
				DateTime dtNow = _ui_dpDate.SelectedDate.Value;
				DateTime dtTmp = (DateTime)_ui_tpStartTime.Value;
				DateTime dtBegin = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtTmp.Hour, dtTmp.Minute, dtTmp.Second);
				double nH = _ui_nudHoursQty.Value;
				DateTime dtEnd = dtBegin.AddHours(nH);
				_cDBI.PlaylistItemsAdvertsGetAsync(dtBegin, dtEnd, dtBegin);
			}
		}
		void _ui_dgAdvPL_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			LivePLItem cLPLI = (LivePLItem)e.Row.DataContext;
			if (!cLPLI.bFileExist)
				e.Row.Background = Coloring.Notifications.cTextBoxError;
			else if (PLIType.AdvBlockItem == cLPLI.eType)
			{
				if (_aAdvertsStoppedPLIsIDs.Contains(cLPLI._cAdvertSCR.nPlaylistID))
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockItemStoppedBackgr;
				else
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockItemBackgr;
			}
			else if (PLIType.AdvBlock == cLPLI.eType || PLIType.JustString == cLPLI.eType)
			{
				long nPLIID;
				if (PLIType.AdvBlock == cLPLI.eType)
					nPLIID = cLPLI.aItemsInThisBlock[1]._cAdvertSCR.nPlaylistID;
				else
					nPLIID = cLPLI.cBlock.aItemsInThisBlock[1]._cAdvertSCR.nPlaylistID;

				if (_aAdvertsStoppedPLIsIDs.Contains(nPLIID))
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockStoppedBackgr;
				else
					e.Row.Background = Coloring.SCR.cPLRow_AdvBlockBackgr;
			}
            MarkAdvTBsInRow(e.Row, Coloring.DataGridRowColorType.Normal);
        }
		private void _ui_dgAdvPL_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			ClearSelected();
			if (null == _ui_dgAdvPL.SelectedItem)
				return;
			LivePLItem cItem = (LivePLItem)_ui_dgAdvPL.SelectedItem;
			if (PLIType.AdvBlock == cItem.eType)
				AddSelected(cItem);
			else
				AddSelected(cItem.cBlock);
		}
		void AddSelected(LivePLItem cBlock)
		{
			_ui_lblNameOfSelected.Content = cBlock.sName;
			_aAdvSelectedWithBlocks.Add(cBlock);
			_aAdvSelectedSingle.AddRange(cBlock.aItemsInThisBlock);
		}
		void ClearSelected()
		{
			_ui_lblNameOfSelected.Content = g.Common.sNoSelection.ToUpper();
            _aAdvSelectedWithBlocks.Clear();
			_aAdvSelectedSingle.Clear();
		}
		private void _ui_dgClipsSCR_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            _ui_lblStatus.Content = "";
            ClearSelected();
			if (null == _ui_dgClipsSCR.SelectedItem)
				return;
			_ui_lblNameOfSelected.Content = ((IP.Clip)_ui_dgClipsSCR.SelectedItem).sName;
			LivePLItem cTMP = new LivePLItem((IP.Clip)_ui_dgClipsSCR.SelectedItem);
			_aAdvSelectedWithBlocks.Add(cTMP);
			_aAdvSelectedSingle.Add(cTMP);
		}
        private void _ui_dgCached_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.MouseEnter += _ui_dgCached_MouseEnter;
            e.Row.MouseLeave += _ui_dgCached_MouseLeave;
            if (_nRowIndexSelected == e.Row.GetIndex())
                MarkCachedTBsInRow(e.Row, Coloring.DataGridRowColorType.Selected);
            else
                MarkCachedTBsInRow(e.Row, Coloring.DataGridRowColorType.Normal);
        }
        private SolidColorBrush ColorTB0Get(LivePLItem cPLI)
        {
            if (cPLI.eType == PLIType.AdvBlock)
                return Coloring.SCR.Cached.cBlockBackgr;
            else
                return Coloring.SCR.Cached.cClipsBackgr;
        }
        private SolidColorBrush ColorTB3Get(LivePLItem cPLI)
        {
            switch (cPLI.eCacheType)
            {
                case LivePLItem.CacheType.cached:
                    if (cPLI.nCountInPlayListFragment > 0 && cPLI.nCountInPlayListFragment <= 3)
                        return Coloring.SCR.Cached.cCachedBackgrGray;
                    else
                        return Coloring.SCR.Cached.cCachedBackgr;
                case LivePLItem.CacheType.in_progress:
                    return Coloring.SCR.Cached.cCachingBackgr;
                case LivePLItem.CacheType.in_queue:
                    return Coloring.SCR.Cached.cInQueueBackgr;
                case LivePLItem.CacheType.not_cached:
                default:
                    return Coloring.SCR.Cached.cNotCachedBackgr;
            }
        }
        private void MarkAdvTBsInRow(DataGridRow cRow, Coloring.DataGridRowColorType enColorType)
        {
            if (cRow != null)
            {
                LivePLItem cPLI = (LivePLItem)cRow.DataContext;
                TextBox tb3 = ((TextBox)_ui_dgAdvPL.Columns[0].GetCellContent(cRow));
                tb3.Background = Coloring.ModifyBrushByType(ColorTB3Get(cPLI), enColorType);
            }
        }
        private void MarkCachedTBsInRow(DataGridRow cRow, Coloring.DataGridRowColorType enColorType)
        {
            if (cRow != null)
            {
                LivePLItem cPLI = (LivePLItem)cRow.DataContext;
                TextBox tb0 = ((TextBox)_ui_dgCached.Columns[1].GetCellContent(cRow));
                tb0.Background = Coloring.ModifyBrushByType(ColorTB0Get(cPLI), enColorType);
                TextBox tb3 = ((TextBox)_ui_dgCached.Columns[0].GetCellContent(cRow));
                tb3.Background = Coloring.ModifyBrushByType(ColorTB3Get(cPLI), enColorType);
            }
        }
        private void _ui_dgCached_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ui_lblStatus.Content = "";
            ClearSelected();
            if (_ui_dgCached.SelectedItem == null)
                return;
            DataGridRow cRow;
            if (null != _cPLISelected)
            {
                cRow = RowCurrentSelected(_nRowIndexSelected, _cPLISelected);
                MarkCachedTBsInRow(cRow, Coloring.DataGridRowColorType.Normal);
            }

            _cPLISelected = (LivePLItem)_ui_dgCached.SelectedItem;
            _ui_lblNameOfSelected.Content = _cPLISelected.cAdvertSCR == null || _cPLISelected.cAdvertSCR.dtStartSoft < DateTime.MaxValue ? g.Common.sNoSelection.ToUpper() : _cPLISelected.cAdvertSCR.sName;
            _nRowIndexSelected = _ui_dgCached.SelectedIndex;
            cRow = RowCurrentSelected(_nRowIndexSelected, _cPLISelected);

            MarkCachedTBsInRow(cRow, Coloring.DataGridRowColorType.Selected);
        }
        private DataGridRow RowCurrentSelected(int nRowIndex, LivePLItem cRowObject)
        {
            FrameworkElement feTemp = _ui_dgCached.Columns[1].GetCellContent(cRowObject);
            return DataGridRow.GetRowContainingElement(feTemp);
        }
        private void _ui_dgCached_MouseLeave(object sender, MouseEventArgs e)
        {
            DataGridRow cRow = (DataGridRow)sender;  // (sender as DependencyObject).ParentOfType<DataGridRow>();
            //if (cRow != null && _ui_dgCached.SelectedIndex != cRow.GetIndex())
            //    _ui_dgCached.SelectedIndex = cRow.GetIndex();
            if (cRow.GetIndex() != _nRowIndexSelected)
                MarkCachedTBsInRow(cRow, Coloring.DataGridRowColorType.Normal);
        }
        private void _ui_dgCached_MouseEnter(object sender, MouseEventArgs e)
        {
            DataGridRow cRow = (DataGridRow)sender; // (sender as DependencyObject).ParentOfType<DataGridRow>();
            if (cRow.GetIndex() != _nRowIndexSelected)
                MarkCachedTBsInRow(cRow, Coloring.DataGridRowColorType.MouseOver);
        }
        #endregion

        #region . DB interact .
        void _cPlayer_ClipsSCRGetCompleted(object sender, ClipsSCRGetCompletedEventArgs e)
		{
			_ui_dgClipsSCR.ItemsSource = e.Result;
			ProgressOff();
		}
		void _cDBI_PlaylistItemsAdvertsGetCompleted(object sender, PlaylistItemsAdvertsGetCompletedEventArgs e)
		{
            DBI.PlaylistItem[] aPLIs = new DBI.PlaylistItem[0];
            if (e.Result == null)
                sLog += _ui_lblStatus.Content = "[_cDBI_PlaylistItemsAdvertsGetCompleted adverts = NULL]";
            else
            {
                aPLIs = e.Result;
                sLog += _ui_lblStatus.Content = "[adverts got = " + aPLIs.Length + "]";
            }

            if (_enType == BlockType.Cached)
            {
                List<IP.Advertisement> aPL = new List<IP.Advertisement>();
                aPL.AddRange(_aCachedClipsOnly);
                IP.Advertisement cPLI;
                foreach (DBI.PlaylistItem cDBPLI in aPLIs)
                {
                    cPLI = ConvertDBPLIToAdvPLI(cDBPLI);
                    if (_ahCachedPLIID_Item.ContainsKey(cPLI.nPlaylistID))
                    {
                        cPLI.bCached = _ahCachedPLIID_Item[cPLI.nPlaylistID].bCached;
                        cPLI.sCopyPercent = _ahCachedPLIID_Item[cPLI.nPlaylistID].sCopyPercent;
                    }
                    aPL.Add(cPLI);
                }
                FindItemsAndBlocks(aPL.OrderBy(o => o.dtStartPlanned).ToArray());
                int nStart = -1;
                foreach (LivePLItem cLPLI in _aAdvPLWithBlocks)
                {
                    if (nStart < 0 && cLPLI.sStartPlanned != "" && cLPLI.eCacheType == LivePLItem.CacheType.cached)
                        nStart = 0;

                    if (nStart >= 0)
                        cLPLI.nCountInPlayListFragment = ++nStart;
                }
                _ui_dgCached.ItemsSource = _aAdvPLWithBlocks;

                ProgressOff();
                return;
            }

			if (!aPLIs.IsNullOrEmpty())
			{
                sLog += _ui_lblStatus.Content = "[File IDs In Stock geting...]";
                _cDBI.FileIDsInStockGetAsync(aPLIs.Select(o => o.cFile.nID).ToArray(), new object[2] { aPLIs, e.UserState });
            }
			else 
			{
				sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser3.Fmt("NULL !!!!!!!!\n");
				ProgressOff();
			}
		}
        void _cDBI_FileIDsInStockGetCompleted(object sender, FileIDsInStockGetCompletedEventArgs e)
        {
            if (!e.Result.IsNullOrEmpty())
            {
                sLog += "[FileIDsInStockGet=" + e.Result.Length + "] ";
                _aFileIDsInStock = e.Result;
                switch (_enType)
                {
                    case BlockType.Adverts:
                        object[] aUser = (object[])e.UserState;
                        _cDBI.LogoBindingGetAsync((DBI.PlaylistItem[])aUser[0], e.UserState);
                        break;
                    case BlockType.Clips:
                        ClipsGetCompleted((DBI.Clip[])e.UserState);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                sLog += "[FileIDsInStockGet=NULL!!!] ";
                ProgressOff();
            }
        }
        void _cDBI_LogoBindingGetCompleted(object sender, LogoBindingGetCompletedEventArgs e)
        {
            sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser4.Fmt(e.Result.Length);
            DBI.PlaylistItem[] aPLIs = null;
			List<IP.Advertisement> aRetVal = new List<IP.Advertisement>();
			int nI;
			List<bool> aLogoBinds;
			try
			{
				if (null != e.UserState && e.UserState is object[] && null != ((object[])e.UserState)[0] && ((object[])e.UserState)[0] is DBI.PlaylistItem[])
				{
					aPLIs = (DBI.PlaylistItem[])(((object[])e.UserState)[0]);
					nI = 0;
					aLogoBinds = new List<bool>();
					if (null != e.Result && aPLIs.Length == e.Result.Count(o => true))
						aLogoBinds.AddRange(e.Result);
					else
						(new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser1);
					foreach (DBI.PlaylistItem cPLI in aPLIs)
					{
                        IP.Advertisement cAdvertisement = ConvertDBPLIToAdvPLI(cPLI);
                        cAdvertisement.bLogoBinding = aLogoBinds[nI];
                        if (_ahCachedPLIID_Item.ContainsKey(cAdvertisement.nPlaylistID))
                        {
                            cAdvertisement.bCached = _ahCachedPLIID_Item[cAdvertisement.nPlaylistID].bCached;
                            cAdvertisement.sCopyPercent = _ahCachedPLIID_Item[cAdvertisement.nPlaylistID].sCopyPercent;
                        }
                        aRetVal.Add(cAdvertisement);
						nI++;
					}
					_cPlayer.AdvertsSCRGetAsync(aRetVal.ToArray(), ((object[])e.UserState)[1]);
					return;
				}
				else
                    (new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser2);
			}
			catch(Exception ex)
			{
				(new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser3.Fmt(Environment.NewLine) + ex.Message);
			}
			ProgressOff();
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser5.Fmt(Environment.NewLine); 
		}
        void FindItemsAndBlocks(IP.Advertisement[] aPL)
        {
            IP.Advertisement cPLI_prevous = null;
            LivePLItem cLPLItem;
            List<LivePLItem> aLPL_block = null;
            _aAdvPLWithBlocks = new List<LivePLItem>();
            _aAdvPLSingle = new List<LivePLItem>();

            foreach (IP.Advertisement cPLI in aPL)
            {
                sLog += " | a " + cPLI.nPlaylistID;
                if (cPLI.dtStartSoft == DateTime.MaxValue) // нашли секвентиал, т.е. клип
                {
                    sLog += " L";
                    if (null != aLPL_block && 0 < aLPL_block.Count) // текущий блок закончен
                    {
                        sLog += " M";
                        aLPL_block.Add(new LivePLItem(cPLI_prevous));
                        cLPLItem = new LivePLItem(aLPL_block);
                        sLog += " [dur=" + cLPLItem.sDuration + "][count=" + aLPL_block.Count() + "]";
                        _aAdvPLWithBlocks.Add(cLPLItem);
                        _aAdvPLSingle.AddRange(cLPLItem.aItemsInThisBlock);
                        aLPL_block = new List<LivePLItem>();
                    }
                    cLPLItem = new LivePLItem(cPLI);
                    _aAdvPLSingle.Add(cLPLItem);
                    _aAdvPLWithBlocks.Add(cLPLItem);
                    continue;
                }

                if (null != cPLI_prevous) // значит уже не первый раз
                {
                    sLog += " b " + cPLI.dtStartSoft + " " + cPLI_prevous.dtStartSoft + " " + cPLI.dtStartSoft.Subtract(cPLI_prevous.dtStartSoft).TotalSeconds;
                    if (1 == (int)cPLI.dtStartSoft.Subtract(cPLI_prevous.dtStartSoft).TotalSeconds)
                    {
                        sLog += " c";
                        if (null != aLPL_block) // первый потенциально неполный блок уже пропущен
                        {
                            sLog += " i";
                            aLPL_block.Add(new LivePLItem(cPLI_prevous));
                        }
                    }
                    else
                    {
                        sLog += " d";
                        if (null == aLPL_block) //следующий блок будет первым
                        {
                            sLog += " e";
                            aLPL_block = new List<LivePLItem>();
                        }
                        else if (0 < aLPL_block.Count) // текущий блок закончен
                        {
                            sLog += " f";
                            aLPL_block.Add(new LivePLItem(cPLI_prevous));
                            cLPLItem = new LivePLItem(aLPL_block);
                            sLog += " [dur=" + cLPLItem.sDuration + "][count=" + aLPL_block.Count() + "]";
                            _aAdvPLWithBlocks.Add(cLPLItem);
                            _aAdvPLSingle.AddRange(cLPLItem.aItemsInThisBlock);
                            aLPL_block = new List<LivePLItem>();
                        }
                    }
                }
                else   // первый раз
                {
                    sLog += " g";
                    if (DateTime.MaxValue == cPLI.dtStartReal)
                    {
                        sLog += " h";
                        aLPL_block = new List<LivePLItem>();
                    }
                }
                cPLI_prevous = cPLI;
            }
            sLog += g.SCR.sNoticeAdvertsBlockChooser7.Fmt(_aAdvPLWithBlocks.Count, _aAdvPLSingle.Count, _aAdvPreviousBlocks.Count);
        }
        void _cPlayer_AdvertsSCRGetCompleted(object sender, AdvertsSCRGetCompletedEventArgs e)
		{
			if (null == e.Result || null != e.Error)
			{
				(new MsgBox()).ShowError(g.SCR.sErrorAdvertsBlockChooser4.Fmt(Environment.NewLine) + e.Error.Message);
				ProgressOff();
				return;
			}
            sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser6.Fmt(e.Result.Length);
			List<LivePLItem> aLPL_block = null;
			List<LivePLItem> aLPL_single = null;
			_aAdvPreviousBlocks = _aAdvPLWithBlocks;

            FindItemsAndBlocks(e.Result);

            aLPL_block = new List<LivePLItem>();
			aLPL_single = new List<LivePLItem>();
			DateTime dtMin = DateTime.MinValue;
			if (null != e.UserState && e.UserState is DateTime)
				dtMin = (DateTime)e.UserState;

			foreach (LivePLItem cLPLI in _aAdvPreviousBlocks)
			{
				if (1 < cLPLI.aItemsInThisBlock.Count && cLPLI.aItemsInThisBlock[1]._cAdvertSCR.dtStartPlanned > dtMin && (0 == _aAdvPLWithBlocks.Count || cLPLI.aItemsInThisBlock[1]._cAdvertSCR.dtStartSoft.AddMinutes(5) < _aAdvPLWithBlocks[0].aItemsInThisBlock[1]._cAdvertSCR.dtStartSoft))
				{
					aLPL_block.Add(cLPLI);
					aLPL_single.AddRange(cLPLI.aItemsInThisBlock);
				}
			}
			aLPL_block.AddRange(_aAdvPLWithBlocks);
			aLPL_single.AddRange(_aAdvPLSingle);
			_aAdvPLWithBlocks = aLPL_block;
			_aAdvPLSingle = aLPL_single;
			_cPlayer.AdvertsStoppedGetAsync();
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser8.Fmt(e.Result.Length, _aAdvPLWithBlocks.Count, _aAdvPLSingle.Count);
		}
		void _cPlayer_AdvertsStoppedGetCompleted(object sender, AdvertsStoppedGetCompletedEventArgs e)
		{
			sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser9.Fmt(_aAdvPLWithBlocks.Count);
			ProgressOff();
			if (null != e.Result)
				_aAdvertsStoppedPLIsIDs = e.Result.ToList();
			_ui_dgAdvPL.ItemsSource = _aAdvPLWithBlocks;
		}
        void _cDBI_ClipsGetCompleted(object sender, ClipsGetCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                sLog += _ui_lblStatus.Content = "[ClipsGetCompleted=" + e.Result.Length + "]";
                _cDBI.FileIDsInStockGetAsync(e.Result.Select(o=>o.cFile.nID).ToArray(), e.Result);
            }
            else
            {
                sLog += _ui_lblStatus.Content = "[ClipsGetCompleted=NULL]";
                ProgressOff();
            }
        }
        void ClipsGetCompleted(DBI.Clip[] aClips)
        {
            List<IP.Clip> aSCRClips = new List<IP.Clip>();
            if (null != aClips)
                foreach (DBI.Clip cClip in aClips)
                {
                    if (null == cClip || null == cClip.cFile)
                        continue;
                    if (!_aFileIDsInStock.IsNullOrEmpty() && !_aFileIDsInStock.Contains(cClip.cFile.nID))
                        continue;
                    aSCRClips.Add(new IP.Clip()
                    {
                        nFramesQty = cClip.nFramesQty,
                        nID = cClip.nID,
                        sFilename = cClip.cFile.sFilename,
                        sName = cClip.sName,
                        sStorageName = cClip.cFile.cStorage.sName,
                        sArtist = cClip.stCues.sArtist,
                        sSong = cClip.stCues.sSong,
                        sRotation = cClip.cRotation.sName,
                        aClasses = cClip.aClasses.ToArrayIP()
                    });
                }
            _cPlayer.ClipsSCRGetAsync(aSCRClips.ToArray());
        }
        private IP.Advertisement ConvertDBPLIToAdvPLI(DBI.PlaylistItem cDBPLI)
        {
            if (null == cDBPLI)
                return null;

            IP.Advertisement cAdvertisement = new IP.Advertisement()
            {
                nFramesQty = cDBPLI.nFramesQty,
                nAssetID = cDBPLI.cAsset.nID,
                sFilename = cDBPLI.cFile.sFilename,
                nPlaylistID = cDBPLI.nID,
                sName = cDBPLI.sName,
                sStorageName = cDBPLI.cFile.cStorage.sName,
                sStoragePath = 0 < aStorages.Count(o => o.nID == cDBPLI.cFile.cStorage.nID) ? aStorages.FirstOrDefault(o => o.nID == cDBPLI.cFile.cStorage.nID).sName : cDBPLI.cFile.cStorage.sPath,
                sDuration = cDBPLI.nFramesQty.ToFramesString(),
                dtStartSoft = cDBPLI.dtStartHard == DateTime.MaxValue ? cDBPLI.dtStartSoft : cDBPLI.dtStartHard,
                dtStartReal = cDBPLI.dtStartReal,
                dtStartPlanned = cDBPLI.dtStartPlanned,
                sStartPlanned = cDBPLI.dtStartPlanned.ToString("HH:mm:ss"),
                aClasses = cDBPLI.aClasses.ToArrayIP(),
                bLogoBinding = false,
                bFileExist = _aFileIDsInStock.IsNullOrEmpty() ? false : _aFileIDsInStock.Contains(cDBPLI.cFile.nID),
            };
            return cAdvertisement;
        }
        private void _cPlayer_ItemsCachedGetCompleted(object sender, ItemsCachedGetCompletedEventArgs e)
        {
            _ahCachedPLIID_Item.Clear();
            _aCachedClipsOnly.Clear();
            if (e.Result == null)
                sLog += _ui_lblStatus.Content = "[_cPlayer_ItemsCachedGetCompleted cached = NULL]";
            else  // got cached and clips from PLFragment
            {
                sLog += _ui_lblStatus.Content = "[_cPlayer_ItemsCachedGetCompleted cached = " + e.Result.Length + "]";
                foreach (IP.Advertisement cA in e.Result)
                {
                    if (_ahCachedPLIID_Item.ContainsKey(cA.nPlaylistID))
                        continue;
                    if (cA.nPlaylistID > 0 && cA.dtStartSoft < DateTime.MaxValue)
                        _ahCachedPLIID_Item.Add(cA.nPlaylistID, cA);
                    else
                        _aCachedClipsOnly.Add(cA);
                }
            }

            if (BlockType.Adverts == enType)
            {
                DateTime dtBegin = DateTime.Now.Subtract(System.TimeSpan.FromMinutes(40));
                DateTime dtEnd = dtBegin.AddHours(3);
                _ui_dpDate.SelectedDate = _ui_dpDate.DisplayDateStart = dtBegin.Date;
                _ui_dpDate.DisplayDateEnd = dtBegin.AddDays(7).Date;
                _ui_tpStartTime.Value = dtBegin;
                _ui_nudHoursQty.Value = 3;
                sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser2.Fmt(dtBegin, dtEnd);
                _cDBI.PlaylistItemsAdvertsGetAsync(dtBegin, dtEnd, dtBegin);
            }
            else if (BlockType.Clips == enType)
            {
                sLog += _ui_lblStatus.Content = "[clips getting...]";
                _cDBI.ClipsGetAsync();
            }
            else if (BlockType.Cached == enType)
            {
                DateTime dtBegin = DateTime.Now.AddMinutes(-App.cPreferences.nFragmentBeforeNow);
                DateTime dtEnd = DateTime.Now.AddMinutes(App.cPreferences.nFragmentAfterNow);
                sLog += _ui_lblStatus.Content = g.SCR.sNoticeAdvertsBlockChooser2.Fmt(dtBegin, dtEnd);
                _cDBI.PlaylistItemsAdvertsGetAsync(dtBegin, dtEnd);
            }
            else
                ProgressOff();
        }


        #endregion

    }
}

