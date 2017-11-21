//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;


	/// <summary>
	/// Describes an operation caught by the Controller and to be handled
	/// by the MaintenanceScanner.
	/// </summary>

	internal class MaintenanceAction
	{

		public enum ActionType
		{
			AddPlaylist,
			AddTracksToPlaylist,
			RemovePlaylist,
			RemoveTracksFromPlaylist
		}

		// one Source followed by one Playlist
		private const string AddPlaylistPattern = "^SP$";

		// one Source followed by one Playlist followed by one or more Tracks
		private const string AddTracksPattern = "^SPT+$";

		// one Playlist followed by one Source
		private const string RemovePlaylistPattern = "^PS$";

		// one or more Tracks followed by one Source followed by one Playlist
		private const string RemoveTracksPattern = "^T+SP$";


		private ActionType action;
		private ObjectIDCollection list;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of the specified type with the given object IDs.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="oid"></param>

		private MaintenanceAction (ActionType action, ObjectIDCollection list)
		{
			this.action = action;
			this.list = list;
		}


		/// <summary>
		/// Instantiate a new maintenance action only if the safe array describes a recognizable
		/// activity pattern.
		/// </summary>
		/// <param name="safeArray"></param>
		/// <returns></returns>

		public static MaintenanceAction Create (object safeArray)
		{
			Array array = safeArray as Array;
			ObjectIDCollection list = new ObjectIDCollection();
			StringBuilder builder = new StringBuilder();
			ObjectID oid;

			// convert the safe array to a collection and construct the pattern map

			int lower = array.GetLowerBound(0);
			int upper = array.GetUpperBound(0);

			for (int i = lower; i <= upper; i++)
			{
				oid = new ObjectID(
					(int)array.GetValue(i, 0),		// source
					(int)array.GetValue(i, 1),		// playlist
					(int)array.GetValue(i, 2),		// trackid
					(int)array.GetValue(i, 3));		// databaseid

				list.Add(oid);

				if (oid.IsPlaylist)
				{
					builder.Append("P");
				}
				else if (oid.IsSource)
				{
					builder.Append("S");
				}
				else if (oid.IsTrack)
				{
					builder.Append("T");
				}
				else
				{
					builder.Append("X");
				}
			}

			// recognize pattern to instantiate new MaintenanceAction

			string map = builder.ToString();

			if (Regex.IsMatch(map, AddPlaylistPattern))
			{
				return new MaintenanceAction(ActionType.AddPlaylist, list);
			}
			else if (Regex.IsMatch(map, AddTracksPattern))
			{
				return new MaintenanceAction(ActionType.AddTracksToPlaylist, list);
			}
			else if (Regex.IsMatch(map, RemovePlaylistPattern))
			{
				return new MaintenanceAction(ActionType.RemovePlaylist, list);
			}
			else if (Regex.IsMatch(map, RemoveTracksPattern))
			{
				return new MaintenanceAction(ActionType.RemoveTracksFromPlaylist, list);
			}

			// Not a recognizable pattern, return null

			list.Clear();
			return null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Gets the ActionType represented by this instance.
		/// </summary>

		public ActionType Action
		{
			get { return action; }
		}


		/// <summary>
		/// Gets the ObjectID of the playlist affected by this action.
		/// </summary>

		public ObjectID PlaylistOID
		{
			get { return list.FirstOrDefault(p => p.IsPlaylist); }
		}


		/// <summary>
		/// Gets a collection of track ObjectIDs affected by this action.
		/// </summary>

		public ObjectIDCollection TrackOIDs
		{
			get
			{
				ObjectIDCollection tracks = new ObjectIDCollection();
				tracks.AddRange(list.FindAll(p => p.IsTrack));
				return tracks;
			}
		}
	}
}
