using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using helpers;
using helpers.extensions;
using helpers.replica.mam;
using helpers.replica.pl;
using SIO = System.IO;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace replica.failover
{
    public class Failover : Playlist.IInteract, Player.IInteract, Cues.IInteract, replica.cues.Template.IInteract
    {
        public enum ErrorTarget
        {
            dbi_plug,
            dbi_framesinitial,
            playlist,
            dbi_commands,
            dbi_statuses,
            dbi_cues,
            dbi_toatalpl,
            dbi_shortpl,
            dbi_classes,
        }
        static public Dictionary<ErrorTarget, DateTime> ahErrors;

        [Serializable]
        class FailoverInfo
        {
            public int nCuesCount;
            public int nPLBigCount;
            public int nCheckedFilesCount;
            public int nHistoryCount;
            public int nGapCount;

            private Dictionary<string, DateTime> _ahCheckedFiles;
            private Dictionary<string, long> _ahCheckedFilesDurs;

            private IdNamePair[] _aStatuses;
            private LinkedList<PlaylistItem> _aPlaylistBig;
            private Dictionary<long, long> _ahFrameStopsInitials;
            //private Dictionary<string, Class> _ahClassesNames_Classes;
            private Dictionary<long, helpers.replica.cues.TemplateBind[]> _ahClassesIDs_Binds;
            private Dictionary<long, helpers.replica.mam.Cues> _ahCues;
            private helpers.replica.media.File _cPlug;

            public FailoverInfo(
                Dictionary<string, DateTime> ahCheckedFiles,
                Dictionary<string, long> ahCheckedFilesDurs,
                IdNamePair[] aStatuses,
                LinkedList<PlaylistItem> aPlaylistBig,
                Dictionary<long, long> ahFrameStopsInitials,
                Dictionary<long, helpers.replica.cues.TemplateBind[]> ahClassesIDs_Binds,
                Dictionary<long, helpers.replica.mam.Cues> ahCues,
                helpers.replica.media.File cPlug
                )
            {
                _ahCheckedFiles = ahCheckedFiles;
                _ahCheckedFilesDurs = ahCheckedFilesDurs;
                _aStatuses = aStatuses;
                _aPlaylistBig = aPlaylistBig;
                _ahFrameStopsInitials = ahFrameStopsInitials;
                _ahClassesIDs_Binds = ahClassesIDs_Binds;
                _ahCues = ahCues;
                _cPlug = cPlug;
                nCuesCount = null == ahCues ? 0 : ahCues.Count;
                nPLBigCount = null == aPlaylistBig ? 0 : aPlaylistBig.Count;
                nCheckedFilesCount = null == ahCheckedFiles ? 0 : ahCheckedFiles.Count;
            }
            public void FileInfoGet(
                out Dictionary<string, DateTime> ahCheckedFiles,
                out Dictionary<string, long> ahCheckedFilesDurs,
                out IdNamePair[] aStatuses,
                out LinkedList<PlaylistItem> aPlaylistBig,
                out Dictionary<long, long> ahFrameStopsInitials,
                out Dictionary<long, helpers.replica.cues.TemplateBind[]> ahClassesIDs_Binds,
                out Dictionary<long, helpers.replica.mam.Cues> ahCues,
                out helpers.replica.media.File cPlug
                )
            {
                ahCheckedFiles = _ahCheckedFiles == null ? new Dictionary<string, DateTime>() : _ahCheckedFiles;
                ahCheckedFilesDurs = _ahCheckedFilesDurs == null ? new Dictionary<string, long>() : _ahCheckedFilesDurs;
                aStatuses = _aStatuses;
                aPlaylistBig = _aPlaylistBig == null ? new LinkedList<PlaylistItem>() : _aPlaylistBig;
                ahFrameStopsInitials = _ahFrameStopsInitials == null ? new Dictionary<long, long>() : _ahFrameStopsInitials;
                ahClassesIDs_Binds = _ahClassesIDs_Binds == null ? new Dictionary<long, helpers.replica.cues.TemplateBind[]>() : _ahClassesIDs_Binds;
                ahCues = _ahCues == null ? new Dictionary<long, helpers.replica.mam.Cues>() : _ahCues;
                cPlug = _cPlug;
            }
        }
        class FailoverInfoWorking
        {
            static private string sPath = Preferences.sInfoPath; //SIO.Directory.GetCurrentDirectory() + SIO.Path.DirectorySeparatorChar;  в сервисах выдаёт путь на system32 ((
            static public string sInfoFile = "failover_info.dat";
            private BinaryFormatter formatter;

            public FailoverInfoWorking()
            {
                this.formatter = new BinaryFormatter();
            }
            public bool Save(FailoverInfo cFI)
            {
                // Gain code access to the file that we are going
                // to write to
                bool bRetVal = false;
                try
                {
                    (new Logger.Sync()).WriteNotice("offline PL info save path [" + sPath + sInfoFile + "][nCuesCount=" + cFI.nCuesCount + "][nPLBigCount=" + cFI.nPLBigCount + "][nCheckedFilesCount=" + cFI.nCheckedFilesCount + "][nHistoryCount=" + cFI.nHistoryCount + "][nGapCount=" + cFI.nGapCount + "]");

                    // Create a FileStream that will write data to file.
                    FileStream writerFileStream = new FileStream(sPath + "!" + sInfoFile, FileMode.Create, FileAccess.Write);
                    // Save our dictionary of friends to file
                    lock (_cSyncRoot)
                    {
                        this.formatter.Serialize(writerFileStream, cFI);
                    }
                    // Close the writerFileStream when we are done.
                    writerFileStream.Close();
                    if (SIO.File.Exists(sPath + sInfoFile))
                    {
                        if (SIO.File.Exists(sPath + sInfoFile + ".bkp"))
                            SIO.File.Delete(sPath + sInfoFile + ".bkp");
                        System.Threading.Thread.Sleep(200);
                        SIO.File.Move(sPath + sInfoFile, sPath + sInfoFile + ".bkp");
                    }
                    System.Threading.Thread.Sleep(200);
                    SIO.File.Move(sPath + "!" + sInfoFile, sPath + sInfoFile);
                    bRetVal = true;
                }
                catch (Exception ex)
                {
                    (new Logger.Sync()).WriteError("Unable to save our friends' information");
                } // end try-catch
                return bRetVal;
            }
            public FailoverInfo Load()
            {
                FailoverInfo cRetVal = null;
                (new Logger.Sync()).WriteNotice("offline PL info load path [" + sPath + sInfoFile + "]");
                if (File.Exists(sPath + sInfoFile))
                {
                    try
                    {
                        // Create a FileStream will gain read access to the 
                        // <span id="IL_AD6" class="IL_AD">data file</span>.
                        FileStream readerFileStream = new FileStream(sPath + sInfoFile, FileMode.Open, FileAccess.Read);
                        // Reconstruct information of our friends from file.
                        cRetVal = (FailoverInfo)this.formatter.Deserialize(readerFileStream);
                        // Close the readerFileStream when we are done
                        readerFileStream.Close();
                    }
                    catch (Exception)
                    {
                        (new Logger("failover_info")).WriteError("There seems to be a file that contains an offline PL information but somehow there is a problem with reading it.");
                    }
                } // end if
                return cRetVal;
            }
        }
        class RoundedHoursPoints
        {
            public class Point
            {
                public int nIndex;
                public DateTime dtStart;
            }
            public class Hour
            {
                public DateTime dtRoundedHour;
                public List<Point> aPoints;
            }
            public Dictionary<DateTime, Hour> ahRoundedHours_Points;
            public List<DateTime> aRoundedHours;
            public RoundedHoursPoints()
            {
                ahRoundedHours_Points = new Dictionary<DateTime, Hour>();
                aRoundedHours = new List<DateTime>();

            }
            private void Add(DateTime dtRoundedHour, DateTime dtTime, int nIndex)
            {
                if (!ahRoundedHours_Points.ContainsKey(dtRoundedHour))
                {
                    aRoundedHours.Add(dtRoundedHour);
                    ahRoundedHours_Points.Add(dtRoundedHour, new Hour() { dtRoundedHour = dtRoundedHour, aPoints = new List<Point>() });
                }
                ahRoundedHours_Points[dtRoundedHour].aPoints.Add(new Point() { dtStart = dtTime, nIndex = nIndex });
            }
            public void GetAllRoundedHoursEnterPoints(List<PlaylistItem> aPL)
            {
                bool bHourFound = false;
                int nBlockStartPLIFound = -1;
                DateTime dtStart = aPL[0].dtStartPlanned;
                DateTime dtStartHour = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, dtStart.Hour, 0, 0);
                for (int nI = 0; nI < aPL.Count; nI++)
                {
                    if (!bHourFound)
                    {
                        if (nI > 0 && aPL[nI - 1].dtStartHardSoft == DateTime.MaxValue && aPL[nI].dtStartHardSoft < DateTime.MaxValue && aPL[nI].dtStartHardSoft > dtStartHour.AddMinutes(-7))
                            nBlockStartPLIFound = nI;
                        if (aPL[nI].dtStopPlanned > dtStartHour)
                        {
                            bHourFound = true;
                            if (nBlockStartPLIFound >= 0)
                            {
                                this.Add(dtStartHour, aPL[nBlockStartPLIFound].dtStartPlanned, nBlockStartPLIFound);
                                nBlockStartPLIFound = -1;
                            }
                        }
                    }
                    if (Math.Abs(aPL[nI].dtStartPlanned.Subtract(dtStartHour).TotalMinutes) < 5)
                    {
                        if (aPL[nI].dtStartHard < DateTime.MaxValue || aPL[nI].dtStartHardSoft == DateTime.MaxValue || aPL[nI].dtStopPlanned > dtStartHour.AddMinutes(-1) && aPL[nI].dtStartSoft < DateTime.MaxValue && aPL[nI].nFramesQty > 3000)
                        {
                            if (!aPL[nI].bPlug)
                                this.Add(dtStartHour, aPL[nI].dtStartPlanned, nI);
                            continue;
                        }
                    }
                    else if (bHourFound)
                    {
                        nBlockStartPLIFound = -1;
                        bHourFound = false;
                        dtStartHour = dtStartHour.AddHours(1);
                    }
                }
            }

            static public List<PlaylistItem> GetRoundedHoursPL(List<PlaylistItem> aPL)
            {
                string sLog = aPL.IsNullOrEmpty() ? "NULL" : "[count=" + aPL.Count + "][start_srcPL=" + aPL[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop_srcPL=" + aPL[aPL.Count - 1].dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]";
                (new Logger.Sync()).WriteNotice("GetRoundedHoursPL. src PL: " + sLog);
                List<PlaylistItem> aRetVal = new List<PlaylistItem>();
                if (aPL.IsNullOrEmpty() || aPL[aPL.Count - 1].dtStopPlanned.Subtract(aPL[0].dtStartPlanned).TotalMinutes < 50)
                {
                    (new Logger.Sync()).WriteNotice("GetRoundedHoursPL. too small src PL");
                    return aRetVal;
                }

                RoundedHoursPoints cRHP = new RoundedHoursPoints();
                cRHP.GetAllRoundedHoursEnterPoints(aPL);
                if (cRHP.aRoundedHours.Count < 2)
                {
                    (new Logger.Sync()).WriteNotice("GetRoundedHoursPL. not found hour points in src PL");
                    return aRetVal;
                }

                DateTime dtStartHour = cRHP.aRoundedHours[0];
                DateTime dtStopHour = cRHP.aRoundedHours[cRHP.aRoundedHours.Count - 1];
                int nDeltaH = (int)(dtStopHour.Subtract(dtStartHour).TotalHours + 0.5);
                RoundedHoursPoints.Point[] aPositive = new RoundedHoursPoints.Point[2];
                RoundedHoursPoints.Point[] aNegative = new RoundedHoursPoints.Point[2];
                double nMinPos = double.MaxValue, nMinNeg = double.MaxValue;

                foreach (RoundedHoursPoints.Point cPF in cRHP.ahRoundedHours_Points[dtStartHour].aPoints) // First
                {
                    DateTime dtPF = cPF.dtStart.AddHours(nDeltaH);
                    foreach (RoundedHoursPoints.Point cPL in cRHP.ahRoundedHours_Points[dtStopHour].aPoints) // Last
                    {
                        if (dtPF >= cPL.dtStart)
                        {
                            if (nMinPos > dtPF.Subtract(cPL.dtStart).TotalMinutes)
                            {
                                nMinPos = dtPF.Subtract(cPL.dtStart).TotalMinutes;
                                aPositive[0] = cPF;
                                aPositive[1] = cPL;
                            }
                        }
                        else
                        {
                            if (nMinNeg > cPL.dtStart.Subtract(dtPF).TotalMinutes)
                            {
                                nMinNeg = cPL.dtStart.Subtract(dtPF).TotalMinutes;
                                aNegative[0] = cPF;
                                aNegative[1] = cPL;
                            }
                        }
                    }
                }

                (new Logger.Sync()).WriteNotice("GetRoundedHoursPL. found hour points in src PL" +
                                            "[min_positive=" + nMinPos.ToString("0.0") + "; start=" + (nMinPos == double.MaxValue ? "NULL" : aPositive[0].dtStart.ToString("HH:mm:ss")) + "; stop=" + (nMinPos == double.MaxValue ? "NULL" : aPositive[1].dtStart.ToString("HH:mm:ss")) + "] " +
                                            "[min_negative=" + (nMinNeg == double.MaxValue ? "MAX_VAL" : nMinNeg.ToString("0.0")) + "; start=" + (nMinNeg == double.MaxValue ? "NULL" : aNegative[0].dtStart.ToString("HH:mm:ss")) + "; stop=" + (nMinNeg == double.MaxValue ? "NULL" : aNegative[1].dtStart.ToString("HH:mm:ss")) + "]");
                if (nMinPos > 0.16 && (nMinNeg < 1.0 || nMinPos == double.MaxValue))
                    aRetVal.AddRange(aPL.GetRange(aNegative[0].nIndex, aNegative[1].nIndex - aNegative[0].nIndex));
                else
                    aRetVal.AddRange(aPL.GetRange(aPositive[0].nIndex, aPositive[1].nIndex - aPositive[0].nIndex));

                RoundedHoursPoints.ReCalcRoundedHourPL(aRetVal);
                return aRetVal;
            }
            static public void ReCalcRoundedHourPL(List<PlaylistItem> aPL)
            {
                if (aPL.IsNullOrEmpty())
                    throw new Exception("ReCalcRoundedHourPL. aPL is NULL");
                long nTotalPlugsDur = 0;
                Recalc(aPL, out nTotalPlugsDur);
                long nDiff = GetDiffInFrames(aPL);
                new Logger.Sync().WriteNotice("ReCalcRoundedHourPL. difference in PL is " + nDiff + " frames. [total_plugs=" + nTotalPlugsDur + "]");
                if (nDiff < 0) // надо отрезать лишнее
                {
                    Cut(aPL, nTotalPlugsDur, nDiff);
                    Recalc(aPL, out nTotalPlugsDur);
                    nDiff = GetDiffInFrames(aPL);
                    new Logger.Sync().WriteNotice("ReCalcRoundedHourPL. difference in PL after cut is " + nDiff + " frames. [total_plugs=" + nTotalPlugsDur + "]");
                }
                else if (nDiff > 0) // надо добавлять плаг
                {
                    ulong nDurPrev;
                    PlaylistItem cPLILast = aPL[aPL.Count - 1];
                    nDurPrev = cPLILast.nDuration;
                    if (null == _cPlug)
                        new Logger.Sync().WriteError("ReCalcRoundedHourPL. _cPlug is NULL !!!");
                    aPL.AddRange(PlugsGet(null, cPLILast.dtStartPlanned.AddMilliseconds((long)nDurPrev * Player.Preferences.nFrameMs), nDiff, new Logger.Sync()));
                }
            }
            static private void Cut(List<PlaylistItem> aPL, long nTotalPlugs, long nCutOff)
            {
                new Logger.Sync().WriteNotice("Cut. need to cut off " + nCutOff + " frames.");
                long nCutOther = nCutOff - nTotalPlugs;
                ulong nDur;
                for (int nI = 0; nI < aPL.Count; nI++)
                {
                    if (nCutOff <= 0)
                        return;
                    if (aPL[nI].bPlug)
                    {
                        nDur = aPL[nI].nDuration;
                        nCutOff -= (long)nDur;
                        if (nCutOff >= 0)
                        {
                            new Logger.Sync().WriteNotice("Cut. remove plug [" + nDur + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            aPL.RemoveAt(nI--);
                        }
                        else
                        {
                            new Logger.Sync().WriteNotice("Cut. cut off from plug [" + nDur + nCutOff + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            aPL[nI].nFrameStop = aPL[nI].nFrameStart + (-nCutOff) - 1;
                        }
                        continue;
                    }
                    else if (nCutOther > 0) // значит плаги отрезаем точно все и еще клипы тут
                    {
                        if (aPL[nI].dtStartHardSoft == DateTime.MaxValue)  // не из блока
                        {
                            nDur = aPL[nI].nDuration;
                            nCutOther -= (long)nDur;
                            if (nCutOther >= 0)
                            {
                                new Logger.Sync().WriteNotice("Cut. remove sequential [" + nDur + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                aPL.RemoveAt(nI--);
                            }
                            else
                            {
                                new Logger.Sync().WriteNotice("Cut. cut off from sequential [" + nDur + nCutOther + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                aPL[nI].nFrameStop = aPL[nI].nFrameStart + (-nCutOther) - 1;
                            }
                        }
                    }
                }

                if (nCutOther > 0)  // тогда ищем в блоке один большой, чтоб отрезать
                {
                    for (int nI = 0; nI < aPL.Count; nI++)
                    {
                        if (nCutOther <= 0)
                            return;

                        nDur = aPL[nI].nDuration;
                        if (aPL[nI].dtStartHardSoft < DateTime.MaxValue && nDur > 3000 && (long)nDur >= nCutOther)
                        {
                            nCutOther -= (long)nDur;
                            new Logger.Sync().WriteNotice("Cut. cut off from long block element [" + nDur + nCutOther + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            aPL[nI].nFrameStop = aPL[nI].nFrameStart + (-nCutOther) - 1;
                        }
                    }
                }

                if (nCutOther > 0) // режем подряд от всего
                {
                    for (int nI = 0; nI < aPL.Count; nI++)
                    {
                        if (nCutOther <= 0)
                            return;

                        nDur = aPL[nI].nDuration;
                        nCutOther -= (long)nDur;
                        if (nCutOther >= 0)
                        {
                            new Logger.Sync().WriteNotice("Cut. remove block element [" + nDur + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            aPL.RemoveAt(nI--);
                        }
                        else
                        {
                            new Logger.Sync().WriteNotice("Cut. cut off from block element [" + nDur + nCutOther + " frames][id=" + aPL[nI].nID + "][name=" + aPL[nI].sName + "][start=" + aPL[nI].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            aPL[nI].nFrameStop = aPL[nI].nFrameStart + (-nCutOther) - 1;
                        }
                    }
                }
            }
            static private void Recalc(List<PlaylistItem> aPL)
            {
                long nTotalPlugsDur;
                Recalc(aPL, out nTotalPlugsDur);
            }
            static public void Recalc(List<PlaylistItem> aPL, out long nTotalPlugsDur)
            {
                Recalc(DateTime.MinValue, aPL, out nTotalPlugsDur);
            }
            static public void Recalc(DateTime dtStart, List<PlaylistItem> aPL, out long nTotalPlugsDur)
            {
                nTotalPlugsDur = 0;
                ulong nDurPrev;
                if (dtStart > DateTime.MinValue && aPL.Count > 0)
                    aPL[0].dtStartPlanned = dtStart;
                for (int nI = 0; nI < aPL.Count; nI++)
                {
                    if (aPL[nI].bPlug)
                    {
                        nTotalPlugsDur += (long)aPL[nI].nDuration;
                    }
                    if (nI > 0)
                    {
                        nDurPrev = aPL[nI - 1].nDuration;
                        aPL[nI].dtStartPlanned = aPL[nI - 1].dtStartPlanned.AddMilliseconds((long)nDurPrev * Player.Preferences.nFrameMs);
                    }
                }
            }
            static public void RecalcForward(List<PlaylistItem> aPL)
            {
                ulong nDurLast = aPL[aPL.Count - 1].nDuration;
                aPL[0].dtStartPlanned = aPL[aPL.Count - 1].dtStartPlanned.AddMilliseconds((long)nDurLast * Player.Preferences.nFrameMs);
                Recalc(aPL);
            }
            static public void RecalcBackward(List<PlaylistItem> aPL)
            {
                ulong nDurLast = aPL[aPL.Count - 1].nDuration;
                DateTime dtStopPL = aPL[aPL.Count - 1].dtStartPlanned.AddMilliseconds((long)nDurLast * Player.Preferences.nFrameMs);
                TimeSpan tsDurPL = dtStopPL.Subtract(aPL[0].dtStartPlanned);
                aPL[0].dtStartPlanned = aPL[0].dtStartPlanned.Subtract(tsDurPL);
                Recalc(aPL);
            }
            static private long GetDiffInFrames(List<PlaylistItem> aPL)
            {
                ulong nDur = aPL[aPL.Count - 1].nDuration;
                DateTime dtEnd = aPL[aPL.Count - 1].dtStartPlanned.AddMilliseconds((long)nDur * Player.Preferences.nFrameMs);
                int nDiffHours = (int)(dtEnd.Subtract(aPL[0].dtStartPlanned).TotalHours + 0.5);
                DateTime dtBeginAdapted = aPL[0].dtStartPlanned.AddHours(nDiffHours);
                return (long)(dtBeginAdapted.Subtract(dtEnd).TotalMilliseconds / Player.Preferences.nFrameMs);
            }
        }

        static private object _cSyncRoot;
        static private byte _nSyncTries;
        static private LinkedList<PlaylistItem> _aPlaylist
        {
            get
            {
                return _aPlaylistOnline;
            }
        }
        static private FailoverInfo _cFailoverInfo;
        static private LinkedList<PlaylistItem> _aPlaylistOnline;
        static private LinkedList<PlaylistItem> _aPlaylistBig;
        static private PlaylistItem _cLastOnlinePLI;
        static private Dictionary<long, long> _ahFrameStopsInitials;
        static private Dictionary<long, helpers.replica.cues.TemplateBind[]> _ahPLIIDs_Binds;
        static private long _nClassesRenewPLIIDStartFrom;
        static private DateTime _dtWhenBigPLGetNextTime;
        static private DateTime _dtWhenShortPLGetNextTime;
        static private bool _bSyncFirstTime;
        static private bool _bWeAreSyncingNow;
        static public bool _bBesiegedFortressMode;
        static public bool _bDoNotDeleteCacheMode;
        static private string _sBesiegedFortressFile;
        static private List<PlaylistItem> _aBesiegedFortressPL;
        static private List<PlaylistItem> _aBesiegedFortressPLNext;
        static private long _nMaxBFID;
        static private Dictionary<string, DateTime> _ahCheckedFiles;
        static private Dictionary<string, long> _ahCheckedFilesDurs;
        static private helpers.replica.media.File _cPlug;
        static private helpers.replica.cues.TemplateBind[] _aDefaultTemplateBinds;
        static private long _nPlugID;
        static private Dictionary<long, helpers.replica.mam.Cues> _ahCues;
        static private sbyte _nAdjustmentDelay = 0;
        #region statuses
        static private IdNamePair[] _aStatuses;
        static private IdNamePair[] _aStatusesLocked
        {
            get
            {
                return _aStatuses.Where(o => Preferences.aStatusesLocked.Contains(o.sName.To<Preferences.PersistentStatus>())).ToArray();
            }
        }
        static private IdNamePair[] _aStatusesStaled
        {
            get
            {
                return _aStatuses.Where(o => Preferences.aStatusesStaled.Contains(o.sName.To<Preferences.PersistentStatus>())).ToArray();
            }
        }
        static private IdNamePair _cStatusFailed
        {
            get
            {
                string sValue = Preferences.PersistentStatus.failed.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        static private IdNamePair _cStatusPlayed
        {
            get
            {
                string sValue = Preferences.PersistentStatus.played.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        static private IdNamePair _cStatusSkipped
        {
            get
            {
                string sValue = Preferences.PersistentStatus.skipped.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        static private IdNamePair _cStatusOnAir
        {
            get
            {
                string sValue = Preferences.PersistentStatus.onair.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        static private IdNamePair _cStatusPrepared
        {
            get
            {
                string sValue = Preferences.PersistentStatus.prepared.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        static private IdNamePair _cStatusQueued
        {
            get
            {
                string sValue = Preferences.PersistentStatus.queued.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        static private IdNamePair _cStatusPlanned
        {
            get
            {
                string sValue = Preferences.PersistentStatus.planned.ToString();
                return _aStatuses.First(o => sValue == o.sName);
            }
        }
        #endregion statuses

        static Failover()
        {
            ahErrors = new Dictionary<ErrorTarget, DateTime>();
            foreach (ErrorTarget e in Enum.GetValues(typeof(ErrorTarget)))
                ahErrors.Add(e, DateTime.MinValue);
            _cSyncRoot = new object();
            _nSyncTries = Preferences.nSyncTries;
            _aPlaylistOnline = new LinkedList<PlaylistItem>();
            _aPlaylistBig = new LinkedList<PlaylistItem>();
            _ahFrameStopsInitials = new Dictionary<long, long>();
            System.Threading.ThreadPool.QueueUserWorkItem((object o) => { ClassesRenewWorker(); });
            _cLastOnlinePLI = null;
            _bSyncFirstTime = true;
            _dtWhenBigPLGetNextTime = DateTime.MinValue;
            _ahPLIIDs_Binds = new Dictionary<long, helpers.replica.cues.TemplateBind[]>();
            _cPlug = Preferences.cDefaultPlug;
            _aStatuses = null;
            _ahCues = new Dictionary<long, helpers.replica.mam.Cues>();
            Adjust(TimeSpan.Zero);

            _aDefaultTemplateBinds = Preferences.aDefaultTemplateBinds;
            _bWeAreSyncingNow = false;

            _ahCheckedFiles = new Dictionary<string, DateTime>();
            _ahCheckedFilesDurs = new Dictionary<string, long>();

            _aBesiegedFortressPL = new List<PlaylistItem>();
            _aBesiegedFortressPLNext = new List<PlaylistItem>();
            _sBesiegedFortressFile = SIO.Path.Combine(Player.Preferences.sCacheFolder, FailoverConstants.sFileBesiegedFortress);
            (new Logger.Sync()).WriteNotice("file for Besieged Fortress mode is [" + _sBesiegedFortressFile + "]");

            _cFailoverInfo = null;
            FailoverInfo cFI = (new FailoverInfoWorking()).Load();
            if (null != cFI)
            {
                (new Logger.Sync()).WriteNotice("offline PL info loaded");
                cFI.FileInfoGet(out _ahCheckedFiles, out _ahCheckedFilesDurs, out _aStatuses, out _aPlaylistBig, out _ahFrameStopsInitials, out _ahPLIIDs_Binds, out _ahCues, out _cPlug);
                (new Logger.Sync()).WriteDebug("\t\tgot PL [count=" + _aPlaylistBig.Count + "]");
                (new Logger.Sync()).WriteDebug("\t\tgot binds [count=" + _ahPLIIDs_Binds.Count + "]");
                foreach (helpers.replica.cues.TemplateBind[] aTB in _ahPLIIDs_Binds.Values)
                    ChangeTemplatesDriveLetter(aTB);
                (new Logger.Sync()).WriteDebug("\t\tgot cues [count=" + _ahCues.Count + "]");
                (new Logger.Sync()).WriteDebug("\t\tgot checked files [count=" + _ahCheckedFiles.Count + "]");
                (new Logger.Sync()).WriteDebug("\t\tgot checked files durs [count=" + _ahCheckedFilesDurs.Count + "]");
                (new Logger.Sync()).WriteDebug("\t\tgot statuses [count=" + _aStatuses.Length + "]");
                (new Logger.Sync()).WriteDebug("\t\tgot frames stops [count=" + _ahFrameStopsInitials.Count + "]");
            }
            else
                (new Logger.Sync()).WriteNotice("info was not loaded");
        }
        static void Adjust(TimeSpan ts)
        {
            if (0 < _nAdjustmentDelay--)
                return;
            double nAdjustment = ts.Add(Preferences.tsAdjustment).TotalSeconds * Player.Preferences.nFPS;
            _nAdjustmentDelay = 5;
            if (25 < nAdjustment)
                Player.AdjustmentFramesAdd((uint)nAdjustment);
            else if (-25 > nAdjustment)
                Player.AdjustmentFramesRemove((uint)(-1 * nAdjustment));
        }
        static private void ReplaceInPath(PlaylistItem cPLI)
        {
            if (Preferences.aReplacePath != null && Preferences.aReplacePath.Length > 1)
            {
                int nIndx = 0;
                int nCount = Preferences.aReplacePath.Length;
                string sPath = cPLI.cFile.cStorage.sPath;
                while (nIndx + 1 < nCount)
                {
                    cPLI.cFile.cStorage.sPath = cPLI.cFile.cStorage.sPath.Replace(Preferences.aReplacePath[nIndx], Preferences.aReplacePath[nIndx + 1]);
                    nIndx += 2;
                }
            }
        }
        static private void RecalcPL()
        {
            PlaylistItem cPrev = null;
            foreach (PlaylistItem cPLItem in _aPlaylist)
            {
                if (null != cPrev && cPLItem.dtStartReal == DateTime.MaxValue)
                    cPLItem.dtStartPlanned = cPrev.dtStartPlanned.AddMilliseconds((long)cPrev.nDuration * Player.Preferences.nFrameMs);
                cPrev = cPLItem;
            }
        }
        static private int PLCount()
        {
            int nRetVal = 0;
            foreach (PlaylistItem cPLI in _aPlaylistOnline)
            {
                if (cPLI.dtStartReal == DateTime.MaxValue && cPLI.dtStartPlanned > DateTime.Now)
                    nRetVal++;
            }
            return nRetVal;
        }
        static private bool PLCountIsSafe()
        {
            lock (_cSyncRoot)
            {
                if (PLCount() >= Preferences.nPLCountSafe)
                    return true;
            }
            return false;
        }
        static private bool PLDurationIsSafe()
        {
            ulong nFramesTotal = 0;
            lock (_cSyncRoot)
            {
                foreach (PlaylistItem cPLI in _aPlaylistOnline)
                {
                    nFramesTotal += cPLI.nDuration;
                    if (nFramesTotal > Preferences.nPLDurSafe)
                        return true;
                }
            }
            return false;
        }
        static private bool ItsSafeToGetBigPL()
        {
            if (_dtWhenBigPLGetNextTime > DateTime.Now)
                return false;

            return PLDurationIsSafe();
        }
        static private Queue<PlaylistItem> ShortPLGetOffline()
        {
            Queue<PlaylistItem> aPL = new Queue<PlaylistItem>();
            bool bFound = false;
            TimeSpan tsPLDur = new TimeSpan(0, 0, 0);


            foreach (PlaylistItem cPLI in _aPlaylistBig)
            {
                if (bFound)
                {
                    aPL.Enqueue(cPLI);
                    tsPLDur = tsPLDur.Add(TimeSpan.FromMilliseconds((long)cPLI.nDuration * Player.Preferences.nFrameMs));
                    if (tsPLDur > Preferences.tsPLGetDurShort)
                        break;
                }
                else if (cPLI.dtStopPlanned > DateTime.Now)
                    bFound = true;
            }
            return aPL;
        }
        static private Queue<PlaylistItem> ShortPLGetFromBesiegedFortressPL()
        {
            Queue<PlaylistItem> aPL = new Queue<PlaylistItem>();
            bool bFound = false;
            TimeSpan tsPLDur = new TimeSpan(0, 0, 0);

            if (_aBesiegedFortressPLNext.IsNullOrEmpty()) // т.е. надо сдвигать назад
            {
                while (_aBesiegedFortressPL[0].dtStartPlanned > DateTime.Now)
                    RoundedHoursPoints.RecalcBackward(_aBesiegedFortressPL);

                while (_aBesiegedFortressPL[_aBesiegedFortressPL.Count - 1].dtStopPlanned < DateTime.Now)
                    RoundedHoursPoints.RecalcForward(_aBesiegedFortressPL);

                for (int nI = 0; nI < _aBesiegedFortressPL.Count; nI++)
                {
                    _aBesiegedFortressPLNext.Add(new PlaylistItem(_aBesiegedFortressPL[nI], _aBesiegedFortressPL[nI].nID));
                    _aBesiegedFortressPLNext[nI].dtStartReal = DateTime.MaxValue;
                    _aBesiegedFortressPLNext[nI].dtStartQueued = DateTime.MaxValue;
                    _aBesiegedFortressPLNext[nI].dtStopReal = DateTime.MaxValue;
                    _aBesiegedFortressPLNext[nI].cStatus = _cStatusPlanned;
                }
                RoundedHoursPoints.RecalcForward(_aBesiegedFortressPLNext);
            }

            while (_aBesiegedFortressPL[_aBesiegedFortressPL.Count - 1].dtStopPlanned < DateTime.Now)
            {
                BesiegedFortressPLCopyToForward();
            }

            foreach (PlaylistItem cPLI in _aBesiegedFortressPL)
            {
                if (!bFound && cPLI.dtStopPlanned > DateTime.Now)
                {
                    bFound = true;
                    (new Logger.Sync()).WriteNotice("ShortPLGetFromBesiegedFortressPL. found 'now' in PL: [id=" + cPLI.nID + "][name=" + cPLI.sName + "][start=" + cPLI.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + cPLI.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                }

                if (bFound)
                {
                    aPL.Enqueue(cPLI);
                    tsPLDur = tsPLDur.Add(TimeSpan.FromMilliseconds((long)cPLI.nDuration * Player.Preferences.nFrameMs));
                    if (tsPLDur > Preferences.tsPLGetDurShort)
                        return aPL;
                }
            }

            foreach (PlaylistItem cPLI in _aBesiegedFortressPLNext)
            {
                if (!bFound && cPLI.dtStopPlanned > DateTime.Now)
                {
                    bFound = true;
                    (new Logger.Sync()).WriteNotice("ShortPLGetFromBesiegedFortressPL. found 'now' in PLNext: [id=" + cPLI.nID + "][name=" + cPLI.sName + "][start=" + cPLI.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + cPLI.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                }

                if (bFound)
                {
                    aPL.Enqueue(cPLI);
                    tsPLDur = tsPLDur.Add(TimeSpan.FromMilliseconds((long)cPLI.nDuration * Player.Preferences.nFrameMs));
                    if (tsPLDur > Preferences.tsPLGetDurShort)
                        return aPL;
                }
            }

            return aPL;
        }
        static private void BesiegedFortressPLCopyToForward()
        {
            PlaylistItem cPLLast = _aBesiegedFortressPL[_aBesiegedFortressPL.Count - 1];
            PlaylistItem cPLNextLast = _aBesiegedFortressPLNext[_aBesiegedFortressPLNext.Count - 1];
            (new Logger.Sync()).WriteDebug("BesiegedFortressPLCopyToForward. before: <br>\t\tstart pl [id=" + _aBesiegedFortressPL[0].nID + "][name=" + _aBesiegedFortressPL[0].sName + "][start=" + _aBesiegedFortressPL[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]" +
                                                                                     "<br>\t\tstop pl[id=" + cPLLast.nID + "][name=" + cPLLast.sName + "][stop=" + cPLLast.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]" +
                                                                                     "<br>\t\tstart pl next [id=" + _aBesiegedFortressPLNext[0].nID + "][name=" + _aBesiegedFortressPLNext[0].sName + "][start=" + _aBesiegedFortressPLNext[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]" +
                                                                                     "<br>\t\tstop pl next [id=" + cPLNextLast.nID + "][name=" + cPLNextLast.sName + "][stop=" + cPLNextLast.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
            _aBesiegedFortressPL = _aBesiegedFortressPLNext.ToList();
            _aBesiegedFortressPLNext.Clear();

            for (int nI = 0; nI < _aBesiegedFortressPL.Count; nI++)
            {
                _aBesiegedFortressPLNext.Add(new PlaylistItem(_aBesiegedFortressPL[nI], _aBesiegedFortressPL[nI].nID));
                _aBesiegedFortressPLNext[nI].dtStartReal = DateTime.MaxValue;
                _aBesiegedFortressPLNext[nI].dtStartQueued = DateTime.MaxValue;
                _aBesiegedFortressPLNext[nI].dtStopReal = DateTime.MaxValue;
            }
            RoundedHoursPoints.RecalcForward(_aBesiegedFortressPLNext);

            cPLLast = _aBesiegedFortressPL[_aBesiegedFortressPL.Count - 1];
            cPLNextLast = _aBesiegedFortressPLNext[_aBesiegedFortressPLNext.Count - 1];
            (new Logger.Sync()).WriteDebug("BesiegedFortressPLCopyToForward. after: <br>\t\tstart pl [id=" + _aBesiegedFortressPL[0].nID + "][name=" + _aBesiegedFortressPL[0].sName + "][start=" + _aBesiegedFortressPL[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]" +
                                                                                     "<br>\t\tstop pl[id=" + cPLLast.nID + "][name=" + cPLLast.sName + "][stop=" + cPLLast.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]" +
                                                                                     "<br>\t\tstart pl next [id=" + _aBesiegedFortressPLNext[0].nID + "][name=" + _aBesiegedFortressPLNext[0].sName + "][start=" + _aBesiegedFortressPLNext[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]" +
                                                                                     "<br>\t\tstop pl next [id=" + cPLNextLast.nID + "][name=" + cPLNextLast.sName + "][stop=" + cPLNextLast.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
        }
        static private void ClassesRenewWorker()
        {
            try
            {
                long nPLIIDStartFrom;
                DBInteract cDBI = new DBInteract();
                helpers.replica.cues.TemplateBind[] aTB;
                while (true)
                {
                    if (_nClassesRenewPLIIDStartFrom > 0)
                    {
                        (new Logger("ClassesRenewWorker")).WriteNotice("start to get classes...");
                        nPLIIDStartFrom = _nClassesRenewPLIIDStartFrom;

                        PlaylistItem[] aPLBig;
                        lock (_cSyncRoot)
                            aPLBig = _aPlaylistBig.ToArray();

                        bool bFound = false;
                        Dictionary<long, helpers.replica.cues.TemplateBind[]> ahPLIIDs_Binds = new Dictionary<long, helpers.replica.cues.TemplateBind[]>();
                        foreach (PlaylistItem cPLI in aPLBig)
                        {
                            if (!bFound && nPLIIDStartFrom == cPLI.nID)
                            {
                                bFound = true;
                                (new Logger("ClassesRenewWorker")).WriteNotice("from this PLI: [pliid=" + cPLI.nID + "][name=" + cPLI.sName + "][planned=" + cPLI.dtStartPlanned.ToStr() + "]");
                            }

                            if (cPLI.bPlug)
                                continue;

                            if (!bFound)
                            {
                                if (!ahPLIIDs_Binds.ContainsKey(cPLI.nID) && _ahPLIIDs_Binds.ContainsKey(cPLI.nID))
                                {
                                    aTB = _ahPLIIDs_Binds[cPLI.nID];
                                    ChangeTemplatesDriveLetter(aTB);
                                    ahPLIIDs_Binds.Add(cPLI.nID, aTB);
                                }
                                else if (!ahPLIIDs_Binds.ContainsKey(cPLI.nID) && !_ahPLIIDs_Binds.ContainsKey(cPLI.nID))
                                    (new Logger("ClassesRenewWorker")).WriteError("class binds not fount for PLI [id=" + cPLI.nID + "][name=" + cPLI.sName + "][planned=" + cPLI.dtStartPlanned.ToStr() + "]");
                            }
                            if (bFound && !ahPLIIDs_Binds.ContainsKey(cPLI.nID))
                            {
                                aTB = cDBI.TemplateBindsActualGet(cPLI);
                                ChangeTemplatesDriveLetter(aTB);
                                ahPLIIDs_Binds.Add(cPLI.nID, aTB);

                                if (nPLIIDStartFrom != _nClassesRenewPLIIDStartFrom) // i.e. new PL got at this moment (in #region PL BIG (FOR OFFLINE WORK) GET)
                                {
                                    (new Logger("ClassesRenewWorker")).WriteNotice("break on new PL got [nPLIIDStartFrom = " + nPLIIDStartFrom + "][_nClassesRenewPLIIDStartFrom = " + _nClassesRenewPLIIDStartFrom + "]");
                                    break;
                                }
                                System.Threading.Thread.Sleep(200);
                            }
                        }
                        lock (_cSyncRoot)
                            if (nPLIIDStartFrom == _nClassesRenewPLIIDStartFrom)
                                _nClassesRenewPLIIDStartFrom = -1;

                        if (!ahPLIIDs_Binds.IsNullOrEmpty())
                        {
                            (new Logger("ClassesRenewWorker")).WriteNotice("class binds was got [count = " + ahPLIIDs_Binds.Count + "]");
                            lock (_cSyncRoot)
                                _ahPLIIDs_Binds = ahPLIIDs_Binds;
                        }
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                (new Logger("ClassesRenewWorker")).WriteError(ex);
            }
            finally
            {
                (new Logger("ClassesRenewWorker")).WriteNotice("Classes renew worker stopped");
            }
        }
        static private void ChangeTemplatesDriveLetter(helpers.replica.cues.TemplateBind[] aTB)
        {
            if (Preferences.sTemplatesDriveLetter.IsNullOrEmpty())
                return;
            if (aTB.IsNullOrEmpty())
                return;

            string sCurrentDrive;
            for (int nI = 0; nI < aTB.Length; nI++)
            {
                if (aTB[nI].cTemplate == null || aTB[nI].cTemplate.sFile.IsNullOrEmpty())
                    continue;
                sCurrentDrive = SIO.Path.GetPathRoot(aTB[nI].cTemplate.sFile);
                if (sCurrentDrive.IsNullOrEmpty() || sCurrentDrive.Length != 3 || sCurrentDrive.Substring(1, 1) != ":")
                {
                    (new Logger("ChangeTemplatesDriveLetter")).WriteNotice("strange template path [id_tempbind=" + aTB[nI].nID + "][file=" + aTB[nI].cTemplate.sFile + "]");
                    continue;
                }
                sCurrentDrive = sCurrentDrive.Substring(0, 2).ToLower();
                if (sCurrentDrive != Preferences.sTemplatesDriveLetter)
                    aTB[nI].cTemplate.sFile = aTB[nI].cTemplate.sFile.ToLower().Replace(sCurrentDrive, Preferences.sTemplatesDriveLetter);
            }
        }
        static private long GetPLIIDChangesStartedFrom(Queue<PlaylistItem> aPL)  //DNF ПОМЕНЯЙ _ahPLIIDs_Binds
        {
            int nIndx = 0;
            PlaylistItem[] aPLCopy = aPL.ToArray();
            while (aPLCopy[nIndx].bPlug)
                nIndx++;
            if (!_ahPLIIDs_Binds.IsNullOrEmpty())
            {
                bool bFound = false;
                lock (_cSyncRoot)
                {
                    foreach (PlaylistItem cPLI in _aPlaylistBig) // find the first element in old PL
                    {
                        if (cPLI.bPlug)
                            continue;
                        if (!bFound && aPLCopy[nIndx].nID == cPLI.nID)
                            bFound = true;

                        if (bFound)
                        {
                            if (!_ahPLIIDs_Binds.ContainsKey(aPLCopy[nIndx].nID) || aPLCopy[nIndx].nID != cPLI.nID)
                                break;
                            nIndx++;
                            while (nIndx < aPLCopy.Length && aPLCopy[nIndx].bPlug)
                                nIndx++;

                            if (nIndx == aPLCopy.Length)
                                break;
                        }
                    }
                }
                if (nIndx == aPLCopy.Length) // nothing to do here
                {
                    (new Logger.Sync()).WriteNotice("GetPLIIDChangesStartedFrom. Nothing to do - PL not changed [count = " + nIndx + "]");
                    return -1;
                }
            }
            (new Logger.Sync()).WriteNotice("GetPLIIDChangesStartedFrom. PL changed [ch started from = " + aPLCopy[nIndx].nID + "]");
            return aPLCopy[nIndx].nID;
        }
        static private bool ShouldAddPLIToPL(PlaylistItem cPLIPrev, PlaylistItem cPLI, string[] aStatusesLockedNames, int nCountPLOnline, bool bCheckPlug)
        {
            if (aStatusesLockedNames.Contains(cPLI.cStatus.sName))
                return true;

            if (cPLIPrev.dtStopPlanned.Subtract(DateTime.Now) < Preferences.tsPLDurMin || nCountPLOnline <= Preferences.nPLCountMin)
                return true;

            if (Math.Abs(cPLI.dtStartHardSoft.Subtract(cPLIPrev.dtStartHardSoft).TotalSeconds) == 1 && cPLI.nDuration < Preferences.nBlockElementDurMax)
                return true;

            if (bCheckPlug && cPLIPrev.bPlug)
                return true;

            return false;
        }
        static private List<string> FilenamesInCacheGet()
        {
            (new Logger.Sync()).WriteNotice("FilenamesInCacheGet. checking cache...");
            List<string> aFilesInCache = new List<string>();
            string sTMP;
            foreach (string sF in SIO.Directory.GetFiles(Player.Preferences.sCacheFolder, "*.*", SearchOption.TopDirectoryOnly).Where(o => !Path.GetExtension(o).IsNullOrEmpty() && FailoverConstants.aPossibleExtensions.Contains(Path.GetExtension(o).ToLower())))
            {
                sTMP = SIO.Path.GetFileNameWithoutExtension(sF);
                if (sTMP.StartsWith("_"))
                    sTMP = sTMP.Substring(1);
                aFilesInCache.Add(sTMP);
            }
            (new Logger.Sync()).WriteNotice("FilenamesInCacheGet. cache count = " + aFilesInCache.Count + ".");
            return aFilesInCache;
        }
        static private void MakeBesiegedFortressPL()
        {
            (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. Will make BF PL. geting cache filenames...");
            lock (_cSyncRoot)
                _aBesiegedFortressPL.Clear();
            List<string> aFilesInCache = FilenamesInCacheGet();

            (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. cache count = " + aFilesInCache.Count + ". Will get from history");

            List<PlaylistItem> aFromOffline = new List<PlaylistItem>();
            List<PlaylistItem> aBoth = new List<PlaylistItem>();
            //int nNotFound = 0;
            PlaylistItem cPLILastNonPlug = null;
            PlaylistItem cPLIFirstNonPlug = null;
            lock (_cSyncRoot)
            {
                (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. Will get from offline PL");

                bool bFound = false, bFoundCache = false;
                List<PlaylistItem> aMaxDur=null;
                List<PlaylistItem> aCurr = new List<PlaylistItem>();
                List<PlaylistItem> aOnlySequentials = new List<PlaylistItem>();
                long nMaxDur = 0, nCurr = 0, nOnlySequentials = 0;
                foreach (PlaylistItem cPLI in _aPlaylistBig)
                {
                    if (cPLI.bPlug)
                        continue;
                    if (cPLI.dtStartHardSoft == DateTime.MaxValue && aFilesInCache.Contains("" + cPLI.nID))
                    {






//if (aOnlySequentials.Count < 1)





                        {
                            aOnlySequentials.Add(cPLI);
                            nOnlySequentials += (long)cPLI.nDuration;
                        }
                    }
                    if (!bFound)
                    {
                        if (aFilesInCache.Contains("" + cPLI.nID))
                        {
                            bFound = true;
                        }
                    }
                    if (bFound)
                    {
                        if (aFilesInCache.Contains("" + cPLI.nID))
                        {
                            aCurr.Add(cPLI);
                            nCurr += (long)cPLI.nDuration;
                        }
                        else
                        {
                            bFound = false;
                            if (nCurr > nMaxDur)
                            {
                                nMaxDur = nCurr;
                                aMaxDur = aCurr;
                            }
                            aCurr = new List<PlaylistItem>();
                            nCurr = 0;
                        }
                    }
                }
                if (bFound)
                {
                    if (nCurr > nMaxDur)
                    {
                        nMaxDur = nCurr;
                        aMaxDur = aCurr;
                    }
                }

                aFromOffline = aMaxDur;
                if (aFromOffline.IsNullOrEmpty())
                    (new Logger.Sync()).WriteError("MakeBesiegedFortressPL. got from offline PL 0");
                else
                {
                    (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. got from offline PL [count=" + aFromOffline.Count + "][start=" + aFromOffline[0].nID + " : " + aFromOffline[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + aFromOffline[aFromOffline.Count - 1].nID + " : " + aFromOffline[aFromOffline.Count - 1].dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                    (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. Will analize");

                    aFromOffline = RoundedHoursPoints.GetRoundedHoursPL(aFromOffline);
                }

aFromOffline = null;

                if (!aFromOffline.IsNullOrEmpty())
                    (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. found whole hours for BF PL in Offline PL [count=" + aFromOffline.Count + "][start=" + aFromOffline[0].nID + " : " + aFromOffline[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + aFromOffline[aFromOffline.Count - 1].nID + " : " + aFromOffline[aFromOffline.Count - 1].dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                else
                {
                    (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. not found in Offline PL - will start in sequential elements only [count=" + aOnlySequentials.Count + "][dur=" + ((float)nOnlySequentials / 25 / 3600).ToString("0:00") + " hours]");
                    DateTime dtNow = DateTime.Now;
                    DateTime dtStart = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, 0, 0);
                    long nPlugsDur;
                    if (!aOnlySequentials.IsNullOrEmpty())
                    {
                        RoundedHoursPoints.Recalc(dtStart, aOnlySequentials, out nPlugsDur);
                        dtStart = aOnlySequentials.Last().dtStopPlanned;
                    }
                    if (nOnlySequentials < 3600 * Player.Preferences.nFPS)
                        aOnlySequentials.AddRange(PlugsGet(null, dtStart, 3600 * Player.Preferences.nFPS - nOnlySequentials, new Logger.Sync()));

                    //aFromOffline = RoundedHoursPoints.GetRoundedHoursPL(aOnlySequentials);
                    aFromOffline = aOnlySequentials;
                    (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. found whole hours for BF PL in sequential elements only [count=" + aFromOffline.Count + "][start=" + aFromOffline[0].nID + " : " + aFromOffline[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + aFromOffline[aFromOffline.Count - 1].nID + " : " + aFromOffline[aFromOffline.Count - 1].dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                }
                AddToBFPL(aFromOffline);
            }
            (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. BF PL made.");
        }
        static private void AddToBFPL(List<PlaylistItem> aPL)
        {
            foreach (PlaylistItem cPLI in aPL)
            {
                if (cPLI.cFile == null)
                    continue;
                if (cPLI.nID > _nMaxBFID)
                    _nMaxBFID = cPLI.nID;
                cPLI.dtStartReal = DateTime.MaxValue;
                cPLI.dtStopReal = DateTime.MaxValue;
                cPLI.dtStartQueued = DateTime.MaxValue;
                cPLI.cStatus = _cStatusPlanned;

                _aBesiegedFortressPL.Add(cPLI);
                (new Logger.Sync()).WriteNotice("MakeBesiegedFortressPL. added to BF PL: " + cPLI.ToStringShort());
            }
        }
        static private PlaylistItem LastCacheCoveredPLIGet(IEnumerable<PlaylistItem> aPL)
        {
            (new Logger.Sync()).WriteNotice("LastCacheCoveredPLIGet. start");
            List<string> aFilesInCache = FilenamesInCacheGet();
            (new Logger.Sync()).WriteNotice("LastCacheCoveredPLIGet. search in PL " + ((aPL == null || aPL.Count() == 0) ? "[count = NULL]" : ("[count=" + aPL.Count() + "][last_start=" + aPL.Last().ToStringShort() + "]")));

            PlaylistItem cRetVal = null;
            bool bFoundFirst = false;
            lock (_cSyncRoot)
            {
                foreach(PlaylistItem cPLI in aPL)
                {
                    if (cPLI.dtStartPlanned < DateTime.Now || cPLI.bPlug)
                        continue;
                    if (!bFoundFirst)
                    {
                        if (!aFilesInCache.Contains("" + cPLI.nID))
                        {
                            (new Logger.Sync()).WriteDebug("LastCacheCoveredPLIGet. notfound after now = " + cPLI.ToStringShort());
                            continue;
                        }
                        bFoundFirst = true;
                        cRetVal = cPLI;
                    }

                    if (!aFilesInCache.Contains("" + cPLI.nID))
                    {
                        (new Logger.Sync()).WriteNotice("LastCacheCoveredPLIGet. notfound = " + (cPLI == null ? "NULL" : cPLI.ToStringShort()));
                        break;
                    }
                    cRetVal = cPLI;
                }
            }
            (new Logger.Sync()).WriteNotice("LastCacheCoveredPLIGet. found = " + (cRetVal == null ? "NULL" : cRetVal.ToStringShort()));
            return cRetVal;
        }

        static public void Synchronize()
        {
            Logger.Sync cLogger = new Logger.Sync();
			lock (_cSyncRoot)
			{
				if (_bWeAreSyncingNow)
				{
					cLogger.WriteNotice("We are already syncing now - end of sync");
					return;
				}
				else
					_bWeAreSyncingNow = true;

                if (_dtWhenShortPLGetNextTime > DateTime.Now && _aPlaylistOnline.Last().dtStartPlanned > DateTime.Now.AddMinutes(3))
                {
					cLogger.WriteNotice("too much attampts to sync - end of sync [next_time=" + _dtWhenShortPLGetNextTime.ToString("HH:mm:ss") + "]");
					_bWeAreSyncingNow = false;
					return;
				}
				else
					_dtWhenShortPLGetNextTime = DateTime.Now.Add(Preferences.tsSyncPeriodAfterError);
			}

			System.Threading.ManualResetEvent mre = new System.Threading.ManualResetEvent(false);
			System.Threading.ManualResetEvent mre2 = new System.Threading.ManualResetEvent(false);
			System.Threading.Thread actionThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
			{
				try
				{
					Synchronize_Thread();
				}
				catch (Exception ex)
				{
					cLogger.WriteError("Synchronize:catch1", ex);
				}
				finally
				{
					mre.Set();
					mre2.Set();
				}
			}));

			actionThread.Start();
			mre.WaitOne((int)Preferences.tsSyncDurMax.TotalMilliseconds); // ждем 1.5 минуты (90000). 20 сек идёт полный лист на сутки (тормозят только классы, а они мало зависят от количества суток). на 3 суток - 28 секунд
			try
			{
				cLogger.WriteDebug("Sync. Before isalive check");
				if (actionThread.IsAlive)
				{
					cLogger.WriteNotice("Sync will be ABORTED!!!");
					actionThread.Abort();
					mre2.WaitOne();
					cLogger.WriteNotice("Sync was ABORTED!!!");
				}
				cLogger.WriteDebug("Sync. After isAlive check");
				if (_cFailoverInfo != null)
				{
					//cLogger.WriteNotice("offline info saving...");
					//if ((new FailoverInfoWorking()).Save(_cFailoverInfo))
					//	cLogger.WriteNotice("info saved");
					//else
					//	cLogger.WriteError("info NOT saved!!");
					_cFailoverInfo = null;
				}
			}
			catch (Exception ex)
			{
				cLogger.WriteError("Synchronize:catch2", ex);
			}
			cLogger.WriteNotice("stop syncing           ------------------------------");
			_bWeAreSyncingNow = false;
		}

		static public void Synchronize_Thread()
		{
			Logger.Sync cLogger = new Logger.Sync();
			DBInteract cDBI = null;
			Exception exDB = null;
			bool bSyncFirstTime;
            PlaylistItem cPLILast = null;
            int nBPCount;

            lock (_cSyncRoot)
            {
                bSyncFirstTime = _bSyncFirstTime;
                _bSyncFirstTime = false;
                nBPCount = _aPlaylistBig.Count;
                if (nBPCount > 0)
                    cPLILast = _aPlaylistBig.Last();
            }

            cLogger.WriteNotice("start syncing...           =============================== [bf_mode=" + _bBesiegedFortressMode + "][dont_delete_mode=" + _bDoNotDeleteCacheMode + "]");
            if (SIO.File.Exists(_sBesiegedFortressFile))
            {
                cLogger.WriteError("Besieged Fortress Mode turned on! If everything is ok stop failover, rename [" + _sBesiegedFortressFile + "] and [" + FailoverConstants.sFilesDoNotRemoveFromCache + "] files, delete [" + FailoverInfoWorking.sInfoFile + "] file from bin folder, turn on failover");
                _bBesiegedFortressMode = true;
            }
            else if (_bBesiegedFortressMode)
            {
                cLogger.WriteWarning("Besieged Fortress Mode turned off!");
                _bBesiegedFortressMode = false;
                _aBesiegedFortressPL.Clear();
            }

            if (SIO.File.Exists(FailoverConstants.sFilesDoNotRemoveFromCache))
            {
                cLogger.WriteError("Don't Delete Cache Files Mode turned on! Perhaps sync service stopped.");
                _bDoNotDeleteCacheMode = true;
            }
            else if (_bDoNotDeleteCacheMode)
            {
                cLogger.WriteWarning("Don't Delete Cache Files Mode turned off!");
                _bDoNotDeleteCacheMode = false;
            }

            for (int ni = 0; ni < 3; ni++)
			{
				if (null != cDBI)
					break;
				if (ni > 0)
					cLogger.WriteNotice("\t\t...one more attempt to get DBI [" + ni + "]");
				try
				{
					cDBI = new DBInteract();
				}
				catch (Exception ex)
				{
					if (exDB == null)
						cLogger.WriteError(ex);
					exDB = ex;
					System.Threading.Thread.Sleep(300);
				}
			}
			if (null == cDBI)
			{
				cLogger.WriteNotice("THERE IS NO DB AVAILABLE!");
			}
			List<IdNamePair> aStatuses = null;
			Dictionary<long, helpers.replica.mam.Cues> ahCues = new Dictionary<long, helpers.replica.mam.Cues>();
			Queue<PlaylistItem> aPLBig;
			Queue<PlaylistItem> aqPLShort = null;
			bool bEverythingIsOk = true;
			helpers.replica.media.File cFPlug = null;
			List<PlaylistItem> aPL = new List<PlaylistItem>();
			PlaylistItem cPLI;
			PlaylistItem cPLIPrev;
            bool bStartByTime = false;

            try
			{
				if (!_bBesiegedFortressMode && !bSyncFirstTime && null != cDBI && (nBPCount < 1 || ItsSafeToGetBigPL()))  // ======================= PL BIG ======================
				#region PL BIG (FOR OFFLINE WORK) GET
				{
                    try
                    {
                        #region GETTING PL BIG
                        cLogger.WriteNotice("start to get full playlist from DB");
                        aPLBig = cDBI.ComingUpGet(Preferences.tsPLGetDurLong);
                        cLogger.WriteNotice("got playlist from DB [" + (aPLBig == null ? "NULL]" : "count=" + aPLBig.Count + "]"));
                        if (aPLBig.IsNullOrEmpty()) 
                        {
                            System.Threading.Thread.Sleep(500);
                            aPLBig = cDBI.ComingUpGet(Preferences.tsPLGetDurLong);
                            cLogger.WriteNotice("TWICE! got playlist from DB [" + (aPLBig == null ? "NULL]" : "count=" + aPLBig.Count + "]"));
                            if (aPLBig.IsNullOrEmpty())
                                throw new Exception("receive(1) empty playlist from DB");
                        }
                        cLogger.WriteNotice("got playlist from DB [count=" + aPLBig.Count + "]  [start=" + aPLBig.First().ToStringShort() + "]  [stop=" + aPLBig.Last().ToStringShort() + "]");
                        if (aPLBig.Last().dtStopPlanned < DateTime.Now.AddMinutes(90))
                            throw new Exception("receive(2) too small playlist from DB (maybe it was finished) - [id=" + aPLBig.Last().nID + "][stop=" + aPLBig.Last().dtStopPlanned.ToStr() + "]");
                        if (cPLILast != null && cPLILast.dtStartPlanned > aPLBig.Last().dtStartPlanned.AddHours(2))
                            throw new Exception("receive(3) playlist from DB smaller than old playlist (maybe it was unexpectedly cuted) - [new last id=" + aPLBig.Last().nID + "][new last start=" + aPLBig.Last().dtStartPlanned.ToStr() + "][old last id=" + cPLILast.nID + "][old last start=" + cPLILast.dtStartPlanned.ToStr() + "]");
                        ahErrors[ErrorTarget.dbi_toatalpl] = DateTime.MinValue;
                        #endregion
                        #region SAVING PL BIG
						try // PLUG
						{
							cLogger.WriteNotice("start to get plugs");
							cFPlug = cDBI.PlaylistPlugsGet().First();
							ahErrors[ErrorTarget.dbi_plug] = DateTime.MinValue;
						}
						catch (Exception ex)
						{
							bEverythingIsOk = false;
							if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_plug])
								cLogger.WriteError(ex);
							ahErrors[ErrorTarget.dbi_plug] = DateTime.Now;
							cLogger.WriteError(new Exception("playlist synchronization failed"));
						}
						lock (_cSyncRoot)
						{
							if (cFPlug == null || cFPlug.sFile.IsNullOrEmpty() || !SIO.File.Exists(cFPlug.sFile))
								_cPlug = Preferences.cDefaultPlug;
							else
								_cPlug = cFPlug;
						}


						try // FRAMES INITIALS
						{
							cLogger.WriteNotice("start to get initials");
							lock (_cSyncRoot)
							{
								_ahFrameStopsInitials = cDBI.PlaylistItemsFramesStopInitialsGet();
							}
							ahErrors[ErrorTarget.dbi_framesinitial] = DateTime.MinValue;
						}
						catch (Exception ex)
						{
							bEverythingIsOk = false;
							if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_framesinitial])
								cLogger.WriteError(ex);
							ahErrors[ErrorTarget.dbi_framesinitial] = DateTime.Now;
						}


						try // STATUSES
						{
							cLogger.WriteNotice("start to get statuses");
							aStatuses = new List<IdNamePair>(cDBI.PlaylistItemsStatusesGet());
							ahErrors[ErrorTarget.dbi_statuses] = DateTime.MinValue;
						}
						catch (Exception ex)
						{
							bEverythingIsOk = false;
							if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_statuses])
								cLogger.WriteError(ex);
							ahErrors[ErrorTarget.dbi_statuses] = DateTime.Now;
						}
						lock (_cSyncRoot)
						{
							if (null != aStatuses)
							{
								IdNamePair cStatus;
								if (!_aStatuses.IsNullOrEmpty())
								{
									foreach (IdNamePair cINP in _aStatuses)
										if (null != (cStatus = aStatuses.FirstOrDefault(o => cINP.nID == o.nID)))
											aStatuses.Remove(cStatus);
									aStatuses.AddRange(_aStatuses);
								}
								_aStatuses = aStatuses.ToArray();
							}
						}


						try // CUES
						{
							cLogger.WriteNotice("start to get cues");
							ahCues = cDBI.ComingUpAssetsCuesGet();
							cLogger.WriteNotice("cues was got successfully. [count=" + ahCues.Count + "]");
							ahErrors[ErrorTarget.dbi_cues] = DateTime.MinValue;

                            lock (_cSyncRoot)
                            {
                                foreach (PlaylistItem cPLIQueued in _aPlaylistOnline)
                                {
                                    if (null != cPLIQueued.cAsset && _ahCues.ContainsKey(cPLIQueued.cAsset.nID) && !ahCues.ContainsKey(cPLIQueued.cAsset.nID))
                                        ahCues.Add(cPLIQueued.cAsset.nID, _ahCues[cPLIQueued.cAsset.nID]);
                                }
                                _ahCues = ahCues;
                            }
                        }
                        catch (Exception ex)
						{
							bEverythingIsOk = false;
							if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_cues])
								cLogger.WriteError(ex);
							ahErrors[ErrorTarget.dbi_cues] = DateTime.Now;
						}

                        // Checking PL on cache coverage 
                        PlaylistItem cPLILastCoveredTMP = LastCacheCoveredPLIGet(aPLBig);
                        if (cPLILastCoveredTMP.dtStopPlanned < DateTime.Now.AddHours(3) || cPLILastCoveredTMP.dtStopPlanned < DateTime.Now.Add(Preferences.tsMinCacheCover))
                        {
                            if (_aPlaylistBig.IsNullOrEmpty())
                                cLogger.WriteError("new PL is low cache covered! But we should get it, cause the current is empty! [cover_id=" + cPLILastCoveredTMP.nID + "][cover_stopp=" + cPLILastCoveredTMP.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            else
                            {
                                cLogger.WriteError("new PL is low cache covered!!! Stay with current PL!!! [cover_id=" + cPLILastCoveredTMP.nID + "][cover_stopp=" + cPLILastCoveredTMP.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                                bEverythingIsOk = false;
                            }
                        }

                        if (bEverythingIsOk)  // PL SAVE
                        {
                            long nPLIID = GetPLIIDChangesStartedFrom(aPLBig);
                            lock (_cSyncRoot)
                            {
                                _aPlaylistBig.Clear();
                                foreach (PlaylistItem cPLI2 in aPLBig)
                                {
                                    _aPlaylistBig.AddLast(cPLI2);
                                }

                                cLogger.WriteNotice("playlist for offline work was got successfully. [count=" + _aPlaylistBig.Count + "]");
                                _cFailoverInfo = new FailoverInfo(_ahCheckedFiles, _ahCheckedFilesDurs, _aStatuses, _aPlaylistBig, _ahFrameStopsInitials, _ahPLIIDs_Binds, _ahCues, _cPlug);
                                cLogger.WriteNotice("offline info saving...");
                                if ((new FailoverInfoWorking()).Save(_cFailoverInfo))
                                    cLogger.WriteNotice("offline info saved");
                                else
                                    cLogger.WriteError("offline info NOT saved!!");

                                _nClassesRenewPLIIDStartFrom = nPLIID;
                            }

                            _dtWhenBigPLGetNextTime = DateTime.Now.Add(Preferences.tsSyncPeriodLong);
                        }
                        else
                        {
                            _dtWhenBigPLGetNextTime = DateTime.Now.AddMinutes(10);
                            cLogger.WriteError("playlist for offline work was NOT got successfully!! [when=" + _dtWhenBigPLGetNextTime.ToString("HH:mm:ss") + "]");
                        }
                        #endregion
                    }
                    catch (Exception ex)
					{
						if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_toatalpl])
                            cLogger.WriteError("getting big PL", ex);
                        ahErrors[ErrorTarget.dbi_toatalpl] = DateTime.Now;
					}
                }
                #endregion

                #region STATUSES and CUES GET
                if (!_bBesiegedFortressMode)
                {
                    lock (_cSyncRoot)
                    {
                        RecalcPL();
                        if (_aStatuses.IsNullOrEmpty())
                        {
                            try // STATUSES FIRST TIME
                            {
                                cLogger.WriteNotice("first time. start to get statuses");
                                _aStatuses = cDBI.PlaylistItemsStatusesGet();
                            }
                            catch (Exception ex)
                            {
                                cLogger.WriteError(ex);
                                return;
                            }
                            if (_aStatuses == null)
                            {
                                cLogger.WriteWarning("statuses were not got. making fake!");
                                _aStatuses = Preferences.FakeStatusesGet();
                            }
                        }
                        if (_ahCues.IsNullOrEmpty())
                        {
                            try
                            {
                                cLogger.WriteNotice("first time. start to get cues");
                                lock (_cSyncRoot)
                                {
                                    _ahCues = cDBI.ComingUpAssetsCuesGet();
                                }
                                cLogger.WriteNotice("first time. cues was got successfully. [count=" + _ahCues.Count + "]");
                            }
                            catch (Exception ex)
                            {
                                cLogger.WriteError("first time. cues getting", ex);
                            }
                        }
                    }
                }
                else
                {
                    if (_aStatuses == null)
                    {
                        cLogger.WriteWarning("statuses were not got in BF mode. making fake!");
                        _aStatuses = Preferences.FakeStatusesGet();
                    }
                }
                #endregion

                #region ENTERING _bBesiegedFortressMode or _bDoNotDeleteCacheMode
                // DNF подумать!!!!
                // по сути _bDoNotDeleteCacheMode не нужен? Т.к. если всем запрещено качать и удалять, то ситуация не может улучшаться...
                // надо только, если конец ПЛ, кэш длинный и люди просто не положили еще следующий день. Подумать как отключить этот режим,
                // если ПЛ опять длинный - надо резрешать качать синкеру... может ему можно качать до 2х кэша всегда? А тогда и из режима вскоре выйдем... ??
                // да, так и сделали, только 1.5 кэша можно перебрать
                PlaylistItem cPLILastCovered = null;
                if (!_bBesiegedFortressMode)
                    cPLILastCovered = LastCacheCoveredPLIGet(_aPlaylistBig);
                if (null != cPLILastCovered)
                {
                    if (!_bBesiegedFortressMode && (_aPlaylistBig.Last().dtStopPlanned < DateTime.Now.AddHours(3) || cPLILastCovered.dtStopPlanned < DateTime.Now.AddHours(3)))
                    {
                        _bBesiegedFortressMode = true;
                        if (!SIO.File.Exists(_sBesiegedFortressFile))
                        {
                            bool bRes = FailoverConstants.EnterBesiegedFortressMode(Player.Preferences.sCacheFolder, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Besieged Fortress Mode entered by failover-1. Reason: Too short PL (or Offline_PL). [last_id=" + _aPlaylistBig.Last().nID + "][last_stopp=" + _aPlaylistBig.Last().dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][cover_id="+ cPLILastCovered.nID + "][cover_stopp=" + cPLILastCovered.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                            if (bRes)
                                cLogger.WriteError("We entered BesiegedFortressMode!! CHECK DB and RAID and SYNC service");
                            else
                                cLogger.WriteError("error entering BesiegedFortressMode!!");
                        }
                    }

                    if (!_bBesiegedFortressMode)
                    {
                        bool bSyncStopped = FailoverConstants.SyncerWasProbablyStopped(Player.Preferences.sCacheFolder, 55);
                        if (!_bDoNotDeleteCacheMode && bSyncStopped && cPLILastCovered.dtStopPlanned < DateTime.Now.Add(Preferences.tsMinCacheCover))
                        {
                            _bDoNotDeleteCacheMode = true;
                            if (!SIO.File.Exists(FailoverConstants.sFilesDoNotRemoveFromCache))
                            {
                                bool bRes = FailoverConstants.EnterFilesDoNotRemoveMode(Player.Preferences.sCacheFolder, "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Don't Remove Files From Cache Mode entered by failover-1. Reason: Sync service was probably stopped AND cache covers less than " + Preferences.tsMinCacheCover.TotalHours + " hours");

                                if (bRes)
                                    cLogger.WriteError("We entered FilesDoNotRemoveFromCache Mode!! CHECK DB and RAID and SYNC service. Maybe PL is too short");
                                else
                                    cLogger.WriteError("error entering FilesDoNotRemoveFromCache Mode!!");
                            }
                        }

                        if (_bDoNotDeleteCacheMode && (!bSyncStopped || cPLILastCovered.dtStopPlanned >= DateTime.Now.Add(Preferences.tsMinCacheCover)))
                        {
                            bool bRes = FailoverConstants.ExitFilesDoNotRemoveMode(Player.Preferences.sCacheFolder);
                            if (bRes)
                                cLogger.WriteError("We exited FilesDoNotRemoveFromCache Mode!! Maybe PL was too short");
                            else
                                cLogger.WriteError("error exitting FilesDoNotRemoveFromCache Mode!!");
                        }
                    }
                }
                else if (!_bBesiegedFortressMode)
                    cLogger.WriteError("error: no PL items in cache found!!");
                #endregion

                #region PL SHORT GET
                if (!_bBesiegedFortressMode)
                {
                    try  // ======================= PL SHORT ======================
                    {
                        cLogger.WriteNotice("start to get short playlist from DB");
                        aqPLShort = cDBI.ComingUpGet(Preferences.tsPLGetDurShort);
                        cLogger.WriteNotice("got short playlist from DB [count=" + (aqPLShort == null ? "NULL]" : (aqPLShort.Count + "]")));
                        if (aqPLShort.IsNullOrEmpty())
                        {
                            System.Threading.Thread.Sleep(500);
                            aqPLShort = cDBI.ComingUpGet(Preferences.tsPLGetDurShort);
                            cLogger.WriteWarning("twice! got short playlist from DB [count=" + (aqPLShort == null ? "NULL]" : (aqPLShort.Count + "]")));
                            if (aqPLShort.IsNullOrEmpty())
                                throw new Exception("receive(2) empty playlist from DB");
                        }
                        cLogger.WriteNotice("got short playlist from DB [count=" + (aqPLShort.IsNullOrEmpty() ? "NULL]" : (aqPLShort.Count + "][last=" + aqPLShort.Last().ToStringShort() + "]")));
                        //if (_ahClassesNames_Classes != null && _ahPLIIDs_Binds != null)
                        //	ClassesRenew(cDBI, aqPLShort, true); 

                        cLogger.WriteNotice("got short playlist from DB - [qty=" + aqPLShort.Count + "][last=" + aqPLShort.Last().dtStopPlanned.ToString("HH:mm:ss") + "]");
                        ahErrors[ErrorTarget.dbi_shortpl] = DateTime.MinValue;
                    }
                    catch (Exception ex)
                    {
                        if (DateTime.MinValue == ahErrors[ErrorTarget.dbi_shortpl])
                            cLogger.WriteError(ex);
                        ahErrors[ErrorTarget.dbi_shortpl] = DateTime.Now;

                        // всё плохо - ПЛ не взялся
                        //if (PLDurationIsSafe() && PLCountIsSafe()) // но можно подождать   -- чо-то были траблы с ожиданием - можно и не дождаться ))
                        //{
                        //	cLogger.WriteWarning("short playlist is empty, but we can wait. end sync.");
                        //	return;
                        //}
                        cLogger.WriteNotice("start to get short playlist from offline PL");

                        if (_aPlaylistBig.Last().dtStopPlanned < DateTime.Now.AddMinutes(120))
                        {
                            _bBesiegedFortressMode = true;
                            if (!SIO.File.Exists(_sBesiegedFortressFile))
                                File.WriteAllText(_sBesiegedFortressFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Besieged Fortress Mode entered by failover-2. Reason: Too short Offline_PL. [last_id=" + _aPlaylistBig.Last().nID + "][last_stopp=" + _aPlaylistBig.Last().dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                        }
                        if (!_bBesiegedFortressMode)
                        {
                            lock (_cSyncRoot)
                            {
                                aqPLShort = ShortPLGetOffline();
                            }

                            if (null == aqPLShort || aqPLShort.Count < 1)
                                throw new Exception("receive(3) empty playlist from Offline PL!!");
                        }
                    }
                } // no else!!

                if (_bBesiegedFortressMode)
                {
                    if (_aBesiegedFortressPL.IsNullOrEmpty())
                    {
                        MakeBesiegedFortressPL();
                        cLogger.WriteNotice("made BesiegedFortressPL [count=" + _aBesiegedFortressPL.Count + "][begin=" + _aBesiegedFortressPL[0].dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][end=" + _aBesiegedFortressPL[_aBesiegedFortressPL.Count - 1].dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                    }
                    aqPLShort = ShortPLGetFromBesiegedFortressPL();

                    if (null == aqPLShort || aqPLShort.Count < 1)
                        throw new Exception("receive(4) empty playlist from Besieged Fortress PL!!");

                    PlaylistItem cPLISh = _aBesiegedFortressPL[_aBesiegedFortressPL.Count - 1];
                    cLogger.WriteNotice("got short PL from BesiegedFortressPL. Last element is: [id=" + cPLISh.nID + "][name=" + cPLISh.sName + "][start=" + cPLISh.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "][stop=" + cPLISh.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                }
                #endregion

                lock (_cSyncRoot)
                    PlaylistClean(); // in BF mode we can do it. check is inside.
            }
            catch (Exception ex)
			{
				cLogger.WriteError(new Exception("playlist synchronization failed", ex));
				return;
			}

#if DEBUG
            //return;  
#endif

            try // добавление в текущий ПЛ из aqPLShort
            {
				PlaylistItem cPLIFound = null;
				bool bFound = false;
				DateTime dtNow = DateTime.Now;
				DateTime dtPrevStop;
				long nDiff;
				long nDur, nFileDur;
				bool bOffline = DateTime.MinValue < ahErrors[ErrorTarget.dbi_shortpl];
				string[] aStatusesLockedNames = _aStatusesLocked.Select(o => o.sName).ToArray();
				string sPLIFull, sPLILight;
				long[] aIDsOnline;
				Class cPlugClass;
				int nCountOnline;
				bool bCheckPlug=true;



				lock (_cSyncRoot)
				{
					_nPlugID = _aPlaylistOnline.Count > 0 ? _aPlaylistOnline.Min(o => o.nID) - 1 : -1;
					aIDsOnline = _aPlaylistOnline.Select(o => o.nID).ToArray();
					cPlugClass = null; // will do default binds
					nCountOnline = PLCount();
                }
                cLogger.WriteNotice("got [" + aqPLShort.Count + "] PLI elements [offline=" + bOffline + "][PlaylistOnline=" + nCountOnline + "]");

                DateTime dtLastOnlinePLIStop = DateTime.MinValue;
				if (_cLastOnlinePLI != null)
					dtLastOnlinePLIStop = _cLastOnlinePLI.dtStopReal < DateTime.MaxValue ? _cLastOnlinePLI.dtStopReal : _cLastOnlinePLI.dtStopPlanned;
				dtLastOnlinePLIStop = dtLastOnlinePLIStop < dtNow ? dtNow : dtLastOnlinePLIStop;
				if (_cLastOnlinePLI != null && dtLastOnlinePLIStop > dtNow)
				{
					cPLIFound = aqPLShort.FirstOrDefault(o => o.nID == _cLastOnlinePLI.nID);
					if (cPLIFound == null)
						cLogger.WriteNotice("end_of_playlist pli NOT found in new PL. We will add by time [id=" + _cLastOnlinePLI.nID + "][start_p=" + cPLIFound.dtStartPlanned.ToString("HH:mm:ss") + "]");
					else
						cLogger.WriteNotice("end_of_playlist pli found in new PL [id=" + cPLIFound.nID + "][old_stop=" + _cLastOnlinePLI.dtStopPlanned.ToString("HH:mm:ss") + "][new_stop=" + cPLIFound.dtStopPlanned.ToString("HH:mm:ss") + "]");
				}


                bStartByTime = false;
                DateTime dtIfStartByTimeEndPL = DateTime.MaxValue;

                while (0 < aqPLShort.Count)
				{
					cPLI = aqPLShort.Dequeue();
					#region some initial checks with cPLI
					if (cPLI == null)
					{
						cLogger.WriteNotice("something goes wrong [0]. PLI is NULL.");
						continue;
					}
					if (0 < aPL.Count(o => cPLI.nID == o.nID))
					{
						cLogger.WriteNotice("something goes wrong [1]. IDs eqals. skip on adding. [id=" + cPLI.nID + "]");
						continue;
					}
                    if (bFound && !_bBesiegedFortressMode && aIDsOnline.Contains(cPLI.nID))
					{
						cLogger.WriteNotice("something goes wrong [2]. IDs eqals online. skip on adding. [id=" + cPLI.nID + "]");
						continue;
					}
					if (bFound && DateTime.MaxValue > cPLI.dtStopReal)
					{
						cLogger.WriteNotice("something goes wrong [3]. realstop is not null. [id=" + cPLI.nID + "]");
					}
					if (null == cPLI.cFile)
					{
						cLogger.WriteNotice("something goes wrong [4]. file is null. [id=" + cPLI.nID + "]");
						continue;
                    }

                    if (bStartByTime && dtIfStartByTimeEndPL < cPLI.dtStartPlanned)
                    {
                        cLogger.WriteNotice("we started by time [start_time=" + dtLastOnlinePLIStop.ToString("HH:mm:ss") + "] and we've added [" + aPL.Count + "] PLIs up to [end_time=" + dtIfStartByTimeEndPL.ToString("HH:mm:ss") + "][next_pli_sp=" + cPLI.dtStartPlanned.ToString("HH:mm:ss") + "]");
                        break;
                    }

                    ReplaceInPath(cPLI);

					sPLIFull = "[" + cPLI.ToString() + "]";
					sPLILight = "[" + cPLI.nID + "][startp=" + cPLI.dtStartPlanned.ToString("HH:mm:ss") + "][file=" + (cPLI.cFile == null ? "NULL" : cPLI.cFile.sFilename) + "][dur=" + cPLI.nDuration + "][stopp=" + cPLI.dtStopPlanned.ToString("HH:mm:ss") + "]";

                    if (cPLI.bPlug && cPLI.nID > 0)  // plug is from DB, not our plug
                    {
						cLogger.WriteNotice("Removed plug. " + sPLILight);
						continue;
					}
					if (_ahFrameStopsInitials.ContainsKey(cPLI.nID))
					{
						cPLI.nFrameStop = _ahFrameStopsInitials[cPLI.nID];
					}
					if (DateTime.MaxValue > cPLI.dtStartReal)
					{
						cPLI.dtStartPlanned = cPLI.dtStartReal;
						cPLI.dtStartQueued = cPLI.dtStartReal;
					}
					else if (DateTime.MaxValue > cPLI.dtStartQueued)
						cPLI.dtStartPlanned = cPLI.dtStartQueued;
					cPLI.dtStartReal = DateTime.MaxValue;
					cPLI.dtStartQueued = DateTime.MaxValue;
					#endregion

					if (bFound)
					{
						#region adding cPLI

						if (aPL.Count > 0)
							cPLIPrev = aPL.Last();
						else
							cPLIPrev = _cLastOnlinePLI;

						if (ShouldAddPLIToPL(cPLIPrev, cPLI, aStatusesLockedNames, nCountOnline + aPL.Count, bCheckPlug))
						{
							cLogger.WriteNotice("try to add into playlist [count=" + aPL.Count + "]: " + sPLIFull);


							if (PLIorPlugsAdd(aPL, cPLI, cPlugClass, cLogger, true) == PLIorPlugs.Plugs)
							{
								bCheckPlug = false;
								continue;
							}

							//System.Threading.Thread.Sleep(9000000); //DNF

							cLogger.WriteDebug("try to add into playlist - 2 " + sPLILight);
							if (DateTime.MaxValue > cPLI.dtStartHard || (DateTime.MaxValue > cPLI.dtStartSoft && (DateTime.MaxValue == cPLIPrev.dtStartHardSoft || cPLI.dtStartSoft.Subtract(cPLIPrev.dtStartHardSoft).TotalSeconds > 1)))
							{ // т.е. начался новый блок - или хард или софт
								cLogger.WriteNotice("\t\tblock beginning detected [id=" + cPLI.nID + "]");
								dtPrevStop = cPLIPrev.dtStartPlanned.AddMilliseconds((long)cPLIPrev.nDuration * Player.Preferences.nFrameMs); // т.е. из пленнеда, а не из реала, который со сдвигом
								nDiff = (long)(cPLI.dtStartPlanned.Subtract(dtPrevStop).TotalSeconds * Player.Preferences.nFPS + 0.5); // зазор в кадрах

								if (Math.Abs(nDiff) > 10 * Player.Preferences.nFPS) // 10 кадров. иначе ничего не делаем
								{
									cLogger.WriteNotice("\t\tdiff is greater than 10sec = " + nDiff);
									if (nDiff >= 0) // т.е. мы позже идём, есть зазор между бкп листом и мейном
									{
										aPL.AddRange(PlugsGet(cPlugClass, dtPrevStop, nDiff, cLogger));
									}
									else // т.е. мы раньше идём - надо отгрызть лишнее
									{
										nDur = (long)dtPrevStop.Subtract(cPLIPrev.dtStartPlanned).TotalSeconds * Player.Preferences.nFPS + nDiff;
										if (0 < nDur && nDur < (long)cPLIPrev.nDuration)
										{
											cPLIPrev.nFrameStop = nDur + cPLIPrev.nFrameStart - 1;
											cLogger.WriteNotice("\t\tDuration restricted! " + nDiff);
										}
										else if (cPLIPrev != _cLastOnlinePLI)
										{
											aPL.Remove(cPLIPrev);
											cLogger.WriteNotice("\t\tPLI Removed - no time! " + cPLIPrev.cFile.sFilename);
										}
									}
								}
								else
									cLogger.WriteNotice("\t\tdiff=" + nDiff);

								if (aPL.Count > 0)
									cPLIPrev = aPL.Last();
								else
									cPLIPrev = _cLastOnlinePLI;
							}
							cPLI.dtStartPlanned = cPLIPrev.dtStartPlanned.AddMilliseconds((long)cPLIPrev.nDuration * Player.Preferences.nFrameMs); // т.е. из пленнеда, а не из реала, который со сдвигом
							cLogger.WriteDebug("try to add into playlist - 3 " + sPLILight);

							cPLI.cStatus = _cStatusPlanned;
							aPL.Add(cPLI);
							cLogger.WriteNotice("\t\tadded [id=" + cPLI.nID + "][new start=" + cPLI.dtStartPlanned.ToString("HH:mm:ss") + "][new stopp=" + cPLI.dtStopPlanned.ToString("HH:mm:ss") + "]");
						}
						else
							break;
						#endregion
						continue;
					}
					else if (cPLIFound != null) // ищем место по ID
					{
						if (cPLI.nID == cPLIFound.nID)
						{
							bFound = true;
							cLogger.WriteNotice("stick to this found by ID PLI [" + cPLI.nID + "][old_stop=" + _cLastOnlinePLI.dtStopPlanned.ToString("HH:mm:ss") + "][new_stop=" + cPLIFound.dtStopPlanned.ToString("HH:mm:ss") + "]");
						}
					}
					else  // ищем место по времени
					{
                        if (cPLI.dtStopPlanned > dtLastOnlinePLIStop.AddSeconds(15))
						{
                            bStartByTime = true;
                            dtIfStartByTimeEndPL = DateTime.Now.AddMinutes(15);

                            if (cPLI.dtStartPlanned > dtLastOnlinePLIStop) // т.е. он оторвался немного - нужен плаг
							{
								nDiff = (long)(cPLI.dtStartPlanned.Subtract(dtLastOnlinePLIStop).TotalSeconds * Player.Preferences.nFPS); // зазор в кадрах
								aPL.AddRange(PlugsGet(cPlugClass, dtLastOnlinePLIStop, nDiff, cLogger));
								dtLastOnlinePLIStop = aPL.Last().dtStartPlanned.AddMilliseconds((long)aPL.Last().nDuration * Player.Preferences.nFrameMs); // т.е. из пленнеда, а не из реала, который со сдвигом
								cLogger.WriteNotice("plug added (1) " + sPLILight);
							}
							else // нужна подрезочка
							{
								cPLI.nFrameStart = (long)(dtLastOnlinePLIStop.Subtract(cPLI.dtStartPlanned).TotalSeconds * Player.Preferences.nFPS) + 1;
							}

							cPLI.dtStartPlanned = dtLastOnlinePLIStop;

							PLIorPlugsAdd(aPL, cPLI, cPlugClass, cLogger, false);

							bFound = true;
							cLogger.WriteNotice("start from this found by Time PLI " + sPLILight);
						}
					}
				}

				ahErrors[ErrorTarget.playlist] = DateTime.MinValue;
			}
			catch (Exception ex)
			{
				cLogger.WriteDebug("catch_in");
				if (DateTime.MinValue == ahErrors[ErrorTarget.playlist])
					cLogger.WriteError(ex);
				ahErrors[ErrorTarget.playlist] = DateTime.Now;
				cLogger.WriteDebug("catch_out");
			}


			lock (_cSyncRoot)
			{
                if (aPL.Count < 1)
                {
                    cLogger.WriteNotice("Strange situation - we cant add anything to PL!");
                }
                else
                {
                    if (bStartByTime)
                    {
                        cLogger.WriteNotice("start by time. correcting start. [frq=" + aPL[0].nFramesQty + "][stopp=" + aPL[0].dtStopPlanned.ToString("HH:mm:ss") + "][sub=" + (long)(aPL[0].dtStopPlanned.Subtract(DateTime.Now).TotalSeconds * Player.Preferences.nFPS) + "]");
                        aPL[0].nFrameStart = aPL[0].nFramesQty - (long)(aPL[0].dtStopPlanned.Subtract(DateTime.Now).TotalSeconds * Player.Preferences.nFPS);
                        cLogger.WriteNotice("start by time. corrected start is [frs=" + aPL[0].nFrameStart + "]");
                        if (aPL[0].nFrameStart < 1)
                            aPL[0].nFrameStart = 1;
                        if (aPL[0].nFrameStart + 100 >= aPL[0].nFramesQty)
                        {
                            cLogger.WriteNotice("start by time. too smal pli - remove [id=" + aPL[0].nID + "]");
                            aPL.RemoveAt(0);
                        }
                    }
                    foreach (PlaylistItem cPLI2 in aPL)
                    {
                        _aPlaylistOnline.AddLast(cPLI2);
                        _cLastOnlinePLI = cPLI2;
                    }
                }
                cLogger.WriteNotice("now we have [PlaylistOnline_greater_now=" + PLCount() + "][last_start=" + (_cLastOnlinePLI == null ? "NULL" : _cLastOnlinePLI.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss")) + "][last_stop=" + (_cLastOnlinePLI == null ? "NULL" : _cLastOnlinePLI.dtStopPlanned.ToString("yyyy-MM-dd HH:mm:ss")) + "]");
                _dtWhenShortPLGetNextTime = DateTime.Now.Add(Preferences.tsSyncPeriodShort);
			}
		}

		private enum PLIorPlugs
		{
			PLI,
			Plugs,
		}
		static private PLIorPlugs PLIorPlugsAdd(List<PlaylistItem> aPL, PlaylistItem cPLI, Class cPlugClass, Logger.Sync cLogger, bool bAddOnlyPlugs)
		{
			long nFileDur;
			long nDiff=0;
			DateTime dtPLIStop = cPLI.dtStartPlanned.AddMilliseconds((long)cPLI.nDuration * Player.Preferences.nFrameMs); // т.е. из пленнеда, а не из реала, который со сдвигом
			cLogger.WriteDebug3("\t\t\t[PLIorPlugsAdd] - 0 ");
			if (1 > (nFileDur = FileIsOk(cPLI.cFile)))
            {
                cLogger.WriteDebug("\t\t\t[PLIorPlugsAdd] - file is NOT ok [dur=" + nFileDur + "]");
                PlaylistItem cPLIplug = null;
				if (aPL.Count > 0 && (cPLIplug = aPL.Last()).bPlug)
				{
					while (aPL.Count > 0 && (cPLIplug = aPL.Last()).bPlug)
						aPL.Remove(cPLIplug);
					aPL.AddRange(PlugsGet(cPlugClass, cPLIplug.dtStartPlanned, (long)(dtPLIStop.Subtract(cPLIplug.dtStartPlanned).TotalSeconds * Player.Preferences.nFPS), cLogger));
				}
				else
					aPL.AddRange(PlugsGet(cPlugClass, cPLI.dtStartPlanned, (long)(dtPLIStop.Subtract(cPLI.dtStartPlanned).TotalSeconds * Player.Preferences.nFPS), cLogger));
				cLogger.WriteDebug3("\t\t\t[PLIorPlugsAdd] - 2 ");
				return PLIorPlugs.Plugs;
			}
			else
			{
                cLogger.WriteDebug("\t\t\t[PLIorPlugsAdd] - file is ok [dur=" + nFileDur + "]");
                if ((long)cPLI.nDuration > nFileDur)
				{
					nDiff = (long)cPLI.nDuration - nFileDur;
					cPLI.nFrameStop = cPLI.nFramesQty = nFileDur;
					cPLI.nFrameStart = cPLI.nFrameStart - nDiff < 1 ? 1 : cPLI.nFrameStart - nDiff;
				}
				cLogger.WriteDebug3("\t\t\t[PLIorPlugsAdd] - 4 ");
				if (!bAddOnlyPlugs)
					aPL.Add(cPLI);
				return PLIorPlugs.PLI;
			}
		}
		static private List<PlaylistItem> PlugsGet(Class cPlugClass, DateTime dtStart, long nDur, Logger.Sync cLogger)
		{
			helpers.replica.media.File cFPlug = _cPlug;
			List<PlaylistItem> aPlugs = new List<PlaylistItem>();
			PlaylistItem cPlug;
			long nCurrentDur = 0, nPlugDur = 0;
			if (1 > (nPlugDur = FileIsOk(cFPlug)))
			{
				if (1 > (nPlugDur = FileIsOk(Preferences.cDefaultPlug)))
					throw new Exception("CANNOT FIND ANY VALID PLUG FILE!!!!! [first=" + cFPlug + "][second=" + Preferences.cDefaultPlug + "]");
				cFPlug = Preferences.cDefaultPlug;
			}
			
			while (nCurrentDur < nDur)
			{
				DateTime dtNextPLIStart = dtStart.AddMilliseconds(nCurrentDur * Player.Preferences.nFrameMs); // т.е. из пленнеда, а не из реала, который со сдвигом
				cPlug = PlaylistItemCreate(cFPlug, cPlugClass, dtNextPLIStart, (nDur - nCurrentDur) <= nPlugDur ? nDur - nCurrentDur : nPlugDur, dtStart, _nPlugID--);
				cPlug.bPlug = true;
				cPlug.cStatus = _cStatusPlanned;
				aPlugs.Add(cPlug);
				nCurrentDur += (long)cPlug.nDuration;
				cLogger.WriteNotice("\t\tPLUG ADDed! [" + cPlug.nID + "][startp=" + cPlug.dtStartPlanned.ToString("HH:mm:ss") + "][file=" + (cPlug.cFile == null ? "NULL" : cPlug.cFile.sFilename) + "][dur=" + cPlug.nDuration + "][stopp=" + cPlug.dtStopPlanned.ToString("HH:mm:ss") + "][class:" + (null == cPlug.aClasses ? "NULL" : "[" + cPlug.aClasses.ToStr() + "]") + "][plug_class=" + (cPlugClass == null ? "NULL" : cPlugClass.sName) + "]");
			}
			return aPlugs;
		}

        // реализация Playlist.IInteract
        static public PlaylistItem PlaylistItemCurrentGet()
		{
			PlaylistItem cRetVal = null;
			try
			{
				Synchronize();
				DateTime dtNow = DateTime.Now;
                bool bIsNull;
                do
                {
                    lock (_cSyncRoot)
                        bIsNull = _aPlaylist.IsNullOrEmpty();
                    if (bIsNull)
                        System.Threading.Thread.Sleep(10);
                }
                while (bIsNull);

                lock (_cSyncRoot)
                    cRetVal = _aPlaylist.FirstOrDefault(o => o.dtStart < dtNow && o.dtStop > dtNow);
            }
            catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
			return cRetVal;
		}
		static public PlaylistItem PlaylistItemLockedPreviousGet()
		{
			PlaylistItem cRetVal = null;
			try
			{
				lock (_cSyncRoot)
					cRetVal = _aPlaylist.Where(o => _aStatusesLocked.Contains(o.cStatus)).OrderByDescending(o => o.dtStartReal).ThenByDescending(o => o.dtStartQueued).FirstOrDefault();
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}

			return cRetVal;
		}
		static private PlaylistItem[] PlaylistItemsPlannedGet()
		{
			PlaylistItem[] aRetVal = null;
			lock (_cSyncRoot)
				aRetVal = _aPlaylist.Where(o => _cStatusPlanned == o.cStatus).OrderBy(o => o.dtStartPlanned).ToArray();
			return aRetVal;
		}
		static public PlaylistItem PlaylistItemOnAirGet()
		{
			lock (_cSyncRoot)
				return _aPlaylist.Where(o => _cStatusOnAir == o.cStatus).OrderByDescending(o => o.dtStartReal).FirstOrDefault();
		}
		static public ulong PlaylistItemOnAirFramesLeftGet(int nOneFrameInMs)
        {
			ulong nRetVal = 0, nCurrentFramesPast;
			try
			{
				PlaylistItem cPLI = null;
                lock (_cSyncRoot)
                    if (null != (cPLI = _aPlaylist.Where(o => _cStatusOnAir == o.cStatus).OrderByDescending(o => o.dtStartReal).FirstOrDefault()))
                    {
                        nRetVal = cPLI.nDuration;
                        nCurrentFramesPast = (ulong)(DateTime.Now.Subtract(cPLI.dtStartReal).TotalMilliseconds / nOneFrameInMs);
                        nRetVal = nRetVal - nCurrentFramesPast;
                    }
            }
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
			return nRetVal;
		}
		static private long FileIsOk(helpers.replica.media.File cFile)
		{
			(new Logger.Sync()).WriteDebug3("\t\t\t\t[FileIsOk] - in");
			if (null == cFile)
			{
				(new Logger.Sync()).WriteError("File is NULL!!");
				return -1;
			}
			if (!SIO.File.Exists(cFile.sFile))
			{
				(new Logger.Sync()).WriteError("NO FILE!! ["+ cFile.sFile + "]");
				return -1;
			}
			(new Logger.Sync()).WriteDebug3("\t\t\t\t[FileIsOk] - 1 ");
			if (_ahCheckedFiles.ContainsKey(cFile.sFile) && _ahCheckedFiles[cFile.sFile] == SIO.File.GetLastWriteTime(cFile.sFile))
				return _ahCheckedFilesDurs[cFile.sFile];

			long nDuration = -1;

			System.Threading.ManualResetEvent mre = new System.Threading.ManualResetEvent(false);
			System.Threading.Thread actionThread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
			{
				try
				{
					nDuration = FileIsOkInFFMPEG(cFile);
				}
				catch (Exception ex) { (new Logger.Sync()).WriteError("FileIsOk:catch2:<br>", ex); }
				finally
				{
					mre.Set();
				}
			}));

			actionThread.Start();
			mre.WaitOne(7000); // ждем 7 сек. 1 сек идёт обычно в худшем случае, если через сеть
			(new Logger.Sync()).WriteDebug3("\t\t\t\t[FileIsOk] - 2 ");
			try
			{
				if (actionThread.IsAlive)
				{
					actionThread.Abort();
				}
			}
			catch (Exception ex)
			{
				(new Logger.Sync()).WriteError("FileIsOk:catch2:<br>", ex);
			}
			(new Logger.Sync()).WriteDebug3("\t\t\t\t[FileIsOk] - 3 ");
			if (nDuration > 0)
			{
				if (_ahCheckedFiles.ContainsKey(cFile.sFile))
				{
					_ahCheckedFiles[cFile.sFile] = SIO.File.GetLastWriteTime(cFile.sFile);
					_ahCheckedFilesDurs[cFile.sFile] = nDuration;
				}
				else
				{
					_ahCheckedFiles.Add(cFile.sFile, SIO.File.GetLastWriteTime(cFile.sFile));
					_ahCheckedFilesDurs.Add(cFile.sFile, nDuration);
				}
			}
			else
				(new Logger.Sync()).WriteWarning("\t\t\t\t[FileIsOk] - ERROR! [dur=" + nDuration + "]");
			(new Logger.Sync()).WriteDebug("\t\t\t\t[FileIsOk] - out");
			return nDuration;
		}
		static private long FileIsOkInFFMPEG(helpers.replica.media.File cFile)
		{
			(new Logger.Sync()).WriteDebug3("\t\t\t\t\t[FileIsOkInFFMPEG] - in");
			long nDuration;
			ffmpeg.net.File.Input cFileInput = null;
			try
			{
				cFileInput = new ffmpeg.net.File.Input(cFile.sFile);
    //            cFileInput.nCacheSize = 15;
    //            cFileInput.nBlockSize = 10000000;
				//ffmpeg.net.File.Input.nDecodingThreads = 3;
				nDuration = (long)cFileInput.nFramesQty;

				if (!Preferences.bDeepFileChecking)
					return nDuration;

				var cFormatVideo = new ffmpeg.net.Format.Video(720, 576, ffmpeg.net.PixelFormat.AV_PIX_FMT_BGRA, ffmpeg.net.AVFieldOrder.AV_FIELD_TT); // для проверки не особо важно какие цифры стоят
				var cFormatAudio = new ffmpeg.net.Format.Audio(48000, 2, ffmpeg.net.AVSampleFormat.AV_SAMPLE_FMT_S16);
				cFileInput.Prepare(cFormatVideo, cFormatAudio, ffmpeg.net.File.Input.PlaybackMode.GivesFrameOnDemand);
                if (!cFileInput.bPrepared)
                    throw new Exception("file was not prepared!");

				//ffmpeg.net.Frame cVFrame, cAFrame;
				//(new Logger.Sync()).WriteDebug("\t\t\t\t\t[FileIsOkInFFMPEG] - 0");
				//for (int nI = 0; nI < 100; nI++)
				//{
				//	cVFrame = cFileInput.FrameNextVideoGet();
				//	if (null == cVFrame || cVFrame.nLength < 1)
				//	{
				//		nDuration = -2;
				//		break;
				//	}
				//	cVFrame.Dispose();
				//	if (cFileInput.bFileEnd)
				//		break;
				//	cAFrame = cFileInput.FrameNextAudioGet();
				//	if (null == cAFrame || cAFrame.nLength < 1)
				//	{
				//		nDuration = -3;
				//		break;
				//	}
				//	cAFrame.Dispose();
				//	if (cFileInput.bFileEnd)
				//		break;
				//}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError("PLI is not ok!!! [file=" + cFile.sFile + "]<br>", ex);
				return -1;
			}
			finally
			{
				if (null != cFileInput)
					cFileInput.Dispose();
			}
			(new Logger.Sync()).WriteDebug3("\t\t\t\t\t[FileIsOkInFFMPEG] - out");
			return nDuration;
		}
		static private PlaylistItem PlaylistItemCreate(string sFile, DateTime dtStart)
		{
			string sFolder = System.IO.Path.GetDirectoryName(sFile) + "/";
			string sDefault = "default";
			helpers.replica.media.File cFile = new helpers.replica.media.File(sFile.GetHashCode(), SIO.Path.GetFileName(sFile), new helpers.replica.media.Storage(sFolder.GetHashCode(), sDefault, sFolder, true, sDefault.GetHashCode(), sDefault, null, null), DateTime.MaxValue, helpers.replica.Error.no, helpers.replica.media.File.Status.InStock, 0);

			return PlaylistItemCreate(cFile, Preferences.cDefaultClass, dtStart, long.MinValue, DateTime.MinValue);
		}
		static private PlaylistItem PlaylistItemCreate(helpers.replica.media.File cFile, Class cClass, DateTime dtStart, long nDuration, DateTime dtTimingsUpdate, long nID)
		{
			PlaylistItem cRetVal = new PlaylistItem();
			cRetVal.dtTimingsUpdate = dtTimingsUpdate;
			cRetVal.dtStartPlanned = dtStart;
			cRetVal.nFrameStart = 1;
            long nDurationReal;
            if (nDuration == long.MinValue)
            {
                ffmpeg.net.File.Input cFileInput = new ffmpeg.net.File.Input(cFile.sFile);
                nDurationReal = (long)cFileInput.nFramesQty;
                cFileInput.Dispose();

            }
            else
                nDurationReal = nDuration;
            cRetVal.nFrameStop = (int)(cRetVal.nFrameStart + nDurationReal - 1);
			cRetVal.nFramesQty = (int)nDurationReal;
			if (nID == long.MinValue)
			{
				lock (_cSyncRoot)
				{
					if (1 > _aPlaylistOnline.Count || -2 < (cRetVal.nID = _aPlaylistOnline.Min(o => o.nID) - 1)) //стремная фигня конечно, но на ум ниче не идет более удобоваримое... т.к. логичный Max здесь не прокатит - если плейлист будет обновлен, а в локах окажется заглушка, мы рискуем поиметь два PLI с одинаковым ID
						cRetVal.nID = -2;
				}
			}
			else
				cRetVal.nID = nID;

			cRetVal.cFile = cFile;
            cRetVal.aClasses = cClass == null ? new Class[0] : new Class[1] { cClass };
            cRetVal.cStatus = _cStatusPlanned;
			return cRetVal;
		}
		static private PlaylistItem PlaylistItemCreate(helpers.replica.media.File cFile, Class cClass, DateTime dtStart, long nDuration, DateTime dtTimingsUpdate)
		{
			return PlaylistItemCreate(cFile, cClass, dtStart, nDuration, dtTimingsUpdate, long.MinValue);
		}

		static public bool PlaylistItemsTimingsUpdate()
		{
			lock (playlist.Calculator.cSyncRoot)
			{
				if (PlaylistItemsTimingsUpdate(TimeSpan.MaxValue))
				{
					playlist.Calculator.dtLast = DateTime.Now;
					return true;
				}
				return false;
			}
		}
		static public bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope)
		{
			return PlaylistItemsTimingsUpdate(tsUpdateScope, -1);
		}
		static public bool PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID)
		{
			lock (playlist.Calculator.cSyncRoot)
			{
				//if (-1 == nStartPlitemsID && TimeSpan.MaxValue > tsUpdateScope && TimeSpan.FromSeconds(60) > DateTime.Now.Subtract(playlist.Calculator.dtLast))
				//	return true;
				Synchronize();
				playlist.Calculator.dtLast = DateTime.Now;
				return true;
			}
		}

		static public void PlaylistItemFail(PlaylistItem cPLI)
		{
			try
			{
                lock (_cSyncRoot)
                {
                    cPLI.cStatus = _cStatusFailed;
                    (new Logger("playlist")).WriteNotice("PLI FAILED [pli:" + cPLI.nID + "  " + cPLI.cFile.sFilename + "  sr=" + cPLI.dtStartReal.ToString("HH:mm:ss") + "]");
                }
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemStop(PlaylistItem cPLI)
        {
            try
            {
                DateTime dtNow = DateTime.Now;
                int nDelta;
				lock (_cSyncRoot)
				{
                    if (!_bBesiegedFortressMode)
                    {
                        if (!cPLI.bPlug)
                        {
                            PlaylistItem cPLIMain = null;
                            try
                            {
                                if (cPLI.nID > 0)  // перестраховка - отсекаем мегамиксы файловера
                                    cPLIMain = (new DBInteract()).PlaylistItemGet(cPLI.nID);
                            }
                            catch (Exception ex)
                            {
                                (new Logger("playlist")).WriteError("this PLI [id=" + cPLI.nID + "][sr=" + cPLI.dtStartReal.ToString("HH:mm:ss") + "] was not found in main! see text below", ex);
                            }
                            finally
                            {
                                if (null != cPLIMain)
                                {
                                    if (cPLIMain.dtStart < DateTime.MaxValue && Math.Abs(nDelta = (int)cPLI.dtStart.Subtract(cPLIMain.dtStart).TotalSeconds) > 20)
                                        (new Logger("playlist")).WriteNotice("Delta > 20 seconds! [delta=" + nDelta + " sec][pli:" + cPLI.nID + "  " + cPLI.cFile.sFilename + "  sr=" + cPLI.dtStartReal.ToString("HH:mm:ss") + "][main_sr=" + cPLIMain.dtStart.ToString("HH:mm:ss") + "]");
                                }
                                else
                                    (new Logger("playlist")).WriteNotice("UNIQUE PLI STOPPED (not found in PL) [pli:" + cPLI.nID + "  " + cPLI.cFile.sFilename + "  sr=" + cPLI.dtStartReal.ToString("HH:mm:ss") + "]");
                            }
                        }
                    }
					cPLI.cStatus = _cStatusPlayed;
					cPLI.dtStopReal = dtNow;
                }
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemSkip(PlaylistItem cPLI)
		{
            try
            {
				DateTime dtNow = DateTime.Now;
				lock (_cSyncRoot)
				{
					cPLI.cStatus = _cStatusSkipped;
					cPLI.dtStopReal = dtNow;
				}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemStart(PlaylistItem cPLI, ulong nDelay)
		{
            try
            {
				DateTime dtNow = DateTime.Now;
				lock (_cSyncRoot)
				{
					cPLI.cStatus = _cStatusOnAir;
                    cPLI.dtStartReal = dtNow; //.AddMilliseconds(nDelay * Player.Preferences.nFrameMs);   stopreal всё-равно не учитывает эту прибавку
                    //cPLI.dtStartPlanned = cPLI.dtStartQueued = dtNow;
                }
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemPrepare(PlaylistItem cPLI)
		{
			try
			{
				lock (_cSyncRoot)
					cPLI.cStatus = _cStatusPrepared;
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public void PlaylistItemQueue(PlaylistItem cPLI)
		{
			try
			{
				lock (_cSyncRoot)
				{
					cPLI.cStatus = _cStatusQueued;
					cPLI.dtStartQueued = cPLI.dtStartPlanned;
				}
				//_aPlaylist.First(o => cPLI.nID == o.nID).dtStartQueued = cPLI.dtStartPlanned;
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}
		static public helpers.replica.pl.Proxy ProxyGet(Class cClass)
		{
			return null; // на бекапе нет проксей, т.к. нет каминапа и прочих сложностей.
		}
		static public void PlaylistClean()
		{
			try
			{
				PlaylistItem[] aPLIs;
				DateTime dt = DateTime.Now.Subtract(TimeSpan.FromMinutes(60));
				lock (_cSyncRoot)
                {
                    if (!_bBesiegedFortressMode)
                        if (!(aPLIs = _aPlaylistBig.Where(o => dt > o.dtStopPlanned).ToArray()).IsNullOrEmpty())
                            foreach (PlaylistItem cPLI in aPLIs)
                                _aPlaylistBig.Remove(cPLI);
                    if (!(aPLIs = _aPlaylistOnline.Where(o => dt > o.dtStopPlanned).ToArray()).IsNullOrEmpty())
                        foreach (PlaylistItem cPLI in aPLIs)
                            _aPlaylistOnline.Remove(cPLI);
				}
			}
			catch (Exception ex)
			{
				(new Logger("playlist")).WriteError(ex);
			}
		}

		static public helpers.replica.mam.Cues? ComingUpCuesGet(long nID, ushort nOffset)
		{
			helpers.replica.mam.Cues? cRetVal = null;
			lock (_cSyncRoot)
			{
				if (null != _ahCues)
				{
					PlaylistItem cPLI = _aPlaylistOnline.OrderBy(o => o.dtStartReal).ThenBy(o => o.dtStartQueued)
                                                                                    .ThenBy(o => o.dtStartPlanned)
                                                                                    .SkipWhile(o => nID != o.nID)
                                                                                    .Where(o => null != o.cAsset && _ahCues.ContainsKey(o.cAsset.nID) && !_aStatusesStaled.Contains(o.cStatus))
                                                                                    .Skip(nOffset).FirstOrDefault();
					if (null == cPLI)
					{
						(new Logger("playlist")).WriteDebug2("pli was not found in _aPlaylistOnline [id=" + nID + "][offset=" + nOffset + "]");
						cPLI = _aPlaylistBig.OrderBy(o => o.dtStartReal).ThenBy(o => o.dtStartQueued)
                                                                        .ThenBy(o => o.dtStartPlanned)
                                                                        .SkipWhile(o => nID != o.nID)
                                                                        .Where(o => null != o.cAsset && _ahCues.ContainsKey(o.cAsset.nID) && !_aStatusesStaled.Contains(o.cStatus))
                                                                        .Skip(nOffset).FirstOrDefault();
					}
					if (null != cPLI)
						cRetVal = _ahCues[cPLI.cAsset.nID];
					else
						(new Logger("playlist")).WriteNotice("pli was not found in _aPlaylistBig [id=" + nID + "][offset=" + nOffset + "]");
				}
			}
			return cRetVal;
		}
		static public string ComingUpFileGet(long nID, ushort nOffset)
		{
			string sRetVal = null;
			lock (_cSyncRoot)
			{
				if (null != _ahCues)
				{
					PlaylistItem cPLI = _aPlaylistOnline.OrderBy(o => o.dtStartReal).ThenBy(o => o.dtStartQueued)
                                                                                    .ThenBy(o => o.dtStartPlanned)
                                                                                    .SkipWhile(o => nID != o.nID)
                                                                                    .Where(o => null != o.cAsset && _ahCues.ContainsKey(o.cAsset.nID) && !_aStatusesStaled.Contains(o.cStatus))
                                                                                    .Skip(nOffset).FirstOrDefault();
					if (null == cPLI)
						cPLI = _aPlaylistBig.OrderBy(o => o.dtStartReal).ThenBy(o => o.dtStartQueued)
                                                                        .ThenBy(o => o.dtStartPlanned)
                                                                        .SkipWhile(o => nID != o.nID)
                                                                        .Where(o => null != o.cAsset && _ahCues.ContainsKey(o.cAsset.nID) && !_aStatusesStaled.Contains(o.cStatus))
                                                                        .Skip(nOffset).FirstOrDefault();
					if (null != cPLI)
						sRetVal = cPLI.cFile.sFile;
				}
			}
			return sRetVal;
		}
		static public Queue<PlaylistItem> PlaylistItemsPreparedGet()
		{
			lock (_cSyncRoot)
			{
				IOrderedEnumerable<PlaylistItem> aPLPrepared = _aPlaylistOnline.Where(o => _cStatusPrepared == o.cStatus).OrderBy(o => o.dtStart);
				if (aPLPrepared.Count() <= 0)
					aPLPrepared = _aPlaylistBig.Where(o => _cStatusPrepared == o.cStatus).OrderBy(o => o.dtStart);
				return new Queue<PlaylistItem>(aPLPrepared);
			}
		}
		static public helpers.replica.cues.TemplateBind[] TemplateBindsGet(PlaylistItem cPLI)
        {
            helpers.replica.cues.TemplateBind[] aRetVal;
            lock (_cSyncRoot)
            {
                if (!cPLI.aClasses.IsNullOrEmpty() && _ahPLIIDs_Binds.Keys.Contains(cPLI.nID))
                {
                    aRetVal = _ahPLIIDs_Binds[cPLI.nID];
                }
                else
                    aRetVal = Preferences.aDefaultTemplateBinds;
            }
            //ChangeTemplatesDriveLetter(aRetVal);
            return aRetVal;
        }
        static public helpers.replica.mam.Macro MacroGet(string sMacroName)
		{
			return new helpers.replica.mam.Macro(sMacroName.GetHashCode(), new IdNamePair(0, "sql"), sMacroName, "{%RUNTIME::PLI::ID%}");
		}
		static public string MacroExecute(helpers.replica.mam.Macro cMacro)
		{
			string sRetVal = "";
			helpers.replica.mam.Cues? cCues = null;
			string sFile = null;
			switch (cMacro.sName)
            {   //{%MACRO::REPLICA::PLI::CUES::ARTIST::CAPS%}      //{%MACRO::REPLICA::PROGRAM::CUES::ARTIST::CAPS%}
                case "{%MACRO::REPLICA::CU(0)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 0)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::PLI::CUES::ARTIST%}":
                case "{%MACRO::REPLICA::PROGRAM::CUES::ARTIST%}":
                case "{%MACRO::REPLICA::CU(0)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 0)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					else
						(new Logger("template")).WriteNotice("MacroExecute. cCues = " + cCues == null ? "NULL" : "NO VALUE [" + cMacro.sName + "]");
					break;
				case "{%MACRO::REPLICA::PLI::CUES::SONG%}":
                case "{%MACRO::REPLICA::PROGRAM::CUES::SONG%}":
                case "{%MACRO::REPLICA::CU(0)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 0)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+1)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 1)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+1)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 1)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+1)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 1)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+2)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 2)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+2)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 2)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+2)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 2)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+3)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 3)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+3)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 3)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+3)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 3)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+4)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 4)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+4)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 4)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+4)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 4)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;

				case "{%MACRO::REPLICA::CU(+5)::MEDIA::FILE%}":
					if (null != (sFile = ComingUpFileGet(cMacro.sValue.ToID(), 5)))
						sRetVal = sFile;
					break;
				case "{%MACRO::REPLICA::CU(+5)::CUES::ARTIST%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 5)) && cCues.HasValue)
						sRetVal = cCues.Value.sArtist;
					break;
				case "{%MACRO::REPLICA::CU(+5)::CUES::SONG%}":
					if (null != (cCues = ComingUpCuesGet(cMacro.sValue.ToID(), 5)) && cCues.HasValue)
						sRetVal = cCues.Value.sSong;
					break;
				default:
					(new Logger("template")).WriteNotice("указан неизвестный макрос [" + cMacro.sName + "]");
					break;
			}
			(new Logger("template")).WriteDebug2("MacroExecute. sRetVal = " + sRetVal + " [" + cMacro.sName + "]");
			return sRetVal;
		}

		#region реализация Playlist.IInteract
		Playlist.IInteract Playlist.IInteract.Init()
		{
			return new Failover();
		}

		void Playlist.IInteract.PlaylistReset()
		{
			Synchronize();
		}

		PlaylistItem Playlist.IInteract.PlaylistItemCurrentGet() { return PlaylistItemCurrentGet(); }
		PlaylistItem Playlist.IInteract.PlaylistItemLockedPreviousGet() { return PlaylistItemLockedPreviousGet(); }
		PlaylistItem[] Playlist.IInteract.PlaylistItemsPlannedGet() { return PlaylistItemsPlannedGet(); }
		ulong Playlist.IInteract.PlaylistItemOnAirFramesLeftGet(int nOneFrameInMs) { return PlaylistItemOnAirFramesLeftGet(nOneFrameInMs); }

		bool Playlist.IInteract.PlaylistItemsTimingsUpdate() { return PlaylistItemsTimingsUpdate(); }
		bool Playlist.IInteract.PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope) { return PlaylistItemsTimingsUpdate(tsUpdateScope); }
		bool Playlist.IInteract.PlaylistItemsTimingsUpdate(TimeSpan tsUpdateScope, int nStartPlitemsID) { return PlaylistItemsTimingsUpdate(tsUpdateScope, nStartPlitemsID); }

		void Playlist.IInteract.PlaylistItemFail(PlaylistItem cPLI) { PlaylistItemFail(cPLI); }
		#endregion реализация Playlist.IInteract
		#region реализация Player.IInteract
		Player.IInteract Player.IInteract.Init()
		{
			return new Failover();
		}
		helpers.replica.pl.Proxy Player.IInteract.ProxyGet(Class cClass) { return ProxyGet(cClass); }
		void Player.IInteract.PlaylistItemQueue(PlaylistItem cPLI) { PlaylistItemQueue(cPLI); }
		void Player.IInteract.PlaylistItemPrepare(PlaylistItem cPLI) { PlaylistItemPrepare(cPLI); }
		void Player.IInteract.PlaylistItemStart(PlaylistItem cPLI, ulong nDelay) { PlaylistItemStart(cPLI, nDelay); }
		void Player.IInteract.PlaylistItemStop(PlaylistItem cPLI) { PlaylistItemStop(cPLI); }
		void Player.IInteract.PlaylistItemFail(PlaylistItem cPLI) { PlaylistItemFail(cPLI); }
		void Player.IInteract.PlaylistItemSkip(PlaylistItem cPLI) { PlaylistItemSkip(cPLI); }
		helpers.replica.mam.Macro Player.IInteract.MacroGet(string sMacroName) { return MacroGet(sMacroName); }
		string Player.IInteract.MacroExecute(helpers.replica.mam.Macro cMacro) { return MacroExecute(cMacro); }
		PlaylistItem[] replica.Player.IInteract.PlaylistClipsGet()
		{
			return new PlaylistItem[0];
		}
		#endregion реализация Player.IInteract
		#region реализация Cues.IInteract
		Cues.IInteract Cues.IInteract.Init()
		{
			return new Failover();
		}

		PlaylistItem Cues.IInteract.PlaylistItemOnAirGet() { return PlaylistItemOnAirGet(); }
		Queue<PlaylistItem> Cues.IInteract.PlaylistItemsPreparedGet() { return PlaylistItemsPreparedGet(); }
		helpers.replica.cues.TemplateBind[] Cues.IInteract.TemplateBindsGet(PlaylistItem cPLI) { return TemplateBindsGet(cPLI); }
		#endregion реализация Cues.IInteract
		#region реализация Template.IInteract
		replica.cues.Template.IInteract replica.cues.Template.IInteract.Init()
		{
			return new Failover();
		}
		helpers.replica.mam.Macro replica.cues.Template.IInteract.MacroGet(string sMacroName) { return MacroGet(sMacroName); }
		string replica.cues.Template.IInteract.MacroExecute(helpers.replica.mam.Macro cMacro) { return MacroExecute(cMacro); }
		helpers.replica.cues.TemplatesSchedule[] replica.cues.Template.IInteract.TemplatesScheduleGet() { return new helpers.replica.cues.TemplatesSchedule[0]; }
		void replica.cues.Template.IInteract.TemplatesScheduleSave(helpers.replica.cues.TemplatesSchedule cTemplatesSchedule) { }
		void replica.cues.Template.IInteract.TemplateStarted(replica.cues.Template.Range cRange) { }
		PlaylistItem replica.cues.Template.IInteract.PlaylistItemPreviousGet(PlaylistItem cPLI) { return new PlaylistItem(); }
		#endregion реализация Template.IInteract
	}
}
