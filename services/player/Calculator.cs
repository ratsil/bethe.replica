using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using helpers;
using helpers.extensions;
using helpers.replica.pl;

namespace replica.playlist
{
	class Calculator
	{
		public class Logger : helpers.Logger
		{
			static public string sFile = null;

			public Logger()
				: base("calc", sFile)
			{ }
		}
		public delegate void ItemsInsertedFillDelegate(Dictionary<long, List<long>> ahItemsInserted, Dictionary<long, PlaylistItem> ahItemsInsertedBinds);
		public delegate void ItemStartPlannedSetDelegate(long nID, DateTime dtStartPlanned);
		public delegate void ItemTimingsUpdateSetDelegate(long nID, DateTime dtStartPlanned);
		public delegate void ItemFrameStopChangeDelegate(long nID, long nFrameStopInitial, long nFrameStopNew);
		public delegate void ItemFrameStopRestoreDelegate(long nID);
		public delegate void PlugUpdateDelegate(long nID, DateTime dtStart, long nDuration, DateTime dtTimingsUpdate);
		public delegate void PlugAddDelegate(DateTime dtStart, long nDuration, DateTime dtTimingsUpdate);
		public delegate void ItemRemoveDelegate(long nID);
		public delegate void ItemsRemoveFromCachedDelegate(List<long> aIDs);

		public static DateTime dtLast = DateTime.MinValue;
		public static object cSyncRoot = new object();

		public ItemsInsertedFillDelegate ItemsInsertedFill;
		public ItemStartPlannedSetDelegate ItemStartPlannedSet;
		public ItemTimingsUpdateSetDelegate ItemTimingsUpdateSet;
		public ItemFrameStopChangeDelegate ItemFrameStopChange;
		public ItemFrameStopRestoreDelegate ItemFrameStopRestore;
		public PlugUpdateDelegate PlugUpdate;
		public PlugAddDelegate PlugAdd;
		public ItemRemoveDelegate ItemRemove;
		public ItemsRemoveFromCachedDelegate ItemsRemoveFromCached;

		public TimeSpan tsUpdateScope;
		public long nStartPlitemsID;
		public List<PlaylistItem> aPLIs;
		public Dictionary<long, long> ahFrameStopsInitials;
		public Queue<PlaylistItem> aqPlugs;
		public List<long> aCached;

		long _nPROBA_ID_WHEN_EXCEPTION = 0;
		DateTime _dtPROBA_ID_WHEN_EXCEPTION;

		private bool ItemIsTimmed(PlaylistItem cPLI)
		{
			if (DateTime.MaxValue > cPLI.dtStartHard || DateTime.MaxValue > cPLI.dtStartSoft)
				return true;
			else
				return false;
		}
		private bool ItemIsSequential(PlaylistItem cPLI)
		{
			if (DateTime.MaxValue == cPLI.dtStartHard && DateTime.MaxValue == cPLI.dtStartSoft)
				return true;
			else
				return false;
		}
		private bool ItemsAreInBlock(PlaylistItem cPLI1, PlaylistItem cPLI2)
		{
			if (null == cPLI1 || null == cPLI2)
				return false;
			if (1 == cPLI2.dtStartHardSoft.Subtract(cPLI1.dtStartHardSoft).TotalSeconds)
				return true;
			else
				return false;
		}
		private void CachedRemover(long nID)
		{
			// Идея была можт неплохая, но т.к. сами файлы не удаляются из кэша, а только инфа о них, то возникает пузырь, т.к. 
			// синк добавляет к концу своего списка из реальных файлов в кэше
			// так что поправлю отображение в клиенте...
			return;



			if (null == aCached)
			{
				(new Logger()).WriteNotice("Calculator doesn't have any information about cached items");
				return;
			}
			if (null == ItemsRemoveFromCached)
			{
				(new Logger()).WriteNotice("Calculator doesn't have function to remove items from cached_items list");
				return;
			}
			if (0 == aCached.Count)
				return;

			if (aCached.Contains(nID))  // пока идут только кэшеды, то их не трогаем, как только вклинился другой, то все остальные удаляем
				aCached.Remove(nID);
			else
			{
				ItemsRemoveFromCached(aCached);
				aCached.Clear();
			}
		}
		public bool Calculate()
		{
			lock (cSyncRoot)
			{
				(new Logger()).WriteNotice("****************************************************************");
				(new Logger()).WriteNotice("begin: UpdateScope: " + (tsUpdateScope == TimeSpan.MaxValue ? "UNLIMITED" : tsUpdateScope.ToString()) + " startPLItem: " + nStartPlitemsID);
				(new Logger()).WriteNotice("****************************************************************");
				bool bRetVal, bForceAdd;
				long nDiff, nDuration;

				Queue<PlaylistItem> aqItemsNew = new Queue<PlaylistItem>();
				Queue<PlaylistItem> aqItemsLocked = new Queue<PlaylistItem>();
				Queue<PlaylistItem> aqItemsSequential = new Queue<PlaylistItem>();
				Queue<PlaylistItem> aqItemsTimed = new Queue<PlaylistItem>();
				aqPlugs = new Queue<PlaylistItem>();

				PlaylistItem cItemPrevious = null, cItemCurrentSequential = null, cItemCurrentTimed = null;
				DateTime dtStart = DateTime.MaxValue;
				DateTime dtTimingsUpdate = DateTime.Now;
				string sLockeds = "";

				foreach (PlaylistItem cPLI in aPLIs.Where(pli => pli.bLocked).OrderBy(pli => pli.dtStartQueued).ToArray())
				{
					sLockeds += ", " + cPLI.nID;
					aqItemsLocked.Enqueue(cPLI);
					if (1 == aqItemsLocked.Count)
						(new Logger()).WriteNotice("first locked: id=" + cPLI.nID + "; status=" + cPLI.cStatus.sName + "; sr=" + cPLI.dtStartReal);
					aPLIs.Remove(cPLI);
				}
				if (null != sLockeds && 2 < sLockeds.Length)
					(new Logger()).WriteNotice("lockeds: " + sLockeds.Substring(2));

				PlaylistItem cStrangePLI = aPLIs.FirstOrDefault(o=>o.dtStartQueued< DateTime.MaxValue);
				if (null != cStrangePLI)
					(new Logger()).WriteNotice("NOT locked, but HAS dtStartQueued: [id=" + cStrangePLI.nID + "][name=" + cStrangePLI.sName + "][planned=" + cStrangePLI.dtStartPlanned + "][queued=" + cStrangePLI.dtStartQueued + "]");

				foreach (PlaylistItem cPLI in aPLIs.Where(pli => DateTime.MaxValue > pli.dtStartReal).ToArray())
				{
					(new Logger()).WriteError("planned has real_start!!! id=[" + cPLI.nID + "]");
				}

				if (nStartPlitemsID > 0 && nStartPlitemsID < int.MaxValue) // ------ СЛУЧАЙ ЧАСТИЧНОГО ПЕРЕСЧЕТА ------  значит надо искать ПЛИ, от которого пересчитывать, а до которого ничего не трогать...
				{
					PlaylistItem[] aOrderedPLIs = aPLIs.Where(pli => pli.dtTimingsUpdate < DateTime.MaxValue).OrderByDescending(pli => pli.dtTimingsUpdate).ThenBy(pli => pli.dtStartPlanned).ToArray();
					int nStartIndx = 0;
					foreach (PlaylistItem cPLI in aOrderedPLIs)  // ищем этот ПЛИ
					{
						if (nStartPlitemsID == cPLI.nID)
							break;
						nStartIndx++;
					}
					if (aOrderedPLIs.Length > nStartIndx)  // нашли этот ПЛИ и всё до него удаляем, его делаем единственным локедом, делаем текущее время пересчета равным его таймингу и считаем как будто так и надо...
					{
						(new Logger()).WriteNotice("clearing locked");
						aqItemsLocked.Clear();
						aqItemsLocked.Enqueue(aOrderedPLIs[nStartIndx]);
						dtTimingsUpdate = aOrderedPLIs[nStartIndx].dtTimingsUpdate;
						for (int nI = 0; nStartIndx >= nI; nI++)
						{
							aPLIs.Remove(aOrderedPLIs[nI]);
						}
					}
					else    // не нашли этот ПЛИ, хотя он должен быть по идее, значит - ошибко....
					{
						(new Logger()).WriteError("the PLI [id=" + nStartPlitemsID + "] was not found in PL 'WHERE dtTimingsUpdate < DateTime.MaxValue AND NOT bLocked'");
						return false;
					}
				}
				foreach (PlaylistItem cPLI in aPLIs.Where(pli => pli.bPlug).ToArray())
				{
					aqPlugs.Enqueue(cPLI);
					aPLIs.Remove(cPLI);
				}
				foreach (PlaylistItem cPLI in aPLIs.Where(pli => DateTime.MaxValue > pli.dtStartSoft || DateTime.MaxValue > pli.dtStartHard).OrderBy(pli => pli.dtStartHardSoft).ToArray())
				{
					aqItemsTimed.Enqueue(cPLI);
					aPLIs.Remove(cPLI);
				}
				//pl."vItemsInserted"
				PlaylistItem[] aItemsNew = aPLIs.Where(pli => DateTime.MaxValue == pli.dtTimingsUpdate).OrderBy(pli => pli.dtStartPlanned).ToArray();
				Dictionary<long, List<long>> ahItemsInserted = new Dictionary<long, List<long>>();
				Dictionary<long, PlaylistItem> ahItemsInsertedBinds = new Dictionary<long, PlaylistItem>();
				if (null != aItemsNew && 0 < aItemsNew.Length && null != ItemsInsertedFill)
				{
					ItemsInsertedFill(ahItemsInserted, ahItemsInsertedBinds);
					foreach (PlaylistItem cPLI in aItemsNew)
					{
						if (ahItemsInsertedBinds.ContainsKey(cPLI.nID))
							ahItemsInsertedBinds[cPLI.nID] = cPLI;
						else
							aqItemsNew.Enqueue(cPLI);
						aPLIs.Remove(cPLI);
					}
				}
				foreach (PlaylistItem cPLI in aPLIs.OrderByDescending(pli => pli.dtTimingsUpdate).ThenBy(pli => pli.dtStartPlanned).ToArray())
				{   // вставляем секвентиалы и привязки к ним...
					if (ahFrameStopsInitials.ContainsKey(cPLI.nID))
						cPLI.nFrameStop = ahFrameStopsInitials[cPLI.nID];
					aqItemsSequential.Enqueue(cPLI);
					if (ahItemsInserted.ContainsKey(cPLI.nID))
					{
						for (int nIndx = 0; ahItemsInserted[cPLI.nID].Count > nIndx; nIndx++)
						{
							if (null != ahItemsInsertedBinds[ahItemsInserted[cPLI.nID][nIndx]])
							{
								aqItemsSequential.Enqueue(ahItemsInsertedBinds[ahItemsInserted[cPLI.nID][nIndx]]);
								aItemsNew[Array.IndexOf(aItemsNew, ahItemsInsertedBinds[ahItemsInserted[cPLI.nID][nIndx]])] = null;
							}
						}
					}
				}
				while (0 < aqItemsNew.Count)
				{    // вставляем непривязанные новинки //и возможные привязки к ним.... новинки вставляем в конец т.к. они были добавлены явно позже (иначе они должны были быть вставлены insert-ом)
					cItemCurrentSequential = aqItemsNew.Dequeue();
					aqItemsSequential.Enqueue(cItemCurrentSequential);
					//if (ahItemsInserted.ContainsKey(cItemCurrentSequential.nID))   // К непересчитанным ни разу айтемам лучше не позволять привязывать ИМХО ??
					//{
					//    for (int nIndx = 0; ahItemsInserted[cItemCurrentSequential.nID].Count > nIndx; nIndx++)
					//    {
					//        if (null != ahItemsInsertedBinds[ahItemsInserted[cItemCurrentSequential.nID][nIndx]])
					//        {
					//            aqItemsSequential.Enqueue(ahItemsInsertedBinds[ahItemsInserted[cItemCurrentSequential.nID][nIndx]]);
					//            aItemsNew[Array.IndexOf(aItemsNew, ahItemsInsertedBinds[ahItemsInserted[cItemCurrentSequential.nID][nIndx]])] = null;
					//        }
					//    }
					//}
					aItemsNew[Array.IndexOf(aItemsNew, cItemCurrentSequential)] = null;
				}
				aItemsNew = aItemsNew.Where(pli => null != pli).OrderBy(pli => pli.dtStartPlanned).ToArray();
				if (0 < aItemsNew.Length)
				{    // вставляем привязки к рекламе и другие странные привязки оставшиеся видимо после удаления хозяев....  вставляем по пленнеду....
					int nIndxNew = 0;
					while (0 < aqItemsSequential.Count)
					{
						cItemCurrentSequential = aqItemsSequential.Dequeue();
						while (aItemsNew.Length > nIndxNew && cItemCurrentSequential.dtStartPlanned > aItemsNew[nIndxNew].dtStartPlanned)  //  && (1 > aqItemsSequential.Count || aqItemsSequential.Peek().dtStartPlanned >= aItemsNew[nIndxNew].dtStartPlanned)
						{  // вставляем всех, кто раньше текущего....
							aqItemsNew.Enqueue(aItemsNew[nIndxNew]);
							aItemsNew[nIndxNew] = null;
							nIndxNew++;
						}
						aqItemsNew.Enqueue(cItemCurrentSequential);  // вставляем текущего...
					}
					aqItemsSequential = aqItemsNew;
					foreach (PlaylistItem cPLI in aItemsNew)   // вставляем всё остальное, что позже последнего секвентиала...
					{
						if (null != cPLI)
							aqItemsSequential.Enqueue(cPLI);
					}
				}

				cItemCurrentSequential = cItemPrevious = null;
//				bRetVal = false;
				bRetVal = true;   // чо-то смысла в ретвале нет уже походу...
				while (0 < aqItemsLocked.Count)
				{
					cItemPrevious = aqItemsLocked.Dequeue();
					if (DateTime.MaxValue == dtStart || DateTime.MaxValue > cItemPrevious.dtStartReal)
					{
						if (DateTime.MaxValue == cItemPrevious.dtStartPlanned)
							throw new Exception("impossible state: dtStartPlanned is empty [" + cItemPrevious.nID + "]");
						if (DateTime.MaxValue != dtStart)
						{
							(new Logger()).WriteError(new Exception("got a few real starts [" + dtStart.ToStr() + "][" + cItemPrevious.nID + "][" + cItemPrevious.dtStartReal.ToStr() + "]"));

						}
						if (DateTime.MaxValue > cItemPrevious.dtStartReal)
						{
							dtStart = cItemPrevious.dtStartReal;
							(new Logger()).WriteNotice("locked start real:" + dtStart.ToStr() + "[id:" + cItemPrevious.nID + "]");
						}
						else
						{
							dtStart = cItemPrevious.dtStartPlanned;
							(new Logger()).WriteNotice("locked start planned:" + dtStart.ToStr() + "[id:" + cItemPrevious.nID + "]");
						}
//						bRetVal = true;
					}
					else
					{
						(new Logger()).WriteNotice("update locked:[new_sp" + dtStart.ToStr() + "][id:" + cItemPrevious.nID + "][sp:" + cItemPrevious.dtStartPlanned.ToStr() + "][sq:" + cItemPrevious.dtStartQueued.ToStr() + "]");
						if(null != ItemStartPlannedSet)
							ItemStartPlannedSet(cItemPrevious.nID, dtStart);   //cItemPrevious.dtStartPlanned
					}
					if (null != ItemTimingsUpdateSet)
						ItemTimingsUpdateSet(cItemPrevious.nID, dtTimingsUpdate);
					CachedRemover(cItemPrevious.nID);
					dtStart = dtStart.AddMilliseconds((1 > cItemPrevious.nDuration ? 25 * 5 : cItemPrevious.nDuration) * 40); //TODO FPS
				}
				if (0 < aqItemsSequential.Count)
					cItemCurrentSequential = aqItemsSequential.Dequeue();
				if (0 < aqItemsTimed.Count)
					cItemCurrentTimed = aqItemsTimed.Dequeue();
				if (null == cItemPrevious)
				{
					if (null != cItemCurrentTimed)
					{
						if (null != cItemCurrentSequential && cItemCurrentSequential.dtStartPlanned < cItemCurrentTimed.dtStartHardSoft)
						{
							cItemPrevious = cItemCurrentSequential;
							if (0 < aqItemsSequential.Count)
								cItemCurrentSequential = aqItemsSequential.Dequeue();
							else
								cItemCurrentSequential = null;
						}
						else
						{
							cItemPrevious = cItemCurrentTimed;
							if (0 < aqItemsTimed.Count)
								cItemCurrentTimed = aqItemsTimed.Dequeue();
							else
								cItemCurrentTimed = null;
						}
					}
					else if (null != cItemCurrentSequential)
					{
						cItemPrevious = cItemCurrentSequential;
						if (0 < aqItemsSequential.Count)
							cItemCurrentSequential = aqItemsSequential.Dequeue();
						else
							throw new Exception("can't find enough sequential items");// cItemCurrentSequential = null;
					}
					else
						throw new Exception("can't find any timed or sequential items"); //TODO LANG
					dtStart = cItemPrevious.dtStartPlanned.AddMilliseconds((1 > cItemPrevious.nDuration ? 25 * 5 : cItemPrevious.nDuration) * 40); //TODO FPS
					(new Logger()).WriteNotice("planned start:" + dtStart.ToStr() + "[id:" + cItemPrevious.nID + "]");
					if (null != ItemTimingsUpdateSet)
						ItemTimingsUpdateSet(cItemPrevious.nID, dtTimingsUpdate);
					CachedRemover(cItemPrevious.nID);
				}

				bForceAdd = false;

				if (null == cItemCurrentTimed && null == cItemCurrentSequential) // кроме локедов ничего и не было - выход
				{
					if (0 > nStartPlitemsID || tsUpdateScope == TimeSpan.MaxValue)  // пересчет был сделан успешно и с самого начала!
						dtLast = DateTime.Now;
					return true;
				}

				while (true) // на данный момент имеем dtstart - начало пересчета. cItemCurrentSequential или cItemCurrentTimed могут быть null
				{
					if (null == cItemCurrentTimed)
					{
						nDiff = 1;  // чтобы не попасть в первый if
						bForceAdd = true;
					}
					else
						nDiff = (long)(cItemCurrentTimed.dtStartHardSoft.Subtract(dtStart).TotalMilliseconds) / 40; // прогал до следующего таймеда

					if (null == cItemCurrentSequential)
						nDuration = 0;
					else
						nDuration = (long)cItemCurrentSequential.nDuration;
					(new Logger()).WriteNotice("nDiff: " + nDiff + "; nSeqDuration: " + nDuration + "; dtStart: " + dtStart.ToStr() + "; bForceAdd: " + bForceAdd + "; nDurMin: " + Playlist.Preferences.nDurationMinimum + "; nDurClipMin: " + Playlist.Preferences.nDurationClipMinimum);


					if (0 >= nDiff || (nDuration == 0 && DateTime.MaxValue == cItemCurrentTimed.dtStartHard) || ItemsAreInBlock(cItemPrevious, cItemCurrentTimed))
					{// прогал от текущего места до ближайшей рекламы исчерпался, либо клипов вообще нет и следующий таймед - это софт или предыдущий был таймедом в блоке со следующим таймедом (1 сек разницы), если он есть
						// тогда спокойно ставим таймед
						if (DateTime.MaxValue > cItemCurrentTimed.dtStartHard && !ItemIsTimmed(cItemPrevious)) // даже хард не может подрезать таймеда
						{
							dtStart = cItemCurrentTimed.dtStartHard;
							(new Logger()).WriteNotice("0 >= nDiff: hard: " + cItemCurrentTimed.dtStartPlanned.ToStr());
						}
						else
							(new Logger()).WriteNotice("0 >= nDiff: soft: " + cItemCurrentTimed.dtStartPlanned.ToStr());
						cItemCurrentTimed.dtStartPlanned = dtStart;
						dtStart = dtStart.AddMilliseconds(cItemCurrentTimed.nDuration * 40);
						cItemPrevious = cItemCurrentTimed;
						if (0 < aqItemsTimed.Count)
						{
							cItemCurrentTimed = aqItemsTimed.Dequeue();
						}
						else
						{
							cItemCurrentTimed = null;
							(new Logger()).WriteNotice("cItemCurrentTimed = null");
						}
					}
					else if (!bForceAdd && DateTime.MaxValue > cItemCurrentTimed.dtStartHard && Playlist.Preferences.nDurationMinimum > (ulong)(40 * nDiff))
					{// если есть таймеды и есть зазор, но меньше минимума даже для плагов и следующий таймед - это хард
						// то спокойно ставим хард на текущее место, т.к. чёрное поле и микропланы еще хуже
						cItemCurrentTimed.dtStartPlanned = dtStart;
						dtStart = dtStart.AddMilliseconds(cItemCurrentTimed.nDuration * 40);
						cItemPrevious = cItemCurrentTimed;
						(new Logger()).WriteNotice("minimum > nDiff: hard: " + cItemCurrentTimed.dtStartPlanned.ToStr());
						if (0 < aqItemsTimed.Count)
						{
							cItemCurrentTimed = aqItemsTimed.Dequeue();
						}
						else
						{
							cItemCurrentTimed = null;
							(new Logger()).WriteNotice("cItemCurrentTimed = null");
						}
					}
					else if (bForceAdd || DateTime.MaxValue == cItemCurrentTimed.dtStartHard || 0 < nDuration && nDiff >= nDuration && Playlist.Preferences.nDurationMinimum < (ulong)(40 * (nDiff - nDuration)))
					{ // или у нас вообще нет таймедов или если следующий таймед - это софт (и время его еще не пришло) или прогал после вставки клипа будет достаточным для плага
						// тогда спокойно вставляем клип
						(new Logger()).WriteNotice("sequential: " + cItemCurrentSequential.dtStartPlanned.ToStr());
						cItemCurrentSequential.dtStartPlanned = dtStart;
						if (null != ItemFrameStopRestore)
							ItemFrameStopRestore(cItemCurrentSequential.nID);
						dtStart = dtStart.AddMilliseconds(cItemCurrentSequential.nDuration * 40);
						cItemPrevious = cItemCurrentSequential;
						if (0 < aqItemsSequential.Count)
							cItemCurrentSequential = aqItemsSequential.Dequeue();
						else
						{
							cItemCurrentSequential = null;
							(new Logger()).WriteNotice("cItemCurrentSequential = null");
						}
						//EXIT WHEN NOT FOUND;
					}
					else if (DateTime.MaxValue > cItemCurrentTimed.dtStartHard && (long)Playlist.Preferences.nDurationClipMinimum < 40 * nDiff && nDuration > nDiff)
					{// следующий таймед - это хард (и время его еще не пришло) и прогал меньше клипа, но достаточен для обрезки (2'30")
						// тогда вставляем клип в обрез
						(new Logger()).WriteNotice("cutted sequential before hard: " + cItemCurrentSequential.dtStartPlanned.ToStr());
						cItemCurrentSequential.dtStartPlanned = dtStart;
						if (null != ItemFrameStopChange)
							ItemFrameStopChange(cItemCurrentSequential.nID, cItemCurrentSequential.nFrameStop, cItemCurrentSequential.nFrameStart + nDiff);
						dtStart = dtStart.AddMilliseconds(nDiff * 40);
						cItemPrevious = cItemCurrentSequential;
						if (0 < aqItemsSequential.Count)
							cItemCurrentSequential = aqItemsSequential.Dequeue();
						else
						{
							cItemCurrentSequential = null;
							(new Logger()).WriteNotice("cItemCurrentSequential = null");
						}
						//EXIT WHEN NOT FOUND;
					}
					else if (DateTime.MaxValue > cItemCurrentTimed.dtStartHard && (nDuration == 0 || (long)Playlist.Preferences.nDurationClipMinimum >= 40 * nDiff || Playlist.Preferences.nDurationMinimum >= (ulong)(40 * (nDiff - nDuration))) && !cItemPrevious.bPlug)
					{// следующий таймед - это хард (и время его еще не пришло) и либо нет вообще клипов, либо прогал недостаточен для вставки клипа в обрез, либо клип влезает, но после него уже ничего не вставишь и предыдущий в ПЛ не был плагом
						// тогда вставляем плаг
						(new Logger()).WriteNotice("plug insert before hard: aqPlugs.Count: " + aqPlugs.Count + "][dtStart: "+"");
						if (0 < aqPlugs.Count)
						{
							if (null != PlugUpdate)
								PlugUpdate(aqPlugs.Dequeue().nID, dtStart, nDiff, dtTimingsUpdate);
						}
						else if (null != PlugAdd)
							PlugAdd(dtStart, nDiff, dtTimingsUpdate);
						dtStart = dtStart.AddMilliseconds(nDiff * 40);
						tsUpdateScope = tsUpdateScope < TimeSpan.MaxValue ? tsUpdateScope.Subtract(TimeSpan.FromMilliseconds(nDiff * 40)) : TimeSpan.MaxValue;
						continue;
					}
					else
					{
						throw new Exception("break because else. maybe hard will start earlier or later.");
					}


					bForceAdd = false;
					(new Logger()).WriteNotice("update:[id:" + cItemPrevious.nID + "][sp:" + cItemPrevious.dtStartPlanned.ToStr() + "][next:" + dtStart.ToStr() + "]");
					_nPROBA_ID_WHEN_EXCEPTION = cItemPrevious.nID; _dtPROBA_ID_WHEN_EXCEPTION = cItemPrevious.dtStartPlanned;
					if (null != ItemStartPlannedSet)
						ItemStartPlannedSet(cItemPrevious.nID, cItemPrevious.dtStartPlanned);
					if (null != ItemTimingsUpdateSet)
						ItemTimingsUpdateSet(cItemPrevious.nID, dtTimingsUpdate);
					CachedRemover(cItemPrevious.nID);
					if (0 > tsUpdateScope.TotalSeconds && (ItemIsSequential(cItemPrevious) || !ItemsAreInBlock(cItemPrevious, cItemCurrentTimed)))
					{// диапазон пересчета кончился и можно прервать (либо после клипа, либо после конца блока)
						string sS;
						if (ItemIsSequential(cItemPrevious))
							sS = "sequential";
						else
							sS = "timed";
						(new Logger()).WriteNotice("break because tsUpdateScope after: " + sS);
						break;
					}
					if (null == cItemCurrentTimed && null == cItemCurrentSequential)
					{
						(new Logger()).WriteNotice("break because ending arrays");
						break;
					}
					tsUpdateScope = tsUpdateScope < TimeSpan.MaxValue ? tsUpdateScope.Subtract(TimeSpan.FromMilliseconds(cItemPrevious.nDuration * 40)) : TimeSpan.MaxValue;
				}
				// cItemPrevious - последний элемент пересчитанной части ПЛ, а  dtStart - его конец


				if (null != ItemRemove)   
				{
					PlaylistItem cPLIPlug;  // что б не сбить значение cItemCurrentSequential....
					while (0 < aqPlugs.Count)
					{
						(new Logger()).WriteNotice("------------plugs remove");
						cPLIPlug = aqPlugs.Dequeue();
						if (cItemPrevious.dtStartPlanned > cPLIPlug.dtStartPlanned) // при пересчете части ПЛ мы удаляем плаги только внутри диапазона
						{
							(new Logger()).WriteNotice("plug remove: " + cPLIPlug.nID);
							ItemRemove(cPLIPlug.nID);
						}
					}
				}
				if (tsUpdateScope < TimeSpan.MaxValue)  // если пересчитали диапазон, а не весь ПЛ...
				{   // чтобы плеер не сосал по пленнеду непересчитанные сейчас айтемы надо их принудительно опустить по плейлисту....
					// время конца = dtStart      // тайминги не пишем, т.к. мы их не пересчитывали, а только вышвырнули за диапазон пересчета....
					// добавка: выкидывать надо не start + .01 , + .02 ....  (start - 1sec) + .01 ....  иначе интерферирует с сиквенсом, у которого sp=stop...
					List<PlaylistItem> aPLIsPlanned = new List<PlaylistItem>();
					IEnumerable<PlaylistItem> aTMP;
					if (null != cItemCurrentSequential)
						aPLIsPlanned.Add(cItemCurrentSequential); 
					if (null != (aTMP = aqItemsSequential.Where(o => o.dtStartPlanned < dtStart)))
						aPLIsPlanned.AddRange(aTMP);
					if (null != cItemCurrentTimed)
						aPLIsPlanned.Add(cItemCurrentTimed);
					if (null != (aTMP = aqItemsTimed.Where(o => o.dtStartPlanned < dtStart)))
						aPLIsPlanned.AddRange(aTMP);
					aPLIsPlanned = aPLIsPlanned.Where(o => o != null).OrderBy(o => o.dtStartPlanned).ToList();
					if (0 < aPLIsPlanned.Count)
						(new Logger()).WriteNotice("Moving [" + aPLIsPlanned.Count + "] PLItems to outside the range of reculculation. Perhaps, the total recalculation is needed.");
					int nI = 0;

					foreach (PlaylistItem cPLI in aPLIsPlanned)
					{
						nI += 10;
						if (null != ItemStartPlannedSet && cPLI.dtStartPlanned <= dtStart)
						{
							(new Logger()).WriteNotice("update:" + dtStart.AddMilliseconds(nI - 1000).ToStr() + "[id:" + cPLI.nID + "]");
							ItemStartPlannedSet(cPLI.nID, dtStart.AddMilliseconds(nI - 1000));
						}
					}
				}

				// теперь всё пересчитывается в основном цикле
				//if (1 > aqItemsSequential.Count)  //при частичном тоже надо это делать  // пересчитали уже все секвентиалы - осталась одна реклама. на всякий сл. проставим ей пленнеды
				//{
				//    PlaylistItem cTimedPrevious = null;
				//    while (null != cItemCurrentTimed)
				//    {
				//        if (null == cTimedPrevious)
				//        {
				//            dtStart = cItemCurrentSequential.dtStartPlanned.AddMilliseconds(40 * cItemCurrentSequential.nDuration);
				//            if (cItemCurrentTimed.dtStartHard < DateTime.MaxValue && cItemCurrentTimed.dtStartHard < dtStart)
				//                dtStart = cItemCurrentTimed.dtStartHard;
				//            tsUpdateScope.Subtract(TimeSpan.FromMilliseconds(40 * cItemCurrentSequential.nDuration));
				//        }
				//        else
				//        {
				//            dtStart = cTimedPrevious.dtStartPlanned.AddMilliseconds(40 * cTimedPrevious.nDuration);
				//            tsUpdateScope.Subtract(TimeSpan.FromMilliseconds(40 * cTimedPrevious.nDuration));
				//            if (ItemsHardSoft(cItemCurrentTimed).Subtract(ItemsHardSoft(cTimedPrevious)).TotalSeconds > 1 && tsUpdateScope.TotalSeconds < 1)
				//            {
				//                (new Logger()).WriteNotice("end timed update: break because tsUpdateScope");
				//                break;
				//            }
				//        }
				//        cItemCurrentTimed.dtStartPlanned = dtStart;

				//        (new Logger()).WriteNotice("end timed update:" + dtStart.ToStr() + "[id:" + cItemCurrentTimed.nID + "]");
				//        if (null != ItemStartPlannedSet)
				//            ItemStartPlannedSet(cItemCurrentTimed.nID, dtStart);
				//        if (null != ItemTimingsUpdateSet)
				//            ItemTimingsUpdateSet(cItemCurrentTimed.nID, dtTimingsUpdate);
				//        if (1 > aqItemsTimed.Count)
				//            break;
				//        cTimedPrevious = cItemCurrentTimed;
				//        cItemCurrentTimed = aqItemsTimed.Dequeue();
				//    }
				//}


				if (bRetVal && (0 > nStartPlitemsID || tsUpdateScope == TimeSpan.MaxValue))  // пересчет был сделан успешно и с самого начала!
					dtLast = DateTime.Now;
				(new Logger()).WriteNotice("@@@@@@@@  the end  @@@@@@@@");
				return bRetVal;
			}
		}
	}
}
