using System;
using System.Threading;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media.Timing
{
	public class TimingAudioSinkAdapter : AudioSinkAdapter
	{
		public TimingAudioSinkAdapter(
			AudioContext audioContext, 
			CaptureSource captureSource, 
			IAudioController audioController, 
			MediaConfig mediaConfig, 
			IMediaEnvironment mediaEnvironment, 
			AudioFormat playedAudioFormat)
			: base(captureSource, audioController, mediaConfig, mediaEnvironment, playedAudioFormat)
		{
			_audioContext = audioContext;
			ClientLogger.Debug(GetType().Name + " created.");
		}

		private readonly AudioContext _audioContext;

		/// <summary>
		/// This overrides the normal audio context retrieval process. The TimingAudioContextFactory returns only the specified audio context, so that we can test it.
		/// </summary>
		protected override IAudioContextFactory GetAudioContextFactory(AudioFormat rawAudioFormat, AudioFormat playedAudioFormat, MediaConfig config, IMediaEnvironment mediaEnvironment)
		{
			ClientLogger.Debug("TimingAudioContextFactory created.");
			return new SingleAudioContextFactory(_audioContext, rawAudioFormat, playedAudioFormat, config, mediaEnvironment);
		}

		private DateTime _firstFrameAt = DateTime.MinValue;
		private long _totalFrames;

		protected override void SubmitFrame(AudioContext audioContext, byte[] frame)
		{
			base.SubmitFrame(audioContext, frame);
			if (_firstFrameAt == DateTime.MinValue)
			{
				_firstFrameAt = DateTime.Now;
			}
			if (++_totalFrames % 400 == 0)
			{
				double totalMs = (DateTime.Now - _firstFrameAt).TotalMilliseconds;
				double averageMs = totalMs/_totalFrames;
				ClientLogger.Debug("AverageSubmissionTime={0}", averageMs);
			}
		}
	}
}
