//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using WinForms = System.Windows.Forms;
	using iTuner.Controls;
	using iTuner.iTunes;
	using Resx = Properties.Resources;
	using Settings = Properties.Settings;


	/// <summary>
	/// Interaction logic for ExportDialog.xaml
	/// </summary>

	internal partial class ExportDialog : MovableWindow
	{
		private const string SettingsFile = "Settings.xml";

		private Controller controller;
		private PersistentIDCollection trackPIDs;
		private EncoderCollection encoders;
		private int percentageCompleted;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>

		public ExportDialog ()
		{
			this.InitializeComponent();
			this.Closed += new EventHandler(DoClosed);
			this.Closing += new CancelEventHandler(DoClosing);

			InitializeDragHandler(detailPanel);

			if (DesignerProperties.GetIsInDesignMode(this))
			{
				// if in VS or Blend designers do not start loading window or iTunes
				return;
			}

			if (!String.IsNullOrEmpty(Settings.Default.ExportLocation))
			{
				locationBox.Text = Settings.Default.ExportLocation;
			}

			treeCheck.IsChecked = Settings.Default.ExportSubdirectories;

			if (!String.IsNullOrEmpty(Settings.Default.ExportPlaylistFormat))
			{
				foreach (ComboBoxItem item in playlistBox.Items)
				{
					if (item.Tag.Equals(Settings.Default.ExportPlaylistFormat))
					{
						playlistBox.SelectedItem = item;
						break;
					}
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="controller"></param>
		/// <param name="trackPIDs"></param>

		public ExportDialog (Controller controller, PersistentIDCollection trackPIDs)
			: this()
		{
			this.controller = controller;
			this.trackPIDs = trackPIDs;

			this.percentageCompleted = 0;

			this.Encoders = controller.Encoders;

			if (!String.IsNullOrEmpty(trackPIDs.Album))
			{
				detailBlock.Text = String.Format(
					Resx.ExportingAlbum, trackPIDs.Count, trackPIDs.Album, trackPIDs.Artist);
			}
			else if (!String.IsNullOrEmpty(trackPIDs.Artist))
			{
				detailBlock.Text = String.Format(
					Resx.ExportingArtist, trackPIDs.Count, trackPIDs.Artist);
			}
			else
			{
				detailBlock.Text = String.Format(
					Resx.ExportingPlaylist, trackPIDs.Count, trackPIDs.Name);
			}
		}


		/// <summary>
		/// Allows the user to choose to cancel the export or stay on 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoClosing (object sender, CancelEventArgs e)
		{
			if ((percentageCompleted > 0) && (percentageCompleted < 100))
			{
				MessageBoxResult result = MessageWindow.Show(
					null, Resx.ExportCancelText, Resx.ExportCancelCaption,
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
		/// Release managed resources
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoClosed (object sender, EventArgs e)
		{
			if (percentageCompleted > 0)
			{
				controller.Librarian.ProgressChanged -= new ProgressChangedEventHandler(DoProgressChanged);
			}

			this.Closed -= new EventHandler(DoClosed);
			this.Closing -= new CancelEventHandler(DoClosing);

			if (percentageCompleted == 100)
			{
				string location = PathHelper.CleanDirectoryPath(locationBox.Text.Trim());

				if (!String.IsNullOrEmpty(location))
				{
					Settings.Default.ExportPlaylistFormat = ((ComboBoxItem)playlistBox.SelectedItem).Tag as string;
					Settings.Default.ExportSubdirectories = (bool)treeCheck.IsChecked;
					Settings.Default.ExportLocation = location;
					Settings.Default.Save();
				}
			}

			controller = null;
			trackPIDs = null;

			// clear the encoderBox UI control
			encoderBox.SelectedItem = null;

			foreach (ComboBoxItem item in encoderBox.Items)
			{
				item.Content = null;
			}

			encoderBox.Items.Clear();

			// dispose the encoders collection
			encoders.Dispose();
			encoders = null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>

		public EncoderCollection Encoders
		{
			set
			{
				// need to keep global ref to collection until dialog closed
				// else we'll destroy the encoders bound to the encoderBox
				encoders = value;

				encoderBox.Items.Clear();
				object mp3Encoder = null;
				object preferredEncoder = null;

				foreach (Encoder encoder in encoders)
				{
					ComboBoxItem item = new ComboBoxItem();
					item.Content = encoder;

					if (encoder.IsEmpty)
					{
						item.FontStyle = FontStyles.Italic;
					}

					encoderBox.Items.Add(item);

					if (!encoder.IsEmpty)
					{
						if (encoder.Format.Equals(Settings.Default.ExportEncoder))
						{
							preferredEncoder = item;
						}
						else if ("MP3".Equals(encoder.Format))
						{
							mp3Encoder = item;
						}
					}
				}

				if (preferredEncoder != null)
				{
					encoderBox.SelectedItem = preferredEncoder;
				}
				else if (mp3Encoder != null)
				{
					encoderBox.SelectedItem = mp3Encoder;
				}
				else
				{
					encoderBox.SelectedIndex = 0;
				}

				mp3Encoder = null;
				preferredEncoder = null;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Invoked when the user clicks the ListOnly checkbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoListOnlyChanged (object sender, RoutedEventArgs e)
		{
			bool enabled = (listOnlyBox.IsChecked != true);

			((ComboBoxItem)playlistBox.Items[0]).IsEnabled = enabled;
			encoderBox.IsEnabled = enabled;
			treeCheck.IsEnabled = enabled;
		}

	
		/// <summary>
		/// Invoked when the user clicks the "browse folders" button (..)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoSelectFolder (object sender, RoutedEventArgs e)
		{
			using (var dialog = new WinForms.FolderBrowserDialog())
			{
				dialog.Description = Resx.ExportFolderDialogDescription;
				dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				dialog.ShowNewFolderButton = true;

				WinForms.DialogResult result = dialog.ShowDialog();

				if (result == WinForms.DialogResult.OK)
				{
					locationBox.Text = dialog.SelectedPath;
				}
			}
		}


		/// <summary>
		/// Invoked when the user clicks the Export button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoExport (object sender, RoutedEventArgs e)
		{
			bool listOnly = (listOnlyBox.IsChecked == true);

			if (!listOnly && (trackPIDs.Count > 30))
			{
				TimeSpan span = TimeSpan.FromMinutes(((double)trackPIDs.Count * 20.0) / 60.0);

				string text = String.Format(
					Resx.ExportWarningDialogText, trackPIDs.Count, span.ToString());

				MessageBoxResult result = MessageWindow.Show(
					text, Resx.ExportWarningDialogCaption,
					MessageBoxButton.YesNo, MessageWindowImage.Warning);

				if (result != MessageBoxResult.Yes)
				{
					return;
				}
			}

			progressPanel.Visibility = Visibility.Visible;
			exportButton.IsEnabled = false;

			Encoder encoder = null;

			if (!listOnly)
			{
				encoder = ((ComboBoxItem)encoderBox.SelectedItem).Content as Encoder;

				if (!encoder.IsEmpty)
				{
					Settings.Default.ExportEncoder = encoder.Format;
				}
			}

			string format = ((ComboBoxItem)playlistBox.SelectedItem).Tag as string;
			string location = PathHelper.CleanDirectoryPath(locationBox.Text.Trim());
			bool createSubdirectories = (treeCheck.IsChecked == true);

			// show iTunes incase a protection fault occurs; otherwise you cannot see the
			// dialog if iTunes is minimized as a Taskbar toolbar
			controller.ShowiTunes();

			controller.Librarian.ProgressChanged += new ProgressChangedEventHandler(DoProgressChanged);
			controller.Librarian.Export(trackPIDs, encoder, format, location, createSubdirectories);

			// [foo] dispose this reference but do not release underlying instance passed to Librarian
			//if (encoder != null) { encoder.Dispose(false); }
		}


		/// <summary>
		/// Callback to update the progress bar during export.
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

			if (percentageCompleted == 100)
			{
				exportButton.IsEnabled = true;
				progressText.Text = Resx.Completed;
				controller.Librarian.ProgressChanged -= new ProgressChangedEventHandler(DoProgressChanged);
			}
		}
	}
}