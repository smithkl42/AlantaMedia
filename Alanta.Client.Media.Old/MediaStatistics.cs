using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using ReactiveUI;

namespace Alanta.Client.Media
{
	/// <summary>
	/// Represents a collection of float arrays, each of which represents the changing value of a specific statistic over time.
	/// </summary>
	public class MediaStatistics : ReactiveObject
	{
		public MediaStatistics()
		{
			Counters = new ObservableCollection<Counter>();
		}

		public ObservableCollection<Counter> Counters { get; private set; }

		public virtual Counter RegisterCounter(string name, int length = 50, bool isActive = true)
		{
			var counter = new Counter
			{
				Name = name,
				Length = length
			};
			RegisterCounter(counter);
			counter.IsActive = isActive;
			return counter;
		}

		public virtual void RegisterCounter(Counter counter)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => Counters.Add(counter));
		}

		public void RemoveCounter(string name)
		{
			var counter = Counters.FirstOrDefault(c => c.Name == name);
			RemoveCounter(counter);
		}

		public void RemoveCounter(Counter counter)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => Counters.Remove(counter));
		}

	}

	public class Counter : ReactiveObject
	{
		public Counter()
		{
			Values = new ObservableCollection<CounterEntry>();
			IsActive = true;
			MinimumDelta = double.MinValue;
		}

		public ObservableCollection<CounterEntry> Values { get; set; }

		public event EventHandler<EventArgs<double>> Updated;

		private string _name;
		public string Name
		{
			get { return _name; }
			set { this.RaiseAndSetIfChanged(x => x.Name, ref _name, value); }
		}

		private int _length;
		public int Length
		{
			get { return _length; }
			set { this.RaiseAndSetIfChanged(x => x.Length, ref _length, value); }
		}

		private CounterEntry _lastCounterEntry;

		private bool _isActive;
		public bool IsActive
		{
			get { return _isActive; }
			set { this.RaiseAndSetIfChanged(x => x.IsActive, ref _isActive, value); }
		}

		private double Minimum
		{
			set
			{
				if (!_isAxisMinimumSet)
				{
					this.RaiseAndSetIfChanged(x => x.AxisMinimum, ref _axisMinimum, value);
				}
			}
		}

		private double Maximum
		{
			set
			{
				if (!_isAxisMaximumSet)
				{
					this.RaiseAndSetIfChanged(x => x.AxisMaximum, ref _axisMaximum, value);
				}
			}
		}

		private bool _isAxisMinimumSet;
		private double _axisMinimum;
		public double AxisMinimum
		{
			get { return _axisMinimum; }
			set
			{
				_isAxisMinimumSet = true;
				this.RaiseAndSetIfChanged(x => x.AxisMinimum, ref _axisMinimum, value);
			}
		}

		private bool _isAxisMaximumSet;
		private double _axisMaximum;
		public double AxisMaximum
		{
			get { return _axisMaximum; }
			set
			{
				_isAxisMaximumSet = true;
				this.RaiseAndSetIfChanged(x => x.AxisMaximum, ref _axisMaximum, value);
			}
		}

		public double MinimumDelta { get; set; }

		public void Update(double value)
		{
			if (IsActive)
			{
				var entry = new CounterEntry();
				if (double.IsInfinity(value) || double.IsNaN(value))
				{
					value = 0;
				}
				entry.Value = value;
				entry.EntryNumber = _lastCounterEntry != null ? _lastCounterEntry.EntryNumber + 1 : 0;
				_lastCounterEntry = entry;
				Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						try
						{
							while (Values.Count > Length)
							{
								Values.RemoveAt(0);
							}
							Values.Add(entry);

							// Set the minimum and maximum values on the 
							var minimum = Values.Min(c => c.Value); // *.95;
							var maximum = Values.Max(c => c.Value); // *1.05;
							var delta = maximum - minimum;
							if (delta < MinimumDelta)
							{
								var center = minimum + (delta / 2);
								minimum = center - (MinimumDelta / 2);
								maximum = center + (MinimumDelta / 2);
							}
							if (minimum >= maximum)
							{
								maximum++;
							}
							Debug.Assert(minimum < maximum, "Minimum should be less than maximum");
							Maximum = maximum;
							Minimum = minimum;
							if (Updated != null)
							{
								Updated(this, new EventArgs<double>(value));
							}
						}
						catch (Exception ex)
						{
							ClientLogger.Debug(ex.ToString);
						}
					});
			}
		}
	}

	public class CounterEntry
	{
		public double Value { get; set; }
		public ulong EntryNumber { get; set; }
	}
}
