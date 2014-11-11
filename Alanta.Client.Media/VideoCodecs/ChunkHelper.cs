using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Alanta.Client.Media.VideoCodecs
{
	public class ChunkHelper
	{
		public ChunkHelper(short maxChunkSize, IObjectPool<FrameBlock> frameBlockPool, IVideoQualityController videoQualityController)
		{
			_maxChunkSize = maxChunkSize;
			_frameBlockPool = frameBlockPool;
			_videoQualityController = videoQualityController;
		}

		readonly short _maxChunkSize;
		// short blockSize = 0;
		protected const short ChunkHeaderLength = 4;
		protected const short BlockHeaderLength = 5;
		private readonly IObjectPool<FrameBlock> _frameBlockPool;
		private readonly IVideoQualityController _videoQualityController;

		public bool GetNextChunkFromQueue(Queue<FrameBlock> queue, ByteStream buffer)
		{
			byte blocks = 0;
			buffer.CurrentOffset = ChunkHeaderLength;
			short jpegQuality = 0;

			// Pull blocks from the queue and insert them into the buffer until the buffer is full or the block queue is empty.
			FrameBlock block = null;
			while (buffer.CurrentOffset < _maxChunkSize)
			{

				// See if there's room left in the current chunk for the next block.
				lock (queue)
				{
					// Stop pulling blocks from the queue if there are no more blocks.
					if (queue.Count == 0) break;

					// Stop pulling blocks from the queue if there's no more room in the chunk.
					var peek = queue.Peek();
					if ((buffer.CurrentOffset + BlockHeaderLength + peek.EncodedStream.Length) > _maxChunkSize && buffer.CurrentOffset > ChunkHeaderLength) break;

					// Stop pulling blocks from the queue if the jpegQuality has changed.
					if ((jpegQuality > 0 && peek.JpegQuality != jpegQuality)) break;

					block = queue.Dequeue();
				}
				blocks++;
				jpegQuality = block.JpegQuality;

				// Set the x,y position of the block.
				buffer.WriteByte(block.BlockX);
				buffer.WriteByte(block.BlockY);

				// Set the type of the block.
				buffer.WriteByte((byte)block.BlockType);

				// Set the size of the block.
				var streamLength = (short)block.EncodedStream.Length;
				Debug.Assert(streamLength > 0, "The length of the encoded stream must be greater than 0");
				buffer.WriteInt16(streamLength);

				// Set the actual block data.
				block.EncodedStream.Position = 0;
				block.EncodedStream.Read(buffer.Data, buffer.CurrentOffset, streamLength);
				buffer.CurrentOffset += streamLength;

				_frameBlockPool.Recycle(block);
			}

			// If we've retrieved at least one block, go back to the beginning of the buffer 
			// and set the chunk header information
			if (block != null)
			{
				buffer.Data[0] = blocks;
				buffer.Data[1] = (byte)_videoQualityController.CommandedVideoQuality;
				buffer.Data[2] = (byte)_videoQualityController.ProposedVideoQuality;
				buffer.Data[3] = (byte)jpegQuality;
				buffer.DataLength = buffer.CurrentOffset;
				buffer.ResetCurrentOffset();
				return true;
			}
			return false;
		}

		public FrameBlock[] ParseChunk(ByteStream chunk, ushort remoteSsrcId)
		{
			byte numBlocks = chunk.Data[0];

			// ks 7/18/11 - This seems like an odd place to do it, but I can't think of a better one.
			_videoQualityController.LogReceivedVideoQuality(remoteSsrcId, (VideoQuality)chunk.Data[1], (VideoQuality)chunk.Data[2]);

			byte jpegQuality = chunk.Data[3];
			Debug.Assert(jpegQuality > 0 && jpegQuality <= 100);

			chunk.CurrentOffset = ChunkHeaderLength;
			var blocks = new FrameBlock[numBlocks];
			for (int i = 0; i < numBlocks; i++)
			{
				var block = _frameBlockPool.GetNext();
				block.JpegQuality = jpegQuality;
				block.BlockX = chunk.ReadByte();
				block.BlockY = chunk.ReadByte();
				block.BlockType = (BlockType)chunk.ReadByte();
				short payloadLength = chunk.ReadInt16();
				Debug.Assert(payloadLength > 0, "The payloadLength must be greater than 0");
				block.EncodedStream = new MemoryStream(chunk.Data, chunk.CurrentOffset, payloadLength);
				chunk.CurrentOffset += payloadLength;
				blocks[i] = block;
			}
			return blocks;
		}
	}
}
