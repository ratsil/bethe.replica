using System;
using System.Linq;
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
using System.Xml;

using helpers;
using helpers.extensions;
using helpers.replica.mam;
using webservice.services;
using ws=webservice.services;

namespace webservice.ia
{
	public partial class Grids : System.Web.UI.Page
	{
		private class Logger : webservice.ia.Logger
		{
			public Logger()
				: base("grids")
			{ }
		}
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				Response.ContentType = "text/xml";
				Response.AddHeader("content-disposition", "filename = grid.xml");
                XmlDocument cXMLDocument = new XmlDocument();
                cXMLDocument.LoadXml(System.IO.File.ReadAllText(Request.MapPath("/") + "/grids/grid.xml"));
                XmlNode cGrid = cXMLDocument.NodeGet("grid");

                string sXML = "<?xml version=`1.0` encoding=`utf-8`?>";
                if (null != Request.Params["format"] && "xmltv" == Request.Params["format"])
                {
                    DateTime dtNow = DateTime.Now;
                    string sDate = dtNow.ToString("yyyyMMdd"), sTime;
                    string sProgram = null;
                    sXML += "<tv generator-info-name=`bas replica` generator-info-url=`https://code.google.com/p/bas-replica/`>"
                        + "<channel id=`channel`><icon src=`http://channel/logo.png` /></channel>";
                    foreach (XmlNode cXmlNode in cGrid.NodesGet(dtNow.DayOfWeek.ToString().ToLower() + "/record"))
                    {
                        sTime = "`" + sDate + cXmlNode.AttributeValueGet("time").Replace(":", "") + "00 +0400`";
                        if (null != sProgram)
                            sXML += sProgram.Replace("stop=``", "stop=" + sTime);
                        sProgram = "<programme start=" + sTime + " stop=`` channel=`channel`><title lang=`ru`>" + cXmlNode.AttributeValueGet("name") + "</title></programme>";
                    }
                    if (null != sProgram)
                        sXML += sProgram.Replace("stop=``", "stop=`" + sDate + "235959 +0400`");
					sXML += "</tv>";
                }
                else
                {
                    XmlNode cDictionary = cXMLDocument.NodeGet("grid/translit");
                    sXML += "<grid>";
					Dictionary<string, string> ahDictionary = new Dictionary<string, string>();
					foreach (XmlNode cRecord in cDictionary.NodesGet("record"))
						ahDictionary.Add(cRecord.AttributeValueGet("ru"), cRecord.AttributeValueGet("en"));
					string sDay;
					foreach (DayOfWeek eDay in Enum.GetValues(typeof(DayOfWeek)))
					{
						sXML += "<" + (sDay = eDay.ToString().ToLower()) + ">";
						foreach (XmlNode cXmlNode in cGrid.NodesGet(sDay + "/record"))
							sXML += "<record name=`" + ahDictionary[cXmlNode.AttributeValueGet("name")] + "` time=`" + cXmlNode.AttributeValueGet("time") + "` />";
						sXML += "</" + (sDay = eDay.ToString().ToLower()) + ">";
					}
					sXML += "</grid>";
				}
				Response.Write(sXML.Replace('`', '"'));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				string sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><error description=\"" + ex.Message.ForXML() + "\"";
				if (null != ex.InnerException)
				{
                    sXML += "<inner description=\"" + ex.InnerException.Message.ForXML() + "\" />" + Environment.NewLine;
                    sXML += "</error>" + Environment.NewLine;
				}
				else
                    sXML += " />" + Environment.NewLine;
				Response.Write(sXML);
                if (null != Request.Params["format"] && "xmltv" == Request.Params["format"]) 
                    Response.StatusCode = 503;
			}
		}
	}
}
