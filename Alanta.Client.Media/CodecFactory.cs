using System.Collections.Generic;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.VideoCodecs;

namespace Alanta.Client.Media
{
	public class CodecFactory : ICodecFactory
	{
		#region Fields and Properties
		private readonly Dictionary<AudioCodecType, IAudioDecoder> decoders = new Dictionary<AudioCodecType, IAudioDecoder>();
		// private readonly SpeexAudioEncoder speexAudioCodec = new SpeexAudioEncoder();
		// private readonly G711MuLawEncoder g711MuLawCodec = new G711MuLawEncoder();
		// private readonly EnvironmentAdapter<IAudioEncoder> environmentAdapter;

		#endregion

		#region Constructors

		public CodecFactory(AudioFormat playedAudioFormat)
		{
			decoders[AudioCodecType.Raw] = new NullAudioDecoder(playedAudioFormat);
			decoders[AudioCodecType.Speex] = new SpeexDecoder(playedAudioFormat);
			decoders[AudioCodecType.G711M] = new G711MuLawDecoder(playedAudioFormat);
			// environmentAdapter = new EnvironmentAdapter<IAudioEncoder>(mediaEnvironment, speexAudioCodec, g711MuLawCodec);
		}
		#endregion

		#region Methods

		public IAudioDecoder GetAudioDecoder(AudioCodecType codecTypeType, MediaStatistics mediaStatistics = null)
		{
			return decoders[codecTypeType];
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
		#endregion
	}
}
