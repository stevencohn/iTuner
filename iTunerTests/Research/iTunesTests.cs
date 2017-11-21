//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Diagnostics;
	using System.Runtime.InteropServices;
	using System.Threading;
	using iTunesLib;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner.iTunes;


	/// <summary>
	/// </summary>

	[TestClass]
	public class iTunesTests : TestBase
	{
		private ManualResetEvent reset;
		private iTunesAppClass itunes;

		private _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler aboutToPromptUserToQuitEvent;
		private _IiTunesEvents_OnCOMCallsDisabledEventEventHandler cOMCallsDisabledEvent;
		private _IiTunesEvents_OnCOMCallsEnabledEventEventHandler cOMCallsEnabledEvent;
		private _IiTunesEvents_OnDatabaseChangedEventEventHandler databaseChangedEvent;
		private _IiTunesEvents_OnPlayerPlayEventEventHandler playerPlayEvent;
		private _IiTunesEvents_OnPlayerPlayingTrackChangedEventEventHandler playerPlayingTrackChangedEvent;
		private _IiTunesEvents_OnPlayerStopEventEventHandler playerStopEvent;
		private _IiTunesEvents_OnQuittingEventEventHandler quittingEvent;
		private _IiTunesEvents_OnSoundVolumeChangedEventEventHandler soundVolumeChangedEvent;
		private _IiTunesEvents_OnUserInterfaceEnabledEventEventHandler userInterfaceEnabledEvent;


		/// <summary>
		/// </summary>

		[TestMethod]
		public void iTunes ()
		{
			itunes = new iTunesAppClass();

			aboutToPromptUserToQuitEvent = new _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler(itunes_OnAboutToPromptUserToQuitEvent);
			cOMCallsDisabledEvent = new _IiTunesEvents_OnCOMCallsDisabledEventEventHandler(itunes_OnCOMCallsDisabledEvent);
			cOMCallsEnabledEvent = new _IiTunesEvents_OnCOMCallsEnabledEventEventHandler(itunes_OnCOMCallsEnabledEvent);
			databaseChangedEvent = new _IiTunesEvents_OnDatabaseChangedEventEventHandler(itunes_OnDatabaseChangedEvent);
			playerPlayEvent = new _IiTunesEvents_OnPlayerPlayEventEventHandler(itunes_OnPlayerPlayEvent);
			playerPlayingTrackChangedEvent = new _IiTunesEvents_OnPlayerPlayingTrackChangedEventEventHandler(itunes_OnPlayerPlayingTrackChangedEvent);
			playerStopEvent = new _IiTunesEvents_OnPlayerStopEventEventHandler(itunes_OnPlayerStopEvent);
			quittingEvent = new _IiTunesEvents_OnQuittingEventEventHandler(itunes_OnQuittingEvent);
			soundVolumeChangedEvent = new _IiTunesEvents_OnSoundVolumeChangedEventEventHandler(itunes_OnSoundVolumeChangedEvent);
			userInterfaceEnabledEvent = new _IiTunesEvents_OnUserInterfaceEnabledEventEventHandler(itunes_OnUserInterfaceEnabledEvent);

			// NOTE: did not see any difference in maintaining these individual handler variables
			// as opposed to unregistering -= new handlers in the OnAboutToPromptToQuit handler

			itunes.OnAboutToPromptUserToQuitEvent += aboutToPromptUserToQuitEvent;
			itunes.OnCOMCallsDisabledEvent += cOMCallsDisabledEvent;
			itunes.OnCOMCallsEnabledEvent += cOMCallsEnabledEvent;
			itunes.OnDatabaseChangedEvent += databaseChangedEvent;
			itunes.OnPlayerPlayEvent += playerPlayEvent;
			itunes.OnPlayerPlayingTrackChangedEvent += playerPlayingTrackChangedEvent;
			itunes.OnPlayerStopEvent += playerStopEvent;
			itunes.OnQuittingEvent += quittingEvent;
			itunes.OnSoundVolumeChangedEvent += soundVolumeChangedEvent;
			itunes.OnUserInterfaceEnabledEvent += userInterfaceEnabledEvent;

			AllProps(itunes);

			reset = new ManualResetEvent(false);

			// waiting for iTunes to quit
			reset.WaitOne();
		}

		[TestCleanup]
		public override void MyTestCleanup ()
		{
			Debug.WriteLine("**** Unit test shutdown");

			// if iTunes is shutdown first then itunes will be null because of OnAboutToPrompt...
			// otherwise if we terminate the unit test first then we want to force our
			// cleanup here...

			if (itunes != null)
			{
				itunes_OnAboutToPromptUserToQuitEvent();
				Marshal.ReleaseComObject(itunes);
				itunes = null;
			}

			base.MyTestCleanup();
		}


		private void itunes_OnAboutToPromptUserToQuitEvent ()
		{
			Debug.WriteLine("OnAboutToPromptUserToQuitEvent()");
			itunes.OnAboutToPromptUserToQuitEvent -= aboutToPromptUserToQuitEvent;
			itunes.OnCOMCallsDisabledEvent -= cOMCallsDisabledEvent;
			itunes.OnCOMCallsEnabledEvent -= cOMCallsEnabledEvent;
			itunes.OnDatabaseChangedEvent -= databaseChangedEvent;
			itunes.OnPlayerPlayEvent -= playerPlayEvent;
			itunes.OnPlayerPlayingTrackChangedEvent -= playerPlayingTrackChangedEvent;
			itunes.OnPlayerStopEvent -= playerStopEvent;
			itunes.OnQuittingEvent -= quittingEvent;
			itunes.OnSoundVolumeChangedEvent -= soundVolumeChangedEvent;
			itunes.OnUserInterfaceEnabledEvent -= userInterfaceEnabledEvent;

			// NOTE: after trying to disconnect and dispose in the OnQuiting handler, noticed
			// that we can disconnect much more reliably here.  Proves we can ostensibly ignore
			// the OnQuiting event

			itunes.Quit();

			reset.Set();
		}

		private void itunes_OnCOMCallsDisabledEvent (ITCOMDisabledReason reason)
		{
			Debug.WriteLine(String.Format("OnCOMCallsDisabledEvent(reason:{0})", reason));
		}

		private void itunes_OnCOMCallsEnabledEvent ()
		{
			Debug.WriteLine("OnCOMCallsEnabledEvent()");
		}

		private void itunes_OnDatabaseChangedEvent (object deletedObjectIDs, object changedObjectIDs)
		{
			// NOTE: this event is both expensive and blocking, so it is not advised to
			// wire up a handler to OnDatabaseChanged.

			Debug.WriteLine("OnDatabaseChangedEvent(deletedObjectIDs, changedObjectIDs)");
			if (deletedObjectIDs == null)
			{
				Debug.WriteLine("DeletedObjectIDs is null");
			}
			else
			{
				ObjectIDCollection deleteIDs = new ObjectIDCollection();
				Array deleted = deletedObjectIDs as Array;
				for (int i = deleted.GetLowerBound(0); i <= deleted.GetUpperBound(0); i++)
				{
					deleteIDs.Add(new ObjectID(
						(int)deleted.GetValue(i, 0),
						(int)deleted.GetValue(i, 1),
						(int)deleted.GetValue(i, 2),
						(int)deleted.GetValue(i, 3)));
				}

				if (deleteIDs.Count > 0)
				{
					DumpChanges("Deleted", deleteIDs);
				}
			}

			if (deletedObjectIDs == null)
			{
				Debug.WriteLine("ChangedObjectIDs is null");
			}
			else
			{
				ObjectIDCollection changeIDs = new ObjectIDCollection();
				Array changed = changedObjectIDs as Array;
				for (int i = changed.GetLowerBound(0); i <= changed.GetUpperBound(0); i++)
				{
					changeIDs.Add(new ObjectID(
						(int)changed.GetValue(i, 0),
						(int)changed.GetValue(i, 1),
						(int)changed.GetValue(i, 2),
						(int)changed.GetValue(i, 3)));
				}

				if (changeIDs.Count > 0)
				{
					DumpChanges("Changes", changeIDs);
				}
			}
		}

		private void DumpChanges (string title, ObjectIDCollection things)
		{
			foreach (ObjectID oid in things)
			{
				if (oid.IsSource)
				{
					IITSource source = (IITSource)itunes.GetITObjectByID(
						oid.SourceID, oid.PlaylistID, oid.TrackID, oid.DatabaseID);

					if (source == null)
					{
						Debug.WriteLine(
							String.Format("Source ({0},{1},{2},{3}): UNKNOWN",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID));
					}
					else
					{
						Debug.WriteLine(
							String.Format("Source ({0},{1},{2},{3}): {4}",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID,
							source.Name));
					}
				}
				else if (oid.IsPlaylist)
				{
					IITPlaylist playlist = (IITPlaylist)itunes.GetITObjectByID(
						oid.SourceID, oid.PlaylistID, oid.TrackID, oid.DatabaseID);

					if (playlist == null)
					{
						Debug.WriteLine(
							String.Format("Playlist ({0},{1},{2},{3}): UNKNOWN",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID));
					}
					else
					{
						Debug.WriteLine(
							String.Format("Playlist ({0},{1},{2},{3}): {4}, tracks {5}",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID,
							playlist.Name, playlist.Tracks.Count));
					}
				}
				else if (oid.IsTrack)
				{
					IITTrack track = (IITTrack)itunes.GetITObjectByID(
						oid.SourceID, oid.PlaylistID, oid.TrackID, oid.DatabaseID);

					if (track == null)
					{
						Debug.WriteLine(
							String.Format("Track ({0},{1},{2},{3}): UNKNOWN",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID));
					}
					else
					{
						Debug.WriteLine(
							String.Format("Track ({0},{1},{2},{3}): {4}, playedCount {5}",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID,
							track.Name, track.PlayedCount));
					}
				}
				else
				{
					IITObject changed = itunes.GetITObjectByID(
						oid.SourceID, oid.PlaylistID, oid.TrackID, oid.DatabaseID);

					if (changed == null)
					{
						Debug.WriteLine(
							String.Format("Unknown ({0},{1},{2},{3})",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID));
					}
					else
					{
						Debug.WriteLine(String.Format(
							"other ({0},{1},{2},{3}) -> Name [{4}]",
							oid.DatabaseID, oid.SourceID, oid.PlaylistID, oid.TrackID,
							changed.Name));
					}
				}
			}
		}

		private void itunes_OnPlayerPlayingTrackChangedEvent (object iTrack)
		{
			IITTrack track = iTrack as IITTrack;
			Debug.WriteLine(String.Format("OnPlayerPlayingTracChangedEvent(track:{0})", MakeKey(track)));
			DumpState(itunes);
		}

		private void itunes_OnPlayerPlayEvent (object iTrack)
		{
			IITTrack track = iTrack as IITTrack;
			Debug.WriteLine(String.Format("OnPlayerPlayEvent(track:{0})", MakeKey(track)));
			DumpState(itunes);
		}

		private void itunes_OnPlayerStopEvent (object iTrack)
		{
			IITTrack track = iTrack as IITTrack;
			Debug.WriteLine(String.Format("OnPlayerStopEvent(track:{0})", MakeKey(track)));
		}

		private void itunes_OnQuittingEvent ()
		{
			Debug.WriteLine("OnQuitingEvent()");
		}

		private void itunes_OnSoundVolumeChangedEvent (int newVolume)
		{
			Debug.WriteLine(String.Format("OnSoundVolumeChangedEvent(newVolume:{0})", newVolume));

			// NOTE: fired every time we start playing a track even if the volume has not
			// changed.  But that shouldn't adversely affect us...
		}

		private void itunes_OnUserInterfaceEnabledEvent ()
		{
			Debug.WriteLine("OnUserInterfaceEnabledEnabed()");
		}


		private string MakeKey (IITTrack track)
		{
			string location = (track is IITFileOrCDTrack
				? ((IITFileOrCDTrack)track).Location : String.Empty);

			return String.Format("{0}~{1}~{2}~~{3}",
				track.Artist, track.Name, track.Album, location);
		}


		private void AllProps (iTunesAppClass itunes)
		{
			bool acmpEnabled = itunes.AppCommandMessageProcessingEnabled;
			IITEncoder encoder = itunes.CurrentEncoder;
			IITEQPreset eqPreset = itunes.CurrentEQPreset;
			IITPlaylist playlist = itunes.CurrentPlaylist;
			string streamTitle = itunes.CurrentStreamTitle;
			string streamURL = itunes.CurrentStreamURL;
			IITTrack track = itunes.CurrentTrack;
			IITVisual visual = itunes.CurrentVisual;
			IITEncoderCollection encoders = itunes.Encoders;
			bool eqEnabled = itunes.EQEnabled;
			IITEQPresetCollection eqPresets = itunes.EQPresets;

			// this always seems to raise a COM exception, whether playing or stopped...
			//IITWindow eqWindow = itunes.EQWindow;
	
			bool fgOnDialog = itunes.ForceToForegroundOnDialog;
			bool fsVisuals = itunes.FullScreenVisuals;
			IITLibraryPlaylist libPlaylist = itunes.LibraryPlaylist;
			IITSource libSource = itunes.LibrarySource;
			string libXmlPath = itunes.LibraryXMLPath;
			bool mute = itunes.Mute;

			// this will raise a COM exception when iTunes first started
			//int position = itunes.PlayerPosition;
	
			ITPlayerState state = itunes.PlayerState;
			IITTrackCollection selectedTracks = itunes.SelectedTracks;
			int volume = itunes.SoundVolume;
			bool volEnabled = itunes.SoundVolumeControlEnabled;
			IITSourceCollection sources = itunes.Sources;
			string version = itunes.Version;
			IITVisualCollection visuals = itunes.Visuals;
			bool visualsEnabled = itunes.VisualsEnabled;
			ITVisualSize visualSize = itunes.VisualSize;
			IITWindowCollection windows = itunes.Windows;
		}


		private void DumpState (iTunesAppClass itunes)
		{
			IITPlaylist playlist = itunes.CurrentPlaylist;
			Debug.WriteLine("itunes.CurrentPlaylist");
			if (itunes.CurrentPlaylist == null)
			{
				Debug.WriteLine("... NULL");
			}
			else
			{
				Debug.WriteLine(String.Format("... Kind [{0}]", itunes.CurrentPlaylist.Kind));
				Debug.WriteLine(String.Format("... Name [{0}]", itunes.CurrentPlaylist.Name));
				Debug.WriteLine(String.Format("... Tracks.Count [{0}]", itunes.CurrentPlaylist.Tracks.Count));
			}

			string streamTitle = itunes.CurrentStreamTitle;
			Debug.WriteLine(String.Format("iTunes.CurrentStreamTitle [{0}]", itunes.CurrentStreamTitle));

			string streamURL = itunes.CurrentStreamURL;
			Debug.WriteLine(String.Format("iTunes.CurrentStreamURL [{0}]", itunes.CurrentStreamURL));

			IITTrack currentTrack = itunes.CurrentTrack;
			Debug.WriteLine("itunes.CurrentTrack");
			if (itunes.CurrentTrack == null)
			{
				Debug.WriteLine("... NULL");
			}
			else
			{
				Debug.WriteLine(String.Format("... Album [{0}]", currentTrack.Album));
				Debug.WriteLine(String.Format("... Artist [{0}]", currentTrack.Artist));
				Debug.WriteLine(String.Format("... Kind [{0}]", currentTrack.Kind));
				Debug.WriteLine(String.Format("... Name [{0}]", currentTrack.Name));
				Debug.WriteLine(String.Format("... trackID [{0}]", currentTrack.trackID));
			}

			int position = itunes.PlayerPosition;
			Debug.WriteLine(String.Format("iTunes.PlayerPosition [{0}]", itunes.PlayerPosition));

			ITPlayerState state = itunes.PlayerState;
			Debug.WriteLine(String.Format("iTunes.PlayerState [{0}]", itunes.PlayerState));

			IITTrackCollection selectedTracks = itunes.SelectedTracks;
			Debug.WriteLine("itunes.SelectedTracks");
			if (itunes.SelectedTracks == null)
			{
				Debug.WriteLine("... NULL");
			}
			else
			{
				foreach (IITTrack track in itunes.SelectedTracks)
				{
					Debug.WriteLine("... track");
					Debug.WriteLine(String.Format("... Album [{0}]", itunes.CurrentTrack.Album));
					Debug.WriteLine(String.Format("... Artist [{0}]", itunes.CurrentTrack.Artist));
					Debug.WriteLine(String.Format("... Kind [{0}]", itunes.CurrentTrack.Kind));
					Debug.WriteLine(String.Format("... Name [{0}]", itunes.CurrentTrack.Name));
					Debug.WriteLine(String.Format("... trackID [{0}]", itunes.CurrentTrack.trackID));
				}
			}
		}
	}
}
