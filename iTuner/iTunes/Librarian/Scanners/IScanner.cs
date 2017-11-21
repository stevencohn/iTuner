//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.ComponentModel;


	/// <summary>
	/// Defines the basic interface necessary for Scanners to implement.
	/// </summary>

	internal interface IScanner
	{

		/// <summary>
		/// This event is fired at regular intervals as the synchronizer progresses.
		/// </summary>

		event ProgressChangedEventHandler ProgressChanged;


		/// <summary>
		/// Gets or sets an Action to invoke at the completion of this scanner.  This is
		/// used to remove disabled scanners from the Librarian <i>disabled</i> collection.
		/// </summary>

		Action Completed { get; set; }

	
		/// <summary>
		/// Gets the user-friendly name of this scanner.  Inheritors must set the protected
		/// <i>description</i> field in their constructors.
		/// </summary>

		string Description { get; }


		/// <summary>
		/// Gets the internal name of this scanner.  Inheritors must set the protected
		/// <i>name</i> field in their constructors.
		/// </summary>

		string Name { get; }


		/// <summary>
		/// Gets the percent completed by this scanner.  This is a bindable
		/// property used for the Librarian status panel.
		/// </summary>

		int ProgressPercentage { get; }


		/// <summary>
		/// Cancel execution of this scanner.  This occurs synchronously at the scope
		/// of an atomic task.  Implementors are required to check the <i>isActive</i>
		/// protected member at regular intervals to ensure reasonable immediacy.
		/// </summary>

		void Cancel ();


		/// <summary>
		/// Execute this scanner synchronously.
		/// </summary>
		/// <remarks>
		/// Scanners should be implemented as a sequence or loop of atomic tasks where
		/// the scanner has reasonably small increments between which they can check
		/// for a cancellation condition.
		/// </remarks>

		void Execute ();
	}
}
