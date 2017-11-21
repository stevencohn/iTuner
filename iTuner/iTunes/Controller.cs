//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Configuration;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using System.Runtime.InteropServices;
	using System.Timers;
	using iTunesLib;
	using Resx = Properties.Resources;

	internal enum InteractionDisabledReason
	{
		Other,
		Dialog,
		Quitting
	}

	internal delegate void TrackHandler (Track track);
	internal delegate void InteractionDisabledHandler (InteractionDisabledReason reason);
	internal delegate void InteractionEnabledHandler ();


	//********************************************************************************************
	// class Controller
	//********************************************************************************************

	/// <summary>
	/// Thread-safe and COM-aware bindable wrapper for iTunesAppClass with track monitoring
	/// and automatic lyric discovery.
	/// </summary>

	internal sealed class Controller : Interaction, INotifyPropertyChanged
	{
		private const int CommunicationErr = -2147417848;

		private const int MinVolume = 0;
		private const int MaxVolume = 100;
		private const int VolumeDelta = 5;

		private const int BeepFrequence = 1000;
		private const int BeepDuration = 100;

		private Librarian librarian;                    // library organizer
		private LyricEngine engine;                     // lyric retrieval engine
		private Timer timer;                            // track update timer

		private Track track;                            // current track
		private Playlist libraryPlaylist;               // the LibraryPlaylist
		private string libraryXmlPath;                  // the LibraryXMLPath

		private InteractionEnabledHandler enabledHandler;
		private InteractionDisabledHandler disabledHandler;

		private _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler quitHandler;
		private _IiTunesEvents_OnDatabaseChangedEventEventHandler databaseChangedHandler;
		private _IiTunesEvents_OnPlayerPlayEventEventHandler playHandler;
		private _IiTunesEvents_OnPlayerPlayingTrackChangedEventEventHandler trackChangedHandler;
		private _IiTunesEvents_OnPlayerStopEventEventHandler stopHandler;
		private _IiTunesEvents_OnSoundVolumeChangedEventEventHandler volumeHandler;

		#region enum PlayerContext

		/// <summary>
		/// Identifies the current known context of iTunes.
		/// </summary>

		internal enum PlayerContext
		{
			None,
			Playlist,
			Track
		}

		#endregion enum PlayerContext


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new iTunesAppClass safe wrapper.
		/// </summary>

		public Controller ()
		{
			// timer needs to be established prior to wiring up host event handlers
			this.timer = new Timer(1000);
			this.timer.Elapsed += DoUpdatePlayer;
			this.timer.Enabled = IsPlaying;

			InitializeHost();

			this.enabledHandler = null;
			this.disabledHandler = null;

			this.engine = LyricEngine.CreateEngine();
			this.engine.LyricsUpdated += DoLyricsUpdated;
			this.engine.LyricsProgressReport += DoLyricsProgressReport;

			if (this.track != null)
			{
				if (!this.track.HasLyrics && NetworkStatus.IsAvailable)
				{
					this.engine.RetrieveLyrics(this.track);
				}
			}

			this.librarian = Librarian.Create(this);
		}


		/// <summary>
		/// Special routine to initialize the base static itunes instance and configure it
		/// for first-time use.
		/// </summary>

		private void InitializeHost ()
		{
			var fresh = !IsHostRunning;

			var attemptConnection = true;
			var disconnected = ConfigurationManager.AppSettings["Disconnected"];
			if (disconnected != null)
			{
				attemptConnection = !disconnected.Trim().ToLower().Equals("true");
			}

			try
			{
				if (attemptConnection)
				{
					Interaction.itunes = new iTunesAppClass();
				}
			}
			catch (Exception exc)
			{
				COMException comex = exc as COMException;
				if ((comex != null) && (comex.ErrorCode == CommunicationErr))
				{
					throw new IncompatibleException(Resx.InitializationFailure);
				}

				// TODO: when a new version of iTunes loads, it anlayzes the entire library
				// this causes a special exception that we should catch here and display an
				// appropriate error message...

				throw new IncompatibleException(Resx.IncompatibleAssemblies);
			}

			if (itunes != null)
			{
				Version version = itunes.GetType().Assembly.GetName().Version;
				if (!Interaction.itunes.CheckVersion(version.Major, version.Minor))
				{
					string message = String.Format(Resx.IncompatibleVersions,
						Interaction.itunes.Version, version.Major, version.Minor);

					throw new IncompatibleException(message);
				}
			}

			InitializeInteraction();

			Track current = CurrentTrack;
			if (((current == null) || current.PersistentID.IsEmpty) &&
				(PlayerState != ITPlayerState.ITPlayerStatePlaying))
			{
				#region TBD... Future
				//if (Settings.Default.PlayerTrackID > 0)
				//{
				//    IITTrack track =
				//        itunes.LibraryPlaylist.Tracks.get_ItemByPersistentID(
				//        ((PersistentID)Settings.Default.PlayerTrackID).HighBits,
				//        ((PersistentID)Settings.Default.PlayerTrackID).LowBits);

				//    // it appears that we cannot call track.Play().  Something about
				//    //"computer not authorized to play blah blah... 
				//    track.Play();
				//    itunes.Pause();
				//}
				//else
				#endregion

				Play();
				Pause();
			}

			this.track = current;

			quitHandler = DoQuit;
			databaseChangedHandler = DoDatabaseChanged;
			playHandler = DoPlayerPlay;
			trackChangedHandler = DoTrackChanged;
			stopHandler = DoPlayerStopped;
			volumeHandler = DoSoundVolumeChanged;

			OnAboutToPromptUserToQuitEvent += quitHandler;
			OnDatabaseChangedEvent += databaseChangedHandler;
			OnPlayerPlayEvent += playHandler;
			OnPlayerPlayingTrackChangedEvent += trackChangedHandler;
			OnPlayerStopEvent += stopHandler;
			OnSoundVolumeChangedEvent += volumeHandler;

			if (fresh)
			{
				// if we started iTunes then presume we can minimize it without surprise
				if (itunes != null)
				{
					itunes.BrowserWindow.Minimized = true;
				}
			}
		}


		#region Lifecycle

		protected override void Cleanup (bool finalRelease)
		{
			if (timer != null)
			{
				timer.Elapsed -= DoUpdatePlayer;
				timer.Dispose();
				timer = null;
			}

			if (librarian != null)
			{
				librarian.Dispose();
				librarian = null;
			}

			if (engine != null)
			{
				engine.LyricsUpdated -= DoLyricsUpdated;
				engine.LyricsProgressReport -= DoLyricsProgressReport;
				engine.Dispose();
				engine = null;
			}

			if (track != null)
			{
				Release(track);
			}

			try
			{
				OnAboutToPromptUserToQuitEvent -= quitHandler;
				OnDatabaseChangedEvent -= databaseChangedHandler;
				OnPlayerPlayEvent -= playHandler;
				OnPlayerPlayingTrackChangedEvent -= trackChangedHandler;
				OnPlayerStopEvent -= stopHandler;
				OnSoundVolumeChangedEvent -= volumeHandler;
			}
			catch
			{
				// no-op
				// Might happen if Alt-F4 is used to close the AppWindow
			}

			quitHandler = null;
			databaseChangedHandler = null;
			playHandler = null;
			trackChangedHandler = null;
			stopHandler = null;
			volumeHandler = null;
		}

		#endregion Lifecycle


		//========================================================================================
		// Events/Properties
		//========================================================================================

		#region Events

		/// <summary>
		/// Fired at each stage of discovery where a stage begins when a new provider is
		/// utilized to retrieve lyrics.
		/// </summary>

		public event LyricEngineProgress LyricsProgressReport;


		/// <summary>
		/// Fired when the lyrics for a particular song were discovered and the song updated.
		/// </summary>

		public event TrackHandler LyricsUpdated;


		/// <summary>
		/// This event is fired when the value of a property is changed.
		/// </summary>

		public event PropertyChangedEventHandler PropertyChanged;


		/// <summary>
		/// This event is fired when iTunes is about prompt the user to quit.  This event
		/// gives clients the opportunity to prevent the warning dialog prompt from occurring.
		/// </summary>

		public event EventHandler Quiting;


		/// <summary>
		/// This event is fired when a track begins playing.
		/// </summary>

		public event TrackHandler TrackPlaying;


		/// <summary>
		/// This event is fired when a track stops playing.
		/// </summary>

		public event TrackHandler TrackStopped;

		#endregion Events

		#region Event wrappers

		/// <summary>
		/// This event is fired when iTunes is about prompt the user to quit. 
		/// </summary>
		/// <remarks>
		/// iTuner uses this event to know when to abort.  It aborts without interruption.
		/// </remarks>

		public event
			_IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler OnAboutToPromptUserToQuitEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke(delegate
				{
					itunes.OnAboutToPromptUserToQuitEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke(delegate
				{
					try
					{
						itunes.OnAboutToPromptUserToQuitEvent -= value;
					}
					catch (Exception exc)
					{
						Logger.WriteLine("Controller",
							"Error unsubscribing from OnAboutToPromptUserToQuitEvent", exc);
					}
				});
			}
		}


		/// <summary>
		/// This event is fired when iTunes COM interop is disabled; usually because
		/// a UI dialog is currently displayed.
		/// </summary>

		public event InteractionDisabledHandler DisabledEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				if (disabledHandler == null)
				{
					Invoke(delegate
					{
						itunes.OnCOMCallsDisabledEvent += DoCOMCallsDisabled;
					});
				}

				disabledHandler =
					(InteractionDisabledHandler)Delegate.Combine(disabledHandler, value);
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				disabledHandler =
					(InteractionDisabledHandler)Delegate.Remove(disabledHandler, value);

				if (disabledHandler == null)
				{
					Invoke(delegate
					{
						itunes.OnCOMCallsDisabledEvent -= DoCOMCallsDisabled;
					});
				}
			}
		}

		private void DoCOMCallsDisabled (ITCOMDisabledReason ireason)
		{
			if (disabledHandler != null)
			{
				InteractionDisabledReason reason;
				switch (ireason)
				{
					case ITCOMDisabledReason.ITCOMDisabledReasonDialog:
						reason = InteractionDisabledReason.Dialog;
						break;

					case ITCOMDisabledReason.ITCOMDisabledReasonQuitting:
						reason = InteractionDisabledReason.Quitting;
						break;

					default:
						reason = InteractionDisabledReason.Other;
						break;
				}

				disabledHandler(reason);
			}
		}


		/// <summary>
		/// This event is fired when iTunes COM interop is enabled.
		/// </summary>

		public event InteractionEnabledHandler EnabledEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				if (enabledHandler == null)
				{
					Invoke(delegate
					{
						itunes.OnCOMCallsEnabledEvent += DoCOMCallsEnabled;
					});
				}

				enabledHandler =
					(InteractionEnabledHandler)Delegate.Combine(enabledHandler, value);
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				enabledHandler =
					(InteractionEnabledHandler)Delegate.Remove(enabledHandler, value);

				if (enabledHandler == null)
				{
					Invoke(delegate
					{
						itunes.OnCOMCallsEnabledEvent -= DoCOMCallsEnabled;
					});
				}
			}
		}

		private void DoCOMCallsEnabled ()
		{
			enabledHandler?.Invoke();
		}


		/// <summary>
		/// This event is fired when the iTunes database is changed.
		/// </summary>

		public event _IiTunesEvents_OnDatabaseChangedEventEventHandler OnDatabaseChangedEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke(delegate
				{
					itunes.OnDatabaseChangedEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke(delegate
				{
					try
					{
						itunes.OnDatabaseChangedEvent -= value;
					}
					catch { }
				});
			}
		}


		/// <summary>
		/// This event is fired when a track begins playing.  When iTunes switches to playing
		/// another track, you will received an ITEventPlayerStop event followed by an
		/// ITEventPlayerPlay event, unless it is playing joined CD tracks 
		/// </summary>

		public event _IiTunesEvents_OnPlayerPlayEventEventHandler OnPlayerPlayEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke(delegate
				{
					itunes.OnPlayerPlayEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke(delegate
				{
					try
					{
						itunes.OnPlayerPlayEvent -= value;
					}
					catch { }
				});
			}
		}


		/// <summary>
		/// This event is fired when information about the currently playing track has changed. 
		/// This event is fired when the user changes information about the currently playing
		/// track (e.g. the name of the track).
		/// </summary>

		public event
			_IiTunesEvents_OnPlayerPlayingTrackChangedEventEventHandler OnPlayerPlayingTrackChangedEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke(delegate
				{
					itunes.OnPlayerPlayingTrackChangedEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke(delegate
				{
					try
					{
						itunes.OnPlayerPlayingTrackChangedEvent -= value;
					}
					catch { }
				});
			}
		}


		/// <summary>
		/// This event is fired when a track stops playing.  When iTunes switches to playing
		/// another track, you will received an ITEventPlayerStop event followed by an
		/// ITEventPlayerPlay event, unless it is playing joined CD tracks
		/// </summary>

		public event _IiTunesEvents_OnPlayerStopEventEventHandler OnPlayerStopEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke(delegate
				{
					itunes.OnPlayerStopEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke(delegate
				{
					try
					{
						itunes.OnPlayerStopEvent -= value;
					}
					catch { }
				});
			}
		}


		/// <summary>
		/// This event is fired when the sound output volume has changed
		/// </summary>

		public event _IiTunesEvents_OnSoundVolumeChangedEventEventHandler OnSoundVolumeChangedEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke(delegate
				{
					itunes.OnSoundVolumeChangedEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke(delegate
				{
					try
					{
						itunes.OnSoundVolumeChangedEvent -= value;
					}
					catch { }
				});
			}
		}

		#endregion Events wrappers


		/// <summary>
		/// Gets or sets a Boolean value indicating whether iTunes should process APPCOMMAND
		/// Windows messages such as APPCOMMAND_MEDIA_PLAY, APPCOMMAND_MEDIA_PAUSE,
		/// APPCOMMAND_MEDIA_NEXTTRACK, etc
		/// </summary>

		public bool AppCommandMessageProcessingEnabled
		{
			get
			{
				return Invoke(() => itunes.AppCommandMessageProcessingEnabled);
			}

			set
			{
				Invoke(delegate
				{
					itunes.AppCommandMessageProcessingEnabled = value;
				});
			}
		}


		/// <summary>
		/// Gets the current context of iTunes: Track, Playlist, or unknown.
		/// </summary>

		public PlayerContext CurrentContext
		{
			get
			{
				PlayerContext context = PlayerContext.None;

				Invoke(delegate
				{
					if (itunes.CurrentTrack != null)
					{
						context = PlayerContext.Track;
					}
					else if (itunes.CurrentPlaylist != null)
					{
						context = PlayerContext.Playlist;
					}
				});

				return context;
			}
		}


		/// <summary>
		/// Gets or sets the current encoder.
		/// </summary>

		public Encoder CurrentEncoder
		{
			get
			{
				return Invoke(delegate
				{
					if (itunes.CurrentEncoder != null)
					{
						return new Encoder(itunes.CurrentEncoder);
					}

					return null;
				});
			}

			set
			{
				Invoke(delegate
				{
					itunes.CurrentEncoder = value.AsInternal;
				});
			}
		}

		/// <summary>
		/// Gets the current playlist or <b>null</b> if no playlist is yet selected.
		/// </summary>

		public Playlist CurrentPlaylist
		{
			get
			{
				return Invoke(delegate
				{
					if (itunes.CurrentPlaylist != null)
					{
						return new Playlist(itunes.CurrentPlaylist);
					}

					return null;
				});
			}
		}


		/// <summary>
		/// Gets the Source enumeration value of the current playlist.
		/// </summary>
		/// <remarks>
		/// Not called directly in code but rather data-bound to TrackPanel controls.
		/// </remarks>

		public Sources CurrentSource
		{
			get
			{
				Sources source;

				string playlistName = Invoke(delegate
				{
					IITPlaylist playlist = itunes.CurrentPlaylist;
					return playlist?.Name;
				});

				if (playlistName == null)
				{
					// Disconnected case
					source = Sources.Music;
				}
				else if (playlistName.Equals(Resx.PlaylistStore))
				{
					source = Sources.Store;
				}
				else if (playlistName.Equals(Resx.PlaylistMovies))
				{
					source = Sources.Movies;
				}
				else if (playlistName.Equals(Resx.PlaylistPodcasts))
				{
					source = Sources.Podcast;
				}
				else if (playlistName.Equals(Resx.PlaylistRadio))
				{
					source = Sources.Radio;
				}
				else if (playlistName.Equals(Resx.PlaylistTVShows))
				{
					source = Sources.TVShow;
				}
				else
				{
					TrackKind kind = Invoke(() => track?.Kind ?? TrackKind.Unknown);

					source = (kind == TrackKind.File) || (kind == TrackKind.Device)
						? Sources.Music
						: Sources.CD;
				}

				return source;
			}
		}


		/// <summary>
		/// Gets the current track or <b>null</b> if there is no current context.
		/// <para>
		/// DO NOT dispose CurrentTrack.
		/// </para>
		/// </summary>

		public Track CurrentTrack
		{
			get
			{
				Invoke(delegate
				{
					IITTrack currentTrack = itunes.CurrentTrack;
					if ((track == null) ||
						((currentTrack != null) && (currentTrack.trackID != track.TrackID)))
					{
						if (track != null)
						{
							track.Dispose();
							track = null;
						}

						track = new Track(currentTrack);
					}
				});

				return track;
			}

			set
			{
				if ((track == null) || ((track.TrackID != value.TrackID)))
				{
					if (track != null)
					{
						if (track.IsBuffered)
						{
							track.ApplyBuffer();
						}

						track.Dispose();
						track = null;
					}

					track = value;

					// current track is always in buffering mode
					track.IsBuffered = true;

					OnPropertyChanged("CurrentTrack");
					OnPropertyChanged("CurrentSource");
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>

		public EncoderCollection Encoders
		{
			get
			{
				EncoderCollection encoders = new EncoderCollection();

				// first encoder in list is always the "No encoder" entry
				encoders.Add(new Encoder(null));

				Invoke(delegate
				{
					foreach (IITEncoder iencoder in itunes.Encoders)
					{
						if (iencoder != null)
						{
							encoders.Add(new Encoder(iencoder));
						}
					}
				});

				return encoders;
			}
		}


		/// <summary>
		/// Internal helper for unit tests and harnesses.
		/// </summary>

		public bool IsConnected
		{
			get { return isConnected; }
		}


		/// <summary>
		/// Gets a Boolean value indicating if iTunes is currently running.
		/// </summary>

		public static bool IsHostRunning
		{
			get
			{
				var running = false;
				var processes = Process.GetProcessesByName("iTunes");
				if ((processes.Length > 0))
				{
					running = true;
				}

				for (var i = 0; i < processes.Length; i++)
				{
					processes[i].Dispose();
					processes[i] = null;
				}

				return running;
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if the current iTunes context is within
		/// the iTunes main Music library playlist.
		/// </summary>

		public bool IsMusicalPlaylist
		{
			get
			{
				bool isMusical = true; // optimistic

				Playlist playlist = CurrentPlaylist;
				if (playlist != null)
				{
					StringCollection list =
						librarian.Catalog.FindExtensionsByPlaylist(playlist.PersistentID);

					if (list.Count == 0)
						isMusical = false;

					playlist.Dispose();
				}

				return isMusical;
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if iTunes output is currently muted.
		/// </summary>

		public bool IsMuted
		{
			get
			{
				return Invoke(() => itunes.Mute);
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating whether iTunes is currently playing. 
		/// This is <b>false</b> if iTunes is not currently playing.
		/// </summary>

		public bool IsPlaying
		{
			get
			{
				return Invoke(() => itunes.PlayerState == ITPlayerState.ITPlayerStatePlaying);
			}
		}


		/// <summary>
		/// Gets the active librarian.
		/// </summary>

		public Librarian Librarian => librarian;


		/// <summary>
		/// Gets the main Library playlist.
		/// </summary>
		/// <remarks>
		/// Consumers should <b>not</b> Dispose the Playlist returned from this property.
		/// </remarks>

		public Playlist LibraryPlaylist
		{
			get
			{
				return libraryPlaylist ??
					(libraryPlaylist = Invoke(() => new Playlist(itunes.LibraryPlaylist)));
			}
		}


		/// <summary>
		/// Gets the full path to the current iTunes library XML file. The default file is
		/// "iTunes Music Library.xml" inside the "iTunes" folder in the user's "My Music" folder.
		/// </summary>

		public string LibraryXMLPath
		{
			get
			{
				return libraryXmlPath ?? (libraryXmlPath = Invoke(() => itunes.LibraryXMLPath));
			}
		}

		/// <summary>
		/// Gets or sets the playback position of the current track.
		/// </summary>

		public int PlayerPosition
		{
			get
			{
				return Invoke(delegate
				{
					try
					{
						return itunes.PlayerPosition;
					}
					catch
					{
						// when we first startup iTunes and nothing is playing then the
						// itunes.PlayerPosition property throws a COM exception
						return 0;
					}
				});
			}

			set
			{
				Invoke(delegate
				{
					itunes.PlayerPosition = value;
				});
			}
		}


		/// <summary>
		/// Returns the current player state.
		/// </summary>

		public ITPlayerState PlayerState
		{
			get
			{
				return Invoke(() => itunes.PlayerState);
			}
		}


		/// <summary>
		/// 
		/// </summary>

		public PlaylistCollection Playlists
		{
			get
			{
				return Invoke(delegate
				{
					PlaylistCollection collection = new PlaylistCollection();
					foreach (IITPlaylist playlist in itunes.LibrarySource.Playlists)
					{
						if (playlist != null)
						{
							collection.Add(new Playlist(playlist));
						}
					}
					return collection;
				});
			}
		}


		/// <summary>
		/// Gets or sets a Boolean value indicating if the playlist is currently shuffled.
		/// </summary>

		public bool Shuffle
		{
			get
			{
				return Invoke(delegate
				{
					try
					{
						if (itunes.CurrentPlaylist != null)
						{
							return itunes.CurrentPlaylist.Shuffle;
						}
						else if (itunes.LibraryPlaylist != null)
						{
							return itunes.LibraryPlaylist.Shuffle;
						}

						return false;
					}
					catch
					{
						// TODO: why?
						return false;
					}
				});
			}

			set
			{
				Invoke(delegate
				{
					if (itunes.CurrentPlaylist != null)
					{
						itunes.CurrentPlaylist.Shuffle = value;
						OnPropertyChanged("Shuffle");
					}
				});
			}
		}


		/// <summary>
		/// Gets or sets the output volume of iTunes, 0=minimum, 100=maximum.
		/// </summary>

		public int Volume
		{
			get
			{
				return Invoke(() => itunes.SoundVolume);
			}

			set
			{
				Invoke(delegate
				{
					itunes.SoundVolume = value;
				});
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		#region Handlers

		/// <summary>
		/// Handle iTunes database changes including adding a playlist, removing a playlist,
		/// adding a track to playlist, and removing a track from a playlist.
		/// </summary>
		/// <param name="deletedObjectIDs">
		/// A two-dimensional safe-array specifying the object IDs of each deleted object.
		/// </param>
		/// <param name="changedObjectIDs">
		/// A two-dimensional safe-array specifying the object IDs of each changed object.
		/// </param>
		/// <remarks>
		/// For the purposes of maintaining our in-memory catalog, we know that these
		/// operations result in very specific patterns that we can recognize in each
		/// safe-array.  If we find a pattern match then we can continue processing...
		/// </remarks>

		private void DoDatabaseChanged (object deletedObjectIDs, object changedObjectIDs)
		{
			// *** This handler blocks iTunes until complete so leave as quickly as possible!

			// I've yet to see both deleted and changed requests at the same time so we can
			// assume this is always true... check for changes first since additions and edits
			// are most likely more probable, then check for deletions

			MaintenanceAction action = MaintenanceAction.Create(changedObjectIDs);
			if (action != null)
			{
				librarian.MaintainLibrary(action);
			}
			else
			{
				action = MaintenanceAction.Create(deletedObjectIDs);
				if (action != null)
				{
					librarian.MaintainLibrary(action);
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="song"></param>
		/// <param name="stage"></param>

		private void DoLyricsProgressReport (ISong song, int stage)
		{
			LyricsProgressReport?.Invoke(song, stage);
		}


		/// <summary>
		/// LyricsEngine.LyricsUpdated event handler.
		/// </summary>
		/// <param name="isong">
		/// The song that was updated; this should be compared against the currently playing
		/// song to make sure the context is still the same.
		/// </param>

		private void DoLyricsUpdated (object isong)
		{
			// Pop Quiz! How many style rules does this line violate? :-)
			ISong song = isong as ISong;

			if (song != null)
			{
				if (LyricsUpdated != null)
				{
					if (track != null)
					{
						if (track.Artist.Equals(song.Artist) && track.Name.Equals(song.Title))
						{
							LyricsUpdated(song as Track);
						}
					}
				}
			}
		}


		/// <summary>
		/// The ITEventPlayerPlay event is fired when a track begins playing.   When iTunes
		/// switches to playing another track, you will received an ITEventPlayerStop event
		/// followed by an ITEventPlayerPlay event, unless it is playing joined CD tracks.
		/// </summary>
		/// <param name="otrack"></param>

		private void DoPlayerPlay (object otrack)
		{
			IITTrack itrack = otrack as IITTrack;
			if (itrack == null)
			{
				// TODO: do we throw an exception or set context to "no track"?
				return;
			}

			var trackId = Invoke(() => itrack.trackID);

			if ((this.track == null) || (this.track.TrackID != trackId))
			{
				CurrentTrack = new Track(itrack);

				if (!this.track.HasLyrics && NetworkStatus.IsAvailable)
				{
					engine.RetrieveLyrics(this.track);
				}
			}

			string album = this.track.Album;
			string artist = this.track.Artist;
			if (!librarian.IsCleansed(album, artist))
			{
				librarian.Clean(album, artist);
			}

			OnPropertyChanged("IsPlaying");
			timer.Enabled = IsPlaying;

			TrackPlaying?.Invoke(this.track);
		}


		/// <summary>
		/// The ITEventPlayerStop event is fired when a track stops playing.  When iTunes
		/// switches to playing another track, you will received an ITEventPlayerStop event
		/// followed by an ITEventPlayerPlay event, unless it is playing joined CD tracks 
		/// </summary>
		/// <param name="iTrack"></param>
		/// <remarks>
		/// We grab the track and treat it as a PlayPlay event just incase we missed a
		/// PlayerPlay event and don't have any current track information... kind of a refresh!
		/// </remarks>

		private void DoPlayerStopped (object iTrack)
		{
			IITTrack iitrack = iTrack as IITTrack ?? itunes.CurrentTrack;
			if (iitrack != null)
			{
				if ((this.track == null) ||
					(this.track.TrackID != iitrack.trackID))
				{
					CurrentTrack = new Track(iitrack);
				}
			}

			OnPropertyChanged("IsPlaying");
			timer.Enabled = IsPlaying;

			TrackStopped?.Invoke(track);
		}


		private void DoQuit ()
		{
			base.Dispose();

			Quiting?.Invoke(null, null);
		}


		/// <summary>
		/// The ITEventSoundVolumeChanged event is fired when the sound output volume has changed
		/// </summary>
		/// <param name="volume"></param>

		private void DoSoundVolumeChanged (int volume)
		{
			OnPropertyChanged("Volume");
		}


		/// <summary>
		/// The ITEventPlayerPlayingTrackChanged event is fired when information about the
		/// currently playing track has changed.  This event is fired when the user changes
		/// information about the currently playing track (e.g. the name of the track).
		/// This event is also fired when iTunes plays the next joined CD track in a CD
		/// playlist, since joined CD tracks are treated as a single track.
		/// </summary>
		/// <param name="otrack"></param>
		/// <remarks>
		/// We simply treat this as a "new" track beginning, same as PlayerPlay.
		/// </remarks>

		private void DoTrackChanged (object otrack)
		{
			IITTrack itrack = otrack as IITTrack;
			if (itrack == null)
			{
				// TODO: do we throw an exception or set context to "no track"?
				return;
			}

			var trackID = Invoke(() => itrack.trackID);

			if ((this.track == null) ||
				(this.track.TrackID != trackID))
			{
				// if this is a notification of the current track details changing then we
				// don't really care and we let binding take care of updating the UI; otherwise
				// we need to treat this as a new track playing event.
				// TODO: Does this duplicate the DoPlayerPlay handler?
				// TODO: do we just need to call PlayerPlay handler so we update lyrics?

				CurrentTrack = new Track(itrack);
			}

			timer.Enabled = IsPlaying;

			TrackPlaying?.Invoke(this.track);
		}


		/// <summary>
		/// Driven by the Controller timer, this notifies property listeners to update
		/// their binding points.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoUpdatePlayer (object sender, ElapsedEventArgs e)
		{
			OnPropertyChanged("PlayerPosition");
		}

		#endregion Handlers

		/// <summary>
		/// Raises the PropertyChanged event when the specified property value is changed.
		/// </summary>
		/// <param name="name"></param>

		private void OnPropertyChanged (string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}


		/// <summary>
		/// Gets the iTunes object specified by an ObjectID.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>

		public Playlist CreatePlaylist (string name)
		{
			return Invoke(() => new Playlist(itunes.CreatePlaylist(name)));
		}


		/// <summary>
		/// Start converting the specified file or folder.  The file or files will added to the
		/// main library after conversion. For a file on an audio CD, this is equivalent to
		/// importing the song.  Use CurrentEncoder to set the current encoder before converting.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public Track ConvertFile2 (string path)
		{
			return Invoke(delegate
			{
				var reset = new System.Threading.ManualResetEvent(false);

				iTunesConvertOperationStatus status = itunes.ConvertFile2(path);

				status.OnConvertOperationCompleteEvent +=
					delegate
					{
						// this anonymous callback delegate signals the scanner
						// thread to continue...
						reset.Set();
					};

				// wait for the current conversion to complete, otherwise iTunes
				// raises an exception when concurrent conversions are requested
				reset.WaitOne();
				reset.Reset();
				reset = null;

				Track track = null;

				if (status.Tracks.Count > 0)
				{
					track = new Track(status.Tracks[1]);
				}

				Release(status);

				return track;
			});
		}


		/// <summary>
		/// Start converting the specified track. For a track in an audio CD playlist, this
		/// is equivalent to importing the song.  Use CurrentEncoder to set the current
		/// encoder before converting
		/// </summary>
		/// <param name="convertible"></param>
		/// <returns></returns>

		public Track ConvertTrack2 (Track convertible)
		{
			return Invoke(delegate
			{
				var reset = new System.Threading.ManualResetEvent(false);

				object refTrack = convertible.AsInternal;
				iTunesConvertOperationStatus status = itunes.ConvertTrack2(ref refTrack);

				status.OnConvertOperationCompleteEvent +=
					delegate
					{
						// this anonymous callback delegate signals the scanner
						// thread to continue...
						reset.Set();
					};

				// wait for the current conversion to complete, otherwise iTunes
				// raises an exception when concurrent conversions are requested
				reset.WaitOne();
				reset.Reset();
				reset = null;

				Track track = null;

				try
				{
					if (status.Tracks.Count > 0)
					{
						track = new Track(status.Tracks[1]);
					}
				}
				catch (NullReferenceException exc)
				{
					// NullRefException can occur when iTunes displays the "protected" dialog.
					// We cannot test if status.Tracks is even null because that alone will
					// throw a COMException

					throw new ProtectedException(
						"Possible protection fault in Controller.ConvertTrack2", exc);
				}
				finally
				{
					Release(status);
				}

				return track;
			});
		}


		/// <summary>
		/// Gets the persisten ID of the iTunes object specified by an ObjectID.
		/// </summary>
		/// <param name="oid"></param>
		/// <returns></returns>

		public PersistentID GetPersistentID (ObjectID oid)
		{
			return Invoke(delegate
			{
				IITObject obj = itunes.GetITObjectByID(
					oid.SourceID, oid.PlaylistID, oid.TrackID, oid.DatabaseID);

				if (obj == null)
				{
					return PersistentID.Empty;
				}

				PersistentID persistentID = GetPersistentID(obj);
				Release(obj);

				return persistentID;
			});
		}


		/// <summary>
		/// Gets the iTunes object specified by an ObjectID.
		/// </summary>
		/// <param name="oid"></param>
		/// <returns></returns>

		public Playlist GetPlaylist (ObjectID oid)
		{
			return Invoke(() => new Playlist((IITPlaylist)itunes.GetITObjectByID(
				oid.SourceID, oid.PlaylistID, oid.TrackID, oid.DatabaseID)));
		}


		/// <summary>
		/// Gets a special collection of playlists suitable for synchronization.
		/// </summary>
		/// <returns></returns>

		public PlaylistCollection GetPreferredPlaylists ()
		{
			PlaylistCollection playlists = new PlaylistCollection();

			Invoke(delegate
			{
				// CurrentPlaylist would be null if viewing the iTunes Store
				var currentID = CurrentPlaylist?.PlaylistID ?? 0;

				foreach (IITPlaylist ilist in itunes.LibrarySource.Playlists)
				{
					if (ilist != null)
					{
						if ((ilist.Kind != ITPlaylistKind.ITPlaylistKindUser) ||
							ilist.Name.Equals(Resx.PlaylistGenius))
						{
							continue;
						}

						IITUserPlaylist ulist = (IITUserPlaylist)ilist;

						if ((ulist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone) ||
							(ulist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindPurchases) ||
							(ulist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindFolder))
						{
							if (ulist.Tracks.Count > 0)
							{
								Playlist playlist = new Playlist(ulist);
								playlist.IsSelected = (playlist.PlaylistID == currentID);
								playlists.Add(playlist);
							}
						}
					}
				}
			});

			return playlists;
		}


		/// <summary>
		/// Move to the next track in the current play list.
		/// </summary>

		public void NextTrack ()
		{
			Invoke(delegate
			{
				itunes.NextTrack();
			});
		}


		/// <summary>
		/// Pause playback.
		/// </summary>

		public void Pause ()
		{
			Invoke(delegate
			{
				itunes.Pause();
			});
		}


		/// <summary>
		/// Move to the previous track in the current play list.
		/// </summary>

		public void PreviousTrack ()
		{
			Invoke(delegate
			{
				itunes.PreviousTrack();
			});
		}


		/// <summary>
		/// Play the currently targeted track.
		/// </summary>

		public void Play ()
		{
			Invoke(delegate
			{
				itunes.Play();
			});
		}


		/// <summary>
		/// Force the iTunes window to the foreground and make it the active window.
		/// </summary>

		public void ShowiTunes ()
		{
			if (!isConnected)
			{
				Console.Beep(BeepFrequence, BeepDuration);
				return;
			}

			var processes = Process.GetProcessesByName("iTunes");
			if (processes.Length > 0)
			{
				IITBrowserWindow browser = itunes.BrowserWindow;
				var foreWinHandle = Interop.GetForegroundWindow();

				if (browser.Visible && (foreWinHandle == ((int)processes[0].MainWindowHandle)))
				{
					// if it's visible and is the active foreground window
					browser.Minimized = true;
				}
				else
				{
					// make it visible and set it as the active foreground window
					browser.Visible = true;
					Interop.SetForegroundWindow(processes[0].MainWindowHandle);
				}
			}

			for (int i = 0; i < processes.Length; i++)
			{
				processes[i].Dispose();
				processes[i] = null;
			}
		}


		/// <summary>
		/// Stop playback.
		/// </summary>

		public void Stop ()
		{
			Invoke(delegate
			{
				itunes.Stop();
			});
		}


		/// <summary>
		/// Toggle the sound volume mute state of the player.
		/// </summary>

		public void ToggleMute ()
		{
			Invoke(delegate
			{
				itunes.Mute = !itunes.Mute;
			});
		}


		/// <summary>
		/// Toggle the play/pause state of the player.
		/// </summary>

		public void TogglePlayPause ()
		{
			if (!isConnected)
			{
				Console.Beep(BeepFrequence, BeepDuration);
				return;
			}

			Invoke(delegate
			{
				itunes.PlayPause();
			});

			timer.Enabled = IsPlaying;
		}


		/// <summary>
		/// Decrease the sound volume level by a set amount of 5%.
		/// </summary>

		public void VolumeDown ()
		{
			Invoke(delegate
			{
				if (itunes.SoundVolume > (MinVolume + VolumeDelta))
				{
					itunes.SoundVolume -= VolumeDelta;
				}
				else if (itunes.SoundVolume > MinVolume)
				{
					itunes.SoundVolume = MinVolume;
				}
			});
		}


		/// <summary>
		/// Increase the sound volume level by a set amount of 5%.
		/// </summary>

		public void VolumeUp ()
		{
			Invoke(delegate
			{
				if (itunes.SoundVolume < (MaxVolume - VolumeDelta))
				{
					itunes.SoundVolume += VolumeDelta;
				}
				else if (itunes.SoundVolume < MaxVolume)
				{
					itunes.SoundVolume = MaxVolume;
				}
			});
		}
	}
}
