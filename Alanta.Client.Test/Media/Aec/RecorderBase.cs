using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Alanta.Client.Media;
using Alanta.Client.UI.Desktop.Controls.AudioVisualizer;

namespace Alanta.Client.Test.Media.Aec
{
	public class RecorderBase : IAudioController
	{
		public RecorderBase(CaptureSource captureSource, AudioSinkAdapter audioSinkAdapter, AudioVisualizer audioVisualizer)
		{
			mCaptureSource = captureSource;
			mAudioSinkAdapter = audioSinkAdapter;
			mAudioVisualizer = audioVisualizer;
			VisualizationRate = 1;
		}

		protected readonly CaptureSource mCaptureSource;
		protected readonly AudioSinkAdapter mAudioSinkAdapter;
		protected readonly AudioVisualizer mAudioVisualizer;
		public List<byte[]> RecordedFrames { get; protected set; }

		/// <summary>
		/// Whether we are currently recording sound.
		/// </summary>
		public bool IsConnected { get; protected set; }

		protected Action mOnRecordingStoppedCallback;
		protected int mRecordedFrameCount;

		private int mVisualizationRate;
		public int VisualizationRate
		{
			get { return mVisualizationRate; }
			set { mVisualizationRate = value < 1 ? 1 : value; }
		}

		public virtual void StartRecording(List<byte[]> recordedFrames, Action onRecordingStoppedCallback = null)
		{
			if (IsConnected)
			{
				throw new InvalidOperationException("Already recording");
			}
			IsConnected = true;
			RecordedFrames = recordedFrames;
			RecordedFrames.Clear();
			mOnRecordingStoppedCallback = onRecordingStoppedCallback;
			if (mAudioSinkAdapter != null) mAudioSinkAdapter.AudioController = this;
			if (mCaptureSource != null) mCaptureSource.Start();
		}

		public virtual void SubmitRecordedFrame(AudioContext ctx, byte[] frame)
		{
			var recordedFrame = new byte[frame.Length];
			Buffer.BlockCopy(frame, 0, recordedFrame, 0, frame.Length);
			RecordedFrames.Add(recordedFrame);
			if (mRecordedFrameCount++ % VisualizationRate == 0)
			{
				mAudioVisualizer.RenderVisualization(recordedFrame);
			}
		}

		public virtual void StopRecording()
		{
			if (!IsConnected) return;
			IsConnected = false;
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (mCaptureSource != null) mCaptureSource.Stop();
				if (mOnRecordingStoppedCallback != null)
				{
					mOnRecordingStoppedCallback();
					mOnRecordingStoppedCallback = null;
				}
			});
		}

		public void GetNextAudioFrame(Action<MemoryStream> callback)
		{
			throw new NotImplementedException();
		}

	}
}
