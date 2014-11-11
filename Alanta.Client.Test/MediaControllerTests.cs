using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlideLinc.Client.Common.RoomService;
using SlideLinc.Client.Common;
using System.Threading;
using Microsoft.Silverlight.Testing;
using SlideLinc.Client.Common.Media;

namespace SlideLinc.Client.Test
{
    // [TestClass]
    public class MediaControllerTests : SilverlightTest
    {
        private TimeSpan timeout = new TimeSpan(0, 0, 30);
        public MediaControllerTests()
        {
            Globals.MediaServerHost = "localhost";
        }

        /// <summary>
        /// Tests to make sure that the MediaController is able to connect to the local media controller.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void MediaControllerConnectTest()
        {
            bool connected = false;
            MediaController controller = MediaController.GetMediaController();
            Assert.IsFalse(controller.IsAvailable);
            controller.LocalMediaAvailable += (s, e) =>
                {
                    Assert.IsTrue(controller.IsAvailable);
                    controller.Dispose();
                    connected = true;
                };
            controller.Connect();
            EnqueueConditional(() => connected);
            EnqueueTestComplete();
        }

        // [TestMethod]
        [Asynchronous]
        public void MediaControllerMuteTest()
        {
            bool muted = false;
            MediaController controller = MediaController.GetMediaController();
            Assert.IsFalse(controller.IsAvailable);
            controller.LocalMediaAvailable += (s, e) =>
            {
                Assert.IsTrue(controller.IsAvailable);
                controller.Mute(error =>
                    {
                        CheckError(error, controller);
                        muted = true;
                    });
                
            };
            controller.Connect();
            EnqueueConditional(() => muted);
            EnqueueTestComplete();
        }

        // [TestMethod]
        [Asynchronous]
        public void MediaControllerUnmuteTest()
        {
            bool unmuted = false;
            MediaController controller = MediaController.GetMediaController();
            Assert.IsFalse(controller.IsAvailable);
            controller.LocalMediaAvailable += (s, e) =>
            {
                Assert.IsTrue(controller.IsAvailable);
                controller.Unmute(error =>
                    {
                        CheckError(error, controller);
                        unmuted = true;
                    });
            };
            controller.Connect();
            EnqueueConditional(() => unmuted);
            EnqueueTestComplete();
        }

        // [TestMethod]
        [Asynchronous]
        public void MediaControllerGetVolumeTest()
        {
            bool volumeRetrieved = false;
            MediaController controller = MediaController.GetMediaController();
            Assert.IsFalse(controller.IsAvailable);
            controller.LocalMediaAvailable += (s, e) =>
            {
                Assert.IsTrue(controller.IsAvailable);
                controller.GetVolume((error, volume) =>
                    {
                        Assert.IsTrue(volume > 0);
                        CheckError(error, controller);
                        volumeRetrieved = true;
                    });
            };
            controller.Connect();
            EnqueueConditional(() => volumeRetrieved);
            EnqueueTestComplete();
        }

        // [TestMethod]
        [Asynchronous]
        public void MediaControllerSetVolumeTest()
        {
            bool volumeSet = false;
            MediaController controller = MediaController.GetMediaController();
            Assert.IsFalse(controller.IsAvailable);
            controller.LocalMediaAvailable += (s, e) =>
            {
                Assert.IsTrue(controller.IsAvailable);
                int volume = 5;
                controller.SetVolume(volume, error => CheckError(error, controller));
                volumeSet = true;
            };
            controller.Connect();
            EnqueueConditional(() => volumeSet);
            EnqueueTestComplete();
        }

        private void CheckError(Exception error, MediaController controller)
        {
            controller.Dispose();
            Assert.IsNull(error);
        }
    }
}
