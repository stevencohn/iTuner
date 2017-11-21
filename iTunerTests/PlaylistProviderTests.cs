//************************************************************************************************
// Copyright © 2012 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Collections.Specialized;
	using System.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner.iTunes;


	/// <summary>
	///</summary>

	[TestClass]
	public class PlaylistProviderTests : TestBase
	{
		private static Controller controller = null;
		private static Playlist playlist = null;
		private StringCollection paths = new StringCollection();
		private string root = Environment.CurrentDirectory;


		//========================================================================================
		// Lifecycle
		//========================================================================================

		#region Lifecycle

		[ClassInitialize]
		public static void MyClassInitialize (TestContext testContext)
		{
			controller = new Controller();

			// find an appropriate User playlist
			foreach (var list in controller.Playlists.Values)
			{
				if (list.Kind == PlaylistKind.User)
				{
					int count = list.TrackCount;

					if (playlist == null)
					{
						if ((count > 2) && (count < 1000))
						{
							playlist = list;
						}
					}
					else
					{
						if ((count > 2) && (count < 1000) && (count < playlist.TrackCount))
						{
							playlist = list;
						}
					}
				}
			}
		}


		[ClassCleanup]
		public static void MyClassCleanup ()
		{
			Console.WriteLine("**** Unit test shutdown");

			playlist.Dispose();
			playlist = null;

			controller.Dispose();
			controller = null;
		}

		#endregion Lifecycle


		//========================================================================================
		// Tests
		//========================================================================================

		[TestMethod]
		public void PlaylistProviders ()
		{
			TestProvider("M3U");
			TestProvider("PLS");
			TestProvider("WPL");
			TestProvider("ZPL");
		}


		public void TestProvider (string key)
		{
			string path;
			int count;

			Console.WriteLine("... Writing {0} playlist", key);
			using (var writer = PlaylistProviderFactory.CreateWriter(
				key, root, "Playlist", true))
			{
				var coll = playlist._GetFirstTracks(10);
				var tracks = coll.Values;

				foreach (Track track in tracks)
				{
					path = Path.Combine(root, Path.GetFileName(track.Location));
					writer.Add(track, path);
					paths.Add(path);
				}
			}

			Console.WriteLine("... Reading {0} playlist", key);
			path = Path.Combine(root, "Playlist." + key.ToLower());
			using (var reader = PlaylistProviderFactory.CreateReader(path))
			{
				count = 0;
				while ((path = reader.GetNext()) != null)
				{
					Assert.IsTrue(paths.Contains(path));
					count++;
				}
			}

			Assert.AreEqual(paths.Count, count);

			paths.Clear();
		}
	}
}
