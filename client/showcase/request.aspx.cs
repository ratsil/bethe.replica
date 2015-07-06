using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using helpers.extensions;
using ingenie.userspace;

namespace showcase
{
    public partial class request : System.Web.UI.Page
    {
        static object _cSyncRoot = new object();
        static Animation _cLogo = null;
        static Clock _cClock = null;
        static Roll _cRollVertical = null;
        static Roll _cRollHorizontal = null;
        static Video _cVideo = null;
        static Playlist _cPlaque = null;

        protected void Page_Load(object sender, EventArgs e)
        {
            string sResult = "";
            try
            {
                List<string> aRequests = new List<string>();
                foreach (string sRequest in Request.QueryString.Keys)
                    aRequests.Add(sRequest);
                aRequests.Remove("cp");
                while(0 < aRequests.Count)
                {
                    switch (aRequests[0])
                    {
                        case "logo":
                            #region logo
                            lock (_cSyncRoot)
                            {
                                Atom.Status eStatus = Atom.Status.Unknown;
                                if(null == _cLogo)
                                {
                                    _cLogo = LogoCreate();
                                    eStatus = Atom.Status.Prepared;
                                }
                                else
                                    eStatus = _cLogo.eStatus;
                                switch (eStatus)
                                {
                                    case Atom.Status.Unknown:
                                    case Atom.Status.Idle:
                                    case Atom.Status.Stopped:
                                    case Atom.Status.Prepared:
                                        if(Atom.Status.Prepared != eStatus)
                                            LogoInit(_cLogo);
                                        _cLogo.Start();
                                        sResult = "logo_started();";
                                        break;
                                    case Atom.Status.Started:
                                        _cLogo.Stop();
                                        sResult = "logo_stopped();";
                                    break;
                                }
                            }
                            break;
                            #endregion
                        case "clock":
                            #region clock
                            lock (_cSyncRoot)
                            {
                                Atom.Status eStatus = Atom.Status.Unknown;
                                if (null == _cClock)
                                {
                                    _cClock = ClockCreate();
                                    eStatus = Atom.Status.Prepared;
                                }
                                else
                                    eStatus = _cClock.eStatus;
                                switch (eStatus)
                                {
                                    case Atom.Status.Unknown:
                                    case Atom.Status.Idle:
                                    case Atom.Status.Stopped:
                                    case Atom.Status.Prepared:
                                        if (Atom.Status.Prepared != eStatus)
                                            ClockInit(_cClock);
                                        _cClock.Start();
                                        sResult = "clock_started();";
                                        break;
                                    case Atom.Status.Started:
                                        _cClock.Stop();
                                        sResult = "clock_stopped();";
                                        break;
                                }
                            }
                            break;
                            #endregion
                        case "roll":
                            #region roll
                            lock (_cSyncRoot)
                            {
                                EffectVideo cEffect = null;
                                switch(Request.QueryString["add"])
                                {
                                    case "logo":
                                        cEffect = LogoCreate();
                                        break;
                                    case "clock":
                                        cEffect = ClockCreate();
                                        break;
                                }
                                aRequests.Remove(Request.QueryString["add"]);
                                aRequests.Remove("add");
                                string sPrefix = null;
                                Atom.Status eStatus = Atom.Status.Unknown;
                                Roll cRoll = RollCreate();
                                switch(cRoll.eDirection)
                                {
                                    case Roll.Direction.LeftToRight:
                                    case Roll.Direction.RightToLeft:
                                        if (null == _cRollHorizontal)
                                        {
                                            _cRollHorizontal = cRoll;
                                            eStatus = Atom.Status.Prepared;
                                        }
                                        else
                                        {
                                            cRoll = _cRollHorizontal;
                                            eStatus = _cRollHorizontal.eStatus;
                                        }
                                        sPrefix = "roll";
                                        break;
                                    case Roll.Direction.UpToDown:
                                    case Roll.Direction.DownToUp:
                                        if (null == _cRollVertical)
                                        {
                                            _cRollVertical = cRoll;
                                            eStatus = Atom.Status.Prepared;
                                        }
                                        else
                                        {
                                            cRoll = _cRollVertical;
                                            eStatus = _cRollVertical.eStatus;
                                        }
                                        sPrefix = "crawl";
                                        break;
                                }
                                switch (eStatus)
                                {
                                    case Atom.Status.Unknown:
                                    case Atom.Status.Idle:
                                    case Atom.Status.Stopped:
                                    case Atom.Status.Prepared:
                                        if (Atom.Status.Prepared != eStatus)
                                            RollInit(cRoll);
                                        cRoll.Start();
                                        sResult = sPrefix + "_started();";
                                        break;
                                    case Atom.Status.Started:
                                        if (null == cEffect)
                                        {
                                            cRoll.Stop();
                                            sResult = sPrefix + "_stopped();";
                                        }
                                        break;
                                }
                                if (null != cEffect)
                                {
                                    cEffect.stArea = helpers.Area.stEmpty;
                                    cRoll.EffectAdd(cEffect);
                                }
                            }
                            break;
                            #endregion
                         case "video":
                            #region video
                            lock (_cSyncRoot)
                            {
                                Atom.Status eStatus = Atom.Status.Unknown;
                                if(null == _cVideo)
                                {
                                    _cVideo = VideoCreate();
                                    eStatus = Atom.Status.Prepared;
                                }
                                else
                                    eStatus = _cVideo.eStatus;
                                switch (eStatus)
                                {
                                    case Atom.Status.Unknown:
                                    case Atom.Status.Idle:
                                    case Atom.Status.Stopped:
                                    case Atom.Status.Prepared:
                                        if(Atom.Status.Prepared != eStatus)
                                            VideoInit(_cVideo);
                                        _cVideo.Start();
                                        sResult = "video_started();";
                                        break;
                                    case Atom.Status.Started:
                                        _cVideo.Stop();
                                        sResult = "video_stopped();";
                                    break;
                                }
                            }
                            break;
                            #endregion
                        case "plaque":
                            #region plaque
                            lock (_cSyncRoot)
                            {
                                Atom.Status eStatus = Atom.Status.Unknown;
                                if(null == _cPlaque)
                                {
                                    _cPlaque = PlaqueCreate();
                                    eStatus = Atom.Status.Prepared;
                                }
                                else
                                    eStatus = _cPlaque.eStatus;
                                switch (eStatus)
                                {
                                    case Atom.Status.Unknown:
                                    case Atom.Status.Idle:
                                    case Atom.Status.Stopped:
                                    case Atom.Status.Prepared:
                                        if(Atom.Status.Prepared != eStatus)
                                            PlaqueInit(_cPlaque);
                                        _cPlaque.Start();
                                        sResult = "plaque_started();";
                                        break;
                                    case Atom.Status.Started:
                                        _cPlaque.Stop();
                                        sResult = "plaque_stopped();";
                                    break;
                                }
                            }
                            break;
                            #endregion
                   }
                    aRequests.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                sResult = "alert('Произошла ошибка [" + ex.Message + "]!');_ui_dvBody.style.display = 'block';";
            }
            Response.Write(sResult);
        }

        #region parse
        private Queue<string> PropertiesCreate(string sKey)
        {
            return new Queue<string>(Request.QueryString[sKey].Split('\n'));
        }
        private helpers.Area AreaCreate(Queue<string> aqProperties)
        {
            return new helpers.Area(aqProperties.Dequeue().ToShort(), aqProperties.Dequeue().ToShort(), aqProperties.Dequeue().ToUShort(), aqProperties.Dequeue().ToUShort());
        }
        private Color ColorCreate(Queue<string> aqProperties)
        {
            Color cRetVal = new Color();
            cRetVal.nAlpha = aqProperties.Dequeue().ToByte();
            cRetVal.nRed = aqProperties.Dequeue().ToByte();
            cRetVal.nGreen = aqProperties.Dequeue().ToByte();
            cRetVal.nBlue = aqProperties.Dequeue().ToByte();
            return cRetVal;
        }
        private Font FontCreate(Queue<string> aqProperties)
        {
            Font cRetVal = new Font();
            cRetVal.sName = aqProperties.Dequeue();
            cRetVal.nSize = aqProperties.Dequeue().ToInt();
            cRetVal.cColor = ColorCreate(aqProperties);
            cRetVal.cBorder = new Border();
            cRetVal.cBorder.nWidth = aqProperties.Dequeue().ToUShort();
            cRetVal.cBorder.cColor = ColorCreate(aqProperties);
            return cRetVal;
        }
        #endregion

        private Animation LogoCreate()
        {
            Animation cRetVal = new Animation();
            cRetVal.bKeepAlive = true;
            cRetVal.nLayer = 110;
            cRetVal.nLoopsQty = 0;
            cRetVal.sFolder = System.IO.Path.Combine(Preferences.sFootagesPath, "logo");
            LogoInit(cRetVal);
            return cRetVal;
        }
        private void LogoInit(Animation cLogo)
        {
            cLogo.stArea = AreaCreate(PropertiesCreate("logo"));
        }
        private Clock ClockCreate()
        {
            Clock cRetVal = new Clock();
            cRetVal.nLayer = 100;
            cRetVal.nInDissolve = 10;
            cRetVal.nOutDissolve = 10;
            ClockInit(cRetVal);
            return cRetVal;
        }
        private void ClockInit(Clock cClock)
        {
            Queue<string> aqProperties = PropertiesCreate("clock");
            cClock.stArea = AreaCreate(aqProperties);
            cClock.sFormat = aqProperties.Dequeue();
            cClock.sSuffix = aqProperties.Dequeue();
            cClock.cFont = FontCreate(aqProperties);
        }
        private Roll RollCreate()
        {
            Roll cRetVal = new Roll();
            cRetVal.nLayer = 50;
            RollInit(cRetVal);
            return cRetVal;
        }
        private void RollInit(Roll cRoll)
        {
            Queue<string> aqProperties = PropertiesCreate("roll");
            cRoll.stArea = AreaCreate(aqProperties);
            switch (aqProperties.Dequeue())
            {
                case "up":
                    cRoll.eDirection = Roll.Direction.DownToUp;
                    break;
                case "down":
                    cRoll.eDirection = Roll.Direction.UpToDown;
                    break;
                case "left":
                    cRoll.eDirection = Roll.Direction.RightToLeft;
                    break;
                case "right":
                    cRoll.eDirection = Roll.Direction.LeftToRight;
                    break;
            }
            cRoll.nSpeed = aqProperties.Dequeue().ToShort();
        }
        private Video VideoCreate()
        {
            Video cRetVal = new Video();
            cRetVal.nLayer = 10;
            VideoInit(cRetVal);
            return cRetVal;
        }
        private void VideoInit(Video cVideo)
        {
            cVideo.stArea = AreaCreate(PropertiesCreate("video"));
        }
        private Playlist PlaqueCreate()
        {
            Playlist cRetVal = new Playlist();
            cRetVal.nLayer = 20;
            cRetVal.cHide = new Atom.HIDE() { enType = Atom.HIDE.TYPE.skip, nDelay = 0 };
            cRetVal.EffectAdd(new Animation()
            {
                bKeepAlive = false,
                nLoopsQty = 1,
                sFolder = System.IO.Path.Combine(Preferences.sFootagesPath, "plaque/in")
            });
            cRetVal.EffectAdd(new Animation()
            {
                bKeepAlive = false,
                nLoopsQty = 0,
                sFolder = System.IO.Path.Combine(Preferences.sFootagesPath, "plaque/loop")
            });
            cRetVal.EffectAdd(new Animation()
            {
                bKeepAlive = true,
                nLoopsQty = 1,
                sFolder = System.IO.Path.Combine(Preferences.sFootagesPath, "plaque/out")
            });
            PlaqueInit(cRetVal);
            return cRetVal;
        }
        private void PlaqueInit(Playlist cPlaque)
        {
            cPlaque.stArea = AreaCreate(PropertiesCreate("plaque"));
        }
    }
}