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

using presentation.sl.services.preferences;

namespace presentation.sl
{
	public partial class App : Application
	{
		public class Preferences
		{
			public string[] aFontFamilies;

			public Preferences()
			{ }
			public Preferences(services.preferences.Presentation cPreferences)
			{
				aFontFamilies = cPreferences.aFontFamilies.OrderBy(o => o).ToArray();
			}
		}
		static public Preferences cPreferences;

		public App()
		{
			this.Startup += this.Application_Startup;
			this.Exit += this.Application_Exit;
			this.UnhandledException += this.Application_UnhandledException;

			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			PreferencesSoapClient cPreferences = new PreferencesSoapClient();
			cPreferences.PresentationGetCompleted += new EventHandler<PresentationGetCompletedEventArgs>(cPreferences_PresentationGetCompleted);
			cPreferences.PresentationGetAsync();
		}

		void cPreferences_PresentationGetCompleted(object sender, PresentationGetCompletedEventArgs e)
		{
			try
			{
				if (null != e.Error)
					throw e.Error;
				cPreferences = new Preferences(e.Result);
			}
			catch (Exception ex)
			{
                (new controls.childs.sl.MsgBox()).ShowError(ex);
				cPreferences = new Preferences();
			}
			this.RootVisual = new MainPage();
		}

		private void Application_Exit(object sender, EventArgs e)
		{

		}

		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
		{
			// If the app is running outside of the debugger then report the exception using
			// the browser's exception mechanism. On IE this will display it a yellow alert 
			// icon in the status bar and Firefox will display a script error.
			if (!System.Diagnostics.Debugger.IsAttached)
			{

				// NOTE: This will allow the application to continue running after an exception has been thrown
				// but not handled. 
				// For production applications this error handling should be replaced with something that will 
				// report the error to the website and stop the application.
				e.Handled = true;
				Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
			}
		}

		private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
		{
			try
			{
				string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
				errorMsg = errorMsg.Replace('"', '\'').Replace("\n", @"\n");

				System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
			}
			catch (Exception)
			{
			}
		}
	}
}
