using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using helpers;
using helpers.extensions;

namespace showcase
{
	public class Preferences : helpers.Preferences
	{
		static private Preferences _cInstance = new Preferences();

		static public string sFootagesPath
		{
			get
			{
                return _cInstance._sFootagesPath;
			}
		}

        private string _sFootagesPath;

		public Preferences()
			: base("//showcase")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			_sFootagesPath = cXmlNode.AttributeValueGet("path");
		}
	}
}
