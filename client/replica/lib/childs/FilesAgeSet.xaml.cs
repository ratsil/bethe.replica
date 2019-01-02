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

using controls.childs.sl;
using controls.extensions.sl;
using controls.sl;
using replica.sl;
using replica.sl.ListProviders;
using helpers.replica.services.dbinteract;
using g = globalization;
using helpers.extensions;

namespace controls.childs.replica.sl
{
	public partial class FilesAgeSet : ChildWindow
	{
		private Progress _dlgProgress;
		private MsgBox _cMsgBox;
		private DBInteract _cDBI;
		private Asset _cAssetBase;
        private int? _nAge;
        private bool _bInitialized;
        private FilesAgeSet()
		{
			InitializeComponent();
            _bInitialized = true;
            Title = g.Helper.sFilesAgeSet.ToLower();
        }
		public FilesAgeSet(AssetSL cAsset)
			:this()
		{
			_cDBI = new DBInteract();
            _cAssetBase = AssetSL.GetAsset(cAsset);
			_cMsgBox = new MsgBox();
            _dlgProgress = new Progress();
            _cDBI.FilesAgeGetCompleted += _cDBI_FilesAgeGetCompleted;
            _cDBI.FilesAgeSetCompleted += _cDBI_FilesAgeSetCompleted;
            _cDBI.FilesAgeGetAsync(_cAssetBase);
            _dlgProgress.Show();
        }

        private int nAgeCurrent
        {
            get
            {
                return _ui_nudAge.Value.ToInt() * (_ui_ddlAction.SelectedIndex == 0 ? 1 : -1);
            }
        }


        private void _cDBI_FilesAgeSetCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            _dlgProgress.Close();
            try
            {
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                _cDBI.ErrorLoggingAsync("FilesAgeSet: " + ex.ToString());
                _cMsgBox.ShowError(ex);
            }
        }

        private void _cDBI_FilesAgeGetCompleted(object sender, FilesAgeGetCompletedEventArgs e)
        {
            _dlgProgress.Close();
            try
            {
                _nAge = e.Result;
                if (_nAge != null)
                {
                    _bInitialized = false;
                    if (_nAge >= 0)
                        _ui_ddlAction.SelectedIndex = 0;
                    else
                        _ui_ddlAction.SelectedIndex = 1;
                    _ui_nudAge.Value = _nAge.Value * (_ui_ddlAction.SelectedIndex == 0 ? 1 : -1);
                    _bInitialized = true;
                }
                IsChanged();
            }
            catch (Exception ex)
            {
                _cDBI.ErrorLoggingAsync("FilesAgeSet: " + ex.ToString());
                _cMsgBox.ShowError(ex);
            }
        }
        private void _ui_ddl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsChanged();
        }
        private void _ui_nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            IsChanged();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
		{
            if (IsChanged())
            {
                _cDBI.FilesAgeSetAsync(_cAssetBase, nAgeCurrent);
                _dlgProgress.Show();
            }
            else
                this.DialogResult = false;
		}
		private bool IsChanged()
		{
            if (!_bInitialized)
                return false;
			bool bRetVal = false;
            if (_nAge == nAgeCurrent)
            {
                _ui_ddlAction.Background = Coloring.Notifications.cButtonNormal;
                _ui_spAge.Background = Coloring.Notifications.cTextBoxActive;
                bRetVal = false;
            }
            else
            {
                if (null != _nAge && (_nAge >= 0 && nAgeCurrent >= 0 || _nAge < 0 && nAgeCurrent < 0))
                {
                    _ui_ddlAction.Background = Coloring.Notifications.cButtonNormal;
                }
                else
                {
                    _ui_ddlAction.Background = Coloring.Notifications.cButtonChanged;
                }
                if (null != _nAge && Math.Abs(_nAge.Value) == Math.Abs(nAgeCurrent))
                {
                    _ui_spAge.Background = Coloring.Notifications.cTextBoxActive;
                }
                else
                {
                    _ui_spAge.Background = Coloring.Notifications.cTextBoxChanged;
                }
                bRetVal = true;
            }
            OKButton.IsEnabled = bRetVal;
            if (_ui_nudAge.Value == 0)
            {
                _ui_ddlAction.SelectedIndex = 0;
                _ui_ddlAction.IsEnabled = false;
                _ui_ddlAction.Background = Coloring.Notifications.cButtonInactive;
            }
            else
            {
                _ui_ddlAction.IsEnabled = true;
            }
            return bRetVal;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
		}

    }
}

