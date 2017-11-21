//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.IO;
	using System.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner;
	using iTuner.iTunes;
	using TagLib;


	/// <summary>
	/// </summary>

	[TestClass]
	[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\AACTagReader.exe", "ThirdParty")]
	[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\libexpat.dll", "ThirdParty")]
	[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\mipcore.exe", "ThirdParty")]
	[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\genpuid.exe", "ThirdParty")]
	[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\taglib-sharp.dll", "ThirdParty")]
	public class LibrarianTests : TestBase
	{

		/// <summary>
		/// For ImportScanner developement, uses the TagLib API and the ICatalog.Search method
		/// to ensure we can find tracks by artist/album/title to check for duplicates.
		/// </summary>

		[TestMethod]
		public void PeruseLibrary ()
		{
			var controller = new Controller();
			System.Threading.Thread.Sleep(5000);

			var playlist = controller.LibraryPlaylist;
			var librarian = controller.Librarian;
			var catalog = librarian.Catalog;
			var tagger = new Tagger();

			// search the library...

			var extensions = catalog.FindExtensionsByPlaylist(controller.LibraryPlaylist.PersistentID);

			var filepaths =
				from e in Directory.EnumerateFiles(catalog.MusicPath, "*", SearchOption.AllDirectories)
				where extensions.Contains(Path.GetExtension(e))
				select e;

			Logger.WriteLine(String.Format("Found {0} possible tracks to analyze", filepaths.Count()));

			var random = new Random();
			int maxIndex = filepaths.Count() - 1;
			int numFound = 0;
			int numFailed = 0;
			int numRetrieved = 0;
			int numSimilar = 0;

#if true
			for (int index = 0; index < maxIndex; index++)
#else
			for (int index = 0; index < Math.Min(20, maxIndex); index++)
#endif
			{
				var trackFile = new TrackFile(filepaths.ElementAt(random.Next(maxIndex)));

				Logger.WriteLine();
				Logger.WriteLine(String.Format("Reading tags from [{0}]", trackFile.Location));

				using (TagLib.File tagFile = TagLib.File.Create(trackFile.Location))
				{
					if (tagFile != null)
					{
						trackFile.Album = tagFile.Tag.Album;
						trackFile.Artist = tagFile.Tag.FirstAlbumArtist;
						trackFile.Title = tagFile.Tag.Title;
						Logger.WriteLine(String.Format(" - Tag.Album  = [{0}]", tagFile.Tag.Album));
						Logger.WriteLine(String.Format(" - Tag.Artist = [{0}]", tagFile.Tag.FirstAlbumArtist));
						Logger.WriteLine(String.Format(" - Tag.Title  = [{0}]", tagFile.Tag.Title));
					}
				}

				if (String.IsNullOrEmpty(trackFile.Title) ||
					(String.IsNullOrEmpty(trackFile.Album) && String.IsNullOrEmpty(trackFile.Artist)))
				{
					Logger.WriteLine(String.Format(" . Retrieving tags for PUID [{0}]", trackFile.UniqueID));
					tagger.RetrieveTags(trackFile);
					numRetrieved++;
				}

				bool found = false;

				// can we find the track by artist/album/title?

				PersistentID pid = catalog.FindTrack(
					trackFile.Album, trackFile.Artist, trackFile.Title);

				if (found = !pid.IsEmpty)
				{
					Track track = playlist.GetTrack(pid);

					if (!track.Artist.Equals(trackFile.Artist))
					{
						Logger.WriteLine(String.Format(
							" ~ Similar track '{0}', '{1}', '{2}'",
							track.Artist, track.Album, track.Title));
						numSimilar++;
					}
					else
					{
						Logger.WriteLine(String.Format(
							" = Found track '{0}', '{1}', '{2}'",
							track.Artist, track.Album, track.Title));
					}

					numFound++;
				}

				if (!found)
				{
					numFailed++;
					Logger.WriteLine(String.Format(
						" x Could not find track '{0}', '{1}', '{2}'",
						trackFile.Artist, trackFile.Album, trackFile.Title));
				}
			}

			Logger.WriteLine();
			Logger.WriteLine(String.Format("... {0} found", numFound));
			Logger.WriteLine(String.Format("... {0} not found", numFailed));
			Logger.WriteLine(String.Format("... {0} similar", numSimilar));
			Logger.WriteLine(String.Format("... {0} retrieved", numRetrieved));
		}
	}
}
