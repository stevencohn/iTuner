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


	/// <summary>
	/// This lyrics provider queries the Lyrics007Provider service for lyrics of a specified song.
	/// </summary>

	internal class Lyrics007Provider : LyricsProviderBase
	{
		private static readonly string QueryFormat =
			"http://www.lyrics007.com/{0} Lyrics/{1} Lyrics.html";

		// [11 Sep 2014]
		// Pattern starts with <br><br>
		// Text continues with newline \n
		// There are ads in the middle delimeted by <div>..</div>
		// Pattern ends  with <center><iframe

		//private static readonly string pattern =
		//	@"(?:<br><br>)(?<aa>(.|\n)*)(?:<div(.|\n)*</div>)(?<bb>(.|\n)*)(?:<center><iframe)";

		/// <summary>
		/// Initialize the provider.
		/// </summary>

		public Lyrics007Provider ()
		{
			this.name = "Lyrics007";
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
					uri = Uri.EscapeUriString(
						String.Format(QueryFormat, song.Artist, song.Title));

					string result = client.DownloadString(uri);

					if (!String.IsNullOrEmpty(result))
					{
						var text = FindText(result);
						if (!String.IsNullOrEmpty(text))
						{
							lyrics = Flatten(text);
							lyrics = Unbreak(lyrics);
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
						Match match = Regex.Match(result, pattern, RegexOptions.IgnoreCase);

						if (match.Success)
						{
							string parts = match.Groups["aa"].Value + match.Groups["bb"].Value;
							lyrics = Flatten(parts);
							lyrics = Unbreak(lyrics);
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
			//<div class="lyrics">It's likely your browser's cookies are disabled.

			var start = result.IndexOf("<div class=\"lyrics\">");
			if (start < 0) return String.Empty;

			start += 20;
			if (result.StartsWith("It's likely your browser's cookies are disabled."))
				return String.Empty;

			var end = result.IndexOf("</div>", start);
			if (end < start) return String.Empty;

			var value = result.Substring(start, end - start + 1);
			return value;
		}
	}
}
