//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml;
	using System.Xml.Linq;
	using Resx = Properties.Resources;

	/*
		<dict>
			<key>Name</key><string>untitled folder</string>
			<key>Playlist ID</key><integer>41164</integer>
			<key>Playlist Persistent ID</key><string>89B3C4E413329348</string>
			<key>All Items</key><true/>
			<key>Folder</key><true/>
		</dict>
		<dict>
			<key>Name</key><string>untitled playlist</string>
			<key>Playlist ID</key><integer>41175</integer>
			<key>Playlist Persistent ID</key><string>EA2D8D62ADC60359</string>
			<key>Parent Persistent ID</key><string>89B3C4E413329348</string>
			<key>All Items</key><true/>
		</dict>
	 */

	/// <summary>
	/// This ICatalog manages and queries the "iTunes Music Library.xml" file by converting
	/// it to a <i>terse XML format</i> from its original iTunes flattened form.
	/// </summary>

	internal class TerseCatalog : CatalogBase
	{
		private class KeyMap : Dictionary<string, string> { }

		private const string LogCategory = "TerseCatalog";

		private KeyMap map;				// terse translation map
		private KeyMap idmap;			// transient trackID/persistentID lookup
		private string path;			// full path of iTunes Music Library.xml file
		private DateTime writeTime;		// timestamp of file while currently reading
		private bool isDisposed;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize an empty instance.  This is used during the iTuner initialization
		/// phase to simplify context menu popup preparation and other activities that require
		/// the existence of a catalog.
		/// </summary>

		public TerseCatalog ()
			: base()
		{
			this.map = null;
			this.idmap = null;
			this.path = null;
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

		public override bool Initialize (string path)
		{
			// initialize these member variables before testing null path so we
			// can still run disconnected...
			root = new XElement("library");
			map = new KeyMap();
			idmap = new KeyMap();

			if (!File.Exists(path))
			{
				return false;
			}

			FileInfo info = new FileInfo(path);
			writeTime = info.LastWriteTime;
			info = null;

			this.path = path;

			try
			{
				using (Stream stream = File.Open(
					path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					XmlReaderSettings settings = new XmlReaderSettings();
					settings.IgnoreComments = true;
					settings.IgnoreProcessingInstructions = true;
					settings.IgnoreWhitespace = true;

					// ProhibitDtd and XmlResolver set to completely ignore the DTD element
					// of the library XML file; otherwise, we get an exception when the
					// network adapter is in a suspicious state
					settings.DtdProcessing = DtdProcessing.Ignore;
					settings.XmlResolver = null;

					using (XmlReader reader = XmlReader.Create(stream, settings))
					{
						// read into //plist/dict and stop at the first ./key
						// this is the library level key/value dictionary

						if (reader.ReadToDescendant("key"))
						{
							root.Add(LoadKeys(reader, "Tracks"));

							if (reader.Name.Equals("dict"))
							{
								// move to first key node of ./Tracks/dict
								reader.ReadToDescendant("key");

								XElement tracks = new XElement("tracks");
								tracks.Add(LoadTracks(reader));
								root.Add(tracks);

								// read </dict>
								reader.ReadEndElement();
							}

							// currently at <key>Playlists</key>
							// now move forward to <array> containing list of playlists

							if (reader.ReadToNextSibling("array"))
							{
								reader.ReadToDescendant("dict");

								XElement playlists = new XElement("playlists");
								playlists.Add(LoadPlaylists(reader));
								root.Add(playlists);
							}
						}
					}

					isReady = true;
				}
			}
			catch (Exception exc)
			{
				Logger.WriteLine(LogCategory, exc);
				return false;
			}
			finally
			{
				idmap.Clear();
				idmap = null;
			}

			return true;
		}


		#region Schema

		/*
		 * <plist>
		 *   <dict>
		 *     <key>meta</key><datatype>data</datatype>
		 *     <key>meta</key><datatype>data</datatype>
		 *     
		 *     <key>Tracks</key>
		 *     <dict>
		 *       <key>trackid</key>
		 *       <dict>
		 *         <key>meta</key><datatype>data</datatype>
		 *         <key>meta</key><datatype>data</datatype>
		 *       </dict>
		 *     </dict>
		 *     
		 *     <key>Playlists</key>
		 *     <array>
		 *       <dict>
		 *         <key>meta</key><datatype>data</datatype>
		 *         <key>meta</key><datatype>data</datatype>
		 *         <key>Playlist Items</key>
		 *         <array>
		 *           <dict>
		 *             <key>Track ID</key><integer>123123</integer>
		 *           </dict>
		 *           <dict>
		 *             <key>Track ID</key><integer>123123</integer>
		 *           </dict>
		 *         </array>
		 *       </dict>
		 *     </array>
		 *   </dict>
		 * </plist>
		 */

		/*
		 * <track TrID="3572" Na="Baby Please Don't Leave Me" Ar="Buddy Guy" AlAr="Buddy Guy"
		 *		  Co="Junior Kimbrough" Al="Sweet Tea" Ge="Blues" Ki="Purchased AAC audio file"
		 *		  Si="15064370" ToTi="442853" DiNu="1" DiCo="1" TrNu="2" TrCo="9" Ye="1998"
		 *		  DaMo="2010-02-02T21:43:56Z" DaAd="2010-02-15T23:09:42Z" BiRa="256"
		 *		  SaRa="44100" PlCo="4" PlDa="3351700522" PlDaUT="2010-03-17T23:55:22Z"
		 *		  ReDa="1998-12-30T08:00:00Z" Ra="100" AlRa="80" AlRaCo="true" ArCo="1"
		 *		  PeID="63D61540C7ED96F2" TrTy="File" Pu="true"
		 *		  Lo="file://localhost/C:/Music/Buddy%20Guy/Sweet%20Tea/02%20Baby%20Please%20Don't%20Leave%20Me.m4a"
		 *		  FiFoCo="4" LiFoCo="1" />
		 *
		 * <playlist Na="ituner" PlID="36618" PlPeID="612F0BCDC4E08C4E" AlIt="true">
		 */

		#endregion Schema

		#region Map
		/*
		map
		Count = 79
		[0]: {[Major Version, MaVe]}
		[1]: {[Minor Version, MiVe]}
		[2]: {[Application Version, ApVe]}
		[3]: {[Features, Fe]}
		[4]: {[Show Content Ratings, ShCoRa]}
		[5]: {[Music Folder, MuFo]}
		[6]: {[Library Persistent ID, LiPeID]}
		[7]: {[Track ID, TrID]}
		[8]: {[Name, Na]}
		[9]: {[Artist, Ar]}
		[10]: {[Album, Al]}
		[11]: {[Genre, Ge]}
		[12]: {[Kind, Ki]}
		[13]: {[Size, Si]}
		[14]: {[Total Time, ToTi]}
		[15]: {[Disc Number, DiNu]}
		[16]: {[Date Modified, DaMo]}
		[17]: {[Date Added, DaAd]}
		[18]: {[Bit Rate, BiRa]}
		[19]: {[Artwork Count, ArCo]}
		[20]: {[Persistent ID, PeID]}
		[21]: {[Track Type, TrTy]}
		[22]: {[Has Video, HaVi]}
		[23]: {[HD, HD]}
		[24]: {[Video Width, ViWi]}
		[25]: {[Video Height, ViHe]}
		[26]: {[Movie, Mo]}
		[27]: {[Location, Lo]}
		[28]: {[File Folder Count, FiFoCo]}
		[29]: {[Library Folder Count, LiFoCo]}
		[30]: {[Year, Ye]}
		[31]: {[Play Count, PlCo]}
		[32]: {[Play Date, PlDa]}
		[33]: {[Play Date UTC, PlDaUT]}
		[34]: {[Album Artist, AlAr]}
		[35]: {[Disc Count, DiCo]}
		[36]: {[Track Number, TrNu]}
		[37]: {[Release Date, ReDa]}
		[38]: {[Series, Se]}
		[39]: {[Season, Se1]}
		[40]: {[Episode, Ep]}
		[41]: {[Episode Order, EpOr]}
		[42]: {[Sort Album, SoAl]}
		[43]: {[Sort Album Artist, SoAlAr]}
		[44]: {[Sort Artist, SoAr]}
		[45]: {[Sort Series, SoSe]}
		[46]: {[Content Rating, CoRa]}
		[47]: {[Protected, Pr]}
		[48]: {[Purchased, Pu]}
		[49]: {[TV Show, TVSh]}
		[50]: {[Sort Name, SoNa]}
		[51]: {[Track Count, TrCo]}
		[52]: {[Podcast, Po]}
		[53]: {[Composer, Co]}
		[54]: {[Sample Rate, SaRa]}
		[55]: {[Comments, Co1]}
		[56]: {[Skip Count, SkCo]}
		[57]: {[Skip Date, SkDa]}
		[58]: {[Music Video, MuVi]}
		[59]: {[Rating, Ra]}
		[60]: {[Album Rating, AlRa]}
		[61]: {[Album Rating Computed, AlRaCo]}
		[62]: {[Sort Composer, SoCo]}
		[63]: {[Compilation, Co2]}
		[64]: {[BPM, BP]}
		[65]: {[Clean, Cl]}
		[66]: {[Explicit, Ex]}
		[67]: {[Master, Ma]}
		[68]: {[Playlist ID, PlID]}
		[69]: {[Playlist Persistent ID, PlPeID]}
		[70]: {[Visible, Vi]}
		[71]: {[All Items, AlIt]}
		[72]: {[Distinguished Kind, DiKi]}
		[73]: {[Music, Mu]}
		[74]: {[Movies, Mo1]}
		[75]: {[TV Shows, TVSh1]}
		[76]: {[Podcasts, Po1]}
		[77]: {[Purchased Music, PuMu]}
		[78]: {[Folder, Fo]}
		*/
		#endregion Map

		#region Loading

		private IEnumerable<XElement> LoadTracks (XmlReader reader)
		{
			while (reader.ReadToNextSibling("dict"))
			{
				reader.ReadToDescendant("key");

				XElement track = new XElement("track");
				track.Add(LoadKeys(reader, null));

				// after some testing, it turns out that building up this temporary ID map is
				// orders of magnitude faster than trying to query the <tracks> node dynamically
				// when we start building the playlists in LoadPlaylistTracks...

				idmap.Add(
					track.Attribute(map["Track ID"]).Value,
					track.Attribute(map["Persistent ID"]).Value);

				yield return track;
			}
		}


		private IEnumerable<XElement> LoadPlaylists (XmlReader reader)
		{
			bool hasTracks;

			do
			{
				hasTracks = false;
				reader.ReadToDescendant("key");
				XElement playlist = new XElement("playlist");
				playlist.Add(LoadKeys(reader, "Playlist Items"));

				if (reader.Name.Equals("array"))
				{
					if (reader.ReadToDescendant("dict"))
					{
						hasTracks = true;
						playlist.Add(LoadPlaylistTracks(reader));
					}
				}

				yield return playlist;

				// if this playlist does not have tracks, we cannot read next end
				// element or we'll skip the next playlist in sequence...

				// [3/2/2012 update] it appears recent versions of iTunes uses the long form
				// of <array></array> instead of just <array /> which means we DO NEED TO read
				// the end element regardless...

				if (hasTracks)
				{
					// read </dict>
					reader.ReadEndElement();
				}
				else
				{
					// read </array>
					reader.ReadEndElement();
				}

			} while (reader.ReadToNextSibling("dict"));
		}


		private IEnumerable<XElement> LoadPlaylistTracks (XmlReader reader)
		{
			// we start at a ./dict node

			while (reader.ReadToDescendant("integer"))
			{
				string id = reader.ReadElementContentAsString();

				XElement track =
					new XElement("track",
						new XAttribute(map["Track ID"], id),
						new XAttribute(map["Persistent ID"], idmap[id]));

				yield return track;

				reader.ReadToNextSibling("dict");
			}
		}


		private IEnumerable<XAttribute> LoadKeys (XmlReader reader, string terminator)
		{
			// we start at a ./key node

			XAttribute attribute = null;

			bool more = true;
			while (more && (reader.NodeType == XmlNodeType.Element))
			{
				// read the ./key content, moving to the next element
				string keyName = reader.ReadElementContentAsString();

				// we don't care about these Smart elements; can't interpret them anyway!
				if (keyName.Equals("Smart Info") || keyName.Equals("Smart Criteria"))
				{
					reader.ReadToNextSibling("key");
				}
				else if (more = !keyName.Equals(terminator))
				{
					string key = MakeKey(keyName);

					// move to the following element, integer, string, or boolean-value

					if (reader.Name.Equals("true") || reader.Name.Equals("false"))
					{
						attribute = new XAttribute(key, reader.Name);
						reader.Skip();
						yield return attribute;
					}
					else
					{
						attribute = new XAttribute(key, reader.ReadElementContentAsString());
						yield return attribute;
					}

				}
			}
		}


		private string MakeKey (string name)
		{
			if (map.ContainsKey(name))
			{
				return map[name];
			}

			// create a new key by taking the first two characters from each word
			StringBuilder builder = new StringBuilder();
			string[] parts = name.Split(' ');
			foreach (string part in parts)
			{
				builder.Append(part.Substring(0, 2));
			}

			string key = builder.ToString();

			if (map.Values.Contains(key))
			{
				// found a duplicate, make it unique with a counter
				int count = 1;
				string newkey = key + count.ToString();
				while (map.Values.Contains(newkey))
				{
					count++;
					newkey = key + count.ToString();
				}

				key = newkey;
			}


			map.Add(name, key);

			return key;
		}

		#endregion Loading


		/// <summary>
		/// Dispose of this instance.
		/// </summary>

		protected override void Dispose (bool disposing)
		{
			if (!isDisposed)
			{
				if (map != null)
				{
					map.Clear();
					map = null;
				}

				if (idmap != null)
				{
					idmap.Clear();
					idmap = null;
				}

				this.isDisposed = false;
				base.Dispose(disposing);
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		#region Queries

		/// <summary>
		/// Retrieves a list of all musical file extensions found in the given playlist.
		/// </summary>
		/// <param name="playlistPID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		public override StringCollection FindExtensionsByPlaylist (PersistentID playlistPID)
		{
			if (!isReady)
			{
				return new StringCollection();
			}

			var attributes =
				from node in root
					.Elements("playlists")
					.Elements("playlist")
				where node.Attribute(map["Playlist Persistent ID"]).Value == (string)playlistPID
				select node.Elements("track").Attributes(map["Persistent ID"]);

			if ((attributes != null) && (attributes.Count() > 0))
			{
				// select all track IDs for the given playlist
				var trackPIDs =
					from attr in attributes.Single()
					select attr.Value;

				if (trackPIDs != null)
				{
					if (trackPIDs.Count() > 0)
					{
						string locmap = map["Location"];

						// select all track Locations from tracks
						var extensions =
							from pid in trackPIDs
							join track in root
								.Elements("tracks")
								.Elements("track")
								.Attributes(map["Persistent ID"])

							// TODO: !! There's an exception here...
		
							on pid equals track.Value
							let loc = track.Parent.Attributes(locmap)
							where (track.Parent != null) &&
								  (loc != null) &&
								  (loc.Count() > 0) &&
								  (loc.First().Value != null)
							select Path.GetExtension(
								track.Parent.Attributes(locmap).First().Value.ToLower());

						if (extensions != null)
						{
							StringCollection list = FilterMusicalExtensions(extensions.Distinct());
							return list;
						}
					}
				}
			}

			return new StringCollection();
		}


		/// <summary>
		/// Find the name of the playlist specified by its persistent ID.
		/// </summary>
		/// <param name="playlistPID">The unique persisent ID of the playlist to find.</param>
		/// <returns></returns>

		public override string FindPlaylistName (PersistentID playlistPID)
		{
			if (!isReady)
			{
				return null;
			}

			string name =
				(from attr in root
					.Elements("playlists")
					.Elements("playlist")
					.Attributes(map["Playlist Persistent ID"])
				 where attr.Value == (string)playlistPID
				 select attr.Parent.Attributes(map["Name"]).Single().Value).Single() as String;

			return name;
		}


		/// <summary>
		/// Retrieves the best possible match for the track specified.
		/// </summary>
		/// <param name="album">The name of the album.</param>
		/// <param name="artist">The name of the artist.</param>
		/// <param name="title">The name of the track.</param>
		/// <returns></returns>

		public override PersistentID FindTrack (string album, string artist, string title)
		{
			if (!isReady)
			{
				return PersistentID.Empty;
			}

			album = album == null ? String.Empty : album.Trim().ToLower();
			artist = artist == null ? String.Empty : artist.Trim().ToLower();
			title = title == null ? String.Empty : title.Trim().ToLower();

			// try artist, album, title
			if (!
				(String.IsNullOrEmpty(artist) ||
				String.IsNullOrEmpty(album) ||
				String.IsNullOrEmpty(title)))
			{
				var tracks =
					from t in root
						.Elements("tracks")
						.Elements("track")
					let aa = t.Attribute(map["Album Artist"])
					let ar = t.Attribute(map["Artist"])
					let al = t.Attribute(map["Album"])
					let na = t.Attribute(map["Name"])
					where (((aa != null) && aa.Value.Trim().ToLower().Equivalent(artist))
						|| ((ar != null) && ar.Value.Trim().ToLower().Equivalent(artist)))
						&& ((al != null) && al.Value.Trim().ToLower().Equals(album))
						&& ((na != null) && na.Value.Trim().ToLower().Equivalent(title))
					select PersistentID.Parse(t.Attribute(map["Persistent ID"]).Value);

				if ((tracks != null) && (tracks.Count() > 0))
				{
					return tracks.First();
				}
			}

			// try artist, title
			if (!(
				String.IsNullOrEmpty(artist) ||
				String.IsNullOrEmpty(title)))
			{
				var tracks =
					from t in root
						.Elements("tracks")
						.Elements("track")
					let aa = t.Attribute(map["Album Artist"])
					let ar = t.Attribute(map["Artist"])
					let na = t.Attribute(map["Name"])
					where (((aa != null) && aa.Value.Trim().ToLower().Equivalent(artist))
						|| ((ar != null) && ar.Value.Trim().ToLower().Equivalent(artist)))
						&& ((na != null) && na.Value.Trim().ToLower().Equivalent(title))
					select PersistentID.Parse(t.Attribute(map["Persistent ID"]).Value);

				if ((tracks != null) && (tracks.Count() > 0))
				{
					return tracks.First();
				}
			}

			// try album, title
			if (!(
				String.IsNullOrEmpty(album) ||
				String.IsNullOrEmpty(title)))
			{
				var tracks =
					from t in root
						.Elements("tracks")
						.Elements("track")
					let al = t.Attribute(map["Album"])
					let na = t.Attribute(map["Name"])
					where (al != null) && (na != null)
					   && al.Value.Trim().ToLower().Equals(album)
					   && na.Value.Trim().ToLower().Equivalent(title)
					select PersistentID.Parse(t.Attribute(map["Persistent ID"]).Value);

				if ((tracks != null) && (tracks.Count() > 0))
				{
					return tracks.First();
				}
			}

			return PersistentID.Empty;
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

			var tracks =
				from t in root
					.Elements("tracks")
					.Elements("track")
				let ar = t.Attribute(map["Artist"])
				let al = t.Attribute(map["Album"])
				where (ar != null) && (al != null)
				   && ar.Value.Trim().ToLower() == artist
				   && al.Value.Trim().ToLower() == album
				select PersistentID.Parse(t.Attribute(map["Persistent ID"]).Value);

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

			var tracks =
				from t in root
					.Elements("tracks")
					.Elements("track")
					.Attributes(map["Artist"])
				where t.Value.Trim().ToLower() == artist
				select PersistentID.Parse(t.Parent.Attributes(map["Persistent ID"]).Single().Value);

			PersistentIDCollection list = new PersistentIDCollection(tracks.ToList<PersistentID>());
			return list;
		}


		/// <summary>
		/// Retrieves a list of all tracks found in the given playlist.
		/// </summary>
		/// <param name="playlistPID">The unique PersistentID of the playlist to examine.</param>
		/// <returns></returns>
		/// <remarks>
		/// iTunes allows users to create multiple playlists with the same name.  So we
		/// must use the PersistentID of the playlist instead of its canonical name.
		/// </remarks>

		public override PersistentIDCollection FindTracksByPlaylist (PersistentID playlistPID)
		{
			if (!isReady)
			{
				return new PersistentIDCollection();
			}

			// select all track IDs for the given playlist
			try
			{
				var tracks =
					from attr in
						((from node in root.Elements("playlists").Elements("playlist")
						  where node.Attribute(map["Playlist Persistent ID"]).Value == (string)playlistPID
						  select node.Elements("track").Attributes(map["Persistent ID"])).FirstOrDefault())
					select PersistentID.Parse(attr.Value);

				if ((tracks != null) && (tracks.Count() > 0))
				{
					PersistentIDCollection list
						= new PersistentIDCollection(tracks.ToList<PersistentID>());

					return list;
				}
			}
			catch { }

			return new PersistentIDCollection();
		}


		/// <summary>
		/// Used during a rename operation to lookup the persistent ID of a track with a
		/// specific location.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public override PersistentID GetPersistentIDByLocation (string path)
		{
			if (!isReady)
			{
				return PersistentID.Empty;
			}

			PersistentID persistentID = PersistentID.Empty;

			if (!Uri.IsWellFormedUriString(path, UriKind.Absolute))
			{
				path = new UriBuilder("file", "localhost", -1, path).Uri.AbsoluteUri;
			}

			var track =
				(from node in root
					.Elements("tracks")
					.Elements("track")
				 where node.Attribute(map["Location"]).Value == path
				 select node).FirstOrDefault();

			if (track != null)
			{
				persistentID = PersistentID.Parse(
					track.Attributes(map["Persistent ID"]).FirstOrDefault().Value);
			}

			return persistentID;
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
			if (!isReady)
			{
				return;
			}

			string trackPID = (string)track.PersistentID;
			UriBuilder builder = new UriBuilder("file", "localhost", -1, track.Location);

			XElement element = new XElement("track",
				new XAttribute(map["Track ID"], track.TrackID),
				new XAttribute(map["Name"], track.Name ?? String.Empty),
				new XAttribute(map["Artist"], track.Artist ?? String.Empty),
				new XAttribute(map["Album Artist"], track.AlbumArtist ?? String.Empty),
				new XAttribute(map["Composer"], track.Composer ?? String.Empty),
				new XAttribute(map["Album"], track.Album ?? String.Empty),
				new XAttribute(map["Genre"], track.Genre ?? String.Empty),
				new XAttribute(map["Kind"], track.KindAsString),
				new XAttribute(map["Size"], track.Size),
				new XAttribute(map["Total Time"], track.Time),
				new XAttribute(map["Disc Number"], track.DiscNumber),
				new XAttribute(map["Disc Count"], track.DiscCount),
				new XAttribute(map["Track Number"], track.TrackNumber),
				new XAttribute(map["Track Count"], track.TrackCount),
				new XAttribute(map["Year"], track.Year),
				new XAttribute(map["Date Modified"],
					track.ModificationDate.ToUniversalTime().ToString(Resx.I_iTunesDttmFormat)),
				new XAttribute(map["Date Added"],
					track.DateAdded.ToUniversalTime().ToString(Resx.I_iTunesDttmFormat)),
				new XAttribute(map["Bit Rate"], track.BitRate),
				new XAttribute(map["Sample Rate"], track.SampleRate),
				new XAttribute(map["Play Count"], track.PlayedCount),
				new XAttribute(map["Play Date UTC"],
					track.PlayedDate.ToUniversalTime().ToString(Resx.I_iTunesDttmFormat)),
				new XAttribute(map["Release Date"],
					track.ReleaseDate.ToUniversalTime().ToString(Resx.I_iTunesDttmFormat)),
				new XAttribute(map["Rating"], track.Rating),
				new XAttribute(map["Album Rating"], track.AlbumRating),
				new XAttribute(map["Album Rating Computed"], "true"),
				// TODO: artwork
				//new XAttribute(map["Artwork Count"], track.Artwork.Count),
				new XAttribute(map["Persistent ID"], (string)trackPID),
				new XAttribute(map["Track Type"], "File"),
				new XAttribute(map["Purchased"], "false"),
				new XAttribute(map["Location"], builder.Uri.AbsoluteUri),
				new XAttribute(map["File Folder Count"], "0"),
				new XAttribute(map["Library Folder Count"], "0")
				);

			root.Element("tracks").Add(element);

			var library =
				(from node in root
					.Element("playlists")
					.Elements("playlist")
				 where node.Attribute(map["Name"]).Value == Properties.Resources.iTunesLibrary
				 select node).FirstOrDefault();

			if (library != null)
			{
				library.Add(
					new XElement("track",
						new XAttribute(map["Track ID"], track.TrackID),
						new XAttribute(map["Persistent ID"], (string)trackPID)));
			}
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
			if (!isReady)
			{
				return PersistentID.Empty;
			}

			UriBuilder builder = new UriBuilder("file", "localhost", -1, path);

			var track =
				(from node in root
					.Elements("tracks")
					.Elements("track")
				 where node.Attribute(map["Location"]).Value == builder.Uri.AbsoluteUri
				 select node).FirstOrDefault();

			if (track != null)
			{
				string persistentID = track.Attributes(map["Persistent ID"]).FirstOrDefault().Value;

				var tracks =
					from node in root
						.Elements("playlists")
						.Elements("playlist")
						.Elements("track")
					where node.Attribute(map["Persistent ID"]).Value == persistentID
					select node;

				if (tracks.Count() > 0)
				{
					tracks.Remove();
				}

				track.Remove();

				return PersistentID.Parse(persistentID);
			}

			return PersistentID.Empty;
		}


		/// <summary>
		/// When a file is manually renamed outside of iTunes, this will update the catalog,
		/// updating the Location of the affected track.
		/// </summary>
		/// <param name="path"></param>

		public override void RenameFile (string path, string newPath)
		{
			if (!isReady)
			{
				return;
			}

			UriBuilder builder = new UriBuilder("file", "localhost", -1, path);

			var track =
				(from node in root
					.Elements("tracks")
					.Elements("track")
				 where node.Attribute(map["Location"]).Value == builder.Uri.AbsoluteUri
				 select node).FirstOrDefault();

			if (track != null)
			{
				XAttribute attribute = track.Attributes(map["Location"]).FirstOrDefault();
				if (attribute != null)
				{
					builder = new UriBuilder("file", "localhost", -1, newPath);
					attribute.Value = builder.Uri.AbsoluteUri;
				}
			}
		}

		#endregion FileWatch Changes

		#region Database Maintenance

		/// <summary>
		/// Adds a new playlist or updates the name of an existing playlist in the catalog.
		/// </summary>
		/// <param name="playlist"></param>
		/// <param name="pid"></param>

		public override void AddPlaylist (Playlist playlist)
		{
			if (!isReady)
			{
				return;
			}

			string pid = playlist.PersistentID;

			var element =
				(from node in root
					.Elements("playlists")
					.Elements("playlist")
				 where node.Attribute(map["Playlist Persistent ID"]).Value == (string)pid
				 select node).FirstOrDefault();

			if (element != null)
			{
				XAttribute attr = element.Attribute(map["Name"]);
				if ((attr != null) && (attr.Value != playlist.Name))
				{
					Logger.WriteLine(LogCategory, String.Format(
						"Updating playlist name '{0}' -> '{1}'", attr.Value, playlist.Name));

					attr.Value = playlist.Name;
				}
			}
			else
			{
				XElement playlists = root.Elements("playlists").FirstOrDefault() as XElement;
				if (playlists != null)
				{
					playlists.Add(
						new XElement("playlist",
							new XAttribute(map["Name"], playlist.Name),
							new XAttribute(map["Playlist ID"], playlist.PlaylistID),
							new XAttribute(map["Playlist Persistent ID"], pid),
							new XAttribute(map["All Items"], "true")
							));

					Logger.WriteLine(LogCategory,
						String.Format("Added new playlist '{0}'", playlist.Name));
				}
				else
				{
					Logger.WriteLine(LogCategory,
						String.Format("Playlist '{0}' already exists", playlist.Name));
				}
			}
		}


		/// <summary>
		/// Adds a track entry to the specified playlist.
		/// </summary>
		/// <param name="playlistPID"></param>
		/// <param name="trackPIDs"></param>

		public override void AddTracksToPlaylist (
			PersistentIDCollection trackPIDs, PersistentID playlistPID)
		{
			if (!isReady)
			{
				return;
			}

			int count = 0;

			var element =
				(from node in root
					.Elements("playlists")
					.Elements("playlist")
				 where node.Attribute(map["Playlist Persistent ID"]).Value == (string)playlistPID
				 select node).FirstOrDefault();

			if (element != null)
			{
				foreach (PersistentID pid in trackPIDs)
				{
					if (!element.Elements("track").Any(
						p => p.Attribute(map["Persistent ID"]).Value == (string)pid))
					{
						element.Add(
							new XElement("track",
								new XAttribute(map["Track ID"], pid.TransientID),
								new XAttribute(map["Persistent ID"], (string)pid)));

						count++;
					}
				}
			}

			Logger.WriteLine(LogCategory,
				String.Format("Added {0} tracks to playlist", count));
		}


		/// <summary>
		/// As a result of removing one or more tracks from a playlist, this reloads that
		/// specified playlist.
		/// </summary>
		/// <param name="playlistPID">The persistent ID of the playlist to refresh.</param>

		public override void RefreshPlaylist (PersistentID playlistPID)
		{
			if (!isReady)
			{
				return;
			}

			int count = 0;
			IEnumerable<string> trackIDs = ReloadPlaylistTracks(playlistPID);
			if (trackIDs != null)
			{
				XElement playlistRoot =
					(from node in root
						.Element("playlists")
						.Elements("playlist")
					 where node.Attribute(map["Playlist Persistent ID"]).Value == playlistPID
					 select node).FirstOrDefault();

				if (playlistRoot != null)
				{
					foreach (XElement track in playlistRoot.Elements("track"))
					{
						if (!trackIDs.Contains(track.Attribute(map["Track ID"]).Value))
						{
							track.Remove();
						}
					}
				}
			}

			Logger.WriteLine(LogCategory,
				String.Format("Removed {0} tracks from playlist", count));
		}


		/// <summary>
		/// Reload the tracks for the specified playlist.
		/// </summary>
		/// <param name="persistentID"></param>
		/// <returns></returns>
		/// <remarks>
		/// While we are reloading and reparsing the entire Library.xml file, this is actually
		/// twice as fast as enumerating the tracks using the iTunesAppClass COM interface.
		/// </remarks>

		private List<string> ReloadPlaylistTracks (PersistentID persistentID)
		{
			if (!IsLibraryUpdated())
			{
				Logger.WriteLine(LogCategory, "Reload not performed since XML not updated");
				return null;
			}

			XElement playlistRoot = null;

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
						// read into //plist/dict and stop at the first ./array
						// this is the playlists collection

						if (reader.ReadToDescendant("array"))
						{
							// by using this subreader and pumping the stream through
							// XElement.ReadFrom, we should avoid the Large Object Heap

							using (XmlReader subreader = reader.ReadSubtree())
							{
								playlistRoot = XElement.ReadFrom(subreader) as XElement;
							}
						}
					}
				}

				writeTime = GetLibraryTime();
			}
			catch (Exception exc)
			{
				Logger.WriteLine(LogCategory, exc);
				return null;
			}

			if (playlistRoot != null)
			{
				// find the specified <playlist> node
				XElement target =
					(from node in playlistRoot
						.Elements("dict")
						.Elements("key")
					 where node.Value == "Playlist Persistent ID" &&
						 ((XElement)node.NextNode).Value == (string)persistentID
					 select node.Parent).FirstOrDefault();

				if (target != null)
				{
					// extract the trackIDs from the playlist
					var list =
						from node in target
							.Elements("array")
							.Elements("dict")
							.Elements("integer")
						select node.Value;

					if ((list != null) && (list.Count() > 0))
					{
						return list.ToList<string>();
					}
				}
			}

			return new List<string>();
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
			if (!isReady)
			{
				return;
			}

			var playlists =
				from node in root
					.Elements("playlists")
					.Elements("playlist")
				select node;

			string key = map["Playlist Persistent ID"];

			int count = 0;
			foreach (XElement playlist in playlists)
			{
				if (!playlistPIDs.Contains(PersistentID.Parse(playlist.Attribute(key).Value)))
				{
					playlist.Remove();
					count++;
				}
			}

			Logger.WriteLine(LogCategory, String.Format("Removed {0} playlists", count));
		}

		#endregion Database Maintenance


		//========================================================================================
		// Helpers
		//========================================================================================

		/// <summary>
		/// Read the Music Folder entry from the library.  This is directory where
		/// the user stores music files, which is typically different than where the 
		/// "iTunes Music Library.xml" file is stored.
		/// </summary>
		/// <returns>A string specifying the absolute path of the music root folder.</returns>

		protected override string FindMusicFolderPath ()
		{
			if (!map.ContainsKey("Music Folder"))
			{
				// Disconnected mode
				return String.Empty;
			}

			// find the <library MuFo="" /> attribute
			string folder =
				(from attr in root.Attributes(map["Music Folder"])
				 select attr.Value).FirstOrDefault();

			Uri uri = new Uri(folder);

			string path = PathHelper.GetLocalPath(uri);
			return path;
		}


		/// <summary>
		/// Retrieves the last-write-time of the "iTunes Music Library.xml" file.
		/// </summary>
		/// <returns></returns>

		private DateTime GetLibraryTime ()
		{
			return new FileInfo(path).LastWriteTime;
		}


		/// <summary>
		/// Determines if the "iTunes Music Library.xml" file has been updated since our
		/// last read.
		/// </summary>
		/// <returns></returns>

		private bool IsLibraryUpdated ()
		{
			return GetLibraryTime().CompareTo(writeTime) > 0;
		}


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

			var locations =
				from attr in root
					.Element("tracks")
					.Elements("track")
					.Attributes(map["Location"])
				select Path.GetExtension(attr.Value).ToLower();

			if (locations != null)
			{
				StringCollection list = FilterMusicalExtensions(locations.Distinct());
				foreach (string ext in list)
				{
					extensions.Add(ext);
				}

				list.Clear();
				list = null;
			}

			// add some well-known extensions if missing
			base.AddKnownExtensions(extensions);

			return extensions;
		}
	}
}
