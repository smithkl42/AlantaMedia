using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Alanta.Client.Common.Loader;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Media;
using Alanta.Client.UI.Common.RoomView;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Client.UI.Desktop.RoomView;
using Alanta.Common;
using AudioFormat = Alanta.Client.Media.AudioFormat;

namespace Alanta.Client.Test.Media
{
	public class SourceRoomController : DesktopRoomController
	{

		public const int RemoteSessionCount = 2;
		public Dictionary<Guid, DestinationRoomPage> _destinationRoomPages = new Dictionary<Guid, DestinationRoomPage>();
		public Dictionary<Guid, DestinationMediaController> _destinationMediaControllers = new Dictionary<Guid, DestinationMediaController>();
		public Dictionary<Guid, DestinationRoomController> _destinationRoomControllers = new Dictionary<Guid, DestinationRoomController>();
		private readonly SourceMediaController _sourceMediaController;
		private readonly SessionCollectionViewModel _sessionCollectionViewModel;

		public SourceRoomController(IViewModelFactory viewModelFactory, IRoomInfo roomInfo, IConfigurationService configurationService, MediaTest roomPage) :
			base(viewModelFactory, roomInfo, configurationService)
		{
			// Set the roomViewModel initial values.
			var rnd = new Random();
			var sourceSsrcId = (ushort)rnd.Next(ushort.MinValue, ushort.MaxValue);
			var sourceConfig = new MediaConfig
			{
				MediaServerHost = DataGlobals.MediaServerHost,
				MediaServerControlPort = Constants.DefaultMediaServerControlPort,
				MediaServerStreamingPort = Constants.DefaultMediaServerStreamingPort,
				LocalSsrcId = sourceSsrcId,
				CodecFactory = new CodecFactory(AudioFormat.Default),
				ExpectedAudioLatency = 250
			};
			var sourceMediaStats = new MediaStatistics();
			var sourceMediaEnvironment = new MediaEnvironment(sourceMediaStats);
			var sourceMediaConnection = new RtpMediaConnection(sourceConfig, sourceMediaStats);
			var vqc = new VideoQualityController(sourceConfig.LocalSsrcId);
			_sourceMediaController = new SourceMediaController(sourceConfig, sourceMediaStats, sourceMediaEnvironment, sourceMediaConnection, vqc);
			RoomVm.RoomName = Constants.DefaultRoomName;
			RoomVm.MediaController = _sourceMediaController;
			_sourceMediaController.InputAudioVisualizer = roomPage.audioVisualizer;

			// Setup the local session.
			var room = new Room { Name = Constants.DefaultRoomName, Sessions = new ObservableCollection<Session>() };
			var user = new RegisteredUser { UserId = Guid.NewGuid(), UserTag = "smithkl42", UserName = "Test User" };
			var session = new Session { SessionId = Guid.NewGuid(), SsrcId = _sourceMediaController.LocalSsrcId, User = user };
			var sessionViewModel = viewModelFactory.GetViewModel<SessionViewModel>(vm => vm.Model.SessionId == session.SessionId);
			sessionViewModel.Model = session;
			_sessionCollectionViewModel = viewModelFactory.GetViewModel<SessionCollectionViewModel>();
			RoomVm.SessionVm = sessionViewModel;
			_sessionCollectionViewModel.ViewModels.Add(sessionViewModel);
			RoomVm.SessionId = RoomVm.SessionVm.Model.SessionId;
			var owner = user;
			owner.SharedFiles = new ObservableCollection<SharedFile>();
			room.SharedFiles = owner.SharedFiles;
			room.User = owner;
			RoomVm.UserTag = owner.UserTag;
			room.UserId = owner.UserId;
			RoomVm.Model = room;
			LocalUserVm.Model = owner;
			LocalUserVm.UserId = owner.UserId;

			// Add the destination sessions.
			var codecFactory = new DestinationCodecFactory();
			for (int i = 0; i < RemoteSessionCount; i++)
			{
				var destinationSsrcId = (ushort)rnd.Next(ushort.MinValue, ushort.MaxValue);
				var config = new MediaConfig
				{
					MediaServerHost = DataGlobals.MediaServerHost,
					MediaServerControlPort = Constants.DefaultMediaServerControlPort,
					MediaServerStreamingPort = Constants.DefaultMediaServerStreamingPort,
					LocalSsrcId = destinationSsrcId,
					CodecFactory = codecFactory,
					ExpectedAudioLatency = 250
				};
				var mediaStatistics = new MediaStatistics();
				var mediaEnvironment = new MediaEnvironment(mediaStatistics);
				var mediaConnection = new RtpMediaConnection(config, mediaStatistics);
				var destinationVqc = new VideoQualityController(config.LocalSsrcId);
				var destinationMediaController = new DestinationMediaController(config, mediaStatistics, mediaEnvironment, mediaConnection, destinationVqc);
				var remoteSession = new Session { SessionId = Guid.NewGuid(), SsrcId = destinationMediaController.LocalSsrcId }; //todo: create sessionViewModel, add it to SessionCollectionViewModel

				_sourceMediaController.RegisterRemoteSession((ushort)(remoteSession.SsrcId));
				remoteSession.User = new RegisteredUser { UserId = Guid.NewGuid(), UserTag = "smithkl42", UserName = "Test User" };
				room.Sessions.Add(remoteSession);
				var destinationRoomPage = new DestinationRoomPage();
				destinationMediaController.OutputAudioVisualizer = destinationRoomPage.audioVisualizer;
				// var newViewModelFactory = new ViewModelFactory(RoomService, MessageService, ViewLocator);
				var destinationController = new DestinationRoomController(destinationMediaController, viewModelFactory, new TestRoomInfo(), configurationService, RoomVm, remoteSession.SessionId);
				destinationMediaController.Connect(RoomVm.Model.RoomId.ToString());

				// Store references to the created objects.
				_destinationMediaControllers[remoteSession.SessionId] = destinationMediaController;
				_destinationRoomPages[remoteSession.SessionId] = destinationRoomPage;
				_destinationRoomControllers[remoteSession.SessionId] = destinationController;
			}

			// We have to wait until all the sessions have been created before we can register them with their media controllers and initialize their pages.
			foreach (Guid sessionId in _destinationRoomControllers.Keys)
			{
				var destinationRoomController = _destinationRoomControllers[sessionId];
				var destinationPage = _destinationRoomPages[sessionId];
				destinationPage.Initialize(destinationRoomController);

			}
		}

		public AudioSinkAdapter GetAudioSink(CaptureSource captureSource, MediaController mediaController)
		{
			var sink = new TestMultipleDestinationAudioSinkAdapter(captureSource, _sourceMediaController, _destinationMediaControllers, MediaConfig.Default);
			return sink;
		}

		public VideoSinkAdapter GetVideoSink(CaptureSource captureSource, MediaController mediaController)
		{
			var sink = new TestMultipleDestinationVideoSinkAdapter(captureSource, _sourceMediaController, _destinationMediaControllers);
			return sink;
		}

	}
}
