using System;
using System.Windows;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{

	/// <summary>
	/// Provides a glimpse into the current operating environment so that the CodecFactory can make
	/// intelligent decisions about which codecs to select.
	/// </summary>
	public class MediaEnvironment : IMediaEnvironment
	{
		public MediaEnvironment(MediaStatistics mediaStatistics = null)
		{
			try
			{
				_analytics = new Analytics();
				if (mediaStatistics != null)
				{
					_cpuCounter = mediaStatistics.RegisterCounter("CPU");
					_cpuCounter.AxisMinimum = 0;
					_cpuCounter.AxisMaximum = 100;
					_cpuCounter.IsActive = true;
				}
			}
			catch (Exception ex)
			{
				// ks 9/23/11 - Not sure why this happens sometimes - might be a Silverlight 5 issue.
				ClientLogger.Debug(ex.ToString);
			}
		}

		public int RemoteSessions { get; set; }

		private readonly Analytics _analytics;
		public Analytics Analytics { get { return _analytics; } }

		private readonly Counter _cpuCounter;
		private double _processorLoad = 50.0;
		private const int checksToCache = 50;
		private int _processorChecks;
		private const int updateRatio = 5;
		private int _updateChecks;

		public DateTime Now
		{
			get { return DateTime.Now; }
		}

		/// <summary>
		/// The local processor load.
		/// </summary>
		public double LocalProcessorLoad
		{
			get
			{
				if (_processorChecks++ % checksToCache == 0 && _analytics != null)
				{
					Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						if (_analytics != null)
						{
							_processorLoad = _analytics.AverageProcessorLoad;
							if (_updateChecks++ % updateRatio == 0 && _cpuCounter != null)
							{
								_cpuCounter.Update(_processorLoad);
							}
						}
					});
				}
				return _processorLoad;
			}
		}

		/// <summary>
		/// The most recent remote processor load.
		/// </summary>
		public double RemoteProcessorLoad { get; set; }

		public bool IsMediaRecommended
		{
			get
			{
				if (Analytics != null && Analytics.AverageProcessLoad > 50)
				{
					return false;
				}
				// ks 5/29/2012 - Removed Macs from the list of disqualifying characteristics, since
				// it seems like Macs work at least marginally acceptably.
				//if (Environment.OSVersion.Platform == PlatformID.MacOSX)
				//{
				//    return false;
				//}
				return true;
			}
		}


	}
}
