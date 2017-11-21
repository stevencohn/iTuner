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


	/// <summary>
	/// Interaction logic for ExceptionDialog.xaml
	/// </summary>

	internal partial class ExceptionDialog : MovableWindow
	{

		/// <summary>
		/// 
		/// </summary>

		public ExceptionDialog ()
		{
			this.InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
			{
				// if in VS or Blend designers do not start loading window or iTunes
				return;
			}

			InitializeDragHandler(detailPanel);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="exc"></param>
		/// <param name="scope"></param>

		public ExceptionDialog (SmartException exc, string scope)
			: this()
		{
			titleBlock.Text = String.Format(
				"{0} {1} Exception", Properties.Resources.I_ApplicationTitle, scope);

			detailBlock.Text = exc.XmlMessage;
		}


		/// <summary>
		/// The Copy button copies the text of the exception to the clipboard.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoCopy (object sender, RoutedEventArgs e)
		{
			Clipboard.SetData(DataFormats.Text, detailBlock.Text);
		}


		/// <summary>
		/// The OK button provides a way for the user to quickly dismiss the About box 
		/// without having to wait for it to fade.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoOK (object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}