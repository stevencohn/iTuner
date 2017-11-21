//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using Resx = Properties.Resources;


	/// <summary>
	/// Scanner to scan for and delete phantom tracks.  Also known as "dead tracks",
	/// phantom tracks are entries in iTunes for which no corresponding file exists.
	/// </summary>

	internal class PhantomScanner : ScannerBase
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

		public PhantomScanner (Controller controller, ICatalog catalog)
			: base(Resx.I_ScanPhantoms, controller, catalog)
		{
			base.description = Resx.ScanPhantoms;
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
				if (value == null)
				{
					// unlikely case.. dead code?
					albumFilter = null;
				}
				else
				{
					albumFilter = value.Trim().ToLower();
					base.name = Resx.I_ScanContextPhantoms;
					base.description = Resx.ScanPhantomByAlbum;
				}

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
				if (value == null)
				{
					// unlikely case.. dead code?
					artistFilter = null;
				}
				else
				{
					artistFilter = value.Trim().ToLower();
					base.name = Resx.I_ScanContextPhantoms;
					base.description = Resx.ScanPhantomByAlbum;
				}

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
				base.name = Resx.I_ScanContextPhantoms;
				base.description = Resx.ScanPhantomByPlaylist;
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
			Logger.WriteLine(base.name, "Phantom scanner beginning");

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
				Logger.WriteLine(base.name, "Phantom scanner completed");
			else
				Logger.WriteLine(base.name, "Phantom scanner cancelled");
		}


		/// <summary>
		/// Scan all tracks in the given playlist.
		/// </summary>
		/// <param name="list"></param>

		private void ScanTracks (PersistentIDCollection pids)
		{
			foreach (PersistentID persistentID in pids)
			{
				if (base.isActive)
				{
					using (Track track = libraryPlaylist.GetTrack(persistentID))
					{
						if ((track != null) && (track.Kind == TrackKind.File))
						{
							//Logger.WriteLine(base.name, "ScanTrying " + track.MakeKey());

							if (String.IsNullOrEmpty(track.Location) || !File.Exists(track.Location))
							{
								Logger.WriteLine(base.name,
									"Deleting phantom track " + track.MakeKey());

								try
								{
									if (ScannerBase.isLive)
									{
										// deletes library entry but not physical media file
										track.Delete();
									}
								}
								catch (Exception exc)
								{
									Logger.WriteLine(base.name,
										String.Format("Error deleting phantom {0}, {1}, {2}",
										track.Artist, track.Name, track.Album), exc);
								}
							}
						}
					}

					count++;
					base.ProgressPercentage = (int)((double)count / (double)total * 100.0);
				}
				else
				{
					Logger.WriteLine(base.name, "Phantom scanner cancelled while scanning");
					break;
				}
			}
		}
	}
}
