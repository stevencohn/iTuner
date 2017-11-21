//************************************************************************************************
// Copyright © 2012 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using Resx = iTuner.Properties.Resources;


	/// <summary>
	/// Import the tracks specified by the given playlist.
	/// </summary>

	internal class ImportPlaylistScanner : ScannerBase
	{
		private string playlistPath;				// path of playlist file to import

		private IPlaylistReader reader;				// playlist writer


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scanner.
		/// </summary>
		/// <param name="controller">The iTunes controller.</param>
		/// <param name="catalog"></param>
		/// <param name="path">The path of the playlist cataloging the tracks to import.</param>

		public ImportPlaylistScanner (
			Controller controller, ICatalog catalog, string playlistPath)
			: base(Resx.I_ScanImportPlaylist, controller, catalog)
		{
			base.description = Resx.ScanImportPlaylist;
			base.tooltip = playlistPath;

			this.playlistPath = playlistPath;
		}


		//========================================================================================
		// Execute()
		//========================================================================================

		/// <summary>
		/// Execute the scanner.
		/// </summary>

		public override void Execute ()
		{
			Logger.WriteLine(base.name,
				String.Format("Import beginning, path '{0}'", playlistPath));

			reader = PlaylistProviderFactory.CreateReader(playlistPath);

			try
			{
				ExecuteInternal();
			}
			catch (Exception exc)
			{
				Logger.WriteLine(base.name, exc);
			}
			finally
			{
				if (reader != null)
				{
					reader.Dispose();
					reader = null;
				}

				base.ProgressPercentage = 100;
				UpdateProgress(Resx.Completed);

				Logger.WriteLine(base.name, "Import completed");
			}
		}


		/// <summary>
		/// 
		/// </summary>

		private void ExecuteInternal ()
		{
			var playlist = controller.CreatePlaylist(
				Path.GetFileNameWithoutExtension(playlistPath));

			if (playlist == null)
			{
				Logger.WriteLine(Logger.Level.Error, base.name, "Error creating playlist");
				return;
			}

			int count = 0;						// tracks successfully imported
			int failed = 0;						// tracks failed import
			int numFound = 0;					// tracks found in library
			int numRetrieved = 0;				// tracks retrieved from GenPUID/MusicBrainz
			int numSimilar = 0;					// tracks identified using similar artists
			int numFailed = 0;					// tracks unidentified

			string path;
			string description = String.Empty;
			Tagger tagger = null;

			while ((path = reader.GetNext()) != null)
			{
				if (!base.isActive)
				{
					// scanner cancelled by user
					break;
				}

				if (!File.Exists(path))
				{
					Logger.WriteLine(base.name, "Could not find " + path);
					continue;
				}

				base.UpdateProgress(Path.GetFileNameWithoutExtension(path));

				var trackFile = new TrackFile(path);
				Tagger.ReadFileTags(trackFile);

				if (String.IsNullOrEmpty(trackFile.Title) ||
					(String.IsNullOrEmpty(trackFile.Album) && String.IsNullOrEmpty(trackFile.Artist)))
				{
					Logger.WriteLine(Logger.Level.Debug, base.name,
						"retrieving tag for " + trackFile.Location);

					if (tagger == null)
					{
						tagger = new Tagger();
					}

					tagger.RetrieveTags(trackFile);
					numRetrieved++;
				}

				PersistentID pid = catalog.FindTrack(
					trackFile.Album, trackFile.Artist, trackFile.Title);

				if (pid.IsEmpty)
				{
					numFailed++;
					Logger.WriteLine(base.name,
						String.Format("Could not find track '{0}', '{1}', '{2}'",
						trackFile.Artist, trackFile.Album, trackFile.Title));

					continue;
				}

				Track track = controller.LibraryPlaylist.GetTrack(pid);
				numFound++;

				if (track.Artist.Equals(trackFile.Artist))
				{
					Logger.WriteLine(Logger.Level.Debug, base.name,
						String.Format("Found track ({0} | {1} | {2})",
						track.Artist, track.Album, track.Title));
				}
				else
				{
					Logger.WriteLine(Logger.Level.Debug, base.name,
						String.Format("Found similar track ({0} | {1} | {2})",
						track.Artist, track.Album, track.Title));

					numSimilar++;
				}

				try
				{
					track = playlist.AddTrack(track);
					if (track == null)
					{
						failed++;

						Logger.WriteLine(Logger.Level.Error, base.name,
							String.Format("Error importing {0}", path));
					}
					else
					{
						description = String.Format("{0}, {1}", track.Name, track.Artist);
						count++;

						Logger.WriteLine(base.name, String.Format("Imported {0}", description));
					}
				}
				catch (Exception)
				{
					// TODO: ?
				}
				finally
				{
					track.Dispose();
				}
			}

			base.UpdateProgress(Resx.Completed);
			playlist.Dispose();

			Logger.WriteLine(base.name,
				String.Format("- found:{0}, failed:{1}, similar:{2}, retrieved:{3}",
				numFound, numFailed, numSimilar, numRetrieved));
		}
	}
}
