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

using helpers.replica.services.dbinteract;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class Media : ChildWindow
	{
        public string ButtonOKCaption
        {
            set
            {
                _ui_btnOK.Content = value;
            }
        }
        public File[] SelectedFiles
        {
            get
            {
                return _ui_MediaList.SelectedFiles;
            }
        }
        public string sStorageDefaultSelection
        {
            set
            {
                _ui_MediaList.sStorageDefaultSelection = value;
            }
        }
        public string sFilenameToScrollTo
        {
            set
            {
                _ui_MediaList.sFilenameToScrollTo = value;
            }
        }
        public bool bSingleSelectionMode
        {
            set
            {
                if (value)
                    _ui_MediaList._ui_dgFiles.SelectionMode = DataGridSelectionMode.Single;
                else
                    _ui_MediaList._ui_dgFiles.SelectionMode = DataGridSelectionMode.Extended;
            }
        }
		public Media()
		{
			InitializeComponent();
            Title = g.Helper.sMedia.ToLower();

            _ui_Media.AddHandler(Button.KeyDownEvent, new KeyEventHandler(_ui_Media_KeyDown), true);
            _ui_MediaList._nMaxHeight = double.PositiveInfinity; //значит высота автоматически
            _ui_MediaList.Init();
		}

#region event handlers
		private void _ui_btnOK_Click(object sender, RoutedEventArgs e)
		{
            if (null == _ui_MediaList.SelectedFiles || 1 > _ui_MediaList.SelectedFiles.Length)
            {
                MessageBox.Show(g.Common.sNoItemsSelected);
                return;
            }
            DialogResult = true;
		}
		private void _ui_btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
#endregion

		

        private void _ui_Media_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) && Visibility.Visible == _ui_btnOK.Visibility)
                _ui_btnOK_Click(null, null);
            if ((e.Key == Key.Escape) && Visibility.Visible == _ui_btnCancel.Visibility)
                _ui_btnCancel_Click(null, null);
        }
	}
}

