//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
// Compilations, Soundtrack
//************************************************************************************************

#define xVerbose

namespace iTuner.iTunes
{
	using System;
	using System.Configuration;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Reflection;
	using System.Text;
	using System.Threading;
	using System.Xml.Linq;


	/// <summary>
	/// Combines MusicDNS genpuid with MusicBrainz Webservice to retrieve extact information
	/// regarding a specific media file.
	/// </summary>
	/// <remarks>
	/// MusicBrainz limits clients requests to no more than one per second.  So we govern
	/// this internally by enforcing up to a one second wait in between retrievals by
	/// blocking for the remaining time.  However, the probability of actually falling into
	/// this is low considering that genpuid usually takes longer than a second anyway.
	/// </remarks>

	internal class Tagger
	{
		private const string LogCategory = "Tagger";

		private const int GenTimeout = 1000 * 60;
		private const int MinWaitTime = 1000;

		// These MusicBrains URIs are
		// described here @ http://musicbrainz.org/doc/XML_Web_Service/Version_2

		private const string BrainzPuidUri =
			"http://musicbrainz.org/ws/2/puid/{0}?inc=releases+artists+media+tags&status=official";

		private const string BrainzArtistUri =
			"http://musicbrainz.org/ws/2/artist/{0}?inc=url-rels";

		private static string DefaultDnsKey;
		private static Assembly tagAssembly;

		private DateTime lastdttm;


		private class ReleaseInfo
		{
			public string Album;
			public string Artist;
			public string ArtistID;
			public string Genre;
			public string Title;
			public string TrackNumber;
			public string Year;
		}


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize the class, overriding default settings with user configuration.
		/// </summary>

		static Tagger ()
		{
			DefaultDnsKey = "e4230822bede81ef71cde723db743e27";
			tagAssembly = null;

			string key = ConfigurationManager.AppSettings["DnsKey"];
			if (key != null)
			{
				key = key.Trim();
				try
				{
					// test if this is a proper guid
					Guid guid = new Guid(key);
					// passed the test so set the default
					DefaultDnsKey = key;
				}
				catch
				{
					// no-op
				}
			}
		}


		/// <summary>
		/// Initialize a new instance.
		/// </summary>

		public Tagger ()
		{
			this.lastdttm = DateTime.MinValue;
		}


		/// <summary>
		/// For unit testing.
		/// </summary>

		public XElement _ArtistRoot
		{
			get;
			set;
		}


		/// <summary>
		/// For unit testing.
		/// </summary>

		public XElement _PuidRoot
		{
			get;
			set;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Use reflection here to load an invoke the taglib-sharp assembly and types.  This way,
		/// we do not need to reference directly the DLL and can store it in the ThirdParty
		/// subdirectory.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public static void ReadFileTags (ITrackBasics track)
		{
			if (tagAssembly == null)
			{
				tagAssembly = Assembly.LoadFrom(@"ThirdParty\taglib-sharp.dll");
			}

			if (tagAssembly != null)
			{
				var type = tagAssembly.GetType("TagLib.File");
				var create = type.GetMethod("Create", new Type[] { typeof(String) });

				var file = create.Invoke(null, new object[] { track.Location }) as IDisposable;
				if (file != null)
				{
					TimeSpan span = (TimeSpan)tagAssembly.GetType("TagLib.Properties")
						.GetProperty("Duration").GetGetMethod()
						.Invoke(type.GetProperty("Properties").GetValue(file, null), null);

					track.Duration = (long)Math.Floor(span.TotalSeconds);
		
					var tag = type.GetProperty("Tag").GetGetMethod().Invoke(file, null);
					type = tagAssembly.GetType("TagLib.Tag");
					track.Album = type.GetProperty("Album").GetGetMethod().Invoke(tag, null) as String;
					track.Artist = type.GetProperty("FirstAlbumArtist").GetGetMethod().Invoke(tag, null) as String;
					track.Title = type.GetProperty("Title").GetGetMethod().Invoke(tag, null) as String;

					Logger.WriteLine(Logger.Level.Debug, "Tagger",
						String.Format("applied TagLib information ({0} | {1} | {2}) @ {3}s",
						track.Artist, track.Album, track.Title, track.Duration));
				}
			}
		}


		/// <summary>
		/// Retrieve the best possible tag information from a series of providers including
		/// genpuid MusicDns and MusicBrainz.
		/// </summary>
		/// <param name="path">Full path of the media file to analyze.</param>
		/// <returns>
		/// A new Tags object containing meta data for the media file.
		/// If metadata could not be found, the Tags.IsPopulated property is <b>false</b>.
		/// </returns>

		public void RetrieveTags (ITrackBasics track)
		{
			if (!File.Exists(track.Location))
			{
				track.UniqueID = null;
				return;
			}

			if (!NetworkStatus.IsAvailable)
			{
				return;
			}

			// enforce MusicBrainz min wait threshold of one second.  the probability of falling
			// waiting is low considering genpuid usually takes longer than a second anyway...
			DateTime now = DateTime.Now;
			TimeSpan span = now.Subtract(lastdttm);
			if (span.TotalMilliseconds < MinWaitTime)
			{
				Thread.Sleep(MinWaitTime - (int)span.TotalMilliseconds);
			}

			lastdttm = DateTime.Now;

			// genpuid can retrieve basic tag information
			RetrieveGenPUIDTags(track);

			// we have the basics, now override with more complete information
			if (track.IsAnalyzed)
			{
				RetrieveBrainzTags(track);
			}
		}


		#region RetrieveGenPUIDTags() with MusicDNS genpuid

		/// <summary>
		/// Invoke the genpuid utility in a subprocess with the given music file path.
		/// This reads metadata from the local track media file and generates a PUID/fingerprint
		/// that uniquely identifies this track; that PUID can then be used by MusicBrainz to
		/// retrieve quality metadata...
		/// </summary>
		/// <param name="path">The full path of the music file to analyze.</param>
		/// <returns>The analyzed Track including the PUID and any available metadata tags.</returns>

		private void RetrieveGenPUIDTags (ITrackBasics track)
		{
			ProcessStartInfo info = new ProcessStartInfo();
			info.Arguments = String.Format(@"{0} -xml ""{1}""", DefaultDnsKey, track.Location);
			info.CreateNoWindow = true;
			info.FileName = @"ThirdParty\genpuid.exe";
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;

			string xml = null;

			try
			{
				using (Process process = new Process())
				{
					process.StartInfo = info;
					process.Start();

					// PriorityClass must be set after process is started.
					process.PriorityClass = ProcessPriorityClass.BelowNormal;

					// read before waiting
					xml = Encoding.UTF8.GetString(
						process.StandardOutput.CurrentEncoding.GetBytes(
						process.StandardOutput.ReadToEnd()));

					if (process.ExitCode != 0)
					{
						string error = process.StandardError.ReadToEnd();
					}

					// ensures process terminates
					bool ok = process.WaitForExit(GenTimeout);
				}
			}
			catch (Exception exc)
			{
				Logger.WriteLine(LogCategory, "Error analyzing " + track.Location, exc);
				return;
			}

			if (!String.IsNullOrEmpty(xml))
			{
				ExtractGenPUIDTags(xml, track);
			}
		}


		/// <summary>
		/// Extract metadata from the genpuid generated XML report.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns>A Tags instance populated with extracted tag information.</returns>

		private void ExtractGenPUIDTags (string xml, ITrackBasics track)
		{
			XElement root;

			try
			{
				root = XElement.Parse(xml, LoadOptions.None);
			}
			catch (Exception exc)
			{
				Logger.WriteLine(LogCategory, "Error parsing analysis", exc);
				Logger.WriteLine(Logger.Level.Error, LogCategory, xml);
				return;
			}
#if Verbose
			Logger.WriteLine(Logger.Level.Debug, LogCategory,
				Environment.NewLine + root.ToString(SaveOptions.None) +
				Environment.NewLine);
#endif
			XNamespace ns = root.GetDefaultNamespace();

			var puid =
				(from a in root.Elements(ns + "track")
				 where a.Attribute(ns + "puid") != null
				 select a.Attribute(ns + "puid")).FirstOrDefault();

			if (puid != null)
			{
				track.UniqueID = puid.Value;
			}
		}

		#endregion RetrieveGenPUIDTags() with MusicDNS genpuid

		#region RetrieveBrainzTags() with MusicBrainz

		/// <summary>
		/// Fetch quality metadata for the given track and overwrite the track information
		/// with these results.
		/// </summary>
		/// <param name="track">The track to analyze and tag.</param>

		private void RetrieveBrainzTags (ITrackBasics track)
		{
			// query and parse PUID information
			var release = RetrieveReleaseInfo(track);

			if ((release == null) || String.IsNullOrEmpty(release.ArtistID))
			{
				// signals that we could not find any information for this recording
				track.UniqueID = null;
				return;
			}

			// query and parse Artist information (Artist/Band Web site URL)
			RetrieveArtistInfo(track, release.ArtistID);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <returns></returns>

		private ReleaseInfo RetrieveReleaseInfo (ITrackBasics track)
		{
			XElement root = _PuidRoot =
				QueryBrainz(String.Format(BrainzPuidUri, track.UniqueID));

			if (root == null)
			{
				return null;
			}

			XNamespace ns = root.GetDefaultNamespace();

			// While pretty lengthy and initially perhaps daunting, this rather elegant bit of
			// LINQ generates an ordered list of distinct recordings.  Given the shape of the XML
			// it was more efficient and straight-forward to start at the release level, pulling
			// out the title, track number, and date, and then move back up to the recording
			// element to read the artist and primary tag.  (Reversing this approach made it much
			// more complicated to read all release elements across multiple recordings.)
			//
			// After the initial select where we instantiate ReleaseInfo instances, we first
			// order by album, artist, title, and year - mainly to make the following groupby
			// more efficient, then filter by Distinct to remove exact duplicates leaving either
			// unique items or those that differ only in date.  So, finally, we group by album,
			// and then select only the first element from each album-group.
			//
			// Album  = metadata/puid/recording-list/recording/release-list/release/title
			// Artist = metadata/puid/recording-list/recording/artist-credit/name-credit/artist/sort-name
			// Genre  = metadata/puid/recording-list/recording/artist-credit/name-credit/artist/name/tag-list/tag/name
			// Title  = metadata/puid/recording-list/recording/title
			// Track# = metadata/puid/recording-list/recording/release-list/release/medium-list/medium/track-list/track/position
			// Year   = metadata/puid/recording-list/recording/release-list/release/date

			var releases =
				(from rel in root.Elements(ns + "puid")
					.Elements(ns + "recording-list").Elements(ns + "recording")
					.Elements(ns + "release-list").Elements(ns + "release")
				 let med = rel
					 .Elements(ns + "medium-list").Elements(ns + "medium")
					 .Elements(ns + "track-list").Elements(ns + "track").FirstOrDefault()
				 let dat = rel.Elements(ns + "date").FirstOrDefault()
				 let rec = rel.Parent.Parent
				 let art = rec
					 .Elements(ns + "artist-credit").Elements(ns + "name-credit")
					 .Elements(ns + "artist").FirstOrDefault()
				 let tag = rec.Elements(ns + "tag-list").Elements(ns + "tag").FirstOrDefault()
				 select new ReleaseInfo
				 {
					 Album = rel.Elements(ns + "title").FirstOrDefault().Value,
					 Artist = art.Element(ns + "name").Value,
					 ArtistID = (art.Attribute("id") == null ? String.Empty : art.Attribute("id").Value),
					 Genre = (tag == null ? String.Empty : tag.Element(ns + "name").Value),
					 Title = rec.Elements(ns + "title").FirstOrDefault().Value,
					 TrackNumber = (med == null ? "0" : med.Element(ns + "position").Value),
					 Year = (dat == null ? String.Empty : dat.Value)
				 }).OrderBy(x => x.Album).ThenBy(x => x.Artist).ThenBy(x => x.Title).ThenBy(x => x.Year)
				.Distinct()
				.GroupBy(c => c.Album, c => c,
					(key, elements) => new { Key = key, Item = elements.ElementAt(0) })
					.Select(r => r.Item);

			if ((releases == null) || (releases.Count() == 0))
			{
				return null;
			}

			// convert LINQ results to a more concrete List<> so we can requery the results
			// without a stack overflow.  When reusing local variables to filter results, the
			// deferred execution nature of LINQ will cause a stack overflow because the original
			// query is run again when that variable is referenced later on.

			var candidates = releases.ToList();

			ReleaseInfo release = null;

			if (candidates.Count() == 1)
			{
				// since the URI filters out all but Official releases, we're presuming
				// if there are multiple releases, we can safely choose the first one
				release = candidates.First();
				ReconcileReleaseInfo(track, release);
			}
			else
			{
				int index;

				// match by priority: 1) Title... 2) Artist... 3) Album
				// we keep filtering the result list until we narrow it down to the most
				// probable candidate based on the available fuzzy criteria

				if (!String.IsNullOrEmpty(track.Title))
				{
					// find the one entry with the best match for Title
					index = track.Title.SelectSimilar(candidates.Select(r => r.Title));

					if (index >= 0)
					{
						// select all entries with that same result Title
						var entries =
							from r in candidates
							where r.Title.Equals(candidates.ElementAt(index).Title)
							select r;

						// the filtered results now becomes our new release candidates list
						if ((entries != null) && (entries.Count() > 0))
						{
							candidates = entries.ToList();
						}
					}
				}

				// 2) Artist

				if ((candidates != null) && (candidates.Count() > 1) &&
					!String.IsNullOrEmpty(track.Artist))
				{
					index = track.Artist.SelectSimilar(candidates.Select(r => r.Artist));

					if (index >= 0)
					{
						var entries =
							from r in candidates
							where r.Artist.Equals(candidates.ElementAt(index).Artist)
							select r;

						if ((entries != null) && (entries.Count() > 0))
						{
							candidates = entries.ToList();
						}
					}
				}

				// 3) Album

				if ((candidates != null) && (candidates.Count() > 1) &&
					!String.IsNullOrEmpty(track.Album))
				{
					index = track.Album.SelectSimilar(candidates.Select(r => r.Album));

					if (index >= 0)
					{
						var entries =
							from r in candidates
							where r.Album.Equals(candidates.ElementAt(index).Album)
							select r;

						if ((entries != null) && (entries.Count() > 0))
						{
							candidates = entries.ToList();
						}
					}
				}

				if ((candidates != null) && (candidates.Count() > 0))
				{
					// found a probable match
					release = candidates.First();
				}
				else
				{
					// did not confidently find a match, so choose the first chronologically
					release = releases.OrderBy(r => r.Year).First();
				}

				if (release != null)
				{
					ReconcileReleaseInfo(track, release);
				}
			}

			return release;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="release"></param>

		private void ReconcileReleaseInfo (ITrackBasics track, ReleaseInfo release)
		{
			if (!String.IsNullOrEmpty(release.Album))
			{
				track.Album = release.Album;
			}

			if (!String.IsNullOrEmpty(release.Artist))
			{
				track.Artist = release.Artist;
			}

			if (!String.IsNullOrEmpty(release.Genre))
			{
				track.Genre = release.Genre;
			}

			if (!String.IsNullOrEmpty(release.Title))
			{
				track.Title = release.Title;
			}

			if (!String.IsNullOrEmpty(release.TrackNumber))
			{
				int number;
				if (Int32.TryParse(release.TrackNumber, out number))
				{
					if (number > 0)
					{
						track.TrackNumber = number;
					}
				}
			}

			DateTime dttm;
			if (DateTime.TryParse(release.Year, out dttm))
			{
				if ((dttm.Year > 1600) && (dttm.Year < DateTime.Now.Year))
				{
					track.Year = dttm.Year;
				}
			}
			else
			{
				int year = 0;
				if (Int32.TryParse(release.Year, out year))
				{
					if ((year >= 1600) && (year <= DateTime.Now.Year))
					{
						track.Year = year;
					}
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="artistID"></param>

		private void RetrieveArtistInfo (ITrackBasics track, string artistID)
		{
			var root = _ArtistRoot =
				QueryBrainz(String.Format(BrainzArtistUri, artistID));

			if (root == null)
			{
				return;
			}

			var ns = root.GetDefaultNamespace();

			// metadata/artist/relation-list/relation@type=url/target
			// seek the relation-list element which contains all (relation type="url") elements

			var relations =
				(from a in root.Elements(ns + "artist").Elements(ns + "relation-list")
				 where a.Attribute("target-type") != null &&
					   a.Attribute("target-type").Value.Equals("url")
				 select a).FirstOrDefault();

			if (relations != null)
			{
				// prioritorize URLs by 1) official homepage, 2) wiki, 3) IMDb
				var target =
					(from a in relations.Elements(ns + "relation")
					 let type = a.Attribute("type")
					 where type != null &&
						 (type.Value.Equals("official homepage") ||
						  type.Value.Equals("wikipedia") ||
						  type.Value.Equals("IMDb"))
					 select a).FirstOrDefault();

				// none of the priorities found so assume first available
				if (target == null)
				{
					target =
						(from a in relations.Elements(ns + "relation").Elements(ns + "target")
						 select a).FirstOrDefault();
				}

				if (target != null)
				{
					track.ArtistURL = target.Value;
				}
			}
		}


		/// <summary>
		/// Send a query to MusicBrainz.
		/// </summary>
		/// <param name="uri">The full URI query string.</param>
		/// <returns>The results parsed as an XElement.</returns>

		private XElement QueryBrainz (string uri)
		{
			string xml;
			XElement root = null;
#if Verbose
			Logger.WriteLine(Logger.Level.Debug, LogCategory,
				String.Format("QueryBrainz({0})", uri));
#endif
			using (WebClient client = new WebClient())
			{
				try
				{
					xml = client.DownloadString(uri);
				}
				catch (Exception exc)
				{
					if (exc.Message.Contains("(404"))
					{
						Logger.WriteLine(Logger.Level.Warn, LogCategory, String.Format(
							"Information not found at {0}", uri));
					}
					else
					{
						Logger.WriteLine(LogCategory, "QueryBrainz exception " + exc.Message, exc);
					}

					xml = null;
				}
			}

			if (!String.IsNullOrEmpty(xml))
			{
				try
				{
					root = XElement.Parse(xml, LoadOptions.None);
#if Verbose
					Logger.WriteLine(Logger.Level.Debug, LogCategory,
						Environment.NewLine + root.ToString(SaveOptions.None) +
						Environment.NewLine);
#endif
				}
				catch (Exception exc)
				{
					Logger.WriteLine(LogCategory, "QueryBrainz Error parsing result", exc);
					Logger.WriteLine(Logger.Level.Error, LogCategory, xml);
					return null;
				}
			}

			return root;
		}

		#endregion RetrieveBrainzTags() with MusicBrainz
	}
}
