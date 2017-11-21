//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Globalization;
	using System.Windows.Data;


	/// <summary>
	/// </summary>

	[ValueConversion(typeof(string), typeof(string))]
	internal class UsbNullConverter : IValueConverter
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
			return value ?? "-";
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
