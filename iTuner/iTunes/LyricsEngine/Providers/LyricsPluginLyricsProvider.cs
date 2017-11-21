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


	/// <summary>
	/// This lyrics provider queries the LyricsPlugin service for lyrics of a specified song.
	/// </summary>
	/// <remarks>
	/// LyricsPlugin provides a one-step mechanism for discovering lyrics.  The response
	/// is an HTML document where the lyrics are available in a <![CDATA[<DIV>]]> element
	/// identified by the "id" attribute value of "lyrics".
	/// </remarks>

	internal class LyricsPluginLyricsProvider : LyricsProviderBase
	{
		private static readonly string QueryFormat =
			"http://www.lyricsplugin.com/plugin/?title={0}&artist={1}";

		private static readonly string StartMarker = @"<div id=""lyrics"">";
		private static readonly string EndMarker = "</div>";
		private static readonly string BreakMarker = "<br />";


		/// <summary>
		/// Initialize the provider.
		/// </summary>

		public LyricsPluginLyricsProvider ()
		{
			this.name = "LyricsPlugin";
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

			string lyrics = String.Empty;

			using (WebClient client = new WebClient())
			{
				try
				{
					string uri = Uri.EscapeUriString(
						String.Format(QueryFormat, song.Title, song.Artist));

					uri = uri.Replace("%20", "+");

					string result = client.DownloadString(uri);

					if (!String.IsNullOrEmpty(result))
					{
						// @"<div id=""lyrics"">(*?)</div>";

						int start = result.IndexOf(StartMarker);
						if (start > 0)
						{
							start += StartMarker.Length;
							int end = result.IndexOf(EndMarker, start);
							if (end > start)
							{
								lyrics = result
									.Substring(start, end - start)
									.Replace(BreakMarker, "");

								// clean it up
								lyrics = Encode(lyrics);
							}
						}
					}

					failures = 0;
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
