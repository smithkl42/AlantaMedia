using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Video
{
	public class Player : IVideoController
	{
		public Player(MediaElement mediaElement, IVideoQualityController videoQualityController)
		{
			mMediaElement = mediaElement;
			timeBetweenFrames = TimeSpan.FromMilliseconds(1000.0 / videoQualityController.AcceptFramesPerSecond);
		}

		private int lastFrameIndex;
		private List<byte[]> mFrames;
		private Action mCallback;
		private readonly MediaElement mMediaElement;
		private TimeSpan timeBetweenFrames;
		private byte[] lastFrame;
		private DateTime lastFramePlayedAt;

		public void StartPlaying(List<byte[]> frames, Action callback)
		{
			if (mFrames != null) return;
			if (mMediaElement != null)
			{
				mMediaElement.Play();
			}
			mFrames = frames;
			mCallback = callback;
			lastFrameIndex = 0;
		}

		public void StopPlaying()
		{
			mFrames = null;
			lastFrame = null;
			Deployment.Current.Dispatcher.BeginInvoke(() => mMediaElement.Stop());
			if (mCallback != null)
			{
				var callback = mCallback; // In case mCallback gets reset.
				Deployment.Current.Dispatcher.BeginInvoke(callback);
			}
		}

		public void GetNextVideoFrame(ushort ssrcId, Action<MemoryStream> callback)
		{
			if (mFrames != null)
			{
				if (lastFrameIndex < mFrames.Count)
				{
					if (lastFrame == null || lastFramePlayedAt + timeBetweenFrames < DateTime.Now)
					{
						lastFrame = mFrames[lastFrameIndex++];
						lastFramePlayedAt = DateTime.Now;
					}
					callback(new MemoryStream(lastFrame));
					return;
				}
				StopPlaying();
			}
			callback(new MemoryStream(new byte[VideoConstants.BytesPerFrame]));
		}

		public void SetVideoFrame(byte[] frame, int stride)
		{
			throw new NotImplementedException();
		}
	}
}
