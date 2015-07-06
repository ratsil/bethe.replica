using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using g = globalization;

namespace helpers.replica.sl
{
	public class helper
	{
		static public string TranslateVideoTypeIntoStorageName(string sVideoType) //EMERGENCY:l эта функция абсурдна, т.к. нет никакой связи между типом и хранилищем
		{
			switch (sVideoType)
			{
				case "clip":
					return g.Helper.sClips;
				case "advertisement":
                    return g.Helper.sAdvertisement;
				case "program":
                    return g.Helper.sPrograms;
				case "design":
                    return g.Helper.sDesign;
			}
			return sVideoType;
		}

		static public long FindNextItemID(System.Collections.IEnumerable a, System.Type cElementType, string sField, long nID)
		{
			System.Reflection.PropertyInfo cPI = (System.Reflection.PropertyInfo)cElementType.GetMember(sField)[0];
			object cScroll = FindNextItem(a, cElementType, sField, nID);
			if (null != cScroll)
				return (long)cPI.GetValue(cScroll, null);
			else
				return -1;
		}
		static public object FindNextItem(System.Collections.IEnumerable a, System.Type cElementType, string sField, object cItem)
		{
			System.Reflection.PropertyInfo cPI = (System.Reflection.PropertyInfo)cElementType.GetMember(sField)[0];
			return FindNextItem(a, cElementType, sField, (long)cPI.GetValue(cItem, null));
		}
		static public object FindNextItem(System.Collections.IEnumerable a, System.Type cElementType, string sField, long nID)
		{
			System.Reflection.PropertyInfo cPI = (System.Reflection.PropertyInfo)cElementType.GetMember(sField)[0];
			object cPrevAss = null, cPrePreAss = null;
			foreach (object cAss in a)
			{
				if (null != cPrevAss && (long)cPI.GetValue(cPrevAss, null) == nID)
					return cAss;
				if (null != cPrevAss && null != cPrePreAss)
					cPrePreAss = cPrevAss;
				cPrevAss = cAss;
			}
			return cPrePreAss;
		}
		static public long FindPrevItemID(System.Collections.IEnumerable a, System.Type cElementType, string sField, long nID)
		{
			System.Reflection.PropertyInfo cPI = (System.Reflection.PropertyInfo)cElementType.GetMember(sField)[0];
			object cRetVal = FindPrevItem(a, cElementType, sField, nID);
			if (null != cRetVal)
				return (long)cPI.GetValue(cRetVal, null);
			else
				return -1;
		}
		static public object FindPrevItem(System.Collections.IEnumerable a, System.Type cElementType, string sField, long nID)
		{
			System.Reflection.PropertyInfo cPI = (System.Reflection.PropertyInfo)cElementType.GetMember(sField)[0];
			object cPrevAss = null;
			foreach (object cAss in a)
			{
				if ((long)cPI.GetValue(cAss, null) == nID)
					return cPrevAss;
				cPrevAss = cAss;
			}
			return cPrevAss;
		}
	}
}
