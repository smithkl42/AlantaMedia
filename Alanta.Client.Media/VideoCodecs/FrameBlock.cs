using System;
using System.Diagnostics;
using System.IO;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media.Jpeg;
using Alanta.Client.Media.Jpeg.Decoder;
using Alanta.Client.Media.Jpeg.Encoder;

namespace Alanta.Client.Media.VideoCodecs
{
	public class FrameBlock : IReferenceCount
	{
		#region Constructors
		public FrameBlock(JpegFrameContextFactory contextFactory)
		{
			this.contextFactory = contextFactory;
			height = contextFactory.Height;
			width = contextFactory.Width;
			RgbaRaw = new byte[height * width * VideoConstants.BytesPerPixel];
			RgbaDelta = new byte[height * width * VideoConstants.BytesPerPixel];
			pixels = RgbaRaw.Length / 4;

			rasterBuffer = Image.CreateRasterBuffer(width, height, 3);
		}

		static FrameBlock()
		{
			colorModel = new ColorModel { ColorSpace = ColorSpace.YCbCr, Opaque = true };
			vSampleFactor = GetInverseSampleFactor(VideoConstants.VSampRatio);
			hSampleFactor = GetInverseSampleFactor(VideoConstants.HSampRatio);
		}
		#endregion

		#region Fields and Properties

		private static readonly ColorModel colorModel;
		private static readonly byte[] vSampleFactor;
		private static readonly byte[] hSampleFactor;
		private readonly ushort height;
		private readonly ushort width;
		private readonly byte[][][] rasterBuffer;
		private readonly int pixels;
		private readonly JpegFrameContextFactory contextFactory;

		public byte[] RgbaRaw { get; private set; }
		public byte[] RgbaDelta { get; private set; }
		public MemoryStream EncodedStream { get; set; }
		public double CumulativeDifferences { get; set; }
		public int FramesSinceSent { get; set; }
		// public int FrameNumber { get; set; }
		public byte BlockX { get; set; }
		public byte BlockY { get; set; }
		public BlockType BlockType { get; set; }

		private int referenceCount;

		/// <summary>
		/// The number of queues or other containers that hold a reference to the object. It is available for recycling when the reference count == 0.
		/// </summary>
		/// <remarks>
		/// This reference count is necessary because FrameBlocks are typically held by two containers: a queue which holds them to be transmitted,
		/// and an "oldFrame" which holds them so that they can be compared against blocks from the new frame.  And we don't know which of the
		/// containers a frameblock will be removed from last. So we need to recycle the frameblock whenever it's removed from one of the containers
		/// AND its reference count is zero.
		/// </remarks>
		public int ReferenceCount
		{
			get
			{
				return referenceCount;
			}
			set
			{
				referenceCount = value;
				if (referenceCount < 0 || referenceCount > 5)
				{
					ClientLogger.Debug("Unexpected reference count: {0}", referenceCount);
				}
			}
		}

		public string Source { get; set; }

		private const byte alphaValue = 0xFF;
		private const int yIndex = 0;
		private const int cbIndex = 1;
		private const int crIndex = 2;

		public byte JpegQuality { get; set; }
		public byte AverageR { get; private set; }
		public byte AverageG { get; private set; }
		public byte AverageB { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Converts the XSampRatio sampling ratio into a sampling factor.
		/// </summary>
		/// <param name="sampleFactor">A sampling ratio, e.g., {2, 1, 1}</param>
		/// <returns>A sampling factor, e.g., {1, 2, 2}</returns>
		/// <remarks>
		/// The Jpeg compression libraries use a sampling ratio, e.g., {2, 1, 1} means that
		/// raster component 0 should be twice as large (in that dimension) as raster components 1 and 2.
		/// However, when actually performing the subsampling, it's more helpful to have these expressed
		/// as the number of bytes to move in each direction, e.g., {1, 2, 2}.
		/// </remarks>
		private static byte[] GetInverseSampleFactor(byte[] sampleFactor)
		{
			var inverse = new byte[sampleFactor.Length];
			byte max = byte.MinValue;
			for (byte i = 0; i < sampleFactor.Length; i++)
			{
				max = Math.Max(sampleFactor[i], max);
			}
			for (byte i = 0; i < sampleFactor.Length; i++)
			{
				inverse[i] = (byte)(max / sampleFactor[i]);
			}
			return inverse;
		}

		public void CalculateAverageColor()
		{
			int totalR = 0, totalG = 0, totalB = 0;
			for (int i = 0; i < RgbaRaw.Length; i += 2)
			{
				totalB += RgbaRaw[i++];
				totalG += RgbaRaw[i++];
				totalR += RgbaRaw[i];
			}
			AverageR = (byte)(totalR / pixels);
			AverageG = (byte)(totalG / pixels);
			AverageB = (byte)(totalB / pixels);
		}

		private const byte byteoffset = 127;

		/// <summary>
		/// Subtracts the current block from the old block and calculates the average color distance between the two
		/// </summary>
		/// <param name="oldBlock">The old block</param>
		/// <param name="lowendCutoff">The minimum amount of distance allowed before we stop comparing blocks (i.e., no need to send this block)</param>
		/// <param name="highendCutoff">The max amount of delta allowed before we stop comparing blocks (i.e., we won't send this block delta-encoded</param>
		/// <returns>The average color distance between the two blocks</returns>
		public double SubtractFrom(FrameBlock oldBlock, double lowendCutoff, double highendCutoff)
		{
			if (oldBlock == null)
			{
				return 0.0f;
			}

			var oldFrame = oldBlock.RgbaRaw;
			int totalDistance = 0;
			float averageDistance = 0.0f;
			const int segments = 5;
			int pixelsProcessed = 0;
			for (int pixelOffset = 0; pixelOffset < segments; pixelOffset++)
			{
				// Only calculate the color distance on the first segment.
				if (pixelOffset == 0)
				{
					for (int index = pixelOffset * VideoConstants.BytesPerPixel; index < RgbaRaw.Length; index += (segments - 1) * VideoConstants.BytesPerPixel)
					{
						byte b = RgbaRaw[index];
						byte oldB = oldFrame[index];
						RgbaDelta[index] = EncodeDelta(oldB, b);
						index++;
						byte g = RgbaRaw[index];
						byte oldG = oldFrame[index];
						RgbaDelta[index] = EncodeDelta(oldG, g);
						index++;
						byte r = RgbaRaw[index];
						byte oldR = oldFrame[index];
						RgbaDelta[index] = EncodeDelta(oldR, r);
						index += 2;
						totalDistance += VideoHelper.GetColorDistance(r, g, b, oldR, oldG, oldB); ; // Square it so that pixels with more color distance will be more likely to trigger a retransmission
						pixelsProcessed++;
					}
					averageDistance = totalDistance / (float)pixelsProcessed;
					if (averageDistance < lowendCutoff || averageDistance > highendCutoff) break;
				}
				else
				{
					for (int index = pixelOffset * VideoConstants.BytesPerPixel; index < RgbaRaw.Length; index += (segments - 1) * VideoConstants.BytesPerPixel)
					{
						byte b = RgbaRaw[index];
						RgbaDelta[index] = EncodeDelta(oldFrame[index], b);
						index++;
						byte g = RgbaRaw[index];
						RgbaDelta[index] = EncodeDelta(oldFrame[index], g);
						index++;
						byte r = RgbaRaw[index];
						RgbaDelta[index] = EncodeDelta(oldFrame[index], r);
						index += 2;
					}
				}
			}
			return averageDistance;
		}

		/// <summary>
		/// Populates the current raw block by adding the current delta to the old raw block.
		/// </summary>
		/// <param name="oldBlock">The old block</param>
		public void AddTo(FrameBlock oldBlock)
		{
			if (oldBlock != null)
			{
				var oldFrame = oldBlock.RgbaRaw;
				for (int i = 0; i < RgbaDelta.Length; i++)
				{
					RgbaRaw[i] = i % 4 != 3 ? ReconstructOriginal(oldFrame[i], RgbaDelta[i]) : (byte)0xFF;
				}
			}
			else
			{
				// If we don't have anything to construct the frame against, just copy the delta to the RgbaRaw buffer.
				Buffer.BlockCopy(RgbaDelta, 0, RgbaRaw, 0, RgbaDelta.Length);
			}
		}

		public static byte EncodeDelta(byte oldByte, byte newByte)
		{
			// (1) Subtract the new byte from the old byte, e.g., 10 - 250 = -240;
			// (2) Add 127 to it so that the numbers from step two (which can be expected to cluster around -10 to 10) will cluster around 127 rather than 0-10 and 245-255,
			//     e.g., -240 + 127 = -113
			// (2) Divide the result in two so that we can encode it in a byte with only minor loss of fidelity, e.g., -113 >> 1 = -57.
			// (4) Convert the int to a byte and return it, e.g., (byte)-57 = 199.
			if (newByte == 255) newByte = 254;
			var delta = oldByte - newByte;
			var offsetdelta = delta + byteoffset;
			var bitshifted = offsetdelta >> 1;
			return (byte)bitshifted;
		}

		public static byte ReconstructOriginal(byte oldByte, byte encoded)
		{
			// Reverse the steps above, with a few tweaks to deal with wraparound issues:
			// (1) Convert the byte to a signed integer, e.g., 128=128, 199=-57, 192=-64, 63=63. 
			//		The way the math works, we only want to convert it to an sbyte if it's > 191, e.g., sbyte.MaxValue + (sbyte.MaxValue >> 1)
			// (2) Double the result, e.g., -57 << 1 = -114.
			// (3) Subtract 127 from the result, e.g., -114 - 127 = -241.
			// (4) Subtract the result from the old byte, e.g., 10 - (-241) = 250 (the original "new byte" above).
			// (5) Clip the resulting value (rather than overflow).
			int encodedsbyte = encoded > 191 ? (int)(sbyte)encoded : encoded;
			var bitshifted = encodedsbyte << 1;
			var offsetdelta = bitshifted - byteoffset;
			var original = oldByte - offsetdelta;
			if (original < byte.MinValue)
			{
				return byte.MinValue;
			}
			if (original > byte.MaxValue)
			{
				return byte.MaxValue;
			}
			return (byte)original;
		}

		public void Encode(byte jpegQuality)
		{
			JpegQuality = jpegQuality;
			LoadSubsampledRaster(BlockType == BlockType.Jpeg ? RgbaRaw : RgbaDelta);
			var image = new Image(colorModel, rasterBuffer);
			EncodedStream = new MemoryStream();
			var ctx = contextFactory.GetJpegFrameContext(JpegQuality, BlockType);
			var encoder = new JpegFrameEncoder(image, EncodedStream, ctx);
			encoder.Encode();
		}

		public void Decode()
		{
			// Get the raster image.
			Debug.Assert(JpegQuality > 0 && JpegQuality <= 100);
			EncodedStream.Position = 0;
			var ctx = contextFactory.GetJpegFrameContext(JpegQuality, BlockType);
			var decoder = new JpegFrameDecoder(EncodedStream, ctx);
			decoder.Decode(rasterBuffer);
			GetUpsampledRgba(rasterBuffer, BlockType == BlockType.Jpeg ? RgbaRaw : RgbaDelta);
		}

		private void LoadSubsampledRaster(byte[] rgbaBuffer)
		{
			int rgbaPos = 0;
			for (short y = 0; y < height; y++)
			{
				int yy = y / vSampleFactor[yIndex];
				int cby = y / vSampleFactor[cbIndex];
				int cry = y / vSampleFactor[crIndex];
				int yx = 0, cbx = 0, crx = 0;
				for (short x = 0; x < width; x++)
				{
					// Convert to YCbCr colorspace.
					// The order of bytes is (oddly enough) BGRA
					byte b = rgbaBuffer[rgbaPos++];
					byte g = rgbaBuffer[rgbaPos++];
					byte r = rgbaBuffer[rgbaPos++];
					YCbCr.fromRGB(ref r, ref g, ref b);

					// Only include the byte in question in the raster if it matches the appropriate sampling factor.
					if (IncludeInSample(yIndex, x, y))
					{
						rasterBuffer[yIndex][yx++][yy] = r;
					}
					if (IncludeInSample(cbIndex, x, y))
					{
						rasterBuffer[cbIndex][cbx++][cby] = g;
					}
					if (IncludeInSample(crIndex, x, y))
					{
						rasterBuffer[crIndex][crx++][cry] = b;
					}

					// For YCbCr, we ignore the Alpha byte of the RGBA byte structure, so advance beyond it.
					rgbaPos++;
				}
			}
		}

		static private bool IncludeInSample(int slice, short x, short y)
		{
			// Presumably this will get inlined . . . 
			return ((x % hSampleFactor[slice]) == 0) && ((y % vSampleFactor[slice]) == 0);
		}

		private void GetUpsampledRgba(byte[][][] raster, byte[] frame)
		{
			// Convert the three-layer raster image to RGBA format.
			int pos = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int posBlue = pos++;
					int posGreen = pos++;
					int posRed = pos++;
					frame[posRed] = raster[yIndex][x][y];
					frame[posGreen] = raster[cbIndex][x][y];
					frame[posBlue] = raster[crIndex][x][y];
					YCbCr.toRGB(ref frame[posRed], ref frame[posGreen], ref frame[posBlue]);
					frame[pos++] = alphaValue;
				}
			}
		}

		#endregion
	}
}
