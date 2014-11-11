using System;
using System.Collections.Generic;
using Alanta.Client.Media;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{

	[TestClass]
	public class VideoQualityControllerTests 
	{

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }

		#region Test Methods

		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void ConstructorTest()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(ushort.MaxValue, remoteSessions);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.NotSpecified, controller.CommandedVideoQuality);
		}

		/// <summary>
		/// If we have the lowest ssrcId, we should set the CommandedVideoQuality.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void IsControllerTest()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(1, remoteSessions);
			controller.LogReceivedVideoQuality(2, VideoQuality.NotSpecified, VideoQuality.High);
			Assert.AreEqual(VideoQuality.High, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.High, controller.CommandedVideoQuality);
		}

		/// <summary>
		/// If we don't have the lowest ssrcId, we shouldn't set the CommandedVideoQuality.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void IsNotControllerTest()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(2, remoteSessions);
			controller.LogReceivedVideoQuality(1, VideoQuality.NotSpecified, VideoQuality.High);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.NotSpecified, controller.CommandedVideoQuality);
		}

		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void VideoQualityPropertyTest()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(ushort.MaxValue, remoteSessions);

			// Fallback
			controller.LocalVideoQuality = VideoQuality.Fallback;
			Assert.AreEqual(VideoQuality.Fallback, controller.LocalVideoQuality);
			Assert.AreEqual(1, controller.AcceptFramesPerSecond);
			Assert.AreEqual(1, controller.InterleaveFactor);
			Assert.AreEqual(8, controller.FullFrameInterval);

			// Low
			controller.LocalVideoQuality = VideoQuality.Low;
			Assert.AreEqual(VideoQuality.Low, controller.LocalVideoQuality);
			Assert.AreEqual(1, controller.AcceptFramesPerSecond);
			Assert.AreEqual(1, controller.InterleaveFactor);
			Assert.AreEqual(4, controller.FullFrameInterval);

			// Medium
			controller.LocalVideoQuality = VideoQuality.Medium;
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			Assert.AreEqual(5, controller.AcceptFramesPerSecond);
			Assert.AreEqual(1, controller.InterleaveFactor);
			Assert.AreEqual(20, controller.FullFrameInterval);

			// High
			controller.LocalVideoQuality = VideoQuality.High;
			Assert.AreEqual(VideoQuality.High, controller.LocalVideoQuality);
			Assert.AreEqual(10, controller.AcceptFramesPerSecond);
			Assert.AreEqual(1, controller.InterleaveFactor);
			Assert.AreEqual(20, controller.FullFrameInterval);
		}

		/// <summary>
		/// We should ignore commands from machines with a higher ssrcId.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogReceivedVideoQualityTest_CommandedVideoQuality_Controller()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(1, remoteSessions);
			controller.LogReceivedVideoQuality(2, VideoQuality.Fallback, VideoQuality.Medium);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			controller.LogReceivedVideoQuality(2, VideoQuality.High, VideoQuality.Medium);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			controller.LogReceivedVideoQuality(2, VideoQuality.NotSpecified, VideoQuality.Medium);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
		}

		/// <summary>
		/// We should obey commands from machines with a lower ssrcId.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogReceivedVideoQualityTest_CommandedVideoQuality_NotController()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(2, remoteSessions);
			controller.LogReceivedVideoQuality(1, VideoQuality.Fallback, VideoQuality.High);
			Assert.AreEqual(VideoQuality.Fallback, controller.LocalVideoQuality);
			controller.LogReceivedVideoQuality(1, VideoQuality.High, VideoQuality.High);
			Assert.AreEqual(VideoQuality.High, controller.LocalVideoQuality);
			controller.LogReceivedVideoQuality(1, VideoQuality.NotSpecified, VideoQuality.High);
			Assert.AreEqual(VideoQuality.High, controller.LocalVideoQuality);
		}

		/// <summary>
		/// If we're the controller, and anyone proposes a lower video quality, immediately move to a lower quality.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogReceivedVideoQualityTest_LowerProposedVideoQuality_Controller()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(1, remoteSessions);
			controller.LogReceivedVideoQuality(2, VideoQuality.NotSpecified, VideoQuality.Low);
			Assert.AreEqual(VideoQuality.Low, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.Low, controller.CommandedVideoQuality);
		}

		/// <summary>
		/// If we're not the controller, and anyone proposes a lower video quality, ignore it.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogReceivedVideoQualityTest_LowerProposedVideoQuality_NotController()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(2, remoteSessions);
			controller.LogReceivedVideoQuality(1, VideoQuality.NotSpecified, VideoQuality.Low);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.NotSpecified, controller.CommandedVideoQuality);
		}

		/// <summary>
		/// If we're the controller, we should select the lowest proposed video quality.
		/// </summary>
		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogReceivedVideoQualityTest_SelectLowestProposedVideoQuality_Controller()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(1, remoteSessions);
			for (ushort i = 2; i < 60; i++)
			{
				controller.LogReceivedVideoQuality(i, VideoQuality.NotSpecified, VideoQuality.High);
				controller.Now += TimeSpan.FromSeconds(1);
			}
			Assert.AreEqual(VideoQuality.High, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.High, controller.CommandedVideoQuality);
			controller.Now += controller.QualityUpdateInterval;
			controller.LogReceivedVideoQuality(2, VideoQuality.NotSpecified, VideoQuality.Low);
			Assert.AreEqual(VideoQuality.Low, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.Low, controller.CommandedVideoQuality);
		}

		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogNetworkGlitchTest_NotController()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(1, remoteSessions);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);
			controller.Now += controller.QualityHoldInterval + TimeSpan.FromSeconds(1);
			for (int i = 0; i < VideoQualityController.MaxGlitches; i++)
			{
				controller.LogGlitch(1);
			}
			Assert.AreEqual(VideoQuality.Low, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.Low, controller.ProposedVideoQuality);
			Assert.AreEqual(VideoQuality.NotSpecified, controller.CommandedVideoQuality);
			controller.Now += controller.QualityHoldInterval + TimeSpan.FromSeconds(1);
			for (int i = 0; i < VideoQualityController.MaxGlitches; i++)
			{
				controller.LogGlitch(1);
			}
			Assert.AreEqual(VideoQuality.Fallback, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.Fallback, controller.ProposedVideoQuality);
			Assert.AreEqual(VideoQuality.NotSpecified, controller.CommandedVideoQuality);
		}

		[TestMethod]
		[Tag("videoquality")]
		[Tag("media")]
		public void LogNetworkGlitchTest_Controller()
		{
			var remoteSessions = new Dictionary<ushort, VideoThreadData>();
			remoteSessions[1] = null;
			var controller = new VideoQualityController(1, remoteSessions);

			// This has the side-effect of setting IsController() to true.
			controller.LogReceivedVideoQuality(2, VideoQuality.NotSpecified, VideoQuality.Medium);
			Assert.AreEqual(VideoQuality.Medium, controller.LocalVideoQuality);

			controller.Now += controller.QualityHoldInterval + TimeSpan.FromSeconds(1);
			for (int i = 0; i < VideoQualityController.MaxGlitches; i++)
			{
				controller.LogGlitch(1);
			}
			Assert.AreEqual(VideoQuality.Low, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.Low, controller.ProposedVideoQuality);
			Assert.AreEqual(VideoQuality.Low, controller.CommandedVideoQuality);

			controller.Now += controller.QualityHoldInterval + TimeSpan.FromSeconds(1);
			for (int i = 0; i < VideoQualityController.MaxGlitches; i++)
			{
				controller.LogGlitch(1);
			}
			Assert.AreEqual(VideoQuality.Fallback, controller.LocalVideoQuality);
			Assert.AreEqual(VideoQuality.Fallback, controller.ProposedVideoQuality);
			Assert.AreEqual(VideoQuality.Fallback, controller.CommandedVideoQuality);
		}


		#endregion

	}
}
