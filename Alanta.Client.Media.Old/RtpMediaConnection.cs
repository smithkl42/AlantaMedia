using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public class RtpMediaConnection : IMediaConnection
	{
		#region Constructors
		public RtpMediaConnection(MediaConfig config, MediaStatistics mediaStats)
		{
			_controlClient = new NetClient(config.MediaServerHost, config.MediaServerControlPort);
			_rtpClient = new NetClient(config.MediaServerHost, config.MediaServerStreamingPort, mediaStats);
			_rtpConnect = new RtpPacketConnect();
			_rtpData = new RtpPacketData();
			_rtpConnect.SsrcId = config.LocalSsrcId;
			_dataReceiveBuffer = new ByteStream(RtpPacketData.DataPacketMaxLength * 10); // Leave room for at least 10 packets.
			_rtpPacketDataListPool = new ObjectPool<List<RtpPacketData>>(() => new List<RtpPacketData>(), list => list.Clear());
			_rtpPacketDataPool = new ObjectPool<RtpPacketData>(() => new RtpPacketData()); // No reset action needed
			_packetBufferPool = new ObjectPool<ByteStream>(() => new ByteStream(RtpPacketData.DataPacketMaxLength), bs => bs.Reset());
		}
		#endregion

		#region Fields and Properties

		public Action<IMediaPacket> AudioPacketHandler { get; set; }
		public Action<IMediaPacket> VideoPacketHandler { get; set; }

		private Timer _connectionTimer;
		private Action<Exception> _connectionCallback;
		private string _roomId;

		protected INetClient _rtpClient;
		protected INetClient _controlClient;
		protected RtpPacketConnect _rtpConnect;
		protected RtpPacketData _rtpData;
		protected MediaCommandSet _commandSet;
		protected bool _expectingControlData;
		private const string success = "Success";
		// private const string failed = "Failed";
		protected ByteStream _dataReceiveBuffer;

		// ks 4/8/10 - These pool objects are intended to reduce the time spent in garbage collection.
		// Testing has shown that for reasonably complex objects or large buffers (>512 bytes), 
		// it's faster to re-use the objects/buffers than to recreate them on each pass.
		protected ObjectPool<List<RtpPacketData>> _rtpPacketDataListPool;
		protected ObjectPool<RtpPacketData> _rtpPacketDataPool;
		protected ObjectPool<ByteStream> _packetBufferPool;

		public bool IsConnecting
		{
			get { return _connectionTimer != null; }
		}

		public virtual bool IsConnected
		{
			get { return _rtpClient != null && _rtpClient.IsConnected; }
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
			if (!IsConnecting)
			{
				_roomId = roomId;
				_connectionTimer = new Timer(ConnectionTimerCallback, null, TimeSpan.FromSeconds(60), TimeSpan.FromMilliseconds(-1));
				_connectionCallback = callback;
				_controlClient.Connect(HandleControlConnect, HandleControlData); // -> HandleControlConnect
			}
			else
			{
				FinalizeConnection(null);
			}
		}

		/// <summary>
		/// Callback method called after attempting to connect to the media server's control port.
		/// </summary>
		/// <param name="connectException">Any exception raised by the connection attempt.</param>
		protected virtual void HandleControlConnect(Exception connectException)
		{
			try
			{
				if (connectException == null)
				{
					RegisterClientOnServer();
				}
				else
				{
					string message = _controlClient != null ? String.Format(MediaStrings.FailedToConnectToControlPort, _controlClient.Port, _controlClient.Host) : "Failed to connect to the media server and controlClient is null.";
					FinalizeConnection(new Exception(message, connectException));
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Error on handling media server connection");
				FinalizeConnection(ex);
			}
		}

		/// <summary>
		/// Registers the local client on the audio server using an out-of-band control channel.
		/// </summary>
		protected virtual void RegisterClientOnServer()
		{
			ClientLogger.Debug("Registering client {0} on server.", _rtpConnect.SsrcId);
			_commandSet = new MediaCommandSet { new MediaCommandCreateRoom(_roomId), new MediaCommandAddClient(_roomId, _rtpConnect.SsrcId.ToString()) };
			_expectingControlData = true;
			_controlClient.Send(_commandSet.ToString()); // -> HandleControlData()
		}

		/// <summary>
		/// Callback method called after sending registration data to the media server over control port (typically 4521).
		/// </summary>
		/// <param name="data">The byte array containing the data sent by the media server</param>
		/// <param name="offset">The offset into the byte array where the actual data starts</param>
		/// <param name="length">The length of the data sent by the media server</param>
		protected virtual void HandleControlData(byte[] data, int offset, int length)
		{
			if (_disposed) return;
			try
			{
				if (_expectingControlData)
				{
					_expectingControlData = false;
					string message = Encoding.UTF8.GetString(data, offset, length); // .Split('\0')[0];
					_commandSet.ParseResult(message);
					if (CheckSuccess(_commandSet))
					{
						_rtpClient.Connect(HandleRtpConnect, HandleRtpData); // -> HandleRtpConnect
					}
					else
					{
						string errorMessage = MediaStrings.FailedToRegister;
#if DEBUG
						errorMessage += Environment.NewLine + "Server = " + _rtpClient.Host + "; Port = " + _rtpClient.Port;
#endif
						FinalizeConnection(new Exception(errorMessage));
					}
				}
				else
				{
					ClientLogger.Error("Unexpectedly received control data.");
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Handle control data failed");
				FinalizeConnection(ex);
			}
		}

		private static bool CheckSuccess(IEnumerable<MediaCommand> commandSet)
		{
			foreach (MediaCommand command in commandSet)
			{
				if (String.Compare(command.Result, success, StringComparison.OrdinalIgnoreCase) != 0)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Resets the state on any classes that need to be fresh before there's a 
		/// </summary>
		private void Reset()
		{
			_dataReceiveBuffer.Reset();
		}

		/// <summary>
		/// Callback method called after attempting to connect to the media server's data port (typically 4522).
		/// </summary>
		/// <param name="ex">Any exception raised by the connection process.</param>
		protected virtual void HandleRtpConnect(Exception ex)
		{
			if (ex == null)
			{
				Reset();
				_rtpClient.Send(_rtpConnect.BuildPacket());
			}

			// This is the final step in the connection chain.
			FinalizeConnection(ex);
		}

		/// <summary>
		/// Handles the case where a connection times out.
		/// </summary>
		/// <param name="state"></param>
		protected void ConnectionTimerCallback(object state)
		{
			FinalizeConnection(new TimeoutException(MediaStrings.Timeout));
		}

		/// <summary>
		/// Closes any outstanding connections to the media server's control port, and issues the final callback.
		/// </summary>
		/// <param name="ex"></param>
		protected void FinalizeConnection(Exception ex)
		{
			if (_disposed) return;
			try
			{
				if (_connectionTimer != null)
				{
					_connectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
					_connectionTimer = null;
				}
				if (_controlClient != null && _controlClient.IsConnected)
				{
					_controlClient.Disconnect();
				}
				if (_connectionCallback != null)
				{
					_connectionCallback(ex);
				}
			}
			finally
			{
				_connectionCallback = null;
			}
		}

		#endregion

		#region Data Methods
		protected void HandleRtpData(byte[] buffer, int offset, int length)
		{
			if (_disposed) return;
			List<RtpPacketData> packets = null;
			try
			{
				// ks 8/23/11 - TODO: Investigate whether we can get rid of this memcpy(). 
				// I think we can do it by passing the dataReceiveBuffer to the NetClient class,
				// and just keeping better track of what's been processed and what hasn't.
				// Kind of like how we handle it in the media server.
				lock (_dataReceiveBuffer)
				{
					// Copy the data in the network receive buffer to the packet receive buffer.
					int dataLength = _dataReceiveBuffer.CurrentOffset + length;
					_dataReceiveBuffer.TryWriteBytes(buffer, offset, length);
					_dataReceiveBuffer.DataOffset = 0;
					_dataReceiveBuffer.CurrentOffset = 0;
					_dataReceiveBuffer.DataLength = dataLength;

					// Pull the packets out of the packet receive buffer.
					packets = RtpPacketData.GetPacketsFromData(_dataReceiveBuffer, _rtpPacketDataListPool, _rtpPacketDataPool);
				}
				#region Packet Processor
				foreach (var packet in packets)
				{
					try
					{
						switch (packet.PayloadType)
						{
							case RtpPayloadType.Audio:
								if (AudioPacketHandler != null)
								{
									AudioPacketHandler(packet);
								}
								break;
							case RtpPayloadType.VideoFromServer:
								if (VideoPacketHandler != null)
								{
									VideoPacketHandler(packet);
								}
								break;
							default:
								ClientLogger.Debug("Unexpected packetBuffer type {0}", packet.PayloadType);
								break;
						}
					}
					finally
					{
						_rtpPacketDataPool.Recycle(packet);
					}
				}
				#endregion
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Error processing RTP data");
			}
			finally
			{
				_rtpPacketDataListPool.Recycle(packets);
			}
		}

		#endregion

		public void Disconnect()
		{
			// Disconnect from the server.
			if (_rtpClient != null)
			{
				_rtpClient.Disconnect();
			}
			if (_controlClient != null)
			{
				_controlClient.Disconnect();
			}
			_disposed = true;
		}

		public void SendAudioPacket(short[] audioBuffer, int length, AudioCodecType codecTypeType, bool isSilent, int localProcessorLoad)
		{
			ByteStream packetBuffer = null;
			try
			{
				bool packetBuilt;
				lock (_rtpData)
				{
					_rtpData.SequenceNumber++;
					_rtpData.PayloadType = RtpPayloadType.Audio;
					_rtpData.AudioCodecType = codecTypeType;
					_rtpData.IsSilent = isSilent;
					_rtpData.Payload = audioBuffer;
					_rtpData.PayloadLength = (ushort)(length * sizeof(short));
					_rtpData.ProcessorLoad = (byte)localProcessorLoad;
					packetBuffer = _packetBufferPool.GetNext();
					packetBuilt = _rtpData.TryBuildPacket(packetBuffer);
				}
				if (packetBuilt)
				{
					if (_rtpClient != null)
					{
						_rtpClient.Send(packetBuffer.Data, packetBuffer.DataOffset, packetBuffer.DataLength);
					}
				}
				else
				{
					ClientLogger.Debug("Error building audio packetBuffer.");
				}
			}
			finally
			{
				_packetBufferPool.Recycle(packetBuffer);
			}
		}

		public void SendVideoPacket(ByteStream videoChunk)
		{
			ByteStream packetBuffer = null;
			try
			{
				packetBuffer = _packetBufferPool.GetNext();
				bool packetBuilt;
				lock (_rtpData)
				{
					_rtpData.PayloadType = RtpPayloadType.VideoFromClient;
					_rtpData.Payload = videoChunk.Data;
					_rtpData.PayloadLength = (ushort)videoChunk.DataLength;
					packetBuilt = _rtpData.TryBuildPacket(packetBuffer);
				}
				if (packetBuilt)
				{
					_rtpClient.Send(packetBuffer.Data, packetBuffer.DataOffset, packetBuffer.DataLength);
				}
				else
				{
					ClientLogger.Debug("Error building video packetBuffer.");
				}
			}
			finally
			{
				_packetBufferPool.Recycle(packetBuffer);
			}
		}


		private bool _disposed;
		public void Dispose()
		{
			Disconnect();
			_disposed = true;
		}
	}
}
