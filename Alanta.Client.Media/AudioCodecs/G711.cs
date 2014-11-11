/*
* SpanDSP - a series of DSP components for telephony
*
* g711.h - In line A-law and u-law conversion routines
*
* Written by Steve Underwood <steveu@coppice.org>
*
* Copyright (C) 2001 Steve Underwood
*
*  Despite my general liking of the GPL, I place this code in the
*  public domain for the benefit of all mankind - even the slimy
*  ones who might try to proprietize my work and use it to my
*  detriment.
*
* $Id: g711.h,v 1.1 2006/06/07 15:46:39 steveu Exp $
*/

namespace Alanta.Client.Media.AudioCodecs
{
	/// <summary>
	/// Provides simple static methods to perform G711 encoding and decoding.
	/// </summary>
	///	<remarks>
	/// Lookup tables for A-law and u-law look attractive, until you consider the impact
	/// on the CPU cache. If it causes a substantial area of your processor cache to get
	/// hit too often, cache sloshing will severely slow things down. The main reason
	/// these routines are slow in C, is the lack of direct access to the CPU's "find
	/// the first 1" instruction. A little in-line assembler fixes that, and the
	/// conversion routines can be faster than lookup tables, in most real world usage.
	/// A "find the first 1" instruction is available on most modern CPUs, and is a
	/// much underused feature.
	/// If an assembly language method of bit searching is not available, these routines
	/// revert to a method that can be a little slow, so the cache thrashing might not
	/// seem so bad :(
	/// ks 9/23/11 - Added LinearToULawFast() and ULawToLinearFast() methods that use
	/// lookup tables instead of calculating each value on the fly (since the assember calls
	/// referenced above aren't available in Silverlight). This results in a 2-3x performance
	/// improvement vs. calculating the values on the fly.
	/// </remarks>
	public static class G711
	{

		#region Constructors
		static G711()
		{
			linearToULawLookup = new byte[ushort.MaxValue + 1];
			for (int i = short.MinValue; i <= short.MaxValue; i++)
			{
				linearToULawLookup[i + short.MaxValue + 1] = LinearToULaw((short)i);
			}

			uLawToLinearLookup = new short[byte.MaxValue + 1];
			for (int i = byte.MinValue; i <= byte.MaxValue; i++)
			{
				uLawToLinearLookup[i] = ULawToLinear((byte)i);
			}
		}
		#endregion

		#region Fields and Properties
		private static readonly byte[] linearToULawLookup;
		private static readonly short[] uLawToLinearLookup;
		private const int alawAmiMask = 0x55;
		private const int ulawBias = 0x84;   /* Bias for linear code. */
		#endregion

		#region Methods

		public static byte LinearToULawFast(short linear)
		{
			return linearToULawLookup[linear + short.MaxValue + 1];
		}

		public static short ULawToLinearFast(byte ulaw)
		{
			return uLawToLinearLookup[ulaw];
		}

		/// <summary>
		/// Encode a linear sample to u-law
		/// </summary>
		/// <param name="linear">The sample to encode</param>
		/// <returns>The u-law value</returns>
		public static byte LinearToULaw(short linear)
		{
			byte uVal;
			int mask;

			/* Get the sign and the magnitude of the value. */
			if (linear < 0)
			{
				linear = (short)(ulawBias - linear);
				mask = 0x7F;
			}
			else
			{
				linear = (short)(ulawBias + linear);
				mask = 0xFF;
			}

			int seg = GetTopBit((uint)(linear | 0xFF)) - 7;

			/*
			* Combine the sign, segment, quantization bits,
			* and complement the code word.
			*/
			if (seg >= 8)
				uVal = (byte)(0x7F ^ mask);
			else
				uVal = (byte)(((seg << 4) | ((linear >> (seg + 3)) & 0xF)) ^ mask);
			/* Optional ITU trap */
			if (uVal == 0)
				uVal = 0x02;
			return uVal;
		}

		/// <summary>
		/// Decode a u-law sample to a linear value
		/// </summary>
		/// <param name="ulaw">The u-law sample to decode</param>
		/// <returns>The linear value</returns>
		public static short ULawToLinear(byte ulaw)
		{
			/* Complement to obtain normal u-law value. */
			ulaw = (byte)~ulaw;
			/*
			* Extract and bias the quantization bits. Then
			* shift up by the segment number and subtract out the bias.
			*/
			int t = (((ulaw & 0x0F) << 3) + ulawBias) << ((ulaw & 0x70) >> 4);
			return (short)((ulaw & 0x80) != 0 ? (ulawBias - t) : (t - ulawBias));
		}

		/// <summary>
		/// Encode a linear sample to A-law
		/// </summary>
		/// <param name="linear">The sample to encode</param>
		/// <returns>The A-law value</returns>
		public static byte LinearToALaw(int linear)
		{
			int mask;

			if (linear >= 0)
			{
				/* Sign (bit 7) bit = 1 */
				mask = alawAmiMask | 0x80;
			}
			else
			{
				/* Sign (bit 7) bit = 0 */
				mask = alawAmiMask;
				linear = -linear - 8;
			}

			/* Convert the scaled magnitude to segment number. */
			int seg = GetTopBit((byte)(linear | 0xFF)) - 7;
			if (seg >= 8)
			{
				if (linear >= 0)
				{
					/* Out of range. Return maximum value. */
					return (byte)(0x7F ^ mask);
				}
				/* We must be just a tiny step below zero */
				return (byte)(0x00 ^ mask);
			}
			/* Combine the sign, segment, and quantization bits. */
			return (byte)(((seg << 4) | ((linear >> ((seg != 0) ? (seg + 3) : 4)) & 0x0F)) ^ mask);
		}

		/// <summary>
		/// Decode an A-law sample to a linear value.
		/// </summary>
		/// <param name="alaw">The A-law sample to decode</param>
		/// <returns>The linear value</returns>
		public static short ALawToLinear(byte alaw)
		{
			alaw ^= alawAmiMask;
			int i = ((alaw & 0x0F) << 4);
			int seg = ((alaw & 0x70) >> 4);
			if (seg != 0)
				i = (i + 0x108) << (seg - 1);
			else
				i += 8;
			return (short)((alaw & 0x80) != 0 ? i : -i);
		}

		// Assumes big-endian processor
		private static int GetTopBit(uint bits)
		{
			if (bits == 0) return -1;
			int i = 0;
			if ((bits & 0xFFFF0000) != 0)
			{
				bits &= 0xFFFF0000;
				i += 16;
			}
			if ((bits & 0xFF00FF00) != 0)
			{
				bits &= 0xFF00FF00;
				i += 8;
			}
			if ((bits & 0xF0F0F0F0) != 0)
			{
				bits &= 0xF0F0F0F0;
				i += 4;
			}
			if ((bits & 0xCCCCCCCC) != 0)
			{
				bits &= 0xCCCCCCCC;
				i += 2;
			}
			if ((bits & 0xAAAAAAAA) != 0)
			{
				i += 1;
			}
			return i;
		}

		private static int GetBottomBit(uint bits)
		{
			if (bits == 0)
				return -1;
			int i = 32;
			if ((bits & 0x0000FFFF) != 0)
			{
				bits &= 0x0000FFFF;
				i -= 16;
			}
			if ((bits & 0x00FF00FF) != 0)
			{
				bits &= 0x00FF00FF;
				i -= 8;
			}
			if ((bits & 0x0F0F0F0F) != 0)
			{
				bits &= 0x0F0F0F0F;
				i -= 4;
			}
			if ((bits & 0x33333333) != 0)
			{
				bits &= 0x33333333;
				i -= 2;
			}
			if ((bits & 0x55555555) != 0)
			{
				i -= 1;
			}
			return i;
		}

		public static short ToShort(short byte1, short byte2)
		{
			return (short)((byte2 << 8) + byte1);
		}

		// Assumes big-endian processor
		public static void FromShort(short number, out byte byte1, out byte byte2)
		{
			byte2 = (byte)(number >> 8);
			byte1 = (byte)(number & 255);
		}

		#endregion

	}
}
