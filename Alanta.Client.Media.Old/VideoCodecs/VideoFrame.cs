using System;

namespace Alanta.Client.Media.VideoCodecs
{
	public abstract class VideoFrame
	{
		protected VideoFrame(int height, int width, IObjectPool<FrameBlock> frameBlockPool)
		{
			if (width % BlockSize != 0 || height % BlockSize != 0)
			{
				throw new ArgumentException(string.Format("The height and width must be multiples of {0}.", VideoConstants.VideoBlockSize));
			}
			this.height = (short)height;
			this.width = (short)width;
			this.frameBlockPool = frameBlockPool;
			widthInBytes = width * VideoConstants.BytesPerPixel;
			horizontalBlocks = width / BlockSize;
			verticalBlocks = height / BlockSize;
			int totalBlocks = horizontalBlocks * verticalBlocks;
			FrameBlocks = new FrameBlock[totalBlocks];
		}

		protected const ushort BlockSize = VideoConstants.VideoBlockSize;
		protected const ushort BlockSizeInBytes = BlockSize * VideoConstants.BytesPerPixel;
		protected const ushort TotalBlockSize = BlockSize * BlockSize;
		protected const ushort TotalBlockSizeInBytes = TotalBlockSize * VideoConstants.BytesPerPixel;
		protected readonly short height;
		protected readonly short width;
		protected readonly int widthInBytes;
		protected readonly int horizontalBlocks;
		protected readonly int verticalBlocks;
		public FrameBlock[] FrameBlocks { get; private set; }
		private readonly IObjectPool<FrameBlock> frameBlockPool;

		public void InsertBlock(FrameBlock newBlock, int index)
		{
			frameBlockPool.Recycle(FrameBlocks[index]);
			newBlock.ReferenceCount++;
			FrameBlocks[index] = newBlock;
		}
	}
}
