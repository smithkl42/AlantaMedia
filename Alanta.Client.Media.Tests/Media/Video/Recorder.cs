using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Alanta.Client.Media;
using Alanta.Client.Media.VideoCodecs;
using ReactiveUI;

namespace Alanta.Client.Test.Media.Video
{
	public class Recorder : ReactiveObject, IVideoController
	{
		#region Constructors
		public Recorder()
		{
		}
		#endregion

		#region Fields and Properties

		public List<byte[]> RecordedFrames { get; private set; }
		private bool isRecording;
		private Action mCallback;
		private int framesRecorded;
		public int FramesRecorded
		{
			get { return framesRecorded; }
			private set
			{
				if (value != framesRecorded)
				{
					framesRecorded = value;
					Deployment.Current.Dispatcher.BeginInvoke(() => this.RaisePropertyChanged(x => x.FramesRecorded));
				}
			}
		}
		#endregion

		#region Methods

		public void StartRecording(List<byte[]> recordedFrames, Action callback)
		{
			RecordedFrames = recordedFrames;
			RecordedFrames.Clear();
			mCallback = callback;
			isRecording = true;
		}

		public void StopRecording()
		{
			isRecording = false;
			if (mCallback != null)
			{
				mCallback();
			}
		}

		public void GetNextVideoFrame(ushort ssrcId, Action<MemoryStream> callback)
		{
			throw new NotImplementedException();
		}

		public void SetVideoFrame(byte[] frame, int stride)
		{
			if (isRecording)
			{
				if (stride <= 0)
				{
					VideoHelper.FlipAndReverse(frame, frame);
				}
				RecordedFrames.Add(frame);
				FramesRecorded++;
			}
		}
		#endregion
	}
}
