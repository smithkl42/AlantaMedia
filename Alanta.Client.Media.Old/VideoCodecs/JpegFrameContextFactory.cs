using Alanta.Client.Media.Jpeg;
using Alanta.Client.Media.Jpeg.Decoder;

namespace Alanta.Client.Media.VideoCodecs
{
	/// <summary>
	/// Allows cached instances of JpegFrameContext to be retrieved quickly.
	/// </summary>
	public class JpegFrameContextFactory
	{
		public JpegFrameContextFactory(ushort height, ushort width)
		{
			Height = height;
			Width = width;
		}

		public ushort Height { get; private set; }
		public ushort Width { get; private set; }

		private readonly JpegFrameContext[] normalContexts = new JpegFrameContext[101];
		private readonly JpegFrameContext[] deltaContexts = new JpegFrameContext[101];

		public JpegFrameContext GetJpegFrameContext(byte quality, BlockType blockType)
		{
			JpegFrameContext ctx = null;
			switch (blockType)
			{
				case BlockType.Jpeg:
					ctx = normalContexts[quality];
					if (ctx == null)
					{
						ctx = new JpegFrameContext(quality, Height, Width, JpegQuantizationTable.K1Luminance, JpegQuantizationTable.K2Chrominance);
						normalContexts[quality] = ctx;
					}
					break;
				case BlockType.JpegDiff:
					ctx = deltaContexts[quality];
					if (ctx == null)
					{
						ctx = new JpegFrameContext(quality, Height, Width, JpegQuantizationTable.Delta, JpegQuantizationTable.Delta);
						deltaContexts[quality] = ctx;
					}
					break;
			}
			return ctx;
		}
	}
}
