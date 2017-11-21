//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Defines the basic interface necessary for lyrics providers to implement.
	/// </summary>

	internal interface ILyricsProvider
	{

		/// <summary>
		/// Gets a Boolean value indicating whether this provider has successfully
		/// connected.  This should be checked after each request.  If not connected
		/// then the provide should be ignored.
		/// </summary>

		bool IsConnected { get; }


		/// <summary>
		/// Gets the name of this lyrics provider.  Inheritors must set the protected
		/// <i>name</i> field in their constructors.
		/// </summary>

		string Name { get; }


		/// <summary>
		/// Contact the provider and retrieve the lyrics.  The lyrics are cleansed
		/// and formatted before returning.
		/// </summary>
		/// <param name="song">The song for which lyrics are to be retrieved.</param>
		/// <returns>A multi-line string specifying the lyrics of the given song.</returns>
		/// <remarks>
		/// TODO: could be improved by passing the BackgroundWorker.  This would allow
		/// multi-step providers to check CancellationPending at each point to cancel earlier.
		/// </remarks>

		string RetrieveLyrics (ISong song);
	}
}