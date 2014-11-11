using System;

namespace Alanta.Client.Media.Dsp
{
	/// <summary>
	/// Resamples inbound audio data from one format to another, applying a correction factor calculated
	/// by observing whether the data is arriving on larger or smaller than 20 ms boundaries. In other words,
	/// it outputs a set amount of data on 20 ms boundaries (on average), even if the data is arriving faster
	/// or slower than that.
	/// </summary>
	/// <remarks>
	/// This class is designed to correct for the fact that many different soundcards submit the right amount of data, 
	/// but on other than 20 ms intervals, i.e., every 19.5 ms, or every 20.5 ms. Over time, that plays havoc with 
	/// jitter buffers in other parts of the system. This class corrects for it by observing how often the
	/// data is submitted, and if it's greater or less than 20 ms, resamples the data and applies a correction factor to it.
	/// Needless to say, its assumption is that the computer's clock is, in fact, accurate.
	/// </remarks>
	public class ResampleFilterTiming : ResampleFilter
	{
		#region Constructors

		public ResampleFilterTiming(AudioFormat input, AudioFormat output)
			: base(input, output)
		{
		}

		#endregion

		#region Fields and Properties
		private DateTime lastResetAt = DateTime.MinValue;
		private long totalFrames;
		private long recentFrames;
		private long recentSumScaledSize = 0;

		/// <summary>The current date/time.</summary>
		/// <remarks>By allowing the current date/time to be set externally, it simplifies unit testing dramatically.</remarks>
		public DateTime Now
		{
			get { return now ?? DateTime.Now; }
			set { now = value; }
		}
		private DateTime? now;

		/// <summary>
		/// Make sure that we recalculate the correction factor at least this often.
		/// </summary>
		public const int MaxFramesBetweenResets = 200;

		/// <summary>
		/// Ignore the first 'n' frames because the various pieces are still spinning up and whatever synchronization issues are present
		/// can't be measured yet.
		/// </summary>
		public const int InitialFramesToIgnore = 200;

		#endregion

		public override void Write(byte[] sampleData)
		{
			// Only start calculating timing, etc. once we're past the initial spin-up period.
			totalFrames++;
			if (RecordTimingStats())
			{
				recentFrames++;
				if (lastResetAt == DateTime.MinValue)
				{
					lastResetAt = Now;
				}
			}

			// If enough time has passed, recalculate the correction factor.
			if (recentFrames >= MaxFramesBetweenResets)
			{
				RecalculateCorrectionFactor();
			}

			base.Write(sampleData);
		}

		/// <summary>
		/// Given the scaled size of the sample, returns the appropriate size to scale it to given how often frames are actually arriving.
		/// </summary>
		/// <remarks>This is our workaround for the MS bug https://connect.microsoft.com/metro/feedback/details/564061/audiosink-onsamples-is-not-called-on-time-on-some-machines </remarks>
		protected override int GetCorrectedLength(int scaledLength)
		{
			if (RecordTimingStats())
			{
				recentSumScaledSize += scaledLength;
			}
			return (int)(scaledLength * CorrectionFactor);
		}

		private bool RecordTimingStats()
		{
			return totalFrames > InitialFramesToIgnore;
		}

		private void RecalculateCorrectionFactor()
		{
			lock (this)
			{
				// The time ratio SHOULD be 1.0, but on some machines will be larger because it doesn't submit the frames often enough.
				double expectedMilliseconds = recentFrames * OutputMillisecondsPerFrame;
				double actualMilliseconds = (Now - lastResetAt).TotalMilliseconds;
				double timeRatio = actualMilliseconds / expectedMilliseconds;

				// The size ratio also SHOULD be 1.0, but  sometimes samples will get submitted that are larger or smaller than expected.
				long expectedScaledSum = OutputBytesPerFrame * recentFrames;
				double sizeRatio = recentSumScaledSize / (double)expectedScaledSum;

				// If the time ratio is larger than 1.0 (e.g., 1.025), this normally means that we haven't received enough frames.  But if 
				// the size ratio is also larger (e.g., also 1.025), then it just means that larger samples were submitted, and the timing doesn't (really) matter.
				// The key problem comes when the time ratio is larger than the size ratio, e.g., 1.025 : 1.00.  So if that's the case, 
				// it means that it's taking longer to get the right number of samples submitted, and hence, we need to apply a correction factor.
				// This correction factor will be applied to the scaledSize of the sample.  So if normally the incoming bytes would be downsampled to
				// 640 bytes, a correction factor of 1.025 would mean that they would get sampled down to 656 bytes instead.  Only 640 of those bytes
				// would get submitted to the media controller, leaving 16 bytes in the queue.  After ~40 frames, we would have built up an extra frame 
				// worth of data, and that 640 bytes would get submitted, emptying the queue, and filling up the extra space.
				double rawCorrectionFactor = timeRatio / sizeRatio;
				CorrectionFactor = Math.Max(.9, Math.Min(rawCorrectionFactor, 1.1));
				// correctionFactorCheck = Math.Min(correctionFactorCheck + correctionFactorCheckIncrement, maxCorrectionFactorCheck);

				_logger.LogCorrectionFactorReset(CorrectionFactor);

				recentFrames = 0;
				recentSumScaledSize = 0;
				lastResetAt = Now;
			}
		}



	}
}
