//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media.Imaging;
	using iTuner.Controls;


	internal enum MessageWindowImage
	{
		None,
		Error,
		Information,
		OK,
		Question,
		Stop,
		Warning
	}


	/// <summary>
	/// Interaction logic for MessageWindow.xaml
	/// </summary>

	internal partial class MessageWindow : MovableWindow
	{
		private MessageBoxResult affirmativeResult;


		public MessageWindow ()
		{
			InitializeComponent();
			KeyDown += DoKeyDown;

			InitializeDragHandler(detailPanel);
		}


		public MessageBoxResult Result { get; set; }



		//========================================================================================
		// Show()
		//========================================================================================

		public static MessageBoxResult Show (string text)
		{
			return Show(null, text, null, null,
				MessageBoxButton.OK, MessageWindowImage.None, MessageBoxResult.None);
		}


		public static MessageBoxResult Show (
			string text, string caption,
			MessageBoxButton button, MessageWindowImage icon)
		{
			return Show(null, text, caption, null, button, icon, MessageBoxResult.None);
		}


		public static MessageBoxResult Show (
			Window owner, string text, string caption,
			MessageBoxButton button, MessageWindowImage icon, MessageBoxResult defaultResult)
		{
			return Show(null, text, caption, null, button, icon, defaultResult);
		}


		public static MessageBoxResult Show (
			Window owner, string text, string caption, string log,
			MessageBoxButton button, MessageWindowImage icon, MessageBoxResult defaultResult)
		{
			MessageWindow box = new MessageWindow();

			if (owner == null)
			{
				if (Application.Current.MainWindow.IsVisible)
				{
					box.Owner = Application.Current.MainWindow;
				}
			}
			else
			{
				box.Owner = owner;
			}

			// allow blank caption, String.Empty
			if (caption != null)
			{
				box.titleBlock.Text = caption;
			}

			// text is required
			box.messageBox.Text = text;

			if (log != null)
			{
				box.logBox.Text = log;
				box.logBox.Visibility = Visibility.Visible;
			}

			box.SetButtons(button);
			box.SetIcon(icon);

			box.SetDefaultResult(defaultResult);

			box.ShowDialog();

			return box.Result;
		}


		#region Setup

		private void SetButtons (MessageBoxButton button)
		{
			switch (button)
			{
				case MessageBoxButton.OK:
					yesButton.Content = "OK";
					noButton.Visibility = Visibility.Collapsed;
					cancelButton.Visibility = Visibility.Collapsed;
					affirmativeResult = MessageBoxResult.OK;
					break;

				case MessageBoxButton.OKCancel:
					yesButton.Content = "OK";
					noButton.Visibility = Visibility.Collapsed;
					affirmativeResult = MessageBoxResult.OK;
					break;

				case MessageBoxButton.YesNo:
				case MessageBoxButton.YesNoCancel:
					cancelButton.Visibility = Visibility.Collapsed;
					affirmativeResult = MessageBoxResult.Yes;
					break;

				// YesNoCancel is the default
				//case MessageBoxButton.YesNoCancel:
			}
		}


		private void SetIcon (MessageWindowImage icon)
		{
			string name = (icon == MessageWindowImage.None ? null : icon.ToString());

			if (name == null)
			{
				imageBox.Visibility = Visibility.Collapsed;
			}
			else
			{
				string path = String.Format("Images/MessageBox/{0}.png", name);
				BitmapImage image = new BitmapImage(AssetExtension.GetResourceUri(path));

				imageBox.Source = image;
			}
		}


		private void SetDefaultResult (MessageBoxResult defaultResult)
		{
			switch (defaultResult)
			{
				case MessageBoxResult.Cancel:
					cancelButton.IsDefault = true;
					break;

				case MessageBoxResult.No:
					noButton.IsDefault = true;
					break;

				case MessageBoxResult.OK:
				case MessageBoxResult.Yes:
					yesButton.IsDefault = true;
					break;

				// None is the default
				//case MessageBoxResult.None:
			}
		}

		#endregion Setup


		//========================================================================================
		// Handlers
		//========================================================================================

		private void DoCancel (object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Cancel;
			Close();
		}


		private void DoNo (object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.No;
			Close();
		}


		private void DoYes (object sender, RoutedEventArgs e)
		{
			Result = affirmativeResult;
			Close();
		}


		private void DoKeyDown (object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					Result = MessageBoxResult.Cancel;
					Close();
					break;

				case Key.N:
					if (noButton.Visibility == Visibility.Visible)
					{
						Result = MessageBoxResult.No;
						Close();
					}
					break;

				case Key.Y:
					if (yesButton.Visibility == Visibility.Visible)
					{
						Result = MessageBoxResult.Yes;
						Close();
					}
					break;
			}
		}
	}
}
