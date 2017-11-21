//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTunesLib;
	using iTuner.iTunes;


	/// <summary>
	/// </summary>

	[TestClass]
	public class LyricsTests : TestBase
	{
		private class MockSong : ISong
		{
			private string lyrics;

			public MockSong (string artist, string title)
			{
				this.Artist = artist;
				this.Title = title;
				this.lyrics = String.Empty;
			}

			public string Artist { get; set; }
			public string Title { get; set; }
			public string Lyrics { get { return lyrics; } set { lyrics = value; } }
			public void CacheLyrics (string lyrics) { this.lyrics = lyrics; }
		}


		[TestMethod]
		public void TestAzLyricsProvider ()
		{
			var song = new MockSong("America", "Tin Man");
			var provider = new AzLyricsProvider();
			string lyrics = provider.RetrieveLyrics(song);
			Assert.IsFalse(String.IsNullOrEmpty(lyrics));
			Console.WriteLine("---Lyrics start---");
			Console.WriteLine(lyrics);
			Console.WriteLine("---Lyrics end---");
			Console.WriteLine("done");
		}


		[TestMethod]
		[Ignore]
		public void TestLyrics007Provider ()
		{
			var song = new MockSong("America", "Tin Man");
			var provider = new Lyrics007Provider();
			string lyrics = provider.RetrieveLyrics(song);
			Assert.IsFalse(String.IsNullOrEmpty(lyrics));
			Console.WriteLine("---Lyrics start---");
			Console.WriteLine(lyrics);
			Console.WriteLine("---Lyrics end---");
			Console.WriteLine("done");
		}


		[TestMethod]
		[Ignore]
		public void TestMP3LyricsProvider ()
		{
			var song = new MockSong("America", "Tin Man");
			var provider = new MP3LyricsProvider();
			string lyrics = provider.RetrieveLyrics(song);
			Assert.IsFalse(String.IsNullOrEmpty(lyrics));
			Console.WriteLine("---Lyrics start---");
			Console.WriteLine(lyrics);
			Console.WriteLine("---Lyrics end---");
			Console.WriteLine("done");
		}


		[TestMethod]
		public void GetLyrics ()
		{
			var song = new MockSong("Cut Paste", "Time Stands Still");

			//engine.RetrieveLyrics(song);

			// TODO: use ManualResetEvent to wait for this to complete...
		}


		private void DoLyricsUpdated (ITrack track)
		{
			throw new NotImplementedException();
		}


		private void DoProgressReport (ISong song, int stage)
		{
			throw new NotImplementedException();
		}
	}
}
