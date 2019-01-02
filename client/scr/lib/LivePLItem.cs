using System;
using System.Net;
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
    //может хранить как одиночный item так и целый блок как item - для облегчения переключения между сокращенным и подробным режимами
    public partial class LivePLItem : IP.PlaylistItem
    {
        public enum CacheType
        {
            cached,
            in_progress,
            in_queue,
            not_cached
        }
        public string sDuration { get; set; }
        public string sStart { get; set; }
        public PLIType eType
        { get { return _eType; } }
        public IP.Class[] aClasses
        {
            get
            {
                switch (eType)
                {
                    case PLIType.AdvBlock:
                        return new IP.Class[0];
                    case PLIType.AdvBlockItem:
                        return _cAdvertSCR.aClasses;
                    case PLIType.Clip:
                        return _cClipSCR.aClasses;
                    case PLIType.File:
                        return new IP.Class[0];
                    case PLIType.JustString:
                        return new IP.Class[0];
                    default:
                        return null;
                }
            }
        }
        public CacheType eCacheType
        {
            get
            {
                if (null == sCachedInfo)
                    return CacheType.not_cached;
                else if (sCachedInfo.ToLower().StartsWith("ok"))
                    return CacheType.cached;
                else if (eType == PLIType.AdvBlock && sCachedInfo.StartsWith("cached") || eType != PLIType.AdvBlock && sCachedInfo.Contains("%"))
                    return CacheType.in_progress;
                else if (eType != PLIType.AdvBlock && sCachedInfo.StartsWith("in "))
                    return CacheType.in_queue;
                else
                    return CacheType.not_cached;
            }
        }
        private string _sCachedInfo;
        public string sCachedInfo
        {
            get
            {
                if (null != _sCachedInfo)
                    return _sCachedInfo;

                if (eType == PLIType.AdvBlockItem)
                {
                    _sCachedInfo = cAdvertSCR.sCopyPercent.IsNullOrEmpty() ? "NOT CACHED" : cAdvertSCR.sCopyPercent;
                }
                else if (eType == PLIType.JustString)
                    _sCachedInfo = "";
                else if (eType == PLIType.AdvBlock)
                {
                    int nCached = 0;
                    foreach (LivePLItem cPLI in aItemsInThisBlock)
                    {
                        if (cPLI.eCacheType == CacheType.cached)
                            nCached++;
                    }
                    if (nCached == 0)
                        _sCachedInfo = "NOT CACHED";
                    else if (nCached >= aItemsInThisBlock.Count - 1)
                        _sCachedInfo = "OK";
                    else
                        _sCachedInfo = "cached " + nCached + " of " + (aItemsInThisBlock.Count - 1);
                }
                else
                    _sCachedInfo = "wrong item";
                return _sCachedInfo;
            }
        }
        public string sStartPlanned
        {
            get
            {
                if (eType == PLIType.AdvBlockItem)
                {
                    return cAdvertSCR.dtStartPlanned < DateTime.MinValue.AddYears(1) ? "" : cAdvertSCR.sStartPlanned;
                }

                if (eType == PLIType.AdvBlock && null != aItemsInThisBlock && aItemsInThisBlock.Count > 1)
                {
                    return aItemsInThisBlock[1].sStartPlanned;
                }
                return "";
            }
        }
        new public string sFilename
        {
            get
            {
                if (eType == PLIType.AdvBlock)
                {
                    return sName;
                }
                return base.sFilename;
            }
            set
            {
                if (eType != PLIType.AdvBlock)
                {
                    base.sFilename = value;
                }
            }
        }
        public int nCountInPlayListFragment;
        public LivePLItem cBlock;   // для отсылки на блок
        private List<LivePLItem> _aItemsInThisBlock = null;
        public List<LivePLItem> aItemsInThisBlock
        {
            set
            {
                value[0].bIsFirstItemInBlock = true;
                _aItemsInThisBlock = value;
                sName = g.SCR.sNotice21;
                sFilename = "";
                if (DateTime.MinValue < value[1].dtStart)
                    dtStart = value[1].dtStart;
                if (null != value[0]._cAdvertSCR)
                    sName += value[0]._cAdvertSCR.sStartPlanned;
                foreach (LivePLItem cLPLI in _aItemsInThisBlock)
                {
                    if (PLIType.JustString == cLPLI._eType)
                        continue;
                    cLPLI.cBlock = this;
                    nFramesQty += cLPLI.nFramesQty;
                    if (!cLPLI.bFileExist)
                        bFileExist = false;
                }
            }
            get { return _aItemsInThisBlock; }
        }
        public void RemoveBlockItem(LivePLItem cBlockItem)
        {
            if (null != _aItemsInThisBlock)
            {
                _aItemsInThisBlock.Remove(cBlockItem);
                nFramesQty = 0;
                LivePLItem tmp = _aItemsInThisBlock[0];
                foreach (LivePLItem cLPLI in _aItemsInThisBlock)
                    if (PLIType.JustString != cLPLI.eType)
                        nFramesQty += cLPLI.nFramesQty;
                tmp.nFramesQty = nFramesQty;
            }
        }
        public IP.Advertisement cAdvertSCR
        {
            set
            {
                _cAdvertSCR = value;
                string sSpace = _eType == PLIType.AdvBlockItem ? "        " : "";
                sName = sSpace + value.sName;
                sFilename = value.sFilename;
                sFilenameFull = value.sStoragePath + value.sFilename;
                nFramesQty = value.nFramesQty;
                sStorageName = value.sStorageName;
                bFileExist = value.bFileExist;
            }
            get
            {
                return _cAdvertSCR;
            }
        }
        public IP.Clip cClipSCR
        {
            set
            {
                _cClipSCR = value;
                sName = value.sName;
                sFilename = value.sFilename;
                sFilenameFull = value.sStoragePath + value.sFilename;
                sStorageName = value.sStorageName;
                nFramesQty = value.nFramesQty;
            }
            get
            {
                return _cClipSCR;
            }
        }
        public long nFramesQty
        {
            get
            {
                return _nFramesQty;
            }
            set
            {
                _nFramesQty = value;
                sDuration = "   " + value.ToFramesString();
            }
        }
        private DateTime _dtStart;
        public DateTime dtStart
        {
            set
            {
                if (PLIType.JustString != _eType)
                    sStart = value.ToString("HH:mm:ss");     // yyyy.MM.dd hh:mm:ss
                _dtStart = value;
            }
            get
            {
                if (DateTime.MinValue == dtStartReal)
                    return _dtStart;
                else
                    return dtStartReal;
            }
        }
        private static int _nMaxID = 1;

        public LivePLItem()
        {
            bFileExist = true;
            dtStartReal = DateTime.MinValue;
            dtStopReal = DateTime.MinValue;
        }
        public LivePLItem(IP.Advertisement cPLI)
            : this()
        {
            _eType = PLIType.AdvBlockItem;
            cAdvertSCR = cPLI;
        }
        public LivePLItem(string sName, string sFilename, bool bIsImage)
            : this()
        {
            _eType = PLIType.File;
            bFileIsImage = bIsImage;
            this.sName = sName;
            this.sFilename = sFilename;
        }
        public LivePLItem(IP.Clip cClip)
            : this()
        {
            _eType = PLIType.Clip;
            cClipSCR = cClip;
        }
        public LivePLItem(List<LivePLItem> aBlock)
            : this()
        {
            aItemsInThisBlock = aBlock;
            List<LivePLItem> tmp = new List<LivePLItem>();
            tmp.Add(new LivePLItem(sName, _nFramesQty, this));
            tmp.AddRange(_aItemsInThisBlock);
            _aItemsInThisBlock = tmp;
            _eType = PLIType.AdvBlock;
        }
        public LivePLItem(string sName, long nDuration, LivePLItem cLPLI)
            : this()
        {
            this.sName = sName;
            cBlock = cLPLI;
            nFramesQty = nDuration;
            _eType = PLIType.JustString;
        }

        public void SetNewId()
        {
            if (0 == nID)
                nID = _nMaxID++;
        }
        static public List<LivePLItem> SetIDs(List<LivePLItem> aLPLIs)
        {
            foreach (LivePLItem cLPLI in aLPLIs)
                cLPLI.SetNewId();
            return aLPLIs;
        }
        static public LivePLItem SetID(LivePLItem cLPLI)
        {
            cLPLI.SetNewId();
            return cLPLI;
        }
        static public LivePLItem LivePLItemGet(IP.PlaylistItem cPLI)
        {
            LivePLItem cLPLI;
            switch (cPLI._eType)
            {
                case PLIType.AdvBlockItem:
                    cLPLI = new LivePLItem(cPLI._cAdvertSCR);
                    cLPLI._cClipSCR = cPLI._cClipSCR;
                    break;
                case PLIType.Clip:
                    cLPLI = new LivePLItem(cPLI._cClipSCR);
                    cLPLI._cAdvertSCR = cPLI._cAdvertSCR;
                    break;
                case PLIType.File:
                    cLPLI = new LivePLItem(cPLI.sName, cPLI.sFilename, cPLI.bFileIsImage);
                    break;
                case PLIType.AdvBlock:
                case PLIType.JustString:
                default:
                    return null;
            }
            cLPLI.bIsFirstItemInBlock = cPLI.bIsFirstItemInBlock;
            cLPLI.bFileExist = cPLI.bFileExist;
            cLPLI.nFramesQty = cPLI._nFramesQty;
            cLPLI.sStorageName = cPLI.sStorageName;
            cLPLI.nTransDuration = cPLI.nTransDuration;
            cLPLI.nEndTransDuration = cPLI.nEndTransDuration;
            cLPLI.dtStartReal = cPLI.dtStartReal;
            cLPLI.dtStopReal = cPLI.dtStopReal;
            cLPLI.nAtomHashCode = cPLI.nAtomHashCode;
            cLPLI.nEffectID = cPLI.nEffectID;
            cLPLI.sFilenameFull = cPLI.sFilenameFull;
            cLPLI.nID = cPLI.nID;
            cLPLI.bFileIsImage = cPLI.bFileIsImage;
            return cLPLI;
        }
        static public IP.PlaylistItem PLItemGet(LivePLItem cLPLI)
        {
            IP.PlaylistItem cRetVal = new IP.PlaylistItem()
            {
                _cAdvertSCR = cLPLI._cAdvertSCR,
                _cClipSCR = cLPLI._cClipSCR,
                _eType = cLPLI._eType,
                _nFramesQty = cLPLI._nFramesQty,
                bFileExist = cLPLI.bFileExist,
                bIsFirstItemInBlock = cLPLI.bIsFirstItemInBlock,
                dtStartReal = cLPLI.dtStartReal,
                dtStopReal = cLPLI.dtStopReal,
                nAtomHashCode = cLPLI.nAtomHashCode,
                nEffectID = cLPLI.nEffectID,
                nEndTransDuration = cLPLI.nEndTransDuration,
                nTransDuration = cLPLI.nTransDuration,
                sFilename = cLPLI.sFilename,
                sFilenameFull = cLPLI.sFilenameFull,
                sName = cLPLI.sName,
                sStorageName = cLPLI.sStorageName,
                nID = cLPLI.nID,
                bFileIsImage = cLPLI.bFileIsImage
            };
            return cRetVal;
        }
        static public List<LivePLItem> GetBlocksFromSingle(List<LivePLItem> aSingle)
        {
            List<LivePLItem> aRetVal = new List<LivePLItem>();
            List<LivePLItem> aBlock = null;
            int nIndex = 0;
            while (nIndex < aSingle.Count)
            {
                if (aSingle[nIndex].bIsFirstItemInBlock)
                {
                    aBlock = new List<LivePLItem>();
                    aBlock.Add(aSingle[nIndex++]);
                    while (nIndex < aSingle.Count && !aSingle[nIndex].bIsFirstItemInBlock && aSingle[nIndex]._eType == PLIType.AdvBlockItem)
                    {
                        aBlock.Add(aSingle[nIndex++]);
                    }
                    if (aBlock.Count < 2)
                        throw new Exception("Block size must be greater than 1 [block_count=" + aBlock.Count + "][single_count=" + aSingle.Count + "]");
                    aRetVal.Add(new LivePLItem(aBlock));
                }
                else
                {
                    aRetVal.Add(aSingle[nIndex++]);
                }
            }

            return aRetVal;
        }
        static public List<LivePLItem> GetSingleFromBlocks(List<LivePLItem> aBlocks)
        {
            List<LivePLItem> aRetVal = new List<LivePLItem>();
            foreach (LivePLItem cLPLI in aBlocks)
            {
                if (cLPLI._eType == PLIType.AdvBlock)
                    aRetVal.AddRange(cLPLI._aItemsInThisBlock);
                else if (cLPLI._eType == PLIType.JustString)
                    continue;
                else
                    aRetVal.Add(cLPLI);
            }
            return aRetVal;
        }
        internal static List<LivePLItem> RemoveJustStrings(List<LivePLItem> _aSelectedClips)
        {
            List<LivePLItem> aRetVal = new List<LivePLItem>();
            foreach (LivePLItem cLPLI in _aSelectedClips)
            {
                if (cLPLI._eType != PLIType.JustString)
                    aRetVal.Add(cLPLI);
            }
            return aRetVal;
        }
    }
}
