//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using System.Xml;


	/// <summary>
	/// 
	/// </summary>

	internal abstract class PlaylistReaderBase : IPlaylistReader
	{
		private bool isOpen;
		private bool isDisposed;				// true if instance disposed

		protected string path;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="createSubdirectories"></param>

		public PlaylistReaderBase (string path)
		{
			this.path = path;
			this.isOpen = false;
			this.isDisposed = false;

			Open();
		}


		/// <summary>
		/// 
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				if (isOpen)
				{
					Close();
				}

				isDisposed = true;
			}
		}


		/// <summary>
		/// Close the playlist, including reader.
		/// </summary>

		protected virtual void Close ()
		{
			isOpen = false;
		}


		/// <summary>
		/// Gets the full path of the next track specified in the playlist file.
		/// </summary>
		/// <returns>
		/// A string specifying the full path of a file or <b>null</b> if there are no
		/// further tracks available.
		/// </returns>

		public abstract string GetNext ();


		/// <summary>
		/// Open the playlist.
		/// </summary>

		protected virtual void Open ()
		{
			isOpen = true;
		}
	}
}
