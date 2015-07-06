#define DEBUG9
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

using helpers;

namespace webservice.services
{
	public class SessionInfo
	{
		//static 
		public class StoreAtom
		{
			public DateTime dt;
			private Dictionary<string, Dictionary<string, object>> _ahStoreValues;
			private string _sSeed;

			public StoreAtom(string sSeed)
			{
				dt = DateTime.Now;
				_ahStoreValues = new Dictionary<string, Dictionary<string, object>>();
				_sSeed = sSeed;
			}
            ~StoreAtom()
            {
                if (Logger.Level.debug4 < Logger.eLevelMinimum)
                    return;
                string sRez = " ~StoreAtom(): StackTrace";
                StackTrace st = new StackTrace(true);
                for (int i = 0; i < st.FrameCount; i++)
                {
                    StackFrame sf = st.GetFrame(i);
                    sRez += "/n/r/t[type " + sf.GetType() + "] [filename " + sf.GetFileName() + "] [method " + sf.GetMethod() + "] [line " + sf.GetFileLineNumber() + "]";
                }
                (new Logger()).WriteDebug4(sRez);
            }

			public object ValueGet(string sValueName)
			{
				if (_ahStoreValues.ContainsKey(_sSeed) && null != _ahStoreValues[_sSeed] && _ahStoreValues[_sSeed].ContainsKey(sValueName))
					return _ahStoreValues[_sSeed][sValueName];
				return null;
			}
			public void ValueSet(string sValueName, object cValue)
			{
				if (!_ahStoreValues.ContainsKey(_sSeed))
					_ahStoreValues[_sSeed] = new Dictionary<string,object>();
				if (_ahStoreValues[_sSeed].ContainsKey(sValueName))
					_ahStoreValues[_sSeed][sValueName] = cValue;
				else
					_ahStoreValues[_sSeed].Add(sValueName, cValue);
			}
		}

		static private Dictionary<string, StoreAtom> _ahContainer;
		static private Dictionary<string, string> _ahSessionIDsReformingBinds;

		private string _sSessionID;
		private string _sSeed;

		protected StoreAtom _cStoreAtom
		{
			get
			{
				if (null == _sSessionID)
					return null;
				if (!_ahContainer.ContainsKey(_sSessionID))
					_ahContainer.Add(_sSessionID, new StoreAtom(_sSeed));
				try
				{
					HttpContext.Current.Session["replica"] = true;
					SessionIDsReform();
				}
				catch { }
				return _ahContainer[_sSessionID];
			}
			set
			{
				if (!_ahContainer.ContainsKey(_sSessionID))
					_ahContainer.Add(_sSessionID, value);
				else
					_ahContainer[_sSessionID] = value;
				try
				{
					HttpContext.Current.Session["replica"] = true;
					SessionIDsReform();
				}
				catch { }
			}
		}
		public SessionInfo(string sSeed)
		{
			(new Logger()).WriteDebug3("in");
			if (null == HttpContext.Current.Request || null == HttpContext.Current.Request.Cookies || null == HttpContext.Current.Request.Cookies["ASP.NET_SessionId"])
			{
				try
				{
					System.Web.Services.WebService cWS = new System.Web.Services.WebService();
					if(null == cWS.Session)
					{
						(new Logger()).WriteDebug4("session object is null");
						throw new MemberAccessException();
					}
					_sSessionID = cWS.Session.SessionID;
				}
				catch (Exception ex)
				{
					if(!(ex is MemberAccessException))
						(new Logger()).WriteError(ex);
					throw new Exception("session object is null");
				}
			}
			else
				_sSessionID = HttpContext.Current.Request.Cookies["ASP.NET_SessionId"].Value;

			_sSeed = sSeed;
			(new Logger()).WriteDebug4("[sid:" + _sSessionID + "][seed:" + _sSeed + "]");

			if (null == _ahSessionIDsReformingBinds)
				_ahSessionIDsReformingBinds = new Dictionary<string, string>();
			else if (_ahSessionIDsReformingBinds.ContainsKey(_sSessionID))
				_sSessionID = _ahSessionIDsReformingBinds[_sSessionID];

			if (null != _ahContainer)
			{
				StoreAtom cSIV = null;
				string[] aKeys = _ahContainer.Keys.ToArray();
				foreach (string sSessionKey in aKeys)
				{
					cSIV = _ahContainer[sSessionKey];
					if (25 < DateTime.Now.Subtract(cSIV.dt).TotalHours)
						_ahContainer.Remove(sSessionKey);
				}
			}
			else
				_ahContainer = new Dictionary<string, StoreAtom>();
			(new Logger()).WriteDebug4("return");
		}

		private void SessionIDsReform()
		{
			if (HttpContext.Current.Session.SessionID != _sSessionID)
			{
				_ahSessionIDsReformingBinds.Add(_sSessionID, HttpContext.Current.Session.SessionID);
				_ahContainer[HttpContext.Current.Session.SessionID] = _ahContainer[_sSessionID];
				_ahContainer.Remove(_sSessionID);
				_sSessionID = HttpContext.Current.Session.SessionID;
			}
		}

		public Queue<WebServiceError> aqErrors
		{
			get
			{
				return (Queue<WebServiceError>)_cStoreAtom.ValueGet("aqErrors");
			}
			set
			{
				_cStoreAtom.ValueSet("aqErrors", value);
			}
		}
	}
}