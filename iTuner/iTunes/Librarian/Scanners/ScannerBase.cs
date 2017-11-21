//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.ComponentModel;
	using System.Configuration;
	using System.IO;
	using System.Threading;
	using Resx = Properties.Resources;


	/// <summary>
	/// Abstract base class for all scanners.
	/// </summary>

	internal abstract class ScannerBase : IScanner, INotifyPropertyChanged
	{

		private static string archivePath;					// path of iTuner archive

		private int progressPercentage;
		private Action completedAction;

		private bool waitAvailable;
		private object waitSyncRoot;						// COM disabled synchronizer
		private ReaderWriterLockSlim slim;					// waitSyncRoot lock
		private InteractionEnabledHandler enabledHandler;
		private InteractionDisabledHandler disabledHandler;


		/// <summary>
		/// Configuration setting, true if data should persist
		/// </summary>

		protected static bool isLive;


		/// <summary>
		/// The iTunes library catalog provider.
		/// </summary>

		protected ICatalog catalog;


		/// <summary>
		/// Reference to iTunes COM interface.
		/// </summary>

		protected Controller controller;


		/// <summary>
		/// The internal name of this scanner.
		/// </summary>

		protected string name;


		/// <summary>
		/// The user-friendly name of this scanner.
		/// </summary>

		protected string description;


		/// <summary>
		/// The context sensitive tooltip of the active scanner.
		/// </summary>

		protected string tooltip;


		/// <summary>
		/// True until explicitly cancelled.
		/// </summary>

		protected bool isActive = true;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize the static configuration for all scanners.
		/// </summary>

		static ScannerBase ()
		{
			archivePath = null;
			string mode = ConfigurationManager.AppSettings["LibraryMode"] ?? String.Empty;
			ScannerBase.isLive = !mode.Trim().ToLower().Equals("debug");
		}


		/// <summary>
		/// Unused.
		/// </summary>

		public ScannerBase ()
		{
			// Do not use
		}


		/// <summary>
		/// Base constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="controller"></param>
		/// <param name="catalog"></param>

		public ScannerBase (string name, Controller controller, ICatalog catalog)
		{
			this.name = name;
			this.description = String.Empty;
			this.tooltip = String.Empty;
			this.controller = controller;
			this.catalog = catalog;
			this.progressPercentage = 0;
			this.completedAction = null;
			this.waitAvailable = false;
			this.waitSyncRoot = null;
		}


		//========================================================================================
		// Properties/Events
		//========================================================================================

		/// <summary>
		/// This event is fired when the value of a bindable property is changed.
		/// </summary>

		public event PropertyChangedEventHandler PropertyChanged;


		/// <summary>
		/// This event is fired at regular intervals as the synchronizer progresses.
		/// </summary>

		public event ProgressChangedEventHandler ProgressChanged;


		/// <summary>
		/// Gets the path of the iTuner archive directory.
		/// </summary>

		public string ArchivePath
		{
			get
			{
				if (ScannerBase.archivePath == null)
				{
					ScannerBase.archivePath = GetArchivePath();
				}

				return ScannerBase.archivePath;
			}
		}


		/// <summary>
		/// Gets or sets an Action to invoke at the completion of this scanner.  This
		/// is used to remove disabled scanners from the Librarian <i>disabled</i> collection.
		/// </summary>

		public Action Completed
		{
			get { return completedAction; }
			set { completedAction = value; }
		}

	
		/// <summary>
		/// Gets the user-friendly name of this scanner.  Inheritors must set the
		/// protected <i>description</i> field in their constructors.
		/// </summary>

		public string Description
		{
			get { return description; }
		}


		/// <summary>
		/// Gets the name of this scanner.  Inheritors must set the protected
		/// <i>name</i> field in their constructors.
		/// </summary>

		public string Name
		{
			get { return name; }
		}


		/// <summary>
		/// Gets or sets the percent completed by this scanner.  This is a bindable
		/// property used for the Librarian status panel.
		/// </summary>

		public int ProgressPercentage
		{
			get
			{
				return progressPercentage;
			}

			set
			{
				progressPercentage = value;
				OnPropertyChanged("ProgressPercentage");
			}
		}


		/// <summary>
		/// Gets the context-sensitive tooltip of this scanner.
		/// </summary>

		public string Tooltip
		{
			get { return tooltip; }
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Cancel execution of this scanner.  This occurs synchronously at the scope
		/// of an atomic task.  Implementors are required to check the <i>isActive</i>
		/// protected member at regular intervals to ensure reasonable immediacy.
		/// </summary>

		public void Cancel ()
		{
			isActive = false;
		}


		/// <summary>
		/// Execute this scanner synchronously.
		/// </summary>
		/// <remarks>
		/// Scanners should be implemented as a sequence or loop of atomic tasks where
		/// the scanner has reasonably small increments between which they can check
		/// for a cancellation condition.
		/// </remarks>

		public abstract void Execute ();


		/// <summary>
		/// Determines the best possible path for the iTuner archive directory.  This
		/// tries a couple of different preferred paths before resorting to the Recycle bin.
		/// </summary>
		/// <returns></returns>

		private string GetArchivePath ()
		{
			// C:\Users\steven\_iTunerArchive
			string root = Environment.GetEnvironmentVariable("USERPROFILE");
			string path = Path.Combine(root, Resx.I_ArchiveRootPath);
			if (!TryCreateArchive(path))
			{
				// C:\Users\steven\AppData\Local\iTuner\_iTunerArchive
				root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				path = Path.Combine(Path.Combine(root, Resx.I_ApplicationProduct), Resx.I_ArchiveRootPath);
				if (!TryCreateArchive(path))
				{
					// C:\_iTunerArchive
					root = @"C:\";
					path = Path.Combine(root, Resx.I_ArchiveRootPath);
					if (!TryCreateArchive(path))
					{
						// C:\Users\steven\AppData\Local\TEMP\_iTunerArchive
						root = Path.GetTempPath();
						path = Path.Combine(root, Resx.I_ArchiveRootPath);
						if (!TryCreateArchive(path))
						{
							path = null;
						}
					}
				}
			}

			return path;
		}


		private bool TryCreateArchive (string path)
		{
			bool success = Directory.Exists(path);

			if (!success)
			{
				try
				{
					Directory.CreateDirectory(path);
					success = true;
				}
				catch
				{
					success = false;
				}
			}

			return success;
		}


		/// <summary>
		/// Get a lits of iTunes Tracks given a list of track IDs.
		/// </summary>
		/// <param name="trackIDs"></param>
		/// <returns></returns>

		protected TrackCollection GetTracks (PersistentIDCollection persistentIDs)
		{
			TrackCollection tracks = new TrackCollection();

			foreach (PersistentID persistentID in persistentIDs)
			{
				Track track = controller.LibraryPlaylist.GetTrack(persistentID);
				if (track != null)
				{
					tracks.Add(track);
				}
			}

			return tracks;
		}


		/// <summary>
		/// Notifies binded consumers that the value of the specified property has changed.
		/// </summary>
		/// <param name="name"></param>

		public void OnPropertyChanged (string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}

	
		/// <summary>
		/// Notify consumers of updated progress information.
		/// </summary>
		/// <param name="userState">A unique user state.</param>

		protected void UpdateProgress (object userState)
		{
			if (ProgressChanged != null)
			{
				ProgressChanged(
					this, new ProgressChangedEventArgs(progressPercentage, userState));
			}
		}


		//========================================================================================
		// COM Interrupt handling
		//========================================================================================

		protected void CheckInterrupt ()
		{
			// iTunes must be blocking with a user dialog
			slim.EnterReadLock();
			try
			{
				if (waitSyncRoot != null)
				{
					Logger.WriteLine(name, "waiting on COM interrupt...");
					lock (waitSyncRoot) Monitor.Wait(waitSyncRoot);
					waitSyncRoot = null;
					Logger.WriteLine(name, "resumed from COM interrupt");
				}
			}
			finally
			{
				slim.ExitReadLock();
			}

			if (waitAvailable)
			{
				// wait until available; until user dismisses dialog box...
				try
				{
					// if we're currently in a protection fault then this will block until
					// the user dismisses the dialog...

					using (var track = controller.LibraryPlaylist.GetTrack(0))
					{
						Logger.WriteLine(Logger.Level.Debug, name, "Block cleared");
					}
				}
				catch (Exception exc)
				{
					Logger.WriteLine(
						name, "Error while waiting for Protected fault to recover", exc);
				}

				waitAvailable = false;
			}
		}


		/// <summary>
		/// 
		/// </summary>

		protected void DisableInterrupt (Controller controller)
		{
			// detach COM status handlers
			controller.DisabledEvent -= disabledHandler;
			controller.EnabledEvent -= enabledHandler;

			disabledHandler = null;
			enabledHandler = null;

			waitSyncRoot = null;

			slim.Dispose();
			slim = null;
		}


		/// <summary>
		/// 
		/// </summary>

		protected void EnableInterrupt (Controller controller)
		{
			waitSyncRoot = null;
			slim = new ReaderWriterLockSlim();

			disabledHandler = new InteractionDisabledHandler(DoCOMCallsDisabled);
			enabledHandler = new InteractionEnabledHandler(DoCOMCallsEnabled);

			// attach COM status handlers
			controller.DisabledEvent += disabledHandler;
			controller.EnabledEvent += enabledHandler;
		}


		/// <summary>
		/// 
		/// </summary>

		protected void SetInterrupt ()
		{
			waitAvailable = true;
		}


		/// <summary>
		/// 
		/// </summary>

		protected void WaitOnInterrupt ()
		{
			slim.EnterWriteLock();
			try
			{
				if (waitSyncRoot == null)
				{
					waitSyncRoot = new object();
				}
			}
			finally
			{
				slim.ExitWriteLock();
			}
		}


		/// <summary>
		/// The ITEventCOMCallsDisabled event is fired when calls to the iTunes COM interface
		/// will be deferred.  Typically, iTunes will defer COM calls when any modal dialog
		/// is being displayed. When the user dismisses the last modal dialog, COM calls will
		/// be enabled again, and any deferred COM calls will be executed. You can use this event
		/// to avoid making a COM call which will be deferred.
		/// </summary>
		/// <param name="reason"></param>

		private void DoCOMCallsDisabled (InteractionDisabledReason reason)
		{
			if (reason == InteractionDisabledReason.Dialog)
			{
				slim.EnterWriteLock();
				try
				{
					if (waitSyncRoot != null)
					{
						// force main thread to block/wait for COM calls to be re-enabled;
						// blocking will end once DoCOMCallsEnabled is called
						waitSyncRoot = new object();
					}
				}
				finally
				{
					slim.ExitWriteLock();
				}
			}
		}


		/// <summary>
		/// The ITEventCOMCallsEnabled event is fired when calls to the iTunes COM interface
		/// will no longer be deferred.  Typically, iTunes will defer COM calls when any modal
		/// dialog is being displayed. When the user dismisses the last modal dialog, COM calls
		/// will be enabled again, and any deferred COM calls will be executed.
		/// </summary>

		private void DoCOMCallsEnabled ()
		{
			slim.EnterReadLock();
			try
			{
				// signal DoCOMCallsDisabled to continue
				lock (waitSyncRoot) Monitor.Pulse(waitSyncRoot);
			}
			finally
			{
				slim.ExitReadLock();
			}
		}
	}
}
