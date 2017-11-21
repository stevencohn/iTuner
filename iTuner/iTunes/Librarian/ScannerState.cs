//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Identifies scanner states as executed by the librarian.
	/// </summary>

	internal enum ScannerState
	{

		/// <summary>
		/// The librarian is about to execute a scheduled scanner.
		/// </summary>

		Beginning,


		/// <summary>
		/// The librarian just completed executing a scheduled scanner.
		/// </summary>

		Completed
	}
}
