//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Windows;
	using System.ComponentModel;


	/// <summary>
	/// Interaction logic for UpgradeWindow.xaml
	/// </summary>

	internal partial class UpgradeWindow : Window
	{

		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Default constructor required by VS/Blend designers
		/// </summary>

		public UpgradeWindow ()
		{
			this.InitializeComponent();
		}


		/// <summary>
		/// Initialize a new instance with the specified upgrade release details.
		/// </summary>
		/// <param name="release"></param>

		public UpgradeWindow (UpgradeRelease release)
			: this()
		{
			this.DataContext = release;

			// generate a local temporary
			string tmp = Path.GetTempFileName();
			string path = Path.Combine(
				Path.GetDirectoryName(tmp),
				Path.GetFileNameWithoutExtension(tmp) + ".htm");

			// rename the file from .tmp to .htm
			File.Move(tmp, path);

			// populate the file with our custom HTML
			File.WriteAllText(path,
				String.Format(Properties.Resources.I_ReleaseHtmFormat, release.Description));

			// navigate to the temporary HTML file
			this.notesBox.Navigate(new Uri(path, UriKind.RelativeOrAbsolute));
		}

	
		//========================================================================================
		// Mehtods
		//========================================================================================

		/// <summary>
		/// Open a Web browser and navigate to the release download page.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoUpgrade (object sender, RoutedEventArgs e)
		{
			UpgradeRelease release = this.DataContext as UpgradeRelease;

			ProcessStartInfo info = new ProcessStartInfo(release.Uri);
			info.WindowStyle = ProcessWindowStyle.Normal;

			try
			{
				Process.Start(info);
			}
			catch (Exception) { }

			//Application.Current.Shutdown();

			this.Close();
		}


		/// <summary>
		/// Cancel.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoCancel (object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
