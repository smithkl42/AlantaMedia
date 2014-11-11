using System;
using System.Collections.Generic;
using System.Linq;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Aec
{
	public class PlaybackResults
	{
		public PlaybackResults(TimeSpan duration, int expectedLatency, int filterLength, List<byte[]> recorded, List<byte[]> cancelled)
		{
			Duration = duration;
			ExpectedLatency = expectedLatency;
			FilterLengthInMs = filterLength;
			FilterLengthInSamples = filterLength * (AudioFormat.Default.SamplesPerSecond / 1000);
			Recorded = recorded;
			int recordedStart = recorded.Count / 2;
			RecordedVolumeStdDev = GetStandardDeviation(recorded, recordedStart);
			RecordedVolumeAverage = GetAverage(recorded, recordedStart);
			RecordedRMS = GetRMS(recorded, recordedStart);

			Cancelled = cancelled;
			int cancelledStart = cancelled.Count / 2;
			CancelledVolumeStdDev = GetStandardDeviation(cancelled, cancelledStart);
			CancelledVolumeAverage = GetAverage(cancelled, cancelledStart);
			CancelledRMS = GetRMS(cancelled, cancelledStart);
		}

		public TimeSpan Duration { get; private set; }
		public int ExpectedLatency { get; private set; }
		public int FilterLengthInMs { get; private set; }
		public int FilterLengthInSamples { get; private set; }
		public double RecordedVolumeStdDev { get; private set; }
		public double RecordedVolumeAverage { get; private set; }
		public double RecordedRMS { get; private set; }
		public double CancelledVolumeStdDev { get; private set; }
		public double CancelledVolumeAverage { get; private set; }
		public double CancelledRMS { get; private set; }
		public List<byte[]> Cancelled { get; private set; }
		public List<byte[]> Recorded { get; private set; }

		public double VolumeReduction
		{
			get
			{
				if (RecordedVolumeAverage > 0 && RecordedVolumeStdDev > 0)
				{
					double dropAverage = (RecordedVolumeAverage - CancelledVolumeAverage) / RecordedVolumeAverage;
					double dropStd = (RecordedVolumeStdDev - CancelledVolumeStdDev) / RecordedVolumeStdDev;
					return ((dropAverage + dropStd) / 2) * -100;
				}
				return 0;
			}
		}

		public double VolumeReductionInDecibels
		{
			get
			{
				if (RecordedVolumeAverage > 0 && RecordedVolumeStdDev > 0)
				{
					return 10 * Math.Log10(CancelledRMS / RecordedRMS);
				}
				return 0;
			}
		}

		private static double GetStandardDeviation(List<byte[]> sampleList, int start = 0)
		{
			long sum = 0;
			long sumOfDerivation = 0;
			long totalLength = 0;
			for (int j = start; j < sampleList.Count; j++) // foreach (byte[] samples in sampleList)
			{
				byte[] samples = sampleList[j];
				var shorts = new short[samples.Length / 2];
				Buffer.BlockCopy(samples, 0, shorts, 0, samples.Length);

				totalLength += shorts.Length;
				foreach (short sample in shorts)
				{
					sum += sample;
					sumOfDerivation += (sample * sample);
				}
			}
			double average = sum / (double)totalLength;
			double sumOfDerivationAverage = sumOfDerivation / (double)totalLength;
			return Math.Sqrt(sumOfDerivationAverage - (average * average));
		}

		private static double GetAverage(List<byte[]> sampleList, int start = 0)
		{
			long sum = 0;
			long totalLength = 0;
			for (int j = start; j < sampleList.Count; j++) // (byte[] samples in sampleList)
			{
				byte[] samples = sampleList[j];
				var shorts = new short[samples.Length / 2];
				Buffer.BlockCopy(samples, 0, shorts, 0, samples.Length);

				totalLength += shorts.Length;
				sum = shorts.Aggregate(sum, (current, t) => current + Math.Abs((int)t));
			}
			return sum / (double)totalLength;
		}

		private static double GetRMS(IList<byte[]> sampleList, int start = 0)
		{
			long sum = 0;
			long totalLength = 0;
			for (int j = start; j < sampleList.Count; j++) // foreach (byte[] samples in sampleList)
			{
				byte[] samples = sampleList[j];
				var shorts = new short[samples.Length / 2];
				Buffer.BlockCopy(samples, 0, shorts, 0, samples.Length);

				totalLength += shorts.Length;
				sum = shorts.Aggregate(sum, (current, t) => current + (t * t));
			}
			double average = sum / (double)totalLength;
			return Math.Sqrt(average);
		}
	}
}
