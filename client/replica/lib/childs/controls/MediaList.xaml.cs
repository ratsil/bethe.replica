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

using helpers.sl;
using controls.childs.sl;
using replica.sl;
using controls.sl;
using helpers.replica.services.dbinteract;
using replica.sl.ListProviders;
using g = globalization;

namespace controls.replica.sl
{
	public partial class MediaList : UserControl
	{
        public double _nMaxHeight;
        private Progress _dlgProgress;
		private MsgBox _dlgMsg;
        private File _cFileSelected;
        private DBInteract _cDBI;
        private DBInteract cDBI
        {
            get
            {
                if (null == _cDBI)
                {
                    _cDBI = new DBInteract();
                }
                return _cDBI;
            }
        }
		public int nMaxItemsInTableFiles
		{
			get { return _nMaxItemsInTableFiles; }
			set
			{
				_nMaxItemsInTableFiles = 0 == value ? int.MaxValue : value;
				if (null != _ui_Search)
					_ui_Search.nMaxItemsInListOrTable = _nMaxItemsInTableFiles;
			}
		}
		private int _nMaxItemsInTableFiles;
        private bool bTakeAllItems;
        public string sStorageDefaultSelection;
        public string sFilenameToScrollTo;
		public string sParent = "";
        DateTime _dtCurrentSelected;
        File[] _aPrevSelected, _aCurrentSelected;
        private void SetCurrentSelection()
        {
            _dtCurrentSelected = DateTime.Now;
            _aPrevSelected = _aCurrentSelected;
            _aCurrentSelected = new File[_ui_dgFiles.SelectedItems.Count];
            int nIndx = 0;
            foreach (File cFile in _ui_dgFiles.SelectedItems)
                _aCurrentSelected[nIndx++] = cFile;
        }
		File[] _aSelectedFiles;
		File[] _aUnusedFiles;
		public File[] SelectedFiles
		{
			get
			{
				if (null == _aSelectedFiles)
				{
                    if (DateTime.Now < _dtCurrentSelected.AddSeconds(1))
                        _aSelectedFiles = _aPrevSelected;
                    else
                        _aSelectedFiles = _aCurrentSelected;
				}
				return _aSelectedFiles;
			}
		}
        
		public MediaList()
		{
			InitializeComponent();
            _dlgProgress = new Progress();
            //App.Current.Host.Content.Resized += new EventHandler(BrouserWindow_Resized);
            _ui_Search.ItemAdd = null;
            _ui_Search.sCaption = g.Common.sSearch + ":";
            _ui_Search.sDisplayMemberPath = "sFilename";
			_ui_Search.DataContext = _ui_dgFiles;
			_ui_dgFiles_RefreshMaxItems();
            //_ui_dgFiles.MaxHeight = 100;
            _ui_dgFiles.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_dgFiles_KeyDown), true);
			this.SizeChanged += new SizeChangedEventHandler(MediaList_SizeChanged);
			App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
			_ui_dgFiles.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgFiles_LoadingRow);
		}

        public void Init()
        {
            if (null == _cDBI && null != cDBI)
            {
                _cDBI.StoragesGetCompleted += new EventHandler<StoragesGetCompletedEventArgs>(_cDBI_StoragesGetCompleted);
                _cDBI.StoragesGetCompleted += new EventHandler<StoragesGetCompletedEventArgs>(_dlgProgress.AsyncRequestCompleted);
                _cDBI.FilesGetCompleted += new EventHandler<FilesGetCompletedEventArgs>(_cDBI_FilesGetCompleted);
				_cDBI.StorageFilesUnusedGetCompleted += new EventHandler<StorageFilesUnusedGetCompletedEventArgs>(_cDBI_StorageFilesUnusedGetCompleted);


                _dlgProgress.Show();
                System.Threading.Thread.Sleep(300);
				_cDBI.StoragesGetAsync();
				_dlgMsg = new MsgBox();

				if (sParent == "Reduced Panel")
				{
					_ui_dgMedia.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
					//_ui_srlFiles.Height = this.ActualHeight > 35 ? this.ActualHeight - 35 : this.ActualHeight;
					_ui_srlFiles.Height = _ui_dgMedia.MaxHeight > 35 ? _ui_dgMedia.MaxHeight - 35 : _ui_dgMedia.MaxHeight;
				}
            }
        }

		public void Init(string sParent)
		{

			Init();
		}
        
        #region event handlers
		
        void MediaList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
			if (sParent != "Reduced Panel")
				_ui_srlFiles.Height = this.ActualHeight > 35 ? this.ActualHeight - 35 : this.ActualHeight;
        }

		private void BrowserWindow_Resized(object sender, EventArgs e)
		{
			if (sParent == "Reduced Panel")
			{
				_ui_dgMedia.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
				_ui_srlFiles.Height = _ui_dgMedia.MaxHeight > 35 ? _ui_dgMedia.MaxHeight - 35 : _ui_dgMedia.MaxHeight;
			}
		}
		void _ui_dgFiles_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			var item = (File)e.Row.DataContext;
			if (item.eError!= Error.no)
				e.Row.Background = Coloring.Notifications.cTextBoxError;
			else if (null != _aUnusedFiles.FirstOrDefault(o => o.nID == item.nID))
			{
				e.Row.Background = Coloring.FilesList.cUnusedBackgr;
				e.Row.Foreground = Coloring.FilesList.cUnusedForegr;
			}
			else
			{
				e.Row.Background = Coloring.FilesList.cNormalBackgr;
				e.Row.Foreground = Coloring.FilesList.cNormalForegr;
			}
		}

		#region DBI
		void _cDBI_StoragesGetCompleted(object sender, StoragesGetCompletedEventArgs e)
		{
			try
			{
				if (null == e.Result)
				{
					_dlgMsg.ShowError();
					_dlgProgress.Close();
					return;
				}
				_ui_dgStorages.ItemsSource = e.Result;
				//if(DataGridSelectionMode.Extended == _ui_dgStorages.SelectionMode)
				//    _ui_dgStorages.SelectedItems.Clear();
				//else
				_ui_dgStorages.SelectedItem = null;
				_ui_dgFiles.ItemsSource = null;
				if (null != sStorageDefaultSelection)
				{
					_ui_dgStorages.SelectedItem = e.Result.FirstOrDefault(o => o.sName == sStorageDefaultSelection);
					//_ui_dgStorages_SelectionChanged(null, null);
				}
				else
					_dlgProgress.Close();
			}
			catch { };
		}
		void _cDBI_FilesGetCompleted(object sender, FilesGetCompletedEventArgs e)
		{
			try
			{
				_ui_dgFiles.ItemsSource = new File[0];
				if (null == e.Result)
					return;
				List<File> aResult = e.Result.ToList();
				List<File> aFiles = new List<File>();
				File cFileScroll = null;
				int ni = 0, nStart = 0;

				_ui_Search.DataContextUpdate();
				_ui_Search.aItemsSourceInitial = aResult;

				if (null != sFilenameToScrollTo && "" != sFilenameToScrollTo && null != (cFileScroll = aResult.FirstOrDefault(o => o.sFilename == sFilenameToScrollTo)))
				{
					ni = aResult.IndexOf(cFileScroll);
					nStart = ni > 7 ? ni - 7 : 0;
					//nStart = aResult.Count - nStart < nMaxItemsInTableFiles ? aResult.Count - nMaxItemsInTableFiles : nStart;
					nMaxItemsInTableFiles = aResult.Count - nStart < nMaxItemsInTableFiles ? aResult.Count - nStart : nMaxItemsInTableFiles;
				}

				if (bTakeAllItems || nMaxItemsInTableFiles >= aResult.Count)
				{
					_ui_dgFiles.ItemsSource = aResult;
					if (10 < nStart)
					{
						_ui_dgFiles.UpdateLayout();
						Dispatcher.BeginInvoke(() => _ui_dgFiles.ScrollIntoView(aFiles[nStart], null));
					}
				}
				else
				{
					_ui_dgFiles.ItemsSource = aResult.GetRange(nStart, nMaxItemsInTableFiles);
				}
				

				if (null != cFileScroll)
					_ui_dgFiles.SelectedItem = cFileScroll;

				bTakeAllItems = false;
				if (null != _ui_dgFiles.Tag)
				{
					_ui_dgFiles.Tag = aResult;
					//_ui_Search._bTextChanged = true;
				}
				else
					_ui_dgFiles.Tag = aResult;
				_ui_dgFiles.Focus();
				_dlgProgress.Close();
			}
			catch { };
		}
		void _cDBI_StorageFilesUnusedGetCompleted(object sender, StorageFilesUnusedGetCompletedEventArgs e)
		{
			if (null != e && null != e.Result)
				_aUnusedFiles = e.Result;
			else
				_aUnusedFiles = new File[0];
			_cDBI.FilesGetAsync((long)e.UserState);
		}
		#endregion
		#region UI
		private void _ui_dgStorages_Loaded(object sender, RoutedEventArgs e)
		{
		}
		private void _ui_dgStorages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null == _ui_dgStorages.SelectedItem)
				return; //UNDONE
			_dlgProgress.Show();
            System.Threading.Thread.Sleep(300);
			Files.Set(new File[0]);
			_cDBI.StorageFilesUnusedGetAsync(((Storage)_ui_dgStorages.SelectedItem).nID, ((Storage)_ui_dgStorages.SelectedItem).nID);
		}
		private void _ui_dgFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            SetCurrentSelection();
			_aSelectedFiles = null;
		}
		//DateTime dtLock;
		string sTextPrev = "";
		//bool bFocusOn_ui_tbMaxShowedFiles = false;
		private void _ui_tbMaxShowedFiles_TextChanged(object sender, TextChangedEventArgs e)
		{
		}
		#region контекстное меню

		private void _ui_cmStorages_Opened(object sender, RoutedEventArgs e)
		{
			try
			{
				if (null != _ui_dgStorages.SelectedItem)
				{
					_ui_cmiStorageDelete.IsEnabled = true;
					_ui_cmiStorageProperties.IsEnabled = true;
				}
				else
				{
					_ui_cmiStorageDelete.IsEnabled = false;
					_ui_cmiStorageProperties.IsEnabled = false;
				}
			}
			catch { }
		}
        private void _ui_dgFiles_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _cFileSelected = (File)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
        }
		private void _ui_cmFiles_Opened(object sender, RoutedEventArgs e)
		{
			try
			{
                _ui_cmiShowAll.IsEnabled = true;
				if (null != _ui_dgStorages.SelectedItem)
				{
					_ui_cmiFileDelete.IsEnabled = true;
					_ui_cmiFileProperties.IsEnabled = true;
				}
				else
				{
					_ui_cmiFileDelete.IsEnabled = false;
					_ui_cmiFileProperties.IsEnabled = false;
				}
			}
			catch { }
		}
		private void _ui_cmiRefresh_Click(object sender, RoutedEventArgs e)
		{
			//TODO запомнить выбранные и на них отскролить и в стораджах, и в файлах
			_cDBI.StoragesGetAsync();
		}
		private void _ui_cmiDelete_Click(object sender, RoutedEventArgs e)
		{

		}
		private void _ui_cmiProperties_Click(object sender, RoutedEventArgs e)
		{

		}
        private void _ui_cmiShowAll_Click(object sender, RoutedEventArgs e)
        {
            bTakeAllItems = true;
            _ui_dgStorages_SelectionChanged(null, null);
        }
		#endregion

        private void _ui_dgFiles_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void _ui_dgFiles_RefreshMaxItems()
		{
			if (sTextPrev != _ui_tbMaxShowedFiles.Text)  // dtLock < DateTime.Now &&
			{
				//dtLock = DateTime.Now.AddSeconds(1);
				sTextPrev = _ui_tbMaxShowedFiles.Text;
				string sTextNew = _ui_tbMaxShowedFiles.Text.Trim();
				sTextNew = sTextNew.Substring(0, 5 < sTextNew.Length ? 5 : sTextNew.Length);
				ushort nMax = UInt16.Parse(sTextNew);
				nMaxItemsInTableFiles = nMax;
				_ui_tbMaxShowedFiles.Text = nMax.ToString();
				//if (null != _ui_Search && !_ui_Search.bIsEmpty)
				//    _ui_Search.Clear();
				if (null != _ui_Search && null != _ui_Search.aItemsSourceInitial) //EMERGENCY:l боюсь из-за того, что ты юзаешь чужие Tag'и в контролах, я вапче не уверен, что теперь будет работать
					_ui_Search.Search();
			}
		}
		private void _ui_hlbtnMaxShowedFiles_Click(object sender, RoutedEventArgs e)
		{
			_ui_dgFiles_RefreshMaxItems();
		}

		#endregion
        
        #endregion
	}
}
