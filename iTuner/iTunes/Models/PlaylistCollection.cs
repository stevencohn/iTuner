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
			}
		}


		/// <summary>
		/// Add the given playlist to the collection, keyed by its persistent ID.
		/// </summary>
		/// <param name="playlist">A Playlist instance.</param>

		public void Add (Playlist playlist)
		{
			base.Add(playlist.PersistentID, playlist);
		}


		/// <summary>
		/// Adds the elements of the specified collection to the end of the collection.
		/// </summary>
		/// <param name="collection"></param>

		public void AddRange (IEnumerable<Playlist> collection)
		{
			foreach (Playlist playlist in collection)
			{
				Add(playlist);
			}
		}


		/// <summary>
		/// Retrieves all the elements that match the conditions defined by the specified predicate
		/// </summary>
		/// <param name="match">
		/// The Predicate delegate that defines the conditions of the elements to search for.
		/// </param>
		/// <returns>
		/// A generic List containing all the elements that match the conditions defined by
		/// the specified predicate, if found; otherwise, an empty List.
		/// </returns>

		public List<Playlist> FindAll (Predicate<Playlist> match)
		{
			if (match == null)
			{
				throw new ArgumentNullException("match");
			}

			List<Playlist> list = new List<Playlist>();
			foreach (Playlist playlist in this.Values)
			{
				if (playlist != null)
				{
					if (match(playlist))
					{
						list.Add(playlist);
					}
				}
			}

			return list;
		}
	}
}
