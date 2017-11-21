//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.IO;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using iTuner.iTunes;


	/// <summary>
	/// Interaction logic for TrackPanel.xaml
	/// </summary>

	internal partial class TrackPanel : UserControl
	{

		/// <summary>
		/// Initialize a new instance of this panel control
		/// </summary>

		public TrackPanel ()
		{
			this.InitializeComponent();
		}


		/// <summary>
		/// Ensures that the play panel is currently displayed.
		/// </summary>

		public void ResetView ()
		{
			flipper.IsChecked = false;
			detailPanel.Visibility = Visibility.Hidden;
			playPanel.Visibility = Visibility.Visible;
		}


		/// <summary>
		/// Toggle the view between the current track panel and the track info detail panel.
		/// </summary>
		/// <param name="sender">Useless</param>
		/// <param name="e">Useless</param>

		private void ToggleView (object sender, RoutedEventArgs e)
		{
			if (flipper.IsChecked == true)
			{
				playPanel.Visibility = Visibility.Hidden;
				sourceImage.Visibility = Visibility.Hidden;
				detailPanel.Visibility = Visibility.Visible;
			}
			else
			{
				detailPanel.Visibility = Visibility.Hidden;
				playPanel.Visibility = Visibility.Visible;
				sourceImage.Visibility = Visibility.Visible;
			}
		}


		private void DoOpenArtistURL(object sender, MouseButtonEventArgs e)
		{
			var controller = this.DataContext as Controller;
			string artistURL = controller.CurrentTrack.ArtistURL;
			if (!String.IsNullOrEmpty(artistURL))
			{
				System.Diagnostics.Process.Start(artistURL);
			}
		}


		private void DoOpenLocation (object sender, MouseButtonEventArgs e)
		{
			string path = Path.GetDirectoryName(locationBlock.Text);
			System.Diagnostics.Process.Start("explorer.exe", "/e," + path);
		}
	}
}