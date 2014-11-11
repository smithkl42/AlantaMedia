using System;

namespace Alanta.Client.Common.Logging
{
	public class LoggingConfig
	{

		public static LoggingConfig Instance { get; set; }

		static LoggingConfig()
		{
			Instance = GetLoggingConfig();
		}

		public static LoggingConfig GetLoggingConfig()
		{
			LoggingConfig loggingConfig;
			try
			{
				string fileName = GetFileName();
				var configurationHelper = new ConfigurationHelper<LoggingConfig>();
				loggingConfig = configurationHelper.GetConfigurationObject(fileName);
			}
			catch (Exception ex)
			{
				ClientLogger.DebugException(ex, "Getting logging config failed");
				loggingConfig = new LoggingConfig();
			}
			return loggingConfig;
		}

		public static void SaveLoggingConfig(LoggingConfig loggingConfig)
		{
			try
			{

				string fileName = GetFileName();
				var configurationHelper = new ConfigurationHelper<LoggingConfig>();
				configurationHelper.SaveConfigurationObject(loggingConfig, fileName);

			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Occured error on saving login config");
			}
		}

		private static string GetFileName()
		{
			return "logging.config";
		}

		public bool EnableIsolatedStorageLogging { get; set; }
	}
}
