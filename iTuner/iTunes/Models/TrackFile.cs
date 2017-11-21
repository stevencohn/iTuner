//************************************************************************************************
// Copyright © 2012 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;

	
	/// <summary>
	/// Describes basic information for a media file.
	/// </summary>

	internal class TrackFile : ITrackBasics
	{
		private string location;
		private string uniqueID;


		/// <summary>
		/// Initialize a new instance based on the media file at the specified path.
		/// </summary>
		/// <param name="location">Path to the media file.</param>

		public TrackFile (string location)
		{
			this.location = location;
			this.uniqueID = null;
		}


		/// <summary>
		/// Cloning construction, actually used to copy basic properties of an actual Track
		/// for the Information Scanner.
		/// </summary>
		/// <param name="track">The track to copy</param>

		public TrackFile (ITrackBasics track)
		{
			this.Album = track.Album;
			this.Artist = track.Artist;
			this.ArtistURL = track.ArtistURL;
			this.Genre = track.Genre;
			this.Title = track.Title;
			this.TrackNumber = track.TrackNumber;
			this.Year = track.Year;
			this.location = track.Location;
			this.uniqueID = track.UniqueID;
		}


		/// <summary>
		/// Gets or sets the name of the album containing the track.
		/// </summary>

		public string Album
		{
			get;
			set;
		}


		/// <summary>
		/// Gets or sets the name of the artist/source of the track.
		/// </summary>

		public string Artist
		{ 
			get;
			set;
		}


		/// <summary>
		/// Gets or sets the URL of the artist Web site.
		/// This is a custom iTuner property storedin IITrack.Comments.
		/// </summary>

		public string ArtistURL
		{
			get;
			set;
		}
	

		/// <summary>
		/// Gets or sets the music/audio genre (category) of the track. 
		/// </summary>

		public string Genre
		{
			get;
			set;
		}


		/// <summary>
		/// Gets a Boolean value indicating if this track was updated using an online
		/// Webservice such as MucicDNS genpuid or MusicBrainz.
		/// </summary>

		public bool IsAnalyzed
		{
			get { return !String.IsNullOrEmpty(uniqueID); }
		}


		/// <summary>
		/// Gets or sets the duration of the track in seconds.
		/// </summary>

		public long Duration
		{
			get;
			set;
		}

	
		/// <summary>
		/// Gets the physical location of the track for either CD or File based tracks.
		/// </summary>

		public string Location
		{
			get { return location; }
		}


		/// <summary>
		/// Gets or sets the name of the current track.
		/// </summary>

		public string Title
		{
			get;
			set;
		}


		/// <summary>
		/// Gets or sets the track number or position on the CD of this track.
		/// </summary>

		public int TrackNumber
		{
			get;
			set;
		}

	
		/// <summary>
		/// Gets or sets the (unique) ID of this track as calculated by an online
		/// service such as MusicDNS genpuid.
		/// </summary>

		public string UniqueID
		{
			get { return uniqueID; }
			set { uniqueID = value; }
		}


		/// <summary>
		/// Gets or sets the year the track was recorded/released.
		/// </summary>

		public int Year
		{
			get;
			set;
		}
	}
}
