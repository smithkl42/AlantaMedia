using System;
using System.Collections.ObjectModel;
using Alanta.Client.Common.Loader;
using Alanta.Client.Data;
using Alanta.Client.Data.RoomService;
using Alanta.Client.Data.Social;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.Navigation;
using Alanta.Client.UI.Common.RoomView;
using Alanta.Client.UI.Common.ViewModels;

namespace Alanta.Client.Test
{
	public class TestController : ControllerBase, IRoomDataController
	{
		public TestController(string userTag, string loginSessionId, IViewModelFactory viewModelFactory, ICompanyInfo routingGroupInfo)
			: base(viewModelFactory, routingGroupInfo)
		{
			RoomService.CreateClient();
			LocalUserVm.UserTag = userTag;
			LocalUserVm.LoginSession = new LoginSession() { UserId = LocalUserVm.UserId, LoginSessionId = loginSessionId };
			RoomVm = viewModelFactory.GetViewModel<RoomViewModel>();
			ContactController = new ContactController(RoomService);
			ContactData = ContactData.GetContactData(ContactController, LocalUserVm.UserId, LocalUserVm.LoginSession);
			SocialData = new SocialData(RoomService, LocalUserVm.Model);
			ContactAccessors = new ObservableCollection<ContactAccessUi>();
		}

		public RoomViewModel RoomVm { get; set; }

		#region IRoomDataController Members

		public ObservableCollection<ContactAccessUi> ContactAccessors { get; set; }

		public ContactController ContactController { get; set; }

		public ContactData ContactData { get; set; }

		public SocialData SocialData { get; set; }

		#endregion

		#region ILoginController Members


		public void Login(string userId, string password, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		public void LoginWithFacebook(long facebookId, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		public void LoginWithOpenId(string identifier, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		public void LoginWithFacebook(long facebookId, string userId, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		public void LoginWithOpenId(string identifier, string userId, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		public void LoginWithTwitter(string identifier, string userId, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		public void LoginWithTwitter(string identifier, string ownerUserId, string roomName, OperationCallback callback)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
