using System.Collections.Generic;
using System.Threading;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media.Aec
{
	public class TestRunnerFast : TestRunnerBase
	{
		public TestRunnerFast(EchoCancellerType echoCancellerType,
			AudioVisualizer sourceAudioVisualizer,
			AudioVisualizer speakersAudioVisualizer,
			AudioVisualizer cancelledAudioVisualizer,
			List<byte[]> speakerFrames)
			: base(echoCancellerType, sourceAudioVisualizer, speakersAudioVisualizer, cancelledAudioVisualizer)
		{
			SpeakerFrames = speakerFrames;
		}

		protected override PlayerAec GetPlayer()
		{
			return new PlayerAec(null, null, mSourceAudioVisualizer, mEchoCancelFilter);
		}

		protected override RecorderAec GetRecorder()
		{
			AudioSinkAdapter.AudioController = null; // Ensures that data from the microphone doesn't get into the system.
			return new RecorderAecFast(mSpeakersAudioVisualizer, mCancelledAudioVisualizer, mEchoCancelFilter, mCancelledFrames);
		}

		protected override void StartNextTest()
		{
			// Set everything up.
			base.StartNextTest();

			// Tweak a few parameters.
			mPlayer.VisualizationRate = 10;
			mRecorder.VisualizationRate = 10;

			// Actually run the test. Since the media element in the player is null, this is how to trigger the echo cancellation and all the rest.
			ThreadPool.QueueUserWorkItem(o =>
			{
				var resampler = new ResampleFilter(audioFormat, audioFormat);
				var dtx = new DtxFilter(audioFormat);
				var encoder = new G711MuLawEncoder(audioFormat);
				var ctx = new AudioContext(audioFormat, resampler, dtx, mEchoCancelFilter, encoder);
				for (int i = 0; i < SourceFrames.Count && i < SpeakerFrames.Count; i++)
				{
					if (mStopRequested)
					{
						mStopRequested = false;
						return;
					}
					int index = i;

					// This has the (necessary) side-effect of registering the (virtually) played frame.
					mPlayer.GetNextAudioFrame(ms => mRecorder.SubmitRecordedFrame(ctx, SpeakerFrames[index]));
				}

				// Stopping everything has the (necessary) side-effect of starting the next test, if there is one.
				mRecorder.StopRecording();
				mPlayer.StopPlaying();
			});
		}

	}
}
