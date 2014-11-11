// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
// Under the MIT License, details: License.txt.

using System;
using System.IO;
using Alanta.Client.Media.Jpeg.Decoder;

namespace Alanta.Client.Media.Jpeg.IO
{
    public class JpegMarkerFoundException : Exception
    {
        public JpegMarkerFoundException(byte marker) { Marker = marker; }
        public byte Marker;
    }

    public class JpegBinaryReader : BinaryReader
    {
        public int eobRun;

        // private byte marker;

        public JpegBinaryReader(Stream input)
            : base(input)
        {
        }

        /// <summary>
        /// Seeks through the stream until a marker is found.
        /// </summary>
        public JpegReadStatusByte GetNextMarker()
        {
            var status = ReadJpegByte();
            while (status.Status == Status.Success)
            {
                status = ReadJpegByte();
            }
            return status;
        }

        byte bitBuffer;

        protected int bitsLeft = 0;

        public int BitOffset
        {
            get { return (8 - bitsLeft) % 8; }
            set
            {
                if (bitsLeft != 0) BaseStream.Seek(-1, SeekOrigin.Current);
                bitsLeft = (8 - value) % 8;
            }
        }

        /// <summary>
        /// Places n bits from the stream, where the most-significant bits
        /// from the first byte read end up as the most-significant of the returned
        /// n bits.
        /// </summary>
        /// <param name="n">Number of bits to return</param>
        /// <returns>Integer containing the bits desired -- shifted all the way right.</returns>
        public JpegReadStatusInt ReadBits(int n)
        {
            JpegReadStatusInt status = new JpegReadStatusInt();
            int result = 0;

            #region Special case -- included for optimization purposes
            if (bitsLeft >= n)
            {
                bitsLeft -= n;
                status.Result = bitBuffer >> (8 - n);
                bitBuffer = (byte)(bitBuffer << n);
                status.Status = Status.Success;
                return status;
            }
            #endregion

            while (n > 0)
            {
                if (bitsLeft == 0)
                {
                    var statusByte = ReadJpegByte();
                    status = statusByte.ToInt();
                    if (status.Status != Status.Success)
                    {
                        return status; // We reached the end of the file, or found a marker.
                    }
                    bitBuffer = statusByte.Result;
                    bitsLeft = 8;
                }

                int take = n <= bitsLeft ? n : bitsLeft;

                result = result | ((bitBuffer >> 8 - take) << (n - take));

                bitBuffer = (byte)(bitBuffer << take);

                bitsLeft -= take;
                n -= take;
            }
            status.Result = result;
            return status;
        }

        protected JpegReadStatusByte ReadJpegByte()
        {
            JpegReadStatusByte status = ReadByte();
            if (status.Status == Status.Success)
            {
                /* If it's 0xFF, check and discard stuffed zero byte */
                if (status.Result == JpegMarker.XFF)
                {
                    // Discard padded oxFFs
                    while ((status = ReadByte()).Result == 0xff) ;

                    // ff00 is the escaped form of 0xff
                    if (status.Result == 0) status.Result = 0xff;
                    else
                    {
                        // Otherwise we've found a new marker.
                        status.Status = Status.MarkerFound;
                        return status;
                    }
                }
                status.Status = Status.Success;
                return status;
            }
            else
            {
                return status;
            }
        }

    }
}
