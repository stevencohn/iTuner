//************************************************************************************************
// Copyright © 2017 Steven M. Cohn. All Rights Reserved.
//************************************************************************************************


namespace iTuner
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Runtime.InteropServices;
	using System.Windows;


	public enum ScreenEdge
	{
		Undefined = -1,
		Left = Interop.ABE_LEFT,
		Top = Interop.ABE_TOP,
		Right = Interop.ABE_RIGHT,
		Bottom = Interop.ABE_BOTTOM
	}


	//********************************************************************************************
	// Interop
	//********************************************************************************************

	[SuppressMessage ("ReSharper", "InconsistentNaming")]
	internal static class Interop
	{

		public const int ABE_BOTTOM = 3;
		public const int ABE_LEFT = 0;
		public const int ABE_RIGHT = 2;
		public const int ABE_TOP = 1;

		public const int ABM_GETTASKBARPOS = 0x00000005;

		public const int SPI_GETNONCLIENTMETRICS = 41;
		public const int LF_FACESIZE = 32;


		[StructLayout (LayoutKind.Sequential)]
		public struct APPBARDATA
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public uint uEdge;
			public RECT rc;
			public int lParam;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct GUID
		{
			public uint Data1;
			public ushort Data2;
			public ushort Data3;

			[MarshalAs (UnmanagedType.ByValArray, SizeConst = 8)] public byte[] Data4;
		}


		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct LOGFONT
		{
			public int lfHeight;
			public int lfWidth;
			public int lfEscapement;
			public int lfOrientation;
			public int lfWeight;
			public byte lfItalic;
			public byte lfUnderline;
			public byte lfStrikeOut;
			public byte lfCharSet;
			public byte lfOutPrecision;
			public byte lfClipPrecision;
			public byte lfQuality;
			public byte lfPitchAndFamily;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string lfFaceSize;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NONCLIENTMETRICS
		{
			public int cbSize;
			public int iBorderWidth;
			public int iScrollWidth;
			public int iScrollHeight;
			public int iCaptionWidth;
			public int iCaptionHeight;
			public LOGFONT lfCaptionFont;
			public int iSmCaptionWidth;
			public int iSmCaptionHeight;
			public LOGFONT lfSmCaptionFont;
			public int iMenuWidth;
			public int iMenuHeight;
			public LOGFONT lfMenuFont;
			public LOGFONT lfStatusFont;
			public LOGFONT lfMessageFont;
		}


		[StructLayout (LayoutKind.Sequential)]
		public struct NOTIFYICONIDENTIFIER
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uID;
			public GUID guidItem; // System.Guid can be used.
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			// convert to a WPF System.Windows.Rec
			public static implicit operator Rect (RECT rect)
			{
				if ((rect.right - rect.left < 0) || (rect.bottom - rect.top < 0))
					return Rect.Empty;

				return new Rect(
					rect.left,
					rect.top,
					rect.right - rect.left,
					rect.bottom - rect.top);
			}
		}


		[DllImport ("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr FindWindow (
			string strClassName,
			string strWindowName);


		[DllImport ("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindowEx (
			IntPtr parentHandle,
			IntPtr childAfter,
			string className,
			IntPtr windowTitle);


		/// <summary>
		/// The GetForegroundWindow function returns a handle to the foreground window
		/// (the window with which the user is currently working). The system assigns
		/// a slightly higher priority to the thread that creates the foreground window
		/// than it does to other threads. 
		/// </summary>
		/// <returns>
		/// The return value is a handle to the foreground window. The foreground window
		/// can be NULL in certain circumstances, such as when a window is losing activation.
		/// </returns>

		[DllImport("user32.dll")]
		public static extern int GetForegroundWindow ();


		[DllImport ("user32.dll")]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetWindowRect (
			IntPtr hWnd,
			out RECT lpRect);

		/// <summary>
		/// The SetForegroundWindow function puts the thread that created the specified window
		/// into the foreground and activates the window. Keyboard input is directed to the
		/// window, and various visual cues are changed for the user. The system assigns a
		/// slightly higher priority to the thread that created the foreground window than
		/// it does to other threads. 
		/// </summary>
		/// <param name="hWnd">
		/// Handle to the window that should be activated and brought to the foreground. 
		/// </param>
		/// <returns>
		/// If the window was brought to the foreground, the return value is nonzero. 
		/// If the window was not brought to the foreground, the return value is zero.
		/// </returns>

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow (IntPtr hWnd);


		[DllImport ("shell32.dll")]
		public static extern uint SHAppBarMessage (
			UInt32 dwMessage,
			ref APPBARDATA data);


		[DllImport ("Shell32.dll", SetLastError = true)]
		public static extern int Shell_NotifyIconGetRect (
			[In] ref NOTIFYICONIDENTIFIER identifier,
			out RECT iconLocation);


		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SystemParametersInfo (
			int uiAction,
			int uiParam,
			ref NONCLIENTMETRICS ncMetrics,
			int fWinIni);
	}
}