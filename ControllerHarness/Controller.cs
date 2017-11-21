//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Runtime.InteropServices;
	using System.Timers;
	using iTunesLib;
	using Resx = ControllerHarness.Resources;


	//********************************************************************************************
	// class Controller
	//********************************************************************************************

	/// <summary>
	/// Thread-safe and COM-aware bindable wrapper for iTunesAppClass with track monitoring
	/// and automatic lyric discovery.
	/// </summary>

	internal sealed class Controller : Interaction, INotifyPropertyChanged
	{
		private const string LogCategory = "Controller";

		private const int MinVolume = 0;
		private const int MaxVolume = 100;
		private const int VolumeDelta = 5;

		private const int BeepFrequence = 1000;
		private const int BeepDuration = 100;

		private Librarian librarian;					// library organizer
		private LyricEngine engine;						// lyric retrieval engine
		private Timer timer;							// track update timer
		private Track track;							// current track

		private _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler quitHandler;
		private _IiTunesEvents_OnDatabaseChangedEventEventHandler databaseChangedHandler;
		private _IiTunesEvents_OnPlayerPlayEventEventHandler playHandler;
		private _IiTunesEvents_OnPlayerPlayingTrackChangedEventEventHandler trackChangedHandler;
		private _IiTunesEvents_OnPlayerStopEventEventHandler stopHandler;
		private _IiTunesEvents_OnSoundVolumeChangedEventEventHandler volumeHandler;

		#region Interop

		private static class Interop
		{
			/// <summary>
			/// The GetForegroundWindow function returns a handle to the foreground window
			/// (the window with which the user is currently working). The system assigns
			/// a slightly higher priority to the thread that creates the foreground window
			/// than it does to other threads. 
			/// </summary>
			/// <returns>
			/// The return value is a handle to the foreground window. The foreground window
			/// can be NULL in certain circumstances, such as when a window is losing activation.
			/// </returns>

			[DllImport("user32.dll")]
			public static extern int GetForegroundWindow ();


			/// <summary>
			/// The SetForegroundWindow function puts the thread that created the specified window
			/// into the foreground and activates the window. Keyboard input is directed to the
			/// window, and various visual cues are changed for the user. The system assigns a
			/// slightly higher priority to the thread that created the foreground window than
			/// it does to other threads. 
			/// </summary>
			/// <param name="hWnd">
			/// Handle to the window that should be activated and brought to the foreground. 
			/// </param>
			/// <returns>
			/// If the window was brought to the foreground, the return value is nonzero. 
			/// If the window was not brought to the foreground, the return value is zero.
			/// </returns>

			[return: MarshalAs(UnmanagedType.Bool)]
			[DllImport("user32.dll")]
			public static extern bool SetForegroundWindow (IntPtr hWnd);
		}

		#endregion Interop


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new iTunesAppClass safe wrapper.
		/// </summary>

		public Controller ()
			: base()
		{
			InitializeHost();

			this.engine = LyricEngine.CreateEngine();
			this.engine.LyricsUpdated += new TrackHandler(DoLyricsUpdated);
			this.engine.LyricsProgressReport += new LyricEngineProgress(DoLyricsProgressReport);

			if (this.track != null)
			{
				if (!this.track.HasLyrics && NetworkStatus.IsAvailable)
				{
					this.engine.RetrieveLyrics(this.track);
				}
			}

			this.librarian = Librarian.Create(this);

			this.timer = new Timer(1000);
			this.timer.Elapsed += new ElapsedEventHandler(DoUpdatePlayer);
			this.timer.Enabled = IsPlaying;
		}


		/// <summary>
		/// Special routine to initialize the base static itunes instance and configure it
		/// for first-time use.
		/// </summary>

		private void InitializeHost ()
		{
			bool fresh = !Controller.IsHostRunning;

			Interaction.itunes = new iTunesAppClass();

			InitializeInteraction();

			if ((CurrentTrack == null) && (PlayerState != ITPlayerState.ITPlayerStatePlaying))
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

			this.track = CurrentTrack;

			quitHandler =
				new _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler(DoQuit);

			databaseChangedHandler =
				new _IiTunesEvents_OnDatabaseChangedEventEventHandler(DoDatabaseChanged);

			playHandler =
				new _IiTunesEvents_OnPlayerPlayEventEventHandler(DoPlayerPlay);

			trackChangedHandler =
				new _IiTunesEvents_OnPlayerPlayingTrackChangedEventEventHandler(DoTrackChanged);

			stopHandler =
				new _IiTunesEvents_OnPlayerStopEventEventHandler(DoPlayerStopped);

			volumeHandler =
				new _IiTunesEvents_OnSoundVolumeChangedEventEventHandler(DoSoundVolumeChanged);

			OnAboutToPromptUserToQuitEvent += quitHandler;
			OnDatabaseChangedEvent += databaseChangedHandler;
			OnPlayerPlayEvent += playHandler;
			OnPlayerPlayingTrackChangedEvent += trackChangedHandler;
			OnPlayerStopEvent += stopHandler;
			OnSoundVolumeChangedEvent += volumeHandler;

			if (fresh)
			{
				// if we started iTunes then presume we can minimize it without surprise
				itunes.BrowserWindow.Minimized = true;
			}
		}


		#region Lifecycle

		protected override void Cleanup ()
		{
			if (timer != null)
			{
				timer.Elapsed -= new ElapsedEventHandler(DoUpdatePlayer);
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
				engine.LyricsUpdated -= new TrackHandler(DoLyricsUpdated);
				engine.LyricsProgressReport -= new LyricEngineProgress(DoLyricsProgressReport);
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
				Invoke((Action)delegate
				{
					itunes.OnAboutToPromptUserToQuitEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke((Action)delegate
				{
					itunes.OnAboutToPromptUserToQuitEvent -= value;
				});
			}
		}


		/// <summary>
		/// This event is fired when the iTunes database is changed.
		/// </summary>

		public event _IiTunesEvents_OnDatabaseChangedEventEventHandler OnDatabaseChangedEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				Invoke((Action)delegate
				{
					itunes.OnDatabaseChangedEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke((Action)delegate
				{
					itunes.OnDatabaseChangedEvent -= value;
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
				Invoke((Action)delegate
				{
					itunes.OnPlayerPlayEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke((Action)delegate
				{
					itunes.OnPlayerPlayEvent -= value;
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
				Invoke((Action)delegate
				{
					itunes.OnPlayerPlayingTrackChangedEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke((Action)delegate
				{
					itunes.OnPlayerPlayingTrackChangedEvent -= value;
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
				Invoke((Action)delegate
				{
					itunes.OnPlayerStopEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke((Action)delegate
				{
					itunes.OnPlayerStopEvent -= value;
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
				Invoke((Action)delegate
				{
					itunes.OnSoundVolumeChangedEvent += value;
				});
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				Invoke((Action)delegate
				{
					itunes.OnSoundVolumeChangedEvent -= value;
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
				return Invoke((Func<bool>)delegate
				{
					return itunes.AppCommandMessageProcessingEnabled;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					itunes.AppCommandMessageProcessingEnabled = value;
				});
			}
		}


		/// <summary>
		/// Gets the Source enumeration value of the current playlist.
		/// </summary>

		[Obsolete("Is this used?")]
		public Sources CurrentSource
		{
			get
			{
				Sources source = Sources.Music;

				string playlistName = Invoke((Func<string>)delegate
				{
					IITPlaylist playlist = itunes.CurrentPlaylist;
					return playlist == null ? null : playlist.Name;
				});

				if (playlistName.Equals(Resx.PlaylistStore))
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
					ITTrackKind kind = Invoke((Func<ITTrackKind>)delegate
					{
						return track == null ? ITTrackKind.ITTrackKindUnknown : track.Kind;
					});

					if ((kind == ITTrackKind.ITTrackKindFile) ||
						(kind == ITTrackKind.ITTrackKindDevice))
					{
						source = Sources.Music;
					}
					else
					{
						source = Sources.CD;
					}
				}

				return source;
			}
		}


		/// <summary>
		/// Gets the current track or <b>null</b> if there is no current context.
		/// </summary>

		public Track CurrentTrack
		{
			get
			{
				Invoke((Action)delegate
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
						if (track.HasPendingLyrics)
						{
							track.ApplyLyrics();
						}

						track.Dispose();
						track = null;
					}

					track = value;
					OnPropertyChanged("CurrentTrack");
					OnPropertyChanged("CurrentSource");
				}
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
				bool running = false;
				Process[] processes = Process.GetProcessesByName("iTunes");
				if ((processes != null) && (processes.Length > 0))
				{
					running = true;
				}

				for (int i = 0; i < processes.Length; i++)
				{
					processes[i].Dispose();
					processes[i] = null;
				}

				processes = null;

				return running;
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if iTunes output is currently muted.
		/// </summary>

		public bool IsMuted
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					return itunes.Mute;
				});
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
				return Invoke((Func<bool>)delegate
				{
					return itunes.PlayerState == ITPlayerState.ITPlayerStatePlaying;
				});
			}
		}


		/// <summary>
		/// Gets or sets a Boolean value indicating if the playlist is currently shuffled.
		/// </summary>

		public bool IsShuffled
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					try
					{
						if (itunes.CurrentPlaylist != null)
						{
							return itunes.CurrentPlaylist.Shuffle;
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
				Invoke((Action)delegate
				{
					if (itunes.CurrentPlaylist != null)
					{
						itunes.CurrentPlaylist.Shuffle = value;
						OnPropertyChanged("IsShuffled");
					}
				});
			}
		}


		/// <summary>
		/// Gets or sets the playback position of the current track.
		/// </summary>

		public int PlayerPosition
		{
			get
			{
				return Invoke((Func<int>)delegate
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
				Invoke((Action)delegate
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
				return Invoke((Func<ITPlayerState>)delegate
				{
					return itunes.PlayerState;
				});
			}
		}


		/// <summary>
		/// 
		/// </summary>

		public PlaylistCollection Playlists
		{
			get
			{
				return Invoke((Func<PlaylistCollection>)delegate
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
		/// Gets or sets the output volume of iTunes, 0=minimum, 100=maximum.
		/// </summary>

		public int Volume
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return itunes.SoundVolume;
				});
			}

			set
			{
				Invoke((Action)delegate
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
			if (LyricsProgressReport != null)
			{
				LyricsProgressReport(song, stage);
			}
		}


		/// <summary>
		/// LyricsEngine.LyricsUpdated event handler.
		/// </summary>
		/// <param name="sender"><b>null</b></param>
		/// <param name="song">
		/// The song that was updated; this should be compared against the currently playing
		/// song to make sure the context is still the same.
		/// </param>

		private void DoLyricsUpdated (object isong)
		{
			// Pop Quiz! How many style rules does this line violate? :-)
			ISong song = isong as ISong;

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


		/// <summary>
		/// The ITEventPlayerPlay event is fired when a track begins playing.   When iTunes
		/// switches to playing another track, you will received an ITEventPlayerStop event
		/// followed by an ITEventPlayerPlay event, unless it is playing joined CD tracks.
		/// </summary>
		/// <param name="iTrack"></param>

		private void DoPlayerPlay (object otrack)
		{
			IITTrack itrack = otrack as IITTrack;
			if (itrack == null)
			{
				// TODO: do we throw an exception or set context to "no track"?
				return;
			}

			int trackID = Invoke((Func<int>)delegate
			{
				return itrack.trackID;
			});

			if ((this.track == null) ||
				(this.track.TrackID != trackID))
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

			if (TrackPlaying != null)
			{
				TrackPlaying(this.track);
			}
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

			if (TrackStopped != null)
			{
				TrackStopped(track);
			}
		}


		private void DoQuit ()
		{
			base.Dispose();

			if (Quiting != null)
			{
				Quiting(null, null);
			}
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
		/// <param name="iTrack"></param>
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

			int trackID = Invoke((Func<int>)delegate
			{
				return itrack.trackID;
			});

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

			if (TrackPlaying != null)
			{
				TrackPlaying(this.track);
			}
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
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}


		/// <summary>
		/// Pause playback.
		/// </summary>

		public void Pause ()
		{
			Invoke((Action)delegate
			{
				itunes.Pause();
			});
		}


		/// <summary>
		/// Play the currently targeted track.
		/// </summary>

		public void Play ()
		{
			Invoke((Action)delegate
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

			Process[] processes = Process.GetProcessesByName("iTunes");
			if ((processes != null) && (processes.Length > 0))
			{
				IITBrowserWindow browser = itunes.BrowserWindow;
				int foreWinHandle = Interop.GetForegroundWindow();

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

			processes = null;
		}


		/// <summary>
		/// Stop playback.
		/// </summary>

		public void Stop ()
		{
			Invoke((Action)delegate
			{
				itunes.Stop();
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

			Invoke((Action)delegate
			{
				itunes.PlayPause();
			});

			timer.Enabled = IsPlaying;
		}
	}
}
