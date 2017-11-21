//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTunesLib;
	using iTuner;

	
	/// <summary>
	/// These are not unit tests specifically but rather utility scripts for altering
	/// large quantities of songs in my library...
	/// </summary>

	[TestClass]
	public class ManualUpdates : TestBase
	{

		//[TestMethod]
		//public void UpgradeTest ()
		//{
		//    UpgradeHelper helper = new UpgradeHelper();
		//    helper.CheckReleases();
		//}


		/// <summary>
		/// All songs in the "Top 500 Rock" album had a title of the form
		/// "track - artist - title", such as "003 - Derek and the Dominos - Layla"
		/// and Artist set to "Various".  So this is not a unit test but rather as
		/// small utility to parse those names and populate the Arist, Name, and TrackNumber
		/// fields accordingly.
		/// </summary>

		[Ignore]
		[TestMethod]
		public void RenameTop500 ()
		{
			iTunesAppClass itunes = new iTunesAppClass();
			IITLibraryPlaylist playlist = itunes.LibraryPlaylist;
			IITTrackCollection tracks = playlist.Tracks;

			foreach (IITTrack track in tracks)
			{
				if ((track != null) &&
					(track.Album != null) &&
					track.Album.Equals("Top 500 Rock"))
				{
					// 003 - Derek and the Dominos - Layla
					string[] parts = track.Name.Split('-');
					if (parts.Length == 3)
					{
						Console.WriteLine("... " + track.Name);

						int trackNumber = Int32.Parse(parts[0].Trim());
						string artist = parts[1].Trim();
						string name = parts[2].Trim();

						track.Comment = track.Name;

						if (trackNumber > 0)
							track.TrackNumber = trackNumber;

						if (!String.IsNullOrEmpty(artist))
							track.Artist = artist;

						if (!String.IsNullOrEmpty(name))
							track.Name = name;
					}
				}
			}
		}



		/// <summary>
		/// </summary>

		[Ignore]
		[TestMethod]
		public void RenameX ()
		{
			iTunesAppClass itunes = new iTunesAppClass();
			IITLibraryPlaylist playlist = itunes.LibraryPlaylist;
			IITTrackCollection tracks = playlist.Tracks;

			foreach (IITTrack track in tracks)
			{
				if ((track != null) &&
					(track.Album != null) &&
					track.Album.Equals("Sheer Heart Attack") &&
					track.Artist.Equals("Queen"))
				{
					// 07 - Lenny Kravitz - Mr. Cab Driver (1989)
					string[] parts = track.Name.Split('-');
					if (parts.Length == 2)
					{
					    Console.WriteLine("... " + track.Name);

					    int trackNumber = Int32.Parse(parts[0].Trim());
					    string name = parts[1].Trim();

					    track.Comment = track.Name;

					    if (trackNumber > 0)
					        track.TrackNumber = trackNumber;

					    if (!String.IsNullOrEmpty(name))
					        track.Name = name;
					}

					//int index = track.Name.IndexOf('(');
					//if (index > 0)
					//{
					//    track.Name = track.Name.Substring(0, index - 1).Trim();
					//}
				}
			}
		}
	}
}
