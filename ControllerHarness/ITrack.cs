//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Declares the public members of the Track class.
	/// </summary>

	internal interface ITrack : ISong, IDisposable
	{

		/// <summary>
		/// Gets the name of the album containing the track.
		/// </summary>

		string Album { get; }


		/// <summary>
		/// Gets the artwork associated with the track.
		/// </summary>

		//Bitmap Artwork { get; }


		/// <summary>
		/// Gets the length of the track in seconds.
		/// </summary>

		long Duration { get; }


		/// <summary>
		/// Set the music/audio genre (category) of the track. 
		/// </summary>

		string Genre { get; }


		/// <summary>
		/// Gets a Boolean value indicating if this is a track that might contain
		/// lyrics; a track for which lyrics could be downloaded.
		/// </summary>

		bool IsLyrical { get; }


		/// <summary>
		/// Gets the name of the kind of track (CD, Device, File, etc)
		/// </summary>

		string KindAsString { get; }


		/// <summary>
		/// Gets the physical location of the track for either CD or File based tracks.
		/// </summary>

		string Location { get; }


		/// <summary>
		/// Gets the rating of the track (0 to 100). If the track rating has never been set,
		/// or has been set to 0, it will be computed based on the album rating. 
		/// </summary>

		int Rating { get; set; }


		/// <summary>
		/// Returns the length of the track (in MM:SS format). 
		/// </summary>

		string Time { get; }


		/// <summary>
		/// Gets the total number of tracks on the source album.
		/// </summary>

		int TrackCount { get; }


		/// <summary>
		/// Returns the ID that identifies the track within the playlist.  This is a
		/// runtime ID, it is only valid while the current instance of iTunes is running.
		/// </summary>

		int TrackID { get; }

			
		/// <summary>
		/// Gets the index of the track on the source album.
		/// </summary>

		int TrackNumber { get; }


		/// <summary>
		/// Gets the year the track was recorded/released.
		/// </summary>

		int Year { get; }


		/// <summary>
		/// Determines if the current track is subjectively better than the specified track.
		/// This is done by comparing critical qualities of the two tracks such as BitRate,
		/// rating, run count, etc.
		/// </summary>
		/// <param name="other">The other track to compare against this instance.</param>
		/// <returns>
		/// <b>True</b> if the current instance is better or <b>false</b> if the
		/// given instance is better.
		/// </returns>

		bool IsBetterThan (Track other);


		/// <summary>
		/// Generates a string that can be used as a header for lyrics exports.
		/// </summary>
		/// <returns>A string specifying the basic information of this track.</returns>

		string MakeLyricReportHeader ();
	}
}