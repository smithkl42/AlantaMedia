
namespace Alanta.Client.Media.VideoCodecs
{
    public class Chunk
    {
        public ushort SsrcId { get; set; }
        public ByteStream Payload { get; set; }
    }
}
