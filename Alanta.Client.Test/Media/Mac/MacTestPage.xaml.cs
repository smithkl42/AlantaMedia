using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Navigation;
using Alanta.Client.Common.Logging;
using Alanta.Client.Media;

namespace Alanta.Client.Test.Media.Mac
{
	public partial class MacTestPage : Page
	{
		public MacTestPage()
		{
			InitializeComponent();
		}

		// Executes when the user navigates to this page.
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			txtHost.Text = HtmlPage.Document.DocumentUri.Host;
			txtRoom.Text = "ken";
			_macTestVm = new MacTestViewModel();
			DataContext = _macTestVm;
		}

		private MacTestViewModel _macTestVm;
		private List<byte[]> _sourceFrames;
		private const string filter = "PCM Files (*.pcm)|*.pcm|All Files (*.*)|*.*";
		private bool _stopExtraCpu;

		private void btnOpenTestfile_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_sourceFrames = new List<byte[]>();
				_sourceFrames.Load(filter, AudioFormat.Default.BytesPerFrame);
				if (_sourceFrames.Count > 0)
				{
					btnAnalyzeFirstFrame.IsEnabled = true;
					btnSendToRoom.IsEnabled = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void btnAnalyzeFirstFrame_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_macTestVm.AnalyzeSingleFrame(_sourceFrames);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void btnSendToRoom_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				btnSendToRoom.IsEnabled = false;
				bool sendLive = chkSendLive.IsChecked ?? false;
				bool extraCpu = chkExtraCpu.IsChecked ?? false;
				_macTestVm.StartSendingAudioToRoom(txtRoom.Text, txtHost.Text, _sourceFrames, sendLive, ex =>
				{
					if (ex == null)
					{
						btnStopSendingToRoom.IsEnabled = true;
						btnSendToRoom.IsEnabled = false;
						if (extraCpu)
						{
							ThreadPool.QueueUserWorkItem(obj => ExtraCpu());
							ThreadPool.QueueUserWorkItem(obj => ExtraCpu());
						}
					}
					else
					{
						MessageBox.Show(ex.ToString());
						btnStopSendingToRoom.IsEnabled = false;
						btnSendToRoom.IsEnabled = true;
					}
					
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void ExtraCpu()
		{
			_stopExtraCpu = false;
			double tmp = 0;
			long i;
			for (i = 0; i < long.MaxValue; i++)
			{
				tmp = Math.Sqrt(i);
				if (_stopExtraCpu)
					break;
			}
			ClientLogger.Debug("i={0}; sqrt(i)={1}", i, tmp);
		}

		private void btnStopSendingToRoom_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_stopExtraCpu = true;
				_macTestVm.StopSendingAudioToRoom();
				btnStopSendingToRoom.IsEnabled = false;
				btnSendToRoom.IsEnabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}


	}
}
