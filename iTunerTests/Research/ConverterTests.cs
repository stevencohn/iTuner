//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.Linq;
	using System.Threading;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTunesLib;
	using iTuner.iTunes;


	/// <summary>
	/// </summary>

	[TestClass]
	public class ConverterTests : TestBase
	{
		private ManualResetEvent reset;



		[TestMethod]
		public void Convert ()
		{
			iTunesAppClass itunes = new iTunesAppClass();
			IITEncoderCollection encoders = itunes.Encoders;
			IITEncoder mp3Encoder = null;
			foreach (IITEncoder encoder in encoders)
			{
				Console.WriteLine(String.Format(
					"Encoder: [{0}] format=[{1}]", encoder.Name, encoder.Format));

				if (encoder.Format.Equals("MP3"))
				{
					mp3Encoder = encoder;
				}
			}

			if (mp3Encoder != null)
			{
				object track = itunes.LibraryPlaylist.Tracks.get_ItemByName("A Horse with No Name");
				itunes.CurrentEncoder = mp3Encoder;
				iTunesConvertOperationStatus status = itunes.ConvertTrack2(ref track);

				status.OnConvertOperationCompleteEvent +=
					new _IITConvertOperationStatusEvents_OnConvertOperationCompleteEventEventHandler(
						status_OnConvertOperationCompleteEvent);

				status.OnConvertOperationStatusChangedEvent += 
					new _IITConvertOperationStatusEvents_OnConvertOperationStatusChangedEventEventHandler(
						status_OnConvertOperationStatusChangedEvent);

				reset = new ManualResetEvent(false);
				reset.WaitOne();
			}
		}


		private void status_OnConvertOperationCompleteEvent ()
		{
			Console.WriteLine("Conversion complete");
			reset.Set();
		}


		private void status_OnConvertOperationStatusChangedEvent (
			string trackName, int progressValue, int maxProgressValue)
		{
			Console.WriteLine(String.Format(
				"Status for track '{0}', progress={1}, max={2}",
				trackName, progressValue, maxProgressValue));
		}
	}
}
