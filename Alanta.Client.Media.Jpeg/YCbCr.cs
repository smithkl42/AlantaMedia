// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.
// Heavily modified by Ken Smith for Alanta

namespace Alanta.Client.Media.Jpeg
{
	public class YCbCr
	{
		private const int maxLookupValue = byte.MaxValue + 1;

		private static readonly short[] crLookup1 = new short[maxLookupValue];
		private static readonly short[] crLookup2 = new short[maxLookupValue];
		private static readonly short[] cbLookup1 = new short[maxLookupValue];
		private static readonly short[] cbLookup2 = new short[maxLookupValue];

		private static readonly short[] rLookup1 = new short[maxLookupValue];
		private static readonly short[] rLookup2 = new short[maxLookupValue];
		private static readonly short[] rLookup3 = new short[maxLookupValue];
		private static readonly short[] gLookup1 = new short[maxLookupValue];
		private static readonly short[] gLookup2 = new short[maxLookupValue];
		private static readonly short[] gLookup3 = new short[maxLookupValue];
		private static readonly short[] bLookup1 = new short[maxLookupValue];
		private static readonly short[] bLookup2 = new short[maxLookupValue];
		private static readonly short[] bLookup3 = new short[maxLookupValue];

		static YCbCr()
		{
			// Fill lookup tables associated with YCbCr to RGB conversion
			FillLookupTable(crLookup1, -128, 1.402f);
			FillLookupTable(crLookup2, -128, 0.71414f);
			FillLookupTable(cbLookup1, -128, 0.34414f);
			FillLookupTable(cbLookup2, -128, 1.722f);

			// Fill lookup tables associated with RGB to YCbCr conversion
			FillLookupTable(rLookup1, 0, 0.299f);
			FillLookupTable(rLookup2, 0, -0.16874f);
			FillLookupTable(rLookup3, 0, 0.5f);
			FillLookupTable(gLookup1, 0, 0.587f);
			FillLookupTable(gLookup2, 0, -0.33126f);
			FillLookupTable(gLookup3, 0, -0.41869f);
			FillLookupTable(bLookup1, 0, 0.114f);
			FillLookupTable(bLookup2, 0, 0.5f);
			FillLookupTable(bLookup3, 0, -0.08131f);
		}

		/// <summary>
		/// Precomputes a lookup table to cache relatively slow floating point calculations.
		/// </summary>
		/// <param name="table">The array to be filled</param>
		/// <param name="offset">An offset to be added to each index</param>
		/// <param name="coefficient">A coefficient with which each index + offset is multiplied</param>
		private static void FillLookupTable(short[] table, int offset, float coefficient)
		{
			for (int i = 0; i < table.Length; i++)
			{
				table[i] = (short)((i + offset) * coefficient);
			}
		}

		/// <summary>
		/// Converts from YCbCr to RGB color formats using a precomputed lookup table to avoid floating point calculations. Runs about 40% faster.
		/// </summary>
		/// <param name="c1">The Y param (to be converted to R)</param>
		/// <param name="c2">The Cb param (to be converted to G)</param>
		/// <param name="c3">The Cr param (to be converted to B)</param>
		/// <remarks>
		/// Because of rounding issues, this doesn't return *exactly* the same values as the toRGBSlow() method, but they should be +/- 1, which is generally close enough.
		/// </remarks>
		public static void toRGB(ref byte c1, ref byte c2, ref byte c3)
		{
			int r = c1 + crLookup1[c3];
			int g = c1 - cbLookup1[c2] - crLookup2[c3];
			int b = c1 + cbLookup2[c2];

			c1 = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + r];
			c2 = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + g];
			c3 = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + b];
		}

		/// <summary>
		/// A relatively slow but accurate conversion from a YCbCr to an RGB colorspace.
		/// </summary>
		public static void toRGBSlow(ref byte c1, ref byte c2, ref byte c3)
		{
			float dY = c1;
			float dCb = c2 - 128f;
			float dCr = c3 - 128f;

			float dR = dY + 1.402f * dCr;
			float dG = dY - 0.34414f * dCb - 0.71414f * dCr;
			float dB = dY + 1.772f * dCb;

			c1 = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + (int)dR];
			c2 = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + (int)dG];
			c3 = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + (int)dB];
		}

		/// <summary>
		/// Converts from RGB to YCbCr colorspace using a precomputed lookup table to avoid floating point calculations.
		/// </summary>
		/// <param name="c1">The R param (to be converted to Y)</param>
		/// <param name="c2">The G param (to be converted to Cb)</param>
		/// <param name="c3">The B param (to be converted to Cr)</param>
		/// <remarks>
		/// Because of rounding issues, this doesn't return *exactly* the same values as the fromRGBSlow() method, but they should be +/- 1, which is generally close enough.
		/// This method runs faster in debug mode, but slower in prod. Probably this is because the lookup tables don't fit in the processor's cache, and it has to do
		/// wait for memory fetches to return, which is slower than just doing the floating point calculations in the first place.
		/// </remarks>
		public static void fromRGBSlow(ref byte c1, ref byte c2, ref byte c3)
		{
			byte r = c1;
			byte g = c2;
			byte b = c3;

			c1 = (byte)(rLookup1[r] + gLookup1[g] + bLookup1[b]);
			c2 = (byte)(rLookup2[r] + gLookup2[g] + bLookup2[b] + 128);
			c3 = (byte)(rLookup3[r] + gLookup3[g] + bLookup3[b] + 128);
		}

		/// <summary>
		/// An accurate conversion from an RGB to a YCbCr colorspace.
		/// </summary>
		/// <remarks>
		///  This method runs slower in debug mode, but is about 15% faster in prod.
		/// </remarks>
		public static void fromRGB(ref byte c1, ref byte c2, ref byte c3)
		{
			float dR = c1;
			float dG = c2;
			float dB = c3;

			c1 = (byte)(0.299f * dR + 0.587f * dG + 0.114f * dB);
			c2 = (byte)(-0.16874f * dR - 0.33126f * dG + 0.5f * dB + 128f);
			c3 = (byte)(0.5f * dR - 0.41869f * dG - 0.08131f * dB + 128f);
		}

		///* RGB to YCbCr range 0-255 */
		//public static void fromRGB(byte[] rgb, byte[] ycbcr)
		//{
		//    ycbcr[0] = (byte)((0.299 * (float)rgb[0] + 0.587 * (float)rgb[1] + 0.114 * (float)rgb[2]));
		//    ycbcr[1] = (byte)(128 + (byte)((-0.16874 * (float)rgb[0] - 0.33126 * (float)rgb[1] + 0.5 * (float)rgb[2])));
		//    ycbcr[2] = (byte)(128 + (byte)((0.5 * (float)rgb[0] - 0.41869 * (float)rgb[1] - 0.08131 * (float)rgb[2])));
		//}
		/* RGB to YCbCr range 0-255 */
		public static float[] fromRGB(float[] data)
		{
			var dest = new float[3];

			dest[0] = 0.299f * data[0] + 0.587f * data[1] + 0.114f * data[2];
			dest[1] = 128 + ((-0.16874f * data[0] - 0.33126f * data[1] + 0.5f * data[2]));
			dest[2] = 128 + ((0.5f * data[0] - 0.41869f * data[1] - 0.08131f * data[2]));

			return (dest);
		}
	}
}