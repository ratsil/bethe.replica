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

using controls.sl;
using controls.childs.sl;
using controls.replica.sl;
using helpers.replica.services.dbinteract;
using g = globalization;

namespace replica.sl
{
    public partial class assets : Page
    {
		private enum Section
		{
			assets,
			media,
			persons
		}
		public assets()
        {
            InitializeComponent();

            Title = g.Helper.sAssets;
            _ui_rpAssets.IsOpenChanged += _ui_rpAssets_IsOpenChanged;
            _ui_rpMedia.IsOpenChanged += _ui_rpMedia_IsOpenChanged;
            _ui_rpPersons.IsOpenChanged += _ui_rpPersons_IsOpenChanged;

			_ui_rpAssets.IsOpen = true;
			_ui_al.Init(AssetsList.Tab.Clips); 

            _ui_rpMedia.IsOpen = false;
            _ui_rpPersons.IsOpen = false;
        }
		#region event handlers
		#region UI
		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}
		private void _ui_rpAssets_Loaded(object sender, RoutedEventArgs e)
		{
            //_ui_rpAssets.MaxHeight = App.Current.RootVisual.RenderSize.Height;
		}
		private void _ui_rpMedia_Loaded(object sender, RoutedEventArgs e)
		{
            //_ui_rpPersons.MaxHeight = App.Current.RootVisual.RenderSize.Height;
		}
		private void _ui_rpPersons_Loaded(object sender, RoutedEventArgs e)
		{
            //_ui_rpPersons.MaxHeight = App.Current.RootVisual.RenderSize.Height;
		}
        void _ui_rpAssets_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpAssets.IsOpen)
            {
                _ui_rpMedia.IsOpen = false;
                _ui_rpPersons.IsOpen = false;
                _ui_al.Init(AssetsList.Tab.Clips);
            }
        }
        void _ui_rpMedia_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpMedia.IsOpen)
            {
                _ui_rpAssets.IsOpen = false;
                _ui_rpPersons.IsOpen = false;
                _ui_ml.sParent = "Reduced Panel";
                _ui_ml.Init();
            }
        }
        void _ui_rpPersons_IsOpenChanged(object sender, EventArgs e)
        {
            if (_ui_rpPersons.IsOpen)
            {
                _ui_rpAssets.IsOpen = false;
                _ui_rpMedia.IsOpen = false;
                _ui_pl.Init();
            }
        }
        
		#endregion
		#endregion
       
    }
}
