//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;


	/// <summary>
	/// Interaction logic for VolumeControl.xaml
	/// </summary>

	internal partial class VolumeControl : UserControl
	{

		/// <summary>
		/// Instantiates a new volume control.
		/// </summary>

		public VolumeControl ()
		{
			this.InitializeComponent();
		}



		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// DP
		/// </summary>

		public static readonly DependencyProperty IsMutedProperty =
			DependencyProperty.Register(
			"IsMuted", typeof(bool), typeof(VolumeControl), new PropertyMetadata(false));


		/// <summary>
		/// Gets or set the muted state of the control.
		/// </summary>

		public bool IsMuted
		{
			get
			{
				return (bool)GetValue(IsMutedProperty);
			}

			set
			{
				SetValue(IsMutedProperty, value);

				if (value == true)
				{
					minButton.Tag = maxButton.Tag = "IsMuted";
				}
				else
				{
					minButton.Tag = maxButton.Tag = String.Empty;
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>

		public static readonly DependencyProperty VolumeProperty =
			DependencyProperty.Register(
			"Volume", typeof(int), typeof(VolumeControl), new PropertyMetadata(0));


		/// <summary>
		/// Gets or sets the current volume level.
		/// </summary>

		public int Volume
		{
			get
			{
				return (int)GetValue(VolumeProperty);
			}

			set
			{
				SetValue(VolumeProperty, value);
				slider.Value = (double)value;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void MaximizeVolume (object sender, RoutedEventArgs e)
		{
			Volume = (int)slider.Maximum;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void MinimizeVolume (object sender, RoutedEventArgs e)
		{
			Volume = (int)slider.Minimum;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void SetToolTip (object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			slider.ToolTip = String.Format(
				Properties.Resources.VolumeSettingFormat, (int)slider.Value);
		}
	}
}