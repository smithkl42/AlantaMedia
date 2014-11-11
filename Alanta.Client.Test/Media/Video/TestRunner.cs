using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using Alanta.Client.Media;
using Alanta.Client.Media.VideoCodecs;
using ReactiveUI;

namespace Alanta.Client.Test.Media.Video
{
	public class TestRunner : ReactiveObject
	{
		#region Constructors
		public TestRunner(Func<IVideoQualityController, IVideoCodec> codecFunction)
		{
			mCodecFunction = codecFunction;
			Results = new ObservableCollection<TestInstance>();
		}
		#endregion

		#region Fields and Properties

		private readonly Func<IVideoQualityController, IVideoCodec> mCodecFunction;
		private Action mOnStopped;

		protected bool mStopRequested;

		public int NoSendStart { get; set; }
		public int NoSendStop { get; set; }
		public int NoSendStep { get; set; }
		public int DeltaSendStart { get; set; }
		public int DeltaSendStop { get; set; }
		public int DeltaSendStep { get; set; }
		public short JpegQualityStart { get; set; }
		public short JpegQualityStop { get; set; }
		public short JpegQualityStep { get; set; }
		public List<byte[]> RawFrames { get; set; }
		public ObservableCollection<TestInstance> Results { get; private set; }
		public bool KeepProcessedFrames { get; set; }
		protected bool isRunning;

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
			if (isRunning)
			{
				throw new InvalidOperationException("Already running");
			}
			isRunning = true;
			mStopRequested = false;
			mOnStopped = onStopped;
			Results.Clear();
			ThreadPool.QueueUserWorkItem(o =>
			{
				try
				{
					RunAllTests();
				}
				catch (Exception ex)
				{
					OnUi(() => MessageBox.Show(ex.ToString()));
				}
				finally
				{
					isRunning = false;
					Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						try
						{
							if (mOnStopped != null)
							{
								mOnStopped();
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.ToString());
						}
					});
				}
			});
		}

		public virtual void Stop()
		{
			mStopRequested = true;
		}

		protected virtual void RunAllTests()
		{
			var videoChunkPool = new ObjectPool<ByteStream>(() => new ByteStream(VideoConstants.MaxPayloadSize * 2), bs => bs.Reset());
			for (int noSendCutoff = NoSendStart; noSendCutoff <= NoSendStop; noSendCutoff += NoSendStep)
			{
				for (int deltaSendCutoff = DeltaSendStart; deltaSendCutoff <= DeltaSendStop; deltaSendCutoff += DeltaSendStep)
				{
					for (short jpegQuality = JpegQualityStart; jpegQuality <= JpegQualityStop; jpegQuality += JpegQualityStep)
					{
						if (mStopRequested) return;
						if (deltaSendCutoff < noSendCutoff) break;
						var vqc = new TestVideoQualityController
						{
							NoSendCutoff = noSendCutoff,
							DeltaSendCutoff = deltaSendCutoff,
							JpegQuality = (byte)jpegQuality,
						};
						OnUi(() => Status = string.Format("Executing test for NoSendCutoff {0}; DeltaSendCutoff {1}; JpegQuality {2}", vqc.NoSendCutoff, vqc.DeltaSendCutoff, vqc.JpegQuality));
						var videoCodec = mCodecFunction(vqc);
						var test = new TestInstance(vqc, videoCodec, RawFrames, new List<byte[]>(), videoChunkPool);
						test.Execute(KeepProcessedFrames);
						OnUi(() => Results.Add(test));
					}
				}
			}
		}

		protected void OnUi(Action action)
		{
			Deployment.Current.Dispatcher.BeginInvoke(action);
		}


		#endregion

	}
}
