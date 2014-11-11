using System;
using System.Windows.Media;
using Alanta.Client.Common;
using Alanta.Client.Media;
using ReactiveUI;

namespace Alanta.Client.Test.Media.MediaServer
{
	public class MediaServerKpi : ReactiveObject, IDisposable 
	{
		public MediaServerKpi(Counter counter, double minAcceptableValue, double maxAcceptableValue)
		{
			this.counter = counter;
			counter.IsActive = true;
			Name = counter.Name;
			MaxAcceptableValue = maxAcceptableValue;
			MinAcceptableValue = minAcceptableValue;
			StatusColor = greenBrush;
			LastValueStatusColor = greenBrush;
			counter.Updated += counter_Updated;
		}

		public void Dispose()
		{
			counter.Updated -= counter_Updated;
		}

		void counter_Updated(object sender, EventArgs<double> e)
		{
			LogValue(e.Value);
		}

		private readonly SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green );
		private readonly SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
		private readonly SolidColorBrush yellowBrush = new SolidColorBrush(Colors.Yellow);
		public string Name { get; set; }
		public double MaxAcceptableValue { get; set; }
		public double MinAcceptableValue { get; set; }
		private Counter counter;

		private KpiStatus kpiStatus;
		public KpiStatus KpiStatus
		{
			get { return kpiStatus; }
			set { this.RaiseAndSetIfChanged(x => x.KpiStatus, ref kpiStatus, value); }
		}

		private SolidColorBrush statusColor;
		public SolidColorBrush StatusColor
		{
			get { return statusColor; }
			set { this.RaiseAndSetIfChanged(x => x.StatusColor, ref statusColor, value); }
		}

		private SolidColorBrush lastValueStatusColor;
		public SolidColorBrush LastValueStatusColor
		{
			get { return lastValueStatusColor; }
			set { this.RaiseAndSetIfChanged(x => x.LastValueStatusColor, ref lastValueStatusColor, value); }
		}


		private double lastValue;
		public double LastValue
		{
			get { return lastValue; }
			set { this.RaiseAndSetIfChanged(x => x.LastValue, ref lastValue, value); }
		}

		private double lastBadValue;
		public double LastBadValue
		{
			get { return lastBadValue; }
			set { this.RaiseAndSetIfChanged(x => x.LastBadValue, ref lastBadValue, value); }
		}

		private DateTime lastBadValueOn = DateTime.MinValue;
		private TimeSpan yellowRecoveryInterval = TimeSpan.FromSeconds(15);
		private TimeSpan greenRecoveryInterval = TimeSpan.FromSeconds(30);

		public void LogValue(double value)
		{
			if (value < MinAcceptableValue)
			{
				LastBadValue = value;
				KpiStatus = KpiStatus.TooLow;
				StatusColor = redBrush;
				LastValueStatusColor = StatusColor;
				lastBadValueOn = DateTime.Now;
			}
			else if (value > MaxAcceptableValue)
			{
				LastBadValue = value;
				KpiStatus = KpiStatus.TooHigh;
				StatusColor = redBrush;
				LastValueStatusColor = StatusColor;
				lastBadValueOn = DateTime.Now;
			}
			else
			{
				LastValueStatusColor = greenBrush;
			}
			LastValue = value;
			if (lastBadValueOn + greenRecoveryInterval < DateTime.Now)
			{
				KpiStatus = KpiStatus.Good;
				StatusColor = greenBrush;
			}
			else if (lastBadValueOn + yellowRecoveryInterval < DateTime.Now)
			{
				StatusColor = yellowBrush;
			}
		}

	}

	public enum KpiStatus
	{
		Good,
		TooHigh,
		TooLow
	}
}
