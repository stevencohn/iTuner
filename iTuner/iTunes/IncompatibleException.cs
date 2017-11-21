//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// This exception indicates that the iTunesLib DLL that ships with iTuner is not
	/// compatible with the currently installed version of iTunes.
	/// </summary>

	internal class IncompatibleException : Exception
	{

		/// <summary>
		/// Blah.
		/// </summary>

		public IncompatibleException ()
			: base("iTuner/iTunes interop assembly is not compatible with current iTunes version")
		{
		}


		/// <summary>
		/// Blah.
		/// </summary>
		/// <param name="message"></param>

		public IncompatibleException (string message)
			: base(message)
		{
		}


		/// <summary>
		/// Blah.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="inner"></param>

		public IncompatibleException (string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
