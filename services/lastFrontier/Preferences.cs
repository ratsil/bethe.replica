using System;
using System.Collections.Generic;
using System.Text;
using helpers;
using helpers.extensions;
using System.Xml;

namespace replica.lfrontier
{
	public class Preferences : helpers.Preferences
	{
		static private Preferences _cInstance = new Preferences();

		static public string sFolder
		{
			get
			{
				return _cInstance._sFolder;
			}
		}

		private string _sFolder;

		public Preferences()
			: base("//replica/lfrontier")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode || _bInitialized)
				return;
            _sFolder = cXmlNode.AttributeValueGet("folder");
			if (!System.IO.Directory.Exists(_sFolder))
				throw new Exception("указанная папка не существует [folder:" + _sFolder + "][" + cXmlNode.Name + "]"); //TODO LANG
		}
	}
}
