using System;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Media.Dsp.WebRtc;

namespace Alanta.Client.Media
{
	public class AudioContextFactory : IAudioContextFactory
	{
		/// <summary>
		/// Creates a new instance of the AudioContextFactory.
		/// </summary>
		/// <param name="rawAudioFormat">The format in which the audio coming directly from the microphone is recorded</param>
		/// <param name="playedAudioFormat">The format in which the audio will be played back on the far end (typically 16Khz)</param>
		/// <param name="config">The currently active MediaConfig instance</param>
		/// <param name="mediaEnvironment">An IMediaEnvironment instance which can be used to make decisions about which context to return, for instance,
		/// if the CPU is running too hot, or multiple people have joined the conference.</param>
		public AudioContextFactory(AudioFormat rawAudioFormat, AudioFormat playedAudioFormat, MediaConfig config, IMediaEnvironment mediaEnvironment)
		{
			RawAudioFormat = rawAudioFormat;
			PlayedAudioFormat = playedAudioFormat;
			MediaConfig = config;
			MediaEnvironment = mediaEnvironment;

			// What we should use when there's only one other person, and CPU is OK: 
			// 16Khz, Speex, WebRtc at full strength
			var directAudioFormat = new AudioFormat();
			var directResampler = new ResampleFilter(rawAudioFormat, directAudioFormat);
			directResampler.InstanceName = "High Quality Direct Resampler";
			var directEnhancer = new WebRtcFilter(config.ExpectedAudioLatency, config.FilterLength, directAudioFormat, playedAudioFormat, config.EnableAec, config.EnableDenoise, config.EnableAgc);
			directEnhancer.InstanceName = "High";
			var directDtx = new DtxFilter(directAudioFormat);
			var directEncoder = new SpeexEncoder(directAudioFormat);
			HighQualityDirectCtx = new AudioContext(directAudioFormat, directResampler, directDtx, directEnhancer, directEncoder);
			HighQualityDirectCtx.Description = "High Quality Direct";

			// What we should use when there are multiple people (and hence the audio will need to be decoded and mixed), but CPU is OK:
			// 8Khz, G711, WebRtc at full strength
			var conferenceAudioFormat = new AudioFormat(AudioConstants.NarrowbandSamplesPerSecond);
			var conferenceResampler = new ResampleFilter(rawAudioFormat, conferenceAudioFormat);
			conferenceResampler.InstanceName = "High Quality Conference Resampler";
			var conferenceEnhancer = new WebRtcFilter(config.ExpectedAudioLatency, config.FilterLength, conferenceAudioFormat, playedAudioFormat, config.EnableAec, config.EnableDenoise, config.EnableAgc);
			conferenceEnhancer.InstanceName = "Medium";
			var conferenceDtx = new DtxFilter(conferenceAudioFormat);
			var conferenceEncoder = new G711MuLawEncoder(conferenceAudioFormat);
			HighQualityConferenceCtx = new AudioContext(conferenceAudioFormat, conferenceResampler, conferenceDtx, conferenceEnhancer, conferenceEncoder);
			HighQualityConferenceCtx.Description = "High Quality Conference";

			// What we should use when one or more remote CPU's isn't keeping up (regardless of how many people are in the room):
			// 8Khz, G711, WebRtc at full-strength
			var remoteFallbackAudioFormat = new AudioFormat(AudioConstants.NarrowbandSamplesPerSecond);
			var remoteFallbackResampler = new ResampleFilter(rawAudioFormat, remoteFallbackAudioFormat);
			remoteFallbackResampler.InstanceName = "Low Quality Remote CPU Resampler";
			var remoteFallbackEnhancer = new WebRtcFilter(config.ExpectedAudioLatency, config.FilterLength, remoteFallbackAudioFormat, playedAudioFormat, config.EnableAec, config.EnableDenoise, config.EnableAgc);
			remoteFallbackEnhancer.InstanceName = "Medium";
			var remoteFallbackDtx = new DtxFilter(remoteFallbackAudioFormat);
			var remoteFallbackEncoder = new G711MuLawEncoder(remoteFallbackAudioFormat);
			LowQualityForRemoteCpuCtx = new AudioContext(remoteFallbackAudioFormat, remoteFallbackResampler, remoteFallbackDtx, remoteFallbackEnhancer, remoteFallbackEncoder);
			LowQualityForRemoteCpuCtx.Description = "Fallback for remote high CPU";

			// What we should use when the local CPU isn't keeping up (regardless of how many people are in the room):
			// 8Khz, G711, WebRtc at half-strength
			var fallbackAudioFormat = new AudioFormat(AudioConstants.NarrowbandSamplesPerSecond);
			var fallbackResampler = new ResampleFilter(rawAudioFormat, fallbackAudioFormat);
			fallbackResampler.InstanceName = "Low Quality Local CPU Resampler";
			var fallbackEnhancer = new WebRtcFilter(config.ExpectedAudioLatencyFallback, config.FilterLengthFallback, fallbackAudioFormat, playedAudioFormat, config.EnableAec, false, false);
			fallbackEnhancer.InstanceName = "Low";
			var fallbackDtx = new DtxFilter(fallbackAudioFormat);
			var fallbackEncoder = new G711MuLawEncoder(fallbackAudioFormat);
			LowQualityForLocalCpuCtx = new AudioContext(fallbackAudioFormat, fallbackResampler, fallbackDtx, fallbackEnhancer, fallbackEncoder);
			LowQualityForLocalCpuCtx.Description = "Fallback for local high CPU";

			_audioContextAdapter = new EnvironmentAdapter<AudioContext>(mediaEnvironment,
				HighQualityDirectCtx,
				HighQualityConferenceCtx,
				LowQualityForRemoteCpuCtx,
				LowQualityForLocalCpuCtx);
		}

		public AudioFormat RawAudioFormat { get; private set; }
		public AudioFormat PlayedAudioFormat { get; private set; }
		public MediaConfig MediaConfig { get; private set; }
		public IMediaEnvironment MediaEnvironment { get; private set; }

		private readonly EnvironmentAdapter<AudioContext> _audioContextAdapter;

		public AudioContext HighQualityDirectCtx { get; private set; }
		public AudioContext HighQualityConferenceCtx { get; private set; }
		public AudioContext LowQualityForRemoteCpuCtx { get; private set; }
		public AudioContext LowQualityForLocalCpuCtx { get; private set; }

		public AudioContext GetAudioContext()
		{
			switch (MediaConfig.AudioContextSelection)
			{
				case AudioContextSelection.Automatic:
					return _audioContextAdapter.GetItem();
				case AudioContextSelection.HighQualityDirect:
					return HighQualityDirectCtx;
				case AudioContextSelection.HighQualityConference:
					return HighQualityConferenceCtx;
				case AudioContextSelection.FallbackForRemoteHighCpu:
					return LowQualityForLocalCpuCtx;
				case AudioContextSelection.FallbackForLocalHighCpu:
					return LowQualityForRemoteCpuCtx;
			}
			throw new InvalidOperationException("No suitable audio context was found");
		}
	}

	public enum AudioContextSelection
	{
		Automatic,
		HighQualityDirect,
		HighQualityConference,
		FallbackForRemoteHighCpu,
		FallbackForLocalHighCpu
	}
}
