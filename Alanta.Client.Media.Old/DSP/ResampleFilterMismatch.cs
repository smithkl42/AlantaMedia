using System;

namespace Alanta.Client.Media.Dsp
{
	/// <summary>
	/// Resamples audio data from one format to another, and applies a correction factor based on
	/// whether data is being ready too quickly or too slowly.
	/// </summary>
	public class ResampleFilterMismatch : ResampleFilter
	{
		#region Constructors
		public ResampleFilterMismatch(AudioFormat input, AudioFormat output)
			: base(input, output)
		{
			maxDrift = output.BytesPerFrame / 5;
		}
		#endregion

		/// <summary>
		/// Don't attempt to recalculate the correction factor unless at least this many frames have been processed since the last recalculation.
		/// </summary>
		private const int minFramesBetweenResets = 100;

		private int recentFrames;

		private bool firstFrameArrived;

		private readonly int maxDrift;
		private int unreadBytesAtLastCorrection;

		public override void Write(byte[] sampleData)
		{
			recentFrames++;
			firstFrameArrived = true;

			// Drift represents how many more bytes are in the buffer now than at the last correction.
			// If everything is perfect, drift will be zero.
			// If we're reading slower than packets are coming in, drift will be positive, i.e., the buffer is growing too much. We should decrease the correction factor.
			// If we're reading faster than packets are coming in, drift will be negative, i.e., we're chewing through the buffer too fast. We should increase the correction factor.
			int drift = UnreadBytes - unreadBytesAtLastCorrection;

			// If we've rea
			if (drift > maxDrift)
			{
				_logger.LogReadingTooSlow(UnreadBytes);
				DecreaseCorrectionFactor();
			}
			else if (drift < -maxDrift)
			{
				_logger.LogReadingTooFast(UnreadBytes);
				IncreaseCorrectionFactor();
			}
			base.Write(sampleData);
		}


		public override bool Read(Array outBuffer, out bool moreFrames)
		{
			bool successful = base.Read(outBuffer, out moreFrames);
			if (!successful && firstFrameArrived)
			{
				// We've been reading too quickly, so we need pad the buffer more, i.e., increase the correction factor.
				IncreaseCorrectionFactor();
			}
			return successful;
		}

		private void IncreaseCorrectionFactor()
		{
			if (recentFrames > minFramesBetweenResets)
			{
				CorrectionFactor += .01;
				recentFrames = 0;
				unreadBytesAtLastCorrection = UnreadBytes;
			}
		}

		private void DecreaseCorrectionFactor()
		{
			if (recentFrames > minFramesBetweenResets)
			{
				CorrectionFactor -= .01;
				recentFrames = 0;
				unreadBytesAtLastCorrection = UnreadBytes;
			}
		}

	}
}
