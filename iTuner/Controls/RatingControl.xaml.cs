//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;

	
	/// <summary>
	/// Interaction logic for RatingControl.xaml
	/// </summary>

	internal partial class RatingControl : UserControl
	{

		private const int MaxRating = 100;


		/// <summary>
		/// Initialize a new instance.
		/// </summary>

		public RatingControl ()
		{
			InitializeComponent();
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// This dependency property is the two-way binding point for the current track.
		/// </summary>

		public static readonly DependencyProperty RatingValueProperty =
			DependencyProperty.Register(
				"RatingValue", typeof(int), typeof(RatingControl),
				new FrameworkPropertyMetadata(
					0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					new PropertyChangedCallback(RatingValueChanged))
				);


		/// <summary>
		/// Gets or sets the rating value of the current track.
		/// </summary>

		public int RatingValue
		{
			get
			{
				return (int)GetValue(RatingValueProperty);
			}

			set
			{
				int rating = (value < 0 ? 0 : (value > MaxRating ? MaxRating : value));
				SetValue(RatingValueProperty, rating);
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Whenever the RatingValue DP is change, this method projects that new value
		/// by toggling the appropriate rating buttons.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private static void RatingValueChanged (
			DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			RatingControl control = sender as RatingControl;
			if (control != null)
			{
				int rating = (int)e.NewValue;
				control.button1.IsChecked = (rating > 0);
				control.button2.IsChecked = (rating > 20);
				control.button3.IsChecked = (rating > 40);
				control.button4.IsChecked = (rating > 60);
				control.button5.IsChecked = (rating > 80);
			}
		}


		/// <summary>
		/// When the user clicks on one of the five rating buttons, this method
		/// extracts the Tag value and uses it to set th RatingValue dependency property.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void RatingButtonClickEventHandler (Object sender, RoutedEventArgs e)
		{
			ToggleButton button = sender as ToggleButton;
			int newRating = int.Parse((String)button.Tag);

			if ((bool)button.IsChecked || newRating < RatingValue)
			{
				RatingValue = newRating;
			}
			else
			{
				RatingValue = newRating - 1;
			}

			e.Handled = true;
		}
	}
}
