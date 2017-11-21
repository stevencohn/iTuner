//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Management;
	using System.Threading;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner;


	/// <summary>
	/// </summary>

	[TestClass]
	public class UsbTests : TestBase
	{
		private UsbManager manager;

		[TestMethod]
		public void CollectionTest ()
		{
			UsbDiskCollection disks = new UsbDiskCollection();
			Assert.IsFalse(disks.Contains("foo"));

			UsbDisk disk = new UsbDisk("foo");
			disks.Add(disk);

			Assert.IsTrue(disks.Contains(disk));
			Assert.IsTrue(disks.Contains("foo"));
			Assert.IsFalse(disks.Contains("missing"));

			disks.Remove("foo");
			Assert.AreEqual(0, disks.Count);
			Assert.IsFalse(disks.Contains("foo"));
		}


		[TestMethod]
		public void ManagerTest ()
		{
			manager = new UsbManager();
			UsbDiskCollection disks = manager.GetAvailableDisks();

			foreach (UsbDisk disk in disks)
			{
				Console.WriteLine(String.Format(
					"{0} {1} ({2})", disk.Name, disk.Volume, disk.Model));
			}

			// need to set GetDiskInformation to 'public' for these lines:
			//UsbDisk d = new UsbDisk("T:", null, null);
			//manager.GetDiskInformation(d);

			manager.StateChanged += new UsbStateChangedEventHandler(manager_StateChanged);

			ManualResetEvent reset = new ManualResetEvent(false);
			reset.WaitOne(120000, false);
		}

		private void manager_StateChanged (UsbStateChangedEventArgs e)
		{
			Console.WriteLine(e.State.ToString() + " - " + e.Disk.ToString());
			Debug.WriteLine(e.State.ToString() + " - " + e.Disk.ToString());
		}


		/// <summary>
		/// </summary>

		[Ignore]
		[TestMethod]
		public void DetectUSbDevices ()
		{
			DriveInfo[] drives = DriveInfo.GetDrives();
			Assert.IsNotNull(drives);

			foreach (DriveInfo drive in drives)
			{
				if (drive.IsReady)
				{
					Console.WriteLine(String.Format(
						"Drive {0} ({1})", drive.Name, drive.VolumeLabel));
				}
			}

			Console.WriteLine(new String('-', 80));

			// browse all USB WMI physical disks
			foreach (ManagementObject drive in
				new ManagementObjectSearcher(
					"select DeviceID, Model from Win32_DiskDrive where InterfaceType='USB'").Get())
			{
			    // associate physical disks with partitions
				ManagementObject partition = new ManagementObjectSearcher(String.Format(
					"associators of {{Win32_DiskDrive.DeviceID='{0}'}} where AssocClass = Win32_DiskDriveToDiskPartition",
					drive["DeviceID"])).First();

				if (partition != null)
				{
			        // associate partitions with logical disks (drive letter volumes)
					ManagementObject disk = new ManagementObjectSearcher(String.Format(
						"associators of {{Win32_DiskPartition.DeviceID='{0}'}} where AssocClass = Win32_LogicalDiskToPartition",
						partition["DeviceID"])).First();

					if (disk != null)
					{
						ManagementObject volume = new ManagementObjectSearcher(String.Format(
							"select VolumeName from Win32_LogicalDisk where Name='{0}'",
							disk["Name"])).First();

						Console.WriteLine(String.Format(
							"{0} {1} ({2})", disk["Name"], volume["VolumeName"], drive["Model"]));
					}
				}
			}
		}
	}


	public static class WmiExtensions
	{
		public static ManagementObject First (this ManagementObjectSearcher searcher)
		{
			ManagementObject result = null;
			foreach (ManagementObject item in searcher.Get())
			{
				result = item;
				break;
			}
			return result;
		}
	}
}
