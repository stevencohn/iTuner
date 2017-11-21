//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Specialized;


	/// <summary>
	/// Declares the interface for Catalog providers.
	/// </summary>

	interface ICatalog : IDisposable
	{

		/// <summary>
		/// Gets a Boolean value indicating whether this provider is ready for use.
		/// </summary>
		/// <remarks>
		/// A provider is not ready to be used while it is loading.  But we allow it to be
		/// instantiated so consumers can depend on it, as long as they check IsReady
		/// before calling any other members.
		/// </remarks>

		bool IsReady { get; }


		/// <summary>
		/// Gets the path of the user's music folder.
		/// </summary>
		/// <remarks>
		/// This is directory where the user stores music files, which is typically different
		/// than where the "iTunes Music Library.xml" file is stored.
		/// </remarks>

		string MusicPath { get; }


		/// <summary>
		/// Adds a new playlist or updates the name of an existing playlist in the catalog.
		/// </summary>
		/// <param name="playlist"></param>
		/// <param name="pid"></param>

		void AddPlaylist (Playlist playlist);

			
		/// <summary>
		/// When a file is manually added to the music library directory tree, this will
		/// add a new track to the catalog in both the &lt;tracks&gt; node and the main
		/// library playlist.
		/// </summary>
		/// <param name="track">The IITFileOrCDTrack to add.</param>
		/// <param name="trackPID"></param>

		void AddTrack (Track track);


		/// <summary>
		/// Adds one or more tracks to the specified playlist.
		/// </summary>
		/// <param name="playlistPID"></param>
		/// <param name="trackPIDs"></param>

		void AddTracksToPlaylist (PersistentIDCollection trackPIDs, PersistentID playlistPID);

			
		/// <summary>
		/// When a file is manually deleted outside of iTunes, this will update the catalog,
		/// removing all references to that file including tracks in all playlists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>
		/// The persistent ID of the affected track or PersistentID.Empty if not found
		/// </returns>

		PersistentID DeleteFile (string path);


		/// <summary>
		/// Reloads the specified playlist from the Library XML file.
		/// </summary>
		/// <param name="playlistPID">The persistent ID of the playlist to refresh.</param>

		void RefreshPlaylist (PersistentID playlistPID);


		/// <summary>
		/// Verifies playlists in the catalog against a list of known existing playlists.
		/// </summary>
		/// <param name="playlistPIDs">
		/// A collection of existing playlist persistent IDs; any playlist not referenced
		/// in the list will be removed from the catalog.
		/// </param>

		void RefreshPlaylists (PersistentIDCollection playlistPIDs);


		/// <summary>
		/// When a file is manually renamed outside of iTunes, this will update the catalog,
		/// updating the Location of the affected track.
		/// </summary>
		/// <param name="path"></param>

		void RenameFile (string path, string newPath);


		/// <summary>
		/// Retrieves a list of all musical file extensions found in the given playlist.
		/// </summary>
		/// <param name="playlistID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		StringCollection FindExtensionsByPlaylist (PersistentID playlistID);


		/// <summary>
		/// Find the name of the playlist specified by its persistent ID.
		/// </summary>
		/// <param name="playlistID">The unique persisent ID of the playlist to find.</param>
		/// <returns></returns>

		string FindPlaylistName (PersistentID playlistID);


		/// <summary>
		/// Retrieves the track specified.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <param name="title">The name of the track.</param>
		/// <returns></returns>

		PersistentID FindTrack (string album, string artist, string title);


		/// <summary>
		/// Retrieves a list of all tracks by the specivied artist on the named album.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <returns></returns>

		PersistentIDCollection FindTracksByAlbum (string album, string artist);


		/// <summary>
		/// Retrieves a list of all tracks by the specified artist.
		/// </summary>
		/// <param name="artist">The name of the artist.</param>
		/// <returns></returns>

		PersistentIDCollection FindTracksByArtist (string artist);


		/// <summary>
		/// Retrieves a list of all tracks found in the given playlist.
		/// </summary>
		/// <param name="playlistID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		PersistentIDCollection FindTracksByPlaylist (PersistentID playlistID);


		/// <summary>
		/// Used during a rename operation to lookup the persistent ID of a track with a
		/// specific location.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		PersistentID GetPersistentIDByLocation (string path);


		/// <summary>
		/// Initialize this instance and load the iTunes library file from the specified path.
		/// </summary>
		/// <param name="path">The full path of the "iTunes Music Library.xml" file.</param>		
		/// <returns>
		/// A Boolean value indicating whether the library file was successfully parsed. This
		/// can be used to determine whether we need to attempt to use a different provider.
		/// </returns>

		bool Initialize (string path);


		/// <summary>
		/// Determines if the specified file extension is a valid extension recognized by
		/// iTunes and available for import.
		/// </summary>
		/// <param name="ext">A file extension with the leading period.</param>
		/// <returns><b>True</b> if the extension is valid; <b>false</b> otherwise.</returns>

		bool IsValidExtension (string ext);
	}
}
