//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Describes the progress reported by the librarian background worker.
	/// </summary>

	internal class ScanningProgress
	{

		/// <summary>
		/// Initialize a new instance with the specifie state and name.
		/// </summary>
		/// <param name="state">The execution state of the scanner.</param>
		/// <param name="name">The name of the current scanner.</param>

		public ScanningProgress (ScannerState state, string name)
		{
			this.State = state;
			this.Name = name;
		}


		/// <summary>
		/// Gets the name of the scanner in context.
		/// </summary>

		public string Name { get; private set; }


		/// <summary>
		/// Gets the state of the scanner in context.
		/// </summary>

		public ScannerState State { get; private set; }
	}
}
