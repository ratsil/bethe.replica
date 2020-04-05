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

using scr.services.ingenie.player;
using scr.services.ingenie.cues;
using controls.sl;
using controls.childs.sl;

using g = globalization;

namespace scr.childs
{
	public partial class FilesChooser : ChildWindow
	{
		public enum Type
		{ 
			Videos,
			Images,
			Sequence,
			Trail
		}
		private PlayerSoapClient _cPlayer;
		private CuesSoapClient _cCues;
		private List<LivePLItem> aFiles;
		public LivePLItem cResult;
		public string sFolder;
		private string[] _aExtensions;
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
		private Type _eType;
		public Type eType
		{
			get
			{
				return _eType;
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

		public FilesChooser()
		{
			InitializeComponent();
		}
		public FilesChooser(PlayerSoapClient cPlayer, CuesSoapClient cCues)
			: this()
		{
			_cPlayer = cPlayer;
			_cCues = cCues;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			_cPlayer.FilesSCRGetCompleted += new EventHandler<FilesSCRGetCompletedEventArgs>(_cPlayer_FilesSCRGetCompleted);
			_cPlayer.VideoFramesQtyGetCompleted += new EventHandler<VideoFramesQtyGetCompletedEventArgs>(_cPlayer_FramesQtyGetCompleted);
			_cCues.DirectoriesSCRGetCompleted += new EventHandler<DirectoriesSCRGetCompletedEventArgs>(_cCues_DirectoriesSCRGetCompleted);

			init();
			LayoutRoot.Visibility = System.Windows.Visibility.Collapsed;
			if (eType == Type.Sequence || eType == Type.Trail)
				_cCues.DirectoriesSCRGetAsync(sCurrentDir);
			else
				_cPlayer.FilesSCRGetAsync(sCurrentDir, _aExtensions);
		}
		protected override void OnClosed(EventArgs e)
		{
			_cPlayer.FilesSCRGetCompleted -= _cPlayer_FilesSCRGetCompleted;
			_cPlayer.VideoFramesQtyGetCompleted -= _cPlayer_FramesQtyGetCompleted;
			_cCues.DirectoriesSCRGetCompleted -= _cCues_DirectoriesSCRGetCompleted;
			base.OnClosed(e);
		}

		public void Show(string sHomeDir, string[] aExtensions, Type eType)
		{
			this.sHomeDir = sHomeDir;
			_aExtensions = aExtensions;
			_eType = eType;
			switch (eType)
			{
				case Type.Videos:
					_ui_dgFilesSCR.SelectionMode = DataGridSelectionMode.Extended;
					break;
				case Type.Images:
					_ui_dgFilesSCR.SelectionMode = DataGridSelectionMode.Extended;
					break;
				case Type.Sequence:
				case Type.Trail:
					_ui_dgFilesSCR.SelectionMode = DataGridSelectionMode.Single;
					break;
			}
			base.Show();
		}
		new public void Show()
		{ 
			throw new NotImplementedException();
		}
		void init()
		{
			_ui_lblSelected.Content = g.Common.sNoSelection;
			sErr = "";
			//sCurrentDir = @"d:\Files\";
			cResult = null;
			aFiles = new List<LivePLItem>();
		}
		void _cPlayer_FramesQtyGetCompleted(object sender, VideoFramesQtyGetCompletedEventArgs e)
		{
			//_cPlayer.VideoFramesQtyGetCompleted -= _cPlayer_FramesQtyGetCompleted;
			try
			{
				if (0 < e.Result)
				{
					cResult.nFramesQty = e.Result;
					this.DialogResult = true;
				}
				else
				{
                    sErr = g.Helper.sErrorGettingFileTimings + ".";
					this.DialogResult = false;
				}
			}
			catch { this.DialogResult = false; }
		}
		void _cPlayer_FilesSCRGetCompleted(object sender, FilesSCRGetCompletedEventArgs e)
		{
			GetCompleted(e.Result);
		}
		void _cCues_DirectoriesSCRGetCompleted(object sender, DirectoriesSCRGetCompletedEventArgs e)
		{
			GetCompleted(e.Result);
		}
		void GetCompleted(string[] aStr)
		{
			bool bIsImage = false;
			if (_eType == Type.Images)
				bIsImage = true;
			aFiles = new List<LivePLItem>();
            string[] aF;
            if (null != aStr)
                foreach (string sFile in aStr)
                {
                    aF = sFile.Split(';');
                    LivePLItem cLPLI = new LivePLItem(aF[0], aF[0], bIsImage);
                    cLPLI.sModificationDate = aF.Length > 1 ? aF[1] : " ";
                    aFiles.Add(cLPLI);

                }
            _ui_dgFilesSCR.ItemsSource = new List<LivePLItem>();
            if (App.cPreferences.sFilesDefaultSort == "name")
                _ui_dgFilesSCR.ItemsSource = aFiles.OrderBy(o => o.sName);
            else
                _ui_dgFilesSCR.ItemsSource = aFiles; // default sorting - by date desc
            LayoutRoot.Visibility = System.Windows.Visibility.Visible;
		}
		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (null != _ui_dgFilesSCR.SelectedItem)
			{
				cResult = (LivePLItem)_ui_dgFilesSCR.SelectedItem;
				cResult.sFilenameFull = sCurrentDir + cResult.sFilename;

				switch (_eType)
				{
					case Type.Videos:
						_cPlayer.VideoFramesQtyGetAsync(cResult.sFilenameFull);
						break;
					case Type.Images:
						cResult.nFramesQty = 25 * int.Parse(_ui_tbFQty.Text);
						this.DialogResult = true;
						break;
					case Type.Sequence:
					case Type.Trail:
						this.DialogResult = true;
						break;
				}
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
				_ui_lblSelected.Content = ((LivePLItem)_ui_dgFilesSCR.SelectedItem).sName;
			}
			else
                _ui_lblSelected.Content = g.Common.sNoSelection.ToLower();
		}
		private void _ui_btnShow_Click(object sender, RoutedEventArgs e)
		{
			LayoutRoot.Visibility = System.Windows.Visibility.Collapsed;
			_sCurrentDir = _ui_tbPath.Text;
			_cPlayer.FilesSCRGetAsync(_sCurrentDir, _aExtensions);
		}
	}
}

