//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Globalization;
	using System.Runtime.Serialization;


	/// <summary>
	/// iTunes track persistent IDs are 64 bit value expressed as hexidecimal string values.
	/// This value type is implicitly compatible with those values and provides convenient
	/// access to the low-order and high-order longword bits which is needed by the iTunes
	/// get_ItemByPersistentID method.
	/// </summary>

	[DataContract(Namespace = "urn:stevencohn.net/iTuner", Name = "persistentID")]
#if LINQPad
	public struct PersistentID
#else
	internal struct PersistentID
#endif
	{

		/// <summary>
		/// Indicate an empty value.
		/// </summary>

		public static readonly PersistentID Empty = new PersistentID(0);


		[DataMember(Name = "persistentTrackID")]
		private ulong persistentID;
		private int transientID;
		private string playlistName;


		/// <summary>
		/// Private constructor invoked by the implicit operators.
		/// </summary>
		/// <param name="value"></param>

		private PersistentID (ulong value)
		{
			this.persistentID = value;
			this.transientID = 0;
			this.playlistName = null;
		}


		/// <summary>
		/// Initialize a new persistent ID by combining the specified high-order and low-order
		/// integers.
		/// </summary>
		/// <param name="high">The high-order integer (4 bytes) of the persistent ID quadword.</param>
		/// <param name="low">The low-order integer (4 bytes) of the persistent ID quadword.</param>

		public PersistentID (int high, int low)
		{
			this.persistentID = (ulong)
				((((ulong)high << 32) & 0xFFFFFFFF00000000) +
				((ulong)low & 0x00000000FFFFFFFF));

			this.transientID = 0;
			this.playlistName = null;
		}


		/// <summary>
		/// Gets the low-order longword bits of the quadword value.
		/// </summary>

		public int LowBits
		{
			get { return (int)(persistentID & 0xFFFFFFFF); }
		}


		/// <summary>
		/// Gets the high-order longword bits of the quadword value.
		/// </summary>

		public int HighBits
		{
			get { return (int)(persistentID >> 32); }
		}


		/// <summary>
		/// Determines if this value is empty.
		/// </summary>
		/// <returns></returns>

		public bool IsEmpty
		{
			get { return this.persistentID.Equals(0); }
		}


		/// <summary>
		/// Gets or sets the name of the playlist specific to this track in the collection.
		/// This allows tracks from two playlists to occupy the same PersistentIDCollection.
		/// </summary>

		public string PlaylistName
		{
			get { return playlistName; }
			set { playlistName = value; }
		}


		/// <summary>
		/// Gets or sets a temporary ID associated with this persistent ID.
		/// </summary>

		public int TransientID
		{
			get { return transientID; }
			set { transientID = value; }
		}


		/// <summary>
		/// Convert from a UInt64 (ulong) value to a PersistentID value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>

		public static implicit operator PersistentID (ulong value)
		{
			return new PersistentID(value);
		}


		/// <summary>
		/// Convert from a PersistentID value to a UInt64 (ulong) value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>

		public static implicit operator ulong (PersistentID value)
		{
			return value.persistentID;
		}


		/// <summary>
		/// Convert from a PersistentID value to a 16 character hexidecimal string value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>

		public static implicit operator string (PersistentID value)
		{
			return value.persistentID.ToString("X8");
		}


		/// <summary>
		/// Parse a 16 character hexidecimal string value to return a PersistentID value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>

		public static PersistentID Parse (string value)
		{
			return new PersistentID(UInt64.Parse(value, NumberStyles.HexNumber));
		}


		/// <summary>
		/// Determines if this value is equal to the given value.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>

		public bool Equals (PersistentID other)
		{
			return this.persistentID.Equals(other.persistentID);
		}
	}
}
