//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Collections.Specialized;
	using System.IO;
	using System.Windows;
	using System.Windows.Controls;
	using WinForms = System.Windows.Forms;


	/// <summary>
	/// Interaction logic for DeviceSelector.xaml
	/// </summary>

	internal partial class DeviceSelector : UserControl
	{

		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance with default behaviors.
		/// </summary>

		public DeviceSelector ()
		{
			this.InitializeComponent();
			this.DataContextChanged += new DependencyPropertyChangedEventHandler(DoDataContextChanged);
		}


		/// <summary>
		/// Capture and process new data context changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		void DoDataContextChanged (object sender, DependencyPropertyChangedEventArgs e)
		{
			UsbDiskCollection disks = e.NewValue as UsbDiskCollection;
			devicesBox.ItemsSource = disks;

			if (disks != null)
			{
				disks.CollectionChanged += new NotifyCollectionChangedEventHandler(DoCollectionChanged);
				if (disks.Count > 0)
				{
					devicesBox.SelectedIndex = 0;
				}
			}
		}


		/// <summary>
		/// Whenever a device is added or removed, detect this change and ensure that
		/// one device, if available, is always selected.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				if (devicesBox.SelectedItem == null)
				{
					devicesBox.SelectedIndex = 0;
				}
			}
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Gets the selected USB device
		/// </summary>

		public UsbDisk Disk
		{
			get { return devicesBox.SelectedItem as UsbDisk; }
		}


		/// <summary>
		/// Gets a Boolean value indicating whether folder capabilities should be auto-detected
		/// by the ExportScanner.
		/// </summary>

		public bool IsAutoDetected
		{
			get { return autoDetectBox.IsChecked == true; }
		}


		/// <summary>
		/// Gets the root path of the target directory into which we will export media files.
		/// </summary>

		public string Location
		{
			get { return locationBox.Text; }
		}


		/// <summary>
		/// Gets the internal resource ID (tag) of the path format string.
		/// </summary>

		public string FormatTag
		{
			get { return ((ComboBoxItem)formatBox.SelectedItem).Tag as string; }
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// When the auto-detect checkbox is toggled, we need to reset the related input
		/// controls to reflect the most appropriate state.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoAutoDetectChanged (object sender, RoutedEventArgs e)
		{
			bool enabled = (autoDetectBox.IsChecked != true);

			formatBox.IsEnabled = enabled;
			locationBox.IsEnabled = enabled;
			selectFolderButton.IsEnabled = enabled;

			if (!enabled)
			{
				UsbDisk disk = devicesBox.SelectedItem as UsbDisk;
				if (disk != null)
				{
					locationBox.Text = disk.Name + Path.DirectorySeparatorChar.ToString();
				}
			}
		}


		/// <summary>
		/// When a new USB device is selected, we need to update the location information.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoDeviceSelectionChanged (object sender, SelectionChangedEventArgs e)
		{
			UsbDisk disk = devicesBox.SelectedItem as UsbDisk;
			if (disk != null)
			{
				locationBox.Text = disk.Name + Path.DirectorySeparatorChar.ToString();
			}
			else
			{
				locationBox.Text = Path.DirectorySeparatorChar.ToString();
			}
		}


		/// <summary>
		/// Allows the user to browse/create a target folder on the USB device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
	
		private void DoSelectFolder (object sender, RoutedEventArgs e)
		{
			UsbDisk disk = devicesBox.SelectedItem as UsbDisk;
			if (disk != null)
			{
				using (var dialog = new WinForms.FolderBrowserDialog())
				{
					dialog.Description = "Select the root synchronization folder";
					dialog.RootFolder = Environment.SpecialFolder.MyComputer;
					dialog.SelectedPath = locationBox.Text;
					dialog.ShowNewFolderButton = true;

					WinForms.DialogResult result = dialog.ShowDialog();

					if (result == WinForms.DialogResult.OK)
					{
						locationBox.Text = dialog.SelectedPath;
					}
				}
			}
		}
	}
}