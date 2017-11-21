//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.ComponentModel;
	using System.Globalization;
	using System.Runtime.Serialization;
	using System.Text;
	using System.Windows.Forms;


	//********************************************************************************************
	// class HotKey
	//********************************************************************************************

	/// <summary>
	/// Describes a single hot key including its key sequence, internal unique identifier,
	/// and the associated action.
	/// </summary>

	[DataContract(Namespace = "urn:stevencohn.net/iTuner", Name = "hotkey")]
	internal class HotKey : IHotKey, INotifyPropertyChanged
	{

		/// <summary>
		/// Maintains next HotID value; we're assuming no one in their right frickin mind
		/// would ever attempt to manually define 49,151 hot keys in a single session!
		/// </summary>

		private static int counter;


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initializes a new instance with the specified properties.
		/// </summary>
		/// <param name="action">The action associated with this key sequence.</param>
		/// <param name="code">The primary keyboard key-code identifier.</param>
		/// <param name="modifier">The secondary keyboard modifier such as Ctrl or Alt.</param>

		public HotKey (HotKeyAction action, Keys code, KeyModifier modifier)
		{
			counter++;

			this.Action = action;
			this.HotId = counter;
			this.Code = code;
			this.Modifier = modifier;
		}


		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="other"></param>

		public HotKey (HotKey other)
		{
			counter++;

			this.Action = other.Action;
			this.HotId = other.HotId;
			this.Code = other.Code;
			this.Modifier = other.Modifier;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// This event is fired when the value of a property is changed.
		/// </summary>

		public event PropertyChangedEventHandler PropertyChanged;


		/// <summary>
		/// Gets the action associated with this key sequence.
		/// </summary>

		[DataMember(Name = "action")]
		public HotKeyAction Action
		{
			get;
			set;
		}


		/// <summary>
		/// Gets the internal unique identifier of this instance.
		/// This is used to register and unregister the hot key. 
		/// </summary>

		public int HotId
		{
			get;
			private set;
		}


		/// <summary>
		/// Gets the primary keyboard key-code identifier.
		/// </summary>

		[DataMember(Name = "code")]
		public Keys Code
		{
			get;
			set;
		}


		/// <summary>
		/// Gets the secondary keyboard key modifier: Alt, Ctrl, Win, and Shfit.
		/// This is a bit mask.
		/// </summary>

		[DataMember(Name = "modifier")]
		public KeyModifier Modifier
		{
			get;
			set;
		}


		/// <summary>
		/// Gets the key sequence as a readable string
		/// </summary>

		public string Sequence => ToString();


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Returns a string describining the specified key sequence.
		/// </summary>
		/// <param name="code">The primary keyboard key-code identifier.</param>
		/// <param name="modifier">The secondary keyboard key modifier.</param>
		/// <returns>A string desribining the key sequence.</returns>

		public static string MakeString (Keys code, KeyModifier modifier)
		{
			StringBuilder builder = new StringBuilder();

			if ((int)modifier != 0)
			{
				bool isMod = (
					(code == Keys.Alt) ||
					(code == Keys.LControlKey) || (code == Keys.RControlKey) ||
					(code == Keys.LWin) || (code == Keys.RWin));

				if (!isMod)
				{
					builder.Append(modifier.ToString());
					builder.Append("+");
				}
			}

			string codes = code.ToString();
			if (codes.StartsWith("oem", StringComparison.InvariantCultureIgnoreCase))
			{
				codes = Char.ToUpper(codes[3], CultureInfo.CurrentCulture) + codes.Substring(4);
			}

			builder.Append(codes);

			return builder.ToString();
		}


		/// <summary>
		/// Changes the key sequence for this hot key action.
		/// </summary>
		/// <param name="modifier">The new modifier flags.</param>
		/// <param name="code">The new keyboard key code.</param>

		public void SetSequence (Keys code, KeyModifier modifier)
		{
			if (code == Keys.Back)
			{
				this.Code = Keys.None;
				this.Modifier = KeyModifier.None;
			}
			else
			{
				this.Code = code;
				this.Modifier = modifier;
			}

			OnPropertyChanged("Sequence");
		}


		/// <summary>
		/// Returns a string describing the key sequence such as "Alt+F9".
		/// </summary>
		/// <returns>A string value.</returns>

		public override string ToString ()
		{
			return MakeString(Code, Modifier);
		}


		/// <summary>
		/// Notify listeners that a property value has changed.
		/// </summary>
		/// <param name="name"></param>

		private void OnPropertyChanged (string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
