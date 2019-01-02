using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace replica.management
{
	class Logger : helpers.Logger
	{
		public Logger()
			: base("management")
		{
		}
		public Logger(string sCategory)
			: base(sCategory)
		{ }
		public void Email(string sTargets, string sSubject, string sBody)
		{
			helpers.Logger.Email(sTargets, sSubject, sBody);
		}
	}
}
