//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Xml.Linq;
	using Microsoft.Win32;


	/// <summary>
	/// Provides various iTunes library filtering capabilities.
	/// </summary>
	/// <remarks>
	/// It turns out, proven by some testing, that parsing the main library catalog XML is
	/// far faster than enumerating the iTunesAppClass collections.  So this class answers
	/// filtering queries by using this cached catalog rather than the itunes COM instance.
	/// </remarks>

	internal abstract class CatalogBase : ICatalog
	{
		protected XElement root;
		protected StringCollection extensions;
		protected string musicPath;

		protected bool isReady;
		private bool isDisposed;


		//========================================================================================
		// Constructor
		//========================================================================================

		public CatalogBase ()
		{
			this.root = null;
			this.extensions = null;

			this.isReady = false;
			this.isDisposed = false;
		}


		/// <summary>
		/// Initialize this instance and load the iTunes library file from the specified path.
		/// </summary>
		/// <param name="path">The full path of the "iTunes Music Library.xml" file.</param>		
		/// <returns>
		/// A Boolean value indicating whether the library file was successfully parsed. This
		/// can be used to determine whether we need to attempt to use a different provider.
		/// </returns>

		public abstract bool Initialize (string path);


		/// <summary>
		/// Dispose instance.
		/// </summary>

		public void Dispose ()
		{
			Dispose(true);
		}


		/// <summary>
		/// Ensures all resources a cleaned up in an orderly fashion.
		/// </summary>

		protected virtual void Dispose (bool disposing)
		{
			if (!isDisposed)
			{
				if (extensions != null)
				{
					extensions.Clear();
					extensions = null;
				}

				if (root != null)
				{
					root = null;
				}

				isDisposed = true;
			}
		}


		//========================================================================================
		// Propeties
		//========================================================================================

		/// <summary>
		/// Gets a Boolean value indicating whether this provider is ready for use.
		/// </summary>
		/// <remarks>
		/// A provider is not ready to be used while it is loading.  But we allow it to be
		/// instantiated so consumers can depend on it, as long as they check IsReady
		/// before calling any other members.
		/// </remarks>

		public bool IsReady
		{
			get { return isReady; }
		}


		/// <summary>
		/// Gets the path of the user's music folder.
		/// </summary>
		/// <remarks>
		/// This is directory where the user stores music files, which is typically different
		/// than where the "iTunes Music Library.xml" file is stored.
		/// </remarks>

		public string MusicPath
		{
			get
			{
				if (musicPath == null)
				{
					musicPath = FindMusicFolderPath();
					if (musicPath.Contains("%"))
					{
						musicPath = Tools.HttpUtility.UrlDecode(FindMusicFolderPath());
					}
				}

				return musicPath;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Adds a new playlist or updates the name of an existing playlist in the catalog.
		/// </summary>
		/// <param name="playlist"></param>

		public abstract void AddPlaylist (Playlist playlist);

	
		/// <summary>
		/// When a file is manually added to the music library directory tree, this will
		/// add a new track to the catalog in both the &lt;tracks&gt; node and the main
		/// library playlist.
		/// </summary>
		/// <param name="track">The IITFileOrCDTrack to add.</param>

		public abstract void AddTrack (Track track);


		/// <summary>
		/// Adds one or more tracks to the specified playlist.
		/// </summary>
		/// <param name="playlistPID"></param>
		/// <param name="trackPIDs"></param>

		public abstract void AddTracksToPlaylist (
			PersistentIDCollection trackPIDs, PersistentID playlistPID);
	
		
		/// <summary>
		/// When a file is manually deleted outside of iTunes, this will update the catalog,
		/// removing all references to that file including tracks in all playlists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>
		/// The persistent ID of the affected track or PersistentID.Empty if not found
		/// </returns>

		public abstract PersistentID DeleteFile (string path);


		/// <summary>
		/// When a file is manually renamed outside of iTunes, this will update the catalog,
		/// updating the Location of the affected track.
		/// </summary>
		/// <param name="path"></param>

		public abstract void RenameFile (string path, string newPath);


		/// <summary>
		/// Retrieves a list of all musical file extensions found in the given playlist.
		/// </summary>
		/// <param name="playlistID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		public abstract StringCollection FindExtensionsByPlaylist (PersistentID playlistID);


		/// <summary>
		/// Find the name of the playlist specified by its persistent ID.
		/// </summary>
		/// <param name="playlistID">The unique persisent ID of the playlist to find.</param>
		/// <returns></returns>

		public abstract string FindPlaylistName (PersistentID playlistID);

	
		/// <summary>
		/// Retrieves the track specified.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <param name="title">The name of the track.</param>
		/// <returns></returns>

		public abstract PersistentID FindTrack (string album, string artist, string title);
	

		/// <summary>
		/// Retrieves a list of all tracks by the specivied artist on the named album.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <returns></returns>

		public abstract PersistentIDCollection FindTracksByAlbum (string album, string artist);


		/// <summary>
		/// Retrieves a list of all tracks by the specified artist.
		/// </summary>
		/// <param name="artist">The name of the artist.</param>
		/// <returns></returns>

		public abstract PersistentIDCollection FindTracksByArtist (string artist);


		/// <summary>
		/// Retrieves a list of all tracks found in the given playlist.
		/// </summary>
		/// <param name="playlistID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		public abstract PersistentIDCollection FindTracksByPlaylist (PersistentID playlistID);


		/// <summary>
		/// Used during a rename operation to lookup the persistent ID of a track with a
		/// specific location.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public abstract PersistentID GetPersistentIDByLocation (string path);

	
		/// <summary>
		/// Determines if the specified file extension is a valid extension recognized by
		/// iTunes and available for import.
		/// </summary>
		/// <param name="ext">A file extension with the leading period.</param>
		/// <returns><b>True</b> if the extension is valid; <b>false</b> otherwise.</returns>

		public bool IsValidExtension (string ext)
		{
			if (isReady)
			{
				if (extensions == null)
				{
					extensions = LoadExtensions();
				}

				return extensions.Contains(ext);
			}

			return false;
		}


		/// <summary>
		/// Reloads the specified playlist from the Library XML file.
		/// </summary>
		/// <param name="playlistPID">The persistent ID of the playlist to refresh.</param>

		public abstract void RefreshPlaylist (PersistentID playlistPID);


		/// <summary>
		/// Verifies playlists in the catalog against a list of known existing playlists.
		/// </summary>
		/// <param name="playlistPIDs">
		/// A collection of existing playlist persistent IDs; any playlist not referenced
		/// in the list will be removed from the catalog.
		/// </param>

		public abstract void RefreshPlaylists (PersistentIDCollection playlistPIDs);


		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		// Protected methods
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

		/// <summary>
		/// Add some well-known extensions: those supposedly supported by iTunes
		/// </summary>
		/// <param name="extensions"></param>

		protected void AddKnownExtensions (StringCollection extensions)
		{
			// TODO: are aifc and aiff real?

			string[] knowns = new string[] {
				".aif", ".aifc", ".aiff", ".cda",
				".m4a", ".m4b", ".m4p", ".mp2", ".mp3", ".wav" };

			foreach (string known in knowns)
			{
				if (!extensions.Contains(known))
				{
					extensions.Add(known);
				}
			}
		}


		/// <summary>
		/// Scan the entire library and extract all unique media file extensions, returning
		/// only extensions mapping to system-registered audio types.
		/// </summary>
		/// <returns>
		/// A collection of file extensions, where each extension is of the form ".xxx"
		/// </returns>

		protected abstract StringCollection LoadExtensions ();



		/// <summary>
		/// Extract and return a list of extensions perceived as "audio" types.
		/// </summary>
		/// <param name="list">A collection of file extensions with preceeding periods.</param>
		/// <returns></returns>

		protected StringCollection FilterMusicalExtensions (IEnumerable<string> list)
		{
			StringCollection extensions = new StringCollection();

			foreach (string ext in list)
			{
				if (!String.IsNullOrEmpty(ext))
				{
					if (!extensions.Contains(ext))
					{
						// key might be null for foreign extension such as "web playlists"
						RegistryKey key = Registry.ClassesRoot.OpenSubKey(ext);
						if (key != null)
						{
							string perceived = key.GetValue("PerceivedType", "") as String;

							if (!String.IsNullOrEmpty(perceived) && perceived.Equals("audio"))
							{
								extensions.Add(ext);
							}
						}
					}
				}
			}

			return extensions;
		}


		/// <summary>
		/// Read the Music Folder entry from the library.  This is directory where
		/// the user stores music files, which is typically different than where the 
		/// "iTunes Music Library.xml" file is stored.
		/// </summary>
		/// <returns>A string specifying the absolute path of the music root folder.</returns>

		protected abstract string FindMusicFolderPath ();
	}
}
