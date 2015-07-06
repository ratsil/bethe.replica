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

using IC = scr.services.ingenie.cues;
using controls.childs.sl;
using helpers.extensions;

namespace scr
{
	public partial class editors : Page
	{
		private Progress _dlgProgress;
		private IC.CuesSoapClient _cCues;
		private FontFamily _cFontFamily = null;
		private int _cFontSize;
		private bool _bInitialized = false;
		private string _sFilename;
		public editors()
		{
			InitializeComponent();

			_cCues = new IC.CuesSoapClient();
			_cCues.AddTextToRollCompleted += new EventHandler<IC.AddTextToRollCompletedEventArgs>(_cCues_AddTextToRollCompleted);

			_dlgProgress = new Progress();
			_ui_btnAddTextToBottomString.IsEnabled = false;
			_ui_lblError.Content = "";
			_cFontFamily = new FontFamily("Verdana");
			_cFontSize = 14;

			services.preferences.Template cTemp = App.cPreferences.aTemplates.FirstOrDefault(o => o.sFile.EndsWith("crawl_bottom.xml"));
			if (null != cTemp)
				_sFilename = cTemp.sFile.Replace("{channel}", App.cPreferences.aPresets[0].sChannel);
			else
			{
				_sFilename = null;
				_ui_lblError.Content = "ERROR - filename not found";
			}

			_bInitialized = true;
		}

		void _cCues_AddTextToRollCompleted(object sender, IC.AddTextToRollCompletedEventArgs e)
		{
			if (e.Result)
				_ui_lblError.Content = "";
			else
				_ui_lblError.Content = "ERROR";
			_dlgProgress.Close();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}

		private void _ui_btnAddTextToBottomString_Click(object sender, RoutedEventArgs e)
		{
			if (null == _sFilename)
				_ui_lblError.Content = "ERROR - filename not found";
			else
			{
				_cCues.AddTextToRollAsync("Template, " + _sFilename, _ui_tbLine.Text);
				_dlgProgress.Show();
			}
		}

		//private void _ui_tbText_TextChanged(object sender, TextChangedEventArgs e)
		//{
		//    if (_ui_lblError.Content != "")
		//        _ui_lblError.Content = "";

		//    if (null != _ui_tbText.Text && _ui_tbText.Text.Length > 0)
		//        _ui_btnAddTextToBottomString.IsEnabled = true;
		//    else
		//        _ui_btnAddTextToBottomString.IsEnabled = false;
		//}

		private void _ui_tbText_ContentChanged(object sender, ContentChangedEventArgs e)
		{
			if (_ui_lblError.Content != "")
				_ui_lblError.Content = "";

			if (PreviewText() != "")
				_ui_btnAddTextToBottomString.IsEnabled = true;
			else
				_ui_btnAddTextToBottomString.IsEnabled = false;
		}

		private string PreviewText()
		{
			string sText = "";
			if (_bInitialized)
			{
				if (_ui_tbLine.FontFamily != _cFontFamily)
					_ui_tbLine.FontFamily = _cFontFamily;
				if (_ui_tbLine.FontSize != _cFontSize)
					_ui_tbLine.FontSize = _cFontSize;
				try
				{
					foreach (Block cB in _ui_tbText.Blocks)
						if (0 < (cB as Paragraph).Inlines.Count)
							sText += ((cB as Paragraph).Inlines[0] as Run).Text + "\n";
                    _ui_tbLine.Text = sText.Replace("\n\n", "                           ").Replace("\n", " ").Remove("\r");
				}
				catch { }

				//_ui_tbText.Blocks.Clear();
				//_ui_tbText.Blocks.Add(_ui_tbText.Blocks[0]);
			}
			return sText;
		}

	}
}
