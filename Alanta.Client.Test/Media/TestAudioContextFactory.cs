using Alanta.Client.Media;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Media.Dsp.Speex;
using Alanta.Client.Media.Dsp.WebRtc;

namespace Alanta.Client.Test.Media
{
	public class TestAudioContextFactory
	{
		public TestAudioContextFactory(MediaConfig mediaConfig, AudioFormat rawAudioFormat, AudioFormat transmittedAudioFormat, SpeechEnhancementStack enhancementStack, AudioCodecType codecType)
		{
			this.mediaConfig = mediaConfig;
			this.rawAudioFormat = rawAudioFormat;
			this.transmittedAudioFormat = transmittedAudioFormat;
			this.enhancementStack = enhancementStack;
			this.codecType = codecType;
		}

		private readonly MediaConfig mediaConfig;
		private readonly AudioFormat rawAudioFormat;
		private readonly AudioFormat transmittedAudioFormat;
		private readonly SpeechEnhancementStack enhancementStack;
		private readonly AudioCodecType codecType;

		public AudioContext GetAudioContext()
		{
			var resampler = new ResampleFilter(rawAudioFormat, transmittedAudioFormat);
			var conferenceDtx = new DtxFilter(transmittedAudioFormat);

			IAudioTwoWayFilter enhancer = null;
			switch (enhancementStack)
			{
				case SpeechEnhancementStack.None:
					enhancer = new NullEchoCancelFilter(mediaConfig.ExpectedAudioLatency, mediaConfig.FilterLength, transmittedAudioFormat, AudioFormat.Default);
					break;
				case SpeechEnhancementStack.Speex:
					enhancer = new SpeexEchoCanceller2(mediaConfig, transmittedAudioFormat, AudioFormat.Default);
					break;
				case SpeechEnhancementStack.WebRtc:
					enhancer = new WebRtcFilter(mediaConfig.ExpectedAudioLatency, mediaConfig.FilterLength, transmittedAudioFormat, AudioFormat.Default, mediaConfig.EnableAec, mediaConfig.EnableDenoise, mediaConfig.EnableAgc);
					break;
			}

			IAudioEncoder encoder = null;
			switch (codecType)
			{
				case AudioCodecType.G711M:
					encoder = new G711MuLawEncoder(transmittedAudioFormat);
					break;
				case AudioCodecType.Speex:
					encoder = new SpeexEncoder(transmittedAudioFormat);
					break;
			}

			var ctx = new AudioContext(transmittedAudioFormat, resampler, conferenceDtx, enhancer, encoder);
			return ctx;
		}

	}


}
