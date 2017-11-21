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
	/// Interaction logic for ImportDialog.xaml
	/// </summary>

	internal partial class ImportDialog : MovableWindow
	{
		private Controller controller;
		private int percentageCompleted;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>

		public ImportDialog ()
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
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="controller"></param>
		/// <param name="trackPIDs"></param>

		public ImportDialog (Controller controller)
			: this()
		{
			this.controller = controller;
			this.percentageCompleted = 0;
		}


		/// <summary>
		/// Allows the user to choose to cancel the import or stay on 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoClosing (object sender, CancelEventArgs e)
		{
			if ((percentageCompleted > 0) && (percentageCompleted < 100))
			{
				MessageBoxResult result = MessageWindow.Show(
					null, Resx.ImportCancelText, Resx.ImportCancelCaption,
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

			controller = null;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Invoked when the user clicks the "browse folders" button (..)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoSelectFolder (object sender, RoutedEventArgs e)
		{
			using (var dialog = new WinForms.OpenFileDialog())
			{
				dialog.CheckFileExists = true;
				dialog.Filter = "Winamp (*.m3u)|*.m3u|RealPlayer (*.pls)|*.pls|Windows Media Player (*.wpl)|*.wpl|Zune (*.zpl)|*.zpl";
				dialog.Multiselect = false;
				dialog.ShowReadOnly = false;
				dialog.Title = Resx.ImportPlaylistDialogTitle;

				string path = locationBox.Text.Trim();
				if (path == String.Empty)
				{
					dialog.InitialDirectory = Environment.GetFolderPath(
						Environment.SpecialFolder.DesktopDirectory);
				}
				else
				{
					dialog.InitialDirectory = path;
				}

				WinForms.DialogResult result = dialog.ShowDialog();

				if (result == WinForms.DialogResult.OK)
				{
					locationBox.Text = dialog.FileName;
				}
			}
		}


		/// <summary>
		/// Invoked when the user clicks the Import button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoImport (object sender, RoutedEventArgs e)
		{
			progressPanel.Visibility = Visibility.Visible;
			importButton.IsEnabled = false;

			string location = PathHelper.CleanDirectoryPath(locationBox.Text.Trim());

			// show iTunes incase a protection fault occurs; otherwise you cannot see the
			// dialog if iTunes is minimized as a Taskbar toolbar
			controller.ShowiTunes();

			controller.Librarian.ProgressChanged += new ProgressChangedEventHandler(DoProgressChanged);
			controller.Librarian.Import(location);
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
				importButton.IsEnabled = true;
				progressText.Text = Resx.Completed;
				controller.Librarian.ProgressChanged -= new ProgressChangedEventHandler(DoProgressChanged);
			}
		}
	}
}