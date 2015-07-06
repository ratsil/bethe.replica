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

using controls.childs.sl;
using helpers.replica.services.dbinteract;
using controls.replica.sl;
using g = globalization;

namespace replica.sl
{
	public partial class programs : Page
	{
		public programs()
		{
			InitializeComponent();
            Title = g.Helper.sPrograms;
            
            _ui_al.TabsClear();
			_ui_al.TabAdd(AssetsList.Tab.Programs);
			_ui_al.Init(AssetsList.Tab.Programs);
		}
		#region event handlers
		#region UI
		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}
		#endregion
		#endregion

	}
}
