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
    public partial class AssetsProperties : ChildWindow
    {
        private controls.replica.sl.AssetsList.Tab _eType;
        private MsgBox _cMsgBox;
        private MsgBox _cErrBox;
        private DBInteract _cDBI;
        private AssetSL[] _aAssets;
        private Asset[] _aClassErrors;
        private Clip[] _aRotationsErrors;

        private AssetsProperties()
        {
            InitializeComponent();
            Title = g.Helper.sAssetsProperties.ToLower();
            _aClassErrors = null;
        }
        public AssetsProperties(System.Collections.IList aAssets, controls.replica.sl.AssetsList.Tab eType)
            :this()
        {
            LayoutRoot.Visibility = Visibility.Collapsed;
            _aAssets = new AssetSL[aAssets.Count];
            for (int nI = 0; nI < aAssets.Count; nI++)
                _aAssets[nI] = (AssetSL)aAssets[nI];

            _eType = eType;
            switch (_eType)
            {
                case controls.replica.sl.AssetsList.Tab.Clips:
                    _ui_ddlRotation.IsEnabled = true;
                    break;
                case controls.replica.sl.AssetsList.Tab.Advertisements:
                case controls.replica.sl.AssetsList.Tab.Programs:
                case controls.replica.sl.AssetsList.Tab.Designs:
                case controls.replica.sl.AssetsList.Tab.All:
                default:
                    _ui_ddlRotation.IsEnabled = false;
                    break;
            }

            _cDBI = new DBInteract();
            _cMsgBox = new MsgBox();
            _cErrBox = new MsgBox();
            _cErrBox.Closed += _cErrBox_Closed;
            _cDBI.ClassesGetCompleted += _cDBI_ClassesGetCompleted;
            _cDBI.RotationsGetCompleted += _cDBI_RotationsGetCompleted;
            _cDBI.ClassesSetCompleted += _cDBI_ClassesSetCompleted;
            _cDBI.RotationsSetCompleted += _cDBI_RotationsSetCompleted;
            _cDBI.ClassesGetAsync();

            _ui_ctrClasses.IsClassesChanged += _ui_ctrClasses_IsClassesChanged;
            Mark();
        }

        public void ChangesApply()
        {
            if (null == _aClassErrors)
                _aClassErrors = new Asset[0];
            if (null == _aRotationsErrors)
                _aRotationsErrors = new Clip[0];

            foreach (AssetSL cA in _aAssets)
            {
                if (_ui_ctrClasses.aSelectedItems != null && (_aClassErrors.FirstOrDefault(o => o.nID == cA.nID) == null))
                    cA.aClasses = _ui_ctrClasses.aSelectedItems;
                if (_ui_ddlRotation.SelectedItem != null && (_aRotationsErrors.FirstOrDefault(o => o.nID == cA.nID) == null))
                    cA.cRotation = (IdNamePair)_ui_ddlRotation.SelectedItem;
            }
        }
        private void _cDBI_ClassesGetCompleted(object sender, ClassesGetCompletedEventArgs e)
        {
            try
            {
                if (null != e.Result)
                {
                    _ui_ctrClasses.Show(e.Result);
                }
                else
                    _cMsgBox.Show(g.Common.sErrorDataReceive + " [classes]");

                _cDBI.RotationsGetAsync();
            }
            catch (Exception ex)
            {
                _cDBI.ErrorLoggingAsync("AssetsProperties: " + ex.ToString());
                _cMsgBox.ShowError(ex);
            }
        }
        private void _cDBI_RotationsGetCompleted(object sender, RotationsGetCompletedEventArgs e)
        {
            try
            {
                if (null != e.Result)
                {
                    _ui_ddlRotation.ItemsSource = e.Result;
                }
                else
                    _cMsgBox.Show(g.Common.sErrorDataReceive + " [rotations]");

                ControlsLoad();
                LayoutRoot.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _cDBI.ErrorLoggingAsync("AssetsProperties: " + ex.ToString());
                _cMsgBox.ShowError(ex);
            }
        }

        void ControlsLoad()
        {
            _ui_ctrClasses.aSelectedItems = null;
            _ui_ddlRotation.SelectedItem = null;
            _ui_ddlRotation.Tag = null;
            _ui_ddlRotation.Background = Coloring.Notifications.cButtonNormal;
        }


        private void _ui_ddlRotation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mark();
        }
        private void _ui_ctrClasses_IsClassesChanged(object sender, SelectionChangedEventArgs e)
        {
            Mark();
        }
        private void Mark()
        {
            if (_ui_ddlRotation.SelectedItem!=null)
                _ui_ddlRotation.Mark(true);

            if (_ui_ddlRotation.SelectedItem == null && (_ui_ctrClasses.aSelectedItems == null || _ui_ctrClasses.aSelectedItems.Length == 0))
                _ui_btnOK.IsEnabled = false;
            else
                _ui_btnOK.IsEnabled = true;
        }


        private void _cDBI_ClassesSetCompleted(object sender, ClassesSetCompletedEventArgs e)
        {
            _cErrBox.Name = "classes_err";
            if (e != null && e.Result != null)
            {
                _aClassErrors = e.Result;
                _cErrBox.ShowError(g.Replica.sErrorAssetsProperties1, new ListBox() { ItemsSource = e.Result, DisplayMemberPath = "sName" });
            }
            else
                _cErrBox_Closed(null, null);
        }
        private void _cDBI_RotationsSetCompleted(object sender, RotationsSetCompletedEventArgs e)
        {
            _cErrBox.Name = "rotations_err";
            if (e != null && e.Result != null)
            {
                _aRotationsErrors = e.Result;
                _cErrBox.ShowError(g.Replica.sErrorAssetsProperties2, new ListBox() { ItemsSource = e.Result, DisplayMemberPath = "sName" });
            }
            else
                _cErrBox_Closed(null, null);
        }
        private void _cErrBox_Closed(object sender, EventArgs e)
        {
            if (_cErrBox.Name == "classes_err")
            {
                if (_ui_ddlRotation.SelectedItem != null)
                {
                    List<Clip> aCs = new List<Clip>();
                    Clip cC;
                    foreach (AssetSL cA in _aAssets)
                    {
                        cC = new Clip() { nID = cA.nID, sName = cA.sName, cRotation = (IdNamePair)_ui_ddlRotation.SelectedItem };
                        aCs.Add(cC);
                    }
                    _cDBI.RotationsSetAsync(aCs.ToArray());
                }
                else
                    _cDBI_RotationsSetCompleted(null, null);
            }
            if (_cErrBox.Name == "rotations_err")
            {
                this.DialogResult = true;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ui_ctrClasses.aSelectedItems != null && _ui_ctrClasses.aSelectedItems.Length > 0)
            {
                List<Asset> aCs = new List<Asset>();
                Asset cC;
                foreach (AssetSL cA in _aAssets)
                {
                    cC = new Asset() { nID = cA.nID, sName = cA.sName, aClasses = _ui_ctrClasses.aSelectedItems };
                    aCs.Add(cC);
                }
                _cDBI.ClassesSetAsync(aCs.ToArray());
            }
            else
            {
                _cDBI_ClassesSetCompleted(null, null);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

    }
}

