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

using helpers.replica.services.dbinteract;
using helpers.extensions;
using controls.sl;


namespace controls.replica.sl
{
    public partial class Classes : UserControl
    {
        public delegate void ClassesChanged(object sender, SelectionChangedEventArgs e);
        public event ClassesChanged IsClassesChanged;
        private List<Class> _aClassesInitial;
        private List<Class[]> _aAllClassArrays;
        private List<ComboBox> _aAllDdls;
        private bool _bMarkedRed;
        private bool _bChanged;
        private Class[] _aClassesResult
        {
            get
            {
                List<Class> aRetVal = null;
                if (!_aAllDdls.IsNullOrEmpty())
                {
                    aRetVal = new List<Class>();
                    foreach (ComboBox cCB in _aAllDdls)
                        if (cCB.SelectedItem != null)
                            aRetVal.Add((Class)cCB.SelectedItem);
                }
                return null == aRetVal ? null : aRetVal.ToArray();
            }
        }

        public Class[] aClassesSrc;
        public bool bMarkedRed
        {
            get
            {
                if (this.IsEnabled)
                    return _aClassesResult.IsNullOrEmpty() || _bMarkedRed;
                return false;
            }
        }
        public bool bChanged
        {
            get
            {
                return _bChanged;
            }
        }
        public Class[] aSelectedItems
        {
            get
            {
                return _aClassesResult;
            }
            set
            {
                if (aClassesSrc.IsNullOrEmpty())
                    throw new Exception("classes array is empty");
                if (_aAllDdls == null)
                    throw new Exception("call set aSelectedItems only after Show(Class[])");

                if (_aAllDdls.Count > 1)
                {
                    for (int nI = 1; nI < _aAllDdls.Count; nI++)
                        _ui_hlbtnMinus_Click(null, null);
                }

                Class cClass;
                _aClassesInitial = new List<Class>();
                if (null != value)
                    foreach (Class cC in value)
                    {
                        if (null != (cClass = aClassesSrc.FirstOrDefault(o => o.nID == cC.nID)))
                            _aClassesInitial.Add(cClass);
                    }
                if (_aClassesInitial.IsNullOrEmpty())
                    _bMarkedRed = true;
                else
                {
                    _ui_ddlClasses.SelectedItem = _aClassesInitial[0];
                    for (int nI = 1; nI < _aClassesInitial.Count; nI++)
                    {
                        _ui_hlbtnPlus_Click(null, null);
                        _aAllDdls[_aAllDdls.Count - 1].SelectedItem = _aClassesInitial[nI];
                    }
                }
            }
        }
        public Classes()
        {
            InitializeComponent();
            this.IsEnabledChanged += Classes_IsEnabledChanged;
        }

        private void Classes_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsEnabled)
                Mark();
            else if (!_aAllDdls.IsNullOrEmpty())
                foreach(ComboBox cCB in _aAllDdls)
                    cCB.Background = Coloring.Notifications.cButtonNormal;
            else
                _ui_ddlClasses.Background = Coloring.Notifications.cButtonNormal;
        }

        public void Show(Class[] aClassesSrc)
        {
            _bMarkedRed = false;
            _bChanged = false;
            _aAllDdls = new List<ComboBox>();
            _aAllClassArrays = new List<Class[]>();
            if (aClassesSrc.IsNullOrEmpty())
            {
                _bMarkedRed = true;
                return;
            }
            this.aClassesSrc = aClassesSrc;
            _aAllClassArrays.Add(this.aClassesSrc);
            _aAllDdls.Add(_ui_ddlClasses);
            _ui_ddlClasses.ItemsSource = this.aClassesSrc;
            _aClassesInitial = null;
            _bMarkedRed = true;
        }
        private void _ui_hlbtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (_aAllDdls[_aAllDdls.Count - 1].SelectedItem == null)
                return;
            _ui_spMain.Children.Remove(_ui_gPlusMinus);
            _ui_spMain.Children.Add(NextDdlGet());
            _ui_spMain.Children.Add(_ui_gPlusMinus);
            Mark();
        }

        private void _ui_hlbtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (_aAllDdls.Count <= 1)
                return;
            int nLastIndx = _aAllDdls.Count - 1;
            _ui_spMain.Children.Remove(_aAllDdls[nLastIndx]);
            _aAllDdls.RemoveAt(nLastIndx);
            _aAllClassArrays.RemoveAt(nLastIndx);
            _aAllDdls[_aAllDdls.Count - 1].IsEnabled = true;
            Mark();
        }

        private void _ui_ddlClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != IsClassesChanged)
                IsClassesChanged(this, null);
            Mark();
        }

        private ComboBox NextDdlGet()
        {
            ComboBox ddlRetVal = new ComboBox();
            Class[] aC = _aAllClassArrays[_aAllClassArrays.Count - 1].Where(o => o != (Class)_aAllDdls[_aAllClassArrays.Count - 1].SelectedItem).ToArray();
            _aAllDdls[_aAllDdls.Count - 1].IsEnabled = false;
            ddlRetVal.Margin = new Thickness(5, 5, 0, 0);
            ddlRetVal.DisplayMemberPath = "sName";
            ddlRetVal.SelectionChanged += _ui_ddlClasses_SelectionChanged;
            ddlRetVal.ItemsSource = aC;
            _aAllClassArrays.Add(aC);
            _aAllDdls.Add(ddlRetVal);
            return ddlRetVal;
        }

        private void Mark()
        {
            bool bAllMatch = true;
            bool bErr = false;

            if (_aAllDdls.Count != _aClassesInitial.Count)
                bAllMatch = false;
            for (int nI = 0; nI < _aAllDdls.Count; nI++)
            {
                if (bAllMatch && null != _aAllDdls[nI].SelectedItem && ((Class)_aAllDdls[nI].SelectedItem).nID == _aClassesInitial[nI].nID)
                    _aAllDdls[nI].Background = Coloring.Notifications.cButtonNormal;
                else
                {
                    bAllMatch = false;
                    _aAllDdls[nI].Background = Coloring.Notifications.cButtonChanged;
                }

                if (_aAllDdls[nI].SelectedItem == null)
                {
                    bErr = true;
                    _aAllDdls[nI].Background = Coloring.Notifications.cButtonError;
                }
            }
            _bChanged = !bAllMatch;
            _bMarkedRed = bErr;
        }
    }
}
