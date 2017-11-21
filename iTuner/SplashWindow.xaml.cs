//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;


	/// <summary>
	/// Interaction logic for SplashWindow.xaml
	/// </summary>

	internal partial class SplashWindow : Window
	{

		public SplashWindow ()
		{
			this.InitializeComponent();

			titleBlock.Text = App.NameVersion;
		}
	}
}