using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Alanta.Client.Media;
using Alanta.Client.Media.Dsp;
using Alanta.Client.UI.Desktop.Controls.AudioVisualizer;

namespace Alanta.Client.Test.Media.Aec
{
	public class RecorderAec : RecorderBase
	{
		public RecorderAec(
			CaptureSource captureSource,
			AudioSinkAdapter audioSinkAdapter,
			AudioVisualizer speakerAudioVisualizer,
			AudioVisualizer cancelledAudioVisualizer,
			EchoCancelFilter echoCancelFilter,
			List<byte[]> cancelledFrames) :
			base(captureSource, audioSinkAdapter, speakerAudioVisualizer)
		{
			mEchoCancelFilter = echoCancelFilter;
			mCancelledAudioVisualizer = cancelledAudioVisualizer;
			CancelledFrames = cancelledFrames;
		}

		protected readonly EchoCancelFilter mEchoCancelFilter;
		private readonly AudioVisualizer mCancelledAudioVisualizer;
		public List<byte[]> CancelledFrames { get; private set; }
		private int mCancelledFrameCount;

		public override void SubmitRecordedFrame(AudioContext ctx, byte[] frame)
		{
			base.SubmitRecordedFrame(ctx, frame);
			EchoCancelFrame(frame);
		}

		protected void EchoCancelFrame(byte[] frame)
		{
			mEchoCancelFilter.Write(frame);
			bool moreFrames;
			do
			{
				var cancelledShorts = new short[frame.Length / sizeof(short)];
				if (mEchoCancelFilter.Read(cancelledShorts, out moreFrames))
				{
					var cancelledBytes = new byte[frame.Length];
					Buffer.BlockCopy(cancelledShorts, 0, cancelledBytes, 0, frame.Length);
					CancelledFrames.Add(cancelledBytes);
					if (mCancelledFrameCount++ % VisualizationRate == 0)
					{
						mCancelledAudioVisualizer.RenderVisualization(cancelledBytes);
					}
				}
			} while (moreFrames);
		}
	}
}
