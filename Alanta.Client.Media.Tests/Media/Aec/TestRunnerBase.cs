using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Media.Dsp.Speex;
using Alanta.Client.Media.Dsp.WebRtc;
using Alanta.Client.Test.Media.Aec;
using Alanta.Client.Ui.Controls.AudioVisualizer;
using ReactiveUI;

namespace Alanta.Client.Media.Tests.Media.Aec
{
	public abstract class TestRunnerBase : ReactiveObject
	{

		#region Constructors
		protected TestRunnerBase(EchoCancellerType echoCancellerType, 
			AudioVisualizer sourceAudioVisualizer, 
			AudioVisualizer speakersAudioVisualizer, 
			AudioVisualizer cancelledAudioVisualizer)
		{
			mEchoCancellerType = echoCancellerType;
			mSourceAudioVisualizer = sourceAudioVisualizer;
			mSpeakersAudioVisualizer = speakersAudioVisualizer;
			mCancelledAudioVisualizer = cancelledAudioVisualizer;
			Results = new ObservableCollection<PlaybackResults>();
			mediaConfig = new MediaConfig
			{
				EnableAec = true,
				EnableAgc = true,
				EnableDenoise = true
			};
			audioFormat = AudioFormat.Default;
		}
		#endregion

		#region Fields and Properties
		private DateTime mTestStartTime;
		private Action mOnStopped;

		protected List<byte[]> mCancelledFrames;
		protected bool mStopRequested;
		protected EchoCancellerType mEchoCancellerType;
		protected PlayerAec mPlayer;
		protected RecorderAec mRecorder;
		protected MediaConfig mediaConfig;
		protected AudioVisualizer mSourceAudioVisualizer;
		protected AudioVisualizer mSpeakersAudioVisualizer;
		protected AudioVisualizer mCancelledAudioVisualizer;
		protected EchoCancelFilter mEchoCancelFilter;

		public CaptureSource CaptureSource { get; set; }
		public AudioSinkAdapter AudioSinkAdapter { get; set; }
		public int ExpectedLatencyStart { get; set; }
		public int ExpectedLatencyEnd { get; set; }
		public int ExpectedLatencyStep { get; set; }
		public int ExpectedLatency { get; set; }
		public int FilterLengthStart { get; set; }
		public int FilterLengthEnd { get; set; }
		public int FilterLengthStep { get; set; }
		public int FilterLength { get; set; }
		public bool AecIsSynchronized { get; set; }
		public List<byte[]> SourceFrames { get; set; }
		public List<byte[]> SpeakerFrames { get; set; }
		public ObservableCollection<PlaybackResults> Results { get; private set; }
		public bool IsRunning { get; private set; }
		public AudioFormat audioFormat;

		private string mStatus;
		public string Status
		{
			get { return mStatus; }
			set { this.RaiseAndSetIfChanged(x => x.Status, ref mStatus, value); }
		}
		#endregion

		#region Methods
		public virtual void Start(Action onStopped)
		{
			if (IsRunning)
			{
				throw new InvalidOperationException("Already running");
			}
			IsRunning = true;
			mStopRequested = false;
			mOnStopped = onStopped;
			ExpectedLatency = ExpectedLatencyStart;
			FilterLength = FilterLengthStart;
			StartNextTest();
		}

		public virtual void Stop()
		{
			mStopRequested = true;
			mPlayer.StopPlaying();
			TestsFinished();
		}

		protected virtual void StartNextTest()
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => Status = string.Format("Executing test for latency {0} and filter length {1}", ExpectedLatency, FilterLength));

			mTestStartTime = DateTime.Now;
			IAudioFilter playedResampler;
			IAudioFilter recordedResampler;
			mCancelledFrames = new List<byte[]>();

			// Decide whether to synchronize the audio or not.
			if (AecIsSynchronized)
			{
				playedResampler = new ResampleFilter(audioFormat, audioFormat);
				recordedResampler = new ResampleFilter(audioFormat, audioFormat);
			}
			else
			{
				playedResampler = new NullAudioFilter(audioFormat.BytesPerFrame);
				recordedResampler = new NullAudioFilter(audioFormat.BytesPerFrame);
			}

			// Initialize the echo canceller
			playedResampler.InstanceName = "EchoCanceller_played";
			recordedResampler.InstanceName = "EchoCanceller_recorded";
			mediaConfig.ExpectedAudioLatency = ExpectedLatency;
			mediaConfig.FilterLength = FilterLength;

			switch (mEchoCancellerType)
			{
				case EchoCancellerType.Speex2:
					mEchoCancelFilter = new SpeexEchoCanceller2(mediaConfig, audioFormat, AudioFormat.Default);
					break;
				case EchoCancellerType.WebRtc:
					mEchoCancelFilter = new WebRtcFilter(mediaConfig.ExpectedAudioLatency,
														mediaConfig.FilterLength,
														audioFormat,
														AudioFormat.Default,
														true,
														true,
														false,
														playedResampler, recordedResampler);
					break;
				case EchoCancellerType.TimeDomain:
					mEchoCancelFilter = new TimeDomainEchoCancelFilter(ExpectedLatency, FilterLength, audioFormat, AudioFormat.Default, playedResampler, recordedResampler);
					break;
				case EchoCancellerType.Speex:
					mEchoCancelFilter = new SpeexEchoCancelFilter(ExpectedLatency, FilterLength, audioFormat, AudioFormat.Default, playedResampler, recordedResampler);
					break;
				default:
					throw new Exception();
			}

			mPlayer = GetPlayer();
			mRecorder = GetRecorder();

			mRecorder.StartRecording(SpeakerFrames, FinishTest);
			mPlayer.StartPlaying(SourceFrames, mRecorder.StopRecording);
		}

		protected abstract PlayerAec GetPlayer();
		protected abstract RecorderAec GetRecorder();

		protected virtual void FinishTest()
		{
			mPlayer.StopPlaying();
			mRecorder.StopRecording();

			TimeSpan duration = DateTime.Now - mTestStartTime;
			var playbackResults = new PlaybackResults(duration, ExpectedLatency, FilterLength, SpeakerFrames, mCancelledFrames);
			Results.Add(playbackResults);

			if (ExpectedLatency >= ExpectedLatencyEnd || mStopRequested)
			{
				TestsFinished();
				return;
			}
			if (FilterLength >= FilterLengthEnd)
			{
				FilterLength = FilterLengthStart;
				ExpectedLatency += ExpectedLatencyStep;
			}
			else
			{
				FilterLength += FilterLengthStep;
			}

			StartNextTest();
		}

		private void TestsFinished()
		{
			IsRunning = false;
			if (mOnStopped != null)
			{
				mOnStopped();
				mOnStopped = null;
			}
		}
		#endregion
	}
}
