using System;
using System.Windows.Controls;
using System.Windows.Navigation;

using controls.childs.sl;
using helpers.replica.services.dbinteract;
using g = globalization;

namespace replica.sl.Views
{
	public partial class artist_search : Page
	{
		private Progress _dlgProgress;
		private MsgBox _dlgMsgBox;
		private DBInteract _cDBI;
		public artist_search()
		{
			InitializeComponent();

            _dlgProgress = new Progress();
			_dlgMsgBox = new MsgBox();

			_cDBI = new DBInteract();
			_cDBI.AssetsGetCompleted += new EventHandler<AssetsGetCompletedEventArgs>(_cDBI_AssetsGetCompleted);
			_cDBI.InitCompleted += new EventHandler<InitCompletedEventArgs>(_cDBI_InitCompleted);
			_cDBI.ArtistsGetCompleted += new EventHandler<ArtistsGetCompletedEventArgs>(_cDBI_ArtistsGetCompleted);

			_ui_SearchAsset.ItemAdd = null;
			_ui_SearchAsset.sCaption = g.Helper.sAssetSearch.ToLower() + ":";
			_ui_SearchAsset.DataContext = _ui_dgAssets;
			_ui_SearchAsset.sDisplayMemberPath = "sCuesName";
			//_ui_SearchAsset.cElementType = typeof(AssetSL);

			_ui_SearchArtist.ItemAdd = null;
            _ui_SearchArtist.sCaption = g.Helper.sPersonSearch + ":";
			_ui_SearchArtist.DataContext = _ui_dgArtists;
			_ui_SearchArtist.sDisplayMemberPath = "sName";

			App.Current.Host.Content.Resized += new EventHandler(BrowserWindow_Resized);

			_dlgProgress.Show();
			_dlgProgress.sInfo = "_cDBI.DBCredentialsSetAsync";
			_cDBI.InitAsync("user", "");
		}

	

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			_ui_dgArtists.MaxHeight= _ui_dgAssets.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInArtistSearchView();
		}
		private void BrowserWindow_Resized(object sender, EventArgs e)
		{
			_ui_dgArtists.MaxHeight = _ui_dgAssets.MaxHeight = UI_Sizes.GetPossibleHeightOfElementInArtistSearchView();
		}



		void _cDBI_InitCompleted(object sender, InitCompletedEventArgs e)
		{
			_dlgProgress.sInfo = "_cDBI.AssetsGetAsync";
			_cDBI.AssetsGetAsync("clip");
		}
		void _cDBI_AssetsGetCompleted(object sender, AssetsGetCompletedEventArgs e)
		{
			if (null == e || null == e.Result || 1 > e.Result.Length)
                _dlgMsgBox.ShowError(g.Helper.sErrorReceiveAssetsList + "...");
			else
				try
				{
					AssetSL[] aRes = AssetSL.GetArrayOfAssetSLs(e.Result);
					_ui_dgAssets.Tag = aRes;
					_ui_dgAssets.ItemsSource = aRes;
					//_ui_SearchAsset.Search();
					_ui_SearchAsset.DataContextUpdateInitial();
					_ui_dgAssets.UpdateLayout();
				}
				catch { }
			_dlgProgress.sInfo = "_cDBI.ArtistsGetAsync";
			_cDBI.ArtistsGetAsync();
		}
		void _cDBI_ArtistsGetCompleted(object sender, ArtistsGetCompletedEventArgs e)
		{
			if (null == e || null == e.Result || 1 > e.Result.Length)
                _dlgMsgBox.ShowError(g.Helper.sErrorReceiveArtistsList + "...");
			else
				try
				{
					_ui_dgArtists.Tag = e.Result;
					_ui_dgArtists.ItemsSource = e.Result;
					//_ui_SearchArtist.Search();
					_ui_SearchArtist.DataContextUpdateInitial();
					_ui_dgArtists.UpdateLayout();
				}
				catch { }
			_dlgProgress.Close();
			_ui_dgAssets.Focus();
		}
	}
}
