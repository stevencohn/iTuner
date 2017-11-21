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

	internal abstract class PlaylistWriterBase : IPlaylistWriter
	{
		protected string root;					// path to the root of the export directory
		protected string name;					// base name of the playlist
		protected string extension;				// file name extension
		protected bool createSubdirectories;	// true if subdirectory structure
		private bool isDisposed;				// true if instnace disposed
		private TextFileWriter writer;			// output writer


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="createSubdirectories"></param>

		public PlaylistWriterBase (
			string root, string name, string extension, bool createSubdirectories)
		{
			this.root = root;
			this.name = name;
			this.extension = extension;
			this.createSubdirectories = createSubdirectories;
			this.isDisposed = false;

			Open();
		}


		/// <summary>
		/// 
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				if (writer != null)
				{
					Close();
				}

				isDisposed = true;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="path"></param>

		public abstract void Add (Track track, string path);


		/// <summary>
		/// 
		/// </summary>

		public void Close ()
		{
			try
			{
				WriteFooter();
			}
			finally
			{
				writer.Dispose();
				writer = null;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>

		protected XmlTextWriter GetXmlTextWriter ()
		{
			return writer.GetXmlTextWriter();
		}


		/// <summary>
		/// 
		/// </summary>

		public void Open ()
		{
			string path = Path.Combine(root, name + extension);
			writer = new TextFileWriter(path);

			WriteHeader();
		}


		/// <summary>
		/// 
		/// </summary>

		protected abstract void WriteFooter ();


		/// <summary>
		/// 
		/// </summary>

		protected abstract void WriteHeader ();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>

		protected void WriteLine (string text)
		{
			writer.WriteLine(text);
		}
	}
}
