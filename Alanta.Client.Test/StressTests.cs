using System;
using System.Threading;
using System.Windows;
using Alanta.Client.Common;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class StressTests : DataTestBase
	{
		[TestCleanup]
		[Asynchronous]
		public override void TestCleanup()
		{
			ClientLogger.Debug(" -- Beginning test cleanup.");
			bool cleanupFinished = false;
			TestCleaning(() =>
				_testController.RoomService.LeaveRoom(_testController.RoomVm.SessionId, leaveRoomError =>
					_testController.RoomService.CloseClientAsync(clientCloseError => cleanupFinished = true)));
			EnqueueConditional(() => cleanupFinished);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("stress")]
		public void OpenAndCloseConnectionMultipleTimes()
		{
			const int maxCalls = 100;
			int callNumber = 0;
			OperationCallback<User>[] getUserHandler = {null};
			OperationCallback[] closeHandler = {null};
			bool finished = false;
			IRoomServiceAdapter client = _testController.RoomService;

			Action openClient = () =>
			{
				client.CreateClient();
				client.GetUser(Guid.NewGuid(), getUserHandler[0]);
			};

			getUserHandler[0] = (e, user) =>
				{
					ClientLogger.Debug("Get user {0} completed; now closing.", callNumber);
					callNumber++;

					// Only try to close the connection if we're going to run another test, as otherwise the test cleanup will take care of it.
					if (callNumber < maxCalls)
					{
						client.CloseClientAsync(closeHandler[0]);
					}
					else
					{
						finished = true;
					}
				};

			closeHandler[0] = e =>
				{
					ClientLogger.Debug("Client {0} closed.", callNumber);
					Deployment.Current.Dispatcher.BeginInvoke(openClient);
				};

			Deployment.Current.Dispatcher.BeginInvoke(openClient);

			EnqueueConditional(() => finished);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("stress")]
		public void DeleteUserMultipleTimes()
		{
			const int maxCalls = 100;
			int actualCalls = 0;
			var client = _testController.RoomService;

			OperationCallback<object> handler = (error, state) =>
			{
				var userState = (object[])state;
				var call = (int)userState[0];
				var startTime = (DateTime)userState[1];
				var elapsed = DateTime.Now - startTime;
				actualCalls++;
				ClientLogger.Debug("Processing delete user call #{0}; actual call = {1}, recentElapsed time = {2} ms", call, actualCalls, elapsed.TotalMilliseconds);
				Assert.IsNull(error);
			};

			// Kick everything off.
			JoinRoom(error =>
				{
					for (int i = 0; i < maxCalls; i++)
					{
						ClientLogger.Debug("Queueing delete user call #{0}", i);
						ThreadPool.QueueUserWorkItem(o =>
							{
								var callNumber = (int)o;
								var startTime = DateTime.Now;
								ClientLogger.Debug("Executing delete user call #{0}", callNumber);
								client.DeleteUser(Guid.NewGuid(), handler, new object[] { callNumber, startTime });
							}, i);
					}
				});
			EnqueueConditional(() => actualCalls >= maxCalls);
			EnqueueTestComplete();
		}

		[TestMethod]
		[Asynchronous]
		[Priority(5)]
		// [Timeout(60000)]
		[Tag("stress")]
		public void SendMultipleChatMessages()
		{
			const int maxMessages = 100;
			int receivedMessages = 0;
			int[] sentMessages = {0};

			// Initialize the chatMessageAdded event handler, which will tell us that the chat message was sent successfully.
			EventHandler<EventArgs<ChatMessage>> handleChatMessageReceived = (s, e) =>
			{
				receivedMessages++;
				ClientLogger.Debug("Message #{0} received.", receivedMessages);
			};
			_roomService.ChatMessageReceived += handleChatMessageReceived;
			// roomViewModel.ChatMessages.CollectionChanged += handleChatMessageAdded;

			// Kick everything off.
			JoinRoom(error =>
				{
					for (int i = 0; i < maxMessages; i++)
					{
						_roomService.SendMessage(_roomVm.SessionId, Guid.NewGuid().ToString(), e1 =>
						{
							Assert.IsNull(e1);
							sentMessages[0]++;
						});
					}
				});
			EnqueueConditional(() => receivedMessages >= maxMessages);
			EnqueueConditional(() => sentMessages[0] == maxMessages);
			EnqueueTestComplete();
		}

		//[TestMethod]
		//[Asynchronous]
		//[Timeout(30000)]
		//[Tag("stress")]
		//public void TestDuplexHttp5ClientsSend1Msg()
		//{
		//    int maxClients = 5;
		//    int sentMessages = 0;
		//    for (int i = 0; i < maxClients; i++)
		//    {
		//        RoomServiceClient service = TestRoomServiceAdapter.GetRoomServiceBasedOnHttp();
		//        service.PingCompleted += delegate
		//        {
		//            sentMessages++;
		//            service.CloseCompleted += delegate
		//            {
		//                service = null;
		//            };
		//            service.CloseAsync();
		//        };
		//        service.PingAsync();
		//    }

		//    EnqueueConditional(() => sentMessages == maxClients);
		//    EnqueueTestComplete();
		//}

		[TestMethod]
		[Asynchronous]
		[Timeout(60000)]
		[Tag("stress")]
		public void TestDuplexTcp500ClientsSend1Msg()
		{
			const int maxClients = 500;
			int[] sentMessages = {0};
			for (int i = 0; i < maxClients; i++)
			{
				var service = TestRoomServiceAdapter.GetRoomServiceBasedOnTcp();
				service.PingCompleted += delegate
				{
					sentMessages[0]++;
					service.CloseCompleted += delegate
					{
						service = null;
					};
					service.CloseAsync();
				};
				service.PingAsync();
			}

			EnqueueConditional(() => sentMessages[0] == maxClients);
			EnqueueTestComplete();
		}
	}

	public class TestRoomServiceAdapter : RoomServiceAdapter
	{
		public static RoomServiceClient GetRoomServiceBasedOnTcp()
		{
			return new RoomServiceClient(GetDuplexTcpBinding(), GetDuplexTcpEndpoint());
		}

		//public static RoomServiceClient GetRoomServiceBasedOnHttp()
		//{
		//    return new RoomServiceClient(GetDuplexHttpBinding(), GetDuplexHttpEndpoint());
		//}
	}
}
