using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace replica.failover
{
	class Logger : helpers.Logger
	{
		public class Sync : helpers.Logger
		{
			public Sync()
				: base("sync", "sync")
			{ }
		}

		public Logger(string sCategory)
			: base(sCategory)
		{ }
	}
}
