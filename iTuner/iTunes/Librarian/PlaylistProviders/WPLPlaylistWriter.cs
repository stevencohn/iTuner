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


	/// <summary>
	/// 
	/// </summary>

	internal class WPLPlaylistWriter : PlaylistWriterBase
	{
		private XmlTextWriter writer;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="createSubdirectories"></param>

		public WPLPlaylistWriter (string root, string name, bool createSubdirectories)
			: base(root, name, ".wpl", createSubdirectories)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>

		public override void Add (Track track, string path)
		{
			writer.WriteStartElement("media");

			if (createSubdirectories)
			{
				writer.WriteAttributeString("src", path);
			}
			else
			{
				writer.WriteAttributeString("src", Path.GetFileName(path));
			}

			writer.WriteAttributeString("tid", Guid.NewGuid().ToString());
			writer.WriteEndElement(); // media
		}


		/// <summary>
		/// 
		/// </summary>

		protected override void WriteFooter ()
		{
			writer.WriteEndElement(); // seq
			writer.WriteEndElement(); // body
			writer.WriteEndElement(); // smil
		}


		/// <summary>
		/// 
		/// </summary>

		protected override void WriteHeader ()
		{
			writer = base.GetXmlTextWriter();
			writer.Formatting = Formatting.Indented;

			writer.WriteProcessingInstruction("xml", "version=\"1.0\"");
			writer.WriteProcessingInstruction("wpl", "version=\"1.0\"");
			writer.WriteStartElement("smil");

			writer.WriteStartElement("head");
			writer.WriteElementString("title", base.name + " Playlist");
			writer.WriteElementString("Generator", App.NameVersion);
			writer.WriteEndElement(); // head

			writer.WriteStartElement("body");
			writer.WriteStartElement("seq");
		}
	}
}
