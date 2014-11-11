using System;
using System.Collections.Generic;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Video
{
	public class TestVideoQualityController : IVideoQualityController
	{

		public TestVideoQualityController()
		{
			AcceptFramesPerSecond = 5;
			InterleaveFactor = 1;
			FullFrameInterval = AcceptFramesPerSecond * 4;
			NoSendCutoff = 100; // 256 * 100;
			JpegQuality = 40;
			CommandedVideoQuality = VideoQuality.Medium;
			ProposedVideoQuality = VideoQuality.Medium;
		}

		public event EventHandler VideoQualityChanged;

		public void OnVideoQualityChanged(EventArgs e)
		{
			EventHandler handler = VideoQualityChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		public int AcceptFramesPerSecond { get; set; }

		public int InterleaveFactor { get; set; }

		public int FullFrameInterval { get; set; }

		public int NoSendCutoff { get; set; }

		public int DeltaSendCutoff { get; set; }

		public byte JpegQuality { get; set; }

		public VideoQuality CommandedVideoQuality { get; set; }

		public VideoQuality ProposedVideoQuality { get; set; }

		public void LogReceivedVideoQuality(ushort remoteSsrcId, VideoQuality remoteCommandedVideoQuality, VideoQuality remoteProposedVideoQuality)
		{
			// No-op
		}

		public void LogGlitch(int points)
		{
			// No-op
		}

		public Dictionary<ushort, VideoThreadData> RemoteSessions { get; set; }
	}
}
