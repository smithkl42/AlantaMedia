using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.UI.Common.Classes;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media
{
	public partial class AudioLoopbackTest : Page, IAudioFrameSource
	{
		public AudioLoopbackTest()
		{
			InitializeComponent();
		}

		private CaptureSource _captureSource;
		private MediaElement _mediaElement;
		private TestAudioStreamSource _audioStreamSource;
		private TestAudioSinkAdapter _audioSink;
		private byte[] _lastFrame;
		private MediaDeviceConfig _mediaDeviceConfig;
		private readonly AudioFormat _audioFormat = new AudioFormat();

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var config = new ConfigurationHelper<MediaDeviceConfig>();
			_mediaDeviceConfig = config.GetConfigurationObject(MediaDeviceConfig.DefaultFileName);
			lstAudioInputDevices.ItemsSource = CaptureDeviceConfiguration.GetAvailableAudioCaptureDevices();
			lstVideoInputDevices.ItemsSource = CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices();
			lstAudioInputDevices.SelectedItem =  _mediaDeviceConfig.SelectBestAudioCaptureDevice();
			lstVideoInputDevices.SelectedItem = _mediaDeviceConfig.SelectBestVideoCaptureDevice();
		}

		private void enableMicrophone_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				InitializeCaptureSource();
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString());
				MessageBox.Show(ex.ToString());
			}
		}

		private void InitializeCaptureSource()
		{
			ClientLogger.Debug("AudioLoopbackTest:InitializeCaptureSource()");
			if (_captureSource != null)
			{
				_captureSource.Stop();
			}
			_captureSource = new CaptureSource();
			_captureSource.AudioCaptureDevice = (AudioCaptureDevice)lstAudioInputDevices.SelectedItem;
			_captureSource.VideoCaptureDevice = (VideoCaptureDevice)lstVideoInputDevices.SelectedItem;
			_mediaElement = new MediaElement();
			_audioStreamSource = new TestAudioStreamSource(this);


			// Set the audio properties.
			if (_captureSource.AudioCaptureDevice != null)
			{
				MediaDeviceConfig.SelectBestAudioFormat(_captureSource.AudioCaptureDevice);
				if (_captureSource.AudioCaptureDevice.DesiredFormat != null)
				{
					_captureSource.AudioCaptureDevice.AudioFrameSize = _audioFormat.MillisecondsPerFrame; // 20 milliseconds
					_audioSink = new TestAudioSinkAdapter(_captureSource, new NullAudioController());
					_audioSink.RawFrameAvailable += audioSink_RawFrameAvailable;
					_audioSink.ProcessedFrameAvailable += audioSink_FrameArrived;
					ClientLogger.Debug("CaptureSource initialized.");
				}
				else
				{
					ClientLogger.Debug("No suitable audio format was found.");
				}

				ClientLogger.Debug("Checking device access.");
				if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
				{
					ClientLogger.Debug("AudioLoopbackTest CaptureSource starting with audio device {0}, video device {1}.",
						_captureSource.AudioCaptureDevice.FriendlyName, _captureSource.VideoCaptureDevice.FriendlyName);
					_captureSource.Start();
					ClientLogger.Debug("CaptureSource started; setting media element source.");
					_mediaElement.SetSource(_audioStreamSource);
					ClientLogger.Debug("MediaElement source set; telling media element to play.");
					_mediaElement.Play();
				}

			}
			else
			{
				// Do something more here eventually, once we figure out what the user experience should be.
				ClientLogger.Debug("No audio capture device was found.");
			}
		}

		long _rawFrameCount;
		void audioSink_RawFrameAvailable(object sender, EventArgs<byte[]> e)
		{
			var frame = e.Value;
			var samples = new short[frame.Length / sizeof(short)];
			Buffer.BlockCopy(frame, 0, samples, 0, frame.Length);
			inputAudioVisualizer.RenderVisualization(samples);
			if (_rawFrameCount++ % 100 == 0)
			{
				var sb = new StringBuilder();
				for (int i = 0; i < 20; i++)
				{
					sb.Append(samples[i] + ",");
				}
				Deployment.Current.Dispatcher.BeginInvoke(() => lblRawSamples.Text = sb.ToString());
			}
		}

		long _processedFrameCount;
		private void audioSink_FrameArrived(object sender, EventArgs<byte[]> e)
		{
			var frame = e.Value;
			var samples = new short[frame.Length / sizeof(short)];
			Buffer.BlockCopy(frame, 0, samples, 0, frame.Length);
			outputAudioVisualizer.RenderVisualization(samples);
			if (_processedFrameCount++ % 100 == 0)
			{
				var sb = new StringBuilder();
				for (int i = 0; i < 20; i++)
				{
					sb.Append(samples[i] + ",");
				}
				Deployment.Current.Dispatcher.BeginInvoke(() => lblProcessedSamples.Text = sb.ToString());
			}
			_lastFrame = e.Value;
		}

		public byte[] GetNextFrame()
		{
			var frame = _lastFrame;
			_lastFrame = null;
			return frame;
		}
	}

}
