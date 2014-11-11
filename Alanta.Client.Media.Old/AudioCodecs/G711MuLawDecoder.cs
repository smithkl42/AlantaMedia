using System;

namespace Alanta.Client.Media.AudioCodecs
{
	public class G711MuLawDecoder : IAudioDecoder
	{

		public G711MuLawDecoder(AudioFormat outputAudioFormat)
		{
			_outputAudioFormat = outputAudioFormat;
		}

		private readonly AudioFormat _outputAudioFormat;

		/// <summary>
		/// Decodes an 8Khz array of 8-bit encoded values into an equivalent set of 16-bit samples (and upsamples them to 16Khz if necessary).
		/// </summary>
		/// <param name="encodedFrame">A byte array with the encoded values</param>
		/// <param name="inputStart">The starting position IN BYTES of the samples</param>
		/// <param name="inputLength">The length of the samples IN BYTES</param>
		/// <param name="decodedFrame">The output frame</param>
		/// <param name="outputStart">The starting position IN BYTES of the output buffer where the decoded data should be written.</param>
		/// <param name="isSilent">Whether the samples contain silence</param>
		/// <returns>The length of the decoded samples IN BYTES</returns>
		public int Decode(byte[] encodedFrame, int inputStart, int inputLength, byte[] decodedFrame, int outputStart, bool isSilent)
		{
			if (isSilent)
			{
				Array.Clear(decodedFrame, outputStart, _outputAudioFormat.BytesPerFrame);
				return _outputAudioFormat.BytesPerFrame;
			}

			int end = inputStart + inputLength;
			int decodedPos = outputStart;

			if (_outputAudioFormat.SamplesPerSecond == 16000)
			{
				// Decode
				for (int i = inputStart; i < end; i++)
				{
					short decodedSample = G711.ULawToLinearFast(encodedFrame[i]);
					G711.FromShort(decodedSample, out decodedFrame[decodedPos++], out decodedFrame[decodedPos++]);
					decodedPos += 2;
				}

				// Upsample
				short next = 0;
				for (int i = 2; i < decodedPos - 2; i += 4)
				{
					short previous = G711.ToShort(decodedFrame[i - 2], decodedFrame[i - 1]);
					next = G711.ToShort(decodedFrame[i + 2], decodedFrame[i + 3]);
					G711.FromShort((short)((previous + next) / 2), out decodedFrame[i], out decodedFrame[i + 1]);
				}
				G711.FromShort(next, out decodedFrame[decodedPos - 2], out decodedFrame[decodedPos - 1]); // Cheat on the last sample
			}
			else // if outputAudioFormat.SamplesPerSecond = 8000
			{
				// Decode
				for (int i = inputStart; i < end; i++)
				{
					short decodedSample = G711.ULawToLinearFast(encodedFrame[i]);
					G711.FromShort(decodedSample, out decodedFrame[decodedPos++], out decodedFrame[decodedPos++]);
				}
			}
			return decodedPos - outputStart;
		}

		/// <summary>
		/// Decodes an 8Khz array of 8-bit encoded values into an equivalent set of 16-bit samples
		/// </summary>
		/// <param name="encodedFrame">A short array with the encoded values</param>
		/// <param name="inputStart">The starting position IN SHORTS of the samples</param>
		/// <param name="inputLength">The length of the samples IN SHORTS</param>
		/// <param name="decodedFrame">The output frame</param>
		/// <param name="outputStart">The starting position IN SHORTS in the output buffer where the decoded data should be written</param>
		/// <param name="isSilent">Whether the samples contain silence</param>
		/// <returns>The length of the decoded samples IN SHORTS</returns>
		public int Decode(short[] encodedFrame, int inputStart, int inputLength, short[] decodedFrame, int outputStart, bool isSilent)
		{
			if (isSilent)
			{
				Array.Clear(decodedFrame, outputStart, _outputAudioFormat.SamplesPerFrame);
				return _outputAudioFormat.SamplesPerFrame;
			}

			// Decode
			int decodedPos = outputStart;

			if (_outputAudioFormat.SamplesPerSecond == 16000)
			{
				for (int i = inputStart; i < inputStart + inputLength; i++)
				{
					byte b1;
					byte b2;
					G711.FromShort(encodedFrame[i], out b1, out b2);
					decodedFrame[decodedPos] = G711.ULawToLinearFast(b1);
					decodedPos += 2;
					decodedFrame[decodedPos] = G711.ULawToLinearFast(b2);
					decodedPos += 2;
				}

				// Upsample using linear interpolation. Not the best quality, but cheap on CPU.
				for (int i = 1; i < decodedPos - 1; i += 2)
				{
					decodedFrame[i] = (short)((decodedFrame[i - 1] + decodedFrame[i + 1]) / 2);
				}
				decodedFrame[decodedPos - 1] = decodedFrame[decodedPos - 2]; // Cheat on the last sample
			}
			else
			{
				for (int i = inputStart; i < inputStart + inputLength; i++)
				{
					byte b1;
					byte b2;
					G711.FromShort(encodedFrame[i], out b1, out b2);
					decodedFrame[decodedPos++] = G711.ULawToLinearFast(b1);
					decodedFrame[decodedPos++] = G711.ULawToLinearFast(b2);
				}
			}
			return decodedPos - outputStart;
		}

		public AudioCodecType CodecType
		{
			get { return AudioCodecType.G711M; }
		}
	}
}
