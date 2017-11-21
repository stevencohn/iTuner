//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Resx = Properties.Resources;


	/// <summary>
	/// Scan for and remove duplicate tracks.  Duplicates are not deleted
	/// but rather moved to a special iTuner archive directory.
	/// </summary>

	internal class DuplicateScanner : ScannerBase
	{
		private string albumFilter;
		private string artistFilter;
		private PersistentID playlistFilter;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scaner with the specified iTunes interface.
		/// </summary>
		/// <param name="itunes"></param>
		/// <param name="catalog"></param>

		public DuplicateScanner (Controller controller, ICatalog catalog)
			: base(Resx.I_ScanDuplicates, controller, catalog)
		{
			base.description = Resx.ScanDuplicates;

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
					base.name = Resx.I_ScanContextDuplicates;
					base.description = Resx.ScanDuplicatesByAlbum;
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
					base.name = Resx.I_ScanContextDuplicates;
					base.description = Resx.ScanDuplicatesByAlbum;
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
				base.name = Resx.I_ScanContextDuplicates;
				base.description = Resx.ScanDuplicatesByPlaylist;
				base.tooltip = playlistFilter;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Execute the scanner.
		/// </summary>
		/// <remarks>
		/// Duplicates are first identified by comparing Artist, Name, and Album.  These are
		/// all stored as (ID3) tags within the media file.  So these must match or the files
		/// are already different and any further analysis (checksum or MD5) would be useless.
		/// </remarks>

		public override void Execute ()
		{
			try
			{
				Logger.WriteLine(base.name, "Duplicate scanner beginning");

				int total;
				int count = 0;
				Track candidate;
				Candidates candidates = new Candidates();
				TrackCollection duplicates = new TrackCollection();
				Playlist libraryPlaylist = controller.LibraryPlaylist;
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
					pids = catalog.FindTracksByPlaylist(libraryPlaylist.PersistentID);
					total = pids.Count;

					Logger.WriteLine(base.name,
						String.Format("Analyzing {0} tracks", total));
				}

				foreach (PersistentID persistentID in pids)
				{
					if (base.isActive)
					{
						Track track = libraryPlaylist.GetTrack(persistentID);

						if ((track != null) && (track.Kind == TrackKind.File))
						{
							candidate = track;
							candidate.IsBuffered = true;

							// need to skip phantoms or Track.IsBetterThan will hang
							if (!String.IsNullOrEmpty(candidate.Location))
							{
								Track demoted = candidates.Reconcile(candidate);
								if (demoted != null)
								{
									duplicates.Add(demoted);

									// DO NOT dispose demoted here because that would corrupt
									// the instance we just stored in the duplicates collection
								}
							}

							// DO NOT dispose candidate here because the instance is stored in
							// either the candidates or duplicates collection and disposing
							// would corrupt that reference
						}

						// DO NOT dispose track here because the instance is stored in
						// either the candidates or duplicates collection and disposing
						// would corrupt that reference

						count++;
						base.ProgressPercentage = (int)((double)count / (double)total * 100.0);
					}
					else
					{
						Logger.WriteLine(base.name, "Duplicate scanner cancelled while scanning");
						break;
					}
				}

				if (base.isActive)
				{
					UpdateCandidates(candidates);
				}

				candidates.Clear();
				candidates = null;

				if (base.isActive)
				{
					ArchiveDuplicates(duplicates);
				}

				duplicates.Dispose();
				duplicates = null;

				pids.Clear();
				pids = null;

				Logger.WriteLine(base.name, "Duplicate scanner completed");
			}
			catch (Exception exc)
			{
				App.LogException(new SmartException(exc));
			}
		}


		#region UpdateCandidates

		/// <summary>
		/// After finishing this duplicate scanner, we may have candidate Tracks with
		/// good tag information retrieved from the online services.  So now apply these
		/// tags to the tracks and update the iTunes library.
		/// </summary>
		/// <param name="candidates"></param>

		private void UpdateCandidates (Candidates candidates)
		{
			Logger.WriteLine(base.name,
				String.Format("Updating buffered tags for {0} candidates", candidates.Count));

			foreach (Tracks tracks in candidates.Values)
			{
				foreach (Track track in tracks)
				{
					if (base.isActive && (track != null))
					{
						if (track.IsAnalyzed && track.IsBuffered)
						{
							if (ScannerBase.isLive)
							{
								track.ApplyBuffer();
							}
						}
					}
					else
					{
						break;
					}
				}

				if (!base.isActive)
				{
					break;
				}
			}

			Logger.WriteLine(base.name, "Buffered updates completed");
		}

		#endregion UpdateCandidates


		#region ArchiveDuplicates

		/// <summary>
		/// Archive all identified duplicate tracks from the main iTunes play list.
		/// </summary>
		/// <remarks>
		/// Duplicate files are moved from their current location into the pre-defined
		/// iTuner archive directory.  The relative path of each file is preserved to
		/// maintain context.
		/// </remarks>

		private void ArchiveDuplicates (TrackCollection duplicates)
		{
			foreach (Track duplicate in duplicates.Values)
			{
				if (base.isActive && (duplicate != null))
				{
					try
					{
						string location = duplicate.Location;
						bool fileIsSafe = false;

						// 1. archive the file

						if (String.IsNullOrEmpty(location))
						{
							Logger.WriteLine(base.name,
								"Removing phantom duplicate " + duplicate.MakeKey());

							// can safely remove library entry without affecting a physical file
							fileIsSafe = true;
						}
						else
						{
							string archive;
							if (location.StartsWith(base.catalog.MusicPath))
							{
								// file is currently under rootPath
								archive = Path.Combine(
									base.ArchivePath, location.Substring(base.catalog.MusicPath.Length));
							}
							else
							{
								// file is located external to rootPath
								string root = Path.GetPathRoot(location);
								archive = Path.Combine(
									base.ArchivePath, location.Substring(root.Length));
							}

							if (!File.Exists(archive))
							{
								string dir = Path.GetDirectoryName(archive);
								if (!Directory.Exists(dir))
								{
									if (ScannerBase.isLive)
									{
										Directory.CreateDirectory(dir);
									}
								}

								// stop playback to avoid "in-use" IOException
								if (controller.CurrentTrack != null)
								{
									if (duplicate.TrackID == controller.CurrentTrack.TrackID)
									{
										controller.Stop();
									}
								}

								if (ScannerBase.isLive)
								{
									//FileInfo file = new FileInfo(location);
									//if ((file.Attributes & FileAttributes.ReadOnly) > 0)
									//{
									//    file.Attributes ^= FileAttributes.ReadOnly;
									//}

									File.Move(location, archive);

									// can safely remove library entry without affecting a physical file
									fileIsSafe = true;
								}

								Logger.WriteLine(base.name, "Archived " + archive);
							}
						}

						// 2. delete library entry

						if (ScannerBase.isLive && fileIsSafe)
						{
							duplicate.Delete();
							duplicate.Dispose();
						}
					}
					catch (Exception exc)
					{
						Logger.WriteLine(base.name,
							String.Format("Error deleting {0}, {1}, {2}",
							duplicate.Artist, duplicate.Title, duplicate.Album),
							exc);
					}
				}
				else
				{
					Logger.WriteLine(base.name, "Duplicate scanner cancelled while cleaning");
					break;
				}
			}
		}

		#endregion ArchiveDuplicates


		//****************************************************************************************
		// Private classes
		//****************************************************************************************

		#region Tracks and Candidates

		/// <summary>
		/// Typedef of a list of tracks.
		/// </summary>

		private class Tracks : List<Track>
		{

			/// <summary>
			/// Removes and properly disposes all elements from the list.
			/// </summary>

			public new void Clear ()
			{
				foreach (Track track in this)
				{
					track.Dispose();
				}

				base.Clear();
			}
		}


		/// <summary>
		/// Ostensibly a list of lists.  There may exist multiple tracks sharing the same
		/// key but all be unique.  So each key may be associated with more than one track.
		/// </summary>

		private class Candidates : Dictionary<string, Tracks>
		{
			private Tagger tagger = null;


			/// <summary>
			/// Removes and properly disposes all keys and values from the Dictionary
			/// </summary>

			public new void Clear ()
			{
				foreach (Tracks tracks in base.Values)
				{
					tracks.Clear();
				}

				base.Clear();
			}


			/// <summary>
			/// Compare the suspect track to existing candidates with the same key
			/// to determine which is most likely the better to preserve, if not both.
			/// </summary>
			/// <param name="key"></param>
			/// <param name="suspect"></param>
			/// <returns>
			/// The track to add to the duplicates list; this may be either the given suspect
			/// or one of the demoted candidates - in this latter case, the suspect would have
			/// swapped places with the demoted candidate in the collection. Or <b>null</b>
			/// if the given suspect is a valid candidate; in this case, the caller need not do
			/// aything further since we add it to the collection here.
			/// </returns>

			public Track Reconcile (Track suspect)
			{
				bool isTernary;
				string key = suspect.MakeKey(out isTernary);

				// promote first suspect to candidate
				if (!base.ContainsKey(key))
				{
					Add(key, suspect);
					return null;
				}

				int i = 0;
				Track candidate;
				Track demoted = null;
				Tracks tracks = base[key];

				while ((i < tracks.Count) && (demoted == null))
				{
					candidate = tracks[i];
					if (isTernary)
					{
						// SIMPLE CASE: duplicates matching all indexes require
						// only simple comparison heuristics

						if (suspect.Duration == candidate.Duration)
						{
							if (suspect.IsBetterThan(candidate))
							{
								// replace candidate with our preferred suspect
								tracks[i] = suspect;
								demoted = candidate;
							}
							else
							{
								// consider the suspect as our duplicate
								demoted = suspect;
							}

							// if both Tracks reference the same file then mark demoted as a
							// dead track so we remove it from library do not delete the file
							if (demoted.Location.Equals(tracks[i].Location))
							{
								demoted.Location = null;
							}
						}
					}
					else
					{
						// COMPLEX CASE: update ID3 tags from online providers
						// and compare using PUIDs and other details

						if (tagger == null)
						{
							tagger = new Tagger();
						}

						if (!candidate.IsAnalyzed)
						{
							tagger.RetrieveTags(candidate);
						}

						if (!suspect.IsAnalyzed)
						{
							tagger.RetrieveTags(suspect);
						}

						if (!String.IsNullOrEmpty(suspect.UniqueID) &&
							!String.IsNullOrEmpty(candidate.UniqueID))
						{
							if (suspect.UniqueID.Equals(candidate.UniqueID))
							{
								if (suspect.IsBetterThan(candidate))
								{
									// replace candidate with our preferred suspect
									tracks[i] = suspect;
									demoted = candidate;
								}
								else
								{
									// consider the suspect as our duplicate
									demoted = suspect;
								}

								// if both Tracks reference the same file then mark demoted as a
								// dead track so we remove it from library do not delete the file
								if (demoted.Location.Equals(tracks[i].Location))
								{
									demoted.Location = null;
								}
							}
						}
						else
						{
							// else we'll move suspect into a new candidate slot
							// for later comparison, which is done below...
							// meanwhile, we'll just log here

							if (String.IsNullOrEmpty(suspect.UniqueID))
							{
								Logger.WriteLine(Resx.I_ScanDuplicates, String.Format(
									"No tags found for suspect '{0}'", suspect.MakeKey()));
							}

							if (String.IsNullOrEmpty(candidate.UniqueID))
							{
								Logger.WriteLine(Resx.I_ScanDuplicates, String.Format(
									"No tags found for candidate '{0}'", candidate.MakeKey()));
							}
						}
					}

					i++;
				}

				if (demoted == null)
				{
					// similar track not found so add suspect for later comparison

					// last chance to promote; apply buffered tags and reassess key
					if (suspect.IsAnalyzed && suspect.IsBuffered)
					{
						suspect.ApplyBuffer();
						key = suspect.MakeKey(out isTernary);
						Add(key, suspect);
					}
				}

				return demoted;
			}


			/// <summary>
			/// Add the given track to the appropriate sub-list.
			/// </summary>
			/// <param name="key"></param>
			/// <param name="track"></param>

			private void Add (string key, Track track)
			{
				Tracks tracks;
				if (base.ContainsKey(key))
				{
					tracks = base[key];
				}
				else
				{
					tracks = new Tracks();
					base.Add(key, tracks);
				}

				tracks.Add(track);
			}
		}

		#endregion Track and Candidates
	}
}
