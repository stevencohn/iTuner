//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using Microsoft.Win32;
	using Resx = Properties.Resources;


	/// <summary>
	/// Automates file changes beneath the iTunes library directory.
	/// </summary>

	internal class FileWatchScanner : ScannerBase
	{
		private FileWatchAction action;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scaner with the specified iTunes interface.
		/// </summary>
		/// <param name="itunes"></param>
		/// <param name="catalog"></param>

		public FileWatchScanner (Controller controller, ICatalog catalog, FileWatchAction action)
			: base(Resx.I_ScanFileWatch, controller, catalog)
		{
			base.description = Resx.ScanFileWatch;
			base.tooltip = action.ChangeType + ": " + action.FullPath;

			this.action = action;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Execute the scanner.
		/// </summary>

		public override void Execute ()
		{
			PersistentID persistentID;

			switch (action.ChangeType)
			{
				case WatcherChangeTypes.Created:
					Track track = CreateTrack(action.FullPath);
					if (track != null)
					{
						catalog.AddTrack(track);
						track.Dispose();
						track = null;
					}
					break;

				case WatcherChangeTypes.Deleted:
					persistentID = catalog.DeleteFile(action.FullPath);
					if (!persistentID.IsEmpty)
					{
						DeleteTrack(persistentID);
					}
					break;

				case WatcherChangeTypes.Renamed:
					persistentID = catalog.GetPersistentIDByLocation(action.OldPath);
					if (!persistentID.IsEmpty)
					{
						string realPath = RenameTrackLocation(persistentID, action.FullPath);
						if (realPath.Equals(action.FullPath))
						{
							catalog.RenameFile(action.OldPath, action.FullPath);
						}
					}
					break;
			}
		}


		/// <summary>
		/// Creates a new track by importing the specified file.
		/// </summary>
		/// <param name="path"></param>

		private Track CreateTrack (string path)
		{
			Track track = null;
			string extension = Path.GetExtension(path);

			try
			{
				if (ScannerBase.isLive)
				{
					if (Encoder.IsNativeExtension(extension))
					{
						track = AddNativeFile(path);
					}

					// either not native or unsuccessfully added as native; needs conversion
					if (track == null)
					{
						track = AddConvertedFile(path, extension);
					}
				}
			}
			catch (Exception exc)
			{
				Logger.WriteLine(base.name, "Error importing " + path, exc);
			}

			return track;
		}


		private Track AddNativeFile (string path)
		{
			Track track = controller.LibraryPlaylist.AddFile(path);

			if (track != null)
			{
				// successfully imported raw file without conversion
				Logger.WriteLine(
					Logger.Level.Debug, base.name, "Imported native file " + path);
			}
			else
			{
				Logger.WriteLine(
					Logger.Level.Debug, base.name, "Failed to import native file " + path);
			}

			return track;
		}


		private Track AddConvertedFile (string path, string extension)
		{
			Track track = null;

			RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension);
			if (key != null)
			{
				string perceived = key.GetValue("PerceivedType", "") as String;

				if (!String.IsNullOrEmpty(perceived) && perceived.Equals("audio"))
				{
					Encoder mp3Encoder = null;
					foreach (Encoder encoder in controller.Encoders)
					{
						// TODO: I'm using the constant and primary object here because
						// encoder.Format sometime returns null; not sure why?

						if ("MP3".Equals(encoder.Format))
						{
							mp3Encoder = encoder;
						}
					}

					if (mp3Encoder != null)
					{
						// remember user's preferred encoder
						Encoder saveEncoder = controller.CurrentEncoder;

						// temporarily set up an MP3 encoder
						controller.CurrentEncoder = mp3Encoder;

						// convert and import the file
						track = controller.ConvertFile2(path);

						// restore user's preferred encoder
						controller.CurrentEncoder = saveEncoder;

						if (track != null)
						{
							Logger.WriteLine(Logger.Level.Debug, base.name, "Imported " + path);
						}
					}
				}
			}

			return track;
		}


		/// <summary>
		/// Delete the specified track from all playlists and the main library.
		/// </summary>
		/// <param name="persistentID"></param>

		private void DeleteTrack (PersistentID persistentID)
		{
			Track track = controller.LibraryPlaylist.GetTrack(persistentID);

			// when a track is deleted from a source's primary playlist, it will be deleted
			// from all playlist's in that source

			if (track != null)
			{
				Logger.WriteLine(base.name, "Deleting track " + track.MakeKey());

				try
				{
					if (ScannerBase.isLive)
					{
						track.Delete();
					}
				}
				catch (Exception exc)
				{
					Logger.WriteLine(base.name,
						"Error deleting track " + track.MakeKey(), exc);
				}
				finally
				{
					track.Dispose();
					track = null;
				}
			}
		}


		/// <summary>
		/// Update the specified track with the given location fullpath.
		/// </summary>
		/// <param name="persistentID"></param>
		/// <param name="path"></param>
		/// <returns>
		/// The iTunes imposed full path of the track.
		/// </returns>

		private string RenameTrackLocation (PersistentID persistentID, string path)
		{
			Track track = controller.LibraryPlaylist.GetTrack(persistentID);
			string realPath = path;

			if (track != null)
			{
				Logger.WriteLine(base.name, String.Format(
					"Updating track location {0} to {1}", track.MakeKey(), path));

				try
				{
					if (ScannerBase.isLive)
					{
						// Note that this WILL set the path correctly, however, if iTunes is
						// set to automatically manage the library, it will IMMEDIATELY
						// rename the file to its prescribed correct name via ID3 tags...
						// In other words, even if you manually renamed the file
						// "Aerosmith\Dream On.mp3" to 'Aerosmith\Dream_On_1.mp3", iTunes will
						// immediately rename it back to "Aerosmith\Dream On.mp3"

						track.Location = path;

						// so now get the track again, otherwise it's not immediately refreshed,
						// and grab the real path applied by iTunes

						track = controller.LibraryPlaylist.GetTrack(persistentID);
						realPath = track.Location;
					}
				}
				catch (Exception exc)
				{
					Logger.WriteLine(base.name,
						"Error changing track location for " + track.MakeKey(), exc);
				}
				finally
				{
					track.Dispose();
					track = null;
				}
			}

			return realPath;
		}
	}
}
