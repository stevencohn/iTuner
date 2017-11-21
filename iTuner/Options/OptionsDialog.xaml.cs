//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using iTuner.Controls;
	using Settings = Properties.Settings;


	/// <summary>
	/// Interaction logic for OptionsDialog.xaml
	/// </summary>

	internal partial class OptionsDialog : MovableWindow
	{

		public OptionsDialog()
		{
			this.InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
			{
				// if in VS or Blend designers do not start loading window or iTunes
				return;
			}

			InitializeDragHandler(detailPanel);
		}


		private void DoOK (object sender, RoutedEventArgs e)
		{
			Settings.Default.Save();
			this.Close();
		}


		private void DoCancel (object sender, RoutedEventArgs e)
		{
			Settings.Default.Reload();
		}
	}
}