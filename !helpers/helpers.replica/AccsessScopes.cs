using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using helpers;
using helpers.extensions;
using System.Reflection;

namespace access.types
{
	static public class ScopesProcessor
	{
		private class Item
		{
			public string sName;
			public Item cParent;
			public types.AccessScope.Permissions ePermissions;
			public string sNameQualified
			{
				get
				{
					string sRetVal = sName;
					Item cParentNext = cParent;
					while (null != cParentNext)
					{
						sRetVal = cParentNext.sName + "." + sRetVal;
						cParentNext = cParentNext.cParent;
					}
					return sRetVal;
				}
			}

			public Item(Item cNode, string sName)
			{
				this.cParent = cNode;
				this.sName = sName;
				this.ePermissions = types.AccessScope.Permissions.read;
			}
			public Item(Item cNode, string sName, types.AccessScope.Permissions ePermissions)
				: this(cNode, sName)
			{
				this.ePermissions = ePermissions;
			}
		}
#if SILVERLIGHT
		static private List<Item> _aItems = new List<Item>();
#else
		static private Dictionary<string, List<Item>> _ahUsers = new Dictionary<string, List<Item>>();
		static private List<Item> _aItems
		{
				get
				{
					string sUser = helpers.replica.DBInteract.cCache.sUserName;
					if(!_ahUsers.ContainsKey(sUser))
						_ahUsers.Add(sUser,new List<Item>());
					return _ahUsers[sUser];
				}
				set
				{
					string sUser = helpers.replica.DBInteract.cCache.sUserName;
					_ahUsers[sUser] = value;
				}
		}
#endif
		static public void init(types.AccessScope[] aData)
		{
			string sName = null;
			int nDotPosition = 0;
			Item cItem = null, cCheckItem = null, cNode = null;
			List<Item> aItemsInitiated = new List<Item>();
			foreach (types.AccessScope cData in aData)
			{
				sName = cData.sNameQualified;
				cNode = null;
				while (-1 < (nDotPosition = sName.IndexOf('.')))
				{
					cCheckItem = new Item(cNode, sName.Substring(0, nDotPosition));
					if (null == (cNode = _aItems.FirstOrDefault(row => cCheckItem.sNameQualified == row.sNameQualified)))
					{
						cNode = cCheckItem;
						_aItems.Add(cNode);
					}
					sName = sName.Substring(nDotPosition + 1);
				}
				cCheckItem = new Item(cNode, sName, cData.ePermissions);
				if (null == (cItem = _aItems.FirstOrDefault(row => cCheckItem.sNameQualified == row.sNameQualified)))
				{
					cItem = cCheckItem;
					_aItems.Add(cItem);
				}
				else
					cItem.ePermissions |= cCheckItem.ePermissions;
				aItemsInitiated.Add(cItem);
			}
			_aItems = aItemsInitiated;
			_aItems = ItemsChildGet(null).ToList<Item>();
		}
		static private Item[] ItemsChildGet(Item cNode)
		{
			List<Item> aRetVal = new List<Item>();
			Item cItem = null;
			while (null != (cItem = _aItems.FirstOrDefault(row => !aRetVal.Contains(row) && cNode == row.cParent)))
			{
				aRetVal.Add(cItem);
				aRetVal.AddRange(ItemsChildGet(cItem));
			}
			return aRetVal.ToArray();
		}

		static private bool CanDo(string sNameQualified, types.AccessScope.Permissions ePermissions)
		{
			Item cItem = null;
			if (null != _aItems)
				cItem = _aItems.FirstOrDefault(row => sNameQualified == row.sNameQualified);
			if (null != cItem && 0 < (cItem.ePermissions & ePermissions))
				return true;
			return false;
		}
		static public bool CanRead(string sNameQualified)
		{
			bool bRetVal = false;
			if (null != _aItems)
				bRetVal = (0 < _aItems.Count(row => sNameQualified == row.sNameQualified));
			return bRetVal;
		}
		static public bool CanCreate(string sNameQualified)
		{
			return CanDo(sNameQualified, types.AccessScope.Permissions.create);
		}
		static public bool CanUpdate(string sNameQualified)
		{
			return CanDo(sNameQualified, types.AccessScope.Permissions.update);
		}
		static public bool CanDelete(string sNameQualified)
		{
			return CanDo(sNameQualified, types.AccessScope.Permissions.delete);
		}
	}
	public class ITEM
	{
		private string sNameFull
		{
			get
			{
				Type t = GetType();
				string sRetVal = t.Name;
				while (null != t.DeclaringType)
				{
					t = t.DeclaringType;
					sRetVal = t.Name + "." + sRetVal;
				}
				return sRetVal.ToLower();
			}
		}
		public bool bCanRead
		{
			get
			{
				return types.ScopesProcessor.CanRead(sNameFull);
			}
		}
		public bool bCanCreate
		{
			get
			{
				return types.ScopesProcessor.CanCreate(sNameFull);
			}
		}
		public bool bCanUpdate
		{
			get
			{
				return types.ScopesProcessor.CanUpdate(sNameFull);
			}
		}
		public bool bCanDelete
		{
			get
			{
				return types.ScopesProcessor.CanDelete(sNameFull);
			}
		}

		protected ITEM()
		{
		}
	}

	public class PROFILE : ITEM
	{
	}

	public class INGEST : ITEM
	{
	}
	public class PLAYLIST : ITEM
	{
	}
	public class ASSETS : ITEM
	{
		public class NAME : ITEM
		{
		}
		public class CLASSES : ITEM
		{
		}
		public class FILE : ITEM
		{
		}
		public class CUSTOM_VALUES : ITEM
		{
		}

		public NAME name = new NAME();
		public CLASSES classes = new CLASSES(); //пришлось сделать множественное число потому что class зарезервированное слово
		public FILE file = new FILE();
		public CUSTOM_VALUES custom_values = new CUSTOM_VALUES();
	}
	public class STAT : ITEM
	{
	}
	public class SMS : ITEM
	{
	}
	public class RT : ITEM
	{
	}
	public class CLIPS : ASSETS //UNDONE
	{
	}
	public class ADVERTISEMENTS : ASSETS //UNDONE
	{
	}
	public class PROGRAMS : ASSETS
	{
		public class CHATINOUTS : ITEM
		{
		}
		public class CLIPS : ITEM
		{
		}

		public CHATINOUTS chatinouts = new CHATINOUTS();
		public CLIPS clips = new CLIPS();
	}
	public class DESIGNS : ASSETS //UNDONE
	{
	}
	[XmlType("AccessScope")]
	public class AccessScope
	{
		[Flags]
		[XmlType("AccessScopePermissions")]
		public enum Permissions
		{
			[XmlIgnore]
			read = 0,
			create = 1,
			update = 2,
			delete = 4
		}
		public string sNameQualified;
		public Permissions ePermissions;

		public AccessScope()
		{
			sNameQualified = null;
			ePermissions = Permissions.read;
		}
		public AccessScope(string sNameQualified, bool bCreate, bool bUpdate, bool bDelete)
			: this()
		{
			this.sNameQualified = sNameQualified;
			if (bCreate || bUpdate || bDelete)
			{
				if (bCreate)
					ePermissions |= Permissions.create;
				if (bUpdate)
					ePermissions |= Permissions.update;
				if (bDelete)
					ePermissions |= Permissions.delete;
			}
		}
#if SILVERLIGHT
		public AccessScope(helpers.replica.services.dbinteract.AccessScope cData)
		{
			sNameQualified = cData.sNameQualified;
			ePermissions = (Permissions)(cData.ePermissions & helpers.replica.services.dbinteract.AccessScopePermissions.create);
			ePermissions |= (Permissions)(cData.ePermissions & helpers.replica.services.dbinteract.AccessScopePermissions.update);
			ePermissions |= (Permissions)(cData.ePermissions & helpers.replica.services.dbinteract.AccessScopePermissions.delete);
		}
#else
		public AccessScope(Hashtable ahRow)
		{
			sNameQualified = ahRow["sAccessScopeNameQualified"].ToString();
			ePermissions = Permissions.read;
			if (ahRow["bCreate"].ToBool())
				ePermissions |= Permissions.create;
			if (ahRow["bUpdate"].ToBool())
				ePermissions |= Permissions.update;
			if (ahRow["bDelete"].ToBool())
				ePermissions |= Permissions.delete;
		}
#endif
	}
}
namespace access
{
	static public class scopes
	{
		static public types.PROFILE profile = null;

		static public types.INGEST ingest = null;
		static public types.PLAYLIST playlist = null;
		static public types.ASSETS assets = null;
		static public types.STAT stat = null;
		static public types.SMS sms = null;
		static public types.RT rt = null;

		static public types.CLIPS clips = null;
		static public types.ADVERTISEMENTS advertisements = null;
		static public types.PROGRAMS programs = null;
		static public types.DESIGNS designs = null;
#if SILVERLIGHT
		static public void init(helpers.replica.services.dbinteract.AccessScope[] aDBIAccessScopes)
		{
			types.AccessScope[] aAccessScopes = new types.AccessScope[aDBIAccessScopes.Length];
			for (int nIndx = 0; aDBIAccessScopes.Length > nIndx; nIndx++)
				aAccessScopes[nIndx] = new types.AccessScope(aDBIAccessScopes[nIndx]);
			init(aAccessScopes);
		}
#endif
		static public void init(types.AccessScope[] aAccessScopes)
		{
			types.ScopesProcessor.init(aAccessScopes);

			profile = new types.PROFILE();
			ingest = new types.INGEST();
			playlist = new types.PLAYLIST();
			assets = new types.ASSETS();
			stat = new types.STAT();
			sms = new types.SMS();
			rt = new types.RT();

			clips = new types.CLIPS();
			advertisements = new types.ADVERTISEMENTS();
			programs = new types.PROGRAMS();
			designs = new types.DESIGNS();
		}
		static public bool IsWebPageVisible(string sWebPage)
		{
			if (sWebPage == "msadv" || sWebPage == "artist_search")
				return true;
			return types.ScopesProcessor.CanRead(sWebPage);
		}
	}
}
