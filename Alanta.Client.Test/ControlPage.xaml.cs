using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Alanta.Client.UI.Common.Classes;

namespace Alanta.Client.Test
{
	public partial class ControlPage : Page
	{
		public ControlPage()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
		}

		private void btnClearLocalStorage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				using (var iso = IsolatedStorageFile.GetUserStoreForSite())
				{
					iso.Remove();
				}
				using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
				{
					iso.Remove();
				}
				txtResult.Text = string.Format("Storage cleared at {0}.", DateTime.Now);
			}
			catch (Exception ex)
			{
				txtResult.Text = ex.ToString();
			}
		}

	}
}
