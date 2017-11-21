//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;


	/// <summary>
	/// Maintains a collection of Tracks.
	/// </summary>

	internal sealed class TrackCollection : Dictionary<PersistentID, Track>, IDisposable
	{
		private bool isDisposed = false;


		#region Lifecycle

		/// <summary>
		/// Dispose of this instance.
		/// </summary>

		~TrackCollection ()
		{
			Dispose();
		}


		/// <summary>
		/// Dispose of this instance releasing all items and the collection itself.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				foreach (IDisposable o in base.Values)
				{
					o.Dispose();
				}

				base.Clear();

				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		#endregion Lifecycle


		/// <summary>
		/// Add the given track to the collection, keyed by its persistent ID.
		/// </summary>
		/// <param name="track">A Track instance.</param>

		public void Add (Track track)
		{
			base.Add(track.PersistentID, track);
		}
	}
}
