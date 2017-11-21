//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Controls;


	/// <summary>
	/// Interaction logic for ControlPanel.xaml
	/// </summary>

	internal partial class ControlPanel : UserControl
	{
		public ControlPanel()
		{
			this.InitializeComponent();
		}


		public event EventHandler PreviousTrack;
		public event EventHandler NextTrack;
		public event EventHandler PlayPause;


		public bool IsMuted
		{
			set { volumeControl.IsMuted = value; }
		}


		public int Volume
		{
			get { return volumeControl.Volume; }
			set { volumeControl.Volume = value; }
		}


		private void DoNextTrack (object sender, RoutedEventArgs e)
		{
			if (NextTrack != null)
			{
				NextTrack(sender, e);
			}
		}


		private void DoPreviousTrack (object sender, RoutedEventArgs e)
		{
			if (PreviousTrack != null)
			{
				PreviousTrack(sender, e);
			}
		}


		private void DoPlayPause (object sender, RoutedEventArgs e)
		{
			if (PlayPause != null)
			{
				PlayPause(sender, e);
			}
		}
	}
}