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
	/// Scanner to scan for album artwork.
	/// </summary>

	internal class ArtworkScanner : ScannerBase
	{
		private string albumFilter;
		private string artistFilter;
		private Playlist libraryPlaylist;
		private PersistentID playlistFilter;
		private int count;
		private int total;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scanner with the specified iTunes interface.
		/// </summary>
		/// <param name="itunes"></param>
		/// <param name="catalog"></param>

		public ArtworkScanner (Controller controller, ICatalog catalog)
			: base(Resx.I_ScanArtwork, controller, catalog)
		{
			base.description = Resx.ScanArtwork;
			base.tooltip = String.Empty;

			this.albumFilter = null;
			this.artistFilter = null;
			this.playlistFilter = PersistentID.Empty;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Sets the album name filter for the scanner.  This must be set prior to execution.
		/// </summary>

		public string AlbumFilter
		{
			set
			{
				albumFilter = value.Trim().ToLower();
				base.tooltip = String.Format("{0}/{1}", (albumFilter ?? "?"), (artistFilter ?? "?"));
			}
		}


		/// <summary>
		/// Sets the artist name filter for the scanner.  This must be set prior to execution.
		/// </summary>

		public string ArtistFilter
		{
			set
			{
				artistFilter = value.Trim().ToLower();
				base.tooltip = String.Format("{0}/{1}", (albumFilter ?? "?"), (artistFilter ?? "?"));
			}
		}


		/// <summary>
		/// Sets the playlist filter for the scanner.  This must be set prior to execution.
		/// </summary>

		public PersistentID PlaylistFilter
		{
			set
			{
				playlistFilter = value;
				base.tooltip = playlistFilter;
			}
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
				Logger.WriteLine(base.name, "Artwork scanner skipped, network not available");
				return;
			}

			Logger.WriteLine(base.name, "Artwork scanner beginning");

			libraryPlaylist = controller.LibraryPlaylist;
			PersistentIDCollection pids;

			if (!String.IsNullOrEmpty(albumFilter) && !String.IsNullOrEmpty(artistFilter))
			{
				pids = catalog.FindTracksByAlbum(albumFilter, artistFilter);
				total = pids.Count;

				Logger.WriteLine(base.name, String.Format(
					"Analyzing album '{0}' by '{1}' with {2} tracks",
					albumFilter, artistFilter, total));
			}
			else if (!playlistFilter.IsEmpty)
			{
				pids = catalog.FindTracksByPlaylist(playlistFilter);
				total = pids.Count;

				Logger.WriteLine(base.name, String.Format(
					"Analyzing playlist '{0}' with {1} tracks",
					catalog.FindPlaylistName(playlistFilter), total));
			}
			else
			{
				// if a track is deleted from a source's primary playlist, it will be deleted
				// from all playlist's in that source, so we only need look at main "Library"

				pids = catalog.FindTracksByPlaylist(libraryPlaylist.PersistentID);
				total = pids.Count;

				Logger.WriteLine(base.name, String.Format(
					"Analyzing Library playlist '{0}' with {1} tracks",
					libraryPlaylist.Name, total));
			}

			ScanTracks(pids);

			pids.Clear();
			pids = null;

			libraryPlaylist = null;

			if (base.isActive)
				Logger.WriteLine(base.name, "Artwork scanner completed");
			else
				Logger.WriteLine(base.name, "Artwork scanner cancelled");
		}


		/// <summary>
		/// Scan all tracks in the given playlist.
		/// </summary>
		/// <param name="list"></param>

		private void ScanTracks (PersistentIDCollection pids)
		{
			// Artwork must be applied to every track in an album.
			// Although we may apply artwork to the CurrentTrack, it will not show up in the
			// iTunes artwork viewer until context is moved to another track and then the user
			// selects or plays that first track again.

			foreach (PersistentID persistentID in pids)
			{
				if (!base.isActive)
				{
					Logger.WriteLine(base.name, "Artwork scanner cancelled while scanning");
					break;
				}

				using (Track track = libraryPlaylist.GetTrack(persistentID))
				{
					if ((track != null) && (track.Kind == TrackKind.File))
					{
						string album = track.Album;
						string artist = track.Artist;

						string key = track.MakeKey();
						if (String.IsNullOrEmpty(track.Artwork))
						{
							Logger.WriteLine(base.name, "Fetching album artwork for " + key);

							try
							{
								if (ScannerBase.isLive)
								{
									track.Artwork = GetArtworkPath(artist, album);
								}
							}
							catch (Exception exc)
							{
								Logger.WriteLine(base.name,
									String.Format("Error fetching artwork {0}, {1}, {2}",
									artist, track.Name, album), exc);
							}
						}
						else
						{
							Logger.WriteLine(base.name, "Album already has artwork " + key);
						}
					}
				}

				count++;
				base.ProgressPercentage = (int)((double)count / (double)total * 100.0);
			}
		}


		private string GetArtworkPath (string artist, string album)
		{
			ArtworkService service = new ArtworkService();
			byte[] data = service.GetArtwork(artist, album);

			if ((data != null) && (data.Length > 0))
			{
				// store the image in CommonDataPath so all users on the machine can take
				// advantage of this cache...
				// make sure the filename is 'cleaned' because some tracks have quite a random
				// array of characters in their names!

				string path = Path.Combine(
					PathHelper.CommonDataPath,
					PathHelper.CleanFileName(
						String.Format(
							"{0}_{1}{2}",
							artist, album, Path.GetExtension(service.FileName))
					)
				);

				if (File.Exists(path))
				{
					File.Delete(path);
				}

				using (var writer = File.Create(path))
				{
					writer.Write(data, 0, data.Length);
				}

				return path;
			}

			return null;
		}
	}
}
