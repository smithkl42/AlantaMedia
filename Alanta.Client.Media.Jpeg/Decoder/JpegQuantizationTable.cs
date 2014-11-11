// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using System.Collections.Generic;

namespace Alanta.Client.Media.Jpeg.Decoder
{
	public class JpegQuantizationTable
	{
		// The table entries, stored in natural order.
		public int[] Table { get; private set; }

		/// <summary>
		/// The standard JPEG luminance quantization table.  
		/// Values are stored in natural order.
		/// </summary>
		public static readonly JpegQuantizationTable K1Luminance = new JpegQuantizationTable(new[]
		{
			16, 11, 10, 16,  24,  40,  51,  61,
			12, 12, 14, 19,  26,  58,  60,  55,
			14, 13, 16, 24,  40,  57,  69,  56,
			14, 17, 22, 29,  51,  87,  80,  62,
			18, 22, 37, 56,  68, 109, 103,  77,
			24, 35, 55, 64,  81, 104, 113,  92,
			49, 64, 78, 87, 103, 121, 120, 101,
			72, 92, 95, 98, 112, 100, 103,  99
		}, false);

		/// <summary>
		/// A special-case quantization table used for encoding delta information (aka p-frames).
		/// </summary>
		public static readonly JpegQuantizationTable Delta = new JpegQuantizationTable(new []
		{
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 
		});

		/// <summary>
		/// The standard JPEG luminance quantization table, scaled by
		/// one-half.  Values are stored in natural order.
		/// </summary>
		public static JpegQuantizationTable K1Div2Luminance = K1Luminance.getScaledInstance(0.5f, true);

		/// <summary>
		/// The standard JPEG chrominance quantization table.  Values are
		/// stored in natural order.
		/// </summary>
		public static JpegQuantizationTable K2Chrominance = new JpegQuantizationTable(new[]
		{
			17, 18, 24, 47, 99, 99, 99, 99,
			18, 21, 26, 66, 99, 99, 99, 99,
			24, 26, 56, 99, 99, 99, 99, 99,
			47, 66, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99
		}, false);

		/// <summary>
		/// The standard JPEG chrominance quantization table, scaled by
		/// one-half.  Values are stored in natural order.
		/// </summary>
		public static JpegQuantizationTable K2Div2Chrominance = K2Chrominance.getScaledInstance(0.5f, true);

		/// <summary>
		/// Construct a new JPEG quantization table.  A copy is created of
		/// the table argument.
		/// </summary>
		/// <param name="table">The 64-element value table, stored in natural order</param>
		public JpegQuantizationTable(int[] table)
			: this(checkTable(table), true)
		{
		}

		/// <summary>
		/// Private constructor that avoids unnecessary copying and argument checking.
		/// </summary>
		/// <param name="table">the 64-element value table, stored in natural order</param>
		/// <param name="copy">true if a copy should be created of the given table</param>
		private JpegQuantizationTable(int[] table, bool copy)
		{
			if (copy)
			{
				Table = new int[table.Length];
				Buffer.BlockCopy(table, 0, Table, 0, table.Length * sizeof(int));
			}
			else
			{
				Table = table;
			}
			// ks 12/30/09 - Was:
			// this.table = copy ? (int[])table.Clone() : table;
		}

		private static int[] checkTable(int[] table)
		{
			if (table == null || table.Length != 64)
				throw new ArgumentException("Invalid JPEG quantization table");

			return table;
		}

		/// <summary>
		/// Retrieve a copy of this JPEG quantization table with every value
		/// scaled by the given scale factor, and clamped from 1 to 255
		/// </summary>
		/// <param name="scaleFactor">the factor by which to scale this table</param>
		/// <param name="forceBaseline"> clamp scaled values to a maximum of 255 if baseline or from 1 to 32767 otherwise.</param>
		/// <returns>new scaled JPEG quantization table</returns>
		public JpegQuantizationTable getScaledInstance(float scaleFactor, bool forceBaseline)
		{
			var scaledTable = new int[Table.Length];
			Buffer.BlockCopy(Table, 0, scaledTable, 0, Table.Length * sizeof(int));
			int max = forceBaseline ? 255 : 32767;

			for (int i = 0; i < scaledTable.Length; i++)
			{
				scaledTable[i] = (int)scaleFactor * scaledTable[i];
				if (scaledTable[i] < 1)
					scaledTable[i] = 1;
				else if (scaledTable[i] > max)
					scaledTable[i] = max;
			}

			return new JpegQuantizationTable(scaledTable, false);
		}

		private static readonly Dictionary<int[], JpegQuantizationTable> cache = new Dictionary<int[], JpegQuantizationTable>();
		public static JpegQuantizationTable GetJpegQuantizationTable(int[] quantumData)
		{
			JpegQuantizationTable table;
			lock (cache)
			{
				if (!cache.TryGetValue(quantumData, out table))
				{
					// The quantData comes out of the JPEG image in zig-zag format, and if the quantization step takes place before
					// the IDCT/unzigzag step (normal in this implementation), the quantization tables should remain in zig-zag format.  
					// However, if the quantization takes place during the IDCT (as in the AAN IDCT implementation), the quantization tables
					// need to be zigzagged back to normal order.
					if (JpegConstants.SelectedIdct != IdctImplementation.AAN)
					{
						var zigZagQuantumData = new int[quantumData.Length];

						for (int j = 0; j < 64; j++)
						{
							zigZagQuantumData[j] = quantumData[ZigZag.ZigZagMap[j]];
						}
						// ZigZag.UnZigZag(quantumData, zigZagQuantumData);
						table = new JpegQuantizationTable(zigZagQuantumData);
					}
					else
					{
						table = new JpegQuantizationTable(quantumData);
					}
					cache.Add(quantumData, table);
				}
			}
			return table;
		}
	}
}
