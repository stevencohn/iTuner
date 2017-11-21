//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;
	using Resx = Properties.Resources;


	/// <summary>
	/// Scan for and empty directories beneath the music library root.  iTunes typically 
	/// leaves empty directories after a reorganization.
	/// </summary>

	internal class EmptyScanner : ScannerBase
	{
		private int count;


		/// <summary>
		/// Initialize a new instance of this scanner.
		/// </summary>
		/// <param name="itunes"></param>

		public EmptyScanner (Controller controller, ICatalog catalog)
			: base(Resx.I_ScanEmptyDirectories, controller, catalog)
		{
			base.description = Resx.ScanEmpty;
			base.tooltip = base.description;

			this.count = 0;
		}


		/// <summary>
		/// Execute the scanner.
		/// </summary>

		public override void Execute ()
		{
			Logger.WriteLine(base.name, "Empty scanner beginning");

			Scan(new DirectoryInfo(base.catalog.MusicPath));

			Logger.WriteLine(base.name, String.Format(
				"Empty scanner completed, analyzed {0} directories", count));
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>

		private void Scan (DirectoryInfo root)
		{
			count++;

			DirectoryInfo[] directories = root.GetDirectories();
			foreach (DirectoryInfo dir in directories)
			{
				if (base.isActive)
				{
					// skip the iTunes 9.x auto-drop directory
					if (dir.Name.Equals(Properties.Resources.iTunesAutoDirectory))
					{
						continue;
					}

					Scan(dir);

					if (IsEmptyish(dir))
					{
						Logger.WriteLine(base.name, "Deleting empty directory " + dir.FullName);

						try
						{
							if (ScannerBase.isLive)
							{
								DeleteDirectory(dir);
							}
						}
						catch (Exception exc)
						{
							Logger.WriteLine(base.name,
								"Error deleting empty directory " + dir.FullName, exc);
						}
					}
				}
				else
				{
					Logger.WriteLine(base.name, "Empty scanner cancelled");
					break;
				}
			}
		}


		/// <summary>
		/// In order for a directory to be considering empty, it must either contain no
		/// directories and no files, or only contain hidden non-media files (ini, db, jpg)
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>

		private bool IsEmptyish (DirectoryInfo dir)
		{
			// skip all reparse points
			if ((dir.Attributes & FileAttributes.ReparsePoint) > 0)
			{
				return false;
			}

			// Directory.GetXxx is more efficient than DirectoryInfo.GetXxx
			if (Directory.GetDirectories(dir.FullName).Length > 0)
			{
				return false;
			}

			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				if (((file.Attributes & FileAttributes.Hidden) == 0) ||
					((file.Attributes & (FileAttributes.ReadOnly | FileAttributes.ReparsePoint)) > 0))
				{
					return false;
				}
				else
				{
					string name = file.Name.ToLower();

					if (!(name.Equals("desktop.ini") || name.Equals("thumbs.db") ||
						file.Extension.ToLower().Equals(".jpg")))
					{
						Logger.WriteLine(base.name, "Skipping " + dir.FullName);
						return false;
					}
				}
			}

			return true;
		}


		/// <summary>
		/// Delete the given directory including all of its files.
		/// </summary>
		/// <param name="dir"></param>

		private void DeleteDirectory (DirectoryInfo dir)
		{
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				if ((file.Attributes & FileAttributes.ReadOnly) > 0)
				{
					file.Attributes ^= FileAttributes.ReadOnly;
				}

				file.Delete();
			}

			if ((dir.Attributes & FileAttributes.ReadOnly) > 0)
			{
				dir.Attributes ^= FileAttributes.ReadOnly;
			}

			dir.Delete(true);
		}
	}
}
