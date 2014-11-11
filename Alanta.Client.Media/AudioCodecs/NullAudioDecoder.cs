using System;

namespace Alanta.Client.Media.AudioCodecs
{
	public class NullAudioDecoder : IAudioDecoder
	{
		public NullAudioDecoder(AudioFormat outputAudioFormat)
		{
			_outputAudioFormat = outputAudioFormat;
		}

		private readonly AudioFormat _outputAudioFormat;

		public int Encode(byte[] frame, int start, int length, byte[] encodedFrame, bool isSilent)
		{
			if (isSilent)
			{
				return 0;
			}
			Buffer.BlockCopy(frame, start, encodedFrame, 0, length);
			return length;
		}

		public int Decode(byte[] frame, int inputStart, int inputLength, byte[] decodedFrame, int outputStart, bool isSilent)
		{
			if (isSilent)
			{
				inputLength = _outputAudioFormat.BytesPerFrame;
				Array.Clear(decodedFrame, outputStart, inputLength);
			}
			else
			{
				Buffer.BlockCopy(frame, inputStart, decodedFrame, 0, inputLength);
			}
			Buffer.BlockCopy(frame, inputStart, decodedFrame, outputStart, inputLength);
			return inputLength;
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

		public int Decode(short[] frame, int inputStart, int inputLength, short[] decodedFrame, int outputStart, bool isSilent)
		{
			if (isSilent)
			{
				inputLength = _outputAudioFormat.SamplesPerFrame;
				Array.Clear(decodedFrame, outputStart, inputLength);
			}
			else
			{
				Buffer.BlockCopy(frame, inputStart * sizeof(short), decodedFrame, outputStart * sizeof(short), inputLength * sizeof(short));
			}
			return inputLength;
		}

		public AudioCodecType CodecType { get { return AudioCodecType.Raw; } }

	}
}
