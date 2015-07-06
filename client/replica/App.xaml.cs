using System;
using System.Windows;
using System.Windows.Controls;
using replica.sl.lib;
using g = globalization;
using helpers.replica.services.dbinteract;

namespace replica.sl
{
	public partial class App : Application
	{
		static public replica.sl.lib.Profile cProfile;
		static private DBInteract _cDBI;
		static private System.Windows.Threading.DispatcherTimer _cPingTimer;

		static public void Ping()
		{
			if (null == _cDBI)
			{
				_cPingTimer = new System.Windows.Threading.DispatcherTimer();
				_cPingTimer.Interval = TimeSpan.FromMinutes(5);
				_cPingTimer.Tick += _cPingTimer_Tick;
				_cDBI = new DBInteract();
				_cDBI.PingCompleted += cDBI_PingCompleted;
				_cDBI.PingAsync();
			}
        }
		static private void _cPingTimer_Tick(object sender, EventArgs e)
		{
			_cPingTimer.Stop();
			_cDBI.PingAsync();
		}
		static private void cDBI_PingCompleted(object sender, PingCompletedEventArgs e)
		{
			_cPingTimer.Stop();
			_cPingTimer.Start();
		}

		public App()
		{
			Startup += this.Application_Startup;
			UnhandledException += this.Application_UnhandledException;
			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
            this.RootVisual = new MainPage() { Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name) };
        }
		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
		{
			// If the app is running outside of the debugger then report the exception using
			// a ChildWindow control.
			if (!System.Diagnostics.Debugger.IsAttached)
			{
				// NOTE: This will allow the application to continue running after an exception has been thrown
				// but not handled. 
				// For production applications this error handling should be replaced with something that will 
				// report the error to the website and stop the application.
				e.Handled = true;
				ChildWindow errorWin = new ErrorWindow(e.ExceptionObject);
				errorWin.Show();
			}
		}
	}
}