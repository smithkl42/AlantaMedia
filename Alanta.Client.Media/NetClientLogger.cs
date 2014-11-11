using System.Threading;

namespace Alanta.Client.Media
{
    public class NetClientLogger
    {
        public NetClientLogger(MediaStatistics stats)
        {
            _timer = new Timer(TimerCallback, null, 1000, 1000);
            if (stats != null)
            {
                _dataSentCounter = stats.RegisterCounter("Network:KbpsSent");
                _dataReceivedCounter = stats.RegisterCounter("Network:KbpsReceived");
                _dataSentCounter.IsActive = false;
                _dataReceivedCounter.IsActive = false;
            }
        }

        private readonly Counter _dataSentCounter;
        private readonly Counter _dataReceivedCounter;
        private Timer _timer;
        private int _dataSent;
        private int _dataReceived;

        public void LogDataSent(int bytes)
        {
            _dataSent += bytes;
        }

        public void LogDataReceived(int bytes)
        {
            _dataReceived += bytes;
        }

        private void TimerCallback(object state)
        {
            _dataSentCounter.Update(_dataSent * 8 / 1024.0);
            _dataReceivedCounter.Update(_dataReceived * 8 / 1024.0);
            _dataSent = 0;
            _dataReceived = 0;
        }
    }
}
