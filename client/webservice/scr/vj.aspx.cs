using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using helpers;
using helpers.extensions;
using webservice.services;
using helpers.replica.scr;

using g = globalization;

namespace webservice.scr
{
	public partial class vj : System.Web.UI.Page
	{
		abstract class Target
		{
			static public DBInteract cDBI;
			protected enum Status
			{
				none,
				finished,
				checking,
				alerted,
				updating,
				updated
			}
			public class Subject : Target
			{
				private string _sValue;

				override public string sJavaScript
				{
					get
					{
						string sRetVal = "";
						switch (_eStatus)
						{
							case Status.none:
                                sRetVal = "_sSubject='(" + g.Helper.sErrorLiveIsMissing + ")'; UpdateSubject();";
								break;
							case Status.updated:
								sRetVal = "_sSubject='";
                                if (null != _sValue && 0 < _sValue.Length)
                                    sRetVal += _sValue.ForHTML();
                                else
                                    sRetVal += "(" + g.Webservice.sNoticeVJ1 + ")";
								sRetVal += "'; UpdateSubject();";
								break;
						}
						return sRetVal;
					}
				}

				public Subject()
					: base()
				{
				}

				override public void Check()
				{
					Update();
				}
				override public void Update()
				{
					switch (_eStatus)
					{
						case Status.alerted:
						case Status.finished:
						case Status.none:
						case Status.updated:
							_eStatus = Status.checking;
							//if (System.Drawing.Color.Empty != _ui_lblSubject.BackColor)
							//{
							//    _ui_lblSubject.BackColor = System.Drawing.Color.FromArgb(255,
							//        (245 < _ui_lblSubject.BackColor.R ? 255 : _ui_lblSubject.BackColor.R + 10),
							//        (245 < _ui_lblSubject.BackColor.G ? 255 : _ui_lblSubject.BackColor.G + 10),
							//        (245 < _ui_lblSubject.BackColor.B ? 255 : _ui_lblSubject.BackColor.B + 10)
							//    );
							//    if (System.Drawing.Color.White == _ui_lblSubject.BackColor)
							//        _ui_lblSubject.BackColor = System.Drawing.Color.Empty;
							//}
							if (null == cDBI)
								cDBI = new DBInteract("vj", "vj");
							Shift cShift = null;
							lock(cDBI)
								cShift = cDBI.ShiftCurrentGet();
							if (null != cShift)
							{
								if(_sValue != cShift.sSubject)
									_eStatus = Status.updated;
								else
									_eStatus = Status.finished;
								_sValue = cShift.sSubject;
							}
							else
								_eStatus = Status.none;
							break;
					}
				}
			}
			public class Messages : Target
			{
				private DBInteract.Message[] _aMessages;

				override public string sJavaScript
				{
					get
					{
						switch (_eStatus)
						{
							case Status.alerted:
								return "AlertMessages();";
							case Status.updated:
								string sRetVal = "_aMessages = new Array(); _aMessagesMarked = new Array();";
								foreach (DBInteract.Message cMessage in _aMessages)
								{
									if (cMessage.bMark)
										sRetVal += "_aMessagesMarked[_aMessagesMarked.length]";
									else
										sRetVal += "_aMessages[_aMessages.length]";
									sRetVal += "='" + cMessage.sText.ForHTML() + "';";
								}
								sRetVal += "UpdateMessages();";
								return sRetVal;
						}
						return "";
					}
				}

				public Messages()
					: base()
				{
				}

				override public void Check()
				{
					switch (_eStatus)
					{
						case Status.none:
						case Status.finished:
						case Status.updated:
							_eStatus = Status.checking;
							if (null == cDBI)
								cDBI = new DBInteract("vj", "vj");
							int nValue = -1;
							lock(cDBI)
								nValue = cDBI.MessageMarkedLastIDGet();
							if(nValue != _nValue)
								_eStatus = Status.alerted;
							else
								_eStatus = Status.finished;
							_nValue = nValue;
							break;
					} 
				}
				override public void Update()
				{
					switch (_eStatus)
					{
						case Status.alerted:
						case Status.finished:
						case Status.none:
						case Status.updated:
							_eStatus = Status.updating;
							if (null == cDBI)
								cDBI = new DBInteract("vj", "vj");
							lock(cDBI)
								_aMessages = cDBI.MessagesQueueGet();
							if (null != _aMessages && 0 < _aMessages.Length)
								_nValue = _aMessages.Max(row => row.nID);
							_eStatus = Status.updated;
							break;
					} 
				}
			}
			public class Announcements : Target
			{
				private Announcement[] _aAnnouncements;

				override public string sJavaScript
				{
					get
					{
						switch (_eStatus)
						{
							case Status.alerted:
								return "AlertAnnouncements();";
							case Status.updated:
								string sRetVal = "_aAnnouncements = new Array();";
								foreach (Announcement cAnnouncement in _aAnnouncements)
								{
									sRetVal += "_aAnnouncements[_aAnnouncements.length]='" + cAnnouncement.sText.ForHTML() + "';";
								}
								sRetVal += "UpdateAnnouncements();";
								return sRetVal;
						}
						return "";
					}
				}

				public Announcements()
					: base()
				{
				}

				override public void Check()
				{
					switch (_eStatus)
					{
						case Status.none:
						case Status.updated:
						case Status.finished:
							_eStatus = Status.checking;
							if (null == cDBI)
								cDBI = new DBInteract("vj", "vj");
							int nValue = -1;
							lock(cDBI)
								nValue = cDBI.AnnouncementLastIDGet();
							if (nValue != _nValue)
								_eStatus = Status.alerted;
							else
								_eStatus = Status.finished;
							_nValue = nValue;
							break;
					}
				}
				override public void Update()
				{
					switch (_eStatus)
					{
						case Status.alerted:
						case Status.finished:
						case Status.none:
						case Status.updated:
							_eStatus = Status.updating;
							if (null == cDBI)
								cDBI = new DBInteract("vj", "vj");
							lock(cDBI)
								_aAnnouncements = cDBI.AnnouncementsActualGet();
							if(null != _aAnnouncements && 0 < _aAnnouncements.Length)
								_nValue = _aAnnouncements.Max(row => row.nID);
							_eStatus = Status.updated;
							break;
					}
				}
			}

			protected Status _eStatus;
			private long _nValue;

			public bool bFinished
			{
				get
				{
					return (Status.finished == _eStatus);
				}
			}
			abstract public string sJavaScript { get; }

			protected Target()
			{
				_nValue = -1;
				_eStatus = Status.none;
			}

			abstract public void Check();
			abstract public void Update();
		}

		private List<Target> _aTargets;

		protected void Page_Load()
		{
			try
			{
				if (null == Session["_aTargets"])
				{
					_aTargets = new List<Target>();
					_aTargets.Add(new Target.Subject());
					_aTargets.Add(new Target.Messages());
					_aTargets.Add(new Target.Announcements());
				}
				else
					_aTargets = (List<Target>)Session["_aTargets"];
				//Target.cDBI = new DBInteract("vj", "dbl;tq");
				if (null != Request.Params["request"])
				{
					Response.AddHeader("Pragma", "no-cache");
					Response.AddHeader("Expires", "0");
					Response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
					Response.AddHeader("Content-Type", "text/javascript");
					Response.AddHeader("Content-disposition", "attachment; filename=" + Request.Params["request"] + ".js");
					string sJavaScript = "";
					switch (Request.Params["request"])
					{
						case "check":
							try
							{
								_aTargets.ForEach(row => row.Check());
								_aTargets.ForEach(row => sJavaScript += row.sJavaScript);
							}
							catch 
							{ }
							sJavaScript += "OnEvent(EventType.checked);";
							break;
						case "update":
							try
							{
								Target cTarget = null;
								if ("messages" == Request.Params["target"])
									cTarget = _aTargets.FirstOrDefault(row => row is Target.Messages);
								else if ("announcments" == Request.Params["target"])
									cTarget = _aTargets.FirstOrDefault(row => row is Target.Announcements);
								if (null != cTarget)
								{
									cTarget.Update();
									sJavaScript += cTarget.sJavaScript;
								}
							}
							catch { }
							sJavaScript += "OnEvent(EventType.updated);";
							break;
					}
					Response.Write(sJavaScript);
				}
			}
			catch { }
		}
		protected void Page_UnLoad()
		{
			Session["_aTargets"] = _aTargets;
		}
	}
}