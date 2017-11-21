//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System.Collections.ObjectModel;
	using System.Linq;


	/// <summary>
	/// Maintains a collection of USB disk objects.
	/// </summary>

	internal class UsbDiskCollection : ObservableCollection<UsbDisk>
	{

		/// <summary>
		/// Determines if the named disk is contained in this collection.
		/// </summary>
		/// <param name="name">The Windows name, or drive letter, of the disk to remove.</param>
		/// <returns>
		/// <b>True</b> if the item is found; otherwise <b>false</b>.
		/// </returns>

		public bool Contains (string name)
		{
			return this.AsQueryable().Any(d => d.Name == name);
		}


		/// <summary>
		/// Remove the named disk from the collection.
		/// </summary>
		/// <param name="name">The Windows name, or drive letter, of the disk to remove.</param>
		/// <returns>
		/// <b>True</b> if the item is removed; otherwise <b>false</b>.
		/// </returns>

		public bool Remove (string name)
		{
			UsbDisk disk = 
				(this.AsQueryable()
				.Where(d => d.Name == name)
				.Select(d => d)).FirstOrDefault();

			return disk != null && Remove(disk);
		}
	}
}
