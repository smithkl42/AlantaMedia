
namespace Alanta.Client.Media
{
    public static class VideoConstants
    {
        /// <summary>
        /// When in Loopback mode, controls the number of cameras into which the video stream is split and played back.
        /// </summary>
        public const int NumLoopbackCameras = 4;

        /// <summary>
        /// The video quality of the compressed JPEG blocks.
        /// </summary>
        // public const int JpegQuality = 20;

        /// <summary>
        /// The maximum payload size of the RTP packet.
        /// </summary>
        public const short MaxPayloadSize = 1024;

        // ks 4/24/10 - I haven't figured out how to make these work correctly.
        // See http://en.wikipedia.org/wiki/Chroma_subsampling
        public static readonly byte[] HSampRatio = { 1, 1, 1 };
        public static readonly byte[] VSampRatio = { 1, 1, 1 };

        public const ushort SampleFramesPerSecond = 30;
        public const ushort Height = 480;
        public const ushort Width = 640;
        public const int BytesPerFrame = Height * Width * BytesPerPixel;

        public const ushort BytesPerPixel = 4;
        public const ushort VideoBlockSize = 16;

        public const float MinimumPixelDistance = 10;
        public const int MaxQueuedBlocksPerStream = ((Height * Width) / (VideoBlockSize ^ 2)) * 2; // Only store two complete frames worth of blocks for transmission.
        public const int MaxFramesOutOfOrder = 5;

        public const int RemoteCameraTimeout = 10000;   // In Milliseconds
        public const int RemoteCameraCheckDelay = 500; // In Milliseconds
 
    }

}
