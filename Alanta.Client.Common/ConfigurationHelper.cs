using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Common
{
	public class ConfigurationHelper<T> where T : class, new()
	{
		private const string ConfigPrefix = "config_";

		public void SaveConfigurationObject(T config, string fileName)
		{
			SaveConfigurationObject(config, fileName, false);
		}

		private static void SaveConfigurationObject(T config, string fileName, bool lastRetry)
		{
			try
			{
				using (var stream = new IsolatedStorageFileStream(ConfigPrefix + fileName, FileMode.Create, IsolatedStorageFile.GetUserStoreForSite()))
				{
					var serializer = new XmlSerializer(typeof(T));
					serializer.Serialize(stream, config);
				}
			}
			catch (IsolatedStorageException ex)
			{
				if (!lastRetry)
				{
					// Ask user to increase IsolatedStorage and on approve try again save data.
					// Note: in catch we can't show window for increase storage, user should click first on some UI button.
					AppMessagingAdapter.Instance.TryIncreaseIsolateStorage(isStorageIncreased =>
						{
							if (isStorageIncreased)
								SaveConfigurationObject(config, fileName, true);
						});

					ClientLogger.ErrorException(ex, "Error on saving configuration file {0}", fileName);
				}
				else
				{
					ClientLogger.ErrorException(ex, "Occured error on saving data in IsolatedStorage after user increased Storage");
				}
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Unknown error on saving configuration");
			}
		}

		public T GetConfigurationObject(string fileName)
		{
			try
			{
				string filePath = ConfigPrefix + fileName;
				if (!IsolatedStorageFile.GetUserStoreForSite().FileExists(filePath))
					return new T();
				using (var stream = new IsolatedStorageFileStream(ConfigPrefix + fileName, FileMode.Open, IsolatedStorageFile.GetUserStoreForSite()))
				{
					var serializer = new XmlSerializer(typeof(T));
					return serializer.Deserialize(stream) as T;
				}
			}
			catch (FileNotFoundException ex)
			{
				ClientLogger.DebugException(ex, "The configuration file {0} was not found", fileName);
				return new T();
			}
			catch (IsolatedStorageException ex)
			{
				ClientLogger.ErrorException(ex, "The IsolatedStorage subsystem threw an error retrieving file {0}", fileName);
				return new T();
			}
			catch (InvalidOperationException ex)
			{
				ClientLogger.DebugException(ex, "InvalidOperationException error getting configuration file {0}", fileName);
				return new T();
			}
			catch (Exception ex)
			{
				ClientLogger.ErrorException(ex, "Unexpected error getting configuration file {0}", fileName);
				throw;
			}
		}
	}
}
