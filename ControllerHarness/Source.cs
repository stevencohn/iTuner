//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using iTunesLib;


	/// <summary>
	/// A safe wrapper of an IITSource object.
	/// </summary>
	
	internal class Source : Interaction
	{
		private IITSource source;


		/// <summary>
		/// Initialize a new instance that wraps the given iTunes source COM object.
		/// </summary>
		/// <param name="source">An IITSource instance.</param>

		public Source (IITSource source)
			: base()
		{
			this.source = source;
		}


		/// <summary>
		/// Interaction.Cleanup override; release reference to internal source.
		/// </summary>

		protected override void Cleanup ()
		{
			Release(source);
			source = null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Gets the kind of this source.
		/// </summary>

		public ITSourceKind Kind
		{
			get
			{
				return Invoke((Func<ITSourceKind>)delegate
				{
					return source.Kind;
				});
			}
		}


		/// <summary>
		/// Gets the total size of the source, if it has a fixed size.
		/// </summary>

		public double Capacity
		{
			get
			{
				return Invoke((Func<double>)delegate
				{
					return source.Capacity;
				});
			}
		}


		/// <summary>
		/// Gets the free space of the source, if it has a fixed size.
		/// </summary>

		public double FreeSpace
		{
			get
			{
				return Invoke((Func<double>)delegate
				{
					return source.FreeSpace;
				});
			}
		}


		/// <summary>
		/// Gets a collection of playlists contained in this source.
		/// </summary>

		public PlaylistCollection Playlists
		{
			get
			{
				return Invoke((Func<PlaylistCollection>)delegate
				{
					PlaylistCollection collection = new PlaylistCollection();
					foreach (IITPlaylist playlist in source.Playlists)
					{
						if (playlist != null)
						{
							collection.Add(new Playlist(playlist));
						}
					}
					return collection;
				});
			}
		}
	}
}
