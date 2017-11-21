//************************************************************************************************
// Copyright © 2017 Steven M. Cohn. All Rights Reserved.
//************************************************************************************************

#define Verbose2

namespace iTuner
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows;
	using Forms = System.Windows.Forms;


	internal class Taskbar
	{

		private readonly IntPtr taskbarHandle;
		private readonly Interop.APPBARDATA data;
		private SystemTray tray;


		public Taskbar ()
		{
			data = new Interop.APPBARDATA();
			data.cbSize = (uint)Marshal.SizeOf(data.GetType());

			// TODO: how do we find the taskbar for the screen containing the iTuner notify icon?
			//var form = (Form)(Control.FromHandle(myHandle));
			//var systrayHandle = Interop.FindWindowEx(taskbarHandle, IntPtr.Zero, "TrayNotifyWnd", IntPtr.Zero);

			taskbarHandle = Interop.FindWindow("Shell_TrayWnd", null);
			if (taskbarHandle == IntPtr.Zero)
			{
				throw new Exception("Failed to find task bar handle");
			}

			var result = Interop.SHAppBarMessage(Interop.ABM_GETTASKBARPOS, ref data);
			if (result != 1)
			{
				throw new Exception("Failed to retrieve taskbar position information");
			}

			#region Verbose
#if Verbose
			Logger.Debug($"Taskbar Edge={Edge}");
			var tr = GetRectangle();
			Logger.Debug($"Taskbar Rect x={tr.X} y={tr.Y} left={tr.Left} top={tr.Top} " +
				$"width={tr.Width} height={tr.Height}");

			var sr = SystemTray.GetRectangle();
			Logger.Debug($"Taskbar Systray.Rect x={sr.X} y={sr.Y} left={sr.Left} top={sr.Top} " +
				$"width={sr.Width} height={sr.Height}");
#endif
			#endregion
		}


		public ScreenEdge Edge => (ScreenEdge)data.uEdge;


		public Rect GetRectangle ()
		{
			return new Rect(
				new Point(data.rc.left, data.rc.top),
				new Size((data.rc.right - data.rc.left) + 1, (data.rc.bottom - data.rc.top) + 1));
		}


		public SystemTray SystemTray => tray ?? (tray = new SystemTray(taskbarHandle));


		public Point GetTangentPosition (Forms.NotifyIcon icon)
		{
			#region Verbose
#if Verbose
			var wa = icon.GetScreen().WorkingArea;
			Logger.Debug($"GetTangent trayIcon.ScreenOf x={wa.X} y={wa.Y} left={wa.Left} " +
				$"top={wa.Top} width={wa.Width} height={wa.Height} (Win32 unscaled)");
#endif
			#endregion

			var location = icon.GetRectangle();
			if (location.Equals(Rect.Empty))
			{
				location = SystemTray.GetRectangle();

				// we need to estimate full geometry of icon based on taskbar orientation
				// presume standard icon size of 16x16
				if ((Edge == ScreenEdge.Bottom) || (Edge == ScreenEdge.Top))
				{
					location = new Rect(new Point(location.X, location.Y), new Size(16, location.Y));
				}
				else // left|right
				{
					location = new Rect(new Point(location.X, location.Y), new Size(location.X, 16));
				}

				#region Verbose
#if Verbose
				Logger.Debug("GetTangent fallback to tray location " +
					$"x={location.X}, y={location.Y} w={location.Width} h={location.Height}");
#endif
				#endregion
			}
			#region Verbose
#if Verbose
			else
			{
				// we should have full geometry of icon rectange regardless of taskbar Edge
				Logger.Debug("GetTangent notify icon location " +
					$"x={location.X}, y={location.Y} w={location.Width} h={location.Height}" +
					" (Win32 unscaled)");
			}

			var xa = SystemParameters.WorkArea;
			Logger.Debug($"...WPF workarea x={xa.X} y={xa.Y} left={xa.Left} top={xa.Top} " +
				$"width={xa.Width} height={xa.Height}");

			var fa = Forms.Screen.PrimaryScreen.WorkingArea;
			Logger.Debug($"...FRM workarea x={fa.X} y={fa.Y} left={fa.Left} top={fa.Top} " +
				$"width={fa.Width} height={fa.Height}");
#endif
			#endregion

			// calculate scaling factors
			// TODO: this is limited to the primary monitor so we must assume this is where our
			// notify icon resides; otherwise, this may not be correct in a multi-DPI desktop

			var xscale = Forms.Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.WorkArea.Width;
			var yscale = Forms.Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.WorkArea.Height;

			location = new Rect(
				new Point(location.X / xscale, location.Y / yscale),
				new Size(location.Width / xscale, location.Height / yscale));

			#region Verbose
#if Verbose
			Logger.Debug("GetTangent scale-adjusted location " +
				$"x={location.X}, y={location.Y} w={location.Width} h={location.Height}");
#endif
			#endregion

			var x = 0.0;
			var y = 0.0;

			switch (Edge)
			{
				case ScreenEdge.Bottom:
				case ScreenEdge.Right:
					// return upper-left corner of icon
					x = location.Left; // + (int)(location.Width / 2);
					y = location.Top;
					break;

				case ScreenEdge.Top:
					// return lower-left corner of tray
					x = location.Left;
					y = location.Bottom;
					break;

				case ScreenEdge.Left:
					// return upper-right corner of tray
					x = location.Top;
					y = location.Right;
					break;
			}

			#region Verbose
#if Verbose
			Logger.Debug($"GetTangent X={x}, Y={y}");
#endif
			#endregion

			return new Point { X = x, Y = y };
		}
	}
}
