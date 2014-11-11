/// Copyright (c) 2008 Jeffrey Powers for Fluxcapacity Open Source.
/// Under the MIT License, details: License.txt.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Alanta.Client.Media.Jpeg.IO;
using System.Diagnostics;

namespace Alanta.Client.Media.Jpeg.Decoder
{
    public enum BlockUpsamplingMode
    {
        /// <summary> The simplest upsampling mode. Produces sharper edges. </summary>
        BoxFilter,
        /// <summary> Smoother upsampling. May improve color spread for some images. </summary>
        Interpolate
    }

    public class JpegDecodeProgressChangedArgs : EventArgs
    {
        public bool SizeReady;
        public int Width;
        public int Height;

        public bool Abort;
        public long ReadPosition; // 0 to input stream length
        public double DecodeProgress; // 0 to 1.0
    }

    public class JpegDecoder
    {
        public static long ProgressUpdateByteInterval = 100;

        public BlockUpsamplingMode BlockUpsamplingMode { get; set; }

        byte majorVersion, minorVersion;
        private enum UnitType { None = 0, Inches = 1, Centimeters = 2 };
        UnitType Units;
        ushort XDensity, YDensity;
        byte Xthumbnail, Ythumbnail;
        byte[] thumbnail;
        Image image;
        int width;
        int height;

        bool progressive = false;

        byte marker;

        /// <summary>
        /// This decoder expects JFIF 1.02 encoding.
        /// </summary>
        internal const byte MAJOR_VERSION = (byte)1;
        internal const byte MINOR_VERSION = (byte)2;

        /// <summary>
        /// The length of the JFIF field not including thumbnail data.
        /// </summary>
        internal static short JFIF_FIXED_LENGTH = 16;

        /// <summary>
        /// The length of the JFIF extension field not including extension data.
        /// </summary>
        internal static short JFXX_FIXED_LENGTH = 8;

        private JpegBinaryReader jpegReader;

        List<JpegFrame> jpegFrames = new List<JpegFrame>();

        JpegHuffmanTable[] dcTables = new JpegHuffmanTable[4];
        JpegHuffmanTable[] acTables = new JpegHuffmanTable[4];
        JpegQuantizationTable[] qTables = new JpegQuantizationTable[4];

        public JpegDecoder(Stream input)
        {
            jpegReader = new JpegBinaryReader(input);
            JpegReadStatusByte status;
            marker = (status = jpegReader.GetNextMarker()).Result;
            if (status.Status != Status.MarkerFound || status.Result != JpegMarker.SOI)
            {
                throw new Exception("Failed to find SOI marker.");
            }
        }

        /// <summary>
        /// Tries to parse the JFIF APP0 header
        /// See http://en.wikipedia.org/wiki/JFIF
        /// </summary>
        private bool TryParseJFIF(byte[] data)
        {
            IO.BinaryReader reader = new IO.BinaryReader(new MemoryStream(data));

            int length = data.Length + 2; // Data & length

            if (!(length >= JFIF_FIXED_LENGTH))
                return false;  // Header's too small.

            byte[] identifier = new byte[5];
            reader.Read(identifier, 0, identifier.Length);
            if (identifier[0] != JpegMarker.JFIF_J
                || identifier[1] != JpegMarker.JFIF_F
                || identifier[2] != JpegMarker.JFIF_I
                || identifier[3] != JpegMarker.JFIF_F
                || identifier[4] != JpegMarker.X00)
                return false;  // Incorrect bytes

            majorVersion = reader.ReadByte().Result;
            minorVersion = reader.ReadByte().Result;
            if (majorVersion != MAJOR_VERSION
                || (majorVersion == MAJOR_VERSION
                    && minorVersion > MINOR_VERSION)) // changed from <
                return false; // Unsupported version

            Units = (UnitType)reader.ReadByte().Result;
            if (Units != UnitType.None &&
                Units != UnitType.Inches &&
                Units != UnitType.Centimeters)
                return false; // Invalid units

            XDensity = reader.ReadShort();
            YDensity = reader.ReadShort();
            Xthumbnail = reader.ReadByte().Result;
            Ythumbnail = reader.ReadByte().Result;

            // 3 * for RGB data
            int thumbnailLength = 3 * Xthumbnail * Ythumbnail;
            if (length > JFIF_FIXED_LENGTH
                && thumbnailLength != length - JFIF_FIXED_LENGTH)
                return false; // Thumbnail fields invalid

            if (thumbnailLength > 0)
            {
                thumbnail = new byte[thumbnailLength];
                if (reader.Read(thumbnail, 0, thumbnailLength) != thumbnailLength)
                    return false; // Thumbnail data was missing!

            }

            return true;
        }

        public DecodedJpeg Decode()
        {
            // The frames in this jpeg are loaded into a list. There is
            // usually just one frame except in heirarchial progression where
            // there are multiple frames.
            JpegFrame frame = null;

            // The restart interval defines how many MCU's we should have
            // between the 8-modulo restart marker. The restart markers allow
            // us to tell whether or not our decoding process is working
            // correctly, also if there is corruption in the image we can
            // recover with these restart intervals. (See RSTm DRI).
            int resetInterval = 0;

            bool haveMarker = false;
            bool foundJFIF = false;

            List<JpegHeader> headers = new List<JpegHeader>();

            // Loop through until there are no more markers to read in, at
            // that point everything is loaded into the jpegFrames array and
            // can be processed.
            while (true)
            {
                #region Switch over marker types
                switch (marker)
                {
                    case JpegMarker.APP0:
                    // APP1 is used for EXIF data
                    case JpegMarker.APP1:
                    // Seldomly, APP2 gets used for extended EXIF, too
                    case JpegMarker.APP2:
                    case JpegMarker.APP3:
                    case JpegMarker.APP4:
                    case JpegMarker.APP5:
                    case JpegMarker.APP6:
                    case JpegMarker.APP7:
                    case JpegMarker.APP8:
                    case JpegMarker.APP9:
                    case JpegMarker.APP10:
                    case JpegMarker.APP11:
                    case JpegMarker.APP12:
                    case JpegMarker.APP13:
                    case JpegMarker.APP14:
                    case JpegMarker.APP15:
                    // COM: Comment
                    case JpegMarker.COM:

                        // Debug.WriteLine(string.Format("Extracting Header, Type={0:X}", marker));

                        JpegHeader header = ExtractHeader();

                        #region Check explicitly for Exif Data

                        if (header.Marker == JpegMarker.APP1 && header.Data.Length >= 6)
                        {
                            byte[] d = header.Data;

                            if (d[0] == 'E' &&
                                d[1] == 'x' &&
                                d[2] == 'i' &&
                                d[3] == 'f' &&
                                d[4] == 0 &&
                                d[5] == 0)
                            {
                                // Exif.  Do something?
                            }
                        }

                        #endregion

                        #region Check for Adobe header

                        if (header.Data.Length >= 5 && header.Marker == JpegMarker.APP14)
                        {
                            string asText = UTF8Encoding.UTF8.GetString(header.Data, 0, 5);
                            if (asText == "Adobe")
                            {
                                // ADOBE HEADER.  Do anything?
                            }
                        }

                        #endregion

                        headers.Add(header);

                        if (!foundJFIF && marker == JpegMarker.APP0)
                        {
                            foundJFIF = TryParseJFIF(header.Data);

                            if (foundJFIF) // Found JFIF... do JFIF extensions follow?
                            {
                                header.IsJFIF = true;
                                var status = jpegReader.GetNextMarker();
                                if (status.Status == Status.MarkerFound)
                                {
                                    // Yes, they do.
                                    marker = status.Result;
                                    if (marker == JpegMarker.APP0)
                                    {
                                        header = ExtractHeader();
                                        headers.Add(header);
                                    }
                                    else // No.  Delay processing this one.
                                    {
                                        haveMarker = true;
                                    }
                                }
                                else
                                {
                                    // ks: This is a legitimate exception, since it indicates that something anomalous has happened.
                                    throw new System.IO.EndOfStreamException();
                                }
                            }
                        }

                        break;

                    case JpegMarker.SOF0:
                    case JpegMarker.SOF2:

                        // SOFn Start of Frame Marker, Baseline DCT - This is the start
                        // of the frame header that defines certain variables that will
                        // be carried out through the rest of the encoding. Multiple
                        // frames are used in a hierarchical system, however most JPEG's
                        // only contain a single frame.

                        // Progressive or baseline?
                        progressive = marker == JpegMarker.SOF2;

                        jpegFrames.Add(new JpegFrame());
                        frame = (JpegFrame)jpegFrames[jpegFrames.Count - 1];

                        // Skip the frame length.
                        jpegReader.ReadShort();
                        // Bits percision, either 8 or 12.
                        frame.Precision = jpegReader.ReadByte().Result;
                        // Scan lines (height) 
                        frame.ScanLines = jpegReader.ReadShort();
                        // Scan samples per line (width) 
                        frame.SamplesPerLine = jpegReader.ReadShort();
                        // Number of Color Components (channels).
                        frame.ComponentCount = jpegReader.ReadByte().Result;

                        // Add all of the necessary components to the frame.
                        for (int i = 0; i < frame.ComponentCount; i++)
                        {
                            byte compId = jpegReader.ReadByte().Result;
                            byte sampleFactors = jpegReader.ReadByte().Result;
                            byte qTableId = jpegReader.ReadByte().Result;

                            byte sampleHFactor = (byte)(sampleFactors >> 4);
                            byte sampleVFactor = (byte)(sampleFactors & 0x0f);

                            frame.AddComponent(compId, sampleHFactor, sampleVFactor, qTableId);
                        }
                        break;

                    case JpegMarker.DHT:

                        // DHT non-SOF Marker - Huffman Table is required for decoding
                        // the JPEG stream, when we receive a marker we load in first
                        // the table length (16 bits), the table class (4 bits), table
                        // identifier (4 bits), then we load in 16 bytes and each byte
                        // represents the count of bytes to load in for each of the 16
                        // bytes. We load this into an array to use later and move on.
                        // Only 4 huffman tables can be used in an image.
                        int huffmanLength = (jpegReader.ReadShort() - 2);

                        // Keep looping until we are out of length.
                        int index = huffmanLength;

                        // Multiple tables may be defined within a DHT marker. This
                        // will keep reading until there are no tables left, most
                        // of the time there is just one table.
                        while (index > 0)
                        {
                            // Read the identifier information and class
                            // information about the Huffman table, then read the
                            // 16 byte codelength in and read in the Huffman values
                            // and put it into table info.
                            byte huffmanInfo = jpegReader.ReadByte().Result;
                            byte tableClass = (byte)(huffmanInfo >> 4);
                            byte huffmanIndex = (byte)(huffmanInfo & 0x0f);
                            short[] codeLength = new short[16];

                            for (int i = 0; i < codeLength.Length; i++)
                                codeLength[i] = jpegReader.ReadByte().Result;

                            int huffmanValueLen = 0;
                            for (int i = 0; i < 16; i++)
                                huffmanValueLen += codeLength[i];
                            index -= (huffmanValueLen + 17);

                            short[] huffmanVal = new short[huffmanValueLen];
                            for (int i = 0; i < huffmanVal.Length; i++)
                            {
                                huffmanVal[i] = jpegReader.ReadByte().Result;
                            }
                            // Assign DC Huffman Table.
                            if (tableClass == HuffmanTable.JPEG_DC_TABLE)
                                dcTables[(int)huffmanIndex] = new JpegHuffmanTable(codeLength, huffmanVal);

                            // Assign AC Huffman Table.
                            else if (tableClass == HuffmanTable.JPEG_AC_TABLE)
                                acTables[(int)huffmanIndex] = new JpegHuffmanTable(codeLength, huffmanVal);
                        }
                        break;

                    case JpegMarker.DQT:

                        // DQT non-SOF Marker - This defines the quantization
                        // coeffecients, this allows us to figure out the quality of
                        // compression and unencode the data. The data is loaded and
                        // then stored in to an array.
                        short quantizationLength = (short)(jpegReader.ReadShort() - 2);
                        for (int j = 0; j < quantizationLength / 65; j++)
                        {
                            byte quantSpecs = jpegReader.ReadByte().Result;
                            int[] quantData = new int[64];
                            if ((byte)(quantSpecs >> 4) == 0)
                            // Precision 8 bit.
                            {
                                for (int i = 0; i < 64; i++)
                                    quantData[i] = jpegReader.ReadByte().Result;

                            }
                            else if ((byte)(quantSpecs >> 4) == 1)
                            // Precision 16 bit.
                            {
                                for (int i = 0; i < 64; i++)
                                    quantData[i] = jpegReader.ReadShort();
                            }

                            // The quantData comes out of the JPEG image in zig-zag format, and if the quantization step takes place before
                            // the IDCT/unzigzag step, the quantization tables should remain in zig-zag format.  However, if the
                            // quantization takes place during the IDCT (as in the AAN IDCT implementation), the quantization tables
                            // need to be unzigzagged back to normal order.
                            if (JpegConstants.SelectedIdct != IdctImplementation.AAN)
                            {
                                qTables[(int)(quantSpecs & 0x0f)] = new JpegQuantizationTable(quantData);
                            }
                            else
                            {
                                int[] zzQuantData = new int[64];
                                ZigZag.UnZigZag<int>(quantData, zzQuantData);
                                qTables[(int)(quantSpecs & 0x0f)] = new JpegQuantizationTable(zzQuantData);
                            }
                        }
                        break;

                    case JpegMarker.SOS:

                        // Debug.WriteLine("Start of Scan (SOS)");

                        // SOS non-SOF Marker - Start Of Scan Marker, this is where the
                        // actual data is stored in a interlaced or non-interlaced with
                        // from 1-4 components of color data, if three components most
                        // likely a YCrCb model, this is a fairly complex process.

                        // Read in the scan length.
                        ushort scanLen = jpegReader.ReadShort();
                        // Number of components in the scan.
                        byte numberOfComponents = jpegReader.ReadByte().Result;
                        byte[] componentSelector = new byte[numberOfComponents];

                        for (int i = 0; i < numberOfComponents; i++)
                        {
                            // Component ID, packed byte containing the Id for the
                            // AC table and DC table.
                            byte componentID = jpegReader.ReadByte().Result;
                            byte tableInfo = jpegReader.ReadByte().Result;

                            int DC = (tableInfo >> 4) & 0x0f;
                            int AC = (tableInfo) & 0x0f;

                            frame.SetHuffmanTables(componentID,
                                                   acTables[(byte)AC],
                                                   dcTables[(byte)DC]);

                            componentSelector[i] = componentID;
                        }

                        byte startSpectralSelection = jpegReader.ReadByte().Result;
                        byte endSpectralSelection = jpegReader.ReadByte().Result;
                        byte successiveApproximation = jpegReader.ReadByte().Result;

                        #region Baseline JPEG Scan Decoding

                        if (!progressive)
                        {
                            frame.DecodeScanBaseline(numberOfComponents, componentSelector, resetInterval, jpegReader, ref marker);
                            haveMarker = true; // use resultant marker for the next switch(..)
                        }

                        #endregion

                        #region Progressive JPEG Scan Decoding

                        if (progressive)
                        {
                            frame.DecodeScanProgressive(
                                successiveApproximation, startSpectralSelection, endSpectralSelection,
                                numberOfComponents, componentSelector, resetInterval, jpegReader, ref marker);

                            haveMarker = true; // use resultant marker for the next switch(..)
                        }

                        #endregion

                        break;


                    case JpegMarker.DRI:
                        jpegReader.BaseStream.Seek(2, System.IO.SeekOrigin.Current);
                        resetInterval = jpegReader.ReadShort();
                        break;

                    /// Defines the number of lines.  (Not usually present)
                    case JpegMarker.DNL:

                        frame.ScanLines = jpegReader.ReadShort();
                        break;

                    /// End of Image.  Finish the decode.
                    case JpegMarker.EOI:

                        if (jpegFrames.Count == 0)
                        {
                            throw new NotSupportedException("No JPEG frames could be located.");
                        }
                        else if (jpegFrames.Count == 1)
                        {
                            // Only one frame, JPEG Non-Hierarchical Frame.
                            byte[][][] raster = Image.CreateRasterBuffer(frame.Width, frame.Height, frame.ComponentCount);

                            IList<JpegComponent> components = frame.Scan.Components;

                            int totalSteps = components.Count * 3; // Three steps per loop

                            for (int i = 0; i < components.Count; i++)
                            {
                                JpegComponent comp = components[i];

                                comp.QuantizationTable = qTables[comp.quantId].Table;

                                // 1. Quantize
                                if (JpegConstants.SelectedIdct != IdctImplementation.AAN)
                                {
                                    comp.QuantizeData();
                                }

                                // 2. Run iDCT (expensive)
                                comp.IdctData();

                                // 3. Scale the image and write the data to the raster.
                                comp.WriteDataScaled(raster, i, BlockUpsamplingMode);

                                // Ensure garbage collection.
                                comp = null; GC.Collect();
                            }

                            // Grayscale Color Image (1 Component).
                            if (frame.ComponentCount == 1)
                            {
                                ColorModel cm = new ColorModel() { ColorSpace = ColorSpace.Gray, Opaque = true };
                                image = new Image(cm, raster);
                            }
                            // YCbCr Color Image (3 Components).
                            else if (frame.ComponentCount == 3)
                            {
                                ColorModel cm = new ColorModel() { ColorSpace = ColorSpace.YCbCr, Opaque = true };
                                image = new Image(cm, raster);
                            }
                            // Possibly CMYK or RGBA ?
                            else
                            {
                                throw new NotSupportedException("Unsupported Color Mode: 4 Component Color Mode found.");
                            }

                            // If needed, convert centimeters to inches.
                            Func<double, double> conv = x =>
                                Units == UnitType.Inches ? x : x / 2.54;

                            image.DensityX = conv(XDensity);
                            image.DensityY = conv(YDensity);

                            height = frame.Height;
                            width = frame.Width;
                        }
                        else
                        {
                            // JPEG Heirarchial Frame
                            throw new NotSupportedException("Unsupported Codec Type: Hierarchial JPEG");
                        }
                        break;

                    // Only SOF0 (baseline) and SOF2 (progressive) are supported by FJCore
                    case JpegMarker.SOF1:
                    case JpegMarker.SOF3:
                    case JpegMarker.SOF5:
                    case JpegMarker.SOF6:
                    case JpegMarker.SOF7:
                    case JpegMarker.SOF9:
                    case JpegMarker.SOF10:
                    case JpegMarker.SOF11:
                    case JpegMarker.SOF13:
                    case JpegMarker.SOF14:
                    case JpegMarker.SOF15:
                        throw new NotSupportedException("Unsupported codec type.");

                    default: break;  // ignore

                }

                #endregion switch over markers

                if (haveMarker) haveMarker = false;
                else
                {
                    var status = jpegReader.GetNextMarker();
                    if (status.Status == Status.EOF)
                    {
                        break; /* done reading the file */
                    }
                    else if (status.Status == Status.MarkerFound)
                    {
                        marker = status.Result;
                    }
                    else
                    {
                        // This should never happen.
                        throw new InvalidOperationException("No marker was found.");
                    }
                }
            }

            DecodedJpeg result = new DecodedJpeg(image, headers);

            return result;
        }

        private JpegHeader ExtractHeader()
        {
            #region Extract the header

            int length = jpegReader.ReadShort() - 2;
            byte[] data = new byte[length];
            jpegReader.Read(data, 0, length);

            #endregion

            JpegHeader header = new JpegHeader()
            {
                Marker = marker,
                Data = data
            };
            return header;
        }

    }
}
