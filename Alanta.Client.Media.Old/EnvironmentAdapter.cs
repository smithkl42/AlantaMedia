using System;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	/// <summary>
	/// Returns different instances of classes based on whether CPU has been running too hot.
	/// </summary>
	/// <typeparam name="T">The type of the object to return</typeparam>
	public class EnvironmentAdapter<T> where T : class
	{
		/// <summary>
		/// Creates a new instance of the EnvironmentAdapter class
		/// </summary>
		/// <param name="mediaEnvironment">The IMediaEnvironment instance that the EnvironmentAdapter can use to gather information about its environment</param>
		/// <param name="directInstance">The instance of type T for when the CPU is low and there's only one remote session</param>
		/// <param name="conferenceInstance">The instance of type T for when the CPU is low and there are multiple remote sessions</param>
		/// <param name="remoteFallbackInstance">The instance of type T that should be returned when one or more remote CPU's is running too hot</param>
		/// <param name="fallbackInstance">The instance of type T that should be returned when the local CPU is running too hot</param>
		public EnvironmentAdapter(IMediaEnvironment mediaEnvironment,
			T directInstance,
			T conferenceInstance,
			T remoteFallbackInstance,
			T fallbackInstance)
		{
			DirectInstance = directInstance;
			ConferenceInstance = conferenceInstance;
			RemoteFallbackInstance = remoteFallbackInstance;
			FallbackInstance = fallbackInstance;

			currentInstance = directInstance;
			this.mediaEnvironment = mediaEnvironment;
			MaxRecommendedLoad = 70;
			MaxSafeLoad = 60;
			MinimumTimeUntilDowngrade = TimeSpan.FromSeconds(5);
			MinimumTimeUntilUpgrade = TimeSpan.FromSeconds(15);
		}

		private readonly IMediaEnvironment mediaEnvironment;

		/// <summary>
		/// The instance to return there are only two people in the call.
		/// </summary>
		public T DirectInstance { get; set; }

		/// <summary>
		/// The instance to return when more than two people are in the call.
		/// </summary>
		public T ConferenceInstance { get; set; }

		/// <summary>
		/// The instance to return when one or more remote CPU's is running too hot (regardless of how many people are in the call).
		/// </summary>
		public T RemoteFallbackInstance { get; set; }

		/// <summary>
		/// The instance to return when the local CPU is running too hot (regardless of how many people are in the call).
		/// </summary>
		public T FallbackInstance { get; set; }

		/// <summary>
		/// The current instance of T.
		/// </summary>
		private T currentInstance;

		/// <summary>
		/// We will wait at least this long before checking to see if we should downgrade.
		/// </summary>
		public TimeSpan MinimumTimeUntilDowngrade { get; set; }

		/// <summary>
		/// We will wait at least this long before checking to see if we should upgrade.
		/// </summary>
		public TimeSpan MinimumTimeUntilUpgrade { get; set; }

		/// <summary>
		/// If the ProcessorLoad is above this, we will downgrade.
		/// </summary>
		public double MaxRecommendedLoad { get; set; }

		/// <summary>
		/// If the ProcessorLoad is below this, we may upgrade.
		/// </summary>
		public double MaxSafeLoad { get; set; }

		private double totalLocalLoad;
		private double totalRemoteLoad;
		private int loadChecks;
		private DateTime lastInstanceCheck = DateTime.MinValue;
		private int lastRemoteSessions;

		public T GetItem()
		{
			// Use an average of the CPU load to determine whether we should use the 
			// normal or the fallback instance of the item.
			totalLocalLoad += mediaEnvironment.LocalProcessorLoad;
			totalRemoteLoad += mediaEnvironment.RemoteProcessorLoad;
			loadChecks++;

			if (TimeToDowngrade() || lastRemoteSessions != mediaEnvironment.RemoteSessions)
			{
				// If it's been more than 5 seconds since the last time we checked, see if CPU utilization is too high,
				// and we need to downgrade.
				double averageLocalLoad = totalLocalLoad / loadChecks;
				double averageRemoteLoad = totalRemoteLoad / loadChecks;
				if (averageLocalLoad > MaxRecommendedLoad)
				{
					// If local CPU is too high, we need to fall back to the lowest quality available to us.
					// This will increase the bandwidth used a bit (i.e., it will switch from Speex to G711)
					// but will decrease the CPU required both locally and on remote machines.
					if (currentInstance != FallbackInstance)
					{
						SwitchToFallbackInstance(FallbackInstance);
						ClientLogger.Debug("Average local CPU load of {0:0.00} is too high; switching to fallback version of {1}; we'll check again in {2:0} seconds",
							averageLocalLoad, typeof(T).Name, MinimumTimeUntilUpgrade.TotalSeconds);
					}
				}
				else if (averageRemoteLoad > MaxRecommendedLoad)
				{
					// If the local CPU is OK but remote CPU sucks, then:
					// - if we're at a lower quality, switch to the higher quality only if enough time has passed.
					// - if we're at a higher quality, switch to the lower quality ASAP.
					if (currentInstance != RemoteFallbackInstance && (TimeToUpgrade() || currentInstance != FallbackInstance))
					{
						SwitchToFallbackInstance(RemoteFallbackInstance);
						ClientLogger.Debug("Average remote CPU load of {0:0.00} is too high; switching to remote fallback version of {1}; we'll check again in {2:0} seconds",
							averageRemoteLoad, typeof(T).Name, MinimumTimeUntilUpgrade.TotalSeconds);
					}
				}
				else if (TimeToUpgrade() || lastRemoteSessions != mediaEnvironment.RemoteSessions)
				{
					// If both local and remote CPU are OK, and if it's been more than 30 seconds since the last time we checked, 
					// see if both remote and local CPU utilization are low enough that we can actually upgrade.
					if (averageLocalLoad < MaxSafeLoad && averageRemoteLoad < MaxSafeLoad)
					{
						if (mediaEnvironment.RemoteSessions > 1 && currentInstance != ConferenceInstance)
						{
							ClientLogger.Debug("CPU seems low enough; switching to conference version of {0}", typeof(T).Name);
							currentInstance = ConferenceInstance;
						}
						else if (mediaEnvironment.RemoteSessions <= 1 && currentInstance != DirectInstance)
						{
							ClientLogger.Debug("CPU seems low enough; switching to direct version of {0}", typeof(T).Name);
							currentInstance = DirectInstance;
						}
					}
					ResetLoadChecks();
				}
				lastRemoteSessions = mediaEnvironment.RemoteSessions;
			}
			return currentInstance;
		}

		private bool TimeToDowngrade()
		{
			return lastInstanceCheck + MinimumTimeUntilDowngrade < mediaEnvironment.Now;
		}

		private bool TimeToUpgrade()
		{
			return lastInstanceCheck + MinimumTimeUntilUpgrade < mediaEnvironment.Now;
		}

		private void SwitchToFallbackInstance(T instance)
		{
			currentInstance = instance;
			ResetLoadChecks();

			// Every time we conclude that we need to downgrade, double the time until we check whether we can upgrade.
			// This is to minimize the scenario where mid-range CPU's can get caught flopping back-and-forth between
			// high and low quality every 30 seconds.
			// https://www.pivotaltracker.com/story/show/23085689
			MinimumTimeUntilUpgrade = TimeSpan.FromSeconds(MinimumTimeUntilUpgrade.TotalSeconds * 2);

		}

		private void ResetLoadChecks()
		{
			totalLocalLoad = 0;
			totalRemoteLoad = 0;
			loadChecks = 0;
			lastInstanceCheck = mediaEnvironment.Now;
		}

	}
}
