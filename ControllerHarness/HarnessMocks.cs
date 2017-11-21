
namespace iTuner.iTunes
{
	using System;


	internal class App { public static string NameVersion { get { return String.Empty; } } }
	internal class Librarian
	{
		public static Librarian Create (Controller controller) { return new Librarian(); }
		public void Dispose () { }
		public void Clean (string Album, string Artist) { }
		public bool IsCleansed (string Album, string Artist) { return true; }
		public void MaintainLibrary (MaintenanceAction action) { }
	}
	internal delegate void LyricEngineProgress (ISong song, int stage);
	internal delegate void TrackHandler (Track track);
	internal class LyricEngine
	{
		public event TrackHandler LyricsUpdated;
		public event LyricEngineProgress LyricsProgressReport;
		public static LyricEngine CreateEngine () { return new LyricEngine(); }
		public void Dispose () { }
		public string RetrieveLyrics (Track track) { return String.Empty; }
	}
	internal class MaintenanceAction
	{
		public static MaintenanceAction Create (object changedObjectIDs) { return new MaintenanceAction(); }
	}
	internal class NetworkStatus
	{
		public static bool IsAvailable { get { return true; } }
	}
}
