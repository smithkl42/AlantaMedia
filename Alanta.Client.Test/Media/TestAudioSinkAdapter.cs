using System;
using System.Windows.Media;
using Alanta.Client.Common;
using Alanta.Client.Media;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media
{
    public class TestAudioSinkAdapter : AudioSinkAdapter
    {
        public TestAudioSinkAdapter(CaptureSource captureSource, IAudioController audioController = null)
            : base(captureSource, audioController, MediaConfig.Default, new TestMediaEnvironment(), AudioFormat.Default)
        {
            _audioController = audioController;
        }

        public event EventHandler<EventArgs<byte[]>> RawFrameAvailable;
        public event EventHandler<EventArgs<byte[]>> ProcessedFrameAvailable;
        private readonly IAudioController _audioController;

        protected override void OnSamples(long sampleTime, long sampleDuration, byte[] sampleData)
        {
            if (RawFrameAvailable != null)
            {
                RawFrameAvailable(this, new EventArgs<byte[]>(sampleData));
            }
            base.OnSamples(sampleTime, sampleDuration, sampleData);
        }

        protected override void SubmitFrame(AudioContext ctx, byte[] frame)
        {
            // This is all kinda hacky, but oh well.
            if (ProcessedFrameAvailable != null)
            {
                ProcessedFrameAvailable(this, new EventArgs<byte[]>(frame));
            }
            if (_audioController != null)
            {
                _audioController.SubmitRecordedFrame(ctx, frame);
            }
        }
    }
}
