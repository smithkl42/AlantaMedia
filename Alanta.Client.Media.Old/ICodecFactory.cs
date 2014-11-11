using Alanta.Client.Media.VideoCodecs;
using Alanta.Client.Media.AudioCodecs;
namespace Alanta.Client.Media
{
	public interface ICodecFactory
	{
		// IAudioEncoder GetAudioEncoder(int remoteSessions, MediaStatistics mediaStatistics = null);
		IAudioDecoder GetAudioDecoder(AudioCodecType codecTypeType, MediaStatistics mediaStatistics = null);
		IVideoCodec GetVideoEncoder(IVideoQualityController videoQualityController, MediaStatistics mediaStatistics = null);
		IVideoCodec GetVideoDecoder(IVideoQualityController videoQualityController, MediaStatistics mediaStatistics = null);
	}
}
