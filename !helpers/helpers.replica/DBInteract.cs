using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using helpers;
using helpers.extensions;
using helpers.replica.hk;
using helpers.replica.media;
using helpers.replica.mam;
using helpers.replica.pl;
using helpers.replica.ia;
using helpers.replica.cues;
using helpers.replica.scr;

using g = globalization;

namespace helpers.replica
{
	public abstract class DBInteract
	{
		public class DB : helpers.DB
		{
			public access.types.AccessScope[] aAccessScopes;

			public override void CredentialsSet(Credentials cDBCredentials)
			{
				base.CredentialsSet(cDBCredentials);
				access.scopes.init(aAccessScopes = Select("SELECT * FROM hk.`fUserAccessScopeGet`()").Select(o => new access.types.AccessScope(o)).ToArray());
			}
		}
        public enum SearchMask
		{
			equal,
			ends,
			starts,
			contains
		}
		static private DBInteract _cCache;
		static internal DBInteract cCache
		{
			get
			{
				if(null == _cCache)
					return (DBInteract)AppDomain.CurrentDomain.GetData(System.Threading.Thread.CurrentThread.ManagedThreadId + ":" + typeof(DBInteract).ToString());
				return _cCache;
			}
		}
		protected DB _cDB;
		protected int _nOpenBumperID;

		public DBInteract()
		{
			AppDomain.CurrentDomain.SetData(System.Threading.Thread.CurrentThread.ManagedThreadId + ":" + typeof(DBInteract).ToString(), this);
		}

		public void Load()
		{
			_cDB = new DB();
			_cDB.CredentialsLoad();
			_cCache = this;
		}

		public bool bTransaction
		{
			get
			{
				return _cDB.bTransaction;
			}
		}

		public void TransactionBegin()
		{
			_cDB.TransactionBegin();
		}
		public void TransactionCommit()
		{
			_cDB.TransactionCommit();
		}
		public void TransactionRollBack()
		{
			_cDB.TransactionRollBack();
		}

		public access.types.AccessScope[] AccessScopesGet()
		{
			return _cDB.aAccessScopes;
		}
		internal string sUserName
        {
			get
			{
				return _cDB.sUserName;
			}
        }
        internal Hashtable RowGet(string sSQL)
        {
            return _cDB.GetRow(sSQL);
        }
        internal Queue<Hashtable> RowsGet(string sSQL)
        {
            return _cDB.Select(sSQL);
        }
        internal string ValueGet(string sSQL)
        {
            return _cDB.GetValue(sSQL);
        }
		internal void Perform(string sSQL)
		{
			_cDB.Perform(sSQL);
		}

		#region hk
		public RegisteredTable RegisteredTableGet(string sSchema, string sName)
		{
			RegisteredTable cRetVal = null;
			try
			{
				cRetVal = RegisteredTablesGet("`sSchema`='" + sSchema + "' AND `sName`='" + sName + "'").Dequeue();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}
		public RegisteredTable RegisteredTableGet(long nID)
		{
			RegisteredTable cRetVal = null;
			try
			{
				cRetVal = RegisteredTablesGet("`id`=" + nID).Dequeue();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}
		private Queue<RegisteredTable> RegisteredTablesGet(string sWhere)
		{
			Queue<RegisteredTable> aqRetVal = new Queue<RegisteredTable>();
			if (null == sWhere)
				sWhere = "";
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT * FROM hk.`tRegisteredTables`" + (0 < sWhere.Length ? " WHERE " + sWhere : "") + " ORDER BY `sName`")))
					while (0 < aqDBValues.Count)
						aqRetVal.Enqueue(new RegisteredTable(aqDBValues.Dequeue()));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aqRetVal;
		}
		#endregion

		#region pl
		public int PlaylistFramesQtyGet()
		{
			//_cDB.RoleSet("replica_playlist");
			int nRetVal = -1;
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT `nDuration` FROM pl.`vPlaylistFramesQty`");
			if (null != aqDBValues && 0 < aqDBValues.Count)
			{
				Hashtable hRow = aqDBValues.Dequeue();
				if (null != hRow && null != hRow["nDuration"])
					nRetVal = hRow["nDuration"].ToInt32();
			}
			return nRetVal;
		}
		public PlaylistItem PlaylistItemGet(long nID)
		{
			return PlaylistItemsGet("SELECT DISTINCT * FROM pl.`vPlayListResolved` WHERE id=" + nID).Dequeue();
		}
		public PlaylistItem PlaylistItemPreviousGet(PlaylistItem cPLI)
		{						   //select * from pl."vPlayListResolvedOrdered" where "dtStartPlanned" < (select "dtStartPlanned" from pl."vPlayListResolvedOrdered" where id=1703412) order by "dtStartPlanned" desc limit 1;
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE id = (SELECT `nValue` FROM pl.`fItemPreviousGet`(" + cPLI.nID + "))").Dequeue();
		}
		public PlaylistItem PlaylistItemOnAirGet()
		{
			PlaylistItem cRetVal = null;
			Queue<PlaylistItem> aqPLIs = PlaylistItemsGet(new IdNamePair("onair"));
			if (null != aqPLIs)
			{
				if (1 < aqPLIs.Count)
					(new Logger()).WriteWarning("dbi", "pli:onair:get: got more than one onair PLIs");
				while (0 < aqPLIs.Count)
					cRetVal = aqPLIs.Dequeue();
			}
			return cRetVal;
		}
		public Queue<PlaylistItem> PlaylistItemsPreparedGet()
		{
			return PlaylistItemsGet(new IdNamePair("prepared"));
		}
		public Queue<PlaylistItem> PlaylistItemsPreparedAndQueuedGet()
		{
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolved` WHERE `sStatusName` IN ('prepared', 'queued') ORDER BY `dtStart`;");
		}
		public Queue<PlaylistItem> PlaylistItemsQueuedGet()
		{
			return PlaylistItemsGet(new IdNamePair("queued"));
		}
		public void PlaylistItemAdd(Queue aqAssetsIDs)
		{
			//_cDB.RoleSet("replica_playlist_full");
			while (null != aqAssetsIDs && 0 < aqAssetsIDs.Count)
			{
				string sSQL = "";
				int nIndex = 0;
				while (0 < aqAssetsIDs.Count && 20 > nIndex)   // попытка бить на сотни, т.к. заливка целого дня (>500) частенько дает таймауты (4 минуты) ((... выяснил - это потому что 1 добавляется 3.5 - 4.5 секунды!!!!
				{                                              // приходится по 20-ке...
					nIndex++;
					sSQL += "SELECT `nValue` as id, `bValue` as `bStatus` FROM pl.`fItemAddAsAsset`(" + aqAssetsIDs.Dequeue() + ");";
				}
				if (0 < sSQL.Length)
					_cDB.Perform(sSQL);
			}
		}
		public void PlaylistItemsAdd(PlaylistItem[] aItems)
		{
			if (null == aItems)
				return;
			//_cDB.RoleSet("replica_playlist_full");
			string sSQL = "", sStart = "";
			PlaylistItem cPLI = null;
			bool bFrom;
			try
			{
				_cDB.TransactionBegin();
				for (int nIndx = 0; aItems.Length > nIndx; nIndx++)
				{
					cPLI = aItems[nIndx];

					sStart = "";
					bFrom = true;

					sSQL = "SELECT ";
					if (DateTime.MinValue < cPLI.dtStartHard && DateTime.MaxValue > cPLI.dtStartHard)
					{
						sStart = ", '" + cPLI.dtStartHard.ToString("yyyy-MM-dd HH:mm:ss") + "'";
						sSQL += "pl.`fItemStartHardSet`(`nValue`" + sStart + ")";
					}
					else if (DateTime.MinValue < cPLI.dtStartSoft && DateTime.MaxValue > cPLI.dtStartSoft)
					{
						sStart = ", '" + cPLI.dtStartSoft.ToString("yyyy-MM-dd HH:mm:ss") + "'";
						sSQL += "pl.`fItemStartSoftSet`(`nValue`" + sStart + ")";
					}
					else
					{
						bFrom = false;
					}
					if (DateTime.MinValue < cPLI.dtStartPlanned && DateTime.MaxValue > cPLI.dtStartPlanned)
						sStart = ", '" + cPLI.dtStartPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "'";

					//if (null != cPLI.cBeepInfo) //DEPRECATED
					//{
					//    if (bFrom)
					//        sSQL += ", ";
					//    bFrom = true;
					//    if (PlaylistItem.BeepInfo.Type.Advertisement == cPLI.cBeepInfo.eType)
					//        sSQL += "pl.`fItemAttributeSet`(`nValue`,'nBeepAdvBlockID'," + cPLI.cBeepInfo.nBlockID + ")";
					//    else
					//        sSQL += "pl.`fItemAttributeSet`(`nValue`,'nBeepClipBlockID'," + cPLI.cBeepInfo.nBlockID + ")";
					//}
					if (bFrom)
						sSQL += " FROM ";

					if (null == cPLI.cAsset)
						sSQL += "pl.`fItemAddAsFile`(" + cPLI.cFile.nID + ", " + cPLI.nFramesQty + ",'" + cPLI.cClass.sName + "'" + sStart + ");";
					else
						sSQL += "pl.`fItemAddAsAsset`(" + cPLI.cAsset.nID + "," + cPLI.cAsset.cClass.nID + sStart + ");";
					_cDB.Cache(sSQL);
				}
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				_cDB.TransactionRollBack();
				Queue<Hashtable> aqDBValues = null;

				if (null == cPLI.cAsset)
				{
					if (null == (aqDBValues = _cDB.Select("select id from media.`tFiles` where id=" + cPLI.cAsset.nID)) || aqDBValues.Count == 0)
						throw new Exception("Playlist Item Error: [id=" + cPLI.nID + "] File was not found: [id=" + cPLI.cFile.nID + "][name=" + cPLI.cFile.sFilename + "]");
				}
				else
				{
					if (null == (aqDBValues = _cDB.Select("select id from mam.`tAssets` where id=" + cPLI.cAsset.nID)) || aqDBValues.Count == 0)
						throw new Exception("Playlist Item Error: [id=" + cPLI.nID + "] Asset was not found: [id=" + cPLI.cAsset.nID + "][name=" + cPLI.sName + "]");
				}
				throw new Exception("Unknown playlist Item Error: [id=" + cPLI.nID + "][name=" + cPLI.sName + "][start_planned=" + cPLI.dtStartPlanned + "]", ex);
			}
		}
		public void PlaylistItemStartsSet(long nItemID, DateTime dtPlanned, DateTime dtHard, DateTime dtSoft)
		{
			_cDB.Perform("SELECT pl.`fItemStartsSet`(" + nItemID + ", " + (DateTime.MaxValue == dtPlanned ? "NULL" : "'" + dtPlanned.ToString("yyyy-MM-dd HH:mm:ss") + "'") + ", " + (DateTime.MaxValue == dtHard ? "NULL" : "'" + dtHard.ToString("yyyy-MM-dd HH:mm:ss") + "'") + ", " + (DateTime.MaxValue == dtSoft ? "NULL" : "'" + dtSoft.ToString("yyyy-MM-dd HH:mm:ss") + "'") + ")");
		}
		public bool PlaylistItemDelete(long nItemID)
		{
			return _cDB.GetValueBool("SELECT `bValue` FROM pl.`fItemRemove`(" + nItemID + ")");
		}
		protected Queue<PlaylistItem> PlaylistItemsGet(string sSQLQuery)
		{
			Queue<PlaylistItem> aqRetVal = new Queue<PlaylistItem>();
			Queue<Hashtable> aqDBValues = null;
			Hashtable ahRow;
			if (null != (aqDBValues = _cDB.Select(sSQLQuery)))
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					PlaylistItem cPLI = new PlaylistItem(ahRow);
					aqRetVal.Enqueue(cPLI);
					if (ahRow.ContainsKey("nRotation") && ahRow.ContainsKey("sRotation") && null != ahRow["nRotation"] && null != cPLI.cAsset)
						cPLI.cAsset = new Clip() { nID = cPLI.cAsset.nID, cRotation = new IdNamePair() { nID = ahRow["nRotation"].ToID(), sName = ahRow["sRotation"].ToString() } };
				}
			return aqRetVal;
		}
		public Queue<PlaylistItem> PlaylistItemsGet(IdNamePair cStatus)
		{
			if (null == cStatus)
				throw new Exception(g.Helper.sErrorDBInteract1);
			string sWhere = "";
			if (!cStatus.sName.IsNullOrEmpty())
				sWhere = "'" + cStatus.sName.ForDB() + "' = `sStatusName`";
			else if (-1 < cStatus.nID)
				sWhere = cStatus.nID + " = `idStatuses`";
			else
				throw new Exception(g.Helper.sErrorDBInteract2 + " [" + cStatus.nID + "][" + cStatus.sName + "]");
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE " + sWhere);
		}
		public Queue<PlaylistItem> PlaylistItemsGet(Asset cAsset)
		{
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `idAssets`=" + cAsset.nID);
		}
		public Queue<PlaylistItem> PlaylistItemsFastGet(DateTime dtBegin, DateTime dtEnd)
		{
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `dtStartPlanned` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' and `dtStartPlanned` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'");
		}
		public Queue<PlaylistItem> PlaylistItemsPlanGet(DateTime dtBegin, DateTime dtEnd)
		{
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `dtStartPlanned` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' and `dtStartPlanned` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' and `sStatusName` = 'planned'");
		}                          //select          * from pl."vPlayListResolved" where "dtStartPlanned" > '2012-02-07 07:07:52+04'                          and "dtStartPlanned" < '2012-02-07 07:07:52+04'                        and "sStatusName" = 'planned' order by "dtStartReal","dtStartQueued","dtStartPlanned"
		public Queue<PlaylistItem> PlaylistItemsWithRotationsPlanGet(DateTime dtBegin, DateTime dtEnd)
		{
			return PlaylistItemsGet("SELECT DISTINCT *, ass.`nValue` AS `nRotation`, cat.`sValue` AS `sRotation` FROM (SELECT DISTINCT * FROM pl.`vPlayListResolved` WHERE `dtStartPlanned` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStartPlanned` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `sStatusName` = 'planned') pl LEFT JOIN (SELECT `idAssets` as `idClips`, `nValue` FROM mam.`tAssetAttributes` WHERE mam.`tAssetAttributes`.`sKey`='rotation') ass ON pl.`idAssets`= ass.`idClips` LEFT JOIN (SELECT id AS `nCatValID`, `sValue` FROM mam.`tCategoryValues`) cat ON cat.`nCatValID`=ass.`nValue` ORDER BY `dtStart`");
		}                          //SELECT DISTINCT *, ass."nValue" AS "nRotation", cat."sValue" AS "sRotation" FROM (SELECT DISTINCT * FROM pl."vPlayListResolved" WHERE "dtStartPlanned" > '2012-02-27 17:15:09+04'                          AND "dtStartPlanned" < '2012-02-28 00:05:09+04'                        AND "sStatusName" = 'planned') pl LEFT JOIN (SELECT "idAssets" as "idClips", "nValue" FROM mam."tAssetAttributes" WHERE mam."tAssetAttributes"."sKey"='rotation') ass ON pl."idAssets"= ass."idClips" LEFT JOIN (SELECT id AS "nCatValID", "sValue" FROM mam."tCategoryValues") cat ON cat."nCatValID"=ass."nValue" ORDER BY "dtStartReal","dtStartQueued","dtStartPlanned"
		public Queue<PlaylistItem> PlaylistItemsWithRotationsArchGet(DateTime dtBegin, DateTime dtEnd)
		{
			return PlaylistItemsGet("SELECT DISTINCT *, ass.`nValue` AS `nRotation`, cat.`sValue` AS `sRotation` FROM (SELECT DISTINCT * FROM archive.`vPlayListResolvedFull` WHERE `dtStop` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStop` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') pl LEFT JOIN (SELECT `idAssets` as `idClips`, `nValue` FROM mam.`tAssetAttributes` WHERE mam.`tAssetAttributes`.`sKey`='rotation') ass ON pl.`idAssets`= ass.`idClips` LEFT JOIN (SELECT id AS `nCatValID`, `sValue` FROM mam.`tCategoryValues`) cat ON cat.`nCatValID`=ass.`nValue` ORDER BY `dtStart`");
		}
		public Queue<PlaylistItem> PlaylistItemsAdvertsGet(DateTime dtBegin, DateTime dtEnd)
		{
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolved` WHERE `dtStartPlanned` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' and `dtStartPlanned` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' AND (`dtStartHard` IS NOT NULL OR `dtStartSoft` IS NOT NULL) AND NOT `bPlug` ORDER BY COALESCE(`dtStartHard`,`dtStartSoft`)");
		}

		public Queue<PlaylistItem> PlaylistItemsGet(IdNamePair[] aStatuses)
		{
            if (null == aStatuses || 1 > aStatuses.Length)
                throw new Exception(g.Common.sWrongItem + ": aStatuses");
			string sStatuses = "", sComma = "";
			foreach (IdNamePair cINP in aStatuses)
			{
				sStatuses += sComma + cINP.nID;
				sComma = ",";
			}
			return PlaylistItemsGet("SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `idStatuses` IN (" + sStatuses + ")");
		}
		public Queue<PlaylistItem> PlaylistItemsWithRotationsGet(IdNamePair[] aStatuses)
		{
			if (null == aStatuses || 1 > aStatuses.Length)
				throw new Exception();
			string sStatuses = "", sComma = "";
			foreach (IdNamePair cINP in aStatuses)
			{
				sStatuses += sComma + cINP.nID;
				sComma = ",";
			}
			return PlaylistItemsGet("SELECT DISTINCT *, ass.`nValue` AS `nRotation`, cat.`sValue` AS `sRotation` FROM (SELECT * FROM pl.`vPlayListResolvedOrdered` WHERE `idStatuses` IN (" + sStatuses + ")) pl LEFT JOIN (SELECT `idAssets`, `nValue` FROM mam.`tAssetAttributes` WHERE mam.`tAssetAttributes`.`sKey`='rotation') ass ON pl.`idAssets`= ass.`idAssets` LEFT JOIN (SELECT id AS `nCatValID`, `sValue` FROM mam.`tCategoryValues`) cat ON cat.`nCatValID`=ass.`nValue` ORDER BY `dtStart`");
		}
		public DateTime PlaylistItemsLastUsageGet()
		{
			//_cDB.RoleSet("replica_playlist");
			return _cDB.GetValueRaw("SELECT max(`dtStopPlanned`) FROM pl.`vPlayListResolved`").ToDT();
		}

		private Queue<PlaylistItem> ComingUpGet(bool bAssetsResolved, uint nOffset, uint nLimit)
		{
			Queue<PlaylistItem> aqRetVal = new Queue<PlaylistItem>();
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM pl.`vComingUp`", null, "`dtStart`", nOffset, nLimit);
			PlaylistItem cPLI = null;
			while (null != aqDBValues && 0 < aqDBValues.Count)
			{
				cPLI = new PlaylistItem(aqDBValues.Dequeue());
				if (bAssetsResolved && null != cPLI.cAsset)
					cPLI.cAsset = Asset.Load(cPLI.cAsset.nID);
				aqRetVal.Enqueue(cPLI);
			}
			return aqRetVal;
		}
		private Queue<PlaylistItem> ComingUpGet(bool bAssetsResolved, TimeSpan tsDuration)
		{
			Queue<PlaylistItem> aqRetVal = new Queue<PlaylistItem>();
			Queue<PlaylistItem> aqPLIs = null;
			if (TimeSpan.MaxValue > tsDuration)
			{
				TimeSpan tsCurrent = TimeSpan.FromTicks(0);
				PlaylistItem cPLI = null;
				uint nOffset = 0;
				uint nLimit;
				DateTime dtStartPrevious = DateTime.MaxValue;
				while (tsCurrent < tsDuration)
				{
					nLimit = (uint)(tsDuration - tsCurrent).TotalSeconds;
					if (25 > nLimit)
						nLimit = 25;
					if (null != (aqPLIs = ComingUpGet(bAssetsResolved, nOffset, nLimit)) && 0 < aqPLIs.Count)
					{
						while (0 < aqPLIs.Count && tsCurrent < tsDuration)
						{
							cPLI = aqPLIs.Dequeue();
							if (DateTime.MaxValue > dtStartPrevious)
								tsCurrent += cPLI.dtStart.Subtract(dtStartPrevious);
							dtStartPrevious = cPLI.dtStart; //теоретически мы можем словить дедлуп во внешнем цикле
							aqRetVal.Enqueue(cPLI);
						}
						nOffset += nLimit;
					}
					else
						break;
				}
			}
			else
				aqRetVal = ComingUpGet(bAssetsResolved, 0, 0);
			return aqRetVal;
		}
		public Queue<PlaylistItem> ComingUpWithAssetsResolvedGet(uint nOffset, uint nLimit)
		{
			return ComingUpGet(true, nOffset, nLimit);
		}
		public Queue<PlaylistItem> ComingUpWithAssetsResolvedGet(TimeSpan tsDuration)
		{
			return ComingUpGet(true, tsDuration);
		}
		public Queue<PlaylistItem> ComingUpGet(uint nOffset, uint nLimit)
		{
			return ComingUpGet(false, nOffset, nLimit);
		}
		public Queue<PlaylistItem> ComingUpGet(TimeSpan tsDuration)
		{
			return ComingUpGet(false, tsDuration);
		}

		public File[] PlaylistPlugsGet()
		{
			//_cDB.RoleSet("replica_playlist");
			File[] aRetVal = null;
			Queue<Hashtable> aqDBValues = null;
			if (null != (aqDBValues = _cDB.Select("SELECT f.* FROM media.`vFiles` f, pl.`tPlugs` p WHERE p.`idFiles`=f.id")))
			{
				aRetVal = new File[aqDBValues.Count];
				int nIndx = 0;
				while (0 < aqDBValues.Count)
					aRetVal[nIndx++] = new File(aqDBValues.Dequeue());
			}
			return aRetVal;
		}
		//public List<int> PlaylistBeepBlocksIDsGet(bool bAdv)
		//{
		//    //_cDB.RoleSet("replica_playlist");
		//    List<int> aRetVal = new List<int>();
		//    Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT `nValue` as `nBlockID` FROM pl.`tItemAttributes` WHERE '" + (bAdv ? "nBeepAdvBlockID" : "nBeepClipBlockID") + "'=`sKey`");
		//    if (null == aqDBValues)
		//        return aRetVal;
		//    while (0 < aqDBValues.Count)
		//        aRetVal.Add(aqDBValues.Dequeue()["nBlockID"].ToID());
		//    return aRetVal;
		//}
		public List<long> PlaylistItemIDsCachedGet()
		{
			//_cDB.RoleSet("replica_playlist");
			List<long> aRetVal = new List<long>();
			//SELECT "idItems" FROM pl."tItemsCached"
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT `idItems` FROM pl.`tItemsCached`");
			if (null == aqDBValues)
				return aRetVal;
			while (0 < aqDBValues.Count)
				aRetVal.Add(aqDBValues.Dequeue()["idItems"].ToID());
			return aRetVal;
		}
        public PlaylistItem PlaylistInsert(Asset cAsset, PlaylistItem cPLIPreceding)
        {
			long nID = _cDB.GetID("SELECT `nValue` FROM " + (null == cPLIPreceding ? "pl.`ItemAddAsAsset`(" + cAsset.nID + ")" : "pl.`fItemInsert`(" + cAsset.nID + ", " + cPLIPreceding.nID + ")") + " pli WHERE pli.`bValue`");
			return new PlaylistItem(_cDB.GetRow("SELECT * FROM pl.`vPlayListResolved` WHERE id = " + nID));
        }
        public IdNamePair[] PlaylistItemsStatusesGet()
		{
			List<IdNamePair> aRetVal = new List<IdNamePair>();
			Queue<Hashtable> aqDBValues = null;
			if (null != (aqDBValues = _cDB.Select("SELECT * FROM pl.`tStatuses` ORDER BY id")))
				while (0 < aqDBValues.Count)
					aRetVal.Add(new IdNamePair(aqDBValues.Dequeue()));
			return aRetVal.ToArray();
		}
		#endregion

		#region media
		public File FileAdd(long nStorageID, string sFilename)
		{
			return FileGet(_cDB.GetValue("SELECT `nValue` FROM media.`fFileAdd`(" + nStorageID + ", '" + sFilename.Replace("'", "\\'") + "');").ToID());
		}
		public File FileAdd(Storage cStorage, string sFilename)
		{
            return FileGet(_cDB.GetValue("SELECT `nValue` FROM media.`fFileAdd`(" + cStorage.nID + ", '" + sFilename.Replace("'", "\\'") + "');").ToID());
		}
        //public File FileMove(Storage cStorage, File cFile)
        //{
        //    File stRetVal = new File();
        //    //return _cDB.GetValue("SELECT `nValue` FROM media.`fFileAdd`(" + nStorageID + ", '" + sFilename.Replace("'", "\\'") + "');").ToID();
        //    return stRetVal;
        //}
        //public File FileMove(Storage cStorage, string sFile)
        //{
        //    File stRetVal = new File();
        //    //return _cDB.GetValue("SELECT `nValue` FROM media.`fFileAdd`(" + nStorageID + ", '" + sFilename.Replace("'", "\\'") + "');").ToID();
        //    return stRetVal;
        //}
        //public File FileGet(string sPath, string sFilename)
        //{
        //    File stRetVal = new File();
        //    //return _cDB.GetValue("SELECT `nValue` FROM media.`fFileAdd`(" + nStorageID + ", '" + sFilename.Replace("'", "\\'") + "');").ToID();
        //    return stRetVal;
        //}
		public File FileGet(string sFilename)
		{
			foreach (File cFile in FilesGet("`sFilename`='" + sFilename + "'").Values)
				return cFile;
			return null;
		}
        public File FileGet(Storage cStorage, string sFilename)
		{
            foreach (File cFile in FilesGet("`idStorages`=" + cStorage.nID + " AND `sFilename`='" + sFilename + "'").Values)
				return cFile;
			return null;
		}
		public File FileGet(long nStorageID, string sFilename)
		{
			foreach (File cFile in FilesGet("`idStorages`=" + nStorageID + " AND `sFilename`='" + sFilename + "'").Values)
				return cFile;
			return null;
		}
		public File FileGet(long nFileID)
		{
			foreach (File cFile in FilesGet("`id`=" + nFileID).Values)
				return cFile;
			return null;
		}
		private Dictionary<long, File> FilesGet(string sWhere)
		{
			Dictionary<long, File> aRetVal = new Dictionary<long, File>();
			if (null == sWhere)
				sWhere = "";
			//_cDB.RoleSet("replica_assets");
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT id, `idStorages`, `idStorageTypes`, `sStorageTypeName`, `sPath`, `sStorageName`, `bStorageEnabled`, `sFilename`, `dtLastFileEvent`, `eError` FROM media.`vFiles`" + (0 < sWhere.Length ? " WHERE " + sWhere : "") + " ORDER BY `sStorageName`, `sFilename`");
			File cFile;
			while (null != aqDBValues && 0 < aqDBValues.Count)
			{
				cFile = new File(aqDBValues.Dequeue());
				aRetVal.Add(cFile.nID, cFile);
			}
			return aRetVal;
		}
		public Dictionary<long, File> FilesGet(long nStorageID)
		{
			return FilesGet(nStorageID > -1 ? " `idStorages`=" + nStorageID : "");
		}
		public Dictionary<long, File> FilesGet()
		{
			return FilesGet(null);
		}
		public void FileRemove(File cFile)
		{
			_cDB.Perform("SELECT * FROM media.`fFileRemove`(" + cFile.nID + ")");
		}
		public void FileErrorSet(File cFile)
		{
			_cDB.Perform("SELECT media.`fFileErrorSet`(" + cFile.nID + ", '" + cFile.eError + "')");
		}
		public void FileErrorRemove(File cFile)
		{
			_cDB.Perform("SELECT media.`fFileErrorRemove`(" + cFile.nID + ")");
		}

		public Storage StorageGet(string sName)
		{
			return StoragesGet("`sName`='" + sName + "'").Dequeue();
		}
        public Storage StorageGetByPath(string sPath)
		{
			return StoragesGet("lower(replace(`sPath`,'\\','/'))='" + sPath.Remove("'") + "'").Dequeue();
		}
		public Storage StorageGet(long nID)
		{
			return StoragesGet("`id`=" + nID).Dequeue();
		}
		public Queue<Storage> StoragesGet()
		{
			return StoragesGet(null);
		}
		private Queue<Storage> StoragesGet(string sWhere)
		{
			Queue<Storage> aqRetVal = new Queue<Storage>();
			if (null == sWhere)
				sWhere = "";
			//_cDB.RoleSet("replica_assets");
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM media.`vStorages`" + (0 < sWhere.Length ? " WHERE " + sWhere : "") + " ORDER BY `sName`");
			while (null != aqDBValues && 0 < aqDBValues.Count)
				aqRetVal.Enqueue(new Storage(aqDBValues.Dequeue()));
			return aqRetVal;
		}
		#endregion

		#region mam
		protected Queue<Asset> AssetsGet(string sWhere)
		{
			return new Queue<Asset>(AssetsResolvedGet(sWhere));
		}
		protected Asset[] AssetsResolvedGet(string sWhere)
		{
			if (null == sWhere)
				sWhere = "";
			else if (0 < sWhere.Length)
				sWhere = " WHERE " + sWhere;
			return _cDB.Select("SELECT DISTINCT * FROM mam.`vAssetsResolved`" + sWhere + " ORDER BY `sName`").Select(o => AssetGet(o)).ToArray();
		}
		private Asset AssetGet(Hashtable ahValues)
		{
			switch (ahValues["sVideoTypeName"].ToStr())
			{
				case "clip":
					return new Clip(ahValues);
				case "advertisement":
					return new Advertisement(ahValues);
				case "program":
					return new Program(ahValues);
				case "design":
					return new Design(ahValues);
				default:
					return new Asset(ahValues);
			}
		}
		public IdNamePair AssetVideoTypeGet(long nAssetID)
		{
			//_cDB.RoleSet("replica_programs");
			return new IdNamePair(_cDB.GetRow("SELECT `idVideoTypes` as id, `sVideoTypeName` as `sName` FROM mam.`vAssetsVideoTypes` WHERE id=" + nAssetID));
		}
		public Asset AssetGet(long nID)
		{
			Queue<Asset> aqAssets = AssetsGet("id=" + nID);
			if (null != aqAssets && 0 < aqAssets.Count)
				return aqAssets.Dequeue();
			return null;
		}
		public Asset AssetGet(string sName)
		{
			Queue<Asset> aqAssets = AssetsGet(sName, SearchMask.equal);
			if (null != aqAssets && 0 < aqAssets.Count)
				return aqAssets.Dequeue();
			return null;
		}
		public Asset AssetGet(CustomValue stCV)
		{
			//_cDB.RoleSet("replica_assets");
			Asset cAsset = null;
			try
			{
				long nID = _cDB.GetValueInt("SELECT id FROM mam.`vAssetsCustomValues` WHERE `sCustomValueName`='" + stCV.sName + "' AND `sCustomValue`='" + stCV.sValue + "' LIMIT 1");
				cAsset = AssetGet(nID);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cAsset;
		}
		public Asset[] AssetsGet(CustomValue stCV)
		{
			//_cDB.RoleSet("replica_assets");
			Asset[] aRetVal = null;
			try
			{
				Queue<Hashtable> aqDBValues = null;
				if (null != (aqDBValues = _cDB.Select("SELECT ar.*, acv.`sCustomValueName`, acv.`sCustomValue` FROM mam.`vAssetsResolved` ar, mam.`vAssetsCustomValues` acv WHERE ar.id = acv.id AND acv.`sCustomValueName`='" + stCV.sName + (null != stCV.sValue ? "' AND acv.`sCustomValue`='" + stCV.sValue : "") + "'")))
				{
					aRetVal = new Asset[aqDBValues.Count];
					int nIndx = 0;
					Hashtable ahRow = null;
					while (0 < aqDBValues.Count)
					{
						ahRow = aqDBValues.Dequeue();
						aRetVal[nIndx] = new Asset(ahRow);
						aRetVal[nIndx].aCustomValues = new CustomValue[1];
						aRetVal[nIndx].aCustomValues[0] = new CustomValue(ahRow["sCustomValueName"].ToString(), ahRow["sCustomValue"].ToString());
						nIndx++;
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		public Queue<Asset> AssetsGet()
		{
			return AssetsGet((string)null);
		}

		public Queue<Asset> AssetsGet(string sName, SearchMask eSearchMask)
		{
			if (SearchMask.equal != eSearchMask)
			{
				switch (eSearchMask)
				{
					case SearchMask.starts:
						sName = sName + "%";
						break;
					case SearchMask.ends:
						sName = "%" + sName;
						break;
					case SearchMask.contains:
						sName = "%" + sName + "%";
						break;
				}
				sName = "LIKE '" + sName + "'";
			}
			else
				sName = "='" + sName + "'";
			return AssetsGet("`sName` " + sName);
		}
		public Queue<Asset> AssetsGet(IdNamePair cVideoTypeFilter)
		{
			string sWhere = null;
			if (null != cVideoTypeFilter)
			{
				if (-1 < cVideoTypeFilter.nID)
					sWhere = cVideoTypeFilter.nID + "=`idVideoTypes`";
				else if (0 < cVideoTypeFilter.sName.Length)
					sWhere = "'" + cVideoTypeFilter.sName + "'=`sVideoTypeName`";
			}
			return AssetsGet(sWhere);
		}
		public Queue<Asset> AssetsGet(File cFile)
		{
			return AssetsGet(cFile.nID + "=`idFiles`");
		}

		public IdNamePair[] AssetVideoTypesGet()
		{
			//_cDB.RoleSet("replica_assets");
			IdNamePair[] astRetVal = null;
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM mam.`tVideoTypes` ORDER BY `sName`");
			if (null == aqDBValues)
				return null;
			astRetVal = new IdNamePair[aqDBValues.Count];
			int nIndx = 0;
			while (0 < aqDBValues.Count)
				astRetVal[nIndx++] = new IdNamePair(aqDBValues.Dequeue());
			return astRetVal;
		}
		public void AssetRemove(Asset cAsset)
		{
			_cDB.GetValueBool("SELECT `bValue` FROM mam.`fAssetRemove`(" + cAsset.nID + ")");
		}
		public void AssetAdd(Asset cAsset)
		{
			try
			{
				cAsset.nID = _cDB.GetValueInt("SELECT `nValue` FROM mam.`fAssetAdd`('" + cAsset.sName.ForDB() + "')");
			}
			catch
			{
                cAsset.nID = extensions.x.ToID(null);
				throw;
			}
		}
		public void AssetSave(Asset cAsset)
		{
			_cDB.TransactionBegin();
			try
			{
				if (1 > cAsset.nID)
				{
					if (!access.scopes.assets.bCanCreate)
						return;
					AssetAdd(cAsset);
					_cDB.Perform("SELECT mam.`fVideoSet`(" + cAsset.nID + "," + cAsset.stVideo.cType.nID + ",'" + cAsset.stVideo.sName.ForDB() + "')");
				}
				else if (access.scopes.assets.name.bCanUpdate)
					_cDB.Perform("SELECT mam.`fAssetNameSet`(" + cAsset.nID + ", '" + cAsset.sName.ForDB() + "')");

				if (access.scopes.assets.bCanUpdate)
				{
					AssetParentSave(cAsset);
					if (null != cAsset.cType && 0 > cAsset.cType.nID && cAsset.cType.eType != Asset.Type.AssetType.part)
						cAsset.cType = AssetTypeGet(cAsset.cType.eType.ToString());
					AssetTypeSave(cAsset);
				}
				if (access.scopes.assets.file.bCanUpdate)
					AssetFileSave(cAsset);
				if (access.scopes.assets.classes.bCanUpdate)
					AssetClassSave(cAsset);
				if (access.scopes.assets.custom_values.bCanUpdate)
					AssetCustomValuesSave(cAsset);
				_cDB.TransactionCommit();
	
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public void AssetFileSave(Asset cAsset)
		{
			//_cDB.RoleSet("replica_programs_full");

			_cDB.TransactionBegin();
			try
			{
				if (null == cAsset.cFile)
				{
					_cDB.Perform("SELECT mam.`fAssetAttributeRemove`(" + cAsset.nID + ", 'nFrameIn');");
					_cDB.Perform("SELECT mam.`fAssetAttributeRemove`(" + cAsset.nID + ", 'nFrameOut');");
					_cDB.Perform("SELECT mam.`fAssetAttributeSet`(" + cAsset.nID + ",'nFramesQty'," + 0 + ")");
					_cDB.Perform("SELECT mam.`fAssetAttributeRemove`(" + cAsset.nID + ", 'file');");
				}
				else
				{
					_cDB.Perform("SELECT mam.`fAssetAttributeSet`(" + cAsset.nID + ",'nFrameIn'," + cAsset.nFrameIn + ")");
					_cDB.Perform("SELECT mam.`fAssetAttributeSet`(" + cAsset.nID + ",'nFrameOut'," + cAsset.nFrameOut + ")");
					_cDB.Perform("SELECT mam.`fAssetAttributeSet`(" + cAsset.nID + ",'nFramesQty'," + cAsset.nFramesQty + ")");
					_cDB.Perform("SELECT mam.`fAssetFileSet`(" + cAsset.nID + "," + cAsset.cFile.nID + ")");
				}
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public void AssetClassSave(Asset cAsset)
		{
			//_cDB.RoleSet("replica_assets_full");
			_cDB.TransactionBegin();
			try
			{
				if (null != cAsset.cClass)
					_cDB.Perform("SELECT mam.`fAssetClassSet`(" + cAsset.nID + "," + cAsset.cClass.nID + ")");
				else
					_cDB.Perform("SELECT mam.`fAssetAttributeRemove`(" + cAsset.nID + ", 'class');");
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public void AssetCustomValuesSave(Asset cAsset)
		{
			//_cDB.RoleSet("replica_assets_full");
			_cDB.TransactionBegin();
			try
			{
				_cDB.Perform("SELECT mam.`fAssetCustomValuesClear`(" + cAsset.nID + ")");
				if (null != cAsset.aCustomValues)
				{
					foreach (CustomValue stCV in cAsset.aCustomValues)
					{
						try
						{
							_cDB.Perform("SELECT mam.`fAssetCustomValueAdd`(" + cAsset.nID + ",'" + stCV.sName.ForDB() + "','" + stCV.sValue.ForDB() + "')");
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
						}
					}
				}
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public void AssetParentSave(Asset cAsset)
		{
			_cDB.TransactionBegin();
			try
			{
				helpers.replica.hk.RegisteredTable cRTtAssets = RegisteredTableGet("mam", "tAssets");
				if (null == cRTtAssets)
					throw new Exception("RegisteredTable is null: [mam.tAssets]");

				_cDB.Perform("DELETE FROM mam.`tAssetAttributes` WHERE `idAssets`=" + cAsset.nID + " AND `sKey`='parent';");
				//DELETE FROM mam."tAssetAttributes" WHERE "idAssets"=6397 AND "sKey"='parent';
				if (0 < cAsset.nIDParent)
					_cDB.Perform("SELECT * FROM mam.`fAssetAttributeAdd`(" + cAsset.nID + ", " + cRTtAssets.nID + ", 'parent', " + cAsset.nIDParent + ");");
				//SELECT * FROM mam."fAssetAttributeAdd"(6397, 20, 'parent', 7071);
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public void AssetTypeSave(Asset cAsset)
		{
			_cDB.TransactionBegin();
			try
			{
				_cDB.Perform("DELETE FROM mam.`tAssetAttributes` WHERE `idAssets`=" + cAsset.nID + " AND `sKey`='asset_type';");
				//DELETE FROM mam."tAssetAttributes" WHERE "idAssets"=6397 AND "sKey"='parent';

				helpers.replica.hk.RegisteredTable cRTtAssetTypes;
				if (null != cAsset.cType && 0 < cAsset.cType.nID)
				{
					cRTtAssetTypes = RegisteredTableGet("mam", "tAssetTypes");
					if (null == cRTtAssetTypes)
						throw new Exception("RegisteredTable is null: [mam.tAssetTypes]");

					_cDB.Perform("SELECT * FROM mam.`fAssetAttributeAdd`(" + cAsset.nID + ", " + cRTtAssetTypes.nID + ", 'asset_type', " + cAsset.cType.nID + ");");
					//SELECT * FROM mam."fAssetAttributeAdd"(6397, 56, 'parent', 7071);
				}
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public Asset.Type AssetTypeGet(string sType)
		{
			Hashtable ahAssetType = _cDB.GetRow("SELECT id, `sName` FROM mam.`tAssetTypes` WHERE `sName`='" + sType + "';");
			//SELECT id, "sValue" FROM mam."tCustomValues" WHERE "sValue"='part';
			return new Asset.Type(ahAssetType["id"], ahAssetType["sName"]);
		}
		public IdNamePair VideoTypeGet(string sType)
		{
			return _cDB.Select("SELECT id, `sName` FROM mam.`tVideoTypes` WHERE `sName`='" + sType + "'").Select(o => new IdNamePair(o)).FirstOrDefault();
			//SELECT id, "sName" FROM mam."tVideoTypes" WHERE "sName"='clip'
		}
		public IdNamePair[] VideoTypesGet()
		{
			return _cDB.Select("SELECT * FROM mam.`tVideoTypes` ORDER BY `sName`").Select(o => new IdNamePair(o)).ToArray();
		}
		public void AssetClassSet(Asset cAsset)
		{
			try
			{
				//_cDB.RoleSet("replica_assets_full");
				_cDB.Perform("SELECT mam.`fAssetClassSet`(" + cAsset.nID + "," + cAsset.cClass.nID + ")");
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		public void AssetDurationRefresh(long nAssetID)
		{
			//_cDB.RoleSet("replica_assets_full");
			_cDB.Perform("SELECT adm.`fAssetDurationRefresh`(" + nAssetID + ")");
		}

		public Queue<Clip> ClipsGet()
		{
			return new Queue<Clip>(AssetsResolvedGet("'clip'=`sVideoTypeName`").Select(o => (Clip)o));
		}
		public void ClipCuesSave(Clip cClip)
		{
			_cDB.Perform("SELECT mam.`fAssetCuesSet`(" + cClip.nID + "," + (null == cClip.stCues.sSong ? "NULL" : "'" + cClip.stCues.sSong.ForDB() + "'") + "," + (null == cClip.stCues.sArtist ? "NULL" : "'" + cClip.stCues.sArtist.ForDB() + "'") + "," + (null == cClip.stCues.sAlbum ? "NULL" : "'" + cClip.stCues.sAlbum.ForDB() + "'") + "," + (null == cClip.stCues.sYear ? "NULL" : cClip.stCues.sYear) + "," + (null == cClip.stCues.sPossessor ? "NULL" : "'" + cClip.stCues.sPossessor.ForDB() + "'") + ")");
		}
		public void ClipSave(Clip cClip)
		{
			//_cDB.RoleSet("replica_assets_full");
			_cDB.TransactionBegin();
			try
			{
				AssetSave(cClip);
				_cDB.Perform("SELECT mam.`fAssetPersonsClear`(" + cClip.nID + ")");
				foreach (Person stPerson in cClip.aPersons)
					_cDB.Perform("SELECT mam.`fAssetPersonAdd`(" + cClip.nID + "," + stPerson.nID + ")");

				_cDB.Perform("SELECT mam.`fAssetStylesClear`(" + cClip.nID + ")");
				foreach (IdNamePair cStyle in cClip.aStyles)
					_cDB.Perform("SELECT mam.`fAssetStyleAdd`(" + cClip.nID + "," + cStyle.nID + ")");
				if (null != cClip.cRotation)
					_cDB.Perform("SELECT mam.`fAssetRotationSet`(" + cClip.nID + "," + cClip.cRotation.nID + ")");
				if (null != cClip.cPalette)
					_cDB.Perform("SELECT mam.`fAssetPaletteSet`(" + cClip.nID + "," + cClip.cPalette.nID + ")");
				if (null != cClip.stSoundLevels.cStart)
					_cDB.Perform("SELECT mam.`fAssetSoundBeginSet`(" + cClip.nID + "," + cClip.stSoundLevels.cStart.nID + ")");
				if (null != cClip.stSoundLevels.cStop)
					_cDB.Perform("SELECT mam.`fAssetSoundEndSet`(" + cClip.nID + "," + cClip.stSoundLevels.cStop.nID + ")");

				ClipCuesSave(cClip);
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}

		public void AdvertisementSave(Advertisement cAdvertisement)
		{
			AssetSave(cAdvertisement);
		}

		public void ProgramSave(Program cProgram)
		{
			AssetSave(cProgram);
			if (access.scopes.programs.clips.bCanUpdate)
				ProgramClipsSet(cProgram);
		}

		public void DesignSave(Design cDesign)
		{
			AssetSave(cDesign);
		}

		public Queue<Program> ProgramsGet()
		{
			return new Queue<Program>(AssetsResolvedGet("'program'=`sVideoTypeName`").Select(o => (Program)o));
		}
		private List<long> ProgramClipIDsGet(Program cProgram)   // отмирает
		{
			//_cDB.RoleSet("replica_programs");
			RegisteredTable cRT = RegisteredTableGet("mam", "tAssets");
			if (null == cRT)
				throw new Exception("RegisteredTable is null: [mam.tAssets]");
			Queue<Hashtable> ahRows = _cDB.Select("SELECT `nValue` FROM mam.`tAssetAttributes` WHERE `idAssets`=" + cProgram.nID + " AND `sKey`='clip' and `idRegisteredTables`=" + cRT.nID + ";");  //"tAssets"
			// SELECT "nValue" FROM mam."tAssetAttributes" WHERE mam."tAssetAttributes"."idAssets"=891 AND mam."tAssetAttributes"."sKey"='clip' and "idRegisteredTables"=20

			//SELECT "idAssets", "nValue" from mam."tAssetAttributes" where id in (select "nValue" from mam."tAssetAttributes" where "idAssets"=1167 and "sKey"='clip' and "idRegisteredTables"=21);    //"tAssetAttributes"


			List<long> aResult = new List<long>();
			if (null != ahRows)
				while (0 < ahRows.Count)
					aResult.Add((int)ahRows.Dequeue()["nValue"]);
			return aResult;
		}
		private List<Program.RAOInfo> ProgramRAOInfoGet(Program cProgram)
		{
			//_cDB.RoleSet("replica_programs");
			RegisteredTable cRT = RegisteredTableGet("mam", "tAssetAttributes");
			if (null == cRT)
				throw new Exception("RegisteredTable is null: [mam.tAssetAttributes]");
			Queue<Hashtable> ahRows = _cDB.Select("SELECT `idAssets`, `nValue` as `nRAOFramesQty` from mam.`tAssetAttributes` where `sKey`='RAOFramesQty' and id in (select `nValue` from mam.`tAssetAttributes` where `idAssets`=" + cProgram.nID + " and `sKey`='clip' and `idRegisteredTables`=" + cRT.nID + ");");  //"tAssets"
			//SELECT "idAssets", "nValue" from mam."tAssetAttributes" where "sKey"='RAOFramesQty' and id in (select "nValue" from mam."tAssetAttributes" where "idAssets"=1167 and "sKey"='clip' and "idRegisteredTables"=21);    //"tAssetAttributes"

			List<Program.RAOInfo> aResult = new List<Program.RAOInfo>();
			Hashtable ahRow;
			if (null != ahRows)
				while (0 < ahRows.Count)
				{
					ahRow = ahRows.Dequeue();
					aResult.Add(new Program.RAOInfo() { cClip = ClipGet((int)ahRow["idAssets"]), nFramesQty = (int)ahRow["nRAOFramesQty"] });
				}
			return aResult;
		}
		public List<Program.RAOInfo> ProgramRAOInfo_old_and_new_Get(Program cProgram)
		{
			List<Program.RAOInfo> aResult = new List<Program.RAOInfo>();
			List<Program.RAOInfo> aRAOInfo = ProgramRAOInfoGet(cProgram);
			List<long> aIDs = ProgramClipIDsGet(cProgram);
			Program.RAOInfo cRAOInfo;

			foreach (long nID in aIDs.Where(o => null == aRAOInfo.FirstOrDefault(oo => oo.cClip.nID == o)))  // берем старым способом только те, которых нету в новом
			{
				aResult.Add(cRAOInfo = new Program.RAOInfo() { cClip = ClipGet(nID) });
				cRAOInfo.nFramesQty = cRAOInfo.cClip.nFramesQty;
			}
			aResult.AddRange(aRAOInfo);   // берем все новым способом
			return aResult;
		}

		protected void ProgramClipsSet(Program cProgram)
		{
			_cDB.TransactionBegin();
			try
			{
				RegisteredTable cRT = RegisteredTableGet("mam", "tAssets");
				RegisteredTable cRT_AA = RegisteredTableGet("mam", "tAssetAttributes");
				if (null == cRT)
					throw new Exception("RegisteredTable is null: [mam.tAssets]");
				if (null == cRT_AA)
					throw new Exception("RegisteredTable is null: [mam.tAssetAttributes]");


				Queue<Hashtable> ahRows = _cDB.Select("select `nValue` from mam.`tAssetAttributes` where `idAssets`=" + cProgram.nID + " and `sKey`='clip' and `idRegisteredTables`=" + cRT_AA.nID + ";");
				// select "nValue" from mam."tAssetAttributes" where "idAssets"=1167 and "sKey"='clip' and "idRegisteredTables"=21
				long nID;
				Hashtable ahRow;
				if (null != ahRows)
					while (0 < ahRows.Count)
					{
						ahRow=ahRows.Dequeue();
						nID = (int)ahRow["nValue"];
						_cDB.Perform("DELETE FROM mam.`tAssetAttributes` WHERE id=" + nID + ";");  // TODO сделать в БД прямую функцию....
						//DELETE FROM mam.`tAssetAttributes` WHERE id=12;
					}

				_cDB.Perform("SELECT * FROM mam.`fAssetAttributeRemove`(" + cProgram.nID + ", 'clip');");
				//SELECT * FROM mam."fAssetAttributeRemove"(6397, 'clip');
				if (null != cProgram.aRAOInfo)
					foreach (Program.RAOInfo cRAOI in cProgram.aRAOInfo)
					{
						nID = _cDB.GetValueLong("INSERT INTO mam.`tAssetAttributes` (`idAssets`,`idRegisteredTables`,`sKey`,`nValue`) VALUES (" + cRAOI.cClip.nID + ", null,'RAOFramesQty'," + cRAOI.nFramesQty + ") RETURNING id;");
						//                    INSERT INTO mam."tAssetAttributes" ("idAssets","idRegisteredTables","sKey","nValue") VALUES (8429, null,'RAOFramesQty',7950) RETURNING id;
						//                    SELECT * FROM mam."fAssetAttributeAdd"(8429, null, 'RAOFramesQty',7950);   
						//  fAssetAttributeAdd   не работает т.к. не вставляет одинаковые строки! Можно вернуть, если её починить
						if (nID < 1)
							throw new Exception("INSERT INTO mam.`tAssetAttributes` was unsuccessful");

						_cDB.Perform("SELECT * FROM mam.`fAssetAttributeAdd`(" + cProgram.nID + ", " + cRT_AA.nID + ", 'clip', " + nID + ");"); //TODO сделать в БД прямую функцию....
						// SELECT * FROM mam."fAssetAttributeAdd"(891, 20, 'clip', 54);
					}
				_cDB.TransactionCommit();
			}
			catch (Exception ex)
			{
				_cDB.TransactionRollBack();
				(new Logger()).WriteError(ex);
				throw;
			}
		}

		private Asset AssetGetByID(long nID)
		{
			return AssetsResolvedGet("id=" + nID).FirstOrDefault();
		}
		private Asset AssetGetByName(string sName)
		{
			return AssetsResolvedGet("`sName`='" + sName.ForDB() + "'").FirstOrDefault();
		}
		public Clip ClipGet(long nID)
		{
			return (Clip)AssetGetByID(nID);
		}
		public Advertisement AdvertisementGet(long nID)
		{
			return (Advertisement)AssetGetByID(nID);
		}
		public Program ProgramGet(long nID)
		{
			return (Program)AssetGetByID(nID);
		}
		public Program ProgramGet(string sName)
		{
			return (Program)AssetGetByName(sName);
		}
		public Design DesignGet(long nID)
		{
			return (Design)AssetGetByID(nID);
		}
		
		private IdNamePair[] INPsListGet(string sSQL)
		{
			return _cDB.Select(sSQL).Select(o => new IdNamePair(o)).ToArray();
		}

		public Class ClassGet(string sName)
		{
			return ClassesGet("`sName`='" + sName + "'").FirstOrDefault();
		}
		public Class ClassGet(long nID)
		{
			return ClassesGet("`id`=" + nID).Dequeue();
		}
		public Dictionary<long, Class> ClassesTreeGet()
		{
			Dictionary<long, Class> aRetVal = new Dictionary<long, Class>();
			Dictionary<long, Class> aRawClassesList = new Dictionary<long, Class>();
			Class cClass = null;
			try
			{
				Queue<Class> aqClasses = ClassesGet(null);
				while (0 < aqClasses.Count)
				{
					cClass = aqClasses.Dequeue();
					if (!aRawClassesList.ContainsKey(cClass.nID))
						aRawClassesList.Add(cClass.nID, cClass);
					else
						cClass = aRawClassesList[cClass.nID];
					if (null != cClass.cTestator)
					{
						if (aRawClassesList.ContainsKey(cClass.cTestator.nID))
						{
							if (!cClass.cTestator.bResolved)
								cClass.cTestator = aRawClassesList[cClass.cTestator.nID];
							else
								aRawClassesList[cClass.cTestator.nID] = cClass.cTestator;
							cClass.cTestator.bResolved = true;
							if (null == cClass.cTestator.aHeritors)
								cClass.cTestator.aHeritors = new Dictionary<long, Class>();
							cClass.cTestator.aHeritors.Add(cClass.nID, cClass);
							continue;
						}
					}
					else if (!aRetVal.ContainsKey(cClass.nID))
					{
						cClass.bResolved = true;
						aRetVal.Add(cClass.nID, cClass);
					}
					else
						continue;
					aqClasses.Enqueue(cClass);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		public Queue<Class> ClassesGet()
		{
			return ClassesGet(null);
		}
		private Queue<Class> ClassesGet(string sWhere)
		{
			//_cDB.RoleSet("replica_programs");
			Queue<Class> aqRetVal = new Queue<Class>();
			if (null == sWhere)
				sWhere = "";
			Class cClass = null;
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM pl.`vClasses`" + (0 < sWhere.Length ? " WHERE " + sWhere : "") + " ORDER BY `sName`");
			Hashtable ahRow = null;
			while (null != aqDBValues && 0 < aqDBValues.Count)
			{
				ahRow = aqDBValues.Dequeue();
				cClass = new Class();
				cClass.nID = ahRow["id"].ToID();
				cClass.sName = ahRow["sName"].ToString();
				if (null != ahRow["idParent"])
				{
					cClass.cTestator = new Class();
					cClass.cTestator.nID = ahRow["idParent"].ToID();
				}
				aqRetVal.Enqueue(cClass);
			}
			return aqRetVal;
		}

		public Person[] ArtistsLoad(long nAssetID)
		{
			//_cDB.RoleSet("replica_assets");
			if (0 > nAssetID)
				return null;
			string sSQL = "SELECT `idPersons` as id, `sPersonName` as `sName` FROM mam.`vAssetsPersons` WHERE id=" + nAssetID;
			Queue<Hashtable> aqDBValues = _cDB.Select(sSQL);
			Person[] astRetVal = new Person[aqDBValues.Count];
			Hashtable ahRow = null;
			int nIndx = 0;
			while (null != aqDBValues && 0 < aqDBValues.Count)
			{
				ahRow = aqDBValues.Dequeue();
				astRetVal[nIndx++] = new Person(ahRow["id"].ToID(), ahRow["sName"].ToString());
			}
			return astRetVal;
		}
		public IdNamePair[] StylesLoad(long nAssetID)
		{
			//_cDB.RoleSet("replica_assets");
			if (0 > nAssetID)
				return null;
			string sSQL = "SELECT `idStyles` as id, `sStyleName` as `sName` FROM mam.`vAssetsStyles` WHERE id=" + nAssetID;
			return INPsListGet(sSQL);
		}
		public CustomValue[] CustomsLoad(long nAssetID)
		{
			if (0 > nAssetID)
				return null;
			CustomValue[] aRetVal = null;
			Hashtable ahRow = null;
			Queue<Hashtable> aqDBValues = null;
			string sSQL = "SELECT `sCustomValueName` as `sName`, `sCustomValue` as `sValue` FROM mam.`vAssetsCustomValues` WHERE id=" + nAssetID + " ORDER BY `sCustomValueName` DESC";
			//_cDB.RoleSet("replica_programs");
			if (null != (aqDBValues = _cDB.Select(sSQL)))
			{
				aRetVal = new CustomValue[aqDBValues.Count];
				int nIndx = 0;
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					aRetVal[nIndx++] = new CustomValue(ahRow["sName"].ToString(), ahRow["sValue"].ToString());
				}
			}
			if (1 > aRetVal.Length)
				aRetVal = null;
			return aRetVal;
		}
		public Cues CuesLoad(long nAssetID)
		{
			//_cDB.RoleSet("replica_assets");
			string sSQL = "SELECT `idCues` as id, `sSong`, `sArtist`, `sAlbum`, `nYear`, `sPossessor` FROM mam.`vAssetsCues` WHERE id=" + nAssetID;
			Hashtable ahRow = _cDB.GetRow(sSQL);
			Cues stRetVal = new Cues(
				ahRow["id"].ToID(),
				null == ahRow["sSong"] ? null : ahRow["sSong"].ToString(),
				null == ahRow["sArtist"] ? null : ahRow["sArtist"].ToString(),
				null == ahRow["sAlbum"] ? null : ahRow["sAlbum"].ToString(),
				null == ahRow["nYear"] ? -1 : ahRow["nYear"].ToInt32(),
				null == ahRow["sPossessor"] ? null : ahRow["sPossessor"].ToString()
			);
			return stRetVal;
		}

		public IdNamePair PersonTypeGet(string sPersonType)
		{
			//_cDB.RoleSet("replica_assets");
			Hashtable ahRow = _cDB.GetRow("SELECT * FROM mam.`tPersonTypes` WHERE '" + sPersonType + "'=`sName`");
			if (null == ahRow)
				return null;
			return new IdNamePair(ahRow);
		}
		public void PersonSave(Person cPerson)
		{
			//_cDB.RoleSet("replica_assets_full");
			if (1 > cPerson.nID)
				cPerson.nID = _cDB.GetValueInt("SELECT mam.`fPersonAdd`(" + cPerson.cType.nID.ToString() + ", '" + cPerson.sName.ForDB() + "')");
			else
				_cDB.Perform("UPDATE mam.`tPersons` SET `sName` = '" + cPerson.sName.ForDB() + "' WHERE " + cPerson.nID + "=id;");
		}
		public void PersonRemove(Person cPerson)
		{
			//_cDB.RoleSet("replica_assets_full");
			bool bRes = _cDB.GetValueBool("SELECT `bValue` FROM mam.`fPersonRemove`(" + cPerson.nID.ToString() + ")");
			if (!bRes)
				throw new Exception("The person with id=" + cPerson.nID + " was not removed correctly");
		}
		public Person PersonGet(long nID)
		{
			return _cDB.Select("SELECT * FROM mam.`vPersons` WHERE id=" + nID).Select(o => new Person(o)).FirstOrDefault();
		}
		public Queue<Person> PersonsGet(IdNamePair cPersonTypeFilter)
		{
			string sWhere = null;
			if (null != cPersonTypeFilter)
			{
				if (-1 < cPersonTypeFilter.nID)
					sWhere = cPersonTypeFilter.nID + "=`idPersonTypes`";
				else if (0 < cPersonTypeFilter.sName.Length)
					sWhere = "'" + cPersonTypeFilter.sName + "'=`sPersonTypeName`";
			}
			return new Queue<Person>(PersonsGet(sWhere));
		}
		public Person[] ArtistsGet()
		{
			return PersonsGet("'artist'=`sPersonTypeName`").ToArray();
		}
		protected IEnumerable<Person> PersonsGet(string sWhere)
		{
			//_cDB.RoleSet("replica_assets");
			Queue<Person> aqRetVal = new Queue<Person>();
			if (null == sWhere)
				sWhere = "";
			else if (0 < sWhere.Length)
				sWhere = " WHERE " + sWhere;
			return _cDB.Select("SELECT DISTINCT * FROM mam.`vPersons`" + sWhere + " ORDER BY `sName`").Select(o => new Person(o));
		}
		public IdNamePair[] StylesGet()
		{
			//_cDB.RoleSet("replica_assets");
			string sSQL = "SELECT id, `sName` FROM mam.`vStyles`";
			return INPsListGet(sSQL);
		}
		public IdNamePair[] RotationsGet()
		{
			//_cDB.RoleSet("replica_assets");
			string sSQL = "SELECT id, `sName` FROM mam.`vRotations` ORDER BY `sName`";
			return INPsListGet(sSQL);
		}
		public IdNamePair[] PalettesGet()
		{
			//_cDB.RoleSet("replica_assets");
			string sSQL = "SELECT id, `sName` FROM mam.`vPalettes` ORDER BY `sName`";
			return INPsListGet(sSQL);
		}
		public IdNamePair[] SoundsGet()
		{
			//_cDB.RoleSet("replica_assets");
			string sSQL = "SELECT id, `sName` FROM mam.`vSoundLevels` ORDER BY `sName`";
			return INPsListGet(sSQL);
		}
		public IdNamePair AssetRotationGet(long nAssetID)
		{                                    // SELECT id, "sValue" AS "sName" FROM mam."tCategoryValues" WHERE id=  (SELECT "nValue" FROM mam."tAssetAttributes" WHERE "idAssets"=7                AND "sKey"='rotation')
			//_cDB.RoleSet("replica_assets");
			Queue<Hashtable> ahRotation = _cDB.Select("SELECT id, `sValue` AS `sName` FROM mam.`tCategoryValues` WHERE id=  (SELECT `nValue` FROM mam.`tAssetAttributes` WHERE `idAssets`=" + nAssetID + " AND `sKey`='rotation')");
			if (null != ahRotation && 0 < ahRotation.Count)
				return new IdNamePair(ahRotation.Dequeue());
			else
				return null;
		}
		#endregion

		#region cues
		protected TemplateBind[] TemplateBindsGet(string sWhere)
		{//	  				    SELECT cc.*, tt."sName", tt."sFile" FROM cues."tTemplates" tt JOIN cues."tClassAndTemplateBinds" cc on tt.id=cc."idTemplates" WHERE "sName" LIKE '%Анонс-%' order by "idTemplates";
			return _cDB.Select("SELECT cc.*, tt.`sName`, tt.`sFile` FROM cues.`tTemplates` tt JOIN cues.`tClassAndTemplateBinds` cc on tt.id=cc.`idTemplates` " + sWhere + " order by `idTemplates`;").Select(o => new TemplateBind(o)).ToArray();
		}
        public TemplateBind[] TemplateBindsGet(PlaylistItem cPLI)
        {
            try
            {
                Dictionary<string, object> ahParams = new Dictionary<string, object>();
                ahParams.Add("idPlaylistItems", (int)cPLI.nID);
                ahParams.Add("idClasses", cPLI.cClass.nID);
								  //SELECT * FROM (SELECT *, cues."fIsTemplateActual"(1707427, id) as "bActual" FROM cues."vClassAndTemplateBinds" WHERE 11="idClasses") s WHERE "bActual";
				return _cDB.Select("SELECT * FROM (SELECT *, cues.`fIsTemplateActual`(:idPlaylistItems, id) as `bActual` FROM cues.`vClassAndTemplateBinds` WHERE :idClasses = `idClasses`) s WHERE `bActual`", ahParams).Select(o => new TemplateBind(o) {
					cTemplate = new cues.Template(o["idTemplates"].ToID(), o["sTemplateName"].ToString(), o["sTemplateFile"].ToString()),
					cRegisteredTable = (null == o["idRegisteredTables"] ? null : new hk.RegisteredTable(o["idRegisteredTables"], o["sRegisteredTableSchema"], o["sRegisteredTableName"], o["sRegisteredTableUpdated"], null))
				}).ToArray();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return null;
        }
        public TemplatesSchedule[] TemplatesScheduleGet()
        {
            try
            {
                return _cDB.Select("SELECT * FROM cues.`tTemplatesSchedule`").Select(o => new TemplatesSchedule(o)).ToArray();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return null;
        }
		public TemplatesSchedule TemplatesScheduleGet(long nID)
		{
			try
			{
				return _cDB.Select("SELECT * FROM cues.`tTemplatesSchedule` where id=" + nID).Select(o => new TemplatesSchedule(o)).FirstOrDefault();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return null;
		}
		public TemplatesSchedule[] TemplatesScheduleGet(TemplateBind[] aTemplateBinds)
		{
			string sTemplIDs = "";
			try
			{
				foreach (TemplateBind cTempl in aTemplateBinds)
					sTemplIDs = sTemplIDs + "," + cTempl.nID;
				sTemplIDs = " (" + sTemplIDs.Substring(1, sTemplIDs.Length - 1) + ")";
				//                                         select ct.*, cc."idTemplates" from cues."tTemplatesSchedule" ct join cues."tClassAndTemplateBinds" cc on ct."idClassAndTemplateBinds"=cc.id where "idTemplates" in (22, 23) order by cc."idTemplates", "dtStart"
				//                                         select * from cues."tTemplatesSchedule" where "idClassAndTemplateBinds" in (68, 71) order by "idClassAndTemplateBinds", "dtStart"
				TemplatesSchedule[] aRetVal = _cDB.Select("select * from cues.`tTemplatesSchedule` where `idClassAndTemplateBinds` in " + sTemplIDs).Select(o => new TemplatesSchedule(o)).ToArray();
				foreach (TemplatesSchedule cTS in aRetVal)
					cTS.cTemplateBind = aTemplateBinds.FirstOrDefault(o => o.nID == cTS.cTemplateBind.nID);
				return aRetVal;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return null;
		}
		public void TemplatesScheduleSave(TemplatesSchedule cTemplatesSchedule)
		{
			Dictionary<string, object> ahParams = new Dictionary<string, object>();
			ahParams.Add("idClassAndTemplateBinds", cTemplatesSchedule.cTemplateBind.nID);
			ahParams["dtStart"] = cTemplatesSchedule.dtStart;
			//EMERGENCY:l может в таком виде и не сработать...  Да, TimeSpan передаётся в СЛ как очень странный бесполезный класс...
			ahParams.Add("tsInterval", new TimeSpan(0, 0, 0, 0, cTemplatesSchedule.nIntervalInMilliseconds));
			ahParams.Add("dtStop", (cTemplatesSchedule.dtStop.IsNullOrEmpty() ? null : cTemplatesSchedule.dtStop.ToString("yyyy-MM-dd HH:mm:ss")));
			if (0 < cTemplatesSchedule.nID)
			{
				ahParams.Add("nID", cTemplatesSchedule.nID);
				ahParams["dtLast"] = cTemplatesSchedule.dtLast;
				_cDB.Perform("UPDATE cues.`tTemplatesSchedule` SET `idClassAndTemplateBinds`=:idClassAndTemplateBinds, `dtLast`=:dtLast, `dtStart`=:dtStart, `tsInterval`=:tsInterval, `dtStop`=:dtStop WHERE :nID = id;", ahParams);
			}
			else
				cTemplatesSchedule.nID = _cDB.GetID("SELECT `nValue` FROM cues.`fTemplatesScheduleAdd`(:idClassAndTemplateBinds, :dtStart, :tsInterval, :dtStop);", ahParams);

			if (null != cTemplatesSchedule.aDictionary && 0 < cTemplatesSchedule.nID)
			{
				RegisteredTable cCuesTemplateSchedule = RegisteredTableGet("cues", "tTemplatesSchedule");
				if (null == cCuesTemplateSchedule)
					throw new Exception("RegisteredTable is null: [cues.tTemplatesSchedule]");

				ahParams.Clear();
				ahParams.Add("nRegisteredTables", cCuesTemplateSchedule.nID);
				ahParams.Add("nID", cTemplatesSchedule.nID);
				ahParams.Add("sKey", "");
				ahParams.Add("sValue", "");
				foreach (DictionaryElement cDE in cTemplatesSchedule.aDictionary)
				{
					ahParams["sKey"] = cDE.sKey;
					ahParams["sValue"] = cDE.sValue;
					_cDB.GetValueBool("SELECT `bValue` FROM cues.`fDictionaryValueAdd`(:nRegisteredTables, :nID, :sKey, :sValue);", ahParams);
				}
			}
		}
		public bool DictionaryElementSave(DictionaryElement cDE)
		{
			Dictionary<string, object> ahParams = new Dictionary<string, object>();
			ahParams.Add("nRegisteredTables", cDE.nRegisteredTablesID);
			ahParams.Add("nTargetID", cDE.nTargetID);
			ahParams.Add("sKey", cDE.sKey);
			ahParams.Add("sValue", cDE.sValue);
			if (false == _cDB.GetValueBool("SELECT `bValue` FROM cues.`fDictionaryValueRemove`(:nRegisteredTables, :nTargetID, :sKey);", ahParams))
				return false;

			return _cDB.GetValueBool("SELECT `bValue` FROM cues.`fDictionaryValueAdd`(:nRegisteredTables, :nTargetID, :sKey, :sValue);", ahParams);
		}
        public bool TemplatesScheduleRemove(TemplatesSchedule cTemplatesSchedule)
        {
            try
            {
				RegisteredTable nCuesTemplateSchedule = RegisteredTableGet("cues", "tTemplatesSchedule");
				if (null == nCuesTemplateSchedule)
					throw new Exception("RegisteredTable is null: [cues.tTemplatesSchedule]");
                Dictionary<string, object> ahParams = new Dictionary<string, object>();
				ahParams.Add("nTargetID", cTemplatesSchedule.nID);
				ahParams.Add("nRegisteredTables", nCuesTemplateSchedule.nID);
				if (false == _cDB.GetValueBool("SELECT `bValue` FROM cues.`fDictionaryValueRemove`(:nRegisteredTables, :nTargetID);", ahParams))
					return false;

				return _cDB.GetValueBool("SELECT `bValue` FROM cues.`fTemplatesScheduleRemove`(:nTargetID);", ahParams);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return false;
        }
		public DictionaryElement[] DictionaryGet(string sWhere)
		{
			return _cDB.Select("SELECT * FROM cues.`tDictionary` " + sWhere + ";").Select(o => new DictionaryElement(o)).ToArray(); 
		}
		public Queue<ChatInOut> ChatInOutsGet(Asset cAsset)
		{
			Queue<ChatInOut> aqRetVal = new Queue<ChatInOut>();
			if (null != cAsset)
			{
				Queue<Hashtable> aqDBValues = null;
				//_cDB.RoleSet("replica_programs");
				if (null != (aqDBValues = _cDB.Select("SELECT id, `nFrameIn`,`nFrameOut` FROM cues.`tChatInOuts` WHERE `idAssets`=" + cAsset.nID + " ORDER BY `nFrameIn`")))
				{
					while (0 < aqDBValues.Count)
						aqRetVal.Enqueue(new ChatInOut(aqDBValues.Dequeue()));
				}
			}
			return aqRetVal;
		}
		public void ChatInOutsSave(Asset cAsset, ChatInOut[] aChatInOuts)
		{
			_cDB.TransactionBegin();
			try
			{
				//_cDB.RoleSet("replica_programs_full");
				_cDB.Perform("SELECT cues.`fChatInOutsClear`(" + cAsset.nID + ")");
				for (int nIndx = 0; aChatInOuts.Length > nIndx; nIndx++)
				{
					if (aChatInOuts[nIndx].cTimeRange.nTicksOut < long.MaxValue && aChatInOuts[nIndx].cTimeRange.nTicksOut > aChatInOuts[nIndx].cTimeRange.nTicksIn)
						_cDB.Perform("SELECT cues.`fChatInOutAdd`(" + cAsset.nID + "," + aChatInOuts[nIndx].cTimeRange.nFrameIn + "," + aChatInOuts[nIndx].cTimeRange.nFrameOut + ")");
				}
				_cDB.TransactionCommit();
			}
			catch
			{
				_cDB.TransactionRollBack();
				throw;
			}
		}
		#endregion

		#region ia
		public void MessageAdd(Message cMsg)
		{
			string sSQL = "SELECT `nValue` FROM ia.`fMessageAdd`(";
			try
			{
				Dictionary<string, object> ahParams = null;
				sSQL += "'" + cMsg.sBindID + "',";
				sSQL += "'" + cMsg.cGatewayIP.cIP.ToString() + "',";
				sSQL += cMsg.nCount + ",";
				sSQL += cMsg.nSourceNumber + ",";
				sSQL += cMsg.nTargetNumber + ",";
				sSQL += "'" + cMsg.sText + "',";
				if (null != cMsg.aImageBytes)
				{
					sSQL += ":ImageBinaryData)";
					ahParams = new Dictionary<string, object>();
					ahParams.Add("ImageBinaryData", cMsg.aImageBytes);
				}
				else
					sSQL += "NULL)";
				long nID = _cDB.GetValueInt(sSQL, ahParams);
				//nRetVal = _cDB.GetValueInt(sSQL);
				cMsg.nID = nID;
			}
			catch (Exception ex)
			{
				throw new Exception(sSQL, ex);
			}
		}

		protected Queue<Message> MessagesGet(string sWhere, string sOrderBy, ushort nLimit)
		{
			Queue<Message> aqRetVal = new Queue<Message>();
			_cDB.Analyze("ia", "tDTEvents");
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT * FROM ia.`vMessagesResolved`", sWhere, sOrderBy, 0, nLimit);
			while (null != aqDBValues && 0 < aqDBValues.Count)
				aqRetVal.Enqueue(new Message(aqDBValues.Dequeue()));
			return aqRetVal;
		}
		protected Queue<Message> MessagesGet(string sWhere, ushort nLimit)
		{
			return MessagesGet(sWhere, "`dtRegister`", nLimit);
		}
		public Message MessageGetByBindID(string sBindID)
		{
			Message cRetVal = null;
			try
			{
				Queue<Message> aqMsgs = MessagesGet("'" + sBindID + "' = `sBindID`", "`dtRegister`", 1);
				if (null != aqMsgs && 0 < aqMsgs.Count)
					cRetVal = aqMsgs.Dequeue();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}

		#endregion

		#region scr
		public Shift ShiftGet(long nID)
		{
			Hashtable ahRow = _cDB.GetRow("SELECT *, `idTemplates` as `idPresets`, `sTemplateName` as `sPresetName` FROM scr.`vShifts` WHERE id=" + nID); //TODO SCR PRESET
			if (null != ahRow)
				return new Shift(ahRow);
			return null;
		}
		public Shift ShiftCurrentGet()
		{
			Hashtable ahRow = _cDB.GetRow("SELECT *, `idTemplates` as `idPresets`, `sTemplateName` as `sPresetName` FROM scr.`vShiftCurrent`"); //TODO SCR PRESET
			if(null != ahRow)
				return new Shift(ahRow);
			return null;
		}
		public Shift[] ShiftsGet(DateTime dtBegin, DateTime dtEnd)
		{										   //SELECT id, "dtStart", "dtStop", "idTemplates" AS "idPresets", "sTemplateName" AS "sPresetName", "sSubject", dt FROM scr."vShifts" WHERE "dtStop" > '2013-01-01 07:40:21'							  AND "dtStart" < '2013-01-15 07:40:21'							  ORDER BY "dtStart";
			Queue<Hashtable> ahShifts = _cDB.Select("SELECT id, `dtStart`, `dtStop`, `idTemplates` AS `idPresets`, `sTemplateName` AS `sPresetName`, `sSubject`, dt FROM scr.`vShifts` WHERE `dtStop` > '" + dtBegin.ToString("yyyy-MM-dd HH:mm:ss") + "' AND `dtStart` < '" + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' ORDER BY `dtStart`;"); //TODO SCR PRESET
			List<Shift> aRetVal = new List<Shift>();
			if (null != ahShifts)
				foreach (Hashtable ahRow in ahShifts)
					aRetVal.Add(new Shift(ahRow));
			return aRetVal.ToArray();
		}
		public IdNamePair PresetGet(string sPresetName)
		{
			Hashtable ahRow = _cDB.GetRow("SELECT * FROM scr.`tTemplates` WHERE `sName`='" + sPresetName + "'"); //TODO SCR PRESET
			if (null != ahRow)
				return new IdNamePair(ahRow);
			return null;
		}
		public Shift ShiftAdd(IdNamePair cPreset, string sSubject)
		{
			Hashtable ahRow = _cDB.GetRow("SELECT * FROM scr.`fShiftAdd`(" + cPreset.nID.ToString() + ",'" + sSubject + "')");
			if (!ahRow["bValue"].ToBool())
				throw new Exception(g.Helper.sErrorDBInteract3 + " [preset:" + cPreset.nID + "][subject:" + sSubject + "]"); 
			return ShiftGet(ahRow["nValue"].ToID());
		}
		public void ShiftStart(Shift cShift)
		{
			if (!_cDB.GetValueBool("SELECT `bValue` FROM scr.`fShiftStart`(" + cShift.nID.ToString() + ")"))
				throw new Exception(g.Helper.sErrorDBInteract4 + " [id:" + cShift.nID + "]");
		}
		public void ShiftStop(Shift cShift)
		{
			if (!_cDB.GetValueBool("SELECT `bValue` FROM scr.`fShiftStop`(" + cShift.nID.ToString() + ")"))
				throw new Exception(g.Helper.sErrorDBInteract5 + " [id:" + cShift.nID + "]");
		}
		public bool IsThereAnyStartedLiveBroadcast()
		{
			Shift cShift = ShiftCurrentGet();
			return (null != cShift && null != cShift.cPreset && 0 < cShift.cPreset.nID);
		}
		public Asset SCRAssetCurrentGet()
		{
			return _cDB.Select("SELECT a.id FROM logs.`tSCR` s, mam.`vAssetsResolved` a WHERE s.`idAssets`=a.id AND `dtStart` > now() - ((a.`nFramesQty` * 40)::text || ' milliseconds')::interval AND `dtStop` IS NULL").Select(o => AssetGet(o["id"].ToID())).FirstOrDefault();
		}
		#endregion

		#region adm
		public bool AdmSCROnAirGet()//удалить, как явление. сам механизм заменить на Shift'ы (scr."tShifts")
		{
			return _cDB.GetValueBool("SELECT `sValue` FROM adm.`vPreferences` WHERE 'runtime'=`sClassName` AND 'scr_onair'=`sName` AND `bClassActive` AND `bActive`");
		}
		public void AdmSCROnAirSet(bool bOnAir)//удалить, как явление. сам механизм заменить на Shift'ы (scr."tShifts")
		{
			_cDB.Perform("UPDATE adm.`tPreferences` SET `sValue`=" + bOnAir.ToString() + " WHERE id in (SELECT id FROM adm.`vPreferences` WHERE 'runtime'=`sClassName` AND 'scr_onair'=`sName` AND `bClassActive` AND `bActive`)");
		}
		public TimeSpan AdmClipDurationMinimumForCutGet()
		{
			return new TimeSpan(0, 0, _cDB.GetValueInt("SELECT `sValue`::int FROM adm.`vPreferences` WHERE 'grid'=`sClassName` AND 'clip_duration_minimum_for_cut'=`sName` AND `bClassActive` AND `bActive`"));
		}
		public TimeSpan AdmPLIDurationMinimumGet()
		{
			return new TimeSpan(0, 0, _cDB.GetValueInt("SELECT `sValue`::int FROM adm.`vPreferences` WHERE 'grid'=`sClassName` AND 'pli_duration_minimum'=`sName` AND `bClassActive` AND `bActive`"));
		}
        public void AdmPreferencesSocialSet(string sKey, string sValue)
        {
            Dictionary<string, object> ahParams = new Dictionary<string, object>();
            ahParams.Add("sKey", sKey);
            ahParams.Add("sValue", sValue);
            _cDB.Perform("SELECT * FROM adm.`fPreferenceSet`('social', :sKey, :sValue);", ahParams);
        }
        public string AdmPreferencesSocialGet(string sKey)
        {
            Dictionary<string, object> ahParams = new Dictionary<string, object>();
            ahParams.Add("sKey", sKey);
            return _cDB.GetValue("SELECT `sValue` FROM adm.`vPreferences` WHERE 'social'=`sClassName` AND :sKey=`sName` AND `bClassActive` AND `bActive`", ahParams);
        }
        #endregion

		#region preferences
		public string UserHomePageGet()
		{
			try
			{
				return _cDB.GetValue("SELECT * FROM hk.`fUserHomePageValueGet`()");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return null;
		}
		#endregion

	}
}
