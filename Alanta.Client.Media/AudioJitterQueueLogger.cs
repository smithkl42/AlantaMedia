using System;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	class AudioJitterQueueLogger
	{
		public AudioJitterQueueLogger(MediaStatistics mediaStatistics = null)
		{
			if (mediaStatistics != null)
			{
				_queueLengthCounter = mediaStatistics.RegisterCounter("AudioQueue.Length");
				_queueLengthCounter.AxisMinimum = 0;
				_queueLengthCounter.AxisMaximum = 50;
				_emptyCounter = mediaStatistics.RegisterCounter("AudioQueue.EmptyReads%");
				_emptyCounter.AxisMinimum = 0;
				_emptyCounter.AxisMaximum = 100;
				_fullCounter = mediaStatistics.RegisterCounter("AudioQueue.FullWrites%");
				_fullCounter.AxisMinimum = 0;
				_fullCounter.AxisMaximum = 100;
				_disorderedCounter = mediaStatistics.RegisterCounter("AudioQueue.PacketsOutOfOrder%");
				_disorderedCounter.IsActive = false; // Hide for now.
			}
			AvgMsBetweenReads = AudioConstants.MillisecondsPerFrame;
			_lastResetAt = DateTime.Now;
		}

		public int Reads { get; private set; }
		public int Writes { get; private set; }
		public int WritesOutOfOrder { get; private set; }
		public int ReadsQueueReduced { get; private set; }
		public int ReadsQueueEmpty { get; private set; }
		public int ReadsQueueFull { get; private set; }
		public int ReadsSilent { get; private set; }
		public int ReadsMultiple { get; private set; }
		public double AvgMsBetweenReads { get; private set; }
		public double OverageRatio { get; private set; }
		private readonly Counter _queueLengthCounter;
		private readonly Counter _disorderedCounter;
		private readonly Counter _emptyCounter;
		private readonly Counter _fullCounter;
		private int _resets;
		private const int resetInterval = 200;
		private DateTime _lastResetAt = DateTime.MinValue;

		public void LogRead(PriorityQueue<AudioJitterQueueEntry> queue, int framesBetweenChecks, bool isSilent)
		{
			if (isSilent)
			{
				ReadsSilent++;
			}
			if (++Reads % resetInterval == 0)
			{
				// Calculate the average reads per second.
				var now = DateTime.Now;
				var msSinceLastReset = (now - _lastResetAt).TotalMilliseconds;
				AvgMsBetweenReads = msSinceLastReset / resetInterval;
				OverageRatio = (AvgMsBetweenReads - AudioConstants.MillisecondsPerFrame) / AudioConstants.MillisecondsPerFrame;
				if (OverageRatio < 0) OverageRatio = 0; // This doesn't seem to normally happen.
				if (OverageRatio > .6)
				{
					ClientLogger.Debug("Resetting excessive overage ratio of {0} to {1}", OverageRatio, 0.6);
					OverageRatio = 0.5;
				}
				_lastResetAt = now;

				if (++_resets % 2 == 0)
				{
					ClientLogger.Debug("Writes {0}; Reads {1}; QueueCount {2}; QueueEmpty {3}; QueueFull {4}; QueueReduced {5}; CheckInterval {6}; OutOfOrder {7}; Silent {8}; Multiple: {9}; AvgMss: {10}; OverageRatio: {11}",
						Writes, Reads, queue.Count, ReadsQueueEmpty, ReadsQueueFull, ReadsQueueReduced, framesBetweenChecks, WritesOutOfOrder, ReadsSilent, ReadsMultiple, AvgMsBetweenReads, OverageRatio);
				}
				if (_queueLengthCounter != null)
				{
					_queueLengthCounter.Update(queue.Count);
				}
				if (_disorderedCounter != null)
				{
					_disorderedCounter.Update((float)WritesOutOfOrder * 100 / Writes);
				}
				if (_emptyCounter != null)
				{
					_emptyCounter.Update((float)ReadsQueueEmpty * 100 / Reads);
				}
				if (_fullCounter != null)
				{
					_fullCounter.Update((float)ReadsQueueFull * 100 / Writes);
				}
				Writes = 0;
				Reads = 0;
				ReadsQueueEmpty = 0;
				ReadsQueueFull = 0;
				ReadsQueueReduced = 0;
				WritesOutOfOrder = 0;
				ReadsSilent = 0;
				ReadsMultiple = 0;
			}
		}

		public void LogWrite()
		{
			Writes++;
		}

		public void LogWriteOutOfOrder()
		{
			WritesOutOfOrder++;
		}

		public void LogQueueReduced()
		{
			ReadsQueueReduced++;
		}

		public void LogQueueEmpty()
		{
			ReadsQueueEmpty++;
		}

		public void LogQueueFull()
		{
			ReadsQueueFull++;
		}

		public void LogMultipleRead()
		{
			ReadsMultiple++;
		}
	}

}
