using System.Windows.Controls;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System;

using scr.services.ingenie.prompter;
using helpers.sl;
using helpers.extensions;
using controls.childs.sl;
using controls.sl;
using controls.replica.sl;

using g = globalization;

namespace scr
{
	public partial class prompter : Page
	{
		private FontFamily _cFontFamily = null;
		private PrompterInteract _cPrompter;
		private ulong _nCompID;
		private Progress _dlgProgress;
		private MsgBox _dlgMsgBox;
		private System.Windows.Threading.DispatcherTimer _cTimerForStatusGet;
		private int _nPromptersHash;
		private int _OnScreenLines;
		private int _OffScreenLines;
		private List<SplittedLine> _aSplittedLines;
		private DateTime _dtNextMouseClickForDoubleClick;
		private SplittedLine _cSLForDoubleClick;
		private bool _bInitialized;

		public class SplittedLine
		{
			public int nLineNumber { get; set; }
			public string sLine { get; set; }
			static public List<SplittedLine> SplittedGet(string[] aSplittedStrings)
			{
				List<SplittedLine> aRetVal = new List<SplittedLine>();
				int nI=1;
				foreach (string sS in aSplittedStrings)
					aRetVal.Add(new SplittedLine() { nLineNumber = nI++, sLine = sS });
				return aRetVal;
			}
		}

		public prompter()
		{
			InitializeComponent();
            Title = g.SCR.sNoticePrompter0;

			_cFontFamily = new FontFamily("Verdana");
			_dlgProgress = new Progress();
			_dlgProgress.Show();
			_dlgMsgBox = new MsgBox();
			_nPromptersHash = -1;
			_OnScreenLines = 0;
			_OffScreenLines = 0;
			_aSplittedLines = null;

			_cPrompter = new PrompterInteract();
			_cPrompter.InitCompleted += new EventHandler<InitCompletedEventArgs>(_cPrompter_InitCompleted);
			_cPrompter.PrepareCompleted += new System.EventHandler<PrepareCompletedEventArgs>(_cPrompter_PrepareCompleted);
			_cPrompter.StartCompleted += new System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cPrompter_StartCompleted);
			_cPrompter.StopCompleted += new System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cPrompter_StopCompleted);
			_cPrompter.OnOffScreenGetCompleted += new EventHandler<OnOffScreenGetCompletedEventArgs>(_cIG_OnOffScreenGetCompleted);
			_cPrompter.RollSpeedSetCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cPrompter_RollSpeedSetCompleted);
			_cPrompter.RestartFromCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cPrompter_RestartFromCompleted);

			string sCompID = common.CookieGet("this_comp_id");
			if (null == sCompID || "" == sCompID)
			{
				_nCompID = DateTime.Now.Subtract(new DateTime(2011, 1, 1)).TotalMilliseconds.ToULong();
				common.CookieSet("this_comp_id", _nCompID.ToString());
			}
			else
				_nCompID = ulong.Parse(sCompID);
			
			_ui_nudSpeed.Tag = _ui_nudSpeed.Value;

			_cPrompter.InitAsync(_nCompID);

			_ui_ctrTB_Prompter.sFile = @"d:\cues\air\orderdesk\bumper.xml";
			_ui_ctrTB_Prompter.TemplatePrepare += _ui_ctrTB_Prompter_TemplatePrepare;
			_ui_ctrTB_Prompter.TemplateStart += _ui_ctrTB_Prompter_TemplateStart;
			_ui_ctrTB_Prompter.TemplateStop += _ui_ctrTB_Prompter_TemplateStop;
			_ui_ctrTB_Prompter.bIsFirstActionChangebleByRightMouseClick = false;

			_ui_lblFontName.Content = _cFontFamily;
			_cTimerForStatusGet = new System.Windows.Threading.DispatcherTimer();
			_cTimerForStatusGet.Tick += new EventHandler(StatusGetting);
			_cTimerForStatusGet.Interval = new TimeSpan(0, 0, 0, 0, 300);  // период проверки статуса темплейта

			App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
			_ui_dgTextPrepared.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgAssets_MouseLeftButtonDown), true);
			_ui_dgTextPrepared.LoadingRow += new EventHandler<DataGridRowEventArgs>(_ui_dgTextPrepared_LoadingRow);
			_ui_btnPause.IsEnabled = false;
			_ui_dgTextPrepared.IsEnabled = false;
			_ui_ctrTB_Prompter.IsEnabled = false;
			_ui_dgTextPrepared.Visibility = Visibility.Collapsed;
			_ui_tbTextPreView.Visibility = Visibility.Visible;
			_bInitialized = true;
		}

		void _ui_ctrTB_Prompter_TemplateStop(object sender, EventArgs e)
		{
			_ui_btnStop_Click(null, null);
		}
		void _ui_ctrTB_Prompter_TemplateStart(object sender, EventArgs e)
		{
			_ui_btnStart_Click(null, null);
		}
		void _ui_ctrTB_Prompter_TemplatePrepare(object sender, EventArgs e)
		{
			_ui_btnPrepare_Click(null, null);
		}


		void _ui_dgTextPrepared_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			SplittedLine cSL = (SplittedLine)e.Row.DataContext;
			if (_OffScreenLines < cSL.nLineNumber && _OnScreenLines >= cSL.nLineNumber)
			{
				e.Row.Background = Coloring.Prompter.cPreparedOnScreenBackgr;
				e.Row.Foreground = Coloring.Prompter.cPreparedOnScreenForegr;
			}
			else
			{
				e.Row.Background = Coloring.Prompter.cPreparedOffScreenBackgr;
				e.Row.Foreground = Coloring.Prompter.cPreparedOffScreenForegr;
			}
		}
		private void BrowserWindow_Resized(object sender, EventArgs e)
		{
			//_ui_tcAssets.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
			//double nH = _ui_spText.ActualHeight - 208;
			double nH = App.Current.RootVisual.RenderSize.Height - 260;
			nH = nH > 150 ? nH : 150;
			_ui_dgTextPrepared.Height = nH;
			_ui_tbTextPreView.Height = nH;
		}
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (Visibility.Collapsed == this.Visibility)
				return;
			double nH = App.Current.RootVisual.RenderSize.Height - 260;
			nH = nH > 150 ? nH : 150;
			_ui_dgTextPrepared.Height = nH;
			_ui_tbTextPreView.Height = nH;
		}

		#region prompter
		void _cPrompter_InitCompleted(object sender, InitCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null != e.Error)
						_dlgMsgBox.ShowError(e.Error.Message);
				}
				else
					_dlgMsgBox.ShowError(g.Common.sErrorCurrentRequestWasCanceled);
			}
			catch { }
			_dlgProgress.Close();
		}
		void _cPrompter_PrepareCompleted(object sender, PrepareCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null == e.Error)
					{
						_ui_dgTextPrepared.IsEnabled = true;
						_ui_ctrTB_Prompter.eStatus = TemplateButton.Status.Prepared;
						if (0 < e.Result.nTemplatesHashCode && null != e.Result.aSplittedText)
						{
							_ui_dgTextPrepared.ItemsSource = _aSplittedLines = SplittedLine.SplittedGet(e.Result.aSplittedText);
							_nPromptersHash = e.Result.nTemplatesHashCode;
							_ui_dgTextPrepared.Visibility = Visibility.Visible;
							_ui_tbTextPreView.Visibility = Visibility.Collapsed;
						}
					}
					else
						_dlgMsgBox.ShowError(e.Error.Message);
				}
				else
                    _dlgMsgBox.ShowError(g.Common.sErrorCurrentRequestWasCanceled);
			}
			catch { }
		}
		void _cPrompter_StartCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null == e.Error)
					{
						_ui_btnPause.IsEnabled = true;
						_ui_nudSpeed.IsEnabled = true;
						_ui_ctrTB_Prompter.eStatus = TemplateButton.Status.Started;
					}
					else
						_dlgMsgBox.ShowError(e.Error.Message);
				}
				else
                    _dlgMsgBox.ShowError(g.Common.sErrorCurrentRequestWasCanceled);
			}
			catch { }
			_cTimerForStatusGet.Start();
		}
		void _cPrompter_StopCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null == e.Error)
					{
						_ui_btnPause.IsEnabled = false;
						_ui_nudSpeed.IsEnabled = true;
						_ui_ctrTB_Prompter.eStatus = TemplateButton.Status.Stopped;
						_OnScreenLines = _OffScreenLines = 0;
						_ui_dgTextPrepared.ItemsSource = null;
					}
					else
						_dlgMsgBox.ShowError(e.Error.Message);
				}
				else
                    _dlgMsgBox.ShowError(g.Common.sErrorCurrentRequestWasCanceled);
			}
			catch { }
			_cTimerForStatusGet.Stop();
		}
		void _cPrompter_RestartFromCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null == e.Error)
					{
						_ui_nudSpeed.IsEnabled = true;
						_ui_dgTextPrepared.IsEnabled = true;
						_ui_ctrTB_Prompter.eStatus = TemplateButton.Status.Started;
					}
					else
						_dlgMsgBox.ShowError(e.Error.Message);
				}
				else
                    _dlgMsgBox.ShowError(g.Common.sErrorCurrentRequestWasCanceled);
			}
			catch { }
		}
		void _cPrompter_RollSpeedSetCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				if (!e.Cancelled)
				{
					if (null == e.Error)
					{
						_ui_nudSpeed.Value = (short)_ui_nudSpeed.Tag;

					}
					else
					{
						_ui_nudSpeed.Tag = _ui_nudSpeed.Value;
						_dlgMsgBox.ShowError(e.Error.Message);
					}
				}
				else
                    _dlgMsgBox.ShowError(g.Common.sErrorCurrentRequestWasCanceled);
			}
			catch { }
		}
		#endregion

		#region ui
		private void _ui_btnPrepare_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			_ui_nudFontSize.IsEnabled = false;
			_ui_tbText.IsEnabled = false;
			_ui_tbTextPreView.IsEnabled = false;
			_ui_nudSpeed.IsEnabled = false;
			DynamicValue[] aDynamicValues = new DynamicValue[3];
			aDynamicValues[0] = new DynamicValue();
			aDynamicValues[0].sName = "{%RUNTIME::USER::TEXT%}";
			_ui_tbText.SelectAll();
			aDynamicValues[0].sValue = _ui_tbText.Selection.Text;

			aDynamicValues[1] = new DynamicValue();
			aDynamicValues[1].sName = "{%RUNTIME::USER::FONTSIZE%}";
			aDynamicValues[1].sValue = _ui_nudFontSize.Value.ToString();

			aDynamicValues[2] = new DynamicValue();
			aDynamicValues[2].sName = "{%RUNTIME::USER::SPEED%}";
			aDynamicValues[2].sValue = _ui_nudSpeed.Value.ToString();

			_cPrompter.PrepareAsync("prompter", aDynamicValues);
		}
		private void _ui_btnStart_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			_cPrompter.StartAsync(_nPromptersHash);
		}
		private void _ui_btnPause_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (_ui_nudSpeed.IsEnabled)
			{
				_ui_nudSpeed.IsEnabled = false;
				_ui_btnPause.Background = Coloring.Prompter.cPreparedPauseOnBackgr;
				_cPrompter.RollSpeedSetAsync(0, _nPromptersHash);
			}
			else
			{
				_ui_nudSpeed.IsEnabled = true;
				_ui_btnPause.Background = Coloring.Prompter.cPreparedPauseOffBackgr;
				_cPrompter.RollSpeedSetAsync((short)_ui_nudSpeed.Value, _nPromptersHash);
			}
		}
		private void _ui_btnStop_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			_cTimerForStatusGet.Stop();
			_ui_dgTextPrepared.Visibility = Visibility.Collapsed;
			_ui_tbTextPreView.Visibility = Visibility.Visible;
			_ui_nudFontSize.IsEnabled = true;
			_ui_tbText.IsEnabled = true;
			_ui_tbTextPreView.IsEnabled = true;
			_ui_nudSpeed.IsEnabled = true;
			_cPrompter.StopAsync(_nPromptersHash);
		}
		private void _ui_nudFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			PreviewText();
		}
		private void _ui_tbText_ContentChanged(object sender, ContentChangedEventArgs e)
		{
			if ("" == PreviewText())
				_ui_ctrTB_Prompter.IsEnabled = false;
			else
				_ui_ctrTB_Prompter.IsEnabled = true;
		}
		void _ui_dgAssets_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (_dtNextMouseClickForDoubleClick < DateTime.Now)
			{
				_dtNextMouseClickForDoubleClick = DateTime.Now.AddMilliseconds(400);
				_cSLForDoubleClick = (SplittedLine)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
			}
			else
			{
				if (_cSLForDoubleClick == (SplittedLine)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext)
				{
					_ui_dgTextPrepared.IsEnabled = false;
					_ui_nudSpeed.IsEnabled = false;
					_ui_ctrTB_Prompter.eStatus = TemplateButton.Status.Preparing;
					_cPrompter.RestartFromAsync(_cSLForDoubleClick.nLineNumber, _nPromptersHash);
				}
				_dtNextMouseClickForDoubleClick = DateTime.MinValue;
			}
		}
		private void _ui_nudSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_bInitialized && _ui_dgTextPrepared.Visibility == System.Windows.Visibility.Visible && _ui_ctrTB_Prompter.eStatus == TemplateButton.Status.Started)
				_cPrompter.RollSpeedSetAsync((short)_ui_nudSpeed.Value, _nPromptersHash);
		}
		#endregion

		#region status
		void StatusGetting(object s, EventArgs args)
		{
			if (0 < _nPromptersHash)
			{
				_cTimerForStatusGet.Stop();
				_cPrompter.OnOffScreenGetAsync(_nPromptersHash);
			}
		}
		void _cIG_OnOffScreenGetCompleted(object sender, OnOffScreenGetCompletedEventArgs e)
		{
			if (null != e && null != e.Result)
			{ 
				bool bChanged=false;
				if (_OnScreenLines != e.Result[0])
				{
					bChanged = true;
					_OnScreenLines = e.Result[0];
				}
				if (_OffScreenLines != e.Result[1])
				{
					bChanged = true;
					_OffScreenLines = e.Result[1];
				}
				if (_OnScreenLines == _OffScreenLines && _OnScreenLines == _aSplittedLines.Count)
				{
					_ui_btnStop_Click(null, null);
					return;
				}
				if (bChanged && _dtNextMouseClickForDoubleClick < DateTime.Now)
				{
					_ui_dgTextPrepared.ItemsSource = null;
					_ui_dgTextPrepared.ItemsSource = _aSplittedLines;
					int nToScroll = _OnScreenLines + 2 <= _aSplittedLines.Count ? _OnScreenLines + 2 : _aSplittedLines.Count;
					if (_aSplittedLines.Count >= nToScroll)
						try
						{
							_ui_dgTextPrepared.Dispatcher.BeginInvoke(delegate
							{
								//_ui_dgTextPrepared.Focus();
								_ui_dgTextPrepared.SelectedItem = _aSplittedLines[nToScroll - 1];
								_ui_dgTextPrepared.CurrentColumn = _ui_dgTextPrepared.Columns[0];
								_ui_dgTextPrepared.UpdateLayout();
								Dispatcher.BeginInvoke(() => _ui_dgTextPrepared.ScrollIntoView(_aSplittedLines[nToScroll - 1], null));
							});

							//_ui_dgTextPrepared.ScrollIntoView(_aSplittedLines[nToScroll - 1], _ui_dgTextPrepared.Columns[0]);
						}
						catch { }
				}
			}
			if (_ui_dgTextPrepared.Visibility == Visibility.Visible)
				_cTimerForStatusGet.Start();
		}
		#endregion
		private string PreviewText()
		{
			string sText = "";
			if (_bInitialized)
			{
				if (_ui_tbTextPreView.FontFamily != _cFontFamily)
					_ui_tbTextPreView.FontFamily = _cFontFamily;
				if (_ui_tbTextPreView.FontSize != _ui_nudFontSize.Value)
					_ui_tbTextPreView.FontSize = _ui_nudFontSize.Value;
				try
				{
					foreach (Block cB in _ui_tbText.Blocks)
						if (0 < (cB as Paragraph).Inlines.Count)
							sText += ((cB as Paragraph).Inlines[0] as Run).Text + Environment.NewLine;
					_ui_tbTextPreView.SelectAll();
					_ui_tbTextPreView.Selection.Text = sText;
				}
				catch { }

				//_ui_tbTextPreView.Blocks.Clear();
				//_ui_tbTextPreView.Blocks.Add(_ui_tbText.Blocks[0]);
			}
			return sText;
		}




	}
}
