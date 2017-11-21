//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Globalization;
	using System.Windows.Data;
	using Resx = Properties.Resources;


	/// <summary>
	/// </summary>

	[ValueConversion(typeof(UsbDisk.UsbSpace), typeof(string))]
	internal class UsbSpaceConverter : IValueConverter
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>

		public object Convert (
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return "-";
			}

			UsbDisk.UsbSpace space = (UsbDisk.UsbSpace)value;
			return String.Format(Resx.FreeSpace,
				UsbDisk.FormatByteCount(space.FreeSpace),
				UsbDisk.FormatByteCount(space.Size));
		}


		/// <summary>
		/// Not used.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>

		public object ConvertBack (
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
