//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	
	/// <summary>
	/// Specifies the available actions that may be bound to hot key sequences.
	/// </summary>
	/// <remarks>
	/// The order of these values prescribes the order in which they are shown
	/// in the hot key editor window.
	/// </remarks>

	internal enum HotKeyAction
	{
		/// <summary>
		/// Toggle play/pause mode.
		/// </summary>

		PlayPause,


		/// <summary>
		/// Move to the next available track.
		/// </summary>

		NextTrack,


		/// <summary>
		/// Move to the previous available track.
		/// </summary>

		PrevTrack,


		/// <summary>
		/// Increase the volume level by about 5%.
		/// </summary>

		VolumeUp,


		/// <summary>
		/// Decrease the volume level by about 5%.
		/// </summary>

		VolumeDown,


		/// <summary>
		/// Mute the volume.
		/// </summary>

		Mute,


		/// <summary>
		/// Show the lyrics for the currently playing track.
		/// </summary>

		ShowLyrics,


		/// <summary>
		/// Bring the iTunes window to the front.
		/// </summary>

		ShowiTunes,


		/// <summary>
		/// Show the iTuner info window.
		/// </summary>

		ShowiTuner,


		/// <summary>
		/// Unspecified action.
		/// </summary>

		None
	}
}
