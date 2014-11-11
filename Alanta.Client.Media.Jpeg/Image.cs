// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using System.Collections.Generic;
using System.Threading;
#if SILVERLIGHT
#else
using System.Drawing;
using System.Drawing.Imaging;
#endif

namespace Alanta.Client.Media.Jpeg
{
	public struct ColorModel
	{
		public ColorSpace ColorSpace;
		public bool Opaque;
	}

	public enum ColorSpace { Gray, YCbCr, Rgb }

	public class Image
	{
		#region Fields and Properties
		private ColorModel currentColorSpace;

		public byte[][][] Raster { get; private set; }
		public ColorModel ColorModel { get { return currentColorSpace; } }

		/// <summary> X density (dots per inch).</summary>
		public double DensityX { get; set; }
		/// <summary> Y density (dots per inch).</summary>
		public double DensityY { get; set; }

		public int ComponentCount { get { return Raster.Length; } }

		private readonly int width;
		private readonly int height;
		public int Width { get { return width; } }
		public int Height { get { return height; } }

		private const int red = 0;
		private const int green = 1;
		private const int blue = 2;
		private const int yIndex = 0;
		private const int cbIndex = 1;
		private const int crIndex = 2;
		#endregion

		#region Constructors
		public Image(ushort width, ushort height, byte[] rgbaFrame)
		{
			this.width = width;
			this.height = height;
			currentColorSpace = new ColorModel { ColorSpace = ColorSpace.YCbCr };
			Raster = GetRaster(width, height, 3);
			LoadRaster(rgbaFrame, Raster, width, height);
		}

		public Image(ColorModel cm, byte[][][] raster)
		{
			width = raster[0].Length;
			height = raster[0][0].Length;

			currentColorSpace = cm;
			Raster = raster;
		}
		#endregion

		#region Methods

		private static void LoadRaster(byte[] rgbaBuffer, byte[][][] rasterBuffer, ushort width, ushort height)
		{
			int rgbaPos = 0;
			for (short y = 0; y < height; y++)
			{
				for (short x = 0; x < width; x++)
				{
					// Convert to YCbCr colorspace.
					// The order of bytes is (oddly enough) BGRA
					byte b = rgbaBuffer[rgbaPos++];
					byte g = rgbaBuffer[rgbaPos++];
					byte r = rgbaBuffer[rgbaPos++];
					YCbCr.fromRGB(ref r, ref g, ref b);

					// Only include the byte in question in the raster if it matches the appropriate sampling factor.
					rasterBuffer[yIndex][x][y] = r;
					rasterBuffer[cbIndex][x][y] = g;
					rasterBuffer[crIndex][x][y] = b;

					// For YCbCr, we ignore the Alpha byte of the RGBA byte structure, so advance beyond it.
					rgbaPos++;
				}
			}
		}

		/// <summary>
		/// Converts the colorspace of an image (in-place)
		/// </summary>
		/// <param name="targetColorSpace">Colorspace to convert into</param>
		/// <returns>Self</returns>
		public Image ChangeColorSpace(ColorSpace targetColorSpace)
		{
			// Colorspace is already correct
			if (currentColorSpace.ColorSpace == targetColorSpace) return this;

			if (currentColorSpace.ColorSpace == ColorSpace.Rgb && targetColorSpace == ColorSpace.YCbCr)
			{
				/*
				 *  Y' =       + 0.299    * R'd + 0.587    * G'd + 0.114    * B'd
					Cb = 128   - 0.168736 * R'd - 0.331264 * G'd + 0.5      * B'd
					Cr = 128   + 0.5      * R'd - 0.418688 * G'd - 0.081312 * B'd
				 * 
				 */

				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						YCbCr.fromRGB(ref Raster[red][x][y], ref Raster[green][x][y], ref Raster[blue][x][y]);
					}
				}
				currentColorSpace.ColorSpace = ColorSpace.YCbCr;
			}
			else if (currentColorSpace.ColorSpace == ColorSpace.YCbCr && targetColorSpace == ColorSpace.Rgb)
			{
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						YCbCr.toRGB(ref Raster[yIndex][x][y], ref Raster[cbIndex][x][y], ref Raster[crIndex][x][y]);
					}
				}
				currentColorSpace.ColorSpace = ColorSpace.Rgb;
			}
			else if (currentColorSpace.ColorSpace == ColorSpace.Gray && targetColorSpace == ColorSpace.YCbCr)
			{
				// To convert to YCbCr, we just add two 128-filled chroma channels
				var cb = new byte[width][]; //, height];
				var cr = new byte[width][]; //, height];
				for (int x = 0; x < width; x++)
				{
					cb[x] = new byte[height];
					cr[x] = new byte[height];
					for (int y = 0; y < height; y++)
					{
						cb[x][y] = 128; cr[x][y] = 128;
					}
				}
				Raster = new[] { Raster[0], cb, cr };
				currentColorSpace.ColorSpace = ColorSpace.YCbCr;
			}
			else if (currentColorSpace.ColorSpace == ColorSpace.Gray && targetColorSpace == ColorSpace.Rgb)
			{
				ChangeColorSpace(ColorSpace.YCbCr);
				ChangeColorSpace(ColorSpace.Rgb);
			}
			else
			{
				throw new Exception("Colorspace conversion not supported.");
			}
			return this;
		}

		/// <summary>
		/// Creates a new raster image with the appropriate width, height and bands.
		/// </summary>
		/// <param name="width">The width of the raster image</param>
		/// <param name="height">The height of the raster image</param>
		/// <param name="bands">The number of bands in the raster image</param>
		/// <returns>A new raster image</returns>
		public static byte[][][] CreateRasterBuffer(int width, int height, int bands)
		{
			// Create the raster
			var raster = new byte[bands][][];
			for (int b = 0; b < bands; b++)
			{
				raster[b] = GetComponentArrays(width, height);
			}
			return raster;
		}

		public static byte[][] GetComponentArrays(int width, int height)
		{
			var component = new byte[width][];
			for (int i = 0; i < width; i++)
			{
				component[i] = new byte[height];
			}
			return component;
		}

		/// <summary>
		/// Gets a cached raster image with the appropriate width, height and bands.
		/// </summary>
		/// <param name="width">The width of the raster image</param>
		/// <param name="height">The height of the raster image</param>
		/// <param name="bands">The number of bands in the raster image</param>
		/// <returns>A cached raster image.</returns>
		public static byte[][][] GetRaster(ushort width, ushort height, short bands)
		{
			long rasterId = GetRasterId(height, width, bands);
			byte[][][] raster;
			if (!rasterDictionary.TryGetValue(rasterId, out raster))
			{
				raster = CreateRasterBuffer(width, height, bands);
				rasterDictionary.Add(rasterId, raster);
			}
			return raster;
		}

		private static Dictionary<long, byte[][][]> rasterDictionary = new Dictionary<long, byte[][][]>();

		private static long GetRasterId(ushort height, ushort width, short bands)
		{
			// Convert the various height/width/quality values into a long (Int64) value.
			var threadId = (ushort)Thread.CurrentThread.ManagedThreadId;
			long l = height |
				(long)width << 16 |
				(long)bands << 32 |
				(long)threadId << 48;
			return l;
		}

		// delegate void ConvertColor(ref byte c1, ref byte c2, ref byte c3);

#if SILVERLIGHT
#else
		public Bitmap ToBitmap()
		{
			ConvertColor ColorConverter;

			switch(_cm.colorspace)
			{
				case ColorSpace.YCbCr:
					ColorConverter = YCbCr.toRGB;
					break;
				default:
					throw new Exception("Colorspace not supported yet.");
			}

			int _width = width;
			int _height = height;

			Bitmap bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);

			BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				System.Drawing.Imaging.ImageLockMode.WriteOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			byte[] outColor = new byte[3];
			byte[] inColor = new byte[3];

			unsafe
			{
				int i = 0;

				byte* ptrBitmap = (byte*)bmData.Scan0;

				for (int y = 0; y < _height; y++)
				{
					for (int x = 0; x < _width; x++)
					{
						ptrBitmap[0] = (byte)_raster[0][x, y];
						ptrBitmap[1] = (byte)_raster[1][x, y];
						ptrBitmap[2] = (byte)_raster[2][x, y];

						ColorConverter(ref ptrBitmap[0], ref ptrBitmap[1], ref ptrBitmap[2]);

						// Swap RGB --> BGR
						byte R = ptrBitmap[0];
						ptrBitmap[0] = ptrBitmap[2];
						ptrBitmap[2] = R;

						ptrBitmap[3] = 255; /* 100% opacity */
						ptrBitmap += 4;     // advance to the next pixel
						i++;                // "
					}
				}
			}

			bitmap.UnlockBits(bmData);

			return bitmap;

		}
#endif
		#endregion

	}
}
