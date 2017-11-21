//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Linq;
	using System.Collections.Generic;


	/// <summary>
	/// Maintains a collection of Playlists.
	/// </summary>

	internal sealed class PlaylistCollection : Dictionary<PersistentID, Playlist>, IDisposable
	{
		private bool isDisposed = false;


		#region Lifecycle

		/// <summary>
		/// Dispose of this instance.
		/// </summary>
		
		~PlaylistCollection ()
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
		/// Add the given playlist to the collection, keyed by its persistent ID.
		/// </summary>
		/// <param name="playlist">A Playlist instance.</param>

		public void Add (Playlist playlist)
		{
			base.Add(playlist.PersistentID, playlist);
		}
	}
}
