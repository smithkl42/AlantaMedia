using System;
using System.IO;
using System.Windows.Controls;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Test.Media.Aec;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media.Aec
{
	public class PlayerAec : PlayerBase
	{
		public PlayerAec(MediaElement mediaElement, AudioMediaStreamSource audioMediaStreamSource, AudioVisualizer audioVisualizer, EchoCancelFilter echoCancelFilter) :
			base(mediaElement, audioMediaStreamSource, audioVisualizer)
		{
			mEchoCancelFilter = echoCancelFilter;
		}

		private readonly EchoCancelFilter mEchoCancelFilter;

		public override void GetNextAudioFrame(Action<MemoryStream> callback)
		{
			base.GetNextAudioFrame(ms =>
			{
				if (Frames != null && Frames.Count > mFrameIndex)
				{
					mEchoCancelFilter.RegisterFramePlayed(Frames[mFrameIndex - 1]); // -1 because base.GetNextAudioFrame() increments the counter after retrieving the frame.
				}
				callback(ms);
			});
		}

	}
}
