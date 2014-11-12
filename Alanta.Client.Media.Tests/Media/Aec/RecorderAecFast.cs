using System;
using System.Collections.Generic;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Test.Media.Aec;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media.Aec
{
	public class RecorderAecFast : RecorderAec
	{

		public RecorderAecFast(AudioVisualizer speakerAudioVisualizer, AudioVisualizer cancelledAudioVisualizer, EchoCancelFilter echoCancelFilter, List<byte[]> cancelledFrames) :
			base(null, null, speakerAudioVisualizer, cancelledAudioVisualizer, echoCancelFilter, cancelledFrames)
		{
		}

		public override void StartRecording(List<byte[]> recordedFrames, Action onRecordingStoppedCallback = null)
		{
			if (IsConnected)
			{
				throw new InvalidOperationException("Already recording");
			}
			IsConnected = true;
			RecordedFrames = recordedFrames;
			mOnRecordingStoppedCallback = onRecordingStoppedCallback;
		}

		public override void SubmitRecordedFrame(AudioContext ctx, byte[] frame)
		{
			if (mRecordedFrameCount++ % VisualizationRate == 0)
			{
				mAudioVisualizer.RenderVisualization(frame);
			}
			EchoCancelFrame(frame);
		}
	}
}
