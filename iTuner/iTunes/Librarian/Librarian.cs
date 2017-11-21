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
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using Resx = Properties.Resources;
	using Settings = Properties.Settings;


	/// <summary>
	/// Maintains the iTunes library by sequentially scheduling requested Scanner
	/// objects.  The Librarian maintains a continual loop, waiting on the blocking
	/// queue for new scanners to arrive.
	/// </summary>

	internal class Librarian : INotifyCollectionChanged, IDisposable
	{
		private const string LogCategory = "Library";

		private const uint InitializingScannerPriority = 0;
		private const uint ExportScannerPriority = 1;
		private const uint ImportScannerPriority = 1;
		private const uint DuplicateScannerPriority = 2;
		private const uint EmptyScannerPriority = 2;
		private const uint FileWatchScannerPriority = 2;
		private const uint MaintenanceScannerPriority = 2;
		private const uint PhantomScannerPriority = 2;
		private const uint ArtworkScannerPriority = 3;
		private const uint FixInformationScannerPriority = 3;

		private static Librarian librarian;

		private Controller controller;				// iTunes Controller
		private bool isDisposed;					// true if Librarian is disposed
		private IScanner scanner;					// currently active scanner, if any

		private object sync;						// critical section sync root
		private ICatalog catalog;					// reference to Catalog
		private BackgroundWorker worker;			// the library background worker
		private FileSystemWatcher watcher;			// watch for new files added to library
		private StringCollection cleansed;			// keep track of albums cleaned in session
		private StringCollection disabled;			// list of scanners explitly disabled
		private BlockingQueue<IScanner> queue;		// the library pipeline
		private SafeCollection<IScanner> actives;	// all queued or running scanners


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Private singleton constructor.
		/// </summary>

		private Librarian (Controller controller)
		{
			this.controller = controller;
			this.queue = new BlockingQueue<IScanner>();
			this.actives = new SafeCollection<IScanner>();
			this.cleansed = new StringCollection();
			this.disabled = new StringCollection();
			this.scanner = null;
			this.sync = new Object();

			// temporarily reference an empty catalog
			this.catalog = new TerseCatalog();

			this.worker = new BackgroundWorker();
			this.worker.DoWork += new DoWorkEventHandler(DoWork);
			this.worker.ProgressChanged += new ProgressChangedEventHandler(DoProgressChanged);
			this.worker.WorkerReportsProgress = true;
			this.worker.WorkerSupportsCancellation = true;
			this.worker.RunWorkerAsync();

			AddScanner(new InitializingScanner(controller, this), InitializingScannerPriority);
		}


		/// <summary>
		/// Completes the instance initialization.
		/// Invoked as the last step of the InitializingScanner.
		/// </summary>

		public void Initialize ()
		{
			string path = catalog.MusicPath;
			if (String.IsNullOrEmpty(path))
			{
				// disconnected mode
				return;
			}

			watcher = new FileSystemWatcher();
			watcher.IncludeSubdirectories = true;
			watcher.InternalBufferSize *= 2;
			watcher.Path = catalog.MusicPath;
			watcher.Filter = "*.*";
			watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName;
			watcher.Created += new FileSystemEventHandler(DoFileExistence);
			watcher.Deleted += new FileSystemEventHandler(DoFileExistence);
			watcher.Renamed += new RenamedEventHandler(DoFileRenamed);
			watcher.EnableRaisingEvents = true;
		}


		/// <summary>
		/// Factory method returning the singleton instance.
		/// </summary>
		/// <returns></returns>

		public static Librarian Create (Controller controller)
		{
			if (librarian == null)
			{
				librarian = new Librarian(controller);
			}

			return librarian;
		}


		#region Lifecycle

		/// <summary>
		/// Destructor.
		/// </summary>

		~Librarian ()
		{
			Dispose();
		}


		/// <summary>
		/// Ensures the queued workers are stopped and disposed.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				if (watcher != null)
				{
					watcher.EnableRaisingEvents = false;
					watcher.Dispose();
					watcher = null;
				}

				if (queue != null)
				{
					queue.Dispose();
					queue = null;
				}

				if (worker != null)
				{
					if (worker.IsBusy)
					{
						worker.CancelAsync();
					}

					worker.Dispose();
					worker = null;
				}

				if (catalog != null)
				{
					catalog.Dispose();
					catalog = null;
				}

				if (actives != null)
				{
					actives.Clear();
					actives = null;
				}

				if (disabled != null)
				{
					disabled.Clear();
					disabled = null;
				}

				if (cleansed != null)
				{
					cleansed.Clear();
					cleansed = null;
				}

				controller = null;

				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		#endregion Lifecycle


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// This event is fired when a scanner is added to or removed from the queue
		/// or when the queue is clears upon disposal.
		/// </summary>

		public event NotifyCollectionChangedEventHandler CollectionChanged;


		/// <summary>
		/// This event is fired at regular intervals as the synchronizer progresses.
		/// </summary>
		/// <remarks>
		/// The <i>sender</i> is set to the scanner instance.  The <i>e</i> arguments
		/// specify the percentage completed and a bit of user data to describe the
		/// current state.
		/// </remarks>

		public event ProgressChangedEventHandler ProgressChanged;


		/// <summary>
		/// Gets the number of tasks currently in queue or running.
		/// </summary>

		public int ActiveCount
		{
			get { return actives.Count; }
		}


		/// <summary>
		/// Gets the lLirary catalog from which we can extract various lists of information.
		/// </summary>

		public ICatalog Catalog
		{
			get { return catalog; }
			internal set { catalog = value; }
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// This is the worker method for the Librarian background thread.  It implements
		/// a continual loop waiting for scanning tasks to perform.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoWork (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;

			while (!worker.CancellationPending)
			{
				scanner = queue.Dequeue();

				if (worker.CancellationPending)
				{
					break;
				}

				if (watcher != null)
				{
					// Disable the watcher while scanning is active.  This way we won't have
					// file lock contentions or complex threading issues.
					watcher.EnableRaisingEvents = false;
				}

				ProgressChangedEventHandler progressHandler =
					new ProgressChangedEventHandler(DoScannerProgressChanged);

				scanner.ProgressChanged += progressHandler;

				worker.ReportProgress(10,
					new ScanningProgress(ScannerState.Beginning, scanner.Name));

				try
				{
					scanner.Execute();
				}
				catch (Exception exc)
				{
					App.LogException(new SmartException(exc));
				}

				RemoveScanner(scanner);

				if (watcher != null)
				{
					watcher.EnableRaisingEvents = true;
				}

				worker.ReportProgress(100,
					new ScanningProgress(ScannerState.Completed, scanner.Name));

				lock (sync)
				{
					scanner.ProgressChanged -= progressHandler;
					scanner = null;
				}
			}
		}


		/// <summary>
		/// Signals transition changes between scanner invocations.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoProgressChanged (object sender, ProgressChangedEventArgs e)
		{
			ScanningProgress progress = (ScanningProgress)e.UserState;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoScannerProgressChanged (object sender, ProgressChangedEventArgs e)
		{
			if (ProgressChanged != null)
			{
				ProgressChanged(sender, e);
			}
		}


		/// <summary>
		/// Handles file creation and deletion events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoFileExistence (object sender, FileSystemEventArgs e)
		{
			if (IsScannerAllowed(Resx.I_ScanFileWatch) &&
				!disabled.Contains(Resx.I_ScanFileWatch))
			{
				string ext = Path.GetExtension(e.FullPath);
				if (catalog.IsValidExtension(ext))
				{
					IScanner scanner = new FileWatchScanner(controller, catalog,
						new FileWatchAction(e.ChangeType, e.FullPath));

					AddScanner(scanner, FileWatchScannerPriority);
				}
			}
		}


		/// <summary>
		/// Handles file renamed events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoFileRenamed (object sender, RenamedEventArgs e)
		{
			if (IsScannerAllowed(Resx.I_ScanFileWatch) &&
				!disabled.Contains(Resx.I_ScanFileWatch))
			{
				string ext = Path.GetExtension(e.FullPath);
				if (catalog.IsValidExtension(ext))
				{
					IScanner scanner = new FileWatchScanner(controller, catalog,
						new FileWatchAction(e.ChangeType, e.FullPath, e.OldFullPath));

					AddScanner(scanner, FileWatchScannerPriority);
				}
			}
		}


		//========================================================================================
		// Public Methods
		//========================================================================================

		/// <summary>
		/// Determines if the specified scanner is currently queued or executing.
		/// </summary>
		/// <param name="name">The name of the scanner to query.</param>
		/// <returns></returns>

		public bool IsActive (string name)
		{
			// catalog is null until InitializingScanner is complete
			// so this effectively disables the Clean context menu items until then...

			if (catalog == null)
			{
				return false;
			}

			return actives.Any<IScanner>(p => p.Name.Equals(name));
		}


		/// <summary>
		/// Determines if the specified artist's album has been cleansed within the
		/// lifetime of this current iTuner session.
		/// </summary>
		/// <param name="album"></param>
		/// <param name="artist"></param>
		/// <returns></returns>

		public bool IsCleansed (string album, string artist)
		{
			return cleansed.Contains(album + "~" + artist);
		}


		/// <summary>
		/// Cancel the currently active scanner.
		/// </summary>

		public void Cancel ()
		{
			lock (sync)
			{
				if (scanner != null)
				{
					scanner.Cancel();
				}
			}
		}


		/// <summary>
		/// Clean the specified (current) artist's album.
		/// </summary>
		/// <param name="album"></param>
		/// <param name="artist"></param>
		/// <remarks>
		/// This is invoked automatically whenever a new track is played, as prescribed by
		/// the isCleansed verification.
		/// </remarks>

		public void Clean (string album, string artist)
		{
			if (catalog == null)
			{
				// catalog is null until InitializingScanner is complete
				return;
			}

			bool allowArtworkScanner = IsScannerAllowed(Resx.I_ScanArtwork);
			bool allowDuplicateScanner = IsScannerAllowed(Resx.I_ScanDuplicates);
			bool allowPhantomScanner = IsScannerAllowed(Resx.I_ScanPhantoms);

			if (allowArtworkScanner || allowDuplicateScanner || allowPhantomScanner)
			{
				string key = album + "~" + artist;

				string logtext = String.Format("Auto-cleaning album '{0}'", key);
				Logger.WriteAppLog(LogCategory, logtext);
				Logger.WriteLine(LogCategory, logtext);

				cleansed.Add(key);

				if (allowDuplicateScanner)
				{
					DuplicateScanner dscanner = new DuplicateScanner(controller, catalog);
					dscanner.AlbumFilter = album;
					dscanner.ArtistFilter = artist;
					AddScanner(dscanner, DuplicateScannerPriority);
				}

				if (allowPhantomScanner)
				{
					PhantomScanner pscanner = new PhantomScanner(controller, catalog);
					pscanner.AlbumFilter = album;
					pscanner.ArtistFilter = artist;
					AddScanner(pscanner, PhantomScannerPriority);
				}

				if (allowArtworkScanner && NetworkStatus.IsAvailable)
				{
					ArtworkScanner ascanner = new ArtworkScanner(controller, catalog);
					ascanner.AlbumFilter = album;
					ascanner.ArtistFilter = artist;
					AddScanner(ascanner, ArtworkScannerPriority);
				}
			}
		}


		/// <summary>
		/// Clean either the current album if in the Music library or the current user-defined
		/// playlist by scanning for duplicates and removing phantom tracks within that context.
		/// </summary>
		/// <remarks>
		/// This is invoked manually by the user from the Clean context menu.
		/// </remarks>

		public void CleanContext (bool isAlbumContext)
		{
			if (catalog == null)
			{
				// catalog is null until InitializingScanner is complete
				return;
			}

			if (isAlbumContext)
			{
				// DO NOT dispose current or we'll destroy the original CurrentTrack RCW
				Track current = controller.CurrentTrack;
				string album = current.Album;
				string artist = current.Artist;

				string key = album + "~" + artist;
				string logtext = String.Format("cleaning album '{0}'", key);
				Logger.WriteAppLog(LogCategory, logtext);
				Logger.WriteLine(LogCategory, logtext);

				cleansed.Add(key);

				var dscanner = new DuplicateScanner(controller, catalog);
				dscanner.AlbumFilter = album;
				dscanner.ArtistFilter = artist;
				AddScanner(dscanner, DuplicateScannerPriority);

				var pscanner = new PhantomScanner(controller, catalog);
				pscanner.AlbumFilter = album;
				pscanner.ArtistFilter = artist;
				AddScanner(pscanner, PhantomScannerPriority);

				if (NetworkStatus.IsAvailable)
				{
					var ascanner = new ArtworkScanner(controller, catalog);
					ascanner.AlbumFilter = album;
					ascanner.ArtistFilter = artist;
					AddScanner(ascanner, ArtworkScannerPriority);
				}
			}
			else // (isPlaylistContext)
			{
				// DO NOT dispose current or we'll destory the original CurrentPlaylist RCW
				Playlist current = controller.CurrentPlaylist;

				string logtext = String.Format("cleaning playlist '{0}'", current.Name);
				Logger.WriteAppLog(LogCategory, logtext);
				Logger.WriteLine(LogCategory, logtext);

				var dscanner = new DuplicateScanner(controller, catalog);
				dscanner.PlaylistFilter = current.PersistentID;
				AddScanner(dscanner, DuplicateScannerPriority);

				var pscanner = new PhantomScanner(controller, catalog);
				pscanner.PlaylistFilter = current.PersistentID;
				AddScanner(pscanner, PhantomScannerPriority);

				if (NetworkStatus.IsAvailable)
				{
					var ascanner = new ArtworkScanner(controller, catalog);
					ascanner.PlaylistFilter = current.PersistentID;
					AddScanner(ascanner, ArtworkScannerPriority);
				}
			}
		}


		/// <summary>
		/// Export the specified tracks to a given directory using the specified encoder.
		/// </summary>
		/// <param name="tracks"></param>
		/// <param name="encoder"></param>
		/// <param name="location"></param>
		/// <param name="createSubdir"></param>

		public void Export (
			PersistentIDCollection list, Encoder encoder,
			string playlistFormat, string location, bool createSubdirectories)
		{
			lock (sync)
			{
				disabled.Add(Resx.I_ScanDuplicates);
				disabled.Add(Resx.I_ScanFileWatch);
				disabled.Add(Resx.I_ScanMaintenance);
			}

			string pathFormat = null;
			if (createSubdirectories)
			{
				pathFormat = PathHelper.GetPathFormat(ExportScanner.DefaultPathFormat);
			}

			IScanner scanner = new ExportScanner(
				controller, catalog, list, encoder, playlistFormat, location, pathFormat, false);

			scanner.Completed = (Action)delegate
			{
				lock (sync)
				{
					disabled.Remove(Resx.I_ScanDuplicates);
					disabled.Remove(Resx.I_ScanFileWatch);
					disabled.Remove(Resx.I_ScanMaintenance);
				}
			};

			AddScanner(scanner, ExportScannerPriority);
		}


		/// <summary>
		/// Export the specified tracks to a given USB MP3 player, synchronizing the
		/// contents of the specified location.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="location"></param>
		/// <param name="pathFormat"></param>

		public void Export (PersistentIDCollection list, string location, string pathFormat)
		{
			if (disabled.Contains(Resx.I_ScanExport))
			{
				return;
			}

			Encoder mp3Encoder = null;
			foreach (Encoder encoder in controller.Encoders)
			{
				// skip the Null encoder that's automatically added to controller.Encoders
				if (!encoder.IsEmpty)
				{
					if (encoder.Format.Equals("MP3"))
					{
						mp3Encoder = encoder;
					}
				}
			}

			if (mp3Encoder != null)
			{
				lock (sync)
				{
					disabled.Add(Resx.I_ScanDuplicates);
					disabled.Add(Resx.I_ScanFileWatch);
					disabled.Add(Resx.I_ScanMaintenance);
				}

				IScanner scanner = new ExportScanner(
					controller, catalog, list,
					mp3Encoder,
					PlaylistProviderFactory.NoFormat,
					location, pathFormat, true);

				scanner.Completed = (Action)delegate
				{
					Encoder mp3 = mp3Encoder;
					lock (sync)
					{
						disabled.Remove(Resx.I_ScanDuplicates);
						disabled.Remove(Resx.I_ScanFileWatch);
						disabled.Remove(Resx.I_ScanMaintenance);
					}
					mp3 = null;
				};

				AddScanner(scanner, ExportScannerPriority);
			}

			// [foo] dispose this reference but do not release underlying instance passed to Librarian
			//mp3Encoder.Dispose(false);
		}


		/// <summary>
		/// Fix missing and incorrect information for all tracks in the current album.
		/// </summary>

		public void FixInformation (PersistentIDCollection list)
		{
			if (IsScannerAllowed(Resx.I_ScanInformation) && NetworkStatus.IsAvailable)
			{
				var scanner = new InformationScanner(controller, catalog, list);
				AddScanner(scanner, FixInformationScannerPriority);
			}
		}


		/// <summary>
		/// Create a new playlist populated with the tracks specified in the given playlist
		/// file.  These tracks are either referenced from the library if they exist or
		/// imported from the file system from the paths specified in the playlist file.
		/// </summary>

		public void Import (string path)
		{
			if (!disabled.Contains(Resx.I_ScanImportPlaylist))
			{
				IScanner scanner = new ImportPlaylistScanner(controller, catalog, path);
				AddScanner(scanner, ImportScannerPriority);
			}
		}


		/// <summary>
		/// Handle the iTunes DatabaseChangedEvent asynchronously.
		/// </summary>

		public void MaintainLibrary (MaintenanceAction action)
		{
			if (IsScannerAllowed(Resx.I_ScanMaintenance) &&
				!disabled.Contains(Resx.I_ScanMaintenance))
			{
				IScanner scanner = new MaintenanceScanner(controller, catalog, action);
				AddScanner(scanner, MaintenanceScannerPriority);
			}
		}


		/// <summary>
		/// Scan the entire iTunes library deleting all duplicate entries.  Determining
		/// the preferred entry vs. duplicate entries is based on attribute priorities.
		/// </summary>

		public void ScanDuplicates ()
		{
			if (!disabled.Contains(Resx.I_ScanDuplicates))
			{
				IScanner scanner = new DuplicateScanner(controller, catalog);
				AddScanner(scanner, DuplicateScannerPriority);
			}
		}


		/// <summary>
		/// Scan the entire iTunes library directory structure, deleting empty
		/// directories.  These may have been result of reorganizing the library
		/// using iTunes, which tends to leave these directories behind.
		/// </summary>

		public void ScanEmptyFolders ()
		{
			if (!disabled.Contains(Resx.I_ScanEmptyDirectories))
			{
				IScanner scanner = new EmptyScanner(controller, catalog);
				AddScanner(scanner, EmptyScannerPriority);
			}
		}


		/// <summary>
		/// Scan the entire iTunes library deleting all entries without a defined location
		/// or pointing to non-existent files.  Typically referred to as "dead tracks",
		/// these are <i>"phantom entries"</i>.
		/// </summary>

		public void ScanPhantomTracks ()
		{
			if (!disabled.Contains(Resx.I_ScanPhantoms))
			{
				IScanner scanner = new PhantomScanner(controller, catalog);
				AddScanner(scanner, PhantomScannerPriority);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scanner"></param>

		private void AddScanner (IScanner scanner, uint priority)
		{
			int index = queue.Enqueue(scanner, priority);

			if (index >= actives.Count)
			{
				actives.Add(scanner);
			}
			else
			{
				actives.Insert(index, scanner);
			}

			if (CollectionChanged != null)
			{
				CollectionChanged(queue,
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Add, scanner));
			}
		}


		/// <summary>
		/// Determines if the named scanner is enabled or disabled by either app configuration
		/// or user settings.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>

		private bool IsScannerAllowed (string name)
		{
			// first check user settings
			object setting = null;
			string settingName = String.Format("{0}IsEnabled", name);

			try
			{
				// unfortunately, there is no Contains method so we need to try/catch this
				setting = Settings.Default[settingName];
			}
			catch (SettingsPropertyNotFoundException)
			{
				setting = null;
			}

			if (setting != null)
			{
				if (setting is Boolean)
				{
					return (bool)setting;
				}
				else if (setting is String)
				{
					bool enabled = false;
					if (Boolean.TryParse((string)setting, out enabled))
					{
						return enabled;
					}
				}
			}

			// second check app.config
			string config = ConfigurationManager.AppSettings[name];
			if (config != null)
			{
				bool enabled = true;
				if (Boolean.TryParse(config, out enabled))
				{
					return enabled;
				}
			}

			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scanner"></param>

		private void RemoveScanner (IScanner scanner)
		{
			if (scanner.Completed != null)
			{
				scanner.Completed();
			}

			actives.Remove(scanner);

			if (CollectionChanged != null)
			{
				CollectionChanged(queue,
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Remove, scanner));
			}
		}
	}
}
