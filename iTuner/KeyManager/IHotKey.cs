//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System.Windows.Forms;


	//********************************************************************************************
	// interface IHotKey
	//********************************************************************************************

	/// <summary>
	/// Defines the minimal interface needed for consumers of HotKey
	/// </summary>

	internal interface IHotKey
	{
		/// <summary>
		/// Gets the action associated with this key sequence.
		/// </summary>

		HotKeyAction Action { get; }


		/// <summary>
		/// Gets the primary keyboard key-code identifier.
		/// </summary>

		Keys Code { get; }


		/// <summary>
		/// Gets the secondary keyboard key modifier: Alt, Ctrl, Win, and Shfit.
		/// This is a bit mask.
		/// </summary>

		KeyModifier Modifier { get; }


		/// <summary>
		/// Returns a string describing the key sequence such as "Alt+F9".
		/// </summary>
		/// <returns>A string value.</returns>

		string ToString ();
	}
}
