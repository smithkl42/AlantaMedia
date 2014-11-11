using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Alanta.Client.Common.Logging;

// ks 4/25/10 - This has been modified from the original at from http://salusemediakit.codeplex.com/
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Ui.Controls.AudioVisualizer
{
	public partial class AudioVisualizer : UserControl
	{
		#region Fields and Properties

		#region Delegates

		public delegate void SampleReadyDelegate(object sender, Int16[] samples);

		#endregion

		private const int marginLeft = 29;
		private readonly bool _clearEffectIsEnabled;

		public static readonly DependencyProperty FrameSourceProperty =
			DependencyProperty.Register("FrameSource", typeof(short[]), typeof(AudioVisualizer), new PropertyMetadata(OnFrameSourceChanged));

		private readonly int _alternatePrimaryColour = Colour.BuildColour(0xff, 0x00, 0x6f, 0xcf);
		private readonly int _clearColour = Colour.BuildColour(0xff, 0x00, 0x00, 0x00);
		private readonly int[] _fftScanFrequencies = new[] { 30, 80, 155, 250, 375, 500, 750, 1000, 4000, 8000 };
		private readonly ScaleTransform _layoutRootScale;
		private readonly int[] _peakMeterFrequencies = new[] { 30, 55, 80, 120, 155, 195, 250, 375, 500, 750, 1000, 1500, 2000, 3000, 4000, 6000, 8000, 12000, 16000 };
		private readonly int _primaryColour = Colour.BuildColour(0xff, 0x00, 0x7f, 0xff);
		private readonly short[] _sampleBuffer = new short[AudioVisualizerConstants.TransformBufferSize * 10];
		private readonly int _secondaryColour = Colour.BuildColour(0xff, 0x00, 0xff, 0x00);
		private readonly object _visualLock = new object();
		private VisualDescription _activeVisualizer;
		private int[] _clearBuffer;
		private Rectangle _clearShape;
		private RenderVisualsDelegate _currentRenderVisuals;
		private bool _fftScanIsCleared;
		private int[] _lastPeaks;
		private int[] _maximumPeaks;
		private WriteableBitmap _outputWriteableBitmap;

		private int _sampleBufferPosition;
		private int _sampleBufferStart;
		private bool _scanIsCleared;
		private int _widthWithoutMarginRight;
		private int _xRunningScanCounter;

		public double VolumeFactor { get; set; }

		public short[] FrameSource
		{
			get { return (short[])GetValue(FrameSourceProperty); }
			set { SetValue(FrameSourceProperty, value); }
		}

		public ObservableCollection<VisualDescription> Visualizers { get; private set; }

		public VisualDescription ActiveVisualizer
		{
			get { return _activeVisualizer; }
			set
			{
				_activeVisualizer = value;
				SetVisualType(_activeVisualizer);
			}
		}

		private delegate void RenderVisualsDelegate(Int16[] sample);

		#endregion

		#region Constructors

		public AudioVisualizer()
			: this(true)
		{
		}

		public AudioVisualizer(bool clearEffectIsEnabled)
		{
			_clearEffectIsEnabled = clearEffectIsEnabled;
			InitializeComponent();
			Loaded += AudioVisualizer_Loaded;
			VolumeFactor = 1.0f;
			_layoutRootScale = new ScaleTransform();
		}

		#endregion

		#region Event Handlers

		private void AudioVisualizer_Loaded(object sender, RoutedEventArgs e)
		{
			LayoutRoot.RenderTransform = _layoutRootScale;
			InitializeDescriptions();
			InitializeRendering();
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (LayoutRoot != null)
			{
				Size desiredSize = LayoutRoot.DesiredSize;
				Size scaleSize = CalculateScaleSize(finalSize, desiredSize);

				_layoutRootScale.ScaleX = scaleSize.Width;
				_layoutRootScale.ScaleY = scaleSize.Height;

				var originalPosition = new Rect(0, 0, desiredSize.Width, desiredSize.Height);
				LayoutRoot.Arrange(originalPosition);

				finalSize.Width = scaleSize.Width * desiredSize.Width;
				finalSize.Height = scaleSize.Height * desiredSize.Height;
			}
			return finalSize;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = new Size();
			if (LayoutRoot != null)
			{
				LayoutRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Size desiredSize = LayoutRoot.DesiredSize;
				Size scaleSize = CalculateScaleSize(availableSize, desiredSize);

				size.Width = scaleSize.Width * desiredSize.Width;
				size.Height = scaleSize.Height * desiredSize.Height;
			}
			return size;
		}

		private static Size CalculateScaleSize(Size availableSize, Size desiredSize)
		{
			double scaleWidth = 1;
			double scaleHeight = 1;

			if (!double.IsPositiveInfinity(availableSize.Width) &&
				!double.IsPositiveInfinity(availableSize.Height))
			{
				scaleWidth = desiredSize.Width == 0 ? 0 : (availableSize.Width / desiredSize.Width);
				scaleHeight = desiredSize.Height == 0 ? 0 : (availableSize.Height / desiredSize.Height);
			}

			return new Size(scaleWidth, scaleHeight);
		}

		private static void OnFrameSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			try
			{
				var newValue = e.NewValue as short[];
				var source = d as AudioVisualizer;
				if (newValue != null && newValue.Length > 0 && source != null)
				{
					source.RenderVisualization(newValue);
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Render Visalization for new value failed");
			}
		}

		#endregion

		#region Methods

		private void InitializeDescriptions()
		{
			Visualizers = new ObservableCollection<VisualDescription>();
			Visualizers.Add(new VisualDescription { Description = "None", VisualType = VisualDescription.VisualTypeEnumeration.None });
			Visualizers.Add(new VisualDescription { Description = "Peak Meter", VisualType = VisualDescription.VisualTypeEnumeration.PeakMeter });
			Visualizers.Add(new VisualDescription { Description = "Oscilloscope", VisualType = VisualDescription.VisualTypeEnumeration.Oscilloscope });
			Visualizers.Add(new VisualDescription { Description = "Scope", VisualType = VisualDescription.VisualTypeEnumeration.Scope });
			Visualizers.Add(new VisualDescription { Description = "Scan", VisualType = VisualDescription.VisualTypeEnumeration.Scan });
			Visualizers.Add(new VisualDescription { Description = "Band Scan", VisualType = VisualDescription.VisualTypeEnumeration.FftScan });
		}

		private void InitializeRendering()
		{
			// Set default visualizer
			ActiveVisualizer = Visualizers.First(vd => vd.VisualType == VisualDescription.VisualTypeEnumeration.PeakMeter);

			_outputWriteableBitmap = new WriteableBitmap((int)outputImage.Width, (int)outputImage.Height);
			outputImage.Source = _outputWriteableBitmap;

			_clearShape = new Rectangle
			{
				Width = _outputWriteableBitmap.PixelWidth,
				Height = _outputWriteableBitmap.PixelHeight,
				Fill = new SolidColorBrush(Color.FromArgb(32, 0, 0, 0)) // the less opaque it is, the longer the blur effects lasts
			};

			_clearBuffer = ArrayHelper.Instantiate(_outputWriteableBitmap.Pixels.Length, _clearColour);
			_lastPeaks = InitializeArray(FourierTransform.FREQUENCYSLOTCOUNT, byte.MaxValue);
			_maximumPeaks = InitializeArray(FourierTransform.FREQUENCYSLOTCOUNT, byte.MaxValue);

			_widthWithoutMarginRight = Convert.ToInt32(outputImage.Width - marginLeft);
		}

		private static int[] InitializeArray(int size, byte initialValue)
		{
			var newArray = new int[size];
			for (int index = 0; index < size; index++)
			{
				newArray[index] = initialValue;
			}
			return newArray;
		}

		private void SetVisualType(VisualDescription visualDescription)
		{
			lock (_visualLock)
			{
				// Certain Visuals require more information on the display
				fftScanHUD.Visibility = visualDescription.VisualType == VisualDescription.VisualTypeEnumeration.FftScan ? Visibility.Visible : Visibility.Collapsed;

				peakMeterHUD.Visibility = visualDescription.VisualType == VisualDescription.VisualTypeEnumeration.PeakMeter ? Visibility.Visible : Visibility.Collapsed;

				switch (visualDescription.VisualType)
				{
					case VisualDescription.VisualTypeEnumeration.None:
						_currentRenderVisuals = null;
						ClearOutputBitmap();
						FlushOutputBitmap();
						break;

					case VisualDescription.VisualTypeEnumeration.PeakMeter:
						FourierTransform.SetMeterFrequencies(_peakMeterFrequencies);
						_lastPeaks = InitializeArray(FourierTransform.FREQUENCYSLOTCOUNT, byte.MaxValue);
						_maximumPeaks = InitializeArray(FourierTransform.FREQUENCYSLOTCOUNT, byte.MaxValue);
						_currentRenderVisuals = RenderPeakMeter;
						break;

					case VisualDescription.VisualTypeEnumeration.Oscilloscope:
						_currentRenderVisuals = RenderOscilloscope;
						break;

					case VisualDescription.VisualTypeEnumeration.Scope:
						_currentRenderVisuals = RenderScope;
						break;

					case VisualDescription.VisualTypeEnumeration.Scan:
						_scanIsCleared = true;
						_currentRenderVisuals = RenderScan;
						break;

					case VisualDescription.VisualTypeEnumeration.FftScan:
						FourierTransform.SetMeterFrequencies(_fftScanFrequencies);
						_maximumPeaks = InitializeArray(_maximumPeaks.Length, 0);
						_fftScanIsCleared = true;
						_currentRenderVisuals = RenderFFTScan;
						break;
				}
			}
		}

		public void RenderVisualization(byte[] samples)
		{
			var shortSamples = new short[samples.Length / sizeof(short)];
			Buffer.BlockCopy(samples, 0, shortSamples, 0, samples.Length);
			RenderVisualization(shortSamples);
		}

		public void RenderVisualization(short[] samples)
		{
			Dispatcher.BeginInvoke(() =>
			{
				try
				{
					// Copy the samples to the appropriate buffer.
					Buffer.BlockCopy(samples, 0, _sampleBuffer, _sampleBufferPosition * sizeof(short), samples.Length * sizeof(short));
					_sampleBufferPosition += samples.Length;

					// Once there's enough data in the buffer to submit something to the visualizer, 
					// loop through the buffer, pulling a frame's worth of samples at a time, until there's no more room.
					if (_sampleBufferPosition >= AudioVisualizerConstants.TransformBufferSize)
					{
						for (; _sampleBufferPosition - _sampleBufferStart >= AudioVisualizerConstants.TransformBufferSize; _sampleBufferStart += AudioVisualizerConstants.TransformBufferSize)
						{
							var samplesToRender = new short[AudioVisualizerConstants.TransformBufferSize];
							Buffer.BlockCopy(_sampleBuffer, _sampleBufferStart * sizeof(short), samplesToRender, 0, AudioFormat.Default.BytesPerFrame * sizeof(short));
							lock (_visualLock)
							{
								if (_currentRenderVisuals != null)
								{
									_currentRenderVisuals(samplesToRender);
								}
							}
						}

						// When we're all done, move the unprocessed bytes back to the beginning of the buffer.
						_sampleBufferPosition = _sampleBufferPosition - _sampleBufferStart;
						Array.Copy(_sampleBuffer, _sampleBufferStart, _sampleBuffer, 0, _sampleBufferPosition);
						_sampleBufferStart = 0;
					}
				}
				catch (Exception ex)
				{
					ClientLogger.ErrorException(ex, "Render visualization failed");
				}
			});
		}

		private void ClearOutputBitmap()
		{
			// Clear bitmap
			_clearBuffer.CopyTo(_outputWriteableBitmap.Pixels, 0);
		}

		private void RunClearEffect()
		{
			_outputWriteableBitmap.Render(_clearShape, new MatrixTransform());
		}

		private void FlushOutputBitmap()
		{
			_outputWriteableBitmap.Invalidate();
		}

		private void RenderOscilloscope(Int16[] samples)
		{
			Dispatcher.BeginInvoke(() =>
			{
				const int leftOffset = 10;
				int sampleWidth = samples.Length;
				int previousLeftX = leftOffset;
				int previousLeftY = 128;
				int previousRightX = leftOffset;
				int previousRightY = 256;

				if (!(_clearEffectIsEnabled))
				{
					ClearOutputBitmap();
				}

				for (int sampleIndex = 0; sampleIndex < sampleWidth; sampleIndex += 8)
				{
					int x = (sampleIndex >> 2) + leftOffset;
					int y = ((samples[sampleIndex] >> 8) + 128);
					_outputWriteableBitmap.DrawLine(previousLeftX, previousLeftY, x, y, _primaryColour);
					previousLeftX = x;
					previousLeftY = y;

					y = ((samples[sampleIndex + 1] >> 8) + 256);
					_outputWriteableBitmap.DrawLine(previousRightX, previousRightY, x, y, _secondaryColour);
					previousRightX = x;
					previousRightY = y;
				}

				if (_clearEffectIsEnabled)
				{
					RunClearEffect();
				}

				FlushOutputBitmap();
			});
		}

		private void RenderScope(Int16[] samples)
		{
			Dispatcher.BeginInvoke(() =>
			{
				int x = 10;
				int sampleWidth = samples.Length;

				if (!(_clearEffectIsEnabled))
				{
					ClearOutputBitmap();
				}

				for (int sampleIndex = 0; sampleIndex < sampleWidth; sampleIndex += 4)
				{
					int y = (Math.Abs(samples[sampleIndex] >> 8));
					_outputWriteableBitmap.DrawLine(x, (128 - y), x, (y + 128), _primaryColour);

					y = (Math.Abs(samples[sampleIndex + 1] >> 8));
					_outputWriteableBitmap.DrawLine(x, (256 - y), x, (y + 256), _secondaryColour);

					x++;
				}

				if (_clearEffectIsEnabled)
				{
					RunClearEffect();
				}

				FlushOutputBitmap();
			});
		}

		private void RenderScan(Int16[] samples)
		{
			Dispatcher.BeginInvoke(() =>
			{
				int y = 0;
				int sampleWidth = samples.Length;
				var height = (int)outputImage.Height;

				if (_scanIsCleared)
				{
					ClearOutputBitmap();
					_xRunningScanCounter = marginLeft;
					_scanIsCleared = false;
				}

				for (int sampleIndex = 0; sampleIndex < sampleWidth; sampleIndex += 4)
				{
					byte strength = (byte)(Math.Abs(samples[sampleIndex] >> 7));
					strength = (byte)((strength + Math.Abs(samples[sampleIndex + 1] >> 7)) >> 1);

					_outputWriteableBitmap.SetPixel(_xRunningScanCounter, y, 0, strength, 0);
					y++;
					if (y >= height)
					{
						break;
					}
				}

				_outputWriteableBitmap.DrawLine((_xRunningScanCounter + 1), 0, (_xRunningScanCounter + 1), height, _primaryColour);

				_xRunningScanCounter++;
				if (_xRunningScanCounter >= _widthWithoutMarginRight)
				{
					_outputWriteableBitmap.DrawLine(_xRunningScanCounter, 0, _xRunningScanCounter, height, _clearColour);
					_xRunningScanCounter = marginLeft;
				}

				FlushOutputBitmap();
			});
		}

		/// <summary>
		/// Calculates the next position for the slowing flowing animation on the peak meter
		/// </summary>
		/// <param name="currentValue"></param>
		/// <param name="comparisonValue"></param>
		/// <param name="speed"></param>
		/// <param name="cutOff"></param>
		/// <returns></returns>
		private static int CalculatePeakDelay(int currentValue, int comparisonValue, int speed, int cutOff)
		{
			int combinedValue = (currentValue + comparisonValue) >> 1;
			int runningValue = Math.Min(currentValue, combinedValue);
			if (runningValue < combinedValue)
			{
				runningValue += speed; // return to empty bar quicker
				if (runningValue > cutOff)
				{
					runningValue = cutOff;
				}
			}
			else
			{
				runningValue = combinedValue;
			}
			return runningValue;
		}

		private void RenderPeakMeter(short[] samples)
		{
			Dispatcher.BeginInvoke(() =>
			{
				int barSpacing = 25;
				int barWidth = (barSpacing - 8);
				int lineWidth = (barWidth + 1);
				int leftOffset = 60;
				byte[] frequencyPeaks = PeakMeter.CalculateFrequencies(samples, AudioFormat.Default.SamplesPerSecond);

				if (!(_clearEffectIsEnabled))
				{
					ClearOutputBitmap();
				}

				const int baseY = 255;
				for (int x = 0; x < frequencyPeaks.Length; x++)
				{
					int offsetX = (x * barSpacing) + leftOffset;
					int offsetY = Convert.ToInt32(Math.Max(baseY - (frequencyPeaks[x] * VolumeFactor), 0));
					// offsetY = baseY - frequencyPeaks[x];

					_lastPeaks[x] = CalculatePeakDelay(_lastPeaks[x], offsetY, 4, baseY);
					_maximumPeaks[x] = CalculatePeakDelay(_maximumPeaks[x], offsetY, 2, baseY);

					_outputWriteableBitmap.DrawFilledRectangle(offsetX, _lastPeaks[x], (offsetX + barWidth), baseY, _primaryColour);
					// Only draw the maximum peak if it is above the empty bar level (cutoff)
					if (_lastPeaks[x] < baseY)
					{
						_outputWriteableBitmap.DrawLine(offsetX, _maximumPeaks[x], (offsetX + lineWidth), _maximumPeaks[x], _secondaryColour);
					}
				}

				if (_clearEffectIsEnabled)
				{
					RunClearEffect();
				}

				FlushOutputBitmap();
			});
		}

		private void RenderFFTScan(short[] samples)
		{
			Dispatcher.BeginInvoke(() =>
			{
				var height = (int)outputImage.Height;
				// var width = (int)outputImage.Width;
				byte[] frequencyPeaks = PeakMeter.CalculateFrequencies(samples, AudioFormat.Default.SamplesPerSecond);

				if (_fftScanIsCleared)
				{
					ClearOutputBitmap();
					_xRunningScanCounter = marginLeft;
					_fftScanIsCleared = false;
				}

				int bandTotal = frequencyPeaks.Length;
				bool isAlternateColour = false;

				// Clear previous scan line
				_outputWriteableBitmap.DrawLine(_xRunningScanCounter, 0, _xRunningScanCounter, height, _clearColour);

				for (int x = 0; x < bandTotal; x++)
				{
					int maximumPeak = (frequencyPeaks[x] >> 1);
					int baseOffsetY = ((bandTotal - x) * 39);
					_maximumPeaks[x] = ((_maximumPeaks[x] + maximumPeak) >> 1);
					int offsetY = (baseOffsetY - _maximumPeaks[x]);

					int drawingColour = isAlternateColour ? _alternatePrimaryColour : _primaryColour;
					isAlternateColour = !(isAlternateColour);

					if (offsetY == baseOffsetY)
					{
						_outputWriteableBitmap.SetPixel(_xRunningScanCounter, baseOffsetY, drawingColour);
					}
					else
					{
						_outputWriteableBitmap.DrawLine(_xRunningScanCounter, offsetY, _xRunningScanCounter, baseOffsetY, drawingColour);
					}
				}

				// Current Scanline
				_outputWriteableBitmap.DrawLine((_xRunningScanCounter + 1), 0, (_xRunningScanCounter + 1), height, _secondaryColour);

				_xRunningScanCounter++;
				if (_xRunningScanCounter >= _widthWithoutMarginRight)
				{
					_outputWriteableBitmap.DrawLine(_xRunningScanCounter, 0, _xRunningScanCounter, height, _clearColour);
					_xRunningScanCounter = marginLeft;
				}

				FlushOutputBitmap();
			});
		}

		#endregion

		#region public classes

		#region Nested type: EffectDescription

		/// <summary>
		///  Describes an Audio Effect type
		/// </summary>
		public class EffectDescription
		{
			#region EffectTypeEnumeration enum

			public enum EffectTypeEnumeration
			{
				None,
				Pan,
				Noise,
				Echo,
				PitchShift,
				Duet
			}

			#endregion

			public string Description { get; set; }

			public EffectTypeEnumeration EffectType { get; set; }
		}

		#endregion

		#region Nested type: VisualDescription

		/// <summary>
		/// 	Describes a Visual rendering type
		/// </summary>
		public class VisualDescription
		{
			#region VisualTypeEnumeration enum

			public enum VisualTypeEnumeration
			{
				None,
				PeakMeter,
				Oscilloscope,
				Scope,
				Scan,
				FftScan
			}

			#endregion

			public string Description { get; set; }

			public VisualTypeEnumeration VisualType { get; set; }
		}

		#endregion

		#endregion
	}
}