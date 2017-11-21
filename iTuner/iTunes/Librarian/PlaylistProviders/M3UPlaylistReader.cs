//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;


	internal class M3UPlaylistReader : PlaylistReaderBase
	{

		private const string EntryTag = "#EXTINF:";

		private StreamReader reader;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>

		public M3UPlaylistReader (string path)
			: base(path)
		{
			Open();
		}


		/// <summary>
		/// Close the playlist, including writing the inheritor-implemented footer.
		/// </summary>

		//public void Close ()
		protected override void  Close()
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
			bool tagged = false;
			string line = null;

			// entry lines come in pairs where the first line contains the EntryTag
			// and the second line is the path

			while (!found && !reader.EndOfStream)
			{
				line = reader.ReadLine().Trim();
				if (line.Length > 0)
				{
					if (line.StartsWith(EntryTag))
					{
						// we found an entry tag so continue reading until we find the entry
						tagged = true;
						line = reader.ReadLine().Trim();
						found = (line.Length > 0);
					}
					else if (tagged)
					{
						// we found an entry tag in a previous loop iteration so now we've
						// found the entry and can return
						found = true;
					}
				}
			}

			return line;
		}


		/// <summary>
		/// Open the playlist, including writing the inheritor-implemented header.
		/// </summary>

		protected override void Open ()
		{
			base.Open();

			reader = new StreamReader(path);
		}
	}
}
