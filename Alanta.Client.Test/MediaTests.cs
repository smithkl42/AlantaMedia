using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using Alanta.Client.Media.Jpeg;
using Alanta.Client.Media.Jpeg.Decoder;
using Alanta.Client.Media.Jpeg.Encoder;
using Alanta.Client.Media.VideoCodecs;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class MediaTests : SilverlightTest
	{
		#region Fields and Properties

		private const ushort blockSize = VideoConstants.VideoBlockSize;
		private const ushort totalBlockSize = blockSize * blockSize * VideoConstants.BytesPerPixel;
		private const int blockLineLength = blockSize * VideoConstants.BytesPerPixel;
		private const int yIndex = 0;
		private const int cbIndex = 1;
		private const int crIndex = 2;
		private readonly byte[][] _rgbaSamples = new byte[20][];
		private const ushort height = VideoConstants.Height;
		private const ushort width = VideoConstants.Width;
		private readonly Dictionary<ushort, VideoThreadData> _remoteSessions = new Dictionary<ushort, VideoThreadData>();
		private readonly VideoQualityController _videoQualityController;

		#endregion

		#region Constructors
		public MediaTests()
		{
			_remoteSessions[1] = null;
			_videoQualityController = new VideoQualityController(0);
		}
		#endregion

		#region Bystream Tests

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_Int16_Test()
		{
			var bs = new ByteStream(new byte[sizeof(Int16) * 4]);
			bs.WriteInt16(Int16.MinValue);
			bs.WriteInt16(Int16.MaxValue);
			bs.WriteInt16Network(Int16.MinValue);
			bs.WriteInt16Network(Int16.MaxValue);
			bs.ResetCurrentOffset();
			Assert.AreEqual(Int16.MinValue, bs.ReadInt16());
			Assert.AreEqual(Int16.MaxValue, bs.ReadInt16());
			Assert.AreEqual(Int16.MinValue, bs.ReadInt16Network());
			Assert.AreEqual(Int16.MaxValue, bs.ReadInt16Network());
		}

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_UInt16_Test()
		{
			var bs = new ByteStream(new byte[sizeof(UInt16) * 4]);
			bs.WriteUInt16(UInt16.MinValue);
			bs.WriteUInt16(UInt16.MaxValue);
			bs.WriteUInt16Network(UInt16.MinValue);
			bs.WriteUInt16Network(UInt16.MaxValue);
			bs.ResetCurrentOffset();
			Assert.AreEqual(UInt16.MinValue, bs.ReadUInt16());
			Assert.AreEqual(UInt16.MaxValue, bs.ReadUInt16());
			Assert.AreEqual(UInt16.MinValue, bs.ReadUInt16Network());
			Assert.AreEqual(UInt16.MaxValue, bs.ReadUInt16Network());
		}

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_Int32_Test()
		{
			var bs = new ByteStream(new byte[sizeof(Int32) * 4]);
			bs.WriteInt32(Int32.MinValue);
			bs.WriteInt32(Int32.MaxValue);
			bs.WriteInt32Network(Int32.MinValue);
			bs.WriteInt32Network(Int32.MaxValue);
			bs.ResetCurrentOffset();
			Assert.AreEqual(Int32.MinValue, bs.ReadInt32());
			Assert.AreEqual(Int32.MaxValue, bs.ReadInt32());
			Assert.AreEqual(Int32.MinValue, bs.ReadInt32Network());
			Assert.AreEqual(Int32.MaxValue, bs.ReadInt32Network());
		}

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_UInt32_Test()
		{
			var bs = new ByteStream(new byte[sizeof(UInt32) * 4]);
			bs.WriteUInt32(UInt32.MinValue);
			bs.WriteUInt32(UInt32.MaxValue);
			bs.WriteUInt32Network(UInt32.MinValue);
			bs.WriteUInt32Network(UInt32.MaxValue);
			bs.ResetCurrentOffset();
			Assert.AreEqual(UInt32.MinValue, bs.ReadUInt32());
			Assert.AreEqual(UInt32.MaxValue, bs.ReadUInt32());
			Assert.AreEqual(UInt32.MinValue, bs.ReadUInt32Network());
			Assert.AreEqual(UInt32.MaxValue, bs.ReadUInt32Network());
		}

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_Byte_Test()
		{
			var bs = new ByteStream(new byte[10]);
			for (int i = 0; i < bs.DataLength; i++)
			{
				bs.WriteByte((byte)i);
			}
			bs.ResetCurrentOffset();
			for (int i = 0; i < bs.DataLength; i++)
			{
				Assert.AreEqual((byte)i, bs.ReadByte());
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_Bytes_Test()
		{
			var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
			var bs = new ByteStream(bytes.Length);
			bs.TryWriteBytes(bytes, 0, bytes.Length);
			bs.ResetCurrentOffset();
			var readBytes = new byte[bytes.Length];
			bs.TryReadBytes(readBytes, 0, readBytes.Length);
			for (int i = 0; i < bytes.Length; i++)
			{
				Assert.AreEqual(bytes[i], readBytes[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("bytestream")]
		public void ByteStream_InsertBytes_Test()
		{
			int arraySize = 100;
			var bs = new ByteStream(arraySize);
			byte[] newBytes = { 0xFF, 0xFF, 0xFF, 0xFF };
			bs.CurrentOffset = 10;
			bs.InsertBytes(newBytes, 0, newBytes.Length);
			Assert.AreEqual(bs.DataLength, arraySize + newBytes.Length);
			Assert.AreEqual(0x00, bs.Data[0]);
			Assert.AreEqual(0xFF, bs.Data[10]);
			Assert.AreEqual(0xFF, bs.Data[11]);
			Assert.AreEqual(0xFF, bs.Data[12]);
			Assert.AreEqual(0xFF, bs.Data[13]);
			Assert.AreEqual(0x00, bs.Data[14]);
		}

		#endregion

		#region Jitter Buffer Tests

		[TestMethod]
		[Tag("media")]
		[Tag("jitter")]
		public void JitterReadWriteTest()
		{
			var vqc = new VideoQualityController(1);
			vqc.RemoteSessions = _remoteSessions;
			var queue = new AudioJitterQueue(new CodecFactory(AudioFormat.Default), vqc);
			for (ushort i = 0; i < 20; i++)
			{
				short[] samples = GetAudioSamples(320, (short)i);
				queue.WriteSamples(samples, 0, samples.Length, i, AudioCodecType.Raw, false);
			}

			var buffer = new short[320];
			for (int i = 0; i < 20; i++)
			{
				queue.ReadSamples(buffer);
				Assert.AreEqual(i, buffer[0]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("jitter")]
		public void JitterOutOfOrderTest()
		{
			var vqc = new VideoQualityController(1);
			vqc.RemoteSessions = _remoteSessions;
			var queue = new AudioJitterQueue(new CodecFactory(AudioFormat.Default), vqc);

			// Write 10 samples in order.
			for (ushort i = 0; i < 10; i++)
			{
				short[] samples = GetAudioSamples(320, (short)i);
				queue.WriteSamples(samples, 0, samples.Length, i, AudioCodecType.Raw, false);
			}

			// Write 10 samples out of order.
			for (ushort i = 19; i >= 10; i--)
			{
				short[] samples = GetAudioSamples(320, (short)i);
				queue.WriteSamples(samples, 0, samples.Length, i, AudioCodecType.Raw, false);
			}

			// Read them back and confirm that they are all in order.
			var buffer = new short[320];
			for (int i = 0; i < 20; i++)
			{
				queue.ReadSamples(buffer);
				Assert.AreEqual(i, buffer[0]);
			}
		}

		#endregion

		#region Video Codec Tests

		/// <summary>
		/// Confirm that the blocks from the sample data are parsed correctly.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void ParsedFrameTest()
		{
			byte[] sample = GetSample();
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			var frameBlockPool = new ObjectPool<FrameBlock>(() => new FrameBlock(ctxFactory));
			var vqc = new VideoQualityController(1);
			vqc.RemoteSessions = _remoteSessions;
			var frame = new ParsedFrame(vqc, sample, height, width, 0, 0, frameBlockPool);
			for (int i = 0; i < frame.FrameBlocks.Length; i++)
			{
				if (frame.FrameBlocks[i] != null)
				{
					var expected = (byte)i;
					foreach (byte b in frame.FrameBlocks[i].RgbaRaw)
					{
						Assert.AreEqual(expected, b);
					}
				}
			}
		}

		/// <summary>
		/// Confirms that chunks are encoded and decoded correctly.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void ChunkHelperTest()
		{
			Queue<FrameBlock> queue = GetFrameBlockQueue();
			int initialLength = queue.Count;
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			var videoChunkPool = new ObjectPool<ByteStream>(() => new ByteStream(VideoConstants.MaxPayloadSize), bs => bs.Reset());
			var frameBlockPool = new ObjectPool<FrameBlock>(() => new FrameBlock(ctxFactory));
			var vqc = new VideoQualityController(1);
			vqc.RemoteSessions = _remoteSessions;
			var helper = new ChunkHelper(VideoConstants.MaxPayloadSize, frameBlockPool, vqc);
			var buffer = videoChunkPool.GetNext();
			Assert.IsTrue(helper.GetNextChunkFromQueue(queue, buffer));
			Assert.IsTrue(queue.Count < initialLength);
			var blocks = helper.ParseChunk(buffer, 1);
			Assert.AreEqual(queue.Count + blocks.Length, initialLength);
			Assert.IsTrue(blocks.Length > 0);
			foreach (var block in blocks)
			{
				Assert.AreEqual(block.BlockX, block.BlockY);
				Assert.AreEqual(block.BlockX + 1, block.EncodedStream.Length);
			}
		}

		/// <summary>
		/// Confirm that the JpegFrameEncoder and Decoder return something like the original image.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void JpegFrameEncoderDecoderTest()
		{
			// Encode and decode a basic raster structure.
			var colorModel = new ColorModel();
			colorModel.ColorSpace = ColorSpace.YCbCr;
			colorModel.Opaque = true;
			byte[][][] originalRaster = GetRaster();
			var image = new Image(colorModel, originalRaster);
			var stream = new MemoryStream();
			var ctx = new JpegFrameContext(50, height, width);
			var encoder = new JpegFrameEncoder(image, stream, ctx);
			encoder.Encode();
			stream.Seek(0, SeekOrigin.Begin);
			var decoder = new JpegFrameDecoder(stream, ctx);
			byte[][][] decodedRaster = Image.GetRaster(width, height, 3);
			decoder.Decode(decodedRaster);

			// Check that the returned raster structure looks something like what we passed in.
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < width; j++)
				{
					for (int k = 0; k < height; k++)
					{
						// Tune this.
						int diff = Math.Abs(decodedRaster[i][j][k] - originalRaster[i][j][k]);
						Assert.IsTrue(diff < 5);
					}
				}
			}
		}

		/// <summary>
		/// Confirm that the JpegFrameEncoder and Decoder return something like the original image.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void JpegEncoderDecoderTest()
		{
			// Encode and decode a basic raster structure.
			var colorModel = new ColorModel();
			colorModel.ColorSpace = ColorSpace.YCbCr;
			colorModel.Opaque = true;
			byte[][][] originalRaster = GetRaster();
			var image = new Image(colorModel, originalRaster);
			var stream = new MemoryStream();
			var encoder = new JpegEncoder(image, 50, stream);
			encoder.Encode();
			stream.Seek(0, SeekOrigin.Begin);
			var decoder = new JpegDecoder(stream);
			DecodedJpeg decodedImage = decoder.Decode();

			// Check that the returned raster structure looks something like what we passed in.
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < width; j++)
				{
					for (int k = 0; k < height; k++)
					{
						// Tune this.
						int diff = Math.Abs(decodedImage.Image.Raster[i][j][k] - originalRaster[i][j][k]);
						Assert.IsTrue(diff < 5);
					}
				}
			}
			ClientLogger.Debug("Finished JpegEncoderDecoder test.");
		}

		/// <summary>
		/// Tests whether the RtpPacket is encoded and decoded correctly.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void RtpPacketEncodingTest()
		{
			var outpacket = new RtpPacketData();
			var rtpPacketDataListRecycler = new ObjectPool<List<RtpPacketData>>(() => new List<RtpPacketData>());
			var rtpPacketDataRecycler = new ObjectPool<RtpPacketData>(() => new RtpPacketData());
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			var frameBlockPool = new ObjectPool<FrameBlock>(() => new FrameBlock(ctxFactory));
			outpacket.SequenceNumber = 255;
			outpacket.PayloadType = RtpPayloadType.Audio;
			Queue<FrameBlock> queue = GetFrameBlockQueue();
			var vqc = new VideoQualityController(1);
			vqc.RemoteSessions = _remoteSessions;
			var helper = new ChunkHelper(VideoConstants.MaxPayloadSize, frameBlockPool, vqc);
			// var videoChunkPool = new ObjectPool<ByteStream>(() => new ByteStream(VideoConstants.MaxPayloadSize), bs => bs.Reset());
			var chunk = new ByteStream(VideoConstants.MaxPayloadSize);
			helper.GetNextChunkFromQueue(queue, chunk);
			outpacket.Payload = chunk.Data;
			outpacket.PayloadLength = (ushort)chunk.DataLength;
			var data = new ByteStream(outpacket.BuildPacket());
			RtpPacketData inpacket = RtpPacketData.GetPacketsFromData(data, rtpPacketDataListRecycler, rtpPacketDataRecycler)[0];
			Assert.AreEqual(outpacket.SequenceNumber, inpacket.SequenceNumber);
			Assert.AreEqual(RtpPayloadType.Audio, inpacket.PayloadType);
			Assert.AreEqual(outpacket.PayloadLength, inpacket.PayloadLength);
		}

		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void FrameBlockDeltaTest()
		{
			for (int oldbyte = byte.MinValue; oldbyte <= byte.MaxValue; oldbyte++)
			{
				for (int newbyte = byte.MinValue; newbyte <= byte.MaxValue; newbyte++)
				{
					var delta = FrameBlock.EncodeDelta((byte)oldbyte, (byte)newbyte);
					var reconstructedNewByte = FrameBlock.ReconstructOriginal((byte)oldbyte, delta);
					var error = Math.Abs(newbyte - reconstructedNewByte);
					Assert.IsTrue(error < 2);
				}
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void FrameBlockSubtractAddTest()
		{
			var ctxFactory = new JpegFrameContextFactory(byte.MaxValue, byte.MaxValue);
			var frameBlockPool = new ObjectPool<FrameBlock>(() => new FrameBlock(ctxFactory));
			var oldFrameBlock = frameBlockPool.GetNext();
			var newFrameBlock = frameBlockPool.GetNext();
			int pos = 0;
			for (int i = 0; i < byte.MaxValue; i++)
			{
				for (int j = 0; j < byte.MaxValue; j++)
				{
					oldFrameBlock.RgbaRaw[pos] = (byte)i;
					newFrameBlock.RgbaRaw[pos++] = (byte)j;
					oldFrameBlock.RgbaRaw[pos] = (byte)i;
					newFrameBlock.RgbaRaw[pos++] = (byte)j;
					oldFrameBlock.RgbaRaw[pos] = (byte)i;
					newFrameBlock.RgbaRaw[pos++] = (byte)j;
					oldFrameBlock.RgbaRaw[pos] = (byte)i;
					newFrameBlock.RgbaRaw[pos++] = (byte)j;
				}
			}

			var original = new byte[newFrameBlock.RgbaRaw.Length];
			Buffer.BlockCopy(newFrameBlock.RgbaRaw, 0, original, 0, original.Length);

			newFrameBlock.SubtractFrom(oldFrameBlock, double.MinValue, double.MaxValue);

			// It would be helpful to do an encode/decode right here, but it turns out that the JPEG encoding process
			// changes the color scheme, etc., enough that there's not enough left to test effectively.
			//newFrameBlock.Encode(50);
			//newFrameBlock.Decode();

			newFrameBlock.AddTo(oldFrameBlock);

			for (int i = 0; i < original.Length; i++)
			{
				if (i % 4 != 3)
				{
					var diff = Math.Abs(newFrameBlock.RgbaRaw[i] - original[i]);
					Assert.IsTrue(diff < 2);
				}
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void FrameBlockJpegDeltaEncodingTest()
		{
			// ks 10/28/11 - This particular method doesn't really test anything, other than that everything runs without exceptions,
			// but it's a helpful little framework to have around when  troubleshooting encoding issues, so I'll leave it here.
			const int offset = 20;
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			var oldFrame = new FrameBlock(ctxFactory) { BlockType = BlockType.Jpeg };
			var newFrame = new FrameBlock(ctxFactory) { BlockType = BlockType.JpegDiff };
			var newFrameWithJpeg = new FrameBlock(ctxFactory) { BlockType = BlockType.Jpeg };
			for (int i = 0; i < oldFrame.RgbaRaw.Length; i++)
			{
				oldFrame.RgbaRaw[i] = (byte)i;
				newFrame.RgbaRaw[i] = (byte)(i + offset);
				newFrameWithJpeg.RgbaRaw[i] = (byte)(i + offset);
			}

			newFrame.SubtractFrom(oldFrame, double.MinValue, double.MaxValue);

			// Make some copies for later comparison
			var oldFrameOriginal = new byte[oldFrame.RgbaRaw.Length];
			Buffer.BlockCopy(oldFrame.RgbaRaw, 0, oldFrameOriginal, 0, oldFrameOriginal.Length);
			var newFrameOriginal = new byte[newFrame.RgbaRaw.Length];
			Buffer.BlockCopy(newFrame.RgbaRaw, 0, newFrameOriginal, 0, newFrameOriginal.Length);
			var newFrameDelta = new byte[newFrame.RgbaDelta.Length];
			Buffer.BlockCopy(newFrame.RgbaDelta, 0, newFrameDelta, 0, newFrameDelta.Length);

			oldFrame.Encode(50);
			oldFrame.Decode();

			newFrameWithJpeg.Encode(50);
			newFrameWithJpeg.Decode();

			newFrame.Encode(50);
			newFrame.Decode();
			newFrame.AddTo(oldFrame);

			int totalOldError = 0;
			int totalNewError = 0;
			int totalNewDeltaError = 0;
			int totalNewJpegError = 0;

			for (int i = 0; i < newFrameOriginal.Length; i++)
			{
				if (i % 4 != 3)
				{
					totalOldError += Math.Abs(oldFrame.RgbaRaw[i] - oldFrameOriginal[i]);
					totalNewError += Math.Abs(newFrame.RgbaRaw[i] - newFrameOriginal[i]);
					totalNewDeltaError += Math.Abs(newFrame.RgbaDelta[i] - newFrameDelta[i]);
					totalNewJpegError += Math.Abs(newFrameWithJpeg.RgbaRaw[i] - newFrameOriginal[i]);

					Debug.WriteLine("OLD: Original: {0}; Encoded: {1}; Error: {2}; NEWJPEG: Original: {3}; Encoded: {4}; Error: {5}",
						oldFrameOriginal[i], oldFrame.RgbaRaw[i], Math.Abs(oldFrameOriginal[i] - oldFrame.RgbaRaw[i]),
						newFrameOriginal[i], newFrameWithJpeg.RgbaRaw[i], Math.Abs(newFrameOriginal[i] - newFrameWithJpeg.RgbaRaw[i]));

					// Assert.IsTrue(diff < 2);
					//if (oldError > 2)
					//{
					//    Debug.WriteLine("JPEG: Original:{0}, Encoded:{1}, Error:{2}", oldFrameOriginal[i], oldFrame.RgbaRaw[i], oldError);
					//}

					// Assert.IsTrue(diff < 2);
					//if (newError > 2)
					//{
					//    Debug.WriteLine("JPEGDIFF: Original:{0}, Encoded:{1}, Error:{2}", newFrameOriginal[i], newFrame.RgbaRaw[i], newError);
					//}
				}
			}
			double samples = oldFrameOriginal.Length * .75;
			double averageOldError = totalOldError / samples;
			double averageNewError = totalNewError / samples;
			double averageNewDeltaError = totalNewDeltaError / samples;
			double averageNewJpegError = totalNewJpegError / samples;

			Debug.WriteLine("old error: {0:0.00}; new error: {1:0.00}; new delta error: {2:0.00}; new with jpeg error: {3:0.00}", averageOldError, averageNewError, averageNewDeltaError, averageNewJpegError);
		}

		[TestMethod]
		[Tag("media")]
		[Tag("codec")]
		public void EncodingOffsetTest()
		{
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			for (int offset = 0; offset < 255; offset++)
			{
				// Setup the frame.
				var frame = new FrameBlock(ctxFactory) { BlockType = BlockType.Jpeg };
				for (int i = 0; i < frame.RgbaRaw.Length; i++)
				{
					frame.RgbaRaw[i] = (byte)(i + offset);
				}
				var original = new byte[frame.RgbaRaw.Length];
				Buffer.BlockCopy(frame.RgbaRaw, 0, original, 0, original.Length);

				// Encode and decode it.
				frame.Encode(50);
				frame.Decode();

				// Calculate and display the error
				int totalError = 0;
				for (int i = 0; i < original.Length; i++)
				{
					if (i % 4 != 3)
					{
						totalError += Math.Abs(frame.RgbaRaw[i] - original[i]);
					}
				}
				double samples = original.Length * .75;
				double averageError = totalError / samples;
				Debug.WriteLine("Offset:{0}; Error:{1:0.00}", offset, averageError);
			}
		}

		/// <summary>
		/// Confirm that the JpegFrameEncoder and Decoder return something like the original image.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("performance")]
		public void zzCodecPerformanceTest()
		{
			// Encode and decode a basic raster structure.
			var perf = new PerformanceMonitor("Encode/Decode", 1);
			var vqc = new VideoQualityController(1);
			vqc.RemoteSessions = _remoteSessions;
			var codec = new JpegDiffVideoCodec(vqc);
			codec.Initialize(height, width, VideoConstants.MaxPayloadSize);
			var videoChunkPool = new ObjectPool<ByteStream>(() => new ByteStream(VideoConstants.MaxPayloadSize), bs => bs.Reset());

			perf.Start();
			const int iterations = 100;
			for (int i = 0; i < iterations; i++)
			{
				byte[] sample = GetRgba(i);
				codec.EncodeFrame(sample, 0);
				bool moreChunks = true;
				var buffer = videoChunkPool.GetNext();
				while (moreChunks)
				{
					if (codec.GetNextChunk(buffer, out moreChunks))
					{
						codec.DecodeChunk(buffer, 2);
					}
				}
				videoChunkPool.Recycle(buffer);
				codec.GetNextFrame();
			}
			perf.Stop();

			ClientLogger.Debug("Finished JpegEncoderDecoder performance test.");
		}

		#endregion

		#region Jpeg Library Tests

		/// <summary>
		/// Tests whether the YCbCr.ToRGB() and YCbCr.ToRGBFast() methods return the same values.
		/// </summary>
		[TestMethod]
		[Tag("jpeg")]
		[Tag("media")]
		public void YCbCrToRGBTest()
		{
			const int maxVariance = 1;
			byte[][][] raster = GetRaster();
			for (short y = 0; y < height; y++)
			{
				for (short x = 0; x < width; x++)
				{
					// Convert values via slower method.
					byte r1 = raster[yIndex][x][y];
					byte g1 = raster[cbIndex][x][y];
					byte b1 = raster[crIndex][x][y];
					YCbCr.toRGBSlow(ref r1, ref g1, ref b1);

					// Convert values via experimental faster method.
					byte r2 = raster[yIndex][x][y];
					byte g2 = raster[cbIndex][x][y];
					byte b2 = raster[crIndex][x][y];
					YCbCr.toRGB(ref r2, ref g2, ref b2);

					Assert.IsTrue(Math.Abs(r1 - r2) <= maxVariance);
					Assert.IsTrue(Math.Abs(r1 - r2) <= maxVariance);
					Assert.IsTrue(Math.Abs(r1 - r2) <= maxVariance);
				}
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("jpeg")]
		[Tag("performance")]
		public void YCbCrToRGBPerformanceTest()
		{
			byte[][][] raster = GetRaster();
			const int runs = 20;
			const int iterations = 100;
			var slowPerf = new PerformanceMonitor("TheoreticallySlower", runs);
			var fastPerf = new PerformanceMonitor("TheoreticallyFaster", runs);

			for (int run = 0; run < runs; run++)
			{
				slowPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					for (short y = 0; y < height; y++)
					{
						for (short x = 0; x < width; x++)
						{
							byte r1 = raster[yIndex][x][y];
							byte g1 = raster[cbIndex][x][y];
							byte b1 = raster[crIndex][x][y];
							YCbCr.toRGBSlow(ref r1, ref g1, ref b1);
						}
					}
				}
				slowPerf.Stop();

				fastPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					for (short y = 0; y < height; y++)
					{
						for (short x = 0; x < width; x++)
						{
							byte r1 = raster[yIndex][x][y];
							byte g1 = raster[cbIndex][x][y];
							byte b1 = raster[crIndex][x][y];
							YCbCr.toRGB(ref r1, ref g1, ref b1);
						}
					}
				}
				fastPerf.Stop();
			}
			Assert.IsTrue(fastPerf.AverageCompletionTimeInMs < slowPerf.AverageCompletionTimeInMs,
				"The fast conversion method took {0:0.00} ms, while the slow method took {0:0.00} ms", fastPerf.AverageCompletionTimeInMs, slowPerf.AverageCompletionTimeInMs);
		}

		[TestMethod]
		[Tag("jpeg")]
		[Tag("media")]
		public void RGBToYCbCrTest()
		{
			const int maxVariance = 1;
			byte[] rgba = GetRgba(1);
			for (int i = 0; i < rgba.Length; i++)
			{
				byte b2 = rgba[i];
				byte b1 = rgba[i++];
				byte g2 = rgba[i];
				byte g1 = rgba[i++];
				byte r2 = rgba[i];
				byte r1 = rgba[i++];
				YCbCr.fromRGBSlow(ref r1, ref g1, ref b1);
				YCbCr.fromRGB(ref r2, ref g2, ref b2);
				Assert.IsTrue(Math.Abs(r1 - r2) <= maxVariance);
				Assert.IsTrue(Math.Abs(g1 - g2) <= maxVariance);
				Assert.IsTrue(Math.Abs(b1 - b2) <= maxVariance);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("jpeg")]
		[Tag("performance")]
		public void RGBToYCbCrPerformanceTest()
		{
			byte[] rgba = GetRgba(1);
			const int runs = 20;
			const int iterations = 100;
			var slowPerf = new PerformanceMonitor("TheoreticallySlower", runs);
			var fastPerf = new PerformanceMonitor("TheoreticallyFaster", runs);

			for (int run = 0; run < runs; run++)
			{
				slowPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					for (int p = 0; p < rgba.Length; p++)
					{
						byte b = rgba[p++];
						byte g = rgba[p++];
						byte r = rgba[p++];
						YCbCr.fromRGBSlow(ref r, ref g, ref b);
					}
				}
				slowPerf.Stop();

				fastPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					for (int p = 0; p < rgba.Length; p++)
					{
						byte b = rgba[p++];
						byte g = rgba[p++];
						byte r = rgba[p++];
						YCbCr.fromRGB(ref r, ref g, ref b);
					}
				}
				fastPerf.Stop();
			}
#if !DEBUG
			// This is only true when running with the optimized JIT.
			Assert.IsTrue(fastPerf.AverageCompletionTimeInMs < slowPerf.AverageCompletionTimeInMs,
				"The fast conversion method took {0:0.00} ms, while the slow took {1:0.00}", fastPerf.AverageCompletionTimeInMs, slowPerf.AverageCompletionTimeInMs);
#endif
		}

		[TestMethod]
		[Tag("media")]
		[Tag("jpeg")]
		public void IDCTComparisonTest()
		{
			const int maxVariance = 1;
			var source = new float[]
			{
				560, -196, -150, -80, 0, 0, 0, 0,
				-210, -60, -35, 0, 0, 0, 0, 0,
				-140, -32, -40, 0, 0, 0, 0, 0,
				-70, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0
			};

			var dct = new DCT(20);

			var naive = new byte[8][];
			var nvidia = new byte[8][];
			for (int i = 0; i < 8; i++)
			{
				naive[i] = new byte[8];
				nvidia[i] = new byte[8];
			}

			dct.DoIDCT_Naive(source, naive);
			dct.DoIDCT_NVidia(source, nvidia);

			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					int diff = Math.Abs(naive[i][j] - nvidia[i][j]);
					// Debug.WriteLine("diff={0}", diff);
					Assert.IsTrue(diff <= maxVariance);
				}
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("jpeg")]
		[Tag("performance")]
		public void IDCTPerformanceTest()
		{
			var source = new float[]
			{
				560, -196, -150, -80, 0, 0, 0, 0,
				-210, -60, -35, 0, 0, 0, 0, 0,
				-140, -32, -40, 0, 0, 0, 0, 0,
				-70, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0
			};

			const int iterations = 100000;
			const int runs = 5;
			var naivePerf = new PerformanceMonitor("Naive", runs);
			var nvidiaPerf = new PerformanceMonitor("NVidia", runs);
			var dct = new DCT(20);

			var naive = new byte[8][];
			var nvidia = new byte[8][];
			for (int i = 0; i < 8; i++)
			{
				naive[i] = new byte[8];
				nvidia[i] = new byte[8];
			}

			for (int run = 0; run < runs; run++)
			{
				naivePerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					dct.DoIDCT_Naive(source, naive);
				}
				naivePerf.Stop();

				nvidiaPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					dct.DoIDCT_NVidia(source, nvidia);
				}
				nvidiaPerf.Stop();
			}

			Assert.IsTrue(nvidiaPerf.AverageCompletionTimeInMs < naivePerf.AverageCompletionTimeInMs,
				"The NVidia IDCT took {0:0.00} ms, while the naive IDCT took {1:0.00}", nvidiaPerf.AverageCompletionTimeInMs, naivePerf.AverageCompletionTimeInMs);
		}

		/// <summary>
		/// ks 12/31/10 - I never did get the NVidia DCT implementation working correctly, and we're not using it, 
		/// because it's basically offers same performance as the AAN implementation.  So let's ignore this test for now.
		/// </summary>
		[TestMethod]
		[Tag("media")]
		[Tag("jpeg")]
		[Ignore]
		public void DCTComparisonTest()
		{
			const int maxVariance = 2;
			var source = new float[8][];
			var destAan = new float[8][];
			var destNvidia = new float[8][];
			for (int i = 0; i < 8; i++)
			{
				source[i] = new float[] { 100, 110, 120, 130, 140, 150, 160, 170 };
				destAan[i] = new float[8];
				destNvidia[i] = new float[8];
			}

			var dct = new DCT(20);
			dct.DoDCT_AAN(source, destAan);
			dct.DoDCT_NVidia(source, destNvidia);

			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					float diff = Math.Abs(destAan[i][j] - destNvidia[i][j]);
					float ratio = destAan[i][j] / destNvidia[i][j];
					Debug.WriteLine("aan={0}, nvidia={1}, diff={2}, ratio={3}", destAan[i][j], destNvidia[i][j], diff, ratio);
					Assert.IsTrue(diff <= maxVariance);
				}
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("jpeg")]
		[Tag("performance")]
		public void DCTPerformanceTest()
		{
			const int runs = 5;
			const int iterations = 100000;
			var aanPerf = new PerformanceMonitor("AAN", runs);
			var nvidiaPerf = new PerformanceMonitor("NVidia", runs);

			var source = new float[8][];
			var destAan = new float[8][];
			var destNvidia = new float[8][];
			for (int i = 0; i < 8; i++)
			{
				source[i] = new float[] { 100, 110, 120, 130, 140, 150, 160, 170 };
				destAan[i] = new float[8];
				destNvidia[i] = new float[8];
			}

			var dct = new DCT(20);

			for (int run = 0; run < runs; run++)
			{
				aanPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					dct.DoDCT_AAN(source, destAan);
				}
				aanPerf.Stop();

				nvidiaPerf.Start();
				for (int i = 0; i < iterations; i++)
				{
					dct.DoDCT_NVidia(source, destNvidia);
				}
				nvidiaPerf.Stop();
			}

			// We don't really care at this point -- they're very close.
			// Assert.IsTrue(nvidiaPerf.AverageCompletionTimeInMs > aanPerf.AverageCompletionTimeInMs);
		}

		#endregion

		#region Support Methods

		private static byte[] GetSample()
		{
			// Fill each block in the sample with the appropriate block number.
			var sample = new byte[height * width * VideoConstants.BytesPerPixel];
			const int lineLength = width * VideoConstants.BytesPerPixel;
			int horizontalBlocks = width / blockSize;
			int verticalBlocks = height / blockSize;
			for (int blockY = 0; blockY < verticalBlocks; blockY++)
			{
				for (int blockX = 0; blockX < horizontalBlocks; blockX++)
				{
					int currentBlock = (blockY * horizontalBlocks) + blockX;
					byte[] lineData = GetLineData((byte)currentBlock);
					int startingOffset = (blockX * blockSize * VideoConstants.BytesPerPixel) + (blockY * horizontalBlocks * totalBlockSize);
					for (int line = 0; line < blockSize; line++)
					{
						int sampleStartPos = startingOffset + (line * lineLength);
						Buffer.BlockCopy(lineData, 0, sample, sampleStartPos, blockLineLength);
					}
				}
			}
			return sample;
		}

		private static byte[] GetLineData(byte b)
		{
			var lineData = new byte[blockLineLength];
			for (int i = 0; i < lineData.Length; i++)
			{
				lineData[i] = b;
			}
			return lineData;
		}

		private static Queue<FrameBlock> GetFrameBlockQueue()
		{
			var queue = new Queue<FrameBlock>();
			var ctxFactory = new JpegFrameContextFactory(VideoConstants.VideoBlockSize, VideoConstants.VideoBlockSize);
			for (byte i = 0; i < 100; i++)
			{
				var block = new FrameBlock(ctxFactory);
				block.BlockX = i;
				block.BlockY = i;
				// block.FrameNumber = i;
				block.JpegQuality = 50;
				var payload = new byte[i + 1];
				block.EncodedStream = new MemoryStream(payload);
				queue.Enqueue(block);
			}
			return queue;
		}

		private static byte[][][] GetRaster()
		{
			byte[][][] raster = Image.CreateRasterBuffer(width, height, 3);
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < width; j++)
				{
					raster[i][j] = new byte[height];
					for (int k = 0; k < height; k++)
					{
						raster[i][j][k] = 100;
					}
				}
			}
			return raster;
		}

		private byte[] GetRgba(int pass)
		{
			int frameNumber = pass % _rgbaSamples.Length;
			if (_rgbaSamples[frameNumber] == null)
			{
				var rgbaSample = new byte[height * width * VideoConstants.BytesPerPixel];
				for (int i = 0; i < rgbaSample.Length; i++)
				{
					rgbaSample[i] = (byte)(frameNumber * (_videoQualityController.NoSendCutoff + 1));
				}
				_rgbaSamples[frameNumber] = rgbaSample;
				return rgbaSample;
			}
			return _rgbaSamples[frameNumber];
		}

		private static short[] GetAudioSamples(int length, short value)
		{
			var samples = new short[length];
			for (int j = 0; j < samples.Length; j++)
			{
				samples[j] = value;
			}
			return samples;
		}

		#endregion
	}
}