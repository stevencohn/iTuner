//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.IO;


	/// <summary>
	/// Describes a file action caught by the FileSystemWatcher and to be handled
	/// by the WatchScanner.
	/// </summary>

	internal class FileWatchAction
	{

		/// <summary>
		/// Initialize a new instance for <i>changed</i> and <i>deleted</i> actions.
		/// </summary>
		/// <param name="changeType">
		/// A WatcherChangeTypes specifying either <i>changed</i> or <i>deleted</i>.
		/// </param>
		/// <param name="fullPath">The full path of the file affected.</param>

		public FileWatchAction (WatcherChangeTypes changeType, string fullPath)
			: this(changeType, fullPath, null)
		{
		}


		/// <summary>
		/// Initialize a new instance for <i>renamed</i> actions.
		/// </summary>
		/// <param name="changeType">
		/// A WatcherChangeTypes specifying <i>renamed</i>.
		/// </param>
		/// <param name="fullPath">The full path of the new file name.</param>
		/// <param name="oldPath">The full path of the renamed file.</param>

		public FileWatchAction (WatcherChangeTypes changeType, string fullPath, string oldPath)
		{
			this.ChangeType = changeType;
			this.FullPath = fullPath;
			this.OldPath = oldPath;
		}


		/// <summary>
		/// Gets the FileSystemWatcher action.
		/// </summary>

		public WatcherChangeTypes ChangeType { get; private set; }
	
		
		/// <summary>
		/// Gets the full path of the file created or deleted, or the newly renamed file.
		/// </summary>

		public string FullPath { get; private set; }


		/// <summary>
		/// Gets the full path of a file before it was just renamed.
		/// </summary>
	
		public string OldPath { get; private set; }
	}
}
