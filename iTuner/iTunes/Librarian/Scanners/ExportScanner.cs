//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Specialized;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using Resx = Properties.Resources;


	/// <summary>
	/// Export the specified tracks to a given directory using the specified encoder.
	/// </summary>

	internal class ExportScanner : ScannerBase
	{
		public const string DefaultPathFormat = "R_L_T";
		public const string FlatPathFormat = "RLT";

		private PersistentIDCollection exportPIDs;	// param, list of persistent IDs to export
		private Encoder encoder;					// selected encoder (or None)
		private string playlistFormat;				// playlist writer format  (or None)
		private string playlistName;				// default playlist name
		private string location;					// root output directory
		private string pathFormat;					// format of output path
		private bool isSynchronized;				// true if sync target path to list
		private bool createSubdirectories;			// true if artist/album subdirectories

		private string expectedType;				// file type of encoded media files
		private IPlaylistWriter writer;				// playlist writer
		private ManualResetEvent reset;				// sequential synchronizer
		private StringCollection exports;			// list of exported files

		private Encoder saveEncoder = null;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scanner.
		/// </summary>
		/// <param name="controller">The iTunesAppClass to use.</param>
		/// <param name="catalog"></param>
		/// <param name="exportPIDs">
		/// The list of persistent track IDs.  When exporting multiple playlists, this list
		/// should include all tracks from all playlists where each track indicates its own
		/// source playlist.  This allows comprehensive/overall feedback for UI progress bars.
		/// </param>
		/// <param name="encoder">The encoder to use to convert tracks.</param>
		/// <param name="playlistFormat">
		/// The playlist output format or PlaylistProviderFactory.NoFormat for no playlist file.
		/// </param>
		/// <param name="location">
		/// The target root directory path to which all entries will be copied or converted.
		/// </param>
		/// <param name="pathFormat">
		/// The path format string as specified in FolderFormts.xml or null for title only.
		/// </param>
		/// <param name="isSynchronized">
		/// True if target location should be synchronized, removing entries in the target
		/// location that are not included in the exportPIDs collection.
		/// </param>

		public ExportScanner (
			Controller controller, ICatalog catalog, PersistentIDCollection exportPIDs,
			Encoder encoder, string playlistFormat, string location, string pathFormat,
			bool isSynchronized)
			: base(Resx.I_ScanExport, controller, catalog)
		{
			base.description = isSynchronized ? Resx.ScanSynchronize : Resx.ScanExport;
			base.tooltip = String.Format(Resx.ExportingCount, exportPIDs.Count);

			this.exportPIDs = exportPIDs;
			this.encoder = encoder;
			this.expectedType = (encoder == null ? String.Empty : encoder.ExpectedType);
			this.playlistFormat = playlistFormat;
			this.playlistName = exportPIDs.Name;
			this.location = location;
			this.createSubdirectories = (pathFormat != null);
			this.pathFormat = pathFormat ?? PathHelper.GetPathFormat(FlatPathFormat);
			this.isSynchronized = isSynchronized;

			this.exports = null;
			this.writer = null;
		}


		//========================================================================================
		// Execute()
		//========================================================================================

		/// <summary>
		/// Execute the scanner.
		/// </summary>

		public override void Execute ()
		{
			Initialize();

			try
			{
				ExecuteInternal();
			}
			catch (Exception exc)
			{
				Logger.WriteLine(base.name, exc);
			}
			finally
			{
				Cleanup();
			}
		}


		#region Initialize/Cleanup

		/// <summary>
		/// Initialize the environment for an export or synchronize operation.
		/// </summary>

		private void Initialize ()
		{
			Logger.WriteLine(base.name,
				String.Format("Export beginning, using encoder '{0}'",
				encoder == null ? "(skip)" : encoder.Name));

			// attach COM status handlers
			base.EnableInterrupt(controller);

			// override current encoder if different than requested
			if ((encoder != null) && !encoder.IsEmpty)
			{
				if (!controller.CurrentEncoder.Name.Equals(encoder.Name))
				{
					// remember user's preferred encoder
					saveEncoder = controller.CurrentEncoder;

					// set the temporary encoder for this playlist
					controller.CurrentEncoder = encoder;
				}
			}

			// ensure that 'location' exists before continuing.  This will be more efficient
			// for cases that do not require further subdirectories but also sets the stage...

			if (!Directory.Exists(location))
			{
				if (ScannerBase.isLive)
				{
					Directory.CreateDirectory(location);
				}
			}

			// if we do want subdirectories then ensure we can create them
			if (!createSubdirectories)
			{
				createSubdirectories = EnsureFolderCapability();
			}

			// initialize and open the playlist writer if a playlist is requested
			if (ScannerBase.isLive && !playlistFormat.Equals(PlaylistProviderFactory.NoFormat))
			{
				writer = PlaylistProviderFactory.CreateWriter(
					playlistFormat, location, exportPIDs.Name, createSubdirectories);
			}

			// this ManualResetEvent is used to synchronize sequential processing of iTunes
			// tracks as they are encoded.  iTunes encoding is an asynchronous procedure
			// however, iTunes does not allow concurrent encodings to occur.

			reset = new ManualResetEvent(false);
			exports = new StringCollection();
		}


		/// <summary>
		/// Cleanup anything started in Initialize.
		/// </summary>

		private void Cleanup ()
		{
			// detach COM status handlers
			base.DisableInterrupt(controller);

			if (saveEncoder != null)
			{
				try
				{
					// restore the iTunes encoder as specified in the user Preferences
					controller.CurrentEncoder = saveEncoder;
				}
				catch
				{
					// no-op
				}
			}

			if (encoder != null)
			{
				encoder.Dispose(true);
				encoder = null;
			}

			if (writer != null)
			{
				writer.Close();
				writer.Dispose();
				writer = null;
			}

			if (exports != null)
			{
				exports.Clear();
				exports = null;
			}

			if (reset != null)
			{
				reset.Dispose();
				reset = null;
			}

			base.ProgressPercentage = 100;
			base.UpdateProgress(Resx.Completed);

			Logger.WriteLine(base.name, "Export completed");
		}

		#endregion Initialize/Cleanup

	
		/// <summary>
		/// 
		/// </summary>

		private void ExecuteInternal ()
		{
			string description = String.Empty;
			int total = exportPIDs.Count;
			int count = 0;

			foreach (PersistentID persistentID in exportPIDs)
			{
				if (!base.isActive)
				{
					// scanner cancelled by user
					break;
				}

				// is iTunes blocking with a user dialog?
				base.CheckInterrupt();

				try
				{
					Track track = controller.LibraryPlaylist.GetTrack(persistentID);
					if (track != null)
					{
						// indicate a slight progress at the beginning by calculating the % with
						// an 0.5 fudge to ensure we see a little "green" in the progress bar
						base.ProgressPercentage = (int)(((double)count + 0.5) / (double)total * 100.0);
						description = String.Format("{0}, {1}", track.Name, track.Artist);
						UpdateProgress(description);

						if (File.Exists(track.Location))
						{
							if (track.KindAsString.Contains(Resx.ProtectedAAC))
							{
								Logger.WriteLine(Logger.Level.Info, base.name,
									"Skipping protected track " + track.Location);
							}
							else
							{
								ExportTrack(track, persistentID.PlaylistName ?? playlistName);
							}
						}
						else
						{
							Logger.WriteLine(Logger.Level.Debug, base.name,
								"Skipping dead track " + track.Location);
						}

						track.Dispose();
						track = null;
					}
				}
				catch (ProtectedException)
				{
					base.SetInterrupt();
				}
				catch (COMException)
				{
					base.WaitOnInterrupt();
				}
				catch (Exception exc)
				{
					Logger.WriteLine(base.name, "Error encoding " + persistentID.ToString(), exc);
				}

				// indicate the end of this step by calculating the percentage
				// of count to end the range...
				count++;
				base.ProgressPercentage = (int)((double)count / (double)total * 100.0);
				UpdateProgress(description);
			}

			if (base.isActive)
			{
				// only remove unwanted files if we know we placed wanted files!
				// otherwise we could end up deleting the 'location' directory itself
				if (isSynchronized && (exports.Count > 0))
				{
					ReconcileExports(new DirectoryInfo(location));
				}
			}
		}


		//========================================================================================
		// Export/Synchronize...
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>

		private void ExportTrack (Track track, string playlistName)
		{
			string filename;
			string path = MakeFormattedPath(track, playlistName, out filename);
			string ext = Path.GetExtension(track.Location);

			if (encoder == null)
			{
				if (writer != null)
				{
					writer.Add(track, track.Location);
				}
			}
			else
			{
				if (encoder.IsEmpty || ext.Equals(expectedType))
				{
					// the existing track file is already in the proper format, no encoding
					// necessary so we only need to copy that file to the archive location

					string export = Path.Combine(path, Path.GetFileName(filename));

					CopyTrack(track, export);
				}
				else
				{
					string export = Path.Combine(
						path, Path.GetFileNameWithoutExtension(filename) + expectedType);

					ConvertTrack(track, export);
				}
			}
		}


		private void CopyTrack (Track track, string export)
		{
			if (!File.Exists(export))
			{
				Exception exception = null;

				if (ScannerBase.isLive)
				{
					try
					{
						EnsurePath(Path.GetDirectoryName(export));
						File.Copy(track.Location, export, true);

						if (writer != null)
						{
							writer.Add(track, export);
						}
					}
					catch (Exception exc)
					{
						exception = exc;
					}
				}

				if (exception == null)
				{
					Logger.WriteLine(Logger.Level.Debug, base.name, "Copied " + export);
				}
				else
				{
					Logger.WriteLine(base.name, "Error copying " + track.Location, exception);
				}
			}

			exports.Add(export.ToLower());
		}


		private void ConvertTrack (Track track, string export)
		{
			// the existing track file needs to be encoded and that encoded file needs to
			// be moved to the archive location, do not leave this file in the library or
			// it would be treated as a rogue track and possibly imported into the library

			try
			{
				if (File.Exists(export))
				{
					exports.Add(export.ToLower());
				}
				else if (ScannerBase.isLive)
				{
					Track transient = controller.ConvertTrack2(track);

					if (transient != null)
					{
						string encoded = transient.Location;

						// remove the new track from the library; does not delete file
						transient.Delete();

						// move the newly created media file to the export location
						EnsurePath(Path.GetDirectoryName(export));
						File.Move(encoded, export);

						if (writer != null)
						{
							writer.Add(track, export);
						}

						exports.Add(export.ToLower());

						Logger.WriteLine(Logger.Level.Debug, base.name, "Encoded " + export);
					}
				}
			}
			catch (COMException exc)
			{
				Logger.WriteLine(base.name, "COMException while encoding " + track.Location, exc);

				throw;
			}
			catch (ProtectedException exc)
			{
				Logger.WriteLine(base.name, "ProtectedException while encoding " + track.Location, exc);

				throw;
			}
			catch (Exception exc)
			{
				Logger.WriteLine(base.name, "Error encoding " + track.Location, exc);
			}
		}


		#region Helpers

		/// <summary>
		/// Determins if we can create sub-directories under the given 'location'.
		/// </summary>
		/// <returns>
		/// A <b>Boolean</b> value indicating whether we can create sub-directories.
		/// </returns>

		private bool EnsureFolderCapability ()
		{
			bool isCapable = (pathFormat != null) &&
				pathFormat.Contains(Path.DirectorySeparatorChar.ToString());

			if (isCapable)
			{
				string tmp = Path.Combine(location, Guid.NewGuid().ToString("N"));
				try
				{
					DirectoryInfo dir = Directory.CreateDirectory(tmp);
					Directory.Delete(tmp);
				}
				catch
				{
					pathFormat = PathHelper.GetPathFormat(FlatPathFormat);
					isCapable = false;
				}
			}

			return isCapable;
		}


		/// <summary>
		/// Ensure the proper sub-directory path exists before exporting.  When exporting
		/// to a single directory, location is returned; otherwise, a track-specific
		/// directory is created.
		/// </summary>
		/// <param name="track"></param>
		/// <param name="playlistName"></param>
		/// <returns>A string specifying the absolute path to the export directory.</returns>

		private void EnsurePath (string path)
		{
			if (!Directory.Exists(path))
			{
				if (ScannerBase.isLive)
				{
					Directory.CreateDirectory(path);
				}
			}
		}


		/// <summary>
		/// Constructs a directory path and file name from the given input according
		/// to the current pathFormat.
		/// </summary>
		/// <param name="track">The track to export.</param>
		/// <param name="playlist">The playlist of the track to export.</param>
		/// <param name="name">The resultant file name without its leading path.</param>
		/// <returns>A string specifying the directory path to contain the file.</returns>

		private string MakeFormattedPath (Track track, string playlistName, out string filename)
		{
			// TODO: do we need to add track # (respective to playlist)

			if (String.IsNullOrEmpty(pathFormat))
			{
				// TODO: do we need to default to "RLT" instead of just track name to
				// avoid conflicts?

				filename = Path.GetFileName(PathHelper.CleanFileName(track.Name))
					+ Path.GetExtension(track.Location);

				return location;
			}

			Match match = Regex.Match(pathFormat, @"\{(.*?)\}");
			StringBuilder result = new StringBuilder(pathFormat);

			while (match.Success)
			{
				switch (match.Groups[1].Value)
				{
					case "album":
						result.Replace(match.Value, PathHelper.CleanFileName(track.Album));
						break;

					case "artist":
						result.Replace(match.Value, PathHelper.CleanFileName(track.Artist));
						break;

					case "genre":
						result.Replace(match.Value, PathHelper.CleanFileName(track.Genre));
						break;

					case "playlist":
						result.Replace(match.Value, PathHelper.CleanFileName(playlistName));
						break;

					case "title":
						result.Replace(match.Value, PathHelper.CleanFileName(track.Name));
						break;
				}

				match = match.NextMatch();
			}

			// clean each part of the full path to fix names like "AC/DC" to be "AC_DC"
			// otherwise, these would be construed as invalid chars in the path

			string path = Path.Combine(location, result.ToString());

			filename = PathHelper.CleanFileName(Path.GetFileName(path)) +
				Path.GetExtension(track.Location);

			path = PathHelper.CleanDirectoryPath(Path.GetDirectoryName(path));

			return path;
		}


		/// <summary>
		/// Delete any file and empty directory that is not associated with the export list.
		/// </summary>
		/// <param name="dir">The directory in which to begin.</param>

		private void ReconcileExports (DirectoryInfo dir)
		{
			foreach (DirectoryInfo subdir in dir.GetDirectories())
			{
				ReconcileExports(subdir);
			}

			foreach (FileInfo file in dir.GetFiles())
			{
				if (!exports.Contains(file.FullName.ToLower()))
				{
					if (ScannerBase.isLive)
					{
						if ((file.Attributes & FileAttributes.ReadOnly) > 0)
						{
							file.Attributes ^= FileAttributes.ReadOnly;
						}

						file.Delete();
					}
				}
			}

			if ((dir.GetFiles().Length == 0) && (dir.GetDirectories().Length == 0))
			{
				if ((dir.Attributes & FileAttributes.ReadOnly) > 0)
				{
					dir.Attributes ^= FileAttributes.ReadOnly;
				}

				dir.Delete();
			}
		}

		#endregion Helpers
	}
}
