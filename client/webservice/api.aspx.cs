using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;

using System.Threading;
using System.Threading.Tasks;
using helpers;
using helpers.extensions;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using WS = webservice.services;
using System.Reflection;

namespace webservice
{
    public partial class api : System.Web.UI.Page
    {
        class Logger : helpers.Logger
        {
            public Logger()
                : base("api", "replica", true)
            { }
        }

        private bool _bAuthenticated
        {
            get
            {
                return null != _cProfile;
            }
        }
        private string _sUser
        {
            get
            {
                return (string)Session["_sUser"];
            }
            set
            {
                Session["_sUser"] = value;
            }
        }
        private string _sPassword
        {
            get
            {
                return (string)Session["_sPassword"];
            }
            set
            {
                Session["_sPassword"] = value;
            }
        }
        private Profile _cProfile
        {
            get
            {
                return (Profile)Session["_cProfile"];
            }
            set
            {
                Session["_cProfile"] = value;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Object oResult = new NotImplementedException();
            try
            {
                string[] aRequests = Request.QueryString.GetValues(null) ?? new string[] { };

                if (!_bAuthenticated)
                {
                    if (aRequests.Contains("authentication"))
                    {
                        oResult = false;
                        if (!Request.QueryString["user"].IsNullOrEmpty())
                        {
                            try
                            {
                                DBInteract cDBI = new DBInteract(Request.QueryString["user"], Request.QueryString["password"]);
                                _cProfile = new Profile(Request.QueryString["user"], Request.QueryString["password"]);
                                access.scopes.init(cDBI.AccessScopesGet());
                                _sUser = Request.QueryString["user"];
                                _sPassword = Request.QueryString["password"];
                                new WS.DBInteract();
                                oResult = true;
                            }
                            catch (Exception ex)
                            {
                                (new Logger()).WriteError(ex);
                            }
                        }
                    }
                }
                else if (1 == aRequests.Length)
                {
                    MethodInfo oMethod;
                    object[] aParameters = JsonConvert.DeserializeObject(RequestBodyGet()).To<object[]>();
                    Type[] aTypes = null;
                    if (null == aParameters)
                        aTypes = new Type[0];
                    var oDBI = Init();
                    Type oType = oDBI.GetType();
                    if (null == aTypes)
                        oMethod = oType.GetMethod(aRequests[0]);
                    else
                        oMethod = oType.GetMethod(aRequests[0], aTypes);
                    if (null != oMethod)
                        oResult = oMethod.Invoke(oDBI, aParameters);
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception ex)
            {
                (new Logger()).WriteError("user=" + (Request == null || null == Request.QueryString["user"] ? "NULL" : Request.QueryString["user"]), ex);
                oResult = ex;
            }
            Response.ContentType = "application/json";
            Response.Write(JsonConvert.SerializeObject(oResult));
        }
        private string RequestBodyGet()
        {
            using (Stream oStream = Request.InputStream)
            {
                using (StreamReader oReader = new StreamReader(oStream, Encoding.UTF8))
                {
                    return oReader.ReadToEnd();
                }
            }
        }
        new private WS.DBInteract Init()
        {
            WS.DBInteract oRetVal = new services.DBInteract();
            oRetVal.Init(_sUser, _sPassword, "DO_NOT_CHECK_VERSION");
            return oRetVal;
        }
    }
}