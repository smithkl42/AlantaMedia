// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Alanta.Client.Media.Jpeg.IO;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media.Jpeg.Decoder
{
	public class JpegComponent
	{
		#region Fields and Properties

		// Values that need to be reset with every decode run.
		private List<float[][][]> scanData;
		private List<byte[][]> scanDecoded;
		public float previousDC;

		// Buffer pools
		private readonly BufferPool<byte> scanDecodedBufferPool = new BufferPool<byte>(8);
		// private BufferPool<float> scanDataBufferPool = new BufferPool<float>(8);

		// Values that stay the same between runs
		public byte factorH, factorV, componentId, quantId;
		public int width, height;

		/// <summary>
		/// Scratch buffer to hold unzigzagged block values.
		/// </summary>
		readonly float[] unZZ = new float[64];

		private static readonly Dictionary<int[], QuantizeDel> quantizeDelegates = new Dictionary<int[], QuantizeDel>();
		public int[] QuantizationTable
		{
			set
			{
				quantizationTable = value;
				lock (quantizeDelegates)
				{
					if (!quantizeDelegates.TryGetValue(quantizationTable, out quant))
					{
						quant = EmitQuantize();
						quantizeDelegates.Add(quantizationTable, quant);
					}
				}
			}
		}
		private int[] quantizationTable;
		private readonly JpegScan parent;

		// Current MCU block
		private float[][][] scanMCUs;

		private readonly byte colorMode;

		public int spectralStart, spectralEnd;
		public int successiveLow;

		private HuffmanTable acTable;
		public HuffmanTable ACTable
		{
			get
			{
				/* Set default tables in case they're not provided.  */
				if (acTable == null && colorMode == JpegFrame.JpegColorYCbCr)
				{
					acTable = HuffmanTable.GetHuffmanTable(componentId == 1 ? JpegHuffmanTable.StdAcLuminance : JpegHuffmanTable.StdAcChrominance);
				}
				return acTable;
			}
			set
			{
				acTable = value;
			}
		}

		private HuffmanTable dcTable;
		public HuffmanTable DCTable
		{
			get
			{
				/* Set default tables in case they're not provided.  */
				if (dcTable == null && colorMode == JpegFrame.JpegColorYCbCr)
				{
					dcTable = HuffmanTable.GetHuffmanTable(componentId == 1 ? JpegHuffmanTable.StdDCLuminance : JpegHuffmanTable.StdAcLuminance);
				}
				return dcTable;
			}
			set
			{
				dcTable = value;
			}
		}

		private delegate void QuantizeDel(float[] arr);
		private QuantizeDel quant;

		readonly DCT dct = new DCT();

		public int BlockCount { get { return scanData.Count; } }
		private int factorUpV { get { return parent.MaxV / factorV; } }
		private int factorUpH { get { return parent.MaxH / factorH; } }

		#endregion

		#region Constructors

		// ks 1/1/10 - Keeping this for backwards compatibility with the JpegDecoder class.
		public JpegComponent(JpegScan parentScan, byte id, byte factorHorizontal, byte factorVertical, byte quantizationId, byte colorMode)
		{
			parent = parentScan;
			this.colorMode = colorMode;
			componentId = id;
			factorH = factorHorizontal;
			factorV = factorVertical;
			quantId = quantizationId;
		}

		// ks 1/1/10 - Added this to improve readability of the JpegFrameDecoder class.
		public JpegComponent(JpegScan parentScan, byte id, byte factorHorizontal, byte factorVertical, JpegQuantizationTable jpegQuantizationTable, byte colorMode)
		{
			parent = parentScan;
			this.colorMode = colorMode;
			componentId = id;
			factorH = factorHorizontal;
			factorV = factorVertical;
			QuantizationTable = jpegQuantizationTable.Table;
		}

		public void Reset()
		{
			scanData = new List<float[][][]>();

			if (scanDecoded != null)
			{
				foreach (var arr in scanDecoded)
				{
					scanDecodedBufferPool.Recycle(arr);
				}
			}
			scanDecoded = new List<byte[][]>();
			previousDC = 0;
		}
		#endregion

		#region Public Methods

		/// <summary>
		/// If a restart marker is found with too little of an MCU count (i.e. our
		/// Restart Interval is 63 and we have 61 we copy the last MCU until it's full)
		/// </summary>
		public void padMCU(int index, int length)
		{
			scanMCUs = new float[factorH][][]; // , factorV][];

			for (int n = 0; n < length; n++)
			{
				if (scanData.Count >= (index + length)) continue;

				for (int i = 0; i < factorH; i++)
				{
					scanMCUs[i] = new float[factorV][];
					for (int j = 0; j < factorV; j++)
					{
						var source = scanData[index - 1][i][j];
						var dest = new float[source.Length];
						Buffer.BlockCopy(source, 0, dest, 0, source.Length * sizeof(float));
						scanMCUs[i][j] = dest;
					}
				}
				scanData.Add(scanMCUs);
			}

		}

		/// <summary>
		/// Reset the interval by setting the previous DC value
		/// </summary>
		public void resetInterval()
		{
			previousDC = 0;
		}

		/// <summary>
		/// Run the Quantization backward method on all of the block data.
		/// </summary>
		public void QuantizeData()
		{
			for (int i = 0; i < scanData.Count; i++)
			{
				for (int h = 0; h < factorH; h++)
				{
					for (int v = 0; v < factorV; v++)
					{
						// Dynamic IL method
						quant(scanData[i][h][v]);

						// Old technique
						//float[] toQuantize = scanData[i][h, v];
						//for (int j = 0; j < 64; j++) toQuantize[j] *= quantizationTable[j];
					}
				}
			}

		}

		public void setDCTable(JpegHuffmanTable table)
		{
			dcTable = HuffmanTable.GetHuffmanTable(table);
		}

		public void setACTable(JpegHuffmanTable table)
		{
			acTable = HuffmanTable.GetHuffmanTable(table);
		}

		/// <summary>
		/// Run the Inverse DCT method on all of the block data
		/// </summary>
		public void IdctData()
		{
			foreach (float[][][] t in scanData)
			{
				for (int v = 0; v < factorV; v++)
				{
					for (int h = 0; h < factorH; h++)
					{
						float[] toDecode = t[h][v];
						ZigZag.UnZigZag(toDecode, unZZ);
						byte[][] decoded = scanDecodedBufferPool.GetNext();
						switch (JpegConstants.SelectedIdct)
						{
							case IdctImplementation.Naive:
								dct.DoIDCT_Naive(unZZ, decoded);
								break;
							case IdctImplementation.AAN:
								dct.DoIDCT_AAN(unZZ, quantizationTable, decoded);
								break;
							case IdctImplementation.NVidia:
								dct.DoIDCT_NVidia(unZZ, decoded);
								break;
						}
						scanDecoded.Add(decoded);
					}
				}
			}
		}

		/// <summary>
		/// Stretches components as needed to normalize the size of all components.
		/// For example, in a 2x1 (4:2:2) sequence, the Cr and Cb channels will be 
		/// scaled vertically by a factor of 2.
		/// </summary>
		public void scaleByFactors(BlockUpsamplingMode mode)
		{
			int factorUpVertical = factorUpV,
				factorUpHorizontal = factorUpH;

			if (factorUpVertical == 1 && factorUpHorizontal == 1) return;

			for (int i = 0; i < scanDecoded.Count; i++)
			{
				byte[][] src = scanDecoded[i];

				int oldV = src.Length,
					oldH = src[0].Length,
					newV = oldV * factorUpVertical,
					newH = oldH * factorUpHorizontal;

				var dest = new byte[newV][]; //, newH];

				switch (mode)
				{
					case BlockUpsamplingMode.BoxFilter:
						#region Upsampling by repeating values
						/* Perform scaling (Box filter) */
						for (int v = 0; v < newV; v++)
						{
							dest[v] = new byte[newH];
							int srcV = v / factorUpVertical;
							for (int u = 0; u < newH; u++)
							{
								int srcU = u / factorUpHorizontal;
								dest[v][u] = src[srcV][srcU];
							}
						}
						#endregion
						break;

					case BlockUpsamplingMode.Interpolate:
						#region Upsampling by interpolation

						for (int v = 0; v < newV; v++)
						{
							dest[v] = new byte[newH];
							for (int u = 0; u < newH; u++)
							{
								int val = 0;

								for (int x = 0; x < factorUpHorizontal; x++)
								{
									int srcU = (u + x) / factorUpHorizontal;
									if (srcU >= oldH) srcU = oldH - 1;

									for (int y = 0; y < factorUpVertical; y++)
									{
										int srcV = (v + y) / factorUpVertical;

										if (srcV >= oldV) srcV = oldV - 1;

										val += src[srcV][srcU];
									}
								}

								dest[v][u] = (byte)(val / (factorUpHorizontal * factorUpVertical));
							}
						}

						#endregion
						break;

					default:
						throw new ArgumentException("Upsampling mode not supported.");
				}

				scanDecoded[i] = dest;
			}

		}

		public void writeBlock(byte[][,] raster, byte[,] data, int compIndex, int x, int y)
		{
			int w = raster[0].GetLength(0),
				h = raster[0].GetLength(1);

			byte[,] comp = raster[compIndex];

			// Blocks may spill over the frame so we bound by the frame size
			int yMax = data.GetLength(0); if ((y + yMax) > h) yMax = h - y;
			int xMax = data.GetLength(1); if ((x + xMax) > w) xMax = w - x;

			for (int yIndex = 0; yIndex < yMax; yIndex++)
			{
				for (int xIndex = 0; xIndex < xMax; xIndex++)
				{
					comp[x + xIndex, y + yIndex] = data[yIndex, xIndex];
				}
			}
		}

		public void WriteDataScaled(byte[][][] raster, int componentIndex, BlockUpsamplingMode mode)
		{
			int x = 0, y = 0, lastblockheight = 0, incrementblock = 0;

			int blockIdx = 0;

			int w = raster[0].Length; //, h = raster[0][0].Length;

			// Keep looping through all of the blocks until there are no more.
			while (blockIdx < scanDecoded.Count)
			{
				int blockwidth = 0;
				int blockheight = 0;

				if (x >= w) { x = 0; y += incrementblock; }

				// Loop through the horizontal component blocks of the MCU first
				// then for each horizontal line write out all of the vertical
				// components
				for (int factorVIndex = 0; factorVIndex < factorV; factorVIndex++)
				{
					blockwidth = 0;

					for (int factorHIndex = 0; factorHIndex < factorH; factorHIndex++)
					{
						// Captures the width of this block so we can increment the X coordinate
						byte[][] blockdata = scanDecoded[blockIdx++];

						// Writes the data at the specific X and Y coordinate of this component
						writeBlockScaled(raster, blockdata, componentIndex, x, y, mode);

						blockwidth += blockdata.Length * factorUpH;
						x += blockdata[0].Length * factorUpH;
						blockheight = blockdata.Length * factorUpV;
					}

					y += blockheight;
					x -= blockwidth;
					lastblockheight += blockheight;
				}
				y -= lastblockheight;
				incrementblock = lastblockheight;
				lastblockheight = 0;
				x += blockwidth;
			}
		}
		public delegate JpegReadStatusInt DecodeFunction(JpegBinaryReader jpegReader, float[] zigzagMCU);
		public DecodeFunction Decode;

		public JpegReadStatusInt DecodeBaseline(JpegBinaryReader stream, float[] dest)
		{
			float dc;
			var status = decode_dc_coefficient(stream, out dc);
			if (status.Status != Status.Success)
			{
				return status;
			}
			status = decode_ac_coefficients(stream, dest);
			if (status.Status == Status.Success)
			{
				dest[0] = dc;
			}
			return status;
		}

		public JpegReadStatusInt DecodeDCFirst(JpegBinaryReader stream, float[] dest)
		{
			var status = DCTable.Decode(stream);
			if (status.Status != Status.Success)
			{
				return status; // We reached the end of the stream.
			}
			int s = status.Result;
			status = stream.ReadBits(s);
			if (status.Status != Status.Success)
			{
				return status;
			}
			int r = status.Result;
			s = HuffmanTable.Extend(r, s);
			s = (int)previousDC + s;
			previousDC = s;

			dest[0] = s << successiveLow;
			return status;
		}

		public JpegReadStatusInt DecodeACFirst(JpegBinaryReader stream, float[] zz)
		{
			if (stream.eobRun > 0)
			{
				stream.eobRun--;
				return JpegReadStatusInt.GetSuccess();
			}

			for (int k = spectralStart; k <= spectralEnd; k++)
			{
				var status = ACTable.Decode(stream);
				if (status.Status != Status.Success)
				{
					return status;
				}
				int s = status.Result;
				int r = s >> 4;
				s &= 15;

				if (s != 0)
				{
					k += r;
					status = stream.ReadBits(s);
					if (status.Status != Status.Success)
					{
						return status;
					}
					r = status.Result;
					s = HuffmanTable.Extend(r, s);
					zz[k] = s << successiveLow;
				}
				else
				{
					if (r != 15)
					{
						stream.eobRun = 1 << r;

						if (r != 0)
						{
							status = stream.ReadBits(r);
							if (status.Status != Status.Success)
							{
								return status;
							}
							stream.eobRun += status.Result;
						}

						stream.eobRun--;
						break;
					}

					k += 15;
				}
			}
			return JpegReadStatusInt.GetSuccess();
		}

		public JpegReadStatusInt DecodeDCRefine(JpegBinaryReader stream, float[] dest)
		{
			var status = stream.ReadBits(1);
			if (status.Status != Status.Success)
			{
				return status;
			}
			if (status.Result == 1)
			{
				dest[0] = (int)dest[0] | (1 << successiveLow);
			}
			return JpegReadStatusInt.GetSuccess();
		}

		public JpegReadStatusInt DecodeACRefine(JpegBinaryReader stream, float[] dest)
		{
			int p1 = 1 << successiveLow;
			int m1 = (-1) << successiveLow;

			int k = spectralStart;

			if (stream.eobRun == 0)
				for (; k <= spectralEnd; k++)
				{
					#region Decode and check S

					var status = ACTable.Decode(stream);
					if (status.Status != Status.Success)
					{
						return status;
					}
					int s = status.Result;
					int r = s >> 4;
					s &= 15;

					if (s != 0)
					{
						if (s != 1)
						{
							throw new Exception("Decode Error");
						}

						status = stream.ReadBits(1);
						if (status.Status != Status.Success)
						{
							return status;
						}
						s = status.Result == 1 ? p1 : m1;
					}
					else
					{
						if (r != 15)
						{
							stream.eobRun = 1 << r;

							if (r > 0)
							{
								status = stream.ReadBits(r);
								if (status.Status != Status.Success)
								{
									return status;
								}
								stream.eobRun += status.Result;
							}
							break;
						}

					} // if (s != 0)

					#endregion

					// Apply the update
					do
					{
						if (dest[k] != 0)
						{
							status = stream.ReadBits(1);
							if (status.Status != Status.Success)
							{
								return status;
							}
							if (status.Result == 1)
							{
								if (((int)dest[k] & p1) == 0)
								{
									if (dest[k] >= 0)
										dest[k] += p1;
									else
										dest[k] += m1;
								}
							}
						}
						else
						{
							if (--r < 0)
								break;
						}

						k++;

					} while (k <= spectralEnd);

					if ((s != 0) && k < 64)
					{
						dest[k] = s;
					}
				} // for k = start ... end


			if (stream.eobRun > 0)
			{
				for (; k <= spectralEnd; k++)
				{
					if (dest[k] != 0)
					{
						var status = stream.ReadBits(1);
						if (status.Status != Status.Success)
						{
							return status;
						}
						if (status.Result == 1)
						{
							if (((int)dest[k] & p1) == 0)
							{
								if (dest[k] >= 0)
									dest[k] += p1;
								else
									dest[k] += m1;
							}
						}
					}
				}

				stream.eobRun--;
			}
			return JpegReadStatusInt.GetSuccess();
		}

		public void SetBlock(int idx)
		{
			if (scanData.Count < idx)
				throw new Exception("Invalid block ID.");

			// expand the data list
			if (scanData.Count == idx)
			{
				scanMCUs = new float[factorH][][]; // factorV][];
				for (int i = 0; i < factorH; i++)
				{
					scanMCUs[i] = new float[factorV][];
					for (int j = 0; j < factorV; j++)
					{
						scanMCUs[i][j] = new float[64];
					}
				}

				scanData.Add(scanMCUs);
			}
			else // reference an existing block
			{
				scanMCUs = scanData[idx];
			}
		}

		public JpegReadStatusInt DecodeMCU(JpegBinaryReader jpegReader, int i, int j)
		{
			return Decode(jpegReader, scanMCUs[i][j]);
		}

		/// <summary>
		/// Generated from text on F-22, F.2.2.1 - Huffman decoding of DC
		/// coefficients on ISO DIS 10918-1. Requirements and Guidelines.
		/// </summary>
		/// <param name="jpegStream">Stream that contains huffman bits</param>
		/// <param name="diff">The DC coefficient</param>
		/// <returns>The result of the read</returns>
		public JpegReadStatusInt decode_dc_coefficient(JpegBinaryReader jpegStream, out float diff)
		{
			diff = 0;
			var status = DCTable.Decode(jpegStream);
			if (status.Status != Status.Success)
			{
				return status;
			}
			int t = status.Result;
			status = jpegStream.ReadBits(t);
			if (status.Status != Status.Success)
			{
				return status;
			}
			diff = HuffmanTable.Extend(status.Result, t);
			diff = (previousDC + diff);
			previousDC = diff;
			return status;
		}

		/// <summary>
		/// Generated from text on F-23, F.13 - Huffman decoded of AC coefficients
		/// on ISO DIS 10918-1. Requirements and Guidelines.
		/// </summary>
		internal JpegReadStatusInt decode_ac_coefficients(JpegBinaryReader jpegStream, float[] zz)
		{
			var status = new JpegReadStatusInt();
			for (int k = 1; k < 64; k++)
			{
				status = ACTable.Decode(jpegStream);
				if (status.Status != Status.Success)
				{
					return status;
				}
				int s = status.Result;
				int r = s >> 4;
				s &= 15;

				if (s != 0)
				{
					k += r;
					status = jpegStream.ReadBits(s);
					if (status.Status != Status.Success)
					{
						return status;
					}
					s = HuffmanTable.Extend(status.Result, s);
					zz[k] = s;
				}
				else
				{
					if (r != 15)
					{
						//throw new JPEGMarkerFoundException();
						return status;
					}
					k += 15;
				}
			}
			return status;
		}

		#endregion

		#region Private Methods
		private QuantizeDel EmitQuantize()
		{
			ClientLogger.Debug("Emitting quantize data.");
			Type[] args = { typeof(float[]) };

			var quantizeMethod = new DynamicMethod("Quantize",
				null, // no return type
				args); // input array

			ILGenerator il = quantizeMethod.GetILGenerator();

			for (int i = 0; i < quantizationTable.Length; i++)
			{
				float mult = quantizationTable[i];

				// Sz Stack:
				il.Emit(OpCodes.Ldarg_0);              // 1  {arr} 
				il.Emit(OpCodes.Ldc_I4_S, (short)i);   // 3  {arr,i}
				il.Emit(OpCodes.Ldarg_0);              // 1  {arr,i,arr}
				il.Emit(OpCodes.Ldc_I4_S, (short)i);   // 3  {arr,i,arr,i}
				il.Emit(OpCodes.Ldelem_R4);            // 1  {arr,i,arr[i]}
				il.Emit(OpCodes.Ldc_R4, mult);         // 5  {arr,i,arr[i],mult}
				il.Emit(OpCodes.Mul);                  // 1  {arr,i,arr[i]*mult}
				il.Emit(OpCodes.Stelem_R4);            // 1  {}
			}

			il.Emit(OpCodes.Ret);

			return (QuantizeDel)quantizeMethod.CreateDelegate(typeof(QuantizeDel));
		}
		private void writeBlockScaled(byte[][][] raster, byte[][] blockdata, int compIndex, int x, int y, BlockUpsamplingMode mode)
		{
			int w = raster[0].Length,
				h = raster[0][0].Length;

			int factorUpVertical = factorUpV,
				factorUpHorizontal = factorUpH;

			int oldV = blockdata.Length,
				oldH = blockdata[0].Length,
				newV = oldV * factorUpVertical,
				newH = oldH * factorUpHorizontal;

			byte[][] comp = raster[compIndex];

			// Blocks may spill over the frame so we bound by the frame size
			int yMax = newV; if ((y + yMax) > h) yMax = h - y;
			int xMax = newH; if ((x + xMax) > w) xMax = w - x;

			switch (mode)
			{
				case BlockUpsamplingMode.BoxFilter:

					#region Upsampling by repeating values

					// Special case 1: No scale-up
					if (factorUpVertical == 1 && factorUpHorizontal == 1)
					{
						for (int u = 0; u < xMax; u++)
							for (int v = 0; v < yMax; v++)
								comp[u + x][y + v] = blockdata[v][u];
					}
					// Special case 2: Perform scale-up 4 pixels at a time
					else if (factorUpHorizontal == 2 &&
							 factorUpVertical == 2 &&
							 xMax == newH && yMax == newV)
					{
						for (int srcU = 0; srcU < oldH; srcU++)
						{
							int bx = srcU * 2 + x;

							for (int srcV = 0; srcV < oldV; srcV++)
							{
								byte val = blockdata[srcV][srcU];
								int by = srcV * 2 + y;

								comp[bx][by] = val;
								comp[bx][by + 1] = val;
								comp[bx + 1][by] = val;
								comp[bx + 1][by + 1] = val;
							}
						}
					}
					else
					{
						/* Perform scaling (Box filter) */
						for (int u = 0; u < xMax; u++)
						{
							int srcU = u / factorUpHorizontal;
							for (int v = 0; v < yMax; v++)
							{
								int srcV = v / factorUpVertical;
								comp[u + x][y + v] = blockdata[srcV][srcU];
							}
						}
					}


					#endregion
					break;

				// JRP 4/7/08 -- This mode is disabled temporarily as it needs to be fixed after
				//               recent performance tweaks.
				//               It can produce slightly better (less blocky) decodings.

				//case BlockUpsamplingMode.Interpolate:
				//    #region Upsampling by interpolation
				//    for (int u = 0; u < newH; u++)
				//    {
				//        for (int v = 0; v < newV; v++)
				//        {
				//            int val = 0;
				//            for (int x = 0; x < factorUpHorizontal; x++)
				//            {
				//                int src_u = (u + x) / factorUpHorizontal;
				//                if (src_u >= oldH) src_u = oldH - 1;
				//                for (int y = 0; y < factorUpVertical; y++)
				//                {
				//                    int src_v = (v + y) / factorUpVertical;
				//                    if (src_v >= oldV) src_v = oldV - 1;
				//                    val += src[src_v, src_u];
				//                }
				//            }
				//            dest[v, u] = (byte)(val / (factorUpHorizontal * factorUpVertical));
				//        }
				//    }
				//    #endregion
				//    break;

				default:
					throw new ArgumentException("Upsampling mode not supported.");
			}

		}
		#endregion Private Methods
	}

}
