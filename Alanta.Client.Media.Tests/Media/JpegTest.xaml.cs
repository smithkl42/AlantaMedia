using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Alanta.Client.Media.Jpeg;
using Alanta.Client.Media.Jpeg.Decoder;
using Alanta.Client.Media.Jpeg.Encoder;
using Alanta.Client.Media.VideoCodecs;
using Alanta.Client.Test.Media;
using Image = System.Windows.Controls.Image;

namespace Alanta.Client.Media.Tests.Media
{
	public partial class JpegTest : Page
	{

		private readonly List<byte[]> mRawFrames = new List<byte[]>();
		private const string filter = "RAWVIDEO Files (*.rawvideo)|*.rawvideo|All Files (*.*)|*.*";
		private const byte jpegQuality = 50;

		public JpegTest()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			mRawFrames.Load(filter, VideoConstants.BytesPerFrame);
			EncodeAndDecodeImages();
		}

		private void EncodeAndDecodeImages()
		{

			// Encode and decode a delta (p-frame) image.
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.Height, VideoConstants.Width);
			var remoteSourceBlock = new FrameBlock(ctxFactory) { BlockType = BlockType.Jpeg };
			Buffer.BlockCopy(mRawFrames[0], 0, remoteSourceBlock.RgbaRaw, 0, mRawFrames[0].Length);
			remoteSourceBlock.Encode(jpegQuality);
			remoteSourceBlock.Decode();
			var imgJpegDeltaFrame0 = new Image();
			SetImage(imgJpegDeltaFrame0, mRawFrames[0], remoteSourceBlock.RgbaRaw, remoteSourceBlock.EncodedStream.Length);
			lstJpegDeltaFrames.Items.Clear();
			lstJpegDeltaFrames.Items.Add(imgJpegDeltaFrame0);

			int frames = Math.Min(mRawFrames.Count, 20);
			for (int i = 1; i < frames; i++)
			{
				// Create a new source block, since we need to start with one that hasn't been encoded yet.
				var localSourceBlock = new FrameBlock(ctxFactory) { BlockType = BlockType.Jpeg };
				Buffer.BlockCopy(mRawFrames[i - 1], 0, localSourceBlock.RgbaRaw, 0, mRawFrames[i - 1].Length);

				// Create the delta block based on the local source block (which has the raw data).
				var deltaFrameBlock = new FrameBlock(ctxFactory) { BlockType = BlockType.JpegDiff };
				Buffer.BlockCopy(mRawFrames[i], 0, deltaFrameBlock.RgbaRaw, 0, mRawFrames[i].Length);
				deltaFrameBlock.SubtractFrom(localSourceBlock, double.MinValue, double.MaxValue);
				deltaFrameBlock.Encode(jpegQuality);

				// Decode the delta block off of the remote source block (which has the cumulative encoded/decoded data)
				deltaFrameBlock.Decode();
				deltaFrameBlock.AddTo(remoteSourceBlock);

				// Add the image to the UI.
				var img = new Image();
				SetImage(img, mRawFrames[i], deltaFrameBlock.RgbaRaw, deltaFrameBlock.EncodedStream.Length);
				lstJpegDeltaFrames.Items.Add(img);

				// Set the remote source block to the most recently processed delta block for the next loop.
				remoteSourceBlock = deltaFrameBlock;
			}

			// Get the initial image and display it.
			SetImage(imgRawFrame0, mRawFrames[0], mRawFrames[0], mRawFrames[0].Length);
			SetImage(imgRawFrame1, mRawFrames[1], mRawFrames[1], mRawFrames[0].Length);

			// Encode and decode two images using the FJCore Jpeg library.
			var stream = EncodeJpeg(mRawFrames[0]);
			stream.Position = 0;
			var jpegOutputBmp = DecodeJpeg(stream);
			SetImage(imgJpegFrame0, mRawFrames[0], jpegOutputBmp, stream.Length);

			stream = EncodeJpeg(mRawFrames[1]);
			stream.Position = 0;
			jpegOutputBmp = DecodeJpeg(stream);
			SetImage(imgJpegFrame1, mRawFrames[1], jpegOutputBmp, stream.Length);

			// Encode and decode two images using the JpegFrame extensions to the FJCore Jpeg library.
			var frameBlock0 = new FrameBlock(ctxFactory);
			Buffer.BlockCopy(mRawFrames[0], 0, frameBlock0.RgbaRaw, 0, mRawFrames[0].Length);
			frameBlock0.Encode(jpegQuality);
			frameBlock0.Decode();
			SetImage(imgJpegFrameFrame0, mRawFrames[0], frameBlock0.RgbaRaw, frameBlock0.EncodedStream.Length);

			var frameBlock1 = new FrameBlock(ctxFactory);
			Buffer.BlockCopy(mRawFrames[1], 0, frameBlock1.RgbaRaw, 0, mRawFrames[1].Length);
			frameBlock1.Encode(jpegQuality);
			frameBlock1.Decode();
			SetImage(imgJpegFrameFrame1, mRawFrames[1], frameBlock1.RgbaRaw, frameBlock1.EncodedStream.Length);

		}

		private static void SetImage(Image image, byte[] source, byte[] result, long encodedSize)
		{
			var bmp = new WriteableBitmap(VideoConstants.Width, VideoConstants.Height);
			Buffer.BlockCopy(result, 0, bmp.Pixels, 0, Buffer.ByteLength(source));
			bmp.Invalidate();
			image.Source = bmp;
			var distance = VideoHelper.GetColorDistance(source, result);
			var tooltip = string.Format("Distance: {0:0.00}; EncodedSize: {1}", distance, encodedSize);
			ToolTipService.SetToolTip(image, tooltip);
		}

		private static MemoryStream EncodeJpeg(byte[] rgbaFrame)
		{
			// Init buffer in FluxJpeg format
			var ms = new MemoryStream();
			var img = new Client.Media.Jpeg.Image(VideoConstants.Width, VideoConstants.Height, rgbaFrame);

			// Encode Image as JPEG using the FluxJpeg library and write to destination stream
			var encoder = new JpegEncoder(img, jpegQuality, ms);
			encoder.Encode();
			return ms;
		}

		public static byte[] DecodeJpeg(Stream sourceStream)
		{
			// Decode JPEG from stream
			var decoder = new JpegDecoder(sourceStream);
			var jpegDecoded = decoder.Decode();
			var img = jpegDecoded.Image;
			img.ChangeColorSpace(ColorSpace.Rgb);

			// Init Buffer
			int w = img.Width;
			int h = img.Height;
			var result = new byte[w * h * 4];
			byte[][][] pixelsFromJpeg = img.Raster;

			// Copy FluxJpeg buffer into WriteableBitmap
			int i = 0;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					result[i++] = pixelsFromJpeg[2][x][y]; // B
					result[i++] = pixelsFromJpeg[1][x][y]; // G
					result[i++] = pixelsFromJpeg[0][x][y]; // R
					result[i++] = 0xFF;
				}
			}

			return result;
		}

	}
}
