using Alanta.Client.Media.Jpeg.Decoder;

namespace Alanta.Client.Media.Jpeg
{
	public class JpegFrameContext
	{
		#region Constructors
		public JpegFrameContext(byte quality, ushort height, ushort width, 
			JpegQuantizationTable luminance = null, JpegQuantizationTable chrominance = null, byte[] hSampFactor = null, byte[] vSampFactor = null)
		{
			Quality = quality;
			Height = height;
			Width = width;

			HSampFactor = hSampFactor ?? FrameDefaults.HSampFactor;
			VSampFactor = vSampFactor ?? FrameDefaults.VSampFactor;
			Luminance = luminance ?? JpegQuantizationTable.K1Luminance;
			Chrominance = chrominance ?? JpegQuantizationTable.K2Chrominance;

			DCT = new DCT(Quality, Luminance, Chrominance);
			HuffmanTable = HuffmanTable.GetHuffmanTable(null);

			JpegFrame = new JpegFrame();
			JpegFrame.ScanLines = height;
			JpegFrame.SamplesPerLine = width;
			JpegFrame.Precision = 8; // Number of bits per sample
			JpegFrame.ComponentCount = FrameDefaults.NumberOfComponents;  // Number of components (Y, Cb, Cr)

			var qTables = new JpegQuantizationTable[2];
			qTables[0] = JpegQuantizationTable.GetJpegQuantizationTable(DCT.quantum[0]);
			qTables[1] = JpegQuantizationTable.GetJpegQuantizationTable(DCT.quantum[1]);

			for (byte i = 0; i < FrameDefaults.NumberOfComponents; i++)
			{
				JpegFrame.AddComponent(FrameDefaults.CompId[i], HSampFactor[i], VSampFactor[i], qTables[FrameDefaults.QtableNumber[i]]);
				JpegFrame.SetHuffmanTables(FrameDefaults.CompId[i], JpegFrame.AcTables[FrameDefaults.ACtableNumber[i]], JpegFrame.DcTables[FrameDefaults.DCtableNumber[i]]);
			}
		}
		#endregion

		#region Fields and Properties
		public byte Quality { get; private set; }

		public ushort Height { get; private set; }

		public ushort Width { get; private set; }

		public DCT DCT { get; private set; }

		public JpegFrame JpegFrame { get; private set; }

		public byte[] HSampFactor { get; private set; }

		public byte[] VSampFactor { get; private set; }

		public JpegQuantizationTable Luminance { get; private set; }

		public JpegQuantizationTable Chrominance { get; private set; }

		public HuffmanTable HuffmanTable { get; private set; }

		public readonly float[][] DctArray1 = JaggedArrayHelper.Create2DJaggedArray<float>(8, 8);
		public readonly float[][] DctArray2 = JaggedArrayHelper.Create2DJaggedArray<float>(8, 8);
		public readonly int[] dctArray3 = new int[8 * 8];

		#endregion

	}
}
