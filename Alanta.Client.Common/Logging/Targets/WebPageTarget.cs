using System;
using System.Windows;
using System.Windows.Browser;

namespace Alanta.Client.Common.Logging.Targets
{
	public class WebPageTarget : BaseLogTarget
	{
		public WebPageTarget()
			: base(LogTargetFormat.Short, LoggingLevel.Debug)
		{

		}

		public WebPageTarget(LogTargetFormat format)
			: base(format, LoggingLevel.Debug)
		{

		}

		protected override void OnLog(string message)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					var window = HtmlPage.Window;
					var isConsoleAvailable = (bool)window.Eval("typeof(console) != 'undefined' && typeof(console.log) != 'undefined'");
					if (isConsoleAvailable)
					{
						var console = (window.Eval("console.log") as ScriptObject);
						if (console != null)
						{
							console.InvokeSelf(message);
						}
					}
					else
					{
						_isAvailable = false;
						RaiseErrorOccured(new Exception("Console of Browser isn't available."));
					}
				}
				catch (Exception ex)
				{
					_isAvailable = false;
					RaiseErrorOccured(ex, "Occured error on logging message in console of Browser.");
				}
			});
		}
	}
}
