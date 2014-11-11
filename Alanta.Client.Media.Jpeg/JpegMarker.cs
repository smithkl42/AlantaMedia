/// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
/// Under the MIT License, details: License.txt.

using System;

namespace Alanta.Client.Media.Jpeg
{
    internal sealed class JpegMarker
    {
        // JFIF identifiers
        public const byte JFIF_J = 0x4a;
        public const byte JFIF_F = 0x46;
        public const byte JFIF_I = 0x49;
        public const byte JFIF_X = 0x46;

        // JFIF extension codes
        public const byte JFXX_JPEG = 0x10;
        public const byte JFXX_ONE_BPP = 0x11;
        public const byte JFXX_THREE_BPP = 0x13;

        // Marker prefix. Next byte is a marker, unless ...
        public const byte XFF = 0xff;
        // ... marker byte encoding an xff.
        public const byte X00 = 0x00;

        #region Section Markers

        /// <summary>Huffman Table</summary>
        public const byte DHT = 0xc4;

        /// <summary>Quantization Table</summary>
        public const byte DQT = 0xdb;

        /// <summary>Start of Scan</summary>
        public const byte SOS = 0xda;

        /// <summary>Define Restart Interval</summary>
        public const byte DRI = 0xdd;

        /// <summary>Comment</summary>
        public const byte COM = 0xfe;

        /// <summary>Start of Image</summary>
        public const byte SOI = 0xd8;

        /// <summary>End of Image</summary>
        public const byte EOI = 0xd9;

        /// <summary>Define Number of Lines</summary>
        public const byte DNL = 0xdc;

        #endregion

        #region Application Reserved Keywords

        public const byte APP0 = 0xe0;
        public const byte APP1 = 0xe1;
        public const byte APP2 = 0xe2;
        public const byte APP3 = 0xe3;
        public const byte APP4 = 0xe4;
        public const byte APP5 = 0xe5;
        public const byte APP6 = 0xe6;
        public const byte APP7 = 0xe7;
        public const byte APP8 = 0xe8;
        public const byte APP9 = 0xe9;
        public const byte APP10 = 0xea;
        public const byte APP11 = 0xeb;
        public const byte APP12 = 0xec;
        public const byte APP13 = 0xed;
        public const byte APP14 = 0xee;
        public const byte APP15 = 0xef;

        #endregion

        public const byte RST0 = 0xd0;
        public const byte RST1 = 0xd1;
        public const byte RST2 = 0xd2;
        public const byte RST3 = 0xd3;
        public const byte RST4 = 0xd4;
        public const byte RST5 = 0xd5;
        public const byte RST6 = 0xd6;
        public const byte RST7 = 0xd7;

        #region Start of Frame (SOF)

        /// <summary>Nondifferential Huffman-coding frame (baseline dct)</summary>
        public const byte SOF0 = 0xc0;

        /// <summary>Nondifferential Huffman-coding frame (extended dct)</summary>
        public const byte SOF1 = 0xc1;

        /// <summary>Nondifferential Huffman-coding frame (progressive dct)</summary>
        public const byte SOF2 = 0xc2;

        /// <summary>Nondifferential Huffman-coding frame Lossless (Sequential)</summary>
        public const byte SOF3 = 0xc3;

        /// <summary>Differential Huffman-coding frame Sequential DCT</summary>
        public const byte SOF5 = 0xc5;

        /// <summary>Differential Huffman-coding frame Progressive DCT</summary> 
        public const byte SOF6 = 0xc6;

        /// <summary>Differential Huffman-coding frame lossless</summary>
        public const byte SOF7 = 0xc7;

        /// <summary>Nondifferential Arithmetic-coding frame (extended dct)</summary>
        public const byte SOF9 = 0xc9;

        /// <summary>Nondifferential Arithmetic-coding frame (progressive dct)</summary>
        public const byte SOF10 = 0xca;

        /// <summary>Nondifferential Arithmetic-coding frame (lossless)</summary>
        public const byte SOF11 = 0xcb;

        /// <summary>Differential Arithmetic-coding frame (sequential dct)</summary>
        public const byte SOF13 = 0xcd;

        /// <summary>Differential Arithmetic-coding frame (progressive dct)</summary>
        public const byte SOF14 = 0xce;

        /// <summary>Differential Arithmetic-coding frame (lossless)</summary>
        public const byte SOF15 = 0xcf;

        #endregion

    }
}
