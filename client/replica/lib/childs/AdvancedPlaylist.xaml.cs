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

using dbi = helpers.replica.services.dbinteract;
using g = globalization;
using replica.sl;
using controls.childs.sl;
using controls.sl;

namespace controls.childs.replica.sl
{
	public partial class AdvancedPlaylist : ChildWindow
	{
		static private AssetsChooser _dlgAssetsChooser;
		private dbi.DBInteract _cDBI;
		private Progress _dlgProgress = new Progress();
		private MsgBox _dlgMsg = new MsgBox();
		private List<AdvancedPlaylistSL> _aAPLs;
		private List<AdvancedPlaylistSL> _aDGRowsOnScreen;
		private List<AdvancedPlaylistSL> _aAPLsUnfolded;
		private bool _bAdding;
		private AdvancedPlaylistSL _cAPLIForDoubleClick;
		private DateTime _dtNextMouseClickForDoubleClick;
		private DateTime _dtSelectedInPL;
		public DateTime dtSelectedInPL
		{
			set
			{
				_dtSelectedInPL = value;
			}
		}
		private DateTime _dtMinimumInPL;
		public DateTime dtMinimumInPL
		{
			set
			{
				_dtMinimumInPL = value;
			}
			get
			{
				if (_dtMinimumInPL == DateTime.MaxValue)
					return DateTime.Now.AddMinutes(20);
				return _dtMinimumInPL;
			}
		}
		public AdvancedPlaylist()
		{
			InitializeComponent();
			_dtSelectedInPL = DateTime.MaxValue;
			_dtMinimumInPL = DateTime.MaxValue;
			_bAdding = false;

			_aDGRowsOnScreen = new List<AdvancedPlaylistSL>();
			_cDBI = new dbi.DBInteract();
			_cDBI.AdvancedPlaylistAddReplaceCompleted += _cDBI_AdvancedPlaylistAddReplaceCompleted;
			_cDBI.AdvancedPlaylistDeleteCompleted += _cDBI_AdvancedPlaylistDeleteCompleted;
			_cDBI.AdvancedPlaylistGetCompleted += _cDBI_AdvancedPlaylistGetCompleted;
			_cDBI.AdvancedPlaylistsGetCompleted += _cDBI_AdvancedPlaylistsGetCompleted;
			_cDBI.AdvancedPlaylistRenameCompleted += _cDBI_AdvancedPlaylistRenameCompleted;
			_cDBI.AdvancedPlaylistStartCompleted += _cDBI_AdvancedPlaylistStartCompleted;
			_cDBI.AdvancedPlaylistItemSaveCompleted += _cDBI_AdvancedPlaylistItemSaveCompleted;
			_cDBI.PlaylistItemMinimumForImmediatePLGetCompleted += _cDBI_PlaylistItemMinimumForImmediatePLGetCompleted;
				
			//_ui_Main.IsEnabled = false;
			_ui_dgAdvancedPL.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgAdvancedPL_LoadingRow);
			_ui_dgAdvancedPL.LayoutUpdated += _ui_dgAdvancedPL_LayoutUpdated;
			_ui_dgAdvancedPL.MouseWheel += _ui_dgAdvancedPL_MouseWheel;
            _ui_dgAdvancedPL.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgAdvancedPL_MouseLeftButtonDown), true);

			_cDBI.PlaylistItemMinimumForImmediatePLGetAsync(new object());
		}




		#region dbi and msg callbacks
		private void _cDBI_PlaylistItemMinimumForImmediatePLGetCompleted(object sender, dbi.PlaylistItemMinimumForImmediatePLGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				_dtMinimumInPL = e.Result.dtStartPlanned.AddSeconds(2);
				if (e.UserState == null)
					_dlgMsg.Show("<" + e.Result.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "  " + e.Result.sName + ">", "Информация", MsgBox.MsgBoxButton.OK);
				_ui_btnCheckMinimum.IsEnabled = true;
			}
			else
				_dtMinimumInPL = DateTime.Now.AddMinutes(20);
        }
		private void _cDBI_AdvancedPlaylistStartCompleted(object sender, dbi.AdvancedPlaylistStartCompletedEventArgs e)
		{
			if (e.Result)
				_ui_lblStatus.Content = g.Replica.sNoticePlaylist34;    //"Задание размещено";
			else
				_dlgMsg.ShowError(g.Replica.sErrorPlaylist12); //"Размещение задания прошло с ошибками"
			_ui_Main.IsEnabled = true;
		}
		private void _cDBI_AdvancedPlaylistsGetCompleted(object sender, dbi.AdvancedPlaylistsGetCompletedEventArgs e)
		{
			_ui_btnEdit.Width = _ui_btnEdit.ActualWidth < 70 ? 75 : _ui_btnEdit.ActualWidth + 10;
			_ui_btnDelete.Width = _ui_btnDelete.ActualWidth < 70 ? 75 : _ui_btnDelete.ActualWidth + 10;
			_ui_btnAdd.Width = _ui_btnAdd.ActualWidth < 70 ? 75 : _ui_btnAdd.ActualWidth + 10;
			_ui_btnRename.Width = _ui_btnRename.ActualWidth < 70 ? 75 : _ui_btnRename.ActualWidth + 10;
			CancelButton.Visibility = Visibility.Collapsed;
            if (null != e.Result)
			{
				_aAPLs = AdvancedPlaylistSL.GetArrayOfSLs(e.Result).ToList();
				_aAPLs.Sort(AdvancedPlaylistSL.dComparison);

				FillDG(_aAPLs);
			}
			_dlgProgress.Close();
		}
		private void _cDBI_AdvancedPlaylistGetCompleted(object sender, dbi.AdvancedPlaylistGetCompletedEventArgs e)
		{
			if (null != e && null == e.Result)
			{
				_dlgMsg.ShowError(g.Common.sErrorSave);
				_ui_Main.IsEnabled = true;
				return;
			}

			_dlgAssetsChooser = new AssetsChooser();
			_dlgAssetsChooser.Closed += new EventHandler(dlgAssetsChooser_Closed);
			_dlgAssetsChooser.bIsBlock = true;
			if (_bAdding)
				_dlgAssetsChooser.dtSelected = _dtSelectedInPL;
			else
			{
				AdvancedPlaylistSL cAPLSL = (AdvancedPlaylistSL)e.UserState;
				if (cAPLSL.dtStart == DateTime.MaxValue)
					_dlgAssetsChooser.bIsBlock = false;
				else
				{
					_dlgAssetsChooser.dtSelected = cAPLSL.dtStart;
				}
				_dlgAssetsChooser.aSelectedAssets = e.Result.aItems.Select(o => AssetSL.GetAssetSL(o.oAsset)).ToList();
			}
			_dlgAssetsChooser.dtStart = dtMinimumInPL;
			_dlgAssetsChooser.dtEnd = DateTime.Now.AddMonths(3);
            _dlgAssetsChooser.Show();
		}
		void dlgAssetsChooser_Closed(object sender, EventArgs e)
		{
			_dlgAssetsChooser.Closed -= dlgAssetsChooser_Closed;
			if (_dlgAssetsChooser.dtSelected < DateTime.Now)
			{
				_dlgMsg.ShowError(g.Common.sErrorDateTime);
				_ui_Main.IsEnabled = true;
				return;
			}
			List<AssetSL> aAssets = _dlgAssetsChooser.aSelectedAssets;
			if (
				_dlgAssetsChooser.DialogResult == null ||
				_dlgAssetsChooser.DialogResult == false ||
				aAssets.Count < 1
				)
			{
				_ui_Main.IsEnabled = true;
				return;
			}
			if (
				_bAdding ||
				_ui_dgAdvancedPL.SelectedItem == null ||
				!(_ui_dgAdvancedPL.SelectedItem is AdvancedPlaylistSL)
				)
			{
				_dlgMsg.Closed += new EventHandler(_dlgMsg_AddNewAPL_Closed);
				_dlgMsg.ShowQuestion(g.Replica.sNoticePlaylist33, "");
				return;
			}
			dbi.Playlist cAPL = AdvancedPlaylistSL.GetBase((AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem);
			dbi.PluginPlaylistItem[] aPLIs = AdvancedPlaylistSL.GetArrayOfAPLIs(aAssets.ToArray(), (_dlgAssetsChooser.bIsBlock ? _dlgAssetsChooser.dtSelected:DateTime.MaxValue));
			cAPL.dtStart = _dlgAssetsChooser.bIsBlock ? _dlgAssetsChooser.dtSelected : DateTime.MaxValue;
			cAPL.dtStop = _dlgAssetsChooser.bIsBlock ? aPLIs[aPLIs.Length - 1].dtStarted.AddMilliseconds(40 * aPLIs[aPLIs.Length - 1].oAsset.nFramesQty) : DateTime.MaxValue;
			cAPL.aItems = aPLIs;
			_cDBI.AdvancedPlaylistAddReplaceAsync(cAPL, cAPL);
		}
        void _dlgMsg_AddNewAPL_Closed(object sender, EventArgs e)
		{
			_dlgMsg.Closed -= _dlgMsg_AddNewAPL_Closed;
			if (_dlgMsg.DialogResult == true)
			{
				if (_bAdding)
				{
					List<AssetSL> aAssets = _dlgAssetsChooser.aSelectedAssets;
					dbi.PluginPlaylistItem[] aPLIs = AdvancedPlaylistSL.GetArrayOfAPLIs(aAssets.ToArray(), (_dlgAssetsChooser.bIsBlock ? _dlgAssetsChooser.dtSelected : DateTime.MaxValue));
					dbi.Playlist cAPL = new dbi.Playlist()
					{
						nID = -1,
						sName = _dlgMsg.sText,
						dtStart = _dlgAssetsChooser.bIsBlock ? _dlgAssetsChooser.dtSelected : DateTime.MaxValue,
						dtStop = _dlgAssetsChooser.bIsBlock ? aPLIs[aPLIs.Length - 1].dtStarted.AddMilliseconds(40 * aPLIs[aPLIs.Length - 1].oAsset.nFramesQty) : DateTime.MaxValue,
						aItems = aPLIs,
					};
					_cDBI.AdvancedPlaylistAddReplaceAsync(cAPL, cAPL);
				}
			}
			else
				_ui_Main.IsEnabled = true;
			_bAdding = false;
		}
		private void _cDBI_AdvancedPlaylistDeleteCompleted(object sender, dbi.AdvancedPlaylistDeleteCompletedEventArgs e)
		{
			if (!e.Result || null == e.UserState || !(e.UserState is dbi.Playlist))
			{
				_dlgMsg.ShowError(g.Common.sErrorSave);
			}
			else
			{
				AdvancedPlaylistSL cAPLSL = (AdvancedPlaylistSL)e.UserState;
				_aAPLs.Remove(cAPLSL);
				_aAPLs = _aAPLs.OrderBy(o => o.dtStart).ToList();
				FillDG(_aAPLs);
				_ui_lblStatus.Content = g.Common.sChangesSaved;
			}
			_ui_Main.IsEnabled = true;
		}
		private void _cDBI_AdvancedPlaylistRenameCompleted(object sender, dbi.AdvancedPlaylistRenameCompletedEventArgs e)
		{
			if (!e.Result || null == e.UserState || !(e.UserState is dbi.Playlist))
			{
				_dlgMsg.ShowError(g.Common.sErrorSave);
			}
			else
			{
				AddOrReplace((dbi.Playlist)e.UserState);
				_ui_lblStatus.Content = g.Common.sChangesSaved;
			}
			_ui_Main.IsEnabled = true;
		}
		private void _cDBI_AdvancedPlaylistAddReplaceCompleted(object sender, dbi.AdvancedPlaylistAddReplaceCompletedEventArgs e)
		{
			if (0 > e.Result || null == e.UserState || !(e.UserState is dbi.Playlist))
			{
				_dlgMsg.ShowError(g.Common.sErrorSave);
			}
			else
			{
				dbi.Playlist cAPL = (dbi.Playlist)e.UserState;
				if (cAPL.nID < 1) // adding
					cAPL.nID = e.Result;
				AddOrReplace(cAPL);
				_ui_lblStatus.Content = g.Common.sChangesSaved;
            }
			_ui_Main.IsEnabled = true;
		}
		private void _cDBI_AdvancedPlaylistItemSaveCompleted(object sender, dbi.AdvancedPlaylistItemSaveCompletedEventArgs e)
		{
			if (!e.Result)
			{
				_dlgMsg.ShowError(g.Common.sErrorSave);
			}
			else
			{
				_ui_lblStatus.Content = g.Common.sChangesSaved;
			}
			_ui_Main.IsEnabled = true;
		}
		#endregion
		#region other
		private void AddOrReplace(dbi.Playlist cAPL)
		{
			if (null == _aAPLs)
				_aAPLs = new List<AdvancedPlaylistSL>();
			AdvancedPlaylistSL cAPLSL = AdvancedPlaylistSL.GetSL(cAPL);
			AdvancedPlaylistSL cAPLSLOld;
			if (null != (cAPLSLOld = _aAPLs.FirstOrDefault(o => o.nID == cAPLSL.nID)))
				_aAPLs.Remove(cAPLSLOld);

			_aAPLs.Add(cAPLSL);
			_aAPLs = _aAPLs.ToList();
			_aAPLs.Sort(AdvancedPlaylistSL.dComparison);
			FillDG(_aAPLs);
		}
		#endregion
		#region ui
		void _ui_dgAdvancedPL_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				_ui_lblStatus.Content = "";
				FrameworkElement cFEClick = (FrameworkElement)((RoutedEventArgs)(e)).OriginalSource;
				if (cFEClick.Parent is DataGridCell)
				{
					if (_dtNextMouseClickForDoubleClick < DateTime.Now)
					{
						_dtNextMouseClickForDoubleClick = DateTime.Now.AddMilliseconds(400);
						_cAPLIForDoubleClick = (AdvancedPlaylistSL)cFEClick.DataContext;
					}
					else
					{
						//FrameworkElement cFEcolumn2 = _ui_dgAdvancedPL.Columns[2].GetCellContent(_cAPLIForDoubleClick);
						_dtNextMouseClickForDoubleClick = DateTime.MinValue;
						if (_cAPLIForDoubleClick == (AdvancedPlaylistSL)cFEClick.DataContext)   // значит был даблклик на этом же объекте
						{
							if (_cAPLIForDoubleClick is AdvancedPlaylistItemSL)
							{
								TimeCodeEnter dlgCodeEnter = new TimeCodeEnter();
								dlgCodeEnter.nInitialCode = _cAPLIForDoubleClick.aItems[0].nFramesQty < long.MaxValue ? _cAPLIForDoubleClick.aItems[0].nFramesQty : _cAPLIForDoubleClick.aItems[0].oAsset.nFramesQty;
								dlgCodeEnter.Closed += dlgCodeEnter_Closed;
								dlgCodeEnter.Show();
							}
							else
							{
								int nIndx = _aAPLs.IndexOf(_cAPLIForDoubleClick);
								_aAPLsUnfolded = _aAPLs.GetRange(0, nIndx + 1);
								_aAPLsUnfolded.AddRange(_cAPLIForDoubleClick.GetAPLIs());
								if (nIndx + 1 < _aAPLs.Count)
									_aAPLsUnfolded.AddRange(_aAPLs.GetRange(nIndx + 1, _aAPLs.Count - nIndx - 1));
								FillDG(_aAPLsUnfolded);
							}
						}
					}
				}
			}
			catch { };
		}
		private void dlgCodeEnter_Closed(object sender, EventArgs e)
		{
			TimeCodeEnter dlgCodeEnter = (TimeCodeEnter)sender;
			dlgCodeEnter.Closed -= dlgCodeEnter_Closed;
			if (true == dlgCodeEnter.DialogResult)
			{
				_ui_Main.IsEnabled = false;
				AdvancedPlaylistItemSL cAPLI = (AdvancedPlaylistItemSL)_cAPLIForDoubleClick;
				cAPLI.nFramesQty = dlgCodeEnter.nResultCode;
				cAPLI.aItems[0].nFramesQty = dlgCodeEnter.nResultCode;

				cAPLI.dtStop = cAPLI.CheckPlannedStartsAndGetEndOfPL(cAPLI.aItems);

				_cDBI.AdvancedPlaylistItemSaveAsync(cAPLI.aItems[0]);
				FillDG(_aAPLsUnfolded);
			}
		}
		private void _ui_dgAdvancedPL_MouseWheel(object sender, MouseWheelEventArgs e)
		{

		}
		private void _ui_dgAdvancedPL_LayoutUpdated(object sender, EventArgs e)
		{
			PaintDG();
            _aDGRowsOnScreen.Clear();
		}
		private void FillDG(List<AdvancedPlaylistSL> aAPL)
		{
			_ui_dgAdvancedPL.ItemsSource = null;
			_ui_dgAdvancedPL.ItemsSource = aAPL;
			//_ui_dgAdvancedPL.UpdateLayout();
			
		}
		private void PaintDG()
		{
			int nIndx;
			foreach (var cAPLSL in _aDGRowsOnScreen)
			{
				nIndx = -1;
				foreach (var col in _ui_dgAdvancedPL.Columns)
				{
					nIndx++;
					var content = col.GetCellContent(cAPLSL);
					if (content != null)
					{
						DataGridCell cell = content.Parent as DataGridCell;
						cell.Background = Coloring.Playlist.cRow_PluginNormalBackgr;
						if (nIndx > 2)
						{
							if (!(cAPLSL is AdvancedPlaylistItemSL) && nIndx == 3)
								continue;
							else
							{
								switch (cAPLSL.sStatus)
								{
									case "planned":
										cell.Background = Coloring.Playlist.cRow_PlannedClipBackgr;
										break;
									case "played":
										cell.Background = Coloring.Playlist.cRow_CachedBackgr;
										break;
									case "onair":
										cell.Background = Coloring.Playlist.cRow_PlannedDesignBackgr;
										break;
									case "skipped":
										cell.Background = Coloring.Playlist.cTypeColumn_AdvertsBackgr;
										break;
									case "error":
										cell.Background = Coloring.Playlist.cTypeColumn_AdvertsBackgr;
										break;
									case "failed":
										cell.Background = Coloring.Playlist.cRow_PlannedAdvBackgr;
										break;
									default:
										cell.Background = Coloring.Playlist.cRow_PlannedOtherBackgr;
										break;
								}
							}
						}
					}
				}
			}
		}
		protected override void OnOpened()
		{
			base.OnOpened();
			_dlgProgress.Show();
			_dlgProgress.sInfo = "getting playlists...";
			_cDBI.AdvancedPlaylistsGetAsync(DateTime.Now.AddDays(-7), DateTime.MaxValue);
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
		private void _ui_btnDelete_Click(object sender, RoutedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			if (_ui_dgAdvancedPL.SelectedItem != null && _ui_dgAdvancedPL.SelectedItem is AdvancedPlaylistSL)
			{
				AdvancedPlaylistSL cAPLSL = (AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem;
				if (cAPLSL.sStatus == "onair" || (cAPLSL.sStatus == "planned" && cAPLSL.dtStart.AddMinutes(-1) < DateTime.Now && cAPLSL.dtStop.AddMinutes(1) > DateTime.Now))
					_dlgMsg.ShowError(g.Replica.sErrorPlaylist10);
				else
				{
					_ui_Main.IsEnabled = false;
					_cDBI.AdvancedPlaylistDeleteAsync(AdvancedPlaylistSL.GetBase(cAPLSL), cAPLSL);
				}
			}
			else
			{
				_dlgMsg.ShowError(g.Common.sNoItemsSelected);
			}
		}
		private void _ui_btnAdd_Click(object sender, RoutedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			_ui_Main.IsEnabled = false;
			_bAdding = true;
			_cDBI_AdvancedPlaylistGetCompleted(null, null);
		}
		private void _ui_btnEdit_Click(object sender, RoutedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			if (_ui_dgAdvancedPL.SelectedItem != null && _ui_dgAdvancedPL.SelectedItem is AdvancedPlaylistSL)
			{
				AdvancedPlaylistSL cAPLSL = (AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem;
				if (cAPLSL.sStatus != "planned")
					_dlgMsg.ShowError(g.Replica.sErrorPlaylist10);
				else
				{
					_ui_Main.IsEnabled = false;
					_cDBI.AdvancedPlaylistGetAsync(AdvancedPlaylistSL.GetBase(cAPLSL), cAPLSL);
				}
			}
			else
			{
				_dlgMsg.ShowError(g.Common.sNoItemsSelected);
            }
		}
		private void _ui_btnRename_Click(object sender, RoutedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			if (_ui_dgAdvancedPL.SelectedItem != null && _ui_dgAdvancedPL.SelectedItem is AdvancedPlaylistSL)
			{
				AdvancedPlaylistSL cAPLSL = (AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem;
				if (cAPLSL.sStatus != "planned" || cAPLSL.dtStart.AddMinutes(-1) < DateTime.Now)
					_dlgMsg.ShowError(g.Replica.sErrorPlaylist10);
				else
				{
					_ui_Main.IsEnabled = false;
					_dlgMsg.Closed += new EventHandler(_dlgMsg_RenameAPL_Closed);
					_dlgMsg.ShowQuestion(g.Replica.sNoticePlaylist33, (cAPLSL).sName);
				}
			}
			else
			{
				_dlgMsg.ShowError(g.Common.sNoItemsSelected);
			}
		}
		void _dlgMsg_RenameAPL_Closed(object sender, EventArgs e)
		{
			_dlgMsg.Closed -= _dlgMsg_RenameAPL_Closed;
			if (_dlgMsg.DialogResult == true)
			{
				dbi.Playlist cAPL = AdvancedPlaylistSL.GetBase((AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem);
				cAPL.sName = _dlgMsg.sText;
				_cDBI.AdvancedPlaylistRenameAsync(cAPL, cAPL);
			}
			else
				_ui_Main.IsEnabled = true;
		}
		private void _ui_btnStart_Click(object sender, RoutedEventArgs e)
		{
			_ui_lblStatus.Content = "";
			_ui_Main.IsEnabled = false;

			_dlgMsg.Closed += _dlgMsg_Start_Closed;
			dbi.Playlist cAPL = AdvancedPlaylistSL.GetBase((AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem);
			_dlgMsg.ShowQuestion(g.Replica.sNoticePlaylist35, cAPL.sName);
		}
		private void _ui_btnCheckMinimum_Click(object sender, RoutedEventArgs e)
		{
			_cDBI.PlaylistItemMinimumForImmediatePLGetAsync();
			_ui_btnCheckMinimum.IsEnabled = false;
		}
			

		private void _dlgMsg_Start_Closed(object sender, EventArgs e)
		{
			_dlgMsg.Closed -= _dlgMsg_Start_Closed;
			if (_dlgMsg.DialogResult == true)
			{
				dbi.Playlist cAPL = AdvancedPlaylistSL.GetBase((AdvancedPlaylistSL)_ui_dgAdvancedPL.SelectedItem);
				_cDBI.AdvancedPlaylistStartAsync(cAPL);
			}
			else
				_ui_Main.IsEnabled = true;
		}

		private void _ui_dgAdvancedPL_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			int nCount = _ui_dgAdvancedPL.SelectedItems.Count;
			if (1 < nCount || 0 == nCount)
			{
				_ui_btnAdd.IsEnabled = true;
				_ui_btnDelete.IsEnabled = false;
				_ui_btnEdit.IsEnabled = false;
				_ui_btnRename.IsEnabled = false;
				_ui_btnStart.IsEnabled = false;
				return;
			}
			if (_ui_dgAdvancedPL.SelectedItem is AdvancedPlaylistItemSL)
			{
				_ui_btnAdd.IsEnabled = true;
				_ui_btnDelete.IsEnabled = false;
				_ui_btnEdit.IsEnabled = false;
				_ui_btnRename.IsEnabled = false;
				_ui_btnStart.IsEnabled = false;
			}
			else
			{
				_ui_btnAdd.IsEnabled = true;
				_ui_btnDelete.IsEnabled = true;
				_ui_btnEdit.IsEnabled = true;
				_ui_btnRename.IsEnabled = true;
				_ui_btnStart.IsEnabled = true;
			}

			//if (0 == i)
			//	_ui_lblPlannedSelected.Content = " ";
			//else
			//	_ui_lblPlannedSelected.Content = i.ToString();

			//if (1 == i)
			//	_cPlayListItemCurrent = (PlaylistItemSL)_ui_dgPlanned.SelectedItem;
			//else
			//	_cPlayListItemCurrent = null;

			//if (0 < _ui_nudHoursQty.Value && 1 == i && DateTime.MaxValue > _cPlayListItemCurrent.dtTimingsUpdate || 0 == _ui_nudHoursQty.Value)
			//	_ui_btnRecalculatePart.IsEnabled = true;
			//else
			//	_ui_btnRecalculatePart.IsEnabled = false;
		}
		
		private void _ui_dgAdvancedPL_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			if (e.Row == null || e.Row.DataContext == null)
				return;

			var cAPLSL = (AdvancedPlaylistSL)e.Row.DataContext;
			_aDGRowsOnScreen.Add(cAPLSL);


			//TextBox tBox = ((TextBox)_ui_dgAdvancedPL.Columns[3].GetCellContent(e.Row));
			//switch (cAPL.sStatus)
			//{
			//	case "planned":
			//		e.Row.Background = Coloring.Playlist.cRow_PlannedClipBackgr;
			//		break;
			//	case "played":
			//		e.Row.Background = Coloring.Playlist.cRow_CachedBackgr;
			//		break;
			//	case "onair":
			//		e.Row.Background = Coloring.Playlist.cRow_PlannedDesignBackgr;
			//		break;
			//	case "skipped":
			//		e.Row.Background = Coloring.Playlist.cTypeColumn_AdvertsBackgr;
			//		break;
			//	case "error":
			//		e.Row.Background = Coloring.Playlist.cTypeColumn_AdvertsBackgr;
			//		break;
			//	case "failed":
			//		e.Row.Background = Coloring.Playlist.cRow_PlannedAdvBackgr;
			//		break;
			//	default:
			//		e.Row.Background = Coloring.Playlist.cRow_PlannedOtherBackgr;
			//		break;
			//}
			if (cAPLSL is AdvancedPlaylistItemSL)
				e.Row.Height = 19;
			else
				e.Row.Height = 28;

		}
		#endregion
	}
}

