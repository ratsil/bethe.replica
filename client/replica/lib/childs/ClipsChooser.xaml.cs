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
using replica.sl;
using helpers.replica.services.dbinteract;
using controls.childs.sl;
using g = globalization;

namespace controls.childs.replica.sl
{
    public partial class ClipsChooser : ChildWindow
    {
        private Progress _dlgProgress = new Progress();
        private DBInteract _cDBI;
		private DateTime dtNextMouseClickForDouble;
        public string _sAssetType = "clip";
        public List<Clip> SelectedClips
        {
            get
            {
				if (null != _ui_lbClipsSelected.ItemsSource)
                {
                    List<Clip> aRetVal = new List<Clip>();
					foreach (Asset cA in (List<Asset>)_ui_lbClipsSelected.ItemsSource)
                    {
                        aRetVal.Add(new Clip()
                        {
                            nID = cA.nID,
                            sName = cA.sName,
                            nFramesQty = cA.nFramesQty,
                            stCues = new Cues(),
                            cFile = new File() { cStorage = new Storage() },
                            stVideo = new Video(),
                            stSoundLevels = new SoundLevels()
                        });
                    }
                    return aRetVal;
                }
                else
                    return null;
            }
        }
        public ClipsChooser()
        {
            InitializeComponent();
            Title = g.Helper.sSelectClip;
        }
        public ClipsChooser(DBInteract cdbi)
            : this()
        {
            _cDBI = cdbi;
			_cDBI.AssetsGetCompleted += new EventHandler<AssetsGetCompletedEventArgs>(_cDBI_AssetsGetCompleted);

			_ui_Search.ItemAdd = null;
			_ui_Search.sCaption = "";
			_ui_Search.nGap2nd = 0;
			_ui_Search.AddButtonWidth = 0;
			_ui_Search.sDisplayMemberPath = "sName";
			_ui_Search.DataContext = _ui_dgClips;
			_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxActive;
        }

        void _cDBI_AssetsGetCompleted(object sender, AssetsGetCompletedEventArgs e)
        {
            if (null == e && null != _ui_dgClips.Tag)
                _ui_dgClips.ItemsSource = (Asset[])_ui_dgClips.Tag;
            else if (null != e && null != e.Result)
            {
                _ui_dgClips.ItemsSource = e.Result;
                _ui_dgClips.Tag = e.Result;
				_ui_Search.DataContextUpdateInitial();
				_ui_dgClips.UpdateLayout();
            }
			if (null != e.UserState && e.UserState is long)
			{
				Asset[] aAss = e.Result.Where(pers => pers.nID.Equals(e.UserState)).ToArray();
				ScrollToAsset(aAss);
			}
			if (null != _ui_Search.Tag) // && 0 == _ui_Search._ui_TextBox.Text.Length
			{
				Asset[] aAss = e.Result.Where(asset => asset.sName.Equals(_ui_Search.Tag.ToString())).ToArray();
				ScrollToAsset(aAss);
				_ui_Search.Tag = null;
			}
			if (null == _ui_lbClipsSelected.ItemsSource)
			{
				_ui_lbClipsSelected.ItemsSource = new List<Asset>();
				_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxActive;
			}
            _dlgProgress.Close();
        }
		private void ScrollToAsset(Asset[] aAss)
		{
			if (0 < aAss.Length)
			{
				_ui_dgClips.ScrollIntoView(aAss[0], _ui_dgClips.Columns[0]);
				_ui_dgClips.SelectedItem = aAss[0];
			}
		}
        new public void Show()
        {
            if (null == _ui_dgClips.Tag)
                _cDBI.AssetsGetAsync(_sAssetType, null, 0);
            else
                _cDBI_AssetsGetCompleted(null, null);
            base.Show();
            _dlgProgress.Show();
        }


		private void _ui_dgClips_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (dtNextMouseClickForDouble < DateTime.Now)
				dtNextMouseClickForDouble = DateTime.Now.AddMilliseconds(400);
			else
			{
				Asset cSelectedPerson = (Asset)((FrameworkElement)(((RoutedEventArgs)(e)).OriginalSource)).DataContext;
				dtNextMouseClickForDouble = DateTime.MinValue;
				_ui_lbClipsSelectedAdd(cSelectedPerson);
			}
		}
		private void _ui_lbClipsSelectedAdd(Asset cAss)
		{
			if (null != cAss)
			{
				List<Asset> aAss;
				aAss = ((IEnumerable<Asset>)_ui_lbClipsSelected.ItemsSource).ToList<Asset>();
				if (!aAss.Contains(cAss))
				{
					aAss.Add(cAss);
					_ui_lbClipsSelected.ItemsSource = aAss;
					_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxChanged;
				}
				_ui_lbClipsSelected.UpdateLayout();
			}
		}
		private void _ui_lbClipsSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Asset cSelectedAsset = (Asset)_ui_lbClipsSelected.SelectedItem;
			_ui_lbClipsSelectedRemove(cSelectedAsset);
		}
		private void _ui_lbClipsSelectedRemove(Asset cAss)
		{
			if (null != cAss)
			{
				List<Asset> aAss;
				aAss = ((IEnumerable<Asset>)_ui_lbClipsSelected.ItemsSource).ToList<Asset>();
				if (aAss.Contains(cAss))
				{
					aAss.Remove(cAss);
					_ui_lbClipsSelected.ItemsSource = aAss;
				}
				_ui_lbClipsSelected.UpdateLayout();
			}
			if (0 == ((IEnumerable<Asset>)_ui_lbClipsSelected.ItemsSource).ToList<Asset>().Count)
				_ui_lbClipsSelected.Background = Coloring.Notifications.cTextBoxActive;
		}
		private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

