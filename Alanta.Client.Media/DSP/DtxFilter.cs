using System;
using System.ComponentModel;
using System.Windows.Threading;
using Alanta.Client.Common;

namespace Alanta.Client.Media.Dsp
{
	public class DtxFilter : IDtxFilter
	{
		public readonly VoiceActivityDetector _vad;
		public short VadDecision { get; set; }
		public bool IsSilent { get; private set; }
		private readonly AudioFormat _audioFormat;

		public DtxFilter(AudioFormat audioFormat)
		{
			_audioFormat = audioFormat;
			_vad = new VoiceActivityDetector(audioFormat, VoiceActivityDetector.Aggressiveness.Normal);
		}

		public override string ToString()
		{
			return "DtxFilter:: AudioFormat:" + _audioFormat;
		}

		public class VadViewModel : INotifyPropertyChangedEx
		{
			readonly Dispatcher _dispatcher;
			public VadViewModel(Dispatcher dispatcher)
			{
				_dispatcher = dispatcher;
			}
			public event PropertyChangedEventHandler PropertyChanged;
			double _decision;
			public double Decision
			{
				get
				{
					return _decision;
				}
				set
				{
					if (_decision == value) return;
					_decision = value;
					RaisePropertyChanged("Decision");
				}
			}

			public void RaisePropertyChanged(string propertyName)
			{
				_dispatcher.BeginInvoke(() =>
				{
					if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				});
			}
		}
		VadViewModel _vadViewModel;
		public VadViewModel GetVadViewModel(Dispatcher dispatcher)
		{
			return _vadViewModel ?? (_vadViewModel = new VadViewModel(dispatcher));
		}

		public void Filter(short[] sampleData)
		{
			VadDecision = _vad.WebRtcVad_CalcVad16khz(sampleData, sampleData.Length);
			if (_vadViewModel != null) _vadViewModel.Decision = VadDecision > 0 ? 0.9 : 0;
			if (VadDecision == 0)
			{
				Array.Clear(sampleData, 0, sampleData.Length);
				IsSilent = true;
			}
			else
			{
				IsSilent = false;
			}
		}

		public string InstanceName
		{
			get;
			set;
		}
	}
}
