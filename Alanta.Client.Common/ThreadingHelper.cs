using System;
using System.Threading;
using System.Windows.Threading;

namespace Alanta.Client.Common
{
	public class ThreadingHelper
	{
		public static void SleepAsync(int millisecondsToSleep, Action action)
		{
			var timer = new Timer(o => action());
			timer.Change(millisecondsToSleep, Timeout.Infinite);
		}

		public static void SleepAsync(TimeSpan interval, Action action)
		{
			SleepAsync((int)interval.TotalMilliseconds, action);
		}

		public static void DispatcherSleepAsync(int millisecondsToSleep, Action action)
		{
			var interval = TimeSpan.FromMilliseconds(millisecondsToSleep);
			DispatcherSleepAsync(interval, action);
		}

		public static void DispatcherSleepAsync(TimeSpan interval, Action action)
		{
			var timer = new DispatcherTimer();
			EventHandler[] timerHandler = { null };
			timerHandler[0] = (s, e) =>
			{
				timer.Tick -= timerHandler[0];
				if (action != null)
				{
					action();
				}
			};
			timer.Tick += timerHandler[0];
			timer.Interval = interval;
			timer.Start();
		}
	}
}
