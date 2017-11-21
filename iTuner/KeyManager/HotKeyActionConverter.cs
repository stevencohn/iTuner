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


	[ValueConversion(typeof(int), typeof(int))]
	internal class HotKeyActionConverter : IValueConverter
	{

		/// <summary>
		/// Convert from a HotKeyAction enumeration value to a descriptive string.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>

		public object Convert (
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			HotKeyAction action = (HotKeyAction)value;
			switch (action)
			{
				case HotKeyAction.PlayPause:
					return Resx.ActionPlayPause;

				case HotKeyAction.NextTrack:
					return Resx.ActionNextTrack;

				case HotKeyAction.PrevTrack:
					return Resx.ActionPrevTrack;

				case HotKeyAction.VolumeUp:
					return Resx.ActionVolumeUp;

				case HotKeyAction.VolumeDown:
					return Resx.ActionVolumeDown;

				case HotKeyAction.Mute:
					return Resx.ActionMute;

				case HotKeyAction.ShowiTunes:
					return Resx.ActionShowiTunes;

				case HotKeyAction.ShowiTuner:
					return Resx.ActionShowiTuner;

				case HotKeyAction.ShowLyrics:
					return Resx.ActionShowLyrics;
			}

			return String.Empty;
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