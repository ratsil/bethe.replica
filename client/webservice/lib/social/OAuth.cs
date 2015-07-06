using System;
using System.Web;


namespace webservice
{
    class OAuth
    {
        public string sID;
        public Uri cUriAuthentication;
        public Uri cUriResponse;
        public string sTokenName;
        public string sToken
        {
            get
            {
                return (string)HttpContext.Current.Session["oauth_" + sID + "_sToken"];
            }
            set
            {
                HttpContext.Current.Session["oauth_" + sID + "_sToken"] = value;
            }
        }
        public Uri cUriImageUpload
        {
            get
            {
                return (Uri)HttpContext.Current.Session["oauth_" + sID + "_cUriImageUpload"];
            }
            set
            {
                HttpContext.Current.Session["oauth_" + sID + "_cUriImageUpload"] = value;
            }
        }
        public bool bAuthorized
        {
            get
            {
                return (null != sToken);
            }
        }
    }
}