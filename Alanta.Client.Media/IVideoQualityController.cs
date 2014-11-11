using System;
using System.Collections.Generic;

namespace Alanta.Client.Media
{
	public interface IVideoQualityController
	{

		event EventHandler VideoQualityChanged;

		/// <summary>
		/// The number of video frames accepted per second. Higher value = higher quality.
		/// </summary>
		int AcceptFramesPerSecond { get; }

		/// <summary>
		/// The number of blocks to skip during every normal frame. Lower value = higher quality.
		/// </summary>
		int InterleaveFactor { get; }

		/// <summary>
		/// The number of predictive frames to be sent in-between i-frames. Lower value = higher quality.
		/// </summary>
		int FullFrameInterval { get; }

		/// <summary>
		/// The maximum difference between video blocks before the block needs to be retransmitted. Lower value = higher quality.
		/// </summary>
		int NoSendCutoff { get; }

		/// <summary>
		/// The maximum difference between video blocks before the block needs to be retransmitted. Lower value = higher quality.
		/// </summary>
		int DeltaSendCutoff { get; }

		/// <summary>
		/// The quality of the compressed JPEG image to send
		/// </summary>
		byte JpegQuality { get; }

		/// <summary>
		/// The video quality commanded by a remote machine
		/// </summary>
		VideoQuality CommandedVideoQuality { get; }

		/// <summary>
		/// The current quality of the video.
		/// </summary>
		VideoQuality ProposedVideoQuality { get; }

		/// <summary>
		/// Log the quality of a received video frame so that we can make some present and future decisions about the quality of the video *we're* sending.
		/// </summary>
		/// <param name="remoteSsrcId">The SsrcId of the remote client</param>
		/// <param name="remoteCommandedVideoQuality">The video quality at which the remote client is actually sending video</param>
		/// <param name="remoteProposedVideoQuality">The video quality at which the remote client thinks it could send send video</param>
		void LogReceivedVideoQuality(ushort remoteSsrcId, VideoQuality remoteCommandedVideoQuality, VideoQuality remoteProposedVideoQuality);

		/// <summary>
		/// Log any network glitch, so that we can request a general reduction in quality if necessary.
		/// </summary>
		/// <param name="points">How bad the glitch is</param>
		void LogGlitch(int points);

		/// <summary>
		/// A list of remote sessions that can be used to determine the 
		/// </summary>
		Dictionary<ushort, VideoThreadData> RemoteSessions { get; set; }


	}
}