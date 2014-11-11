using System;
using Alanta.Client.Media;
using Alanta.Client.Media.Dsp;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class ResampleFilterTests : SilverlightTest
	{
		#region ResampleFilter Tests
		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_NoChanges()
		{
			var resampler = new ResampleFilter(AudioFormat.Default, AudioFormat.Default);
			var inboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			var outboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			for (int i = 0; i < inboundFrame.Length; i++)
			{
				inboundFrame[i] = (byte)i;
			}
			resampler.Write(inboundFrame);
			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);
			for (int i = 0; i < AudioFormat.Default.BytesPerFrame; i++)
			{
				Assert.AreEqual(inboundFrame[i], outboundFrame[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_TwoChannelsToOne()
		{
			var inputFormat = new AudioFormat(channels: 2);
			var outputFormat = new AudioFormat();
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrame = new short[inputFormat.SamplesPerFrame];
			var outboundFrame = new short[outputFormat.SamplesPerFrame];

			// Give both channels the same values.
			int index = 0;
			for (short i = 0; i < outputFormat.SamplesPerFrame; i++)
			{
				inboundFrame[index++] = i;
				inboundFrame[index++] = i;
			}
			var temp = new byte[Buffer.ByteLength(inboundFrame)];
			Buffer.BlockCopy(inboundFrame, 0, temp, 0, temp.Length);
			resampler.Write(temp);

			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);

			// We should get just one channel's values back.
			for (int i = 0; i < outputFormat.SamplesPerFrame; i++)
			{
				Assert.AreEqual(i, outboundFrame[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_OneChannelToTwo()
		{
			var inputFormat = new AudioFormat();
			var outputFormat = new AudioFormat(channels: 2);
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrame = new short[inputFormat.SamplesPerFrame];
			var outboundFrame = new short[outputFormat.SamplesPerFrame];

			// Populate one channel
			for (short i = 0; i < inputFormat.SamplesPerFrame; i++)
			{
				inboundFrame[i] = i;
			}
			var temp = new byte[Buffer.ByteLength(inboundFrame)];
			Buffer.BlockCopy(inboundFrame, 0, temp, 0, temp.Length);
			resampler.Write(temp);

			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);

			// We should have two channels worth of data back. Because we interpolate and smooth, it isn't a simple 
			// doubling of the original data, but some patterns can still be tested.
			for (int i = 0; i < (inputFormat.SamplesPerFrame * 2) - 4; i++)
			{
				Assert.IsTrue(outboundFrame[i] <= outboundFrame[i + 4]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_16KhzTo8Khz()
		{
			var inputFormat = new AudioFormat();
			var outputFormat = new AudioFormat(8000);
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrame = new short[inputFormat.SamplesPerFrame];
			var outboundFrame = new short[outputFormat.SamplesPerFrame];

			for (short i = 0; i < inputFormat.SamplesPerFrame; i++)
			{
				inboundFrame[i] = i;
			}
			var temp = new byte[Buffer.ByteLength(inboundFrame)];
			Buffer.BlockCopy(inboundFrame, 0, temp, 0, temp.Length);
			resampler.Write(temp);

			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);

			for (int i = 0; i < outputFormat.SamplesPerFrame; i++)
			{
				Assert.AreEqual(i * 2, outboundFrame[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_32KhzTo16Khz()
		{
			var inputFormat = new AudioFormat(32000);
			var outputFormat = new AudioFormat();
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrame = new short[inputFormat.SamplesPerFrame];
			var outboundFrame = new short[outputFormat.SamplesPerFrame];

			for (short i = 0; i < inputFormat.SamplesPerFrame; i++)
			{
				inboundFrame[i] = i;
			}
			var temp = new byte[Buffer.ByteLength(inboundFrame)];
			Buffer.BlockCopy(inboundFrame, 0, temp, 0, temp.Length);
			resampler.Write(temp);

			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);

			for (int i = 0; i < outputFormat.SamplesPerFrame; i++)
			{
				Assert.AreEqual(i * 2, outboundFrame[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_32KhzTo8Khz()
		{
			var inputFormat = new AudioFormat(32000);
			var outputFormat = new AudioFormat(8000);
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrame = new short[inputFormat.SamplesPerFrame];
			var outboundFrame = new short[outputFormat.SamplesPerFrame];

			for (short i = 0; i < inputFormat.SamplesPerFrame; i++)
			{
				inboundFrame[i] = i;
			}
			var temp = new byte[Buffer.ByteLength(inboundFrame)];
			Buffer.BlockCopy(inboundFrame, 0, temp, 0, temp.Length);
			resampler.Write(temp);

			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);

			for (int i = 0; i < outputFormat.SamplesPerFrame; i++)
			{
				Assert.AreEqual(i * 4, outboundFrame[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_96KhzTo16Khz_And_TwoChannelsToOne()
		{
			var inputFormat = new AudioFormat(96000, channels: 2);
			var outputFormat = new AudioFormat();
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrame = new short[inputFormat.SamplesPerFrame];
			var outboundFrame = new short[outputFormat.SamplesPerFrame];

			int index = 0;
			for (short i = 0; i < inputFormat.SamplesPerFrame / inputFormat.Channels; i++)
			{
				inboundFrame[index++] = i;
				inboundFrame[index++] = i;
			}
			var temp = new byte[Buffer.ByteLength(inboundFrame)];
			Buffer.BlockCopy(inboundFrame, 0, temp, 0, temp.Length);
			resampler.Write(temp);

			bool moreFrames;
			bool successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);

			for (int i = 0; i < outputFormat.SamplesPerFrame; i++)
			{
				Assert.AreEqual(i * 6, outboundFrame[i]);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Base_Read_44KhzTo16Khz_And_2048To640()
		{
			// This tests the scenario we run into on some Macs, where the amount of data submitted is always on 2048 byte boundaries.

			// 44100 / 16000 = 2.75625
			const int writeFrameSize = 1024; // in shorts
			const int readFrameSize = 320; // in shorts
			const int inboundFrameSize = (int)(readFrameSize * 2.75625); //  = 882
			const int frameCount = 100;
			var inputFormat = new AudioFormat(44100);
			var outputFormat = new AudioFormat();
			var resampler = new ResampleFilter(inputFormat, outputFormat);
			var inboundFrames = new short[inboundFrameSize * frameCount];
			var outboundFrame = new byte[readFrameSize * sizeof(short)];

			// Fill the inbound buffer.
			for (short frame = 0; frame < frameCount; frame++)
			{
				for (int i = 0; i < inboundFrameSize; i++)
				{
					inboundFrames[frame * inboundFrameSize + i] = frame;
				}
			}

			int index = 0;
			short readFrame = 0;
			while (index + writeFrameSize < inboundFrames.Length)
			{
				// Write data to the frame in 1024 sample/2048 byte chunks (we'll be reading from it in 320 sample chunks).
				var tmpWrite = new byte[writeFrameSize * sizeof(short)];
				Buffer.BlockCopy(inboundFrames, index * sizeof(short), tmpWrite, 0, writeFrameSize * sizeof(short));
				resampler.Write(tmpWrite);
				index += writeFrameSize;

				bool moreFrames;
				do
				{
					if (resampler.Read(outboundFrame, out moreFrames))
					{
						// Copy the byte array to a short array so we can check the values.
						var tmpRead = new short[readFrameSize];
						Buffer.BlockCopy(outboundFrame, 0, tmpRead, 0, readFrameSize * sizeof(short));

						// There's some expected leakage around the ends of the buffers,
						// so ignore the values there.
						for (int i = 0; i < tmpRead.Length - (readFrame / 2); i++)
						{
							Assert.AreEqual(readFrame, tmpRead[i]);
						}
						readFrame++;
					}
				} while (moreFrames);
			}
		}


		#endregion

		#region ResampleFilterTimer Tests

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Timing_EverythingOnTime()
		{
			var resampler = new ResampleFilterTiming(AudioFormat.Default, AudioFormat.Default);
			var inboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			var outboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			for (int i = 0; i < inboundFrame.Length; i++)
			{
				inboundFrame[i] = (byte)i;
			}
			resampler.Now = DateTime.Now;
			for (int i = 0; i < ResampleFilterTiming.InitialFramesToIgnore + ResampleFilterTiming.MaxFramesBetweenResets * 2; i++)
			{
				resampler.Write(inboundFrame);
				bool moreFrames;
				bool successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsFalse(moreFrames);
				for (int j = 0; j < AudioFormat.Default.BytesPerFrame; j++)
				{
					Assert.AreEqual(inboundFrame[j], outboundFrame[j]);
				}
				Assert.AreEqual(1.0f, resampler.CorrectionFactor);
				Assert.AreEqual(0, resampler.UnreadBytes);
				resampler.Now += TimeSpan.FromMilliseconds(resampler.OutputMillisecondsPerFrame);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Timing_TooSlow()
		{
			var resampler = new ResampleFilterTiming(AudioFormat.Default, AudioFormat.Default);
			var inboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			var outboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			for (int i = 0; i < inboundFrame.Length; i++)
			{
				inboundFrame[i] = (byte)i;
			}
			resampler.Now = DateTime.Now;

			bool successful;
			bool moreFrames = false;

			// Read normally until we get through the warmup period.
			for (int i = 0; i < ResampleFilterTiming.InitialFramesToIgnore; i++)
			{
				resampler.Write(inboundFrame);
				successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsFalse(moreFrames);
				Assert.IsTrue(resampler.UnreadBytes == 0);
				resampler.Now += TimeSpan.FromMilliseconds(resampler.OutputMillisecondsPerFrame);
			}

			// Read/write every 21 milliseconds
			for (int i = 0; i < ResampleFilterTiming.MaxFramesBetweenResets; i++)
			{
				resampler.Write(inboundFrame);
				successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsFalse(moreFrames);
				resampler.Now += TimeSpan.FromMilliseconds(21);
			}

			// Confirm that the correction factor has been set correctly.
			Assert.AreEqual(21.0 / resampler.OutputMillisecondsPerFrame, resampler.CorrectionFactor);

			// Read another 19 frames
			for (int i = 0; i < 19; i++)
			{
				resampler.Write(inboundFrame);
				successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsTrue(resampler.UnreadBytes > 0);
				resampler.Now += TimeSpan.FromMilliseconds(21);
			}

			// After 19 reads, there should be one more frame waiting to be read.
			Assert.IsTrue(moreFrames);
			successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);
			Assert.IsTrue(resampler.UnreadBytes == 0);

		}

		[TestMethod]
		[Tag("media")]
		[Tag("resamplefilter")]
		public void Timing_TooFast()
		{
			var resampler = new ResampleFilterTiming(AudioFormat.Default, AudioFormat.Default);
			var inboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			var outboundFrame = new byte[AudioFormat.Default.BytesPerFrame];
			for (int i = 0; i < inboundFrame.Length; i++)
			{
				inboundFrame[i] = (byte)i;
			}
			resampler.Now = DateTime.Now;

			bool successful;
			bool moreFrames;

			// Read normally until we get through the warmup period.
			for (int i = 0; i < ResampleFilterTiming.InitialFramesToIgnore; i++)
			{
				resampler.Write(inboundFrame);
				successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsFalse(moreFrames);
				Assert.IsTrue(resampler.UnreadBytes == 0);
				resampler.Now += TimeSpan.FromMilliseconds(resampler.OutputMillisecondsPerFrame);
			}

			// Read/write every 19 milliseconds
			for (int i = 0; i < ResampleFilterTiming.MaxFramesBetweenResets - 1; i++)
			{
				resampler.Write(inboundFrame);
				successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsFalse(moreFrames);
				resampler.Now += TimeSpan.FromMilliseconds(19);
			}

			// Write and try to read another frame. It should have been downsampled, and hence shouldn't be ready for reading yet.
			resampler.Write(inboundFrame);
			Assert.AreEqual((int)(19.0 / resampler.OutputMillisecondsPerFrame) * 1000, (int)resampler.CorrectionFactor * 1000);
			successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsFalse(successful);
			Assert.IsFalse(moreFrames);
			resampler.Now += TimeSpan.FromMilliseconds(19);

			// The next 18 read/writes should succeed, but leave data.
			for (int i = 0; i < 18; i++)
			{
				resampler.Write(inboundFrame);
				successful = resampler.Read(outboundFrame, out moreFrames);
				Assert.IsTrue(successful);
				Assert.IsTrue(resampler.UnreadBytes > 0);
				Assert.IsFalse(moreFrames);
				resampler.Now += TimeSpan.FromMilliseconds(19);
			}

			// The 19th read/write should drain the buffer.
			resampler.Write(inboundFrame);
			successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsTrue(successful);
			Assert.IsFalse(moreFrames);
			Assert.IsTrue(resampler.UnreadBytes == 0);

			// The 20th read/write should fail.
			resampler.Write(inboundFrame);
			Assert.AreEqual((int)(19.0 / resampler.OutputMillisecondsPerFrame) * 1000, (int)resampler.CorrectionFactor * 1000);
			successful = resampler.Read(outboundFrame, out moreFrames);
			Assert.IsFalse(successful);
			Assert.IsFalse(moreFrames);

		}
		#endregion

	}
}
