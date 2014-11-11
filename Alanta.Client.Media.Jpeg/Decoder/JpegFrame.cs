// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using Alanta.Client.Media.Jpeg.IO;

namespace Alanta.Client.Media.Jpeg.Decoder
{
	public class JpegFrame
	{
		public static byte JpegColorGray = 1;
		public static byte JpegColorRgb = 2;
		public static byte JpegColorYCbCr = 3;
		public static byte JpegColorCmyk = 4;

		public byte precision = 8;
		public byte colorMode = JpegColorYCbCr;

		public ushort Width { get; private set; }
		public ushort Height { get; private set; }

		public static readonly JpegHuffmanTable[] DcTables = new JpegHuffmanTable[2];
		public  static readonly JpegHuffmanTable[] AcTables = new JpegHuffmanTable[2];

		static JpegFrame()
		{
			// Default values for use when called by JpegFrameDecoder.
			DcTables[0] = JpegHuffmanTable.StdDCLuminance;
			DcTables[1] = JpegHuffmanTable.StdDCChrominance;
			AcTables[0] = JpegHuffmanTable.StdAcLuminance;
			AcTables[1] = JpegHuffmanTable.StdAcChrominance;
		}

		public JpegScan Scan = new JpegScan();
		public Action<long> ProgressUpdateMethod;

		public void AddComponent(byte componentId, byte sampleHFactor, byte sampleVFactor, byte quantizationTableId)
		{
			Scan.AddComponent(componentId, sampleHFactor, sampleVFactor, quantizationTableId, colorMode);
		}

		public void AddComponent(byte componentId, byte sampleHFactor, byte sampleVFactor, JpegQuantizationTable jpegQuantizationTable)
		{
			Scan.AddComponent(componentId, sampleHFactor, sampleVFactor, jpegQuantizationTable, colorMode);
		}

		public byte Precision
		{
			set { precision = value; }
		}

		public ushort ScanLines { set { Height = value; } }
		public ushort SamplesPerLine { set { Width = value; } }

		public byte ColorMode
		{
			get
			{
				return ComponentCount == 1 ? JpegColorGray : JpegColorYCbCr;
			}
		}

		public byte ComponentCount { get; set; }

		public void SetHuffmanTables(byte componentId, JpegHuffmanTable acTable, JpegHuffmanTable dcTable)
		{
			JpegComponent comp = Scan.GetComponentById(componentId);
			if (dcTable != null) comp.setDCTable(dcTable);
			if (acTable != null) comp.setACTable(acTable);
		}

		public void DecodeScanBaseline(byte numberOfComponents, byte[] componentSelector, int resetInterval, JpegBinaryReader jpegReader, ref byte marker)
		{
			// Set the decode function for all the components
			for (int compIndex = 0; compIndex < numberOfComponents; compIndex++)
			{
				JpegComponent comp = Scan.GetComponentById(componentSelector[compIndex]);
				comp.Decode = comp.DecodeBaseline;
			}

			DecodeScan(numberOfComponents, componentSelector, resetInterval, jpegReader, ref marker);
		}

		private int mcus_per_row(JpegComponent c)
		{
			return ((((Width * c.factorH) + (Scan.MaxH - 1)) / Scan.MaxH) + 7) / 8;
		}

		public void DecodeScan(byte numberOfComponents, byte[] componentSelector, int resetInterval, JpegBinaryReader jpegReader, ref byte marker)
		{
			//TODO: not necessary
			jpegReader.eobRun = 0;

			int mcuIndex = 0;
			int mcuTotalIndex = 0;

			// This loops through until a MarkerTagFound exception is
			// found, if the marker tag is a RST (Restart Marker) it
			// simply skips it and moves on this system does not handle
			// corrupt data streams very well, it could be improved by
			// handling misplaced restart markers.

			int h = 0, v = 0;
			int x = 0;

			long lastPosition = jpegReader.BaseStream.Position;

			foreach (JpegComponent component in Scan.Components)
			{
				component.Reset();
			}

			//TODO: replace this with a loop which knows how much data to expect
			while (true)
			{
				#region Inform caller of decode progress

				if (ProgressUpdateMethod != null)
				{
					if (jpegReader.BaseStream.Position >= lastPosition + JpegDecoder.ProgressUpdateByteInterval)
					{
						lastPosition = jpegReader.BaseStream.Position;
						ProgressUpdateMethod(lastPosition);
					}
				}

				#endregion

				// Loop though capturing MCU, instruct each
				// component to read in its necessary count, for
				// scaling factors the components automatically
				// read in how much they need

				// Sec A.2.2 from CCITT Rec. T.81 (1992 E)
				bool interleaved = numberOfComponents != 1;

				if (!interleaved)
				{
					#region Non-Interleaved (less common)
					JpegComponent comp = Scan.GetComponentById(componentSelector[0]);
					comp.SetBlock(mcuIndex);
					var status = comp.DecodeMCU(jpegReader, h, v);
					if (status.Status == Status.EOF)
					{
						return;
					}
					int mcusPerLine = mcus_per_row(comp);
					var blocksPerLine = (int)Math.Ceiling((double)Width / (8 * comp.factorH));

					// TODO: Explain the non-interleaved scan ------
					h++; x++;
					if (h == comp.factorH)
					{
						h = 0; mcuIndex++;
					}

					if ((x % mcusPerLine) == 0)
					{
						x = 0;
						v++;
						if (v == comp.factorV)
						{
							if (h != 0) { mcuIndex++; h = 0; }
							v = 0;
						}
						else
						{
							mcuIndex -= blocksPerLine;
							// we were mid-block
							if (h != 0) { mcuIndex++; h = 0; }
						}
					}

					// -----------------------------------------------
					#endregion

				}
				else // Components are interleaved
				{
					#region Interleaved (more common)
					for (int compIndex = 0; compIndex < numberOfComponents; compIndex++)
					{
						JpegComponent comp = Scan.GetComponentById(componentSelector[compIndex]);
						comp.SetBlock(mcuTotalIndex);

						for (int j = 0; j < comp.factorV; j++)
						{
							for (int i = 0; i < comp.factorH; i++)
							{
								// Decode the MCU
								var status = comp.DecodeMCU(jpegReader, i, j);
								if (status.Status == Status.EOF)
								{
									return;
								}
								if (status.Status == Status.MarkerFound)
								{
									// We've found a marker, see if the marker is a restart
									// marker or just the next marker in the stream. If
									// it's the next marker in the stream break out of the
									// while loop, if it's just a restart marker skip it
									marker = (byte)status.Result;

									// Handle JPEG Restart Markers, this is where the
									// count of MCU's per interval is compared with
									// the count actually obtained, if it's short then
									// pad on some MCU's ONLY for components that are
									// greater than one. Also restart the DC prediction
									// to zero.
									if (marker == JpegMarker.RST0
									    || marker == JpegMarker.RST1
									    || marker == JpegMarker.RST2
									    || marker == JpegMarker.RST3
									    || marker == JpegMarker.RST4
									    || marker == JpegMarker.RST5
									    || marker == JpegMarker.RST6
									    || marker == JpegMarker.RST7)
									{
										for (int compIndex2 = 0; compIndex2 < numberOfComponents; compIndex2++)
										{
											JpegComponent comp2 = Scan.GetComponentById(componentSelector[compIndex]);
											if (compIndex2 > 1)
												comp2.padMCU(mcuTotalIndex, resetInterval - mcuIndex);
											comp2.resetInterval();
										}

										mcuTotalIndex += (resetInterval - mcuIndex);
										mcuIndex = 0;
									}
									else
									{
										return; // We're at the end of our scan, exit out.
									}
								}
							}
						}
					}

					mcuIndex++;
					mcuTotalIndex++;
					#endregion
				}
			}
		}

		public bool DecodeScanProgressive(byte successiveApproximation, byte startSpectralSelection, byte endSpectralSelection,
										  byte numberOfComponents, byte[] componentSelector, int resetInterval, JpegBinaryReader jpegReader, ref byte marker)
		{

			var successiveHigh = (byte)(successiveApproximation >> 4);
			var successiveLow = (byte)(successiveApproximation & 0x0f);

			if ((startSpectralSelection > endSpectralSelection) || (endSpectralSelection > 63))
				throw new Exception("Bad spectral selection.");

			bool dcOnly = startSpectralSelection == 0;
			bool refinementScan = (successiveHigh != 0);

			if (dcOnly) // DC scan
			{
				if (endSpectralSelection != 0)
					throw new Exception("Bad spectral selection for DC only scan.");
			}
			else // AC scan
			{
				if (numberOfComponents > 1)
					throw new Exception("Too many components for AC scan!");
			}

			// Set the decode function for all the components
			// TODO: set this for the scan and let the component figure it out
			for (int compIndex = 0; compIndex < numberOfComponents; compIndex++)
			{
				JpegComponent comp = Scan.GetComponentById(componentSelector[compIndex]);

				comp.successiveLow = successiveLow;

				if (dcOnly)
				{
					if (refinementScan) // DC refine
						comp.Decode = comp.DecodeDCRefine;
					else  //               DC first
						comp.Decode = comp.DecodeDCFirst;
				}
				else
				{
					comp.spectralStart = startSpectralSelection;
					comp.spectralEnd = endSpectralSelection;

					if (refinementScan) // AC refine
						comp.Decode = comp.DecodeACRefine;
					else  //               AC first
						comp.Decode = comp.DecodeACFirst;
				}
			}

			DecodeScan(numberOfComponents, componentSelector, resetInterval, jpegReader, ref marker);
			return true;
		}

	}

}
