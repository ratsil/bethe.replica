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

using helpers.extensions;
using helpers.replica.sl;
using controls.sl;
using controls.childs.sl;
//using controls.replica.sl;
using controls.childs.replica.sl;
using replica.sl;
using helpers.replica.services.dbinteract;
using g = globalization;

namespace controls.replica.sl
{
    public partial class RecalculateFileDuration : UserControl
    {
        private DBInteract _cDBI;
        MsgBox _dlgMsgBox;
        private AssetSL _cAsset;
        private File _cFileFirst;
		private bool _bFrameChanging;
        public string sDefaultFileStorageName;
        public File _cFile;
		public long _nIn, _nOut, _nTotal;
        public delegate void OnFileChanged(File cFile);
        public event OnFileChanged FileChanged;
		public bool bMarkRed
		{
			set
			{
				if (value)
				{
					_ui_spMain.Background = Coloring.Notifications.cTextBoxError;
					_ui_tbFile.Background = Coloring.Notifications.cTextBoxError;
				}
				else
				{
					_ui_spMain.Background = _cMainBGColor;
					_ui_tbFile.Background = _cFileBGColor;
				}
			}
		}
		private Brush _cMainBGColor, _cFileBGColor;
        private System.Windows.Threading.DispatcherTimer _cTimerForCommandResult = null;
        private DateTime dtCommandBegin;
        public RecalculateFileDuration()
        {
            InitializeComponent();

			try
			{
				_cDBI = new DBInteract();
				_cDBI.FileDurationQueryCompleted += new EventHandler<FileDurationQueryCompletedEventArgs>(_cDBI_FileDurationQueryCompleted);
				_cDBI.CommandStatusGetCompleted += new EventHandler<CommandStatusGetCompletedEventArgs>(_cDBI_CommandStatusGetCompleted);
				_cDBI.FramesQtyGetCompleted += new EventHandler<FramesQtyGetCompletedEventArgs>(_cDBI_FramesQtyGetCompleted);
			}
			catch { }
			_cMainBGColor = _ui_spMain.Background;
			_cFileBGColor = _ui_tbFile.Background;
            _ui_tudFrameIn.ValueChanged += new RoutedPropertyChangedEventHandler<DateTime?>(_ui_tudFrameIn_ValueChanged);
            _ui_tudFrameOut.ValueChanged += new RoutedPropertyChangedEventHandler<DateTime?>(_ui_tudFrameOut_ValueChanged);
			_ui_nudExtraFrames.ValueChanged += _ui_nudExtraFrames_ValueChanged;
			_ui_nudExtraFrames.Maximum = Preferences.cServer.nFramesBase - 1;
			_bFrameChanging = false;
            _ui_tbError.Visibility = Visibility.Collapsed;
        }


		private void _ui_tbFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            MarkTB(_ui_tbFile);
        }
        private bool MarkTB(TextBox ui)
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
        private bool MarkTUD(TimeUpDown ui, bool bDurationRecalculate, bool bInclude)
        {
            if (null == ui.Tag)
				return false;
            if (ui.Value.Value.ToFrames(bInclude) != (long)ui.Tag)
				ui.Background = Coloring.Notifications.cTextBoxChanged;
            else
				ui.Background = Coloring.Notifications.cTextBoxActive;
            if (bDurationRecalculate)
                MarkAndRecalculateDuration();
            return true;
        }
		private bool MarkNUD(NumericUpDown ui, StackPanel sp, bool bDurationRecalculate)
		{
			if (null == ui.Tag)
				return false;
			if (DurCalculate() > (long)_ui_tbDuration.Tag)
				sp.Background = Coloring.Notifications.cTextBoxError;
			else if ((long)ui.Value != (long)ui.Tag)
				sp.Background = Coloring.Notifications.cTextBoxChanged;
			else
				sp.Background = Coloring.Notifications.cTextBoxActive;
			if (bDurationRecalculate)
				MarkAndRecalculateDuration();
			return true;
		}
		private long DurCalculate()
		{
			return 1 + _ui_tudFrameOut.Value.Value.ToFrames(false) + (long)_ui_nudExtraFrames.Value - _ui_tudFrameIn.Value.Value.ToFrames(true);
		}
		private bool MarkAndRecalculateDuration()
        {
            //reculc
			long nDuration = DurCalculate();  //c 0:00 по 0:11 = 0:11 секунд!!!!!
			long nDiv = nDuration % Preferences.cServer.nFramesBase;
            string sPostfix = "";
            if (0 != nDiv)
				sPostfix = "." + nDiv.ToString() + ""; // + "frames"
			_ui_tbDuration.Text = nDuration.ToFramesString(false, false, true) + sPostfix;
            //mark
            _ui_tbRemeins.Text = "";
            if (Preferences.cServer.nFramesMinimum > nDuration)
            {
				_ui_tbDuration.Foreground = Coloring.Notifications.cErrorForeground;
				_ui_tudFrameIn.Background = Coloring.Notifications.cTextBoxError;
				_ui_tudFrameOut.Background = Coloring.Notifications.cTextBoxError;
                return false;
			}
			else if (nDuration == (long)_ui_tbDuration.Tag)
			{
				if (Coloring.Notifications.cErrorForeground == _ui_tbDuration.Foreground)
                {
                    MarkTUD(_ui_tudFrameIn, false, true);
                    MarkTUD(_ui_tudFrameOut, false, false);
					MarkNUD(_ui_nudExtraFrames, _ui_spExtraFrames, false);
                }
				_ui_tbDuration.Foreground = Coloring.Notifications.cNormalForeground;
            }
            else
            {
				if (Coloring.Notifications.cErrorForeground == _ui_tbDuration.Foreground)
                {
                    MarkTUD(_ui_tudFrameIn, false, true);
                    MarkTUD(_ui_tudFrameOut, false, false);
					MarkNUD(_ui_nudExtraFrames, _ui_spExtraFrames, false);
				}
				_ui_tbDuration.Foreground = Coloring.Notifications.cChangedForeground;
                _ui_tbRemeins.Text = "(" + g.Helper.sCutted.ToLower() + ": " + ((long)_ui_tbDuration.Tag - nDuration).ToFramesString(true, false, true) + ")";
            }
            return true;
        }
        private void _ui_tudFrameIn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            MarkTUD(_ui_tudFrameIn, true, true);
        }
        private void _ui_tudFrameOut_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            MarkTUD(_ui_tudFrameOut, true, false);
        }
		private void _ui_nudExtraFrames_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_bFrameChanging)
				return;
			_bFrameChanging = true;
			if (DurCalculate() > (long)_ui_tbDuration.Tag)
				_ui_nudExtraFrames.Value = (long)_ui_tbDuration.Tag % Preferences.cServer.nFramesBase;
			MarkNUD(_ui_nudExtraFrames, _ui_spExtraFrames, true);
			_bFrameChanging = false;
		}
		private void _ui_btnDurationReset_Click(object sender, EventArgs e)
		{
			if (null != this.Tag)  // это простой лок
                return;

            this.Tag = true;

            FileDurationUpdate();

            this.Tag = null;
        }
        private void _ui_btnFile_Click(object sender, RoutedEventArgs e)   //было EventArgs e
        {
            Media ui_dlgMedia = new Media();
			if (null != _ui_tbFile.Text && "" != _ui_tbFile.Text)
				ui_dlgMedia.sStorageDefaultSelection = _cFile.cStorage.sName;
			else
				ui_dlgMedia.sStorageDefaultSelection = sDefaultFileStorageName;  // _cAsset.cFile.cStorage.sName;
            ui_dlgMedia.sFilenameToScrollTo = _ui_tbFile.Text;
            //ui_dlgMedia.FileMultiSelect = false;
            ui_dlgMedia.bSingleSelectionMode = true;
            ui_dlgMedia.ButtonOKCaption = g.Common.sSelect1;
            //ui_dlgMedia.
            ui_dlgMedia.Closed += new EventHandler(ui_dlgMedia_Closed);
            ui_dlgMedia.Show();
        }
        private void ui_dlgMedia_Closed(object sender, EventArgs e)
        {
            Media ui_dlgMedia = (Media)sender;
            if ((bool)ui_dlgMedia.DialogResult && 0 < ui_dlgMedia.SelectedFiles.Length)
            {
                _cFile = ui_dlgMedia.SelectedFiles[0];
                FileErrorsShow();
                FileDurationUpdate();
            }
            this.Focus();
        }
        public void FileDurationUpdate()
        {
            _ui_spTimeCodes.Visibility = Visibility.Collapsed;
            _ui_pbCodesProgress.Visibility = Visibility.Visible;
            _ui_btnDurationReset.IsEnabled = false;
            _ui_pbCodesProgress.Width = _ui_spTimeCodes.ActualWidth;
            _ui_spTimeCodes.Tag = _cFile;
            _ui_tbFile.Text = _cFile.sFilename;  // _cFile.cStorage.sPath + 
            FileErrorsShow();
            _cDBI.FileDurationQueryAsync(_cFile.nID);
        }
        private void FileErrorsShow()
        {
            if (_cFile == null || _cFile.eError == Error.no)
                _ui_tbError.Visibility = Visibility.Collapsed;
            else
            {
                _ui_tbError.Visibility = Visibility.Visible;
                if (_cFile.eError == Error.missed)
                    _ui_tbError.Content = g.Common.sErrorNoFile.ToUpper() + "!";
                else if (_cFile.eError == Error.unknown)
                    _ui_tbError.Content = g.Common.sError.ToUpper() + "!";
            }
        }
        void _cDBI_FileDurationQueryCompleted(object sender, FileDurationQueryCompletedEventArgs e)
        {
            _cDBI.CommandStatusGetAsync(e.Result, e.Result);
        }
        void _cDBI_CommandStatusGetCompleted(object sender, CommandStatusGetCompletedEventArgs e)
        {
			try
			{
				long nStatus = 3;
				if (null != e.Result)
					nStatus = e.Result.nID;
				switch (nStatus)
				{
					case 1:
					case 2:
						if (null == _cTimerForCommandResult)
						{
							dtCommandBegin = DateTime.Now;
							_cTimerForCommandResult = new System.Windows.Threading.DispatcherTimer();
							_cTimerForCommandResult.Tick +=
									delegate(object s, EventArgs args)
									{
										_cTimerForCommandResult.Stop();
										_cDBI.CommandStatusGetAsync((long)e.UserState, e.UserState);
									};
							_cTimerForCommandResult.Interval = new System.TimeSpan(0, 0, 0, 0, 500);
						}
						if (DateTime.Now < dtCommandBegin.AddSeconds(10))
						{
							_cTimerForCommandResult.Start();
							return;
						}
						else
						{
                            _dlgMsgBox.ShowError(g.Helper.sErrorGettingFileTimings.ToLower() + "!"); //ch=child 
							_cAsset.nFramesQty = -1;
							ResetToDefault(_cAsset); // это ресет
							break;
						}
					case 3:                     // месага об ошибке + вернуть старый файл
						_dlgMsgBox.ShowError(g.Helper.sErrorGettingFileTimings.ToLower() + "!");   //ch=child 
						_cAsset.nFramesQty = -1;
						ResetToDefault(_cAsset); // это ресет
						break;
					case 4:
						_cDBI.FramesQtyGetAsync((long)e.UserState); // id запроса
						break;
				}
				_cTimerForCommandResult = null;
			}
			catch (Exception ex)
			{
                _dlgMsgBox.ShowError(g.Helper.sErrorGettingFileTimings.ToLower() + "! " + ex.Message);   //ch=child 
			}
        }
		public void ResetToDefault(AssetSL cAsset)
        {
            _ui_pbCodesProgress.Visibility = Visibility.Collapsed;
            if (-1 < cAsset.nID)
            {
                _ui_spTimeCodes.Visibility = Visibility.Visible;
                _ui_btnDurationReset.IsEnabled = true;
            }
            else
            {
                _ui_tbDuration.Text = "";
                _ui_tbRemeins.Text = "";
                _ui_spTimeCodes.Visibility = Visibility.Collapsed;
                _ui_btnDurationReset.IsEnabled = false;
            }
			_cAsset = cAsset;
			_cFile = _cFileFirst = _cAsset.cFile;
            FileErrorsShow();
            _ui_spTimeCodes.Tag = _cAsset.cFile;
			if (null != _cFile && null != _cFile.sFilename)
			{
				_ui_tbFile.Tag = _cFile.sFilename;
				_ui_tbFile.Text = _cFile.sFilename;

				_ui_tudFrameIn.Tag = _cAsset.nFrameIn;
				long nFrames = _cAsset.nFrameOut % Preferences.cServer.nFramesBase;
				_ui_tudFrameOut.Tag = _cAsset.nFrameOut - nFrames;
				_ui_nudExtraFrames.Tag = nFrames;
			}
			if (null == _dlgMsgBox)
				_dlgMsgBox = new MsgBox();
			TimeCodesControlsReset();
		}
        private void TimeCodesControlsReset()
        {
            if (-1 == _cAsset.nID || ( -1 < _cAsset.nID && (null == _cAsset.cFile)))
            {
                _ui_spTimeCodes.Visibility = Visibility.Collapsed;
                _ui_btnDurationReset.IsEnabled = false;
            }
            if (-1 < _cAsset.nID && null != _cAsset.cFile)
			{
				if (0 == _cAsset.nFramesQty)
					_ui_btnDurationReset_Click(null, null);
				else if (-1 == _cAsset.nFramesQty)
					TimeCodesControlsReset(0, -1, -1);
				else
					TimeCodesControlsReset(_cAsset.nFrameIn, _cAsset.nFrameOut, _cAsset.nFramesQty);
            }
        }
		private void TimeCodesControlsReset(long nFramesQty)
        {
            TimeCodesControlsReset(1, nFramesQty, nFramesQty);
        }
		private void TimeCodesControlsReset(long nFrameIn, long nFrameOut, long nFramesQty)
        {
			if (Preferences.cServer.nFramesMinimum - 1 > nFrameOut - nFrameIn)
                return;
			long nFramesOutRemains = nFrameOut % Preferences.cServer.nFramesBase;
            if (0 == nFramesQty)
                nFramesQty = nFrameOut;
            this.Tag = true;
            _ui_tbDuration.Tag = nFramesQty;

            _ui_spTimeCodes.Visibility = Visibility.Visible;

            long nOldIn = _ui_tudFrameIn.Tag == null ? 0 : (long)_ui_tudFrameIn.Tag;
            long nOldOut = _ui_tudFrameOut.Tag == null ? 0 : (long)_ui_tudFrameOut.Tag;
			long nOldFrOut = _ui_nudExtraFrames.Tag == null ? 0 : (long)_ui_nudExtraFrames.Tag;

			_ui_tudFrameIn.Tag = null;  // что б не вызывался MarkTUD;
			_ui_tudFrameIn.Maximum = DateTime.Parse((nFramesQty - Preferences.cServer.nFramesMinimum + 1).ToFramesString(false, true, false, true));
            _ui_tudFrameIn.Minimum = DateTime.Parse("0:00:00");
            _ui_tudFrameIn.Value = DateTime.Parse(nFrameIn.ToFramesString(false, true, false, true));

			if (null != _ui_tbFile.Tag && _ui_tbFile.Tag.ToString() != _ui_tbFile.Text)    
			{
				_ui_tudFrameIn.Tag = nFrameIn;
				_ui_tudFrameOut.Tag = nFrameOut - nFramesOutRemains;
				_ui_nudExtraFrames.Tag = nFramesOutRemains;
			}
			else  // если файл не менялся, то тэги кодов тоже д.б. старые
			{
				_ui_tudFrameIn.Tag = nOldIn;
				_ui_tudFrameOut.Tag = nOldOut;
				_ui_nudExtraFrames.Tag = nOldFrOut;
			}

			if (null != _ui_tbFile.Tag && _ui_tbFile.Tag.ToString() == _ui_tbFile.Text)
			{
				_ui_spMain.Background = _cMainBGColor;
				_ui_tbFile.Background = Coloring.Notifications.cTextBoxActive;
			}
			else if (null==_ui_tbFile.Tag)
			{
				_ui_spMain.Background = Coloring.Notifications.cTextBoxActive;
				_ui_tbFile.Background = Coloring.Notifications.cTextBoxActive;
			}
			else
			{
				_ui_spMain.Background = Coloring.Notifications.cTextBoxChanged;
				_ui_tbFile.Background = Coloring.Notifications.cTextBoxChanged;
			}



			_ui_tudFrameOut.Maximum = DateTime.Parse(nFrameOut.ToFramesString(false, false, false, true));
			_ui_tudFrameOut.Minimum = DateTime.Parse((((DateTime)_ui_tudFrameIn.Minimum).ToFrames(false) + Preferences.cServer.nFramesMinimum).ToFramesString(false, false, false, true));
			_ui_tudFrameOut.Value = _ui_tudFrameOut.Maximum;
			if (_ui_nudExtraFrames.Value != nFramesOutRemains)
				_ui_nudExtraFrames.Value = nFramesOutRemains;
			this.Tag = null;
		}
		void _cDBI_FramesQtyGetCompleted(object sender, FramesQtyGetCompletedEventArgs e)
        {
            long nFramesQty = e.Result;
            _ui_btnDurationReset.IsEnabled = true;
            _ui_pbCodesProgress.Visibility = Visibility.Collapsed;
            _ui_spTimeCodes.Visibility = Visibility.Visible;
            TimeCodesControlsReset(nFramesQty);

            if (null != FileChanged)
                FileChanged(_cFile);
        }
        public void IsInputCorrect()
        {
            if (Visibility.Visible == _ui_pbCodesProgress.Visibility)
                throw new Exception(g.Replica.sErrorRecalculate2);
            if (Visibility.Collapsed == _ui_spTimeCodes.Visibility)
                throw new Exception(g.Replica.sErrorRecalculate3);
			if (Coloring.Notifications.cTextBoxError == _ui_tbFile.Background ||
				Coloring.Notifications.cTextBoxError == _ui_tudFrameIn.Background ||
				Coloring.Notifications.cTextBoxError == _ui_tudFrameOut.Background ||
				_ui_spExtraFrames.Background == Coloring.Notifications.cTextBoxError)
                throw new Exception(g.Common.sErrorWrongFields + "!");
			_nIn = ((DateTime)_ui_tudFrameIn.Value).ToFrames(true);
			_nOut = ((DateTime)_ui_tudFrameOut.Value).ToFrames(false) + (long)_ui_nudExtraFrames.Value;
			_nTotal = (long)_ui_tbDuration.Tag;
        }
    }
}
