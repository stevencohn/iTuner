//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Windows.Input;
	using System.Windows.Documents;
	using iTuner.iTunes;


	/// <summary>
	/// Interaction logic for TaskPanel.xaml
	/// </summary>

	internal partial class TaskPanel : UserControl
	{

		public static readonly DependencyProperty IsShuffledProperty;

		private Controller controller;
		private ObservableCollection<IScanner> tasks;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Static constructor, initialize dependency properties.
		/// </summary>

		static TaskPanel ()
		{
			IsShuffledProperty = DependencyProperty.Register("IsShuffled",
				typeof(bool), typeof(TaskPanel), new PropertyMetadata(true));
		}


		/// <summary>
		/// Initialize a new instance.
		/// </summary>

		public TaskPanel ()
		{
			InitializeComponent();

			this.tasks = new ObservableCollection<IScanner>();
			this.taskList.ItemsSource = this.tasks;

			this.DataContextChanged +=
				new DependencyPropertyChangedEventHandler(DoDataContextChanged);
		}


		/// <summary>
		/// Grab a reference to the Controller.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoDataContextChanged (object sender, DependencyPropertyChangedEventArgs e)
		{
			controller = e.NewValue as Controller;

			controller.Librarian.CollectionChanged +=
				new NotifyCollectionChangedEventHandler(DoTaskCollectionChanged);
		}


		//========================================================================================
		// Events/Properties
		//========================================================================================

		public static readonly RoutedEvent
			EditKeysEvent = EventManager.RegisterRoutedEvent("EditKeys",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskPanel));


		public static readonly RoutedEvent
			ShowLyricsEvent = EventManager.RegisterRoutedEvent("ShowLyrics",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskPanel));


		public event RoutedEventHandler EditKeys
		{
			add { AddHandler(EditKeysEvent, value); }
			remove { RemoveHandler(EditKeysEvent, value); }
		}


		public event RoutedEventHandler ShowLyrics
		{
			add { AddHandler(ShowLyricsEvent, value); }
			remove { RemoveHandler(ShowLyricsEvent, value); }
		}


		/// <summary>
		/// Gets or sets the shuffle state of the player.
		/// </summary>
		/// <remarks>
		/// This directly wraps the IsShuffledProperty dependency property.
		/// </remarks>

		public bool IsShuffled
		{
			get { return (bool)GetValue(IsShuffledProperty); }
			set { SetValue(IsShuffledProperty, value); }
		}


		/// <summary>
		/// 
		/// </summary>

		public string LyricsState
		{
			set
			{
				LyricsButton.Tag = value;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Resets the state of the EditKeys button back to unchecked.
		/// </summary>

		public void ResetEditKeys ()
		{
			EditKeyButton.IsChecked = false;
		}


		/// <summary>
		/// Ensures that the play panel is currently displayed.
		/// </summary>

		public void ResetView ()
		{
			flipper.IsChecked = false;
			actionPane.Visibility = Visibility.Visible;
			taskPane.Visibility = Visibility.Hidden;
		}


		/// <summary>
		/// Toggle the view between the action panel and the task view panel.
		/// </summary>
		/// <param name="sender">Useless</param>
		/// <param name="e">Useless</param>

		private void DoToggleView (object sender, RoutedEventArgs e)
		{
			if (flipper.IsChecked == true)
			{
				actionPane.Visibility = Visibility.Hidden;
				taskPane.Visibility = Visibility.Visible;
			}
			else
			{
				actionPane.Visibility = Visibility.Visible;
				taskPane.Visibility = Visibility.Hidden;
			}
		}


		private void DoEnterLink (object sender, MouseEventArgs e)
		{
			Hyperlink link = sender as Hyperlink;
			if (link != null)
			{
				link.Foreground = Brushes.Blue;
			}
		}

		private void DoLeaveLink (object sender, MouseEventArgs e)
		{
			Hyperlink link = sender as Hyperlink;
			if (link != null)
			{
				link.Foreground = Brushes.Black;
			}
		}

		private void DoChangeOptions (object sender, RoutedEventArgs e)
		{
			OptionsDialog dialog = new OptionsDialog();
			dialog.ShowDialog();
		}


		/// <summary>
		/// Raise the EditKeys event to inform the main app window to show the
		/// hot key editor.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoEditKeys (object sender, RoutedEventArgs e)
		{
			EditKeyButton.IsChecked = true;
			RoutedEventArgs args = new RoutedEventArgs(EditKeysEvent);
			RaiseEvent(args);
		}


		/// <summary>
		/// Shows the iTunes main window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoShowiTunes (object sender, RoutedEventArgs e)
		{
			controller.ShowiTunes();
		}


		/// <summary>
		/// Raise the ShowLyrics event to inform the main app window to show the
		/// lyrics for the current track.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoShowLyrics (object sender, RoutedEventArgs e)
		{
			RoutedEventArgs args = new RoutedEventArgs(ShowLyricsEvent);
			RaiseEvent(args);
		}


		/// <summary>
		/// Resize taskList columns.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoTaskCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!this.Dispatcher.CheckAccess())
			{
				this.Dispatcher.BeginInvoke(
					(Action)delegate { DoTaskCollectionChanged(sender, e); });
				return;
			}

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (object item in e.NewItems)
					{
						tasks.Add(item as IScanner);
					}
					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (object item in e.OldItems)
					{
						tasks.Remove(item as IScanner);
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					tasks.Clear();
					break;
			}

			emptyBlock.Visibility = (tasks.Count == 0 ? Visibility.Visible : Visibility.Hidden);
		}
	}
}
