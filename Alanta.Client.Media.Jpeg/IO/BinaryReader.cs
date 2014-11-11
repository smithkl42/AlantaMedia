// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System.IO;
using Alanta.Client.Media.Jpeg.Decoder;

namespace Alanta.Client.Media.Jpeg.IO
{
	/// <summary>
	/// Big-endian binary reader
	/// </summary>
	public class BinaryReader
	{
		readonly byte[] buffer;

		public Stream BaseStream { get; private set; }

		public BinaryReader(byte[] data) : this(new MemoryStream(data)) { }

		public BinaryReader(Stream stream)
		{
			BaseStream = stream;
			buffer = new byte[2];
		}

		//public byte ReadByte()
		//{
		//    int b = _stream.ReadByte();
		//    if (b == -1) throw new EndOfStreamException();
		//    return (byte)b;
		//}

		public JpegReadStatusByte ReadByte()
		{
			var status = new JpegReadStatusByte();
			int b = BaseStream.ReadByte();
			if (b == -1)
			{
				status.Result = 0;
				status.Status = Status.EOF;
			}
			else
			{
				status.Result = (byte)b;
				status.Status = Status.Success;
			}
			return status;
		}

		public ushort ReadShort()
		{
			BaseStream.Read(buffer, 0, 2);
			return (ushort)((buffer[0] << 8) | (buffer[1] & 0xff));
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			return BaseStream.Read(buffer, offset, count);
		}

	}
}
