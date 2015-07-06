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
	public partial class MediaPreview : ChildWindow
	{
		public MediaPreview()
		{
			InitializeComponent();
		}
		public void Show(string sFile)
		{
			_ui_ctrlMediaPreview.Init(sFile);
			base.Show();
		}
		new public void Show()
		{
			throw new NotImplementedException();
		}
		//protected override void OnClosed(EventArgs e)
		//{
		//    base.OnClosed(e);
		//}

		//private void ChildWindow_Closed(object sender, EventArgs e)
		//{
		//    this.DialogResult = false;
		//}


		//private void OKButton_Click(object sender, RoutedEventArgs e)
		//{
		//    this.DialogResult = true;
		//}

		//private void CancelButton_Click(object sender, RoutedEventArgs e)
		//{
		//    this.DialogResult = false;
		//}
	}
}

