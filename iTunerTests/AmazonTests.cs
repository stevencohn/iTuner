//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Threading;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner.Amazon;


	/// <summary>
	/// 
	/// </summary>

	[TestClass]
	public class AmazonTests : TestBase
	{

		[TestMethod]
		public void GetArtwork ()
		{
			GetArtwork("Crosby, Stills, Nash & Young", "Déjà Vu");
			GetArtwork("Pink Floyd", "Dark Side of the Moon");
			GetArtwork("The B-52's", "Cosmic Thing");
		}


		private void GetArtwork (string artist, string album)
		{
			var service = new ArtworkService();
			byte[] data = service.GetArtwork(artist, album);

			Assert.IsNotNull(data);
			Assert.AreNotEqual(0, data.Length);

			string path = GetArchivePath();
			if (path != null)
			{
				path = Path.Combine(path,
					String.Format("{0}_{1}{2}", artist, album, Path.GetExtension(service.FileName)));

				if (File.Exists(path))
				{
					File.Delete(path);
				}

				Console.WriteLine("Creating file " + path);

				using (var writer = File.Create(path))
				{
					writer.Write(data, 0, data.Length);
				}
			}
		}


		private string GetArchivePath ()
		{
			// C:\Users\steven\AppData\Local\Temp
			string path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");

			bool success = Directory.Exists(path);

			if (!success)
			{
				try
				{
					Directory.CreateDirectory(path);
					success = true;
				}
				catch
				{
					success = false;
				}
			}

			return success ? path : null;
		}
	}
}
