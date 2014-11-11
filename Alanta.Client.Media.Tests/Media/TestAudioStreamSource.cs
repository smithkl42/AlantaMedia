using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using Alanta.Client.Media;
using Alanta.Client.Common.Logging;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media
{
    public interface IAudioFrameSource
    {
        byte[] GetNextFrame();
    }

    public class TestAudioStreamSource : MediaStreamSource
    {
        #region Constructors
        public TestAudioStreamSource(IAudioFrameSource frameSource)
        {
            waveFormat = new WaveFormat();
            waveFormat.FormatTag = WaveFormatType.Pcm;
            waveFormat.BitsPerSample = AudioConstants.BitsPerSample;
            waveFormat.Channels = AudioConstants.Channels;
            waveFormat.SamplesPerSec = AudioFormat.Default.SamplesPerSecond;
            waveFormat.AvgBytesPerSec = (AudioConstants.BitsPerSample / 8) * AudioConstants.Channels * AudioFormat.Default.SamplesPerSecond;
            waveFormat.Ext = null;
            waveFormat.BlockAlign = AudioConstants.Channels * (AudioConstants.BitsPerSample / 8);
            waveFormat.Size = 0;
            AudioBufferLength = AudioFormat.Default.MillisecondsPerFrame; //  *2; // 15 is the smallest buffer length that it will accept.
            LastPulseSubmittedAt = DateTime.MinValue;
            this.frameSource = frameSource;
        }
        #endregion

        #region Fields and Properties
        private AudioMediaStreamSourceLogger logger = new AudioMediaStreamSourceLogger();
        private readonly WaveFormat waveFormat;
        private MediaStreamDescription mediaStreamDescription;
        private readonly Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>(); // Not used so empty.
        private DateTime startTime;
        private byte[] emptyFrame = new byte[AudioFormat.Default.MillisecondsPerFrame];
        private readonly IAudioFrameSource frameSource;
        public DateTime LastPulseSubmittedAt { get; set; }

        #endregion

        #region Methods
        protected override void OpenMediaAsync()
        {
            // Define the available streams.
            var streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = waveFormat.ToHexString();
            mediaStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);
            var availableStreams = new List<MediaStreamDescription>();
            availableStreams.Add(mediaStreamDescription);

            // Define pieces that are common to all streams.
            var sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            sourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.Zero.Ticks.ToString(CultureInfo.InvariantCulture);   // 0 = Indefinite
            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = "0"; // 0 = False

            // Start the timer.
            startTime = DateTime.Now;

            // Tell Silverlight we're ready to play.
            ReportOpenMediaCompleted(sourceAttributes, availableStreams);
        }

        protected override void CloseMedia()
        {
            // Nothing for right now.   
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }

        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            try
            {
                MemoryStream rawSampleStream;
                byte[] rawSample;
                if ((rawSample = frameSource.GetNextFrame()) != null)
                {
                    rawSampleStream = new MemoryStream(rawSample);
                    LastPulseSubmittedAt = DateTime.Now;
                }
                else
                {
                    rawSampleStream = new MemoryStream(emptyFrame);
                }

                MediaStreamSample sample = new MediaStreamSample(
                    mediaStreamDescription,
                    rawSampleStream,
                    0,
                    rawSampleStream.Length,
                    (DateTime.Now - startTime).Ticks,
                    emptySampleDict);
                ReportGetSampleCompleted(sample);
            }
            catch (Exception ex)
            {
                ClientLogger.Debug(ex.ToString());
            }
        }

        protected override void SeekAsync(long seekToTime)
        {
            ReportSeekCompleted(seekToTime);
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
