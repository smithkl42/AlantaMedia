using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Browser;
using Alanta.Client.Common.Logging.Targets;

namespace Alanta.Client.Common.Logging
{
	public static class ClientLogger
	{
		#region Fields

		static readonly MemoryTarget memUiTarget = new MemoryTarget();
		static readonly MemoryTarget memTarget = new MemoryTarget(LogTargetFormat.Short);
		static readonly MemoryTarget memExtendedTarget = new MemoryTarget(LogTargetFormat.Extended);
		static readonly SilverlightDebugTarget debugTarget = new SilverlightDebugTarget();
		static readonly IsolatedStorageTarget isolatedStorageTarget = new IsolatedStorageTarget("ClientLog", LogTargetFormat.Short);
		static readonly WebPageTarget webPageTarget = new WebPageTarget();
		private static readonly List<BaseLogTarget> targets = new List<BaseLogTarget>();

		#endregion

		#region Constructors

		static ClientLogger()
		{
			targets.Add(debugTarget);
			if (DesignerProperties.IsInDesignTool) return;
			targets.Add(memTarget);
			targets.Add(memExtendedTarget);
			targets.Add(isolatedStorageTarget);
			targets.Add(webPageTarget);

			foreach (var target in targets)
			{
				target.ErrorOccured += target_ErrorOccured;
			}
		}

		static void target_ErrorOccured(object sender, TargetExceptionArgs e)
		{
			try
			{
				DateTime date = DateTime.Now;
				var stackTrace = new StackTrace();
				foreach (var target in targets)
				{
					if (target != sender)
					{
						target.DebugException(date, stackTrace, string.Format("Error on logging in {0}: ", sender.GetType()) + e.Message, e.Exception);
					}
				}

				var targetSender = (BaseLogTarget)sender;
				if (!e.IsTargetAvailable && targets.Contains(targetSender))
				{
					targets.Remove(targetSender);
					foreach (var target in targets)
					{
						if (target != sender)
						{
							target.Debug(date, stackTrace, string.Format("Removed target  {0} from log targets.", targetSender.GetType()));
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Exception occurred while handling an exception that occurred while writing a debug message (whew): {0}", ex);
			}
		}

		#endregion

		#region Methods

		public static string GetLogData()
		{
			return memTarget.GetLogData();
		}

		public static string GetExtendedLogData()
		{
			return memExtendedTarget.GetLogData();
		}

		public static string GetIsolatedStorageLogData()
		{
			return isolatedStorageTarget.GetLogData();
		}

		public static string GetLogUIData()
		{
			return memUiTarget.GetLogData();
		}

		public static void ErrorException(Exception ex)
		{
			ErrorException(ex, ex.Message);
		}

		public static void ErrorException(Exception ex, string message, params object[] values)
		{
			DateTime date = DateTime.Now;
			var stackTrace = new StackTrace();
			targets.ForEach(t => t.ErrorException(date, stackTrace, string.Format(message, values), ex));
		}

		public static void Error(string message, params object[] values)
		{
			DateTime date = DateTime.Now;
			var stackTrace = new StackTrace();
			targets.ForEach(t => t.Error(date, stackTrace, string.Format(message, values)));
		}

		[Conditional("DEBUG")]
		public static void Debug(string message, params object[] values)
		{
			DateTime date = DateTime.Now;
			var stackTrace = new StackTrace();
			targets.ForEach(t => t.Debug(date, stackTrace, string.Format(message, values)));
		}

		[Conditional("DEBUG")]
		public static void DebugException(Exception ex, string message, params object[] values)
		{
			DateTime date = DateTime.Now;
			var stackTrace = new StackTrace();
			targets.ForEach(t => t.DebugException(date, stackTrace, string.Format(message, values), ex));
		}

		[Conditional("DEBUG")]
		public static void Debug(Func<string> funcMessage, params object[] values)
		{
			DateTime date = DateTime.Now;
			string message = funcMessage.Invoke();
			var stackTrace = new StackTrace();
			targets.ForEach(t => t.Debug(date, stackTrace, string.Format(message, values)));
		}

		[Conditional("DEBUG_UI")]
		public static void DebugUI(string message, params object[] values)
		{
			var stackTrace = new StackTrace();
			DateTime date = DateTime.Now;
			memUiTarget.Debug(date, stackTrace, string.Format(message, values));
		}

		public static string GetTraceLogData()
		{
			return memTraceTarget.GetLogData();
		}

		static readonly MemoryTarget memTraceTarget = new MemoryTarget(LogTargetFormat.Extended);
		[Conditional("DEBUG")]
		public static void Trace(string message, params object[] values)
		{
			var stackTrace = new StackTrace();
			DateTime date = DateTime.Now;
			memTraceTarget.Debug(date, stackTrace, string.Format(message, values));
		}

		/// <summary>
		/// This method is called from JavaScript, so among other things, it doesn't write BACK to the web page.
		/// </summary>
		/// <param name="message"></param>
		[ScriptableMember]
		public static void LogMessage(string message)
		{
			DateTime date = DateTime.Now;
			var stackTrace = new StackTrace();
			foreach (var target in targets)
			{
				if (target != webPageTarget)
				{
					target.Debug(date, stackTrace, message);
				}
			}
		}

		#endregion

	}
}
