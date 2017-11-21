//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using System.Xml;


	/// <summary>
	/// 
	/// </summary>

	internal class ZPLPlaylistWriter : PlaylistWriterBase
	{
		private XmlTextWriter writer;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="createSubdirectories"></param>

		public ZPLPlaylistWriter (string root, string name, bool createSubdirectories)
			: base(root, name, ".zpl", createSubdirectories)
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
			writer.WriteProcessingInstruction("zpl", "version=\"1.0\"");
			writer.WriteStartElement("smil");

			writer.WriteStartElement("head");
			writer.WriteElementString("title", base.name + " Playlist");
			writer.WriteElementString("generator", App.NameVersion);
			writer.WriteEndElement(); // head

			writer.WriteStartElement("body");
			writer.WriteStartElement("seq");
		}
	}
}
