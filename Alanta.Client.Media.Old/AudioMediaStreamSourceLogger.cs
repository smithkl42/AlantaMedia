using System;
using System.Diagnostics;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
    public class AudioMediaStreamSourceLogger
    {
    	protected DateTime firstSampleRequested = DateTime.MinValue;
        protected DateTime lastSampleRequested = DateTime.MinValue;
        protected long samplesRequested;

        [Conditional("DEBUG")]
        public void LogSampleRequested()
        {
            if (firstSampleRequested == DateTime.MinValue)
            {
                firstSampleRequested = DateTime.Now;
                lastSampleRequested = firstSampleRequested;
            }

            if (++samplesRequested % 1000 == 0)
            {
                TimeSpan recentElapsed = DateTime.Now - lastSampleRequested;
                TimeSpan totalElapsed = DateTime.Now - firstSampleRequested;
                double averageRecentTime = recentElapsed.TotalMilliseconds / 1000d;
                double averageTotalTime = totalElapsed.TotalMilliseconds / samplesRequested;
                ClientLogger.Debug("Total audio samples requested={0}; recentElapsed={1}; time/frame={2:f3}; recent time/frame={3:f3}",
                    samplesRequested, recentElapsed.TotalMilliseconds, averageTotalTime, averageRecentTime);
                lastSampleRequested = DateTime.Now;
            }
        }
    }
}
