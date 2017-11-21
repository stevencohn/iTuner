//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace ControllerHarness
{
	using System;
	using System.Threading;
	using iTuner.iTunes;

	
	class Program
	{
		private static Controller controller;
	
		static void Main (string[] args)
		{
			controller = new Controller();

			Thread thread = new Thread(new ThreadStart(delegate
			{
				while (controller.IsConnected)
				{
					using (Track track = controller.CurrentTrack)
					{
						if (track != null)
						{
							Console.WriteLine(String.Format("CurrenTrack.Name = '{0}'", track.Name));
						}
					}

					using (PlaylistCollection collection = controller.Playlists)
					{
						int sum = 0;
						if (collection != null)
						{
							foreach (Playlist playlist in collection.Values)
							{
								if (playlist != null)
								{
									sum += playlist.PlaylistID;
								}
							}
						}

						Console.WriteLine(String.Format("Playlist sum [{0}]", sum));
					}

					Thread.Sleep(1000);
				}
			}));

			thread.Start();

			Console.WriteLine("... Press Enter to quit");
			Console.ReadLine();

			controller.Dispose();

			thread.Abort();
			thread = null;

			controller = null;

			Console.WriteLine("... Done");
		}
	}
}
