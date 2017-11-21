//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Text.RegularExpressions;
	using iTuner;
	using iTunesLib;
	using Resx = Properties.Resources;


	/// <summary>
	/// A safe wrapper of an IITTrack object.
	/// </summary>

	internal sealed class Track : Interaction, ITrack, ITrackBasics, INotifyPropertyChanged
	{
		private class Buffer : Dictionary<string, string> { }

		private IITTrack track;					// internal iTunes track
		private PersistentID persistentID;		// internal iTunes ID

		private string artist;					// cached artist or composer
		private string artistURL;				// cached artist/band URL
		private string location;				// cached location, kind specific
		private string uniqueID;				// uniqueish ID for this track (PUID)

		private bool isBuffered;				// true if in buffering mode
		private Buffer buffer;					// buffer for transient tag values


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance that wraps the given iTunes playlist COM object.
		/// </summary>
		/// <param name="track">An IITTrack instance.</param>

		public Track (IITTrack track)
			: base()
		{
			this.track = track;
			this.persistentID = PersistentID.Empty;

			this.artist = null;
			this.artistURL = null;
			this.location = null;
			this.uniqueID = null;

			this.isBuffered = false;
			this.buffer = null;
		}


		/// <summary>
		/// Interaction.Cleanup override; release reference to internal track.
		/// </summary>

		protected override void Cleanup (bool finalRelease)
		{
			if (buffer != null)
			{
				buffer.Clear();
				buffer = null;
			}

			Release(track);
			track = null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// This event is fired when the value of a property is changed.
		/// </summary>

		public event PropertyChangedEventHandler PropertyChanged;


		/// <summary>
		/// Gets or sets the name of the album containing this track.
		/// </summary>
		/// <remarks>
		/// This is a buffered properties; updates are deferred until ApplyBuffer is called.
		/// </remarks>

		public string Album
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Album"))
				{
					return buffer["Album"];
				}

				if (track == null)
				{
					return String.Empty;
				}

				return Invoke((Func<string>)delegate
				{
					string album = track.Album;
					return album == null ? null : album.Trim();
				});
			}

			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					if (isBuffered)
					{
						BufferTag("Album", value);
					}
					else
					{
						Invoke((Action)delegate
						{
							track.Album = value;
						});
					}

					OnPropertyChanged("Album");
				}
			}
		}


		/// <summary>
		/// Gets or sets the name of the album artist of the track.
		/// </summary>

		public string AlbumArtist
		{
			get
			{
				if (track == null)
				{
					return String.Empty;
				}

				return Invoke((Func<string>)delegate
				{
					if (track is IITFileOrCDTrack)
					{
						return ((IITFileOrCDTrack)track).AlbumArtist;
					}

					return String.Empty;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					if (track is IITFileOrCDTrack)
					{
						((IITFileOrCDTrack)track).AlbumArtist = value;
					}
				});

				OnPropertyChanged("AlbumArtist");
			}
		}


		/// <summary>
		/// Gets or sets the album rating of the album that this track belongs to (0 to 100).
		/// If the album rating is set to 0, it will be computed based on the ratings of tracks
		/// in the album
		/// </summary>

		public int AlbumRating
		{
			get
			{
				if (track == null)
				{
					return 0;
				}

				return Invoke((Func<int>)delegate
				{
					if (track is IITFileOrCDTrack)
					{
						return ((IITFileOrCDTrack)track).AlbumRating;
					}

					return 0;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					if (track is IITFileOrCDTrack)
					{
						((IITFileOrCDTrack)track).AlbumRating = value;
					}
				});

				OnPropertyChanged("AlbumRating");
			}
		}



		/// <summary>
		/// Gets the internal iTunes IITTrack object.
		/// </summary>

		public IITTrack AsInternal
		{
			get { return track; }
		}


		/// <summary>
		/// Gets or sets the name of the artist/source of this track.
		/// </summary>
		/// <remarks>
		/// This is a buffered properties; updates are deferred until ApplyBuffer is called.
		/// </remarks>

		public string Artist
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Artist"))
				{
					return buffer["Artist"];
				}

				if (track == null)
				{
					return String.Empty;
				}

				if (artist == null)
				{
					artist = Invoke((Func<string>)delegate
					{
						if (!String.IsNullOrEmpty(track.Artist))
						{
							return track.Artist;
						}
						else if (!String.IsNullOrEmpty(track.Composer))
						{
							return track.Composer;
						}
						else
						{
							return Resx.Unknown;
						}
					});

					if (artist != null)
					{
						artist = artist.Trim();
					}
				}

				return artist;
			}

			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					if (isBuffered)
					{
						BufferTag("Artist", value);
					}
					else
					{
						Invoke((Action)delegate
						{
							track.Artist = value;
						});
					}

					OnPropertyChanged("Artist");
				}
			}
		}


		/// <summary>
		/// Gets or sets the URL of the artist Web site.
		/// This is a custom iTuner property storedin IITrack.Comments.
		/// </summary>

		public string ArtistURL
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("ArtistURL"))
				{
					return buffer["ArtistURL"];
				}

				if (track == null)
				{
					return String.Empty;
				}

				if (artistURL == null)
				{
					string comment = Invoke((Func<string>)delegate
					{
						return track.Comment;
					});

					if (comment != null)
					{
						Match match = Regex.Match(comment, @"\[ArtistURL\:(.*?)\]");
						if (match.Success)
						{
							artistURL = match.Groups[1].Value;
						}
					}
				}

				return artistURL;
			}

			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					if (isBuffered)
					{
						BufferTag("ArtistURL", value);
					}
					else
					{
						ApplyArtistURL(value);
					}

					OnPropertyChanged("ArtistURL");
				}
			}
		}
	

		/// <summary>
		/// Gets or sets the path of a cached data file containing the image
		/// for this track's album.
		/// </summary>

		public string Artwork
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Artwork"))
				{
					return buffer["Artwork"];
				}

				if (track == null)
				{
					return null;
				}

				string path = Invoke((Func<string>)delegate
				{
					if ((track.Artwork != null) && (track.Artwork.Count > 0))
					{
						string ext = null;
						switch (track.Artwork[1].Format)  // collections are one-based
						{
							case ITArtworkFormat.ITArtworkFormatBMP: ext = ".bmp"; break;
							case ITArtworkFormat.ITArtworkFormatJPEG: ext = ".jpg"; break;
							case ITArtworkFormat.ITArtworkFormatPNG: ext = ".png"; break;
							default: ext = ".jpg"; break;
						}

						return Path.Combine(
							PathHelper.CommonDataPath,
							PathHelper.CleanFileName(String.Format("{0}_{1}{2}", Artist, Album, ext)));
					}

					return null;
				});

				if (path == null)
				{
					return null;
				}

				if (File.Exists(path))
				{
					return path;
				}

				string dirpath = Path.GetDirectoryName(path);
				if (!Directory.Exists(dirpath))
				{
					Directory.CreateDirectory(dirpath);
				}

				Invoke((Action)delegate
				{
					track.Artwork[1].SaveArtworkToFile(path);
				});

				return path;
			}

			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					if (isBuffered)
					{
						BufferTag("Artwork", value);
					}
					else
					{
						ApplyArtwork(value);
					}

					OnPropertyChanged("Artwork");
				} 
			}
		}


		/// <summary>
		/// Gets the bit rate of the track (in kbps).
		/// </summary>

		public long BitRate
		{
			get
			{
				if (track == null)
				{
					return 0;
				}

				return Invoke((Func<long>)delegate
				{
					return track.BitRate;
				});
			}
		}


		/// <summary>
		/// Gets or sets the tempo of the track (in beats per minute).
		/// </summary>

		public int BPM
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.BPM;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.BPM = value;
				});

				OnPropertyChanged("BPM");
			}
		}


		/// <summary>
		/// Gets or sets freeform notes about this track.
		/// </summary>

		public string Comment
		{
			get
			{
				if (track == null)
				{
					return String.Empty;
				}

				return Invoke((Func<string>)delegate
				{
					return track.Comment;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Comment = value;
				});

				OnPropertyChanged("Comment");
			}
		}


		/// <summary>
		/// Gets or sets whether this track is from a compilation album.
		/// </summary>

		public bool Compilation
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					return track.Compilation;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Compilation = value;
				});

				OnPropertyChanged("Compilation");
			}
		}


		/// <summary>
		/// Gets or sets the composer of this track.
		/// </summary>

		public string Composer
		{
			get
			{
				if (track == null)
				{
					return String.Empty;
				}

				return Invoke((Func<string>)delegate
				{
					return track.Composer;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Composer = value;
				});

				OnPropertyChanged("Composer");
			}
		}


		/// <summary>
		/// Gets the date the track was added to the playlist.
		/// </summary>

		public DateTime DateAdded
		{
			get
			{
				return Invoke((Func<DateTime>)delegate
				{
					return track.DateAdded;
				});
			}
		}


		/// <summary>
		/// Gets or sets total number of discs in the source album.
		/// </summary>

		public int DiscCount
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.DiscCount;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.DiscCount = value;
				});

				OnPropertyChanged("DiscCount");
			}
		}


		/// <summary>
		/// Gets or sets the index of the disc containing the track on the source album.
		/// </summary>

		public int DiscNumber
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.DiscNumber;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.DiscNumber = value;
				});

				OnPropertyChanged("DiscNumber");
			}
		}


		/// <summary>
		/// Gets the length of the track (in seconds).
		/// </summary>

		public long Duration
		{
			get
			{
				if (track == null)
				{
					return 0;
				}

				return Invoke((Func<long>)delegate
				{
					return track.Duration;
				});
			}

			set { }
		}


		/// <summary>
		/// Gets or sets whether this track is checked for playback.
		/// </summary>

		public bool Enabled
		{
			get
			{
				return Invoke((Func<bool>)delegate
				{
					return track.Enabled;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Enabled = value;
				});
			}
		}


		/// <summary>
		/// Gets or sets the name of the EQ preset of the track.
		/// </summary>

		public string EQ
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return track.EQ;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.EQ = value;
				});

				OnPropertyChanged("EQ");
			}
		}


		/// <summary>
		/// Gets or sets the stop time of the track (in seconds).
		/// </summary>

		public int Finish
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.Finish;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Finish = value;
				});

				OnPropertyChanged("Finish");
			}
		}


		/// <summary>
		/// Gets or sets the music/audio genre (category) of the track.
		/// </summary>
		/// <remarks>
		/// This is a buffered properties; updates are deferred until ApplyBuffer is called.
		/// </remarks>

		public string Genre
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Genre"))
				{
					return buffer["Genre"];
				}

				if (track == null)
				{
					return String.Empty;
				}

				return Invoke((Func<string>)delegate
				{
					return track.Genre;
				});
			}

			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					if (isBuffered)
					{
						BufferTag("Genre", value);
					}
					else
					{
						Invoke((Action)delegate
						{
							track.Genre = value;
						});
					}

					OnPropertyChanged("Genre");
				}
			}
		}


		/// <summary>
		/// Gets or sets the grouping (piece) of the track.   Generally used to denote
		/// movements within classical work.
		/// </summary>

		public string Grouping
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return track.Grouping;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Grouping = value;
				});

				OnPropertyChanged("Grouping");
			}
		}


		/// <summary>
		/// Gets the kind of this track.
		/// </summary>

		public TrackKind Kind
		{
			get
			{
				ITTrackKind kind = Invoke((Func<ITTrackKind>)delegate
				{
					return track.Kind;
				});

				switch (kind)
				{
					case ITTrackKind.ITTrackKindCD: return TrackKind.CD;
					case ITTrackKind.ITTrackKindDevice: return TrackKind.Device;
					case ITTrackKind.ITTrackKindFile: return TrackKind.File;
					case ITTrackKind.ITTrackKindSharedLibrary: return TrackKind.SharedLibrary;
					case ITTrackKind.ITTrackKindURL: return TrackKind.URL;
					default: return TrackKind.Unknown;
				}
			}
		}


		/// <summary>
		/// Gets the text description of the track (e.g. "AAC audio file").
		/// </summary>

		public string KindAsString
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return track.KindAsString;
				});
			}
		}


		/// <summary>
		/// Gets the full path to the file represented by this track. iTunes will not
		/// move or copy any existing file from the track's current location to the new
		/// location, the full path must point to an existing file that is playable by iTunes.
		/// Attempting to set the location of a track on CD will always fail with E_INVALIDARG.
		/// </summary>

		public string Location
		{
			get
			{
				if (track == null)
				{
					return String.Empty;
				}

				if (location == null)
				{
					location = Invoke((Func<string>)delegate
					{
						switch (track.Kind)
						{
							case ITTrackKind.ITTrackKindCD:
							case ITTrackKind.ITTrackKindFile:
								return ((IITFileOrCDTrack)track).Location;

							case ITTrackKind.ITTrackKindURL:
								return ((IITURLTrack)track).URL;

							default:
								return String.Empty;
						}
					});
				}

				return location;
			}

			set
			{
				// Note that set is used for unit testing only
				location = value;
			}
		}


		/// <summary>
		/// Gets or sets the lyrics for the track.
		/// <para>
		/// Note that setting the lyrics only caches them as <i>pending</i> until
		/// ApplyLyrics is called.
		/// </para>
		/// </summary>
		/// <value>
		/// The lyrics are returned as a string if available.  When lyrics are not available,
		/// an empty string is returned.
		/// </value>

		public string Lyrics
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Lyrics"))
				{
					return buffer["Lyrics"];
				}

				if (track == null)
				{
					return String.Empty;
				}

				if (track is IITFileOrCDTrack)
				{
					return Invoke((Func<string>)delegate
					{
						string lyrics = ((IITFileOrCDTrack)track).Lyrics;
						return lyrics == null ? String.Empty : lyrics.Trim();
					});
				}

				return String.Empty;
			}

			set
			{
				var kind = Invoke((Func<ITTrackKind>)delegate
				{
					return track.Kind;
				});

				if (kind == ITTrackKind.ITTrackKindFile)
				{
					if (isBuffered)
					{
						BufferTag("Lyrics", (value == null ? null : value.Trim()));
					}
					else
					{
						ApplyLyrics(value);
					}
				}

				OnPropertyChanged("Lyrics");
			}
		}


		/// <summary>
		/// Gets the modification date of the content of the track.
		/// </summary>

		public DateTime ModificationDate
		{
			get
			{
				return Invoke((Func<DateTime>)delegate
				{
					return track.ModificationDate;
				});
			}
		}


		/// <summary>
		/// Gets or sets the publicily visible name of this track.
		/// </summary>
		/// <remarks>
		/// This is a buffered properties; updates are deferred until ApplyBuffer is called.
		/// </remarks>

		public string Name
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Name"))
				{
					return buffer["Name"];
				}

				if (track == null)
				{
					return String.Empty;
				}

				return Invoke((Func<string>)delegate
				{
					string name = track.Name;
					return name == null ? null : name.Trim();
				});
			}

			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					if (isBuffered)
					{
						BufferTag("Name", value);
					}
					else
					{
						Invoke((Action)delegate
						{
							track.Name = value;
						});
					}

					OnPropertyChanged("Name");
				}
			}
		}


		/// <summary>
		/// Gets the four-part object ID of this track.
		/// </summary>

		public ObjectID ObjectID
		{
			get
			{
				return base.GetObjectID();
			}
		}


		/// <summary>
		/// Gets the unique persistent ID of this track.
		/// </summary>

		public PersistentID PersistentID
		{
			get
			{
				if (track == null)
				{
					return PersistentID.Empty;
				}

				if (persistentID.IsEmpty)
				{
					persistentID = base.GetPersistentID(track);
				}

				return persistentID;
			}
		}


		/// <summary>
		/// Gets or sets the number of times the track has been played. This property 
		/// cannot be set if the track is not playable (e.g. a PDF file).
		/// </summary>

		public int PlayedCount
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.PlayedCount;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.PlayedCount = value;
				});

				OnPropertyChanged("PlayedCount");
			}
		}


		/// <summary>
		/// Gets or sets the date and time the track was last played. This property cannot 
		/// be set if the track is not playable (e.g. a PDF file). 
		/// </summary>

		public DateTime PlayedDate
		{
			get
			{
				return Invoke((Func<DateTime>)delegate
				{
					return track.PlayedDate;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.PlayedDate = value;
				});

				OnPropertyChanged("PlayedDate");
			}
		}


		/// <summary>
		/// Gets a Playlist object corresponding to the playlist that contains the track.
		/// </summary>

		public Playlist Playlist
		{
			get
			{
				return Invoke((Func<Playlist>)delegate
				{
					return new Playlist(track.Playlist);
				});
			}
		}


		/// <summary>
		/// Gets the play order index of the track in the owner playlist (1-based).
		/// </summary>

		public int PlayOrderIndex
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.PlayOrderIndex;
				});
			}
		}


		/// <summary>
		/// Gets or sets the rating of the track (0 to 100). If the track rating is set to 0, 
		/// it will be computed based on the album rating.
		/// </summary>

		public int Rating
		{
			get
			{
				if (track == null)
				{
					return 0;
				}

				return Invoke((Func<int>)delegate
				{
					return track.Rating;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Rating = value;
				});

				OnPropertyChanged("Rating");
			}
		}


		/// <summary>
		/// Gets the release date of the track.
		/// </summary>

		public DateTime ReleaseDate
		{
			get
			{
				if (track == null)
				{
					return DateTime.MinValue;
				}

				return Invoke((Func<DateTime>)delegate
				{
					if (track is IITFileOrCDTrack)
					{
						return ((IITFileOrCDTrack)track).ReleaseDate;
					}

					return DateTime.MinValue;
				});
			}
		}


		/// <summary>
		/// Gets the sample rate of the track (in Hz).
		/// </summary>

		public int SampleRate
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.SampleRate;
				});
			}
		}


		/// <summary>
		/// Gets the Source enumeration value of the current track.
		/// </summary>

		public Sources Source
		{
			get
			{
				Sources source = Sources.Music;

				string playlistName = Invoke((Func<string>)delegate
				{
					return track.Playlist.Name;
				});

				switch (playlistName)
				{
					case "iTunes Store":
						source = Sources.Store;
						break;

					case "Movies":
						source = Sources.Movies;
						break;

					case "Podcasts":
						source = Sources.Podcast;
						break;

					case "Radio":
						source = Sources.Radio;
						break;

					case "TV Shows":
						source = Sources.TVShow;
						break;

					default:
						ITTrackKind kind = Invoke((Func<ITTrackKind>)delegate
						{
							return track == null ? ITTrackKind.ITTrackKindUnknown : track.Kind;
						});

						if ((kind == ITTrackKind.ITTrackKindFile) ||
							(kind == ITTrackKind.ITTrackKindDevice))
						{
							source = Sources.Music;
						}
						else
						{
							source = Sources.CD;
						}
						break;
				}

				return source;
			}
		}
	
		
		/// <summary>
		/// Gets the size of the track (in bytes).
		/// </summary>

		public int Size
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.Size;
				});
			}
		}


		/// <summary>
		/// Gets or sets the start time of the track (in seconds).
		/// </summary>

		public int Start
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.Start;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.Start = value;
				});

				OnPropertyChanged("Start");
			}
		}


		/// <summary>
		/// Gets the time of the track (in MM:SS format).
		/// </summary>

		public string Time
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return track.Time;
				});
			}
		}


		/// <summary>
		/// Gets or sets the title of this track (equivalent to the Name property).
		/// </summary>
		/// <remarks>
		/// This is a buffered properties; updates are deferred until ApplyBuffer is called.
		/// </remarks>

		public string Title
		{
			get { return Name; }
			set { Name = value; }
		}


		/// <summary>
		/// Gets or sets the total number of tracks on the source album.
		/// </summary>

		public int TrackCount
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.TrackCount;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.TrackCount = value;
				});

				OnPropertyChanged("TrackCount");
			}
		}


		/// <summary>
		/// Gets the ID that identifies the track, independent of its playlist.  Valid for a
		/// track. Will be zero for a source or playlist. If the same music file is in two
		/// different playlists, each of the tracks will have the same track database ID. This
		/// is a runtime ID, it is only valid while the current instance of iTunes is running
		/// </summary>

		public int TrackID
		{
			get
			{
				if (track == null)
				{
					return 0;
				}

				return Invoke((Func<int>)delegate
				{
					// TODO: why/how could track be null here?
					return track.trackID;
				});
			}
		}


		/// <summary>
		/// Gets or sets the index of the track on the source album.
		/// </summary>

		public int TrackNumber
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.TrackNumber;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.TrackNumber = value;
				});

				OnPropertyChanged("TrackNumber");
			}
		}


		/// <summary>
		/// Gets or sets the (unique) ID of this track as calculated by an online
		/// service such as MusicDNS genpuid.
		/// </summary>

		public string UniqueID
		{
			get { return uniqueID; }
			set { uniqueID = value; }
		}


		/// <summary>
		/// Gets or sets the relative volume adjustment of the track (-100% to 100%).
		/// </summary>

		public int VolumeAdjustment
		{
			get
			{
				return Invoke((Func<int>)delegate
				{
					return track.VolumeAdjustment;
				});
			}

			set
			{
				Invoke((Action)delegate
				{
					track.VolumeAdjustment = value;
				});

				OnPropertyChanged("VolumeAdjustment");
			}
		}


		/// <summary>
		/// Gets or sets the year the track was recorded/released.
		/// </summary>
		/// <remarks>
		/// This is a buffered properties; updates are deferred until ApplyBuffer is called.
		/// </remarks>

		public int Year
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Year"))
				{
					int year;
					if (Int32.TryParse(buffer["Year"], out year))
					{
						return year;
					}
				}

				if (track == null)
				{
					return 3000;
				}

				return Invoke((Func<int>)delegate
				{
					return track.Year;
				});
			}

			set
			{
				if (isBuffered)
				{
					BufferTag("Year", value.ToString());
				}
				else
				{
					Invoke((Action)delegate
					{
						track.Year = value;
					});
				}

				OnPropertyChanged("Year");
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Delete this track entry; it does not delete the physical media file.
		/// <para>
		/// <i>The instance is disposed by this method; callers should dereference immediately
		/// after this method returns.</i>
		/// </para>
		/// </summary>

		public void Delete ()
		{
			Invoke((Action)delegate
			{
				track.Delete();
			});
		}


		/// <summary>
		/// Start playing this track.
		/// </summary>

		public void Play ()
		{
			Invoke((Action)delegate
			{
				track.Play();
			});
		}


		//========================================================================================
		// iTuner Extensions
		//========================================================================================

		/// <summary>
		/// Gets a Boolean value indicating whether this track has visible artwork.
		/// </summary>

		public bool HasArtwork
		{
			get
			{
				if (isBuffered && buffer.ContainsKey("Artwork"))
				{
					return true;
				}

				return Invoke((Func<bool>)delegate
				{
					if ((track.Artwork != null) && (track.Artwork.Count > 0))
					{
						// collections are one-based
						return track.Artwork[1].Format != ITArtworkFormat.ITArtworkFormatUnknown;
					}

					return false;
				});
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating whether this track has readable lyrics.
		/// </summary>

		public bool HasLyrics
		{
			get
			{
				if (track == null)
				{
					// fakes out the Lyrics Engine into thinking we have lyrics
					// to avoid sending bogus requests to the online providers
					return true;
				}

				if (isBuffered && buffer.ContainsKey("Lyrics"))
				{
					return true;
				}

				string lyrics = this.Lyrics;
				if (!String.IsNullOrEmpty(lyrics))
				{
					// if lyrics looks like HTML, we might have <A HREF="blahbidyblah"> referring
					// us to a search page!  so let's just look it up again and overwrite...
					// this would be caused by older versions of iTuner that didn't know how
					// to handle this properly, yikes!

					return !(lyrics.StartsWith("<") || lyrics.EndsWith(">"));
				}

				// no pending and no persisted
				return false;
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if this track was updated using an online
		/// Webservice such as MucicDNS genpuid or MusicBrainz.
		/// </summary>

		public bool IsAnalyzed
		{
			get { return !String.IsNullOrEmpty(uniqueID); }
		}


		/// <summary>
		/// Determines if the current track is subjectively better than the specified track.
		/// This is done by comparing critical qualities of the two tracks such as BitRate,
		/// rating, run count, etc.
		/// </summary>
		/// <param name="other">The other track to compare against this instance.</param>
		/// <returns>
		/// <b>True</b> if the current instance is better or <b>false</b> if the
		/// given instance is better.
		/// </returns>

		public bool IsBetterThan (Track other)
		{
			if (other == null)
			{
				return true;
			}

			// QUALITATIVE PRIORITIES

			// higher bitrate - top priority
			long tBitRate = this.BitRate;
			long oBitRate = other.BitRate;
			if (tBitRate != oBitRate)
			{
				return (tBitRate > oBitRate);
			}

			// contains lyrics
			if (String.IsNullOrEmpty(this.Lyrics) && !String.IsNullOrEmpty(other.Lyrics))
				return true;

			// contains artwork
			if (this.HasArtwork && !other.HasArtwork)
				return true;

			// higher rating
			if (this.Rating > other.Rating)
				return true;

			// higher play count
			if (this.PlayedCount > other.PlayedCount)
				return true;

			// longer duration
			// TODO: if the duration is different, are they really the same song?
			if (this.Duration > other.Duration)
				return true;

			// No, this track is not better than other
			return false;
		}

	
		/// <summary>
		/// Gets or sets a Boolean value indicating whether this track is currently in a
		/// buffered state where updates to crtiical identifying properties are buffered
		/// rather than permanently persisted.
		/// </summary>

		public bool IsBuffered
		{
			get
			{
				return isBuffered;
			}

			set
			{
				isBuffered = value;
				buffer = (isBuffered ? new Buffer() : null);
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating if this is a track that might contain
		/// lyrics; a track for which lyrics could be downloaded and stored.
		/// </summary>

		public bool IsLyrical
		{
			get
			{
				if (track != null)
				{
					var kind = Invoke((Func<ITTrackKind>)delegate
					{
						return track.Kind;
					});

					if (kind == ITTrackKind.ITTrackKindFile)
					{
						// TODO: should we also check for "audio" file type?
						return true;
					}
				}

				return false;
			}
		}


		/// <summary>
		/// Persist the transient tags stored in the buffer to the actual internal track.
		/// </summary>

		public void ApplyBuffer ()
		{
			if (buffer != null)
			{
				// disable buffer mode
				isBuffered = false;

				Type type = this.GetType();
				PropertyInfo property;

				foreach (string key in buffer.Keys)
				{
					if (key.Equals("ArtistURL"))
					{
						ApplyArtistURL(buffer[key]);
					}
					else if (key.Equals("Artwork"))
					{
						ApplyArtwork(buffer[key]);
					}
					else if (key.Equals("Lyrics"))
					{
						ApplyLyrics(buffer[key]);
					}
					else
					{
						property = type.GetProperty(key);

						try
						{
							// need to convert the buffered String value to the native type
							// of the property

							object value = Convert.ChangeType(buffer[key], property.PropertyType);
							if (value == null)
							{
								// if null then only set the property if it is a reference type
								if (!property.PropertyType.IsValueType)
								{
									Invoke((Action)delegate
									{
										property.SetValue(this, value, null);
									});
								}
							}
							else
							{
								Invoke((Action)delegate
								{
									property.SetValue(this, value, null);
								});
							}
						}
						catch
						{
							// no-op
						}
					}
				}

				buffer.Clear();
				buffer = null;
			}
		}


		/// <summary>
		/// Apply the cached artist URL to the track.  This is deferred while the current track
		/// is playing so we don't interrupt playback causing a momentary pause.
		/// </summary>

		private void ApplyArtistURL (string artistURL)
		{
			if (!String.IsNullOrEmpty(artistURL))
			{
				string comment = Invoke((Func<string>)delegate
				{
					string c = track.Comment;
					return (c == null ? String.Empty : c.Trim());
				});

				Match match = Regex.Match(comment, @"\[ArtistURL\:(.*?)\]");
				if (match.Success)
				{
					comment = comment.Replace(match.Groups[1].Value, artistURL);
				}
				else
				{
					if (comment.Length == 0)
					{
						comment += String.Format("[ArtistURL:{0}]", artistURL);
					}
					else
					{
						comment +=
							Environment.NewLine +
							String.Format("[ArtistURL:{0}]", artistURL);
					}
				}

				Invoke((Action)delegate
				{
					track.Comment = comment;
				});
			}
		}


		/// <summary>
		/// Apply the cached artwork to the track.  This is deferred while the current track
		/// is playing so we don't interrupt playback causing a momentary pause.
		/// </summary>

		private void ApplyArtwork (string artwork)
		{
			if (!String.IsNullOrEmpty(artwork))
			{
				var kind = Invoke((Func<ITTrackKind>)delegate
				{
					return track.Kind;
				});

				if (kind == ITTrackKind.ITTrackKindFile)
				{
					Invoke((Action)delegate
					{
						track.AddArtworkFromFile(artwork);
					});
				}
			}
		}


		/// <summary>
		/// Apply the cached lyrics to the track.  This is deferred while the current track
		/// is playing so we don't interrupt playback causing a momentary pause.
		/// </summary>

		private void ApplyLyrics (string lyrics)
		{
			if (!String.IsNullOrEmpty(lyrics))
			{
				var kind = Invoke((Func<ITTrackKind>)delegate
				{
					return track.Kind;
				});

				if (kind == ITTrackKind.ITTrackKindFile)
				{
					Invoke((Action)delegate
					{
						((IITFileOrCDTrack)track).Lyrics = lyrics;
					});
				}
			}
		}


		/// <summary>
		/// Stores a given keyed value in the temporary buffer.  These should either be
		/// applied using the Update method or ignored.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>

		private void BufferTag (string key, string value)
		{
			if (buffer.ContainsKey(key))
			{
				buffer[key] = value;
			}
			else
			{
				buffer.Add(key, value);
			}
		}


		/// <summary>
		/// Create a three-part key string from the current track.
		/// </summary>
		/// <param name="track">The track to use.</param>
		/// <returns></returns>
		/// <remarks>
		/// This differs from ToString() because this is not Buffer-aware.
		/// </remarks>

		public string MakeKey ()
		{
			return Invoke((Func<string>)delegate
			{
				return String.Format("{0}~{1}~{2}", track.Artist, track.Name, track.Album);
			});
		}


		/// <summary>
		/// Create a three-part key string from the current track and determine 
		/// if the key is complete - has all parts.
		/// </summary>
		/// <param name="track">The track to use.</param>
		/// <param name="isTernary">True if all three parts of the key are available.</param>
		/// <returns></returns>

		public string MakeKey (out bool isTernary)
		{
			isTernary = !(
				String.IsNullOrEmpty(Artist) ||
				String.IsNullOrEmpty(Name) ||
				String.IsNullOrEmpty(Album));

			// use (buffered) properties here instead of calling Track.MakeKey(IITTrack)
			return String.Format("{0}~{1}~{2}", Artist, Name, Album);
		}


		/// <summary>
		/// Generates a string that can be used as a header for lyrics exports.
		/// </summary>
		/// <returns>A string specifying the basic information of this track.</returns>

		public string MakeLyricReportHeader ()
		{
			/*
			 * Title...: {title} ({time})
			 * Artist..: {artist}
			 * Album...: {album} ({year})
			 * Rating..: {rating}                 iTuner 1.0.1234
			 * ==================================================
			 * 
			 */

			string title = App.NameVersion;

			string rating = String.Format(Resx.LyricsRatingLine,
				Rating == 0 ? "-" : new String('*', Rating / 20));

			StringBuilder builder = new StringBuilder();
			builder.Append(Environment.NewLine);
			builder.Append(String.Format(Resx.LyricsTitleLine, Name, Time));
			builder.Append(Environment.NewLine);
			builder.Append(String.Format(Resx.LyricsArtistLine, Artist));
			builder.Append(Environment.NewLine);
			builder.Append(String.Format(Resx.LyricsAlbumLine, Album, Year.ToString()));
			builder.Append(Environment.NewLine);
			builder.Append(rating);
			builder.Append(new String(' ', 79 - rating.Length - title.Length));
			builder.Append(title);
			builder.Append(Environment.NewLine);
			builder.Append(new String('=', 79));
			builder.Append(Environment.NewLine);
			builder.Append(Environment.NewLine);

			return builder.ToString();
		}


		/// <summary>
		/// Raises the PropertyChanged event when the specified property value is changed.
		/// </summary>
		/// <param name="name"></param>

		private void OnPropertyChanged (string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}


		/// <summary>
		/// Generate a simple identifier string mainly for debugging.  
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This differs from MakeKey(IITTrack) because this is Buffer-aware.
		/// </remarks>

		public override string ToString ()
		{
			return String.Format(
				"Artist:{0} ~Title:{1} ~Album:{2} ~Location:{3}",
				Artist, Name, Album, Location);
		}
	}
}
