//************************************************************************************************
// Copyright © 2017 Steven M. Cohn. All Rights Reserved.
//************************************************************************************************

namespace iTuner
{
	using System.Windows;
	using System.Windows.Interop;
	using Forms = System.Windows.Forms;


	/// <summary>
	/// Extends the Window class with helper methods
	/// </summary>

	internal static class WindowExtensions
	{

		public static Forms.Screen GetScreen (this Window window)
		{
			return Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
		}
	}
}
