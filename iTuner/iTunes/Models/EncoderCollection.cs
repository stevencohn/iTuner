//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;


	/// <summary>
	/// Maintains a collection of Encoders.
	/// </summary>

	internal sealed class EncoderCollection : List<Encoder>, IDisposable
	{
		private bool isDisposed = false;


		/// <summary>
		/// Dispose of this instance releasing all items and the collection itself.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				foreach (IDisposable o in this)
				{
					o.Dispose();
				}

				base.Clear();

				isDisposed = true;
			}
		}
	}
}
