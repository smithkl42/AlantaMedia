using System.Windows;
using System.Windows.Media;

namespace Alanta.Client.Ui.Controls.AudioVisualizer
{
	public static class AudioVisualizerConstants
	{
		public const int TransformBufferSize = 1024;
	}

	public static class Colour
	{
		public static int BuildColour(byte alpha, byte red, byte green, byte blue)
		{
			return ((alpha << 24) | (red << 16) | (green << 8) | blue);
		}
	}

	public static class ArrayHelper
	{
		public static int[] Fill(int[] array, int fillValue)
		{
			for (int index = 0; index < array.Length; index++)
			{
				array[index] = fillValue;
			}

			return array;
		}

		public static int[] Instantiate(int length, int fillValue)
		{
			return Fill(new int[length], fillValue);
		}
	}

	public static class UiHelper
	{
		public static Point GetPosition(UIElement uiElement)
		{
			GeneralTransform generalTransform = uiElement.TransformToVisual(Application.Current.RootVisual);
			return generalTransform.Transform(new Point(0, 0));
		}

		public static Point GetPosition(UIElement uiElement, UIElement relativeToUIElement)
		{
			GeneralTransform generalTransform = uiElement.TransformToVisual(relativeToUIElement);
			return generalTransform.Transform(new Point(0, 0));
		}
	}

	/// <summary>
	///  FFT peak calculations based on Cristian Ricciolo Civera's Direct Show for Silverlight: http://directshow4sl.codeplex.com/
	/// </summary>
	public static class PeakMeter
	{
		private static byte[] ComputeFFT(double[] fftRealInput, int sampleFrequency)
		{
			var fftRealOutput = new double[fftRealInput.Length];
			var fftImaginaryOutput = new double[fftRealInput.Length];
			var fftAmplitude = new double[fftRealInput.Length];

			FourierTransform.Compute(
				AudioVisualizerConstants.TransformBufferSize,
				fftRealInput,
				null,
				fftRealOutput,
				fftImaginaryOutput,
				false);

			FourierTransform.Norm(AudioVisualizerConstants.TransformBufferSize, fftRealOutput, fftImaginaryOutput, fftAmplitude);
			return FourierTransform.GetPeaks(fftAmplitude, null, sampleFrequency);
		}

		/*
		public static byte[] CalculateFrequencies(Int16[] samples, int sampleFrequency)
		{
			byte[] peaks;
			byte[] rightPeaks;
			double[] fftLeftRealInput = new double[samples.Length >> 1];
			double[] fftRightRealInput = new double[fftLeftRealInput.Length];
			for (int index = 0; index < fftLeftRealInput.Length; index++)
			{
				// Extract only the left channel
				fftLeftRealInput[index] = samples[(index * 2)];
				fftRightRealInput[index] = samples[((index * 2) + 1)];
			}

			peaks = ComputeFFT(fftLeftRealInput, sampleFrequency);
			rightPeaks = ComputeFFT(fftRightRealInput, sampleFrequency);

			for (int index = 0; index < peaks.Length; index++)
			{
				peaks[index] = Math.Max(peaks[index], rightPeaks[index]);
			}

			return peaks;
		}
		*/

		public static byte[] CalculateFrequencies(short[] samples, int sampleFrequency)
		{
			// ks 4/25/10 - Changed to expect sample size of 1024 not 2048.
			var fftRealInput = new double[samples.Length]; //  >> 1];
			for (int index = 0; index < fftRealInput.Length; index++)
			{
				// fftRealInput[index] = Math.Max(samples[(index * 2)], samples[((index * 2) + 1)]);
				fftRealInput[index] = samples[index];
			}

			byte[] peaks = ComputeFFT(fftRealInput, sampleFrequency);

			return peaks;
		}
	}
}