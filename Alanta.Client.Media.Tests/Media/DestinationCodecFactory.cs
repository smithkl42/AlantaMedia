using Alanta.Client.Media;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.VideoCodecs;

namespace Alanta.Client.Test.Media
{
	public class DestinationCodecFactory : ICodecFactory
	{

		public IAudioDecoder GetAudioDecoder(AudioCodecType codecTypeType, MediaStatistics mediaStatistics = null)
		{
			var audioFormat = new AudioFormat();
			return new SpeexDecoder(audioFormat);
		}

		public IVideoCodec GetVideoEncoder(IVideoQualityController videoQualityController, MediaStatistics mediaStatistics = null)
		{
			var videoCodec = new JpegDiffVideoCodec(videoQualityController, mediaStatistics);
			videoCodec.Initialize(VideoConstants.Height, VideoConstants.Width, VideoConstants.MaxPayloadSize);
			return videoCodec;
		}

		public IVideoCodec GetVideoDecoder(IVideoQualityController videoQualityController, MediaStatistics mediaStatistics = null)
		{
			var videoCodec = new JpegDiffVideoCodec(videoQualityController, mediaStatistics);
			videoCodec.Initialize(VideoConstants.Height, VideoConstants.Width, VideoConstants.MaxPayloadSize);
			return videoCodec;
		}
	}
}
