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

namespace controls.childs.replica.sl
{
	public partial class AdvancedPlaylist : ChildWindow
	{
		public AdvancedPlaylist()
		{
			InitializeComponent();
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
		private void _ui_dgAdvancedPL_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//int i = _ui_dgPlanned.SelectedItems.Count;
			//if (0 == i)
			//	_ui_lblPlannedSelected.Content = " ";
			//else
			//	_ui_lblPlannedSelected.Content = i.ToString();

			//if (1 == i)
			//	_cPlayListItemCurrent = (PlaylistItemSL)_ui_dgPlanned.SelectedItem;
			//else
			//	_cPlayListItemCurrent = null;

			//if (0 < _ui_nudHoursQty.Value && 1 == i && DateTime.MaxValue > _cPlayListItemCurrent.dtTimingsUpdate || 0 == _ui_nudHoursQty.Value)
			//	_ui_btnRecalculatePart.IsEnabled = true;
			//else
			//	_ui_btnRecalculatePart.IsEnabled = false;
		}

		private void _ui_btnAdd_Click(object sender, RoutedEventArgs e)
		{

		}

	}
}

