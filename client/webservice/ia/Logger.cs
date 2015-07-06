using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace webservice.ia
{
	class Logger : helpers.Logger
	{
		public Logger(string sCategory)
			: base("ia" + ((null != sCategory && 0 < sCategory.Length) ? "." + sCategory : ""), "webservice")
		{ }
		public Logger()
			: this("")
		{ }
	}
}