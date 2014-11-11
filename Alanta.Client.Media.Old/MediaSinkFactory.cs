using System.Windows.Media;

namespace Alanta.Client.Media
{
	public class MediaSinkFactory : IMediaSinkFactory
	{
		public MediaSinkFactory(IAudioController audioController, 
			IVideoController videoController, 
			MediaConfig mediaConfig, 
			IMediaEnvironment mediaEnvironment, 
			IVideoQualityController videoQualityController)
		{
			_audioController = audioController;
			_videoController = videoController;
			_mediaConfig = mediaConfig;
			_mediaEnvironment = mediaEnvironment;
			_videoQualityController = videoQualityController;
		}

		private readonly IAudioController _audioController;
		private readonly IVideoController _videoController;
		private readonly MediaConfig _mediaConfig;
		private readonly IMediaEnvironment _mediaEnvironment;
		private readonly IVideoQualityController _videoQualityController;

		public AudioSinkAdapter GetAudioSink(CaptureSource captureSource)
		{
			return new AudioSinkAdapter(captureSource, _audioController, _mediaConfig, _mediaEnvironment, AudioFormat.Default);
		}

		public VideoSinkAdapter GetVideoSink(CaptureSource captureSource)
		{
			return new VideoSinkAdapter(captureSource, _videoController, _videoQualityController);
		}

	}
}
