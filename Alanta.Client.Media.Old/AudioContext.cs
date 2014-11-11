using System.Text;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;

namespace Alanta.Client.Media
{
	public class AudioContext
	{
		public AudioContext(AudioFormat audioFormat, IAudioFilter resampler, IDtxFilter dtxFilter, IAudioTwoWayFilter speechEnhancer, IAudioEncoder encoder)
		{
			AudioFormat = audioFormat;
			Resampler = resampler;
			SpeechEnhancementStack = speechEnhancer;
			Encoder = encoder;
			DtxFilter = dtxFilter;
			ResampleBuffer = new byte[audioFormat.BytesPerFrame];
			CancelBuffer = new short[audioFormat.SamplesPerFrame];
			EncodeBuffer = new short[audioFormat.SamplesPerFrame];
			SendBuffer = new short[audioFormat.SamplesPerFrame];
		}

		public string Description { get; set; }
		public AudioFormat AudioFormat { get; private set; }
		public IAudioFilter Resampler { get; private set; }
		public IAudioTwoWayFilter SpeechEnhancementStack { get; private set; }
		public IAudioEncoder Encoder { get; private set; }
		public IDtxFilter DtxFilter { get; private set; }

		/// <summary>
		/// A buffer used when resampling data.
		/// </summary>
		public byte[] ResampleBuffer { get; private set; }

		/// <summary>
		/// A buffer used when canceling acoustic echo.
		/// </summary>
		public short[] CancelBuffer { get; private set; }

		/// <summary>
		/// A buffer used when encoding the audio.
		/// </summary>
		public short[] EncodeBuffer { get; private set; }

		/// <summary>
		/// A buffer used when sending the encoded audio data.
		/// </summary>
		public short[] SendBuffer { get; private set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine("AudioContext " + Description + "::");
			sb.AppendLine(" - AudioFormat: " + AudioFormat);
			sb.AppendLine(" - Resampler: " + Resampler);
			sb.AppendLine(" - SpeechEnhancementStack: " + SpeechEnhancementStack);
			sb.AppendLine(" - Encoder: " + Encoder);
			sb.AppendLine(" - DtxFilter: " + DtxFilter);
			return sb.ToString();
		}
	}
}
