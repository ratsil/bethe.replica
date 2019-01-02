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
using controls.sl;
using g=globalization;

namespace controls.replica.sl
{
	public partial class TemplateButton : UserControl
    {
		#region dependency properties
		static public readonly DependencyProperty sTextProperty = DependencyProperty.Register("sText", typeof(string), typeof(TemplateButton), new PropertyMetadata(OnTextChanged));
		static public readonly DependencyProperty sFileProperty = DependencyProperty.Register("sFile", typeof(string), typeof(TemplateButton), new PropertyMetadata(OnFileChanged));
		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((TemplateButton)d).ProcessText();
		}
		private static void OnFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((TemplateButton)d).ProcessFile();
		}
		#endregion

		public enum FirstAction
        { 
            Prepare,
            Start
        }
        public enum Status
        {
            Unknown = 0,
            Idle = 1,
            Prepared = 2,
            Started = 3,
            Stopped = 4,
            Error = -1,
            Preparing = 5  // чисто локальный статус, чтобы резко менялся с Idle
        }
		public class Item
		{
			static protected List<Item> aItems = new List<Item>();
			public ulong nID;
			public string sPreset;
			public string sInfo;
			public TemplateButton.Status eStatusPrevious;
			public TemplateButton.Status eStatus;
		}
		
		public event EventHandler TemplatePrepare;
		public event EventHandler TemplateStart;
		public event EventHandler TemplateStop;
		public event EventHandler TemplateSkip;
		public event EventHandler TemplateStopped;
		public event EventHandler TemplateStarted;
		public event EventHandler TemplateDrop;

		public string sText
		{
			get
			{
				return (string)GetValue(sTextProperty);
			}
			set
			{
				SetValue(sTextProperty, value);
			}
		}
		public string sFile
		{
			get
			{
				return (string)GetValue(sFileProperty);
			}
			set
			{
				SetValue(sFileProperty, value);
			}
		}

        private System.Windows.Threading.DispatcherTimer _cTimerForStartedPause;
        private System.Windows.Threading.DispatcherTimer _cTimerForStoppedPause;
        private System.Windows.Threading.DispatcherTimer _cTimerForErrorPause;
        private System.Windows.Threading.DispatcherTimer _cTimerForSkipErrorPause;
        private bool _bSkipResult;
        private FirstAction _eFirstAction;
        private string sFirstText;
        private Status _eStatus;

		public Item cItem { get; set; }
		public Item cItemPrevious { get; set; }
        public bool bTemplateButtonIsBusy = false;
		private bool bTemplateButtonIsInProcess = false;
        public bool bPressedByUser = false;
		public bool bIsFirstActionChangebleByRightMouseClick = true;  //TODO это нечитабельно
		public TemplateButton cButtonToActivateOnEvents;

        public void BreakStopTimer()
        {
            if (_cTimerForStoppedPause.IsEnabled || _cTimerForErrorPause.IsEnabled)
            {
                _cTimerForStoppedPause.Stop();
                _cTimerForErrorPause.Stop();
                StoppedEnd(null, null);
            }
        }
        public Button btnPlay
        {
			get
			{
				return _ui_btnPlay;
			}
		}
        public bool bSkipResult
        {
            get
			{
				return _bSkipResult;
			}
            set
            {
                _bSkipResult = value;
                if (value)
                {
                    SkipErrorEnd(null, null);
                }
                else
                {
                    ((TextBlock)_ui_btnSkip.Content).Text = "ERROR";
					_ui_btnSkip.Background = Coloring.Notifications.cButtonError;
                    _cTimerForSkipErrorPause.Start();
                }
            }
        }
        public Status eStatus
        {
			get
			{
				return _eStatus;
			}
			set 
            {
                _eStatus = value;
                switch (_eStatus)
                {
                    case Status.Idle:
                        StoppedEnd(null, null);
                        break;
                    case Status.Prepared:
                        if (!bPressedByUser || _eFirstAction == FirstAction.Start)
                        {
                            _ui_btnTemplate.Content = sText + "STARTING";
                            ((TextBlock)_ui_btnPlay.Content).Text = "STARTING";
                            if (null != TemplateStart)
                                TemplateStart(this, null);
                        }
                        else
                        {
                            _ui_btnTemplate.Content = sText + "START";
                            ((TextBlock)_ui_btnPlay.Content).Text = "PLAY";
                            _ui_btnTemplate.IsEnabled = _ui_btnPlay.IsEnabled = true;
							bTemplateButtonIsInProcess = false;
                        }
                        break;
                    case Status.Started:
                        _ui_btnTemplate.Content = sText + "STARTED";
                        ((TextBlock)_ui_btnPlay.Content).Text = "STARTED";
                        _ui_btnTemplate.IsEnabled = _ui_btnPlay.IsEnabled = false;
                        _cTimerForStartedPause.Start();
                        break;
                    case Status.Stopped:
                        _ui_btnTemplate.Content = sText + "STOPPED";
                        _ui_btnTemplate.IsEnabled = false;
                        ((TextBlock)_ui_btnStop.Content).Text = "STOPPED";
                        _cTimerForStoppedPause.Start();
                        break;
                    case Status.Error:
                        _ui_btnTemplate.Content = sText + "ERROR";
						_ui_btnPlay.Background = _ui_btnStop.Background = _ui_btnTemplate.Background = Coloring.Notifications.cButtonError;
						_ui_btnPlay.IsEnabled = _ui_btnStop.IsEnabled = _ui_btnTemplate.IsEnabled = false;
                        ((TextBlock)_ui_btnStop.Content).Text = "ERROR";
                        ((TextBlock)_ui_btnPlay.Content).Text = "ERROR";
                        _cTimerForErrorPause.Start();
                        if (null != TemplateStopped)
							TemplateStopped(this, null);
                        break;
                    default:
                        break;
                }
            }
        }
        public FirstAction eFirstAction
        {
            get
            {
                return _eFirstAction;
            }
            set
            {
                if (_eStatus == Status.Idle)
                {
                    _eFirstAction = value;
                    switch (value)
                    {
                        case FirstAction.Prepare:
                            sFirstText = "PREPARE";
                            break;
                        case FirstAction.Start:
							if ("3" == sText) //EMERGENCY:l Валя, что это?!?!?!
                                sFirstText = "PLAY";
                            else
                                sFirstText = "START";
                            break;
                        default:
                            break;
                    }
                }
            }
        }
		public double nWidth
		{
			get
			{
				return _ui_btnTemplate.Width;
			}
			set
			{
				_ui_btnTemplate.Width = value;
			}
		}
        public double nHeight
		{
			get
			{
				return _ui_btnTemplate.Height;
			}
			set
			{
				_ui_btnTemplate.Height = value;
			}
		}
        public bool bSkipBtnIsEnabled
        {
            get
            {
                return _ui_btnSkip.IsEnabled;
            }
            set
            {
                _ui_btnSkip.IsEnabled = value;
            }
        }
		public string sLog { get; set; }

        public TemplateButton()
        {
            InitializeComponent();
            _cTimerForStartedPause = new System.Windows.Threading.DispatcherTimer();
            _cTimerForStartedPause.Tick += new EventHandler(StartedEnd);
            _cTimerForStartedPause.Interval = new TimeSpan(0, 0, 0, 0, 2000);
            _cTimerForStoppedPause = new System.Windows.Threading.DispatcherTimer();
            _cTimerForStoppedPause.Tick += new EventHandler(StoppedEnd);
            _cTimerForStoppedPause.Interval = new TimeSpan(0, 0, 0, 0, 2000);
            _cTimerForErrorPause = new System.Windows.Threading.DispatcherTimer();
            _cTimerForErrorPause.Tick += new EventHandler(StoppedEnd);
            _cTimerForErrorPause.Interval = new TimeSpan(0, 0, 0, 0, 7000);
            _cTimerForSkipErrorPause = new System.Windows.Threading.DispatcherTimer();
            _cTimerForSkipErrorPause.Tick += new EventHandler(SkipErrorEnd);
            _cTimerForSkipErrorPause.Interval = new TimeSpan(0, 0, 0, 0, 7000);
            _ui_btnTemplate.FontSize = 11;
            _eStatus = Status.Idle;
			sLog = "";
        }

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
		public void Refresh()
		{
			_ui_btnTemplate.Content = sText + sFirstText;
		}
		private void ProcessText()
		{
		}
		private void ProcessFile()
		{
		}

		private void btnTemplate_Click()
        {
			if (0 < sLog.Length)
				helpers.sl.common.CookieSet("logTB", sLog += "_ui_btnTemplate_Click: begin |");

			else if (bTemplateButtonIsInProcess)
			{
				if (0 < sLog.Length)
					helpers.sl.common.CookieSet("logTB", sLog += " end1 |");
				return;
			}
			if (0 < sLog.Length)
				helpers.sl.common.CookieSet("logTB", sLog += " switch: " + _eStatus + " |");

			switch (_eStatus)
			{
				case Status.Idle:
					Preparing();
					if (null != TemplatePrepare) 
						TemplatePrepare(this, null);
					break;
				case Status.Prepared:
					bTemplateButtonIsInProcess = true;
					_ui_btnTemplate.Content = sText + "STARTING";
					((TextBlock)_ui_btnPlay.Content).Text = "STARTING";
					_ui_btnPlay.IsEnabled = _ui_btnTemplate.IsEnabled = false;
					if (null != TemplateStart)
						TemplateStart(this, null);
					break;
				case Status.Started:
					Stopping();
					if (null != TemplateStop) TemplateStop(this, null);
					break;
				case Status.Stopped:
					break;
				case Status.Error:
					break;
				default:
					break;
			}
			if (0 < sLog.Length)
				helpers.sl.common.CookieSet("logTB", sLog += "_ui_btnTemplate_Click: end2 |");
		}
		private void SkipErrorEnd(object s, EventArgs args)
        {
            ((TextBlock)_ui_btnSkip.Content).Text = "SKIP";
            _ui_btnSkip.IsEnabled = true;
			_ui_btnSkip.Background = Coloring.Notifications.cTextBoxActive;
            _cTimerForSkipErrorPause.Stop();
        }
        private void StartedEnd(object s, EventArgs args)
        {
            _ui_btnTemplate.Content = sText + "STOP";
            _ui_btnTemplate.Background = Coloring.Notifications.cButtonError;
            _ui_btnSkip.IsEnabled = _ui_btnStop.IsEnabled = _ui_btnTemplate.IsEnabled = true;
            _ui_btnPlay.IsEnabled = false;
            ((TextBlock)_ui_btnPlay.Content).Text = "STARTED";
            _ui_btnPlay.Background = Coloring.Notifications.cButtonChanged;
            _cTimerForStartedPause.Stop();
			bTemplateButtonIsInProcess = false;
			if (null != TemplateStarted)
				TemplateStarted(this, null);
        }
        private void StoppedEnd(object s, EventArgs args)
        {
            _ui_btnTemplate.Content = sText + sFirstText; // "START"
            _ui_btnPlay.Background = _ui_btnTemplate.Background = Coloring.Notifications.cButtonNormal;
            _ui_btnSkip.Background = _ui_btnStop.Background = Coloring.Notifications.cButtonNormal;
            _ui_btnTemplate.IsEnabled = true;
            bPressedByUser = false;
            _ui_btnSkip.IsEnabled = _ui_btnStop.IsEnabled = false;
            ((TextBlock)_ui_btnStop.Content).Text = "STOP";
            ((TextBlock)_ui_btnPlay.Content).Text = sFirstText; // "PLAY"
            ((TextBlock)_ui_btnSkip.Content).Text = "SKIP";
            _cTimerForStoppedPause.Stop();
            _cTimerForErrorPause.Stop();
            _eStatus = Status.Idle;
			bTemplateButtonIsBusy = bTemplateButtonIsInProcess = false;
			if (null != s && null != TemplateStopped)  // т.е. если по таймеру
				TemplateStopped(this, null);
        }
		public void BreakStopedTimer()
		{
			if (_cTimerForStoppedPause.IsEnabled || _cTimerForErrorPause.IsEnabled)
			{
				_cTimerForStoppedPause.Stop();
				_cTimerForErrorPause.Stop();
				StoppedEnd(null, null);
			}
		}
		private void Preparing()
        {
			bTemplateButtonIsBusy = bTemplateButtonIsInProcess = true;
			if (0 < sLog.Length)
				helpers.sl.common.CookieSet("logTB", sLog += " Preparing: |");
            eStatus = Status.Preparing;
            _ui_btnTemplate.Content = sText + "PREPARING";
            _ui_btnPlay.Background = Coloring.Notifications.cButtonNormal;
            _ui_btnTemplate.Background = Coloring.Notifications.cButtonChanged;
            _ui_btnPlay.IsEnabled = _ui_btnTemplate.IsEnabled = false;
            ((TextBlock)_ui_btnPlay.Content).Text = "PREPARING";
        }
        private void Stopping()
        {
			bTemplateButtonIsInProcess = true;
            _ui_btnTemplate.Content = sText + "STOPPING";
            _ui_btnStop.Background = _ui_btnTemplate.Background = Coloring.Notifications.cButtonChanged;
            _ui_btnStop.IsEnabled = _ui_btnTemplate.IsEnabled = false;
            ((TextBlock)_ui_btnStop.Content).Text = "STOPPING";
        }

        private void _ui_btnTemplate_Click(object sender, RoutedEventArgs e)
        {
            bPressedByUser = true;
			btnTemplate_Click();
		}
        private void _ui_btnTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            nWidth = this.Width;
            nHeight = this.Height;
            if ("3" == sText) //EMERGENCY:l Валя, что это?!?!?!
                _ui_btnTemplate.Visibility = Visibility.Collapsed;
            else
                _ui_sp3Buttons.Visibility = Visibility.Collapsed;

            if (_eStatus == Status.Started && (sText + "STOPPING") != (string)_ui_btnTemplate.Content)  // перейти на куки и убрать
            {
                eStatus = Status.Started;
                return;
            }

			eFirstAction = _eFirstAction;
			eStatus = Status.Idle;
		}
        private void _ui_btnPlay_Click(object sender, RoutedEventArgs e)
        {
			if ("Player Playlist" == sFile)
				helpers.sl.common.CookieSet("logTB", sLog = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]") + " _ui_btnPlay_Click: begin |");
			else
				sLog = "";
			_ui_btnTemplate_Click(sender, e);
			if (0 < sLog.Length)
				helpers.sl.common.CookieSet("logTB", sLog += "_ui_btnPlay_Click: end |");
		}
        private void _ui_btnSkip_Click(object sender, RoutedEventArgs e)
        {
            if (null != sender && null != e)
                bPressedByUser = true;
			TemplateSkip(this, null);
            ((TextBlock)_ui_btnSkip.Content).Text = "SKIPPING";
            _ui_btnSkip.Background = Coloring.Notifications.cButtonChanged;
            _ui_btnSkip.IsEnabled = false;
        }
        private void _ui_btnStop_Click(object sender, RoutedEventArgs e)
        {
			_ui_btnTemplate_Click(sender, e);
        }
        private void LayoutRoot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MsgBox dlgMsgBox;
            if (_eStatus == Status.Idle && !bTemplateButtonIsBusy && bIsFirstActionChangebleByRightMouseClick)
            {
                switch (_eFirstAction)
                {
                    case FirstAction.Prepare:
                        eFirstAction = FirstAction.Start;
                        break;
                    case FirstAction.Start:
                        eFirstAction = FirstAction.Prepare;
                        break;
                }
                eStatus = Status.Idle;
            }
            else if (_eStatus == Status.Prepared)
            {
                dlgMsgBox = new MsgBox();
                dlgMsgBox.Closed += new EventHandler(dlgMsgBox_Closed);
                dlgMsgBox.ShowQuestion(g.Helper.sNoticeTemplateButton1);
            }
        }
        private void dlgMsgBox_Closed(object sender, EventArgs e)
        {
            if (((MsgBox)sender).enMsgResult == MsgBox.MsgBoxButton.OK && TemplateDrop != null)
				TemplateDrop(this, null);
        }

        public void Click()
        {
			bPressedByUser = false;
			btnTemplate_Click();
		}
		public void ClickByUser()
		{
			bPressedByUser = true;
			btnTemplate_Click();
		}
		public void SkippingCancel()
        {
            ((TextBlock)_ui_btnSkip.Content).Text = "SKIP";
            _ui_btnSkip.Background = Coloring.Notifications.cTextBoxActive;
            _ui_btnSkip.IsEnabled = true;
        }
    }
}
