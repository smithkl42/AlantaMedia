using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Video
{
	public class TestVideoMediaStreamSource : MediaStreamSource
	{
		#region Constructors
		public TestVideoMediaStreamSource(IVideoController videoController, ushort ssrcId, int frameWidth, int frameHeight)
		{
			this.frameWidth = frameWidth;
			this.frameHeight = frameHeight;
			this.videoController = videoController;
			this.ssrcId = ssrcId;
		}
		#endregion

		#region Fields and Properties
		private readonly int frameWidth;
		private readonly int frameHeight;
		private readonly ushort ssrcId;
		private readonly IVideoController videoController;
		private readonly Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();
		private MediaStreamDescription videoDesc;
		private DateTime startTime;

		#endregion

		#region Methods
		protected override void OpenMediaAsync()
		{
			// Prepare the description of the stream.
			var sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
			var availableStreams = new List<MediaStreamDescription>();
			var streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
			streamAttributes[MediaStreamAttributeKeys.VideoFourCC] = "RGBA";
			streamAttributes[MediaStreamAttributeKeys.Height] = frameHeight.ToString();
			streamAttributes[MediaStreamAttributeKeys.Width] = frameWidth.ToString();
			videoDesc = new MediaStreamDescription(MediaStreamType.Video, streamAttributes);
			availableStreams.Add(videoDesc);

			// a zero timespan is an infinite video
			sourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.Zero.Ticks.ToString(CultureInfo.InvariantCulture);
			sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();
			startTime = DateTime.Now;

			// Tell Silverlight that we've prepared and opened our video
			ReportOpenMediaCompleted(sourceAttributes, availableStreams);
		}

		protected override void GetSampleAsync(MediaStreamType mediaStreamType)
		{
			try
			{
				if (mediaStreamType == MediaStreamType.Video)
				{
					videoController.GetNextVideoFrame(ssrcId, frameStream =>
					{
						if (frameStream != null)
						{
							// Send out the next sample
							frameStream.Position = 0;
							var msSamp = new MediaStreamSample(
								videoDesc,
								frameStream,
								0,
								frameStream.Length,
								(DateTime.Now - startTime).Ticks,
								emptySampleDict);
							ReportGetSampleCompleted(msSamp);
						}
					});
				}
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString());
			}
		}

		protected override void CloseMedia()
		{
		}

		protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
		{
			throw new NotImplementedException();
		}

		protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
		{
			throw new NotImplementedException();
		}

		protected override void SeekAsync(long seekToTime)
		{
			ReportSeekCompleted(seekToTime);
		}
		#endregion

	}
}
