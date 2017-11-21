//************************************************************************************************
// Copyright © 2011 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using iTuner.Tools;

	//<div id="lyrics_text" linenums="33" class="nocopy lyricsdisplaynoscriptNOTOK">
	//  <span id="findmorespan">Find more similar lyrics on <a href="http://mp3lyrics.com/cjV" id="findmorea">http://mp3lyrics.com/cjV</a></span>		
	//</div>

	/// <summary>
	/// This lyrics provider queries the MP3Lyrics.org service for lyrics of a specified song.
	/// </summary>

	internal class MP3LyricsProvider : LyricsProviderBase
	{
		private static readonly string QueryFormat = "http://www.mp3lyrics.org/{0}/{1}/{2}/";

		private static readonly string pattern =
			"(?<=<div id=\"lyrics_text\".*?>)((.|\n)*?)(?=</div>)";

		private static readonly string advertisingPattern = "(<span.*/span>)";


		/// <summary>
		/// Initialize the provider.
		/// </summary>

		public MP3LyricsProvider ()
		{
			this.name = "MP3Lyrics";
		}


		/// <summary>
		/// Retrieve the lyrics for the given song
		/// </summary>
		/// <param name="song">The song whose lyrics are to be fetched</param>
		/// <returns>The lyrics or an empty string if the lyrics could not be found</returns>

		public override string RetrieveLyrics (ISong song)
		{
			// clean the title; we don't need quotes
			string title = song.Title.Replace("\"", "");

			string lyrics = String.Empty;
			string uri = String.Empty;

			using (WebClient client = new WebClient())
			{
				try
				{
					string category = song.Artist.Substring(0, 1);
					if (Char.IsNumber(category, 0))
						category = "0-9";

					string artist = song.Artist.ToLower().Replace(" ", "-");
					title = title.ToLower().Replace(" ", "-").Replace("'", "-");

					uri = Uri.EscapeUriString(
						String.Format(QueryFormat, category, artist, title));

					string result = client.DownloadString(uri);

					if (!String.IsNullOrEmpty(result))
					{
						Match match = Regex.Match(result, pattern);

						if (match.Success)
						{
							lyrics = match.Value;
							match = Regex.Match(lyrics, advertisingPattern);
							if (match.Success)
							{
								var builder = new StringBuilder(lyrics);
								lyrics = builder.Remove(match.Index, match.Length).ToString();
							}

							lyrics = Unbreak(lyrics)
								.Replace("<i/>", String.Empty)
								.Replace("<i>", String.Empty)
								.Replace("<u/>", String.Empty)
								.Replace("<u>", String.Empty);

							lyrics = Encode(lyrics);

							if (IsReferrer(lyrics))
							{
								lyrics = String.Empty;
								failures++;
							}
							else
							{
								failures = 0;
							}
						}
						else
						{
							failures++;
						}
					}
					else
					{
						failures++;
					}
				}
				catch (Exception exc)
				{
#if Verbose
					if (exc.Message.Contains(LyricsProviderBase.NotFound404))
					{
						Logger.WriteLine(Logger.Level.Warn, base.name, "Lyrics not found");
					}
					else
#else
					if (!exc.Message.Contains(LyricsProviderBase.NotFound404))
#endif
					{
						Logger.WriteLine(base.name, exc);
						Logger.WriteLine(Logger.Level.Error, base.name,
							String.Format("URI [{0}]", uri));
					}

					failures++;
					lyrics = String.Empty;
				}
			}

			return lyrics;
		}
	}
}
