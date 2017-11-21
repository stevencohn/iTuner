//************************************************************************************************
// Copyright © 2012 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Specialized;
	using System.IO;
	using iTuner.Amazon;
	using Resx = Properties.Resources;


	/// <summary>
	/// Scanner to fix missing and incorrect ID tags in media files.
	/// </summary>

	internal class InformationScanner : ScannerBase
	{
		private PersistentIDCollection pids;
		private Playlist libraryPlaylist;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scanner with the specified iTunes interface.
		/// </summary>
		/// <param name="itunes"></param>
		/// <param name="catalog"></param>

		public InformationScanner (
			Controller controller, ICatalog catalog, PersistentIDCollection pids)
			: base(Resx.I_ScanInformation, controller, catalog)
		{
			this.libraryPlaylist = controller.LibraryPlaylist;

			base.description = Resx.ScanInformation;
			base.tooltip = String.Format("{0}/{1}", (pids.Album ?? "?"), (pids.Artist ?? "?"));

			this.pids = pids;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Execute the scanner.
		/// </summary>

		public override void Execute ()
		{
			if (!NetworkStatus.IsAvailable)
			{
				Logger.WriteLine(base.name, "Information scanner skipped, network not available");
				return;
			}

			Logger.WriteLine(base.name, String.Format(
				"Information scanner beginning for {0} tracks on '{1}'/'{2}'",
				pids.Count, pids.Artist, pids.Album));

			ScanTracks(pids);

			libraryPlaylist = null;
			pids.Clear();
			pids = null;

			if (base.isActive)
				Logger.WriteLine(base.name, "Information scanner completed");
			else
				Logger.WriteLine(base.name, "Information scanner cancelled");
		}


		/// <summary>
		/// Scan all tracks in the given playlist.
		/// </summary>
		/// <param name="list"></param>

		private void ScanTracks (PersistentIDCollection pids)
		{
			var tagger = new Tagger();
			int total = pids.Count;
			int count = 0;
			
			foreach (PersistentID persistentID in pids)
			{
				if (!base.isActive)
				{
					Logger.WriteLine(base.name, "Information scanner cancelled while scanning");
					break;
				}

				using (Track track = libraryPlaylist.GetTrack(persistentID))
				{
					if ((track != null) && (track.Kind == TrackKind.File))
					{
						Logger.WriteLine(base.name, String.Format(
							"Fetching tag information for '{0}' ({1})",
							track.MakeKey(), track.UniqueID));

						try
						{
							// store into a temporary TrackFile so we can decide which
							// properties to update...

							var buffer = new TrackFile(track);
							tagger.RetrieveTags(buffer);

							Reconcile(track, buffer);
						}
						catch (Exception exc)
						{
							Logger.WriteLine(base.name,
								String.Format("Error fetching information {0}, {1}, {2}",
								track.Artist, track.Name, track.Album), exc);
						}
					}
				}

				count++;
				base.ProgressPercentage = (int)((double)count / (double)total * 100.0);
			}

			tagger = null;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="buffer"></param>

		private void Reconcile (Track track, TrackFile buffer)
		{
			if (!String.IsNullOrEmpty(buffer.Album) &&
				!buffer.Album.Equals(track.Album, StringComparison.InvariantCultureIgnoreCase))
			{
				Logger.WriteLine(Logger.Level.Debug, base.name,
					String.Format(" - updating {0} Album '{1}' to '{2}'",
					track.UniqueID, track.Album, buffer.Album));

				if (ScannerBase.isLive)
				{
					track.Album = buffer.Album;
				}
			}

			if (!String.IsNullOrEmpty(buffer.Artist) &&
				!buffer.Artist.Equals(track.Artist, StringComparison.InvariantCultureIgnoreCase))
			{
				Logger.WriteLine(Logger.Level.Debug, base.name,
					String.Format(" - updating {0} Artist '{1}'  to '{2}'",
					track.UniqueID, track.Artist, buffer.Artist));

				if (ScannerBase.isLive)
				{
					track.Artist = buffer.Artist;
				}
			}

			if (!String.IsNullOrEmpty(buffer.ArtistURL) &&
				!buffer.ArtistURL.Equals(track.ArtistURL, StringComparison.InvariantCultureIgnoreCase))
			{
				track.ArtistURL = buffer.ArtistURL;
				Logger.WriteLine(Logger.Level.Debug, base.name,
					String.Format(" - updating {0} ArtistURL to '{1}'",
					track.UniqueID, buffer.ArtistURL));

				if (ScannerBase.isLive)
				{
					track.ArtistURL = buffer.ArtistURL;
				}
			}

			if (!String.IsNullOrEmpty(buffer.Genre) &&
				!buffer.Genre.Equals(track.Genre, StringComparison.InvariantCultureIgnoreCase))
			{
				Logger.WriteLine(Logger.Level.Debug, base.name,
					String.Format(" - updating {0} Genre '{1}' to '{2}'",
					track.UniqueID, track.Genre, buffer.Genre));

				if (ScannerBase.isLive)
				{
					track.Genre = buffer.Genre;
				}
			}

			if (!String.IsNullOrEmpty(buffer.Title) &&
				!buffer.Title.Equals(track.Title, StringComparison.InvariantCultureIgnoreCase))
			{
				Logger.WriteLine(Logger.Level.Debug, base.name,
					String.Format(" - updating {0} Title '{1}' to '{2}'",
					track.UniqueID, track.Title, buffer.Title));

				if (ScannerBase.isLive)
				{
					track.Title = buffer.Title;
				}
			}

			if (track.Year != buffer.Year)
			{
				Logger.WriteLine(Logger.Level.Debug, base.name,
					String.Format(" - updating {0} Year '{1}' to '{2}'",
					track.UniqueID, track.Year, buffer.Year));

				if (ScannerBase.isLive)
				{
					track.Year = buffer.Year;
				}
			}
		}
	}
}
