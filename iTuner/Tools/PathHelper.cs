//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;
	using Resx = Properties.Resources;


	/// <summary>
	/// 
	/// </summary>

	public static class PathHelper
	{
		private static string appDataPath = null;
		private static string comDataPath = null;


		/// <summary>
		/// Gets the path of the local application data directory,
		/// for example "C:\Users\steven\AppData\Local\River Software\iTuner"
		/// </summary>

		public static string ApplicationDataPath
		{
			get
			{
				if (appDataPath == null)
				{
					appDataPath = CleanDirectoryPath(Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						String.Format(
							CultureInfo.CurrentCulture, @"{0}{1}{2}",
							Resx.I_ApplicationCompany,
							Path.DirectorySeparatorChar,
							Resx.I_ApplicationProduct)));
				}

				return appDataPath;
			}
		}


		/// <summary>
		/// Gets the path of the common application data directory,
		/// for example "C:\ProgramData\River Software\iTuner"
		/// </summary>

		public static string CommonDataPath
		{
			get
			{
				if (comDataPath == null)
				{
					comDataPath = CleanDirectoryPath(Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
						String.Format(
							CultureInfo.CurrentCulture, @"{0}{1}{2}",
							Resx.I_ApplicationCompany,
							Path.DirectorySeparatorChar,
							Resx.I_ApplicationProduct)));
				}

				return comDataPath;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public static string Clean (string path)
		{
			path = Path.GetFullPath(path);

			StringBuilder dirpath = new StringBuilder(Path.GetDirectoryName(path));
			StringBuilder filname = new StringBuilder(Path.GetFileName(path));

			// replace invalid chars with underscore
			foreach (char ch in Path.GetInvalidPathChars())
			{
				dirpath.Replace(ch, '_');
			}

			foreach (char ch in Path.GetInvalidFileNameChars())
			{
				filname.Replace(ch, '_');
			}

			return Path.Combine(dirpath.ToString(), filname.ToString());
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>

		public static string CleanDirectoryPath (string path)
		{
			path = Path.GetFullPath(path);

			StringBuilder dirpath = new StringBuilder(path);

			// replace invalid chars with underscore
			foreach (char ch in Path.GetInvalidPathChars())
			{
				dirpath.Replace(ch, '_');
			}

			return dirpath.ToString();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>

		public static string CleanFileName (string fileName)
		{
			StringBuilder filname = new StringBuilder(fileName);

			// replace invalid chars with underscore
			foreach (char ch in Path.GetInvalidFileNameChars())
			{
				filname.Replace(ch, '_');
			}

			return filname.ToString();
		}


		/// <summary>
		/// Converts a URI of the form "file:///C:/Music/" to a local path like "C:\Music"
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>

		public static string GetLocalPath (Uri uri)
		{
			// there must be a nicer way of taking a URI of the form file:///C:/Music/
			// and normalizing it into something like C:\Music

			StringBuilder builder = new StringBuilder();
			foreach (string seg in uri.Segments)
			{
				// check builder.Length first and then seg value
				if ((builder.Length > 0) || !seg.Equals("/"))
				{
					builder.Append(seg.Replace("%20", " "));
				}
			}

			string path = builder.Replace('/', '\\').ToString();
			return path;
		}


		/// <summary>
		/// Retrieve the path format associated with the given resource ID.
		/// </summary>
		/// <param name="resID">The resource ID of the path format.</param>
		/// <returns></returns>

		public static string GetPathFormat (string resID)
		{
			// determine folder path layout

			XElement root = XElement.Parse(Resx.I_FolderFormats);
			XNamespace ns = root.GetDefaultNamespace();

			string format = ((XAttribute)
				(from node in root.Elements()
				 where node.Attribute(ns + "FID").Value == resID
				 select node.Attribute(ns + "pattern")).Single()).Value;

			return format;
		}
	}
}