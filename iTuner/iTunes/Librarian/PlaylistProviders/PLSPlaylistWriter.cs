//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************
/*
[playlist]
File1=C:\Exported\Crash Kings\Crash Kings\14 Arms.mp3
Title1=14 Arms
Length1=163
File2=C:\Exported\Crash Kings\Crash Kings\Mountain Man.mp3
Title2=Mountain Man
Length2=197
File3=C:\Exported\Crash Kings\Crash Kings\My Love.mp3
Title3=My Love
Length3=218
File4=C:\Exported\Crash Kings\Crash Kings\Non Believer.mp3
Title4=Non Believer
Length4=231
File5=C:\Exported\Crash Kings\Crash Kings\Saving Grace.mp3
Title5=Saving Grace
Length5=211
File6=C:\Exported\Crash Kings\Crash Kings\1985.mp3
Title6=1985
Length6=222
File7=C:\Exported\Crash Kings\Crash Kings\Raincoat.mp3
Title7=Raincoat
Length7=237
NumberOfEntries=7
Version=2
*/

namespace iTuner.iTunes
{
	using System;
	using System.IO;


	/// <summary>
	/// 
	/// </summary>

	internal class PLSPlaylistWriter : PlaylistWriterBase
	{
		private const int Version = 2;
		private int count;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="createSubdirectories"></param>

		public PLSPlaylistWriter (string root, string name, bool createSubdirectories)
			: base(root, name, ".pls", createSubdirectories)
		{
			count = 0;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>

		public override void Add (Track track, string path)
		{
			//File2=http://example.com/song.mp3
			//Title2=Remote MP3
			//Length2=286

			count++;

			if (createSubdirectories)
			{
				WriteLine(String.Format("File{0}={1}", count, path));
			}
			else
			{
				WriteLine(String.Format("File{0}={1}", count, Path.GetFileName(path)));
			}

			WriteLine(String.Format("Title{0}={1}", count, track.Name));
			WriteLine(String.Format("Length{0}={1}", count, track.Duration));
		}


		/// <summary>
		/// 
		/// </summary>

		protected override void WriteFooter ()
		{
			WriteLine(String.Format("NumberOfEntries={0}", count));
			WriteLine(String.Format("Version={0}", Version));
		}


		/// <summary>
		/// 
		/// </summary>

		protected override void WriteHeader ()
		{
			WriteLine("[playlist]");
		}
	}
}
