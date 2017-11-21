//************************************************************************************************
// Copyright © 2017 Steven M. Cohn. All Rights Reserved.
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;


	//********************************************************************************************
	// SystemTray
	//********************************************************************************************

	internal class SystemTray
	{
		private readonly IntPtr taskbarHandle;


		public SystemTray (IntPtr taskbarHandle)
		{
			this.taskbarHandle = taskbarHandle;
		}


		public Rect GetRectangle ()
		{
			var systrayHandle = Interop.FindWindowEx(taskbarHandle, IntPtr.Zero, "TrayNotifyWnd", IntPtr.Zero);
			if (systrayHandle == IntPtr.Zero)
			{
				throw new Exception("Failed to find notify window handle");
			}

			Interop.RECT rect;
			if (!Interop.GetWindowRect(systrayHandle, out rect))
			{
				throw new Exception("Failed to get notify window rectangle");
			}
			return new Rect(
				new Point(rect.left, rect.top),
				new Size((rect.right - rect.left) + 1, (rect.bottom - rect.top) + 1));
		}
	}
}