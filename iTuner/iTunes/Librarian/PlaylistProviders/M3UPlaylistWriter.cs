//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************
/*
#EXTM3U
#EXTINF:163,Crash Kings - 14 Arms
C:\Exported\Crash Kings\Crash Kings\14 Arms.mp3
#EXTINF:197,Crash Kings - Mountain Man
C:\Exported\Crash Kings\Crash Kings\Mountain Man.mp3
#EXTINF:218,Crash Kings - My Love
C:\Exported\Crash Kings\Crash Kings\My Love.mp3
#EXTINF:231,Crash Kings - Non Believer
C:\Exported\Crash Kings\Crash Kings\Non Believer.mp3
#EXTINF:211,Crash Kings - Saving Grace
C:\Exported\Crash Kings\Crash Kings\Saving Grace.mp3
#EXTINF:222,Crash Kings - 1985
C:\Exported\Crash Kings\Crash Kings\1985.mp3
#EXTINF:237,Crash Kings - Raincoat
C:\Exported\Crash Kings\Crash Kings\Raincoat.mp3
*/

namespace iTuner.iTunes
{
	using System;
	using System.IO;


	/// <summary>
	/// 
	/// </summary>

	internal class M3UPlaylistWriter : PlaylistWriterBase
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="createSubdirectories"></param>

		public M3UPlaylistWriter (string root, string name, bool createSubdirectories)
			: base(root, name, ".m3u", createSubdirectories)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>

		public override void Add (Track track, string path)
		{
			//#EXTINF:123,Sample Artist - Sample title
			//C:\Documents and Settings\I\My Music\Sample.mp3

			WriteLine(String.Format("#EXTINF:{0},{1} - {2}", track.Duration, track.Artist, track.Name));

			if (createSubdirectories)
			{
				WriteLine(path);
			}
			else
			{
				WriteLine(Path.GetFileName(path));
			}
		}


		/// <summary>
		/// 
		/// </summary>

		protected override void WriteFooter ()
		{
		}


		/// <summary>
		/// 
		/// </summary>

		protected override void WriteHeader ()
		{
			WriteLine("#EXTM3U");
		}
	}
}
