//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner.iTunes;
	using iTunesLib;


	[TestClass]
	public class iTunesPlaylistTests : TestBase
	{

		/*
		 * index,ID,name				Kind		Visible	Smart	SpecialKind
		 * ---------------------------- ----------- ------- ------- --------------
		 [1:18468] Library				Library		False
		 [2:27639] Music				User		True	True	Music
		 [3:36326] Movies				User		True	True	Movies
		 [5:36332] TV Shows				User		True	True	TVShows
		 [6:27630] Podcasts				User		True	False	Podcasts
		[11:36374] Purchased			User		True	False	PurchasedMusic
		[14:36371] Genius				User		True	True	None
		[28:27611] iTunes DJ			User		True	False	PartyShuffle
		[29:36570] My Devices			User		True	True	Folder
		[30:36487] Classical Music		User		True	True	None
		[31:27606] Music Videos			User		True	True	None
		[32:27186] My Top Rated			User		True	True	None
		[33:27599] Recently Added		User		True	True	None
		[34:27220] Recently Played		User		True	True	None
		[35:27192] Top 25 Most Played	User		True	True	None
		[36:36566] Ben					User		True	False	None
		[37:36559] ituner				User		True	False	None
		 * 
		 * 
		 * library: "C:\\Users\\cohns\\Music\\iTunes\\iTunes Music Library.xml"
		 * 
		 *  Library			Visible=false	Master=true				All Items=true
		 *  Music			DKind=4			Music=true				All Items=true		Smart Info
		 *  Moves			DKind=2			Movies=true				All Items=true		Smart Info
		 *  TV Shows		DKind=3			TV Shows=true			All Items=true		Smart Info
		 *  Podcasts		DKind=10		Podcasts=true			All Items=true
		 * -Purchased		DKind=19		Purchased Music=true	All Items=true
		 *  Genius			DKind=26								All Items=true
		 *  iTunes DJ		DKind=22		Party Shuffle=true		All Items=true
		 * -My Devices						Folder=true				All Items=true
		 * -Classical Music											All Items=true		Smart Info
		 * -Music Videos											All Items=true		Smart Info
		 * -My Top Rated											All Items=true		Smart Info
		 * -Recently Added											All Items=true		Smart Info
		 * -Recently Played											All Items=true		Smart Info
		 * -Top 25 Most Played										All Items=true		Smart Info
		 * -Ben														All Items=true
		 * -ituner													All Items=true
 		 */

		[TestMethod]
		public void DumpPlaylists ()
		{
			iTunesAppClass itunes = new iTunesAppClass();
			IITPlaylistCollection playlists = itunes.LibrarySource.Playlists;

			foreach (IITPlaylist playlist in playlists)
			{
				if (playlist.Kind == ITPlaylistKind.ITPlaylistKindUser)
				{
					IITUserPlaylist ulist = (IITUserPlaylist)playlist;

					Console.WriteLine(String.Format(
						"[{0}:{1}] {2} - Kind:{3}, Visible:{4}, Smart:{5}, {6}",
						ulist.Index, ulist.playlistID,
						ulist.Name, ulist.Kind.ToString(), ulist.Visible,
						ulist.Smart, ulist.SpecialKind
						));
				}
				else
				{
					Console.WriteLine(String.Format(
						"[{0}:{1}] {2} - Kind:{3}, Visible:{4}",
						playlist.Index, playlist.playlistID,
						playlist.Name, playlist.Kind.ToString(), playlist.Visible
						));
				}
			}

			Console.WriteLine();
			Console.WriteLine("Filtered:");
			Console.WriteLine();

			foreach (IITPlaylist playlist in playlists)
			{
				if ((playlist.Kind != ITPlaylistKind.ITPlaylistKindUser) ||
					playlist.Name.Equals("Genius"))
				{
					continue;
				}

				IITUserPlaylist ulist = (IITUserPlaylist)playlist;

				if ((ulist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone) ||
					(ulist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindPurchases) ||
					(ulist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindFolder))
				{
					Console.WriteLine(ulist.Name);
				}
			}

			itunes.Quit();
			Marshal.ReleaseComObject(itunes);
			itunes = null;
		}


		[TestMethod]
		public void LibraryTracks ()
		{
			Controller controller = new Controller();
			TrackCollection tracks = controller.LibraryPlaylist.Tracks;

			PersistentIDCollection pids = new PersistentIDCollection();
			//List<int> tids = new List<int>();

			watch.Start();

			foreach (Track track in tracks.Values)
			{
				if (track != null)
				{
					pids.Add(track.PersistentID);
					//tids.Add(track.trackID);
					track.Dispose();
				}
			}

			watch.Stop();
			Console.WriteLine(String.Format("Found {0} tracks", pids.Count));
			Console.WriteLine(String.Format("Completed in {0} ms", watch.GetElapsedMilliseconds()));

			tracks.Dispose();
			tracks = null;

			controller.Dispose();
			controller = null;
		}
	}
}
