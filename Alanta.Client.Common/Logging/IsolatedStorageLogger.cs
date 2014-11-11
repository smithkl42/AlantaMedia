using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;

namespace Alanta.Client.Common.Logging
{
	/// <summary>
	/// Logger, what logs into IsoalatedSorage.
	/// </summary>
	/// <remarks>
	/// If not enough space it try to remove old logs, if still not enough space then it stop working. 
	/// </remarks>
	public class IsolatedStorageLogger : IDisposable
	{
		#region Constants
		const string folderName = "AlantaLogs";
		const int minimumAvailableSpace = 524288; //512KB
		private bool _canWrite = true;

		#endregion

		#region Fields

		private StreamWriter _writer;
		private IsolatedStorageFile _storage;
		private IsolatedStorageFileStream _stream;
		private readonly string _filePrefix;
		private bool _initialized;
		#endregion

		#region Constructors

		public IsolatedStorageLogger(string filePrefix)
		{
			_filePrefix = filePrefix;
			IsAvailable = true;
		}

		#endregion

		#region Properties

		public bool IsAvailable { get; set; }

		#endregion

		#region Events

		public event EventHandler<ExceptionEventArgs> ErrorOccured;

		#endregion

		#region Methods

		public void Initialize()
		{
			InitializeIsolatedStorage();
			_initialized = true;
		}

		private string GetTargetFile()
		{
			string targetFile = string.Format(_filePrefix + "_{0}-{1}-{2}.txt", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Year);
			return targetFile;
		}

		private void InitializeIsolatedStorage()
		{
			try
			{
				string targetFile = GetTargetFile();
				_storage = IsolatedStorageFile.GetUserStoreForApplication();

				if (_storage.DirectoryExists(folderName))
				{
					// remove old log files
					var files = _storage.GetFileNames(folderName + @"\" + _filePrefix + "*");
					foreach (string file in files)
					{
						if (!file.EndsWith(targetFile))
						{
							try
							{
								_storage.DeleteFile(folderName + @"\" + file);
							}
							catch (Exception ex)
							{
								// can't delete file, it can be opened. It's not critical for us.
								RaiseErrorOccured(ex, "Can't delete file '{0}' from IsolatedStorage");
							}
						}
					}
				}
				else
				{
					_storage.CreateDirectory(folderName);
				}

				if (_storage.AvailableFreeSpace < minimumAvailableSpace)
				{
					try
					{
						// remove current log file
						if (_storage.FileExists(folderName + @"\" + targetFile))
						{
							_storage.DeleteFile(folderName + @"\" + targetFile);
						}
					}
					catch (Exception ex)
					{
						// can't delete file, it can be opened. It's not critical for us.
						RaiseErrorOccured(ex, string.Format("Can't delete file '{0}' from IsolatedStorage", targetFile));
					}

					// check again is enough space
					if (_storage.AvailableFreeSpace < minimumAvailableSpace)
					{
						// Not enoug available free space, disable logging to Isolated Storage.
						double availableSpace = _storage.AvailableFreeSpace;
						Dispose();
						RaiseErrorOccured(null, string.Format("Not enough space in IsolatedStorage for log: Available={0}, Needed={1}", availableSpace, minimumAvailableSpace));
						return;
					}
				}

				_stream = new IsolatedStorageFileStream(folderName + @"\" + targetFile, FileMode.Append,
													   FileAccess.Write, FileShare.ReadWrite, _storage);
				_writer = new StreamWriter(_stream);
				_writer.AutoFlush = true;
			}
			catch (Exception ex)
			{
				Dispose();
				RaiseErrorOccured(ex, "Can't initialize logging to IsolatedStorage: ");
			}
		}

		private void RaiseErrorOccured(Exception exception, string message)
		{
			if (ErrorOccured != null)
				ErrorOccured(this, new ExceptionEventArgs(exception, message));
		}

		public string GetData()
		{
			string targetFile = GetTargetFile();
			string filePath = folderName + @"\" + targetFile;
			try
			{
				using (var storageRead = IsolatedStorageFile.GetUserStoreForApplication())
				{
					using (var streamRead = new IsolatedStorageFileStream(filePath, FileMode.Open,
															FileAccess.Read, FileShare.ReadWrite, storageRead))
					{
						using (var reader = new StreamReader(streamRead))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}
			catch (FileNotFoundException ex)
			{
				RaiseErrorOccured(ex, "File {0} was not found : {1}");
				return string.Format("File {0} was not found : {1}", filePath, ex);
			}
			catch (Exception ex)
			{
				RaiseErrorOccured(ex, "Occured error on getting data from {0} : ");
				return string.Format("Occured error on getting data from {0} : " + ex, filePath);
			}
		}

		public void WriteLine(string message)
		{
			if (!_initialized)
			{
				_initialized = true;
				throw new InvalidOperationException("IsolatedStorageLogger.Initialize() must be called before using logging");
			}
			if (!_canWrite || !LoggingConfig.Instance.EnableIsolatedStorageLogging)
				return;
			try
			{
				lock (_writer)
				{
					_writer.WriteLine(message);
				}
			}
			catch (Exception ex)
			{
				Dispose();
				RaiseErrorOccured(ex, "Occured error on writing loggin to IsolatedStorage: ");
			}
		}

		#region IDisposable Members

		private bool _disposed;

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				IsAvailable = false;
				try
				{
					_canWrite = false;
					if (_writer != null)
					{
						lock (_writer)
						{
							_writer.Close();
						}
					}
					if (_stream != null)
						_stream.Close();
					if (_storage != null)
						_storage.Dispose();
				}
				catch(Exception ex)
				{
					Debug.WriteLine(ex.ToString());
				}
				finally
				{
					_disposed = true;
				}
			}
		}

		#endregion

		#endregion
	}
}
