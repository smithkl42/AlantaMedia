using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public class AudioSinkAdapterLogger
	{
		public AudioFormat RawAudioFormat { get; set; }
		public AudioFormat AudioFormat { get; set; }

		public double RawFramesAverageLengthRecent { get { return _rawFrameLengthRecent / (double)_rawFramesRecent; } }
		public double RawFramesAverageLengthTotal { get { return _rawFrameLengthTotal / (double)_rawFramesTotal; } }
		public double RawFramesAverageDurationRecent { get { return _rawDurationRecent / (double)_rawFramesRecent; } }
		public double RawFramesAverageDurationTotal { get { return _rawDurationTotal / (double)_rawFramesTotal; } }

		public double ResampledFramesAverageLengthRecent { get { return _resampledFrameLengthRecent / (double)_resampledFramesRecent; } }
		public double ResampledFramesAverageLengthTotal { get { return _resampledFrameLengthTotal / (double)_resampledFramesTotal; } }

		private DateTime _firstFrameAt = DateTime.MinValue;
		private DateTime _lastResetAt;
		private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
		private long _rawFramesRecent;
		private long _rawFramesTotal;
		private long _rawDurationRecent;
		private long _rawDurationTotal;
		private long _rawFrameLengthRecent;
		private long _rawFrameLengthTotal;
		private long _resampledFramesRecent;
		private long _resampledFramesTotal;
		private long _resampledFrameLengthRecent;
		private long _resampledFrameLengthTotal;

		public void LogRawFrame(long sampleTime, long sampleDuration, byte[] rawFrame)
		{
			var now = DateTime.Now;
			if (_firstFrameAt == DateTime.MinValue)
			{
				_firstFrameAt = now;
				_lastResetAt = now;
			}
			_rawFramesRecent++;
			_rawFramesTotal++;
			_rawDurationRecent += sampleDuration;
			_rawDurationTotal += sampleDuration;
			_rawFrameLengthRecent += rawFrame.Length;
			_rawFrameLengthTotal += rawFrame.Length;

			if (now - _lastResetAt > _interval)
			{
				ClientLogger.Debug("AudioSinkLogger Summary: \r\n" +
					"*RawFramesRecent:{0}\r\n" +
					"*RawFramesTotal:{1}\r\n" +
					"*RawFramesAverageLengthRecent:{2}\r\n" +
					"*RawFramesAverageLengthTotal:{3}\r\n" +
					"*RawFramesAverageDurationRecent:{4}\r\n" +
					"*RawFramesAverageDurationTotal:{5}\r\n" +
					"*ResampledFramesRecent:{6}\r\n" +
					"*ResampledFramesTotal:{7}\r\n" +
					"*ResampledFramesAverageLengthRecent:{8}\r\n" +
					"*ResampledFramesAverageLengthTotal:{9}\r\n",
					_rawFramesRecent,
					_rawFramesTotal,
					RawFramesAverageLengthRecent,
					RawFramesAverageLengthTotal,
					RawFramesAverageDurationRecent,
					RawFramesAverageDurationTotal,
					_resampledFramesRecent,
					_resampledFramesTotal,
					ResampledFramesAverageLengthRecent,
					ResampledFramesAverageLengthTotal);

				_rawFramesRecent = 0;
				_rawDurationRecent = 0;
				_rawFrameLengthRecent = 0;
				_resampledFramesRecent = 0;
				_resampledFrameLengthRecent = 0;
				_lastResetAt = now;
			}
		}

		public void LogResampledFrame(byte[] resampledFrame)
		{
			_resampledFramesRecent++;
			_resampledFramesTotal++;
			_resampledFrameLengthRecent += resampledFrame.Length;
			_resampledFrameLengthTotal += resampledFrame.Length;
		}
	}
}
