using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Alanta.Client.Data;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Client.UI.Desktop.RoomView;
using Alanta.Client.UI.Desktop.RoomView.Workspace;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	public class UiTestBase : PresentationTest
	{
		protected ToolControl toolControl;
		protected WorkspacePanel workspacePanel;
		protected WebCamerasControl webcamerasControl;
		protected RoomHeaderControl loginControl;
		protected DesktopRoomController roomController;
		protected IViewModelFactory viewModelFactory;
		protected RoomViewModel roomVm;
		protected CompanyViewModel companyVm;
		protected AuthenticationGroupViewModel authenticationGroupVm;
		protected LocalUserViewModel localUserVm;
		protected WorkspaceViewModel workspaceVm;
		protected RoomPage roomPage;

		[TestInitialize]
		[Asynchronous]
		public void ClientTestInitialize()
		{
			bool pageInitialized = false;
			bool isInitializingCompleted = false;

			EnqueueConditional(() => TestGlobals.Initialized);
			EnqueueCallback(() =>
			{
				roomPage = new RoomPage();
				viewModelFactory = roomPage.ViewModelFactory;
				viewModelFactory.MessageService = new TestMessageService();
				viewModelFactory.RoomService.CreateClient();
				companyVm = viewModelFactory.GetViewModel<CompanyViewModel>();
				companyVm.Model = TestGlobals.Company;
				authenticationGroupVm = viewModelFactory.GetViewModel<AuthenticationGroupViewModel>();
				authenticationGroupVm.Model = TestGlobals.AuthenticationGroup;
				localUserVm = viewModelFactory.GetViewModel<LocalUserViewModel>();
				localUserVm.CompanyInfo = new TestCompanyInfo();
				roomVm = viewModelFactory.GetViewModel<RoomViewModel>();

				// Simulates the results of the private InitializeAsync() method.
				roomVm.UserTag = TestGlobals.OwnerUserTag;
				roomVm.RoomName = TestGlobals.RoomName;
				DataGlobals.LoginSession = localUserVm.LoginSession;
				DataGlobals.OwnerUserTag = TestGlobals.OwnerUserTag;
				DataGlobals.RoomName = TestGlobals.RoomName;

				localUserVm.Login(TestGlobals.UserTag, TestGlobals.Password, loginError => roomVm.JoinRoom(joinRoomError =>
				{
					roomPage.PageInitialized += (page, initializedArgs) => Deployment.Current.Dispatcher.BeginInvoke(() =>
					{
						workspaceVm = viewModelFactory.GetViewModel<WorkspaceViewModel>();
						roomController = initializedArgs.Value;
						Assert.IsNotNull(initializedArgs.Value, "RoomController is Null");
						toolControl = roomPage.AlantaControls.SingleOrDefault(c => c is ToolControl) as ToolControl;
						Assert.IsNotNull(toolControl, "ToolControl is Null");
						workspacePanel = roomPage.AlantaControls.SingleOrDefault(c => c is WorkspacePanel) as WorkspacePanel;
						Assert.IsNotNull(workspacePanel, "WorkspacePanel is Null");
						webcamerasControl = roomPage.AlantaControls.SingleOrDefault(c => c is WebCamerasControl) as WebCamerasControl;
						Assert.IsNotNull(webcamerasControl, "WebCamerasPanel is Null");
						loginControl = roomPage.AlantaControls.SingleOrDefault(c => c is RoomHeaderControl) as RoomHeaderControl;
						Assert.IsNotNull(loginControl, "LoginControl is Null");
						pageInitialized = true;
						TestInitializing(() => isInitializingCompleted = true);
					});

					//rb 7/7/2010 fix
					roomPage.MinWidth = 800;
					roomPage.MinHeight = 600;
					var parentPanel = roomPage.Parent as Panel;
					if (parentPanel != null)
						parentPanel.Children.Remove(roomPage);
					var scrll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = roomPage };
					TestPanel.Children.Add(scrll);
					roomPage.Initialize(new TestRoomInfo());
				}));
			});
			EnqueueConditional(() => pageInitialized && isInitializingCompleted);
			EnqueueTestComplete();
		}

		public virtual void TestInitializing(Action callback)
		{
			callback();
		}

		[TestCleanup]
		[Asynchronous]
		public void ClientTestCleanup()
		{
			TestCleaning(() => roomPage.LeaveRoom());
			EnqueueConditional(() => roomVm.SessionVm == null);
			EnqueueTestComplete();
		}

		public virtual void TestCleaning(Action callback)
		{
			callback();
		}


	}
}
