//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;


	/// <summary>
	/// ITrack.Rating is a value from 0 to 100.  The "stars" in the iTunes interface 
	/// represents increments of 20.  So 1 star is 20, 2 stars is 40, etc.  This will
	/// convert between the iTunes scale (0 to 100) and the UI scale (0-5).
	/// </summary>

	[ValueConversion(typeof(int), typeof(int))]
	internal class RatingConverter : IValueConverter
	{

		/// <summary>
		/// Convert from the iTunes scale to the UI scale.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>

		public object Convert (
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			int original = (int)value;
			return original / 20;
		}


		/// <summary>
		/// Convert from the UI scale to the iTunes scale.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>

		public object ConvertBack (
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			int converted = (int)value;
			return converted * 20;
		}
	}
}
