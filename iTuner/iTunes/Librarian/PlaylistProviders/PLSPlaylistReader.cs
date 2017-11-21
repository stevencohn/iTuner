//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using System.Text.RegularExpressions;


	internal class PLSPlaylistReader : PlaylistReaderBase
	{
		private const string EntryPattern = @"File\d+=(.*)";

		private StreamReader reader;
		private Regex pattern;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>

		public PLSPlaylistReader (string path)
			: base(path)
		{
			Open();
		}


		/// <summary>
		/// Close the playlist, including writing the inheritor-implemented footer.
		/// </summary>

		protected override void Close ()
		{
			base.Close();

			if (reader != null)
			{
				reader.Close();
				reader.Dispose();
				reader = null;
			}
		}


		/// <summary>
		/// Gets the full path of the next track specified in the playlist file.
		/// </summary>
		/// <returns>
		/// A string specifying the full path of a file or <b>null</b> if there are no
		/// further tracks available.
		/// </returns>

		public override string GetNext ()
		{
			bool found = false;
			string line = null;

			// entries are described by multiple lines of key=value pairs including
			// File, Title, and Length.  But we are only interested in the File lines.
			// Each set of lines are numbered by appending digits to the tags, such as
			// File1, File2, File3.

			while (!found && !reader.EndOfStream)
			{
				line = reader.ReadLine().Trim();
				if (line.Length > 0)
				{
					Match match = pattern.Match(line);
					if (match != null)
					{
						if (match.Captures.Count > 0)
						{
							line = match.Groups[1].Value;
							found = true;
						}
					}
				}
			}

			return found ? line : null;
		}


		/// <summary>
		/// Open the playlist, including writing the inheritor-implemented header.
		/// </summary>

		protected override void Open ()
		{
			base.Open();

			reader = new StreamReader(path);
			pattern = new Regex(EntryPattern);
		}
	}
}
