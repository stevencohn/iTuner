//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Declares the minimum interface of a playlist writer needed by general consumers.
	/// </summary>

	internal interface IPlaylistWriter : IDisposable
	{

		/// <summary>
		/// Add a single entry to the playlist.
		/// </summary>
		/// <param name="track"></param>

		void Add (Track track, string path);


		/// <summary>
		/// Close the playlist, including writing the inheritor-implemented footer.
		/// </summary>
	
		void Close ();


		/// <summary>
		/// Open the playlist, including writing the inheritor-implemented header.
		/// </summary>

		void Open ();
	}
}