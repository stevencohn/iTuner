//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Text;
	using System.Text.RegularExpressions;

	
	/// <summary>
	/// Abstract base class for all lyrics providers.
	/// </summary>
	/// <remarks>
	/// Consecutive failures are allowed only up to a maximum threshold.  At each successfull
	/// discovery, the failure count is reset to zero.  If the consecutive failures reach
	/// the threshold then this provider is permanently disabled (for this Process session).
	/// </remarks>

	internal abstract class LyricsProviderBase : ILyricsProvider
	{
		private const int MaxFailures = 5;

		protected const string NotFound404 = "(404)";
		//private static readonly string pattern = "(\r)(?:[^\n])|(\n)(?:[^\r])";

		protected int failures = 0;
		protected string name;


		/// <summary>
		/// Gets a Boolean value indicating whether this provider has successfully
		/// connected.  This should be checked after each request.  If not connected
		/// then the provide should be ignored.
		/// </summary>

		public bool IsConnected
		{
			get { return failures < MaxFailures; }
		}


		/// <summary>
		/// Gets the name of this lyrics provider.  Inheritors must set the protected
		/// <i>name</i> field in their constructors.
		/// </summary>

		public string Name
		{
			get { return name; }
		}


		/// <summary>
		/// Retrieve the lyrics for the given song
		/// </summary>
		/// <param name="song">The song whose lyrics are to be fetched</param>
		/// <returns>The lyrics or an empty string if the lyrics could not be found</returns>

		public abstract string RetrieveLyrics (ISong song);


		/// <summary>
		/// Clean up the lyrics and encode into Unicode to preserve special characters.
		/// </summary>
		/// <param name="lyrics"></param>
		/// <returns></returns>

		protected string Encode (string text)
		{
			Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
			return Encoding.UTF8.GetString(encoding.GetBytes(text.Trim()));
		}


		/// <summary>
		/// Remove all carriage return and vertical line feed characters from the
		/// string so we can format it ourselves using BR (HTML Break) hints.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>

		protected string Flatten (string text)
		{
			return text.Replace("\r", String.Empty).Replace("\n", String.Empty);
		}


		/// <summary>
		/// Determines if the text is actually an HTML referrer, <![CDATA[<A HREF=""/>]]>
		/// redirecting us to a search or alternate page for actual lyrics.  We consider
		/// this a failure and must attempt to use another provider.
		/// </summary>
		/// <param name="lyrics">The lyrics to examine.</param>
		/// <returns></returns>

		protected bool IsReferrer (string lyrics)
		{
			// simplified, but we can always retrieve again without hurting anyone!
			return lyrics.StartsWith("<") || lyrics.EndsWith(">");
		}


		/// <summary>
		/// Replace all instances of BR tags (HTML Break) with proper NewLine sequences.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>

		protected string Unbreak (string text)
		{
			MatchCollection matches = Regex.Matches(
				text, @"(<\s*br\s*/?\s*>)", RegexOptions.IgnoreCase);

			if (matches.Count > 0)
			{
				var builder = new StringBuilder(text);
				for (int i = matches.Count - 1; i >= 0; i--)
				{
					builder.Remove(matches[i].Index, matches[i].Length);
					builder.Insert(matches[i].Index, Environment.NewLine);
				}

				return builder.ToString();
			}

			return text;
		}
	}
}
