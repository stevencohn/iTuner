//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;


	/// <summary>
	/// A simple typedef defining a collection to maintain persistent track IDs.
	/// </summary>
	/// <remarks>
	/// The persistent IDs of a track does not change, while the the track TracKID property
	/// is a run-time only value that differs between sessions.
	/// </remarks>

	internal class PersistentIDCollection : List<PersistentID>
	{

		/// <summary>
		/// 
		/// </summary>

		public PersistentIDCollection ()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="list"></param>

		public PersistentIDCollection (List<PersistentID> list)
			: base(list)
		{
		}


		/// <summary>
		/// Gets or sets the preferred album name.  This is derived from the selected track
		/// when an export request is made.  It overrides the album name of other tracks 
		/// in this collection when building relative subdirectories during an export.
		/// </summary>

		public string Album
		{
			get;
			set;
		}


		/// <summary>
		/// Gets or sets the preferred artist name.  This is derived from the selected track
		/// when an export request is made.  It overrides both track.Artist and track.Composer
		/// when building relative subdirectories during an export.
		/// </summary>

		public string Artist
		{
			get;
			set;
		}


		/// <summary>
		/// Gets or sets the canonical name of this collection.
		/// </summary>
		/// <remarks>
		/// The name is typically derived from the export option such as Album name,
		/// Artist name, or Playlist name.
		/// </remarks>

		public string Name
		{
			get;
			set;
		}
	}
}
