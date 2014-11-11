using System;
using System.IO;

namespace Alanta.Client.Media
{
	public interface IVideoController
	{
		void GetNextVideoFrame(ushort ssrcId, Action<MemoryStream> callback);
		void SetVideoFrame(byte[] frame, int stride);
	}
}
