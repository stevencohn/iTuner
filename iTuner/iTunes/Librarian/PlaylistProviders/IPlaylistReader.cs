//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Declares the minimum interface of a playlist reader needed by general consumers.
	/// </summary>

	internal interface IPlaylistReader : IDisposable
	{

		/// <summary>
		/// Gets the full path of the next track specified in the playlist file.
		/// </summary>
		/// <returns>
		/// A string specifying the full path of a file or <b>null</b> if there are no
		/// further tracks available.
		/// </returns>

		string GetNext ();
	}
}