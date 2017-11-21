//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using System.Windows.Threading;


	/// <summary>
	/// Represents the details of a codeplex release item retrieved from the Releases RSS feed.
	/// </summary>

	internal class UpgradeRelease
	{

		/// <summary>
		/// Initialize a new instance with the specified details.
		/// </summary>
		/// <param name="title">The full release title.</param>
		/// <param name="uri">The release link URI</param>
		/// <param name="pubDate">The release publication date.</param>
		/// <param name="description">The release descripts or release notes.</param>

		public UpgradeRelease (string title, string uri, string pubDate, string description)
		{
			Title = title;
			Description = description;
			Uri = uri;
			ReleaseDate = DateTime.Parse(pubDate);

			// extract the version string (major.minor.build) from the title

			Match match = new Regex(@"\d+\.\d+.\d+").Match(this.Title);
			if (match != null)
			{
				ReleaseVersion = new Version(match.Value);
			}

			// get the current version information

			var assembly = Assembly.GetExecutingAssembly();
			CurrentVersion = assembly.GetName().Version;

			var file = new FileInfo(assembly.Location);
			CurrentDate = file.CreationTime;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Gets the creation date/time of the executing assembly.  This is considered th
		/// current application's release date.
		/// </summary>

		public DateTime CurrentDate { get; private set; }


		/// <summary>
		/// Gets the version of the currently executing assembly.
		/// </summary>

		public Version CurrentVersion { get; private set; }


		/// <summary>
		/// Gets the release notes or description of the most recent release described as HTML.
		/// </summary>

		public string Description { get; private set; }


		/// <summary>
		/// An internal reference to the UI dispatcher used by the BackgroundWorker
		/// to invoke back to the UI thread.
		/// </summary>

		public Dispatcher Dispatcher { get; set; }


		/// <summary>
		/// Gets the date/time of the most recent release.
		/// </summary>

		public DateTime ReleaseDate { get; private set; }


		/// <summary>
		/// Gets the version details of the most recent release.
		/// </summary>

		public Version ReleaseVersion { get; private set; }


		/// <summary>
		/// Gets the full title string of the most recent release.
		/// </summary>

		public string Title { get; private set; }


		/// <summary>
		/// Gets the URL of the release download page on codeplex.
		/// </summary>

		public string Uri { get; private set; }


		/// <summary>
		/// Gets a Boolean value indicating whether an upgrade is available.
		/// </summary>
		/// <remarks>
		/// Compares the current application version against the most recent release details.
		/// </remarks>

		public bool IsUpgradeAvailable => (CurrentVersion.CompareTo(ReleaseVersion) < 0);
	}
}
