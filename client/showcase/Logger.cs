using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using helpers;
using helpers.extensions;

namespace showcase
{
    public class Logger : helpers.Logger
    {
        public Logger()
            : base("web")
        { }
    }
}