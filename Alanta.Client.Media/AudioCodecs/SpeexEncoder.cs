using System;
using speexEncoder = cspeex.SpeexEncoder;

namespace Alanta.Client.Media.AudioCodecs
{
	public class SpeexEncoder : IAudioEncoder
	{
		/// <summary>
		/// Initializes a speex encoder.
		/// </summary>
		/// <param name="inputFormat">The format in which the samples will be arriving.</param>
		public SpeexEncoder(AudioFormat inputFormat)
		{
			_inputAudioFormat = inputFormat;
			_speexEncoder = new speexEncoder();
			int speexMode;
			switch (inputFormat.SamplesPerSecond)
			{
				case AudioConstants.NarrowbandSamplesPerSecond:
					speexMode = 0;
					break;
				case AudioConstants.WidebandSamplesPerSecond:
					speexMode = 1;
					break;
				case AudioConstants.UltrawidebandSamplesPerSecond:
					speexMode = 2;
					break;
				default:
					throw new InvalidOperationException("The frequency is not supported");
			}
			_speexEncoder.init(speexMode, AudioConstants.SpeexQuality, inputFormat.SamplesPerSecond, AudioConstants.Channels);
			_speexEncoder.getEncoder().setComplexity(AudioConstants.SpeexComplexity);
			_speexEncoder.getEncoder().setVbrQuality(AudioConstants.SpeexQuality);
			_speexEncoder.getEncoder().setVbr(true);
			_speexEncoder.getEncoder().setVad(true);
			_speexEncoder.getEncoder().setDtx(true);
		}

		private readonly AudioFormat _inputAudioFormat;
		private readonly speexEncoder _speexEncoder;

		public override string ToString()
		{
			return "SpeexEncoder; InputAudioFormat:" + _inputAudioFormat;
		}

		public int Encode(byte[] rawFrame, int start, int length, byte[] encodedFrame, bool isSilent)
		{
			_speexEncoder.processData(rawFrame, start, length);
			return _speexEncoder.getProcessedData(encodedFrame, 0);
		}

		public int Encode(short[] rawFrame, int start, int length, short[] encodedFrame, bool isSilent)
		{
			_speexEncoder.processData(rawFrame, start, length);
			return _speexEncoder.getProcessedData(encodedFrame, 0);
		}

		public AudioCodecType CodecType { get { return AudioCodecType.Speex; } }
	}
}
