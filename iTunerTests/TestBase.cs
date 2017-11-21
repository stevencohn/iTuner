//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Resx = Properties.Resources;


	/// <summary>
	/// Declares the base features and services of a standardized unit test framework,
	/// providing automatic test metrics such as total time and memory consumption.
	/// </summary>

	[TestClass]
	public abstract class TestBase
	{
		#region class Stopwatch

		/// <summary>
		/// This basic stopwatch mechanism uses Windows Performance counters to accurately
		/// measure the run-time of a scoped section of code.
		/// </summary>

		protected class Stopwatch
		{
			[DllImport("kernel32.dll")]
			extern static short QueryPerformanceCounter (ref long x);
			[DllImport("kernel32.dll")]
			extern static short QueryPerformanceFrequency (ref long x);

			private long startTime;
			private long stopTime;
			private long splitTime;
			private long frequency;
			private long calibration;
			private bool isRunning;


			/// <summary>
			/// Initialize a new instance in a ready state.
			/// </summary>

			public Stopwatch ()
			{
				isRunning = false;
				startTime = stopTime = splitTime = frequency = calibration = 0;
				Calibrate();
			}


			private void Calibrate ()
			{
				QueryPerformanceFrequency(ref frequency);

				for (int i = 0; i < 1000; i++)
				{
					Start();
					Stop();
					calibration += stopTime - startTime;
				}

				calibration /= 1000;
			}


			/// <summary>
			/// Gets a Boolean value indicating if this instance is currently running.
			/// </summary>

			public bool IsRunning
			{
				get { return isRunning; }
			}


			/// <summary>
			/// Stops and resets the watch to the initial state, 0 time.
			/// </summary>

			public void Reset ()
			{
				Stop();
				startTime = stopTime = splitTime = 0;
			}


			/// <summary>
			/// Starts the watch.
			/// </summary>

			public void Start ()
			{
				QueryPerformanceCounter(ref startTime);
				splitTime = startTime;
				isRunning = true;
			}


			/// <summary>
			/// Stops the watch.
			/// </summary>

			public void Stop ()
			{
				if (isRunning)
				{
					QueryPerformanceCounter(ref stopTime);
					isRunning = false;
				}
			}


			/// <summary>
			/// Calculates the elapsed time in milliseconds after a run has completed; call
			/// this only after a start/stop sequence as it will not return a valid value
			/// while running.
			/// </summary>
			/// <returns>A double value specifying the ellapsed time in milleseconds.</returns>

			public double GetElapsedMilliseconds ()
			{
				return ((stopTime - startTime - calibration) * 1000000.0 / frequency) / 1000.0;
			}


			/// <summary>
			/// Calculates the elapsed time in micoseconds after a run has completed; call
			/// this only after a start/stop sequence as it will not return a valid value
			/// while running.
			/// </summary>
			/// <returns>A double value specifying the ellapsed time in micoseconds.</returns>

			public double GetElapsedMicroseconds ()
			{
				return (stopTime - startTime - calibration) * 1000000.0 / frequency;
			}


			/// <summary>
			/// Calculates the split time in milliseconds during an active run; call this
			/// after a start but before a stop.  It will not return a valid value if called
			/// after the watch is stopped.
			/// </summary>
			/// <returns>A double value specifying the ellapsed time in milliseconds.</returns>

			public double GetSplitMilliseconds ()
			{
				long current = 0;
				QueryPerformanceCounter(ref current);
				double time = ((current - splitTime - calibration) * 1000000.0 / frequency) / 1000.0;
				splitTime = current;
				return time;
			}

			/// <summary>
			/// Calculates the split time in microseconds during an active run; call this
			/// after a start but before a stop.  It will not return a valid value if called
			/// after the watch is stopped.
			/// </summary>
			/// <returns>A double value specifying the ellapsed time in microseconds.</returns>

			public double GetSplitMicroseconds ()
			{
				long current = 0;
				QueryPerformanceCounter(ref current);
				double time = (current - startTime - calibration) * 1000000.0 / frequency;
				splitTime = current;
				return time;
			}
		}

		#endregion class Stopwatch

		#region class Counters

		private class Counters
		{
			public const string PrivateBytesName = "Private Bytes";
			public const string NumCCWsName = "# of CCWs";
			public const string NumStubsName = "# of Stubs";

			private class CounterInfo
			{
				public CounterInfo (string category, string name, string processName)
				{
					this.Name = name;
					this.Counter = new PerformanceCounter(category, name, processName);
					this.Current = this.Last = this.Start = Counter.NextValue();
				}

				public string Name { get; private set; }
				public PerformanceCounter Counter { get; private set; }
				public double Start { get; private set; }
				public double Current { get; private set; }
				public double Last { get; set; }

				public double CurrentValue
				{
					get { return Counter.NextValue(); }
				}

				public double StartValue
				{
					get { return Start; }
				}

				public double GetLifetime ()
				{
					Current = Counter.NextValue();
					return Current - Start;
				}

				public double GetSpan ()
				{
					Current = Counter.NextValue();
					double span = Current - Start;
					return span;
				}

				public double GetSplit ()
				{
					Current = Counter.NextValue();
					double split = Current - Last;
					Last = Current;
					return split;
				}
			}

			private Dictionary<string, CounterInfo> counters;

			public Counters ()
			{
				counters = new Dictionary<string, CounterInfo>();

				using (Process process = Process.GetCurrentProcess())
				{
					counters.Add(PrivateBytesName, new CounterInfo("Process", PrivateBytesName, process.ProcessName));
					counters.Add(NumCCWsName, new CounterInfo(".NET CLR Interop", NumCCWsName, process.ProcessName));
					counters.Add(NumStubsName, new CounterInfo(".NET CLR Interop", NumStubsName, process.ProcessName));
				}
			}

			public double GetCurrent (string name)
			{
				return counters[name].CurrentValue;
			}

			public double GetLifetime (string name)
			{
				return counters[name].GetLifetime();
			}

			public double GetSplit (string name)
			{
				return counters[name].GetSplit();
			}

			public double GetSpan (string name)
			{
				return counters[name].GetSpan();
			}

			public double GetStart (string name)
			{
				return counters[name].StartValue;
			}
		}

		#endregion class Counters

		private const int KB = 1024;
		private const int MB = KB * 1000;
		private const int GB = MB * 1000;

		private TestContext context;
		private Counters counters;
		protected Stopwatch watch;


		#region TestContext/ClassInitialize/Cleanup

		/// <summary>
		/// Gets or sets the test context which provides information about
		/// and functionality for the current test run.
		/// </summary>

		public TestContext TestContext
		{
			get { return context; }
			set { context = value; }
		}

	
		//You can use the following additional attributes as you write your tests:
		//Use ClassInitialize to run code before running the first test in the class

		//[ClassInitialize]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}

		//[ClassCleanup]
		//public static void MyClassCleanup ()
		//{
		//}
		#endregion TextContext/ClassInitialize/Cleanup


		/// <summary>
		/// Run before running each test.  Inheritors must call this from their overrides.
		/// </summary>

		[TestInitialize]
		public virtual void MyTestInitialize ()
		{
			Console.WriteLine(context.TestName.PadRight(90, '-'));

			counters = new Counters();

			watch = new Stopwatch();
			watch.Start();
		}


		/// <summary>
		/// Run after each test has completed.  Inheritors must call this from their overrides.
		/// </summary>

		[TestCleanup]
		public virtual void MyTestCleanup ()
		{
			Console.WriteLine(String.Empty);

			if (watch.IsRunning)
			{
				watch.Stop();
				Console.WriteLine(String.Format(
					"... Completed in {0} ms", watch.GetElapsedMilliseconds()));
			}

			PrintCounterLifetime(Counters.PrivateBytesName);
			PrintCounterLifetime(Counters.NumCCWsName);
			PrintCounterLifetime(Counters.NumStubsName);

			Console.WriteLine(String.Format("{0} Completed: {1}",
				context.TestName,
				context.CurrentTestOutcome.ToString()).PadRight(90, '-'));
		}


		private void PrintCounterLifetime (string name)
		{
			Console.WriteLine(String.Format(
				"... '{0}' began at {1}, ended at {2}, total change is {3}", name,
				counters.GetStart(name), counters.GetCurrent(name), counters.GetLifetime(name)));
		}


		/// <summary>
		/// Reports intermediate delta in private-byte consumption.
		/// </summary>

		protected void ReportPrivateDelta ()
		{
			Console.WriteLine(String.Empty);
			PrintCounterSplits(Counters.PrivateBytesName);
			PrintCounterSplits(Counters.NumCCWsName);
			PrintCounterSplits(Counters.NumStubsName);
			Console.WriteLine(String.Empty);
		}

		private void PrintCounterSplits (string name)
		{
			Console.WriteLine(String.Format("... '{0}' split {1}, span {2}", name,
				FormatByteCount(counters.GetSplit(name)),
				FormatByteCount(counters.GetSpan(name))));
		}


		/// <summary>
		/// Reports intermediate split time during execution.
		/// </summary>

		protected void ReportSplitTime ()
		{
			Console.WriteLine(String.Empty);

			Console.WriteLine(String.Format(
				"... Split time is {0} ms", watch.GetSplitMilliseconds()));

			Console.WriteLine(String.Empty);
		}


		/// <summary>
		/// Formats the specified number of bytes into a readable string converted to
		/// bytes, KB, MB, or GB as appropriate.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>

		protected static string FormatByteCount (double bytes)
		{
			string format = null;

			if (bytes < KB)
			{
				format = String.Format(Resx.FreeBytes, bytes);
			}
			else if (bytes < MB)
			{
				bytes = bytes / KB;
				format = String.Format(Resx.FreeKB, bytes.ToString("0.##"));
			}
			else if (bytes < GB)
			{
				double dree = bytes / MB;
				format = String.Format(Resx.FreeMB, dree.ToString("0.####"));
			}
			else
			{
				double gree = bytes / GB;
				format = String.Format(Resx.FreeGB, gree.ToString("0.####"));
			}

			return format;
		}
	}
}
