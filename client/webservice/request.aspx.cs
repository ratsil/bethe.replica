using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;

using Facebook;
using LinqToTwitter;
using System.Threading;
using System.Threading.Tasks;
using helpers;
using helpers.extensions;
using System.Xml;
using System.IO;
using System.Text;

namespace webservice
{
    public partial class request : System.Web.UI.Page
    {
        class Logger : helpers.Logger
        {
            public Logger()
                : base("request", "replica", true)
            { }
        }
        //class WebBrowser : SWF.ApplicationContext
        //{
        //    private int _nNavigatingQty = 0;
        //    private ManualResetEvent _cMRE = new ManualResetEvent(false);
        //    private Thread _cThread;
        //    private SWF.WebBrowser _cWebBrowser;
        //    private string _sURL;

        //    public WebBrowser(string sURL)
        //    {
        //        _sURL = sURL;
        //        _nNavigatingQty = 0;
        //        _cMRE = new ManualResetEvent(false);
        //        _cThread = new Thread(Worker);
        //        _cThread.SetApartmentState(ApartmentState.STA);
        //        this.
        //        _cThread.Start();
        //    }
        //    public void Worker()
        //    {
        //        _cWebBrowser = new SWF.WebBrowser();
        //        _cWebBrowser.DocumentCompleted += new SWF.WebBrowserDocumentCompletedEventHandler(_cWebBrowser_DocumentCompleted);
        //        _cWebBrowser.Navigating += new SWF.WebBrowserNavigatingEventHandler(_cWebBrowser_Navigating);
        //        _cWebBrowser.Navigate("http://odnoklassniki.ru");
        //        //_cWebBrowser.Navigate("http://google.com");
        //        SWF.Application.Run(this);
        //    }

        //    void _cWebBrowser_Navigating(object sender, SWF.WebBrowserNavigatingEventArgs e)
        //    {
        //        _nNavigatingQty++;
        //    }

        //    void _cWebBrowser_DocumentCompleted(object sender, SWF.WebBrowserDocumentCompletedEventArgs e)
        //    {
        //        if (1 > (--_nNavigatingQty))
        //        {
        //            SWF.HtmlElement uiEmail, uiPassword, uiSubmit;
        //            if (
        //                null != (uiEmail = (SWF.HtmlElement)_cWebBrowser.Document.GetElementById("field_email"))
        //                && null != (uiPassword = (SWF.HtmlElement)_cWebBrowser.Document.GetElementById("field_password"))
        //                && null != (uiSubmit = (SWF.HtmlElement)_cWebBrowser.Document.GetElementById("hook_FormButton_button_go"))
        //            )
        //            {
        //                uiEmail.SetAttribute("value", "listar@mail.ru");
        //                uiPassword.SetAttribute("value", "v2ksi3");
        //                uiSubmit.InvokeMember("click");
        //                return;
        //            }
        //            _cMRE.Set();
        //        }
        //    }
        //    public void Wait()
        //    {
        //        _cMRE.WaitOne();
        //    }
        //}

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
                if (false && !_bAuthenticated)
                {
                    if (aRequests.Contains("authentication"))
                    {
                        sResult = "error('wrong credentials');authorization();";
                        if (!Request.QueryString["user"].IsNullOrEmpty())
                        {
                            try
                            {
                                DBInteract cDBI = new DBInteract(Request.QueryString["user"], Request.QueryString["pwd"]);
                                _cProfile = new Profile(Request.QueryString["user"], Request.QueryString["pwd"]);
                                access.scopes.init(cDBI.AccessScopesGet());
                                _sUser = Request.QueryString["user"];
                                _sPassword = Request.QueryString["pwd"];
                                sResult = "statistics();";
                            }
                            catch (Exception ex)
                            {
                                (new Logger()).WriteError(ex);
                            }
                        }
                    }
                    Response.Write(sResult.IsNullOrEmpty() ? "" : sResult);
                    return;
                }
                _sUser = "user";
                _sPassword = "";

                foreach (string sRequest in aRequests)
                {
                    switch (sRequest)
                    {
                        case "authentication":
                            sResult = "statistics();";
                            break;
                        case "social":
                            DBInteract cDBI = null;
                            OAuth cOAuth = null;
                            string sResponseQuery = "social&scope=" + Request.Params["scope"] + "&type=response";
                            string sCookieSocialGuid = "replica:social:guid";
                            string sGuid;
                            switch (Request.Params["scope"])
                            {
                                #region fb
                                case "fb":
                                    switch (Request.Params["type"])
                                    {
                                        case "at":
                                            string sAccessToken = Request.Params["value"];
                                            Session["social:fb:at"] = sAccessToken;
                                            FacebookClient cFacebookClient = new FacebookClient(sAccessToken);
                                            cFacebookClient.AppId = Preferences.sFacebookAppID;
                                            cFacebookClient.AppSecret = Preferences.sFacebookSecret;
                                            try
                                            {

                                                dynamic dResult = cFacebookClient.Get("oauth/access_token", new { grant_type = "fb_exchange_token", fb_exchange_token = sAccessToken, client_id = cFacebookClient.AppId, client_secret = cFacebookClient.AppSecret });
                                                cDBI = new DBInteract(_sUser, _sPassword);
                                                cDBI.AdmPreferencesSocialSet("fb:at", dResult.access_token);
                                            }
                                            catch (FacebookOAuthException ex)
                                            {
                                                sResult = "error('" + ex.Message.ForHTML() + "')";
                                            }
                                            catch (Exception ex)
                                            {
                                                sResult = "error('" + ex.Message.ForHTML() + "')";
                                            }
                                            break;
                                    }
                                    break;
                                #endregion

                                #region tt
                                case "tt":
                                    IOAuthCredentials iCredentials = new SessionStateCredentials();
                                    if (null == iCredentials.ConsumerKey || null == iCredentials.ConsumerSecret)
                                    {
                                        iCredentials.ConsumerKey = Preferences.sTwitterKey;
                                        iCredentials.ConsumerSecret = Preferences.sTwitterSecret;
                                        cDBI = new DBInteract(_sUser, _sPassword);
                                        iCredentials.AccessToken = cDBI.AdmPreferencesSocialGet("twitter:at");
                                        iCredentials.OAuthToken = cDBI.AdmPreferencesSocialGet("twitter:ot");
                                    }
                                    WebAuthorizer cWebAuthorizer = new WebAuthorizer
                                    {
                                        Credentials = iCredentials,
                                        PerformRedirect = authUrl => Response.Redirect(authUrl)
                                    };
                                    switch (Request.Params["type"])
                                    {
                                        case "status":
                                            sResult = "social.status(social.scope.tt, " + cWebAuthorizer.IsAuthorized.ToString().ToLower() + ");";
                                            break;
                                        case "response":
                                            cWebAuthorizer.CompleteAuthorization(new Uri(Request.Url.AbsoluteUri.Replace(sResponseQuery + "&", "")));
                                            if (cWebAuthorizer.IsAuthorized)
                                            {
                                                if(null == cDBI)
                                                    cDBI = new DBInteract(_sUser, _sPassword);
                                                cDBI.AdmPreferencesSocialSet("twitter:at", cWebAuthorizer.Credentials.AccessToken);
                                                cDBI.AdmPreferencesSocialSet("twitter:ot", cWebAuthorizer.Credentials.OAuthToken);
                                            }
                                            sResult = "<script type='text/javascript'>window.close();</script>";
                                            break;
                                        case "login":
                                            cWebAuthorizer.BeginAuthorization(new Uri(Request.Url, "request.aspx?" + sResponseQuery));
                                            break;
                                    }
                                    break;
                                #endregion

                                #region vk
                                case "vk":
                                    XmlDocument cResponse;
                                    XmlNode cXmlNode, cXmlNodeChild;
                                    cOAuth = new OAuth
                                    {
                                        cUriAuthentication = new Uri("https://oauth.vk.com/authorize?client_id=" + Preferences.sVKontakteAppID + "&scope=photos,wall,offline&display=popup&v=5.2&response_type=token&redirect_uri=" + Preferences.sOAuthDomain + "/social.aspx?vk")
                                    };
                                    switch (Request.Params["type"])
                                    {
                                        case "status":
                                            if (!cOAuth.bAuthorized && null != Request.Cookies[sCookieSocialGuid])
                                            {
                                                cDBI = new DBInteract(_sUser, _sPassword);
                                                sGuid = Response.Cookies[sCookieSocialGuid].Value;
                                                cDBI.AdmPreferencesSocialSet(sGuid, DateTime.Now.Ticks.ToString());
                                                cOAuth.sToken = cDBI.AdmPreferencesSocialGet(sGuid + ":vkontakte:ot");
                                            }
                                            if (cOAuth.bAuthorized)
                                            {
                                                //https://api.vk.com/method/'''METHOD_NAME'''?'''PARAMETERS'''&access_token='''ACCESS_TOKEN'''
                                                //https://api.vk.com/method/users.get.xml?uid=66748&v=5.2&access_token=533bacf01e11f55b536a565b57531ac114461ae8736d6506a3
                                                System.Net.WebClient cWebClient = new System.Net.WebClient();
                                                string sResponse = cWebClient.DownloadString("https://api.vk.com/method/photos.getWallUploadServer.xml?v=5.2&access_token=" + cOAuth.sToken);
                                                cResponse = new XmlDocument();
                                                cResponse.Load(new StringReader(sResponse));
                                                cXmlNode = cResponse.GetElementsByTagName("response").Item(0);
                                                if (null == cXmlNode)
                                                    throw new Exception("wrong response format");
                                                cXmlNodeChild = cXmlNode.NodeGet("upload_url");
                                                if (null == cXmlNodeChild)
                                                {
                                                    cXmlNodeChild = cXmlNode.NodeGet("error");
                                                }
                                                else
                                                    cOAuth.cUriImageUpload = new Uri(cXmlNodeChild.InnerText);
                                            }
                                            else
                                                sResult = "social.status(social.scope.vk, false);";
                                            break;
                                        case "login":
                                            Response.Redirect(cOAuth.cUriAuthentication.ToString());
                                            break;
                                        case "publish":
                                            if (cOAuth.bAuthorized)
                                            {
                                                //https://api.vk.com/method/'''METHOD_NAME'''?'''PARAMETERS'''&access_token='''ACCESS_TOKEN'''
                                                //https://api.vk.com/method/users.get.xml?uid=66748&v=5.2&access_token=533bacf01e11f55b536a565b57531ac114461ae8736d6506a3
                                                string sBoundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                                                byte[] aBytes = Encoding.ASCII.GetBytes(
                                                    "--" + sBoundary + Environment.NewLine
                                                    + "Content-Disposition: form-data; name=\"photo\"; filename=\"" + Request.Files[0].FileName + "\"" + Environment.NewLine
                                                    + "Content-Type: " + Request.Files[0].ContentType + Environment.NewLine + Environment.NewLine
                                                );
                                                WebRequest cWebRequest = WebRequest.Create(cOAuth.cUriImageUpload);
                                                cWebRequest.Method = "POST";
                                                cWebRequest.ContentType = "multipart/form-data; boundary=" + sBoundary;
                                                Stream cStream = cWebRequest.GetRequestStream();
                                                cStream.Write(aBytes, 0, aBytes.Length);
                                                Request.Files[0].InputStream.CopyTo(cStream);
                                                aBytes = Encoding.ASCII.GetBytes(Environment.NewLine + "--" + sBoundary + "--" + Environment.NewLine);
                                                cStream.Write(aBytes, 0, aBytes.Length);

                                                MemoryStream cResponseBuffer = new MemoryStream();
                                                cWebRequest.GetResponse().GetResponseStream().CopyTo(cResponseBuffer);
                                                string sResponse = UTF8Encoding.ASCII.GetString(cResponseBuffer.ToArray());
                                                sResponse = sResponse.Substring(1, sResponse.Length - 3).Replace("\"server\":", "&server=").Replace(",\"photo\":\"", "&photo=").Replace("\",\"hash\":\"", "&hash=").Replace("\\\"","\"");
                                                WebClient cWebClient = new WebClient();
                                                sResponse = cWebClient.DownloadString("https://api.vk.com/method/photos.saveWallPhoto.xml?v=5.2&access_token=" + cOAuth.sToken + sResponse);
                                                cResponse = new XmlDocument();
                                                cResponse.Load(new StringReader(sResponse));
                                                cXmlNode = cResponse.GetElementsByTagName("response").Item(0);
                                                if (null == cXmlNode)
                                                    throw new Exception("wrong response format");
                                                cXmlNode = cXmlNode.NodeGet("photo", false);
                                                if (null == cXmlNode)
                                                {
                                                    cXmlNodeChild = cXmlNode.NodeGet("error");
                                                }
                                                else
                                                    sResponse = cWebClient.DownloadString("https://api.vk.com/method/wall.post.xml?v=5.2&access_token=" + cOAuth.sToken + "&message=" + HttpUtility.UrlEncode(Request.Params["sMessage"]) + "attachments=photo" + cXmlNode.NodeGet("owner_id").InnerText + "_" + cXmlNode.NodeGet("id").InnerText);

                                                //wall.post
                                            }
                                            else
                                                sResult = "social.status(social.scope.vk, false);";
                                            break;
                                    }
                                    //sResult = "social.vk();";
                                    //Response.Redirect("social.html");
                                    //switch (Request.Params["type"])
                                    //{
                                    //    case "status":
                                    //        sResult = "document.getElementById('_ui_imgVKLogin').style.display=" + (cWebAuthorizer.IsAuthorized?"'none';":"'block';");
                                    //        break;
                                    //    case "response":
                                    //        cWebAuthorizer.CompleteAuthorization(new Uri(Request.Url.AbsoluteUri.Replace(sResponseQuery + "&", "")));
                                    //        if (cWebAuthorizer.IsAuthorized)
                                    //        {
                                    //            if(null == cDBI)
                                    //                cDBI = new DBInteract(_sUser, _sPassword);
                                    //            cDBI.AdmPreferencesSocialSet("instagram:at", cWebAuthorizer.Credentials.AccessToken);
                                    //            cDBI.AdmPreferencesSocialSet("instagram:ot", cWebAuthorizer.Credentials.OAuthToken);
                                    //        }
                                    //        sResult = "<script type='text/javascript'>window.close();</script>";
                                    //        break;
                                    //    case "login":
                                    //        cWebAuthorizer.BeginAuthorization(new Uri(Request.Url, "request.aspx?" + sResponseQuery));
                                    //        break;
                                    //}
                                    break;
                                #endregion
                                case "all":
                                    switch (Request.Params["type"])
                                    {
                                        case "publish":
                                            cDBI = new DBInteract(_sUser, _sPassword);

                                            #region fb
                                            //FacebookClient cFacebookClient = new Facebook.FacebookClient(cDBI.AdmPreferencesSocialGet("fb:at"));

                                            //var postParams = new
                                            //{
                                            //    name = "Test-Тест:имя",
                                            //    caption = "Заголовок Caption",
                                            //    description = "Описание Description",
                                            //    link = "http://example.com/",
                                            //    picture = "https://example.com/pic.png"
                                            //};

                                            //try
                                            //{
                                            //    Task<object> cPostTask = cFacebookClient.PostTaskAsync("/me/feed", postParams);
                                            //    cPostTask.Wait();
                                            //    IDictionary<string, object> ahResult = (IDictionary<string, object>)cPostTask.Result;

                                            //    string sSuccessMessageDialog = (string)ahResult["id"];
                                            //}
                                            //catch (Exception ex)
                                            //{
                                            //}
                                            #endregion

                                            #region tt
                                            //TwitterContext cTwitterContext = new TwitterContext(new WebAuthorizer() { Credentials = new SessionStateCredentials() });
                                            //string status = "Testing " + DateTime.Now.ToString();
                                            //const bool possiblySensitive = false;
                                            //const decimal latitude = StatusExtensions.NoCoordinate;
                                            //const decimal longitude = StatusExtensions.NoCoordinate;
                                            //const bool displayCoordinates = false;

                                            //var mediaItems =
                                            //    new List<Media>
                                            //    {
                                            //        new Media
                                            //        {
                                            //            Data = Utilities.GetFileBytes(@"d:\photo\IMG_7705.jpg"),
                                            //            FileName = "IMG_7705.jpg",
                                            //            ContentType = MediaContentType.Jpeg
                                            //        }
                                            //    };
                                            //Status tweet = cTwitterContext.TweetWithMedia(
                                            //    status, possiblySensitive, latitude, longitude, 
                                            //    null, displayCoordinates, mediaItems, null);
                                            #endregion

                                            #region vk
                                            //https://api.vk.com/method/'''METHOD_NAME'''?'''PARAMETERS'''&access_token='''ACCESS_TOKEN'''
                                            //https://api.vk.com/method/users.get.xml?uid=66748&v=5.2&access_token=533bacf01e11f55b536a565b57531ac114461ae8736d6506a3
                                            cOAuth = new OAuth();
                                            if (cOAuth.bAuthorized)
                                            {
                                                System.Net.WebClient cWebClient = new System.Net.WebClient();
                                                string sResponse = cWebClient.DownloadString("https://api.vk.com/method/photos.getProfileUploadServer.xml?v=5.2&access_token=" + cOAuth.sToken);
                                                //var oResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(sResponse);

                                                //oResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(cWebClient.DownloadString("https://api.vk.com/method/photos.saveProfilePhoto.xml?v=5.2&access_token=" + cOAuth.sToken));

                                                //wall.post
                                            }
                                            else
                                            {
                                                sResult += "";
                                            }
                                            #endregion
                                            break;
                                    }
                                    break;
                            }
                            //var client = new FacebookClient();
                            //dynamic me = client.Get("isayko.alexey");
                            break;
                        case "exit":
                            _cProfile = null;
                            sResult = "window.location = window.location;";
                            break;
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