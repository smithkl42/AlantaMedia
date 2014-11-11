using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using Alanta.Client.UI.Common.Classes;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media.Aec
{
	public partial class ParamsTest : Page
	{
		#region Constructors and Initializers

		public ParamsTest()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			btnSaveSpeakers.IsEnabled = false;
			btnSaveSource.IsEnabled = false;
			btnPlaySource.IsEnabled = false;
			btnPlaySpeakers.IsEnabled = false;
			_sourceFrames = new List<byte[]>();
			InitializeCaptureSource();

			_mediaElement = new MediaElement();
			_audioStreamSource = new AudioMediaStreamSource(null, AudioFormat.Default);
			_mediaElement.SetSource(_audioStreamSource);
			_player = new PlayerBase(_mediaElement, _audioStreamSource, sourceAudioVisualizer);

		}

		#endregion

		#region Fields and Properties

		private const string startText = "Start";
		private const string stopText = "Stop";
		private const string playText = "Play";
		private const string recordSourceText = "Record Source";
		private const string filter = "PCM Files (*.pcm)|*.pcm|All Files (*.*)|*.*";
		private AudioSinkAdapter _audioSink;
		private MediaElement _mediaElement;
		private AudioMediaStreamSource _audioStreamSource;
		private CaptureSource _captureSource;
		private TestRunnerBase _testRunner;

		private RecorderBase _recorder;
		private PlayerBase _player;

		private List<byte[]> _sourceFrames;
		private List<byte[]> _speakerFrames;

		#endregion

		#region Enable Microphone

		private void btnEnableMicrophone_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_captureSource != null)
				{
					if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
					{
						txtStatus.Text = "Microphone enabled and capture source started.";
						ClientLogger.Debug("AudioTimingPage CaptureSource started.");
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void InitializeCaptureSource()
		{
			if (_captureSource == null)
			{
				// Setup the capture source (for recording audio)
				_captureSource = new CaptureSource();
				_captureSource.AudioCaptureDevice = CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
				if (_captureSource.AudioCaptureDevice != null)
				{
					MediaDeviceConfig.SelectBestAudioFormat(_captureSource.AudioCaptureDevice);
					if (_captureSource.AudioCaptureDevice.DesiredFormat != null)
					{
						_captureSource.AudioCaptureDevice.AudioFrameSize = AudioFormat.Default.MillisecondsPerFrame; // 20 milliseconds
						_audioSink = new AudioSinkAdapter(_captureSource, null, MediaConfig.Default, new TestMediaEnvironment(), AudioFormat.Default);
						_recorder = new RecorderBase(_captureSource, _audioSink, speakersAudioVisualizer);
						chkSynchronizeRecording.DataContext = _audioSink;
						ClientLogger.Debug("CaptureSource initialized.");
					}
					else
					{
						ClientLogger.Debug("No suitable audio format was found.");
					}
					panelMicrophone.DataContext = _captureSource;
				}
				else
				{
					// Do something more here eventually, once we figure out what the user experience should be.
					ClientLogger.Debug("No audio capture device was found.");
				}

			}
		}

		#endregion

		#region Source

		private void btnRecordSource_Click(object sender, RoutedEventArgs e)
		{
			if (!_recorder.IsConnected)
			{
				btnRecordSource.Content = stopText;
				_recorder.StartRecording(_sourceFrames);
			}
			else
			{
				_recorder.StopRecording();
				btnRecordSource.Content = recordSourceText;
				btnSaveSource.IsEnabled = true;
				btnPlaySource.IsEnabled = true;
			}
		}

		private void btnOpenSource_Click(object sender, RoutedEventArgs e)
		{
			_sourceFrames = new List<byte[]>();
			_sourceFrames.Load(filter, AudioFormat.Default.BytesPerFrame);
			txtStatus.Text = "Source file opened.";
			btnSaveSource.IsEnabled = true;
			btnPlaySource.IsEnabled = true;
		}

		private void btnPlaySource_Click(object sender, RoutedEventArgs e)
		{
			if (!_player.IsConnected)
			{
				btnPlaySource.Content = stopText;
				_player.StartPlaying(_sourceFrames, () =>
				{
					btnPlaySource.Content = playText;
				});
			}
			else
			{
				_player.StopPlaying();
			}
		}

		private void btnSaveSource_Click(object sender, RoutedEventArgs e)
		{
			_sourceFrames.Save(filter);
		}

		#endregion

		#region Speakers

		private void btnRecordSpeakers_Click(object sender, RoutedEventArgs e)
		{
			if (!_player.IsConnected)
			{
				btnSaveSpeakers.IsEnabled = false;
				btnRecordSpeakers.Content = stopText;
				_speakerFrames = new List<byte[]>();
				_recorder.StartRecording(_speakerFrames);
				_player.StartPlaying(_sourceFrames, () =>
				{
					_recorder.StopRecording();
					btnRecordSpeakers.Content = "Record Speakers";
					btnStart.IsEnabled = true;
					txtStatus.Text = "Source audio played and microphone input saved in speakers buffer.";
					btnSaveSpeakers.IsEnabled = true;
					btnPlaySpeakers.IsEnabled = true;
				});
			}
			else
			{
				_player.StopPlaying();
			}
		}

		private void btnOpenSpeakers_Click(object sender, RoutedEventArgs e)
		{
			_speakerFrames = new List<byte[]>();
			_speakerFrames.Load(filter, AudioFormat.Default.BytesPerFrame);
			btnSaveSpeakers.IsEnabled = true;
			btnPlaySpeakers.IsEnabled = true;
			txtStatus.Text = "Speaker file opened.";
		}

		private void btnSaveSpeakers_Click(object sender, RoutedEventArgs e)
		{
			_speakerFrames.Save(filter);
		}

		private void btnPlaySpeakers_Click(object sender, RoutedEventArgs e)
		{
			if (!_player.IsConnected)
			{
				btnPlaySpeakers.Content = stopText;
				_player.StartPlaying(_speakerFrames, () =>
				{
					btnPlaySpeakers.Content = playText;
				});
			}
			else
			{
				_player.StopPlaying();
			}
		}

		#endregion

		#region Test

		private void btnStart_Click(object sender, RoutedEventArgs e)
		{
			if (_testRunner == null || !_testRunner.IsRunning)
			{
				try
				{
					var selection = (ComboBoxItem)cboEchoCancelFilter.SelectedValue;
					var selectedEchoCanceller = (EchoCancellerType)Enum.Parse(typeof(EchoCancellerType), (string)selection.Content, true);
					if (chkLive.IsChecked == true)
					{
						_testRunner = new TestRunnerLive(selectedEchoCanceller, sourceAudioVisualizer, speakersAudioVisualizer, cancelledAudioVisualizer);
					}
					else
					{
						if (_speakerFrames == null || _speakerFrames.Count == 0)
						{
							MessageBox.Show("No speaker audio has been loaded.");
							return;
						}
						_testRunner = new TestRunnerFast(selectedEchoCanceller, sourceAudioVisualizer, speakersAudioVisualizer, cancelledAudioVisualizer, _speakerFrames);
					}

					grdResults.ItemsSource = _testRunner.Results;

					// Setup the parameters.
					_testRunner.CaptureSource = _captureSource;
					_testRunner.AudioSinkAdapter = _audioSink;
					_testRunner.ExpectedLatencyStart = int.Parse(txtExpectedLatencyStart.Text);
					_testRunner.ExpectedLatencyEnd = int.Parse(txtExpectedLatencyEnd.Text);
					_testRunner.ExpectedLatencyStep = int.Parse(txtExpectedLatencyStep.Text);
					_testRunner.FilterLengthStart = int.Parse(txtFilterLengthStart.Text);
					_testRunner.FilterLengthEnd = int.Parse(txtFilterLengthEnd.Text);
					_testRunner.FilterLengthStep = int.Parse(txtFilterLengthStep.Text);
					_testRunner.AecIsSynchronized = chkSynchronizeAEC.IsChecked == true;
					_testRunner.SourceFrames = _sourceFrames;
					btnStart.Content = stopText;

					_testRunner.Start(() =>
					{
						if (_speakerFrames != null && _speakerFrames.Count > 0)
						{
							btnSaveSpeakers.IsEnabled = true;
						}
						btnStart.Content = startText;
					});
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}
			else
			{
				_testRunner.Stop();
			}
		}

		private void btnPlayResults_Click(object sender, RoutedEventArgs e)
		{
			var src = (Button)sender;
			var results = src.DataContext as PlaybackResults;
			if (results == null) return;
			if (!_player.IsConnected)
			{
				src.Content = stopText;
				_player.StartPlaying(results.Cancelled, () =>
				{
					src.Content = playText;
				});
			}
			else
			{
				_player.StopPlaying();
			}
		}

		private void btnSaveResults_Click(object sender, RoutedEventArgs e)
		{
			var src = (Button)sender;
			var results = src.DataContext as PlaybackResults;
			if (results != null)
			{
				results.Cancelled.Save(filter);
			}
		}

		private void btnExport_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_testRunner != null && _testRunner.Results.Count > 0)
				{
					const string header = "ExpectedLatency,FilterLengthInMs,FilterLengthInSamples,RecordedVolumeSD,RecordedVolumeAverage,RecordedRMS,CancelledVolumeSD,CancelledVolumeAverage,CancelledRMS,VolumeChangePercent,VolumeChangeDecibels\r\n";
					_testRunner.Results.SaveToCsv(header, result => string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\r\n",
															result.ExpectedLatency,
															result.FilterLengthInMs,
															result.FilterLengthInSamples,
															result.RecordedVolumeStdDev,
															result.RecordedVolumeAverage,
															result.RecordedRMS,
															result.CancelledVolumeStdDev,
															result.CancelledVolumeAverage,
															result.CancelledRMS,
															result.VolumeReduction,
															result.VolumeReductionInDecibels));
					txtStatus.Text = "File exported.";
				}
				else
				{
					MessageBox.Show("No test results available.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		#endregion
	}

	public enum EchoCancellerType
	{
		Speex,
		Speex2,
		TimeDomain,
		WebRtc
	}
}