using System.IO;

namespace Alanta.Client.Media.Jpeg.Encoder
{
    public class HuffmanBuffer
    {
        public HuffmanBuffer(Stream outStream)
        {
            this.outStream = outStream;
        }

        private Stream outStream;
        int bufferPutBits, bufferPutBuffer;

        /// <summary>
        /// Uses an integer long (32 bits) buffer to store the Huffman encoded bits
        /// and sends them to outStream by the byte.
        /// </summary>
        public void AddToBuffer(int code, int size)
        {
            int putBuffer = code;
            int putBits = bufferPutBits;

            putBuffer &= (1 << size) - 1;
            putBits += size;
            putBuffer <<= 24 - putBits;
            putBuffer |= bufferPutBuffer;

            while (putBits >= 8)
            {
                int c = ((putBuffer >> 16) & 0xFF);
                outStream.WriteByte((byte)c);

                // FF must be escaped
                if (c == 0xFF) outStream.WriteByte(0);

                putBuffer <<= 8;
                putBits -= 8;
            }
            bufferPutBuffer = putBuffer;
            bufferPutBits = putBits;

        }

        public void FlushBuffer()
        {
            int putBuffer = bufferPutBuffer;
            int putBits = bufferPutBits;
            while (putBits >= 8)
            {
                int c = ((putBuffer >> 16) & 0xFF);
                outStream.WriteByte((byte)c);

                // FF must be escaped
                if (c == 0xFF) outStream.WriteByte(0);

                putBuffer <<= 8;
                putBits -= 8;
            }
            if (putBits > 0)
            {
                int c = ((putBuffer >> 16) & 0xFF);
                outStream.WriteByte((byte)c);
            }
        }

    }
}
