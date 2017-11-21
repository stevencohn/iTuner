//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Specialized;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml;
	using System.Xml.Linq;

	/*
	 * NOTE THIS CATALOG IS BEING PHASED-OUT AND WILL BE REPLACED BY TerseCatalog ONCE
	 * PROVEN ENTIRELY STABLE AND SUPPORTIVE ON ALL INSTALLATIONS...
	 * 
	 */

	/// <summary>
	/// This ICatalog manages and queries the "iTunes Music Library.xml" file
	/// in its original flattened form.
	/// </summary>

	internal class FlatCatalog : CatalogBase
	{
		private XNamespace ns;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize an empty instance.  This is used during the iTuner initialization
		/// phase to simplify context menu popup preparation and other activities that require
		/// the existence of a catalog.
		/// </summary>

		public FlatCatalog ()
			: base()
		{
		}


		/// <summary>
		/// Initialize this instance and load the iTunes library file from the specified path.
		/// </summary>
		/// <param name="path">The full path of the "iTunes Music Library.xml" file.</param>		
		/// <returns>
		/// A Boolean value indicating whether the library file was successfully parsed. This
		/// can be used to determine whether we need to attempt to use a different provider.
		/// </returns>

		public override bool Initialize (string path)
		{
			try
			{
				using (Stream stream = File.Open(
					path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					XmlReaderSettings settings = new XmlReaderSettings();
					settings.IgnoreComments = true;
					settings.IgnoreProcessingInstructions = true;
					settings.IgnoreWhitespace = true;
					settings.DtdProcessing = DtdProcessing.Ignore;

					using (XmlReader reader = XmlReader.Create(stream, settings))
					{
						root = XElement.Load(reader, LoadOptions.None);
						ns = root.GetDefaultNamespace();
					}

					isReady = true;
				}
			}
			catch (Exception exc)
			{
				Logger.WriteLine("FlatCatalog", exc);
				return false;
			}

			return true;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		#region Queries

		/// <summary>
		/// Retrieves a list of all musical file extensions found in the given playlist.
		/// </summary>
		/// <param name="playlistID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		public override StringCollection FindExtensionsByPlaylist (PersistentID playlistID)
		{
			if (!isReady)
			{
				return new StringCollection();
			}

			// find the <plist><dict><key>Playlists</key><array> root node
			var playlistRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Playlists"
				select node.NextNode;

			// find the parent <array><dict> node of the named playlist
			// <array><dict><key>Name</key><string>Library</string>
			var playlistNodes =
				from node in ((XElement)playlistRoot.Single())
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Playlist Persistent ID"
					&& ((XElement)node.NextNode).Value == (string)playlistID
				select node.Parent;

			// collect all Track ID values from this playlist
			var trackIDs =
				from node in ((XElement)playlistNodes.Single())
					.Elements(ns + "array")
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Track ID"
				select node.NextNode;

			// find the <plist><dict><key>Tracks</key><dict> root node
			var trackRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Tracks"
				select node.NextNode;

			// join tracks on trackID to extract the distinct extensions
			var extensions =
				from node in
					(from node in ((XElement)trackRoot.Single()).Elements(ns + "key")
					 join id in trackIDs on ((XElement)node).Value equals ((XElement)id).Value
					 select ((XElement)node.NextNode)
					).Elements(ns + "key")
				where ((XElement)node).Value == "Location"
				select Path.GetExtension(((XElement)node.NextNode).Value).ToLower();

			StringCollection list = null;
			
			if (extensions != null)
			{
				list = FilterMusicalExtensions(extensions.Distinct());
			}

			return list;
		}


		/// <summary>
		/// Find the name of the playlist specified by its persistent ID.
		/// </summary>
		/// <param name="playlistID">The unique persisent ID of the playlist to find.</param>
		/// <returns></returns>

		public override string FindPlaylistName (PersistentID playlistID)
		{
			if (!isReady)
			{
				return null;
			}

			// find the <plist><dict><key>Playlists</key><array> root node
			var playlistRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Playlists"
				select node.NextNode;

			// find the parent <array><dict> node of the named playlist
			// <array><dict><key>Name</key><string>Library</string>
			var playlistNodes =
				from node in ((XElement)playlistRoot.Single())
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Playlist Persistent ID"
					&& ((XElement)node.NextNode).Value == (string)playlistID
				select node.Parent;

			if (playlistNodes == null)
			{
				return null;
			}

			string name =
				(from node in ((XElement)playlistNodes.Single())
					.Elements(ns + "key")
				 where node.Value == "Name"
				 select ((XElement)node.NextNode).Value).FirstOrDefault() as String;

			return name;
		}

	
		/// <summary>
		/// Retrieves the track specified.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <param name="title">The name of the track.</param>
		/// <returns></returns>

		public override PersistentID FindTrack (string album, string artist, string title)
		{
			throw new NotImplementedException();
		}
		
		
		/// <summary>
		/// Retrieves a list of all tracks by the specivied artist on the named album.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <returns></returns>

		public override PersistentIDCollection FindTracksByAlbum (string album, string artist)
		{
			if (!isReady)
			{
				return new PersistentIDCollection();
			}

			album = album.Trim().ToLower();
			artist = artist.Trim().ToLower();

			// find the <plist><dict><key>Tracks</key><dict> root node
			var trackRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Tracks"
				select node.NextNode;

			// Tracks/dict is the container for all tracks where each is a key/dict pair
			// collect all dict elements related to specified album

			var albumTracks =
				from node in
					(from node in ((XElement)trackRoot.Single())
						.Elements(ns + "dict")
						.Elements(ns + "key")
					 where node.Value == "Album"
						 && ((XElement)node.NextNode).Value.Trim().ToLower() == album
					 select node.Parent
					).Elements(ns + "key")
				where node.Value == "Artist"
					&& ((XElement)node.NextNode).Value.Trim().ToLower() == artist
				select node.Parent;

			// collect all persistent IDs from these dict elements
			var tracks =
				from node in albumTracks.Elements(ns + "key")
				where node.Value == "Persistent ID"
				select PersistentID.Parse(((XElement)node.NextNode).Value);

			PersistentIDCollection list = new PersistentIDCollection(tracks.ToList<PersistentID>());

			return list;
		}


		/// <summary>
		/// Retrieves a list of all tracks by the specified artist.
		/// </summary>
		/// <param name="artist">The name of the artist.</param>
		/// <returns></returns>

		public override PersistentIDCollection FindTracksByArtist (string artist)
		{
			if (!isReady)
			{
				return new PersistentIDCollection();
			}

			artist = artist.Trim().ToLower();

			// find the <plist><dict><key>Tracks</key><dict> root node
			var trackRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Tracks"
				select node.NextNode;

			// Tracks/dict is the container for all tracks where each is a key/dict pair
			// collect all dict elements related to specified artist

			var artistTracks =
				from node in ((XElement)trackRoot.Single())
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where (node.Value == "Artist")
					//|| node.Value == "Album Artist"
					//|| node.Value == "Composer")
					&& ((XElement)node.NextNode).Value.Trim().ToLower() == artist
				select node.Parent;

			// collect all persistent IDs from these dict elements
			var tracks =
				from node in artistTracks.Elements(ns + "key")
				where node.Value == "Persistent ID"
				select PersistentID.Parse(((XElement)node.NextNode).Value);

			PersistentIDCollection list = new PersistentIDCollection(tracks.ToList<PersistentID>());

			return list;
		}


		/// <summary>
		/// Retrieves a list of all tracks found in the given playlist.
		/// </summary>
		/// <param name="playlistID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		public override PersistentIDCollection FindTracksByPlaylist (PersistentID playlistID)
		{
			if (!isReady)
			{
				return new PersistentIDCollection();
			}

			// find the <plist><dict><key>Playlists</key><array> root node
			var playlistRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Playlists"
				select node.NextNode;

			// find the parent <array><dict> node of the named playlist
			// <array><dict><key>Name</key><string>Library</string>
			var playlistNodes =
				from node in ((XElement)playlistRoot.Single())
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Playlist Persistent ID"
					&& ((XElement)node.NextNode).Value == (string)playlistID
				select node.Parent;

			// collect all Track ID values from this playlist
			var trackIDs =
				from node in ((XElement)playlistNodes.Single())
					.Elements(ns + "array")
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Track ID"
				select node.NextNode;

			// find the <plist><dict><key>Tracks</key><dict> root node
			var trackRoot =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Tracks"
				select node.NextNode;

			// join tracks on trackID to extract the persistent IDs
			var tracks =
				from node in
					(from node in ((XElement)trackRoot.Single()).Elements(ns + "key")
					 join id in trackIDs on ((XElement)node).Value equals ((XElement)id).Value
					 select ((XElement)node.NextNode)
					).Elements(ns + "key")
				where ((XElement)node).Value == "Persistent ID"
				select PersistentID.Parse(((XElement)node.NextNode).Value);

			PersistentIDCollection list = new PersistentIDCollection(tracks.ToList<PersistentID>());
			return list;
		}


		/// <summary>
		/// Used during a rename operation to lookup the persistent ID of a track with a
		/// specific location.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public override PersistentID GetPersistentIDByLocation (string path)
		{
			return PersistentID.Empty;
		}

		#endregion Queries

		#region FileWatch Changes

		/// <summary>
		/// When a file is manually added to the music library directory tree, this will
		/// add a new track to the catalog in both the &lt;tracks&gt; node and the main
		/// library playlist.
		/// </summary>
		/// <param name="track">The IITFileOrCDTrack to add.</param>

		public override void AddTrack (Track track)
		{
		}


		/// <summary>
		/// When a file is manually deleted outside of iTunes, this will update the catalog,
		/// removing all references to that file including tracks in all playlists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>
		/// The persistent ID of the affected track or PersistentID.Empty if not found
		/// </returns>

		public override PersistentID DeleteFile (string path)
		{
			return PersistentID.Empty;
		}


		/// <summary>
		/// When a file is manually renamed outside of iTunes, this will update the catalog,
		/// updating the Location of the affected track.
		/// </summary>
		/// <param name="path"></param>

		public override void RenameFile (string path, string newPath)
		{
		}

		#endregion FileWatch Changes

		#region Database Maintenance

		/// <summary>
		/// Adds a new playlist or updates the name of an existing playlist in the catalog.
		/// </summary>
		/// <param name="playlist"></param>

		public override void AddPlaylist (Playlist playlist)
		{
		}


		/// <summary>
		/// Adds a track entry to the specified playlist.
		/// </summary>
		/// <param name="playlistPID"></param>
		/// <param name="trackPIDs"></param>

		public override void AddTracksToPlaylist (
			PersistentIDCollection trackPIDs, PersistentID playlistPID)
		{
		}


		/// <summary>
		/// Reloads the specified playlist from the Library XML file.
		/// </summary>
		/// <param name="playlistPID">The persistent ID of the playlist to refresh.</param>

		public override void RefreshPlaylist (PersistentID playlistPID)
		{
		}


		/// <summary>
		/// Verifies playlists in the catalog against a list of known existing playlists.
		/// </summary>
		/// <param name="playlistPIDs">
		/// A collection of existing playlist persistent IDs; any playlist not referenced
		/// in the list will be removed from the catalog.
		/// </param>

		public override void RefreshPlaylists (PersistentIDCollection playlistPIDs)
		{
		}

		#endregion Database Maintenance


		//========================================================================================
		// Helpers
		//========================================================================================

		/// <summary>
		/// Scan the entire library and extract all unique media file extensions, returning
		/// only extensions mapping to system-registered audio types.
		/// </summary>
		/// <returns>
		/// A collection of file extensions, where each extension is of the form ".xxx"
		/// </returns>

		protected override StringCollection LoadExtensions ()
		{
			StringCollection extensions = new StringCollection();

			// dive down to <key>Tracks</key>

			var tracks =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Tracks"
				select node.NextNode;

			// Tracks/dict is the container for all tracks where each is a key/dict pair
			// Extract all Location extension values into an IEnumerable<string>

			var extlist =
				from node in ((XElement)tracks.Single())
					.Elements(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Location"
				select Path.GetExtension(((XElement)node.NextNode).Value).ToLower();

			if (extlist != null)
			{
				StringCollection list = FilterMusicalExtensions(extlist.Distinct());
				foreach (string ext in list)
				{
					extensions.Add(ext);
				}

				list.Clear();
				list = null;
			}

			extlist = null;
			tracks = null;

			// add some well-known extensions if missing
			base.AddKnownExtensions(extensions);

			return extensions;
		}


		/// <summary>
		/// Read the Music Folder entry from the library.  This is directory where
		/// the user stores music files, which is typically different than where the 
		/// "iTunes Music Library.xml" file is stored.
		/// </summary>
		/// <returns>A string specifying the absolute path of the music root folder.</returns>

		protected override string FindMusicFolderPath ()
		{
			// Library is a key-value dictionary so find the key then next node will be value...
			// so dive down to <plist/dict/key>Music Folder</key> and read next <string>

			var folder =
				from node in root
					.Element(ns + "dict")
					.Elements(ns + "key")
				where node.Value == "Music Folder"
				select node.NextNode;					// NextNode should be <string>

			Uri uri = new Uri(((XElement)folder.Single()).Value);

			string path = PathHelper.GetLocalPath(uri);
			return path;
		}
	}
}
