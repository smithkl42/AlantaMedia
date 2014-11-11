using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Timing
{
	public class TimingMediaStatistics : MediaStatistics
	{
		public override Counter RegisterCounter(string name, int length = 50, bool isActive = true)
		{
			var counter = base.RegisterCounter(name, length, isActive);
			counter.IsActive = counter.Name.ToLower().Contains("audio");
			return counter;
		}

		public override void RegisterCounter(Counter counter)
		{
			counter.IsActive = counter.Name.ToLower().Contains("audio");
			base.RegisterCounter(counter);
		}
	}
}
