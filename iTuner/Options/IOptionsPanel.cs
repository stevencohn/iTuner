//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;


	/// <summary>
	/// Interaction logic for ScannerOptions.xaml
	/// </summary>

	internal interface IOptionsPanel
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>

		bool ContainsOption (string name);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>

		bool GetOption (string name);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>

		void SetOption (string name, bool value);
	}
}