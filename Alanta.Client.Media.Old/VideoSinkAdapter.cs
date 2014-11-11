using System;
using System.Windows.Media;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public class VideoSinkAdapter : VideoSink
	{
		public VideoSinkAdapter(CaptureSource captureSource, IVideoController mediaController, IVideoQualityController videoQualityController)
		{
			CaptureSource = captureSource;
			_mediaController = mediaController;
			_videoQualityController = videoQualityController;
		}

		private readonly IVideoController _mediaController;
		private readonly IVideoQualityController _videoQualityController;
		private VideoFormat _videoFormat;
		private int _framesPerSecond;
		private int _frameNumber;
		private float _scaleX;
		private float _scaleY;
		private readonly byte[] _resampleBuffer = new byte[VideoConstants.BytesPerFrame];
		protected bool _dataReceived;
		public event EventHandler CaptureSuccessful;
		private int _errorCount;

		protected override void OnCaptureStarted()
		{
			ClientLogger.Debug("VideoSinkAdapter: capture started.");
		}

		protected override void OnCaptureStopped()
		{
			ClientLogger.Debug("VideoSinkAdapter: capture stopped.");
		}

		protected override void OnFormatChange(VideoFormat videoFormat)
		{
			ClientLogger.Debug("The video format was changed: FramesPerSecond={0}, PixelFormat={1}, PixelHeight={2}, PixelWidth={3}, Stride={4}", 
				videoFormat.FramesPerSecond, videoFormat.PixelFormat, videoFormat.PixelHeight, videoFormat.PixelWidth, videoFormat.Stride);

			_videoFormat = videoFormat;
			_framesPerSecond = (int)videoFormat.FramesPerSecond;
			_frameNumber = 0;

			// Determine the scaling factor to be used in resizing the image; 
			// e.g., if the actual width is 320 and the desired width is 160, the scaling factor will be 2.0f.
			_scaleX = videoFormat.PixelWidth / (float)VideoConstants.Width;
			_scaleY = videoFormat.PixelHeight / (float)VideoConstants.Height;
		}

		protected override void OnSample(long sampleTime, long frameDuration, byte[] sampleData)
		{
			// Raise an event if we've managed to successfully capture data.
			try
			{
				if (!_dataReceived)
				{
					_dataReceived = true;
					if (CaptureSuccessful != null)
					{
						CaptureSuccessful(this, new EventArgs());
					}
				}

				if (FrameShouldBeSubmitted())
				{
					var resampledData = ResizeFrame(sampleData);
					SubmitFrame(resampledData, _videoFormat.Stride);
				}
			}
			catch (Exception ex)
			{
				if (_errorCount++ % 100 == 0)
				{
					ClientLogger.Debug("Error {0} submitting frame: {1}", _errorCount, ex);
				}
			}
		}

		protected virtual void SubmitFrame(byte[] frame, int stride)
		{
			_mediaController.SetVideoFrame(frame, stride);
		}

		/// <summary>
		/// Applies a chronological downsampling ratio.
		/// </summary>
		/// <returns>True if the frame should be submitted, false if not.</returns>
		/// <remarks>
		/// In other words, if:
		/// VideoConstant.AcceptFramesPerSecond = 5
		/// videoFormat.FramesPerSecond = 18
		/// Then if:
		/// frame = 0, step will be 0, and frame WILL be sent.
		/// frame = 1, step will be 5, and frame will NOT be sent.
		/// frame = 2, step will be 10, and frame will NOT be sent.
		/// frame = 3, step will be 15, and frame will NOT be sent.
		/// frame = 4, step will be 2, and frame WILL be sent.
		/// frame = 5, step will be 7, and frame will NOT be sent.
		/// </remarks>
		private bool FrameShouldBeSubmitted()
		{
			if (_framesPerSecond == 0)
			{
				return false;
			}
			int step = (_frameNumber++ * _videoQualityController.AcceptFramesPerSecond) % _framesPerSecond;
			return step < _videoQualityController.AcceptFramesPerSecond;
		}

		/// <summary>
		/// Applies a naive but fast "nearest neighbor" algorithm to upsample or downsample the image in question.
		/// </summary>
		/// <param name="sampleData">The sample data from the webcam.</param>
		/// <returns>An upsampled or downsampled image.</returns>
		/// <remarks>This method should work at some level for both upsampled and downsampled images, but upsampling will likely produce significant artifacts.</remarks>
		private byte[] ResizeFrame(byte[] sampleData)
		{
			if (_videoFormat.PixelHeight == VideoConstants.Height && _videoFormat.PixelWidth == VideoConstants.Width)
			{
				return sampleData;
			}
			for (int i = 0; i < _resampleBuffer.Length; i++)
			{
				WriteResampledPixel(sampleData, _resampleBuffer, ref i);
			}
			return _resampleBuffer;
		}

		/// <summary>
		/// Writes the appropriate pixel information to the resized image.
		/// </summary>
		/// <param name="originalImage">The sample data from the webcam.</param>
		/// <param name="resizedImage">The buffer ont which we should write the resampled data</param>
		/// <param name="resizedImagePosition">The position where the next pixel should be written</param>
		/// <returns></returns>
		private void WriteResampledPixel(byte[] originalImage, byte[] resizedImage, ref int resizedImagePosition)
		{
			// Figure out which pixel in the resized image we're talking about.
			int resizedImagePixel = resizedImagePosition / VideoConstants.BytesPerPixel;

			// Figure out that pixel's x & y location.
			int resizedImageX = resizedImagePixel % VideoConstants.Width;
			int resizedImageY = resizedImagePixel / VideoConstants.Width;

			// Figure out which x & y that corresponds to in the source image.
			var originalImageX = (int)(resizedImageX * _scaleX);
			var originalImageY = (int)(resizedImageY * _scaleY);

			// Figure out where that pixel is located in the original image.
			int originalImagePixel = originalImageY * _videoFormat.PixelWidth + originalImageX;
			int originalImagePosition = originalImagePixel * VideoConstants.BytesPerPixel;

			// Copy that pixel information to the resized image.
			// Buffer.BlockCopy(originalImage, originalImagePosition, resizedImage, resizedImagePosition, MediaConstants.BytesPerPixel);
			// Dropped Buffer.BlockCopy because performance tests showed that a manual copy for small numbers of bytes is faster
			// without the method call overhead.
			resizedImage[resizedImagePosition++] = originalImage[originalImagePosition++];
			resizedImage[resizedImagePosition++] = originalImage[originalImagePosition++];
			resizedImage[resizedImagePosition++] = originalImage[originalImagePosition++];
			resizedImage[resizedImagePosition] = originalImage[originalImagePosition];
		}
	}
}
