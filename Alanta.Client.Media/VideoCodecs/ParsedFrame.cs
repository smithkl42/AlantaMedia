using System;

namespace Alanta.Client.Media.VideoCodecs
{
	public class ParsedFrame : VideoFrame
	{
		public ParsedFrame(IVideoQualityController videoQualityController, byte[] image, int height, int width, int frameNumber, int stride, IObjectPool<FrameBlock> frameBlockPool)
			: base(height, width, frameBlockPool)
		{
			this.videoQualityController = videoQualityController;
			this.frameBlockPool = frameBlockPool;
			this.stride = stride;
			FrameNumber = frameNumber;
			IsIFrame = FrameNumber % videoQualityController.FullFrameInterval == 0;
			ParseImage(image);
		}

		private readonly IVideoQualityController videoQualityController;
		public int FrameNumber { get; set; }
		private readonly IObjectPool<FrameBlock> frameBlockPool;
		private readonly int stride;

		private void ParseImage(byte[] image)
		{

			// If this is an IFrame, flag all blocks to be sent.
			int interleave = FrameNumber % videoQualityController.InterleaveFactor;
			for (int blockY = 0; blockY < verticalBlocks; blockY++)
			{
				for (int blockX = 0; blockX < horizontalBlocks; blockX++)
				{
					int currentBlock = (blockY * horizontalBlocks) + blockX;

					// If this is an IFrame, send the block, otherwise, check to see if we should send it.
					if (IsIFrame || currentBlock % videoQualityController.InterleaveFactor == interleave)
					{
						var block = GetBlockFromImage(image, blockX, blockY);
						FrameBlocks[currentBlock] = block;
					}
				}
			}
		}

		private FrameBlock GetBlockFromImage(byte[] image, int blockX, int blockY)
		{
			FrameBlock block = frameBlockPool.GetNext();
			block.BlockX = (byte)blockX;
			if (stride <= 0)
			{
				block.BlockY = (byte)(verticalBlocks - blockY - 1); // Reverse the y-axis because the image is upside down.
			}
			else
			{
				block.BlockY = (byte)blockY;
			}

			int startingOffset = (blockX * BlockSizeInBytes) + (blockY * horizontalBlocks * TotalBlockSizeInBytes);
			for (int line = 0; line < BlockSize; line++)
			{
				int sampleLine;
				if (stride <= 0)
				{
					sampleLine = BlockSize - line - 1; // Reverse the y axis because the image is upside down.
				}
				else
				{
					sampleLine = line;
				}
				int sourceStartPos = startingOffset + (sampleLine * widthInBytes);
				int destStartPos = line * BlockSizeInBytes;
				Buffer.BlockCopy(image, sourceStartPos, block.RgbaRaw, destStartPos, BlockSizeInBytes);
			}
			return block;
		}

		public bool IsIFrame { get; private set;}

	}
}
