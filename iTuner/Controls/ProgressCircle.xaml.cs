//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.Controls
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Threading;
	using System.Windows.Input;
	using System.Windows.Shapes;


	/// <summary>
	/// A circular type progress bar, that is simliar to popular web based
	/// progress bars
	/// </summary>

	internal partial class ProgressCircle
	{

		private readonly DispatcherTimer timer;


		public ProgressCircle ()
		{
			InitializeComponent();

			timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
			timer.Tick += DoProgress;
			timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
		}


		private void DoLoaded (object sender, RoutedEventArgs e)
		{
			const double offset = Math.PI;
			const double step = Math.PI * 2 / 10.0;

			SetPosition(C0, offset, 0.0, step);
			SetPosition(C1, offset, 1.0, step);
			SetPosition(C2, offset, 2.0, step);
			SetPosition(C3, offset, 3.0, step);
			SetPosition(C4, offset, 4.0, step);
			SetPosition(C5, offset, 5.0, step);
			SetPosition(C6, offset, 6.0, step);
			SetPosition(C7, offset, 7.0, step);
			SetPosition(C8, offset, 8.0, step);
		}


		private void SetPosition (
			Ellipse ellipse, double offset, double posOffSet, double step)
		{
			ellipse.SetValue(
				Canvas.LeftProperty, 50.0 + Math.Sin(offset + posOffSet * step) * 50.0);

			ellipse.SetValue(
				Canvas.TopProperty, 50 + Math.Cos(offset + posOffSet * step) * 50.0);
		}


		private void Start ()
		{
			this.Cursor = Cursors.AppStarting;

			if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
			{
				timer.Start();
			}
		}


		private void Stop ()
		{
			timer.Stop();
			this.Cursor = Cursors.Arrow;
		}


		private void DoProgress (object sender, EventArgs e)
		{
			SpinnerRotate.Angle = (SpinnerRotate.Angle + 36) % 360;
		}


		private void DoUnloaded (object sender, RoutedEventArgs e)
		{
			Stop();
		}


		private void DoVisibleChanged (object sender, DependencyPropertyChangedEventArgs e)
		{
			bool isVisible = (bool)e.NewValue;

			if (isVisible)
			{
				Start();
			}
			else
			{
				Stop();
			}
		}
	}
}