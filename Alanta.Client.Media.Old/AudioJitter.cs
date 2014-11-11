using System;
using Alanta.Client.Media.AudioCodecs;

namespace Alanta.Client.Media
{
    public class AudioJitter : IAudioJitter
    {
        private const int DefaultLatency = 10;
        private const int Buffers = 2 * DefaultLatency + 1;
        private const int MonoSoundBufferSize = MediaConstants.AudioSamplesPerSecond * (MediaConstants.AudioBitsPerSample / 8) * MediaConstants.AudioMillisecondsPerFrame / 1000; // 32000 * 20 / 1000;

        public class JitterBuffer
        {
            public bool Received;
            public ushort SequenceNumber;
            public int DataLength;
            public byte[] Samples;
        }

        private int offset;
        private bool recalculateOffset = true;
        private JitterBuffer nullBuffer;
        private JitterBuffer[] jitterBuffers = new JitterBuffer[Buffers];
        private AudioJitterLogger logger;
        private IAudioCodec audioCodec;

        public AudioJitter(IAudioCodec audioCodec)
        {
            logger = new AudioJitterLogger(this);
            nullBuffer = new JitterBuffer();
            nullBuffer.Samples = new byte[MonoSoundBufferSize];
            nullBuffer.Received = true;
            this.audioCodec = audioCodec;

            for (int i = 0; i < Buffers; i++)
            {
                jitterBuffers[i] = new JitterBuffer();
                jitterBuffers[i].Samples = new byte[MonoSoundBufferSize];
                jitterBuffers[i].Received = false;
            }
        }

        public void ReadSamples(ByteStream decodedBuffer, ushort readSequenceNumber, out ushort writeSequenceNumber)
        {
            int bufferNumber = readSequenceNumber % Buffers;
            JitterBuffer buffer = jitterBuffers[bufferNumber];
            logger.LogRead(buffer);
            if (buffer.Received)
            {
                buffer.Received = false;
                ByteStream encodedBuffer = new ByteStream(buffer.Samples, 0, buffer.DataLength);
                audioCodec.Decode(encodedBuffer, decodedBuffer);
                writeSequenceNumber = buffer.SequenceNumber;
            }
            else
            {
                audioCodec.Decode(null, decodedBuffer);
                writeSequenceNumber = 0;
            }
        }

        public void WriteSamples(byte[] samples, int start, int dataLength, ushort readSequenceNumber, ushort writeSequenceNumber)
        {
            logger.LogWrite(samples);
            int bufferIndex;
            if (GetJitterBufferOffset(readSequenceNumber, writeSequenceNumber, out bufferIndex))
            {
                JitterBuffer buffer = jitterBuffers[bufferIndex];
                Buffer.BlockCopy(samples, start, buffer.Samples, 0, dataLength);
                buffer.SequenceNumber = writeSequenceNumber;
                buffer.DataLength = dataLength;
                buffer.Received = true;
            }
        }

        public void WriteSamples(ByteStream samples, ushort readSequenceNumber, ushort writeSequenceNumber)
        {
            logger.LogWrite(samples.Data);
            int bufferIndex;
            if (GetJitterBufferOffset(readSequenceNumber, writeSequenceNumber, out bufferIndex))
            {
                JitterBuffer buffer = jitterBuffers[bufferIndex];
                buffer.DataLength = Math.Min(samples.DataLength, MonoSoundBufferSize);
                Buffer.BlockCopy(samples.Data, samples.DataOffset, buffer.Samples, 0, buffer.DataLength);
                buffer.SequenceNumber = writeSequenceNumber;
                buffer.Received = true;
            }
        }

        private bool GetJitterBufferOffset(ushort readSequenceNumber, ushort writeSequenceNumber, out int bufferOffset)
        {
            int diff = writeSequenceNumber - readSequenceNumber;

            // ks 5/11/10 - The code below removes the recalculation piece, but 
            // that seems to result in an odd echo when the sound is played.  Not sure why,
            // so I moved it back to the original, which includes a periodic offset recalculation.

            //if (recalculateOffset)
            //{
            //    recalculateOffset = false;
            //    offset = DefaultLatency - diff;
            //}

            //logger.LogBufferOffsetCalculation(offset, diff, Buffers);

            // If the packet isn't too new or too old, return a valid offset.
            //if (offset + diff > 0 && offset + diff < Buffers)
            //{
            //    bufferOffset = (writeSequenceNumber + offset) % Buffers;
            //    return true;
            //}
            //else
            //{
            //    bufferOffset = 0;
            //    return false;
            //}

            if (offset + diff < 0 || offset + diff >= Buffers)
            {
                recalculateOffset = true;
            }
            if (recalculateOffset == true)
            {
                recalculateOffset = false;
                offset = DefaultLatency - diff;
                logger.LogOffsetRecalculation(offset);
            }
            bufferOffset = (writeSequenceNumber + offset) % Buffers;
            return true;

            //if (recalculateOffset == true)
            //{
            //    // If this is the first packet read, we need to calculate the initial offset.
            //    recalculateOffset = false;
            //    offset = DefaultLatency - diff;
            //    logger.LogOffsetRecalculation(offset);
            //}
            //else if (offset + diff < 0)
            //{
            //    // If the packets are being ready too slowly, increment the pointer by one, see if we've got that one.
            //    offset++;
            //    return false;
            //}
            //else if (offset + diff >= Buffers)
            //{
            //    offset--;
            //}
            //logger.LogBufferOffsetCalculation(offset, diff, Buffers);
            //bufferOffset = (writeSequenceNumber + offset) % Buffers;

        }

    }
}
