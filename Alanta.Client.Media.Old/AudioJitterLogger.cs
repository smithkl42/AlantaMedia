using System.Diagnostics;

namespace Alanta.Client.Media
{
    public class AudioJitterLogger
    {
        public AudioJitterLogger(AudioJitter audioJitter)
        {
            this.audioJitter = audioJitter;
        }

        private const int ReportingInterval = 1000;
        private AudioJitter audioJitter;
        private bool recordReads;

        private long totalOffsetCalculations;
        private long offsetTooOld;
        private long offsetTooNew;
        private long totalReads;
        private long nullReads;
        private long nullReadsRecent;
        private long emptyReads;
        private long emptyReadsRecent;
        private long totalWrites;
        private long emptyWrites;
        private long emptyWritesRecent;
        private int mostRecentOffset;
        private int originalOffset;
        private bool offsetCalculated;

        [Conditional("DEBUG")]
        public void LogRead(AudioJitter.JitterBuffer buffer)
        {
            if (buffer.Received)
            {
                recordReads = true;
                if (IsEmpty(buffer.Samples))
                {
                    emptyReads++;
                    emptyReadsRecent++;
                }
            }
            else
            {
                if (recordReads)
                {
                    nullReads++;
                    nullReadsRecent++;
                }
            }
            if (recordReads && ++totalReads % 1000 == 0)
            {
                if (nullReadsRecent > 0 || emptyReadsRecent > 0)
                {
                    MediaLogger.LogDebugMessage("Jitter reads={0}, recent null={1}, recent empty={2}, null={3}, % null={4:0%}, empty={5}, % empty={6:0%}",
                        totalReads, nullReadsRecent, emptyReadsRecent, nullReads, (float)nullReads / (float)totalReads, emptyReads, (float)emptyReads / (float)totalReads);
                }
                nullReadsRecent = 0;
                emptyReadsRecent = 0;
            }

        }

        [Conditional("DEBUG")]
        public void LogWrite(byte[] buffer)
        {
            if (IsEmpty(buffer))
            {
                emptyWrites++;
                emptyWritesRecent++;
            }
            if (++totalWrites % 1000 == 0)
            {
                if (emptyWritesRecent > 0)
                {
                    MediaLogger.LogDebugMessage("Jitter writes={0}, recent empty writes = {1}, empty writes={2}, % empty={3:0%}",
                        totalWrites, emptyWritesRecent, emptyWrites, (float)emptyWrites / (float)totalWrites);
                }
                emptyWritesRecent = 0;
            }
        }

        [Conditional("DEBUG")]
        public void LogBufferOffsetCalculation(int offset, int diff, int buffers)
        {
            if (offset + diff < 0)
            {
                offsetTooOld++;
            }
            else if (offset + diff >= buffers)
            {
                offsetTooNew++;
            }
            if (++totalOffsetCalculations % ReportingInterval == 0 && (offsetTooOld > 0 || offsetTooNew > 0))
            {
                MediaLogger.LogDebugMessage("Total offset calculations = {0}, recent offsets too old = {1}, recent offsets too new = {2}", totalOffsetCalculations, offsetTooOld, offsetTooNew);
                offsetTooNew = 0;
                offsetTooOld = 0;
            }
        }

        [Conditional("DEBUG")]
        public void LogOffsetRecalculation(int offset)
        {
            if (!offsetCalculated)
            {
                offsetCalculated = true;
                originalOffset = offset;
                mostRecentOffset = offset;
            }
            else
            {
                MediaLogger.LogDebugMessage("The jitter buffer offset was recalculated: difference={2}, total difference={3}", mostRecentOffset, offset, offset - mostRecentOffset, offset - originalOffset);
                mostRecentOffset = offset;
            }
        }

        private bool IsEmpty(byte[] sample)
        {
            // Figure out whether the block being written appears to be empty.
            bool empty = true;
            for (int i = 0; i < sample.Length; i += 25)
            {
                if (sample[i] != 0)
                {
                    empty = false;
                    break;
                }
            }
            return empty;
        }
    }
}
