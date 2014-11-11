using speexDecoder = cspeex.SpeexDecoder;

namespace Alanta.Client.Media.AudioCodecs
{
	public class SpeexDecoder : IAudioDecoder
	{
		public SpeexDecoder(AudioFormat outputFormat)
		{
			_speexDecoder = new speexDecoder();
			_speexDecoder.init(1, outputFormat.SamplesPerSecond, AudioConstants.Channels, false);
		}

		private readonly speexDecoder _speexDecoder;

		public int Decode(byte[] encodedFrame, int inputStart, int inputLength, byte[] decodedFrame, int outputStart, bool isSilent)
		{
			if (encodedFrame == null || encodedFrame.Length == 0)
			{
				_speexDecoder.processData(true);
			}
			else
			{
				_speexDecoder.processData(encodedFrame, inputStart, inputLength);
			}
			return _speexDecoder.getProcessedData(decodedFrame, outputStart);
		}

		public int Decode(short[] encodedFrame, int inputStart, int inputLength, short[] decodedFrame, int outputStart, bool isSilent)
		{
			if (encodedFrame == null || encodedFrame.Length == 0)
			{
				_speexDecoder.processData(true);
			}
			else
			{
				_speexDecoder.processData(encodedFrame, inputStart, inputLength);
			}
			return _speexDecoder.getProcessedData(decodedFrame, outputStart);
		}

		public AudioCodecType CodecType { get { return AudioCodecType.Speex; } }

	}
}
