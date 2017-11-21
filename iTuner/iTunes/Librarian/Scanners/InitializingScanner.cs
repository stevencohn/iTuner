//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using Resx = Properties.Resources;


	/// <summary>
	/// Not actually a library scanner but rather an initialization step.  We're simply
	/// queueing this through the librarian pipline because it's time consuming.
	/// </summary>

	internal class InitializingScanner : ScannerBase
	{
		private Librarian librarian;


		/// <summary>
		/// Initialize a new instance of this scanner.
		/// </summary>
		/// <param name="itunes"></param>

		public InitializingScanner (Controller controller, Librarian librarian)
			: base(Resx.I_ScanInitialize, controller, librarian.Catalog)
		{
			base.description = Resx.ScanInitialize;
			base.tooltip = controller.LibraryXMLPath;

			this.librarian = librarian;
		}


		/// <summary>
		/// Instantiates the catalog, loading and caching the iTunes LibraryXML file.
		/// </summary>

		public override void Execute ()
		{
			Logger.WriteLine(base.name, "Initialization beginning");

			try
			{
				librarian.Catalog.Initialize(controller.LibraryXMLPath);
			}
			catch (Exception exc)
			{
				Logger.WriteLine(base.name, exc);

				Logger.WriteLine(base.name, "Reverting to FlatCatalog");
				librarian.Catalog = new FlatCatalog();
				librarian.Catalog.Initialize(controller.LibraryXMLPath);
			}

			librarian.Initialize();

			Logger.WriteLine(base.name, "Initialization completed");
		}
	}
}
