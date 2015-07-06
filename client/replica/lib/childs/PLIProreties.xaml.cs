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
using controls.extensions.sl;
using controls.sl;
using replica.sl;
using replica.sl.ListProviders;
using helpers.replica.services.dbinteract;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class PLIProreties : ChildWindow
	{
		private Progress _dlgProgress;
		private MsgBox _cMsgBox;
		private DBInteract _cDBI;
		private PlaylistItemSL _cPLI;
		private PLIProreties()
		{
			InitializeComponent();
            Title = g.Helper.sPlaylistItemProperties.ToLower();
        }
		public PLIProreties(PlaylistItemSL cPLI)
			:this()
		{
			_cDBI = new DBInteract();
			_cPLI = cPLI;
			_cMsgBox = new MsgBox();
			_cDBI.ClassesGetCompleted += new EventHandler<ClassesGetCompletedEventArgs>(_cDBI_ClassesGetCompleted);
			_cDBI.PLIPropertiesSetCompleted += new EventHandler<PLIPropertiesSetCompletedEventArgs>(_cDBI_PLIPropertiesSetCompleted);
			_cDBI.ClassesGetAsync();
		}

		void _cDBI_PLIPropertiesSetCompleted(object sender, PLIPropertiesSetCompletedEventArgs e)
		{
			if (e.Result)
				this.DialogResult = true;
			else
			{
				_cMsgBox.Closed += new EventHandler(_cMsgBox_Closed);
                _cMsgBox.ShowError(g.Common.sErrorUpdate);
			}
		}

		void _cMsgBox_Closed(object sender, EventArgs e)
		{
			_cMsgBox.Closed -= _cMsgBox_Closed;
			this.DialogResult = false;
		}

		void _cDBI_ClassesGetCompleted(object sender, ClassesGetCompletedEventArgs e)
		{
			if (null != e.Result)
			{
				Classes.Set(e.Result);
				_ui_ddlClasses.ItemsSource = Classes.Array;
			}
			else
                _cMsgBox.Show(g.Common.sErrorDataReceive);

			ControlsLoad();
		}

		void ControlsLoad()
		{
			if (null != _ui_ddlClasses.Items)
				foreach (Class cEnum in _ui_ddlClasses.Items)
				{
					if (_cPLI.cClass.nID == cEnum.nID)
					{
						_ui_ddlClasses.Tag = cEnum;
						_ui_ddlClasses.SelectedItem = cEnum;
						_ui_ddlClasses.Background = Coloring.Notifications.cButtonNormal;
						break;
					}
				}
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (PLIFill())
				_cDBI.PLIPropertiesSetAsync(PlaylistItemSL.GetBase(_cPLI));
			else
				this.DialogResult = false;
		}
		private bool PLIFill()
		{
			bool bRetVal = false;
			if (((Class)_ui_ddlClasses.SelectedItem).nID != _cPLI.cClass.nID)
			{
				bRetVal = true;
				_cPLI.cClass = (Class)_ui_ddlClasses.SelectedItem;
			}
			return bRetVal;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}

		private void _ui_ddlClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((Class)_ui_ddlClasses.SelectedItem).nID == _cPLI.cClass.nID)
				_ui_ddlClasses.Background = Coloring.Notifications.cButtonNormal;
			else
				_ui_ddlClasses.Background = Coloring.Notifications.cButtonChanged;
		}
	}
}

