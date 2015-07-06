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

using controls.sl;
using controls.childs.sl;
using helpers.replica.services.dbinteract;
using controls.extensions.sl;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class FolderChooser : ChildWindow
	{
		public string sSelectedFolder;
		public string sSelectedFullPath;
		private DBInteractSoapClient _cDBI;
		private string _sCurrentDir;
		private string _sHomeDir;
		private string sHomeDir
		{
			get
			{
				return _sHomeDir;
			}
			set
			{
				_sHomeDir = value;
				sCurrentDir = value;
			}
		}
		public string sErr;
		private string sCurrentDir
		{
			get { return _sCurrentDir; }
			set
			{
				_sCurrentDir = value;
				_ui_tbPath.Text = value;
			}
		}
		public FolderChooser()
		{
			InitializeComponent();
            Title = g.Common.sSelectFiles.ToLower();
		}
		public FolderChooser(DBInteractSoapClient cDBI)
			: this()
		{
			_cDBI = cDBI;
		}
		protected override void OnOpened()
		{
			base.OnOpened();
			_cDBI.DirectoriesTrailsGetCompleted += new EventHandler<DirectoriesTrailsGetCompletedEventArgs>(_cDBI_DirectoriesTrailsGetCompleted);

			init();
			LayoutRoot.Visibility = System.Windows.Visibility.Collapsed;
			_cDBI.DirectoriesTrailsGetAsync(sHomeDir);
		}

		protected override void OnClosed(EventArgs e)
		{
			_cDBI.DirectoriesTrailsGetCompleted -= _cDBI_DirectoriesTrailsGetCompleted;
			base.OnClosed(e);
		}
		public void Show(string sHomeDir)
		{
			this.sHomeDir = sHomeDir;
			_ui_dgFilesSCR.SelectionMode = DataGridSelectionMode.Single;
			base.Show();
		}
		new public void Show()
		{
			throw new Exception("Don't use .Show() with 0 arguments");
		}
		void init()
		{
			_ui_lblSelected.Content = g.Common.sNoSelection;
			sErr = "";
			sSelectedFolder = null;
		}
		void _cDBI_DirectoriesTrailsGetCompleted(object sender, DirectoriesTrailsGetCompletedEventArgs e)
		{
			GetCompleted(e.Result.ToArray());
		}
		void GetCompleted(string[] aStr)
		{
			_ui_dgFilesSCR.ItemsSource = new string[0];
			_ui_dgFilesSCR.ItemsSource = aStr;
			LayoutRoot.Visibility = System.Windows.Visibility.Visible;
		}
		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (null != _ui_dgFilesSCR.SelectedItem)
			{
				sSelectedFolder = (string)_ui_dgFilesSCR.SelectedItem;
				sSelectedFullPath = sCurrentDir + sSelectedFolder;
				this.DialogResult = true;
			}
			else
				this.DialogResult = false;
		}
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
		private void _ui_dgFilesSCR_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null != _ui_dgFilesSCR.SelectedItem)
			{
				_ui_lblSelected.Content = (string)_ui_dgFilesSCR.SelectedItem;
			}
			else
                _ui_lblSelected.Content = g.Common.sNoSelection;
		}
		private void _ui_btnShow_Click(object sender, RoutedEventArgs e)
		{
			LayoutRoot.Visibility = System.Windows.Visibility.Collapsed;
			_sCurrentDir = _ui_tbPath.Text;
			_cDBI.DirectoriesTrailsGetAsync(sHomeDir);
		}
	}
}

