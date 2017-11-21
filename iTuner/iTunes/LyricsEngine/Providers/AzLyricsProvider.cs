//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Net;
	using System.Text.RegularExpressions;


	/// <summary>
	/// This lyrics provider queries the AzLyrics service for lyrics of a specified song.
	/// </summary>

	internal class AzLyricsProvider : LyricsProviderBase
	{
		private static readonly string QueryFormat =
			"http://www.azlyrics.com/lyrics/{0}/{1}.html";

		//private static readonly string pattern =
		//	"(?<=<!-- Usage of azlyrics.com content by any third-party lyrics provider is prohibited by our licensing agreement.Sorry about that. -->)((.|\n)*)(?=</div>)";


		/// <summary>
		/// Initialize the provider.
		/// </summary>

		public AzLyricsProvider ()
		{
			this.name = "AzLyrics";
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
			string uri = String.Empty;

			using (WebClient client = new WebClient())
			{
				try
				{
					string artist =
						song.Artist.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase)
						? song.Artist.Substring(4)
						: song.Artist;

					uri = Uri.EscapeUriString(String.Format(
						QueryFormat,
						artist.Replace(" ", String.Empty).ToLower(),
						song.Title.Replace(" ", String.Empty).ToLower()));

					string result = client.DownloadString(uri);

					if (!String.IsNullOrEmpty(result))
					{
						var text = FindText(result);
						if (!String.IsNullOrEmpty(text))
						{
							lyrics = Flatten(text);

							lyrics = Unbreak(lyrics)
								.Replace("<i>", "")
								.Replace("</i>", "");

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
						/*
						Match match = Regex.Match(result, pattern);
						if (match.Success)
						{
							lyrics = Flatten(match.Value);

							lyrics = Unbreak(lyrics)
								.Replace("<i>", "")
								.Replace("</i>", "");

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
						*/
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


		private string FindText (string result)
		{
			var start = result.IndexOf("<!-- Usage of azlyrics.com");
			if (start < 0) return String.Empty;

			start = result.IndexOf("-->", start);
			if (start < 0) return String.Empty;

			start += 3;

			var end = result.IndexOf("</div>", start);
			if (end < start) return String.Empty;

			var value = result.Substring(start, end - start + 1);
			return value;
		}
	}
}
