namespace Alanta.Client.Common.Logging.Targets
{
	public class IsolatedStorageTarget : BaseLogTarget
	{
		private readonly IsolatedStorageLogger logger;

		public IsolatedStorageTarget(string filePrefix)
			: this(filePrefix, LogTargetFormat.Extended)
		{
		}

		public IsolatedStorageTarget(string filePrefix, LogTargetFormat format)
			: base(format, LoggingLevel.Debug)
		{
			logger = new IsolatedStorageLogger(filePrefix);
			logger.ErrorOccured += logger_ErrorOccured;
			logger.Initialize();
		}

		void logger_ErrorOccured(object sender, ExceptionEventArgs e)
		{
			_isAvailable = logger.IsAvailable;
			RaiseErrorOccured(e.Exception, e.Message);
		}

		protected override void OnLog(string message)
		{
			logger.WriteLine(message);
		}

		public string GetLogData()
		{
			return logger.GetData();
		}
	}
}
