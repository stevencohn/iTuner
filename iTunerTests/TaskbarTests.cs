//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.IO;
	using System.Windows;
	using WinForms = System.Windows.Forms;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner;


	/// <summary>
	/// </summary>

	[TestClass]
	public class TaskbarTests : TestBase
	{

		/// <summary>
		/// </summary>

		[TestMethod]
		public void GetTaskbarInfo ()
		{
			WinForms.NotifyIcon icon = new WinForms.NotifyIcon();

			Taskbar bar = new Taskbar();
			Point point = bar.GetTangentPosition(icon);
		}


		[TestMethod]
		public void PathTests ()
		{
			string path = @"C:\Music\AC/DC\Alternating Current\01 Live Wire.mp3";
			string clean;
			
			clean = PathHelper.Clean(path);
			clean = PathHelper.CleanDirectoryPath(Path.GetDirectoryName(path));
			clean = PathHelper.CleanFileName(Path.GetFileNameWithoutExtension(path));
			clean = PathHelper.CleanFileName("AC/DC");
		}


		[TestMethod]
		public void UpdateLive ()
		{
			YMEWLMNowPlaying.WLMController.SetWLMNowPlaying(
				"a", "b", "c");
		}
	}
}

namespace YMEWLMNowPlaying
{
	using System;
	using System.Runtime.InteropServices;

	public class WLMController
	{
		struct COPYDATASTRUCT
		{
			public int dwData;
			public int cbData;
			public IntPtr lpData;
		}

		private const UInt32 WM_COPYDATA = 0x004A;

		[DllImport("user32.dll")]
		static extern int SendMessage (IntPtr hWnd, uint Msg, int wParam, ref COPYDATASTRUCT lParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr FindWindowEx (IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		private WLMController ()
		{ }

		public static void HideWLMNowPlaying ()
		{
			SetWLMNowPlaying("\\0Music\\00\\0\\0\\0\\0\\0");
		}

		public static void SetWLMNowPlaying (string title, string artist, string album)
		{
			string MSNMusicString = "\\0Music\\0{0}\\0{2} - {1}\\0{2}\\0{3}\\0{4}\\0";

			string buffer;

			buffer = string.Format(MSNMusicString, 1, title, artist, album, 0);

			SetWLMNowPlaying(buffer);
		}

		static void SetWLMNowPlaying (string nowPlaying)
		{
			COPYDATASTRUCT msndata;

			GCHandle handle = GCHandle.Alloc(nowPlaying, GCHandleType.Pinned);

			try
			{
				IntPtr msnui = IntPtr.Zero;
				msndata.dwData = 0x547;
				msndata.lpData = handle.AddrOfPinnedObject();
				msndata.cbData = (nowPlaying.Length * 2) + 2;

				msnui = FindWindowEx(IntPtr.Zero, msnui, "MsnMsgrUIManager", null);

				SendMessage(msnui, WM_COPYDATA, 0, ref msndata);
			}
			finally
			{
				if (handle != null)
					handle.Free();
			}
		}
	}
}
