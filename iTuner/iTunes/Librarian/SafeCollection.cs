//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Threading;


	/// <summary>
	/// Provides a simple thread-safe wrapper of a generic collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>

	internal class SafeCollection<T> : List<T>, INotifyCollectionChanged
	{

		private ReaderWriterLockSlim slim;


		/// <summary>
		/// Initialize a new instance for the current dispatcher.
		/// </summary>

		public SafeCollection ()
		{
			slim = new ReaderWriterLockSlim();
		}


		/// <summary>
		/// 
		/// </summary>

		public event NotifyCollectionChangedEventHandler CollectionChanged;


		/// <summary>
		/// Add an object to the end of the list.
		/// </summary>
		/// <param name="item"></param>

		public new void Add (T item)
		{
			slim.EnterWriteLock();
			try
			{
				base.Add(item);

				if (CollectionChanged != null)
				{
					CollectionChanged(this,
						new NotifyCollectionChangedEventArgs(
							NotifyCollectionChangedAction.Add, item));
				}
			}
			finally
			{
				slim.ExitWriteLock();
			}
		}


		/// <summary>
		/// Removes all elements from the list.
		/// </summary>

		public new void Clear ()
		{
			slim.EnterWriteLock();
			try
			{
				base.Clear();

				if (CollectionChanged != null)
				{
					CollectionChanged(this,
						new NotifyCollectionChangedEventArgs(
							NotifyCollectionChangedAction.Reset));
				}
			}
			finally
			{
				slim.ExitWriteLock();
			}
		}


		/// <summary>
		/// Determines whether an element is in the list.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>

		public new bool Contains (T item)
		{
			bool contains = false;

			slim.EnterReadLock();
			try
			{
				contains = base.Contains(item);
			}
			finally
			{
				slim.ExitReadLock();
			}

			return contains;
		}


		/// <summary>
		/// Inserts an object within this list at the given index.
		/// </summary>
		/// <param name="index">Index of new item.</param>
		/// <param name="item">Object to insert.</param>

		public new void Insert (int index, T item)
		{
			slim.EnterWriteLock();
			try
			{
				base.Insert(index, item);

				if (CollectionChanged != null)
				{
					CollectionChanged(this,
						new NotifyCollectionChangedEventArgs(
							NotifyCollectionChangedAction.Add, item));
				}
			}
			finally
			{
				slim.ExitWriteLock();
			}
		}


		/// <summary>
		/// Removes the first occurrence of a specific object from the list.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>

		public new bool Remove (T item)
		{
			bool removed = false;

			slim.EnterWriteLock();
			try
			{
				removed = base.Remove(item);

				if (CollectionChanged != null)
				{
					CollectionChanged(this,
						new NotifyCollectionChangedEventArgs(
							NotifyCollectionChangedAction.Remove, item));
				}
			}
			finally
			{
				slim.ExitWriteLock();
			}

			return removed;
		}
	}
}
