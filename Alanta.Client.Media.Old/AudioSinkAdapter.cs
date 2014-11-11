using System;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using wmAudioFormat = System.Windows.Media.AudioFormat;

namespace Alanta.Client.Media
{
	public class AudioSinkAdapter : AudioSink
	{
		#region Constructors

		public AudioSinkAdapter(CaptureSource captureSource, IAudioController audioController, MediaConfig mediaConfig, IMediaEnvironment mediaEnvironment, AudioFormat playedAudioFormat)
		{
			ClientLogger.Debug(GetType().Name + " created.");
			CaptureSource = captureSource;
			AudioController = audioController;
			_mediaConfig = mediaConfig;
			_mediaEnvironment = mediaEnvironment;
			_playedAudioFormat = playedAudioFormat;
			_logger = new AudioSinkAdapterLogger();
		}

		#endregion

		#region Fields and Properties
		// protected wm.AudioFormat wmAudioFormat;
		private AudioSinkAdapterLogger _logger;
		protected ushort _outputBytesPerSample = (AudioConstants.BitsPerSample * AudioConstants.Channels) / 8;
		public IAudioController AudioController { get; set; }
		protected bool _dataReceived;
		public event EventHandler CaptureSuccessful;
		protected IAudioContextFactory _audioContextFactory;
		protected MediaConfig _mediaConfig;
		public string InstanceName { get; set; }
		private readonly IMediaEnvironment _mediaEnvironment;
		private readonly AudioFormat _playedAudioFormat;
		#endregion

		protected override void OnCaptureStarted()
		{
			// Open an RTP stream.
			ClientLogger.Debug("AudioSinkAdapter: capture started.");
		}

		protected override void OnCaptureStopped()
		{
			ClientLogger.Debug("Audio capture stopped.");
		}

		protected override void OnFormatChange(wmAudioFormat audioFormat)
		{
			// We may need to do more with this.
			if (audioFormat == null ||
				audioFormat.WaveFormat != WaveFormatType.Pcm ||
				audioFormat.BitsPerSample != AudioConstants.BitsPerSample)
			{
				throw new ArgumentException("The audio format was not supported.");
			}

			// Only change the audio context factory if the raw audio format has actually changed.
			var rawAudioFormat = new AudioFormat(audioFormat.SamplesPerSecond, AudioConstants.MillisecondsPerFrame, audioFormat.Channels, audioFormat.BitsPerSample);
			if (_audioContextFactory == null || _audioContextFactory.RawAudioFormat != rawAudioFormat)
			{
				_audioContextFactory = GetAudioContextFactory(rawAudioFormat, _playedAudioFormat, _mediaConfig, _mediaEnvironment);
				_logger.RawAudioFormat = rawAudioFormat;
				ClientLogger.Debug("The recorded audio format was changed: BitsPerSample = {0}, Channels = {1}, SamplesPerSecond = {2}",
					rawAudioFormat.BitsPerSample, rawAudioFormat.Channels, rawAudioFormat.SamplesPerSecond);
			}
		}

		protected virtual IAudioContextFactory GetAudioContextFactory(AudioFormat rawAudioFormat, AudioFormat playedAudioFormat, MediaConfig config, IMediaEnvironment mediaEnvironment)
		{
			return new AudioContextFactory(rawAudioFormat, playedAudioFormat, config, mediaEnvironment);
		}

		private AudioContext _lastAudioContext;

		protected override void OnSamples(long sampleTime, long sampleDuration, byte[] sampleData)
		{
			try
			{
				// Raise an event if we managed to successfully capture real audio.
				if (!_dataReceived && HasRealAudio(sampleData))
				{
					ClientLogger.Debug("The AudioSinkAdapter has detected that there's real audio coming in.");
					_dataReceived = true;
					if (CaptureSuccessful != null)
					{
						CaptureSuccessful(this, new EventArgs());
					}
				}

				if (_audioContextFactory != null && AudioController != null && AudioController.IsConnected)
				{
					var ctx = _audioContextFactory.GetAudioContext();
					if (ctx != _lastAudioContext)
					{
						ClientLogger.Debug("Changed audio context: \r\nFrom: {0}\r\nTo: {1}", _lastAudioContext == null ? "" : _lastAudioContext.ToString(), ctx.ToString());
						_lastAudioContext = ctx;
					}
					ctx.Resampler.Write(sampleData);
					_logger.LogRawFrame(sampleTime, sampleDuration, sampleData);

					bool moreFrames;
					do
					{
						if (ctx.Resampler.Read(ctx.ResampleBuffer, out moreFrames))
						{
							SubmitFrame(ctx, ctx.ResampleBuffer);
						}
					} while (moreFrames);
				}
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.Message);
			}
		}

		private static bool HasRealAudio(byte[] sampleData)
		{
			for (int i = 0; i < sampleData.Length; i++)
			{
				var sample = (short)(sampleData[i] | sampleData[++i] << 8);
				if (sample != 0)
				{
					return true;
				}
			}
			return false;
		}

		protected virtual void SubmitFrame(AudioContext audioContext, byte[] frame)
		{
			AudioController.SubmitRecordedFrame(audioContext, frame);
			_logger.AudioFormat = audioContext.AudioFormat;
			_logger.LogResampledFrame(frame);
		}

	}
}
