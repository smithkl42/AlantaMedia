using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using Alanta.Client.Media.VideoCodecs;
using Alanta.Client.UI.Common.Classes;

namespace Alanta.Client.Test.Media.Video
{
	public partial class CodecTest : Page
	{

		#region Constructors
		public CodecTest()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			btnSaveRaw.IsEnabled = false;
			btnPlayRaw.IsEnabled = false;

			videoCapture.Height = VideoConstants.Height;
			videoCapture.Width = VideoConstants.Width;
			mRawMediaElement.Height = VideoConstants.Height;
			mRawMediaElement.Width = VideoConstants.Width;
			mProcessedMediaElement.Height = VideoConstants.Height;
			mProcessedMediaElement.Width = VideoConstants.Width;

			// Initialize and capture the webcam.
			InitializeCaptureSource();
			CaptureSelectedInputDevices();
			var videoBrush = new VideoBrush();
			videoBrush.SetSource(mCaptureSource);
			videoCapture.Fill = videoBrush;

			// Setup the recorder
			mVideoQualityController = new TestVideoQualityController();
			mRecorder = new Recorder();
			mVideoSink = new VideoSinkAdapter(mCaptureSource, mRecorder, mVideoQualityController);
			lblFramesRecorded.DataContext = mRecorder;

		}
		#endregion

		#region Fields and Properties
		private const string startText = "Start";
		private const string stopText = "Stop";
		private const string playText = "Play";
		private const string recordSourceText = "Record Source";
		private const string filter = "RawVideo Files (*.rawvideo)|*.rawvideo|All Files (*.*)|*.*";
		private CaptureSource mCaptureSource;
		// ReSharper disable NotAccessedField.Local
		private VideoSinkAdapter mVideoSink;
		// ReSharper restore NotAccessedField.Local

		private readonly List<byte[]> mRawFrames = new List<byte[]>();

		private Recorder mRecorder;

		private Player mRawPlayer;
		private MediaStreamSource mRawVideoMediaStreamSource;

		private Player mProcessedPlayer;
		private MediaStreamSource mProcessedVideoMediaStreamSource;
		private TestVideoQualityController mVideoQualityController;

		private TestRunner mTestRunner;

		#endregion

		#region Enable Microphone

		private void btnEnableWebcam_Click(object sender, RoutedEventArgs e)
		{
			CaptureSelectedInputDevices();
		}

		private void InitializeCaptureSource()
		{
			if (mCaptureSource == null)
			{
				// Setup the capture source (for recording audio)
				mCaptureSource = new CaptureSource { VideoCaptureDevice = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice() };
				if (mCaptureSource.VideoCaptureDevice != null)
				{
					MediaDeviceConfig.SelectBestVideoFormat(mCaptureSource.VideoCaptureDevice);
					if (mCaptureSource.AudioCaptureDevice.DesiredFormat != null)
					{
						mCaptureSource.AudioCaptureDevice.AudioFrameSize = AudioConstants.MillisecondsPerFrame; // 20 milliseconds
						mVideoSink = new VideoSinkAdapter(mCaptureSource, mRecorder, mVideoQualityController);
						ClientLogger.Debug("CaptureSource initialized.");
					}
					else
					{
						ClientLogger.Debug("No suitable audio format was found.");
					}
					panelWebcam.DataContext = mCaptureSource;
				}
				else
				{
					// Do something more here eventually, once we figure out what the user experience should be.
					ClientLogger.Debug("No audio capture device was found.");
				}
			}
		}

		private void CaptureSelectedInputDevices()
		{
			try
			{
				if (mCaptureSource != null && mCaptureSource.State != CaptureState.Started)
				{
					if (CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess())
					{
						mCaptureSource.Start();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}


		#endregion

		#region Manage Source

		private void btnRecordRaw_Click(object sender, RoutedEventArgs e)
		{
			if ((string)btnRecordRaw.Content == recordSourceText)
			{
				btnRecordRaw.Content = stopText;
				mRecorder.StartRecording(mRawFrames, () =>
				{
					btnSaveRaw.IsEnabled = true;
					btnPlayRaw.IsEnabled = true;
					btnStart.IsEnabled = true;
					btnRecordRaw.Content = recordSourceText;
				});
			}
			else
			{
				mRecorder.StopRecording();
			}
		}

		private void btnOpenRaw_Click(object sender, RoutedEventArgs e)
		{
			mRawFrames.Load(filter, VideoConstants.BytesPerFrame);
			btnSaveRaw.IsEnabled = true;
			btnPlayRaw.IsEnabled = true;
			btnStart.IsEnabled = true;
		}

		private void btnSaveSource_Click(object sender, RoutedEventArgs e)
		{
			mRawFrames.Save(filter);
		}

		private void btnPlayRaw_Click(object sender, RoutedEventArgs e)
		{
			PlayRaw();
		}

		private void PlayRaw()
		{
			try
			{
				if ((string)btnPlayRaw.Content == playText)
				{
					// Setup the player for raw video.
					mRawPlayer = new Player(mRawMediaElement, mVideoQualityController);
					mRawVideoMediaStreamSource = new TestVideoMediaStreamSource(mRawPlayer, 100, VideoConstants.Width, VideoConstants.Height);
					mRawMediaElement.BufferingTime = TimeSpan.FromMilliseconds(100);
					mRawMediaElement.SetSource(mRawVideoMediaStreamSource);
					mRawMediaElement.Visibility = Visibility.Visible;
					imgLastRawFrame.Visibility = Visibility.Collapsed;
					mRawPlayer.StartPlaying(mRawFrames, () =>
					{
						btnPlayRaw.Content = playText;
						btnPlayRaw.IsEnabled = true;
						mRawMediaElement.Source = null;
						mRawVideoMediaStreamSource = null;
						mRawPlayer = null;
						mRawMediaElement.Visibility = Visibility.Collapsed;
						imgLastRawFrame.Visibility = Visibility.Visible;
						SetImage(imgLastRawFrame, mRawFrames[mRawFrames.Count - 1]);
					});
					btnPlayRaw.Content = stopText;
				}
				else
				{
					btnPlayRaw.IsEnabled = false;
					mRawPlayer.StopPlaying();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		#endregion

		#region Manage Tests

		private void btnStart_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if ((string)btnStart.Content == startText)
				{
					btnStart.Content = stopText;
					Func<IVideoQualityController, IVideoCodec> codecFunction;
					var selection = cboVideoCodec.SelectionBoxItem.ToString().ToLower();
					if (selection == "jdif")
					{
						codecFunction = GetJdifVideoCodec;
					}
					else
					{
						throw new InvalidOperationException("The selected codec is not supported");
					}
					mTestRunner = new TestRunner(codecFunction);
					mTestRunner.NoSendStart = int.Parse(txtNoSendStart.Text);
					mTestRunner.NoSendStop = int.Parse(txtNoSendEnd.Text);
					mTestRunner.NoSendStep = int.Parse(txtNoSendStep.Text);
					mTestRunner.DeltaSendStart = int.Parse(txtDeltaSendStart.Text);
					mTestRunner.DeltaSendStop = int.Parse(txtDeltaSendEnd.Text);
					mTestRunner.DeltaSendStep = int.Parse(txtDeltaSendStep.Text);
					mTestRunner.JpegQualityStart = short.Parse(txtJpegQualityStart.Text);
					mTestRunner.JpegQualityStop = short.Parse(txtJpegQualityEnd.Text);
					mTestRunner.JpegQualityStep = short.Parse(txtJpegQualityStep.Text);
					mTestRunner.RawFrames = mRawFrames;
					mTestRunner.KeepProcessedFrames = chkKeepProcessedFrames.IsChecked ?? false;
					grdResults.ItemsSource = mTestRunner.Results;
					txtStatus.DataContext = mTestRunner;
					mTestRunner.Start(() =>
					{
						btnStart.Content = startText;
						btnExport.IsEnabled = true;
					});
				}
				else
				{
					mTestRunner.Stop();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private static IVideoCodec GetJdifVideoCodec(IVideoQualityController videoQualityController)
		{
			var codec = new JpegDiffVideoCodec(videoQualityController);
			codec.Initialize(VideoConstants.Height, VideoConstants.Width, VideoConstants.MaxPayloadSize);
			return codec;
		}

		private void btnExport_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (mTestRunner != null && mTestRunner.Results.Count > 0)
				{
					const string header = "Duration,AcceptFps,InterleaveFactor,FullFrameInterval,NoSendCutoff,DeltaCutoff,JpegQuality,Fidelity,RawSize,EncodedSize,Compression,DeltaBlockSize,JpegBlockSize,BlocksNotSent,BlocksAsDelta,BlocksAsJpeg,Kbps\r\n";
					mTestRunner.Results.SaveToCsv(header, result => string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}\r\n",
																				  result.Duration,
																				  result.VideoQualityController.AcceptFramesPerSecond,
																				  result.VideoQualityController.InterleaveFactor,
																				  result.VideoQualityController.FullFrameInterval,
																				  result.VideoQualityController.NoSendCutoff,
																				  result.VideoQualityController.DeltaSendCutoff,
																				  result.VideoQualityController.JpegQuality,
																				  result.Fidelity,
																				  result.RawSize,
																				  result.EncodedSize,
																				  result.Compression,
																				  result.DeltaBlockSize,
																				  result.JpegBlockSize,
																				  result.BlocksNotSent,
																				  result.BlocksAsDelta,
																				  result.BlocksAsJpeg,
																				  result.Kbps));
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

		private void btnPlayResults_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var button = (Button)sender;
				if ((string)button.Content == playText)
				{
					// Setup the player for processed video.
					var test = (TestInstance)button.DataContext;
					if (test.ProcessedFrames == null || test.ProcessedFrames.Count == 0) return;
					button.Content = stopText;
					mProcessedPlayer = new Player(mProcessedMediaElement, test.VideoQualityController);
					mProcessedVideoMediaStreamSource = new TestVideoMediaStreamSource(mProcessedPlayer, 100, VideoConstants.Width, VideoConstants.Height);
					mProcessedMediaElement.BufferingTime = TimeSpan.FromMilliseconds(100);
					mProcessedMediaElement.SetSource(mProcessedVideoMediaStreamSource);
					mProcessedMediaElement.Visibility = Visibility.Visible;
					imgLastProcessedFrame.Visibility = Visibility.Collapsed;
					mProcessedPlayer.StartPlaying(test.ProcessedFrames, () =>
					{
						button.Content = playText;
						button.IsEnabled = true;
						mProcessedMediaElement.Source = null;
						mProcessedMediaElement.Visibility = Visibility.Collapsed;
						imgLastProcessedFrame.Visibility = Visibility.Visible;
						SetImage(imgLastProcessedFrame, test.ProcessedFrames[test.ProcessedFrames.Count - 1]);
					});
					PlayRaw();
				}
				else
				{
					button.IsEnabled = false;
					mTestRunner.Stop();
					PlayRaw();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void btnSaveResults_Click(object sender, RoutedEventArgs e)
		{

		}

		private static void SetImage(Image image, Array source)
		{
			var bmp = new WriteableBitmap(VideoConstants.Width, VideoConstants.Height);
			Buffer.BlockCopy(source, 0, bmp.Pixels, 0, Buffer.ByteLength(source));
			bmp.Invalidate();
			image.Source = bmp;
		}

		#endregion

	}
}
