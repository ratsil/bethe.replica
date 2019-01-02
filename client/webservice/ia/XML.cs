using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using helpers;
using helpers.extensions;
using helpers.replica.pl;
using helpers.replica.mam;
using System.Net;

namespace webservice.ia
{
	public enum AssetType
	{
		clip,
		program,
		advertisement,
		design,
	}
	static public class XML
	{
		static public string PlaylistItemGet(PlaylistItem cPLI)
		{
			string sRetVal = null;
			AssetType eAssetType = AssetType.design;
			if (!cPLI.bPlug)
			{
				foreach (AssetType eAT in Enum.GetValues(typeof(AssetType)))
				{
					if (null != cPLI.cAsset && null != cPLI.cAsset.stVideo.cType)
					{
						if (cPLI.cAsset.stVideo.cType.sName.ToLower().Contains(eAT.ToString().ToLower()))
						{
							eAssetType = eAT;
							break;
						}
                    }
                    else if (cPLI.aClasses.ContainsPartOfName(eAT.ToString()))
					{
						eAssetType = eAT;
						break;
					}
				}
			}
			DateTime dt = cPLI.dtStartPlanned;
			if (DateTime.MaxValue > cPLI.dtStartReal)
				dt = cPLI.dtStartReal;
			sRetVal = "<pli type=\"" + eAssetType.ToString() + "\" start=\"" + dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") + "\" frames=\"" + (cPLI.nFrameStop - cPLI.nFrameStart) + "\">";
			switch (eAssetType)
			{
				case AssetType.clip:
					sRetVal += ClipGet(Clip.Load(cPLI.cAsset.nID));
					break;
				case AssetType.program:
					Program cProgram = Program.Load(cPLI.cAsset.nID);
					sRetVal += "<program id=\"" + cProgram.nID + "\" name=\"" + cProgram.sName + "\" />";
					break;
				case AssetType.advertisement:
					sRetVal += "<advertisement id=\"" + cPLI.cAsset.nID + "\" name=\"" + cPLI.cAsset.sName + "\" />";
					break;
				case AssetType.design:
					if (cPLI.bPlug)
						sRetVal += "<design id=\"" + cPLI.nID + "\" name=\"" + System.IO.Path.GetFileNameWithoutExtension(cPLI.cFile.sFilename) + "\" />";
					else
						sRetVal += "<design id=\"" + cPLI.cAsset.nID + "\" name=\"" + cPLI.cAsset.sName + "\" />";
					break;
			}
			sRetVal += "</pli>";
			return sRetVal;
		}
		static public string SCRItemGet(Asset cAsset)
		{
			string sRetVal = null;
			if (null == cAsset)
				return "<live type=\"unknown\" />";
			AssetType eAssetType = AssetType.design;
			foreach (AssetType eAT in Enum.GetValues(typeof(AssetType)))
			{
				if (null != cAsset.stVideo.cType)
				{
					if (cAsset.stVideo.cType.sName.ToLower().Contains(eAT.ToString().ToLower()))
					{
						eAssetType = eAT;
						break;
					}
				}
				else if (cAsset.aClasses.ContainsPartOfName(eAT.ToString()))
				{
					eAssetType = eAT;
					break;
				}
			}
			sRetVal = "<live type=\"" + eAssetType.ToString() + "\">";
			switch (eAssetType)
			{
				case AssetType.clip:
					sRetVal += ClipGet((Clip)cAsset);
					break;
				case AssetType.program:
					sRetVal += "<program id=\"" + cAsset.nID + "\" name=\"" + cAsset.sName + "\" />";
					break;
				case AssetType.advertisement:
					sRetVal += "<advertisement id=\"" + cAsset.nID + "\" name=\"" + cAsset.sName + "\" />";
					break;
				case AssetType.design:
					sRetVal += "<design id=\"" + cAsset.nID + "\" name=\"" + cAsset.sName + "\" />";
					break;
			}
			sRetVal += "</live>";
			return sRetVal;
		}
		static public string ClipGet(Clip cClip)
		{
			string sRetVal = null;
			sRetVal += "<clip";
			sRetVal += " id=\"" + cClip.nID + "\"";
			sRetVal += " name=\"" + cClip.sName.ForXML() + "\"";
			sRetVal += ">";
			sRetVal += "<cues";
			sRetVal += " id=\"" + cClip.stCues.nID + "\"";
			sRetVal += " artist=\"" + cClip.stCues.sArtist.ForXML() + "\"";
			sRetVal += " song=\"" + cClip.stCues.sSong.ForXML() + "\"";
			if (null != cClip.stCues.sAlbum)
				sRetVal += " album=\"" + cClip.stCues.sAlbum.ForXML() + "\"";
			sRetVal += " />";
			// c артистами очень долго берётся
			//cClip.PersonsLoad();
			//sRetVal += "<persons>";
			//foreach (Person cPerson in cClip.aPersons)
			//	sRetVal += PersonGet(cPerson);
			//sRetVal += "</persons>";
			sRetVal += "</clip>";
			//if (null == cClip.aCustomValues)
			//    cClip.aCustomValues = cDBI.CustomsLoad(cClip.nID);
			//if (null != cClip.aCustomValues && (-1 < (nCVIndx = CustomValue.FindByName(cClip.aCustomValues, "beep_id"))))
			//	sXML += cClip.aCustomValues[nCVIndx].ToString();
			return sRetVal;
		}
		static public string PersonGet(Person cPerson)
		{
			return "<person id=\"" + cPerson.nID + "\" name=\"" + cPerson.sName.ForXML() + "\" />";
		}
		static public bool Redirect(System.Web.UI.Page cPage)
		{
#if !AUTOCHANNEL
			return false;
#endif
			string sURL = cPage.Request.RawUrl;
			if (sURL.Contains('?'))
				sURL += "&";
			else
				sURL += "?";
			sURL += "ip_source=" + cPage.Request.UserHostAddress;
			HttpWebRequest cWebRequest = (HttpWebRequest)HttpWebRequest.Create("http://127.0.0.1" + sURL);
			HttpWebResponse cWebResponse = (HttpWebResponse)cWebRequest.GetResponse();
			for (int i = 0; i < cWebResponse.Headers.Count; ++i)
				cPage.Response.AddHeader(cWebResponse.Headers.Keys[i], cWebResponse.Headers[i]);
			cPage.Response.ContentType = cWebResponse.ContentType;
			System.IO.Stream cStream = cWebResponse.GetResponseStream();
			byte[] aBuffer = new byte[1024];
			int nQty = 0;
			while (0 < (nQty = cStream.Read(aBuffer, 0, aBuffer.Length)))
				cPage.Response.BinaryWrite(aBuffer.Take(nQty).ToArray());
			return true;
		}
	}
}
