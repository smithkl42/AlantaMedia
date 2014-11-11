using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Alanta.Client.Media;
using ReactiveUI;

namespace Alanta.Client.Test.Media.MediaServer
{
	public class MediaServerViewModel : ReactiveObject
	{
		#region Constructors
		public MediaServerViewModel(MediaConfig mediaConfig, AudioFormat audioFormat, MediaStatistics mediaStatistics, MediaEnvironment mediaEnvironment, IMediaConnection mediaConnection, IVideoQualityController videoQualityController, string roomId)
		{
			MediaController = new MediaController(mediaConfig, audioFormat, mediaStatistics, mediaEnvironment, mediaConnection, videoQualityController);
			RoomId = roomId;
			MediaServerKpis = new ObservableCollection<MediaServerKpi>();
			MediaController.MediaStats.Counters.CollectionChanged += Counters_CollectionChanged;
		}

		void Counters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			foreach (Counter counter in e.NewItems)
			{
				if (counter.Name == "AudioQueue.Length") MediaServerKpis.Add(new MediaServerKpi(counter, 1, 40));
				if (counter.Name == "AudioQueue.EmptyReads%") MediaServerKpis.Add(new MediaServerKpi(counter, 0, 5));
				if (counter.Name == "AudioQueue.FullWrites%") MediaServerKpis.Add(new MediaServerKpi(counter, 0, 1));
				if (counter.Name == "AudioQueue.PacketsOutOfOrder%") MediaServerKpis.Add(new MediaServerKpi(counter, 0, 1));
				if (counter.Name == "Audio:Duplicate SequenceNumbers") MediaServerKpis.Add(new MediaServerKpi(counter, 0, 0));
				if (counter.Name == "Audio:FrequencyMismatch") MediaServerKpis.Add(new MediaServerKpi(counter, 0, 0));
			}
		}

		#endregion

		#region Fields and Properties

		public MediaController MediaController { get; private set; }
		public ObservableCollection<MediaServerKpi> MediaServerKpis { get; private set; }
		public ushort SsrcId
		{
			get { return MediaController.MediaConfig.LocalSsrcId; }
		}

		public string RoomId { get; set; }

		#endregion

		#region Methods

		public void Connect()
		{
			MediaController.Connect(RoomId);
		}

		public void Disconnect()
		{
			MediaController.MediaStats.Counters.CollectionChanged -= Counters_CollectionChanged;
			foreach (var mediaServerKpi in MediaServerKpis)
			{
				mediaServerKpi.Dispose();
			}
			MediaController.Dispose();
		}

		#endregion

	}
}
