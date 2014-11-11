using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media.VideoCodecs
{

	public class JpegDiffVideoCodec : IVideoCodec
	{

		#region Constructors

		public JpegDiffVideoCodec(IVideoQualityController videoQualityController, MediaStatistics mediaStatistics = null)
		{
			_videoQualityController = videoQualityController;
			EncodedBlocks = new Queue<FrameBlock>(VideoConstants.MaxQueuedBlocksPerStream);
			if (mediaStatistics != null)
			{
				_blocksNotTransmittedCounter = mediaStatistics.RegisterCounter("Video: Blocks Not Transmitted %");
				_blocksNotTransmittedCounter.AxisMinimum = 0;
				_blocksNotTransmittedCounter.AxisMaximum = 100;
				_blocksJpegCounter = mediaStatistics.RegisterCounter("Video:Blocks Jpeg Transmitted %");
				_blocksJpegCounter.AxisMinimum = 0;
				_blocksJpegCounter.AxisMaximum = 100;
				_blocksDeltaCounter = mediaStatistics.RegisterCounter("Video:Blocks Delta Transmitted %");
				_blocksDeltaCounter.AxisMinimum = 0;
				_blocksDeltaCounter.AxisMaximum = 100;
			}

			var contextFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			_frameBlockPool = new ReferenceCountedObjectPool<FrameBlock>(
				() => new FrameBlock(contextFactory),
				fb =>
				{
					fb.CumulativeDifferences = 0;
				});
		}

		public void Initialize(ushort height, ushort width, short maxPacketSize)
		{
			_height = height;
			_width = width;
			_referenceFrame = new ParsedFrame(_videoQualityController, new byte[height * width * VideoConstants.BytesPerPixel], height, width, 0, 0, _frameBlockPool);
			_decodedFrame = new DecodedFrame(height, width, _frameBlockPool);
			_chunkHelper = new ChunkHelper(maxPacketSize, _frameBlockPool, _videoQualityController);
		}
		#endregion

		#region Fields and Properties

		private readonly IVideoQualityController _videoQualityController;
		private ushort _height;
		private ushort _width;

		public readonly Queue<FrameBlock> EncodedBlocks;
		private readonly IObjectPool<FrameBlock> _frameBlockPool;
		private ParsedFrame _newFrame;
		private ParsedFrame _referenceFrame;
		private DecodedFrame _decodedFrame;
		private ChunkHelper _chunkHelper;
		private int _frameNumber;
		private readonly Counter _blocksNotTransmittedCounter;
		private readonly Counter _blocksJpegCounter;
		private readonly Counter _blocksDeltaCounter;
		private bool _nextFrameIsIFrame;

		private static long blocksNotEncoded;
		private static long blocksJpegEncoded;
		private static long blocksDeltaEncoded;
		private static long blocksTotal;
		#endregion

		#region Public Methods
		/// <summary>
		/// Encodes a raw video image and stores the resulting blocks in an EncodedBlocks queue.
		/// </summary>
		/// <param name="image">The raw RGBA video image to be encoded.</param>
		/// <param name="stride">The stride of the image, i.e., whether it's encoded upside down or not</param>
		public void EncodeFrame(byte[] image, int stride)
		{
			_newFrame = new ParsedFrame(_videoQualityController, image, _height, _width, _frameNumber++, stride, _frameBlockPool);
			if (_newFrame.IsIFrame || _nextFrameIsIFrame)
			{
				_nextFrameIsIFrame = false;
				EnqueueAllBlocks();
			}
			else
			{
				EnqueueChangedBlocks();
			}
		}

		/// <summary>
		/// Pulls encoded blocks from the EncodedBlocks queue and writes them onto a ByteStream buffer.
		/// </summary>
		/// <param name="buffer">The buffer onto which the chunks should be written.</param>
		/// <param name="moreChunks">Whether there are still more chunks which could be retrieved.</param>
		/// <returns>True if a chunk was retrieved, false if not.</returns>
		public bool GetNextChunk(ByteStream buffer, out bool moreChunks)
		{
			bool chunkFound = _chunkHelper.GetNextChunkFromQueue(EncodedBlocks, buffer);
			moreChunks = EncodedBlocks.Count > 0;
			return chunkFound;
		}

		public void DecodeChunk(ByteStream frame, ushort remoteSsrcId)
		{
			var blocks = _chunkHelper.ParseChunk(frame, remoteSsrcId);
			foreach (var block in blocks)
			{
				_decodedFrame.ProcessBlock(block);
				_frameBlockPool.Recycle(block);
			}
		}

		public MemoryStream GetNextFrame()
		{
			return _decodedFrame.GetCurrentFrame();
		}

		public bool IsReceivingData
		{
			get
			{
				return (DateTime.Now < _decodedFrame.LastBlockProcessedAt + TimeSpan.FromMilliseconds(VideoConstants.RemoteCameraTimeout));
			}
		}

		public void Synchronize()
		{
			// Tell the codec to send iframes (typically when a new user joins).
			ThreadingHelper.SleepAsync(TimeSpan.FromMilliseconds(1000), () => _nextFrameIsIFrame = true);
			ThreadingHelper.SleepAsync(TimeSpan.FromMilliseconds(2000), () => _nextFrameIsIFrame = true);
			ThreadingHelper.SleepAsync(TimeSpan.FromMilliseconds(3000), () => _nextFrameIsIFrame = true);
			ThreadingHelper.SleepAsync(TimeSpan.FromMilliseconds(4000), () => _nextFrameIsIFrame = true);
		}

		#endregion

		#region Private Methods
		private void EnqueueAllBlocks()
		{
			for (int i = 0; i < _newFrame.FrameBlocks.Length; i++)
			{
				var newBlock = _newFrame.FrameBlocks[i];
				if (newBlock != null)
				{
					newBlock.BlockType = BlockType.Jpeg;
					EncodeAndEnqueueBlock(newBlock);
					_referenceFrame.InsertBlock(newBlock, i);
					_frameBlockPool.Recycle(newBlock);
				}
			}
		}

		private DateTime _lastUpdate = DateTime.MinValue;
		private readonly TimeSpan _timeSpanBetweenStatUpdates = TimeSpan.FromSeconds(5);

		private void EnqueueChangedBlocks()
		{
			for (int i = 0; i < _newFrame.FrameBlocks.Length; i++)
			{
				var newBlock = _newFrame.FrameBlocks[i];
				if (newBlock != null)
				{
					var oldBlock = _referenceFrame.FrameBlocks[i];
					double noSendCutoff = _videoQualityController.NoSendCutoff - oldBlock.CumulativeDifferences;
					double deltaCutoff = _videoQualityController.DeltaSendCutoff - oldBlock.CumulativeDifferences;
					var distance = newBlock.SubtractFrom(oldBlock, noSendCutoff, deltaCutoff);
					var cumulativeDifferences = distance + oldBlock.CumulativeDifferences;
					if (cumulativeDifferences > _videoQualityController.NoSendCutoff)
					{
						// newBlock.CumulativeDifferences = cumulativeDifferences;
						if (cumulativeDifferences < _videoQualityController.DeltaSendCutoff)
						{
							newBlock.CumulativeDifferences = oldBlock.CumulativeDifferences + 500; // 500 seems to be about the error added by delta encoding.
							newBlock.BlockType = BlockType.JpegDiff;
							blocksDeltaEncoded++;
						}
						else
						{
							newBlock.CumulativeDifferences = 0;
							newBlock.BlockType = BlockType.Jpeg;
							blocksJpegEncoded++;
						}
						EncodeAndEnqueueBlock(newBlock);

						// Update the previous reference block
						_referenceFrame.InsertBlock(newBlock, i);
					}
					else
					{
						oldBlock.CumulativeDifferences = cumulativeDifferences;
						blocksNotEncoded++;
					}

					// Reclaim the reference created when we first retrieved the block.
					_frameBlockPool.Recycle(newBlock);

					Interlocked.Increment(ref blocksTotal);
					if ((DateTime.Now - _lastUpdate) > _timeSpanBetweenStatUpdates)
					{
						if (_blocksNotTransmittedCounter != null)
						{
							float percent = blocksNotEncoded / (float)blocksTotal;
							_blocksNotTransmittedCounter.Update(percent * 100);
						}
						if (_blocksJpegCounter != null)
						{
							float percent = blocksJpegEncoded / (float)blocksTotal;
							_blocksJpegCounter.Update(percent * 100);
						}
						if (_blocksDeltaCounter != null)
						{
							float percent = blocksDeltaEncoded / (float)blocksTotal;
							_blocksDeltaCounter.Update(percent * 100);
						}
						blocksTotal = 0;
						blocksJpegEncoded = 0;
						blocksDeltaEncoded = 0;
						blocksNotEncoded = 0;
						_lastUpdate = DateTime.Now;
					}
				}
			}
		}

		/// <summary>
		/// Enqueues an encoded 16x16 block for later transmission.
		/// </summary>
		/// <param name="newBlock">The FrameBlock to be queued for transmission</param>
		private void EncodeAndEnqueueBlock(FrameBlock newBlock)
		{
			lock (EncodedBlocks)
			{
				TrimQueueIfNecessary();
				newBlock.Encode(_videoQualityController.JpegQuality);

				// Increment the reference count before we insert the block into an external container.
				newBlock.ReferenceCount++;
				EncodedBlocks.Enqueue(newBlock);
			}
		}

		/// <summary>
		/// Removes the oldest blocks from the queue if the queue has grown too large.
		/// </summary>
		private void TrimQueueIfNecessary()
		{
			while (EncodedBlocks.Count > VideoConstants.MaxQueuedBlocksPerStream)
			{
				ClientLogger.Debug("Encode queue overrun: more than " + VideoConstants.MaxQueuedBlocksPerStream + " blocks in the queue.");
				var block = EncodedBlocks.Dequeue();
				_frameBlockPool.Recycle(block);
			}
		}

		#endregion

	}
}
