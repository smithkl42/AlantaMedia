
namespace Alanta.Client.Media.AudioCodecs
{
	public interface IAudioEncoder
	{
		/// <returns>Length *in bytes* of the encoded data</returns>
		int Encode(byte[] rawFrame, int start, int length, byte[] encodedFrame, bool isSilent);

		/// <returns>Length *in shorts* of the encoded data</returns>
		int Encode(short[] rawFrame, int start, int length, short[] encodedFrame, bool isSilent);

		AudioCodecType CodecType { get; }
	}

	public interface IAudioDecoder
	{
		/// <returns>Length *in bytes* of the decoded data</returns>
		int Decode(byte[] encodedFrame, int inputStart, int inputLength, byte[] decodedFrame, int decodedStart, bool isSilent);

		/// <returns>Length *in shorts* of the decoded data</returns>
		int Decode(short[] encodedFrame, int inputStart, int inputLength, short[] decodedFrame, int decodedStart, bool isSilent);

		AudioCodecType CodecType { get; }
	}


}
