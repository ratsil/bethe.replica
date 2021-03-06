﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO.IsolatedStorage;
using controls.childs.sl;

using replica.sl;
using controls.sl;
using g = globalization;
using helpers.extensions;

namespace controls.childs.replica.sl
{
	public partial class AssetsChooser : ChildWindow
	{
		private List<AssetSL> _aSelectedAssets;
		public List<AssetSL> aSelectedAssets
		{
			get
			{
				List<AssetSL> aRetVal = null;
				if (null != _ui_lbClipsSelected.ItemsSource && (aRetVal = (List<AssetSL>)_ui_lbClipsSelected.ItemsSource).Count > 0)
				{
					return aRetVal;
				}
				else
					return _aSelectedAssets;
			}
			set
			{
				_aSelectedAssets = value;
				_ui_btnAddToSequence_Click(null, null);
			}
		}
		public DateTime dtStart
		{
			set
			{
				_dtMinimum = value;
				_ui_dtpDateTime.DisplayDateStart = value;
			}
		}
		public DateTime dtEnd
		{
			set
			{
				_ui_dtpDateTime.DisplayDateEnd = value;
			}
		}
		public DateTime dtSelected
		{
			set
			{
				_ui_dtpDateTime.SelectedDate = value;
				_ui_tmpDateTime.Value = value;
			}
			get
			{
				return DateTimeGet();
			}
		}
		public bool bIsBlock
		{
			get
			{
				if (null == _ui_chbAsBlock.IsChecked)
					return false;
				else
					return _ui_chbAsBlock.IsChecked.Value;
			}
			set
			{
				_ui_chbAsBlock.IsChecked = value;
			}
		}
		private DateTime _dtMinimum;
		private DateTime _dtNextMouseClickForDoubleClick;
		private AssetSL _cAssetForDoubleClick;
		public DateTime dtHard
		{
			get
			{
				if (bIsBlock && _ui_rbtnHard.IsChecked.Value)
					return DateTimeGet();
				else
					return DateTime.MaxValue;
			}
		}
		public DateTime dtSoft
		{
			get
			{
				if (bIsBlock && _ui_rbtnSoft.IsChecked.Value)
					return DateTimeGet();
				else
					return DateTime.MaxValue;
			}
		}
		public DateTime dtPlanned
		{
			get
			{
				if (bIsBlock && _ui_rbtnPlanned.IsChecked.Value)
					return DateTimeGet();
				else
					return DateTime.MaxValue;
			}
		}
		private bool bChangedByMe;
		public AssetsChooser()
		{
			InitializeComponent();
			Title = g.Helper.sSelectAsset;

			_aSelectedAssets = new List<AssetSL>();

			_ui_chbAsBlock_Unchecked(null, null);
			_ui_chbAsBlock.IsEnabled = false;
			_ui_rbtnSoft.IsChecked = true;
			_ui_rbtnHard.IsChecked = false;
			_ui_rbtnPlanned.IsChecked = false;

			_ui_lbClipsSelected.ItemsSource = new List<AssetSL>();
			_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxActive;
			_ui_tmpDateTime.ValueChanged += _ui_tmpDateTime_ValueChanged;

			_ui_rbtnHard.Checked += new RoutedEventHandler(_ui_rbtn_Checked);
			_ui_rbtnSoft.Checked += new RoutedEventHandler(_ui_rbtn_Checked);
			_ui_rbtnPlanned.Checked += new RoutedEventHandler(_ui_rbtn_Checked);
			_ui_lbClipsSelected.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_lbClipsSelected_MouseLeftButtonDown), true);
			_ui_lbClipsSelected.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_lbClipsSelected_KeyDown), true);
			_ui_lbClipsSelected.AddHandler(Button.KeyUpEvent, new KeyEventHandler(_ui_lbClipsSelected_KeyUp), true);
            _ui_nudFramesQty.ValueChanged += _ui_nudFramesQty_ValueChanged;
			bChangedByMe = false;
        }
        private Progress _dlgProgress;
        

        private void _ui_nudFramesQty_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_ui_lbClipsSelected.SelectedItem == null)
				return;
			if (!bChangedByMe)
			{
				AssetSL cSelectedAsset = (AssetSL)_ui_lbClipsSelected.SelectedItem;
				cSelectedAsset.nFrameOut = (long)_ui_nudFramesQty.Value;
			}
			bChangedByMe = false;
			ShowDuration();
		}
		private void ShowDuration()
		{
			if (null == _ui_lbClipsSelected.ItemsSource)
			{
				_ui_tbDuration.Text = "----";
				return;
			}
            List<AssetSL> aAssets = (List<AssetSL>)_ui_lbClipsSelected.ItemsSource;

			long nDur = 0;
			foreach (AssetSL cA in aAssets)
				nDur += cA.nFrameOut;
			_ui_tbDuration.Text = nDur.ToFramesString(true, true, true, true);
		}

		private void _ui_tmpDateTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
		{
			if (null != _ui_dtpDateTime.SelectedDate && null != _ui_dtpDateTime.DisplayDateStart && _ui_dtpDateTime.SelectedDate.Value.Date == _ui_dtpDateTime.DisplayDateStart.Value.Date)
				if (null != _ui_tmpDateTime.Value && _ui_tmpDateTime.Value.Value.TimeOfDay < _dtMinimum.TimeOfDay)
				{
					_ui_tmpDateTime.ValueChanged -= _ui_tmpDateTime_ValueChanged;
                    _ui_tmpDateTime.Value = _dtMinimum;
					_ui_tmpDateTime.ValueChanged += _ui_tmpDateTime_ValueChanged;
				}
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dlgProgress.Show();
            _dlgProgress.Close();
        }
        protected override void OnOpened()
		{
            base.OnOpened();
            controls.replica.sl.AssetsList.Tab eDefault = controls.replica.sl.AssetsList.Tab.Clips;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("ac_default_tab"))
                eDefault = (controls.replica.sl.AssetsList.Tab)IsolatedStorageSettings.ApplicationSettings["ac_default_tab"];
			_ui_al.dgSelectionChanged = SelectionChanged;
			_ui_al.dgDoubleClick = OnDoubleClick;
            _ui_al.dgOnTabChanged = OnTabChanged;
            _ui_al.Init(eDefault);
            _ui_btnDelete.IsEnabled = false;
			_ui_btnDown.IsEnabled = false;
			_ui_btnUp.IsEnabled = false;
			_ui_nudFramesQty.Minimum = 25;
			_ui_nudFramesQty.IsEnabled = false;
            _ui_tbDuration.Text = "----";
            _dlgProgress = new Progress();
        }

        private void OnTabChanged(controls.replica.sl.AssetsList.Tab eCurrentTab)
        {
            IsolatedStorageSettings.ApplicationSettings["ac_default_tab"] = eCurrentTab;
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        private DateTime DateTimeGet()
		{
			DateTime dtD, dtT;
			dtT = _ui_tmpDateTime.Value.Value;
			dtD = _ui_dtpDateTime.SelectedDate.Value;
			return new DateTime(dtD.Year, dtD.Month, dtD.Day, dtT.Hour, dtT.Minute, dtT.Second, dtT.Millisecond);
		}
        private void SelectionChanged(List<AssetSL> aAssetsSelected)
        {
            if (null != aAssetsSelected)
            {
                if (1 == aAssetsSelected.Count)
                    _ui_lblNameOfSelected.Content = aAssetsSelected[0].sCuesName == "" ? aAssetsSelected[0].sName : aAssetsSelected[0].sCuesName;
                else
                    _ui_lblNameOfSelected.Content = g.Common.sQuantity + ":" + aAssetsSelected.Count;
                _aSelectedAssets = aAssetsSelected;
            }
            else
                _ui_lblNameOfSelected.Content = g.Common.sNoItemsSelected.ToUpper();
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

		public void OnDoubleClick(AssetSL cAssetSL)
		{
			_ui_lbClipsSelected_Add(cAssetSL);
		}
		private void _ui_lbClipsSelected_Add(AssetSL cAss)
		{
			if (null != cAss)
			{
				List<AssetSL> aAss;
				aAss = ((IEnumerable<AssetSL>)_ui_lbClipsSelected.ItemsSource).ToList<AssetSL>();
				if (!aAss.Contains(cAss))
				{
					aAss.Add(cAss);
					_ui_lbClipsSelected.ItemsSource = aAss;
					ShowDuration();
					_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxChanged;
				}
				_ui_lbClipsSelected.UpdateLayout();
				_ui_chbAsBlock.IsEnabled = true;
			}
		}
		private void _ui_lbClipsSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null != _ui_lbClipsSelected.SelectedItem)
			{
				_ui_btnDelete.IsEnabled = true;
				_ui_btnDown.IsEnabled = true;
				_ui_btnUp.IsEnabled = true;
				_ui_nudFramesQty.IsEnabled = true;
				ShowFramesQty((AssetSL)_ui_lbClipsSelected.SelectedItem);
			}
			else
			{
				_ui_btnDelete.IsEnabled = false;
				_ui_btnDown.IsEnabled = false;
				_ui_btnUp.IsEnabled = false;
				bChangedByMe = true;
				_ui_nudFramesQty.Value = 0;
				_ui_nudFramesQty.IsEnabled = false;
            }
			//AssetSL cSelectedAsset = (AssetSL)_ui_lbClipsSelected.SelectedItem;
			//_ui_lbClipsSelected_Remove(cSelectedAsset);
		}
		private void ShowFramesQty(AssetSL cA)
		{
			bChangedByMe = true;
			_ui_nudFramesQty.Maximum = cA.nFramesQty;
			_ui_nudFramesQty.Value = cA.nFrameOut;
        }
		private void _ui_lbClipsSelected_Remove(AssetSL cAsset)
		{
			if (null != cAsset)
			{
				List<AssetSL> aAss;
				aAss = ((IEnumerable<AssetSL>)_ui_lbClipsSelected.ItemsSource).ToList<AssetSL>();
				if (aAss.Contains(cAsset))
				{
					int nIndx = aAss.IndexOf(cAsset);
					aAss.Remove(cAsset);
					_ui_lbClipsSelected.ItemsSource = aAss;
					ShowDuration();
                    if (aAss.Count > 0)
						_ui_lbClipsSelected.SelectedItem = aAss[nIndx > aAss.Count - 1 ? aAss.Count - 1 : nIndx];
				}
				_ui_lbClipsSelected.UpdateLayout();
			}
			if (0 == ((IEnumerable<AssetSL>)_ui_lbClipsSelected.ItemsSource).ToList<AssetSL>().Count)
			{
				_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxActive;
				_ui_chbAsBlock.IsEnabled = false;
			}
		}
		private void _ui_btnAddToSequence_Click(object sender, RoutedEventArgs e)
		{
			foreach (AssetSL cAss in _aSelectedAssets)
			{
				_ui_lbClipsSelected_Add(cAss);
			}
		}

		private void _ui_chbAsBlock_Checked(object sender, RoutedEventArgs e)
		{
			_ui_spDateTime.Visibility = System.Windows.Visibility.Visible;
		}
		private void _ui_chbAsBlock_Unchecked(object sender, RoutedEventArgs e)
		{
			_ui_spDateTime.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void _ui_rbtn_Checked(object sender, RoutedEventArgs e)
		{
			_ui_rbtnHard.Checked -= _ui_rbtn_Checked;
			_ui_rbtnSoft.Checked -= _ui_rbtn_Checked;
			_ui_rbtnPlanned.Checked -= _ui_rbtn_Checked;

			RadioButton cRB = (RadioButton)sender;
			_ui_rbtnHard.IsChecked = false;
			_ui_rbtnSoft.IsChecked = false;
			_ui_rbtnPlanned.IsChecked = false;
			cRB.IsChecked = true;
			_ui_rbtnHard.Checked += new RoutedEventHandler(_ui_rbtn_Checked);
			_ui_rbtnSoft.Checked += new RoutedEventHandler(_ui_rbtn_Checked);
			_ui_rbtnPlanned.Checked += new RoutedEventHandler(_ui_rbtn_Checked);
		}

		private void _ui_btnUp_Click(object sender, RoutedEventArgs e)
		{
			AssetSL cSelectedAsset = (AssetSL)_ui_lbClipsSelected.SelectedItem;
			List<AssetSL> aAssets;
			aAssets = ((IEnumerable<AssetSL>)_ui_lbClipsSelected.ItemsSource).ToList<AssetSL>();
			int nIndx;
			if (aAssets.Contains(cSelectedAsset) && (nIndx = aAssets.IndexOf(cSelectedAsset)) > 0)
			{
				aAssets.Remove(cSelectedAsset);
				aAssets.Insert(nIndx-1, cSelectedAsset);
				_ui_lbClipsSelected.ItemsSource = aAssets;
				ShowDuration();
				_ui_lbClipsSelected.UpdateLayout();
				_ui_lbClipsSelected.SelectedItem = cSelectedAsset;
			}
		}

		private void _ui_btnDelete_Click(object sender, RoutedEventArgs e)
		{
			if (null != _ui_lbClipsSelected.SelectedItem)
			{
				AssetSL cSelectedAsset = (AssetSL)_ui_lbClipsSelected.SelectedItem;
				_ui_lbClipsSelected_Remove(cSelectedAsset);
			}
		}

		private void _ui_btnDown_Click(object sender, RoutedEventArgs e)
		{
			AssetSL cSelectedAsset = (AssetSL)_ui_lbClipsSelected.SelectedItem;
			List<AssetSL> aAssets;
			aAssets = ((IEnumerable<AssetSL>)_ui_lbClipsSelected.ItemsSource).ToList<AssetSL>();
			int nIndx;
			if (aAssets.Contains(cSelectedAsset) && (nIndx = aAssets.IndexOf(cSelectedAsset)) < aAssets.Count - 1)
			{
				aAssets.Remove(cSelectedAsset);
				aAssets.Insert(nIndx + 1, cSelectedAsset);
				_ui_lbClipsSelected.ItemsSource = aAssets;
				ShowDuration();
				_ui_lbClipsSelected.UpdateLayout();
				_ui_lbClipsSelected.SelectedItem = cSelectedAsset;
			}
		}

		private void _ui_lbClipsSelected_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			FrameworkElement FE = (FrameworkElement)((RoutedEventArgs)(e)).OriginalSource;
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
					_ui_lbClipsSelected_Remove(_cAssetForDoubleClick);
				}
			}
		}
		private void _ui_lbClipsSelected_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete && _ui_btnDelete.IsEnabled == true)
			{
				_ui_btnDelete_Click(null, null);
				_ui_lbClipsSelected.Focus();
			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				if (e.Key == Key.Up && _ui_btnUp.IsEnabled == true)
					_ui_btnUp_Click(null, null);
				else if (e.Key == Key.Down && _ui_btnDown.IsEnabled == true)
					_ui_btnDown_Click(null, null);
				_ui_lbClipsSelected.Focus();
			}
		}
		private void _ui_lbClipsSelected_KeyUp(object sender, KeyEventArgs e)
		{
			//if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			//{
			//    _ui_dgAssets.CancelEdit();
			//}
		}

	}
}

