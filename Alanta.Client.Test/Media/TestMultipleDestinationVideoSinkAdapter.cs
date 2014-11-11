using System;
using System.Collections.Generic;
using System.Windows.Media;
using Alanta.Client.Media;
using System.Linq;

namespace Alanta.Client.Test.Media
{
    public class TestMultipleDestinationVideoSinkAdapter : VideoSinkAdapter
    {
        public TestMultipleDestinationVideoSinkAdapter(CaptureSource captureSource, SourceMediaController mediaController, Dictionary<Guid, DestinationMediaController> mediaControllers)
            : base(captureSource, mediaController, mediaController.VideoQualityController)
        {
            this.mediaControllers = mediaControllers;
        }

        private Dictionary<Guid, DestinationMediaController> mediaControllers { get; set; }

        protected override void SubmitFrame(byte[] frame, int stride)
        {
            // Instead of just submitting it to one MediaController, submit it to an array of them.
            base.SubmitFrame(frame, stride);
            foreach (DestinationMediaController testMediaController in mediaControllers.Values)
            {
                testMediaController.SetVideoFrame(frame, stride);
            }
        }
    }
}
