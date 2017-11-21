//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.ComponentModel;
	using iTunesLib;


	/// <summary>
	/// A safe wrapper of an IITPlaylist object.
	/// </summary>

	internal sealed class Playlist : Interaction, INotifyPropertyChanged
	{
		private IITPlaylist playlist;
		private bool isSelected;


		/// <summary>
		/// Initialize a new instance that wraps the given iTunes playlist COM object.
		/// </summary>
		/// <param name="playlist">An IITPlaylist instance.</param>

		public Playlist (IITPlaylist playlist)
			: base()
		{
			this.playlist = playlist;
			this.isSelected = false;
		}


		/// <summary>
		/// Interaction.Cleanup override; release reference to internal playlist.
		/// </summary>

		protected override void Cleanup (bool finalRelease)
		{
			Release(playlist);
			playlist = null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// This event is fired when the value of a property is changed.
		/// </summary>

		public event PropertyChangedEventHandler PropertyChanged;

	
		/// <summary>
		/// Gets the total length of all songs in the playlist (in seconds). 
		/// </summary>

		public int Duration
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return playlist.Duration;
				});
			}
		}


		/// <summary>
		/// Returns the index of the object in internal application order. 
		/// <para>
		/// You can pass this index to IITSourceCollection::Item(), IITPlaylistCollection::Item(), 
		/// or IITTrackCollection::Item() to retrieve this object.
		/// </para>
		/// <para>
		/// For tracks, this index is independent of play order. The play order index of a track
		/// can be retrieved using IITTrack::PlayOrderIndex().
		/// </para>
		/// </summary>

		[Obsolete("Not used by iTuner")]
		public int Index
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return playlist.Index;
				});
			}
		}


		/// <summary>
		/// Gets or sets the selected state of this item.  Used when showing playlists in
		/// a UI control to determine which ones are currently selected.
		/// </summary>

		public bool IsSelected
		{
			get
			{
				return isSelected;
			}

			set
			{
				isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}
	

		/// <summary>
		/// Gets the kind of the playlist.
		/// </summary>

		public PlaylistKind Kind
		{
			get
			{
				ITPlaylistKind kind = Invoke((Func<ITPlaylistKind>)delegate
				{
					return playlist.Kind;
				});

				switch (kind)
				{
					case ITPlaylistKind.ITPlaylistKindCD: return PlaylistKind.CD;
					case ITPlaylistKind.ITPlaylistKindDevice: return PlaylistKind.Device;
					case ITPlaylistKind.ITPlaylistKindLibrary: return PlaylistKind.Library;
					case ITPlaylistKind.ITPlaylistKindRadioTuner: return PlaylistKind.RadioTuner;
					case ITPlaylistKind.ITPlaylistKindUser: return PlaylistKind.User;
					default: return PlaylistKind.Unknown;
				}
			}
		}


		/// <summary>
		/// Gets or sets the publicily visible name of this playlist.
		/// </summary>

		public string Name
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return playlist.Name;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					playlist.Name = value;
				});
			}
		}


		/// <summary>
		/// Gets the four-part object ID of this playlist.
		/// </summary>

		public ObjectID ObjectID
		{
			get
			{
				return base.GetObjectID();
			}
		}


		/// <summary>
		/// Gets the unique persistent ID of this playlist.
		/// </summary>

		public PersistentID PersistentID
		{
			get
			{
				return base.GetPersistentID(playlist);
			}
		}


		/// <summary>
		/// Returns the ID that identifies the playlist. Valid for a playlist or track. Will
		/// be zero for a source. This is a runtime ID, it is only valid while the current
		/// instance of iTunes is running.
		/// </summary>

		public int PlaylistID
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return playlist.playlistID;
				});
			}
		}


		/// <summary>
		/// Gets or sets a Boolean value indicating whether songs in the playlist should be
		/// played in a random order.
		/// </summary>

		public bool Shuffle
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					return playlist.Shuffle;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					playlist.Shuffle = value;
				});
			}
		}


		/// <summary>
		/// Returns the total size of all songs in the playlist (in bytes).
		/// </summary>

		public double Size
		{
			get
			{
				return Invoke((Func<double>)delegate
				{
					return playlist.Size;
				});
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating whether this playlist is a smart playlist.
		/// </summary>

		public bool Smart
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					if (playlist.Kind == ITPlaylistKind.ITPlaylistKindUser)
					{
						return ((IITUserPlaylist)playlist).Smart;
					}

					return false;
				});
			}
		}

	
		/// <summary>
		/// Gets or sets the playback repeat mode.
		/// </summary>

		public ITPlaylistRepeatMode SongRepeat
		{
			get
			{
				return Invoke((Func<ITPlaylistRepeatMode>)delegate
				{
					return playlist.SongRepeat;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					playlist.SongRepeat = value;
				});
			}
		}


		/// <summary>
		/// Gets a Source object corresponding to the source that contains the playlist.
		/// </summary>

		public Source Source
		{
			get
			{
				return Invoke((Func<Source>)delegate
				{
					return new Source(playlist.Source);
				});
			}
		}


		/*
		 * // TODO: not used by iTuner
		 * public int SourceID { get; }
		 */


		/// <summary>
		/// Gets the kind of the playlist.
		/// </summary>

		public PlaylistKind SpecialKind
		{
			get
			{
				ITUserPlaylistSpecialKind kind = Invoke((Func<ITUserPlaylistSpecialKind>)delegate
				{
					if (playlist.Kind == ITPlaylistKind.ITPlaylistKindUser)
					{
						return ((IITUserPlaylist)playlist).SpecialKind;
					}

					return ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone;
				});

				switch (kind)
				{
					case ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindPurchases:
						return PlaylistKind.Purchased;

					case ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindFolder:
						return PlaylistKind.Folder;

					case ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone:
						return ((IITUserPlaylist)playlist).Smart ? PlaylistKind.Smart : PlaylistKind.None;
				}

				return PlaylistKind.None;
			}
		}
	

		/// <summary>
		/// Gets the total length of all songs in the playlist (in MM:SS format).
		/// </summary>

		public string Time
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return playlist.Time;
				});
			}
		}


		/// <summary>
		/// Gets the number of tracks in this playlist.
		/// </summary>

		public int TrackCount
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return playlist.Tracks.Count;
				});
			}
		}


		/*
		 * // TOOD: not used by iTuner
		 * public int TrackDatabaseID { get; }
		 */


		/*
		 * // TODO: not used by iTuner
		 *  public int TrackID { get; }
		 */


		/// <summary>
		/// Gets the collection of tracks in this playlst.
		/// </summary>

		public TrackCollection Tracks
		{
			get
			{
				return Invoke((Func<TrackCollection>)delegate
				{
					TrackCollection collection = new TrackCollection();
					foreach (IITTrack track in playlist.Tracks)
					{
						if (track != null)
						{
							collection.Add(new Track(track));
						}
					}
					return collection;
				});
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating whether this playlist is visible in the Source list.
		/// </summary>

		public bool Visible
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					return playlist.Visible;
				});
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================
		
		/// <summary>
		/// Adds a new track to this playlist as specified by the given file path.
		/// </summary>
		/// <param name="path">The file system path of the track to add.</param>
		/// <returns>
		/// A new Track describinging the newly added track or <b>null</b> if the track
		/// could not be added.
		/// </returns>

		public Track AddFile (string path)
		{
			var list = playlist as IITUserPlaylist;
			if (list == null)
			{
				return null;
			}

			return Invoke((Func<Track>)delegate
			{
				Track track = null;

				IITOperationStatus status = list.AddFile(path);
				while (status.InProgress)
				{
					// this is horrible!  How could Apple not provide a better way?
					System.Threading.Thread.Sleep(10);
				}

				if ((status.Tracks != null) && (status.Tracks.Count > 0))
				{
					// successfully imported raw file without conversion
					track = new Track(status.Tracks[1]);
				}

				return track;
			});
		}


		/// <summary>
		/// Add an existing track to the current playlist.  Used for importing.
		/// </summary>
		/// <param name="track"></param>
		/// <returns></returns>

		public Track AddTrack (Track track)
		{
			var list = playlist as IITUserPlaylist;
			if (list == null)
			{
				return null;
			}

			return Invoke((Func<Track>)delegate
			{
				object reftrack = track.AsInternal;
				IITTrack result = list.AddTrack(ref reftrack);

				if (result != null)
				{
					track = new Track(result);
				}

				return track;
			});
		}


		/// <summary>
		/// Delete this playlist.
		/// <para>
		/// <i>The instance is disposed by this method; callers should dereference immediately
		/// after this method returns.</i>
		/// </para>
		/// </summary>

		public void Delete ()
		{
			Invoke((Action)delegate
			{
				playlist.Delete();
			});
		}


		/// <summary>
		/// Used specifically to test for blocking operations in ScannerBase.
		/// This should block until user dismisses the current dialog box...
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>

		public Track GetTrack (int index)
		{
			return Invoke((Func<Track>)delegate
			{
				return new Track(playlist.Tracks.get_ItemByPlayOrder(index));
			});
		}


		/// <summary>
		/// Gets the track specified by its persistent ID.
		/// </summary>

		public Track GetTrack (PersistentID persistentID)
		{
			return Invoke((Func<Track>)delegate
			{
				return new Track(playlist.Tracks
					.get_ItemByPersistentID(persistentID.HighBits, persistentID.LowBits));
			});
		}


		/// <summary>
		/// Start playing the first track in this playlist. 
		/// </summary>

		public void PlayFirstTrack ()
		{
			Invoke((Action)delegate
			{
				playlist.PlayFirstTrack();
			});
		}


		/// <summary>
		/// Print this playlist.
		/// </summary>
		/// <param name="showPrintDialog">If true, display the print dialog.</param>
		/// <param name="printKind">The printout kind.</param>
		/// <param name="theme">
		/// The name of the theme to use. This corresponds to the name of a Theme combo box item
		/// in the print dialog for the specified printKind (e.g. "Track length").  This string
		/// cannot be longer than 255 characters, but it may be NULL or empty. 
		/// </param>

		public void Print (bool showPrintDialog, ITPlaylistPrintKind printKind, string theme)
		{
			Invoke((Action)delegate
			{
				playlist.Print(showPrintDialog, printKind, theme);
			});
		}


		/// <summary>
		/// Returns a collection containing the tracks with the specified text. 
		/// <para>
		/// If searchFields is ITPlaylistSearchFieldVisible , this is identical to the list of
		/// tracks displayed if the user enters the search text in the Search edit field in
		/// iTunes when this playlist is being displayed.
		/// </para>
		/// </summary>
		/// <param name="searchText">
		/// The text to search for. This string cannot be longer than 255 characters.
		/// </param>
		/// <param name="searchFields">
		/// Specifies which fields of each track should be searched for searchText.
		/// </param>
		/// <returns>
		/// Collection of Track objects. This will be empty if no tracks meet the search
		/// criteria.
		/// </returns>

		public TrackCollection Search (string searchText, ITPlaylistSearchField searchFields)
		{
			return Invoke((Func<TrackCollection>)delegate
			{
				TrackCollection collection = new TrackCollection();
				IITTrackCollection results = playlist.Search(searchText, searchFields);
				if (results != null)
				{
					foreach (IITTrack track in results)
					{
						if (track != null)
						{
							collection.Add(new Track(track));
						}
					}
				}
				return collection;
			});
		}


		/// <summary>
		/// Raises the PropertyChanged event when the specified property value is changed.
		/// </summary>
		/// <param name="name"></param>

		private void OnPropertyChanged (string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}


		/// <summary>
		/// For unit testing; returns the first specified number of tracks from the playlist.
		/// </summary>

		public TrackCollection _GetFirstTracks (int count)
		{
			return Invoke((Func<TrackCollection>)delegate
			{
				TrackCollection collection = new TrackCollection();
				foreach (IITTrack track in playlist.Tracks)
				{
					if (track != null)
					{
						collection.Add(new Track(track));
					}

					if (collection.Count >= count)
					{
						break;
					}
				}

				return collection;
			});
		}
	}
}
