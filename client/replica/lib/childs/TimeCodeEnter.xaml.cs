using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using helpers.extensions;
using g = globalization;

namespace controls.childs.replica.sl
{
	public partial class TimeCodeEnter : ChildWindow
	{
		public long nInitialCode = 0;
		public long nResultCode
		{
			get
			{
				return _ui_tbCode.Text.ToFrames(false);
			}
		}

		public TimeCodeEnter()
		{
			InitializeComponent();
            Title = g.Helper.sTimings.ToLower();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			//_ui_tbCode.Text = nInitialCode.ToFramesString(false, false, false, true);   //легче новый код вбить...

			_ui_tbCode.TextChanged += new TextChangedEventHandler(_ui_tbCode_TextChanged);
			_ui_tbCode.KeyDown += new KeyEventHandler(_ui_tbCode_KeyDown);
			_ui_tbCode.Text = "0";
		}
		
		void _ui_tbCode_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				this.DialogResult = true;
			if (e.Key == Key.Escape)
				this.DialogResult = false;
		}

		void _ui_tbCode_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox cTimeCode = (TextBox)sender;
			if (1 > cTimeCode.Text.Length)
			{
				cTimeCode.Text = "";
				return;
			}

			string sIn = "", s0 = "00", s1 = "00", s2 = "00";   // "00:00:00"
			int ni;
			for (ni = 0; ni < cTimeCode.Text.Length; ni++)
				if (cTimeCode.Text[ni] >= '0' && cTimeCode.Text[ni] <= '9')
					sIn += cTimeCode.Text[ni];
			for (ni = 0; ni < sIn.Length; ni++)
				if (sIn[ni] != '0')
					break;
			sIn = sIn.Substring(ni, sIn.Length - ni > 6 ? 6 : sIn.Length - ni);

			if (sIn.Length > 1)
			{
				s2 = sIn.Substring(sIn.Length - 2, 2);
				if (sIn.Length > 3)
				{
					s1 = sIn.Substring(sIn.Length - 4, 2);
					if (sIn.Length > 5)
						s0 = sIn.Substring(sIn.Length - 6, 2);
					else if (sIn.Length == 5)
						s0 = "0" + sIn.Substring(sIn.Length - 5, 1);
				}
				else if (sIn.Length == 3)
				{
					s1 = "0" + sIn.Substring(sIn.Length - 3, 1);
				}
			}
			else if (sIn.Length == 1)
			{
				s2 = "0" + sIn;
			}
			cTimeCode.Text = s0 + ":" + s1 + ":" + s2;
			cTimeCode.SelectionStart = cTimeCode.Text.Length;
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}

		private void ChildWindow_Closed(object sender, EventArgs e)
		{

		}
	}
}

