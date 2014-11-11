using System.Collections.Generic;
using System.IO;
using System;

namespace Alanta.Client.Media.VideoCodecs
{
	public class NullVideoCodec : IVideoCodec
	{
		private byte[] lastFrame;

		public void Initialize(ushort height, ushort width, short maxPacketSize)
		{
			lastFrame = new byte[height * width * VideoConstants.BytesPerPixel];
		}

		public void EncodeFrame(byte[] image, int stride)
		{
			int size = image.Length;
			if (stride <= 0)
			{
				// If the stride is 0 or negative, the image needs to be reversed.
				for (int i = 0; i < size; i += VideoConstants.BytesPerPixel) // No need to increment here.
				{
					lastFrame[i] = image[size - i - 4];
					lastFrame[i + 1] = image[size - i - 3];
					lastFrame[i + 2] = image[size - i - 2];
					lastFrame[i + 3] = image[size - i - 1];
				}
			}
			else
			{
				Buffer.BlockCopy(image, 0, lastFrame, 0, size);
			}
		}

		public List<ByteStream> GetNextChunk(IObjectPool<ByteStream> videoChunkPool)
		{
			return new List<ByteStream>();
		}

		public bool GetNextChunk(ByteStream buffer, out bool moreChunks)
		{
			moreChunks = false;
			return true;
		}

		public void DecodeChunk(ByteStream frame, ushort remoteSsrcId)
		{
			// No-op.
		}

		public MemoryStream GetNextFrame()
		{
			return new MemoryStream(lastFrame);
		}

		public bool IsReceivingData
		{
			get
			{
				return true;
			}
		}

		public void Synchronize()
		{
			// No-op
		}
	}
}
