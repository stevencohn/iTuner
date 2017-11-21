//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.IO;
	using System.Text;
	using System.Xml;


	/// <summary>
	/// Provide clean access to a simple output text file.
	/// </summary>

	public class TextFileWriter : IDisposable
	{
		private readonly string fileName;
		private TextWriter writer;
		private bool isDisposed;


		/// <summary>
		/// Initialize a new instance that will write to the specified file path/name.
		/// </summary>
		/// <param name="fileName"></param>

		public TextFileWriter (string fileName)
		{
			this.fileName = fileName;
			writer = null;
			isDisposed = false;
		}


		/// <summary>
		/// Close this file and release internal resources.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				if (writer != null)
				{
					writer.Flush();
					writer.Close();
					writer.Dispose();
					writer = null;
				}

				isDisposed = true;
			}
		}


		/// <summary>
		/// Open the file for writing.
		/// </summary>
		/// <returns></returns>

		private bool EnsureWriter ()
		{
			if (writer == null)
			{
				if (!String.IsNullOrEmpty(fileName))
				{
					string path = PathHelper.Clean(fileName);
					Encoding encoding = GetEncodingWithFallback(new UTF8Encoding(false));

					try
					{
						writer = new StreamWriter(path, false, encoding, 0x1000);
					}
					catch
					{
						writer = null;
					}
				}
			}

			return (writer != null);
		}


		private static Encoding GetEncodingWithFallback (Encoding encoding)
		{
			Encoding copy = (Encoding)encoding.Clone();
			copy.EncoderFallback = EncoderFallback.ReplacementFallback;
			copy.DecoderFallback = DecoderFallback.ReplacementFallback;
			return copy;
		}


		/// <summary>
		/// Gets a reference to the internal TextWriter used by this TextFileWriter.
		/// </summary>
		/// <remarks>
		/// This should only be used by consumers who need the TextFileWriter to construct
		/// a different writer such as XmlTextWriter.
		/// </remarks>

		public XmlTextWriter GetXmlTextWriter ()
		{
			if (EnsureWriter())
			{
				return new XmlTextWriter(writer);
			}

			return null;
		}


		/// <summary>
		/// Write text to the file without a newline character.
		/// </summary>
		/// <param name="message"></param>

		public void Write (string message)
		{
			if (EnsureWriter())
			{
				writer.Write(message);
			}
		}


		/// <summary>
		/// Write text to the file including a newline character.
		/// </summary>
		/// <param name="message"></param>

		public void WriteLine (string message)
		{
			if (EnsureWriter())
			{
				writer.WriteLine(message);
			}
		}
	}
}