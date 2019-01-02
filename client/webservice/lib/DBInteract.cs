using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using helpers;
using helpers.extensions;
using helpers.replica.mam;
using cues=helpers.replica.cues;
using helpers.replica.pl;
using helpers.replica.scr;
using System.Linq;

using g = globalization;

namespace webservice
{
	public class DBInteract : helpers.replica.DBInteract
	{
        public class Message
		{
			private helpers.replica.ia.Message _cMessage;
			public long nID
			{
				get
				{
					return _cMessage.nID;
				}
				set
				{
                    throw new NotImplementedException();
                }
			}
			public ulong nSourceNumber
			{
				get
				{
					return _cMessage.nSourceNumber;
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			public ulong nTargetNumber
			{
				get
				{
					return _cMessage.nTargetNumber;
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			public string sText
			{
				get
				{
					return _cMessage.sText;
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			public string sBindID
			{
				get
				{
					return _cMessage.sBindID;
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			public string sRegisterDT
			{
				get
				{
					return _cMessage.dtRegister.ToString("yyyy-MM-dd HH:mm:ss");
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			public string sDisplayDT
			{
				get
				{
					return _cMessage.dtDisplay.ToString("yyyy-MM-dd HH:mm:ss");
				}
				set
				{
					throw new NotImplementedException();
				}
			}
			public bool bMark {get; set;}
			public Message()
			{
				_cMessage = null;
				bMark = false;
			}
			public Message(Hashtable ahRow)
			{
				this.bMark = ahRow["bMark"].ToBool();
				_cMessage = new helpers.replica.ia.Message(ahRow);
			}
		}

		public DBInteract()
		{
			_cDB = new DB();
		}
		public DBInteract(string sName, string sPassword)
			: this()
		{
			_cDB.CredentialsSet(Preferences.DBCredentialsGet(sName, sPassword));
		}

        #region ia
		public Message[] MessagesQueueGet()
		{
			Message[] aRetVal = null;

			//_cDB.RoleSet("replica_scr");
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT DISTINCT * FROM scr.`vMessagesQueue`", null, "`bMark` DESC, `dtRegister` DESC", 0, 0, null);
			//_cDB.RoleReset();
			if (null != aqDBValues)
			{
				aRetVal = new Message[aqDBValues.Count];
				int nIndx = 0;
				while (0 < aqDBValues.Count)
					aRetVal[nIndx++] = new Message(aqDBValues.Dequeue());
			}
			return aRetVal;
		}
		public Queue<helpers.replica.ia.Message> MessagesRegisteredGet(DateTime dtFrom, DateTime dtUpto, bool bDescending)
		{
            return MessagesGet(dtFrom, dtUpto, bDescending, "dtRegister");
        }
        public Queue<helpers.replica.ia.Message> MessagesDisplayedGet(DateTime dtFrom, DateTime dtUpto, bool bDescending)
        {
            return MessagesGet(dtFrom, dtUpto, bDescending, "dtDisplay");
        }
        private Queue<helpers.replica.ia.Message> MessagesGet(DateTime dtFrom, DateTime dtUpto, bool bDescending, string sDTColumn)
        {
            string sWhere = "";
            sDTColumn = "`" + sDTColumn + "`";
            if (DateTime.MaxValue > dtFrom)
                sWhere = sDTColumn + " > '" + dtFrom.ToStr() + "'";
            if (DateTime.MaxValue > dtUpto)
            {
                if (0 < sWhere.Length)
                    sWhere += " AND ";
                sWhere += sDTColumn + " < '" + dtUpto.ToStr() + "'";
            }
            return MessagesGet(sWhere, sDTColumn + (bDescending ? " DESC" : ""), 0);
        }

		public int MessageLastIDGet()
		{
			int nRetVal = -1;
			//_cDB.RoleSet("replica_scr");
			try
			{
				nRetVal = _cDB.GetValueInt("SELECT max(id) FROM ia.`tMessages`");
			}
			catch { }
			//_cDB.RoleReset();
			return nRetVal;
		}
		public int MessagesCountGet(helpers.replica.ia.Gateway.IP cGatewayIP)
		{
			int nRetVal = -1;
			try
			{
				nRetVal = _cDB.GetValueInt("SELECT `nValue` FROM ia.`fMessagesCountGet`('" + cGatewayIP.cIP.ToString() + "')");
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return nRetVal;
		}
		public int MessagesDisplayedCountGet(helpers.replica.ia.Gateway.IP cGatewayIP)
		{
			int nRetVal = -1;
			try
			{
				nRetVal = _cDB.GetValueInt("SELECT `nValue` FROM ia.`fMessagesDisplayedCountGet`('" + cGatewayIP.cIP.ToString() + "')");
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return nRetVal;
		}
        #endregion ia
        #region scr
		public Announcement[] AnnouncementsActualGet()
		{
			Announcement[] aRetVal = null;

			//_cDB.RoleSet("replica_scr");
			Queue<Hashtable> aqDBValues = _cDB.Select("SELECT * FROM scr.`vAnnouncementsActual` ORDER BY `idShifts` NULLS LAST");
			//_cDB.RoleReset();
			if (null != aqDBValues)
			{
				aRetVal = new Announcement[aqDBValues.Count];
				int nIndx = 0;
				Shift cShift = null;
				Hashtable ahRow = null;
				while (0 < aqDBValues.Count)
				{
					ahRow = aqDBValues.Dequeue();
					aRetVal[nIndx++] = new Announcement(ahRow);
					if (null != ahRow["idShifts"])
					{
						if (null == cShift)
							cShift = ShiftCurrentGet();
                        if (null != cShift && cShift.nID != ahRow["idShifts"].ToID())
                            throw new Exception(g.Webservice.sErrorDBInteract7);
						aRetVal[nIndx - 1].cShift = cShift;
					}
				}
			}
			return aRetVal;
		}
		public void MessageMark(long nID)
		{
			_cDB.Perform("SELECT scr.`fMessageMarkAdd`(" + nID + ")");
		}
		public void MessageUnMark(long nID)
		{
			_cDB.Perform("SELECT scr.`fMessageMarkRemove`(" + nID + ")");
		}

		public int MessageMarkedLastIDGet()
		{
			int nRetVal = -1;
			//_cDB.RoleSet("replica_scr");
			try
			{
				nRetVal = _cDB.GetValueInt("SELECT max(id) FROM scr.`tMessagesMarks`");
			}
			catch { }
			//_cDB.RoleReset();
			return nRetVal;
		}
		public int AnnouncementLastIDGet()
		{
			int nRetVal = -1;
			//_cDB.RoleSet("replica_scr");
			try
			{
				nRetVal = _cDB.GetValueInt("SELECT max(id) FROM scr.`tAnnouncements`");
			}
			catch { }
			//_cDB.RoleReset();
			return nRetVal;
		}
		#endregion
        #region cues
        protected Macro[] CrawlsMacrosGet()
        {
            try
            {
                //EMERGENCY:l вот от таких твоих конструкций я теряю сознание, конечно(( Ведь если делать правильно, то отталкиваться от имени нельзя никогда!!! нужно делать справочник
                return _cDB.Select("SELECT * FROM mam.`vMacros` WHERE substring(`sName` FROM 19 FOR 5)='CRAWL' ORDER BY `sName`").Select(o => new Macro(o)).ToArray();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return new Macro[0];
        }
        protected cues.Template[] MessagesTemplatesGet()
        {
            try
            {
                //EMERGENCY:l вот от таких твоих конструкций я теряю сознание, конечно(( Ведь если делать правильно, то отталкиваться от имени нельзя никогда!!! нужно делать справочник
                return _cDB.Select("SELECT * FROM cues.`tTemplates` WHERE `sName` LIKE '%Message-%'").Select(o => new cues.Template(o)).ToArray();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return new cues.Template[0];
        }
        #endregion

		public string VJMessageCurrentGet()
		{
			string sRetVal = null;
			try
			{
				sRetVal = _cDB.GetValue("SELECT `sText` FROM ia.`fVJMessageCurrentGet`()");
			}
			catch { }
			return sRetVal;
		}
		public void VJMessageAdd(string sText)
		{
			if (null != VJMessageCurrentGet())
				VJMessageCurrentStop();
			_cDB.Perform("SELECT ia.`fVJMessageAdd`('" + sText.ForDB() + "')");
		}
		public void VJMessageCurrentStop()
		{
			_cDB.Perform("SELECT ia.`fVJMessageStop`()");
		}
    }
}
