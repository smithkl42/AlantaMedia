using System.Collections.Generic;
using System.Windows.Media;
using Alanta.Client.Media;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media.Mac
{
	public class FromFileAudioSinkAdapter : AudioSinkAdapter
	{
		public FromFileAudioSinkAdapter(
			CaptureSource captureSource, 
			IAudioController audioController, 
			MediaConfig mediaConfig, 
			IMediaEnvironment mediaEnvironment, 
			AudioFormat playedAudioFormat, 
			List<byte[]> testFrames)
			:base(captureSource, audioController, mediaConfig, mediaEnvironment, playedAudioFormat)
		{
			_testFrames = testFrames;
		}

		private readonly List<byte[]> _testFrames;
		private int _frameIndex;

		protected override void SubmitFrame(AudioContext audioContext, byte[] frame)
		{
			if (++_frameIndex >= _testFrames.Count)
			{
				_frameIndex = 0;
			}
			base.SubmitFrame(audioContext, _testFrames[_frameIndex]);
		}
	}
}
