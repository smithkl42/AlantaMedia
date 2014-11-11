using System;
namespace Alanta.Client.Media
{
	public interface IAudioJitter
	{
		/// <summary>
		/// Reads the samples from the audio jitter.
		/// </summary>
		/// <param name="outputBuffer">The buffer onto which the samples are written.</param>
		/// <returns>The length IN SAMPLES of the returned data.</returns>
		int ReadSamples(short[] outputBuffer);

		/// <summary>
		/// Writes the samples to the audio jitter.
		/// </summary>
		/// <param name="samples">A ByteStream containing the samples.</param>
		/// <param name="sequenceNumber">The sequence number of the packet</param>
		/// <param name="audioCodecType">The audio codec used to encode the packet</param>
		/// <param name="isSilent">Whether the packet is silent</param>
		void WriteSamples(ByteStream samples, ushort sequenceNumber, AudioCodecType audioCodecType, bool isSilent);

		/// <summary>
		/// Writes the samples to the audio jitter.
		/// </summary>
		/// <param name="samples">An array of samples. This will typically either be a byte[] or a short[].</param>
		/// <param name="start">The offset IN BYTES from the start of the array where the data begins.</param>
		/// <param name="dataLength">The length IN BYTES of the data to be copied to the jitter buffer.</param>
		/// <param name="sequenceId">The read sequence number.</param>
		/// <param name="audioCodecType">The audio codec used to encode the packet</param>
		/// <param name="isSilent">Whether the packet is silent</param>
		void WriteSamples(Array samples, int start, int dataLength, ushort sequenceId, AudioCodecType audioCodecType, bool isSilent);

		/// <summary>
		/// Resets the jitter buffer.
		/// </summary>
		void Reset();
	}
}
