// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt..

using System;
using Alanta.Client.Media.Jpeg.Decoder;

namespace Alanta.Client.Media.Jpeg
{
	/// <summary>
	/// Contains several different implementations of the forward and inverse discrete cosine transforms.
	/// </summary>
	/// <remarks>
	/// ks 11/18/10 - Not all of the implementations are currently working.
	/// </remarks>
	public class DCT
	{

		#region Fields and Properties
		public const int N = 8;

		public int[][] quantum = new int[2][];
		public double[][] divisors = new double[2][];

		/// <summary>
		/// Quantitization Matrix for luminace.
		/// </summary>
		public double[] DivisorsLuminance = new double[N * N];

		/// <summary>
		/// Quantitization Matrix for chrominance.
		/// </summary>
		public double[] DivisorsChrominance = new double[N * N];

		/// <summary>
		/// Scratch workspace.
		/// </summary>
		private readonly float[] tmpWorkingTable = new float[64];

		/// <summary>
		/// Cosine matrix. Used in naive IDCT.
		/// </summary>
		private static readonly float[][] cosineTable;

		/// <summary>
		/// Transposed cosine matrix. Used in naive IDCT.
		/// </summary>
		private static readonly float[][] transposedCosineTable;

		/// <summary>
		/// Helps to deal with excessively large or small values during IDCT normalization step. Used in AAN IDCT.
		/// </summary>
		private const int rangeMask = (JpegConstants.MAXJSAMPLE * 4 + 3); /* 2 bits wider than legal samples */

		/// <summary>
		/// a = (2^0.5) * cos(    pi / 16);  Used in the NVIDIA forward and inverse DCT.  
		/// </summary>
		private const float cA = 1.387039845322148f;

		/// <summary>
		/// b = (2^0.5) * cos(    pi /  8);  Used in the NVIDIA forward and inverse DCT.  
		/// </summary>
		private const float cB = 1.306562964876377f;

		/// <summary>
		/// c = (2^0.5) * cos(3 * pi / 16);  Used in the NVIDIA forward and inverse DCT.  
		/// </summary>
		private const float cC = 1.175875602419359f;

		/// <summary>
		/// d = (2^0.5) * cos(5 * pi / 16);  Used in the NVIDIA forward and inverse DCT.  
		/// </summary>
		private const float cD = 0.785694958387102f;

		/// <summary>
		/// e = (2^0.5) * cos(3 * pi /  8);  Used in the NVIDIA forward and inverse DCT.  
		/// </summary>
		private const float cE = 0.541196100146197f;

		/// <summary>
		/// f = (2^0.5) * cos(7 * pi / 16);  Used in the NVIDIA forward and inverse DCT.  
		/// </summary>
		private const float cF = 0.275899379282943f;

		/// <summary>
		/// Normalization constant used in the NVIDIA forward and inverse DCT
		/// </summary>
		private const float cNorm = 0.3535533905932737f; // 1 / (8^0.5)

		#endregion

		#region Constructors

		internal DCT()
		{
		}

		public DCT(int quality)
			: this()
		{
			Initialize(quality, JpegQuantizationTable.K1Luminance, JpegQuantizationTable.K2Chrominance);
		}

		public DCT(int quality, JpegQuantizationTable luminance, JpegQuantizationTable chrominance)
		{
			Initialize(quality, luminance, chrominance);
		}

		private void Initialize(int quality, JpegQuantizationTable luminance, JpegQuantizationTable chrominance)
		{
			double[] aanScaleFactor = 
			{
				1.0, 1.387039845, 1.306562965, 1.175875602,
				1.0, 0.785694958, 0.541196100, 0.275899379
			};

			int i, j, scaledQuality;

			// jpeg_quality_scaling
			if (quality <= 0) quality = 1;
			if (quality > 100) quality = 100;
			if (quality < 50) scaledQuality = 5000 / quality;
			else scaledQuality = 200 - quality * 2;

			int[] scaledLum = luminance.getScaledInstance(scaledQuality / 100f, true).Table;

			int index = 0;
			for (i = 0; i < 8; i++)
			{
				for (j = 0; j < 8; j++)
				{
					DivisorsLuminance[index] = 1.0 / (scaledLum[index] * aanScaleFactor[i] * aanScaleFactor[j] * 8.0);
					index++;
				}
			}

			// Creating the chrominance matrix
			int[] scaledChrom = chrominance.getScaledInstance(scaledQuality / 100f, true).Table;

			index = 0;
			for (i = 0; i < 8; i++)
			{
				for (j = 0; j < 8; j++)
				{
					DivisorsChrominance[index] = 1.0 / (scaledChrom[index] * aanScaleFactor[i] * aanScaleFactor[j] * 8.0);
					index++;
				}
			}

			quantum[0] = scaledLum;
			divisors[0] = DivisorsLuminance;
			quantum[1] = scaledChrom;
			divisors[1] = DivisorsChrominance;
		}

		static DCT()
		{
			cosineTable = BuildCosineTable();
			transposedCosineTable = BuildTransposedCosineTable();
		}

		/// <summary>
		/// Precomputes cosine terms in A.3.3 of 
		/// http://www.w3.org/Graphics/JPEG/itu-t81.pdf
		/// 
		/// Closely follows the term precomputation in the
		/// Java Advanced Imaging library.
		/// </summary>
		private static float[][] BuildCosineTable()
		{
			var c = new float[8][];

			for (int i = 0; i < 8; i++) // i == u or v
			{
				c[i] = new float[8];
				for (int j = 0; j < 8; j++) // j == x or y
				{
					c[i][j] = i == 0 ?
						0.353553391f : /* 1 / SQRT(8) */
						(float)(0.5 * Math.Cos(((2.0 * j + 1) * i * Math.PI) / 16.0));
				}
			}

			return c;
		}
		private static float[][] BuildTransposedCosineTable()
		{
			// Transpose i,k <-- j,i
			var cT = new float[8][]; //, 8];
			for (int j = 0; j < 8; j++)
			{
				cT[j] = new float[8];
				for (int i = 0; i < 8; i++)
				{
					cT[j][i] = cosineTable[i][j];
				}
			}
			return cT;
		}

		#endregion

		#region DCT Implementations

		/// <summary>
		/// Performs a forward DCT on one block of samples using the Arai, Agui, and Nakajima algorithm
		/// </summary>        
		/// <remarks>
		/// NOTE: this code only copes with 8x8 DCTs.
		/// 
		/// A floating-point implementation of the forward DCT (Discrete Cosine Transform).
		/// 
		/// This implementation should be more accurate than either of the integer
		/// DCT implementations.  However, it may not give the same results on all
		/// machines because of differences in roundoff behavior.  Speed will depend
		/// on the hardware's floating point capacity.
		/// 
		/// A 2-D DCT can be done by 1-D DCT on each row followed by 1-D DCT
		/// on each column.  Direct algorithms are also available, but they are
		/// much more complex and seem not to be any faster when reduced to code.
		/// 
		/// This implementation is based on Arai, Agui, and Nakajima's algorithm for
		/// scaled DCT.  Their original paper (Trans. IEICE E-71(11):1095) is in
		/// Japanese, but the algorithm is described in the Pennebaker &amp; Mitchell
		/// JPEG textbook (see REFERENCES section in file README).  The following code
		/// is based directly on figure 4-8 in P&amp;M.
		/// While an 8-point DCT cannot be done in less than 11 multiplies, it is
		/// possible to arrange the computation so that many of the multiplies are
		/// simple scalings of the final outputs.  These multiplies can then be
		/// folded into the multiplications or divisions by the JPEG quantization
		/// table entries.  The AA&amp;N method leaves only 5 multiplies and 29 adds
		/// to be done in the DCT itself.
		/// The primary disadvantage of this method is that with a fixed-point
		/// implementation, accuracy is lost due to imprecise representation of the
		/// scaled quantization values.  However, that problem does not arise if
		/// we use floating point arithmetic.
		/// </remarks>
		public void DoDCT_AAN(float[][] input, float[][] output)
		{
			float tmp0, tmp1, tmp2, tmp3, tmp4, tmp5, tmp6, tmp7;
			float tmp10, tmp11, tmp12, tmp13;
			float z1, z2, z3, z4, z5, z11, z13;
			int i;

			for (i = 0; i < 8; i++)
			{
				int j;
				for (j = 0; j < 8; j++)
					output[i][j] = input[i][j] - 128f;
			}

			// Pass 1: process rows.

			for (i = 0; i < 8; i++)
			{
				tmp0 = output[i][0] + output[i][7];
				tmp7 = output[i][0] - output[i][7];
				tmp1 = output[i][1] + output[i][6];
				tmp6 = output[i][1] - output[i][6];
				tmp2 = output[i][2] + output[i][5];
				tmp5 = output[i][2] - output[i][5];
				tmp3 = output[i][3] + output[i][4];
				tmp4 = output[i][3] - output[i][4];

				// Even part
				tmp10 = tmp0 + tmp3;
				tmp13 = tmp0 - tmp3;
				tmp11 = tmp1 + tmp2;
				tmp12 = tmp1 - tmp2;

				output[i][0] = tmp10 + tmp11;
				output[i][4] = tmp10 - tmp11;

				z1 = (tmp12 + tmp13) * 0.707106781f;
				output[i][2] = tmp13 + z1;
				output[i][6] = tmp13 - z1;

				// Odd part
				tmp10 = tmp4 + tmp5;
				tmp11 = tmp5 + tmp6;
				tmp12 = tmp6 + tmp7;

				// The rotator is modified from fig 4-8 to avoid extra negations.
				z5 = (tmp10 - tmp12) * 0.382683433f;
				z2 = (0.541196100f) * tmp10 + z5;
				z4 = (1.306562965f) * tmp12 + z5;
				z3 = tmp11 * (0.707106781f);

				z11 = tmp7 + z3;
				z13 = tmp7 - z3;

				output[i][5] = z13 + z2;
				output[i][3] = z13 - z2;
				output[i][1] = z11 + z4;
				output[i][7] = z11 - z4;
			}

			// Pass 2: process columns

			for (i = 0; i < 8; i++)
			{
				tmp0 = output[0][i] + output[7][i];
				tmp7 = output[0][i] - output[7][i];
				tmp1 = output[1][i] + output[6][i];
				tmp6 = output[1][i] - output[6][i];
				tmp2 = output[2][i] + output[5][i];
				tmp5 = output[2][i] - output[5][i];
				tmp3 = output[3][i] + output[4][i];
				tmp4 = output[3][i] - output[4][i];

				// Even part
				tmp10 = tmp0 + tmp3;
				tmp13 = tmp0 - tmp3;
				tmp11 = tmp1 + tmp2;
				tmp12 = tmp1 - tmp2;

				output[0][i] = tmp10 + tmp11;
				output[4][i] = tmp10 - tmp11;

				z1 = (tmp12 + tmp13) * 0.707106781f;
				output[2][i] = tmp13 + z1;
				output[6][i] = tmp13 - z1;

				// Odd part
				tmp10 = tmp4 + tmp5;
				tmp11 = tmp5 + tmp6;
				tmp12 = tmp6 + tmp7;

				// The rotator is modified from fig 4-8 to avoid extra negations.
				z5 = (tmp10 - tmp12) * 0.382683433f;
				z2 = (0.541196100f) * tmp10 + z5;
				z4 = (1.306562965f) * tmp12 + z5;
				z3 = tmp11 * (0.707106781f);

				z11 = tmp7 + z3;
				z13 = tmp7 - z3;

				output[5][i] = z13 + z2;
				output[3][i] = z13 - z2;
				output[1][i] = z11 + z4;
				output[7][i] = z11 - z4;
			}
		}

		/// <summary>
		/// Performs a forward DCT on one block of samples using the Sung, Shieh, Yu and Hsin algorithm
		/// </summary>
		/// <param name="input">source samples</param>
		/// <param name="output">destination samples</param>
		/// <remarks>
		/// See http://www.naic.edu/~phil/hardware/nvidia/doc/src/dct8x8/doc/dct8x8.pdf for references.
		/// ks 11/18/10 - I can't seem to get this working correctly, and it turns out that it's only very slightly faster than the AAN algorithm. Abandoning for now.
		/// </remarks>
		public void DoDCT_NVidia(float[][] input, float[][] output)
		{
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
					output[i][j] = input[i][j] - 128f;
			}

			//process rows
			for (int row = 0; row < 8; row++)
			{
				DCTRow(output, row, output);
			}

			//process columns
			for (int col = 0; col < 8; col++)
			{
				DCTColumn(output, col, output);
			}
		}

		/// <summary>
		/// Performs DCT of vector of 8 elements.
		/// </summary>
		/// <param name="input">Input vector</param>
		/// <param name="stepIn">Value to add to pointer to access other input elements</param>
		/// <param name="output">Output vector</param>
		/// <param name="stepOut">Value to add to pointer to access other output elements</param>
		private void DCTRow(float[][] input, int row, float[][] output)
		{
			float X07P = input[row][0] + input[row][7];
			float X16P = input[row][1] + input[row][6];
			float X25P = input[row][2] + input[row][5];
			float X34P = input[row][3] + input[row][4];

			float X07M = input[row][0] - input[row][7];
			float X61M = input[row][6] - input[row][1];
			float X25M = input[row][2] - input[row][5];
			float X43M = input[row][4] - input[row][3];

			float X07P34PP = X07P + X34P;
			float X07P34PM = X07P - X34P;
			float X16P25PP = X16P + X25P;
			float X16P25PM = X16P - X25P;

			output[row][0] = cNorm * (X07P34PP + X16P25PP);
			output[row][2] = cNorm * (cB * X07P34PM + cE * X16P25PM);
			output[row][4] = cNorm * (X07P34PP - X16P25PP);
			output[row][6] = cNorm * (cE * X07P34PM - cB * X16P25PM);

			output[row][1] = cNorm * (cA * X07M - cC * X61M + cD * X25M - cF * X43M);
			output[row][3] = cNorm * (cC * X07M + cF * X61M - cA * X25M + cD * X43M);
			output[row][5] = cNorm * (cD * X07M + cA * X61M + cF * X25M - cC * X43M);
			output[row][7] = cNorm * (cF * X07M + cD * X61M + cC * X25M + cA * X43M);
		}

		/// <summary>
		/// Performs DCT of vector of 8 elements.
		/// </summary>
		/// <param name="input">Input vector</param>
		/// <param name="stepIn">Value to add to pointer to access other input elements</param>
		/// <param name="output">Output vector</param>
		/// <param name="stepOut">Value to add to pointer to access other output elements</param>
		private void DCTColumn(float[][] input, int column, float[][] output)
		{
			float X07P = input[0][column] + input[7][column];
			float X16P = input[1][column] + input[6][column];
			float X25P = input[2][column] + input[5][column];
			float X34P = input[3][column] + input[4][column];

			float X07M = input[0][column] - input[7][column];
			float X61M = input[6][column] - input[1][column];
			float X25M = input[2][column] - input[5][column];
			float X43M = input[4][column] - input[3][column];

			float X07P34PP = X07P + X34P;
			float X07P34PM = X07P - X34P;
			float X16P25PP = X16P + X25P;
			float X16P25PM = X16P - X25P;

			output[0][column] = cNorm * (X07P34PP + X16P25PP);
			output[2][column] = cNorm * (cB * X07P34PM + cE * X16P25PM);
			output[4][column] = cNorm * (X07P34PP - X16P25PP);
			output[6][column] = cNorm * (cE * X07P34PM - cB * X16P25PM);

			output[1][column] = cNorm * (cA * X07M - cC * X61M + cD * X25M - cF * X43M);
			output[5][column] = cNorm * (cD * X07M + cA * X61M + cF * X25M - cC * X43M);
			output[7][column] = cNorm * (cF * X07M + cD * X61M + cC * X25M + cA * X43M);
		}

		#endregion

		#region IDCT Implementations

		/// <summary>
		/// See figure A.3.3 IDCT (informative) on A-5.
		/// http://www.w3.org/Graphics/JPEG/itu-t81.pdf
		/// </summary>
		/// <param name="input">Unzigzagged and dequantized component block</param>
		/// <returns>A jagged 8x8 byte array representing the YCbCr encoded bitmap</returns>
		/// <remarks>
		/// ks 9/27/10 - There are faster implementations of this available.  For instance,
		/// the current implementation below seems to require 1024 additions + 1024 multiplications per 8x8 block.  However, 
		/// http://vsr.informatik.tu-chemnitz.de/~jan/MPEG/HTML/IDCT.html describes the Feig algorithm which includes 462 additions + 54 multiplications + 6 left shifts per block.  
		/// And http://www.hindawi.com/journals/mpe/2010/185398.html describes a very impressive-sounding algorithm which uses only 25 multiplications + 100 additions.
		/// The math looks fairly complicated, however, so I'm not entirely sure how best to implement them here.
		/// The next option to try is the floating point implementation from LIBJPEG, which uses the Arai, Agui, and Nakajima algorithm (which is a precursor of the Feig algorithm).
		/// Or the implementation NVidia provides in some of their sample code: 
		/// http://www.cse.nd.edu/courses/cse60881/www/source_code/dct8x8/DCT8x8_Gold.cpp
		/// Or here: http://www.koders.com/java/fid906B80CC7D811D6C01B182E17E872F1D019526BC.aspx?s=file:semap*.java
		/// </remarks>
		public void DoIDCT_Naive(float[] input, byte[][] output)
		{
			float temp, val = 0;
			int idx = 0;
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					val = 0;

					for (int k = 0; k < 8; k++)
					{
						val += input[i * 8 + k] * cosineTable[k][j];
					}

					tmpWorkingTable[idx++] = val;
				}
			}
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					temp = 128f;

					for (int k = 0; k < 8; k++)
					{
						temp += transposedCosineTable[i][k] * tmpWorkingTable[k * 8 + j];
					}

					output[i][j] = ByteRangeLimiter.Table[ByteRangeLimiter.TableOffset + (int)(temp)];
					// Old way:
					//if (temp < 0) output[i][j] = 0;
					//else if (temp > 255) output[i][j] = 255;
					//else output[i][j] = (byte)(temp + 0.5); // Implements rounding
				}
			}
		}

		/// <summary>
		/// Quantization and IDCT implementation borrowed from LibJpeg.NET.
		/// </summary>
		/// <param name="input">Quantized and unzigzagged float values</param>
		/// <param name="quantTable">Quantization table</param>
		/// <returns>A jagged 8x8 byte array representing the YCbCr encoded bitmap</returns>
		/// <remarks>
		/// Uses a floating-point implementation of the AAN algorithm.
		/// ks 11/18/10 - This has basically the same speed as the NVidia implementation, but it doesn't seem to produce the right values, i.e.,
		/// the resulting image seems to be corrupted.  So we're going with the NVidia implementation.
		/// </remarks>
		public void DoIDCT_AAN(float[] input, int[] quantTable, byte[][] output)
		{
			/* buffers data between passes */
			float[] workspace = tmpWorkingTable;

			/* Pass 1: process columns from input, store into work array. */
			int coefBlockIndex = 0;
			int workspaceIndex = 0;
			int quantTableIndex = 0;

			for (int ctr = 8; ctr > 0; ctr--)
			{

				/* Due to quantization, we will usually find that many of the input
				* coefficients are zero, especially the AC terms.  We can exploit this
				* by short-circuiting the IDCT calculation for any column in which all
				* the AC terms are zero.  In that case each output is equal to the
				* DC coefficient (with scale factor as needed).
				* With typical images and quantization tables, half or more of the
				* column DCT calculations can be simplified this way.
				*/

				if (input[coefBlockIndex + 8 * 1] == 0 &&
					input[coefBlockIndex + 8 * 2] == 0 &&
					input[coefBlockIndex + 8 * 3] == 0 &&
					input[coefBlockIndex + 8 * 4] == 0 &&
					input[coefBlockIndex + 8 * 5] == 0 &&
					input[coefBlockIndex + 8 * 6] == 0 &&
					input[coefBlockIndex + 8 * 7] == 0)
				{
					/* AC terms all zero */
					float dcval = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 0], quantTable[quantTableIndex + 8 * 0]);

					workspace[workspaceIndex + 8 * 0] = dcval;
					workspace[workspaceIndex + 8 * 1] = dcval;
					workspace[workspaceIndex + 8 * 2] = dcval;
					workspace[workspaceIndex + 8 * 3] = dcval;
					workspace[workspaceIndex + 8 * 4] = dcval;
					workspace[workspaceIndex + 8 * 5] = dcval;
					workspace[workspaceIndex + 8 * 6] = dcval;
					workspace[workspaceIndex + 8 * 7] = dcval;

					coefBlockIndex++;            /* advance pointers to next column */
					quantTableIndex++;
					workspaceIndex++;
					continue;
				}

				/* Even part */
				float tmp0 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 0], quantTable[quantTableIndex + 8 * 0]);
				float tmp1 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 2], quantTable[quantTableIndex + 8 * 2]);
				float tmp2 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 4], quantTable[quantTableIndex + 8 * 4]);
				float tmp3 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 6], quantTable[quantTableIndex + 8 * 6]);

				float tmp10 = tmp0 + tmp2;    /* phase 3 */
				float tmp11 = tmp0 - tmp2;

				float tmp13 = tmp1 + tmp3;    /* phases 5-3 */
				float tmp12 = (tmp1 - tmp3) * 1.414213562f - tmp13; /* 2*c4 */

				tmp0 = tmp10 + tmp13;   /* phase 2 */
				tmp3 = tmp10 - tmp13;
				tmp1 = tmp11 + tmp12;
				tmp2 = tmp11 - tmp12;

				/* Odd part */
				float tmp4 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 1], quantTable[quantTableIndex + 8 * 1]);
				float tmp5 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 3], quantTable[quantTableIndex + 8 * 3]);
				float tmp6 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 5], quantTable[quantTableIndex + 8 * 5]);
				float tmp7 = FLOAT_DEQUANTIZE(input[coefBlockIndex + 8 * 7], quantTable[quantTableIndex + 8 * 7]);

				float z13 = tmp6 + tmp5;      /* phase 6 */
				float z10 = tmp6 - tmp5;
				float z11 = tmp4 + tmp7;
				float z12 = tmp4 - tmp7;

				tmp7 = z11 + z13;       /* phase 5 */
				tmp11 = (z11 - z13) * 1.414213562f; /* 2*c4 */

				float z5 = (z10 + z12) * 1.847759065f; /* 2*c2 */
				tmp10 = 1.082392200f * z12 - z5; /* 2*(c2-c6) */
				tmp12 = -2.613125930f * z10 + z5; /* -2*(c2+c6) */

				tmp6 = tmp12 - tmp7;    /* phase 2 */
				tmp5 = tmp11 - tmp6;
				tmp4 = tmp10 + tmp5;

				workspace[workspaceIndex + 8 * 0] = tmp0 + tmp7;
				workspace[workspaceIndex + 8 * 7] = tmp0 - tmp7;
				workspace[workspaceIndex + 8 * 1] = tmp1 + tmp6;
				workspace[workspaceIndex + 8 * 6] = tmp1 - tmp6;
				workspace[workspaceIndex + 8 * 2] = tmp2 + tmp5;
				workspace[workspaceIndex + 8 * 5] = tmp2 - tmp5;
				workspace[workspaceIndex + 8 * 4] = tmp3 + tmp4;
				workspace[workspaceIndex + 8 * 3] = tmp3 - tmp4;

				coefBlockIndex++;            /* advance pointers to next column */
				quantTableIndex++;
				workspaceIndex++;
			}

			/* Pass 2: process rows from work array, store into output array. */
			/* Note that we must descale the results by a factor of 8 == 2**3. */
			workspaceIndex = 0;
			int limitOffset = ByteRangeLimiter.TableOffset + JpegConstants.CENTERJSAMPLE;

			for (int ctr = 0; ctr < 8; ctr++)
			{
				/* Rows of zeroes can be exploited in the same way as we did with columns.
				* However, the column calculation has created many nonzero AC terms, so
				* the simplification applies less often (typically 5% to 10% of the time).
				* And testing floats for zero is relatively expensive, so we don't bother.
				*/

				/* Even part */
				float tmp10 = workspace[workspaceIndex + 0] + workspace[workspaceIndex + 4];
				float tmp11 = workspace[workspaceIndex + 0] - workspace[workspaceIndex + 4];

				float tmp13 = workspace[workspaceIndex + 2] + workspace[workspaceIndex + 6];
				float tmp12 = (workspace[workspaceIndex + 2] - workspace[workspaceIndex + 6]) * 1.414213562f - tmp13;

				float tmp0 = tmp10 + tmp13;
				float tmp3 = tmp10 - tmp13;
				float tmp1 = tmp11 + tmp12;
				float tmp2 = tmp11 - tmp12;

				/* Odd part */
				float z13 = workspace[workspaceIndex + 5] + workspace[workspaceIndex + 3];
				float z10 = workspace[workspaceIndex + 5] - workspace[workspaceIndex + 3];
				float z11 = workspace[workspaceIndex + 1] + workspace[workspaceIndex + 7];
				float z12 = workspace[workspaceIndex + 1] - workspace[workspaceIndex + 7];

				float tmp7 = z11 + z13;
				tmp11 = (z11 - z13) * 1.414213562f;

				float z5 = (z10 + z12) * 1.847759065f; /* 2*c2 */
				tmp10 = 1.082392200f * z12 - z5; /* 2*(c2-c6) */
				tmp12 = -2.613125930f * z10 + z5; /* -2*(c2+c6) */

				float tmp6 = tmp12 - tmp7;
				float tmp5 = tmp11 - tmp6;
				float tmp4 = tmp10 + tmp5;

				/* Final output stage: scale down by a factor of 8 and range-limit */
				output[ctr][0] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp0 + tmp7), 3) & rangeMask];
				output[ctr][7] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp0 - tmp7), 3) & rangeMask];
				output[ctr][1] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp1 + tmp6), 3) & rangeMask];
				output[ctr][6] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp1 - tmp6), 3) & rangeMask];
				output[ctr][2] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp2 + tmp5), 3) & rangeMask];
				output[ctr][5] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp2 - tmp5), 3) & rangeMask];
				output[ctr][4] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp3 + tmp4), 3) & rangeMask];
				output[ctr][3] = ByteRangeLimiter.Table[limitOffset + DESCALE((int)(tmp3 - tmp4), 3) & rangeMask];

				workspaceIndex += 8;       /* advance pointer to next row */
			}
		}

		/// <summary>
		/// Performs 8x8 block-wise Forward Discrete Cosine Transform of the given 
		/// image plane and outputs result to the plane of coefficients.
		/// 2nd version.
		/// </summary>
		/// <param name="fSrc">source image</param>
		/// <param name="fDst">destination coefficients</param>
		/// <param name="Stride">stride</param>
		/// <returns>A jagged 8x8 byte array representing the decoded block.</returns>
		public void DoIDCT_NVidia(float[] input, byte[][] output)
		{
			const int step = 8;

			// Process rows
			for (int k = 0; k < step; k++)
			{
				IDCTRow(input, tmpWorkingTable, k * step);
			}

			// Process columns
			for (int k = 0; k < step; k++)
			{
				IDCTColumn(tmpWorkingTable, tmpWorkingTable, k);
			}

			// Range limit and copy the results into a jagged byte[][] array.
			const int tableOffset = ByteRangeLimiter.TableOffset + 128;
			for (int i = 0; i < step; i++)
			{
				for (int j = 0; j < step; j++)
				{
					output[i][j] = ByteRangeLimiter.Table[tableOffset + (int)(tmpWorkingTable[i * step + j])];
				}
			}
		}

		/// <summary>
		/// Performs IDCT of vector of 8 elements.
		/// </summary>
		/// <param name="input">Input vector</param>
		/// <param name="output">Output vector</param>
		/// <param name="stepIn">Value to add to pointer to access other input elements</param>
		/// <param name="offset">Offset for input vector</param>
		/// <param name="offsetOut">Offset for output vector</param>
		/// <param name="stepOut">Value to add to pointer to access other output elements</param>
		private void IDCTRow(float[] input, float[] output, int offset)
		{
			// Check to see if the AC terms in the current row are all zero. 
			// If they are, in theory we can short-circuit the row calculations, 
			// since they're simply equal to the scaled DC value of the row.
			if (input[offset + 1] == 0 &&
				input[offset + 2] == 0 &&
				input[offset + 3] == 0 &&
				input[offset + 4] == 0 &&
				input[offset + 5] == 0 &&
				input[offset + 6] == 0 &&
				input[offset + 7] == 0)
			{
				float dcval = input[offset + 0] * cNorm;
				output[offset + 0] = dcval;
				output[offset + 1] = dcval;
				output[offset + 2] = dcval;
				output[offset + 3] = dcval;
				output[offset + 4] = dcval;
				output[offset + 5] = dcval;
				output[offset + 6] = dcval;
				output[offset + 7] = dcval;
			}
			else
			{
				float Y04P = input[offset + 0] + input[offset + 4];
				float Y2b6eP = cB * input[offset + 2] + cE * input[offset + 6];

				float Y04P2b6ePP = Y04P + Y2b6eP;
				float Y04P2b6ePM = Y04P - Y2b6eP;
				float Y7f1aP3c5dPP = cF * input[offset + 7] + cA * input[offset + 1] + cC * input[offset + 3] + cD * input[offset + 5];
				float Y7a1fM3d5cMP = cA * input[offset + 7] - cF * input[offset + 1] + cD * input[offset + 3] - cC * input[offset + 5];

				float Y04M = input[offset + 0] - input[offset + 4];
				float Y2e6bM = cE * input[offset + 2] - cB * input[offset + 6];

				float Y04M2e6bMP = Y04M + Y2e6bM;
				float Y04M2e6bMM = Y04M - Y2e6bM;
				float Y1c7dM3f5aPM = cC * input[offset + 1] - cD * input[offset + 7] - cF * input[offset + 3] - cA * input[offset + 5];
				float Y1d7cP3a5fMM = cD * input[offset + 1] + cC * input[offset + 7] - cA * input[offset + 3] + cF * input[offset + 5];

				output[offset + 0] = cNorm * (Y04P2b6ePP + Y7f1aP3c5dPP);
				output[offset + 7] = cNorm * (Y04P2b6ePP - Y7f1aP3c5dPP);
				output[offset + 4] = cNorm * (Y04P2b6ePM + Y7a1fM3d5cMP);
				output[offset + 3] = cNorm * (Y04P2b6ePM - Y7a1fM3d5cMP);

				output[offset + 1] = cNorm * (Y04M2e6bMP + Y1c7dM3f5aPM);
				output[offset + 5] = cNorm * (Y04M2e6bMM - Y1d7cP3a5fMM);
				output[offset + 2] = cNorm * (Y04M2e6bMM + Y1d7cP3a5fMM);
				output[offset + 6] = cNorm * (Y04M2e6bMP - Y1c7dM3f5aPM);
			}
		}

		/// <summary>
		/// Performs IDCT on 1-d a single 8-element column in a 64-element array.
		/// </summary>
		/// <param name="input">Input vector</param>
		/// <param name="output">Output vector</param>
		/// <param name="offset">Offset for input vector (column)</param>
		private void IDCTColumn(float[] input, float[] output, int offset)
		{
			const int stepIn = 8;
			const int stepOut = 8;
			float Y04P = input[offset + 0 * stepIn] + input[offset + 4 * stepIn];
			float Y2b6eP = cB * input[offset + 2 * stepIn] + cE * input[offset + 6 * stepIn];

			float Y04P2b6ePP = Y04P + Y2b6eP;
			float Y04P2b6ePM = Y04P - Y2b6eP;
			float Y7f1aP3c5dPP = cF * input[offset + 7 * stepIn] + cA * input[offset + 1 * stepIn] + cC * input[offset + 3 * stepIn] + cD * input[offset + 5 * stepIn];
			float Y7a1fM3d5cMP = cA * input[offset + 7 * stepIn] - cF * input[offset + 1 * stepIn] + cD * input[offset + 3 * stepIn] - cC * input[offset + 5 * stepIn];

			float Y04M = input[offset + 0 * stepIn] - input[offset + 4 * stepIn];
			float Y2e6bM = cE * input[offset + 2 * stepIn] - cB * input[offset + 6 * stepIn];

			float Y04M2e6bMP = Y04M + Y2e6bM;
			float Y04M2e6bMM = Y04M - Y2e6bM;
			float Y1c7dM3f5aPM = cC * input[offset + 1 * stepIn] - cD * input[offset + 7 * stepIn] - cF * input[offset + 3 * stepIn] - cA * input[offset + 5 * stepIn];
			float Y1d7cP3a5fMM = cD * input[offset + 1 * stepIn] + cC * input[offset + 7 * stepIn] - cA * input[offset + 3 * stepIn] + cF * input[offset + 5 * stepIn];

			output[offset + 0 * stepOut] = cNorm * (Y04P2b6ePP + Y7f1aP3c5dPP);
			output[offset + 7 * stepOut] = cNorm * (Y04P2b6ePP - Y7f1aP3c5dPP);
			output[offset + 4 * stepOut] = cNorm * (Y04P2b6ePM + Y7a1fM3d5cMP);
			output[offset + 3 * stepOut] = cNorm * (Y04P2b6ePM - Y7a1fM3d5cMP);

			output[offset + 1 * stepOut] = cNorm * (Y04M2e6bMP + Y1c7dM3f5aPM);
			output[offset + 5 * stepOut] = cNorm * (Y04M2e6bMM - Y1d7cP3a5fMM);
			output[offset + 2 * stepOut] = cNorm * (Y04M2e6bMM + Y1d7cP3a5fMM);
			output[offset + 6 * stepOut] = cNorm * (Y04M2e6bMP - Y1c7dM3f5aPM);
		}

		#endregion

		#region Supporting Methods

		/// <summary>
		/// Performs quantization step on an 8x8 component block during encoding.
		/// </summary>
		/// <param name="inputData">An 8x8 component block.</param>
		/// <param name="code">Which divisors vector to use.</param>
		/// <param name="output">An int[] array to hold the quantized data.</param>
		internal void QuantizeBlock(float[][] inputData, int code, int[] output)
		{
			int index = 0;

			for (int i = 0; i < N; i++)
			{
				for (int j = 0; j < N; j++)
				{
					// output[index] = (int)(Math.Round(inputData[i][j] * divisors[code][index]));
					output[index] = (int)(inputData[i][j] * divisors[code][index]);
					index++;
				}
			}
		}

		/// <summary>
		/// Dequantize a coefficient by multiplying it by the multiplier-table
		/// entry; produce a float result.
		/// </summary>
		private static float FLOAT_DEQUANTIZE(short coef, float quantval)
		{
			return (coef * (quantval));
		}

		/// <summary>
		/// Dequantize a coefficient by multiplying it by the multiplier-table
		/// entry; produce a float result.
		/// </summary>
		private static float FLOAT_DEQUANTIZE(float coef, int quantval)
		{
			return (coef * (quantval));
		}

		/* We assume that right shift corresponds to signed division by 2 with
		* rounding towards minus infinity.  This is correct for typical "arithmetic
		* shift" instructions that shift in copies of the sign bit.
		* RIGHT_SHIFT provides a proper signed right shift of an int quantity.
		* It is only applied with constant shift counts.  SHIFT_TEMPS must be
		* included in the variables of any routine using RIGHT_SHIFT.
		*/
		public static int RIGHT_SHIFT(int x, int shft)
		{
			return (x >> shft);
		}

		/* Descale and correctly round an int value that's scaled by N bits.
		* We assume RIGHT_SHIFT rounds towards minus infinity, so adding
		* the fudge factor is correct for either sign of X.
		*/
		public static int DESCALE(int x, int n)
		{
			return RIGHT_SHIFT(x + (1 << (n - 1)), n);
		}
		#endregion

	}

}
