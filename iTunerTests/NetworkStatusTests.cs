//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Net.NetworkInformation;
	using System.Threading;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner;


	/// <summary>
	/// </summary>

	[TestClass]
	public class NetworkStatusTests : TestBase
	{

		/// <summary>
		/// </summary>

		[TestMethod]
		public void GetStatus ()
		{
			DumpNetworkStats();

			bool isAvailable = NetworkStatus.IsAvailable;

			ManualResetEvent reset = new ManualResetEvent(false);

			NetworkStatus.AvailabilityChanged +=
				delegate(object sender, NetworkStatusChangedArgs args)
				{
					Console.WriteLine(String.Format(
						"Availability changed to '{0}'", args.IsAvailable ? "Up" : "Down"));

					// only continue when network is brought back online
					if (args.IsAvailable)
					{
						reset.Set();
					}
				};


			reset.WaitOne(120000, false);

			Assert.IsTrue(NetworkStatus.IsAvailable);

			DumpNetworkStats();
		}


		private void DumpNetworkStats ()
		{
			Console.WriteLine();

			Console.WriteLine("Domain name is [{0}]", NetworkStatus.GetBestDomainName());

			bool isAvailable = NetworkInterface.GetIsNetworkAvailable();
			Console.WriteLine(String.Format("isAvailable = [{0}]", isAvailable ? "Yes" : "No"));

			NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface face in interfaces)
			{
				Console.WriteLine(String.Format(
					"Interface [{0}] ({1}), type=[{2}]",
					face.Name, face.Description, face.NetworkInterfaceType.ToString()));

				IPv4InterfaceStatistics stats = face.GetIPv4Statistics();

				Console.WriteLine(String.Format(
					"    Status......: [{0}] Rcv/Snd=[{1}/{2}]",
					face.OperationalStatus.ToString(), stats.BytesReceived, stats.BytesSent));
			}
		}
	}

}
