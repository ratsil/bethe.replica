using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace webservice.services
{
	public class Logger : helpers.Logger
	{
		public Logger()
			: base("host:services", "webservice")
		{ }
	}
}
