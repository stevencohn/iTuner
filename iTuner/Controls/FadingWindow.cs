//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media.Animation;
	using System.Windows.Threading;
	using F = System.Windows.Forms;


	//********************************************************************************************
	// class FadingWindow
	//********************************************************************************************

	/// <summary>
	/// Base class for windows that can fade-in and fade-out.
	/// Also provides fade-out cancellation by moving the mouse over the window
	/// and static pinning to "keep" the window visible.
	/// </summary>

	internal class FadingWindow : Window, IDisposable
	{

		// time in milliseconds when fade-out begins after fade-in completes
		private readonly TimeSpan defaultFadeOutDelay = TimeSpan.FromMilliseconds(3000);

		// time in milliseconds when fade-out begins after mouse leaves the window
		private readonly TimeSpan leaveFadeOutDelay = TimeSpan.FromMilliseconds(2000);

		// standard Windows 7 offset of windows from taskbar
		protected const int DefaultWindowMargin = 2;

		private const int AppVerticalOffset = 39;
		private const int AppHorizontalOffset = 83;

		// hidden/visible Opacity levels
		private const double HiddenOpacity = 0.0;
		private const double VisibleOpacity = 1.0;

		private bool isPinned;
		private bool hasMouse;
		private bool isDisposed;
		private DispatcherTimer timer;
		private FrameworkElement element;

		private Storyboard fadeInStoryboard;
		private Storyboard fadeOutStoryboard;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// This constructor must be called by implementors or the fading functionality
		/// will not work.
		/// </summary>

		public FadingWindow ()
		{
			Opacity = 0.0;

			// by setting it hidden, this hides the window from the Alt-Tab program switcher
			Visibility = Visibility.Hidden;

			var animation = new DoubleAnimation(
				HiddenOpacity, VisibleOpacity, new Duration(TimeSpan.FromMilliseconds(300)))
			{
				BeginTime = TimeSpan.FromMilliseconds(100),
				AutoReverse = false
			};

			fadeInStoryboard = new Storyboard();
			fadeInStoryboard.Children.Add(animation);
			fadeInStoryboard.Completed += ShowCompleted;

			animation = new DoubleAnimation(
				VisibleOpacity, HiddenOpacity, new Duration(TimeSpan.FromMilliseconds(500)))
			{
				BeginTime = TimeSpan.FromMilliseconds(100),
				AutoReverse = false
			};

			fadeOutStoryboard = new Storyboard();
			fadeOutStoryboard.Children.Add(animation);
			fadeOutStoryboard.Completed += HideCompleted;

			timer = new DispatcherTimer();
			timer.Tick += InitiateFadeOut;
			timer.Interval = defaultFadeOutDelay;
		}


		/// <summary>
		/// Clean up all references and event handlers.
		/// </summary>

		public virtual void Dispose ()
		{
			if (!isDisposed)
			{
				if (IsOpaque)
				{
					Close();
				}

				if (timer != null)
				{
					timer.Tick -= InitiateFadeOut;
					timer.Stop();
					timer = null;
				}

				if (fadeInStoryboard != null)
				{
					fadeInStoryboard.Completed -= ShowCompleted;
					fadeInStoryboard.Stop();
					fadeInStoryboard.Children.Clear();
					fadeInStoryboard = null;
				}

				if (fadeOutStoryboard != null)
				{
					fadeOutStoryboard.Completed -= HideCompleted;
					fadeOutStoryboard.Stop();
					fadeOutStoryboard.Children.Clear();
					fadeOutStoryboard = null;
				}

				element = null;

				isDisposed = true;
			}
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Sets the primary element to be animated.  This should be a Border or similar
		/// FrameworkElement and not the Window itself.
		/// </summary>

		public FrameworkElement AnimatedElement
		{
			set
			{
				element = value;

				DoubleAnimation animation = fadeInStoryboard.Children[0] as DoubleAnimation;
				if (animation != null)
				{
					Storyboard.SetTargetName(animation, element.Name);
					Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
				}

				animation = fadeOutStoryboard.Children[0] as DoubleAnimation;
				if (animation != null)
				{
					Storyboard.SetTargetName(animation, element.Name);
					Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
				}
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating whether this window is currently in a visible
		/// and opaque state.
		/// </summary>

		public bool IsOpaque =>
			IsVisible &&
			// if element.Opacity == VisibleOpacity
			Math.Abs(element.Opacity - VisibleOpacity) < double.Epsilon;


		/// <summary>
		/// Gets or sets the pinned state of the window.  If pinned then fade-outs are
		/// disabled, otherwise fade-outs are enabled.
		/// </summary>

		public bool IsPinned
		{
			get
			{
				return isPinned;
			}

			set
			{
				// if unpinning a pinned window and the mouse is not over this window
				// then we can restart the fade-out timer
				if (isPinned && !value)
				{
					if (!hasMouse)
					{
						lock (timer)
						{
							timer.Interval = leaveFadeOutDelay;
							timer.Start();
						}
					}
				}

				isPinned = value;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Show the window, fading in opacity.
		/// </summary>

		public new void Show ()
		{
			// special case for TrackerWindow where track changes while window is displayed, we
			// want to reset the timer so it continues displaying the window for another cycle
			lock (timer)
			{
				if (timer.IsEnabled)
				{
					timer.Stop();
					timer.Interval = defaultFadeOutDelay;
					timer.Start();
					return;
				}
			}

			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(Show);
				return;
			}

			try
			{
				Visibility = Visibility.Visible;

				// stop fadeout if in progress
				fadeOutStoryboard.Stop(element);

				// pull it forward and show the window
				Topmost = true;
				Opacity = VisibleOpacity;

				if ((element.Opacity > 0) && (element.Opacity < VisibleOpacity))
				{
					// if animating then just complete it immediately
					ShowCompleted(this, new EventArgs());
				}
				// if (element.Opacity == 0)
				else if (Math.Abs(element.Opacity - 0) < double.Epsilon)
				{
					fadeInStoryboard.Begin(element, true);
				}
			}
			catch (Exception exc)
			{
				var dialog = new ExceptionDialog(new SmartException(exc), "Fading Window");
				dialog.ShowDialog();
			}
		}


		private void ShowCompleted (object sender, EventArgs e)
		{
			element.Opacity = VisibleOpacity;

			if (!hasMouse)
			{
				lock (timer)
				{
					timer.Interval = defaultFadeOutDelay;
					timer.Start();
				}
			}
		}


		//----------------------------------------------------------------------------------------

		/// <summary>
		/// Hide the window, fading-out opacity.
		/// </summary>

		public new void Hide ()
		{
			// stop fadein if in progress
			fadeInStoryboard.Stop(element);

			// allow it to be eclipsed
			Topmost = false;

			// start only if fully visible to avoid flicker
			// if (element.Opacity == VisibleOpacity)
			if (Math.Abs(element.Opacity - VisibleOpacity) < double.Epsilon)
			{
				fadeOutStoryboard.Begin(element, true);
			}
			else
			{
				// just hide immediatley
				HideCompleted(this, new EventArgs());
			}
		}


		public void HideNow ()
		{
			HideCompleted(null, null);
			hasMouse = false;
			isPinned = false;
		}


		private void HideCompleted (object sender, EventArgs e)
		{
			element.Opacity = HiddenOpacity;
			Opacity = HiddenOpacity;
			Visibility = Visibility.Hidden;

			OnHideCompleted();
		}


		/// <summary>
		/// Allows inheritors to perform additiona logic immediately after the window
		/// has completely faded out.
		/// </summary>

		protected virtual void OnHideCompleted ()
		{
		}


		//----------------------------------------------------------------------------------------

		/// <summary>
		/// When the mouse is visible to this window, that means it has a "virtual capture"
		/// of the window and prevents it from fading.
		/// </summary>
		/// <param name="e"></param>

		protected override void OnMouseMove (MouseEventArgs e)
		{
			hasMouse = true;

			// if Opacity != VisibleOpacity
			if (Math.Abs(Opacity - VisibleOpacity) > double.Epsilon)
			{
				fadeInStoryboard.Stop(element);
				fadeOutStoryboard.Stop(element);
				ShowCompleted(this, new EventArgs());
			}

			lock (timer)
			{
				if (timer.IsEnabled)
				{
					timer.Stop();
				}
			}

			base.OnMouseMove(e);
		}


		/// <summary>
		/// When the mouse is no longer visible to this window, that means it releases
		/// its "virtual capture", allowing subsequent fading.
		/// </summary>
		/// <param name="e"></param>

		protected override void OnMouseLeave (MouseEventArgs e)
		{
			hasMouse = false;

			if (!isPinned)
			{
				lock (timer)
				{
					timer.Interval = leaveFadeOutDelay;
					timer.Start();
				}
			}

			base.OnMouseLeave(e);
		}


		private void InitiateFadeOut (object sender, EventArgs e)
		{
			lock (timer)
			{
				timer.Stop();
			}

			Hide();
		}


		//----------------------------------------------------------------------------------------

		/// <summary>
		/// Positions the window near the given coordinates. Horizontal and vertical
		/// centering around that location is determined by taskbar docking edge.
		/// </summary>
		/// <param name="point">The "upper-left" point of the system tray</param>
		/// <param name="docking">The edge of the screen to which the taskbar is docked</param>

		public void SetPositionRelativeTo (Point point, ScreenEdge docking)
		{
			var height = (int)Math.Round(Height);
			var width = (int)Math.Round(Width);

			if ((docking == ScreenEdge.Bottom) || (docking == ScreenEdge.Top))
			{
				Left = point.X - (width / 2.0) - DefaultWindowMargin;

				Top = docking == ScreenEdge.Bottom
					? point.Y - height - DefaultWindowMargin
					: point.Y + DefaultWindowMargin;
			}
			else // Left|Right
			{
				Left = docking == ScreenEdge.Left
					? point.X + DefaultWindowMargin
					: point.X - DefaultWindowMargin - width;

				Top = point.Y - (int)Math.Round(height / 2.0);
			}

			Logger.Debug($"SetPosition x,y={Left},{Top} relative to x,y={point.X},{point.Y} at w={width} h={height}");
		}
	}
}
