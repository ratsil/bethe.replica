using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using swc=System.Windows.Controls;

using helpers.sl;
using helpers.extensions;
using replica.sl;
using helpers.replica.services.dbinteract;
using controls.childs.sl;
using controls.extensions.sl;
using controls.sl;
using helpers.replica.sl;
using dbi = global::helpers.replica.services.dbinteract;
using g = globalization;

namespace controls.replica.sl
{
	public partial class AssetsList : UserControl
	{
        public enum Tab
        {
            Clips,
            Advertisements,
            Programs,
            Designs,
			All
        }
		public class TabInnerHierarchy // пока юзается только в программах. 
		{
			public List<string> aErrorChilds;
			private AssetSL[] _aAssetsOriginal;  // исходный набор ассетов из БД
			public AssetSL[] aAssetsOriginal
			{
				get
				{
					return _aAssetsOriginal;
				}
				set
				{
					Dictionary<long, AssetSL> aID_Parent = new Dictionary<long, AssetSL>();
                    if (value != null)
                    {
					foreach (AssetSL cAss in value)
						if (null != cAss && null != cAss.cType && cAss.cType.eType != AssetType.part)
						{
							cAss.nChildsQty = 0;
							aID_Parent.Add(cAss.nID, cAss);
						}
					foreach (AssetSL cAss in value)
					{
                        if (0 < cAss.nIDParent)
                            if (aID_Parent.ContainsKey(cAss.nIDParent))
                            {
                                aID_Parent[cAss.nIDParent].nChildsQty++;
                                if (cAss.cFile == null || cAss.cFile.eError != Error.no)
                                    aID_Parent[cAss.nIDParent].bChildError = true;
                            }
                            else
                            {
                                cAss.nIDParent = -2;  // without parrent, but needs parrent
                                aErrorChilds.Add(cAss.sName);
                            }
					}
                    }
					_aAssetsOriginal = value;
					MsgBox _dlgErr;
					if (0 < aErrorChilds.Count)
					{
						_dlgErr = new MsgBox();
                        _dlgErr.ShowError(g.Replica.sErrorAssetsList1, new ListBox() { ItemsSource = aErrorChilds });
					}
				}
			}
			public byte nLevel // чтобы понимать какой сейчас вариант отображения. 0-обычный, 1-более детализир, 2-еще более и т.д.
			{
				get
				{
					return _nLevel;
				}
			}
			private byte _nLevel;
			public AssetSL cParentAsset
			{
				get
				{
					return aParents[nLevel];
				}
			}
			private List<AssetSL> aParents;
			public TabInnerHierarchy()
			{
				_aAssetsOriginal = new AssetSL[0];
				aErrorChilds = new List<string>();
				aParents = new List<AssetSL>();
				aParents.Add(null); // при нулевом левле нет родителя
				_nLevel = 0;
			}
			public void LevelUp()
			{
				if (0 < _nLevel)
				{
					aParents.RemoveAt(_nLevel);
					_nLevel--;
				}
			}
			public void LevelDown(AssetSL cParentAss)
			{
				aParents.Add(cParentAss);
				_nLevel++;
			}
		}
		static private int _nSortedColumn;
		private Progress _dlgProgress;
        private MsgBox _dlgMsgBox;
        private DBInteract _cDBI;
        private DateTime _dtNextMouseClickForDoubleClick;
		private AssetSL _cAssetForDoubleClick;
        private AssetSL _cAssetCurrent;
		private DataGridsSortState _cCurrent_dgAssetsSortState;
		private long _nIDAssetToScrollTo;
		private bool bFirstTime;
        private Tab _eTabCurrent
        {
            get
            {
				if (null != _ui_tcAssets.SelectedItem)
				{
					switch (((TabItem)_ui_tcAssets.SelectedItem).Name)
					{
						case "_ui_tpAdvertisement":
							return Tab.Advertisements;
						case "_ui_tpPrograms":
							return Tab.Programs;
						case "_ui_tpDesign":
							return Tab.Designs;
						case "_ui_tpClips":
							return Tab.Clips;
						case "_ui_tpAll":
							return Tab.All;
					}
				}
				throw new Exception(g.Replica.sErrorAssetsList2);
            }
        }
        private Tab _eTabDefault;
		private List<Tab> _aTabs;
		private TabInnerHierarchy _cCurrentProgramHierarchy;
        private Dictionary<Tab, AssetSL[]> _ahTab_Assets;
		public delegate void OnDoubleClick(AssetSL cAssetSL);
		public OnDoubleClick dgDoubleClick;
		public delegate void dgSelectionChangedCallback(List<AssetSL> cAssetSelected);
        public dgSelectionChangedCallback dgSelectionChanged;
        public delegate void dgTabChangedCallback(Tab eCurrentTab);
        public dgTabChangedCallback dgOnTabChanged;
        public bool bReadOnly = false;
        public DataGridSelectionMode eDataGridSelectionMode
        {
            get { return _ui_dgAssets.SelectionMode; }
            set { _ui_dgAssets.SelectionMode = value; }
        }
		
		public AssetsList()
        {
            InitializeComponent();

			bFirstTime = true;
		}

		public void Init(Tab eTabDefault)
        {
			if (bFirstTime)
			{
				bFirstTime = false;

				_dlgProgress = new Progress();
				_dlgMsgBox = new MsgBox();
				_cDBI = new DBInteract();
				_cDBI.AssetsGetCompleted += new EventHandler<AssetsGetCompletedEventArgs>(_cDBI_AssetsGetCompleted);
				_cDBI.AssetsRemoveCompleted += new EventHandler<AssetsRemoveCompletedEventArgs>(_cDBI_AssetsRemoveCompleted);
				_cDBI.AssetsSaveCompleted += new EventHandler<AssetsSaveCompletedEventArgs>(_cDBI_AssetsSaveCompleted);
				_cDBI.ProgramsGetCompleted += new EventHandler<ProgramsGetCompletedEventArgs>(_cDBI_ProgramsGetCompleted);
				_cDBI.AssetsParentAssignCompleted += new EventHandler<AssetsParentAssignCompletedEventArgs>(_cDBI_AssetsParentAssignCompleted);

                _ahTab_Assets = new Dictionary<Tab, AssetSL[]>();
				_aTabs = new List<Tab>();
				_aTabs.AddRange(Enum.GetValues(typeof(Tab)));
				//_eTabDefault = Tab.Clips;
				_cCurrentProgramHierarchy = new TabInnerHierarchy();

				App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
				_ui_dgAssets.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_tmpDateTime_KeyDown), true);
				_ui_dgAssets.AddHandler(Button.KeyUpEvent, new KeyEventHandler(_ui_tmpDateTime_KeyUp), true);
				_ui_dgAssets.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgAssets_MouseLeftButtonDown), true);
				_ui_dgAssets.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgAssets_LoadingRow);
				_ui_Search.ItemAdd = null;
				_ui_Search.sCaption = g.Helper.sAssetSearch.ToLower() + ":";
				_ui_Search.sDisplayMemberPath = "sName";
				_ui_Search.DataContext = _ui_dgAssets;
                //_ui_Search.nMaxItemsInListOrTable = 200;

                _cAssetCurrent = null;
				_nIDAssetToScrollTo = -1;
				_nSortedColumn = 0;
				_cCurrent_dgAssetsSortState = new DataGridsSortState() { sHeaderName = g.Common.sName, bBackward = false, sBindingPath = "sName" }; // сортировка по умолчанию.

				_ui_tpClips.Tag = "clip";
				_ui_tpAdvertisement.Tag = "advertisement";
                // _ui_tpPrograms.Tag = (Action)_cDBI.ProgramsGetAsync;
                _ui_tpPrograms.Tag = "program";
				_ui_tpDesign.Tag = "design";

				_ui_btnBack.Visibility = System.Windows.Visibility.Collapsed;
			}



			_eTabDefault = eTabDefault;	
            //_dlgProgress.Show();
			TabDefaultSelect();
        }
		public void TabAdd(Tab eTab)
        {
			_aTabs.Add(eTab);
			TabsRedraw();
		}
		public void TabRemove(Tab eTab)
        {
			_aTabs.Remove(eTab);
			TabsRedraw();
		}
		public void TabsClear()
        {
			_aTabs.Clear();
			TabsRedraw();
		}

		private TabItem TabControlGet(Tab eTab)
        {
			switch (eTab)
			{
				case Tab.Advertisements:
					return _ui_tpAdvertisement;
				case Tab.Programs:
					return _ui_tpPrograms;
				case Tab.Designs:
					return _ui_tpDesign;
				case Tab.Clips:
					return _ui_tpClips;
				case Tab.All:
					return _ui_tpAll;
			}
			return null;
		}
		private void TabsRedraw()
        {
			foreach (Tab eTab in Enum.GetValues(typeof(Tab)))
			{
				if (_aTabs.Contains(eTab))
					TabControlGet(eTab).Visibility = System.Windows.Visibility.Visible;
				else
					TabControlGet(eTab).Visibility = System.Windows.Visibility.Collapsed;
			}
		}
        private void ColumnsPrepare(Tab eTab)
        {
            string sColumn = g.Helper.sArtist + " : " + g.Helper.sClip;
            switch (eTab)
            {
                case Tab.Clips:
					if (!_ui_dgAssets.ColumnExist("sCuesName"))
					{
                        _ui_dgAssets.ColumnAdd(sColumn, "sCuesName", 1);
						_ui_dgAssets.ColumnResize("sCuesName", new DataGridLength(1, DataGridLengthUnitType.Star));
					}
                    _ui_dgAssets.ColumnRemove("sName");   //g.Common.sName
                    sColumn = g.Helper.sRotation;
					if (!_ui_dgAssets.ColumnExist("sRotationName"))
					{
                        _ui_dgAssets.ColumnAdd(sColumn, "sRotationName", _ui_dgAssets.Columns.Count - 1);
						_ui_dgAssets.ColumnResize("sRotationName", new DataGridLength(50, DataGridLengthUnitType.Auto));
					}
                    break;
                case Tab.All:
                case Tab.Advertisements:
                case Tab.Programs:
                case Tab.Designs:
                default:
					_ui_dgAssets.ColumnRemove("sCuesName");
					_ui_dgAssets.ColumnRemove("sRotationName");
                    sColumn = g.Common.sName;
					if (!_ui_dgAssets.ColumnExist("sName"))
					{
						_ui_dgAssets.ColumnAdd(sColumn, "sName", 1);
					}
					_ui_dgAssets.ColumnMove("sName", 1);
					_ui_dgAssets.ColumnResize("sName", new DataGridLength(1, DataGridLengthUnitType.Star));
                    break;
            }
			sColumn = "Type";  //g.Common.sType;
            _ui_dgAssets.ColumnRemove(sColumn);
            if (eTab == Tab.All)
            {
                _ui_dgAssets.ColumnAdd(sColumn, "sVideoTypeName", 4);
                _ui_dgAssets.ColumnResize(sColumn, new DataGridLength(50, DataGridLengthUnitType.Auto));
            }
        }
		private void TabDefaultSelect()
		{
            switch (_eTabDefault)
            {
                case Tab.Advertisements:
                    _ui_tcAssets.SelectedItem = _ui_tpAdvertisement;
                    break;
                case Tab.Programs:
                    _ui_tcAssets.SelectedItem = _ui_tpPrograms;
                    break;
                case Tab.Designs:
                    _ui_tcAssets.SelectedItem = _ui_tpDesign;
                    break;
                case Tab.Clips:
                    _ui_tcAssets.SelectedItem = _ui_tpClips;
                    break;
                case Tab.All:
                    _ui_tcAssets_SelectionChanged(null, null);
                    break;
            }
		}
        private void ScrollTo(long nID)
        {
            AssetSL cAss = ((AssetSL[])_ui_dgAssets.Tag).FirstOrDefault(ass => ass.nID == nID);
            if (null != cAss)
                ScrollTo(cAss);
        }
        private void ScrollTo(AssetSL cAss)
        {
            try
            {
				List<object> aAss = ((IEnumerable<object>)_ui_dgAssets.ItemsSource).ToList();
				if (0 < aAss.Count)
				{
					int ni = aAss.IndexOf(cAss);
					//AssetSL[] aAss = (AssetSL[])_ui_dgAssets.Tag;
					//int ni = aAss.ToList().IndexOf(cAss);
					int nj = ni + 5 > aAss.Count - 1 ? aAss.Count - 1 : ni + 5;
					AssetSL cAssToScroll = (AssetSL)aAss[nj];
					_ui_dgAssets.SelectedItem = cAss;
					Dispatcher.BeginInvoke(() => _ui_dgAssets.ScrollIntoView(cAssToScroll, null));
				}
            }
            catch { }
        }
		private long FindNextItemID(AssetSL cItem, System.Collections.IEnumerable aAss)
        {
            AssetSL cScroll = FindNextItem(cItem, aAss);
            if (null != cScroll)
                return cScroll.nID;
            else
                return -1;
        }
        private AssetSL FindNextItem(AssetSL cItem, System.Collections.IEnumerable aAss)
        {
            AssetSL cPrevAss = null, cPrePreAss = null;
            foreach (AssetSL cAss in aAss)
            {
                if (null != cPrevAss && cPrevAss.nID == cItem.nID)
                    return cAss;
                if (null != cPrevAss && null != cPrePreAss)
                    cPrePreAss = cPrevAss;
                cPrevAss = cAss;
            }
            return cPrePreAss;
        }
        private void TableRedraw()
        {
            _ui_dgAssets.ItemsSource = new AssetSL[0];
			//if (_ui_Search.sText.IsNullOrEmpty())
			//    _ui_dgAssets.ItemsSource = (AssetSL[])_ui_dgAssets.Tag;
			//else
			//    _ui_Search.Search();

			_ui_dgAssets.ItemsSource = (AssetSL[])_ui_dgAssets.Tag;
			_ui_Search.DataContextUpdateInitial();


            _ui_dgAssets.UpdateLayout();
			_ui_dgAssets.Sort(typeof(AssetSL), _cCurrent_dgAssetsSortState.sBindingPath, _cCurrent_dgAssetsSortState.bBackward);
        }
		private string VideoTypeNameGet(Tab eTab)
		{
			switch (eTab)
			{
				case Tab.Clips:
					return "clip";
				case Tab.Advertisements:
					return "advertisement";
				case Tab.Designs:
					return "design";
				case Tab.Programs:
					return "program";
			}
			return null;
		}

		#region event handlers
		#region UI
        private void BrowserWindow_Resized(object sender, EventArgs e)
        {
            _ui_tcAssets.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
        }
		private void _ui_tcAssets_Loaded(object sender, RoutedEventArgs e)
		{
            if (Visibility.Collapsed == this.Visibility)
                return;
            _ui_tcAssets.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
		}
		private void _ui_tcAssets_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

            if (null != _ui_dgAssets)
            {
                //_dlgProgress.Show();

				if (_eTabCurrent == Tab.Programs && 1 == _cCurrentProgramHierarchy.nLevel)
					_cCurrent_dgAssetsSortState.bBackward = true;
				else
					_cCurrent_dgAssetsSortState.bBackward = false;

				if (null != e && 0 < e.RemovedItems.Count)
                    ((TabItem)e.RemovedItems[0]).Content = null;
                TabItem ui_ti = (TabItem)_ui_tcAssets.SelectedItem;
                ui_ti.Content = _ui_grdContainer;
                if (null != ui_ti.Tag)
                {
					if (!bReadOnly)
						_ui_btnAdd.Visibility = System.Windows.Visibility.Visible;
					if (ui_ti.Tag is Action)
						((Action)ui_ti.Tag)();
					else
                    {
                        if (sender != null && _ahTab_Assets.ContainsKey(_eTabCurrent)) // !refresh && contains
                            AssetsGetCompletedRefresh(_ahTab_Assets[_eTabCurrent]);
                        else
                        {
                            if (_ui_dgAssets.ItemsSource != null)
                                AssetsGetCompletedRefresh(null);
                            _cDBI.AssetsGetAsync(VideoTypeNameGet(_eTabCurrent), null, 0, _eTabCurrent);
                        }
                    }

                    dgOnTabChanged(_eTabCurrent);
				}
                else
                {
					_ui_btnAdd.Visibility = System.Windows.Visibility.Collapsed;
					_cDBI.AssetsGetAsync(null, null, 0);
                }
            }
		}
		private void _ui_btnAdd_Click(object sender, RoutedEventArgs e)
		{
			string sError = "";
			switch (_eTabCurrent)
			{
				case Tab.Programs:
					if (!access.scopes.programs.bCanCreate)
                        sError = g.Helper.sPrograms.ToLower();
					break;
			}
			if (sError.IsNullOrEmpty())
			{
				controls.childs.replica.sl.AssetProperties ui_dlgAssetProperties;
				ui_dlgAssetProperties = new controls.childs.replica.sl.AssetProperties();
				ui_dlgAssetProperties.sDefaultFileStorageName = helper.TranslateVideoTypeIntoStorageName(VideoTypeNameGet(_eTabCurrent));
				ui_dlgAssetProperties.Closed += new EventHandler(ui_dlgAssetProperties_Closed);
				ui_dlgAssetProperties._aAssets = (AssetSL[])_ui_dgAssets.Tag;
				if (_eTabCurrent == Tab.Programs)
				{
					switch (_cCurrentProgramHierarchy.nLevel)
					{
						case 0:
							ui_dlgAssetProperties.cAssetType = new dbi.Type() { nID = -1, eType = AssetType.series };
							break;
						case 1:
							ui_dlgAssetProperties.cAssetType = new dbi.Type() { nID = -1, eType = AssetType.episode };
							break;
						default:
							ui_dlgAssetProperties.cAssetType = new dbi.Type() { nID = -1, eType = AssetType.part };
							break;
					}
					ui_dlgAssetProperties.cParent = _cCurrentProgramHierarchy.cParentAsset;
				}
				ui_dlgAssetProperties.Show(VideoTypeNameGet(_eTabCurrent));
			}
			else
			{
				_dlgProgress.Show();
				_dlgProgress.Close();
				MessageBox.Show(g.Common.sErrorPermissions.Fmt(g.Common.sToAdd.ToLower(), sError));
			}
		}
		private void ui_dlgAssetProperties_Closed(object sender, EventArgs e)
		{
			controls.childs.replica.sl.AssetProperties ui_dlgAssetProperties=(controls.childs.replica.sl.AssetProperties)sender;
			ui_dlgAssetProperties.Closed -= ui_dlgAssetProperties_Closed;
            _dlgProgress.Show();
            _dlgProgress.Close();
            if (true == ui_dlgAssetProperties.DialogResult)
			{
				_nIDAssetToScrollTo = ui_dlgAssetProperties.nThisAssetID;
				AddAssetToList(ui_dlgAssetProperties.cAsset);
				_ui_dgAssets.Focus();
                //_ui_tcAssets_SelectionChanged(null, null);
            }
        }
        private void _ui_dgAssets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != dgSelectionChanged && null != _ui_dgAssets.SelectedItems)
            {
                List<AssetSL> aAss=new List<AssetSL>();
                foreach (object cObj in _ui_dgAssets.SelectedItems)
                    aAss.Add((AssetSL)cObj);
                dgSelectionChanged(aAss);
            }
        }
		private void _ui_btnBack_Click(object sender, RoutedEventArgs e)
		{
			_nIDAssetToScrollTo = _cCurrentProgramHierarchy.cParentAsset.nID;
			_cCurrentProgramHierarchy.LevelUp();
			if (_cCurrentProgramHierarchy.nLevel == 0)
				_ui_btnBack.Visibility = System.Windows.Visibility.Collapsed;
			ProgramsShow();
		}
		private void _ui_dgAssets_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			AssetSL cRowItem = (AssetSL)e.Row.DataContext;

            if (_eTabCurrent == Tab.Programs)
            {
                if (null != cRowItem.cType && cRowItem.cType.eType == AssetType.series)
                {
                    e.Row.Background = Coloring.AssetsList.cRow_ProgramSeriesBackgr;
                    if (cRowItem.nChildsQty <= 0)
                        e.Row.Background = Coloring.AssetsList.cRow_ProgramParentEmptyBackgr;
                }
                else if (null != cRowItem.cType && cRowItem.cType.eType == AssetType.episode)
                {
                    e.Row.Background = Coloring.AssetsList.cRow_ProgramEpisodeBackgr;
                    if (cRowItem.nChildsQty <= 0)
                        e.Row.Background = Coloring.AssetsList.cRow_ProgramParentEmptyBackgr;
                    else if (cRowItem.bChildError)
                        e.Row.Background = Coloring.AssetsList.cRow_ItemErrorNoFileBackgr;
                }
                else
                {
                    if (null == cRowItem.cFile)
                        e.Row.Background = Coloring.AssetsList.cRow_ItemErrorBackgr;
                    else if (cRowItem.cFile.eError != Error.no)
                        e.Row.Background = Coloring.AssetsList.cRow_ItemErrorNoFileBackgr;
                    else
                        e.Row.Background = Coloring.AssetsList.cRow_ProgramPartBackgr;
                }

				if (-2 == cRowItem.nIDParent)
					e.Row.Background = Coloring.AssetsList.cRow_ItemErrorBackgr;
			}
			else
			{
				if (null == cRowItem.cFile)
					e.Row.Background = Coloring.AssetsList.cRow_ItemErrorBackgr;
				else
					e.Row.Background = null;
			}
		}
		private void _ui_dgAssets_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				FrameworkElement FE = (FrameworkElement)((RoutedEventArgs)(e)).OriginalSource;
				if (FE.Parent is DataGridCell)
				{
					if (_dtNextMouseClickForDoubleClick < DateTime.Now)
					{
						_dtNextMouseClickForDoubleClick = DateTime.Now.AddMilliseconds(400);
						_cAssetForDoubleClick = (AssetSL)FE.DataContext;
					}
					else
					{
						_dtNextMouseClickForDoubleClick = DateTime.MinValue;
						if (_cAssetForDoubleClick == (AssetSL)FE.DataContext)   // значит был даблклик на этом ассете
						{
							_cAssetCurrent = _cAssetForDoubleClick;

							if (_eTabCurrent == Tab.Programs && null != _cAssetCurrent.cType && (_cAssetCurrent.cType.eType == AssetType.episode || _cAssetCurrent.cType.eType == AssetType.series))
							{
								_cCurrentProgramHierarchy.LevelDown(_cAssetCurrent);
								_ui_btnBack.Visibility = System.Windows.Visibility.Visible;
								ProgramsShow();
							}
							else
							{
								if (null != dgDoubleClick)
								{
									dgDoubleClick(_cAssetCurrent);
									return;
								}
								_ui_cmAssets_Properties(null, null);
							}
						}
					}
				}
				else if (FE.Parent is swc.Grid)
				{
					swc.Grid ParentGrid = (swc.Grid)FE.Parent;
					string sHeaderName = null;
					while (true)
					{
						bool bGridFound = false;
						foreach (object o in ParentGrid.Children)
						{
							if (o is ContentPresenter)
							{
								sHeaderName = (string)((ContentPresenter)o).Content;
								break;
							}
							else if (o is swc.Grid)
							{
								ParentGrid = (swc.Grid)o;
								bGridFound = true;
								break;
							}
							else if (o is TextBlock)
							{
								sHeaderName = ((TextBlock)o).Text;
								break;
							}
						}
						if (null != sHeaderName || !bGridFound)
							break;
					}
					int nColumnNumber = -1;
					int nI = 0;
					foreach (DataGridColumn DGC in _ui_dgAssets.Columns)
					{
						if ((string)DGC.Header == sHeaderName)
							nColumnNumber = nI;
						nI++;
					}
					if (0 <= nColumnNumber)
					{
						if (_cCurrent_dgAssetsSortState.sHeaderName == sHeaderName)
							_cCurrent_dgAssetsSortState.bBackward = !_cCurrent_dgAssetsSortState.bBackward;
						else
						{
							_cCurrent_dgAssetsSortState.sHeaderName = sHeaderName;
							_cCurrent_dgAssetsSortState.bBackward = true;
							_cCurrent_dgAssetsSortState.sBindingPath = ((swc.DataGridTextColumn)_ui_dgAssets.Columns[nColumnNumber]).Binding.Path.Path;  //controls.sl.
						}
						_ui_dgAssets.Sort(typeof(AssetSL), _cCurrent_dgAssetsSortState.sBindingPath, _cCurrent_dgAssetsSortState.bBackward);
					}
				}
			}
			catch { };
		}
		#region контекстное меню
		private void _ui_dgAssets_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			_cAssetCurrent = (AssetSL)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
            if (null != _cAssetCurrent && 2 > _ui_dgAssets.SelectedItems.Count)
                _ui_dgAssets.SelectedItem = _cAssetCurrent;
		}
		private void _ui_cmAssets_Opened(object sender, RoutedEventArgs e)
        {
            //TODO права - когда перейдем на новый способ аутентификации
            if (!bReadOnly)
                _ui_cmAssetsDelete.IsEnabled = true;
            string sRSRDelete = g.Common.sDelete.ToLower();
            _ui_cmAssetsParentAssign.Header = g.Helper.sSetParent;
            _ui_cmAssetsParentRemove.Header = g.Helper.sUnsetParent;
            _ui_cmAssetsProperties.Header = g.Common.sProperties.ToLower();
			_ui_cmAssetsRecalculate.Header = g.Helper.sRecalculateTimings;
			_ui_cmAssetsDelete.Header = sRSRDelete;
            string sCurrent = null;
            if (null != _cAssetCurrent)
                sCurrent = _cAssetCurrent.sName;
            else if (1 == _ui_dgAssets.SelectedItems.Count)
                sCurrent = ((AssetSL)_ui_dgAssets.SelectedItem).sName;
            if (null != sCurrent)
            {
                _ui_cmAssetsProperties.Header = g.Common.sProperties.ToLower() + " '" + sCurrent + "'     (CTRL+ENTER)";
                _ui_cmAssetsProperties.IsEnabled = true;
                _ui_cmAssetsRecalculate.Header = g.Helper.sRecalculateTimings + " '" + sCurrent + "'     (CTRL+G)";
                _ui_cmAssetsRecalculate.IsEnabled = true;
                _ui_cmAssetsDelete.Header = sRSRDelete + " '" + sCurrent + "'     (CTRL+DELETE)";
                _ui_cmAssetsDelete.IsEnabled = true;
				_ui_cmAssetsPreview.IsEnabled = true;
				if (_eTabCurrent == Tab.Programs)
				{
                    _ui_cmAssetsParentAssign.Header = g.Helper.sSetParent + " " + g.Helper.sFor + " '" + sCurrent + "'";
					_ui_cmAssetsParentAssign.IsEnabled = true;
                    _ui_cmAssetsParentRemove.Header = g.Helper.sUnsetParent + " " + g.Helper.sFor + " '" + sCurrent + "'";
                    _ui_cmAssetsParentRemove.IsEnabled = true;
                    if (null != ((AssetSL)_ui_dgAssets.SelectedItem).cType && ((AssetSL)_ui_dgAssets.SelectedItem).cType.eType == AssetType.series && access.scopes.programs.bCanUpdate)
                        _ui_cmAssetsAgeSet.IsEnabled = true;
                }
            }
            if (1 < _ui_dgAssets.SelectedItems.Count)
            {
                _ui_cmAssetsProperties.Header = g.Common.sProperties.ToLower() + "     (CTRL+ENTER)";
                if ((_eTabCurrent == Tab.Clips || _eTabCurrent == Tab.Designs || _eTabCurrent == Tab.Advertisements) && access.scopes.clips.bCanUpdate)
                    _ui_cmAssetsProperties.IsEnabled = true;
                else
                    _ui_cmAssetsProperties.IsEnabled = false;
                string sRSRQty = " (" + g.Common.sQty + ":" + _ui_dgAssets.SelectedItems.Count + ")";
                _ui_cmAssetsDelete.Header = sRSRDelete + sRSRQty + "     (CTRL+DELETE)";
                _ui_cmAssetsDelete.IsEnabled = true;
				if (_eTabCurrent == Tab.Programs)
				{
                    _ui_cmAssetsParentAssign.Header = g.Helper.sSetParent + sRSRQty;
					_ui_cmAssetsParentAssign.IsEnabled = true;
                    _ui_cmAssetsParentRemove.Header = g.Helper.sUnsetParent + sRSRQty;
					_ui_cmAssetsParentRemove.IsEnabled = true;
				}
            }
            _ui_cmAssetsAgeSet.Foreground = _ui_cmAssetsAgeSet.IsEnabled? Coloring.Notifications.cNormalForeground: Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsDelete.Foreground = _ui_cmAssetsDelete.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsRecalculate.Foreground = _ui_cmAssetsRecalculate.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsProperties.Foreground = _ui_cmAssetsProperties.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsRefresh.Foreground = _ui_cmAssetsRefresh.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsPreview.Foreground = _ui_cmAssetsPreview.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsParentAssign.Foreground = _ui_cmAssetsParentAssign.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsParentRemove.Foreground = _ui_cmAssetsParentRemove.IsEnabled ? Coloring.Notifications.cNormalForeground : Coloring.Notifications.cInactiveForeground;
            _ui_cmAssetsAgeSet.Focus();
            _ui_cmAssetsDelete.Focus();
            _ui_cmAssetsRecalculate.Focus();
            _ui_cmAssetsProperties.Focus();
            _ui_cmAssetsRefresh.Focus();
            _ui_cmAssetsPreview.Focus();
            _ui_cmAssetsParentAssign.Focus();
            _ui_cmAssetsParentRemove.Focus();
            _ui_cmAssets.Focus();
		}
        private void _ui_cmAssets_Closed(object sender, RoutedEventArgs e)
        {
            _ui_cmAssetsAgeSet.IsEnabled = false;
            _ui_cmAssetsProperties.IsEnabled = false;
            _ui_cmAssetsRecalculate.IsEnabled = false;
            _ui_cmAssetsDelete.IsEnabled = false;
			_ui_cmAssetsPreview.IsEnabled =false;
			_ui_cmAssetsParentAssign.IsEnabled = false;
			_ui_cmAssetsParentRemove.IsEnabled = false;
            _ui_dgAssets.Focus();
        }
		private void _ui_cmAssets_Refresh(object sender, RoutedEventArgs e)
		{
			_ui_tcAssets_SelectionChanged(null, null);
		}
		private void _ui_cmAssets_Delete(object sender, RoutedEventArgs e)
		{
            if (1 > _ui_dgAssets.SelectedItems.Count)
			{
                _dlgMsgBox.Show(g.Common.sNoItemsSelected);
				return;
			}
			AssetSL cAssetChild;
			foreach (AssetSL cAss in _ui_dgAssets.SelectedItems)
			{
				if (null != (cAssetChild = _cCurrentProgramHierarchy.aAssetsOriginal.FirstOrDefault(o => o.nIDParent == cAss.nID)))
				{
					MessageBox.Show(g.Replica.sErrorAssetsList8.Fmt(Environment.NewLine, cAss.sName, cAssetChild.sName));
					return;
				}
			}

			ListBox clb = new ListBox();
            clb.ItemsSource = _ui_dgAssets.SelectedItems;
            clb.DisplayMemberPath = "sName";
            MsgBox cMsg = new MsgBox();
            cMsg.Closed += new EventHandler(msgDelete_Closed);
            cMsg.Show(g.Common.sItemsDeleteConfirmation, g.Common.sWarning, MsgBox.MsgBoxButton.OKCancel, clb);
		}
        private void msgDelete_Closed(object sender, EventArgs e)
        {
			//_dlgProgress.Show();
			//_dlgProgress.Close();
            MsgBox cMsg = (MsgBox)sender;
            cMsg.Closed -= msgDelete_Closed;
            if (MsgBox.MsgBoxButton.OK == cMsg.enMsgResult)
            {
				string sError = "";
				switch (_eTabCurrent)
				{
					case Tab.Programs:
						if(!access.scopes.programs.bCanDelete)
                            sError = g.Helper.sPrograms.ToLower();
						break;
				}
				if (sError.IsNullOrEmpty())
				{
					_dlgProgress.Show();
					Asset[] aAsset = new Asset[_ui_dgAssets.SelectedItems.Count];
					//System.Threading.Thread.Sleep(300); // для проявления плашки _dlgProgress
					int ni = 0;
					foreach (AssetSL cAsset in _ui_dgAssets.SelectedItems)
						aAsset[ni++] = AssetSL.GetAsset(cAsset);
					_nIDAssetToScrollTo = FindNextItemID((AssetSL)_ui_dgAssets.SelectedItem, _ui_dgAssets.ItemsSource);
					_cDBI.AssetsRemoveAsync(aAsset, aAsset);
				}
				else
                    MessageBox.Show(g.Common.sErrorPermissions.Fmt(g.Common.sToDelete.ToLower(), sError));
            }
            _ui_dgAssets.Focus();
        }
        private void _ui_cmAssetsAgeSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null == _cAssetCurrent && 1 != _ui_dgAssets.SelectedItems.Count)
                {
                    _dlgMsgBox.Show(g.Common.sNoSelection);
                    _ui_dgAssets.Focus();
                    return;
                }
                AssetSL cA = _cAssetCurrent == null ? (AssetSL)_ui_dgAssets.SelectedItems : _cAssetCurrent;
                controls.childs.replica.sl.FilesAgeSet uiAgeSet = new childs.replica.sl.FilesAgeSet(cA);
                uiAgeSet.Closed += UiAgeSet_Closed;
                uiAgeSet.Show();
            }
            catch (Exception ex)
            {
                _cDBI.ErrorLoggingAsync("AssetsList: " + ex.ToString());
                _dlgMsgBox.ShowError(ex);
            }
        }
        private void UiAgeSet_Closed(object sender, EventArgs e)
        {
            controls.childs.replica.sl.FilesAgeSet uiAgeSet = (controls.childs.replica.sl.FilesAgeSet)sender;
            uiAgeSet.Closed -= ui_dlgAssetProperties_Closed;
            _dlgProgress.Show();
            _dlgProgress.Close();
            _ui_dgAssets.Focus();
        }

        private void _ui_cmAssets_Properties(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null == _cAssetCurrent && 1 > _ui_dgAssets.SelectedItems.Count) // || !_ui_cmAssetsProperties.IsEnabled
                {
                    _dlgMsgBox.Show(g.Common.sNoItemsSelected);
                    _ui_dgAssets.Focus();
                    return;
                }
                if (_ui_dgAssets.SelectedItems.Count > 1)
                {
                    controls.childs.replica.sl.AssetsProperties ui_dlgAssetsProperties;
                    ui_dlgAssetsProperties = new childs.replica.sl.AssetsProperties(_ui_dgAssets.SelectedItems, _eTabCurrent);
                    ui_dlgAssetsProperties.Closed += Ui_dlgAssetsProperties_Closed;
                    ui_dlgAssetsProperties.Show();
                }
                else
                {
                    controls.childs.replica.sl.AssetProperties ui_dlgAssetProperties;
                    ui_dlgAssetProperties = new controls.childs.replica.sl.AssetProperties();
                    ui_dlgAssetProperties._aAssets = (AssetSL[])_ui_dgAssets.Tag;
                    ui_dlgAssetProperties.sDefaultFileStorageName = helper.TranslateVideoTypeIntoStorageName(VideoTypeNameGet(_eTabCurrent));
                    ui_dlgAssetProperties.Closed += new EventHandler(ui_dlgAssetProperties_Closed);
                    ui_dlgAssetProperties.bReadOnly = !access.scopes.programs.bCanUpdate;
                    if (_eTabCurrent == Tab.Programs)
                    {
                        ui_dlgAssetProperties.cParent = _cCurrentProgramHierarchy.cParentAsset;
                    }
                    try
                    {
                        if (null != _cAssetCurrent)
                        {
                            ui_dlgAssetProperties.Show(_cAssetCurrent.nID);
                            if (!_ui_dgAssets.SelectedItems.Contains(_cAssetCurrent))
                                if (DataGridSelectionMode.Extended == _ui_dgAssets.SelectionMode)
                                    _ui_dgAssets.SelectedItems.Add(_cAssetCurrent);
                                else
                                    _ui_dgAssets.SelectedItem = _cAssetCurrent;
                        }
                        else
                            ui_dlgAssetProperties.Show(((AssetSL)_ui_dgAssets.SelectedItem).nID);
                    }
                    catch
                    {
                        _dlgMsgBox.Show(g.Common.sYouHaveToRefresh.ToLower() + " " + g.Helper.sPlaylist.ToLower() + "!");
                    }
                }
            }
            catch (Exception ex)
            {
                //_cDBI.ErrorLoggingAsync("AssetsList: " + ex.ToString());
                _dlgMsgBox.ShowError(ex);
            }
        }

        private void Ui_dlgAssetsProperties_Closed(object sender, EventArgs e)
        {
            controls.childs.replica.sl.AssetsProperties ui_dlgAssetsProperties = (controls.childs.replica.sl.AssetsProperties)sender;
            ui_dlgAssetsProperties.Closed -= Ui_dlgAssetsProperties_Closed;
            _dlgProgress.Show();
            _dlgProgress.Close();
            if (ui_dlgAssetsProperties.DialogResult.Value)
            {
                ui_dlgAssetsProperties.ChangesApply();
                _nIDAssetToScrollTo = ((AssetSL)_ui_dgAssets.SelectedItem).nID;
                AssetsListRefresh((AssetSL[])_ui_dgAssets.Tag);
                _ui_dgAssets.Focus();
            }
        }

        private void _ui_cmAssetsRecalculate_Click(object sender, RoutedEventArgs e)
        {
			MsgBox dlgDurationRecalculate = new MsgBox(MsgBox.Type.FileDuration);
			RecalculateFileDuration _ui_ctrRecalcFileDur = new RecalculateFileDuration();
			_ui_ctrRecalcFileDur.sDefaultFileStorageName = helper.TranslateVideoTypeIntoStorageName(VideoTypeNameGet(_eTabCurrent));
			_ui_ctrRecalcFileDur.ResetToDefault(_cAssetCurrent);
			dlgDurationRecalculate.ControlAdd(_ui_ctrRecalcFileDur, _ui_ctrRecalcFileDur.IsInputCorrect);
            dlgDurationRecalculate.Closed += new EventHandler(dlgDurationRecalculate_Closed);
            dlgDurationRecalculate.Show();
        }
        private void dlgDurationRecalculate_Closed(object sender, EventArgs e)
        {
			bool bAll = false;
			MsgBox dlgDurationRecalculate = (MsgBox)sender;
			dlgDurationRecalculate.Closed -= dlgDurationRecalculate_Closed;
			switch ((dlgDurationRecalculate).enMsgResult)
			{
				case MsgBox.MsgBoxButton.OK:
					break;
                case MsgBox.MsgBoxButton.ALL:
                    bAll = true;
                    break;
                case MsgBox.MsgBoxButton.Cancel:
                    //_dlgProgress.Show();
                    //_dlgProgress.Close();
                    _ui_dgAssets.Focus();
                    return;
                default:
                    return;
            }
            List<AssetSL> aAssetsToChange = new List<AssetSL>();
            if (bAll)
            {
				AssetSL[] aAss = ((AssetSL[])(_ui_dgAssets.Tag)).Where(c => (null == c.cFile && null == _cAssetCurrent.cFile) || (null != c.cFile && null != _cAssetCurrent.cFile && c.cFile.nID == _cAssetCurrent.cFile.nID)).ToArray();
                aAssetsToChange.AddRange(aAss);
            }
            else
                aAssetsToChange.Add(_cAssetCurrent);
			RecalculateFileDuration ui_ctrlRecalculateFileDuration = ((RecalculateFileDuration)((MsgBox)sender).ControlGet());
            foreach (AssetSL cAss in aAssetsToChange)
            {
				cAss.nFrameIn = ui_ctrlRecalculateFileDuration._nIn;
				cAss.nFrameOut = ui_ctrlRecalculateFileDuration._nOut;
				cAss.nFramesQty = ui_ctrlRecalculateFileDuration._nTotal;
				cAss.cFile = ui_ctrlRecalculateFileDuration._cFile;
            }
            _cDBI.AssetsSaveAsync(AssetSL.GetArrayOfBases(aAssetsToChange.ToArray()));
            _dlgProgress.Show();
        }
        private void _ui_tmpDateTime_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Delete && !bReadOnly)
                    _ui_cmAssets_Delete(null, null);
                else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && e.Key == Key.C && null != _ui_dgAssets.SelectedItem)
                {
                    string sClip = "";
                    foreach (AssetSL cAss in _ui_dgAssets.SelectedItems)
                    {
                        if (0 < sClip.Length)
                            sClip += Environment.NewLine;
                        sClip += ((TextBlock)_ui_dgAssets.CurrentColumn.GetCellContent(cAss)).Text;
                    }
                    if (0 < sClip.Length)
                        Clipboard.SetText(sClip);
                }
            }
            if (e.Key == Key.F9)
                _ui_cmAssets_Refresh(null, null);
            _cAssetCurrent = (AssetSL)_ui_dgAssets.SelectedItem;
            if (null != _cAssetCurrent)
            {
                if (e.Key == Key.G && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    _ui_cmAssetsRecalculate_Click(null, null);
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    _ui_cmAssets_Properties(null, null);
            }
        }
        private void _ui_tmpDateTime_KeyUp(object sender, KeyEventArgs e)
        {
            //if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //{
            //    _ui_dgAssets.CancelEdit();
            //}
        }
		private void _ui_cmAssetsPreview_Click(object sender, RoutedEventArgs e)
		{
			childs.replica.sl.MediaPreview cMediaPreview = new childs.replica.sl.MediaPreview();
			cMediaPreview.Closed += new EventHandler(cMediaPreview_Closed);
			Asset cAsset = (Asset)_ui_dgAssets.SelectedItem;
			string sPath = Preferences.cServer.sPreviewsPath;
			switch (cAsset.cFile.cStorage.nID)
			{
				case 1:
					sPath += "clips";
					break;
				case 2:
					sPath += "advertisements";
					break;
				case 3:
					sPath += "design";
					break;
				case 4:
					sPath += "programs";
					break;
			}
			try
			{
				cMediaPreview.Show(sPath + "/" + cAsset.cFile.sFilename);
			}
			catch { }
		}
		private void _ui_cmAssetsParentAssign_Click(object sender, RoutedEventArgs e)
		{
			if (1 > _ui_dgAssets.SelectedItems.Count)
			{
                _dlgMsgBox.Show(g.Common.sNoItemsSelected);
				return;
			}
			MsgBox cMsg = new MsgBox();
			cMsg.Closed += new EventHandler(msgParentAssign_Closed);
            cMsg.Show(g.Helper.sParentAssetID, g.Helper.sSettingAssetsParent, MsgBox.MsgBoxButton.OKCancel, "");
		}
		private void msgParentAssign_Closed(object sender, EventArgs e)
		{
			//_dlgProgress.Show();
			//_dlgProgress.Close();
			MsgBox cMsg = (MsgBox)sender;
			cMsg.Closed -= msgParentAssign_Closed;
			if (null == cMsg.DialogResult || !cMsg.DialogResult.Value)
			{
				_ui_dgAssets.Focus();
				return;
			}
			long nIDParent = cMsg.sText.ToLong();
			if (null != _cCurrentProgramHierarchy.aAssetsOriginal.FirstOrDefault(o => o.nID == nIDParent))
			{
				if (MsgBox.MsgBoxButton.OK == cMsg.enMsgResult)
				{
					string sError = "";
					switch (_eTabCurrent)
					{
						case Tab.Programs:
							if (!access.scopes.programs.bCanUpdate)
                                sError = g.Helper.sPrograms.ToLower();
							break;
					}
					if (sError.IsNullOrEmpty())
                    {
						if (0 < nIDParent)
						{
							_dlgProgress.Show();
							List<Asset> aAsset = new List<Asset>();
							foreach (AssetSL cAsset in _ui_dgAssets.SelectedItems)
								if (null == cAsset.cType || cAsset.cType.eType == AssetType.part)
								{
									cAsset.nIDParent = nIDParent;
									aAsset.Add(AssetSL.GetAsset(cAsset));
								}
							_nIDAssetToScrollTo = FindNextItemID((AssetSL)_ui_dgAssets.SelectedItem, _ui_dgAssets.ItemsSource);
							_cDBI.AssetsParentAssignAsync(aAsset.ToArray());
						}
						else
                            MessageBox.Show(g.Common.sErrorWrongID);
                    }
					else
                        MessageBox.Show(g.Common.sErrorPermissions.Fmt(g.Common.sToModify.ToLower(), sError));
				}
			}
			else
                MessageBox.Show(g.Common.sErrorNoItemWithSuchID);
			_ui_dgAssets.Focus();
		}
		private void _ui_cmAssetsParentRemove_Click(object sender, RoutedEventArgs e)
		{
			MsgBox cMsg = new MsgBox();
			cMsg.Closed += new EventHandler(msgParentRemove_Closed);
            cMsg.Show(g.Replica.sErrorAssetsList9, g.Helper.sRemovingAssetsParent, MsgBox.MsgBoxButton.OKCancel, new ListBox() { ItemsSource = _ui_dgAssets.SelectedItems, DisplayMemberPath = "sName" });
		}
        private void msgParentRemove_Closed(object sender, EventArgs e)
		{
			//_dlgProgress.Show();
			//_dlgProgress.Close();
			MsgBox cMsg = (MsgBox)sender;
			cMsg.Closed -= msgParentRemove_Closed;
			if (MsgBox.MsgBoxButton.OK == cMsg.enMsgResult)
			{
				string sError = "";
				switch (_eTabCurrent)
				{
					case Tab.Programs:
						if (!access.scopes.programs.bCanUpdate)
                            sError = g.Helper.sPrograms.ToLower();
						break;
				}
				if (sError.IsNullOrEmpty())
				{
					_dlgProgress.Show();
					List<Asset> aAsset = new List<Asset>();
					foreach (AssetSL cAsset in _ui_dgAssets.SelectedItems)
						if (null == cAsset.cType || cAsset.cType.eType == AssetType.part)
						{
							cAsset.nIDParent = -1;
							aAsset.Add(AssetSL.GetAsset(cAsset));
						}
					_nIDAssetToScrollTo = FindNextItemID((AssetSL)_ui_dgAssets.SelectedItem, _ui_dgAssets.ItemsSource);
					_cDBI.AssetsParentAssignAsync(aAsset.ToArray());
				}
				else
                    MessageBox.Show(g.Common.sErrorPermissions.Fmt(g.Common.sToModify.ToLower(), sError));
			}
		}
		private void cMediaPreview_Closed(object sender, EventArgs e)
		{
			((childs.replica.sl.MediaPreview)sender).Closed -= cMediaPreview_Closed;
			_dlgProgress.Show();
			_dlgProgress.Close();
		}
		#endregion
		#endregion
		#region DBI
		void AddAssetToList(AssetSL cAsset)
		{
			if (_cCurrentProgramHierarchy.aAssetsOriginal != null && _cCurrentProgramHierarchy.aAssetsOriginal.Length > 0)
			{
				AssetSL cAssetOld = _cCurrentProgramHierarchy.aAssetsOriginal.FirstOrDefault(o => o.sName == cAsset.sName);
				if (null != cAssetOld)
					cAsset.nChildsQty = cAssetOld.nChildsQty;
				_cCurrentProgramHierarchy.aAssetsOriginal = _cCurrentProgramHierarchy.aAssetsOriginal.AddOrReplaceElement(cAsset);
			}
			AssetsListRefresh(((AssetSL[])_ui_dgAssets.Tag).AddOrReplaceElement(cAsset));
		}
		void RemoveAssetsFromList(Asset[] aAssetsToRemove)
		{
			if (_cCurrentProgramHierarchy.aAssetsOriginal != null && _cCurrentProgramHierarchy.aAssetsOriginal.Length > 0)
				_cCurrentProgramHierarchy.aAssetsOriginal = _cCurrentProgramHierarchy.aAssetsOriginal.RemoveElements(aAssetsToRemove);
			AssetsListRefresh(((AssetSL[])_ui_dgAssets.Tag).RemoveElements(aAssetsToRemove));
		}

		void AssetsListRefresh(AssetSL[] aAssets)
		{
			_ui_dgAssets.Tag = aAssets;
			_ui_dgAssets.ItemsSource = aAssets;
            if (aAssets.IsNullOrEmpty())
                return;

			_ui_Search.DataContextUpdateInitial();
			//_ui_Search.Search();
			_ui_dgAssets.UpdateLayout();
			_ui_dgAssets.Sort(typeof(AssetSL), _cCurrent_dgAssetsSortState.sBindingPath, _cCurrent_dgAssetsSortState.bBackward);

			if (0 < _nIDAssetToScrollTo)   // scroll to this asset
			{
				ScrollTo(_nIDAssetToScrollTo);
				_nIDAssetToScrollTo = -1;
			}
		}
		void _cDBI_AssetsGetCompleted(object sender, AssetsGetCompletedEventArgs e)
		{
            try
            {
                if (null == e || null == e.Result)
                    throw new NullReferenceException();

                AssetSL[] aRes = AssetSL.GetArrayOfAssetSLs(e.Result);
                Tab eT = (Tab)e.UserState;
                _ahTab_Assets[eT] = aRes;

                if (eT == _eTabCurrent)
                {
                    AssetsGetCompletedRefresh(aRes);
                }
                //_dlgProgress.Close();
                _ui_dgAssets.Focus();
            }
            catch { }
        }
        void AssetsGetCompletedRefresh(AssetSL[] aAssets)
        {
            if (_eTabCurrent == Tab.Programs)
            {
                _cCurrentProgramHierarchy.aAssetsOriginal = aAssets;
                ProgramsShow();
            }
            else
            {
                ColumnsPrepare(_eTabCurrent);
                if (_eTabCurrent == Tab.Clips)
                {
                    _ui_Search.sDisplayMemberPath = "sCuesName";
                    _ui_Search.aAdditionalSearchFields = new string[3] { "sName", "nID", "sFilename" };
                }
                else
                {
                    _ui_Search.sDisplayMemberPath = "sName";
                    _ui_Search.aAdditionalSearchFields = new string[2] { "nID", "sFilename" };
                }
                AssetsListRefresh(aAssets);
            }
		}
        void _cDBI_AssetsRemoveCompleted(object sender, AssetsRemoveCompletedEventArgs e)
		{
			try
			{
				if (null == e.Result)
                    _dlgMsgBox.Show(g.Replica.sErrorAssetsList3, g.Common.sError, MsgBox.MsgBoxButton.OK);
				else if (0 < e.Result.Length)   // Count  с локала
				{
					MsgBox dlgRes = new MsgBox();
					ListBox ui_lbErr = new ListBox();
					ui_lbErr.ItemsSource = e.Result;
                    dlgRes.Show(g.Replica.sErrorAssetsList4, g.Common.sError, MsgBox.MsgBoxButton.OK, ui_lbErr);
				}
				else
				{
					RemoveAssetsFromList((Asset[])e.UserState);
					//_ui_tcAssets_SelectionChanged(null, null);
				}
			}
			catch { }
			_dlgProgress.Close();
			_ui_dgAssets.Focus();
        }
        void _cDBI_AssetsSaveCompleted(object sender, AssetsSaveCompletedEventArgs e)
		{
			try
			{
				_dlgProgress.Close();
				if (null == e.Result)
                    MessageBox.Show(g.Common.sErrorDataSave, g.Common.sError, MessageBoxButton.OK);
				else if (0 < e.Result.Length)   // Count  с локала
				{
					MsgBox dlgRes = new MsgBox();
					ListBox ui_lbErr = new ListBox();
					ui_lbErr.ItemsSource = e.Result;
                    dlgRes.Show(g.Replica.sErrorAssetsList4, g.Common.sError, MsgBox.MsgBoxButton.OK, ui_lbErr);
				}
				else
				{
					TableRedraw(); //_ui_Search._bTextChanged = true; //
					if (null != _cAssetCurrent)
						ScrollTo(_cAssetCurrent);   //_cAssetCurrent.nID
					_ui_dgAssets.Focus();
				}
			}
			catch { }
        }
		void _cDBI_ProgramsGetCompleted(object sender, ProgramsGetCompletedEventArgs e)
		{
			try
			{
				_cCurrentProgramHierarchy.aAssetsOriginal = AssetSL.GetArrayOfAssetSLs(e.Result);
				ProgramsShow();
			}
			catch { }
			//_dlgProgress.Close();
			_ui_dgAssets.Focus();
		}
		void ProgramsShow()
		{
			if (null != _cCurrentProgramHierarchy.aAssetsOriginal)
			{
				if (_eTabCurrent == Tab.Programs && 1 == _cCurrentProgramHierarchy.nLevel)
					_cCurrent_dgAssetsSortState.bBackward = true;
				else
					_cCurrent_dgAssetsSortState.bBackward = false;

				ColumnsPrepare(Tab.Programs);
				_ui_Search.sDisplayMemberPath = "sName";
				_ui_dgAssets.ItemsSource = ProgramsFilter(_cCurrentProgramHierarchy.aAssetsOriginal);
				_ui_dgAssets.Tag = _ui_dgAssets.ItemsSource;
				_ui_Search.DataContextUpdateInitial();
				//_ui_Search.Search();
				_ui_dgAssets.UpdateLayout();
				_ui_dgAssets.Sort(typeof(AssetSL), _cCurrent_dgAssetsSortState.sBindingPath, _cCurrent_dgAssetsSortState.bBackward);

				if (0 < _nIDAssetToScrollTo)   // scroll to this asset
				{
					ScrollTo(_nIDAssetToScrollTo);
					_nIDAssetToScrollTo = -1;
				}
			}
            else
            {
                _ui_dgAssets.Tag = null;
                _ui_dgAssets.ItemsSource = null;
            }
		}
		AssetSL[] ProgramsFilter(AssetSL[] aAss)
		{
			List<AssetSL> aRes = new List<AssetSL>();
			foreach (AssetSL cAss in aAss)
				switch (_cCurrentProgramHierarchy.nLevel)
				{
					case 0:
						if (cAss.nIDParent < 0)
							aRes.Add(cAss);
						break;
					case 1:
					case 2:
						if (_cCurrentProgramHierarchy.cParentAsset != null && _cCurrentProgramHierarchy.cParentAsset.nID == cAss.nIDParent)
							aRes.Add(cAss);
						break;
				}
			return aRes.ToArray();
		}
		void _cDBI_AssetsParentAssignCompleted(object sender, AssetsParentAssignCompletedEventArgs e)
		{
			if (!e.Result)
				_dlgMsgBox.ShowError(g.Replica.sErrorAssetsList6);
			else
				_ui_cmAssets_Refresh(null, null);
			_dlgProgress.Close();
		}

		#endregion

		private void TabAdd_Click(object sender, RoutedEventArgs e)
		{

		}
		#endregion
	}
}
