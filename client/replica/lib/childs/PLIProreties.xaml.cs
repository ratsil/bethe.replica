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
		//private Progress _dlgProgress;
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
            try
            {
                if (e.Result)
                    this.DialogResult = true;
                else
                {
                    _cMsgBox.Closed += new EventHandler(_cMsgBox_Closed);
                    _cMsgBox.ShowError(g.Common.sErrorUpdate);
                }
            }
            catch (Exception ex)
            {
                _cDBI.ErrorLoggingAsync("PLIProreties: " + ex.ToString());
                _cMsgBox.ShowError(ex);
            }
        }

        void _cMsgBox_Closed(object sender, EventArgs e)
		{
			_cMsgBox.Closed -= _cMsgBox_Closed;
			this.DialogResult = false;
		}

		void _cDBI_ClassesGetCompleted(object sender, ClassesGetCompletedEventArgs e)
		{
            try
            {
                if (null != e.Result)
                {
                    Classes.Set(e.Result);
                    _ui_ctrClasses.Show(Classes.Array);
                }
                else
                    _cMsgBox.Show(g.Common.sErrorDataReceive);

                ControlsLoad();
            }
            catch(Exception ex)
            {
                _cDBI.ErrorLoggingAsync("PLIProreties: " + ex.ToString());
                _cMsgBox.ShowError(ex);
            }
        }

		void ControlsLoad()
		{
            _ui_ctrClasses.aSelectedItems = _cPLI.aClasses;
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
            if (!_ui_ctrClasses.bMarkedRed && _ui_ctrClasses.bChanged)
            {
                bRetVal = true;
                _cPLI.aClasses = _ui_ctrClasses.aSelectedItems;
            }
            return bRetVal;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
	}
}

