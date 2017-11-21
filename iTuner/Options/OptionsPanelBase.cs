//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.ComponentModel;
	using System.Configuration;
	using System.Windows.Controls;
	using Settings = Properties.Settings;


	/// <summary>
	/// Interaction logic for ScannerOptions.xaml
	/// </summary>

	internal class OptionsPanelBase : UserControl, IOptionsPanel, INotifyPropertyChanged
	{


		/// <summary>
		/// 
		/// </summary>

		public event PropertyChangedEventHandler PropertyChanged;



		#region IOptionsPanel Members

		bool IOptionsPanel.ContainsOption (string name)
		{
			throw new NotImplementedException();
		}

		bool IOptionsPanel.GetOption (string name)
		{
			throw new NotImplementedException();
		}

		void IOptionsPanel.SetOption (string name, bool value)
		{
			throw new NotImplementedException();
		}

		#endregion

	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>

		protected bool ContainsOption (string name)
		{
			foreach (SettingsProperty property in Settings.Default.Properties)
			{
				if (property != null)
				{
					if (property.Name.Equals(name))
					{
						return true;
					}
				}
			}

			return false;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>

		protected bool GetOption (string name)
		{
			if (ContainsOption(name))
			{
				return (bool)Settings.Default[name];
			}

			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>

		protected void SetOption (string name, bool value)
		{
			if (ContainsOption(name))
			{
				Settings.Default[name] = value;
			}
			else
			{
				SettingsProperty property = new SettingsProperty(name);
				property.PropertyType = typeof(bool);
				property.DefaultValue = true;
				property.IsReadOnly = false;

				property.Attributes.Add(
					typeof(UserScopedSettingAttribute),
					new UserScopedSettingAttribute());

				Settings.Default.Properties.Add(property);
			}

			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}
}