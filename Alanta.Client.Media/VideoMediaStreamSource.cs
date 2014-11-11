using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Media;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public class VideoMediaStreamSource : MediaStreamSource
	{
		#region Constructors
		public VideoMediaStreamSource(IVideoController videoController, IVideoQualityController videoQualityController, ushort ssrcId, int frameWidth, int frameHeight)
		{
			_frameWidth = frameWidth;
			_frameHeight = frameHeight;
			_videoController = videoController;
			_videoQualityController = videoQualityController;
			_ssrcId = ssrcId;
		}
		#endregion

		#region Fields and Properties
		private readonly int _frameWidth;
		private readonly int _frameHeight;
		private readonly ushort _ssrcId;
		private readonly IVideoController _videoController;
		private readonly IVideoQualityController _videoQualityController;
		private readonly Dictionary<MediaSampleAttributeKeys, string> _emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();
		private MediaStreamDescription _videoDesc;
		private DateTime _startTime;
		private Timer _sampleTimer;
		private bool _sampleRequested;
		private bool _isClosed = true;
		#endregion

		#region Methods
		protected override void OpenMediaAsync()
		{
			// Prepare the description of the stream.
			var sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
			var availableStreams = new List<MediaStreamDescription>();
			var streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
			streamAttributes[MediaStreamAttributeKeys.VideoFourCC] = "RGBA";
			streamAttributes[MediaStreamAttributeKeys.Height] = _frameHeight.ToString();
			streamAttributes[MediaStreamAttributeKeys.Width] = _frameWidth.ToString();
			_videoDesc = new MediaStreamDescription(MediaStreamType.Video, streamAttributes);
			availableStreams.Add(_videoDesc);

			// a zero timespan is an infinite video
			sourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.Zero.Ticks.ToString(CultureInfo.InvariantCulture);
			sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();
			_startTime = DateTime.Now;

			// Start the callback timer (so that we limit the number of frames sent to the media element, to reduce CPU utilization).
			var callback = new TimerCallback(GetSample);
			double frameDelayInMs = (1000 / (double)_videoQualityController.AcceptFramesPerSecond);
			_sampleTimer = new Timer(callback);
			_sampleTimer.Change(TimeSpan.FromMilliseconds(frameDelayInMs), TimeSpan.FromMilliseconds(frameDelayInMs));
			_videoQualityController.VideoQualityChanged += videoQualityController_VideoQualityChanged;

			_isClosed = false;

			// Tell Silverlight that we've prepared and opened our video
			ReportOpenMediaCompleted(sourceAttributes, availableStreams);
		}

		void videoQualityController_VideoQualityChanged(object sender, EventArgs e)
		{
			double frameDelayInMs = (1000 / (double)_videoQualityController.AcceptFramesPerSecond);
			_sampleTimer.Change(TimeSpan.FromMilliseconds(frameDelayInMs), TimeSpan.FromMilliseconds(frameDelayInMs));
		}

		protected override void GetSampleAsync(MediaStreamType mediaStreamType)
		{
			try
			{
				if (mediaStreamType == MediaStreamType.Video)
				{
					_sampleRequested = true;
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Get sample failed");
			}
		}

		private void GetSample(object userState)
		{
			try
			{
				if (_sampleRequested && !_isClosed)
				{
					_sampleRequested = false;
					_videoController.GetNextVideoFrame(_ssrcId, ReportSample);
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Report sample failed");
			}
		}

		protected virtual void ReportSample(MemoryStream frameStream)
		{
			try
			{
				if (frameStream != null)
				{
					frameStream.Position = 0; // .Seek(0, SeekOrigin.Begin);

					// Send out the next sample
					var msSamp = new MediaStreamSample(
						_videoDesc,
						frameStream,
						0,
						frameStream.Length,
						(DateTime.Now - _startTime).Ticks,
						_emptySampleDict);

					ReportGetSampleCompleted(msSamp);
				}
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString);
			}
		}

		protected override void CloseMedia()
		{
			_isClosed = true;
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
