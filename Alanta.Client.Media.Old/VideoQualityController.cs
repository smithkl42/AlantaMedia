using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using Alanta.Client.Common.Collections;
using Alanta.Client.Common.Logging;
using ReactiveUI;

namespace Alanta.Client.Media
{
	public class VideoQualityController : ReactiveObject, IVideoQualityController
	{

		#region Constructors
		public VideoQualityController(ushort ssrcId, Dictionary<ushort, VideoThreadData> remoteSessions = null)
		{
			_ssrcId = ssrcId;

			RemoteSessions = remoteSessions;

			LocalVideoQuality = VideoQuality.Medium;
			ProposedVideoQuality = VideoQuality.Medium;
			CommandedVideoQuality = VideoQuality.NotSpecified;

			QualityUpdateInterval = TimeSpan.FromSeconds(30);
			QualityHoldInterval = TimeSpan.FromSeconds(30);
			_lowestProposedVideoQuality = VideoQuality.High;
		}

		#endregion

		#region Fields and Properties

		/* 
		 * Video quality is determined through a multi-step process:
		 * (1) The initial video quality is set to Medium, with a proposed quality also set to Medium.
		 * (2) If a "CommandedVideoQuality" is received, its local video quality will be updated (until more glitches are encountered).
		 * (3) Every client records each network glitch it receives.
		 * (4) If there are more than two network glitches in the last 30 seconds, its (local) actual and proposed quality is reduced.
		 * (5) If this client is the controller, i.e., has the lowest SsrcId of those received in the last 30 seconds, 
		 *     it will always send out as the "CommandedVideoQuality" whatever its local video quality is.
		 * */

		private readonly ushort _ssrcId;
		public Dictionary<ushort, VideoThreadData> RemoteSessions { get; set; }
		private ushort _lowestSsrcId;
		private readonly TimeSpan _pruningInterval = TimeSpan.FromSeconds(15);
		private DateTime _lastPrune = DateTime.MinValue;
		public TimeSpan QualityUpdateInterval { get; private set; }
		public TimeSpan QualityHoldInterval { get; private set; }
		private DateTime _lastQualityCheck = DateTime.MinValue;
		private DateTime _lastQualityUpdate = DateTime.MinValue;

		public event EventHandler VideoQualityChanged;

		public readonly TimeSpan GlitchInterval = TimeSpan.FromSeconds(30);

		/// <summary>
		/// If we've received more than this number of glitches, back off quality.
		/// </summary>
		public const int MaxGlitches = 50;

		/// <summary>
		/// If we've received less than this number of glitches, increase quality.
		/// </summary>
		public const int MinGlitches = 10;

		/// <summary>
		/// For testing purposes.
		/// </summary>
		public DateTime Now
		{
			get { return _now ?? DateTime.Now; }
			set { _now = value; }
		}
		private DateTime? _now;

		/// <summary>
		/// The current quality of the video.
		/// </summary>
		public VideoQuality LocalVideoQuality
		{
			get { return _localVideoQuality; }
			set
			{
				if (_localVideoQuality == value) return;
				Debug.Assert(value != VideoQuality.NotSpecified);
				_localVideoQuality = value;
				switch (LocalVideoQuality)
				{
					case VideoQuality.Fallback:
						AcceptFramesPerSecond = 1;
						InterleaveFactor = 1;
						FullFrameInterval = AcceptFramesPerSecond * 8;
						NoSendCutoff = 2000;
						DeltaSendCutoff = 2600;
						JpegQuality = 20;
						break;
					case VideoQuality.Low:
						AcceptFramesPerSecond = 1;
						InterleaveFactor = 1;
						FullFrameInterval = AcceptFramesPerSecond * 4;
						NoSendCutoff = 1800;
						DeltaSendCutoff = 1800;
						JpegQuality = 30;
						break;
					case VideoQuality.Medium:
						AcceptFramesPerSecond = 5;
						InterleaveFactor = 1;
						FullFrameInterval = AcceptFramesPerSecond * 4;
						NoSendCutoff = 1400;
						DeltaSendCutoff = 1400;
						JpegQuality = 40;
						break;
					case VideoQuality.High:
						AcceptFramesPerSecond = 10;
						InterleaveFactor = 1;
						FullFrameInterval = AcceptFramesPerSecond * 2;
						NoSendCutoff = 1000;
						DeltaSendCutoff = 1000;
						JpegQuality = 50;
						break;
				}
				if (VideoQualityChanged != null)
				{
					VideoQualityChanged(this, new EventArgs());
				}
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					this.RaisePropertyChanged(x => x.LocalVideoQuality);
					this.RaisePropertyChanged(x => x.LocalVideoQualityImage);
				});
			}
		}
		private VideoQuality _localVideoQuality;

		/// <summary>
		/// The value at which the current client thinks it itself could send and receive data 
		/// (were it not for other constraints, such as other clients' bandwidth).
		/// </summary>
		public VideoQuality ProposedVideoQuality
		{
			get { return _proposedVideoQuality; }
			set { this.RaiseAndSetIfChanged(x => x.ProposedVideoQuality, ref _proposedVideoQuality, value); }
		}
		private VideoQuality _proposedVideoQuality;

		/// <summary>
		/// The quality at which the local client wants other clients to send and receive data.
		/// This is only specified if the current client has the lowest SsrcId of all clients in the current conversation.
		/// </summary>
		public VideoQuality CommandedVideoQuality
		{
			get { return _commandedVideoQuality; }
			private set { this.RaiseAndSetIfChanged(x => x.CommandedVideoQuality, ref _commandedVideoQuality, value); }
		}
		private VideoQuality _commandedVideoQuality;

		public string LocalVideoQualityImage
		{
			get
			{
				string image = "/Alanta.Client.UI.Common;component/Images/VideoQuality" + _localVideoQuality + ".png";
				return image;
			}
		}

		private VideoQuality _lowestProposedVideoQuality;
		private readonly List<LoggedVideoQuality> _proposedVideoQualityList = new List<LoggedVideoQuality>();
		private readonly Dictionary<int, DateTime> _ssrcIdDictionary = new Dictionary<int, DateTime>();
		private readonly List<LoggedGlitch> _loggedGlitchList = new List<LoggedGlitch>();

		/// <summary>
		/// The number of video frames accepted per second. Higher value = higher quality.
		/// </summary>
		public int AcceptFramesPerSecond { get; private set; }

		/// <summary>
		/// The number of blocks to skip during every normal frame. Lower value = higher quality.
		/// </summary>
		public int InterleaveFactor { get; private set; }

		/// <summary>
		/// The number of predictive frames to be sent in-between i-frames. Lower value = higher quality.
		/// </summary>
		public int FullFrameInterval { get; private set; }

		/// <summary>
		/// If the cumulative difference in the packets is lower than this, the packet will not be sent at all. Lower value = higher quality.
		/// </summary>
		public int NoSendCutoff { get; private set; }

		/// <summary>
		/// If the cumulative difference in the packets is between the NoSendCutoff and this value, the packet will be sent using delta encoding. Lower value = higher quality.
		/// </summary>
		public int DeltaSendCutoff { get; private set; }

		/// <summary>
		/// The quality of the compressed JPEG image to send
		/// </summary>
		public byte JpegQuality { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Log the quality of a received video frame so that we can make some present and future decisions about the quality of the video *we're* sending.
		/// </summary>
		/// <param name="remoteSsrcId">The SsrcId of the remote client</param>
		/// <param name="remoteCommandedVideoQuality">The video quality at which the remote client is actually sending video</param>
		/// <param name="remoteProposedVideoQuality">The video quality at which the remote client thinks it could send send video</param>
		public void LogReceivedVideoQuality(ushort remoteSsrcId, VideoQuality remoteCommandedVideoQuality, VideoQuality remoteProposedVideoQuality)
		{
			Debug.Assert(remoteProposedVideoQuality != VideoQuality.NotSpecified);
			var now = Now;
			_ssrcIdDictionary[remoteSsrcId] = now;
			_lowestSsrcId = Math.Min(_lowestSsrcId, remoteSsrcId);

			// Update the proposed quality indicators.
			_lowestProposedVideoQuality = (VideoQuality)Math.Min((byte)_lowestProposedVideoQuality, (byte)remoteProposedVideoQuality);

			lock (_proposedVideoQualityList)
			{
				_proposedVideoQualityList.Add(new LoggedVideoQuality(remoteProposedVideoQuality, now));

				// Remove any old log entries and update the various "lowest" entries.
				if (_lastPrune + _pruningInterval <= now)
				{
					_ssrcIdDictionary.RemoveWhere(kvp => kvp.Value < _lastPrune);
					_proposedVideoQualityList.RemoveWhere(ql => ql.ReceivedOn < _lastPrune);
					_loggedGlitchList.RemoveWhere(lg => lg.ReceivedOn <= now - GlitchInterval);
					_lowestProposedVideoQuality = _proposedVideoQualityList.Min(ql => ql.VideoQuality);
					_lowestSsrcId = Math.Min(_ssrcId, (ushort)_ssrcIdDictionary.Min(kvp => kvp.Key));
					_lastPrune = now;
				}
			}

			// If the remote machine is the controller, and is commanding us to change our video quality, do so.
			if (remoteSsrcId <= _lowestSsrcId && remoteCommandedVideoQuality != VideoQuality.NotSpecified && remoteCommandedVideoQuality != LocalVideoQuality)
			{
				ClientLogger.Debug("Remote machine {0} has commanded us to change our local video quality from {1} to {2}", remoteSsrcId, LocalVideoQuality, remoteCommandedVideoQuality);
				LocalVideoQuality = remoteCommandedVideoQuality;
			}

			// If we haven't encountered many glitches personally, let the controller know we think we can do better.
			if (_loggedGlitchList.Count <= MinGlitches)
			{
				ProposedVideoQuality = (VideoQuality)Math.Min((int)VideoQuality.High, (int)LocalVideoQuality + 1);
			}

			// If we're not the quality controller, make sure we're not telling anyone what to do, and bail.
			if (!IsController())
			{
				CommandedVideoQuality = VideoQuality.NotSpecified;
				return;
			}

			// If we are the controller, and the proposed video quality is lower than our current video quality, 
			// immediately command everyone to adopt that video quality, and bail.
			if (remoteProposedVideoQuality < LocalVideoQuality)
			{
				LocalVideoQuality = remoteProposedVideoQuality;
				CommandedVideoQuality = remoteProposedVideoQuality;
				return;
			}

			// Bail if we're not yet at a point where we can move the video quality up.
			if (_lastQualityCheck + QualityUpdateInterval > now) return;

			// Move the video quality up if we think we can do so.
			if (LocalVideoQuality < _lowestProposedVideoQuality)
			{
				ClientLogger.Debug("Everyone thinks we can handle better video; bumping quality from {0} to {1}", LocalVideoQuality, _lowestProposedVideoQuality);
				LocalVideoQuality = _lowestProposedVideoQuality;
				CommandedVideoQuality = _lowestProposedVideoQuality;
			}
			_lastQualityCheck = now;
		}

		/// <summary>
		/// Log any network glitch, so that we can request a general reduction in quality if necessary.
		/// </summary>
		public void LogGlitch(int points)
		{
			// Don't log a glitch if there's nobody else in the room.
			if (RemoteSessions == null || RemoteSessions.Count == 0) return;

			// See if we can figure out why we're sometimes getting invalid point values.
			if (points > 50 || points < 1)
			{
				var st = new StackTrace();
				var frames = st.GetFrames();
				var sb = new StringBuilder();
				if (frames != null)
				{
					foreach (var frame in frames)
					{
						sb.Append(frame.GetMethod().Name + "->");
					}
				}
				ClientLogger.Debug("Unexpected network glitch point value: {0}; callstack={1}", points, sb);
				points = 1;
			}

			// ClientLogger.LogDebugMessage("Network glitch encountered.");
			var now = Now;
			_loggedGlitchList.RemoveWhere(lg => lg.ReceivedOn <= now - GlitchInterval);
			_loggedGlitchList.Add(new LoggedGlitch(points, now));
			try
			{
				if (_loggedGlitchList.Sum(lg => lg.Points) < MaxGlitches) return;
			}
			catch (OverflowException ex)
			{
				// Try to figure out what's causing this.
				var sb = new StringBuilder();
				foreach (var loggedGlitch in _loggedGlitchList)
				{
					sb.Append(loggedGlitch.Points + ",");
				}
				ClientLogger.Debug("Overflow exception: \r\nError:{0};\r\nPoints:{1}", ex.ToString(), sb.ToString());
			}

			// Bail if we've already updated the quality recently.
			if (_lastQualityUpdate + QualityHoldInterval > now) return;

			var newVideoQuality = (VideoQuality)Math.Max((int)VideoQuality.Fallback, (int)LocalVideoQuality - 1);
			if (newVideoQuality < LocalVideoQuality)
			{
				ClientLogger.Debug("{0} glitch points encountered. Dropping video quality to {1}", _loggedGlitchList.Count, newVideoQuality);
				_lastQualityUpdate = now;
				LocalVideoQuality = newVideoQuality;
				ProposedVideoQuality = newVideoQuality;

				// If we're the controller, go ahead and set the quality for everyone.
				if (IsController())
				{
					CommandedVideoQuality = newVideoQuality;
				}
			}
		}

		private bool IsController()
		{
			return _ssrcId <= _lowestSsrcId;
		}

		#endregion

	}

	public enum VideoQuality : byte
	{
		NotSpecified = 0,
		Fallback = 1,
		Low = 2,
		Medium = 3,
		High = 4
	}

	public struct LoggedGlitch
	{
		public LoggedGlitch(int points, DateTime receivedOn)
		{
			Points = points;
			ReceivedOn = receivedOn;
		}

		public DateTime ReceivedOn;
		public int Points;
	}

	public struct LoggedVideoQuality
	{
		public LoggedVideoQuality(VideoQuality videoQuality, DateTime receivedOn)
		{
			VideoQuality = videoQuality;
			ReceivedOn = receivedOn;
		}

		public DateTime ReceivedOn;
		public VideoQuality VideoQuality;
	}
}
