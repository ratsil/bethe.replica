using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Facebook;
using LinqToTwitter;
using System.Threading;
using System.Threading.Tasks;
using helpers;
using helpers.extensions;

namespace webservice
{
    public partial class social : System.Web.UI.Page
    {
        class Logger : helpers.Logger
        {
            public Logger()
                : base("social", "replica", true)
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
            string sResult = "";
            try
            {
                string[] aRequests = Request.QueryString.GetValues(null);
                string sCookieSocialGuid = "replica:social:guid";
                string sGuid;
                foreach (string sRequest in aRequests)
                {
                    switch (sRequest)
                    {
                        #region vk
                        case "vk":
                            //http://localhost/social.aspx?social&scope=vk&type=response#access_token=e0ddeb394f18bd3ff3170b9d25c5741a8655901640fdaef75f89bff4ec7ea8c9ff5689826541fa4f5fa09&expires_in=86400&user_id=1785663
                            if (null != Request.Params["access_token"])
                            {
                                OAuth cOAuth = new OAuth();
                                cOAuth.sToken = Request.Params["access_token"];
                                DBInteract cDBI = new DBInteract(_sUser, _sPassword);
                                if (null == Request.Cookies[sCookieSocialGuid])
                                {
                                    sGuid = "social:" + Guid.NewGuid().ToString();
                                    Response.AppendCookie(new HttpCookie(sCookieSocialGuid, sGuid));
                                    Response.Cookies[sCookieSocialGuid].Expires = DateTime.Now.AddYears(28);
                                }
                                else
                                    sGuid = Response.Cookies[sCookieSocialGuid].Value;
                                cDBI.AdmPreferencesSocialSet(sGuid, DateTime.Now.Ticks.ToString());
                                cDBI.AdmPreferencesSocialSet(sGuid + ":vkontakte:ot", cOAuth.sToken);

                                sResult = "<script type='text/javascript'>window.close();</script>";
                            }
                            else
                                sResult = "<script type='text/javascript'>window.location = window.location.href.replace('#','&');</script>";
                            break;
                        #endregion
                    }
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                sResult = "error('[" + (ex.Message + "][st:" + ex.StackTrace).ForHTML() + "]');";
            }
            Response.Write(sResult);
        }
    }
}