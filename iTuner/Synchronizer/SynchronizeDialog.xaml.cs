//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using iTuner.iTunes;
	using iTuner.Controls;
	using Resx = Properties.Resources;


	/// <summary>
	/// Interaction logic for SynchronizeDialog.xaml
	/// </summary>

	internal partial class SynchronizeDialog : MovableWindow
	{
		private Controller controller;
		private UsbManager manager;
		private UsbDiskCollection disks;
		private PlaylistCollection playlists;
		private int percentageCompleted;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Default constructor for VS and Blend designers.
		/// </summary>

		public SynchronizeDialog ()
			: base()
		{
			this.InitializeComponent();
			this.Closed += new EventHandler(DoClosed);
			this.Closing += new CancelEventHandler(DoClosing);

			InitializeDragHandler(detailPanel);
		}


		/// <summary>
		/// Initialize a new instance of the dialog using the given Controller.
		/// </summary>
		/// <param name="controller"></param>

		public SynchronizeDialog (Controller controller)
			: this()
		{
			this.controller = controller;

			this.manager = new UsbManager();
			this.disks = manager.GetAvailableDisks();
			this.playlists = null;
			this.percentageCompleted = 0;

			this.selector.Loaded += new RoutedEventHandler(DoLoaded);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoLoaded (object sender, RoutedEventArgs e)
		{
			manager.StateChanged += new UsbStateChangedEventHandler(DoStateChanged);
			ConveyDevices();

			playlists = controller.GetPreferredPlaylists();
			playlistBox.ItemsSource = playlists.Values;
			selector.DataContext = disks;
		}


		/// <summary>
		/// Invoked when the user clicks the Cancel button or presses Esc and provides
		/// and opportunity to stay and continue the synchronization.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoClosing (object sender, CancelEventArgs e)
		{
			if ((percentageCompleted > 0) && (percentageCompleted < 100))
			{
				MessageBoxResult result = MessageWindow.Show(
					null, Resx.SyncCancelText, Resx.SyncCancelCaption,
					MessageBoxButton.YesNo, MessageWindowImage.Warning, MessageBoxResult.No);

				if (result != MessageBoxResult.Yes)
				{
					e.Cancel = true;
					return;
				}

				if (percentageCompleted < 100)
				{
					controller.Librarian.Cancel();
				}
			}
		}

	
		/// <summary>
		/// Release manages resources.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoClosed (object sender, EventArgs e)
		{
			if (percentageCompleted > 0)
			{
				controller.Librarian.ProgressChanged -= new ProgressChangedEventHandler(DoProgressChanged);
			}

			// must detach collection from WPF control otherwise will not dispose
			selector.DataContext = null;
			disks.Clear();
			disks = null;

			playlistBox.ItemsSource = null;

			playlists.Dispose();
			playlists = null;

			manager.Dispose();
			manager = null;

			controller = null;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// When a USB device comes online or goes offline, this handler manages the
		/// disk collection accordingly.
		/// </summary>
		/// <param name="e"></param>

		private void DoStateChanged (UsbStateChangedEventArgs e)
		{
			if (e.State == UsbStateChange.Added)
			{
				if (!disks.Contains(e.Disk.Name))
				{
					disks.Add(e.Disk);
				}
			}
			else if (e.State == UsbStateChange.Removed)
			{
				if (disks.Contains(e.Disk.Name))
				{
					disks.Remove(e.Disk.Name);
				}
			}

			ConveyDevices();
		}


		/// <summary>
		/// Change the UI based on available USB devices.
		/// </summary>

		private void ConveyDevices ()
		{
			if (!this.Dispatcher.CheckAccess())
			{
				this.Dispatcher.BeginInvoke((Action)delegate { ConveyDevices(); });
				return;
			}

			if (disks.Count == 0)
			{
				noDeviceNotice.Visibility = Visibility.Visible;
				selector.Visibility = Visibility.Hidden;
				syncButton.Visibility = Visibility.Hidden;
				playlistBox.IsEnabled = false;
			}
			else
			{
				noDeviceNotice.Visibility = Visibility.Hidden;
				selector.Visibility = Visibility.Visible;
				syncButton.Visibility = Visibility.Visible;
				playlistBox.IsEnabled = true;
			}
		}


		private void DoSelectionChanged (object sender, RoutedEventArgs e)
		{
			// yeah, it's a hack but it works for now
			if (disks == null)
			{
				return;
			}

			int count =
				(int)playlists.FindAll(p => p.IsSelected == true).Sum(p => p.Tracks.Count);

			if (count == 0)
			{
				countBlock.Text = String.Empty;
				countBox.Visibility = Visibility.Hidden;
			}
			else if (count == 1)
			{
				countBlock.Text = String.Format(Resx.TrackCountSingular, count);
				countBox.Visibility = Visibility.Visible;
			}
			else
			{
				countBlock.Text = String.Format(Resx.TrackCountPlural, count);
				countBox.Visibility = Visibility.Visible;
			}

			syncButton.IsEnabled = (count > 0);
		}


		//----------------------------------------------------------------------------------------

		/// <summary>
		/// Invoked when the Sync button is pressed, starts the synchronization process.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoSync (object sender, RoutedEventArgs e)
		{
			string text = String.Format(Resx.SyncWarningText, selector.Location);

			MessageBoxResult result = MessageWindow.Show(
				null, text, Resx.SyncWarningCaption,
				MessageBoxButton.OKCancel, MessageWindowImage.Warning, MessageBoxResult.Cancel);

			if (result != MessageBoxResult.OK)
			{
				e.Handled = true;
				return;
			}

			countBox.Visibility = Visibility.Hidden;
			playlistBox.IsEnabled = false;
			selector.IsEnabled = false;
			syncButton.Visibility = Visibility.Collapsed;
			progressPanel.Visibility = Visibility.Visible;

			string format = PathHelper.GetPathFormat(selector.FormatTag);
			bool withPlaylist = format.Contains("playlist");

			var list = new PersistentIDCollection();

			using (var selectedPlaylists = new PlaylistCollection())
			{
				selectedPlaylists.AddRange(playlists.FindAll(p => p.IsSelected == true));

				foreach (Playlist playlist in selectedPlaylists.Values)
				{
					if (playlist != null)
					{
						PersistentID pid = playlist.PersistentID;

						PersistentIDCollection tracks =
							controller.Librarian.Catalog.FindTracksByPlaylist(pid);

						if (withPlaylist)
						{
							// Cannot modify a value type (PersistentID) from within a foreach loop
							// or the .ForEach extension method, so we need to recreate each pid
							// to set the playlist name.  Unfortunate design consequence...
							foreach (PersistentID track in tracks)
							{
								PersistentID tid = track;
								tid.PlaylistName = playlist.Name;
								list.Add(tid);
							}
						}
						else
						{
							list.AddRange(tracks);
						}
					}
				}
			}

			// show iTunes incase a protection fault occurs; otherwise you cannot see the
			// dialog if iTunes is minimized as a Taskbar toolbar
			controller.ShowiTunes();

			controller.Librarian.ProgressChanged += new ProgressChangedEventHandler(DoProgressChanged);
			controller.Librarian.Export(list, selector.Location, format);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoProgressChanged (object sender, ProgressChangedEventArgs e)
		{
			if (!this.Dispatcher.CheckAccess())
			{
				this.Dispatcher.BeginInvoke((Action)delegate { DoProgressChanged(sender, e); });
				return;
			}

			percentageCompleted = e.ProgressPercentage;

			progressBar.Value = e.ProgressPercentage;
			progressText.Text = e.UserState as String;
		}
	}
}