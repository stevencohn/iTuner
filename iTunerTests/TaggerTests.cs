//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner;
	using iTuner.iTunes;


	/// <summary>
	/// </summary>

	[DeploymentItem(@"ThirdParty\", @"ThirdParty")]
	[TestClass]
	public class TaggerTests : TestBase
	{

		/// <summary>
		/// </summary>

		[TestMethod]
		public void RetrieveEmbeddedTags ()
		{
			ITrackBasics track = new TrackFile(
				@"C:\Users\cohns\Music\iTunes\iTunes Media\Music\Adele\Rolling In the Deep - Single\01 Rolling In The Deep.mp3");

			Tagger.ReadFileTags(track);

			Tagger tagger = new Tagger();
			tagger.RetrieveTags(track);
			Assert.IsNotNull(track.UniqueID);
			Assert.IsNotNull(track.ArtistURL);
			Assert.AreNotEqual(0, track.Year);
		}


		/// <summary>
		/// 
		/// </summary>

		[TestMethod]
		[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\AACTagReader.exe", "ThirdParty")]
		[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\libexpat.dll", "ThirdParty")]
		[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\mipcore.exe", "ThirdParty")]
		[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\genpuid.exe", "ThirdParty")]
		[DeploymentItem(@"C:\iTuner\iTuner\ThirdParty\taglib-sharp.dll", "ThirdParty")]
		public void RunTagger ()
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

			int MaxTagged = 20;

			for (int index = 0, total = 0; (index < maxIndex) && (total < MaxTagged); index++)
			{
				// using the Location, find the track in our library so we can take advantage
				// of iTunes cataloging and tagging information

				var track = playlist.GetTrack(
					catalog.GetPersistentIDByLocation(filepaths.ElementAt(random.Next(maxIndex))));

				// convert the Track to a TrackFile so we don't actually change our iTunes
				// media files during this unit test

				var trackFile = new TrackFile(track);
				if (String.IsNullOrEmpty(trackFile.Location) || !File.Exists(trackFile.Location))
				{
					Logger.WriteLine(Logger.Level.Warn, "TEST", String.Format(
						"{0} Track location not found: [{1}]", index, trackFile.Location));
					Logger.WriteLine(String.Format(" ? track.Album  = [{0}]", track.Album));
					Logger.WriteLine(String.Format(" ? track.Artist = [{0}]", track.Artist));
					Logger.WriteLine(String.Format(" ? track.Title  = [{0}]", track.Title));
					Logger.WriteLine(String.Format(" ? track.Year   = [{0}]", track.Year));
					Logger.WriteLine(String.Format(" ? track.Genre  = [{0}]", track.Genre));
					continue;
				}

				Logger.WriteLine();
				Logger.WriteLine(String.Format("{0}. Reading tags from [{1}]", index, trackFile.Location));

				using (TagLib.File tagFile = TagLib.File.Create(trackFile.Location))
				{
					if (tagFile != null)
					{
						var tag = tagFile.Tag;
						trackFile.Album = tag.Album;
						trackFile.Artist = tag.FirstAlbumArtist;
						trackFile.Title = tag.Title;
						Logger.WriteLine(String.Format(" - Tag.Album  = [{0}]", tag.Album));
						Logger.WriteLine(String.Format(" - Tag.Artist = [{0}]", tag.FirstAlbumArtist));
						Logger.WriteLine(String.Format(" - Tag.Title  = [{0}]", tag.Title));
						Logger.WriteLine(String.Format(" - Tag.Number = [{0}]", tag.Track));
						Logger.WriteLine(String.Format(" - Tag.Year   = [{0}]", tag.Year));
						Logger.WriteLine(String.Format(" - Tag.Genre  = [{0}]", tag.FirstGenre));

						trackFile.Album = tag.Album;
						trackFile.Artist = tag.FirstAlbumArtist;
						trackFile.Title = tag.Title;
						trackFile.TrackNumber = (int)tag.Track;
						trackFile.Year = (int)tag.Year;
						trackFile.Genre = tag.FirstGenre;
					}
				}

				if (String.IsNullOrEmpty(trackFile.Title) ||
					String.IsNullOrEmpty(trackFile.Album) ||
					String.IsNullOrEmpty(trackFile.Artist))
				{
					Logger.WriteLine(String.Format(" . Retrieving tags for PUID [{0}]", trackFile.UniqueID));
					tagger.RetrieveTags(trackFile);

					if (String.IsNullOrEmpty(trackFile.UniqueID))
					{
						Logger.WriteLine(" > No tags found");
					}
					else
					{
						Logger.WriteLine(String.Format(" > Track.Album  = [{0}]", trackFile.Album));
						Logger.WriteLine(String.Format(" > Track.Artist = [{0}]", trackFile.Artist));
						Logger.WriteLine(String.Format(" > Track.Title  = [{0}]", trackFile.Title));
						Logger.WriteLine(String.Format(" > Track.Number = [{0}]", trackFile.TrackNumber));
						Logger.WriteLine(String.Format(" > Track.Year   = [{0}]", trackFile.Year));
						Logger.WriteLine(String.Format(" > Track.Genre  = [{0}]", trackFile.Genre));
						Logger.WriteLine(String.Format(" > ArtistURL    = [{0}]", trackFile.ArtistURL));

						if (trackFile.IsAnalyzed)
						{
							XNamespace ns = tagger._PuidRoot.GetDefaultNamespace();

							var releases =
								from recording in tagger._PuidRoot
									 .Elements(ns + "puid")
									 .Elements(ns + "recording-list")
									 .Elements(ns + "recording")
								select recording;
#if Verbose
							if (releases.Count() > 1)
							{
								Logger.WriteLine(String.Format(" * Found {0} recordings in the following", releases.Count()));
							}

							Logger.WriteLine("PuidRoot");
							Logger.WriteLine(tagger._PuidRoot.ToString(SaveOptions.None));

							if (tagger._ArtistRoot != null)
							{
								Logger.WriteLine("ArtistRoot");
								Logger.WriteLine(tagger._ArtistRoot.ToString(SaveOptions.None));
							}
							else
							{
								Logger.WriteLine(" * Artist information not found");
							}
#else
							if (releases.Count() > 1)
							{
								Logger.WriteLine(String.Format(" * Found {0} recordings", releases.Count()));
							}

							if (tagger._ArtistRoot == null)
							{
								Logger.WriteLine(" * Artist information not found");
							}
#endif
							// count tagged tracks
							total++;
						}
					}
				}
			}

			controller.Dispose(true);
		}
	}
}
