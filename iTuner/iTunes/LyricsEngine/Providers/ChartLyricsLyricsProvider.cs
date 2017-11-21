//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Diagnostics;
	using System.Net;
	using System.Reflection;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Linq;
	using System.Xml.Linq;


	/// <summary>
	/// This lyrics provider queries the ChartLyrics service for lyrics of a specified song.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ChartLyrics provides a two-step mechanism for discovering lyrics.  In the first step, an
	/// index of entires is returned for the given song where each entry specifies an Id and a
	/// Checksum; these are used as keys in the second step.
	/// </para>
	/// <para>
	/// <![CDATA[
	///     <ArrayOfSearchLyricResult ...>
	///       <SearchLyricResult>
	///         <LyricChecksum>2a3b73342fcfbfa6d88cfb68c44dfed1</LyricChecksum> 
	///         <LyricId>1710</LyricId> 
	///         <SongUrl>http://www.chartlyrics.com/28h-8gWvNk-Rbj1X-R7PXg/Bad.aspx</SongUrl> 
	///         <ArtistUrl>http://www.chartlyrics.com/28h-8gWvNk-Rbj1X-R7PXg.aspx</ArtistUrl> 
	///         <Artist>Michael Jackson</Artist> 
	///         <Song>Bad</Song> 
	///         <SongRank>9</SongRank> 
	///       </SearchLyricResult>
	///     </ArrayOfSearchLyericResult>
	/// ]]>
	/// </para>
	/// <para>
	/// In the second step, we use the id and checksum to lookup the lyrics
	/// </para>
	/// <para>
	/// <![CDATA[
	///     <GetLyricResult ...>
	///       <LyricSong>Bad</LyricSong> 
	///       <LyricArtist>Michael Jackson</LyricArtist> 
	///       <LyricUrl>http://www.chartlyrics.com/28h-8gWvNk-Rbj1X-R7PXg/Bad.aspx</LyricUrl>
	///       <LyricCovertArtUrl>http://www.chartlyrics.com/cover/m2EqEH6DLUm4RlwMeLwX6A.aspx</LyricCovertArtUrl>
	///       <LyricRank>9</LyricRank> 
	///       <LyricCorrectUrl>http://www.chartlyrics.com/app/correct.aspx?lid=MQA3ADEAMAA=</LyricCorrectUrl> 
	///       <Lyric>Your butt is mine... </Lyric> 
	///     </GetLyricResult>
	/// ]]>
	/// </para>
	/// </remarks>

	internal class ChartLyricsLyricsProvider : LyricsProviderBase
	{
		private static readonly string IndexFormat =
			"http://api.chartlyrics.com/apiv1.asmx/SearchLyric?artist={0}&song={1}";

		private static readonly string LyricsFormat =
			"http://api.chartlyrics.com/apiv1.asmx/GetLyric?lyricId={0}&lyricChecksum={1}";


		/// <summary>
		/// Initialize the provider.
		/// </summary>

		public ChartLyricsLyricsProvider ()
		{
			this.name = "ChartLyrics";
		}


		/// <summary>
		/// Retrieve the lyrics for the given song
		/// </summary>
		/// <param name="song">The song whose lyrics are to be fetched</param>
		/// <returns>The lyrics or an empty string if the lyrics could not be found</returns>

		public override string RetrieveLyrics (ISong song)
		{
			// clean the title; we don't need quotes
			string title = Regex.Replace(song.Title, "['\"]", "");

			string index = String.Empty;
			string lyrics = String.Empty;
			XNamespace ns;

			using (WebClient client = new WebClient())
			{
				try
				{
					// seek based on preferred Artist and Title
					string uri = Uri.EscapeUriString(
						String.Format(IndexFormat, song.Artist, song.Title)).Replace("%20", " ");

					Logger.WriteLine(Logger.Level.Debug,
						base.name, String.Format("URI-1 [{0}]", uri));

					index = client.DownloadString(uri);

					// if not found then swap Artist and Title and try again
					if (String.IsNullOrEmpty(index))
					{
						uri = Uri.EscapeUriString(
							String.Format(IndexFormat, song.Title, song.Artist)).Replace("%20", " ");

						Logger.WriteLine(Logger.Level.Debug,
							base.name, String.Format("URI-1B [{0}]", uri));

						// pay no attention to the following line, nothing to see here, move it
						// along...!  actually, ChartLyrics misbehaves if you query it too fast;
						// after a quick e-mail exchange, they've confirmed that their servers are
						// under-powered and suggested a 5 sec window in between requests.  Hmph.

						Thread.Sleep(5000);
						index = client.DownloadString(uri);
					}

					if (!String.IsNullOrEmpty(index))
					{
						XElement root = XElement.Parse(index);
						ns = root.GetDefaultNamespace();

						var results =
							from artistNode in root
								.Elements(ns + "SearchLyricResult").Elements(ns + "Artist")
							from songNode in root
								.Elements(ns + "SearchLyricResult").Elements(ns + "Song")
							where (artistNode.Parent == songNode.Parent) &&
								(artistNode.Value == "Cut Copy") &&
								(songNode.Value == "Time Stands Still")
							select artistNode.Parent;

						if (results != null)
						{
							string checksum =
								(from node in results.Elements(ns + "LyricChecksum")
								 select node.Value).FirstOrDefault() as String;

							string id =
								(from node in results.Elements(ns + "LyricId")
								 select node.Value).FirstOrDefault() as String;

							if (!String.IsNullOrEmpty(checksum) &&
								(!String.IsNullOrEmpty(id) && !id.Equals("0")))
							{
								// second step...
								uri = String.Format(LyricsFormat, id, checksum);

								Logger.WriteLine(Logger.Level.Debug,
									base.name, String.Format("URI-2 [{0}]", uri));

								Thread.Sleep(5000);

								lyrics = client.DownloadString(uri);

								if (!String.IsNullOrEmpty(lyrics))
								{
									root = XElement.Parse(lyrics);
									lyrics = root.Element(ns + "Lyric").Value;

									// found lyrics so break out and complete
									lyrics = Encode(lyrics);
								}
							}
						}

						failures = 0;
					}
				}
				catch (Exception exc)
				{
					Logger.WriteLine(base.name, exc);

					failures++;
					lyrics = String.Empty;
				}
			}

			return lyrics;
		}
	}
}
