using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
    public class MediaControllerLogger
    {
        private readonly object _packetSentLock = new object();
        private int _audioFramesPlayed;
        private int _audioFramesPlayedRecent;
        protected int _audioFramesRecorded;
        protected int _audioFramesRecordedRecent;
        protected long _audioFramesSet;
        protected int _audioFramesSilent;
        protected int _audioFramesSilentRecent;
        private double _averageFramePlayedTime;
        private double _averageFrameRecordedTime;
        protected Counter _duplicateSequenceNumbers;
        protected DateTime _firstAudioFrameSet = DateTime.MinValue;
        protected bool _firstFramePlayed;
        protected bool _firstPacketSent;
        protected DateTime _firstPlayedMessage;
        protected DateTime _firstTransmissionMessage;
        protected DateTime _lastAudioFrameSet;
        private DateTime _lastPlayedLog;
        protected DateTime _lastPlayedMessage;
        private DateTime _lastRecordedLog;
        private int _lastSequenceNumber;
        protected DateTime _lastTransmissionMessage;
        protected DateTime _lastVideoResetTime = DateTime.MinValue;
        protected Counter _playingRateCounter;
        private int _recentPacketsDuplicated;
        private int _recentPacketsOutOfOrder;
        protected long _recentVideoBytesSent;
        protected long _recentVideoPacketsSent;
        protected Counter _recordingRateCounter;
        private int _totalFramesReceived;
        private int _totalPacketsDuplicated;
        private int _totalPacketsOutOfOrder;
        protected long _videoBytesSent;
        protected Counter _videoKbpsCounter;
        protected long _videoPacketsSent;
        protected IVideoQualityController _videoQualityController;
        protected DateTime _videoStartTime;
        protected Dictionary<ushort, long> _videoThreadDataNotFoundDictionary = new Dictionary<ushort, long>();

        public MediaControllerLogger(IVideoQualityController videoQualityController,
            MediaStatistics mediaStatistics = null)
        {
            _videoQualityController = videoQualityController;
            if (mediaStatistics != null)
            {
                _videoKbpsCounter = mediaStatistics.RegisterCounter("Video:KbpsEncoded");
                _videoKbpsCounter.AxisMinimum = 0;
                _duplicateSequenceNumbers = mediaStatistics.RegisterCounter("Audio:Duplicate SequenceNumbers");
                _duplicateSequenceNumbers.IsActive = false;
                _recordingRateCounter = mediaStatistics.RegisterCounter("Audio:RecordingRate");
                _recordingRateCounter.IsActive = true;
                _recordingRateCounter.MinimumDelta = 2;
                _playingRateCounter = mediaStatistics.RegisterCounter("Audio:PlayingRate");
                _playingRateCounter.MinimumDelta = 2;
                _playingRateCounter.IsActive = true;
            }
        }

        public double AverageFramePlayedTime
        {
            get { return _averageFramePlayedTime; }
            set
            {
                if (_averageFramePlayedTime == value)
                {
                    return;
                }
                _averageFramePlayedTime = value;
            }
        }

        public double AverageFrameRecordedTime
        {
            get { return _averageFrameRecordedTime; }
            set
            {
                if (_averageFrameRecordedTime == value)
                {
                    return;
                }
                _averageFrameRecordedTime = value;
            }
        }

        public long AudioFramesSet
        {
            get { return _audioFramesSet; }
        }

        public void LogAudioFramePlayed()
        {
            var now = DateTime.Now;
            if (!_firstFramePlayed)
            {
                _firstFramePlayed = true;
                _firstPlayedMessage = now;
                _lastPlayedMessage = _firstPlayedMessage;
                _lastPlayedLog = now;
            }

            const int interval = 2000;

            _audioFramesPlayed++;
            _audioFramesPlayedRecent++;

            var recentElapsed = now - _lastPlayedMessage;
            if (recentElapsed.TotalMilliseconds < interval) return;

            var totalElapsed = now - _firstPlayedMessage;
            AverageFramePlayedTime = recentElapsed.TotalMilliseconds/_audioFramesPlayedRecent;
            var averageTotalTime = totalElapsed.TotalMilliseconds/_audioFramesPlayed;
            _playingRateCounter.Update(AverageFramePlayedTime);
            if ((now - _lastPlayedLog).TotalSeconds > 10)
            {
                ClientLogger.Debug(
                    "MediaController: Total audio frames played={0}; recentElapsed={1}; time/entry={2:f3}; recent time/entry={3:f3}",
                    _audioFramesPlayed, recentElapsed.TotalMilliseconds, averageTotalTime, AverageFramePlayedTime);
                _lastPlayedLog = now;
            }
            _audioFramesPlayedRecent = 0;
            _lastPlayedMessage = now;
        }

        public void LogAudioFrameTransmitted(bool isSilent)
        {
            var now = DateTime.Now;
            if (!_firstPacketSent)
            {
                _lastTransmissionMessage = now;
                _firstTransmissionMessage = _lastTransmissionMessage;
                _lastRecordedLog = now;
                _firstPacketSent = true;
            }
            if (isSilent)
            {
                _audioFramesSilent++;
                _audioFramesSilentRecent++;
            }

            const int interval = 2000;

            _audioFramesRecorded++;
            _audioFramesRecordedRecent++;

            var recentElapsed = now - _lastTransmissionMessage;
            if (recentElapsed.TotalMilliseconds < interval) return;

            var totalElapsed = now - _firstTransmissionMessage;
            AverageFrameRecordedTime = recentElapsed.TotalMilliseconds/_audioFramesRecordedRecent;
            var averageTotalTime = totalElapsed.TotalMilliseconds/_audioFramesRecorded;
            _recordingRateCounter.Update(AverageFrameRecordedTime);
            if ((now - _lastRecordedLog).TotalSeconds > 10)
            {
                ClientLogger.Debug(
                    "MediaController: Total audio frames sent={0}; recentElapsed={1}; time/entry={2:f3}; recent time/entry={3:f3}",
                    _audioFramesRecorded, recentElapsed.TotalMilliseconds, averageTotalTime, AverageFrameRecordedTime);
                _lastRecordedLog = now;
            }
            _lastTransmissionMessage = now;
            _audioFramesSilentRecent = 0;
            _audioFramesRecordedRecent = 0;
        }

        public void LogVideoPacketSent(ByteStream packetBuffer)
        {
            // Calculate packets sent, etc.
            var now = DateTime.Now;
            if (_videoStartTime == DateTime.MinValue)
            {
                _videoStartTime = now;
                _lastVideoResetTime = now;
            }
            _videoBytesSent += packetBuffer.DataLength;
            _recentVideoBytesSent += packetBuffer.DataLength;
            _recentVideoPacketsSent++;
            _videoPacketsSent++;
            lock (_packetSentLock)
            {
                var msSinceLastReset = (now - _lastVideoResetTime).TotalMilliseconds;
                if (msSinceLastReset > 2000)
                {
                    _lastVideoResetTime = now;
                    var recentBitsPerSecond = (float) ((_recentVideoBytesSent/(msSinceLastReset/1000))*8)/1024;
                    if (_videoKbpsCounter != null)
                    {
                        _videoKbpsCounter.Update(recentBitsPerSecond);
                    }
                    _recentVideoPacketsSent = 0;
                    _recentVideoBytesSent = 0;
                }
            }
        }

        public void LogVideoThreadDataNotFound(ushort ssrcId)
        {
            long count;
            if (_videoThreadDataNotFoundDictionary.TryGetValue(ssrcId, out count))
            {
                _videoThreadDataNotFoundDictionary[ssrcId] = ++count;
                if (count%100 == 0)
                {
                    ClientLogger.Debug("VideoThreadData for {0} has not been found {1} times.", ssrcId, count);
                }
            }
            else
            {
                _videoThreadDataNotFoundDictionary[ssrcId] = 1;
            }
        }

        public void LogAudioFrameSet()
        {
            if (_firstAudioFrameSet == DateTime.MinValue)
            {
                _firstAudioFrameSet = DateTime.Now;
                _lastAudioFrameSet = _firstAudioFrameSet;
            }
            if (++_audioFramesSet%1000 == 0)
            {
                var recentElapsed = DateTime.Now - _lastAudioFrameSet;
                var totalElapsed = DateTime.Now - _firstAudioFrameSet;
                var averageRecentTime = recentElapsed.TotalMilliseconds/1000d;
                var averageTotalTime = totalElapsed.TotalMilliseconds/_audioFramesSet;
                ClientLogger.Debug(
                    "Total audio frames set={0}; recentElapsed={1}; time/entry={2:f3}; recent time/entry={3:f3}",
                    _audioFramesSet, recentElapsed.TotalMilliseconds, averageTotalTime, averageRecentTime);
                _lastAudioFrameSet = DateTime.Now;

                // If we're not getting frames fast enough, this may be an indication that the CPU is running hot, 
                // and we need to back down the video quality.
                if (_videoQualityController == null)
                {
                    return;
                }
                _videoQualityController.LogGlitch((int) (averageRecentTime - 30)/10);
            }
        }

        [Conditional("DEBUG")]
        public void LogAudioInputQueueStatus(Queue<ByteStream> queue)
        {
            if (queue.Count > 5)
            {
                ClientLogger.Debug(
                    "The audio input queue has {0} members; this is larger than we should probably have.", queue.Count);
            }
        }

        [Conditional("DEBUG")]
        internal void LogAudioFrameReceived(IMediaPacket packet)
        {
            if (packet.SequenceNumber == _lastSequenceNumber)
            {
                _recentPacketsDuplicated++;
                if (++_totalPacketsDuplicated%10 == 0)
                {
                    ClientLogger.Debug("{0} packets duplicated. Bizarre.", _totalPacketsDuplicated);
                }
            }
            else if (packet.SequenceNumber < _lastSequenceNumber && _lastSequenceNumber < ushort.MaxValue)
            {
                _recentPacketsOutOfOrder++;
                if (++_totalPacketsOutOfOrder%10 == 0)
                {
                    ClientLogger.Debug("{0} packets received out of order.", _totalPacketsOutOfOrder);
                }
            }
            _lastSequenceNumber = packet.SequenceNumber;

            if (++_totalFramesReceived%200 == 0)
            {
                _duplicateSequenceNumbers.Update(_recentPacketsDuplicated);
                _recentPacketsDuplicated = 0;
                _recentPacketsOutOfOrder = 0;
            }
        }
    }
}