using System;

namespace Alanta.Client.Media
{
	public interface IMediaEnvironment
	{
		DateTime Now { get; }
		int RemoteSessions { get; set; }
		double LocalProcessorLoad { get; }
		double RemoteProcessorLoad { get; set; }
		bool IsMediaRecommended { get; }
	}
}
