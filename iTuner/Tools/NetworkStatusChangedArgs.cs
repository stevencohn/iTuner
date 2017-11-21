//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;


	//********************************************************************************************
	// class NetworkStatusChangedArgs
	//********************************************************************************************

	/// <summary>
	/// Describes the overall network connectivity of the machine.
	/// </summary>

	internal class NetworkStatusChangedArgs : EventArgs
	{
		private bool isAvailable;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="isAvailable"></param>

		public NetworkStatusChangedArgs (bool isAvailable)
		{
			this.isAvailable = isAvailable;
		}


		/// <summary>
		/// Gets a Boolean value indicating the current state of Internet connectivity.
		/// </summary>

		public bool IsAvailable
		{
			get { return isAvailable; }
		}
	}


	//********************************************************************************************
	// delegate NetworkStatusChangedHandler
	//********************************************************************************************

	/// <summary>
	/// Define the method signature for network status changes.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>

	internal delegate void NetworkStatusChangedHandler (
		object sender, NetworkStatusChangedArgs e);
}
