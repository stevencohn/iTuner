//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;
	using System.Threading;


	/// <summary>
	/// Represents a prioritorized FIFO collection of objects.  This is a thread-safe
	/// collection that blocks dequeue requests until an item is available in the queue.
	/// </summary>

	internal class BlockingQueue<T> : IDisposable
	{
		private List<Entry> queue;
		private object sync;
		private bool isDisposed;


		private class Entry
		{
			public uint Priority;
			public T Value;

			public Entry (T obj, uint priority)
			{
				this.Value = obj;
				this.Priority = priority;
			}
		}


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Initialize a new empty instance.
		/// </summary>

		public BlockingQueue ()
			: base()
		{
			this.queue = new List<Entry>();
			this.sync = new object();
			this.isDisposed = false;
		}


		/// <summary>
		/// Clear the queue and free up waiting threads.
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				isDisposed = true;

				lock (sync)
				{
					queue.Clear();
					Monitor.PulseAll(sync);
				}
			}
		}


		/// <summary>
		/// Gets the number of items current waiting in the queue.
		/// </summary>

		public int Count
		{
			get { return queue.Count; }
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Remove all objects from the Queue.
		/// </summary>

		public void Clear ()
		{
			lock (sync)
			{
				queue.Clear();
			}
		}


		/// <summary>
		/// Removes and returns the object at the beginning of the Queue.
		/// This is a blocking operation: if the queue is empty, this method waits
		/// until a new entry is added, otherwise the top entry is returned immediately.
		/// </summary>
		/// <returns>The first T object in queue.</returns>

		public T Dequeue ()
		{
			lock (sync)
			{
				while (!isDisposed && (queue.Count == 0))
				{
					Monitor.Wait(sync);
				}

				T obj = default(T);

				try
				{
					// may be zero when shutting down?
					if (queue.Count > 0)
					{
						obj = queue[0].Value;
						queue.RemoveAt(0);
					}
				}
				catch
				{
					// no-op: may end up here when shutting down
				}

				return obj;
			}
		}


		/// <summary>
		/// Adds an object to the Queue after other priority 0 objects but before lesser
		/// priority objects.
		/// </summary>
		/// <param name="obj">Object to put in queue</param>
		/// <returns>
		/// Returns the index within the queue at which this object was inserted.
		/// </returns>

		public int Enqueue (T obj)
		{
			return Enqueue(obj, 0);
		}


		/// <summary>
		/// Adds an object with the specifed priority to the queue.  This object is inserted
		/// after all objects of the same priority but before lesser priority objects.
		/// </summary>
		/// <param name="obj">Object to put in queue.</param>
		/// <param name="priority">The priority of this object.</param>
		/// <returns>
		/// Returns the index within the queue at which this object was inserted.
		/// </returns>
		/// <remarks>
		/// Priorities are specified as integer values starting at zero, where zero is top priority,
		/// one is less priority than zero, two is less priority than one, etc.
		/// </remarks>

		public int Enqueue (T obj, uint priority)
		{
			int index = 0;

			lock (sync)
			{
				while ((index < queue.Count) && (priority >= queue[index].Priority))
				{
					index++;
				}

				if (index >= queue.Count)
				{
					queue.Add(new Entry(obj, priority));
				}
				else
				{
					queue.Insert(index, new Entry(obj, priority));
				}

				Monitor.Pulse(sync);
			}

			return index;
		}
	}
}
