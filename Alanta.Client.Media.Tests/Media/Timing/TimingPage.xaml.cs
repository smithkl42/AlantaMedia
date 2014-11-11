using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Alanta.Client.Common;
using Alanta.Client.Data;
using Alanta.Client.UI.Common.Classes;
using Alanta.Client.UI.Common.ViewModels;
using Alanta.Client.UI.Desktop.Classes;
using Alanta.Client.UI.Desktop.Controls;

namespace Alanta.Client.Test.Media.Timing
{
	public partial class TimingPage : Page
	{
		public TimingPage()
		{
			InitializeComponent();
			var messageService = new DesktopMessageService();
			AppMessagingAdapter.Instance.Initialize(messageService, new IsolatedStorageService());
			var roomService = new RoomServiceAdapter();
			var viewLocator = new ViewLocator();
			_viewModelFactory = new ViewModelFactory(roomService, messageService, viewLocator);
			_statsVm = _viewModelFactory.GetViewModel<StatisticsViewModel>();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			btnStart.IsEnabled = true;
			btnStop.IsEnabled = false;
			_timingViewModel = new TimingViewModel();
			DataContext = _timingViewModel;
			cboAudioContext.SelectedIndex = 0;
		}

		private TimingViewModel _timingViewModel;
		private readonly ViewModelFactory _viewModelFactory;
		private readonly StatisticsViewModel _statsVm;

		private void btnStart_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_timingViewModel.StartTimingTest();
				_statsVm.Model = _timingViewModel.MediaStatistics;
				var statsHost = new StatisticsHost();
				statisticsPanel.Children.Clear();
				statisticsPanel.Children.Add(statsHost);
				statsHost.Initialize(_statsVm);
				foreach (var counter in _statsVm.Model.Counters)
				{
					if (!counter.Name.Contains("Audio")) counter.IsActive = false;
				}
				btnStart.IsEnabled = false;
				btnStop.IsEnabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void btnStop_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_timingViewModel.StopTimingTest();
				btnStart.IsEnabled = true;
				btnStop.IsEnabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void btnSelectAudioDevice_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_timingViewModel.SelectDevices();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

	}
}
