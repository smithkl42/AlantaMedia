using System;
using System.Threading;
using Alanta.Client.Common.Logging;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media
{
	public class DestinationMediaController : MediaController
	{

		// ReSharper disable NotAccessedField.Local
		Timer _audioTimer;
		// ReSharper restore NotAccessedField.Local

		public DestinationMediaController(MediaConfig mediaConfig, MediaStatistics mediaStatistics, MediaEnvironment mediaEnvironment, IMediaConnection mediaConnection, IVideoQualityController videoQualityController) :
			base(mediaConfig, AudioFormat.Default, mediaStatistics, mediaEnvironment, mediaConnection, videoQualityController)
		{
			_audioTimer = new Timer(audioTimer_Tick, null, 20, 20);
		}

		protected override void ApplyAudioOutputFilter(short[] frame, int start, int length)
		{
			RenderVisualization(frame, start, length, OutputAudioVisualizer);
		}

		private static void RenderVisualization(short[] frame, int start, int length, AudioVisualizer audioVisualizer)
		{
			if (audioVisualizer != null)
			{
				var samples = new short[length];
				Buffer.BlockCopy(frame, start * sizeof(short), samples, 0, length * sizeof(short));
				audioVisualizer.RenderVisualization(samples);
			}
		}

		private void audioTimer_Tick(object target)
		{
			// Since we aren't wired up to a media stream source, use this to fake pulling the audio data.
			try
			{
				GetNextAudioFrame(ms =>
				{
					// No-op.
				});
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString());
			}
		}

		public AudioVisualizer OutputAudioVisualizer { get; set; }

	}
}
