using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Alanta.Client.Ui.Controls.AudioVisualizer;

namespace Alanta.Client.Media.Tests.Media.Aec
{
	public class PlayerBase : IAudioController
	{
		public PlayerBase(MediaElement mediaElement, AudioMediaStreamSource audioStreamSource, AudioVisualizer audioVisualizer)
		{
			mMediaElement = mediaElement;
			mAudioStreamSource = audioStreamSource;
			Frames = new List<byte[]>();
			mAudioVisualizer = audioVisualizer;
			VisualizationRate = 1;
		}

		private readonly MediaElement mMediaElement;
		private readonly AudioMediaStreamSource mAudioStreamSource;
		private readonly AudioVisualizer mAudioVisualizer;

		/// <summary>
		/// Whether we are currently playing sound
		/// </summary>
		public bool IsConnected { get; set; }

		public List<byte[]> Frames { get; private set; }
		protected int mFrameIndex;
		protected Action mPlayFinishedCallback;

		private int mVisualizationRate;
		public int VisualizationRate
		{
			get { return mVisualizationRate; }
			set { mVisualizationRate = value < 1 ? 1 : value; }
		}

		public virtual void StartPlaying(List<byte[]> frames, Action playFinishedCallback = null)
		{
			if (IsConnected)
			{
				throw new InvalidOperationException("Already playing");
			}
			IsConnected = true;
			Frames = frames;
			mFrameIndex = 0;
			mPlayFinishedCallback = playFinishedCallback;
			if (mAudioStreamSource != null)
			{
				mAudioStreamSource.AudioController = this;
			}
			if (mMediaElement != null)
			{
				mMediaElement.Play();
			}
		}

		public virtual void StopPlaying()
		{
			if (!IsConnected) return;
			IsConnected = false;
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (mMediaElement != null)
				{
					mMediaElement.Stop();
				}
				Frames = null;
				if (mPlayFinishedCallback != null)
				{
					mPlayFinishedCallback();
					mPlayFinishedCallback = null;
				}
			});
		}

		public virtual void GetNextAudioFrame(Action<MemoryStream> callback)
		{
			if (IsConnected)
			{
				if (mFrameIndex < Frames.Count)
				{
					byte[] frame = Frames[mFrameIndex++];
					if (mFrameIndex % VisualizationRate == 0)
					{
						Deployment.Current.Dispatcher.BeginInvoke(() => mAudioVisualizer.RenderVisualization(frame));
					}
					callback(new MemoryStream(frame));
					return;
				}
				StopPlaying();
			}
			callback(new MemoryStream());
		}

		public void SubmitRecordedFrame(AudioContext ctx, byte[] frame)
		{
			throw new NotImplementedException();
		}
	}
}
