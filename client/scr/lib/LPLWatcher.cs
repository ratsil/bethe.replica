using System;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Navigation;

using System.Threading;
using helpers.replica.services.dbinteract;
using scr.services.ingenie.cues;
using IC = scr.services.ingenie.cues;
using scr.services.ingenie.player;
using IP = scr.services.ingenie.player;
using scr.childs;
using controls.replica.sl;
using controls.sl;
using controls.childs.sl;
using controls.extensions.sl;
using helpers.sl;
using helpers.extensions;
using scr.services.preferences;
using scr.Views;
using DBI = helpers.replica.services.dbinteract;

using g = globalization;

namespace scr
{
    public class LPLWatcher
    {
        static public SCR _cPage;
        public DateTime dtPLStarted;
        public bool _bTimerStarted = false;
        public long nCurrentRemain;
        public long nTotalRemain;
        public long nCurrentPast;
        public long nTotalPast;
        public long nTotalDuration;
        public string sCurrentRemain;
        public string sTotalRemain;
        public string sCurrentPast;
        public string sTotalPast;
        public string sTotalDuration
        {
            set
            {
                _cPage._ui_lblTotalDuration.Content = value;
            }
        }
        public string sCurrentDuration;
        private List<long> _aInTitled, _aOutTitled, _aRefreshed, _aSmokingTitred, _aAgeTitred;
        private List<string> _aWasAutomated;
        private List<PLITemplatePair> _aDontAutomate;
        private LivePLItem _cCurrentLPLItem = null;
        public LivePLItem cCurrentLPLItem
        {
            get
            { return _cCurrentLPLItem; }
            set
            {
                if (value != _cCurrentLPLItem)
                {
                    _cPreviousLPLItem = _cCurrentLPLItem;
                    _cCurrentLPLItem = value;
                }
            }
        }
        private LivePLItem _cNextLPLItem;
        private LivePLItem _cPreviousLPLItem = null;
        public LivePLItem cNextLPLItem
        {
            get
            { return _cNextLPLItem; }
        }
        private System.Windows.Threading.DispatcherTimer _cTimerForWatcher;
        private class PLITemplatePair
        {
            public LivePLItem cPLI;
            public scr.services.preferences.Template cTemplate;
            public PLITemplatePair()
            { }
        }
        public LPLWatcher(SCR cPage)
        {
            _cTimerForWatcher = new System.Windows.Threading.DispatcherTimer();
            _cTimerForWatcher.Tick += new EventHandler(_cTimerForWatcher_Tick);
            _cTimerForWatcher.Interval = new System.TimeSpan(0, 0, 0, 0, 1000);
            _cPage = cPage;
            dtPLStarted = DateTime.MinValue;
            _aWasAutomated = new List<string>();
            _aDontAutomate = new List<PLITemplatePair>();
            _aInTitled = new List<long>();
            _aOutTitled = new List<long>();
            _aRefreshed = new List<long>();
            _aSmokingTitred = new List<long>();
            _aAgeTitred = new List<long>();
        }
        public void TimerStart()
        {
            try
            {
                if (!_bTimerStarted && 0 < _cPage._aLivePLTotal.Count)
                {
                    _bTimerStarted = true;
                    if (DateTime.MinValue == _cPage._aLivePLTotal[0].dtStartReal)
                    {
                        dtPLStarted = DateTime.Now;
                    }
                    else
                    {
                        dtPLStarted = _cPage._aLivePLTotal[0].dtStartReal;
                        CurrentParamsSet(false);
                    }
                    PlaylistRecalculate();
                    _cTimerForWatcher.Start();
                }
            }
            catch (Exception ex)
            {
                _cPage.WritePlayerError(ex);
            }
        }
        public long GetCurrentClipAssetID
        {
            get
            {
                long nRetVal = 0;
                if (null != cCurrentLPLItem && null != cCurrentLPLItem.cClipSCR)
                    nRetVal = cCurrentLPLItem.cClipSCR.nID;
                return nRetVal;
            }
        }
        public void PlaylistRecalculate()
        {
            if (null == _cPage._aLivePLTotal || 0 == _cPage._aLivePLTotal.Count)
            {
                nTotalDuration = 0;
                sTotalDuration = "";
            }
            lock (_cPage._aLivePLTotal)
            {
                long nRetVal = 0;
                if (DateTime.MinValue != _cPage._aLivePLTotal[0].dtStartReal)
                    dtPLStarted = _cPage._aLivePLTotal[0].dtStartReal;
                DateTime dtLastStopReal = DateTime.MinValue;
                DateTime dtPrevousStop = dtPLStarted;
                foreach (LivePLItem cLPLI in _cPage._aLivePLTotal)
                {
                    if (DateTime.MinValue < dtPLStarted)
                        cLPLI.dtStart = dtPrevousStop;
                    dtPrevousStop = cLPLI.dtStopReal == DateTime.MinValue ? dtPrevousStop.AddMilliseconds(cLPLI._nFramesQty * 40) : cLPLI.dtStopReal; //FPS
                    if (0 == nRetVal && DateTime.MinValue != cLPLI.dtStopReal)
                    {
                        dtLastStopReal = cLPLI.dtStopReal;
                        continue;
                    }
                    if (0 == nRetVal && DateTime.MinValue != dtLastStopReal)
                        nRetVal = (long)(dtLastStopReal.Subtract(dtPLStarted).TotalMilliseconds) / 40; //FPS
                    nRetVal += cLPLI._nFramesQty;
                }
                nTotalDuration = nRetVal;
                sTotalDuration = nRetVal.ToFramesString(false);
            }
        }
        private void CurrentParamsSet(bool bRefreshOnCurrentChanged)
        {
            if (DateTime.MaxValue == dtPLStarted)
                return;
            long nTotalDelta = (long)(DateTime.Now.Subtract(dtPLStarted).TotalMilliseconds) / 40; //FPS
            long nDelta = 0;
            nTotalPast = nTotalDelta;
            nTotalRemain = nTotalDuration - nTotalPast;
            lock (_cPage._aLivePLTotal)
                foreach (LivePLItem cLPLI in _cPage._aLivePLTotal)
                {
                    if (DateTime.MinValue != cLPLI.dtStopReal)
                    {
                        continue;
                    }
                    if (0 == nDelta)
                        nDelta = (long)(DateTime.Now.Subtract(cLPLI.dtStart).TotalMilliseconds) / 40; //FPS
                    nDelta -= cLPLI._nFramesQty;
                    if (0 > nDelta)
                    {
                        nCurrentRemain = -nDelta;
                        nCurrentPast = cLPLI._nFramesQty - nCurrentRemain;
                        if (cCurrentLPLItem != cLPLI)
                        {
                            cCurrentLPLItem = cLPLI;
                            if (bRefreshOnCurrentChanged)
                                _cPage.ShowPL();
                        }

                        int nCurIndx = _cPage._aLivePLTotal.IndexOf(cLPLI);
                        if (_cPage._aLivePLTotal.Count > nCurIndx + 1)
                            _cNextLPLItem = _cPage._aLivePLTotal[nCurIndx + 1];
                        else
                            _cNextLPLItem = null;
                        return;
                    }
                }
        }
        void ConvertToString()
        {
            sCurrentRemain = nCurrentRemain.ToFramesString(false);
            sTotalRemain = nTotalRemain.ToFramesString(false);
            sCurrentPast = nCurrentPast.ToFramesString(false);
            sTotalPast = nTotalPast.ToFramesString(false);
            sCurrentDuration = cCurrentLPLItem._nFramesQty.ToFramesString(false);
        }
        void _cTimerForWatcher_Tick(object sender, EventArgs e)
        {
            try
            {
                scr.services.preferences.Template[] aTemplatesToAutomate = null;
                if (TemplateButton.Status.Started == _cPage._ui_ctrTB_PlayList.eStatus)
                {
                    CurrentParamsSet(true);
                    if (PLIType.Clip == cCurrentLPLItem.eType && 1500 < nCurrentPast && !_aRefreshed.Contains(cCurrentLPLItem.nID))   // 1500
                    {                                                   // загрузка плейлиста из IG на клипах
                        _cPage._cPlayer_PlaylistItemsGet();
                        _aRefreshed.Add(cCurrentLPLItem.nID);
                    }
                    if ((PLIType.File == cCurrentLPLItem.eType || PLIType.AdvBlockItem == cCurrentLPLItem.eType) && 220 < cCurrentLPLItem._nFramesQty && 100 < nCurrentPast && !_aRefreshed.Contains(cCurrentLPLItem.nID))
                    {                                                   // загрузка плейлиста из IG на рекламе
                        _cPage._cPlayer_PlaylistItemsGet();
                        _aRefreshed.Add(cCurrentLPLItem.nID);
                    }
                    if (PLIType.AdvBlockItem == cCurrentLPLItem.eType || _cCurrentLPLItem == null)   // блокировка скипа
                        _cPage._ui_ctrTB_PlayList.bSkipBtnIsEnabled = false;
                    else if (_cPage._ui_ctrTB_PlayList.bSkipBtnIsEnabled != true)
                        _cPage._ui_ctrTB_PlayList.bSkipBtnIsEnabled = true;
                    ConvertToString();
                    _cPage.RenewTimers();


                    if (null != _cPage._cShiftCurrent && _cPage.IsAirGoingNow)
                    { // если идет прямой эфир
                        if (PLIType.AdvBlockItem == cCurrentLPLItem.eType)
                        { // идет реклама
                          // надо полноценно заменять ручное управление на авто через префы. Чат и титры тоже фигарить как-то так.
                          // меняем. пока оставил разделение на ифы.

                        }

                        if (PLIType.File == cCurrentLPLItem.eType)
                        {// идет пользовательский файл какой-то

                        }

                        if (PLIType.Clip == cCurrentLPLItem.eType && TemplateButton.Status.Started == _cPage._ui_ctrTB_PlayList.eStatus)
                        { // идет клип в плейлисте


                        }
                        AutomateTemplates();   //TODO потом снабдить обработкой конфликтов
                    }
                }
            }
            catch (Exception ex)
            {
                _cPage.WritePlayerError(ex);
            }
        }
        private void AutomateTemplates()
        {
            long nPresetID = _cPage._cPresetSelected.nID;
            int nDurSafe;
            string sHash;
            scr.services.preferences.Parameters cParam;
            string sLog = "";
            foreach (scr.services.preferences.Template cT in App.cPreferences.aTemplates.Where(o => o.aOffsets != null))
            {
                if (3000 < nCurrentRemain && 3045 > nCurrentRemain && null != cT.sFile && cT.sFile.Contains("chat")) { sLog += "AutomateTemplates: chat offsets: [_aDontAutomate.count=" + _aDontAutomate.Count + "]"; }

                if (null != _aDontAutomate.FirstOrDefault(o => o.cPLI.nID == cCurrentLPLItem.nID && o.cTemplate.eBind.ToString() == cT.eBind.ToString()))
                    continue;

                if (sLog.Length > 0) { sLog += "[after _aDontAutomate]"; }

                cParam = cT.aParameters.Get(nPresetID);

                if (null == cParam)
                    continue;

                if (sLog.Length > 0) { sLog += "[cParam:enabl=" + cParam.bIsEnabled + ";autos=" + cParam.bAutostart + "]"; }

                foreach (Offset cOffset in cT.aOffsets)
                {
                    if (sLog.Length > 0 && cOffset.nPresetID == 1 && cOffset.nOffsetOut < 0) { sLog += "[cOffset.nOffsetOut=" + cOffset.nOffsetOut + "][peset=" + nPresetID + "][curtype=" + cCurrentLPLItem.eType + "][nextype=" + (null == cNextLPLItem ? "NULL" : "" + cNextLPLItem.eType) + "]"; }

                    if (!cParam.bIsEnabled || !cOffset.IsOffsetFeats(nPresetID, cCurrentLPLItem, cNextLPLItem, _cPreviousLPLItem))
                        continue;

                    if (sLog.Length > 0 && cOffset.nPresetID == 1 && cOffset.nOffsetOut < 0) { sLog += "[pass-1]"; }

                    nDurSafe = int.MaxValue == cOffset.nDurationSafe ? 0 : cOffset.nDurationSafe;
                    if (cCurrentLPLItem._nFramesQty <= nDurSafe)
                        continue;

                    if (sLog.Length > 0 && cOffset.nPresetID == 1 && cOffset.nOffsetOut < 0) { sLog += "[pass-2]"; }

                    if (cOffset.bDoOnlyIfLast && !cOffset.IsOffsetFeats(nPresetID, cNextLPLItem, null, cCurrentLPLItem))
                        continue;

                    if (sLog.Length > 0 && cOffset.nPresetID == 1 && cOffset.nOffsetOut < 0) { sLog += "[pass-3][sHash=" + (sHash = cCurrentLPLItem.nID + "_" + cT.eBind.ToString() + "_" + "out=" + cOffset.nOffsetOut.ToString()) + "][was=" + _aWasAutomated.Contains(sHash) + "][was.count=" + _aWasAutomated.Count + "]"; _cPage.WritePlayerNotice(sLog); sLog = ""; }

                    if (int.MaxValue > cOffset.nOffsetIn && (cOffset.nOffsetIn >= 0 && cOffset.nOffsetIn < nCurrentPast || cOffset.nOffsetIn < 0 && -cOffset.nOffsetIn > nCurrentRemain))
                    {
                        sHash = cCurrentLPLItem.nID + "_" + cT.eBind.ToString() + "_" + "in=" + cOffset.nOffsetIn.ToString();
                        if (!_aWasAutomated.Contains(sHash) && cParam.bAutostart)
                        {
                            _aWasAutomated.Add(sHash);
                            _cPage.StartTemplate(cT);
                        }
                    }
                    if (int.MaxValue > cOffset.nOffsetOut && (cOffset.nOffsetOut >= 0 && cOffset.nOffsetOut < nCurrentPast || cOffset.nOffsetOut < 0 && -cOffset.nOffsetOut > nCurrentRemain))
                    {
                        sHash = cCurrentLPLItem.nID + "_" + cT.eBind.ToString() + "_" + "out=" + cOffset.nOffsetOut.ToString();
                        if (!_aWasAutomated.Contains(sHash) && cParam.bAutostart)
                        {
                            _aWasAutomated.Add(sHash);
                            if (cT.sFile.Contains("chat"))
                                _cPage.WritePlayerNotice("AutomateTemplates: stopping chat [curremain=" + nCurrentRemain + "][cur=" + cCurrentLPLItem.sFilename + "]");
                            _cPage.StopTemplate(cT);
                        }
                    }
                }
            }
            if (!sLog.IsNullOrEmpty())
                _cPage.WritePlayerNotice(sLog);
        }
        public void TurnOffTemplateOnCurrentPLI(TemplateButton cTB)
        {
            services.preferences.Template cT = App.cPreferences.aTemplates.FirstOrDefault(o => o.sFile == cTB.sFile);
            if (null != cT && null != cCurrentLPLItem)
                _aDontAutomate.Add(new PLITemplatePair() { cPLI = cCurrentLPLItem, cTemplate = cT });
        }
        internal void Dispose()
        {
            //if (TemplateButton.Status.Started == _cPage._ui_ctrTB_Template1Chat.eStatus)
            //    _cPage._ui_ctrTB_Template1Chat.Click();
            _cPage.TimersOff();
            _cTimerForWatcher.Stop();
            _cPage._cLPLWatcher = null;
        }
    }
}
