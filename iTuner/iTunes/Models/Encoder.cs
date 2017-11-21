//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Linq;
	using iTunesLib;
	using Resx = Properties.Resources;


	/// <summary>
	/// A safe wrapper of an IITEncoder object.
	/// </summary>

	internal class Encoder : Interaction
	{
		private static string[] knownExtensions = new string[]
		{
			".aif", ".aifc", ".aiff", ".cda", ".m4b",
			".mp2", ".mp3", ".m4a", ".m4p", ".wav"
		};

		private IITEncoder encoder;
		private string format;
		private string name;


		/// <summary>
		/// Initialize a new instance that wraps the given iTunes encoder COM object.
		/// </summary>
		/// <param name="encoder">An IITEncoder instance.</param>

		public Encoder (IITEncoder encoder)
			: base()
		{
			this.encoder = encoder;
			if (encoder == null)
			{
				this.format = "?";
				this.name = Resx.EncoderNone.ToLower();
			}
			else
			{
				this.format = encoder.Format;
				this.name = encoder.Name;
			}
		}


		/// <summary>
		/// Interaction.Cleanup override; release reference to internal encoder.
		/// </summary>

		protected override void Cleanup (bool finalRelease)
		{
			if (finalRelease)
			{
				Release(encoder);
				encoder = null;
			}
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Gets the internal iTunes IITEncoder object.
		/// </summary>

		public IITEncoder AsInternal
		{
			get { return encoder; }
		}


		/// <summary>
		/// Gets the expected file type of files that would be encoded by this encoder.
		/// </summary>

		public string ExpectedType
		{
			get
			{
				if (encoder == null)
				{
					return null;
				}

				switch (format)
				{
					// AAC Encoder
					case "AAC":
						return ".aac";

					// AIFF Encoder
					case "AIFF":
						return ".aif";

					// Lossless Encoder
					case "Apple Lossless":
						return ".m4a";

					// MP3 Encoder
					case "MP3":
						return ".mp3";

					// WAV Encoder
					case "WAV":
						return ".wav";
				}

				return null;
			}
		}


		/// <summary>
		/// Gets the data format created by this encoder, such as "AAC".
		/// </summary>

		public string Format
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return encoder == null ? "?" : encoder.Format;
				});
			}
		}


		/// <summary>
		/// Gets a Boolean value indicating whether this encoder is empty; this indicates
		/// that no encoding should be performed when exporting - just a straight copy.
		/// </summary>

		public bool IsEmpty
		{
			get { return encoder == null; }
		}


		/// <summary>
		/// Determines if the specified file extension is one of the iTunes native extensions,
		/// implying that it could be directly added to the library without conversion.
		/// </summary>
		/// <param name="ext"></param>
		/// <returns></returns>

		public static bool IsNativeExtension (string ext)
		{
			return knownExtensions.Any(p => p.Equals(ext));
		}


		/// <summary>
		/// Gets the name of this encoder, such as "AAC Encoder".
		/// </summary>

		public string Name
		{
			get
			{
				return Invoke((Func<string>)delegate
				{
					return encoder == null ? Resx.EncoderNone.ToLower() : encoder.Name;
				});
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>

		public override string ToString ()
		{
			return encoder == null ? Resx.EncoderNone : encoder.Name;
		}
	}
}
