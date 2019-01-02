using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace webservice.services
{
	public class WebServiceError
	{
		public DateTime dt;
		public string sMessage;
		public string sStackTrace;
		public WebServiceError cInnerError;

		public WebServiceError()
		{
			dt = DateTime.Now;
			sMessage = null;
			sStackTrace = null;
			cInnerError = null;
		}
		public WebServiceError(Exception ex)
			: this()
		{
			sMessage = ex.Message;
			sStackTrace = ex.StackTrace;
			if (null != ex.InnerException)
				cInnerError = new WebServiceError(ex.InnerException);
		}
		#region errors handling

		static private Queue<WebServiceError> _aqServerErrors;

		static public bool IsThereAny(SessionInfo cSI)
		{
			if (null != cSI && null != cSI.aqErrors && 0 < cSI.aqErrors.Count)
				return true;
			return false;
		}

		static public void Clear(SessionInfo cSI)
		{
			if (null != cSI && null != cSI.aqErrors && 0 < cSI.aqErrors.Count)
				cSI.aqErrors.Clear();
		}

		static public WebServiceError[] Get(SessionInfo cSI)
		{
			WebServiceError[] aRetVal = null;
			if (null != cSI)
			{
				if (null != cSI.aqErrors)
					aRetVal = cSI.aqErrors.ToArray();
			}
			else
				aRetVal = _aqServerErrors.ToArray();

			if (null == aRetVal)
					aRetVal = new WebServiceError[0];
			return aRetVal;
		}

		static public WebServiceError[] Get()
		{
			return Get(null);
		}

		static public WebServiceError LastGet(SessionInfo cSI)
		{
			WebServiceError[] aErrors = Get(cSI);
			if (0 < aErrors.Length)
				return aErrors[aErrors.Length - 1];
			return null;
		}
		static public void Add(SessionInfo cSI, Exception ex)
		{
			Add(cSI, ex, false);
		}
        static public void Add(SessionInfo cSI, Exception ex, bool bDoNotSend)
        {
            Add(cSI, ex, null, bDoNotSend);
        }
        static public void Add(SessionInfo cSI, Exception ex, string sAdditionalInfo)
        {
            Add(cSI, ex, sAdditionalInfo, false);
        }
        static public void Add(SessionInfo cSI, Exception ex, string sAdditionalInfo, bool bDoNotSend)
        {
            sAdditionalInfo = sAdditionalInfo == null ? "" : sAdditionalInfo + " ";
            if (bDoNotSend)
                (new Logger()).WriteNotice("error: " + sAdditionalInfo + ex.ToString());
            else
                (new Logger()).WriteError(sAdditionalInfo, ex);
			WebServiceError cError = new WebServiceError(ex);
			if (null == _aqServerErrors)
				_aqServerErrors = new Queue<WebServiceError>();
			_aqServerErrors.Enqueue(cError);

			if (null != cSI)
			{
				if (null == cSI.aqErrors)
					cSI.aqErrors = new Queue<WebServiceError>();
				cSI.aqErrors.Enqueue(cError);
			}
		}
		static public void Add(Exception ex)
		{
			Add(null, ex);
		}
		static public void Add(SessionInfo cSI, string sMessage)
		{
			Add(cSI, sMessage, false);
		}
		static public void Add(SessionInfo cSI, string sMessage, bool bDoNotSend)
		{
			Add(cSI, new Exception(sMessage), bDoNotSend);
		}
		static public void Add(string sMessage)
		{
			Add(null, new Exception(sMessage));
		}
		#endregion
	}
}