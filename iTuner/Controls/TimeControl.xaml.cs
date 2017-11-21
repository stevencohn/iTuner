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
	/// Interaction logic for TimeControl.xaml
	/// </summary>

	internal partial class TimeControl : UserControl
	{
		private int duration;

		public static readonly DependencyProperty DurationProperty =
			DependencyProperty.Register(
			"Duration", typeof(int), typeof(TimeControl), new PropertyMetadata(0));

		public static readonly DependencyProperty PlayerPositionProperty =
			DependencyProperty.Register(
			"PlayerPosition", typeof(int), typeof(TimeControl), new PropertyMetadata(0));

	
		/// <summary>
		/// 
		/// </summary>

		public TimeControl ()
		{
			this.InitializeComponent();

			slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider_ValueChanged);
		}


	
		private void slider_ValueChanged (object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			TimeSpan span = new TimeSpan(0, 0, 0, (int)slider.Value, 0);
			string time = span.ToString().Substring(3);
			if (time[0] == '0')
				time = time.Substring(1);

			minTime.Text = time;

			span = new TimeSpan(0, 0, 0, (int)(slider.Maximum - slider.Value), 0);
			time = span.ToString().Substring(3);
			if (time[0] == '0')
				time = time.Substring(1);

			maxTime.Text = "-" + time;
		}


		/// <summary>
		/// Gets or sets the duration in seconds of the current track.
		/// </summary>

		public int Duration
		{
			get
			{
				return (int)GetValue(DurationProperty);
			}

			set
			{
				SetValue(DurationProperty, value);
				duration = value;

				TimeSpan span = new TimeSpan(0, 0, 0, (int)slider.Value, 0);
				minTime.Text = span.ToString();

				span = new TimeSpan(0, 0, 0, duration - (int)slider.Value, 0);
				maxTime.Text = span.ToString();
			}
		}


		/// <summary>
		/// Gets or sets the position in seconds of the currently playing track.
		/// </summary>

		public int PlayerPosition
		{
			get
			{
				return (int)slider.Value;
			}

			set
			{
				slider.Value = (double)value;
				SetValue(PlayerPositionProperty, value);
			}
		}
	}
}