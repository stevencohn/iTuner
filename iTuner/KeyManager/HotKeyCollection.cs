//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using WinForms = System.Windows.Forms;


	/// <summary>
	/// Manages a collection of hot key instances.  This is serialized to a config file
	/// when preserving key preferences.
	/// </summary>

	[CollectionDataContract(Namespace = "urn:stevencohn.net/iTuner", Name = "hotkeys", ItemName = "hotkey")]
	[KnownType(typeof(HotKey))]
	internal class HotKeyCollection : List<IHotKey>
	{

		/// <summary>
		/// Default constructor required for data contract serialization.
		/// </summary>

		public HotKeyCollection ()
		{
		}


		/// <summary>
		/// Initialize a new instance seeded with the given hot keys.
		/// </summary>
		/// <param name="keys">A list of hot keys to use.</param>

		public HotKeyCollection (IEnumerable<HotKey> keys)
		{
			foreach (HotKey key in keys)
			{
				this.Add(key);
			}
		}


		/// <summary>
		/// Adds the given item, maintaining the expected Action-based order of items
		/// in the collection.
		/// </summary>
		/// <param name="item">The new item to add</param>

		public new void Add (IHotKey item)
		{
			var index = 0;
			var found = false;

			while ((index < this.Count) && !found)
			{
				if (item.Action < this[index].Action)
				{
					Insert(index, item);
					found = true;
				}
				else if (item.Action == this[index].Action)
				{
					var key = (HotKey)this[index];
					key.Code = item.Code;
					key.Modifier = item.Modifier;
					found = true;
				}
				else
				{
					index++;
				}
			}

			if (!found)
			{
				base.Add(item);
			}
		}


		/// <summary>
		/// Determines if the specified hot key action is defined in the collection.
		/// </summary>
		/// <param name="action">The HotKeyAction to match</param>
		/// <returns><b>true</b> if the collections contains a HotKey for this action.</returns>

		public bool Contains (HotKeyAction action)
		{
			return this.Any(p => p.Action == action);
		}


		public bool Contains (WinForms.Keys keys, KeyModifier modifier)
		{
			return this.Any(p => (p.Code == keys) && p.Modifier == modifier);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sequence"></param>

		public void SetSequence (HotKey entry, HotKey sequence)
		{
			foreach (var hotKey in this)
			{
				var other = (HotKey) hotKey;
				if ((other.Code == sequence.Code) && (other.Modifier == sequence.Modifier))
				{
					other.SetSequence(WinForms.Keys.None, KeyModifier.None);
				}
			}

			entry.SetSequence(sequence.Code, sequence.Modifier);
		}
	}
}
