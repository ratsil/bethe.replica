using System;
using System.Windows.Data;
using helpers;
using helpers.extensions;
using helpers.replica.services.dbinteract;

namespace replica.sl
{
    public class FramesConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
			long nFrames = value.ToLong();
			if (typeof(string) == targetType)
			{
				string sRetVal;
				try
				{
					bool bFramesShow = true;
					if (null != parameter)
						bFramesShow = parameter.ToBool();
					sRetVal = nFrames.ToFramesString(bFramesShow);
				}
				catch
				{
					sRetVal = "error";
				}
				return sRetVal;
			}
			else if (typeof(DateTime?) == targetType)
			{
				DateTime? cRetVal = null;
				try
				{
					cRetVal = DateTime.MinValue.AddMilliseconds(nFrames * 40); 
				}
				catch { }
				return cRetVal;
			}
			return null;
        }
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string sRetVal;
			long nIN = 1, nOUT = 0;
            try
            {
                PlaylistItem cPLI;
                Asset cAsset;
                if (value is PlaylistItem)
                {
                    cPLI = (PlaylistItem)value;
                    nIN = cPLI.nFrameStart;
                    nOUT = cPLI.nFrameStop;
                }
                else if (value is Asset)
                {
                    cAsset = (Asset)value;
                    nIN = cAsset.nFrameIn;
                    nOUT = cAsset.nFrameOut;
                }
                sRetVal = (nOUT - nIN + 1).ToFramesString(true);
            }
            catch
            {
                sRetVal = "error";
            }
            return sRetVal;
        }
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    // это пример как делать если надо передать несколько параметров в конвертер...  можно еще целый объект передавать, но говорят, что это медленно...
	//public class DurationFromAssetConverter : IValueConverter
	//{
	//    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
	//    {
	//        string sRetVal = "";
	//        try
	//        {
	//            IConverterData iConverterData = (IConverterData)value;
	//            if (null != iConverterData)
	//            {
	//                int nFramesQty = value.ToInt32(); //EMERGENCY это здесь точно нужно?   // это просто пример. кажись он даже не работает нигде ))
	//                sRetVal = (iConverterData.nFrameOut - iConverterData.nFrameIn + 1).ToFramesString(true);
	//            }
	//        }
	//        catch
	//        {
	//            sRetVal = "error";
	//        }
	//        return sRetVal;
	//    }
	//    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
	//    {
	//        throw new NotImplementedException();
	//    }
	//    public interface IConverterData // это пример как делать если надо передать несколько параметров в конвертер...
	//    {
	//        int nFrameIn { get; set; }
	//        int nFrameOut { get; set; }
	//    }
	//}
}
