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

                if (_bAuthenticated || aRequests.Contains("signin"))
                {
                    if (1 == aRequests.Length)
                    {
                        switch (aRequests[0])
                        {
                            case "signin":
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
                                break;
                            case "authorize":
                                oResult = _bAuthenticated;
                                break;
                            case "signout":
                                _sUser = _sPassword = null;
                                _cProfile = null;
                                oResult = null;
                                break;
                            default:
                                Data oData = null;
                                object[] aParameters = null;
                                //(new Logger()).WriteNotice("1");
                                try
                                {
                                    //(new Logger()).WriteNotice("2");
                                    oData = JsonConvert.DeserializeObject<Data>(RequestBodyGet());
                                    //(new Logger()).WriteNotice("3");
                                    aParameters = oData.data;
                                } catch { }
                                MethodInfo oMethod;
                                //object[] aParameters = JsonConvert.DeserializeObject<Data>(RequestBodyGet()).data;
                                Type[] aTypes = null;
                                //(new Logger()).WriteNotice("4");
                                if (null != aParameters)
                                {
                                    //(new Logger()).WriteNotice("5");
                                    aTypes = new Type[aParameters.Length];
                                    for (int n = 0; aParameters.Length > n; n++) {
                                        aTypes[n] = Type.GetType(oData.types[n], true);
                                        switch (oData.data[n].GetType().Name) {
                                            case "JArray":
                                                switch (aTypes[n].Name)
                                                {
                                                    case "IdNamePair[]":
                                                        oData.data[n]= ((Newtonsoft.Json.Linq.JArray)oData.data[n]).Select(o => new IdNamePair
                                                        {
                                                            nID = (long)o["nID"],
                                                            sName = (string)o["sName"]
                                                        }).ToArray();
                                                        break;
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                    aTypes = new Type[0];
                                //(new Logger()).WriteNotice("6");

                                var oDBI = Init();
                                Type oType = oDBI.GetType();
                                if (null == aTypes)
                                    oMethod = oType.GetMethod(aRequests[0]);
                                else
                                    oMethod = oType.GetMethod(aRequests[0], aTypes);
                                //(new Logger()).WriteNotice("7");
                                if (null != oMethod)
                                    oResult = oMethod.Invoke(oDBI, aParameters);
                                //(new Logger()).WriteNotice("8");
                                break;
                        }
                    }
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
        public class Data
        {
            public object[] data;
            public string[] types;
        }
    }
}