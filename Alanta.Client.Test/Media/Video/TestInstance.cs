using System;
using System.Collections.Generic;
using Alanta.Client.Media;
using Alanta.Client.Media.VideoCodecs;

namespace Alanta.Client.Test.Media.Video
{
	public class TestInstance
	{
		#region Constructors
		public TestInstance(IVideoQualityController videoQualityController, IVideoCodec videoCodec, List<byte[]> rawFrames, List<byte[]> processedFrames, ObjectPool<ByteStream> videoChunkPool)
		{
			VideoQualityController = videoQualityController;
			VideoCodec = videoCodec;
			RawFrames = rawFrames;
			ProcessedFrames = processedFrames;
			RawSize = RawFrames.Count * RawFrames[0].Length;
			mVideoChunkPool = videoChunkPool;
		}

		#endregion

		#region Fields and Properties

		private DateTime mStartTime;
		private DateTime mFinishTime;
		public TimeSpan Duration { get; private set; }
		public IVideoCodec VideoCodec { get; private set; }
		public IVideoQualityController VideoQualityController { get; private set; }
		public List<byte[]> RawFrames { get; private set; }
		public List<byte[]> ProcessedFrames { get; private set; }
		public double Fidelity { get; private set; }
		public int RawSize { get; private set; }
		public int Kbps { get; private set; }
		public double Compression { get; private set; }
		public double BlocksNotSent { get; private set; }
		public double BlocksAsDelta { get; private set; }
		public double BlocksAsJpeg { get; private set; }
		public double DeltaBlockSize { get; private set; }
		public double JpegBlockSize { get; private set; }
		public int EncodedSize { get; set; }
		private readonly ObjectPool<ByteStream> mVideoChunkPool;
		public bool KeepProcessedFrames { get; private set; }

		private int blocksAsDelta;
		private long bytesAsDelta;
		private long blocksAsJpeg;
		private long bytesAsJpeg;

		#endregion

		#region
		public void Execute(bool keepProcessedFrames)
		{
			mStartTime = DateTime.Now;
			foreach (var frame in RawFrames)
			{
				// Encode the frame
				VideoCodec.EncodeFrame(frame, 1);
				var dvc = VideoCodec as JpegDiffVideoCodec;
				if (dvc != null)
				{
					foreach (var block in dvc.EncodedBlocks)
					{
						if (block.BlockType == BlockType.Jpeg)
						{
							blocksAsJpeg++;
							bytesAsJpeg += block.EncodedStream.Length;
						}
						else
						{
							blocksAsDelta++;
							bytesAsDelta += block.EncodedStream.Length;
						}
					}
				}

				// Decode the frame
				bool moreChunks = true;
				var buffer = mVideoChunkPool.GetNext();
				while (moreChunks)
				{
					buffer.Reset();
					if (VideoCodec.GetNextChunk(buffer, out moreChunks))
					{
						EncodedSize += buffer.DataLength;
						VideoCodec.DecodeChunk(buffer, 2);
					}
				}
				mVideoChunkPool.Recycle(buffer);

				// Retrieve the frame
				var processedStream = VideoCodec.GetNextFrame();
				ProcessedFrames.Add(processedStream.ToArray());
			}
			Finish(keepProcessedFrames);
		}

		private void Finish(bool keepProcessedFrames)
		{
			mFinishTime = DateTime.Now;
			Duration = mFinishTime - mStartTime;
			Compression = EncodedSize / (double)RawSize;
			int seconds = RawFrames.Count / VideoQualityController.AcceptFramesPerSecond;
			Kbps = (EncodedSize / seconds) / 1024;
			Fidelity = CalculateFidelity(RawFrames, ProcessedFrames);

			const int blocksPerFrame = (VideoConstants.Height / VideoConstants.VideoBlockSize) * (VideoConstants.Width / VideoConstants.VideoBlockSize);
			float totalBlocks = RawFrames.Count * blocksPerFrame;
			BlocksAsDelta = blocksAsDelta / totalBlocks;
			BlocksAsJpeg = blocksAsJpeg / totalBlocks;
			BlocksNotSent = (totalBlocks - (blocksAsDelta + blocksAsJpeg)) / totalBlocks;
			DeltaBlockSize = bytesAsDelta / (double)blocksAsDelta;
			JpegBlockSize = bytesAsJpeg / (double)blocksAsJpeg;

			// Release references to potentially large objects we won't need anymore
			RawFrames = null;
			VideoCodec = null;
			if (!keepProcessedFrames)
			{
				ProcessedFrames.Clear();
			}
			KeepProcessedFrames = keepProcessedFrames;
		}

		private static double CalculateFidelity(IList<byte[]> rawFrames, IList<byte[]> processedFrames)
		{
			double sumDifferences = 0.0;
			int totalPixels = rawFrames.Count * (rawFrames[0].Length / 4);
			for (int i = 0; i < rawFrames.Count; i++)
			{
				var rawFrame = rawFrames[i];
				var processedFrame = processedFrames[i];
				for (int j = 0; j < rawFrame.Length; j++)
				{
					var rawB = rawFrame[j];
					var processedB = processedFrame[j++];
					var rawG = rawFrame[j];
					var processedG = processedFrame[j++];
					var rawR = rawFrame[j];
					var processedR = processedFrame[j++];
					var distance = VideoHelper.GetColorDistance(rawR, rawG, rawB, processedR, processedG, processedB);
					sumDifferences += distance; // (distance * distance);
				}
			}
			var avg = sumDifferences / totalPixels;
			return avg;
		}

		#endregion
	}
}
