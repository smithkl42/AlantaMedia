using System;
using System.Diagnostics;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media.Dsp
{
	public class ResampleFilterLogger
	{
		public string InstanceName { get; set; }

		private DateTime _firstFrameSubmittedAt = DateTime.MinValue;
		private DateTime _lastResetAt = DateTime.MinValue;
		private long _totalSamplesSubmitted;
		private long _totalOriginalLength;
		private long _totalScaledLength;
		private long _totalMaxSampleLength = long.MinValue;
		private long _totalMinSamplelength = long.MaxValue;
		private long _recentOriginalLength;
		private long _recentSamplesSubmitted;
		private long _recentScaledLength;
		private long _recentMaxScaledLength = long.MinValue;
		private long _recentMinScaledLength = long.MaxValue;
		private long _totalCorrectedLength;
		private long _totalMinCorrectedLength = long.MaxValue;
		private long _totalMaxCorrectedLength = long.MinValue;
		private long _recentCorrectedLength;
		private long _recentMinCorrectedLength = long.MaxValue;
		private long _recentMaxCorrectedLength = long.MinValue;
		private long _recentReadsTooSlow;
		private long _totalReadsTooSlow;
		private long _recentReadsTooFast;
		private long _totalReadsTooFast;
		private double _minCorrectionFactor = double.MaxValue;
		private double _maxCorrectionFactor = double.MinValue;

		private DateTime _firstFrameRetrievedAt = DateTime.MinValue;
		private long _totalSamplesRetrieved;
		private long _recentSamplesRetrieved;
		private long _totalSampleLengthRetrieved;
		private long _recentSampleLengthRetrieved;

		private const int reportingInterval = 1000;

		public double AverageSubmissionTimeTotal { get { return _totalSamplesSubmitted > 0 ? (DateTime.Now - _firstFrameSubmittedAt).TotalMilliseconds / _totalSamplesSubmitted : 0; } }
		public double AverageSubmissionTimeRecent { get { return _recentSamplesSubmitted > 0 ? (DateTime.Now - _lastResetAt).TotalMilliseconds / _recentSamplesSubmitted : 0; } }
		public double AverageOriginalLengthRecent { get { return _recentSamplesSubmitted > 0 ? _recentOriginalLength / _recentSamplesSubmitted : 0; } }
		public double AverageOriginalLengthTotal { get { return _totalSamplesSubmitted > 0 ? _totalOriginalLength / _totalSamplesSubmitted : 0; } }
		public double AverageScaledLengthRecent { get { return _recentSamplesSubmitted > 0 ? _recentScaledLength / _recentSamplesSubmitted : 0; } }
		public double AverageScaledLengthTotal { get { return _totalSamplesSubmitted > 0 ? _totalScaledLength / _totalSamplesSubmitted : 0; } }
		public double AverageCorrectedLengthRecent { get { return _recentSamplesSubmitted > 0 ? _recentCorrectedLength / _recentSamplesSubmitted : 0; } }
		public double AverageCorrectedLengthTotal { get { return _totalSamplesSubmitted > 0 ? _totalCorrectedLength / _totalSamplesSubmitted : 0; } }

		public double AverageRetrievalTimeTotal { get { return _totalSamplesRetrieved > 0 ? (DateTime.Now - _firstFrameSubmittedAt).TotalMilliseconds / _totalSamplesSubmitted : 0; } }
		public double AverageRetrievalTimeRecent { get { return _recentSamplesRetrieved > 0 ? (DateTime.Now - _lastResetAt).TotalMilliseconds / _recentSamplesSubmitted : 0; } }
		public double AverageSamplesLengthRetrievedTotal { get { return _totalSamplesRetrieved > 0 ? _totalSampleLengthRetrieved / _totalSamplesRetrieved : 0; } }
		public double AverageSamplesLengthRetrievedRecent { get { return _recentSamplesRetrieved > 0 ? _recentSampleLengthRetrieved / _recentSamplesRetrieved : 0; } }

		private void ResetRecentCounters(DateTime now)
		{
			lock (this)
			{
				_lastResetAt = now;
				_recentScaledLength = 0;
				_recentSamplesSubmitted = 0;
				_recentMinScaledLength = long.MaxValue;
				_recentMaxScaledLength = long.MinValue;
				_recentCorrectedLength = 0;
				_recentMinCorrectedLength = long.MaxValue;
				_recentMaxCorrectedLength = long.MinValue;
				_recentReadsTooFast = 0;
				_recentReadsTooSlow = 0;
				_recentSamplesRetrieved = 0;
				_recentSampleLengthRetrieved = 0;
			}
		}

		[Conditional("DEBUG")]
		public void LogSampleSubmitted(byte[] sampleData, int scaledLength, int correctedLength, double correctionFactor)
		{
			var now = DateTime.Now;
			_totalSamplesSubmitted++;
			_recentSamplesSubmitted++;
			if (_firstFrameSubmittedAt == DateTime.MinValue)
			{
				_firstFrameSubmittedAt = now;
				_lastResetAt = now;
			}

			lock (this)
			{
				_totalOriginalLength += sampleData.Length;
				_recentOriginalLength += sampleData.Length;
				_totalScaledLength += scaledLength;
				_recentScaledLength += scaledLength;
				_totalMaxSampleLength = Math.Max(_totalMaxSampleLength, scaledLength);
				_totalMinSamplelength = Math.Min(_totalMinSamplelength, scaledLength);
				_recentMaxScaledLength = Math.Max(_recentMaxScaledLength, scaledLength);
				_recentMinScaledLength = Math.Min(_recentMinScaledLength, scaledLength);

				_totalCorrectedLength += correctedLength;
				_recentCorrectedLength += correctedLength;
				_totalMaxCorrectedLength = Math.Max(_totalMaxCorrectedLength, correctedLength);
				_totalMinCorrectedLength = Math.Min(_totalMinCorrectedLength, correctedLength);
				_recentMaxCorrectedLength = Math.Max(_recentMaxCorrectedLength, correctedLength);
				_recentMinCorrectedLength = Math.Min(_recentMinCorrectedLength, correctedLength);
			}

			if (_totalSamplesSubmitted % reportingInterval == 0)
			{
				string stats = string.Format(
					"Instance: {0}\r\n" +
					"Total Submissions: {1}\r\n" +
					"Total Retrievals: {2}\r\n" +
					"Recent Submissions: {3}\r\n" +
					"Recent Retrievals: {4}\r\n" +
					"Total Avg Submission Time: {5}\r\n" +
					"Total Avg Retrieval Time: {6}\r\n" +
					"Recent Avg Submission Time: {7}\r\n" +
					"Recent Avg Retrieval Time: {8}\r\n" +
					"Total Avg Original Length: {9}\r\n" +
					"Total Avg Scaled Length: {10}\r\n" +
					"Total Avg Retrieval Length: {11}\r\n" +
					"Recent Avg Original Length: {12}\r\n" +
					"Recent Avg Scaled Length: {13}\r\n" +
					"Recent Avg Retrieval Length: {14}",
					InstanceName, // 0
					_totalSamplesSubmitted, // 1
					_totalSamplesRetrieved, // 2
					_recentSamplesSubmitted, // 3
					_recentSamplesRetrieved, // 4
					AverageSubmissionTimeTotal, // 5
					AverageRetrievalTimeTotal, // 6
					AverageSubmissionTimeRecent, // 7
					AverageRetrievalTimeRecent, // 8 
					AverageOriginalLengthTotal, // 9
					AverageScaledLengthTotal, // 10
					AverageSamplesLengthRetrievedTotal, // 11
					AverageOriginalLengthRecent, //12
					AverageScaledLengthRecent, // 13
					AverageSamplesLengthRetrievedRecent); // 14
				ClientLogger.Debug(stats);
				ResetRecentCounters(DateTime.Now);
			}
		}

		[Conditional("DEBUG")]
		public void LogSamplesRetrieved(int length)
		{
			var now = DateTime.Now;
			_totalSamplesRetrieved++;
			_recentSamplesRetrieved++;
			if (_firstFrameRetrievedAt == DateTime.MinValue)
			{
				_firstFrameRetrievedAt = now;
			}
			_totalSampleLengthRetrieved += length;
			_recentSampleLengthRetrieved += length;
		}

		[Conditional("DEBUG")]
		public void LogCorrectionFactorReset(double correctionFactor)
		{
			_maxCorrectionFactor = Math.Max(_maxCorrectionFactor, correctionFactor);
			_minCorrectionFactor = Math.Min(_minCorrectionFactor, correctionFactor);
			double range = _maxCorrectionFactor - _minCorrectionFactor;
			string stats = string.Format(
				"Instance: {0}\r\n" +
				"Total frames: {1}\r\n" +
				"Avg Arrival Time Total: {2}\r\n" +
				"Avg Arrival Time Recent: {3}\r\n" +
				"Avg Corrected Length Total: {4}\r\n" +
				"Avg Corrected Length Recent: {5}\r\n" +
				"Max Corrected Length Total: {6}\r\n" +
				"Min Corrected Length Total: {7}\r\n" +
				"Max Corrected Length Recent: {8}\r\n" +
				"Min Corrected Length Recent: {9}\r\n" +
				"Reads Too Fast Total: {10}\r\n" +
				"Reads Too Fast Recent: {11}\r\n" +
				"Reads Too Slow total: {12}\r\n" +
				"Reads Too Slow Recent: {13}\r\n" +
				"Current Correction Factor: {14}\r\n" +
				"Max Correction Factor: {15}\r\n" +
				"Min Correction Factor: {16}\r\n" +
				"Range Correction Factor: {17}",
				InstanceName,
				_totalSamplesSubmitted,
				AverageSubmissionTimeTotal,
				AverageSubmissionTimeRecent,
				AverageCorrectedLengthTotal,
				AverageCorrectedLengthRecent,
				_totalMaxCorrectedLength,
				_totalMinCorrectedLength,
				_recentMaxCorrectedLength,
				_recentMinCorrectedLength,
				_totalReadsTooFast,
				_recentReadsTooFast,
				_totalReadsTooSlow,
				_recentReadsTooSlow,
				correctionFactor,
				_maxCorrectionFactor,
				_minCorrectionFactor,
				range);
			ResetRecentCounters(DateTime.Now);
			ClientLogger.Debug(stats);
		}

		internal void LogReadingTooSlow(int unreadBytes)
		{
			_recentReadsTooSlow++;
			_totalReadsTooSlow++;
		}

		internal void LogReadingTooFast(int unreadBytes)
		{
			_recentReadsTooFast++;
			_totalReadsTooFast++;
		}
	}
}
