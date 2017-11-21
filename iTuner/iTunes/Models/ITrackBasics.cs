//************************************************************************************************
// Copyright © 2012 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Note there are some specific overlaps between this interface and ISong and ITrack.
	/// This is on purpose as I didn't want to carve those up any further just to make
	/// the interfaces pure; it would have resulted in an overly confusing hierarchy of
	/// interfaces.  So each interface completely describes all the properties necessary
	/// for the intended operations.
	/// </summary>

	internal interface ITrackBasics
	{
		/// <summary>
		/// Gets or sets the name of the album containing the track.
		/// </summary>

		string Album { get; set; }


		/// <summary>
		/// Gets or sets the name of the artist/source of the track.
		/// </summary>

		string Artist { get; set; }


		/// <summary>
		/// Gets or sets the URL of the artist Web site.
		/// This is a custom iTuner property storedin IITrack.Comments.
		/// </summary>

		string ArtistURL { get; set; }


		/// <summary>
		/// Gets or sets the duration of the track in seconds.
		/// </summary>

		long Duration { get; set; }


		/// <summary>
		/// Gets or sets the music/audio genre (category) of the track. 
		/// </summary>

		string Genre { get; set; }


		/// <summary>
		/// Gets a Boolean value indicating if this track was updated using an online
		/// Webservice such as MucicDNS genpuid or MusicBrainz.
		/// </summary>

		bool IsAnalyzed { get; }


		/// <summary>
		/// Gets the physical location of the track for either CD or File based tracks.
		/// </summary>

		string Location { get; }


		/// <summary>
		/// Gets or sets the name of the current track.
		/// </summary>

		string Title { get; set; }


		/// <summary>
		/// Gets or sets the track number or position on the CD of this track.
		/// </summary>

		int TrackNumber { get; set; }


		/// <summary>
		/// Gets or sets the (unique) ID of this track as calculated by an online
		/// service such as MusicDNS genpuid.
		/// </summary>

		string UniqueID { get; set; }


		/// <summary>
		/// Gets or sets the year the track was recorded/released.
		/// </summary>

		int Year { get; set; }
	}
}
