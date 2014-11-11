// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.
//
// Partially derives from a Java encoder, JpegEncoder.java by James R Weeks.
// Implements Baseline JPEG Encoding http://www.opennet.ru/docs/formats/jpeg.txt

using System;
using System.IO;
using Alanta.Client.Media.Jpeg.Decoder;
using BinaryWriter = Alanta.Client.Media.Jpeg.IO.BinaryWriter;

namespace Alanta.Client.Media.Jpeg.Encoder
{
	public class JpegEncodeProgressChangedArgs : EventArgs
	{
		public double EncodeProgress; // 0.0 to 1.0
	}

	public class JpegEncoder
	{
		private const int Ss = 0;
		private const int Se = 63;
		private const int Ah = 0;
		private const int Al = 0;
		private readonly DCT dct;

		private readonly int height;
		private readonly HuffmanTable huf;
		private readonly DecodedJpeg input;
		private readonly Stream outStream;
		private readonly short quality;
		private readonly int width;

		private readonly float[][] dctArray1 = JaggedArrayHelper.Create2DJaggedArray<float>(8, 8); // new float[8, 8];
		private readonly float[][] dctArray2 = JaggedArrayHelper.Create2DJaggedArray<float>(8, 8); // new float[8, 8];
		private readonly int[] dctArray3 = new int[8 * 8];
		private JpegEncodeProgressChangedArgs progress;

		public JpegEncoder(Image image, short quality, Stream outStream)
			: this(new DecodedJpeg(image), quality, outStream)
		{
			/* see overload */
		}

		/// <summary>
		/// Encodes a JPEG, preserving the colorspace and metadata of the input JPEG.
		/// </summary>
		/// <param name="decodedJpeg">Decoded Jpeg to start with.</param>
		/// <param name="quality">Quality of the image from 0 to 100.  (Compression from max to min.)</param>
		/// <param name="outStream">Stream where the result will be placed.</param>
		public JpegEncoder(DecodedJpeg decodedJpeg, short quality, Stream outStream)
		{
			input = decodedJpeg;

			/* This encoder requires YCbCr */
			input.Image.ChangeColorSpace(ColorSpace.YCbCr);

			this.quality = quality;

			height = input.Image.Height;
			width = input.Image.Width;
			this.outStream = outStream;
			dct = new DCT(quality);
			huf = HuffmanTable.GetHuffmanTable(null);
		}

		public void Encode()
		{
			progress = new JpegEncodeProgressChangedArgs();

			WriteHeaders();
			CompressTo(outStream);
			WriteMarker(new byte[] { 0xFF, 0xD9 }); // End of Image

			progress.EncodeProgress = 1.0;

			outStream.Flush();
		}

		internal void WriteHeaders()
		{
			int i, j;

			// Start of Image
			byte[] SOI = { 0xFF, 0xD8 };
			WriteMarker(SOI);

			if (!input.HasJFIF) // Supplement JFIF if missing
			{
				var JFIF = new byte[18]
				{
					0xff, 0xe0,
					0x00, 0x10,
					0x4a, 0x46,
					0x49, 0x46,
					0x00, 0x01,
					0x00, 0x00,
					0x00, 0x01,
					0x00, 0x01,
					0x00, 0x00
				};

				WriteArray(JFIF);
			}

			var writer = new BinaryWriter(outStream);

			/* APP headers and COM headers follow the same format 
			 * which has a 16-bit integer length followed by a block
			 * of binary data. */
			foreach (JpegHeader header in input.MetaHeaders)
			{
				writer.Write(JpegMarker.XFF);
				writer.Write(header.Marker);

				// Header's length
				writer.Write((short)(header.Data.Length + 2));
				writer.Write(header.Data);
			}

			// The DQT header
			// 0 is the luminance index and 1 is the chrominance index
			var DQT = new byte[134];
			DQT[0] = JpegMarker.XFF;
			DQT[1] = JpegMarker.DQT;
			DQT[2] = 0x00;
			DQT[3] = 0x84;
			int offset = 4;
			for (i = 0; i < 2; i++)
			{
				DQT[offset++] = (byte)((0 << 4) + i);
				int[] tempArray = dct.quantum[i];

				for (j = 0; j < 64; j++)
				{
					DQT[offset++] = (byte)tempArray[ZigZag.ZigZagMap[j]];
				}
			}

			WriteArray(DQT);

			// Start of Frame Header ( Baseline JPEG )
			var SOF = new byte[19];
			SOF[0] = JpegMarker.XFF;
			SOF[1] = JpegMarker.SOF0;
			SOF[2] = 0x00;
			SOF[3] = 17;
			SOF[4] = (byte)input.Precision;
			SOF[5] = (byte)((input.Image.Height >> 8) & 0xFF);
			SOF[6] = (byte)((input.Image.Height) & 0xFF);
			SOF[7] = (byte)((input.Image.Width >> 8) & 0xFF);
			SOF[8] = (byte)((input.Image.Width) & 0xFF);
			SOF[9] = (byte)input.Image.ComponentCount;
			int index = 10;

			for (i = 0; i < SOF[9]; i++)
			{
				SOF[index++] = FrameDefaults.CompId[i];
				SOF[index++] = (byte)((input.HSampFactor[i] << 4) + input.VSampFactor[i]);
				SOF[index++] = FrameDefaults.QtableNumber[i];
			}

			WriteArray(SOF);

			// The DHT Header
			index = 4;
			int oldindex = 4;
			var DHT1 = new byte[17];
			var DHT4 = new byte[4];
			DHT4[0] = JpegMarker.XFF;
			DHT4[1] = JpegMarker.DHT;
			for (i = 0; i < 4; i++)
			{
				int bytes = 0;

				//  top 4 bits: table class (0=DC, 1=AC)
				//  bottom 4: index (0=luminance, 1=chrominance)
				byte huffmanInfo = (i == 0) ? (byte)0x00 :
															(i == 1) ? (byte)0x10 :
																					(i == 2) ? (byte)0x01 : (byte)0x11;

				DHT1[index++ - oldindex] = huffmanInfo;

				for (j = 0; j < 16; j++)
				{
					int temp = huf.bitsList[i][j];
					DHT1[index++ - oldindex] = (byte)temp;
					bytes += temp;
				}

				int intermediateindex = index;
				var DHT2 = new byte[bytes];
				for (j = 0; j < bytes; j++)
				{
					DHT2[index++ - intermediateindex] = (byte)huf.val[i][j];
				}
				var DHT3 = new byte[index];
				Array.Copy(DHT4, 0, DHT3, 0, oldindex);
				Array.Copy(DHT1, 0, DHT3, oldindex, 17);
				Array.Copy(DHT2, 0, DHT3, oldindex + 17, bytes);
				DHT4 = DHT3;
				oldindex = index;
			}
			DHT4[2] = (byte)(((index - 2) >> 8) & 0xFF);
			DHT4[3] = (byte)((index - 2) & 0xFF);
			WriteArray(DHT4);

			// Start of Scan Header
			var SOS = new byte[14];
			SOS[0] = JpegMarker.XFF;
			SOS[1] = JpegMarker.SOS;
			SOS[2] = 0x00;
			SOS[3] = 12;
			SOS[4] = (byte)input.Image.ComponentCount;

			index = 5;

			for (i = 0; i < SOS[4]; i++)
			{
				SOS[index++] = FrameDefaults.CompId[i];
				SOS[index++] = (byte)((FrameDefaults.DCtableNumber[i] << 4) + FrameDefaults.ACtableNumber[i]);
			}

			SOS[index++] = Ss;
			SOS[index++] = Se;
			SOS[index] = ((Ah << 4) + Al);
			WriteArray(SOS);
		}

		internal void CompressTo(Stream outStream)
		{
			int comp;

			var lastDCvalue = new int[input.Image.ComponentCount];

			var buffer = new HuffmanBuffer(outStream);

			// This initial setting of MinBlockWidth and MinBlockHeight is done to
			// ensure they start with values larger than will actually be the case.
			int minBlockWidth = ((width % 8 != 0) ? (int)(Math.Floor(width / 8.0) + 1) * 8 : width);
			int minBlockHeight = ((height % 8 != 0) ? (int)(Math.Floor(height / 8.0) + 1) * 8 : height);
			for (comp = 0; comp < input.Image.ComponentCount; comp++)
			{
				minBlockWidth = Math.Min(minBlockWidth, input.BlockWidth[comp]);
				minBlockHeight = Math.Min(minBlockHeight, input.BlockHeight[comp]);
			}

			for (int r = 0; r < minBlockHeight; r++)
			{
				for (int c = 0; c < minBlockWidth; c++)
				{
					int xpos = c * 8;
					int ypos = r * 8;
					for (comp = 0; comp < input.Image.ComponentCount; comp++)
					{
						byte[][] inputArray = input.Image.Raster[comp];

						for (int i = 0; i < input.VSampFactor[comp]; i++)
						{
							for (int j = 0; j < input.VSampFactor[comp]; j++)
							{
								int xblockoffset = j * 8;
								int yblockoffset = i * 8;
								for (int a = 0; a < 8; a++)
								{
									// set Y value.  check bounds
									int y = ypos + yblockoffset + a;
									if (y >= height)
									{
										break;
									}

									for (int b = 0; b < 8; b++)
									{
										int x = xpos + xblockoffset + b;
										if (x >= width)
										{
											break;
										}
										dctArray1[a][b] = inputArray[x][y];
									}
								}
								dct.DoDCT_AAN(dctArray1, dctArray2);
								dct.QuantizeBlock(dctArray2, FrameDefaults.QtableNumber[comp], dctArray3);
								huf.HuffmanBlockEncoder(buffer, dctArray3, lastDCvalue[comp], FrameDefaults.DCtableNumber[comp], FrameDefaults.ACtableNumber[comp]);
								lastDCvalue[comp] = dctArray3[0];
							}
						}
					}
				}
			}

			buffer.FlushBuffer();
		}


		private void WriteMarker(byte[] data)
		{
			outStream.Write(data, 0, 2);
		}

		private void WriteArray(byte[] data)
		{
			int length = ((data[2] & 0xFF) << 8) + (data[3] & 0xFF) + 2;
			outStream.Write(data, 0, length);
		}
	}
}