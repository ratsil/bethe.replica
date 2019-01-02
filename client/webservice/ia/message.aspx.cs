using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using helpers;
using helpers.extensions;
using helpers.replica.ia;
using System.Text;

namespace webservice.ia
{
	public partial class message : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("message")
			{ }
		}
        protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				if (XML.Redirect(this))
					return;
				DBInteract cDBI = new DBInteract("replica_ia", "");
                byte[] aImageBytes = null;
                string sBindID = null, sText = null;
                int nCount = -1;
                ulong nSource = 0, nTarget = 0;
                Gateway.IP cIP = new Gateway.IP(Request.UserHostAddress);

				if (null != Request.Params["request"])
				{
					if ("status" == Request.Params["request"] && null != Request.Params["id"])
					{
						Message cMessage = cDBI.MessageGetByBindID(Request.Params["id"]);
						if (null != cMessage)
						{
							Response.ContentType = "text/xml";
							String sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><messages><message ";
							sXML += " id=\"" + cMessage.sBindID + "\"";
							sXML += " registered=\"" + cMessage.dtRegister.ToString("yyyy-MM-dd HH:mm:ss") + "\"";
							if(cMessage.dtDisplay > DateTime.MinValue && cMessage.dtDisplay < DateTime.MaxValue) 
								sXML += " displayed=\"" + cMessage.dtDisplay.ToString("yyyy-MM-dd HH:mm:ss") + "\"";
							sXML += "/></messages>";
							Response.Write(sXML);
						}
						else
							Response.StatusCode = 404;
					}
					else if ("count" == Request.Params["request"] && null != Request.Params["target"])
					{
						if ("received" == Request.Params["target"])
							Response.Write(cDBI.MessagesCountGet(cIP));
						else if ("showed" == Request.Params["target"])
							Response.Write(cDBI.MessagesDisplayedCountGet(cIP));
						else
							Response.StatusCode = 400;
					}
					else if ("list" == Request.Params["request"])
					{
						Queue<Message> aqMessages = null;
						if (null != (aqMessages = cDBI.MessagesRegisteredGet(DateTime.Now.Subtract(TimeSpan.FromHours(3)), DateTime.MaxValue, false)))
						{
							Message cMessage = null;
							Response.AppendHeader("Expires", "0");
							Response.AppendHeader("Cache-Control", "must-revalidate, post-check=0, pre-check=0");
							Response.ContentType = "text/html";
							while (0 < aqMessages.Count)
							{
								cMessage = aqMessages.Dequeue();
								Response.Write(cMessage.dtRegister.ToString("yyyy-MM-dd HH:mm:ss") + ";" + cMessage.sText.ForHTML() + "\n");
							}
						}
						return;
					}
					else
						Response.StatusCode = 400;
					return;
				}

				if (null == Request.Params["id"] && null != Request.Params["id_sms"])
					sBindID = Request.Params["id_sms"];
				else if (null != Request.Params["id"])
					sBindID = Request.Params["id"];
				else
					throw new Exception("parameter id is missing");

				if (null == Request.Params["source"] && null != Request.Params["addr_from"])
					nSource = Request.Params["addr_from"].Replace("+", "").ToULong();
				else if (null != Request.Params["source"])
					nSource = Request.Params["source"].Replace("+", "").ToULong();
				else
                    throw new Exception("parameter source is missing");

				if (null == Request.Params["target"] && null != Request.Params["addr_to"])
					nTarget = Request.Params["addr_to"].Replace("+", "").ToULong();
				else if (null != Request.Params["target"])
					nTarget = Request.Params["target"].Replace("+", "").ToULong();
				else
                    throw new Exception("parameter target is missing");

				if (null != Request.Params["count"])
					nCount = Request.Params["count"].ToInt16();
				else
					throw new Exception("parameter count is missing");

				if (null != Request.Params["text"])
					sText = HttpUtility.ParseQueryString(Request.RawUrl, Encoding.GetEncoding("utf-8"))["text"];
				else
                    throw new Exception("parameter text is missing");
					
				if (null != Request.Params["image"])
						aImageBytes = System.IO.File.ReadAllBytes(Request.Params["image"]);


				
				Message cMsg = new Message(-1, sBindID, cIP, nCount, nSource, nTarget, sText, aImageBytes, DateTime.Now, DateTime.MaxValue);
				cDBI.MessageAdd(cMsg);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"+ Environment.NewLine;
				sXML += "<error description=\"" + ex.Message.ForXML() + "\">" + Environment.NewLine;
				if (null != ex.InnerException)
				{
					sXML += "<inner description=\"" + ex.InnerException.Message.ForXML() + "\" />" + Environment.NewLine;
					sXML += "</error>" + Environment.NewLine;
				}
				else
					sXML += " />" + Environment.NewLine;
				Response.Write(sXML);
				//Response.StatusCode = 500;
			}
		}
	}
}
