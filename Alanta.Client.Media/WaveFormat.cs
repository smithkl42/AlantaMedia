//-----------------------------------------------------------------------
// <copyright file="WaveFormatEx.cs" company="Gilles Khouzam">
// (c) Copyright Gilles Khouzam
// Based on the sample from Larry Olson
// This source is subject to the Microsoft Public License (Ms-PL)
// All other rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Alanta.Client.Media
{
    using System;
    using System.Text;
    using System.Windows.Media;

    /// <summary>
    /// Class WAVEFORMATEX
    /// Implementation of a standard WAVEFORMATEX structure 
    /// </summary>
    public class WaveFormat
    {
        #region Data
        /// <summary>
        /// The size of the basic structure
        /// </summary>
        public const uint SizeOf = 18;

        /// <summary>
        /// The different formats allowable. For now PCM is the only one we support
        /// </summary>
        private const short FormatPCM = 1;

        /// <summary>
        ///  Gets or sets the FormatTag
        /// </summary>
        public WaveFormatType FormatTag
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the number of Channels
        /// </summary>
        public short Channels
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the number of samples per second
        /// </summary>
        public int SamplesPerSec
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the average bytes per second
        /// </summary>
        public int AvgBytesPerSec
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the alignment of the blocks
        /// </summary>
        public short BlockAlign
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of bits per sample (8 or 16)
        /// </summary>
        public short BitsPerSample
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the structure
        /// </summary>
        public short Size
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the extension buffer
        /// </summary>
        public byte[] Ext
        {
            get;
            set;
        }
        #endregion Data

        #region Methods
        /// <summary>
        /// Convert a BigEndian string to a LittleEndian string
        /// </summary>
        /// <param name="bigEndianString">A big endian string</param>
        /// <returns>The little endian string</returns>
        public static string ToLittleEndianString(string bigEndianString)
        {
            if (bigEndianString == null)
            {
                return string.Empty;
            }

            char[] bigEndianChars = bigEndianString.ToCharArray();

            // Guard
            if (bigEndianChars.Length % 2 != 0)
            {
                return string.Empty;
            }

            int i, ai, bi, ci, di;
            char a, b, c, d;
            for (i = 0; i < bigEndianChars.Length / 2; i += 2)
            {
                // front byte
                ai = i;
                bi = i + 1;

                // back byte
                ci = bigEndianChars.Length - 2 - i;
                di = bigEndianChars.Length - 1 - i;

                a = bigEndianChars[ai];
                b = bigEndianChars[bi];
                c = bigEndianChars[ci];
                d = bigEndianChars[di];

                bigEndianChars[ci] = a;
                bigEndianChars[di] = b;
                bigEndianChars[ai] = c;
                bigEndianChars[bi] = d;
            }

            return new string(bigEndianChars);
        }

        /// <summary>
        /// Convert the data to a hex string
        /// </summary>
        /// <returns>A string in hexadecimal</returns>
        public string ToHexString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(ToLittleEndianString(string.Format("{0:X4}", (short)this.FormatTag)));
            sb.Append(ToLittleEndianString(string.Format("{0:X4}", this.Channels)));
            sb.Append(ToLittleEndianString(string.Format("{0:X8}", this.SamplesPerSec)));
            sb.Append(ToLittleEndianString(string.Format("{0:X8}", this.AvgBytesPerSec)));
            sb.Append(ToLittleEndianString(string.Format("{0:X4}", this.BlockAlign)));
            sb.Append(ToLittleEndianString(string.Format("{0:X4}", this.BitsPerSample)));
            sb.Append(ToLittleEndianString(string.Format("{0:X4}", this.Size)));

            return sb.ToString();
        }

        /// <summary>
        /// Set the data from a byte array (usually read from a file)
        /// </summary>
        /// <param name="byteArray">The array used as input to the stucture</param>
        public void SetFromByteArray(byte[] byteArray)
        {
            if ((byteArray.Length + 2) < SizeOf)
            {
                throw new ArgumentException("Byte array is too small");
            }

            this.FormatTag = (WaveFormatType)BitConverter.ToInt16(byteArray, 0);
            this.Channels = BitConverter.ToInt16(byteArray, 2);
            this.SamplesPerSec = BitConverter.ToInt32(byteArray, 4);
            this.AvgBytesPerSec = BitConverter.ToInt32(byteArray, 8);
            this.BlockAlign = BitConverter.ToInt16(byteArray, 12);
            this.BitsPerSample = BitConverter.ToInt16(byteArray, 14);
            if (byteArray.Length >= SizeOf)
            {
                this.Size = BitConverter.ToInt16(byteArray, 16);
            }
            else
            {
                this.Size = 0;
            }

            if (byteArray.Length > WaveFormat.SizeOf)
            {
                this.Ext = new byte[byteArray.Length - WaveFormat.SizeOf];
                Buffer.BlockCopy(byteArray, (int)WaveFormat.SizeOf, this.Ext, 0, this.Ext.Length);
            }
            else
            {
                this.Ext = null;
            }
        }

        /// <summary>
        /// Ouput the data into a string.
        /// </summary>
        /// <returns>A string representing the WAVEFORMATEX</returns>
        public override string ToString()
        {
            char[] rawData = new char[18];
            BitConverter.GetBytes((short)this.FormatTag).CopyTo(rawData, 0);
            BitConverter.GetBytes(this.Channels).CopyTo(rawData, 2);
            BitConverter.GetBytes(this.SamplesPerSec).CopyTo(rawData, 4);
            BitConverter.GetBytes(this.AvgBytesPerSec).CopyTo(rawData, 8);
            BitConverter.GetBytes(this.BlockAlign).CopyTo(rawData, 12);
            BitConverter.GetBytes(this.BitsPerSample).CopyTo(rawData, 14);
            BitConverter.GetBytes(this.Size).CopyTo(rawData, 16);
            return new string(rawData);
        }
        #endregion
    }
}
