using System.Collections.Generic;
using System.IO;

namespace Alanta.Client.Media.VideoCodecs
{
	public interface IVideoCodec
	{
		void Initialize(ushort height, ushort width, short maxChunkSize);
		void EncodeFrame(byte[] image, int stride);

		/// <summary>
		/// Writes the next chunk onto a buffer.
		/// </summary>
		/// <param name="buffer">The buffer onto which the chunks should be written.</param>
		/// <param name="moreChunks">Whether there are still more chunks which could be retrieved.</param>
		/// <returns>True if a chunk was retrieved, false if not.</returns>
		bool GetNextChunk(ByteStream buffer, out bool moreChunks);

		void DecodeChunk(ByteStream frame, ushort remoteSsrcId);
		MemoryStream GetNextFrame();
		bool IsReceivingData { get; }

		/// <summary>
		/// Tells the video codec to send a complete frame. Usually called when someone new joins the room.
		/// </summary>
		void Synchronize();
	}
}
