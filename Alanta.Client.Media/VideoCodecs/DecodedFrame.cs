using System;
using System.IO;

namespace Alanta.Client.Media.VideoCodecs
{
	public class DecodedFrame : VideoFrame
	{
		public DecodedFrame(int height, int width, IObjectPool<FrameBlock> frameBlockPool)
			: base(height, width, frameBlockPool)
		{
			frame = new byte[height * width * VideoConstants.BytesPerPixel];
		}

		#region Fields and Properties
		private readonly byte[] frame;
		private DateTime lastBlockProcessedAt = DateTime.Now - TimeSpan.FromMilliseconds(VideoConstants.RemoteCameraTimeout + 1);
		public DateTime LastBlockProcessedAt
		{
			get
			{
				return lastBlockProcessedAt;
			}
		}
		#endregion

		#region Methods
		public MemoryStream GetCurrentFrame()
		{
			lock (frame)
			{
				return new MemoryStream(frame);
			}
		}

		public void ProcessBlock(FrameBlock newBlock)
		{
			// Decode the block
			lastBlockProcessedAt = DateTime.Now;
			newBlock.Decode();
			int blockIndex = horizontalBlocks*newBlock.BlockY + newBlock.BlockX;
			if (newBlock.BlockType == BlockType.JpegDiff)
			{
				var oldBlock = FrameBlocks[blockIndex];
				newBlock.AddTo(oldBlock);
			}
			InsertBlock(newBlock, blockIndex);

			// Insert the decoded block into the current frame buffer
			int startingOffset = (newBlock.BlockX * BlockSizeInBytes) + (newBlock.BlockY * horizontalBlocks * TotalBlockSizeInBytes);
			newBlock.EncodedStream.Position = 0;
			int blockOffset = 0;
			lock (frame)
			{
				for (int line = 0; line < BlockSize; line++)
				{
					int frameOffset = startingOffset + (line * widthInBytes);
					Buffer.BlockCopy(newBlock.RgbaRaw, blockOffset, frame, frameOffset, BlockSizeInBytes);
					blockOffset += BlockSizeInBytes;
				}
			}
		}
		#endregion
	}
}
