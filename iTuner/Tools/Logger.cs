//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Configuration;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using System.Threading;
	using Resx = Properties.Resources;


	/// <summary>
	/// A simple wrapper of a TextWriterTraceListener to append to a log file.
	/// </summary>

	internal static class Logger
	{
		public enum Level
		{
			Debug = 0,
			Warn = 1,
			Info = 2,
			Error = 3,
			None = int.MaxValue
		}


		private const string DefaultCategory = "LOG";

		private static readonly Level LogLevel;
		private static readonly bool IsApplogEnabled;

		private static TextWriterTraceListener applog;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initialize a new instance with the given output path.
		/// </summary>

		static Logger ()
		{
			Trace.Listeners.Clear();				// complete control of listeners

			var path = ConfigurationManager.AppSettings["LogFile"];
			path = path?.Trim();

			// only enable logging if LogFile is specified
			if (!string.IsNullOrEmpty(path))
			{
				var configLevel = ConfigurationManager.AppSettings["LogLevel"];
				if (configLevel == null)
				{
					LogLevel = Level.Debug;
				}
				else
				{
					try
					{
						LogLevel = (Level)Enum.Parse(typeof(Level), configLevel, true);
					}
					catch
					{
						LogLevel = Level.Debug;
					}
				}

				string dirpath;
				string filname;

				if (path.IndexOf(Path.DirectorySeparatorChar) < 0)
				{
					// if no directory specified then place in our local AppData directory
					dirpath = PathHelper.ApplicationDataPath;
					filname = PathHelper.CleanFileName(path);
				}
				else
				{
					dirpath = PathHelper.CleanDirectoryPath(Path.GetDirectoryName(path));
					filname = PathHelper.CleanFileName(Path.GetFileName(path));
				}

				path = Path.Combine(dirpath, filname);

				try
				{
					TextWriterTraceListener listener = new TextWriterTraceListener(path);
					Trace.Listeners.Add(listener);
				}
				catch
				{
					// probably a bad path so ignore logging
				}
			}

			var applogEnabled = ConfigurationManager.AppSettings["AppLogEnabled"];
			IsApplogEnabled = applogEnabled != null && applogEnabled.Trim().ToLower().Equals("true");
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Append an empty line to the log file.
		/// </summary>

		public static void WriteLine ()
		{
			if (Trace.Listeners.Count > 0)
			{
				foreach (TraceListener listener in Trace.Listeners)
				{
					listener.WriteLine(string.Empty);
					listener.Flush();
				}
			}
		}


		/// <summary>
		/// Write a single line to the log file with the given category and text.
		/// </summary>
		/// <param name="text">The message text to append to the log.</param>

		public static void WriteLine (string text)
		{
			WriteLine(Level.Info, DefaultCategory, text);
		}


		/// <summary>
		/// Write a single line to the log file with the given category and text.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text">The message text to append to the log.</param>

		public static void WriteLine (string category, string text)
		{
			WriteLine(Level.Info, category, text);
		}


		public static void Debug (string text)
		{
			WriteLine(Level.Debug, DefaultCategory, text);
		}


		/// <summary>
		/// Write a single line to the log file with the given category and text,
		/// filtering on the specified level.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="category"></param>
		/// <param name="text"></param>

		public static void WriteLine (Level level, string category, string text)
		{
			if (Trace.Listeners.Count > 0)
			{
				if (level >= LogLevel)
				{
					StringBuilder builder = new StringBuilder();
					builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff"));
					builder.Append(" ");
					builder.Append($"{category,-12}".Substring(0, 12));
					builder.Append(" ");
					builder.Append($"{level,-6}".Substring(0, 6));
					builder.Append(" ");

					builder.Append($"{Thread.CurrentThread.ManagedThreadId,-5}".Substring(0, 5));

					builder.Append(" ");
					builder.Append(text);

					string message = builder.ToString();

					foreach (TraceListener listener in Trace.Listeners)
					{
						listener.WriteLine(message);
						listener.Flush();
					}
				}
			}
		}


		/// <summary>
		/// Write a formatted exception to the log.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="exc"></param>

		public static void WriteLine (string category, Exception exc)
		{
			if (Trace.Listeners.Count > 0)
			{
				SmartException smart = new SmartException(exc);
				WriteLine(Level.Error, category, new string('=', 80));
				WriteLine(Level.Error, category, smart.Message);
				WriteLine(Level.Error, category, new string('-', 80));
				WriteLine(Level.Error, category, smart.XmlMessage);
			}
		}


		/// <summary>
		/// Write a message and formatted exception to the log.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="message"></param>
		/// <param name="exc"></param>

		public static void WriteLine (string category, string message, Exception exc)
		{
			if (Trace.Listeners.Count > 0)
			{
				WriteLine(Level.Error, category, message);

				SmartException smart = new SmartException(exc);
				WriteLine(Level.Error, category, new string('=', 80));
				WriteLine(Level.Error, category, smart.Message);
				WriteLine(Level.Error, category, new string('-', 80));
				WriteLine(Level.Error, category, smart.XmlMessage);
			}
		}


		/// <summary>
		/// Write a single log to the application log file with the given category and text.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>

		public static void WriteAppLog (string category, string text)
		{
			if (IsApplogEnabled)
			{
				if (applog == null)
				{
					var path = Path.Combine(PathHelper.ApplicationDataPath, Resx.FilenameAppLog);
					applog = new TextWriterTraceListener(path);
				}

				StringBuilder builder = new StringBuilder();
				builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				builder.Append(" ");
				builder.Append($"{category,-12}".Substring(0, 12));
				builder.Append(" ");
				builder.Append(text);

				var message = builder.ToString();

				applog.WriteLine(message);
				applog.Flush();
			}
		}
	}
}
