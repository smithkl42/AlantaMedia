using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media.AudioCodecs;
using Alanta.Client.Media.Dsp;
using Alanta.Client.Media.VideoCodecs;
using ReactiveUI;

namespace Alanta.Client.Media
{
	/// <summary>
	/// Acts as a central point of coordination for all the various pieces of our little media world.
	/// </summary>
	/// <remarks>
	/// ks 4/6/12 - This class really needs to be refactored. It has way too much logic in it, and is used for way too many things. One of these days...
	/// </remarks>
	public class MediaController : ReactiveObject, IAudioController, IVideoController, IDisposable
	{
		#region Fields and Properties
		public MediaConfig MediaConfig { get; private set; }
		public IMediaConnection MediaConnection { get; private set; }

		protected AudioJitterQueue _audioJitter;
		protected IAudioEncoder _debugAudioEncoder;
		public Dictionary<ushort, VideoThreadData> RemoteSessions { get; private set; }
		protected IVideoCodec _videoEncoder;
		protected string _roomId;
		protected bool _isActive = true;
		protected IMediaEnvironment _mediaEnvironment;


		private bool _audioSentSuccessfully;
		protected bool AudioSentSuccessfully
		{
			get { return _audioSentSuccessfully; }
			set
			{
				_audioSentSuccessfully = value;
				RaiseConversationOccurredIfNecessary();
			}
		}

		private bool _audioReceivedSuccessfully;
		protected bool AudioReceivedSuccessfully
		{
			get { return _audioReceivedSuccessfully; }
			set
			{
				_audioReceivedSuccessfully = value;
				RaiseConversationOccurredIfNecessary();
			}
		}

		protected bool _conversationOccurredRaised;
		public event EventHandler ConversationOccurred;
		private void RaiseConversationOccurredIfNecessary()
		{
			if (AudioReceivedSuccessfully && AudioSentSuccessfully && !_conversationOccurredRaised && ConversationOccurred != null)
			{
				_conversationOccurredRaised = true;
				ConversationOccurred(this, EventArgs.Empty);
			}
		}
		public IVideoQualityController VideoQualityController { get; private set; }

		public MediaControllerLogger Logger { get; protected set; }

		// Various buffers to hold data at appropriate stages of processing.
		protected byte[] _audioDecodeBuffer;
		private readonly byte[] _silentBytes;
		private readonly short[] _decodedFrameBuffer;
		protected ByteStream _audioSendBuffer;

		public AudioFormat PlayedAudioFormat { get; set; }

		protected byte[] _lastRawVideoInput; // stores the last webcam sample for the video encoding thread.
		protected Dictionary<ushort, MemoryStream> _lastRawVideoOutput = new Dictionary<ushort, MemoryStream>(); // stores the last webcam sample sent to the media player.

		// ks 4/8/10 - These pool objects are intended to reduce the time spent in garbage collection.
		// Testing has shown that for reasonably complex objects or large buffers (>512 bytes), 
		// it's faster to re-use the objects/buffers than to recreate them on each pass.
		protected ObjectPool<ByteStream> _videoBufferPool;
		protected ObjectPool<Chunk> _videoChunkPool;
		protected ObjectPool<ByteStream> _packetBufferPool;

		protected Thread _audioEncodeThread;
		protected ManualResetEvent _audioEncodeResetEvent;
		protected Thread _videoTransmitThread;
		protected ManualResetEvent _videoEncodeResetEvent;
		protected Queue<AudioFrame> _audioFrames = new Queue<AudioFrame>();

		public event EventHandler<ExceptionEventArgs> ErrorOccurred;

		public ObservableCollection<AudioStatistics> AudioStats { get; private set; }
		private readonly AudioStatistics _speakerStatistics;
		private readonly AudioStatistics _microphoneStatistics;
		private readonly AudioStatistics _cancelledStatistics;
		public MediaStatistics MediaStats { get; private set; }

		private byte[] _lastVideoFrame;
		private int _lastStride;

		protected ICodecFactory _codecFactory;
		public ICodecFactory CodecFactory
		{
			get { return _codecFactory; }
		}

		public virtual bool IsConnected
		{
			get { return MediaConnection.IsConnected; }
		}

		private bool _isMicrophoneMuted;
		public bool IsMicrophoneMuted
		{
			get { return _isMicrophoneMuted; }
			set { this.RaiseAndSetIfChanged(x => x.IsMicrophoneMuted, ref _isMicrophoneMuted, value); }
		}

		private bool _isSpeakerMuted;
		public bool IsSpeakerMuted
		{
			get { return _isSpeakerMuted; }
			set { this.RaiseAndSetIfChanged(x => x.IsSpeakerMuted, ref _isSpeakerMuted, value); }
		}

		private bool _isRemoteVideoMuted;
		public bool IsRemoteVideoMuted
		{
			get { return _isRemoteVideoMuted; }
			set { this.RaiseAndSetIfChanged(x => x.IsRemoteVideoMuted, ref _isRemoteVideoMuted, value); }
		}

		private bool _isLocalWebcamMuted;
		public bool IsLocalWebcamMuted
		{
			get { return _isLocalWebcamMuted; }
			set { this.RaiseAndSetIfChanged(x => x.IsLocalWebcamMuted, ref _isLocalWebcamMuted, value); }
		}

		public bool IsConnecting
		{
			get { return MediaConnection.IsConnected; }
		}

		protected ushort _localSsrcId;
		public virtual ushort LocalSsrcId
		{
			get { return _localSsrcId; }
		}

		private const double defaultVolume = 0.15d;
		private double _microphoneVolume = defaultVolume;
		public double MicrophoneVolume
		{
			get { return _microphoneVolume; }
			set { _microphoneVolume = NormalizeVolume(value); }
		}
		private double _speakerVolume = defaultVolume * 2;  // Give the speaker some boost, as it seems to be a little quiet out the gate.
		public double SpeakerVolume
		{
			get { return _speakerVolume; }
			set { _speakerVolume = NormalizeVolume(value); }
		}

		private AudioCodecType _lastAudioEncoder;
		public AudioCodecType LastAudioEncoder
		{
			get { return _lastAudioEncoder; }
			private set
			{
				if (_lastAudioEncoder != value)
				{
					Deployment.Current.Dispatcher.BeginInvoke((() => this.RaiseAndSetIfChanged(x => x.LastAudioEncoder, ref _lastAudioEncoder, value)));
				}
			}
		}

		private AudioCodecType _lastAudioDecoder;
		public AudioCodecType LastAudioDecoder
		{
			get { return _lastAudioDecoder; }
			private set
			{
				if (_lastAudioDecoder != value)
				{
					Deployment.Current.Dispatcher.BeginInvoke(() => this.RaiseAndSetIfChanged(x => x.LastAudioDecoder, ref _lastAudioDecoder, value));
				}
			}
		}

		private string _lastSpeechEnhancementName;
		public string LastSpeechEnhancementName
		{
			get { return _lastSpeechEnhancementName; }
			private set
			{
				if (_lastSpeechEnhancementName != value)
				{
					Deployment.Current.Dispatcher.BeginInvoke(() => this.RaiseAndSetIfChanged(x => x.LastSpeechEnhancementName, ref _lastSpeechEnhancementName, value));
				}
			}
		}

		private IAudioTwoWayFilter _lastSpeechEnhancementStack;


		#endregion

		#region Events

		public event EventHandler<RemoteCameraChangedEventArgs> RemoteCameraChanged;

		void audioJitter_CodecTypeChanged(object sender, EventArgs<AudioCodecType> e)
		{
			LastAudioDecoder = e.Value;
		}

		#endregion

		#region Constructors
		public MediaController(MediaConfig config, AudioFormat playedAudioFormat, MediaStatistics mediaStats, IMediaEnvironment mediaEnvironment, IMediaConnection mediaConnection, IVideoQualityController videoQualityController)
		{
			// Initialize the class variables.
			_mediaEnvironment = mediaEnvironment;
			MediaConfig = config;
			MediaStats = mediaStats;
			MediaConnection = mediaConnection;
			VideoQualityController = videoQualityController;
			MediaConnection.AudioPacketHandler = HandleAudioPacket;
			MediaConnection.VideoPacketHandler = HandleVideoPacket;

			Logger = new MediaControllerLogger(VideoQualityController, MediaStats);
			_localSsrcId = config.LocalSsrcId;
			RemoteSessions = new Dictionary<ushort, VideoThreadData>();
			VideoQualityController.RemoteSessions = RemoteSessions;
			PlayedAudioFormat = playedAudioFormat;

			_silentBytes = new byte[PlayedAudioFormat.BytesPerFrame];
			_decodedFrameBuffer = new short[PlayedAudioFormat.SamplesPerFrame * 10]; // Make room for 10 frames.

			_codecFactory = config.CodecFactory;
			_videoEncoder = _codecFactory.GetVideoEncoder(VideoQualityController, MediaStats);

			// Instantiate the audio jitter class
			_audioJitter = new AudioJitterQueue(_codecFactory, VideoQualityController, MediaStats);
			_audioJitter.CodecTypeChanged += audioJitter_CodecTypeChanged;

			_audioDecodeBuffer = new byte[VideoConstants.MaxPayloadSize];
			_audioSendBuffer = new ByteStream(RtpPacketData.DataPacketMaxLength);

			// Spin up the various audio and video encoding threads.
			// On multiprocessor machines, these can spread the load, but even on single-processor machines it helps a great deal 
			// if the various audio and video sinks can return immediately.
			_audioEncodeResetEvent = new ManualResetEvent(false);
			_audioEncodeThread = new Thread(TransmitAudio);
			_audioEncodeThread.Name = "MediaController.TransmitAudio";
			_audioEncodeThread.Start();
			_videoEncodeResetEvent = new ManualResetEvent(false);
			_videoTransmitThread = new Thread(TransmitVideo);
			_videoTransmitThread.Name = "MediaController.TransmitVideo";
			_videoTransmitThread.Start();

			// Create the object pools that will help us reduce time spent in garbage collection.
			_videoBufferPool = new ObjectPool<ByteStream>(() => new ByteStream(VideoConstants.MaxPayloadSize * 2), bs => bs.Reset());
			_packetBufferPool = new ObjectPool<ByteStream>(() => new ByteStream(RtpPacketData.DataPacketMaxLength), bs => bs.Reset());
			_videoChunkPool = new ObjectPool<Chunk>(() => new Chunk { Payload = new ByteStream(VideoConstants.MaxPayloadSize * 2) }, chunk => { chunk.SsrcId = 0; chunk.Payload.Reset(); });

			AudioStats = new ObservableCollection<AudioStatistics>();

			_speakerStatistics = new AudioStatistics("Volume:Sent to Speaker", MediaStats);
			_microphoneStatistics = new AudioStatistics("Volume:Received from Microphone", MediaStats);
			_cancelledStatistics = new AudioStatistics("Volume:Echo Cancelled", MediaStats);

			AudioStats.Add(_speakerStatistics);
			AudioStats.Add(_microphoneStatistics);
			AudioStats.Add(_cancelledStatistics);
		}

		#endregion

		#region Connect Methods

		/// <summary>
		/// Connects to the media server.
		/// </summary>
		/// <param name="roomId">The room on the media server</param>
		/// <param name="callback">An optional callback to be called when connection is finished.</param>
		/// <remarks>
		/// The connection process is complicated, due to the fact that we have to first connect to the control port (4521), 
		/// and then if that is successful, we then connect to the data port (4522).  And of course, both connection attempts are
		/// asynchronous. The result is that the logic in a successful connection attempt flows like this:
		///   Connect() -> controlClient.Connect() =>
		///   HandleControlConnect() -> RegisterClientOnServer() -> controlClient.Send() =>
		///   HandleControlData() -> rtpClient.Connect() =>
		///   HandleRtpConnect() -> rtpClient.Send() =>
		///   HandleRtpData() -> FinalizeConnection() -> connectionCallback()
		/// An error anywhere in that flow will result in control being transferred to FinalizeConnection().
		/// </remarks>
		public virtual void Connect(string roomId, Action<Exception> callback = null)
		{
			if (_isActive )
			{
				MediaConnection.Connect(roomId, callback);
				_roomId = roomId;
			}
			else
			{
				if (callback != null)
				{
					callback(null);
				}
			}
		}

		public virtual void ClearRemoteSessions()
		{
			var sessionKeys = RemoteSessions.Keys.ToList();
			foreach (var key in sessionKeys)
			{
				UnregisterRemoteSession(key);
			}
		}

		/// <summary>
		/// Registers a media stream source listener for a given ssrcId.
		/// </summary>
		/// <param name="ssrcId">The ssrcId of the media stream source for which it should be listening.</param>
		/// <remarks>
		/// This method is called when the main client is notified (via the web service) that a new user has joined the room.
		/// This method tells the media controller to be prepared for video data tagged for the specified SsrcId 
		/// to start coming down the pipe.
		/// </remarks>
		public virtual void RegisterRemoteSession(ushort ssrcId)
		{
			VideoThreadData videoThreadData;
			lock (RemoteSessions)
			{
				if (RemoteSessions.ContainsKey(ssrcId))
				{
					ClientLogger.Debug("A remote session for ssrcId {0} already exists.", ssrcId);
					return;
				}
				ClientLogger.Debug("Registering remote session for ssrcId {0}", ssrcId);
				videoThreadData = new VideoThreadData(ssrcId, _codecFactory.GetVideoDecoder(VideoQualityController));
				videoThreadData.Validator.Timer = new Timer(timerCheckingRemoteCameras_Tick, ssrcId, VideoConstants.RemoteCameraCheckDelay, VideoConstants.RemoteCameraTimeout);
				RemoteSessions.Add(ssrcId, videoThreadData);
				_videoEncoder.Synchronize();
				_mediaEnvironment.RemoteSessions = RemoteSessions.Count;
			}
			var decoderThread = new Thread(ProcessVideoQueue);
			decoderThread.Start(videoThreadData);
		}

		/// <summary>
		/// Unregister a media stream source listener for a given ssrcid
		/// </summary>
		/// <param name="ssrcId">The ssrcId of the media stream source for which it should stop listening.</param>
		public virtual void UnregisterRemoteSession(ushort ssrcId)
		{
			if (RemoteSessions.ContainsKey(ssrcId))
			{
				ClientLogger.Debug("Unregistering remote session for ssrcId {0}", ssrcId);
				var validator = RemoteSessions[ssrcId].Validator;
				validator.Timer.Change(Timeout.Infinite, Timeout.Infinite);
				validator.Timer.Dispose();
				validator.Timer = null;
				RemoteSessions.Remove(ssrcId);
				_mediaEnvironment.RemoteSessions = RemoteSessions.Count;
			}
			else
			{
				ClientLogger.Debug("LocalSsrcId {0} not found in remoteSessions", ssrcId);
			}
		}

		private bool OtherMembersInRoom
		{
			get { return RemoteSessions.Count > 0; }
		}

		#endregion

		#region Data Methods

		private void HandleAudioPacket(IMediaPacket packet)
		{
			// The media server sets the ProcessorLoad field to be the max of all the processor loads noted in all the packets it's mixing.
			_mediaEnvironment.RemoteProcessorLoad = packet.ProcessorLoad;
			_audioJitter.WriteSamples(packet.Payload, 0, packet.PayloadLength, packet.SequenceNumber, packet.AudioCodecType, packet.IsSilent);
			Logger.LogAudioFrameReceived(packet);
		}

		private void HandleVideoPacket(IMediaPacket packet)
		{
			if (!IsRemoteVideoMuted)
			{
				VideoThreadData videoThreadData;
				if (RemoteSessions.TryGetValue(packet.SsrcId, out videoThreadData))
				{
					lock (videoThreadData.VideoChunkQueue)
					{
						if (videoThreadData.VideoChunkQueue.Count > VideoConstants.MaxQueuedBlocksPerStream)
						{
							var outdatedChunk = videoThreadData.VideoChunkQueue.Dequeue();
							_videoChunkPool.Recycle(outdatedChunk);
						}
						var chunk = _videoChunkPool.GetNext();
						chunk.SsrcId = packet.SsrcId;
						chunk.Payload.TryWriteBytes(packet.Payload, 0, packet.PayloadLength);
						chunk.Payload.DataLength = packet.PayloadLength;
						videoThreadData.VideoChunkQueue.Enqueue(chunk);
					}
					videoThreadData.ResetEvent.Set(); // Tell the thread responsible for processing this queue to get going.
				}
				else
				{
					Logger.LogVideoThreadDataNotFound(packet.SsrcId);
				}
			}
		}

		#region Audio Methods
		/// <summary>
		/// Reads the next audio frame from the jitter buffer, decodes it, and presents it to the speaker.
		/// </summary>
		/// <returns>A MemoryStream object wrapping the most recently decoded frame.</returns>
		public void GetNextAudioFrame(Action<MemoryStream> callback)
		{
			// ThreadPool.QueueUserWorkItem(o => DecodeFrame(callback));
			DecodeFrame(callback);
		}

		private void DecodeFrame(Action<MemoryStream> callback)
		{
			// At least right now, this is performant enough that we don't need to spin it out onto a separate thread.
			try
			{
				if (_audioJitter == null)
				{
					callback(new MemoryStream(_silentBytes));
					return;
				}

				int length = _audioJitter.ReadSamples(_decodedFrameBuffer);

				// If we haven't yet received real audio, check to see if there's any sound
				if (!AudioReceivedSuccessfully)
				{
					for (int i = 0; i < length; i++)
					{
						if (_decodedFrameBuffer[i] > 500 || _decodedFrameBuffer[i] < 500)
						{
							AudioReceivedSuccessfully = true;
							break;
						}
					}
				}

				// ks 5/28/10 - Clear the data if we're supposed to be playing silence.
				// We need to wait until here to clear it so that when we get unmuted, the codec has been processing
				// sound correctly and can easily deal with turning back up the volume.
				if (IsSpeakerMuted)
				{
					Array.Clear(_decodedFrameBuffer, 0, length);
				}

				Logger.LogAudioFramePlayed();
				if (MediaConfig.ApplyVolumeFilterToPlayedSound)
				{
					ApplyVolumeFilter(SpeakerVolume, _decodedFrameBuffer, 0, length);
				}
				ApplyAudioOutputFilter(_decodedFrameBuffer, 0, length);
				_speakerStatistics.LatestFrame = _decodedFrameBuffer;

				// Copy the shorts to a byte[] array, so that we can submit it to the MediaStreamSource.
				// No point in getting this from an object pool, as its lifecycle is beyond our control.
				var frame = new byte[length * sizeof(short)];
				Buffer.BlockCopy(_decodedFrameBuffer, 0, frame, 0, frame.Length);

				// Queue the played frame so that we can cancel the echo later.
				if (_lastSpeechEnhancementStack != null)
				{
					_lastSpeechEnhancementStack.RegisterFramePlayed(frame);
				}

				callback(new MemoryStream(frame));
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString);
				callback(new MemoryStream(0));
			}
		}

		/// <summary>
		/// Sets the audio frame to be processed before it is transmitted to the media server. Typically called by the RtpAudioSink class.
		/// </summary>
		/// <param name="audioContext">The audio context for the frame in question</param>
		/// <param name="frame">A byte[] array representing the samples received from the local microphone.</param>
		public void SubmitRecordedFrame(AudioContext audioContext, byte[] frame)
		{
			Logger.LogAudioFrameSet();
			_microphoneStatistics.LatestFrame = frame;

			if (!IsConnected || !OtherMembersInRoom) return;

			if (IsMicrophoneMuted)
			{
				frame = _silentBytes;
			}

			lock (_audioFrames)
			{
				_audioFrames.Enqueue(new AudioFrame(audioContext, frame));
			}
			_audioEncodeResetEvent.Set();
		}

		private void TransmitAudio()
		{
			while (_audioEncodeResetEvent.WaitOne() && _isActive)
			{
				_audioEncodeResetEvent.Reset();

				while (_audioFrames.Count > 0)
				{
					AudioFrame audioFrame;
					lock (_audioFrames)
					{
						audioFrame = _audioFrames.Dequeue();
					}
					if (audioFrame == null) continue;

					// Cancel any echo onto the audioCancelBuffer.
					var audioContext = audioFrame.AudioContext;
					var frame = audioFrame.Samples;
					_lastSpeechEnhancementStack = audioContext.SpeechEnhancementStack;
					LastSpeechEnhancementName = _lastSpeechEnhancementStack.InstanceName;
					_lastSpeechEnhancementStack.Write(frame);

					bool moreFrames;
					do
					{
						if (_lastSpeechEnhancementStack.Read(audioContext.CancelBuffer, out moreFrames))
						{
							if (!MediaConfig.PlayEchoCancelledSound)
							{
								Buffer.BlockCopy(frame, 0, audioContext.CancelBuffer, 0, frame.Length);
							}
							_cancelledStatistics.LatestFrame = audioContext.CancelBuffer;
							if (OtherMembersInRoom)
							{
								PrepareAndSendAudioPacket(audioContext, audioContext.CancelBuffer);
							}
						}
					} while (moreFrames);
				}
			}
		}

		// private int _packetsSent;

		private void PrepareAndSendAudioPacket(AudioContext audioContext, short[] audioBuffer)
		{
			// Apply any custom filters.
			ApplyAudioInputFilter(audioBuffer);

			//if (++_packetsSent % 100 == 0)
			//{
			//    DebugHelper.AnalyzeAudioFrame("MediaController_PrepareAndSendAudioPacket", audioBuffer, 0, audioBuffer.Length);
			//}

			if (MediaConfig.EnableDenoise)
			{
				audioContext.DtxFilter.Filter(audioBuffer);
			}

			// Check to see if we've succeeded in sending some audio.
			if (!AudioSentSuccessfully)
			{
				AudioSentSuccessfully = !audioContext.DtxFilter.IsSilent;
			}

			// Set the volume.
			if (MediaConfig.ApplyVolumeFilterToRecordedSound)
			{
				ApplyVolumeFilter(MicrophoneVolume, audioBuffer, 0, audioBuffer.Length);
			}

			// Compress the audio onto the audioEncodeBuffer.
			LastAudioEncoder = audioContext.Encoder.CodecType;
			int length = audioContext.Encoder.Encode(audioBuffer, 0, audioBuffer.Length, audioContext.SendBuffer, audioContext.DtxFilter.IsSilent);

			// Send the packet
			MediaConnection.SendAudioPacket(audioContext.SendBuffer, length, audioContext.Encoder.CodecType, audioContext.DtxFilter.IsSilent, (int)_mediaEnvironment.LocalProcessorLoad);
			Logger.LogAudioFrameTransmitted(audioContext.DtxFilter.IsSilent);
		}

		protected virtual void ApplyAudioInputFilter(short[] frame)
		{
			// ks 8/23/11 - Speex preprocessor shouldn't be active with WebRTC.
			// speexPreProcessor.Filter(frame);
		}

		protected virtual void ApplyAudioOutputFilter(short[] frame, int start, int length)
		{
			// denoiser.Filter(frame);
		}

		private static void ApplyVolumeFilter(double volume, IList<short> audioFrame, int start, int length)
		{
			double volumeFactor = volume / defaultVolume;
			if (volumeFactor == 1.0d)
			{
				return;
			}

			int max = start + length;

			for (int i = start; i < max; i++)
			{
				var adjustedSample = (int)(audioFrame[i] * volumeFactor);
				if (adjustedSample > short.MaxValue)
				{
					adjustedSample = short.MaxValue;
				}
				else if (adjustedSample < short.MinValue)
				{
					adjustedSample = short.MinValue;
				}
				audioFrame[i] = (short)adjustedSample;
			}
		}

		#endregion

		#region Video Methods

		public void GetNextVideoFrame(ushort ssrcId, Action<MemoryStream> callback)
		{
			VideoThreadData videoThreadData;
			if (RemoteSessions.TryGetValue(ssrcId, out videoThreadData))
			{
				callback(videoThreadData.Decoder.GetNextFrame());
			}
			else
			{
				ClientLogger.Debug("Unable to find ssrcId {0} in remoteSessions.", ssrcId);
				callback(null); // This tells the media stream source not to try playing the frame.
			}
		}

		public void SetVideoFrame(byte[] frame, int stride)
		{
			if (IsConnected && OtherMembersInRoom && !IsLocalWebcamMuted)
			{
				_lastVideoFrame = frame;
				_lastStride = stride;
				_videoEncodeResetEvent.Set(); // Tell the TransmitVideo() thread to get moving.
			}
		}

		protected virtual void TransmitVideo()
		{
			// Video frames will almost always be split up into multiple chunks, each of which will have multiple blocks.
			// The chunks that are ready for transmission are stored in the video codec's encodedChunks buffer.
			while (_videoEncodeResetEvent.WaitOne() && _isActive)
			{
				_videoEncodeResetEvent.Reset();
				if (_lastVideoFrame == null) continue;
				_videoEncoder.EncodeFrame(_lastVideoFrame, _lastStride);
				_lastVideoFrame = null;
				var buffer = _videoBufferPool.GetNext();
				try
				{
					bool moreChunks = true;
					while (moreChunks)
					{
						buffer.Reset();
						if (_videoEncoder.GetNextChunk(buffer, out moreChunks))
						{
							MediaConnection.SendVideoPacket(buffer);
							Logger.LogVideoPacketSent(buffer);
						}
					}
				}
				catch (Exception ex)
				{
					ClientLogger.Debug(ex.ToString);
				}
				finally
				{
					_videoBufferPool.Recycle(buffer);
				}
			}
		}

		protected void ProcessVideoQueue(object threadState)
		{
			// The idea here is that we wait for the ProcessRtpData thread to tell us that we've got something to do.
			// Any chunks that arrive before the current chunk is decoded and processed will be discarded.
			var videoThreadData = (VideoThreadData)threadState;
			while (videoThreadData.ResetEvent.WaitOne() && _isActive)
			{
				try
				{
					videoThreadData.ResetEvent.Reset();
					while (videoThreadData.VideoChunkQueue.Count > 0)
					{
						Chunk chunk = null;
						try
						{
							lock (videoThreadData.VideoChunkQueue)
							{
								chunk = videoThreadData.VideoChunkQueue.Dequeue();
							}
							if (chunk != null)
							{
								videoThreadData.Decoder.DecodeChunk(chunk.Payload, videoThreadData.SsrcId);
							}
						}
						finally
						{
							_videoChunkPool.Recycle(chunk);
						}
					}
					UpdateRemoteCameraStatus(videoThreadData);
				}
				catch (Exception ex)
				{
					ClientLogger.ErrorException(ex, "Process video queue failed");
				}
			}
		}

		#endregion

		#endregion

		#region Protected Methods

		private static double NormalizeVolume(double volume)
		{
			if (volume > 1.0d)
			{
				return 1.0d;
			}
			return volume < 0.0d ? 0.0d : volume;
		}

		protected void RaiseErrorOccurred(Exception error)
		{
			if (ErrorOccurred != null)
			{
				var args = new ExceptionEventArgs(error, error.Message);
				ErrorOccurred(this, args);
			}
		}

		protected virtual void timerCheckingRemoteCameras_Tick(object target)
		{
			var ssrcId = (ushort)target;
			VideoThreadData videoThreadData;
			if (RemoteSessions.TryGetValue(ssrcId, out videoThreadData))
			{
				UpdateRemoteCameraStatus(videoThreadData);
			}
		}

		public bool IsRemoteCameraVisible(ushort ssrcId)
		{
			VideoThreadData videoThreadData;
			return RemoteSessions.TryGetValue(ssrcId, out videoThreadData) && videoThreadData.Validator.IsCameraWorking;
		}

		protected void UpdateRemoteCameraStatus(VideoThreadData videoThreadData)
		{
			var decoder = videoThreadData.Decoder;
			var validator = videoThreadData.Validator;

			if (decoder.IsReceivingData != validator.IsCameraWorking)
			{
				validator.IsCameraWorking = decoder.IsReceivingData;
				if (RemoteCameraChanged != null)
				{
					RemoteCameraChanged(this, new RemoteCameraChangedEventArgs(videoThreadData.SsrcId, validator.IsCameraWorking));
				}
			}
		}

		#endregion

		#region IDisposable Members

		private bool _disposed;
		public void Dispose()
		{
			Dispose(true);
		}
		protected void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				// Tell all the active threads they can finish.
				_isActive = false;
				_audioEncodeResetEvent.Set();
				_videoEncodeResetEvent.Set();
				foreach (var videoThreadData in RemoteSessions.Values)
				{
					videoThreadData.ResetEvent.Set();
				}
				_disposed = true;
			}
		}

		#endregion

		protected class AudioFrame
		{
			public AudioFrame(AudioContext audioContext, byte[] samples)
			{
				AudioContext = audioContext;
				Samples = samples;
			}

			public AudioContext AudioContext { get; private set; }
			public byte[] Samples { get; private set; }
		}

	}

	public class RemoteCameraChangedEventArgs : EventArgs
	{
		public ushort SsrcId { get; set; }
		public bool IsAvailable { get; set; }

		public RemoteCameraChangedEventArgs(ushort ssrcId, bool isAvailable)
		{
			SsrcId = ssrcId;
			IsAvailable = isAvailable;
		}
	}

	public class RemoteCameraValidatorEntity
	{
		/// <summary>
		/// Timer for validate is frames changed 
		/// </summary>
		public Timer Timer { get; set; }

		/// <summary>
		/// Is remote camera working
		/// </summary>
		public bool IsCameraWorking { get; set; }
	}
}
