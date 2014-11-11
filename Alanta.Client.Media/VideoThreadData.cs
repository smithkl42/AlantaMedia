using System.Collections.Generic;
using System.Threading;
using Alanta.Client.Media.VideoCodecs;

namespace Alanta.Client.Media
{
    public class VideoThreadData
    {
        public VideoThreadData(ushort ssrcId, IVideoCodec decoder)
        {
            SsrcId = ssrcId;
            VideoChunkQueue = new Queue<Chunk>(VideoConstants.MaxQueuedBlocksPerStream * 2); // Make it bigger so it never has to get resized.
            ResetEvent = new ManualResetEvent(false);
            Decoder = decoder;
            Validator = new RemoteCameraValidatorEntity();
        }

        public Queue<Chunk> VideoChunkQueue { get; private set; }
        public ManualResetEvent ResetEvent { get; private set; }
        public IVideoCodec Decoder { get; private set; }
        public RemoteCameraValidatorEntity Validator { get; private set; }
        public ushort SsrcId { get; private set; }
        public int PendingDecodeJobs;
        public int ChunksProcessed;
    }
}
