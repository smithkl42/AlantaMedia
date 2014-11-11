using System;
using System.Windows.Media;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using AudioFormat = Alanta.Client.Media.AudioFormat;
using wm = System.Windows.Media;

namespace Alanta.Client.Test.Media
{
	public class TestTimingAudioSink : AudioSink
	{
		#region Constructors

		public TestTimingAudioSink(CaptureSource captureSource, AudioFormat outputAudioFormat)
		{
			CaptureSource = captureSource;
			this.outputAudioFormat = outputAudioFormat;
		}

		#endregion

		#region Fields and Properties
		protected wm.AudioFormat audioFormat;
		protected AudioFormat outputAudioFormat;
		protected ushort desiredSampleSizeInBytes = (AudioConstants.BitsPerSample * AudioConstants.Channels) / 8;
		protected float scalingFactor;
		protected byte[] scaledBuffer = new byte[640 * 100]; // Leave room for ~10 samples.
		protected int sampleBufferStart;
		protected int sampleBufferPosition;

		public event EventHandler<EventArgs<int, DateTime>> FrameArrived;

		public double AverageSubmissionTimeTotal { get { return (DateTime.Now - firstFrameArrivedAt).TotalMilliseconds / totalSamplesProvided; } }

		public double AverageSubmissionTimeRecent { get { return (DateTime.Now - lastResetAt).TotalMilliseconds / recentSamplesProvided; } }

		public double AverageSizeRecent { get { return recentSampleLength / (double)recentSamplesProvided; } }

		public double AverageSizeTotal { get { return totalSampleLength / (double)totalSamplesProvided; } }

		#endregion

		protected override void OnCaptureStarted()
		{
			// Open an RTP stream.
			ClientLogger.Debug("Audio capture started.");
		}

		protected override void OnCaptureStopped()
		{
			ClientLogger.Debug("Audio capture stopped.");
		}

		protected override void OnFormatChange(wm.AudioFormat audioFormat)
		{
			// We may need to do more with this.
			ClientLogger.Debug("The audio format was changed: BitsPerSample = {0}, Channels = {1}, SamplesPerSecond = {2}", AudioConstants.BitsPerSample, AudioConstants.Channels, audioFormat.SamplesPerSecond);
			if (!(audioFormat != null && audioFormat.WaveFormat == WaveFormatType.Pcm))
			{
				throw new ArgumentException("The audio format was not supported.");
			}
			this.audioFormat = audioFormat;

			// e.g., if the actual sample is 32,000 and the desired is 16,000, the scaling factor will be 2.0; 
			// in other words, every other sample would be discarded.
			scalingFactor = audioFormat.SamplesPerSecond * AudioConstants.Channels / (float)(audioFormat.SamplesPerSecond * AudioConstants.Channels);
		}

		private DateTime firstFrameArrivedAt = DateTime.MinValue;
		private DateTime lastResetAt = DateTime.MinValue;
		private long totalSamplesProvided = 0;
		private long totalSampleTime = 0;
		private long totalSampleDuration = 0;
		private long totalSampleLength = 0;
		public long totalMaxSampleLength = long.MinValue;
		public long totalMinSamplelength = long.MaxValue;
		private long recentSamplesProvided = 0;
		private long recentSampleTime = 0;
		private long recentSampleDuration = 0;
		private long recentSampleLength = 0;
		public long recentMaxSampleLength = long.MinValue;
		public long recentMinSampleLength = long.MaxValue;

		public void ResetRecentCounters()
		{
			lastResetAt = DateTime.Now;
			recentSampleDuration = 0;
			recentSampleLength = 0;
			recentSampleTime = 0;
			recentSamplesProvided = 0;
			recentMinSampleLength = long.MaxValue;
			recentMaxSampleLength = long.MinValue;
		}

		protected override void OnSamples(long sampleTime, long sampleDuration, byte[] sampleData)
		{
			try
			{
				totalSamplesProvided++;
				totalSampleTime += sampleTime;
				totalSampleDuration += sampleDuration;
				recentSamplesProvided++;
				recentSampleTime += sampleTime;
				recentSampleDuration += sampleDuration;
				if (firstFrameArrivedAt == DateTime.MinValue)
				{
					firstFrameArrivedAt = DateTime.Now;
					lastResetAt = DateTime.Now;
				}

				int scaledLength = (int)((sampleData.Length / (double)desiredSampleSizeInBytes) / scalingFactor) * desiredSampleSizeInBytes;
				ScaleSampleOntoBuffer(sampleData, scaledLength);
				PullFramesFromBuffer();
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.Message);
			}
		}

		protected virtual void ScaleSampleOntoBuffer(byte[] originalData, int scaledLength)
		{
			// If there's not enough room left in the buffer, move any unprocessed data back to the beginning.
			totalSampleLength += scaledLength;
			recentSampleLength += scaledLength;
			totalMaxSampleLength = Math.Max(totalMaxSampleLength, scaledLength);
			totalMinSamplelength = Math.Min(totalMinSamplelength, scaledLength);
			recentMaxSampleLength = Math.Max(recentMaxSampleLength, scaledLength);
			recentMinSampleLength = Math.Min(recentMinSampleLength, scaledLength);
			if (scaledLength > scaledBuffer.Length - sampleBufferPosition)
			{
				sampleBufferPosition -= sampleBufferStart;
				Buffer.BlockCopy(scaledBuffer, sampleBufferStart, scaledBuffer, 0, sampleBufferPosition);
				sampleBufferStart = 0;
			}

			// For each sample in the scaled buffer, determine the best location to pull from in the original sample buffer.
			for (int scaledSamplePosition = 0; scaledSamplePosition < scaledLength; scaledSamplePosition += desiredSampleSizeInBytes)
			{
				// Get the best original sample, aligned on sample size boundaries.
				int originalSamplePosition = (int)((scaledSamplePosition / (double)desiredSampleSizeInBytes) * scalingFactor) * desiredSampleSizeInBytes;
				// Assumes that this is only two bytes -- no loop for extra speed.
				// Note that this is ~ twice as fast as Buffer.BlockCopy().
				scaledBuffer[sampleBufferPosition++] = originalData[originalSamplePosition++];
				scaledBuffer[sampleBufferPosition++] = originalData[originalSamplePosition];
			}
		}

		protected virtual void PullFramesFromBuffer()
		{
			if (sampleBufferPosition >= outputAudioFormat.BytesPerFrame)
			{
				// Loop through the buffer, pulling a frame's worth of samples at a time, until there's no more room.
				for (; sampleBufferPosition - sampleBufferStart >= outputAudioFormat.BytesPerFrame; sampleBufferStart += outputAudioFormat.BytesPerFrame)
				{
					var frame = new byte[outputAudioFormat.BytesPerFrame];
					Buffer.BlockCopy(scaledBuffer, sampleBufferStart, frame, 0, outputAudioFormat.BytesPerFrame);
					SubmitFrame(frame);
				}

				// If there are no more remaining bytes, move the pointer to the start of the buffer.
				if (sampleBufferPosition == sampleBufferStart)
				{
					sampleBufferPosition = 0;
					sampleBufferStart = 0;
				}
			}
		}

		protected virtual void SubmitFrame(byte[] frame)
		{
			DateTime now = DateTime.Now;
			int volume = GetVolume(frame);
			if (FrameArrived != null)
			{
				FrameArrived(this, new EventArgs<int, DateTime>(volume, now));
			}
		}

		protected virtual int GetVolume(byte[] frame)
		{
			// Convert the bytes to shorts.
			var samples = new short[frame.Length / 2];
			Buffer.BlockCopy(frame, 0, samples, 0, frame.Length);

			// Set every sample to its absolute value.
			short maxVolume = 0;
			for (int i = 0; i < samples.Length; i++)
			{
				if (samples[i] == short.MinValue)
				{
					samples[i] = short.MaxValue;
				}
				else
				{
					samples[i] = Math.Abs(samples[i]);
				}
				if (samples[i] > maxVolume) maxVolume = samples[i];
			}
			return maxVolume;

			// Find the peaks and calculate their averages.
			//int total = 0;
			//int peaks = 0;
			//for (int i = 2; i < samples.Length - 2; i++)
			//{
			//    if (samples[i] > samples[i - 1] && samples[i] > samples[i-2] && samples[i] > samples[i + 1] && samples[i] > samples[i+2])
			//    {
			//        peaks++;
			//        total += samples[i];
			//    }
			//}
			//return total / peaks;
		}

	}
}
