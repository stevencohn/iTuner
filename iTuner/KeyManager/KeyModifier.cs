//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;


	/// <summary>
	/// Specifies the available keyboard key modifiers.
	/// </summary>
	/// <remarks>
	/// The numeric value must match the Windows bit mask value.
	/// </remarks>

	[Flags]
	internal enum KeyModifier : uint
	{
		/// <summary>
		/// Indicates that no modifier applies.
		/// </summary>

		None = 0,


		/// <summary>
		/// The Alt key.
		/// </summary>

		Alt = 1,


		/// <summary>
		/// The Ctrl key.
		/// </summary>

		Ctrl = 2,


		/// <summary>
		/// The Shift key.
		/// </summary>

		Shift = 4,


		/// <summary>
		/// The Windows key.
		/// </summary>

		Win = 8
	}
}
