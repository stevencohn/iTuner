//************************************************************************************************
// Copyright © 2012 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Resx = iTuner.Properties.Resources;


	/// <summary>
	/// Some simple extensions to the System.String class.
	/// </summary>

#if LINQPad
	public static class StringExtensions
#else
	internal static class StringExtensions
#endif
	{
		private static readonly string ThePrefix;
		private static readonly string TheSuffix;

		/// <summary>
		/// Static constructor.
		/// </summary>

		static StringExtensions ()
		{
			ThePrefix = Resx.The + " ";
			TheSuffix = ", " + Resx.The;
		}


		//========================================================================================
		// Equivalent()
		//========================================================================================

		/// <summary>
		/// Test for special equivalence between two strings.  Whitespace is irrelevant.
		/// We also assume beginning "The " and ending ", The" are irrelevant.
		/// </summary>
		/// <param name="current">The string to test.</param>
		/// <param name="candidate">The string to use as a comparison.</param>
		/// <returns></returns>
		/// <remarks>
		/// Since we're using this in iTuner specificall for artist/album/title comparisons,
		/// we can assume a few things.  First, we strip "The " from the beginning of strings;
		/// we also strip ", The" from the end of strings.  We strip digits and optionally
		/// hyphens from the beginning of string incase this indicates a track number.  Once
		/// we have a trimmed string, we compare only LETTERs assuming anything else is
		/// irrelevant, such as whitespace and punctuations, e.g., "Mr. "
		/// </remarks>

		public static bool Equivalent (this string current, string candidate)
		{
			// test absolute values

			if (current.Equals(candidate))
			{
				return true;
			}

			// we're going to index through both strings but first want to set the min/max
			// indecies based on whether the strings begin with "the " or ends with ", the".
			// also skip any leading digits assuming that might be a track number, "12" or "12 -"

			int cur = StartIndex(current);
			int can = StartIndex(candidate);

			int curmax = EndIndex(current);
			int canmax = EndIndex(candidate);

			// compare...
			while ((cur <= curmax) && (can <= canmax))
			{
				// skip any non-letter in current
				while ((cur <= curmax) && !char.IsLetter(current[cur]))
				{
					cur++;
				}

				// skip any non-letter in candidate
				while ((can <= canmax) && !char.IsLetter(candidate[can]))
				{
					can++;
				}

				// now compare the current index characters
				if ((cur <= curmax) && (can <= canmax))
				{
					if (current[cur] == candidate[can])
					{
						cur++;
						can++;
					}
					else
					{
						break;
					}
				}
			}

			return (cur > curmax) && (can > canmax);
		}


		/// <summary>
		/// Find the starting index of the given string, exclude the prefix "The " and any
		/// sequential counter, 01, 02, 03 or 1, 2, 3.
		/// </summary>
		/// <param name="current">The string to examine.</param>
		/// <returns>The starting index within current skipping optional prefixes.</returns>

		private static int StartIndex (string current)
		{
			// skip starting digits and possibly a hyphen
			// this is common in track names, such as "01 - Cool Title"
			// but is also sometimes present in album names, such as "2004 - Feedback"

			int index = 0;
			while ((index < current.Length) &&
				(char.IsWhiteSpace(current[index]) || char.IsDigit(current[index]) ||
				(current[index] == '-')))
			{
				index++;
			}

			// skip prefix of "The "
			// this is common in album names and artist names

			if ((index < current.Length - ThePrefix.Length) &&
				current
					.Substring(index, ThePrefix.Length)
					.Equals(ThePrefix, StringComparison.InvariantCultureIgnoreCase))
			{
				index += ThePrefix.Length;
			}

			return index;
		}


		/// <summary>
		/// Find the ending index of the given string, excluding the suffix ", The".
		/// </summary>
		/// <param name="current">The string to examine.</param>
		/// <returns>The ending index within current minus an optional ", The" suffix.</returns>

		private static int EndIndex (string current)
		{
			if (current.EndsWith(TheSuffix, StringComparison.InvariantCultureIgnoreCase))
			{
				return current.Length - (TheSuffix.Length + 1);
			}

			return current.Length - 1;
		}


		//========================================================================================
		// SelectSimilar()
		//========================================================================================

		/// <summary>
		/// This String extension accepts a list of candidate strings for comparison against
		/// the current value and finds the closest match with a distance score above 50%.
		/// </summary>
		/// <param name="current">The current instance.</param>
		/// <param name="candidates">A list of candidate string for comparison.</param>
		/// <returns>
		/// The index of the best match for the current instance.
		/// </returns>

		public static int SelectSimilar (this string current, IEnumerable<string> candidates)
		{
			// remove irrelevant words from current
			current = Prepare(current);
			if (current.Length == 0)
			{
				throw new ArgumentNullException(nameof(current));
			}

			var bestScore = 0;
			var bestIndex = -1;

			var enumerable = candidates as string[] ?? candidates.ToArray();
			for (var i = 0; i < enumerable.Length; i++)
			{
				var candidate = enumerable.ElementAt(i);

				// remove irrelevant words from this candidate
				var can = Prepare(candidate);
				if (can.Length > 0)
				{
					// determine the similarity score
					var score = current.Similarity(can);

					// need to match more than 50%
					if ((score > 50) && (score > bestScore))
					{
						// select our current winner (finally winner TBD)
						bestScore = score;
						bestIndex = i;
					}
				}
			}

			// we have a winner!  or at least a close facsimile
			return bestIndex;
		}


		/// <summary>
		/// Use the preparation method, StartIndex and EndIndex, from the Equivalent extension
		/// method to strip irrelevant bits from the given string.
		/// </summary>
		/// <param name="current">The string to prepare.</param>
		/// <returns>The inner substring of current stripped of irrelevent information.</returns>

		private static string Prepare (string current)
		{
			if (current != null)
			{
				int ax = StartIndex(current);
				int bx = EndIndex(current);
				string tmp = current.Substring(ax, bx - ax + 1).Trim();

				// if we've totally emptied the string then that doesn't help us at all!
				// so just return the entire string as is...
				return tmp.Length == 0 ? current : tmp;
			}

			return String.Empty;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="current"></param>
		/// <param name="candidate"></param>
		/// <returns></returns>

		public static int Similarity (this string current, string candidate)
		{
			// start off really optimistic!
			if (current.Equals(candidate, StringComparison.InvariantCultureIgnoreCase))
			{
				return 100;
			}

			var separators = new[] { ' ', ',', '.', '-', '\t', '\r', '\n' };

			// ok, back to reality, break up strings into words
			var curwords = new List<string>(
				current.ToLower().Split(separators, StringSplitOptions.RemoveEmptyEntries));

			var canwords = new List<string>(
				candidate.ToLower().Split(separators, StringSplitOptions.RemoveEmptyEntries));

			// compare shorest string to longest string, otherwise we're just wasting CPU
			List<string> shorter;
			List<string> longer;

			if (curwords.Count > canwords.Count)
			{
				shorter = canwords;
				longer = curwords;
			}
			else
			{
				shorter = curwords;
				longer = canwords;
			}

			// match each shorter-string against each longer-string.  When a match is discovered
			// assume that longer-string is "used" and do not use it again; this is always
			// forward-looking, we will never look behind within the longer collection.

			int score = 0;
			int index = 0;
			foreach (string s in shorter)
			{
				int i = longer.IndexOf(s, index);
				if (i >= index)
				{
					// exact match, so reord and move up the index
					score += s.Length;
					index = i;
				}
				else
				{
					// find similar match, advancing index;
					// this may skip words that are too different
					score += BestDistance(s, longer, ref index);
				}
			}

			var length = longer.Sum(w => w.Length);
			return (int)(score / (double)length * 100.0);
		}


		/// <summary>
		/// A simplified variation of the Levenshtein distance algorithm, optimized for our
		/// needs, this does a forward-only comparison of characters in the subject string
		/// with each character of the candidate strings, selecting the closest match.
		/// </summary>
		/// <param name="subject">The string to find within the candidates.</param>
		/// <param name="candidates">The list of strings to compare against the subject.</param>
		/// <param name="startIndex">The starting index within the candidate list.</param>
		/// <returns>
		/// The candidate with the closest match above 50%.
		/// </returns>

		private static int BestDistance (
			string subject, IReadOnlyList<string> candidates, ref int startIndex)
		{
			int best = 0;
			int bestIndex = 0;
			for (int six = startIndex; six < candidates.Count; six++)
			{
				string candidate = candidates[six];

				int index = 0;
				int score = 0;
				foreach (char c in subject)
				{
					int i = candidate.IndexOf(c, index);
					if (i >= index)
					{
						score++;
					}
				}

				// we need to match at least 50% of string for even close similarity
				if ((score > (candidate.Length / 2)) && (score > best))
				{
					best = score;
					bestIndex = six;
				}
			}

			if (bestIndex > 0)
			{
				startIndex = bestIndex;
			}

			return best;
		}
	}
}
