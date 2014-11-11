using System.Windows.Browser;
using Alanta.Client.Common.Loader;
using Alanta.Client.UI.Common.Navigation;
using Alanta.Common;

namespace Alanta.Client.Test
{
	public class TestCompanyInfo : CompanyInfoBase
	{
		public TestCompanyInfo()
		{
			CompanyDomain = HtmlPage.Document.DocumentUri.Host;
			AuthenticationGroupTag = Constants.DefaultAuthenticationGroupTag;
		}
	}
}
