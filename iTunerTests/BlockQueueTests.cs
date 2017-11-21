//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using System.ComponentModel;
	using System.Threading;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner.iTunes;


	/// <summary>
	/// 
	/// </summary>

	[TestClass]
	public class BlockingQueueTests : TestBase
	{
		private const int QueueSize = 5;


		/// <summary>
		/// 
		/// </summary>

		[TestMethod]
		public void Queue ()
		{
			using (var queue = new BlockingQueue<string>())
			{
				using (var worker = new BackgroundWorker())
				{
					worker.DoWork += new DoWorkEventHandler(DoWork);
					worker.ProgressChanged += new ProgressChangedEventHandler(DoProgressChanged);
					worker.WorkerReportsProgress = true;
					worker.WorkerSupportsCancellation = true;
					worker.RunWorkerAsync(queue);

					int count = 0;
					while (count < QueueSize)
					{
						string s = queue.Dequeue();
						Console.WriteLine("dequeued " + count + "(" + s + ")");
						count++;
					}

					worker.CancelAsync();
				}

				queue.Clear();

				// priority

				queue.Enqueue("one");
				queue.Enqueue("two");
				queue.Enqueue("thr");

				queue.Enqueue("one1", 1);
				queue.Enqueue("one0", 0);
				queue.Enqueue("two1", 1);
				queue.Enqueue("two0");
				queue.Enqueue("thr1", 1);
				queue.Enqueue("thr0");


				Assert.AreEqual("one", queue.Dequeue());
				Assert.AreEqual("two", queue.Dequeue());
				Assert.AreEqual("thr", queue.Dequeue());
				Assert.AreEqual("one0", queue.Dequeue());
				Assert.AreEqual("two0", queue.Dequeue());
				Assert.AreEqual("thr0", queue.Dequeue());
				Assert.AreEqual("one1", queue.Dequeue());
				Assert.AreEqual("two1", queue.Dequeue());
				Assert.AreEqual("thr1", queue.Dequeue());
			}
		}


		private void DoWork (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;
			BlockingQueue<string> queue = e.Argument as BlockingQueue<string>;

			// slowly populate the queue, add a couple of additional entries to make sure
			// we can clear out any remaining after the worker is stopped
			for (int i = 0; i < QueueSize + 2; i++)
			{
				if (!worker.CancellationPending)
				{
					Thread.Sleep(1000);
					Console.WriteLine("enqueue " + i);
					queue.Enqueue("enqueue " + i);
					worker.ReportProgress(i + 1);
				}
			}
		}


		private void DoProgressChanged (object sender, ProgressChangedEventArgs e)
		{
			Console.WriteLine("progress " + e.ProgressPercentage);
		}
	}
}
