using Alanta.Client.Media;
using Alanta.Client.Media.Dsp;

namespace Alanta.Client.Test.Media
{
	public class SingleAudioContextFactory : IAudioContextFactory
	{
		public SingleAudioContextFactory(AudioContext audioContext, AudioFormat rawAudioFormat, AudioFormat playedAudioFormat, MediaConfig mediaConfig, IMediaEnvironment mediaEnvironment)
		{
			RawAudioFormat = rawAudioFormat;
			PlayedAudioFormat = playedAudioFormat;
			MediaConfig = mediaConfig;
			MediaEnvironment = mediaEnvironment;

			// Hack!!!! We need to make a copy of the audioContext, but with a few tweaks.
			// When the audio context is first created, we don't know what the rawAudioFormat will be,
			// but it should be accurate by this point, so we need to recreate the AudioContext.
			var resampler = new ResampleFilter(rawAudioFormat, playedAudioFormat);
			resampler.InstanceName = audioContext.Resampler.InstanceName;
			_audioContext = new AudioContext(playedAudioFormat, resampler, audioContext.DtxFilter, audioContext.SpeechEnhancementStack, audioContext.Encoder);
			_audioContext.Description = audioContext.Description;
		}

		public AudioFormat RawAudioFormat { get;  set; }
		public AudioFormat PlayedAudioFormat { get;  set; }
		public MediaConfig MediaConfig { get;  set; }
		public IMediaEnvironment MediaEnvironment { get;  set; }

		private readonly AudioContext _audioContext;
		public AudioContext GetAudioContext()
		{
			return _audioContext;
		}
	}

}
