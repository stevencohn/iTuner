//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Diagnostics;
	using System.Runtime.InteropServices;
	using System.Threading;
	using iTunesLib;


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
		protected static iTunesApp itunes;

		// allow all inheritors to determine if we're still alive
		protected static bool isConnected;

		// single synchronizer to coordinate COM state changes
		private static ManualResetEvent reset;

		private const string LogCategory = "Interaction";

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
			enabledEvent = DoEnabledEvent;
			disabledEvent = DoDisabledEvent;

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

			if (itunes != null)
			{
				itunes.OnCOMCallsEnabledEvent += enabledEvent;
				itunes.OnCOMCallsDisabledEvent += disabledEvent;
			}

			isConnected = true;
		}


		#region Lifecycle

		/// <summary>
		/// Base destructor for all inheritors; do not override.
		/// </summary>

		~Interaction ()
		{
			Dispose(false);
		}


		/// <summary>
		/// Dispose of this instance; do not override.
		/// </summary>

		public void Dispose ()
		{
			Dispose(false);
		}


		/// <summary>
		/// Dispose of this instance, optionally exiting iTunes.
		/// </summary>
		/// <param name="finalRelease"><b>true</b> to exit iTunes.</param>

		public void Dispose (bool finalRelease)
		{
			if (!isDisposed)
			{
				// invoke Cleanup on behalf of the derived class
				Cleanup(finalRelease);

				if (isPrimaryController)
				{
					isConnected = false;

					if (itunes != null)
					{
						try
						{
							itunes.OnCOMCallsDisabledEvent -= disabledEvent;
							itunes.OnCOMCallsEnabledEvent -= enabledEvent;
						}
						catch (InvalidComObjectException)
						{
							// RCW may be released already... why?
						}

						disabledEvent = null;
						enabledEvent = null;

						if (finalRelease)
						{
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

			Logger.WriteLine(LogCategory,
				String.Format("OnCOMCallsDisabledEvent(reason:{0})", reason));
		}


		/// <summary>
		/// When COM interaction is re-enabled, this releases all blocked wrapper calls.
		/// </summary>

		private static void DoEnabledEvent ()
		{
			reset.Set();

			Logger.WriteLine(LogCategory, "OnCOMCallsEnabledEvent()");
		}

		#endregion Handlers


		//========================================================================================
		// Inheritor interface
		//========================================================================================

		/// <summary>
		/// Inheritors should override if this need to dispose their own private COM
		/// wrapper instances.  This is invoked automatically by the base.Dispose method.
		/// </summary>

		protected virtual void Cleanup (bool finalRelease)
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

					Invoke(delegate
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

				if (itunes != null)
				{
					Invoke(delegate
					{
						object refobj = obj;
						itunes.GetITObjectPersistentIDs(ref refobj, out high, out low);
						// do not Release(refobj) here or we'll blow away the original obj!
					});
				}

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
			if (itunes == null)
			{
				return;
			}

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
			catch (Exception exc)
			{
				DumpStack(exc, new StackTrace(exc, true).GetFrames());
			}
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

			if (itunes == null)
			{
				return result;
			}

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
					// here, WaitOne will block until set and then perform the action again.
					// however, this will skip if attempting to export a "protected" file
					// so we specifically watch for ProtectedException below...

					reset.WaitOne();
					result = action();
				}
			}
			catch (AbandonedMutexException) { return default(T); }
			catch (InvalidComObjectException) { return default(T); }
			catch (COMException) { return default(T); }
			catch (ThreadAbortException) { return default(T); }
			catch (ProtectedException)
			{
				throw;
			}
			catch (Exception exc)
			{
				DumpStack(exc, new StackTrace(exc, true).GetFrames());
			}

			return result;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="exc"></param>
		/// <param name="frames"></param>

		private static void DumpStack (Exception exc, StackFrame[] frames)
		{
			Logger.WriteLine(LogCategory, $"--> '{exc.Message}'");

			var count = 0;
			foreach (StackFrame frame in frames)
			{
				var method = frame.GetMethod();
				if (method != null)
				{
					var type = method.ReflectedType;
					if (type != null)
					{
						Logger.WriteLine(LogCategory,
							$"--> [{count}] {type.FullName}.{method.Name} @{frame.GetFileLineNumber()},{frame.GetFileColumnNumber()}");
					}
				}

				count++;
			}
		}


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
