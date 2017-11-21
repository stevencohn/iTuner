//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Threading;


	/// <summary>
	/// Interaction logic for TrackerWindow.xaml
	/// </summary>

	internal partial class TrackerWindow : FadingWindow
	{

		private bool isDisposed;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Default constructor required for VS and Blend designers.
		/// </summary>

		public TrackerWindow ()
			: base()
		{
			this.InitializeComponent();

			this.mainBorder.Opacity = 0.0;
			this.AnimatedElement = mainBorder;
			this.Visibility = Visibility.Hidden;

			EventManager.RegisterClassHandler(
				typeof(EditBlock), EditBlock.BeginEditEvent,
				new RoutedEventHandler(DoBeginEdit));

			EventManager.RegisterClassHandler(
				typeof(EditBlock), EditBlock.CompleteEditEvent,
				new RoutedEventHandler(DoCompleteEdit));
		}


		/// <summary>
		/// Initializes a new instances with the given iTunes controller.
		/// </summary>
		/// <param name="controller">The controller to monitor.</param>

		public TrackerWindow (iTunes.Controller controller)
			: this()
		{
			this.DataContext = controller;
		}


		/// <summary>
		/// Dereference unmanaged resources - the controller.
		/// </summary>

		public override void Dispose ()
		{
			if (!isDisposed)
			{
				base.Dispose();

				DataContext = null;

				isDisposed = true;

				GC.SuppressFinalize(this);
			}
		}



		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Handler for the EditBlock BeginEdit event, pins the current window so it
		/// cannot fade out while editing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoBeginEdit (object sender, RoutedEventArgs e)
		{
			IsPinned = true;
		}


		/// <summary>
		/// Handler for the EditBlock CompleteEdit event, unpins the current window so
		/// it can fade out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoCompleteEdit (object sender, RoutedEventArgs e)
		{
			IsPinned = false;
		}
	
		
		/// <summary>
		/// Close the window when the Esc key is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoKeyDown (object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Hide();
			}
		}


		/// <summary>
		/// Ensures that the play panel is displayed the next time the window is shown.
		/// </summary>

		protected override void OnHideCompleted ()
		{
			trackPanel.ResetView();
		}
	}
}