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
	/// This lyrics provider queries the Lyrdb service for lyrics of a specified song.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Lyrdb provides a two-step mechanism for discovering lyrics.  In the first step, an
	/// index of lyric entires is returned for the given song where each entry is a slight
	/// variation of the lyrics.  In the second step, we iterate this index, asking for
	/// lyrics associated with the current entry, stopping on the first reasonable response.
	/// </para>
	/// <para>
	/// The index packet looks similar to the following:
	/// <![CDATA[
	///     9323\Walk On Water\Aerosmith
	///     s00015177\Walk On Water\Aerosmith
	///     t00187085\Walk On Water\Aerosmith<!--f0c25b539901624b460e129d15264305-->
	/// ]]>
	/// </para>
	/// <para>
	/// Or even just the following, which means there are no entries available.
	/// <![CDATA[
	///     <!--f0c25b539901624b460e129d15264305-->
	/// ]]>
	/// </para>
	/// <para>
	/// </para>
	/// </remarks>

	internal class LyrdbLyricsProvider : LyricsProviderBase
	{
		private static readonly string IndexFormat =
			"http://webservices.lyrdb.com/lookup.php?q={0}|{1}&for=match&agent={2}/{3}";

		private static readonly string LyricsFormat =
			"http://webservices.lyrdb.com/getlyr.php?q={0}";

		private string programName;
		private string programVersion;


		/// <summary>
		/// 
		/// </summary>

		public LyrdbLyricsProvider ()
		{
			this.name = "Lyrdb";

			this.programName = Properties.Resources.I_ApplicationTitle;

			Assembly assembly = Assembly.GetExecutingAssembly();
			Version version = assembly.GetName().Version;

			this.programVersion = String.Format(
				"{0}.{1}.{2}",
				version.Major, version.Minor, version.Build);
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

			using (WebClient client = new WebClient())
			{
				try
				{
					// seek based on preferred Artist and Title
					string uri = Uri.EscapeUriString(String.Format(
						IndexFormat, song.Artist, song.Title, programName, programVersion));

					index = client.DownloadString(uri);

					// if not found then swap Artist and Title and try again
					if (String.IsNullOrEmpty(index))
					{
						uri = Uri.EscapeUriString(String.Format(
							IndexFormat, song.Title, song.Artist, programName, programVersion));

						index = client.DownloadString(uri);
					}

					if (!String.IsNullOrEmpty(index))
					{
						// walk index until we find lyrics
						foreach (string line in index.Split('\n'))
						{
							int offset = line.IndexOf('\\');
							if (offset > 0)
							{
								string id = line.Substring(0, offset);

								uri = String.Format(LyricsFormat, id);

								lyrics = client.DownloadString(uri);

								if (!(String.IsNullOrEmpty(lyrics) || lyrics.StartsWith("error:")))
								{
									// remove the GUID from the end of the lyrics text
									offset = lyrics.IndexOf("<!--");
									if (offset > 0)
									{
										lyrics = lyrics.Substring(0, offset).Trim();
									}

									lyrics = Encode(lyrics);
									break;
								}
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
