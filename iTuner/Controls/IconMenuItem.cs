//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Drawing;
	using System.Drawing.Drawing2D;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;


	/// <summary>
	/// A very simple menu item with icon, specialized for iTuner.
	/// </summary>

	internal class IconMenuItem : MenuItem
	{
		private const int DefaultHeight = 20;
		private const int MarginWidth = 28;

		private Font font;
		private Icon icon;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Used to add separators to the menu ("-")
		/// </summary>
		/// <param name="text">The caption text.</param>

		public IconMenuItem (string text)
		{
			this.font = GetSystemMenuFont();

			this.icon = null;
			Text = text;
			OwnerDraw = true;
		}


		/// <summary>
		/// Initializes an item with the specified caption.
		/// </summary>
		/// <param name="text">The caption text</param>
		/// <param name="handler"></param>

		public IconMenuItem (string text, EventHandler handler)
			: this(text)
		{
			Click += handler;
		}


		/// <summary>
		/// Initializes an item with the specified icon and caption.
		/// </summary>
		/// <param name="icon">The icon resource to display</param>
		/// <param name="text">The caption text</param>
		/// <param name="handler"></param>

		public IconMenuItem (Icon icon, string text, EventHandler handler)
			: this(text)
		{
			this.icon = icon;
			Click += handler;
		}


		/// <summary>
		/// Initializes an item with the specified icon and caption.
		/// </summary>
		/// <param name="icon">The icon resource to display</param>
		/// <param name="text">The caption text</param>
		/// <param name="items"></param>

		public IconMenuItem (Icon icon, string text, MenuItem[] items)
			: this(text)
		{
			this.icon = icon;
			MenuItems.AddRange(items);
		}


		/// <summary>
		/// Clean up any contained objects that need to be disposed.
		/// </summary>
		/// <param name="disposing">
		/// <b>true</b> if dicpose is called from the client;
		/// <b>false</b> if called from the finalizer.
		/// </param>

		protected override void Dispose (bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				icon?.Dispose();
				font?.Dispose();
			}

			icon = null;
			font = null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Gets or sets the icon to display within this menu item.
		/// </summary>

		public Icon Icon
		{
			get
			{
				return icon;
			}

			set
			{
				icon?.Dispose();
				icon = value;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Raises the MeasureItem event.
		/// </summary>
		/// <param name="e">The event args for measuring this item.</param>

		protected override void OnMeasureItem (MeasureItemEventArgs e)
		{
			base.OnMeasureItem(e);

			if (this.Text.Equals("-"))
			{
				e.ItemHeight = 6;

				// just an arbitrary minimum width
				e.ItemWidth = 50;
			}
			else
			{
				using (StringFormat format = new StringFormat(StringFormatFlags.NoWrap))
				{
					SizeF size = e.Graphics.MeasureString(this.Text, font, 10000, format);

					e.ItemHeight =
						(int)(size.Height < DefaultHeight ? DefaultHeight : size.Height + 4);

					double fudge = (this.Parent is ContextMenu ? 2.5 : 1.2);

					e.ItemWidth =
						(int)(e.Graphics.MeasureString(this.Text, font).Width + (MarginWidth * fudge));
				}
			}
		}


		/// <summary>
		/// Raises the DrawItem event.
		/// </summary>
		/// <param name="e">A DrawItemEventArgs that contains the event data.</param>

		protected override void OnDrawItem (DrawItemEventArgs e)
		{
			//e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

			Rectangle bounds = e.Bounds;

			// draw the basic background
			e.Graphics.FillRectangle(SystemBrushes.Control, bounds);

			// draw the vertical icon|text separator
			e.Graphics.DrawLine(
				SystemPens.ControlLight,
				bounds.Left + MarginWidth - 1, bounds.Top,
				bounds.Left + MarginWidth - 1, bounds.Bottom);
			e.Graphics.DrawLine(
				SystemPens.ControlLightLight,
				bounds.Left + MarginWidth, bounds.Top,
				bounds.Left + MarginWidth, bounds.Bottom);

			if (Text.Equals("-"))
			{
				// draw horizontal separator
				e.Graphics.DrawLine(
					SystemPens.ControlLight,
					bounds.Left + MarginWidth + 1, bounds.Top + 2,
					bounds.Right, bounds.Top + 2);
				e.Graphics.DrawLine(
					SystemPens.ControlLightLight,
					bounds.Left + MarginWidth, bounds.Top + 3,
					bounds.Right, bounds.Top + 3);
			}
			else
			{
				// draw check state
				if ((e.State & DrawItemState.Checked) > 0)
				{
					if ((e.State & DrawItemState.Selected) > 0)
					{
						DrawRoundedRectangle(e.Graphics,
							new Rectangle(bounds.X, bounds.Y, MarginWidth - 3, bounds.Height),
							(float)2.2);
					}
					else
					{
						FillRoundedRectangle(e.Graphics,
							new Rectangle(bounds.X, bounds.Y, MarginWidth - 3, bounds.Height),
							(float)2.2);
					}
				}

				// draw selection rectangle
				if ((e.State & DrawItemState.Disabled) == 0)
				{
					if ((e.State & DrawItemState.Selected) > 0)
					{
						FillRoundedRectangle(e.Graphics, bounds, (float)2.2);
					}
				}

				// draw the icon
				if (icon != null)
				{
					e.Graphics.DrawIcon(icon, bounds.Left + 5, bounds.Top + 2);
				}

				// draw the text
				Color color = (this.Enabled ? SystemColors.MenuText : SystemColors.GrayText);
				using (Brush brush = new SolidBrush(color))
				{
					e.Graphics.DrawString(
						this.Text, font, brush, bounds.Left + MarginWidth + 6, e.Bounds.Top + 2);
				}
			}
		}


		/// <summary>
		/// Discover the current system default font for menu items.
		/// </summary>
		/// <returns>A Font instance.</returns>

		private Font GetSystemMenuFont ()
		{
			var metrics = new Interop.NONCLIENTMETRICS
			{
				cbSize = Marshal.SizeOf(typeof(Interop.NONCLIENTMETRICS))
			};

			try
			{
				var result = Interop.SystemParametersInfo(
					Interop.SPI_GETNONCLIENTMETRICS, metrics.cbSize, ref metrics, 0);

				if (result)
				{
					return Font.FromLogFont(metrics.lfMenuFont);
				}

				var lastError = Marshal.GetLastWin32Error();
				Logger.WriteLine(Logger.Level.Error, "IconMenu",
					$"Error {lastError} calling Interop.SystemParametersInfo");

				return null;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}

			return null;
		}


		private void DrawRoundedRectangle (
			Graphics g, Rectangle bounds, float radius)
		{
			Color ac = SystemColors.ActiveCaption;
			using (var pen = new Pen(Color.FromArgb(0xF0, ac.R, ac.B, ac.B)))
			{
				int x = bounds.Left;
				int y = bounds.Top;
				int height = bounds.Height - 1;
				int width = bounds.Width - 1;

				GraphicsPath path = MakePath(x, y, width, height, radius);
				g.DrawPath(pen, path);
			}
		}


		private void FillRoundedRectangle (
			Graphics g, Rectangle bounds, float radius)
		{
			Color ac = SystemColors.ActiveCaption;
			using (var pen = new Pen(Color.FromArgb(0xF0, ac.R, ac.B, ac.B)))
			{
				Color gic = SystemColors.GradientInactiveCaption;
				Color gac = SystemColors.GradientActiveCaption;
				using (var brush = new LinearGradientBrush(
					bounds,
					Color.FromArgb(0x20, gic.R, gic.G, gic.B),
					Color.FromArgb(0x60, gac.R, gac.G, gac.B),
					LinearGradientMode.Vertical))
				{
					FillRoundedRectangle(g,
						pen, brush,
						bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1, radius);
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="g"></param>
		/// <param name="pen"></param>
		/// <param name="brush"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="radius"></param>

		private void FillRoundedRectangle (
			Graphics g, Pen pen, Brush brush,
			float x, float y, float width, float height, float radius)
		{
			using (GraphicsPath path = MakePath(x, y, width, height, radius))
			{
				g.FillPath(brush, path);
				g.DrawPath(pen, path);
			}
		}


		private GraphicsPath MakePath (float x, float y, float width, float height, float radius)
		{
			GraphicsPath path = new GraphicsPath();

			// top line
			path.AddLine(x + radius, y, x + width - (radius * 2), y);

			// top right corner
			path.AddArc(x + width - (radius * 2), y, radius * 2, radius * 2, 270, 90);

			// right line
			path.AddLine(x + width, y + radius, x + width, y + height - (radius * 2));

			// bottom right corner
			path.AddArc(x + width - (radius * 2), y + height - (radius * 2), radius * 2, radius * 2, 0, 90);

			// bottom line
			path.AddLine(x + width - (radius * 2), y + height, x + radius, y + height);

			// bottom left corner
			path.AddArc(x, y + height - (radius * 2), radius * 2, radius * 2, 90, 90);

			// left line
			path.AddLine(x, y + height - (radius * 2), x, y + radius);

			// top left corner
			path.AddArc(x, y, radius * 2, radius * 2, 180, 90);

			path.CloseFigure();

			return path;
		}
	}
}
