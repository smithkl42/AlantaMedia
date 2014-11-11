using System;

namespace Alanta.Client.Media
{
	class AudioJitterQueueEntry : IComparable<AudioJitterQueueEntry>
	{
		/// <summary>
		/// A buffer to hold the audio data.
		/// </summary>
		/// <remarks>Big enough to hold two frames, but should normally just contain data for one frame.</remarks>
		public short[] Frame = new short[AudioConstants.FrameBufferSizeInShorts];

		public AudioCodecType AudioCodecType;

		/// <summary>
		/// The length IN SAMPLES of the data contained in the Frame.
		/// </summary>
		public int DataLength;

		/// <summary>
		/// The sequence number of the packet. Used to sort packets when one arrives out of order.
		/// </summary>
		public ushort SequenceNumber;

		/// <summary>
		/// Whether the container the packet arrived in said the packet was silent.
		/// </summary>
		public bool IsSilent;

		/// <summary>
		/// Since the lower sequence number is the higher priority, sorts entries in reverse order, correcting for wrap-around.
		/// </summary>
		/// <param name="other">The other instance which we're comparing ourselves to.</param>
		/// <returns>
		/// -1 if this instance has a lower sequence number
		/// </returns>
		public int CompareTo(AudioJitterQueueEntry other)
		{
			// If the other object is null, we're greater than that object.
			if (other == null)
			{
				return 1;
			}

			// If they're very small and we're very big, this indicates that there's been wraparound, 
			// and they're really bigger than we are, so we would normally return -1.
			// However, the lower sequence number has a higher priority, so return 1 instead of -1.
			// We don't need to check for the opposite case, because it'll be handled correctly
			// through the normal SequenceNumber.CompareTo() case.
			if (other.SequenceNumber < ushort.MinValue + 20 && ushort.MaxValue - 20 < SequenceNumber)
			{
				return -1;
			}

			// Call it this way because we want to sort in reverse order.
			return SequenceNumber.CompareTo(other.SequenceNumber);
		}
	}


}
