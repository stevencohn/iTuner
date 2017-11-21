//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

#define TestNO

namespace iTuner
{
	using System;
	using System.Linq;
	using System.Net;
	using System.Windows;
	using System.Windows.Threading;
	using System.Xml.Linq;
	using System.ComponentModel;
	using Resx = Properties.Resources;


	/// <summary>
	/// Retrieves the codeplex Releases RSS feed for this project and, if a new release
	/// is available, displays an upgrade dialog to the user.
	/// </summary>

	internal static class UpgradeHelper
	{
#if !Test
		private static readonly string ReleaseRss =
			"http://ituner.codeplex.com/Project/ProjectRss.aspx?ProjectRSSFeed=codeplex%3a%2f%2frelease%2fituner";
#endif

		private static BackgroundWorker worker;
		private static bool isRunning;
		private static bool isUserInvoked;


		/// <summary>
		/// Begins an asynchronous background request to the retrieve recent release information.
		/// </summary>
		/// <param name="dispatcher">The main UI dispatcher.</param>

		public static void CheckUpgrades (Dispatcher dispatcher)
		{
			CheckUpgrades(dispatcher, false);
		}


		public static void CheckUpgrades (Dispatcher dispatcher, bool isUserInvoked)
		{
			if (NetworkStatus.IsAvailable)
			{
				if (!isRunning)
				{
					isRunning = true;
					UpgradeHelper.isUserInvoked = isUserInvoked;

					worker = new BackgroundWorker();
					worker.DoWork += DoWork;
					worker.RunWorkerCompleted += DoWorkCompleted;
					worker.WorkerSupportsCancellation = true;
					worker.RunWorkerAsync(dispatcher);
				}
			}
		}


		/// <summary>
		/// Retrieve the Releases RSS feed and parse the items to determine the most recent
		/// release.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private static void DoWork (object sender, DoWorkEventArgs e)
		{
			string xml = null;

			try
			{
#if Test
				xml =
@"<?xml version=""1.0""?>
<?xml-stylesheet type=""text/xsl"" href=""http://www.codeplex.com/rss.xsl""?>
<rss version=""2.0"">
  <channel>
	<title>ituner Releases Rss Feed</title>
	<link>http://ituner.codeplex.com/Release/ProjectReleases.aspx</link>
	<description>ituner Releases Rss Description</description>
	<item>
	  <title>Updated Release: iTuner 99.12.3456 Beta 2 (Mar 27, 2010)</title>
	  <link>http://ituner.codeplex.com/releases/view/42534</link>
	  <description>
		&lt;div class=""wikidoc""&gt;V1.2 allows you to synchronize one or more iTunes playlists to a USB MP3 player. Beta 2 resolves all known issues.  This continues the evolution yet maintains the minimalistic approach of iTuner. This beta release is ostensibly feature complete and stable, providing the final UI, overall functionality, and simplicity requested by you, the users.  For developers, this feature shows how to enumerate USB disk drives, detect state changes in drives as they come online or go offline, and synchronize a specific playlist with a selected USB drive or music player.&lt;br /&gt;&lt;br /&gt;The final release of V1.2 will provide these additional features&lt;br /&gt;
		&lt;ul&gt;&lt;li&gt;A Librarian status panel showing active and queued Librarian scanners.  This will be hidden behind the &amp;quot;big buttons&amp;quot; on the main window and can be displayed by clicking a toggle button similar to the toggle button in the Track info status panel at the top.&lt;/li&gt;
		&lt;li&gt;An automated library import feature that scans the Library folders and imports any rogue files that have not yet been included in iTunes.&lt;/li&gt;&lt;/ul&gt;
		&lt;i&gt;Beta 2 Notes&lt;/i&gt;&lt;br /&gt;
		&lt;ul&gt;&lt;li&gt;Synchronize one or more iTunes playlists with a USB MP3 player&lt;/li&gt;
		&lt;li&gt;Fix #6240 to allow multiple playlists with the same name&lt;/li&gt;
		&lt;li&gt;Fix #6241 to properly parse multiple results from ChartLyrics&lt;/li&gt;
		&lt;li&gt;Fix #6242 to properly position windows relative to docked taskbar&lt;/li&gt;
		&lt;li&gt;Fix to auto-detect folder capabilities feature of synchronizer&lt;/li&gt;
		&lt;li&gt;Fix to Synchronize dialog when using playlist in folder layout&lt;/li&gt;
		&lt;li&gt;Fix to Export dialog to properly italicize &amp;quot;No encoder&amp;quot; item&lt;/li&gt;
		&lt;li&gt;Fix to Export dialog to correctly interperet encoder ComboBox&lt;/li&gt;&lt;/ul&gt;&lt;/div&gt;&lt;div class=""ClearBoth""&gt;&lt;/div&gt;
	  </description>
	  <author>stevenmcohn</author>
	  <pubDate>Sun, 28 Mar 2010 02:03:35 GMT</pubDate>
	  <guid isPermaLink=""false"">Updated Release: iTuner 1.2.3738 Beta 2 (Mar 27, 2010) 20100328020335A</guid>
	</item>
  </channel>
</rss>";
#else
				Logger.WriteLine(Logger.Level.Info, "Upgrade", 
					$"Checking for upgrades for current version '{App.NameVersion}'");

				// call home to the codeplex project Releases RSS feed
				using (WebClient client = new WebClient())
				{
					xml = client.DownloadString(ReleaseRss);
				}
#endif
			}
			catch (Exception exc)
			{
				Logger.WriteLine("UpgradeWeb", exc);
			}

			if (!string.IsNullOrEmpty(xml))
			{
				UpgradeRelease release;

				Logger.WriteLine(Logger.Level.Info, "Upgrade", "Parsing release information");

				try
				{
					XElement root = XElement.Parse(xml, LoadOptions.None);
					XNamespace ns = root.GetDefaultNamespace();

					// This is specific to the title string used in the iTuner project where
					// title are of the form:
					//
					//     Updated Release: iTuner 1.2.3738 Beta 2 (Mar 27, 2010)
					//
					// The version string is always of the format "major.minor.build" but the
					// build part might be exactly "xxxx" for "planned" releases.  Since releases
					// are given in reverse-chronological order, we can extract the first item
					// ignoring any items that contains ".xxxx"

					release =
						(from item in
							 ((from x in root
								   .Elements("channel")
								   .Elements("item")
							   where !(x.Element("title").Value.Contains(".xxxx") ||
									   x.Element("title").Value.Contains("Beta"))
							   select x).Take(1))
						 select new UpgradeRelease(
							 item.Element("title").Value,
							 item.Element("link").Value,
							 item.Element("pubDate").Value,
							 item.Element("description").Value
						 )).Single();
				}
				catch (Exception exc)
				{
					Logger.WriteLine("Upgrade", exc);
					release = null;
				}

				release.Dispatcher = e.Argument as Dispatcher;

				e.Result = release;
			}
		}


		/// <summary>
		/// If we have a valid release and it is later than the current running release
		/// then display the Upgrade dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		static void DoWorkCompleted (object sender, RunWorkerCompletedEventArgs e)
		{
			UpgradeRelease release = e.Result as UpgradeRelease;

			if (release == null)
			{
				isRunning = false;

				if (isUserInvoked)
				{
					isUserInvoked = false;

					MessageWindow.Show(
						Resx.NoUpdateConnection, Resx.I_ApplicationTitle,
						MessageBoxButton.OK, MessageWindowImage.OK);
				}

				return;
			}

			if (!release.Dispatcher.CheckAccess())
			{
				release.Dispatcher.BeginInvoke((Action)delegate { DoWorkCompleted(sender, e); });
				return;
			}

			if (release.IsUpgradeAvailable)
			{
				Logger.WriteLine(Logger.Level.Info, "Upgrade",
					$"Upgrade is available, {release.CurrentVersion} -> {release.ReleaseVersion}");

				isRunning = false;
				UpgradeWindow dialog = new UpgradeWindow(release);
				dialog.ShowDialog();
			}
			else if (isUserInvoked)
			{
				Logger.WriteLine(Logger.Level.Info, "Upgrade", 
					$"Upgrade is not available for {release.CurrentVersion}");

				isRunning = false;
				isUserInvoked = false;

				MessageWindow.Show(
					Resx.NoUpgrades, Resx.I_ApplicationTitle,
					MessageBoxButton.OK, MessageWindowImage.OK);
			}
			else
			{
				Logger.WriteLine(Logger.Level.Info, "Upgrade", 
					$"Running version {release.CurrentVersion}");

				isRunning = false;
			}
		}
	}
}
