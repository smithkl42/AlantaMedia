using System.ComponentModel;
using System;
using System.Windows;
using Alanta.Client.Common;

namespace Alanta.Client.Media
{
	public class AudioStatistics : INotifyPropertyChangedEx
	{
		public AudioStatistics(string name, MediaStatistics stats = null, int reportingInterval = 50)
		{
			Name = name;
			ReportingInterval = reportingInterval;
			if (stats != null)
			{
				Counter = stats.RegisterCounter(name);
				Counter.IsActive = false; // ks 7/28/11 - Disable for now
			}
		}

		public Counter Counter { get; set; }

		private string _name;
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
				this.RaisePropertyChangedEx(x => x.Name);
			}
		}

		private int _frameCount;
		public int FrameCount
		{
			get
			{
				return _frameCount;
			}
			private set
			{
				_frameCount = value;
				if (_framesSinceUpdate % ReportingInterval == 0)
				{
					this.RaisePropertyChangedEx(x => x.FrameCount);
				}
			}
		}

		public int ReportingInterval { get; set; }

		private double _volumeSinceUpdate;
		private int _framesSinceUpdate;
		private short[] _latestFrame;
		public Array LatestFrame
		{
			get
			{
				return _latestFrame;
			}
			set
			{
#if DEBUG
				if (Counter != null && Counter.IsActive)
				{
					_latestFrame = new short[Buffer.ByteLength(value)/sizeof (short)];
					Buffer.BlockCopy(value, 0, _latestFrame, 0, Buffer.ByteLength(value));
					if (Counter != null)
					{
						_volumeSinceUpdate += Dsp.DspHelper.GetRootMeanSquare(_latestFrame);
						_framesSinceUpdate++;
					}
					if (++FrameCount%ReportingInterval == 0 && value != null)
					{
						if (Counter != null)
						{
							Counter.Update(_volumeSinceUpdate/_framesSinceUpdate);
							_framesSinceUpdate = 0;
							_volumeSinceUpdate = 0;
						}
						this.RaisePropertyChangedEx(x => x.LatestFrame);
					}
				}
#endif
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void RaisePropertyChanged(string propertyName)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					if (PropertyChanged != null)
					{
						PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
					}
				});
		}
	}
}
