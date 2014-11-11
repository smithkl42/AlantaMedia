using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public class AudioMediaStreamSource : MediaStreamSource
	{
		#region Constructors
		public AudioMediaStreamSource(IAudioController audioController, AudioFormat audioFormat)
		{
			AudioController = audioController;
			_waveFormat = new WaveFormat();
			_waveFormat.FormatTag = WaveFormatType.Pcm;
			_waveFormat.BitsPerSample = AudioConstants.BitsPerSample;
			_waveFormat.Channels = AudioConstants.Channels;
			_waveFormat.SamplesPerSec = audioFormat.SamplesPerSecond;
			_waveFormat.AvgBytesPerSec = (AudioConstants.BitsPerSample / 8) * AudioConstants.Channels * audioFormat.SamplesPerSecond;
			_waveFormat.Ext = null;
			_waveFormat.BlockAlign = AudioConstants.Channels * (AudioConstants.BitsPerSample / 8);
			_waveFormat.Size = 0;
			AudioBufferLength = 50; //  audioFormat.MillisecondsPerFrame * 2; // 15 is the smallest buffer length that it will accept.
		}
		#endregion

		#region Fields and Properties
		protected AudioMediaStreamSourceLogger _logger = new AudioMediaStreamSourceLogger();
		protected WaveFormat _waveFormat;
		protected MediaStreamDescription _mediaStreamDescription;
		protected Dictionary<MediaSampleAttributeKeys, string> _emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>(); // Not used so empty.
		protected DateTime _startTime;

		public IAudioController AudioController { get; set; }
		public string InstanceName { get; set; }

		protected long _timestamp;

		#endregion

		#region Methods
		protected override void OpenMediaAsync()
		{
			// Define the available streams.
			var streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
			streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = _waveFormat.ToHexString();
			_mediaStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);
			var availableStreams = new List<MediaStreamDescription> { _mediaStreamDescription };

			// Define pieces that are common to all streams.
			var sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
			sourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.Zero.Ticks.ToString(CultureInfo.InvariantCulture);   // 0 = Indefinite
			sourceAttributes[MediaSourceAttributesKeys.CanSeek] = "0"; // 0 = False

			// Start the timer.
			_startTime = DateTime.Now;

			// Tell Silverlight we're ready to play.
			ReportOpenMediaCompleted(sourceAttributes, availableStreams);
		}

		protected override void CloseMedia()
		{
			// Nothing for right now.   
		}

		protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
		{
			throw new NotImplementedException();
		}

		// private DateTime _firstSampleRequestedAt = DateTime.MinValue;
		// private int _samplesRequested;
		protected override void GetSampleAsync(MediaStreamType mediaStreamType)
		{
			try
			{
				if (mediaStreamType != MediaStreamType.Audio)
				{
					return;
				}
				_logger.LogSampleRequested();
				if (AudioController == null)
				{
					ReportSample(new MemoryStream(0));
				}
				else
				{
					//if (_firstSampleRequestedAt == DateTime.MinValue)
					//{
					//    _firstSampleRequestedAt = DateTime.Now;
					//}
					//_samplesRequested++;
					AudioController.GetNextAudioFrame(ReportSample);
				}
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString);
			}
		}

		//private DateTime _firstSampleReportedAt = DateTime.MinValue;
		//private int _samplesReported;
		protected virtual void ReportSample(MemoryStream memoryStream)
		{
			try
			{
				//if (_firstSampleReportedAt == DateTime.MinValue)
				//{
				//    _firstSampleReportedAt = DateTime.Now;
				//}
				//if (++_samplesReported % 200 == 0)
				//{
				//    double averageSampleRequestTime = (DateTime.Now - _firstSampleRequestedAt).TotalMilliseconds/_samplesRequested;
				//    double averageSampleReportTime = (DateTime.Now - _firstSampleReportedAt).TotalMilliseconds/_samplesReported;
				//    ClientLogger.Debug("Samples requested:{0}; reported:{1}; avgRequestInterval:{2}; avgReportInterval:{3}", _samplesRequested, _samplesReported, averageSampleRequestTime, averageSampleReportTime);	
				//}

				var sample = new MediaStreamSample(
					_mediaStreamDescription,
					memoryStream,
					0,
					memoryStream.Length,
					(DateTime.Now - _startTime).Ticks,
					_emptySampleDict);
				ReportGetSampleCompleted(sample);
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString);
			}
		}

		protected override void SeekAsync(long seekToTime)
		{
			ReportSeekCompleted(seekToTime);
		}

		protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
		{
			throw new NotImplementedException();
		}
		#endregion

	}
}
