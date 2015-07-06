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
using System.Windows.Navigation;

namespace scr.Views
{
	public partial class Test : Page
	{
		public Test()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}
		private void _ui_btnStart_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_ui_ctrlMediaPreview.Init(_ui_tbFilename.Text);
				_ui_ctrlMediaPreview.Visibility = System.Windows.Visibility.Visible;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}

	}
}
