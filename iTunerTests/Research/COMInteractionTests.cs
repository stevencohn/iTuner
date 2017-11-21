//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests.COM
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.InteropServices;
	using System.Threading;
	using iTunesLib;
	using Microsoft.VisualStudio.TestTools.UnitTesting;


	/// <summary>
	/// Demonstrates how to handle COMCallsDisabled and COMCallsEnabled during long processing
	/// operations such as batch exporting.
	/// <para>
	/// To use this unit test, start it up as an individual harness in Debug mode.  It will begin
	/// invoking various iTunes COM methods at a one-second interval.  While this is running, open
	/// an iTunes dialog such as Preferences or Get Info.  Watch what happens in the VS Output 
	/// window.
	/// </para>
	/// </summary>

	[TestClass]
	public class COMInteractionTests : TestBase
	{
		private Controller controller;


		/// <summary>
		/// Begin the interactive test...
		/// </summary>

		[TestMethod]
		public void Harness ()
		{
			controller = new Controller();

			// let this run and open/close iTunes dialogs...

			// waiting for iTunes to quit
			while (controller.IsActive)
			{
				Thread.Sleep(1000);

				Track track = controller.CurrentTrack;
				if (track != null)
				{
					Debug.WriteLine(String.Format("CurrenTrack.Name = '{0}'", track.Name));
				}
				// DO NOT dispose current or we'll destroy the original CurrentTrack RCW

				// bogus algorithm just to exercise the collection
				int sum = 0;
				foreach (Playlist playlist in controller.Playlists)
				{
					if (playlist != null)
					{
						sum += playlist.PlaylistID;
					}
				}

				Debug.WriteLine(String.Format("Playlist sum [{0}]", sum));

				ReportPrivateDelta();
			}
		}


		[TestCleanup]
		public override void MyTestCleanup ()
		{
			Debug.WriteLine("... Done");

			// if iTunes is shutdown first then itunes will be null because of OnAboutToPrompt...
			// otherwise if we terminate the unit test first then we want to force our
			// cleanup here...

			if (controller != null)
			{
				controller.Dispose();
				controller = null;
			}

			base.MyTestCleanup();
		}
	}

	internal sealed class Controller : Interaction
	{
		private _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler quitHandler;
		private bool isActive;

		public Controller ()
			: base(true)
		{
			quitHandler = new _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler(DoQuit);
			itunes.OnAboutToPromptUserToQuitEvent += quitHandler;

			isActive = true;
		}

		protected override void Cleanup ()
		{
			itunes.OnAboutToPromptUserToQuitEvent -= quitHandler;
			quitHandler = null;
		}

		private void DoQuit ()
		{
			isActive = false;
		}

		public Track CurrentTrack
		{
			get
			{
				return Invoke(new Func<Track>(delegate
				{
					IITTrack track = itunes.CurrentTrack;
					return track == null ? null : new Track(track);
				}));
			}
		}

		public bool IsActive
		{
			get { return isActive; }
		}

		public PlaylistCollection Playlists
		{
			get
			{
				return Invoke(new Func<PlaylistCollection>(delegate
				{
					PlaylistCollection collection = new PlaylistCollection();
					foreach (IITPlaylist playlist in itunes.LibrarySource.Playlists)
					{
						collection.Add(new Playlist(playlist));
					}
					return collection;
				}));
			}
		}

		public void Stop ()
		{
			Invoke(new Action(delegate
			{
				itunes.Stop();
			}));
		}
	}

	internal sealed class PlaylistCollection : List<Playlist>
	{
	}

	internal sealed class Playlist : Interaction
	{
		private IITPlaylist playlist;

		public Playlist (IITPlaylist playlist)
			: base()
		{
			this.playlist = playlist;
		}

		protected override void Cleanup ()
		{
			Release(playlist);
			playlist = null;
		}

		public string Name
		{
			get
			{
				return Invoke(new Func<string>(delegate
				{
					return playlist.Name;
				}));
			}
		}

		public int PlaylistID
		{
			get
			{
				return Invoke(new Func<int>(delegate
				{
					return playlist.playlistID;
				}));
			}
		}

		public TrackCollection Tracks
		{
			get
			{
				return Invoke(new Func<TrackCollection>(delegate
				{
					TrackCollection collection = new TrackCollection();
					foreach (IITTrack track in playlist.Tracks)
					{
						collection.Add(new Track(track));
					}
					return collection;
				}));
			}
		}

		//int playlist.Duration
		//int playlist.Index
		//ITPlaylistKind playlist.Kind
		//void playlist.PlayFirstTrack()
		//void playlist.Print(boolShowPrintDialog, ITPlaylistPrintKindPrintKind, stringTheme)
		//IITTrackCollection playlist.Search(stringSearchText, ITPlaylistSearchFieldSearchFields)
		//bool playlist.Shuffle
		//double playlist.Size
		//ITPlaylistRepeatMode playlist.SongRepeat
		//IITSource playlist.Source
		//int playlist.sourceID
		//string playlist.Time
		//int playlist.TrackDatabaseID
		//int playlist.trackID
		//bool playlist.Visible
	}

	internal sealed class TrackCollection : List<Track>
	{
	}

	internal sealed class Track : Interaction
	{
		private IITTrack track;

		public Track (IITTrack track)
			: base()
		{
			this.track = track;
		}

		protected override void Cleanup ()
		{
			Release(track);
			track = null;
		}

		public string Name
		{
			get
			{
				return Invoke(new Func<string>(delegate
				{
					return track.Name;
				}));
			}
		}

	}


	//********************************************************************************************
	// class Interaction
	//********************************************************************************************

	/// <summary>
	/// Provides base mechanisms for inheritors to safely control access to the iTunes COM
	/// interface, blocking callers when COM interaction is disabled, allowing callers while
	/// COM interaction is enabled.
	/// </summary>

	internal abstract class Interaction : IDisposable
	{
		// single iTunes COM interface used by all instances
		protected static iTunesAppClass itunes;

		// single synchronizer to coordinate COM state changes
		private static ManualResetEvent reset;

		private static _IiTunesEvents_OnCOMCallsEnabledEventEventHandler enabledEvent;
		private static _IiTunesEvents_OnCOMCallsDisabledEventEventHandler disabledEvent;

		private bool isPrimaryController;
		private bool isDisposed;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Initialize class static fields.
		/// </summary>

		static Interaction ()
		{
			enabledEvent = new _IiTunesEvents_OnCOMCallsEnabledEventEventHandler(DoEnabledEvent);
			disabledEvent = new _IiTunesEvents_OnCOMCallsDisabledEventEventHandler(DoDisabledEvent);

			itunes = new iTunesAppClass();
			itunes.OnCOMCallsEnabledEvent += enabledEvent;
			itunes.OnCOMCallsDisabledEvent += disabledEvent;

			reset = new ManualResetEvent(true);
		}


		/// <summary>
		/// Initialize a new instance for secondary wrappers such as Playlists, Tracks, etc.
		/// </summary>

		public Interaction ()
			: this(false)
		{
		}


		/// <summary>
		/// Initialize a new instance for the primary iTunesAppClass wrapper, specifying
		/// <b>true</b> as the <i>isPrimaryController</i> parameter.
		/// </summary>
		/// <param name="isPrimaryController">
		/// Specify <b>true</b> to indicate the primary iTunesAppClass wrapper; otherwise,
		/// all other wrappers should use the default constructor.
		/// </param>

		public Interaction (bool isPrimaryController)
		{
			this.isPrimaryController = isPrimaryController;
			this.isDisposed = false;
		}


		#region Lifecycle

		/// <summary>
		/// Base destructor for all inheritors; do not override.
		/// </summary>

		~Interaction ()
		{
			Dispose();
		}


		/// <summary>
		/// Dispose of this instance; do not override.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				// invoke Cleanup on behalf of the derived class
				Cleanup();

				if (isPrimaryController)
				{
					if (itunes != null)
					{
						itunes.OnCOMCallsDisabledEvent -= disabledEvent;
						itunes.OnCOMCallsEnabledEvent -= enabledEvent;
						disabledEvent = null;
						enabledEvent = null;

						try
						{
							itunes.Quit();
						}
						catch
						{
							// no-op
						}
						finally
						{
							Marshal.ReleaseComObject(itunes);
							itunes = null;
						}

						reset.Set();
						reset.Close();
						reset = null;
					}
				}

				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		#endregion Lifecycle

		#region Handlers

		/// <summary>
		/// When COM interaction is disabled, this forces all subsequent wrappers to block
		/// until interaction is re-enabled.
		/// </summary>
		/// <param name="reason">The reason why COM interaction was disabled.</param>

		private static void DoDisabledEvent (ITCOMDisabledReason reason)
		{
			reset.Reset();
			Debug.WriteLine(String.Format("OnCOMCallsDisabledEvent(reason:{0})", reason));
		}


		/// <summary>
		/// When COM interaction is re-enabled, this releases all blocked wrapper calls.
		/// </summary>

		private static void DoEnabledEvent ()
		{
			reset.Set();
			Debug.WriteLine("OnCOMCallsEnabledEvent()");
		}

		#endregion Handlers


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Inheritors should override if this need to dispose their own private COM
		/// wrapper instances.  This is invoked automatically by the base.Dispose method.
		/// </summary>

		protected virtual void Cleanup ()
		{
			// override this in derived classes
		}


		/// <summary>
		/// Safely invokes the given Action, which is a method with no parameters and
		/// no return value.
		/// </summary>
		/// <param name="action">An Action to perform.</param>

		protected void Invoke (Action action)
		{
			try
			{
				try
				{
					// will return immediately if already set
					// so we can then perform the following action
					reset.WaitOne();
					action();
				}
				catch (COMException)
				{
					// block until set and then try to perform the action again
					reset.WaitOne();
					action();
				}
			}
			catch (AbandonedMutexException)
			{
				// TODO: do we need to give up?  Can we reinitialize 'reset'?
				// no-op
			}
			catch (COMException)
			{
				// last chance: no-op
			}
		}


		/// <summary>
		/// Safely invokes the given Func, which is a method with no parameters and a return
		/// value of the specified return type
		/// </summary>
		/// <typeparam name="T">The type to return.</typeparam>
		/// <param name="action">The Func to perform.</param>
		/// <returns>A instance of the generic type T.</returns>

		protected T Invoke<T> (Func<T> action)
		{
			T result = default(T);
			try
			{
				try
				{
					// will return immediately if already set
					// so we can then perform the following action
					reset.WaitOne();
					result = action();
				}
				catch (COMException)
				{
					// block until set and then try to perform the action again
					reset.WaitOne();
					result = action();
				}
			}
			catch (AbandonedMutexException)
			{
				// TODO: do we need to give up?  Can we reinitialize 'reset'?
				return default(T);
			}
			catch (COMException)
			{
				// last chance: give up
				return default(T);
			}

			return result;
		}


		/// <summary>
		/// Releases the given COM object wrapper.  Inheritors should use this in their
		/// Cleanup overrides.
		/// </summary>
		/// <param name="wrapper">The COM wrapper instance.</param>

		protected void Release (object wrapper)
		{
			if (wrapper is MarshalByRefObject)
			{
				Marshal.ReleaseComObject(wrapper);
			}
		}
	}
}
