using System;
using System.Net;

namespace Alanta.Client.Media
{
    /// <summary>
    /// The ByteStream class serves as a thin wrapper around a byte[] array.  It's modeled roughly after the functionality
    /// of a MemoryStream wrapped with a BinaryReader or BinaryWriter, but it handles endian conversions correctly,
    /// and is ~2x faster.
    /// </summary>
    public class ByteStream
    {
        #region Constructors
        public ByteStream()
        {
        }

        public ByteStream(int dataLength)
        {
            Data = new byte[dataLength];
            DataLength = dataLength;
            DataOffset = 0;
            CurrentOffset = 0;
        }

        public ByteStream(byte[] array)
        {
            Data = array;
            DataOffset = 0;
            CurrentOffset = 0;
            DataLength = array.Length;
        }

        public ByteStream(byte[] array, int offset, int length)
        {
            Data = array;
            DataOffset = offset;
            DataLength = length;
            CurrentOffset = offset;
        }
        #endregion

        #region Fields and Properties

        // Implemented as fields rather than properties because testing shows (against all docs) that field references are somewhat faster.
        public byte[] Data;
        public int CurrentOffset;
        public int DataOffset;
        public int DataLength;

        public int EndOffset
        {
            get
            {
                return DataOffset + DataLength;
            }
        }

        public int RemainingBytes
        {
            get
            {
                return EndOffset - CurrentOffset;
            }
        }

        #endregion

        #region Methods
        public void Reset()
        {
            DataOffset = 0;
            CurrentOffset = 0;
            DataLength = Data.Length;
        }

        public void ResetCurrentOffset()
        {
            CurrentOffset = DataOffset;
        }

        #region UInt16
        public ushort ReadUInt16Network()
        {
            ushort value = BitConverter.ToUInt16(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(Data, CurrentOffset))), 2);
            CurrentOffset += sizeof(ushort);
            return value;
        }

        public void WriteUInt16Network(ushort value)
        {
            byte[] valueInBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            Buffer.BlockCopy(valueInBytes, 2, Data, CurrentOffset, sizeof(ushort));
            CurrentOffset += sizeof(ushort);
        }

        public ushort ReadUInt16()
        {
            ushort value = BitConverter.ToUInt16(Data, CurrentOffset);
            CurrentOffset += sizeof(ushort);
            return value;
        }

        public void WriteUInt16(ushort value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Data, CurrentOffset, sizeof(ushort));
            CurrentOffset += sizeof(ushort);
        }
        #endregion Uint16

        #region Int16
        public short ReadInt16Network()
        {
            short value = BitConverter.ToInt16(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(Data, CurrentOffset))), 2);
            CurrentOffset += sizeof(short);
            return value;
        }

        public void WriteInt16Network(short value)
        {
            byte[] valueInBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            Buffer.BlockCopy(valueInBytes, 0, Data, CurrentOffset, sizeof(short));
            CurrentOffset += sizeof(short);
        }

        public short ReadInt16()
        {
            short value = BitConverter.ToInt16(Data, CurrentOffset);
            CurrentOffset += sizeof(short);
            return value;
        }

        public void WriteInt16(short value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Data, CurrentOffset, sizeof(short));
            CurrentOffset += sizeof(short);
        }
        #endregion Int16

        #region Int32
        public int ReadInt32Network()
        {
            int value = BitConverter.ToInt32(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Data, CurrentOffset))), 0);
            CurrentOffset += sizeof(int);
            return value;
        }

        public void WriteInt32Network(int value)
        {
            byte[] valueInBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            Buffer.BlockCopy(valueInBytes, 0, Data, CurrentOffset, sizeof(int));
            CurrentOffset += sizeof(int);
        }

        public int ReadInt32()
        {
            int value = BitConverter.ToInt32(Data, CurrentOffset);
            CurrentOffset += sizeof(int);
            return value;
        }

        public void WriteInt32(int value)
        {
            byte[] valueInBytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(valueInBytes, 0, Data, CurrentOffset, sizeof(int));
            CurrentOffset += sizeof(int);
        }
        #endregion Int32

        #region Uint32
        public uint ReadUInt32Network()
        {
            uint value = BitConverter.ToUInt32(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(BitConverter.ToUInt32(Data, CurrentOffset))), 4);
            CurrentOffset += sizeof(uint);
            return value;
        }

        public void WriteUInt32Network(uint value)
        {
            byte[] valueInBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            Buffer.BlockCopy(valueInBytes, 4, Data, CurrentOffset, sizeof(uint));
            CurrentOffset += sizeof(uint);
        }

        public uint ReadUInt32()
        {
            uint value = BitConverter.ToUInt32(Data, CurrentOffset);
            CurrentOffset += sizeof(uint);
            return value;
        }

        public void WriteUInt32(uint value)
        {
            byte[] valueInBytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(valueInBytes, 0, Data, CurrentOffset, sizeof(uint));
            CurrentOffset += sizeof(uint);
        }
        #endregion Uint32

        #region Bytes
        /// <summary>
        /// Read bytes from the ByteArray onto a destination ByteStream buffer
        /// </summary>
        /// <param name="destination">The destination of the read bytes</param>
        /// <param name="length">The number of bytes to read</param>
        /// <returns>True if successful, false if not.</returns>
        public bool TryReadBytes(ByteStream destination, int length)
        {
            bool success = TryReadBytes(destination.Data, destination.CurrentOffset, length);
            if (success)
            {
                destination.CurrentOffset += length;
            }
            return success;
        }

        public bool TryReadBytes(Array destination, int offset, int length)
        {
            // Buffer.BlockCopy does the same checks, but it throws an exception when they fail, 
            // so it's faster to do the same check ourselves and then return false.
            if (CurrentOffset + length > Data.Length || offset + length > destination.Length)
            {
                return false;
            }
            Buffer.BlockCopy(Data, CurrentOffset, destination, offset, length);
            CurrentOffset += length;
            return true;
        }

        /// <summary>
        /// Read bytes from the ByteArray onto a destination ByteStream buffer.
        /// </summary>
        /// <param name="destination">The destination of the read bytes</param>
        /// <param name="length">The number of bytes to read</param>
        public void ReadBytes(ByteStream destination, int length)
        {
            ReadBytes(destination.Data, destination.CurrentOffset, length);
            destination.CurrentOffset += length;
        }

        public void ReadBytes(byte[] destination, int offset, int length)
        {
            Buffer.BlockCopy(Data, CurrentOffset, destination, offset, length);
            CurrentOffset += length;
        }

        public bool TryWriteBytes(ByteStream source, int length)
        {
            bool success = TryWriteBytes(source.Data, source.CurrentOffset, length);
            if (success)
            {
                source.CurrentOffset += length;
            }
            return success;
        }

        public bool TryWriteBytes(Array source, int offset, int length)
        {
            if (CurrentOffset + length > Data.Length || offset + length > source.Length)
            {
                return false;
            }
            Buffer.BlockCopy(source, offset, Data, CurrentOffset, length);
            CurrentOffset += length;
            return true;
        }

        public void WriteBytes(ByteStream source, int length)
        {
            WriteBytes(source.Data, source.CurrentOffset, length);
            source.CurrentOffset += length;
        }

        public void WriteBytes(byte[] source, int offset, int length)
        {
            Buffer.BlockCopy(source, offset, Data, CurrentOffset, length);
            CurrentOffset += length;
        }

        public byte[] ToByteArray()
        {
            byte[] array = new byte[DataLength];
            Buffer.BlockCopy(Data, DataOffset, array, 0, DataLength);
            return array;
        }

        public byte ReadByte()
        {
            return Data[CurrentOffset++];
        }

        public void WriteByte(byte value)
        {
            Data[CurrentOffset++] = value;
        }

        public void InsertBytes(ByteStream source)
        {
            InsertBytes(source.Data, source.CurrentOffset, source.RemainingBytes);
        }

        public void InsertBytes(byte[] source, int offset, int length)
        {
            // Make the new packet two bytes larger than the old, to handle the ssrcId.
            byte[] newArray = new byte[Data.Length + length];

            // Copy the basic stream header.
            Buffer.BlockCopy(Data, 0, newArray, 0, CurrentOffset);

            // Insert the new bytes into the new array.
            Buffer.BlockCopy(source, offset, newArray, CurrentOffset, length);

            // Copy the remainder of the old array.
            int newOffset = CurrentOffset + length;
            Buffer.BlockCopy(Data, CurrentOffset, newArray, newOffset, Data.Length - CurrentOffset);

            Data = newArray;
            CurrentOffset = newOffset;
            DataLength = DataLength + length;

        }
        #endregion Bytes

        #endregion
    }
}
