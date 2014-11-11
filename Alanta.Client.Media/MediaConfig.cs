
using Alanta.Common;

namespace Alanta.Client.Media
{
	public class MediaConfig
	{
		public string MediaServerHost { get; set; }
		public int MediaServerControlPort { get; set; }
		public int MediaServerStreamingPort { get; set; }
		public ushort LocalSsrcId { get; set; }
		public ICodecFactory CodecFactory { get; set; }

		/// <summary>
		/// How to select the audio context.
		/// </summary>
		public AudioContextSelection AudioContextSelection { get; set; }

		/// <summary>
		/// The expected time between when sound is submitted to be played through the speakers and when it comes back in through the microphone
		/// </summary>
		public int ExpectedAudioLatency { get; set; }

		/// <summary>
		/// The expected latency value to use when the CPU is running too hot
		/// </summary>
		public int ExpectedAudioLatencyFallback { get; set; }

		/// <summary>
		/// The length of the AEC filter in milliseconds
		/// </summary>
		public int FilterLength { get; set; }

		/// <summary>
		/// The length of the AEC filter (in ms) to use when CPU is running too hot.
		/// </summary>
		public int FilterLengthFallback { get; set; }

		/// <summary>
		/// The speech enhancement stack to use (typically Speex or WebRTC)
		/// </summary>
		// public SpeechEnhancementStack SpeechEnhancementStack { get; set; }

		/// <summary>
		/// Whether to enable residual echo suppression in the SpeexPreProcessor.
		/// </summary>
		public bool EnableAec { get; set; }

		/// <summary>
		/// Whether to perform denoising on the recorded audio signal.
		/// </summary>
		public bool EnableDenoise { get; set; }

		/// <summary>
		/// Whether to perform automatic gain control on the recorded audio signal.
		/// </summary>
		public bool EnableAgc { get; set; }

		/// <summary>
		/// Whether to return the echo cancelled sound to the far end or use the original, non-echo cancelled sound.
		/// </summary>
		/// <remarks>
		/// This is mostly for testing purposes. It's a separate setting from CancelEcho because I want the difference to be as stark as possible, which means no spin-up time.
		/// </remarks>
		public bool PlayEchoCancelledSound { get; set; }

		/// <summary>
		/// Whether to apply a volume filter to recorded sound.
		/// </summary>
		public bool ApplyVolumeFilterToRecordedSound { get; set; }

		/// <summary>
		/// Whether to apply a volume filter to played sound.
		/// </summary>
		public bool ApplyVolumeFilterToPlayedSound { get; set; }

		private static MediaConfig defaultConfig;
		public static MediaConfig Default
		{
			get
			{
				return defaultConfig ?? (defaultConfig = new MediaConfig
				{
					ApplyVolumeFilterToPlayedSound = true,
					ApplyVolumeFilterToRecordedSound = true,
					CodecFactory = new CodecFactory(AudioFormat.Default),
					EnableDenoise = true,
					EnableAgc = true,
					EnableAec = true,
					ExpectedAudioLatency = 240,
					FilterLength = 100,
					FilterLengthFallback = 70,
					LocalSsrcId = 0,
                    MediaServerControlPort = MediaConstants.DefaultMediaServerControlPort,
					MediaServerStreamingPort = MediaConstants.DefaultMediaServerStreamingPort,
					PlayEchoCancelledSound = true
				});
			}
		}

	}
	public enum SpeechEnhancementStack
	{
		None,
		Speex,
		WebRtc,
	}
}
