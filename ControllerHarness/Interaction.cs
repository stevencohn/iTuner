//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

#define Debugging

namespace iTuner.iTunes
{
	using System;
	using System.Diagnostics;
	using System.Runtime.InteropServices;
	using System.Threading;
	using iTunesLib;


	#region internal interface ITObject...
	/*
	internal interface ITObject
	{
		string Name { get; set; }
		int DatabaseID { get; }
		int Index { get; }
		int PlaylistID { get; }
		int SourceID { get; }
		int TrackID { get; }
		int TrackDatabaseID { get; }

		void GetObjectIDs (
			out int sourceID, out int playlistID, out int trackID, out int databaseID);
	}
	*/
	#endregion ITObject


	//********************************************************************************************
	// class Interaction
	//********************************************************************************************

	/// <summary>
	/// Provides base mechanisms for inheritors to safely control access to the iTunes COM
	/// interface, blocking callers when COM interaction is disabled, allowing callers while
	/// COM interaction is enabled.
	/// </summary>

	internal abstract class Interaction : IDisposable
	{
		// single iTunes COM interface used by all instances
		protected static iTunesAppClass itunes;

		// allow all inheritors to determine if we're still alive
		protected static bool isConnected;

		// single synchronizer to coordinate COM state changes
		private static ManualResetEvent reset;

		private static _IiTunesEvents_OnCOMCallsEnabledEventEventHandler enabledEvent;
		private static _IiTunesEvents_OnCOMCallsDisabledEventEventHandler disabledEvent;

		private ObjectID objectID;
		private PersistentID persistentID;
		private bool isPrimaryController;
		private bool isDisposed;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Initialize class static fields.
		/// </summary>

		static Interaction ()
		{
			enabledEvent = new _IiTunesEvents_OnCOMCallsEnabledEventEventHandler(DoEnabledEvent);
			disabledEvent = new _IiTunesEvents_OnCOMCallsDisabledEventEventHandler(DoDisabledEvent);

			reset = new ManualResetEvent(true);

			isConnected = false;
		}


		/// <summary>
		/// Initialize a new instance for secondary wrappers such as Playlists, Tracks, etc.
		/// </summary>

		public Interaction ()
		{
			this.isPrimaryController = false;
			this.objectID = null;
			this.persistentID = PersistentID.Empty;

			this.isDisposed = false;
		}


		/// <summary>
		/// Only called by the primary Controller, sets the internal itunes reference.
		/// </summary>

		public void InitializeInteraction ()
		{
			this.isPrimaryController = true;

			Interaction.itunes.OnCOMCallsEnabledEvent += enabledEvent;
			Interaction.itunes.OnCOMCallsDisabledEvent += disabledEvent;

			Interaction.isConnected = true;
		}


		#region Lifecycle

		/// <summary>
		/// Base destructor for all inheritors; do not override.
		/// </summary>

		~Interaction ()
		{
			Dispose();
		}


		/// <summary>
		/// Dispose of this instance; do not override.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				// invoke Cleanup on behalf of the derived class
				Cleanup();

				if (isPrimaryController)
				{
					isConnected = false;

					if (itunes != null)
					{
						itunes.OnCOMCallsDisabledEvent -= disabledEvent;
						itunes.OnCOMCallsEnabledEvent -= enabledEvent;
						disabledEvent = null;
						enabledEvent = null;

						try
						{
							itunes.Quit();
						}
						catch
						{
							// no-op
						}
						finally
						{
							Release(itunes);
							itunes = null;
						}

						reset.Set();
						reset.Close();
						reset = null;
					}
				}

				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		#endregion Lifecycle

		#region Handlers

		/// <summary>
		/// When COM interaction is disabled, this forces all subsequent wrappers to block
		/// until interaction is re-enabled.
		/// </summary>
		/// <param name="reason">The reason why COM interaction was disabled.</param>

		private static void DoDisabledEvent (ITCOMDisabledReason reason)
		{
			reset.Reset();
			Console.WriteLine(String.Format("OnCOMCallsDisabledEvent(reason:{0})", reason));
		}


		/// <summary>
		/// When COM interaction is re-enabled, this releases all blocked wrapper calls.
		/// </summary>

		private static void DoEnabledEvent ()
		{
			reset.Set();
			Console.WriteLine("OnCOMCallsEnabledEvent()");
		}

		#endregion Handlers


		//========================================================================================
		// Inheritor interface
		//========================================================================================

		/// <summary>
		/// Inheritors should override if this need to dispose their own private COM
		/// wrapper instances.  This is invoked automatically by the base.Dispose method.
		/// </summary>

		protected virtual void Cleanup ()
		{
			// override this in derived classes
		}


		/// <summary>
		/// Gets the four-part object ID of the current IITObject.
		/// </summary>
		/// <returns>
		/// An ObjectID that represents this IITObject or <b>null</b> if this
		/// instance does not derive from IITObject.
		/// </returns>

		protected ObjectID GetObjectID ()
		{
			if (this is IITObject)
			{
				if (objectID == null)
				{
					int sourceID = 0;
					int playlistID = 0;
					int trackID = 0;
					int databaseID = 0;

					Invoke((Action)delegate
					{
						((IITObject)this).GetITObjectIDs(
							out sourceID, out playlistID, out trackID, out databaseID);
					});

					return new ObjectID(sourceID, playlistID, trackID, databaseID);
				}
			}

			return null;
		}


		/// <summary>
		/// Gets the persisten ID of the current IITObject.
		/// </summary>
		/// <returns>
		/// A PersistentID that represents this IITObject or PersistentID.Empty if this
		/// instance does not derive from IITObject.
		/// </returns>

		protected PersistentID GetPersistentID (IITObject obj)
		{
			if (persistentID.IsEmpty)
			{
				int high = 0;
				int low = 0;

				Invoke((Action)delegate
				{
					object refobj = obj;
					itunes.GetITObjectPersistentIDs(ref refobj, out high, out low);
					// do not Release(refobj) here or we'll blow away the original obj!
				});

				return new PersistentID(high, low);
			}

			return persistentID;
		}


		/// <summary>
		/// Safely invokes the given Action, which is a method with no parameters and
		/// no return value.
		/// </summary>
		/// <param name="action">An Action to perform.</param>

		protected static void Invoke (Action action)
		{
			try
			{
				try
				{
					// WaitOne will return immediately if already set
					// so we can then perform the following action
					reset.WaitOne();
					action();
				}
				catch (COMException)
				{
					// here, WaitOne will block until set and then perform the action again
					reset.WaitOne();
					action();
				}
			}
			catch (AbandonedMutexException) { }
			catch (InvalidComObjectException) { }
			catch (COMException) { }
			catch (ThreadAbortException) { }
#if Debugging
			catch (Exception exc)
			{
				DumpStack(exc, new StackTrace(exc).GetFrames());
			}
#endif

		}


		/// <summary>
		/// Safely invokes the given Func, which is a method with no parameters and a return
		/// value of the specified return type
		/// </summary>
		/// <typeparam name="T">The type to return.</typeparam>
		/// <param name="action">The Func to perform.</param>
		/// <returns>A instance of the generic type T.</returns>

		protected static T Invoke<T> (Func<T> action)
		{
			T result = default(T);
			try
			{
				try
				{
					// WaitOne will return immediately if already set
					// so we can then perform the following action
					reset.WaitOne();
					result = action();
				}
				catch (COMException)
				{
					// here, WaitOne will block until set and then perform the action again
					reset.WaitOne();
					result = action();
				}
			}
			catch (AbandonedMutexException) { return default(T); }
			catch (InvalidComObjectException) { return default(T); }
			catch (COMException) { return default(T); }
			catch (ThreadAbortException) { return default(T); }
#if Debugging
			catch (Exception exc)
			{
				DumpStack(exc, new StackTrace(exc).GetFrames());
			}
#endif
			return result;
		}


#if Debugging
		private static void DumpStack (Exception exc, StackFrame[] frames)
		{
			Console.WriteLine(String.Format("--> '{0}'", exc.Message));

			foreach (StackFrame frame in frames)
			{
				Console.WriteLine(String.Format("--> {0}.{1} @{2},{3}",
					frame.GetMethod().ReflectedType.FullName, frame.GetMethod().Name,
					frame.GetFileLineNumber(), frame.GetFileColumnNumber()));
			}
		}
#endif


		/// <summary>
		/// Releases the given COM object wrapper.  Inheritors should use this in their
		/// Cleanup overrides.
		/// </summary>
		/// <param name="wrapper">The COM wrapper instance.</param>

		protected void Release (object wrapper)
		{
			if (wrapper is MarshalByRefObject)
			{
				while (Marshal.ReleaseComObject(wrapper) > 0) ;
			}
		}
	}
}
