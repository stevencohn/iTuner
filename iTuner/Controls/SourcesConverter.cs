//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Globalization;
	using System.Windows.Data;
	using System.Windows.Media.Imaging;
	using iTuner.iTunes;

	/// <summary>
	/// Controller.CurrentSource is a Sources enumeration derived from the IITPlaylist name
	/// or track type.  We take that enum and convert it to a bitmap for visualization in
	/// the TrackPanel.
	/// </summary>

	[ValueConversion(typeof(Sources), typeof(BitmapImage))]
	internal class SourcesConverter : IValueConverter
	{

		/// <summary>
		/// Convert a Sources enumeration value into a BitmapImage (PNG)
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>

		public object Convert (
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			string path;

			switch ((Sources)value)
			{
				case Sources.CD:
					path = @"Images\Sources\SourceCD.png";
					break;

				case Sources.Movies:
					path = @"Images\Sources\SourceMovie.png";
					break;

				case Sources.Podcast:
					path = @"Images\Sources\SourcePodcast.png";
					break;

				case Sources.Radio:
					path = @"Images\Sources\SourceRadio.png";
					break;

				case Sources.Store:
					path = @"Images\Sources\SourceStore.png";
					break;

				case Sources.TVShow:
					path = @"Images\Sources\SourceTVShow.png";
					break;

				default:
					path = @"Images\Sources\SourceMusic.png";
					break;
			}

			BitmapImage image = new BitmapImage(new Uri(path, UriKind.Relative));
			return image;
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
