using System;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Alanta.Client.Test
{
	public partial class MainPage : Page
	{
		public MainPage()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			//if (!string.IsNullOrEmpty(TestGlobals.InitialPage))
			//{
			//    MainFrame.Navigate(new Uri(TestGlobals.InitialPage, UriKind.Relative));
			//}
		}

	}
}
