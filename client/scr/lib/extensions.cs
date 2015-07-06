using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using scr.services.preferences;
using helpers.replica.services.dbinteract;
using DBI=helpers.replica.services.dbinteract;
using scr.services.ingenie.player;
using IP = scr.services.ingenie.player;
using scr.services.ingenie.cues;
using IC = scr.services.ingenie.cues;
using controls.replica.sl;
using helpers.extensions;

namespace scr
{
	static public class x
	{
		static public scr.services.preferences.Template Get(this scr.services.preferences.Template[] aTemplates, Bind eBind)
		{
			return aTemplates.FirstOrDefault(o => o.eBind == eBind);
		}
		static public scr.services.preferences.Parameters Get(this scr.services.preferences.Parameters[] aParameters, long nPresetID)
		{
			return aParameters.FirstOrDefault(o => o.nPresetID == nPresetID);
		}
		static public scr.services.preferences.Offset[] Get(this scr.services.preferences.Offset[] aOffsets, long nPresetID)
		{
			return aOffsets.Where(o => o.nPresetID == nPresetID).ToArray();
		}
		static public bool IsOffsetFeats(this scr.services.preferences.Offset cOffset, long nPresetID, LivePLItem cPLI, LivePLItem cNextPLI, LivePLItem cPreviousPLI)
		{
			if (cOffset.nPresetID != nPresetID)
				return false;
			if (null != cOffset.sType && (null==cPLI || cOffset.sType.ToLower() != cPLI.eType.ToString().ToLower()))
				return false;
			if (null != cOffset.sClass && (null==cPLI || cOffset.sClass.ToLower() != cPLI.sClassName.ToLower()))
				return false;
			if (null != cOffset.sNextType && (null==cNextPLI || cOffset.sNextType.ToLower() != cNextPLI.eType.ToString().ToLower()))
				return false;
			if (null != cOffset.sNextClass && (null == cNextPLI || cOffset.sNextClass.ToLower() != cNextPLI.sClassName.ToLower()))
				return false;
			if (null != cOffset.sPreType && (null == cPreviousPLI || cOffset.sPreType.ToLower() != cPreviousPLI.eType.ToString().ToLower()))
				return false;
			if (null != cOffset.sPreClass && (null == cPreviousPLI || cOffset.sPreClass.ToLower() != cPreviousPLI.sClassName.ToLower()))
				return false;
			return true;
		}
		static public scr.services.preferences.Template[] GetAutomateNeeded(this scr.services.preferences.Template[] aTemplates, long nPresetID, LivePLItem cPLI, LivePLItem cNextPLI, LivePLItem cPreviousPLI)
		{
			return aTemplates.Where
				(
					o => 
					o.aOffsets != null 
					&& o.aOffsets.Get(nPresetID).FirstOrDefault
					(
						oo => oo.IsOffsetFeats(nPresetID, cPLI, cNextPLI, cPreviousPLI)
					) != null
				).ToArray();
		}
		static public Item[] Translate(this IC.Item[] aItems)
		{
			return aItems.Select(o => (Item)o).ToArray();
		}
		static public Item[] Translate(this IP.Item[] aItems)
		{
			return aItems.Select(o => (Item)o).ToArray();
		}
		static public bool IsItConflicts(this scr.services.preferences.Template cTemp, long nPresetID, Bind eBind)
		{
			if (null == cTemp.aConflicts || 0 == cTemp.aConflicts.Length)
				return false;
			foreach (Conflict cC in cTemp.aConflicts)
				if (cC.nPresetID == nPresetID && cC.eBind == eBind)
					return true;
			return false;
		}
		public static void Kill(this Item cItem)
		{
			Item.Kill(cItem);
		}
	}
	#region services types casts
	public class Item : TemplateButton.Item
	{
		public string sCurrentClass;

		private static Item Get(ulong nID, string sInfo)
		{ 
			Item cRetVal;
			if (null == (cRetVal = (Item)aItems.FirstOrDefault(o => o.nID == nID && o.sInfo == sInfo)))
			{
				cRetVal = new Item();
				cRetVal.nID = nID;
				cRetVal.sInfo = sInfo;
				aItems.Add(cRetVal);
			}
			return cRetVal;
		}
		public static void Kill(Item cItem)
		{
			if (aItems.Contains(cItem))
				aItems.Remove(cItem);
		}

		public static implicit operator Item(IC.Item cItem)
		{
			if (null == cItem)
				return null;
			Item cRetVal = Item.Get(cItem.nID, cItem.sInfo);
			cRetVal.eStatus = cItem.eStatus.To<TemplateButton.Status>();
			cRetVal.sPreset = cItem.sPreset;
			return cRetVal;
		}
		public static implicit operator Item(IP.Item cItem)
		{
			if (null == cItem)
				return null;
			Item cRetVal = Item.Get(cItem.nID, cItem.sInfo);
			cRetVal.eStatus = cItem.eStatus.To<TemplateButton.Status>();
			cRetVal.sPreset = cItem.sPreset;
			return cRetVal;
		}
		public static implicit operator IC.Item(Item cItem)
		{
			if (null == cItem)
				return null;
			return new IC.Template() { nID = cItem.nID, eStatus = cItem.eStatus.To<IC.Status>(), sPreset = cItem.sPreset, sInfo = cItem.sInfo };
		}
		public static implicit operator IP.Item(Item cItem)
		{
			if (null == cItem)
				return null;
			return new IP.Playlist() { nID = cItem.nID, eStatus = cItem.eStatus.To<IP.Status>(), sCurrentClass = cItem.sCurrentClass, sPreset = cItem.sPreset, sInfo = cItem.sInfo };
		}
		static protected Item Get(ulong nID)
		{
			Item cRetVal;
			if (null == (cRetVal = (Item)aItems.FirstOrDefault(o => o.nID == nID)))
			{
				cRetVal = new Item();
				cRetVal.nID = nID;
				aItems.Add(cRetVal);
			}
			return cRetVal;
		}
	}
	public class IdNamePair : DBI.IdNamePair
	{
		public static implicit operator IdNamePair(services.preferences.IdNamePair cINP)
		{
			return new IdNamePair() { nID = cINP.nID, sName = cINP.sName };
		}
	}
	#endregion
}
