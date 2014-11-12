using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Test.Media;
using Alanta.Client.Ui.Common;

namespace Alanta.Client.Media.Tests.Media
{
	public partial class VadTestPage : Page
	{
		// SpeexPreprocessFilter speexPreprocessor;
		private VoiceActivityDetector voiceActivityDetector;
		CaptureSource captureSource;
		TestAudioSinkAdapter audioSink;
		const int sampleRate = 22050;

		public VadTestPage()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			listBoxAudioSources.ItemsSource = CaptureDeviceConfiguration.GetAvailableAudioCaptureDevices();
			if (listBoxAudioSources.Items.Count != 0) listBoxAudioSources.SelectedIndex = 0;
		}

		private void buttonStartTest_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// speexPreprocessor = new SpeexPreprocessFilter(AudioConstants.SamplesPerFrame, sampleRate, new MediaConfig(), null, "VAD test page Speex Preprocessor");
				voiceActivityDetector = new VoiceActivityDetector(AudioFormat.Default, VoiceActivityDetector.Aggressiveness.Normal);

				// DataContext = voiceActivityDetector. speexPreprocessor.GetVadViewModel(this.Dispatcher);
				InitializeCaptureSource();
				//buttonStartTest.IsEnabled = false;
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString());
				MessageBox.Show(ex.ToString());
		}
		}

		private void InitializeCaptureSource()
		{
			if (captureSource != null)
			{
				captureSource.Stop();
			}
			captureSource = new CaptureSource();
			captureSource.AudioCaptureDevice = (AudioCaptureDevice)listBoxAudioSources.SelectedItem;

			MediaDeviceConfig.SelectBestAudioFormat(captureSource.AudioCaptureDevice);

			captureSource.AudioCaptureDevice.DesiredFormat = captureSource.AudioCaptureDevice.SupportedFormats
				.First(format => format.BitsPerSample == AudioConstants.BitsPerSample &&
											format.WaveFormat == WaveFormatType.Pcm &&
											format.Channels == 1 &&
			    format.SamplesPerSecond == sampleRate);
			captureSource.AudioCaptureDevice.AudioFrameSize = AudioFormat.Default.MillisecondsPerFrame; // 20 milliseconds

			audioSink = new TestAudioSinkAdapter(captureSource, new NullAudioController());
			audioSink.RawFrameAvailable += audioSink_RawFrameAvailable;
			audioSink.ProcessedFrameAvailable += audioSink_FrameArrived;

			ClientLogger.Debug("Checking device access.");
			if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
			{
				savedFramesForDebug = new List<byte[]>();
				captureSource.Start();
				ClientLogger.Debug("CaptureSource started.");					
			}

		}

		readonly short[] buffer = new short[AudioFormat.Default.SamplesPerFrame];
		int bufferPos = 0;
		void audioSink_RawFrameAvailable(object sender, Common.EventArgs<byte[]> e)
		{
			//todo: frame length is invalid. must equal to 20ms
			byte[] frame = e.Value;
			var src = new short[frame.Length / sizeof(short)];
			Buffer.BlockCopy(frame, 0, src, 0, frame.Length);

			int srcOffset = 0;

		_again:
			int srcAvailable = src.Length - srcOffset;
			int dstRemaining = buffer.Length - bufferPos;

			int cb = Math.Min(srcAvailable, dstRemaining);
			Array.Copy(src, srcOffset, buffer, bufferPos, cb);
			srcOffset += cb;
			bufferPos += cb;

			if (bufferPos == buffer.Length)
			{
				voiceActivityDetector.WebRtcVad_CalcVad16khz(buffer, buffer.Length);
				bufferPos = 0;
				goto _again;
			}


			SaveSamplesForDebug(frame);
		}

		private static void audioSink_FrameArrived(object sender, Common.EventArgs<byte[]> e)
		{
		}

		List<byte[]> savedFramesForDebug;
		void SaveSamplesForDebug(byte[] frame)
		{
			lock (savedFramesForDebug)
			{
				savedFramesForDebug.Add(frame);
			}
		}

		private void buttonSaveDump_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog() { Filter = "Raw audio files|*.raw" };

			if (dlg.ShowDialog() == true)
			{
				using (var writer = new BinaryWriter(dlg.OpenFile()))
				{
					lock (savedFramesForDebug)
					{
						foreach (var frame in savedFramesForDebug)
							writer.Write(frame);
					}
				}
			}
		}

		//public class ChartElement
		//{
		//    public double Time { get; set; }
		//    public double Value { get; set; }
		//}
		//void DisplayWaveDataOnChart()
		//{
		//    var elements = new List<ChartElement>();
		//    var rnd = new Random();
		//    for (int i = 0; i < 1000; i++)
		//        elements.Add(new ChartElement() { Time = i, Value = rnd.NextDouble() });
		//    waveDataChart.DataContext = elements;
		//}

		//private void buttonDebugDump_Click(object sender, RoutedEventArgs e)
		//{
		//    DisplayWaveDataOnChart();
		//}
	}
}
