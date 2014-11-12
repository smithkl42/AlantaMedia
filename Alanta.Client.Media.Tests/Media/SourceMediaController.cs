using System;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media
{
	public class SourceMediaController: MediaController
	{
		public SourceMediaController(MediaConfig mediaConfig, MediaStatistics mediaStatistics, MediaEnvironment mediaEnvironment, IMediaConnection mediaConnection, IVideoQualityController videoQualityController) :
			base(mediaConfig, AudioFormat.Default, mediaStatistics, mediaEnvironment, mediaConnection, videoQualityController)
		{
		}

		protected override void ApplyAudioInputFilter(short[] frame)
		{
			RenderVisualization(frame, InputAudioVisualizer);
		}

		private void RenderVisualization(short[] frame, AudioVisualizer audioVisualizer)
		{
			if (audioVisualizer != null)
			{
				var samples = new short[frame.Length / sizeof(short)];
				Buffer.BlockCopy(frame, 0, samples, 0, frame.Length);
				audioVisualizer.RenderVisualization(samples);
			}
		}

		public AudioVisualizer InputAudioVisualizer { get; set; }

	}
}
