using System;
using System.Collections.Generic;
using System.IO;
using Alanta.Client.Media.Jpeg;
using Alanta.Client.Media.Jpeg.Decoder;
using Alanta.Client.Media.Jpeg.Encoder;

namespace Alanta.Client.Media.VideoCodecs
{
    public class JpegVideoCodec : IVideoCodec
    {

        private short maxPacketSize;
        private short height;
        private short width;

        private Queue<MemoryStream> encodedStreams;
        private const int maxEncodedStreams = 5; // Only store 5 encoded frames.
        private ColorModel colorModel;
        private MemoryStream nullMemoryStream;
        private byte[] nullFrame;

        private const byte AlphaValue = 0xFF;
        private const int Y = 0;
        private const int Cb = 1;
        private const int Cr = 2;

        public JpegVideoCodec()
        {
            colorModel = new ColorModel();
            colorModel.ColorSpace = ColorSpace.YCbCr;
            colorModel.Opaque = true;
            encodedStreams = new Queue<MemoryStream>();
        }

        #region IVideoCodec Members

        public void Initialize(short height, short width, short maxPacketSize)
        {
            this.height = height;
            this.width = width;
            this.maxPacketSize = maxPacketSize;
            nullFrame = new byte[height * width * VideoConstants.VideoBytesPerPixel];
            nullMemoryStream = new MemoryStream(nullFrame);
        }

        public void EncodeFrame(byte[] frame)
        {
            if (encodedStreams.Count < maxEncodedStreams)
            {
                byte[][][] imageData = Image.CreateRaster((short)width, (short)height, 3);

                for (short y = (short)(height - 1), sy = 0; sy < height; sy++, y--)
                {
                    for (short x = 0; x < width; x++)
                    {
                        int pos = (((sy * width) + x) * VideoConstants.VideoBytesPerPixel);

                        imageData[Y][x][y] = frame[pos++];
                        imageData[Cb][x][y] = frame[pos++];
                        imageData[Cr][x][y] = frame[pos++];
                        toYCbCr(ref imageData[Y][x][y], ref imageData[Cb][x][y], ref imageData[Cr][x][y]);
                        // For YCbCr, we ignore the Alpha byte of the RGBA byte structure.
                    }
                }

                Alanta.Client.Media.Jpeg.Image image = new Alanta.Client.Media.Jpeg.Image(colorModel, imageData);
                var encodedStream = new MemoryStream();
                var encoder = new JpegEncoder(image, VideoConstants.JpegQuality, encodedStream);
                encoder.Encode();
                lock (encodedStreams)
                {
                    encodedStreams.Enqueue(encodedStream);
                }
            }
        }

        public List<ByteStream> GetEncodedChunks(IObjectPool<ByteStream> videoChunkPool)
        {
            throw new NotImplementedException();
        }

        public void DecodeChunk(ByteStream frame)
        {
            throw new NotImplementedException();
        }

        public MemoryStream GetNextFrame()
        {
            MemoryStream encodedStream;
            lock (encodedStreams)
            {
                if (encodedStreams.Count == 0)
                {
                    return nullMemoryStream;
                }
                encodedStream = encodedStreams.Dequeue();
            }

            // Make sure we've got real data.
            if (encodedStream.Length == 0)
            {
                return nullMemoryStream;
            }

            // Get the raster image.
            JpegDecoder decoder;
            DecodedJpeg decodedJpeg;
            encodedStream.Seek(0, SeekOrigin.Begin);
            decoder = new JpegDecoder(encodedStream);
            decodedJpeg = decoder.Decode();
            var imageData = decodedJpeg.Image.Raster;

            // Convert the three-layer raster image to RGBA format.
            byte[] frame = new byte[height * width * VideoConstants.VideoBytesPerPixel];
            int pos = 0;
            int posRed;
            int posGreen;
            int posBlue;
            for (short y = 0; y < height; y++)
            {
                for (short x = 0; x < width; x++)
                {
                    posRed = pos++;
                    posGreen = pos++;
                    posBlue = pos++;
                    frame[posRed] = imageData[Y][x][y];
                    frame[posGreen] = imageData[Cb][x][y];
                    frame[posBlue] = imageData[Cr][x][y];
                    toRGB(ref frame[posRed], ref frame[posGreen], ref frame[posBlue]);
                    frame[pos++] = AlphaValue;
                }
            }
            return new MemoryStream(frame);
        }

        public bool IsReceivingData
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Private Methods
        private static void toRGB(ref byte c1, ref byte c2, ref byte c3)
        {
            double dY = (double)c1;
            double dCb2 = (double)c2 - 128;
            double dCr2 = (double)c3 - 128;

            double dR = dY + 1.402 * dCr2;
            double dG = dY - 0.34414 * dCb2 - 0.71414 * dCr2;
            double dB = dY + 1.772 * dCb2;

            c1 = dR > 255 ? (byte)255 : dR < 0 ? (byte)0 : (byte)dR;
            c2 = dG > 255 ? (byte)255 : dG < 0 ? (byte)0 : (byte)dG;
            c3 = dB > 255 ? (byte)255 : dB < 0 ? (byte)0 : (byte)dB;
        }

        private static void toYCbCr(ref byte c1, ref byte c2, ref byte c3)
        {
            double dR = (double)c1;
            double dG = (double)c2;
            double dB = (double)c3;

            c1 = (byte)(0.299 * dR + 0.587 * dG + 0.114 * dB);
            c2 = (byte)(-0.16874 * dR - 0.33126 * dG + 0.5 * dB + 128);
            c3 = (byte)(0.5 * dR - 0.41869 * dG - 0.08131 * dB + 128);
        }
        #endregion

    }
}
