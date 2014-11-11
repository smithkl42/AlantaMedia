using System.Collections.Generic;
using System.Windows.Controls;
using Alanta.Client.Media;
using Alanta.Client.UI.Desktop.Controls.AudioVisualizer;

namespace Alanta.Client.Test.Media.Aec
{
	public class TestRunnerLive:TestRunnerBase
	{
		public TestRunnerLive(EchoCancellerType echoCancellerType, 
			AudioVisualizer sourceAudioVisualizer, 
			AudioVisualizer speakersAudioVisualizer, 
			AudioVisualizer cancelledAudioVisualizer)
			: base(echoCancellerType, sourceAudioVisualizer, speakersAudioVisualizer, cancelledAudioVisualizer)
		{
			SpeakerFrames = new List<byte[]>();
		}

		protected override PlayerAec GetPlayer()
		{
			var mediaElement = new MediaElement();
			var audioStreamSource = new AudioMediaStreamSource(null, AudioFormat.Default);
			mediaElement.SetSource(audioStreamSource);
			var player = new PlayerAec(mediaElement, audioStreamSource, mSourceAudioVisualizer, mEchoCancelFilter);
			audioStreamSource.AudioController = player;
			audioStreamSource.InstanceName = "ForTestRunnerLive";
			return player;
		}

		protected override RecorderAec GetRecorder()
		{
			var recorder = new RecorderAec(CaptureSource, AudioSinkAdapter, mSpeakersAudioVisualizer, mCancelledAudioVisualizer, mEchoCancelFilter, mCancelledFrames);
			AudioSinkAdapter.InstanceName = "ForTestRunnerLive";
			return recorder;
		}

	}

}
