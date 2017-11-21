//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************
/*
<?xml version="1.0"?>
<?wpl version="1.0"?>
<smil>
  <head>
    <title>Max Playlist</title>
    <Generator>iTuner v1.2.3782</Generator>
  </head>
  <body>
    <seq>
      <media src="C:\Exported\The Dresden Dolls\The Dresden Dolls\Coin-Operated Boy.mp3" tid="c9a6baed-bdd4-4a91-b7ca-874e97e6ab00" />
      <media src="C:\Exported\Crash Kings\Crash Kings\14 Arms.mp3" tid="4e68304f-d9b3-45a3-93e0-7f9b29578a91" />
      <media src="C:\Exported\Crash Kings\Crash Kings\Mountain Man.mp3" tid="ce7c982d-54bf-4f59-9ec6-8191713f8fde" />
      <media src="C:\Exported\Crash Kings\Crash Kings\My Love.mp3" tid="fb27ba7a-335c-47a2-9a22-0674359bc83e" />
      <media src="C:\Exported\Crash Kings\Crash Kings\Non Believer.mp3" tid="4a54600f-1df1-4e82-bb1d-8bea9a5c9b4f" />
      <media src="C:\Exported\Crash Kings\Crash Kings\Saving Grace.mp3" tid="a58d0f38-fcf9-45eb-adfb-77adb04de5c1" />
      <media src="C:\Exported\Crash Kings\Crash Kings\1985.mp3" tid="f88b9e2d-406b-4cee-8f34-4c47181afeec" />
      <media src="C:\Exported\Crash Kings\Crash Kings\Raincoat.mp3" tid="9ed4eda4-35a8-475a-9889-0fd08ae4839d" />
    </seq>
  </body>
</smil>
*/

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using System.Xml;


	internal class WPLPlaylistReader : PlaylistReaderBase
	{
		private Stream stream;
		private XmlReaderSettings settings;
		private XmlReader reader;
		private bool isMediaElement;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>

		public WPLPlaylistReader (string path)
			: base(path)
		{
			this.isMediaElement = false;

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
				reader = null;
			}

			if (stream != null)
			{
				stream.Close();
				stream.Dispose();
				stream = null;
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
			if (isMediaElement)
			{
				if (reader.MoveToAttribute("src"))
				{
					if (reader.ReadAttributeValue())
					{
						string src = reader.Value;
						isMediaElement = reader.ReadToFollowing("media");

						return src;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Open the playlist, including writing the inheritor-implemented header.
		/// </summary>

		protected override void Open ()
		{
			base.Open();

			try
			{
				stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

				settings = new XmlReaderSettings();
				settings.IgnoreComments = true;
				settings.IgnoreProcessingInstructions = true;
				settings.IgnoreWhitespace = true;

				// ProhibitDtd and XmlResolver set to completely ignore the DTD element
				// of the library XML file; otherwise, we get an exception when the
				// network adapter is in a suspicious state
				settings.DtdProcessing = DtdProcessing.Ignore;
				settings.XmlResolver = null;

				reader = XmlReader.Create(stream, settings);
				isMediaElement = reader.ReadToDescendant("media");
			}
			catch (Exception)
			{
				stream = null;
			}
		}
	}
}
