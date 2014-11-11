// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.
//
// Partially derives from a Java encoder, JpegEncoder.java by James R Weeks.
// Implements Baseline JPEG Encoding http://www.opennet.ru/docs/formats/jpeg.txt

// Modified by Ken Smith c. 12/2009 and later to implement just the portions of the encoder needed for our custom video codec.

using System;
using System.Diagnostics;
using System.IO;
using Alanta.Client.Media.Jpeg.Decoder;

namespace Alanta.Client.Media.Jpeg.Encoder
{
	public class JpegFrameEncoder
	{
		private readonly DecodedJpeg inputDecodedJpeg;
		private readonly Stream outStream;
		private readonly HuffmanTable huffmanTable;
		private readonly JpegFrameContext context;

		public JpegFrameEncoder(Image image, Stream outStream, JpegFrameContext context)
			: this(new DecodedJpeg(image), outStream, context) { /* see overload */ }

		/// <summary>
		/// Encodes a JPEG, preserving the colorspace and metadata of the input JPEG.
		/// </summary>
		/// <param name="decodedJpeg">Decoded Jpeg to start with.</param>
		/// <param name="outStream">Stream where the result will be placed.</param>
		/// <param name="context">The frame context</param>
		public JpegFrameEncoder(DecodedJpeg decodedJpeg, Stream outStream, JpegFrameContext context)
		{
			inputDecodedJpeg = decodedJpeg;

			/* This encoder requires YCbCr */
			inputDecodedJpeg.Image.ChangeColorSpace(ColorSpace.YCbCr);
			this.context = context;

			this.outStream = outStream;
			huffmanTable = context.HuffmanTable;
		}

		public void Encode()
		{
			CompressTo(outStream);
			outStream.Flush();
		}

		private void CompressTo(Stream outStream)
		{
			var lastDCvalue = new int[inputDecodedJpeg.Image.ComponentCount];

			// int Width = 0, Height = 0;
			var buffer = new HuffmanBuffer(outStream);

			// This initial setting of MinBlockWidth and MinBlockHeight is done to
			// ensure they start with values larger than will actually be the case.
			int minBlockWidth = ((context.Width % 8 != 0) ? (int)(Math.Floor(context.Width / 8.0) + 1) * 8 : context.Width);
			int minBlockHeight = ((context.Height % 8 != 0) ? (int)(Math.Floor(context.Height / 8.0) + 1) * 8 : context.Height);
			for (int comp = 0; comp < inputDecodedJpeg.Image.ComponentCount; comp++)
			{
				minBlockWidth = Math.Min(minBlockWidth, inputDecodedJpeg.BlockWidth[comp]);
				minBlockHeight = Math.Min(minBlockHeight, inputDecodedJpeg.BlockHeight[comp]);
			}
			// xpos = 0;

			for (int r = 0; r < minBlockHeight; r++)
			{
				for (int c = 0; c < minBlockWidth; c++)
				{
					int xpos = c * 8;
					int ypos = r * 8;
					for (int comp = 0; comp < inputDecodedJpeg.Image.ComponentCount; comp++)
					{
						byte[][] inputArray = inputDecodedJpeg.Image.Raster[comp];

						for (int i = 0; i < inputDecodedJpeg.VSampFactor[comp]; i++)
						{
							for (int j = 0; j < inputDecodedJpeg.HSampFactor[comp]; j++)
							{
								int xblockoffset = j * 8;
								int yblockoffset = i * 8;
								for (int a = 0; a < 8; a++)
								{
									// set y value and check bounds
									int y = ypos + yblockoffset + a; 
									if (y >= context.Height) break;

									for (int b = 0; b < 8; b++)
									{
										// set x value and check bounds
										int x = xpos + xblockoffset + b;
										if (x >= context.Width) break;
										context.DctArray1[a][b] = inputArray[x][y];
									}
								}

								// This is the heart of the encoding process. Everything else up to here is just housecleaning.
								context.DCT.DoDCT_AAN(context.DctArray1, context.DctArray2);
								context.DCT.QuantizeBlock(context.DctArray2, FrameDefaults.QtableNumber[comp], context.dctArray3);
								huffmanTable.HuffmanBlockEncoder(buffer, context.dctArray3, lastDCvalue[comp], FrameDefaults.DCtableNumber[comp], FrameDefaults.ACtableNumber[comp]);
								lastDCvalue[comp] = context.dctArray3[0];
							}
						}
					}
				}
			}

			buffer.FlushBuffer();
		}

	}

}