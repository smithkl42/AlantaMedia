// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System.Collections.Generic;
using System.IO;
using Alanta.Client.Media.Jpeg.IO;

namespace Alanta.Client.Media.Jpeg.Decoder
{
	/// <summary>
	/// The JpegFrameDecoder is a stripped-down version of the JpegDecoder for use in video codecs.  It attempts only to decode an encoded JPEG frame,
	/// without any of the JFIF headers.  Configuration values such as height and width must be supplied to the JpegFrameDecoder in some other fashion.
	/// </summary>
	public class JpegFrameDecoder
	{
		public BlockUpsamplingMode BlockUpsamplingMode { get; set; }

		private readonly JpegFrameContext context;
		readonly Stream input;

		public JpegFrameDecoder(Stream input, JpegFrameContext context)
		{
			this.input = input;
			this.context = context;
		}

		public void Decode(byte[][][] rasterOutput)
		{
			// This assumes that the stream contains only a single frame.
			var jpegReader = new JpegBinaryReader(input);
			JpegFrame frame = context.JpegFrame;

			// Not relevant
			byte marker = JpegMarker.XFF;
			const int resetInterval = 255;

			frame.DecodeScan(FrameDefaults.NumberOfComponents, FrameDefaults.CompId, resetInterval, jpegReader, ref marker);

			// Only one frame, JPEG Non-Hierarchical Frame.
			// byte[][,] raster = Image.CreateRasterBuffer(frame.Width, frame.Height, frame.ComponentCount);

			IList<JpegComponent> components = frame.Scan.Components;

			// parse.Stop();

			for (int i = 0; i < components.Count; i++)
			{
				JpegComponent comp = components[i];

				// 1. Quantize
				// comp.QuantizationTable = qTables[comp.quant_id].Table;
				// Only the AAN needs this. The quantization step is built into the other IDCT implementations
				if (JpegConstants.SelectedIdct != IdctImplementation.AAN)
				{
					comp.QuantizeData();
				}

				// 2. Run iDCT (expensive)
				// idct.Start();
				comp.IdctData();
				// idct.Stop();

				// 3. Scale the image and write the data to the raster.
				comp.WriteDataScaled(rasterOutput, i, BlockUpsamplingMode);
			}
		}


	}
}
