using System;
using System.Collections.Generic;
using Alanta.Client.Media.AudioCodecs;
using System.Threading;

namespace Alanta.Client.Media
{
    #region Supporting Classes
    class JitterOffset
    {
        public bool calculateOffset;
        public int offset;
        public int delta;
    }

    class JitterFrame
    {
        public int encodedLength;
        public byte[] decodedSamples = new byte[MediaConstants.AudioBytesPerFrame];
        public byte[] encodedSamples = new byte[MediaConstants.AudioBytesPerFrame];
        public byte status;
        public ushort sequenceNumber;
    }

    class JitterBuffer
    {
        public JitterOffset jitterOffset = new JitterOffset();
        public List<JitterFrame> jitterFrames = new List<JitterFrame>();
    }

    class JitterResamplerDifference
    {
        public int adjustable;
        public int acumulated;
    }

    class JitterResamplerStatus
    {
        public object locker = new object();
        public bool startResampling;
        public int countRead;
        public int countWrite;
        public ushort previousReadSequenceNumber;
        public ushort previousWriteSequenceNumber;
        public JitterResamplerDifference difference =  new JitterResamplerDifference();
    }

    class JitterStatistics
    {
        int mReadsCount;
        int mWritesCount;
        int mWritesOutOfScopeCount;
        int mUpsamplesCount;
        int mDownsamplesCount;
        int mAccumulated;
        int mAdjustable;
        int mDelta;
        bool mOffsetCalculated;

        internal void logStatistics()
        {
            lock (this)
            {
                MediaLogger.LogDebugMessage("Reads={0}; Writes={1}; WritesOutOfScope={2}; Upsamples={3}; Downsamples={4}; Accumulate={5}; Adjustable={6}; Delta={7}; OffsetCalculated={8}",
                    mReadsCount, mWritesCount, mWritesOutOfScopeCount, mUpsamplesCount, mDownsamplesCount, mAccumulated, mAdjustable, mDelta, mOffsetCalculated);
                mReadsCount = 0;
                mWritesCount = 0;
                mWritesOutOfScopeCount = 0;
                mUpsamplesCount = 0;
                mDownsamplesCount = 0;
                mAccumulated = 0;
                mAdjustable = 0;
                mDelta = 0;
                mOffsetCalculated = false;
            }
        }

        internal void countReads()
        {
            lock (this)
            {
                mReadsCount++;
            }
            if (mReadsCount % 50 == 0)
            {
                logStatistics();
            }
        }

        internal void countWrites()
        {
            lock (this)
            {
                mWritesCount++;
            }
        }

        internal void countWritesOutOfScope()
        {
            lock (this)
            {
                mWritesOutOfScopeCount++;
            }
        }

        internal void setCalculateOffset()
        {
            lock (this)
            {
                mOffsetCalculated = true;
            }
        }

        internal void countUpsamples()
        {
            lock (this)
            {
                mUpsamplesCount++;
            }
        }

        internal void countDownsamples()
        {
            lock (this)
            {
                mDownsamplesCount++;
            }
        }

        internal void setAccumulated(int p)
        {
            lock (this)
            {
                mAccumulated = p;
            }
        }

        internal void setAdjustable(int p)
        {
            lock (this)
            {
                mAdjustable = p;
            }
        }

        internal void setDelta(int p)
        {
            lock (this)
            {
                mDelta = p;
            }
        }

        internal bool initialize()
        {
            return true;
        }
    }
    #endregion

    public class AudioJitter2 : IAudioJitter
    {
        #region Constructors
        public AudioJitter2(IAudioCodec audioCodec)
        {
            mDecoder = audioCodec;
            mJitterBuffer = new JitterBuffer();
            mJitterResamplerStatus = new JitterResamplerStatus();
            mJitterStatistics = new JitterStatistics();
            initialize();
        }
        #endregion

        #region Fields and Properties
        private const int DefaultLatency = 10;
        private const int Buffers = 2 * DefaultLatency + 1;
        private const int MonoSoundBufferSize = MediaConstants.AudioBytesPerFrame;
        private const int MONO_SOUND_BUFFER_SIZE = MediaConstants.AudioBytesPerFrame;
        private const int MeasureInterval = 50;
        private const int AdjustInterval = 10;

        private object mLocker = new object();
        private IAudioCodec mDecoder;
        private JitterBuffer mJitterBuffer;
        private JitterResamplerStatus mJitterResamplerStatus;
        private JitterStatistics mJitterStatistics;

        byte[] emptyBuffer = new byte[MonoSoundBufferSize];

        private const int JITTER_SEQUENCE_NUMBER_ROLLOVER_SIZE = 65536;
        private const int JITTER_SEQUENCE_NUMBER_OVERFLOW_EDGE = (JITTER_SEQUENCE_NUMBER_ROLLOVER_SIZE / 2);

        private byte JITTER_FRAME_RECEIVED = 0x01;
        private byte JITTER_FRAME_DECODED = 0x02;

        #endregion

        #region Public Methods

        private bool initialize()
        {
            for (int i = 0; i < Buffers; i++)
            {
                var jitterFrame = new JitterFrame();

                mJitterBuffer.jitterFrames.Add(jitterFrame);

                clearReceived(mJitterBuffer.jitterFrames.Count - 1);
                clearDecoded(mJitterBuffer.jitterFrames.Count - 1);
            }

            if (mDecoder == null)
            {
                MediaLogger.LogDebugMessage("Failed to instantiate decoder");
                return false;
            }

            if (mJitterStatistics.initialize() == false)
            {
                MediaLogger.LogDebugMessage("Failed to initialize statistics");
                return false;
            }

            mJitterBuffer.jitterOffset.calculateOffset = true;

            mJitterResamplerStatus.startResampling = false;
            mJitterResamplerStatus.countRead = 0;
            mJitterResamplerStatus.countWrite = 0;
            mJitterResamplerStatus.difference.acumulated = 0;
            mJitterResamplerStatus.difference.adjustable = 0;

            return true;
        }

        public void WriteSamples(byte[] encodedSamples, int start, int encodedLength, ushort readSequenceNumber, ushort writeSequenceNumber)
        {
            lock (mLocker)
            {
                mJitterResamplerStatus.startResampling = true;
                mJitterStatistics.countWrites();
                countWrite();
                int frameIndex = 0;
                if (getOffset(ref frameIndex, readSequenceNumber, writeSequenceNumber))
                {
                    mJitterBuffer.jitterFrames[frameIndex].encodedLength = encodedLength;
                    mJitterBuffer.jitterFrames[frameIndex].sequenceNumber = writeSequenceNumber;
                    Buffer.BlockCopy(encodedSamples, 0, mJitterBuffer.jitterFrames[frameIndex].encodedSamples, 0, (int)encodedLength);
                    // memcpy((void *)mJitterBuffer.jitterFrames[frameIndex].encodedSamples, (void *)encodedSamples, encodedLength);

                    setReceived(frameIndex);
                    clearDecoded(frameIndex);
                }
                else
                {
                    mJitterStatistics.countWritesOutOfScope();
                }
            }
        }

        public void WriteSamples(ByteStream encodedSamples, ushort readSequenceNumber, ushort writeSequenceNumber)
        {
            WriteSamples(encodedSamples.Data, encodedSamples.DataOffset, encodedSamples.DataLength, readSequenceNumber, writeSequenceNumber);
        }

        public void ReadSamples(ByteStream decodedSamples, ushort readSequenceNumber, out ushort writeSequenceNumber)
        {
            lock (mLocker)
            {
                mJitterStatistics.countReads();
                countRead();
                refreshResamplerStatus();

                int frameIndex = readSequenceNumber % mJitterBuffer.jitterFrames.Count;
                if (!mJitterBuffer.jitterOffset.calculateOffset)
                {
                    decodeFrame(frameIndex);
                    resample(frameIndex);
                    decodedSamples.TryWriteBytes(mJitterBuffer.jitterFrames[frameIndex].decodedSamples, 0, MonoSoundBufferSize);
                    // Buffer.BlockCopy(mJitterBuffer.jitterFrames[frameIndex].decodedSamples, 0, decodedSamples, 0, MONO_SOUND_BUFFER_SIZE);
                    // memcpy((void *) decodedSamples, (void *) mJitterBuffer.jitterFrames[frameIndex].decodedSamples, MONO_SOUND_BUFFER_SIZE * sizeof(short));
                }
                else
                {
                    decodedSamples.TryWriteBytes(emptyBuffer, 0, MonoSoundBufferSize);
                    // Buffer.BlockCopy(emptyBuffer, 0, decodedSamples, 0, emptyBuffer.Length);
                    // memset((void *) decodedSamples, 0, MONO_SOUND_BUFFER_SIZE * sizeof(short));
                }

                clearReceived(frameIndex);
                clearDecoded(frameIndex);
                writeSequenceNumber = mJitterBuffer.jitterFrames[frameIndex].sequenceNumber;
            }
        }

        #endregion

        #region Private Methods
        void decodeFrame(int frameIndex)
        {
            if (isReceived(frameIndex) == true)
            {
                if (isDecoded(frameIndex) == false)
                {
                    mJitterBuffer.jitterFrames[frameIndex].decodedSamples = mDecoder.Decode(mJitterBuffer.jitterFrames[frameIndex].encodedSamples, 0, mJitterBuffer.jitterFrames[frameIndex].encodedLength);
                }
            }
            else
            {
                mJitterBuffer.jitterFrames[frameIndex].decodedSamples = mDecoder.Decode(null, 0, 0);
            }
            setDecoded(frameIndex);
        }

        bool getOffset(ref int frameIndex, ushort readSequenceNumber, ushort writeSequenceNumber)
        {
            if (mJitterBuffer.jitterOffset.calculateOffset == false)
            {
                if (mJitterResamplerStatus.previousReadSequenceNumber - readSequenceNumber > JITTER_SEQUENCE_NUMBER_OVERFLOW_EDGE)
                {
                    mJitterBuffer.jitterOffset.offset -= JITTER_SEQUENCE_NUMBER_ROLLOVER_SIZE;
                }
                else if (readSequenceNumber - mJitterResamplerStatus.previousReadSequenceNumber > JITTER_SEQUENCE_NUMBER_OVERFLOW_EDGE)
                {
                    mJitterBuffer.jitterOffset.offset += JITTER_SEQUENCE_NUMBER_ROLLOVER_SIZE;
                }
                else if (mJitterResamplerStatus.previousWriteSequenceNumber - writeSequenceNumber > JITTER_SEQUENCE_NUMBER_OVERFLOW_EDGE)
                {
                    mJitterBuffer.jitterOffset.offset += JITTER_SEQUENCE_NUMBER_ROLLOVER_SIZE;
                }
                else if (writeSequenceNumber - mJitterResamplerStatus.previousWriteSequenceNumber > JITTER_SEQUENCE_NUMBER_OVERFLOW_EDGE)
                {
                    mJitterBuffer.jitterOffset.offset -= JITTER_SEQUENCE_NUMBER_ROLLOVER_SIZE;
                }

                mJitterResamplerStatus.previousReadSequenceNumber = readSequenceNumber;
                mJitterResamplerStatus.previousWriteSequenceNumber = writeSequenceNumber;
            }

            if (mJitterBuffer.jitterOffset.calculateOffset == true)
            {
                for (int i = 0; i < mJitterBuffer.jitterFrames.Count; i++)
                {
                    clearReceived(i);
                    clearDecoded(i);
                }

                mJitterResamplerStatus.countRead = 0;
                mJitterResamplerStatus.countWrite = 0;
                mJitterResamplerStatus.difference.adjustable = 0;
                mJitterResamplerStatus.difference.acumulated = 0;
                mJitterResamplerStatus.previousReadSequenceNumber = readSequenceNumber;
                mJitterResamplerStatus.previousWriteSequenceNumber = writeSequenceNumber;

                mJitterBuffer.jitterOffset.calculateOffset = false;
                mJitterBuffer.jitterOffset.offset = DefaultLatency - (writeSequenceNumber - readSequenceNumber);
                mJitterBuffer.jitterOffset.delta = 0;

                mJitterStatistics.setCalculateOffset();
            }

            frameIndex = (mJitterBuffer.jitterOffset.offset + writeSequenceNumber + mJitterBuffer.jitterOffset.delta) % mJitterBuffer.jitterFrames.Count;

            // Check to see if we've slipped outside our window.
            int position = mJitterBuffer.jitterOffset.offset + writeSequenceNumber - readSequenceNumber;
            if ((position < 0) || (position >= mJitterBuffer.jitterFrames.Count))
            {
                return false;
            }

            return true;
        }

        bool resample(int frameIndex)
        {
            if (mJitterResamplerStatus.startResampling == true)
            {
                if (mJitterResamplerStatus.countRead % AdjustInterval == 0)
                {
                    if (mJitterResamplerStatus.difference.adjustable > 0)
                    {
                        if (upsample(frameIndex) == false)
                        {
                            return false;
                        }
                    }
                    else if (mJitterResamplerStatus.difference.adjustable < 0)
                    {
                        if (downsample(frameIndex) == false)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        bool upsample(int frameIndex)
        {
            mJitterStatistics.countUpsamples();

            if (isDecoded(frameIndex) == true)
            {
                int destinyFrameIndex;
                int originFrameIndex;

                for (int i = 2; i < mJitterBuffer.jitterFrames.Count; i++)
                {
                    /* Add serverConfiguration.jitterConfiguration.size to make sure positionDestiny and positionOrigin are not negative */
                    destinyFrameIndex = (frameIndex - i + 1 + mJitterBuffer.jitterFrames.Count) % mJitterBuffer.jitterFrames.Count;
                    originFrameIndex = (frameIndex - i + mJitterBuffer.jitterFrames.Count) % mJitterBuffer.jitterFrames.Count;

                    if (isDecoded(originFrameIndex) == true)
                    {
                        copyReceived(destinyFrameIndex, originFrameIndex);
                        setDecoded(destinyFrameIndex);
                        Buffer.BlockCopy(mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples, 0, mJitterBuffer.jitterFrames[destinyFrameIndex].decodedSamples, 0, MonoSoundBufferSize);
                        // memcpy((void *)mJitterBuffer.jitterFrames[destinyFrameIndex].decodedSamples, (void *)mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples, MONO_SOUND_BUFFER_SIZE * sizeof(short));
                    }
                    if (isReceived(originFrameIndex) == true)
                    {
                        setReceived(destinyFrameIndex);
                        clearDecoded(destinyFrameIndex);

                        mJitterBuffer.jitterFrames[destinyFrameIndex].encodedLength = mJitterBuffer.jitterFrames[originFrameIndex].encodedLength;
                        Buffer.BlockCopy(mJitterBuffer.jitterFrames[originFrameIndex].encodedSamples, 0, mJitterBuffer.jitterFrames[destinyFrameIndex].encodedSamples, 0, MonoSoundBufferSize);
                        // memcpy((void *)mJitterBuffer.jitterFrames[destinyFrameIndex].encodedSamples, (void *)mJitterBuffer.jitterFrames[originFrameIndex].encodedSamples, MONO_SOUND_BUFFER_SIZE * sizeof(short));
                    }
                    else
                    {
                        clearReceived(destinyFrameIndex);
                        clearDecoded(destinyFrameIndex);
                    }
                }

                destinyFrameIndex = (frameIndex + 1) % mJitterBuffer.jitterFrames.Count;
                originFrameIndex = (frameIndex) % mJitterBuffer.jitterFrames.Count;

                setDecoded(destinyFrameIndex);

                int destinySampleIndex = MONO_SOUND_BUFFER_SIZE;
                int originSampleIndex = MONO_SOUND_BUFFER_SIZE;

                for (int i = 0; i < MONO_SOUND_BUFFER_SIZE / 2; i++)
                {
                    mJitterBuffer.jitterFrames[destinyFrameIndex].decodedSamples[--destinySampleIndex] = mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples[--originSampleIndex];
                    mJitterBuffer.jitterFrames[destinyFrameIndex].decodedSamples[--destinySampleIndex] = mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples[originSampleIndex];
                }

                destinySampleIndex = MONO_SOUND_BUFFER_SIZE;

                for (int i = 0; i < MONO_SOUND_BUFFER_SIZE / 2; i++)
                {
                    mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples[--destinySampleIndex] = mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples[--originSampleIndex];
                    mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples[--destinySampleIndex] = mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples[originSampleIndex];
                }

                mJitterBuffer.jitterOffset.delta++;

                mJitterResamplerStatus.difference.adjustable--;
                mJitterResamplerStatus.difference.acumulated--;
            }
            else
            {
                MediaLogger.LogDebugMessage("Unable to upsample because entry is not decoded");
            }

            return true;
        }

        bool downsample(int frameIndex)
        {
            mJitterStatistics.countDownsamples();

            int firstFrameIndex = (frameIndex) % mJitterBuffer.jitterFrames.Count;
            int secondFrameIndex = (frameIndex + 1) % mJitterBuffer.jitterFrames.Count;

            if (isDecoded(firstFrameIndex))
            {
                if (!isDecoded(secondFrameIndex))
                {
                    mJitterBuffer.jitterFrames[secondFrameIndex].decodedSamples = mDecoder.Decode(null, 0, 0);
                    setDecoded(secondFrameIndex);
                }

                int sampleIndex = 0;

                for (; sampleIndex < MONO_SOUND_BUFFER_SIZE / 2; sampleIndex++)
                {
                    mJitterBuffer.jitterFrames[firstFrameIndex].decodedSamples[sampleIndex] = mJitterBuffer.jitterFrames[firstFrameIndex].decodedSamples[2 * sampleIndex];
                }

                for (; sampleIndex < MONO_SOUND_BUFFER_SIZE; sampleIndex++)
                {
                    mJitterBuffer.jitterFrames[firstFrameIndex].decodedSamples[sampleIndex] = mJitterBuffer.jitterFrames[secondFrameIndex].decodedSamples[2 * sampleIndex - MONO_SOUND_BUFFER_SIZE];
                }

                int destinyFrameIndex = (frameIndex + 1) % mJitterBuffer.jitterFrames.Count;
                int originFrameIndex = (frameIndex + 2) % mJitterBuffer.jitterFrames.Count;

                for (uint i = 2; i < mJitterBuffer.jitterFrames.Count; i++)
                {
                    if (isDecoded(originFrameIndex) == true)
                    {
                        copyReceived(destinyFrameIndex, originFrameIndex);
                        setDecoded(destinyFrameIndex);
                        Buffer.BlockCopy(mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples, 0, mJitterBuffer.jitterFrames[destinyFrameIndex].decodedSamples, 0, MonoSoundBufferSize);
                        // memcpy((void*)mJitterBuffer.jitterFrames[destinyFrameIndex].decodedSamples, (void*)mJitterBuffer.jitterFrames[originFrameIndex].decodedSamples, MONO_SOUND_BUFFER_SIZE * sizeof(short));
                    }
                    else if (isReceived(originFrameIndex) == true)
                    {
                        setReceived(destinyFrameIndex);
                        clearDecoded(destinyFrameIndex);

                        mJitterBuffer.jitterFrames[destinyFrameIndex].encodedLength = mJitterBuffer.jitterFrames[originFrameIndex].encodedLength;
                        Buffer.BlockCopy(mJitterBuffer.jitterFrames[originFrameIndex].encodedSamples, 0, mJitterBuffer.jitterFrames[destinyFrameIndex].encodedSamples, 0, MonoSoundBufferSize);
                        // memcpy((void*)mJitterBuffer.jitterFrames[destinyFrameIndex].encodedSamples, (void*)mJitterBuffer.jitterFrames[originFrameIndex].encodedSamples, MONO_SOUND_BUFFER_SIZE * sizeof(short));
                    }
                    else
                    {
                        clearReceived(destinyFrameIndex);
                        clearDecoded(destinyFrameIndex);
                    }

                    destinyFrameIndex = (destinyFrameIndex + 1) % mJitterBuffer.jitterFrames.Count;
                    originFrameIndex = (originFrameIndex + 1) % mJitterBuffer.jitterFrames.Count;
                }

                mJitterBuffer.jitterOffset.delta--;

                mJitterResamplerStatus.difference.adjustable++;
                mJitterResamplerStatus.difference.acumulated++;
            }
            else
            {
                MediaLogger.LogDebugMessage("Unable to upsample because entry is not decoded");
            }

            return true;
        }

        void countRead()
        {
            lock (mJitterResamplerStatus.locker)
            {
                mJitterResamplerStatus.countRead++;
            }
        }

        void countWrite()
        {
            lock (mJitterResamplerStatus.locker)
            {
                mJitterResamplerStatus.countWrite++;
            }
        }

        void refreshResamplerStatus()
        {
            lock (mJitterResamplerStatus.locker)
            {

                if (mJitterResamplerStatus.countRead == MeasureInterval)
                {
                    int currentDifference = mJitterResamplerStatus.countRead - mJitterResamplerStatus.countWrite;

                    if (((currentDifference < 0) && (mJitterResamplerStatus.difference.acumulated < 0))
                    || ((currentDifference > 0) && (mJitterResamplerStatus.difference.acumulated > 0)))
                    {
                        mJitterResamplerStatus.difference.adjustable = mJitterResamplerStatus.difference.acumulated;
                    }
                    else
                    {
                        mJitterResamplerStatus.difference.adjustable = 0;
                    }

                    mJitterResamplerStatus.difference.acumulated += currentDifference;

                    mJitterResamplerStatus.countRead = 0;
                    mJitterResamplerStatus.countWrite = 0;

                    mJitterStatistics.setAccumulated(mJitterResamplerStatus.difference.acumulated);
                    mJitterStatistics.setAdjustable(mJitterResamplerStatus.difference.adjustable);
                    mJitterStatistics.setDelta(mJitterBuffer.jitterOffset.delta);
                }

                if (Math.Abs(mJitterResamplerStatus.difference.adjustable) > DefaultLatency)
                {
                    mJitterBuffer.jitterOffset.calculateOffset = true;
                }
            }
        }

        bool isReceived(int frameIndex)
        {
            if ((mJitterBuffer.jitterFrames[frameIndex].status & JITTER_FRAME_RECEIVED) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void setReceived(int frameIndex)
        {
            mJitterBuffer.jitterFrames[frameIndex].status |= JITTER_FRAME_RECEIVED;
        }

        void clearReceived(int frameIndex)
        {
            mJitterBuffer.jitterFrames[frameIndex].status &= (byte)(0xFF - JITTER_FRAME_RECEIVED);
        }

        void copyReceived(int frameIndex, int originFrameIndex)
        {
            if (isReceived(originFrameIndex))
            {
                setReceived(frameIndex);
            }
            else
            {
                clearReceived(frameIndex);
            }
        }

        bool isDecoded(int frameIndex)
        {
            return (mJitterBuffer.jitterFrames[frameIndex].status & JITTER_FRAME_DECODED) > 0;
        }

        void setDecoded(int frameIndex)
        {
            mJitterBuffer.jitterFrames[frameIndex].status |= JITTER_FRAME_DECODED;
        }

        void clearDecoded(int frameIndex)
        {
            mJitterBuffer.jitterFrames[frameIndex].status &= (byte)(0xFF - JITTER_FRAME_DECODED);
        }

        void copyDecoded(int frameIndex, int originFrameIndex)
        {
            if (isReceived(originFrameIndex))
            {
                setReceived(frameIndex);
            }
            else
            {
                clearReceived(frameIndex);
            }
        }
        #endregion

    }
}
