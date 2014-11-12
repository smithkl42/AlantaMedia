using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Media.Dsp.Speex;
using Alanta.Client.Test.Media;
using Alanta.Client.Ui.Common;

namespace Alanta.Client.Media.Tests.Media
{
    public partial class EchoCancellationTest : Page, IAudioFrameSource
    {
        #region Constructors and Initializers
        public EchoCancellationTest()
        {
            InitializeComponent();
            recordedAudio = new List<byte[]>();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            btnRecord.IsEnabled = false;
            btnPlay.IsEnabled = false;
            InitializeCaptureSource();
        }
        #endregion

        #region Fields and Properties
        private CaptureSource captureSource;
        private MediaElement mediaElement;
        private TestAudioStreamSource audioStreamSource;
        private TestAudioSinkAdapter audioSink;
        private EchoCancelFilter echoCanceller;
        private readonly List<byte[]> recordedAudio;
        private int playedFrameIndex;
        private int framePlaybackInterval;
        private int expectedLatency;
        private int filterLength;
        private Func<byte[]> playbackFunction;
        #endregion

        #region Event Handlers

        private void btnEnableMicrophone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (captureSource != null)
                {
                    if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
                    {
                        ClientLogger.Debug("AudioTimingPage CaptureSource started.");
                        btnRecord.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (btnRecord.Content.ToString() == "Start")
            {
                btnPlay.IsEnabled = false;
                btnRecord.Content = "Stop";
                recordedAudio.Clear();
                captureSource.Start();
            }
            else
            {
                captureSource.Stop();
                btnRecord.Content = "Start";
                btnPlay.IsEnabled = true;
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (btnPlay.Content.ToString() == "Start")
            {
                try
                {
                    // Setup the parameters.
                    playedFrameIndex = 0;
                    int playbackDelay = Int32.Parse(txtPlaybackDelay.Text);
                    framePlaybackInterval = playbackDelay / 20;
                    expectedLatency = Int32.Parse(txtExpectedLatency.Text);
                    filterLength = Int32.Parse(txtTailSize.Text);

                    IAudioFilter playedResampler;
                    IAudioFilter recordedResampler;

                    // Decide whether to synchronize the audio or not.
                    if (chkSynchronize.IsChecked == true)
                    {
                        playedResampler = new ResampleFilter(AudioFormat.Default, AudioFormat.Default);

                    	recordedResampler = new ResampleFilter(AudioFormat.Default, AudioFormat.Default);
                    }
                    else
                    {
                        playedResampler = new NullAudioFilter(AudioFormat.Default.BytesPerFrame);
                        recordedResampler = new NullAudioFilter(AudioFormat.Default.BytesPerFrame);
                    }

                    // Initialize the echo canceller
                    playedResampler.InstanceName = "EchoCanceller_played";
                    recordedResampler.InstanceName = "EchoCanceller_recorded";
                    echoCanceller = new SpeexEchoCancelFilter(expectedLatency, filterLength, AudioFormat.Default, AudioFormat.Default, playedResampler, recordedResampler);

                    if (chkPlaySilently.IsChecked == true)
                    {
                        PerformEchoCancellation();
                        return;
                    }
                    if (chkPlayWithAEC.IsChecked == true)
                    {
                        playbackFunction = GetNextAECFrame;
                    }
                    else
                    {
                        playbackFunction = GetNextRawFrame;
                    }

                    // Start playing the audio.
                    mediaElement.Play();
                    btnPlay.Content = "Stop";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                btnPlay.Content = "Start";
                mediaElement.Stop();
            }
        }

        private void audioSink_FrameArrived(object sender, EventArgs<byte[]> e)
        {
            inputAudioVisualizer.RenderVisualization(e.Value);
            recordedAudio.Add(e.Value);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (recordedAudio.Count == 0)
                {
                    MessageBox.Show("No audio has been recorded.");
                    return;
                }

                var dlg = new SaveFileDialog();
                dlg.Filter = "PCM Files (*.pcm)|*.pcm|All Files (*.*)|*.*";
                dlg.FilterIndex = 1;
                var userClickedOk = dlg.ShowDialog();
                if (userClickedOk == true)
                {
                    using (var fs = dlg.OpenFile())
                    {
                        foreach (byte[] frame in recordedAudio)
                        {
                            fs.Write(frame, 0, frame.Length);
                        }
                    }
                    txtStatus.Text = "File saved.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnSaveShifted_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (recordedAudio.Count == 0)
                {
                    MessageBox.Show("No audio has been recorded.");
                    return;
                }

                var dlg = new SaveFileDialog();
                dlg.Filter = "PCM Files (*.pcm)|*.pcm|All Files (*.*)|*.*";
                dlg.FilterIndex = 1;
                var userClickedOk = dlg.ShowDialog();
                if (userClickedOk == true)
                {
                    using (var fs = dlg.OpenFile())
                    {
                        int playbackDelay = Int32.Parse(txtPlaybackDelay.Text);
                        framePlaybackInterval = playbackDelay / 20;
                        var emptyFrame = new byte[AudioFormat.Default.BytesPerFrame];
                        for (int i = 0; i < framePlaybackInterval; i++)
                        {
                            fs.Write(emptyFrame, 0, emptyFrame.Length);
                        }
                        foreach (byte[] frame in recordedAudio)
                        {
                            fs.Write(frame, 0, frame.Length);
                        }
                    }
                    txtStatus.Text = "File saved.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnSelectPlaybackFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "PCM Files (*.pcm)|*.pcm|All Files (*.*)|*.*";
                dlg.FilterIndex = 1;
                var userClickedOk = dlg.ShowDialog();
                if (userClickedOk == true)
                {
                    using (Stream fs = dlg.File.OpenRead())
                    {
                        recordedAudio.Clear();
                        while (fs.Position < fs.Length - AudioFormat.Default.BytesPerFrame)
                        {
                            var frame = new byte[AudioFormat.Default.BytesPerFrame];
                            fs.Read(frame, 0, frame.Length);
                            recordedAudio.Add(frame);
                        }
                        txtStatus.Text = "File read.";
                        btnPlay.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        #endregion

        #region Methods
        private void InitializeCaptureSource()
        {
            if (captureSource == null)
            {
                mediaElement = new MediaElement();
                audioStreamSource = new TestAudioStreamSource(this);
                mediaElement.SetSource(audioStreamSource);

                // Set the audio properties.
                captureSource = new CaptureSource();
                captureSource.AudioCaptureDevice = CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
                if (captureSource.AudioCaptureDevice != null)
                {
                    MediaDeviceConfig.SelectBestAudioFormat(captureSource.AudioCaptureDevice);
                    if (captureSource.AudioCaptureDevice.DesiredFormat != null)
                    {
                        captureSource.AudioCaptureDevice.AudioFrameSize = AudioFormat.Default.MillisecondsPerFrame; // 20 milliseconds
                        audioSink = new TestAudioSinkAdapter(captureSource);
                        audioSink.ProcessedFrameAvailable += audioSink_FrameArrived;
                        ClientLogger.Debug("CaptureSource initialized.");
                    }
                    else
                    {
                        ClientLogger.Debug("No suitable audio format was found.");
                    }
                }
                else
                {
                    // Do something more here eventually, once we figure out what the user experience should be.
                    ClientLogger.Debug("No audio capture device was found.");
                }
            }
        }

        private void PerformEchoCancellation()
        {
            var start = DateTime.Now;
            while (playedFrameIndex < recordedAudio.Count)
            {
                GetNextAECFrame();
                playedFrameIndex++;
            }
            double elapsed = (DateTime.Now - start).TotalMilliseconds;
            double average = elapsed / recordedAudio.Count;
            string message = string.Format("Elapsed (ms)={0}; average ms/frame={1:f}", elapsed, average);
            ClientLogger.Debug(message);
            txtStatus.Text = message;
        }

        public byte[] GetNextFrame()
        {
            if (playedFrameIndex < recordedAudio.Count)
            {
                var frame = playbackFunction();
                outputAudioVisualizer.RenderVisualization(frame);
                playedFrameIndex++;
                return frame;
            }
        	Dispatcher.BeginInvoke(() =>
        	{
        		mediaElement.Stop();
        		btnPlay.Content = "Start";
        	});
        	return null;
        }

        private byte[] GetNextRawFrame()
        {
            var playedFrame = recordedAudio[playedFrameIndex];
            outputAudioVisualizer.RenderVisualization(playedFrame);
            return playedFrame;
        }

        private byte[] GetNextAECFrame()
        {
            // Get the data representing the audio we're pretending we're about to "play" and note it as such on the echo canceller.
            var playedFrame = recordedAudio[playedFrameIndex];
            echoCanceller.RegisterFramePlayed(playedFrame);

            // Get the data representing the audio we just pretended to recorded 
            // (which is roughly equal to the frame we pretended to play ~10 frames back).
            int recordedFrameIndex = playedFrameIndex - framePlaybackInterval;
            byte[] recordedFrame;
            if (recordedFrameIndex >= 0 && recordedFrameIndex < recordedAudio.Count)
            {
                recordedFrame = recordedAudio[recordedFrameIndex];
            }
            else
            {
                recordedFrame = new byte[playedFrame.Length];
            }

            // Cancel the echo onto a new frame.
            echoCanceller.Write(recordedFrame);
            var cancelledFrame = new byte[playedFrame.Length];
            bool moreSamples;

            // Even if we don't play all the samples we get, retrieve them all,
            // so that the various buffers stay in sync.
            do
            {
                echoCanceller.Read(cancelledFrame, out moreSamples);
            }
            while (moreSamples);

            // Play the final frame retrieved.
            return cancelledFrame;
        }

        private byte[] MixAudioFrames(byte[] frame1, byte[] frame2)
        {
            var sFrame1 = new short[frame1.Length / sizeof(short)];
            var sFrame2 = new short[frame2.Length / sizeof(short)];
            Buffer.BlockCopy(frame1, 0, sFrame1, 0, frame1.Length);
            Buffer.BlockCopy(frame2, 0, sFrame2, 0, frame2.Length);
            var sMixed = new short[sFrame1.Length];
            for (int i = 0; i < sMixed.Length; i++)
            {
                int m = sFrame1[i] + sFrame2[i];
                if (m > short.MaxValue) m = short.MaxValue;
                else if (m < short.MinValue) m = short.MinValue;
                sMixed[i] = (short)m;
            }
            var mixed = new byte[frame1.Length];
            Buffer.BlockCopy(sMixed, 0, mixed, 0, mixed.Length);
            return mixed;
        }
        #endregion

    }
}
