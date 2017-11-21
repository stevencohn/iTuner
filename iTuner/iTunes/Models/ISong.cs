//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Defines the minimal interface of a basic song used by generic consumers
	/// such as LyricEngine.
	/// </summary>

	internal interface ISong
	{

		/// <summary>
		/// Gets the name of the artist/source of the track.
		/// </summary>

		string Artist { get; }


		/// <summary>
		/// Gets or sets the lyrics for the track.
		/// </summary>

		string Lyrics { get; set; }


		/// <summary>
		/// Gets the name of the current track.
		/// </summary>

		string Title { get; }
	}
}
