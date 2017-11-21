//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Configuration;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Threading;
	using System.Text;
	using System.Windows;
	using System.Windows.Threading;
	using Microsoft.Win32;
	using Resx = Properties.Resources;


	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>

	internal partial class App : Application
	{
		private static string nameVersion;		// formatted name and version string
		private static bool excepted;			// true when first exc thrown
		private Mutex mutex;					// single instance mutex


		//========================================================================================
		// Constructor
		//========================================================================================

		public App ()
			: base()
		{
			// for development, explicitly override workstation UI culture
			OverrideUICulture();

			// given the /u parameter, we must uninstall the app
			CheckUninstall();

			// verify that iTunes is installed, otherwise there's no point, is there?
			EnsureiTunesInstalled();

			// allow only a single instance to run at a time
			try
			{
				mutex = Mutex.OpenExisting(Resx.I_SingletonID);

				// successfully opened existing mutex... another instance of iTuner is already
				// running, so check if iTunes is also running; if it is then assume we're a
				// duplicate and politely kill overselves.  otherwise, if iTunes is not running
				// assume that the other instance has gone rogue and needs to be assassinated!

				if (iTunesRunning())
				{
					MessageBox.Show(
						String.Format(CultureInfo.CurrentCulture,
							Resx.SingletonMessage, Resx.I_ApplicationTitle),
						Resx.I_ApplicationTitle,
						MessageBoxButton.OK, MessageBoxImage.Information);

					Application.Current.Shutdown();
					return;
				}
				else
				{
					StopiTuner();
					mutex = new Mutex(true, Resx.I_SingletonID);
				}
			}
			catch (WaitHandleCannotBeOpenedException)
			{
				mutex = new Mutex(true, Resx.I_SingletonID);
			}

			// passed the singleton test... wire up exception handling

			this.DispatcherUnhandledException +=
				new DispatcherUnhandledExceptionEventHandler(CatchUnhandledUIExceptions);

			AppDomain.CurrentDomain.UnhandledException +=
				new UnhandledExceptionEventHandler(CatchUnhandledBackgroundExceptions);

			UpgradeHelper.CheckUpgrades(this.Dispatcher);
		}


		/// <summary>
		/// Clean up disposables.
		/// </summary>
		/// <param name="e"></param>

		protected override void OnExit (ExitEventArgs e)
		{
			// TOOD: does this run before or after the exception handlers?
			// if after then we should unregister the event handlers

			if (mutex != null)
			{
				mutex.Dispose();
				mutex = null;
			}

			base.OnExit(e);
		}


		/// <summary>
		/// For development, explicitly override workstation UI culture
		/// </summary>

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		private void OverrideUICulture ()
		{
			string culture = ConfigurationManager.AppSettings["UICulture"];
			if (!String.IsNullOrEmpty(culture))
			{
				try
				{
					Thread.CurrentThread.CurrentCulture =
					Thread.CurrentThread.CurrentUICulture =
						CultureInfo.CreateSpecificCulture(culture);
				}
				catch
				{
					// no-op
				}
			}
		}


		/// <summary>
		/// Uninstall the application if passed /u=[ProductCode]
		/// </summary>

		private void CheckUninstall ()
		{
			string[] args = Environment.GetCommandLineArgs();
			foreach (string arg in args)
			{
				string[] parts = arg.Split('=');
				if (parts.Length == 2)
				{
					if (parts[0].ToLower(CultureInfo.InvariantCulture) == "/u")
					{
						StopiTuner();

						string path = Environment.GetFolderPath(Environment.SpecialFolder.System);

						ProcessStartInfo info = new ProcessStartInfo();
						info.CreateNoWindow = true;
						info.FileName = Path.Combine(path, "msiexec.exe");

						// the /x arg allows only uninstall whereas /i allows repair|uninstall
						info.Arguments = "/x " + parts[1];

						// do not set working directory to app folder or we will not delete it
						info.WorkingDirectory = path;

						Process.Start(info);

						Application.Current.Shutdown();
						break;
					}
				}
			}
		}


		/// <summary>
		/// If iTuner is currently running, we must terminate the process so we can continue
		/// the uninstall procedure.
		/// </summary>

		private void StopiTuner ()
		{
			int currentID = Process.GetCurrentProcess().Id;

			bool found = false;
			Process[] processes = Process.GetProcessesByName("iTuner");

			// Length must be > 1 because this current process counts as one of them!
			if ((processes != null) && (processes.Length > 1))
			{
				for (int i = 0; i < processes.Length; i++)
				{
					if (!found)
					{
						if (processes[i].Id != currentID)
						{
							found = true;
							processes[i].Kill();
						}
					}

					processes[i].Dispose();
					processes[i] = null;
				}
			}
		}


		/// <summary>
		/// Quick hack to check if iTunes is installed on the current workstation.  
		/// Simply peeks in the Registry for the appropriate iTunes entries.
		/// </summary>

		private void EnsureiTunesInstalled ()
		{
			// TODO: also investigate use of ITDETECTORLIB (ITDetector.ocx)

			string disconnected = ConfigurationManager.AppSettings["Disconnected"];
			if (disconnected != null)
			{
				bool runDisconnected = disconnected.Trim().ToLower().Equals("true");
				if (runDisconnected)
				{
					return;
				}
			}

			string path = @"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Computer, Inc.\iTunes";
			string folder = Registry.GetValue(path, "ProgramFolder", "") as string;
			if (String.IsNullOrEmpty(folder))
			{
				path = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Apple Computer, Inc.\iTunes";
				folder = Registry.GetValue(path, "ProgramFolder", "") as string;
			}

			if (String.IsNullOrEmpty(folder))
			{

				MessageBox.Show(
					Resx.iTunesMissing, Resx.I_ApplicationTitle,
					MessageBoxButton.OK, MessageBoxImage.Stop);

				Application.Current.Shutdown();
			}
		}


		/// <summary>
		/// Determine if iTunes is currently running.
		/// </summary>

		private bool iTunesRunning ()
		{
			bool found = false;
			Process[] processes = Process.GetProcessesByName("iTunes");

			if ((processes != null) && (processes.Length > 0))
			{
				for (int i = 0; i < processes.Length; i++)
				{
					processes[i].Dispose();
					processes[i] = null;
				}

				found = true;
			}

			return found;
		}


		/// <summary>
		/// Gets a formatted string specifying the application name and full version information.
		/// </summary>

		public static string NameVersion
		{
			get
			{
				if (nameVersion == null)
				{
					Assembly assembly = Assembly.GetExecutingAssembly();
					Version version = assembly.GetName().Version;

					nameVersion = String.Format(CultureInfo.CurrentCulture,
						Resx.VersionFormat,
						Resx.I_ApplicationTitle,
						version.Major, version.Minor, version.Build);
				}

				return nameVersion;
			}
		}



		//========================================================================================
		// Unhandled Exceptions
		//========================================================================================

		private void CatchUnhandledBackgroundExceptions (
			object sender, UnhandledExceptionEventArgs e)
		{
			HandleException(e.ExceptionObject as Exception, "Background");
		}


		private void CatchUnhandledUIExceptions (
			object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (IsIncompatible(e.Exception))
			{
				e.Handled = true;
				this.Shutdown();
				return;
			}

			HandleException(e.Exception, "UI");
		}


		public static void HandleException (Exception exc, string scope)
		{
			if (!excepted)
			{
				// set excepted to true before we do anything furthe or we may fall into
				// an infinite loop of thrown exceptions!
				excepted = true;
				SmartException smartExc = new SmartException(exc);
				LogException(smartExc);

				ExceptionDialog dialog = new ExceptionDialog(smartExc, scope);
				dialog.ShowDialog();
				dialog = null;
			}
		}


		private bool IsIncompatible (Exception exc)
		{
			if (exc is iTuner.iTunes.IncompatibleException)
			{
				return true;
			}

			if (exc.InnerException != null)
			{
				return IsIncompatible(exc.InnerException);
			}

			return false;
		}


		public static void LogException (SmartException exc)
		{
			try
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				string filename = assembly.Location + ".log";

				using (var log = new StreamWriter(filename, true))
				{
					StringBuilder builder = new StringBuilder();
					builder.Append(DateTime.Now.ToString("MM/dd HH:mm:ss"));
					builder.Append(" EXCEPTION: ");
					builder.Append(exc.Message);

					log.WriteLine(new String('=', 80));
					log.WriteLine(builder.ToString());
					log.WriteLine(new String('-', 80));
					log.WriteLine(exc.XmlMessage);

					log.Close();
				}
			}
			catch
			{
				// no-op
			}
		}
	}
}
