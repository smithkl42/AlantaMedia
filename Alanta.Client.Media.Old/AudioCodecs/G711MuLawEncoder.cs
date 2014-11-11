using System;

namespace Alanta.Client.Media.AudioCodecs
{
	public class G711MuLawEncoder : IAudioEncoder
	{

		public G711MuLawEncoder(AudioFormat inputAudioFormat)
		{
			_inputAudioFormat = inputAudioFormat;
			_byteStep = inputAudioFormat.SamplesPerSecond == 16000 ? 3 : 1;
			_shortStep = inputAudioFormat.SamplesPerSecond == 16000 ? 2 : 1;
		}

		private readonly AudioFormat _inputAudioFormat;
		private readonly int _byteStep;
		private readonly int _shortStep;

		public override string ToString()
		{
			return "G711 MuLawEncoder; InputAudioFormat:" + _inputAudioFormat;
		}

		/// <summary>
		/// Encodes an array of 16-bit samples in a roughly equivalent set of 8-bit samples and downsamples them from 16Khz to 8Khz
		/// </summary>
		/// <param name="rawFrame">A byte-array of the 16-bit 16-Khz source samples</param>
		/// <param name="start">The starting index of the source samples IN BYTES</param>
		/// <param name="length">The length IN BYTES of the samples</param>
		/// <param name="encodedFrame">The byte array in which to place the encoded 8-bit samples</param>
		/// <param name="isSilent">Whether the frame is silent or not</param>
		/// <returns>The length IN BYTES of the encoded data</returns>
		public int Encode(byte[] rawFrame, int start, int length, byte[] encodedFrame, bool isSilent)
		{
			if (isSilent) return 0;
			int encodedPos = 0;
			for (int i = start; i < start + length; i += _byteStep)
			{
				short rawSample = G711.ToShort(rawFrame[i], rawFrame[++i]);
				encodedFrame[encodedPos++] = G711.LinearToULawFast(rawSample);
			}
			return encodedPos;
		}

		/// <summary>
		/// Encodes an array of 16-bit samples in a roughly equivalent set of 8-bit samples and downsamples from 16Khz to 8Khz
		/// </summary>
		/// <param name="rawFrame">A short-array of the 16-bit source samples</param>
		/// <param name="start">The starting index of the source samples IN SHORTS</param>
		/// <param name="length">The length IN SHORTS of the samples</param>
		/// <param name="encodedFrame">The short array in which to place the packed and encoded 8-bit samples</param>
		/// <param name="isSilent">Whether the array contains silence</param>
		/// <returns>The length IN SHORTS of the encoded data</returns>
		public int Encode(short[] rawFrame, int start, int length, short[] encodedFrame, bool isSilent)
		{
			if (isSilent) return 0;
			int encodedPos = 0;
			for (int i = start; i < start + length; i += _shortStep)
			{
				byte b1 = G711.LinearToULawFast(rawFrame[i]);
				i += _shortStep;
				byte b2 = G711.LinearToULawFast(rawFrame[i]);
				encodedFrame[encodedPos++] = G711.ToShort(b1, b2);
			}
			return encodedPos;
		}

		public AudioCodecType CodecType
		{
			get { return AudioCodecType.G711M; }
		}
	}
}
