//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using Resx = Properties.Resources;


	/// <summary>
	/// Specifically suited to handle playlist modifications - adding tracks or removing tracks.
	/// </summary>

	internal class MaintenanceScanner : ScannerBase
	{

		private MaintenanceAction action;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance of this scaner with the specified iTunes interface.
		/// </summary>
		/// <param name="itunes"></param>
		/// <param name="catalog"></param>

		public MaintenanceScanner (
			Controller controller, ICatalog catalog, MaintenanceAction action)
			: base(Resx.I_ScanMaintenance, controller, catalog)
		{
			base.description = Resx.ScanMaintenance;
			base.tooltip = action.Action.ToString();

			this.action = action;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Execute the scanner.
		/// </summary>

		public override void Execute ()
		{
			Logger.WriteLine(base.name, String.Format(
				"Beginning DB maintenance: {0}", action.Action.ToString()));

			if (base.isActive)
			{
				switch (action.Action)
				{
					case MaintenanceAction.ActionType.AddPlaylist:
						AddPlaylist();
						break;

					case MaintenanceAction.ActionType.AddTracksToPlaylist:
						AddTracksToPlaylist();
						break;

					case MaintenanceAction.ActionType.RemovePlaylist:
						RemovePlaylist();
						break;

					case MaintenanceAction.ActionType.RemoveTracksFromPlaylist:
						RemoveTracksFromPlaylist();
						break;
				}
			}
			else
			{
				Logger.WriteLine(base.name, "itunes is not yet active");
			}

			Logger.WriteLine(base.name, String.Format(
				"Completed DB maintenance: {0}", action.Action.ToString()));
		}


		/// <summary>
		/// Add a new playlist or update the name of an existing playlist in the catalog.
		/// </summary>

		private void AddPlaylist ()
		{
			Playlist playlist = controller.GetPlaylist(action.PlaylistOID);
			if (playlist != null)
			{
				catalog.AddPlaylist(playlist);
			}
		}


		/// <summary>
		/// Adds one or more tracks to an existing playlist in the catalog.
		/// </summary>

		private void AddTracksToPlaylist ()
		{
			PersistentID playlistPID = controller.GetPersistentID(action.PlaylistOID);
			if (!playlistPID.IsEmpty)
			{
				PersistentIDCollection trackPIDs = new PersistentIDCollection();

				ObjectIDCollection trackOIDs = action.TrackOIDs;
				foreach (ObjectID trackOID in trackOIDs)
				{
					PersistentID trackPID = controller.GetPersistentID(trackOID);
					if (!trackPID.IsEmpty)
					{
						trackPID.TransientID = trackOID.TrackID;
						trackPIDs.Add(trackPID);
					}
				}

				if (trackPIDs.Count > 0)
				{
					catalog.AddTracksToPlaylist(trackPIDs, playlistPID);
				}
			}
		}


		/// <summary>
		/// Removes a playlist from the catalog.
		/// </summary>

		private void RemovePlaylist ()
		{
			PersistentIDCollection playlistPIDs = new PersistentIDCollection();
			foreach (Playlist playlist in controller.Playlists.Values)
			{
				if (playlist != null)
				{
					playlistPIDs.Add(playlist.PersistentID);
					playlist.Dispose();
				}
			}

			if (playlistPIDs.Count > 0)
			{
				catalog.RefreshPlaylists(playlistPIDs);
			}
		}


		/// <summary>
		/// Removes one or more tracks from an existing playlist in the catalog.
		/// </summary>

		private void RemoveTracksFromPlaylist ()
		{
			PersistentID playlistPID = controller.GetPersistentID(action.PlaylistOID);
			if (!playlistPID.IsEmpty)
			{
				catalog.RefreshPlaylist(playlistPID);
			}
		}
	}
}
