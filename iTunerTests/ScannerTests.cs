//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Text;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTunesLib;
	using iTuner.iTunes;


	/// <summary>
	/// </summary>

	[TestClass]
	public class ScannerTests : TestBase
	{
		private static Controller controller;
		private static ICatalog catalog;


		[ClassInitialize()]
		public static void MyClassInitialize (TestContext testContext)
		{
			controller = new Controller();
			catalog = new FlatCatalog();
			catalog.Initialize(controller.LibraryXMLPath);
		}
	

		[TestMethod]
		public void DuplicateScanner ()
		{
			DuplicateScanner scanner = new DuplicateScanner(controller, catalog);
			scanner.Execute();
		}


		[TestMethod]
		public void DuplicateScannerInAlbum ()
		{
			// for this test, create new 'ituner' playlist and add duplicate entries..

			DuplicateScanner scanner = new DuplicateScanner(controller, catalog);
			scanner.PlaylistFilter = PersistentID.Parse("612F0BCDC4E08C4E");
			scanner.Execute();
		}


		[TestMethod]
		public void EmptyScanner ()
		{
			EmptyScanner scanner = new EmptyScanner(controller, catalog);
			scanner.Execute();
		}


		[TestMethod]
		public void PhantomScanner ()
		{
			PhantomScanner scanner = new PhantomScanner(controller, catalog);
			scanner.ArtistFilter = "pomplamoose";
			scanner.AlbumFilter = "pomplamoose video songs";
			scanner.Execute();
		}


		[TestMethod]
		public void PathTest ()
		{
			string value = "file://localhost/C:/Music/";
			Uri uri = new Uri(value);

			StringBuilder builder = new StringBuilder();
			foreach (string seg in uri.Segments)
			{
				// check builder.Length first and then seg value
				if ((builder.Length > 0) || !seg.Equals("/"))
				{
					builder.Append(seg);
				}
			}

			string path = builder.Replace('/', '\\').ToString();

			Assert.AreEqual(@"C:\Music\", path);
		}
	}
}
