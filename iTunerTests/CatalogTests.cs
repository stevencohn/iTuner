//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Xml;
	using System.Xml.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTunesLib;
	using iTuner.iTunes;


	[TestClass]
	public class CatalogTests : TestBase
	{
		private static iTunesAppClass itunes;
		private static string libraryXMLPath;


		//========================================================================================
		// Lifecycle
		//========================================================================================

		#region Lifecycle

		[ClassInitialize()]
		public static void MyClassInitialize (TestContext testContext)
		{
			itunes = new iTunesAppClass();
			libraryXMLPath = itunes.LibraryXMLPath;
		}


		[ClassCleanup]
		public static void MyClassCleanup ()
		{
			Console.WriteLine("**** Unit test shutdown");

			if (itunes != null)
			{
				itunes = null;
			}
		}

		#endregion Lifecycle


		//========================================================================================
		// Loading Tests
		//========================================================================================

		[TestMethod]
		[Ignore]
		public void LoadFlat ()
		{
			ICatalog catalog = new FlatCatalog();
			catalog.Initialize(libraryXMLPath);
		}


		[TestMethod]
		public void LoadTerse ()
		{
			ICatalog catalog = new TerseCatalog();
			catalog.Initialize(libraryXMLPath);

			string music = catalog.MusicPath;

			//catalog.DeleteFile(@"C:\Music\4 Non Blondes\Bigger, Better, Faster, More\02 Superfly.mp3");
			//catalog.DeleteFile(@"C:\Music\4 Non Blondes\Bigger, Better, Faster, More\02 Superfly.mp3");
		}


		//========================================================================================
		// Tests
		//========================================================================================

		[TestMethod]
		public void FlatTerseComparisons ()
		{
			// create two playlists, both name "Ben" and use this to test that we can
			// find the extensions in the first one

			ICatalog flat = new FlatCatalog();
			flat.Initialize(libraryXMLPath);

			ICatalog terse = new TerseCatalog();
			terse.Initialize(libraryXMLPath);

			PersistentID pid = GetPlaylistPersistentID("Ben");

			// FindExtensionsByPlaylist()

			StringCollection flatExtensions = flat.FindExtensionsByPlaylist(pid);
			Assert.IsNotNull(flatExtensions);
			Assert.AreNotEqual(0, flatExtensions.Count);

			StringCollection terseExtensions = terse.FindExtensionsByPlaylist(pid);
			Assert.IsNotNull(terseExtensions);
			Assert.AreNotEqual(0, terseExtensions.Count);

			Assert.AreEqual(flatExtensions.Count, terseExtensions.Count);

			foreach (string ext in terseExtensions)
			{
				Assert.IsTrue(flatExtensions.Contains(ext));
			}

			Console.WriteLine("FindExtensionsByPlaylist() OK");

			// FindPlaylistName()

			string name = flat.FindPlaylistName(pid);
			Assert.AreEqual("Ben", name);

			name = terse.FindPlaylistName(pid);
			Assert.AreEqual("Ben", name);

			Console.WriteLine("FindPlaylistName() OK");

			// FindTracksByAlbum()

			PersistentIDCollection flatTracks =
				flat.FindTracksByAlbum("Greatest Hits", "Alice Cooper");
			Assert.IsNotNull(flatTracks);
			Assert.AreNotEqual(0, flatTracks.Count);

			PersistentIDCollection terseTracks =
				terse.FindTracksByAlbum("Greatest Hits", "Alice Cooper");
			Assert.IsNotNull(terseTracks);
			Assert.AreNotEqual(0, terseTracks.Count);

			Assert.AreEqual(flatTracks.Count, terseTracks.Count);

			foreach (PersistentID id in terseTracks)
			{
				Assert.IsTrue(flatTracks.Contains(id));
			}

			Console.WriteLine("FindTracksByAlbum() OK");

			// FindTracksByArtist()

			flatTracks = flat.FindTracksByArtist("Alice Cooper");
			Assert.IsNotNull(flatTracks);
			Assert.AreNotEqual(0, flatTracks.Count);

			terseTracks = terse.FindTracksByArtist("Alice Cooper");
			Assert.IsNotNull(terseTracks);
			Assert.AreNotEqual(0, terseTracks.Count);

			Assert.AreEqual(flatTracks.Count, terseTracks.Count);

			foreach (PersistentID id in terseTracks)
			{
				Assert.IsTrue(flatTracks.Contains(id));
			}

			Console.WriteLine("FindTracksByArtist() OK");

			// FindTracksByPlaylist()

			pid = GetPlaylistPersistentID("My Top Rated");

			flatTracks = flat.FindTracksByPlaylist(pid);
			Assert.IsNotNull(flatTracks);
			Assert.AreNotEqual(0, flatTracks.Count);

			terseTracks = terse.FindTracksByPlaylist(pid);
			Assert.IsNotNull(terseTracks);
			Assert.AreNotEqual(0, terseTracks.Count);

			Assert.AreEqual(flatTracks.Count, terseTracks.Count);

			foreach (PersistentID id in terseTracks)
			{
				Assert.IsTrue(flatTracks.Contains(id));
			}

			Console.WriteLine("FindTracksByPlaylist() OK");
		}


		private PersistentID GetPlaylistPersistentID (string name)
		{
			IITPlaylist playlist = itunes.LibrarySource.Playlists.get_ItemByName(name);
			PersistentID pid = GetPersistentID(playlist);
			return pid;
		}


		//========================================================================================
		// Linq Speed Tests
		//========================================================================================

		#region Linq Speed Tests

		//FindCatalogedTracksByAlbum--------------------------------------------------------------
		//new FlatCatalog() -> 14284.0023654521 ms
		//FindTracksByAlbum -> 22.4926922855539 ms
		//Tracks[] -> 3.66109802877771 ms
		//Completed in 14310.163854851 ms
		//FindCatalogedTracksByAlbum Completed: Passed--------------------------------------------

		[TestMethod]
		public void FindCatalogedTracksByAlbum ()
		{
			// FlatCatalog

			ICatalog catalog = new FlatCatalog();
			catalog.Initialize(libraryXMLPath);
			Console.WriteLine(String.Format("new FlatCatalog() -> {0} ms", watch.GetSplitMilliseconds()));

			PersistentIDCollection trackIDs = catalog.FindTracksByAlbum("Ganging Up on the Sun", "Guster");
			Console.WriteLine(String.Format("FindTracksByAlbum -> {0} ms", watch.GetSplitMilliseconds()));

			Assert.IsNotNull(trackIDs);
			Assert.AreNotEqual(0, trackIDs.Count);
			Console.WriteLine(String.Format("Found {0} tracks", trackIDs.Count));

			ReportPrivateDelta();

			// TerseCatalog

			catalog = new TerseCatalog();
			catalog.Initialize(libraryXMLPath);
			Console.WriteLine(String.Format("new TerseCatalog() -> {0} ms", watch.GetSplitMilliseconds()));

			trackIDs = catalog.FindTracksByAlbum("Ganging Up on the Sun", "Guster");
			Console.WriteLine(String.Format("FindTracksByAlbum -> {0} ms", watch.GetSplitMilliseconds()));

			Assert.IsNotNull(trackIDs);
			Assert.AreNotEqual(0, trackIDs.Count);
			Console.WriteLine(String.Format("Found {0} tracks", trackIDs.Count));

			ReportPrivateDelta();

			// Controller

			using (var controller = new Controller())
			{
				System.Threading.Thread.Sleep(2000); // let the controller initialize

				List<Track> tracks = new List<Track>();
				foreach (PersistentID persistentID in trackIDs)
				{
					Track track = controller.LibraryPlaylist.GetTrack(persistentID);
					tracks.Add(track);
				}

				Console.WriteLine(String.Format("Tracks[] -> {0} ms", watch.GetSplitMilliseconds()));

				// iTunes

				List<Track> tracks2 = new List<Track>();
				foreach (PersistentID persistentID in trackIDs)
				{
					IITTrack itrack = itunes.LibraryPlaylist.Tracks.get_ItemByPersistentID(
						persistentID.HighBits, persistentID.LowBits);

					Track track = new Track(itrack);
					tracks2.Add(track);
				}

				Console.WriteLine(String.Format("ITracks[] -> {0} ms", watch.GetSplitMilliseconds()));

				Assert.AreNotEqual(0, tracks.Count);
			}
		}


		[TestMethod]
		public void FindCatalogedTracksByArtist ()
		{
			// FlatCatalog

			ICatalog catalog = new FlatCatalog();
			catalog.Initialize(libraryXMLPath);
			Console.WriteLine(String.Format("new FlatCatalog() -> {0} ms", watch.GetSplitMilliseconds()));

			PersistentIDCollection trackIDs = catalog.FindTracksByArtist("Guster");
			Console.WriteLine(String.Format("FindTracksByArtist/Flat -> {0} ms", watch.GetSplitMilliseconds()));

			Assert.IsNotNull(trackIDs);
			Assert.AreNotEqual(0, trackIDs.Count);
			Console.WriteLine(String.Format("Found {0} tracks", trackIDs.Count));
			ReportPrivateDelta();

			// TerseCatalog

			catalog = new TerseCatalog();
			catalog.Initialize(libraryXMLPath);
			Console.WriteLine(String.Format("new TerseCatalog() -> {0} ms", watch.GetSplitMilliseconds()));

			trackIDs = catalog.FindTracksByArtist("Guster");
			Console.WriteLine(String.Format("FindTracksByArtist/Terse -> {0} ms", watch.GetSplitMilliseconds()));

			Assert.IsNotNull(trackIDs);
			Assert.AreNotEqual(0, trackIDs.Count);
			Console.WriteLine(String.Format("Found {0} tracks", trackIDs.Count));
			ReportPrivateDelta();

			// Controller

			using (var controller = new Controller())
			{
				System.Threading.Thread.Sleep(2000); // let the controller initialize

				trackIDs = new PersistentIDCollection();
				foreach (Track track in controller.LibraryPlaylist.Tracks.Values)
				{
					if ((track != null) && !String.IsNullOrEmpty(track.Artist))
					{
						if (track.Artist.Equals("Guster"))
						{
							trackIDs.Add(track.PersistentID);
						}
					}
				}
			}

			Console.WriteLine(String.Format("FindTracksByArtist/Controller -> {0} ms", watch.GetSplitMilliseconds()));

			// iTunes

			trackIDs = new PersistentIDCollection();
			foreach (IITTrack track in itunes.LibraryPlaylist.Tracks)
			{
				if ((track != null) && !String.IsNullOrEmpty(track.Artist))
				{
					if (track.Artist.Equals("Guster"))
					{
						trackIDs.Add(GetPersistentID(track));
					}
				}
			}

			Console.WriteLine(String.Format("FindTracksByArtist/iTunes -> {0} ms", watch.GetSplitMilliseconds()));

			Assert.IsNotNull(trackIDs);
			Assert.AreNotEqual(0, trackIDs.Count);
			Console.WriteLine(String.Format("Found {0} tracks", trackIDs.Count));
			ReportPrivateDelta();
		}


		private PersistentID GetPersistentID (IITObject obj)
		{
			object refobj = obj;
			int high, low;
			itunes.GetITObjectPersistentIDs(ref refobj, out high, out low);
			return new PersistentID(high, low);
		}


		//FindITunesTracksByAlbum-----------------------------------------------------------------
		//Completed in 29889.8081351461 ms
		//FindITunesTracksByAlbum Completed: Passed-----------------------------------------------

		[TestMethod]
		public void FindITunesTracksByAlbum ()
		{
			List<Track> tracks = new List<Track>();

			foreach (IITTrack track in itunes.LibraryPlaylist.Tracks)
			{
				if (track.Album != null)
				{
					if (track.Album.ToLower().Equals("ganging up on the sun"))
					{
						tracks.Add(new Track(track));
					}
				}
			}
		}


		[TestMethod]
		public void FindCatalogedTracksByPlaylist ()
		{
			// FlatCatalog

			ICatalog catalog = new FlatCatalog();
			catalog.Initialize(libraryXMLPath);
			Console.WriteLine(String.Format("new FlatCatalog() -> {0} ms", watch.GetSplitMilliseconds()));

			var tracks = new List<IITTrack>();

			PersistentID pid = GetPlaylistPersistentID("ituner");
			PersistentIDCollection persistentIDs = catalog.FindTracksByPlaylist(pid);

			foreach (PersistentID persistentID in persistentIDs)
			{
				IITTrack itrack = itunes.LibraryPlaylist.Tracks.get_ItemByPersistentID(
					persistentID.HighBits, persistentID.LowBits);

				tracks.Add(itrack);
			}

			Console.WriteLine(String.Format("FindTracksByPlaylist -> {0} ms", watch.GetSplitMilliseconds()));

			Assert.IsNotNull(persistentIDs);
			Assert.AreNotEqual(0, persistentIDs.Count);
			Console.WriteLine(String.Format("Found {0} tracks", persistentIDs.Count));

			watch.Stop();
			Console.WriteLine(String.Format("Completed in {0} ms", watch.GetElapsedMilliseconds()));

			// iTunes

			watch.Reset();
			watch.Start();

			IITTrackCollection itracks =
				itunes.LibrarySource.Playlists.get_ItemByName("ituner").Tracks;

			Console.WriteLine(String.Format("get_ItemByName -> {0} ms", watch.GetSplitMilliseconds()));

			watch.Stop();
			Console.WriteLine(String.Format("Completed in {0} ms", watch.GetElapsedMilliseconds()));
		}

		#endregion Linq Speed Tests



		[TestMethod]
		public void ReloadPlaylist ()
		{
			// 46CD262697DA37D9 == Classical Music
			PersistentID persistentID = PersistentID.Parse("46CD262697DA37D9");

			XElement playlist = null;

			try
			{
				using (Stream stream = File.Open(libraryXMLPath,
					FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					var settings = new XmlReaderSettings();
					settings.IgnoreComments = true;
					settings.IgnoreProcessingInstructions = true;
					settings.IgnoreWhitespace = true;
					settings.DtdProcessing = DtdProcessing.Ignore;

					using (var reader = XmlReader.Create(stream, settings))
					{
						// read into //plist/dict and stop at the first ./array
						// this is the playlists collection

						if (reader.ReadToDescendant("array"))
						{
							// by using this subreader and pumping the stream through
							// XElement.ReadFrom, we should avoid the Large Object Heap

							using (var subreader = reader.ReadSubtree())
							{
								playlist = XElement.ReadFrom(subreader) as XElement;
							}
						}
					}
				}
			}
			catch (Exception exc)
			{
				Debug.WriteLine(exc.Message);
				return;
			}

			if (playlist != null)
			{
				XElement target =
					(from node in playlist
						.Elements("dict")
						.Elements("key")
					 where node.Value == "Playlist Persistent ID" &&
						 ((XElement)node.NextNode).Value == (string)persistentID
					 select node.Parent).FirstOrDefault();

				if (target != null)
				{
					var list =
						from node in target
							.Elements("array")
							.Elements("dict")
							.Elements("integer")
						select node.Value;
				}
			}
		}
	}
}
