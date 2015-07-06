using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using helpers.replica.services.dbinteract;
using controls.sl;
using controls.childs.sl;
using g = globalization;

namespace replica.sl
{
	public partial class sms : Page
	{
        private Progress _dlgProgress = new Progress();
        private DBInteract _cDBI;
		private DateTime _dtNextMouseClickForDoubleClick;
		private Message _cMessageForDoubleClick;
        public sms()
        {
            InitializeComponent();

            _cDBI = new DBInteract();
            _cDBI.MessageMarkCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cDBI_MessageMarkCompleted);
            _cDBI.MessagesQueueGetCompleted += new EventHandler<MessagesQueueGetCompletedEventArgs>(_cDBI_MessagesQueueGetCompleted);
            _cDBI.MessageUnMarkCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_cDBI_MessageMarkCompleted);
            _ui_rpSMS.IsOpenChanged += _ui_rpSMS_IsOpenChanged;

			_ui_dgMessages.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgMessages_MouseLeftButtonDown), true);
			_ui_dgMessagesMarked.AddHandler(Button.MouseLeftButtonDownEvent, new MouseButtonEventHandler(_ui_dgMessagesMarked_MouseLeftButtonDown), true);

			App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
			_ui_svMainViewer.MaxHeight = UI_Sizes.GetPossibleHeightOfPlaylistScrollViewer();
			_ui_dgMessages.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInSMSView_Single();
			_ui_dgMessagesMarked.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInSMSView_Single();
			Init();
        }

		private void Init()
		{
			if (_ui_rpSMS.IsOpen)
			{
				_ui_rpSMS.IsEnabled = false;
				//_dlgProgress.Show();
				_cDBI.MessagesQueueGetAsync();
			}

			_ui_Search.ItemAdd = null;
            _ui_Search.sCaption = g.Common.sSearch + ":";
			_ui_Search.sDisplayMemberPath = "sText";
			_ui_Search.nGap2nd = 2;
			_ui_Search.DataContext = _ui_dgMessages;
		}
		void BrowserWindow_Resized(object sender, EventArgs e)
		{
			_ui_svMainViewer.MaxHeight = UI_Sizes.GetPossibleHeightOfPlaylistScrollViewer();
			_ui_dgMessages.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInSMSView_Single();
			_ui_dgMessagesMarked.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInSMSView_Single();
		}

            
        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
           
        //    base.OnNavigatedFrom(e);
        //}
        // Executes when the user navigates to this page.
        #region . sms .
        Message _cMessageSelected = null;
        Message _cMessageMarkedSelected = null;
        Message[] aAllMessages = new Message[0];
        #region ui
        private void _ui_dgMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < e.AddedItems.Count)
            {
                _cMessageSelected = (Message)e.AddedItems[0];
                _ui_btnMessageMark.IsEnabled = true;
				_ui_btnMessageBlock.IsEnabled = true;
            }
            else
            {
                _cMessageSelected = null;
				_ui_btnMessageMark.IsEnabled = false;
				_ui_btnMessageBlock.IsEnabled = false;
            }
        }
        private void _ui_dgMessagesMarked_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < e.AddedItems.Count)
            {
                _cMessageMarkedSelected = (Message)e.AddedItems[0];
				_ui_btnMessageUnMark.IsEnabled = true;
            }
            else
            {
                _cMessageMarkedSelected = null;
				_ui_btnMessageUnMark.IsEnabled = false;
            }
        }

        private void _ui_btnMessageAdd_Click(object sender, RoutedEventArgs e)
        {

        }
        private void _ui_btnMessageMark_Click(object sender, RoutedEventArgs e)
        {
            _cMessageSelected.bMark = true;
            _cDBI.MessageMarkAsync(_cMessageSelected.nID);
            //_dlgProgress.Show();
			_ui_rpSMS.IsEnabled = false;
        }
        private void _ui_btnMessageUnMark_Click(object sender, RoutedEventArgs e)
        {
            _cMessageMarkedSelected.bMark = false;
            _cDBI.MessageUnMarkAsync(_cMessageMarkedSelected.nID);
            //_dlgProgress.Show();
			_ui_rpSMS.IsEnabled = false;
        }
        private void _ui_btnMessageBlock_Click(object sender, RoutedEventArgs e)
        {

        }
        private void _ui_btnMessageMarkedAdd_Click(object sender, RoutedEventArgs e)
        {

        }
        private void _ui_rpSMS_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpSMS.IsOpen)
            {
                //_dlgProgress.Show();
                _ui_rpSMS.IsEnabled = false;
                _cDBI.MessagesQueueGetAsync();
            }
        }

		private void MessagesShow()
		{
			List<Message> aMess = aAllMessages.Where(row => !row.bMark).ToList();
			_ui_dgMessages.ItemsSource = aMess;
			_ui_Search.DataContextUpdateInitial();
			_ui_dgMessages.Tag = aMess;
			_ui_dgMessagesMarked.ItemsSource = aAllMessages.Where(row => row.bMark);
			//if (null != _ui_Search && !_ui_Search.bIsEmpty)
			//    _ui_Search.Clear();
			if (null != _ui_Search && null == _ui_Search.aItemsSourceInitial) //EMERGENCY:l боюсь из-за того, что ты юзаешь чужие Tag'и в контролах, я вапче не уверен, что теперь будет работать
				_ui_Search.Search();
		}
		private void _ui_hlbtnRefresh_Click(object sender, RoutedEventArgs e)
		{
			//подсветка нажатия на гиперлинк...
			Brush cTMP = _ui_hlbtnRefresh.Foreground;
			_ui_hlbtnRefresh.Foreground = Coloring.SMS.cRefreshBtnPressed;
			System.Windows.Threading.DispatcherTimer _cTimerFor_ui_hlbtnRefresh = new System.Windows.Threading.DispatcherTimer();
			_cTimerFor_ui_hlbtnRefresh.Interval = new System.TimeSpan(0, 0, 1);
			_cTimerFor_ui_hlbtnRefresh.Tick +=
				delegate(object s, EventArgs args)
				{
					_cTimerFor_ui_hlbtnRefresh.Stop();
					_ui_hlbtnRefresh.Foreground = cTMP;
				};
			_cTimerFor_ui_hlbtnRefresh.Start();

			_ui_rpSMS_IsOpenChanged(_ui_rpSMS, null);
		}
		void _ui_dgMessages_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (_dtNextMouseClickForDoubleClick < DateTime.Now)
			{
				_dtNextMouseClickForDoubleClick = DateTime.Now.AddMilliseconds(400);
				_cMessageForDoubleClick = (Message)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
			}
			else
			{
				_dtNextMouseClickForDoubleClick = DateTime.MinValue;
				if (_cMessageForDoubleClick == (Message)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext)
				{
					_ui_dgMessages.SelectedItem = _cMessageForDoubleClick;
					_ui_btnMessageMark_Click(null, null);
				}
			}
		}
		void _ui_dgMessagesMarked_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (_dtNextMouseClickForDoubleClick < DateTime.Now)
			{
				_dtNextMouseClickForDoubleClick = DateTime.Now.AddMilliseconds(400);
				_cMessageForDoubleClick = (Message)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
			}
			else
			{
				_dtNextMouseClickForDoubleClick = DateTime.MinValue;
				if (_cMessageForDoubleClick == (Message)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext)
				{
					_ui_dgMessagesMarked.SelectedItem = _cMessageForDoubleClick;
					_ui_btnMessageUnMark_Click(null, null);
				}
			}
		}

        #endregion
        #region dbi
        void _cDBI_MessagesQueueGetCompleted(object sender, MessagesQueueGetCompletedEventArgs e)
        {
            if (null != e && null != e.Result)
                aAllMessages = e.Result;
			MessagesShow();
            _ui_rpSMS.IsEnabled = true;
            //_dlgProgress.Close();
        }
        void _cDBI_MessageMarkCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //_dlgProgress.Close();
			_ui_rpSMS.IsEnabled = true;
            _cDBI_MessagesQueueGetCompleted(null, null);
        }
        #endregion

        #endregion


    }
}

