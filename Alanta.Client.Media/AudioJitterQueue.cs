using System;
using System.Collections.Generic;
using Alanta.Client.Common;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;

namespace Alanta.Client.Media
{
	/// <summary>
	/// This jitter queue assumes that all packets arrive in the correct order, which simplifies things pretty substantially. This only really works for TCP, of course.
	/// </summary>
	public class AudioJitterQueue : IAudioJitter
	{

		#region Constructors
		public AudioJitterQueue(ICodecFactory codecFactory, IVideoQualityController videoQualityController, MediaStatistics mediaStatistics = null)
		{
			_codecFactory = codecFactory;
			AudioDecoder = codecFactory.GetAudioDecoder(AudioCodecType.Speex); // Default to Speex.
			_videoQualityController = videoQualityController;
			_queue = new PriorityQueue<AudioJitterQueueEntry>();
			_logger = new AudioJitterQueueLogger(mediaStatistics);
			_entryPool = new ObjectPool<AudioJitterQueueEntry>(() => new AudioJitterQueueEntry());
			SetDefaults();
		}

		private void SetDefaults()
		{
			_framesSinceLastCheck = 0;
			_framesBetweenChecks = 50;
			_framesLostSinceLastCheck = 0;
			_firstPacketReceived = false;
		}
		#endregion

		#region Fields and Properties

		private readonly ICodecFactory _codecFactory;

		private long _codecSets;
		public event EventHandler<EventArgs<AudioCodecType>> CodecTypeChanged;
		private IAudioDecoder _audioDecoder;
		public IAudioDecoder AudioDecoder
		{
			get { return _audioDecoder; }
			set
			{
				if (value == _audioDecoder && ++_codecSets % 200 != 0) return;
				_audioDecoder = value;
				if (CodecTypeChanged != null)
				{
					CodecTypeChanged(this, new EventArgs<AudioCodecType>(_audioDecoder.CodecType));
				}
			}
		}

		private readonly IVideoQualityController _videoQualityController;
		private readonly PriorityQueue<AudioJitterQueueEntry> _queue;
		private readonly IObjectPool<AudioJitterQueueEntry> _entryPool;
		private ushort _lastSequenceNumberRead;
		private const ushort lowEndWrapAround = ushort.MinValue + 50;
		private const ushort highEndWrapAround = ushort.MaxValue - 50;

		private readonly List<int> _queueSizes = new List<int>();

		private const int maxQueueSizeEntries = 20;

		/// <summary>
		/// The number of reads in-between buffer readjustments.
		/// </summary>
		private int _framesSinceLastCheck;

		/// <summary>
		/// The number of reads which went unfulfilled since the last request.
		/// </summary>
		private int _framesLostSinceLastCheck;

		/// <summary>
		/// How often to check to see whether we can reduce the queue size. This will be adjusted up or down based on network conditions.
		/// </summary>
		private int _framesBetweenChecks;

		/// <summary>
		/// A flag which records whether the first packet has been received.  Latency tuning only takes effect after the first packet has been received.
		/// </summary>
		private bool _firstPacketReceived;

		/// <summary>
		/// The minimum targeted number of frames in the queue.
		/// </summary>
		private const int queuedFramesTargetMin = 5;

		/// <summary>
		/// The maximum number of frames in the queue.
		/// </summary>
		private const int queuedFramesTargetMax = 50;

		/// <summary>
		/// The lowest possible value of framesBetweenChecks.
		/// </summary>
		private const int framesBetweenChecksMin = 50;

		/// <summary>
		/// The largest possible value of framesBetweenChecks.
		/// </summary>
		private const int framesBetweenChecksMax = 200;

		/// <summary>
		/// How much to slow down framesBetweenChecks when network problems are detected.
		/// </summary>
		private const int badTrafficAdjustment = 10;

		/// <summary>
		/// How much to speed up framesBetweenChecks when no network problems are detected.
		/// </summary>
		private const int goodTrafficAdjustment = 3;

		private readonly AudioJitterQueueLogger _logger;

		// private double _doubleReadCounter;
		// private int _doubleReadFloor;

		#endregion

		#region Methods

		/// <returns>Length *in shorts* of the decoded data</returns>
		public int ReadSamples(short[] outputBuffer)
		{
			int length = 0;
			bool isSilent = false;
			lock (_queue)
			{
				// If there are too many frames in the queue, pull them out one by one and decode them (so the speex buffer stays OK),
				// but don't bother playing them.  This is a case where downsampling would be helpful, but we'll ignore it for now.
				while (_queue.Count > queuedFramesTargetMax)
				{
					_logger.LogQueueFull();
					var entry = _queue.Dequeue();
					AudioDecoder = _codecFactory.GetAudioDecoder(entry.AudioCodecType);
					AudioDecoder.Decode(entry.Frame, 0, entry.DataLength, outputBuffer, length, entry.IsSilent);
					_entryPool.Recycle(entry);
					_videoQualityController.LogGlitch(1);
				}

				// If we haven't lost any frames since the last check, pull one frame out of the queue to reduce latency.
				// This is a case where downsampling would be helpful, but we'll ignore it for now.
				if (++_framesSinceLastCheck > _framesBetweenChecks && _firstPacketReceived)
				{
					// Keep a record of the queue size, so that we can know how "bad" it is when we miss a packet.
					// It's not a big deal to miss a read when the queue size is stable at < 4 frames,
					// but it's a pretty big deal when the queue size is jumping around between 0 and 50.
					while (_queueSizes.Count > maxQueueSizeEntries)
					{
						_queueSizes.RemoveAt(0);
					}
					_queueSizes.Add(_queue.Count);

					if (_framesLostSinceLastCheck == 0 && _queue.Count > queuedFramesTargetMin)
					{
						var entry = _queue.Dequeue();
						AudioDecoder = _codecFactory.GetAudioDecoder(entry.AudioCodecType);
						AudioDecoder.Decode(entry.Frame, 0, entry.DataLength, outputBuffer, length, entry.IsSilent);
						_entryPool.Recycle(entry);

						if (_framesBetweenChecks > framesBetweenChecksMin)
						{
							_framesBetweenChecks -= goodTrafficAdjustment; // Speed up (slightly) the rate at which we can decrease the queue size.
						}
						_logger.LogQueueReduced();
					}
					_framesLostSinceLastCheck = 0;
					_framesSinceLastCheck = 0;
				}

				// Calculate the number of packets we should retrieve.
				// Here's the logic. Let's say that we're only reading packets every 23.3 milliseconds instead of every 20 milliseconds.
				// This means that for about 3.3/20 = 16.5% of the reads, we actually need to request *two* packets.
				// So each time we read, we add .165 to a counter, and then take its floor. As soon as the counter floor
				// rolls over to a new integer, we know that we need to read a second packet.
				// Unfortunately, dammit, it doesn't look like this works. We'll need to create a better approach,
				// presumably using a resampler.

				//_doubleReadCounter += _logger.OverageRatio;
				//int packetsToRead = 1;
				//var newDoubleReadFloor = (int)Math.Floor(_doubleReadCounter);
				//if (newDoubleReadFloor > _doubleReadFloor)
				//{
				//    packetsToRead += newDoubleReadFloor - _doubleReadFloor;
				//    _doubleReadFloor = newDoubleReadFloor;
				//    _logger.LogMultipleRead();
				//}

				//for (int i = 0; i < packetsToRead; i++)
				{
					if (_queue.Count > 0)
					{
						// If we have anything in the queue, fulfill the request.
						var entry = _queue.Dequeue();
						isSilent = entry.IsSilent;
						_lastSequenceNumberRead = entry.SequenceNumber;
						AudioDecoder = _codecFactory.GetAudioDecoder(entry.AudioCodecType);

						length += AudioDecoder.Decode(entry.Frame, 0, entry.DataLength, outputBuffer, length, entry.IsSilent);
						_entryPool.Recycle(entry);
						_firstPacketReceived = true;
					}
					else
					{
						// Record the fact that we missed a read, so the rest of the system can adjust.
						_logger.LogQueueEmpty();
						if (_firstPacketReceived)
						{
							double stdDev = DspHelper.GetStandardDeviation(_queueSizes);
							_videoQualityController.LogGlitch((int)Math.Floor(stdDev) + 1);
						}

						// If the frame hasn't arrived yet, let the last audio codec interpolate the missing packet.
						// Most likely, the additional frames will arrive in a bunch by the time the next read happens.
						// We may want to investigate our own upsampling algorithm at some point.
						length += AudioDecoder.Decode(null, 0, 0, outputBuffer, length, true);
						if (_framesBetweenChecks < framesBetweenChecksMax && _firstPacketReceived)
						{
							_framesBetweenChecks += badTrafficAdjustment; // Slow down (substantially) the rate at which we decrease the queue size.
						}
					}
				}
			}
			_logger.LogRead(_queue, _framesBetweenChecks, isSilent);
			return length;
		}

		public void WriteSamples(ByteStream samples, ushort sequenceNumber, AudioCodecType audioCodecType, bool isSilent)
		{
			WriteSamples(samples.Data, samples.DataOffset, samples.DataLength, sequenceNumber, audioCodecType, isSilent);
		}

		public void WriteSamples(Array samples, int start, int dataLength, ushort sequenceNumber, AudioCodecType audioCodecType, bool isSilent)
		{
			// Only write the frame to the queue if it's newer than the most recently played frame.
			// This particular way of checking will miss some out-of-order packets if they occur right when a wrap-around
			// is happening, but not many.

			if (sequenceNumber > _lastSequenceNumberRead ||
				_lastSequenceNumberRead < lowEndWrapAround ||
				_lastSequenceNumberRead > highEndWrapAround ||
				sequenceNumber < lowEndWrapAround ||
				sequenceNumber > highEndWrapAround)
			{
				var entry = _entryPool.GetNext();
				Buffer.BlockCopy(samples, start, entry.Frame, 0, dataLength);
				entry.DataLength = dataLength / sizeof(short);
				entry.SequenceNumber = sequenceNumber;
				entry.AudioCodecType = audioCodecType;
				entry.IsSilent = isSilent;
				lock (_queue)
				{
					_queue.Enqueue(entry);
				}
				_logger.LogWrite();
			}
			else
			{
				_logger.LogWriteOutOfOrder();
			}
		}

		public void Reset()
		{
			_queue.Clear();
			SetDefaults();
		}

		#endregion
	}
}
