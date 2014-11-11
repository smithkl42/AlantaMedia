using System.Windows.Media;

namespace Alanta.Client.Media
{
	public interface IMediaSinkFactory
	{
		AudioSinkAdapter GetAudioSink(CaptureSource captureSource);
		VideoSinkAdapter GetVideoSink(CaptureSource captureSource);
	}
}
