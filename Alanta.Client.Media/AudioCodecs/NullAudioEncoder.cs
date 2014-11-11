using System;

namespace Alanta.Client.Media.AudioCodecs
{
	public class NullAudioEncoder : IAudioEncoder
	{
		public int Encode(byte[] frame, int start, int length, byte[] encodedFrame, bool isSilent)
		{
			if (isSilent)
			{
				return 0;
			}
			Buffer.BlockCopy(frame, start, encodedFrame, 0, length);
			return length;
		}

		public int Encode(short[] frame, int start, int length, short[] encodedFrame, bool isSilent)
		{
			if (isSilent)
			{
				return 0;
			}
			Buffer.BlockCopy(frame, start * sizeof(short), encodedFrame, 0, length * sizeof(short));
			return length;
		}

		public AudioCodecType CodecType { get { return AudioCodecType.Raw; } }
	}
}
