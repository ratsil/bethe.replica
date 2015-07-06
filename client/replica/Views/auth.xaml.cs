using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.IO.IsolatedStorage;

using controls.childs.sl;
using helpers.replica.services.dbinteract;
using g = globalization;

namespace replica.sl
{
	public partial class Authorization : Page
	{
		private System.Windows.Threading.DispatcherTimer _cTimerFor_ui_hlbtnSave;
		Progress _dlgProgress;
		DBInteract _cDBI;

		public Authorization()
		{
			InitializeComponent();

            Title = g.Common.sAuthorization;
            _ui_lbVersion.Content = g.Common.sVersion.ToLower() + " " + (new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName)).Version.ToString();  // менять в свойствах проекта или в Properties/AssemblyInfo.cs; описание: <major version>.<minor version>.<build number>.<revision>; revision has autoincrement
			_cTimerFor_ui_hlbtnSave = new System.Windows.Threading.DispatcherTimer();
			_cTimerFor_ui_hlbtnSave.Tick += new EventHandler(SavedBack);
			_cTimerFor_ui_hlbtnSave.Interval = new System.TimeSpan(0, 0, 0, 0, 800);  // период индикации нажатия на кнопки "сохранить"
			_dlgProgress = new Progress();
			_cDBI = new DBInteract();
			_cDBI.InitCompleted += new EventHandler<InitCompletedEventArgs>(_cDBI_InitCompleted);
			_cDBI.AccessScopesGetCompleted += new EventHandler<AccessScopesGetCompletedEventArgs>(_cDBI_AccessScopesGetCompleted);
			_cDBI.ProfileGetCompleted += new EventHandler<ProfileGetCompletedEventArgs>(_cDBI_ProfileGetCompleted);

            if(null != Preferences.sUser)
                _ui_tbUserName.Text = Preferences.sUser;
            if (null != Preferences.sPassword)
                _ui_pswbPassword.Password = Preferences.sPassword;
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}
		private void _ui_btnOK_Click(object sender, RoutedEventArgs e)
		{
			_dlgProgress.Show();
			_cDBI.InitAsync(_ui_tbUserName.Text, _ui_pswbPassword.Password);
		}
		private void _ui_hlbtnLogin_Click(object sender, RoutedEventArgs e)
		{
            _ui_hlbtnLogin.Content = g.Common.sOk.ToLower();
			_cTimerFor_ui_hlbtnSave.Start();
			_ui_tbUserName.Focus();
            Preferences.sUser = _ui_tbUserName.Text;
        }
		private void _ui_hlbtnPass_Click(object sender, RoutedEventArgs e)
		{
            _ui_hlbtnPass.Content = g.Common.sOk.ToLower();
			_cTimerFor_ui_hlbtnSave.Start();
			_ui_pswbPassword.Focus();
            Preferences.sPassword = _ui_pswbPassword.Password;
        }
		void SavedBack(object s, EventArgs args)
		{
			_cTimerFor_ui_hlbtnSave.Stop();
            _ui_hlbtnPass.Content = _ui_hlbtnLogin.Content = g.Common.sSave.ToLower();
		}
		void _cDBI_InitCompleted(object sender, InitCompletedEventArgs e)
		{
			_cDBI.InitSessionAsync();
			if (null == e.Error && e.Result)
				_cDBI.AccessScopesGetAsync();
			else
				ErrorShow();
		}
		void _cDBI_AccessScopesGetCompleted(object sender, AccessScopesGetCompletedEventArgs e)
		{
			if (null == e.Error && null != e.Result && 0 < e.Result.Length)
			{
				access.scopes.init(e.Result);
				_cDBI.ProfileGetAsync();
				bool b;
				try
				{
					b = access.scopes.programs.chatinouts.bCanDelete;
					
				}
				catch { }
			}
			else
				ErrorShow();

		}
		void _cDBI_ProfileGetCompleted(object sender, ProfileGetCompletedEventArgs e)
		{
			try
			{
				App.cProfile = new lib.Profile(e.Result);
				this.NavigationService.Navigate(App.cProfile.cHomePage);
				_dlgProgress.Close();
				App.Ping();
			}
			catch { }
		}
		void ErrorShow()
		{
			_ui_txtError.Visibility = Visibility.Visible;
			_dlgProgress.Close();
		}

		private void _ui_pswbPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				_ui_btnOK_Click(null, null);
		}

		private void _ui_tbUserName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				_ui_pswbPassword.Focus();
		}
        
        //public string CookiesEscape(string sIn)
        //{
        //    if (null != sIn)
        //        return sIn.Replace(";", "%59");
        //    else
        //        return sIn;
        //}
        //public string CookiesUnEscape(string sIn)
        //{
        //    if (null != sIn)
        //        return sIn.Replace("%59", ";");
        //    else
        //        return sIn;
        //}
    }
}
