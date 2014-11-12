using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Alanta.Client.Common.Logging;
using Alanta.Client.Test.Media;
using Alanta.Client.Ui.Common;

namespace Alanta.Client.Media.Tests.Media
{
    public partial class AudioTimingPage : Page, IAudioFrameSource
    {
        public AudioTimingPage()
        {
            InitializeComponent();
        }

        private CaptureSource captureSource;
        private MediaElement mediaElement;
        private TestAudioStreamSource audioStreamSource;
        private TestTimingAudioSink audioSink;
        private const int reportingInterval = 50; // Report once every second
        private Oscillator oscillator;
		private readonly DispatcherTimer timer = new DispatcherTimer();
		private AudioFormat audioFormat = new AudioFormat(AudioConstants.WidebandSamplesPerSecond, AudioConstants.MillisecondsPerFrame);
        private byte[] lastFrame;

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeCaptureSource();
            oscillator = new Oscillator();
            oscillator.Frequency = 2000;
            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);
            sendPulse.IsEnabled = false;
            stopPulse.IsEnabled = false;
            pulseDelays = new List<double>();
        }

        private void enableMicrophone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (captureSource != null)
                {
                    if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
                    {
                        ClientLogger.Debug("AudioTimingPage CaptureSource started.");
                        captureSource.Start();
                        mediaElement.SetSource(audioStreamSource);
                        mediaElement.Play();
                        sendPulse.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void sendPulse_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            stopPulse.IsEnabled = true;
            sendPulse.IsEnabled = false;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            SendPulse();
        }

        private void SendPulse()
        {
			var shortSamples = new short[audioFormat.BytesPerFrame / 2];
            for (int i = 0; i < shortSamples.Length; i++)
            {
                shortSamples[i] = oscillator.GetNextSample();
            }
			var byteSamples = new byte[audioFormat.BytesPerFrame];
            Buffer.BlockCopy(shortSamples, 0, byteSamples, 0, byteSamples.Length);
            lastPulseSentAt = DateTime.Now;
            lastFrame = byteSamples;
        }

        private void stopPulse_Click(object sender, RoutedEventArgs e)
        {
            sendPulse.IsEnabled = true;
            timer.Stop();
        }

        private void InitializeCaptureSource()
        {
            if (captureSource != null)
            {
                return;
            }
            captureSource = new CaptureSource();
            captureSource.AudioCaptureDevice = CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
            mediaElement = new MediaElement();
            audioStreamSource = new TestAudioStreamSource(this);

            // Set the audio properties.
            if (captureSource.AudioCaptureDevice != null)
            {
                MediaDeviceConfig.SelectBestAudioFormat(captureSource.AudioCaptureDevice);
                if (captureSource.AudioCaptureDevice.DesiredFormat != null)
                {
					captureSource.AudioCaptureDevice.AudioFrameSize = audioFormat.MillisecondsPerFrame; // 20 milliseconds
					audioSink = new TestTimingAudioSink(captureSource, audioFormat);
					audioSink.FrameArrived += audioSink_FrameArrived;
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

        private DateTime firstFrameArrivedAt = DateTime.MinValue;
        private DateTime lastFrameArrivedAt = DateTime.MinValue;
        private DateTime lastPulseSentAt = DateTime.MinValue;
        private DateTime lastPulseArrivedAt = DateTime.MinValue;
        private int framesArrived = 0;
        private long totalVolume;
        private long recentVolume;
        private int totalMaxVolume;
        private int recentMaxVolume;
        private int pulsesReceived;
        private int totalPulseDelays;
        private List<double> pulseDelays;

        void audioSink_FrameArrived(object sender, Common.EventArgs<int, DateTime> e)
        {
            // Handle first-frame issues.
            if (firstFrameArrivedAt == DateTime.MinValue)
            {
                firstFrameArrivedAt = e.Value2;
                lastFrameArrivedAt = e.Value2;
            }

            // Handle volume issues
            totalVolume += e.Value1;
            recentVolume += e.Value1;
            if (e.Value1 > totalMaxVolume) totalMaxVolume = e.Value1;
            if (e.Value1 > recentMaxVolume) recentMaxVolume = e.Value1;

            // Check to see if a pulse has arrived.
            if (audioStreamSource.LastPulseSubmittedAt != DateTime.MinValue)
            {
                if (e.Value1 > 10000)
                {
                    lastPulseArrivedAt = DateTime.Now;
					var pulseDelay = (int)(lastPulseArrivedAt - audioStreamSource.LastPulseSubmittedAt).TotalMilliseconds;
                    if (pulseDelay < 1000)
                    {
                        ClientLogger.Debug("Pulse received.");
                        pulsesReceived++;
                        totalPulseDelays += pulseDelay;
						pulseDelays.Add(pulseDelay);
                        string stats = string.Format("Pulse Delay: {0}\r\n", pulseDelay);
                        Deployment.Current.Dispatcher.BeginInvoke(() => txtStats.Text += stats);
                    }
                    else
                    {
                        ClientLogger.Debug("Possible pulse received, but only after timeout.");
                    }
                    audioStreamSource.LastPulseSubmittedAt = DateTime.MinValue;
                }
            }

            // Check to see if we need to issue a report.
            if (++framesArrived % reportingInterval == 0)
            {
                double averageArrivalTimeTotal = (e.Value2 - firstFrameArrivedAt).TotalMilliseconds / (double)framesArrived;
                double averageArrivalTimeRecent = (e.Value2 - lastFrameArrivedAt).TotalMilliseconds / (double)reportingInterval;
                double averageVolumeTotal = totalVolume / framesArrived;
                double averageVolumeRecent = recentVolume / reportingInterval;
                double averagePulseDelay = (double)totalPulseDelays / (double)pulsesReceived;
                double stdDevPulseDelay = GetStandardDeviation(pulseDelays);
                string stats = string.Format("Total frames: {0}\r\n" +
                    "Average Arrival Time Total: {1}\r\n" +
                    "Average Arrival Time Recent: {2}\r\n" +
                    "Average Volume Total: {3}\r\n" +
                    "Average Volume Recent: {4}\r\n" + 
                    "Max Volume Ever: {5}\r\n" +
                    "Max Volume Recent: {6}\r\n" +
                    "Average Pulse Delay: {7}\r\n" +
                    "Average Submission Time Total: {8}\r\n" +
                    "Average Submission Time Recent: {9}\r\n" +
                    "Average DataLength Total: {10}\r\n" + 
                    "Average DataLength Recent: {11}\r\n" +
                    "Max Sample Length Total: {12}\r\n" +
                    "Min Sample Length Total: {13}\r\n" +
                    "Max Sample Length Recent: {14}\r\n" +
                    "Min Sample Length Recent: {15}\r\n",
                    framesArrived, averageArrivalTimeTotal, averageArrivalTimeRecent, averageVolumeTotal, averageVolumeRecent, totalMaxVolume, recentMaxVolume, averagePulseDelay,
                    audioSink.AverageSubmissionTimeTotal, audioSink.AverageSubmissionTimeRecent, audioSink.AverageSizeTotal, audioSink.AverageSizeRecent,
                    audioSink.totalMaxSampleLength, audioSink.totalMinSamplelength, audioSink.recentMaxSampleLength, audioSink.recentMinSampleLength);
                audioSink.ResetRecentCounters();
                lastFrameArrivedAt = e.Value2;
                recentVolume = 0;
                recentMaxVolume = 0;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        lblAverageVolume.Content = averageVolumeRecent;
                        lblMaxVolume.Content = totalMaxVolume;
                        lblAveragePulseDelay.Content = averagePulseDelay;
                        lblStdDevPulseDelay.Content = stdDevPulseDelay;
                        lblMaxPulseDelay.Content = pulseDelays.Max();
                        lblMinPulseDelay.Content = pulseDelays.Min();
                    });
                ClientLogger.Debug(stats);
            }
        }

        private static double GetStandardDeviation(List<double> doubleList)
        {
            double average = doubleList.Average();
            double sumOfDerivation = doubleList.Sum(value => (value)*(value));
            double sumOfDerivationAverage = sumOfDerivation / doubleList.Count;
            return Math.Sqrt(sumOfDerivationAverage - (average * average));
        }

        public byte[] GetNextFrame()
        {
            byte[] frame = lastFrame;
            lastFrame = null;
            return frame;
        }
    }
}
