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

using replica.sl;
using replica.sl.ListProviders;
using helpers.replica.services.dbinteract;
using controls.childs.sl;
using g = globalization;

namespace controls.replica.sl
{
	public partial class PersonsList : UserControl
	{
		private Progress _dlgProgress;
        private DBInteract _cDBI = null;
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
        private MsgBox _cMsgBox;
        private Dictionary<string, string> _ahTransliteration;
        private Dictionary<string, string> _ahTransliterationInvert;
        //private Dictionary<string, string> _ahWrongKeyboard, _ahWrongKeyboardInvert;
        //private DateTime dtNextMouseClickForDouble;
        public string _CellValueBeforeEditing;

		public PersonsList()
		{
			InitializeComponent();

			_dlgProgress = new Progress();
            _cMsgBox = new MsgBox();
            _ahTransliteration = new Dictionary<string, string>();
            _ahTransliterationInvert = new Dictionary<string, string>();

            App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);
			_ui_tpArtists.Tag = "artist";

            _ui_Search.ItemAdd = _ui_btnAdd_Click;
            _ui_Search.sCaption = g.Common.sSearch + ":";
            _ui_Search.sDisplayMemberPath = "sName";
			_ui_Search.DataContext = _ui_dgPersons;
		}
        public void Init()
        {
            if (null == _cDBI && null != cDBI)
            {
                _cDBI.PersonsGetCompleted += new EventHandler<PersonsGetCompletedEventArgs>(_cDBI_PersonsGetCompleted);
                _cDBI.PersonTypeGetCompleted += new EventHandler<PersonTypeGetCompletedEventArgs>(_cDBI_PersonTypeGetCompleted);
                _cDBI.PersonSaveCompleted += new EventHandler<PersonSaveCompletedEventArgs>(_cDBI_PersonSaveCompleted);
                _cDBI.PersonsRemoveCompleted += new EventHandler<PersonsRemoveCompletedEventArgs>(_cDBI_PersonsRemoveCompleted); _dlgProgress.Show();
                _dlgProgress.Show();
                System.Threading.Thread.Sleep(300);
				_ui_tcPersons_SelectionChanged(null, null);
                _ui_tcPersons.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
            }
        }
        
		#region event handlers
		#region UI
        void BrowserWindow_Resized(object sender, EventArgs e)
        {
            _ui_tcPersons.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInAssetView();
        }
		private void _ui_tcPersons_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null != _ui_dgPersons)
			{
				_dlgProgress.Show();
				_ui_dgPersons.ItemsSource = null;
				if (null != e && 0 < e.RemovedItems.Count)
					((TabItem)e.RemovedItems[0]).Content = null;
				TabItem ui_ti = (TabItem)_ui_tcPersons.SelectedItem;
				ui_ti.Content = _ui_grdContainer;
				if (null == ui_ti.Tag)
				{
					_ui_grdNew.Visibility = System.Windows.Visibility.Collapsed;
					_cDBI.PersonsGetAsync(null);
				}
				else
				{
					_ui_grdNew.Visibility = System.Windows.Visibility.Visible;
					_cDBI.PersonsGetAsync(ui_ti.Tag.ToString());
				}
			}
		}
   
		private void _ui_btnAdd_Click(string sText)
		{
			_dlgProgress.Show();
			_cDBI.PersonTypeGetAsync(((TabItem)_ui_tcPersons.SelectedItem).Tag.ToString());
		}
        private void _ui_dgPersons_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
			_CellValueBeforeEditing = ((TextBlock)e.EditingEventArgs.OriginalSource).Text;
        }
        private void _ui_dgPersons_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Person cPers = (Person)_ui_dgPersons.SelectedItem;
            if (1 < ((Person[])_ui_dgPersons.Tag).Where(nam => nam.sName.Equals(cPers.sName)).ToArray().Length)
            {
                e.Cancel = true;
            }
        }
		private void _ui_dgPersons_RowEditEnded(object sender, DataGridRowEditEndedEventArgs e)
		{
			if (DataGridEditAction.Commit == e.EditAction)
            {// еще одинаковость не может быть!!!

                Person cPers = (Person)_ui_dgPersons.SelectedItem;
                if (_CellValueBeforeEditing != cPers.sName)
                {
                    _dlgProgress.Show();
                    _ui_dgPersons.SelectedIndex = e.Row.GetIndex();
                    cPers.sName = cPers.sName.ToLower().Trim();
                    _cDBI.PersonSaveAsync(cPers);
                    _ui_Search.Tag = cPers.sName;
                }
			}
		}
		#region контекстное меню
		private void _ui_dgPersons_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			Person cSelectedPerson = (Person)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
            if (null != cSelectedPerson && 2 > _ui_dgPersons.SelectedItems.Count)
                _ui_dgPersons.SelectedItem = cSelectedPerson;
		}
		private void _ui_cmPersons_Opened(object sender, RoutedEventArgs e)
		{
            //TODO права
		}
		private void _ui_cmPersons_Refresh(object sender, RoutedEventArgs e)
		{
			_ui_tcPersons_SelectionChanged(null, null);
		}
		private void _ui_cmPersons_Delete(object sender, RoutedEventArgs e)
		{
            if (1 > _ui_dgPersons.SelectedItems.Count)
                _cMsgBox.Show(g.Common.sNoItemsSelected);
            else
            {
                _cMsgBox.Closed += new EventHandler(msgOk_Closed);
                ListBox cLB = new ListBox();
                cLB.ItemsSource = _ui_dgPersons.SelectedItems;
                cLB.DisplayMemberPath = "sName";
                _cMsgBox.ShowQuestion(g.Common.sItemsDeleteConfirmation, cLB);
                _cMsgBox.Tag = "msgOk_Closed";
            }
		}
        void msgOk_Closed(object sender, EventArgs e)
        {
			_cMsgBox.Closed -= msgOk_Closed;
			//MsgBox msg = (MsgBox)sender;
			if (MsgBox.MsgBoxButton.OK == _cMsgBox.enMsgResult && "msgOk_Closed" == _cMsgBox.Tag.ToString())
            {
                _cDBI.PersonsRemoveAsync(_ui_dgPersons.SelectedItems.Cast<Person>().ToArray());
                _cMsgBox.Tag = null;
            }
        }
		#endregion
		#endregion
		#region DBI
		void _cDBI_PersonTypeGetCompleted(object sender, PersonTypeGetCompletedEventArgs e)
		{
			string sText = _ui_Search.sText.ToLower().Trim();
            _cDBI.PersonSaveAsync(new Person() { sName = sText, cType = e.Result, nID = -1 });
			_ui_Search.Tag = sText;
			_ui_Search.Clear();
		}
		void _cDBI_PersonsGetCompleted(object sender, PersonsGetCompletedEventArgs e)
		{
            _ui_dgPersons.Tag = e.Result;
            _ui_dgPersons.ItemsSource = e.Result;
			_ui_Search.DataContextUpdateInitial();
			//_ui_Search.Search();
            _ui_dgPersons.UpdateLayout();
            if (null != _ui_Search.Tag) // && 0 == _ui_Search._ui_TextBox.Text.Length
            {
                Person[] cPer = e.Result.Where(nam => nam.sName.Equals(_ui_Search.Tag.ToString())).ToArray();
                if (0 < cPer.Length)
                {
					_ui_dgPersons.SelectedItem = cPer[0];
					Dispatcher.BeginInvoke(() => _ui_dgPersons.ScrollIntoView(cPer[0], null));
                }
                _ui_Search.Tag = null;
            }
			_dlgProgress.Close();
		}

        void _cDBI_PersonSaveCompleted(object sender, PersonSaveCompletedEventArgs e)
		{
            if (1 > e.Result)
                _cMsgBox.ShowError(g.Common.sErrorUnknown);
			_ui_tcPersons_SelectionChanged(null, null);
		}
        void _cDBI_PersonsRemoveCompleted(object sender, PersonsRemoveCompletedEventArgs e)
		{
            if (0 < e.Result.Length)
            {
                ListBox cLB = new ListBox();
                cLB.ItemsSource = e.Result;
                cLB.DisplayMemberPath = "sName";
                _cMsgBox.ShowWarning(g.Replica.sErrorPersonsList1 + ":", cLB);
            }
			_ui_tcPersons_SelectionChanged(null, null);
		}
		#endregion
		#endregion
	}
}