using System;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;
using Alanta.Client.Media.AudioCodecs;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class AudioCodecTests : SilverlightTest
	{
		private readonly AudioFormat audioFormat = new AudioFormat();

		[TestMethod]
		[Tag("media")]
		[Tag("audiocodec")]
		public void G711Test()
		{
			var input = new short[audioFormat.SamplesPerFrame];
			var encoded = new short[audioFormat.SamplesPerFrame / 4];
			var output = new short[audioFormat.SamplesPerFrame];
			var encoder = new G711MuLawEncoder(audioFormat);
			var decoder = new G711MuLawDecoder(audioFormat);

			// Get some raw data for input.
			for (short i = 0; i < input.Length; i++)
			{
				input[i] = i;
			}

			int length = encoder.Encode(input, 0, input.Length, encoded, false);
			Assert.AreEqual(encoded.Length, length);

			length = decoder.Decode(encoded, 0, encoded.Length, output, 0, false);
			Assert.AreEqual(output.Length, length);

			for (short i = 0; i < output.Length; i++)
			{
				int absoluteDiff = Math.Abs(input[i] - output[i]);
				float percentDiff = Math.Abs((absoluteDiff) / (float)input[i]) * 100;
				Assert.IsTrue(absoluteDiff < 10 || percentDiff < 10);
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("audiocodec")]
		public void G711ComparisonTest()
		{
			for (int i = short.MinValue; i <= short.MaxValue; i++)
			{
				Assert.AreEqual(G711.LinearToULaw((byte)i), G711.LinearToULawFast((byte)i));
			}

			for (int i = byte.MinValue; i <= byte.MaxValue; i++)
			{
				Assert.AreEqual(G711.ULawToLinear((byte)i), G711.ULawToLinearFast((byte)i));
			}
		}

		[TestMethod]
		[Tag("media")]
		[Tag("audiocodec")]
		[Tag("performance")]
		public void G711LinearToULawPerformanceComparison()
		{
			const int innerIterations = 10000;
			var input = new short[audioFormat.SamplesPerFrame];
			var rnd = new Random();
			for (int i = 0; i < input.Length; i++)
			{
				input[i] = (short)rnd.Next(short.MinValue, short.MaxValue);
			}

			var calculationElapsed = new TimeSpan();
			var lookupElapsed = new TimeSpan();
			for (int i = 0; i < 10; i++)
			{
				var start = DateTime.Now;
				for (int j = 0; j < innerIterations; j++)
				{
					foreach (short k in input)
					{
						var encoded = G711.LinearToULaw(k);
					}
				}
				calculationElapsed += DateTime.Now - start;

				start = DateTime.Now;
				for (int j = 0; j < innerIterations; j++)
				{
					foreach (short k in input)
					{
						var encoded = G711.LinearToULawFast(k);
					}
				}
				lookupElapsed += DateTime.Now - start;
			}
			ClientLogger.Debug("CalculationElapsed: {0}; LookupElapsed: {1}", calculationElapsed.TotalMilliseconds, lookupElapsed.TotalMilliseconds);
		}

		[TestMethod]
		[Tag("media")]
		[Tag("audiocodec")]
		[Tag("performance")]
		public void G711ULawToLinearPerformanceComparison()
		{
			const int innerIterations = 10000;
			var input = new byte[audioFormat.SamplesPerFrame];
			var rnd = new Random();
			for (int i = 0; i < input.Length; i++)
			{
				input[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
			}

			var calculationElapsed = new TimeSpan();
			var lookupElapsed = new TimeSpan();
			for (int i = 0; i < 10; i++)
			{
				var start = DateTime.Now;
				for (int j = 0; j < innerIterations; j++)
				{
					foreach (byte k in input)
					{
						var encoded = G711.ULawToLinear(k);
					}
				}
				calculationElapsed += DateTime.Now - start;

				start = DateTime.Now;
				for (int j = 0; j < innerIterations; j++)
				{
					foreach (byte k in input)
					{
						var encoded = G711.ULawToLinearFast(k);
					}
				}
				lookupElapsed += DateTime.Now - start;
			}
			ClientLogger.Debug("CalculationElapsed: {0}; LookupElapsed: {1}", calculationElapsed.TotalMilliseconds, lookupElapsed.TotalMilliseconds);
		}


		//[TestMethod]
		//[Tag("media")]
		//[Tag("audiocodec")]
		//public void CodecFactory_GetAudioEncoder_OneSession()
		//{
		//    var env = new TestMediaEnvironment();
		//    var codecFactory = new CodecFactory(env);
		//    var codec = codecFactory.GetAudioEncoder(1);
		//    Assert.AreEqual(typeof(SpeexAudioEncoder), codec.GetType());
		//    codec = codecFactory.GetAudioEncoder(2);
		//    Assert.AreEqual(typeof(G711MuLawEncoder), codec.GetType());
		//}

		//[TestMethod]
		//[Tag("media")]
		//[Tag("audiocodec")]
		//public void CodecFactory_GetAudioEncoder_MultipleSessions()
		//{
		//    var env = new TestMediaEnvironment();
		//    var codecFactory = new CodecFactory(env);
		//    var codec = codecFactory.GetAudioEncoder(2);
		//    Assert.AreEqual(typeof(G711MuLawEncoder), codec.GetType());
		//}

		//[TestMethod]
		//[Tag("media")]
		//[Tag("audiocodec")]
		//public void CodecFactory_GetAudioEncoder_MultipleSessions_LocalCpu()
		//{
		//    var env = new TestMediaEnvironment();
		//    var scratchAdapter = new EnvironmentAdapter<object>(env, null, null);
		//    var codecFactory = new CodecFactory(env);
		//    env.Now = DateTime.Now;

		//    // It should start out defaulting to Speex.
		//    var codec = codecFactory.GetAudioEncoder(1);
		//    Assert.AreEqual(typeof(SpeexAudioEncoder), codec.GetType());



		//    for (int i = 0; i < 10; i++)
		//    {
		//        // Tell the environment that enough time has passed that we should be able to see if we should downgrade the codec.
		//        env.Now += (scratchAdapter.MinimumTimeUntilDowngrade + TimeSpan.FromSeconds(1));
		//        env.LocalProcessorLoad = scratchAdapter.MaxRecommendedLoad + 1;
		//        codec = codecFactory.GetAudioEncoder(1);
		//        Assert.AreEqual(typeof(G711MuLawEncoder), codec.GetType());

		//        // Tell the environment that enough time has passed that we should be able to see if we should upgrade the codec.
		//        env.Now += (scratchAdapter.MinimumTimeUntilUpgrade + TimeSpan.FromSeconds(1));
		//        env.LocalProcessorLoad = scratchAdapter.MaxSafeLoad - 1;
		//        codec = codecFactory.GetAudioEncoder(1);
		//        Assert.AreEqual(typeof(SpeexAudioEncoder), codec.GetType());
		//    }
		//}

		//[TestMethod]
		//[Tag("media")]
		//[Tag("audiocodec")]
		//public void CodecFactory_GetAudioEncoder_MultipleSessions_RemoteCpu()
		//{
		//    var env = new TestMediaEnvironment();
		//    var codecFactory = new CodecFactory(env);
		//    env.Now = DateTime.Now;
		//    var scratchAdapter = new EnvironmentAdapter<object>(env, null, null);

		//    // It should start out defaulting to Speex.
		//    var codec = codecFactory.GetAudioEncoder(1);
		//    Assert.AreEqual(typeof(SpeexAudioEncoder), codec.GetType());

		//    for (int i = 0; i < 10; i++)
		//    {
		//        // Tell the environment that enough time has passed that we should be able to see if we should downgrade the codec.
		//        env.Now += (scratchAdapter.MinimumTimeUntilDowngrade + TimeSpan.FromSeconds(1));
		//        env.RemoteProcessorLoad = scratchAdapter.MaxRecommendedLoad + 1;
		//        codec = codecFactory.GetAudioEncoder(1);
		//        Assert.AreEqual(typeof(G711MuLawEncoder), codec.GetType());

		//        // Tell the environment that enough time has passed that we should be able to see if we should upgrade the codec.
		//        env.Now += (scratchAdapter.MinimumTimeUntilUpgrade + TimeSpan.FromSeconds(1));
		//        env.RemoteProcessorLoad = scratchAdapter.MaxSafeLoad - 1;
		//        codec = codecFactory.GetAudioEncoder(1);
		//        Assert.AreEqual(typeof(SpeexAudioEncoder), codec.GetType());
		//    }
		//}
	}


	public class TestMediaEnvironment : IMediaEnvironment
	{
		public DateTime Now { get; set; }
		public double LocalProcessorLoad { get; set; }
		public double RemoteProcessorLoad { get; set; }
		public int RemoteSessions { get; set; }
		public bool IsMediaRecommended { get; set; }
	}

}
