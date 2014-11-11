using System;
using System.Diagnostics;
using System.Text;

namespace Alanta.Client.Common.Logging.Targets
{
	public abstract class BaseLogTarget
	{
		#region Fields

		private GetMessage getMsg;
		private bool _canDebugLog = true;
		private bool _canErrorLog = true;

		#endregion

		#region Delegates

		private delegate StringBuilder GetMessage(DateTime date, StackTrace stackTrace, string message, string type);

		#endregion

		#region Constructors

		protected BaseLogTarget(LogTargetFormat format, LoggingLevel minLoggginLevel)
		{
			targetFormat = format;
			switch (targetFormat)
			{
				case LogTargetFormat.Short:
					getMsg = GetShortMsg;
					break;
				case LogTargetFormat.Extended:
					getMsg = GetExtendedMsg;
					break;
				case LogTargetFormat.ExtendedWithoutDate:
					getMsg = GetExtendedMsgWithoutDate;
					break;
			}
			MinLoggingLevel = minLoggginLevel;
		}

		#endregion

		#region Events

		public event EventHandler<TargetExceptionArgs> ErrorOccured;

		#endregion

		#region Properties

		private LogTargetFormat targetFormat;
		public LogTargetFormat TargetFormat
		{
			get
			{
				return targetFormat;
			}
		}

		protected bool _isAvailable = true;
		public bool IsAvailable
		{
			get
			{
				return _isAvailable;
			}
		}

		private LoggingLevel _minLogginLevel = LoggingLevel.Debug;
		public LoggingLevel MinLoggingLevel
		{
			get { return _minLogginLevel; }
			set
			{
				if (_minLogginLevel == value)
					return;
				_minLogginLevel = value;
				if (_minLogginLevel == LoggingLevel.Error)
				{
					_canDebugLog = false;
					_canErrorLog = true;
				}
				else
				{
					_canDebugLog = true;
					_canErrorLog = true;
				}
			}
		}

		public string Name { get; set; }

		#endregion

		#region Methods

		protected abstract void OnLog(string message);

		protected void RaiseErrorOccured(Exception exception)
		{
			if (ErrorOccured != null)
				ErrorOccured(this, new TargetExceptionArgs(exception, null, IsAvailable));
		}

		protected void RaiseErrorOccured(Exception exception, string message)
		{
			if (ErrorOccured != null)
				ErrorOccured(this, new TargetExceptionArgs(exception, message, IsAvailable));
		}

		public void Error(DateTime date, StackTrace stackTrace, string error)
		{
			if (!_isAvailable || !_canErrorLog)
				return;
			StringBuilder sb = getMsg.Invoke(date, stackTrace, error, "Error");
			OnLog(sb.ToString());
		}

		public void ErrorException(DateTime date, StackTrace stackTrace, string error, Exception exception)
		{
			if (!_isAvailable || !_canErrorLog)
				return;
			StringBuilder sb = GetErrorMessage(date, stackTrace, error, exception, "Error");
			OnLog(sb.ToString());
		}

		public void Debug(DateTime date, StackTrace stackTrace, string message)
		{
			if (!_isAvailable || !_canDebugLog)
				return;
			StringBuilder sb = getMsg.Invoke(date, stackTrace, message, "Debug");
			OnLog(sb.ToString());
		}

		public void DebugException(DateTime date, StackTrace stackTrace, string error, Exception exception)
		{
			if (!_isAvailable || !_canDebugLog)
				return;
			StringBuilder sb = GetErrorMessage(date, stackTrace, error, exception, "Debug");
			OnLog(sb.ToString());
		}

		private StringBuilder GetErrorMessage(DateTime date, StackTrace stackTrace, string message, Exception ex, string type)
		{
			StringBuilder sb = getMsg.Invoke(date, stackTrace, message, type);
			if (ex != null)
			{
				sb.AppendLine("Exception: ");
				sb.Append(ex);
			}

			return sb;
		}

		private static StringBuilder GetShortMsg(DateTime date, StackTrace stackTrace, string message, string type)
		{
			var sb = new StringBuilder();
			sb.Append(date.ToLongTimeString());
			sb.Append("|");
			sb.Append(type);
			sb.Append("|");
			if (message != null)
				sb.Append(message);
			sb.AppendLine();
			if (stackTrace.FrameCount > 1)
			{
				sb.Append(stackTrace.GetFrame(1).GetMethod().Name);
			}
			else if (stackTrace.FrameCount == 1)
			{
				sb.Append(stackTrace.GetFrame(0).GetMethod().Name);
			}

			sb.Append("();");
			return sb;
		}

		private static StringBuilder GetExtendedMsg(DateTime date, StackTrace stackTrace, string message, string type)
		{
			var sb = new StringBuilder();
			sb.Append(date.ToString());
			sb.Append("|");
			sb.Append(type);
			sb.Append("|");
			if (message != null)
				sb.Append(message);
			sb.AppendLine();
			if (stackTrace.FrameCount > 1)
			{
				for (int i = stackTrace.FrameCount - 1; i > 1; i--)
				{
					sb.Append("=>");
					sb.Append(stackTrace.GetFrame(i).GetMethod().Name);
					sb.Append("()");
				}
			}
			else if (stackTrace.FrameCount == 1)
			{
				sb.AppendLine("=>");
				sb.Append(stackTrace.GetFrame(0).GetMethod().Name);
				sb.Append("()");
			}

			sb.Append(";");
			sb.AppendLine();
			return sb;
		}

		private static StringBuilder GetExtendedMsgWithoutDate(DateTime date, StackTrace stackTrace, string message, string type)
		{
			var sb = new StringBuilder();
			sb.Append(type);
			sb.Append("|");
			if (message != null)
				sb.Append(message);
			sb.AppendLine();
			if (stackTrace.FrameCount > 1)
			{
				for (int i = stackTrace.FrameCount - 1; i > 1; i--)
				{
					sb.Append("=>");
					sb.Append(stackTrace.GetFrame(i).GetMethod().Name);
					sb.Append("()");
				}
			}
			else if (stackTrace.FrameCount == 1)
			{
				sb.AppendLine("=>");
				sb.Append(stackTrace.GetFrame(0).GetMethod().Name);
				sb.Append("()");
			}

			sb.Append(";");
			sb.AppendLine();
			return sb;
		}
		#endregion
	}
}
