//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// This exception indicates that iTunes attempted to convert a protected music
	/// file to a different format.
	/// </summary>
	/// <remarks>
	/// iTunesLib will raise a NullReferenceException as iTunes displays the "protected"
	/// dialog.  It does not disable COM interop or raise a COMException.  However we cannot
	/// test if status.Tracks is null because that alone will throw a COMException.  So
	/// when a NullReferenceException is raised, we assume it's because of a protected file
	/// and we instead raise a ProtectedException. 
	/// </remarks>

	internal class ProtectedException : Exception
	{

		/// <summary>
		/// Blah.
		/// </summary>

		public ProtectedException ()
		{
		}


		/// <summary>
		/// Blah.
		/// </summary>
		/// <param name="message"></param>

		public ProtectedException (string message)
			: base(message)
		{
		}


		/// <summary>
		/// Blah.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="inner"></param>

		public ProtectedException (string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
