//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Maintains the 4-part unique IDs of an iTunes object.
	/// </summary>
	/// <remarks>
	/// Note, this is a class instead of a struct so we don't kill the heap!
	/// </remarks>

	internal sealed class ObjectID
	{
		private int sourceID;
		private int playlistID;
		private int trackID;
		private int databaseID;


		/// <summary>
		/// Initialize a new instance with the given ID values.
		/// </summary>
		/// <param name="sourceID"></param>
		/// <param name="playlistID"></param>
		/// <param name="trackID"></param>
		/// <param name="databaseID"></param>

		public ObjectID (int sourceID, int playlistID, int trackID, int databaseID)
		{
			this.sourceID = sourceID;
			this.playlistID = playlistID;
			this.trackID = trackID;
			this.databaseID = databaseID;
		}



		/// <summary>
		/// Gets the track database ID of this object. 
		/// Valid for a track.  Must be zero for playlist or source.
		/// </summary>

		public int DatabaseID { get { return databaseID; } }


		/// <summary>
		/// Gets the playlist ID of this object. 
		/// Valid for a playlist or track.  Must be zero for a source.
		/// </summary>

		public int PlaylistID { get { return playlistID; } }


		/// <summary>
		/// Gets the source ID of this object. 
		/// Valid for a source, playlist, or track.
		/// </summary>

		public int SourceID { get { return sourceID; } }


		/// <summary>
		/// Gets the track ID of this object. 
		/// Valid for a track.  Must be zero for playlist or source.
		/// </summary>

		public int TrackID { get { return trackID; } }


		/// <summary>
		/// Gets a Boolean value indicating if this object ID specifies an IITPlaylist object.
		/// </summary>

		public bool IsPlaylist
		{
			get
			{
				return (databaseID == 0) && (playlistID > 0) && (trackID == 0);
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if this object ID specifies an IITSource object.
		/// </summary>

		public bool IsSource
		{
			get
			{
				return (playlistID == 0) && (sourceID > 0) && (trackID == 0);
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if this object ID specifies an IITTrack object.
		/// </summary>

		public bool IsTrack
		{
			get
			{
				return (databaseID > 0) && (playlistID > 0) && (trackID > 0);
			}
		}
	}
}
